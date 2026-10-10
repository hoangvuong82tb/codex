using ASMILE.BO.Application.FrontOffice.Maintenance;
using ASMILE.BO.Application.FrontOffice.Foundation.Contexts;
using ASMILE.BO.Core.FrontOffice.Maintenance;
using ASMILE.BO.Infrastructure.FrontOffice.Foundation;
using ASMILE.BO.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ASMILE.BO.Infrastructure.FrontOffice.Maintenance;

public sealed class EfPreventiveMaintenanceService(
    AppDbContext db,
    IFrontOfficeExecutionContextAccessor contextAccessor,
    IClock clock) : IPreventiveService
{
    public async Task<IReadOnlyList<PreventivePlan>> GetPlansAsync(AssetCategory? category, CancellationToken ct)
    {
        var propertyId = RequireContext().Property.PropertyId.Value;
        var query = db.PreventiveMaintenancePlans.AsNoTracking()
            .Include(x => x.Checklist)
            .Where(x => x.IsActive && (x.PropertyId == null || x.PropertyId == propertyId));
        if (category is AssetCategory requested)
            query = query.Where(x => x.Category == requested.ToString());

        var rows = await query.OrderBy(x => x.PlanName).ToListAsync(ct);
        return rows.Select(Map).ToList();
    }

    public async Task<IReadOnlyList<PreventiveSchedule>> GetScheduleAsync(Guid propertyId, DateTime from, DateTime to, CancellationToken ct)
    {
        RequireContext(propertyId);
        if (to < from) throw new ArgumentException("MAINTENANCE_SCHEDULE_RANGE_INVALID");

        var rows = await db.PreventiveMaintenanceSchedules.AsNoTracking()
            .Where(x => x.PropertyId == propertyId && x.ScheduledAtUtc >= from && x.ScheduledAtUtc <= to)
            .OrderBy(x => x.ScheduledAtUtc)
            .ToListAsync(ct);
        return rows.Select(Map).ToList();
    }

    public async Task<IReadOnlyList<PreventiveSchedule>> GetOverdueAsync(Guid propertyId, CancellationToken ct)
    {
        RequireContext(propertyId);
        var now = clock.UtcNow;
        var rows = await db.PreventiveMaintenanceSchedules.AsNoTracking()
            .Where(x => x.PropertyId == propertyId && x.CompletedAtUtc == null && x.ScheduledAtUtc < now)
            .OrderBy(x => x.ScheduledAtUtc)
            .ToListAsync(ct);
        return rows.Select(Map).ToList();
    }

    private FrontOfficeExecutionContext RequireContext(Guid? propertyId = null)
    {
        var context = contextAccessor.Current ?? throw new InvalidOperationException("FRONT_OFFICE_CONTEXT_REQUIRED");
        if (propertyId is Guid requested && requested != context.Property.PropertyId.Value)
            throw new InvalidOperationException("MAINTENANCE_PROPERTY_CONTEXT_MISMATCH");
        return context;
    }

    private PreventivePlan Map(PreventiveMaintenancePlan plan)
        => new(
            plan.PlanId,
            plan.PlanName,
            ParseCategory(plan.Category),
            plan.Description,
            plan.AssetId,
            plan.IntervalDays,
            plan.EstimatedMinutes,
            plan.Checklist.OrderBy(x => x.Code).Select(x => new MaintenanceChecklistItem(
                x.Code,
                x.Description,
                x.Category,
                x.IsMandatory,
                x.Reference)).ToList(),
            plan.IsActive);

    private PreventiveSchedule Map(PreventiveMaintenanceSchedule schedule)
        => new(
            schedule.ScheduleId,
            schedule.PlanId,
            schedule.AssetId,
            schedule.RoomId,
            schedule.ScheduledAtUtc,
            schedule.CompletedAtUtc,
            schedule.WorkOrderId,
            schedule.CompletedAtUtc == null && schedule.ScheduledAtUtc < clock.UtcNow);

    private static AssetCategory ParseCategory(string value)
        => Enum.TryParse<AssetCategory>(value, true, out var parsed)
            ? parsed
            : throw new InvalidOperationException("MAINTENANCE_PLAN_CATEGORY_INVALID");
}
