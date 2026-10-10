namespace ASMILE.BO.Core.Platform.Rules;

/// <summary>Severity of a rule violation or action.</summary>
public enum RuleSeverity { Info, Warning, Error, Critical, Blocking }

/// <summary>Rule evaluation result.</summary>
public enum RuleResult { Passed, Failed, Skipped, Error, NotApplicable }

/// <summary>Rule conflict resolution strategy.</summary>
public enum ConflictResolution { FirstWins, HighestPriorityWins, MostSpecificWins, AllApply, Manual, Custom }

/// <summary>Rule status for lifecycle management.</summary>
public enum RuleStatus { Draft, Active, Inactive, Expired, Archived }

// =============================================================================
// RULE DEFINITION
// =============================================================================

/// <summary>Generic rule definition — typed by context and fact.</summary>
public sealed record RuleDefinition<TFact>(
    string RuleId, string Name, string Description, string Category,
    int Priority, bool Exclusive, bool IsActive, RuleSeverity Severity,
    ConflictResolution ConflictStrategy,
    string? PropertyScope, DateTime EffectiveFrom, DateTime? ExpiresAt,
    int Version, DateTime CreatedAtUtc, string? CreatedBy,
    Func<TFact, RuleEvaluationContext, RuleEvalResult> Evaluator
);

/// <summary>Evaluation context — shared state during rule execution.</summary>
public sealed record RuleEvaluationContext(
    Guid CorrelationId, Guid PropertyId, string Module, DateTime Now,
    Dictionary<string, object>? Variables = null
);

/// <summary>Result of evaluating a single rule.</summary>
public sealed record RuleEvalResult(
    RuleResult Result, decimal? Score, string? Message, string? Action,
    Dictionary<string, object>? AuditData = null
);

/// <summary>Batch rule evaluation result with audit trail.</summary>
public sealed record RuleEvaluationReport(
    Guid CorrelationId, string RuleSet, int TotalRules, int Passed, int Failed,
    int Skipped, int Errors, int Conflicts, TimeSpan Duration,
    IReadOnlyList<RuleExecutionRecord> Records
);

/// <summary>Single rule execution record for audit trail.</summary>
public sealed record RuleExecutionRecord(
    string RuleId, string RuleName, RuleResult Result, decimal? Score,
    RuleSeverity Severity, string? Message, long ExecutionMs,
    DateTime EvaluatedAtUtc, Dictionary<string, object>? AuditData
);

/// <summary>Rule change history entry.</summary>
public sealed record RuleChangeRecord(
    Guid ChangeId, string RuleId, int FromVersion, int ToVersion,
    string ChangeType, string ChangedBy, string? ChangeSummary,
    DateTime ChangedAtUtc
);

// =============================================================================
// RULE ENGINE INTERFACES
// =============================================================================

/// <summary>Generic rule engine — evaluates rules against facts.</summary>
public interface IRuleEngine<TFact>
{
    Task<RuleEvaluationReport> EvaluateAsync(string ruleSet, TFact facts, RuleEvaluationContext context, CancellationToken ct);
    Task<RuleEvalResult> EvaluateSingleAsync(string ruleId, TFact facts, RuleEvaluationContext context, CancellationToken ct);
}

/// <summary>Rule repository — manage rule definitions.</summary>
public interface IRuleRepository<TFact>
{
    Task<IReadOnlyList<RuleDefinition<TFact>>> GetActiveRulesAsync(string ruleSet, string? propertyScope, DateTime? at, CancellationToken ct);
    Task<RuleDefinition<TFact>?> GetRuleAsync(string ruleId, CancellationToken ct);
    Task RegisterRuleAsync(RuleDefinition<TFact> rule, CancellationToken ct);
    Task ActivateRuleAsync(string ruleId, CancellationToken ct);
    Task DeactivateRuleAsync(string ruleId, CancellationToken ct);
    Task<IReadOnlyList<RuleChangeRecord>> GetHistoryAsync(string ruleId, CancellationToken ct);
    Task<int> GetVersionAsync(string ruleId, CancellationToken ct);
}

/// <summary>Rule conflict resolver.</summary>
public interface IRuleConflictResolver<TFact>
{
    IReadOnlyList<RuleDefinition<TFact>> Resolve(IReadOnlyList<RuleDefinition<TFact>> rules, TFact facts);
}

// =============================================================================
// BUILT-IN RULE SETS
// =============================================================================

/// <summary>Well-known rule set names across the platform.</summary>
public static class RuleSets
{
    public const string Pricing = "Pricing";
    public const string Discount = "Discount";
    public const string Promotion = "Promotion";
    public const string Vip = "VIP";
    public const string Corporate = "Corporate";
    public const string Tax = "Tax";
    public const string Package = "Package";
    public const string Approval = "Approval";
    public const string Validation = "Validation";
    public const string RateCalculation = "RateCalculation";
    public const string AllocationScoring = "AllocationScoring";
    public const string FraudDetection = "FraudDetection";
    public const string Compliance = "Compliance";
    public const string Overbooking = "Overbooking";
    public const string RevenueManagement = "RevenueManagement";
}
