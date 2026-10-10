using ASMILE.BO.Application.DTOs;
using ASMILE.BO.Application.DTOs.Settings;
using ASMILE.BO.Application.Interfaces;
using ASMILE.BO.Application.Interfaces.Settings;
using ASMILE.BO.Core.Entities;
using ASMILE.BO.Core.Entities.Settings;
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
    public void ComputeNextRun_FollowsStandardCronDayOfMonthAndDayOfWeekRules()
    {
        var saturday = new DateTime(2026, 5, 23, 0, 0, 0, DateTimeKind.Utc);

        // With both day fields restricted, cron runs on either a matching day
        // of month or weekday. May 25 is Monday even though it is not the 1st.
        BackgroundSchedulerService.ComputeNextRun(saturday, null, "0 0 1 * 1")
            .Should().Be(new DateTime(2026, 5, 25, 0, 0, 0, DateTimeKind.Utc));

        // Standard cron accepts both 0 and 7 for Sunday.
        BackgroundSchedulerService.ComputeNextRun(saturday, null, "0 0 * * 0")
            .Should().Be(new DateTime(2026, 5, 24, 0, 0, 0, DateTimeKind.Utc));
        BackgroundSchedulerService.ComputeNextRun(saturday, null, "0 0 * * 7")
            .Should().Be(new DateTime(2026, 5, 24, 0, 0, 0, DateTimeKind.Utc));
        BackgroundSchedulerService.ComputeNextRun(saturday, null, "0 0 */2 * 1")
            .Should().Be(new DateTime(2026, 5, 25, 0, 0, 0, DateTimeKind.Utc));

        var afterFebruary = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc);
        BackgroundSchedulerService.ComputeNextRun(afterFebruary, null, "0 0 29 2 *")
            .Should().Be(new DateTime(2028, 2, 29, 0, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public void ComputeNextRun_SupportsStandardCronStepFormsAndRejectsOutOfRangeValues()
    {
        var now = new DateTime(2026, 5, 23, 10, 2, 0, DateTimeKind.Utc);

        BackgroundSchedulerService.ComputeNextRun(now, null, "1-10/3 * * * *")
            .Should().Be(new DateTime(2026, 5, 23, 10, 4, 0, DateTimeKind.Utc));
        BackgroundSchedulerService.ComputeNextRun(now, null, "1/5 * * * *")
            .Should().Be(new DateTime(2026, 5, 23, 10, 6, 0, DateTimeKind.Utc));
        BackgroundSchedulerService.ComputeNextRun(now, null, "0,10-20/5 * * * *")
            .Should().Be(new DateTime(2026, 5, 23, 10, 10, 0, DateTimeKind.Utc));
        BackgroundSchedulerService.ComputeNextRun(now, null, "*/15 * * * *")
            .Should().Be(new DateTime(2026, 5, 23, 10, 15, 0, DateTimeKind.Utc));

        BackgroundSchedulerService.ComputeNextRun(now, null, "0 0 1-40 * *").Should().BeNull();
        BackgroundSchedulerService.ComputeNextRun(now, null, "0 0 * * 8").Should().BeNull();
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
        // Keep the test job running long enough for cancellation to be observed
        // after the execution row reaches Running. A short timeout races the
        // scheduler's startup and can turn the execution into TimedOut first.
        var definition = await fixture.AddJobAsync("CANCEL_TEST", timeoutSeconds: 10, allowConcurrentExecution: false, intervalMinutes: 5);
        var executionId = await fixture.AddQueuedExecutionAsync(definition.Id, definition.Code, retryAttempt: 0);

        var runningTask = scheduler.ExecuteNextQueuedAsync();
        var reachedRunning = await fixture.WaitForExecutionStatusAsync(executionId, "Running", TimeSpan.FromSeconds(10));
        reachedRunning.Should().BeTrue("the cancellation assertion must exercise a running execution");
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
    public async Task Recovery_does_not_fail_a_running_execution_before_its_configured_timeout()
    {
        await using var fixture = await SqliteFixture.CreateAsync();
        var scheduler = fixture.CreateService();
        const int timeoutSeconds = 3600;
        var definition = await fixture.AddJobAsync("LONG_RUNNING_RECOVERY_TEST", timeoutSeconds: timeoutSeconds);
        var runningExecutionId = await fixture.AddRunningExecutionAsync(
            definition.Id,
            definition.Code,
            startedAtUtc: DateTime.UtcNow.AddMinutes(-20));

        (await scheduler.RecoverStaleExecutionsAsync(DateTime.UtcNow, TimeSpan.FromMinutes(15))).Should().Be(0);
        (await fixture.Db.BackgroundJobExecutions.AsNoTracking().SingleAsync(x => x.Id == runningExecutionId))
            .Status.Should().Be("Running");
    }

    [Fact]
    public async Task StartupValidation_SeedsDefaultJobs()
    {
        await using var fixture = await SqliteFixture.CreateAsync();
        var scheduler = fixture.CreateService();

        await scheduler.ValidateStartupAsync();

        var codes = (await scheduler.GetDefinitionsAsync()).Select(x => x.Code).ToArray();
        codes.Should().Contain(["DEPRECIATION", "TOOL_ALLOCATION", "VAT_SYNC", "EINVOICE_ISSUE_QUEUE", "REPORT_SNAPSHOT_CLEANUP", "AUDIT_CLEANUP", "TEMP_CLEANUP", "SEARCH_INDEX_REFRESH", "WORKSPACE_CLEANUP", "TELEMETRY_CLEANUP", "DATABASE_BACKUP"]);
    }

    [Fact]
    public async Task StartupValidation_applies_persisted_database_backup_policy()
    {
        await using var fixture = await SqliteFixture.CreateAsync();
        var scheduler = fixture.CreateService(new StubSettingsService(new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Backup.EnableAutoBackup"] = "true",
            ["Backup.Schedule"] = "*/15 * * * *"
        }));

        await scheduler.ValidateStartupAsync();

        var definition = (await scheduler.GetDefinitionsAsync()).Single(x => x.Code == "DATABASE_BACKUP");
        definition.IsEnabled.Should().BeTrue();
        definition.CronExpression.Should().Be("*/15 * * * *");
        definition.IntervalMinutes.Should().BeNull();
        definition.NextRunAtUtc.Should().NotBeNull();
    }

    [Fact]
    public async Task StartupValidation_keeps_automatic_backup_disabled_until_explicitly_enabled()
    {
        await using var fixture = await SqliteFixture.CreateAsync();
        var scheduler = fixture.CreateService(new StubSettingsService(new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Backup.EnableAutoBackup"] = "false",
            ["Backup.Schedule"] = "0 0 * * *"
        }));

        await scheduler.ValidateStartupAsync();

        var definition = (await scheduler.GetDefinitionsAsync()).Single(x => x.Code == "DATABASE_BACKUP");
        definition.IsEnabled.Should().BeFalse();
        definition.NextRunAtUtc.Should().BeNull();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("not-a-boolean")]
    public async Task Missing_or_invalid_auto_backup_setting_fails_closed(string? enabledValue)
    {
        await using var fixture = await SqliteFixture.CreateAsync();
        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Backup.Schedule"] = "0 0 * * *"
        };
        if (enabledValue is not null)
        {
            values["Backup.EnableAutoBackup"] = enabledValue;
        }

        var scheduler = fixture.CreateService(new StubSettingsService(values));

        await scheduler.ValidateStartupAsync();

        var definition = (await scheduler.GetDefinitionsAsync()).Single(x => x.Code == "DATABASE_BACKUP");
        definition.IsEnabled.Should().BeFalse();
        definition.NextRunAtUtc.Should().BeNull();
    }

    [Fact]
    public async Task Invalid_or_disabled_database_backup_policy_fails_closed()
    {
        await using var fixture = await SqliteFixture.CreateAsync();
        var scheduler = fixture.CreateService(new StubSettingsService(new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Backup.EnableAutoBackup"] = "false",
            ["Backup.Schedule"] = "not-a-cron"
        }));

        await scheduler.ValidateStartupAsync();

        var definition = (await scheduler.GetDefinitionsAsync()).Single(x => x.Code == "DATABASE_BACKUP");
        definition.IsEnabled.Should().BeFalse();
        definition.NextRunAtUtc.Should().BeNull();
    }

    [Fact]
    public async Task Database_backup_job_calls_backup_provider_and_logs_result()
    {
        await using var fixture = await SqliteFixture.CreateAsync();
        var backupService = new RecordingBackupService();
        var scheduler = fixture.CreateService(backupService: backupService);
        var definition = await fixture.AddJobAsync("DATABASE_BACKUP", timeoutSeconds: 10);
        var executionId = await fixture.AddQueuedExecutionAsync(definition.Id, definition.Code, retryAttempt: 0);

        await scheduler.ExecuteNextQueuedAsync();

        backupService.CallCount.Should().Be(1);
        (await scheduler.GetExecutionsAsync()).Should().ContainSingle(x => x.Id == executionId && x.Status == "Completed");
        (await scheduler.GetLogsAsync(executionId)).Should().Contain(x => x.Message.Contains("Database backup created and verified", StringComparison.Ordinal));
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

        public BackgroundSchedulerService CreateService(ISettingsService? settingsService = null, IBackupService? backupService = null)
            => new(new SqliteScopeFactory(_databasePath, backupService), Db, NullLogger<BackgroundSchedulerService>.Instance, settingsService: settingsService);

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

        public async Task<bool> WaitForExecutionStatusAsync(Guid executionId, string status, TimeSpan timeout)
        {
            var deadline = DateTime.UtcNow.Add(timeout);
            while (DateTime.UtcNow < deadline)
            {
                Db.ChangeTracker.Clear();
                var current = await Db.BackgroundJobExecutions.AsNoTracking().FirstOrDefaultAsync(x => x.Id == executionId);
                if (current?.Status == status)
                {
                    return true;
                }

                await Task.Delay(25);
            }

            return false;
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
        private readonly IBackupService? _backupService;

        public SqliteScopeFactory(string databasePath, IBackupService? backupService)
        {
            _databasePath = databasePath;
            _backupService = backupService;
        }

        public IServiceScope CreateScope()
            => new SqliteScope(_databasePath, _backupService);
    }

    private sealed class SqliteScope : IServiceScope
    {
        private readonly SqliteConnection _connection;
        private readonly AppDbContext _dbContext;

        public SqliteScope(string databasePath, IBackupService? backupService)
        {
            _connection = new SqliteConnection($"Data Source={databasePath}");
            _connection.Open();
            var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_connection).Options;
            _dbContext = new AppDbContext(options);
            ServiceProvider = new SqliteServiceProvider(_dbContext, backupService);
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
        private readonly IBackupService? _backupService;

        public SqliteServiceProvider(AppDbContext dbContext, IBackupService? backupService)
        {
            _dbContext = dbContext;
            _backupService = backupService;
        }

        public object? GetService(Type serviceType)
        {
            if (serviceType == typeof(AppDbContext)) return _dbContext;
            if (serviceType == typeof(IBackupService)) return _backupService;
            return null;
        }
    }

    private sealed class RecordingBackupService : IBackupService
    {
        public int CallCount { get; private set; }

        public Task<IReadOnlyList<BackupRecordDto>> GetRecentAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<BackupRecordDto>>([]);

        public Task<BackupRecordDto> BackupNowAsync(CancellationToken cancellationToken = default)
        {
            CallCount++;
            return Task.FromResult(new BackupRecordDto { FileName = "scheduler-test.bak", Status = "Completed" });
        }

        public Task<bool> ValidateBackupAsync(string backupFilePath, CancellationToken cancellationToken = default)
            => Task.FromResult(true);

        public Task RestoreAsync(string backupFilePath, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }

    private sealed class StubSettingsService(IReadOnlyDictionary<string, string> values) : ISettingsService
    {
        public Task<SettingValueDto?> GetValueAsync(string key, SettingScope scope, string? scopeTarget)
            => Task.FromResult(values.TryGetValue(key, out var value)
                ? new SettingValueDto { Key = key, Scope = scope, Value = value }
                : null);

        public Task<IReadOnlyList<SettingDefinitionDto>> GetDefinitionsAsync(SettingScope scope, string? scopeTarget)
            => throw new NotSupportedException();

        public Task<IReadOnlyList<SettingValueDto>> GetValuesAsync(SettingScope scope, string? scopeTarget)
            => throw new NotSupportedException();

        public Task SaveValueAsync(SaveSettingValueRequest request) => throw new NotSupportedException();
        public Task SaveValuesAsync(IReadOnlyList<SaveSettingValueRequest> requests) => throw new NotSupportedException();
        public Task ResetToDefaultAsync(string key, SettingScope scope, string? scopeTarget) => throw new NotSupportedException();
    }
}
