using CPRD.KnowledgeDesk.Core.Models;
using CPRD.KnowledgeDesk.Infrastructure.Data;
using Microsoft.Data.Sqlite;

namespace CPRD.KnowledgeDesk.Infrastructure.Repositories;

public sealed class FolderRepository
{
    private readonly KnowledgeDb _db;

    public FolderRepository(KnowledgeDb db) => _db = db;

    public async Task<IReadOnlyList<Folder>> GetAllAsync(CancellationToken cancellationToken)
    {
        var result = new List<Folder>();
        await using var connection = _db.OpenConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT id,parent_id,name,sort_order,is_archived,created_at_utc,modified_at_utc
              FROM folders
             ORDER BY COALESCE(parent_id,''),sort_order,name COLLATE NOCASE
            """;
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken)) result.Add(Read(reader));
        return result;
    }

    public async Task<Folder?> FindByPathAsync(string path, CancellationToken cancellationToken)
    {
        await using var connection = _db.OpenConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            WITH RECURSIVE folder_paths(id,parent_id,name,sort_order,is_archived,created_at_utc,modified_at_utc,path) AS (
              SELECT id,parent_id,name,sort_order,is_archived,created_at_utc,modified_at_utc,name
                FROM folders WHERE parent_id IS NULL
              UNION ALL
              SELECT f.id,f.parent_id,f.name,f.sort_order,f.is_archived,f.created_at_utc,f.modified_at_utc,
                     fp.path || ' > ' || f.name
                FROM folders f JOIN folder_paths fp ON f.parent_id=fp.id
            )
            SELECT id,parent_id,name,sort_order,is_archived,created_at_utc,modified_at_utc
              FROM folder_paths WHERE path=$path COLLATE NOCASE
            """;
        command.Parameters.AddWithValue("$path", NormalizePath(path));
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? Read(reader) : null;
    }

    public async Task<Folder> CreateAsync(Guid? parentId, string name, CancellationToken cancellationToken)
    {
        var normalizedName = NormalizeName(name);
        if (normalizedName.Length == 0) throw new ArgumentException("Folder name is required.", nameof(name));

        var now = DateTimeOffset.UtcNow;
        var folder = new Folder(Guid.NewGuid(), parentId, normalizedName, await NextSortOrderAsync(parentId, cancellationToken), false, now, now);
        await using var connection = _db.OpenConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO folders(id,parent_id,name,sort_order,is_archived,created_at_utc,modified_at_utc)
            VALUES($id,$parentId,$name,$sortOrder,0,$created,$modified)
            """;
        command.Parameters.AddWithValue("$id", folder.Id.ToString("D"));
        command.Parameters.AddWithValue("$parentId", parentId?.ToString("D") ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("$name", folder.Name);
        command.Parameters.AddWithValue("$sortOrder", folder.SortOrder);
        command.Parameters.AddWithValue("$created", now.ToString("O"));
        command.Parameters.AddWithValue("$modified", now.ToString("O"));
        try
        {
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
        catch (SqliteException ex) when (ex.SqliteErrorCode == 19)
        {
            throw new InvalidOperationException($"A folder named '{folder.Name}' already exists at this level.", ex);
        }
        return folder;
    }

    public async Task RenameAsync(SqliteConnection connection, SqliteTransaction transaction, Guid folderId, string newName, CancellationToken cancellationToken)
    {
        var name = NormalizeName(newName);
        if (name.Length == 0) throw new ArgumentException("Folder name is required.", nameof(newName));
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "UPDATE folders SET name=$name,modified_at_utc=$modified WHERE id=$id";
        command.Parameters.AddWithValue("$id", folderId.ToString("D"));
        command.Parameters.AddWithValue("$name", name);
        command.Parameters.AddWithValue("$modified", DateTimeOffset.UtcNow.ToString("O"));
        try
        {
            if (await command.ExecuteNonQueryAsync(cancellationToken) == 0)
                throw new KeyNotFoundException($"Folder '{folderId}' was not found.");
        }
        catch (SqliteException ex) when (ex.SqliteErrorCode == 19)
        {
            throw new InvalidOperationException($"A folder named '{name}' already exists at this level.", ex);
        }
    }

    public async Task MoveAsync(SqliteConnection connection, SqliteTransaction transaction, Guid folderId, Guid? newParentId, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "UPDATE folders SET parent_id=$parentId,modified_at_utc=$modified WHERE id=$id";
        command.Parameters.AddWithValue("$id", folderId.ToString("D"));
        command.Parameters.AddWithValue("$parentId", newParentId?.ToString("D") ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("$modified", DateTimeOffset.UtcNow.ToString("O"));
        try
        {
            if (await command.ExecuteNonQueryAsync(cancellationToken) == 0)
                throw new KeyNotFoundException($"Folder '{folderId}' was not found.");
        }
        catch (SqliteException ex) when (ex.SqliteErrorCode == 19)
        {
            throw new InvalidOperationException("The destination already contains a folder with this name.", ex);
        }
    }

    public async Task SetArchivedAsync(Guid folderId, bool archived, CancellationToken cancellationToken)
    {
        await using var connection = _db.OpenConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "UPDATE folders SET is_archived=$archived,modified_at_utc=$modified WHERE id=$id";
        command.Parameters.AddWithValue("$id", folderId.ToString("D"));
        command.Parameters.AddWithValue("$archived", archived ? 1 : 0);
        command.Parameters.AddWithValue("$modified", DateTimeOffset.UtcNow.ToString("O"));
        if (await command.ExecuteNonQueryAsync(cancellationToken) == 0)
            throw new KeyNotFoundException($"Folder '{folderId}' was not found.");
    }

    public async Task<IReadOnlySet<Guid>> GetDescendantIdsAsync(Guid folderId, CancellationToken cancellationToken)
    {
        var result = new HashSet<Guid>();
        await using var connection = _db.OpenConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            WITH RECURSIVE descendants(id) AS (
              SELECT id FROM folders WHERE parent_id=$folderId
              UNION ALL
              SELECT f.id FROM folders f JOIN descendants d ON f.parent_id=d.id
            )
            SELECT id FROM descendants
            """;
        command.Parameters.AddWithValue("$folderId", folderId.ToString("D"));
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken)) result.Add(Guid.Parse(reader.GetString(0)));
        return result;
    }

    private async Task<int> NextSortOrderAsync(Guid? parentId, CancellationToken cancellationToken)
    {
        await using var connection = _db.OpenConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = parentId is null
            ? "SELECT COALESCE(MAX(sort_order),-1)+1 FROM folders WHERE parent_id IS NULL"
            : "SELECT COALESCE(MAX(sort_order),-1)+1 FROM folders WHERE parent_id=$parentId";
        if (parentId is not null) command.Parameters.AddWithValue("$parentId", parentId.Value.ToString("D"));
        return Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken));
    }

    private static Folder Read(SqliteDataReader reader) => new(
        Guid.Parse(reader.GetString(0)),
        reader.IsDBNull(1) ? null : Guid.Parse(reader.GetString(1)),
        reader.GetString(2),
        reader.GetInt32(3),
        reader.GetInt64(4) != 0,
        DateTimeOffset.Parse(reader.GetString(5)),
        DateTimeOffset.Parse(reader.GetString(6)));

    private static string NormalizeName(string? name) =>
        string.Join(' ', (name ?? string.Empty).Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries));

    private static string NormalizePath(string? path) =>
        string.Join(" > ", (path ?? string.Empty).Split('>', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
}
