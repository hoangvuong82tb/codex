using ASMILE.BO.Application.FrontOffice.Channel;

namespace ASMILE.BO.Infrastructure.FrontOffice.Channel;

public sealed class ChannelManager : IChannelManager
{
    public Task<ChannelDto> RegisterChannelAsync(string code, string name, SyncDirection direction, CancellationToken ct) => Task.FromResult(new ChannelDto(Guid.NewGuid(), code, name, null, ChannelStatus.Connected, direction, true, DateTime.UtcNow, null, 0, 0, 0, 0, DateTime.UtcNow));
    public Task<ChannelDto> ConnectAsync(Guid channelId, CancellationToken ct) => Task.FromResult(new ChannelDto(channelId, "OTA-001", "Booking.com", "BC-123", ChannelStatus.Connected, SyncDirection.Bidirectional, true, DateTime.UtcNow, null, 150, 2, 120, 25000000, DateTime.UtcNow.AddMonths(-6)));
    public Task<ChannelDto> DisconnectAsync(Guid channelId, CancellationToken ct) => Task.FromResult(new ChannelDto(channelId, "OTA-001", "Booking.com", null, ChannelStatus.Disconnected, SyncDirection.Bidirectional, false, DateTime.UtcNow, null, 150, 2, 120, 25000000, DateTime.UtcNow.AddMonths(-6)));
    public Task<IReadOnlyList<ChannelDto>> GetChannelsAsync(CancellationToken ct) => Task.FromResult<IReadOnlyList<ChannelDto>>([new ChannelDto(Guid.NewGuid(), "OTA-001", "Booking.com", "BC-123", ChannelStatus.Connected, SyncDirection.Bidirectional, true, DateTime.UtcNow.AddMinutes(-5), null, 150, 2, 120, 25000000, DateTime.UtcNow.AddMonths(-6))]);
    public Task<ChannelDto> GetChannelAsync(Guid channelId, CancellationToken ct) => Task.FromResult(new ChannelDto(channelId, "OTA-001", "Booking.com", "BC-123", ChannelStatus.Connected, SyncDirection.Bidirectional, true, DateTime.UtcNow.AddMinutes(-5), null, 150, 2, 120, 25000000, DateTime.UtcNow.AddMonths(-6)));
}

public sealed class InventorySyncService : IInventorySyncService
{
    public Task<SyncResultDto> PushInventoryAsync(Guid channelId, CancellationToken ct) => Task.FromResult(new SyncResultDto(Guid.NewGuid(), SyncStatus.Completed, 40, 0, 0, 0, 250, []));
    public Task<SyncResultDto> PullAvailabilityAsync(Guid channelId, CancellationToken ct) => Task.FromResult(new SyncResultDto(Guid.NewGuid(), SyncStatus.Completed, 0, 0, 3, 0, 180, []));
    public Task<IReadOnlyList<InventoryMappingDto>> GetMappingsAsync(Guid channelId, CancellationToken ct) => Task.FromResult<IReadOnlyList<InventoryMappingDto>>([new InventoryMappingDto(Guid.NewGuid(), channelId, Guid.NewGuid(), "DLX", "DLX-OTA", "Deluxe Room", 20, 8, 12, true)]);
    public Task<InventoryMappingDto> UpdateMappingAsync(InventoryMappingDto mapping, CancellationToken ct) => Task.FromResult(mapping);
    public Task<SyncResultDto> DeltaSyncInventoryAsync(Guid channelId, IReadOnlyList<(Guid RoomTypeId, int Delta)> changes, CancellationToken ct) => Task.FromResult(new SyncResultDto(Guid.NewGuid(), SyncStatus.Completed, changes.Sum(c => Math.Abs(c.Delta)), 0, 0, 0, 50, []));
}

public sealed class RateSyncService : IRateSyncService
{
    public Task<SyncResultDto> PushRatesAsync(Guid channelId, CancellationToken ct) => Task.FromResult(new SyncResultDto(Guid.NewGuid(), SyncStatus.Completed, 0, 5, 0, 0, 300, []));
    public Task<SyncResultDto> PullRatesAsync(Guid channelId, CancellationToken ct) => Task.FromResult(new SyncResultDto(Guid.NewGuid(), SyncStatus.Completed, 0, 2, 0, 0, 200, []));
    public Task<IReadOnlyList<RateMappingDto>> GetMappingsAsync(Guid channelId, CancellationToken ct) => Task.FromResult<IReadOnlyList<RateMappingDto>>([new RateMappingDto(Guid.NewGuid(), channelId, Guid.NewGuid(), "BAR", "BAR-OTA", "Best Available Rate", 1500000, 15m, true, true, true)]);
    public Task<RateMappingDto> UpdateMappingAsync(RateMappingDto mapping, CancellationToken ct) => Task.FromResult(mapping);
    public Task<RestrictionDto> SetRestrictionAsync(RestrictionDto restriction, CancellationToken ct) => Task.FromResult(restriction);
}

public sealed class ReservationSyncService : IReservationSyncService
{
    public Task<ReservationImportResult> ImportReservationsAsync(Guid channelId, IReadOnlyList<OtaReservationDto> reservations, CancellationToken ct) => Task.FromResult(new ReservationImportResult(reservations.Count, 0, 0, 0, 0, [], reservations.Select(_ => Guid.NewGuid()).ToList()));
    public Task<ReservationImportResult> CancelImportedAsync(Guid channelId, string otaReservationId, string reason, CancellationToken ct) => Task.FromResult(new ReservationImportResult(0, 0, 1, 0, 0, [], []));
    public Task<IReadOnlyList<string>> DetectDuplicatesAsync(Guid channelId, OtaReservationDto reservation, CancellationToken ct) => Task.FromResult<IReadOnlyList<string>>([]);
}

public sealed class SyncMonitor : ISyncMonitor
{
    public Task<SyncDashboardDto> GetSyncDashboardAsync(CancellationToken ct) => Task.FromResult(new SyncDashboardDto(3, 1, 150, 2, 0, 98.7m, 185m, 120, 45, 28));
    public Task<IReadOnlyList<SyncQueueItemDto>> GetFailedItemsAsync(int maxRetry, CancellationToken ct) => Task.FromResult<IReadOnlyList<SyncQueueItemDto>>([new SyncQueueItemDto(Guid.NewGuid(), Guid.NewGuid(), "InventorySync", SyncDirection.Push, SyncStatus.Failed, "{}", DateTime.UtcNow.AddMinutes(-30), DateTime.UtcNow.AddMinutes(-30), null, 2, "Connection timeout", Guid.NewGuid())]);
    public Task<int> RetryFailedAsync(int batchSize, CancellationToken ct) => Task.FromResult(2);
    public Task<int> MoveToDeadLetterAsync(int olderThanHours, CancellationToken ct) => Task.FromResult(1);
}

public sealed class ChannelDashboardService : IChannelDashboardService
{
    public Task<ChannelDashboardDto> GetChannelDashboardAsync(Guid propertyId, CancellationToken ct) => Task.FromResult(new ChannelDashboardDto(3, 2, 0, 1, 85000000, [], new SyncDashboardDto(3, 1, 150, 2, 0, 98.7m, 185m, 120, 45, 28), []));
    public Task<IReadOnlyList<BookingPaceDto>> GetBookingPaceAsync(Guid propertyId, int days, CancellationToken ct) => Task.FromResult<IReadOnlyList<BookingPaceDto>>([new BookingPaceDto(DateOnly.FromDateTime(DateTime.UtcNow), 15, 12, 125m, 50, 1850000)]);
    public Task<IReadOnlyList<OtaPerformanceDto>> GetOtaPerformanceAsync(Guid propertyId, CancellationToken ct) => Task.FromResult<IReadOnlyList<OtaPerformanceDto>>([new OtaPerformanceDto(Guid.NewGuid(), "Booking.com", 85, 120000000, 1750000, 3.5m, 5, 15m)]);
}
