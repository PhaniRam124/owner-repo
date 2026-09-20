using CPRD.KnowledgeDesk.Core.Models;

namespace CPRD.KnowledgeDesk.Core.Services;

public interface IAttachmentService
{
    Task<Attachment> AddAsync(Guid noteId, string sourcePath, CancellationToken cancellationToken);
    Task<IReadOnlyList<Attachment>> ListAsync(Guid noteId, CancellationToken cancellationToken);
    Task RemoveAsync(Guid attachmentId, CancellationToken cancellationToken);
    Task SaveAsAsync(Guid attachmentId, string destinationPath, CancellationToken cancellationToken);
}
