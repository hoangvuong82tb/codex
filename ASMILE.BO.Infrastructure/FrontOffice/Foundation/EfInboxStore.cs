using ASMILE.BO.Application.FrontOffice.Foundation.Contexts;
using ASMILE.BO.Application.FrontOffice.Foundation.Persistence;
using ASMILE.BO.Core.FrontOffice.Foundation.Identifiers;
using ASMILE.BO.Core.FrontOffice.Foundation.Persistence;
using ASMILE.BO.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ASMILE.BO.Infrastructure.FrontOffice.Foundation;

public sealed class EfInboxStore(AppDbContext dbContext, IClock clock) : IInboxStore
{
    public async Task<InboxReservationResult> ReserveAsync(string consumerName, MessageId messageId, string payloadHash, CancellationToken cancellationToken = default)
    {
        var existing = await dbContext.FrontOfficeInboxMessages.SingleOrDefaultAsync(x => x.ConsumerName == consumerName && x.MessageId == messageId, cancellationToken);
        if (existing is not null) return existing.PayloadHash != payloadHash ? InboxReservationResult.PayloadHashMismatch : existing.Status == InboxStatus.Completed ? InboxReservationResult.DuplicateCompleted : InboxReservationResult.InProgress;
        dbContext.FrontOfficeInboxMessages.Add(FrontOfficeInboxMessage.Reserve(consumerName, messageId, payloadHash, clock.UtcNow));
        await dbContext.SaveChangesAsync(cancellationToken);
        return InboxReservationResult.Reserved;
    }
    public async Task CompleteAsync(string consumerName, MessageId messageId, CancellationToken cancellationToken = default)
    {
        var record = await dbContext.FrontOfficeInboxMessages.SingleAsync(x => x.ConsumerName == consumerName && x.MessageId == messageId, cancellationToken);
        record.Complete(clock.UtcNow);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
