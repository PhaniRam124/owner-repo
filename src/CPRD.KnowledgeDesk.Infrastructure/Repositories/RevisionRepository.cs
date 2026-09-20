using CPRD.KnowledgeDesk.Core.Models;
using CPRD.KnowledgeDesk.Infrastructure.Data;
using Microsoft.Data.Sqlite;

namespace CPRD.KnowledgeDesk.Infrastructure.Repositories;

public sealed class RevisionRepository
{
    private readonly KnowledgeDb _db;

    public RevisionRepository(KnowledgeDb db) => _db = db;

    public async Task<NoteRevision> CreateAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        Note note,
        CancellationToken cancellationToken)
    {
        var nextNumber = await GetNextNumberAsync(connection, transaction, note.Id, cancellationToken);
        var createdAt = DateTimeOffset.UtcNow;

        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            INSERT INTO note_revisions(
                note_id,revision_number,title_snapshot,content_package_snapshot,
                plain_text_snapshot,structured_json_snapshot,created_at_utc)
            VALUES($noteId,$number,$title,$content,$plain,$structured,$created);
            SELECT last_insert_rowid();
            """;
        command.Parameters.AddWithValue("$noteId", note.Id.ToString("D"));
        command.Parameters.AddWithValue("$number", nextNumber);
        command.Parameters.AddWithValue("$title", note.Title);
        command.Parameters.AddWithValue("$content", note.ContentPackage);
        command.Parameters.AddWithValue("$plain", note.PlainText);
        command.Parameters.AddWithValue("$structured", note.StructuredJson);
        command.Parameters.AddWithValue("$created", createdAt.ToString("O"));
        var id = Convert.ToInt64(await command.ExecuteScalarAsync(cancellationToken));

        return new NoteRevision(
            id,
            note.Id,
            nextNumber,
            note.Title,
            note.ContentPackage,
            note.PlainText,
            note.StructuredJson,
            createdAt);
    }

    public async Task<IReadOnlyList<NoteRevision>> ListAsync(Guid noteId, CancellationToken cancellationToken)
    {
        var result = new List<NoteRevision>();
        await using var connection = _db.OpenConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT id,note_id,revision_number,title_snapshot,content_package_snapshot,
                   plain_text_snapshot,structured_json_snapshot,created_at_utc
              FROM note_revisions
             WHERE note_id=$noteId
             ORDER BY revision_number DESC
            """;
        command.Parameters.AddWithValue("$noteId", noteId.ToString("D"));
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
            result.Add(Read(reader));
        return result;
    }

    public async Task<NoteRevision?> GetAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        Guid noteId,
        long revisionId,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            SELECT id,note_id,revision_number,title_snapshot,content_package_snapshot,
                   plain_text_snapshot,structured_json_snapshot,created_at_utc
              FROM note_revisions
             WHERE id=$id AND note_id=$noteId
            """;
        command.Parameters.AddWithValue("$id", revisionId);
        command.Parameters.AddWithValue("$noteId", noteId.ToString("D"));
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? Read(reader) : null;
    }

    private static async Task<int> GetNextNumberAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        Guid noteId,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "SELECT COALESCE(MAX(revision_number),0)+1 FROM note_revisions WHERE note_id=$noteId";
        command.Parameters.AddWithValue("$noteId", noteId.ToString("D"));
        return Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken));
    }

    private static NoteRevision Read(SqliteDataReader reader) => new(
        reader.GetInt64(0),
        Guid.Parse(reader.GetString(1)),
        reader.GetInt32(2),
        reader.GetString(3),
        reader.GetString(4),
        reader.GetString(5),
        reader.GetString(6),
        DateTimeOffset.Parse(reader.GetString(7)));
}
