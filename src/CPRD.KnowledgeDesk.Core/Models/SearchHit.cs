namespace CPRD.KnowledgeDesk.Core.Models;

public sealed record SearchQuery(
    string Text,
    Guid? FolderId,
    IReadOnlyList<Guid> TagIds,
    DateTimeOffset? ModifiedFromUtc,
    DateTimeOffset? ModifiedToUtc,
    string? NoteType,
    bool IncludeArchived,
    bool FavoritesOnly,
    bool PinnedOnly,
    bool HasAttachments,
    int Limit);

public sealed record SearchHit(
    Guid NoteId,
    string Title,
    string Snippet,
    string FolderPath,
    string NoteType,
    bool IsFavorite,
    bool IsPinned,
    DateTimeOffset ModifiedAtUtc,
    double Rank,
    bool IsArchived = false);
