using ASMILE.BO.Core.Entities;

namespace ASMILE.BO.Infrastructure.Services;

public static class CompanyScopeQueryableExtensions
{
    public static IQueryable<T> ApplyCompanyScope<T>(this IQueryable<T> query, Guid? companyId, Guid? branchId = null, bool includeLegacyRows = true)
        where T : class, ICompanyScoped
    {
        if (companyId.HasValue)
        {
            query = query.Where(x => x.CompanyId == companyId || (includeLegacyRows && x.CompanyId == null));
        }

        if (branchId.HasValue && typeof(IBranchScoped).IsAssignableFrom(typeof(T)))
        {
            query = query.Where(x => (x as IBranchScoped)!.BranchId == branchId || (includeLegacyRows && (x as IBranchScoped)!.BranchId == null));
        }

        return query;
    }

    public static IQueryable<T> ApplyCompanyBranchScope<T>(this IQueryable<T> query, Guid? companyId, Guid? branchId = null, bool includeLegacyRows = true)
        where T : class, ICompanyScoped, IBranchScoped
    {
        if (companyId.HasValue)
        {
            query = query.Where(x => x.CompanyId == companyId || (includeLegacyRows && x.CompanyId == null));
        }

        if (branchId.HasValue)
        {
            query = query.Where(x => x.BranchId == branchId || (includeLegacyRows && x.BranchId == null));
        }

        return query;
    }
}
