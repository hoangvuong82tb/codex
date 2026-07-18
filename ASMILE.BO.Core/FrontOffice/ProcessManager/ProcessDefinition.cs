namespace ASMILE.BO.Core.FrontOffice.ProcessManager;

public enum ProcessSignalOrderPolicy { RejectEarly, HoldForReplay, IgnoreLate, RequireReconciliation }
public enum ProcessRetryClassification { TransientSafe, TransientIdempotent, PermanentBusiness, PermanentTechnical, UnknownExternalOutcome, ConcurrencyConflict, UnsupportedSchema, ManualReconciliation }
public enum ProcessEscalation { None, DeadLetter, ManualAction, ReconciliationRequired, Fail }

public sealed record ProcessRetryPolicy(int MaxAttempts, TimeSpan InitialDelay, double BackoffFactor, TimeSpan MaxDelay, double JitterRatio, IReadOnlySet<string> RetryableErrorCodes, ProcessEscalation ExhaustedEscalation)
{
    public static ProcessRetryPolicy NoRetry { get; } = new(0, TimeSpan.Zero, 1, TimeSpan.Zero, 0, new HashSet<string>(StringComparer.Ordinal), ProcessEscalation.Fail);
    public bool CanRetry(int attempt, ProcessRetryClassification classification, string? errorCode) => attempt < MaxAttempts && classification is ProcessRetryClassification.TransientSafe or ProcessRetryClassification.TransientIdempotent or ProcessRetryClassification.ConcurrencyConflict && (RetryableErrorCodes.Count == 0 || (errorCode is not null && RetryableErrorCodes.Contains(errorCode)));
    public TimeSpan DelayFor(int attempt, double jitterUnit = 0.5)
    {
        var baseDelay = Math.Min(MaxDelay.TotalMilliseconds, InitialDelay.TotalMilliseconds * Math.Pow(BackoffFactor, Math.Max(0, attempt - 1)));
        var boundedJitter = Math.Clamp(jitterUnit, 0d, 1d) * 2d - 1d;
        return TimeSpan.FromMilliseconds(Math.Max(0, baseDelay * (1d + boundedJitter * Math.Clamp(JitterRatio, 0d, 1d))));
    }
}

public sealed record ProcessTimeoutPolicy(TimeSpan? Timeout, string TimeoutType);
public sealed record ProcessCompensationPolicy(bool Required, bool ApprovalRequired, ProcessEscalation FailureEscalation);
public sealed record ProcessManualActionPolicy(bool Required, string Permission, bool DualControl, TimeSpan? DueAfter);
public sealed record ProcessTransition<TState, TStep, TSignal>(TState From, TSignal Signal, TState To, TStep? NextStep, Func<ProcessTransitionContext<TState, TStep, TSignal>, bool>? Guard = null, ProcessSignalOrderPolicy OrderPolicy = ProcessSignalOrderPolicy.RejectEarly, ProcessTimeoutPolicy? Timeout = null, ProcessRetryPolicy? Retry = null, ProcessCompensationPolicy? Compensation = null, ProcessManualActionPolicy? ManualAction = null) where TState : struct, Enum where TStep : struct, Enum where TSignal : struct, Enum;
public sealed record ProcessTransitionContext<TState, TStep, TSignal>(TState State, TStep? Step, TSignal Signal, int ProcessVersion, int SignalSchemaVersion, DateTime OccurredAtUtc) where TState : struct, Enum where TStep : struct, Enum where TSignal : struct, Enum;

public interface IProcessDefinition<TState, TStep, TSignal> where TState : struct, Enum where TStep : struct, Enum where TSignal : struct, Enum
{
    string ProcessType { get; }
    int Version { get; }
    TState InitialState { get; }
    TStep? InitialStep { get; }
    bool IsTerminal(TState state);
    bool TryGetTransition(TState state, TSignal signal, out ProcessTransition<TState, TStep, TSignal> transition);
}

public sealed class ProcessDefinition<TState, TStep, TSignal>(string processType, int version, TState initialState, TStep? initialStep, IEnumerable<TState> terminalStates, IEnumerable<ProcessTransition<TState, TStep, TSignal>> transitions) : IProcessDefinition<TState, TStep, TSignal> where TState : struct, Enum where TStep : struct, Enum where TSignal : struct, Enum
{
    private readonly HashSet<TState> _terminal = terminalStates.ToHashSet();
    private readonly Dictionary<(TState, TSignal), ProcessTransition<TState, TStep, TSignal>> _transitions = transitions.ToDictionary(x => (x.From, x.Signal));
    public string ProcessType { get; } = processType;
    public int Version { get; } = version;
    public TState InitialState { get; } = initialState;
    public TStep? InitialStep { get; } = initialStep;
    public bool IsTerminal(TState state) => _terminal.Contains(state);
    public bool TryGetTransition(TState state, TSignal signal, out ProcessTransition<TState, TStep, TSignal> transition) => _transitions.TryGetValue((state, signal), out transition!);
}
