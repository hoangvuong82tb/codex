namespace ASMILE.BO.Core.Entities;

public sealed class TrainingDataRun
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string OperationType { get; set; } = string.Empty;
    public string? SeedProfile { get; set; }
    public DateTime StartedAtUtc { get; set; }
    public DateTime? FinishedAtUtc { get; set; }
    public string Status { get; set; } = string.Empty;
    public Guid? RequestedByUserId { get; set; }
    public bool IsDryRun { get; set; }
    public string? SummaryJson { get; set; }
    public string? ErrorMessage { get; set; }

    public ICollection<TrainingDataRunStep> Steps { get; set; } = [];
}

public sealed class TrainingDataRunStep
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TrainingDataRunId { get; set; }
    public int StepOrder { get; set; }
    public string StepCode { get; set; } = string.Empty;
    public string StepName { get; set; } = string.Empty;
    public string? EntityName { get; set; }
    public bool Success { get; set; }
    public int BeforeCount { get; set; }
    public int DeletedCount { get; set; }
    public int AfterCount { get; set; }
    public int CreatedCount { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? ErrorMessage { get; set; }
    public DateTime StartedAtUtc { get; set; }
    public DateTime FinishedAtUtc { get; set; }

    public TrainingDataRun? TrainingDataRun { get; set; }
}

public sealed class TrainingDataMarker
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TrainingDataRunId { get; set; }
    public string Profile { get; set; } = string.Empty;
    public string EntityName { get; set; } = string.Empty;
    public string RecordId { get; set; } = string.Empty;
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
