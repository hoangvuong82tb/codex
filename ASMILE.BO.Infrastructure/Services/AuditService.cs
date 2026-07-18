using System.Reflection;
using System.Text.Json;
using ASMILE.BO.Application.Interfaces;
using ASMILE.BO.Core.Entities;
using ASMILE.BO.Core.Enums;
using ASMILE.BO.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ASMILE.BO.Infrastructure.Services;

public sealed class AuditService : IAuditService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly AppDbContext _dbContext;
    private readonly IAuditCorrelationService? _auditCorrelationService;
    private readonly ICurrentUserService? _currentUserService;

    public AuditService(AppDbContext dbContext, IAuditCorrelationService? auditCorrelationService = null, ICurrentUserService? currentUserService = null)
    {
        _dbContext = dbContext;
        _auditCorrelationService = auditCorrelationService;
        _currentUserService = currentUserService;
    }

    public async Task LogAsync(
        string entityName,
        string entityId,
        string action,
        object? oldValues = null,
        object? newValues = null,
        string? userName = null,
        string? ipAddress = null,
        string? correlationId = null,
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var effectiveCorrelationId = _auditCorrelationService?.EnsureCorrelationId(correlationId) ?? correlationId ?? Guid.NewGuid().ToString("N");
        var auditSessionId = await EnsureAuditSessionAsync(effectiveCorrelationId, entityName, action, userName, now, cancellationToken);

        var auditLog = new AuditLog
        {
            CreatedAt = now,
            Username = string.IsNullOrWhiteSpace(userName) ? "system" : userName,
            Action = action,
            Detail = BuildDetail(entityName, entityId, action),
            EntityName = entityName,
            EntityId = entityId,
            OldValuesJson = oldValues is null ? null : AuditSnapshotUtility.ToStableJson(oldValues),
            NewValuesJson = newValues is null ? null : AuditSnapshotUtility.ToStableJson(newValues),
            IpAddress = ipAddress,
            CorrelationId = effectiveCorrelationId
        };

        _dbContext.AuditLogs.Add(auditLog);
        AddAuditCorrelation(effectiveCorrelationId, auditSessionId, entityName, entityId, action, now);
        await AddReplayEntryAsync(effectiveCorrelationId, auditSessionId, entityName, entityId, action, newValues, now, cancellationToken);

        await _dbContext.SaveChangesAsync(cancellationToken);

        if (oldValues is not null || newValues is not null)
        {
            var snapshot = AuditSnapshotUtility.Build(oldValues, newValues);
            _dbContext.AuditEntitySnapshots.Add(new AuditEntitySnapshot
            {
                AuditLogId = auditLog.Id,
                CompanyId = auditLog.CompanyId,
                BranchId = auditLog.BranchId,
                EntityType = entityName,
                EntityId = entityId,
                BeforeJson = snapshot.BeforeJson,
                AfterJson = snapshot.AfterJson,
                DiffJson = snapshot.DiffJson,
                SnapshotHash = snapshot.SnapshotHash,
                CompressedPayload = snapshot.CompressedPayload,
                CompressionType = snapshot.CompressionType,
                SnapshotSize = snapshot.SnapshotSize,
                HashAlgorithm = snapshot.HashAlgorithm
            });

            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<IReadOnlyList<AuditLog>> QueryAsync(
        string? entityName = null,
        string? entityId = null,
        string? action = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.AuditLogs.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(entityName))
        {
            query = query.Where(x => x.EntityName == entityName);
        }

        if (!string.IsNullOrWhiteSpace(entityId))
        {
            query = query.Where(x => x.EntityId == entityId);
        }

        if (!string.IsNullOrWhiteSpace(action))
        {
            query = query.Where(x => x.Action == action);
        }

        if (fromDate.HasValue)
        {
            query = query.Where(x => x.CreatedAt >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(x => x.CreatedAt <= toDate.Value);
        }

        return await query
            .OrderByDescending(x => x.CreatedAt)
            .ThenByDescending(x => x.Id)
            .Take(500)
            .ToListAsync(cancellationToken);
    }

    private static string BuildDetail(string entityName, string entityId, string action)
        => $"{action}: {entityName}/{entityId}";

    private async Task<Guid> EnsureAuditSessionAsync(string correlationId, string entityName, string action, string? userName, DateTime now, CancellationToken cancellationToken)
    {
        var existing = await _dbContext.AuditSessions.FirstOrDefaultAsync(x => x.CorrelationId == correlationId, cancellationToken);
        if (existing is not null)
        {
            if (existing.Status is AuditSessionStatus.Started or AuditSessionStatus.Active)
            {
                existing.Status = AuditSessionStatus.Active;
                existing.LastActivityAtUtc = now;
                existing.DurationMs = Math.Max(0, (long)(now - existing.StartedAtUtc).TotalMilliseconds);
            }

            return existing.Id;
        }

        var currentUserId = _currentUserService?.CurrentUser?.Id;
        var session = new AuditSession
        {
            CorrelationId = correlationId,
            UserId = currentUserId,
            CompanyId = _dbContext.CurrentCompanyId,
            BranchId = _dbContext.CurrentBranchId,
            StartedAtUtc = now,
            LastActivityAtUtc = now,
            Status = AuditSessionStatus.Active,
            Source = $"{entityName}.{action}",
            ClientMachine = Environment.MachineName,
            AppVersion = typeof(AuditService).Assembly.GetName().Version?.ToString(),
            DeviceId = Environment.MachineName,
            Platform = Environment.OSVersion.Platform.ToString(),
            SessionMetadataJson = JsonSerializer.Serialize(new
            {
                userName = string.IsNullOrWhiteSpace(userName) ? "system" : userName,
                machine = Environment.MachineName,
                appVersion = typeof(AuditService).Assembly.GetName().Version?.ToString()
            }, JsonOptions)
        };

        _dbContext.AuditSessions.Add(session);
        return session.Id;
    }

    private void AddAuditCorrelation(string correlationId, Guid auditSessionId, string entityName, string entityId, string action, DateTime now)
    {
        _dbContext.AuditCorrelations.Add(new AuditCorrelation
        {
            CorrelationId = correlationId,
            ParentCorrelationId = _auditCorrelationService?.ParentCorrelationId,
            AuditSessionId = auditSessionId,
            EntityType = entityName,
            EntityId = entityId,
            Module = ResolveModule(entityName, action),
            Action = action,
            CreatedAtUtc = now
        });
    }

    private async Task AddReplayEntryAsync(string correlationId, Guid auditSessionId, string entityName, string entityId, string action, object? metadataSource, DateTime now, CancellationToken cancellationToken)
    {
        var metadataJson = metadataSource is null ? null : AuditSnapshotUtility.ToStableJson(metadataSource);
        var nextSequence = (await _dbContext.AuditReplayEntries.AsNoTracking().Where(x => x.CorrelationId == correlationId).MaxAsync(x => (int?)x.SequenceNo, cancellationToken) ?? 0) + 1;
        _dbContext.AuditReplayEntries.Add(new AuditReplayEntry
        {
            CorrelationId = correlationId,
            ParentCorrelationId = _auditCorrelationService?.ParentCorrelationId,
            AuditSessionId = auditSessionId,
            SequenceNo = nextSequence,
            Route = ResolveRoute(entityName, metadataSource),
            Module = ResolveModule(entityName, action),
            Action = action,
            EntityType = entityName,
            EntityId = entityId,
            MetadataJson = metadataJson,
            TimestampUtc = now
        });
    }

    private static string ResolveModule(string entityName, string action)
    {
        var text = $"{entityName} {action}".ToLowerInvariant();
        if (text.Contains("journal")) return "general-ledger";
        if (text.Contains("report")) return "report-center";
        if (text.Contains("import")) return "import";
        if (text.Contains("scheduler") || text.Contains("background")) return "operations-center";
        if (text.Contains("task") || text.Contains("notification")) return "notification-center";
        if (text.Contains("vat") || text.Contains("tax")) return "tax-financial";
        if (text.Contains("closing")) return "fiscal-periods";
        if (text.Contains("navigation")) return "navigation";
        return entityName.ToLowerInvariant();
    }

    private static string ResolveRoute(string entityName, object? metadataSource)
    {
        var route = AuditSnapshotUtility.TryReadSimpleValue(metadataSource, "Route");
        if (!string.IsNullOrWhiteSpace(route))
        {
            return route;
        }

        var sourceType = AuditSnapshotUtility.TryReadSimpleValue(metadataSource, "SourceType");
        if (!string.IsNullOrWhiteSpace(sourceType))
        {
            return sourceType;
        }

        return entityName;
    }
}
