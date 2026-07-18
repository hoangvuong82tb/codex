using ASMILE.BO.Application.DTOs;
using ASMILE.BO.Application.DTOs.Security;
using ASMILE.BO.Application.Interfaces;
using ASMILE.BO.Core.Entities;
using ASMILE.BO.Infrastructure.Persistence;
using ASMILE.BO.Infrastructure.Services;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace ASMILE.BO.Tests.Unit;

public sealed class TT200ComplianceTests
{
    [Fact]
    public async Task GetAccountMappingsAsync_SeedsFoundation()
    {
        await using var fixture = await SqliteFixture.CreateAsync();
        var mappings = await fixture.CreateService().GetAccountMappingsAsync();

        mappings.Should().NotBeEmpty();
        mappings.Should().Contain(x => x.AccountCode == "111");
    }

    [Fact]
    public async Task ValidateComplianceAsync_GeneratesIssuesForMissingMapping()
    {
        await using var fixture = await SqliteFixture.CreateAsync();
        fixture.Db.LedgerAccounts.AddRange(
            new LedgerAccount
            {
                Id = Guid.NewGuid(),
                AccountCode = "1111",
                AccountName = "Tiền mặt",
                ReportName = "Tiền mặt",
                AccountType = "Asset",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                IsActive = true
            },
            new LedgerAccount
            {
                Id = Guid.NewGuid(),
                AccountCode = "1121",
                AccountName = "Tiền gửi",
                ReportName = "Tiền gửi",
                AccountType = "Asset",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                IsActive = true
            });
        await fixture.Db.SaveChangesAsync();

        await fixture.CreateService().GetAccountMappingsAsync();
        var mapping = await fixture.Db.TT200AccountMappings.FirstAsync(x => x.AccountCode == "1121");
        fixture.Db.TT200AccountMappings.Remove(mapping);
        await fixture.Db.SaveChangesAsync();

        var issues = await fixture.CreateService().ValidateComplianceAsync();

        issues.Should().Contain(x => x.RuleCode == "TT200_MISSING_MAPPING");
    }

    [Fact]
    public async Task CreateSnapshotAsync_PersistsImmutableSnapshot()
    {
        await using var fixture = await SqliteFixture.CreateAsync();
        var snapshot = await fixture.CreateService().CreateSnapshotAsync("2026-05");

        snapshot.SnapshotHash.Should().NotBeNullOrWhiteSpace();
        (await fixture.Db.TT200ComplianceSnapshots.SingleAsync(x => x.Id == snapshot.Id)).PeriodCode.Should().Be("2026-05");
    }

    [Fact]
    public async Task ReportEngine_ExposesTt200ReportCodes()
    {
        await using var fixture = await SqliteFixture.CreateAsync();
        await fixture.CreateService().GetAccountMappingsAsync();

        var engine = new ReportEngine(fixture.Db);
        var result = await engine.ExecuteAsync(new ReportRequestDto
        {
            ReportCode = "TT200_ACCOUNT_MAPPING",
            ExecutedBy = "tester"
        });

        result.ReportCode.Should().Be("TT200_ACCOUNT_MAPPING");
        result.Rows.Should().NotBeEmpty();
    }

    [Fact]
    public async Task ValidateComplianceAsync_HonorsCancellation()
    {
        await using var fixture = await SqliteFixture.CreateAsync();
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        var act = () => fixture.CreateService().ValidateComplianceAsync(cancellationToken: cts.Token);
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task Snapshots_AreCompanyIsolated()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"tt200-company-{Guid.NewGuid():N}.db");
        var companyA = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var companyB = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

        await using (var fixtureA = await SqliteFixture.CreateAsync(new TestCompanyContext(companyA), dbPath, deleteOnDispose: false))
        {
            await fixtureA.CreateService().CreateSnapshotAsync("2026-05");
        }

        await using (var fixtureB = await SqliteFixture.CreateAsync(new TestCompanyContext(companyB), dbPath, ensureCreated: false, deleteOnDispose: false))
        {
            await fixtureB.CreateService().CreateSnapshotAsync("2026-05");
        }

        await using var verifyFixture = await SqliteFixture.CreateAsync(new TestCompanyContext(companyA), dbPath, ensureCreated: false);
        (await verifyFixture.CreateService().GetSnapshotsAsync()).Should().ContainSingle(x => x.PeriodCode == "2026-05");
    }

    private sealed class SqliteFixture : IAsyncDisposable
    {
        private readonly string _databasePath;
        private readonly SqliteConnection _connection;
        private readonly bool _deleteOnDispose;
        public AppDbContext Db { get; }

        private SqliteFixture(string databasePath, SqliteConnection connection, AppDbContext db, bool deleteOnDispose)
        {
            _databasePath = databasePath;
            _connection = connection;
            _deleteOnDispose = deleteOnDispose;
            Db = db;
        }

        public static async Task<SqliteFixture> CreateAsync(ICurrentCompanyContext? companyContext = null, string? databasePath = null, bool ensureCreated = true, bool deleteOnDispose = true)
        {
            var path = databasePath ?? Path.Combine(Path.GetTempPath(), $"tt200-tests-{Guid.NewGuid():N}.db");
            var connection = new SqliteConnection($"Data Source={path}");
            await connection.OpenAsync();
            var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;
            var db = companyContext is null ? new AppDbContext(options) : new AppDbContext(options, companyContext);
            if (ensureCreated)
            {
                await db.Database.EnsureCreatedAsync();
            }

            return new SqliteFixture(path, connection, db, deleteOnDispose);
        }

        public TT200ComplianceService CreateService()
            => new(Db, null, new AuditCorrelationService(), null, new TestCurrentUserService(), null);

        public async ValueTask DisposeAsync()
        {
            await Db.DisposeAsync();
            await _connection.DisposeAsync();
            try
            {
                if (_deleteOnDispose && File.Exists(_databasePath))
                {
                    File.Delete(_databasePath);
                }
            }
            catch
            {
            }
        }
    }

    private sealed class TestCurrentUserService : ICurrentUserService
    {
        public UserAccountDto? CurrentUser { get; private set; } = new() { Id = Guid.NewGuid(), UserName = "tt200.user", DisplayName = "TT200 User", IsActive = true, Roles = [] };
        public Guid? CurrentSessionId { get; private set; } = Guid.NewGuid();
        public bool IsAuthenticated => CurrentUser is not null;
        public bool HasPermission(string permissionCode) => true;
        public void SetCurrentUser(LoginResultDto login) { }
        public void Clear()
        {
            CurrentUser = null;
            CurrentSessionId = null;
        }
    }

    private sealed class TestCompanyContext : ICurrentCompanyContext
    {
        public TestCompanyContext(Guid companyId)
        {
            CompanyId = companyId;
        }

        public Guid? CompanyId { get; private set; }
        public Guid? BranchId { get; private set; }
        public string CompanyName => "Test";
        public string? BranchName => null;
        public Task EnsureInitializedAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task SetCompanyAsync(Guid companyId, CancellationToken cancellationToken = default)
        {
            CompanyId = companyId;
            return Task.CompletedTask;
        }
        public Task SetBranchAsync(Guid? branchId, CancellationToken cancellationToken = default)
        {
            BranchId = branchId;
            return Task.CompletedTask;
        }
        public Task<IReadOnlyList<CompanyContextOptionDto>> GetCompaniesAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<CompanyContextOptionDto>>([]);
        public Task<IReadOnlyList<BranchContextOptionDto>> GetBranchesAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<BranchContextOptionDto>>([]);
    }
}
