using ASMILE.BO.Core.Entities;
using ASMILE.BO.Core.Interfaces.Repositories;
using ASMILE.BO.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ASMILE.BO.Infrastructure.Repositories;

public sealed class DepartmentRepository : EfRepository<Department>, IDepartmentRepository
{
    public DepartmentRepository(AppDbContext dbContext) : base(dbContext)
    {
    }

    public async Task<IReadOnlyList<Department>> SearchAsync(string keyword, bool? isActive = null, CancellationToken cancellationToken = default)
    {
        var query = DbSet.AsNoTracking();
        keyword = keyword?.Trim() ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            query = query.Where(x => x.DepartmentCode.Contains(keyword) || x.DepartmentName.Contains(keyword));
        }

        if (isActive.HasValue)
        {
            query = query.Where(x => x.IsActive == isActive.Value);
        }

        return await query.OrderBy(x => x.Level).ThenBy(x => x.DepartmentCode).ToListAsync(cancellationToken);
    }

    public Task<bool> ExistsCodeAsync(string departmentCode, int excludingId = 0, CancellationToken cancellationToken = default)
        => DbSet.AnyAsync(x => x.DepartmentCode == departmentCode && x.Id != excludingId, cancellationToken);

    public Task<bool> HasChildrenAsync(int departmentId, CancellationToken cancellationToken = default)
        => DbSet.AnyAsync(x => x.ParentDepartmentId == departmentId, cancellationToken);

    public Task<bool> HasPeopleAsync(int departmentId, CancellationToken cancellationToken = default)
        => DbContext.People.AnyAsync(x => x.DepartmentId == departmentId, cancellationToken);
}
