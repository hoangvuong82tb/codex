namespace ASMILE.BO.Application.FrontOffice.Concierge;

// =============================================================================
// SERVICE REQUEST
// =============================================================================

public enum ServiceType { WakeUpCall, AirportTransfer, Taxi, Limousine, Luggage, BellService, PackageHandling, MailMessage, SpecialRequest, VipService, RestaurantBooking, SpaBooking, TourBooking, TicketBooking, Other }
public enum ServiceStatus { Requested, Assigned, Accepted, InProgress, Completed, Cancelled, Escalated }
public enum ServicePriority { Normal, High, Urgent, Vip }
public enum SlaStatus { OnTrack, AtRisk, Breached }
public enum TransportType { AirportPickup, AirportDropoff, Taxi, Limousine, Shuttle, PrivateDriver }

public sealed record ServiceRequestDto(
    Guid RequestId, string RequestNumber, Guid? GuestId, string? GuestName,
    Guid? StayId, string? RoomNumber, ServiceType Type, ServiceStatus Status,
    ServicePriority Priority, string Description, Guid? AssignedTo,
    string? AssignedName, DateTime RequestedAtUtc, DateTime? CompletedAtUtc,
    DateTime? SlaDeadlineAtUtc, SlaStatus SlaStatus, bool IsVip,
    string? Category = null, decimal? EstimatedCost = null, decimal? ActualCost = null,
    string? ChargeType = null, string? AssignedDepartment = null
);

public sealed record CreateServiceRequestRequest(
    Guid? GuestId,
    Guid? StayId,
    Guid? RoomId,
    string? RoomNumber,
    string? GuestName,
    ServiceType Type,
    ServicePriority Priority,
    string Description,
    DateTime? SlaDeadlineAtUtc,
    decimal? EstimatedCost,
    string? ChargeType
);

public sealed record ServiceSlaDto(
    ServiceType Type, int TotalRequests, int Completed, int OnTime, int Breached,
    decimal OnTimePct, int AvgResponseMinutes, int AvgCompletionMinutes
);

// =============================================================================
// WAKE-UP CALL
// =============================================================================

public enum WakeUpCallStatus { Scheduled, InProgress, Completed, NoAnswer, Cancelled, Failed }

public sealed record WakeUpCallDto(
    Guid CallId, string CallNumber, Guid? StayId, Guid? RoomId, string? RoomNumber,
    string? GuestName, DateTime WakeTime, DateTime? ActualCallTime,
    WakeUpCallStatus Status, int RetryCount, bool IsRecurring,
    string? Notes, Guid? AssignedStaffId, DateTime CreatedAtUtc
);

public sealed record WakeUpDashboardDto(
    int ScheduledToday, int Completed, int NoAnswer, int Failed,
    int Pending, decimal CompletionRate
);

// =============================================================================
// LUGGAGE
// =============================================================================

public enum LuggageStatus { Stored, InTransit, Delivered, Collected, Lost }

public sealed record LuggageItemDto(
    Guid ItemId, string TagNumber, Guid? GuestId, string? GuestName,
    Guid? StayId, string? RoomNumber, int Pieces, string? Description,
    string? StorageLocation, LuggageStatus Status,
    DateTime StoredAtUtc, DateTime? DeliveredAtUtc, DateTime? CollectedAtUtc,
    Guid? HandledBy, string? HandlerName
);

// =============================================================================
// TRANSPORTATION
// =============================================================================

public sealed record TransportationDto(
    Guid BookingId, string BookingNumber, TransportType Type,
    Guid? GuestId, string? GuestName, Guid? StayId, string? RoomNumber,
    string? DriverName, string? VehicleNumber, string? VehicleType,
    DateTime ScheduledAtUtc, string? PickupLocation, string? DropoffLocation,
    string? FlightNumber, int PassengerCount, ServiceStatus Status,
    decimal? Cost, string? Notes
);

// =============================================================================
// DASHBOARDS
// =============================================================================

public sealed record ConciergeDashboardDto(
    int OpenRequests, int AssignedToday, int CompletedToday, int Escalated,
    int WakeupsScheduled, int LuggageStored, int TransportBookings,
    decimal AvgResponseMinutes, decimal SlaCompliancePct,
    IReadOnlyList<ServiceRequestDto> UrgentRequests,
    IReadOnlyList<ServiceRequestDto> BreachedSla
);

public sealed record ServicePerformanceDto(
    ServiceType Type, int Volume, decimal AvgRating, decimal CompletionRate,
    decimal AvgMinutes, decimal SlaCompliance
);

// =============================================================================
// CONTRACTS
// =============================================================================

public interface IServiceRequestService
{
    Task<ServiceRequestDto> CreateAsync(Guid? guestId, Guid? stayId, ServiceType type, ServicePriority priority, string description, DateTime? slaDeadline, CancellationToken ct);
    Task<ServiceRequestDto> CreateAsync(CreateServiceRequestRequest request, CancellationToken ct);
    Task<ServiceRequestDto> AssignAsync(Guid requestId, Guid staffId, CancellationToken ct);
    Task<ServiceRequestDto> TransitionAsync(Guid requestId, ServiceStatus to, string? note, CancellationToken ct);
    Task<IReadOnlyList<ServiceRequestDto>> GetOpenAsync(Guid propertyId, CancellationToken ct);
    Task<IReadOnlyList<ServiceRequestDto>> GetAllAsync(Guid propertyId, CancellationToken ct);
    Task<IReadOnlyList<ServiceSlaDto>> GetSlaReportAsync(Guid propertyId, CancellationToken ct);
}

public interface IWakeUpCallService
{
    Task<WakeUpCallDto> ScheduleAsync(Guid? stayId, Guid? roomId, DateTime wakeTime, string? notes, CancellationToken ct);
    Task<WakeUpCallDto> CompleteAsync(Guid callId, WakeUpCallStatus result, CancellationToken ct);
    Task<WakeUpCallDto> RetryAsync(Guid callId, CancellationToken ct);
    Task<WakeUpDashboardDto> GetDashboardAsync(Guid propertyId, CancellationToken ct);
    Task<IReadOnlyList<WakeUpCallDto>> GetScheduledAsync(Guid propertyId, DateTime forDate, CancellationToken ct);
}

public interface ILuggageService
{
    Task<LuggageItemDto> StoreAsync(LuggageItemDto item, CancellationToken ct);
    Task<LuggageItemDto> DeliverAsync(Guid itemId, Guid staffId, CancellationToken ct);
    Task<LuggageItemDto> CollectAsync(Guid itemId, CancellationToken ct);
    Task<IReadOnlyList<LuggageItemDto>> GetByGuestAsync(Guid guestId, CancellationToken ct);
    Task<IReadOnlyList<LuggageItemDto>> GetStoredAsync(Guid propertyId, CancellationToken ct);
}

public interface ITransportationService
{
    Task<TransportationDto> BookAsync(TransportationDto booking, CancellationToken ct);
    Task<TransportationDto> CompleteAsync(Guid bookingId, CancellationToken ct);
    Task<IReadOnlyList<TransportationDto>> GetUpcomingAsync(Guid propertyId, CancellationToken ct);
}

public interface IConciergeDashboardService
{
    Task<ConciergeDashboardDto> GetDashboardAsync(Guid propertyId, CancellationToken ct);
    Task<IReadOnlyList<ServicePerformanceDto>> GetPerformanceAsync(Guid propertyId, CancellationToken ct);
}

// =============================================================================
// TELEPHONE
// =============================================================================

public enum TelephoneCallDirection { Incoming, Outgoing }

public sealed record TelephoneCallDto(
    Guid CallId, TelephoneCallDirection Direction, string PhoneNumber,
    string? GuestName, string? RoomNumber, string? Subject, string? Notes,
    DateTime StartedAtUtc, int DurationSeconds, string Status, DateTime CreatedAtUtc);

public sealed record TelephoneContactDto(
    Guid ContactId, string DisplayName, string PhoneNumber, string? RoomNumber,
    string Category, string? Notes, bool IsActive, DateTime UpdatedAtUtc);

public interface ITelephoneService
{
    Task<IReadOnlyList<TelephoneCallDto>> GetCallsAsync(Guid propertyId, TelephoneCallDirection? direction, string? keyword, CancellationToken ct);
    Task<TelephoneCallDto> LogCallAsync(TelephoneCallDirection direction, string phoneNumber, string? guestName, string? roomNumber, string? subject, string? notes, DateTime startedAtUtc, int durationSeconds, CancellationToken ct);
    Task<IReadOnlyList<TelephoneContactDto>> GetContactsAsync(Guid propertyId, string? keyword, CancellationToken ct);
    Task<TelephoneContactDto> SaveContactAsync(Guid? contactId, string displayName, string phoneNumber, string? roomNumber, string? category, string? notes, CancellationToken ct);
    Task DeactivateContactAsync(Guid contactId, CancellationToken ct);
}

// =============================================================================
// EVENTS
// =============================================================================

public static class ConciergeEvents
{
    public const string ServiceRequested = "Concierge.ServiceRequested";
    public const string ServiceAssigned = "Concierge.ServiceAssigned";
    public const string ServiceCompleted = "Concierge.ServiceCompleted";
    public const string ServiceEscalated = "Concierge.ServiceEscalated";
    public const string WakeupScheduled = "Concierge.WakeupScheduled";
    public const string WakeupCompleted = "Concierge.WakeupCompleted";
    public const string WakeupNoAnswer = "Concierge.WakeupNoAnswer";
    public const string LuggageStored = "Concierge.LuggageStored";
    public const string LuggageDelivered = "Concierge.LuggageDelivered";
    public const string TransportationBooked = "Concierge.TransportationBooked";
    public const string SlaBreached = "Concierge.SlaBreached";
}
