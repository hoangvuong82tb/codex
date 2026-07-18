namespace ASMILE.BO.Core.Entities;

public sealed class CompliancePackage : ICompanyScoped, IBranchScoped
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public string PackageCode { get; set; } = string.Empty;
    public string StandardCode { get; set; } = "TT99";
    public string PeriodCode { get; set; } = string.Empty;
    public string Status { get; set; } = "Draft";
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAtUtc { get; set; }
    public string? CorrelationId { get; set; }
}

public sealed class ComplianceManifest : ICompanyScoped, IBranchScoped
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public Guid CompliancePackageId { get; set; }
    public string ManifestType { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public string Hash { get; set; } = string.Empty;
    public long RowCount { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

public sealed class ComplianceSubmission : ICompanyScoped, IBranchScoped
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public Guid CompliancePackageId { get; set; }
    public string SubmissionChannel { get; set; } = string.Empty;
    public string Status { get; set; } = "Prepared";
    public string? ReferenceNo { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public string? MetadataJson { get; set; }
}

public sealed class ComplianceArchive : ICompanyScoped, IBranchScoped
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public Guid CompliancePackageId { get; set; }
    public string ArchivePath { get; set; } = string.Empty;
    public string Hash { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public bool IsVerified { get; set; }
}

public sealed class ProductionCertificationReport : ICompanyScoped, IBranchScoped
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public string PhaseCode { get; set; } = "PHASE4";
    public string Status { get; set; } = "Draft";
    public DateTime GeneratedAtUtc { get; set; } = DateTime.UtcNow;
    public string ReportJson { get; set; } = "{}";
}
