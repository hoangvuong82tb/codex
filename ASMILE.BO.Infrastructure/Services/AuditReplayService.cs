using System.IO.Compression;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ASMILE.BO.Application.DTOs;
using ASMILE.BO.Application.Interfaces;
using ASMILE.BO.Core.Entities;
using ASMILE.BO.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ASMILE.BO.Infrastructure.Services;

public sealed class AuditReplayService : IAuditReplayService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    private static readonly JsonSerializerOptions CompactJsonOptions = new(JsonSerializerDefaults.Web);

    private readonly AppDbContext _dbContext;
    private readonly IRuntimeTelemetryService? _telemetryService;
    private readonly ICurrentUserService? _currentUserService;

    public AuditReplayService(AppDbContext dbContext, ICurrentUserService? currentUserService = null, IRuntimeTelemetryService? telemetryService = null)
    {
        _dbContext = dbContext;
        _telemetryService = telemetryService;
        _currentUserService = currentUserService;
    }

    public async Task<IReadOnlyList<AuditReplayEntryDto>> GetReplayEntriesAsync(string? correlationId = null, string? entityType = null, string? entityId = null, int take = 500, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.AuditReplayEntries.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(correlationId))
        {
            query = query.Where(x => x.CorrelationId == correlationId);
        }

        if (!string.IsNullOrWhiteSpace(entityType))
        {
            query = query.Where(x => x.EntityType == entityType);
        }

        if (!string.IsNullOrWhiteSpace(entityId))
        {
            query = query.Where(x => x.EntityId == entityId);
        }

        return await query
            .OrderByDescending(x => x.TimestampUtc)
            .ThenByDescending(x => x.SequenceNo)
            .ThenByDescending(x => x.Id)
            .Take(Math.Clamp(take, 1, 2000))
            .Select(x => new AuditReplayEntryDto
            {
                Id = x.Id,
                CorrelationId = x.CorrelationId,
                ParentCorrelationId = x.ParentCorrelationId,
                AuditSessionId = x.AuditSessionId,
                SequenceNo = x.SequenceNo,
                Route = x.Route,
                Module = x.Module,
                Action = x.Action,
                EntityType = x.EntityType,
                EntityId = x.EntityId,
                MetadataJson = x.MetadataJson,
                TimestampUtc = x.TimestampUtc
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AuditCorrelationDto>> GetCorrelationsAsync(string? entityType = null, string? entityId = null, int take = 200, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.AuditCorrelations.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(entityType))
        {
            query = query.Where(x => x.EntityType == entityType);
        }

        if (!string.IsNullOrWhiteSpace(entityId))
        {
            query = query.Where(x => x.EntityId == entityId);
        }

        return await query
            .OrderByDescending(x => x.CreatedAtUtc)
            .Take(Math.Clamp(take, 1, 500))
            .Select(x => new AuditCorrelationDto
            {
                Id = x.Id,
                CorrelationId = x.CorrelationId,
                ParentCorrelationId = x.ParentCorrelationId,
                AuditSessionId = x.AuditSessionId,
                EntityType = x.EntityType,
                EntityId = x.EntityId,
                Module = x.Module,
                Action = x.Action,
                CreatedAtUtc = x.CreatedAtUtc
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AuditEntitySnapshotDto>> GetSnapshotsAsync(int auditLogId, CancellationToken cancellationToken = default)
    {
        var snapshots = await _dbContext.AuditEntitySnapshots.AsNoTracking()
            .Where(x => x.AuditLogId == auditLogId)
            .OrderBy(x => x.EntityType)
            .ThenBy(x => x.EntityId)
            .ToListAsync(cancellationToken);

        return snapshots.Select(ToSnapshotDto).ToArray();
    }

    public async Task<AuditReplaySummaryDto> GetReplaySummaryAsync(string correlationId, CancellationToken cancellationToken = default)
    {
        var entries = await GetReplayEntriesAsync(correlationId, null, null, 1000, cancellationToken);
        return new AuditReplaySummaryDto
        {
            CorrelationId = correlationId,
            EntryCount = entries.Count,
            FailedCount = entries.Count(x => x.Action.Contains("fail", StringComparison.OrdinalIgnoreCase) || x.Action.Contains("error", StringComparison.OrdinalIgnoreCase)),
            RouteTrail = entries.OrderBy(x => x.SequenceNo).ThenBy(x => x.TimestampUtc).Select(x => x.Route).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).ToArray(),
            Entries = entries.OrderBy(x => x.SequenceNo).ThenBy(x => x.TimestampUtc).ToArray()
        };
    }

    public async Task<IReadOnlyList<AuditExportHistoryDto>> GetExportHistoryAsync(int take = 100, CancellationToken cancellationToken = default)
    {
        return await _dbContext.AuditExportHistory.AsNoTracking()
            .OrderByDescending(x => x.CreatedAtUtc)
            .Take(Math.Clamp(take, 1, 500))
            .Select(x => new AuditExportHistoryDto
            {
                Id = x.Id,
                RequestedByUserId = x.RequestedByUserId,
                ExportType = x.ExportType,
                FilterJson = x.FilterJson,
                FilePath = x.FilePath,
                CreatedAtUtc = x.CreatedAtUtc,
                ExportHash = x.ExportHash,
                RowCount = x.RowCount,
                HashAlgorithm = x.HashAlgorithm,
                CorrelationCoverageJson = x.CorrelationCoverageJson,
                IntegrityManifestJson = x.IntegrityManifestJson
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AuditTimelineNodeDto>> GetTimelineAsync(string? correlationId = null, Guid? sessionId = null, int page = 1, int pageSize = 200, CancellationToken cancellationToken = default)
    {
        var safePage = Math.Max(1, page);
        var safePageSize = Math.Clamp(pageSize, 1, 500);
        var query = _dbContext.AuditReplayEntries.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(correlationId))
        {
            query = query.Where(x => x.CorrelationId == correlationId);
        }

        if (sessionId.HasValue)
        {
            query = query.Where(x => x.AuditSessionId == sessionId.Value);
        }

        return await query
            .OrderBy(x => x.TimestampUtc)
            .ThenBy(x => x.SequenceNo)
            .ThenBy(x => x.Id)
            .Skip((safePage - 1) * safePageSize)
            .Take(safePageSize)
            .Select(x => new AuditTimelineNodeDto
            {
                Id = x.Id,
                CorrelationId = x.CorrelationId,
                ParentCorrelationId = x.ParentCorrelationId,
                SequenceNo = x.SequenceNo,
                Module = x.Module,
                Action = x.Action,
                EntityType = x.EntityType,
                EntityId = x.EntityId,
                Route = x.Route,
                TimestampUtc = x.TimestampUtc
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<AuditReplayDiffResultDto> ReconstructStateAsync(string correlationId, CancellationToken cancellationToken = default)
    {
        var timeline = await GetTimelineAsync(correlationId, null, 1, 500, cancellationToken);
        var auditLogIds = await _dbContext.AuditLogs.AsNoTracking()
            .Where(x => x.CorrelationId == correlationId)
            .OrderBy(x => x.CreatedAt)
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);
        var snapshots = await _dbContext.AuditEntitySnapshots.AsNoTracking()
            .Where(x => x.AuditLogId.HasValue && auditLogIds.Contains(x.AuditLogId.Value))
            .OrderBy(x => x.AuditLogId)
            .ToListAsync(cancellationToken);
        var snapshotDtos = snapshots.Select(ToSnapshotDto).ToArray();
        var frames = snapshotDtos
            .Select((x, index) => new AuditReplayStateFrameDto
            {
                SequenceNo = index + 1,
                EntityType = x.EntityType,
                EntityId = x.EntityId,
                StateJson = x.AfterJson ?? x.BeforeJson,
                TimestampUtc = timeline.ElementAtOrDefault(Math.Min(index, Math.Max(0, timeline.Count - 1)))?.TimestampUtc ?? DateTime.MinValue
            })
            .ToArray();

        return new AuditReplayDiffResultDto
        {
            CorrelationId = correlationId,
            Timeline = timeline,
            StateFrames = frames,
            Snapshots = snapshotDtos
        };
    }

    public Task<IReadOnlyList<AuditReplayEntryDto>> GetReplayBySessionAsync(Guid sessionId, CancellationToken cancellationToken = default)
        => GetReplayEntriesByQueryAsync(_dbContext.AuditReplayEntries.AsNoTracking().Where(x => x.AuditSessionId == sessionId), cancellationToken);

    public Task<IReadOnlyList<AuditReplayEntryDto>> GetReplayByJournalAsync(Guid journalHeaderId, CancellationToken cancellationToken = default)
        => GetReplayEntriesByQueryAsync(_dbContext.AuditReplayEntries.AsNoTracking().Where(x => x.EntityType == "JournalHeader" && x.EntityId == journalHeaderId.ToString()), cancellationToken);

    public Task<IReadOnlyList<AuditReplayEntryDto>> GetReplayBySourceAsync(string sourceType, string sourceId, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.AuditReplayEntries.AsNoTracking()
            .Where(x => x.EntityType == sourceType && x.EntityId == sourceId);
        return GetReplayEntriesByQueryAsync(query, cancellationToken);
    }

    public async Task<AuditExportResultDto> ExportAsync(AuditExportRequestDto request, CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();
        var exportType = NormalizeExportType(request.ExportType);
        var filePath = ResolveFilePath(request.FilePath, exportType);
        Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);

        var query = BuildQuery(request);
        var recordCount = 0;
        await using (var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.Read, bufferSize: 64 * 1024, useAsync: true))
        {
            switch (exportType)
            {
                case "csv":
                    recordCount = await WriteCsvAsync(query, stream, cancellationToken);
                    break;
                case "ndjson":
                    recordCount = await WriteNdjsonAsync(query, stream, cancellationToken);
                    break;
                case "zip":
                    recordCount = await WriteZipAsync(query, stream, cancellationToken);
                    break;
                default:
                    recordCount = await WriteJsonAsync(query, stream, cancellationToken);
                    break;
            }

            await stream.FlushAsync(cancellationToken);
        }

        var exportHash = await ComputeFileHashAsync(filePath, cancellationToken);
        var coverage = await BuildCoverageJsonAsync(BuildQuery(request), cancellationToken);
        var manifest = BuildManifestJson(filePath, exportType, recordCount, exportHash, coverage);

        var history = new AuditExportHistory
        {
            RequestedByUserId = _currentUserService?.CurrentUser?.Id,
            CompanyId = _dbContext.CurrentCompanyId,
            BranchId = _dbContext.CurrentBranchId,
            ExportType = exportType.ToUpperInvariant(),
            FilterJson = request.FilterJson ?? BuildFilterJson(request),
            FilePath = filePath,
            CreatedAtUtc = DateTime.UtcNow,
            ExportHash = exportHash,
            RowCount = recordCount,
            HashAlgorithm = "SHA256",
            CorrelationCoverageJson = coverage,
            IntegrityManifestJson = manifest
        };
        _dbContext.AuditExportHistory.Add(history);
        await _dbContext.SaveChangesAsync(cancellationToken);
        if (_telemetryService is not null)
        {
            sw.Stop();
            await _telemetryService.TrackSlowOperationAsync("audit-replay", "export", sw.ElapsedMilliseconds, "Information", "Audit export completed.", request.CorrelationId, null, new { exportType, recordCount }, cancellationToken);
        }

        return new AuditExportResultDto
        {
            ExportHistoryId = history.Id,
            FilePath = filePath,
            ExportType = exportType.ToUpperInvariant(),
            RecordCount = recordCount,
            ExportHash = exportHash,
            IntegrityManifestJson = manifest
        };
    }

    private IQueryable<AuditReplayEntry> BuildQuery(AuditExportRequestDto request)
    {
        var query = _dbContext.AuditReplayEntries.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(request.CorrelationId))
        {
            query = query.Where(x => x.CorrelationId == request.CorrelationId);
        }

        if (!string.IsNullOrWhiteSpace(request.EntityType))
        {
            query = query.Where(x => x.EntityType == request.EntityType);
        }

        if (!string.IsNullOrWhiteSpace(request.EntityId))
        {
            query = query.Where(x => x.EntityId == request.EntityId);
        }

        return query.OrderBy(x => x.SequenceNo).ThenBy(x => x.TimestampUtc).ThenBy(x => x.Id);
    }

    private static async Task<int> WriteJsonAsync(IQueryable<AuditReplayEntry> query, Stream destination, CancellationToken cancellationToken)
    {
        var count = 0;
        await using var writer = new StreamWriter(destination, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false), leaveOpen: true);
        await writer.WriteAsync("[");
        var first = true;
        await foreach (var item in query.AsAsyncEnumerable().WithCancellation(cancellationToken))
        {
            if (!first)
            {
                await writer.WriteAsync(",");
            }

            first = false;
            await writer.WriteAsync(JsonSerializer.Serialize(new AuditReplayEntryDto
            {
                Id = item.Id,
                CorrelationId = item.CorrelationId,
                ParentCorrelationId = item.ParentCorrelationId,
                AuditSessionId = item.AuditSessionId,
                SequenceNo = item.SequenceNo,
                Route = item.Route,
                Module = item.Module,
                Action = item.Action,
                EntityType = item.EntityType,
                EntityId = item.EntityId,
                MetadataJson = item.MetadataJson,
                TimestampUtc = item.TimestampUtc
            }, JsonOptions));
            count++;
        }

        await writer.WriteAsync("]");
        await writer.FlushAsync(cancellationToken);
        return count;
    }

    private static async Task<int> WriteCsvAsync(IQueryable<AuditReplayEntry> query, Stream destination, CancellationToken cancellationToken)
    {
        var count = 0;
        await using var writer = new StreamWriter(destination, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true), leaveOpen: true);
        await writer.WriteLineAsync("Id,CorrelationId,SequenceNo,Route,Module,Action,EntityType,EntityId,MetadataJson,TimestampUtc".AsMemory(), cancellationToken);
        await foreach (var item in query.AsAsyncEnumerable().WithCancellation(cancellationToken))
        {
            var line = string.Join(",",
                Escape(item.Id),
                Escape(item.CorrelationId),
                Escape(item.SequenceNo),
                Escape(item.Route),
                Escape(item.Module),
                Escape(item.Action),
                Escape(item.EntityType),
                Escape(item.EntityId),
                Escape(item.MetadataJson),
                Escape(item.TimestampUtc.ToString("O")));
            await writer.WriteLineAsync(line.AsMemory(), cancellationToken);
            count++;
        }

        await writer.FlushAsync(cancellationToken);
        return count;
    }

    private static async Task<int> WriteNdjsonAsync(IQueryable<AuditReplayEntry> query, Stream destination, CancellationToken cancellationToken)
    {
        var count = 0;
        await using var writer = new StreamWriter(destination, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false), leaveOpen: true);
        await foreach (var item in query.AsAsyncEnumerable().WithCancellation(cancellationToken))
        {
            await writer.WriteLineAsync(JsonSerializer.Serialize(ToReplayDto(item), CompactJsonOptions).AsMemory(), cancellationToken);
            count++;
        }

        await writer.FlushAsync(cancellationToken);
        return count;
    }

    private static async Task<int> WriteZipAsync(IQueryable<AuditReplayEntry> query, Stream destination, CancellationToken cancellationToken)
    {
        var count = 0;
        using var archive = new ZipArchive(destination, ZipArchiveMode.Create, leaveOpen: true);
        var jsonEntry = archive.CreateEntry("audit-replay.json", CompressionLevel.SmallestSize);
        await using (var jsonStream = jsonEntry.Open())
        await using (var writer = new StreamWriter(jsonStream, new UTF8Encoding(false), leaveOpen: true))
        {
            await writer.WriteAsync("[");
            var first = true;
            await foreach (var item in query.AsAsyncEnumerable().WithCancellation(cancellationToken))
            {
                if (!first)
                {
                    await writer.WriteAsync(",");
                }

                first = false;
                await writer.WriteAsync(JsonSerializer.Serialize(new AuditReplayEntryDto
                {
                    Id = item.Id,
                    CorrelationId = item.CorrelationId,
                    ParentCorrelationId = item.ParentCorrelationId,
                    AuditSessionId = item.AuditSessionId,
                    SequenceNo = item.SequenceNo,
                    Route = item.Route,
                    Module = item.Module,
                    Action = item.Action,
                    EntityType = item.EntityType,
                    EntityId = item.EntityId,
                    MetadataJson = item.MetadataJson,
                    TimestampUtc = item.TimestampUtc
                }, JsonOptions));
                count++;
            }

            await writer.WriteAsync("]");
            await writer.FlushAsync(cancellationToken);
        }

        var csvEntry = archive.CreateEntry("audit-replay.csv", CompressionLevel.SmallestSize);
        await using (var csvStream = csvEntry.Open())
        {
            count = await WriteCsvAsync(query, csvStream, cancellationToken);
        }

        var manifestEntry = archive.CreateEntry("manifest.json", CompressionLevel.SmallestSize);
        await using (var manifestStream = manifestEntry.Open())
        await using (var manifestWriter = new StreamWriter(manifestStream, new UTF8Encoding(false), leaveOpen: true))
        {
            await manifestWriter.WriteAsync(JsonSerializer.Serialize(new
            {
                exportedAtUtc = DateTime.UtcNow,
                rowCount = count,
                hashAlgorithm = "SHA256",
                entries = new[] { "audit-replay.json", "audit-replay.csv" }
            }, JsonOptions));
            await manifestWriter.FlushAsync(cancellationToken);
        }

        return count;
    }

    private static string ResolveFilePath(string? filePath, string exportType)
    {
        if (!string.IsNullOrWhiteSpace(filePath))
        {
            return Path.GetFullPath(filePath);
        }

        var folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "ASMILE-BO", "audit-exports");
        var ext = exportType.ToLowerInvariant() switch
        {
            "csv" => ".csv",
            "ndjson" => ".ndjson",
            "zip" => ".zip",
            _ => ".json"
        };
        var safeName = MakeSafeFileName($"audit-export-{DateTime.UtcNow:yyyyMMdd-HHmmss}{ext}");
        return Path.Combine(folder, safeName);
    }

    private static string MakeSafeFileName(string fileName)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var builder = new StringBuilder(fileName.Length);
        foreach (var ch in fileName)
        {
            builder.Append(invalid.Contains(ch) ? '_' : ch);
        }

        return builder.ToString();
    }

    private static string NormalizeExportType(string? exportType)
    {
        var normalized = string.IsNullOrWhiteSpace(exportType) ? "json" : exportType.Trim().ToLowerInvariant();
        return normalized is "json" or "csv" or "zip" or "ndjson" ? normalized : "json";
    }

    private static string BuildFilterJson(AuditExportRequestDto request)
        => JsonSerializer.Serialize(new
        {
            request.CorrelationId,
            request.EntityType,
            request.EntityId
        }, JsonOptions);

    private static string Escape(object? value)
    {
        var text = value?.ToString() ?? string.Empty;
        return "\"" + text.Replace("\"", "\"\"", StringComparison.Ordinal) + "\"";
    }

    private static AuditEntitySnapshotDto ToSnapshotDto(AuditEntitySnapshot snapshot)
    {
        var payload = AuditSnapshotUtility.Read(snapshot.BeforeJson, snapshot.AfterJson, snapshot.DiffJson, snapshot.SnapshotHash, snapshot.CompressedPayload, snapshot.CompressionType, snapshot.SnapshotSize, snapshot.HashAlgorithm);
        return new AuditEntitySnapshotDto
        {
            Id = snapshot.Id,
            AuditLogId = snapshot.AuditLogId,
            EntityType = snapshot.EntityType,
            EntityId = snapshot.EntityId,
            BeforeJson = payload.BeforeJson,
            AfterJson = payload.AfterJson,
            DiffJson = payload.DiffJson,
            SnapshotHash = payload.SnapshotHash,
            CompressionType = payload.CompressionType,
            SnapshotSize = payload.SnapshotSize,
            HashAlgorithm = payload.HashAlgorithm,
            HashVerified = payload.HashVerified,
            HashMismatchDetected = !payload.HashVerified
        };
    }

    private static AuditReplayEntryDto ToReplayDto(AuditReplayEntry item)
        => new()
        {
            Id = item.Id,
            CorrelationId = item.CorrelationId,
            ParentCorrelationId = item.ParentCorrelationId,
            AuditSessionId = item.AuditSessionId,
            SequenceNo = item.SequenceNo,
            Route = item.Route,
            Module = item.Module,
            Action = item.Action,
            EntityType = item.EntityType,
            EntityId = item.EntityId,
            MetadataJson = item.MetadataJson,
            TimestampUtc = item.TimestampUtc
        };

    private static async Task<IReadOnlyList<AuditReplayEntryDto>> GetReplayEntriesByQueryAsync(IQueryable<AuditReplayEntry> query, CancellationToken cancellationToken)
        => await query
            .OrderBy(x => x.TimestampUtc)
            .ThenBy(x => x.SequenceNo)
            .ThenBy(x => x.Id)
            .Take(2000)
            .Select(x => new AuditReplayEntryDto
            {
                Id = x.Id,
                CorrelationId = x.CorrelationId,
                ParentCorrelationId = x.ParentCorrelationId,
                AuditSessionId = x.AuditSessionId,
                SequenceNo = x.SequenceNo,
                Route = x.Route,
                Module = x.Module,
                Action = x.Action,
                EntityType = x.EntityType,
                EntityId = x.EntityId,
                MetadataJson = x.MetadataJson,
                TimestampUtc = x.TimestampUtc
            })
            .ToListAsync(cancellationToken);

    private static async Task<string> ComputeFileHashAsync(string filePath, CancellationToken cancellationToken)
    {
        using var sha = SHA256.Create();
        await using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 64 * 1024, useAsync: true);
        var buffer = new byte[64 * 1024];
        int read;
        while ((read = await stream.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken)) > 0)
        {
            sha.TransformBlock(buffer, 0, read, null, 0);
        }

        sha.TransformFinalBlock([], 0, 0);
        return Convert.ToHexString(sha.Hash ?? []);
    }

    private static async Task<string> BuildCoverageJsonAsync(IQueryable<AuditReplayEntry> query, CancellationToken cancellationToken)
    {
        var correlations = await query
            .Select(x => x.CorrelationId)
            .Distinct()
            .OrderBy(x => x)
            .Take(200)
            .ToListAsync(cancellationToken);
        return JsonSerializer.Serialize(new { count = correlations.Count, correlations }, JsonOptions);
    }

    private static string BuildManifestJson(string filePath, string exportType, int rowCount, string exportHash, string coverageJson)
        => JsonSerializer.Serialize(new
        {
            exportedAtUtc = DateTime.UtcNow,
            fileName = Path.GetFileName(filePath),
            exportType = exportType.ToUpperInvariant(),
            rowCount,
            hashAlgorithm = "SHA256",
            hash = exportHash,
            correlationCoverage = JsonSerializer.Deserialize<JsonElement>(coverageJson)
        }, JsonOptions);
}
