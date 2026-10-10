using ASMILE.BO.Application.FrontOffice.Concierge;
using ASMILE.BO.Application.FrontOffice.Foundation.Contexts;
using ASMILE.BO.Application.FrontOffice.Foundation.Persistence;
using ASMILE.BO.Core.FrontOffice.Foundation.Identifiers;
using ASMILE.BO.Infrastructure.FrontOffice.Concierge;
using ASMILE.BO.Infrastructure.FrontOffice.Foundation;
using ASMILE.BO.Infrastructure.Persistence;
using ASMILE.BO.Shared.Foundation;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace ASMILE.BO.Tests.Unit;

public sealed class ServiceRequestPersistenceTests
{
    [Fact]
    public async Task SR_001_create_query_and_transition_use_real_persistence()
    {
        var propertyId = PropertyId.New();
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .Options);
        await db.Database.EnsureCreatedAsync();

        var accessor = new FrontOfficeExecutionContextAccessor();
        var clock = new FixedClock(new DateTime(2026, 9, 8, 8, 0, 0, DateTimeKind.Utc));
        var persistence = new EfFrontOfficePersistenceSession(
            db,
            accessor,
            new PassthroughIntegrationEventMapper(),
            new CanonicalOutboxMessageFactory(clock),
            new FrontOfficeAuditWriter(),
            clock);
        var service = new EfServiceRequestService(db, persistence, accessor, clock);
        using var scope = accessor.BeginScope(CreateContext(propertyId));

        var created = await service.CreateAsync(new CreateServiceRequestRequest(
            null,
            null,
            null,
            "405",
            "Bùi Thanh Hà",
            ServiceType.AirportTransfer,
            ServicePriority.High,
            "Đặt xe sân bay",
            null,
            300000,
            "ImmediateCharge"), CancellationToken.None);

        created.RequestNumber.Should().StartWith("SRV-");
        created.RoomNumber.Should().Be("405");
        created.SlaDeadlineAtUtc.Should().Be(clock.UtcNow.AddHours(1));
        (await service.GetAllAsync(propertyId.Value, CancellationToken.None))
            .Should().ContainSingle(x => x.RequestId == created.RequestId);

        var completed = await service.TransitionAsync(
            created.RequestId,
            ServiceStatus.Completed,
            "Đã hoàn tất",
            CancellationToken.None);

        completed.Status.Should().Be(ServiceStatus.Completed);
        completed.SlaStatus.Should().Be(SlaStatus.OnTrack);
        var terminalTransition = () => service.TransitionAsync(
            created.RequestId,
            ServiceStatus.Cancelled,
            "Khách hủy sau khi đã hoàn tất",
            CancellationToken.None);
        await terminalTransition.Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage("SERVICE_REQUEST_TERMINAL_STATUS_CANNOT_CANCEL");
        var sla = await service.GetSlaReportAsync(propertyId.Value, CancellationToken.None);
        sla.Should().ContainSingle(x => x.Type == ServiceType.AirportTransfer && x.Completed == 1 && x.OnTime == 1 && x.Breached == 0);
        (await db.FrontOfficeServiceRequests.CountAsync()).Should().Be(1);
        (await db.FrontOfficeServiceHistories.CountAsync()).Should().Be(1);
        (await db.AuditLogs.AnyAsync(x => x.Action == "ServiceRequestCreated")).Should().BeTrue();
        (await db.AuditLogs.AnyAsync(x => x.Action == "ServiceRequestStatusChanged")).Should().BeTrue();
    }

    private static FrontOfficeExecutionContext CreateContext(PropertyId propertyId) => new(
        new ActorContext(ActorId.New(), new HashSet<string> { "Reception" }, "session", "workstation", false),
        new PropertyContext(propertyId, "Asia/Ho_Chi_Minh", "VND"),
        new BusinessDateContext(propertyId, new DateOnly(2026, 9, 8), "Test"),
        new CorrelationContext(CorrelationId.New(), null, CommandId.New(), "trace"));

    private sealed class FixedClock(DateTime utcNow) : IClock
    {
        public DateTime UtcNow { get; } = utcNow;
    }
}
