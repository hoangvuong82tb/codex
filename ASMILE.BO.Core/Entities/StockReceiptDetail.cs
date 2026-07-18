using System.ComponentModel.DataAnnotations.Schema;

namespace ASMILE.BO.Core.Entities;

public sealed class StockReceiptDetail : ICompanyScoped, IBranchScoped
{
    public int Id { get; set; }
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public int ReceiptId { get; set; }
    public int LineNo { get; set; }
    public int ItemId { get; set; }

    [Column(TypeName = "nvarchar(50)")]
    public string Unit { get; set; } = string.Empty;

    public int? WarehouseId { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal Quantity { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal UnitPrice { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; }

    [Column(TypeName = "decimal(5,2)")]
    public decimal VatRate { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal VatAmount { get; set; }
}

