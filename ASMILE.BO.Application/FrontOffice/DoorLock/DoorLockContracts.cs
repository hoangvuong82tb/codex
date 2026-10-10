namespace ASMILE.BO.Application.FrontOffice.DoorLock;

// =============================================================================
// DOOR LOCK DOMAIN
// =============================================================================

public enum LockVendor { VingCard, Salto, Onity, Dormakaba, AssaAbloy, Kaba, Generic }
public enum LockStatus { Online, Offline, LowBattery, Tampered, Maintenance, Decommissioned }
public enum KeyType { RFID, Mobile, PIN, Magstripe, BLE, NFC }
public enum KeyStatus { Encoded, Active, Suspended, Revoked, Expired, Lost, Returned }
public enum AccessLevel { Guest, Housekeeping, Engineering, Supervisor, Manager, Emergency, Master }
public enum DoorEventType { Opened, Closed, AccessGranted, AccessDenied, LowBattery, Offline, Online, TamperDetected, LockFailure, BatteryReplaced }

public sealed record LockDeviceDto(
    Guid DeviceId, string DeviceCode, string RoomNumber, Guid? RoomId,
    LockVendor Vendor, string Model, string FirmwareVersion, LockStatus Status,
    int BatteryLevel, DateTime? LastSeenAtUtc, DateTime? LastBatteryChangeAtUtc,
    int TotalAccesses, int FailedAttempts, bool IsSynced
);

public sealed record KeyCardDto(
    Guid KeyId, string KeyCode, KeyType Type, Guid? GuestId, string? GuestName,
    Guid? StayId, string? RoomNumber, IReadOnlyList<Guid> RoomIds,
    AccessLevel Level, KeyStatus Status,
    DateTime? ValidFromUtc, DateTime? ValidUntilUtc,
    DateTime? IssuedAtUtc, Guid? IssuedBy, string? IssuedByName,
    DateTime? RevokedAtUtc, Guid? RevokedBy, string? RevokeReason,
    int EncoderId, bool IsMaster
);

public sealed record DoorEventDto(
    Guid EventId, Guid DeviceId, string RoomNumber, DoorEventType Type,
    string? KeyCode, AccessLevel? AccessLevel, bool Granted,
    DateTime OccurredAtUtc, string? Detail
);

public sealed record KeyInventoryDto(
    int TotalKeys, int Active, int Suspended, int Revoked,
    int Expired, int Lost, int AvailableForIssue
);

// =============================================================================
// VENDOR ADAPTER (Abstraction for multiple lock hardware vendors)
// =============================================================================

public interface ILockVendorAdapter
{
    LockVendor Vendor { get; }
    Task<bool> EncodeKeyAsync(KeyCardDto key, CancellationToken ct);
    Task<bool> RevokeKeyAsync(string keyCode, CancellationToken ct);
    Task<bool> SyncLockAsync(LockDeviceDto device, CancellationToken ct);
    Task<IReadOnlyList<DoorEventDto>> PullEventsAsync(Guid deviceId, DateTime since, CancellationToken ct);
    Task<bool> PingAsync(Guid deviceId, CancellationToken ct);
    Task<bool> EmergencyOpenAsync(Guid deviceId, string reason, Guid actorId, CancellationToken ct);
}

// =============================================================================
// CONTRACTS
// =============================================================================

public interface IDoorLockService
{
    Task<IReadOnlyList<LockDeviceDto>> GetDevicesAsync(Guid propertyId, string? floor, CancellationToken ct);
    Task<LockDeviceDto> GetDeviceAsync(Guid deviceId, CancellationToken ct);
    Task<IReadOnlyList<LockDeviceDto>> GetOfflineDevicesAsync(Guid propertyId, CancellationToken ct);
    Task<IReadOnlyList<LockDeviceDto>> GetLowBatteryDevicesAsync(Guid propertyId, int threshold, CancellationToken ct);
}

public interface IKeyCardService
{
    Task<KeyCardDto> IssueKeyAsync(Guid? guestId, Guid? stayId, Guid roomId, AccessLevel level, DateTime? validUntil, int encoderId, CancellationToken ct);
    Task<KeyCardDto> IssueDuplicateAsync(Guid originalKeyId, CancellationToken ct);
    Task<KeyCardDto> RevokeKeyAsync(Guid keyId, string reason, Guid actorId, CancellationToken ct);
    Task<KeyCardDto> SuspendKeyAsync(Guid keyId, CancellationToken ct);
    Task<KeyCardDto> ReactivateKeyAsync(Guid keyId, CancellationToken ct);
    Task<KeyCardDto> ReportLostAsync(Guid keyId, CancellationToken ct);
    Task<KeyCardDto> ReturnKeyAsync(Guid keyId, CancellationToken ct);
    Task<IReadOnlyList<KeyCardDto>> GetKeysForStayAsync(Guid stayId, CancellationToken ct);
    Task<KeyInventoryDto> GetInventoryAsync(Guid propertyId, CancellationToken ct);
    Task<IReadOnlyList<KeyCardDto>> AutoRevokeOnCheckoutAsync(Guid stayId, CancellationToken ct);
}

public interface IDoorEventService
{
    Task<IReadOnlyList<DoorEventDto>> GetEventsAsync(Guid deviceId, DateTime since, CancellationToken ct);
    Task<IReadOnlyList<DoorEventDto>> GetFailedAttemptsAsync(Guid propertyId, DateTime since, CancellationToken ct);
    Task<IReadOnlyList<DoorEventDto>> GetSecurityAlertsAsync(Guid propertyId, CancellationToken ct);
}

public interface IDoorLockDashboardService
{
    Task<DoorLockDashboardDto> GetDashboardAsync(Guid propertyId, CancellationToken ct);
}

public sealed record DoorLockDashboardDto(
    int TotalLocks, int Online, int Offline, int LowBattery, int Tampered,
    int ActiveKeys, int KeysIssuedToday, int KeysRevokedToday,
    int AccessGrantedToday, int AccessDeniedToday, int FailedAttempts,
    decimal SystemHealthPct,
    IReadOnlyList<LockDeviceDto> OfflineDevices,
    IReadOnlyList<LockDeviceDto> LowBatteryDevices,
    IReadOnlyList<DoorEventDto> RecentSecurityAlerts
);

// =============================================================================
// EVENTS
// =============================================================================

public static class DoorLockEvents
{
    public const string KeyIssued = "DoorLock.KeyIssued";
    public const string KeyRevoked = "DoorLock.KeyRevoked";
    public const string KeyExpired = "DoorLock.KeyExpired";
    public const string KeyLost = "DoorLock.KeyLost";
    public const string KeyReturned = "DoorLock.KeyReturned";
    public const string DoorUnlocked = "DoorLock.DoorUnlocked";
    public const string DoorAccessDenied = "DoorLock.DoorAccessDenied";
    public const string DoorOffline = "DoorLock.DoorOffline";
    public const string DoorOnline = "DoorLock.DoorOnline";
    public const string BatteryLow = "DoorLock.BatteryLow";
    public const string TamperDetected = "DoorLock.TamperDetected";
    public const string LockSynced = "DoorLock.LockSynced";
}
