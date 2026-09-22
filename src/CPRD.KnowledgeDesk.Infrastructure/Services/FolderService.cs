using CPRD.KnowledgeDesk.Core.Models;
using CPRD.KnowledgeDesk.Core.Services;
using CPRD.KnowledgeDesk.Infrastructure.Data;
using CPRD.KnowledgeDesk.Infrastructure.Repositories;
using Microsoft.Data.Sqlite;

namespace CPRD.KnowledgeDesk.Infrastructure.Services;

public sealed class FolderService : IFolderService
{
    private readonly FolderRepository _folders;
    private readonly SearchRepository _search;
    private readonly KnowledgeDb _db;

    public FolderService(FolderRepository folders, SearchRepository search, KnowledgeDb db)
    {
        _folders = folders;
        _search = search;
        _db = db;
    }

    public Task<IReadOnlyList<Folder>> GetTreeAsync(CancellationToken cancellationToken) =>
        _folders.GetAllAsync(cancellationToken);

    public Task<Folder?> FindByPathAsync(string path, CancellationToken cancellationToken) =>
        _folders.FindByPathAsync(path, cancellationToken);

    public Task<Folder> CreateAsync(Guid? parentId, string name, CancellationToken cancellationToken) =>
        _folders.CreateAsync(parentId, name, cancellationToken);

    public async Task RenameAsync(Guid folderId, string newName, CancellationToken cancellationToken)
    {
        await using var connection = _db.OpenConnection();
        await connection.OpenAsync(cancellationToken);
        await using var transaction = (SqliteTransaction)await connection.BeginTransactionAsync(cancellationToken);
        await _folders.RenameAsync(connection, transaction, folderId, newName, cancellationToken);
        await _search.RefreshFolderSubtreeAsync(connection, transaction, folderId, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task MoveAsync(Guid folderId, Guid? newParentId, CancellationToken cancellationToken)
    {
        if (newParentId == folderId)
            throw new InvalidOperationException("A folder cannot be moved under itself.");

        if (newParentId is not null)
        {
            var descendants = await _folders.GetDescendantIdsAsync(folderId, cancellationToken);
            if (descendants.Contains(newParentId.Value))
                throw new InvalidOperationException("A folder cannot be moved under one of its descendants.");
        }

        await using var connection = _db.OpenConnection();
        await connection.OpenAsync(cancellationToken);
        await using var transaction = (SqliteTransaction)await connection.BeginTransactionAsync(cancellationToken);
        await _folders.MoveAsync(connection, transaction, folderId, newParentId, cancellationToken);
        await _search.RefreshFolderSubtreeAsync(connection, transaction, folderId, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public Task ArchiveAsync(Guid folderId, CancellationToken cancellationToken) =>
        _folders.SetArchivedAsync(folderId, true, cancellationToken);

    public Task RestoreAsync(Guid folderId, CancellationToken cancellationToken) =>
        _folders.SetArchivedAsync(folderId, false, cancellationToken);

    public async Task DeleteAsync(
        Guid folderId,
        Guid destinationFolderId,
        CancellationToken cancellationToken)
    {
        if (folderId == destinationFolderId)
            throw new InvalidOperationException("A folder cannot be deleted into itself.");

        var descendants = await _folders.GetDescendantIdsAsync(folderId, cancellationToken);
        if (descendants.Contains(destinationFolderId))
            throw new InvalidOperationException(
                "Choose a destination outside the folder being deleted.");

        await using var connection = _db.OpenConnection();
        await connection.OpenAsync(cancellationToken);
        await using var transaction =
            (SqliteTransaction)await connection.BeginTransactionAsync(cancellationToken);

        var parentId = await GetParentIdAsync(
            connection,
            transaction,
            folderId,
            cancellationToken);

        await EnsureActiveFolderExistsAsync(
            connection,
            transaction,
            destinationFolderId,
            cancellationToken);

        var movedNoteIds = await LoadIdsAsync(
            connection,
            transaction,
            "SELECT id FROM notes WHERE folder_id=$folderId",
            "$folderId",
            folderId,
            cancellationToken);

        var childFolderIds = await LoadIdsAsync(
            connection,
            transaction,
            "SELECT id FROM folders WHERE parent_id=$folderId",
            "$folderId",
            folderId,
            cancellationToken);

        await using (var moveNotes = connection.CreateCommand())
        {
            moveNotes.Transaction = transaction;
            moveNotes.CommandText = """
                UPDATE notes
                   SET folder_id=$destination,
                       modified_at_utc=$modified
                 WHERE folder_id=$folderId
                """;
            moveNotes.Parameters.AddWithValue("$destination", destinationFolderId.ToString("D"));
            moveNotes.Parameters.AddWithValue("$folderId", folderId.ToString("D"));
            moveNotes.Parameters.AddWithValue("$modified", DateTimeOffset.UtcNow.ToString("O"));
            await moveNotes.ExecuteNonQueryAsync(cancellationToken);
        }

        await using (var reparentChildren = connection.CreateCommand())
        {
            reparentChildren.Transaction = transaction;
            reparentChildren.CommandText = """
                UPDATE folders
                   SET parent_id=$parentId,
                       modified_at_utc=$modified
                 WHERE parent_id=$folderId
                """;
            reparentChildren.Parameters.AddWithValue(
                "$parentId",
                parentId?.ToString("D") ?? (object)DBNull.Value);
            reparentChildren.Parameters.AddWithValue("$folderId", folderId.ToString("D"));
            reparentChildren.Parameters.AddWithValue("$modified", DateTimeOffset.UtcNow.ToString("O"));

            try
            {
                await reparentChildren.ExecuteNonQueryAsync(cancellationToken);
            }
            catch (SqliteException ex) when (ex.SqliteErrorCode == 19)
            {
                throw new InvalidOperationException(
                    "A child folder has the same name as a folder at the destination level. Rename it before deleting this folder.",
                    ex);
            }
        }

        await using (var delete = connection.CreateCommand())
        {
            delete.Transaction = transaction;
            delete.CommandText = "DELETE FROM folders WHERE id=$id";
            delete.Parameters.AddWithValue("$id", folderId.ToString("D"));
            if (await delete.ExecuteNonQueryAsync(cancellationToken) == 0)
                throw new KeyNotFoundException($"Folder '{folderId}' was not found.");
        }

        foreach (var noteId in movedNoteIds)
            await _search.RefreshAsync(connection, transaction, noteId, cancellationToken);

        foreach (var childId in childFolderIds)
            await _search.RefreshFolderSubtreeAsync(
                connection,
                transaction,
                childId,
                cancellationToken);

        await transaction.CommitAsync(cancellationToken);
    }

    private static async Task<Guid?> GetParentIdAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        Guid folderId,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "SELECT parent_id FROM folders WHERE id=$id";
        command.Parameters.AddWithValue("$id", folderId.ToString("D"));
        var value = await command.ExecuteScalarAsync(cancellationToken);

        if (value is null)
            throw new KeyNotFoundException($"Folder '{folderId}' was not found.");

        return value is DBNull ? null : Guid.Parse(Convert.ToString(value)!);
    }

    private static async Task EnsureActiveFolderExistsAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        Guid folderId,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            SELECT COUNT(*)
              FROM folders
             WHERE id=$id
               AND is_archived=0
            """;
        command.Parameters.AddWithValue("$id", folderId.ToString("D"));
        if (Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken)) == 0)
            throw new KeyNotFoundException("The selected destination folder is not available.");
    }

    private static async Task<IReadOnlyList<Guid>> LoadIdsAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        string sql,
        string parameterName,
        Guid parameterValue,
        CancellationToken cancellationToken)
    {
        var result = new List<Guid>();
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = sql;
        command.Parameters.AddWithValue(parameterName, parameterValue.ToString("D"));
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
            result.Add(Guid.Parse(reader.GetString(0)));
        return result;
    }
}
