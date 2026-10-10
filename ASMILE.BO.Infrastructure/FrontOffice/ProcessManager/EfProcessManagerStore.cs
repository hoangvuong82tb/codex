using ASMILE.BO.Application.FrontOffice.ProcessManager;
using ASMILE.BO.Core.FrontOffice.ProcessManager;
using ASMILE.BO.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Security.Cryptography;
using System.Text;

namespace ASMILE.BO.Infrastructure.FrontOffice.ProcessManager;

public sealed class EfProcessManagerStore(AppDbContext dbContext) : IProcessRepository, IProcessMonitorQueries
{
    public Task<ProcessInstance?> GetByIdAsync(Guid processId, CancellationToken cancellationToken = default) => dbContext.FrontOfficeProcesses.SingleOrDefaultAsync(x => x.ProcessId == processId, cancellationToken);
    public Task<ProcessInstance?> GetActiveByKeyAsync(string processKey, CancellationToken cancellationToken = default) => dbContext.FrontOfficeProcesses.SingleOrDefaultAsync(x => x.ProcessKey == processKey && x.Status != ProcessStatus.Completed && x.Status != ProcessStatus.Failed && x.Status != ProcessStatus.Cancelled, cancellationToken);
    public Task AddAsync(ProcessInstance process, CancellationToken cancellationToken = default) { dbContext.FrontOfficeProcesses.Add(process); return Task.CompletedTask; }
    public Task AppendHistoryAsync(ProcessHistory history, CancellationToken cancellationToken = default) { dbContext.FrontOfficeProcessHistory.Add(history); return Task.CompletedTask; }
    public async Task<ProcessSignalReservationResult> ReserveSignalAsync(ProcessSignal signal, CancellationToken cancellationToken = default)
    {
        var existing = await dbContext.FrontOfficeProcessSignals.AsNoTracking().SingleOrDefaultAsync(x => x.ProcessId == signal.ProcessId && x.SignalId == signal.SignalId, cancellationToken);
        if (existing is not null) return existing.PayloadHash == signal.PayloadHash ? ProcessSignalReservationResult.DuplicateSamePayload : ProcessSignalReservationResult.DuplicatePayloadMismatch;
        dbContext.FrontOfficeProcessSignals.Add(signal);
        return ProcessSignalReservationResult.Reserved;
    }
    public async Task MarkSignalProcessedAsync(Guid processId, Guid signalId, DateTime nowUtc, CancellationToken cancellationToken = default)
    {
        var signal = await dbContext.FrontOfficeProcessSignals.SingleAsync(x => x.ProcessId == processId && x.SignalId == signalId, cancellationToken);
        signal.Complete(nowUtc);
    }
    public async Task<IReadOnlyList<ProcessTimeout>> ClaimDueTimeoutsAsync(string workerId, DateTime nowUtc, TimeSpan leaseDuration, int batchSize, CancellationToken cancellationToken = default)
    {
        if (!dbContext.Database.IsSqlServer()) return [];
        var leaseUntil = nowUtc.Add(leaseDuration);
        const string sql = """
;WITH due AS
(
    SELECT TOP ({0}) *
    FROM [fo_process].[HenGioQuyTrinh] WITH (UPDLOCK, READPAST, READCOMMITTEDLOCK, ROWLOCK)
    WHERE ([Status] = N'Pending' AND [DueAtUtc] <= {1})
       OR ([Status] = N'Claimed' AND [LeaseExpiresAtUtc] <= {1})
    ORDER BY [DueAtUtc], [TimeoutId]
)
UPDATE due
SET [Status] = N'Claimed', [ClaimedBy] = {2}, [LeaseExpiresAtUtc] = {3}, [AttemptCount] = [AttemptCount] + 1
OUTPUT INSERTED.*;
""";
        // READPAST is only valid under READ COMMITTED or REPEATABLE READ.  The
        // worker can run under an ambient snapshot/read-uncommitted scope, so
        // make the isolation level explicit for this atomic claim operation.
        await using var transaction = await dbContext.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken);
        var claimed = await dbContext.FrontOfficeProcessTimeouts
            .FromSqlRaw(sql, batchSize, nowUtc, workerId, leaseUntil)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return claimed;
    }
    public async Task<bool> MarkTimeoutFiredAsync(Guid timeoutId, string workerId, DateTime nowUtc, CancellationToken cancellationToken = default)
    {
        if (!dbContext.Database.IsSqlServer()) return false;
        var affected = await dbContext.Database.ExecuteSqlInterpolatedAsync($"UPDATE [fo_process].[HenGioQuyTrinh] SET [Status] = {nameof(ProcessTimeoutStatus.Fired)}, [FiredAtUtc] = {nowUtc}, [ClaimedBy] = NULL, [LeaseExpiresAtUtc] = NULL WHERE [TimeoutId] = {timeoutId} AND [Status] = {nameof(ProcessTimeoutStatus.Claimed)} AND [ClaimedBy] = {workerId} AND [LeaseExpiresAtUtc] > {nowUtc}", cancellationToken);
        return affected == 1;
    }
    public async Task<ProcessLeaseRenewalResult> RenewTimeoutLeaseAsync(Guid timeoutId, string workerId, DateTime nowUtc, TimeSpan leaseDuration, byte[]? expectedRowVersion = null, CancellationToken cancellationToken = default)
    {
        if (!dbContext.Database.IsSqlServer()) return new(false, null);
        var affected = expectedRowVersion is null
            ? await dbContext.Database.ExecuteSqlInterpolatedAsync($"UPDATE [fo_process].[HenGioQuyTrinh] SET [LeaseExpiresAtUtc] = {nowUtc.Add(leaseDuration)} WHERE [TimeoutId] = {timeoutId} AND [Status] = {nameof(ProcessTimeoutStatus.Claimed)} AND [ClaimedBy] = {workerId} AND [LeaseExpiresAtUtc] > {nowUtc}", cancellationToken)
            : await dbContext.Database.ExecuteSqlInterpolatedAsync($"UPDATE [fo_process].[HenGioQuyTrinh] SET [LeaseExpiresAtUtc] = {nowUtc.Add(leaseDuration)} WHERE [TimeoutId] = {timeoutId} AND [Status] = {nameof(ProcessTimeoutStatus.Claimed)} AND [ClaimedBy] = {workerId} AND [LeaseExpiresAtUtc] > {nowUtc} AND [RowVersion] = {expectedRowVersion}", cancellationToken);
        if (affected != 1) return new(false, null);
        var rowVersion = await dbContext.FrontOfficeProcessTimeouts.AsNoTracking().Where(x => x.TimeoutId == timeoutId).Select(x => x.RowVersion).SingleAsync(cancellationToken);
        return new(true, rowVersion);
    }
    public async Task<ProcessTimeoutFinalizeResult> FinalizeTimeoutAsync(ProcessTimeout timeout, string workerId, DateTime nowUtc, CancellationToken cancellationToken = default)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var tracked = await dbContext.FrontOfficeProcessTimeouts.SingleOrDefaultAsync(x => x.TimeoutId == timeout.TimeoutId && x.Status == ProcessTimeoutStatus.Claimed && x.ClaimedBy == workerId && x.LeaseExpiresAtUtc > nowUtc, cancellationToken);
        if (tracked is null) return ProcessTimeoutFinalizeResult.LeaseLost;
        var process = await dbContext.FrontOfficeProcesses.SingleAsync(x => x.ProcessId == tracked.ProcessId, cancellationToken);
        if (process.IsTerminal)
        {
            tracked.Cancel(nowUtc);
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return ProcessTimeoutFinalizeResult.ProcessTerminal;
        }
        var existing = await dbContext.FrontOfficeProcessSignals.SingleOrDefaultAsync(x => x.ProcessId == tracked.ProcessId && x.SignalId == tracked.TimeoutId, cancellationToken);
        var hash = Hash(tracked.TimeoutId);
        if (existing is not null && !string.Equals(existing.PayloadHash, hash, StringComparison.Ordinal))
            throw new InvalidOperationException("A timeout signal id was reused with a different payload hash.");
        if (existing is null)
        {
            dbContext.FrontOfficeProcessSignals.Add(ProcessSignal.Reserve(tracked.ProcessId, tracked.TimeoutId, $"Timeout.{tracked.TimeoutType}", hash, tracked.CorrelationId, nowUtc));
            dbContext.FrontOfficeProcessHistory.Add(ProcessHistory.RecordTimeout(tracked.ProcessId, tracked.TimeoutId, "TimeoutRaised", "Timeout", null, null, tracked.CorrelationId, nowUtc));
        }
        tracked.MarkFired(nowUtc);
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return ProcessTimeoutFinalizeResult.Completed;
    }
    public async Task<ProcessTimeoutFailureResult> RecordTimeoutFailureAsync(ProcessTimeout timeout, string workerId, ProcessWorkerFailureKind failureKind, string errorCode, int maxAttempts, DateTime nowUtc, TimeSpan retryDelay, CancellationToken cancellationToken = default)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var tracked = await dbContext.FrontOfficeProcessTimeouts.SingleOrDefaultAsync(x => x.TimeoutId == timeout.TimeoutId && x.Status == ProcessTimeoutStatus.Claimed && x.ClaimedBy == workerId && x.LeaseExpiresAtUtc > nowUtc, cancellationToken);
        if (tracked is null) return ProcessTimeoutFailureResult.LeaseLost;
        var retryable = failureKind == ProcessWorkerFailureKind.Transient && tracked.AttemptCount < maxAttempts;
        if (retryable)
        {
            tracked.ScheduleRetry(errorCode, nowUtc.Add(retryDelay));
            dbContext.FrontOfficeProcessHistory.Add(ProcessHistory.RecordTimeout(tracked.ProcessId, tracked.TimeoutId, "TimeoutRetryScheduled", "Retry", errorCode, failureKind.ToString(), tracked.CorrelationId, nowUtc));
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return ProcessTimeoutFailureResult.RetryScheduled;
        }
        var process = await dbContext.FrontOfficeProcesses.SingleAsync(x => x.ProcessId == tracked.ProcessId, cancellationToken);
        tracked.DeadLetter(nowUtc, errorCode);
        if (!await dbContext.FrontOfficeProcessDeadLetters.AnyAsync(x => x.TimeoutId == tracked.TimeoutId, cancellationToken))
            dbContext.FrontOfficeProcessDeadLetters.Add(ProcessDeadLetter.Create(tracked.TimeoutId, tracked.ProcessId, tracked.TimeoutId, failureKind == ProcessWorkerFailureKind.Unknown ? "Unknown outcome" : "Retry exhausted", errorCode, tracked.AttemptCount, nowUtc, workerId, tracked.CorrelationId, process.BusinessDate));
        var manualExists = await dbContext.FrontOfficeProcessManualActions.AnyAsync(x => x.ProcessId == tracked.ProcessId && x.ActionType == "ProcessTimeoutDeadLetter" && x.Status != ProcessManualActionStatus.Completed && x.Status != ProcessManualActionStatus.Cancelled, cancellationToken);
        if (!manualExists)
            dbContext.FrontOfficeProcessManualActions.Add(ProcessManualAction.Open(tracked.ProcessId, "ProcessTimeoutDeadLetter", "FrontOffice.Process.ManualReview", nowUtc, role: "FrontOfficeSupervisor"));
        dbContext.FrontOfficeProcessHistory.Add(ProcessHistory.RecordTimeout(tracked.ProcessId, tracked.TimeoutId, "TimeoutDeadLetter", "DeadLetter", errorCode, failureKind.ToString(), tracked.CorrelationId, nowUtc));
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return ProcessTimeoutFailureResult.DeadLettered;
    }
    public async Task<ProcessWorkerHealth> GetWorkerHealthAsync(DateTime nowUtc, CancellationToken cancellationToken = default)
    {
        var pending = await dbContext.FrontOfficeProcessTimeouts.CountAsync(x => x.Status == ProcessTimeoutStatus.Pending, cancellationToken);
        var claimed = await dbContext.FrontOfficeProcessTimeouts.CountAsync(x => x.Status == ProcessTimeoutStatus.Claimed && x.LeaseExpiresAtUtc > nowUtc, cancellationToken);
        var deadLetters = await dbContext.FrontOfficeProcessDeadLetters.CountAsync(cancellationToken);
        var status = deadLetters > 0 ? "Degraded" : "Healthy";
        return new ProcessWorkerHealth(status, pending, claimed, deadLetters, deadLetters > 0 ? "Dead letters require manual review." : null);
    }
    public Task SaveChangesAsync(CancellationToken cancellationToken = default) => dbContext.SaveChangesAsync(cancellationToken);
    public Task<ProcessMonitorItem?> GetProcessByIdAsync(Guid processId, CancellationToken cancellationToken = default) => Project().SingleOrDefaultAsync(x => x.ProcessId == processId, cancellationToken);
    public async Task<IReadOnlyList<ProcessMonitorItem>> SearchActiveProcessesAsync(Guid propertyId, int take = 100, CancellationToken cancellationToken = default) => await Project().Where(x => x.PropertyId == propertyId && x.Status != ProcessStatus.Completed && x.Status != ProcessStatus.Failed && x.Status != ProcessStatus.Cancelled).OrderByDescending(x => x.UpdatedAtUtc).Take(take).ToListAsync(cancellationToken);
    public async Task<IReadOnlyList<ProcessMonitorItem>> GetProcessesWaitingManualActionAsync(Guid propertyId, CancellationToken cancellationToken = default) => await Project().Where(x => x.PropertyId == propertyId && x.RequiresManualAction).OrderBy(x => x.ExpiresAtUtc).ToListAsync(cancellationToken);
    public async Task<IReadOnlyList<ProcessMonitorItem>> GetProcessesRequiringReconciliationAsync(Guid propertyId, CancellationToken cancellationToken = default) => await Project().Where(x => x.PropertyId == propertyId && x.Status == ProcessStatus.ReconciliationRequired).ToListAsync(cancellationToken);
    public async Task<IReadOnlyList<ProcessTimeout>> GetDueTimeoutsAsync(DateTime nowUtc, int take = 100, CancellationToken cancellationToken = default) => await dbContext.FrontOfficeProcessTimeouts.AsNoTracking().Where(x => x.Status == ProcessTimeoutStatus.Pending && x.DueAtUtc <= nowUtc).OrderBy(x => x.DueAtUtc).Take(take).ToListAsync(cancellationToken);
    public async Task<IReadOnlyList<ProcessMonitorItem>> GetStuckProcessesAsync(DateTime olderThanUtc, int take = 100, CancellationToken cancellationToken = default) => await Project().Where(x => x.Status != ProcessStatus.Completed && x.Status != ProcessStatus.Failed && x.Status != ProcessStatus.Cancelled && x.UpdatedAtUtc <= olderThanUtc).OrderBy(x => x.UpdatedAtUtc).Take(take).ToListAsync(cancellationToken);
    public async Task<IReadOnlyList<ProcessTimelineItem>> GetProcessTimelineAsync(Guid processId, CancellationToken cancellationToken = default) => await dbContext.FrontOfficeProcessHistory.AsNoTracking().Where(x => x.ProcessId == processId).OrderBy(x => x.OccurredAtUtc).Select(x => new ProcessTimelineItem(x.HistoryId, x.TransitionType, x.PreviousState, x.NewState, x.PreviousStep, x.NewStep, x.OccurredAtUtc, x.ErrorCode, x.ReasonCode)).ToListAsync(cancellationToken);
    private IQueryable<ProcessMonitorItem> Project() => dbContext.FrontOfficeProcesses.AsNoTracking().Select(x => new ProcessMonitorItem(x.ProcessId, x.ProcessType, x.CurrentState, x.CurrentStep, x.Status, x.RootSubjectType, x.RootSubjectId, x.PropertyId, x.BusinessDate, x.StartedAtUtc, x.UpdatedAtUtc, x.ExpiresAtUtc, x.RetryCount, x.LastErrorCode, x.RequiresManualAction, x.CorrelationId));
    private static string Hash(Guid id) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(id.ToString("N"))));
}
