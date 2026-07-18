namespace ASMILE.BO.Application.DTOs;

public sealed class ToolAssetCategoryDto
{
    public Guid Id { get; set; }
    public string CategoryCode { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public string? ToolAccountCode { get; set; }
    public string? PrepaidExpenseAccountCode { get; set; }
    public string? ExpenseAccountCode { get; set; }
    public int DefaultAllocationMonths { get; set; }
    public bool IsActive { get; set; }
}

public sealed class ToolAssetDto
{
    public Guid Id { get; set; }
    public string ToolCode { get; set; } = string.Empty;
    public string ToolName { get; set; } = string.Empty;
    public Guid CategoryId { get; set; }
    public string? CategoryCode { get; set; }
    public string? CategoryName { get; set; }
    public DateTime PurchaseDate { get; set; }
    public decimal OriginalCost { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitCost { get; set; }
    public decimal AllocatedAmount { get; set; }
    public decimal RemainingAmount { get; set; }
    public int AllocationMonths { get; set; }
    public DateTime? StartAllocationDate { get; set; }
    public Guid? JournalHeaderId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Notes { get; set; }
}

public sealed class ToolIssueDto
{
    public Guid Id { get; set; }
    public string IssueNo { get; set; } = string.Empty;
    public DateTime IssueDate { get; set; }
    public Guid ToolAssetId { get; set; }
    public string? ToolCode { get; set; }
    public string? ToolName { get; set; }
    public decimal Quantity { get; set; }
    public decimal Amount { get; set; }
    public Guid? JournalHeaderId { get; set; }
    public string Status { get; set; } = string.Empty;
}

public sealed class ToolAllocationScheduleDto
{
    public Guid Id { get; set; }
    public Guid ToolAssetId { get; set; }
    public string? ToolCode { get; set; }
    public string? ToolName { get; set; }
    public string AllocationPeriod { get; set; } = string.Empty;
    public DateTime AllocationDate { get; set; }
    public decimal AllocationAmount { get; set; }
    public decimal AccumulatedAfter { get; set; }
    public decimal RemainingAfter { get; set; }
    public Guid? JournalHeaderId { get; set; }
    public string Status { get; set; } = string.Empty;
}

public sealed class ToolAllocationRunDto
{
    public Guid Id { get; set; }
    public string RunNo { get; set; } = string.Empty;
    public string AllocationPeriod { get; set; } = string.Empty;
    public DateTime RunDate { get; set; }
    public decimal TotalAmount { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Message { get; set; }
}

public sealed class ToolSummaryDto
{
    public int TotalToolCount { get; set; }
    public int InUseCount { get; set; }
    public int AllocatingCount { get; set; }
    public int FullyAllocatedCount { get; set; }
    public decimal TotalOriginalCost { get; set; }
    public decimal TotalAllocatedAmount { get; set; }
    public decimal TotalRemainingAmount { get; set; }
}

public sealed class CreateToolAssetRequest
{
    public string ToolCode { get; set; } = string.Empty;
    public string ToolName { get; set; } = string.Empty;
    public Guid CategoryId { get; set; }
    public DateTime PurchaseDate { get; set; }
    public decimal OriginalCost { get; set; }
    public decimal Quantity { get; set; }
    public int? AllocationMonths { get; set; }
    public Guid? DepartmentId { get; set; }
    public Guid? EmployeeId { get; set; }
    public Guid? WarehouseId { get; set; }
    public Guid? SupplierId { get; set; }
    public string? Notes { get; set; }
    public string? CreatedBy { get; set; }
}

public sealed class IssueToolRequest
{
    public Guid ToolAssetId { get; set; }
    public DateTime IssueDate { get; set; }
    public decimal Quantity { get; set; }
    public Guid? DepartmentId { get; set; }
    public Guid? EmployeeId { get; set; }
    public string? Description { get; set; }
    public string? PostedBy { get; set; }
}

public sealed class AllocationRunRequest
{
    public string AllocationPeriod { get; set; } = string.Empty;
    public DateTime? AllocationDate { get; set; }
    public Guid? ToolAssetId { get; set; }
    public string? PostedBy { get; set; }
}
