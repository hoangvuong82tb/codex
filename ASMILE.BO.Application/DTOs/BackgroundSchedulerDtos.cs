namespace ASMILE.BO.Application.DTOs;

public sealed class BackgroundSchedulerDefinitionDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsEnabled { get; set; }
    public bool AllowConcurrentExecution { get; set; }
    public int RetryCount { get; set; }
    public int TimeoutSeconds { get; set; }
    public DateTime? NextRunAtUtc { get; set; }
    public bool IsPaused { get; set; }
}

public sealed class BackgroundSchedulerExecutionDto
{
    public Guid Id { get; set; }
    public Guid? JobDefinitionId { get; set; }
    public string JobCode { get; set; } = string.Empty;
    public string JobName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime? QueuedAtUtc { get; set; }
    public DateTime? StartedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public long DurationMs { get; set; }
    public int RetryAttempt { get; set; }
    public string TriggerSource { get; set; } = string.Empty;
    public bool CancellationRequested { get; set; }
    public string? ErrorMessage { get; set; }
}

public sealed class BackgroundSchedulerLogDto
{
    public Guid Id { get; set; }
    public Guid ExecutionId { get; set; }
    public DateTime TimestampUtc { get; set; }
    public string Severity { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? MetadataJson { get; set; }
}

public sealed class BackgroundSchedulerDurationPointDto
{
    public string Label { get; set; } = string.Empty;
    public double AverageDurationMs { get; set; }
    public int ExecutionCount { get; set; }
}

public sealed class BackgroundSchedulerFailureSummaryDto
{
    public string Status { get; set; } = string.Empty;
    public int Count { get; set; }
    public string? LatestError { get; set; }
    public DateTime? LastOccurredAtUtc { get; set; }
}

public sealed class SchedulerDashboardDto
{
    public int RunningJobs { get; set; }
    public int FailedJobs { get; set; }
    public int QueuedJobs { get; set; }
    public int NextScheduledRuns { get; set; }
    public double AverageDurationMs { get; set; }
}
