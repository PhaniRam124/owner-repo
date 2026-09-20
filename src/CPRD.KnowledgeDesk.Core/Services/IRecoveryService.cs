using CPRD.KnowledgeDesk.Core.Models;

namespace CPRD.KnowledgeDesk.Core.Services;

public interface IRecoveryService
{
    Task SaveDraftAsync(RecoveryDraft draft, CancellationToken cancellationToken);
    Task DeleteDraftAsync(Guid noteId, CancellationToken cancellationToken);
    Task<IReadOnlyList<RecoveryDraft>> ListPendingAsync(CancellationToken cancellationToken);
    Task<int> RecoverPendingAsync(CancellationToken cancellationToken);
}
