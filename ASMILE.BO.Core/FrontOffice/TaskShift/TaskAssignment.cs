using ASMILE.BO.Core.FrontOffice.Foundation.Identifiers;

namespace ASMILE.BO.Core.FrontOffice.TaskShift;

public enum TaskStatus { Open, Assigned, Accepted, InProgress, OnHold, Completed, Cancelled, Overdue }
public enum ShiftType { Morning, Afternoon, Evening, Night }

public sealed class TaskAssignment
{
    private readonly List<TaskHistory> _history = [];
    private TaskAssignment() { }

    public Guid TaskAssignmentId { get; private set; }
    public Guid PropertyId { get; private set; }
    public string TaskNumber { get; private set; } = string.Empty;
    public string TaskType { get; private set; } = string.Empty;
    public string SourceModule { get; private set; } = string.Empty;
    public string Priority { get; private set; } = "Normal";
    public TaskStatus Status { get; private set; }
    public string? Department { get; private set; }
    public ShiftType? AssignedShift { get; private set; }
    public Guid? AssignedStaffId { get; private set; }
    public string? AssignedStaffName { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public Guid? CreatedBy { get; private set; }
    public DateTime? DueTime { get; private set; }
    public DateTime? StartedAtUtc { get; private set; }
    public DateTime? CompletedAtUtc { get; private set; }
    public int? EstimatedMinutes { get; private set; }
    public int? ActualMinutes { get; private set; }
    public Guid? RelatedEntityId { get; private set; }
    public string? Description { get; private set; }
    public string? Notes { get; private set; }
    public byte[]? RowVersion { get; private set; }
    public IReadOnlyList<TaskHistory> History => _history;

    public bool IsOverdue => DueTime.HasValue && DateTime.UtcNow > DueTime && Status is not TaskStatus.Completed and not TaskStatus.Cancelled;

    public static TaskAssignment Create(Guid propertyId, string taskNumber, string taskType, string sourceModule, string priority, string? department, ShiftType? shift, string? description, DateTime? dueTime, int? estimatedMinutes, DateTime nowUtc, ActorId? actorId) => new()
    { TaskAssignmentId = Uuid7.NewGuid(), PropertyId = propertyId, TaskNumber = taskNumber, TaskType = taskType, SourceModule = sourceModule, Priority = priority, Department = department, AssignedShift = shift, Description = description, DueTime = dueTime, EstimatedMinutes = estimatedMinutes, Status = TaskStatus.Open, CreatedAtUtc = nowUtc, CreatedBy = actorId?.Value };

    public void Assign(string? staffName, Guid? staffId, ShiftType? shift, DateTime nowUtc) { AssignedStaffName = staffName; AssignedStaffId = staffId; AssignedShift = shift; Status = TaskStatus.Assigned; _history.Add(TaskHistory.Record(TaskAssignmentId, PropertyId, $"Assigned to {staffName ?? "staff"}", nowUtc)); }
    public void Accept(DateTime nowUtc) { Status = TaskStatus.Accepted; _history.Add(TaskHistory.Record(TaskAssignmentId, PropertyId, "Accepted", nowUtc)); }
    public void StartWork(DateTime nowUtc) { Status = TaskStatus.InProgress; StartedAtUtc = nowUtc; _history.Add(TaskHistory.Record(TaskAssignmentId, PropertyId, "Started", nowUtc)); }
    public void Hold(string reason, DateTime nowUtc) { Status = TaskStatus.OnHold; _history.Add(TaskHistory.Record(TaskAssignmentId, PropertyId, $"On hold: {reason}", nowUtc)); }
    public void Complete(DateTime nowUtc) { Status = TaskStatus.Completed; CompletedAtUtc = nowUtc; ActualMinutes = StartedAtUtc.HasValue ? (int)(nowUtc - StartedAtUtc.Value).TotalMinutes : 0; _history.Add(TaskHistory.Record(TaskAssignmentId, PropertyId, "Completed", nowUtc)); }
    public void Cancel(string reason, DateTime nowUtc) { Status = TaskStatus.Cancelled; _history.Add(TaskHistory.Record(TaskAssignmentId, PropertyId, $"Cancelled: {reason}", nowUtc)); }

    public void CheckOverdue() { if (IsOverdue && Status != TaskStatus.Overdue) { Status = TaskStatus.Overdue; _history.Add(TaskHistory.Record(TaskAssignmentId, PropertyId, "Marked overdue", DateTime.UtcNow)); } }
}

public sealed class TaskHistory
{
    private TaskHistory() { }
    public Guid TaskHistoryId { get; private set; } public Guid TaskAssignmentId { get; private set; } public Guid PropertyId { get; private set; }
    public string Action { get; private set; } = string.Empty; public DateTime OccurredAtUtc { get; private set; }
    public static TaskHistory Record(Guid taskId, Guid propertyId, string action, DateTime nowUtc) => new() { TaskHistoryId = Uuid7.NewGuid(), TaskAssignmentId = taskId, PropertyId = propertyId, Action = action, OccurredAtUtc = nowUtc };
}
