namespace CPRD.KnowledgeDesk.Core.Services;

public interface IAppPaths
{
    string DataDirectory { get; }
    string DatabasePath { get; }
    string AttachmentsDirectory { get; }
    string MigrationSafetyDirectory { get; }
    string DocumentsDirectory { get; }
    string BackupsDirectory { get; }
    string ExportsDirectory { get; }
}
