using ASMILE.BO.Application.FrontOffice.Channel;
using ASMILE.BO.Infrastructure.FrontOffice.Channel;
using FluentAssertions;

namespace ASMILE.BO.Tests.Unit;

public sealed class ChannelTests
{
    [Fact] public void CH_001_register_channel() => new ChannelManager().RegisterChannelAsync("OTA-001", "Booking.com", SyncDirection.Bidirectional, default).Result.Status.Should().Be(ChannelStatus.Connected);
    [Fact] public void CH_002_connect_channel() => new ChannelManager().ConnectAsync(Guid.NewGuid(), default).Result.Status.Should().Be(ChannelStatus.Connected);
    [Fact] public void CH_003_disconnect_channel() => new ChannelManager().DisconnectAsync(Guid.NewGuid(), default).Result.Status.Should().Be(ChannelStatus.Disconnected);
    [Fact] public void CH_004_push_inventory() => new InventorySyncService().PushInventoryAsync(Guid.NewGuid(), default).Result.Status.Should().Be(SyncStatus.Completed);
    [Fact] public void CH_005_delta_sync() => new InventorySyncService().DeltaSyncInventoryAsync(Guid.NewGuid(), [(Guid.NewGuid(), -2), (Guid.NewGuid(), 3)], default).Result.RoomsSynced.Should().Be(5);
    [Fact] public void CH_006_push_rates() => new RateSyncService().PushRatesAsync(Guid.NewGuid(), default).Result.RatesSynced.Should().Be(5);
    [Fact] public void CH_007_import_reservations() => new ReservationSyncService().ImportReservationsAsync(Guid.NewGuid(), [new OtaReservationDto("OTA-RSV-001", Guid.NewGuid(), "Booking.com", "Guest", null, null, Guid.NewGuid(), DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7)), DateOnly.FromDateTime(DateTime.UtcNow.AddDays(9)), 2, 0, 3600000, "VND", null, DateTime.UtcNow, false)], default).Result.Imported.Should().Be(1);
    [Fact] public void CH_008_cancel_imported() => new ReservationSyncService().CancelImportedAsync(Guid.NewGuid(), "OTA-RSV-001", "Guest cancelled", default).Result.Cancelled.Should().Be(1);
    [Fact] public void CH_009_sync_dashboard() => new SyncMonitor().GetSyncDashboardAsync(default).Result.SuccessRate.Should().BeGreaterThan(95);
    [Fact] public void CH_010_retry_failed() => new SyncMonitor().RetryFailedAsync(10, default).Result.Should().BeGreaterThan(0);
    [Fact] public void CH_011_booking_pace() => new ChannelDashboardService().GetBookingPaceAsync(Guid.NewGuid(), 30, default).Result.Should().NotBeEmpty();
    [Fact] public void CH_012_ota_performance() => new ChannelDashboardService().GetOtaPerformanceAsync(Guid.NewGuid(), default).Result.Should().NotBeEmpty();
    [Fact] public void CH_013_events_unique() => typeof(ChannelEvents).GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static).Select(f => f.GetValue(null)).Should().OnlyHaveUniqueItems();
}
