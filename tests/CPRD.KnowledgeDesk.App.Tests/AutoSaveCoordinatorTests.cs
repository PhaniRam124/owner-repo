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

    [Fact]
    public async Task Flush_does_not_overlap_an_active_autosave()
    {
        var clock = new FakeDelayScheduler();
        using var coordinator = new AutoSaveCoordinator(clock, TimeSpan.FromMilliseconds(10));

        var firstStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseFirst = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var concurrent = 0;
        var maxConcurrent = 0;
        var calls = 0;

        async Task SaveAsync(CancellationToken _)
        {
            var call = Interlocked.Increment(ref calls);
            var nowConcurrent = Interlocked.Increment(ref concurrent);
            UpdateMax(ref maxConcurrent, nowConcurrent);

            try
            {
                if (call == 1)
                {
                    firstStarted.TrySetResult();
                    await releaseFirst.Task;
                }
            }
            finally
            {
                Interlocked.Decrement(ref concurrent);
            }
        }

        await coordinator.ScheduleAsync(SaveAsync);
        await clock.AdvanceAsync(TimeSpan.FromMilliseconds(10));
        await firstStarted.Task;

        var flushTask = coordinator.FlushAsync(SaveAsync, CancellationToken.None);
        await Task.Yield();

        Assert.False(flushTask.IsCompleted);

        releaseFirst.TrySetResult();
        await flushTask;

        Assert.Equal(1, maxConcurrent);
    }

    private static void UpdateMax(ref int target, int candidate)
    {
        while (true)
        {
            var snapshot = Volatile.Read(ref target);
            if (candidate <= snapshot) return;
            if (Interlocked.CompareExchange(ref target, candidate, snapshot) == snapshot) return;
        }
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
