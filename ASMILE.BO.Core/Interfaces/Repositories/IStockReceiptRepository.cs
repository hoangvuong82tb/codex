using ASMILE.BO.Core.Entities;

namespace ASMILE.BO.Core.Interfaces.Repositories;

public interface IStockReceiptRepository : IRepository<StockReceipt>
{
    Task<StockReceipt?> GetAggregateByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<StockReceipt?> FindByNoAsync(string receiptNo, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<StockReceipt>> SearchAsync(DateTime? fromDate, DateTime? toDate, string keyword, int? warehouseId, CancellationToken cancellationToken = default);
}

