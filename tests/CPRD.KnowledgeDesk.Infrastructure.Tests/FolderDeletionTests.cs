using CPRD.KnowledgeDesk.Core.Models;
using CPRD.KnowledgeDesk.Core.Services;
using CPRD.KnowledgeDesk.Infrastructure.Data;
using CPRD.KnowledgeDesk.Infrastructure.Repositories;
using CPRD.KnowledgeDesk.Infrastructure.Services;
using Xunit;

namespace CPRD.KnowledgeDesk.Infrastructure.Tests;

public sealed class FolderDeletionTests
{
    [Fact]
    public async Task Delete_moves_notes_preserves_children_and_removes_folder()
    {
        using var env = new TestEnvironment();
        await env.Db.InitializeAsync(default);

        var root = await env.Folders.CreateAsync(null, "Delete Me", default);
        var child = await env.Folders.CreateAsync(root.Id, "Child", default);
        var destination = await env.Folders.CreateAsync(null, "Destination", default);

        var note = await env.Notes.CreateAsync(
            new NewNoteRequest(
                "Move me",
                string.Empty,
                "body",
                root.Id,
                "Standard Note",
                "{}",
                Array.Empty<string>()),
            default);

        await env.Folders.DeleteAsync(root.Id, destination.Id, default);

        var moved = await env.Notes.GetAsync(note.Id, default);
        Assert.NotNull(moved);
        Assert.Equal(destination.Id, moved!.FolderId);

        var allFolders = await env.Folders.GetTreeAsync(default);
        Assert.DoesNotContain(allFolders, folder => folder.Id == root.Id);

        var preservedChild = Assert.Single(allFolders.Where(folder => folder.Id == child.Id));
        Assert.Null(preservedChild.ParentId);
    }

    private sealed class TestEnvironment : IDisposable
    {
        private readonly string _root;

        public TestEnvironment()
        {
            _root = Path.Combine(
                Path.GetTempPath(),
                "CPRDKnowledgeDeskFolderDeleteTests",
                Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_root);

            var paths = new TestAppPaths(_root);
            Db = new KnowledgeDb(paths);
            var folderRepository = new FolderRepository(Db);
            var searchRepository = new SearchRepository(Db);
            Folders = new FolderService(folderRepository, searchRepository, Db);
            var noteRepository = new NoteRepository(Db);
            Notes = new NoteService(noteRepository, searchRepository, Db);
        }

        public KnowledgeDb Db { get; }
        public IFolderService Folders { get; }
        public INoteService Notes { get; }

        public void Dispose()
        {
            try
            {
                Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
                if (Directory.Exists(_root))
                    Directory.Delete(_root, true);
            }
            catch
            {
            }
        }
    }

    private sealed class TestAppPaths : IAppPaths
    {
        public TestAppPaths(string root)
        {
            DataDirectory = Path.Combine(root, "Data");
            DatabasePath = Path.Combine(DataDirectory, "knowledge.db");
            AttachmentsDirectory = Path.Combine(root, "Attachments");
            MigrationSafetyDirectory = Path.Combine(root, "MigrationSafety");
            DocumentsDirectory = Path.Combine(root, "Documents");
            BackupsDirectory = Path.Combine(DocumentsDirectory, "Backups");
            ExportsDirectory = Path.Combine(DocumentsDirectory, "Exports");
        }

        public string DataDirectory { get; }
        public string DatabasePath { get; }
        public string AttachmentsDirectory { get; }
        public string MigrationSafetyDirectory { get; }
        public string DocumentsDirectory { get; }
        public string BackupsDirectory { get; }
        public string ExportsDirectory { get; }
    }
}
