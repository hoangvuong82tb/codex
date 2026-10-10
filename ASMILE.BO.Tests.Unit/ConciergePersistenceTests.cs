using ASMILE.BO.Application.FrontOffice.Concierge;
using ASMILE.BO.Application.FrontOffice.Foundation.Audit;
using ASMILE.BO.Application.FrontOffice.Foundation.Contexts;
using ASMILE.BO.Core.FrontOffice.Foundation.Identifiers;
using ASMILE.BO.Infrastructure.FrontOffice.Concierge;
using ASMILE.BO.Infrastructure.FrontOffice.Foundation;
using ASMILE.BO.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace ASMILE.BO.Tests.Unit;

public sealed class ConciergePersistenceTests
{
    [Fact]
    public async Task CON_REAL_001_wakeup_luggage_transport_and_dashboard_use_real_property_scoped_data()
    {
        var propertyId = PropertyId.New();
        var otherPropertyId = PropertyId.New();
        var guestId = Guid.NewGuid();
        var now = new DateTime(2026, 9, 8, 8, 0, 0, DateTimeKind.Utc);
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options);
        await db.Database.EnsureCreatedAsync();

        var accessor = new FrontOfficeExecutionContextAccessor();
        var clock = new FixedClock(now);
        var persistence = new EfFrontOfficePersistenceSession(
            db,
            accessor,
            new PassthroughIntegrationEventMapper(),
            new CanonicalOutboxMessageFactory(clock),
            new FrontOfficeAuditWriter(),
            clock);
        using var scope = accessor.BeginScope(CreateContext(propertyId));

        var wakeups = new EfWakeUpCallService(db, persistence, accessor, clock);
        var luggage = new EfLuggageService(db, persistence, accessor, clock);
        var transport = new EfTransportationService(db, persistence, accessor, clock);
        var dashboard = new EfConciergeDashboardService(db, accessor, clock);

        var wake = await wakeups.ScheduleAsync(Guid.NewGuid(), null, now.AddHours(2), "Gọi phòng 101", CancellationToken.None);
        wake.Status.Should().Be(WakeUpCallStatus.Scheduled);
        (await wakeups.GetScheduledAsync(propertyId.Value, now, CancellationToken.None)).Should().ContainSingle(x => x.CallId == wake.CallId);
        (await wakeups.CompleteAsync(wake.CallId, WakeUpCallStatus.NoAnswer, CancellationToken.None)).Status.Should().Be(WakeUpCallStatus.NoAnswer);
        (await wakeups.RetryAsync(wake.CallId, CancellationToken.None)).RetryCount.Should().Be(1);
        (await wakeups.CompleteAsync(wake.CallId, WakeUpCallStatus.Completed, CancellationToken.None)).Status.Should().Be(WakeUpCallStatus.Completed);

        var stored = await luggage.StoreAsync(new LuggageItemDto(Guid.Empty, "", guestId, "Khách thật", null, "101", 2, "Vali", "A-01", LuggageStatus.Stored, default, null, null, null, null), CancellationToken.None);
        stored.Status.Should().Be(LuggageStatus.Stored);
        (await luggage.GetByGuestAsync(guestId, CancellationToken.None)).Should().ContainSingle(x => x.ItemId == stored.ItemId);
        (await luggage.DeliverAsync(stored.ItemId, Guid.NewGuid(), CancellationToken.None)).Status.Should().Be(LuggageStatus.Delivered);
        (await luggage.CollectAsync(stored.ItemId, CancellationToken.None)).Status.Should().Be(LuggageStatus.Collected);

        var ride = await transport.BookAsync(new TransportationDto(Guid.Empty, "", TransportType.AirportPickup, guestId, "Khách thật", null, "101", null, null, null,
            now.AddHours(3), "Sân bay", "Khách sạn", "VN123", 2, ServiceStatus.Requested, 350000m, null), CancellationToken.None);
        (await transport.GetUpcomingAsync(propertyId.Value, CancellationToken.None)).Should().ContainSingle(x => x.BookingId == ride.BookingId);
        (await transport.CompleteAsync(ride.BookingId, CancellationToken.None)).Status.Should().Be(ServiceStatus.Completed);

        var summary = await dashboard.GetDashboardAsync(propertyId.Value, CancellationToken.None);
        summary.WakeupsScheduled.Should().Be(0);
        summary.LuggageStored.Should().Be(0);
        summary.TransportBookings.Should().Be(0);

        var breachedRequest = ASMILE.BO.Core.FrontOffice.ServiceRequest.ServiceRequest.Create(
            propertyId.Value, "SRV-BREACHED", "BellService", ASMILE.BO.Core.FrontOffice.ServiceRequest.ServiceCategory.FrontOffice,
            ASMILE.BO.Core.FrontOffice.ServiceRequest.ServicePriority.Urgent, "Mang hành lý", null, null, "101", "Khách quá hạn",
            ASMILE.BO.Core.FrontOffice.ServiceRequest.ChargeType.Complimentary, null, now.AddHours(-1), now.AddHours(-2), null);
        var completedRequest = ASMILE.BO.Core.FrontOffice.ServiceRequest.ServiceRequest.Create(
            propertyId.Value, "SRV-COMPLETED", "Taxi", ASMILE.BO.Core.FrontOffice.ServiceRequest.ServiceCategory.Transportation,
            ASMILE.BO.Core.FrontOffice.ServiceRequest.ServicePriority.Normal, "Đặt taxi", null, null, "101", "Khách đã phục vụ",
            ASMILE.BO.Core.FrontOffice.ServiceRequest.ChargeType.ImmediateCharge, 100000m, now.AddHours(1), now.AddMinutes(-10), null);
        completedRequest.Assign("Concierge", null, now.AddMinutes(-5));
        completedRequest.Complete(100000m, now);
        db.FrontOfficeServiceRequests.AddRange(breachedRequest, completedRequest);
        await db.SaveChangesAsync();

        var dashboardWithRequests = await dashboard.GetDashboardAsync(propertyId.Value, CancellationToken.None);
        dashboardWithRequests.OpenRequests.Should().Be(1);
        dashboardWithRequests.Escalated.Should().Be(1);
        dashboardWithRequests.AvgResponseMinutes.Should().Be(5m);
        dashboardWithRequests.SlaCompliancePct.Should().Be(100m);
        dashboardWithRequests.UrgentRequests.Should().ContainSingle(x => x.RequestNumber == "SRV-BREACHED");
        dashboardWithRequests.BreachedSla.Should().ContainSingle(x => x.RequestNumber == "SRV-BREACHED");
        var performance = await dashboard.GetPerformanceAsync(propertyId.Value, CancellationToken.None);
        performance.Should().Contain(x => x.Type == ServiceType.Taxi && x.AvgMinutes == 10m && x.SlaCompliance == 100m);

        ConciergeLuggageItemForOtherProperty(db, otherPropertyId, guestId, now);
        (await luggage.GetByGuestAsync(guestId, CancellationToken.None)).Should().ContainSingle(x => x.ItemId == stored.ItemId);
    }

    private static void ConciergeLuggageItemForOtherProperty(AppDbContext db, PropertyId propertyId, Guid guestId, DateTime now)
    {
        db.ConciergeLuggageItems.Add(ASMILE.BO.Core.FrontOffice.Concierge.ConciergeLuggageItem.Create(
            propertyId.Value, "OTHER-001", guestId, "Khách khác", null, "999", 1, null, "B-01", now));
        db.SaveChanges();
    }

    private static FrontOfficeExecutionContext CreateContext(PropertyId propertyId) => new(
        new ActorContext(ActorId.New(), new HashSet<string> { "Concierge" }, "session", "workstation", false),
        new PropertyContext(propertyId, "Asia/Ho_Chi_Minh", "VND"),
        new BusinessDateContext(propertyId, new DateOnly(2026, 9, 8), "Test"),
        new CorrelationContext(CorrelationId.New(), null, CommandId.New(), "trace"));

    private sealed class FixedClock(DateTime utcNow) : IClock
    {
        public DateTime UtcNow { get; } = utcNow;
    }
}
