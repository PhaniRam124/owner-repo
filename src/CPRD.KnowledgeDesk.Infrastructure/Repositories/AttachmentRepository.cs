using CPRD.KnowledgeDesk.Core.Models;
using CPRD.KnowledgeDesk.Infrastructure.Data;
using Microsoft.Data.Sqlite;

namespace CPRD.KnowledgeDesk.Infrastructure.Repositories;

public sealed class AttachmentRepository
{
    private readonly KnowledgeDb _db;

    public AttachmentRepository(KnowledgeDb db) => _db = db;

    public async Task InsertAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        Attachment attachment,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            INSERT INTO attachments(id,note_id,filename,stored_path,mime_type,size_bytes,hash_sha256,created_at_utc)
            VALUES($id,$noteId,$filename,$storedPath,$mimeType,$sizeBytes,$hash,$createdAt)
            """;
        command.Parameters.AddWithValue("$id", attachment.Id.ToString("D"));
        command.Parameters.AddWithValue("$noteId", attachment.NoteId.ToString("D"));
        command.Parameters.AddWithValue("$filename", attachment.Filename);
        command.Parameters.AddWithValue("$storedPath", attachment.StoredPath);
        command.Parameters.AddWithValue("$mimeType", attachment.MimeType);
        command.Parameters.AddWithValue("$sizeBytes", attachment.SizeBytes);
        command.Parameters.AddWithValue("$hash", attachment.HashSha256);
        command.Parameters.AddWithValue("$createdAt", attachment.CreatedAtUtc.ToString("O"));
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<Attachment?> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        await using var connection = _db.OpenConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT * FROM attachments WHERE id=$id";
        command.Parameters.AddWithValue("$id", id.ToString("D"));
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? Read(reader) : null;
    }

    public async Task<IReadOnlyList<Attachment>> ListAsync(Guid noteId, CancellationToken cancellationToken)
    {
        var result = new List<Attachment>();
        await using var connection = _db.OpenConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT * FROM attachments WHERE note_id=$noteId ORDER BY created_at_utc, filename COLLATE NOCASE";
        command.Parameters.AddWithValue("$noteId", noteId.ToString("D"));
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken)) result.Add(Read(reader));
        return result;
    }

    public async Task DeleteAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        Guid id,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "DELETE FROM attachments WHERE id=$id";
        command.Parameters.AddWithValue("$id", id.ToString("D"));
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static Attachment Read(SqliteDataReader reader) => new(
        Guid.Parse(reader.GetString(reader.GetOrdinal("id"))),
        Guid.Parse(reader.GetString(reader.GetOrdinal("note_id"))),
        reader.GetString(reader.GetOrdinal("filename")),
        reader.GetString(reader.GetOrdinal("stored_path")),
        reader.GetString(reader.GetOrdinal("mime_type")),
        reader.GetInt64(reader.GetOrdinal("size_bytes")),
        reader.GetString(reader.GetOrdinal("hash_sha256")),
        DateTimeOffset.Parse(reader.GetString(reader.GetOrdinal("created_at_utc"))));
}
