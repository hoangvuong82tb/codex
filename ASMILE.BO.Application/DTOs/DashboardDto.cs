namespace ASMILE.BO.Application.DTOs;

public sealed class DashboardDto
{
    public int TotalItems { get; set; }
    public int TotalSuppliers { get; set; }
    public int TotalWarehouses { get; set; }
    public int ReceiptsThisMonth { get; set; }
    public int IssuesThisMonth { get; set; }
    public decimal InventoryValue { get; set; }
}

