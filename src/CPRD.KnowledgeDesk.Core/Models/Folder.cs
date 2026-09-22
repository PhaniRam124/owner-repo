namespace CPRD.KnowledgeDesk.Core.Models;

public sealed record Folder(
    Guid Id,
    Guid? ParentId,
    string Name,
    int SortOrder,
    bool IsArchived,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset ModifiedAtUtc);
