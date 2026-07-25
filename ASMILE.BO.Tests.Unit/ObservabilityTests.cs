using ASMILE.BO.Application.Platform.Observability;
using ASMILE.BO.Infrastructure.Platform.Observability;
using FluentAssertions;

namespace ASMILE.BO.Tests.Unit;

public sealed class ObservabilityTests
{
    [Fact]
    public void OBS_001_trace_span_creates_with_correct_ids()
    {
        using var tracer = new TracingService();
        var span = tracer.StartSpan("TestOperation", "Test");
        span.OperationName.Should().Be("TestOperation");
        span.TraceId.Should().NotBeNullOrEmpty();
        span.SpanId.Should().NotBeNullOrEmpty();
        span.Status.Should().Be(TraceSpanStatus.Ok);
        tracer.EndSpan(span);
    }

    [Fact]
    public void OBS_002_multiple_spans_share_trace_id()
    {
        using var tracer = new TracingService();
        var span1 = tracer.StartSpan("Op1", "Test");
        var span2 = tracer.StartSpan("Op2", "Test");
        span1.TraceId.Should().NotBe(span2.TraceId); // Different traces
        tracer.EndSpan(span1);
        tracer.EndSpan(span2);
    }

    [Fact]
    public async Task OBS_003_retrieve_trace_by_id()
    {
        using var tracer = new TracingService();
        var span = tracer.StartSpan("Op", "Cat");
        tracer.EndSpan(span);
        var trace = await tracer.GetTraceAsync(span.TraceId, default);
        trace.Should().HaveCount(1);
        trace[0].OperationName.Should().Be("Op");
    }

    [Fact]
    public void OBS_004_metrics_counter_increments()
    {
        var collector = new MetricsCollector();
        collector.Increment("test.counter", 3);
        collector.Increment("test.counter", 2);
        var metrics = collector.GetMetricsAsync("test", DateTime.UtcNow.AddMinutes(-1), default).Result;
        metrics.Should().HaveCount(2);
        metrics.Sum(m => m.Value).Should().Be(5);
    }

    [Fact]
    public void OBS_005_metrics_gauge_and_histogram()
    {
        var collector = new MetricsCollector();
        collector.Gauge("cpu.usage", 75.5);
        collector.Histogram("request.latency", 150);
        var all = collector.GetMetricsAsync(null, DateTime.UtcNow.AddMinutes(-1), default).Result;
        all.Should().Contain(m => m.Type == MetricType.Gauge);
        all.Should().Contain(m => m.Type == MetricType.Histogram);
    }

    [Fact]
    public void OBS_006_metrics_prefix_filter()
    {
        var collector = new MetricsCollector();
        collector.Increment("payment.count", 1);
        collector.Increment("reservation.count", 1);
        var paymentMetrics = collector.GetMetricsAsync("payment", DateTime.UtcNow.AddMinutes(-1), default).Result;
        paymentMetrics.Should().HaveCount(1);
        paymentMetrics[0].Name.Should().Be("payment.count");
    }

    [Fact]
    public void OBS_007_duration_measurement()
    {
        var collector = new MetricsCollector();
        using (collector.MeasureDuration("test.duration")) { /* Simulated work */ }
        var metrics = collector.GetMetricsAsync("test", DateTime.UtcNow.AddMinutes(-1), default).Result;
        metrics.Should().HaveCount(1);
        metrics[0].Type.Should().Be(MetricType.Histogram);
    }

    [Fact]
    public void OBS_008_structured_logger_categories()
    {
        var logger = new StructuredLogger();
        logger.Business("Order created", "Payment");
        logger.Technical("Connection timeout", "Database");
        logger.Security("Failed login attempt", "Auth");
        logger.Audit("User modified settings", "Settings");
        // All categories logged without error
    }

    [Fact]
    public void OBS_009_health_checks_all_pass()
    {
        var checks = new IHealthChecker[] { new OutboxHealthChecker(), new SagaHealthChecker(), new DatabaseHealthChecker() };
        foreach (var check in checks) { var result = check.CheckAsync(default).Result; result.Status.Should().Be(HealthStatus.Healthy); }
    }

    [Fact]
    public void OBS_010_alert_fire_and_retrieve()
    {
        var alerting = new AlertingService();
        alerting.RegisterAlert(PlatformAlerts.WorkflowFailed);
        var alert = new Alert("ALERT_WF_001", "Workflow Failed", AlertSeverity.Warning, "Workflow", "Workflow WF-001 failed at step 3", DateTime.UtcNow, Guid.NewGuid(), new Dictionary<string, object> { ["WorkflowId"] = "WF-001" });
        alerting.FireAsync(alert, default).Wait();

        var active = alerting.GetActiveAlertsAsync("Workflow", DateTime.UtcNow.AddMinutes(-1), default).Result;
        active.Should().HaveCount(1);
        active[0].Message.Should().Contain("WF-001");
    }

    [Fact]
    public void OBS_011_module_filter_alerts()
    {
        var alerting = new AlertingService();
        alerting.FireAsync(new Alert("A1", "Payment Failed", AlertSeverity.Critical, "Payment", "Payment declined", DateTime.UtcNow, Guid.NewGuid(), new Dictionary<string, object>()), default).Wait();
        alerting.FireAsync(new Alert("A2", "Workflow Failed", AlertSeverity.Warning, "Workflow", "Workflow error", DateTime.UtcNow, Guid.NewGuid(), new Dictionary<string, object>()), default).Wait();

        alerting.GetActiveAlertsAsync("Payment", DateTime.UtcNow.AddMinutes(-1), default).Result.Should().HaveCount(1);
        alerting.GetActiveAlertsAsync(null, DateTime.UtcNow.AddMinutes(-1), default).Result.Should().HaveCount(2);
    }

    [Fact]
    public void OBS_012_metrics_histogram_captures_distribution()
    {
        var collector = new MetricsCollector();
        for (int i = 0; i < 10; i++) collector.Histogram("request.time", i * 10);
        var metrics = collector.GetMetricsAsync("request", DateTime.UtcNow.AddMinutes(-1), default).Result;
        metrics.Should().HaveCount(10);
        metrics.Select(m => m.Value).Should().Contain([0, 10, 20, 30, 40, 50, 60, 70, 80, 90]);
    }
}
