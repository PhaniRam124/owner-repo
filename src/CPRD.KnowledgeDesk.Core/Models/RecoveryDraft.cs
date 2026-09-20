namespace CPRD.KnowledgeDesk.Core.Models;

public sealed record RecoveryDraft(
    Guid NoteId,
    string Title,
    string ContentPackage,
    string PlainText,
    string StructuredJson,
    DateTimeOffset SavedAtUtc);
