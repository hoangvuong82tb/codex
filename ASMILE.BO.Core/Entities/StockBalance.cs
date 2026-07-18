using System.ComponentModel.DataAnnotations.Schema;

namespace ASMILE.BO.Core.Entities;

public sealed class StockBalance : ICompanyScoped, IBranchScoped
{
    public int Id { get; set; }
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public int WarehouseId { get; set; }
    public int ItemId { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal OpeningQty { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal OpeningAmount { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal InQty { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal InAmount { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal OutQty { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal OutAmount { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal ClosingQty { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal ClosingAmount { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal AvgPrice { get; set; }
}

