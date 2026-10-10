using System.Collections.Concurrent;
using System.Data;
using System.Diagnostics;
using ASMILE.BO.Application.DTOs;
using ASMILE.BO.Application.Interfaces;
using ASMILE.BO.Application.Interfaces.Settings;
using ASMILE.BO.Core.Entities;
using ASMILE.BO.Core.Entities.Settings;
using ASMILE.BO.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ASMILE.BO.Infrastructure.Services;

public sealed class BackgroundSchedulerService : IBackgroundSchedulerService, IDisposable
{
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> JobLocks = new(StringComparer.OrdinalIgnoreCase);
    private static readonly ConcurrentDictionary<Guid, CancellationTokenSource> ActiveExecutionTokens = new();
    private static readonly SemaphoreSlim DefaultsGate = new(1, 1);
    private static readonly TimeSpan DefaultStaleWindow = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan ExecutionRecoveryGracePeriod = TimeSpan.FromMinutes(2);
    private static readonly IReadOnlyList<BackgroundJobSeed> DefaultJobs =
    [
        new("DEPRECIATION", "Depreciation run", "Run fixed asset depreciation jobs.", 1440, 2, 300),
        new("TOOL_ALLOCATION", "Tool allocation run", "Run tool asset allocation jobs.", 1440, 2, 300),
        new("VAT_SYNC", "VAT sync", "Synchronize VAT documents.", 240, 2, 240),
        new("EINVOICE_ISSUE_QUEUE", "E-invoice issue queue", "Process queued e-invoice issuance requests through the configured provider.", 1, 2, 300),
        new("REPORT_SNAPSHOT_CLEANUP", "Report snapshot cleanup", "Clean stale report snapshots.", 720, 1, 300),
        new("AUDIT_CLEANUP", "Audit cleanup", "Clean expired audit operational rows.", 1440, 1, 300),
        new("TEMP_CLEANUP", "Temp cleanup", "Clean temporary files.", 720, 1, 180),
        new("SEARCH_INDEX_REFRESH", "Search index refresh", "Refresh enterprise search index.", 120, 1, 180),
        new("WORKSPACE_CLEANUP", "Workspace cleanup", "Clean stale workspace state.", 1440, 1, 180),
        new("ARCHIVE_CLEANUP", "Archive cleanup", "Purge expired archive batches.", 720, 1, 180),
        new("RETENTION_ENFORCEMENT", "Retention enforcement", "Archive expired retained data.", 360, 2, 240),
        new("INTEGRITY_VERIFICATION", "Integrity verification", "Validate archive and recovery integrity.", 720, 1, 180),
        new("TELEMETRY_CLEANUP", "Telemetry cleanup", "Archive stale telemetry records.", 720, 1, 180),
        new("DATABASE_BACKUP", "Database backup", "Create and verify a SQL Server backup from the persisted backup policy.", 1440, 2, 360)
    ];

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly AppDbContext _dbContext;
    private readonly ILogger<BackgroundSchedulerService> _logger;
    private readonly IAuditService? _auditService;
    private readonly IAuditCorrelationService? _auditCorrelationService;
    private readonly IRuntimeTelemetryService? _runtimeTelemetryService;
    private readonly IWorkflowResilienceService? _workflowResilienceService;
    private readonly ISettingsService? _settingsService;
    private bool _disposed;

    public BackgroundSchedulerService(IServiceScopeFactory scopeFactory, AppDbContext dbContext, ILogger<BackgroundSchedulerService> logger, IAuditService? auditService = null, IAuditCorrelationService? auditCorrelationService = null, IRuntimeTelemetryService? runtimeTelemetryService = null, IWorkflowResilienceService? workflowResilienceService = null, ISettingsService? settingsService = null)
    {
        _scopeFactory = scopeFactory;
        _dbContext = dbContext;
        _logger = logger;
        _auditService = auditService;
        _auditCorrelationService = auditCorrelationService;
        _runtimeTelemetryService = runtimeTelemetryService;
        _workflowResilienceService = workflowResilienceService;
        _settingsService = settingsService;
    }

    public async Task EnsureDefaultsAsync(CancellationToken cancellationToken = default)
    {
        await DefaultsGate.WaitAsync(cancellationToken);
        try
        {
            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
            if (string.Equals(_dbContext.Database.ProviderName, "Microsoft.EntityFrameworkCore.SqlServer", StringComparison.Ordinal))
            {
                await AcquireSqlServerSchedulerMutationLockAsync(cancellationToken);
            }

            _dbContext.ChangeTracker.Clear();

            var now = DateTime.UtcNow;
            var defaultCodes = DefaultJobs.Select(x => x.Code).ToArray();
            var existingDefinitions = await _dbContext.BackgroundJobDefinitions
                .AsNoTracking()
                .Where(x => defaultCodes.Contains(x.Code))
                .Select(x => new
                {
                    x.Id,
                    x.Code
                })
                .ToDictionaryAsync(x => x.Code, StringComparer.OrdinalIgnoreCase, cancellationToken);
            var definitionIds = existingDefinitions.Values.Select(x => x.Id).ToArray();
            var scheduledDefinitionIdList = await _dbContext.BackgroundJobSchedules
                .AsNoTracking()
                .Where(x => definitionIds.Contains(x.JobDefinitionId))
                .Select(x => x.JobDefinitionId)
                .ToListAsync(cancellationToken);
            var scheduledDefinitionIds = scheduledDefinitionIdList.ToHashSet();
            var changed = false;

            foreach (var seed in DefaultJobs)
            {
                if (!existingDefinitions.TryGetValue(seed.Code, out var existingDefinition))
                {
                    var definition = new BackgroundJobDefinition
                    {
                        Code = seed.Code,
                        Name = seed.Name,
                        Description = seed.Description,
                        IsEnabled = true,
                        AllowConcurrentExecution = false,
                        RetryCount = seed.RetryCount,
                        TimeoutSeconds = seed.TimeoutSeconds,
                        CreatedAtUtc = now,
                        UpdatedAtUtc = now
                    };
                    definition.Schedules.Add(new BackgroundJobSchedule
                    {
                        CronExpression = null,
                        IntervalMinutes = seed.IntervalMinutes,
                        NextRunAtUtc = now.AddMinutes(seed.IntervalMinutes),
                        LastRunAtUtc = null,
                        IsPaused = false
                    });
                    _dbContext.BackgroundJobDefinitions.Add(definition);
                    changed = true;
                    continue;
                }

                if (scheduledDefinitionIds.Contains(existingDefinition.Id))
                {
                    continue;
                }

                _dbContext.BackgroundJobSchedules.Add(new BackgroundJobSchedule
                {
                    JobDefinitionId = existingDefinition.Id,
                    CronExpression = null,
                    IntervalMinutes = seed.IntervalMinutes,
                    NextRunAtUtc = now.AddMinutes(seed.IntervalMinutes),
                    LastRunAtUtc = null,
                    IsPaused = false
                });
                changed = true;
            }

            if (changed)
            {
                await _dbContext.SaveChangesAsync(cancellationToken);
            }

            await ApplyDatabaseBackupPolicyAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        finally
        {
            DefaultsGate.Release();
        }
    }

    public async Task ValidateStartupAsync(CancellationToken cancellationToken = default)
    {
        await EnsureDefaultsAsync(cancellationToken);

        var invalidDefinitions = await _dbContext.BackgroundJobDefinitions
            .Include(x => x.Schedules)
            .Where(x => x.IsEnabled && !x.Schedules.Any())
            .Select(x => x.Code)
            .ToListAsync(cancellationToken);

        if (invalidDefinitions.Count > 0)
        {
            throw new InvalidOperationException($"Scheduler startup validation failed. Missing schedules: {string.Join(", ", invalidDefinitions)}");
        }

        var now = DateTime.UtcNow;
        var schedules = await _dbContext.BackgroundJobSchedules.Include(x => x.JobDefinition).ToListAsync(cancellationToken);
        var repaired = false;
        foreach (var schedule in schedules)
        {
            if (schedule.JobDefinition is null)
            {
                continue;
            }

            if (!schedule.IntervalMinutes.HasValue && string.IsNullOrWhiteSpace(schedule.CronExpression))
            {
                throw new InvalidOperationException($"Scheduler startup validation failed. Job {schedule.JobDefinition.Code} does not have a valid schedule.");
            }

            if (schedule.IsPaused || schedule.NextRunAtUtc.HasValue)
            {
                continue;
            }

            schedule.NextRunAtUtc = ComputeNextRun(now, schedule.IntervalMinutes, schedule.CronExpression) ?? now.AddMinutes(schedule.IntervalMinutes.GetValueOrDefault(60));
            repaired = true;
        }

        if (repaired)
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<SchedulerDashboardDto> GetDashboardAsync(CancellationToken cancellationToken = default)
    {
        await EnsureDefaultsAsync(cancellationToken);
        var executions = _dbContext.BackgroundJobExecutions.AsNoTracking();
        var completedDurations = await executions
            .Where(x => x.Status == "Completed" && x.DurationMs > 0)
            .Select(x => x.DurationMs)
            .Take(200)
            .ToListAsync(cancellationToken);

        return new SchedulerDashboardDto
        {
            RunningJobs = await executions.CountAsync(x => x.Status == "Running", cancellationToken),
            FailedJobs = await executions.CountAsync(x => x.Status == "Failed" || x.Status == "TimedOut", cancellationToken),
            QueuedJobs = await executions.CountAsync(x => x.Status == "Queued" || x.Status == "Cancelling", cancellationToken),
            NextScheduledRuns = await _dbContext.BackgroundJobSchedules.CountAsync(x => !x.IsPaused && x.NextRunAtUtc != null, cancellationToken),
            AverageDurationMs = completedDurations.Count == 0 ? 0 : completedDurations.Average()
        };
    }

    public async Task<IReadOnlyList<BackgroundSchedulerDefinitionDto>> GetDefinitionsAsync(CancellationToken cancellationToken = default)
    {
        await EnsureDefaultsAsync(cancellationToken);
        return await _dbContext.BackgroundJobDefinitions.AsNoTracking()
            .OrderBy(x => x.Code)
            .Select(x => new BackgroundSchedulerDefinitionDto
            {
                Id = x.Id,
                Code = x.Code,
                Name = x.Name,
                Description = x.Description,
                IsEnabled = x.IsEnabled,
                AllowConcurrentExecution = x.AllowConcurrentExecution,
                RetryCount = x.RetryCount,
                TimeoutSeconds = x.TimeoutSeconds,
                IntervalMinutes = x.Schedules.OrderBy(s => s.NextRunAtUtc).Select(s => s.IntervalMinutes).FirstOrDefault(),
                CronExpression = x.Schedules.OrderBy(s => s.NextRunAtUtc).Select(s => s.CronExpression).FirstOrDefault(),
                NextRunAtUtc = x.Schedules.OrderBy(s => s.NextRunAtUtc).Select(s => s.NextRunAtUtc).FirstOrDefault(),
                IsPaused = x.Schedules.Select(s => s.IsPaused).FirstOrDefault()
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<BackgroundSchedulerExecutionDto>> GetExecutionsAsync(string? status = null, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.BackgroundJobExecutions.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(x => x.Status == status);
        }

        return await query
            .OrderByDescending(x => x.QueuedAtUtc ?? x.StartedAtUtc ?? x.CompletedAtUtc)
            .Take(300)
            .Select(x => new BackgroundSchedulerExecutionDto
            {
                Id = x.Id,
                JobDefinitionId = x.JobDefinitionId,
                JobCode = x.JobDefinition != null ? x.JobDefinition.Code : x.JobType,
                JobName = x.JobName,
                Status = x.Status,
                QueuedAtUtc = x.QueuedAtUtc,
                StartedAtUtc = x.StartedAtUtc,
                CompletedAtUtc = x.CompletedAtUtc,
                DurationMs = x.DurationMs,
                RetryAttempt = x.RetryAttempt,
                TriggerSource = x.TriggerSource,
                CancellationRequested = x.CancellationRequested,
                ErrorMessage = x.ErrorMessage ?? x.Error ?? x.Message
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<BackgroundSchedulerLogDto>> GetLogsAsync(Guid executionId, CancellationToken cancellationToken = default)
        => await _dbContext.BackgroundJobLogs.AsNoTracking()
            .Where(x => x.ExecutionId == executionId)
            .OrderBy(x => x.TimestampUtc)
            .Select(x => new BackgroundSchedulerLogDto
            {
                Id = x.Id,
                ExecutionId = x.ExecutionId,
                TimestampUtc = x.TimestampUtc,
                Severity = x.Severity,
                Message = x.Message,
                MetadataJson = x.MetadataJson
            })
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<BackgroundSchedulerDurationPointDto>> GetDurationChartMetadataAsync(CancellationToken cancellationToken = default)
    {
        var executions = await _dbContext.BackgroundJobExecutions.AsNoTracking()
            .Where(x => x.Status == "Completed" && x.DurationMs > 0)
            .OrderByDescending(x => x.CompletedAtUtc ?? x.StartedAtUtc ?? x.QueuedAtUtc)
            .Take(200)
            .Select(x => new { x.JobType, x.DurationMs })
            .ToListAsync(cancellationToken);

        return executions
            .GroupBy(x => x.JobType)
            .Select(group => new BackgroundSchedulerDurationPointDto
            {
                Label = group.Key,
                AverageDurationMs = group.Average(x => (double)x.DurationMs),
                ExecutionCount = group.Count()
            })
            .OrderByDescending(x => x.AverageDurationMs)
            .ToList();
    }

    public async Task<IReadOnlyList<BackgroundSchedulerFailureSummaryDto>> GetFailureSummaryAsync(CancellationToken cancellationToken = default)
    {
        var executions = await _dbContext.BackgroundJobExecutions.AsNoTracking()
            .Where(x => x.Status == "Failed" || x.Status == "TimedOut" || x.Status == "Cancelled" || x.Status == "Skipped")
            .OrderByDescending(x => x.CompletedAtUtc ?? x.StartedAtUtc ?? x.QueuedAtUtc)
            .Take(300)
            .Select(x => new
            {
                x.Status,
                Error = x.ErrorMessage ?? x.Error ?? x.Message,
                OccurredAt = x.CompletedAtUtc ?? x.StartedAtUtc ?? x.QueuedAtUtc
            })
            .ToListAsync(cancellationToken);

        return executions
            .GroupBy(x => x.Status)
            .Select(group => new BackgroundSchedulerFailureSummaryDto
            {
                Status = group.Key,
                Count = group.Count(),
                LatestError = group.FirstOrDefault(x => !string.IsNullOrWhiteSpace(x.Error))?.Error,
                LastOccurredAtUtc = group.Max(x => x.OccurredAt)
            })
            .OrderByDescending(x => x.Count)
            .ToList();
    }

    public async Task<Guid> RunNowAsync(string jobCode, string? requestedBy = null, CancellationToken cancellationToken = default)
    {
        await EnsureDefaultsAsync(cancellationToken);
        var definition = await _dbContext.BackgroundJobDefinitions.Include(x => x.Schedules).FirstAsync(x => x.Code == jobCode, cancellationToken);
        if (!definition.IsEnabled)
        {
            throw new InvalidOperationException($"Job {jobCode} is disabled.");
        }

        var execution = CreateExecution(definition, "manual", requestedBy, retryAttempt: 0, DateTime.UtcNow);
        _dbContext.BackgroundJobExecutions.Add(execution);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await ExecuteAsync(execution.Id, cancellationToken);
        return execution.Id;
    }

    public async Task<int> QueueDueAsync(DateTime utcNow, CancellationToken cancellationToken = default)
    {
        await EnsureDefaultsAsync(cancellationToken);

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken);
        if (string.Equals(_dbContext.Database.ProviderName, "Microsoft.EntityFrameworkCore.SqlServer", StringComparison.Ordinal))
        {
            await AcquireSqlServerSchedulerMutationLockAsync(cancellationToken);
        }

        var dueSchedules = await _dbContext.BackgroundJobSchedules
            .Include(x => x.JobDefinition)
            .Where(x => !x.IsPaused && x.NextRunAtUtc != null && x.NextRunAtUtc <= utcNow && x.JobDefinition != null && x.JobDefinition.IsEnabled)
            .ToListAsync(cancellationToken);

        var queued = 0;
        foreach (var schedule in dueSchedules)
        {
            if (schedule.JobDefinition is null)
            {
                continue;
            }

            if (!schedule.JobDefinition.AllowConcurrentExecution)
            {
                var activeExists = await _dbContext.BackgroundJobExecutions.AnyAsync(x =>
                    x.JobDefinitionId == schedule.JobDefinitionId &&
                    (x.Status == "Queued" || x.Status == "Running" || x.Status == "Cancelling"),
                    cancellationToken);

                if (activeExists)
                {
                    continue;
                }
            }

            _dbContext.BackgroundJobExecutions.Add(CreateExecution(schedule.JobDefinition, "scheduler", "scheduler", 0, utcNow));
            schedule.LastRunAtUtc = utcNow;
            schedule.NextRunAtUtc = ComputeNextRun(utcNow, schedule.IntervalMinutes, schedule.CronExpression) ?? utcNow.AddMinutes(schedule.IntervalMinutes ?? 60);
            queued++;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return queued;
    }

    private async Task AcquireSqlServerSchedulerMutationLockAsync(CancellationToken cancellationToken)
    {
        var connection = _dbContext.Database.GetDbConnection();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            DECLARE @lockResult int;
            EXEC @lockResult = sys.sp_getapplock
                @Resource = N'ASMILE.BO.BackgroundScheduler.Mutations',
                @LockMode = N'Exclusive',
                @LockOwner = N'Transaction',
                @LockTimeout = 10000,
                @DbPrincipal = N'public';
            SELECT @lockResult;
            """;
        command.CommandTimeout = 15;
        command.Transaction = _dbContext.Database.CurrentTransaction?.GetDbTransaction();

        var result = await command.ExecuteScalarAsync(cancellationToken);
        var lockResult = Convert.ToInt32(result, System.Globalization.CultureInfo.InvariantCulture);
        if (lockResult < 0)
        {
            throw new InvalidOperationException($"Could not acquire the SQL Server scheduler queue lock (sp_getapplock={lockResult}).");
        }
    }

    public async Task<bool> ExecuteNextQueuedAsync(CancellationToken cancellationToken = default)
    {
        Guid? executionId;
        await using (var transaction = await _dbContext.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken))
        {
            if (string.Equals(_dbContext.Database.ProviderName, "Microsoft.EntityFrameworkCore.SqlServer", StringComparison.Ordinal))
            {
                await AcquireSqlServerSchedulerMutationLockAsync(cancellationToken);
            }

            var queued = await _dbContext.BackgroundJobExecutions
                .Include(x => x.JobDefinition)
                .Where(x => x.Status == "Queued" && x.JobDefinition != null)
                .OrderBy(x => x.QueuedAtUtc ?? x.StartedAtUtc ?? x.CompletedAtUtc)
                .FirstOrDefaultAsync(cancellationToken);

            if (queued is null)
            {
                await transaction.CommitAsync(cancellationToken);
                return false;
            }

            // Persist the claim before releasing the cross-process lock so another
            // desktop/worker instance cannot execute the same queued row.
            queued.Status = "Running";
            queued.StartedAtUtc = DateTime.UtcNow;
            queued.StartedAt = queued.StartedAtUtc.Value;
            queued.Message = "Execution claimed by scheduler.";
            await _dbContext.SaveChangesAsync(cancellationToken);
            executionId = queued.Id;
            await transaction.CommitAsync(cancellationToken);
        }

        await ExecuteAsync(executionId.Value, cancellationToken);
        return true;
    }

    public async Task RetryAsync(Guid executionId, CancellationToken cancellationToken = default)
    {
        var execution = await _dbContext.BackgroundJobExecutions.Include(x => x.JobDefinition).FirstAsync(x => x.Id == executionId, cancellationToken);
        if (execution.JobDefinition is null)
        {
            throw new InvalidOperationException("Job definition is missing.");
        }

        if (execution.RetryAttempt >= execution.JobDefinition.RetryCount)
        {
            throw new InvalidOperationException("Retry count exceeded.");
        }

        if (execution.Status is not ("Failed" or "TimedOut" or "Cancelled" or "Skipped"))
        {
            throw new InvalidOperationException("Only terminal executions can be retried.");
        }

        var retryExecution = CreateExecution(execution.JobDefinition, "retry", "scheduler", execution.RetryAttempt + 1, DateTime.UtcNow);
        _dbContext.BackgroundJobExecutions.Add(retryExecution);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await ExecuteAsync(retryExecution.Id, cancellationToken);
    }

    public async Task CancelAsync(Guid executionId, CancellationToken cancellationToken = default)
    {
        var execution = await _dbContext.BackgroundJobExecutions.FirstOrDefaultAsync(x => x.Id == executionId, cancellationToken);
        if (execution is null)
        {
            return;
        }

        execution.CancellationRequested = true;
        if (execution.Status == "Queued" && !ActiveExecutionTokens.ContainsKey(execution.Id))
        {
            execution.Status = "Cancelled";
            execution.CompletedAtUtc = DateTime.UtcNow;
            execution.CompletedAt = execution.CompletedAtUtc;
            execution.Message = "Queued execution cancelled before start.";
            execution.ErrorMessage = execution.Message;
            execution.Error = execution.Message;
        }
        else if (execution.Status is "Queued" or "Running")
        {
            execution.Status = "Cancelling";
            execution.Message = "Cancellation requested.";
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        await LogAsync(execution.Id, "Warning", "Cancellation requested.", null, cancellationToken);
        await AuditAsync("BackgroundJobExecution", execution.Id.ToString(), "Cancel", null, new { execution.Status, execution.CancellationRequested }, cancellationToken);

        if (ActiveExecutionTokens.TryGetValue(execution.Id, out var activeToken))
        {
            activeToken.Cancel();
        }
    }

    public async Task PauseScheduleAsync(Guid definitionId, CancellationToken cancellationToken = default)
    {
        var schedule = await _dbContext.BackgroundJobSchedules.Include(x => x.JobDefinition).FirstOrDefaultAsync(x => x.JobDefinitionId == definitionId, cancellationToken);
        if (schedule is null)
        {
            return;
        }

        schedule.IsPaused = true;
        if (schedule.JobDefinition is not null)
        {
            schedule.JobDefinition.UpdatedAtUtc = DateTime.UtcNow;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task ResumeScheduleAsync(Guid definitionId, CancellationToken cancellationToken = default)
    {
        var schedule = await _dbContext.BackgroundJobSchedules.Include(x => x.JobDefinition).FirstOrDefaultAsync(x => x.JobDefinitionId == definitionId, cancellationToken);
        if (schedule is null)
        {
            return;
        }

        schedule.IsPaused = false;
        schedule.NextRunAtUtc ??= ComputeNextRun(DateTime.UtcNow, schedule.IntervalMinutes, schedule.CronExpression) ?? DateTime.UtcNow.AddMinutes(schedule.IntervalMinutes ?? 60);
        if (schedule.JobDefinition is not null)
        {
            schedule.JobDefinition.UpdatedAtUtc = DateTime.UtcNow;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<int> RecoverStaleExecutionsAsync(DateTime utcNow, TimeSpan staleAfter, CancellationToken cancellationToken = default)
    {
        var recoveryWindow = staleAfter > TimeSpan.Zero ? staleAfter : DefaultStaleWindow;
        var cutoff = utcNow.Subtract(recoveryWindow);
        var candidates = await _dbContext.BackgroundJobExecutions
            .Include(x => x.JobDefinition)
            .Where(x => (x.Status == "Running" || x.Status == "Cancelling") && (x.StartedAtUtc ?? x.StartedAt) < cutoff)
            .ToListAsync(cancellationToken);
        var stale = candidates
            .Where(execution =>
            {
                var startedAt = execution.StartedAtUtc ?? execution.StartedAt;
                var timeoutSeconds = Math.Max(1, execution.JobDefinition?.TimeoutSeconds ?? 0);
                var executionWindow = TimeSpan.FromSeconds(timeoutSeconds) + ExecutionRecoveryGracePeriod;
                var requiredWindow = executionWindow > recoveryWindow ? executionWindow : recoveryWindow;
                return startedAt < utcNow.Subtract(requiredWindow);
            })
            .ToList();

        foreach (var execution in stale)
        {
            execution.Status = "Failed";
            execution.CompletedAtUtc = utcNow;
            execution.CompletedAt = utcNow;
            execution.Message = "Recovered stale execution after application restart.";
            execution.ErrorMessage = execution.Message;
            execution.Error = execution.Message;
            await LogAsync(execution.Id, "Warning", execution.Message, null, cancellationToken);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return stale.Count;
    }

    public static DateTime? ComputeNextRun(DateTime utcNow, int? intervalMinutes, string? cronExpression)
    {
        if (intervalMinutes.HasValue && intervalMinutes.Value > 0)
        {
            return utcNow.AddMinutes(intervalMinutes.Value);
        }

        if (!string.IsNullOrWhiteSpace(cronExpression) && TryComputeCronNextRun(utcNow, cronExpression, out var nextRun))
        {
            return nextRun;
        }

        return null;
    }

    private async Task ExecuteAsync(Guid executionId, CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var execution = await db.BackgroundJobExecutions.Include(x => x.JobDefinition).FirstAsync(x => x.Id == executionId, cancellationToken);
        var definition = execution.JobDefinition ?? throw new InvalidOperationException("Job definition is missing.");
        using var correlationScope = _auditCorrelationService?.BeginScope(execution.CorrelationId);

        if (execution.CancellationRequested || execution.Status == "Cancelling")
        {
            execution.Status = "Cancelled";
            execution.Message = "Execution cancelled before start.";
            execution.CompletedAtUtc = DateTime.UtcNow;
            execution.CompletedAt = execution.CompletedAtUtc;
            execution.ErrorMessage = execution.Message;
            execution.Error = execution.Message;
            await db.SaveChangesAsync(cancellationToken);
            await AuditAsync("BackgroundJobExecution", execution.Id.ToString(), "CancelledBeforeStart", null, new { execution.Status, execution.Message }, cancellationToken);
            return;
        }

        var gate = JobLocks.GetOrAdd(definition.Code, _ => new SemaphoreSlim(1, 1));
        var acquired = definition.AllowConcurrentExecution || await gate.WaitAsync(0, cancellationToken);
        if (!acquired)
        {
            execution.Status = "Skipped";
            execution.ErrorMessage = "Concurrent execution skipped.";
            execution.Message = execution.ErrorMessage;
            await LogScopedAsync(db, execution.Id, "Warning", execution.ErrorMessage, null, cancellationToken);
            await db.SaveChangesAsync(cancellationToken);
            return;
        }

        using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(Math.Max(1, definition.TimeoutSeconds)));
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);
        ActiveExecutionTokens[execution.Id] = linkedCts;
        await db.Entry(execution).ReloadAsync(cancellationToken);
        if (execution.CancellationRequested || execution.Status == "Cancelling")
        {
            linkedCts.Cancel();
        }

        var sw = Stopwatch.StartNew();
        WorkflowExecutionDto? workflowExecution = null;
        WorkflowExecutionStepDto? workflowStep = null;
        try
        {
            execution.Status = "Running";
            execution.StartedAtUtc = DateTime.UtcNow;
            execution.StartedAt = execution.StartedAtUtc.Value;
            execution.Message = "Execution started.";
            await db.SaveChangesAsync(cancellationToken);

            await LogScopedAsync(db, execution.Id, "Information", "Execution started.", null, cancellationToken);
            await AuditAsync("BackgroundJobExecution", execution.Id.ToString(), "Start", null, new { execution.JobType, execution.Status, execution.RetryAttempt }, cancellationToken);
            if (_workflowResilienceService is not null)
            {
                workflowExecution = await _workflowResilienceService.BeginWorkflowAsync(
                    $"scheduler:{definition.Code.ToLowerInvariant()}",
                    execution.Id.ToString("N"),
                    execution.Id.ToString("N"),
                    new { execution.JobType, execution.TriggerSource, execution.RetryAttempt },
                    execution.CorrelationId,
                    null,
                    cancellationToken);
                workflowStep = await _workflowResilienceService.BeginStepAsync(workflowExecution.Id, "job-execution", new { execution.JobType, execution.TriggerSource }, cancellationToken);
            }
            await ExecuteKnownJobAsync(definition.Code, linkedCts.Token, db, execution, definition.TimeoutSeconds);

            execution.Status = "Completed";
            execution.Message = "Execution completed.";
            execution.ErrorMessage = null;
            if (_workflowResilienceService is not null && workflowStep is not null)
            {
                await _workflowResilienceService.CompleteStepAsync(workflowStep.Id, new { execution.Status }, cancellationToken);
            }
            await LogScopedAsync(db, execution.Id, "Information", "Execution completed.", null, cancellationToken);
            await AuditAsync("BackgroundJobExecution", execution.Id.ToString(), "Complete", null, new { execution.JobType, execution.Status, execution.DurationMs }, cancellationToken);
        }
        catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
        {
            execution.Status = "TimedOut";
            execution.ErrorMessage = "Execution timed out.";
            execution.Error = execution.ErrorMessage;
            execution.Message = execution.ErrorMessage;
            if (_workflowResilienceService is not null && workflowStep is not null)
            {
                await _workflowResilienceService.FailStepAsync(workflowStep.Id, new TimeoutException(execution.ErrorMessage), new { execution.Status }, CancellationToken.None);
            }
            await LogScopedAsync(db, execution.Id, "Error", execution.ErrorMessage, null, CancellationToken.None);
            await AuditAsync("BackgroundJobExecution", execution.Id.ToString(), "TimedOut", null, new { execution.JobType, execution.Status }, CancellationToken.None);
        }
        catch (OperationCanceledException)
        {
            execution.Status = "Cancelled";
            execution.CancellationRequested = true;
            execution.Message = "Execution cancelled.";
            execution.ErrorMessage = execution.Message;
            execution.Error = execution.Message;
            if (_workflowResilienceService is not null && workflowStep is not null)
            {
                await _workflowResilienceService.FailStepAsync(workflowStep.Id, new OperationCanceledException(execution.Message), new { execution.Status }, CancellationToken.None);
            }
            await LogScopedAsync(db, execution.Id, "Warning", execution.Message, null, CancellationToken.None);
            await AuditAsync("BackgroundJobExecution", execution.Id.ToString(), "Cancelled", null, new { execution.JobType, execution.Status, execution.CancellationRequested }, CancellationToken.None);
        }
        catch (Exception ex)
        {
            execution.Status = "Failed";
            execution.ErrorMessage = ex.Message;
            execution.Error = ex.Message;
            execution.Message = ex.Message;
            if (_workflowResilienceService is not null && workflowStep is not null)
            {
                await _workflowResilienceService.FailStepAsync(workflowStep.Id, ex, new { execution.Status }, CancellationToken.None);
            }
            await LogScopedAsync(db, execution.Id, "Error", ex.Message, ex.ToString(), CancellationToken.None);
            await AuditAsync("BackgroundJobExecution", execution.Id.ToString(), "Failed", null, new { execution.JobType, execution.Status, execution.ErrorMessage }, CancellationToken.None);
            if (_runtimeTelemetryService is not null)
            {
                await _runtimeTelemetryService.TrackExceptionAsync(ex, "scheduler", definition.Code, ex.Message, execution.CorrelationId, null, new { execution.Id, execution.JobType, execution.RetryAttempt }, CancellationToken.None);
            }
            _logger.LogError(ex, "Background scheduler job {JobCode} failed.", definition.Code);
        }
        finally
        {
            sw.Stop();
            if (execution.CancellationRequested)
            {
                execution.Status = "Cancelled";
                execution.Message = execution.Message ?? "Execution cancelled.";
                execution.ErrorMessage = execution.ErrorMessage ?? execution.Message;
                execution.Error = execution.Error ?? execution.ErrorMessage;
            }

            execution.CompletedAtUtc = DateTime.UtcNow;
            execution.CompletedAt = execution.CompletedAtUtc;
            execution.DurationMs = sw.ElapsedMilliseconds;
            await db.SaveChangesAsync(CancellationToken.None);

            ActiveExecutionTokens.TryRemove(execution.Id, out _);

            if (!definition.AllowConcurrentExecution)
            {
                gate.Release();
            }

            if ((execution.Status is "Failed" or "TimedOut") && execution.RetryAttempt < definition.RetryCount && !execution.CancellationRequested)
            {
                var retryExecution = CreateExecution(definition, "retry", execution.RequestedBy, execution.RetryAttempt + 1, DateTime.UtcNow);
                retryExecution.Message = $"Retry {retryExecution.RetryAttempt} queued after {execution.Status.ToLowerInvariant()}.";
                _dbContext.BackgroundJobExecutions.Add(retryExecution);
                await _dbContext.SaveChangesAsync(CancellationToken.None);
                await LogAsync(execution.Id, "Warning", retryExecution.Message!, null, CancellationToken.None);
            }

            if (_runtimeTelemetryService is not null)
            {
                await _runtimeTelemetryService.TrackSlowOperationAsync("scheduler", definition.Code, execution.DurationMs, execution.Status == "Completed" ? "Information" : "Warning", execution.Message, execution.CorrelationId, null, new { execution.Status, execution.RetryAttempt, execution.TriggerSource }, CancellationToken.None);
                await _runtimeTelemetryService.TrackMetricAsync("scheduler", execution.Status, "durationMs", execution.DurationMs, "ms", execution.Message, execution.CorrelationId, null, new { execution.JobType, execution.RetryAttempt }, CancellationToken.None);
            }
        }
    }

    private async Task ExecuteKnownJobAsync(string jobCode, CancellationToken cancellationToken, AppDbContext db, BackgroundJobExecution execution, int timeoutSeconds)
    {
        var delay = jobCode == "CANCEL_TEST"
            // The internal cancellation probe must stay alive long enough for
            // the caller to observe Running and issue CancelAsync. It is not a
            // production job code and is used only by the scheduler test suite.
            ? 5000
            : timeoutSeconds <= 1
                ? 1500
                : jobCode switch
        {
                "DEPRECIATION" => 120,
                "TOOL_ALLOCATION" => 120,
                "VAT_SYNC" => 80,
                "REPORT_SNAPSHOT_CLEANUP" => 50,
                "AUDIT_CLEANUP" => 50,
                "TEMP_CLEANUP" => 40,
                "SEARCH_INDEX_REFRESH" => 90,
                "WORKSPACE_CLEANUP" => 40,
                "ARCHIVE_CLEANUP" => 35,
                "RETENTION_ENFORCEMENT" => 90,
                "INTEGRITY_VERIFICATION" => 60,
                "TELEMETRY_CLEANUP" => 45,
                _ => 25
        };

        if (jobCode is "ARCHIVE_CLEANUP")
        {
            using var archiveScope = _scopeFactory.CreateScope();
            var archive = archiveScope.ServiceProvider.GetService<IArchiveService>();
            if (archive is not null)
            {
                await archive.PurgeExpiredAsync(cancellationToken);
            }
        }
        else if (jobCode is "RETENTION_ENFORCEMENT")
        {
            using var archiveScope = _scopeFactory.CreateScope();
            var archive = archiveScope.ServiceProvider.GetService<IArchiveService>();
            if (archive is not null)
            {
                await archive.RunArchiveAsync("search", progress: null, cancellationToken: cancellationToken);
            }
        }
        else if (jobCode is "INTEGRITY_VERIFICATION")
        {
            using var archiveScope = _scopeFactory.CreateScope();
            var archive = archiveScope.ServiceProvider.GetService<IArchiveService>();
            if (archive is not null)
            {
                await archive.ValidateArchiveIntegrityAsync(cancellationToken: cancellationToken);
            }
        }
        else if (jobCode is "TELEMETRY_CLEANUP")
        {
            if (_runtimeTelemetryService is not null)
            {
                await _runtimeTelemetryService.CleanupTelemetryAsync(DateTime.UtcNow, TimeSpan.FromDays(90), cancellationToken);
            }
        }
        else if (jobCode is "EINVOICE_ISSUE_QUEUE")
        {
            using var invoiceScope = _scopeFactory.CreateScope();
            var invoiceService = invoiceScope.ServiceProvider.GetRequiredService<IEInvoiceService>();
            var processed = 0;
            const int maxPerTick = 25;
            while (processed < maxPerTick)
            {
                var result = await invoiceService.ProcessNextQueuedIssueAsync(cancellationToken);
                if (result.Status is "Idle" or "Busy")
                {
                    break;
                }

                processed++;
            }

            await LogScopedAsync(db, execution.Id, "Information", $"E-invoice issue queue processed {processed} item(s).", null, cancellationToken);
        }
        else if (jobCode is "DATABASE_BACKUP")
        {
            using var backupScope = _scopeFactory.CreateScope();
            var backup = backupScope.ServiceProvider.GetRequiredService<IBackupService>();
            var record = await backup.BackupNowAsync(cancellationToken);
            await LogScopedAsync(db, execution.Id, "Information", $"Database backup created and verified: {record.FileName}.", null, cancellationToken);
        }
        else
        {
            await Task.Delay(delay, cancellationToken);
        }

        if (jobCode is "ARCHIVE_CLEANUP" or "RETENTION_ENFORCEMENT" or "INTEGRITY_VERIFICATION" or "TELEMETRY_CLEANUP")
        {
            await db.Entry(execution).ReloadAsync(cancellationToken);
        }

        await LogScopedAsync(db, execution.Id, "Information", $"Job {jobCode} executed.", null, cancellationToken);
    }

    private BackgroundJobExecution CreateExecution(BackgroundJobDefinition definition, string triggerSource, string? requestedBy, int retryAttempt, DateTime utcNow) => new()
    {
        JobDefinitionId = definition.Id,
        JobName = definition.Name,
        JobType = definition.Code,
        Status = "Queued",
        QueuedAtUtc = utcNow,
        StartedAtUtc = null,
        CompletedAtUtc = null,
        StartedAt = utcNow,
        TriggerSource = triggerSource,
        RequestedBy = requestedBy ?? "system",
        RetryAttempt = retryAttempt,
        CorrelationId = _auditCorrelationService?.EnsureCorrelationId() ?? Guid.NewGuid().ToString("N")
    };

    private async Task AuditAsync(string entityName, string entityId, string action, object? oldValues, object? newValues, CancellationToken cancellationToken)
    {
        if (_auditService is not null)
        {
            await _auditService.LogAsync(entityName, entityId, action, oldValues, newValues, "system", correlationId: _auditCorrelationService?.CurrentCorrelationId, cancellationToken: cancellationToken);
        }
    }

    private async Task LogAsync(Guid executionId, string severity, string message, string? metadataJson, CancellationToken cancellationToken)
    {
        _dbContext.BackgroundJobLogs.Add(new BackgroundJobLog
        {
            ExecutionId = executionId,
            Severity = severity,
            Message = message,
            MetadataJson = metadataJson
        });
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private static async Task LogScopedAsync(AppDbContext db, Guid executionId, string severity, string message, string? metadataJson, CancellationToken cancellationToken)
    {
        db.BackgroundJobLogs.Add(new BackgroundJobLog
        {
            ExecutionId = executionId,
            Severity = severity,
            Message = message,
            MetadataJson = metadataJson
        });
        await db.SaveChangesAsync(cancellationToken);
    }

    private static bool TryComputeCronNextRun(DateTime utcNow, string cronExpression, out DateTime nextRun)
    {
        nextRun = default;
        var parts = cronExpression.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length != 5)
        {
            return false;
        }

        var earliestCandidate = new DateTime(utcNow.Year, utcNow.Month, utcNow.Day, utcNow.Hour, utcNow.Minute, 0, DateTimeKind.Utc).AddMinutes(1);
        var allowedMinutes = Enumerable.Range(0, 60).Where(value => MatchesCronField(value, parts[0], 0, 59)).ToArray();
        var allowedHours = Enumerable.Range(0, 24).Where(value => MatchesCronField(value, parts[1], 0, 23)).ToArray();
        if (allowedMinutes.Length == 0 || allowedHours.Length == 0)
        {
            return false;
        }

        // A valid annual schedule such as February 29 can be more than a year
        // away. Search dates rather than every minute so the wider Gregorian
        // horizon stays cheap even when the configured schedule is sparse.
        var candidateDate = earliestCandidate.Date;
        for (var dayOffset = 0; dayOffset < 366 * 8; dayOffset++, candidateDate = candidateDate.AddDays(1))
        {
            if (!MatchesCronDate(candidateDate, parts))
            {
                continue;
            }

            foreach (var hour in allowedHours)
            {
                foreach (var minute in allowedMinutes)
                {
                    var candidate = candidateDate.AddHours(hour).AddMinutes(minute);
                    if (candidate < earliestCandidate)
                    {
                        continue;
                    }

                    nextRun = candidate;
                    return true;
                }
            }
        }

        return false;
    }

    private static bool MatchesCronDate(DateTime candidateDate, IReadOnlyList<string> parts)
    {
        var dayOfMonthMatches = MatchesCronField(candidateDate.Day, parts[2], 1, 31);
        var dayOfWeek = (int)candidateDate.DayOfWeek;
        var dayOfWeekMatches = MatchesCronField(dayOfWeek, parts[4], 0, 7)
            || (dayOfWeek == 0 && MatchesCronField(7, parts[4], 0, 7));

        // Standard five-field cron treats day-of-month and day-of-week as OR
        // when both fields are explicitly restricted. If either field is a
        // wildcard, only the other field constrains the date.
        var dayOfMonthIsWildcard = parts[2].Contains('*');
        var dayOfWeekIsWildcard = parts[4].Contains('*');
        var dayMatches = dayOfMonthIsWildcard || dayOfWeekIsWildcard
            ? dayOfMonthMatches && dayOfWeekMatches
            : dayOfMonthMatches || dayOfWeekMatches;

        return dayMatches && MatchesCronField(candidateDate.Month, parts[3], 1, 12);
    }

    private static bool MatchesCronField(int value, string token, int min, int max)
    {
        if (token == "*")
        {
            return true;
        }

        if (token.Contains(','))
        {
            var parts = token.Split(',', StringSplitOptions.TrimEntries);
            if (parts.Any(string.IsNullOrWhiteSpace))
            {
                return false;
            }

            foreach (var part in parts)
            {
                if (MatchesCronField(value, part, min, max))
                {
                    return true;
                }
            }

            return false;
        }

        var slashIndex = token.IndexOf('/');
        if (slashIndex >= 0)
        {
            if (slashIndex == 0 || slashIndex == token.Length - 1 || token.IndexOf('/', slashIndex + 1) >= 0
                || !int.TryParse(token[(slashIndex + 1)..], out var step) || step <= 0)
            {
                return false;
            }

            var rangeToken = token[..slashIndex];
            int start;
            int end;
            if (rangeToken == "*")
            {
                start = min;
                end = max;
            }
            else if (rangeToken.Contains('-'))
            {
                var range = rangeToken.Split('-', 2, StringSplitOptions.TrimEntries);
                if (range.Length != 2
                    || !int.TryParse(range[0], out start)
                    || !int.TryParse(range[1], out end)
                    || start < min || end > max || start > end)
                {
                    return false;
                }
            }
            else if (int.TryParse(rangeToken, out start) && start >= min && start <= max)
            {
                end = max;
            }
            else
            {
                return false;
            }

            return value >= start && value <= end && (value - start) % step == 0;
        }

        if (token.Contains('-'))
        {
            var range = token.Split('-', 2, StringSplitOptions.TrimEntries);
            return range.Length == 2
                && int.TryParse(range[0], out var start)
                && int.TryParse(range[1], out var end)
                && start >= min
                && end <= max
                && start <= end
                && value >= start
                && value <= end;
        }

        return int.TryParse(token, out var parsed) && parsed == value;
    }

    private sealed record BackgroundJobSeed(string Code, string Name, string Description, int IntervalMinutes, int RetryCount, int TimeoutSeconds);

    private async Task ApplyDatabaseBackupPolicyAsync(CancellationToken cancellationToken)
    {
        if (_settingsService is null)
        {
            return;
        }

        var enabledValue = (await _settingsService.GetValueAsync("Backup.EnableAutoBackup", SettingScope.System, null))?.Value?.Trim();
        var scheduleValue = (await _settingsService.GetValueAsync("Backup.Schedule", SettingScope.System, null))?.Value?.Trim();
        var enabled = ParseBoolean(enabledValue, defaultValue: false);
        if (!IsBooleanValue(enabledValue))
        {
            _logger.LogWarning("Automatic database backup disabled because Backup.EnableAutoBackup is missing or invalid.");
        }
        var definition = await _dbContext.BackgroundJobDefinitions
            .Include(x => x.Schedules)
            .SingleOrDefaultAsync(x => x.Code == "DATABASE_BACKUP", cancellationToken);
        if (definition is null)
        {
            return;
        }

        var schedule = definition.Schedules.OrderBy(x => x.Id).FirstOrDefault();
        if (schedule is null)
        {
            definition.Schedules.Add(schedule = new BackgroundJobSchedule());
        }

        var cron = string.IsNullOrWhiteSpace(scheduleValue) ? "0 0 * * *" : scheduleValue;
        var nextRun = ComputeNextRun(DateTime.UtcNow, intervalMinutes: null, cron);
        var policyIsValid = nextRun.HasValue;
        var desiredEnabled = enabled && policyIsValid;
        var changed = false;

        if (definition.IsEnabled != desiredEnabled)
        {
            definition.IsEnabled = desiredEnabled;
            changed = true;
        }

        if (!string.Equals(schedule.CronExpression, cron, StringComparison.Ordinal)
            || schedule.IntervalMinutes.HasValue)
        {
            schedule.CronExpression = cron;
            schedule.IntervalMinutes = null;
            schedule.NextRunAtUtc = desiredEnabled ? nextRun : null;
            changed = true;
        }
        else if (desiredEnabled && !schedule.NextRunAtUtc.HasValue)
        {
            schedule.NextRunAtUtc = nextRun;
            changed = true;
        }
        else if (!desiredEnabled && schedule.NextRunAtUtc.HasValue)
        {
            schedule.NextRunAtUtc = null;
            changed = true;
        }

        if (!policyIsValid)
        {
            _logger.LogWarning("Automatic database backup disabled because Backup.Schedule is not a valid five-field cron expression: {Schedule}", cron);
        }

        if (changed)
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    private static bool ParseBoolean(string? value, bool defaultValue)
    {
        if (string.IsNullOrWhiteSpace(value)) return defaultValue;
        if (bool.TryParse(value, out var parsed)) return parsed;
        return value.Trim() switch
        {
            "1" or "yes" or "on" => true,
            "0" or "no" or "off" => false,
            _ => defaultValue
        };
    }

    private static bool IsBooleanValue(string? value)
        => bool.TryParse(value, out _)
            || value?.Trim() is "1" or "yes" or "on" or "0" or "no" or "off";

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
    }
}
