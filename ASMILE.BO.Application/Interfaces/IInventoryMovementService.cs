using ASMILE.BO.Application.DTOs;

namespace ASMILE.BO.Application.Interfaces;

public interface IInventoryMovementService
{
    Task<InventoryDashboardDto> GetDashboardAsync(InventoryMovementFilterDto filter, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<InventoryStockBalanceDto>> GetStockBalancesAsync(InventoryMovementFilterDto filter, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<InventoryMovementDto>> GetMovementsAsync(InventoryMovementFilterDto filter, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<InventoryMovementDto>> GetMovementsByProductAsync(string productId, string? warehouseId, DateTime? fromDate, DateTime? toDate, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<InventoryDocumentDto>> GetDocumentsAsync(InventoryMovementFilterDto filter, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<InventoryMovementDto>> GetDocumentLinesAsync(string documentNo, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<InventoryLowStockAlertDto>> GetLowStockAlertsAsync(InventoryMovementFilterDto filter, CancellationToken cancellationToken = default);
    Task RecalculateStockAsync(CancellationToken cancellationToken = default);
    Task CreateAdjustmentAsync(string productId, string warehouseId, decimal actualQuantity, string reason, CancellationToken cancellationToken = default);
    Task PrintStockCardAsync(string productId, string? warehouseId, CancellationToken cancellationToken = default);
    Task<string> ExportCsvAsync(InventoryMovementFilterDto filter, CancellationToken cancellationToken = default);
    Task ExportStockReportAsync(InventoryMovementFilterDto filter, CancellationToken cancellationToken = default);
    Task ImportFromExcelAsync(CancellationToken cancellationToken = default);
}
