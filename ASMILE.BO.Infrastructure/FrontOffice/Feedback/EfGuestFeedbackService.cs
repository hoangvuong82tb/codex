using ASMILE.BO.Application.FrontOffice.Feedback;
using ASMILE.BO.Core.FrontOffice.Feedback;
using ASMILE.BO.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ASMILE.BO.Infrastructure.FrontOffice.Feedback;

public sealed class EfGuestFeedbackService(AppDbContext db) : IGuestFeedbackService
{
    public async Task<GuestFeedbackQueryResult> SearchAsync(Guid propertyId, string? keyword = null, CancellationToken cancellationToken = default)
    {
        RequireProperty(propertyId);
        var query = db.GuestFeedbacks
            .AsNoTracking()
            .Include(x => x.History)
            .Where(x => x.PropertyId == propertyId);

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var value = keyword.Trim();
            query = query.Where(x =>
                x.FeedbackNumber.Contains(value) ||
                (x.GuestName != null && x.GuestName.Contains(value)) ||
                (x.RoomNumber != null && x.RoomNumber.Contains(value)) ||
                x.Description.Contains(value));
        }

        var entities = await query
            .OrderByDescending(x => x.CreatedAtUtc)
            .Take(500)
            .ToListAsync(cancellationToken);

        var totalNew = entities.Count(x => x.Status is FeedbackStatus.New or FeedbackStatus.Acknowledged or FeedbackStatus.Assigned or FeedbackStatus.InProgress or FeedbackStatus.WaitingGuest);
        var totalResolved = entities.Count(x => x.Status is FeedbackStatus.Resolved or FeedbackStatus.Closed);
        var rated = entities.Where(x => x.Rating > 0).Select(x => x.Rating).ToArray();
        return new GuestFeedbackQueryResult(
            entities.Select(ToDto).ToList(),
            totalNew,
            totalResolved,
            rated.Length == 0 ? 0 : rated.Average(),
            entities.Count);
    }

    public async Task<GuestFeedbackDto> CreateAsync(CreateGuestFeedbackCommand command, CancellationToken cancellationToken = default)
    {
        RequireProperty(command.PropertyId);
        if (string.IsNullOrWhiteSpace(command.FeedbackType)) throw new ArgumentException("Feedback type is required.", nameof(command));
        if (string.IsNullOrWhiteSpace(command.Category)) throw new ArgumentException("Feedback category is required.", nameof(command));
        if (string.IsNullOrWhiteSpace(command.Description)) throw new ArgumentException("Feedback description is required.", nameof(command));
        if (command.Rating is < 0 or > 5) throw new ArgumentOutOfRangeException(nameof(command), "Rating must be between 0 and 5.");

        var now = DateTime.UtcNow;
        var number = await GenerateNumberAsync(command.PropertyId, now, cancellationToken);
        var feedback = GuestFeedback.Create(
            command.PropertyId,
            number,
            command.FeedbackType.Trim(),
            command.Category.Trim(),
            command.Description.Trim(),
            NullIfWhiteSpace(command.GuestName),
            NullIfWhiteSpace(command.RoomNumber),
            command.Rating,
            NullIfWhiteSpace(command.Source),
            string.IsNullOrWhiteSpace(command.Priority) ? "Normal" : command.Priority.Trim(),
            false,
            null,
            NullIfWhiteSpace(command.Subject),
            now,
            command.ActorId is { } actor && actor != Guid.Empty ? ASMILE.BO.Core.FrontOffice.Foundation.Identifiers.ActorId.From(actor) : null);

        db.GuestFeedbacks.Add(feedback);
        await db.SaveChangesAsync(cancellationToken);
        return ToDto(feedback);
    }

    public Task AssignAsync(Guid propertyId, Guid feedbackId, string department, CancellationToken cancellationToken = default) =>
        TransitionAsync(propertyId, feedbackId, cancellationToken, feedback =>
        {
            if (string.IsNullOrWhiteSpace(department)) throw new ArgumentException("Department is required.", nameof(department));
            feedback.Assign(department.Trim(), DateTime.UtcNow);
        });

    public Task ResolveAsync(Guid propertyId, Guid feedbackId, string resolution, CancellationToken cancellationToken = default) =>
        TransitionAsync(propertyId, feedbackId, cancellationToken, feedback =>
        {
            if (string.IsNullOrWhiteSpace(resolution)) throw new ArgumentException("Resolution is required.", nameof(resolution));
            feedback.Resolve(resolution.Trim(), DateTime.UtcNow);
        });

    public Task CloseAsync(Guid propertyId, Guid feedbackId, CancellationToken cancellationToken = default) =>
        TransitionAsync(propertyId, feedbackId, cancellationToken, feedback => feedback.Close(DateTime.UtcNow));

    private async Task TransitionAsync(Guid propertyId, Guid feedbackId, CancellationToken cancellationToken, Action<GuestFeedback> transition)
    {
        RequireProperty(propertyId);
        var feedback = await db.GuestFeedbacks
            .Include(x => x.History)
            .SingleOrDefaultAsync(x => x.GuestFeedbackId == feedbackId && x.PropertyId == propertyId, cancellationToken)
            ?? throw new InvalidOperationException("Không tìm thấy phản hồi khách.");
        transition(feedback);
        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task<string> GenerateNumberAsync(Guid propertyId, DateTime now, CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < 5; attempt++)
        {
            var number = $"FB-{now:yyyyMMddHHmmssfff}-{Random.Shared.Next(100, 1000)}";
            if (!await db.GuestFeedbacks.AnyAsync(x => x.PropertyId == propertyId && x.FeedbackNumber == number, cancellationToken)) return number;
        }
        throw new InvalidOperationException("Không thể tạo số phản hồi duy nhất.");
    }

    private static GuestFeedbackDto ToDto(GuestFeedback entity) => new(
        entity.GuestFeedbackId,
        entity.PropertyId,
        entity.FeedbackNumber,
        entity.GuestName,
        entity.RoomNumber,
        entity.FeedbackType,
        entity.Category,
        entity.Priority,
        entity.Status,
        entity.Source,
        entity.Rating,
        entity.Subject,
        entity.Description,
        entity.AssignedDepartment,
        entity.Resolution,
        entity.CreatedAtUtc,
        entity.History.OrderBy(x => x.OccurredAtUtc).Select(x => new GuestFeedbackHistoryDto(x.GuestFeedbackHistoryId, x.Action, x.OccurredAtUtc)).ToList());

    private static string? NullIfWhiteSpace(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static void RequireProperty(Guid propertyId)
    {
        if (propertyId == Guid.Empty) throw new ArgumentException("Property identifier is required.", nameof(propertyId));
    }
}
