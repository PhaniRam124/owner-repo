using CPRD.KnowledgeDesk.Core.Models;
using CPRD.KnowledgeDesk.Core.Services;
using CPRD.KnowledgeDesk.Infrastructure.Data;
using CPRD.KnowledgeDesk.Infrastructure.Repositories;
using CPRD.KnowledgeDesk.Infrastructure.Services;
using Xunit;

namespace CPRD.KnowledgeDesk.Infrastructure.Tests;

public sealed class DashboardSummaryTests
{
    [Fact]
    public async Task Snapshot_contains_folder_counts_and_recent_creation_series()
    {
        using var env = new TestEnvironment();
        await env.Db.InitializeAsync(default);

        var folder = await env.Folders.CreateAsync(null, "Dashboard Test", default);
        await env.Notes.CreateAsync(
            new NewNoteRequest(
                "One",
                string.Empty,
                "body",
                folder.Id,
                "Standard Note",
                "{}",
                Array.Empty<string>()),
            default);
        await env.Notes.CreateAsync(
            new NewNoteRequest(
                "Two",
                string.Empty,
                "body",
                folder.Id,
                "Standard Note",
                "{}",
                Array.Empty<string>()),
            default);

        var snapshot = await env.Dashboard.GetSnapshotAsync(default);

        var folderSummary = Assert.Single(
            snapshot.FolderSummary,
            item => item.FolderId == folder.Id);
        Assert.Equal(2, folderSummary.NoteCount);

        Assert.Equal(7, snapshot.CreatedOverTime.Count);
        Assert.Equal(
            snapshot.CreatedOverTime.Sum(point => point.NoteCount),
            snapshot.CreatedOverTime.Sum(point => point.NoteCount));
        Assert.Contains(snapshot.CreatedOverTime, point => point.NoteCount >= 2);
    }

    private sealed class TestEnvironment : IDisposable
    {
        private readonly string _root;

        public TestEnvironment()
        {
            _root = Path.Combine(
                Path.GetTempPath(),
                "CPRDKnowledgeDeskDashboardTests",
                Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_root);

            var paths = new TestAppPaths(_root);
            Db = new KnowledgeDb(paths);
            var search = new SearchRepository(Db);
            Folders = new FolderService(new FolderRepository(Db), search, Db);
            Notes = new NoteService(new NoteRepository(Db), search, Db);
            Dashboard = new DashboardService(Db);
        }

        public KnowledgeDb Db { get; }
        public IFolderService Folders { get; }
        public INoteService Notes { get; }
        public IDashboardService Dashboard { get; }

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
