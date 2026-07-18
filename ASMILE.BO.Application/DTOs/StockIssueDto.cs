namespace ASMILE.BO.Application.DTOs;

public sealed class StockIssueDto
{
    public int Id { get; set; }
    public string IssueNo { get; set; } = string.Empty;
    public DateTime IssueDate { get; set; } = DateTime.Today;
    public string Description { get; set; } = string.Empty;
    public int WarehouseId { get; set; }
    public string WarehouseCode { get; set; } = string.Empty;
    public int? ToWarehouseId { get; set; }
    public string ToWarehouseCode { get; set; } = string.Empty;
    public string DepartmentCode { get; set; } = string.Empty;
    public string DebitAccountCode { get; set; } = string.Empty;
    public string CreditAccountCode { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public bool IsApproved { get; set; }
    public DateTime? PostedAt { get; set; }
    public string? PostedBy { get; set; }
    public Guid? JournalHeaderId { get; set; }
    public string? PostingStatus { get; set; }
    public bool IsPosted => JournalHeaderId.HasValue || string.Equals(PostingStatus, "Posted", StringComparison.OrdinalIgnoreCase);
    public bool IsReversed => string.Equals(PostingStatus, "Reversed", StringComparison.OrdinalIgnoreCase);
    public string WorkflowStatusText => IsReversed ? "Đã đảo" : IsPosted ? "Đã ghi sổ" : IsApproved ? "Đã duyệt" : "Chờ duyệt";
    public List<StockTransactionDetailDto> Details { get; set; } = new();
}

