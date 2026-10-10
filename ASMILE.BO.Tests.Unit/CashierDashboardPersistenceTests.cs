using ASMILE.BO.Application.FrontOffice.Cashier;
using ASMILE.BO.Application.FrontOffice.Foundation.Contexts;
using ASMILE.BO.Core.FrontOffice.Cashier;
using ASMILE.BO.Core.FrontOffice.Foundation.Identifiers;
using ASMILE.BO.Infrastructure.FrontOffice.Cashier;
using ASMILE.BO.Infrastructure.FrontOffice.Foundation;
using ASMILE.BO.Infrastructure.Persistence;
using ASMILE.BO.Shared.Foundation;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace ASMILE.BO.Tests.Unit;

public sealed class CashierDashboardPersistenceTests
{
    [Fact]
    public async Task CSH_REAL_001_dashboard_reads_open_shift_from_database()
    {
        var propertyId = PropertyId.New();
        var cashierId = Guid.NewGuid();
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options);
        await db.Database.EnsureCreatedAsync();

        var accessor = new FrontOfficeExecutionContextAccessor();
        var clock = new FixedClock(new DateTime(2026, 9, 8, 8, 0, 0, DateTimeKind.Utc));
        using var scope = accessor.BeginScope(CreateContext(propertyId));
        var shiftService = new EfCashShiftService(db, accessor, clock);
        var opened = await shiftService.OpenShiftAsync(Guid.NewGuid(), cashierId, 1_000_000, CancellationToken.None);
        var dashboard = new EfCashierDashboardService(db, accessor, clock);

        var cashier = await dashboard.GetCashierDashboardAsync(cashierId, CancellationToken.None);
        cashier.CurrentShiftId.Should().Be(opened.ShiftId);
        cashier.CurrentBalance.Should().Be(1_000_000);
        cashier.OpeningFloat.Should().Be(1_000_000);
        cashier.DrawerStatus.Should().Be(DrawerStatus.Open);

        var supervisor = await dashboard.GetSupervisorDashboardAsync(propertyId.Value, CancellationToken.None);
        supervisor.ActiveShifts.Should().Be(1);
        supervisor.OpenDrawerCount.Should().Be(1);
        var treasury = await dashboard.GetTreasuryDashboardAsync(propertyId.Value, CancellationToken.None);
        treasury.ActiveShiftCount.Should().Be(1);
        treasury.TotalCashOnHand.Should().Be(1_000_000);

        var closedShift = await shiftService.OpenShiftAsync(Guid.NewGuid(), Guid.NewGuid(), 500_000, CancellationToken.None);
        await shiftService.CloseShiftAsync(closedShift.ShiftId, 500_001, "test variance", CancellationToken.None);
        var closedEntity = await db.CashierShifts
            .Include(x => x.Movements)
            .SingleAsync(x => x.CashierShiftId == closedShift.ShiftId);
        db.CashReconciliations.Add(CashReconciliation.Create(
            propertyId.Value,
            closedEntity,
            500_001,
            Guid.NewGuid(),
            "Other",
            "test variance",
            clock.UtcNow));
        await db.SaveChangesAsync();

        var supervisorWithPending = await dashboard.GetSupervisorDashboardAsync(propertyId.Value, CancellationToken.None);
        supervisorWithPending.PendingReconciliations.Should().Be(1);
        supervisorWithPending.Unresolved.Should().ContainSingle(x => x.ShiftId == closedShift.ShiftId);
    }

    private static FrontOfficeExecutionContext CreateContext(PropertyId propertyId) => new(
        new ActorContext(ActorId.New(), new HashSet<string> { "Cashier" }, "session", "workstation", false),
        new PropertyContext(propertyId, "Asia/Ho_Chi_Minh", "VND"),
        new BusinessDateContext(propertyId, new DateOnly(2026, 9, 8), "Test"),
        new CorrelationContext(CorrelationId.New(), null, CommandId.New(), "trace"));

    private sealed class FixedClock(DateTime utcNow) : IClock
    {
        public DateTime UtcNow { get; } = utcNow;
    }
}
