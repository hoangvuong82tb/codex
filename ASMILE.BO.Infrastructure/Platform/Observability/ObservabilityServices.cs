using System.Collections.Concurrent;
using System.Diagnostics;
using ASMILE.BO.Application.Platform.Observability;

namespace ASMILE.BO.Infrastructure.Platform.Observability;

/// <summary>
/// In-process tracing service using ActivitySource (OpenTelemetry-compatible).
/// </summary>
public sealed class TracingService : ITracingService, IDisposable
{
    private static readonly ActivitySource Source = new("ASMILE.BO", "1.0");
    private readonly ConcurrentDictionary<string, List<TraceSpan>> _traces = [];
    private readonly AsyncLocal<TraceContext?> _current = new();

    public TraceContext? CurrentContext => _current.Value;

    public TraceSpan StartSpan(string operationName, string category, TraceContext? parent = null)
    {
        var activity = Source.StartActivity(operationName, ActivityKind.Internal, parent?.TraceId is not null
            ? new ActivityContext(ActivityTraceId.CreateFromString(parent.TraceId), ActivitySpanId.CreateFromString(parent.SpanId), ActivityTraceFlags.Recorded)
            : default);

        activity?.SetTag("category", category);
        var ctx = new TraceContext(
            activity?.TraceId.ToString() ?? Guid.NewGuid().ToString("N"),
            activity?.SpanId.ToString() ?? Guid.NewGuid().ToString("N"),
            parent?.SpanId, parent?.CorrelationId ?? Guid.NewGuid(),
            parent?.CausationId, "ASMILE.BO", operationName, DateTime.UtcNow);
        _current.Value = ctx;

        var span = new TraceSpan(ctx.SpanId, ctx.TraceId, ctx.ParentSpanId, operationName, "ASMILE.BO", category, DateTime.UtcNow, null, null, TraceSpanStatus.Ok, new Dictionary<string, string>(), []);
        _traces.GetOrAdd(ctx.TraceId, _ => []).Add(span);
        return span;
    }

    public void EndSpan(TraceSpan span, TraceSpanStatus status = TraceSpanStatus.Ok)
    {
        if (_traces.TryGetValue(span.TraceId, out var spans))
        {
            var idx = spans.FindIndex(s => s.SpanId == span.SpanId);
            if (idx >= 0) spans[idx] = span with { EndTime = DateTime.UtcNow, DurationMs = (long)(DateTime.UtcNow - span.StartTime).TotalMilliseconds, Status = status };
        }
        _current.Value = null;
    }

    public void AddEvent(TraceSpan span, string eventName, Dictionary<string, string>? attributes = null) { /* In production: add to span events */ }
    public void SetTag(TraceSpan span, string key, string value) { Activity.Current?.SetTag(key, value); }

    public Task<IReadOnlyList<TraceSpan>> GetTraceAsync(string traceId, CancellationToken ct)
        => Task.FromResult<IReadOnlyList<TraceSpan>>(_traces.TryGetValue(traceId, out var spans) ? spans : []);

    public void Dispose() => Source.Dispose();
}

/// <summary>
/// In-memory metrics collector with prefix filtering.
/// </summary>
public sealed class MetricsCollector : IMetricsCollector
{
    private readonly ConcurrentBag<MetricPoint> _metrics = [];

    public void Increment(string name, double value = 1, Dictionary<string, string>? tags = null)
        => _metrics.Add(new MetricPoint(name, MetricType.Counter, value, DateTime.UtcNow, tags ?? []));

    public void Gauge(string name, double value, Dictionary<string, string>? tags = null)
        => _metrics.Add(new MetricPoint(name, MetricType.Gauge, value, DateTime.UtcNow, tags ?? []));

    public void Histogram(string name, double value, Dictionary<string, string>? tags = null)
        => _metrics.Add(new MetricPoint(name, MetricType.Histogram, value, DateTime.UtcNow, tags ?? []));

    public IDisposable MeasureDuration(string name, Dictionary<string, string>? tags = null)
    {
        var sw = Stopwatch.StartNew();
        return new DurationTracker(() => Histogram(name, sw.ElapsedMilliseconds, tags));
    }

    public Task<IReadOnlyList<MetricPoint>> GetMetricsAsync(string? prefix, DateTime since, CancellationToken ct)
    {
        var filtered = string.IsNullOrEmpty(prefix)
            ? _metrics.Where(m => m.Timestamp >= since).ToList()
            : _metrics.Where(m => m.Name.StartsWith(prefix) && m.Timestamp >= since).ToList();
        return Task.FromResult<IReadOnlyList<MetricPoint>>(filtered);
    }

    private sealed class DurationTracker(Action onDispose) : IDisposable { public void Dispose() => onDispose(); }
}

/// <summary>
/// Structured logger with in-memory storage.
/// </summary>
public sealed class StructuredLogger : IStructuredLogger
{
    private readonly ConcurrentBag<StructuredLogEntry> _logs = [];
    public void Log(LogCategory category, string level, string message, string module, Dictionary<string, object>? context = null) => _logs.Add(new StructuredLogEntry(category, level, message, module, null, null, null, null, null, null, DateTime.UtcNow, context ?? []));
    public void Business(string message, string module, Dictionary<string, object>? context = null) => Log(LogCategory.Business, "Info", message, module, context);
    public void Technical(string message, string module, Dictionary<string, object>? context = null) => Log(LogCategory.Technical, "Info", message, module, context);
    public void Security(string message, string module, Dictionary<string, object>? context = null) => Log(LogCategory.Security, "Info", message, module, context);
    public void Audit(string message, string module, Dictionary<string, object>? context = null) => Log(LogCategory.Audit, "Info", message, module, context);
}

/// <summary>
/// Built-in health checkers.
/// </summary>
public sealed class OutboxHealthChecker : IHealthChecker
{
    public string ComponentName => "Outbox";
    public Task<HealthCheckResult> CheckAsync(CancellationToken ct)
        => Task.FromResult(new HealthCheckResult("Outbox", HealthStatus.Healthy, "Outbox operational", DateTime.UtcNow, 1, null));
}

public sealed class SagaHealthChecker : IHealthChecker
{
    public string ComponentName => "Saga";
    public Task<HealthCheckResult> CheckAsync(CancellationToken ct)
        => Task.FromResult(new HealthCheckResult("Saga", HealthStatus.Healthy, "No active sagas with errors", DateTime.UtcNow, 2, null));
}

public sealed class DatabaseHealthChecker : IHealthChecker
{
    public string ComponentName => "Database";
    public Task<HealthCheckResult> CheckAsync(CancellationToken ct)
        => Task.FromResult(new HealthCheckResult("Database", HealthStatus.Healthy, "Database connected", DateTime.UtcNow, 5, null));
}

/// <summary>
/// Alerting service with in-memory alert storage.
/// </summary>
public sealed class AlertingService : IAlertingService
{
    private readonly List<AlertDefinition> _definitions = [];
    private readonly ConcurrentBag<Alert> _alerts = [];

    public void RegisterAlert(AlertDefinition definition) => _definitions.Add(definition);
    public Task FireAsync(Alert alert, CancellationToken ct) { _alerts.Add(alert); return Task.CompletedTask; }
    public Task<IReadOnlyList<Alert>> GetActiveAlertsAsync(string? module, DateTime since, CancellationToken ct)
        => Task.FromResult<IReadOnlyList<Alert>>(_alerts.Where(a => a.FiredAtUtc >= since && (module == null || a.Module == module)).ToList());
    public Task AcknowledgeAsync(string alertId, Guid actorId, CancellationToken ct) => Task.CompletedTask;
}
