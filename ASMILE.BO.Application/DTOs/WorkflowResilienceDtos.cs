namespace ASMILE.BO.Application.DTOs;

public sealed class WorkflowExecutionDto
{
    public Guid Id { get; set; }
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public Guid? ParentWorkflowExecutionId { get; set; }
    public string CorrelationId { get; set; } = string.Empty;
    public string WorkflowType { get; set; } = string.Empty;
    public string WorkflowKey { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int RetryCount { get; set; }
    public DateTime StartedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public DateTime? FailedAtUtc { get; set; }
    public DateTime LastHeartbeatUtc { get; set; }
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
}

public sealed class WorkflowExecutionStepDto
{
    public Guid Id { get; set; }
    public Guid WorkflowExecutionId { get; set; }
    public string CorrelationId { get; set; } = string.Empty;
    public string StepName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int RetryCount { get; set; }
    public DateTime StartedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public DateTime? FailedAtUtc { get; set; }
    public DateTime LastHeartbeatUtc { get; set; }
    public string? ExceptionType { get; set; }
    public string? ErrorMessage { get; set; }
    public string? MetadataJson { get; set; }
}

public sealed class WorkflowConsistencyViolationDto
{
    public Guid Id { get; set; }
    public Guid? WorkflowExecutionId { get; set; }
    public string CorrelationId { get; set; } = string.Empty;
    public string WorkflowType { get; set; } = string.Empty;
    public string WorkflowKey { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Recommendation { get; set; } = string.Empty;
    public string AffectedEntityModule { get; set; } = string.Empty;
    public string? MetadataJson { get; set; }
    public DateTime RecordedAtUtc { get; set; }
}
