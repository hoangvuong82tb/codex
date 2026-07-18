using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ASMILE.BO.Core.Entities;

public sealed class StockReceipt : ICompanyScoped, IBranchScoped
{
    public int Id { get; set; }
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }

    [MaxLength(60)]
    public string ReceiptNo { get; set; } = string.Empty;

    public DateTime ReceiptDate { get; set; } = DateTime.Today;

    [MaxLength(50)]
    public string SupplierCode { get; set; } = string.Empty;

    [MaxLength(255)]
    public string SupplierName { get; set; } = string.Empty;

    [MaxLength(255)]
    public string ReceiverName { get; set; } = string.Empty;

    [MaxLength(500)]
    public string Description { get; set; } = string.Empty;

    public int WarehouseId { get; set; }

    [MaxLength(50)]
    public string DebitAccountCode { get; set; } = string.Empty;

    [MaxLength(50)]
    public string CreditAccountCode { get; set; } = string.Empty;

    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalAmount { get; set; }

    public bool IsApproved { get; set; }

    public DateTime? PostedAt { get; set; }

    [MaxLength(255)]
    public string? PostedBy { get; set; }

    public Guid? JournalHeaderId { get; set; }

    [MaxLength(50)]
    public string? PostingStatus { get; set; }

    public byte[]? RowVersion { get; set; }

    public List<StockReceiptDetail> Details { get; set; } = new();
}

