using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CPRD.KnowledgeDesk.App.Services;
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
    private readonly WorkspaceService _workspace;
    private Guid? _selectedFolderId;
    private string _globalSearchText = string.Empty;
    private string _statusText = "Ready";
    private string _emptyStateText = string.Empty;
    private int _selectedWorkspaceIndex;

    public MainWindowViewModel(
        IFolderService folders,
        INoteService notes,
        ITagService tags,
        ISearchService search,
        IDashboardService dashboard)
        : this(folders, notes, tags, search, dashboard, new WorkspaceService(), new EditorViewModel(notes))
    {
    }

    public MainWindowViewModel(
        IFolderService folders,
        INoteService notes,
        ITagService tags,
        ISearchService search,
        IDashboardService dashboard,
        WorkspaceService workspace)
        : this(folders, notes, tags, search, dashboard, workspace, new EditorViewModel(notes))
    {
    }

    public MainWindowViewModel(
        IFolderService folders,
        INoteService notes,
        ITagService tags,
        ISearchService search,
        IDashboardService dashboard,
        WorkspaceService workspace,
        EditorViewModel editor)
    {
        _folders = folders;
        _notes = notes;
        _tags = tags;
        _search = search;
        _dashboard = dashboard;
        _workspace = workspace;

        Dashboard = new DashboardViewModel(_dashboard);
        Editor = editor;
        Trash = new TrashViewModel(_notes);
        SelectFolderCommand = new AsyncRelayCommand<Guid>(SelectFolderAsync);
        SelectNoteCommand = new AsyncRelayCommand<Guid>(SelectNoteAsync);
        SearchCommand = new AsyncRelayCommand(SearchAsync);
        ClearSearchCommand = new AsyncRelayCommand(ClearSearchAsync);
        RefreshDashboardCommand = new AsyncRelayCommand(RefreshDashboardAsync);
        ShowDashboardCommand = new AsyncRelayCommand(ShowDashboardAsync);
        ShowTrashCommand = new AsyncRelayCommand(ShowTrashAsync);
        ShowAllNotesCommand = new AsyncRelayCommand(() => LoadNavigationAsync("All Notes", false, false));
        ShowFavoritesCommand = new AsyncRelayCommand(() => LoadNavigationAsync("Favorites", true, false));
        ShowPinnedCommand = new AsyncRelayCommand(() => LoadNavigationAsync("Pinned", false, true));
        ShowRecentCommand = new AsyncRelayCommand(() => LoadNavigationAsync("Recent", false, false));
        ShowArchivedCommand = new AsyncRelayCommand(LoadArchivedAsync);
        CreateNewNoteCommand = new AsyncRelayCommand(CreateNewNoteAsync);
        CloseWorkspaceNoteCommand = new AsyncRelayCommand<Guid>(CloseWorkspaceNoteAsync);
        CloseActiveWorkspaceCommand = new AsyncRelayCommand(CloseActiveWorkspaceAsync, () => Editor.CurrentNote is not null);
    }

    public ObservableCollection<Folder> Folders { get; } = new();
    public ObservableCollection<Note> Notes { get; } = new();
    public ObservableCollection<SearchHit> SearchResults { get; } = new();
    public ObservableCollection<Note> WorkspaceNotes { get; } = new();
    public ObservableCollection<string> RecentSearches { get; } = new();

    public DashboardViewModel Dashboard { get; }
    public EditorViewModel Editor { get; }
    public TrashViewModel Trash { get; }

    public AsyncRelayCommand<Guid> SelectFolderCommand { get; }
    public AsyncRelayCommand<Guid> SelectNoteCommand { get; }
    public IAsyncRelayCommand SearchCommand { get; }
    public IAsyncRelayCommand ClearSearchCommand { get; }
    public IAsyncRelayCommand RefreshDashboardCommand { get; }
    public IAsyncRelayCommand ShowDashboardCommand { get; }
    public IAsyncRelayCommand ShowTrashCommand { get; }
    public IAsyncRelayCommand ShowAllNotesCommand { get; }
    public IAsyncRelayCommand ShowFavoritesCommand { get; }
    public IAsyncRelayCommand ShowPinnedCommand { get; }
    public IAsyncRelayCommand ShowRecentCommand { get; }
    public IAsyncRelayCommand ShowArchivedCommand { get; }
    public IAsyncRelayCommand CreateNewNoteCommand { get; }
    public AsyncRelayCommand<Guid> CloseWorkspaceNoteCommand { get; }
    public IAsyncRelayCommand CloseActiveWorkspaceCommand { get; }

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

    public string EmptyStateText
    {
        get => _emptyStateText;
        private set => SetProperty(ref _emptyStateText, value);
    }

    public int SelectedWorkspaceIndex
    {
        get => _selectedWorkspaceIndex;
        set => SetProperty(ref _selectedWorkspaceIndex, Math.Clamp(value, 0, 2));
    }

    public async Task InitializeAsync()
    {
        Folders.Clear();
        foreach (var folder in await _folders.GetTreeAsync(CancellationToken.None))
            Folders.Add(folder);

        await Dashboard.RefreshAsync();
        await Trash.RefreshAsync();

        var defaultFolder = Folders.FirstOrDefault(folder =>
                !folder.IsArchived && folder.Name.Equals("General", StringComparison.OrdinalIgnoreCase))
            ?? Folders.FirstOrDefault(folder => !folder.IsArchived);

        if (defaultFolder is not null)
            await SelectFolderAsync(defaultFolder.Id);
        else
            StatusText = "Create a folder to start adding notes.";
    }

    public Task FlushEditorAsync(CancellationToken cancellationToken = default) =>
        Editor.FlushAsync(true, cancellationToken);

    private async Task CreateNewNoteAsync()
    {
        try
        {
            await Editor.FlushAsync(true, CancellationToken.None);

            var folderId = SelectedFolderId
                ?? Folders.FirstOrDefault(folder =>
                        !folder.IsArchived && folder.Name.Equals("General", StringComparison.OrdinalIgnoreCase))?.Id
                ?? Folders.FirstOrDefault(folder => !folder.IsArchived)?.Id;

            if (folderId is null)
            {
                StatusText = "Create a folder before adding a note.";
                return;
            }

            var untitledNumber = Notes.Count(note =>
                note.Title.StartsWith("Untitled Note", StringComparison.OrdinalIgnoreCase)) + 1;
            var title = untitledNumber == 1 ? "Untitled Note" : $"Untitled Note {untitledNumber}";

            var note = await _notes.CreateAsync(
                new NewNoteRequest(
                    title,
                    string.Empty,
                    string.Empty,
                    folderId.Value,
                    "Standard Note",
                    "{}",
                    Array.Empty<string>()),
                CancellationToken.None);

            SelectedWorkspaceIndex = 0;
            SelectedFolderId = folderId.Value;
            SearchResults.Clear();

            var existing = Notes.FirstOrDefault(item => item.Id == note.Id);
            if (existing is not null)
                Notes.Remove(existing);
            Notes.Insert(0, note);

            _workspace.Open(note.Id);
            var openExisting = WorkspaceNotes.FirstOrDefault(item => item.Id == note.Id);
            if (openExisting is not null)
                WorkspaceNotes.Remove(openExisting);
            WorkspaceNotes.Add(note);

            while (WorkspaceNotes.Count > WorkspaceService.MaximumOpenNotes)
                WorkspaceNotes.RemoveAt(0);

            await _notes.MarkOpenedAsync(note.Id, DateTimeOffset.UtcNow, CancellationToken.None);
            Editor.Load(note);
            CloseActiveWorkspaceCommand.NotifyCanExecuteChanged();
            StatusText = "New note created — start typing.";
        }
        catch (Exception ex)
        {
            StatusText = $"Unable to create note: {ex.Message}";
        }
    }

    private async Task SelectFolderAsync(Guid folderId)
    {
        await Editor.FlushAsync(true, CancellationToken.None);

        SelectedWorkspaceIndex = 0;
        SelectedFolderId = folderId;
        Notes.Clear();
        foreach (var note in await _notes.ListByFolderAsync(folderId, CancellationToken.None))
            Notes.Add(note);
        SearchResults.Clear();
        EmptyStateText = string.Empty;
        StatusText = $"{Notes.Count} note(s)";
    }

    private async Task SelectNoteAsync(Guid noteId)
    {
        if (Editor.CurrentNote?.Id != noteId)
            await Editor.FlushAsync(true, CancellationToken.None);

        SelectedWorkspaceIndex = 0;
        var note = await _notes.GetAsync(noteId, CancellationToken.None);
        if (note is null) return;

        await _notes.MarkOpenedAsync(note.Id, DateTimeOffset.UtcNow, CancellationToken.None);
        _workspace.Open(note.Id);

        var existing = WorkspaceNotes.FirstOrDefault(item => item.Id == note.Id);
        if (existing is not null)
            WorkspaceNotes.Remove(existing);
        WorkspaceNotes.Add(note);

        while (WorkspaceNotes.Count > WorkspaceService.MaximumOpenNotes)
            WorkspaceNotes.RemoveAt(0);

        Editor.Load(note);
        CloseActiveWorkspaceCommand.NotifyCanExecuteChanged();
        StatusText = note.Title;
    }

    private async Task CloseWorkspaceNoteAsync(Guid noteId)
    {
        if (Editor.CurrentNote?.Id == noteId)
            await Editor.FlushAsync(true, CancellationToken.None);

        _workspace.Close(noteId);
        var item = WorkspaceNotes.FirstOrDefault(note => note.Id == noteId);
        if (item is not null)
            WorkspaceNotes.Remove(item);

        if (Editor.CurrentNote?.Id != noteId)
            return;

        if (WorkspaceNotes.Count == 0)
        {
            Editor.Clear();
            CloseActiveWorkspaceCommand.NotifyCanExecuteChanged();
            StatusText = "Workspace empty";
            return;
        }

        await SelectNoteAsync(WorkspaceNotes[^1].Id);
    }

    private Task CloseActiveWorkspaceAsync()
    {
        var noteId = Editor.CurrentNote?.Id;
        return noteId is null ? Task.CompletedTask : CloseWorkspaceNoteAsync(noteId.Value);
    }

    private async Task LoadNavigationAsync(string label, bool favoritesOnly, bool pinnedOnly)
    {
        await Editor.FlushAsync(true, CancellationToken.None);

        SelectedWorkspaceIndex = 0;
        SelectedFolderId = null;
        Notes.Clear();
        SearchResults.Clear();

        var query = new SearchQuery(
            string.Empty,
            null,
            Array.Empty<Guid>(),
            null,
            null,
            null,
            false,
            favoritesOnly,
            pinnedOnly,
            false,
            200);

        foreach (var hit in await _search.SearchAsync(query, CancellationToken.None))
            SearchResults.Add(hit);

        EmptyStateText = SearchResults.Count == 0 ? $"No notes found in {label}." : string.Empty;
        StatusText = $"{label}: {SearchResults.Count} note(s)";
    }

    private async Task LoadArchivedAsync()
    {
        await Editor.FlushAsync(true, CancellationToken.None);

        SelectedWorkspaceIndex = 0;
        SelectedFolderId = null;
        Notes.Clear();
        SearchResults.Clear();

        var query = new SearchQuery(
            string.Empty,
            null,
            Array.Empty<Guid>(),
            null,
            null,
            null,
            true,
            false,
            false,
            false,
            200);

        foreach (var hit in await _search.SearchAsync(query, CancellationToken.None))
        {
            if (hit.IsArchived)
                SearchResults.Add(hit);
        }

        EmptyStateText = SearchResults.Count == 0 ? "Archive is empty." : string.Empty;
        StatusText = $"Archive: {SearchResults.Count} note(s)";
    }

    private async Task SearchAsync()
    {
        Notes.Clear();
        SearchResults.Clear();
        var text = GlobalSearchText.Trim();
        if (text.Length == 0)
        {
            EmptyStateText = string.Empty;
            StatusText = "Search cleared";
            return;
        }

        var previous = RecentSearches.FirstOrDefault(item =>
            item.Equals(text, StringComparison.OrdinalIgnoreCase));
        if (previous is not null)
            RecentSearches.Remove(previous);
        RecentSearches.Insert(0, text);
        while (RecentSearches.Count > 8)
            RecentSearches.RemoveAt(RecentSearches.Count - 1);

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

        EmptyStateText = SearchResults.Count == 0
            ? "No notes found matching your search."
            : string.Empty;
        StatusText = $"{SearchResults.Count} result(s)";
    }

    private async Task ClearSearchAsync()
    {
        GlobalSearchText = string.Empty;
        SearchResults.Clear();
        EmptyStateText = string.Empty;

        if (SelectedFolderId is Guid folderId)
            await SelectFolderAsync(folderId);
        else
            StatusText = "Search cleared";
    }

    private async Task RefreshDashboardAsync()
    {
        await Dashboard.RefreshAsync();
        StatusText = "Dashboard refreshed";
    }

    private async Task ShowDashboardAsync()
    {
        SelectedWorkspaceIndex = 1;
        await Dashboard.RefreshAsync();
        StatusText = "Dashboard refreshed";
    }

    private async Task ShowTrashAsync()
    {
        SelectedWorkspaceIndex = 2;
        await Trash.RefreshAsync();
        StatusText = Trash.Status;
    }
}
