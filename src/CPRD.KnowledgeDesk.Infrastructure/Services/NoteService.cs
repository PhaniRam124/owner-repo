using CPRD.KnowledgeDesk.Core.Models;
using CPRD.KnowledgeDesk.Core.Services;
using CPRD.KnowledgeDesk.Infrastructure.Data;
using CPRD.KnowledgeDesk.Infrastructure.Repositories;
using Microsoft.Data.Sqlite;

namespace CPRD.KnowledgeDesk.Infrastructure.Services;

public sealed class NoteService : INoteService
{
    private readonly NoteRepository _notes;
    private readonly SearchRepository _search;
    private readonly KnowledgeDb _db;

    public NoteService(NoteRepository notes, SearchRepository search, KnowledgeDb db)
    {
        _notes = notes;
        _search = search;
        _db = db;
    }

    public async Task<Note> CreateAsync(NewNoteRequest request, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var note = new Note(
            Guid.NewGuid(),
            NormalizeTitle(request.Title),
            request.ContentPackage ?? string.Empty,
            request.PlainText ?? string.Empty,
            request.FolderId,
            string.IsNullOrWhiteSpace(request.NoteType) ? "Standard Note" : request.NoteType.Trim(),
            false,
            false,
            false,
            now,
            now,
            null,
            null,
            null,
            null,
            string.IsNullOrWhiteSpace(request.StructuredJson) ? "{}" : request.StructuredJson);

        await using var connection = _db.OpenConnection();
        await connection.OpenAsync(cancellationToken);
        await using var transaction = (SqliteTransaction)await connection.BeginTransactionAsync(cancellationToken);

        await _notes.InsertAsync(connection, transaction, note, cancellationToken);
        await _notes.SyncTagsAsync(connection, transaction, note.Id, request.Tags, cancellationToken);
        await _search.RefreshAsync(connection, transaction, note.Id, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return note;
    }

    public Task<Note?> GetAsync(Guid id, CancellationToken cancellationToken) =>
        _notes.GetAsync(id, cancellationToken);

    public async Task UpdateAsync(UpdateNoteRequest request, CancellationToken cancellationToken)
    {
        await using var connection = _db.OpenConnection();
        await connection.OpenAsync(cancellationToken);
        await using var transaction = (SqliteTransaction)await connection.BeginTransactionAsync(cancellationToken);

        await _notes.UpdateAsync(connection, transaction, request with { Title = NormalizeTitle(request.Title) }, DateTimeOffset.UtcNow, cancellationToken);
        await _notes.SyncTagsAsync(connection, transaction, request.Id, request.Tags, cancellationToken);
        await _search.RefreshAsync(connection, transaction, request.Id, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public Task<IReadOnlyList<Note>> ListByFolderAsync(Guid folderId, CancellationToken cancellationToken) =>
        _notes.ListByFolderAsync(folderId, cancellationToken);

    public Task MarkOpenedAsync(Guid id, DateTimeOffset openedAtUtc, CancellationToken cancellationToken) =>
        _notes.MarkOpenedAsync(id, openedAtUtc, cancellationToken);

    public Task<IReadOnlyList<Note>> ListTrashAsync(CancellationToken cancellationToken) =>
        _notes.ListTrashAsync(cancellationToken);

    public async Task ArchiveAsync(Guid id, CancellationToken cancellationToken)
    {
        await using var connection = _db.OpenConnection();
        await connection.OpenAsync(cancellationToken);
        await using var transaction = (SqliteTransaction)await connection.BeginTransactionAsync(cancellationToken);
        await _notes.SetArchivedAsync(connection, transaction, id, true, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task UnarchiveAsync(Guid id, CancellationToken cancellationToken)
    {
        await using var connection = _db.OpenConnection();
        await connection.OpenAsync(cancellationToken);
        await using var transaction = (SqliteTransaction)await connection.BeginTransactionAsync(cancellationToken);
        await _notes.SetArchivedAsync(connection, transaction, id, false, cancellationToken);
        await _search.RefreshAsync(connection, transaction, id, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task MoveToTrashAsync(Guid id, CancellationToken cancellationToken)
    {
        await using var connection = _db.OpenConnection();
        await connection.OpenAsync(cancellationToken);
        await using var transaction = (SqliteTransaction)await connection.BeginTransactionAsync(cancellationToken);
        await _notes.SetDeletedAtAsync(connection, transaction, id, DateTimeOffset.UtcNow, cancellationToken);
        await _search.RemoveAsync(connection, transaction, id, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task RestoreFromTrashAsync(Guid id, CancellationToken cancellationToken)
    {
        await using var connection = _db.OpenConnection();
        await connection.OpenAsync(cancellationToken);
        await using var transaction = (SqliteTransaction)await connection.BeginTransactionAsync(cancellationToken);
        await _notes.SetDeletedAtAsync(connection, transaction, id, null, cancellationToken);
        await _search.RefreshAsync(connection, transaction, id, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task DeletePermanentlyAsync(Guid id, CancellationToken cancellationToken)
    {
        await using var connection = _db.OpenConnection();
        await connection.OpenAsync(cancellationToken);
        await using var transaction = (SqliteTransaction)await connection.BeginTransactionAsync(cancellationToken);
        await _search.RemoveAsync(connection, transaction, id, cancellationToken);
        await _notes.DeleteAsync(connection, transaction, id, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task<int> PurgeTrashOlderThanAsync(DateTimeOffset cutoffUtc, CancellationToken cancellationToken)
    {
        var ids = await _notes.GetTrashIdsOlderThanAsync(cutoffUtc, cancellationToken);
        foreach (var id in ids)
            await DeletePermanentlyAsync(id, cancellationToken);
        return ids.Count;
    }

    private static string NormalizeTitle(string? title) =>
        string.IsNullOrWhiteSpace(title) ? "Untitled Note" : title.Trim();
}
