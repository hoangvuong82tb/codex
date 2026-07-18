using System.Text.Json;
using ASMILE.BO.Application.FrontOffice.Foundation.Audit;
using ASMILE.BO.Application.FrontOffice.Foundation.Contexts;
using ASMILE.BO.Application.FrontOffice.Foundation.Persistence;
using ASMILE.BO.Core.FrontOffice.Foundation.DomainEvents;
using ASMILE.BO.Core.FrontOffice.Foundation.Identifiers;
using ASMILE.BO.Infrastructure.FrontOffice.Foundation;
using ASMILE.BO.Infrastructure.Persistence;
using ASMILE.BO.Shared.Foundation;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace ASMILE.BO.Tests.Unit;

public sealed class FrontOfficeFoundationTests
{
    [Fact]
    public void Result_and_error_preserve_business_rejection_without_exception()
    {
        var result = Result<string>.Failure(Error.Business("FO_RULE", "Rejected by a business rule."));
        result.IsFailure.Should().BeTrue();
        result.Error!.Category.Should().Be(ErrorCategory.Business);
        result.Error.Retryable.Should().BeFalse();
    }

    [Fact]
    public void Strongly_typed_id_is_uuid_v7_parseable_and_json_round_trippable()
    {
        var id = PropertyId.New();
        Span<byte> bytes = stackalloc byte[16];
        id.Value.TryWriteBytes(bytes, bigEndian: true, out _);
        ((bytes[6] >> 4) & 0x0F).Should().Be(7);
        PropertyId.TryParse(id.ToString(), out var parsed).Should().BeTrue();
        parsed.Should().Be(id);
        JsonSerializer.Deserialize<PropertyId>(JsonSerializer.Serialize(id)).Should().Be(id);
    }

    [Fact]
    public void Execution_context_scope_restores_previous_context()
    {
        var accessor = new FrontOfficeExecutionContextAccessor();
        accessor.Current.Should().BeNull();
        using (accessor.BeginScope(CreateContext())) accessor.Current!.BusinessDate.BusinessDate.Should().Be(new DateOnly(2026, 7, 17));
        accessor.Current.Should().BeNull();
    }

    [Fact]
    public void Architecture_boundaries_keep_domain_and_application_free_of_ef_and_wpf()
    {
        var domainReferences = typeof(PropertyId).Assembly.GetReferencedAssemblies().Select(x => x.Name).ToArray();
        var applicationReferences = typeof(IFrontOfficePersistenceSession).Assembly.GetReferencedAssemblies().Select(x => x.Name).ToArray();
        domainReferences.Should().NotContain(x => x!.Contains("EntityFramework", StringComparison.OrdinalIgnoreCase) || x.Contains("Presentation", StringComparison.OrdinalIgnoreCase) || x.Contains("WindowsBase", StringComparison.OrdinalIgnoreCase));
        applicationReferences.Should().NotContain(x => x!.Contains("Presentation", StringComparison.OrdinalIgnoreCase) || x.Contains("WindowsBase", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Technical_records_are_durable_and_outbox_and_audit_commit_together()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;
        await using var db = new AppDbContext(options);
        await db.Database.EnsureCreatedAsync();
        var accessor = new FrontOfficeExecutionContextAccessor();
        var clock = new FixedClock(new DateTime(2026, 7, 17, 8, 0, 0, DateTimeKind.Utc));
        var audit = new FrontOfficeAuditWriter();
        var session = new EfFrontOfficePersistenceSession(db, accessor, new PassthroughIntegrationEventMapper(), new CanonicalOutboxMessageFactory(clock), audit, clock);
        var eventId = EventId.New();
        using (accessor.BeginScope(CreateContext()))
        {
            session.StageDomainEvent(new TestEvent(eventId, clock.UtcNow));
            session.StageAudit(new FrontOfficeAuditIntent("FoundationCommit", "Foundation", "none", "Succeeded"));
            (await session.CommitAsync()).IsSuccess.Should().BeTrue();
        }
        (await db.FrontOfficeOutboxMessages.SingleAsync()).PayloadHash.Should().HaveLength(64);
        (await db.AuditLogs.SingleAsync(x => x.Action == "FoundationCommit")).CorrelationId.Should().NotBeNullOrWhiteSpace();
        var inbox = new EfInboxStore(db, clock);
        var messageId = MessageId.New();
        (await inbox.ReserveAsync("foundation-test", messageId, "hash")).Should().Be(InboxReservationResult.Reserved);
        (await inbox.ReserveAsync("foundation-test", messageId, "hash")).Should().Be(InboxReservationResult.InProgress);
        (await inbox.ReserveAsync("foundation-test", messageId, "different")).Should().Be(InboxReservationResult.PayloadHashMismatch);
        await inbox.CompleteAsync("foundation-test", messageId);
        (await inbox.ReserveAsync("foundation-test", messageId, "hash")).Should().Be(InboxReservationResult.DuplicateCompleted);
        var idempotency = new EfIdempotencyStore(db, clock);
        var key = IdempotencyKey.From("foundation-test-key");
        (await idempotency.ReserveAsync("front-office", key, CommandId.New(), "request-hash", clock.UtcNow.AddHours(1))).Should().Be(IdempotencyReservationResult.Reserved);
        (await idempotency.ReserveAsync("front-office", key, CommandId.New(), "other-hash", clock.UtcNow.AddHours(1))).Should().Be(IdempotencyReservationResult.RequestHashMismatch);
        await idempotency.CompleteAsync("front-office", key, "Result", "safe-reference");
        (await idempotency.ReserveAsync("front-office", key, CommandId.New(), "request-hash", clock.UtcNow.AddHours(1))).Should().Be(IdempotencyReservationResult.DuplicateCompleted);
    }

    private static FrontOfficeExecutionContext CreateContext() => new(new ActorContext(ActorId.New(), new HashSet<string> { "Reception" }, "session", "workstation", false), new PropertyContext(PropertyId.New(), "Asia/Ho_Chi_Minh", "VND"), new BusinessDateContext(PropertyId.New(), new DateOnly(2026, 7, 17), "Test"), new CorrelationContext(CorrelationId.New(), null, CommandId.New(), "trace"));
    private sealed class FixedClock(DateTime utcNow) : IClock { public DateTime UtcNow => utcNow; }
    private sealed record TestEvent(EventId EventId, DateTime OccurredAtUtc) : IFrontOfficeDomainEvent { public string EventName => "FrontOffice.Foundation.Tested"; public object Payload => new { Source = "test" }; }
}
