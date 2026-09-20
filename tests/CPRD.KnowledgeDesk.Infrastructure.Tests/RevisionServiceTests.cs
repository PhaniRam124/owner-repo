using CPRD.KnowledgeDesk.Core.Services;
using CPRD.KnowledgeDesk.Infrastructure.Data;
using CPRD.KnowledgeDesk.Infrastructure.Repositories;
using CPRD.KnowledgeDesk.Infrastructure.Services;
using Xunit;

namespace CPRD.KnowledgeDesk.Infrastructure.Tests;

public sealed class RevisionServiceTests
{
    [Fact]
    public async Task RestoreAsync_creates_new_current_revision_without_destroying_history()
    {
        using var env = await TestEnvironment.CreateAsync();
        var officeId = await env.GetRootFolderIdAsync("Office");
        var note = await env.Notes.CreateAsync(
            new NewNoteRequest("One", string.Empty, "First", officeId, "Standard Note", "{}", Array.Empty<string>()),
            default);

        await env.Revisions.CreateRevisionAsync(note, default);
        await env.Notes.UpdateAsync(
            new UpdateNoteRequest(note.Id, "Two", string.Empty, "Second", officeId, "Standard Note", false, false, "{}", Array.Empty<string>()),
            default);

        var revision = Assert.Single(await env.Revisions.ListAsync(note.Id, default));
        await env.Revisions.RestoreAsync(note.Id, revision.Id, default);

        var restored = await env.Notes.GetAsync(note.Id, default);
        Assert.Equal("One", restored!.Title);
        Assert.Equal("First", restored.PlainText);
        Assert.True((await env.Revisions.ListAsync(note.Id, default)).Count >= 2);
    }

    private sealed class TestEnvironment : IDisposable
    {
        private readonly string _root;

        private TestEnvironment(string root, KnowledgeDb db, INoteService notes, IRevisionService revisions)
        {
            _root = root;
            Db = db;
            Notes = notes;
            Revisions = revisions;
        }

        public KnowledgeDb Db { get; }
        public INoteService Notes { get; }
        public IRevisionService Revisions { get; }

        public static async Task<TestEnvironment> CreateAsync()
        {
            var root = Path.Combine(Path.GetTempPath(), "CPRDKnowledgeDeskTests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            var db = new KnowledgeDb(new TestAppPaths(root));
            await db.InitializeAsync(default);
            var notes = new NoteService(new NoteRepository(db), new SearchRepository(db), db);
            var revisions = new RevisionService(new RevisionRepository(db), new NoteRepository(db), new SearchRepository(db), db);
            return new TestEnvironment(root, db, notes, revisions);
        }

        public async Task<Guid> GetRootFolderIdAsync(string name)
        {
            await using var connection = Db.OpenConnection();
            await connection.OpenAsync();
            var command = connection.CreateCommand();
            command.CommandText = "SELECT id FROM folders WHERE name=$name AND parent_id IS NULL";
            command.Parameters.AddWithValue("$name", name);
            return Guid.Parse((string)(await command.ExecuteScalarAsync())!);
        }

        public void Dispose()
        {
            try
            {
                if (Directory.Exists(_root)) Directory.Delete(_root, true);
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
