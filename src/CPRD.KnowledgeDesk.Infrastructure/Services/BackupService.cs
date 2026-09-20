using CPRD.KnowledgeDesk.Core.Models;
using CPRD.KnowledgeDesk.Core.Services;
using CPRD.KnowledgeDesk.Infrastructure.Data;
using Microsoft.Data.Sqlite;

namespace CPRD.KnowledgeDesk.Infrastructure.Services;

public sealed class BackupService : IBackupService
{
    private const int RetentionCount = 30;
    private readonly KnowledgeDb _db;
    private readonly IAppPaths _paths;

    public BackupService(KnowledgeDb db, IAppPaths paths)
    {
        _db = db;
        _paths = paths;
    }

    public Task<BackupInfo> BackupNowAsync(CancellationToken cancellationToken) =>
        CreateBackupAsync(isAutomatic: false, cancellationToken);

    public async Task<BackupInfo?> BackupAutomaticallyIfDueAsync(CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(_paths.BackupsDirectory);
        var latestAutomatic = Directory.EnumerateFiles(_paths.BackupsDirectory, "Auto-*.db")
            .Select(path => new FileInfo(path))
            .OrderByDescending(file => file.LastWriteTimeUtc)
            .FirstOrDefault();

        if (latestAutomatic is not null &&
            DateTime.UtcNow - latestAutomatic.LastWriteTimeUtc < TimeSpan.FromHours(24))
            return null;

        return await CreateBackupAsync(isAutomatic: true, cancellationToken);
    }

    public Task<IReadOnlyList<BackupInfo>> ListAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Directory.CreateDirectory(_paths.BackupsDirectory);

        IReadOnlyList<BackupInfo> result = Directory.EnumerateFiles(_paths.BackupsDirectory, "*.db")
            .Select(path =>
            {
                var file = new FileInfo(path);
                return new BackupInfo(
                    file.FullName,
                    new DateTimeOffset(file.LastWriteTimeUtc, TimeSpan.Zero),
                    file.Length,
                    file.Name.StartsWith("Auto-", StringComparison.OrdinalIgnoreCase));
            })
            .OrderByDescending(item => item.CreatedAtUtc)
            .ToArray();

        return Task.FromResult(result);
    }

    public async Task RestoreAsync(string backupPath, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(backupPath);
        var fullBackupPath = Path.GetFullPath(backupPath);
        if (!File.Exists(fullBackupPath))
            throw new FileNotFoundException("The selected backup file does not exist.", fullBackupPath);

        await ValidateDatabaseAsync(fullBackupPath, cancellationToken);

        var safetyDirectory = Path.Combine(_paths.BackupsDirectory, "PreRestore");
        Directory.CreateDirectory(safetyDirectory);
        var safetyPath = Path.Combine(
            safetyDirectory,
            $"Before-Restore-{DateTime.UtcNow:yyyyMMdd-HHmmssfff}.db");

        await Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            SqliteConnection.ClearAllPools();

            if (File.Exists(_paths.DatabasePath))
                File.Copy(_paths.DatabasePath, safetyPath, overwrite: true);

            DeleteIfExists(_paths.DatabasePath + "-wal");
            DeleteIfExists(_paths.DatabasePath + "-shm");

            var tempPath = _paths.DatabasePath + ".restore";
            File.Copy(fullBackupPath, tempPath, overwrite: true);

            try
            {
                File.Move(tempPath, _paths.DatabasePath, overwrite: true);
            }
            catch
            {
                DeleteIfExists(tempPath);
                if (File.Exists(safetyPath))
                    File.Copy(safetyPath, _paths.DatabasePath, overwrite: true);
                throw;
            }

            SqliteConnection.ClearAllPools();
        }, cancellationToken);

        await ValidateDatabaseAsync(_paths.DatabasePath, cancellationToken);
    }

    private async Task<BackupInfo> CreateBackupAsync(bool isAutomatic, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(_paths.BackupsDirectory);
        var prefix = isAutomatic ? "Auto" : "Manual";
        var targetPath = Path.Combine(
            _paths.BackupsDirectory,
            $"{prefix}-{DateTime.UtcNow:yyyyMMdd-HHmmssfff}.db");

        await Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();

            using var source = _db.OpenConnection();
            source.Open();

            var destinationBuilder = new SqliteConnectionStringBuilder
            {
                DataSource = targetPath,
                Mode = SqliteOpenMode.ReadWriteCreate,
                Pooling = false
            };
            using var destination = new SqliteConnection(destinationBuilder.ToString());
            destination.Open();

            source.BackupDatabase(destination);
        }, cancellationToken);

        await ValidateDatabaseAsync(targetPath, cancellationToken);
        TrimOldBackups();

        var file = new FileInfo(targetPath);
        return new BackupInfo(
            file.FullName,
            new DateTimeOffset(file.LastWriteTimeUtc, TimeSpan.Zero),
            file.Length,
            isAutomatic);
    }

    private static async Task ValidateDatabaseAsync(string databasePath, CancellationToken cancellationToken)
    {
        var builder = new SqliteConnectionStringBuilder
        {
            DataSource = databasePath,
            Mode = SqliteOpenMode.ReadOnly,
            Pooling = false
        };

        await using var connection = new SqliteConnection(builder.ToString());
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA integrity_check";
        var result = Convert.ToString(await command.ExecuteScalarAsync(cancellationToken));
        if (!string.Equals(result, "ok", StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException($"Backup database integrity check failed: {result}");
    }

    private void TrimOldBackups()
    {
        var files = Directory.EnumerateFiles(_paths.BackupsDirectory, "*.db")
            .Select(path => new FileInfo(path))
            .OrderByDescending(file => file.LastWriteTimeUtc)
            .Skip(RetentionCount)
            .ToArray();

        foreach (var file in files)
        {
            try { file.Delete(); }
            catch { }
        }
    }

    private static void DeleteIfExists(string path)
    {
        if (File.Exists(path))
            File.Delete(path);
    }
}
