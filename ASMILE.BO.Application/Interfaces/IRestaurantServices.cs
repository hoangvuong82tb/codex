using ASMILE.BO.Application.DTOs;

namespace ASMILE.BO.Application.Interfaces;

public interface IAlacarteRestaurantService
{
    Task<AlacarteDashboardDto> GetDashboardAsync(DateTime? businessDate = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DiningRoomDto>> GetRoomsAsync(CancellationToken cancellationToken = default);
    Task<DiningRoomDto> SaveRoomAsync(SaveDiningRoomRequest request, CancellationToken cancellationToken = default);
    Task DeleteRoomAsync(Guid roomId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DiningTableDto>> GetTablesAsync(CancellationToken cancellationToken = default);
    Task<DiningTableDto> SaveTableAsync(SaveDiningTableRequest request, CancellationToken cancellationToken = default);
    Task DeleteTableAsync(Guid tableId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<string>> ValidateReservationAsync(SaveTableReservationRequest request, CancellationToken cancellationToken = default);
    Task<TableReservationDto> SaveReservationAsync(SaveTableReservationRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TableReservationDto>> GetReservationsAsync(DateTime? date = null, CancellationToken cancellationToken = default);
    Task<AlacarteOrderDto> OpenOrderAsync(CreateAlacarteOrderRequest request, CancellationToken cancellationToken = default);
    Task<AlacarteOrderDto> OpenRoomWalkInOrderAsync(CreateRoomWalkInOrderRequest request, CancellationToken cancellationToken = default);
    Task<AlacarteOrderDto> ConfigureRoomWalkInOrderAsync(ConfigureRoomWalkInOrderRequest request, CancellationToken cancellationToken = default);
    Task<AlacarteOrderDto> UpdateRoomWalkInServingPlanAsync(UpdateRoomWalkInServingPlanRequest request, CancellationToken cancellationToken = default);
    Task<AlacarteOrderDto> AddOrUpdateLineAsync(SaveAlacarteOrderLineRequest request, CancellationToken cancellationToken = default);
    Task<AlacarteOrderDto> AddExtraServiceToOrderAsync(AddAlacarteOrderExtraServiceRequest request, CancellationToken cancellationToken = default);
    Task<AlacarteOrderDto> RemoveLineAsync(Guid orderId, Guid orderLineId, bool createCancelTicket, string? cancelledBy = null, CancellationToken cancellationToken = default);
    Task<AlacarteOrderDto> PayOrderAsync(PayAlacarteOrderRequest request, CancellationToken cancellationToken = default);
    Task<AlacarteOrderDto> CancelOrderAsync(CancelAlacarteOrderRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RestaurantPreparationTicketDto>> SendToPreparationAsync(SendAlacartePreparationRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RestaurantServingGroupDto>> GetServingGroupsAsync(Guid orderId, CancellationToken cancellationToken = default);
    Task<RestaurantServingGroupDto> SaveServingGroupAsync(SaveRestaurantServingGroupRequest request, CancellationToken cancellationToken = default);
    Task DeleteServingGroupAsync(Guid servingGroupId, CancellationToken cancellationToken = default);
    Task<RestaurantDepositDto> AddDepositAsync(AddRestaurantDepositRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AlacarteOrderDto>> GetOpenOrdersAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RestaurantExtraServiceDto>> GetExtraServicesAsync(CancellationToken cancellationToken = default);
    Task<RestaurantExtraServiceDto> SaveExtraServiceAsync(SaveRestaurantExtraServiceRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RestaurantExtraServiceDto>> SeedDefaultExtraServicesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RestaurantContractDto>> GetContractsAsync(CancellationToken cancellationToken = default);
    Task<RestaurantContractDto> SaveContractAsync(SaveRestaurantContractRequest request, CancellationToken cancellationToken = default);
    Task<RestaurantContractDto> CreateContractFromQuoteAsync(Guid quoteId, string? createdBy = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RestaurantQuoteDto>> GetQuotesAsync(CancellationToken cancellationToken = default);
    Task<RestaurantQuoteDto> SaveQuoteAsync(SaveRestaurantQuoteRequest request, CancellationToken cancellationToken = default);
    Task<RestaurantQuoteDto> ApproveQuoteAsync(Guid quoteId, string? approvedBy = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<BanquetMenuPackageDto>> GetMenuPackagesAsync(CancellationToken cancellationToken = default);
    Task DeleteQuoteAsync(Guid quoteId, CancellationToken cancellationToken = default);
    Task<AlacarteOrderDto> CopyMenuFromTableAsync(Guid orderId, Guid sourceOrderTableId, Guid targetOrderTableId, bool scaleByGuestCount = true, CancellationToken cancellationToken = default);
}

public interface IBanquetRestaurantService
{
    Task<BanquetDashboardDto> GetDashboardAsync(DateTime? businessDate = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<BanquetRoomDto>> GetRoomsAsync(CancellationToken cancellationToken = default);
    Task<BanquetRoomDto> SaveRoomAsync(SaveBanquetRoomRequest request, CancellationToken cancellationToken = default);
    Task DeleteRoomAsync(Guid roomId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<BanquetEventDto>> GetEventsAsync(DateTime? fromDate = null, DateTime? toDate = null, CancellationToken cancellationToken = default);
    Task<BanquetEventDetailDto> GetEventDetailAsync(Guid banquetEventId, CancellationToken cancellationToken = default);
    Task<BanquetEventDto> SaveEventAsync(SaveBanquetEventRequest request, CancellationToken cancellationToken = default);
    Task<BanquetEventDto> UpdateEventStatusAsync(Guid banquetEventId, string status, CancellationToken cancellationToken = default);
    Task<BanquetEventDto> FinalizeSettlementAsync(Guid banquetEventId, string paymentMethod, string? receiver = null, string? note = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<string>> ValidateEventScheduleAsync(SaveBanquetEventRequest request, CancellationToken cancellationToken = default);
    Task<BanquetMenuPackageDto> SaveMenuPackageAsync(SaveBanquetMenuPackageRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<BanquetMenuPackageDto>> GetMenuPackagesAsync(CancellationToken cancellationToken = default);
    Task<BanquetMenuGroupDto> SaveMenuGroupAsync(SaveBanquetMenuGroupRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<BanquetMenuGroupDto>> GetMenuGroupsAsync(CancellationToken cancellationToken = default);
    Task<BanquetEventDto> AttachMenuPackageAsync(AttachBanquetMenuRequest request, CancellationToken cancellationToken = default);
    Task<BanquetEventDto> SaveCustomMenuAsync(SaveBanquetCustomMenuRequest request, CancellationToken cancellationToken = default);
    Task DetachMenuPackageAsync(Guid banquetEventMenuId, CancellationToken cancellationToken = default);
    Task<BanquetServiceLineDto> SaveServiceLineAsync(SaveBanquetServiceLineRequest request, CancellationToken cancellationToken = default);
    Task DeleteServiceLineAsync(Guid banquetServiceLineId, CancellationToken cancellationToken = default);
    Task<BanquetDepositDto> AddDepositAsync(AddBanquetDepositRequest request, CancellationToken cancellationToken = default);
    Task<BanquetDepositDto> RefundDepositAsync(RefundBanquetDepositRequest request, CancellationToken cancellationToken = default);
    Task<BanquetContractDto> CreateContractAsync(Guid banquetEventId, DateTime? signDate = null, CancellationToken cancellationToken = default);
    Task<BanquetContractDto> UpdateContractAsync(UpdateBanquetContractRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<BanquetChecklistDto>> GetChecklistAsync(Guid banquetEventId, CancellationToken cancellationToken = default);
    Task<BanquetChecklistDto> SaveChecklistItemAsync(SaveBanquetChecklistItemRequest request, CancellationToken cancellationToken = default);
    Task<BanquetChecklistDto> ToggleChecklistAsync(Guid checklistId, bool isDone, string? doneBy = null, CancellationToken cancellationToken = default);
    Task DeleteChecklistItemAsync(Guid checklistId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<BanquetTimelineItemDto>> GetTimelineAsync(Guid banquetEventId, CancellationToken cancellationToken = default);
    Task<BanquetTimelineItemDto> SaveTimelineItemAsync(SaveBanquetTimelineItemRequest request, CancellationToken cancellationToken = default);
    Task<BanquetTimelineItemDto> UpdateTimelineItemStatusAsync(Guid timelineItemId, string status, CancellationToken cancellationToken = default);
    Task DeleteTimelineItemAsync(Guid timelineItemId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<BanquetQuantificationBatchDto>> GetQuantificationBatchesAsync(Guid? banquetEventId = null, CancellationToken cancellationToken = default);
    Task<BanquetQuantificationBatchDto> SaveQuantificationBatchAsync(SaveBanquetQuantificationBatchRequest request, CancellationToken cancellationToken = default);
    Task<BanquetQuantificationBatchDto> UpdateQuantificationStatusAsync(Guid batchId, string status, string? handledBy = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<BanquetPurchaseRequestDto>> GetPurchaseRequestsAsync(Guid? banquetEventId = null, CancellationToken cancellationToken = default);
    Task<BanquetPurchaseRequestDto> CreatePurchaseRequestAsync(Guid quantificationBatchId, string? requestedBy = null, CancellationToken cancellationToken = default);
    Task<BanquetPurchaseRequestDto> UpdatePurchaseRequestStatusAsync(Guid requestId, string status, string? handledBy = null, CancellationToken cancellationToken = default);
    Task<BanquetModuleSettingsDto> GetModuleSettingsAsync(CancellationToken cancellationToken = default);
    Task<BanquetModuleSettingsDto> SaveModuleSettingsAsync(BanquetModuleSettingsDto request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<BanquetFloorPlanDto>> GetFloorPlansAsync(Guid banquetEventId, CancellationToken cancellationToken = default);
    Task<BanquetFloorPlanDto> SaveFloorPlanAsync(SaveBanquetFloorPlanRequest request, CancellationToken cancellationToken = default);
}

public interface IQuoteChangeService
{
    Task<PreviewAdjustmentResponse> PreviewAdjustmentAsync(PreviewAdjustmentRequest request, CancellationToken cancellationToken = default);
    Task<QuoteChangeRequestDto> RequestChangeAsync(RequestChangeRequest request, CancellationToken cancellationToken = default);
    Task<QuoteChangeRequestDto> ApproveChangeRequestAsync(ReviewChangeRequest request, CancellationToken cancellationToken = default);
    Task<QuoteChangeRequestDto> RejectChangeRequestAsync(ReviewChangeRequest request, CancellationToken cancellationToken = default);
    Task<QuoteChangeRequestDto> ApplyChangeAsync(ApplyChangeRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<QuoteChangeRequestDto>> GetChangeHistoryAsync(Guid quoteId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<QuoteAuditLogDto>> GetAuditLogsAsync(Guid quoteId, CancellationToken cancellationToken = default);
}
