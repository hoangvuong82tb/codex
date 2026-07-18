namespace ASMILE.BO.Application.DTOs;

public sealed class StockBalanceDto
{
    public int ItemId { get; set; }
    public string ItemCode { get; set; } = string.Empty;
    public string ItemName { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public decimal OpeningQty { get; set; }
    public decimal InQty { get; set; }
    public decimal OutQty { get; set; }
    public decimal ClosingQty { get; set; }
    public decimal AvgPrice { get; set; }
    public decimal ClosingAmount { get; set; }
}

