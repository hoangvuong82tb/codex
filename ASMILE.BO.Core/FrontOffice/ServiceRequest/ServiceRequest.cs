using ASMILE.BO.Core.FrontOffice.Foundation.Identifiers;

namespace ASMILE.BO.Core.FrontOffice.ServiceRequest;

public enum ServiceRequestStatus { Open, Assigned, Accepted, InProgress, Completed, Cancelled, Rejected }
public enum ServicePriority { Low, Normal, High, Urgent, Emergency }
public enum ServiceCategory { Housekeeping, Engineering, FrontOffice, Laundry, Transportation, FoodBeverage, Security, Management, Custom }
public enum ChargeType { ImmediateCharge, ChargeAtCheckout, Complimentary }

public sealed class ServiceRequest
{
    private readonly List<ServiceHistory> _history = [];
    private ServiceRequest() { }

    public Guid ServiceRequestId { get; private set; }
    public Guid PropertyId { get; private set; }
    public string RequestNumber { get; private set; } = string.Empty;
    public Guid? HotelStayId { get; private set; }
    public Guid? HotelRoomId { get; private set; }
    public string? RoomNumber { get; private set; }
    public string? GuestName { get; private set; }
    public string ServiceType { get; private set; } = string.Empty;
    public ServiceCategory Category { get; private set; }
    public ServicePriority Priority { get; private set; }
    public ServiceRequestStatus Status { get; private set; }
    public string? Description { get; private set; }
    public string? AssignedDepartment { get; private set; }
    public Guid? AssignedStaffId { get; private set; }
    public decimal? EstimatedCost { get; private set; }
    public decimal? ActualCost { get; private set; }
    public ChargeType ChargeType { get; private set; }
    public Guid? FolioEntryId { get; private set; }
    public DateTime RequestedAtUtc { get; private set; }
    public DateTime? SlaDeadlineAtUtc { get; private set; }
    public DateTime? FirstResponseAtUtc { get; private set; }
    public DateTime? CompletedAtUtc { get; private set; }
    public Guid? RequestedBy { get; private set; }
    public byte[]? RowVersion { get; private set; }
    public IReadOnlyList<ServiceHistory> History => _history;

    public static ServiceRequest Create(Guid propertyId, string requestNumber, string serviceType, ServiceCategory category, ServicePriority priority, string? description, Guid? stayId, Guid? roomId, string? roomNumber, string? guestName, ChargeType chargeType, decimal? estimatedCost, DateTime? slaDeadlineAtUtc, DateTime nowUtc, ActorId? actorId) => new()
    { ServiceRequestId = Uuid7.NewGuid(), PropertyId = propertyId, RequestNumber = requestNumber, ServiceType = serviceType, Category = category, Priority = priority, Description = description, HotelStayId = stayId, HotelRoomId = roomId, RoomNumber = roomNumber, GuestName = guestName, ChargeType = chargeType, EstimatedCost = estimatedCost, Status = ServiceRequestStatus.Open, RequestedAtUtc = nowUtc, SlaDeadlineAtUtc = slaDeadlineAtUtc, RequestedBy = actorId?.Value };

    public void Assign(string department, Guid? staffId, DateTime nowUtc)
    {
        EnsureNotTerminal("Assign");
        if (string.IsNullOrWhiteSpace(department))
            throw new InvalidOperationException("SERVICE_REQUEST_ASSIGNMENT_DEPARTMENT_REQUIRED");

        MarkFirstResponse(nowUtc);
        AssignedDepartment = department.Trim();
        AssignedStaffId = staffId;
        Status = ServiceRequestStatus.Assigned;
        _history.Add(ServiceHistory.Record(ServiceRequestId, PropertyId, "Assigned", AssignedDepartment, nowUtc));
    }

    public void Accept(DateTime nowUtc)
    {
        EnsureNotTerminal("Accept");
        MarkFirstResponse(nowUtc);
        Status = ServiceRequestStatus.Accepted;
        _history.Add(ServiceHistory.Record(ServiceRequestId, PropertyId, "Accepted", null, nowUtc));
    }

    public void StartWork(DateTime nowUtc)
    {
        EnsureNotTerminal("StartWork");
        MarkFirstResponse(nowUtc);
        Status = ServiceRequestStatus.InProgress;
        _history.Add(ServiceHistory.Record(ServiceRequestId, PropertyId, "InProgress", null, nowUtc));
    }

    public void Complete(decimal? actualCost, DateTime nowUtc)
    {
        EnsureNotTerminal("Complete");
        Status = ServiceRequestStatus.Completed;
        ActualCost = actualCost;
        CompletedAtUtc = nowUtc;
        _history.Add(ServiceHistory.Record(ServiceRequestId, PropertyId, "Completed", null, nowUtc));
    }

    public void Cancel(string reason, DateTime nowUtc)
    {
        EnsureNotTerminal("Cancel");
        if (string.IsNullOrWhiteSpace(reason))
            throw new InvalidOperationException("SERVICE_REQUEST_CANCEL_REASON_REQUIRED");

        Status = ServiceRequestStatus.Cancelled;
        _history.Add(ServiceHistory.Record(ServiceRequestId, PropertyId, $"Cancelled: {reason.Trim()}", null, nowUtc));
    }
    public void LinkFolioEntry(Guid entryId) => FolioEntryId = entryId;

    private void MarkFirstResponse(DateTime nowUtc)
    {
        FirstResponseAtUtc ??= nowUtc;
    }

    private void EnsureNotTerminal(string operation)
    {
        if (Status is ServiceRequestStatus.Completed or ServiceRequestStatus.Cancelled or ServiceRequestStatus.Rejected)
            throw new InvalidOperationException($"SERVICE_REQUEST_TERMINAL_STATUS_CANNOT_{operation.ToUpperInvariant()}");
    }
}

public sealed class ServiceHistory
{
    private ServiceHistory() { }
    public Guid ServiceHistoryId { get; private set; } public Guid ServiceRequestId { get; private set; } public Guid PropertyId { get; private set; }
    public string Action { get; private set; } = string.Empty; public string? Detail { get; private set; } public DateTime OccurredAtUtc { get; private set; }
    public static ServiceHistory Record(Guid requestId, Guid propertyId, string action, string? detail, DateTime nowUtc) => new()
    { ServiceHistoryId = Uuid7.NewGuid(), ServiceRequestId = requestId, PropertyId = propertyId, Action = action, Detail = detail, OccurredAtUtc = nowUtc };
}
