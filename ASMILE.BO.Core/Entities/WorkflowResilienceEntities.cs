namespace ASMILE.BO.Core.Entities;

public sealed class WorkflowExecution : ICompanyScoped, IBranchScoped
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public Guid? ParentWorkflowExecutionId { get; set; }
    public string CorrelationId { get; set; } = string.Empty;
    public string WorkflowType { get; set; } = string.Empty;
    public string WorkflowKey { get; set; } = string.Empty;
    public string Status { get; set; } = "Started";
    public int RetryCount { get; set; }
    public DateTime StartedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAtUtc { get; set; }
    public DateTime? FailedAtUtc { get; set; }
    public DateTime LastHeartbeatUtc { get; set; } = DateTime.UtcNow;
    public string? IdempotencyKey { get; set; }
    public string ExecutionHash { get; set; } = string.Empty;
    public string PayloadHash { get; set; } = string.Empty;
    public string? RecoveryStateJson { get; set; }
    public string? CompensationStateJson { get; set; }
    public string? ExceptionType { get; set; }
    public string? ErrorMessage { get; set; }
    public string? MachineName { get; set; }
    public string? UserName { get; set; }
    public string? MetadataJson { get; set; }
    public ICollection<WorkflowExecutionStep> Steps { get; set; } = [];
    public WorkflowExecution? ParentWorkflowExecution { get; set; }
    public ICollection<WorkflowExecution> ChildWorkflowExecutions { get; set; } = [];
}

public sealed class WorkflowExecutionStep : ICompanyScoped, IBranchScoped
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid WorkflowExecutionId { get; set; }
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public string CorrelationId { get; set; } = string.Empty;
    public string StepName { get; set; } = string.Empty;
    public string Status { get; set; } = "Started";
    public int RetryCount { get; set; }
    public DateTime StartedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAtUtc { get; set; }
    public DateTime? FailedAtUtc { get; set; }
    public DateTime LastHeartbeatUtc { get; set; } = DateTime.UtcNow;
    public string? ExceptionType { get; set; }
    public string? ErrorMessage { get; set; }
    public string? MetadataJson { get; set; }
    public WorkflowExecution? WorkflowExecution { get; set; }
}

public sealed class WorkflowCompensationAction : ICompanyScoped, IBranchScoped
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid WorkflowExecutionId { get; set; }
    public Guid? WorkflowExecutionStepId { get; set; }
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public string CorrelationId { get; set; } = string.Empty;
    public string StepName { get; set; } = string.Empty;
    public string Status { get; set; } = "Registered";
    public int RetryCount { get; set; }
    public string? CompensationStateJson { get; set; }
    public string? ExceptionType { get; set; }
    public string? ErrorMessage { get; set; }
    public string? MetadataJson { get; set; }
    public DateTime StartedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAtUtc { get; set; }
    public DateTime? FailedAtUtc { get; set; }
    public WorkflowExecution? WorkflowExecution { get; set; }
    public WorkflowExecutionStep? WorkflowExecutionStep { get; set; }
}

public sealed class WorkflowRetryHistory : ICompanyScoped, IBranchScoped
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid WorkflowExecutionId { get; set; }
    public Guid? WorkflowExecutionStepId { get; set; }
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public string CorrelationId { get; set; } = string.Empty;
    public string WorkflowType { get; set; } = string.Empty;
    public string? StepName { get; set; }
    public int RetryCount { get; set; }
    public string Status { get; set; } = "Queued";
    public string? ExceptionType { get; set; }
    public string? ErrorMessage { get; set; }
    public string? MetadataJson { get; set; }
    public DateTime StartedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAtUtc { get; set; }
    public WorkflowExecution? WorkflowExecution { get; set; }
    public WorkflowExecutionStep? WorkflowExecutionStep { get; set; }
}

public sealed class WorkflowConsistencyViolation : ICompanyScoped, IBranchScoped
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? WorkflowExecutionId { get; set; }
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public string CorrelationId { get; set; } = string.Empty;
    public string WorkflowType { get; set; } = string.Empty;
    public string WorkflowKey { get; set; } = string.Empty;
    public string Severity { get; set; } = "Warning";
    public string Category { get; set; } = string.Empty;
    public string Recommendation { get; set; } = string.Empty;
    public string AffectedEntityModule { get; set; } = string.Empty;
    public string? MetadataJson { get; set; }
    public DateTime RecordedAtUtc { get; set; } = DateTime.UtcNow;
    public WorkflowExecution? WorkflowExecution { get; set; }
}

public sealed class DistributedOperationMarker : ICompanyScoped, IBranchScoped
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid WorkflowExecutionId { get; set; }
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public string CorrelationId { get; set; } = string.Empty;
    public string WorkflowType { get; set; } = string.Empty;
    public string WorkflowKey { get; set; } = string.Empty;
    public string StepName { get; set; } = string.Empty;
    public string Status { get; set; } = "Owned";
    public string MachineName { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string? MetadataJson { get; set; }
    public DateTime StartedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAtUtc { get; set; }
    public WorkflowExecution? WorkflowExecution { get; set; }
}

public sealed class IdempotencyKeyEntry : ICompanyScoped, IBranchScoped
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public string CorrelationId { get; set; } = string.Empty;
    public string WorkflowType { get; set; } = string.Empty;
    public string WorkflowKey { get; set; } = string.Empty;
    public string IdempotencyKey { get; set; } = string.Empty;
    public string ExecutionHash { get; set; } = string.Empty;
    public string PayloadHash { get; set; } = string.Empty;
    public string Status { get; set; } = "InProgress";
    public DateTime StartedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAtUtc { get; set; }
    public string? MetadataJson { get; set; }
}

public sealed class WorkflowLease : ICompanyScoped, IBranchScoped
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public Guid? WorkflowExecutionId { get; set; }
    public string CorrelationId { get; set; } = string.Empty;
    public string WorkflowType { get; set; } = string.Empty;
    public string WorkflowKey { get; set; } = string.Empty;
    public string LeaseKey { get; set; } = string.Empty;
    public string Status { get; set; } = "Leased";
    public string HolderMachineName { get; set; } = string.Empty;
    public int HolderProcessId { get; set; }
    public DateTime StartedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime LastHeartbeatUtc { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAtUtc { get; set; } = DateTime.UtcNow;
    public string? MetadataJson { get; set; }
    public WorkflowExecution? WorkflowExecution { get; set; }
}

public sealed class WorkflowDistributedLock : ICompanyScoped, IBranchScoped
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public Guid? WorkflowExecutionId { get; set; }
    public string CorrelationId { get; set; } = string.Empty;
    public string WorkflowType { get; set; } = string.Empty;
    public string WorkflowKey { get; set; } = string.Empty;
    public string LockScope { get; set; } = string.Empty;
    public string LockKey { get; set; } = string.Empty;
    public string Status { get; set; } = "Locked";
    public DateTime LockedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime LastHeartbeatUtc { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAtUtc { get; set; } = DateTime.UtcNow;
    public string HolderMachineName { get; set; } = string.Empty;
    public int HolderProcessId { get; set; }
    public string? MetadataJson { get; set; }
    public WorkflowExecution? WorkflowExecution { get; set; }
}

public sealed class WorkflowDeadLetter : ICompanyScoped, IBranchScoped
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public Guid? WorkflowExecutionId { get; set; }
    public string CorrelationId { get; set; } = string.Empty;
    public string WorkflowType { get; set; } = string.Empty;
    public string WorkflowKey { get; set; } = string.Empty;
    public string DeadLetterReason { get; set; } = string.Empty;
    public string Status { get; set; } = "DeadLettered";
    public string? PayloadJson { get; set; }
    public string? ExceptionType { get; set; }
    public string? ErrorMessage { get; set; }
    public string? MetadataJson { get; set; }
    public DateTime DeadLetteredAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? RecoveredAtUtc { get; set; }
    public WorkflowExecution? WorkflowExecution { get; set; }
}

public sealed class WorkflowRecoveryCheckpoint : ICompanyScoped, IBranchScoped
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public Guid? WorkflowExecutionId { get; set; }
    public Guid? ParentCheckpointId { get; set; }
    public string CorrelationId { get; set; } = string.Empty;
    public string WorkflowType { get; set; } = string.Empty;
    public string WorkflowKey { get; set; } = string.Empty;
    public string CheckpointName { get; set; } = string.Empty;
    public string Status { get; set; } = "Created";
    public string SnapshotJson { get; set; } = string.Empty;
    public string SnapshotHash { get; set; } = string.Empty;
    public string? MetadataJson { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? RestoredAtUtc { get; set; }
    public WorkflowExecution? WorkflowExecution { get; set; }
}

public sealed class WorkflowExecutionMetric : ICompanyScoped, IBranchScoped
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public Guid? WorkflowExecutionId { get; set; }
    public string CorrelationId { get; set; } = string.Empty;
    public string WorkflowType { get; set; } = string.Empty;
    public string WorkflowKey { get; set; } = string.Empty;
    public string MetricName { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Severity { get; set; } = "Information";
    public double MetricValue { get; set; }
    public long DurationMs { get; set; }
    public string? MetadataJson { get; set; }
    public DateTime RecordedAtUtc { get; set; } = DateTime.UtcNow;
    public WorkflowExecution? WorkflowExecution { get; set; }
}

public sealed class EnterpriseConsistencyViolation : ICompanyScoped, IBranchScoped
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public string Category { get; set; } = string.Empty;
    public string Severity { get; set; } = "Warning";
    public string Module { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public string Recommendation { get; set; } = string.Empty;
    public bool CanAutoRecover { get; set; }
    public string? MetadataJson { get; set; }
    public DateTime RecordedAtUtc { get; set; } = DateTime.UtcNow;
}
