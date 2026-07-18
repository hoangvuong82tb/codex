using ASMILE.BO.Core.Enums;

namespace ASMILE.BO.Core.Entities;

public sealed class AccountingStandard
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public DateTime? EffectiveFrom { get; set; }
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public sealed class AccountingPeriod : ICompanyScoped
{
    public Guid Id { get; set; }
    public Guid? CompanyId { get; set; }
    public string PeriodCode { get; set; } = string.Empty;
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public AccountingPeriodStatus Status { get; set; } = AccountingPeriodStatus.Open;
    public DateTime? LockedAt { get; set; }
    public string? LockedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public sealed class FiscalYear : ICompanyScoped
{
    public Guid Id { get; set; }
    public Guid? CompanyId { get; set; }
    public int Year { get; set; }
    public string FiscalYearCode { get; set; } = string.Empty;
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public string Status { get; set; } = "Open";
    public DateTime? ClosedAt { get; set; }
    public string? ClosedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public sealed class PeriodLock : ICompanyScoped, IBranchScoped
{
    public Guid Id { get; set; }
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public string PeriodCode { get; set; } = string.Empty;
    public bool IsLocked { get; set; } = true;
    public string LockedBy { get; set; } = string.Empty;
    public DateTime LockedAt { get; set; }
    public string? LockReason { get; set; }
    public string? UnlockedBy { get; set; }
    public DateTime? UnlockedAt { get; set; }
    public string? UnlockReason { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public sealed class CompanyBranchPermission : ICompanyScoped, IBranchScoped
{
    public Guid Id { get; set; }
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public Guid UserId { get; set; }
    public string PermissionCode { get; set; } = string.Empty;
    public bool IsGranted { get; set; } = true;
    public string? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public sealed class JournalHeader : ICompanyScoped, IBranchScoped
{
    public Guid Id { get; set; }
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public string JournalNo { get; set; } = string.Empty;
    public DateTime JournalDate { get; set; }
    public DateTime PostingDate { get; set; }
    public string SourceType { get; set; } = string.Empty;
    public Guid? SourceId { get; set; }
    public string SourceNo { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string PeriodCode { get; set; } = string.Empty;
    public JournalStatus Status { get; set; } = JournalStatus.Draft;
    public decimal TotalDebit { get; set; }
    public decimal TotalCredit { get; set; }
    public decimal Difference { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string? PostedBy { get; set; }
    public DateTime? PostedAt { get; set; }
    public string? ReversedBy { get; set; }
    public DateTime? ReversedAt { get; set; }
    public byte[]? RowVersion { get; set; }
    public ICollection<JournalLine> Lines { get; set; } = new List<JournalLine>();
}

public sealed class JournalLine : ICompanyScoped, IBranchScoped
{
    public Guid Id { get; set; }
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public Guid JournalHeaderId { get; set; }
    public int LineNo { get; set; }
    public string AccountCode { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public decimal DebitAmount { get; set; }
    public decimal CreditAmount { get; set; }
    public string CurrencyCode { get; set; } = "VND";
    public decimal ExchangeRate { get; set; } = 1m;
    public string? ObjectType { get; set; }
    public Guid? ObjectId { get; set; }
    public Guid? CustomerId { get; set; }
    public Guid? SupplierId { get; set; }
    public Guid? WarehouseId { get; set; }
    public Guid? ItemId { get; set; }
    public Guid? DepartmentId { get; set; }
    public Guid? EmployeeId { get; set; }
    public string? Description { get; set; }
    public JournalHeader? JournalHeader { get; set; }
}

public sealed class PostingBatch : ICompanyScoped, IBranchScoped
{
    public Guid Id { get; set; }
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public string BatchNo { get; set; } = string.Empty;
    public string SourceType { get; set; } = string.Empty;
    public Guid? SourceId { get; set; }
    public PostingBatchStatus Status { get; set; } = PostingBatchStatus.Draft;
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}

public sealed class PostingSource : ICompanyScoped, IBranchScoped
{
    public Guid Id { get; set; }
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public string SourceType { get; set; } = string.Empty;
    public Guid SourceId { get; set; }
    public string SourceNo { get; set; } = string.Empty;
    public Guid? JournalHeaderId { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public sealed class PostingRule
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string SourceType { get; set; } = string.Empty;
    public string AccountingStandardCode { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public ICollection<PostingRuleLine> Lines { get; set; } = new List<PostingRuleLine>();
}

public sealed class PostingRuleLine
{
    public Guid Id { get; set; }
    public Guid PostingRuleId { get; set; }
    public int LineNo { get; set; }
    public DebitCreditSide DebitCredit { get; set; }
    public string AccountRole { get; set; } = string.Empty;
    public string? FixedAccountCode { get; set; }
    public string? Formula { get; set; }
    public string? DescriptionTemplate { get; set; }
    public PostingRule? PostingRule { get; set; }
}

public sealed class AccountRoleMapping
{
    public Guid Id { get; set; }
    public string AccountingStandardCode { get; set; } = string.Empty;
    public string RoleCode { get; set; } = string.Empty;
    public string AccountCode { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; }
}

public sealed class AccountTagMapping
{
    public Guid Id { get; set; }
    public string AccountCode { get; set; } = string.Empty;
    public string TagCode { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
