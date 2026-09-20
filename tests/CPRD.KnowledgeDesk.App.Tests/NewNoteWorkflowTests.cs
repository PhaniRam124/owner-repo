using System.IO;
using CPRD.KnowledgeDesk.App.Services;
using CPRD.KnowledgeDesk.App.ViewModels;
using CPRD.KnowledgeDesk.Core.Models;
using CPRD.KnowledgeDesk.Core.Services;
using Xunit;

namespace CPRD.KnowledgeDesk.App.Tests;

public sealed class NewNoteWorkflowTests
{
    [Fact]
    public async Task New_note_uses_default_folder_and_opens_immediately()
    {
        var generalId = Guid.NewGuid();
        var folders = new FakeFolderService(
            new Folder(generalId, null, "General", 0, false, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow));
        var notes = new FakeNoteService();
        var workspace = new WorkspaceService();
        var editor = new EditorViewModel(notes);

        var vm = new MainWindowViewModel(
            folders,
            notes,
            new FakeTagService(),
            new FakeSearchService(),
            new FakeDashboardService(),
            workspace,
            editor);

        await vm.InitializeAsync();
        await vm.CreateNewNoteCommand.ExecuteAsync(null);

        Assert.Equal(generalId, vm.SelectedFolderId);
        Assert.Equal(generalId, notes.LastCreatedFolderId);
        Assert.Single(vm.Notes);
        Assert.NotNull(vm.Editor.CurrentNote);
        Assert.Equal(vm.Notes[0].Id, vm.Editor.CurrentNote!.Id);
    }

    [Fact]
    public async Task Repeated_new_note_creates_multiple_visible_notes()
    {
        var generalId = Guid.NewGuid();
        var folders = new FakeFolderService(
            new Folder(generalId, null, "General", 0, false, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow));
        var notes = new FakeNoteService();

        var vm = new MainWindowViewModel(
            folders,
            notes,
            new FakeTagService(),
            new FakeSearchService(),
            new FakeDashboardService(),
            new WorkspaceService(),
            new EditorViewModel(notes));

        await vm.InitializeAsync();
        await vm.CreateNewNoteCommand.ExecuteAsync(null);
        await vm.CreateNewNoteCommand.ExecuteAsync(null);

        Assert.Equal(2, notes.CreatedCount);
        Assert.Equal(2, vm.Notes.Count);
        Assert.Equal(2, vm.WorkspaceNotes.Count);
    }

    [Fact]
    public async Task New_note_failure_is_reported_without_crashing_command()
    {
        var generalId = Guid.NewGuid();
        var folders = new FakeFolderService(
            new Folder(generalId, null, "General", 0, false, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow));

        var vm = new MainWindowViewModel(
            folders,
            new ThrowingNoteService(),
            new FakeTagService(),
            new FakeSearchService(),
            new FakeDashboardService(),
            new WorkspaceService(),
            new EditorViewModel(new ThrowingNoteService()));

        await vm.InitializeAsync();
        await vm.CreateNewNoteCommand.ExecuteAsync(null);

        Assert.Contains("Unable to create note", vm.StatusText, StringComparison.OrdinalIgnoreCase);
    }

    private sealed class ThrowingNoteService : INoteService
    {
        public Task<Note> CreateAsync(NewNoteRequest request, CancellationToken cancellationToken) =>
            throw new IOException("simulated write failure");
        public Task<Note?> GetAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult<Note?>(null);
        public Task UpdateAsync(UpdateNoteRequest request, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task<IReadOnlyList<Note>> ListByFolderAsync(Guid folderId, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<Note>>(Array.Empty<Note>());
        public Task MarkOpenedAsync(Guid id, DateTimeOffset openedAtUtc, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task ArchiveAsync(Guid id, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task UnarchiveAsync(Guid id, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task MoveToTrashAsync(Guid id, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task RestoreFromTrashAsync(Guid id, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task DeletePermanentlyAsync(Guid id, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task<int> PurgeTrashOlderThanAsync(DateTimeOffset cutoffUtc, CancellationToken cancellationToken) => Task.FromResult(0);
        public Task<IReadOnlyList<Note>> ListTrashAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<Note>>(Array.Empty<Note>());
    }

    private sealed class FakeNoteService : INoteService
    {
        private readonly List<Note> _notes = new();

        public int CreatedCount { get; private set; }
        public Guid? LastCreatedFolderId { get; private set; }

        public Task<Note> CreateAsync(NewNoteRequest request, CancellationToken cancellationToken)
        {
            LastCreatedFolderId = request.FolderId;
            CreatedCount++;
            var now = DateTimeOffset.UtcNow;
            var note = new Note(
                Guid.NewGuid(),
                string.IsNullOrWhiteSpace(request.Title) ? $"Untitled Note {CreatedCount}" : request.Title,
                request.ContentPackage,
                request.PlainText,
                request.FolderId,
                request.NoteType,
                false,
                false,
                false,
                now,
                now,
                null,
                null,
                null,
                null,
                request.StructuredJson);
            _notes.Add(note);
            return Task.FromResult(note);
        }

        public Task<Note?> GetAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult(_notes.FirstOrDefault(n => n.Id == id));

        public Task UpdateAsync(UpdateNoteRequest request, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task<IReadOnlyList<Note>> ListByFolderAsync(Guid folderId, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<Note>>(_notes.Where(n => n.FolderId == folderId).ToArray());

        public Task MarkOpenedAsync(Guid id, DateTimeOffset openedAtUtc, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task ArchiveAsync(Guid id, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task UnarchiveAsync(Guid id, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task MoveToTrashAsync(Guid id, CancellationToken cancellationToken) => Task.CompletedTask;
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
        public Task<Folder?> FindByPathAsync(string path, CancellationToken cancellationToken) =>
            Task.FromResult(_folders.FirstOrDefault(f => f.Name.Equals(path, StringComparison.OrdinalIgnoreCase)));
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
        public Task<IReadOnlyList<SearchHit>> SearchAsync(SearchQuery query, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<SearchHit>>(Array.Empty<SearchHit>());
    }

    private sealed class FakeDashboardService : IDashboardService
    {
        public Task<DashboardSnapshot> GetSnapshotAsync(CancellationToken cancellationToken) =>
            Task.FromResult(new DashboardSnapshot(0, 1, 0, 0, Array.Empty<DashboardItem>(), Array.Empty<DashboardItem>(), Array.Empty<DashboardItem>(), "Ready"));
    }
}
