using System.Text.RegularExpressions;
using CPRD.KnowledgeDesk.Core.Models;
using CPRD.KnowledgeDesk.Core.Services;
using CPRD.KnowledgeDesk.Infrastructure.Data;

namespace CPRD.KnowledgeDesk.Infrastructure.Services;

public sealed class SearchService : ISearchService
{
    private static readonly Regex TokenRegex = new(@"[\p{L}\p{N}]+", RegexOptions.Compiled | RegexOptions.CultureInvariant);
    private readonly KnowledgeDb _db;

    public SearchService(KnowledgeDb db) => _db = db;

    public async Task<IReadOnlyList<SearchHit>> SearchAsync(SearchQuery query, CancellationToken cancellationToken)
    {
        if (query.HasAttachments)
            return Array.Empty<SearchHit>();

        var tokens = TokenRegex.Matches(query.Text ?? string.Empty)
            .Select(match => match.Value)
            .Where(value => value.Length > 0)
            .Take(20)
            .ToArray();

        if (tokens.Length == 0)
            return await SearchWithoutTextAsync(query, cancellationToken);

        var matchExpression = string.Join(' ', tokens.Select(token => "\"" + token + "\"*"));
        var tagCondition = BuildTagCondition(query.TagIds.Count);
        var sql = $"""
            WITH RECURSIVE subtree(id) AS (
              SELECT CAST($folderId AS TEXT) WHERE $folderId IS NOT NULL
              UNION ALL
              SELECT f.id FROM folders f JOIN subtree s ON f.parent_id=s.id
            )
            SELECT n.id,
                   n.title,
                   snippet(notes_fts, 2, '<mark>', '</mark>', '...', 18) AS snippet,
                   notes_fts.folder_path,
                   n.note_type,
                   n.is_favorite,
                   n.is_pinned,
                   n.modified_at_utc,
                   bm25(notes_fts) AS rank,
                   n.is_archived
              FROM notes_fts
              JOIN notes n ON n.id=notes_fts.note_id
             WHERE notes_fts MATCH $match
               AND n.deleted_at_utc IS NULL
               AND ($includeArchived=1 OR n.is_archived=0)
               AND ($folderId IS NULL OR n.folder_id IN (SELECT id FROM subtree))
               AND ($noteType IS NULL OR n.note_type=$noteType)
               AND ($modifiedFrom IS NULL OR n.modified_at_utc >= $modifiedFrom)
               AND ($modifiedTo IS NULL OR n.modified_at_utc <= $modifiedTo)
               AND ($favoritesOnly=0 OR n.is_favorite=1)
               AND ($pinnedOnly=0 OR n.is_pinned=1)
               {tagCondition}
             ORDER BY rank, n.modified_at_utc DESC
             LIMIT $limit
            """;

        var result = new List<SearchHit>();
        await using var connection = _db.OpenConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        BindCommon(command, query);
        command.Parameters.AddWithValue("$match", matchExpression);
        BindTags(command, query.TagIds);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            result.Add(new SearchHit(
                Guid.Parse(reader.GetString(0)),
                reader.GetString(1),
                reader.GetString(2),
                reader.GetString(3),
                reader.GetString(4),
                reader.GetInt64(5) != 0,
                reader.GetInt64(6) != 0,
                DateTimeOffset.Parse(reader.GetString(7)),
                reader.GetDouble(8),
                reader.GetInt64(9) != 0));
        }

        if ((query.Text ?? string.Empty).Any(char.IsDigit))
        {
            foreach (var dateHit in await SearchDateTextAsync(query, cancellationToken))
            {
                if (result.All(existing => existing.NoteId != dateHit.NoteId))
                    result.Add(dateHit);
            }
        }

        return result
            .OrderBy(hit => hit.Rank)
            .ThenByDescending(hit => hit.ModifiedAtUtc)
            .Take(Math.Clamp(query.Limit, 1, 200))
            .ToArray();
    }

    private async Task<IReadOnlyList<SearchHit>> SearchDateTextAsync(
        SearchQuery query,
        CancellationToken cancellationToken)
    {
        var dateText = (query.Text ?? string.Empty).Trim();
        if (dateText.Length == 0)
            return Array.Empty<SearchHit>();

        var tagCondition = BuildTagCondition(query.TagIds.Count);
        var sql = $"""
            WITH RECURSIVE subtree(id) AS (
              SELECT CAST($folderId AS TEXT) WHERE $folderId IS NOT NULL
              UNION ALL
              SELECT f.id FROM folders f JOIN subtree s ON f.parent_id=s.id
            )
            SELECT n.id,
                   n.title,
                   'Created ' || n.created_at_utc || ' • Modified ' || n.modified_at_utc,
                   COALESCE(fts.folder_path,''),
                   n.note_type,
                   n.is_favorite,
                   n.is_pinned,
                   n.modified_at_utc,
                   1000.0,
                   n.is_archived
              FROM notes n
              LEFT JOIN notes_fts fts ON fts.note_id=n.id
             WHERE n.deleted_at_utc IS NULL
               AND ($includeArchived=1 OR n.is_archived=0)
               AND ($folderId IS NULL OR n.folder_id IN (SELECT id FROM subtree))
               AND ($noteType IS NULL OR n.note_type=$noteType)
               AND ($modifiedFrom IS NULL OR n.modified_at_utc >= $modifiedFrom)
               AND ($modifiedTo IS NULL OR n.modified_at_utc <= $modifiedTo)
               AND ($favoritesOnly=0 OR n.is_favorite=1)
               AND ($pinnedOnly=0 OR n.is_pinned=1)
               AND (n.created_at_utc LIKE $dateText OR n.modified_at_utc LIKE $dateText)
               {tagCondition}
             ORDER BY n.modified_at_utc DESC
             LIMIT $limit
            """;

        var result = new List<SearchHit>();
        await using var connection = _db.OpenConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        BindCommon(command, query);
        command.Parameters.AddWithValue("$dateText", "%" + dateText + "%");
        BindTags(command, query.TagIds);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            result.Add(new SearchHit(
                Guid.Parse(reader.GetString(0)),
                reader.GetString(1),
                reader.GetString(2),
                reader.GetString(3),
                reader.GetString(4),
                reader.GetInt64(5) != 0,
                reader.GetInt64(6) != 0,
                DateTimeOffset.Parse(reader.GetString(7)),
                reader.GetDouble(8),
                reader.GetInt64(9) != 0));
        }

        return result;
    }
    private async Task<IReadOnlyList<SearchHit>> SearchWithoutTextAsync(SearchQuery query, CancellationToken cancellationToken)
    {
        var tagCondition = BuildTagCondition(query.TagIds.Count);
        var sql = $"""
            WITH RECURSIVE subtree(id) AS (
              SELECT CAST($folderId AS TEXT) WHERE $folderId IS NOT NULL
              UNION ALL
              SELECT f.id FROM folders f JOIN subtree s ON f.parent_id=s.id
            )
            SELECT n.id,n.title,
                   CASE WHEN length(n.plain_text)>180 THEN substr(n.plain_text,1,180) || '...' ELSE n.plain_text END,
                   COALESCE(f.folder_path,''),
                   n.note_type,n.is_favorite,n.is_pinned,n.modified_at_utc,0.0,n.is_archived
              FROM notes n
              LEFT JOIN notes_fts f ON f.note_id=n.id
             WHERE n.deleted_at_utc IS NULL
               AND ($includeArchived=1 OR n.is_archived=0)
               AND ($folderId IS NULL OR n.folder_id IN (SELECT id FROM subtree))
               AND ($noteType IS NULL OR n.note_type=$noteType)
               AND ($modifiedFrom IS NULL OR n.modified_at_utc >= $modifiedFrom)
               AND ($modifiedTo IS NULL OR n.modified_at_utc <= $modifiedTo)
               AND ($favoritesOnly=0 OR n.is_favorite=1)
               AND ($pinnedOnly=0 OR n.is_pinned=1)
               {tagCondition}
             ORDER BY n.modified_at_utc DESC
             LIMIT $limit
            """;

        var result = new List<SearchHit>();
        await using var connection = _db.OpenConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        BindCommon(command, query);
        BindTags(command, query.TagIds);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            result.Add(new SearchHit(
                Guid.Parse(reader.GetString(0)),
                reader.GetString(1),
                reader.GetString(2),
                reader.GetString(3),
                reader.GetString(4),
                reader.GetInt64(5) != 0,
                reader.GetInt64(6) != 0,
                DateTimeOffset.Parse(reader.GetString(7)),
                reader.GetDouble(8),
                reader.GetInt64(9) != 0));
        }
        return result;
    }

    private static string BuildTagCondition(int count)
    {
        if (count == 0) return string.Empty;
        var parameters = string.Join(',', Enumerable.Range(0, count).Select(index => $"$tag{index}"));
        return $"""
            AND n.id IN (
              SELECT nt.note_id
                FROM note_tags nt
               WHERE nt.tag_id IN ({parameters})
               GROUP BY nt.note_id
              HAVING COUNT(DISTINCT nt.tag_id)={count}
            )
            """;
    }

    private static void BindTags(Microsoft.Data.Sqlite.SqliteCommand command, IReadOnlyList<Guid> tagIds)
    {
        for (var index = 0; index < tagIds.Count; index++)
            command.Parameters.AddWithValue($"$tag{index}", tagIds[index].ToString("D"));
    }

    private static void BindCommon(Microsoft.Data.Sqlite.SqliteCommand command, SearchQuery query)
    {
        command.Parameters.AddWithValue("$folderId", query.FolderId?.ToString("D") ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("$includeArchived", query.IncludeArchived ? 1 : 0);
        command.Parameters.AddWithValue("$noteType", string.IsNullOrWhiteSpace(query.NoteType) ? DBNull.Value : query.NoteType.Trim());
        command.Parameters.AddWithValue("$modifiedFrom", query.ModifiedFromUtc?.ToString("O") ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("$modifiedTo", query.ModifiedToUtc?.ToString("O") ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("$favoritesOnly", query.FavoritesOnly ? 1 : 0);
        command.Parameters.AddWithValue("$pinnedOnly", query.PinnedOnly ? 1 : 0);
        command.Parameters.AddWithValue("$limit", Math.Clamp(query.Limit, 1, 200));
    }
}
