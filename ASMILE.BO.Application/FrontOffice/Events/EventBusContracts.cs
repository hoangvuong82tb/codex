using ASMILE.BO.Core.FrontOffice.Foundation.Identifiers;

namespace ASMILE.BO.Application.FrontOffice.Events;

/// <summary>Strongly-typed domain event names — single catalog for all FO modules.</summary>
public static class FrontOfficeEventNames
{
    // Reservation
    public const string ReservationCreated = "FrontOffice.Reservation.Created";
    public const string ReservationConfirmed = "FrontOffice.Reservation.Confirmed";
    public const string ReservationCancelled = "FrontOffice.Reservation.Cancelled";
    public const string ReservationRoomAssigned = "FrontOffice.Reservation.RoomAssigned";
    public const string ReservationGuaranteed = "FrontOffice.Reservation.Guaranteed";
    public const string ReservationCheckedIn = "FrontOffice.Reservation.CheckedIn";
    public const string ReservationNoShow = "FrontOffice.Reservation.NoShow";
    public const string ReservationCompleted = "FrontOffice.Reservation.Completed";

    // Stay
    public const string StayPrepared = "StayPrepared";
    public const string StayCheckedIn = "StayCheckedIn";
    public const string StayCheckedOut = "StayCheckedOut";
    public const string StayExtended = "StayExtended";
    public const string StayRoomMoved = "StayRoomMoved";
    public const string StayEarlyDeparture = "StayEarlyDeparture";
    public const string StayCompleted = "StayCompleted";
    public const string StayReopened = "StayReopened";
    public const string StayPreArrivalConfirmed = "StayPreArrivalConfirmed";

    // Folio
    public const string FolioCreated = "FrontOffice.Folio.Created";
    public const string FolioClosed = "FrontOffice.Folio.Closed";
    public const string FolioSettled = "FrontOffice.Folio.Settled";
    public const string FolioReopened = "FrontOffice.Folio.Reopened";

    // Payment
    public const string PaymentReceived = "FrontOffice.Payment.Received";
    public const string PaymentAuthorized = "FrontOffice.Payment.Authorized";
    public const string PaymentCaptured = "FrontOffice.Payment.Captured";
    public const string PaymentCompleted = "FrontOffice.Payment.Completed";
    public const string PaymentRefunded = "FrontOffice.Payment.Refunded";
    public const string PaymentVoided = "FrontOffice.Payment.Voided";

    // Night Audit
    public const string NightAuditStarted = "FrontOffice.NightAudit.Started";
    public const string NightAuditCompleted = "FrontOffice.NightAudit.Completed";
    public const string NightAuditFailed = "FrontOffice.NightAudit.Failed";
    public const string BusinessDateRolled = "FrontOffice.BusinessDate.Rolled";

    // Posting
    public const string RoomChargePosted = "FrontOffice.Posting.RoomChargePosted";
    public const string TaxPosted = "FrontOffice.Posting.TaxPosted";
    public const string ServiceChargePosted = "FrontOffice.Posting.ServiceChargePosted";

    // Reporting
    public const string RevenueSnapshotGenerated = "FrontOffice.Reporting.RevenueSnapshot";
    public const string OccupancyUpdated = "FrontOffice.Reporting.OccupancyUpdated";

    // Guest
    public const string GuestCheckedIn = "FrontOffice.Guest.CheckedIn";
    public const string GuestCheckedOut = "FrontOffice.Guest.CheckedOut";
    public const string GuestProfileUpdated = "FrontOffice.Guest.ProfileUpdated";
    public const string VipTierChanged = "FrontOffice.Guest.VipTierChanged";
}

/// <summary>Integration event DTO — standardized payload for cross-module communication.</summary>
public sealed record IntegrationEvent(
    string EventName, string AggregateType, string AggregateId, long AggregateVersion,
    Guid PropertyId, DateOnly? BusinessDate, Guid CorrelationId, Guid? CausationId,
    DateTime OccurredAtUtc, string PayloadJson, string PartitionKey
);

/// <summary>Event handler — react to integration events.</summary>
public interface IEventHandler
{
    Task HandleAsync(IntegrationEvent @event, CancellationToken ct);
    string EventName { get; }
}

/// <summary>Event Bus — pub/sub with retry, dead letter, and correlation.</summary>
public interface IEventBus
{
    Task PublishAsync(IntegrationEvent @event, CancellationToken ct);
    Task SubscribeAsync(IEventHandler handler, CancellationToken ct);
    Task PublishFromAggregateAsync<T>(T aggregate, string aggregateType, Guid correlationId, CancellationToken ct) where T : IHasDomainEvents;
    Task<int> RetryFailedAsync(string eventName, int maxRetry, CancellationToken ct);
    Task<int> MoveToDeadLetterAsync(string eventName, int olderThanHours, CancellationToken ct);
}

/// <summary>Convenience interface for aggregates that raise events.</summary>
public interface IHasDomainEvents
{
    IReadOnlyCollection<object> DomainEvents { get; }
    void ClearDomainEvents();
    Guid GetAggregateId();
    long GetVersion();
}

/// <summary>Event handler registry — maps event names to handlers.</summary>
public sealed record EventSubscription(string EventName, Type HandlerType, string ConsumerName, bool IsActive = true);

/// <summary>Dead letter record for failed events.</summary>
public sealed record DeadLetterEvent(
    Guid EventId, string EventName, string AggregateType, string AggregateId,
    string PayloadJson, int RetryCount, string LastError, DateTime FailedAtUtc,
    DateTime? NextRetryAtUtc
);
