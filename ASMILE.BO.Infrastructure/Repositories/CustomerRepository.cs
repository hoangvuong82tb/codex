using ASMILE.BO.Core.Entities;
using ASMILE.BO.Core.Interfaces.Repositories;
using ASMILE.BO.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ASMILE.BO.Infrastructure.Repositories;

public sealed class CustomerRepository : EfRepository<Customer>, ICustomerRepository
{
    public CustomerRepository(AppDbContext dbContext) : base(dbContext)
    {
    }

    public async Task<IReadOnlyList<Customer>> SearchAsync(string keyword, CancellationToken cancellationToken = default)
    {
        var q = DbSet.AsNoTracking();
        keyword = keyword?.Trim() ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            q = q.Where(x => x.CustomerCode.Contains(keyword) || x.CustomerName.Contains(keyword) || x.Phone.Contains(keyword));
        }

        return await q.OrderBy(x => x.CustomerCode).ToListAsync(cancellationToken);
    }

    public Task<bool> ExistsCodeAsync(string customerCode, int excludingId = 0, CancellationToken cancellationToken = default)
        => DbSet.AnyAsync(x => x.CustomerCode == customerCode && x.Id != excludingId, cancellationToken);
}
