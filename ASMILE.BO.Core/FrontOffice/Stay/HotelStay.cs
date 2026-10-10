using ASMILE.BO.Core.FrontOffice.Foundation.DomainEvents;
using ASMILE.BO.Core.FrontOffice.Foundation.Identifiers;

namespace ASMILE.BO.Core.FrontOffice.Stay;

public sealed class HotelStay : IHasFrontOfficeDomainEvents
{
    private readonly List<IFrontOfficeDomainEvent> _events = [];
    private readonly List<RoomMoveRecord> _roomMoves = [];
    private readonly List<KeyCardRecord> _keyCards = [];
    private readonly List<WakeUpCallRecord> _wakeUpCalls = [];
    private readonly List<ComplaintRecord> _complaints = [];
    private readonly List<string> _tags = [];
    private HotelStay() { }

    // === Identity ===
    public Guid HotelStayId { get; private set; }
    public Guid PropertyId { get; private set; }
    public string StayNumber { get; private set; } = string.Empty;
    public Guid? ReservationId { get; private set; }
    public string? ReservationNumberSnapshot { get; private set; }
    public Guid? GuestId { get; private set; }
    public string GuestNameSnapshot { get; private set; } = string.Empty;
    public string? GuestPhoneSnapshot { get; private set; }
    public Guid RoomId { get; private set; }
    public string RoomNumberSnapshot { get; private set; } = string.Empty;
    public Guid RoomTypeId { get; private set; }
    public string RoomTypeCodeSnapshot { get; private set; } = string.Empty;

    // === Dates & Times ===
    public DateOnly ScheduledArrivalDate { get; private set; }
    public DateOnly ScheduledDepartureDate { get; private set; }
    public DateTime? ActualCheckInAtUtc { get; private set; }
    public DateTime? ActualCheckOutAtUtc { get; private set; }
    public DateTime? PreArrivalConfirmedAtUtc { get; private set; }
    public DateTime? LateCheckoutRequestedAtUtc { get; private set; }
    public DateTime? LateCheckoutUntilUtc { get; private set; }

    // === Guest Details ===
    public int Adults { get; private set; }
    public int Children { get; private set; }

    // === Pre Check-In Tracking ===
    public bool IsIdentityVerified { get; private set; }
    public bool AreSpecialRequestsConfirmed { get; private set; }
    public bool AreRoomPreferencesConfirmed { get; private set; }
    public DateTime? Eta { get; private set; }
    public DateTime? PreCheckInCompletedAtUtc { get; private set; }

    // === Status ===
    public HotelStayStatus Status { get; private set; }
    public string Source { get; private set; } = string.Empty;
    public string? InternalNotes { get; private set; }

    // === In-House Management ===
    public bool IsDoNotDisturb { get; private set; }
    public bool IsHouseUse { get; private set; }
    public bool IsVipService { get; private set; }
    public string? SpecialRequests { get; private set; }

    // === Departure Tracking ===
    public string? DepartureReason { get; private set; }
    public string? DepartureNotes { get; private set; }
    public bool HasPendingCharges { get; private set; }
    public bool HasOutstandingBalance { get; private set; }
    public bool IsDepartureApproved { get; private set; }

    // === Extension Tracking ===
    public int ExtensionCount { get; private set; }
    public DateOnly? OriginalDepartureDate { get; private set; }

    // === Audit ===
    public DateTime CreatedAtUtc { get; private set; }
    public Guid? CreatedBy { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }
    public Guid? UpdatedBy { get; private set; }
    public byte[]? RowVersion { get; private set; }

    // === Collections ===
    public IReadOnlyCollection<IFrontOfficeDomainEvent> DomainEvents => _events;
    public IReadOnlyList<RoomMoveRecord> RoomMoves => _roomMoves;
    public IReadOnlyList<KeyCardRecord> KeyCards => _keyCards;
    public IReadOnlyList<WakeUpCallRecord> WakeUpCalls => _wakeUpCalls;
    public IReadOnlyList<ComplaintRecord> Complaints => _complaints;
    public IReadOnlyList<string> Tags => _tags;

    // =============================================================================
    // FACTORY: Prepare stay from reservation
    // =============================================================================
    public static HotelStay Prepare(
        string stayNumber, PropertyId propertyId, Guid? reservationId, string? reservationNumber,
        Guid? guestId, string guestName, string? guestPhone,
        Guid roomId, string roomNumber, Guid roomTypeId, string roomTypeCode,
        HotelStayPeriod period, HotelStayGuestCount guests, string source, DateTime nowUtc, ActorId? actorId)
    {
        if (string.IsNullOrWhiteSpace(stayNumber) || string.IsNullOrWhiteSpace(guestName) || string.IsNullOrWhiteSpace(roomNumber) || string.IsNullOrWhiteSpace(roomTypeCode) || string.IsNullOrWhiteSpace(source))
            throw new ArgumentException("STAY_REQUIRED_FIELD");
        if (roomId == Guid.Empty) throw new ArgumentException("STAY_ROOM_REQUIRED");
        if (roomTypeId == Guid.Empty) throw new ArgumentException("STAY_ROOM_TYPE_REQUIRED");

        var stay = new HotelStay
        {
            HotelStayId = Uuid7.NewGuid(), PropertyId = propertyId.Value, StayNumber = stayNumber.Trim(),
            ReservationId = reservationId, ReservationNumberSnapshot = Normalize(reservationNumber),
            GuestId = guestId, GuestNameSnapshot = guestName.Trim(), GuestPhoneSnapshot = Normalize(guestPhone),
            RoomId = roomId, RoomNumberSnapshot = roomNumber.Trim(), RoomTypeId = roomTypeId, RoomTypeCodeSnapshot = roomTypeCode.Trim(),
            ScheduledArrivalDate = period.ScheduledArrivalDate, ScheduledDepartureDate = period.ScheduledDepartureDate,
            OriginalDepartureDate = period.ScheduledDepartureDate,
            Adults = guests.Adults, Children = guests.Children,
            Status = HotelStayStatus.PendingArrival, Source = source.Trim(),
            CreatedAtUtc = nowUtc, UpdatedAtUtc = nowUtc, CreatedBy = actorId?.Value, UpdatedBy = actorId?.Value
        };
        stay.AddEvent("StayPrepared", null, HotelStayStatus.PendingArrival, nowUtc);
        return stay;
    }

    // =============================================================================
    // STATE TRANSITIONS
    // =============================================================================

    /// <summary>Confirm pre-arrival within 24h of scheduled arrival.</summary>
    public void ConfirmPreArrival(DateTime nowUtc, ActorId? actorId)
    {
        Require(HotelStayStatus.PendingArrival);
        Status = HotelStayStatus.PreArrival;
        PreArrivalConfirmedAtUtc = nowUtc;
        Touch(nowUtc, actorId);
        AddEvent("StayPreArrivalConfirmed", HotelStayStatus.PendingArrival, HotelStayStatus.PreArrival, nowUtc);
    }

    /// <summary>Complete pre check-in: verify identity, confirm requests & preferences, record ETA.</summary>
    public void PreCheckIn(DateTime nowUtc, ActorId? actorId, DateTime? eta = null, bool identityVerified = true, bool specialRequestsConfirmed = true, bool roomPreferencesConfirmed = true)
    {
        Require(HotelStayStatus.PendingArrival, HotelStayStatus.PreArrival);
        if (!identityVerified) throw new ArgumentException("PRE_CHECKIN_IDENTITY_REQUIRED");
        IsIdentityVerified = true;
        AreSpecialRequestsConfirmed = specialRequestsConfirmed;
        AreRoomPreferencesConfirmed = roomPreferencesConfirmed;
        Eta = eta;
        PreCheckInCompletedAtUtc = nowUtc;
        if (Status == HotelStayStatus.PendingArrival)
        {
            Status = HotelStayStatus.PreArrival;
            PreArrivalConfirmedAtUtc = nowUtc;
        }
        Touch(nowUtc, actorId);
        AddEvent("StayPreCheckInCompleted", HotelStayStatus.PendingArrival, HotelStayStatus.PreArrival, nowUtc,
            $"IdentityVerified=True;SpecialRequests={specialRequestsConfirmed};RoomPrefs={roomPreferencesConfirmed};Eta={eta:O}");
    }

    /// <summary>Guest physically checked in.</summary>
    public void CheckIn(DateTime actualCheckInAtUtc, ActorId? actorId)
    {
        Require(HotelStayStatus.PendingArrival, HotelStayStatus.PreArrival);
        Status = HotelStayStatus.InHouse;
        ActualCheckInAtUtc = actualCheckInAtUtc;
        Touch(actualCheckInAtUtc, actorId);
        AddEvent("StayCheckedIn", HotelStayStatus.PendingArrival, HotelStayStatus.InHouse, actualCheckInAtUtc);
    }

    /// <summary>Move guest to a different room mid-stay.</summary>
    public void MoveRoom(Guid toRoomId, string toRoomNumber, Guid toRoomTypeId, string reason, DateTime nowUtc, ActorId? actorId, int toRoomTypeDisplayOrder = 0, int fromRoomTypeDisplayOrder = 0)
    {
        Require(HotelStayStatus.InHouse, HotelStayStatus.CheckedIn, HotelStayStatus.RoomMoved, HotelStayStatus.Extended);
        if (toRoomId == Guid.Empty) throw new ArgumentException("ROOM_MOVE_TARGET_REQUIRED");
        if (toRoomId == RoomId) throw new ArgumentException("ROOM_MOVE_SAME_ROOM");
        if (string.IsNullOrWhiteSpace(reason)) throw new ArgumentException("ROOM_MOVE_REASON_REQUIRED");
        var moveType = DetectMoveType(reason, toRoomTypeDisplayOrder, fromRoomTypeDisplayOrder);
        var fromStatus = Status;
        var record = new RoomMoveRecord(RoomId, RoomNumberSnapshot, RoomTypeId, toRoomId, toRoomNumber, toRoomTypeId, moveType, reason, nowUtc, actorId?.Value);
        _roomMoves.Add(record);
        RoomId = toRoomId; RoomNumberSnapshot = toRoomNumber; RoomTypeId = toRoomTypeId;
        Status = HotelStayStatus.RoomMoved;
        Touch(nowUtc, actorId);
        AddEvent("StayRoomMoved", fromStatus, HotelStayStatus.RoomMoved, nowUtc, $"From={record.FromRoomNumber};To={toRoomNumber};Type={moveType};Reason={reason}");
    }

    private static RoomMoveType DetectMoveType(string reason, int toDisplayOrder, int fromDisplayOrder)
    {
        var r = reason.ToLowerInvariant();
        if (r.Contains("maintenance") || r.Contains("bảo trì")) return RoomMoveType.Maintenance;
        if (r.Contains("emergency") || r.Contains("khẩn cấp")) return RoomMoveType.Emergency;
        if (r.Contains("vip") || r.Contains("đặc biệt")) return RoomMoveType.Vip;
        if (r.Contains("out of order") || r.Contains("ooo") || r.Contains("hỏng")) return RoomMoveType.OutOfOrder;
        if (toDisplayOrder > 0 && fromDisplayOrder > 0)
        {
            if (toDisplayOrder < fromDisplayOrder) return RoomMoveType.Upgrade;
            if (toDisplayOrder > fromDisplayOrder) return RoomMoveType.Downgrade;
        }
        return RoomMoveType.SameType;
    }

    /// <summary>Extend the departure date (tracked; supports multiple extensions).</summary>
    public void ExtendStay(DateOnly newDepartureDate, DateTime nowUtc, ActorId? actorId)
    {
        Require(HotelStayStatus.InHouse, HotelStayStatus.CheckedIn, HotelStayStatus.RoomMoved, HotelStayStatus.Extended);
        if (newDepartureDate <= ScheduledDepartureDate) throw new ArgumentException("STAY_EXTEND_INVALID_DATE");
        if (OriginalDepartureDate is null) OriginalDepartureDate = ScheduledDepartureDate;
        var fromStatus = Status;
        ScheduledDepartureDate = newDepartureDate;
        ExtensionCount++;
        Status = HotelStayStatus.Extended;
        Touch(nowUtc, actorId);
        AddEvent("StayExtended", fromStatus, HotelStayStatus.Extended, nowUtc, $"NewDeparture={newDepartureDate};ExtensionCount={ExtensionCount}");
    }

    /// <summary>Mark today as expected departure (called by Night Audit).</summary>
    public void MarkExpectedDeparture(DateTime nowUtc)
    {
        Require(HotelStayStatus.InHouse, HotelStayStatus.CheckedIn, HotelStayStatus.RoomMoved, HotelStayStatus.Extended);
        var fromStatus = Status;
        Status = HotelStayStatus.ExpectedDeparture;
        Touch(nowUtc, null);
        AddEvent("StayExpectedDeparture", fromStatus, HotelStayStatus.ExpectedDeparture, nowUtc);
    }

    /// <summary>Request late checkout.</summary>
    public void RequestLateCheckout(DateTime untilUtc, DateTime nowUtc, ActorId? actorId)
    {
        Require(HotelStayStatus.InHouse, HotelStayStatus.CheckedIn, HotelStayStatus.RoomMoved, HotelStayStatus.Extended);
        LateCheckoutRequestedAtUtc = nowUtc;
        LateCheckoutUntilUtc = untilUtc;
        AddEvent("StayLateCheckoutRequested", Status, Status, nowUtc, $"LateCheckoutUntil={untilUtc:O}");
    }

    /// <summary>Guest checked out with departure tracking.</summary>
    public void MarkCheckedOut(DateTime actualCheckOutAtUtc, ActorId? actorId, DepartureType departureType = DepartureType.Normal, string? reason = null, string? notes = null, bool hasPendingCharges = false, bool hasOutstandingBalance = false)
    {
        Require(HotelStayStatus.InHouse, HotelStayStatus.CheckedIn, HotelStayStatus.RoomMoved, HotelStayStatus.Extended, HotelStayStatus.ExpectedDeparture);
        var fromStatus = Status;
        Status = HotelStayStatus.CheckedOut;
        ActualCheckOutAtUtc = actualCheckOutAtUtc;
        DepartureReason = Normalize(reason);
        DepartureNotes = Normalize(notes);
        HasPendingCharges = hasPendingCharges;
        HasOutstandingBalance = hasOutstandingBalance;
        Touch(actualCheckOutAtUtc, actorId);
        AddEvent("StayCheckedOut", fromStatus, HotelStayStatus.CheckedOut, actualCheckOutAtUtc,
            $"DepartureType={departureType};Reason={reason};PendingCharges={hasPendingCharges};OutstandingBalance={hasOutstandingBalance}");
    }

    public void ApproveDeparture(DateTime nowUtc, ActorId? actorId)
    {
        Require(HotelStayStatus.InHouse, HotelStayStatus.CheckedIn, HotelStayStatus.RoomMoved, HotelStayStatus.Extended, HotelStayStatus.ExpectedDeparture);
        IsDepartureApproved = true;
        Touch(nowUtc, actorId);
        AddEvent("StayDepartureApproved", Status, Status, nowUtc);
    }

    /// <summary>Mark early departure (departing before scheduled date).</summary>
    public void MarkEarlyDeparture(DateTime actualCheckOutAtUtc, ActorId? actorId, string? reason = null)
    {
        Require(HotelStayStatus.InHouse, HotelStayStatus.CheckedIn, HotelStayStatus.RoomMoved, HotelStayStatus.Extended);
        Status = HotelStayStatus.CheckedOut;
        ActualCheckOutAtUtc = actualCheckOutAtUtc;
        DepartureReason = Normalize(reason);
        Touch(actualCheckOutAtUtc, actorId);
        AddEvent("StayEarlyDeparture", HotelStayStatus.InHouse, HotelStayStatus.CheckedOut, actualCheckOutAtUtc,
            $"EarlyDeparture;Scheduled={ScheduledDepartureDate};Actual={DateOnly.FromDateTime(actualCheckOutAtUtc)};Reason={reason}");
    }

    /// <summary>Complete post-checkout processing (folio closed, payment settled).</summary>
    public void Complete(DateTime nowUtc, ActorId? actorId)
    {
        Require(HotelStayStatus.CheckedOut);
        Status = HotelStayStatus.Completed;
        Touch(nowUtc, actorId);
        AddEvent("StayCompleted", HotelStayStatus.CheckedOut, HotelStayStatus.Completed, nowUtc);
    }

    /// <summary>Archive completed/cancelled stay data.</summary>
    public void Archive(DateTime nowUtc, ActorId? actorId)
    {
        Require(HotelStayStatus.Completed, HotelStayStatus.Cancelled);
        Status = HotelStayStatus.Archived;
        Touch(nowUtc, actorId);
        AddEvent("StayArchived", HotelStayStatus.Completed, HotelStayStatus.Archived, nowUtc);
    }

    /// <summary>Reopen a completed stay (e.g., billing correction).</summary>
    public void Reopen(DateTime nowUtc, ActorId? actorId)
    {
        Require(HotelStayStatus.Completed);
        Status = HotelStayStatus.CheckedOut;
        Touch(nowUtc, actorId);
        AddEvent("StayReopened", HotelStayStatus.Completed, HotelStayStatus.CheckedOut, nowUtc);
    }

    /// <summary>Cancel a stay before arrival.</summary>
    public void CancelPreparation(DateTime nowUtc, ActorId? actorId)
    {
        Require(HotelStayStatus.PendingArrival, HotelStayStatus.PreArrival);
        var fromStatus = Status;
        Status = HotelStayStatus.Cancelled;
        Touch(nowUtc, actorId);
        AddEvent("StayCancelled", fromStatus, HotelStayStatus.Cancelled, nowUtc);
    }

    // =============================================================================
    // KEY CARD MANAGEMENT
    // =============================================================================
    public KeyCardRecord IssueKeyCard(int cardNumber, Guid issuedBy, DateTime nowUtc)
    {
        var card = new KeyCardRecord(cardNumber, nowUtc, null, issuedBy);
        _keyCards.Add(card);
        AddEvent("StayKeyCardIssued", Status, Status, nowUtc, $"CardNumber={cardNumber}");
        return card;
    }

    public void ReturnKeyCard(int cardNumber, DateTime nowUtc)
    {
        var card = _keyCards.FirstOrDefault(k => k.CardNumber == cardNumber && k.ReturnedAtUtc == null)
            ?? throw new ArgumentException("STAY_KEYCARD_NOT_FOUND");
        _keyCards.Remove(card);
        _keyCards.Add(card with { ReturnedAtUtc = nowUtc });
        AddEvent("StayKeyCardReturned", Status, Status, nowUtc, $"CardNumber={cardNumber}");
    }

    // =============================================================================
    // GUEST MANAGEMENT
    // =============================================================================
    public void UpdateGuestInfo(string guestName, string? guestPhone, DateTime nowUtc, ActorId? actorId)
    {
        if (string.IsNullOrWhiteSpace(guestName)) throw new ArgumentException("STAY_GUEST_NAME_REQUIRED");
        GuestNameSnapshot = guestName.Trim();
        GuestPhoneSnapshot = Normalize(guestPhone);
        Touch(nowUtc, actorId);
    }

    public void ReconcileGuestCounts(int adults, int children, DateTime nowUtc, ActorId? actorId)
    {
        if (adults < 1 || children < 0) throw new ArgumentException("STAY_GUEST_COUNT_INVALID");
        Adults = Math.Max(Adults, adults);
        Children = Math.Max(Children, children);
        Touch(nowUtc, actorId);
        AddEvent("StayGuestCountsReconciled", Status, Status, nowUtc, $"Adults={Adults};Children={Children}");
    }

    public void ReconcileGuestCountsAfterOccupantRemoval(int adults, int children, DateTime nowUtc, ActorId? actorId)
    {
        if (adults < 1 || children < 0) throw new ArgumentException("STAY_GUEST_COUNT_INVALID");
        Adults = adults;
        Children = children;
        Touch(nowUtc, actorId);
        AddEvent("StayGuestCountsReconciled", Status, Status, nowUtc, $"Adults={Adults};Children={Children}");
    }

    public void UpdateNotes(string? notes, DateTime nowUtc, ActorId? actorId)
    {
        InternalNotes = Normalize(notes);
        Touch(nowUtc, actorId);
    }

    // =============================================================================
    // IN-HOUSE MANAGEMENT
    // =============================================================================
    public void ToggleDoNotDisturb(bool enabled, DateTime nowUtc, ActorId? actorId)
    {
        Require(HotelStayStatus.InHouse, HotelStayStatus.CheckedIn, HotelStayStatus.RoomMoved, HotelStayStatus.Extended);
        IsDoNotDisturb = enabled;
        Touch(nowUtc, actorId);
        AddEvent(enabled ? "StayDndEnabled" : "StayDndDisabled", Status, Status, nowUtc);
    }

    public void MarkHouseUse(bool enabled, DateTime nowUtc, ActorId? actorId)
    {
        IsHouseUse = enabled;
        Touch(nowUtc, actorId);
        AddEvent(enabled ? "StayHouseUseEnabled" : "StayHouseUseDisabled", Status, Status, nowUtc);
    }

    public void MarkVipService(bool enabled, DateTime nowUtc, ActorId? actorId)
    {
        IsVipService = enabled;
        Touch(nowUtc, actorId);
        AddEvent(enabled ? "StayVipServiceStarted" : "StayVipServiceStopped", Status, Status, nowUtc);
    }

    public void UpdateSpecialRequests(string? requests, DateTime nowUtc, ActorId? actorId)
    {
        SpecialRequests = Normalize(requests);
        Touch(nowUtc, actorId);
    }

    public WakeUpCallRecord RequestWakeUpCall(DateTime requestedAtUtc, string? notes, DateTime nowUtc, ActorId? actorId)
    {
        var call = new WakeUpCallRecord(_wakeUpCalls.Count + 1, requestedAtUtc, notes, nowUtc, actorId?.Value);
        _wakeUpCalls.Add(call);
        AddEvent("StayWakeUpCallRequested", Status, Status, nowUtc, $"WakeUpAt={requestedAtUtc:O}");
        return call;
    }

    public ComplaintRecord LogComplaint(string category, string description, DateTime nowUtc, ActorId? actorId)
    {
        if (string.IsNullOrWhiteSpace(category)) throw new ArgumentException("COMPLAINT_CATEGORY_REQUIRED");
        if (string.IsNullOrWhiteSpace(description)) throw new ArgumentException("COMPLAINT_DESCRIPTION_REQUIRED");
        var complaint = new ComplaintRecord(_complaints.Count + 1, category.Trim(), description.Trim(), nowUtc, actorId?.Value);
        _complaints.Add(complaint);
        AddEvent("StayComplaintLogged", Status, Status, nowUtc, $"Category={category};ComplaintId={complaint.ComplaintId}");
        return complaint;
    }

    public void AddTag(string tag, DateTime nowUtc, ActorId? actorId)
    {
        if (string.IsNullOrWhiteSpace(tag)) throw new ArgumentException("STAY_TAG_REQUIRED");
        if (_tags.Contains(tag)) return;
        _tags.Add(tag.Trim());
        Touch(nowUtc, actorId);
    }

    public void RemoveTag(string tag, DateTime nowUtc, ActorId? actorId)
    {
        _tags.Remove(tag);
        Touch(nowUtc, actorId);
    }

    // =============================================================================
    // COMPATIBILITY & HELPERS
    // =============================================================================
    public void MarkInHouse(DateTime actualCheckInAtUtc, ActorId? actorId) => CheckIn(actualCheckInAtUtc, actorId);

    public void ClearDomainEvents() => _events.Clear();

    private void Require(params HotelStayStatus[] allowed)
    {
        if (!allowed.Contains(Status))
            throw new InvalidOperationException($"STAY_INVALID_STATE: Cannot transition from {Status}.");
    }

    private void Touch(DateTime atUtc, ActorId? actorId) { UpdatedAtUtc = atUtc; UpdatedBy = actorId?.Value; }

    private void AddEvent(string eventName, HotelStayStatus? fromStatus, HotelStayStatus toStatus, DateTime occurredAtUtc, string? metadata = null)
        => _events.Add(new StayLifecycleEvent(EventId.New(), occurredAtUtc, eventName, HotelStayId, PropertyId, StayNumber, ReservationId, RoomId, fromStatus, toStatus, metadata));

    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

// =============================================================================
// DOMAIN EVENTS
// =============================================================================

/// <summary>Unified stay lifecycle event covering all transitions.</summary>
public sealed record StayLifecycleEvent(
    EventId EventId, DateTime OccurredAtUtc, string EventName,
    Guid HotelStayId, Guid PropertyId, string StayNumber, Guid? ReservationId, Guid RoomId,
    HotelStayStatus? FromStatus, HotelStayStatus ToStatus, string? Metadata
) : IFrontOfficeDomainEvent
{
    public object Payload => new { HotelStayId, PropertyId, StayNumber, ReservationId, RoomId, EventName, FromStatus, ToStatus, Metadata, OccurredAtUtc };
}

/// <summary>History record for persistence.</summary>
public sealed class HotelStayHistory
{
    private HotelStayHistory() { }
    public Guid HistoryId { get; private set; } = Uuid7.NewGuid();
    public Guid HotelStayId { get; private set; }
    public Guid PropertyId { get; private set; }
    public string Action { get; private set; } = string.Empty;
    public HotelStayStatus? FromStatus { get; private set; }
    public HotelStayStatus ToStatus { get; private set; }
    public Guid? ActorId { get; private set; }
    public DateTime OccurredAtUtc { get; private set; }
    public Guid CorrelationId { get; private set; }
    public string? SafeMetadata { get; private set; }

    public static HotelStayHistory Record(HotelStay stay, string action, DateTime occurredAtUtc, Guid? actorId, Guid correlationId, string? metadata = null)
        => new()
        {
            HotelStayId = stay.HotelStayId, PropertyId = stay.PropertyId, Action = action,
            FromStatus = GetFromStatus(stay, action), ToStatus = stay.Status,
            ActorId = actorId, OccurredAtUtc = occurredAtUtc, CorrelationId = correlationId, SafeMetadata = metadata
        };

    public static HotelStayHistory RecordPrepared(HotelStay stay, DateTime occurredAtUtc, Guid? actorId, Guid correlationId)
        => Record(stay, "StayPrepared", occurredAtUtc, actorId, correlationId, "PreparedFromReservation");

    public static HotelStayHistory RecordCheckedIn(HotelStay stay, DateTime occurredAtUtc, Guid? actorId, Guid correlationId)
        => Record(stay, "StayCheckedIn", occurredAtUtc, actorId, correlationId, $"RoomId={stay.RoomId:D}");

    public static HotelStayHistory RecordCheckedOut(HotelStay stay, DateTime occurredAtUtc, Guid? actorId, Guid correlationId)
        => Record(stay, "StayCheckedOut", occurredAtUtc, actorId, correlationId, $"RoomId={stay.RoomId:D}");

    private static HotelStayStatus? GetFromStatus(HotelStay stay, string action) => action switch
    {
        "StayPrepared" => null,
        "StayCheckedIn" => HotelStayStatus.PendingArrival,
        "StayCheckedOut" => HotelStayStatus.InHouse,
        "StayExtended" => HotelStayStatus.InHouse,
        "StayRoomMoved" => HotelStayStatus.InHouse,
        _ => null
    };
}
