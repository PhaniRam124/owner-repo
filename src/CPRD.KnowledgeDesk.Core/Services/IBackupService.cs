using CPRD.KnowledgeDesk.Core.Models;

namespace CPRD.KnowledgeDesk.Core.Services;

public interface IBackupService
{
    Task<BackupInfo> BackupNowAsync(CancellationToken cancellationToken);
    Task<BackupInfo?> BackupAutomaticallyIfDueAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<BackupInfo>> ListAsync(CancellationToken cancellationToken);
    Task RestoreAsync(string backupPath, CancellationToken cancellationToken);
}
