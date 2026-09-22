using CPRD.KnowledgeDesk.App.ViewModels;
using CPRD.KnowledgeDesk.Core.Models;
using CPRD.KnowledgeDesk.Core.Services;
using Xunit;

namespace CPRD.KnowledgeDesk.App.Tests;

public sealed class MainWindowViewModelTests
{
    [Fact]
    public async Task Selecting_folder_loads_notes_without_database_access_in_viewmodel()
    {
        var paymentsId = Guid.NewGuid();
        var folders = new FakeFolderService(new Folder(
            paymentsId, null, "Payments", 0, false, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow));
        var notes = new FakeNoteService();

        var vm = new MainWindowViewModel(
            folders,
            notes,
            new FakeTagService(),
            new FakeSearchService(),
            new FakeDashboardService());

        await vm.InitializeAsync();
        await vm.SelectFolderCommand.ExecuteAsync(paymentsId);

        Assert.Equal(paymentsId, notes.LastRequestedFolderId);
    }

    [Fact]
    public async Task Show_dashboard_refreshes_data_and_switches_workspace_tab()
    {
        var folderId = Guid.NewGuid();
        var dashboard = new FakeDashboardService();
        var vm = new MainWindowViewModel(
            new FakeFolderService(new Folder(
                folderId, null, "General", 0, false, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow)),
            new FakeNoteService(),
            new FakeTagService(),
            new FakeSearchService(),
            dashboard);

        await vm.InitializeAsync();
        var refreshesAfterStartup = dashboard.RefreshCount;

        await vm.ShowDashboardCommand.ExecuteAsync(null);

        Assert.Equal(1, vm.SelectedWorkspaceIndex);
        Assert.Equal(refreshesAfterStartup + 1, dashboard.RefreshCount);
        Assert.Equal("Dashboard refreshed", vm.StatusText);
    }

    [Fact]
    public async Task Folder_tree_preserves_parent_child_hierarchy()
    {
        var rootId = Guid.NewGuid();
        var childId = Guid.NewGuid();
        var grandChildId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        var vm = new MainWindowViewModel(
            new FakeFolderService(
                new Folder(rootId, null, "Office", 0, false, now, now),
                new Folder(childId, rootId, "IT Operations", 0, false, now, now),
                new Folder(grandChildId, childId, "Firewall", 0, false, now, now)),
            new FakeNoteService(),
            new FakeTagService(),
            new FakeSearchService(),
            new FakeDashboardService());

        await vm.InitializeAsync();

        var office = Assert.Single(vm.FolderRoots);
        Assert.Equal("Office", office.Name);
        var it = Assert.Single(office.Children);
        Assert.Equal("IT Operations", it.Name);
        var firewall = Assert.Single(it.Children);
        Assert.Equal("Firewall", firewall.Name);
    }

    [Fact]
    public async Task Navigation_commands_query_all_favorites_pinned_and_recent()
    {
        var folderId = Guid.NewGuid();
        var folders = new FakeFolderService(new Folder(
            folderId, null, "General", 0, false, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow));
        var search = new FakeSearchService();

        var vm = new MainWindowViewModel(
            folders,
            new FakeNoteService(),
            new FakeTagService(),
            search,
            new FakeDashboardService());

        await vm.InitializeAsync();

        await vm.ShowAllNotesCommand.ExecuteAsync(null);
        Assert.NotNull(search.LastQuery);
        Assert.False(search.LastQuery!.FavoritesOnly);
        Assert.False(search.LastQuery.PinnedOnly);

        await vm.ShowFavoritesCommand.ExecuteAsync(null);
        Assert.True(search.LastQuery!.FavoritesOnly);

        await vm.ShowPinnedCommand.ExecuteAsync(null);
        Assert.True(search.LastQuery!.PinnedOnly);

        await vm.ShowRecentCommand.ExecuteAsync(null);
        Assert.False(search.LastQuery!.FavoritesOnly);
        Assert.False(search.LastQuery.PinnedOnly);
    }

    [Fact]
    public async Task Dashboard_time_kpis_open_corresponding_filtered_notes()
    {
        var folderId = Guid.NewGuid();
        var search = new FakeSearchService();
        var vm = new MainWindowViewModel(
            new FakeFolderService(new Folder(
                folderId, null, "General", 0, false, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow)),
            new FakeNoteService(),
            new FakeTagService(),
            search,
            new FakeDashboardService());

        await vm.InitializeAsync();

        await vm.ShowCreatedTodayCommand.ExecuteAsync(null);
        Assert.NotNull(search.LastQuery!.CreatedFromUtc);

        await vm.ShowCreatedThisWeekCommand.ExecuteAsync(null);
        Assert.NotNull(search.LastQuery!.CreatedFromUtc);

        await vm.ShowModifiedTodayCommand.ExecuteAsync(null);
        Assert.NotNull(search.LastQuery!.ModifiedFromUtc);
    }

    [Fact]
    public async Task Search_tracks_recent_queries_and_exposes_zero_result_message()
    {
        var folderId = Guid.NewGuid();
        var vm = new MainWindowViewModel(
            new FakeFolderService(new Folder(
                folderId, null, "General", 0, false, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow)),
            new FakeNoteService(),
            new FakeTagService(),
            new FakeSearchService(),
            new FakeDashboardService());

        await vm.InitializeAsync();
        vm.GlobalSearchText = "2027";
        await vm.SearchCommand.ExecuteAsync(null);

        Assert.Equal("2027", vm.RecentSearches[0]);
        Assert.Contains("No notes found", vm.EmptyStateText, StringComparison.OrdinalIgnoreCase);

        await vm.ClearSearchCommand.ExecuteAsync(null);

        Assert.Equal(string.Empty, vm.GlobalSearchText);
        Assert.Equal(string.Empty, vm.EmptyStateText);
    }

    [Fact]
    public async Task Note_sorting_reorders_visible_folder_notes()
    {
        var folderId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var notes = new FakeNoteService
        {
            NotesToReturn = new[]
            {
                new Note(
                    Guid.NewGuid(), "Zulu", string.Empty, "z body", folderId, "Standard Note",
                    false, false, false, now.AddDays(-2), now.AddMinutes(-10),
                    null, null, null, null, "{}"),
                new Note(
                    Guid.NewGuid(), "Alpha", string.Empty, "a body", folderId, "Standard Note",
                    false, false, false, now.AddDays(-1), now,
                    null, null, null, null, "{}")
            }
        };

        var vm = new MainWindowViewModel(
            new FakeFolderService(new Folder(
                folderId, null, "General", 0, false, now, now)),
            notes,
            new FakeTagService(),
            new FakeSearchService(),
            new FakeDashboardService());

        await vm.InitializeAsync();

        vm.SelectedNoteSort = "Title A-Z";
        Assert.Equal(new[] { "Alpha", "Zulu" }, vm.Notes.Select(note => note.Title));

        vm.SelectedNoteSort = "Oldest";
        Assert.Equal(new[] { "Zulu", "Alpha" }, vm.Notes.Select(note => note.Title));
    }

    [Fact]
    public async Task Archive_navigation_queries_archived_notes_and_returns_to_editor_workspace()
    {
        var folderId = Guid.NewGuid();
        var search = new FakeSearchService();
        var vm = new MainWindowViewModel(
            new FakeFolderService(new Folder(
                folderId, null, "General", 0, false, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow)),
            new FakeNoteService(),
            new FakeTagService(),
            search,
            new FakeDashboardService());

        await vm.InitializeAsync();
        await vm.ShowArchivedCommand.ExecuteAsync(null);

        Assert.NotNull(search.LastQuery);
        Assert.True(search.LastQuery!.IncludeArchived);
        Assert.Equal(0, vm.SelectedWorkspaceIndex);
        Assert.Contains("Archive", vm.StatusText, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Archive_and_trash_current_note_use_persistent_note_service()
    {
        var folderId = Guid.NewGuid();
        var noteId = Guid.NewGuid();
        var notes = new FakeNoteService
        {
            NoteToReturn = new Note(
                noteId,
                "Current",
                string.Empty,
                "body",
                folderId,
                "Standard Note",
                false,
                false,
                false,
                DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow,
                null,
                null,
                null,
                null,
                "{}")
        };

        var vm = new MainWindowViewModel(
            new FakeFolderService(new Folder(
                folderId, null, "General", 0, false, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow)),
            notes,
            new FakeTagService(),
            new FakeSearchService(),
            new FakeDashboardService());

        await vm.InitializeAsync();
        await vm.SelectNoteCommand.ExecuteAsync(noteId);
        await vm.ArchiveCurrentNoteCommand.ExecuteAsync(null);

        Assert.Equal(noteId, notes.LastArchivedId);

        notes.NoteToReturn = notes.NoteToReturn with { IsArchived = false };
        await vm.SelectNoteCommand.ExecuteAsync(noteId);
        await vm.MoveCurrentNoteToTrashCommand.ExecuteAsync(null);

        Assert.Equal(noteId, notes.LastTrashedId);
    }

    [Fact]
    public async Task Folder_management_refreshes_live_folder_collection()
    {
        var root = new Folder(
            Guid.NewGuid(), null, "General", 0, false, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
        var folders = new MutableFolderService(root);
        var vm = new MainWindowViewModel(
            folders,
            new FakeNoteService(),
            new FakeTagService(),
            new FakeSearchService(),
            new FakeDashboardService());

        await vm.InitializeAsync();

        var created = await vm.CreateFolderAsync(null, "Meetings");
        Assert.Contains(vm.Folders, folder => folder.Id == created.Id && folder.Name == "Meetings");

        await vm.RenameFolderAsync(created.Id, "Meetings 2027");
        Assert.Contains(vm.Folders, folder => folder.Id == created.Id && folder.Name == "Meetings 2027");

        await vm.ArchiveFolderAsync(created.Id);
        Assert.DoesNotContain(vm.Folders, folder => folder.Id == created.Id);
    }

    private sealed class MutableFolderService : IFolderService
    {
        private readonly List<Folder> _folders;
        public MutableFolderService(params Folder[] folders) => _folders = folders.ToList();

        public Task<IReadOnlyList<Folder>> GetTreeAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<Folder>>(_folders.ToArray());

        public Task<Folder?> FindByPathAsync(string path, CancellationToken cancellationToken) =>
            Task.FromResult<Folder?>(null);

        public Task<Folder> CreateAsync(Guid? parentId, string name, CancellationToken cancellationToken)
        {
            var now = DateTimeOffset.UtcNow;
            var folder = new Folder(Guid.NewGuid(), parentId, name, _folders.Count, false, now, now);
            _folders.Add(folder);
            return Task.FromResult(folder);
        }

        public Task RenameAsync(Guid folderId, string newName, CancellationToken cancellationToken)
        {
            var index = _folders.FindIndex(folder => folder.Id == folderId);
            _folders[index] = _folders[index] with { Name = newName };
            return Task.CompletedTask;
        }

        public Task MoveAsync(Guid folderId, Guid? newParentId, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task ArchiveAsync(Guid folderId, CancellationToken cancellationToken)
        {
            var index = _folders.FindIndex(folder => folder.Id == folderId);
            _folders[index] = _folders[index] with { IsArchived = true };
            return Task.CompletedTask;
        }

        public Task RestoreAsync(Guid folderId, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class FakeNoteService : INoteService
    {
        public Guid? LastRequestedFolderId { get; private set; }
        public Guid? LastArchivedId { get; private set; }
        public Guid? LastTrashedId { get; private set; }
        public Note? NoteToReturn { get; set; }
        public IReadOnlyList<Note> NotesToReturn { get; set; } = Array.Empty<Note>();
        public Task<Note> CreateAsync(NewNoteRequest request, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
        public Task<Note?> GetAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult(NoteToReturn?.Id == id ? NoteToReturn : null);
        public Task UpdateAsync(UpdateNoteRequest request, CancellationToken cancellationToken) =>
            Task.CompletedTask;
        public Task<IReadOnlyList<Note>> ListByFolderAsync(Guid folderId, CancellationToken cancellationToken)
        {
            LastRequestedFolderId = folderId;
            return Task.FromResult(NotesToReturn);
        }
        public Task MarkOpenedAsync(Guid id, DateTimeOffset openedAtUtc, CancellationToken cancellationToken) =>
            Task.CompletedTask;
        public Task ArchiveAsync(Guid id, CancellationToken cancellationToken)
        {
            LastArchivedId = id;
            return Task.CompletedTask;
        }
        public Task UnarchiveAsync(Guid id, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task MoveToTrashAsync(Guid id, CancellationToken cancellationToken)
        {
            LastTrashedId = id;
            return Task.CompletedTask;
        }
        public Task RestoreFromTrashAsync(Guid id, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task DeletePermanentlyAsync(Guid id, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task<int> PurgeTrashOlderThanAsync(DateTimeOffset cutoffUtc, CancellationToken cancellationToken) => Task.FromResult(0);
        public Task<IReadOnlyList<Note>> ListTrashAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<Note>>(Array.Empty<Note>());
    }

    private sealed class FakeFolderService : IFolderService
    {
        private readonly IReadOnlyList<Folder> _folders;
        public FakeFolderService(params Folder[] folders) => _folders = folders;
        public Task<IReadOnlyList<Folder>> GetTreeAsync(CancellationToken cancellationToken) => Task.FromResult(_folders);
        public Task<Folder?> FindByPathAsync(string path, CancellationToken cancellationToken) => Task.FromResult<Folder?>(null);
        public Task<Folder> CreateAsync(Guid? parentId, string name, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task RenameAsync(Guid folderId, string newName, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task MoveAsync(Guid folderId, Guid? newParentId, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task ArchiveAsync(Guid folderId, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task RestoreAsync(Guid folderId, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class FakeTagService : ITagService
    {
        public Task<IReadOnlyList<Tag>> GetAllAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<Tag>>(Array.Empty<Tag>());
        public Task<Tag> GetOrCreateAsync(string name, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task RenameAsync(Guid tagId, string newName, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class FakeSearchService : ISearchService
    {
        public SearchQuery? LastQuery { get; private set; }

        public Task<IReadOnlyList<SearchHit>> SearchAsync(SearchQuery query, CancellationToken cancellationToken)
        {
            LastQuery = query;
            return Task.FromResult<IReadOnlyList<SearchHit>>(Array.Empty<SearchHit>());
        }
    }

    private sealed class FakeDashboardService : IDashboardService
    {
        public int RefreshCount { get; private set; }

        public Task<DashboardSnapshot> GetSnapshotAsync(CancellationToken cancellationToken)
        {
            RefreshCount++;
            return Task.FromResult(new DashboardSnapshot(
                0, 0, 0, 0,
                Array.Empty<DashboardItem>(),
                Array.Empty<DashboardItem>(),
                Array.Empty<DashboardItem>(),
                "Not configured"));
        }
    }
}
