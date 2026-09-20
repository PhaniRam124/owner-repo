using CPRD.KnowledgeDesk.Core.Services;

namespace CPRD.KnowledgeDesk.Infrastructure.Services;

public sealed class AppPaths : IAppPaths
{
    public AppPaths()
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var root = Path.Combine(localAppData, "CPRD Knowledge Desk");

        DataDirectory = Path.Combine(root, "Data");
        DatabasePath = Path.Combine(DataDirectory, "knowledge.db");
        AttachmentsDirectory = Path.Combine(root, "Attachments");
        MigrationSafetyDirectory = Path.Combine(root, "MigrationSafety");
        DocumentsDirectory = Path.Combine(userProfile, "Documents", "CPRD Knowledge Desk");
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
