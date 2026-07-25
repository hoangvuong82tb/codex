using ASMILE.BO.Application.FrontOffice.Foundation.Cqrs;
using ASMILE.BO.Core.FrontOffice.Housekeeping;

namespace ASMILE.BO.Application.FrontOffice.Housekeeping;

// =============================================================================
// ENHANCED STATUS MODEL
// =============================================================================

/// <summary>Extended housekeeping room status — superset of existing HousekeepingRoomStatus.</summary>
public enum HkRoomStatus
{
    VacantClean, VacantDirty, OccupiedClean, OccupiedDirty,
    Inspected, Pickup, Turndown, DoNotDisturb, SleepOut, Skipped,
    CleaningInProgress, InspectionFailed, ReadyForSale, MaintenancePending,
    OutOfOrder, OutOfService, Blocked
}

/// <summary>Status transition — defines valid state changes.</summary>
public sealed record HkStatusTransition(HkRoomStatus From, HkRoomStatus To, string? RequiredRole, string? RequiredEvent);

public static class HkStatusMachine
{
    public static readonly IReadOnlyList<HkStatusTransition> Transitions =
    [
        new(HkRoomStatus.VacantDirty, HkRoomStatus.CleaningInProgress, "Housekeeper", "CleaningStarted"),
        new(HkRoomStatus.CleaningInProgress, HkRoomStatus.Inspected, "Housekeeper", "CleaningCompleted"),
        new(HkRoomStatus.Inspected, HkRoomStatus.VacantClean, "Supervisor", "RoomInspected"),
        new(HkRoomStatus.Inspected, HkRoomStatus.InspectionFailed, "Supervisor", "InspectionFailed"),
        new(HkRoomStatus.InspectionFailed, HkRoomStatus.CleaningInProgress, "Housekeeper", "Reclean"),
        new(HkRoomStatus.OccupiedDirty, HkRoomStatus.CleaningInProgress, "Housekeeper", "CleaningStarted"),
        new(HkRoomStatus.VacantClean, HkRoomStatus.OccupiedClean, null, "GuestCheckedIn"),
        new(HkRoomStatus.OccupiedClean, HkRoomStatus.OccupiedDirty, null, "GuestCheckedOut"),
        new(HkRoomStatus.VacantClean, HkRoomStatus.MaintenancePending, "Supervisor", "MaintenanceRequested"),
        new(HkRoomStatus.MaintenancePending, HkRoomStatus.OutOfOrder, "Maintenance", "MaintenanceConfirmed"),
        new(HkRoomStatus.OutOfOrder, HkRoomStatus.VacantDirty, "Maintenance", "MaintenanceResolved"),
        new(HkRoomStatus.VacantClean, HkRoomStatus.DoNotDisturb, null, "DndRequested"),
        new(HkRoomStatus.DoNotDisturb, HkRoomStatus.VacantDirty, null, "DndReleased"),
    ];

    public static bool IsValidTransition(HkRoomStatus from, HkRoomStatus to) => Transitions.Any(t => t.From == from && t.To == to);
    public static IReadOnlyList<HkRoomStatus> AllowedNext(HkRoomStatus from) => Transitions.Where(t => t.From == from).Select(t => t.To).ToList();
}

// =============================================================================
// ROOM ASSIGNMENT
// =============================================================================

public sealed record AttendantDto(Guid AttendantId, string Name, string? Zone, string? Floor, int SkillLevel, int CurrentWorkload, int MaxCapacity, IReadOnlyList<string> Certifications);
public sealed record AssignmentRequest(Guid PropertyId, string? Floor, string? Zone, int RoomCount, AssignmentStrategy Strategy);
public enum AssignmentStrategy { Auto, Manual, SkillBased, FloorBased, ZoneBased, Balanced, PriorityQueue }
public sealed record AssignmentResult(Guid AttendantId, IReadOnlyList<Guid> AssignedRoomIds, int TotalDurationMinutes, bool IsBalanced);

// =============================================================================
// INSPECTION ENGINE
// =============================================================================

public sealed record InspectionChecklist(Guid ChecklistId, string Name, IReadOnlyList<InspectionItem> Items);
public sealed record InspectionItem(string Code, string Description, string Category, int MaxScore, bool IsMandatory, string? PhotoRequired);
public sealed record InspectionResult(Guid TaskId, Guid InspectorId, int TotalScore, int MaxScore, decimal PassPercent, bool Passed, IReadOnlyList<InspectionItemResult> Items, IReadOnlyList<string> PhotoUrls, string? Notes);
public sealed record InspectionItemResult(string Code, int Score, bool Passed, string? Comment, string? PhotoUrl);
public enum InspectionOutcome { Approved, Rejected, ConditionalPass }

// =============================================================================
// LINEN MANAGEMENT
// =============================================================================

public sealed record LinenType(string Code, string Name, string Category, int DefaultParLevel, int MaxWashCycles);
public sealed record LinenInventory(Guid RoomId, string LinenCode, int Quantity, int WashCycleCount, int DamagedCount, DateTime LastRestockedUtc);
public enum LinenAction { Issued, Returned, Damaged, Discarded, Restocked, Washed }
public sealed record LinenTransaction(Guid TransactionId, string LinenCode, Guid RoomId, LinenAction Action, int Quantity, DateTime OccurredAtUtc, Guid? ActorId);

// =============================================================================
// AMENITIES
// =============================================================================

public sealed record AmenityType(string Code, string Name, string Category, int DefaultParLevel, int MinAlertThreshold);
public sealed record AmenityInventory(Guid RoomId, string AmenityCode, int CurrentStock, int ParLevel, int ConsumptionLast30Days, DateTime LastRefillUtc);
public sealed record AmenityRefillRequest(Guid RoomId, IReadOnlyList<(string AmenityCode, int Quantity)> Items, DateTime RequestedAtUtc);

// =============================================================================
// LOST & FOUND (enhanced from existing LostFound domain)
// =============================================================================

public enum LostFoundItemStatus { Found, Stored, Claimed, Returned, Donated, Disposed, Unclaimed }
public sealed record LostFoundSearchFilter(Guid PropertyId, string? Keyword, LostFoundItemStatus? Status, DateTime? FoundFrom, DateTime? FoundTo, string? Location, string? Category);
public sealed record LostFoundDashboardDto(int TotalItems, int Stored, int Claimed, int Returned, int Disposed, int Unclaimed, decimal ClaimRate);

// =============================================================================
// DASHBOARD
// =============================================================================

public sealed record HkExecutiveDashboard(
    int TotalRooms, int VacantClean, int VacantDirty, int OccupiedClean, int OccupiedDirty,
    int CleaningInProgress, int Inspected, int OutOfOrder, int OutOfService,
    decimal ReadinessPct, int ArrivalsToday, int DeparturesToday,
    int ActiveAttendants, int CompletedTasks, int PendingInspections,
    decimal AvgCleaningMinutes, decimal InspectionPassRate,
    int LostFoundActive, int LinenDiscardedThisMonth, int AmenityRefillsToday
);

public sealed record HkSupervisorDashboard(
    string SupervisorName, string? Floor, string? Zone,
    IReadOnlyList<AttendantDto> Attendants,
    IReadOnlyList<HkTaskBoardDto> PendingInspections,
    IReadOnlyList<HkTaskBoardDto> OverdueTasks,
    decimal TeamCleaningAvg, decimal TeamInspectionRate
);

public sealed record HkAttendantDashboard(
    Guid AttendantId, string Name, int AssignedRooms, int CompletedToday,
    int RemainingRooms, decimal AvgTimePerRoom, int EstimatedMinutesLeft,
    IReadOnlyList<HkTaskBoardDto> CurrentTasks,
    IReadOnlyList<HkTaskBoardDto> NextUp
);

public sealed record HkFloorDashboard(
    string Floor, int TotalRooms, int Occupied, int Vacant, int Cleaning,
    int Ready, IReadOnlyList<HkTaskBoardDto> ActiveTasks,
    IReadOnlyList<Guid> RoomsNeedingAttention
);

public sealed record HkTaskBoardDto(
    Guid TaskId, string TaskNumber, string RoomNumber, HkRoomStatus RoomStatus,
    HousekeepingTaskType TaskType, HousekeepingTaskStatus TaskStatus,
    RoomPriority Priority, string? AssignedTo, string? AssignedName,
    DateTime? StartedAt, int? EstimatedMinutesLeft, bool IsOverdue,
    bool IsVip, bool HasInspectionPending
);

// =============================================================================
// ENHANCED CONTRACTS
// =============================================================================

public interface IHousekeepingBoardService
{
    Task<HkExecutiveDashboard> GetExecutiveDashboardAsync(Guid propertyId, CancellationToken ct);
    Task<HkSupervisorDashboard> GetSupervisorDashboardAsync(Guid propertyId, Guid supervisorId, CancellationToken ct);
    Task<HkAttendantDashboard> GetAttendantDashboardAsync(Guid propertyId, Guid attendantId, CancellationToken ct);
    Task<HkFloorDashboard> GetFloorDashboardAsync(Guid propertyId, string floor, CancellationToken ct);
    Task<IReadOnlyList<HkTaskBoardDto>> GetTaskBoardAsync(Guid propertyId, string? floor, string? zone, RoomPriority? priority, CancellationToken ct);
}

public interface IAssignmentEngine
{
    Task<AssignmentResult> AssignRoomsAsync(AssignmentRequest request, CancellationToken ct);
    Task<AssignmentResult> ReassignAsync(Guid attendantId, Guid roomId, string reason, CancellationToken ct);
    Task<IReadOnlyList<AttendantDto>> GetAvailableAttendantsAsync(Guid propertyId, string? floor, CancellationToken ct);
    Task<int> GetAttendantWorkloadAsync(Guid attendantId, CancellationToken ct);
}

public interface IInspectionEngine
{
    Task<InspectionResult> InspectAsync(Guid taskId, Guid inspectorId, InspectionChecklist checklist, CancellationToken ct);
    Task<InspectionChecklist> GetChecklistAsync(HousekeepingTaskType taskType, CancellationToken ct);
    Task<decimal> GetPassRateAsync(Guid propertyId, DateOnly? from, DateOnly? to, CancellationToken ct);
}

public interface ILinenService
{
    Task<IReadOnlyList<LinenInventory>> GetInventoryAsync(Guid propertyId, string? floor, CancellationToken ct);
    Task RecordLinenTransactionAsync(LinenTransaction tx, CancellationToken ct);
    Task<IReadOnlyList<LinenType>> GetLinenTypesAsync(CancellationToken ct);
}

public interface IAmenityService
{
    Task<IReadOnlyList<AmenityInventory>> GetInventoryAsync(Guid propertyId, string? floor, CancellationToken ct);
    Task<AmenityRefillRequest> CreateRefillRequestAsync(Guid roomId, CancellationToken ct);
    Task<IReadOnlyList<AmenityType>> GetAmenityTypesAsync(CancellationToken ct);
}
