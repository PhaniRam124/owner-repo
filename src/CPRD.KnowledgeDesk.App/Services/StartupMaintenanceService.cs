using CPRD.KnowledgeDesk.Core.Services;

namespace CPRD.KnowledgeDesk.App.Services;

public sealed record StartupMaintenanceResult(
    int RecoveredDrafts,
    bool BackupCreated);

public sealed class StartupMaintenanceService
{
    private readonly IRecoveryService _recovery;
    private readonly IBackupService _backups;

    public StartupMaintenanceService(
        IRecoveryService recovery,
        IBackupService backups)
    {
        _recovery = recovery;
        _backups = backups;
    }

    public async Task<StartupMaintenanceResult> RunAsync(
        CancellationToken cancellationToken)
    {
        var recovered = await _recovery.RecoverPendingAsync(cancellationToken);
        var backup = await _backups.BackupAutomaticallyIfDueAsync(cancellationToken);

        return new StartupMaintenanceResult(
            recovered,
            backup is not null);
    }
}
