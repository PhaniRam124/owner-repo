using CPRD.KnowledgeDesk.Core.Services;
using CPRD.KnowledgeDesk.Infrastructure.Data;
using Xunit;

namespace CPRD.KnowledgeDesk.Infrastructure.Tests;

public sealed class KnowledgeDbTests
{
    [Fact]
    public async Task InitializeAsync_creates_core_tables_and_fts_index()
    {
        using var temp = new TempDirectory();
        var paths = new TestAppPaths(temp.Path);
        var db = new KnowledgeDb(paths);

        await db.InitializeAsync(CancellationToken.None);

        await using var connection = db.OpenConnection();
        await connection.OpenAsync();
        var command = connection.CreateCommand();
        command.CommandText = "SELECT name FROM sqlite_master WHERE name IN ('notes','folders','tags','note_tags','notes_fts') ORDER BY name";
        var names = new List<string>();
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync()) names.Add(reader.GetString(0));

        Assert.Equal(new[] { "folders", "note_tags", "notes", "notes_fts", "tags" }, names);
    }

    [Fact]
    public async Task InitializeAsync_enables_wal_and_busy_timeout()
    {
        using var temp = new TempDirectory();
        var paths = new TestAppPaths(temp.Path);
        var db = new KnowledgeDb(paths);

        await db.InitializeAsync(CancellationToken.None);

        await using var connection = db.OpenConnection();
        await connection.OpenAsync();

        var journal = connection.CreateCommand();
        journal.CommandText = "PRAGMA journal_mode";
        var mode = Convert.ToString(await journal.ExecuteScalarAsync());

        var timeout = connection.CreateCommand();
        timeout.CommandText = "PRAGMA busy_timeout";
        var busyTimeout = Convert.ToInt32(await timeout.ExecuteScalarAsync());

        Assert.Equal("wal", mode, ignoreCase: true);
        Assert.True(busyTimeout >= 5000);
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

    private sealed class TempDirectory : IDisposable
    {
        public TempDirectory()
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "CPRDKnowledgeDeskTests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path);
        }

        public string Path { get; }

        public void Dispose()
        {
            try
            {
                if (Directory.Exists(Path)) Directory.Delete(Path, recursive: true);
            }
            catch
            {
            }
        }
    }
}
