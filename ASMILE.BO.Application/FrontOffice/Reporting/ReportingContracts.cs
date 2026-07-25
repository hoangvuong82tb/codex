using ASMILE.BO.Application.FrontOffice.Foundation.Cqrs;
using ASMILE.BO.Shared.Foundation;

namespace ASMILE.BO.Application.FrontOffice.Reporting;

public sealed record OccupancyKpiDto(DateOnly Date, int TotalRooms, int RoomsSold, int RoomsAvailable, decimal OccupancyPct, decimal ADR, decimal RevPAR);
public sealed record RevenueKpiDto(DateOnly Date, decimal RoomRevenue, decimal OtherRevenue, decimal TotalRevenue, decimal TotalPayments, decimal TotalRefunds, decimal OutstandingBalance);
public sealed record OperationalKpiDto(DateOnly Date, int Arrivals, int Departures, int NoShows, int Cancellations, int InHouse, int ExpectedArrivals, int ExpectedDepartures, int VacantClean, int VacantDirty, int OccupiedClean, int OccupiedDirty, int OutOfOrder);
public sealed record CashierKpiDto(decimal TotalCashIn, decimal TotalCashOut, decimal TotalRefund, decimal ExpectedCash, decimal? Difference);
public sealed record HousekeepingKpiDto(int DirtyRooms, int CleaningRooms, int InspectedRooms, int VacantCleanRooms, int OutOfOrderRooms, decimal AvgCleaningMinutes, int TasksPending, int TasksCompleted);
public sealed record ExecutiveDashboardDto(OccupancyKpiDto Occupancy, RevenueKpiDto Revenue, OperationalKpiDto Operations, CashierKpiDto Cashier, HousekeepingKpiDto Housekeeping);

public sealed record DashboardQuery(DateOnly BusinessDate) : IQuery<ExecutiveDashboardDto>;
public sealed record OccupancyReportQuery(Guid PropertyId, DateOnly From, DateOnly To) : IQuery<IReadOnlyList<OccupancyKpiDto>>;
public sealed record RevenueReportQuery(Guid PropertyId, DateOnly From, DateOnly To) : IQuery<IReadOnlyList<RevenueKpiDto>>;
public sealed record OperationalReportQuery(Guid PropertyId, DateOnly Date) : IQuery<OperationalKpiDto>;

public interface IReportingQueryService
{
    Task<Result<ExecutiveDashboardDto>> GetDashboardAsync(DashboardQuery query, CancellationToken ct);
    Task<Result<IReadOnlyList<OccupancyKpiDto>>> GetOccupancyReportAsync(OccupancyReportQuery query, CancellationToken ct);
    Task<Result<IReadOnlyList<RevenueKpiDto>>> GetRevenueReportAsync(RevenueReportQuery query, CancellationToken ct);
    Task<Result<OperationalKpiDto>> GetOperationalReportAsync(OperationalReportQuery query, CancellationToken ct);
}
