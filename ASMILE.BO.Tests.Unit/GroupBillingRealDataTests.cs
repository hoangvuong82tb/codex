using ASMILE.BO.Application.FrontOffice.Foundation.Contexts;
using ASMILE.BO.Core.FrontOffice.Foundation.Identifiers;
using ASMILE.BO.Core.FrontOffice.Folio;
using ASMILE.BO.Core.FrontOffice.Group;
using ASMILE.BO.Core.FrontOffice.Payment;
using ASMILE.BO.Core.FrontOffice.Reservation;
using ASMILE.BO.Core.FrontOffice.Stay;
using ASMILE.BO.Infrastructure.FrontOffice.Foundation;
using ASMILE.BO.Infrastructure.FrontOffice.Group;
using ASMILE.BO.Infrastructure.Persistence;
using ASMILE.BO.Tests.Unit.TestSupport;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace ASMILE.BO.Tests.Unit;

public sealed class GroupBillingRealDataTests
{
    [Fact]
    public async Task Deposit_schedule_round_trip_uses_real_group_persistence()
    {
        var propertyId = Guid.NewGuid();
        var databaseName = $"group-deposit-schedule-{Guid.NewGuid():N}";
        var now = new DateTime(2026, 9, 8, 8, 0, 0, DateTimeKind.Utc);
        var groupId = Guid.Empty;

        await using (var db = TestDbContextFactory.CreateContext(null, databaseName, seedAccountingCore: false))
        {
            var group = GroupReservation.Create(
                propertyId,
                "GRP-SCHEDULE-REAL",
                "Đoàn lịch đặt cọc thật",
                new DateOnly(2026, 10, 20),
                new DateOnly(2026, 10, 22),
                2,
                4,
                0,
                now,
                null);
            var schedules = new[]
            {
                GroupDepositSchedule.Create(propertyId, group.GroupReservationId, new DateOnly(2026, 9, 20), 30m, now, null),
                GroupDepositSchedule.Create(propertyId, group.GroupReservationId, new DateOnly(2026, 10, 1), 70m, now, null)
            };
            group.ConfigureDepositPolicy(true, "Bank Transfer", new DateOnly(2026, 10, 13), new DateOnly(2026, 10, 22), schedules, now, null);

            db.FrontOfficeGroupReservations.Add(group);
            db.FrontOfficeGroupDepositSchedules.AddRange(schedules);
            await db.SaveChangesAsync();
            groupId = group.GroupReservationId;
        }

        await using (var db = TestDbContextFactory.CreateContext(null, databaseName, seedAccountingCore: false))
        {
            var reloaded = await db.FrontOfficeGroupReservations
                .Include(x => x.DepositSchedules)
                .SingleAsync(x => x.GroupReservationId == groupId);

            reloaded.DepositRequired.Should().BeTrue();
            reloaded.GuaranteeType.Should().Be("Bank Transfer");
            reloaded.DepositSchedules.Should().HaveCount(2);
            reloaded.DepositSchedules.Sum(x => x.Percent).Should().Be(100m);
            reloaded.DepositSchedules.Should().OnlyContain(x => x.GroupReservationId == groupId && x.PropertyId == propertyId);
        }
    }

    [Fact]
    public async Task Billing_summary_reads_persisted_service_and_deposit_ledgers()
    {
        var propertyId = Guid.NewGuid();
        var now = new DateTime(2026, 9, 8, 8, 0, 0, DateTimeKind.Utc);
        await using var db = TestDbContextFactory.CreateContext(null, seedAccountingCore: false);
        var group = GroupReservation.Create(
            propertyId,
            "GRP-REAL-001",
            "Đoàn dữ liệu thật",
            new DateOnly(2026, 9, 10),
            new DateOnly(2026, 9, 12),
            2,
            4,
            0,
            now,
            null);
        group.RecordDeposit(150m, now, null);
        var serviceOrder = GroupServiceOrder.Create(
            propertyId,
            group.GroupReservationId,
            "CONF-01",
            "Phòng hội nghị",
            "Meeting",
            new DateOnly(2026, 9, 10),
            new TimeOnly(9, 0),
            120,
            2,
            100m,
            0m,
            0m,
            0m,
            "MasterFolio",
            now,
            null);
        var deposit = GroupDeposit.Create(
            propertyId,
            group.GroupReservationId,
            150m,
            "VND",
            "Cash",
            new DateOnly(2026, 9, 8),
            "DEP-REAL-001",
            "Đặt cọc kiểm thử",
            now,
            null);

        db.FrontOfficeGroupReservations.Add(group);
        db.FrontOfficeGroupServiceOrders.Add(serviceOrder);
        db.FrontOfficeGroupDeposits.Add(deposit);
        await db.SaveChangesAsync();

        var accessor = new FrontOfficeExecutionContextAccessor();
        using var scope = accessor.BeginScope(CreateContext(propertyId));
        var service = new EfGroupBillingService(db, accessor);

        var summary = await service.GetBillingSummaryAsync(group.GroupReservationId, CancellationToken.None);

        summary.TotalCharges.Should().Be(200m);
        summary.TotalPayments.Should().Be(0m);
        summary.TotalDeposits.Should().Be(150m);
        summary.Outstanding.Should().Be(50m);
        summary.IsBalanced.Should().BeFalse();
    }

    [Fact]
    public async Task Revenue_dashboard_classifies_persisted_group_service_by_category()
    {
        var propertyId = Guid.NewGuid();
        var now = new DateTime(2026, 9, 8, 8, 0, 0, DateTimeKind.Utc);
        await using var db = TestDbContextFactory.CreateContext(null, seedAccountingCore: false);
        var group = GroupReservation.Create(
            propertyId,
            "GRP-REAL-002",
            "Đoàn doanh thu thật",
            new DateOnly(2026, 9, 10),
            new DateOnly(2026, 9, 12),
            1,
            2,
            0,
            now,
            null);
        var restaurant = GroupServiceOrder.Create(
            propertyId,
            group.GroupReservationId,
            "BF-01",
            "Bữa sáng",
            "Breakfast",
            new DateOnly(2026, 9, 10),
            new TimeOnly(7, 0),
            60,
            2,
            40m,
            0m,
            0m,
            0m,
            "MasterFolio",
            now,
            null);
        var other = GroupServiceOrder.Create(
            propertyId,
            group.GroupReservationId,
            "TR-01",
            "Xe đưa đón",
            "Transport",
            new DateOnly(2026, 9, 10),
            new TimeOnly(8, 0),
            60,
            1,
            60m,
            0m,
            0m,
            0m,
            "MasterFolio",
            now,
            null);

        db.FrontOfficeGroupReservations.Add(group);
        db.FrontOfficeGroupServiceOrders.AddRange(restaurant, other);
        await db.SaveChangesAsync();

        var accessor = new FrontOfficeExecutionContextAccessor();
        using var scope = accessor.BeginScope(CreateContext(propertyId));
        var dashboard = await new EfGroupBillingService(db, accessor)
            .GetRevenueDashboardAsync(group.GroupReservationId, CancellationToken.None);

        dashboard.FbRevenue.Should().Be(80m);
        dashboard.OtherRevenue.Should().Be(60m);
        dashboard.TotalRevenue.Should().Be(140m);
    }

    [Fact]
    public async Task Billing_summary_keeps_discount_and_deposit_out_of_payment_total()
    {
        var propertyId = Guid.NewGuid();
        var now = new DateTime(2026, 9, 8, 8, 0, 0, DateTimeKind.Utc);
        await using var db = TestDbContextFactory.CreateContext(null, seedAccountingCore: false);

        var group = GroupReservation.Create(
            propertyId,
            "GRP-REAL-003",
            "Đoàn đối chiếu signed ledger",
            new DateOnly(2026, 9, 10),
            new DateOnly(2026, 9, 12),
            1,
            2,
            0,
            now,
            null);
        var reservation = Reservation.Create(
            "RES-GRP-REAL-003",
            new PropertyId(propertyId),
            null,
            "Khách đối chiếu",
            StayPeriod.Create(new DateOnly(2026, 9, 10), new DateOnly(2026, 9, 12)),
            GuestCount.Create(2, 0),
            null,
            "Group",
            now,
            null);
        var assignment = group.AddRoom(Guid.NewGuid(), "301", "STD", "Khách đối chiếu", 2, 0, now);
        assignment.LinkReservation(reservation.ReservationId);
        var stay = HotelStay.Prepare(
            "STY-GRP-REAL-003",
            new PropertyId(propertyId),
            reservation.ReservationId,
            reservation.ReservationNumber,
            null,
            "Khách đối chiếu",
            null,
            assignment.HotelRoomId,
            assignment.RoomNumber,
            Guid.NewGuid(),
            assignment.RoomType,
            HotelStayPeriod.Create(new DateOnly(2026, 9, 10), new DateOnly(2026, 9, 12)),
            HotelStayGuestCount.Create(2, 0),
            "Group",
            now,
            null);
        var folio = HotelFolio.Create(propertyId, stay.HotelStayId, "FOL-GRP-REAL-003", now, null);
        folio.AddRoomCharge(1_000m, "Room", new DateOnly(2026, 9, 10), null, now, null);
        folio.AddDiscount(100m, "Group discount", now, null);
        folio.AddPayment(300m, "Advance payment", HotelPaymentMethod.Cash, null, now, null);
        folio.AddDeposit(200m, "Group deposit", now, null);
        folio.AddRefund(50m, "Refunded advance", now, null);

        db.FrontOfficeGroupReservations.Add(group);
        db.FrontOfficeGroupRoomAssignments.Add(assignment);
        db.FrontOfficeReservations.Add(reservation);
        db.HotelStays.Add(stay);
        db.HotelFolios.Add(folio);
        await db.SaveChangesAsync();

        var accessor = new FrontOfficeExecutionContextAccessor();
        using var scope = accessor.BeginScope(CreateContext(propertyId));
        var summary = await new EfGroupBillingService(db, accessor)
            .GetBillingSummaryAsync(group.GroupReservationId, CancellationToken.None);

        summary.TotalCharges.Should().Be(900m);
        summary.TotalPayments.Should().Be(250m);
        summary.TotalDeposits.Should().Be(200m);
        summary.Outstanding.Should().Be(450m);
        summary.IsBalanced.Should().BeFalse();
    }

    private static FrontOfficeExecutionContext CreateContext(Guid propertyId)
    {
        var id = new PropertyId(propertyId);
        return new FrontOfficeExecutionContext(
            new ActorContext(ActorId.New(), new HashSet<string> { "Reception" }, "test-session", "test-workstation", false),
            new PropertyContext(id, "Asia/Ho_Chi_Minh", "VND"),
            new BusinessDateContext(id, new DateOnly(2026, 9, 8), "Test"),
            new CorrelationContext(CorrelationId.New(), null, CommandId.New(), "test-trace"));
    }
}
