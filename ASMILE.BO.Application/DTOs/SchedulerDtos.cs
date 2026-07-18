namespace ASMILE.BO.Application.DTOs;

public sealed class JobDefinitionDto
{
    public Guid Id { get; set; }
    public string JobCode { get; set; } = string.Empty;
    public string JobName { get; set; } = string.Empty;
    public string JobType { get; set; } = string.Empty;
    public string ScheduleType { get; set; } = string.Empty;
    public int? IntervalMinutes { get; set; }
    public string? CronExpression { get; set; }
    public bool IsActive { get; set; }
    public DateTime? NextRunAt { get; set; }
}

public sealed class JobExecutionStatusDto
{
    public Guid Id { get; set; }
    public string JobCode { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime QueuedAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public long DurationMs { get; set; }
    public bool CancellationRequested { get; set; }
    public string? ErrorMessage { get; set; }
}
