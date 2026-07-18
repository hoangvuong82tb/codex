using ASMILE.BO.Core.Entities;
using ASMILE.BO.Core.Interfaces.Repositories;
using ASMILE.BO.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ASMILE.BO.Infrastructure.Repositories;

public sealed class StockReceiptRepository : EfRepository<StockReceipt>, IStockReceiptRepository
{
    public StockReceiptRepository(AppDbContext dbContext) : base(dbContext)
    {
    }

    public Task<StockReceipt?> GetAggregateByIdAsync(int id, CancellationToken cancellationToken = default)
        => DbContext.StockReceipts.Include(x => x.Details).FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<StockReceipt?> FindByNoAsync(string receiptNo, CancellationToken cancellationToken = default)
        => DbContext.StockReceipts.AsNoTracking().FirstOrDefaultAsync(x => x.ReceiptNo == receiptNo, cancellationToken);

    public async Task<IReadOnlyList<StockReceipt>> SearchAsync(DateTime? fromDate, DateTime? toDate, string keyword, int? warehouseId, CancellationToken cancellationToken = default)
    {
        var q = DbContext.StockReceipts.Include(x => x.Details).AsQueryable();
        if (fromDate.HasValue) q = q.Where(x => x.ReceiptDate.Date >= fromDate.Value.Date);
        if (toDate.HasValue) q = q.Where(x => x.ReceiptDate.Date <= toDate.Value.Date);
        if (warehouseId.HasValue && warehouseId.Value > 0) q = q.Where(x => x.WarehouseId == warehouseId.Value);

        keyword = keyword?.Trim() ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            q = q.Where(x => x.ReceiptNo.Contains(keyword)
                             || x.SupplierCode.Contains(keyword)
                             || x.SupplierName.Contains(keyword)
                             || x.ReceiverName.Contains(keyword));
        }

        return await q.OrderByDescending(x => x.ReceiptDate).ThenByDescending(x => x.Id).AsNoTracking().ToListAsync(cancellationToken);
    }
}

