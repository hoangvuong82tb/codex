namespace ASMILE.BO.Application.FrontOffice.Message;

public sealed record GuestMessageDto(
    Guid GuestMessageId,
    string MessageNumber,
    Guid? GuestId,
    Guid? ReservationId,
    Guid? StayId,
    string? GuestName,
    string? RoomNumber,
    string MessageType,
    string Channel,
    string Priority,
    string Status,
    string? Subject,
    string Content,
    bool RequiresResponse,
    string? ResponseContent,
    string? AssignedStaff,
    DateTime CreatedAtUtc,
    DateTime? DeliveredAtUtc,
    DateTime? ReadAtUtc,
    DateTime? CompletedAtUtc,
    IReadOnlyList<string> History
);

public sealed record CreateGuestMessageRequest(
    Guid? GuestId,
    Guid? ReservationId,
    Guid? StayId,
    Guid? RoomId,
    string? GuestName,
    string? RoomNumber,
    string MessageType,
    string Channel,
    string Priority,
    string? Subject,
    string Content,
    bool RequiresResponse,
    string? Department
);

public interface IGuestMessageService
{
    Task<IReadOnlyList<GuestMessageDto>> GetAllAsync(Guid propertyId, CancellationToken ct);
    Task<GuestMessageDto> CreateAndSendAsync(CreateGuestMessageRequest request, CancellationToken ct);
    Task<GuestMessageDto> MarkReadAsync(Guid messageId, CancellationToken ct);
    Task<GuestMessageDto> ReplyAsync(Guid messageId, string response, CancellationToken ct);
}
