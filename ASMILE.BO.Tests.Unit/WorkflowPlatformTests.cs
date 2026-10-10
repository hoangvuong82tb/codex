using ASMILE.BO.Application.Platform.Workflow;
using ASMILE.BO.Core.FrontOffice.Foundation.Identifiers;
using ASMILE.BO.Core.FrontOffice.ProcessManager;
using FluentAssertions;

namespace ASMILE.BO.Tests.Unit;

public sealed class WorkflowPlatformTests
{
    // =============================================================================
    // WORKFLOW REGISTRY
    // =============================================================================
    [Fact]
    public void WF_PLAT_001_registry_registers_and_retrieves_definition()
    {
        var registry = new TestWorkflowRegistry();
        var def = CreateTestDefinition();
        registry.Register(def);
        var retrieved = registry.Get("TestProcess", 1);
        retrieved.Should().NotBeNull();
        retrieved!.ProcessType.Should().Be("TestProcess");
        retrieved.IsTerminal("Completed").Should().BeTrue();
    }

    [Fact]
    public void WF_PLAT_002_registry_returns_null_for_unknown_type()
    {
        var registry = new TestWorkflowRegistry();
        registry.Register(CreateTestDefinition());
        registry.Get("Unknown", 1).Should().BeNull();
    }

    [Fact]
    public void WF_PLAT_003_registry_lists_all_registered_types()
    {
        var registry = new TestWorkflowRegistry();
        registry.Register(CreateTestDefinition("A", 1));
        registry.Register(CreateTestDefinition("B", 1));
        var types = registry.GetRegisteredTypes();
        types.Should().Contain(t => t.StartsWith("A"));
        types.Should().Contain(t => t.StartsWith("B"));
    }

    // =============================================================================
    // PROCESS DEFINITION
    // =============================================================================
    [Fact]
    public void WF_PLAT_004_terminal_states_are_recognized()
    {
        var def = CreateTestDefinition();
        def.IsTerminal("Completed").Should().BeTrue();
        def.IsTerminal("Failed").Should().BeTrue();
        def.IsTerminal("Started").Should().BeFalse();
    }

    [Fact]
    public void WF_PLAT_005_transition_lookup_by_state_and_signal()
    {
        var def = CreateTestDefinition();
        def.TryGetTransition("Started", "Submit", out var t).Should().BeTrue();
        t.From.Should().Be("Started");
        t.To.Should().Be("Processing");
    }

    [Fact]
    public void WF_PLAT_006_unknown_signal_returns_no_transition()
    {
        var def = CreateTestDefinition();
        def.TryGetTransition("Started", "UnknownSignal", out _).Should().BeFalse();
    }

    // =============================================================================
    // PROCESS INSTANCE
    // =============================================================================
    [Fact]
    public void WF_PLAT_007_instance_created_with_correct_initial_state()
    {
        var process = ProcessInstance.Start("KEY-001", "TestProcess", 1,
            new(PropertyId.New().Value), DateOnly.FromDateTime(DateTime.UtcNow),
            "Test", "T-001", new(Guid.NewGuid()), DateTime.UtcNow, "Started", "Validate");
        process.ProcessKey.Should().Be("KEY-001");
        process.ProcessType.Should().Be("TestProcess");
        process.CurrentState.Should().Be("Started");
        process.CurrentStep.Should().Be("Validate");
        process.Status.Should().Be(ProcessStatus.Active);
        process.IsTerminal.Should().BeFalse();
    }

    [Fact]
    public void WF_PLAT_008_instance_terminal_detection()
    {
        var process = CreateTestProcess();
        process.IsTerminal.Should().BeFalse();
        process.GetType().GetProperty("Status")!.SetValue(process, ProcessStatus.Completed);
        process.IsTerminal.Should().BeTrue();
    }

    [Fact]
    public void WF_PLAT_009_retry_count_increments()
    {
        var process = CreateTestProcess();
        process.RetryCount.Should().Be(0);
        process.IncrementRetry("ERR_TIMEOUT", DateTime.UtcNow);
        process.RetryCount.Should().Be(1);
        process.LastErrorCode.Should().Be("ERR_TIMEOUT");
    }

    [Fact]
    public void WF_PLAT_010_compensation_status_tracking()
    {
        var process = CreateTestProcess();
        process.CompensationStatus.Should().BeNull();
        process.SetCompensation(ProcessCompensationStatus.Requested, DateTime.UtcNow);
        process.CompensationStatus.Should().Be(ProcessCompensationStatus.Requested);
    }

    // =============================================================================
    // PROCESS STEP
    // =============================================================================
    [Fact]
    public void WF_PLAT_011_step_lifecycle()
    {
        var process = CreateTestProcess();
        var step = ProcessStep.Create(process.ProcessId, "Validate", 1, 1, "ValidateCmd", DateTime.UtcNow.AddMinutes(5));
        step.StepName.Should().Be("Validate");
        step.Status.Should().Be(ProcessStepStatus.Pending);
        step.Start(DateTime.UtcNow);
        step.Status.Should().Be(ProcessStepStatus.Running);
        step.Complete("OK-001", DateTime.UtcNow);
        step.Status.Should().Be(ProcessStepStatus.Completed);
        step.ResultReference.Should().Be("OK-001");
    }

    // =============================================================================
    // RETRY POLICY
    // =============================================================================
    [Fact]
    public void WF_PLAT_012_retry_policy_allows_retry_within_max()
    {
        var policy = new ProcessRetryPolicy(3, TimeSpan.FromSeconds(1), 2, TimeSpan.FromSeconds(60), 0.1,
            new HashSet<string>(), ProcessEscalation.DeadLetter);
        policy.CanRetry(0, ProcessRetryClassification.TransientSafe, null).Should().BeTrue();
        policy.CanRetry(2, ProcessRetryClassification.TransientSafe, null).Should().BeTrue();
        policy.CanRetry(3, ProcessRetryClassification.TransientSafe, null).Should().BeFalse();
    }

    [Fact]
    public void WF_PLAT_013_retry_policy_rejects_permanent_errors()
    {
        var policy = new ProcessRetryPolicy(3, TimeSpan.FromSeconds(1), 2, TimeSpan.FromSeconds(60), 0.1,
            new HashSet<string>(), ProcessEscalation.Fail);
        policy.CanRetry(0, ProcessRetryClassification.PermanentBusiness, null).Should().BeFalse();
        policy.CanRetry(0, ProcessRetryClassification.PermanentTechnical, null).Should().BeFalse();
    }

    [Fact]
    public void WF_PLAT_014_retry_delay_exponential_backoff()
    {
        var policy = new ProcessRetryPolicy(5, TimeSpan.FromSeconds(1), 2, TimeSpan.FromMinutes(10), 0,
            new HashSet<string>(), ProcessEscalation.DeadLetter);
        var d1 = policy.DelayFor(1);
        var d2 = policy.DelayFor(2);
        var d3 = policy.DelayFor(3);
        d1.Should().BeLessThanOrEqualTo(TimeSpan.FromSeconds(2));
        d2.Should().BeLessThanOrEqualTo(TimeSpan.FromSeconds(4));
        d3.Should().BeLessThanOrEqualTo(TimeSpan.FromSeconds(8));
    }

    // =============================================================================
    // SAGA CATALOG
    // =============================================================================
    [Fact]
    public void WF_PLAT_015_checkin_saga_has_correct_structure()
    {
        var saga = SagaCatalog.CheckInProcess;
        saga.SagaType.Should().Be("FO.CheckIn.Saga");
        saga.Steps.Count.Should().Be(5);
        saga.Steps[0].StepName.Should().Be("ValidateReservation");
        saga.Steps[4].StepName.Should().Be("IssueKeyCard");
        saga.CompensationStrategy.Should().Be(SagaCompensationStrategy.Backward);
    }

    [Fact]
    public void WF_PLAT_016_checkout_saga_has_compensatable_steps()
    {
        var saga = SagaCatalog.CheckOutProcess;
        saga.CompensationStrategy.Should().Be(SagaCompensationStrategy.Backward);
        saga.Steps.Count(s => s.IsCompensatable).Should().BeGreaterThanOrEqualTo(3);
    }

    [Fact]
    public void WF_PLAT_017_room_move_saga_compensatable()
    {
        var saga = SagaCatalog.RoomMoveProcess;
        saga.Steps[0].IsCompensatable.Should().BeTrue();
    }

    [Fact]
    public void WF_PLAT_018_night_audit_saga_is_manual_compensation()
    {
        var saga = SagaCatalog.NightAuditProcess;
        saga.CompensationStrategy.Should().Be(SagaCompensationStrategy.Manual);
        saga.Steps.Count.Should().Be(6);
    }

    [Fact]
    public void WF_PLAT_019_all_saga_steps_have_timeout()
    {
        foreach (var saga in new[] { SagaCatalog.CheckInProcess, SagaCatalog.CheckOutProcess, SagaCatalog.RoomMoveProcess, SagaCatalog.NightAuditProcess })
            saga.Steps.Should().AllSatisfy(s => s.TimeoutSeconds.Should().BeGreaterThan(0));
    }

    // =============================================================================
    // WORKFLOW CATALOG
    // =============================================================================
    [Fact]
    public void WF_PLAT_020_catalog_contains_all_fo_workflows()
    {
        var foTypes = new[] { WorkflowCatalog.ReservationLifecycle, WorkflowCatalog.StayLifecycle, WorkflowCatalog.FolioLifecycle, WorkflowCatalog.PaymentLifecycle, WorkflowCatalog.NightAuditPipeline, WorkflowCatalog.CheckInProcess, WorkflowCatalog.CheckOutProcess, WorkflowCatalog.RoomMoveProcess };
        foTypes.Should().OnlyHaveUniqueItems();
        foTypes.Should().AllSatisfy(t => t.Should().StartWith("FO."));
    }

    // =============================================================================
    // HELPERS
    // =============================================================================
    private static ProcessInstance CreateTestProcess() => ProcessInstance.Start("KEY-TEST", "Test", 1, new(PropertyId.New().Value), DateOnly.FromDateTime(DateTime.UtcNow), "Test", "T-001", new(Guid.NewGuid()), DateTime.UtcNow);

    private static TestDefinition CreateTestDefinition(string type = "TestProcess", int version = 1) => new(type, version);

    private sealed class TestDefinition
    {
        private readonly HashSet<string> _terminals;
        private readonly Dictionary<(string, string), (string From, string Signal, string To)> _transitions;
        public string ProcessType { get; }
        public int Version { get; }

        public TestDefinition(string type, int version)
        {
            ProcessType = type; Version = version;
            _terminals = ["Completed", "Failed", "Cancelled"];
            _transitions = new() { [("Started", "Submit")] = ("Started", "Submit", "Processing"), [("Processing", "Complete")] = ("Processing", "Complete", "Completed"), [("Processing", "Fail")] = ("Processing", "Fail", "Failed"), [("Started", "Cancel")] = ("Started", "Cancel", "Cancelled") };
        }

        public bool IsTerminal(string state) => _terminals.Contains(state);
        public bool TryGetTransition(string state, string signal, out (string From, string Signal, string To) t) => _transitions.TryGetValue((state, signal), out t!);
    }

    private sealed class TestWorkflowRegistry
    {
        private readonly Dictionary<string, TestDefinition> _defs = [];
        public void Register(TestDefinition d) => _defs[$"{d.ProcessType}:v{d.Version}"] = d;
        public TestDefinition? Get(string type, int ver) => _defs.TryGetValue($"{type}:v{ver}", out var d) ? d : null;
        public IReadOnlyList<string> GetRegisteredTypes() => _defs.Keys.ToList();
    }
}
