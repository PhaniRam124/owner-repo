using CPRD.KnowledgeDesk.Core.Services;
using CPRD.KnowledgeDesk.Infrastructure.Data;
using CPRD.KnowledgeDesk.Infrastructure.Repositories;
using CPRD.KnowledgeDesk.Infrastructure.Services;
using Xunit;

namespace CPRD.KnowledgeDesk.Infrastructure.Tests;

public sealed class BackupServiceTests
{
    [Fact]
    public async Task Backup_then_restore_recovers_previous_database_state()
    {
        using var temp = new TempDirectory();
        var paths = new TestAppPaths(temp.Path);
        var db = new KnowledgeDb(paths);
        await db.InitializeAsync(default);

        var notes = new NoteService(new NoteRepository(db), new SearchRepository(db), db);
        var folderId = await GetFolderIdAsync(db, "Office");

        var original = await notes.CreateAsync(
            new NewNoteRequest("Original", string.Empty, "first version", folderId, "Standard Note", "{}", Array.Empty<string>()),
            default);

        var backups = new BackupService(db, paths);
        var backup = await backups.BackupNowAsync(default);

        await notes.UpdateAsync(
            new UpdateNoteRequest(original.Id, "Changed", string.Empty, "second version", folderId, "Standard Note", false, false, "{}", Array.Empty<string>()),
            default);

        await backups.RestoreAsync(backup.Path, default);

        var restored = await notes.GetAsync(original.Id, default);
        Assert.NotNull(restored);
        Assert.Equal("Original", restored!.Title);
        Assert.Equal("first version", restored.PlainText);
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
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "CPRDKnowledgeDeskBackupTests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path);
        }

        public string Path { get; }

        public void Dispose()
        {
            try
            {
                Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
                if (Directory.Exists(Path)) Directory.Delete(Path, true);
            }
            catch
            {
            }
        }
    }
}
