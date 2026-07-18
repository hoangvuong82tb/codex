namespace ASMILE.BO.Application.DTOs;

public sealed class FixedAssetCategoryDto
{
    public Guid Id { get; set; }
    public string CategoryCode { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public string? AssetAccountCode { get; set; }
    public string? DepreciationAccountCode { get; set; }
    public string? ExpenseAccountCode { get; set; }
    public int UsefulLifeMonthsDefault { get; set; }
    public string DepreciationMethodDefault { get; set; } = "StraightLine";
    public bool IsActive { get; set; }
}

public sealed class FixedAssetDto
{
    public Guid Id { get; set; }
    public string AssetCode { get; set; } = string.Empty;
    public string AssetName { get; set; } = string.Empty;
    public Guid CategoryId { get; set; }
    public string? CategoryCode { get; set; }
    public string? CategoryName { get; set; }
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
    public Guid? JournalHeaderId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public bool IsActive { get; set; }
}

public sealed class FixedAssetDepreciationDto
{
    public Guid Id { get; set; }
    public Guid FixedAssetId { get; set; }
    public string? AssetCode { get; set; }
    public string? AssetName { get; set; }
    public string DepreciationPeriod { get; set; } = string.Empty;
    public DateTime DepreciationDate { get; set; }
    public decimal DepreciationAmount { get; set; }
    public decimal AccumulatedAfter { get; set; }
    public decimal RemainingValueAfter { get; set; }
    public Guid? JournalHeaderId { get; set; }
    public string Status { get; set; } = string.Empty;
}

public sealed class FixedAssetScheduleDto
{
    public string Period { get; set; } = string.Empty;
    public DateTime DepreciationDate { get; set; }
    public decimal DepreciationAmount { get; set; }
    public decimal AccumulatedAfter { get; set; }
    public decimal RemainingValueAfter { get; set; }
    public string Status { get; set; } = string.Empty;
}

public sealed class FixedAssetSummaryDto
{
    public decimal TotalOriginalCost { get; set; }
    public decimal TotalAccumulatedDepreciation { get; set; }
    public decimal TotalRemainingValue { get; set; }
    public int ActiveAssetCount { get; set; }
    public int DraftAssetCount { get; set; }
    public int DisposedAssetCount { get; set; }
}

public sealed class FixedAssetAdjustmentDto
{
    public Guid Id { get; set; }
    public Guid FixedAssetId { get; set; }
    public string? AssetCode { get; set; }
    public string? AssetName { get; set; }
    public DateTime AdjustmentDate { get; set; }
    public string AdjustmentType { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string? Description { get; set; }
    public Guid? JournalHeaderId { get; set; }
}

public sealed class CreateFixedAssetRequest
{
    public string AssetCode { get; set; } = string.Empty;
    public string AssetName { get; set; } = string.Empty;
    public Guid CategoryId { get; set; }
    public string? SerialNumber { get; set; }
    public DateTime PurchaseDate { get; set; }
    public DateTime StartDepreciationDate { get; set; }
    public decimal OriginalCost { get; set; }
    public decimal SalvageValue { get; set; }
    public int? UsefulLifeMonths { get; set; }
    public string? DepreciationMethod { get; set; }
    public decimal Quantity { get; set; } = 1;
    public Guid? DepartmentId { get; set; }
    public Guid? EmployeeId { get; set; }
    public Guid? WarehouseId { get; set; }
    public Guid? SupplierId { get; set; }
    public string? Notes { get; set; }
    public bool ActivateImmediately { get; set; }
    public string? CounterAccountCode { get; set; }
    public string? CreatedBy { get; set; }
}

public sealed class AdjustFixedAssetRequest
{
    public Guid FixedAssetId { get; set; }
    public DateTime AdjustmentDate { get; set; } = DateTime.Today;
    public string AdjustmentType { get; set; } = "Increase";
    public decimal Amount { get; set; }
    public string? Description { get; set; }
    public string? AdjustedBy { get; set; }
}

public sealed class DepreciationRunRequest
{
    public string PeriodCode { get; set; } = string.Empty;
    public DateTime? DepreciationDate { get; set; }
    public Guid? FixedAssetId { get; set; }
    public bool PostImmediately { get; set; }
    public string? CreatedBy { get; set; }
}

public sealed class DisposeFixedAssetRequest
{
    public Guid FixedAssetId { get; set; }
    public DateTime DisposalDate { get; set; }
    public string DisposalType { get; set; } = "Liquidation";
    public decimal DisposalAmount { get; set; }
    public string? Notes { get; set; }
    public string? DisposedBy { get; set; }
}
