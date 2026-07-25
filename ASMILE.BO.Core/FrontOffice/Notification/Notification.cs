using ASMILE.BO.Core.FrontOffice.Foundation.Identifiers;

namespace ASMILE.BO.Core.FrontOffice.Notification;

public enum NotificationStatus { New, Unread, Read, Acknowledged, Dismissed, Expired }
public enum NotificationSeverity { Info, Success, Warning, Critical }
public enum NotificationType { Reservation, CheckIn, CheckOut, Payment, Housekeeping, ServiceRequest, WakeUpCall, Concierge, LostFound, GuestFeedback, Task, VIP, System, Custom }

public sealed class Notification
{
    private readonly List<NotificationHistory> _history = [];
    private Notification() { }

    public Guid NotificationId { get; private set; }
    public Guid PropertyId { get; private set; }
    public string NotificationNumber { get; private set; } = string.Empty;
    public NotificationType Type { get; private set; }
    public NotificationSeverity Severity { get; private set; }
    public string Priority { get; private set; } = "Normal";
    public string? SourceModule { get; private set; }
    public Guid? RelatedEntityId { get; private set; }
    public string? RecipientDepartment { get; private set; }
    public string? RecipientStaff { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Message { get; private set; } = string.Empty;
    public NotificationStatus Status { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? ReadAtUtc { get; private set; }
    public DateTime? AcknowledgedAtUtc { get; private set; }
    public DateTime? DismissedAtUtc { get; private set; }
    public DateTime? ExpiredAtUtc { get; private set; }
    public string? ActionUrl { get; private set; }
    public bool RequiresAcknowledgement { get; private set; }
    public string? AcknowledgedBy { get; private set; }
    public string? Notes { get; private set; }
    public Guid? CreatedBy { get; private set; }
    public byte[]? RowVersion { get; private set; }
    public IReadOnlyList<NotificationHistory> History => _history;

    public static Notification Create(Guid propertyId, string number, NotificationType type, NotificationSeverity severity, string title, string message, string? sourceModule, string? recipientDept, bool requiresAck, string? actionUrl, DateTime nowUtc, ActorId? actorId) => new()
    { NotificationId = Uuid7.NewGuid(), PropertyId = propertyId, NotificationNumber = number, Type = type, Severity = severity, Title = title, Message = message, SourceModule = sourceModule, RecipientDepartment = recipientDept, RequiresAcknowledgement = requiresAck, ActionUrl = actionUrl, Status = NotificationStatus.New, CreatedAtUtc = nowUtc, CreatedBy = actorId?.Value };

    public void MarkRead(DateTime nowUtc) { Status = NotificationStatus.Read; ReadAtUtc = nowUtc; _history.Add(NotificationHistory.Record(NotificationId, PropertyId, "Read", nowUtc)); }
    public void Acknowledge(string by, DateTime nowUtc) { Status = NotificationStatus.Acknowledged; AcknowledgedBy = by; AcknowledgedAtUtc = nowUtc; _history.Add(NotificationHistory.Record(NotificationId, PropertyId, $"Acknowledged by {by}", nowUtc)); }
    public void Dismiss(DateTime nowUtc) { Status = NotificationStatus.Dismissed; DismissedAtUtc = nowUtc; _history.Add(NotificationHistory.Record(NotificationId, PropertyId, "Dismissed", nowUtc)); }
}

public sealed class NotificationHistory
{
    private NotificationHistory() { }
    public Guid NotificationHistoryId { get; private set; } public Guid NotificationId { get; private set; } public Guid PropertyId { get; private set; }
    public string Action { get; private set; } = string.Empty; public DateTime OccurredAtUtc { get; private set; }
    public static NotificationHistory Record(Guid nId, Guid propertyId, string action, DateTime nowUtc) => new() { NotificationHistoryId = Uuid7.NewGuid(), NotificationId = nId, PropertyId = propertyId, Action = action, OccurredAtUtc = nowUtc };
}
