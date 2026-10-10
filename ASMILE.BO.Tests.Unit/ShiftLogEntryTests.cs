using ASMILE.BO.Application.FrontOffice.Foundation.Persistence;
using ASMILE.BO.Application.FrontOffice.Foundation.Contexts;
using ASMILE.BO.Application.FrontOffice.ShiftLog;
using ASMILE.BO.Core.FrontOffice.Foundation.Identifiers;
using ASMILE.BO.Core.FrontOffice.Incident;
using ASMILE.BO.Core.FrontOffice.ShiftLog;
using ASMILE.BO.Infrastructure.FrontOffice.Foundation;
using ASMILE.BO.Infrastructure.FrontOffice.ShiftLog;
using ASMILE.BO.Infrastructure.Persistence;
using ASMILE.BO.Shared.Foundation;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace ASMILE.BO.Tests.Unit;

public sealed class ShiftLogEntryTests
{
    [Fact]
    public void SHIFTLOG_001_domain_requires_operational_content()
    {
        var now = new DateTime(2026, 9, 9, 8, 0, 0, DateTimeKind.Utc);
        var act = () => FrontOfficeShiftLogEntry.Create(
            Guid.NewGuid(), now, "Ca sáng", "", "Bàn giao", IncidentPriority.Medium,
            "Mô tả", now, null);

        act.Should().Throw<ArgumentException>().WithMessage("SHIFT_LOG_TITLE_REQUIRED");
    }

    [Fact]
    public async Task SHIFTLOG_002_persistent_create_query_and_status_round_trip_uses_real_context()
    {
        var propertyId = PropertyId.New();
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;
        await using var db = new AppDbContext(options);
        await db.Database.EnsureCreatedAsync();

        var accessor = new FrontOfficeExecutionContextAccessor();
        var clock = new FixedClock(new DateTime(2026, 9, 9, 8, 0, 0, DateTimeKind.Utc));
        var audit = new FrontOfficeAuditWriter();
        var persistence = new EfFrontOfficePersistenceSession(
            db, accessor, new PassthroughIntegrationEventMapper(),
            new CanonicalOutboxMessageFactory(clock), audit, clock);
        var service = new EfShiftLogEntryService(db, persistence, accessor, clock);
        using var scope = accessor.BeginScope(CreateContext(propertyId));

        var created = await service.CreateAsync(new CreateShiftLogEntryRequest(
            clock.UtcNow, "Ca sáng", "Bàn giao phòng 405", "Bàn giao", IncidentPriority.Medium,
            "405", "Nguyễn Văn A", "RES-001", "Lễ tân", "Theo dõi bổ sung minibar", true), CancellationToken.None);

        var rows = await service.SearchAsync(new ShiftLogEntrySearchRequest(propertyId.Value), CancellationToken.None);
        rows.Should().ContainSingle(x => x.ShiftLogEntryId == created.ShiftLogEntryId && x.Status == ShiftLogEntryStatus.Open);

        var changed = await service.ChangeStatusAsync(new ChangeShiftLogEntryStatusRequest(
            created.ShiftLogEntryId, ShiftLogEntryStatus.InProgress), CancellationToken.None);
        changed.Status.Should().Be(ShiftLogEntryStatus.InProgress);
        (await db.FrontOfficeShiftLogEntries.CountAsync()).Should().Be(1);
        (await db.AuditLogs.AnyAsync(x => x.Action == "ShiftLogEntryCreated")).Should().BeTrue();
        (await db.AuditLogs.AnyAsync(x => x.Action == "ShiftLogEntryStatusChanged")).Should().BeTrue();
    }

    private static FrontOfficeExecutionContext CreateContext(PropertyId propertyId) => new(
        new ActorContext(ActorId.New(), new HashSet<string> { "Reception" }, "session", "workstation", false),
        new PropertyContext(propertyId, "Asia/Ho_Chi_Minh", "VND"),
        new BusinessDateContext(propertyId, new DateOnly(2026, 9, 9), "Test"),
        new CorrelationContext(CorrelationId.New(), null, CommandId.New(), "trace"));

    private sealed class FixedClock(DateTime utcNow) : IClock
    {
        public DateTime UtcNow { get; } = utcNow;
    }
}
