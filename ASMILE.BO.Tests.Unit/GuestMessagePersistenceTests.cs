using ASMILE.BO.Application.FrontOffice.Foundation.Contexts;
using ASMILE.BO.Application.FrontOffice.Message;
using ASMILE.BO.Core.FrontOffice.Foundation.Identifiers;
using ASMILE.BO.Infrastructure.FrontOffice.Foundation;
using ASMILE.BO.Infrastructure.FrontOffice.Message;
using ASMILE.BO.Infrastructure.Persistence;
using ASMILE.BO.Shared.Foundation;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace ASMILE.BO.Tests.Unit;

public sealed class GuestMessagePersistenceTests
{
    [Fact]
    public async Task MSG_001_create_read_and_reply_use_real_persistence()
    {
        var propertyId = PropertyId.New();
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .Options);
        await db.Database.EnsureCreatedAsync();

        var accessor = new FrontOfficeExecutionContextAccessor();
        var clock = new FixedClock(new DateTime(2026, 9, 8, 8, 0, 0, DateTimeKind.Utc));
        var persistence = new EfFrontOfficePersistenceSession(
            db,
            accessor,
            new PassthroughIntegrationEventMapper(),
            new CanonicalOutboxMessageFactory(clock),
            new FrontOfficeAuditWriter(),
            clock);
        var service = new EfGuestMessageService(db, persistence, accessor, clock);
        using var scope = accessor.BeginScope(CreateContext(propertyId));

        var created = await service.CreateAndSendAsync(new CreateGuestMessageRequest(
            null, null, null, null, "Nguyễn Văn A", "101", "VIP Notice", "FrontDesk", "High",
            "Chào mừng", "Chúc quý khách có kỳ nghỉ vui vẻ.", true, "FrontOffice"), CancellationToken.None);

        created.Status.Should().Be("Sent");
        created.History.Should().Contain("Sent");
        (await service.GetAllAsync(propertyId.Value, CancellationToken.None)).Should().ContainSingle();

        var read = await service.MarkReadAsync(created.GuestMessageId, CancellationToken.None);
        read.Status.Should().Be("Read");
        read.History.Should().Contain("Read");

        var replied = await service.ReplyAsync(created.GuestMessageId, "Đã xác nhận.", CancellationToken.None);
        replied.Status.Should().Be("Replied");
        replied.ResponseContent.Should().Be("Đã xác nhận.");
        replied.History.Should().Contain("Replied");
        (await db.FrontOfficeMessageHistories.CountAsync()).Should().Be(3);
        (await db.AuditLogs.CountAsync(x => x.EntityName == "GuestMessage")).Should().Be(3);
    }

    private static FrontOfficeExecutionContext CreateContext(PropertyId propertyId) => new(
        new ActorContext(ActorId.New(), new HashSet<string> { "Reception" }, "session", "workstation", false),
        new PropertyContext(propertyId, "Asia/Ho_Chi_Minh", "VND"),
        new BusinessDateContext(propertyId, new DateOnly(2026, 9, 8), "Test"),
        new CorrelationContext(CorrelationId.New(), null, CommandId.New(), "trace"));

    private sealed class FixedClock(DateTime utcNow) : IClock
    {
        public DateTime UtcNow { get; } = utcNow;
    }
}
