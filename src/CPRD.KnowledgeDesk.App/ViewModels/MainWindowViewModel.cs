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
    private string _selectedNoteSort = "Recently Modified";
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
        Editor.Saved += Editor_Saved;
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
        ShowCreatedTodayCommand = new AsyncRelayCommand(ShowCreatedTodayAsync);
        ShowCreatedThisWeekCommand = new AsyncRelayCommand(ShowCreatedThisWeekAsync);
        ShowModifiedTodayCommand = new AsyncRelayCommand(ShowModifiedTodayAsync);
        ShowArchivedCommand = new AsyncRelayCommand(LoadArchivedAsync);
        CreateNewNoteCommand = new AsyncRelayCommand(CreateNewNoteAsync);
        ArchiveCurrentNoteCommand = new AsyncRelayCommand(ArchiveCurrentNoteAsync);
        MoveCurrentNoteToTrashCommand = new AsyncRelayCommand(MoveCurrentNoteToTrashAsync);
        CloseWorkspaceNoteCommand = new AsyncRelayCommand<Guid>(CloseWorkspaceNoteAsync);
        CloseActiveWorkspaceCommand = new AsyncRelayCommand(CloseActiveWorkspaceAsync, () => Editor.CurrentNote is not null);
    }

    public ObservableCollection<Folder> Folders { get; } = new();
    public ObservableCollection<FolderTreeNode> FolderRoots { get; } = new();
    public ObservableCollection<Note> Notes { get; } = new();
    public ObservableCollection<SearchHit> SearchResults { get; } = new();
    public ObservableCollection<Note> WorkspaceNotes { get; } = new();
    public ObservableCollection<string> RecentSearches { get; } = new();

    public IReadOnlyList<string> NoteSortOptions { get; } = new[]
    {
        "Recently Modified",
        "Newest",
        "Oldest",
        "Title A-Z",
        "Title Z-A",
        "Folder"
    };

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
    public IAsyncRelayCommand ShowCreatedTodayCommand { get; }
    public IAsyncRelayCommand ShowCreatedThisWeekCommand { get; }
    public IAsyncRelayCommand ShowModifiedTodayCommand { get; }
    public IAsyncRelayCommand ShowArchivedCommand { get; }
    public IAsyncRelayCommand CreateNewNoteCommand { get; }
    public IAsyncRelayCommand ArchiveCurrentNoteCommand { get; }
    public IAsyncRelayCommand MoveCurrentNoteToTrashCommand { get; }
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

    public string SelectedNoteSort
    {
        get => _selectedNoteSort;
        set
        {
            if (SetProperty(ref _selectedNoteSort, value))
                ApplyNoteSort();
        }
    }

    public async Task InitializeAsync()
    {
        await RefreshFoldersAsync();

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

    public async Task<Folder> CreateFolderAsync(Guid? parentId, string name)
    {
        var folder = await _folders.CreateAsync(parentId, name, CancellationToken.None);
        await RefreshFoldersAsync();
        await Dashboard.RefreshAsync();
        StatusText = $"Folder created: {folder.Name}";
        return folder;
    }

    public async Task RenameFolderAsync(Guid folderId, string newName)
    {
        await _folders.RenameAsync(folderId, newName, CancellationToken.None);
        await RefreshFoldersAsync();
        await Dashboard.RefreshAsync();
        StatusText = $"Folder renamed: {newName.Trim()}";
    }

    public async Task ArchiveFolderAsync(Guid folderId)
    {
        await _folders.ArchiveAsync(folderId, CancellationToken.None);
        if (SelectedFolderId == folderId)
            SelectedFolderId = null;

        await RefreshFoldersAsync();
        await Dashboard.RefreshAsync();
        StatusText = "Folder archived.";
    }

    public async Task DeleteFolderAsync(Guid folderId, Guid destinationFolderId)
    {
        await Editor.FlushAsync(true, CancellationToken.None);
        await _folders.DeleteAsync(folderId, destinationFolderId, CancellationToken.None);

        if (SelectedFolderId == folderId)
            SelectedFolderId = destinationFolderId;

        await RefreshFoldersAsync();
        await Dashboard.RefreshAsync();

        if (SelectedFolderId is Guid selected)
            await SelectFolderAsync(selected);

        StatusText = "Folder deleted safely; its notes were moved.";
    }

    private async Task RefreshFoldersAsync()
    {
        Folders.Clear();
        FolderRoots.Clear();

        var activeFolders = (await _folders.GetTreeAsync(CancellationToken.None))
            .Where(folder => !folder.IsArchived)
            .OrderBy(folder => folder.SortOrder)
            .ThenBy(folder => folder.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        foreach (var folder in activeFolders)
            Folders.Add(folder);

        var nodes = activeFolders.ToDictionary(
            folder => folder.Id,
            folder => new FolderTreeNode(folder));

        foreach (var folder in activeFolders)
        {
            var node = nodes[folder.Id];
            if (folder.ParentId is Guid parentId && nodes.TryGetValue(parentId, out var parent))
                parent.Children.Add(node);
            else
                FolderRoots.Add(node);
        }
    }

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
            ApplyNoteSort();

            _workspace.Open(note.Id);
            var openExisting = WorkspaceNotes.FirstOrDefault(item => item.Id == note.Id);
            if (openExisting is not null)
                WorkspaceNotes.Remove(openExisting);
            WorkspaceNotes.Add(note);

            while (WorkspaceNotes.Count > WorkspaceService.MaximumOpenNotes)
                WorkspaceNotes.RemoveAt(0);

            await _notes.MarkOpenedAsync(note.Id, DateTimeOffset.UtcNow, CancellationToken.None);
            Editor.Load(note);
            Editor.LoadTags(Array.Empty<string>());
            await Dashboard.RefreshAsync();
            CloseActiveWorkspaceCommand.NotifyCanExecuteChanged();
            StatusText = "New note created — start typing.";
        }
        catch (Exception ex)
        {
            StatusText = $"Unable to create note: {ex.Message}";
        }
    }

    private async Task ArchiveCurrentNoteAsync()
    {
        var note = Editor.CurrentNote;
        if (note is null)
        {
            StatusText = "Select a note to archive.";
            return;
        }

        await Editor.FlushAsync(true, CancellationToken.None);
        await _notes.ArchiveAsync(note.Id, CancellationToken.None);

        RemoveNoteFromVisibleCollections(note.Id);
        await CloseWorkspaceNoteAsync(note.Id);
        await Dashboard.RefreshAsync();

        StatusText = "Note archived.";
    }

    private async Task MoveCurrentNoteToTrashAsync()
    {
        var note = Editor.CurrentNote;
        if (note is null)
        {
            StatusText = "Select a note to move to Trash.";
            return;
        }

        await Editor.FlushAsync(true, CancellationToken.None);
        await _notes.MoveToTrashAsync(note.Id, CancellationToken.None);

        RemoveNoteFromVisibleCollections(note.Id);
        await CloseWorkspaceNoteAsync(note.Id);
        await Trash.RefreshAsync();
        await Dashboard.RefreshAsync();

        StatusText = "Note moved to Trash.";
    }

    private void RemoveNoteFromVisibleCollections(Guid noteId)
    {
        var note = Notes.FirstOrDefault(item => item.Id == noteId);
        if (note is not null)
            Notes.Remove(note);

        var hit = SearchResults.FirstOrDefault(item => item.NoteId == noteId);
        if (hit is not null)
            SearchResults.Remove(hit);
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
        ApplyNoteSort();
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
        Editor.LoadTags(await _notes.GetTagsAsync(note.Id, CancellationToken.None));
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
        ApplyNoteSort();
        StatusText = $"{label}: {SearchResults.Count} note(s)";
    }

    private Task ShowCreatedTodayAsync()
    {
        var (startUtc, endUtc) = LocalDayBoundsUtc(DateTimeOffset.Now);
        return LoadDateFilteredNavigationAsync(
            "Created Today",
            startUtc,
            endUtc,
            null,
            null);
    }

    private Task ShowCreatedThisWeekAsync()
    {
        var now = DateTimeOffset.Now;
        var dayStart = new DateTimeOffset(
            now.Year,
            now.Month,
            now.Day,
            0,
            0,
            0,
            now.Offset);
        var daysSinceMonday = ((int)dayStart.DayOfWeek + 6) % 7;
        var weekStartUtc = dayStart.AddDays(-daysSinceMonday).ToUniversalTime();
        var nextWeekUtc = dayStart.AddDays(7 - daysSinceMonday).ToUniversalTime();

        return LoadDateFilteredNavigationAsync(
            "Created This Week",
            weekStartUtc,
            nextWeekUtc,
            null,
            null);
    }

    private Task ShowModifiedTodayAsync()
    {
        var (startUtc, endUtc) = LocalDayBoundsUtc(DateTimeOffset.Now);
        return LoadDateFilteredNavigationAsync(
            "Modified Today",
            null,
            null,
            startUtc,
            endUtc);
    }

    private async Task LoadDateFilteredNavigationAsync(
        string label,
        DateTimeOffset? createdFromUtc,
        DateTimeOffset? createdToUtc,
        DateTimeOffset? modifiedFromUtc,
        DateTimeOffset? modifiedToUtc)
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
            modifiedFromUtc,
            modifiedToUtc,
            null,
            false,
            false,
            false,
            false,
            200)
        {
            CreatedFromUtc = createdFromUtc,
            CreatedToUtc = createdToUtc
        };

        foreach (var hit in await _search.SearchAsync(query, CancellationToken.None))
            SearchResults.Add(hit);

        EmptyStateText = SearchResults.Count == 0
            ? $"No notes found for {label}."
            : string.Empty;
        ApplyNoteSort();
        StatusText = $"{label}: {SearchResults.Count} note(s)";
    }

    private static (DateTimeOffset StartUtc, DateTimeOffset EndUtc) LocalDayBoundsUtc(
        DateTimeOffset localNow)
    {
        var startLocal = new DateTimeOffset(
            localNow.Year,
            localNow.Month,
            localNow.Day,
            0,
            0,
            0,
            localNow.Offset);

        return (
            startLocal.ToUniversalTime(),
            startLocal.AddDays(1).ToUniversalTime());
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
        ApplyNoteSort();
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
        ApplyNoteSort();
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

    private async void Editor_Saved(object? sender, EventArgs e)
    {
        try
        {
            await Dashboard.RefreshAsync();
        }
        catch
        {
            // Save status remains authoritative; dashboard can be refreshed manually if needed.
        }
    }

    private void ApplyNoteSort()
    {
        if (Notes.Count > 1)
        {
            IEnumerable<Note> orderedNotes = SelectedNoteSort switch
            {
                "Newest" => Notes.OrderByDescending(note => note.CreatedAtUtc),
                "Oldest" => Notes.OrderBy(note => note.CreatedAtUtc),
                "Title A-Z" => Notes.OrderBy(note => note.Title, StringComparer.OrdinalIgnoreCase),
                "Title Z-A" => Notes.OrderByDescending(note => note.Title, StringComparer.OrdinalIgnoreCase),
                _ => Notes.OrderByDescending(note => note.ModifiedAtUtc)
            };

            ReplaceCollection(Notes, orderedNotes.ToArray());
        }

        if (SearchResults.Count > 1)
        {
            IEnumerable<SearchHit> orderedHits = SelectedNoteSort switch
            {
                "Oldest" => SearchResults.OrderBy(hit => hit.ModifiedAtUtc),
                "Title A-Z" => SearchResults.OrderBy(hit => hit.Title, StringComparer.OrdinalIgnoreCase),
                "Title Z-A" => SearchResults.OrderByDescending(hit => hit.Title, StringComparer.OrdinalIgnoreCase),
                "Folder" => SearchResults
                    .OrderBy(hit => hit.FolderPath, StringComparer.OrdinalIgnoreCase)
                    .ThenBy(hit => hit.Title, StringComparer.OrdinalIgnoreCase),
                _ => SearchResults.OrderByDescending(hit => hit.ModifiedAtUtc)
            };

            ReplaceCollection(SearchResults, orderedHits.ToArray());
        }
    }

    private static void ReplaceCollection<T>(
        ObservableCollection<T> collection,
        IReadOnlyList<T> items)
    {
        collection.Clear();
        foreach (var item in items)
            collection.Add(item);
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
