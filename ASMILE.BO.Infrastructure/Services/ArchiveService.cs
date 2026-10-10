using System.IO.Compression;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using ASMILE.BO.Application.DTOs;
using ASMILE.BO.Application.Interfaces;
using ASMILE.BO.Core.Entities;
using ASMILE.BO.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ASMILE.BO.Infrastructure.Services;

public sealed class ArchiveService : IArchiveService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        ReferenceHandler = ReferenceHandler.IgnoreCycles
    };
    private const int CompressionThresholdBytes = 8 * 1024;

    private readonly AppDbContext _dbContext;
    private readonly IRuntimeTelemetryService? _telemetryService;
    private readonly IAuditService? _auditService;
    private readonly IAuditCorrelationService? _correlationService;
    private readonly ICurrentUserService? _currentUserService;

    public ArchiveService(AppDbContext dbContext, IAuditService? auditService = null, IAuditCorrelationService? correlationService = null, ICurrentUserService? currentUserService = null, IRuntimeTelemetryService? telemetryService = null)
    {
        _dbContext = dbContext;
        _telemetryService = telemetryService;
        _auditService = auditService;
        _correlationService = correlationService;
        _currentUserService = currentUserService;
    }

    public async Task<IReadOnlyList<DataRetentionPolicyDto>> GetRetentionPoliciesAsync(CancellationToken cancellationToken = default)
    {
        await EnsureDefaultPoliciesAsync(cancellationToken);
        var policies = await _dbContext.DataRetentionPolicies.AsNoTracking()
            .OrderBy(x => x.Module)
            .ThenBy(x => x.EntityName)
            .ToListAsync(cancellationToken);
        return policies.Select(ToPolicyDto).ToArray();
    }

    public async Task<DataRetentionPolicyDto> SaveRetentionPolicyAsync(DataRetentionPolicyDto policy, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(policy);
        if (policy.Id == Guid.Empty)
        {
            throw new ArgumentException("A persisted retention policy is required.", nameof(policy));
        }

        if (policy.RetentionDays is < 1 or > 36500
            || policy.ArchiveAfterDays is < 1 or > 36500
            || policy.DeleteAfterDays is < 1 or > 36500
            || policy.DeleteAfterDays < policy.ArchiveAfterDays)
        {
            throw new ArgumentOutOfRangeException(nameof(policy), "Retention values must be between 1 and 36500 days, and deletion cannot precede archival.");
        }

        var entity = await _dbContext.DataRetentionPolicies
            .FirstOrDefaultAsync(x => x.Id == policy.Id, cancellationToken)
            ?? throw new InvalidOperationException("Retention policy no longer exists. Refresh the list and try again.");
        var before = new
        {
            entity.RetentionDays,
            entity.ArchiveAfterDays,
            entity.DeleteAfterDays,
            entity.CompressArchivedPayload,
            entity.IsEnabled
        };
        entity.RetentionDays = policy.RetentionDays;
        entity.ArchiveAfterDays = policy.ArchiveAfterDays;
        entity.DeleteAfterDays = policy.DeleteAfterDays;
        entity.CompressArchivedPayload = policy.CompressArchivedPayload;
        entity.IsEnabled = policy.IsEnabled;
        await _dbContext.SaveChangesAsync(cancellationToken);

        if (_auditService is not null)
        {
            await _auditService.LogAsync(
                "DataRetentionPolicy",
                entity.Id.ToString(),
                "Update",
                before,
                new { entity.RetentionDays, entity.ArchiveAfterDays, entity.DeleteAfterDays, entity.CompressArchivedPayload, entity.IsEnabled },
                correlationId: _correlationService?.CurrentCorrelationId,
                cancellationToken: cancellationToken);
        }

        return ToPolicyDto(entity);
    }

    public async Task<IReadOnlyList<RetentionPreviewDto>> PreviewRetentionAsync(CancellationToken cancellationToken = default)
    {
        await EnsureDefaultPoliciesAsync(cancellationToken);
        var now = DateTime.UtcNow;
        var policies = await _dbContext.DataRetentionPolicies.AsNoTracking().Where(x => x.IsEnabled).ToListAsync(cancellationToken);
        var previews = new List<RetentionPreviewDto>();
        foreach (var policy in policies)
        {
            var expiredCount = await CountExpiredAsync(policy, now, cancellationToken);
            previews.Add(new RetentionPreviewDto
            {
                Module = policy.Module,
                EntityName = policy.EntityName,
                ExpiredCount = expiredCount,
                EstimatedPayloadSizeBytes = expiredCount * 1024L,
                PolicyCount = 1
            });
        }

        return previews;
    }

    public async Task<ArchiveBatchDto> RunArchiveAsync(string? module = null, bool compressArchivedPayload = true, IProgress<int>? progress = null, CancellationToken cancellationToken = default)
    {
        await EnsureDefaultPoliciesAsync(cancellationToken);
        var now = DateTime.UtcNow;
        var effectiveModule = string.IsNullOrWhiteSpace(module) ? null : module.Trim();
        var policies = await _dbContext.DataRetentionPolicies.AsNoTracking()
            .Where(x => x.IsEnabled && (effectiveModule == null || x.Module == effectiveModule))
            .OrderBy(x => x.Module)
            .ThenBy(x => x.EntityName)
            .ToListAsync(cancellationToken);

        var batch = new ArchiveBatch
        {
            Module = effectiveModule ?? "all",
            EntityName = string.IsNullOrWhiteSpace(effectiveModule) ? "all" : effectiveModule,
            Status = "Running",
            CreatedAtUtc = now,
            ArchivedAtUtc = now,
            CreatedBy = _currentUserService?.CurrentUser?.UserName,
            CorrelationId = _correlationService?.CurrentCorrelationId,
            MetadataJson = JsonSerializer.Serialize(new { compressArchivedPayload, now }, JsonOptions)
        };
        _dbContext.ArchiveBatches.Add(batch);

        var itemCount = 0;
        foreach (var policy in policies)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var items = await ArchivePolicyAsync(batch, policy, compressArchivedPayload && policy.CompressArchivedPayload, cancellationToken);
            itemCount += items;
            progress?.Report(itemCount);
        }

        batch.ItemCount = itemCount;
        batch.PayloadSizeBytes = batch.Items.Sum(x => x.PayloadSizeBytes);
        batch.Hash = ComputeHash(batch.Items.Select(x => x.Hash).Where(x => !string.IsNullOrWhiteSpace(x)));
        batch.Status = "Completed";
        await _dbContext.SaveChangesAsync(cancellationToken);

        await AuditArchiveAsync("RunArchive", batch, cancellationToken);

        return ToDto(batch);
    }

    public async Task<ArchiveBatchDto> RestoreArchiveAsync(Guid archiveBatchId, CancellationToken cancellationToken = default)
    {
        var batch = await _dbContext.ArchiveBatches.Include(x => x.Items).FirstAsync(x => x.Id == archiveBatchId, cancellationToken);
        foreach (var item in batch.Items)
        {
            await RestoreItemAsync(item, cancellationToken);
            item.Status = "Restored";
            item.DeletedAtUtc = null;
        }

        batch.Status = "Restored";
        batch.ArchivedAtUtc = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
        await AuditArchiveAsync("RestoreArchive", batch, cancellationToken);
        return ToDto(batch);
    }

    public async Task<int> PurgeExpiredAsync(CancellationToken cancellationToken = default)
    {
        await EnsureDefaultPoliciesAsync(cancellationToken);
        var now = DateTime.UtcNow;
        var policies = await _dbContext.DataRetentionPolicies.AsNoTracking().Where(x => x.IsEnabled).ToListAsync(cancellationToken);
        var batches = await _dbContext.ArchiveBatches.Include(x => x.Items).ToListAsync(cancellationToken);
        var purged = 0;

        foreach (var batch in batches)
        {
            var policy = policies.FirstOrDefault(x => x.Module == batch.Module && x.EntityName == batch.EntityName)
                ?? policies.FirstOrDefault(x => x.Module == batch.Module);
            if (policy is null)
            {
                continue;
            }

            if (batch.DeletedAtUtc.HasValue)
            {
                continue;
            }

            if (batch.ArchivedAtUtc.HasValue && batch.ArchivedAtUtc.Value.AddDays(policy.DeleteAfterDays) <= now)
            {
                batch.Status = "Purged";
                batch.DeletedAtUtc = now;
                foreach (var item in batch.Items)
                {
                    item.Status = "Purged";
                    item.DeletedAtUtc = now;
                }

                purged++;
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        if (purged > 0)
        {
            await AuditArchiveAsync("PurgeExpired", null, cancellationToken);
        }

        return purged;
    }

    public async Task<IReadOnlyList<ArchiveBatchDto>> GetArchiveHistoryAsync(int take = 100, CancellationToken cancellationToken = default)
    {
        return await _dbContext.ArchiveBatches.AsNoTracking()
            .OrderByDescending(x => x.CreatedAtUtc)
            .Take(Math.Clamp(take, 1, 500))
            .Select(x => new ArchiveBatchDto
            {
                Id = x.Id,
                CompanyId = x.CompanyId,
                BranchId = x.BranchId,
                Module = x.Module,
                EntityName = x.EntityName,
                Status = x.Status,
                ItemCount = x.ItemCount,
                PayloadSizeBytes = x.PayloadSizeBytes,
                Hash = x.Hash,
                MetadataJson = x.MetadataJson,
                CreatedAtUtc = x.CreatedAtUtc,
                ArchivedAtUtc = x.ArchivedAtUtc,
                DeletedAtUtc = x.DeletedAtUtc,
                CreatedBy = x.CreatedBy,
                CorrelationId = x.CorrelationId
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ArchiveIntegrityResultDto>> ValidateArchiveIntegrityAsync(Guid? archiveBatchId = null, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.ArchiveBatches.Include(x => x.Items).AsNoTracking().AsQueryable();
        if (archiveBatchId.HasValue)
        {
            query = query.Where(x => x.Id == archiveBatchId.Value);
        }

        var batches = await query.ToListAsync(cancellationToken);
        return batches.Select(batch =>
        {
            var itemHashes = new List<string>();
            var itemsValid = true;
            foreach (var item in batch.Items)
            {
                var payloadBytes = item.CompressedPayload is { Length: > 0 }
                    ? Decompress(item.CompressedPayload)
                    : Encoding.UTF8.GetBytes(item.PayloadJson ?? string.Empty);
                var itemHash = ComputeHash(payloadBytes);
                itemHashes.Add(itemHash);
                itemsValid &= string.Equals(itemHash, item.Hash, StringComparison.OrdinalIgnoreCase);
            }

            var computed = ComputeHash(itemHashes);
            var isValid = itemsValid && string.Equals(computed, batch.Hash, StringComparison.OrdinalIgnoreCase);
            return new ArchiveIntegrityResultDto
            {
                ArchiveBatchId = batch.Id,
                IsValid = isValid,
                ErrorMessage = isValid ? null : "Archive hash mismatch.",
                Hash = batch.Hash,
                ItemCount = batch.Items.Count
            };
        }).ToArray();
    }

    private async Task<int> ArchivePolicyAsync(ArchiveBatch batch, DataRetentionPolicy policy, bool compressArchivedPayload, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var archived = 0;
        switch (policy.EntityName)
        {
            case nameof(SearchIndexEntry):
                archived += await ArchiveSearchEntriesAsync(batch, policy, compressArchivedPayload, cancellationToken);
                break;
            case nameof(RecentSearch):
                archived += await ArchiveRecentSearchesAsync(batch, policy, compressArchivedPayload, cancellationToken);
                break;
            case nameof(AuditSession):
                archived += await ArchiveAuditSessionsAsync(batch, policy, compressArchivedPayload, cancellationToken);
                break;
            case nameof(BackgroundJobExecution):
                archived += await ArchiveBackgroundExecutionsAsync(batch, policy, compressArchivedPayload, cancellationToken);
                break;
            case nameof(ReportExecution):
            case nameof(ImportSession):
            case nameof(UserNotification):
                archived += await ArchiveHistoryMetadataAsync(batch, policy, compressArchivedPayload, cancellationToken);
                break;
            default:
                archived += await ArchiveHistoryMetadataAsync(batch, policy, compressArchivedPayload, cancellationToken);
                break;
        }

        if (archived > 0 && _auditService is not null)
        {
            await _auditService.LogAsync("ArchiveBatch", batch.Id.ToString(), "Archive", null, new { policy.Module, policy.EntityName, archived, now }, correlationId: batch.CorrelationId, cancellationToken: cancellationToken);
        }

        return archived;
    }

    private async Task<int> ArchiveSearchEntriesAsync(ArchiveBatch batch, DataRetentionPolicy policy, bool compressArchivedPayload, CancellationToken cancellationToken)
    {
        var cutoff = DateTime.UtcNow.AddDays(-Math.Max(0, policy.ArchiveAfterDays));
        var rows = await _dbContext.SearchIndexEntries.Where(x => !x.IsArchived && x.IndexedAt <= cutoff).ToListAsync(cancellationToken);
        return await ArchiveRowsAsync(batch, policy, compressArchivedPayload, rows, row =>
        {
            row.IsArchived = true;
            row.ArchivedAtUtc = DateTime.UtcNow;
        }, cancellationToken);
    }

    private async Task<int> ArchiveRecentSearchesAsync(ArchiveBatch batch, DataRetentionPolicy policy, bool compressArchivedPayload, CancellationToken cancellationToken)
    {
        var cutoff = DateTime.UtcNow.AddDays(-Math.Max(0, policy.ArchiveAfterDays));
        var rows = await _dbContext.RecentSearches.Where(x => !x.IsArchived && x.SearchedAt <= cutoff).ToListAsync(cancellationToken);
        return await ArchiveRowsAsync(batch, policy, compressArchivedPayload, rows, row =>
        {
            row.IsArchived = true;
            row.ArchivedAtUtc = DateTime.UtcNow;
        }, cancellationToken);
    }

    private async Task<int> ArchiveAuditSessionsAsync(ArchiveBatch batch, DataRetentionPolicy policy, bool compressArchivedPayload, CancellationToken cancellationToken)
    {
        var cutoff = DateTime.UtcNow.AddDays(-Math.Max(0, policy.ArchiveAfterDays));
        var rows = await _dbContext.AuditSessions.Where(x => x.StartedAtUtc <= cutoff).ToListAsync(cancellationToken);
        return await ArchiveRowsAsync(batch, policy, compressArchivedPayload, rows, _ => { }, cancellationToken);
    }

    private async Task<int> ArchiveBackgroundExecutionsAsync(ArchiveBatch batch, DataRetentionPolicy policy, bool compressArchivedPayload, CancellationToken cancellationToken)
    {
        var cutoff = DateTime.UtcNow.AddDays(-Math.Max(0, policy.ArchiveAfterDays));
        var rows = await _dbContext.BackgroundJobExecutions.Where(x => (x.CompletedAtUtc ?? x.StartedAtUtc ?? x.QueuedAtUtc) <= cutoff).ToListAsync(cancellationToken);
        return await ArchiveRowsAsync(batch, policy, compressArchivedPayload, rows, _ => { }, cancellationToken);
    }

    private async Task<int> ArchiveHistoryMetadataAsync<T>(ArchiveBatch batch, DataRetentionPolicy policy, bool compressArchivedPayload, List<T> rows, Action<T> onArchived, CancellationToken cancellationToken)
    {
        return await ArchiveRowsAsync(batch, policy, compressArchivedPayload, rows, onArchived, cancellationToken);
    }

    private async Task<int> ArchiveHistoryMetadataAsync(ArchiveBatch batch, DataRetentionPolicy policy, bool compressArchivedPayload, CancellationToken cancellationToken)
    {
        var expiredCount = await CountExpiredAsync(policy, DateTime.UtcNow, cancellationToken);
        if (expiredCount == 0)
        {
            return 0;
        }

        var payload = JsonSerializer.Serialize(new
        {
            policy.Module,
            policy.EntityName,
            expiredCount,
            capturedAtUtc = DateTime.UtcNow
        }, JsonOptions);
        var bytes = Encoding.UTF8.GetBytes(payload);
        var compressed = compressArchivedPayload && bytes.Length > CompressionThresholdBytes ? Compress(bytes) : null;
        batch.Items.Add(new ArchiveBatchItem
        {
            ArchiveBatch = batch,
            Module = policy.Module,
            EntityName = policy.EntityName,
            EntityId = $"{policy.Module}:{policy.EntityName}:{DateTime.UtcNow:yyyyMMddHHmmss}",
            Status = "Archived",
            PayloadSizeBytes = bytes.Length,
            Hash = ComputeHash(bytes),
            PayloadJson = compressed is null ? payload : null,
            CompressedPayload = compressed,
            CompressionType = compressed is null ? "None" : "GZip",
            MetadataJson = JsonSerializer.Serialize(new { metadataOnly = true, expiredCount }, JsonOptions),
            ArchivedAtUtc = DateTime.UtcNow,
            CreatedBy = _currentUserService?.CurrentUser?.UserName,
            CorrelationId = _correlationService?.CurrentCorrelationId
        });
        await _dbContext.SaveChangesAsync(cancellationToken);
        return 1;
    }

    private async Task<int> ArchiveRowsAsync<T>(ArchiveBatch batch, DataRetentionPolicy policy, bool compressArchivedPayload, List<T> rows, Action<T> onArchived, CancellationToken cancellationToken)
    {
        var count = 0;
        foreach (var row in rows)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var payload = JsonSerializer.Serialize(row, JsonOptions);
            onArchived(row);
            var bytes = Encoding.UTF8.GetBytes(payload);
            var compressed = compressArchivedPayload && bytes.Length > CompressionThresholdBytes ? Compress(bytes) : null;
            var item = new ArchiveBatchItem
            {
                ArchiveBatch = batch,
                CompanyId = GetCompanyId(row),
                BranchId = GetBranchId(row),
                Module = policy.Module,
                EntityName = policy.EntityName,
                EntityId = GetEntityId(row),
                Status = "Archived",
                PayloadSizeBytes = bytes.Length,
                Hash = ComputeHash(bytes),
                MetadataJson = JsonSerializer.Serialize(new { policy.RetentionDays, policy.ArchiveAfterDays, policy.DeleteAfterDays }, JsonOptions),
                PayloadJson = compressed is null ? payload : null,
                CompressedPayload = compressed,
                CompressionType = compressed is null ? "None" : "GZip",
                ArchivedAtUtc = DateTime.UtcNow,
                CreatedBy = _currentUserService?.CurrentUser?.UserName,
                CorrelationId = _correlationService?.CurrentCorrelationId
            };
            batch.Items.Add(item);
            count++;
        }

        batch.ItemCount += count;
        batch.PayloadSizeBytes += rows.Sum(row => Encoding.UTF8.GetByteCount(JsonSerializer.Serialize(row, JsonOptions)));
        await _dbContext.SaveChangesAsync(cancellationToken);
        return count;
    }

    private async Task<int> CountExpiredAsync(DataRetentionPolicy policy, DateTime utcNow, CancellationToken cancellationToken)
    {
        var cutoff = utcNow.AddDays(-Math.Max(0, policy.RetentionDays));
        return policy.EntityName switch
        {
            nameof(SearchIndexEntry) => await _dbContext.SearchIndexEntries.CountAsync(x => x.IndexedAt <= cutoff, cancellationToken),
            nameof(RecentSearch) => await _dbContext.RecentSearches.CountAsync(x => x.SearchedAt <= cutoff, cancellationToken),
            nameof(AuditSession) => await _dbContext.AuditSessions.CountAsync(x => x.StartedAtUtc <= cutoff, cancellationToken),
            nameof(BackgroundJobExecution) => await _dbContext.BackgroundJobExecutions.CountAsync(x => (x.CompletedAtUtc ?? x.StartedAtUtc ?? x.QueuedAtUtc) <= cutoff, cancellationToken),
            _ => 0
        };
    }

    private async Task EnsureDefaultPoliciesAsync(CancellationToken cancellationToken)
    {
        if (await _dbContext.DataRetentionPolicies.AnyAsync(cancellationToken))
        {
            return;
        }

        var now = DateTime.UtcNow;
        var modulePolicies = new[]
        {
            ("audit", nameof(AuditSession)),
            ("report", nameof(ReportExecution)),
            ("import", nameof(ImportSession)),
            ("notification", nameof(UserNotification)),
            ("scheduler", nameof(BackgroundJobExecution)),
            ("workspace", "WorkspaceHistory"),
            ("search", nameof(SearchIndexEntry)),
            ("session", nameof(RecentSearch))
        };

        foreach (var (module, entity) in modulePolicies)
        {
            _dbContext.DataRetentionPolicies.Add(new DataRetentionPolicy
            {
                Module = module,
                EntityName = entity,
                RetentionDays = 365,
                ArchiveAfterDays = 180,
                DeleteAfterDays = 730,
                CompressArchivedPayload = true,
                EncryptArchive = false,
                CreatedAtUtc = now,
                CreatedBy = _currentUserService?.CurrentUser?.UserName ?? "system",
                CorrelationId = _correlationService?.CurrentCorrelationId,
                MetadataJson = JsonSerializer.Serialize(new { seeded = true }, JsonOptions)
            });
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task RestoreItemAsync(ArchiveBatchItem item, CancellationToken cancellationToken)
    {
        switch (item.EntityName)
        {
            case nameof(SearchIndexEntry):
                var search = await _dbContext.SearchIndexEntries.FirstOrDefaultAsync(x => x.EntityId == item.EntityId, cancellationToken);
                if (search is not null)
                {
                    search.IsArchived = false;
                    search.ArchivedAtUtc = null;
                }
                break;
            case nameof(RecentSearch):
                var recent = await _dbContext.RecentSearches.FirstOrDefaultAsync(x => x.Id.ToString() == item.EntityId, cancellationToken);
                if (recent is not null)
                {
                    recent.IsArchived = false;
                    recent.ArchivedAtUtc = null;
                }
                break;
        }
    }

    private async Task AuditArchiveAsync(string action, ArchiveBatch? batch, CancellationToken cancellationToken)
    {
        if (_auditService is null || batch is null)
        {
            return;
        }

        await _auditService.LogAsync("ArchiveBatch", batch.Id.ToString(), action, null, new { batch.Module, batch.EntityName, batch.Status }, correlationId: batch.CorrelationId, cancellationToken: cancellationToken);
    }

    private static byte[] Compress(byte[] payload)
    {
        using var target = new MemoryStream();
        using (var gzip = new GZipStream(target, CompressionLevel.SmallestSize, leaveOpen: true))
        {
            gzip.Write(payload, 0, payload.Length);
        }

        return target.ToArray();
    }

    private static byte[] Decompress(byte[] payload)
    {
        using var source = new MemoryStream(payload);
        using var gzip = new GZipStream(source, CompressionMode.Decompress);
        using var target = new MemoryStream();
        gzip.CopyTo(target);
        return target.ToArray();
    }

    private static string ComputeHash(IEnumerable<string?> hashes)
    {
        var payload = string.Join("|", hashes.Where(x => !string.IsNullOrWhiteSpace(x)));
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(payload)));
    }

    private static string ComputeHash(byte[] bytes)
        => Convert.ToHexString(SHA256.HashData(bytes));

    private static string GetEntityId<T>(T row)
        => row switch
        {
            SearchIndexEntry value => value.EntityId,
            RecentSearch value => value.Id.ToString(),
            AuditSession value => value.Id.ToString(),
            BackgroundJobExecution value => value.Id.ToString(),
            _ => row?.GetHashCode().ToString() ?? string.Empty
        };

    private static Guid? GetCompanyId<T>(T row)
        => row switch
        {
            ICompanyScoped scoped => scoped.CompanyId,
            _ => null
        };

    private static Guid? GetBranchId<T>(T row)
        => row switch
        {
            IBranchScoped scoped => scoped.BranchId,
            _ => null
        };

    private static ArchiveBatchDto ToDto(ArchiveBatch batch)
        => new()
        {
            Id = batch.Id,
            CompanyId = batch.CompanyId,
            BranchId = batch.BranchId,
            Module = batch.Module,
            EntityName = batch.EntityName,
            Status = batch.Status,
            ItemCount = batch.ItemCount,
            PayloadSizeBytes = batch.PayloadSizeBytes,
            Hash = batch.Hash,
            MetadataJson = batch.MetadataJson,
            CreatedAtUtc = batch.CreatedAtUtc,
            ArchivedAtUtc = batch.ArchivedAtUtc,
            DeletedAtUtc = batch.DeletedAtUtc,
            CreatedBy = batch.CreatedBy,
            CorrelationId = batch.CorrelationId
        };

    private static DataRetentionPolicyDto ToPolicyDto(DataRetentionPolicy policy)
        => new()
        {
            Id = policy.Id,
            CompanyId = policy.CompanyId,
            BranchId = policy.BranchId,
            Module = policy.Module,
            EntityName = policy.EntityName,
            RetentionDays = policy.RetentionDays,
            ArchiveAfterDays = policy.ArchiveAfterDays,
            DeleteAfterDays = policy.DeleteAfterDays,
            CompressArchivedPayload = policy.CompressArchivedPayload,
            EncryptArchive = policy.EncryptArchive,
            IsEnabled = policy.IsEnabled,
            CreatedAtUtc = policy.CreatedAtUtc,
            CreatedBy = policy.CreatedBy,
            CorrelationId = policy.CorrelationId,
            MetadataJson = policy.MetadataJson
        };
}
