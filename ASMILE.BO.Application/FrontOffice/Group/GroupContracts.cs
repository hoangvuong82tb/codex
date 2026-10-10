using ASMILE.BO.Core.FrontOffice.Group;

namespace ASMILE.BO.Application.FrontOffice.Group;

// =============================================================================
// ROOM BLOCK
// =============================================================================

public enum BlockStatus { Tentative, Confirmed, Released, Expired }
public enum CutoffType { None, Automatic, Manual }

public sealed record RoomBlockDto(
    Guid BlockId, Guid GroupId, string GroupCode, string GroupName,
    Guid RoomTypeId, string RoomTypeName, int RoomsRequested, int RoomsBlocked,
    int RoomsPickedUp, int RoomsRemaining, DateOnly ArrivalDate, DateOnly DepartureDate,
    DateOnly? CutoffDate, CutoffType CutoffType, BlockStatus Status,
    decimal? NegotiatedRate, DateTime CreatedAtUtc
);

public sealed record PickupAnalysisDto(
    Guid BlockId, DateOnly Date, int TotalBlocked, int PickedUp,
    decimal PickupPercent, int Remaining, int DaysUntilArrival,
    string Trend // "OnTrack", "Behind", "Ahead", "AtRisk"
);

public sealed record RoomBlockDashboardDto(
    int TotalBlocks, int ConfirmedBlocks, int RoomsBlocked, int RoomsPickedUp,
    decimal OverallPickupPct, int CutoffsApproaching, int BlocksAtRisk,
    IReadOnlyList<RoomBlockDto> ActiveBlocks,
    IReadOnlyList<PickupAnalysisDto> PickupTrends
);

// =============================================================================
// ROOMING LIST
// =============================================================================

public sealed record RoomingListItemDto(
    Guid AssignmentId, Guid HotelRoomId, string RoomNumber, string RoomType,
    Guid? GuestId, string? GuestName, int Adults, int Children, bool IsVip,
    bool IsCheckedIn, bool IsCheckedOut, Guid? ReservationId,
    DateTime? CheckInAt, DateTime? CheckOutAt
);

public sealed record RoomingListImportResult(
    int TotalRows, int Imported, int Skipped, int Errors,
    IReadOnlyList<string> ErrorDetails
);

// =============================================================================
// BILLING
// =============================================================================

public sealed record GroupBillingSetup(
    Guid GroupId, bool UseMasterFolio, ChargeRouting DefaultRouting,
    bool AllowIndividualFolios, decimal? SplitPercentage,
    Guid? MasterFolioId, bool DepositRequired
);

public sealed record BillingSummaryDto(
    Guid GroupId, string GroupName, decimal TotalCharges, decimal TotalPayments,
    decimal TotalDeposits, decimal Outstanding, int MasterFolioCount,
    int IndividualFolioCount, bool IsBalanced
);

public sealed record GroupDepositDto(
    Guid GroupDepositId, Guid PropertyId, Guid GroupReservationId,
    string GroupCode, string GroupName, decimal Amount, string CurrencyCode,
    string PaymentMethod, string? ExternalReference, DateOnly BusinessDate,
    string? Note, GroupDepositStatus Status, DateTime CreatedAtUtc,
    DateTime? ReversedAtUtc, string? ReversalReason, byte[]? RowVersion);

public sealed record RecordGroupDepositRequest(
    Guid GroupReservationId, decimal Amount, string CurrencyCode,
    string PaymentMethod, DateOnly BusinessDate, string? ExternalReference,
    string? Note);

public sealed record ReverseGroupDepositRequest(
    Guid GroupDepositId, byte[] ExpectedRowVersion, string Reason);

public interface IGroupDepositService
{
    Task<IReadOnlyList<GroupDepositDto>> GetAsync(Guid propertyId, Guid groupReservationId, CancellationToken ct);
    Task<GroupDepositDto> RecordAsync(RecordGroupDepositRequest request, CancellationToken ct);
    Task<GroupDepositDto> ReverseAsync(ReverseGroupDepositRequest request, CancellationToken ct);
}

// =============================================================================
// GROUP DASHBOARD
// =============================================================================

public sealed record GroupDashboardDto(
    int ActiveGroups, int TotalRoomsBlocked, int TotalRoomsPickedUp,
    decimal OverallPickup, int ArrivingToday, int DepartingToday,
    decimal TotalRevenue, decimal TotalOutstanding,
    IReadOnlyList<RoomBlockDto> TopBlocks,
    IReadOnlyList<PickupAnalysisDto> AtRiskPickups
);

public sealed record GroupRevenueDashboardDto(
    Guid GroupId, string GroupName, decimal RoomRevenue, decimal FbRevenue,
    decimal OtherRevenue, decimal TotalRevenue, decimal AveragePerRoom,
    decimal RevenuePerAvailableRoom, decimal ForecastAccuracy
);

public sealed record GroupReservationSummaryDto(
    Guid GroupReservationId, Guid PropertyId, string GroupCode, string GroupName,
    string? CompanyName, string? AgencyName, string? ContactPerson, string? ContactPhone,
    string? ContactEmail, DateOnly ArrivalDate, DateOnly DepartureDate,
    int ExpectedRooms, int ConfirmedRooms, int TotalAdults, int TotalChildren,
    int CheckedInGuests, GroupReservationStatus Status, string? Source,
    string? SalesManager, string? MarketSegment, bool IsVip, ChargeRouting Routing,
    decimal TotalDeposit, decimal TotalRevenue, decimal OutstandingBalance,
    DateTime CreatedAtUtc, IReadOnlyList<RoomingListItemDto> RoomingList
);

public sealed record CreateGroupServiceDraft(
    string ServiceName, string Category, DateOnly ServiceDate, TimeOnly ServiceTime,
    int DurationMinutes, int Quantity, decimal UnitPrice, decimal DiscountRate,
    decimal TaxRate, decimal ServiceChargeRate, string ChargeTo,
    string? SubCategory = null, string? Department = null, string? Provider = null,
    string? Location = null, string? GuestName = null, string? Remark = null
);

public sealed record CreateGroupRoomBlockDraft(
    Guid RoomTypeId, int Rooms, DateOnly ArrivalDate, DateOnly DepartureDate,
    DateOnly? CutoffDate, decimal? NegotiatedRate
);

public sealed record CreateGroupRequest(
    Guid PropertyId, string? GroupCode, string GroupName,
    DateOnly ArrivalDate, DateOnly DepartureDate, int ExpectedRooms,
    int TotalAdults, int TotalChildren, string? CompanyName,
    string? AgencyName, string? ContactPerson, string? ContactPhone,
    string? ContactEmail, string? Source, string? SalesManager,
    string? MarketSegment, bool IsVip, ChargeRouting Routing,
    string? SpecialRequests, string? Notes,
    IReadOnlyCollection<CreateGroupServiceDraft>? ServiceDrafts = null,
    IReadOnlyCollection<CreateGroupRoomBlockDraft>? RoomBlockDrafts = null
);

public sealed record CreatedGroupDto(
    Guid GroupReservationId, Guid PropertyId, string GroupCode,
    string GroupName, GroupReservationStatus Status, DateTime CreatedAtUtc
);

public sealed record GroupStatusChangeDto(
    Guid GroupReservationId, Guid PropertyId, string GroupCode,
    GroupReservationStatus Status, DateTime UpdatedAtUtc
);

public sealed record GroupDepositScheduleDraft(DateOnly DueDate, decimal Percent);

public sealed record GroupDepositPolicyDraft(
    bool DepositRequired,
    string? GuaranteeType,
    DateOnly? CutoffDate,
    DateOnly? ReleaseDate,
    IReadOnlyCollection<GroupDepositScheduleDraft> Schedules
);

public sealed record ConfigureGroupBookingRequest(
    Guid GroupReservationId,
    IReadOnlyCollection<CreateGroupRoomBlockDraft> RoomBlockDrafts,
    bool Confirm,
    GroupDepositPolicyDraft? DepositPolicy = null
);

public sealed record ConfiguredGroupBookingDto(
    Guid GroupReservationId,
    Guid PropertyId,
    string GroupCode,
    GroupReservationStatus Status,
    int RoomBlockCount,
    DateTime SavedAtUtc
);

public sealed record GroupCheckInResult(
    Guid GroupReservationId, string GroupCode, GroupReservationStatus Status,
    int CheckedInRooms, int CheckedInGuests, DateTime CheckedInAtUtc
);

public sealed record GroupCheckOutResult(
    Guid GroupReservationId, string GroupCode, GroupReservationStatus Status,
    int CheckedOutRooms, int CheckedOutGuests, DateTime CheckedOutAtUtc,
    int InvoicesGenerated = 0,
    int HousekeepingTasksCreated = 0
);

public sealed record AssignGroupRoomRequest(
    Guid GroupReservationId, Guid HotelRoomId, Guid? GuestId,
    string? GuestName, int Adults, int Children
);

public sealed record AssignGroupRoomItem(
    Guid HotelRoomId, Guid? GuestId, string? GuestName, int Adults, int Children
);

public sealed record ReplaceGroupRoomAssignmentsRequest(
    Guid GroupReservationId, IReadOnlyCollection<AssignGroupRoomItem> Assignments
);

public sealed record ReplaceGroupRoomAssignmentsResult(
    Guid GroupReservationId, int AssignmentCount, DateTime SavedAtUtc
);

public sealed record GroupRoomAssignmentDto(
    Guid AssignmentId, Guid GroupReservationId, Guid PropertyId,
    Guid HotelRoomId, string RoomNumber, string RoomType, Guid? GuestId,
    string? GuestName, int Adults, int Children, bool IsCheckedIn, bool IsCheckedOut
);

// =============================================================================
// GROUP SERVICE ORDERS
// =============================================================================

public sealed record GroupServiceOrderDto(
    Guid GroupServiceOrderId, Guid PropertyId, Guid GroupReservationId,
    string ServiceCode, string ServiceName, string Category, string? SubCategory,
    string? Department, string? Provider, string? Location, DateOnly ServiceDate,
    TimeOnly ServiceTime, int DurationMinutes, int Quantity, decimal UnitPrice,
    decimal DiscountRate, decimal TaxRate, decimal ServiceChargeRate,
    decimal SubTotal, decimal DiscountAmount, decimal TaxAmount,
    decimal ServiceChargeAmount, decimal TotalAmount, decimal PaidAmount,
    decimal Balance, GroupServiceOrderStatus Status, string ChargeTo,
    string GroupCode, string GroupName, string? GuestName, string? Remark,
    DateTime CreatedAtUtc, DateTime UpdatedAtUtc, byte[]? RowVersion
);

public sealed record CreateGroupServiceOrderRequest(
    Guid GroupReservationId, string ServiceName, string Category, string? SubCategory,
    string? Department, string? Provider, string? Location, DateOnly ServiceDate,
    TimeOnly ServiceTime, int DurationMinutes, int Quantity, decimal UnitPrice,
    decimal DiscountRate, decimal TaxRate, decimal ServiceChargeRate,
    string ChargeTo, string? GuestName, string? Remark
);

public sealed record UpdateGroupServiceOrderRequest(
    Guid GroupServiceOrderId, string ServiceName, string Category, string? SubCategory,
    string? Department, string? Provider, string? Location, DateOnly ServiceDate,
    TimeOnly ServiceTime, int DurationMinutes, int Quantity, decimal UnitPrice,
    decimal DiscountRate, decimal TaxRate, decimal ServiceChargeRate,
    string ChargeTo, string? GuestName, string? Remark, byte[]? ExpectedRowVersion
);

public sealed record ChangeGroupServiceOrderStatusRequest(
    Guid GroupServiceOrderId, GroupServiceOrderStatus Status, string? Reason
);

public sealed record PostGroupServiceOrderToFolioRequest(
    Guid GroupServiceOrderId, Guid? TargetFolioId = null
);

public sealed record PostGroupServiceOrdersToFolioRequest(
    IReadOnlyCollection<Guid> GroupServiceOrderIds, Guid TargetFolioId
);

public sealed record GroupServiceBatchPostResult(
    Guid TargetFolioId, IReadOnlyList<Guid> GroupServiceOrderIds,
    int PostedCount, decimal TotalAmount
);

public sealed record BatchChangeGroupServiceOrderStatusRequest(
    IReadOnlyCollection<Guid> GroupServiceOrderIds, GroupServiceOrderStatus Status, string? Reason = null
);

public sealed record GroupServiceBatchStatusResult(
    IReadOnlyList<Guid> GroupServiceOrderIds, GroupServiceOrderStatus Status
);

// =============================================================================
// CONTRACTS
// =============================================================================

public interface IRoomBlockService
{
    Task<RoomBlockDto> CreateBlockAsync(Guid groupId, Guid roomTypeId, int rooms, DateOnly arrival, DateOnly departure, DateOnly? cutoff, decimal? negotiatedRate, CancellationToken ct);
    Task<RoomBlockDto> ModifyBlockAsync(Guid blockId, int? additionalRooms, DateOnly? newCutoff, CancellationToken ct);
    Task<RoomBlockDto> ReleaseRoomsAsync(Guid blockId, int count, CancellationToken ct);
    Task<PickupAnalysisDto> GetPickupAsync(Guid blockId, DateOnly? asOf, CancellationToken ct);
    Task<RoomBlockDashboardDto> GetBlockDashboardAsync(Guid propertyId, CancellationToken ct);
    Task<IReadOnlyList<RoomBlockDto>> GetActiveBlocksAsync(Guid propertyId, CancellationToken ct);
    Task<IReadOnlyList<PickupAnalysisDto>> GetPickupTrendsAsync(Guid propertyId, CancellationToken ct);
}

public interface IRoomingListService
{
    Task<IReadOnlyList<RoomingListItemDto>> GetRoomingListAsync(Guid groupId, CancellationToken ct);
    Task<RoomingListImportResult> ImportAsync(Guid groupId, IReadOnlyList<RoomingListItemDto> items, CancellationToken ct);
    Task<IReadOnlyList<RoomingListItemDto>> ExportAsync(Guid groupId, CancellationToken ct);
    Task AssignGuestAsync(Guid assignmentId, Guid guestId, string guestName, CancellationToken ct);
    Task MarkArrivalAsync(Guid assignmentId, DateTime checkInAt, CancellationToken ct);
    Task MarkDepartureAsync(Guid assignmentId, DateTime checkOutAt, CancellationToken ct);
}

public interface IGroupBillingService
{
    Task<GroupBillingSetup> ConfigureBillingAsync(GroupBillingSetup setup, CancellationToken ct);
    Task<BillingSummaryDto> GetBillingSummaryAsync(Guid groupId, CancellationToken ct);
    Task<GroupRevenueDashboardDto> GetRevenueDashboardAsync(Guid groupId, CancellationToken ct);
}

public interface IGroupDashboardService
{
    Task<GroupDashboardDto> GetDashboardAsync(Guid propertyId, CancellationToken ct);
}

public interface IGroupReservationQueryService
{
    Task<IReadOnlyList<GroupReservationSummaryDto>> GetGroupsAsync(Guid propertyId, CancellationToken ct);
}

public interface IGroupReservationCommandService
{
    Task<CreatedGroupDto> CreateAsync(CreateGroupRequest request, CancellationToken ct);
    Task<ConfiguredGroupBookingDto> ConfigureBookingAsync(ConfigureGroupBookingRequest request, CancellationToken ct);
    Task<GroupStatusChangeDto> ConfirmAsync(Guid groupId, CancellationToken ct);
    Task<GroupStatusChangeDto> CancelAsync(Guid groupId, string reason, CancellationToken ct);
    Task<GroupStatusChangeDto> CloseAsync(Guid groupId, CancellationToken ct);
    Task DeleteDraftAsync(Guid groupId, CancellationToken ct);
}

public interface IGroupCheckInCommandService
{
    Task<GroupCheckInResult> CheckInAsync(Guid groupId, IReadOnlyCollection<Guid> assignmentIds, CancellationToken ct);
}

public interface IGroupCheckOutCommandService
{
    Task<GroupCheckOutResult> CheckOutAsync(Guid groupId, IReadOnlyCollection<Guid> assignmentIds, DateTime checkOutAtUtc, CancellationToken ct);
    Task<GroupCheckOutResult> CheckOutAndIssueInvoicesAsync(Guid groupId, IReadOnlyCollection<Guid> assignmentIds, DateTime checkOutAtUtc, CancellationToken ct);
}

public interface IGroupRoomAssignmentCommandService
{
    Task<GroupRoomAssignmentDto> AssignAsync(AssignGroupRoomRequest request, CancellationToken ct);
    Task RemoveAsync(Guid groupId, Guid hotelRoomId, CancellationToken ct);
    Task<ReplaceGroupRoomAssignmentsResult> ReplaceAsync(ReplaceGroupRoomAssignmentsRequest request, CancellationToken ct);
}

public interface IGroupServiceOrderQueryService
{
    Task<IReadOnlyList<GroupServiceOrderDto>> SearchAsync(Guid propertyId, Guid? groupReservationId, CancellationToken ct);
}

public interface IGroupServiceOrderCommandService
{
    Task<GroupServiceOrderDto> CreateAsync(CreateGroupServiceOrderRequest request, CancellationToken ct);
    Task<GroupServiceOrderDto> UpdateAsync(UpdateGroupServiceOrderRequest request, CancellationToken ct);
    Task<GroupServiceOrderDto> ChangeStatusAsync(ChangeGroupServiceOrderStatusRequest request, CancellationToken ct);
    Task<GroupServiceBatchStatusResult> ChangeBatchStatusAsync(BatchChangeGroupServiceOrderStatusRequest request, CancellationToken ct);
    Task<GroupServiceOrderDto> PostToFolioAsync(PostGroupServiceOrderToFolioRequest request, CancellationToken ct);
    Task<GroupServiceBatchPostResult> PostBatchToFolioAsync(PostGroupServiceOrdersToFolioRequest request, CancellationToken ct);
}

// =============================================================================
// EVENTS
// =============================================================================

public static class GroupEvents
{
    public const string GroupCreated = "Group.Created";
    public const string GroupConfirmed = "Group.Confirmed";
    public const string RoomBlocked = "Group.RoomBlocked";
    public const string RoomReleased = "Group.RoomReleased";
    public const string PickupUpdated = "Group.PickupUpdated";
    public const string RoomingListImported = "Group.RoomingListImported";
    public const string GuestAssigned = "Group.GuestAssigned";
    public const string GroupCheckedIn = "Group.GroupCheckedIn";
    public const string GroupCheckedOut = "Group.GroupCheckedOut";
    public const string BlockCutoffExpired = "Group.BlockCutoffExpired";
}
