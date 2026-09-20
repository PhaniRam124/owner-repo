using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CPRD.KnowledgeDesk.Core.Models;
using CPRD.KnowledgeDesk.Core.Services;

namespace CPRD.KnowledgeDesk.App.ViewModels;

public sealed class MainWindowViewModel : ObservableObject
{
    private readonly IFolderService _folders;
    private readonly INoteService _notes;
    private readonly ITagService _tags;
    private readonly ISearchService _search;
    private readonly IDashboardService _dashboard;
    private Guid? _selectedFolderId;
    private string _globalSearchText = string.Empty;
    private string _statusText = "Ready";

    public MainWindowViewModel(
        IFolderService folders,
        INoteService notes,
        ITagService tags,
        ISearchService search,
        IDashboardService dashboard)
    {
        _folders = folders;
        _notes = notes;
        _tags = tags;
        _search = search;
        _dashboard = dashboard;

        Dashboard = new DashboardViewModel(_dashboard);
        Editor = new EditorViewModel(_notes);
        SelectFolderCommand = new AsyncRelayCommand<Guid>(SelectFolderAsync);
        SelectNoteCommand = new AsyncRelayCommand<Guid>(SelectNoteAsync);
        SearchCommand = new AsyncRelayCommand(SearchAsync);
        RefreshDashboardCommand = new AsyncRelayCommand(RefreshDashboardAsync);
    }

    public ObservableCollection<Folder> Folders { get; } = new();
    public ObservableCollection<Note> Notes { get; } = new();
    public ObservableCollection<SearchHit> SearchResults { get; } = new();

    public DashboardViewModel Dashboard { get; }
    public EditorViewModel Editor { get; }

    public AsyncRelayCommand<Guid> SelectFolderCommand { get; }
    public AsyncRelayCommand<Guid> SelectNoteCommand { get; }
    public IAsyncRelayCommand SearchCommand { get; }
    public IAsyncRelayCommand RefreshDashboardCommand { get; }

    public Guid? SelectedFolderId
    {
        get => _selectedFolderId;
        private set => SetProperty(ref _selectedFolderId, value);
    }

    public string GlobalSearchText
    {
        get => _globalSearchText;
        set => SetProperty(ref _globalSearchText, value);
    }

    public string StatusText
    {
        get => _statusText;
        private set => SetProperty(ref _statusText, value);
    }

    public async Task InitializeAsync()
    {
        Folders.Clear();
        foreach (var folder in await _folders.GetTreeAsync(CancellationToken.None))
            Folders.Add(folder);

        await Dashboard.RefreshAsync();
        StatusText = "Ready";
    }

    private async Task SelectFolderAsync(Guid folderId)
    {
        SelectedFolderId = folderId;
        Notes.Clear();
        foreach (var note in await _notes.ListByFolderAsync(folderId, CancellationToken.None))
            Notes.Add(note);
        SearchResults.Clear();
        StatusText = $"{Notes.Count} note(s)";
    }

    private async Task SelectNoteAsync(Guid noteId)
    {
        var note = await _notes.GetAsync(noteId, CancellationToken.None);
        if (note is null) return;

        await _notes.MarkOpenedAsync(note.Id, DateTimeOffset.UtcNow, CancellationToken.None);
        Editor.Load(note);
        StatusText = note.Title;
    }

    private async Task SearchAsync()
    {
        SearchResults.Clear();
        var text = GlobalSearchText.Trim();
        if (text.Length == 0)
        {
            StatusText = "Search cleared";
            return;
        }

        var query = new SearchQuery(
            text,
            null,
            Array.Empty<Guid>(),
            null,
            null,
            null,
            false,
            false,
            false,
            false,
            100);

        foreach (var hit in await _search.SearchAsync(query, CancellationToken.None))
            SearchResults.Add(hit);

        StatusText = $"{SearchResults.Count} result(s)";
    }

    private async Task RefreshDashboardAsync()
    {
        await Dashboard.RefreshAsync();
        StatusText = "Dashboard refreshed";
    }
}
