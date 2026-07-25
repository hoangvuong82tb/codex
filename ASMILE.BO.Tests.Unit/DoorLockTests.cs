using ASMILE.BO.Application.FrontOffice.DoorLock;
using ASMILE.BO.Infrastructure.FrontOffice.DoorLock;
using FluentAssertions;

namespace ASMILE.BO.Tests.Unit;

public sealed class DoorLockTests
{
    private static LockAdapterRegistry Registry => new([new GenericLockAdapter(LockVendor.VingCard), new GenericLockAdapter(LockVendor.Generic)]);

    [Fact] public void DL_001_get_devices() => new DoorLockService(Registry).GetDevicesAsync(Guid.NewGuid(), null, default).Result.Should().NotBeEmpty();
    [Fact] public void DL_002_offline_devices() => new DoorLockService(Registry).GetOfflineDevicesAsync(Guid.NewGuid(), default).Result[0].Status.Should().Be(LockStatus.Offline);
    [Fact] public void DL_003_low_battery() => new DoorLockService(Registry).GetLowBatteryDevicesAsync(Guid.NewGuid(), 20, default).Result[0].Status.Should().Be(LockStatus.LowBattery);
    [Fact] public void DL_004_issue_key() => new KeyCardService(Registry).IssueKeyAsync(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), AccessLevel.Guest, DateTime.UtcNow.AddDays(3), 1, default).Result.Status.Should().Be(KeyStatus.Active);
    [Fact] public void DL_005_issue_master_key() => new KeyCardService(Registry).IssueKeyAsync(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), AccessLevel.Supervisor, null, 1, default).Result.IsMaster.Should().BeTrue();
    [Fact] public void DL_006_revoke_key() => new KeyCardService(Registry).RevokeKeyAsync(Guid.NewGuid(), "Checkout", Guid.NewGuid(), default).Result.Status.Should().Be(KeyStatus.Revoked);
    [Fact] public void DL_007_suspend_key() => new KeyCardService(Registry).SuspendKeyAsync(Guid.NewGuid(), default).Result.Status.Should().Be(KeyStatus.Suspended);
    [Fact] public void DL_008_reactivate_key() => new KeyCardService(Registry).ReactivateKeyAsync(Guid.NewGuid(), default).Result.Status.Should().Be(KeyStatus.Active);
    [Fact] public void DL_009_report_lost() => new KeyCardService(Registry).ReportLostAsync(Guid.NewGuid(), default).Result.Status.Should().Be(KeyStatus.Lost);
    [Fact] public void DL_010_key_inventory() => new KeyCardService(Registry).GetInventoryAsync(Guid.NewGuid(), default).Result.Active.Should().BeGreaterThan(0);
    [Fact] public void DL_011_access_events() => new DoorEventService().GetEventsAsync(Guid.NewGuid(), DateTime.UtcNow.AddDays(-1), default).Result.Should().NotBeEmpty();
    [Fact] public void DL_012_failed_attempts() => new DoorEventService().GetFailedAttemptsAsync(Guid.NewGuid(), DateTime.UtcNow.AddDays(-1), default).Result.Should().NotBeEmpty();
    [Fact] public void DL_013_dashboard() => new DoorLockDashboardService().GetDashboardAsync(Guid.NewGuid(), default).Result.SystemHealthPct.Should().BeGreaterThan(90);
    [Fact] public void DL_014_events_unique() => typeof(DoorLockEvents).GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static).Select(f => f.GetValue(null)).Should().OnlyHaveUniqueItems();
}
