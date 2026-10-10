using ASMILE.BO.Application.FrontOffice.DoorLock;
using ASMILE.BO.Application.FrontOffice.Foundation.Contexts;
using ASMILE.BO.Core.FrontOffice.Stay;
using ASMILE.BO.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ASMILE.BO.Infrastructure.FrontOffice.DoorLock;

/// <summary>
/// Persistent key-card service. Hardware encoding remains an integration seam;
/// this service is responsible for the authoritative credential and audit data.
/// </summary>
public sealed class EfKeyCardService(
    AppDbContext db,
    IFrontOfficeExecutionContextAccessor contextAccessor,
    IClock clock) : IKeyCardService
{
    public async Task<KeyCardDto> IssueKeyAsync(
        Guid? guestId,
        Guid? stayId,
        Guid roomId,
        AccessLevel level,
        DateTime? validUntil,
        int encoderId,
        CancellationToken ct)
    {
        var context = RequireContext();
        if (stayId is null || stayId == Guid.Empty)
        {
            throw new InvalidOperationException("KEYCARD_STAY_REQUIRED");
        }

        var stay = await db.HotelStays.AsNoTracking()
            .SingleOrDefaultAsync(x => x.PropertyId == context.PropertyId && x.HotelStayId == stayId.Value, ct)
            ?? throw new InvalidOperationException("KEYCARD_STAY_NOT_FOUND");

        if (!stay.Status.IsOccupying())
        {
            throw new InvalidOperationException("KEYCARD_STAY_NOT_OCCUPYING");
        }

        var room = await db.HotelRooms.AsNoTracking()
            .SingleOrDefaultAsync(x => x.PropertyId == context.PropertyId && x.HotelRoomId == roomId, ct)
            ?? throw new InvalidOperationException("KEYCARD_ROOM_NOT_FOUND");

        var effectiveGuestId = guestId ?? stay.GuestId;
        if (effectiveGuestId is not null)
        {
            var guestExists = await db.GuestProfiles.AsNoTracking()
                .AnyAsync(x => x.PropertyId == context.PropertyId && x.GuestProfileId == effectiveGuestId.Value, ct);
            if (!guestExists)
            {
                throw new InvalidOperationException("KEYCARD_GUEST_NOT_FOUND");
            }
        }

        var now = clock.UtcNow;
        var credential = KeyCardCredential.Issue(
            context.PropertyId,
            await GenerateCardSerialAsync(ct),
            stay.HotelStayId,
            effectiveGuestId,
            room.HotelRoomId,
            now,
            context.ActorId,
            validUntil,
            level.ToString(),
            encoderId,
            level == AccessLevel.Master);
        db.KeyCardCredentials.Add(credential);
        db.KeyCardEvents.Add(KeyCardEvent.Create(
            context.PropertyId,
            credential.CredentialId,
            DoorLockEvents.KeyIssued,
            now,
            room.RoomNumber,
            true,
            $"Level={level};EncoderId={encoderId};IsMaster={level == AccessLevel.Master}"));
        await db.SaveChangesAsync(ct);

        return await ToDtoAsync(credential, stay.GuestNameSnapshot, room.RoomNumber, ct);
    }

    public async Task<KeyCardDto> IssueDuplicateAsync(Guid originalKeyId, CancellationToken ct)
    {
        var context = RequireContext();
        var original = await db.KeyCardCredentials
            .SingleOrDefaultAsync(x => x.PropertyId == context.PropertyId && x.CredentialId == originalKeyId, ct)
            ?? throw new InvalidOperationException("KEYCARD_NOT_FOUND");

        if (MapStatus(original.Status) is not (KeyStatus.Active or KeyStatus.Encoded))
        {
            throw new InvalidOperationException("KEYCARD_DUPLICATE_SOURCE_NOT_ACTIVE");
        }

        var now = clock.UtcNow;
        var duplicate = KeyCardCredential.Issue(
            context.PropertyId,
            await GenerateCardSerialAsync(ct),
            original.StayId,
            original.GuestId,
            original.HotelRoomId,
            now,
            context.ActorId,
            original.ExpiresAtUtc,
            original.AccessLevel,
            original.EncoderId,
            original.IsMaster);
        db.KeyCardCredentials.Add(duplicate);
        db.KeyCardEvents.Add(KeyCardEvent.Create(
            context.PropertyId,
            duplicate.CredentialId,
            DoorLockEvents.KeyIssued,
            now,
            null,
            true,
            $"DuplicateOf={original.CredentialId}"));
        await db.SaveChangesAsync(ct);

        return await ToDtoAsync(duplicate, null, null, ct);
    }

    public Task<KeyCardDto> RevokeKeyAsync(Guid keyId, string reason, Guid actorId, CancellationToken ct)
        => MutateAsync(keyId, actorId, DoorLockEvents.KeyRevoked, (credential, atUtc) => credential.Disable(actorId, reason, atUtc), reason, ct);

    public Task<KeyCardDto> SuspendKeyAsync(Guid keyId, CancellationToken ct)
        => MutateAsync(keyId, null, DoorLockEvents.KeyRevoked, (credential, atUtc) => credential.Suspend(RequireContext().ActorId, atUtc), "Suspended", ct);

    public Task<KeyCardDto> ReactivateKeyAsync(Guid keyId, CancellationToken ct)
        => MutateAsync(keyId, null, DoorLockEvents.KeyIssued, (credential, atUtc) => credential.Reactivate(atUtc), "Reactivated", ct);

    public Task<KeyCardDto> ReportLostAsync(Guid keyId, CancellationToken ct)
        => MutateAsync(keyId, null, DoorLockEvents.KeyLost, (credential, atUtc) => credential.MarkLost(RequireContext().ActorId, atUtc, "Reported lost"), "Reported lost", ct);

    public Task<KeyCardDto> ReturnKeyAsync(Guid keyId, CancellationToken ct)
        => MutateAsync(keyId, null, DoorLockEvents.KeyReturned, (credential, atUtc) => credential.Return(atUtc), "Returned to inventory", ct);

    public async Task<IReadOnlyList<KeyCardDto>> GetKeysForStayAsync(Guid stayId, CancellationToken ct)
    {
        var context = RequireContext();
        var stay = await db.HotelStays.AsNoTracking()
            .SingleOrDefaultAsync(x => x.PropertyId == context.PropertyId && x.HotelStayId == stayId, ct);
        if (stay is null)
        {
            return [];
        }

        var credentials = await db.KeyCardCredentials.AsNoTracking()
            .Where(x => x.PropertyId == context.PropertyId && x.StayId == stayId)
            .OrderByDescending(x => x.IssuedAtUtc)
            .ToListAsync(ct);
        var roomNumber = await GetRoomNumberAsync(credentials.Select(x => x.HotelRoomId).FirstOrDefault(), context.PropertyId, ct);
        return credentials.Select(x => ToDto(x, stay.GuestNameSnapshot, roomNumber)).ToList();
    }

    public async Task<KeyInventoryDto> GetInventoryAsync(Guid propertyId, CancellationToken ct)
    {
        var context = RequireContext();
        if (context.PropertyId != propertyId)
        {
            throw new InvalidOperationException("KEYCARD_PROPERTY_SCOPE_MISMATCH");
        }

        var statuses = await db.KeyCardCredentials.AsNoTracking()
            .Where(x => x.PropertyId == propertyId)
            .Select(x => x.Status)
            .ToListAsync(ct);
        var active = statuses.Count(x => x is "Issued" or "Active" or "Encoded");
        var suspended = statuses.Count(x => x == "Suspended");
        var revoked = statuses.Count(x => x is "Revoked" or "Disabled");
        var expired = statuses.Count(x => x == "Expired");
        var lost = statuses.Count(x => x == "Lost");
        return new KeyInventoryDto(statuses.Count, active, suspended, revoked, expired, lost, 0);
    }

    public async Task<IReadOnlyList<KeyCardDto>> AutoRevokeOnCheckoutAsync(Guid stayId, CancellationToken ct)
    {
        var context = RequireContext();
        var credentials = await db.KeyCardCredentials
            .Where(x => x.PropertyId == context.PropertyId && x.StayId == stayId && (x.Status == "Issued" || x.Status == "Active" || x.Status == "Encoded"))
            .ToListAsync(ct);
        if (credentials.Count == 0)
        {
            return [];
        }

        var now = clock.UtcNow;
        foreach (var credential in credentials)
        {
            credential.Disable(context.ActorId, "Checkout", now);
            db.KeyCardEvents.Add(KeyCardEvent.Create(context.PropertyId, credential.CredentialId, DoorLockEvents.KeyRevoked, now, null, true, "Auto revoke on checkout"));
        }

        await db.SaveChangesAsync(ct);
        var stay = await db.HotelStays.AsNoTracking().SingleOrDefaultAsync(x => x.PropertyId == context.PropertyId && x.HotelStayId == stayId, ct);
        return credentials.Select(x => ToDto(x, stay?.GuestNameSnapshot, null)).ToList();
    }

    private async Task<KeyCardDto> MutateAsync(
        Guid keyId,
        Guid? actorId,
        string eventType,
        Action<KeyCardCredential, DateTime> mutation,
        string notes,
        CancellationToken ct)
    {
        var context = RequireContext();
        var credential = await db.KeyCardCredentials
            .SingleOrDefaultAsync(x => x.PropertyId == context.PropertyId && x.CredentialId == keyId, ct)
            ?? throw new InvalidOperationException("KEYCARD_NOT_FOUND");
        var now = clock.UtcNow;
        mutation(credential, now);
        db.KeyCardEvents.Add(KeyCardEvent.Create(context.PropertyId, credential.CredentialId, eventType, now, null, true, notes));
        await db.SaveChangesAsync(ct);
        return await ToDtoAsync(credential, null, null, ct);
    }

    private async Task<KeyCardDto> ToDtoAsync(KeyCardCredential credential, string? guestName, string? roomNumber, CancellationToken ct)
    {
        guestName ??= await db.GuestProfiles.AsNoTracking()
            .Where(x => x.PropertyId == credential.PropertyId && x.GuestProfileId == credential.GuestId)
            .Select(x => x.FullName)
            .SingleOrDefaultAsync(ct);
        roomNumber ??= await GetRoomNumberAsync(credential.HotelRoomId, credential.PropertyId, ct);
        return ToDto(credential, guestName, roomNumber);
    }

    private static KeyCardDto ToDto(KeyCardCredential credential, string? guestName, string? roomNumber)
        => new(
            credential.CredentialId,
            credential.CardSerial,
            KeyType.RFID,
            credential.GuestId,
            guestName,
            credential.StayId,
            roomNumber,
            credential.HotelRoomId is { } roomId ? [roomId] : [],
            Enum.TryParse<AccessLevel>(credential.AccessLevel, ignoreCase: true, out var level) ? level : AccessLevel.Guest,
            MapStatus(credential.Status),
            credential.IssuedAtUtc,
            credential.ExpiresAtUtc,
            credential.IssuedAtUtc,
            credential.IssuedBy,
            null,
            credential.DisabledAtUtc,
            credential.DisabledBy,
            credential.DisableReason,
            credential.EncoderId,
            credential.IsMaster);

    private async Task<string> GenerateCardSerialAsync(CancellationToken ct)
    {
        string serial;
        do
        {
            serial = $"KEY-{Guid.NewGuid():N}"[..20].ToUpperInvariant();
        }
        while (await db.KeyCardCredentials.AnyAsync(x => x.CardSerial == serial, ct));
        return serial;
    }

    private async Task<string?> GetRoomNumberAsync(Guid? roomId, Guid propertyId, CancellationToken ct)
        => roomId is null
            ? null
            : await db.HotelRooms.AsNoTracking()
                .Where(x => x.PropertyId == propertyId && x.HotelRoomId == roomId.Value)
                .Select(x => x.RoomNumber)
                .SingleOrDefaultAsync(ct);

    private (Guid PropertyId, Guid? ActorId) RequireContext()
    {
        var context = contextAccessor.Current ?? throw new InvalidOperationException("FRONT_OFFICE_CONTEXT_REQUIRED");
        return (context.Property.PropertyId.Value, context.Actor.ActorId?.Value);
    }

    private static KeyStatus MapStatus(string status)
        => status switch
        {
            "Issued" or "Active" => KeyStatus.Active,
            "Encoded" => KeyStatus.Encoded,
            "Suspended" => KeyStatus.Suspended,
            "Revoked" or "Disabled" => KeyStatus.Revoked,
            "Expired" => KeyStatus.Expired,
            "Lost" => KeyStatus.Lost,
            "Returned" => KeyStatus.Returned,
            _ => KeyStatus.Revoked
        };
}
