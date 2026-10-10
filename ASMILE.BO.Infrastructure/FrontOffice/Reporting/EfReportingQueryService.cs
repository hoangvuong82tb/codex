using ASMILE.BO.Application.FrontOffice.Foundation.Contexts;
using ASMILE.BO.Application.FrontOffice.Reporting;
using ASMILE.BO.Application.Interfaces;
using ASMILE.BO.Core.FrontOffice.Cashier;
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
        var operationalResult = await GetOperationalReportAsync(new OperationalReportQuery(pid, date), ct);
        if (operationalResult.IsFailure) return Result<ExecutiveDashboardDto>.Failure(operationalResult.Error!);
        var operational = operationalResult.Value!;
        var inHouse = operational.InHouse;
        var occPct = totalRooms > 0 ? (decimal)inHouse / totalRooms * 100 : 0;
        var revenueResult = await GetRevenueReportAsync(new RevenueReportQuery(pid, date, date), ct);
        if (revenueResult.IsFailure) return Result<ExecutiveDashboardDto>.Failure(revenueResult.Error!);
        var revenue = revenueResult.Value!.Single();
        var adr = inHouse > 0 ? revenue.RoomRevenue / inHouse : 0;
        var revpar = totalRooms > 0 ? revenue.RoomRevenue / totalRooms : 0;

        var roomCounts = await db.Set<RoomHousekeepingState>().AsNoTracking()
            .Where(r => r.PropertyId == pid)
            .GroupBy(r => r.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Status, x => x.Count, ct);
        var vc = roomCounts.GetValueOrDefault(HousekeepingRoomStatus.VacantClean);
        var vd = roomCounts.GetValueOrDefault(HousekeepingRoomStatus.VacantDirty);
        var oo = roomCounts.GetValueOrDefault(HousekeepingRoomStatus.OutOfOrder);
        var cleaning = roomCounts.GetValueOrDefault(HousekeepingRoomStatus.Cleaning);
        var inspected = roomCounts.GetValueOrDefault(HousekeepingRoomStatus.Inspected);

        var (dayStartUtc, nextDayStartUtc) = GetBusinessDateUtcRange(date, cx.Property.TimeZoneId);
        var taskQuery = db.HousekeepingTasks.AsNoTracking().Where(t => t.PropertyId == pid);
        var tasksPending = await taskQuery.CountAsync(t =>
            t.Status != HousekeepingTaskStatus.Completed && t.Status != HousekeepingTaskStatus.Cancelled, ct);
        var completedTasks = await taskQuery
            .Where(t => t.CompletedAtUtc.HasValue
                && t.CompletedAtUtc.Value >= dayStartUtc
                && t.CompletedAtUtc.Value < nextDayStartUtc
                && t.StartedAtUtc.HasValue
                && t.CompletedAtUtc.Value >= t.StartedAtUtc.Value)
            .Select(t => new { Started = t.StartedAtUtc!.Value, Completed = t.CompletedAtUtc!.Value, t.TotalPauseMinutes })
            .ToListAsync(ct);
        var averageCleaningMinutes = completedTasks.Count == 0
            ? 0m
            : (decimal)completedTasks.Average(t => Math.Max(0d, (t.Completed - t.Started).TotalMinutes - t.TotalPauseMinutes));

        var shifts = await db.CashierShifts.AsNoTracking()
            .Include(x => x.Movements)
            .Where(x => x.PropertyId == pid && x.BusinessDate == date)
            .ToListAsync(ct);
        var cashier = new CashierKpiDto(
            shifts.Sum(x => x.Movements.Where(m => m.Type == CashMovementType.CashIn).Sum(m => m.Amount)),
            shifts.Sum(x => x.Movements.Where(m => m.Type == CashMovementType.CashOut).Sum(m => m.Amount)),
            shifts.Sum(x => x.Movements.Where(m => m.Type == CashMovementType.Refund).Sum(m => m.Amount)),
            shifts.Sum(x => x.ExpectedCash),
            shifts.Count > 0 && shifts.All(x => x.ActualCash.HasValue)
                ? shifts.Sum(x => x.Difference ?? 0m)
                : null);

        return Result<ExecutiveDashboardDto>.Success(new ExecutiveDashboardDto(
            new OccupancyKpiDto(date, totalRooms, inHouse, Math.Max(0, totalRooms - inHouse), occPct, adr, revpar),
            revenue,
            operational,
            cashier,
            new HousekeepingKpiDto(vd, cleaning, inspected, vc, oo, averageCleaningMinutes, tasksPending, completedTasks.Count)));
    }

    public async Task<Result<IReadOnlyList<OccupancyKpiDto>>> GetOccupancyReportAsync(OccupancyReportQuery q, CancellationToken ct)
    {
        var scope = ValidateScope(q.PropertyId);
        if (scope.IsFailure) return Result<IReadOnlyList<OccupancyKpiDto>>.Failure(scope.Error!);
        if (q.From > q.To) return Result<IReadOnlyList<OccupancyKpiDto>>.Failure(Error.Validation("REPORT_DATE_RANGE_INVALID", "From must be before To."));
        var datedRoomLedger = await ValidateRoomRevenueBusinessDatesAsync(q.PropertyId, ct);
        if (datedRoomLedger.IsFailure) return Result<IReadOnlyList<OccupancyKpiDto>>.Failure(datedRoomLedger.Error!);

        var totalRooms = await db.HotelRooms.CountAsync(r => r.PropertyId == q.PropertyId && r.IsActive, ct);
        var stays = await db.HotelStays.AsNoTracking()
            .Where(s => s.PropertyId == q.PropertyId && s.ScheduledArrivalDate <= q.To && s.ScheduledDepartureDate > q.From && s.Status != HotelStayStatus.Cancelled)
            .Select(s => new { s.ScheduledArrivalDate, s.ScheduledDepartureDate })
            .ToListAsync(ct);
        var fromDay = q.From.DayNumber;
        var toDayExclusive = q.To.DayNumber + 1;
        var occupancyChanges = new Dictionary<int, int>();
        foreach (var stay in stays)
        {
            var startDay = Math.Max(fromDay, stay.ScheduledArrivalDate.DayNumber);
            var endDay = Math.Min(toDayExclusive, stay.ScheduledDepartureDate.DayNumber);
            if (startDay >= endDay) continue;
            occupancyChanges[startDay] = occupancyChanges.GetValueOrDefault(startDay) + 1;
            occupancyChanges[endDay] = occupancyChanges.GetValueOrDefault(endDay) - 1;
        }
        var roomCharges = await (
            from entry in db.HotelFolioEntries.AsNoTracking()
            join original in db.HotelFolioEntries.AsNoTracking()
                on entry.ReversesEntryId equals original.HotelFolioEntryId into originals
            from original in originals.DefaultIfEmpty()
            where entry.PropertyId == q.PropertyId
                && entry.BusinessDate.HasValue
                && entry.BusinessDate.Value >= q.From
                && entry.BusinessDate.Value <= q.To
                && (entry.EntryType == HotelFolioEntryType.RoomCharge
                    || (entry.EntryType == HotelFolioEntryType.Correction
                        && original != null
                        && original.EntryType == HotelFolioEntryType.RoomCharge))
            group entry by entry.BusinessDate!.Value into dailyEntries
            select new { Date = dailyEntries.Key, Amount = dailyEntries.Sum(entry => entry.Amount) })
            .ToListAsync(ct);
        var roomRevenueByDate = roomCharges.ToDictionary(x => x.Date, x => x.Amount);

        var result = new List<OccupancyKpiDto>();
        var sold = 0;
        for (var dayNumber = fromDay; dayNumber < toDayExclusive; dayNumber++)
        {
            sold += occupancyChanges.GetValueOrDefault(dayNumber);
            var date = DateOnly.FromDayNumber(dayNumber);
            var revenue = roomRevenueByDate.GetValueOrDefault(date);
            result.Add(new OccupancyKpiDto(date, totalRooms, sold, Math.Max(0, totalRooms - sold), totalRooms == 0 ? 0 : (decimal)sold / totalRooms * 100, sold == 0 ? 0 : revenue / sold, totalRooms == 0 ? 0 : revenue / totalRooms));
        }
        return Result<IReadOnlyList<OccupancyKpiDto>>.Success(result);
    }

    public async Task<Result<IReadOnlyList<RevenueKpiDto>>> GetRevenueReportAsync(RevenueReportQuery q, CancellationToken ct)
    {
        var scope = ValidateScope(q.PropertyId);
        if (scope.IsFailure) return Result<IReadOnlyList<RevenueKpiDto>>.Failure(scope.Error!);
        if (q.From > q.To) return Result<IReadOnlyList<RevenueKpiDto>>.Failure(Error.Validation("REPORT_DATE_RANGE_INVALID", "From must be before To."));
        var datedLedger = await ValidateRevenueBusinessDatesAsync(q.PropertyId, ct);
        if (datedLedger.IsFailure) return Result<IReadOnlyList<RevenueKpiDto>>.Failure(datedLedger.Error!);

        var dailyTotals = await (
            from entry in db.HotelFolioEntries.AsNoTracking()
            join original in db.HotelFolioEntries.AsNoTracking()
                on entry.ReversesEntryId equals original.HotelFolioEntryId into originals
            from original in originals.DefaultIfEmpty()
            where entry.PropertyId == q.PropertyId
                && entry.BusinessDate.HasValue
                && entry.BusinessDate.Value >= q.From
                && entry.BusinessDate.Value <= q.To
            group new { entry, original } by entry.BusinessDate!.Value into dailyEntries
            select new
            {
                Date = dailyEntries.Key,
                RoomRevenue = dailyEntries.Sum(x =>
                    x.entry.EntryType == HotelFolioEntryType.RoomCharge
                    || (x.entry.EntryType == HotelFolioEntryType.Correction && x.original != null && x.original.EntryType == HotelFolioEntryType.RoomCharge)
                        ? x.entry.Amount : 0m),
                OtherRevenue = dailyEntries.Sum(x =>
                    (x.entry.EntryType != HotelFolioEntryType.RoomCharge
                        && x.entry.EntryType != HotelFolioEntryType.Payment
                        && x.entry.EntryType != HotelFolioEntryType.Deposit
                        && x.entry.EntryType != HotelFolioEntryType.Refund
                        && x.entry.EntryType != HotelFolioEntryType.Correction)
                    || (x.entry.EntryType == HotelFolioEntryType.Correction && x.original == null)
                    || (x.entry.EntryType == HotelFolioEntryType.Correction
                        && x.original != null
                        && x.original.EntryType != HotelFolioEntryType.RoomCharge
                        && x.original.EntryType != HotelFolioEntryType.Payment
                        && x.original.EntryType != HotelFolioEntryType.Deposit
                        && x.original.EntryType != HotelFolioEntryType.Refund)
                        ? x.entry.Amount : 0m),
                Payments = dailyEntries.Sum(x =>
                    x.entry.EntryType == HotelFolioEntryType.Payment
                    || (x.entry.EntryType == HotelFolioEntryType.Correction && x.original != null && x.original.EntryType == HotelFolioEntryType.Payment)
                        ? x.entry.Amount : 0m),
                Deposits = dailyEntries.Sum(x =>
                    x.entry.EntryType == HotelFolioEntryType.Deposit
                    || (x.entry.EntryType == HotelFolioEntryType.Correction && x.original != null && x.original.EntryType == HotelFolioEntryType.Deposit)
                        ? x.entry.Amount : 0m),
                Refunds = dailyEntries.Sum(x =>
                    x.entry.EntryType == HotelFolioEntryType.Refund
                    || (x.entry.EntryType == HotelFolioEntryType.Correction && x.original != null && x.original.EntryType == HotelFolioEntryType.Refund)
                        ? x.entry.Amount : 0m)
            })
            .ToListAsync(ct);
        var totalsByDate = dailyTotals.ToDictionary(x => x.Date);
        var result = new List<RevenueKpiDto>();
        for (var dayNumber = q.From.DayNumber; dayNumber <= q.To.DayNumber; dayNumber++)
        {
            var date = DateOnly.FromDayNumber(dayNumber);
            totalsByDate.TryGetValue(date, out var daily);
            var roomRevenue = daily?.RoomRevenue ?? 0m;
            var otherRevenue = daily?.OtherRevenue ?? 0m;
            var payments = Math.Abs(daily?.Payments ?? 0m);
            var deposits = Math.Abs(daily?.Deposits ?? 0m);
            var refunds = Math.Abs(daily?.Refunds ?? 0m);
            var totalRevenue = roomRevenue + otherRevenue;
            result.Add(new RevenueKpiDto(date, roomRevenue, otherRevenue, totalRevenue, payments, deposits, refunds, totalRevenue - payments - deposits + refunds));
        }
        return Result<IReadOnlyList<RevenueKpiDto>>.Success(result);
    }

    public async Task<Result<IReadOnlyList<CashierShiftReportRow>>> GetCashierReportAsync(CashierReportQuery q, CancellationToken ct)
    {
        var scope = ValidateScope(q.PropertyId);
        if (scope.IsFailure) return Result<IReadOnlyList<CashierShiftReportRow>>.Failure(scope.Error!);
        if (q.From > q.To || q.To.DayNumber - q.From.DayNumber > 365)
            return Result<IReadOnlyList<CashierShiftReportRow>>.Failure(Error.Validation("REPORT_DATE_RANGE_INVALID", "Khoảng ngày báo cáo không hợp lệ hoặc vượt quá 366 ngày."));

        var shifts = await db.CashierShifts.AsNoTracking()
            .Include(x => x.Movements)
            .Where(x => x.PropertyId == q.PropertyId && x.BusinessDate >= q.From && x.BusinessDate <= q.To)
            .OrderByDescending(x => x.BusinessDate)
            .ThenByDescending(x => x.OpenedAtUtc)
            .Take(2_000)
            .ToListAsync(ct);

        var cashierIds = shifts.Select(x => x.CashierId).Distinct().ToArray();
        var cashierNames = await db.AppUsers.AsNoTracking()
            .Where(x => cashierIds.Contains(x.Id))
            .Select(x => new { x.Id, Name = string.IsNullOrWhiteSpace(x.DisplayName) ? x.UserName : x.DisplayName })
            .ToDictionaryAsync(x => x.Id, x => x.Name, ct);
        var shiftIds = shifts.Select(x => x.CashierShiftId).ToArray();
        var reconciliations = await db.CashReconciliations.AsNoTracking()
            .Where(x => x.PropertyId == q.PropertyId && shiftIds.Contains(x.CashierShiftId))
            .Select(x => new { x.CashierShiftId, x.Status })
            .ToDictionaryAsync(x => x.CashierShiftId, x => x.Status.ToString(), ct);

        var rows = shifts.Select(shift => new CashierShiftReportRow(
            shift.BusinessDate,
            shift.ShiftNumber,
            cashierNames.GetValueOrDefault(shift.CashierId, "Tài khoản đã ngưng hoạt động"),
            shift.Status.ToString(),
            shift.OpeningCash,
            shift.Movements.Where(x => x.Type == CashMovementType.CashIn).Sum(x => x.Amount),
            shift.Movements.Where(x => x.Type == CashMovementType.CashOut).Sum(x => x.Amount),
            shift.Movements.Where(x => x.Type == CashMovementType.Refund).Sum(x => x.Amount),
            shift.Movements.Where(x => x.Type == CashMovementType.TransferIn).Sum(x => x.Amount),
            shift.Movements.Where(x => x.Type == CashMovementType.TransferOut).Sum(x => x.Amount),
            shift.Movements.Where(x => x.Type == CashMovementType.Adjustment).Sum(x => x.Amount),
            shift.ExpectedCash,
            shift.ActualCash,
            shift.Difference,
            reconciliations.GetValueOrDefault(shift.CashierShiftId),
            shift.OpenedAtUtc,
            shift.ClosedAtUtc)).ToList();
        return Result<IReadOnlyList<CashierShiftReportRow>>.Success(rows);
    }

    public async Task<Result<IReadOnlyList<FolioBusinessDateGapRow>>> GetFolioBusinessDateGapReportAsync(FolioBusinessDateGapQuery q, CancellationToken ct)
    {
        var scope = ValidateScope(q.PropertyId);
        if (scope.IsFailure) return Result<IReadOnlyList<FolioBusinessDateGapRow>>.Failure(scope.Error!);
        if (q.From > q.To || q.To.DayNumber - q.From.DayNumber > 365)
            return Result<IReadOnlyList<FolioBusinessDateGapRow>>.Failure(Error.Validation("REPORT_DATE_RANGE_INVALID", "Khoảng ngày báo cáo không hợp lệ hoặc vượt quá 366 ngày."));

        DateTime fromUtc;
        DateTime nextDayUtc;
        TimeZoneInfo timeZone;
        try
        {
            var current = ctx.Current!;
            timeZone = TimeZoneInfo.FindSystemTimeZoneById(current.Property.TimeZoneId);
            fromUtc = ConvertLocalBoundaryToUtc(DateTime.SpecifyKind(q.From.ToDateTime(TimeOnly.MinValue), DateTimeKind.Unspecified), timeZone);
            nextDayUtc = ConvertLocalBoundaryToUtc(DateTime.SpecifyKind(q.To.AddDays(1).ToDateTime(TimeOnly.MinValue), DateTimeKind.Unspecified), timeZone);
        }
        catch (TimeZoneNotFoundException)
        {
            return Result<IReadOnlyList<FolioBusinessDateGapRow>>.Failure(Error.Validation("REPORT_PROPERTY_TIME_ZONE_INVALID", "Cơ sở chưa cấu hình múi giờ hợp lệ."));
        }
        catch (InvalidTimeZoneException)
        {
            return Result<IReadOnlyList<FolioBusinessDateGapRow>>.Failure(Error.Validation("REPORT_PROPERTY_TIME_ZONE_INVALID", "Cấu hình múi giờ của cơ sở không hợp lệ."));
        }
        catch (ArgumentException)
        {
            return Result<IReadOnlyList<FolioBusinessDateGapRow>>.Failure(Error.Validation("REPORT_PROPERTY_BUSINESS_DAY_INVALID", "Không thể xác định ranh giới ngày nghiệp vụ trong múi giờ của cơ sở."));
        }

        var gaps = await db.HotelFolioEntries.AsNoTracking()
            .Where(x => x.PropertyId == q.PropertyId
                && !x.BusinessDate.HasValue
                && x.CreatedAtUtc >= fromUtc
                && x.CreatedAtUtc < nextDayUtc)
            .OrderBy(x => x.CreatedAtUtc)
            .Take(2_000)
            .Select(x => new { x.HotelFolioEntryId, x.HotelFolioId, x.EntryType, x.Amount, x.CreatedAtUtc, x.Description })
            .ToListAsync(ct);

        var rows = gaps.Select(x => new FolioBusinessDateGapRow(
            x.HotelFolioEntryId,
            x.HotelFolioId,
            x.EntryType.ToString(),
            x.Amount,
            x.CreatedAtUtc,
            DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(x.CreatedAtUtc, DateTimeKind.Utc), timeZone)),
            x.Description)).ToList();
        return Result<IReadOnlyList<FolioBusinessDateGapRow>>.Success(rows);
    }

    public async Task<Result<OperationalKpiDto>> GetOperationalReportAsync(OperationalReportQuery q, CancellationToken ct)
    {
        var scope = ValidateScope(q.PropertyId);
        if (scope.IsFailure) return Result<OperationalKpiDto>.Failure(scope.Error!);
        var current = ctx.Current!;
        DateTime dayStartUtc;
        DateTime nextDayStartUtc;
        try
        {
            (dayStartUtc, nextDayStartUtc) = GetBusinessDateUtcRange(q.Date, current.Property.TimeZoneId);
        }
        catch (TimeZoneNotFoundException)
        {
            return Result<OperationalKpiDto>.Failure(Error.Validation("REPORT_PROPERTY_TIME_ZONE_INVALID", "Cơ sở chưa cấu hình múi giờ hợp lệ."));
        }
        catch (InvalidTimeZoneException)
        {
            return Result<OperationalKpiDto>.Failure(Error.Validation("REPORT_PROPERTY_TIME_ZONE_INVALID", "Cấu hình múi giờ của cơ sở không hợp lệ."));
        }
        catch (ArgumentException)
        {
            return Result<OperationalKpiDto>.Failure(Error.Validation("REPORT_PROPERTY_BUSINESS_DAY_INVALID", "Không thể xác định ranh giới ngày nghiệp vụ trong múi giờ của cơ sở."));
        }

        var stays = await db.HotelStays.AsNoTracking()
            .Where(s => s.PropertyId == q.PropertyId
                && (s.ScheduledArrivalDate == q.Date
                    || s.ScheduledDepartureDate == q.Date
                    || (s.ScheduledArrivalDate < q.Date && s.ScheduledDepartureDate > q.Date)
                    || (s.ActualCheckInAtUtc.HasValue && s.ActualCheckInAtUtc.Value >= dayStartUtc && s.ActualCheckInAtUtc.Value < nextDayStartUtc)
                    || (s.ActualCheckOutAtUtc.HasValue && s.ActualCheckOutAtUtc.Value >= dayStartUtc && s.ActualCheckOutAtUtc.Value < nextDayStartUtc)))
            .Select(s => new { s.ScheduledArrivalDate, s.ScheduledDepartureDate, s.ActualCheckInAtUtc, s.ActualCheckOutAtUtc, s.Status })
            .ToListAsync(ct);
        var (roomCounts, roomStatusHistoryCoverage, isRoomStatusHistorical) = await GetRoomStatusCountsAtAsync(q.PropertyId, nextDayStartUtc, ct);
        var inHouse = stays.Count(s => s.ScheduledArrivalDate <= q.Date && s.ScheduledDepartureDate > q.Date && s.Status.IsOccupying());
        var arrivals = stays.Count(s => s.ActualCheckInAtUtc >= dayStartUtc && s.ActualCheckInAtUtc < nextDayStartUtc);
        var departures = stays.Count(s => s.ActualCheckOutAtUtc >= dayStartUtc && s.ActualCheckOutAtUtc < nextDayStartUtc);
        var noShows = await db.Set<Res.Reservation>().CountAsync(r => r.PropertyId == q.PropertyId && r.Status == Res.ReservationStatus.NoShow && r.ArrivalDate == q.Date, ct);
        var cancellations = await db.Set<Res.Reservation>().CountAsync(r => r.PropertyId == q.PropertyId && r.Status == Res.ReservationStatus.Cancelled && r.ArrivalDate == q.Date, ct);
        return Result<OperationalKpiDto>.Success(new OperationalKpiDto(q.Date, arrivals, departures, noShows, cancellations, inHouse,
            stays.Count(s => s.ScheduledArrivalDate == q.Date && s.Status.IsPreCheckIn()),
            stays.Count(s => s.ScheduledDepartureDate == q.Date && s.Status.IsOccupying()),
            roomCounts.GetValueOrDefault(HousekeepingRoomStatus.VacantClean),
            roomCounts.GetValueOrDefault(HousekeepingRoomStatus.VacantDirty),
            roomCounts.GetValueOrDefault(HousekeepingRoomStatus.OccupiedClean),
            roomCounts.GetValueOrDefault(HousekeepingRoomStatus.OccupiedDirty),
            roomCounts.GetValueOrDefault(HousekeepingRoomStatus.OutOfOrder),
            roomStatusHistoryCoverage,
            isRoomStatusHistorical));
    }

    private async Task<(Dictionary<HousekeepingRoomStatus, int> Counts, int Coverage, bool IsHistorical)> GetRoomStatusCountsAtAsync(
        Guid propertyId,
        DateTime endExclusiveUtc,
        CancellationToken ct)
    {
        var history = db.RoomHousekeepingStatusHistories.AsNoTracking()
            .Where(x => x.PropertyId == propertyId && x.OccurredAtUtc < endExclusiveUtc);
        var latestPerRoom = history
            .GroupBy(x => x.HotelRoomId)
            .Select(group => new { HotelRoomId = group.Key, OccurredAtUtc = group.Max(x => x.OccurredAtUtc) });
        var coverage = await latestPerRoom.CountAsync(ct);
        if (coverage == 0)
        {
            var currentCounts = await db.RoomHousekeepingStates.AsNoTracking()
                .Where(x => x.PropertyId == propertyId)
                .GroupBy(x => x.Status)
                .Select(group => new { Status = group.Key, Count = group.Count() })
                .ToDictionaryAsync(x => x.Status, x => x.Count, ct);
            if (currentCounts.Count > 0)
                return (currentCounts, currentCounts.Values.Sum(), false);

            // Compatibility path for properties created before the operational
            // state table was backfilled. Do not report zeroes when the room
            // master already contains an explicit housekeeping status.
            var roomStatuses = await db.HotelRooms.AsNoTracking()
                .Where(x => x.PropertyId == propertyId && x.IsActive)
                .Select(x => new { x.OperationalStatus, x.HousekeepingStatus })
                .ToListAsync(ct);
            var fallbackCounts = roomStatuses
                .Select(x => x.OperationalStatus switch
                {
                    HotelRoomOperationalStatus.OutOfOrder => HousekeepingRoomStatus.OutOfOrder,
                    HotelRoomOperationalStatus.OutOfService => HousekeepingRoomStatus.OutOfService,
                    _ => ParseRoomHousekeepingStatus(x.HousekeepingStatus)
                })
                .Where(x => x.HasValue)
                .GroupBy(x => x!.Value)
                .ToDictionary(group => group.Key, group => group.Count());
            return (fallbackCounts, fallbackCounts.Values.Sum(), false);
        }

        var counts = await (
            from item in history
            join latest in latestPerRoom
                on new { item.HotelRoomId, item.OccurredAtUtc }
                equals new { latest.HotelRoomId, latest.OccurredAtUtc }
            group item by item.ToStatus into statusGroup
            select new { Status = statusGroup.Key, Count = statusGroup.Count() })
            .ToDictionaryAsync(x => x.Status, x => x.Count, ct);
        return (counts, coverage, true);
    }

    private static HousekeepingRoomStatus? ParseRoomHousekeepingStatus(string? value)
        => value switch
        {
            "Clean" => HousekeepingRoomStatus.VacantClean,
            "Dirty" => HousekeepingRoomStatus.VacantDirty,
            "Cleaning" => HousekeepingRoomStatus.Cleaning,
            "Inspected" => HousekeepingRoomStatus.Inspected,
            "OutOfOrder" => HousekeepingRoomStatus.OutOfOrder,
            "OutOfService" => HousekeepingRoomStatus.OutOfService,
            _ => null
        };

    private Result<bool> ValidateScope(Guid propertyId)
    {
        if (!auth.HasPermission("FrontOffice.Reporting.View")) return Result<bool>.Failure(Error.Authorization("REPORT_PERMISSION_DENIED", ""));
        var current = ctx.Current;
        if (current is null) return Result<bool>.Failure(Error.Authorization("REPORT_CONTEXT_REQUIRED", ""));
        return current.Property.PropertyId.Value == propertyId
            ? Result<bool>.Success(true)
            : Result<bool>.Failure(Error.Authorization("REPORT_PROPERTY_SCOPE_MISMATCH", ""));
    }

    private static (DateTime StartUtc, DateTime NextStartUtc) GetBusinessDateUtcRange(DateOnly date, string timeZoneId)
    {
        var timeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
        var localStart = DateTime.SpecifyKind(date.ToDateTime(TimeOnly.MinValue), DateTimeKind.Unspecified);
        var localEnd = DateTime.SpecifyKind(date.AddDays(1).ToDateTime(TimeOnly.MinValue), DateTimeKind.Unspecified);
        return (ConvertLocalBoundaryToUtc(localStart, timeZone), ConvertLocalBoundaryToUtc(localEnd, timeZone));
    }

    private static DateTime ConvertLocalBoundaryToUtc(DateTime localBoundary, TimeZoneInfo timeZone)
    {
        if (timeZone.IsInvalidTime(localBoundary))
            throw new ArgumentException("Business-date boundary falls in a daylight-saving gap.", nameof(localBoundary));
        if (timeZone.IsAmbiguousTime(localBoundary))
        {
            var firstOccurrenceOffset = timeZone.GetAmbiguousTimeOffsets(localBoundary).Max();
            return new DateTimeOffset(localBoundary, firstOccurrenceOffset).UtcDateTime;
        }
        return TimeZoneInfo.ConvertTimeToUtc(localBoundary, timeZone);
    }

    private async Task<Result<bool>> ValidateRoomRevenueBusinessDatesAsync(Guid propertyId, CancellationToken ct)
    {
        var hasUndatedRoomCharge = await db.HotelFolioEntries.AsNoTracking().AnyAsync(entry =>
            entry.PropertyId == propertyId
            && !entry.BusinessDate.HasValue
            && (entry.EntryType == HotelFolioEntryType.RoomCharge
                || (entry.EntryType == HotelFolioEntryType.Correction
                    && db.HotelFolioEntries.Any(original => original.HotelFolioEntryId == entry.ReversesEntryId
                        && original.PropertyId == propertyId
                        && original.EntryType == HotelFolioEntryType.RoomCharge))), ct);
        return hasUndatedRoomCharge
            ? Result<bool>.Failure(Error.Validation("REPORT_ROOM_REVENUE_BUSINESS_DATE_MISSING", "Có bút toán doanh thu phòng thiếu ngày hạch toán; báo cáo công suất bị chặn để tránh ADR/RevPAR thiếu. Cần đối chiếu và backfill có lưu vết kiểm toán."))
            : Result<bool>.Success(true);
    }

    private async Task<Result<bool>> ValidateRevenueBusinessDatesAsync(Guid propertyId, CancellationToken ct)
    {
        var hasUndatedEntry = await db.HotelFolioEntries.AsNoTracking()
            .AnyAsync(entry => entry.PropertyId == propertyId && !entry.BusinessDate.HasValue, ct);
        return hasUndatedEntry
            ? Result<bool>.Failure(Error.Validation("REPORT_FOLIO_BUSINESS_DATE_MISSING", "Có bút toán Folio thiếu ngày hạch toán; báo cáo doanh thu bị chặn để tránh tổng thiếu. Cần đối chiếu và backfill có lưu vết kiểm toán."))
            : Result<bool>.Success(true);
    }

}
