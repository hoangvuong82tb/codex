using ASMILE.BO.Core.FrontOffice.ProcessManager;

namespace ASMILE.BO.Application.FrontOffice.ProcessManager;

public sealed record ProcessRetryDecision(bool RetryScheduled, DateTime? DueAtUtc, ProcessEscalation Escalation);
public sealed class ProcessRetryEngine
{
    public ProcessRetryDecision Apply(ProcessTimeout timeout, ProcessRetryPolicy policy, ProcessRetryClassification classification, string errorCode, DateTime nowUtc, double jitterUnit = 0.5)
    {
        if (policy.CanRetry(timeout.AttemptCount, classification, errorCode))
        {
            var dueAt = nowUtc.Add(policy.DelayFor(timeout.AttemptCount, jitterUnit));
            timeout.ScheduleRetry(errorCode, dueAt);
            return new(true, dueAt, ProcessEscalation.None);
        }
        if (policy.ExhaustedEscalation == ProcessEscalation.DeadLetter) timeout.DeadLetter(nowUtc, errorCode);
        return new(false, null, policy.ExhaustedEscalation);
    }
}
