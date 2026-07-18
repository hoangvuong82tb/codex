using ASMILE.BO.Core.Entities;

namespace ASMILE.BO.Core.Interfaces.Repositories;

public interface IWarehouseRepository : IRepository<Warehouse>
{
    Task<IReadOnlyList<Warehouse>> SearchAsync(string keyword, CancellationToken cancellationToken = default);
    Task<bool> ExistsCodeAsync(string warehouseCode, int excludingId = 0, CancellationToken cancellationToken = default);
    Task<bool> HasChildrenAsync(int warehouseId, CancellationToken cancellationToken = default);
    Task<bool> IsUsedInTransactionsAsync(int warehouseId, CancellationToken cancellationToken = default);
}

