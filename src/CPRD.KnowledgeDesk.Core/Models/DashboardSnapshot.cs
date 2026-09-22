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
    string BackupStatus)
{
    public int CreatedToday { get; init; }
    public int CreatedThisWeek { get; init; }
    public int PinnedCount { get; init; }
    public int ArchivedCount { get; init; }
    public int TrashCount { get; init; }
    public int TagCount { get; init; }
    public IReadOnlyList<DashboardItem> Favorites { get; init; } = Array.Empty<DashboardItem>();
    public IReadOnlyList<DashboardItem> RecentlyModified { get; init; } = Array.Empty<DashboardItem>();
}
