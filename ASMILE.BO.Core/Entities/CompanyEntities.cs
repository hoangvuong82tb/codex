namespace ASMILE.BO.Core.Entities;

public interface ICompanyScoped
{
    Guid? CompanyId { get; set; }
}

public interface IBranchScoped
{
    Guid? BranchId { get; set; }
}

public sealed class Company
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? TaxCode { get; set; }
    public string? Address { get; set; }
    public string? DefaultCurrencyCode { get; set; }
    public string? AccountingStandardProfile { get; set; }
    public string? TimeZoneId { get; set; }
    public string? Locale { get; set; }
    public string? FiscalConfigJson { get; set; }
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsArchived { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime ModifiedAtUtc { get; set; } = DateTime.UtcNow;
    public byte[]? RowVersion { get; set; }
    public ICollection<Branch> Branches { get; set; } = new List<Branch>();
    public ICollection<CompanySettings> Settings { get; set; } = new List<CompanySettings>();
    public ICollection<CompanyFiscalPeriod> FiscalPeriods { get; set; } = new List<CompanyFiscalPeriod>();
    public ICollection<CompanyFeatureFlag> FeatureFlags { get; set; } = new List<CompanyFeatureFlag>();
}

public sealed class Branch
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public Guid? ParentBranchId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Address { get; set; }
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsArchived { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime ModifiedAtUtc { get; set; } = DateTime.UtcNow;
    public byte[]? RowVersion { get; set; }
    public Company? Company { get; set; }
    public Branch? ParentBranch { get; set; }
    public ICollection<Branch> ChildBranches { get; set; } = new List<Branch>();
    public ICollection<BranchSettings> Settings { get; set; } = new List<BranchSettings>();
}

public sealed class CompanySettings : ICompanyScoped
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? CompanyId { get; set; }
    public string SettingKey { get; set; } = string.Empty;
    public string? ValueJson { get; set; }
    public string? MetadataJson { get; set; }
    public bool IsArchived { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime ModifiedAtUtc { get; set; } = DateTime.UtcNow;
    public byte[]? RowVersion { get; set; }
}

public sealed class BranchSettings : ICompanyScoped, IBranchScoped
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public string SettingKey { get; set; } = string.Empty;
    public string? ValueJson { get; set; }
    public string? MetadataJson { get; set; }
    public bool IsArchived { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime ModifiedAtUtc { get; set; } = DateTime.UtcNow;
    public byte[]? RowVersion { get; set; }
}

public sealed class CompanyFiscalPeriod : ICompanyScoped
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? CompanyId { get; set; }
    public string FiscalYearCode { get; set; } = string.Empty;
    public int PeriodNo { get; set; }
    public string PeriodCode { get; set; } = string.Empty;
    public DateTime StartDateUtc { get; set; }
    public DateTime EndDateUtc { get; set; }
    public bool IsOpen { get; set; } = true;
    public bool IsClosed { get; set; }
    public bool IsArchived { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime ModifiedAtUtc { get; set; } = DateTime.UtcNow;
    public byte[]? RowVersion { get; set; }
}

public sealed class CompanyFeatureFlag : ICompanyScoped
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? CompanyId { get; set; }
    public string FeatureKey { get; set; } = string.Empty;
    public bool IsEnabled { get; set; } = true;
    public string? ValueJson { get; set; }
    public string? MetadataJson { get; set; }
    public bool IsArchived { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime ModifiedAtUtc { get; set; } = DateTime.UtcNow;
    public byte[]? RowVersion { get; set; }
}

public sealed class UserCompanyAccess
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public Guid CompanyId { get; set; }
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsArchived { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime ModifiedAtUtc { get; set; } = DateTime.UtcNow;
    public byte[]? RowVersion { get; set; }
}

public sealed class UserBranchAccess
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public Guid CompanyId { get; set; }
    public Guid BranchId { get; set; }
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsArchived { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime ModifiedAtUtc { get; set; } = DateTime.UtcNow;
    public byte[]? RowVersion { get; set; }
}

public sealed class ActiveCompanySession : ICompanyScoped, IBranchScoped
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public Guid UserId { get; set; }
    public Guid? SessionId { get; set; }
    public string Status { get; set; } = "Active";
    public string? Source { get; set; }
    public string? MachineName { get; set; }
    public string? AppVersion { get; set; }
    public string? SessionMetadataJson { get; set; }
    public DateTime StartedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime LastActivityAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAtUtc { get; set; }
    public DateTime? ExpiresAtUtc { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsArchived { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime ModifiedAtUtc { get; set; } = DateTime.UtcNow;
    public byte[]? RowVersion { get; set; }
}
