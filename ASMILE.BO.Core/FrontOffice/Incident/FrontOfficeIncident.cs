using ASMILE.BO.Core.FrontOffice.Foundation.Identifiers;

namespace ASMILE.BO.Core.FrontOffice.Incident;

public enum IncidentPriority
{
    Low,
    Medium,
    High,
    Critical
}

public enum IncidentStatus
{
    Open,
    Assigned,
    Investigating,
    Pending,
    Resolved,
    Closed,
    Cancelled
}

/// <summary>
/// Operational incident aggregate. Guest/room/reservation references are
/// deliberately textual until the hotel schema exposes stable foreign keys.
/// </summary>
public sealed class FrontOfficeIncident
{
    private FrontOfficeIncident() { }

    public Guid IncidentId { get; private set; }
    public Guid PropertyId { get; private set; }
    public string IncidentNumber { get; private set; } = string.Empty;
    public DateTime OccurredAtUtc { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Category { get; private set; } = string.Empty;
    public IncidentPriority Priority { get; private set; }
    public string? RoomReference { get; private set; }
    public string? GuestReference { get; private set; }
    public string? ReservationReference { get; private set; }
    public string? Department { get; private set; }
    public string ReporterName { get; private set; } = string.Empty;
    public string? AssignedTo { get; private set; }
    public IncidentStatus Status { get; private set; }
    public string Impact { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public string? RootCause { get; private set; }
    public string? TemporaryAction { get; private set; }
    public string? CorrectiveAction { get; private set; }
    public string? PreventiveAction { get; private set; }
    public bool IsVip { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public Guid? CreatedBy { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }
    public Guid? UpdatedBy { get; private set; }
    public DateTime? ResolvedAtUtc { get; private set; }
    public DateTime? ClosedAtUtc { get; private set; }
    public byte[]? RowVersion { get; private set; }

    public static FrontOfficeIncident Create(
        Guid propertyId,
        string incidentNumber,
        DateTime occurredAtUtc,
        string title,
        string category,
        IncidentPriority priority,
        string reporterName,
        string impact,
        string description,
        DateTime nowUtc,
        ActorId? actorId,
        string? roomReference = null,
        string? guestReference = null,
        string? reservationReference = null,
        string? department = null,
        bool isVip = false)
    {
        if (propertyId == Guid.Empty) throw new ArgumentException("INCIDENT_PROPERTY_REQUIRED");
        if (string.IsNullOrWhiteSpace(incidentNumber)) throw new ArgumentException("INCIDENT_NUMBER_REQUIRED");
        if (string.IsNullOrWhiteSpace(title)) throw new ArgumentException("INCIDENT_TITLE_REQUIRED");
        if (string.IsNullOrWhiteSpace(category)) throw new ArgumentException("INCIDENT_CATEGORY_REQUIRED");
        if (string.IsNullOrWhiteSpace(reporterName)) throw new ArgumentException("INCIDENT_REPORTER_REQUIRED");
        if (string.IsNullOrWhiteSpace(impact)) throw new ArgumentException("INCIDENT_IMPACT_REQUIRED");
        if (string.IsNullOrWhiteSpace(description)) throw new ArgumentException("INCIDENT_DESCRIPTION_REQUIRED");

        return new FrontOfficeIncident
        {
            IncidentId = Uuid7.NewGuid(),
            PropertyId = propertyId,
            IncidentNumber = incidentNumber.Trim(),
            OccurredAtUtc = occurredAtUtc,
            Title = title.Trim(),
            Category = category.Trim(),
            Priority = priority,
            RoomReference = Normalize(roomReference),
            GuestReference = Normalize(guestReference),
            ReservationReference = Normalize(reservationReference),
            Department = Normalize(department),
            ReporterName = reporterName.Trim(),
            Status = IncidentStatus.Open,
            Impact = impact.Trim(),
            Description = description.Trim(),
            IsVip = isVip,
            CreatedAtUtc = nowUtc,
            CreatedBy = actorId?.Value,
            UpdatedAtUtc = nowUtc,
            UpdatedBy = actorId?.Value
        };
    }

    public void Assign(string assignee, DateTime nowUtc, ActorId? actorId)
    {
        if (string.IsNullOrWhiteSpace(assignee)) throw new ArgumentException("INCIDENT_ASSIGNEE_REQUIRED");
        RequireNotTerminal();
        AssignedTo = assignee.Trim();
        Status = IncidentStatus.Assigned;
        Touch(nowUtc, actorId);
    }

    public void StartInvestigation(DateTime nowUtc, ActorId? actorId)
    {
        RequireStatus(IncidentStatus.Open, IncidentStatus.Assigned, IncidentStatus.Pending);
        Status = IncidentStatus.Investigating;
        Touch(nowUtc, actorId);
    }

    public void SetPending(string reason, DateTime nowUtc, ActorId? actorId)
    {
        if (string.IsNullOrWhiteSpace(reason)) throw new ArgumentException("INCIDENT_PENDING_REASON_REQUIRED");
        RequireNotTerminal();
        Status = IncidentStatus.Pending;
        TemporaryAction = reason.Trim();
        Touch(nowUtc, actorId);
    }

    public void Resolve(string rootCause, string correctiveAction, DateTime nowUtc, ActorId? actorId)
    {
        if (string.IsNullOrWhiteSpace(rootCause)) throw new ArgumentException("INCIDENT_ROOT_CAUSE_REQUIRED");
        if (string.IsNullOrWhiteSpace(correctiveAction)) throw new ArgumentException("INCIDENT_CORRECTIVE_ACTION_REQUIRED");
        RequireStatus(IncidentStatus.Open, IncidentStatus.Assigned, IncidentStatus.Investigating, IncidentStatus.Pending);
        RootCause = rootCause.Trim();
        CorrectiveAction = correctiveAction.Trim();
        Status = IncidentStatus.Resolved;
        ResolvedAtUtc = nowUtc;
        Touch(nowUtc, actorId);
    }

    public void Close(string preventiveAction, DateTime nowUtc, ActorId? actorId)
    {
        if (string.IsNullOrWhiteSpace(preventiveAction)) throw new ArgumentException("INCIDENT_PREVENTIVE_ACTION_REQUIRED");
        RequireStatus(IncidentStatus.Resolved);
        PreventiveAction = preventiveAction.Trim();
        Status = IncidentStatus.Closed;
        ClosedAtUtc = nowUtc;
        Touch(nowUtc, actorId);
    }

    public void Cancel(string reason, DateTime nowUtc, ActorId? actorId)
    {
        if (string.IsNullOrWhiteSpace(reason)) throw new ArgumentException("INCIDENT_CANCEL_REASON_REQUIRED");
        if (Status is IncidentStatus.Resolved or IncidentStatus.Closed)
            throw new InvalidOperationException("INCIDENT_STATUS_NOT_CANCELLABLE");
        Status = IncidentStatus.Cancelled;
        TemporaryAction = reason.Trim();
        Touch(nowUtc, actorId);
    }

    private void RequireNotTerminal()
    {
        if (Status is IncidentStatus.Resolved or IncidentStatus.Closed or IncidentStatus.Cancelled)
            throw new InvalidOperationException("INCIDENT_STATUS_TERMINAL");
    }

    private void RequireStatus(params IncidentStatus[] allowed)
    {
        if (!allowed.Contains(Status))
            throw new InvalidOperationException($"INCIDENT_STATUS_{Status.ToString().ToUpperInvariant()}");
    }

    private void Touch(DateTime nowUtc, ActorId? actorId)
    {
        UpdatedAtUtc = nowUtc;
        UpdatedBy = actorId?.Value;
    }

    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
