using ASMILE.BO.Application.FrontOffice.Foundation.Contexts;
using ASMILE.BO.Application.FrontOffice.Foundation.Persistence;
using ASMILE.BO.Application.FrontOffice.Incident;
using ASMILE.BO.Core.FrontOffice.Foundation.Identifiers;
using ASMILE.BO.Infrastructure.FrontOffice.Foundation;
using ASMILE.BO.Infrastructure.FrontOffice.Incident;
using ASMILE.BO.Infrastructure.Persistence;
using ASMILE.BO.Shared.Foundation;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace ASMILE.BO.Tests.Unit;

public sealed class IncidentTests
{
    [Fact]
    public void INC_001_domain_lifecycle_rejects_invalid_close_order()
    {
        var now = new DateTime(2026, 9, 7, 8, 0, 0, DateTimeKind.Utc);
        var incident = Core.FrontOffice.Incident.FrontOfficeIncident.Create(
            Guid.NewGuid(), "INC-TEST-001", now, "Door lock failed", "Door Lock",
            Core.FrontOffice.Incident.IncidentPriority.High, "Reception", "Room", "Guest cannot enter", now, null);

        var act = () => incident.Close("Preventive action", now, null);
        act.Should().Throw<InvalidOperationException>();

        incident.StartInvestigation(now, null);
        incident.Resolve("Battery exhausted", "Issued a new key", now, null);
        incident.Close("Add battery inspection to checklist", now, null);
        incident.Status.Should().Be(Core.FrontOffice.Incident.IncidentStatus.Closed);
        incident.ResolvedAtUtc.Should().Be(now);
        incident.ClosedAtUtc.Should().Be(now);
    }

    [Fact]
    public async Task INC_002_persistent_create_query_and_status_round_trip_uses_real_context()
    {
        var propertyId = PropertyId.New();
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;
        await using var db = new AppDbContext(options);
        await db.Database.EnsureCreatedAsync();

        var accessor = new FrontOfficeExecutionContextAccessor();
        var clock = new FixedClock(new DateTime(2026, 9, 7, 8, 0, 0, DateTimeKind.Utc));
        var audit = new FrontOfficeAuditWriter();
        var persistence = new EfFrontOfficePersistenceSession(
            db,
            accessor,
            new PassthroughIntegrationEventMapper(),
            new CanonicalOutboxMessageFactory(clock),
            audit,
            clock);
        var service = new EfIncidentService(db, persistence, accessor, clock);
        using var scope = accessor.BeginScope(CreateContext(propertyId));

        var created = await service.CreateAsync(new CreateIncidentRequest(
            clock.UtcNow,
            "Khóa cửa không hoạt động",
            "Door Lock",
            Core.FrontOffice.Incident.IncidentPriority.High,
            "405",
            "Nguyễn Văn A",
            "RES-001",
            "FO",
            "Room",
            "Khách không vào được phòng",
            false), CancellationToken.None);

        var rows = await service.SearchAsync(new IncidentSearchRequest(propertyId.Value), CancellationToken.None);
        rows.Should().ContainSingle(x => x.IncidentId == created.IncidentId && x.Status == Core.FrontOffice.Incident.IncidentStatus.Open);

        var changed = await service.ChangeStatusAsync(new ChangeIncidentStatusRequest(
            created.IncidentId,
            IncidentStatusAction.StartInvestigation), CancellationToken.None);
        changed.Status.Should().Be(Core.FrontOffice.Incident.IncidentStatus.Investigating);
        (await db.FrontOfficeIncidents.CountAsync()).Should().Be(1);
        (await db.AuditLogs.AnyAsync(x => x.Action == "IncidentCreated")).Should().BeTrue();
        (await db.AuditLogs.AnyAsync(x => x.Action == "IncidentStatusChanged")).Should().BeTrue();
    }

    private static FrontOfficeExecutionContext CreateContext(PropertyId propertyId) => new(
        new ActorContext(ActorId.New(), new HashSet<string> { "Reception" }, "session", "workstation", false),
        new PropertyContext(propertyId, "Asia/Ho_Chi_Minh", "VND"),
        new BusinessDateContext(propertyId, new DateOnly(2026, 9, 7), "Test"),
        new CorrelationContext(CorrelationId.New(), null, CommandId.New(), "trace"));

    private sealed class FixedClock(DateTime utcNow) : IClock
    {
        public DateTime UtcNow { get; } = utcNow;
    }
}
