namespace CPRD.KnowledgeDesk.Core.Models;

public sealed record DashboardItem(
    Guid NoteId,
    string Title,
    string FolderPath,
    DateTimeOffset ModifiedAtUtc);

public sealed record DashboardSnapshot(
    int TotalNotes,
    int FolderCount,
    int FavoriteCount,
    int UpdatedToday,
    IReadOnlyList<DashboardItem> Recent,
    IReadOnlyList<DashboardItem> Pinned,
    IReadOnlyList<DashboardItem> RecentlyImported,
    string BackupStatus);
