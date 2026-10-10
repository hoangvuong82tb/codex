using ASMILE.BO.Application.FrontOffice.DoorLock;
using ASMILE.BO.Application.FrontOffice.Foundation.Contexts;
using ASMILE.BO.Core.FrontOffice.DoorLock;
using ASMILE.BO.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ASMILE.BO.Infrastructure.FrontOffice.DoorLock;

public sealed class EfDoorLockService(AppDbContext db, IFrontOfficeExecutionContextAccessor contextAccessor) : IDoorLockService
{
    public async Task<IReadOnlyList<LockDeviceDto>> GetDevicesAsync(Guid propertyId, string? floor, CancellationToken ct)
    {
        RequireProperty(propertyId);
        var query = db.DoorLockDevices.AsNoTracking().Where(x => x.PropertyId == propertyId);
        if (!string.IsNullOrWhiteSpace(floor)) query = query.Where(x => x.RoomNumber != null && x.RoomNumber.StartsWith(floor.Trim()));
        return (await query.OrderBy(x => x.DeviceCode).ToListAsync(ct)).Select(Map).ToList();
    }

    public async Task<LockDeviceDto> GetDeviceAsync(Guid deviceId, CancellationToken ct)
    {
        var propertyId = RequireContext().Property.PropertyId.Value;
        var device = await db.DoorLockDevices.AsNoTracking()
            .SingleOrDefaultAsync(x => x.PropertyId == propertyId && x.DeviceId == deviceId, ct)
            ?? throw new KeyNotFoundException("DOOR_LOCK_DEVICE_NOT_FOUND");
        return Map(device);
    }

    public async Task<IReadOnlyList<LockDeviceDto>> GetOfflineDevicesAsync(Guid propertyId, CancellationToken ct)
    {
        RequireProperty(propertyId);
        return (await db.DoorLockDevices.AsNoTracking()
            .Where(x => x.PropertyId == propertyId && x.Status == nameof(LockStatus.Offline))
            .OrderBy(x => x.DeviceCode).ToListAsync(ct)).Select(Map).ToList();
    }

    public async Task<IReadOnlyList<LockDeviceDto>> GetLowBatteryDevicesAsync(Guid propertyId, int threshold, CancellationToken ct)
    {
        RequireProperty(propertyId);
        if (threshold is < 0 or > 100) throw new ArgumentOutOfRangeException(nameof(threshold));
        return (await db.DoorLockDevices.AsNoTracking()
            .Where(x => x.PropertyId == propertyId && x.BatteryLevel <= threshold && x.Status != nameof(LockStatus.Decommissioned))
            .OrderBy(x => x.BatteryLevel).ThenBy(x => x.DeviceCode).ToListAsync(ct)).Select(Map).ToList();
    }

    private FrontOfficeExecutionContext RequireContext()
        => contextAccessor.Current ?? throw new InvalidOperationException("FRONT_OFFICE_CONTEXT_REQUIRED");

    private void RequireProperty(Guid propertyId)
    {
        if (propertyId == Guid.Empty || RequireContext().Property.PropertyId.Value != propertyId)
            throw new InvalidOperationException("DOOR_LOCK_PROPERTY_SCOPE_MISMATCH");
    }

    internal static LockDeviceDto Map(DoorLockDevice device)
        => new(device.DeviceId, device.DeviceCode, device.RoomNumber ?? string.Empty, device.RoomId,
            Parse<LockVendor>(device.Vendor), device.Model, device.FirmwareVersion, Parse<LockStatus>(device.Status),
            device.BatteryLevel, device.LastSeenAtUtc, device.LastBatteryChangeAtUtc,
            device.TotalAccesses, device.FailedAttempts, device.IsSynced);

    internal static T Parse<T>(string value) where T : struct, Enum
        => Enum.TryParse<T>(value, true, out var parsed) ? parsed : throw new InvalidOperationException("DOOR_LOCK_ENUM_INVALID");
}

public sealed class EfDoorEventService(AppDbContext db, IFrontOfficeExecutionContextAccessor contextAccessor) : IDoorEventService
{
    public async Task<IReadOnlyList<DoorEventDto>> GetEventsAsync(Guid deviceId, DateTime since, CancellationToken ct)
    {
        var propertyId = RequireContext().Property.PropertyId.Value;
        return (await db.DoorLockEvents.AsNoTracking()
            .Where(x => x.PropertyId == propertyId && x.DeviceId == deviceId && x.OccurredAtUtc >= since)
            .OrderByDescending(x => x.OccurredAtUtc).ToListAsync(ct)).Select(Map).ToList();
    }

    public async Task<IReadOnlyList<DoorEventDto>> GetFailedAttemptsAsync(Guid propertyId, DateTime since, CancellationToken ct)
    {
        RequireProperty(propertyId);
        return (await db.DoorLockEvents.AsNoTracking()
            .Where(x => x.PropertyId == propertyId && !x.Granted && x.OccurredAtUtc >= since)
            .OrderByDescending(x => x.OccurredAtUtc).ToListAsync(ct)).Select(Map).ToList();
    }

    public async Task<IReadOnlyList<DoorEventDto>> GetSecurityAlertsAsync(Guid propertyId, CancellationToken ct)
    {
        RequireProperty(propertyId);
        var types = new[] { nameof(DoorEventType.TamperDetected), nameof(DoorEventType.LockFailure), nameof(DoorEventType.Offline), nameof(DoorEventType.LowBattery) };
        return (await db.DoorLockEvents.AsNoTracking()
            .Where(x => x.PropertyId == propertyId && types.Contains(x.Type))
            .OrderByDescending(x => x.OccurredAtUtc).Take(50).ToListAsync(ct)).Select(Map).ToList();
    }

    private FrontOfficeExecutionContext RequireContext()
        => contextAccessor.Current ?? throw new InvalidOperationException("FRONT_OFFICE_CONTEXT_REQUIRED");

    private void RequireProperty(Guid propertyId)
    {
        if (propertyId == Guid.Empty || RequireContext().Property.PropertyId.Value != propertyId)
            throw new InvalidOperationException("DOOR_LOCK_PROPERTY_SCOPE_MISMATCH");
    }

    internal static DoorEventDto Map(DoorLockEvent e)
        => new(e.EventId, e.DeviceId, e.RoomNumber ?? string.Empty, EfDoorLockService.Parse<DoorEventType>(e.Type), e.KeyCode,
            e.AccessLevel is null ? null : EfDoorLockService.Parse<AccessLevel>(e.AccessLevel), e.Granted, e.OccurredAtUtc, e.Detail);
}

public sealed class EfDoorLockDashboardService(
    AppDbContext db,
    IFrontOfficeExecutionContextAccessor contextAccessor,
    IClock clock) : IDoorLockDashboardService
{
    public async Task<DoorLockDashboardDto> GetDashboardAsync(Guid propertyId, CancellationToken ct)
    {
        if (propertyId == Guid.Empty || (contextAccessor.Current?.Property.PropertyId.Value ?? Guid.Empty) != propertyId)
            throw new InvalidOperationException("DOOR_LOCK_PROPERTY_SCOPE_MISMATCH");
        var now = clock.UtcNow;
        var devices = await db.DoorLockDevices.AsNoTracking().Where(x => x.PropertyId == propertyId).ToListAsync(ct);
        var events = await db.DoorLockEvents.AsNoTracking().Where(x => x.PropertyId == propertyId && x.OccurredAtUtc >= now.Date).ToListAsync(ct);
        var alerts = events.Where(x => x.Type is nameof(DoorEventType.TamperDetected) or nameof(DoorEventType.LockFailure) or nameof(DoorEventType.Offline) or nameof(DoorEventType.LowBattery))
            .OrderByDescending(x => x.OccurredAtUtc).Take(20).Select(EfDoorEventService.Map).ToList();
        var activeKeys = await db.KeyCardCredentials.AsNoTracking().CountAsync(x => x.PropertyId == propertyId && (x.Status == "Issued" || x.Status == "Active" || x.Status == "Encoded"), ct);
        var issued = events.Count(x => x.Type == DoorLockEvents.KeyIssued);
        var revoked = events.Count(x => x.Type is DoorLockEvents.KeyRevoked or DoorLockEvents.KeyLost or DoorLockEvents.KeyReturned);
        var online = devices.Count(x => x.Status == nameof(LockStatus.Online));
        var health = devices.Count == 0 ? 0m : decimal.Round(online * 100m / devices.Count, 2);
        return new DoorLockDashboardDto(devices.Count, online,
            devices.Count(x => x.Status == nameof(LockStatus.Offline)),
            devices.Count(x => x.Status == nameof(LockStatus.LowBattery)),
            devices.Count(x => x.Status == nameof(LockStatus.Tampered)), activeKeys, issued, revoked,
            events.Count(x => x.Granted),
            events.Count(x => !x.Granted),
            events.Count(x => !x.Granted),
            health,
            devices.Where(x => x.Status == nameof(LockStatus.Offline)).Select(EfDoorLockService.Map).Take(20).ToList(),
            devices.Where(x => x.BatteryLevel <= 20 && x.Status != nameof(LockStatus.Decommissioned)).Select(EfDoorLockService.Map).Take(20).ToList(), alerts);
    }
}
