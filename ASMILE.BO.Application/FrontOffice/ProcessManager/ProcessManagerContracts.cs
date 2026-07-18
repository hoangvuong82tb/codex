using ASMILE.BO.Core.FrontOffice.ProcessManager;

namespace ASMILE.BO.Application.FrontOffice.ProcessManager;

public sealed record ProcessMonitorItem(Guid ProcessId, string ProcessType, string CurrentState, string? CurrentStep, ProcessStatus Status, string RootSubjectType, string RootSubjectId, Guid PropertyId, DateOnly BusinessDate, DateTime StartedAtUtc, DateTime UpdatedAtUtc, DateTime? ExpiresAtUtc, int RetryCount, string? LastErrorCode, bool RequiresManualAction, Guid CorrelationId);
public sealed record ProcessTimelineItem(Guid HistoryId, string TransitionType, string? PreviousState, string NewState, string? PreviousStep, string? NewStep, DateTime OccurredAtUtc, string? ErrorCode, string? ReasonCode);
public enum ProcessSignalReservationResult { Reserved, DuplicateSamePayload, DuplicatePayloadMismatch }
public enum ProcessTimeoutFinalizeResult { Completed, LeaseLost, ProcessTerminal }
public enum ProcessTimeoutFailureResult { RetryScheduled, DeadLettered, LeaseLost }
public sealed record ProcessLeaseRenewalResult(bool Renewed, byte[]? RowVersion);

public interface IProcessRepository
{
    Task<ProcessInstance?> GetByIdAsync(Guid processId, CancellationToken cancellationToken = default);
    Task<ProcessInstance?> GetActiveByKeyAsync(string processKey, CancellationToken cancellationToken = default);
    Task AddAsync(ProcessInstance process, CancellationToken cancellationToken = default);
    Task AppendHistoryAsync(ProcessHistory history, CancellationToken cancellationToken = default);
    Task<ProcessSignalReservationResult> ReserveSignalAsync(ProcessSignal signal, CancellationToken cancellationToken = default);
    Task MarkSignalProcessedAsync(Guid processId, Guid signalId, DateTime nowUtc, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProcessTimeout>> ClaimDueTimeoutsAsync(string workerId, DateTime nowUtc, TimeSpan leaseDuration, int batchSize, CancellationToken cancellationToken = default);
    Task<bool> MarkTimeoutFiredAsync(Guid timeoutId, string workerId, DateTime nowUtc, CancellationToken cancellationToken = default);
    Task<ProcessLeaseRenewalResult> RenewTimeoutLeaseAsync(Guid timeoutId, string workerId, DateTime nowUtc, TimeSpan leaseDuration, byte[]? expectedRowVersion = null, CancellationToken cancellationToken = default);
    Task<ProcessTimeoutFinalizeResult> FinalizeTimeoutAsync(ProcessTimeout timeout, string workerId, DateTime nowUtc, CancellationToken cancellationToken = default);
    Task<ProcessTimeoutFailureResult> RecordTimeoutFailureAsync(ProcessTimeout timeout, string workerId, ProcessWorkerFailureKind failureKind, string errorCode, int maxAttempts, DateTime nowUtc, TimeSpan retryDelay, CancellationToken cancellationToken = default);
    Task<ProcessWorkerHealth> GetWorkerHealthAsync(DateTime nowUtc, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}

public interface IProcessMonitorQueries
{
    Task<ProcessMonitorItem?> GetProcessByIdAsync(Guid processId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProcessMonitorItem>> SearchActiveProcessesAsync(Guid propertyId, int take = 100, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProcessMonitorItem>> GetProcessesWaitingManualActionAsync(Guid propertyId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProcessMonitorItem>> GetProcessesRequiringReconciliationAsync(Guid propertyId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProcessTimeout>> GetDueTimeoutsAsync(DateTime nowUtc, int take = 100, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProcessMonitorItem>> GetStuckProcessesAsync(DateTime olderThanUtc, int take = 100, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProcessTimelineItem>> GetProcessTimelineAsync(Guid processId, CancellationToken cancellationToken = default);
}
