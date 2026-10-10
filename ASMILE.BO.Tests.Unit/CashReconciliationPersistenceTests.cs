using ASMILE.BO.Application.FrontOffice.Cashier;
using ASMILE.BO.Application.FrontOffice.Foundation.Contexts;
using ASMILE.BO.Core.FrontOffice.Foundation.Identifiers;
using ASMILE.BO.Infrastructure.FrontOffice.Cashier;
using ASMILE.BO.Infrastructure.FrontOffice.Foundation;
using ASMILE.BO.Infrastructure.Persistence;
using ASMILE.BO.Shared.Foundation;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace ASMILE.BO.Tests.Unit;

public sealed class CashReconciliationPersistenceTests
{
    [Fact]
    public async Task CSH_REAL_002_reconcile_and_approve_variance_use_real_persistence()
    {
        var propertyId = PropertyId.New();
        var cashierId = Guid.NewGuid();
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options);
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
        using var scope = accessor.BeginScope(CreateContext(propertyId));
        var shiftService = new EfCashShiftService(db, accessor, clock);
        var opened = await shiftService.OpenShiftAsync(Guid.NewGuid(), cashierId, 1_000_000, CancellationToken.None);
        await shiftService.CloseShiftAsync(opened.ShiftId, 1_000_000, null, CancellationToken.None);

        var service = new EfCashReconciliationService(db, persistence, accessor, clock);
        var reconciliation = await service.ReconcileAsync(opened.ShiftId, 1_100_000, CancellationToken.None);
        reconciliation.Status.Should().Be(ReconciliationStatus.VarianceDetected);
        reconciliation.Variance.Should().Be(100_000);
        (await service.GetPendingAsync(propertyId.Value, CancellationToken.None)).Should().ContainSingle();

        var approved = await service.ApproveVarianceAsync(
            reconciliation.ReconciliationId,
            Guid.NewGuid(),
            "Đã kiểm đếm lại và phê duyệt chênh lệch.",
            CancellationToken.None);
        approved.Status.Should().Be(ReconciliationStatus.Approved);
        approved.ApprovedBy.Should().NotBeNull();
        (await db.CashReconciliations.SingleAsync()).Status.Should().Be(ASMILE.BO.Core.FrontOffice.Cashier.CashReconciliationStatus.Approved);
        (await db.AuditLogs.CountAsync(x => x.EntityName == "CashReconciliation")).Should().Be(2);
    }

    private static FrontOfficeExecutionContext CreateContext(PropertyId propertyId) => new(
        new ActorContext(ActorId.New(), new HashSet<string> { "Cashier", "Supervisor" }, "session", "workstation", false),
        new PropertyContext(propertyId, "Asia/Ho_Chi_Minh", "VND"),
        new BusinessDateContext(propertyId, new DateOnly(2026, 9, 8), "Test"),
        new CorrelationContext(CorrelationId.New(), null, CommandId.New(), "trace"));

    private sealed class FixedClock(DateTime utcNow) : IClock
    {
        public DateTime UtcNow { get; } = utcNow;
    }
}
