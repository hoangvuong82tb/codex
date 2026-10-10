using ASMILE.BO.Core.Entities;
using ASMILE.BO.Core.Interfaces.Repositories;
using ASMILE.BO.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ASMILE.BO.Infrastructure.Repositories;

public sealed class ItemRepository : EfRepository<Item>, IItemRepository
{
    public ItemRepository(AppDbContext dbContext) : base(dbContext)
    {
    }

    public async Task<IReadOnlyList<Item>> SearchAsync(string keyword, CancellationToken cancellationToken = default)
    {
        var q = DbSet.AsNoTracking();
        keyword = keyword?.Trim() ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            q = q.Where(x => x.ItemCode.Contains(keyword) || x.ItemName.Contains(keyword));
        }

        return await q.OrderBy(x => x.ItemCode).ToListAsync(cancellationToken);
    }

    public Task<Item?> FindByCodeAsync(string itemCode, CancellationToken cancellationToken = default)
        => DbSet.AsNoTracking().FirstOrDefaultAsync(x => x.ItemCode == itemCode, cancellationToken);

    public Task<bool> ExistsCodeAsync(string itemCode, int excludingId = 0, CancellationToken cancellationToken = default)
        => DbSet.AnyAsync(x => x.ItemCode == itemCode && x.Id != excludingId, cancellationToken);

    public Task<bool> ExistsNameAsync(string itemName, int excludingId = 0, CancellationToken cancellationToken = default)
        => DbSet.AnyAsync(x => x.ItemName == itemName && x.Id != excludingId, cancellationToken);

    public async Task<IReadOnlyList<string>> GetCodesStartingWithAsync(string prefix, CancellationToken cancellationToken = default)
        => await DbSet.AsNoTracking()
            .Where(x => x.ItemCode.StartsWith(prefix))
            .Select(x => x.ItemCode)
            .ToListAsync(cancellationToken);

    public async Task<bool> IsUsedInTransactionsAsync(int itemId, CancellationToken cancellationToken = default)
    {
        return await DbContext.StockReceiptDetails.AnyAsync(x => x.ItemId == itemId, cancellationToken)
               || await DbContext.StockIssueDetails.AnyAsync(x => x.ItemId == itemId, cancellationToken);
    }
}

