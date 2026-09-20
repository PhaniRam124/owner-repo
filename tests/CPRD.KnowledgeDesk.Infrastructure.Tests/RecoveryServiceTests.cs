using CPRD.KnowledgeDesk.Core.Models;
using CPRD.KnowledgeDesk.Core.Services;
using CPRD.KnowledgeDesk.Infrastructure.Data;
using CPRD.KnowledgeDesk.Infrastructure.Repositories;
using CPRD.KnowledgeDesk.Infrastructure.Services;
using Xunit;

namespace CPRD.KnowledgeDesk.Infrastructure.Tests;

public sealed class RecoveryServiceTests
{
    [Fact]
    public async Task Newer_recovery_draft_is_replayed_into_database_and_removed()
    {
        using var temp = new TempDirectory();
        var paths = new TestAppPaths(temp.Path);
        var db = new KnowledgeDb(paths);
        await db.InitializeAsync(default);

        var search = new SearchRepository(db);
        var notes = new NoteService(new NoteRepository(db), search, db);
        var folderId = await GetFolderIdAsync(db, "Office");
        var note = await notes.CreateAsync(
            new NewNoteRequest("Before crash", string.Empty, "old body", folderId, "Standard Note", "{}", Array.Empty<string>()),
            default);

        var recovery = new RecoveryService(db, search, paths);
        await recovery.SaveDraftAsync(
            new RecoveryDraft(
                note.Id,
                "Recovered title",
                string.Empty,
                "recovered body",
                "{}",
                DateTimeOffset.UtcNow.AddMinutes(1)),
            default);

        var recoveredCount = await recovery.RecoverPendingAsync(default);

        var restored = await notes.GetAsync(note.Id, default);
        Assert.Equal(1, recoveredCount);
        Assert.NotNull(restored);
        Assert.Equal("Recovered title", restored!.Title);
        Assert.Equal("recovered body", restored.PlainText);
        Assert.Empty(await recovery.ListPendingAsync(default));
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
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "CPRDKnowledgeDeskRecoveryTests", Guid.NewGuid().ToString("N"));
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
