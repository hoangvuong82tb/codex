using ASMILE.BO.Application.DTOs;
using ASMILE.BO.Application.DTOs.Security;
using ASMILE.BO.Application.Interfaces;
using ASMILE.BO.Core.Entities;
using ASMILE.BO.Infrastructure.Persistence;
using ASMILE.BO.Infrastructure.Services;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace ASMILE.BO.Tests.Unit;

public sealed class RuntimeTelemetryServiceTests
{
    [Fact]
    public async Task TrackMetricAndFlush_PersistsMetricAndEvent()
    {
        await using var fixture = await SqliteFixture.CreateAsync();
        var service = fixture.CreateTelemetryService();

        await service.TrackMetricAsync("operations-center", "dashboard", "active-users", 3, "count");
        await service.FlushAsync();

        (await fixture.Db.TelemetryEvents.CountAsync()).Should().Be(1);
        (await fixture.Db.RuntimeMetricSnapshots.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task DuplicateExceptionSuppression_PersistsSingleRow()
    {
        await using var fixture = await SqliteFixture.CreateAsync();
        var service = fixture.CreateTelemetryService();

        var exception = new InvalidOperationException("duplicate");
        await service.TrackExceptionAsync(exception, "scheduler", "job");
        await service.TrackExceptionAsync(exception, "scheduler", "job");
        await service.FlushAsync();

        (await fixture.Db.RuntimeExceptionLogs.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task SlowOperationTracking_PersistsSlowOperation()
    {
        await using var fixture = await SqliteFixture.CreateAsync();
        var service = fixture.CreateTelemetryService();

        await service.TrackSlowOperationAsync("report", "execute", 1450, "Warning");
        await service.FlushAsync();

        var log = await fixture.Db.SlowOperationLogs.SingleAsync();
        log.DurationMs.Should().Be(1450);
        log.Module.Should().Be("report");
    }

    [Fact]
    public async Task RuntimeSummary_ReturnsAggregates()
    {
        await using var fixture = await SqliteFixture.CreateAsync();
        var service = fixture.CreateTelemetryService();

        await service.TrackMetricAsync("ops", "summary", "requests", 2, "count");
        await service.TrackExceptionAsync(new InvalidOperationException("boom"), "ops", "summary");
        await service.TrackUiLatencyAsync("ops", "open", 1250, isFrozen: true);
        await service.TrackResourceUsageAsync("ops", cpuUsagePercent: 12.5, memoryUsageBytes: 2048, workingSetBytes: 4096, threadCount: 8);
        await service.FlushAsync();

        var summary = await service.GetRuntimeSummaryAsync();
        summary.TotalEvents.Should().BeGreaterThan(0);
        summary.ExceptionCount.Should().Be(1);
        summary.UiLatencyCount.Should().Be(1);
        summary.ResourceSnapshotCount.Should().Be(1);
    }

    [Fact]
    public async Task CleanupTelemetry_ArchivesExpiredRows()
    {
        await using var fixture = await SqliteFixture.CreateAsync();
        fixture.Db.TelemetryEvents.Add(new TelemetryEvent
        {
            Module = "ops",
            Category = "cleanup",
            Severity = "Information",
            EventType = "Metric",
            EventName = "legacy",
            Message = "old",
            SearchText = "OLD",
            RecordedAtUtc = DateTime.UtcNow.AddDays(-120)
        });
        await fixture.Db.SaveChangesAsync();

        var service = fixture.CreateTelemetryService();
        (await service.CleanupTelemetryAsync(DateTime.UtcNow, TimeSpan.FromDays(30))).Should().Be(1);
        (await fixture.Db.TelemetryEvents.AsNoTracking().SingleAsync()).IsArchived.Should().BeTrue();
    }

    [Fact]
    public async Task CompanyIsolation_HidesOtherCompanyTelemetry()
    {
        await using var fixture = await SqliteFixture.CreateAsync();
        var companyA = new TestCompanyContext(Guid.NewGuid(), Guid.NewGuid());
        var companyB = new TestCompanyContext(Guid.NewGuid(), Guid.NewGuid());
        var user = new TestCurrentUserService();

        var serviceA = fixture.CreateTelemetryService(companyA, user);
        await serviceA.TrackMetricAsync("ops", "company", "value", 1);
        await serviceA.FlushAsync();

        var serviceB = fixture.CreateTelemetryService(companyB, user);
        (await serviceB.GetTelemetryTimelineAsync()).Should().BeEmpty();
    }

    [Fact]
    public async Task RecoverySuggestions_IncludeUiFreezeAction()
    {
        await using var fixture = await SqliteFixture.CreateAsync();
        fixture.Db.UiResponsivenessSnapshots.Add(new UiResponsivenessSnapshot
        {
            Module = "ui",
            Category = "freeze",
            Severity = "Error",
            OperationName = "shell",
            DurationMs = 2500,
            IsFrozen = true,
            Message = "frozen",
            RecordedAtUtc = DateTime.UtcNow
        });
        await fixture.Db.SaveChangesAsync();

        var recovery = fixture.CreateRecoveryService();
        var suggestions = await recovery.DetectUiFreezePatternsAsync();

        suggestions.Should().ContainSingle(x => x.RecommendationCode == "CLEAR_UI_CACHE");
    }

    private sealed class SqliteFixture : IAsyncDisposable
    {
        private readonly string _databasePath;
        private readonly SqliteConnection _rootConnection;
        private readonly TestCompanyContext _defaultCompanyContext;
        public AppDbContext Db { get; }

        private SqliteFixture(string databasePath, SqliteConnection rootConnection, TestCompanyContext defaultCompanyContext, AppDbContext db)
        {
            _databasePath = databasePath;
            _rootConnection = rootConnection;
            _defaultCompanyContext = defaultCompanyContext;
            Db = db;
        }

        public static async Task<SqliteFixture> CreateAsync()
        {
            var databasePath = Path.Combine(Path.GetTempPath(), $"telemetry-tests-{Guid.NewGuid():N}.db");
            var rootConnection = new SqliteConnection($"Data Source={databasePath}");
            await rootConnection.OpenAsync();
            var defaultCompanyContext = new TestCompanyContext();
            var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(rootConnection).Options;
            var db = new AppDbContext(options, defaultCompanyContext);
            await db.Database.EnsureCreatedAsync();
            return new SqliteFixture(databasePath, rootConnection, defaultCompanyContext, db);
        }

        public RuntimeTelemetryService CreateTelemetryService(TestCompanyContext? companyContext = null, TestCurrentUserService? currentUserService = null)
        {
            var context = companyContext ?? _defaultCompanyContext;
            return new RuntimeTelemetryService(new SqliteScopeFactory(_databasePath, context), context, NullLogger<RuntimeTelemetryService>.Instance, null, currentUserService ?? new TestCurrentUserService());
        }

        public FailureRecoveryService CreateRecoveryService(TestCompanyContext? companyContext = null, TestCurrentUserService? currentUserService = null)
        {
            var context = companyContext ?? _defaultCompanyContext;
            var telemetry = CreateTelemetryService(context, currentUserService);
            return new FailureRecoveryService(Db, telemetry, null!, null!, (IAuditService?)null, (IAuditCorrelationService?)null, currentUserService ?? new TestCurrentUserService());
        }

        public async ValueTask DisposeAsync()
        {
            await Db.DisposeAsync();
            await _rootConnection.DisposeAsync();
            try
            {
                if (File.Exists(_databasePath))
                {
                    File.Delete(_databasePath);
                }
            }
            catch (IOException)
            {
            }
        }
    }

    private sealed class SqliteScopeFactory : IServiceScopeFactory
    {
        private readonly string _databasePath;
        private readonly TestCompanyContext _companyContext;

        public SqliteScopeFactory(string databasePath, TestCompanyContext companyContext)
        {
            _databasePath = databasePath;
            _companyContext = companyContext;
        }

        public IServiceScope CreateScope()
            => new SqliteScope(_databasePath, _companyContext);
    }

    private sealed class SqliteScope : IServiceScope
    {
        private readonly SqliteConnection _connection;
        private readonly AppDbContext _dbContext;

        public SqliteScope(string databasePath, TestCompanyContext companyContext)
        {
            _connection = new SqliteConnection($"Data Source={databasePath}");
            _connection.Open();
            var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_connection).Options;
            _dbContext = new AppDbContext(options, companyContext);
            ServiceProvider = new SqliteServiceProvider(_dbContext);
        }

        public IServiceProvider ServiceProvider { get; }

        public void Dispose()
        {
            _dbContext.Dispose();
            _connection.Dispose();
        }
    }

    private sealed class SqliteServiceProvider : IServiceProvider
    {
        private readonly AppDbContext _dbContext;

        public SqliteServiceProvider(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public object? GetService(Type serviceType)
            => serviceType == typeof(AppDbContext) ? _dbContext : null;
    }

    private sealed class TestCompanyContext : ICurrentCompanyContext
    {
        public TestCompanyContext()
        {
            CompanyId = Guid.NewGuid();
            BranchId = Guid.NewGuid();
            CompanyName = "Test Company";
            BranchName = "Main";
        }

        public TestCompanyContext(Guid companyId, Guid branchId)
        {
            CompanyId = companyId;
            BranchId = branchId;
            CompanyName = "Test Company";
            BranchName = "Main";
        }

        public Guid? CompanyId { get; set; }
        public Guid? BranchId { get; set; }
        public string CompanyName { get; set; }
        public string? BranchName { get; set; }

        public Task EnsureInitializedAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task SetCompanyAsync(Guid companyId, CancellationToken cancellationToken = default) { CompanyId = companyId; return Task.CompletedTask; }
        public Task SetBranchAsync(Guid? branchId, CancellationToken cancellationToken = default) { BranchId = branchId; return Task.CompletedTask; }
        public Task<IReadOnlyList<CompanyContextOptionDto>> GetCompaniesAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<CompanyContextOptionDto>>([]);
        public Task<IReadOnlyList<BranchContextOptionDto>> GetBranchesAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<BranchContextOptionDto>>([]);
    }

    private sealed class TestCurrentUserService : ICurrentUserService
    {
        public UserAccountDto? CurrentUser { get; private set; } = new()
        {
            Id = Guid.NewGuid(),
            UserName = "telemetry-test",
            DisplayName = "Telemetry Test"
        };

        public Guid? CurrentSessionId => null;
        public bool IsAuthenticated => CurrentUser is not null;
        public bool HasPermission(string permissionCode) => true;
        public void SetCurrentUser(LoginResultDto login) { }
        public void Clear() => CurrentUser = null;
    }
}
