using CPRD.KnowledgeDesk.Core.Models;
using CPRD.KnowledgeDesk.Core.Services;
using CPRD.KnowledgeDesk.Infrastructure.Data;
using CPRD.KnowledgeDesk.Infrastructure.Repositories;
using Microsoft.Data.Sqlite;

namespace CPRD.KnowledgeDesk.Infrastructure.Services;

public sealed class RevisionService : IRevisionService
{
    private readonly RevisionRepository _revisions;
    private readonly SearchRepository _search;
    private readonly KnowledgeDb _db;

    public RevisionService(
        RevisionRepository revisions,
        NoteRepository notes,
        SearchRepository search,
        KnowledgeDb db)
    {
        _revisions = revisions;
        _search = search;
        _db = db;
        _ = notes;
    }

    public async Task<NoteRevision> CreateRevisionAsync(Note note, CancellationToken cancellationToken)
    {
        await using var connection = _db.OpenConnection();
        await connection.OpenAsync(cancellationToken);
        await using var transaction = (SqliteTransaction)await connection.BeginTransactionAsync(cancellationToken);
        var revision = await _revisions.CreateAsync(connection, transaction, note, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return revision;
    }

    public Task<IReadOnlyList<NoteRevision>> ListAsync(Guid noteId, CancellationToken cancellationToken) =>
        _revisions.ListAsync(noteId, cancellationToken);

    public async Task RestoreAsync(Guid noteId, long revisionId, CancellationToken cancellationToken)
    {
        await using var connection = _db.OpenConnection();
        await connection.OpenAsync(cancellationToken);
        await using var transaction = (SqliteTransaction)await connection.BeginTransactionAsync(cancellationToken);

        var current = await _revisions.GetCurrentNoteAsync(connection, transaction, noteId, cancellationToken)
            ?? throw new KeyNotFoundException($"Note '{noteId}' was not found.");

        var selected = await _revisions.GetAsync(connection, transaction, noteId, revisionId, cancellationToken)
            ?? throw new KeyNotFoundException($"Revision '{revisionId}' was not found for note '{noteId}'.");

        await _revisions.CreateAsync(connection, transaction, current, cancellationToken);
        await _revisions.RestoreSnapshotAsync(
            connection,
            transaction,
            noteId,
            selected,
            DateTimeOffset.UtcNow,
            cancellationToken);
        await _search.RefreshAsync(connection, transaction, noteId, cancellationToken);

        await transaction.CommitAsync(cancellationToken);
    }
}
