using CPRD.KnowledgeDesk.Core.Models;
using CPRD.KnowledgeDesk.Core.Services;
using CPRD.KnowledgeDesk.Infrastructure.Data;
using CPRD.KnowledgeDesk.Infrastructure.Repositories;
using CPRD.KnowledgeDesk.Infrastructure.Services;
using Xunit;

namespace CPRD.KnowledgeDesk.Infrastructure.Tests;

public sealed class SearchDateTests
{
    [Fact]
    public async Task Search_matches_created_and_modified_date_text()
    {
        using var env = new TestEnvironment();
        await env.Db.InitializeAsync(default);

        var folderId = await GetFolderIdAsync(env.Db, "Office");
        var note = await env.Notes.CreateAsync(
            new NewNoteRequest(
                "Conference plan",
                string.Empty,
                "No year in this text",
                folderId,
                "Standard Note",
                "{}",
                Array.Empty<string>()),
            default);

        await using (var connection = env.Db.OpenConnection())
        {
            await connection.OpenAsync();
            var command = connection.CreateCommand();
            command.CommandText = """
                UPDATE notes
                   SET created_at_utc='2027-01-15T09:30:00.0000000+00:00',
                       modified_at_utc='2027-02-20T10:45:00.0000000+00:00'
                 WHERE id=$id
                """;
            command.Parameters.AddWithValue("$id", note.Id.ToString("D"));
            await command.ExecuteNonQueryAsync();
        }

        var results = await env.Search.SearchAsync(
            new SearchQuery(
                "2027",
                null,
                Array.Empty<Guid>(),
                null,
                null,
                null,
                false,
                false,
                false,
                false,
                20),
            default);

        Assert.Contains(results, hit => hit.NoteId == note.Id);
    }

    private static async Task<Guid> GetFolderIdAsync(KnowledgeDb db, string name)
    {
        await using var connection = db.OpenConnection();
        await connection.OpenAsync();
        var command = connection.CreateCommand();
        command.CommandText = "SELECT id FROM folders WHERE name=$name LIMIT 1";
        command.Parameters.AddWithValue("$name", name);
        return Guid.Parse((string)(await command.ExecuteScalarAsync())!);
    }

    private sealed class TestEnvironment : IDisposable
    {
        private readonly string _root;

        public TestEnvironment()
        {
            _root = Path.Combine(
                Path.GetTempPath(),
                "CPRDKnowledgeDeskSearchDateTests",
                Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_root);

            var paths = new TestAppPaths(_root);
            Db = new KnowledgeDb(paths);
            var notesRepo = new NoteRepository(Db);
            var searchRepo = new SearchRepository(Db);
            Notes = new NoteService(notesRepo, searchRepo, Db);
            Search = new SearchService(Db);
        }

        public KnowledgeDb Db { get; }
        public INoteService Notes { get; }
        public ISearchService Search { get; }

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
