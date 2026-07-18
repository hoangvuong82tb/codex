namespace ASMILE.BO.Application.DTOs;

public sealed class FinancialTemplateDto
{
    public Guid Id { get; set; }
    public string StandardCode { get; set; } = string.Empty;
    public string TemplateCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string StatementType { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public sealed class FinancialTemplateVersionDto
{
    public Guid Id { get; set; }
    public Guid FinancialTemplateId { get; set; }
    public string VersionCode { get; set; } = string.Empty;
    public bool IsActiveVersion { get; set; }
    public DateTime EffectiveFromUtc { get; set; }
    public DateTime? EffectiveToUtc { get; set; }
}

public sealed class FinancialStatementSnapshotDto
{
    public Guid Id { get; set; }
    public Guid FinancialTemplateVersionId { get; set; }
    public string PeriodCode { get; set; } = string.Empty;
    public DateTime CapturedAtUtc { get; set; }
    public string SnapshotHash { get; set; } = string.Empty;
}
