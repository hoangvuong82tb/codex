namespace ASMILE.BO.Core.Entities;

public sealed class ToolAssetCategory : ICompanyScoped
{
    public Guid Id { get; set; }
    public Guid? CompanyId { get; set; }
    public string CategoryCode { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public string? ToolAccountCode { get; set; }
    public string? PrepaidExpenseAccountCode { get; set; }
    public string? ExpenseAccountCode { get; set; }
    public int DefaultAllocationMonths { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public ICollection<ToolAsset> Tools { get; set; } = new List<ToolAsset>();
}

public sealed class ToolAsset : ICompanyScoped, IBranchScoped
{
    public Guid Id { get; set; }
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public string ToolCode { get; set; } = string.Empty;
    public string ToolName { get; set; } = string.Empty;
    public Guid CategoryId { get; set; }
    public DateTime PurchaseDate { get; set; }
    public decimal OriginalCost { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitCost { get; set; }
    public decimal AllocatedAmount { get; set; }
    public decimal RemainingAmount { get; set; }
    public int AllocationMonths { get; set; }
    public DateTime? StartAllocationDate { get; set; }
    public Guid? DepartmentId { get; set; }
    public Guid? EmployeeId { get; set; }
    public Guid? WarehouseId { get; set; }
    public Guid? SupplierId { get; set; }
    public Guid? JournalHeaderId { get; set; }
    public string Status { get; set; } = "Draft";
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public byte[]? RowVersion { get; set; }
    public ToolAssetCategory? Category { get; set; }
    public ICollection<ToolIssue> Issues { get; set; } = new List<ToolIssue>();
    public ICollection<ToolAllocationSchedule> AllocationSchedules { get; set; } = new List<ToolAllocationSchedule>();
}

public sealed class ToolIssue : ICompanyScoped, IBranchScoped
{
    public Guid Id { get; set; }
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public string IssueNo { get; set; } = string.Empty;
    public DateTime IssueDate { get; set; }
    public Guid ToolAssetId { get; set; }
    public decimal Quantity { get; set; }
    public decimal Amount { get; set; }
    public Guid? DepartmentId { get; set; }
    public Guid? EmployeeId { get; set; }
    public string? Description { get; set; }
    public Guid? JournalHeaderId { get; set; }
    public string Status { get; set; } = "Draft";
    public DateTime CreatedAt { get; set; }
    public DateTime? PostedAt { get; set; }
    public ToolAsset? ToolAsset { get; set; }
}

public sealed class ToolAllocationSchedule : ICompanyScoped, IBranchScoped
{
    public Guid Id { get; set; }
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public Guid ToolAssetId { get; set; }
    public string AllocationPeriod { get; set; } = string.Empty;
    public DateTime AllocationDate { get; set; }
    public decimal AllocationAmount { get; set; }
    public decimal AccumulatedAfter { get; set; }
    public decimal RemainingAfter { get; set; }
    public Guid? JournalHeaderId { get; set; }
    public string Status { get; set; } = "Draft";
    public DateTime CreatedAt { get; set; }
    public DateTime? PostedAt { get; set; }
    public ToolAsset? ToolAsset { get; set; }
}

public sealed class ToolAllocationRun : ICompanyScoped, IBranchScoped
{
    public Guid Id { get; set; }
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public string RunNo { get; set; } = string.Empty;
    public string AllocationPeriod { get; set; } = string.Empty;
    public DateTime RunDate { get; set; }
    public decimal TotalAmount { get; set; }
    public string Status { get; set; } = "Draft";
    public DateTime CreatedAt { get; set; }
    public DateTime? PostedAt { get; set; }
    public string? Message { get; set; }
}

public sealed class ToolRecovery : ICompanyScoped, IBranchScoped
{
    public Guid Id { get; set; }
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public string RecoveryNo { get; set; } = string.Empty;
    public DateTime RecoveryDate { get; set; }
    public Guid ToolAssetId { get; set; }
    public decimal Quantity { get; set; }
    public decimal Amount { get; set; }
    public string? Description { get; set; }
    public string Status { get; set; } = "Draft";
    public DateTime CreatedAt { get; set; }
    public ToolAsset? ToolAsset { get; set; }
}

public sealed class ToolDisposal : ICompanyScoped, IBranchScoped
{
    public Guid Id { get; set; }
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public string DisposalNo { get; set; } = string.Empty;
    public DateTime DisposalDate { get; set; }
    public Guid ToolAssetId { get; set; }
    public decimal Quantity { get; set; }
    public decimal RemainingAmount { get; set; }
    public string? Reason { get; set; }
    public Guid? JournalHeaderId { get; set; }
    public string Status { get; set; } = "Draft";
    public DateTime CreatedAt { get; set; }
    public ToolAsset? ToolAsset { get; set; }
}
