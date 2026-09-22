using CPRD.KnowledgeDesk.Core.Services;

namespace CPRD.KnowledgeDesk.App.Services;

public sealed record StartupMaintenanceResult(
    int RecoveredDrafts,
    bool BackupCreated);

public sealed class StartupMaintenanceService
{
    private readonly IRecoveryService _recovery;
    private readonly IBackupService _backups;
    private readonly IAppLogService? _log;

    public StartupMaintenanceService(
        IRecoveryService recovery,
        IBackupService backups)
        : this(recovery, backups, null)
    {
    }

    public StartupMaintenanceService(
        IRecoveryService recovery,
        IBackupService backups,
        IAppLogService? log)
    {
        _recovery = recovery;
        _backups = backups;
        _log = log;
    }

    public async Task<StartupMaintenanceResult> RunAsync(
        CancellationToken cancellationToken,
        bool recoverPendingDrafts = true)
    {
        var recovered = 0;
        var backupCreated = false;

        if (recoverPendingDrafts)
        {
            try
            {
                recovered = await _recovery.RecoverPendingAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                if (_log is not null)
                    await _log.LogAsync(AppLogLevel.Error, "Crash recovery failed during startup.", ex, cancellationToken);
            }
        }

        try
        {
            var backup = await _backups.BackupAutomaticallyIfDueAsync(cancellationToken);
            backupCreated = backup is not null;
        }
        catch (Exception ex)
        {
            if (_log is not null)
                await _log.LogAsync(AppLogLevel.Error, "Automatic startup backup failed.", ex, cancellationToken);
        }

        return new StartupMaintenanceResult(recovered, backupCreated);
    }
}
