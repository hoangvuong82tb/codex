using ASMILE.BO.Core.Entities;
using ASMILE.BO.Core.Interfaces.Repositories;
using ASMILE.BO.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ASMILE.BO.Infrastructure.Repositories;

public sealed class StockBalanceRepository : EfRepository<StockBalance>, IStockBalanceRepository
{
    public StockBalanceRepository(AppDbContext dbContext) : base(dbContext)
    {
    }

    public Task<StockBalance?> GetByWarehouseAndItemAsync(int warehouseId, int itemId, CancellationToken cancellationToken = default)
        => DbSet.AsNoTracking().FirstOrDefaultAsync(x => x.WarehouseId == warehouseId && x.ItemId == itemId, cancellationToken);

    public async Task<IReadOnlyList<StockBalance>> GetByWarehouseAsync(int warehouseId, CancellationToken cancellationToken = default)
    {
        return await DbSet.AsNoTracking().Where(x => x.WarehouseId == warehouseId).ToListAsync(cancellationToken);
    }

    public Task RemoveAllAsync(CancellationToken cancellationToken = default)
    {
        DbSet.RemoveRange(DbSet);
        return Task.CompletedTask;
    }
}

