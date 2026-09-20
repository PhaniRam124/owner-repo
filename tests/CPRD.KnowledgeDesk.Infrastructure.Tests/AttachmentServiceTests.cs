using CPRD.KnowledgeDesk.Core.Services;
using CPRD.KnowledgeDesk.Infrastructure.Data;
using CPRD.KnowledgeDesk.Infrastructure.Repositories;
using CPRD.KnowledgeDesk.Infrastructure.Services;
using Xunit;

namespace CPRD.KnowledgeDesk.Infrastructure.Tests;

public sealed class AttachmentServiceTests
{
    [Fact]
    public async Task AddAsync_copies_file_verifies_hash_then_commits_metadata()
    {
        using var env = await TestEnvironment.CreateAsync();
        var source = Path.Combine(env.Root, "invoice.pdf");
        await File.WriteAllTextAsync(source, "sample-pdf-bytes");
        var note = await env.CreateNoteAsync("Invoice", "Attached");

        var attachment = await env.Attachments.AddAsync(note.Id, source, default);

        Assert.True(File.Exists(attachment.StoredPath));
        Assert.Equal(await File.ReadAllBytesAsync(source), await File.ReadAllBytesAsync(attachment.StoredPath));
        Assert.Equal(1, (await env.Attachments.ListAsync(note.Id, default)).Count);
    }

    private sealed class TestEnvironment : IDisposable
    {
        private TestEnvironment(
            string root,
            KnowledgeDb db,
            INoteService notes,
            IAttachmentService attachments)
        {
            Root = root;
            Db = db;
            Notes = notes;
            Attachments = attachments;
        }

        public string Root { get; }
        public KnowledgeDb Db { get; }
        public INoteService Notes { get; }
        public IAttachmentService Attachments { get; }

        public static async Task<TestEnvironment> CreateAsync()
        {
            var root = Path.Combine(Path.GetTempPath(), "CPRDKnowledgeDeskTests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            var paths = new TestAppPaths(root);
            var db = new KnowledgeDb(paths);
            await db.InitializeAsync(default);
            var notes = new NoteService(new NoteRepository(db), new SearchRepository(db), db);
            var attachments = new AttachmentService(new AttachmentRepository(db), new SearchRepository(db), db, paths);
            return new TestEnvironment(root, db, notes, attachments);
        }

        public async Task<CPRD.KnowledgeDesk.Core.Models.Note> CreateNoteAsync(string title, string body)
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
