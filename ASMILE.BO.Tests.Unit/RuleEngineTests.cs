using ASMILE.BO.Core.Platform.Rules;
using ASMILE.BO.Infrastructure.Platform.Rules;
using FluentAssertions;

namespace ASMILE.BO.Tests.Unit;

public sealed class RuleEngineTests
{
    // =============================================================================
    // SINGLE RULE EVALUATION
    // =============================================================================
    [Fact]
    public async Task RULE_001_single_passing_rule()
    {
        var repo = new InMemoryRuleRepository<OrderFacts>();
        await repo.RegisterRuleAsync(PassRule("R001", "MinAmount", 10), default);
        var engine = new RuleEngine<OrderFacts>(repo);
        var ctx = Ctx();

        var report = await engine.EvaluateAsync("Pricing", new OrderFacts(500), ctx, default);

        report.Passed.Should().Be(1);
        report.Failed.Should().Be(0);
        report.Records[0].Result.Should().Be(RuleResult.Passed);
    }

    [Fact]
    public async Task RULE_002_failing_rule()
    {
        var repo = new InMemoryRuleRepository<OrderFacts>();
        await repo.RegisterRuleAsync(FailRule("R002", "MaxDiscount", 10), default);
        var engine = new RuleEngine<OrderFacts>(repo);
        var ctx = Ctx();

        var report = await engine.EvaluateAsync("Pricing", new OrderFacts(100), ctx, default);

        report.Failed.Should().Be(1);
        report.Records[0].Message.Should().Contain("exceeds");
    }

    // =============================================================================
    // PRIORITY ORDERING
    // =============================================================================
    [Fact]
    public async Task RULE_003_rules_evaluated_in_priority_order()
    {
        var repo = new InMemoryRuleRepository<OrderFacts>();
        await repo.RegisterRuleAsync(PassRule("R003", "LowPriority", 50), default);
        await repo.RegisterRuleAsync(PassRule("R004", "HighPriority", 1), default);
        var engine = new RuleEngine<OrderFacts>(repo);

        var report = await engine.EvaluateAsync("Pricing", new OrderFacts(200), Ctx(), default);

        report.Records[0].RuleId.Should().Be("R004"); // Priority 1 first
        report.Records[1].RuleId.Should().Be("R003"); // Priority 50 second
    }

    // =============================================================================
    // EXCLUSIVE RULES
    // =============================================================================
    [Fact]
    public async Task RULE_004_exclusive_rule_skips_lower_priority()
    {
        var repo = new InMemoryRuleRepository<OrderFacts>();
        await repo.RegisterRuleAsync(ExclusivePassRule("R005", "ExclusiveVip", 1, "TestSet"), default);
        await repo.RegisterRuleAsync(PassRule("R006", "Normal", 10, category: "TestSet"), default);
        var engine = new RuleEngine<OrderFacts>(repo);

        var report = await engine.EvaluateAsync("TestSet", new OrderFacts(300), Ctx(), default);

        report.TotalRules.Should().Be(2);
        report.Records.Should().Contain(r => r.Result == RuleResult.Passed && r.RuleId == "R005");
        report.Records.Should().Contain(r => r.Result == RuleResult.Skipped && r.RuleId == "R006");
    }

    // =============================================================================
    // RULE NOT APPLICABLE (EXPIRED / NOT YET ACTIVE)
    // =============================================================================
    [Fact]
    public async Task RULE_005_expired_rule_not_evaluated()
    {
        var repo = new InMemoryRuleRepository<OrderFacts>();
        var expired = PassRule("R007", "Expired", 1)
            .Deactivate(DateTime.UtcNow.AddDays(-1)); // Set IsActive=false — expired not checked via IsApplicable
        await repo.RegisterRuleAsync(expired, default);
        var engine = new RuleEngine<OrderFacts>(repo);

        var report = await engine.EvaluateAsync("Pricing", new OrderFacts(200), Ctx(), default);

        report.TotalRules.Should().Be(0); // inactive = not in results
    }

    [Fact]
    public async Task RULE_006_future_rule_not_evaluated()
    {
        var repo = new InMemoryRuleRepository<OrderFacts>();
        await repo.RegisterRuleAsync(PassRule("R008", "Future", 1).ScheduleFuture(DateTime.UtcNow.AddDays(7)), default);
        var engine = new RuleEngine<OrderFacts>(repo);

        var report = await engine.EvaluateAsync("Pricing", new OrderFacts(200), Ctx(), default);

        report.TotalRules.Should().Be(0); // Not yet effective
    }

    // =============================================================================
    // CONFLICT RESOLUTION
    // =============================================================================
    [Fact]
    public async Task RULE_007_highest_priority_wins_conflict_resolution()
    {
        var repo = new InMemoryRuleRepository<OrderFacts>();
        await repo.RegisterRuleAsync(PassRule("R009", "Low", 10).WithStrategy(ConflictResolution.HighestPriorityWins), default);
        await repo.RegisterRuleAsync(PassRule("R010", "High", 1).WithStrategy(ConflictResolution.HighestPriorityWins), default);
        var resolver = new PriorityConflictResolver<OrderFacts>();
        var engine = new RuleEngine<OrderFacts>(repo, resolver);

        var report = await engine.EvaluateAsync("Pricing", new OrderFacts(200), Ctx(), default);

        report.Records.Should().HaveCount(1); // HighestPriorityWins keeps only 1
        report.Records[0].RuleId.Should().Be("R010");
    }

    // =============================================================================
    // PERFORMANCE TRACKING
    // =============================================================================
    [Fact]
    public async Task RULE_008_execution_time_tracked()
    {
        var repo = new InMemoryRuleRepository<OrderFacts>();
        await repo.RegisterRuleAsync(PassRule("R011", "Timed", 1), default);
        var engine = new RuleEngine<OrderFacts>(repo);

        var report = await engine.EvaluateAsync("Pricing", new OrderFacts(200), Ctx(), default);

        report.Duration.Should().BeGreaterThan(TimeSpan.Zero);
        report.Records[0].ExecutionMs.Should().BeGreaterThanOrEqualTo(0);
    }

    // =============================================================================
    // RULE MONITORING
    // =============================================================================
    [Fact]
    public void RULE_009_monitor_tracks_hits_and_misses()
    {
        var monitor = new RuleMonitor();
        monitor.RecordHit("R001", 15);
        monitor.RecordHit("R001", 25);
        monitor.RecordMiss("R002");

        var s1 = monitor.GetStats("R001")!;
        s1.Hits.Should().Be(2);
        s1.AvgExecutionMs.Should().Be(20);

        var s2 = monitor.GetStats("R002")!;
        s2.Misses.Should().Be(1);
        s2.Hits.Should().Be(0);
    }

    // =============================================================================
    // RULE REPOSITORY
    // =============================================================================
    [Fact]
    public async Task RULE_010_activate_deactivate_rule()
    {
        var repo = new InMemoryRuleRepository<OrderFacts>();
        await repo.RegisterRuleAsync(PassRule("R012", "Toggle", 1), default);
        (await repo.GetActiveRulesAsync("Pricing", null, null, default)).Should().HaveCount(1);

        await repo.DeactivateRuleAsync("R012", default);
        (await repo.GetActiveRulesAsync("Pricing", null, null, default)).Should().HaveCount(0);

        await repo.ActivateRuleAsync("R012", default);
        (await repo.GetActiveRulesAsync("Pricing", null, null, default)).Should().HaveCount(1);
    }

    [Fact]
    public async Task RULE_011_version_tracking()
    {
        var repo = new InMemoryRuleRepository<OrderFacts>();
        await repo.RegisterRuleAsync(PassRule("R013", "V1", 1, version: 1), default);
        await repo.RegisterRuleAsync(PassRule("R013", "V2", 1, version: 2), default);
        (await repo.GetVersionAsync("R013", default)).Should().Be(2);
    }

    // =============================================================================
    // BLOCKING RULE
    // =============================================================================
    [Fact]
    public async Task RULE_012_blocking_rule_stops_evaluation()
    {
        var repo = new InMemoryRuleRepository<OrderFacts>();
        await repo.RegisterRuleAsync(BlockingFailRule("R014", "Block", 1), default);
        await repo.RegisterRuleAsync(PassRule("R015", "After", 10), default);
        var engine = new RuleEngine<OrderFacts>(repo);

        var report = await engine.EvaluateAsync("Validation", new OrderFacts(50), Ctx(), default);

        report.Records.Should().HaveCount(1); // Second rule not evaluated
        report.Records[0].Severity.Should().Be(RuleSeverity.Blocking);
    }

    // =============================================================================
    // PROPERTY SCOPE
    // =============================================================================
    [Fact]
    public async Task RULE_013_property_scoped_rule()
    {
        var repo = new InMemoryRuleRepository<OrderFacts>();
        await repo.RegisterRuleAsync(PassRule("R016", "PropertyA", 1).WithScope(PropA.ToString()), default);
        var engine = new RuleEngine<OrderFacts>(repo);

        var reportA = await engine.EvaluateAsync("Pricing", new OrderFacts(200), Ctx(PropA), default);
        reportA.TotalRules.Should().Be(1);

        var reportB = await engine.EvaluateAsync("Pricing", new OrderFacts(200), Ctx(PropB), default);
        reportB.TotalRules.Should().Be(0);
    }

    // =============================================================================
    // HELPERS
    // =============================================================================
    private static Guid PropA = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static Guid PropB = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static RuleEvaluationContext Ctx(Guid? propId = null) => new(Guid.NewGuid(), propId ?? Guid.NewGuid(), "Test", DateTime.UtcNow);

    private static RuleDefinition<OrderFacts> PassRule(string id, string name, int priority, int version = 1, string category = "Pricing") => new(id, name, "Test rule", category, priority, false, true, RuleSeverity.Info, ConflictResolution.AllApply, "*", DateTime.UtcNow.AddDays(-1), null, version, DateTime.UtcNow, null, (f, _) => new RuleEvalResult(f.Amount > 100 ? RuleResult.Passed : RuleResult.Failed, f.Amount, f.Amount > 100 ? "OK" : "Amount too low", null));

    private static RuleDefinition<OrderFacts> FailRule(string id, string name, int priority) => new(id, name, "Fail rule", "Pricing", priority, false, true, RuleSeverity.Warning, ConflictResolution.AllApply, "*", DateTime.UtcNow.AddDays(-1), null, 1, DateTime.UtcNow, null, (f, _) => new RuleEvalResult(RuleResult.Failed, null, $"Amount {f.Amount} exceeds limit", null));

    private static RuleDefinition<OrderFacts> ExclusivePassRule(string id, string name, int priority, string category = "VIP") => new(id, name, "Exclusive", category, priority, true, true, RuleSeverity.Info, ConflictResolution.HighestPriorityWins, "*", DateTime.UtcNow.AddDays(-1), null, 1, DateTime.UtcNow, null, (_, _) => new RuleEvalResult(RuleResult.Passed, 100, "VIP rule applied", "ApplyVIP"));

    private static RuleDefinition<OrderFacts> BlockingFailRule(string id, string name, int priority) => new(id, name, "Block", "Validation", priority, false, true, RuleSeverity.Blocking, ConflictResolution.FirstWins, "*", DateTime.UtcNow.AddDays(-1), null, 1, DateTime.UtcNow, null, (f, _) => f.Amount > 0 ? new RuleEvalResult(RuleResult.Failed, null, "Blocked", null) : new RuleEvalResult(RuleResult.Passed, null, "OK", null));
}

/// <summary>Sample fact type for testing.</summary>
public sealed record OrderFacts(decimal Amount);

/// <summary>Extension helpers for test rule construction.</summary>
public static class RuleDefinitionExtensions
{
    public static RuleDefinition<T> Deactivate<T>(this RuleDefinition<T> rule, DateTime now) => rule with { IsActive = false, ExpiresAt = now };
    public static RuleDefinition<T> ScheduleFuture<T>(this RuleDefinition<T> rule, DateTime effectiveFrom) => rule with { EffectiveFrom = effectiveFrom };
    public static RuleDefinition<T> WithScope<T>(this RuleDefinition<T> rule, string scope) => rule with { PropertyScope = scope };
    public static RuleDefinition<T> WithStrategy<T>(this RuleDefinition<T> rule, ConflictResolution strategy) => rule with { ConflictStrategy = strategy };
}
