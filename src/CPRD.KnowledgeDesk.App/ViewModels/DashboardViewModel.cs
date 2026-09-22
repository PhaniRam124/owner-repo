using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CPRD.KnowledgeDesk.Core.Models;
using CPRD.KnowledgeDesk.Core.Services;

namespace CPRD.KnowledgeDesk.App.ViewModels;

public sealed class DashboardViewModel : ObservableObject
{
    private readonly IDashboardService _dashboard;
    private int _totalNotes;
    private int _folderCount;
    private int _favoriteCount;
    private int _updatedToday;
    private int _createdToday;
    private int _createdThisWeek;
    private int _pinnedCount;
    private int _archivedCount;
    private int _trashCount;
    private int _tagCount;
    private string _backupStatus = "Not configured";

    public DashboardViewModel(IDashboardService dashboard) => _dashboard = dashboard;

    public ObservableCollection<DashboardItem> Recent { get; } = new();
    public ObservableCollection<DashboardItem> Pinned { get; } = new();
    public ObservableCollection<DashboardItem> RecentlyImported { get; } = new();
    public ObservableCollection<DashboardItem> Favorites { get; } = new();
    public ObservableCollection<DashboardItem> RecentlyModified { get; } = new();
    public ObservableCollection<DashboardFolderSummary> FolderSummary { get; } = new();
    public ObservableCollection<DashboardTrendPoint> CreatedOverTime { get; } = new();

    public int TotalNotes { get => _totalNotes; private set => SetProperty(ref _totalNotes, value); }
    public int FolderCount { get => _folderCount; private set => SetProperty(ref _folderCount, value); }
    public int FavoriteCount { get => _favoriteCount; private set => SetProperty(ref _favoriteCount, value); }
    public int UpdatedToday { get => _updatedToday; private set => SetProperty(ref _updatedToday, value); }
    public int CreatedToday { get => _createdToday; private set => SetProperty(ref _createdToday, value); }
    public int CreatedThisWeek { get => _createdThisWeek; private set => SetProperty(ref _createdThisWeek, value); }
    public int PinnedCount { get => _pinnedCount; private set => SetProperty(ref _pinnedCount, value); }
    public int ArchivedCount { get => _archivedCount; private set => SetProperty(ref _archivedCount, value); }
    public int TrashCount { get => _trashCount; private set => SetProperty(ref _trashCount, value); }
    public int TagCount { get => _tagCount; private set => SetProperty(ref _tagCount, value); }
    public string BackupStatus { get => _backupStatus; private set => SetProperty(ref _backupStatus, value); }

    public async Task RefreshAsync()
    {
        var snapshot = await _dashboard.GetSnapshotAsync(CancellationToken.None);
        TotalNotes = snapshot.TotalNotes;
        FolderCount = snapshot.FolderCount;
        FavoriteCount = snapshot.FavoriteCount;
        UpdatedToday = snapshot.UpdatedToday;
        CreatedToday = snapshot.CreatedToday;
        CreatedThisWeek = snapshot.CreatedThisWeek;
        PinnedCount = snapshot.PinnedCount;
        ArchivedCount = snapshot.ArchivedCount;
        TrashCount = snapshot.TrashCount;
        TagCount = snapshot.TagCount;
        BackupStatus = snapshot.BackupStatus;
        Replace(Recent, snapshot.Recent);
        Replace(Pinned, snapshot.Pinned);
        Replace(RecentlyImported, snapshot.RecentlyImported);
        Replace(Favorites, snapshot.Favorites);
        Replace(RecentlyModified, snapshot.RecentlyModified);
        Replace(FolderSummary, snapshot.FolderSummary);
        Replace(CreatedOverTime, snapshot.CreatedOverTime);
    }

    private static void Replace<T>(ObservableCollection<T> target, IEnumerable<T> values)
    {
        target.Clear();
        foreach (var value in values) target.Add(value);
    }
}
