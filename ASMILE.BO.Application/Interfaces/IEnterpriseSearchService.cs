using ASMILE.BO.Application.DTOs;

namespace ASMILE.BO.Application.Interfaces;

public interface IEnterpriseSearchService
{
    Task UpsertAsync(SearchIndexUpsertRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SearchIndexEntryDto>> SearchAsync(string query, int take = 30, bool includeArchived = false, CancellationToken cancellationToken = default);
    Task RecordRecentSearchAsync(string query, SearchIndexEntryDto? selected = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RecentSearchDto>> GetRecentSearchesAsync(int take = 10, CancellationToken cancellationToken = default);
}
