using ASMILE.BO.Core.FrontOffice.ProcessManager;
using ASMILE.BO.Shared.Foundation;

namespace ASMILE.BO.Application.FrontOffice.ProcessManager;

public sealed record ProcessTransitionOutcome(bool Applied, bool IdempotentNoOp, string OutcomeCode, string? TimeoutType, DateTime? TimeoutDueAtUtc, ProcessEscalation Escalation, string? RequiredPermission);

public sealed class ProcessTransitionEngine
{
    public Result<ProcessTransitionOutcome> Apply<TState, TStep, TSignal>(ProcessInstance process, IProcessDefinition<TState, TStep, TSignal> definition, TSignal signal, DateTime nowUtc, int signalSchemaVersion = 1)
        where TState : struct, Enum where TStep : struct, Enum where TSignal : struct, Enum
    {
        if (!string.Equals(process.ProcessType, definition.ProcessType, StringComparison.Ordinal) || process.ProcessVersion != definition.Version)
            return Result<ProcessTransitionOutcome>.Failure(Error.Validation("PM_DEFINITION_MISMATCH", "The process definition does not match the persisted process."));
        if (!Enum.TryParse<TState>(process.CurrentState, out var currentState))
            return Result<ProcessTransitionOutcome>.Failure(Error.Validation("PM_STATE_DEFINITION_MISMATCH", "The persisted state is not valid for the definition."));
        if (definition.IsTerminal(currentState) || process.IsTerminal)
            return Result<ProcessTransitionOutcome>.Success(new(false, true, "PM_TERMINAL_SIGNAL_NOOP", null, null, ProcessEscalation.None, null));
        if (!definition.TryGetTransition(currentState, signal, out var transition))
            return Result<ProcessTransitionOutcome>.Failure(Error.Business("PM_UNEXPECTED_OR_OUT_OF_ORDER_SIGNAL", "The signal is not valid in the current process state."));
        var context = new ProcessTransitionContext<TState, TStep, TSignal>(currentState, ParseStep<TStep>(process.CurrentStep), signal, process.ProcessVersion, signalSchemaVersion, nowUtc);
        if (transition.Guard is not null && !transition.Guard(context))
            return Result<ProcessTransitionOutcome>.Failure(Error.Business("PM_TRANSITION_GUARD_REJECTED", "The transition guard rejected the signal."));
        var nextStep = transition.NextStep?.ToString();
        var status = definition.IsTerminal(transition.To) ? ProcessStatus.Completed : transition.ManualAction?.Required == true ? ProcessStatus.WaitingManualAction : ProcessStatus.Active;
        process.Transition(status, transition.To.ToString(), nextStep, nowUtc);
        DateTime? dueAt = transition.Timeout?.Timeout is { } timeout ? nowUtc.Add(timeout) : null;
        return Result<ProcessTransitionOutcome>.Success(new(true, false, "PM_TRANSITION_APPLIED", transition.Timeout?.TimeoutType, dueAt, transition.ManualAction?.Required == true ? ProcessEscalation.ManualAction : ProcessEscalation.None, transition.ManualAction?.Permission));
    }
    private static TStep? ParseStep<TStep>(string? value) where TStep : struct, Enum => Enum.TryParse<TStep>(value, out var step) ? step : null;
}
