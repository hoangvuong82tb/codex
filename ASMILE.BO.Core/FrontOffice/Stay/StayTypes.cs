using ASMILE.BO.Core.FrontOffice.Foundation.DomainEvents;
using ASMILE.BO.Core.FrontOffice.Foundation.Identifiers;

namespace ASMILE.BO.Core.FrontOffice.Stay;

/// <summary>Complete Stay state machine: 10 states covering the full guest lifecycle.</summary>
public enum HotelStayStatus
{
    /// <summary>Stay has been prepared from a reservation, awaiting pre-arrival.</summary>
    PendingArrival,
    /// <summary>Guest confirmed arrival within 24h; room readiness check started.</summary>
    PreArrival,
    /// <summary>Guest physically checked in; room key issued.</summary>
    CheckedIn,
    /// <summary>Guest is occupying the room (post check-in processing complete).</summary>
    InHouse,
    /// <summary>Guest has been moved to a different room mid-stay.</summary>
    RoomMoved,
    /// <summary>Stay departure date has been extended.</summary>
    Extended,
    /// <summary>Guest is expected to depart today.</summary>
    ExpectedDeparture,
    /// <summary>Guest has checked out; room vacated.</summary>
    CheckedOut,
    /// <summary>All post-stay processing complete (folio closed, payment settled).</summary>
    Completed,
    /// <summary>Stay data archived for long-term retention.</summary>
    Archived,
    /// <summary>Stay was cancelled before arrival.</summary>
    Cancelled
}

public static class HotelStayStatusExtensions
{
    public static bool IsActive(this HotelStayStatus s) => s is HotelStayStatus.PendingArrival or HotelStayStatus.PreArrival or HotelStayStatus.CheckedIn or HotelStayStatus.InHouse or HotelStayStatus.RoomMoved or HotelStayStatus.Extended or HotelStayStatus.ExpectedDeparture;
    public static bool IsTerminal(this HotelStayStatus s) => s is HotelStayStatus.Cancelled or HotelStayStatus.Completed or HotelStayStatus.Archived;
    public static bool IsOccupying(this HotelStayStatus s) => s is HotelStayStatus.CheckedIn or HotelStayStatus.InHouse or HotelStayStatus.RoomMoved or HotelStayStatus.Extended or HotelStayStatus.ExpectedDeparture;
    public static bool IsPreCheckIn(this HotelStayStatus s) => s is HotelStayStatus.PendingArrival or HotelStayStatus.PreArrival;
}

/// <summary>Stay period with duration tracking.</summary>
public readonly record struct HotelStayPeriod(DateOnly ScheduledArrivalDate, DateOnly ScheduledDepartureDate)
{
    public int Nights => ScheduledDepartureDate.DayNumber - ScheduledArrivalDate.DayNumber;
    public static HotelStayPeriod Create(DateOnly arrivalDate, DateOnly departureDate)
    {
        if (departureDate <= arrivalDate) throw new ArgumentException("STAY_INVALID_DATE_RANGE");
        return new(arrivalDate, departureDate);
    }
}

/// <summary>Guest count with capacity validation.</summary>
public readonly record struct HotelStayGuestCount(int Adults, int Children)
{
    public static HotelStayGuestCount Create(int adults, int children)
    {
        if (adults < 1 || children < 0) throw new ArgumentException("STAY_INVALID_GUEST_COUNT");
        return new(adults, children);
    }
}

public enum RoomMoveType { SameType, Upgrade, Downgrade, Maintenance, OutOfOrder, Emergency, Vip }
public enum DepartureType { Normal, Early, Late }

/// <summary>Room move record — tracks every room change during a stay.</summary>
public sealed record RoomMoveRecord(
    Guid FromRoomId, string FromRoomNumber, Guid FromRoomTypeId,
    Guid ToRoomId, string ToRoomNumber, Guid ToRoomTypeId,
    RoomMoveType MoveType, string Reason, DateTime MovedAtUtc, Guid? MovedBy);

/// <summary>Key card record.</summary>
public sealed record KeyCardRecord(int CardNumber, DateTime IssuedAtUtc, DateTime? ReturnedAtUtc, Guid? IssuedBy);

/// <summary>Wake-up call request during stay.</summary>
public sealed record WakeUpCallRecord(int CallNumber, DateTime RequestedAtUtc, string? Notes, DateTime CreatedAtUtc, Guid? CreatedBy);

/// <summary>Guest complaint logged during stay.</summary>
public sealed record ComplaintRecord(int ComplaintId, string Category, string Description, DateTime LoggedAtUtc, Guid? LoggedBy);

/// <summary>Timeline entry for every stay event — unified audit log.</summary>
public sealed record StayTimelineEntry(
    Guid StayTimelineEntryId,
    DateTime OccurredAtUtc,
    string EventName,
    HotelStayStatus? FromStatus,
    HotelStayStatus ToStatus,
    Guid? ActorId,
    Guid CorrelationId,
    string? SafeMetadata
)
{
    public static StayTimelineEntry Create(Guid stayId, Guid propertyId, DateTime occurredAtUtc, string eventName, HotelStayStatus? fromStatus, HotelStayStatus toStatus, Guid? actorId, Guid correlationId, string? metadata = null)
        => new(Uuid7.NewGuid(), occurredAtUtc, eventName, fromStatus, toStatus, actorId, correlationId, metadata);
}
