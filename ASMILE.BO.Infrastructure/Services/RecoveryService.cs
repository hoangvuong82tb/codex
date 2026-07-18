using System.Text.Json;
using System.Diagnostics;
using System.Text.Json.Serialization;
using ASMILE.BO.Application.DTOs;
using ASMILE.BO.Application.Interfaces;
using ASMILE.BO.Core.Entities;
using ASMILE.BO.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ASMILE.BO.Infrastructure.Services;

public sealed class RecoveryService : IRecoveryService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        ReferenceHandler = ReferenceHandler.IgnoreCycles
    };
    private readonly AppDbContext _dbContext;
    private readonly IRuntimeTelemetryService? _telemetryService;
    private readonly IAuditService? _auditService;
    private readonly IAuditCorrelationService? _correlationService;
    private readonly ICurrentUserService? _currentUserService;

    public RecoveryService(AppDbContext dbContext, IAuditService? auditService = null, IAuditCorrelationService? correlationService = null, ICurrentUserService? currentUserService = null, IRuntimeTelemetryService? telemetryService = null)
    {
        _dbContext = dbContext;
        _telemetryService = telemetryService;
        _auditService = auditService;
        _correlationService = correlationService;
        _currentUserService = currentUserService;
    }

    public async Task<RecoveryCheckpointDto> CreateCheckpointAsync(string module = "system", CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();
        var checkpoint = new RecoveryCheckpoint
        {
            Module = module,
            EntityName = "RecoveryCheckpoint",
            Status = "Created",
            MigrationVersion = await GetLatestMigrationAsync(cancellationToken),
            SchedulerStateJson = JsonSerializer.Serialize(await BuildSchedulerStateAsync(cancellationToken), JsonOptions),
            WorkspaceMetadataJson = JsonSerializer.Serialize(await BuildWorkspaceStateAsync(cancellationToken), JsonOptions),
            AuditSessionMetadataJson = JsonSerializer.Serialize(await BuildAuditStateAsync(cancellationToken), JsonOptions),
            MetadataJson = JsonSerializer.Serialize(new { module, createdAt = DateTime.UtcNow }, JsonOptions),
            Hash = Guid.NewGuid().ToString("N"),
            PayloadSizeBytes = 0,
            CreatedAtUtc = DateTime.UtcNow,
            CreatedBy = _currentUserService?.CurrentUser?.UserName,
            CorrelationId = _correlationService?.CurrentCorrelationId
        };
        _dbContext.RecoveryCheckpoints.Add(checkpoint);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await AuditAsync("RecoveryCheckpoint", checkpoint.Id.ToString(), "CreateCheckpoint", checkpoint, cancellationToken);
        if (_telemetryService is not null)
        {
            sw.Stop();
            await _telemetryService.TrackSlowOperationAsync("recovery", "create-checkpoint", sw.ElapsedMilliseconds, "Information", "Recovery checkpoint created.", checkpoint.CorrelationId, null, new { checkpoint.Module }, cancellationToken);
        }
        return ToDto(checkpoint);
    }

    public async Task<RecoveryCheckpointDto> RestoreCheckpointAsync(Guid checkpointId, CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();
        var checkpoint = await _dbContext.RecoveryCheckpoints.FirstAsync(x => x.Id == checkpointId, cancellationToken);
        var operation = new RecoveryOperation
        {
            RecoveryCheckpointId = checkpoint.Id,
            OperationType = "Restore",
            Status = "Completed",
            CreatedAtUtc = DateTime.UtcNow,
            StartedAtUtc = DateTime.UtcNow,
            CompletedAtUtc = DateTime.UtcNow,
            CreatedBy = _currentUserService?.CurrentUser?.UserName,
            CorrelationId = _correlationService?.CurrentCorrelationId,
            MetadataJson = checkpoint.MetadataJson,
            Hash = checkpoint.Hash
        };
        _dbContext.RecoveryOperations.Add(operation);
        checkpoint.Status = "Restored";
        checkpoint.ArchivedAtUtc = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
        await AuditAsync("RecoveryCheckpoint", checkpoint.Id.ToString(), "RestoreCheckpoint", checkpoint, cancellationToken);
        if (_telemetryService is not null)
        {
            sw.Stop();
            await _telemetryService.TrackSlowOperationAsync("recovery", "restore-checkpoint", sw.ElapsedMilliseconds, "Information", "Recovery checkpoint restored.", checkpoint.CorrelationId, null, new { checkpointId }, cancellationToken);
        }
        return ToDto(checkpoint);
    }

    public async Task<IReadOnlyList<RecoveryTimelineEntryDto>> GetRecoveryTimelineAsync(int take = 100, CancellationToken cancellationToken = default)
    {
        return await _dbContext.RecoveryOperations.AsNoTracking()
            .OrderByDescending(x => x.CreatedAtUtc)
            .Take(Math.Clamp(take, 1, 500))
            .Select(x => new RecoveryTimelineEntryDto
            {
                Id = x.Id,
                OperationType = x.OperationType,
                Status = x.Status,
                CreatedAtUtc = x.CreatedAtUtc,
                StartedAtUtc = x.StartedAtUtc,
                CompletedAtUtc = x.CompletedAtUtc,
                CorrelationId = x.CorrelationId,
                ErrorMessage = x.ErrorMessage
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<ArchiveExportManifestDto> ValidateRecoveryAsync(Guid checkpointId, CancellationToken cancellationToken = default)
    {
        var checkpoint = await _dbContext.RecoveryCheckpoints.AsNoTracking().FirstAsync(x => x.Id == checkpointId, cancellationToken);
        return new ArchiveExportManifestDto
        {
            Id = checkpoint.Id,
            Module = checkpoint.Module,
            EntityName = checkpoint.EntityName,
            Status = checkpoint.Status,
            Hash = checkpoint.Hash,
            PayloadSizeBytes = checkpoint.PayloadSizeBytes,
            MetadataJson = checkpoint.MetadataJson,
            CreatedAtUtc = checkpoint.CreatedAtUtc,
            ArchivedAtUtc = checkpoint.ArchivedAtUtc,
            DeletedAtUtc = checkpoint.DeletedAtUtc,
            CreatedBy = checkpoint.CreatedBy,
            CorrelationId = checkpoint.CorrelationId
        };
    }

    private async Task<string> GetLatestMigrationAsync(CancellationToken cancellationToken)
    {
        var migrations = await _dbContext.Database.GetAppliedMigrationsAsync(cancellationToken);
        return migrations.LastOrDefault() ?? "none";
    }

    private async Task<object> BuildSchedulerStateAsync(CancellationToken cancellationToken)
        => new
        {
            definitions = await _dbContext.BackgroundJobDefinitions.CountAsync(cancellationToken),
            executions = await _dbContext.BackgroundJobExecutions.CountAsync(cancellationToken),
            logs = await _dbContext.BackgroundJobLogs.CountAsync(cancellationToken)
        };

    private async Task<object> BuildWorkspaceStateAsync(CancellationToken cancellationToken)
        => new
        {
            preferences = await _dbContext.UserWorkspacePreferences.CountAsync(cancellationToken),
            modules = await _dbContext.AppModules.CountAsync(cancellationToken)
        };

    private async Task<object> BuildAuditStateAsync(CancellationToken cancellationToken)
        => new
        {
            auditLogs = await _dbContext.AuditLogs.CountAsync(cancellationToken),
            auditSessions = await _dbContext.AuditSessions.CountAsync(cancellationToken),
            recoverableArchives = await _dbContext.ArchiveBatches.CountAsync(cancellationToken)
        };

    private async Task AuditAsync(string entityName, string entityId, string action, object? payload, CancellationToken cancellationToken)
    {
        if (_auditService is null)
        {
            return;
        }

        await _auditService.LogAsync(entityName, entityId, action, null, payload, correlationId: _correlationService?.CurrentCorrelationId, cancellationToken: cancellationToken);
    }

    private static RecoveryCheckpointDto ToDto(RecoveryCheckpoint checkpoint)
        => new()
        {
            Id = checkpoint.Id,
            CompanyId = checkpoint.CompanyId,
            BranchId = checkpoint.BranchId,
            Module = checkpoint.Module,
            EntityName = checkpoint.EntityName,
            Status = checkpoint.Status,
            MigrationVersion = checkpoint.MigrationVersion,
            SchedulerStateJson = checkpoint.SchedulerStateJson,
            WorkspaceMetadataJson = checkpoint.WorkspaceMetadataJson,
            AuditSessionMetadataJson = checkpoint.AuditSessionMetadataJson,
            MetadataJson = checkpoint.MetadataJson,
            Hash = checkpoint.Hash,
            PayloadSizeBytes = checkpoint.PayloadSizeBytes,
            CreatedAtUtc = checkpoint.CreatedAtUtc,
            ArchivedAtUtc = checkpoint.ArchivedAtUtc,
            DeletedAtUtc = checkpoint.DeletedAtUtc,
            CreatedBy = checkpoint.CreatedBy,
            CorrelationId = checkpoint.CorrelationId
        };
}
