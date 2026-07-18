using ASMILE.BO.Application.FrontOffice.Foundation.Contexts;
using ASMILE.BO.Application.FrontOffice.Foundation.Persistence;
using ASMILE.BO.Core.FrontOffice.Foundation.Identifiers;
using ASMILE.BO.Core.FrontOffice.Foundation.Persistence;
using ASMILE.BO.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ASMILE.BO.Infrastructure.FrontOffice.Foundation;

public sealed class EfIdempotencyStore(AppDbContext dbContext, IClock clock) : IIdempotencyStore
{
    public async Task<IdempotencyReservationResult> ReserveAsync(string @namespace, IdempotencyKey key, CommandId commandId, string requestHash, DateTime expiresAtUtc, CancellationToken cancellationToken = default)
    {
        var existing = await dbContext.FrontOfficeIdempotencyRecords.SingleOrDefaultAsync(x => x.Namespace == @namespace && x.Key == key, cancellationToken);
        if (existing is null)
        {
            dbContext.FrontOfficeIdempotencyRecords.Add(FrontOfficeIdempotencyRecord.Reserve(@namespace, key, commandId, requestHash, clock.UtcNow, expiresAtUtc));
            await dbContext.SaveChangesAsync(cancellationToken);
            return IdempotencyReservationResult.Reserved;
        }
        if (existing.RequestHash != requestHash) return IdempotencyReservationResult.RequestHashMismatch;
        if (existing.ExpiresAtUtc <= clock.UtcNow) { existing.Expire(); await dbContext.SaveChangesAsync(cancellationToken); return IdempotencyReservationResult.Expired; }
        return existing.Status switch { IdempotencyStatus.Completed => IdempotencyReservationResult.DuplicateCompleted, IdempotencyStatus.FailedRetryable => IdempotencyReservationResult.FailedRetryable, IdempotencyStatus.FailedPermanent => IdempotencyReservationResult.FailedPermanent, _ => IdempotencyReservationResult.InProgress };
    }
    public async Task CompleteAsync(string @namespace, IdempotencyKey key, string resultType, string resultReference, CancellationToken cancellationToken = default)
    {
        var record = await dbContext.FrontOfficeIdempotencyRecords.SingleAsync(x => x.Namespace == @namespace && x.Key == key, cancellationToken);
        record.Complete(resultType, resultReference, clock.UtcNow);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
