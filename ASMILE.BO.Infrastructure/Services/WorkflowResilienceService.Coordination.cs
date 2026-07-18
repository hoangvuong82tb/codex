using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ASMILE.BO.Application.DTOs;
using ASMILE.BO.Application.Interfaces;
using ASMILE.BO.Core.Entities;
using ASMILE.BO.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ASMILE.BO.Infrastructure.Services;

public sealed partial class WorkflowResilienceService
{
    public async Task<WorkflowExecutionDto> CancelWorkflowAsync(Guid workflowExecutionId, string reason, CancellationToken cancellationToken = default)
    {
        var execution = await _dbContext.WorkflowExecutions.FirstAsync(x => x.Id == workflowExecutionId, cancellationToken);
        execution.Status = "Cancelled";
        execution.LastHeartbeatUtc = DateTime.UtcNow;
        execution.CompletedAtUtc = DateTime.UtcNow;
        execution.ErrorMessage = reason;
        await ReleaseWorkflowResourceAsync(execution.WorkflowType, execution.WorkflowKey, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await AuditAsync("WorkflowExecution", execution.Id.ToString(), "Cancel", new { reason }, cancellationToken);
        await RecordWorkflowExecutionMetricAsync(execution.Id, execution.WorkflowType, execution.WorkflowKey, "workflow-cancel", 1, "workflow", "Warning", 0, new { reason }, cancellationToken);
        return ToDto(execution);
    }

    public async Task<WorkflowLeaseDto> AcquireLeaseAsync(string workflowType, string workflowKey, TimeSpan leaseDuration, Guid? workflowExecutionId = null, object? metadata = null, CancellationToken cancellationToken = default)
    {
        var normalizedType = Normalize(workflowType, nameof(workflowType));
        var normalizedKey = Normalize(workflowKey, nameof(workflowKey));
        var leaseKey = BuildLeaseKey(normalizedType, normalizedKey);
        var gate = GetResourceGate($"lease:{leaseKey}:{_dbContext.CurrentCompanyId?.ToString("N") ?? "global"}:{_dbContext.CurrentBranchId?.ToString("N") ?? "global"}");
        await gate.WaitAsync(cancellationToken);
        try
        {
            var now = DateTime.UtcNow;
            var existing = await _dbContext.WorkflowLeases.FirstOrDefaultAsync(x =>
                x.CompanyId == _dbContext.CurrentCompanyId &&
                x.BranchId == _dbContext.CurrentBranchId &&
                x.WorkflowType == normalizedType &&
                x.WorkflowKey == normalizedKey &&
                x.LeaseKey == leaseKey &&
                x.Status != "Released" &&
                x.ExpiresAtUtc > now, cancellationToken);

            if (existing is not null)
            {
                existing.Status = "Waiting";
                existing.LastHeartbeatUtc = now;
                existing.ExpiresAtUtc = now.Add(leaseDuration > TimeSpan.Zero ? leaseDuration : TimeSpan.FromMinutes(15));
                existing.MetadataJson = MergeMetadata(existing.MetadataJson, metadata);
                await _dbContext.SaveChangesAsync(cancellationToken);
                return ToDto(existing);
            }

            var lease = new WorkflowLease
            {
                CompanyId = _dbContext.CurrentCompanyId,
                BranchId = _dbContext.CurrentBranchId,
                WorkflowExecutionId = workflowExecutionId,
                CorrelationId = _correlationService?.CurrentCorrelationId ?? Guid.NewGuid().ToString("N"),
                WorkflowType = normalizedType,
                WorkflowKey = normalizedKey,
                LeaseKey = leaseKey,
                Status = "Leased",
                HolderMachineName = Environment.MachineName,
                HolderProcessId = Environment.ProcessId,
                StartedAtUtc = now,
                LastHeartbeatUtc = now,
                ExpiresAtUtc = now.Add(leaseDuration > TimeSpan.Zero ? leaseDuration : TimeSpan.FromMinutes(15)),
                MetadataJson = metadata is null ? null : JsonSerializer.Serialize(metadata, JsonOptions)
            };
            _dbContext.WorkflowLeases.Add(lease);
            await _dbContext.SaveChangesAsync(cancellationToken);
            return ToDto(lease);
        }
        finally
        {
            gate.Release();
        }
    }

    public async Task<WorkflowLeaseDto> HeartbeatLeaseAsync(Guid leaseId, TimeSpan leaseDuration, CancellationToken cancellationToken = default)
    {
        var lease = await _dbContext.WorkflowLeases.FirstAsync(x => x.Id == leaseId, cancellationToken);
        var now = DateTime.UtcNow;
        lease.LastHeartbeatUtc = now;
        lease.ExpiresAtUtc = now.Add(leaseDuration > TimeSpan.Zero ? leaseDuration : TimeSpan.FromMinutes(15));
        lease.Status = "Leased";
        await _dbContext.SaveChangesAsync(cancellationToken);
        return ToDto(lease);
    }

    public async Task ReleaseLeaseAsync(Guid leaseId, CancellationToken cancellationToken = default)
    {
        var lease = await _dbContext.WorkflowLeases.FirstOrDefaultAsync(x => x.Id == leaseId, cancellationToken);
        if (lease is null)
        {
            return;
        }

        lease.Status = "Released";
        lease.LastHeartbeatUtc = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<WorkflowLeaseDto>> GetLeasesAsync(int take = 100, CancellationToken cancellationToken = default)
    {
        return await _dbContext.WorkflowLeases.AsNoTracking()
            .OrderByDescending(x => x.LastHeartbeatUtc)
            .Take(Math.Clamp(take, 1, 500))
            .Select(x => ToDto(x))
            .ToListAsync(cancellationToken);
    }

    public async Task<WorkflowDistributedLockDto> AcquireDistributedLockAsync(string lockScope, string lockKey, TimeSpan leaseDuration, Guid? workflowExecutionId = null, object? metadata = null, CancellationToken cancellationToken = default)
    {
        var normalizedScope = Normalize(lockScope, nameof(lockScope));
        var normalizedKey = Normalize(lockKey, nameof(lockKey));
        var fullKey = BuildDistributedLockKey(normalizedScope, normalizedKey, _dbContext.CurrentCompanyId, _dbContext.CurrentBranchId);
        var gate = GetResourceGate($"lock:{fullKey}");
        await gate.WaitAsync(cancellationToken);
        try
        {
            var now = DateTime.UtcNow;
            var existing = await _dbContext.WorkflowDistributedLocks.FirstOrDefaultAsync(x =>
                x.CompanyId == _dbContext.CurrentCompanyId &&
                x.BranchId == _dbContext.CurrentBranchId &&
                x.LockScope == normalizedScope &&
                x.LockKey == normalizedKey &&
                x.Status != "Released" &&
                x.ExpiresAtUtc > now, cancellationToken);

            if (existing is not null)
            {
                existing.Status = "Waiting";
                existing.LastHeartbeatUtc = now;
                existing.ExpiresAtUtc = now.Add(leaseDuration > TimeSpan.Zero ? leaseDuration : TimeSpan.FromMinutes(15));
                existing.MetadataJson = MergeMetadata(existing.MetadataJson, metadata);
                await _dbContext.SaveChangesAsync(cancellationToken);
                return ToDto(existing);
            }

            var lockEntry = new WorkflowDistributedLock
            {
                CompanyId = _dbContext.CurrentCompanyId,
                BranchId = _dbContext.CurrentBranchId,
                WorkflowExecutionId = workflowExecutionId,
                CorrelationId = _correlationService?.CurrentCorrelationId ?? Guid.NewGuid().ToString("N"),
                WorkflowType = normalizedScope,
                WorkflowKey = normalizedKey,
                LockScope = normalizedScope,
                LockKey = normalizedKey,
                Status = "Locked",
                HolderMachineName = Environment.MachineName,
                HolderProcessId = Environment.ProcessId,
                LockedAtUtc = now,
                LastHeartbeatUtc = now,
                ExpiresAtUtc = now.Add(leaseDuration > TimeSpan.Zero ? leaseDuration : TimeSpan.FromMinutes(15)),
                MetadataJson = metadata is null ? null : JsonSerializer.Serialize(metadata, JsonOptions)
            };
            _dbContext.WorkflowDistributedLocks.Add(lockEntry);
            await _dbContext.SaveChangesAsync(cancellationToken);
            return ToDto(lockEntry);
        }
        finally
        {
            gate.Release();
        }
    }

    public async Task<WorkflowDistributedLockDto> HeartbeatDistributedLockAsync(Guid lockId, TimeSpan leaseDuration, CancellationToken cancellationToken = default)
    {
        var lockEntry = await _dbContext.WorkflowDistributedLocks.FirstAsync(x => x.Id == lockId, cancellationToken);
        var now = DateTime.UtcNow;
        lockEntry.LastHeartbeatUtc = now;
        lockEntry.ExpiresAtUtc = now.Add(leaseDuration > TimeSpan.Zero ? leaseDuration : TimeSpan.FromMinutes(15));
        lockEntry.Status = "Locked";
        await _dbContext.SaveChangesAsync(cancellationToken);
        return ToDto(lockEntry);
    }

    public async Task ReleaseDistributedLockAsync(Guid lockId, CancellationToken cancellationToken = default)
    {
        var lockEntry = await _dbContext.WorkflowDistributedLocks.FirstOrDefaultAsync(x => x.Id == lockId, cancellationToken);
        if (lockEntry is null)
        {
            return;
        }

        lockEntry.Status = "Released";
        lockEntry.LastHeartbeatUtc = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<WorkflowDistributedLockDto>> GetDistributedLocksAsync(int take = 100, bool includeExpired = false, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var query = _dbContext.WorkflowDistributedLocks.AsNoTracking();
        if (!includeExpired)
        {
            query = query.Where(x => x.ExpiresAtUtc > now && x.Status != "Released");
        }

        return await query.OrderByDescending(x => x.LastHeartbeatUtc)
            .Take(Math.Clamp(take, 1, 500))
            .Select(x => ToDto(x))
            .ToListAsync(cancellationToken);
    }

    public async Task<int> ReleaseStaleLocksAsync(DateTime utcNow, CancellationToken cancellationToken = default)
    {
        var staleLocks = await _dbContext.WorkflowDistributedLocks
            .Where(x => x.Status != "Released" && x.ExpiresAtUtc <= utcNow)
            .ToListAsync(cancellationToken);

        foreach (var lockEntry in staleLocks)
        {
            lockEntry.Status = "Released";
            lockEntry.LastHeartbeatUtc = utcNow;
        }

        var staleLeases = await _dbContext.WorkflowLeases
            .Where(x => x.Status != "Released" && x.ExpiresAtUtc <= utcNow)
            .ToListAsync(cancellationToken);

        foreach (var lease in staleLeases)
        {
            lease.Status = "Expired";
            lease.LastHeartbeatUtc = utcNow;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return staleLocks.Count + staleLeases.Count;
    }

    public async Task<WorkflowDeadLetterDto> DeadLetterAsync(Guid? workflowExecutionId, string workflowType, string workflowKey, string reason, object? payload = null, Exception? exception = null, CancellationToken cancellationToken = default)
    {
        var normalizedType = Normalize(workflowType, nameof(workflowType));
        var normalizedKey = Normalize(workflowKey, nameof(workflowKey));
        var execution = workflowExecutionId.HasValue
            ? await _dbContext.WorkflowExecutions.FirstOrDefaultAsync(x => x.Id == workflowExecutionId.Value, cancellationToken)
            : null;

        var deadLetter = new WorkflowDeadLetter
        {
            CompanyId = execution?.CompanyId ?? _dbContext.CurrentCompanyId,
            BranchId = execution?.BranchId ?? _dbContext.CurrentBranchId,
            WorkflowExecutionId = workflowExecutionId,
            CorrelationId = execution?.CorrelationId ?? _correlationService?.CurrentCorrelationId ?? Guid.NewGuid().ToString("N"),
            WorkflowType = normalizedType,
            WorkflowKey = normalizedKey,
            DeadLetterReason = reason,
            Status = "DeadLettered",
            PayloadJson = payload is null ? null : JsonSerializer.Serialize(payload, JsonOptions),
            ExceptionType = exception?.GetType().FullName,
            ErrorMessage = exception?.Message,
            MetadataJson = JsonSerializer.Serialize(new { reason, deadLetteredAtUtc = DateTime.UtcNow }, JsonOptions),
            DeadLetteredAtUtc = DateTime.UtcNow
        };
        _dbContext.WorkflowDeadLetters.Add(deadLetter);

        if (execution is not null)
        {
            execution.Status = "DeadLettered";
            execution.FailedAtUtc = DateTime.UtcNow;
            execution.ErrorMessage = reason;
            execution.ExceptionType = exception?.GetType().FullName;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        await AuditAsync("WorkflowDeadLetter", deadLetter.Id.ToString(), "DeadLetter", new { reason }, cancellationToken);
        await RecordWorkflowExecutionMetricAsync(workflowExecutionId, normalizedType, normalizedKey, "workflow-deadletter", 1, "workflow", "Error", 0, new { reason }, cancellationToken);
        return ToDto(deadLetter);
    }

    public async Task<IReadOnlyList<WorkflowDeadLetterDto>> GetDeadLettersAsync(int take = 100, CancellationToken cancellationToken = default)
    {
        return await _dbContext.WorkflowDeadLetters.AsNoTracking()
            .OrderByDescending(x => x.DeadLetteredAtUtc)
            .Take(Math.Clamp(take, 1, 500))
            .Select(x => ToDto(x))
            .ToListAsync(cancellationToken);
    }

    public async Task<WorkflowRecoveryCheckpointDto> CreateRecoveryCheckpointAsync(Guid? workflowExecutionId, string workflowType, string workflowKey, string checkpointName, object? state = null, object? metadata = null, CancellationToken cancellationToken = default)
    {
        var normalizedType = Normalize(workflowType, nameof(workflowType));
        var normalizedKey = Normalize(workflowKey, nameof(workflowKey));
        var snapshotJson = state is null ? "{}" : JsonSerializer.Serialize(state, JsonOptions);
        var checkpoint = new WorkflowRecoveryCheckpoint
        {
            CompanyId = _dbContext.CurrentCompanyId,
            BranchId = _dbContext.CurrentBranchId,
            WorkflowExecutionId = workflowExecutionId,
            CorrelationId = _correlationService?.CurrentCorrelationId ?? Guid.NewGuid().ToString("N"),
            WorkflowType = normalizedType,
            WorkflowKey = normalizedKey,
            CheckpointName = Normalize(checkpointName, nameof(checkpointName)),
            Status = "Created",
            SnapshotJson = snapshotJson,
            SnapshotHash = ComputeSnapshotHash(snapshotJson),
            MetadataJson = metadata is null ? null : JsonSerializer.Serialize(metadata, JsonOptions),
            CreatedAtUtc = DateTime.UtcNow
        };
        _dbContext.WorkflowRecoveryCheckpoints.Add(checkpoint);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await AuditAsync("WorkflowRecoveryCheckpoint", checkpoint.Id.ToString(), "Create", new { checkpoint.CheckpointName }, cancellationToken);
        return ToDto(checkpoint);
    }

    public async Task<WorkflowExecutionDto> ResumeFromCheckpointAsync(Guid checkpointId, CancellationToken cancellationToken = default)
    {
        var checkpoint = await _dbContext.WorkflowRecoveryCheckpoints.FirstAsync(x => x.Id == checkpointId, cancellationToken);
        checkpoint.Status = "Restored";
        checkpoint.RestoredAtUtc = DateTime.UtcNow;

        WorkflowExecution? execution = null;
        if (checkpoint.WorkflowExecutionId.HasValue)
        {
            execution = await _dbContext.WorkflowExecutions.FirstOrDefaultAsync(x => x.Id == checkpoint.WorkflowExecutionId.Value, cancellationToken);
            if (execution is null)
            {
                execution = new WorkflowExecution
                {
                    CompanyId = checkpoint.CompanyId,
                    BranchId = checkpoint.BranchId,
                    CorrelationId = checkpoint.CorrelationId,
                    WorkflowType = checkpoint.WorkflowType,
                    WorkflowKey = checkpoint.WorkflowKey,
                    Status = "Resumed",
                    StartedAtUtc = DateTime.UtcNow,
                    LastHeartbeatUtc = DateTime.UtcNow,
                    RecoveryStateJson = checkpoint.SnapshotJson,
                    MetadataJson = checkpoint.MetadataJson
                };
                _dbContext.WorkflowExecutions.Add(execution);
            }
            else
            {
                execution.Status = "Resumed";
                execution.RecoveryStateJson = checkpoint.SnapshotJson;
                execution.LastHeartbeatUtc = DateTime.UtcNow;
            }
        }
        else
        {
            execution = new WorkflowExecution
            {
                CompanyId = checkpoint.CompanyId,
                BranchId = checkpoint.BranchId,
                CorrelationId = checkpoint.CorrelationId,
                WorkflowType = checkpoint.WorkflowType,
                WorkflowKey = checkpoint.WorkflowKey,
                Status = "Resumed",
                StartedAtUtc = DateTime.UtcNow,
                LastHeartbeatUtc = DateTime.UtcNow,
                RecoveryStateJson = checkpoint.SnapshotJson,
                MetadataJson = checkpoint.MetadataJson
            };
            _dbContext.WorkflowExecutions.Add(execution);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        await AuditAsync("WorkflowRecoveryCheckpoint", checkpoint.Id.ToString(), "Restore", new { checkpoint.CheckpointName }, cancellationToken);
        return ToDto(execution);
    }

    public async Task<IReadOnlyList<WorkflowRecoveryCheckpointDto>> GetRecoveryCheckpointsAsync(int take = 100, CancellationToken cancellationToken = default)
    {
        return await _dbContext.WorkflowRecoveryCheckpoints.AsNoTracking()
            .OrderByDescending(x => x.CreatedAtUtc)
            .Take(Math.Clamp(take, 1, 500))
            .Select(x => ToDto(x))
            .ToListAsync(cancellationToken);
    }

    public async Task<WorkflowExecutionMetricDto> RecordWorkflowExecutionMetricAsync(Guid? workflowExecutionId, string workflowType, string workflowKey, string metricName, double metricValue, string category, string severity = "Information", long durationMs = 0, object? metadata = null, CancellationToken cancellationToken = default)
    {
        var normalizedType = Normalize(workflowType, nameof(workflowType));
        var normalizedKey = Normalize(workflowKey, nameof(workflowKey));
        var execution = workflowExecutionId.HasValue
            ? await _dbContext.WorkflowExecutions.FirstOrDefaultAsync(x => x.Id == workflowExecutionId.Value, cancellationToken)
            : null;
        var metric = new WorkflowExecutionMetric
        {
            CompanyId = execution?.CompanyId ?? _dbContext.CurrentCompanyId,
            BranchId = execution?.BranchId ?? _dbContext.CurrentBranchId,
            WorkflowExecutionId = workflowExecutionId,
            CorrelationId = execution?.CorrelationId ?? _correlationService?.CurrentCorrelationId ?? Guid.NewGuid().ToString("N"),
            WorkflowType = normalizedType,
            WorkflowKey = normalizedKey,
            MetricName = Normalize(metricName, nameof(metricName)),
            Category = Normalize(category, nameof(category)),
            Severity = string.IsNullOrWhiteSpace(severity) ? "Information" : severity.Trim(),
            MetricValue = metricValue,
            DurationMs = durationMs,
            MetadataJson = metadata is null ? null : JsonSerializer.Serialize(metadata, JsonOptions),
            RecordedAtUtc = DateTime.UtcNow
        };
        _dbContext.WorkflowExecutionMetrics.Add(metric);
        await _dbContext.SaveChangesAsync(cancellationToken);

        if (_telemetryService is not null)
        {
            if (durationMs > 0)
            {
                await _telemetryService.TrackSlowOperationAsync("workflow", metricName, durationMs, severity, null, metric.CorrelationId, null, metadata, cancellationToken);
            }
            else
            {
                await _telemetryService.TrackMetricAsync("workflow", category, metricName, metricValue, null, null, metric.CorrelationId, null, metadata, cancellationToken);
            }
        }

        await AuditAsync("WorkflowExecutionMetric", metric.Id.ToString(), "Record", new { metric.MetricName, metric.MetricValue }, cancellationToken);
        return ToDto(metric);
    }

    public async Task<IReadOnlyList<WorkflowExecutionMetricDto>> GetWorkflowExecutionMetricsAsync(int take = 200, CancellationToken cancellationToken = default)
    {
        return await _dbContext.WorkflowExecutionMetrics.AsNoTracking()
            .OrderByDescending(x => x.RecordedAtUtc)
            .Take(Math.Clamp(take, 1, 1000))
            .Select(x => ToDto(x))
            .ToListAsync(cancellationToken);
    }

    public async Task<WorkflowReplaySessionDto> ReplayWorkflowAsync(Guid workflowExecutionId, CancellationToken cancellationToken = default)
    {
        var execution = await _dbContext.WorkflowExecutions.AsNoTracking().FirstAsync(x => x.Id == workflowExecutionId, cancellationToken);
        var steps = await _dbContext.WorkflowExecutionSteps.AsNoTracking()
            .Where(x => x.WorkflowExecutionId == workflowExecutionId)
            .OrderBy(x => x.StartedAtUtc)
            .ThenBy(x => x.Id)
            .Select(x => ToDto(x))
            .ToListAsync(cancellationToken);
        var metrics = await _dbContext.WorkflowExecutionMetrics.AsNoTracking()
            .Where(x => x.WorkflowExecutionId == workflowExecutionId)
            .OrderBy(x => x.RecordedAtUtc)
            .ThenBy(x => x.Id)
            .Select(x => ToDto(x))
            .ToListAsync(cancellationToken);
        var deadLetters = await _dbContext.WorkflowDeadLetters.AsNoTracking().CountAsync(x => x.WorkflowExecutionId == workflowExecutionId, cancellationToken);

        return new WorkflowReplaySessionDto
        {
            WorkflowExecutionId = execution.Id,
            CorrelationId = execution.CorrelationId,
            WorkflowType = execution.WorkflowType,
            WorkflowKey = execution.WorkflowKey,
            StepCount = steps.Count,
            MetricCount = metrics.Count,
            DeadLetterCount = deadLetters,
            Steps = steps,
            Metrics = metrics
        };
    }

    private async Task ReleaseWorkflowResourceAsync(string workflowType, string workflowKey, CancellationToken cancellationToken)
    {
        var normalizedType = Normalize(workflowType, nameof(workflowType));
        var normalizedKey = Normalize(workflowKey, nameof(workflowKey));
        var now = DateTime.UtcNow;
        var activeLeases = await _dbContext.WorkflowLeases.Where(x => x.WorkflowType == normalizedType && x.WorkflowKey == normalizedKey && x.Status != "Released").ToListAsync(cancellationToken);
        foreach (var lease in activeLeases)
        {
            lease.Status = "Released";
            lease.LastHeartbeatUtc = now;
        }

        var activeLocks = await _dbContext.WorkflowDistributedLocks.Where(x => x.WorkflowType == normalizedType && x.WorkflowKey == normalizedKey && x.Status != "Released").ToListAsync(cancellationToken);
        foreach (var lockEntry in activeLocks)
        {
            lockEntry.Status = "Released";
            lockEntry.LastHeartbeatUtc = now;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private static SemaphoreSlim GetResourceGate(string key)
        => ResourceGates.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));

    private static string ComputeSnapshotHash(string snapshotJson)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(snapshotJson ?? string.Empty)));

    private static string BuildLeaseKey(string workflowType, string workflowKey)
        => $"{workflowType.Trim().ToUpperInvariant()}::{workflowKey.Trim().ToUpperInvariant()}";

    private static string BuildDistributedLockKey(string lockScope, string lockKey, Guid? companyId, Guid? branchId)
        => $"{companyId?.ToString("N") ?? "global"}::{branchId?.ToString("N") ?? "global"}::{lockScope.Trim().ToUpperInvariant()}::{lockKey.Trim().ToUpperInvariant()}";

    private static WorkflowLeaseDto ToDto(WorkflowLease x) => new()
    {
        Id = x.Id,
        WorkflowExecutionId = x.WorkflowExecutionId,
        CorrelationId = x.CorrelationId,
        WorkflowType = x.WorkflowType,
        WorkflowKey = x.WorkflowKey,
        LeaseKey = x.LeaseKey,
        Status = x.Status,
        HolderMachineName = x.HolderMachineName,
        HolderProcessId = x.HolderProcessId,
        StartedAtUtc = x.StartedAtUtc,
        LastHeartbeatUtc = x.LastHeartbeatUtc,
        ExpiresAtUtc = x.ExpiresAtUtc,
        MetadataJson = x.MetadataJson
    };

    private static WorkflowDistributedLockDto ToDto(WorkflowDistributedLock x) => new()
    {
        Id = x.Id,
        WorkflowExecutionId = x.WorkflowExecutionId,
        CorrelationId = x.CorrelationId,
        WorkflowType = x.WorkflowType,
        WorkflowKey = x.WorkflowKey,
        LockScope = x.LockScope,
        LockKey = x.LockKey,
        Status = x.Status,
        HolderMachineName = x.HolderMachineName,
        HolderProcessId = x.HolderProcessId,
        LockedAtUtc = x.LockedAtUtc,
        LastHeartbeatUtc = x.LastHeartbeatUtc,
        ExpiresAtUtc = x.ExpiresAtUtc,
        MetadataJson = x.MetadataJson
    };

    private static WorkflowDeadLetterDto ToDto(WorkflowDeadLetter x) => new()
    {
        Id = x.Id,
        WorkflowExecutionId = x.WorkflowExecutionId,
        CorrelationId = x.CorrelationId,
        WorkflowType = x.WorkflowType,
        WorkflowKey = x.WorkflowKey,
        DeadLetterReason = x.DeadLetterReason,
        Status = x.Status,
        PayloadJson = x.PayloadJson,
        ExceptionType = x.ExceptionType,
        ErrorMessage = x.ErrorMessage,
        MetadataJson = x.MetadataJson,
        DeadLetteredAtUtc = x.DeadLetteredAtUtc,
        RecoveredAtUtc = x.RecoveredAtUtc
    };

    private static WorkflowRecoveryCheckpointDto ToDto(WorkflowRecoveryCheckpoint x) => new()
    {
        Id = x.Id,
        WorkflowExecutionId = x.WorkflowExecutionId,
        ParentCheckpointId = x.ParentCheckpointId,
        CorrelationId = x.CorrelationId,
        WorkflowType = x.WorkflowType,
        WorkflowKey = x.WorkflowKey,
        CheckpointName = x.CheckpointName,
        Status = x.Status,
        SnapshotJson = x.SnapshotJson,
        SnapshotHash = x.SnapshotHash,
        MetadataJson = x.MetadataJson,
        CreatedAtUtc = x.CreatedAtUtc,
        RestoredAtUtc = x.RestoredAtUtc
    };

    private static WorkflowExecutionMetricDto ToDto(WorkflowExecutionMetric x) => new()
    {
        Id = x.Id,
        WorkflowExecutionId = x.WorkflowExecutionId,
        CorrelationId = x.CorrelationId,
        WorkflowType = x.WorkflowType,
        WorkflowKey = x.WorkflowKey,
        MetricName = x.MetricName,
        Category = x.Category,
        Severity = x.Severity,
        MetricValue = x.MetricValue,
        DurationMs = x.DurationMs,
        MetadataJson = x.MetadataJson,
        RecordedAtUtc = x.RecordedAtUtc
    };
}
