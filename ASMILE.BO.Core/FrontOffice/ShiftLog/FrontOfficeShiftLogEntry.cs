using ASMILE.BO.Core.FrontOffice.Foundation.Identifiers;
using ASMILE.BO.Core.FrontOffice.Incident;

namespace ASMILE.BO.Core.FrontOffice.ShiftLog;

public enum ShiftLogEntryStatus
{
    Open,
    InProgress,
    Completed,
    Cancelled
}

/// <summary>
/// A property-scoped operational note recorded during a front-office shift.
/// This is intentionally separate from incidents and cash shifts.
/// </summary>
public sealed class FrontOfficeShiftLogEntry
{
    private FrontOfficeShiftLogEntry() { }

    public Guid ShiftLogEntryId { get; private set; }
    public Guid PropertyId { get; private set; }
    public DateTime OccurredAtUtc { get; private set; }
    public string ShiftName { get; private set; } = string.Empty;
    public string Title { get; private set; } = string.Empty;
    public string Category { get; private set; } = string.Empty;
    public IncidentPriority Priority { get; private set; }
    public string? RoomReference { get; private set; }
    public string? GuestReference { get; private set; }
    public string? ReservationReference { get; private set; }
    public string? AssignedTo { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public bool IsVip { get; private set; }
    public ShiftLogEntryStatus Status { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public Guid? CreatedBy { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }
    public Guid? UpdatedBy { get; private set; }
    public byte[]? RowVersion { get; private set; }

    public static FrontOfficeShiftLogEntry Create(
        Guid propertyId,
        DateTime occurredAtUtc,
        string shiftName,
        string title,
        string category,
        IncidentPriority priority,
        string description,
        DateTime nowUtc,
        ActorId? actorId,
        string? roomReference = null,
        string? guestReference = null,
        string? reservationReference = null,
        string? assignedTo = null,
        bool isVip = false)
    {
        if (propertyId == Guid.Empty) throw new ArgumentException("SHIFT_LOG_PROPERTY_REQUIRED");
        if (string.IsNullOrWhiteSpace(shiftName)) throw new ArgumentException("SHIFT_LOG_SHIFT_REQUIRED");
        if (string.IsNullOrWhiteSpace(title)) throw new ArgumentException("SHIFT_LOG_TITLE_REQUIRED");
        if (string.IsNullOrWhiteSpace(category)) throw new ArgumentException("SHIFT_LOG_CATEGORY_REQUIRED");
        if (string.IsNullOrWhiteSpace(description)) throw new ArgumentException("SHIFT_LOG_DESCRIPTION_REQUIRED");

        return new FrontOfficeShiftLogEntry
        {
            ShiftLogEntryId = Uuid7.NewGuid(),
            PropertyId = propertyId,
            OccurredAtUtc = occurredAtUtc,
            ShiftName = shiftName.Trim(),
            Title = title.Trim(),
            Category = category.Trim(),
            Priority = priority,
            RoomReference = Normalize(roomReference),
            GuestReference = Normalize(guestReference),
            ReservationReference = Normalize(reservationReference),
            AssignedTo = Normalize(assignedTo),
            Description = description.Trim(),
            IsVip = isVip,
            Status = ShiftLogEntryStatus.Open,
            CreatedAtUtc = nowUtc,
            CreatedBy = actorId?.Value,
            UpdatedAtUtc = nowUtc,
            UpdatedBy = actorId?.Value
        };
    }

    public void ChangeStatus(ShiftLogEntryStatus status, DateTime nowUtc, ActorId? actorId)
    {
        if (status == ShiftLogEntryStatus.Open && Status != ShiftLogEntryStatus.Open)
            throw new InvalidOperationException("SHIFT_LOG_STATUS_CANNOT_REOPEN");
        if (Status == ShiftLogEntryStatus.Cancelled && status != ShiftLogEntryStatus.Cancelled)
            throw new InvalidOperationException("SHIFT_LOG_STATUS_TERMINAL");
        Status = status;
        UpdatedAtUtc = nowUtc;
        UpdatedBy = actorId?.Value;
    }

    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
