using ASMILE.BO.Core.Entities;

namespace ASMILE.BO.Core.Interfaces.Repositories;

public interface IStockIssueRepository : IRepository<StockIssue>
{
    Task<StockIssue?> GetAggregateByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<StockIssue?> FindByNoAsync(string issueNo, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<StockIssue>> SearchAsync(DateTime? fromDate, DateTime? toDate, string keyword, int? warehouseId, CancellationToken cancellationToken = default);
}

