using ASMILE.BO.Core.FrontOffice.Foundation.Identifiers;

namespace ASMILE.BO.Core.FrontOffice.Housekeeping;

public enum HousekeepingTaskType { DepartureClean, StayoverService, DeepClean, Inspection, MaintenanceFollowUp, VIPPreparation, ExtraBed, SpecialRequest }
public enum HousekeepingTaskStatus { Pending, Assigned, Accepted, Cleaning, WaitingInspection, Completed, Cancelled }
public enum HousekeepingInspectionResult { Approved, Rejected }
public enum RoomPriority { Normal, VIP, ArrivingToday, EarlyCheckIn, LateCheckOut, LongStay, OutOfOrder, ManagerPriority }
public enum HousekeepingRoomStatus { VacantClean, VacantDirty, OccupiedClean, OccupiedDirty, Cleaning, Inspected, OutOfOrder, OutOfService, Blocked }

public sealed class HousekeepingTask
{
    private HousekeepingTask() { }
    public Guid HousekeepingTaskId { get; private set; }
    public Guid PropertyId { get; private set; }
    public Guid HotelRoomId { get; private set; }
    public string TaskNumber { get; private set; } = string.Empty;
    public HousekeepingTaskType TaskType { get; private set; }
    public HousekeepingTaskStatus Status { get; private set; }
    public RoomPriority Priority { get; private set; }
    public Guid? AssignedHousekeeperId { get; private set; }
    public string? InspectorNotes { get; private set; }
    public HousekeepingInspectionResult? InspectionResult { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public Guid? CreatedBy { get; private set; }
    public DateTime? StartedAtUtc { get; private set; }
    public DateTime? CompletedAtUtc { get; private set; }
    public DateTime? InspectedAtUtc { get; private set; }
    public DateTime? CancelledAtUtc { get; private set; }
    public byte[]? RowVersion { get; private set; }

    public static HousekeepingTask Create(Guid propertyId, Guid roomId, string taskNumber, HousekeepingTaskType type, RoomPriority priority, DateTime nowUtc, ActorId? actorId) => new()
    { HousekeepingTaskId = Uuid7.NewGuid(), PropertyId = propertyId, HotelRoomId = roomId, TaskNumber = taskNumber, TaskType = type, Status = HousekeepingTaskStatus.Pending, Priority = priority, CreatedAtUtc = nowUtc, CreatedBy = actorId?.Value };

    public void Assign(Guid housekeeperId, DateTime nowUtc) { RequireStatus(HousekeepingTaskStatus.Pending); Status = HousekeepingTaskStatus.Assigned; AssignedHousekeeperId = housekeeperId; }
    public void Accept(DateTime nowUtc) { RequireStatus(HousekeepingTaskStatus.Assigned); Status = HousekeepingTaskStatus.Accepted; }
    public void StartCleaning(DateTime nowUtc) { RequireStatus(HousekeepingTaskStatus.Accepted); Status = HousekeepingTaskStatus.Cleaning; StartedAtUtc = nowUtc; }
    public void CompleteCleaning(DateTime nowUtc) { RequireStatus(HousekeepingTaskStatus.Cleaning); Status = HousekeepingTaskStatus.WaitingInspection; CompletedAtUtc = nowUtc; }
    public void Inspect(HousekeepingInspectionResult result, string? notes, DateTime nowUtc)
    { RequireStatus(HousekeepingTaskStatus.WaitingInspection); Status = result == HousekeepingInspectionResult.Approved ? HousekeepingTaskStatus.Completed : HousekeepingTaskStatus.Pending; InspectionResult = result; InspectorNotes = notes; InspectedAtUtc = nowUtc; }
    public void Cancel(string reason, DateTime nowUtc) { Status = HousekeepingTaskStatus.Cancelled; CancelledAtUtc = nowUtc; }
    private void RequireStatus(HousekeepingTaskStatus expected) { if (Status != expected) throw new InvalidOperationException($"HK_TASK_STATUS_{Status}"); }
}

public sealed class RoomHousekeepingState
{
    private RoomHousekeepingState() { }
    public Guid HotelRoomId { get; private set; }
    public Guid PropertyId { get; private set; }
    public HousekeepingRoomStatus Status { get; private set; }
    public DateTime StatusChangedAtUtc { get; private set; }
    public byte[]? RowVersion { get; private set; }

    public static RoomHousekeepingState Create(Guid roomId, Guid propertyId, DateTime nowUtc) => new()
    { HotelRoomId = roomId, PropertyId = propertyId, Status = HousekeepingRoomStatus.VacantClean, StatusChangedAtUtc = nowUtc };

    public void MarkDirty(DateTime nowUtc)
    {
        Status = Status switch
        {
            HousekeepingRoomStatus.OccupiedClean => HousekeepingRoomStatus.OccupiedDirty,
            HousekeepingRoomStatus.VacantClean or HousekeepingRoomStatus.Inspected => HousekeepingRoomStatus.VacantDirty,
            HousekeepingRoomStatus.OccupiedDirty or HousekeepingRoomStatus.VacantDirty or HousekeepingRoomStatus.Cleaning => Status,
            _ => Status
        };
        StatusChangedAtUtc = nowUtc;
    }
    public void MarkOccupied(DateTime nowUtc) { if (Status is HousekeepingRoomStatus.VacantClean or HousekeepingRoomStatus.Inspected) Status = HousekeepingRoomStatus.OccupiedClean; StatusChangedAtUtc = nowUtc; }
    public void MarkVacant(DateTime nowUtc) { if (Status is HousekeepingRoomStatus.OccupiedClean or HousekeepingRoomStatus.OccupiedDirty) Status = Status == HousekeepingRoomStatus.OccupiedDirty ? HousekeepingRoomStatus.VacantDirty : HousekeepingRoomStatus.VacantClean; StatusChangedAtUtc = nowUtc; }
    public void StartCleaning(DateTime nowUtc) { Status = HousekeepingRoomStatus.Cleaning; StatusChangedAtUtc = nowUtc; }
    public void MarkInspected(DateTime nowUtc) { Status = HousekeepingRoomStatus.Inspected; StatusChangedAtUtc = nowUtc; }
    public void MarkClean(DateTime nowUtc) { Status = HousekeepingRoomStatus.VacantClean; StatusChangedAtUtc = nowUtc; }
    public void MarkOutOfOrder(string? reason, DateTime nowUtc) { Status = HousekeepingRoomStatus.OutOfOrder; StatusChangedAtUtc = nowUtc; }
    public void MarkOutOfService(string? reason, DateTime nowUtc) { Status = HousekeepingRoomStatus.OutOfService; StatusChangedAtUtc = nowUtc; }
}
