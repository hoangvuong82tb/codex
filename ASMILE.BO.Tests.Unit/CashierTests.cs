using ASMILE.BO.Application.FrontOffice.Cashier;
using ASMILE.BO.Application.FrontOffice.Foundation.Contexts;
using ASMILE.BO.Core.FrontOffice.Cashier;
using ASMILE.BO.Core.FrontOffice.Foundation.Identifiers;
using ASMILE.BO.Infrastructure.FrontOffice.Cashier;
using ASMILE.BO.Infrastructure.FrontOffice.Foundation;
using ASMILE.BO.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace ASMILE.BO.Tests.Unit;

public sealed class CashierTests
{
    [Fact] public void CSH_001_events_unique() { typeof(CashierEvents).GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static).Select(f => f.GetValue(null)).Should().OnlyHaveUniqueItems(); }

    [Fact]
    public void CSH_016_adjustments_change_expected_cash_in_signed_direction()
    {
        var now = new DateTime(2026, 9, 21, 8, 0, 0, DateTimeKind.Utc);
        var shift = CashierShift.Open(Guid.NewGuid(), Guid.NewGuid(), "CS-ADJUSTMENT-001", new DateOnly(2026, 9, 21), 100m, now, ActorId.New());

        shift.RecordAdjustment(25m, "Overage adjustment", now, ActorId.New());
        shift.RecordAdjustment(-10m, "Shortage adjustment", now, ActorId.New());

        shift.ExpectedCash.Should().Be(115m);
    }

    [Fact]
    public async Task CSH_013_persistent_shift_round_trip_uses_real_context()
    {
        var propertyId = PropertyId.New();
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;
        await using var db = new AppDbContext(options);
        await db.Database.EnsureCreatedAsync();
        var accessor = new FrontOfficeExecutionContextAccessor();
        using var scope = accessor.BeginScope(CreateContext(propertyId));
        var service = new EfCashShiftService(db, accessor, new FixedClock(new DateTime(2026, 9, 7, 8, 0, 0, DateTimeKind.Utc)));

        var cashierId = Guid.NewGuid();
        var opened = await service.OpenShiftAsync(Guid.NewGuid(), cashierId, 3_000_000m, CancellationToken.None);
        opened.Status.Should().Be(Core.FrontOffice.Cashier.CashierShiftStatus.Open);
        opened.Movements.Should().ContainSingle(x => x.Type == Core.FrontOffice.Cashier.CashMovementType.OpeningCash);

        var active = await service.GetActiveShiftsAsync(propertyId.Value, CancellationToken.None);
        active.Should().ContainSingle(x => x.ShiftId == opened.ShiftId);

        var closed = await service.CloseShiftAsync(opened.ShiftId, 3_000_000m, "Counted", CancellationToken.None);
        closed.Status.Should().Be(Core.FrontOffice.Cashier.CashierShiftStatus.Closed);
        (await service.GetActiveShiftsAsync(propertyId.Value, CancellationToken.None)).Should().BeEmpty();
    }

    [Fact]
    public async Task CSH_014_persistent_handover_closes_source_and_opens_target()
    {
        var propertyId = PropertyId.New();
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;
        await using var db = new AppDbContext(options);
        await db.Database.EnsureCreatedAsync();
        var accessor = new FrontOfficeExecutionContextAccessor();
        using var scope = accessor.BeginScope(CreateContext(propertyId));
        var service = new EfCashShiftService(db, accessor, new FixedClock(new DateTime(2026, 9, 7, 8, 0, 0, DateTimeKind.Utc)));

        var sourceCashierId = Guid.NewGuid();
        var targetCashierId = Guid.NewGuid();
        var source = await service.OpenShiftAsync(Guid.NewGuid(), sourceCashierId, 2_000_000m, CancellationToken.None);
        var handover = await service.HandOverAsync(source.ShiftId, targetCashierId, 1_950_000m, "Counted at handover", CancellationToken.None);

        handover.FromShiftId.Should().Be(source.ShiftId);
        handover.ToCashierId.Should().Be(targetCashierId);
        handover.Variance.Should().Be(-50_000m);
        var active = await service.GetActiveShiftsAsync(propertyId.Value, CancellationToken.None);
        active.Should().ContainSingle(x => x.CashierId == targetCashierId && x.OpeningCash == 1_950_000m);
        (await service.GetShiftAsync(source.ShiftId, CancellationToken.None)).Status.Should().Be(Core.FrontOffice.Cashier.CashierShiftStatus.Closed);
        var closingMovement = await db.CashMovements.SingleAsync(x => x.CashierShiftId == source.ShiftId && x.Type == Core.FrontOffice.Cashier.CashMovementType.ClosingCash);
        closingMovement.Description.Should().Contain("Handover to cashier");
        closingMovement.Description.Should().Contain("Counted at handover");
    }

    [Fact]
    public async Task CSH_015_shift_history_reads_persisted_rows_and_respects_property_scope()
    {
        var propertyId = PropertyId.New();
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;
        await using var db = new AppDbContext(options);
        await db.Database.EnsureCreatedAsync();
        var accessor = new FrontOfficeExecutionContextAccessor();
        using var scope = accessor.BeginScope(CreateContext(propertyId));
        var service = new EfCashShiftService(db, accessor, new FixedClock(new DateTime(2026, 9, 7, 8, 0, 0, DateTimeKind.Utc)));

        var opened = await service.OpenShiftAsync(Guid.NewGuid(), Guid.NewGuid(), 1_000_000m, CancellationToken.None);
        await service.CloseShiftAsync(opened.ShiftId, 990_000m, "Counting error", CancellationToken.None);

        var history = await service.GetShiftHistoryAsync(
            propertyId.Value,
            new DateTime(2026, 9, 6, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 9, 8, 0, 0, 0, DateTimeKind.Utc),
            CancellationToken.None);

        history.Should().ContainSingle(x => x.ShiftId == opened.ShiftId);
        history.Single().Status.Should().Be(Core.FrontOffice.Cashier.CashierShiftStatus.Closed);
        history.Single().Variance.Should().Be(-10_000m);

        using var otherScope = accessor.BeginScope(CreateContext(PropertyId.New()));
        await FluentActions.Invoking(() => service.GetShiftHistoryAsync(propertyId.Value, null, null, CancellationToken.None))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("PROPERTY_CONTEXT_MISMATCH");
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
