using CPRD.KnowledgeDesk.App.Services;
using CPRD.KnowledgeDesk.Core.Models;
using CPRD.KnowledgeDesk.Core.Services;
using Xunit;

namespace CPRD.KnowledgeDesk.App.Tests;

public sealed class StartupMaintenanceTests
{
    [Fact]
    public async Task Startup_recovers_pending_drafts_before_automatic_backup()
    {
        var calls = new List<string>();
        var recovery = new FakeRecoveryService(calls);
        var backups = new FakeBackupService(calls);
        var sut = new StartupMaintenanceService(recovery, backups);

        var result = await sut.RunAsync(CancellationToken.None);

        Assert.Equal(new[] { "recover", "backup" }, calls);
        Assert.Equal(2, result.RecoveredDrafts);
        Assert.True(result.BackupCreated);
    }

    [Fact]
    public async Task Startup_can_skip_automatic_recovery_after_user_has_handled_prompt()
    {
        var calls = new List<string>();
        var recovery = new FakeRecoveryService(calls);
        var backups = new FakeBackupService(calls);
        var sut = new StartupMaintenanceService(recovery, backups);

        var result = await sut.RunAsync(
            CancellationToken.None,
            recoverPendingDrafts: false);

        Assert.Equal(new[] { "backup" }, calls);
        Assert.Equal(0, result.RecoveredDrafts);
    }

    private sealed class FakeRecoveryService : IRecoveryService
    {
        private readonly List<string> _calls;
        public FakeRecoveryService(List<string> calls) => _calls = calls;

        public Task SaveDraftAsync(RecoveryDraft draft, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task DeleteDraftAsync(Guid noteId, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task<IReadOnlyList<RecoveryDraft>> ListPendingAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<RecoveryDraft>>(Array.Empty<RecoveryDraft>());

        public Task<int> RecoverPendingAsync(CancellationToken cancellationToken)
        {
            _calls.Add("recover");
            return Task.FromResult(2);
        }
    }

    private sealed class FakeBackupService : IBackupService
    {
        private readonly List<string> _calls;
        public FakeBackupService(List<string> calls) => _calls = calls;

        public Task<BackupInfo> BackupNowAsync(CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<BackupInfo?> BackupAutomaticallyIfDueAsync(CancellationToken cancellationToken)
        {
            _calls.Add("backup");
            return Task.FromResult<BackupInfo?>(
                new BackupInfo("auto.db", DateTimeOffset.UtcNow, 100, true));
        }

        public Task<IReadOnlyList<BackupInfo>> ListAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<BackupInfo>>(Array.Empty<BackupInfo>());

        public Task RestoreAsync(string backupPath, CancellationToken cancellationToken) =>
            Task.CompletedTask;
    }
}
