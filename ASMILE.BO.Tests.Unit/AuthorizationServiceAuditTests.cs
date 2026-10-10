using ASMILE.BO.Application.DTOs.Security;
using ASMILE.BO.Application.Interfaces;
using ASMILE.BO.Core.Security;
using ASMILE.BO.Infrastructure.Security;
using FluentAssertions;

namespace ASMILE.BO.Tests.Unit;

public sealed class AuthorizationServiceAuditTests
{
    [Theory]
    [InlineData(true, SecurityEventType.DangerousActionConfirmed)]
    [InlineData(false, SecurityEventType.DangerousActionRejected)]
    public async Task ConfirmDangerousAction_persists_decision_without_logging_prompt_content(
        bool confirmed,
        SecurityEventType expectedEvent)
    {
        var prompt = new FakeSecurityPromptService(confirmed);
        var audit = new RecordingSecurityAuditService();
        var service = new AuthorizationService(new FakeCurrentUserService(), prompt, audit);

        var result = await service.ConfirmDangerousActionAsync("Restore customer database", "secret customer data");

        result.Should().Be(confirmed);
        prompt.Calls.Should().Be(1);
        audit.Events.Should().ContainSingle();
        audit.Events[0].EventType.Should().Be(expectedEvent);
        audit.Events[0].Module.Should().Be("Authorization");
        audit.Events[0].EntityType.Should().Be("DangerousActionConfirmation");
        audit.Events[0].EntityId.Should().BeNull();
        audit.Events[0].Message.Should().NotContain("Restore customer database");
        audit.Events[0].Message.Should().NotContain("secret customer data");
    }

    [Fact]
    public async Task Confirmed_action_fails_closed_when_audit_cannot_be_persisted()
    {
        var service = new AuthorizationService(
            new FakeCurrentUserService(),
            new FakeSecurityPromptService(true),
            new RecordingSecurityAuditService { Failure = new InvalidOperationException("audit unavailable") });

        var act = () => service.ConfirmDangerousActionAsync("Protected action", "Details");

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    private sealed class FakeCurrentUserService : ICurrentUserService
    {
        public UserAccountDto? CurrentUser => null;
        public Guid? CurrentSessionId => null;
        public bool IsAuthenticated => false;
        public bool HasPermission(string permissionCode) => false;
        public void SetCurrentUser(LoginResultDto login) { }
        public void Clear() { }
    }

    private sealed class FakeSecurityPromptService(bool result) : ISecurityPromptService
    {
        public int Calls { get; private set; }

        public Task<bool> ConfirmDangerousActionAsync(string title, string message)
        {
            Calls++;
            return Task.FromResult(result);
        }
    }

    private sealed class RecordingSecurityAuditService : ISecurityAuditService
    {
        public List<AuditEvent> Events { get; } = [];
        public Exception? Failure { get; init; }

        public Task LogAsync(
            SecurityEventType eventType,
            string module,
            string entityType,
            string? entityId,
            string message,
            IDictionary<string, string>? metadata = null,
            CancellationToken cancellationToken = default)
        {
            if (Failure is not null)
            {
                throw Failure;
            }

            Events.Add(new AuditEvent(eventType, module, entityType, entityId, message));
            return Task.CompletedTask;
        }
    }

    private sealed record AuditEvent(
        SecurityEventType EventType,
        string Module,
        string EntityType,
        string? EntityId,
        string Message);
}
