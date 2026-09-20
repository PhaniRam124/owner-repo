using CPRD.KnowledgeDesk.Core.Models;
using CPRD.KnowledgeDesk.Core.Services;
using CPRD.KnowledgeDesk.Infrastructure.Data;
using CPRD.KnowledgeDesk.Infrastructure.Repositories;
using Microsoft.Data.Sqlite;

namespace CPRD.KnowledgeDesk.Infrastructure.Services;

public sealed class FolderService : IFolderService
{
    private readonly FolderRepository _folders;
    private readonly SearchRepository _search;
    private readonly KnowledgeDb _db;

    public FolderService(FolderRepository folders, SearchRepository search, KnowledgeDb db)
    {
        _folders = folders;
        _search = search;
        _db = db;
    }

    public Task<IReadOnlyList<Folder>> GetTreeAsync(CancellationToken cancellationToken) =>
        _folders.GetAllAsync(cancellationToken);

    public Task<Folder?> FindByPathAsync(string path, CancellationToken cancellationToken) =>
        _folders.FindByPathAsync(path, cancellationToken);

    public Task<Folder> CreateAsync(Guid? parentId, string name, CancellationToken cancellationToken) =>
        _folders.CreateAsync(parentId, name, cancellationToken);

    public async Task RenameAsync(Guid folderId, string newName, CancellationToken cancellationToken)
    {
        await using var connection = _db.OpenConnection();
        await connection.OpenAsync(cancellationToken);
        await using var transaction = (SqliteTransaction)await connection.BeginTransactionAsync(cancellationToken);
        await _folders.RenameAsync(connection, transaction, folderId, newName, cancellationToken);
        await _search.RefreshFolderSubtreeAsync(connection, transaction, folderId, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task MoveAsync(Guid folderId, Guid? newParentId, CancellationToken cancellationToken)
    {
        if (newParentId == folderId)
            throw new InvalidOperationException("A folder cannot be moved under itself.");

        if (newParentId is not null)
        {
            var descendants = await _folders.GetDescendantIdsAsync(folderId, cancellationToken);
            if (descendants.Contains(newParentId.Value))
                throw new InvalidOperationException("A folder cannot be moved under one of its descendants.");
        }

        await using var connection = _db.OpenConnection();
        await connection.OpenAsync(cancellationToken);
        await using var transaction = (SqliteTransaction)await connection.BeginTransactionAsync(cancellationToken);
        await _folders.MoveAsync(connection, transaction, folderId, newParentId, cancellationToken);
        await _search.RefreshFolderSubtreeAsync(connection, transaction, folderId, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public Task ArchiveAsync(Guid folderId, CancellationToken cancellationToken) =>
        _folders.SetArchivedAsync(folderId, true, cancellationToken);

    public Task RestoreAsync(Guid folderId, CancellationToken cancellationToken) =>
        _folders.SetArchivedAsync(folderId, false, cancellationToken);
}
