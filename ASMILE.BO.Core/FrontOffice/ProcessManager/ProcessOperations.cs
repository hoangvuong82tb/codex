namespace ASMILE.BO.Core.FrontOffice.ProcessManager;

public sealed partial class ProcessTimeout
{
    public bool TryRenewLease(string workerId, DateTime nowUtc, TimeSpan duration)
    {
        if (Status != ProcessTimeoutStatus.Claimed || !string.Equals(ClaimedBy, workerId, StringComparison.Ordinal) || LeaseExpiresAtUtc is null || LeaseExpiresAtUtc <= nowUtc) return false;
        LeaseExpiresAtUtc = nowUtc.Add(duration);
        return true;
    }
    public bool IsLeaseOwnedBy(string workerId, DateTime nowUtc) => Status == ProcessTimeoutStatus.Claimed && string.Equals(ClaimedBy, workerId, StringComparison.Ordinal) && LeaseExpiresAtUtc > nowUtc;
    public void ScheduleRetry(string errorCode, DateTime dueAtUtc) { Status = ProcessTimeoutStatus.Pending; LastErrorCode = errorCode; NextRetryAtUtc = dueAtUtc; DueAtUtc = dueAtUtc; ClaimedBy = null; LeaseExpiresAtUtc = null; }
    public void DeadLetter(DateTime nowUtc, string? errorCode = null) { Status = ProcessTimeoutStatus.DeadLetter; LastErrorCode = errorCode ?? LastErrorCode; DeadLetteredAtUtc = nowUtc; ClaimedBy = null; LeaseExpiresAtUtc = null; }
}

public sealed partial class ProcessCompensation
{
    public void Start(DateTime nowUtc) { if (Status != ProcessCompensationStatus.Requested) throw new InvalidOperationException("Compensation is not requestable."); Status = ProcessCompensationStatus.Running; StartedAtUtc = nowUtc; }
    public void Complete(DateTime nowUtc, string? auditReference = null) { if (Status != ProcessCompensationStatus.Running) throw new InvalidOperationException("Compensation is not running."); Status = ProcessCompensationStatus.Completed; CompletedAtUtc = nowUtc; AuditReference = auditReference; }
    public void Fail(string errorCode, bool requiresManualReview) { Status = requiresManualReview ? ProcessCompensationStatus.ManualReview : ProcessCompensationStatus.Failed; ErrorCode = errorCode; RetryCount++; }
}

public sealed partial class ProcessManualAction
{
    public bool TryClaim(Guid actorId, IReadOnlySet<string> roles)
    {
        if (Status is not (ProcessManualActionStatus.Open or ProcessManualActionStatus.Assigned)) return false;
        if (AssignedActorId.HasValue && AssignedActorId != actorId) return false;
        if (AssignedRole is not null && !roles.Contains(AssignedRole)) return false;
        AssignedActorId = actorId; Status = ProcessManualActionStatus.Assigned; return true;
    }
    public bool TryComplete(Guid actorId, bool hasPermission, string? reasonCode, string resultCode, DateTime nowUtc)
    {
        if (!hasPermission || Status != ProcessManualActionStatus.Assigned || AssignedActorId != actorId || (ReasonRequired && string.IsNullOrWhiteSpace(reasonCode))) return false;
        if (ApprovalRequired && ApprovedByActorId is null) return false;
        Status = ProcessManualActionStatus.Completed; CompletedAtUtc = nowUtc; CompletedByActorId = actorId; ResultCode = resultCode; return true;
    }
    public bool TryApprove(Guid actorId, bool hasPermission, string? reasonCode)
    {
        if (!ApprovalRequired || !hasPermission || actorId == CreatedByActorId || (ReasonRequired && string.IsNullOrWhiteSpace(reasonCode)) || Status is ProcessManualActionStatus.Completed or ProcessManualActionStatus.Cancelled) return false;
        ApprovedByActorId = actorId;
        return true;
    }
    public void Reassign(Guid? actorId, string? role) { if (Status == ProcessManualActionStatus.Completed) throw new InvalidOperationException("A completed manual action cannot be reassigned."); AssignedActorId = actorId; AssignedRole = role; Status = actorId.HasValue ? ProcessManualActionStatus.Assigned : ProcessManualActionStatus.Open; }
}
