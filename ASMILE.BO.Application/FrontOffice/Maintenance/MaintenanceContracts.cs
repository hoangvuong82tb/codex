using ASMILE.BO.Application.FrontOffice.Foundation.Cqrs;
using ASMILE.BO.Core.FrontOffice.HotelMasterData;

namespace ASMILE.BO.Application.FrontOffice.Maintenance;

// =============================================================================
// MAINTENANCE REQUEST & WORK ORDER
// =============================================================================

public enum MaintenanceType { Corrective, Preventive, Predictive, Emergency, Inspection }
public enum WorkOrderStatus { Open, Assigned, Accepted, InProgress, Paused, Completed, Verified, Closed, Cancelled, Escalated }
public enum MaintenancePriority { Low, Medium, High, Critical, Emergency }
public enum AssetCategory { HVAC, TV, DoorLock, Lighting, Bathroom, Furniture, Elevator, FireEquipment, Generator, KitchenEquipment, Plumbing, Electrical, Network, Other }

/// <summary>Maintenance request — raised by housekeeping, guest, or inspection.</summary>
public sealed record MaintenanceRequest(
    Guid RequestId, Guid PropertyId, Guid RoomId, string RoomNumber,
    MaintenanceType Type, MaintenancePriority Priority, AssetCategory Category,
    string Description, string? ReportedBy, DateTime ReportedAtUtc,
    bool IsEmergency, bool RequiresExternalVendor, string? PhotoUrl
);

/// <summary>Work order — tracks execution of maintenance work.</summary>
public sealed record WorkOrder(
    Guid WorkOrderId, string OrderNumber, Guid PropertyId,
    Guid? RequestId, Guid? RoomId, string? RoomNumber,
    MaintenanceType Type, MaintenancePriority Priority, AssetCategory Category,
    string Description, WorkOrderStatus Status,
    Guid? AssignedEngineerId, string? AssignedEngineerName,
    DateTime CreatedAtUtc, DateTime? StartedAtUtc, DateTime? CompletedAtUtc,
    DateTime? VerifiedAtUtc, Guid? VerifiedBy,
    int EstimatedMinutes, int? ActualMinutes,
    string? Resolution, IReadOnlyList<WorkOrderNote> Notes
);

public sealed record WorkOrderNote(DateTime AtUtc, string Action, string? Detail, Guid? ActorId);

/// <summary>Work order state transitions.</summary>
public static class WorkOrderStateMachine
{
    public static readonly IReadOnlyDictionary<WorkOrderStatus, IReadOnlyList<WorkOrderStatus>> AllowedTransitions = new Dictionary<WorkOrderStatus, IReadOnlyList<WorkOrderStatus>>
    {
        [WorkOrderStatus.Open] = [WorkOrderStatus.Assigned, WorkOrderStatus.Cancelled],
        [WorkOrderStatus.Assigned] = [WorkOrderStatus.Accepted, WorkOrderStatus.Escalated, WorkOrderStatus.Cancelled],
        [WorkOrderStatus.Accepted] = [WorkOrderStatus.InProgress, WorkOrderStatus.Escalated],
        [WorkOrderStatus.InProgress] = [WorkOrderStatus.Paused, WorkOrderStatus.Completed, WorkOrderStatus.Escalated],
        [WorkOrderStatus.Paused] = [WorkOrderStatus.InProgress, WorkOrderStatus.Cancelled],
        [WorkOrderStatus.Completed] = [WorkOrderStatus.Verified, WorkOrderStatus.InProgress],
        [WorkOrderStatus.Verified] = [WorkOrderStatus.Closed],
        [WorkOrderStatus.Closed] = [],
        [WorkOrderStatus.Cancelled] = [WorkOrderStatus.Open],
        [WorkOrderStatus.Escalated] = [WorkOrderStatus.Assigned, WorkOrderStatus.Closed],
    };

    public static bool IsValid(WorkOrderStatus from, WorkOrderStatus to) => AllowedTransitions.TryGetValue(from, out var allowed) && allowed.Contains(to);
    public static IReadOnlyList<WorkOrderStatus> Next(WorkOrderStatus from) => AllowedTransitions.GetValueOrDefault(from, []);
    public static bool IsTerminal(WorkOrderStatus s) => s is WorkOrderStatus.Closed or WorkOrderStatus.Cancelled;
}

// =============================================================================
// ASSET MANAGEMENT
// =============================================================================

public sealed record AssetDto(
    Guid AssetId, string AssetCode, string Name, AssetCategory Category,
    string? Location, Guid? RoomId, string? SerialNumber, string? Model,
    string? Vendor, DateTime? WarrantyExpiry, DateTime? InstalledAtUtc,
    int TotalRepairs, int RuntimeHours, int FailureCount, decimal ConditionScore,
    DateTime? LastMaintenanceAtUtc, DateTime? NextMaintenanceDueUtc, bool IsActive
);

public sealed record AssetHealthDto(
    Guid AssetId, string AssetCode, string Name, AssetCategory Category,
    decimal ConditionScore, int RuntimeHours, int FailureCount,
    decimal RemainingUsefulLife, bool NeedsMaintenance, bool AtRisk
);

// =============================================================================
// PREVENTIVE MAINTENANCE
// =============================================================================

public sealed record PreventivePlan(
    Guid PlanId, string PlanName, AssetCategory Category, string Description,
    Guid? AssetId, int IntervalDays, int EstimatedMinutes,
    IReadOnlyList<MaintenanceChecklistItem> Checklist, bool IsActive
);

public sealed record MaintenanceChecklistItem(string Code, string Description, string Category, bool IsMandatory, string? Reference);

public sealed record PreventiveSchedule(
    Guid ScheduleId, Guid PlanId, Guid? AssetId, Guid? RoomId,
    DateTime ScheduledAtUtc, DateTime? CompletedAtUtc, Guid? WorkOrderId,
    bool IsOverdue
);

// =============================================================================
// DASHBOARD
// =============================================================================

public sealed record MaintenanceDashboard(
    int TotalOpenWorkOrders, int InProgress, int CompletedToday, int Overdue,
    int EmergencyActive, decimal AvgResolutionMinutes, decimal SlaCompliancePct,
    int TotalAssets, int AssetsNeedingMaintenance, int AssetsAtRisk,
    int PreventiveDueThisWeek, int PreventiveOverdue,
    IReadOnlyList<WorkOrder> RecentEmergencies,
    IReadOnlyList<AssetHealthDto> CriticalAssets
);

public sealed record AssetDashboard(
    int TotalAssets, int Active, int UnderWarranty, int ExpiredWarranty,
    int WithOpenWorkOrders, decimal AverageConditionScore,
    IReadOnlyList<AssetHealthDto> WorstCondition,
    IReadOnlyList<AssetDto> DueForMaintenance
);

public sealed record SlaDashboard(
    decimal OverallCompliance, decimal EmergencyResponseAvgMinutes,
    int SlaBreaches, decimal PreventiveCompletionRate,
    IReadOnlyList<(string Category, decimal Compliance)> ByCategory
);

// =============================================================================
// CONTRACTS
// =============================================================================

public interface IWorkOrderService
{
    Task<WorkOrder> CreateAsync(MaintenanceRequest request, CancellationToken ct);
    Task<WorkOrder> AssignAsync(Guid orderId, Guid engineerId, CancellationToken ct);
    Task<WorkOrder> TransitionAsync(Guid orderId, WorkOrderStatus to, string? note, Guid? actorId, CancellationToken ct);
    Task<IReadOnlyList<WorkOrder>> GetActiveAsync(Guid propertyId, MaintenancePriority? minPriority, CancellationToken ct);
    Task<MaintenanceDashboard> GetDashboardAsync(Guid propertyId, CancellationToken ct);
}

public interface IAssetService
{
    Task<IReadOnlyList<AssetDto>> GetAssetsByRoomAsync(Guid roomId, CancellationToken ct);
    Task<IReadOnlyList<AssetHealthDto>> GetHealthAsync(Guid propertyId, AssetCategory? category, CancellationToken ct);
    Task<AssetDashboard> GetDashboardAsync(Guid propertyId, CancellationToken ct);
    Task RecordFailureAsync(Guid assetId, string description, CancellationToken ct);
}

public interface IPreventiveService
{
    Task<IReadOnlyList<PreventivePlan>> GetPlansAsync(AssetCategory? category, CancellationToken ct);
    Task<IReadOnlyList<PreventiveSchedule>> GetScheduleAsync(Guid propertyId, DateTime from, DateTime to, CancellationToken ct);
    Task<IReadOnlyList<PreventiveSchedule>> GetOverdueAsync(Guid propertyId, CancellationToken ct);
}

// =============================================================================
// EVENTS
// =============================================================================

public static class MaintenanceEvents
{
    public const string MaintenanceRequested = "Maintenance.Requested";
    public const string WorkOrderCreated = "Maintenance.WorkOrderCreated";
    public const string WorkStarted = "Maintenance.WorkStarted";
    public const string WorkCompleted = "Maintenance.WorkCompleted";
    public const string WorkVerified = "Maintenance.WorkVerified";
    public const string RoomOutOfOrder = "Maintenance.RoomOutOfOrder";
    public const string RoomReturnedToService = "Maintenance.RoomReturnedToService";
    public const string AssetFailureDetected = "Maintenance.AssetFailureDetected";
    public const string PreventiveDue = "Maintenance.PreventiveDue";
    public const string PreventiveOverdue = "Maintenance.PreventiveOverdue";
    public const string InspectionCompleted = "Maintenance.InspectionCompleted";
}
