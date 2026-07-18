namespace ASMILE.BO.Application.DTOs;

public sealed class WorkflowLeaseDto
{
    public Guid Id { get; set; }
    public Guid? WorkflowExecutionId { get; set; }
    public string CorrelationId { get; set; } = string.Empty;
    public string WorkflowType { get; set; } = string.Empty;
    public string WorkflowKey { get; set; } = string.Empty;
    public string LeaseKey { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string HolderMachineName { get; set; } = string.Empty;
    public int HolderProcessId { get; set; }
    public DateTime StartedAtUtc { get; set; }
    public DateTime LastHeartbeatUtc { get; set; }
    public DateTime ExpiresAtUtc { get; set; }
    public string? MetadataJson { get; set; }
}

public sealed class WorkflowDistributedLockDto
{
    public Guid Id { get; set; }
    public Guid? WorkflowExecutionId { get; set; }
    public string CorrelationId { get; set; } = string.Empty;
    public string WorkflowType { get; set; } = string.Empty;
    public string WorkflowKey { get; set; } = string.Empty;
    public string LockScope { get; set; } = string.Empty;
    public string LockKey { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string HolderMachineName { get; set; } = string.Empty;
    public int HolderProcessId { get; set; }
    public DateTime LockedAtUtc { get; set; }
    public DateTime LastHeartbeatUtc { get; set; }
    public DateTime ExpiresAtUtc { get; set; }
    public string? MetadataJson { get; set; }
}

public sealed class WorkflowDeadLetterDto
{
    public Guid Id { get; set; }
    public Guid? WorkflowExecutionId { get; set; }
    public string CorrelationId { get; set; } = string.Empty;
    public string WorkflowType { get; set; } = string.Empty;
    public string WorkflowKey { get; set; } = string.Empty;
    public string DeadLetterReason { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? PayloadJson { get; set; }
    public string? ExceptionType { get; set; }
    public string? ErrorMessage { get; set; }
    public string? MetadataJson { get; set; }
    public DateTime DeadLetteredAtUtc { get; set; }
    public DateTime? RecoveredAtUtc { get; set; }
}

public sealed class WorkflowRecoveryCheckpointDto
{
    public Guid Id { get; set; }
    public Guid? WorkflowExecutionId { get; set; }
    public Guid? ParentCheckpointId { get; set; }
    public string CorrelationId { get; set; } = string.Empty;
    public string WorkflowType { get; set; } = string.Empty;
    public string WorkflowKey { get; set; } = string.Empty;
    public string CheckpointName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string SnapshotJson { get; set; } = string.Empty;
    public string SnapshotHash { get; set; } = string.Empty;
    public string? MetadataJson { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? RestoredAtUtc { get; set; }
}

public sealed class WorkflowExecutionMetricDto
{
    public Guid Id { get; set; }
    public Guid? WorkflowExecutionId { get; set; }
    public string CorrelationId { get; set; } = string.Empty;
    public string WorkflowType { get; set; } = string.Empty;
    public string WorkflowKey { get; set; } = string.Empty;
    public string MetricName { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public double MetricValue { get; set; }
    public long DurationMs { get; set; }
    public string? MetadataJson { get; set; }
    public DateTime RecordedAtUtc { get; set; }
}

public sealed class EnterpriseConsistencyViolationDto
{
    public Guid Id { get; set; }
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public string Category { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public string Module { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public string Recommendation { get; set; } = string.Empty;
    public bool CanAutoRecover { get; set; }
    public string? MetadataJson { get; set; }
    public DateTime RecordedAtUtc { get; set; }
}

public sealed class WorkflowReplaySessionDto
{
    public Guid WorkflowExecutionId { get; set; }
    public string CorrelationId { get; set; } = string.Empty;
    public string WorkflowType { get; set; } = string.Empty;
    public string WorkflowKey { get; set; } = string.Empty;
    public int StepCount { get; set; }
    public int MetricCount { get; set; }
    public int DeadLetterCount { get; set; }
    public IReadOnlyList<WorkflowExecutionStepDto> Steps { get; set; } = [];
    public IReadOnlyList<WorkflowExecutionMetricDto> Metrics { get; set; } = [];
}
