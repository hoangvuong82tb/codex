using ASMILE.BO.Application.DTOs;

namespace ASMILE.BO.Application.Interfaces;

public interface IInventoryCostingService
{
    Task<InventoryCostResult> CalculateIssueCostAsync(Guid stockIssueId, CancellationToken cancellationToken = default);
    Task<InventoryCostResult> ApplyReceiptCostAsync(Guid stockReceiptId, CancellationToken cancellationToken = default);
    Task<InventoryCostResult> ApplyIssueCostAsync(Guid stockIssueId, CancellationToken cancellationToken = default);
    Task RecalculateCostAsync(Guid itemId, Guid? warehouseId, DateTime fromDate, CancellationToken cancellationToken = default);
    Task<decimal> GetMovingAverageCostAsync(Guid itemId, Guid warehouseId, DateTime asOfDate, CancellationToken cancellationToken = default);
}
