using CPRD.KnowledgeDesk.App.Services;
using Xunit;

namespace CPRD.KnowledgeDesk.App.Tests;

public sealed class AutoSaveCoordinatorTests
{
    [Fact]
    public async Task Rapid_edits_collapse_to_one_save()
    {
        var clock = new FakeDelayScheduler();
        var coordinator = new AutoSaveCoordinator(clock, TimeSpan.FromMilliseconds(750));
        var saves = 0;

        await coordinator.ScheduleAsync(_ =>
        {
            saves++;
            return Task.CompletedTask;
        });
        await coordinator.ScheduleAsync(_ =>
        {
            saves++;
            return Task.CompletedTask;
        });

        await clock.AdvanceAsync(TimeSpan.FromMilliseconds(750));

        Assert.Equal(1, saves);
    }

    [Fact]
    public async Task Flush_executes_latest_pending_save_immediately()
    {
        var clock = new FakeDelayScheduler();
        var coordinator = new AutoSaveCoordinator(clock, TimeSpan.FromSeconds(10));
        var saves = 0;

        await coordinator.ScheduleAsync(_ =>
        {
            saves++;
            return Task.CompletedTask;
        });

        await coordinator.FlushAsync(CancellationToken.None);

        Assert.Equal(1, saves);
    }

    private sealed class FakeDelayScheduler : IDelayScheduler
    {
        private readonly List<PendingDelay> _pending = new();

        public Task DelayAsync(TimeSpan delay, CancellationToken cancellationToken)
        {
            var tcs = new TaskCompletionSource();
            var pending = new PendingDelay(delay, tcs);
            _pending.Add(pending);
            cancellationToken.Register(() => tcs.TrySetCanceled(cancellationToken));
            return tcs.Task;
        }

        public async Task AdvanceAsync(TimeSpan elapsed)
        {
            var ready = _pending.Where(item => item.Delay <= elapsed).ToArray();
            foreach (var item in ready)
            {
                _pending.Remove(item);
                item.Completion.TrySetResult();
            }

            await Task.WhenAll(ready.Select(item => item.Completion.Task).Select(async task =>
            {
                try { await task; } catch (OperationCanceledException) { }
            }));
            await Task.Yield();
        }

        private sealed record PendingDelay(TimeSpan Delay, TaskCompletionSource Completion);
    }
}
