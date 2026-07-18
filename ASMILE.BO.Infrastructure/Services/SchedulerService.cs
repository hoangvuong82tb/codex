using ASMILE.BO.Application.DTOs;
using ASMILE.BO.Application.Interfaces;
using ASMILE.BO.Core.Entities;
using ASMILE.BO.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ASMILE.BO.Infrastructure.Services;

public sealed class SchedulerService : ISchedulerService
{
    private readonly AppDbContext _dbContext;

    public SchedulerService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<JobDefinitionDto>> GetDefinitionsAsync(CancellationToken cancellationToken = default)
    {
        await EnsureDefaultDefinitionsAsync(cancellationToken);
        return await _dbContext.JobDefinitions.AsNoTracking()
            .OrderBy(x => x.JobCode)
            .Select(x => new JobDefinitionDto
            {
                Id = x.Id,
                JobCode = x.JobCode,
                JobName = x.JobName,
                JobType = x.JobType,
                ScheduleType = x.ScheduleType,
                IntervalMinutes = x.IntervalMinutes,
                CronExpression = x.CronExpression,
                IsActive = x.IsActive,
                NextRunAt = x.NextRunAt
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<JobExecutionStatusDto>> GetExecutionsAsync(string? status = null, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.JobExecutions.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(x => x.Status == status);
        }

        return await query.OrderByDescending(x => x.QueuedAt).Take(300)
            .Select(x => new JobExecutionStatusDto
            {
                Id = x.Id,
                JobCode = x.JobCode,
                Status = x.Status,
                QueuedAt = x.QueuedAt,
                StartedAt = x.StartedAt,
                CompletedAt = x.CompletedAt,
                DurationMs = x.DurationMs,
                CancellationRequested = x.CancellationRequested,
                ErrorMessage = x.ErrorMessage
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<Guid> ManualTriggerAsync(string jobCode, string? triggeredBy = null, CancellationToken cancellationToken = default)
    {
        await EnsureDefaultDefinitionsAsync(cancellationToken);
        var definition = await _dbContext.JobDefinitions.FirstAsync(x => x.JobCode == jobCode, cancellationToken);
        var execution = new JobExecution
        {
            JobDefinitionId = definition.Id,
            JobCode = definition.JobCode,
            Status = "Queued",
            TriggeredBy = triggeredBy ?? "system"
        };
        _dbContext.JobExecutions.Add(execution);
        _dbContext.JobLogs.Add(new JobLog { JobExecutionId = execution.Id, Message = "Đã đưa job vào hàng đợi thủ công." });
        await _dbContext.SaveChangesAsync(cancellationToken);
        return execution.Id;
    }

    public async Task RetryAsync(Guid executionId, CancellationToken cancellationToken = default)
    {
        var execution = await _dbContext.JobExecutions.FirstAsync(x => x.Id == executionId, cancellationToken);
        var attempt = await _dbContext.JobRetries.CountAsync(x => x.JobExecutionId == executionId, cancellationToken) + 1;
        execution.Status = "Queued";
        execution.CancellationRequested = false;
        execution.ErrorMessage = null;
        _dbContext.JobRetries.Add(new JobRetry { JobExecutionId = execution.Id, AttemptNo = attempt, Status = "Queued" });
        _dbContext.JobLogs.Add(new JobLog { JobExecutionId = execution.Id, Message = $"Retry lần {attempt} đã được đưa vào hàng đợi." });
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task CancelAsync(Guid executionId, CancellationToken cancellationToken = default)
    {
        var execution = await _dbContext.JobExecutions.FirstOrDefaultAsync(x => x.Id == executionId, cancellationToken);
        if (execution is null)
        {
            return;
        }

        execution.CancellationRequested = true;
        if (execution.Status is "Queued" or "Running")
        {
            execution.Status = "Cancelling";
        }

        _dbContext.JobLogs.Add(new JobLog { JobExecutionId = execution.Id, Level = "Warning", Message = "Đã yêu cầu hủy job." });
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<int> QueueDueJobsAsync(DateTime utcNow, CancellationToken cancellationToken = default)
    {
        await EnsureDefaultDefinitionsAsync(cancellationToken);
        var due = await _dbContext.JobDefinitions
            .Where(x => x.IsActive && x.NextRunAt != null && x.NextRunAt <= utcNow)
            .ToListAsync(cancellationToken);
        foreach (var definition in due)
        {
            _dbContext.JobExecutions.Add(new JobExecution
            {
                JobDefinitionId = definition.Id,
                JobCode = definition.JobCode,
                Status = "Queued",
                TriggeredBy = "scheduler"
            });
            definition.NextRunAt = ComputeNextRun(definition, utcNow);
            definition.UpdatedAt = DateTime.UtcNow;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return due.Count;
    }

    private async Task EnsureDefaultDefinitionsAsync(CancellationToken cancellationToken)
    {
        if (await _dbContext.JobDefinitions.AnyAsync(cancellationToken))
        {
            return;
        }

        var now = DateTime.UtcNow;
        _dbContext.JobDefinitions.AddRange(
            Definition("DEPRECIATION", "Chạy khấu hao TSCĐ", "Depreciation", 1440, now.AddDays(1)),
            Definition("TOOL_ALLOCATION", "Chạy phân bổ CCDC", "Allocation", 1440, now.AddDays(1)),
            Definition("VAT_SYNC", "Đồng bộ VAT", "VatSync", 240, now.AddHours(4)),
            Definition("REPORT_GENERATION", "Sinh báo cáo nền", "ReportGeneration", 120, now.AddHours(2)),
            Definition("CLEANUP", "Dọn dữ liệu tạm", "Cleanup", 720, now.AddHours(12)));
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private static JobDefinition Definition(string code, string name, string type, int intervalMinutes, DateTime nextRun)
        => new()
        {
            JobCode = code,
            JobName = name,
            JobType = type,
            ScheduleType = "Interval",
            IntervalMinutes = intervalMinutes,
            NextRunAt = nextRun
        };

    private static DateTime? ComputeNextRun(JobDefinition definition, DateTime utcNow)
        => definition.ScheduleType == "Interval" && definition.IntervalMinutes.HasValue
            ? utcNow.AddMinutes(definition.IntervalMinutes.Value)
            : definition.NextRunAt;
}
