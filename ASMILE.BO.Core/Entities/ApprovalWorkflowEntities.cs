namespace ASMILE.BO.Core.Entities;

public sealed class ApprovalWorkflow : ICompanyScoped, IBranchScoped
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public string Module { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Status { get; set; } = "Draft";
    public int CurrentLevel { get; set; }
    public int TotalLevels { get; set; }
    public bool IsCompleted { get; set; }
    public bool IsRejected { get; set; }
    public DateTime SubmittedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public DateTime? LastActivityAtUtc { get; set; }
    public string? CorrelationId { get; set; }
    public string? MetadataJson { get; set; }
    public byte[]? RowVersion { get; set; }
    public ICollection<ApprovalStep> Steps { get; set; } = new List<ApprovalStep>();
    public ICollection<ApprovalHistory> History { get; set; } = new List<ApprovalHistory>();
    public ICollection<ApprovalSnapshot> Snapshots { get; set; } = new List<ApprovalSnapshot>();
}

public sealed class ApprovalStep : ICompanyScoped, IBranchScoped
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public Guid WorkflowId { get; set; }
    public int Level { get; set; }
    public string StepName { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending";
    public DateTime? StartedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public DateTime? DueAtUtc { get; set; }
    public string? Comment { get; set; }
    public string? MetadataJson { get; set; }
    public ApprovalWorkflow? Workflow { get; set; }
    public ICollection<ApprovalAssignment> Assignments { get; set; } = new List<ApprovalAssignment>();
}

public sealed class ApprovalAssignment : ICompanyScoped, IBranchScoped
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public Guid WorkflowId { get; set; }
    public Guid StepId { get; set; }
    public Guid AssignedUserId { get; set; }
    public bool IsPrimary { get; set; } = true;
    public string Status { get; set; } = "Pending";
    public DateTime AssignedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? ActedAtUtc { get; set; }
    public string? ActionComment { get; set; }
    public ApprovalWorkflow? Workflow { get; set; }
    public ApprovalStep? Step { get; set; }
}

public sealed class ApprovalDelegation : ICompanyScoped, IBranchScoped
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public Guid AssignmentId { get; set; }
    public Guid FromUserId { get; set; }
    public Guid ToUserId { get; set; }
    public DateTime DelegatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? ExpiresAtUtc { get; set; }
    public string? Reason { get; set; }
    public string Status { get; set; } = "Active";
    public string? MetadataJson { get; set; }
}

public sealed class ApprovalEscalation : ICompanyScoped, IBranchScoped
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public Guid WorkflowId { get; set; }
    public Guid StepId { get; set; }
    public Guid? AssignmentId { get; set; }
    public Guid EscalatedToUserId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public DateTime EscalatedAtUtc { get; set; } = DateTime.UtcNow;
    public bool IsResolved { get; set; }
    public DateTime? ResolvedAtUtc { get; set; }
    public string? MetadataJson { get; set; }
}

public sealed class ApprovalHistory : ICompanyScoped, IBranchScoped
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public Guid WorkflowId { get; set; }
    public Guid? StepId { get; set; }
    public Guid? AssignmentId { get; set; }
    public string Action { get; set; } = string.Empty;
    public Guid? ActorUserId { get; set; }
    public DateTime OccurredAtUtc { get; set; } = DateTime.UtcNow;
    public string? Comment { get; set; }
    public string? MetadataJson { get; set; }
    public ApprovalWorkflow? Workflow { get; set; }
    public ApprovalStep? Step { get; set; }
}

public sealed class ApprovalSnapshot : ICompanyScoped, IBranchScoped
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public Guid WorkflowId { get; set; }
    public DateTime CapturedAtUtc { get; set; } = DateTime.UtcNow;
    public string Status { get; set; } = string.Empty;
    public int CurrentLevel { get; set; }
    public string SnapshotJson { get; set; } = "{}";
    public string SnapshotHash { get; set; } = string.Empty;
    public ApprovalWorkflow? Workflow { get; set; }
}
