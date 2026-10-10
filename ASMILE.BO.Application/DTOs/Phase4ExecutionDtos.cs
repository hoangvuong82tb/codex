namespace ASMILE.BO.Application.DTOs;

public sealed class ReconciliationSessionDto
{
    public Guid Id { get; set; }
    public string Module { get; set; } = string.Empty;
    public string SessionCode { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime StartedAtUtc { get; set; }
}

public sealed class ExportExecutionDto
{
    public Guid Id { get; set; }
    public string ExportCode { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime QueuedAtUtc { get; set; }
}

public sealed class ExportRetentionPolicyDto
{
    public Guid Id { get; set; }
    public string ExportCode { get; set; } = string.Empty;
    public int RetentionDays { get; set; } = 30;
    public bool IsActive { get; set; } = true;
}

public sealed class DataIntegrityIssueDto
{
    public Guid Id { get; set; }
    public string Module { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public bool IsCorrupted { get; set; }
}

public sealed class CompliancePackageDto
{
    public Guid Id { get; set; }
    public string PackageCode { get; set; } = string.Empty;
    public string StandardCode { get; set; } = string.Empty;
    public string PeriodCode { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}
