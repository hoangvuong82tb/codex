using ASMILE.BO.Application.FrontOffice.Foundation.Audit;
using ASMILE.BO.Application.FrontOffice.Foundation.Contexts;
using ASMILE.BO.Application.FrontOffice.Foundation.Persistence;
using ASMILE.BO.Application.FrontOffice.Maintenance;
using ASMILE.BO.Core.FrontOffice.Foundation.Identifiers;
using ASMILE.BO.Core.FrontOffice.Maintenance;
using ASMILE.BO.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ASMILE.BO.Infrastructure.FrontOffice.Maintenance;

/// <summary>
/// EF-backed maintenance work-order vertical slice. No mock/fallback path is
/// allowed here: missing persistence or execution context is a hard failure.
/// </summary>
public sealed class EfWorkOrderService(
    AppDbContext db,
    IFrontOfficePersistenceSession persistence,
    IFrontOfficeExecutionContextAccessor contextAccessor,
    IClock clock) : IWorkOrderService
{
    public async Task<WorkOrder> CreateAsync(MaintenanceRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        var context = RequireContext(request.PropertyId);
        if (request.RoomId == Guid.Empty) throw new ArgumentException("MAINTENANCE_ROOM_REQUIRED");

        var room = await db.HotelRooms.AsNoTracking()
            .SingleOrDefaultAsync(x => x.PropertyId == request.PropertyId && x.HotelRoomId == request.RoomId, ct)
            ?? throw new KeyNotFoundException("MAINTENANCE_ROOM_NOT_FOUND");

        var now = clock.UtcNow;
        var order = MaintenanceWorkOrder.Create(
            request.PropertyId,
            await GenerateOrderNumberAsync(request.PropertyId, now, ct),
            request.RequestId,
            room.HotelRoomId,
            room.RoomNumber,
            request.Type.ToString(),
            request.Priority.ToString(),
            request.Category.ToString(),
            request.Description,
            estimatedMinutes: 0,
            now,
            context.Actor.ActorId);

        db.MaintenanceWorkOrders.Add(order);
        StageAudit("MaintenanceWorkOrderCreated", order, request.PropertyId);
        await CommitAsync(ct, "MAINTENANCE_CREATE_FAILED");
        return Map(order);
    }

    public async Task<WorkOrder> AssignAsync(Guid orderId, Guid engineerId, CancellationToken ct)
    {
        var context = RequireContext();
        var propertyId = context.Property.PropertyId.Value;
        var order = await Load(orderId, propertyId, ct);
        var engineer = await db.AppUsers.AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == engineerId && x.IsActive, ct)
            ?? throw new KeyNotFoundException("MAINTENANCE_ENGINEER_NOT_FOUND");

        var now = clock.UtcNow;
        order.Assign(engineer.Id, string.IsNullOrWhiteSpace(engineer.DisplayName) ? engineer.UserName : engineer.DisplayName, now, context.Actor.ActorId);
        StageAudit("MaintenanceWorkOrderAssigned", order, propertyId);
        await CommitAsync(ct, "MAINTENANCE_ASSIGN_FAILED");
        return Map(order);
    }

    public async Task<WorkOrder> TransitionAsync(Guid orderId, WorkOrderStatus to, string? note, Guid? actorId, CancellationToken ct)
    {
        var context = RequireContext();
        var propertyId = context.Property.PropertyId.Value;
        var order = await Load(orderId, propertyId, ct);
        if (!WorkOrderStateMachine.IsValid(ParseStatus(order.Status), to))
            throw new InvalidOperationException($"MAINTENANCE_INVALID_TRANSITION_{order.Status.ToUpperInvariant()}_{to.ToString().ToUpperInvariant()}");

        var now = clock.UtcNow;
        order.Transition(to.ToString(), note, actorId ?? context.Actor.ActorId?.Value, now);
        StageAudit("MaintenanceWorkOrderTransitioned", order, propertyId, note);
        await CommitAsync(ct, "MAINTENANCE_TRANSITION_FAILED");
        return Map(order);
    }

    public async Task<IReadOnlyList<WorkOrder>> GetActiveAsync(Guid propertyId, MaintenancePriority? minPriority, CancellationToken ct)
    {
        RequireContext(propertyId);
        var query = db.MaintenanceWorkOrders.AsNoTracking()
            .Include(x => x.Notes)
            .Where(x => x.PropertyId == propertyId && x.Status != nameof(WorkOrderStatus.Closed) && x.Status != nameof(WorkOrderStatus.Cancelled));
        if (minPriority is MaintenancePriority priority)
        {
            var allowed = Enum.GetValues<MaintenancePriority>()
                .Where(x => x >= priority)
                .Select(x => x.ToString())
                .ToArray();
            query = query.Where(x => allowed.Contains(x.Priority));
        }

        var rows = await query
            .OrderByDescending(x => x.Priority)
            .ThenBy(x => x.CreatedAtUtc)
            .ToListAsync(ct);
        return rows.Select(Map).ToList();
    }

    public async Task<MaintenanceDashboard> GetDashboardAsync(Guid propertyId, CancellationToken ct)
    {
        RequireContext(propertyId);
        var now = clock.UtcNow;
        var rows = await db.MaintenanceWorkOrders.AsNoTracking()
            .Include(x => x.Notes)
            .Where(x => x.PropertyId == propertyId)
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToListAsync(ct);

        // Keep the work-order dashboard complete: these values used to be
        // hard-coded as zero even when the property had real assets and
        // preventive-maintenance schedules in the database.
        var assets = await db.MaintenanceAssets.AsNoTracking()
            .Where(x => x.PropertyId == propertyId)
            .OrderBy(x => x.ConditionScore)
            .ThenBy(x => x.AssetCode)
            .ToListAsync(ct);
        var schedules = await db.PreventiveMaintenanceSchedules.AsNoTracking()
            .Where(x => x.PropertyId == propertyId)
            .ToListAsync(ct);

        var active = rows.Where(x => x.Status is not nameof(WorkOrderStatus.Closed) and not nameof(WorkOrderStatus.Cancelled)).ToList();
        var completedToday = rows.Count(x => x.Status == nameof(WorkOrderStatus.Completed) && x.CompletedAtUtc?.Date == now.Date);
        var completed = rows.Where(x => x.ActualMinutes.HasValue).Select(x => x.ActualMinutes!.Value).ToArray();
        var avg = completed.Length == 0 ? 0m : decimal.Round((decimal)completed.Average(), 2);
        var measured = rows.Where(x => x.Status == nameof(WorkOrderStatus.Completed) && x.ActualMinutes.HasValue && x.EstimatedMinutes > 0).ToArray();
        var sla = measured.Length == 0 ? 0m : decimal.Round(measured.Count(x => x.ActualMinutes <= x.EstimatedMinutes) * 100m / measured.Length, 2);
        var activeAssets = assets.Where(x => x.IsActive).ToList();
        var overdueSchedules = schedules.Count(x => x.CompletedAtUtc is null && x.ScheduledAtUtc < now);
        var dueThisWeek = schedules.Count(x => x.CompletedAtUtc is null && x.ScheduledAtUtc >= now && x.ScheduledAtUtc < now.AddDays(7));
        var assetsNeedingMaintenance = activeAssets.Count(x =>
            (x.NextMaintenanceDueUtc is DateTime due && due <= now) || x.ConditionScore < 70m);
        var assetsAtRisk = activeAssets.Count(x => x.ConditionScore < 50m || x.FailureCount >= 3);
        var criticalAssets = activeAssets
            .Where(x => x.ConditionScore < 50m || x.FailureCount >= 3)
            .Take(10)
            .Select(x => new AssetHealthDto(
                x.AssetId,
                x.AssetCode,
                x.Name,
                ParseAssetCategory(x.Category),
                x.ConditionScore,
                x.RuntimeHours,
                x.FailureCount,
                decimal.Round(Math.Clamp(x.ConditionScore / 100m, 0m, 1m), 2),
                (x.NextMaintenanceDueUtc is DateTime due && due <= now) || x.ConditionScore < 70m,
                true))
            .ToList();

        return new MaintenanceDashboard(
            active.Count,
            active.Count(x => x.Status == nameof(WorkOrderStatus.InProgress)),
            completedToday,
            active.Count(x => x.CreatedAtUtc < now.AddMinutes(-Math.Max(1, x.EstimatedMinutes)) && x.Status is nameof(WorkOrderStatus.Open) or nameof(WorkOrderStatus.Assigned) or nameof(WorkOrderStatus.Accepted) or nameof(WorkOrderStatus.InProgress)),
            active.Count(x => x.Priority == nameof(MaintenancePriority.Emergency)),
            avg,
            sla,
            assets.Count,
            assetsNeedingMaintenance,
            assetsAtRisk,
            dueThisWeek,
            overdueSchedules,
            active.Where(x => x.Priority == nameof(MaintenancePriority.Emergency)).Take(10).Select(Map).ToList(),
            criticalAssets);
    }

    private static AssetCategory ParseAssetCategory(string value)
        => Enum.TryParse<AssetCategory>(value, true, out var parsed)
            ? parsed
            : AssetCategory.Other;

    private async Task<MaintenanceWorkOrder> Load(Guid orderId, Guid propertyId, CancellationToken ct)
        => await db.MaintenanceWorkOrders
            .Include(x => x.Notes)
            .SingleOrDefaultAsync(x => x.WorkOrderId == orderId && x.PropertyId == propertyId, ct)
            ?? throw new KeyNotFoundException("MAINTENANCE_WORK_ORDER_NOT_FOUND");

    private FrontOfficeExecutionContext RequireContext(Guid? propertyId = null)
    {
        var context = contextAccessor.Current ?? throw new InvalidOperationException("FRONT_OFFICE_CONTEXT_REQUIRED");
        if (propertyId is Guid requested && requested != context.Property.PropertyId.Value)
            throw new InvalidOperationException("MAINTENANCE_PROPERTY_CONTEXT_MISMATCH");
        return context;
    }

    private async Task<string> GenerateOrderNumberAsync(Guid propertyId, DateTime nowUtc, CancellationToken ct)
    {
        for (var attempt = 0; attempt < 3; attempt++)
        {
            var value = $"WO-{nowUtc:yyyyMMddHHmmss}-{Uuid7.NewGuid():N}"[..42].ToUpperInvariant();
            if (!await db.MaintenanceWorkOrders.AnyAsync(x => x.PropertyId == propertyId && x.OrderNumber == value, ct))
                return value;
        }
        throw new InvalidOperationException("MAINTENANCE_ORDER_NUMBER_GENERATION_FAILED");
    }

    private void StageAudit(string action, MaintenanceWorkOrder order, Guid propertyId, string? reason = null)
        => persistence.StageAudit(new FrontOfficeAuditIntent(
            action,
            nameof(MaintenanceWorkOrder),
            order.WorkOrderId.ToString("D"),
            "Succeeded",
            ReasonCode: reason,
            Metadata: new Dictionary<string, string>
            {
                ["PropertyId"] = propertyId.ToString("D"),
                ["OrderNumber"] = order.OrderNumber,
                ["Status"] = order.Status,
                ["RoomId"] = order.RoomId?.ToString("D") ?? string.Empty
            }));

    private async Task CommitAsync(CancellationToken ct, string errorCode)
    {
        var result = await persistence.CommitAsync(ct);
        if (result.IsFailure)
            throw new InvalidOperationException(result.Error?.Code ?? errorCode);
    }

    private static WorkOrderStatus ParseStatus(string value)
        => Enum.TryParse<WorkOrderStatus>(value, ignoreCase: true, out var parsed)
            ? parsed
            : throw new InvalidOperationException("MAINTENANCE_STATUS_INVALID");

    private static WorkOrder Map(MaintenanceWorkOrder order)
        => new(
            order.WorkOrderId,
            order.OrderNumber,
            order.PropertyId,
            order.RequestId,
            order.RoomId,
            order.RoomNumber,
            Parse<MaintenanceType>(order.Type, "MAINTENANCE_TYPE_INVALID"),
            Parse<MaintenancePriority>(order.Priority, "MAINTENANCE_PRIORITY_INVALID"),
            Parse<AssetCategory>(order.Category, "MAINTENANCE_CATEGORY_INVALID"),
            order.Description,
            ParseStatus(order.Status),
            order.AssignedEngineerId,
            order.AssignedEngineerName,
            order.CreatedAtUtc,
            order.StartedAtUtc,
            order.CompletedAtUtc,
            order.VerifiedAtUtc,
            order.VerifiedBy,
            order.EstimatedMinutes,
            order.ActualMinutes,
            order.Resolution,
            order.Notes.OrderBy(x => x.AtUtc).Select(x => new WorkOrderNote(x.AtUtc, x.Action, x.Detail, x.ActorId)).ToList());

    private static T Parse<T>(string value, string error) where T : struct
        => Enum.TryParse<T>(value, ignoreCase: true, out var parsed) ? parsed : throw new InvalidOperationException(error);
}
