using ASMILE.BO.Core.FrontOffice.NightAudit;
using ASMILE.BO.Core.FrontOffice.Folio;
using ASMILE.BO.Core.FrontOffice.Foundation.Identifiers;
using ASMILE.BO.Core.FrontOffice.Reservation;
using ASMILE.BO.Infrastructure.FrontOffice.NightAudit;
using ASMILE.BO.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace ASMILE.BO.Tests.Unit;

public sealed class NightAuditRepositoryTests
{
    [Fact]
    public async Task Current_audit_query_returns_persisted_step_progress()
    {
        var propertyId = Guid.NewGuid();
        var now = new DateTime(2026, 9, 15, 10, 0, 0, DateTimeKind.Utc);
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        await using var db = new AppDbContext(options);
        var audit = NightAudit.Begin(propertyId, DateOnly.FromDateTime(now), "NA-TEST-001", now, null);
        db.Set<NightAudit>().Add(audit);
        await db.SaveChangesAsync();

        audit.CompleteStep(NightAuditStep.LockBusinessDate, "locked", now.AddMinutes(1));
        await db.SaveChangesAsync();

        var current = await new EfNightAuditRepository(db).GetCurrentAsync(propertyId, default);

        current.Should().NotBeNull();
        current!.CompletedSteps.Should().Be(1);
        current.TotalSteps.Should().Be(Enum.GetValues<NightAuditStep>().Length);
    }

    [Fact]
    public async Task Financial_reader_aggregates_real_folio_ledger_without_cross_property_rows()
    {
        var propertyId = Guid.NewGuid();
        var otherPropertyId = Guid.NewGuid();
        var businessDate = new DateOnly(2026, 9, 15);
        var now = businessDate.ToDateTime(new TimeOnly(10, 0));
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        await using var db = new AppDbContext(options);
        var folio = HotelFolio.Create(propertyId, Guid.NewGuid(), "F-001", now, null);
        folio.AddRoomCharge(100_000, "Room", businessDate, null, now, null);
        folio.AddRestaurantCharge(50_000, "Breakfast", null, now, null);
        folio.AddTax(10_000, "VAT", now, null);
        folio.AddPayment(80_000, "Cash", null, null, now, null);
        folio.AddRefund(20_000, "Refund", now, null);
        var otherPropertyFolio = HotelFolio.Create(otherPropertyId, Guid.NewGuid(), "F-002", now, null);
        otherPropertyFolio.AddRoomCharge(999_000, "Other property", businessDate, null, now, null);
        db.HotelFolios.AddRange(folio, otherPropertyFolio);
        await db.SaveChangesAsync();

        var metrics = await new EfNightAuditFinancialReader(db).GetAsync(propertyId, businessDate, default);

        metrics.RoomRevenue.Should().Be(100_000);
        metrics.OtherRevenue.Should().Be(50_000);
        metrics.TotalTax.Should().Be(10_000);
        metrics.TotalPayments.Should().Be(80_000);
        metrics.TotalRefunds.Should().Be(20_000);
        metrics.OutstandingBalance.Should().Be(100_000);
        metrics.RoomNightsSold.Should().Be(1);
    }

    [Fact]
    public async Task Reservation_metrics_reader_counts_real_outcomes_for_arrivals_and_property()
    {
        var propertyId = Guid.NewGuid();
        var otherPropertyId = Guid.NewGuid();
        var businessDate = new DateOnly(2026, 9, 15);
        var now = businessDate.ToDateTime(new TimeOnly(10, 0), DateTimeKind.Utc);
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        var noShow = Reservation.Create(
            "RES-NS-001", new PropertyId(propertyId), Guid.NewGuid(), "No Show Guest",
            StayPeriod.Create(businessDate, businessDate.AddDays(2)), GuestCount.Create(1, 0),
            Guid.NewGuid(), "Direct", now, null);
        noShow.MarkTentative(now, null);
        noShow.Confirm(now, null);
        noShow.MarkNoShow(now, null);

        var cancelled = Reservation.Create(
            "RES-CX-001", new PropertyId(propertyId), Guid.NewGuid(), "Cancelled Guest",
            StayPeriod.Create(businessDate, businessDate.AddDays(1)), GuestCount.Create(1, 0),
            Guid.NewGuid(), "Direct", now, null);
        cancelled.Cancel("Guest request", now, null);

        var differentArrival = Reservation.Create(
            "RES-CX-002", new PropertyId(propertyId), Guid.NewGuid(), "Different Date Guest",
            StayPeriod.Create(businessDate.AddDays(1), businessDate.AddDays(2)), GuestCount.Create(1, 0),
            Guid.NewGuid(), "Direct", now, null);
        differentArrival.Cancel("Guest request", now, null);

        var otherProperty = Reservation.Create(
            "RES-NS-002", new PropertyId(otherPropertyId), Guid.NewGuid(), "Other Property Guest",
            StayPeriod.Create(businessDate, businessDate.AddDays(1)), GuestCount.Create(1, 0),
            Guid.NewGuid(), "Direct", now, null);
        otherProperty.MarkTentative(now, null);
        otherProperty.Confirm(now, null);
        otherProperty.MarkNoShow(now, null);

        await using var db = new AppDbContext(options);
        db.FrontOfficeReservations.AddRange(noShow, cancelled, differentArrival, otherProperty);
        await db.SaveChangesAsync();

        var metrics = await new EfNightAuditReservationMetricsReader(db).GetAsync(propertyId, businessDate, default);

        metrics.NoShows.Should().Be(1);
        metrics.Cancellations.Should().Be(1);
    }
}
