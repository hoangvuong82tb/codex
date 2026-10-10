using ASMILE.BO.Core.FrontOffice.Foundation.Identifiers;

namespace ASMILE.BO.Core.FrontOffice.Stay;

public enum HotelStayOccupantType
{
    Adult = 1,
    Child = 2
}

/// <summary>
/// A real guest profile registered as an additional occupant of a stay.
/// The primary guest remains on HotelStay; this entity keeps companions
/// auditable without overwriting that snapshot.
/// </summary>
public sealed class HotelStayOccupant
{
    private HotelStayOccupant() { }

    public Guid OccupantId { get; private set; }
    public Guid PropertyId { get; private set; }
    public Guid HotelStayId { get; private set; }
    public Guid GuestProfileId { get; private set; }
    public string GuestNameSnapshot { get; private set; } = string.Empty;
    public string? GuestPhoneSnapshot { get; private set; }
    public HotelStayOccupantType OccupantType { get; private set; }
    public DateTime AddedAtUtc { get; private set; }
    public Guid? AddedBy { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime? RemovedAtUtc { get; private set; }
    public Guid? RemovedBy { get; private set; }
    public byte[]? RowVersion { get; private set; }

    public static HotelStayOccupant Create(
        PropertyId propertyId,
        Guid hotelStayId,
        Guid guestProfileId,
        string guestName,
        string? guestPhone,
        HotelStayOccupantType occupantType,
        DateTime addedAtUtc,
        ActorId? addedBy)
    {
        if (hotelStayId == Guid.Empty) throw new ArgumentException("STAY_OCCUPANT_STAY_REQUIRED");
        if (guestProfileId == Guid.Empty) throw new ArgumentException("STAY_OCCUPANT_GUEST_REQUIRED");
        if (string.IsNullOrWhiteSpace(guestName)) throw new ArgumentException("STAY_OCCUPANT_NAME_REQUIRED");

        return new HotelStayOccupant
        {
            OccupantId = Uuid7.NewGuid(),
            PropertyId = propertyId.Value,
            HotelStayId = hotelStayId,
            GuestProfileId = guestProfileId,
            GuestNameSnapshot = guestName.Trim(),
            GuestPhoneSnapshot = Normalize(guestPhone),
            OccupantType = occupantType,
            AddedAtUtc = addedAtUtc,
            AddedBy = addedBy?.Value,
            IsActive = true
        };
    }

    public void Remove(DateTime removedAtUtc, ActorId? removedBy)
    {
        if (!IsActive) throw new InvalidOperationException("STAY_OCCUPANT_ALREADY_REMOVED");
        IsActive = false;
        RemovedAtUtc = removedAtUtc;
        RemovedBy = removedBy?.Value;
    }

    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
