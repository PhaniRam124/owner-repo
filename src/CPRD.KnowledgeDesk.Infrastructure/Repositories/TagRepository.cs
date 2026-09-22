using CPRD.KnowledgeDesk.Core.Models;
using CPRD.KnowledgeDesk.Infrastructure.Data;
using Microsoft.Data.Sqlite;

namespace CPRD.KnowledgeDesk.Infrastructure.Repositories;

public sealed class TagRepository
{
    private readonly KnowledgeDb _db;

    public TagRepository(KnowledgeDb db) => _db = db;

    public async Task<IReadOnlyList<Tag>> GetAllAsync(CancellationToken cancellationToken)
    {
        var result = new List<Tag>();
        await using var connection = _db.OpenConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT id,name,normalized_name FROM tags ORDER BY name COLLATE NOCASE";
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
            result.Add(new Tag(Guid.Parse(reader.GetString(0)), reader.GetString(1), reader.GetString(2)));
        return result;
    }

    public async Task<Tag> GetOrCreateAsync(string name, CancellationToken cancellationToken)
    {
        var display = NormalizeDisplay(name);
        if (display.Length == 0) throw new ArgumentException("Tag name is required.", nameof(name));
        var normalized = NormalizeTag(display);

        await using var connection = _db.OpenConnection();
        await connection.OpenAsync(cancellationToken);
        var proposedId = Guid.NewGuid();

        await using (var insert = connection.CreateCommand())
        {
            insert.CommandText = """
                INSERT INTO tags(id,name,normalized_name)
                VALUES($id,$name,$normalized)
                ON CONFLICT(normalized_name) DO NOTHING
                """;
            insert.Parameters.AddWithValue("$id", proposedId.ToString("D"));
            insert.Parameters.AddWithValue("$name", display);
            insert.Parameters.AddWithValue("$normalized", normalized);
            await insert.ExecuteNonQueryAsync(cancellationToken);
        }

        await using var select = connection.CreateCommand();
        select.CommandText = "SELECT id,name,normalized_name FROM tags WHERE normalized_name=$normalized";
        select.Parameters.AddWithValue("$normalized", normalized);
        await using var reader = await select.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken)) throw new InvalidOperationException("Tag creation failed.");
        return new Tag(Guid.Parse(reader.GetString(0)), reader.GetString(1), reader.GetString(2));
    }

    public async Task RenameAsync(Guid tagId, string newName, CancellationToken cancellationToken)
    {
        var display = NormalizeDisplay(newName);
        if (display.Length == 0) throw new ArgumentException("Tag name is required.", nameof(newName));
        var normalized = NormalizeTag(display);
        await using var connection = _db.OpenConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "UPDATE tags SET name=$name,normalized_name=$normalized WHERE id=$id";
        command.Parameters.AddWithValue("$id", tagId.ToString("D"));
        command.Parameters.AddWithValue("$name", display);
        command.Parameters.AddWithValue("$normalized", normalized);
        try
        {
            if (await command.ExecuteNonQueryAsync(cancellationToken) == 0)
                throw new KeyNotFoundException($"Tag '{tagId}' was not found.");
        }
        catch (SqliteException ex) when (ex.SqliteErrorCode == 19)
        {
            throw new InvalidOperationException($"Tag '{display}' already exists.", ex);
        }
    }

    public static string NormalizeTag(string value) =>
        string.Join(' ', value.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries)).ToUpperInvariant();

    private static string NormalizeDisplay(string? value) =>
        string.Join(' ', (value ?? string.Empty).Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries));
}
