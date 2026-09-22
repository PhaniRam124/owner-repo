namespace CPRD.KnowledgeDesk.Core.Models;

public sealed record NoteRevision(
    long Id,
    Guid NoteId,
    int RevisionNumber,
    string TitleSnapshot,
    string ContentPackageSnapshot,
    string PlainTextSnapshot,
    string StructuredJsonSnapshot,
    DateTimeOffset CreatedAtUtc);
