# EP-003 ENTERPRISE OBSERVABILITY & TELEMETRY PLATFORM — ASMILE-BO

**Ngày:** 2026-07-20
**Loại:** ENTERPRISE PLATFORM
**Trạng thái:** HOÀN THÀNH ✅

---

## 1. Architecture

```
┌────────────────────────────────────────────────────────────┐
│            OBSERVABILITY & TELEMETRY PLATFORM               │
│                                                             │
│  ┌──────────┐  ┌──────────┐  ┌──────────┐  ┌────────────┐ │
│  │ Tracing  │  │ Metrics  │  │ Logging  │  │  Alerting  │ │
│  │ Service  │  │ Collector│  │(Structured)│  │  Service   │ │
│  └────┬─────┘  └────┬─────┘  └────┬─────┘  └─────┬──────┘ │
│       │             │             │               │         │
│  ┌────▼─────────────▼─────────────▼───────────────▼──────┐ │
│  │              RuntimeTelemetryService (EXISTING)        │ │
│  │       Background worker, batching, deduplication       │ │
│  └───────────────────────────────────────────────────────┘ │
│                                                             │
│  ┌──────────┐  ┌──────────┐  ┌──────────────────────────┐ │
│  │  Health  │  │Dashboard │  │     Existing Telemetry    │ │
│  │  Checks  │  │   DTOs   │  │     (RuntimeTelemetry)    │ │
│  └──────────┘  └──────────┘  └──────────────────────────┘ │
└────────────────────────────────────────────────────────────┘
```

---

## 2. Distributed Tracing

### Interface
```csharp
public interface ITracingService
{
    TraceSpan StartSpan(string operationName, string category, TraceContext? parent = null);
    void EndSpan(TraceSpan span, TraceSpanStatus status = TraceSpanStatus.Ok);
    void AddEvent(TraceSpan span, string eventName, Dictionary<string, string>? attributes = null);
    void SetTag(TraceSpan span, string key, string value);
    Task<IReadOnlyList<TraceSpan>> GetTraceAsync(string traceId, CancellationToken ct);
    TraceContext? CurrentContext { get; }
}
```

### Trace Model
```
Trace (TraceId)
  ├── Span 1 (SpanId, ParentSpanId=null)
  │     ├── Event: "ValidateRequest"
  │     └── Tag: "customer=VipGold"
  ├── Span 2 (SpanId, ParentSpanId=Span1)
  │     └── Event: "RateCalculated"
  └── Span 3 (SpanId, ParentSpanId=Span2)
        └── Tag: "result=success"
```

### Implementation
- Uses `System.Diagnostics.ActivitySource` (OpenTelemetry-compatible)
- In-memory trace store for development
- Production: plug into OpenTelemetry exporter (Jaeger, Zipkin)

---

## 3. Metrics (30+ Predefined)

### Workflow (5)
| Metric | Type | Description |
|--------|------|-------------|
| `workflow.active` | Gauge | Active workflow instances |
| `workflow.completed` | Counter | Completed workflows |
| `workflow.failed` | Counter | Failed workflows |
| `workflow.duration_ms` | Histogram | Workflow execution time |
| `workflow.retry_count` | Counter | Retry attempts |

### Saga (4)
| `saga.active` | Gauge | Active sagas |
| `saga.completed` | Counter | Completed sagas |
| `saga.compensated` | Counter | Compensated sagas |
| `saga.duration_ms` | Histogram | Saga execution time |

### Rule Engine (4)
| `rule.evaluation_count` | Counter | Rules evaluated |
| `rule.evaluation_ms` | Histogram | Rule execution time |
| `rule.hit_rate` | Gauge | Rule hit rate % |
| `rule.conflict_count` | Counter | Conflict resolutions |

### Event Bus (4)
| `event.published` | Counter | Events published |
| `event.failed` | Counter | Event failures |
| `event.retry_count` | Counter | Retry attempts |
| `event.dead_letter` | Counter | Dead lettered |

### Night Audit (4)
| `night_audit.duration_ms` | Histogram | Audit duration |
| `night_audit.steps_completed` | Counter | Steps completed |
| `night_audit.room_charges` | Counter | Room charges posted |
| `night_audit.revenue` | Gauge | Revenue posted |

### Payment (4)
| `payment.count` | Counter | Payments processed |
| `payment.amount` | Histogram | Payment amounts |
| `payment.refund_count` | Counter | Refunds processed |
| `payment.processing_ms` | Histogram | Processing time |

### Reservation (4)
| `reservation.created` | Counter | Reservations created |
| `reservation.confirmed` | Counter | Reservations confirmed |
| `reservation.cancelled` | Counter | Reservations cancelled |
| `reservation.occupancy_pct` | Gauge | Occupancy rate |

### System (4)
| `outbox.pending` | Gauge | Pending outbox messages |
| `outbox.failed` | Gauge | Failed outbox messages |
| `queue.depth` | Gauge | Queue depth |
| `worker.active` | Gauge | Active workers |

---

## 4. Structured Logging

### Categories

| Category | Purpose | Example |
|----------|---------|---------|
| `Business` | Business events | "Order created", "Payment received" |
| `Technical` | System events | "Connection timeout", "Cache miss" |
| `Security` | Security events | "Failed login", "Permission denied" |
| `Audit` | Compliance events | "Settings changed by Admin" |
| `Performance` | Performance events | "Query took 5000ms" |

### Interface
```csharp
public interface IStructuredLogger
{
    void Log(LogCategory category, string level, string message, string module, Dictionary<string, object>? context = null);
    void Business(string message, string module, Dictionary<string, object>? context = null);
    void Technical(string message, string module, Dictionary<string, object>? context = null);
    void Security(string message, string module, Dictionary<string, object>? context = null);
    void Audit(string message, string module, Dictionary<string, object>? context = null);
}
```

---

## 5. Health Monitoring

### Built-in Health Checkers

| Checker | Component | Criteria |
|---------|-----------|----------|
| `OutboxHealthChecker` | Outbox | Outbox operational |
| `SagaHealthChecker` | Saga | No active sagas with errors |
| `DatabaseHealthChecker` | Database | Database connected |

### Interface
```csharp
public interface IHealthChecker
{
    Task<HealthCheckResult> CheckAsync(CancellationToken ct);
    string ComponentName { get; }
}
```

### Status
```
Healthy → Degraded → Unhealthy → Unknown
```

---

## 6. Alerting

### Predefined Alerts (7)

| Alert | Module | Severity | Trigger |
|-------|--------|----------|---------|
| `ALERT_WF_001` | Workflow | Warning | Workflow failed |
| `ALERT_SG_001` | Saga | Critical | Saga failed |
| `ALERT_NA_001` | NightAudit | Critical | Night Audit failed |
| `ALERT_PY_001` | Payment | Critical | Payment failed |
| `ALERT_RL_001` | RuleEngine | Warning | Rule conflict |
| `ALERT_OB_001` | Outbox | Warning | Outbox backlog > 1000 |
| `ALERT_DL_001` | DeadLetter | Critical | Dead letter queue > 0 |

### Severity Levels
```
Info → Warning → Critical → Emergency
```

---

## 7. Dashboards

### System Dashboard
- Health status (Healthy/Degraded/Unhealthy)
- Key metrics (outbox pending, workflow active, sagas running)
- Active alerts
- Worker status

### Business Dashboard
- Today revenue, Occupancy %, ADR, RevPAR
- Arrivals/Departures, Rooms occupied/available
- Reservations created, Payments processed

### Performance Dashboard
- Avg rule evaluation time
- Avg workflow/saga/payment duration
- Outbox publish rate, Event processing rate
- Total traces/spans

---

## 8. Tests (12)

| Test | Description |
|------|-------------|
| OBS_001 | Trace span creates with correct IDs |
| OBS_002 | Multiple spans have different trace IDs |
| OBS_003 | Retrieve trace by ID |
| OBS_004 | Metrics counter increments |
| OBS_005 | Metrics gauge and histogram types |
| OBS_006 | Metrics prefix filtering |
| OBS_007 | Duration measurement |
| OBS_008 | Structured logger categories |
| OBS_009 | Health checks all report healthy |
| OBS_010 | Alert fire and retrieve |
| OBS_011 | Module filter for alerts |
| OBS_012 | Histogram captures distribution |

---

## 9. Files Changed

| File | Action | Lines |
|------|--------|-------|
| `Application/Platform/Observability/ObservabilityContracts.cs` | NEW — Interfaces, DTOs, 30+ metrics, 7 alerts | 190 |
| `Infrastructure/Platform/Observability/ObservabilityServices.cs` | NEW — Tracing, Metrics, Logging, Health, Alerting | 150 |
| `Tests.Unit/ObservabilityTests.cs` | NEW — 12 tests | 135 |
| `Infrastructure/DependencyInjection.cs` | EXPANDED — 9 registrations | 12 |

**Total:** 3 new files, 1 modified

---

## 10. Build & Test

| Check | Result |
|-------|--------|
| Debug Build | PASS (0 errors) |
| Observability Tests | 12/12 PASS |

---

*Enterprise Observability & Telemetry Platform complete. Distributed tracing, 30+ metrics, structured logging, health checks, 7 predefined alerts, and 3 dashboards operational. Build 0 errors, 12/12 tests pass.*
