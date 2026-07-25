using ASMILE.BO.Application.FrontOffice.Foundation.Cqrs;
using ASMILE.BO.Core.FrontOffice.ProcessManager;

namespace ASMILE.BO.Application.Platform.Workflow;

// =============================================================================
// WORKFLOW CATALOG — registers all known process definitions
// =============================================================================

/// <summary>Well-known workflow types across the entire platform.</summary>
public static class WorkflowCatalog
{
    // Front Office
    public const string ReservationLifecycle = "FO.Reservation.Lifecycle";
    public const string StayLifecycle = "FO.Stay.Lifecycle";
    public const string FolioLifecycle = "FO.Folio.Lifecycle";
    public const string PaymentLifecycle = "FO.Payment.Lifecycle";
    public const string NightAuditPipeline = "FO.NightAudit.Pipeline";
    public const string CheckInProcess = "FO.CheckIn.Process";
    public const string CheckOutProcess = "FO.CheckOut.Process";
    public const string RoomMoveProcess = "FO.RoomMove.Process";

    // Banquet & Restaurant
    public const string BanquetEventLifecycle = "BQ.Event.Lifecycle";
    public const string BanquetContractApproval = "BQ.Contract.Approval";
    public const string RestaurantOrderLifecycle = "RS.Order.Lifecycle";

    // CRM
    public const string GuestOnboarding = "CRM.Guest.Onboarding";
    public const string VipEnrollment = "CRM.Vip.Enrollment";
    public const string ComplaintResolution = "CRM.Complaint.Resolution";

    // Inventory
    public const string StockReceiptApproval = "INV.StockReceipt.Approval";
    public const string StockIssueApproval = "INV.StockIssue.Approval";

    // AI
    public const string TrainingDataPipeline = "AI.TrainingData.Pipeline";
    public const string ModelDeployment = "AI.Model.Deployment";
}

/// <summary>Workflow instance DTO for monitoring and dashboard.</summary>
public sealed record WorkflowInstanceDto(
    Guid ProcessId, string ProcessKey, string ProcessType, int Version,
    string CurrentState, string? CurrentStep, ProcessStatus Status,
    DateTime StartedAtUtc, DateTime UpdatedAtUtc, DateTime? CompletedAtUtc,
    int RetryCount, string? LastErrorCode, bool RequiresManualAction,
    ProcessCompensationStatus? CompensationStatus
);

/// <summary>Workflow dashboard — summary view.</summary>
public sealed record WorkflowDashboardDto(
    int TotalActive, int TotalWaiting, int TotalFailed,
    int TotalCompleted, int TotalRequiringAction, int TotalCompensating,
    IReadOnlyList<WorkflowInstanceDto> RecentFailures,
    IReadOnlyList<WorkflowInstanceDto> PendingManualActions
);

/// <summary>Workflow search filter.</summary>
public sealed record WorkflowSearchFilter(
    string? ProcessType, ProcessStatus? Status, string? ProcessKey,
    Guid? PropertyId, DateOnly? DateFrom, DateOnly? DateTo,
    bool? RequiresManualAction, int? MinRetryCount
);

// =============================================================================
// SAGA ORCHESTRATOR
// =============================================================================

/// <summary>Saga definition — a long-running distributed transaction.</summary>
public sealed record SagaDefinition(
    string SagaType, int Version, string Description,
    IReadOnlyList<SagaStep> Steps, SagaCompensationStrategy CompensationStrategy
);

/// <summary>A single step in a saga.</summary>
public sealed record SagaStep(
    string StepName, string CommandType, string AggregateType,
    string? CompensationCommandType, int TimeoutSeconds, int MaxRetries,
    bool IsIdempotent, bool IsCompensatable
);

/// <summary>How to handle compensation when a saga step fails.</summary>
public enum SagaCompensationStrategy { Backward, Forward, Manual }

/// <summary>Saga instance — tracks execution of a saga.</summary>
public sealed record SagaInstanceDto(
    Guid ProcessId, string SagaType, int CurrentStepIndex, int TotalSteps,
    ProcessStatus Status, IReadOnlyList<SagaStepResultDto> StepResults
);

/// <summary>Result of a single saga step.</summary>
public sealed record SagaStepResultDto(
    string StepName, ProcessStepStatus Status, int AttemptCount,
    string? ErrorCode, DateTime? StartedAtUtc, DateTime? CompletedAtUtc,
    bool CompensationTriggered
);

/// <summary>Saga orchestrator interface.</summary>
public interface ISagaOrchestrator
{
    Task<ProcessInstance> StartSagaAsync<TContext>(SagaDefinition saga, TContext context, Guid propertyId, Guid correlationId, CancellationToken ct) where TContext : class;
    Task<ProcessInstance> AdvanceSagaAsync(Guid processId, string stepResult, CancellationToken ct);
    Task<ProcessInstance> CompensateSagaAsync(Guid processId, int fromStepIndex, string reason, CancellationToken ct);
    Task<SagaInstanceDto?> GetSagaStatusAsync(Guid processId, CancellationToken ct);
    Task<IReadOnlyList<SagaInstanceDto>> GetActiveSagasAsync(string? sagaType, CancellationToken ct);
}

// =============================================================================
// WORKFLOW REGISTRY
// =============================================================================

/// <summary>Registers and discovers workflow definitions.</summary>
public interface IWorkflowRegistry
{
    void Register<TState, TStep, TSignal>(ProcessDefinition<TState, TStep, TSignal> definition) where TState : struct, Enum where TStep : struct, Enum where TSignal : struct, Enum;
    ProcessDefinition<TState, TStep, TSignal>? Get<TState, TStep, TSignal>(string processType, int version) where TState : struct, Enum where TStep : struct, Enum where TSignal : struct, Enum;
    IReadOnlyList<string> GetRegisteredTypes();
    bool IsKnownType(string processType);
}

/// <summary>Workflow query service for monitoring.</summary>
public interface IWorkflowQueryService
{
    Task<WorkflowDashboardDto> GetDashboardAsync(Guid propertyId, CancellationToken ct);
    Task<PageResult<WorkflowInstanceDto>> SearchAsync(Guid propertyId, WorkflowSearchFilter filter, PageRequest page, CancellationToken ct);
    Task<WorkflowInstanceDto?> GetInstanceAsync(Guid propertyId, Guid processId, CancellationToken ct);
    Task<IReadOnlyList<ProcessStep>> GetStepsAsync(Guid processId, CancellationToken ct);
    Task<IReadOnlyList<ProcessHistory>> GetHistoryAsync(Guid processId, CancellationToken ct);
}

// =============================================================================
// PREDEFINED SAGA DEFINITIONS
// =============================================================================

/// <summary>Pre-built saga catalog — ready-to-use distributed transactions.</summary>
public static class SagaCatalog
{
    /// <summary>Check-in saga: Reservation → Allocation → Stay → Folio → KeyCard</summary>
    public static SagaDefinition CheckInProcess => new(
        "FO.CheckIn.Saga", 1, "Full check-in process from reservation to key card issuance",
        [
            new SagaStep("ValidateReservation", "CheckInHotelStay", "Reservation", null, 30, 3, true, false),
            new SagaStep("AllocateRoom", "AutoAssignReservationRoom", "Allocation", "UnassignReservationRoom", 30, 2, true, true),
            new SagaStep("PrepareStay", "PrepareStayFromReservation", "Stay", "CancelPreparation", 30, 2, true, true),
            new SagaStep("CreateFolio", "CreateFolio", "Folio", null, 15, 2, true, false),
            new SagaStep("IssueKeyCard", "IssueKeyCard", "Stay", null, 10, 1, true, false)
        ],
        SagaCompensationStrategy.Backward
    );

    /// <summary>Check-out saga: Validate → Post Charges → Settle → Close → Update</summary>
    public static SagaDefinition CheckOutProcess => new(
        "FO.CheckOut.Saga", 1, "Full check-out process with posting, settlement, and close",
        [
            new SagaStep("ValidateStay", "CheckOutHotelStay", "Stay", null, 30, 3, true, false),
            new SagaStep("PostOutstanding", "PostRoomCharges", "Folio", "ReverseFolioEntry", 30, 2, true, true),
            new SagaStep("SettleFolio", "SettleFolio", "Folio", "ReopenFolio", 60, 2, true, true),
            new SagaStep("CloseFolio", "CloseFolio", "Folio", "ReopenFolio", 30, 1, true, true),
            new SagaStep("UpdateGuest", "CompleteStay", "Stay", null, 15, 1, true, false)
        ],
        SagaCompensationStrategy.Backward
    );

    /// <summary>Room move saga: Validate → Assign New → Move → Update Housekeeping</summary>
    public static SagaDefinition RoomMoveProcess => new(
        "FO.RoomMove.Saga", 1, "Room move with allocation and housekeeping notification",
        [
            new SagaStep("AllocateNewRoom", "MoveRoom", "Allocation", "MoveRoom", 30, 2, true, true),
            new SagaStep("NotifyHousekeeping", "CreateHousekeepingTask", "Housekeeping", null, 15, 1, true, false),
            new SagaStep("UpdateFolio", "AddFolioAdjustment", "Folio", "ReverseFolioEntry", 15, 1, true, true)
        ],
        SagaCompensationStrategy.Backward
    );

    /// <summary>Night Audit saga: Lock → Validate → Post → Snapshot → Roll → Unlock</summary>
    public static SagaDefinition NightAuditProcess => new(
        "FO.NightAudit.Saga", 1, "Full night audit pipeline with business date roll",
        [
            new SagaStep("LockBusinessDate", "BeginNightAudit", "NightAudit", null, 30, 2, true, false),
            new SagaStep("ValidateStays", "ExecuteNightAuditStep", "NightAudit", null, 30, 1, true, false),
            new SagaStep("PostRoomCharges", "ExecuteNightAuditStep", "NightAudit", "ReverseFolioEntry", 120, 2, true, true),
            new SagaStep("GenerateSnapshot", "ExecuteNightAuditStep", "NightAudit", null, 30, 1, true, false),
            new SagaStep("RollBusinessDate", "ExecuteNightAuditStep", "NightAudit", null, 15, 1, true, false),
            new SagaStep("UnlockProperty", "ExecuteNightAuditStep", "NightAudit", null, 10, 1, true, false)
        ],
        SagaCompensationStrategy.Manual
    );
}
