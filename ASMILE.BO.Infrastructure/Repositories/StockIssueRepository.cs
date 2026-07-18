using ASMILE.BO.Core.Entities;
using ASMILE.BO.Core.Interfaces.Repositories;
using ASMILE.BO.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ASMILE.BO.Infrastructure.Repositories;

public sealed class StockIssueRepository : EfRepository<StockIssue>, IStockIssueRepository
{
    public StockIssueRepository(AppDbContext dbContext) : base(dbContext)
    {
    }

    public Task<StockIssue?> GetAggregateByIdAsync(int id, CancellationToken cancellationToken = default)
        => DbContext.StockIssues.Include(x => x.Details).FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<StockIssue?> FindByNoAsync(string issueNo, CancellationToken cancellationToken = default)
        => DbContext.StockIssues.AsNoTracking().FirstOrDefaultAsync(x => x.IssueNo == issueNo, cancellationToken);

    public async Task<IReadOnlyList<StockIssue>> SearchAsync(DateTime? fromDate, DateTime? toDate, string keyword, int? warehouseId, CancellationToken cancellationToken = default)
    {
        var q = DbContext.StockIssues.Include(x => x.Details).AsQueryable();
        if (fromDate.HasValue) q = q.Where(x => x.IssueDate.Date >= fromDate.Value.Date);
        if (toDate.HasValue) q = q.Where(x => x.IssueDate.Date <= toDate.Value.Date);
        if (warehouseId.HasValue && warehouseId.Value > 0) q = q.Where(x => x.WarehouseId == warehouseId.Value);

        keyword = keyword?.Trim() ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            q = q.Where(x => x.IssueNo.Contains(keyword) || x.Description.Contains(keyword) || x.DepartmentCode.Contains(keyword));
        }

        return await q.OrderByDescending(x => x.IssueDate).ThenByDescending(x => x.Id).AsNoTracking().ToListAsync(cancellationToken);
    }
}

