using ASMILE.BO.Application.FrontOffice.Foundation.Audit;
using ASMILE.BO.Core.FrontOffice.Foundation.DomainEvents;
using ASMILE.BO.Core.FrontOffice.Foundation.Identifiers;
using ASMILE.BO.Shared.Foundation;

namespace ASMILE.BO.Application.FrontOffice.Foundation.Persistence;

public enum InboxReservationResult { Reserved, DuplicateCompleted, InProgress, PayloadHashMismatch }
public enum IdempotencyReservationResult { Reserved, DuplicateCompleted, InProgress, RequestHashMismatch, FailedRetryable, FailedPermanent, Expired }
public interface IFrontOfficePersistenceSession
{
    void StageDomainEvent(IFrontOfficeDomainEvent domainEvent);
    void StageAudit(FrontOfficeAuditIntent intent);
    Task<Result<int>> CommitAsync(CancellationToken cancellationToken = default);
}
public interface IInboxStore
{
    Task<InboxReservationResult> ReserveAsync(string consumerName, MessageId messageId, string payloadHash, CancellationToken cancellationToken = default);
    Task CompleteAsync(string consumerName, MessageId messageId, CancellationToken cancellationToken = default);
}
public interface IIdempotencyStore
{
    Task<IdempotencyReservationResult> ReserveAsync(string @namespace, IdempotencyKey key, CommandId commandId, string requestHash, DateTime expiresAtUtc, CancellationToken cancellationToken = default);
    Task CompleteAsync(string @namespace, IdempotencyKey key, string resultType, string resultReference, CancellationToken cancellationToken = default);
}
