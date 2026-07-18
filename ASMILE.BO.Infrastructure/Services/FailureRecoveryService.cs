using System.Text.Json;
using ASMILE.BO.Application.DTOs;
using ASMILE.BO.Application.Interfaces;
using ASMILE.BO.Core.Entities;
using ASMILE.BO.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ASMILE.BO.Infrastructure.Services;

public sealed class FailureRecoveryService : IFailureRecoveryService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly AppDbContext _dbContext;
    private readonly IRuntimeTelemetryService _telemetryService;
    private readonly IWorkflowResilienceService _workflowResilienceService;
    private readonly IEnterpriseConsistencyValidatorService _consistencyValidatorService;
    private readonly IAuditService? _auditService;
    private readonly IAuditCorrelationService? _correlationService;
    private readonly ICurrentUserService? _currentUserService;

    public FailureRecoveryService(AppDbContext dbContext, IRuntimeTelemetryService telemetryService, IWorkflowResilienceService workflowResilienceService, IEnterpriseConsistencyValidatorService consistencyValidatorService, IAuditService? auditService = null, IAuditCorrelationService? correlationService = null, ICurrentUserService? currentUserService = null)
    {
        _dbContext = dbContext;
        _telemetryService = telemetryService;
        _workflowResilienceService = workflowResilienceService;
        _consistencyValidatorService = consistencyValidatorService;
        _auditService = auditService;
        _correlationService = correlationService;
        _currentUserService = currentUserService;
    }

    public async Task<FailureAnalysisDto> AnalyzeFailureAsync(string? module = null, string? correlationId = null, CancellationToken cancellationToken = default)
    {
        var suggestions = await GenerateRecoverySuggestionsAsync(module, cancellationToken);
        var exceptions = await QueryExceptionsAsync(module, correlationId, cancellationToken);
        var uiFreezes = await QueryUiFreezesAsync(module, cancellationToken);
        var slowOps = await QuerySlowOperationsAsync(module, cancellationToken);

        await _telemetryService.TrackMetricAsync(
            "recovery",
            "analysis",
            "recurringFailures",
            exceptions.Count,
            "count",
            "Failure analysis completed.",
            correlationId,
            null,
            new { module, uiFreezes = uiFreezes.Count, slowOps = slowOps.Count },
            cancellationToken);

        return new FailureAnalysisDto
        {
            Module = module,
            RecurringFailureCount = exceptions.Count,
            UiFreezeCount = uiFreezes.Count,
            SlowOperationCount = slowOps.Count,
            Recommendations = suggestions
        };
    }

    public async Task<IReadOnlyList<RecoveryRecommendationDto>> GenerateRecoverySuggestionsAsync(string? module = null, CancellationToken cancellationToken = default)
    {
        var analysis = await BuildSuggestionsAsync(module, cancellationToken);
        if (analysis.Count == 0)
        {
            return [];
        }

        var now = DateTime.UtcNow;
        foreach (var suggestion in analysis)
        {
            if (await _dbContext.RecoveryRecommendations.AnyAsync(x =>
                    x.RecommendationCode == suggestion.RecommendationCode &&
                    x.Module == suggestion.Module &&
                    x.RecordedAtUtc >= now.AddHours(-1), cancellationToken))
            {
                continue;
            }

            _dbContext.RecoveryRecommendations.Add(new RecoveryRecommendation
            {
                Module = suggestion.Module,
                Category = suggestion.Category,
                Severity = suggestion.Severity,
                RecommendationCode = suggestion.RecommendationCode,
                SuggestedAction = suggestion.SuggestedAction,
                Message = suggestion.Message,
                ConfidenceScore = suggestion.ConfidenceScore,
                MetadataJson = suggestion.MetadataJson,
                CorrelationId = suggestion.CorrelationId,
                SessionId = suggestion.SessionId,
                RecordedAtUtc = now,
                MachineName = Environment.MachineName,
                UserName = _currentUserService?.CurrentUser?.UserName,
                CompanyId = _dbContext.CurrentCompanyId,
                BranchId = _dbContext.CurrentBranchId
            });
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        await AuditAsync("RecoveryRecommendation", module ?? "system", "GenerateSuggestions", new { count = analysis.Count }, cancellationToken);
        return analysis;
    }

    public async Task<IReadOnlyList<RecoveryRecommendationDto>> DetectRecurringFailureAsync(string? module = null, CancellationToken cancellationToken = default)
        => await BuildRecurringFailureSuggestionsAsync(module, cancellationToken);

    public async Task<IReadOnlyList<RecoveryRecommendationDto>> DetectUiFreezePatternsAsync(CancellationToken cancellationToken = default)
        => await BuildUiFreezeSuggestionsAsync(null, cancellationToken);

    private async Task<IReadOnlyList<RecoveryRecommendationDto>> BuildSuggestionsAsync(string? module, CancellationToken cancellationToken)
    {
        var recurring = await BuildRecurringFailureSuggestionsAsync(module, cancellationToken);
        var uiFreezes = await BuildUiFreezeSuggestionsAsync(module, cancellationToken);
        var slowOps = await BuildSlowOperationSuggestionsAsync(module, cancellationToken);
        return recurring.Concat(uiFreezes).Concat(slowOps).GroupBy(x => x.RecommendationCode).Select(x => x.First()).ToArray();
    }

    private async Task<IReadOnlyList<RecoveryRecommendationDto>> BuildRecurringFailureSuggestionsAsync(string? module, CancellationToken cancellationToken)
    {
        var exceptions = await QueryExceptionsAsync(module, null, cancellationToken);
        if (exceptions.Count == 0)
        {
            return [];
        }

        var grouped = exceptions.GroupBy(x => new { x.Module, x.StackHash }).OrderByDescending(x => x.Count()).Take(5);
        var results = new List<RecoveryRecommendationDto>();
        foreach (var group in grouped)
        {
            var count = group.Count();
            var code = $"RETRY_{group.Key.Module.ToUpperInvariant()}_{group.Key.StackHash[..8]}";
            results.Add(new RecoveryRecommendationDto
            {
                Module = group.Key.Module,
                Category = "RecurringFailure",
                Severity = count >= 5 ? "Error" : "Warning",
                RecommendationCode = code,
                SuggestedAction = group.Key.Module.Contains("scheduler", StringComparison.OrdinalIgnoreCase)
                    ? "Restart scheduler and retry failed jobs."
                    : "Retry failed operations and inspect recent exceptions.",
                Message = $"Detected {count} repeated exception(s) for {group.Key.Module}.",
                ConfidenceScore = Math.Min(1m, count / 10m),
                MetadataJson = JsonSerializer.Serialize(new { count, stackHash = group.Key.StackHash }, JsonOptions),
                CorrelationId = _correlationService?.CurrentCorrelationId,
                RecordedAtUtc = DateTime.UtcNow,
                UserName = _currentUserService?.CurrentUser?.UserName,
                MachineName = Environment.MachineName,
                Status = "Suggested"
            });
        }

        return results;
    }

    private async Task<IReadOnlyList<RecoveryRecommendationDto>> BuildUiFreezeSuggestionsAsync(string? module, CancellationToken cancellationToken)
    {
        var freezes = await QueryUiFreezesAsync(module, cancellationToken);
        if (freezes.Count == 0)
        {
            return [];
        }

        return
        [
            new RecoveryRecommendationDto
            {
                Module = module ?? "ui",
                Category = "UiFreeze",
                Severity = "Warning",
                RecommendationCode = "CLEAR_UI_CACHE",
                SuggestedAction = "Clear temp cache and compact workspace cache.",
                Message = $"Detected {freezes.Count} UI freeze sample(s).",
                ConfidenceScore = Math.Min(1m, freezes.Count / 10m),
                MetadataJson = JsonSerializer.Serialize(new { freezeCount = freezes.Count }, JsonOptions),
                CorrelationId = _correlationService?.CurrentCorrelationId,
                RecordedAtUtc = DateTime.UtcNow,
                UserName = _currentUserService?.CurrentUser?.UserName,
                MachineName = Environment.MachineName,
                Status = "Suggested"
            }
        ];
    }

    private async Task<IReadOnlyList<RecoveryRecommendationDto>> BuildSlowOperationSuggestionsAsync(string? module, CancellationToken cancellationToken)
    {
        var slowOps = await QuerySlowOperationsAsync(module, cancellationToken);
        if (slowOps.Count == 0)
        {
            return [];
        }

        var suggestions = new List<RecoveryRecommendationDto>();
        if (slowOps.Any(x => x.Module.Contains("search", StringComparison.OrdinalIgnoreCase)))
        {
            suggestions.Add(CreateSuggestion("REBUILD_SEARCH_INDEX", module ?? "search", "Rebuild search index.", "Search operations are slow.", slowOps.Count));
        }

        if (slowOps.Any(x => x.Module.Contains("archive", StringComparison.OrdinalIgnoreCase)))
        {
            suggestions.Add(CreateSuggestion("CLEANUP_ARCHIVE_TEMP", module ?? "archive", "Cleanup archive temp.", "Archive operations are slow.", slowOps.Count));
        }

        if (slowOps.Any(x => x.Module.Contains("scheduler", StringComparison.OrdinalIgnoreCase)))
        {
            suggestions.Add(CreateSuggestion("RESTART_SCHEDULER", module ?? "scheduler", "Restart scheduler and retry failed jobs.", "Scheduler operations are slow.", slowOps.Count));
        }

        if (slowOps.Any(x => x.Module.Contains("workspace", StringComparison.OrdinalIgnoreCase)))
        {
            suggestions.Add(CreateSuggestion("COMPACT_WORKSPACE_CACHE", module ?? "workspace", "Compact workspace cache.", "Workspace operations are slow.", slowOps.Count));
        }

        return suggestions;
    }

    private static RecoveryRecommendationDto CreateSuggestion(string code, string module, string action, string message, int count)
        => new()
        {
            Module = module,
            Category = "SlowOperation",
            Severity = "Warning",
            RecommendationCode = code,
            SuggestedAction = action,
            Message = message,
            ConfidenceScore = Math.Min(1m, count / 10m),
            MetadataJson = JsonSerializer.Serialize(new { count }, JsonOptions),
            RecordedAtUtc = DateTime.UtcNow,
            MachineName = Environment.MachineName,
            Status = "Suggested"
        };

    private Task<List<RuntimeExceptionLogDto>> QueryExceptionsAsync(string? module, string? correlationId, CancellationToken cancellationToken)
    {
        var query = _dbContext.RuntimeExceptionLogs.AsNoTracking().Where(x => !x.IsArchived);
        if (!string.IsNullOrWhiteSpace(module))
        {
            query = query.Where(x => x.Module == module);
        }

        if (!string.IsNullOrWhiteSpace(correlationId))
        {
            query = query.Where(x => x.CorrelationId == correlationId);
        }

        return query
            .OrderByDescending(x => x.RecordedAtUtc)
            .Take(200)
            .Select(x => new RuntimeExceptionLogDto
            {
                Id = x.Id,
                Module = x.Module,
                Category = x.Category,
                Severity = x.Severity,
                ExceptionType = x.ExceptionType,
                StackHash = x.StackHash,
                Message = x.Message,
                MetadataJson = x.MetadataJson,
                OccurrenceCount = x.OccurrenceCount,
                CorrelationId = x.CorrelationId,
                SessionId = x.SessionId,
                CompanyId = x.CompanyId,
                BranchId = x.BranchId,
                MachineName = x.MachineName,
                UserName = x.UserName,
                RecordedAtUtc = x.RecordedAtUtc,
                IsArchived = x.IsArchived,
                ArchivedAtUtc = x.ArchivedAtUtc
            })
            .ToListAsync(cancellationToken);
    }

    private Task<List<UiResponsivenessSnapshotDto>> QueryUiFreezesAsync(string? module, CancellationToken cancellationToken)
    {
        var query = _dbContext.UiResponsivenessSnapshots.AsNoTracking().Where(x => !x.IsArchived && (x.IsFrozen || x.DurationMs >= 1000));
        if (!string.IsNullOrWhiteSpace(module))
        {
            query = query.Where(x => x.Module == module);
        }

        return query
            .OrderByDescending(x => x.RecordedAtUtc)
            .Take(200)
            .Select(x => new UiResponsivenessSnapshotDto
            {
                Id = x.Id,
                Module = x.Module,
                Category = x.Category,
                Severity = x.Severity,
                OperationName = x.OperationName,
                DurationMs = x.DurationMs,
                IsFrozen = x.IsFrozen,
                Message = x.Message,
                MetadataJson = x.MetadataJson,
                CorrelationId = x.CorrelationId,
                SessionId = x.SessionId,
                CompanyId = x.CompanyId,
                BranchId = x.BranchId,
                MachineName = x.MachineName,
                UserName = x.UserName,
                RecordedAtUtc = x.RecordedAtUtc,
                IsArchived = x.IsArchived,
                ArchivedAtUtc = x.ArchivedAtUtc
            })
            .ToListAsync(cancellationToken);
    }

    private Task<List<SlowOperationLogDto>> QuerySlowOperationsAsync(string? module, CancellationToken cancellationToken)
    {
        var query = _dbContext.SlowOperationLogs.AsNoTracking().Where(x => !x.IsArchived && x.DurationMs >= 500);
        if (!string.IsNullOrWhiteSpace(module))
        {
            query = query.Where(x => x.Module == module);
        }

        return query
            .OrderByDescending(x => x.RecordedAtUtc)
            .Take(200)
            .Select(x => new SlowOperationLogDto
            {
                Id = x.Id,
                Module = x.Module,
                Category = x.Category,
                Severity = x.Severity,
                OperationName = x.OperationName,
                DurationMs = x.DurationMs,
                Message = x.Message,
                MetadataJson = x.MetadataJson,
                CorrelationId = x.CorrelationId,
                SessionId = x.SessionId,
                CompanyId = x.CompanyId,
                BranchId = x.BranchId,
                MachineName = x.MachineName,
                UserName = x.UserName,
                RecordedAtUtc = x.RecordedAtUtc,
                IsArchived = x.IsArchived,
                ArchivedAtUtc = x.ArchivedAtUtc
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<int> RecoverInterruptedOperationsAsync(DateTime utcNow, CancellationToken cancellationToken = default)
    {
        var recovered = 0;
        var deadLetters = await _dbContext.WorkflowDeadLetters.Where(x => x.RecoveredAtUtc == null).ToListAsync(cancellationToken);
        foreach (var deadLetter in deadLetters)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (deadLetter.WorkflowExecutionId is null)
            {
                continue;
            }

            var execution = await _dbContext.WorkflowExecutions.FirstOrDefaultAsync(x => x.Id == deadLetter.WorkflowExecutionId.Value, cancellationToken);
            if (execution is null || execution.Status is "Completed" or "Cancelled")
            {
                continue;
            }

            await _workflowResilienceService.ResumeWorkflowAsync(execution.Id, deadLetter.PayloadJson, cancellationToken);
            deadLetter.RecoveredAtUtc = utcNow;
            deadLetter.Status = "Recovered";
            recovered++;
        }

        var checkpoints = await _dbContext.WorkflowRecoveryCheckpoints.Where(x => x.Status == "Created").ToListAsync(cancellationToken);
        foreach (var checkpoint in checkpoints)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await _workflowResilienceService.ResumeFromCheckpointAsync(checkpoint.Id, cancellationToken);
            recovered++;
        }

        if (recovered > 0)
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
            await AuditAsync("RecoveryOperation", "interrupted", "RecoverInterruptedOperations", new { recovered }, cancellationToken);
        }

        return recovered;
    }

    public async Task<int> TakeoverStaleWorkflowAsync(DateTime utcNow, CancellationToken cancellationToken = default)
    {
        var released = await _workflowResilienceService.ReleaseStaleLocksAsync(utcNow, cancellationToken);
        var cleaned = await _workflowResilienceService.CleanupStaleWorkflowAsync(utcNow, TimeSpan.FromMinutes(30), cancellationToken);
        if (released > 0 || cleaned > 0)
        {
            await AuditAsync("RecoveryOperation", "stale-workflow", "TakeoverStaleWorkflow", new { released, cleaned }, cancellationToken);
        }

        return released + cleaned;
    }

    public async Task<IReadOnlyList<RecoveryRecommendationDto>> DetectOrphanPatternsAsync(CancellationToken cancellationToken = default)
    {
        var violations = await _consistencyValidatorService.ValidateAsync(DateTime.UtcNow, cancellationToken);
        return violations.Select(x => new RecoveryRecommendationDto
        {
            Module = x.Module,
            Category = x.Category,
            Severity = x.Severity,
            RecommendationCode = $"{x.Category}_{x.EntityType}".ToUpperInvariant(),
            SuggestedAction = x.Recommendation,
            Message = $"{x.EntityType}:{x.EntityId}",
            ConfidenceScore = x.CanAutoRecover ? 0.8m : 0.5m,
            MetadataJson = x.MetadataJson,
            RecordedAtUtc = x.RecordedAtUtc,
            MachineName = Environment.MachineName,
            Status = x.CanAutoRecover ? "AutoRecoverable" : "Suggested"
        }).ToArray();
    }

    private async Task AuditAsync(string entityName, string entityId, string action, object? newValues, CancellationToken cancellationToken)
    {
        if (_auditService is null)
        {
            return;
        }

        await _auditService.LogAsync(entityName, entityId, action, null, newValues, _currentUserService?.CurrentUser?.UserName ?? "system", correlationId: _correlationService?.CurrentCorrelationId, cancellationToken: cancellationToken);
    }
}
