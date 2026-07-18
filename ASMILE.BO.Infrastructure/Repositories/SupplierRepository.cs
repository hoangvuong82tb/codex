using ASMILE.BO.Core.Entities;
using ASMILE.BO.Core.Interfaces.Repositories;
using ASMILE.BO.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ASMILE.BO.Infrastructure.Repositories;

public sealed class SupplierRepository : EfRepository<Supplier>, ISupplierRepository
{
    public SupplierRepository(AppDbContext dbContext) : base(dbContext)
    {
    }

    public async Task<IReadOnlyList<Supplier>> SearchAsync(string keyword, CancellationToken cancellationToken = default)
    {
        var q = DbSet.AsNoTracking();
        keyword = keyword?.Trim() ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            q = q.Where(x => x.SupplierCode.Contains(keyword) || x.SupplierName.Contains(keyword) || x.Phone.Contains(keyword));
        }

        return await q.OrderBy(x => x.SupplierCode).ToListAsync(cancellationToken);
    }

    public Task<bool> ExistsCodeAsync(string supplierCode, int excludingId = 0, CancellationToken cancellationToken = default)
        => DbSet.AnyAsync(x => x.SupplierCode == supplierCode && x.Id != excludingId, cancellationToken);

    public Task<bool> IsUsedInReceiptsAsync(string supplierCode, CancellationToken cancellationToken = default)
        => DbContext.StockReceipts.AnyAsync(x => x.SupplierCode == supplierCode, cancellationToken);
}

