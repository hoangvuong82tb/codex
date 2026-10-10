using ASMILE.BO.Core.FrontOffice.Incident;
using ASMILE.BO.Core.FrontOffice.ShiftLog;

namespace ASMILE.BO.Application.FrontOffice.ShiftLog;

public sealed record ShiftLogEntrySearchRequest(
    Guid PropertyId,
    string? SearchText = null,
    string? Category = null,
    ShiftLogEntryStatus? Status = null,
    DateTime? FromUtc = null,
    DateTime? ToUtc = null);

public sealed record ShiftLogEntryDto(
    Guid ShiftLogEntryId,
    Guid PropertyId,
    DateTime OccurredAtUtc,
    string ShiftName,
    string Title,
    string Category,
    IncidentPriority Priority,
    string? RoomReference,
    string? GuestReference,
    string? ReservationReference,
    string? AssignedTo,
    string Description,
    bool IsVip,
    ShiftLogEntryStatus Status,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    byte[]? RowVersion);

public sealed record CreateShiftLogEntryRequest(
    DateTime OccurredAtUtc,
    string ShiftName,
    string Title,
    string Category,
    IncidentPriority Priority,
    string? RoomReference,
    string? GuestReference,
    string? ReservationReference,
    string? AssignedTo,
    string Description,
    bool IsVip);

public sealed record ChangeShiftLogEntryStatusRequest(
    Guid ShiftLogEntryId,
    ShiftLogEntryStatus Status,
    byte[]? ExpectedRowVersion = null);

public interface IShiftLogEntryQueryService
{
    Task<IReadOnlyList<ShiftLogEntryDto>> SearchAsync(ShiftLogEntrySearchRequest request, CancellationToken ct);
}

public interface IShiftLogEntryCommandService
{
    Task<ShiftLogEntryDto> CreateAsync(CreateShiftLogEntryRequest request, CancellationToken ct);
    Task<ShiftLogEntryDto> ChangeStatusAsync(ChangeShiftLogEntryStatusRequest request, CancellationToken ct);
}
