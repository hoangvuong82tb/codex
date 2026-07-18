namespace ASMILE.BO.Application.DTOs;

public sealed record ApprovalSubmitRequestDto(
    string Module,
    string EntityType,
    string EntityId,
    string Title,
    IReadOnlyList<ApprovalStepDefinitionDto> Steps,
    string? CorrelationId = null,
    string? MetadataJson = null);

public sealed record ApprovalStepDefinitionDto(
    int Level,
    string StepName,
    IReadOnlyList<Guid> AssignedUserIds,
    DateTime? DueAtUtc = null);

public sealed class ApprovalWorkflowDto
{
    public Guid Id { get; set; }
    public string Module { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int CurrentLevel { get; set; }
    public int TotalLevels { get; set; }
    public DateTime SubmittedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public string? CorrelationId { get; set; }
}

public sealed class ApprovalStepDto
{
    public Guid Id { get; set; }
    public Guid WorkflowId { get; set; }
    public int Level { get; set; }
    public string StepName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime? StartedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public DateTime? DueAtUtc { get; set; }
}

public sealed class ApprovalHistoryDto
{
    public Guid Id { get; set; }
    public Guid WorkflowId { get; set; }
    public string Action { get; set; } = string.Empty;
    public Guid? ActorUserId { get; set; }
    public DateTime OccurredAtUtc { get; set; }
    public string? Comment { get; set; }
}

public sealed class ApprovalSnapshotDto
{
    public Guid Id { get; set; }
    public Guid WorkflowId { get; set; }
    public DateTime CapturedAtUtc { get; set; }
    public string Status { get; set; } = string.Empty;
    public int CurrentLevel { get; set; }
    public string SnapshotHash { get; set; } = string.Empty;
}
