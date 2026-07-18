namespace ASMILE.BO.Core.Entities;

public sealed class RuntimeMetricSnapshot : ICompanyScoped, IBranchScoped
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public string? CorrelationId { get; set; }
    public Guid? SessionId { get; set; }
    public string Module { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Severity { get; set; } = "Information";
    public string MetricName { get; set; } = string.Empty;
    public double MetricValue { get; set; }
    public string? Unit { get; set; }
    public string? Message { get; set; }
    public string? MetadataJson { get; set; }
    public string MachineName { get; set; } = Environment.MachineName;
    public string? UserName { get; set; }
    public DateTime RecordedAtUtc { get; set; } = DateTime.UtcNow;
    public bool IsArchived { get; set; }
    public DateTime? ArchivedAtUtc { get; set; }
}

public sealed class TelemetryEvent : ICompanyScoped, IBranchScoped
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public string? CorrelationId { get; set; }
    public Guid? SessionId { get; set; }
    public string Module { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Severity { get; set; } = "Information";
    public string EventType { get; set; } = string.Empty;
    public string EventName { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? MetadataJson { get; set; }
    public string SearchText { get; set; } = string.Empty;
    public string MachineName { get; set; } = Environment.MachineName;
    public string? UserName { get; set; }
    public DateTime RecordedAtUtc { get; set; } = DateTime.UtcNow;
    public bool IsArchived { get; set; }
    public DateTime? ArchivedAtUtc { get; set; }
}

public sealed class SlowOperationLog : ICompanyScoped, IBranchScoped
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public string? CorrelationId { get; set; }
    public Guid? SessionId { get; set; }
    public string Module { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Severity { get; set; } = "Warning";
    public string OperationName { get; set; } = string.Empty;
    public long DurationMs { get; set; }
    public string? Message { get; set; }
    public string? MetadataJson { get; set; }
    public string MachineName { get; set; } = Environment.MachineName;
    public string? UserName { get; set; }
    public DateTime RecordedAtUtc { get; set; } = DateTime.UtcNow;
    public bool IsArchived { get; set; }
    public DateTime? ArchivedAtUtc { get; set; }
}

public sealed class RuntimeExceptionLog : ICompanyScoped, IBranchScoped
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public string? CorrelationId { get; set; }
    public Guid? SessionId { get; set; }
    public string Module { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Severity { get; set; } = "Error";
    public string ExceptionType { get; set; } = string.Empty;
    public string StackHash { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? MetadataJson { get; set; }
    public int OccurrenceCount { get; set; } = 1;
    public string MachineName { get; set; } = Environment.MachineName;
    public string? UserName { get; set; }
    public DateTime RecordedAtUtc { get; set; } = DateTime.UtcNow;
    public bool IsArchived { get; set; }
    public DateTime? ArchivedAtUtc { get; set; }
}

public sealed class UiResponsivenessSnapshot : ICompanyScoped, IBranchScoped
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public string? CorrelationId { get; set; }
    public Guid? SessionId { get; set; }
    public string Module { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Severity { get; set; } = "Information";
    public string OperationName { get; set; } = string.Empty;
    public long DurationMs { get; set; }
    public bool IsFrozen { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? MetadataJson { get; set; }
    public string MachineName { get; set; } = Environment.MachineName;
    public string? UserName { get; set; }
    public DateTime RecordedAtUtc { get; set; } = DateTime.UtcNow;
    public bool IsArchived { get; set; }
    public DateTime? ArchivedAtUtc { get; set; }
}

public sealed class RecoveryRecommendation : ICompanyScoped, IBranchScoped
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public string? CorrelationId { get; set; }
    public Guid? SessionId { get; set; }
    public string Module { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Severity { get; set; } = "Information";
    public string RecommendationCode { get; set; } = string.Empty;
    public string SuggestedAction { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public decimal ConfidenceScore { get; set; }
    public string? MetadataJson { get; set; }
    public string MachineName { get; set; } = Environment.MachineName;
    public string? UserName { get; set; }
    public string Status { get; set; } = "Suggested";
    public DateTime RecordedAtUtc { get; set; } = DateTime.UtcNow;
    public bool IsArchived { get; set; }
    public DateTime? ArchivedAtUtc { get; set; }
}

public sealed class SystemResourceSnapshot : ICompanyScoped, IBranchScoped
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public string? CorrelationId { get; set; }
    public Guid? SessionId { get; set; }
    public string Module { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Severity { get; set; } = "Information";
    public double CpuUsagePercent { get; set; }
    public long MemoryUsageBytes { get; set; }
    public long WorkingSetBytes { get; set; }
    public int ThreadCount { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? MetadataJson { get; set; }
    public string MachineName { get; set; } = Environment.MachineName;
    public string? UserName { get; set; }
    public DateTime RecordedAtUtc { get; set; } = DateTime.UtcNow;
    public bool IsArchived { get; set; }
    public DateTime? ArchivedAtUtc { get; set; }
}
