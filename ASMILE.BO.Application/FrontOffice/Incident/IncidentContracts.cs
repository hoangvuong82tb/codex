using ASMILE.BO.Core.FrontOffice.Incident;

namespace ASMILE.BO.Application.FrontOffice.Incident;

public sealed record IncidentSearchRequest(
    Guid PropertyId,
    string? SearchText = null,
    string? Category = null,
    IncidentPriority? Priority = null,
    IncidentStatus? Status = null,
    DateTime? FromUtc = null,
    DateTime? ToUtc = null
);

public sealed record IncidentDto(
    Guid IncidentId,
    Guid PropertyId,
    string IncidentNumber,
    DateTime OccurredAtUtc,
    string Title,
    string Category,
    IncidentPriority Priority,
    string? RoomReference,
    string? GuestReference,
    string? ReservationReference,
    string? Department,
    string ReporterName,
    string? AssignedTo,
    IncidentStatus Status,
    string Impact,
    string Description,
    string? RootCause,
    string? TemporaryAction,
    string? CorrectiveAction,
    string? PreventiveAction,
    bool IsVip,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    DateTime? ResolvedAtUtc,
    DateTime? ClosedAtUtc,
    byte[]? RowVersion
);

public sealed record CreateIncidentRequest(
    DateTime OccurredAtUtc,
    string Title,
    string Category,
    IncidentPriority Priority,
    string? RoomReference,
    string? GuestReference,
    string? ReservationReference,
    string? Department,
    string Impact,
    string Description,
    bool IsVip
);

public enum IncidentStatusAction
{
    Assign,
    StartInvestigation,
    SetPending,
    Resolve,
    Close,
    Cancel
}

public sealed record ChangeIncidentStatusRequest(
    Guid IncidentId,
    IncidentStatusAction Action,
    string? Assignee = null,
    string? Reason = null,
    string? RootCause = null,
    string? CorrectiveAction = null,
    string? PreventiveAction = null,
    byte[]? ExpectedRowVersion = null
);

public interface IIncidentQueryService
{
    Task<IReadOnlyList<IncidentDto>> SearchAsync(IncidentSearchRequest request, CancellationToken ct);
}

public interface IIncidentCommandService
{
    Task<IncidentDto> CreateAsync(CreateIncidentRequest request, CancellationToken ct);
    Task<IncidentDto> ChangeStatusAsync(ChangeIncidentStatusRequest request, CancellationToken ct);
}
