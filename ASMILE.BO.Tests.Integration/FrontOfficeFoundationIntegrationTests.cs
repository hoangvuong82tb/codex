using ASMILE.BO.Application.FrontOffice.Foundation.Contexts;
using ASMILE.BO.Application.FrontOffice.Foundation.Persistence;
using ASMILE.BO.Core.FrontOffice.Foundation.Identifiers;
using ASMILE.BO.Infrastructure.FrontOffice.Foundation;
using ASMILE.BO.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace ASMILE.BO.Tests.Integration;

public sealed class FrontOfficeFoundationIntegrationTests
{
    [Fact]
    public async Task Sqlite_model_enforces_durable_inbox_and_idempotency_keys()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;
        await using var db = new AppDbContext(options);
        await db.Database.EnsureCreatedAsync();
        var clock = new FixedClock(new DateTime(2026, 7, 17, 8, 0, 0, DateTimeKind.Utc));
        var inbox = new EfInboxStore(db, clock);
        var messageId = MessageId.New();
        (await inbox.ReserveAsync("fo-process-manager", messageId, "a".PadLeft(64, '0'))).Should().Be(InboxReservationResult.Reserved);
        (await inbox.ReserveAsync("fo-process-manager", messageId, "b".PadLeft(64, '0'))).Should().Be(InboxReservationResult.PayloadHashMismatch);
        var idempotency = new EfIdempotencyStore(db, clock);
        var key = IdempotencyKey.From("integration-key");
        (await idempotency.ReserveAsync("fo-command", key, CommandId.New(), "a".PadLeft(64, '0'), clock.UtcNow.AddMinutes(10))).Should().Be(IdempotencyReservationResult.Reserved);
        (await idempotency.ReserveAsync("fo-command", key, CommandId.New(), "a".PadLeft(64, '0'), clock.UtcNow.AddMinutes(10))).Should().Be(IdempotencyReservationResult.InProgress);
    }

    private sealed class FixedClock(DateTime utcNow) : IClock { public DateTime UtcNow => utcNow; }
}
