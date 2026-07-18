using ASMILE.BO.Application.DTOs;
using ASMILE.BO.Core.Entities;
using ASMILE.BO.Infrastructure.Persistence;
using ASMILE.BO.Infrastructure.Services;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace ASMILE.BO.Tests.Unit;

public sealed class BackgroundSchedulerServiceTests
{
    [Fact]
    public void ComputeNextRun_UsesIntervalAndCronMetadata()
    {
        var now = new DateTime(2026, 5, 23, 10, 15, 0, DateTimeKind.Utc);

        BackgroundSchedulerService.ComputeNextRun(now, 30, null).Should().Be(now.AddMinutes(30));
        BackgroundSchedulerService.ComputeNextRun(now, null, "*/5 * * * *").Should().Be(new DateTime(2026, 5, 23, 10, 20, 0, DateTimeKind.Utc));
    }

    [Fact]
    public async Task TimeoutLogic_CreatesTimedOutExecutionAndRetryRow()
    {
        await using var fixture = await SqliteFixture.CreateAsync();
        var scheduler = fixture.CreateService();
        var definition = await fixture.AddJobAsync("SLOW_TIMEOUT", timeoutSeconds: 1, retryCount: 1);

        var executionId = await scheduler.RunNowAsync(definition.Code, "unit-test");

        var executions = await scheduler.GetExecutionsAsync();
        executions.Should().ContainSingle(x => x.Id == executionId && x.Status == "TimedOut");
        executions.Should().ContainSingle(x => x.JobCode == definition.Code && x.Status == "Queued" && x.RetryAttempt == 1);
        (await scheduler.GetLogsAsync(executionId)).Should().NotBeEmpty();
    }

    [Fact]
    public async Task ConcurrencyGuard_SkipsDuplicateQueuedRun()
    {
        await using var fixture = await SqliteFixture.CreateAsync();
        var scheduler = fixture.CreateService();
        var definition = await fixture.AddJobAsync("CONCURRENCY_TEST");
        await fixture.AddRunningExecutionAsync(definition.Id, definition.Code);

        var queued = await scheduler.QueueDueAsync(DateTime.UtcNow);

        queued.Should().Be(0);
        (await scheduler.GetExecutionsAsync()).Should().ContainSingle(x => x.Status == "Running");
    }

    [Fact]
    public async Task Cancellation_PropagatesToRunningExecution()
    {
        await using var fixture = await SqliteFixture.CreateAsync();
        var scheduler = fixture.CreateService();
        var definition = await fixture.AddJobAsync("CANCEL_TEST", timeoutSeconds: 1, allowConcurrentExecution: false, intervalMinutes: 5);
        var executionId = await fixture.AddQueuedExecutionAsync(definition.Id, definition.Code, retryAttempt: 0);

        var runningTask = scheduler.ExecuteNextQueuedAsync();
        await fixture.WaitForExecutionStatusAsync(executionId, "Running", TimeSpan.FromSeconds(2));
        await scheduler.CancelAsync(executionId);
        await runningTask;

        var executions = await scheduler.GetExecutionsAsync();
        executions.Should().ContainSingle(x => x.Id == executionId && x.Status == "Cancelled");
    }

    [Fact]
    public async Task PauseResumeAndRecovery_FlowWorksWithPersistedRows()
    {
        await using var fixture = await SqliteFixture.CreateAsync();
        var scheduler = fixture.CreateService();
        var definition = await fixture.AddJobAsync("PAUSE_RESUME_TEST");

        await scheduler.PauseScheduleAsync(definition.Id);
        (await scheduler.GetDefinitionsAsync()).Single(x => x.Id == definition.Id).IsPaused.Should().BeTrue();

        await scheduler.ResumeScheduleAsync(definition.Id);
        (await scheduler.GetDefinitionsAsync()).Single(x => x.Id == definition.Id).IsPaused.Should().BeFalse();

        await fixture.SetScheduleDueAsync(definition.Id);
        (await scheduler.QueueDueAsync(DateTime.UtcNow)).Should().Be(1);

        var staleExecutionId = await fixture.AddRunningExecutionAsync(definition.Id, definition.Code, startedAtUtc: DateTime.UtcNow.AddHours(-2));
        (await scheduler.RecoverStaleExecutionsAsync(DateTime.UtcNow, TimeSpan.FromMinutes(15))).Should().Be(1);
        (await scheduler.GetExecutionsAsync()).Single(x => x.Id == staleExecutionId).Status.Should().Be("Failed");
    }

    [Fact]
    public async Task StartupValidation_SeedsDefaultJobs()
    {
        await using var fixture = await SqliteFixture.CreateAsync();
        var scheduler = fixture.CreateService();

        await scheduler.ValidateStartupAsync();

        var codes = (await scheduler.GetDefinitionsAsync()).Select(x => x.Code).ToArray();
        codes.Should().Contain(["DEPRECIATION", "TOOL_ALLOCATION", "VAT_SYNC", "REPORT_SNAPSHOT_CLEANUP", "AUDIT_CLEANUP", "TEMP_CLEANUP", "SEARCH_INDEX_REFRESH", "WORKSPACE_CLEANUP", "TELEMETRY_CLEANUP"]);
    }

    private sealed class SqliteFixture : IAsyncDisposable
    {
        private readonly string _databasePath;
        private readonly SqliteConnection _rootConnection;
        public AppDbContext Db { get; }

        private SqliteFixture(string databasePath, SqliteConnection rootConnection, AppDbContext db)
        {
            _databasePath = databasePath;
            _rootConnection = rootConnection;
            Db = db;
        }

        public static async Task<SqliteFixture> CreateAsync()
        {
            var databasePath = Path.Combine(Path.GetTempPath(), $"scheduler-tests-{Guid.NewGuid():N}.db");
            var rootConnection = new SqliteConnection($"Data Source={databasePath}");
            await rootConnection.OpenAsync();
            var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(rootConnection).Options;
            var db = new AppDbContext(options);
            await db.Database.EnsureCreatedAsync();
            return new SqliteFixture(databasePath, rootConnection, db);
        }

        public BackgroundSchedulerService CreateService()
            => new(new SqliteScopeFactory(_databasePath), Db, NullLogger<BackgroundSchedulerService>.Instance);

        public async Task<BackgroundJobDefinition> AddJobAsync(string code, int timeoutSeconds = 5, bool allowConcurrentExecution = false, int intervalMinutes = 60, int retryCount = 1)
        {
            var definition = new BackgroundJobDefinition
            {
                Code = code,
                Name = code,
                Description = code,
                IsEnabled = true,
                AllowConcurrentExecution = allowConcurrentExecution,
                RetryCount = retryCount,
                TimeoutSeconds = timeoutSeconds,
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            };
            definition.Schedules.Add(new BackgroundJobSchedule
            {
                IntervalMinutes = intervalMinutes,
                NextRunAtUtc = DateTime.UtcNow.AddMinutes(-1),
                IsPaused = false
            });
            Db.BackgroundJobDefinitions.Add(definition);
            await Db.SaveChangesAsync();
            return definition;
        }

        public async Task<Guid> AddQueuedExecutionAsync(Guid definitionId, string code, int retryAttempt, DateTime? queuedAtUtc = null)
        {
            var execution = new BackgroundJobExecution
            {
                JobDefinitionId = definitionId,
                JobName = code,
                JobType = code,
                Status = "Queued",
                QueuedAtUtc = queuedAtUtc ?? DateTime.UtcNow,
                StartedAt = queuedAtUtc ?? DateTime.UtcNow,
                RetryAttempt = retryAttempt,
                TriggerSource = "manual",
                RequestedBy = "unit-test",
                CorrelationId = Guid.NewGuid().ToString("N")
            };
            Db.BackgroundJobExecutions.Add(execution);
            await Db.SaveChangesAsync();
            return execution.Id;
        }

        public async Task<Guid> AddRunningExecutionAsync(Guid definitionId, string code, DateTime? startedAtUtc = null)
        {
            var execution = new BackgroundJobExecution
            {
                JobDefinitionId = definitionId,
                JobName = code,
                JobType = code,
                Status = "Running",
                QueuedAtUtc = startedAtUtc ?? DateTime.UtcNow,
                StartedAtUtc = startedAtUtc ?? DateTime.UtcNow,
                StartedAt = startedAtUtc ?? DateTime.UtcNow,
                RetryAttempt = 0,
                TriggerSource = "scheduler",
                RequestedBy = "unit-test",
                CorrelationId = Guid.NewGuid().ToString("N")
            };
            Db.BackgroundJobExecutions.Add(execution);
            await Db.SaveChangesAsync();
            return execution.Id;
        }

        public async Task SetScheduleDueAsync(Guid definitionId)
        {
            var schedule = await Db.BackgroundJobSchedules.FirstAsync(x => x.JobDefinitionId == definitionId);
            schedule.NextRunAtUtc = DateTime.UtcNow.AddMinutes(-1);
            await Db.SaveChangesAsync();
        }

        public async Task WaitForExecutionStatusAsync(Guid executionId, string status, TimeSpan timeout)
        {
            var deadline = DateTime.UtcNow.Add(timeout);
            while (DateTime.UtcNow < deadline)
            {
                Db.ChangeTracker.Clear();
                var current = await Db.BackgroundJobExecutions.AsNoTracking().FirstOrDefaultAsync(x => x.Id == executionId);
                if (current?.Status == status)
                {
                    return;
                }

                await Task.Delay(25);
            }
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

        public SqliteScopeFactory(string databasePath)
        {
            _databasePath = databasePath;
        }

        public IServiceScope CreateScope()
            => new SqliteScope(_databasePath);
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
            return serviceType == typeof(AppDbContext) ? _dbContext : null;
        }
    }
}
