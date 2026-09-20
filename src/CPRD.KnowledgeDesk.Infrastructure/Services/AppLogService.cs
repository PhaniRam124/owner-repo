using System.Text;
using CPRD.KnowledgeDesk.Core.Services;

namespace CPRD.KnowledgeDesk.Infrastructure.Services;

public sealed class AppLogService : IAppLogService
{
    private readonly string _logsDirectory;
    private readonly SemaphoreSlim _gate = new(1, 1);

    public AppLogService(IAppPaths paths)
    {
        _logsDirectory = Path.Combine(paths.DataDirectory, "Logs");
    }

    public async Task LogAsync(
        AppLogLevel level,
        string message,
        Exception? exception = null,
        CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(_logsDirectory);
        var path = Path.Combine(
            _logsDirectory,
            $"knowledge-desk-{DateTime.UtcNow:yyyyMMdd}.log");

        var builder = new StringBuilder();
        builder.Append(DateTimeOffset.UtcNow.ToString("O"))
            .Append(" [")
            .Append(level.ToString().ToUpperInvariant())
            .Append("] ")
            .AppendLine(message);

        if (exception is not null)
            builder.AppendLine(exception.ToString());

        await _gate.WaitAsync(cancellationToken);
        try
        {
            await File.AppendAllTextAsync(
                path,
                builder.ToString(),
                Encoding.UTF8,
                cancellationToken);
        }
        finally
        {
            _gate.Release();
        }
    }
}
