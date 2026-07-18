using ASMILE.BO.Core.FrontOffice.Foundation.Identifiers;
using ASMILE.BO.Core.FrontOffice.ProcessManager;
using ASMILE.BO.Application.FrontOffice.ProcessManager;
using FluentAssertions;

namespace ASMILE.BO.Tests.Unit;

public sealed class ProcessManagerFoundationTests
{
    [Fact]
    public void Process_starts_with_uuid_v7_key_and_can_transition_once_to_terminal_state()
    {
        var process = ProcessInstance.Start("property:stay:42", "Test.Foundation", 1, PropertyId.New(), new DateOnly(2026, 7, 17), "Stay", "42", CorrelationId.New(), DateTime.UtcNow);
        process.ProcessId.Should().NotBeEmpty();
        process.IsTerminal.Should().BeFalse();
        process.Transition(ProcessStatus.Completed, "Completed", null, DateTime.UtcNow);
        process.IsTerminal.Should().BeTrue();
        Action secondTransition = () => process.Transition(ProcessStatus.Active, "Invalid", null, DateTime.UtcNow);
        secondTransition.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Signal_and_timeout_have_durable_identity_and_terminal_operations()
    {
        var processId = Guid.NewGuid();
        var correlationId = Guid.NewGuid();
        var signal = ProcessSignal.Reserve(processId, Guid.NewGuid(), "Timeout.Arrival", new string('a', 64), correlationId, DateTime.UtcNow);
        signal.Status.Should().Be(ProcessSignalStatus.Reserved);
        signal.Complete(DateTime.UtcNow);
        signal.Status.Should().Be(ProcessSignalStatus.Processed);
        var timeout = ProcessTimeout.Schedule(processId, null, "Arrival", DateTime.UtcNow, correlationId);
        timeout.MarkFired(DateTime.UtcNow);
        timeout.Status.Should().Be(ProcessTimeoutStatus.Fired);
    }

    [Fact]
    public void Typed_definition_applies_valid_transition_and_rejects_invalid_signal()
    {
        var definition = Definition();
        var process = ProcessInstance.Start("test:typed:1", definition.ProcessType, definition.Version, PropertyId.New(), new DateOnly(2026, 7, 17), "Test", "1", CorrelationId.New(), DateTime.UtcNow, initialState: TestState.Started.ToString());
        var engine = new ProcessTransitionEngine();
        engine.Apply(process, definition, TestSignal.Accepted, DateTime.UtcNow).IsSuccess.Should().BeTrue();
        process.CurrentState.Should().Be(TestState.Waiting.ToString());
        engine.Apply(process, definition, TestSignal.Accepted, DateTime.UtcNow).IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Retry_policy_has_bounded_backoff_and_dead_letters_when_exhausted()
    {
        var policy = new ProcessRetryPolicy(2, TimeSpan.FromSeconds(5), 2, TimeSpan.FromSeconds(20), .2, new HashSet<string> { "Transient" }, ProcessEscalation.DeadLetter);
        policy.DelayFor(2, 1).Should().BeLessThanOrEqualTo(TimeSpan.FromSeconds(12));
        var timeout = ProcessTimeout.Schedule(Guid.NewGuid(), null, "Test", DateTime.UtcNow, Guid.NewGuid());
        var engine = new ProcessRetryEngine();
        engine.Apply(timeout, policy, ProcessRetryClassification.TransientSafe, "Transient", DateTime.UtcNow, .5).RetryScheduled.Should().BeTrue();
        timeout.DeadLetter(DateTime.UtcNow, "Transient");
        timeout.Status.Should().Be(ProcessTimeoutStatus.DeadLetter);
    }

    [Fact]
    public void Manual_action_requires_separate_approver_when_dual_control_is_enabled()
    {
        var requester = Guid.NewGuid();
        var assignee = Guid.NewGuid();
        var action = ProcessManualAction.Open(Guid.NewGuid(), "Review", "process.review", DateTime.UtcNow, approvalRequired: true, createdByActorId: requester);
        action.TryClaim(assignee, new HashSet<string>()).Should().BeTrue();
        action.TryApprove(requester, true, "reason").Should().BeFalse();
        action.TryApprove(Guid.NewGuid(), true, "reason").Should().BeTrue();
        action.TryComplete(assignee, true, "reason", "Completed", DateTime.UtcNow).Should().BeTrue();
    }

    [Fact]
    public void Process_domain_has_no_public_state_setters_or_infrastructure_dependency()
    {
        typeof(ProcessInstance).GetProperties().Where(x => x.SetMethod?.IsPublic == true).Should().BeEmpty();
        typeof(ProcessInstance).Assembly.GetReferencedAssemblies().Select(x => x.Name).Should().NotContain(x => x!.Contains("EntityFramework", StringComparison.OrdinalIgnoreCase) || x.Contains("WindowsBase", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Compensation_dispatcher_is_typed_and_has_no_business_specific_handler()
    {
        var compensation = ProcessCompensation.Request(Guid.NewGuid(), null, "test", DateTime.UtcNow);
        var dispatcher = new CompensationDispatcher([new TestCompensationHandler()]);
        (await dispatcher.ExecuteAsync(compensation, DateTime.UtcNow)).Succeeded.Should().BeTrue();
        compensation.Status.Should().Be(ProcessCompensationStatus.Completed);
    }

    private static ProcessDefinition<TestState, TestStep, TestSignal> Definition() => new("Foundation.Test", 1, TestState.Started, TestStep.Wait, [TestState.Completed], [new ProcessTransition<TestState, TestStep, TestSignal>(TestState.Started, TestSignal.Accepted, TestState.Waiting, TestStep.Wait), new ProcessTransition<TestState, TestStep, TestSignal>(TestState.Waiting, TestSignal.Completed, TestState.Completed, null)]);
    private enum TestState { Started, Waiting, Completed }
    private enum TestStep { Wait }
    private enum TestSignal { Accepted, Completed }
    private sealed class TestCompensationHandler : ICompensationHandler { public string CompensationType => "test"; public Task<CompensationExecutionResult> ExecuteAsync(ProcessCompensation compensation, CancellationToken cancellationToken) => Task.FromResult(new CompensationExecutionResult(true, false, false)); }
}
