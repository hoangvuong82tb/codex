using System.Collections.Concurrent;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;
using ASMILE.BO.Application.DTOs;
using ASMILE.BO.Application.Interfaces;
using ASMILE.BO.Core.Entities;
using ASMILE.BO.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ASMILE.BO.Infrastructure.Services;

public sealed class RuntimeTelemetryService : IRuntimeTelemetryService, IHostedService, IDisposable
{
    private const int BatchSize = 100;
    private static readonly TimeSpan FlushInterval = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan DuplicateWindow = TimeSpan.FromSeconds(30);
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly Channel<TelemetryEnvelope> _channel = Channel.CreateBounded<TelemetryEnvelope>(new BoundedChannelOptions(4096)
    {
        SingleReader = true,
        SingleWriter = false,
        FullMode = BoundedChannelFullMode.Wait
    });
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IAuditCorrelationService? _correlationService;
    private readonly ICurrentCompanyContext _companyContext;
    private readonly ICurrentUserService? _currentUserService;
    private readonly ILogger<RuntimeTelemetryService> _logger;
    private readonly ConcurrentDictionary<string, DateTime> _exceptionSuppression = new(StringComparer.OrdinalIgnoreCase);
    private readonly SemaphoreSlim _flushGate = new(1, 1);
    private CancellationTokenSource? _workerCts;
    private Task? _workerTask;
    private bool _disposed;

    public RuntimeTelemetryService(
        IServiceScopeFactory scopeFactory,
        ICurrentCompanyContext companyContext,
        ILogger<RuntimeTelemetryService> logger,
        IAuditCorrelationService? correlationService = null,
        ICurrentUserService? currentUserService = null)
    {
        _scopeFactory = scopeFactory;
        _companyContext = companyContext;
        _logger = logger;
        _correlationService = correlationService;
        _currentUserService = currentUserService;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (_workerTask is not null)
        {
            return Task.CompletedTask;
        }

        _workerCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _workerTask = RunAsync(_workerCts.Token);
        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_workerCts is null)
        {
            return;
        }

        _channel.Writer.TryComplete();
        _workerCts.Cancel();
        if (_workerTask is not null)
        {
            try
            {
                await _workerTask.WaitAsync(cancellationToken);
            }
            catch (OperationCanceledException)
            {
            }
        }

        await FlushAsync(CancellationToken.None);
    }

    public async Task TrackMetricAsync(string module, string category, string metricName, double metricValue, string? unit = null, string? message = null, string? correlationId = null, Guid? sessionId = null, object? metadata = null, CancellationToken cancellationToken = default)
    {
        var envelope = CreateEnvelope(
            eventType: "Metric",
            module: module,
            category: category,
            severity: "Information",
            eventName: "Metric",
            message: message ?? $"{metricName}={metricValue}",
            correlationId: correlationId,
            sessionId: sessionId,
            metadata: metadata,
            metric: new RuntimeMetricSnapshot
            {
                Module = module,
                Category = category,
                Severity = "Information",
                MetricName = metricName,
                MetricValue = metricValue,
                Unit = unit,
                Message = message ?? $"{metricName}={metricValue}",
                MetadataJson = ToMetadataJson(metadata),
                CorrelationId = correlationId,
                SessionId = sessionId,
                RecordedAtUtc = DateTime.UtcNow,
                MachineName = Environment.MachineName,
                UserName = _currentUserService?.CurrentUser?.UserName,
                CompanyId = _companyContext.CompanyId,
                BranchId = _companyContext.BranchId
            });
        await EnqueueAsync(envelope, cancellationToken);
    }

    public async Task TrackExceptionAsync(Exception exception, string module, string category, string? message = null, string? correlationId = null, Guid? sessionId = null, object? metadata = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(exception);
        var stackHash = ComputeStackHash(exception);
        var suppressionKey = $"{module}|{category}|{exception.GetType().FullName}|{stackHash}";
        var now = DateTime.UtcNow;
        if (_exceptionSuppression.TryGetValue(suppressionKey, out var lastSeen) && now - lastSeen < DuplicateWindow)
        {
            return;
        }

        _exceptionSuppression[suppressionKey] = now;
        await EnqueueAsync(CreateEnvelope(
            "Exception",
            module,
            category,
            "Error",
            exception.GetType().Name,
            exception.Message,
            correlationId,
            sessionId,
            metadata,
            exception: new RuntimeExceptionLog
            {
                Module = module,
                Category = category,
                Severity = "Error",
                ExceptionType = exception.GetType().FullName ?? exception.GetType().Name,
                StackHash = stackHash,
                Message = message ?? exception.Message,
                MetadataJson = ToMetadataJson(metadata),
                OccurrenceCount = 1,
                CorrelationId = correlationId,
                SessionId = sessionId,
                RecordedAtUtc = now,
                MachineName = Environment.MachineName,
                UserName = _currentUserService?.CurrentUser?.UserName,
                CompanyId = _companyContext.CompanyId,
                BranchId = _companyContext.BranchId
            }), cancellationToken);
    }

    public async Task TrackSlowOperationAsync(string module, string operationName, long durationMs, string severity = "Warning", string? message = null, string? correlationId = null, Guid? sessionId = null, object? metadata = null, CancellationToken cancellationToken = default)
    {
        await EnqueueAsync(CreateEnvelope(
            "SlowOperation",
            module,
            operationName,
            severity,
            operationName,
            message ?? operationName,
            correlationId,
            sessionId,
            metadata,
            slowOperation: new SlowOperationLog
            {
                Module = module,
                Category = operationName,
                Severity = severity,
                OperationName = operationName,
                DurationMs = durationMs,
                Message = message ?? operationName,
                MetadataJson = ToMetadataJson(metadata),
                CorrelationId = correlationId,
                SessionId = sessionId,
                RecordedAtUtc = DateTime.UtcNow,
                MachineName = Environment.MachineName,
                UserName = _currentUserService?.CurrentUser?.UserName,
                CompanyId = _companyContext.CompanyId,
                BranchId = _companyContext.BranchId
            }), cancellationToken);
    }

    public async Task TrackUiLatencyAsync(string module, string operationName, long durationMs, bool isFrozen = false, string? message = null, string? correlationId = null, Guid? sessionId = null, object? metadata = null, CancellationToken cancellationToken = default)
    {
        await EnqueueAsync(CreateEnvelope(
            "UiLatency",
            module,
            operationName,
            isFrozen ? "Error" : "Information",
            operationName,
            message ?? operationName,
            correlationId,
            sessionId,
            metadata,
            uiResponsiveness: new UiResponsivenessSnapshot
            {
                Module = module,
                Category = operationName,
                Severity = isFrozen ? "Error" : "Information",
                OperationName = operationName,
                DurationMs = durationMs,
                IsFrozen = isFrozen,
                Message = message ?? operationName,
                MetadataJson = ToMetadataJson(metadata),
                CorrelationId = correlationId,
                SessionId = sessionId,
                RecordedAtUtc = DateTime.UtcNow,
                MachineName = Environment.MachineName,
                UserName = _currentUserService?.CurrentUser?.UserName,
                CompanyId = _companyContext.CompanyId,
                BranchId = _companyContext.BranchId
            }), cancellationToken);
    }

    public async Task TrackResourceUsageAsync(string module, double? cpuUsagePercent = null, long? memoryUsageBytes = null, long? workingSetBytes = null, int? threadCount = null, string? message = null, string? correlationId = null, Guid? sessionId = null, object? metadata = null, CancellationToken cancellationToken = default)
    {
        var process = Process.GetCurrentProcess();
        await EnqueueAsync(CreateEnvelope(
            "ResourceUsage",
            module,
            "runtime",
            "Information",
            "ResourceUsage",
            message ?? "Runtime resource snapshot recorded.",
            correlationId,
            sessionId,
            metadata,
            resource: new SystemResourceSnapshot
            {
                Module = module,
                Category = "runtime",
                Severity = "Information",
                CpuUsagePercent = cpuUsagePercent ?? 0d,
                MemoryUsageBytes = memoryUsageBytes ?? process.PrivateMemorySize64,
                WorkingSetBytes = workingSetBytes ?? process.WorkingSet64,
                ThreadCount = threadCount ?? process.Threads.Count,
                Message = message ?? "Runtime resource snapshot recorded.",
                MetadataJson = ToMetadataJson(metadata),
                CorrelationId = correlationId,
                SessionId = sessionId,
                RecordedAtUtc = DateTime.UtcNow,
                MachineName = Environment.MachineName,
                UserName = _currentUserService?.CurrentUser?.UserName,
                CompanyId = _companyContext.CompanyId,
                BranchId = _companyContext.BranchId
            }), cancellationToken);
    }

    public Task TrackCommandLatencyAsync(string module, string commandName, long durationMs, string? message = null, string? correlationId = null, Guid? sessionId = null, object? metadata = null, CancellationToken cancellationToken = default)
        => TrackSlowOperationAsync(module, commandName, durationMs, "Information", message, correlationId, sessionId, metadata, cancellationToken);

    public Task TrackQueryLatencyAsync(string module, string queryName, long durationMs, string? message = null, string? correlationId = null, Guid? sessionId = null, object? metadata = null, CancellationToken cancellationToken = default)
        => TrackSlowOperationAsync(module, queryName, durationMs, "Information", message, correlationId, sessionId, metadata, cancellationToken);

    public Task TrackExportDurationAsync(string module, string exportName, long durationMs, string? message = null, string? correlationId = null, Guid? sessionId = null, object? metadata = null, CancellationToken cancellationToken = default)
        => TrackSlowOperationAsync(module, exportName, durationMs, "Information", message, correlationId, sessionId, metadata, cancellationToken);

    public Task TrackImportDurationAsync(string module, string importName, long durationMs, string? message = null, string? correlationId = null, Guid? sessionId = null, object? metadata = null, CancellationToken cancellationToken = default)
        => TrackSlowOperationAsync(module, importName, durationMs, "Information", message, correlationId, sessionId, metadata, cancellationToken);

    public Task TrackSchedulerExecutionMetricsAsync(string module, string workflowKey, long durationMs, string status, string? message = null, string? correlationId = null, Guid? sessionId = null, object? metadata = null, CancellationToken cancellationToken = default)
        => TrackSlowOperationAsync(module, workflowKey, durationMs, string.Equals(status, "Completed", StringComparison.OrdinalIgnoreCase) ? "Information" : "Warning", message, correlationId, sessionId, metadata, cancellationToken);

    public Task TrackWorkflowMetricsAsync(string module, string workflowKey, long durationMs, string status, string? message = null, string? correlationId = null, Guid? sessionId = null, object? metadata = null, CancellationToken cancellationToken = default)
        => TrackSlowOperationAsync(module, workflowKey, durationMs, string.Equals(status, "Completed", StringComparison.OrdinalIgnoreCase) ? "Information" : "Warning", message, correlationId, sessionId, metadata, cancellationToken);

    public Task TrackRecoveryMetricsAsync(string module, string recoveryName, long durationMs, string status, string? message = null, string? correlationId = null, Guid? sessionId = null, object? metadata = null, CancellationToken cancellationToken = default)
        => TrackSlowOperationAsync(module, recoveryName, durationMs, string.Equals(status, "Completed", StringComparison.OrdinalIgnoreCase) ? "Information" : "Warning", message, correlationId, sessionId, metadata, cancellationToken);

    public Task TrackCacheMetricAsync(string module, string cacheName, bool hit, string? message = null, string? correlationId = null, Guid? sessionId = null, object? metadata = null, CancellationToken cancellationToken = default)
        => TrackMetricAsync(module, "cache", hit ? $"{cacheName}.hit" : $"{cacheName}.miss", hit ? 1 : 0, "count", message, correlationId, sessionId, metadata, cancellationToken);

    public Task TrackNavigationLatencyAsync(string module, string route, long durationMs, string? message = null, string? correlationId = null, Guid? sessionId = null, object? metadata = null, CancellationToken cancellationToken = default)
        => TrackUiLatencyAsync(module, route, durationMs, durationMs >= 1000, message, correlationId, sessionId, metadata, cancellationToken);

    public Task TrackSearchLatencyAsync(string module, string query, long durationMs, string? message = null, string? correlationId = null, Guid? sessionId = null, object? metadata = null, CancellationToken cancellationToken = default)
        => TrackQueryLatencyAsync(module, query, durationMs, message, correlationId, sessionId, metadata, cancellationToken);

    public async Task<RuntimeTelemetrySummaryDto> GetRuntimeSummaryAsync(DateTime? fromUtc = null, DateTime? toUtc = null, CancellationToken cancellationToken = default)
    {
        var from = fromUtc ?? DateTime.UtcNow.AddDays(-1);
        var to = toUtc ?? DateTime.UtcNow;
        await using var scope = _scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var events = db.TelemetryEvents.AsNoTracking().Where(x => x.RecordedAtUtc >= from && x.RecordedAtUtc <= to);
        var slowOps = db.SlowOperationLogs.AsNoTracking().Where(x => x.RecordedAtUtc >= from && x.RecordedAtUtc <= to);
        var uiSnapshots = db.UiResponsivenessSnapshots.AsNoTracking().Where(x => x.RecordedAtUtc >= from && x.RecordedAtUtc <= to);
        var resourceSnapshots = db.SystemResourceSnapshots.AsNoTracking().Where(x => x.RecordedAtUtc >= from && x.RecordedAtUtc <= to);
        var durationSamples = await slowOps.Select(x => (double?)x.DurationMs).Concat(uiSnapshots.Select(x => (double?)x.DurationMs)).ToListAsync(cancellationToken);
        var durations = durationSamples.Where(x => x.HasValue).Select(x => x!.Value).ToArray();
        var totalEvents = await events.CountAsync(cancellationToken);
        var exceptionCount = await events.CountAsync(x => x.EventType == "Exception", cancellationToken);
        var cancellationCount = await events.CountAsync(x => x.SearchText.Contains("CANCEL"), cancellationToken);
        var timeoutCount = await events.CountAsync(x => x.SearchText.Contains("TIMEOUT") || x.SearchText.Contains("TIMEDOUT"), cancellationToken);
        var longRunningCount = await slowOps.CountAsync(x => x.DurationMs >= 1000, cancellationToken) + await uiSnapshots.CountAsync(x => x.DurationMs >= 1000, cancellationToken);
        var durationAverage = durations.Length == 0 ? 0d : durations.Average();

        return new RuntimeTelemetrySummaryDto
        {
            FromUtc = from,
            ToUtc = to,
            TotalEvents = totalEvents,
            ExceptionCount = exceptionCount,
            SlowOperationCount = await slowOps.CountAsync(cancellationToken),
            UiLatencyCount = await uiSnapshots.CountAsync(cancellationToken),
            ResourceSnapshotCount = await resourceSnapshots.CountAsync(cancellationToken),
            ArchivedCount = await events.CountAsync(x => x.IsArchived, cancellationToken),
            AverageDurationMs = durationAverage,
            P50DurationMs = Percentile(durations, 50),
            P95DurationMs = Percentile(durations, 95),
            P99DurationMs = Percentile(durations, 99),
            ThroughputPerMinute = totalEvents <= 0 ? 0d : totalEvents / Math.Max(1d, (to - from).TotalMinutes),
            ErrorRate = totalEvents <= 0 ? 0d : (double)exceptionCount / totalEvents,
            CancellationRate = totalEvents <= 0 ? 0d : (double)cancellationCount / totalEvents,
            TimeoutRate = totalEvents <= 0 ? 0d : (double)timeoutCount / totalEvents,
            LongRunningOperationCount = longRunningCount,
            AverageCpuUsagePercent = await resourceSnapshots.AnyAsync(cancellationToken) ? await resourceSnapshots.AverageAsync(x => x.CpuUsagePercent, cancellationToken) : 0d,
            AverageMemoryUsageBytes = await resourceSnapshots.AnyAsync(cancellationToken) ? (long)await resourceSnapshots.AverageAsync(x => (double)x.MemoryUsageBytes, cancellationToken) : 0,
            PeakMemoryUsageBytes = await resourceSnapshots.AnyAsync(cancellationToken) ? await resourceSnapshots.MaxAsync(x => x.MemoryUsageBytes, cancellationToken) : 0,
            LastRecordedAtUtc = await events.MaxAsync(x => (DateTime?)x.RecordedAtUtc, cancellationToken)
        };
    }

    public async Task<IReadOnlyList<RuntimeTelemetryTimelineDto>> GetTelemetryTimelineAsync(string? query = null, string? module = null, bool includeArchived = false, int take = 200, CancellationToken cancellationToken = default)
    {
        var safeTake = Math.Clamp(take, 1, 1000);
        var normalizedQuery = string.IsNullOrWhiteSpace(query) ? null : query.Trim().ToUpperInvariant();
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var timeline = db.TelemetryEvents.AsNoTracking().AsQueryable();
        if (!includeArchived)
        {
            timeline = timeline.Where(x => !x.IsArchived);
        }

        if (!string.IsNullOrWhiteSpace(module))
        {
            timeline = timeline.Where(x => x.Module == module);
        }

        if (!string.IsNullOrWhiteSpace(normalizedQuery))
        {
            timeline = timeline.Where(x => x.SearchText.Contains(normalizedQuery));
        }

        return await timeline
            .OrderByDescending(x => x.RecordedAtUtc)
            .Take(safeTake)
            .Select(x => new RuntimeTelemetryTimelineDto
            {
                Id = x.Id,
                SourceType = x.EventType,
                CompanyId = x.CompanyId,
                BranchId = x.BranchId,
                CorrelationId = x.CorrelationId,
                SessionId = x.SessionId,
                Module = x.Module,
                Category = x.Category,
                Severity = x.Severity,
                EventName = x.EventName,
                Message = x.Message,
                DurationMs = 0,
                ExceptionType = null,
                StackHash = null,
                MetadataJson = x.MetadataJson,
                RecordedAtUtc = x.RecordedAtUtc,
                IsArchived = x.IsArchived
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<SlowOperationLogDto>> GetSlowOperationsAsync(bool includeArchived = false, int take = 100, CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var query = db.SlowOperationLogs.AsNoTracking();
        if (!includeArchived)
        {
            query = query.Where(x => !x.IsArchived);
        }

        return await query.OrderByDescending(x => x.RecordedAtUtc)
            .Take(Math.Clamp(take, 1, 500))
            .Select(x => new SlowOperationLogDto
            {
                Id = x.Id,
                CompanyId = x.CompanyId,
                BranchId = x.BranchId,
                CorrelationId = x.CorrelationId,
                SessionId = x.SessionId,
                Module = x.Module,
                Category = x.Category,
                Severity = x.Severity,
                OperationName = x.OperationName,
                DurationMs = x.DurationMs,
                Message = x.Message,
                MetadataJson = x.MetadataJson,
                MachineName = x.MachineName,
                UserName = x.UserName,
                RecordedAtUtc = x.RecordedAtUtc,
                IsArchived = x.IsArchived,
                ArchivedAtUtc = x.ArchivedAtUtc
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<RuntimeExceptionLogDto>> GetRuntimeExceptionsAsync(bool includeArchived = false, int take = 100, CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var query = db.RuntimeExceptionLogs.AsNoTracking();
        if (!includeArchived)
        {
            query = query.Where(x => !x.IsArchived);
        }

        return await query.OrderByDescending(x => x.RecordedAtUtc)
            .Take(Math.Clamp(take, 1, 500))
            .Select(x => new RuntimeExceptionLogDto
            {
                Id = x.Id,
                CompanyId = x.CompanyId,
                BranchId = x.BranchId,
                CorrelationId = x.CorrelationId,
                SessionId = x.SessionId,
                Module = x.Module,
                Category = x.Category,
                Severity = x.Severity,
                ExceptionType = x.ExceptionType,
                StackHash = x.StackHash,
                Message = x.Message,
                MetadataJson = x.MetadataJson,
                OccurrenceCount = x.OccurrenceCount,
                MachineName = x.MachineName,
                UserName = x.UserName,
                RecordedAtUtc = x.RecordedAtUtc,
                IsArchived = x.IsArchived,
                ArchivedAtUtc = x.ArchivedAtUtc
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<UiResponsivenessSnapshotDto>> GetUiResponsivenessSnapshotsAsync(bool includeArchived = false, int take = 100, CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var query = db.UiResponsivenessSnapshots.AsNoTracking();
        if (!includeArchived)
        {
            query = query.Where(x => !x.IsArchived);
        }

        return await query.OrderByDescending(x => x.RecordedAtUtc)
            .Take(Math.Clamp(take, 1, 500))
            .Select(x => new UiResponsivenessSnapshotDto
            {
                Id = x.Id,
                CompanyId = x.CompanyId,
                BranchId = x.BranchId,
                CorrelationId = x.CorrelationId,
                SessionId = x.SessionId,
                Module = x.Module,
                Category = x.Category,
                Severity = x.Severity,
                OperationName = x.OperationName,
                DurationMs = x.DurationMs,
                IsFrozen = x.IsFrozen,
                Message = x.Message,
                MetadataJson = x.MetadataJson,
                MachineName = x.MachineName,
                UserName = x.UserName,
                RecordedAtUtc = x.RecordedAtUtc,
                IsArchived = x.IsArchived,
                ArchivedAtUtc = x.ArchivedAtUtc
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<SystemResourceSnapshotDto>> GetSystemResourceSnapshotsAsync(bool includeArchived = false, int take = 100, CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var query = db.SystemResourceSnapshots.AsNoTracking();
        if (!includeArchived)
        {
            query = query.Where(x => !x.IsArchived);
        }

        return await query.OrderByDescending(x => x.RecordedAtUtc)
            .Take(Math.Clamp(take, 1, 500))
            .Select(x => new SystemResourceSnapshotDto
            {
                Id = x.Id,
                CompanyId = x.CompanyId,
                BranchId = x.BranchId,
                CorrelationId = x.CorrelationId,
                SessionId = x.SessionId,
                Module = x.Module,
                Category = x.Category,
                Severity = x.Severity,
                CpuUsagePercent = x.CpuUsagePercent,
                MemoryUsageBytes = x.MemoryUsageBytes,
                WorkingSetBytes = x.WorkingSetBytes,
                ThreadCount = x.ThreadCount,
                Message = x.Message,
                MetadataJson = x.MetadataJson,
                MachineName = x.MachineName,
                UserName = x.UserName,
                RecordedAtUtc = x.RecordedAtUtc,
                IsArchived = x.IsArchived,
                ArchivedAtUtc = x.ArchivedAtUtc
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<int> CleanupTelemetryAsync(DateTime utcNow, TimeSpan retention, CancellationToken cancellationToken = default)
    {
        var cutoff = utcNow.Subtract(retention > TimeSpan.Zero ? retention : TimeSpan.FromDays(30));
        var archived = 0;
        await using var scope = _scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        archived += await ArchiveAsync(db.TelemetryEvents.Where(x => !x.IsArchived && x.RecordedAtUtc <= cutoff), utcNow, cancellationToken);
        archived += await ArchiveAsync(db.SlowOperationLogs.Where(x => !x.IsArchived && x.RecordedAtUtc <= cutoff), utcNow, cancellationToken);
        archived += await ArchiveAsync(db.RuntimeExceptionLogs.Where(x => !x.IsArchived && x.RecordedAtUtc <= cutoff), utcNow, cancellationToken);
        archived += await ArchiveAsync(db.UiResponsivenessSnapshots.Where(x => !x.IsArchived && x.RecordedAtUtc <= cutoff), utcNow, cancellationToken);
        archived += await ArchiveAsync(db.SystemResourceSnapshots.Where(x => !x.IsArchived && x.RecordedAtUtc <= cutoff), utcNow, cancellationToken);
        archived += await ArchiveAsync(db.RuntimeMetricSnapshots.Where(x => !x.IsArchived && x.RecordedAtUtc <= cutoff), utcNow, cancellationToken);

        if (archived > 0)
        {
            await db.SaveChangesAsync(cancellationToken);
            await AuditAsync("TelemetryEvent", "cleanup", "Cleanup", null, new { cutoff, archived }, cancellationToken);
        }

        return archived;
    }

    public async Task FlushAsync(CancellationToken cancellationToken = default)
    {
        if (!await _flushGate.WaitAsync(0, cancellationToken))
        {
            return;
        }

        try
        {
            var batch = new List<TelemetryEnvelope>(BatchSize);
            while (_channel.Reader.TryRead(out var item))
            {
                batch.Add(item);
                if (batch.Count >= BatchSize)
                {
                    await PersistBatchAsync(batch, cancellationToken);
                    batch.Clear();
                }
            }

            if (batch.Count > 0)
            {
                await PersistBatchAsync(batch, cancellationToken);
            }
        }
        finally
        {
            _flushGate.Release();
        }
    }

    private async Task RunAsync(CancellationToken cancellationToken)
    {
        using var timer = new PeriodicTimer(FlushInterval);
        try
        {
            while (await timer.WaitForNextTickAsync(cancellationToken))
            {
                await FlushAsync(cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            await FlushAsync(CancellationToken.None);
        }
    }

    private async Task EnqueueAsync(TelemetryEnvelope envelope, CancellationToken cancellationToken)
    {
        await _channel.Writer.WriteAsync(envelope, cancellationToken);
        if (_workerTask is null)
        {
            await FlushAsync(cancellationToken);
        }
    }

    private async Task PersistBatchAsync(IReadOnlyList<TelemetryEnvelope> batch, CancellationToken cancellationToken)
    {
        if (batch.Count == 0)
        {
            return;
        }

        await using var scope = _scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        foreach (var item in batch)
        {
            db.TelemetryEvents.Add(item.Event);
            if (item.Metric is not null) db.RuntimeMetricSnapshots.Add(item.Metric);
            if (item.SlowOperation is not null) db.SlowOperationLogs.Add(item.SlowOperation);
            if (item.ExceptionLog is not null) db.RuntimeExceptionLogs.Add(item.ExceptionLog);
            if (item.UiResponsiveness is not null) db.UiResponsivenessSnapshots.Add(item.UiResponsiveness);
            if (item.Resource is not null) db.SystemResourceSnapshots.Add(item.Resource);
        }

        await db.SaveChangesAsync(cancellationToken);
        await IndexSearchEntriesAsync(batch, cancellationToken);
    }

    private static async Task<double> AverageDurationAsync(IQueryable<SlowOperationLog> slowOps, IQueryable<UiResponsivenessSnapshot> uiSnapshots, CancellationToken cancellationToken)
    {
        var slowDurations = await slowOps.Select(x => (double?)x.DurationMs).ToListAsync(cancellationToken);
        var uiDurations = await uiSnapshots.Select(x => (double?)x.DurationMs).ToListAsync(cancellationToken);
        var durations = slowDurations.Concat(uiDurations).Where(x => x.HasValue).Select(x => x!.Value).ToArray();
        return durations.Length == 0 ? 0d : durations.Average();
    }

    private static double Percentile(double[] values, double percentile)
    {
        if (values.Length == 0)
        {
            return 0d;
        }

        var ordered = values.OrderBy(x => x).ToArray();
        var rank = (percentile / 100d) * (ordered.Length - 1);
        var lower = (int)Math.Floor(rank);
        var upper = (int)Math.Ceiling(rank);
        if (lower == upper)
        {
            return ordered[lower];
        }

        var weight = rank - lower;
        return ordered[lower] * (1 - weight) + ordered[upper] * weight;
    }

    private static async Task<int> ArchiveAsync<T>(IQueryable<T> query, DateTime archivedAtUtc, CancellationToken cancellationToken) where T : class
    {
        var rows = await query.ToListAsync(cancellationToken);
        var count = 0;
        foreach (var row in rows)
        {
            switch (row)
            {
                case TelemetryEvent item:
                    item.IsArchived = true;
                    item.ArchivedAtUtc = archivedAtUtc;
                    count++;
                    break;
                case SlowOperationLog item:
                    item.IsArchived = true;
                    item.ArchivedAtUtc = archivedAtUtc;
                    count++;
                    break;
                case RuntimeExceptionLog item:
                    item.IsArchived = true;
                    item.ArchivedAtUtc = archivedAtUtc;
                    count++;
                    break;
                case UiResponsivenessSnapshot item:
                    item.IsArchived = true;
                    item.ArchivedAtUtc = archivedAtUtc;
                    count++;
                    break;
                case SystemResourceSnapshot item:
                    item.IsArchived = true;
                    item.ArchivedAtUtc = archivedAtUtc;
                    count++;
                    break;
                case RuntimeMetricSnapshot item:
                    item.IsArchived = true;
                    item.ArchivedAtUtc = archivedAtUtc;
                    count++;
                    break;
            }
        }

        return count;
    }

    private async Task AuditAsync(string entityName, string entityId, string action, object? oldValues, object? newValues, CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var auditService = scope.ServiceProvider.GetService<IAuditService>();
        if (auditService is not null)
        {
            await auditService.LogAsync(entityName, entityId, action, oldValues, newValues, "system", correlationId: _correlationService?.CurrentCorrelationId, cancellationToken: cancellationToken);
        }
    }

    private static string ComputeStackHash(Exception exception)
    {
        var payload = $"{exception.GetType().FullName}|{exception.Message}|{exception.StackTrace}";
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(payload));
        return Convert.ToHexString(bytes);
    }

    private static string? ToMetadataJson(object? metadata)
        => metadata is null ? null : JsonSerializer.Serialize(metadata, JsonOptions);

    private async Task IndexSearchEntriesAsync(IReadOnlyList<TelemetryEnvelope> batch, CancellationToken cancellationToken)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var search = scope.ServiceProvider.GetService<IEnterpriseSearchService>();
        if (search is null)
        {
            return;
        }

        foreach (var item in batch)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (item.Event is not null)
            {
                await search.UpsertAsync(new SearchIndexUpsertRequest
                {
                    EntityType = nameof(TelemetryEvent),
                    EntityId = item.Event.Id.ToString("N"),
                    Title = item.Event.EventName,
                    Subtitle = item.Event.Message,
                    Category = item.Event.Category,
                    TargetType = "module",
                    TargetRoute = "operations-center/telemetry",
                    SourceType = "telemetry",
                    MetadataJson = item.Event.MetadataJson,
                    SearchText = item.Event.SearchText,
                    BaseScore = 50
                }, cancellationToken);
            }

            if (item.ExceptionLog is not null)
            {
                await search.UpsertAsync(new SearchIndexUpsertRequest
                {
                    EntityType = nameof(RuntimeExceptionLog),
                    EntityId = item.ExceptionLog.Id.ToString("N"),
                    Title = item.ExceptionLog.ExceptionType ?? item.ExceptionLog.Category,
                    Subtitle = item.ExceptionLog.Message,
                    Category = item.ExceptionLog.Category,
                    TargetType = "module",
                    TargetRoute = "operations-center/telemetry",
                    SourceType = "telemetry",
                    MetadataJson = item.ExceptionLog.MetadataJson,
                    SearchText = string.Join(' ', item.ExceptionLog.Module, item.ExceptionLog.Category, item.ExceptionLog.ExceptionType, item.ExceptionLog.Message, item.ExceptionLog.StackHash),
                    BaseScore = 80
                }, cancellationToken);
            }

            if (item.SlowOperation is not null)
            {
                await search.UpsertAsync(new SearchIndexUpsertRequest
                {
                    EntityType = nameof(SlowOperationLog),
                    EntityId = item.SlowOperation.Id.ToString("N"),
                    Title = item.SlowOperation.OperationName,
                    Subtitle = item.SlowOperation.Message ?? item.SlowOperation.OperationName,
                    Category = item.SlowOperation.Category,
                    TargetType = "module",
                    TargetRoute = "operations-center/telemetry",
                    SourceType = "telemetry",
                    MetadataJson = item.SlowOperation.MetadataJson,
                    SearchText = string.Join(' ', item.SlowOperation.Module, item.SlowOperation.Category, item.SlowOperation.OperationName, item.SlowOperation.Message),
                    BaseScore = 40
                }, cancellationToken);
            }
        }
    }

    private static TelemetryEnvelope CreateEnvelope(string eventType, string module, string category, string severity, string eventName, string message, string? correlationId, Guid? sessionId, object? metadata, RuntimeMetricSnapshot? metric = null, SlowOperationLog? slowOperation = null, RuntimeExceptionLog? exception = null, UiResponsivenessSnapshot? uiResponsiveness = null, SystemResourceSnapshot? resource = null)
    {
        var now = DateTime.UtcNow;
        var metadataJson = ToMetadataJson(metadata);
        var searchText = string.Join(' ', new[]
        {
            eventType,
            module,
            category,
            severity,
            eventName,
            message,
            metadataJson
        }.Where(x => !string.IsNullOrWhiteSpace(x))).ToUpperInvariant();

        var telemetryEvent = new TelemetryEvent
        {
            Module = module,
            Category = category,
            Severity = severity,
            EventType = eventType,
            EventName = eventName,
            Message = message,
            MetadataJson = metadataJson,
            SearchText = searchText,
            CorrelationId = correlationId,
            SessionId = sessionId,
            RecordedAtUtc = now,
            MachineName = Environment.MachineName
        };

        return new TelemetryEnvelope(telemetryEvent, metric, slowOperation, exception, uiResponsiveness, resource);
    }

    private sealed record TelemetryEnvelope(TelemetryEvent Event, RuntimeMetricSnapshot? Metric, SlowOperationLog? SlowOperation, RuntimeExceptionLog? ExceptionLog, UiResponsivenessSnapshot? UiResponsiveness, SystemResourceSnapshot? Resource);

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _workerCts?.Cancel();
        _workerCts?.Dispose();
        _flushGate.Dispose();
    }
}
