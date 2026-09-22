namespace CPRD.KnowledgeDesk.App.Services;

public interface IDelayScheduler
{
    Task DelayAsync(TimeSpan delay, CancellationToken cancellationToken);
}

public sealed class SystemDelayScheduler : IDelayScheduler
{
    public Task DelayAsync(TimeSpan delay, CancellationToken cancellationToken) =>
        Task.Delay(delay, cancellationToken);
}
