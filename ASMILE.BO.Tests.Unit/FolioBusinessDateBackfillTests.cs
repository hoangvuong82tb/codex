using ASMILE.BO.Application.DTOs.Security;
using ASMILE.BO.Application.FrontOffice.Foundation.Contexts;
using ASMILE.BO.Application.FrontOffice.Reporting;
using ASMILE.BO.Application.Interfaces;
using ASMILE.BO.Core.FrontOffice.Folio;
using ASMILE.BO.Core.FrontOffice.Foundation.Identifiers;
using ASMILE.BO.Core.FrontOffice.Payment;
using ASMILE.BO.Core.Interfaces.Repositories;
using ASMILE.BO.Core.Interfaces.Services;
using ASMILE.BO.Core.Security;
using ASMILE.BO.Infrastructure.FrontOffice.Foundation;
using ASMILE.BO.Infrastructure.FrontOffice.Reporting;
using ASMILE.BO.Infrastructure.Persistence;
using ASMILE.BO.Infrastructure.Services;
using ASMILE.BO.Shared.Foundation;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using ASMILE.BO.Tests.Unit.TestSupport;

namespace ASMILE.BO.Tests.Unit;

public sealed class FolioBusinessDateBackfillTests
{
    [Fact]
    public async Task Apply_updates_only_missing_rows_and_writes_audit()
    {
        var propertyId = Guid.NewGuid();
        var businessDate = new DateOnly(2026, 9, 23);
        await using var db = TestDbContextFactory.CreateContext(null, seedAccountingCore: false);
        var folio = HotelFolio.Create(propertyId, Guid.NewGuid(), "FOL-BACKFILL-001", DateTime.UtcNow, null);
        var entry = folio.AddManualCharge(100_000m, "Legacy charge", DateTime.UtcNow, null);
        db.HotelFolios.Add(folio);
        await db.SaveChangesAsync();

        var accessor = new FrontOfficeExecutionContextAccessor();
        using var scope = accessor.BeginScope(CreateContext(propertyId, businessDate));
        var service = CreateService(db, accessor);
        var result = await service.ApplyAsync(new FolioBusinessDateBackfillCommand(
            propertyId,
            [new FolioBusinessDateBackfillItem(entry.HotelFolioEntryId, businessDate)],
            "Đối chiếu sổ lịch sử",
            "BB-2026-09-23-001"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(new FolioBusinessDateBackfillResult(1, 0));
        (await db.HotelFolioEntries.SingleAsync()).BusinessDate.Should().Be(businessDate);
        (await db.AuditLogs.SingleAsync(x => x.Action == "FOLIO_BUSINESS_DATE_BACKFILL")).Detail.Should().Contain("BB-2026-09-23-001");

        var retry = await service.ApplyAsync(new FolioBusinessDateBackfillCommand(
            propertyId,
            [new FolioBusinessDateBackfillItem(entry.HotelFolioEntryId, businessDate)],
            "Đối chiếu sổ lịch sử",
            "BB-2026-09-23-001"), CancellationToken.None);
        retry.IsSuccess.Should().BeTrue();
        retry.Value.Should().Be(new FolioBusinessDateBackfillResult(0, 1));
        (await db.AuditLogs.CountAsync(x => x.Action == "FOLIO_BUSINESS_DATE_BACKFILL")).Should().Be(1);
    }

    [Fact]
    public async Task Apply_rejects_a_conflicting_date_without_overwriting_history()
    {
        var propertyId = Guid.NewGuid();
        var existingDate = new DateOnly(2026, 9, 22);
        await using var db = TestDbContextFactory.CreateContext(null, seedAccountingCore: false);
        var folio = HotelFolio.Create(propertyId, Guid.NewGuid(), "FOL-BACKFILL-002", DateTime.UtcNow, null);
        var entry = folio.AddManualCharge(100_000m, "Already dated charge", DateTime.UtcNow, null, existingDate);
        db.HotelFolios.Add(folio);
        await db.SaveChangesAsync();

        var accessor = new FrontOfficeExecutionContextAccessor();
        using var scope = accessor.BeginScope(CreateContext(propertyId, existingDate));
        var service = CreateService(db, accessor);
        var result = await service.ApplyAsync(new FolioBusinessDateBackfillCommand(
            propertyId,
            [new FolioBusinessDateBackfillItem(entry.HotelFolioEntryId, existingDate.AddDays(1))],
            "Đối chiếu sổ lịch sử",
            "BB-2026-09-23-002"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("REPORT_BACKFILL_DATE_CONFLICT");
        (await db.HotelFolioEntries.SingleAsync()).BusinessDate.Should().Be(existingDate);
    }

    private static EfFolioBusinessDateBackfillService CreateService(AppDbContext db, IFrontOfficeExecutionContextAccessor accessor)
        => new(
            db,
            accessor,
            new AllowAllAuthorization(),
            new TestCurrentUserService(),
            TestDbContextFactory.AuditLogService(db),
            new DirectUnitOfWork(db));

    private static FrontOfficeExecutionContext CreateContext(Guid propertyId, DateOnly businessDate)
    {
        var id = new PropertyId(propertyId);
        return new FrontOfficeExecutionContext(
            new ActorContext(ActorId.New(), new HashSet<string> { "Reception" }, "test-session", "test-workstation", false),
            new PropertyContext(id, "Asia/Ho_Chi_Minh", "VND"),
            new BusinessDateContext(id, businessDate, "Test"),
            new CorrelationContext(CorrelationId.New(), null, CommandId.New(), "backfill-test"));
    }

    private sealed class DirectUnitOfWork(AppDbContext db) : IUnitOfWork
    {
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => db.SaveChangesAsync(cancellationToken);
        public Task ExecuteInTransactionAsync(Func<CancellationToken, Task> operation, CancellationToken cancellationToken = default) => operation(cancellationToken);
    }

    private sealed class AllowAllAuthorization : IAuthorizationService
    {
        public bool HasPermission(string permissionCode) => true;
        public void Demand(string permissionCode) { }
        public Task<bool> CanAccessAsync(string permissionCode) => Task.FromResult(true);
        public bool HasPermission(Permission permission) => true;
        public void Demand(Permission permission) { }
        public Task<bool> ConfirmDangerousActionAsync(string title, string message) => Task.FromResult(true);
    }

    private sealed class TestCurrentUserService : ICurrentUserService
    {
        public UserAccountDto? CurrentUser => new() { UserName = "backfill-tester" };
        public Guid? CurrentSessionId => null;
        public bool IsAuthenticated => true;
        public bool HasPermission(string permissionCode) => true;
        public void SetCurrentUser(LoginResultDto login) { }
        public void Clear() { }
    }
}
