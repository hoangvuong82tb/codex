using ASMILE.BO.Core.FrontOffice.Foundation.Identifiers;

namespace ASMILE.BO.Core.FrontOffice.Feedback;

public enum FeedbackStatus { New, Acknowledged, Assigned, InProgress, WaitingGuest, Resolved, Closed, Rejected }

public sealed class GuestFeedback
{
    private readonly List<GuestFeedbackHistory> _history = [];
    private GuestFeedback() { }

    public Guid GuestFeedbackId { get; private set; }
    public Guid PropertyId { get; private set; }
    public string FeedbackNumber { get; private set; } = string.Empty;
    public Guid? GuestId { get; private set; }
    public Guid? ReservationId { get; private set; }
    public Guid? HotelStayId { get; private set; }
    public Guid? HotelRoomId { get; private set; }
    public string? GuestName { get; private set; }
    public string? RoomNumber { get; private set; }
    public string FeedbackType { get; private set; } = string.Empty;
    public string Category { get; private set; } = string.Empty;
    public string Priority { get; private set; } = "Normal";
    public FeedbackStatus Status { get; private set; }
    public string? Source { get; private set; }
    public int Rating { get; private set; }
    public string? Subject { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public string? AssignedDepartment { get; private set; }
    public Guid? AssignedStaffId { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? AcknowledgedAtUtc { get; private set; }
    public DateTime? ResolvedAtUtc { get; private set; }
    public DateTime? ClosedAtUtc { get; private set; }
    public string? Resolution { get; private set; }
    public bool FollowUpRequired { get; private set; }
    public DateTime? FollowUpDate { get; private set; }
    public Guid? CreatedBy { get; private set; }
    public byte[]? RowVersion { get; private set; }
    public IReadOnlyList<GuestFeedbackHistory> History => _history;

    public static GuestFeedback Create(Guid propertyId, string number, string feedbackType, string category, string description, string? guestName, string? roomNumber, int rating, string? source, string priority, bool followUp, DateTime? followUpDate, string? subject, DateTime nowUtc, ActorId? actorId) => new()
    { GuestFeedbackId = Uuid7.NewGuid(), PropertyId = propertyId, FeedbackNumber = number, FeedbackType = feedbackType, Category = category, Description = description, GuestName = guestName, RoomNumber = roomNumber, Rating = rating, Source = source, Priority = priority, Status = FeedbackStatus.New, Subject = subject, FollowUpRequired = followUp, FollowUpDate = followUpDate, CreatedAtUtc = nowUtc, CreatedBy = actorId?.Value };

    public void Acknowledge(DateTime nowUtc) { Status = FeedbackStatus.Acknowledged; AcknowledgedAtUtc = nowUtc; _history.Add(GuestFeedbackHistory.Record(GuestFeedbackId, PropertyId, "Acknowledged", nowUtc)); }
    public void Assign(string department, DateTime nowUtc) { AssignedDepartment = department; Status = FeedbackStatus.Assigned; _history.Add(GuestFeedbackHistory.Record(GuestFeedbackId, PropertyId, $"Assigned to {department}", nowUtc)); }
    public void StartInvestigation(DateTime nowUtc) { Status = FeedbackStatus.InProgress; _history.Add(GuestFeedbackHistory.Record(GuestFeedbackId, PropertyId, "Investigation started", nowUtc)); }
    public void Resolve(string resolution, DateTime nowUtc) { Resolution = resolution; Status = FeedbackStatus.Resolved; ResolvedAtUtc = nowUtc; _history.Add(GuestFeedbackHistory.Record(GuestFeedbackId, PropertyId, "Resolved", nowUtc)); }
    public void Close(DateTime nowUtc) { Status = FeedbackStatus.Closed; ClosedAtUtc = nowUtc; _history.Add(GuestFeedbackHistory.Record(GuestFeedbackId, PropertyId, "Closed", nowUtc)); }
}

public sealed class GuestFeedbackHistory
{
    private GuestFeedbackHistory() { }
    public Guid GuestFeedbackHistoryId { get; private set; } public Guid GuestFeedbackId { get; private set; } public Guid PropertyId { get; private set; }
    public string Action { get; private set; } = string.Empty; public DateTime OccurredAtUtc { get; private set; }
    public static GuestFeedbackHistory Record(Guid fbId, Guid propertyId, string action, DateTime nowUtc) => new() { GuestFeedbackHistoryId = Uuid7.NewGuid(), GuestFeedbackId = fbId, PropertyId = propertyId, Action = action, OccurredAtUtc = nowUtc };
}
