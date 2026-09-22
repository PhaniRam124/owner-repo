using CPRD.KnowledgeDesk.App.ViewModels;
using CPRD.KnowledgeDesk.Core.Models;
using CPRD.KnowledgeDesk.Core.Services;
using Xunit;

namespace CPRD.KnowledgeDesk.App.Tests;

public sealed class TrashViewModelTests
{
    [Fact]
    public async Task EmptyTrash_permanently_deletes_every_trashed_note()
    {
        var first = CreateNote("One");
        var second = CreateNote("Two");
        var notes = new FakeNoteService(first, second);
        var vm = new TrashViewModel(notes);

        await vm.RefreshAsync();
        await vm.EmptyTrashCommand.ExecuteAsync(null);

        Assert.Equal(
            new HashSet<Guid> { first.Id, second.Id },
            notes.DeletedIds.ToHashSet());
        Assert.Empty(vm.Items);
        Assert.Equal("Trash is empty", vm.Status);
    }

    private static Note CreateNote(string title)
    {
        var now = DateTimeOffset.UtcNow;
        return new Note(
            Guid.NewGuid(), title, string.Empty, string.Empty, Guid.NewGuid(),
            "Standard Note", false, false, false, now, now, null, now,
            null, null, "{}");
    }

    private sealed class FakeNoteService : INoteService
    {
        private readonly List<Note> _trash;
        public FakeNoteService(params Note[] notes) => _trash = notes.ToList();
        public List<Guid> DeletedIds { get; } = new();

        public Task<Note> CreateAsync(NewNoteRequest request, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
        public Task<Note?> GetAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult<Note?>(null);
        public Task UpdateAsync(UpdateNoteRequest request, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task<IReadOnlyList<Note>> ListByFolderAsync(Guid folderId, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<Note>>(Array.Empty<Note>());
        public Task MarkOpenedAsync(Guid id, DateTimeOffset openedAtUtc, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task ArchiveAsync(Guid id, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task UnarchiveAsync(Guid id, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task MoveToTrashAsync(Guid id, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task RestoreFromTrashAsync(Guid id, CancellationToken cancellationToken)
        {
            _trash.RemoveAll(note => note.Id == id);
            return Task.CompletedTask;
        }
        public Task DeletePermanentlyAsync(Guid id, CancellationToken cancellationToken)
        {
            DeletedIds.Add(id);
            _trash.RemoveAll(note => note.Id == id);
            return Task.CompletedTask;
        }
        public Task<int> PurgeTrashOlderThanAsync(DateTimeOffset cutoffUtc, CancellationToken cancellationToken) => Task.FromResult(0);
        public Task<IReadOnlyList<Note>> ListTrashAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<Note>>(_trash.ToArray());
    }
}
