using ASMILE.BO.Core.FrontOffice.Foundation.Identifiers;

namespace ASMILE.BO.Core.FrontOffice.ProcessManager;

public enum ProcessStatus { Active, WaitingSignal, WaitingManualAction, ReconciliationRequired, Completed, Failed, Cancelled }
public enum ProcessStepStatus { Pending, Running, WaitingSignal, Completed, Failed, Compensating, Compensated }
public enum ProcessSignalStatus { Reserved, Processed, Rejected, Duplicate, Unsupported }
public enum ProcessTimeoutStatus { Pending, Claimed, Fired, Cancelled, DeadLetter }
public enum ProcessCompensationStatus { Requested, Running, Completed, Failed, ManualReview }
public enum ProcessManualActionStatus { Open, Assigned, Completed, Cancelled }

public sealed class ProcessInstance
{
    private ProcessInstance() { }
    public Guid ProcessId { get; private set; }
    public string ProcessKey { get; private set; } = string.Empty;
    public string ProcessType { get; private set; } = string.Empty;
    public int ProcessVersion { get; private set; }
    public Guid PropertyId { get; private set; }
    public DateOnly BusinessDate { get; private set; }
    public string RootSubjectType { get; private set; } = string.Empty;
    public string RootSubjectId { get; private set; } = string.Empty;
    public ProcessStatus Status { get; private set; }
    public string CurrentState { get; private set; } = string.Empty;
    public string? CurrentStep { get; private set; }
    public DateTime StartedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }
    public DateTime? CompletedAtUtc { get; private set; }
    public DateTime? ExpiresAtUtc { get; private set; }
    public Guid CorrelationId { get; private set; }
    public Guid? CausationId { get; private set; }
    public Guid? InitiatingCommandId { get; private set; }
    public Guid? InitiatedByActorId { get; private set; }
    public int RetryCount { get; private set; }
    public string? LastErrorCode { get; private set; }
    public DateTime? LastErrorAtUtc { get; private set; }
    public bool RequiresManualAction { get; private set; }
    public ProcessCompensationStatus? CompensationStatus { get; private set; }
    public string? ResumeToken { get; private set; }
    public byte[]? RowVersion { get; private set; }

    public static ProcessInstance Start(string processKey, string processType, int processVersion, PropertyId propertyId, DateOnly businessDate, string rootSubjectType, string rootSubjectId, CorrelationId correlationId, DateTime nowUtc, string initialState = "Started", string? initialStep = null, DateTime? expiresAtUtc = null, CommandId? commandId = null, ActorId? actorId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(processKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(processType);
        return new ProcessInstance { ProcessId = Uuid7.NewGuid(), ProcessKey = processKey, ProcessType = processType, ProcessVersion = processVersion, PropertyId = propertyId.Value, BusinessDate = businessDate, RootSubjectType = rootSubjectType, RootSubjectId = rootSubjectId, CorrelationId = correlationId.Value, Status = ProcessStatus.Active, CurrentState = initialState, CurrentStep = initialStep, StartedAtUtc = nowUtc, UpdatedAtUtc = nowUtc, ExpiresAtUtc = expiresAtUtc, InitiatingCommandId = commandId?.Value, InitiatedByActorId = actorId?.Value };
    }
    public bool IsTerminal => Status is ProcessStatus.Completed or ProcessStatus.Failed or ProcessStatus.Cancelled;
    internal void Transition(ProcessStatus status, string state, string? step, DateTime nowUtc, string? errorCode = null)
    {
        if (IsTerminal) throw new InvalidOperationException("A terminal process cannot transition.");
        Status = status; CurrentState = state; CurrentStep = step; UpdatedAtUtc = nowUtc; LastErrorCode = errorCode; LastErrorAtUtc = errorCode is null ? null : nowUtc;
        RequiresManualAction = status is ProcessStatus.WaitingManualAction or ProcessStatus.ReconciliationRequired;
        if (status is ProcessStatus.Completed or ProcessStatus.Failed or ProcessStatus.Cancelled) CompletedAtUtc = nowUtc;
    }
    public void IncrementRetry(string errorCode, DateTime nowUtc) { RetryCount++; LastErrorCode = errorCode; LastErrorAtUtc = nowUtc; UpdatedAtUtc = nowUtc; }
    public void SetCompensation(ProcessCompensationStatus status, DateTime nowUtc) { CompensationStatus = status; UpdatedAtUtc = nowUtc; }
}

public sealed class ProcessStep
{
    private ProcessStep() { }
    public Guid StepId { get; private set; }
    public Guid ProcessId { get; private set; }
    public string StepName { get; private set; } = string.Empty;
    public int StepVersion { get; private set; }
    public int Sequence { get; private set; }
    public ProcessStepStatus Status { get; private set; }
    public DateTime? StartedAtUtc { get; private set; }
    public DateTime? CompletedAtUtc { get; private set; }
    public DateTime? TimeoutAtUtc { get; private set; }
    public int RetryCount { get; private set; }
    public string? LastErrorCode { get; private set; }
    public string? ExpectedSignalType { get; private set; }
    public string? CommandReference { get; private set; }
    public string? ResultReference { get; private set; }
    public bool CompensationRequired { get; private set; }
    public byte[]? RowVersion { get; private set; }
    public static ProcessStep Create(Guid processId, string stepName, int stepVersion, int sequence, string? expectedSignalType = null, DateTime? timeoutAtUtc = null) => new() { StepId = Uuid7.NewGuid(), ProcessId = processId, StepName = stepName, StepVersion = stepVersion, Sequence = sequence, Status = ProcessStepStatus.Pending, ExpectedSignalType = expectedSignalType, TimeoutAtUtc = timeoutAtUtc };
    public void Start(DateTime nowUtc) { Status = ProcessStepStatus.Running; StartedAtUtc = nowUtc; }
    public void Complete(string? resultReference, DateTime nowUtc) { Status = ProcessStepStatus.Completed; ResultReference = resultReference; CompletedAtUtc = nowUtc; }
}

public sealed class ProcessStepAttempt { public Guid AttemptId { get; private set; } = Uuid7.NewGuid(); public Guid ProcessId { get; private set; } public Guid StepId { get; private set; } public int AttemptNumber { get; private set; } public DateTime StartedAtUtc { get; private set; } public DateTime? CompletedAtUtc { get; private set; } public string Outcome { get; private set; } = string.Empty; public string? ErrorCode { get; private set; } public string? RetryClassification { get; private set; } public Guid CorrelationId { get; private set; } public Guid? CommandId { get; private set; } public Guid? MessageId { get; private set; } public string? WorkerId { get; private set; } public long? DurationMilliseconds { get; private set; } public string? PayloadHash { get; private set; } public string? SanitizedMetadata { get; private set; } private ProcessStepAttempt() { } public static ProcessStepAttempt Start(Guid processId, Guid stepId, int number, Guid correlationId, DateTime nowUtc, string? workerId = null) => new() { ProcessId = processId, StepId = stepId, AttemptNumber = number, CorrelationId = correlationId, StartedAtUtc = nowUtc, WorkerId = workerId, Outcome = "Started" }; public void Complete(string outcome, DateTime nowUtc, string? errorCode = null) { Outcome = outcome; ErrorCode = errorCode; CompletedAtUtc = nowUtc; DurationMilliseconds = (long)(nowUtc - StartedAtUtc).TotalMilliseconds; } }
public sealed class ProcessSignal { public Guid SignalId { get; private set; } public Guid ProcessId { get; private set; } public string SignalType { get; private set; } = string.Empty; public Guid? MessageId { get; private set; } public string PayloadHash { get; private set; } = string.Empty; public DateTime ReceivedAtUtc { get; private set; } public DateTime? ProcessedAtUtc { get; private set; } public ProcessSignalStatus Status { get; private set; } public bool IsOutOfOrder { get; private set; } public Guid CorrelationId { get; private set; } public Guid? CausationId { get; private set; } public int SchemaVersion { get; private set; } public string? ErrorCode { get; private set; } private ProcessSignal() { } public static ProcessSignal Reserve(Guid processId, Guid signalId, string type, string hash, Guid correlationId, DateTime nowUtc, int schemaVersion = 1, Guid? messageId = null) => new() { ProcessId = processId, SignalId = signalId, SignalType = type, PayloadHash = hash, CorrelationId = correlationId, ReceivedAtUtc = nowUtc, SchemaVersion = schemaVersion, MessageId = messageId, Status = ProcessSignalStatus.Reserved }; public void Complete(DateTime nowUtc) { Status = ProcessSignalStatus.Processed; ProcessedAtUtc = nowUtc; } public void Reject(ProcessSignalStatus status, string error, DateTime nowUtc) { Status = status; ErrorCode = error; ProcessedAtUtc = nowUtc; } }
public sealed partial class ProcessTimeout { public Guid TimeoutId { get; private set; } = Uuid7.NewGuid(); public Guid ProcessId { get; private set; } public Guid? StepId { get; private set; } public string TimeoutType { get; private set; } = string.Empty; public DateTime DueAtUtc { get; private set; } public ProcessTimeoutStatus Status { get; private set; } = ProcessTimeoutStatus.Pending; public string? ClaimedBy { get; private set; } public DateTime? LeaseExpiresAtUtc { get; private set; } public int AttemptCount { get; private set; } public DateTime? NextRetryAtUtc { get; private set; } public string? LastErrorCode { get; private set; } public DateTime? DeadLetteredAtUtc { get; private set; } public DateTime? FiredAtUtc { get; private set; } public DateTime? CancelledAtUtc { get; private set; } public Guid CorrelationId { get; private set; } public byte[]? RowVersion { get; private set; } private ProcessTimeout() { } public static ProcessTimeout Schedule(Guid processId, Guid? stepId, string type, DateTime dueAtUtc, Guid correlationId) => new() { ProcessId = processId, StepId = stepId, TimeoutType = type, DueAtUtc = dueAtUtc, CorrelationId = correlationId }; public void MarkFired(DateTime nowUtc) { Status = ProcessTimeoutStatus.Fired; FiredAtUtc = nowUtc; ClaimedBy = null; LeaseExpiresAtUtc = null; } public void Cancel(DateTime nowUtc) { Status = ProcessTimeoutStatus.Cancelled; CancelledAtUtc = nowUtc; } }
public sealed class ProcessDeadLetter
{
    private ProcessDeadLetter() { }
    public Guid DeadLetterId { get; private set; } = Uuid7.NewGuid();
    public Guid TimeoutId { get; private set; }
    public Guid ProcessId { get; private set; }
    public Guid SignalId { get; private set; }
    public string Reason { get; private set; } = string.Empty;
    public string ErrorCode { get; private set; } = string.Empty;
    public int RetryCount { get; private set; }
    public DateTime OccurredAtUtc { get; private set; }
    public string WorkerId { get; private set; } = string.Empty;
    public Guid CorrelationId { get; private set; }
    public DateOnly BusinessDate { get; private set; }
    public static ProcessDeadLetter Create(Guid timeoutId, Guid processId, Guid signalId, string reason, string errorCode, int retryCount, DateTime occurredAtUtc, string workerId, Guid correlationId, DateOnly businessDate) => new()
    {
        TimeoutId = timeoutId, ProcessId = processId, SignalId = signalId, Reason = reason, ErrorCode = errorCode,
        RetryCount = retryCount, OccurredAtUtc = occurredAtUtc, WorkerId = workerId, CorrelationId = correlationId, BusinessDate = businessDate
    };
}
public sealed partial class ProcessCompensation { public Guid CompensationId { get; private set; } = Uuid7.NewGuid(); public Guid ProcessId { get; private set; } public Guid? StepId { get; private set; } public string CompensationType { get; private set; } = string.Empty; public ProcessCompensationStatus Status { get; private set; } = ProcessCompensationStatus.Requested; public DateTime RequestedAtUtc { get; private set; } public DateTime? StartedAtUtc { get; private set; } public DateTime? CompletedAtUtc { get; private set; } public string? CommandReference { get; private set; } public string? ErrorCode { get; private set; } public int RetryCount { get; private set; } public string? ApprovalReference { get; private set; } public string? AuditReference { get; private set; } private ProcessCompensation() { } public static ProcessCompensation Request(Guid processId, Guid? stepId, string type, DateTime nowUtc) => new() { ProcessId = processId, StepId = stepId, CompensationType = type, RequestedAtUtc = nowUtc }; }
public sealed partial class ProcessManualAction { public Guid ManualActionId { get; private set; } = Uuid7.NewGuid(); public Guid ProcessId { get; private set; } public string ActionType { get; private set; } = string.Empty; public ProcessManualActionStatus Status { get; private set; } = ProcessManualActionStatus.Open; public string RequiredPermission { get; private set; } = string.Empty; public string? AssignedRole { get; private set; } public Guid? AssignedActorId { get; private set; } public bool ReasonRequired { get; private set; } public bool ApprovalRequired { get; private set; } public Guid? CreatedByActorId { get; private set; } public Guid? ApprovedByActorId { get; private set; } public DateTime CreatedAtUtc { get; private set; } public DateTime? DueAtUtc { get; private set; } public DateTime? CompletedAtUtc { get; private set; } public Guid? CompletedByActorId { get; private set; } public string? ResultCode { get; private set; } public string? AuditReference { get; private set; } private ProcessManualAction() { } public static ProcessManualAction Open(Guid processId, string type, string permission, DateTime nowUtc, string? role = null, DateTime? dueAtUtc = null, bool reasonRequired = true, bool approvalRequired = false, Guid? createdByActorId = null) => new() { ProcessId = processId, ActionType = type, RequiredPermission = permission, CreatedAtUtc = nowUtc, AssignedRole = role, DueAtUtc = dueAtUtc, ReasonRequired = reasonRequired, ApprovalRequired = approvalRequired, CreatedByActorId = createdByActorId }; }
public sealed class ProcessHistory { public Guid HistoryId { get; private set; } = Uuid7.NewGuid(); public Guid ProcessId { get; private set; } public string? PreviousState { get; private set; } public string NewState { get; private set; } = string.Empty; public string? PreviousStep { get; private set; } public string? NewStep { get; private set; } public string TransitionType { get; private set; } = string.Empty; public Guid? SignalId { get; private set; } public Guid? CommandId { get; private set; } public Guid? EventId { get; private set; } public Guid? ActorId { get; private set; } public DateTime OccurredAtUtc { get; private set; } public string? ErrorCode { get; private set; } public string? ReasonCode { get; private set; } public Guid CorrelationId { get; private set; } private ProcessHistory() { } public static ProcessHistory Record(Guid processId, string? previousState, string state, string? previousStep, string? step, string transition, Guid correlationId, DateTime nowUtc) => new() { ProcessId = processId, PreviousState = previousState, NewState = state, PreviousStep = previousStep, NewStep = step, TransitionType = transition, CorrelationId = correlationId, OccurredAtUtc = nowUtc }; public static ProcessHistory RecordTimeout(Guid processId, Guid signalId, string state, string transition, string? errorCode, string? reasonCode, Guid correlationId, DateTime nowUtc) => new() { ProcessId = processId, SignalId = signalId, NewState = state, TransitionType = transition, ErrorCode = errorCode, ReasonCode = reasonCode, CorrelationId = correlationId, OccurredAtUtc = nowUtc }; }
