using ASMILE.BO.Application.FrontOffice.Foundation.Audit;
using ASMILE.BO.Application.FrontOffice.Foundation.Contexts;
using ASMILE.BO.Application.FrontOffice.Foundation.Persistence;
using ASMILE.BO.Application.FrontOffice.Message;
using ASMILE.BO.Core.FrontOffice.Foundation.Identifiers;
using ASMILE.BO.Core.FrontOffice.Message;
using ASMILE.BO.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ASMILE.BO.Infrastructure.FrontOffice.Message;

public sealed class EfGuestMessageService(
    AppDbContext db,
    IFrontOfficePersistenceSession persistence,
    IFrontOfficeExecutionContextAccessor contextAccessor,
    IClock clock) : IGuestMessageService
{
    public async Task<IReadOnlyList<GuestMessageDto>> GetAllAsync(Guid propertyId, CancellationToken ct)
    {
        var context = RequireContext();
        EnsureProperty(propertyId, context);

        var rows = await db.FrontOfficeGuestMessages
            .AsNoTracking()
            .Where(x => x.PropertyId == propertyId)
            .OrderByDescending(x => x.CreatedAtUtc)
            .Take(5000)
            .ToListAsync(ct);

        var ids = rows.Select(x => x.GuestMessageId).ToArray();
        var history = await db.FrontOfficeMessageHistories
            .AsNoTracking()
            .Where(x => ids.Contains(x.GuestMessageId))
            .OrderBy(x => x.OccurredAtUtc)
            .ToListAsync(ct);
        var historyByMessage = history.GroupBy(x => x.GuestMessageId)
            .ToDictionary(x => x.Key, x => (IReadOnlyList<string>)x.Select(h => h.Action).ToList());

        return rows.Select(x => Map(x, historyByMessage.GetValueOrDefault(x.GuestMessageId, []))).ToList();
    }

    public async Task<GuestMessageDto> CreateAndSendAsync(CreateGuestMessageRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (string.IsNullOrWhiteSpace(request.Content))
            throw new InvalidOperationException("GUEST_MESSAGE_CONTENT_REQUIRED");

        var context = RequireContext();
        var propertyId = context.Property.PropertyId.Value;
        var now = clock.UtcNow;
        var message = GuestMessage.Create(
            propertyId,
            await NextNumberAsync(propertyId, now, ct),
            request.MessageType,
            request.Content,
            request.GuestName,
            request.RoomNumber,
            request.Subject,
            string.IsNullOrWhiteSpace(request.Channel) ? "FrontDesk" : request.Channel,
            string.IsNullOrWhiteSpace(request.Priority) ? "Normal" : request.Priority,
            request.RequiresResponse,
            request.Department,
            now,
            context.Actor.ActorId);

        message.Send(now);
        db.FrontOfficeGuestMessages.Add(message);
        AddPendingHistory(message);
        StageAudit("GuestMessageCreated", message, propertyId);
        await CommitAsync(ct, "GUEST_MESSAGE_CREATE_FAILED");
        return Map(message, message.History.Select(x => x.Action).ToList());
    }

    public Task<GuestMessageDto> MarkReadAsync(Guid messageId, CancellationToken ct)
        => MutateAsync(messageId, (message, now) => message.MarkRead(now), "GuestMessageRead", "GUEST_MESSAGE_READ_FAILED", ct);

    public async Task<GuestMessageDto> ReplyAsync(Guid messageId, string response, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(response))
            throw new InvalidOperationException("GUEST_MESSAGE_REPLY_REQUIRED");
        return await MutateAsync(messageId, (message, now) => message.Reply(response.Trim(), now), "GuestMessageReplied", "GUEST_MESSAGE_REPLY_FAILED", ct);
    }

    private async Task<GuestMessageDto> MutateAsync(Guid messageId, Action<GuestMessage, DateTime> mutation, string auditAction, string errorCode, CancellationToken ct)
    {
        var context = RequireContext();
        var propertyId = context.Property.PropertyId.Value;
        var message = await db.FrontOfficeGuestMessages
            .SingleOrDefaultAsync(x => x.PropertyId == propertyId && x.GuestMessageId == messageId, ct)
            ?? throw new KeyNotFoundException("GUEST_MESSAGE_NOT_FOUND");

        mutation(message, clock.UtcNow);
        AddPendingHistory(message);
        StageAudit(auditAction, message, propertyId);
        await CommitAsync(ct, errorCode);
        return Map(message, message.History.Select(x => x.Action).ToList());
    }

    private async Task<string> NextNumberAsync(Guid propertyId, DateTime nowUtc, CancellationToken ct)
    {
        for (var attempt = 0; attempt < 5; attempt++)
        {
            var candidate = $"MSG-{nowUtc:yyyyMMdd}-{Uuid7.NewGuid():N}"[..25].ToUpperInvariant();
            if (!await db.FrontOfficeGuestMessages.AnyAsync(x => x.PropertyId == propertyId && x.MessageNumber == candidate, ct))
                return candidate;
        }

        throw new InvalidOperationException("GUEST_MESSAGE_NUMBER_GENERATION_FAILED");
    }

    private void AddPendingHistory(GuestMessage message)
    {
        var history = message.History.LastOrDefault();
        if (history is not null && db.Entry(history).State == EntityState.Detached)
            db.FrontOfficeMessageHistories.Add(history);
    }

    private void StageAudit(string action, GuestMessage message, Guid propertyId)
        => persistence.StageAudit(new FrontOfficeAuditIntent(
            action,
            nameof(GuestMessage),
            message.GuestMessageId.ToString("D"),
            "Succeeded",
            Metadata: new Dictionary<string, string>
            {
                ["PropertyId"] = propertyId.ToString("D"),
                ["MessageNumber"] = message.MessageNumber,
                ["Status"] = message.Status.ToString()
            }));

    private async Task CommitAsync(CancellationToken ct, string errorCode)
    {
        var result = await persistence.CommitAsync(ct);
        if (result.IsFailure)
            throw new InvalidOperationException(result.Error?.Code ?? errorCode);
    }

    private FrontOfficeExecutionContext RequireContext()
        => contextAccessor.Current ?? throw new InvalidOperationException("FRONT_OFFICE_CONTEXT_REQUIRED");

    private static void EnsureProperty(Guid propertyId, FrontOfficeExecutionContext context)
    {
        if (propertyId == Guid.Empty || propertyId != context.Property.PropertyId.Value)
            throw new InvalidOperationException("PROPERTY_CONTEXT_MISMATCH");
    }

    private static GuestMessageDto Map(GuestMessage message, IReadOnlyList<string> history)
        => new(
            message.GuestMessageId,
            message.MessageNumber,
            message.GuestId,
            message.ReservationId,
            message.HotelStayId,
            message.GuestName,
            message.RoomNumber,
            message.MessageType,
            message.Channel,
            message.Priority,
            message.Status.ToString(),
            message.Subject,
            message.Content,
            message.RequiresResponse,
            message.ResponseContent,
            message.AssignedStaff,
            message.CreatedAtUtc,
            message.DeliveredAtUtc,
            message.ReadAtUtc,
            message.CompletedAtUtc,
            history);
}
