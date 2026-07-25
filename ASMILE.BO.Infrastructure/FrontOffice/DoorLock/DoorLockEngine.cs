using ASMILE.BO.Application.FrontOffice.DoorLock;

namespace ASMILE.BO.Infrastructure.FrontOffice.DoorLock;

/// <summary>Generic lock vendor adapter — base implementation.</summary>
public sealed class GenericLockAdapter(LockVendor vendor) : ILockVendorAdapter
{
    public LockVendor Vendor => vendor;
    public Task<bool> EncodeKeyAsync(KeyCardDto key, CancellationToken ct) => Task.FromResult(true);
    public Task<bool> RevokeKeyAsync(string keyCode, CancellationToken ct) => Task.FromResult(true);
    public Task<bool> SyncLockAsync(LockDeviceDto device, CancellationToken ct) => Task.FromResult(true);
    public Task<IReadOnlyList<DoorEventDto>> PullEventsAsync(Guid deviceId, DateTime since, CancellationToken ct) => Task.FromResult<IReadOnlyList<DoorEventDto>>([]);
    public Task<bool> PingAsync(Guid deviceId, CancellationToken ct) => Task.FromResult(true);
    public Task<bool> EmergencyOpenAsync(Guid deviceId, string reason, Guid actorId, CancellationToken ct) => Task.FromResult(true);
}

/// <summary>Adapter registry — selects the right adapter per lock vendor.</summary>
public sealed class LockAdapterRegistry(IEnumerable<ILockVendorAdapter> adapters)
{
    private readonly Dictionary<LockVendor, ILockVendorAdapter> _adapters = adapters.ToDictionary(a => a.Vendor);
    public ILockVendorAdapter Get(LockVendor v) => _adapters.TryGetValue(v, out var a) ? a : _adapters[LockVendor.Generic];
}

public sealed class DoorLockService(LockAdapterRegistry registry) : IDoorLockService
{
    public Task<IReadOnlyList<LockDeviceDto>> GetDevicesAsync(Guid propertyId, string? floor, CancellationToken ct) => Task.FromResult<IReadOnlyList<LockDeviceDto>>([SampleDevice()]);
    public Task<LockDeviceDto> GetDeviceAsync(Guid deviceId, CancellationToken ct) => Task.FromResult(SampleDevice());
    public Task<IReadOnlyList<LockDeviceDto>> GetOfflineDevicesAsync(Guid propertyId, CancellationToken ct) => Task.FromResult<IReadOnlyList<LockDeviceDto>>([SampleDevice(LockStatus.Offline)]);
    public Task<IReadOnlyList<LockDeviceDto>> GetLowBatteryDevicesAsync(Guid propertyId, int threshold, CancellationToken ct) => Task.FromResult<IReadOnlyList<LockDeviceDto>>([SampleDevice(LockStatus.LowBattery)]);
    private static LockDeviceDto SampleDevice(LockStatus s = LockStatus.Online) => new(Guid.NewGuid(), "LCK-101", "101", Guid.NewGuid(), LockVendor.VingCard, "VisionLine", "3.2.1", s, 85, DateTime.UtcNow.AddMinutes(-5), DateTime.UtcNow.AddMonths(-2), 15000, 3, true);
}

public sealed class KeyCardService(LockAdapterRegistry registry) : IKeyCardService
{
    public Task<KeyCardDto> IssueKeyAsync(Guid? guestId, Guid? stayId, Guid roomId, AccessLevel level, DateTime? validUntil, int encoderId, CancellationToken ct) => Task.FromResult(new KeyCardDto(Guid.NewGuid(), $"KEY-{Guid.NewGuid():N}"[..8], KeyType.RFID, guestId, "Guest", stayId, "101", [roomId], level, KeyStatus.Active, DateTime.UtcNow, validUntil, DateTime.UtcNow, Guid.NewGuid(), "Front Desk", null, null, null, encoderId, level >= AccessLevel.Supervisor));
    public Task<KeyCardDto> IssueDuplicateAsync(Guid originalKeyId, CancellationToken ct) => Task.FromResult(new KeyCardDto(Guid.NewGuid(), $"KEY-{Guid.NewGuid():N}"[..8], KeyType.RFID, Guid.NewGuid(), "Guest", Guid.NewGuid(), "101", [Guid.NewGuid()], AccessLevel.Guest, KeyStatus.Active, DateTime.UtcNow, DateTime.UtcNow.AddDays(3), DateTime.UtcNow, Guid.NewGuid(), "Front Desk", null, null, null, 1, false));
    public Task<KeyCardDto> RevokeKeyAsync(Guid keyId, string reason, Guid actorId, CancellationToken ct) => Task.FromResult(new KeyCardDto(keyId, "KEY-001", KeyType.RFID, Guid.NewGuid(), "Guest", Guid.NewGuid(), "101", [Guid.NewGuid()], AccessLevel.Guest, KeyStatus.Revoked, DateTime.UtcNow.AddDays(-2), DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(-2), Guid.NewGuid(), "Front Desk", DateTime.UtcNow, actorId, reason, 1, false));
    public Task<KeyCardDto> SuspendKeyAsync(Guid keyId, CancellationToken ct) => Task.FromResult(new KeyCardDto(keyId, "KEY-001", KeyType.RFID, null, null, null, null, [], AccessLevel.Guest, KeyStatus.Suspended, null, null, null, null, null, null, null, null, 1, false));
    public Task<KeyCardDto> ReactivateKeyAsync(Guid keyId, CancellationToken ct) => Task.FromResult(new KeyCardDto(keyId, "KEY-001", KeyType.RFID, null, null, null, null, [], AccessLevel.Guest, KeyStatus.Active, DateTime.UtcNow, DateTime.UtcNow.AddDays(2), null, null, null, null, null, null, 1, false));
    public Task<KeyCardDto> ReportLostAsync(Guid keyId, CancellationToken ct) => Task.FromResult(new KeyCardDto(keyId, "KEY-001", KeyType.RFID, null, null, null, null, [], AccessLevel.Guest, KeyStatus.Lost, null, null, null, null, null, null, null, "Reported lost by guest", 1, false));
    public Task<IReadOnlyList<KeyCardDto>> GetKeysForStayAsync(Guid stayId, CancellationToken ct) => Task.FromResult<IReadOnlyList<KeyCardDto>>([new KeyCardDto(Guid.NewGuid(), "KEY-001", KeyType.RFID, Guid.NewGuid(), "Guest", stayId, "101", [Guid.NewGuid()], AccessLevel.Guest, KeyStatus.Active, DateTime.UtcNow, DateTime.UtcNow.AddDays(3), DateTime.UtcNow, Guid.NewGuid(), "Front Desk", null, null, null, 1, false)]);
    public Task<KeyInventoryDto> GetInventoryAsync(Guid propertyId, CancellationToken ct) => Task.FromResult(new KeyInventoryDto(500, 120, 5, 10, 50, 3, 312));
    public Task<IReadOnlyList<KeyCardDto>> AutoRevokeOnCheckoutAsync(Guid stayId, CancellationToken ct) => Task.FromResult<IReadOnlyList<KeyCardDto>>([]);
}

public sealed class DoorEventService : IDoorEventService
{
    public Task<IReadOnlyList<DoorEventDto>> GetEventsAsync(Guid deviceId, DateTime since, CancellationToken ct) => Task.FromResult<IReadOnlyList<DoorEventDto>>([new DoorEventDto(Guid.NewGuid(), deviceId, "101", DoorEventType.AccessGranted, "KEY-001", AccessLevel.Guest, true, DateTime.UtcNow.AddMinutes(-10), null)]);
    public Task<IReadOnlyList<DoorEventDto>> GetFailedAttemptsAsync(Guid propertyId, DateTime since, CancellationToken ct) => Task.FromResult<IReadOnlyList<DoorEventDto>>([new DoorEventDto(Guid.NewGuid(), Guid.NewGuid(), "102", DoorEventType.AccessDenied, "KEY-EXPIRED", AccessLevel.Guest, false, DateTime.UtcNow.AddMinutes(-30), "Key expired")]);
    public Task<IReadOnlyList<DoorEventDto>> GetSecurityAlertsAsync(Guid propertyId, CancellationToken ct) => Task.FromResult<IReadOnlyList<DoorEventDto>>([new DoorEventDto(Guid.NewGuid(), Guid.NewGuid(), "101", DoorEventType.TamperDetected, null, null, false, DateTime.UtcNow.AddHours(-1), "Tamper alert - door forced")]);
}

public sealed class DoorLockDashboardService : IDoorLockDashboardService
{
    public Task<DoorLockDashboardDto> GetDashboardAsync(Guid propertyId, CancellationToken ct) => Task.FromResult(new DoorLockDashboardDto(120, 115, 3, 2, 0, 120, 45, 8, 850, 15, 3, 96m, [], [], []));
}
