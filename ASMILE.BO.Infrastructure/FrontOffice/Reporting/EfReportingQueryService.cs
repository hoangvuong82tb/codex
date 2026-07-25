using ASMILE.BO.Application.FrontOffice.Foundation.Contexts;
using ASMILE.BO.Application.FrontOffice.Reporting;
using ASMILE.BO.Application.Interfaces;
using ASMILE.BO.Core.FrontOffice.Folio;
using ASMILE.BO.Core.FrontOffice.HotelMasterData;
using ASMILE.BO.Core.FrontOffice.Housekeeping;
using ASMILE.BO.Core.FrontOffice.Stay;
using Res = ASMILE.BO.Core.FrontOffice.Reservation;
using ASMILE.BO.Infrastructure.Persistence;
using ASMILE.BO.Shared.Foundation;
using Microsoft.EntityFrameworkCore;

namespace ASMILE.BO.Infrastructure.FrontOffice.Reporting;

public sealed class EfReportingQueryService(AppDbContext db, IFrontOfficeExecutionContextAccessor ctx, IAuthorizationService auth) : IReportingQueryService
{
    public async Task<Result<ExecutiveDashboardDto>> GetDashboardAsync(DashboardQuery q, CancellationToken ct)
    {
        if (!auth.HasPermission("FrontOffice.Reporting.View"))
            return Result<ExecutiveDashboardDto>.Failure(Error.Authorization("REPORT_PERMISSION_DENIED", ""));
        var cx = ctx.Current;
        if (cx is null) return Result<ExecutiveDashboardDto>.Failure(Error.Authorization("REPORT_CONTEXT_REQUIRED", ""));
        var pid = cx.Property.PropertyId.Value;
        var date = q.BusinessDate;

        var totalRooms = await db.HotelRooms.CountAsync(r => r.PropertyId == pid && r.IsActive, ct);
        var inHouse = await db.HotelStays.CountAsync(s => s.PropertyId == pid && s.Status == HotelStayStatus.InHouse, ct);
        var occPct = totalRooms > 0 ? (decimal)inHouse / totalRooms * 100 : 0;
        var roomRev = await db.HotelFolioEntries.Where(e => e.PropertyId == pid && e.EntryType == HotelFolioEntryType.RoomCharge).SumAsync(e => e.Amount, ct);
        var adr = inHouse > 0 ? roomRev / inHouse : 0;
        var revpar = totalRooms > 0 ? roomRev / totalRooms : 0;
        var payments = Math.Abs(await db.HotelFolioEntries.Where(e => e.PropertyId == pid && e.EntryType == HotelFolioEntryType.Payment).SumAsync(e => e.Amount, ct));
        var refunds = await db.HotelFolioEntries.Where(e => e.PropertyId == pid && e.EntryType == HotelFolioEntryType.Refund).SumAsync(e => e.Amount, ct);
        var arrivals = await db.HotelStays.CountAsync(s => s.PropertyId == pid && s.ScheduledArrivalDate == date, ct);
        var departures = await db.HotelStays.CountAsync(s => s.PropertyId == pid && s.ScheduledDepartureDate == date, ct);
        var noShows = await db.Set<Res.Reservation>().CountAsync(r => r.PropertyId == pid && r.Status == Res.ReservationStatus.NoShow, ct);
        var cancellations = await db.Set<Res.Reservation>().CountAsync(r => r.PropertyId == pid && r.Status == Res.ReservationStatus.Cancelled, ct);
        var vc = await db.Set<RoomHousekeepingState>().CountAsync(r => r.PropertyId == pid && r.Status == HousekeepingRoomStatus.VacantClean, ct);
        var vd = await db.Set<RoomHousekeepingState>().CountAsync(r => r.PropertyId == pid && (r.Status == HousekeepingRoomStatus.VacantDirty), ct);
        var oo = await db.Set<RoomHousekeepingState>().CountAsync(r => r.PropertyId == pid && r.Status == HousekeepingRoomStatus.OutOfOrder, ct);

        return Result<ExecutiveDashboardDto>.Success(new ExecutiveDashboardDto(
            new OccupancyKpiDto(date, totalRooms, inHouse, totalRooms - inHouse, occPct, adr, revpar),
            new RevenueKpiDto(date, roomRev, 0, roomRev, payments, refunds, 0),
            new OperationalKpiDto(date, arrivals, departures, noShows, cancellations, inHouse, 0, 0, vc, vd, 0, 0, oo),
            new CashierKpiDto(0, 0, 0, 0, null),
            new HousekeepingKpiDto(vd, await db.Set<RoomHousekeepingState>().CountAsync(r => r.PropertyId == pid && r.Status == HousekeepingRoomStatus.Cleaning, ct), await db.Set<RoomHousekeepingState>().CountAsync(r => r.PropertyId == pid && r.Status == HousekeepingRoomStatus.Inspected, ct), vc, oo, 0, 0, 0)));
    }

    public Task<Result<IReadOnlyList<OccupancyKpiDto>>> GetOccupancyReportAsync(OccupancyReportQuery q, CancellationToken ct) => Task.FromResult(Result<IReadOnlyList<OccupancyKpiDto>>.Success([]));
    public Task<Result<IReadOnlyList<RevenueKpiDto>>> GetRevenueReportAsync(RevenueReportQuery q, CancellationToken ct) => Task.FromResult(Result<IReadOnlyList<RevenueKpiDto>>.Success([]));
    public Task<Result<OperationalKpiDto>> GetOperationalReportAsync(OperationalReportQuery q, CancellationToken ct) => Task.FromResult(Result<OperationalKpiDto>.Success(new OperationalKpiDto(q.Date, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0)));
}
