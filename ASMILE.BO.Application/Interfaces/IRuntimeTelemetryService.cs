using ASMILE.BO.Application.DTOs;

namespace ASMILE.BO.Application.Interfaces;

public interface IRuntimeTelemetryService
{
    Task TrackMetricAsync(string module, string category, string metricName, double metricValue, string? unit = null, string? message = null, string? correlationId = null, Guid? sessionId = null, object? metadata = null, CancellationToken cancellationToken = default);
    Task TrackExceptionAsync(Exception exception, string module, string category, string? message = null, string? correlationId = null, Guid? sessionId = null, object? metadata = null, CancellationToken cancellationToken = default);
    Task TrackSlowOperationAsync(string module, string operationName, long durationMs, string severity = "Warning", string? message = null, string? correlationId = null, Guid? sessionId = null, object? metadata = null, CancellationToken cancellationToken = default);
    Task TrackUiLatencyAsync(string module, string operationName, long durationMs, bool isFrozen = false, string? message = null, string? correlationId = null, Guid? sessionId = null, object? metadata = null, CancellationToken cancellationToken = default);
    Task TrackResourceUsageAsync(string module, double? cpuUsagePercent = null, long? memoryUsageBytes = null, long? workingSetBytes = null, int? threadCount = null, string? message = null, string? correlationId = null, Guid? sessionId = null, object? metadata = null, CancellationToken cancellationToken = default);
    Task TrackCommandLatencyAsync(string module, string commandName, long durationMs, string? message = null, string? correlationId = null, Guid? sessionId = null, object? metadata = null, CancellationToken cancellationToken = default);
    Task TrackQueryLatencyAsync(string module, string queryName, long durationMs, string? message = null, string? correlationId = null, Guid? sessionId = null, object? metadata = null, CancellationToken cancellationToken = default);
    Task TrackExportDurationAsync(string module, string exportName, long durationMs, string? message = null, string? correlationId = null, Guid? sessionId = null, object? metadata = null, CancellationToken cancellationToken = default);
    Task TrackImportDurationAsync(string module, string importName, long durationMs, string? message = null, string? correlationId = null, Guid? sessionId = null, object? metadata = null, CancellationToken cancellationToken = default);
    Task TrackSchedulerExecutionMetricsAsync(string module, string workflowKey, long durationMs, string status, string? message = null, string? correlationId = null, Guid? sessionId = null, object? metadata = null, CancellationToken cancellationToken = default);
    Task TrackWorkflowMetricsAsync(string module, string workflowKey, long durationMs, string status, string? message = null, string? correlationId = null, Guid? sessionId = null, object? metadata = null, CancellationToken cancellationToken = default);
    Task TrackRecoveryMetricsAsync(string module, string recoveryName, long durationMs, string status, string? message = null, string? correlationId = null, Guid? sessionId = null, object? metadata = null, CancellationToken cancellationToken = default);
    Task TrackCacheMetricAsync(string module, string cacheName, bool hit, string? message = null, string? correlationId = null, Guid? sessionId = null, object? metadata = null, CancellationToken cancellationToken = default);
    Task TrackNavigationLatencyAsync(string module, string route, long durationMs, string? message = null, string? correlationId = null, Guid? sessionId = null, object? metadata = null, CancellationToken cancellationToken = default);
    Task TrackSearchLatencyAsync(string module, string query, long durationMs, string? message = null, string? correlationId = null, Guid? sessionId = null, object? metadata = null, CancellationToken cancellationToken = default);
    Task<RuntimeTelemetrySummaryDto> GetRuntimeSummaryAsync(DateTime? fromUtc = null, DateTime? toUtc = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RuntimeTelemetryTimelineDto>> GetTelemetryTimelineAsync(string? query = null, string? module = null, bool includeArchived = false, int take = 200, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SlowOperationLogDto>> GetSlowOperationsAsync(bool includeArchived = false, int take = 100, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RuntimeExceptionLogDto>> GetRuntimeExceptionsAsync(bool includeArchived = false, int take = 100, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<UiResponsivenessSnapshotDto>> GetUiResponsivenessSnapshotsAsync(bool includeArchived = false, int take = 100, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SystemResourceSnapshotDto>> GetSystemResourceSnapshotsAsync(bool includeArchived = false, int take = 100, CancellationToken cancellationToken = default);
    Task<int> CleanupTelemetryAsync(DateTime utcNow, TimeSpan retention, CancellationToken cancellationToken = default);
    Task FlushAsync(CancellationToken cancellationToken = default);
}
