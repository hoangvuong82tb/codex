using ASMILE.BO.Application.FrontOffice.Foundation.Contexts;
using ASMILE.BO.Application.FrontOffice.Reporting;
using ASMILE.BO.Application.Interfaces;
using ASMILE.BO.Core.FrontOffice.Cashier;
using ASMILE.BO.Core.FrontOffice.Foundation.Identifiers;
using ASMILE.BO.Core.FrontOffice.Folio;
using ASMILE.BO.Core.FrontOffice.HotelMasterData;
using ASMILE.BO.Core.FrontOffice.Housekeeping;
using ASMILE.BO.Core.FrontOffice.Payment;
using ASMILE.BO.Core.FrontOffice.Reservation;
using ASMILE.BO.Core.FrontOffice.Stay;
using ASMILE.BO.Core.Security;
using ASMILE.BO.Infrastructure.FrontOffice.Foundation;
using ASMILE.BO.Infrastructure.FrontOffice.Reporting;
using ASMILE.BO.Tests.Unit.TestSupport;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace ASMILE.BO.Tests.Unit;

public sealed class FrontOfficeReportingQueryServiceTests
{
    [Fact]
    public async Task Revenue_report_includes_payments_and_refunds_by_property_business_date()
    {
        var propertyId = Guid.NewGuid();
        var businessDate = new DateOnly(2026, 9, 21);
        var stayDate = businessDate.AddDays(1);
        var now = new DateTime(2026, 9, 21, 12, 0, 0, DateTimeKind.Utc);
        await using var db = TestDbContextFactory.CreateContext(null, seedAccountingCore: false);

        var reservation = Reservation.Create(
            "RPT-RES-001",
            new PropertyId(propertyId),
            null,
            "Reporting guest",
            StayPeriod.Create(stayDate, stayDate.AddDays(1)),
            GuestCount.Create(1, 0),
            null,
            "Direct",
            now,
            null);
        var stay = HotelStay.Prepare(
            "RPT-STY-001",
            new PropertyId(propertyId),
            reservation.ReservationId,
            reservation.ReservationNumber,
            null,
            "Reporting guest",
            null,
            Guid.NewGuid(),
            "101",
            Guid.NewGuid(),
            "STD",
            HotelStayPeriod.Create(stayDate, stayDate.AddDays(1)),
            HotelStayGuestCount.Create(1, 0),
            "Direct",
            now,
            null);
        var folio = HotelFolio.Create(propertyId, stay.HotelStayId, "RPT-FOL-001", now, null);
        folio.AddRoomCharge(200m, "Room", businessDate, null, now, null);
        folio.AddRestaurantCharge(40m, "Restaurant", null, now, null, businessDate);
        folio.AddPayment(100m, "Cash payment", HotelPaymentMethod.Cash, null, now, null, businessDate);
        var voidedPayment = folio.AddPayment(20m, "Voided payment", HotelPaymentMethod.Cash, null, now, null, businessDate);
        folio.AddCorrection(20m, "Void payment correction", voidedPayment.HotelFolioEntryId, now, null, businessDate);
        folio.AddDeposit(50m, "Advance deposit", now, null, businessDate);
        folio.AddRefund(10m, "Partial refund", now, null, businessDate);

        db.FrontOfficeReservations.Add(reservation);
        db.HotelStays.Add(stay);
        db.HotelFolios.Add(folio);
        await db.SaveChangesAsync();

        var accessor = new FrontOfficeExecutionContextAccessor();
        using var scope = accessor.BeginScope(CreateContext(propertyId, businessDate));
        var service = new EfReportingQueryService(db, accessor, new AllowAllAuthorization());

        var result = await service.GetRevenueReportAsync(
            new RevenueReportQuery(propertyId, businessDate, businessDate),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var daily = result.Value.Should().ContainSingle().Subject;
        daily.RoomRevenue.Should().Be(200m);
        daily.OtherRevenue.Should().Be(40m);
        daily.TotalRevenue.Should().Be(240m);
        daily.TotalPayments.Should().Be(100m);
        daily.TotalDeposits.Should().Be(50m);
        daily.TotalRefunds.Should().Be(10m);
        daily.FolioBalance.Should().Be(100m);
    }

    [Fact]
    public async Task Revenue_report_fails_closed_when_legacy_folio_entry_has_no_business_date()
    {
        var propertyId = Guid.NewGuid();
        var businessDate = new DateOnly(2026, 9, 21);
        var now = new DateTime(2026, 9, 21, 12, 0, 0, DateTimeKind.Utc);
        await using var db = TestDbContextFactory.CreateContext(null, seedAccountingCore: false);

        var stay = CreateStay(propertyId, "RPT-STY-UNDATED", businessDate, businessDate.AddDays(1));
        var folio = HotelFolio.Create(propertyId, stay.HotelStayId, "RPT-FOL-UNDATED", now, null);
        folio.AddPayment(75m, "Legacy payment without business date", HotelPaymentMethod.Cash, null, now, null);
        db.HotelStays.Add(stay);
        db.HotelFolios.Add(folio);
        await db.SaveChangesAsync();

        var accessor = new FrontOfficeExecutionContextAccessor();
        using var scope = accessor.BeginScope(CreateContext(propertyId, businessDate));
        var service = new EfReportingQueryService(db, accessor, new AllowAllAuthorization());

        var result = await service.GetRevenueReportAsync(
            new RevenueReportQuery(propertyId, businessDate, businessDate),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("REPORT_FOLIO_BUSINESS_DATE_MISSING");
    }

    [Fact]
    public async Task Operational_report_separates_actual_and_expected_guest_movements_by_property_day()
    {
        var propertyId = Guid.NewGuid();
        var businessDate = new DateOnly(2026, 9, 21);
        var utcStartOfLocalDay = new DateTime(2026, 9, 20, 17, 0, 0, DateTimeKind.Utc);
        await using var db = TestDbContextFactory.CreateContext(null, seedAccountingCore: false);

        var arrived = CreateStay(propertyId, "RPT-STY-ARRIVED", businessDate, businessDate.AddDays(1));
        arrived.CheckIn(utcStartOfLocalDay.AddHours(1), null);

        var expectedArrival = CreateStay(propertyId, "RPT-STY-EXPECTED-ARRIVAL", businessDate, businessDate.AddDays(1));

        var departed = CreateStay(propertyId, "RPT-STY-DEPARTED", businessDate.AddDays(-1), businessDate.AddDays(1));
        departed.CheckIn(utcStartOfLocalDay.AddDays(-1), null);
        departed.MarkCheckedOut(utcStartOfLocalDay.AddHours(23), null);

        var expectedDeparture = CreateStay(propertyId, "RPT-STY-EXPECTED-DEPARTURE", businessDate.AddDays(-1), businessDate);
        expectedDeparture.CheckIn(utcStartOfLocalDay.AddDays(-1), null);

        db.HotelStays.AddRange(arrived, expectedArrival, departed, expectedDeparture);
        await db.SaveChangesAsync();

        var accessor = new FrontOfficeExecutionContextAccessor();
        using var scope = accessor.BeginScope(CreateContext(propertyId, businessDate));
        var service = new EfReportingQueryService(db, accessor, new AllowAllAuthorization());

        var result = await service.GetOperationalReportAsync(
            new OperationalReportQuery(propertyId, businessDate),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var daily = result.Value!;
        daily.Arrivals.Should().Be(1);
        daily.Departures.Should().Be(1);
        daily.ExpectedArrivals.Should().Be(1);
        daily.ExpectedDepartures.Should().Be(1);
    }

    [Fact]
    public async Task Operational_report_uses_last_persisted_housekeeping_status_for_the_selected_day()
    {
        var propertyId = Guid.NewGuid();
        var businessDate = new DateOnly(2026, 9, 21);
        var utcStartOfLocalDay = new DateTime(2026, 9, 20, 17, 0, 0, DateTimeKind.Utc);
        await using var db = TestDbContextFactory.CreateContext(null, seedAccountingCore: false);

        var roomType = HotelRoomType.Create(new PropertyId(propertyId), "RPT-HISTORY", "History test", null, 1, 0, 2, 1, 1, 1, utcStartOfLocalDay, null);
        var room = HotelRoom.Create(new PropertyId(propertyId), roomType.HotelRoomTypeId, "RPT-HIST-101", null, "1", null, null, null, utcStartOfLocalDay, null);
        db.HotelRoomTypes.Add(roomType);
        db.HotelRooms.Add(room);
        db.RoomHousekeepingStatusHistories.Add(RoomHousekeepingStatusHistory.Create(
            propertyId,
            room.HotelRoomId,
            HousekeepingRoomStatus.VacantClean,
            HousekeepingRoomStatus.VacantDirty,
            "MigrationBaseline",
            null,
            utcStartOfLocalDay.AddHours(1)));
        await db.SaveChangesAsync();

        var accessor = new FrontOfficeExecutionContextAccessor();
        using var scope = accessor.BeginScope(CreateContext(propertyId, businessDate));
        var service = new EfReportingQueryService(db, accessor, new AllowAllAuthorization());

        var result = await service.GetOperationalReportAsync(new OperationalReportQuery(propertyId, businessDate), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.IsRoomStatusHistorical.Should().BeTrue();
        result.Value.RoomStatusHistoryCoverage.Should().Be(1);
        result.Value.VacantDirty.Should().Be(1);
    }

    [Fact]
    public async Task Executive_dashboard_uses_persisted_cashier_data_for_requested_business_date()
    {
        var propertyId = Guid.NewGuid();
        var businessDate = new DateOnly(2026, 9, 21);
        var now = new DateTime(2026, 9, 21, 12, 0, 0, DateTimeKind.Utc);
        var actorId = ActorId.New();
        await using var db = TestDbContextFactory.CreateContext(null, seedAccountingCore: false);

        var shift = CashierShift.Open(propertyId, Guid.NewGuid(), "CS-RPT-20260921-A", businessDate, 1_000m, now, actorId);
        shift.RecordCashIn(500m, "Cash payment", Guid.NewGuid(), now, actorId);
        shift.RecordCashOut(100m, "Cash expense", now, actorId);
        shift.RecordRefund(50m, "Guest refund", Guid.NewGuid(), now, actorId);
        shift.RecordAdjustment(25m, "Approved cash adjustment", now, actorId);
        shift.Close(1_325m, 1_325m, now, actorId);

        var nextDayShift = CashierShift.Open(propertyId, Guid.NewGuid(), "CS-RPT-20260922-B", businessDate.AddDays(1), 9_000m, now.AddDays(1), actorId);
        var roomType = HotelRoomType.Create(new PropertyId(propertyId), "RPT-STD", "Report test room", null, 1, 0, 2, 1, 1, 1, now, actorId);
        var room = HotelRoom.Create(new PropertyId(propertyId), roomType.HotelRoomTypeId, "RPT-101", null, "1", null, null, null, now, actorId);
        var completedTask = HousekeepingTask.Create(propertyId, room.HotelRoomId, "HK-RPT-DONE", HousekeepingTaskType.DepartureClean, RoomPriority.Normal, now, actorId);
        var housekeeperId = Guid.NewGuid();
        completedTask.Assign(housekeeperId, now.AddHours(-2));
        completedTask.Accept(now.AddHours(-2));
        completedTask.StartCleaning(now.AddMinutes(-90));
        completedTask.CompleteCleaning(now.AddMinutes(-30));
        completedTask.Inspect(HousekeepingInspectionResult.Approved, null, now);
        var pendingTask = HousekeepingTask.Create(propertyId, room.HotelRoomId, "HK-RPT-PENDING", HousekeepingTaskType.StayoverService, RoomPriority.Normal, now, actorId);
        db.HotelRoomTypes.Add(roomType);
        db.HotelRooms.Add(room);
        db.HousekeepingTasks.AddRange(completedTask, pendingTask);
        db.CashierShifts.AddRange(shift, nextDayShift);
        await db.SaveChangesAsync();

        var accessor = new FrontOfficeExecutionContextAccessor();
        using var scope = accessor.BeginScope(CreateContext(propertyId, businessDate));
        var service = new EfReportingQueryService(db, accessor, new AllowAllAuthorization());

        var result = await service.GetDashboardAsync(new DashboardQuery(businessDate), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var cashier = result.Value!.Cashier;
        cashier.TotalCashIn.Should().Be(500m);
        cashier.TotalCashOut.Should().Be(100m);
        cashier.TotalRefund.Should().Be(50m);
        cashier.ExpectedCash.Should().Be(1_375m);
        cashier.Difference.Should().Be(-50m);
        result.Value.Housekeeping.AvgCleaningMinutes.Should().Be(60m);
        result.Value.Housekeeping.TasksPending.Should().Be(1);
        result.Value.Housekeeping.TasksCompleted.Should().Be(1);
    }

    [Fact]
    public async Task Cashier_report_uses_persisted_shifts_movements_and_reconciliation_status()
    {
        var propertyId = Guid.NewGuid();
        var businessDate = new DateOnly(2026, 9, 21);
        var now = new DateTime(2026, 9, 21, 12, 0, 0, DateTimeKind.Utc);
        var actorId = ActorId.New();
        await using var db = TestDbContextFactory.CreateContext(null, seedAccountingCore: false);

        var shift = CashierShift.Open(propertyId, Guid.NewGuid(), "CS-REPORT-001", businessDate, 1_000m, now, actorId);
        shift.RecordCashIn(500m, "Cash sale", null, now, actorId);
        shift.RecordCashOut(100m, "Petty cash", now, actorId);
        shift.RecordRefund(50m, "Refund", null, now, actorId);
        shift.RecordTransferOut(25m, "Transfer", now, actorId);
        shift.RecordAdjustment(-10m, "Counting adjustment", now, actorId);
        shift.Close(1_315m, 1_315m, now, actorId);
        var reconciliation = CashReconciliation.Create(propertyId, shift, 1_315m, Guid.NewGuid(), "CountingError", "Verified", now);
        db.CashierShifts.Add(shift);
        db.CashReconciliations.Add(reconciliation);
        await db.SaveChangesAsync();

        var accessor = new FrontOfficeExecutionContextAccessor();
        using var scope = accessor.BeginScope(CreateContext(propertyId, businessDate));
        var service = new EfReportingQueryService(db, accessor, new AllowAllAuthorization());

        var result = await service.GetCashierReportAsync(new CashierReportQuery(propertyId, businessDate, businessDate), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var row = result.Value.Should().ContainSingle().Subject;
        row.OpeningCash.Should().Be(1_000m);
        row.CashIn.Should().Be(500m);
        row.CashOut.Should().Be(100m);
        row.Refund.Should().Be(50m);
        row.TransferOut.Should().Be(25m);
        row.Adjustment.Should().Be(-10m);
        row.ExpectedCash.Should().Be(1_315m);
        row.ActualCash.Should().Be(1_315m);
        row.Difference.Should().Be(0m);
        row.ReconciliationStatus.Should().Be("Completed");
    }

    [Fact]
    public async Task Folio_business_date_gap_report_is_read_only_and_suggests_a_local_date()
    {
        var propertyId = Guid.NewGuid();
        var businessDate = new DateOnly(2026, 9, 21);
        var now = new DateTime(2026, 9, 21, 12, 0, 0, DateTimeKind.Utc);
        await using var db = TestDbContextFactory.CreateContext(null, seedAccountingCore: false);

        var stay = CreateStay(propertyId, "RPT-STY-GAP", businessDate, businessDate.AddDays(1));
        var folio = HotelFolio.Create(propertyId, stay.HotelStayId, "RPT-FOL-GAP", now, null);
        folio.AddManualCharge(125m, "Legacy manual charge", now, null);
        db.HotelStays.Add(stay);
        db.HotelFolios.Add(folio);
        await db.SaveChangesAsync();

        var accessor = new FrontOfficeExecutionContextAccessor();
        using var scope = accessor.BeginScope(CreateContext(propertyId, businessDate));
        var service = new EfReportingQueryService(db, accessor, new AllowAllAuthorization());

        var result = await service.GetFolioBusinessDateGapReportAsync(new FolioBusinessDateGapQuery(propertyId, businessDate, businessDate), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var row = result.Value.Should().ContainSingle().Subject;
        row.Amount.Should().Be(125m);
        row.SuggestedBusinessDate.Should().Be(businessDate);
        (await db.HotelFolioEntries.SingleAsync()).BusinessDate.Should().BeNull("the review report must not change accounting data");
    }

    private static HotelStay CreateStay(Guid propertyId, string stayNumber, DateOnly arrival, DateOnly departure)
    {
        var now = new DateTime(2026, 9, 20, 12, 0, 0, DateTimeKind.Utc);
        return HotelStay.Prepare(
            stayNumber,
            new PropertyId(propertyId),
            null,
            null,
            null,
            stayNumber,
            null,
            Guid.NewGuid(),
            stayNumber,
            Guid.NewGuid(),
            "STD",
            HotelStayPeriod.Create(arrival, departure),
            HotelStayGuestCount.Create(1, 0),
            "Direct",
            now,
            null);
    }

    private static FrontOfficeExecutionContext CreateContext(Guid propertyId, DateOnly businessDate)
    {
        var id = new PropertyId(propertyId);
        return new FrontOfficeExecutionContext(
            new ActorContext(ActorId.New(), new HashSet<string> { "Reception" }, "test-session", "test-workstation", false),
            new PropertyContext(id, "Asia/Ho_Chi_Minh", "VND"),
            new BusinessDateContext(id, businessDate, "Test"),
            new CorrelationContext(CorrelationId.New(), null, CommandId.New(), "report-test"));
    }

    private sealed class AllowAllAuthorization : IAuthorizationService
    {
        public bool HasPermission(string permissionCode) => true;
        public void Demand(string permissionCode) { }
        public Task<bool> CanAccessAsync(string permissionCode) => Task.FromResult(true);
        public bool HasPermission(Permission permission) => true;
        public void Demand(Permission permission) { }
        public Task<bool> ConfirmDangerousActionAsync(string title, string message) => Task.FromResult(true);
    }
}
