using ASMILE.BO.Application.FrontOffice.Foundation.Cqrs;
using ASMILE.BO.Shared.Foundation;

namespace ASMILE.BO.Application.FrontOffice.Reporting;

public sealed record OccupancyKpiDto(DateOnly Date, int TotalRooms, int RoomsSold, int RoomsAvailable, decimal OccupancyPct, decimal ADR, decimal RevPAR);
public sealed record RevenueKpiDto(DateOnly Date, decimal RoomRevenue, decimal OtherRevenue, decimal TotalRevenue, decimal TotalPayments, decimal TotalDeposits, decimal TotalRefunds, decimal FolioBalance);
public sealed record OperationalKpiDto(
    DateOnly Date,
    int Arrivals,
    int Departures,
    int NoShows,
    int Cancellations,
    int InHouse,
    int ExpectedArrivals,
    int ExpectedDepartures,
    int VacantClean,
    int VacantDirty,
    int OccupiedClean,
    int OccupiedDirty,
    int OutOfOrder,
    int RoomStatusHistoryCoverage = 0,
    bool IsRoomStatusHistorical = false);
public sealed record CashierKpiDto(decimal TotalCashIn, decimal TotalCashOut, decimal TotalRefund, decimal ExpectedCash, decimal? Difference);
public sealed record HousekeepingKpiDto(int DirtyRooms, int CleaningRooms, int InspectedRooms, int VacantCleanRooms, int OutOfOrderRooms, decimal AvgCleaningMinutes, int TasksPending, int TasksCompleted);
public sealed record ExecutiveDashboardDto(OccupancyKpiDto Occupancy, RevenueKpiDto Revenue, OperationalKpiDto Operations, CashierKpiDto Cashier, HousekeepingKpiDto Housekeeping);
public sealed record CashierShiftReportRow(
    DateOnly BusinessDate,
    string ShiftNumber,
    string CashierName,
    string Status,
    decimal OpeningCash,
    decimal CashIn,
    decimal CashOut,
    decimal Refund,
    decimal TransferIn,
    decimal TransferOut,
    decimal Adjustment,
    decimal ExpectedCash,
    decimal? ActualCash,
    decimal? Difference,
    string? ReconciliationStatus,
    DateTime OpenedAtUtc,
    DateTime? ClosedAtUtc);
public sealed record FolioBusinessDateGapRow(
    Guid EntryId,
    Guid FolioId,
    string EntryType,
    decimal Amount,
    DateTime CreatedAtUtc,
    DateOnly SuggestedBusinessDate,
    string Description);

public sealed record DashboardQuery(DateOnly BusinessDate) : IQuery<ExecutiveDashboardDto>;
public sealed record OccupancyReportQuery(Guid PropertyId, DateOnly From, DateOnly To) : IQuery<IReadOnlyList<OccupancyKpiDto>>;
public sealed record RevenueReportQuery(Guid PropertyId, DateOnly From, DateOnly To) : IQuery<IReadOnlyList<RevenueKpiDto>>;
public sealed record OperationalReportQuery(Guid PropertyId, DateOnly Date) : IQuery<OperationalKpiDto>;
public sealed record CashierReportQuery(Guid PropertyId, DateOnly From, DateOnly To) : IQuery<IReadOnlyList<CashierShiftReportRow>>;
public sealed record FolioBusinessDateGapQuery(Guid PropertyId, DateOnly From, DateOnly To) : IQuery<IReadOnlyList<FolioBusinessDateGapRow>>;
public sealed record FolioBusinessDateBackfillItem(Guid EntryId, DateOnly BusinessDate);
public sealed record FolioBusinessDateBackfillCommand(
    Guid PropertyId,
    IReadOnlyList<FolioBusinessDateBackfillItem> Items,
    string ApprovalReason,
    string ApprovalReference);
public sealed record FolioBusinessDateBackfillResult(int UpdatedCount, int AlreadyAppliedCount);

public interface IFolioBusinessDateBackfillService
{
    Task<Result<FolioBusinessDateBackfillResult>> ApplyAsync(FolioBusinessDateBackfillCommand command, CancellationToken ct);
}

public interface IReportingQueryService
{
    Task<Result<ExecutiveDashboardDto>> GetDashboardAsync(DashboardQuery query, CancellationToken ct);
    Task<Result<IReadOnlyList<OccupancyKpiDto>>> GetOccupancyReportAsync(OccupancyReportQuery query, CancellationToken ct);
    Task<Result<IReadOnlyList<RevenueKpiDto>>> GetRevenueReportAsync(RevenueReportQuery query, CancellationToken ct);
    Task<Result<OperationalKpiDto>> GetOperationalReportAsync(OperationalReportQuery query, CancellationToken ct);
    Task<Result<IReadOnlyList<CashierShiftReportRow>>> GetCashierReportAsync(CashierReportQuery query, CancellationToken ct);
    Task<Result<IReadOnlyList<FolioBusinessDateGapRow>>> GetFolioBusinessDateGapReportAsync(FolioBusinessDateGapQuery query, CancellationToken ct);
}
