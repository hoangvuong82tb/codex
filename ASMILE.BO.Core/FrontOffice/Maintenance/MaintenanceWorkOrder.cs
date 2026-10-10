using ASMILE.BO.Core.FrontOffice.Foundation.Identifiers;

namespace ASMILE.BO.Core.FrontOffice.Maintenance;

/// <summary>
/// Persisted maintenance work order. The application contract owns the public
/// enum values; this aggregate stores their stable names so old databases can
/// be upgraded without relying on enum ordinals.
/// </summary>
public sealed class MaintenanceWorkOrder
{
    private MaintenanceWorkOrder() { }

    public Guid WorkOrderId { get; private set; }
    public string OrderNumber { get; private set; } = string.Empty;
    public Guid PropertyId { get; private set; }
    public Guid? RequestId { get; private set; }
    public Guid? RoomId { get; private set; }
    public string? RoomNumber { get; private set; }
    public string Type { get; private set; } = string.Empty;
    public string Priority { get; private set; } = string.Empty;
    public string Category { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public string Status { get; private set; } = "Open";
    public Guid? AssignedEngineerId { get; private set; }
    public string? AssignedEngineerName { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public Guid? CreatedBy { get; private set; }
    public DateTime? StartedAtUtc { get; private set; }
    public DateTime? CompletedAtUtc { get; private set; }
    public DateTime? VerifiedAtUtc { get; private set; }
    public Guid? VerifiedBy { get; private set; }
    public int EstimatedMinutes { get; private set; }
    public int? ActualMinutes { get; private set; }
    public string? Resolution { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }
    public Guid? UpdatedBy { get; private set; }
    public byte[]? RowVersion { get; private set; }
    public ICollection<MaintenanceWorkOrderNote> Notes { get; private set; } = new List<MaintenanceWorkOrderNote>();

    public static MaintenanceWorkOrder Create(
        Guid propertyId,
        string orderNumber,
        Guid? requestId,
        Guid? roomId,
        string? roomNumber,
        string type,
        string priority,
        string category,
        string description,
        int estimatedMinutes,
        DateTime nowUtc,
        ActorId? actorId)
    {
        if (propertyId == Guid.Empty) throw new ArgumentException("MAINTENANCE_PROPERTY_REQUIRED");
        if (string.IsNullOrWhiteSpace(orderNumber)) throw new ArgumentException("MAINTENANCE_ORDER_NUMBER_REQUIRED");
        if (string.IsNullOrWhiteSpace(type)) throw new ArgumentException("MAINTENANCE_TYPE_REQUIRED");
        if (string.IsNullOrWhiteSpace(priority)) throw new ArgumentException("MAINTENANCE_PRIORITY_REQUIRED");
        if (string.IsNullOrWhiteSpace(category)) throw new ArgumentException("MAINTENANCE_CATEGORY_REQUIRED");
        if (string.IsNullOrWhiteSpace(description)) throw new ArgumentException("MAINTENANCE_DESCRIPTION_REQUIRED");
        if (estimatedMinutes < 0) throw new ArgumentException("MAINTENANCE_ESTIMATE_INVALID");

        return new MaintenanceWorkOrder
        {
            WorkOrderId = Uuid7.NewGuid(),
            OrderNumber = orderNumber.Trim(),
            PropertyId = propertyId,
            RequestId = requestId,
            RoomId = roomId,
            RoomNumber = Normalize(roomNumber),
            Type = type.Trim(),
            Priority = priority.Trim(),
            Category = category.Trim(),
            Description = description.Trim(),
            Status = "Open",
            EstimatedMinutes = estimatedMinutes,
            CreatedAtUtc = nowUtc,
            CreatedBy = actorId?.Value,
            UpdatedAtUtc = nowUtc,
            UpdatedBy = actorId?.Value
        };
    }

    public void Assign(Guid engineerId, string? engineerName, DateTime nowUtc, ActorId? actorId)
    {
        if (engineerId == Guid.Empty) throw new ArgumentException("MAINTENANCE_ENGINEER_REQUIRED");
        if (Status is not ("Open" or "Escalated"))
            throw new InvalidOperationException($"MAINTENANCE_ASSIGN_INVALID_STATUS_{Status.ToUpperInvariant()}");

        AssignedEngineerId = engineerId;
        AssignedEngineerName = Normalize(engineerName);
        Status = "Assigned";
        Touch(nowUtc, actorId);
        AddNote("Assigned", AssignedEngineerName, actorId, nowUtc);
    }

    public void Transition(string targetStatus, string? note, Guid? actorId, DateTime nowUtc)
    {
        if (string.IsNullOrWhiteSpace(targetStatus)) throw new ArgumentException("MAINTENANCE_TARGET_STATUS_REQUIRED");
        var from = Status;
        if (string.Equals(from, targetStatus, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("MAINTENANCE_STATUS_UNCHANGED");

        Status = targetStatus.Trim();
        if (Status == "InProgress" && StartedAtUtc is null) StartedAtUtc = nowUtc;
        if (Status == "Completed")
        {
            CompletedAtUtc = nowUtc;
            if (StartedAtUtc is DateTime started)
                ActualMinutes = Math.Max(0, (int)(nowUtc - started).TotalMinutes);
        }
        if (Status == "Verified")
        {
            VerifiedAtUtc = nowUtc;
            VerifiedBy = actorId;
        }
        Touch(nowUtc, actorId is Guid value ? ActorId.From(value) : null);
        AddNote($"{from}->{Status}", note, actorId is Guid value2 ? ActorId.From(value2) : null, nowUtc);
    }

    private void AddNote(string action, string? detail, ActorId? actorId, DateTime nowUtc)
        => Notes.Add(MaintenanceWorkOrderNote.Create(WorkOrderId, action, detail, actorId?.Value, nowUtc));

    private void Touch(DateTime nowUtc, ActorId? actorId)
    {
        UpdatedAtUtc = nowUtc;
        UpdatedBy = actorId?.Value;
    }

    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

public sealed class MaintenanceWorkOrderNote
{
    private MaintenanceWorkOrderNote() { }

    public Guid NoteId { get; private set; }
    public Guid WorkOrderId { get; private set; }
    public DateTime AtUtc { get; private set; }
    public string Action { get; private set; } = string.Empty;
    public string? Detail { get; private set; }
    public Guid? ActorId { get; private set; }

    public static MaintenanceWorkOrderNote Create(Guid workOrderId, string action, string? detail, Guid? actorId, DateTime atUtc)
        => new()
        {
            NoteId = Uuid7.NewGuid(),
            WorkOrderId = workOrderId,
            AtUtc = atUtc,
            Action = action.Trim(),
            Detail = string.IsNullOrWhiteSpace(detail) ? null : detail.Trim(),
            ActorId = actorId
        };
}
