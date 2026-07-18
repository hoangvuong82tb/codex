namespace ASMILE.BO.Application.DTOs;

public sealed class InventoryMovementFilterDto
{
    public string Keyword { get; set; } = string.Empty;
    public string WarehouseCode { get; set; } = string.Empty;
    public string ItemGroup { get; set; } = string.Empty;
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public InventoryDocumentType? DocumentType { get; set; }
    public bool LowStockOnly { get; set; }
    public bool NegativeStockOnly { get; set; }
}

public sealed class InventoryStockBalanceDto
{
    public string ProductId { get; set; } = string.Empty;
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string ItemGroup { get; set; } = string.Empty;
    public string WarehouseId { get; set; } = string.Empty;
    public string WarehouseCode { get; set; } = string.Empty;
    public string WarehouseName { get; set; } = string.Empty;
    public decimal OpeningQuantity { get; set; }
    public decimal TotalIn { get; set; }
    public decimal TotalOut { get; set; }
    public decimal ClosingQuantity { get; set; }
    public decimal MinStockThreshold { get; set; }
    public string Unit { get; set; } = string.Empty;
    public decimal AverageCost { get; set; }
    public decimal StockValue { get; set; }
    public DateTime? LastMovementDate { get; set; }
    public string Status { get; set; } = "Bình thường";

    public decimal OpeningValue => OpeningQuantity * AverageCost;
    public decimal InValue => TotalIn * AverageCost;
    public decimal OutValue => TotalOut * AverageCost;
    public decimal ClosingValue => ClosingQuantity * AverageCost;
}

public sealed class InventoryMovementDto
{
    public string Id { get; set; } = string.Empty;
    public string ProductId { get; set; } = string.Empty;
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string ItemGroup { get; set; } = string.Empty;
    public string WarehouseId { get; set; } = string.Empty;
    public string WarehouseCode { get; set; } = string.Empty;
    public string WarehouseName { get; set; } = string.Empty;
    public string DocumentNo { get; set; } = string.Empty;
    public InventoryDocumentType DocumentType { get; set; }
    public DateTime MovementDate { get; set; }
    public decimal QuantityIn { get; set; }
    public decimal QuantityOut { get; set; }
    public string Unit { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public decimal Amount { get; set; }
    public decimal BalanceQuantity { get; set; }
    public string Description { get; set; } = string.Empty;
    public string CreatedBy { get; set; } = "system";
    public string Note { get; set; } = string.Empty;

    public decimal SystemStockQuantity =>
        DocumentType switch
        {
            InventoryDocumentType.AdjustmentIn => BalanceQuantity - QuantityIn,
            InventoryDocumentType.AdjustmentOut => BalanceQuantity + QuantityOut,
            _ => BalanceQuantity
        };

    public decimal CountedStockQuantity =>
        DocumentType is InventoryDocumentType.AdjustmentIn or InventoryDocumentType.AdjustmentOut
            ? BalanceQuantity
            : BalanceQuantity;
}

public sealed class InventoryDashboardDto
{
    public decimal TotalItems { get; set; }
    public decimal TotalStock { get; set; }
    public decimal TotalInPeriod { get; set; }
    public decimal TotalOutPeriod { get; set; }
    public decimal TotalStockValue { get; set; }
    public int LowStockCount { get; set; }
    public int NegativeStockCount { get; set; }
    public int RecentMovementCount { get; set; }
    public IReadOnlyList<InventoryWarehouseSummaryDto> WarehouseSummaries { get; set; } = [];
    public IReadOnlyList<InventoryDailyFlowDto> DailyFlows { get; set; } = [];
}

public sealed class InventoryWarehouseSummaryDto
{
    public string WarehouseCode { get; set; } = string.Empty;
    public string WarehouseName { get; set; } = string.Empty;
    public decimal TotalStock { get; set; }
    public decimal TotalValue { get; set; }
}

public sealed class InventoryDailyFlowDto
{
    public DateTime Date { get; set; }
    public decimal QuantityIn { get; set; }
    public decimal QuantityOut { get; set; }
}

public sealed class InventoryDocumentDto
{
    public string DocumentNo { get; set; } = string.Empty;
    public DateTime DocumentDate { get; set; }
    public InventoryDocumentType DocumentType { get; set; }
    public string WarehouseCode { get; set; } = string.Empty;
    public string WarehouseName { get; set; } = string.Empty;
    public int TotalItems { get; set; }
    public decimal TotalQuantity { get; set; }
    public decimal TotalAmount { get; set; }
    public string Status { get; set; } = "Đã duyệt";
}

public sealed class InventoryLowStockAlertDto
{
    public string ProductId { get; set; } = string.Empty;
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string WarehouseId { get; set; } = string.Empty;
    public string WarehouseCode { get; set; } = string.Empty;
    public decimal CurrentStock { get; set; }
    public decimal MinStockThreshold { get; set; }
    public decimal MissingQuantity { get; set; }
    public string SuggestedSupplier { get; set; } = string.Empty;
    public decimal SuggestedPurchaseQty { get; set; }
    public string Severity { get; set; } = "Warning";
    public bool IsHidden { get; set; }
}
