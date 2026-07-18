using ASMILE.BO.Application.DTOs;

namespace ASMILE.BO.Application.Interfaces;

public interface IBreakfastCafeSalesService
{
    Task<IReadOnlyList<MenuItemDto>> GetMenuAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PosTableDto>> GetTablesAsync(CancellationToken cancellationToken = default);
    Task<BreakfastCafeOrderDto> OpenOrderForTableAsync(Guid posTableId, string? createdBy = null, CancellationToken cancellationToken = default);
    Task<BreakfastCafeOrderDto> CreateOrderAsync(CreateBreakfastCafeOrderRequest request, CancellationToken cancellationToken = default);
    Task<BreakfastCafeOrderDto> AddLineAsync(UpdateBreakfastCafeOrderLineRequest request, CancellationToken cancellationToken = default);
    Task<BreakfastCafeOrderDto> UpdateLineQuantityAsync(UpdateBreakfastCafeOrderLineRequest request, CancellationToken cancellationToken = default);
    Task<BreakfastCafeOrderDto> UpdateLineNoteAsync(Guid orderId, Guid orderLineId, string? note, CancellationToken cancellationToken = default);
    Task<BreakfastCafeOrderDto> RemoveLineAsync(Guid orderId, Guid orderLineId, CancellationToken cancellationToken = default);
    Task<BreakfastCafeOrderDto> ApplyDiscountAsync(Guid orderId, decimal discountAmount, CancellationToken cancellationToken = default);
    Task<List<PosKitchenTicketDto>> SendToKitchenAsync(SendBreakfastCafeKitchenRequest request, CancellationToken cancellationToken = default);
    Task<BreakfastCafeOrderDto> PayOrderAsync(PayBreakfastCafeOrderRequest request, CancellationToken cancellationToken = default);
    Task<BreakfastCafeOrderDto> CancelOrderAsync(Guid orderId, string? reason, CancellationToken cancellationToken = default);
    Task<BreakfastCafeOrderDto> UpdateOrderNoteAsync(Guid orderId, string? note, CancellationToken cancellationToken = default);
    Task<BreakfastCafeOrderDto> ChangeTableAsync(Guid orderId, Guid newPosTableId, string? createdBy = null, CancellationToken cancellationToken = default);
    Task<BreakfastCafeOrderDto> MergeOrdersAsync(Guid targetOrderId, Guid sourceOrderId, string? createdBy = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<BreakfastCafeOrderDto>> GetOpenOrdersAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<BreakfastCafeOrderDto>> GetTodayOrdersAsync(DateTime? businessDate = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<BreakfastCafeOrderDto>> GetOrdersByDateRangeAsync(DateTime fromDate, DateTime toDate, CancellationToken cancellationToken = default);
    Task<BreakfastCafeSalesSummaryDto> GetSalesSummaryAsync(DateTime? businessDate = null, CancellationToken cancellationToken = default);
    Task<BreakfastCafeOrderDto?> GetOrderDetailAsync(Guid orderId, CancellationToken cancellationToken = default);
    Task<PosTableDto> SaveTableAsync(PosTableDto dto, CancellationToken cancellationToken = default);
    Task DeleteTableAsync(Guid tableId, CancellationToken cancellationToken = default);
}
