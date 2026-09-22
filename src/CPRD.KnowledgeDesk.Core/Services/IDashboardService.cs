using CPRD.KnowledgeDesk.Core.Models;

namespace CPRD.KnowledgeDesk.Core.Services;

public interface IDashboardService
{
    Task<DashboardSnapshot> GetSnapshotAsync(CancellationToken cancellationToken);
}
