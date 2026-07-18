using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ASMILE.BO.Application.FrontOffice.Foundation.Contexts;
using ASMILE.BO.Application.FrontOffice.ProcessManager;
using ASMILE.BO.Core.FrontOffice.Foundation.Identifiers;
using ASMILE.BO.Core.FrontOffice.Foundation.Persistence;
using ASMILE.BO.Core.FrontOffice.ProcessManager;
using ASMILE.BO.Infrastructure.Persistence;
using ASMILE.BO.Shared.Foundation;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace ASMILE.BO.Infrastructure.FrontOffice.ProcessManager;

public sealed record ProcessSignalHandlingResult(bool Applied, bool DuplicateNoOp, string OutcomeCode, Guid? OutboxMessageId);

/// <summary>Single mutation boundary for persisted Front Office process state.</summary>
public sealed class ProcessSignalHandler(AppDbContext dbContext, ProcessTransitionEngine transitionEngine, IProcessFaultInjector faultInjector, IClock clock)
{
    public async Task<Result<ProcessSignalHandlingResult>> HandleAsync<TState, TStep, TSignal>(ProcessSignal signal, IProcessDefinition<TState, TStep, TSignal> definition, TSignal typedSignal, CancellationToken cancellationToken = default)
        where TState : struct, Enum where TStep : struct, Enum where TSignal : struct, Enum
    {
        await using IDbContextTransaction transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var now = clock.UtcNow;
        var existing = await dbContext.FrontOfficeProcessSignals.AsNoTracking().SingleOrDefaultAsync(x => x.ProcessId == signal.ProcessId && x.SignalId == signal.SignalId, cancellationToken);
        if (existing is not null)
        {
            if (!string.Equals(existing.PayloadHash, signal.PayloadHash, StringComparison.Ordinal))
                return Result<ProcessSignalHandlingResult>.Failure(Error.Validation("PM_SIGNAL_PAYLOAD_CONFLICT", "The signal id was already received with a different payload hash."));
            return Result<ProcessSignalHandlingResult>.Success(new(false, true, "PM_SIGNAL_DUPLICATE_NOOP", null));
        }

        dbContext.FrontOfficeProcessSignals.Add(signal);
        await faultInjector.CheckpointAsync(ProcessFaultPoint.AfterSignalReserved, cancellationToken);
        await faultInjector.CheckpointAsync(ProcessFaultPoint.BeforeProcessLoad, cancellationToken);
        var process = await dbContext.FrontOfficeProcesses.SingleOrDefaultAsync(x => x.ProcessId == signal.ProcessId, cancellationToken);
        if (process is null) return Result<ProcessSignalHandlingResult>.Failure(Error.Validation("PM_PROCESS_NOT_FOUND", "The process does not exist."));
        await faultInjector.CheckpointAsync(ProcessFaultPoint.AfterProcessLoaded, cancellationToken);

        var transition = transitionEngine.Apply(process, definition, typedSignal, now, signal.SchemaVersion);
        if (transition.IsFailure)
            return Result<ProcessSignalHandlingResult>.Failure(transition.Error!);
        if (transition.Value!.IdempotentNoOp)
        {
            signal.Complete(now);
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return Result<ProcessSignalHandlingResult>.Success(new(false, true, transition.Value.OutcomeCode, null));
        }

        await faultInjector.CheckpointAsync(ProcessFaultPoint.AfterTransitionApplied, cancellationToken);
        await faultInjector.CheckpointAsync(ProcessFaultPoint.BeforeHistoryPersist, cancellationToken);
        dbContext.FrontOfficeProcessHistory.Add(ProcessHistory.Record(process.ProcessId, null, process.CurrentState, null, process.CurrentStep, "SignalApplied", process.CorrelationId, now));
        if (transition.Value.TimeoutDueAtUtc is { } timeoutAt && transition.Value.TimeoutType is { } timeoutType)
            dbContext.FrontOfficeProcessTimeouts.Add(ProcessTimeout.Schedule(process.ProcessId, null, timeoutType, timeoutAt, process.CorrelationId));
        if (transition.Value.Escalation == ProcessEscalation.ManualAction && transition.Value.RequiredPermission is { } permission)
            dbContext.FrontOfficeProcessManualActions.Add(ProcessManualAction.Open(process.ProcessId, "ProcessTransitionReview", permission, now));

        await faultInjector.CheckpointAsync(ProcessFaultPoint.BeforeOutboxPersist, cancellationToken);
        var messageId = MessageId.New();
        var payload = JsonSerializer.Serialize(new { ProcessId = process.ProcessId, State = process.CurrentState, Step = process.CurrentStep, Signal = signal.SignalType, OccurredAtUtc = now });
        dbContext.FrontOfficeOutboxMessages.Add(FrontOfficeOutboxMessage.Create(messageId, "FrontOffice.Process.Transitioned", 1, now, payload, Hash(payload), "ProcessInstance", process.ProcessId.ToString("N"), process.ProcessVersion, process.ProcessId.ToString("N"), now, PropertyId.From(process.PropertyId), process.BusinessDate, CorrelationId.From(process.CorrelationId), null));
        signal.Complete(now);
        await faultInjector.CheckpointAsync(ProcessFaultPoint.BeforeSaveChanges, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        await faultInjector.CheckpointAsync(ProcessFaultPoint.AfterSaveChangesBeforeCommit, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        await faultInjector.CheckpointAsync(ProcessFaultPoint.AfterCommitBeforeAck, cancellationToken);
        return Result<ProcessSignalHandlingResult>.Success(new(true, false, transition.Value.OutcomeCode, messageId.Value));
    }

    private static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
}
