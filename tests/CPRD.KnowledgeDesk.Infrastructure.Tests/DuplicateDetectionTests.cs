using CPRD.KnowledgeDesk.Core.Services;
using CPRD.KnowledgeDesk.Infrastructure.Data;
using CPRD.KnowledgeDesk.Infrastructure.Repositories;
using CPRD.KnowledgeDesk.Infrastructure.Services;
using Xunit;

namespace CPRD.KnowledgeDesk.Infrastructure.Tests;

public sealed class DuplicateDetectionTests
{
    [Theory]
    [InlineData("HIM SIM Payment", "Payment done", "him sim payment", "Payment done", "ExactContent")]
    [InlineData("Fortigate Renewal", "Payment to vendor", "FortiGate Renewal 2026", "Payment to vendor today", "HighSimilarity")]
    public async Task Finds_expected_duplicate_classification(
        string existingTitle,
        string existingContent,
        string probeTitle,
        string probeContent,
        string expected)
    {
        using var env = await TestEnvironment.CreateAsync();
        await env.CreateNoteAsync(existingTitle, existingContent);

        var candidates = await env.Duplicates.FindCandidatesAsync(
            new DuplicateProbe(probeTitle, probeContent),
            default);

        Assert.Contains(candidates, candidate => candidate.Reason == expected);
    }

    private sealed class TestEnvironment : IDisposable
    {
        private TestEnvironment(string root, KnowledgeDb db, INoteService notes, IDuplicateDetectionService duplicates)
        {
            Root = root;
            Db = db;
            Notes = notes;
            Duplicates = duplicates;
        }

        public string Root { get; }
        public KnowledgeDb Db { get; }
        public INoteService Notes { get; }
        public IDuplicateDetectionService Duplicates { get; }

        public static async Task<TestEnvironment> CreateAsync()
        {
            var root = Path.Combine(Path.GetTempPath(), "CPRDKnowledgeDeskTests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            var db = new KnowledgeDb(new TestAppPaths(root));
            await db.InitializeAsync(default);
            var notes = new NoteService(new NoteRepository(db), new SearchRepository(db), db);
            var duplicates = new DuplicateDetectionService(db);
            return new TestEnvironment(root, db, notes, duplicates);
        }

        public async Task CreateNoteAsync(string title, string body)
        {
            await using var connection = Db.OpenConnection();
            await connection.OpenAsync();
            var command = connection.CreateCommand();
            command.CommandText = "SELECT id FROM folders WHERE name='Office' AND parent_id IS NULL";
            var folderId = Guid.Parse((string)(await command.ExecuteScalarAsync())!);
            await Notes.CreateAsync(
                new NewNoteRequest(title, string.Empty, body, folderId, "Standard Note", "{}", Array.Empty<string>()),
                default);
        }

        public void Dispose()
        {
            try
            {
                if (Directory.Exists(Root)) Directory.Delete(Root, true);
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
