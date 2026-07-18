using ASMILE.BO.Application.DTOs;

namespace ASMILE.BO.Application.Interfaces;

public interface IInventoryAppService
{
    Task<IReadOnlyList<StockReceiptDto>> SearchReceiptsAsync(DateTime? fromDate, DateTime? toDate, string keyword, int? warehouseId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<StockIssueDto>> SearchIssuesAsync(DateTime? fromDate, DateTime? toDate, string keyword, int? warehouseId, CancellationToken cancellationToken = default);
    Task<StockReceiptDto> SaveReceiptAsync(StockReceiptDto dto, CancellationToken cancellationToken = default);
    Task<StockIssueDto> SaveIssueAsync(StockIssueDto dto, CancellationToken cancellationToken = default);
    Task ApproveReceiptAsync(int id, CancellationToken cancellationToken = default);
    Task ApproveIssueAsync(int id, CancellationToken cancellationToken = default);
    Task ApproveReceiptAndPostAsync(int id, string? postedBy = null, CancellationToken cancellationToken = default);
    Task ApproveIssueAndPostAsync(int id, string? postedBy = null, CancellationToken cancellationToken = default);
    Task UnapproveReceiptAsync(int id, CancellationToken cancellationToken = default);
    Task UnapproveIssueAsync(int id, CancellationToken cancellationToken = default);
    Task ReverseReceiptAsync(int id, string reason, string? reversedBy = null, CancellationToken cancellationToken = default);
    Task ReverseIssueAsync(int id, string reason, string? reversedBy = null, CancellationToken cancellationToken = default);
    Task DeleteReceiptAsync(int id, CancellationToken cancellationToken = default);
    Task DeleteIssueAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<StockBalanceDto>> GetWarehouseStockAsync(int warehouseId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<InventoryStockInsightDto>> GetItemStockInsightsAsync(int itemId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<InventoryStockInsightDto>> GetWarehouseStockInsightsAsync(int warehouseId, CancellationToken cancellationToken = default);
    Task RebuildBalancesAsync(CancellationToken cancellationToken = default);
    Task<DashboardDto> GetDashboardAsync(CancellationToken cancellationToken = default);
}

