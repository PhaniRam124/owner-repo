namespace CPRD.KnowledgeDesk.Core.Models;

public sealed record Attachment(
    Guid Id,
    Guid NoteId,
    string Filename,
    string StoredPath,
    string MimeType,
    long SizeBytes,
    string HashSha256,
    DateTimeOffset CreatedAtUtc);
