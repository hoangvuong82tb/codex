using ASMILE.BO.Application.FrontOffice.DoorLock;
using ASMILE.BO.Infrastructure.FrontOffice.Foundation;
using ASMILE.BO.Application.FrontOffice.Foundation.Contexts;
using ASMILE.BO.Core.FrontOffice.DoorLock;
using ASMILE.BO.Core.FrontOffice.Foundation.Identifiers;
using ASMILE.BO.Infrastructure.FrontOffice.DoorLock;
using ASMILE.BO.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ASMILE.BO.Tests.Unit;

public sealed class DoorLockPersistenceTests
{
    [Fact]
    public async Task DL_REAL_001_devices_events_and_dashboard_use_real_persistence()
    {
        var propertyId = PropertyId.New();
        var now = new DateTime(2026, 9, 8, 8, 0, 0, DateTimeKind.Utc);
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options);
        await db.Database.EnsureCreatedAsync();

        var online = DoorLockDevice.Create(propertyId.Value, "LOCK-101", "101", Guid.NewGuid(), nameof(LockVendor.VingCard), "VisionLine", "3.2.1", nameof(LockStatus.Online), 90, now.AddMinutes(-2), now.AddMonths(-2), 12, 1, true);
        var offline = DoorLockDevice.Create(propertyId.Value, "LOCK-102", "102", Guid.NewGuid(), nameof(LockVendor.Salto), "XS4", "1.0", nameof(LockStatus.Offline), 18, now.AddHours(-3), null, 4, 2, false);
        db.DoorLockDevices.AddRange(online, offline);
        db.DoorLockEvents.AddRange(
            DoorLockEvent.Create(propertyId.Value, online.DeviceId, nameof(DoorEventType.AccessGranted), "101", "KEY-101", nameof(AccessLevel.Guest), true, now.AddMinutes(-5), null),
            DoorLockEvent.Create(propertyId.Value, offline.DeviceId, nameof(DoorEventType.AccessDenied), "102", "KEY-EXPIRED", nameof(AccessLevel.Guest), false, now.AddMinutes(-4), "Key expired"),
            DoorLockEvent.Create(propertyId.Value, offline.DeviceId, nameof(DoorEventType.TamperDetected), "102", null, null, false, now.AddMinutes(-3), "Door forced"));
        await db.SaveChangesAsync();

        var accessor = new FrontOfficeExecutionContextAccessor();
        using var scope = accessor.BeginScope(CreateContext(propertyId));
        var devices = new EfDoorLockService(db, accessor);
        var events = new EfDoorEventService(db, accessor);
        var dashboard = new EfDoorLockDashboardService(db, accessor, new FixedClock(now));

        (await devices.GetDevicesAsync(propertyId.Value, null, CancellationToken.None)).Should().HaveCount(2);
        (await devices.GetOfflineDevicesAsync(propertyId.Value, CancellationToken.None)).Should().ContainSingle(x => x.DeviceCode == "LOCK-102");
        (await devices.GetLowBatteryDevicesAsync(propertyId.Value, 20, CancellationToken.None)).Should().ContainSingle(x => x.DeviceCode == "LOCK-102");
        (await events.GetFailedAttemptsAsync(propertyId.Value, now.AddHours(-1), CancellationToken.None)).Should().HaveCount(2);
        (await events.GetSecurityAlertsAsync(propertyId.Value, CancellationToken.None)).Should().ContainSingle(x => x.Type == DoorEventType.TamperDetected);

        var summary = await dashboard.GetDashboardAsync(propertyId.Value, CancellationToken.None);
        summary.TotalLocks.Should().Be(2);
        summary.Online.Should().Be(1);
        summary.Offline.Should().Be(1);
        summary.LowBattery.Should().Be(0);
        summary.AccessGrantedToday.Should().Be(1);
        summary.AccessDeniedToday.Should().Be(2);
        summary.FailedAttempts.Should().Be(2);
        summary.RecentSecurityAlerts.Should().ContainSingle();
    }

    private static FrontOfficeExecutionContext CreateContext(PropertyId propertyId) => new(
        new ActorContext(ActorId.New(), new HashSet<string> { "FrontOffice" }, "session", "workstation", false),
        new PropertyContext(propertyId, "Asia/Ho_Chi_Minh", "VND"),
        new BusinessDateContext(propertyId, new DateOnly(2026, 9, 8), "Test"),
        new CorrelationContext(CorrelationId.New(), null, CommandId.New(), "trace"));

    private sealed class FixedClock(DateTime utcNow) : IClock
    {
        public DateTime UtcNow { get; } = utcNow;
    }
}
