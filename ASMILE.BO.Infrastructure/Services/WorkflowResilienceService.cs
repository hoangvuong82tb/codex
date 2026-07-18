using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Collections.Concurrent;
using ASMILE.BO.Application.DTOs;
using ASMILE.BO.Application.Interfaces;
using ASMILE.BO.Core.Entities;
using ASMILE.BO.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ASMILE.BO.Infrastructure.Services;

public sealed partial class WorkflowResilienceService : IWorkflowResilienceService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly SemaphoreSlim BeginGate = new(1, 1);
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> ResourceGates = new(StringComparer.OrdinalIgnoreCase);
    private readonly AppDbContext _dbContext;
    private readonly IAuditCorrelationService? _correlationService;
    private readonly IAuditService? _auditService;
    private readonly IRuntimeTelemetryService? _telemetryService;
    private readonly ICurrentUserService? _currentUserService;
    private readonly IEnterpriseSearchService? _enterpriseSearchService;

    public WorkflowResilienceService(
        AppDbContext dbContext,
        IAuditCorrelationService? correlationService = null,
        IAuditService? auditService = null,
        IRuntimeTelemetryService? telemetryService = null,
        ICurrentUserService? currentUserService = null,
        IEnterpriseSearchService? enterpriseSearchService = null)
    {
        _dbContext = dbContext;
        _correlationService = correlationService;
        _auditService = auditService;
        _telemetryService = telemetryService;
        _currentUserService = currentUserService;
        _enterpriseSearchService = enterpriseSearchService;
    }

    public async Task<WorkflowExecutionDto> BeginWorkflowAsync(string workflowType, string workflowKey, string? idempotencyKey = null, object? payload = null, string? correlationId = null, Guid? parentWorkflowExecutionId = null, CancellationToken cancellationToken = default)
    {
        var normalizedType = Normalize(workflowType, nameof(workflowType));
        var normalizedKey = Normalize(workflowKey, nameof(workflowKey));
        var effectiveCorrelation = string.IsNullOrWhiteSpace(correlationId)
            ? _correlationService?.EnsureCorrelationId() ?? Guid.NewGuid().ToString("N")
            : correlationId.Trim();

        var payloadHash = ComputeHash(payload);
        var executionHash = ComputeHash(new { normalizedType, normalizedKey, idempotencyKey, effectiveCorrelation, payloadHash, parentWorkflowExecutionId });

        await BeginGate.WaitAsync(cancellationToken);
        try
        {
            if (!string.IsNullOrWhiteSpace(idempotencyKey))
            {
                var existingKey = await _dbContext.IdempotencyKeyEntries
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.WorkflowType == normalizedType && x.WorkflowKey == normalizedKey && x.IdempotencyKey == idempotencyKey, cancellationToken);
                if (existingKey is not null)
                {
                    var existingExecution = await _dbContext.WorkflowExecutions.AsNoTracking()
                        .OrderByDescending(x => x.StartedAtUtc)
                        .FirstOrDefaultAsync(x => x.WorkflowType == normalizedType && x.WorkflowKey == normalizedKey && x.IdempotencyKey == idempotencyKey, cancellationToken);
                    if (existingExecution is not null)
                    {
                        return ToDto(existingExecution);
                    }
                }
            }

            var duplicate = await _dbContext.WorkflowExecutions
                .AsNoTracking()
                .Where(x => x.WorkflowType == normalizedType && x.WorkflowKey == normalizedKey && (x.Status == "Started" || x.Status == "Running" || x.Status == "Retrying" || x.Status == "Waiting"))
                .OrderByDescending(x => x.StartedAtUtc)
                .FirstOrDefaultAsync(cancellationToken);
            if (duplicate is not null)
            {
                await PersistViolationAsync(duplicate, "Warning", "DuplicateExecution", "Bỏ qua workflow trùng đang chạy.", normalizedType, cancellationToken);
                return ToDto(duplicate);
            }

            var now = DateTime.UtcNow;
            var execution = new WorkflowExecution
            {
                CorrelationId = effectiveCorrelation,
                WorkflowType = normalizedType,
                WorkflowKey = normalizedKey,
                ParentWorkflowExecutionId = parentWorkflowExecutionId,
                Status = "Started",
                StartedAtUtc = now,
                LastHeartbeatUtc = now,
                IdempotencyKey = idempotencyKey?.Trim(),
                ExecutionHash = executionHash,
                PayloadHash = payloadHash,
                RecoveryStateJson = null,
                CompensationStateJson = null,
                MetadataJson = payload is null ? null : JsonSerializer.Serialize(payload, JsonOptions),
                MachineName = Environment.MachineName,
                UserName = _currentUserService?.CurrentUser?.UserName
            };

            _dbContext.WorkflowExecutions.Add(execution);
            if (!string.IsNullOrWhiteSpace(idempotencyKey))
            {
                _dbContext.IdempotencyKeyEntries.Add(new IdempotencyKeyEntry
                {
                    CorrelationId = effectiveCorrelation,
                    WorkflowType = normalizedType,
                    WorkflowKey = normalizedKey,
                    IdempotencyKey = idempotencyKey.Trim(),
                    ExecutionHash = executionHash,
                    PayloadHash = payloadHash,
                    Status = "InProgress",
                    StartedAtUtc = now,
                    MetadataJson = execution.MetadataJson
                });
            }

            _dbContext.DistributedOperationMarkers.Add(new DistributedOperationMarker
            {
                WorkflowExecution = execution,
                CorrelationId = effectiveCorrelation,
                WorkflowType = normalizedType,
                WorkflowKey = normalizedKey,
                StepName = "workflow-begin",
                Status = "Owned",
                MachineName = Environment.MachineName,
                UserName = _currentUserService?.CurrentUser?.UserName ?? "system",
                MetadataJson = JsonSerializer.Serialize(new { executionHash }, JsonOptions)
            });

            await _dbContext.SaveChangesAsync(cancellationToken);
            await AuditAsync("WorkflowExecution", execution.Id.ToString(), "Begin", new { execution.WorkflowType, execution.WorkflowKey, execution.IdempotencyKey }, cancellationToken);
            await IndexExecutionAsync(execution, cancellationToken);
            return ToDto(execution);
        }
        finally
        {
            BeginGate.Release();
        }
    }

    public async Task<WorkflowExecutionStepDto> BeginStepAsync(Guid workflowExecutionId, string stepName, object? metadata = null, CancellationToken cancellationToken = default)
    {
        var execution = await _dbContext.WorkflowExecutions.FirstAsync(x => x.Id == workflowExecutionId, cancellationToken);
        var step = new WorkflowExecutionStep
        {
            WorkflowExecutionId = workflowExecutionId,
            CorrelationId = execution.CorrelationId,
            StepName = Normalize(stepName, nameof(stepName)),
            Status = "Started",
            StartedAtUtc = DateTime.UtcNow,
            LastHeartbeatUtc = DateTime.UtcNow,
            MetadataJson = metadata is null ? null : JsonSerializer.Serialize(metadata, JsonOptions)
        };
        _dbContext.WorkflowExecutionSteps.Add(step);
        execution.Status = "Running";
        execution.LastHeartbeatUtc = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
        return ToDto(step);
    }

    public async Task<WorkflowExecutionStepDto> CompleteStepAsync(Guid stepId, object? metadata = null, CancellationToken cancellationToken = default)
    {
        var step = await _dbContext.WorkflowExecutionSteps.Include(x => x.WorkflowExecution).FirstAsync(x => x.Id == stepId, cancellationToken);
        step.Status = "Completed";
        step.CompletedAtUtc = DateTime.UtcNow;
        step.LastHeartbeatUtc = DateTime.UtcNow;
        step.MetadataJson = MergeMetadata(step.MetadataJson, metadata);

        if (step.WorkflowExecution is not null)
        {
            step.WorkflowExecution.LastHeartbeatUtc = DateTime.UtcNow;
            if (await _dbContext.WorkflowExecutionSteps.AllAsync(x => x.WorkflowExecutionId == step.WorkflowExecutionId && x.Status == "Completed", cancellationToken))
            {
                step.WorkflowExecution.Status = "Completed";
                step.WorkflowExecution.CompletedAtUtc = DateTime.UtcNow;
                await MarkIdempotencyCompletedAsync(step.WorkflowExecution, cancellationToken);
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return ToDto(step);
    }

    public async Task<WorkflowExecutionStepDto> FailStepAsync(Guid stepId, Exception exception, object? metadata = null, CancellationToken cancellationToken = default)
    {
        var step = await _dbContext.WorkflowExecutionSteps.Include(x => x.WorkflowExecution).FirstAsync(x => x.Id == stepId, cancellationToken);
        step.Status = "Failed";
        step.FailedAtUtc = DateTime.UtcNow;
        step.LastHeartbeatUtc = DateTime.UtcNow;
        step.ExceptionType = exception.GetType().FullName;
        step.ErrorMessage = exception.Message;
        step.MetadataJson = MergeMetadata(step.MetadataJson, metadata);
        step.RetryCount += 1;

        if (step.WorkflowExecution is not null)
        {
            step.WorkflowExecution.Status = "Failed";
            step.WorkflowExecution.FailedAtUtc = DateTime.UtcNow;
            step.WorkflowExecution.LastHeartbeatUtc = DateTime.UtcNow;
            step.WorkflowExecution.ExceptionType = exception.GetType().FullName;
            step.WorkflowExecution.ErrorMessage = exception.Message;
            step.WorkflowExecution.RetryCount += 1;

            _dbContext.WorkflowRetryHistories.Add(new WorkflowRetryHistory
            {
                WorkflowExecutionId = step.WorkflowExecutionId,
                WorkflowExecutionStepId = step.Id,
                CorrelationId = step.CorrelationId,
                WorkflowType = step.WorkflowExecution.WorkflowType,
                StepName = step.StepName,
                RetryCount = step.RetryCount,
                Status = "Failed",
                ExceptionType = step.ExceptionType,
                ErrorMessage = step.ErrorMessage,
                StartedAtUtc = step.StartedAtUtc,
                CompletedAtUtc = step.FailedAtUtc,
                MetadataJson = step.MetadataJson
            });
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        await AuditAsync("WorkflowExecutionStep", step.Id.ToString(), "Fail", new { step.StepName, step.ErrorMessage }, cancellationToken);
        if (_telemetryService is not null)
        {
            await _telemetryService.TrackExceptionAsync(exception, "workflow", step.StepName, step.ErrorMessage, step.CorrelationId, null, null, cancellationToken);
        }
        return ToDto(step);
    }

    public async Task<WorkflowExecutionDto> ResumeWorkflowAsync(Guid workflowExecutionId, object? recoveryState = null, CancellationToken cancellationToken = default)
    {
        var execution = await _dbContext.WorkflowExecutions.FirstAsync(x => x.Id == workflowExecutionId, cancellationToken);
        execution.Status = "Resumed";
        execution.LastHeartbeatUtc = DateTime.UtcNow;
        execution.RecoveryStateJson = recoveryState is null ? execution.RecoveryStateJson : JsonSerializer.Serialize(recoveryState, JsonOptions);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await IndexExecutionAsync(execution, cancellationToken);
        return ToDto(execution);
    }

    public async Task<WorkflowExecutionDto> RetryWorkflowAsync(Guid workflowExecutionId, string reason, CancellationToken cancellationToken = default)
    {
        var execution = await _dbContext.WorkflowExecutions.FirstAsync(x => x.Id == workflowExecutionId, cancellationToken);
        execution.Status = "Retrying";
        execution.RetryCount += 1;
        execution.LastHeartbeatUtc = DateTime.UtcNow;
        execution.MetadataJson = MergeMetadata(execution.MetadataJson, new { retryReason = reason, retriedAtUtc = DateTime.UtcNow });
        _dbContext.WorkflowRetryHistories.Add(new WorkflowRetryHistory
        {
            WorkflowExecutionId = execution.Id,
            CorrelationId = execution.CorrelationId,
            WorkflowType = execution.WorkflowType,
            RetryCount = execution.RetryCount,
            Status = "Retrying",
            StartedAtUtc = DateTime.UtcNow,
            MetadataJson = execution.MetadataJson
        });
        await _dbContext.SaveChangesAsync(cancellationToken);
        return ToDto(execution);
    }

    public async Task RegisterCompensationAsync(Guid workflowExecutionId, Guid? workflowExecutionStepId, string stepName, object? compensationState = null, CancellationToken cancellationToken = default)
    {
        var execution = await _dbContext.WorkflowExecutions.FirstAsync(x => x.Id == workflowExecutionId, cancellationToken);
        _dbContext.WorkflowCompensationActions.Add(new WorkflowCompensationAction
        {
            WorkflowExecutionId = workflowExecutionId,
            WorkflowExecutionStepId = workflowExecutionStepId,
            CorrelationId = execution.CorrelationId,
            StepName = Normalize(stepName, nameof(stepName)),
            Status = "Registered",
            CompensationStateJson = compensationState is null ? null : JsonSerializer.Serialize(compensationState, JsonOptions),
            StartedAtUtc = DateTime.UtcNow
        });
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<int> ExecuteCompensationAsync(Guid workflowExecutionId, CancellationToken cancellationToken = default)
    {
        var actions = await _dbContext.WorkflowCompensationActions
            .Where(x => x.WorkflowExecutionId == workflowExecutionId && (x.Status == "Registered" || x.Status == "Retrying"))
            .OrderByDescending(x => x.StartedAtUtc)
            .ToListAsync(cancellationToken);
        var count = 0;
        foreach (var action in actions)
        {
            cancellationToken.ThrowIfCancellationRequested();
            action.Status = "Completed";
            action.CompletedAtUtc = DateTime.UtcNow;
            count++;
        }

        var execution = await _dbContext.WorkflowExecutions.FirstOrDefaultAsync(x => x.Id == workflowExecutionId, cancellationToken);
        if (execution is not null && count > 0)
        {
            execution.CompensationStateJson = JsonSerializer.Serialize(new { completedActions = count, completedAtUtc = DateTime.UtcNow }, JsonOptions);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return count;
    }

    public async Task<IReadOnlyList<WorkflowConsistencyViolationDto>> ValidateConsistencyAsync(DateTime utcNow, CancellationToken cancellationToken = default)
    {
        var results = new List<WorkflowConsistencyViolation>();
        var staleCutoff = utcNow.AddMinutes(-15);

        var stale = await _dbContext.WorkflowExecutions
            .Where(x => (x.Status == "Started" || x.Status == "Running" || x.Status == "Retrying" || x.Status == "Waiting") && x.LastHeartbeatUtc <= staleCutoff)
            .ToListAsync(cancellationToken);
        foreach (var item in stale)
        {
            results.Add(new WorkflowConsistencyViolation
            {
                WorkflowExecutionId = item.Id,
                CorrelationId = item.CorrelationId,
                WorkflowType = item.WorkflowType,
                WorkflowKey = item.WorkflowKey,
                Severity = "Error",
                Category = "StaleHeartbeat",
                Recommendation = "Khôi phục workflow treo hoặc chạy retry có kiểm soát.",
                AffectedEntityModule = item.WorkflowType,
                MetadataJson = item.MetadataJson,
                RecordedAtUtc = utcNow
            });
        }

        var activeExecutions = await _dbContext.WorkflowExecutions
            .Where(x => x.Status == "Running" || x.Status == "Started" || x.Status == "Retrying")
            .ToListAsync(cancellationToken);
        var duplicates = activeExecutions
            .GroupBy(x => new { x.WorkflowType, x.WorkflowKey, x.CompanyId, x.BranchId })
            .Where(x => x.Count() > 1)
            .ToList();
        foreach (var group in duplicates)
        {
            results.Add(new WorkflowConsistencyViolation
            {
                CorrelationId = group.OrderByDescending(x => x.StartedAtUtc).First().CorrelationId,
                WorkflowType = group.Key.WorkflowType,
                WorkflowKey = group.Key.WorkflowKey,
                Severity = "Error",
                Category = "DuplicateExecution",
                Recommendation = "Giữ một execution, đánh dấu các execution dư là failed.",
                AffectedEntityModule = group.Key.WorkflowType,
                MetadataJson = JsonSerializer.Serialize(new { count = group.Count() }, JsonOptions),
                RecordedAtUtc = utcNow
            });
        }

        if (results.Count > 0)
        {
            _dbContext.WorkflowConsistencyViolations.AddRange(results);
            await _dbContext.SaveChangesAsync(cancellationToken);
            if (_enterpriseSearchService is not null)
            {
                foreach (var item in results)
                {
                    await _enterpriseSearchService.UpsertAsync(new SearchIndexUpsertRequest
                    {
                        EntityType = nameof(WorkflowConsistencyViolation),
                        EntityId = item.Id.ToString("N"),
                        Title = $"{item.Severity} {item.Category}",
                        Subtitle = item.WorkflowType,
                        Category = "workflow",
                        SearchText = $"{item.WorkflowType} {item.WorkflowKey} {item.Category} {item.Severity} {item.Recommendation}",
                        TargetType = "module",
                        TargetRoute = "operations-center/workflow",
                        SourceType = "workflow"
                    }, cancellationToken);
                }
            }
        }

        return results.Select(ToDto).ToArray();
    }

    public async Task<int> CleanupStaleWorkflowAsync(DateTime utcNow, TimeSpan staleAfter, CancellationToken cancellationToken = default)
    {
        var cutoff = utcNow.Subtract(staleAfter > TimeSpan.Zero ? staleAfter : TimeSpan.FromMinutes(15));
        var stale = await _dbContext.WorkflowExecutions
            .Where(x => (x.Status == "Started" || x.Status == "Running" || x.Status == "Retrying" || x.Status == "Waiting") && x.LastHeartbeatUtc <= cutoff)
            .ToListAsync(cancellationToken);
        foreach (var item in stale)
        {
            item.Status = "Failed";
            item.FailedAtUtc = utcNow;
            item.ErrorMessage = "Stale workflow execution recovered by cleanup.";
            item.ExceptionType = typeof(TimeoutException).FullName;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return stale.Count;
    }

    public async Task<IReadOnlyList<WorkflowExecutionDto>> GetWorkflowHistoryAsync(string? workflowType = null, int take = 100, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.WorkflowExecutions.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(workflowType))
        {
            query = query.Where(x => x.WorkflowType == workflowType);
        }

        return await query.OrderByDescending(x => x.StartedAtUtc)
            .Take(Math.Clamp(take, 1, 500))
            .Select(x => ToDto(x))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<WorkflowExecutionStepDto>> GetWorkflowStepsAsync(Guid workflowExecutionId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.WorkflowExecutionSteps.AsNoTracking()
            .Where(x => x.WorkflowExecutionId == workflowExecutionId)
            .OrderBy(x => x.StartedAtUtc)
            .Select(x => ToDto(x))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<WorkflowConsistencyViolationDto>> GetConsistencyViolationsAsync(int take = 100, CancellationToken cancellationToken = default)
    {
        return await _dbContext.WorkflowConsistencyViolations.AsNoTracking()
            .OrderByDescending(x => x.RecordedAtUtc)
            .Take(Math.Clamp(take, 1, 500))
            .Select(x => ToDto(x))
            .ToListAsync(cancellationToken);
    }

    private async Task PersistViolationAsync(WorkflowExecution execution, string severity, string category, string recommendation, string module, CancellationToken cancellationToken)
    {
        _dbContext.WorkflowConsistencyViolations.Add(new WorkflowConsistencyViolation
        {
            WorkflowExecutionId = execution.Id,
            CorrelationId = execution.CorrelationId,
            WorkflowType = execution.WorkflowType,
            WorkflowKey = execution.WorkflowKey,
            Severity = severity,
            Category = category,
            Recommendation = recommendation,
            AffectedEntityModule = module,
            RecordedAtUtc = DateTime.UtcNow
        });
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task MarkIdempotencyCompletedAsync(WorkflowExecution execution, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(execution.IdempotencyKey))
        {
            return;
        }

        var entry = await _dbContext.IdempotencyKeyEntries.FirstOrDefaultAsync(
            x => x.WorkflowType == execution.WorkflowType && x.WorkflowKey == execution.WorkflowKey && x.IdempotencyKey == execution.IdempotencyKey,
            cancellationToken);
        if (entry is null)
        {
            return;
        }

        entry.Status = "Completed";
        entry.CompletedAtUtc = DateTime.UtcNow;
    }

    private async Task AuditAsync(string entityType, string entityId, string action, object? payload, CancellationToken cancellationToken)
    {
        if (_auditService is null)
        {
            return;
        }

        await _auditService.LogAsync(entityType, entityId, action, null, payload, _currentUserService?.CurrentUser?.UserName ?? "system", correlationId: _correlationService?.CurrentCorrelationId, cancellationToken: cancellationToken);
    }

    private async Task IndexExecutionAsync(WorkflowExecution execution, CancellationToken cancellationToken)
    {
        if (_enterpriseSearchService is null)
        {
            return;
        }

        await _enterpriseSearchService.UpsertAsync(new SearchIndexUpsertRequest
        {
            EntityType = nameof(WorkflowExecution),
            EntityId = execution.Id.ToString("N"),
            Title = execution.WorkflowType,
            Subtitle = execution.WorkflowKey,
            Category = "workflow",
            SearchText = $"{execution.WorkflowType} {execution.WorkflowKey} {execution.Status} {execution.CorrelationId} {execution.ErrorMessage}",
            TargetType = "module",
            TargetRoute = "operations-center/workflow",
            SourceType = "workflow"
        }, cancellationToken);
    }

    private static string Normalize(string? value, string paramName)
        => string.IsNullOrWhiteSpace(value) ? throw new ArgumentException($"{paramName} is required.", paramName) : value.Trim();

    private static string ComputeHash(object? payload)
    {
        var json = payload is null ? "null" : JsonSerializer.Serialize(payload, JsonOptions);
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(json)));
    }

    private static string MergeMetadata(string? sourceJson, object? value)
    {
        if (value is null)
        {
            return sourceJson ?? string.Empty;
        }

        if (string.IsNullOrWhiteSpace(sourceJson))
        {
            return JsonSerializer.Serialize(value, JsonOptions);
        }

        return JsonSerializer.Serialize(new { previous = sourceJson, current = value }, JsonOptions);
    }

    private static WorkflowExecutionDto ToDto(WorkflowExecution x) => new()
    {
        Id = x.Id,
        CompanyId = x.CompanyId,
        BranchId = x.BranchId,
        ParentWorkflowExecutionId = x.ParentWorkflowExecutionId,
        CorrelationId = x.CorrelationId,
        WorkflowType = x.WorkflowType,
        WorkflowKey = x.WorkflowKey,
        Status = x.Status,
        RetryCount = x.RetryCount,
        StartedAtUtc = x.StartedAtUtc,
        CompletedAtUtc = x.CompletedAtUtc,
        FailedAtUtc = x.FailedAtUtc,
        LastHeartbeatUtc = x.LastHeartbeatUtc,
        IdempotencyKey = x.IdempotencyKey,
        ExecutionHash = x.ExecutionHash,
        PayloadHash = x.PayloadHash,
        RecoveryStateJson = x.RecoveryStateJson,
        CompensationStateJson = x.CompensationStateJson,
        ExceptionType = x.ExceptionType,
        ErrorMessage = x.ErrorMessage,
        MachineName = x.MachineName,
        UserName = x.UserName,
        MetadataJson = x.MetadataJson
    };

    private static WorkflowExecutionStepDto ToDto(WorkflowExecutionStep x) => new()
    {
        Id = x.Id,
        WorkflowExecutionId = x.WorkflowExecutionId,
        CorrelationId = x.CorrelationId,
        StepName = x.StepName,
        Status = x.Status,
        RetryCount = x.RetryCount,
        StartedAtUtc = x.StartedAtUtc,
        CompletedAtUtc = x.CompletedAtUtc,
        FailedAtUtc = x.FailedAtUtc,
        LastHeartbeatUtc = x.LastHeartbeatUtc,
        ExceptionType = x.ExceptionType,
        ErrorMessage = x.ErrorMessage,
        MetadataJson = x.MetadataJson
    };

    private static WorkflowConsistencyViolationDto ToDto(WorkflowConsistencyViolation x) => new()
    {
        Id = x.Id,
        WorkflowExecutionId = x.WorkflowExecutionId,
        CorrelationId = x.CorrelationId,
        WorkflowType = x.WorkflowType,
        WorkflowKey = x.WorkflowKey,
        Severity = x.Severity,
        Category = x.Category,
        Recommendation = x.Recommendation,
        AffectedEntityModule = x.AffectedEntityModule,
        MetadataJson = x.MetadataJson,
        RecordedAtUtc = x.RecordedAtUtc
    };
}

