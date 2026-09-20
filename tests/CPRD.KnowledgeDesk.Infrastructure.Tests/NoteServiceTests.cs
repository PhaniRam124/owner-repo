using CPRD.KnowledgeDesk.Core.Services;
using CPRD.KnowledgeDesk.Infrastructure.Data;
using CPRD.KnowledgeDesk.Infrastructure.Repositories;
using CPRD.KnowledgeDesk.Infrastructure.Services;
using Xunit;

namespace CPRD.KnowledgeDesk.Infrastructure.Tests;

public sealed class NoteServiceTests
{
    [Fact]
    public async Task Create_then_update_persists_note_and_refreshes_fts()
    {
        using var temp = new TempDirectory();
        var paths = new TestAppPaths(temp.Path);
        var db = new KnowledgeDb(paths);
        await db.InitializeAsync(default);

        var folderId = await GetFolderIdAsync(db, "Office");
        var sut = new NoteService(new NoteRepository(db), new SearchRepository(db), db);

        var note = await sut.CreateAsync(
            new NewNoteRequest("HIM SIM Payment", "<FlowDocument />", "Payment reference 123", folderId, "Standard Note", "{}", Array.Empty<string>()),
            default);

        await sut.UpdateAsync(
            new UpdateNoteRequest(note.Id, "HIM SIM Payment", "<FlowDocument />", "Payment reference 456", folderId, "Standard Note", false, false, "{}", Array.Empty<string>()),
            default);

        var saved = await sut.GetAsync(note.Id, default);
        Assert.Equal("Payment reference 456", saved!.PlainText);

        await using var connection = db.OpenConnection();
        await connection.OpenAsync();
        var command = connection.CreateCommand();
        command.CommandText = "SELECT plain_text FROM notes_fts WHERE note_id=$id";
        command.Parameters.AddWithValue("$id", note.Id.ToString("D"));
        Assert.Equal("Payment reference 456", (string?)await command.ExecuteScalarAsync());
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
