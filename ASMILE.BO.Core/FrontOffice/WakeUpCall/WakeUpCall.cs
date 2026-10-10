using ASMILE.BO.Core.FrontOffice.Foundation.Identifiers;

namespace ASMILE.BO.Core.FrontOffice.WakeUpCall;

public enum WakeUpCallStatus { Scheduled, Pending, Calling, Answered, NoAnswer, Completed, Cancelled, Expired }
public enum WakeUpResult { Answered, Busy, NoAnswer, WrongNumber, CancelledByGuest, CompletedSuccessfully }

public sealed class WakeUpCall
{
    private readonly List<WakeUpCallHistory> _history = [];
    private WakeUpCall() { }

    public Guid WakeUpCallId { get; private set; }
    public Guid PropertyId { get; private set; }
    public string CallNumber { get; private set; } = string.Empty;
    public Guid? HotelStayId { get; private set; }
    public Guid? HotelRoomId { get; private set; }
    public string? RoomNumber { get; private set; }
    public string? GuestName { get; private set; }
    public DateTime WakeTime { get; private set; }
    public DateTime? ActualCallTime { get; private set; }
    public WakeUpCallStatus Status { get; private set; }
    public WakeUpResult? Result { get; private set; }
    public bool IsRecurring { get; private set; }
    public bool IsDaily { get; private set; }
    public int RetryCount { get; private set; }
    public string? Notes { get; private set; }
    public Guid? AssignedStaffId { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public Guid? CreatedBy { get; private set; }
    public byte[]? RowVersion { get; private set; }
    public IReadOnlyList<WakeUpCallHistory> History => _history;

    public DateTime DueTime => WakeTime.AddMinutes(-5);
    public bool IsOverdue => Status is WakeUpCallStatus.Scheduled or WakeUpCallStatus.Pending && DateTime.UtcNow > WakeTime.AddMinutes(10);
    public bool IsDueSoon => Status is WakeUpCallStatus.Scheduled or WakeUpCallStatus.Pending && DateTime.UtcNow >= DueTime && DateTime.UtcNow <= WakeTime;

    public static WakeUpCall Create(Guid propertyId, string callNumber, Guid? stayId, Guid? roomId, string? roomNumber, string? guestName, DateTime wakeTime, bool recurring, bool daily, string? notes, DateTime nowUtc, ActorId? actorId) => new()
    { WakeUpCallId = Uuid7.NewGuid(), PropertyId = propertyId, CallNumber = callNumber, HotelStayId = stayId, HotelRoomId = roomId, RoomNumber = roomNumber, GuestName = guestName, WakeTime = wakeTime, Status = WakeUpCallStatus.Scheduled, IsRecurring = recurring, IsDaily = daily, Notes = notes, CreatedAtUtc = nowUtc, CreatedBy = actorId?.Value };

    public void Activate(DateTime nowUtc) { Status = WakeUpCallStatus.Pending; _history.Add(WakeUpCallHistory.Record(WakeUpCallId, PropertyId, "Activated", nowUtc)); }
    public void StartCalling(DateTime nowUtc) { Status = WakeUpCallStatus.Calling; ActualCallTime = nowUtc; _history.Add(WakeUpCallHistory.Record(WakeUpCallId, PropertyId, "Calling", nowUtc)); }
    public void RecordResult(WakeUpResult result, DateTime nowUtc)
    {
        Status = result switch { WakeUpResult.Answered or WakeUpResult.CompletedSuccessfully => WakeUpCallStatus.Completed, WakeUpResult.NoAnswer => WakeUpCallStatus.NoAnswer, WakeUpResult.Busy => WakeUpCallStatus.Pending, _ => WakeUpCallStatus.Completed };
        Result = result;
        if (result == WakeUpResult.NoAnswer) { RetryCount++; Status = WakeUpCallStatus.Pending; }
        _history.Add(WakeUpCallHistory.Record(WakeUpCallId, PropertyId, $"Result: {result}", nowUtc));
    }
    public void Cancel(string reason, DateTime nowUtc) { Status = WakeUpCallStatus.Cancelled; _history.Add(WakeUpCallHistory.Record(WakeUpCallId, PropertyId, $"Cancelled: {reason}", nowUtc)); }
    public void Expire(DateTime nowUtc) { Status = WakeUpCallStatus.Expired; _history.Add(WakeUpCallHistory.Record(WakeUpCallId, PropertyId, "Expired", nowUtc)); }
}

public sealed class WakeUpCallHistory
{
    private WakeUpCallHistory() { }
    public Guid WakeUpCallHistoryId { get; private set; } public Guid WakeUpCallId { get; private set; } public Guid PropertyId { get; private set; }
    public string Action { get; private set; } = string.Empty; public DateTime OccurredAtUtc { get; private set; }
    public static WakeUpCallHistory Record(Guid callId, Guid propertyId, string action, DateTime nowUtc) => new() { WakeUpCallHistoryId = Uuid7.NewGuid(), WakeUpCallId = callId, PropertyId = propertyId, Action = action, OccurredAtUtc = nowUtc };
}
