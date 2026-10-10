namespace ASMILE.BO.Core.FrontOffice.DoorLock;

public sealed class DoorLockDevice
{
    private DoorLockDevice() { }

    public Guid DeviceId { get; private set; }
    public Guid PropertyId { get; private set; }
    public string DeviceCode { get; private set; } = string.Empty;
    public string? RoomNumber { get; private set; }
    public Guid? RoomId { get; private set; }
    public string Vendor { get; private set; } = string.Empty;
    public string Model { get; private set; } = string.Empty;
    public string FirmwareVersion { get; private set; } = string.Empty;
    public string Status { get; private set; } = string.Empty;
    public int BatteryLevel { get; private set; }
    public DateTime? LastSeenAtUtc { get; private set; }
    public DateTime? LastBatteryChangeAtUtc { get; private set; }
    public int TotalAccesses { get; private set; }
    public int FailedAttempts { get; private set; }
    public bool IsSynced { get; private set; }
    public byte[]? RowVersion { get; private set; }

    public static DoorLockDevice Create(
        Guid propertyId,
        string deviceCode,
        string? roomNumber,
        Guid? roomId,
        string vendor,
        string model,
        string firmwareVersion,
        string status,
        int batteryLevel,
        DateTime? lastSeenAtUtc,
        DateTime? lastBatteryChangeAtUtc,
        int totalAccesses,
        int failedAttempts,
        bool isSynced)
    {
        if (propertyId == Guid.Empty) throw new ArgumentException("DOOR_LOCK_PROPERTY_REQUIRED");
        if (string.IsNullOrWhiteSpace(deviceCode)) throw new ArgumentException("DOOR_LOCK_DEVICE_CODE_REQUIRED");
        if (string.IsNullOrWhiteSpace(vendor)) throw new ArgumentException("DOOR_LOCK_VENDOR_REQUIRED");
        if (string.IsNullOrWhiteSpace(model)) throw new ArgumentException("DOOR_LOCK_MODEL_REQUIRED");
        if (batteryLevel is < 0 or > 100) throw new ArgumentException("DOOR_LOCK_BATTERY_INVALID");
        if (totalAccesses < 0 || failedAttempts < 0) throw new ArgumentException("DOOR_LOCK_COUNTER_INVALID");

        return new DoorLockDevice
        {
            DeviceId = Foundation.Identifiers.Uuid7.NewGuid(),
            PropertyId = propertyId,
            DeviceCode = deviceCode.Trim(),
            RoomNumber = Normalize(roomNumber),
            RoomId = roomId,
            Vendor = vendor.Trim(),
            Model = model.Trim(),
            FirmwareVersion = firmwareVersion?.Trim() ?? string.Empty,
            Status = status?.Trim() ?? string.Empty,
            BatteryLevel = batteryLevel,
            LastSeenAtUtc = lastSeenAtUtc,
            LastBatteryChangeAtUtc = lastBatteryChangeAtUtc,
            TotalAccesses = totalAccesses,
            FailedAttempts = failedAttempts,
            IsSynced = isSynced
        };
    }

    private static string? Normalize(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

public sealed class DoorLockEvent
{
    private DoorLockEvent() { }

    public Guid EventId { get; private set; }
    public Guid PropertyId { get; private set; }
    public Guid DeviceId { get; private set; }
    public string? RoomNumber { get; private set; }
    public string Type { get; private set; } = string.Empty;
    public string? KeyCode { get; private set; }
    public string? AccessLevel { get; private set; }
    public bool Granted { get; private set; }
    public DateTime OccurredAtUtc { get; private set; }
    public string? Detail { get; private set; }

    public static DoorLockEvent Create(
        Guid propertyId,
        Guid deviceId,
        string type,
        string? roomNumber,
        string? keyCode,
        string? accessLevel,
        bool granted,
        DateTime occurredAtUtc,
        string? detail)
    {
        if (propertyId == Guid.Empty) throw new ArgumentException("DOOR_LOCK_PROPERTY_REQUIRED");
        if (deviceId == Guid.Empty) throw new ArgumentException("DOOR_LOCK_DEVICE_REQUIRED");
        if (string.IsNullOrWhiteSpace(type)) throw new ArgumentException("DOOR_LOCK_EVENT_TYPE_REQUIRED");

        return new DoorLockEvent
        {
            EventId = Foundation.Identifiers.Uuid7.NewGuid(),
            PropertyId = propertyId,
            DeviceId = deviceId,
            RoomNumber = Normalize(roomNumber),
            Type = type.Trim(),
            KeyCode = Normalize(keyCode),
            AccessLevel = Normalize(accessLevel),
            Granted = granted,
            OccurredAtUtc = occurredAtUtc,
            Detail = Normalize(detail)
        };
    }

    private static string? Normalize(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
