namespace ASMILE.BO.Application.FrontOffice.ProcessManager;

public sealed class ProcessWorkerOptions
{
    public TimeSpan LeaseDuration { get; init; } = TimeSpan.FromMinutes(2);
    public TimeSpan HeartbeatInterval { get; init; } = TimeSpan.FromSeconds(20);
    public TimeSpan PollInterval { get; init; } = TimeSpan.FromSeconds(10);
    public int BatchSize { get; init; } = 32;
    public int MaxAttempts { get; init; } = 3;
    public TimeSpan RetryBaseDelay { get; init; } = TimeSpan.FromMinutes(1);
}

public enum ProcessWorkerFailureKind { Transient, Permanent, Unknown, BusinessValidation }

public sealed class ProcessWorkerException(string errorCode, ProcessWorkerFailureKind kind, string message) : Exception(message)
{
    public string ErrorCode { get; } = errorCode;
    public ProcessWorkerFailureKind Kind { get; } = kind;
}

public sealed class ProcessWorkerFailureClassifier
{
    public (ProcessWorkerFailureKind Kind, string ErrorCode) Classify(Exception exception) => exception switch
    {
        ProcessWorkerException known => (known.Kind, known.ErrorCode),
        OperationCanceledException => (ProcessWorkerFailureKind.Transient, "PM_WORKER_CANCELLED"),
        TimeoutException => (ProcessWorkerFailureKind.Transient, "PM_EXTERNAL_TIMEOUT"),
        InvalidOperationException => (ProcessWorkerFailureKind.BusinessValidation, "PM_BUSINESS_VALIDATION"),
        _ => (ProcessWorkerFailureKind.Unknown, "PM_UNKNOWN_FAILURE")
    };
}

public interface IProcessWorkerMetrics
{
    void WorkerStarted(string workerId);
    void WorkerStopped(string workerId);
    void TimeoutClaimed(string workerId, int count);
    void TimeoutCompleted(string workerId);
    void LeaseRenewed(string workerId);
    void LeaseLost(string workerId);
    void RetryScheduled(string workerId);
    void RetryExhausted(string workerId);
    void DeadLetter(string workerId);
    void ManualActionCreated(string workerId);
    void WorkerRestart(string workerId);
    void WorkerRecovery(string workerId);
}

public sealed class NoOpProcessWorkerMetrics : IProcessWorkerMetrics
{
    public void WorkerStarted(string workerId) { } public void WorkerStopped(string workerId) { }
    public void TimeoutClaimed(string workerId, int count) { } public void TimeoutCompleted(string workerId) { }
    public void LeaseRenewed(string workerId) { } public void LeaseLost(string workerId) { }
    public void RetryScheduled(string workerId) { } public void RetryExhausted(string workerId) { }
    public void DeadLetter(string workerId) { } public void ManualActionCreated(string workerId) { }
    public void WorkerRestart(string workerId) { } public void WorkerRecovery(string workerId) { }
}

public sealed record ProcessWorkerHealth(string Status, int PendingTimeoutCount, int ClaimedTimeoutCount, int DeadLetterCount, string? Detail = null);

public interface IProcessTimeoutWorkExecutor
{
    Task ExecuteAsync(Guid processId, Guid timeoutId, string timeoutType, CancellationToken cancellationToken = default);
}

public sealed class DurableProcessTimeoutWorkExecutor : IProcessTimeoutWorkExecutor
{
    public Task ExecuteAsync(Guid processId, Guid timeoutId, string timeoutType, CancellationToken cancellationToken = default) => Task.CompletedTask;
}
