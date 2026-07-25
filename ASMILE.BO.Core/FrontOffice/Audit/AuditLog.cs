using ASMILE.BO.Core.FrontOffice.Foundation.Identifiers;

namespace ASMILE.BO.Core.FrontOffice.Audit;

public enum AuditAction { Create, Update, Delete, Approve, Cancel, CheckIn, CheckOut, Payment, Refund, Assign, Complete, Custom }

public sealed class AuditLog
{
    public Guid AuditLogId { get; private set; }
    public Guid PropertyId { get; private set; }
    public string AuditNumber { get; private set; } = string.Empty;
    public string Module { get; private set; } = string.Empty;
    public string EntityType { get; private set; } = string.Empty;
    public Guid EntityId { get; private set; }
    public AuditAction Action { get; private set; }
    public string? OldValue { get; private set; }
    public string? NewValue { get; private set; }
    public string? PerformedBy { get; private set; }
    public string? Department { get; private set; }
    public DateTime OccurredAtUtc { get; private set; }
    public DateOnly BusinessDate { get; private set; }
    public string? IpAddress { get; private set; }
    public string? SessionId { get; private set; }
    public Guid? CorrelationId { get; private set; }

    public static AuditLog Record(Guid propertyId, string auditNumber, string module, string entityType, Guid entityId, AuditAction action, string? oldValue, string? newValue, string? performedBy, string? department, DateTime nowUtc, DateOnly businessDate, string? ip, string? sessionId, Guid? correlationId) => new()
    { AuditLogId = Uuid7.NewGuid(), PropertyId = propertyId, AuditNumber = auditNumber, Module = module, EntityType = entityType, EntityId = entityId, Action = action, OldValue = oldValue, NewValue = newValue, PerformedBy = performedBy, Department = department, OccurredAtUtc = nowUtc, BusinessDate = businessDate, IpAddress = ip, SessionId = sessionId, CorrelationId = correlationId };
}

public sealed class ActivityTimeline
{
    public Guid ActivityTimelineId { get; private set; }
    public Guid PropertyId { get; private set; }
    public string TimelineType { get; private set; } = string.Empty;
    public string Title { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public Guid AuditLogId { get; private set; }
    public Guid? RelatedGuestId { get; private set; }
    public Guid? RelatedStayId { get; private set; }
    public Guid? RelatedReservationId { get; private set; }
    public string Severity { get; private set; } = "Info";
    public DateTime OccurredAtUtc { get; private set; }

    public static ActivityTimeline Create(Guid propertyId, string type, string title, string description, Guid auditLogId, Guid? guestId, Guid? stayId, Guid? reservationId, string severity, DateTime nowUtc) => new()
    { ActivityTimelineId = Uuid7.NewGuid(), PropertyId = propertyId, TimelineType = type, Title = title, Description = description, AuditLogId = auditLogId, RelatedGuestId = guestId, RelatedStayId = stayId, RelatedReservationId = reservationId, Severity = severity, OccurredAtUtc = nowUtc };
}
