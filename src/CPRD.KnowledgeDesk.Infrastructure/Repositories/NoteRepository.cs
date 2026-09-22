using CPRD.KnowledgeDesk.Core.Models;
using CPRD.KnowledgeDesk.Core.Services;
using CPRD.KnowledgeDesk.Infrastructure.Data;
using Microsoft.Data.Sqlite;

namespace CPRD.KnowledgeDesk.Infrastructure.Repositories;

public sealed class NoteRepository
{
    private readonly KnowledgeDb _db;

    public NoteRepository(KnowledgeDb db) => _db = db;

    public async Task InsertAsync(SqliteConnection connection, SqliteTransaction transaction, Note note, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            INSERT INTO notes(
              id,title,content_package,plain_text,folder_id,note_type,is_favorite,is_pinned,is_archived,
              created_at_utc,modified_at_utc,last_opened_at_utc,deleted_at_utc,source_type,source_reference,structured_json)
            VALUES(
              $id,$title,$content,$plain,$folder,$type,$favorite,$pinned,$archived,
              $created,$modified,$opened,$deleted,$sourceType,$sourceReference,$structured)
            """;
        BindNote(command, note);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task UpdateAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        UpdateNoteRequest request,
        DateTimeOffset modifiedAtUtc,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            UPDATE notes
               SET title=$title,
                   content_package=$content,
                   plain_text=$plain,
                   folder_id=$folder,
                   note_type=$type,
                   is_favorite=$favorite,
                   is_pinned=$pinned,
                   structured_json=$structured,
                   modified_at_utc=$modified
             WHERE id=$id AND deleted_at_utc IS NULL
            """;
        command.Parameters.AddWithValue("$id", request.Id.ToString("D"));
        command.Parameters.AddWithValue("$title", NormalizeTitle(request.Title));
        command.Parameters.AddWithValue("$content", request.ContentPackage ?? string.Empty);
        command.Parameters.AddWithValue("$plain", request.PlainText ?? string.Empty);
        command.Parameters.AddWithValue("$folder", request.FolderId.ToString("D"));
        command.Parameters.AddWithValue("$type", string.IsNullOrWhiteSpace(request.NoteType) ? "Standard Note" : request.NoteType.Trim());
        command.Parameters.AddWithValue("$favorite", request.IsFavorite ? 1 : 0);
        command.Parameters.AddWithValue("$pinned", request.IsPinned ? 1 : 0);
        command.Parameters.AddWithValue("$structured", string.IsNullOrWhiteSpace(request.StructuredJson) ? "{}" : request.StructuredJson);
        command.Parameters.AddWithValue("$modified", modifiedAtUtc.ToString("O"));
        var rows = await command.ExecuteNonQueryAsync(cancellationToken);
        if (rows == 0)
            throw new KeyNotFoundException($"Note '{request.Id}' was not found.");
    }

    public async Task SyncTagsAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        Guid noteId,
        IReadOnlyList<string> tags,
        CancellationToken cancellationToken)
    {
        await using (var delete = connection.CreateCommand())
        {
            delete.Transaction = transaction;
            delete.CommandText = "DELETE FROM note_tags WHERE note_id=$noteId";
            delete.Parameters.AddWithValue("$noteId", noteId.ToString("D"));
            await delete.ExecuteNonQueryAsync(cancellationToken);
        }

        foreach (var raw in tags)
        {
            var display = CollapseWhitespace(raw);
            if (display.Length == 0) continue;
            var normalized = display.ToUpperInvariant();
            var tagId = Guid.NewGuid();

            await using (var insertTag = connection.CreateCommand())
            {
                insertTag.Transaction = transaction;
                insertTag.CommandText = """
                    INSERT INTO tags(id,name,normalized_name)
                    VALUES($id,$name,$normalized)
                    ON CONFLICT(normalized_name) DO NOTHING
                    """;
                insertTag.Parameters.AddWithValue("$id", tagId.ToString("D"));
                insertTag.Parameters.AddWithValue("$name", display);
                insertTag.Parameters.AddWithValue("$normalized", normalized);
                await insertTag.ExecuteNonQueryAsync(cancellationToken);
            }

            await using var link = connection.CreateCommand();
            link.Transaction = transaction;
            link.CommandText = """
                INSERT OR IGNORE INTO note_tags(note_id,tag_id)
                SELECT $noteId,id FROM tags WHERE normalized_name=$normalized
                """;
            link.Parameters.AddWithValue("$noteId", noteId.ToString("D"));
            link.Parameters.AddWithValue("$normalized", normalized);
            await link.ExecuteNonQueryAsync(cancellationToken);
        }
    }

    public async Task<Note?> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        await using var connection = _db.OpenConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT * FROM notes WHERE id=$id AND deleted_at_utc IS NULL";
        command.Parameters.AddWithValue("$id", id.ToString("D"));
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? Read(reader) : null;
    }

    public async Task<IReadOnlyList<string>> GetTagsAsync(Guid noteId, CancellationToken cancellationToken)
    {
        var result = new List<string>();
        await using var connection = _db.OpenConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT t.name
              FROM note_tags nt
              JOIN tags t ON t.id=nt.tag_id
             WHERE nt.note_id=$noteId
             ORDER BY t.name COLLATE NOCASE
            """;
        command.Parameters.AddWithValue("$noteId", noteId.ToString("D"));
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
            result.Add(reader.GetString(0));
        return result;
    }

    public async Task<IReadOnlyList<Note>> ListByFolderAsync(Guid folderId, CancellationToken cancellationToken)
    {
        var result = new List<Note>();
        await using var connection = _db.OpenConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT * FROM notes
             WHERE folder_id=$folderId AND deleted_at_utc IS NULL AND is_archived=0
             ORDER BY is_pinned DESC, modified_at_utc DESC
            """;
        command.Parameters.AddWithValue("$folderId", folderId.ToString("D"));
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken)) result.Add(Read(reader));
        return result;
    }

    public async Task MarkOpenedAsync(Guid id, DateTimeOffset openedAtUtc, CancellationToken cancellationToken)
    {
        await using var connection = _db.OpenConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "UPDATE notes SET last_opened_at_utc=$opened WHERE id=$id AND deleted_at_utc IS NULL";
        command.Parameters.AddWithValue("$id", id.ToString("D"));
        command.Parameters.AddWithValue("$opened", openedAtUtc.ToString("O"));
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task SetArchivedAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        Guid id,
        bool archived,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "UPDATE notes SET is_archived=$archived, modified_at_utc=$modified WHERE id=$id AND deleted_at_utc IS NULL";
        command.Parameters.AddWithValue("$id", id.ToString("D"));
        command.Parameters.AddWithValue("$archived", archived ? 1 : 0);
        command.Parameters.AddWithValue("$modified", DateTimeOffset.UtcNow.ToString("O"));
        if (await command.ExecuteNonQueryAsync(cancellationToken) == 0)
            throw new KeyNotFoundException($"Note '{id}' was not found.");
    }

    public async Task SetDeletedAtAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        Guid id,
        DateTimeOffset? deletedAtUtc,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "UPDATE notes SET deleted_at_utc=$deleted, modified_at_utc=$modified WHERE id=$id";
        command.Parameters.AddWithValue("$id", id.ToString("D"));
        command.Parameters.AddWithValue("$deleted", deletedAtUtc?.ToString("O") ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("$modified", DateTimeOffset.UtcNow.ToString("O"));
        if (await command.ExecuteNonQueryAsync(cancellationToken) == 0)
            throw new KeyNotFoundException($"Note '{id}' was not found.");
    }

    public async Task DeleteAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        Guid id,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "DELETE FROM notes WHERE id=$id";
        command.Parameters.AddWithValue("$id", id.ToString("D"));
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Guid>> GetTrashIdsOlderThanAsync(
        DateTimeOffset cutoffUtc,
        CancellationToken cancellationToken)
    {
        var result = new List<Guid>();
        await using var connection = _db.OpenConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT id FROM notes WHERE deleted_at_utc IS NOT NULL AND deleted_at_utc < $cutoff";
        command.Parameters.AddWithValue("$cutoff", cutoffUtc.ToString("O"));
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
            result.Add(Guid.Parse(reader.GetString(0)));
        return result;
    }

    public async Task<IReadOnlyList<Note>> ListTrashAsync(CancellationToken cancellationToken)
    {
        var result = new List<Note>();
        await using var connection = _db.OpenConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT * FROM notes WHERE deleted_at_utc IS NOT NULL ORDER BY deleted_at_utc DESC";
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken)) result.Add(Read(reader));
        return result;
    }

    private static void BindNote(SqliteCommand command, Note note)
    {
        command.Parameters.AddWithValue("$id", note.Id.ToString("D"));
        command.Parameters.AddWithValue("$title", NormalizeTitle(note.Title));
        command.Parameters.AddWithValue("$content", note.ContentPackage ?? string.Empty);
        command.Parameters.AddWithValue("$plain", note.PlainText ?? string.Empty);
        command.Parameters.AddWithValue("$folder", note.FolderId.ToString("D"));
        command.Parameters.AddWithValue("$type", string.IsNullOrWhiteSpace(note.NoteType) ? "Standard Note" : note.NoteType.Trim());
        command.Parameters.AddWithValue("$favorite", note.IsFavorite ? 1 : 0);
        command.Parameters.AddWithValue("$pinned", note.IsPinned ? 1 : 0);
        command.Parameters.AddWithValue("$archived", note.IsArchived ? 1 : 0);
        command.Parameters.AddWithValue("$created", note.CreatedAtUtc.ToString("O"));
        command.Parameters.AddWithValue("$modified", note.ModifiedAtUtc.ToString("O"));
        command.Parameters.AddWithValue("$opened", note.LastOpenedAtUtc?.ToString("O") ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("$deleted", note.DeletedAtUtc?.ToString("O") ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("$sourceType", note.SourceType ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("$sourceReference", note.SourceReference ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("$structured", string.IsNullOrWhiteSpace(note.StructuredJson) ? "{}" : note.StructuredJson);
    }

    private static Note Read(SqliteDataReader reader) => new(
        Guid.Parse(reader.GetString(reader.GetOrdinal("id"))),
        reader.GetString(reader.GetOrdinal("title")),
        reader.GetString(reader.GetOrdinal("content_package")),
        reader.GetString(reader.GetOrdinal("plain_text")),
        Guid.Parse(reader.GetString(reader.GetOrdinal("folder_id"))),
        reader.GetString(reader.GetOrdinal("note_type")),
        reader.GetInt64(reader.GetOrdinal("is_favorite")) != 0,
        reader.GetInt64(reader.GetOrdinal("is_pinned")) != 0,
        reader.GetInt64(reader.GetOrdinal("is_archived")) != 0,
        DateTimeOffset.Parse(reader.GetString(reader.GetOrdinal("created_at_utc"))),
        DateTimeOffset.Parse(reader.GetString(reader.GetOrdinal("modified_at_utc"))),
        ReadNullableDate(reader, "last_opened_at_utc"),
        ReadNullableDate(reader, "deleted_at_utc"),
        ReadNullableString(reader, "source_type"),
        ReadNullableString(reader, "source_reference"),
        reader.GetString(reader.GetOrdinal("structured_json")));

    private static DateTimeOffset? ReadNullableDate(SqliteDataReader reader, string name)
    {
        var ordinal = reader.GetOrdinal(name);
        return reader.IsDBNull(ordinal) ? null : DateTimeOffset.Parse(reader.GetString(ordinal));
    }

    private static string? ReadNullableString(SqliteDataReader reader, string name)
    {
        var ordinal = reader.GetOrdinal(name);
        return reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
    }

    private static string NormalizeTitle(string? title) => string.IsNullOrWhiteSpace(title) ? "Untitled Note" : title.Trim();

    private static string CollapseWhitespace(string? value) =>
        string.Join(' ', (value ?? string.Empty).Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries));
}
