using ASMILE.BO.Core.FrontOffice.Foundation.Identifiers;

namespace ASMILE.BO.Core.FrontOffice.NightAudit;

/// <summary>Night Audit pipeline steps — each step completes before the next begins.</summary>
public enum NightAuditStep
{
    LockBusinessDate = 1,
    ValidateOpenStays = 2,
    ValidateOpenFolios = 3,
    PostRoomCharges = 4,
    PostTaxes = 5,
    PostServiceCharges = 6,
    ApplyPackages = 7,
    SettlementValidation = 8,
    UpdateExpectedDepartures = 9,
    GenerateRevenueSnapshot = 10,
    RollBusinessDate = 11,
    FinalizeAndUnlock = 12
}

public enum NightAuditStatus { Pending, Running, Completed, Failed, Cancelled }
public enum NightAuditStepStatus { Pending, Completed, Failed, Skipped }
public enum BusinessDateStatus { Open, Closing, Closed, Archived }

/// <summary>Validation result for a single check.</summary>
public sealed record AuditValidationResult(bool Passed, string Rule, string? Message, string Severity);

/// <summary>Summary of a Night Audit run.</summary>
public sealed record NightAuditSummary(
    Guid NightAuditId, string AuditNumber, DateOnly BusinessDate, NightAuditStatus Status,
    int TotalSteps, int CompletedSteps, int FailedSteps,
    int RoomsCharged, decimal TotalRoomRevenue, decimal TotalTaxPosted,
    decimal TotalServiceCharge, int FoliosClosed, int StaysUpdated,
    int BusinessRuleViolations, string? ErrorSummary
);

public sealed class NightAudit
{
    private readonly List<NightAuditStepExecution> _steps = [];
    private readonly List<AuditValidationResult> _validations = [];
    private NightAudit() { }

    public Guid NightAuditId { get; private set; }
    public Guid PropertyId { get; private set; }
    public string AuditNumber { get; private set; } = string.Empty;
    public DateOnly BusinessDate { get; private set; }
    public NightAuditStatus Status { get; private set; }
    public NightAuditStep CurrentStep { get; private set; }
    public string? ErrorMessage { get; private set; }
    public DateTime StartedAtUtc { get; private set; }
    public DateTime? CompletedAtUtc { get; private set; }
    public Guid? StartedBy { get; private set; }

    // Metrics collected during audit
    public int RoomsCharged { get; private set; }
    public decimal RoomRevenue { get; private set; }
    public decimal TaxPosted { get; private set; }
    public decimal ServiceChargePosted { get; private set; }
    public int FoliosClosed { get; private set; }
    public int StaysUpdated { get; private set; }
    public int ValidationFailures { get; private set; }

    public byte[]? RowVersion { get; private set; }
    public IReadOnlyList<NightAuditStepExecution> Steps => _steps;
    public IReadOnlyList<AuditValidationResult> Validations => _validations;

    // =============================================================================
    // FACTORY
    // =============================================================================
    public static NightAudit Begin(Guid propertyId, DateOnly businessDate, string auditNumber, DateTime nowUtc, ActorId? actorId)
    {
        if (string.IsNullOrWhiteSpace(auditNumber)) throw new ArgumentException("AUDIT_NUMBER_REQUIRED");
        var audit = new NightAudit { NightAuditId = Uuid7.NewGuid(), PropertyId = propertyId, AuditNumber = auditNumber.Trim(), BusinessDate = businessDate, Status = NightAuditStatus.Running, CurrentStep = NightAuditStep.LockBusinessDate, StartedAtUtc = nowUtc, StartedBy = actorId?.Value };
        foreach (NightAuditStep step in Enum.GetValues<NightAuditStep>()) audit._steps.Add(NightAuditStepExecution.Create(audit.NightAuditId, propertyId, step, nowUtc));
        return audit;
    }

    // =============================================================================
    // STEP MANAGEMENT
    // =============================================================================
    public void CompleteStep(NightAuditStep step, string? detail = null, DateTime? now = null)
    {
        AssertRunning();
        var execution = _steps.Single(s => s.Step == step);
        if (execution.Status == NightAuditStepStatus.Completed) return; // Idempotent
        execution.MarkCompleted(now ?? DateTime.UtcNow, detail);
        if (step == NightAuditStep.FinalizeAndUnlock) { Status = NightAuditStatus.Completed; CompletedAtUtc = now ?? DateTime.UtcNow; }
        else CurrentStep = Enum.GetValues<NightAuditStep>().First(s => (int)s > (int)step && _steps.Any(es => es.Step == s && es.Status != NightAuditStepStatus.Completed));
    }

    public void FailStep(NightAuditStep step, string error, DateTime? now = null)
    {
        AssertRunning();
        var execution = _steps.Single(s => s.Step == step);
        execution.MarkFailed(now ?? DateTime.UtcNow, error);
    }

    public void Fail(string error, DateTime? now = null) { AssertRunning(); Status = NightAuditStatus.Failed; ErrorMessage = error; CompletedAtUtc = now ?? DateTime.UtcNow; }
    public void Cancel(string reason, DateTime nowUtc) { AssertRunning(); Status = NightAuditStatus.Cancelled; ErrorMessage = reason; CompletedAtUtc = nowUtc; }

    // =============================================================================
    // METRICS
    // =============================================================================
    public void RecordRoomCharge(decimal amount) { RoomsCharged++; RoomRevenue += amount; }
    public void RecordTax(decimal amount) { TaxPosted += amount; }
    public void RecordServiceCharge(decimal amount) { ServiceChargePosted += amount; }
    public void RecordFolioClosed() { FoliosClosed++; }
    public void RecordStayUpdated() { StaysUpdated++; }
    public void AddValidation(AuditValidationResult r) { _validations.Add(r); if (!r.Passed && r.Severity == "Error") ValidationFailures++; }

    // =============================================================================
    // RECOVERY — RESUMABLE
    // =============================================================================
    public NightAuditStep GetLastCompletedStep() => _steps.Where(s => s.Status == NightAuditStepStatus.Completed).OrderByDescending(s => (int)s.Step).Select(s => s.Step).FirstOrDefault();
    public bool CanResume() => Status == NightAuditStatus.Failed && GetLastCompletedStep() != NightAuditStep.FinalizeAndUnlock;

    public void Resume(DateTime nowUtc)
    {
        if (!CanResume()) throw new InvalidOperationException("NIGHT_AUDIT_CANNOT_RESUME");
        Status = NightAuditStatus.Running;
        ErrorMessage = null;
        CurrentStep = GetLastCompletedStep() + 1;
    }

    public NightAuditSummary ToSummary() => new(NightAuditId, AuditNumber, BusinessDate, Status, _steps.Count, _steps.Count(s => s.Status == NightAuditStepStatus.Completed), _steps.Count(s => s.Status == NightAuditStepStatus.Failed), RoomsCharged, RoomRevenue, TaxPosted, ServiceChargePosted, FoliosClosed, StaysUpdated, ValidationFailures, ErrorMessage);

    private void AssertRunning() { if (Status != NightAuditStatus.Running) throw new InvalidOperationException($"NIGHT_AUDIT_NOT_RUNNING: {Status}"); }
}

// =============================================================================
// SUPPORTING TYPES
// =============================================================================
public sealed class NightAuditStepExecution
{
    private NightAuditStepExecution() { }
    public Guid NightAuditStepExecutionId { get; private set; }
    public Guid NightAuditId { get; private set; }
    public Guid PropertyId { get; private set; }
    public NightAuditStep Step { get; private set; }
    public NightAuditStepStatus Status { get; private set; }
    public string? Detail { get; private set; }
    public DateTime? CompletedAtUtc { get; private set; }

    public static NightAuditStepExecution Create(Guid auditId, Guid propertyId, NightAuditStep step, DateTime nowUtc) => new() { NightAuditStepExecutionId = Uuid7.NewGuid(), NightAuditId = auditId, PropertyId = propertyId, Step = step, Status = NightAuditStepStatus.Pending };
    public void MarkCompleted(DateTime now, string? detail) { Status = NightAuditStepStatus.Completed; CompletedAtUtc = now; Detail = detail; }
    public void MarkFailed(DateTime now, string? error) { Status = NightAuditStepStatus.Failed; CompletedAtUtc = now; Detail = error; }
}

public sealed class BusinessDateRecord
{
    private BusinessDateRecord() { }
    public Guid BusinessDateId { get; private set; }
    public Guid PropertyId { get; private set; }
    public DateOnly BusinessDate { get; private set; }
    public BusinessDateStatus Status { get; private set; }
    public DateTime OpenedAtUtc { get; private set; }
    public Guid? OpenedBy { get; private set; }
    public DateTime? ClosedAtUtc { get; private set; }
    public Guid? ClosedBy { get; private set; }
    public Guid? NightAuditId { get; private set; }
    public byte[]? RowVersion { get; private set; }

    public static BusinessDateRecord Open(Guid propertyId, DateOnly date, DateTime nowUtc, ActorId? actorId) => new() { BusinessDateId = Uuid7.NewGuid(), PropertyId = propertyId, BusinessDate = date, Status = BusinessDateStatus.Open, OpenedAtUtc = nowUtc, OpenedBy = actorId?.Value };
    public void BeginClosing(Guid nightAuditId, DateTime nowUtc) { Status = BusinessDateStatus.Closing; NightAuditId = nightAuditId; }
    public void Reopen()
    {
        if (Status != BusinessDateStatus.Closing)
            return;

        Status = BusinessDateStatus.Open;
        NightAuditId = null;
    }
    public void MarkClosed(DateTime nowUtc, ActorId? actorId) { Status = BusinessDateStatus.Closed; ClosedAtUtc = nowUtc; ClosedBy = actorId?.Value; }
    public void Archive(DateTime nowUtc) { Status = BusinessDateStatus.Archived; }
}

public sealed class RevenueSnapshot
{
    private RevenueSnapshot() { }
    public Guid RevenueSnapshotId { get; private set; }
    public Guid PropertyId { get; private set; }
    public Guid NightAuditId { get; private set; }
    public DateOnly BusinessDate { get; private set; }
    public decimal RoomRevenue { get; private set; }
    public decimal OtherRevenue { get; private set; }
    public decimal TotalRevenue { get; private set; }
    public decimal TotalTax { get; private set; }
    public decimal TotalPayments { get; private set; }
    public decimal TotalRefunds { get; private set; }
    public decimal OutstandingBalance { get; private set; }
    public int RoomsSold { get; private set; }
    public int TotalRooms { get; private set; }
    public decimal OccupancyRate { get; private set; }
    public decimal ADR { get; private set; }
    public decimal RevPAR { get; private set; }
    public int Arrivals { get; private set; }
    public int Departures { get; private set; }
    public int NoShows { get; private set; }
    public int Cancellations { get; private set; }
    public string SnapshotJson { get; private set; } = "{}";
    public DateTime CreatedAtUtc { get; private set; }

    public static RevenueSnapshot Create(Guid propertyId, Guid nightAuditId, DateOnly date, decimal roomRev, decimal otherRev, decimal totalRev, decimal tax, decimal payments, decimal refunds, decimal outstanding, int roomsSold, int totalRooms, decimal occupancy, decimal adr, decimal revpar, int arrivals, int departures, int noShows, int cancellations, string json, DateTime nowUtc) => new() { RevenueSnapshotId = Uuid7.NewGuid(), PropertyId = propertyId, NightAuditId = nightAuditId, BusinessDate = date, RoomRevenue = roomRev, OtherRevenue = otherRev, TotalRevenue = totalRev, TotalTax = tax, TotalPayments = payments, TotalRefunds = refunds, OutstandingBalance = outstanding, RoomsSold = roomsSold, TotalRooms = totalRooms, OccupancyRate = occupancy, ADR = adr, RevPAR = revpar, Arrivals = arrivals, Departures = departures, NoShows = noShows, Cancellations = cancellations, SnapshotJson = json, CreatedAtUtc = nowUtc };
}
