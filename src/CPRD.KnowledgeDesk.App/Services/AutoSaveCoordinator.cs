namespace CPRD.KnowledgeDesk.App.Services;

public sealed class AutoSaveCoordinator : IDisposable
{
    private readonly object _gate = new();
    private readonly IDelayScheduler _delayScheduler;
    private readonly TimeSpan _delay;
    private CancellationTokenSource? _pendingCts;
    private Func<CancellationToken, Task>? _pendingSave;
    private bool _disposed;

    public AutoSaveCoordinator(IDelayScheduler delayScheduler, TimeSpan delay)
    {
        _delayScheduler = delayScheduler ?? throw new ArgumentNullException(nameof(delayScheduler));
        if (delay < TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(delay));
        _delay = delay;
    }

    public Task ScheduleAsync(Func<CancellationToken, Task> saveOperation)
    {
        ArgumentNullException.ThrowIfNull(saveOperation);

        CancellationTokenSource current;
        lock (_gate)
        {
            ThrowIfDisposed();

            _pendingCts?.Cancel();
            _pendingCts?.Dispose();

            _pendingSave = saveOperation;
            current = new CancellationTokenSource();
            _pendingCts = current;
        }

        _ = RunAfterDelayAsync(current);
        return Task.CompletedTask;
    }

    public Task FlushAsync(CancellationToken cancellationToken) =>
        FlushCoreAsync(null, cancellationToken);

    public Task FlushAsync(Func<CancellationToken, Task> fallbackSave, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(fallbackSave);
        return FlushCoreAsync(fallbackSave, cancellationToken);
    }

    private async Task FlushCoreAsync(
        Func<CancellationToken, Task>? fallbackSave,
        CancellationToken cancellationToken)
    {
        Func<CancellationToken, Task>? save;

        lock (_gate)
        {
            ThrowIfDisposed();

            save = _pendingSave ?? fallbackSave;
            _pendingSave = null;

            _pendingCts?.Cancel();
            _pendingCts?.Dispose();
            _pendingCts = null;
        }

        if (save is not null)
            await save(cancellationToken).ConfigureAwait(false);
    }

    private Task RunAfterDelayAsync(CancellationTokenSource scheduledCts)
    {
        var delayTask = _delayScheduler.DelayAsync(_delay, scheduledCts.Token);

        return delayTask.ContinueWith(
            completed =>
            {
                if (completed.IsCanceled || scheduledCts.IsCancellationRequested)
                    return Task.CompletedTask;

                if (completed.IsFaulted)
                    return Task.FromException(completed.Exception?.GetBaseException() ?? new InvalidOperationException("Autosave delay failed."));

                return ExecuteScheduledSaveAsync(scheduledCts);
            },
            CancellationToken.None,
            TaskContinuationOptions.ExecuteSynchronously,
            TaskScheduler.Default).Unwrap();
    }

    private async Task ExecuteScheduledSaveAsync(CancellationTokenSource scheduledCts)
    {
        Func<CancellationToken, Task>? save;
        lock (_gate)
        {
            if (_disposed || !ReferenceEquals(_pendingCts, scheduledCts))
                return;

            save = _pendingSave;
            _pendingSave = null;
            _pendingCts = null;
        }

        try
        {
            if (save is not null)
                await save(CancellationToken.None).ConfigureAwait(false);
        }
        finally
        {
            scheduledCts.Dispose();
        }
    }

    public void Dispose()
    {
        lock (_gate)
        {
            if (_disposed) return;
            _disposed = true;
            _pendingSave = null;
            _pendingCts?.Cancel();
            _pendingCts?.Dispose();
            _pendingCts = null;
        }
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }
}
