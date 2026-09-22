using System.Text.Json;
using CPRD.KnowledgeDesk.Core.Models;
using CPRD.KnowledgeDesk.Core.Services;
using CPRD.KnowledgeDesk.Infrastructure.Data;
using CPRD.KnowledgeDesk.Infrastructure.Repositories;
using Microsoft.Data.Sqlite;

namespace CPRD.KnowledgeDesk.Infrastructure.Services;

public sealed class RecoveryService : IRecoveryService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = false
    };

    private readonly KnowledgeDb _db;
    private readonly SearchRepository _search;
    private readonly string _recoveryDirectory;

    public RecoveryService(KnowledgeDb db, SearchRepository search, IAppPaths paths)
    {
        _db = db;
        _search = search;
        _recoveryDirectory = Path.Combine(paths.DataDirectory, "Recovery");
    }

    public async Task SaveDraftAsync(RecoveryDraft draft, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(_recoveryDirectory);
        var finalPath = DraftPath(draft.NoteId);
        var tempPath = finalPath + ".tmp";

        await using (var stream = new FileStream(
            tempPath,
            FileMode.Create,
            FileAccess.Write,
            FileShare.None,
            16 * 1024,
            FileOptions.Asynchronous | FileOptions.WriteThrough))
        {
            await JsonSerializer.SerializeAsync(stream, draft, JsonOptions, cancellationToken);
            await stream.FlushAsync(cancellationToken);
        }

        File.Move(tempPath, finalPath, overwrite: true);
    }

    public Task DeleteDraftAsync(Guid noteId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var path = DraftPath(noteId);
        if (File.Exists(path))
            File.Delete(path);
        return Task.CompletedTask;
    }

    public async Task<IReadOnlyList<RecoveryDraft>> ListPendingAsync(CancellationToken cancellationToken)
    {
        if (!Directory.Exists(_recoveryDirectory))
            return Array.Empty<RecoveryDraft>();

        var result = new List<RecoveryDraft>();
        foreach (var path in Directory.EnumerateFiles(_recoveryDirectory, "*.json"))
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                await using var stream = new FileStream(
                    path,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.Read,
                    16 * 1024,
                    FileOptions.Asynchronous);
                var draft = await JsonSerializer.DeserializeAsync<RecoveryDraft>(
                    stream,
                    JsonOptions,
                    cancellationToken);
                if (draft is not null)
                    result.Add(draft);
            }
            catch (JsonException)
            {
                TryQuarantine(path);
            }
            catch (IOException)
            {
                // Keep the draft for the next startup if another operation is using it.
            }
        }

        return result.OrderBy(item => item.SavedAtUtc).ToArray();
    }

    public async Task<int> RecoverPendingAsync(CancellationToken cancellationToken)
    {
        var drafts = await ListPendingAsync(cancellationToken);
        var recovered = 0;

        foreach (var draft in drafts)
        {
            cancellationToken.ThrowIfCancellationRequested();

            await using var connection = _db.OpenConnection();
            await connection.OpenAsync(cancellationToken);
            await using var transaction =
                (SqliteTransaction)await connection.BeginTransactionAsync(cancellationToken);

            var modifiedAt = await GetModifiedAtAsync(
                connection,
                transaction,
                draft.NoteId,
                cancellationToken);

            if (modifiedAt is not null && draft.SavedAtUtc > modifiedAt.Value)
            {
                await using var update = connection.CreateCommand();
                update.Transaction = transaction;
                update.CommandText = """
                    UPDATE notes
                       SET title=$title,
                           content_package=$content,
                           plain_text=$plain,
                           structured_json=$structured,
                           modified_at_utc=$modified
                     WHERE id=$id
                       AND deleted_at_utc IS NULL
                    """;
                update.Parameters.AddWithValue("$id", draft.NoteId.ToString("D"));
                update.Parameters.AddWithValue(
                    "$title",
                    string.IsNullOrWhiteSpace(draft.Title) ? "Untitled Note" : draft.Title.Trim());
                update.Parameters.AddWithValue("$content", draft.ContentPackage ?? string.Empty);
                update.Parameters.AddWithValue("$plain", draft.PlainText ?? string.Empty);
                update.Parameters.AddWithValue(
                    "$structured",
                    string.IsNullOrWhiteSpace(draft.StructuredJson) ? "{}" : draft.StructuredJson);
                update.Parameters.AddWithValue("$modified", draft.SavedAtUtc.ToString("O"));

                if (await update.ExecuteNonQueryAsync(cancellationToken) > 0)
                {
                    await _search.RefreshAsync(
                        connection,
                        transaction,
                        draft.NoteId,
                        cancellationToken);
                    recovered++;
                }
            }

            await transaction.CommitAsync(cancellationToken);
            await DeleteDraftAsync(draft.NoteId, cancellationToken);
        }

        return recovered;
    }

    private async Task<DateTimeOffset?> GetModifiedAtAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        Guid noteId,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            SELECT modified_at_utc
              FROM notes
             WHERE id=$id
               AND deleted_at_utc IS NULL
            """;
        command.Parameters.AddWithValue("$id", noteId.ToString("D"));
        var value = await command.ExecuteScalarAsync(cancellationToken);
        return value is null || value is DBNull
            ? null
            : DateTimeOffset.Parse(Convert.ToString(value)!);
    }

    private string DraftPath(Guid noteId) =>
        Path.Combine(_recoveryDirectory, noteId.ToString("D") + ".json");

    private static void TryQuarantine(string path)
    {
        try
        {
            File.Move(path, path + ".corrupt", overwrite: true);
        }
        catch
        {
        }
    }
}
