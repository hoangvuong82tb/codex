using ASMILE.BO.Core.FrontOffice.Foundation.Identifiers;

namespace ASMILE.BO.Core.FrontOffice.Concierge;

public enum ConciergeStatus { Open, Assigned, Accepted, InProgress, WaitingGuest, Completed, Cancelled, Rejected }

public sealed class ConciergeRequest
{
    private readonly List<ConciergeHistory> _history = [];
    private ConciergeRequest() { }

    public Guid ConciergeRequestId { get; private set; }
    public Guid PropertyId { get; private set; }
    public string RequestNumber { get; private set; } = string.Empty;
    public Guid? GuestId { get; private set; }
    public Guid? ReservationId { get; private set; }
    public Guid? HotelStayId { get; private set; }
    public Guid? HotelRoomId { get; private set; }
    public string? GuestName { get; private set; }
    public string? RoomNumber { get; private set; }
    public string RequestType { get; private set; } = string.Empty;
    public string Priority { get; private set; } = "Normal";
    public ConciergeStatus Status { get; private set; }
    public DateTime RequestedAtUtc { get; private set; }
    public string? AssignedDepartment { get; private set; }
    public Guid? AssignedStaffId { get; private set; }
    public DateTime? ExpectedCompletion { get; private set; }
    public DateTime? CompletedAtUtc { get; private set; }
    public decimal? EstimatedCost { get; private set; }
    public decimal? ActualCost { get; private set; }
    public string? ChargeType { get; private set; }
    public Guid? FolioEntryId { get; private set; }
    public string? Notes { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public Guid? CreatedBy { get; private set; }
    public byte[]? RowVersion { get; private set; }
    public IReadOnlyList<ConciergeHistory> History => _history;

    public static ConciergeRequest Create(Guid propertyId, string requestNumber, string requestType, string priority, Guid? guestId, Guid? stayId, Guid? roomId, string? guestName, string? roomNumber, string? notes, decimal? estimatedCost, string? chargeType, DateTime nowUtc, ActorId? actorId) => new()
    { ConciergeRequestId = Uuid7.NewGuid(), PropertyId = propertyId, RequestNumber = requestNumber, RequestType = requestType, Priority = priority, GuestId = guestId, HotelStayId = stayId, HotelRoomId = roomId, GuestName = guestName, RoomNumber = roomNumber, Notes = notes, EstimatedCost = estimatedCost, ChargeType = chargeType, Status = ConciergeStatus.Open, RequestedAtUtc = nowUtc, CreatedAtUtc = nowUtc, CreatedBy = actorId?.Value };

    public void Assign(string department, DateTime? expectedCompletion, DateTime nowUtc) { AssignedDepartment = department; ExpectedCompletion = expectedCompletion; Status = ConciergeStatus.Assigned; _history.Add(ConciergeHistory.Record(ConciergeRequestId, PropertyId, $"Assigned to {department}", nowUtc)); }
    public void Accept(DateTime nowUtc) { Status = ConciergeStatus.Accepted; _history.Add(ConciergeHistory.Record(ConciergeRequestId, PropertyId, "Accepted", nowUtc)); }
    public void StartWork(DateTime nowUtc) { Status = ConciergeStatus.InProgress; _history.Add(ConciergeHistory.Record(ConciergeRequestId, PropertyId, "In progress", nowUtc)); }
    public void WaitGuest(DateTime nowUtc) { Status = ConciergeStatus.WaitingGuest; _history.Add(ConciergeHistory.Record(ConciergeRequestId, PropertyId, "Waiting for guest", nowUtc)); }
    public void Complete(decimal? actualCost, DateTime nowUtc) { Status = ConciergeStatus.Completed; ActualCost = actualCost; CompletedAtUtc = nowUtc; _history.Add(ConciergeHistory.Record(ConciergeRequestId, PropertyId, "Completed", nowUtc)); }
    public void Cancel(string reason, DateTime nowUtc) { Status = ConciergeStatus.Cancelled; _history.Add(ConciergeHistory.Record(ConciergeRequestId, PropertyId, $"Cancelled: {reason}", nowUtc)); }
    public void LinkFolioEntry(Guid entryId) => FolioEntryId = entryId;
}

public sealed class ConciergeHistory
{
    private ConciergeHistory() { }
    public Guid ConciergeHistoryId { get; private set; } public Guid ConciergeRequestId { get; private set; } public Guid PropertyId { get; private set; }
    public string Action { get; private set; } = string.Empty; public DateTime OccurredAtUtc { get; private set; }
    public static ConciergeHistory Record(Guid requestId, Guid propertyId, string action, DateTime nowUtc) => new() { ConciergeHistoryId = Uuid7.NewGuid(), ConciergeRequestId = requestId, PropertyId = propertyId, Action = action, OccurredAtUtc = nowUtc };
}
