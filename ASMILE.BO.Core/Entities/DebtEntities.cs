namespace ASMILE.BO.Core.Entities;

public sealed class PaymentAllocation : ICompanyScoped, IBranchScoped
{
    public Guid Id { get; set; }
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public string PaymentSourceType { get; set; } = string.Empty;
    public Guid? PaymentSourceId { get; set; }
    public Guid? PaymentJournalHeaderId { get; set; }
    public string TargetType { get; set; } = string.Empty;
    public Guid TargetTransactionId { get; set; }
    public decimal AllocatedAmount { get; set; }
    public DateTime AllocationDate { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
}

public sealed class DebtReconciliation : ICompanyScoped, IBranchScoped
{
    public Guid Id { get; set; }
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public string ReconciliationNo { get; set; } = string.Empty;
    public string PartnerType { get; set; } = string.Empty;
    public Guid PartnerId { get; set; }
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal TotalDebit { get; set; }
    public decimal TotalCredit { get; set; }
    public decimal Balance { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? ConfirmedAt { get; set; }
    public string? ConfirmedBy { get; set; }
    public ICollection<DebtReconciliationLine> Lines { get; set; } = new List<DebtReconciliationLine>();
}

public sealed class DebtReconciliationLine : ICompanyScoped, IBranchScoped
{
    public Guid Id { get; set; }
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public Guid DebtReconciliationId { get; set; }
    public string SourceType { get; set; } = string.Empty;
    public Guid? SourceId { get; set; }
    public string DocumentNo { get; set; } = string.Empty;
    public DateTime DocumentDate { get; set; }
    public decimal DebitAmount { get; set; }
    public decimal CreditAmount { get; set; }
    public decimal BalanceAmount { get; set; }
    public string? Description { get; set; }
    public DebtReconciliation? DebtReconciliation { get; set; }
}

public sealed class AgingSnapshot : ICompanyScoped, IBranchScoped
{
    public Guid Id { get; set; }
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public DateTime SnapshotDate { get; set; }
    public string PartnerType { get; set; } = string.Empty;
    public Guid PartnerId { get; set; }
    public decimal CurrentAmount { get; set; }
    public decimal Overdue1To30 { get; set; }
    public decimal Overdue31To60 { get; set; }
    public decimal Overdue61To90 { get; set; }
    public decimal OverdueOver90 { get; set; }
    public decimal TotalAmount { get; set; }
    public DateTime CreatedAt { get; set; }
}
