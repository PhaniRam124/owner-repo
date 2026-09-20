using CPRD.KnowledgeDesk.Core.Models;

namespace CPRD.KnowledgeDesk.Core.Services;

public interface IFolderService
{
    Task<IReadOnlyList<Folder>> GetTreeAsync(CancellationToken cancellationToken);
    Task<Folder?> FindByPathAsync(string path, CancellationToken cancellationToken);
    Task<Folder> CreateAsync(Guid? parentId, string name, CancellationToken cancellationToken);
    Task RenameAsync(Guid folderId, string newName, CancellationToken cancellationToken);
    Task MoveAsync(Guid folderId, Guid? newParentId, CancellationToken cancellationToken);
    Task ArchiveAsync(Guid folderId, CancellationToken cancellationToken);
    Task RestoreAsync(Guid folderId, CancellationToken cancellationToken);
}
