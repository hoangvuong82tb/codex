using ASMILE.BO.Application.FrontOffice.Foundation.Audit;
using ASMILE.BO.Application.FrontOffice.Foundation.Contexts;
using ASMILE.BO.Application.FrontOffice.Foundation.Persistence;
using ASMILE.BO.Application.FrontOffice.Marketing;
using ASMILE.BO.Core.FrontOffice.Foundation.DomainEvents;
using ASMILE.BO.Core.FrontOffice.Foundation.Identifiers;
using ASMILE.BO.Core.FrontOffice.Marketing;
using ASMILE.BO.Infrastructure.FrontOffice.Marketing;
using ASMILE.BO.Infrastructure.FrontOffice.Foundation;
using ASMILE.BO.Infrastructure.Persistence;
using ASMILE.BO.Shared.Foundation;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace ASMILE.BO.Tests.Unit;

public sealed class MarketingServiceTests
{
    [Fact]
    public async Task MarketingRecords_ArePersistedScopedAndStatusCanChange()
    {
        var propertyId = PropertyId.New();
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options);
        await db.Database.EnsureCreatedAsync();

        var accessor = new FrontOfficeExecutionContextAccessor();
        using var scope = accessor.BeginScope(CreateContext(propertyId));
        var persistence = new TestPersistenceSession(db);
        var service = new EfMarketingService(db, persistence, accessor, new FixedClock());

        var created = await service.CreateAsync(
            propertyId.Value,
            new CreateMarketingRecordRequest(
                MarketingSection.Source,
                "OTA-BOOKING",
                "Booking.com",
                "OTA",
                "Sales",
                "Nguồn thật từ đặt phòng"),
            CancellationToken.None);

        created.Code.Should().Be("OTA-BOOKING");
        (await service.SearchAsync(propertyId.Value, new MarketingSearchFilter(MarketingSection.Source), CancellationToken.None))
            .Should().ContainSingle(x => x.Name == "Booking.com");

        var updated = await service.UpdateAsync(
            propertyId.Value,
            new UpdateMarketingRecordRequest(
                created.MarketingRecordId,
                created.Code,
                "Booking.com Partner",
                created.GroupName,
                "Sales Ops",
                "Đã cập nhật từ service thật",
                created.RowVersion),
            CancellationToken.None);
        updated.Name.Should().Be("Booking.com Partner");

        await service.ChangeStatusAsync(
            propertyId.Value,
            new ChangeMarketingRecordStatusRequest(
                created.MarketingRecordId,
                MarketingRecordStatus.Paused,
                updated.RowVersion),
            CancellationToken.None);

        var paused = await service.SearchAsync(
            propertyId.Value,
            new MarketingSearchFilter(MarketingSection.Source, Status: MarketingRecordStatus.Paused),
            CancellationToken.None);
        paused.Should().ContainSingle(x => x.MarketingRecordId == created.MarketingRecordId);
        persistence.Audits.Should().HaveCount(3);
    }

    [Fact]
    public async Task MarketingRecords_RejectDuplicateCodeWithinPropertyAndSection()
    {
        var propertyId = PropertyId.New();
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options);
        await db.Database.EnsureCreatedAsync();

        var accessor = new FrontOfficeExecutionContextAccessor();
        using var scope = accessor.BeginScope(CreateContext(propertyId));
        var service = new EfMarketingService(db, new TestPersistenceSession(db), accessor, new FixedClock());
        var request = new CreateMarketingRecordRequest(MarketingSection.Agency, "AGY-001", "Agency 1", "Corporate", "Sales");
        await service.CreateAsync(propertyId.Value, request, CancellationToken.None);

        var act = () => service.CreateAsync(propertyId.Value, request, CancellationToken.None);
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("MARKETING_DUPLICATE_CODE");
    }

    private static FrontOfficeExecutionContext CreateContext(PropertyId propertyId) => new(
        new ActorContext(ActorId.New(), new HashSet<string> { "Marketing" }, "session", "workstation", false),
        new PropertyContext(propertyId, "Asia/Ho_Chi_Minh", "VND"),
        new BusinessDateContext(propertyId, new DateOnly(2026, 9, 8), "Test"),
        new CorrelationContext(CorrelationId.New(), null, CommandId.New(), "trace"));

    private sealed class FixedClock : IClock
    {
        public DateTime UtcNow => new(2026, 9, 8, 8, 0, 0, DateTimeKind.Utc);
    }

    private sealed class TestPersistenceSession(AppDbContext db) : IFrontOfficePersistenceSession
    {
        public List<FrontOfficeAuditIntent> Audits { get; } = [];
        public void StageDomainEvent(IFrontOfficeDomainEvent domainEvent) { }
        public void StageAudit(FrontOfficeAuditIntent intent) => Audits.Add(intent);
        public async Task<Result<int>> CommitAsync(CancellationToken cancellationToken = default)
            => Result<int>.Success(await db.SaveChangesAsync(cancellationToken));
    }
}
