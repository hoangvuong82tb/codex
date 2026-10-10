namespace ASMILE.BO.Application.FrontOffice.Channel;

// =============================================================================
// CHANNEL & OTA
// =============================================================================

public enum ChannelStatus { Connected, Disconnected, Syncing, Degraded, Error }
public enum SyncDirection { Push, Pull, Bidirectional }
public enum SyncStatus { Pending, InProgress, Completed, Failed, PartiallyCompleted, DeadLettered }
public enum ReservationConflictResolution { LastWins, FirstWins, ManualReview, ByPriority, ByRevenue }
public enum RestrictionType { LengthOfStay, ClosedToArrival, ClosedToDeparture, MinStay, MaxStay, MinAdvanceBooking }

public sealed record ChannelDto(
    Guid ChannelId, string ChannelCode, string ChannelName, string? OtaCode,
    ChannelStatus Status, SyncDirection Direction, bool IsActive,
    DateTime? LastSyncAtUtc, string? LastError, int SuccessfulSyncs, int FailedSyncs,
    int AverageLatencyMs, decimal BookingVolume, DateTime CreatedAtUtc
);

public sealed record InventoryMappingDto(
    Guid MappingId, Guid ChannelId, Guid RoomTypeId, string RoomTypeName,
    string OtaRoomCode, string OtaRoomName, int AllocatedRooms, int BookedRooms,
    int AvailableRooms, bool IsActive
);

public sealed record RateMappingDto(
    Guid MappingId, Guid ChannelId, Guid RatePlanId, string RatePlanName,
    string OtaRateCode, string OtaRateName, decimal BaseRate, decimal MarkupPct,
    bool IncludeTax, bool IncludeServiceCharge, bool IsActive
);

public sealed record RestrictionDto(
    Guid RestrictionId, Guid MappingId, RestrictionType Type,
    int? Value, DateOnly FromDate, DateOnly ToDate, bool IsActive
);

// =============================================================================
// SYNC
// =============================================================================

public sealed record SyncQueueItemDto(
    Guid ItemId, Guid ChannelId, string EventType, SyncDirection Direction,
    SyncStatus Status, string PayloadJson, DateTime CreatedAtUtc,
    DateTime? StartedAtUtc, DateTime? CompletedAtUtc,
    int RetryCount, string? LastError, Guid CorrelationId
);

public sealed record SyncResultDto(
    Guid ItemId, SyncStatus Status, int RoomsSynced, int RatesSynced,
    int ReservationsSynced, int Errors, long DurationMs,
    IReadOnlyList<string> Warnings
);

public sealed record SyncDashboardDto(
    int PendingSyncs, int InProgress, int CompletedToday, int FailedToday,
    int DeadLettered, decimal SuccessRate, decimal AvgLatencyMs,
    int TotalRoomsSynced, int TotalRatesSynced, int TotalReservationsSynced
);

// =============================================================================
// RESERVATION IMPORT
// =============================================================================

public sealed record OtaReservationDto(
    string OtaReservationId, Guid ChannelId, string ChannelName,
    string GuestName, string? GuestEmail, string? GuestPhone,
    Guid RoomTypeId, DateOnly ArrivalDate, DateOnly DepartureDate,
    int Adults, int Children, decimal TotalAmount, string Currency,
    string? SpecialRequests, DateTime BookedAtUtc, bool IsModification
);

public sealed record ReservationImportResult(
    int Imported, int Modified, int Cancelled, int Skipped, int Errors,
    IReadOnlyList<string> Warnings, IReadOnlyList<Guid> CreatedReservationIds
);

public sealed record ChannelDashboardDto(
    int ActiveChannels, int Connected, int Disconnected, int Degraded,
    decimal TotalBookingVolume,
    IReadOnlyList<ChannelDto> Channels,
    SyncDashboardDto SyncStats,
    IReadOnlyList<SyncQueueItemDto> RecentFailures
);

public sealed record BookingPaceDto(
    DateOnly Date, int BookingsToday, int BookingsSameDayLastYear,
    decimal PacePercent, int RoomsRemaining, decimal AverageRate
);

public sealed record OtaPerformanceDto(
    Guid ChannelId, string ChannelName, int BookingsThisMonth, decimal RevenueThisMonth,
    decimal AverageRate, decimal CancellationRate, int AveragePickupTime, decimal CommissionRate
);

// =============================================================================
// CONTRACTS
// =============================================================================

public interface IChannelManager
{
    Task<ChannelDto> RegisterChannelAsync(string code, string name, SyncDirection direction, CancellationToken ct);
    Task<ChannelDto> ConnectAsync(Guid channelId, CancellationToken ct);
    Task<ChannelDto> DisconnectAsync(Guid channelId, CancellationToken ct);
    Task<IReadOnlyList<ChannelDto>> GetChannelsAsync(CancellationToken ct);
    Task<ChannelDto> GetChannelAsync(Guid channelId, CancellationToken ct);
}

public interface IInventorySyncService
{
    Task<SyncResultDto> PushInventoryAsync(Guid channelId, CancellationToken ct);
    Task<SyncResultDto> PullAvailabilityAsync(Guid channelId, CancellationToken ct);
    Task<IReadOnlyList<InventoryMappingDto>> GetMappingsAsync(Guid channelId, CancellationToken ct);
    Task<InventoryMappingDto> UpdateMappingAsync(InventoryMappingDto mapping, CancellationToken ct);
    Task<SyncResultDto> DeltaSyncInventoryAsync(Guid channelId, IReadOnlyList<(Guid RoomTypeId, int Delta)> changes, CancellationToken ct);
}

public interface IRateSyncService
{
    Task<SyncResultDto> PushRatesAsync(Guid channelId, CancellationToken ct);
    Task<SyncResultDto> PullRatesAsync(Guid channelId, CancellationToken ct);
    Task<IReadOnlyList<RateMappingDto>> GetMappingsAsync(Guid channelId, CancellationToken ct);
    Task<RateMappingDto> UpdateMappingAsync(RateMappingDto mapping, CancellationToken ct);
    Task<RestrictionDto> SetRestrictionAsync(RestrictionDto restriction, CancellationToken ct);
}

public interface IReservationSyncService
{
    Task<ReservationImportResult> ImportReservationsAsync(Guid channelId, IReadOnlyList<OtaReservationDto> reservations, CancellationToken ct);
    Task<ReservationImportResult> CancelImportedAsync(Guid channelId, string otaReservationId, string reason, CancellationToken ct);
    Task<IReadOnlyList<string>> DetectDuplicatesAsync(Guid channelId, OtaReservationDto reservation, CancellationToken ct);
}

public interface ISyncMonitor
{
    Task<SyncDashboardDto> GetSyncDashboardAsync(CancellationToken ct);
    Task<IReadOnlyList<SyncQueueItemDto>> GetFailedItemsAsync(int maxRetry, CancellationToken ct);
    Task<int> RetryFailedAsync(int batchSize, CancellationToken ct);
    Task<int> MoveToDeadLetterAsync(int olderThanHours, CancellationToken ct);
}

public interface IChannelDashboardService
{
    Task<ChannelDashboardDto> GetChannelDashboardAsync(Guid propertyId, CancellationToken ct);
    Task<IReadOnlyList<BookingPaceDto>> GetBookingPaceAsync(Guid propertyId, int days, CancellationToken ct);
    Task<IReadOnlyList<OtaPerformanceDto>> GetOtaPerformanceAsync(Guid propertyId, CancellationToken ct);
}

// =============================================================================
// EVENTS
// =============================================================================

public static class ChannelEvents
{
    public const string InventorySynced = "Channel.InventorySynced";
    public const string RateSynced = "Channel.RateSynced";
    public const string ReservationImported = "Channel.ReservationImported";
    public const string ReservationModified = "Channel.ReservationModified";
    public const string ReservationCancelled = "Channel.ReservationCancelled";
    public const string ChannelConnected = "Channel.ChannelConnected";
    public const string ChannelDisconnected = "Channel.ChannelDisconnected";
    public const string ChannelRecovered = "Channel.ChannelRecovered";
    public const string SyncFailed = "Channel.SyncFailed";
    public const string SyncRetryExhausted = "Channel.SyncRetryExhausted";
    public const string MappingUpdated = "Channel.MappingUpdated";
}
