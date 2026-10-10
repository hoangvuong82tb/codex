using ASMILE.BO.Application.FrontOffice.Rate;
using ASMILE.BO.Core.FrontOffice.Rate;
using ASMILE.BO.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ASMILE.BO.Infrastructure.FrontOffice.Rate;

public sealed class EfRateRepository(AppDbContext dbContext) : IRateRepository
{
    public Task<RatePlan?> GetAsync(Guid id, Guid propertyId, CancellationToken ct)
        => dbContext.RatePlans
            .Include(x => x.Calendar).Include(x => x.Rules).Include(x => x.Seasons)
            .SingleOrDefaultAsync(x => x.RatePlanId == id && x.PropertyId == propertyId, ct);

    public Task<List<RatePlan>> GetAllAsync(Guid propertyId, RatePlanSearchFilter filter, CancellationToken ct)
        => BuildQuery(propertyId, filter).Include(x => x.Calendar).Include(x => x.Rules).Include(x => x.Seasons).ToListAsync(ct);

    public async Task<List<RatePlanDetailDto>> GetAllDetailDtosAsync(Guid propertyId, RatePlanSearchFilter filter, CancellationToken ct)
    {
        var plans = await BuildQuery(propertyId, filter)
            .Include(x => x.Calendar).Include(x => x.Rules).Include(x => x.Seasons)
            .ToListAsync(ct);
        return plans.Select(ToDetailDto).ToList();
    }

    public Task AddAsync(RatePlan plan, CancellationToken ct) { dbContext.RatePlans.Add(plan); return Task.CompletedTask; }
    public Task<int> CountAsync(Guid propertyId, CancellationToken ct) => dbContext.RatePlans.CountAsync(x => x.PropertyId == propertyId, ct);

    private IQueryable<RatePlan> BuildQuery(Guid propertyId, RatePlanSearchFilter filter)
    {
        var q = dbContext.RatePlans.AsNoTracking().Where(x => x.PropertyId == propertyId);
        if (filter.Status is not null) q = q.Where(x => x.Status == filter.Status);
        if (filter.Hierarchy is not null) q = q.Where(x => x.Hierarchy == filter.Hierarchy);
        if (!string.IsNullOrWhiteSpace(filter.Keyword)) q = q.Where(x => x.PlanCode.Contains(filter.Keyword) || x.PlanName.Contains(filter.Keyword));
        return q.OrderBy(x => x.Hierarchy).ThenBy(x => x.PlanCode);
    }

    private static RatePlanDetailDto ToDetailDto(RatePlan p) => new(
        new RatePlanDto(p.RatePlanId, p.PlanCode, p.PlanName, p.RoomTypeId, p.BaseRate, p.Status, p.Hierarchy, p.EffectiveFrom, p.EffectiveTo, p.AllowYieldManagement, p.YieldMinRate, p.YieldMaxRate, p.YieldThresholdOccupancy, p.YieldAdjustmentPct, p.CreatedAtUtc, p.RowVersion),
        p.Calendar.Select(c => new RateCalendarDto(c.Date, c.Rate)).ToList(),
        p.Rules.Select(r => new RateRuleDto(r.RateRuleId, r.Type, r.Value, r.Priority, r.Exclusive, r.Combinable, r.IsActive, r.Description, r.ExpiryDate)).ToList(),
        p.Seasons.Select(s => new RateSeasonDto(s.RateSeasonId, s.Type, s.FromDate, s.ToDate, s.Multiplier, s.IsActive, s.Description)).ToList()
    );
}
