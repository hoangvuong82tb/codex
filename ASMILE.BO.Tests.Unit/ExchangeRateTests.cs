using ASMILE.BO.Application.FrontOffice.Foundation.Audit;
using ASMILE.BO.Application.FrontOffice.Foundation.Contexts;
using ASMILE.BO.Application.FrontOffice.Foundation.Events;
using ASMILE.BO.Application.FrontOffice.Foundation.Persistence;
using ASMILE.BO.Application.FrontOffice.Rate;
using ASMILE.BO.Core.FrontOffice.Foundation.DomainEvents;
using ASMILE.BO.Core.FrontOffice.Foundation.Identifiers;
using ASMILE.BO.Infrastructure.FrontOffice.Foundation;
using ASMILE.BO.Infrastructure.FrontOffice.Rate;
using ASMILE.BO.Infrastructure.Persistence;
using ASMILE.BO.Shared.Foundation;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace ASMILE.BO.Tests.Unit;

public sealed class ExchangeRateTests
{
    [Fact]
    public async Task EXR_001_create_and_query_reads_real_property_rows()
    {
        var propertyId = PropertyId.New();
        await using var connection = new SqliteConnection("Data Source=:memory:"); await connection.OpenAsync();
        await using var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options); await db.Database.EnsureCreatedAsync();
        var accessor = new FrontOfficeExecutionContextAccessor(); using var scope = accessor.BeginScope(Context(propertyId)); var persistence = new TestPersistence(db); var clock = new FixedClock(new DateTime(2026, 9, 7, 8, 0, 0, DateTimeKind.Utc)); var service = new EfExchangeRateService(db, persistence, accessor, clock);

        var created = await service.CreateAsync(new CreateExchangeRateRequest(propertyId.Value, "usd", "US Dollar", "$", 25_000, 25_500, 25_700, clock.UtcNow.AddDays(-1), clock.UtcNow.AddDays(10), true, "seed test"), CancellationToken.None);
        created.CurrencyCode.Should().Be("USD"); created.IsDefault.Should().BeTrue(); persistence.Commits.Should().Be(1);
        var rows = await service.SearchAsync(new ExchangeRateSearchRequest(propertyId.Value), CancellationToken.None);
        rows.Should().ContainSingle(x => x.HotelExchangeRateId == created.HotelExchangeRateId && x.DisplayState == ExchangeRateDisplayState.Effective);
    }

    [Fact]
    public async Task EXR_002_set_default_clears_previous_default_and_honours_property_scope()
    {
        var propertyId = PropertyId.New();
        await using var connection = new SqliteConnection("Data Source=:memory:"); await connection.OpenAsync();
        await using var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options); await db.Database.EnsureCreatedAsync();
        var accessor = new FrontOfficeExecutionContextAccessor(); using var scope = accessor.BeginScope(Context(propertyId)); var persistence = new TestPersistence(db); var clock = new FixedClock(new DateTime(2026, 9, 7, 8, 0, 0, DateTimeKind.Utc)); var service = new EfExchangeRateService(db, persistence, accessor, clock);
        var first = await service.CreateAsync(new CreateExchangeRateRequest(propertyId.Value, "USD", "US Dollar", "$", 1, 2, 3, clock.UtcNow.AddDays(-2), null, true, null), CancellationToken.None);
        var second = await service.CreateAsync(new CreateExchangeRateRequest(propertyId.Value, "EUR", "Euro", "€", 4, 5, 6, clock.UtcNow.AddDays(-2), null, false, null), CancellationToken.None);
        await service.SetDefaultAsync(new SetDefaultExchangeRateRequest(second.HotelExchangeRateId), CancellationToken.None);
        var rows = await service.SearchAsync(new ExchangeRateSearchRequest(propertyId.Value), CancellationToken.None);
        rows.Single(x => x.HotelExchangeRateId == first.HotelExchangeRateId).IsDefault.Should().BeFalse(); rows.Single(x => x.HotelExchangeRateId == second.HotelExchangeRateId).IsDefault.Should().BeTrue();
        using var otherScope = accessor.BeginScope(Context(PropertyId.New())); await FluentActions.Invoking(() => service.SearchAsync(new ExchangeRateSearchRequest(propertyId.Value), CancellationToken.None)).Should().ThrowAsync<InvalidOperationException>().WithMessage("PROPERTY_CONTEXT_MISMATCH");
    }

    private static FrontOfficeExecutionContext Context(PropertyId id) => new(new ActorContext(ActorId.New(), new HashSet<string> { "Reception" }, "session", "workstation", false), new PropertyContext(id, "Asia/Ho_Chi_Minh", "VND"), new BusinessDateContext(id, new DateOnly(2026, 9, 7), "Test"), new CorrelationContext(CorrelationId.New(), null, CommandId.New(), "trace"));
    private sealed class FixedClock(DateTime now) : IClock { public DateTime UtcNow { get; } = now; }
    private sealed class TestPersistence(AppDbContext db) : IFrontOfficePersistenceSession { public int Commits { get; private set; } public void StageDomainEvent(IFrontOfficeDomainEvent domainEvent) { } public void StageAudit(FrontOfficeAuditIntent intent) { } public async Task<Result<int>> CommitAsync(CancellationToken cancellationToken = default) { Commits++; return Result<int>.Success(await db.SaveChangesAsync(cancellationToken)); } }
}
