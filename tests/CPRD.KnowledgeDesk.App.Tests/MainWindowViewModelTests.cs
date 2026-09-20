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

    private sealed class FakeNoteService : INoteService
    {
        public Guid? LastRequestedFolderId { get; private set; }
        public Task<Note> CreateAsync(NewNoteRequest request, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
        public Task<Note?> GetAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult<Note?>(null);
        public Task UpdateAsync(UpdateNoteRequest request, CancellationToken cancellationToken) =>
            Task.CompletedTask;
        public Task<IReadOnlyList<Note>> ListByFolderAsync(Guid folderId, CancellationToken cancellationToken)
        {
            LastRequestedFolderId = folderId;
            return Task.FromResult<IReadOnlyList<Note>>(Array.Empty<Note>());
        }
        public Task MarkOpenedAsync(Guid id, DateTimeOffset openedAtUtc, CancellationToken cancellationToken) =>
            Task.CompletedTask;
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
        public Task<IReadOnlyList<SearchHit>> SearchAsync(SearchQuery query, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<SearchHit>>(Array.Empty<SearchHit>());
    }

    private sealed class FakeDashboardService : IDashboardService
    {
        public Task<DashboardSnapshot> GetSnapshotAsync(CancellationToken cancellationToken) =>
            Task.FromResult(new DashboardSnapshot(0, 0, 0, 0, Array.Empty<DashboardItem>(), Array.Empty<DashboardItem>(), Array.Empty<DashboardItem>(), "Not configured"));
    }
}
