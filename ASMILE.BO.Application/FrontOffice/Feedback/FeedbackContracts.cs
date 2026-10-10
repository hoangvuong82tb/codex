using ASMILE.BO.Core.FrontOffice.Feedback;

namespace ASMILE.BO.Application.FrontOffice.Feedback;

public sealed record GuestFeedbackHistoryDto(
    Guid GuestFeedbackHistoryId,
    string Action,
    DateTime OccurredAtUtc);

public sealed record GuestFeedbackDto(
    Guid GuestFeedbackId,
    Guid PropertyId,
    string FeedbackNumber,
    string? GuestName,
    string? RoomNumber,
    string FeedbackType,
    string Category,
    string Priority,
    FeedbackStatus Status,
    string? Source,
    int Rating,
    string? Subject,
    string Description,
    string? AssignedDepartment,
    string? Resolution,
    DateTime CreatedAtUtc,
    IReadOnlyList<GuestFeedbackHistoryDto> History);

public sealed record GuestFeedbackQueryResult(
    IReadOnlyList<GuestFeedbackDto> Items,
    int TotalNew,
    int TotalResolved,
    double AverageRating,
    int TotalCount);

public sealed record CreateGuestFeedbackCommand(
    Guid PropertyId,
    string FeedbackType,
    string Category,
    string Description,
    string? GuestName,
    string? RoomNumber,
    int Rating,
    string? Source,
    string Priority,
    string? Subject,
    Guid? ActorId);

public interface IGuestFeedbackService
{
    Task<GuestFeedbackQueryResult> SearchAsync(Guid propertyId, string? keyword = null, CancellationToken cancellationToken = default);
    Task<GuestFeedbackDto> CreateAsync(CreateGuestFeedbackCommand command, CancellationToken cancellationToken = default);
    Task AssignAsync(Guid propertyId, Guid feedbackId, string department, CancellationToken cancellationToken = default);
    Task ResolveAsync(Guid propertyId, Guid feedbackId, string resolution, CancellationToken cancellationToken = default);
    Task CloseAsync(Guid propertyId, Guid feedbackId, CancellationToken cancellationToken = default);
}
