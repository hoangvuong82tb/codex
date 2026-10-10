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

public sealed class RetentionArchiveTests
{
    [Fact]
    public async Task PreviewRetention_SeedsEnterprisePolicies()
    {
        await using var fixture = await SqliteFixture.CreateAsync();
        var service = fixture.CreateArchiveService();

        var preview = await service.PreviewRetentionAsync();

        preview.Select(x => x.Module).Should().Contain(["audit", "report", "import", "notification", "scheduler", "workspace", "search", "session"]);
        fixture.Db.DataRetentionPolicies.Should().HaveCountGreaterThanOrEqualTo(8);
    }

    [Fact]
    public async Task SaveRetentionPolicy_UpdatesPersistedPolicyAndReloadsIt()
    {
        await using var fixture = await SqliteFixture.CreateAsync();
        var service = fixture.CreateArchiveService();
        var policy = (await service.GetRetentionPoliciesAsync()).First(x => x.Module == "search");

        var saved = await service.SaveRetentionPolicyAsync(new DataRetentionPolicyDto
        {
            Id = policy.Id,
            RetentionDays = 120,
            ArchiveAfterDays = 90,
            DeleteAfterDays = 365,
            CompressArchivedPayload = false,
            IsEnabled = false
        });

        saved.RetentionDays.Should().Be(120);
        saved.IsEnabled.Should().BeFalse();
        var persisted = await fixture.Db.DataRetentionPolicies.AsNoTracking().SingleAsync(x => x.Id == policy.Id);
        persisted.RetentionDays.Should().Be(120);
        persisted.ArchiveAfterDays.Should().Be(90);
        persisted.DeleteAfterDays.Should().Be(365);
        persisted.CompressArchivedPayload.Should().BeFalse();
        persisted.IsEnabled.Should().BeFalse();
    }

    [Fact]
    public async Task SaveRetentionPolicy_RejectsDeleteBeforeArchive()
    {
        await using var fixture = await SqliteFixture.CreateAsync();
        var service = fixture.CreateArchiveService();
        var policy = (await service.GetRetentionPoliciesAsync()).First();

        var act = () => service.SaveRetentionPolicyAsync(new DataRetentionPolicyDto
        {
            Id = policy.Id,
            RetentionDays = 365,
            ArchiveAfterDays = 180,
            DeleteAfterDays = 179,
            CompressArchivedPayload = true,
            IsEnabled = true
        });

        await act.Should().ThrowAsync<ArgumentOutOfRangeException>();
    }

    [Fact]
    public async Task RunArchive_SoftArchivesSearchEntriesAndPersistsBatch()
    {
        await using var fixture = await SqliteFixture.CreateAsync();
        fixture.Db.SearchIndexEntries.Add(new SearchIndexEntry
        {
            EntityType = "Voucher",
            EntityId = "V-100",
            Title = "Voucher archive",
            Category = "Voucher",
            SearchText = "VOUCHER ARCHIVE",
            IndexedAt = DateTime.UtcNow.AddDays(-220)
        });
        await fixture.Db.SaveChangesAsync();

        var batch = await fixture.CreateArchiveService().RunArchiveAsync("search");

        batch.ItemCount.Should().Be(1);
        batch.Status.Should().Be("Completed");
        (await fixture.Db.SearchIndexEntries.SingleAsync()).IsArchived.Should().BeTrue();
        fixture.Db.ArchiveBatchItems.Should().ContainSingle(x => x.EntityId == "V-100");
    }

    [Fact]
    public async Task RestoreArchive_UnarchivesSearchEntry()
    {
        await using var fixture = await SqliteFixture.CreateAsync();
        fixture.Db.SearchIndexEntries.Add(new SearchIndexEntry
        {
            EntityType = "Voucher",
            EntityId = "V-RESTORE",
            Title = "Restore archive",
            Category = "Voucher",
            SearchText = "RESTORE ARCHIVE",
            IndexedAt = DateTime.UtcNow.AddDays(-220)
        });
        await fixture.Db.SaveChangesAsync();
        var service = fixture.CreateArchiveService();
        var batch = await service.RunArchiveAsync("search");

        await service.RestoreArchiveAsync(batch.Id);

        (await fixture.Db.SearchIndexEntries.SingleAsync()).IsArchived.Should().BeFalse();
        (await fixture.Db.ArchiveBatches.SingleAsync()).Status.Should().Be("Restored");
    }

    [Fact]
    public async Task ValidateArchiveIntegrity_DetectsValidAndCorruptPayload()
    {
        await using var fixture = await SqliteFixture.CreateAsync();
        fixture.Db.SearchIndexEntries.Add(new SearchIndexEntry
        {
            EntityType = "Voucher",
            EntityId = "V-HASH",
            Title = "Hash archive",
            Category = "Voucher",
            SearchText = "HASH ARCHIVE",
            IndexedAt = DateTime.UtcNow.AddDays(-220)
        });
        await fixture.Db.SaveChangesAsync();
        var service = fixture.CreateArchiveService();
        var batch = await service.RunArchiveAsync("search");

        (await service.ValidateArchiveIntegrityAsync(batch.Id)).Single().IsValid.Should().BeTrue();
        var item = await fixture.Db.ArchiveBatchItems.SingleAsync();
        item.PayloadJson = "{}";
        await fixture.Db.SaveChangesAsync();

        (await service.ValidateArchiveIntegrityAsync(batch.Id)).Single().IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task RunArchive_CompressesLargePayload()
    {
        await using var fixture = await SqliteFixture.CreateAsync();
        fixture.Db.SearchIndexEntries.Add(new SearchIndexEntry
        {
            EntityType = "Report",
            EntityId = "R-LARGE",
            Title = "Large archive",
            Subtitle = new string('A', 20_000),
            Category = "Report",
            SearchText = new string('B', 20_000),
            IndexedAt = DateTime.UtcNow.AddDays(-220)
        });
        await fixture.Db.SaveChangesAsync();

        await fixture.CreateArchiveService().RunArchiveAsync("search", compressArchivedPayload: true);

        var item = await fixture.Db.ArchiveBatchItems.SingleAsync();
        item.CompressionType.Should().Be("GZip");
        item.CompressedPayload.Should().NotBeNull();
        item.PayloadJson.Should().BeNull();
    }

    [Fact]
    public async Task RunArchive_HonorsCancellation()
    {
        await using var fixture = await SqliteFixture.CreateAsync();
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        var act = () => fixture.CreateArchiveService().RunArchiveAsync("search", cancellationToken: cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task RecoveryService_CreateAndRestoreCheckpoint_PersistsTimeline()
    {
        await using var fixture = await SqliteFixture.CreateAsync();
        var service = fixture.CreateRecoveryService();

        var checkpoint = await service.CreateCheckpointAsync("operations");
        await service.RestoreCheckpointAsync(checkpoint.Id);

        checkpoint.MigrationVersion.Should().NotBeNullOrWhiteSpace();
        var timeline = await service.GetRecoveryTimelineAsync();
        timeline.Should().ContainSingle(x => x.OperationType == "Restore" && x.Status == "Completed");
        (await service.ValidateRecoveryAsync(checkpoint.Id)).Status.Should().Be("Restored");
    }

    [Fact]
    public async Task SchedulerRetentionJob_ExecutesAndCreatesArchiveBatch()
    {
        await using var fixture = await SqliteFixture.CreateAsync();
        fixture.Db.SearchIndexEntries.Add(new SearchIndexEntry
        {
            EntityType = "Voucher",
            EntityId = "V-SCHED",
            Title = "Scheduler archive",
            Category = "Voucher",
            SearchText = "SCHEDULER ARCHIVE",
            IndexedAt = DateTime.UtcNow.AddDays(-220)
        });
        await fixture.Db.SaveChangesAsync();
        var scheduler = fixture.CreateSchedulerService();

        var executionId = await scheduler.RunNowAsync("RETENTION_ENFORCEMENT", "test");

        var execution = await fixture.Db.BackgroundJobExecutions.AsNoTracking().SingleAsync(x => x.Id == executionId);
        execution.Status.Should().Be("Completed", execution.ErrorMessage);
        fixture.Db.ArchiveBatches.Should().Contain(x => x.Status == "Completed");
    }

    [Fact]
    public async Task EnterpriseSearch_ExcludesArchivedByDefaultAndCanIncludeArchived()
    {
        await using var fixture = await SqliteFixture.CreateAsync();
        var search = fixture.CreateSearchService();
        await search.UpsertAsync(new SearchIndexUpsertRequest { EntityType = "Voucher", EntityId = "V-ACTIVE", Title = "Archived Toggle", Category = "Voucher", SearchText = "Archived Toggle" });
        var row = await fixture.Db.SearchIndexEntries.SingleAsync();
        row.IsArchived = true;
        row.ArchivedAtUtc = DateTime.UtcNow;
        await fixture.Db.SaveChangesAsync();

        (await search.SearchAsync("Archived")).Should().BeEmpty();
        (await search.SearchAsync("Archived", includeArchived: true)).Should().ContainSingle(x => x.EntityId == "V-ACTIVE");
    }

    [Fact]
    public async Task ArchiveHistory_IsCompanyIsolated()
    {
        var databasePath = Path.Combine(Path.GetTempPath(), $"retention-company-{Guid.NewGuid():N}.db");
        var companyA = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var companyB = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        await using (var fixture = await SqliteFixture.CreateAsync(new TestCompanyContext(companyA), databasePath, deleteOnDispose: false))
        {
            fixture.Db.ArchiveBatches.Add(new ArchiveBatch { Module = "search", EntityName = "SearchIndexEntry", Status = "Completed", ItemCount = 1 });
            await fixture.Db.SaveChangesAsync();
        }
        await using (var fixture = await SqliteFixture.CreateAsync(new TestCompanyContext(companyB), databasePath, ensureCreated: false, deleteOnDispose: false))
        {
            fixture.Db.ArchiveBatches.Add(new ArchiveBatch { Module = "search", EntityName = "SearchIndexEntry", Status = "Completed", ItemCount = 1 });
            await fixture.Db.SaveChangesAsync();
        }
        await using var companyAFixture = await SqliteFixture.CreateAsync(new TestCompanyContext(companyA), databasePath, ensureCreated: false);

        (await companyAFixture.CreateArchiveService().GetArchiveHistoryAsync()).Should().ContainSingle(x => x.CompanyId == companyA);
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
            var path = databasePath ?? Path.Combine(Path.GetTempPath(), $"retention-tests-{Guid.NewGuid():N}.db");
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

        public ArchiveService CreateArchiveService()
            => new(Db, null, new AuditCorrelationService(), new TestCurrentUserService());

        public RecoveryService CreateRecoveryService()
            => new(Db, null, new AuditCorrelationService(), new TestCurrentUserService());

        public EnterpriseSearchService CreateSearchService()
            => new(Db, new TestCurrentUserService());

        public BackgroundSchedulerService CreateSchedulerService()
            => new(new SqliteScopeFactory(_databasePath), Db, NullLogger<BackgroundSchedulerService>.Instance);

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

    private sealed class SqliteScopeFactory : IServiceScopeFactory
    {
        private readonly string _databasePath;

        public SqliteScopeFactory(string databasePath)
        {
            _databasePath = databasePath;
        }

        public IServiceScope CreateScope() => new SqliteScope(_databasePath);
    }

    private sealed class SqliteScope : IServiceScope
    {
        private readonly SqliteConnection _connection;
        private readonly AppDbContext _dbContext;

        public SqliteScope(string databasePath)
        {
            _connection = new SqliteConnection($"Data Source={databasePath}");
            _connection.Open();
            var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_connection).Options;
            _dbContext = new AppDbContext(options);
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
        {
            if (serviceType == typeof(AppDbContext))
            {
                return _dbContext;
            }

            if (serviceType == typeof(IArchiveService))
            {
                return new ArchiveService(_dbContext, null, new AuditCorrelationService(), new TestCurrentUserService());
            }

            return null;
        }
    }

    private sealed class TestCurrentUserService : ICurrentUserService
    {
        public UserAccountDto? CurrentUser { get; private set; } = new()
        {
            Id = Guid.NewGuid(),
            UserName = "retention.user",
            DisplayName = "Retention User",
            Roles = [],
            IsActive = true
        };

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
