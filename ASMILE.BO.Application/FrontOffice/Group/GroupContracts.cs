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
    Guid? MasterFolioId, Guid? DepositRequired
);

public sealed record BillingSummaryDto(
    Guid GroupId, string GroupName, decimal TotalCharges, decimal TotalPayments,
    decimal TotalDeposits, decimal Outstanding, int MasterFolioCount,
    int IndividualFolioCount, bool IsBalanced
);

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
