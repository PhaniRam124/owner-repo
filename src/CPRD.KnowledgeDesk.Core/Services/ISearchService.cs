using CPRD.KnowledgeDesk.Core.Models;

namespace CPRD.KnowledgeDesk.Core.Services;

public interface ISearchService
{
    Task<IReadOnlyList<SearchHit>> SearchAsync(SearchQuery query, CancellationToken cancellationToken);
}
