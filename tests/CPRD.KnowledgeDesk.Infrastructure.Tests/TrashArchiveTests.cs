using CPRD.KnowledgeDesk.Core.Models;
using CPRD.KnowledgeDesk.Core.Services;
using CPRD.KnowledgeDesk.Infrastructure.Data;
using CPRD.KnowledgeDesk.Infrastructure.Repositories;
using CPRD.KnowledgeDesk.Infrastructure.Services;
using Xunit;

namespace CPRD.KnowledgeDesk.Infrastructure.Tests;

public sealed class TrashArchiveTests
{
    [Fact]
    public async Task Trashed_note_is_removed_from_default_search_and_can_be_restored()
    {
        using var env = await TestEnvironment.CreateAsync();
        var note = await env.CreateNoteAsync("Temporary", "unique-trash-token");

        await env.Notes.MoveToTrashAsync(note.Id, default);

        Assert.Empty(await env.Search.SearchAsync(Query("unique-trash-token"), default));

        await env.Notes.RestoreFromTrashAsync(note.Id, default);

        Assert.Single(await env.Search.SearchAsync(Query("unique-trash-token"), default));
    }

    private static SearchQuery Query(string text) =>
        new(text, null, Array.Empty<Guid>(), null, null, null, false, false, false, false, 50);

    private sealed class TestEnvironment : IDisposable
    {
        private readonly string _root;

        private TestEnvironment(string root, KnowledgeDb db, INoteService notes, ISearchService search)
        {
            _root = root;
            Db = db;
            Notes = notes;
            Search = search;
        }

        public KnowledgeDb Db { get; }
        public INoteService Notes { get; }
        public ISearchService Search { get; }

        public static async Task<TestEnvironment> CreateAsync()
        {
            var root = Path.Combine(Path.GetTempPath(), "CPRDKnowledgeDeskTests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            var db = new KnowledgeDb(new TestAppPaths(root));
            await db.InitializeAsync(default);
            var notes = new NoteService(new NoteRepository(db), new SearchRepository(db), db);
            return new TestEnvironment(root, db, notes, new SearchService(db));
        }

        public async Task<Note> CreateNoteAsync(string title, string body)
        {
            await using var connection = Db.OpenConnection();
            await connection.OpenAsync();
            var command = connection.CreateCommand();
            command.CommandText = "SELECT id FROM folders WHERE name='Office' AND parent_id IS NULL";
            var folderId = Guid.Parse((string)(await command.ExecuteScalarAsync())!);

            return await Notes.CreateAsync(
                new NewNoteRequest(title, string.Empty, body, folderId, "Standard Note", "{}", Array.Empty<string>()),
                default);
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
