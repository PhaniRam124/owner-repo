using CPRD.KnowledgeDesk.Core.Models;
using CPRD.KnowledgeDesk.Core.Services;
using CPRD.KnowledgeDesk.Infrastructure.Repositories;

namespace CPRD.KnowledgeDesk.Infrastructure.Services;

public sealed class TagService : ITagService
{
    private readonly TagRepository _tags;

    public TagService(TagRepository tags) => _tags = tags;

    public Task<IReadOnlyList<Tag>> GetAllAsync(CancellationToken cancellationToken) =>
        _tags.GetAllAsync(cancellationToken);

    public Task<Tag> GetOrCreateAsync(string name, CancellationToken cancellationToken) =>
        _tags.GetOrCreateAsync(name, cancellationToken);

    public Task RenameAsync(Guid tagId, string newName, CancellationToken cancellationToken) =>
        _tags.RenameAsync(tagId, newName, cancellationToken);
}
