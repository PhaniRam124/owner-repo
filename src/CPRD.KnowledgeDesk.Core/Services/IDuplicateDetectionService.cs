using CPRD.KnowledgeDesk.Core.Models;

namespace CPRD.KnowledgeDesk.Core.Services;

public interface IDuplicateDetectionService
{
    Task<IReadOnlyList<DuplicateCandidate>> FindCandidatesAsync(
        DuplicateProbe probe,
        CancellationToken cancellationToken);
}
