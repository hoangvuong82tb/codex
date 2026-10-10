namespace ASMILE.BO.Core.FrontOffice.Stay;

public sealed class KeyCardCredential
{
    private KeyCardCredential() { }

    public Guid CredentialId { get; private set; }
    public Guid PropertyId { get; private set; }
    public string CardSerial { get; private set; } = string.Empty;
    public string Status { get; private set; } = "Issued";
    public string AccessLevel { get; private set; } = "Guest";
    public int EncoderId { get; private set; }
    public bool IsMaster { get; private set; }
    public Guid StayId { get; private set; }
    public Guid? GuestId { get; private set; }
    public Guid? HotelRoomId { get; private set; }
    public DateTime IssuedAtUtc { get; private set; }
    public Guid? IssuedBy { get; private set; }
    public DateTime? ExpiresAtUtc { get; private set; }
    public DateTime? DisabledAtUtc { get; private set; }
    public Guid? DisabledBy { get; private set; }
    public string? DisableReason { get; private set; }
    public DateTime? ReturnedAtUtc { get; private set; }
    public byte[]? RowVersion { get; private set; }
    public ICollection<KeyCardEvent> Events { get; private set; } = new List<KeyCardEvent>();

    public static KeyCardCredential Issue(Guid propertyId, string cardSerial, Guid stayId, Guid? guestId, Guid? roomId, DateTime issuedAtUtc, Guid? issuedBy, DateTime? expiresAtUtc, string accessLevel = "Guest", int encoderId = 0, bool isMaster = false) => new()
    {
        CredentialId = Guid.NewGuid(), PropertyId = propertyId, CardSerial = cardSerial,
        StayId = stayId, GuestId = guestId, HotelRoomId = roomId, IssuedAtUtc = issuedAtUtc,
        IssuedBy = issuedBy, ExpiresAtUtc = expiresAtUtc, Status = "Issued",
        AccessLevel = accessLevel, EncoderId = encoderId, IsMaster = isMaster
    };

    public void Disable(Guid? disabledBy, string reason, DateTime atUtc) { Status = "Revoked"; DisabledBy = disabledBy; DisableReason = reason; DisabledAtUtc = atUtc; }
    public void Suspend(Guid? actorId, DateTime atUtc) { Status = "Suspended"; DisabledBy = actorId; DisabledAtUtc = atUtc; DisableReason = null; }
    public void Reactivate(DateTime atUtc) { Status = "Active"; DisabledBy = null; DisabledAtUtc = null; DisableReason = null; }
    public void MarkLost(Guid? actorId, DateTime atUtc, string reason) { Status = "Lost"; DisabledBy = actorId; DisabledAtUtc = atUtc; DisableReason = reason; }
    public void Return(DateTime atUtc) { Status = "Returned"; ReturnedAtUtc = atUtc; }
}

public sealed class KeyCardEvent
{
    private KeyCardEvent() { }

    public Guid EventId { get; private set; }
    public Guid CredentialId { get; private set; }
    public Guid PropertyId { get; private set; }
    public string EventType { get; private set; } = string.Empty;
    public DateTime OccurredAtUtc { get; private set; }
    public string? DoorId { get; private set; }
    public bool AccessGranted { get; private set; }
    public string? Notes { get; private set; }
    public KeyCardCredential? Credential { get; private set; }

    public static KeyCardEvent Create(Guid propertyId, Guid credentialId, string eventType, DateTime occurredAtUtc, string? doorId, bool accessGranted, string? notes) => new()
    {
        EventId = Guid.NewGuid(), PropertyId = propertyId, CredentialId = credentialId,
        EventType = eventType, OccurredAtUtc = occurredAtUtc, DoorId = doorId,
        AccessGranted = accessGranted, Notes = notes
    };
}
