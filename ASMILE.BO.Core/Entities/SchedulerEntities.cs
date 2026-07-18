namespace ASMILE.BO.Core.Entities;

public sealed class JobDefinition : ICompanyScoped, IBranchScoped
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public string JobCode { get; set; } = string.Empty;
    public string JobName { get; set; } = string.Empty;
    public string JobType { get; set; } = string.Empty;
    public string ScheduleType { get; set; } = "Interval";
    public int? IntervalMinutes { get; set; }
    public string? CronExpression { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime? NextRunAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public sealed class JobExecution : ICompanyScoped, IBranchScoped
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public Guid JobDefinitionId { get; set; }
    public string JobCode { get; set; } = string.Empty;
    public string Status { get; set; } = "Queued";
    public DateTime QueuedAt { get; set; } = DateTime.UtcNow;
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public long DurationMs { get; set; }
    public bool CancellationRequested { get; set; }
    public string? TriggeredBy { get; set; }
    public string CorrelationId { get; set; } = Guid.NewGuid().ToString("N");
    public string? ErrorMessage { get; set; }
    public JobDefinition? JobDefinition { get; set; }
    public ICollection<JobRetry> Retries { get; set; } = new List<JobRetry>();
    public ICollection<JobLog> Logs { get; set; } = new List<JobLog>();
}

public sealed class JobRetry : ICompanyScoped, IBranchScoped
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public Guid JobExecutionId { get; set; }
    public int AttemptNo { get; set; }
    public DateTime ScheduledAt { get; set; } = DateTime.UtcNow;
    public DateTime? ExecutedAt { get; set; }
    public string Status { get; set; } = "Queued";
    public string? ErrorMessage { get; set; }
    public JobExecution? JobExecution { get; set; }
}

public sealed class JobLog : ICompanyScoped, IBranchScoped
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public Guid JobExecutionId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string Level { get; set; } = "Information";
    public string Message { get; set; } = string.Empty;
    public JobExecution? JobExecution { get; set; }
}
