using ASMILE.BO.Core.FrontOffice.Foundation.DomainEvents;
using ASMILE.BO.Core.FrontOffice.Foundation.Identifiers;

namespace ASMILE.BO.Core.FrontOffice.Reservation;

public enum ReservationStatus { Draft, Tentative, Confirmed, Cancelled, NoShow }
public readonly record struct StayPeriod(DateOnly ArrivalDate, DateOnly DepartureDate)
{
    public int Nights => DepartureDate.DayNumber - ArrivalDate.DayNumber;
    public static StayPeriod Create(DateOnly arrivalDate, DateOnly departureDate)
    {
        if (departureDate <= arrivalDate) throw new ArgumentException("RESERVATION_INVALID_STAY");
        return new(arrivalDate, departureDate);
    }
}
public readonly record struct GuestCount(int Adults, int Children)
{
    public static GuestCount Create(int adults, int children)
    {
        if (adults < 1 || children < 0) throw new ArgumentException("RESERVATION_INVALID_GUEST_COUNT");
        return new(adults, children);
    }
}
public sealed class Reservation : IHasFrontOfficeDomainEvents
{
    private readonly List<IFrontOfficeDomainEvent> _events = [];
    private Reservation() { }
    public Guid ReservationId { get; private set; }
    public string ReservationNumber { get; private set; } = string.Empty;
    public Guid PropertyId { get; private set; }
    public Guid? GuestId { get; private set; }
    public string GuestNameSnapshot { get; private set; } = string.Empty;
    public string? GuestPhoneSnapshot { get; private set; }
    public string? GuestEmailSnapshot { get; private set; }
    public DateOnly ArrivalDate { get; private set; }
    public DateOnly DepartureDate { get; private set; }
    public int AdultCount { get; private set; }
    public int ChildCount { get; private set; }
    public Guid? RoomTypeId { get; private set; }
    public Guid? AssignedRoomId { get; private set; }
    public Guid? RatePlanId { get; private set; }
    public string Source { get; private set; } = string.Empty;
    public ReservationStatus Status { get; private set; }
    public string? SpecialRequests { get; private set; }
    public string? InternalNotes { get; private set; }
    public string? CancellationReason { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public Guid? CreatedBy { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }
    public Guid? UpdatedBy { get; private set; }
    public DateTime? ConfirmedAtUtc { get; private set; }
    public DateTime? CancelledAtUtc { get; private set; }
    public DateTime? NoShowAtUtc { get; private set; }
    public byte[]? RowVersion { get; private set; }
    public IReadOnlyCollection<IFrontOfficeDomainEvent> DomainEvents => _events;
    public bool IsTerminal => Status is ReservationStatus.Cancelled or ReservationStatus.NoShow;
    public static Reservation Create(string number, PropertyId propertyId, Guid? guestId, string guestName, StayPeriod stay, GuestCount guests, Guid? roomTypeId, string source, DateTime nowUtc, ActorId? actorId)
    {
        if (string.IsNullOrWhiteSpace(number) || string.IsNullOrWhiteSpace(guestName) || string.IsNullOrWhiteSpace(source)) throw new ArgumentException("RESERVATION_REQUIRED_FIELD");
        var reservation = new Reservation { ReservationId = Uuid7.NewGuid(), ReservationNumber = number, PropertyId = propertyId.Value, GuestId = guestId, GuestNameSnapshot = guestName.Trim(), ArrivalDate = stay.ArrivalDate, DepartureDate = stay.DepartureDate, AdultCount = guests.Adults, ChildCount = guests.Children, RoomTypeId = roomTypeId, Source = source.Trim(), Status = ReservationStatus.Draft, CreatedAtUtc = nowUtc, UpdatedAtUtc = nowUtc, CreatedBy = actorId?.Value, UpdatedBy = actorId?.Value };
        reservation.Add("FrontOffice.Reservation.Created", nowUtc); return reservation;
    }
    public void Update(string guestName, string? phone, string? email, Guid? roomTypeId, Guid? ratePlanId, string? specialRequests, string? internalNotes, DateTime nowUtc, ActorId? actor) { EnsureMutable(); if (string.IsNullOrWhiteSpace(guestName)) throw new ArgumentException("RESERVATION_REQUIRED_FIELD"); GuestNameSnapshot = guestName.Trim(); GuestPhoneSnapshot = phone; GuestEmailSnapshot = email; RoomTypeId = roomTypeId; RatePlanId = ratePlanId; SpecialRequests = specialRequests; InternalNotes = internalNotes; Touch(nowUtc, actor); }
    public void ChangeStay(StayPeriod stay, DateTime nowUtc, ActorId? actor) { EnsureMutable(); ArrivalDate = stay.ArrivalDate; DepartureDate = stay.DepartureDate; Touch(nowUtc, actor); Add("FrontOffice.Reservation.StayChanged", nowUtc); }
    public void ChangeGuests(GuestCount guests, DateTime nowUtc, ActorId? actor) { EnsureMutable(); AdultCount = guests.Adults; ChildCount = guests.Children; Touch(nowUtc, actor); Add("FrontOffice.Reservation.GuestCountChanged", nowUtc); }
    public void AssignRoom(Guid roomId, DateTime nowUtc, ActorId? actor) { EnsureMutable(); AssignedRoomId = roomId; Touch(nowUtc, actor); Add("FrontOffice.Reservation.RoomAssigned", nowUtc); }
    public void UnassignRoom(DateTime nowUtc, ActorId? actor) { EnsureMutable(); AssignedRoomId = null; Touch(nowUtc, actor); Add("FrontOffice.Reservation.RoomUnassigned", nowUtc); }
    public void MarkTentative(DateTime nowUtc, ActorId? actor) { Require(ReservationStatus.Draft); Status = ReservationStatus.Tentative; Touch(nowUtc, actor); Add("FrontOffice.Reservation.Tentative", nowUtc); }
    public void Confirm(DateTime nowUtc, ActorId? actor) { Require(ReservationStatus.Tentative); if (GuestId is null || string.IsNullOrWhiteSpace(GuestNameSnapshot)) throw new ArgumentException("RESERVATION_GUEST_REQUIRED"); if (RoomTypeId is null) throw new ArgumentException("RESERVATION_ROOM_TYPE_REQUIRED"); Status = ReservationStatus.Confirmed; ConfirmedAtUtc = nowUtc; Touch(nowUtc, actor); Add("FrontOffice.Reservation.Confirmed", nowUtc); }
    public void Cancel(string reason, DateTime nowUtc, ActorId? actor) { if (IsTerminal || Status is not (ReservationStatus.Draft or ReservationStatus.Tentative or ReservationStatus.Confirmed)) throw new InvalidOperationException("RESERVATION_INVALID_STATE"); if (string.IsNullOrWhiteSpace(reason)) throw new ArgumentException("RESERVATION_CANCEL_REASON_REQUIRED"); Status = ReservationStatus.Cancelled; CancellationReason = reason.Trim(); CancelledAtUtc = nowUtc; Touch(nowUtc, actor); Add("FrontOffice.Reservation.Cancelled", nowUtc); }
    public void MarkNoShow(DateTime nowUtc, ActorId? actor) { Require(ReservationStatus.Confirmed); Status = ReservationStatus.NoShow; NoShowAtUtc = nowUtc; Touch(nowUtc, actor); Add("FrontOffice.Reservation.NoShow", nowUtc); }
    public void ClearDomainEvents() => _events.Clear();
    private void EnsureMutable() { if (IsTerminal) throw new InvalidOperationException("RESERVATION_TERMINAL"); }
    private void Require(ReservationStatus status) { if (Status != status) throw new InvalidOperationException("RESERVATION_INVALID_STATE"); }
    private void Touch(DateTime nowUtc, ActorId? actor) { UpdatedAtUtc = nowUtc; UpdatedBy = actor?.Value; }
    private void Add(string name, DateTime nowUtc) => _events.Add(new ReservationEvent(EventId.New(), nowUtc, name, ReservationId, PropertyId, Status, ArrivalDate, DepartureDate));
}
public sealed record ReservationEvent(EventId EventId, DateTime OccurredAtUtc, string EventName, Guid ReservationId, Guid PropertyId, ReservationStatus Status, DateOnly ArrivalDate, DateOnly DepartureDate) : IFrontOfficeDomainEvent
{ public object Payload => new { ReservationId, PropertyId, Status, ArrivalDate, DepartureDate }; }
public sealed class ReservationHistory { private ReservationHistory() { } public Guid HistoryId { get; private set; } = Uuid7.NewGuid(); public Guid ReservationId { get; private set; } public string Action { get; private set; } = string.Empty; public ReservationStatus? FromStatus { get; private set; } public ReservationStatus ToStatus { get; private set; } public DateTime OccurredAtUtc { get; private set; } public Guid? ActorId { get; private set; } public Guid CorrelationId { get; private set; } public string? ReasonCode { get; private set; } public string? SafeSummary { get; private set; } public static ReservationHistory Record(Guid reservationId, string action, ReservationStatus? from, ReservationStatus to, DateTime at, Guid? actor, Guid correlation, string? reason = null) => new() { ReservationId = reservationId, Action = action, FromStatus = from, ToStatus = to, OccurredAtUtc = at, ActorId = actor, CorrelationId = correlation, ReasonCode = reason, SafeSummary = action }; }
