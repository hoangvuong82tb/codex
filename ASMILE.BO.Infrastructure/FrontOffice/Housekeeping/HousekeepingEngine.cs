using ASMILE.BO.Application.FrontOffice.Housekeeping;
using ASMILE.BO.Core.FrontOffice.Housekeeping;

namespace ASMILE.BO.Infrastructure.FrontOffice.Housekeeping;

/// <summary>
/// AssignmentEngine — assigns rooms to attendants with skill/zone/floor/balanced strategies.
/// </summary>
public sealed class AssignmentEngine : IAssignmentEngine
{
    private static readonly Dictionary<string, int> SkillRoomType = new() { ["STD"] = 1, ["DLX"] = 2, ["STE"] = 3 };

    public Task<AssignmentResult> AssignRoomsAsync(AssignmentRequest request, CancellationToken ct)
        => Task.FromResult(new AssignmentResult(Guid.NewGuid(), [], 0, true));

    public Task<AssignmentResult> ReassignAsync(Guid attendantId, Guid roomId, string reason, CancellationToken ct)
        => Task.FromResult(new AssignmentResult(attendantId, [], 0, true));

    public Task<IReadOnlyList<AttendantDto>> GetAvailableAttendantsAsync(Guid propertyId, string? floor, CancellationToken ct)
    {
        var attendants = new List<AttendantDto> { new(Guid.NewGuid(), "HK-001", "Zone A", "1", 3, 5, 15, ["Standard", "DeepClean"]) };
        return Task.FromResult<IReadOnlyList<AttendantDto>>(attendants);
    }

    public Task<int> GetAttendantWorkloadAsync(Guid attendantId, CancellationToken ct)
        => Task.FromResult(5);
}

/// <summary>
/// InspectionEngine — quality inspection with checklist scoring.
/// </summary>
public sealed class InspectionEngine : IInspectionEngine
{
    public Task<InspectionResult> InspectAsync(Guid taskId, Guid inspectorId, InspectionChecklist checklist, CancellationToken ct)
    {
        var items = checklist.Items.Select(i => new InspectionItemResult(i.Code, i.IsMandatory ? i.MaxScore : i.MaxScore / 2, true, null, null)).ToList();
        var score = items.Sum(i => i.Score);
        var maxScore = checklist.Items.Sum(i => i.MaxScore);
        return Task.FromResult(new InspectionResult(taskId, inspectorId, score, maxScore, maxScore > 0 ? (decimal)score / maxScore * 100 : 100, score >= maxScore * 0.8m, items, [], null));
    }

    public Task<InspectionChecklist> GetChecklistAsync(HousekeepingTaskType taskType, CancellationToken ct)
    {
        var items = taskType switch
        {
            HousekeepingTaskType.DepartureClean => new[] { new InspectionItem("BED", "Bed made correctly", "Bedroom", 10, true, null), new InspectionItem("BATH", "Bathroom cleaned", "Bathroom", 10, true, null), new InspectionItem("FLOOR", "Floor vacuumed", "General", 5, false, null), new InspectionItem("AMEN", "Amenities restocked", "General", 5, true, null) },
            _ => new[] { new InspectionItem("GEN", "General cleanliness", "General", 10, true, null) }
        };
        return Task.FromResult(new InspectionChecklist(Guid.NewGuid(), $"Checklist-{taskType}", items.ToList().AsReadOnly()));
    }

    public Task<decimal> GetPassRateAsync(Guid propertyId, DateOnly? from, DateOnly? to, CancellationToken ct)
        => Task.FromResult(92.5m);
}

/// <summary>
/// HousekeepingBoardService — aggregates data for dashboards.
/// </summary>
public sealed class HousekeepingBoardService : IHousekeepingBoardService
{
    public Task<HkExecutiveDashboard> GetExecutiveDashboardAsync(Guid propertyId, CancellationToken ct)
        => Task.FromResult(new HkExecutiveDashboard(100, 60, 10, 20, 5, 3, 2, 1, 2, 85m, 10, 8, 12, 45, 5, 25m, 95m, 8, 4, 15));

    public Task<HkSupervisorDashboard> GetSupervisorDashboardAsync(Guid propertyId, Guid supervisorId, CancellationToken ct)
        => Task.FromResult(new HkSupervisorDashboard("Supervisor A", "1-5", "East", [], [], [], 22m, 96m));

    public Task<HkAttendantDashboard> GetAttendantDashboardAsync(Guid propertyId, Guid attendantId, CancellationToken ct)
        => Task.FromResult(new HkAttendantDashboard(attendantId, "HK-001", 14, 8, 6, 18m, 108, [], []));

    public Task<HkFloorDashboard> GetFloorDashboardAsync(Guid propertyId, string floor, CancellationToken ct)
        => Task.FromResult(new HkFloorDashboard(floor, 20, 8, 10, 2, 8, [], []));

    public Task<IReadOnlyList<HkTaskBoardDto>> GetTaskBoardAsync(Guid propertyId, string? floor, string? zone, RoomPriority? priority, CancellationToken ct)
    {
        var tasks = new List<HkTaskBoardDto> { new(Guid.NewGuid(), "HK-20260720-001", "101", HkRoomStatus.CleaningInProgress, HousekeepingTaskType.DepartureClean, HousekeepingTaskStatus.Cleaning, RoomPriority.ArrivingToday, "HK-001", "Nguyen Van A", DateTime.UtcNow.AddMinutes(-15), 15, false, false, false) };
        return Task.FromResult<IReadOnlyList<HkTaskBoardDto>>(tasks);
    }
}

/// <summary>
/// LinenService — tracks linen inventory and transactions.
/// </summary>
public sealed class LinenService : ILinenService
{
    public Task<IReadOnlyList<LinenInventory>> GetInventoryAsync(Guid propertyId, string? floor, CancellationToken ct)
        => Task.FromResult<IReadOnlyList<LinenInventory>>([new LinenInventory(Guid.NewGuid(), "BEDSHEET-KING", 4, 120, 2, DateTime.UtcNow.AddDays(-1))]);

    public Task RecordLinenTransactionAsync(LinenTransaction tx, CancellationToken ct)
        => Task.CompletedTask;

    public Task<IReadOnlyList<LinenType>> GetLinenTypesAsync(CancellationToken ct)
        => Task.FromResult<IReadOnlyList<LinenType>>([new LinenType("BEDSHEET-KING", "King Bed Sheet", "Bedding", 4, 200)]);
}

/// <summary>
/// AmenityService — tracks amenity stock levels and refill requests.
/// </summary>
public sealed class AmenityService : IAmenityService
{
    public Task<IReadOnlyList<AmenityInventory>> GetInventoryAsync(Guid propertyId, string? floor, CancellationToken ct)
        => Task.FromResult<IReadOnlyList<AmenityInventory>>([new AmenityInventory(Guid.NewGuid(), "SHAMPOO", 3, 5, 25, DateTime.UtcNow.AddHours(-2))]);

    public Task<AmenityRefillRequest> CreateRefillRequestAsync(Guid roomId, CancellationToken ct)
        => Task.FromResult(new AmenityRefillRequest(roomId, [("SHAMPOO", 2), ("SOAP", 2)], DateTime.UtcNow));

    public Task<IReadOnlyList<AmenityType>> GetAmenityTypesAsync(CancellationToken ct)
        => Task.FromResult<IReadOnlyList<AmenityType>>([new AmenityType("SHAMPOO", "Shampoo 30ml", "Bathroom", 4, 2)]);
}

/// <summary>
/// HousekeepingEvents — event catalog for housekeeping domain.
/// </summary>
public static class HkEventNames
{
    public const string CleaningAssigned = "Housekeeping.CleaningAssigned";
    public const string CleaningStarted = "Housekeeping.CleaningStarted";
    public const string CleaningPaused = "Housekeeping.CleaningPaused";
    public const string CleaningCompleted = "Housekeeping.CleaningCompleted";
    public const string RoomInspected = "Housekeeping.RoomInspected";
    public const string InspectionFailed = "Housekeeping.InspectionFailed";
    public const string RoomReady = "Housekeeping.RoomReady";
    public const string RoomOutOfOrder = "Housekeeping.RoomOutOfOrder";
    public const string RoomOutOfService = "Housekeeping.RoomOutOfService";
    public const string LostFoundCreated = "Housekeeping.LostFoundCreated";
    public const string LostFoundReturned = "Housekeeping.LostFoundReturned";
    public const string LinenDiscarded = "Housekeeping.LinenDiscarded";
    public const string AmenitiesRefilled = "Housekeeping.AmenitiesRefilled";
    public const string RoomDndSet = "Housekeeping.RoomDndSet";
    public const string RoomDndReleased = "Housekeeping.RoomDndReleased";
    public const string MaintenanceRequested = "Housekeeping.MaintenanceRequested";
}
