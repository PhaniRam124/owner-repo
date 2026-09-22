using CPRD.KnowledgeDesk.Core.Models;
using CPRD.KnowledgeDesk.Core.Services;
using CPRD.KnowledgeDesk.Infrastructure.Data;
using Microsoft.Data.Sqlite;

namespace CPRD.KnowledgeDesk.Infrastructure.Services;

public sealed class DashboardService : IDashboardService
{
    private readonly KnowledgeDb _db;

    public DashboardService(KnowledgeDb db) => _db = db;

    public async Task<DashboardSnapshot> GetSnapshotAsync(CancellationToken cancellationToken)
    {
        await using var connection = _db.OpenConnection();
        await connection.OpenAsync(cancellationToken);

        var todayUtc = new DateTimeOffset(DateTime.UtcNow.Date, TimeSpan.Zero);
        var daysSinceMonday = ((int)todayUtc.DayOfWeek + 6) % 7;
        var weekStartUtc = todayUtc.AddDays(-daysSinceMonday);

        var totalNotes = await ScalarIntAsync(connection,
            "SELECT COUNT(*) FROM notes WHERE deleted_at_utc IS NULL AND is_archived=0", cancellationToken);
        var folderCount = await ScalarIntAsync(connection,
            "SELECT COUNT(*) FROM folders WHERE is_archived=0", cancellationToken);
        var favoriteCount = await ScalarIntAsync(connection,
            "SELECT COUNT(*) FROM notes WHERE deleted_at_utc IS NULL AND is_archived=0 AND is_favorite=1", cancellationToken);
        var updatedToday = await ScalarIntAsync(connection,
            "SELECT COUNT(*) FROM notes WHERE deleted_at_utc IS NULL AND is_archived=0 AND modified_at_utc >= $today",
            cancellationToken,
            ("$today", todayUtc.ToString("O")));
        var createdToday = await ScalarIntAsync(connection,
            "SELECT COUNT(*) FROM notes WHERE deleted_at_utc IS NULL AND created_at_utc >= $today",
            cancellationToken,
            ("$today", todayUtc.ToString("O")));
        var createdThisWeek = await ScalarIntAsync(connection,
            "SELECT COUNT(*) FROM notes WHERE deleted_at_utc IS NULL AND created_at_utc >= $weekStart",
            cancellationToken,
            ("$weekStart", weekStartUtc.ToString("O")));
        var pinnedCount = await ScalarIntAsync(connection,
            "SELECT COUNT(*) FROM notes WHERE deleted_at_utc IS NULL AND is_archived=0 AND is_pinned=1",
            cancellationToken);
        var archivedCount = await ScalarIntAsync(connection,
            "SELECT COUNT(*) FROM notes WHERE deleted_at_utc IS NULL AND is_archived=1",
            cancellationToken);
        var trashCount = await ScalarIntAsync(connection,
            "SELECT COUNT(*) FROM notes WHERE deleted_at_utc IS NOT NULL",
            cancellationToken);
        var tagCount = await ScalarIntAsync(connection,
            "SELECT COUNT(*) FROM tags",
            cancellationToken);

        var recent = await LoadItemsAsync(connection,
            "n.deleted_at_utc IS NULL AND n.is_archived=0",
            "COALESCE(n.last_opened_at_utc,n.modified_at_utc) DESC",
            cancellationToken);
        var pinned = await LoadItemsAsync(connection,
            "n.deleted_at_utc IS NULL AND n.is_archived=0 AND n.is_pinned=1",
            "n.modified_at_utc DESC",
            cancellationToken);
        var imported = await LoadItemsAsync(connection,
            "n.deleted_at_utc IS NULL AND n.is_archived=0 AND n.source_type IS NOT NULL",
            "n.created_at_utc DESC",
            cancellationToken);
        var favorites = await LoadItemsAsync(connection,
            "n.deleted_at_utc IS NULL AND n.is_archived=0 AND n.is_favorite=1",
            "n.modified_at_utc DESC",
            cancellationToken);
        var recentlyModified = await LoadItemsAsync(connection,
            "n.deleted_at_utc IS NULL AND n.is_archived=0",
            "n.modified_at_utc DESC",
            cancellationToken);

        return new DashboardSnapshot(
            totalNotes,
            folderCount,
            favoriteCount,
            updatedToday,
            recent,
            pinned,
            imported,
            "Automatic local backup enabled")
        {
            CreatedToday = createdToday,
            CreatedThisWeek = createdThisWeek,
            PinnedCount = pinnedCount,
            ArchivedCount = archivedCount,
            TrashCount = trashCount,
            TagCount = tagCount,
            Favorites = favorites,
            RecentlyModified = recentlyModified
        };
    }

    private static async Task<int> ScalarIntAsync(
        SqliteConnection connection,
        string sql,
        CancellationToken cancellationToken,
        params (string Name, object Value)[] parameters)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        foreach (var parameter in parameters)
            command.Parameters.AddWithValue(parameter.Name, parameter.Value);
        return Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken));
    }

    private static async Task<IReadOnlyList<DashboardItem>> LoadItemsAsync(
        SqliteConnection connection,
        string where,
        string orderBy,
        CancellationToken cancellationToken)
    {
        var result = new List<DashboardItem>();
        await using var command = connection.CreateCommand();
        command.CommandText = $"""
            SELECT n.id,n.title,COALESCE(f.folder_path,''),n.modified_at_utc
              FROM notes n
              LEFT JOIN notes_fts f ON f.note_id=n.id
             WHERE {where}
             ORDER BY {orderBy}
             LIMIT 10
            """;
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            result.Add(new DashboardItem(
                Guid.Parse(reader.GetString(0)),
                reader.GetString(1),
                reader.GetString(2),
                DateTimeOffset.Parse(reader.GetString(3))));
        }
        return result;
    }
}
