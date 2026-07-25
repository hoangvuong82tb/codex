using System.Diagnostics;
using ASMILE.BO.Core.Platform.Rules;

namespace ASMILE.BO.Infrastructure.Platform.Rules;

/// <summary>
/// Generic Rule Engine — evaluates rules against facts with priority-based conflict resolution.
/// Pipeline: PreProcess → Evaluate → Resolve → Execute → Audit.
/// </summary>
public sealed class RuleEngine<TFact> : IRuleEngine<TFact>
{
    private readonly IRuleRepository<TFact> _repository;
    private readonly IRuleConflictResolver<TFact>? _resolver;

    public RuleEngine(IRuleRepository<TFact> repository, IRuleConflictResolver<TFact>? resolver = null)
    {
        _repository = repository;
        _resolver = resolver;
    }

    public async Task<RuleEvaluationReport> EvaluateAsync(string ruleSet, TFact facts, RuleEvaluationContext context, CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();
        var records = new List<RuleExecutionRecord>();

        // 1. Fetch active rules
        var rules = await _repository.GetActiveRulesAsync(ruleSet, context.PropertyId.ToString(), context.Now, ct);
        if (rules.Count == 0) return Report(ruleSet, [], records, sw.Elapsed, context);

        // 2. Pre-process: filter applicable, order by priority
        var applicable = rules.Where(r => IsApplicable(r, context)).OrderBy(r => r.Priority).ToList();

        // 3. Conflict resolution
        if (_resolver is not null) applicable = [.. _resolver.Resolve(applicable, facts)];

        // 4. Evaluate each rule
        bool exclusiveBreak = false;
        foreach (var rule in applicable)
        {
            if (exclusiveBreak) { records.Add(Skip(rule, "Exclusive rule already applied")); continue; }
            var rec = EvaluateRule(rule, facts, context);
            records.Add(rec);
            if (rec.Result == RuleResult.Failed && rule.Severity == RuleSeverity.Blocking) break;
            if (rule.Exclusive && rec.Result == RuleResult.Passed) exclusiveBreak = true;
        }

        sw.Stop();
        return Report(ruleSet, applicable, records, sw.Elapsed, context);
    }

    public async Task<RuleEvalResult> EvaluateSingleAsync(string ruleId, TFact facts, RuleEvaluationContext context, CancellationToken ct)
    {
        var rule = await _repository.GetRuleAsync(ruleId, ct);
        if (rule is null) return new RuleEvalResult(RuleResult.Error, null, $"Rule '{ruleId}' not found.", null);
        if (!IsApplicable(rule, context)) return new RuleEvalResult(RuleResult.NotApplicable, null, "Rule not applicable in this context.", null);
        try
        {
            return rule.Evaluator(facts, context);
        }
        catch (Exception ex)
        {
            return new RuleEvalResult(RuleResult.Error, null, $"Evaluation error: {ex.Message}", null);
        }
    }

    // === Pipeline steps ===

    private static bool IsApplicable(RuleDefinition<TFact> rule, RuleEvaluationContext ctx)
    {
        if (!rule.IsActive) return false;
        if (rule.EffectiveFrom > ctx.Now) return false;
        if (rule.ExpiresAt.HasValue && rule.ExpiresAt.Value < ctx.Now) return false;
        return true;
    }

    private static RuleExecutionRecord EvaluateRule(RuleDefinition<TFact> rule, TFact facts, RuleEvaluationContext ctx)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            var result = rule.Evaluator(facts, ctx);
            sw.Stop();
            return new RuleExecutionRecord(rule.RuleId, rule.Name, result.Result, result.Score,
                result.Result == RuleResult.Failed ? rule.Severity : RuleSeverity.Info,
                result.Message, sw.ElapsedMilliseconds, ctx.Now, result.AuditData);
        }
        catch (Exception ex)
        {
            sw.Stop();
            return new RuleExecutionRecord(rule.RuleId, rule.Name, RuleResult.Error, null,
                RuleSeverity.Error, $"Rule evaluation exception: {ex.Message}",
                sw.ElapsedMilliseconds, ctx.Now, new Dictionary<string, object> { ["Exception"] = ex.GetType().Name, ["StackTrace"] = ex.StackTrace ?? "" });
        }
    }

    private static RuleEvaluationReport Report(string ruleSet, IReadOnlyList<RuleDefinition<TFact>> rules,
        List<RuleExecutionRecord> records, TimeSpan duration, RuleEvaluationContext ctx) => new(
        ctx.CorrelationId, ruleSet, rules.Count,
        records.Count(r => r.Result == RuleResult.Passed),
        records.Count(r => r.Result == RuleResult.Failed),
        records.Count(r => r.Result == RuleResult.Skipped),
        records.Count(r => r.Result == RuleResult.Error),
        records.Count(r => r.Result == RuleResult.Failed && r.Severity >= RuleSeverity.Warning),
        duration, records);

    private static RuleExecutionRecord Skip(RuleDefinition<TFact> rule, string reason) => new(rule.RuleId, rule.Name, RuleResult.Skipped, null, RuleSeverity.Info, reason, 0, DateTime.UtcNow, null);
}

/// <summary>Default conflict resolver — highest priority wins, exclusive blocks others.</summary>
public sealed class PriorityConflictResolver<TFact> : IRuleConflictResolver<TFact>
{
    public IReadOnlyList<RuleDefinition<TFact>> Resolve(IReadOnlyList<RuleDefinition<TFact>> rules, TFact facts)
    {
        // Group by conflict strategy, keep highest priority per group
        return rules
            .GroupBy(r => r.ConflictStrategy)
            .SelectMany(g => g.Key switch
            {
                ConflictResolution.HighestPriorityWins => [g.OrderBy(r => r.Priority).First()],
                ConflictResolution.FirstWins => [g.First()],
                ConflictResolution.AllApply => g.AsEnumerable(),
                _ => g.OrderBy(r => r.Priority).ToList()
            })
            .OrderBy(r => r.Priority)
            .ToList();
    }
}

/// <summary>In-memory rule repository for testing and small rule sets.</summary>
public sealed class InMemoryRuleRepository<TFact> : IRuleRepository<TFact>
{
    private readonly List<RuleDefinition<TFact>> _rules = [];
    private readonly List<RuleChangeRecord> _history = [];

    public Task<IReadOnlyList<RuleDefinition<TFact>>> GetActiveRulesAsync(string ruleSet, string? scope, DateTime? at, CancellationToken ct)
    {
        var now = at ?? DateTime.UtcNow;
        return Task.FromResult<IReadOnlyList<RuleDefinition<TFact>>>(
            _rules.Where(r => r.Category == ruleSet && r.IsActive && r.EffectiveFrom <= now && (r.ExpiresAt == null || r.ExpiresAt > now) && (r.PropertyScope == null || r.PropertyScope == scope || r.PropertyScope == "*")).OrderBy(r => r.Priority).ToList());
    }

    public Task<RuleDefinition<TFact>?> GetRuleAsync(string ruleId, CancellationToken ct)
        => Task.FromResult(_rules.FirstOrDefault(r => r.RuleId == ruleId));

    public Task RegisterRuleAsync(RuleDefinition<TFact> rule, CancellationToken ct) { _rules.Add(rule); return Task.CompletedTask; }
    public Task ActivateRuleAsync(string ruleId, CancellationToken ct) { var r = _rules.FirstOrDefault(r => r.RuleId == ruleId); if (r is not null) _rules[_rules.IndexOf(r)] = r with { IsActive = true }; return Task.CompletedTask; }
    public Task DeactivateRuleAsync(string ruleId, CancellationToken ct) { var r = _rules.FirstOrDefault(r => r.RuleId == ruleId); if (r is not null) _rules[_rules.IndexOf(r)] = r with { IsActive = false }; return Task.CompletedTask; }
    public Task<IReadOnlyList<RuleChangeRecord>> GetHistoryAsync(string ruleId, CancellationToken ct) => Task.FromResult<IReadOnlyList<RuleChangeRecord>>(_history.Where(h => h.RuleId == ruleId).ToList());
    public Task<int> GetVersionAsync(string ruleId, CancellationToken ct) => Task.FromResult(_rules.Where(r => r.RuleId == ruleId).Max(r => r.Version));
}

/// <summary>Rule monitoring — collects execution statistics.</summary>
public sealed class RuleMonitor
{
    private readonly Dictionary<string, RuleStats> _stats = [];

    public void RecordHit(string ruleId, long executionMs)
    {
        if (!_stats.ContainsKey(ruleId)) _stats[ruleId] = new RuleStats();
        var s = _stats[ruleId];
        s.Hits++; s.TotalExecutionMs += executionMs;
        s.LastHitAtUtc = DateTime.UtcNow;
    }

    public void RecordMiss(string ruleId) { if (!_stats.ContainsKey(ruleId)) _stats[ruleId] = new RuleStats(); _stats[ruleId].Misses++; }
    public void RecordConflict(string ruleId) { if (!_stats.ContainsKey(ruleId)) _stats[ruleId] = new RuleStats(); _stats[ruleId].Conflicts++; }

    public RuleStats? GetStats(string ruleId) => _stats.GetValueOrDefault(ruleId);
    public IReadOnlyDictionary<string, RuleStats> AllStats => _stats;
}

public sealed class RuleStats
{
    public int Hits { get; set; }
    public int Misses { get; set; }
    public int Conflicts { get; set; }
    public long TotalExecutionMs { get; set; }
    public DateTime? LastHitAtUtc { get; set; }
    public double AvgExecutionMs => Hits > 0 ? (double)TotalExecutionMs / Hits : 0;
    public double HitRate => Hits + Misses > 0 ? (double)Hits / (Hits + Misses) * 100 : 0;
}
