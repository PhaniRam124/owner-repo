namespace CPRD.KnowledgeDesk.Core.Models;

public sealed record Note(
    Guid Id,
    string Title,
    string ContentPackage,
    string PlainText,
    Guid FolderId,
    string NoteType,
    bool IsFavorite,
    bool IsPinned,
    bool IsArchived,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset ModifiedAtUtc,
    DateTimeOffset? LastOpenedAtUtc,
    DateTimeOffset? DeletedAtUtc,
    string? SourceType,
    string? SourceReference,
    string StructuredJson);
