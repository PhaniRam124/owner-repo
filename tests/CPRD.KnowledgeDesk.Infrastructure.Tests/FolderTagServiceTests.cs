using CPRD.KnowledgeDesk.Core.Services;
using CPRD.KnowledgeDesk.Infrastructure.Data;
using CPRD.KnowledgeDesk.Infrastructure.Repositories;
using CPRD.KnowledgeDesk.Infrastructure.Services;
using Xunit;

namespace CPRD.KnowledgeDesk.Infrastructure.Tests;

public sealed class FolderTagServiceTests
{
    [Fact]
    public async Task MoveAsync_rejects_moving_parent_under_its_descendant()
    {
        using var env = await TestEnvironment.CreateAsync();
        var folders = new FolderService(new FolderRepository(env.Db), new SearchRepository(env.Db), env.Db);
        var office = await folders.FindByPathAsync("Office", default);
        var parent = await folders.CreateAsync(office!.Id, "A", default);
        var child = await folders.CreateAsync(parent.Id, "B", default);

        await Assert.ThrowsAsync<InvalidOperationException>(() => folders.MoveAsync(parent.Id, child.Id, default));
    }

    [Fact]
    public async Task Tags_are_case_insensitive_and_reused()
    {
        using var env = await TestEnvironment.CreateAsync();
        var tags = new TagService(new TagRepository(env.Db));
        var a = await tags.GetOrCreateAsync("Payment", default);
        var b = await tags.GetOrCreateAsync(" payment ", default);
        Assert.Equal(a.Id, b.Id);
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
