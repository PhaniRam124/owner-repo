using CPRD.KnowledgeDesk.App.Services;
using CPRD.KnowledgeDesk.App.ViewModels;
using CPRD.KnowledgeDesk.Core.Models;
using CPRD.KnowledgeDesk.Core.Services;
using Xunit;

namespace CPRD.KnowledgeDesk.App.Tests;

public sealed class EditorRecoveryTests
{
    [Fact]
    public async Task Dirty_editor_writes_recovery_draft_before_database_autosave()
    {
        var notes = new FakeNoteService();
        var recovery = new FakeRecoveryService();
        using var autoSave = new AutoSaveCoordinator(new SystemDelayScheduler(), TimeSpan.FromSeconds(10));
        using var vm = new EditorViewModel(notes, new FakeRevisionService(), autoSave, recovery);

        var now = DateTimeOffset.UtcNow;
        var note = new Note(
            Guid.NewGuid(),
            "Draft",
            string.Empty,
            "old",
            Guid.NewGuid(),
            "Standard Note",
            false,
            false,
            false,
            now,
            now,
            null,
            null,
            null,
            null,
            "{}");

        vm.Load(note);
        vm.PlainText = "unsaved recovery text";

        await Task.Delay(450);

        Assert.NotNull(recovery.LastDraft);
        Assert.Equal(note.Id, recovery.LastDraft!.NoteId);
        Assert.Equal("unsaved recovery text", recovery.LastDraft.PlainText);
        Assert.Equal(0, notes.UpdateCount);
    }

    private sealed class FakeRecoveryService : IRecoveryService
    {
        public RecoveryDraft? LastDraft { get; private set; }
        public Task SaveDraftAsync(RecoveryDraft draft, CancellationToken cancellationToken)
        {
            LastDraft = draft;
            return Task.CompletedTask;
        }
        public Task DeleteDraftAsync(Guid noteId, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task<IReadOnlyList<RecoveryDraft>> ListPendingAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<RecoveryDraft>>(Array.Empty<RecoveryDraft>());
        public Task<int> RecoverPendingAsync(CancellationToken cancellationToken) => Task.FromResult(0);
    }

    private sealed class FakeRevisionService : IRevisionService
    {
        public Task<NoteRevision> CreateRevisionAsync(Note note, CancellationToken cancellationToken) =>
            Task.FromResult(new NoteRevision(1, note.Id, 1, note.Title, note.ContentPackage, note.PlainText, note.StructuredJson, DateTimeOffset.UtcNow));
        public Task<IReadOnlyList<NoteRevision>> ListAsync(Guid noteId, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<NoteRevision>>(Array.Empty<NoteRevision>());
        public Task RestoreAsync(Guid noteId, long revisionId, CancellationToken cancellationToken) =>
            Task.CompletedTask;
    }

    private sealed class FakeNoteService : INoteService
    {
        public int UpdateCount { get; private set; }
        public Task<Note> CreateAsync(NewNoteRequest request, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<Note?> GetAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult<Note?>(null);
        public Task UpdateAsync(UpdateNoteRequest request, CancellationToken cancellationToken)
        {
            UpdateCount++;
            return Task.CompletedTask;
        }
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
}
