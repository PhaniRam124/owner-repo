using CPRD.KnowledgeDesk.Core.Services;
using CPRD.KnowledgeDesk.Infrastructure.Services;
using Xunit;

namespace CPRD.KnowledgeDesk.Infrastructure.Tests;

public sealed class AppLogServiceTests
{
    [Fact]
    public async Task LogAsync_appends_timestamp_level_message_and_exception()
    {
        using var temp = new TempDirectory();
        var paths = new TestAppPaths(temp.Path);
        var logs = new AppLogService(paths);

        await logs.LogAsync(
            AppLogLevel.Error,
            "Save failed",
            new InvalidOperationException("database locked"),
            CancellationToken.None);

        var files = Directory.GetFiles(
            Path.Combine(paths.DataDirectory, "Logs"),
            "knowledge-desk-*.log");

        Assert.Single(files);
        var text = await File.ReadAllTextAsync(files[0]);
        Assert.Contains("ERROR", text);
        Assert.Contains("Save failed", text);
        Assert.Contains("database locked", text);
        Assert.Contains("InvalidOperationException", text);
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
            Path = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
                "CPRDKnowledgeDeskLogTests",
                Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path);
        }

        public string Path { get; }

        public void Dispose()
        {
            try
            {
                if (Directory.Exists(Path))
                    Directory.Delete(Path, true);
            }
            catch
            {
            }
        }
    }
}
