namespace ASMILE.BO.Application.FrontOffice.Housekeeping;

/// <summary>
/// Read model for the housekeeping workforce directory. Assignment and KPI
/// values remain separate contracts until their operational source is approved.
/// </summary>
public sealed record WorkforceStaffSnapshot(
    int PersonId,
    string StaffCode,
    string FullName,
    string Role,
    string Shift,
    string Building,
    string Floor,
    string? Phone,
    string? Email,
    string? Supervisor,
    string Status,
    int TotalAssigned = 0,
    int Completed = 0,
    int InProgress = 0,
    int PendingInspection = 0,
    int VipRooms = 0,
    int RushRooms = 0,
    double AvgCleaningMinutes = 0,
    bool HasLinkedOperationalAccount = false,
    IReadOnlyList<WorkforceAssignmentSnapshot>? Assignments = null,
    int InspectionPass = 0,
    int InspectionFail = 0);

public sealed record WorkforceAssignmentSnapshot(
    string RoomNumber,
    string Priority,
    string Status,
    int? ActualMinutes = null);

public sealed record WorkforceShiftChange(
    string StaffCode,
    string Shift,
    string? Reason);

public sealed record WorkforceAreaChange(
    string StaffCode,
    string Building,
    string Floor,
    string? Reason);

public interface IWorkforceQueryService
{
    Task<IReadOnlyList<WorkforceStaffSnapshot>> QueryAsync(CancellationToken cancellationToken = default);
}

public interface IWorkforceCommandService
{
    Task ChangeShiftAsync(WorkforceShiftChange request, CancellationToken cancellationToken = default);
    Task ChangeAreaAsync(WorkforceAreaChange request, CancellationToken cancellationToken = default);
}
