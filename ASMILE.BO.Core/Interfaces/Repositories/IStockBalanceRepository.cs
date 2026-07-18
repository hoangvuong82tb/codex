using ASMILE.BO.Core.Entities;

namespace ASMILE.BO.Core.Interfaces.Repositories;

public interface IStockBalanceRepository : IRepository<StockBalance>
{
    Task<StockBalance?> GetByWarehouseAndItemAsync(int warehouseId, int itemId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<StockBalance>> GetByWarehouseAsync(int warehouseId, CancellationToken cancellationToken = default);
    Task RemoveAllAsync(CancellationToken cancellationToken = default);
}

