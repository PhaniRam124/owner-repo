using CPRD.KnowledgeDesk.Core.Services;
using Microsoft.Data.Sqlite;

namespace CPRD.KnowledgeDesk.Infrastructure.Data;

public sealed class KnowledgeDb
{
    private static readonly (int Version, string ResourceName)[] Migrations =
    {
        (2, "CPRD.KnowledgeDesk.Infrastructure.Data.Migrations.V002_Safety.sql")
    };

    private readonly IAppPaths _paths;

    public KnowledgeDb(IAppPaths paths) => _paths = paths;

    public SqliteConnection OpenConnection()
    {
        var builder = new SqliteConnectionStringBuilder
        {
            DataSource = _paths.DatabasePath,
            Mode = SqliteOpenMode.ReadWriteCreate,
            Cache = SqliteCacheMode.Shared,
            ForeignKeys = true
        };
        return new SqliteConnection(builder.ToString());
    }

    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(_paths.DataDirectory);
        Directory.CreateDirectory(_paths.AttachmentsDirectory);
        Directory.CreateDirectory(_paths.MigrationSafetyDirectory);
        Directory.CreateDirectory(_paths.BackupsDirectory);
        Directory.CreateDirectory(_paths.ExportsDirectory);

        await using var connection = OpenConnection();
        await connection.OpenAsync(cancellationToken);
        await using var transaction = (SqliteTransaction)await connection.BeginTransactionAsync(cancellationToken);

        await using (var schemaCommand = connection.CreateCommand())
        {
            schemaCommand.Transaction = transaction;
            schemaCommand.CommandText = await LoadResourceAsync(
                "CPRD.KnowledgeDesk.Infrastructure.Data.Schema.sql",
                cancellationToken);
            await schemaCommand.ExecuteNonQueryAsync(cancellationToken);
        }

        await ApplyMigrationsAsync(connection, transaction, cancellationToken);

        await using var countCommand = connection.CreateCommand();
        countCommand.Transaction = transaction;
        countCommand.CommandText = "SELECT COUNT(*) FROM folders";
        var count = Convert.ToInt32(await countCommand.ExecuteScalarAsync(cancellationToken));
        if (count == 0)
            await SeedFoldersAsync(connection, transaction, cancellationToken);

        await transaction.CommitAsync(cancellationToken);
    }

    private static async Task ApplyMigrationsAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        CancellationToken cancellationToken)
    {
        await using var versionCommand = connection.CreateCommand();
        versionCommand.Transaction = transaction;
        versionCommand.CommandText = "SELECT COALESCE(MAX(version),1) FROM schema_info";
        var currentVersion = Convert.ToInt32(await versionCommand.ExecuteScalarAsync(cancellationToken));

        foreach (var migration in Migrations.OrderBy(item => item.Version))
        {
            if (migration.Version <= currentVersion)
                continue;

            await using var migrationCommand = connection.CreateCommand();
            migrationCommand.Transaction = transaction;
            migrationCommand.CommandText = await LoadResourceAsync(migration.ResourceName, cancellationToken);
            await migrationCommand.ExecuteNonQueryAsync(cancellationToken);

            await using var updateVersion = connection.CreateCommand();
            updateVersion.Transaction = transaction;
            updateVersion.CommandText = "UPDATE schema_info SET version=$version";
            updateVersion.Parameters.AddWithValue("$version", migration.Version);
            await updateVersion.ExecuteNonQueryAsync(cancellationToken);

            currentVersion = migration.Version;
        }
    }

    private static async Task<string> LoadResourceAsync(string resourceName, CancellationToken cancellationToken)
    {
        await using var stream = typeof(KnowledgeDb).Assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Embedded database resource '{resourceName}' was not found.");
        using var reader = new StreamReader(stream);
        return await reader.ReadToEndAsync(cancellationToken);
    }

    private static async Task SeedFoldersAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var officeId = Guid.NewGuid();
        var itOperationsId = Guid.NewGuid();

        await InsertFolderAsync(connection, transaction, officeId, null, "Office", 0, now, cancellationToken);
        await InsertFolderAsync(connection, transaction, Guid.NewGuid(), null, "Projects", 1, now, cancellationToken);
        await InsertFolderAsync(connection, transaction, Guid.NewGuid(), null, "Personal", 2, now, cancellationToken);
        await InsertFolderAsync(connection, transaction, Guid.NewGuid(), null, "Reference", 3, now, cancellationToken);
        await InsertFolderAsync(connection, transaction, Guid.NewGuid(), null, "Imported Notes", 4, now, cancellationToken);

        await InsertFolderAsync(connection, transaction, itOperationsId, officeId, "IT Operations", 0, now, cancellationToken);
        var officeChildren = new[] { "Payments", "Vendors", "Accounts & Finance", "Conferences", "Travel", "Renewals", "General" };
        for (var index = 0; index < officeChildren.Length; index++)
            await InsertFolderAsync(connection, transaction, Guid.NewGuid(), officeId, officeChildren[index], index + 1, now, cancellationToken);

        var itChildren = new[] { "Firewall", "Internet / ISP", "Microsoft 365", "Cloud", "VDI", "Assets" };
        for (var index = 0; index < itChildren.Length; index++)
            await InsertFolderAsync(connection, transaction, Guid.NewGuid(), itOperationsId, itChildren[index], index, now, cancellationToken);
    }

    private static async Task InsertFolderAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        Guid id,
        Guid? parentId,
        string name,
        int sortOrder,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            INSERT INTO folders(id, parent_id, name, sort_order, is_archived, created_at_utc, modified_at_utc)
            VALUES($id, $parentId, $name, $sortOrder, 0, $createdAt, $modifiedAt)
            """;
        command.Parameters.AddWithValue("$id", id.ToString("D"));
        command.Parameters.AddWithValue("$parentId", parentId?.ToString("D") ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("$name", name);
        command.Parameters.AddWithValue("$sortOrder", sortOrder);
        command.Parameters.AddWithValue("$createdAt", now.ToString("O"));
        command.Parameters.AddWithValue("$modifiedAt", now.ToString("O"));
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
