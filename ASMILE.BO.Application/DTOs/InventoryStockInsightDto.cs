namespace ASMILE.BO.Application.DTOs;

public sealed class InventoryStockInsightDto
{
    public int ItemId { get; set; }
    public string ItemCode { get; set; } = string.Empty;
    public string ItemName { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public int WarehouseId { get; set; }
    public string WarehouseCode { get; set; } = string.Empty;
    public string WarehouseName { get; set; } = string.Empty;
    public decimal ClosingQty { get; set; }
    public decimal OpeningQty { get; set; }
    public decimal OpeningAmount { get; set; }
    public decimal InQty { get; set; }
    public decimal InAmount { get; set; }
    public decimal OutQty { get; set; }
    public decimal OutAmount { get; set; }
    public decimal PreviousImportPrice { get; set; }
    public decimal CurrentImportPrice { get; set; }
    public decimal AveragePrice { get; set; }
    public decimal ClosingAmount { get; set; }
}
