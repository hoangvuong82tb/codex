using System.Text.Json;
using ASMILE.BO.Application.DTOs;
using ASMILE.BO.Application.Interfaces;
using ASMILE.BO.Core.Entities;
using ASMILE.BO.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ASMILE.BO.Infrastructure.Services;

public sealed class EnterpriseConsistencyValidatorService : IEnterpriseConsistencyValidatorService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly AppDbContext _dbContext;
    private readonly IWorkflowResilienceService _workflowResilienceService;
    private readonly IRuntimeTelemetryService _telemetryService;
    private readonly IAuditService? _auditService;
    private readonly IAuditCorrelationService? _correlationService;
    private readonly ICurrentUserService? _currentUserService;

    public EnterpriseConsistencyValidatorService(
        AppDbContext dbContext,
        IWorkflowResilienceService workflowResilienceService,
        IRuntimeTelemetryService telemetryService,
        IAuditService? auditService = null,
        IAuditCorrelationService? correlationService = null,
        ICurrentUserService? currentUserService = null)
    {
        _dbContext = dbContext;
        _workflowResilienceService = workflowResilienceService;
        _telemetryService = telemetryService;
        _auditService = auditService;
        _correlationService = correlationService;
        _currentUserService = currentUserService;
    }

    public async Task<IReadOnlyList<EnterpriseConsistencyViolationDto>> ValidateAsync(DateTime utcNow, CancellationToken cancellationToken = default)
    {
        var results = new List<EnterpriseConsistencyViolation>();
        results.AddRange(await ValidateJournalAsync(utcNow, cancellationToken));
        results.AddRange(await ValidateVatAsync(utcNow, cancellationToken));
        results.AddRange(await ValidateReconciliationAsync(utcNow, cancellationToken));
        results.AddRange(await ValidateInventoryAsync(utcNow, cancellationToken));
        results.AddRange(await ValidateAssetAsync(utcNow, cancellationToken));
        results.AddRange(await ValidateWorkflowAsync(utcNow, cancellationToken));
        results.AddRange(await ValidateArchiveAsync(utcNow, cancellationToken));
        results.AddRange(await ValidateTelemetryAsync(utcNow, cancellationToken));

        if (results.Count > 0)
        {
            _dbContext.Set<EnterpriseConsistencyViolation>().AddRange(results);
            await _dbContext.SaveChangesAsync(cancellationToken);
            await AuditAsync("EnterpriseConsistencyViolation", "validate", "Validate", new { count = results.Count }, cancellationToken);
            await _telemetryService.TrackMetricAsync("consistency", "validator", "violationCount", results.Count, "count", "Consistency validation completed.", _correlationService?.CurrentCorrelationId, null, new { results.Count }, cancellationToken);
        }

        return results.Select(ToDto).ToArray();
    }

    public async Task<IReadOnlyList<EnterpriseConsistencyViolationDto>> GetViolationsAsync(int take = 200, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Set<EnterpriseConsistencyViolation>().AsNoTracking()
            .OrderByDescending(x => x.RecordedAtUtc)
            .Take(Math.Clamp(take, 1, 1000))
            .Select(x => ToDto(x))
            .ToListAsync(cancellationToken);
    }

    public async Task<int> AutoRecoverAsync(DateTime utcNow, CancellationToken cancellationToken = default)
    {
        var staleLocks = await _workflowResilienceService.ReleaseStaleLocksAsync(utcNow, cancellationToken);
        var staleWorkflows = await _workflowResilienceService.CleanupStaleWorkflowAsync(utcNow, TimeSpan.FromMinutes(30), cancellationToken);
        if (staleLocks > 0 || staleWorkflows > 0)
        {
            await AuditAsync("EnterpriseConsistencyViolation", "auto-recover", "AutoRecover", new { staleLocks, staleWorkflows }, cancellationToken);
        }

        return staleLocks + staleWorkflows;
    }

    private async Task<IReadOnlyList<EnterpriseConsistencyViolation>> ValidateJournalAsync(DateTime utcNow, CancellationToken cancellationToken)
    {
        var rows = await _dbContext.JournalHeaders.AsNoTracking()
            .Where(x => x.TotalDebit != x.TotalCredit || x.Difference != 0)
            .Select(x => new EnterpriseConsistencyViolation
            {
                CompanyId = x.CompanyId,
                BranchId = x.BranchId,
                Category = "JournalImbalance",
                Severity = "Error",
                Module = "journal",
                EntityType = nameof(JournalHeader),
                EntityId = x.Id.ToString(),
                Recommendation = "Kiểm tra bút toán và cân bằng lại Nợ/Có.",
                CanAutoRecover = false,
                MetadataJson = JsonSerializer.Serialize(new { x.TotalDebit, x.TotalCredit, x.Difference }, JsonOptions),
                RecordedAtUtc = utcNow
            })
            .ToListAsync(cancellationToken);

        var orphanLines = await (
            from line in _dbContext.JournalLines.AsNoTracking()
            join header in _dbContext.JournalHeaders.AsNoTracking() on line.JournalHeaderId equals header.Id into headers
            from header in headers.DefaultIfEmpty()
            where header == null
            select new EnterpriseConsistencyViolation
            {
                CompanyId = line.CompanyId,
                BranchId = line.BranchId,
                Category = "OrphanJournalLine",
                Severity = "Error",
                Module = "journal",
                EntityType = nameof(JournalLine),
                EntityId = line.Id.ToString(),
                Recommendation = "Xác minh dòng sổ không còn bút toán cha.",
                CanAutoRecover = false,
                MetadataJson = JsonSerializer.Serialize(new { line.JournalHeaderId, line.LineNo }, JsonOptions),
                RecordedAtUtc = utcNow
            }).ToListAsync(cancellationToken);

        rows.AddRange(orphanLines);
        return rows;
    }

    private async Task<IReadOnlyList<EnterpriseConsistencyViolation>> ValidateVatAsync(DateTime utcNow, CancellationToken cancellationToken)
    {
        var orphan = await (
            from vat in _dbContext.VatTransactions.AsNoTracking()
            join header in _dbContext.JournalHeaders.AsNoTracking() on vat.JournalHeaderId equals header.Id into headers
            from header in headers.DefaultIfEmpty()
            where vat.JournalHeaderId != null && header == null
            select new EnterpriseConsistencyViolation
            {
                CompanyId = vat.CompanyId,
                BranchId = vat.BranchId,
                Category = "OrphanVatTransaction",
                Severity = "Error",
                Module = "vat",
                EntityType = nameof(VatTransaction),
                EntityId = vat.Id.ToString(),
                Recommendation = "Kiểm tra giao dịch VAT tham chiếu sai bút toán.",
                CanAutoRecover = false,
                MetadataJson = JsonSerializer.Serialize(new { vat.JournalHeaderId, vat.JournalLineId }, JsonOptions),
                RecordedAtUtc = utcNow
            }).ToListAsync(cancellationToken);
        return orphan;
    }

    private async Task<IReadOnlyList<EnterpriseConsistencyViolation>> ValidateReconciliationAsync(DateTime utcNow, CancellationToken cancellationToken)
    {
        var rows = await _dbContext.DebtReconciliations.AsNoTracking()
            .Where(x => x.TotalDebit != x.TotalCredit || x.Balance != x.TotalDebit - x.TotalCredit)
            .Select(x => new EnterpriseConsistencyViolation
            {
                CompanyId = x.CompanyId,
                BranchId = x.BranchId,
                Category = "OrphanReconciliation",
                Severity = "Warning",
                Module = "debt",
                EntityType = nameof(DebtReconciliation),
                EntityId = x.Id.ToString(),
                Recommendation = "Kiểm tra đối chiếu công nợ và các dòng tham chiếu.",
                CanAutoRecover = false,
                MetadataJson = JsonSerializer.Serialize(new { x.TotalDebit, x.TotalCredit, x.Balance }, JsonOptions),
                RecordedAtUtc = utcNow
            })
            .ToListAsync(cancellationToken);
        return rows;
    }

    private async Task<IReadOnlyList<EnterpriseConsistencyViolation>> ValidateInventoryAsync(DateTime utcNow, CancellationToken cancellationToken)
    {
        var rows = await _dbContext.InventoryCostTransactions.AsNoTracking()
            .Where(x => x.Quantity < 0 || x.TotalCost < 0)
            .Select(x => new EnterpriseConsistencyViolation
            {
                CompanyId = x.CompanyId,
                BranchId = x.BranchId,
                Category = "InventoryNegativeInconsistency",
                Severity = "Error",
                Module = "inventory",
                EntityType = nameof(InventoryCostTransaction),
                EntityId = x.Id.ToString(),
                Recommendation = "Kiểm tra tồn âm hoặc giao dịch giá vốn lỗi.",
                CanAutoRecover = false,
                MetadataJson = JsonSerializer.Serialize(new { x.Quantity, x.TotalCost }, JsonOptions),
                RecordedAtUtc = utcNow
            })
            .ToListAsync(cancellationToken);
        return rows;
    }

    private async Task<IReadOnlyList<EnterpriseConsistencyViolation>> ValidateAssetAsync(DateTime utcNow, CancellationToken cancellationToken)
    {
        var depreciation = await _dbContext.FixedAssetDepreciations.AsNoTracking()
            .Where(x => x.Status == "Posted" && x.JournalHeaderId == null)
            .Select(x => new EnterpriseConsistencyViolation
            {
                CompanyId = x.CompanyId,
                BranchId = x.BranchId,
                Category = "DepreciationMismatch",
                Severity = "Warning",
                Module = "asset",
                EntityType = nameof(FixedAssetDepreciation),
                EntityId = x.Id.ToString(),
                Recommendation = "Kiểm tra dòng khấu hao đã ghi sổ nhưng thiếu bút toán.",
                CanAutoRecover = false,
                MetadataJson = JsonSerializer.Serialize(new { x.FixedAssetId, x.DepreciationPeriod }, JsonOptions),
                RecordedAtUtc = utcNow
            })
            .ToListAsync(cancellationToken);

        var allocation = await _dbContext.ToolAllocationSchedules.AsNoTracking()
            .Where(x => x.Status == "Posted" && x.JournalHeaderId == null)
            .Select(x => new EnterpriseConsistencyViolation
            {
                CompanyId = x.CompanyId,
                BranchId = x.BranchId,
                Category = "AllocationMismatch",
                Severity = "Warning",
                Module = "asset",
                EntityType = nameof(ToolAllocationSchedule),
                EntityId = x.Id.ToString(),
                Recommendation = "Kiểm tra dòng phân bổ CCDC đã ghi sổ nhưng thiếu bút toán.",
                CanAutoRecover = false,
                MetadataJson = JsonSerializer.Serialize(new { x.ToolAssetId, x.AllocationPeriod }, JsonOptions),
                RecordedAtUtc = utcNow
            })
            .ToListAsync(cancellationToken);

        depreciation.AddRange(allocation);
        return depreciation;
    }

    private async Task<IReadOnlyList<EnterpriseConsistencyViolation>> ValidateWorkflowAsync(DateTime utcNow, CancellationToken cancellationToken)
    {
        var staleWorkflows = await _dbContext.WorkflowExecutions.AsNoTracking()
            .Where(x => (x.Status == "Started" || x.Status == "Running" || x.Status == "Retrying") && x.LastHeartbeatUtc <= utcNow.AddMinutes(-15))
            .Select(x => new EnterpriseConsistencyViolation
            {
                CompanyId = x.CompanyId,
                BranchId = x.BranchId,
                Category = "WorkflowStuckState",
                Severity = "Error",
                Module = "workflow",
                EntityType = nameof(WorkflowExecution),
                EntityId = x.Id.ToString(),
                Recommendation = "Khôi phục workflow treo hoặc đánh dấu dead-letter nếu không thể tiếp tục.",
                CanAutoRecover = true,
                MetadataJson = JsonSerializer.Serialize(new { x.WorkflowType, x.WorkflowKey, x.Status }, JsonOptions),
                RecordedAtUtc = utcNow
            })
            .ToListAsync(cancellationToken);

        var staleLocks = await _dbContext.WorkflowDistributedLocks.AsNoTracking()
            .Where(x => x.Status == "Locked" && x.ExpiresAtUtc <= utcNow)
            .Select(x => new EnterpriseConsistencyViolation
            {
                CompanyId = x.CompanyId,
                BranchId = x.BranchId,
                Category = "StaleDistributedLock",
                Severity = "Warning",
                Module = "workflow",
                EntityType = nameof(WorkflowDistributedLock),
                EntityId = x.Id.ToString(),
                Recommendation = "Giải phóng lock treo an toàn.",
                CanAutoRecover = true,
                MetadataJson = JsonSerializer.Serialize(new { x.LockScope, x.LockKey }, JsonOptions),
                RecordedAtUtc = utcNow
            })
            .ToListAsync(cancellationToken);

        staleWorkflows.AddRange(staleLocks);
        return staleWorkflows;
    }

    private async Task<IReadOnlyList<EnterpriseConsistencyViolation>> ValidateArchiveAsync(DateTime utcNow, CancellationToken cancellationToken)
    {
        var rows = await _dbContext.ArchiveBatches.AsNoTracking()
            .Where(x => x.Hash == null || x.Status == "Failed")
            .Select(x => new EnterpriseConsistencyViolation
            {
                CompanyId = x.CompanyId,
                BranchId = x.BranchId,
                Category = "ArchiveIntegrityMismatch",
                Severity = "Warning",
                Module = "archive",
                EntityType = nameof(ArchiveBatch),
                EntityId = x.Id.ToString(),
                Recommendation = "Kiểm tra hash và payload archive.",
                CanAutoRecover = false,
                MetadataJson = JsonSerializer.Serialize(new { x.Status, x.ItemCount }, JsonOptions),
                RecordedAtUtc = utcNow
            })
            .ToListAsync(cancellationToken);
        return rows;
    }

    private async Task<IReadOnlyList<EnterpriseConsistencyViolation>> ValidateTelemetryAsync(DateTime utcNow, CancellationToken cancellationToken)
    {
        var rows = await _dbContext.TelemetryEvents.AsNoTracking()
            .Where(x => string.IsNullOrWhiteSpace(x.SearchText) || string.IsNullOrWhiteSpace(x.Module))
            .Select(x => new EnterpriseConsistencyViolation
            {
                CompanyId = x.CompanyId,
                BranchId = x.BranchId,
                Category = "TelemetryCorruption",
                Severity = "Warning",
                Module = "telemetry",
                EntityType = nameof(TelemetryEvent),
                EntityId = x.Id.ToString(),
                Recommendation = "Kiểm tra dữ liệu telemetry bị thiếu chỉ mục tìm kiếm.",
                CanAutoRecover = false,
                MetadataJson = JsonSerializer.Serialize(new { x.EventType, x.EventName }, JsonOptions),
                RecordedAtUtc = utcNow
            })
            .ToListAsync(cancellationToken);
        return rows;
    }

    private async Task AuditAsync(string entityType, string entityId, string action, object? payload, CancellationToken cancellationToken)
    {
        if (_auditService is null)
        {
            return;
        }

        await _auditService.LogAsync(entityType, entityId, action, null, payload, _currentUserService?.CurrentUser?.UserName ?? "system", correlationId: _correlationService?.CurrentCorrelationId, cancellationToken: cancellationToken);
    }

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

    private static EnterpriseConsistencyViolationDto ToDto(EnterpriseConsistencyViolation x) => new()
    {
        Id = x.Id,
        CompanyId = x.CompanyId,
        BranchId = x.BranchId,
        Category = x.Category,
        Severity = x.Severity,
        Module = x.Module,
        EntityType = x.EntityType,
        EntityId = x.EntityId,
        Recommendation = x.Recommendation,
        CanAutoRecover = x.CanAutoRecover,
        MetadataJson = x.MetadataJson,
        RecordedAtUtc = x.RecordedAtUtc
    };

    private static string BuildLeaseKey(string workflowType, string workflowKey)
        => $"{workflowType}:{workflowKey}".ToUpperInvariant();

    private static string BuildDistributedLockKey(string lockScope, string lockKey, Guid? companyId, Guid? branchId)
        => $"{companyId?.ToString("N") ?? "global"}:{branchId?.ToString("N") ?? "global"}:{lockScope}:{lockKey}".ToUpperInvariant();
}
