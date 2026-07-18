using ASMILE.BO.Application.FrontOffice.Foundation.Audit;
using ASMILE.BO.Application.FrontOffice.Foundation.Contexts;
using ASMILE.BO.Application.FrontOffice.Foundation.Events;
using ASMILE.BO.Application.FrontOffice.Foundation.Persistence;
using ASMILE.BO.Core.FrontOffice.Foundation.DomainEvents;
using ASMILE.BO.Shared.Exceptions;
using ASMILE.BO.Shared.Foundation;
using ASMILE.BO.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace ASMILE.BO.Infrastructure.FrontOffice.Foundation;

public sealed class EfFrontOfficePersistenceSession(AppDbContext dbContext, IFrontOfficeExecutionContextAccessor contextAccessor, IIntegrationEventMapper mapper, IOutboxMessageFactory outboxFactory, IFrontOfficeAuditWriter auditWriter, IClock clock) : IFrontOfficePersistenceSession
{
    private readonly List<IFrontOfficeDomainEvent> _events = [];
    public void StageDomainEvent(IFrontOfficeDomainEvent domainEvent) => _events.Add(domainEvent);
    public void StageAudit(FrontOfficeAuditIntent intent)
    {
        var context = contextAccessor.Current ?? throw new InvalidOperationException("A trusted Front Office execution context is required before audit can be staged.");
        auditWriter.Stage(intent, context);
    }
    public async Task<Result<int>> CommitAsync(CancellationToken cancellationToken = default)
    {
        var context = contextAccessor.Current;
        if (context is null) return Result<int>.Failure(Error.Validation("FO_CONTEXT_REQUIRED", "A trusted Front Office execution context is required."));
        await using IDbContextTransaction? transaction = dbContext.Database.IsRelational() && dbContext.Database.CurrentTransaction is null ? await dbContext.Database.BeginTransactionAsync(cancellationToken) : null;
        try
        {
            foreach (var domainEvent in _events) dbContext.FrontOfficeOutboxMessages.Add(outboxFactory.Create(domainEvent, mapper.Map(domainEvent), context));
            if (auditWriter is FrontOfficeAuditWriter writer) dbContext.AuditLogs.AddRange(writer.Drain(clock.UtcNow));
            var saved = await dbContext.SaveChangesAsync(cancellationToken);
            if (transaction is not null) await transaction.CommitAsync(cancellationToken);
            _events.Clear();
            return Result<int>.Success(saved);
        }
        catch (AppConcurrencyException)
        {
            if (transaction is not null) await transaction.RollbackAsync(cancellationToken);
            _events.Clear();
            dbContext.ChangeTracker.Clear();
            return Result<int>.Failure(Error.Concurrency("FO_CONCURRENCY_CONFLICT", "The record changed before the operation could be saved."));
        }
        catch
        {
            if (transaction is not null) await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }
}
