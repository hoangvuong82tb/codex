using ASMILE.BO.Core.FrontOffice.Foundation.Identifiers;

namespace ASMILE.BO.Core.FrontOffice.Message;

public enum MessageStatus { Draft, Pending, Sent, Delivered, Read, Replied, Completed, Cancelled }

public sealed class GuestMessage
{
    private readonly List<MessageHistory> _history = [];
    private GuestMessage() { }

    public Guid GuestMessageId { get; private set; }
    public Guid PropertyId { get; private set; }
    public string MessageNumber { get; private set; } = string.Empty;
    public Guid? GuestId { get; private set; }
    public Guid? ReservationId { get; private set; }
    public Guid? HotelStayId { get; private set; }
    public Guid? HotelRoomId { get; private set; }
    public string? GuestName { get; private set; }
    public string? RoomNumber { get; private set; }
    public string MessageType { get; private set; } = string.Empty;
    public string Channel { get; private set; } = "FrontDesk";
    public string Priority { get; private set; } = "Normal";
    public MessageStatus Status { get; private set; }
    public string? Subject { get; private set; }
    public string Content { get; private set; } = string.Empty;
    public string? Sender { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? DeliveredAtUtc { get; private set; }
    public DateTime? ReadAtUtc { get; private set; }
    public DateTime? CompletedAtUtc { get; private set; }
    public bool RequiresResponse { get; private set; }
    public string? ResponseContent { get; private set; }
    public string? AssignedStaff { get; private set; }
    public string? Department { get; private set; }
    public Guid? CreatedBy { get; private set; }
    public byte[]? RowVersion { get; private set; }
    public IReadOnlyList<MessageHistory> History => _history;

    public static GuestMessage Create(Guid propertyId, string messageNumber, string messageType, string content, string? guestName, string? roomNumber, string? subject, string channel, string priority, bool requiresResponse, string? department, DateTime nowUtc, ActorId? actorId) => new()
    { GuestMessageId = Uuid7.NewGuid(), PropertyId = propertyId, MessageNumber = messageNumber, MessageType = messageType, Content = content, GuestName = guestName, RoomNumber = roomNumber, Subject = subject, Channel = channel, Priority = priority, RequiresResponse = requiresResponse, Department = department, Status = MessageStatus.Draft, CreatedAtUtc = nowUtc, CreatedBy = actorId?.Value };

    public void Send(DateTime nowUtc) { Status = MessageStatus.Sent; _history.Add(MessageHistory.Record(GuestMessageId, PropertyId, "Sent", nowUtc)); }
    public void MarkDelivered(DateTime nowUtc) { Status = MessageStatus.Delivered; DeliveredAtUtc = nowUtc; _history.Add(MessageHistory.Record(GuestMessageId, PropertyId, "Delivered", nowUtc)); }
    public void MarkRead(DateTime nowUtc) { Status = MessageStatus.Read; ReadAtUtc = nowUtc; _history.Add(MessageHistory.Record(GuestMessageId, PropertyId, "Read", nowUtc)); }
    public void Reply(string response, DateTime nowUtc) { ResponseContent = response; Status = MessageStatus.Replied; _history.Add(MessageHistory.Record(GuestMessageId, PropertyId, "Replied", nowUtc)); }
    public void Complete(DateTime nowUtc) { Status = MessageStatus.Completed; CompletedAtUtc = nowUtc; _history.Add(MessageHistory.Record(GuestMessageId, PropertyId, "Completed", nowUtc)); }
    public void Cancel(DateTime nowUtc) { Status = MessageStatus.Cancelled; _history.Add(MessageHistory.Record(GuestMessageId, PropertyId, "Cancelled", nowUtc)); }
}

public sealed class MessageHistory
{
    private MessageHistory() { }
    public Guid MessageHistoryId { get; private set; } public Guid GuestMessageId { get; private set; } public Guid PropertyId { get; private set; }
    public string Action { get; private set; } = string.Empty; public DateTime OccurredAtUtc { get; private set; }
    public static MessageHistory Record(Guid msgId, Guid propertyId, string action, DateTime nowUtc) => new() { MessageHistoryId = Uuid7.NewGuid(), GuestMessageId = msgId, PropertyId = propertyId, Action = action, OccurredAtUtc = nowUtc };
}
