using CPRD.KnowledgeDesk.App.ViewModels;
using CPRD.KnowledgeDesk.Core.Models;
using CPRD.KnowledgeDesk.Core.Services;
using Xunit;

namespace CPRD.KnowledgeDesk.App.Tests;

public sealed class QuickCaptureDuplicateTests
{
    [Fact]
    public async Task First_save_warns_duplicate_second_save_keeps_both()
    {
        var folder = new Folder(
            Guid.NewGuid(),
            null,
            "General",
            0,
            false,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow);

        var notes = new FakeNoteService();
        var vm = new QuickCaptureViewModel(
            notes,
            new FakeFolderService(folder),
            new FakeDuplicateDetectionService());

        await vm.InitializeAsync();
        vm.Title = "Freeman";
        vm.PlainText = "Booth shipping details";

        await vm.SaveCommand.ExecuteAsync(null);

        Assert.Equal(0, notes.CreatedCount);
        Assert.Contains("Possible duplicate", vm.Status, StringComparison.OrdinalIgnoreCase);

        await vm.SaveCommand.ExecuteAsync(null);

        Assert.Equal(1, notes.CreatedCount);
    }

    private sealed class FakeDuplicateDetectionService : IDuplicateDetectionService
    {
        public Task<IReadOnlyList<DuplicateCandidate>> FindCandidatesAsync(
            DuplicateProbe probe,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<DuplicateCandidate>>(
                new[]
                {
                    new DuplicateCandidate(
                        Guid.NewGuid(),
                        "Freeman",
                        "ExactTitle",
                        1.0)
                });
    }

    private sealed class FakeFolderService : IFolderService
    {
        private readonly Folder _folder;
        public FakeFolderService(Folder folder) => _folder = folder;

        public Task<IReadOnlyList<Folder>> GetTreeAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<Folder>>(new[] { _folder });

        public Task<Folder?> FindByPathAsync(string path, CancellationToken cancellationToken) =>
            Task.FromResult<Folder?>(_folder);

        public Task<Folder> CreateAsync(Guid? parentId, string name, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task RenameAsync(Guid folderId, string newName, CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public Task MoveAsync(Guid folderId, Guid? newParentId, CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public Task ArchiveAsync(Guid folderId, CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public Task RestoreAsync(Guid folderId, CancellationToken cancellationToken) =>
            Task.CompletedTask;
    }

    private sealed class FakeNoteService : INoteService
    {
        public int CreatedCount { get; private set; }

        public Task<Note> CreateAsync(NewNoteRequest request, CancellationToken cancellationToken)
        {
            CreatedCount++;
            var now = DateTimeOffset.UtcNow;
            return Task.FromResult(new Note(
                Guid.NewGuid(),
                request.Title,
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
                request.StructuredJson));
        }

        public Task<Note?> GetAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult<Note?>(null);

        public Task UpdateAsync(UpdateNoteRequest request, CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public Task<IReadOnlyList<Note>> ListByFolderAsync(Guid folderId, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<Note>>(Array.Empty<Note>());

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
}
