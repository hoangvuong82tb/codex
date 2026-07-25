namespace ASMILE.BO.Application.Platform.Observability;

// =============================================================================
// DISTRIBUTED TRACING
// =============================================================================

/// <summary>Trace context — propagates through the entire request chain.</summary>
public sealed record TraceContext(
    string TraceId, string SpanId, string? ParentSpanId,
    Guid CorrelationId, Guid? CausationId,
    string ServiceName, string OperationName,
    DateTime StartedAtUtc
);

/// <summary>Span represents a single unit of work in a trace.</summary>
public sealed record TraceSpan(
    string SpanId, string TraceId, string? ParentSpanId,
    string OperationName, string ServiceName, string Category,
    DateTime StartTime, DateTime? EndTime, long? DurationMs,
    TraceSpanStatus Status, IReadOnlyDictionary<string, string> Tags,
    IReadOnlyList<TraceEvent> Events
);

public enum TraceSpanStatus { Ok, Error, Cancelled }

public sealed record TraceEvent(string Name, DateTime Timestamp, IReadOnlyDictionary<string, string> Attributes);

/// <summary>Tracing interface — create spans and propagate context.</summary>
public interface ITracingService
{
    TraceSpan StartSpan(string operationName, string category, TraceContext? parent = null);
    void EndSpan(TraceSpan span, TraceSpanStatus status = TraceSpanStatus.Ok);
    void AddEvent(TraceSpan span, string eventName, Dictionary<string, string>? attributes = null);
    void SetTag(TraceSpan span, string key, string value);
    Task<IReadOnlyList<TraceSpan>> GetTraceAsync(string traceId, CancellationToken ct);
    TraceContext? CurrentContext { get; }
}

// =============================================================================
// METRICS
// =============================================================================

/// <summary>Metric types supported by the platform.</summary>
public enum MetricType { Counter, Gauge, Histogram, Timer }

/// <summary>A single metric data point.</summary>
public sealed record MetricPoint(string Name, MetricType Type, double Value, DateTime Timestamp, IReadOnlyDictionary<string, string> Tags);

/// <summary>Predefined metrics across modules.</summary>
public static class PlatformMetrics
{
    // Workflow
    public const string WorkflowActive = "workflow.active";
    public const string WorkflowCompleted = "workflow.completed";
    public const string WorkflowFailed = "workflow.failed";
    public const string WorkflowDuration = "workflow.duration_ms";
    public const string WorkflowRetryCount = "workflow.retry_count";

    // Saga
    public const string SagaActive = "saga.active";
    public const string SagaCompleted = "saga.completed";
    public const string SagaCompensated = "saga.compensated";
    public const string SagaDuration = "saga.duration_ms";

    // Rule Engine
    public const string RuleEvaluationCount = "rule.evaluation_count";
    public const string RuleEvaluationDuration = "rule.evaluation_ms";
    public const string RuleHitRate = "rule.hit_rate";
    public const string RuleConflictCount = "rule.conflict_count";

    // Event Bus
    public const string EventPublished = "event.published";
    public const string EventFailed = "event.failed";
    public const string EventRetryCount = "event.retry_count";
    public const string EventDeadLetter = "event.dead_letter";

    // Night Audit
    public const string NightAuditDuration = "night_audit.duration_ms";
    public const string NightAuditStepsCompleted = "night_audit.steps_completed";
    public const string NightAuditRoomChargesPosted = "night_audit.room_charges";
    public const string NightAuditRevenue = "night_audit.revenue";

    // Payment
    public const string PaymentCount = "payment.count";
    public const string PaymentAmount = "payment.amount";
    public const string PaymentRefundCount = "payment.refund_count";
    public const string PaymentProcessingDuration = "payment.processing_ms";

    // Reservation
    public const string ReservationCreated = "reservation.created";
    public const string ReservationConfirmed = "reservation.confirmed";
    public const string ReservationCancelled = "reservation.cancelled";
    public const string ReservationOccupancy = "reservation.occupancy_pct";

    // System
    public const string OutboxPending = "outbox.pending";
    public const string OutboxFailed = "outbox.failed";
    public const string QueueDepth = "queue.depth";
    public const string WorkerActive = "worker.active";
}

/// <summary>Metrics collector interface.</summary>
public interface IMetricsCollector
{
    void Increment(string name, double value = 1, Dictionary<string, string>? tags = null);
    void Gauge(string name, double value, Dictionary<string, string>? tags = null);
    void Histogram(string name, double value, Dictionary<string, string>? tags = null);
    IDisposable MeasureDuration(string name, Dictionary<string, string>? tags = null);
    Task<IReadOnlyList<MetricPoint>> GetMetricsAsync(string? prefix, DateTime since, CancellationToken ct);
}

// =============================================================================
// STRUCTURED LOGGING
// =============================================================================

/// <summary>Log categories for structured filtering.</summary>
public enum LogCategory { Business, Technical, Security, Audit, Performance }

/// <summary>Structured log entry.</summary>
public sealed record StructuredLogEntry(
    LogCategory Category, string Level, string Message, string Module,
    Guid? CorrelationId, Guid? CausationId, string? TraceId, string? SpanId,
    Guid? PropertyId, Guid? ActorId, DateTime Timestamp,
    IReadOnlyDictionary<string, object> Context
);

/// <summary>Structured logging interface.</summary>
public interface IStructuredLogger
{
    void Log(LogCategory category, string level, string message, string module, Dictionary<string, object>? context = null);
    void Business(string message, string module, Dictionary<string, object>? context = null);
    void Technical(string message, string module, Dictionary<string, object>? context = null);
    void Security(string message, string module, Dictionary<string, object>? context = null);
    void Audit(string message, string module, Dictionary<string, object>? context = null);
}

// =============================================================================
// HEALTH MONITORING
// =============================================================================

/// <summary>Health status.</summary>
public enum HealthStatus { Healthy, Degraded, Unhealthy, Unknown }

/// <summary>Health check result.</summary>
public sealed record HealthCheckResult(
    string Component, HealthStatus Status, string? Message,
    DateTime CheckedAtUtc, long? ResponseMs,
    IReadOnlyDictionary<string, object>? Details
);

/// <summary>System health summary.</summary>
public sealed record SystemHealth(
    DateTime CheckedAtUtc, HealthStatus Overall,
    IReadOnlyList<HealthCheckResult> Components
);

/// <summary>Health check interface.</summary>
public interface IHealthChecker
{
    Task<HealthCheckResult> CheckAsync(CancellationToken ct);
    string ComponentName { get; }
}

// =============================================================================
// ALERTING
// =============================================================================

/// <summary>Alert severity.</summary>
public enum AlertSeverity { Info, Warning, Critical, Emergency }

/// <summary>Alert definition.</summary>
public sealed record AlertDefinition(
    string AlertId, string Name, string Description, string Module,
    AlertSeverity Severity, string Condition, string? RecommendedAction
);

/// <summary>Alert instance — fired when condition is met.</summary>
public sealed record Alert(
    string AlertId, string Name, AlertSeverity Severity, string Module,
    string Message, DateTime FiredAtUtc, Guid? CorrelationId,
    IReadOnlyDictionary<string, object> Context
);

/// <summary>Predefined alerts.</summary>
public static class PlatformAlerts
{
    public static AlertDefinition WorkflowFailed => new("ALERT_WF_001", "Workflow Failed", "A workflow instance has failed", "Workflow", AlertSeverity.Warning, "Workflow.Status == Failed", "Check workflow details and retry or escalate.");
    public static AlertDefinition SagaFailed => new("ALERT_SG_001", "Saga Failed", "A saga orchestration has failed", "Saga", AlertSeverity.Critical, "Saga.Status == Failed", "Check saga steps, run compensation if needed.");
    public static AlertDefinition NightAuditFailed => new("ALERT_NA_001", "Night Audit Failed", "Night Audit process has failed", "NightAudit", AlertSeverity.Critical, "NightAudit.Status == Failed", "Resume or restart Night Audit immediately.");
    public static AlertDefinition PaymentFailed => new("ALERT_PY_001", "Payment Failed", "Payment processing has failed", "Payment", AlertSeverity.Critical, "Payment.Status == Failed", "Investigate payment gateway or retry.");
    public static AlertDefinition RuleConflict => new("ALERT_RL_001", "Rule Conflict Detected", "Multiple rules in conflict", "RuleEngine", AlertSeverity.Warning, "Rule.Conflicts > 0", "Review conflicting rules and resolve.");
    public static AlertDefinition OutboxBacklog => new("ALERT_OB_001", "Outbox Backlog", "Outbox message backlog exceeding threshold", "Outbox", AlertSeverity.Warning, "Outbox.Pending > 1000", "Check publisher health and scale workers.");
    public static AlertDefinition DeadLetterQueue => new("ALERT_DL_001", "Dead Letter Queue", "Messages in dead letter queue", "DeadLetter", AlertSeverity.Critical, "DeadLetter.Count > 0", "Investigate dead letter messages and take action.");
}

/// <summary>Alerting interface.</summary>
public interface IAlertingService
{
    Task FireAsync(Alert alert, CancellationToken ct);
    Task<IReadOnlyList<Alert>> GetActiveAlertsAsync(string? module, DateTime since, CancellationToken ct);
    Task AcknowledgeAsync(string alertId, Guid actorId, CancellationToken ct);
    void RegisterAlert(AlertDefinition definition);
}

/// <summary>Dashboard data DTOs.</summary>
public sealed record SystemDashboardDto(
    SystemHealth Health, IReadOnlyList<MetricPoint> KeyMetrics,
    IReadOnlyList<Alert> ActiveAlerts, int OutboxPending, int OutboxFailed,
    int ActiveWorkflows, int ActiveSagas
);

public sealed record BusinessDashboardDto(
    decimal TodayRevenue, int OccupancyPct, int ArrivalsToday, int DeparturesToday,
    int RoomsOccupied, int RoomsAvailable, decimal ADR, decimal RevPAR,
    int ReservationsCreated, int PaymentsProcessed
);

public sealed record PerformanceDashboardDto(
    double AvgRuleEvaluationMs, double AvgWorkflowDurationMs, double AvgPaymentProcessingMs,
    double AvgNightAuditDurationMs, double OutboxPublishRate, double EventProcessingRate,
    int TotalTraces, int TotalSpans
);
