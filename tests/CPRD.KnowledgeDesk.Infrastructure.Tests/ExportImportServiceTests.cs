using CPRD.KnowledgeDesk.Core.Services;
using CPRD.KnowledgeDesk.Infrastructure.Data;
using CPRD.KnowledgeDesk.Infrastructure.Repositories;
using CPRD.KnowledgeDesk.Infrastructure.Services;
using Xunit;

namespace CPRD.KnowledgeDesk.Infrastructure.Tests;

public sealed class ExportImportServiceTests
{
    [Fact]
    public async Task Export_then_import_restores_note_title_content_and_flags()
    {
        using var source = new TestEnvironment();
        await source.InitializeAsync();

        var officeId = await source.GetFolderIdAsync("Office");
        var created = await source.Notes.CreateAsync(
            new NewNoteRequest(
                "Freeman",
                string.Empty,
                "Booth shipping details",
                officeId,
                "Standard Note",
                "{}",
                new[] { "conference", "vendor" }),
            default);

        await source.Notes.UpdateAsync(
            new UpdateNoteRequest(
                created.Id,
                created.Title,
                created.ContentPackage,
                created.PlainText,
                created.FolderId,
                created.NoteType,
                true,
                true,
                created.StructuredJson,
                new[] { "conference", "vendor" }),
            default);

        var exportPath = Path.Combine(source.Root, "notes.cprdnotes");
        var exported = await source.ExportImport.ExportAllAsync(exportPath, default);

        using var target = new TestEnvironment();
        await target.InitializeAsync();
        var targetFolderId = await target.GetFolderIdAsync("Office");

        var imported = await target.ExportImport.ImportAsync(
            exportPath,
            targetFolderId,
            default);

        var restored = await target.Search.SearchAsync(
            new CPRD.KnowledgeDesk.Core.Models.SearchQuery(
                "Freeman",
                null,
                Array.Empty<Guid>(),
                null,
                null,
                null,
                true,
                false,
                false,
                false,
                20),
            default);

        Assert.Equal(1, exported.NoteCount);
        Assert.Equal(1, imported.ImportedCount);
        Assert.Single(restored);

        var note = await target.Notes.GetAsync(restored[0].NoteId, default);
        Assert.NotNull(note);
        Assert.Equal("Freeman", note!.Title);
        Assert.Equal("Booth shipping details", note.PlainText);
        Assert.True(note.IsFavorite);
        Assert.True(note.IsPinned);
    }

    private sealed class TestEnvironment : IDisposable
    {
        private readonly TestAppPaths _paths;

        public TestEnvironment()
        {
            Root = Path.Combine(
                Path.GetTempPath(),
                "CPRDKnowledgeDeskExportTests",
                Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Root);

            _paths = new TestAppPaths(Root);
            Db = new KnowledgeDb(_paths);
            var notesRepo = new NoteRepository(Db);
            var searchRepo = new SearchRepository(Db);
            Notes = new NoteService(notesRepo, searchRepo, Db);
            Search = new SearchService(Db);
            ExportImport = new ExportImportService(Db, notesRepo, searchRepo);
        }

        public string Root { get; }
        public KnowledgeDb Db { get; }
        public INoteService Notes { get; }
        public ISearchService Search { get; }
        public ExportImportService ExportImport { get; }

        public Task InitializeAsync() => Db.InitializeAsync(default);

        public async Task<Guid> GetFolderIdAsync(string name)
        {
            await using var connection = Db.OpenConnection();
            await connection.OpenAsync();
            var command = connection.CreateCommand();
            command.CommandText = "SELECT id FROM folders WHERE name=$name AND parent_id IS NULL";
            command.Parameters.AddWithValue("$name", name);
            return Guid.Parse((string)(await command.ExecuteScalarAsync())!);
        }

        public void Dispose()
        {
            try
            {
                Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
                if (Directory.Exists(Root))
                    Directory.Delete(Root, true);
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
