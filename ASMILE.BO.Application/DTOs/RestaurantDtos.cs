namespace ASMILE.BO.Application.DTOs;

public sealed class DiningRoomDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string RoomType { get; set; } = "Standard";
    public int Capacity { get; set; }
    public string? Description { get; set; }
    public string Status { get; set; } = "Available";
    public bool IsActive { get; set; } = true;
    public int TableCount { get; set; }
    public string BookingSummaryText { get; set; } = string.Empty;
    public string ServingSummaryText { get; set; } = string.Empty;
}

public sealed class DiningTableDto
{
    public Guid Id { get; set; }
    public Guid? OrderTableId { get; set; }
    public Guid DiningRoomId { get; set; }
    public string RoomName { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int SeatCount { get; set; }
    public decimal TableAmount { get; set; }
    public string Status { get; set; } = "Empty";
    public string? Note { get; set; }
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; }
}

public sealed class TableReservationDto
{
    public Guid Id { get; set; }
    public string ReservationNo { get; set; } = string.Empty;
    public int? CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public string Phone { get; set; } = string.Empty;
    public DateTime ReservationDateTime { get; set; }
    public int GuestCount { get; set; }
    public Guid? DiningRoomId { get; set; }
    public string? RoomName { get; set; }
    public Guid? DiningTableId { get; set; }
    public string? TableName { get; set; }
    public Guid? QuoteId { get; set; }
    public string? QuoteNo { get; set; }
    public Guid? ApprovedOrderId { get; set; }
    public string Status { get; set; } = "Draft";
    public string? Note { get; set; }
}

public sealed class AlacarteOrderLineDto
{
    public Guid Id { get; set; }
    public Guid? OrderTableId { get; set; }
    public int LineNo { get; set; }
    public Guid MenuItemId { get; set; }
    public string MenuItemCode { get; set; } = string.Empty;
    public string MenuItemName { get; set; } = string.Empty;
    public string UnitName { get; set; } = string.Empty;
    public decimal Qty { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal VatRate { get; set; }
    public decimal VatAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal Amount { get; set; }
    public decimal SupplementQuantity { get; set; }
    public string? KitchenNote { get; set; }
    public string? SupplementNote { get; set; }
    public string? SupplementNoteRaw { get; set; }
    public string LineStatus { get; set; } = string.Empty;
    public string KitchenStatus { get; set; } = string.Empty;
    public string LineType { get; set; } = string.Empty;
    public string PricingRule { get; set; } = "Manual";
    public bool IsSupplement { get; set; }
}

public sealed class RestaurantLineIngredientAdjustmentDto
{
    public Guid RecipeLineId { get; set; }
    public int? SelectedInventoryItemId { get; set; }
    public string? SelectedItemName { get; set; }
    public decimal ConversionRate { get; set; } = 1m;
    public decimal ExtraQuantity { get; set; }
    public string? Note { get; set; }
}

public sealed class AlacarteOrderDto
{
    public Guid Id { get; set; }
    public string OrderNo { get; set; } = string.Empty;
    public Guid? DiningTableId { get; set; }
    public Guid? DiningRoomId { get; set; }
    public Guid? RestaurantQuoteId { get; set; }
    public string? TableName { get; set; }
    public string? RoomName { get; set; }
    public int? CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public string? CustomerPhone { get; set; }
    public Guid? ReservationId { get; set; }
    public string? ReservationNo { get; set; }
    public int GuestCount { get; set; }
    public string OrderType { get; set; } = "Basic";
    public string? SourceQuoteNo { get; set; }
    public DateTime OrderDate { get; set; }
    public DateTime? ArrivalDateTime { get; set; }
    public string StartTime { get; set; } = string.Empty;
    public string EndTime { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string PaymentStatus { get; set; } = string.Empty;
    public decimal SubtotalAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal VatAmount { get; set; }
    public decimal ServiceChargeAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal RemainingAmount { get; set; }
    public DateTime? KitchenSentAt { get; set; }
    public string? Note { get; set; }
    public IReadOnlyList<RestaurantOrderTableDto> Tables { get; set; } = Array.Empty<RestaurantOrderTableDto>();
    public IReadOnlyList<RestaurantServingGroupDto> ServingGroups { get; set; } = Array.Empty<RestaurantServingGroupDto>();
    public IReadOnlyList<AlacarteOrderLineDto> Lines { get; set; } = Array.Empty<AlacarteOrderLineDto>();
    public IReadOnlyList<RestaurantPreparationTicketDto> PreparationTickets { get; set; } = Array.Empty<RestaurantPreparationTicketDto>();
}

public sealed class RestaurantOrderTableDto
{
    public Guid Id { get; set; }
    public Guid? DiningTableId { get; set; }
    public Guid? QuoteTableId { get; set; }
    public int TableNo { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public int GuestCount { get; set; }
    public int SortOrder { get; set; }
    public bool IsVirtual { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Note { get; set; }
}

public sealed class RestaurantServingGroupDto
{
    public Guid Id { get; set; }
    public Guid AlacarteOrderId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string ScopeType { get; set; } = "Custom";
    public string ServingRule { get; set; } = "PerTable";
    public int SortOrder { get; set; }
    public string? Note { get; set; }
    public bool IsSystemDefined { get; set; }
    public int TableCount { get; set; }
    public int GuestCount { get; set; }
    public IReadOnlyList<RestaurantServingGroupTableDto> Tables { get; set; } = Array.Empty<RestaurantServingGroupTableDto>();
}

public sealed class RestaurantServingGroupTableDto
{
    public Guid Id { get; set; }
    public Guid RestaurantOrderTableId { get; set; }
    public Guid? DiningTableId { get; set; }
    public string TableName { get; set; } = string.Empty;
    public int GuestCount { get; set; }
    public int ExtraGuestCount { get; set; }
    public bool IsServing { get; set; }
}

public sealed class RestaurantPreparationTicketDto
{
    public Guid Id { get; set; }
    public Guid AlacarteOrderId { get; set; }
    public string TicketNo { get; set; } = string.Empty;
    public string Station { get; set; } = string.Empty;
    public string TicketType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime SentAt { get; set; }
    public IReadOnlyList<RestaurantPreparationTicketLineDto> Lines { get; set; } = Array.Empty<RestaurantPreparationTicketLineDto>();
}

public sealed class RestaurantPreparationTicketLineDto
{
    public string MenuItemName { get; set; } = string.Empty;
    public decimal Qty { get; set; }
    public decimal SupplementQuantity { get; set; }
    public string? KitchenNote { get; set; }
    public string? SupplementNote { get; set; }
}

public sealed class RestaurantDepositDto
{
    public Guid Id { get; set; }
    public string DepositNo { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public decimal Amount { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
    public string? Receiver { get; set; }
    public string? Note { get; set; }
    public string Status { get; set; } = string.Empty;
}

public sealed class RestaurantContractDto
{
    public Guid Id { get; set; }
    public string ContractNo { get; set; } = string.Empty;
    public int? CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public string? Phone { get; set; }
    public DateTime? EventDate { get; set; }
    public string? RoomName { get; set; }
    public string? EventDescription { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal DepositAmount { get; set; }
    public decimal RemainingAmount { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime? SignedDate { get; set; }
    public string? Note { get; set; }
}

public sealed class RestaurantExtraServiceDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string ServiceType { get; set; } = "Other";
    public string UnitName { get; set; } = "lần";
    public decimal DefaultPrice { get; set; }
    public decimal VatRate { get; set; }
    public bool IsVatIncluded { get; set; } = true;
    public bool IsActive { get; set; } = true;
    public string? Note { get; set; }
    public string? ImagePath { get; set; }
    public string? ImageUrl { get; set; }
}

public sealed class RestaurantQuoteDto
{
    public Guid Id { get; set; }
    public string QuoteNo { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public int TableCount { get; set; }
    public int GuestCount { get; set; }
    public int TableStandard { get; set; } = 10;
    public Guid? DiningRoomId { get; set; }
    public Guid? DiningTableId { get; set; }
    public Guid? TableReservationId { get; set; }
    public Guid? ApprovedOrderId { get; set; }
    public Guid? DepositId { get; set; }
    public Guid? DepositCashTransactionId { get; set; }
    public string? DepositCashTransactionNo { get; set; }
    public string RoomName { get; set; } = string.Empty;
    public DateTime EventDate { get; set; }
    public string StartTime { get; set; } = string.Empty;
    public string EndTime { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public decimal DepositAmount { get; set; }
    public string PaymentDeadline { get; set; } = string.Empty;
    public string PaymentMethod { get; set; } = "Chuyển khoản";
    public decimal DiscountAmount { get; set; }
    public string Promotion { get; set; } = string.Empty;
    public int ValidityDays { get; set; } = 7;
    public int CancelDeadlineDays { get; set; } = 3;
    public decimal CancelFeePercent { get; set; } = 30;
    public string BankAccount { get; set; } = string.Empty;
    public string Status { get; set; } = "Nháp";
    public string Note { get; set; } = string.Empty;
    public string PreparedBy { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public IReadOnlyList<RestaurantQuoteTableDto> Tables { get; set; } = Array.Empty<RestaurantQuoteTableDto>();
    public IReadOnlyList<RestaurantQuoteLineDto> Lines { get; set; } = Array.Empty<RestaurantQuoteLineDto>();
}

public sealed class RestaurantQuoteTableDto
{
    public Guid Id { get; set; }
    public int TableNo { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public int GuestCount { get; set; }
    public int SortOrder { get; set; }
    public bool IsVirtual { get; set; }
    public string? Note { get; set; }
}

public sealed class RestaurantQuoteLineDto
{
    public Guid Id { get; set; }
    public Guid? MenuItemId { get; set; }
    public int LineNo { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = "Món ăn";
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
    public string PricingRule { get; set; } = "Manual";
    public decimal CalculatedQuantity { get; set; }
    public decimal Total { get; set; }
}

public sealed class AlacarteDashboardDto
{
    public AlacarteDashboardKpisDto Kpis { get; set; } = new();
    public IReadOnlyList<AlacarteDashboardRoomStatusDto> RoomStatuses { get; set; } = Array.Empty<AlacarteDashboardRoomStatusDto>();
    public IReadOnlyList<AlacarteDashboardServingCardDto> ServingBoard { get; set; } = Array.Empty<AlacarteDashboardServingCardDto>();
    public IReadOnlyList<AlacarteDashboardTimelineItemDto> TodayTimeline { get; set; } = Array.Empty<AlacarteDashboardTimelineItemDto>();
    public IReadOnlyList<AlacarteDashboardChartPointDto> RevenueChart { get; set; } = Array.Empty<AlacarteDashboardChartPointDto>();
    public AlacarteDashboardQuickReportsDto QuickReports { get; set; } = new();
    public IReadOnlyList<AlacarteDashboardAlertDto> Alerts { get; set; } = Array.Empty<AlacarteDashboardAlertDto>();
    public IReadOnlyList<AlacarteDashboardActivityDto> RecentActivities { get; set; } = Array.Empty<AlacarteDashboardActivityDto>();
    public int ActiveRooms => Kpis.ActiveRooms;
    public int ServingTables => Kpis.ServingTables;
    public int CurrentGuests => Kpis.CurrentGuests;
    public decimal TodayRevenue => Kpis.TodayRevenue;
    public decimal TodayDeposits => Kpis.TodayDeposits;
    public int TodayReservations => Kpis.TodayReservations;
    public int OpenOrders => Kpis.OpenOrders;
    public int PendingQuotes => Kpis.PendingQuotes;
    public int ExpectedGuests => Kpis.ExpectedGuests;
}

public sealed class AlacarteDashboardKpisDto
{
    public int ActiveRooms { get; set; }
    public int ServingTables { get; set; }
    public int CurrentGuests { get; set; }
    public decimal TodayRevenue { get; set; }
    public decimal TodayDeposits { get; set; }
    public int TodayReservations { get; set; }
    public int OpenOrders { get; set; }
    public int PendingQuotes { get; set; }
    public int ExpectedGuests { get; set; }
}

public sealed class AlacarteDashboardRoomStatusDto
{
    public Guid RoomId { get; set; }
    public string RoomCode { get; set; } = string.Empty;
    public string RoomName { get; set; } = string.Empty;
    public string AreaName { get; set; } = string.Empty;
    public int TotalTables { get; set; }
    public int ServingTables { get; set; }
    public int CurrentGuests { get; set; }
    public decimal RevenueToday { get; set; }
    public string Status { get; set; } = "Available";
    public double OccupancyPercent { get; set; }
}

public sealed class AlacarteDashboardServingCardDto
{
    public Guid OrderId { get; set; }
    public Guid? ReservationId { get; set; }
    public Guid? QuoteId { get; set; }
    public string OrderNo { get; set; } = string.Empty;
    public string RoomName { get; set; } = string.Empty;
    public string TableLabel { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public int GuestCount { get; set; }
    public int ServingMinutes { get; set; }
    public int ItemCount { get; set; }
    public decimal TotalAmount { get; set; }
    public string Status { get; set; } = string.Empty;
    public string WarningLevel { get; set; } = "normal";
    public string Note { get; set; } = string.Empty;
}

public sealed class AlacarteDashboardTimelineItemDto
{
    public Guid ReservationId { get; set; }
    public Guid? QuoteId { get; set; }
    public Guid? ApprovedOrderId { get; set; }
    public string ReservationNo { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string RoomName { get; set; } = string.Empty;
    public string TableName { get; set; } = string.Empty;
    public int GuestCount { get; set; }
    public DateTime ReservationDateTime { get; set; }
    public string Status { get; set; } = string.Empty;
    public string TimelineState { get; set; } = "Upcoming";
}

public sealed class AlacarteDashboardChartPointDto
{
    public string Label { get; set; } = string.Empty;
    public decimal Value { get; set; }
}

public sealed class AlacarteDashboardQuickReportsDto
{
    public IReadOnlyList<AlacarteDashboardTopItemDto> TopDishes { get; set; } = Array.Empty<AlacarteDashboardTopItemDto>();
    public IReadOnlyList<AlacarteDashboardTopItemDto> TopServices { get; set; } = Array.Empty<AlacarteDashboardTopItemDto>();
    public IReadOnlyList<AlacarteDashboardTopCustomerDto> TopCustomers { get; set; } = Array.Empty<AlacarteDashboardTopCustomerDto>();
    public IReadOnlyList<AlacarteDashboardTopRoomDto> TopRooms { get; set; } = Array.Empty<AlacarteDashboardTopRoomDto>();
}

public sealed class AlacarteDashboardTopItemDto
{
    public string Name { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal Revenue { get; set; }
}

public sealed class AlacarteDashboardTopCustomerDto
{
    public string CustomerName { get; set; } = string.Empty;
    public string RoomName { get; set; } = string.Empty;
    public int BookingCount { get; set; }
    public decimal Revenue { get; set; }
}

public sealed class AlacarteDashboardTopRoomDto
{
    public string RoomName { get; set; } = string.Empty;
    public int BookingCount { get; set; }
    public decimal Revenue { get; set; }
}

public sealed class AlacarteDashboardAlertDto
{
    public string Title { get; set; } = string.Empty;
    public string Subtitle { get; set; } = string.Empty;
    public string Severity { get; set; } = "info";
    public string TargetType { get; set; } = string.Empty;
    public Guid? ReservationId { get; set; }
    public Guid? OrderId { get; set; }
    public Guid? QuoteId { get; set; }
    public DateTime? Timestamp { get; set; }
}

public sealed class AlacarteDashboardActivityDto
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ActivityType { get; set; } = "info";
    public DateTime OccurredAt { get; set; }
    public string TargetType { get; set; } = string.Empty;
    public Guid? ReservationId { get; set; }
    public Guid? OrderId { get; set; }
    public Guid? QuoteId { get; set; }
}

public sealed class SaveDiningRoomRequest
{
    public Guid? Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string RoomType { get; set; } = "Standard";
    public int Capacity { get; set; }
    public string? Description { get; set; }
    public string Status { get; set; } = "Available";
    public bool IsActive { get; set; } = true;
}

public sealed class SaveDiningTableRequest
{
    public Guid? Id { get; set; }
    public Guid DiningRoomId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int SeatCount { get; set; }
    public string Status { get; set; } = "Empty";
    public string? Note { get; set; }
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; }
}

public sealed class SaveTableReservationRequest
{
    public Guid? Id { get; set; }
    public int? CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public string Phone { get; set; } = string.Empty;
    public DateTime ReservationDateTime { get; set; }
    public int GuestCount { get; set; }
    public Guid? DiningRoomId { get; set; }
    public Guid? DiningTableId { get; set; }
    public string Status { get; set; } = "Draft";
    public string? Note { get; set; }
}

public sealed class CreateAlacarteOrderRequest
{
    public Guid? DiningTableId { get; set; }
    public Guid? DiningRoomId { get; set; }
    public int? CustomerId { get; set; }
    public Guid? ReservationId { get; set; }
    public int GuestCount { get; set; }
    public string? Note { get; set; }
    public string? CreatedBy { get; set; }
}

public sealed class CreateRoomWalkInOrderRequest
{
    public Guid DiningRoomId { get; set; }
    public int GuestCount { get; set; }
    public int TableCountToOpen { get; set; }
    public string StartTime { get; set; } = string.Empty;
    public string EndTime { get; set; } = string.Empty;
    public string? CustomerName { get; set; }
    public string Phone { get; set; } = string.Empty;
    public string? Note { get; set; }
    public string? CreatedBy { get; set; }
}

public sealed class ConfigureRoomWalkInOrderRequest
{
    public Guid OrderId { get; set; }
    public int GuestCount { get; set; }
    public int TableCountToOpen { get; set; }
    public string StartTime { get; set; } = string.Empty;
    public string EndTime { get; set; } = string.Empty;
    public string? CustomerName { get; set; }
    public string Phone { get; set; } = string.Empty;
    public string? Note { get; set; }
    public string? UpdatedBy { get; set; }
}

public sealed class UpdateRoomWalkInServingPlanRequest
{
    public Guid OrderId { get; set; }
    public string? UpdatedBy { get; set; }
    public IReadOnlyList<UpdateRoomWalkInServingTableRequest> Tables { get; set; } = Array.Empty<UpdateRoomWalkInServingTableRequest>();
}

public sealed class UpdateRoomWalkInServingTableRequest
{
    public Guid OrderTableId { get; set; }
    public bool IsServing { get; set; }
    public int GuestCount { get; set; }
}

public sealed class SaveAlacarteOrderLineRequest
{
    public Guid OrderId { get; set; }
    public Guid? OrderLineId { get; set; }
    public Guid? OrderTableId { get; set; }
    public Guid MenuItemId { get; set; }
    public decimal Qty { get; set; }
    public decimal SupplementQuantity { get; set; }
    public string? KitchenNote { get; set; }
    public string? SupplementNote { get; set; }
    public string? AdjustmentReason { get; set; }
    public string? AdjustmentBy { get; set; }
}

public sealed class AddAlacarteOrderExtraServiceRequest
{
    public Guid OrderId { get; set; }
    public Guid RestaurantExtraServiceId { get; set; }
    public decimal Qty { get; set; } = 1m;
}

public sealed class PayAlacarteOrderRequest
{
    public Guid OrderId { get; set; }
    public decimal? PaidAmount { get; set; }
    public string? PaidBy { get; set; }
    public string? PaymentMethod { get; set; }
}

public sealed class CancelAlacarteOrderRequest
{
    public Guid OrderId { get; set; }
    public string? Reason { get; set; }
    public string? CancelledBy { get; set; }
}

public sealed class SaveRestaurantExtraServiceRequest
{
    public Guid? Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string ServiceType { get; set; } = "Other";
    public string UnitName { get; set; } = "lần";
    public decimal DefaultPrice { get; set; }
    public decimal VatRate { get; set; }
    public bool IsVatIncluded { get; set; } = true;
    public bool IsActive { get; set; } = true;
    public string? Note { get; set; }
    public string? ImagePath { get; set; }
    public string? ImageUrl { get; set; }
}

public sealed class SaveRestaurantContractRequest
{
    public Guid? Id { get; set; }
    public string ContractNo { get; set; } = string.Empty;
    public int? CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public string? Phone { get; set; }
    public DateTime? EventDate { get; set; }
    public string? RoomName { get; set; }
    public string? EventDescription { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal DepositAmount { get; set; }
    public string Status { get; set; } = "Draft";
    public DateTime? SignedDate { get; set; }
    public string? Note { get; set; }
}

public sealed class SendAlacartePreparationRequest
{
    public Guid OrderId { get; set; }
    public string? SentBy { get; set; }
    public string? Note { get; set; }
}

public sealed class SaveRestaurantServingGroupRequest
{
    public Guid? Id { get; set; }
    public Guid OrderId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string ScopeType { get; set; } = "Custom";
    public string ServingRule { get; set; } = "PerTable";
    public string? Note { get; set; }
    public IReadOnlyList<Guid> OrderTableIds { get; set; } = Array.Empty<Guid>();
}

public sealed class AddRestaurantDepositRequest
{
    public Guid? AlacarteOrderId { get; set; }
    public Guid? RestaurantContractId { get; set; }
    public Guid? TableReservationId { get; set; }
    public DateTime? Date { get; set; }
    public decimal Amount { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
    public Guid? CashFundId { get; set; }
    public Guid? BankAccountId { get; set; }
    public string? Receiver { get; set; }
    public string? Note { get; set; }
}

public sealed class SaveRestaurantQuoteRequest
{
    public Guid? Id { get; set; }
    public string? QuoteNo { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public int TableCount { get; set; }
    public int GuestCount { get; set; }
    public int TableStandard { get; set; } = 10;
    public Guid? DiningRoomId { get; set; }
    public Guid? DiningTableId { get; set; }
    public string RoomName { get; set; } = string.Empty;
    public DateTime EventDate { get; set; }
    public string StartTime { get; set; } = string.Empty;
    public string EndTime { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public decimal DepositAmount { get; set; }
    public string PaymentDeadline { get; set; } = string.Empty;
    public string PaymentMethod { get; set; } = "Chuyển khoản";
    public decimal DiscountAmount { get; set; }
    public string Promotion { get; set; } = string.Empty;
    public int ValidityDays { get; set; } = 7;
    public int CancelDeadlineDays { get; set; } = 3;
    public decimal CancelFeePercent { get; set; } = 30;
    public string BankAccount { get; set; } = string.Empty;
    public string Status { get; set; } = "Nháp";
    public string Note { get; set; } = string.Empty;
    public string PreparedBy { get; set; } = string.Empty;
    public IReadOnlyList<SaveRestaurantQuoteLineRequest> Lines { get; set; } = Array.Empty<SaveRestaurantQuoteLineRequest>();
}

public sealed class SaveRestaurantQuoteLineRequest
{
    public Guid? MenuItemId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = "Món ăn";
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
    public string PricingRule { get; set; } = "Manual";
    public decimal Total { get; set; }
}

public sealed class BanquetRoomDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int Capacity { get; set; }
    public int MaxTables { get; set; }
    public string? Description { get; set; }
    public string? LayoutJson { get; set; }
    public string Status { get; set; } = "Available";
    public bool IsActive { get; set; } = true;
}

public sealed class BanquetEventDto
{
    public Guid Id { get; set; }
    public string EventNo { get; set; } = string.Empty;
    public string EventType { get; set; } = "Other";
    public int? CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public string Phone { get; set; } = string.Empty;
    public DateTime EventDate { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public int GuestCount { get; set; }
    public int TableCount { get; set; }
    public int ExtraGuestCount { get; set; }
    public int ExtraTableCount { get; set; }
    public string? BrideName { get; set; }
    public string? GroomName { get; set; }
    public string? ConferenceName { get; set; }
    public string? BirthdayPersonName { get; set; }
    public DateTime? BirthdayDate { get; set; }
    public string? MeetingTopic { get; set; }
    public string? PartyTheme { get; set; }
    public string? OtherEventName { get; set; }
    public Guid? BanquetRoomId { get; set; }
    public string? RoomName { get; set; }
    public string Status { get; set; } = "Draft";
    public bool IsFinanciallyLocked { get; set; }
    public DateTime? FinanciallyLockedAt { get; set; }
    public string? FinanciallyLockedBy { get; set; }
    public byte[]? RowVersion { get; set; }
    public string StatusDisplay => Status switch
    {
        "Draft" => "Nháp",
        "Hold" => "Giữ chỗ",
        "Confirmed" => "Đã xác nhận",
        "Deposited" => "Đã đặt cọc",
        "Preparing" => "Đang chuẩn bị",
        "Completed" => "Hoàn thành",
        "Cancelled" => "Đã hủy",
        _ => Status
    };
    public string EventTypeDisplay => EventType switch
    {
        "Wedding" => "Tiệc Cưới",
        "Conference" => "Hội nghị",
        "Birthday" => "Sinh nhật",
        "Meeting" => "Họp mặt",
        "Party" => "Tiệc / Party",
        "Other" => "Sự kiện khác",
        _ => EventType
    };
    public string? Note { get; set; }
    public decimal MenuAmount { get; set; }
    public decimal ServiceAmount { get; set; }
    public decimal MenuMainAmount { get; set; }
    public decimal MenuExtraAmount { get; set; }
    public decimal ServiceBaseAmount { get; set; }
    public decimal VatAmount { get; set; }
    public decimal SurchargeAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal DepositAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal RemainingAmount { get; set; }
    public decimal IngredientEstimatedCost { get; set; }
    public decimal ServiceEstimatedCost { get; set; }
    public decimal EstimatedCost => IngredientEstimatedCost + ServiceEstimatedCost;
    public decimal EstimatedGrossProfit => TotalAmount - EstimatedCost;
    public decimal EstimatedGrossMarginRate => TotalAmount <= 0m ? 0m : EstimatedGrossProfit / TotalAmount;
    public double PaymentProgress => TotalAmount <= 0m ? 0d : Math.Clamp((double)(DepositAmount / TotalAmount * 100m), 0d, 100d);
    public string PaymentStatusDisplay => Status == "Cancelled" ? "Đã hủy"
        : DepositAmount <= 0m ? "Chưa đặt cọc"
        : RemainingAmount <= 0m ? "Đã thanh toán"
        : "Đã đặt cọc một phần";
}

public sealed class BanquetMenuPackageDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? GroupName { get; set; }
    public string PricingMode { get; set; } = "PerTable";
    public decimal Price { get; set; }
    public decimal VatRate { get; set; }
    public bool IsActive { get; set; } = true;
    public int LineCount { get; set; }
    public IReadOnlyList<BanquetMenuPackageLineDto> Lines { get; set; } = Array.Empty<BanquetMenuPackageLineDto>();
}

public sealed class BanquetMenuGroupDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class BanquetMenuPackageLineDto
{
    public Guid Id { get; set; }
    public Guid MenuItemId { get; set; }
    public int LineNo { get; set; }
    public string CourseType { get; set; } = "Main";
    public string PricingRule { get; set; } = "PerTable";
    public decimal Qty { get; set; } = 1m;
    public string MenuItemCode { get; set; } = string.Empty;
    public string MenuItemName { get; set; } = string.Empty;
    public string UnitName { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public string? Note { get; set; }
}

public sealed class BanquetServiceLineDto
{
    public int LineNo { get; set; }
    public Guid Id { get; set; }
    public Guid BanquetEventId { get; set; }
    public string ServiceType { get; set; } = "Other";
    public string ServiceTypeDisplay { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string UnitName { get; set; } = "lần";
    public decimal Qty { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal VatRate { get; set; }
    public decimal Amount { get; set; }
    public decimal CostPrice { get; set; }
    public decimal CostAmount { get; set; }
}

public sealed class BanquetContractDto
{
    public Guid Id { get; set; }
    public string ContractNo { get; set; } = string.Empty;
    public Guid BanquetEventId { get; set; }
    public DateTime? SignDate { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal DepositAmount { get; set; }
    public decimal RemainingAmount { get; set; }
    public string Status { get; set; } = "Draft";
    public string? Note { get; set; }
    public byte[]? RowVersion { get; set; }
}

public sealed class UpdateBanquetContractRequest
{
    public Guid ContractId { get; set; }
    public DateTime? SignDate { get; set; }
    public string Status { get; set; } = "Confirmed";
    public string? Note { get; set; }
}

public sealed class BanquetDepositDto
{
    public Guid Id { get; set; }
    public string DepositNo { get; set; } = string.Empty;
    public Guid BanquetEventId { get; set; }
    public Guid? BanquetContractId { get; set; }
    public DateTime DepositDate { get; set; }
    public decimal Amount { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
    public Guid? CashTransactionId { get; set; }
    public string? CashTransactionNo { get; set; }
    public decimal RefundAmount { get; set; }
    public decimal NetAmount => Math.Max(0m, Amount - RefundAmount);
    public bool CanRefund => NetAmount > 0m && Status is "Confirmed" or "PartiallyRefunded";
    public DateTime? RefundedAt { get; set; }
    public string? RefundedBy { get; set; }
    public string? RefundReason { get; set; }
    public string? RefundCashTransactionNo { get; set; }
    public byte[]? RowVersion { get; set; }
    public string PaymentMethodDisplay => PaymentMethod switch
    {
        "Cash" => "Tiền mặt",
        "Transfer" => "Chuyển khoản",
        "Card" => "Thẻ",
        "Debt" => "Công nợ",
        _ => PaymentMethod
    };
    public string? Receiver { get; set; }
    public string? Note { get; set; }
    public string Status { get; set; } = "Confirmed";
    public string StatusDisplay => Status switch
    {
        "Confirmed" => "Đã xác nhận",
        "PartiallyRefunded" => "Đã hoàn một phần",
        "Cancelled" => "Đã hủy",
        "Refunded" => "Đã hoàn tiền",
        _ => Status
    };
}

public sealed class BanquetChecklistDto
{
    public Guid Id { get; set; }
    public Guid BanquetEventId { get; set; }
    public string Title { get; set; } = string.Empty;
    public bool IsDone { get; set; }
    public DateTime? DoneAt { get; set; }
    public string? DoneBy { get; set; }
    public int SortOrder { get; set; }
    public string? Note { get; set; }
}

public sealed class BanquetDashboardDto
{
    public int TodayEvents { get; set; }
    public int ConfirmedEvents { get; set; }
    public int PreparingEvents { get; set; }
    public decimal TodayDeposits { get; set; }
    public int ExpectedGuests { get; set; }
    public int RoomConflicts { get; set; }
}

public sealed class BanquetEventMenuDto
{
    public Guid Id { get; set; }
    public Guid BanquetEventId { get; set; }
    public Guid? BanquetMenuPackageId { get; set; }
    public Guid? MenuItemId { get; set; }
    public string? ItemName { get; set; }
    public decimal Qty { get; set; }
    public string? Note { get; set; }
    public string? PackageCode { get; set; }
    public string? PackageName { get; set; }
    public string PricingMode { get; set; } = "PerTable";
    public int TableCount { get; set; }
    public int GuestCount { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Amount { get; set; }
    public IReadOnlyList<BanquetMenuPackageLineDto> PackageLines { get; set; } = Array.Empty<BanquetMenuPackageLineDto>();
}

public sealed class BanquetTimelineItemDto
{
    public Guid Id { get; set; }
    public Guid BanquetEventId { get; set; }
    public TimeSpan StartTime { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ResponsiblePerson { get; set; }
    public int SortOrder { get; set; }
    public string Status { get; set; } = "Planned";
    public bool IsDone => string.Equals(Status, "Done", StringComparison.OrdinalIgnoreCase);
    public string StatusDisplay => IsDone ? "Hoàn thành" : "Chờ thực hiện";
    public string ResponsiblePersonDisplay => string.IsNullOrWhiteSpace(ResponsiblePerson) ? "Chưa phân công" : ResponsiblePerson;
}

public sealed class BanquetEventDetailDto
{
    public BanquetEventDto Event { get; set; } = new();
    public IReadOnlyList<BanquetEventMenuDto> Menus { get; set; } = Array.Empty<BanquetEventMenuDto>();
    public IReadOnlyList<BanquetServiceLineDto> Services { get; set; } = Array.Empty<BanquetServiceLineDto>();
    public IReadOnlyList<BanquetDepositDto> Deposits { get; set; } = Array.Empty<BanquetDepositDto>();
    public IReadOnlyList<BanquetContractDto> Contracts { get; set; } = Array.Empty<BanquetContractDto>();
    public IReadOnlyList<BanquetChecklistDto> Checklist { get; set; } = Array.Empty<BanquetChecklistDto>();
    public IReadOnlyList<BanquetTimelineItemDto> Timeline { get; set; } = Array.Empty<BanquetTimelineItemDto>();
}

public sealed class BanquetQuantificationBatchDto
{
    public Guid Id { get; set; }
    public Guid BanquetEventId { get; set; }
    public int VersionNo { get; set; }
    public string Fingerprint { get; set; } = string.Empty;
    public string Status { get; set; } = "Sent";
    public DateTime SentAt { get; set; }
    public string? SentBy { get; set; }
    public DateTime? ReceivedAt { get; set; }
    public string? ReceivedBy { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? CompletedBy { get; set; }
    public string? Note { get; set; }
    public IReadOnlyList<BanquetQuantificationLineDto> Lines { get; set; } = Array.Empty<BanquetQuantificationLineDto>();
}

public sealed class BanquetFloorPlanDto
{
    public Guid Id { get; set; }
    public Guid BanquetEventId { get; set; }
    public string Name { get; set; } = string.Empty;
    public double CanvasWidth { get; set; }
    public double CanvasHeight { get; set; }
    public string? LayoutJson { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public sealed class SaveBanquetFloorPlanRequest
{
    public Guid? Id { get; set; }
    public Guid BanquetEventId { get; set; }
    public string Name { get; set; } = string.Empty;
    public double CanvasWidth { get; set; } = 760;
    public double CanvasHeight { get; set; } = 520;
    public string LayoutJson { get; set; } = "[]";
}

public sealed class BanquetQuantificationLineDto
{
    public int SortOrder { get; set; }
    public string LineType { get; set; } = "Ingredient";
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string UnitName { get; set; } = string.Empty;
    public decimal RequiredQuantity { get; set; }
    public decimal EstimatedCost { get; set; }
    public string? SourceText { get; set; }
    public string? Note { get; set; }
}

public sealed class BanquetPurchaseRequestDto
{
    public Guid Id { get; set; }
    public string RequestNo { get; set; } = string.Empty;
    public Guid BanquetEventId { get; set; }
    public Guid BanquetQuantificationBatchId { get; set; }
    public DateTime RequiredDate { get; set; }
    public string Status { get; set; } = "Requested";
    public DateTime RequestedAt { get; set; }
    public string? RequestedBy { get; set; }
    public string? Note { get; set; }
    public decimal EstimatedTotal { get; set; }
    public IReadOnlyList<BanquetPurchaseRequestLineDto> Lines { get; set; } = Array.Empty<BanquetPurchaseRequestLineDto>();
}

public sealed class BanquetPurchaseRequestLineDto
{
    public int SortOrder { get; set; }
    public string ItemCode { get; set; } = string.Empty;
    public string ItemName { get; set; } = string.Empty;
    public string UnitName { get; set; } = string.Empty;
    public decimal RequestedQuantity { get; set; }
    public decimal EstimatedCost { get; set; }
    public string? Note { get; set; }
}

public sealed class SaveBanquetQuantificationBatchRequest
{
    public Guid BanquetEventId { get; set; }
    public string Fingerprint { get; set; } = string.Empty;
    public string? SentBy { get; set; }
    public string? Note { get; set; }
    public IReadOnlyList<BanquetQuantificationLineDto> Lines { get; set; } = Array.Empty<BanquetQuantificationLineDto>();
}

public sealed class BanquetModuleSettingsDto
{
    public string VenueName { get; set; } = "Nhà hàng Tiệc (Banquet)";
    public string VenueCode { get; set; } = "BNQ001";
    public string Address { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Website { get; set; } = string.Empty;
    public string TaxCode { get; set; } = string.Empty;
    public int GuestsPerTable { get; set; } = 10;
    public decimal DefaultDepositRate { get; set; } = 30m;
    public decimal DefaultVatRate { get; set; } = 8m;
    public int HoldDays { get; set; } = 7;
    public int ReminderDays { get; set; } = 3;
    public string LunchStartTime { get; set; } = "11:00";
    public string DinnerStartTime { get; set; } = "18:00";
    public bool AutoGenerateEventNo { get; set; } = true;
    public bool WarnRoomConflict { get; set; } = true;
    public bool RequireContractBeforeConfirm { get; set; }
    public bool EnableDepositReminder { get; set; } = true;
}

public sealed class SaveBanquetRoomRequest
{
    public Guid? Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int Capacity { get; set; }
    public int MaxTables { get; set; }
    public string? Description { get; set; }
    public string? LayoutJson { get; set; }
    public string Status { get; set; } = "Available";
    public bool IsActive { get; set; } = true;
}

public sealed class SaveBanquetEventRequest
{
    public Guid? Id { get; set; }
    public byte[]? RowVersion { get; set; }
    public string EventType { get; set; } = "Other";
    public int? CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public string Phone { get; set; } = string.Empty;
    public DateTime EventDate { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public int GuestCount { get; set; }
    public int TableCount { get; set; }
    public int ExtraGuestCount { get; set; }
    public int ExtraTableCount { get; set; }
    public string? BrideName { get; set; }
    public string? GroomName { get; set; }
    public string? ConferenceName { get; set; }
    public string? BirthdayPersonName { get; set; }
    public DateTime? BirthdayDate { get; set; }
    public string? MeetingTopic { get; set; }
    public string? PartyTheme { get; set; }
    public string? OtherEventName { get; set; }
    public Guid? BanquetRoomId { get; set; }
    public string Status { get; set; } = "Draft";
    public string? Note { get; set; }
}

public sealed class SaveBanquetMenuPackageRequest
{
    public Guid? Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? GroupName { get; set; }
    public string PricingMode { get; set; } = "PerTable";
    public decimal Price { get; set; }
    public decimal VatRate { get; set; }
    public bool IsActive { get; set; } = true;
    public IReadOnlyList<SaveBanquetMenuPackageLineRequest> Lines { get; set; } = Array.Empty<SaveBanquetMenuPackageLineRequest>();
}

public sealed class SaveBanquetMenuGroupRequest
{
    public Guid? Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class SaveBanquetMenuPackageLineRequest
{
    public Guid MenuItemId { get; set; }
    public string CourseType { get; set; } = "Main";
    public string PricingRule { get; set; } = "PerTable";
    public decimal Qty { get; set; } = 1m;
    public string? Note { get; set; }
}

public sealed class AttachBanquetMenuRequest
{
    public Guid BanquetEventId { get; set; }
    public Guid BanquetMenuPackageId { get; set; }
}

public sealed class SaveBanquetCustomMenuRequest
{
    public Guid BanquetEventId { get; set; }
    public IReadOnlyList<SaveBanquetCustomMenuLineRequest> Lines { get; set; } = Array.Empty<SaveBanquetCustomMenuLineRequest>();
}

public sealed class SaveBanquetCustomMenuLineRequest
{
    public Guid MenuItemId { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public decimal Qty { get; set; }
    public decimal UnitPrice { get; set; }
    public string? Note { get; set; }
}

public sealed class SaveBanquetServiceLineRequest
{
    public Guid? Id { get; set; }
    public Guid BanquetEventId { get; set; }
    public string ServiceType { get; set; } = "Other";
    public string Description { get; set; } = string.Empty;
    public string UnitName { get; set; } = "lần";
    public decimal Qty { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal CostPrice { get; set; }
    public decimal VatRate { get; set; }
    public int? SupplierId { get; set; }
    public string? Note { get; set; }
}

public sealed class AddBanquetDepositRequest
{
    public Guid BanquetEventId { get; set; }
    public Guid? BanquetContractId { get; set; }
    public DateTime? DepositDate { get; set; }
    public decimal Amount { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
    public Guid? CashFundId { get; set; }
    public Guid? BankAccountId { get; set; }
    public string? Receiver { get; set; }
    public string? Note { get; set; }
    public string? IdempotencyKey { get; set; }
}

public sealed class RefundBanquetDepositRequest
{
    public Guid DepositId { get; set; }
    public decimal Amount { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string? RefundedBy { get; set; }
    public byte[]? RowVersion { get; set; }
}

public sealed class SaveBanquetTimelineItemRequest
{
    public Guid? Id { get; set; }
    public Guid BanquetEventId { get; set; }
    public TimeSpan StartTime { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ResponsiblePerson { get; set; }
    public int SortOrder { get; set; }
    public string Status { get; set; } = "Planned";
}

public sealed class SaveBanquetChecklistItemRequest
{
    public Guid? Id { get; set; }
    public Guid BanquetEventId { get; set; }
    public string Title { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public string? Note { get; set; }
}

public sealed class QuoteChangeRequestDto
{
    public Guid Id { get; set; }
    public Guid QuoteId { get; set; }
    public string RequestNumber { get; set; } = string.Empty;
    public int PreviousGuestCount { get; set; }
    public int NewGuestCount { get; set; }
    public int GuestCountDelta { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string RequestedBy { get; set; } = string.Empty;
    public DateTime RequestedAt { get; set; }
    public string? ReviewedBy { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public string? AppliedBy { get; set; }
    public DateTime? AppliedAt { get; set; }
    public decimal PreviousTotalAmount { get; set; }
    public decimal NewTotalAmount { get; set; }
    public decimal AmountDelta { get; set; }
    public string? AdjustmentPlanJson { get; set; }
    public Guid PreviousOrderId { get; set; }
    public Guid? NewOrderId { get; set; }
    public string? Note { get; set; }
    public IReadOnlyList<QuoteChangeRequestDetailDto> Details { get; set; } = Array.Empty<QuoteChangeRequestDetailDto>();
    public string OrderStatus { get; set; } = string.Empty;
    public bool HasSentLines { get; set; }
    public bool CanApply { get; set; }
}

public sealed class QuoteChangeRequestDetailDto
{
    public Guid Id { get; set; }
    public string ChangeType { get; set; } = "Unchanged";
    public string ItemName { get; set; } = string.Empty;
    public decimal PreviousQuantity { get; set; }
    public decimal NewQuantity { get; set; }
    public decimal QuantityDelta { get; set; }
    public decimal PreviousAmount { get; set; }
    public decimal NewAmount { get; set; }
    public decimal AmountDelta { get; set; }
    public string? TableDisplayName { get; set; }
    public bool IsAlreadySent { get; set; }
    public string? Warning { get; set; }
    public string? Note { get; set; }
}

public sealed class PreviewAdjustmentRequest
{
    public Guid QuoteId { get; set; }
    public int NewGuestCount { get; set; }
}

public sealed class PreviewAdjustmentResponse
{
    public QuoteChangeRequestDto Preview { get; set; } = new();
    public IReadOnlyList<TableChangeDto> TableChanges { get; set; } = Array.Empty<TableChangeDto>();
    public IReadOnlyList<QuoteChangeRequestDetailDto> LineChanges { get; set; } = Array.Empty<QuoteChangeRequestDetailDto>();
    public decimal TotalDelta { get; set; }
    public IReadOnlyList<string> Warnings { get; set; } = Array.Empty<string>();
    public bool RequiresFullRebuild { get; set; }
}

public sealed class TableChangeDto
{
    public string Action { get; set; } = "Keep";
    public string TableDisplayName { get; set; } = string.Empty;
    public int GuestCount { get; set; }
    public bool IsVirtual { get; set; }
}

public sealed class RequestChangeRequest
{
    public Guid QuoteId { get; set; }
    public int NewGuestCount { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string RequestedBy { get; set; } = string.Empty;
}

public sealed class ReviewChangeRequest
{
    public Guid ChangeRequestId { get; set; }
    public string ReviewedBy { get; set; } = string.Empty;
}

public sealed class ApplyChangeRequest
{
    public Guid ChangeRequestId { get; set; }
    public string AppliedBy { get; set; } = string.Empty;
}

public sealed class QuoteAuditLogDto
{
    public Guid Id { get; set; }
    public Guid QuoteId { get; set; }
    public DateTime Timestamp { get; set; }
    public string Action { get; set; } = string.Empty;
    public string ChangedBy { get; set; } = string.Empty;
    public string? FieldName { get; set; }
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public string? Note { get; set; }
}
