using CPRD.KnowledgeDesk.Core.Models;

namespace CPRD.KnowledgeDesk.Core.Services;

public interface IRevisionService
{
    Task<NoteRevision> CreateRevisionAsync(Note note, CancellationToken cancellationToken);
    Task<IReadOnlyList<NoteRevision>> ListAsync(Guid noteId, CancellationToken cancellationToken);
    Task RestoreAsync(Guid noteId, long revisionId, CancellationToken cancellationToken);
}
