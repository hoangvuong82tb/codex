using ASMILE.BO.Core.FrontOffice.Foundation.Identifiers;

namespace ASMILE.BO.Core.FrontOffice.Concierge;

/// <summary>
/// Persisted concierge records. Application contracts intentionally keep their
/// enums separate; these records store status/type as strings so the database
/// remains compatible when the application contract evolves.
/// </summary>
public sealed class ConciergeWakeUpCall
{
    private ConciergeWakeUpCall() { }

    public Guid CallId { get; private set; }
    public Guid PropertyId { get; private set; }
    public string CallNumber { get; private set; } = string.Empty;
    public Guid? StayId { get; private set; }
    public Guid? RoomId { get; private set; }
    public string? RoomNumber { get; private set; }
    public string? GuestName { get; private set; }
    public DateTime WakeTime { get; private set; }
    public DateTime? ActualCallTime { get; private set; }
    public string Status { get; private set; } = "Scheduled";
    public int RetryCount { get; private set; }
    public bool IsRecurring { get; private set; }
    public string? Notes { get; private set; }
    public Guid? AssignedStaffId { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public byte[]? RowVersion { get; private set; }

    public static ConciergeWakeUpCall Create(
        Guid propertyId,
        string callNumber,
        Guid? stayId,
        Guid? roomId,
        DateTime wakeTime,
        string? notes,
        DateTime nowUtc)
    {
        if (propertyId == Guid.Empty) throw new ArgumentException("Property is required.", nameof(propertyId));
        if (stayId is null && roomId is null) throw new ArgumentException("Stay or room is required.");
        if (string.IsNullOrWhiteSpace(callNumber)) throw new ArgumentException("Call number is required.", nameof(callNumber));

        return new ConciergeWakeUpCall
        {
            CallId = Uuid7.NewGuid(),
            PropertyId = propertyId,
            CallNumber = callNumber,
            StayId = stayId,
            RoomId = roomId,
            WakeTime = wakeTime,
            Notes = notes,
            CreatedAtUtc = nowUtc,
            Status = "Scheduled"
        };
    }

    public void Complete(string status, DateTime nowUtc)
    {
        if (status is not ("Completed" or "NoAnswer" or "Failed" or "Cancelled"))
            throw new InvalidOperationException("WAKEUP_RESULT_NOT_SUPPORTED");
        if (Status is not ("Scheduled" or "InProgress"))
            throw new InvalidOperationException("WAKEUP_NOT_PENDING");

        Status = status;
        ActualCallTime = nowUtc;
    }

    public void Retry()
    {
        if (Status is not ("NoAnswer" or "Failed"))
            throw new InvalidOperationException("WAKEUP_RETRY_NOT_ALLOWED");
        RetryCount++;
        ActualCallTime = null;
        Status = "Scheduled";
    }
}

public sealed class ConciergeLuggageItem
{
    private ConciergeLuggageItem() { }

    public Guid ItemId { get; private set; }
    public Guid PropertyId { get; private set; }
    public string TagNumber { get; private set; } = string.Empty;
    public Guid? GuestId { get; private set; }
    public string? GuestName { get; private set; }
    public Guid? StayId { get; private set; }
    public string? RoomNumber { get; private set; }
    public int Pieces { get; private set; }
    public string? Description { get; private set; }
    public string? StorageLocation { get; private set; }
    public string Status { get; private set; } = "Stored";
    public DateTime StoredAtUtc { get; private set; }
    public DateTime? DeliveredAtUtc { get; private set; }
    public DateTime? CollectedAtUtc { get; private set; }
    public Guid? HandledBy { get; private set; }
    public string? HandlerName { get; private set; }
    public byte[]? RowVersion { get; private set; }

    public static ConciergeLuggageItem Create(
        Guid propertyId,
        string tagNumber,
        Guid? guestId,
        string? guestName,
        Guid? stayId,
        string? roomNumber,
        int pieces,
        string? description,
        string? storageLocation,
        DateTime storedAtUtc)
    {
        if (propertyId == Guid.Empty) throw new ArgumentException("Property is required.", nameof(propertyId));
        if (pieces <= 0) throw new ArgumentOutOfRangeException(nameof(pieces));
        if (string.IsNullOrWhiteSpace(tagNumber)) throw new ArgumentException("Tag number is required.", nameof(tagNumber));

        return new ConciergeLuggageItem
        {
            ItemId = Uuid7.NewGuid(),
            PropertyId = propertyId,
            TagNumber = tagNumber,
            GuestId = guestId,
            GuestName = guestName,
            StayId = stayId,
            RoomNumber = roomNumber,
            Pieces = pieces,
            Description = description,
            StorageLocation = storageLocation,
            StoredAtUtc = storedAtUtc,
            Status = "Stored"
        };
    }

    public void Deliver(Guid staffId, DateTime nowUtc, string? handlerName)
    {
        if (staffId == Guid.Empty) throw new ArgumentException("Staff is required.", nameof(staffId));
        if (Status is not ("Stored" or "InTransit")) throw new InvalidOperationException("LUGGAGE_NOT_DELIVERABLE");
        Status = "Delivered";
        DeliveredAtUtc = nowUtc;
        HandledBy = staffId;
        HandlerName = handlerName;
    }

    public void Collect(DateTime nowUtc)
    {
        if (Status is not ("Delivered" or "Stored")) throw new InvalidOperationException("LUGGAGE_NOT_COLLECTABLE");
        Status = "Collected";
        CollectedAtUtc = nowUtc;
    }
}

public sealed class ConciergeTransportationBooking
{
    private ConciergeTransportationBooking() { }

    public Guid BookingId { get; private set; }
    public Guid PropertyId { get; private set; }
    public string BookingNumber { get; private set; } = string.Empty;
    public string Type { get; private set; } = string.Empty;
    public Guid? GuestId { get; private set; }
    public string? GuestName { get; private set; }
    public Guid? StayId { get; private set; }
    public string? RoomNumber { get; private set; }
    public string? DriverName { get; private set; }
    public string? VehicleNumber { get; private set; }
    public string? VehicleType { get; private set; }
    public DateTime ScheduledAtUtc { get; private set; }
    public string? PickupLocation { get; private set; }
    public string? DropoffLocation { get; private set; }
    public string? FlightNumber { get; private set; }
    public int PassengerCount { get; private set; }
    public string Status { get; private set; } = "Requested";
    public decimal? Cost { get; private set; }
    public string? Notes { get; private set; }
    public byte[]? RowVersion { get; private set; }

    public static ConciergeTransportationBooking Create(
        Guid propertyId,
        string bookingNumber,
        string type,
        Guid? guestId,
        string? guestName,
        Guid? stayId,
        string? roomNumber,
        string? driverName,
        string? vehicleNumber,
        string? vehicleType,
        DateTime scheduledAtUtc,
        string? pickupLocation,
        string? dropoffLocation,
        string? flightNumber,
        int passengerCount,
        decimal? cost,
        string? notes)
    {
        if (propertyId == Guid.Empty) throw new ArgumentException("Property is required.", nameof(propertyId));
        if (string.IsNullOrWhiteSpace(bookingNumber)) throw new ArgumentException("Booking number is required.", nameof(bookingNumber));
        if (string.IsNullOrWhiteSpace(type)) throw new ArgumentException("Transport type is required.", nameof(type));
        if (passengerCount <= 0) throw new ArgumentOutOfRangeException(nameof(passengerCount));

        return new ConciergeTransportationBooking
        {
            BookingId = Uuid7.NewGuid(),
            PropertyId = propertyId,
            BookingNumber = bookingNumber,
            Type = type,
            GuestId = guestId,
            GuestName = guestName,
            StayId = stayId,
            RoomNumber = roomNumber,
            DriverName = driverName,
            VehicleNumber = vehicleNumber,
            VehicleType = vehicleType,
            ScheduledAtUtc = scheduledAtUtc,
            PickupLocation = pickupLocation,
            DropoffLocation = dropoffLocation,
            FlightNumber = flightNumber,
            PassengerCount = passengerCount,
            Cost = cost,
            Notes = notes,
            Status = "Requested"
        };
    }

    public void Complete()
    {
        if (Status is "Completed" or "Cancelled") throw new InvalidOperationException("TRANSPORT_NOT_COMPLETABLE");
        Status = "Completed";
    }
}
