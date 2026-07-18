using ASMILE.BO.Core.Entities;
using ASMILE.BO.Core.Interfaces.Repositories;
using ASMILE.BO.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ASMILE.BO.Infrastructure.Repositories;

public sealed class PersonRepository : EfRepository<Person>, IPersonRepository
{
    public PersonRepository(AppDbContext dbContext) : base(dbContext)
    {
    }

    public async Task<IReadOnlyList<Person>> SearchAsync(int? departmentId, string keyword, bool? isActive = null, CancellationToken cancellationToken = default)
    {
        var query = DbSet.AsNoTracking();
        keyword = keyword?.Trim() ?? string.Empty;

        if (departmentId.HasValue && departmentId.Value > 0)
        {
            query = query.Where(x => x.DepartmentId == departmentId.Value);
        }

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            query = query.Where(x =>
                x.PersonCode.Contains(keyword) ||
                x.FullName.Contains(keyword) ||
                x.Phone.Contains(keyword) ||
                x.Position.Contains(keyword));
        }

        if (isActive.HasValue)
        {
            query = query.Where(x => x.IsActive == isActive.Value);
        }

        return await query.OrderBy(x => x.PersonCode).ToListAsync(cancellationToken);
    }

    public Task<bool> ExistsCodeAsync(string personCode, int excludingId = 0, CancellationToken cancellationToken = default)
        => DbSet.AnyAsync(x => x.PersonCode == personCode && x.Id != excludingId, cancellationToken);
}
