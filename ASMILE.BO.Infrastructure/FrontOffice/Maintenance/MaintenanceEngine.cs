using ASMILE.BO.Application.FrontOffice.Maintenance;

namespace ASMILE.BO.Infrastructure.FrontOffice.Maintenance;

/// <summary>WorkOrderService — manages work order lifecycle.</summary>
public sealed class WorkOrderService : IWorkOrderService
{
    public Task<WorkOrder> CreateAsync(MaintenanceRequest r, CancellationToken ct)
    {
        var wo = new WorkOrder(Guid.NewGuid(), $"WO-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid():N}"[..6], r.PropertyId, r.RequestId, r.RoomId, r.RoomNumber, r.Type, r.Priority, r.Category, r.Description, WorkOrderStatus.Open, null, null, DateTime.UtcNow, null, null, null, null, 30, null, null, [new WorkOrderNote(DateTime.UtcNow, "Created", r.Description, null)]);
        return Task.FromResult(wo);
    }

    public Task<WorkOrder> AssignAsync(Guid orderId, Guid engineerId, CancellationToken ct)
        => Task.FromResult(new WorkOrder(orderId, "WO-001", Guid.NewGuid(), null, null, "101", MaintenanceType.Corrective, MaintenancePriority.High, AssetCategory.HVAC, "AC not cooling", WorkOrderStatus.Assigned, engineerId, "Engineer A", DateTime.UtcNow, null, null, null, null, 45, null, null, [new WorkOrderNote(DateTime.UtcNow, "Assigned", "Engineer A", null)]));

    public Task<WorkOrder> TransitionAsync(Guid orderId, WorkOrderStatus to, string? note, Guid? actorId, CancellationToken ct)
    {
        var wo = new WorkOrder(orderId, "WO-001", Guid.NewGuid(), null, null, "101", MaintenanceType.Corrective, MaintenancePriority.High, AssetCategory.HVAC, "AC not cooling", to, Guid.NewGuid(), "Engineer A", DateTime.UtcNow, to >= WorkOrderStatus.InProgress ? DateTime.UtcNow : null, to >= WorkOrderStatus.Completed ? DateTime.UtcNow : null, to >= WorkOrderStatus.Verified ? DateTime.UtcNow : null, actorId, 45, to == WorkOrderStatus.Completed ? 30 : null, to == WorkOrderStatus.Completed ? "Replaced capacitor" : null, [new WorkOrderNote(DateTime.UtcNow, to.ToString(), note, actorId)]);
        return Task.FromResult(wo);
    }

    public Task<IReadOnlyList<WorkOrder>> GetActiveAsync(Guid propertyId, MaintenancePriority? minPriority, CancellationToken ct)
    {
        var wo = new WorkOrder(Guid.NewGuid(), "WO-001", propertyId, null, null, "101", MaintenanceType.Emergency, MaintenancePriority.Critical, AssetCategory.DoorLock, "Door lock jammed", WorkOrderStatus.InProgress, Guid.NewGuid(), "Engineer A", DateTime.UtcNow.AddHours(-1), DateTime.UtcNow.AddMinutes(-30), null, null, null, 30, null, null, [new WorkOrderNote(DateTime.UtcNow.AddHours(-1), "Created", "Emergency", null)]);
        return Task.FromResult<IReadOnlyList<WorkOrder>>([wo]);
    }

    public Task<MaintenanceDashboard> GetDashboardAsync(Guid propertyId, CancellationToken ct)
    {
        var dash = new MaintenanceDashboard(12, 5, 8, 2, 1, 45m, 92m, 200, 15, 3, 4, 1, [], [new AssetHealthDto(Guid.NewGuid(), "AC-001", "AC Unit 5F", AssetCategory.HVAC, 72m, 15000, 2, 0.85m, false, false)]);
        return Task.FromResult(dash);
    }
}

/// <summary>AssetService — tracks hotel assets and their health.</summary>
public sealed class AssetService : IAssetService
{
    public Task<IReadOnlyList<AssetDto>> GetAssetsByRoomAsync(Guid roomId, CancellationToken ct)
    {
        var assets = new List<AssetDto> { new(Guid.NewGuid(), "TV-101", "Samsung TV 55\"", AssetCategory.TV, "Room 101", roomId, "SN12345", "QLED-2025", "Samsung", DateTime.UtcNow.AddYears(2), DateTime.UtcNow.AddYears(-1), 1, 8000, 1, 85m, DateTime.UtcNow.AddMonths(-3), DateTime.UtcNow.AddMonths(3), true) };
        return Task.FromResult<IReadOnlyList<AssetDto>>(assets);
    }

    public Task<IReadOnlyList<AssetHealthDto>> GetHealthAsync(Guid propertyId, AssetCategory? category, CancellationToken ct)
    {
        var health = new List<AssetHealthDto> { new(Guid.NewGuid(), "ELV-001", "Elevator Main", AssetCategory.Elevator, 90m, 20000, 0, 0.95m, false, false) };
        return Task.FromResult<IReadOnlyList<AssetHealthDto>>(health);
    }

    public Task<AssetDashboard> GetDashboardAsync(Guid propertyId, CancellationToken ct)
        => Task.FromResult(new AssetDashboard(200, 185, 50, 20, 12, 82m, [], []));

    public Task RecordFailureAsync(Guid assetId, string description, CancellationToken ct)
        => Task.CompletedTask;
}

/// <summary>PreventiveService — manages preventive maintenance plans and schedules.</summary>
public sealed class PreventiveService : IPreventiveService
{
    public Task<IReadOnlyList<PreventivePlan>> GetPlansAsync(AssetCategory? category, CancellationToken ct)
    {
        var plans = new List<PreventivePlan> { new(Guid.NewGuid(), "AC Quarterly Service", AssetCategory.HVAC, "Quarterly AC filter replacement and coil cleaning", null, 90, 60, [new MaintenanceChecklistItem("FILTER", "Replace air filter", "HVAC", true, "Use OEM filter only"), new MaintenanceChecklistItem("COIL", "Clean condenser coil", "HVAC", true, null), new MaintenanceChecklistItem("TEST", "Test cooling performance", "HVAC", true, "Target: 18-22°C")], true) };
        return Task.FromResult<IReadOnlyList<PreventivePlan>>(plans);
    }

    public Task<IReadOnlyList<PreventiveSchedule>> GetScheduleAsync(Guid propertyId, DateTime from, DateTime to, CancellationToken ct)
    {
        var sched = new List<PreventiveSchedule> { new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow.AddDays(3), null, null, false) };
        return Task.FromResult<IReadOnlyList<PreventiveSchedule>>(sched);
    }

    public Task<IReadOnlyList<PreventiveSchedule>> GetOverdueAsync(Guid propertyId, CancellationToken ct)
    {
        var overdue = new List<PreventiveSchedule> { new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow.AddDays(-5), null, null, true) };
        return Task.FromResult<IReadOnlyList<PreventiveSchedule>>(overdue);
    }
}
