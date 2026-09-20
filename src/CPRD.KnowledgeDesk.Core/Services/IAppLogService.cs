namespace CPRD.KnowledgeDesk.Core.Services;

public enum AppLogLevel
{
    Info,
    Warning,
    Error
}

public interface IAppLogService
{
    Task LogAsync(
        AppLogLevel level,
        string message,
        Exception? exception = null,
        CancellationToken cancellationToken = default);
}
