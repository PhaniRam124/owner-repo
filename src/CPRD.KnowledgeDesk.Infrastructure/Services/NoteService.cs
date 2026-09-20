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

    private static string NormalizeTitle(string? title) =>
        string.IsNullOrWhiteSpace(title) ? "Untitled Note" : title.Trim();
}
