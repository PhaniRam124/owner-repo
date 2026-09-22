using CPRD.KnowledgeDesk.App.ViewModels;
using CPRD.KnowledgeDesk.Core.Models;
using CPRD.KnowledgeDesk.Core.Services;
using Xunit;

namespace CPRD.KnowledgeDesk.App.Tests;

public sealed class EditorPersistenceTests
{
    [Fact]
    public void Loading_another_note_advances_document_version_after_content_is_loaded()
    {
        var notes = new FakeNoteService();
        using var vm = new EditorViewModel(notes);

        var first = CreateNote("First", "First body", "<first/>");
        var second = CreateNote("Second", "Second body", "<second/>");

        vm.Load(first);
        var firstVersion = vm.DocumentVersion;

        vm.Load(second);

        Assert.True(vm.DocumentVersion > firstVersion);
        Assert.Equal("Second body", vm.PlainText);
        Assert.Equal("<second/>", vm.ContentPackage);
        Assert.Equal(second.Id, vm.CurrentNote!.Id);
    }

    [Fact]
    public async Task Saving_edited_note_preserves_loaded_tags()
    {
        var notes = new FakeNoteService();
        using var vm = new EditorViewModel(notes);
        var note = CreateNote("Tagged", "body", string.Empty);

        vm.Load(note);
        vm.LoadTags(new[] { "Conference", "Vendor" });
        vm.Title = "Tagged Updated";

        await vm.FlushAsync(true, CancellationToken.None);

        Assert.NotNull(notes.LastUpdate);
        Assert.Equal(
            new[] { "Conference", "Vendor" },
            notes.LastUpdate!.Tags.OrderBy(value => value, StringComparer.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Changing_folder_is_persisted_with_note_save()
    {
        var notes = new FakeNoteService();
        using var vm = new EditorViewModel(notes);
        var note = CreateNote("Move Me", "body", string.Empty);
        var destination = Guid.NewGuid();

        vm.Load(note);
        vm.LoadTags(Array.Empty<string>());
        vm.SelectedFolderId = destination;

        await vm.FlushAsync(true, CancellationToken.None);

        Assert.NotNull(notes.LastUpdate);
        Assert.Equal(destination, notes.LastUpdate!.FolderId);
    }

    private static Note CreateNote(string title, string plainText, string package)
    {
        var now = DateTimeOffset.UtcNow;
        return new Note(
            Guid.NewGuid(),
            title,
            package,
            plainText,
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
    }

    private sealed class FakeNoteService : INoteService
    {
        public UpdateNoteRequest? LastUpdate { get; private set; }

        public Task<Note> CreateAsync(NewNoteRequest request, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
        public Task<Note?> GetAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult<Note?>(null);
        public Task UpdateAsync(UpdateNoteRequest request, CancellationToken cancellationToken)
        {
            LastUpdate = request;
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
