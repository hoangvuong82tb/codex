using ASMILE.BO.Application.DTOs;
using ASMILE.BO.Application.Interfaces;
using ASMILE.BO.Core.Entities;
using ASMILE.BO.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ASMILE.BO.Infrastructure.Services;

public sealed class InfrastructureHealthService : IInfrastructureHealthService
{
    private readonly AppDbContext _dbContext;
    private readonly IDatabaseMigrationService _migrationService;
    private readonly ILogger<InfrastructureHealthService> _logger;

    public InfrastructureHealthService(AppDbContext dbContext, IDatabaseMigrationService migrationService, ILogger<InfrastructureHealthService> logger)
    {
        _dbContext = dbContext;
        _migrationService = migrationService;
        _logger = logger;
    }

    public async Task<InfrastructureHealthReportDto> CheckAsync(CancellationToken cancellationToken = default)
    {
        var checks = new List<InfrastructureHealthCheckDto>();

        try
        {
            var canConnect = await _dbContext.Database.CanConnectAsync(cancellationToken);
            checks.Add(new InfrastructureHealthCheckDto("Database", canConnect ? "Healthy" : "Unhealthy", canConnect ? "Kết nối database thành công." : "Không kết nối được database."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database health check failed.");
            checks.Add(new InfrastructureHealthCheckDto("Database", "Unhealthy", ex.Message));
        }

        try
        {
            var migrationStatus = await _migrationService.ValidatePendingMigrationsAsync(cancellationToken);
            var pendingMessage = migrationStatus.PendingMigrations.Count == 0
                ? "Không có migration pending."
                : $"Có {migrationStatus.PendingMigrations.Count} migration pending.";
            checks.Add(new InfrastructureHealthCheckDto("Migrations", migrationStatus.PendingMigrations.Count == 0 ? "Healthy" : "Warning", pendingMessage));

            foreach (var warning in migrationStatus.SeedWarnings)
            {
                checks.Add(new InfrastructureHealthCheckDto("Seed data", "Warning", warning));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Migration health check failed.");
            checks.Add(new InfrastructureHealthCheckDto("Migrations", "Unhealthy", ex.Message));
        }

        var status = checks.Any(x => x.Status == "Unhealthy") ? "Unhealthy" : checks.Any(x => x.Status == "Warning") ? "Warning" : "Healthy";
        return new InfrastructureHealthReportDto(status, DateTime.UtcNow, checks);
    }
}

public sealed class BackgroundJobRunner : IBackgroundJobRunner
{
    private static readonly BackgroundJobDefinitionDto[] Definitions =
    [
        new() { Name = "Import validation sweep", JobType = "Import", Description = "Validate pending import sessions for operational review.", CanRetry = true, CanCancel = true },
        new() { Name = "Report export cleanup", JobType = "ReportExport", Description = "Clean stale report export artifacts.", CanRetry = true, CanCancel = true },
        new() { Name = "Inventory cost recalculation preview", JobType = "Costing", Description = "Preview inventory cost recalculation workload.", CanRetry = true, CanCancel = true },
        new() { Name = "Closing preview refresh", JobType = "Closing", Description = "Refresh closing preview operational cache.", CanRetry = true, CanCancel = true },
        new() { Name = "Depreciation scheduler", JobType = "Depreciation", Description = "Run fixed asset depreciation jobs.", CanRetry = true, CanCancel = true },
        new() { Name = "Tool allocation scheduler", JobType = "Allocation", Description = "Run tool asset allocation jobs.", CanRetry = true, CanCancel = true },
        new() { Name = "VAT sync scheduler", JobType = "VatSync", Description = "Synchronize VAT documents and declarations.", CanRetry = true, CanCancel = true },
        new() { Name = "Report generation scheduler", JobType = "ReportGeneration", Description = "Generate scheduled reports.", CanRetry = true, CanCancel = true },
        new() { Name = "Operational cleanup", JobType = "Cleanup", Description = "Clean temporary operational data.", CanRetry = true, CanCancel = true }
    ];

    private readonly AppDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<BackgroundJobRunner> _logger;

    public BackgroundJobRunner(AppDbContext dbContext, ICurrentUserService currentUserService, ILogger<BackgroundJobRunner> logger)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task RunAsync(IBackgroundJob job, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(job);
        var correlationId = Guid.NewGuid().ToString("N");
        using var scope = _logger.BeginScope(new Dictionary<string, object> { ["JobName"] = job.Name, ["CorrelationId"] = correlationId });
        var started = DateTime.UtcNow;
        var execution = new BackgroundJobExecution
        {
            JobName = job.Name,
            JobType = Definitions.FirstOrDefault(x => string.Equals(x.Name, job.Name, StringComparison.OrdinalIgnoreCase))?.JobType ?? "AdHoc",
            Status = "Running",
            StartedAt = started,
            RequestedBy = _currentUserService.CurrentUser?.UserName ?? "system",
            CorrelationId = correlationId
        };
        _dbContext.BackgroundJobExecutions.Add(execution);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Starting background job {JobName}.", job.Name);
        try
        {
            await job.ExecuteAsync(cancellationToken);
            execution.Status = "Completed";
            execution.Message = "Job completed.";
            _logger.LogInformation("Completed background job {JobName}.", job.Name);
        }
        catch (OperationCanceledException)
        {
            execution.Status = "Cancelled";
            execution.CancellationRequested = true;
            execution.Message = "Job cancelled.";
            throw;
        }
        catch (Exception ex)
        {
            execution.Status = "Failed";
            execution.Error = ex.Message;
            _logger.LogError(ex, "Background job {JobName} failed.", job.Name);
            throw;
        }
        finally
        {
            execution.CompletedAt = DateTime.UtcNow;
            execution.DurationMs = (long)(execution.CompletedAt.Value - started).TotalMilliseconds;
            await _dbContext.SaveChangesAsync(CancellationToken.None);
        }
    }

    public Task<IReadOnlyList<BackgroundJobDefinitionDto>> GetDefinitionsAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<BackgroundJobDefinitionDto>>(Definitions);
    }

    public async Task<IReadOnlyList<BackgroundJobExecutionDto>> GetExecutionsAsync(string? status = null, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.BackgroundJobExecutions.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(x => x.Status == status);
        }

        return await query
            .OrderByDescending(x => x.StartedAt)
            .Take(200)
            .Select(x => new BackgroundJobExecutionDto
            {
                Id = x.Id,
                JobName = x.JobName,
                JobType = x.JobType,
                Status = x.Status,
                StartedAt = x.StartedAt,
                CompletedAt = x.CompletedAt,
                DurationMs = x.DurationMs,
                Message = x.Message,
                Error = x.Error,
                RequestedBy = x.RequestedBy,
                CancellationRequested = x.CancellationRequested
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<Guid> RetryAsync(string jobName, CancellationToken cancellationToken = default)
    {
        var job = new OperationalNoOpJob(jobName);
        await RunAsync(job, cancellationToken);
        var latest = await _dbContext.BackgroundJobExecutions
            .AsNoTracking()
            .Where(x => x.JobName == jobName)
            .OrderByDescending(x => x.StartedAt)
            .Select(x => x.Id)
            .FirstAsync(cancellationToken);
        return latest;
    }

    public async Task CancelAsync(Guid executionId, CancellationToken cancellationToken = default)
    {
        var execution = await _dbContext.BackgroundJobExecutions.FirstOrDefaultAsync(x => x.Id == executionId, cancellationToken);
        if (execution is null)
        {
            return;
        }

        execution.CancellationRequested = true;
        if (execution.Status is "Queued" or "Running")
        {
            execution.Status = "Cancelling";
            execution.Message = "Cancellation requested.";
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private sealed class OperationalNoOpJob : IBackgroundJob
    {
        public OperationalNoOpJob(string name)
        {
            Name = name;
        }

        public string Name { get; }

        public async Task ExecuteAsync(CancellationToken cancellationToken = default)
        {
            await Task.Delay(150, cancellationToken);
        }
    }
}
