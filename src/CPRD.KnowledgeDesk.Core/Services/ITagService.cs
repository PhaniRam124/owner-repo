using CPRD.KnowledgeDesk.Core.Models;

namespace CPRD.KnowledgeDesk.Core.Services;

public interface ITagService
{
    Task<IReadOnlyList<Tag>> GetAllAsync(CancellationToken cancellationToken);
    Task<Tag> GetOrCreateAsync(string name, CancellationToken cancellationToken);
    Task RenameAsync(Guid tagId, string newName, CancellationToken cancellationToken);
}
