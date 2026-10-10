using ASMILE.BO.Core.FrontOffice.TaskShift;
using TaskStatus = ASMILE.BO.Core.FrontOffice.TaskShift.TaskStatus;

namespace ASMILE.BO.Application.FrontOffice.TaskShift;

public sealed record TaskAssignmentSnapshot(
    Guid TaskAssignmentId,
    string TaskNumber,
    string TaskType,
    string SourceModule,
    string Priority,
    TaskStatus Status,
    string? Department,
    ShiftType? AssignedShift,
    Guid? AssignedStaffId,
    string? AssignedStaffName,
    DateTime? DueTime,
    int? EstimatedMinutes,
    int? ActualMinutes,
    string? Description,
    DateTime CreatedAtUtc,
    IReadOnlyList<string> History);

public sealed record CreateTaskAssignmentRequest(
    string TaskType,
    string SourceModule,
    string Priority,
    string? Department,
    ShiftType? AssignedShift,
    string? Description,
    DateTime? DueTime,
    int? EstimatedMinutes);

public interface ITaskShiftService
{
    Task<IReadOnlyList<TaskAssignmentSnapshot>> QueryAsync(CancellationToken cancellationToken = default);
    Task<TaskAssignmentSnapshot> CreateAsync(CreateTaskAssignmentRequest request, CancellationToken cancellationToken = default);
    Task<TaskAssignmentSnapshot> AssignAsync(Guid taskAssignmentId, string? staffName, Guid? staffId, ShiftType? shift, CancellationToken cancellationToken = default);
    Task<TaskAssignmentSnapshot> StartAsync(Guid taskAssignmentId, CancellationToken cancellationToken = default);
    Task<TaskAssignmentSnapshot> CompleteAsync(Guid taskAssignmentId, CancellationToken cancellationToken = default);
}
