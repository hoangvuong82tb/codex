namespace ASMILE.BO.Core.Entities;

public sealed class FixedAssetCategory : ICompanyScoped
{
    public Guid Id { get; set; }
    public Guid? CompanyId { get; set; }
    public string CategoryCode { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public string? AssetAccountCode { get; set; }
    public string? DepreciationAccountCode { get; set; }
    public string? ExpenseAccountCode { get; set; }
    public int UsefulLifeMonthsDefault { get; set; }
    public string DepreciationMethodDefault { get; set; } = "StraightLine";
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public ICollection<FixedAsset> Assets { get; set; } = new List<FixedAsset>();
}

public sealed class FixedAsset : ICompanyScoped, IBranchScoped
{
    public Guid Id { get; set; }
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public string AssetCode { get; set; } = string.Empty;
    public string AssetName { get; set; } = string.Empty;
    public Guid CategoryId { get; set; }
    public string? SerialNumber { get; set; }
    public DateTime PurchaseDate { get; set; }
    public DateTime StartDepreciationDate { get; set; }
    public decimal OriginalCost { get; set; }
    public decimal SalvageValue { get; set; }
    public int UsefulLifeMonths { get; set; }
    public string DepreciationMethod { get; set; } = "StraightLine";
    public decimal AccumulatedDepreciation { get; set; }
    public decimal RemainingValue { get; set; }
    public decimal Quantity { get; set; }
    public Guid? DepartmentId { get; set; }
    public Guid? EmployeeId { get; set; }
    public Guid? WarehouseId { get; set; }
    public Guid? SupplierId { get; set; }
    public Guid? JournalHeaderId { get; set; }
    public string Status { get; set; } = "Draft";
    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public byte[]? RowVersion { get; set; }
    public FixedAssetCategory? Category { get; set; }
    public ICollection<FixedAssetDepreciation> Depreciations { get; set; } = new List<FixedAssetDepreciation>();
    public ICollection<FixedAssetAdjustment> Adjustments { get; set; } = new List<FixedAssetAdjustment>();
    public ICollection<FixedAssetDisposal> Disposals { get; set; } = new List<FixedAssetDisposal>();
}

public sealed class FixedAssetDepreciation : ICompanyScoped, IBranchScoped
{
    public Guid Id { get; set; }
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public Guid FixedAssetId { get; set; }
    public string DepreciationPeriod { get; set; } = string.Empty;
    public DateTime DepreciationDate { get; set; }
    public decimal DepreciationAmount { get; set; }
    public decimal AccumulatedAfter { get; set; }
    public decimal RemainingValueAfter { get; set; }
    public Guid? JournalHeaderId { get; set; }
    public string Status { get; set; } = "Draft";
    public DateTime CreatedAt { get; set; }
    public DateTime? PostedAt { get; set; }
    public FixedAsset? FixedAsset { get; set; }
}

public sealed class FixedAssetAdjustment : ICompanyScoped, IBranchScoped
{
    public Guid Id { get; set; }
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public Guid FixedAssetId { get; set; }
    public DateTime AdjustmentDate { get; set; }
    public string AdjustmentType { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string? Description { get; set; }
    public Guid? JournalHeaderId { get; set; }
    public DateTime CreatedAt { get; set; }
    public FixedAsset? FixedAsset { get; set; }
}

public sealed class FixedAssetDisposal : ICompanyScoped, IBranchScoped
{
    public Guid Id { get; set; }
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public Guid FixedAssetId { get; set; }
    public DateTime DisposalDate { get; set; }
    public string DisposalType { get; set; } = string.Empty;
    public decimal DisposalAmount { get; set; }
    public decimal GainLossAmount { get; set; }
    public Guid? JournalHeaderId { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public FixedAsset? FixedAsset { get; set; }
}
