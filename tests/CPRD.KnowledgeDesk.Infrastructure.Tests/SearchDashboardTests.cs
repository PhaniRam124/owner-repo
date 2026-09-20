using CPRD.KnowledgeDesk.Core.Models;
using CPRD.KnowledgeDesk.Core.Services;
using CPRD.KnowledgeDesk.Infrastructure.Data;
using CPRD.KnowledgeDesk.Infrastructure.Repositories;
using CPRD.KnowledgeDesk.Infrastructure.Services;
using Xunit;

namespace CPRD.KnowledgeDesk.Infrastructure.Tests;

public sealed class SearchDashboardTests
{
    [Fact]
    public async Task SearchAsync_matches_body_and_respects_folder_filter()
    {
        using var env = await TestEnvironment.CreateAsync();
        var folders = new FolderService(new FolderRepository(env.Db), new SearchRepository(env.Db), env.Db);
        var firewall = await folders.FindByPathAsync("Office > IT Operations > Firewall", default);
        var office = await folders.FindByPathAsync("Office", default);
        Assert.NotNull(firewall);
        Assert.NotNull(office);

        var notes = new NoteService(new NoteRepository(env.Db), new SearchRepository(env.Db), env.Db);
        await notes.CreateAsync(new NewNoteRequest(
            "Firewall Renewal",
            "<FlowDocument />",
            "FortiGate 200F renewal and support payment",
            firewall!.Id,
            "Standard Note",
            "{}",
            new[] { "Firewall", "Renewal" }), default);

        await notes.CreateAsync(new NewNoteRequest(
            "Unrelated Office Note",
            "<FlowDocument />",
            "FortiGate mentioned outside the firewall folder",
            office!.Id,
            "Standard Note",
            "{}",
            Array.Empty<string>()), default);

        var search = new SearchService(env.Db);
        var result = await search.SearchAsync(
            new SearchQuery("fortigate", firewall.Id, Array.Empty<Guid>(), null, null, null, false, false, false, false, 50),
            default);

        Assert.Single(result);
        Assert.Contains("fortigate", result[0].Snippet, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Dashboard_snapshot_counts_active_notes_and_favorites()
    {
        using var env = await TestEnvironment.CreateAsync();
        var officeId = await GetFolderIdAsync(env.Db, "Office");
        var notes = new NoteService(new NoteRepository(env.Db), new SearchRepository(env.Db), env.Db);
        var created = await notes.CreateAsync(new NewNoteRequest(
            "Pinned Reference", "<FlowDocument />", "Reference body", officeId, "Standard Note", "{}", Array.Empty<string>()), default);
        await notes.UpdateAsync(new UpdateNoteRequest(
            created.Id, created.Title, created.ContentPackage, created.PlainText, officeId, created.NoteType, true, true, "{}", Array.Empty<string>()), default);

        var dashboard = new DashboardService(env.Db);
        var snapshot = await dashboard.GetSnapshotAsync(default);

        Assert.Equal(1, snapshot.TotalNotes);
        Assert.Equal(1, snapshot.FavoriteCount);
        Assert.Single(snapshot.Pinned);
    }

    private static async Task<Guid> GetFolderIdAsync(KnowledgeDb db, string name)
    {
        await using var connection = db.OpenConnection();
        await connection.OpenAsync();
        var command = connection.CreateCommand();
        command.CommandText = "SELECT id FROM folders WHERE name=$name AND parent_id IS NULL";
        command.Parameters.AddWithValue("$name", name);
        return Guid.Parse((string)(await command.ExecuteScalarAsync())!);
    }

    private sealed class TestEnvironment : IDisposable
    {
        private readonly string _root;

        private TestEnvironment(string root, KnowledgeDb db)
        {
            _root = root;
            Db = db;
        }

        public KnowledgeDb Db { get; }

        public static async Task<TestEnvironment> CreateAsync()
        {
            var root = Path.Combine(Path.GetTempPath(), "CPRDKnowledgeDeskTests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            var paths = new TestAppPaths(root);
            var db = new KnowledgeDb(paths);
            await db.InitializeAsync(default);
            return new TestEnvironment(root, db);
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
