using ASMILE.BO.Core.FrontOffice.Foundation.Identifiers;

namespace ASMILE.BO.Core.FrontOffice.Reservation;

public enum ReservationDenialReason { NoAvailability, Rate, RoomType, GuestDeclined, Other }
public enum ReservationDenialStatus { Open, Recovered, Closed }

/// <summary>
/// Lost-business record for a reservation request that did not become a
/// reservation.  It is deliberately separate from Reservation because a
/// denied request has no reservation number and must remain auditable.
/// </summary>
public sealed class ReservationDenial
{
    private ReservationDenial() { }

    public Guid DenialId { get; private set; }
    public Guid PropertyId { get; private set; }
    public string DenialNumber { get; private set; } = string.Empty;
    public string GuestName { get; private set; } = string.Empty;
    public string? GuestPhone { get; private set; }
    public DateOnly ArrivalDate { get; private set; }
    public DateOnly DepartureDate { get; private set; }
    public int AdultCount { get; private set; }
    public int ChildCount { get; private set; }
    public Guid? RoomTypeId { get; private set; }
    public string Source { get; private set; } = "Direct";
    public ReservationDenialReason Reason { get; private set; }
    public ReservationDenialStatus Status { get; private set; } = ReservationDenialStatus.Open;
    public string? Notes { get; private set; }
    public Guid? RecoveredReservationId { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? ClosedAtUtc { get; private set; }
    public byte[]? RowVersion { get; private set; }

    public static ReservationDenial Create(
        Guid propertyId,
        string denialNumber,
        string guestName,
        string? guestPhone,
        DateOnly arrivalDate,
        DateOnly departureDate,
        int adultCount,
        int childCount,
        Guid? roomTypeId,
        string source,
        ReservationDenialReason reason,
        string? notes,
        DateTime nowUtc)
    {
        if (propertyId == Guid.Empty) throw new ArgumentException("Property is required.", nameof(propertyId));
        if (string.IsNullOrWhiteSpace(denialNumber)) throw new ArgumentException("Denial number is required.", nameof(denialNumber));
        if (string.IsNullOrWhiteSpace(guestName)) throw new ArgumentException("Guest name is required.", nameof(guestName));
        if (departureDate <= arrivalDate) throw new ArgumentException("Departure must be after arrival.", nameof(departureDate));
        if (adultCount < 1 || childCount < 0) throw new ArgumentException("Guest count is invalid.");
        if (string.IsNullOrWhiteSpace(source)) throw new ArgumentException("Source is required.", nameof(source));

        return new ReservationDenial
        {
            DenialId = Uuid7.NewGuid(),
            PropertyId = propertyId,
            DenialNumber = denialNumber.Trim(),
            GuestName = guestName.Trim(),
            GuestPhone = NullIfEmpty(guestPhone),
            ArrivalDate = arrivalDate,
            DepartureDate = departureDate,
            AdultCount = adultCount,
            ChildCount = childCount,
            RoomTypeId = roomTypeId,
            Source = source.Trim(),
            Reason = reason,
            Notes = NullIfEmpty(notes),
            CreatedAtUtc = nowUtc,
            Status = ReservationDenialStatus.Open
        };
    }

    public void Recover(Guid reservationId, DateTime nowUtc)
    {
        if (reservationId == Guid.Empty) throw new ArgumentException("Reservation is required.", nameof(reservationId));
        if (Status != ReservationDenialStatus.Open) throw new InvalidOperationException("RESERVATION_DENIAL_NOT_OPEN");
        RecoveredReservationId = reservationId;
        Status = ReservationDenialStatus.Recovered;
        ClosedAtUtc = nowUtc;
    }

    public void Close(DateTime nowUtc)
    {
        if (Status != ReservationDenialStatus.Open) throw new InvalidOperationException("RESERVATION_DENIAL_NOT_OPEN");
        Status = ReservationDenialStatus.Closed;
        ClosedAtUtc = nowUtc;
    }

    private static string? NullIfEmpty(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
