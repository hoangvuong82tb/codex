namespace ASMILE.BO.Application.DTOs;

public sealed class RuntimeMetricSnapshotDto
{
    public Guid Id { get; set; }
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public string? CorrelationId { get; set; }
    public Guid? SessionId { get; set; }
    public string Module { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public string MetricName { get; set; } = string.Empty;
    public double MetricValue { get; set; }
    public string? Unit { get; set; }
    public string? Message { get; set; }
    public string? MetadataJson { get; set; }
    public string MachineName { get; set; } = string.Empty;
    public string? UserName { get; set; }
    public DateTime RecordedAtUtc { get; set; }
    public bool IsArchived { get; set; }
    public DateTime? ArchivedAtUtc { get; set; }
}

public sealed class TelemetryEventDto
{
    public Guid Id { get; set; }
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public string? CorrelationId { get; set; }
    public Guid? SessionId { get; set; }
    public string Module { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public string EventName { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? MetadataJson { get; set; }
    public string SearchText { get; set; } = string.Empty;
    public string MachineName { get; set; } = string.Empty;
    public string? UserName { get; set; }
    public DateTime RecordedAtUtc { get; set; }
    public bool IsArchived { get; set; }
    public DateTime? ArchivedAtUtc { get; set; }
}

public sealed class SlowOperationLogDto
{
    public Guid Id { get; set; }
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public string? CorrelationId { get; set; }
    public Guid? SessionId { get; set; }
    public string Module { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public string OperationName { get; set; } = string.Empty;
    public long DurationMs { get; set; }
    public string? Message { get; set; }
    public string? MetadataJson { get; set; }
    public string MachineName { get; set; } = string.Empty;
    public string? UserName { get; set; }
    public DateTime RecordedAtUtc { get; set; }
    public bool IsArchived { get; set; }
    public DateTime? ArchivedAtUtc { get; set; }
}

public sealed class RuntimeExceptionLogDto
{
    public Guid Id { get; set; }
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public string? CorrelationId { get; set; }
    public Guid? SessionId { get; set; }
    public string Module { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public string ExceptionType { get; set; } = string.Empty;
    public string StackHash { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? MetadataJson { get; set; }
    public int OccurrenceCount { get; set; }
    public string MachineName { get; set; } = string.Empty;
    public string? UserName { get; set; }
    public DateTime RecordedAtUtc { get; set; }
    public bool IsArchived { get; set; }
    public DateTime? ArchivedAtUtc { get; set; }
}

public sealed class UiResponsivenessSnapshotDto
{
    public Guid Id { get; set; }
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public string? CorrelationId { get; set; }
    public Guid? SessionId { get; set; }
    public string Module { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public string OperationName { get; set; } = string.Empty;
    public long DurationMs { get; set; }
    public bool IsFrozen { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? MetadataJson { get; set; }
    public string MachineName { get; set; } = string.Empty;
    public string? UserName { get; set; }
    public DateTime RecordedAtUtc { get; set; }
    public bool IsArchived { get; set; }
    public DateTime? ArchivedAtUtc { get; set; }
}

public sealed class RecoveryRecommendationDto
{
    public Guid Id { get; set; }
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public string? CorrelationId { get; set; }
    public Guid? SessionId { get; set; }
    public string Module { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public string RecommendationCode { get; set; } = string.Empty;
    public string SuggestedAction { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public decimal ConfidenceScore { get; set; }
    public string? MetadataJson { get; set; }
    public string MachineName { get; set; } = string.Empty;
    public string? UserName { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime RecordedAtUtc { get; set; }
    public bool IsArchived { get; set; }
    public DateTime? ArchivedAtUtc { get; set; }
}

public sealed class SystemResourceSnapshotDto
{
    public Guid Id { get; set; }
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public string? CorrelationId { get; set; }
    public Guid? SessionId { get; set; }
    public string Module { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public double CpuUsagePercent { get; set; }
    public long MemoryUsageBytes { get; set; }
    public long WorkingSetBytes { get; set; }
    public int ThreadCount { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? MetadataJson { get; set; }
    public string MachineName { get; set; } = string.Empty;
    public string? UserName { get; set; }
    public DateTime RecordedAtUtc { get; set; }
    public bool IsArchived { get; set; }
    public DateTime? ArchivedAtUtc { get; set; }
}

public sealed class RuntimeTelemetryTimelineDto
{
    public Guid Id { get; set; }
    public string SourceType { get; set; } = string.Empty;
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public string? CorrelationId { get; set; }
    public Guid? SessionId { get; set; }
    public string Module { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public string EventName { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public long DurationMs { get; set; }
    public string? ExceptionType { get; set; }
    public string? StackHash { get; set; }
    public string? MetadataJson { get; set; }
    public DateTime RecordedAtUtc { get; set; }
    public bool IsArchived { get; set; }
}

public sealed class RuntimeTelemetrySummaryDto
{
    public DateTime FromUtc { get; set; }
    public DateTime ToUtc { get; set; }
    public int TotalEvents { get; set; }
    public int ExceptionCount { get; set; }
    public int SlowOperationCount { get; set; }
    public int UiLatencyCount { get; set; }
    public int ResourceSnapshotCount { get; set; }
    public int ArchivedCount { get; set; }
    public double AverageDurationMs { get; set; }
    public double P50DurationMs { get; set; }
    public double P95DurationMs { get; set; }
    public double P99DurationMs { get; set; }
    public double ThroughputPerMinute { get; set; }
    public double ErrorRate { get; set; }
    public double CancellationRate { get; set; }
    public double TimeoutRate { get; set; }
    public int LongRunningOperationCount { get; set; }
    public double AverageCpuUsagePercent { get; set; }
    public long AverageMemoryUsageBytes { get; set; }
    public long PeakMemoryUsageBytes { get; set; }
    public DateTime? LastRecordedAtUtc { get; set; }
}

public sealed class FailureAnalysisDto
{
    public string? Module { get; set; }
    public int RecurringFailureCount { get; set; }
    public int UiFreezeCount { get; set; }
    public int SlowOperationCount { get; set; }
    public IReadOnlyList<RecoveryRecommendationDto> Recommendations { get; set; } = [];
}
