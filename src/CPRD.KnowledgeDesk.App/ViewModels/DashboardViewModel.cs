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
    private string _backupStatus = "Not configured";

    public DashboardViewModel(IDashboardService dashboard) => _dashboard = dashboard;

    public ObservableCollection<DashboardItem> Recent { get; } = new();
    public ObservableCollection<DashboardItem> Pinned { get; } = new();
    public ObservableCollection<DashboardItem> RecentlyImported { get; } = new();

    public int TotalNotes { get => _totalNotes; private set => SetProperty(ref _totalNotes, value); }
    public int FolderCount { get => _folderCount; private set => SetProperty(ref _folderCount, value); }
    public int FavoriteCount { get => _favoriteCount; private set => SetProperty(ref _favoriteCount, value); }
    public int UpdatedToday { get => _updatedToday; private set => SetProperty(ref _updatedToday, value); }
    public string BackupStatus { get => _backupStatus; private set => SetProperty(ref _backupStatus, value); }

    public async Task RefreshAsync()
    {
        var snapshot = await _dashboard.GetSnapshotAsync(CancellationToken.None);
        TotalNotes = snapshot.TotalNotes;
        FolderCount = snapshot.FolderCount;
        FavoriteCount = snapshot.FavoriteCount;
        UpdatedToday = snapshot.UpdatedToday;
        BackupStatus = snapshot.BackupStatus;
        Replace(Recent, snapshot.Recent);
        Replace(Pinned, snapshot.Pinned);
        Replace(RecentlyImported, snapshot.RecentlyImported);
    }

    private static void Replace<T>(ObservableCollection<T> target, IEnumerable<T> values)
    {
        target.Clear();
        foreach (var value in values) target.Add(value);
    }
}
