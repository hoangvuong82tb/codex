using ASMILE.BO.Core.FrontOffice.LostFound;

namespace ASMILE.BO.Application.FrontOffice.Housekeeping;

public sealed record LostFoundHistoryReadDto(
    Guid LostFoundHistoryId,
    string Action,
    DateTime OccurredAtUtc);

public sealed record LostFoundItemReadDto(
    Guid LostFoundItemId,
    string ItemNumber,
    string ItemName,
    LostFoundStatus Status,
    string Category,
    string? Description,
    string? Color,
    string? Brand,
    string? SerialNumber,
    string? FoundLocation,
    DateTime FoundDateTime,
    string? FoundByStaff,
    string? StoredLocation,
    string? OwnerGuestName,
    Guid? OwnerGuestId,
    Guid? ReservationId,
    Guid? HotelStayId,
    DateTime? ClaimedDate,
    DateTime? ContactDate,
    string? ContactedBy,
    string? ContactMethod,
    string? ContactResult,
    string? ContactNotes,
    string? ReleasedBy,
    string? ReleasedTo,
    DateTime? DisposalDate,
    string? PhotoPath,
    string? Notes,
    DateTime CreatedAtUtc,
    IReadOnlyList<LostFoundHistoryReadDto> History);

public sealed record LostFoundQueryResult(
    IReadOnlyList<LostFoundItemReadDto> Items,
    LostFoundDashboardDto Dashboard,
    int TotalCount);

public sealed record StoreLostFoundItemCommand(
    Guid ItemId,
    Guid PropertyId,
    string Location,
    DateTime OccurredAtUtc,
    Guid? ActorId);

public sealed record RegisterLostFoundItemCommand(
    Guid PropertyId,
    string ItemNumber,
    string ItemName,
    LostFoundCategory Category,
    string? Description,
    string? FoundLocation,
    DateTime FoundDateTime,
    string? FoundByStaff,
    string? StoredLocation,
    string? Color,
    string? Brand,
    string? SerialNumber,
    string? Notes,
    Guid? ActorId);

public sealed record RegisterLostFoundItemResult(Guid ItemId, string ItemNumber);

public sealed record UpdateLostFoundItemCommand(
    Guid ItemId,
    Guid PropertyId,
    string ItemName,
    LostFoundCategory Category,
    string? Description,
    string? Color,
    string? Brand,
    string? SerialNumber,
    string? Notes,
    Guid? ActorId);

public sealed record ContactLostFoundItemCommand(
    Guid ItemId,
    Guid PropertyId,
    string ContactedBy,
    DateTime ContactDate,
    string ContactMethod,
    string ContactResult,
    string? ContactNotes,
    Guid? ActorId);

public sealed record DeleteLostFoundItemCommand(
    Guid ItemId,
    Guid PropertyId,
    Guid? ActorId);

public sealed record ReturnLostFoundItemCommand(
    Guid ItemId,
    Guid PropertyId,
    string ReleasedBy,
    string ReleasedTo,
    DateTime OccurredAtUtc,
    Guid? ActorId);

public sealed record DisposeLostFoundItemCommand(
    Guid ItemId,
    Guid PropertyId,
    string Reason,
    DateTime OccurredAtUtc,
    Guid? ActorId);

public interface ILostFoundQueryService
{
    Task<LostFoundQueryResult> SearchAsync(
        LostFoundSearchFilter filter,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
}

public interface ILostFoundCommandService
{
    Task<RegisterLostFoundItemResult> RegisterAsync(RegisterLostFoundItemCommand command, CancellationToken cancellationToken = default);
    Task UpdateAsync(UpdateLostFoundItemCommand command, CancellationToken cancellationToken = default);
    Task ContactAsync(ContactLostFoundItemCommand command, CancellationToken cancellationToken = default);
    Task DeleteAsync(DeleteLostFoundItemCommand command, CancellationToken cancellationToken = default);
    Task StoreAsync(StoreLostFoundItemCommand command, CancellationToken cancellationToken = default);
    Task ReturnAsync(ReturnLostFoundItemCommand command, CancellationToken cancellationToken = default);
    Task DisposeAsync(DisposeLostFoundItemCommand command, CancellationToken cancellationToken = default);
}
