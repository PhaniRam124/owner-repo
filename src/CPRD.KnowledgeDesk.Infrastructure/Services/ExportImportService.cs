using System.Text.Json;
using CPRD.KnowledgeDesk.Core.Models;
using CPRD.KnowledgeDesk.Core.Services;
using CPRD.KnowledgeDesk.Infrastructure.Data;
using CPRD.KnowledgeDesk.Infrastructure.Repositories;
using Microsoft.Data.Sqlite;

namespace CPRD.KnowledgeDesk.Infrastructure.Services;

public sealed class ExportImportService : IExportImportService
{
    private const int CurrentFormatVersion = 1;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    private readonly KnowledgeDb _db;
    private readonly NoteRepository _notes;
    private readonly SearchRepository _search;

    public ExportImportService(
        KnowledgeDb db,
        NoteRepository notes,
        SearchRepository search)
    {
        _db = db;
        _notes = notes;
        _search = search;
    }

    public async Task<ExportResult> ExportAllAsync(
        string targetPath,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(targetPath);

        var export = new PortableExport(
            CurrentFormatVersion,
            DateTimeOffset.UtcNow,
            await LoadExportNotesAsync(cancellationToken));

        var fullPath = Path.GetFullPath(targetPath);
        Directory.CreateDirectory(
            Path.GetDirectoryName(fullPath)
            ?? throw new InvalidOperationException("Export path has no directory."));

        var tempPath = fullPath + ".tmp";
        await using (var stream = new FileStream(
            tempPath,
            FileMode.Create,
            FileAccess.Write,
            FileShare.None,
            64 * 1024,
            FileOptions.Asynchronous | FileOptions.WriteThrough))
        {
            await JsonSerializer.SerializeAsync(
                stream,
                export,
                JsonOptions,
                cancellationToken);
            await stream.FlushAsync(cancellationToken);
        }

        File.Move(tempPath, fullPath, overwrite: true);
        return new ExportResult(fullPath, export.Notes.Count);
    }

    public async Task<ImportResult> ImportAsync(
        string sourcePath,
        Guid fallbackFolderId,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourcePath);
        var fullPath = Path.GetFullPath(sourcePath);

        await using var stream = new FileStream(
            fullPath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            64 * 1024,
            FileOptions.Asynchronous);

        var export = await JsonSerializer.DeserializeAsync<PortableExport>(
            stream,
            JsonOptions,
            cancellationToken)
            ?? throw new InvalidDataException("The notes export is empty or invalid.");

        if (export.FormatVersion != CurrentFormatVersion)
            throw new InvalidDataException(
                $"Unsupported notes export version {export.FormatVersion}.");

        var imported = 0;
        var skipped = 0;

        foreach (var item in export.Notes)
        {
            cancellationToken.ThrowIfCancellationRequested();

            await using var connection = _db.OpenConnection();
            await connection.OpenAsync(cancellationToken);
            await using var transaction =
                (SqliteTransaction)await connection.BeginTransactionAsync(cancellationToken);

            if (await IsExactDuplicateAsync(
                connection,
                transaction,
                item.Title ?? string.Empty,
                item.PlainText ?? string.Empty,
                cancellationToken))
            {
                await transaction.RollbackAsync(cancellationToken);
                skipped++;
                continue;
            }

            var folderId = await ResolveFolderAsync(
                connection,
                transaction,
                item.FolderPath,
                fallbackFolderId,
                cancellationToken);

            var id = Guid.NewGuid();
            var note = new Note(
                id,
                string.IsNullOrWhiteSpace(item.Title) ? "Untitled Note" : item.Title.Trim(),
                item.ContentPackage ?? string.Empty,
                item.PlainText ?? string.Empty,
                folderId,
                string.IsNullOrWhiteSpace(item.NoteType) ? "Standard Note" : item.NoteType,
                item.IsFavorite,
                item.IsPinned,
                item.IsArchived,
                item.CreatedAtUtc == default ? DateTimeOffset.UtcNow : item.CreatedAtUtc,
                item.ModifiedAtUtc == default ? DateTimeOffset.UtcNow : item.ModifiedAtUtc,
                null,
                null,
                "PortableImport",
                fullPath,
                string.IsNullOrWhiteSpace(item.StructuredJson) ? "{}" : item.StructuredJson);

            await _notes.InsertAsync(connection, transaction, note, cancellationToken);
            await _notes.SyncTagsAsync(
                connection,
                transaction,
                note.Id,
                item.Tags ?? Array.Empty<string>(),
                cancellationToken);
            await _search.RefreshAsync(
                connection,
                transaction,
                note.Id,
                cancellationToken);

            await transaction.CommitAsync(cancellationToken);
            imported++;
        }

        return new ImportResult(imported, skipped);
    }

    private async Task<IReadOnlyList<PortableNote>> LoadExportNotesAsync(
        CancellationToken cancellationToken)
    {
        var result = new List<PortableNote>();

        await using var connection = _db.OpenConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT n.id,
                   n.title,
                   n.content_package,
                   n.plain_text,
                   n.note_type,
                   n.is_favorite,
                   n.is_pinned,
                   n.is_archived,
                   n.created_at_utc,
                   n.modified_at_utc,
                   n.structured_json,
                   COALESCE(fts.folder_path,''),
                   COALESCE((
                       SELECT group_concat(t.name, char(31))
                         FROM note_tags nt
                         JOIN tags t ON t.id=nt.tag_id
                        WHERE nt.note_id=n.id
                   ),'')
              FROM notes n
              LEFT JOIN notes_fts fts ON fts.note_id=n.id
             WHERE n.deleted_at_utc IS NULL
             ORDER BY n.created_at_utc,n.id
            """;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var tagsRaw = reader.GetString(12);
            var tags = tagsRaw.Length == 0
                ? Array.Empty<string>()
                : tagsRaw.Split((char)31, StringSplitOptions.RemoveEmptyEntries);

            result.Add(new PortableNote(
                reader.GetString(1),
                reader.GetString(2),
                reader.GetString(3),
                reader.GetString(4),
                reader.GetInt64(5) != 0,
                reader.GetInt64(6) != 0,
                reader.GetInt64(7) != 0,
                DateTimeOffset.Parse(reader.GetString(8)),
                DateTimeOffset.Parse(reader.GetString(9)),
                reader.GetString(10),
                reader.GetString(11),
                tags));
        }

        return result;
    }

    private static async Task<bool> IsExactDuplicateAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        string title,
        string plainText,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            SELECT EXISTS(
                SELECT 1
                  FROM notes
                 WHERE deleted_at_utc IS NULL
                   AND title=$title COLLATE NOCASE
                   AND plain_text=$plain
            )
            """;
        command.Parameters.AddWithValue("$title", title?.Trim() ?? string.Empty);
        command.Parameters.AddWithValue("$plain", plainText ?? string.Empty);
        return Convert.ToInt32(
            await command.ExecuteScalarAsync(cancellationToken)) != 0;
    }

    private static async Task<Guid> ResolveFolderAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        string? folderPath,
        Guid fallbackFolderId,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(folderPath))
        {
            await using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = """
                WITH RECURSIVE folder_paths(id,parent_id,name,path) AS (
                  SELECT id,parent_id,name,name
                    FROM folders
                   WHERE parent_id IS NULL
                  UNION ALL
                  SELECT f.id,f.parent_id,f.name,fp.path || ' > ' || f.name
                    FROM folders f
                    JOIN folder_paths fp ON f.parent_id=fp.id
                )
                SELECT id
                  FROM folder_paths
                 WHERE path=$path COLLATE NOCASE
                 LIMIT 1
                """;
            command.Parameters.AddWithValue("$path", folderPath.Trim());
            var value = await command.ExecuteScalarAsync(cancellationToken);
            if (value is string id)
                return Guid.Parse(id);
        }

        return fallbackFolderId;
    }

    private sealed record PortableExport(
        int FormatVersion,
        DateTimeOffset ExportedAtUtc,
        IReadOnlyList<PortableNote> Notes);

    private sealed record PortableNote(
        string Title,
        string ContentPackage,
        string PlainText,
        string NoteType,
        bool IsFavorite,
        bool IsPinned,
        bool IsArchived,
        DateTimeOffset CreatedAtUtc,
        DateTimeOffset ModifiedAtUtc,
        string StructuredJson,
        string FolderPath,
        IReadOnlyList<string> Tags);
}
