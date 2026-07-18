namespace ASMILE.BO.Application.DTOs;

public sealed class TT99AccountMappingDto
{
    public Guid Id { get; set; }
    public string AccountCode { get; set; } = string.Empty;
    public string TT99Category { get; set; } = string.Empty;
    public string TT99ReportSection { get; set; } = string.Empty;
    public string TT99DeclarationGroup { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public sealed class TT99ComplianceIssueDto
{
    public Guid Id { get; set; }
    public string RuleCode { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public string Module { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? AffectedEntity { get; set; }
    public DateTime DetectedAtUtc { get; set; }
}

public sealed class TT99ComplianceSnapshotDto
{
    public Guid Id { get; set; }
    public string PeriodCode { get; set; } = string.Empty;
    public decimal ComplianceScore { get; set; }
    public string DeclarationStatus { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
    public string SnapshotHash { get; set; } = string.Empty;
}

public sealed class TT99ExportManifestDto
{
    public Guid Id { get; set; }
    public string ExportType { get; set; } = string.Empty;
    public string PeriodCode { get; set; } = string.Empty;
    public DateTime ExportedAtUtc { get; set; }
    public string Hash { get; set; } = string.Empty;
    public int RowCount { get; set; }
    public string ComplianceVersion { get; set; } = string.Empty;
}

public sealed class TT99ValidationSummaryDto
{
    public decimal ComplianceScore { get; set; }
    public int TotalIssues { get; set; }
    public int CriticalIssues { get; set; }
    public int ErrorIssues { get; set; }
    public int WarningIssues { get; set; }
    public int InfoIssues { get; set; }
}
