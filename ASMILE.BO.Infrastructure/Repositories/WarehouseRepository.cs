using ASMILE.BO.Core.Entities;
using ASMILE.BO.Core.Interfaces.Repositories;
using ASMILE.BO.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ASMILE.BO.Infrastructure.Repositories;

public sealed class WarehouseRepository : EfRepository<Warehouse>, IWarehouseRepository
{
    public WarehouseRepository(AppDbContext dbContext) : base(dbContext)
    {
    }

    public async Task<IReadOnlyList<Warehouse>> SearchAsync(string keyword, CancellationToken cancellationToken = default)
    {
        var q = DbSet.AsNoTracking();
        keyword = keyword?.Trim() ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            q = q.Where(x => x.WarehouseCode.Contains(keyword) || x.WarehouseName.Contains(keyword));
        }

        return await q.OrderBy(x => x.WarehouseCode).ToListAsync(cancellationToken);
    }

    public Task<bool> ExistsCodeAsync(string warehouseCode, int excludingId = 0, CancellationToken cancellationToken = default)
        => DbSet.AnyAsync(x => x.WarehouseCode == warehouseCode && x.Id != excludingId, cancellationToken);

    public Task<bool> HasChildrenAsync(int warehouseId, CancellationToken cancellationToken = default)
        => DbSet.AnyAsync(x => x.ParentWarehouseId == warehouseId, cancellationToken);

    public async Task<bool> IsUsedInTransactionsAsync(int warehouseId, CancellationToken cancellationToken = default)
    {
        return await DbContext.StockReceipts.AnyAsync(x => x.WarehouseId == warehouseId, cancellationToken)
               || await DbContext.StockIssues.AnyAsync(x => x.WarehouseId == warehouseId || x.ToWarehouseId == warehouseId, cancellationToken);
    }
}

