# ASMILE ERP — ENTERPRISE PLATFORM CORE

## Kiến trúc nền tảng cho toàn bộ hệ sinh thái ASMILE

**Phiên bản:** 1.0
**Ngày:** 2026-07-20
**Loại:** Platform Core Architecture
**Mục tiêu chất lượng:** Microsoft .NET Platform · SAP Kernel · Oracle Fusion Middleware · JetBrains Platform

---

# PHẦN 1: PLATFORM OVERVIEW

## 1.1. Platform Layer Diagram

```
┌─────────────────────────────────────────────────────────────┐
│                   APPLICATION LAYER                          │
│  Front Office │ Back Office │ POS │ Restaurant │ Banquet     │
│  Housekeeping │ Inventory │ Accounting │ CRM │ HR │ Mobile   │
└─────────────────────────────────────────────────────────────┘
                              │
┌─────────────────────────────┼───────────────────────────────┐
│                   PLATFORM CORE                              │
│  ┌──────────┐ ┌──────────┐ ┌──────────┐ ┌───────────────┐  │
│  │ App Host │ │   DI     │ │ Plugins  │ │ Configuration │  │
│  └──────────┘ └──────────┘ └──────────┘ └───────────────┘  │
│  ┌──────────┐ ┌──────────┐ ┌──────────┐ ┌───────────────┐  │
│  │Event Bus │ │Messaging │ │Bg Jobs   │ │    Cache      │  │
│  └──────────┘ └──────────┘ └──────────┘ └───────────────┘  │
│  ┌──────────┐ ┌──────────┐ ┌──────────┐ ┌───────────────┐  │
│  │ Logging  │ │  Audit   │ │Validation│ │ Localization  │  │
│  └──────────┘ └──────────┘ └──────────┘ └───────────────┘  │
│  ┌──────────┐ ┌──────────┐ ┌──────────┐ ┌───────────────┐  │
│  │Permission│ │  Flags   │ │  Health  │ │  Telemetry    │  │
│  └──────────┘ └──────────┘ └──────────┘ └───────────────┘  │
│  ┌──────────┐ ┌──────────┐ ┌──────────┐ ┌───────────────┐  │
│  │Exception │ │Diagnostic│ │ Offline  │ │  Recovery     │  │
│  └──────────┘ └──────────┘ └──────────┘ └───────────────┘  │
└─────────────────────────────────────────────────────────────┘
                              │
┌─────────────────────────────┼───────────────────────────────┐
│                   INFRASTRUCTURE LAYER                       │
│  EF Core │ SQL Server │ Serilog │ Redis │ RabbitMQ           │
│  File System │ Network │ DPAPI │ Certificate Store           │
└─────────────────────────────────────────────────────────────┘
```

## 1.2. Platform Dependency Graph

```
ASMILE.BO.Platform (foundation — zero external dependencies)
  │
  ├── ASMILE.BO.Platform.Abstractions     ← Interfaces ONLY
  │     ├── IApplicationHost
  │     ├── IEventBus
  │     ├── IMessageBus
  │     ├── IBackgroundJobScheduler
  │     ├── ICacheProvider
  │     ├── ILogger (Serilog abstraction)
  │     ├── IAuditService
  │     ├── IValidationEngine
  │     ├── IPermissionService
  │     ├── IFeatureFlagService
  │     ├── IHealthMonitor
  │     ├── ITelemetryService
  │     ├── IDiagnosticService
  │     └── ILocalizationService
  │
  ├── ASMILE.BO.Platform.Core             ← Default implementations
  │     ├── ApplicationHost
  │     ├── EventBus (InMemory)
  │     ├── BackgroundJobScheduler
  │     ├── MemoryCache
  │     ├── ValidationEngine
  │     ├── PermissionEngine (RBAC)
  │     ├── FeatureFlagService
  │     └── ExceptionHandler
  │
  ├── ASMILE.BO.Platform.Logging          ← Serilog integration
  ├── ASMILE.BO.Platform.Audit            ← Audit pipeline
  ├── ASMILE.BO.Platform.Configuration    ← Multi-source config
  ├── ASMILE.BO.Platform.Localization     ← .resx + DB
  ├── ASMILE.BO.Platform.Health           ← Health checks
  ├── ASMILE.BO.Platform.Telemetry        ← Metrics + tracing
  ├── ASMILE.BO.Platform.Diagnostics      ← Diagnostic tools
  └── ASMILE.BO.Platform.Offline          ← Offline/sync
```

---

# PHẦN 2: APPLICATION HOST — LIFECYCLE

## 2.1. Startup Pipeline

```
Application.Start()
  │
  ├── Phase 0: Pre-Boot (< 500ms)
  │   ├── Show splash screen
  │   ├── Load native dependencies
  │   └── Initialize crash handler
  │
  ├── Phase 1: Configuration (< 1000ms)
  │   ├── Load appsettings.json
  │   ├── Load appsettings.{Environment}.json
  │   ├── Load hotel-specific config (DB)
  │   ├── Load machine-specific config
  │   └── Validate required settings
  │
  ├── Phase 2: DI Container (< 500ms)
  │   ├── Register platform services (singletons)
  │   ├── Register module services (scoped)
  │   ├── Register UI services (transient)
  │   └── Validate DI graph
  │
  ├── Phase 3: Infrastructure (< 1000ms)
  │   ├── Initialize Serilog
  │   ├── Test database connectivity
  │   ├── Apply pending migrations (if enabled)
  │   ├── Initialize cache
  │   └── Initialize event bus
  │
  ├── Phase 4: Modules (< 2000ms)
  │   ├── Discover modules (scan assemblies)
  │   ├── Validate module dependencies
  │   ├── Load module configurations
  │   └── Initialize module registry
  │
  ├── Phase 5: UI Shell (< 1000ms)
  │   ├── Create MainWindow
  │   ├── Initialize ShellViewModel
  │   ├── Restore previous session (if enabled)
  │   └── Hide splash screen
  │
  └── Phase 6: Post-Boot (background)
      ├── Warm up caches
      ├── Health check
      ├── Telemetry ping
      └── Check for updates
```

## 2.2. Shutdown Pipeline

```
Application.Exit()
  │
  ├── Phase 0: Pre-Shutdown
  │   ├── Show "Saving..." indicator
  │   ├── Check for dirty documents
  │   └── Prompt user if unsaved changes
  │
  ├── Phase 1: Save State
  │   ├── Capture session snapshot
  │   ├── Save window positions
  │   ├── Save filter states
  │   └── Flush pending audit entries
  │
  ├── Phase 2: Module Shutdown
  │   ├── Notify all modules: OnShuttingDown()
  │   ├── Wait for background jobs to complete
  │   ├── Dispose module DI scopes
  │   └── Unload module assemblies
  │
  ├── Phase 3: Infrastructure Shutdown
  │   ├── Flush log buffers
  │   ├── Close database connections
  │   ├── Dispose cache
  │   └── Stop event bus
  │
  └── Phase 4: Process Exit
      ├── Release native resources
      └── Environment.Exit(code)
```

## 2.3. Crash Recovery

```csharp
public interface ICrashRecoveryService
{
    // Called on next startup after a crash
    Task<bool> HasPendingRecoveryAsync();
    Task<RecoveryResult> AttemptRecoveryAsync();

    // Auto-save snapshot for recovery
    Task SaveRecoverySnapshotAsync();
    TimeSpan SnapshotInterval { get; }  // Default: 30 seconds
}

public class RecoveryResult
{
    public bool Success { get; set; }
    public RecoveryAction Action { get; set; }
    public string Message { get; set; }
    public List<Guid> RestoredDocumentIds { get; set; }
    public List<Guid> LostDocumentIds { get; set; }
}

public enum RecoveryAction
{
    FullRestore,      // All documents restored
    PartialRestore,   // Some documents restored, some lost
    Discard,          // User chose to discard
    Failed            // Recovery failed
}
```

---

# PHẦN 3: DEPENDENCY INJECTION

## 3.1. DI Lifetime Scopes

```
Application Scope (singleton — entire app lifetime)
  ├── IApplicationHost
  ├── IEventBus
  ├── ICacheProvider
  ├── IPermissionService
  ├── IFeatureFlagService
  ├── IHealthMonitor
  └── ITelemetryService

Module Scope (scoped — per module instance)
  ├── IModuleHost
  ├── Module-specific services
  └── Module ViewModels

Request Scope (scoped — per operation)
  ├── AppDbContext
  ├── IUnitOfWork
  └── IAuditService (request-level)

Transient (new instance every time)
  ├── ViewModels (lightweight)
  ├── DTOs
  └── Validators
```

## 3.2. Module DI Registration

```csharp
public interface IModule
{
    string ModuleId { get; }

    // Each module registers its OWN services
    void RegisterServices(IServiceCollection services, IConfiguration configuration);

    // Called after ALL modules registered
    void OnServicesRegistered(IServiceProvider services);
}

// Example: Front Office module registration
public class FrontOfficeModule : IModule
{
    public void RegisterServices(IServiceCollection services, IConfiguration config)
    {
        // Infrastructure
        services.AddScoped<IHotelStayRepository, EfHotelStayRepository>();
        services.AddScoped<IHotelFolioRepository, EfHotelFolioRepository>();

        // Application services
        services.AddScoped<HotelStayApplicationService>();
        services.AddScoped<HotelFolioApplicationService>();
        services.AddScoped<SettlementApplicationService>();

        // CQRS handlers
        services.AddScoped<CheckInHotelStayCommandHandler>();
        services.AddScoped<CheckOutHotelStayCommandHandler>();
        services.AddScoped<CreateFolioCommandHandler>();
    }
}
```

## 3.3. Lazy Resolution & Decorators

```csharp
// Lazy: resolve only when needed (expensive services)
services.AddScoped<Lazy<IReportEngine>>(sp =>
    new Lazy<IReportEngine>(() => sp.GetRequiredService<IReportEngine>()));

// Decorator: wrap existing service
services.Decorate<IAuditService, AuditBatchingDecorator>();
// AuditBatchingDecorator wraps IAuditService, batches writes for performance

// Factory: custom creation logic
services.AddScoped<IUnitOfWork>(sp =>
{
    var db = sp.GetRequiredService<AppDbContext>();
    var audit = sp.GetRequiredService<IAuditService>();
    return new UnitOfWork(db, audit);
});
```

---

# PHẦN 4: PLUGIN SYSTEM

## 4.1. Plugin Architecture

```
┌──────────────────────────────────────────────┐
│              PLUGIN REGISTRY                  │
│  ┌────────┐ ┌────────┐ ┌────────┐ ┌────────┐ │
│  │ FO     │ │ BO     │ │ POS    │ │ Rest.  │ │
│  │Plugin  │ │Plugin  │ │Plugin  │ │Plugin  │ │
│  └────────┘ └────────┘ └────────┘ └────────┘ │
│  ┌────────┐ ┌────────┐ ┌────────┐ ┌────────┐ │
│  │Banquet │ │ House. │ │ Accnt. │ │ CRM    │ │
│  │Plugin  │ │Plugin  │ │Plugin  │ │Plugin  │ │
│  └────────┘ └────────┘ └────────┘ └────────┘ │
└──────────────────────────────────────────────┘
```

## 4.2. Plugin Manifest

```json
{
  "pluginId": "asmile.frontoffice",
  "version": "2.1.0",
  "displayName": "Front Office",
  "description": "Hotel Front Office Management",
  "author": "ASMILE Technology",
  "dependencies": {
    "asmile.platform": ">=2.0.0",
    "asmile.accounting": ">=1.5.0"
  },
  "optionalDependencies": {
    "asmile.crm": ">=1.0.0"
  },
  "permissions": [
    "FrontOffice.Reservation.View",
    "FrontOffice.CheckIn",
    "FrontOffice.Folio.View"
  ],
  "modules": [
    { "moduleId": "reservation", "route": "/front-office/reservation" },
    { "moduleId": "reception", "route": "/front-office/reception" },
    { "moduleId": "room-rack", "route": "/front-office/reception/room-rack" }
  ],
  "databaseMigrations": [
    "20260718011133_AddFrontOfficeReservation",
    "20260718092912_AddFrontOfficeHotelStayFoundation"
  ]
}
```

## 4.3. Plugin Validation

```csharp
public interface IPluginValidator
{
    Task<PluginValidationResult> ValidateAsync(IPlugin plugin);
}

public class PluginValidationResult
{
    public bool IsValid { get; set; }
    public List<PluginValidationError> Errors { get; set; }
}

public class PluginValidationError
{
    public string Code { get; set; }         // "MISSING_DEPENDENCY"
    public string Message { get; set; }      // "Requires asmile.platform >= 2.0.0"
    public PluginValidationSeverity Severity { get; set; }
}

public enum PluginValidationSeverity
{
    Error,    // Block plugin load
    Warning,  // Allow but warn
    Info      // Informational
}
```

## 4.4. Plugin Isolation

```csharp
// Each plugin has its OWN assembly load context
public class PluginLoadContext : AssemblyLoadContext
{
    private readonly AssemblyDependencyResolver _resolver;

    public PluginLoadContext(string pluginPath)
    {
        _resolver = new AssemblyDependencyResolver(pluginPath);
    }

    protected override Assembly Load(AssemblyName name)
    {
        var assemblyPath = _resolver.ResolveAssemblyToPath(name);
        return assemblyPath != null
            ? LoadFromAssemblyPath(assemblyPath)
            : null;
    }
}

// Plugin sandbox: restrict permissions
public class PluginSandbox
{
    public bool AllowFileSystemAccess { get; set; } = false;
    public bool AllowNetworkAccess { get; set; } = true;
    public bool AllowReflection { get; set; } = false;
    public string[] AllowedNamespaces { get; set; }
}
```

---

# PHẦN 5: CONFIGURATION SYSTEM

## 5.1. Configuration Sources (Priority Order)

```
1. Command-line arguments          (--Hotel:Code=SAIGON)
2. Environment variables           (ASMILE_Hotel__Code=SAIGON)
3. appsettings.{Environment}.json  (appsettings.Production.json)
4. appsettings.json                (base config)
5. Database: HotelSettings table   (per-hotel overrides)
6. Database: UserSettings table    (per-user overrides)
7. Machine.config                  (machine-specific, rarely used)
```

## 5.2. Configuration Scopes

```csharp
public enum ConfigurationScope
{
    Global,         // All hotels, all machines
    Hotel,          // Per hotel/property
    Department,     // Front Office, Housekeeping, etc.
    User,           // Per user preferences
    Machine,        // Per physical machine
    Environment     // Development, Staging, Production
}

public interface IConfigurationService
{
    T Get<T>(string key, ConfigurationScope scope = ConfigurationScope.Global);
    Task SetAsync<T>(string key, T value, ConfigurationScope scope);
    IConfigurationSection GetSection(string key);
    IChangeToken Watch(string key);  // Reactive: notify on change
    Task ReloadAsync();              // Reload from all sources
}
```

## 5.3. Configuration Validation

```csharp
public interface IConfigurationValidator
{
    Task<ConfigurationValidationResult> ValidateAsync();
}

public class ConfigurationValidationResult
{
    public bool IsValid { get; set; }
    public List<ConfigurationError> Errors { get; set; }
}

// Built-in validators:
// - ConnectionStringValidator: checks DB reachable
// - RequiredKeysValidator: checks mandatory settings exist
// - TypeValidator: checks value types (int, bool, enum)
// - RangeValidator: checks value ranges
// - DependencyValidator: checks conditional requirements
```

---

# PHẦN 6: EVENT BUS

## 6.1. Event Bus Architecture

```
Publisher                    Event Bus                    Subscriber
┌──────────┐              ┌──────────────┐              ┌──────────┐
│ CheckIn  │──Publish──→  │              │──Deliver──→  │Housekeep │
│ Handler  │              │  EVENT BUS   │              │ Listener │
└──────────┘              │              │              └──────────┘
                          │ ┌──────────┐ │
┌──────────┐              │ │ InMemory │ │              ┌──────────┐
│ Payment  │──Publish──→  │ │  Queue   │ │──Deliver──→  │ Audit    │
│ Handler  │              │ └──────────┘ │              │ Listener │
└──────────┘              │              │              └──────────┘
                          │ ┌──────────┐ │
                          │ │ RabbitMQ │ │ (future: distributed)
                          │ │  Bridge  │ │
                          │ └──────────┘ │
                          └──────────────┘
```

## 6.2. Event Types

```csharp
// Domain Event — raised by aggregates
public interface IDomainEvent
{
    Guid EventId { get; }
    DateTime OccurredAtUtc { get; }
    string EventName { get; }
}

// Integration Event — cross-module, cross-service
public interface IIntegrationEvent : IDomainEvent
{
    string SourceModule { get; }
    int Version { get; }       // Schema version for evolution
}

// UI Event — within same process, UI updates
public interface IUiEvent
{
    string EventName { get; }
}

// Sticky Event — delivered to late subscribers
public interface IStickyEvent : IDomainEvent
{
    // Late subscribers receive the last published event
    // Example: CurrentBusinessDate — new subscriber gets current value
}
```

## 6.3. Event Bus Interface

```csharp
public interface IEventBus
{
    // Publish
    Task PublishAsync<TEvent>(TEvent @event, CancellationToken ct = default)
        where TEvent : IDomainEvent;

    // Subscribe
    IDisposable Subscribe<TEvent>(Func<TEvent, Task> handler)
        where TEvent : IDomainEvent;

    // Sticky events
    Task PublishStickyAsync<TEvent>(TEvent @event, CancellationToken ct = default)
        where TEvent : IStickyEvent;

    // Async events (fire-and-forget)
    void PublishAsyncFireAndForget<TEvent>(TEvent @event)
        where TEvent : IDomainEvent;

    // Priority
    Task PublishWithPriorityAsync<TEvent>(TEvent @event, int priority,
        CancellationToken ct = default) where TEvent : IDomainEvent;
}

// Extension method for convenience
public static class EventBusExtensions
{
    public static IDisposable Subscribe<TEvent>(this IEventBus bus,
        Action<TEvent> handler) where TEvent : IDomainEvent
        => bus.Subscribe<TEvent>(e => { handler(e); return Task.CompletedTask; });
}
```

## 6.4. Event Delivery Guarantees

| Mode | Delivery | Ordering | Use Case |
|---|---|---|---|
| `PublishAsync` | At-least-once | FIFO | Audit, History |
| `PublishAsyncFireAndForget` | Best-effort | N/A | UI refresh, notifications |
| `PublishStickyAsync` | Last-value | N/A | Business date, current shift |

---

# PHẦN 7: BACKGROUND JOBS

## 7.1. Job Scheduler

```csharp
public interface IBackgroundJobScheduler
{
    // Enqueue
    Task<string> EnqueueAsync<TJob>(TJob job, JobOptions options = null)
        where TJob : IBackgroundJob;

    // Schedule (delayed)
    Task<string> ScheduleAsync<TJob>(TJob job, TimeSpan delay,
        JobOptions options = null) where TJob : IBackgroundJob;

    // Recurring
    Task ScheduleRecurringAsync<TJob>(string jobId, TJob job,
        string cronExpression, JobOptions options = null)
        where TJob : IRecurringJob;

    // Management
    Task CancelAsync(string jobId);
    Task<JobStatus> GetStatusAsync(string jobId);
    Task RetryAsync(string jobId);
}

public class JobOptions
{
    public int MaxRetries { get; set; } = 3;
    public TimeSpan RetryDelay { get; set; } = TimeSpan.FromMinutes(1);
    public bool RetryOnFailure { get; set; } = true;
    public int Priority { get; set; } = 0;           // Higher = first
    public TimeSpan? Timeout { get; set; }
    public bool TrackProgress { get; set; } = false;
    public string Queue { get; set; } = "default";   // Queue name for dedicated workers
}
```

## 7.2. Recurring Job Examples

```csharp
// Night Audit — daily at 2:00 AM
scheduler.ScheduleRecurringAsync("night-audit", new NightAuditJob(),
    "0 2 * * *", new JobOptions { MaxRetries = 0, Priority = 999 });

// Room charge posting — every hour
scheduler.ScheduleRecurringAsync("room-charge", new PostRoomChargeJob(),
    "0 * * * *");

// Health check — every 5 minutes
scheduler.ScheduleRecurringAsync("health-check", new HealthCheckJob(),
    "*/5 * * * *", new JobOptions { RetryOnFailure = false });

// Session auto-save — every 30 seconds
scheduler.ScheduleRecurringAsync("session-save", new SessionSaveJob(),
    null, new JobOptions { /* interval-based, not cron */ });
```

## 7.3. Job Retry Strategy

```
Job Failed
  │
  ├── Retry 1 (after 1 min) ──→ Failed
  │   └── Retry 2 (after 5 min) ──→ Failed
  │       └── Retry 3 (after 15 min) ──→ Failed
  │           └── Move to Dead Letter Queue
  │               └── Manual review required
  │
  └── Transient errors (network, deadlock) → retry
     Permanent errors (validation, not found) → no retry, dead letter immediately
```

---

# PHẦN 8: CACHING

## 8.1. Cache Architecture

```
Request
  │
  ├── L1: Memory Cache (fastest, local)
  │   ├── TTL: 30s - 5min
  │   ├── Max size: 500MB
  │   └── Eviction: LRU
  │
  ├── L2: Distributed Cache (Redis — future)
  │   ├── TTL: 5min - 1hr
  │   ├── Shared across app instances
  │   └── Fallback: memory cache if Redis unavailable
  │
  └── L3: Database (source of truth)
```

## 8.2. Cache Provider Interface

```csharp
public interface ICacheProvider
{
    Task<T> GetOrCreateAsync<T>(string key, Func<Task<T>> factory,
        CacheOptions options = null);
    Task SetAsync<T>(string key, T value, CacheOptions options = null);
    Task RemoveAsync(string key);
    Task RemoveByPrefixAsync(string prefix);
    Task ClearAsync();
    Task<bool> ExistsAsync(string key);
}

public class CacheOptions
{
    public TimeSpan? AbsoluteExpiration { get; set; }
    public TimeSpan? SlidingExpiration { get; set; }
    public CachePriority Priority { get; set; } = CachePriority.Normal;
    public string[] Tags { get; set; }     // Invalidate by tag
}

public enum CachePriority
{
    Low,         // Evict first
    Normal,
    High,        // Keep as long as possible
    NeverEvict   // Critical data
}
```

## 8.3. Cache Categories

| Category | TTL | Priority | Example |
|---|---|---|
| Lookup | 5 min | High | Room types, rate plans |
| Session | Session lifetime | Normal | User preferences, filters |
| Reference | 30 min | Normal | Country list, currency |
| Query Result | 30 sec | Low | Dashboard KPIs, reports |
| Image | 1 hour | Normal | Guest photos, room images |
| Permission | 5 min | High | User permission tree |

---

# PHẦN 9: LOGGING

## 9.1. Structured Logging

```csharp
public interface IPlatformLogger
{
    // Standard levels
    void Trace(string message, [CallerMemberName] string caller = "");
    void Debug(string message, [CallerMemberName] string caller = "");
    void Info(string message, [CallerMemberName] string caller = "");
    void Warning(string message, [CallerMemberName] string caller = "");
    void Error(string message, Exception ex = null, [CallerMemberName] string caller = "");
    void Fatal(string message, Exception ex = null, [CallerMemberName] string caller = "");

    // Structured (with context)
    IDisposable BeginScope(string module, string correlationId);
    IDisposable BeginTimedOperation(string operationName);
}

// Usage with Serilog
logger.Info("Guest {GuestName} checked into Room {RoomNumber}",
    "Nguyễn Văn A", "101");

// Output:
// [2026-07-20 08:30:15 INF] Guest "Nguyễn Văn A" checked into Room "101"
// { GuestName: "Nguyễn Văn A", RoomNumber: "101", Module: "FO",
//   CorrelationId: "abc-123", Machine: "RECEPTION-01" }
```

## 9.2. Log Destinations

```yaml
Development:
  - Console (colored)
  - Debug output
  - File: logs/asmile-dev-{Date}.log

Production:
  - File: logs/asmile-{Date}.log (rolling, 14 days retention)
  - File: logs/asmile-errors-{Date}.log (errors only, 30 days)
  - Database: ErrorLog table (critical errors only)
  - External: Seq / Elasticsearch (future)

Performance Logs:
  - File: logs/asmile-perf-{Date}.log
  - Logged: operations > 200ms
  - Format: {Operation} took {Duration}ms | {Module} | {CorrelationId}
```

## 9.3. Performance Logging

```csharp
// Automatic performance logging via attribute
[LogPerformance(WarningThresholdMs = 200)]
public async Task<Result<HotelStayDto>> CheckInAsync(CheckInHotelStayCommand cmd, CancellationToken ct)
{
    // Method automatically logs:
    // [PERF] CheckInAsync took 145ms | Module: FO | CorrelationId: guid
}

// Manual timing
using (logger.BeginTimedOperation("LoadRoomRack"))
{
    var rooms = await queryService.GetRoomRackAsync(query, ct);
}
// Logs: [PERF] LoadRoomRack took 320ms WARNING | Module: FO
```

---

# PHẦN 10: AUDIT

## 10.1. Audit Pipeline

```
Business Operation
  │
  ▼
AuditInterceptor (automatic — no manual audit code)
  │
  ├── Capture: who, what, when, old value, new value
  ├── Filter: exclude PII (phone, email, passport)
  ├── Enrich: add module, machine, session, correlation
  └── Persist: batch write or immediate write
```

## 10.2. Audit Levels

```csharp
public enum AuditLevel
{
    None,        // No audit
    Basic,       // Who, what, when
    Detailed,    // + Old values, new values
    Full         // + Full entity snapshot
}

// Configure per module, per entity type
[AuditConfiguration(AuditLevel.Full)]
public class HotelPayment { }

[AuditConfiguration(AuditLevel.Basic, ExcludeProperties = new[] { "GuestPhone", "GuestEmail" })]
public class HotelStay { }
```

## 10.3. Audit Storage

```csharp
public interface IAuditService
{
    // Automatic (via interceptor)
    Task RecordAsync(string entityType, Guid entityId, string action,
        string oldValue, string newValue, string performedBy);

    // Manual (for operations that bypass ORM)
    Task RecordCustomAsync(AuditEntry entry);

    // Query
    Task<IReadOnlyList<AuditEntry>> GetEntityHistoryAsync(
        string entityType, Guid entityId, int maxResults = 50);

    Task<IReadOnlyList<AuditEntry>> GetUserActivityAsync(
        string userId, DateTime from, DateTime to);

    // Batching
    Task FlushAsync();  // Write pending batch
}

// Audit entries are immutable — NO UPDATE, NO DELETE
public class AuditEntry
{
    public Guid AuditId { get; set; }
    public string Module { get; set; }
    public string EntityType { get; set; }
    public Guid EntityId { get; set; }
    public string Action { get; set; }
    public string OldValue { get; set; }      // JSON, PII filtered
    public string NewValue { get; set; }      // JSON, PII filtered
    public string PerformedBy { get; set; }
    public DateTime OccurredAtUtc { get; set; }
    public string CorrelationId { get; set; }
    public string MachineName { get; set; }
    public string SessionId { get; set; }
}
```

---

# PHẦN 11: VALIDATION

## 11.1. Validation Engine

```csharp
public interface IValidationEngine
{
    Task<ValidationResult> ValidateAsync<T>(T entity, ValidationContext context = null);
    void RegisterValidator<T>(IValidator<T> validator);
}

public class ValidationResult
{
    public bool IsValid { get; set; }
    public List<ValidationError> Errors { get; set; }
}

public class ValidationError
{
    public string Property { get; set; }     // "GuestName"
    public string Code { get; set; }         // "REQUIRED"
    public string Message { get; set; }      // "Tên khách không được để trống"
    public ValidationSeverity Severity { get; set; }
}

public enum ValidationSeverity
{
    Error,     // Block operation
    Warning,   // Allow but warn
    Info       // Informational
}

public class ValidationContext
{
    public string Module { get; set; }
    public string Operation { get; set; }    // "Create", "Update", "CheckIn"
    public IServiceProvider Services { get; set; }  // For DB lookups
}
```

## 11.2. Validation Rules (Built-in)

```csharp
// Built-in validators
public static class ValidationRules
{
    // Required
    public static IValidator<T> Required<T>(Expression<Func<T, object>> property);

    // String length
    public static IValidator<T> MaxLength<T>(Expression<Func<T, string>> property, int maxLength);

    // Range
    public static IValidator<T> Range<T>(Expression<Func<T, IComparable>> property,
        IComparable min, IComparable max);

    // Pattern (regex)
    public static IValidator<T> Pattern<T>(Expression<Func<T, string>> property,
        string pattern, string message);

    // Custom
    public static IValidator<T> Custom<T>(Func<T, ValidationContext, Task<ValidationError>> rule);

    // Conditional
    public static IValidator<T> When<T>(Func<T, bool> condition, params IValidator<T>[] validators);

    // Cross-property
    public static IValidator<T> CrossProperty<T>(Func<T, bool> condition, string message);
}

// Example: Reservation validation
public class ReservationValidator : IValidator<Reservation>
{
    public IEnumerable<IValidator<Reservation>> Rules() => new[]
    {
        Required<Reservation>(r => r.GuestNameSnapshot)
            .WithMessage("Tên khách không được để trống"),
        Required<Reservation>(r => r.ArrivalDate)
            .WithMessage("Ngày đến không được để trống"),
        Range<Reservation>(r => r.AdultCount, 1, 10)
            .WithMessage("Số người lớn phải từ 1 đến 10"),
        CrossProperty<Reservation>(
            r => r.DepartureDate > r.ArrivalDate,
            "Ngày đi phải sau ngày đến")
    };
}
```

---

# PHẦN 12: PERMISSION ENGINE (RBAC)

## 12.1. Permission Model

```
Role (e.g., "Receptionist")
  ├── PermissionGroup: "FrontOffice"
  │     ├── "FrontOffice.Reservation.View"
  │     ├── "FrontOffice.CheckIn"
  │     ├── "FrontOffice.CheckOut"
  │     └── "FrontOffice.Folio.View"
  ├── PermissionGroup: "Housekeeping"
  │     ├── "Housekeeping.Task.View"
  │     └── "Housekeeping.Task.Assign"
  └── DataScope: Hotel = "SAIGON" (data-level restriction)
```

## 12.2. Permission Service

```csharp
public interface IPermissionService
{
    // Basic check
    Task<bool> HasPermissionAsync(string permissionCode);

    // Multiple permissions (AND)
    Task<bool> HasAllPermissionsAsync(params string[] permissionCodes);

    // Multiple permissions (OR)
    Task<bool> HasAnyPermissionAsync(params string[] permissionCodes);

    // Data scope
    Task<bool> IsInDataScopeAsync(string entityType, Guid entityId);

    // Role management
    Task AssignRoleAsync(Guid userId, string roleName);
    Task RevokeRoleAsync(Guid userId, string roleName);
    Task<IReadOnlyList<string>> GetUserPermissionsAsync(Guid userId);

    // Cache
    Task InvalidateCacheAsync(Guid userId);
    TimeSpan CacheTtl { get; set; }  // Default: 5 minutes
}

// Permission check is cached per user for 5 minutes
// Invalidation: on role change, permission change, user login/logout
```

## 12.3. Permission Tree

```
FrontOffice.*
  ├── FrontOffice.Reservation.*
  │     ├── FrontOffice.Reservation.View
  │     ├── FrontOffice.Reservation.Create
  │     ├── FrontOffice.Reservation.Update
  │     ├── FrontOffice.Reservation.Cancel
  │     └── FrontOffice.Reservation.AssignRoom
  ├── FrontOffice.CheckIn
  ├── FrontOffice.CheckOut
  ├── FrontOffice.Folio.*
  │     ├── FrontOffice.Folio.View
  │     ├── FrontOffice.Folio.Create
  │     └── FrontOffice.Folio.Close
  ├── FrontOffice.Payment.*
  ├── FrontOffice.NightAudit.*
  └── FrontOffice.Reporting.*

Housekeeping.*
  ├── Housekeeping.Task.*
  ├── Housekeeping.Inspect.*
  └── Housekeeping.Room.*

Manager.* (includes all of the above)
  └── Manager.Override.*
        ├── Manager.Override.Rate
        ├── Manager.Override.CheckIn
        └── Manager.Override.FolioClose
```

---

# PHẦN 13: FEATURE FLAGS

## 13.1. Feature Flag Service

```csharp
public interface IFeatureFlagService
{
    Task<bool> IsEnabledAsync(string featureKey, Guid? userId = null);
    Task SetEnabledAsync(string featureKey, bool enabled, Guid? targetUserId = null);

    // Targeting
    Task<bool> IsEnabledForHotelAsync(string featureKey, Guid hotelId);
    Task<bool> IsEnabledForDepartmentAsync(string featureKey, string department);

    // Percentage rollout
    Task<bool> IsInRolloutPercentageAsync(string featureKey, Guid userId);

    // A/B testing
    Task<string> GetVariantAsync(string experimentKey, Guid userId);
}

// Feature flag keys
public static class FeatureFlags
{
    public const string NightAudit = "FrontOffice.NightAudit";
    public const string WalkInCheckIn = "FrontOffice.WalkInCheckIn";
    public const string AutoRoomAssignment = "FrontOffice.AutoRoomAssignment";
    public const string DarkMode = "UI.DarkMode";
    public const string OfflineMode = "Platform.OfflineMode";
    public const string NewCheckInFlow = "FrontOffice.NewCheckInFlow";  // A/B test
}
```

## 13.2. Feature Flag States

```
Disabled     → Feature hidden, code not executed
Enabled      → Feature visible, fully functional
Preview      → Feature visible with "Preview" badge
Experimental → Feature visible with "Experimental" badge, may break
Graduated    → Feature is permanent, flag can be removed
Deprecated   → Feature will be removed in next version
```

## 13.3. Usage in Code

```csharp
// Guard pattern
if (!await featureFlags.IsEnabledAsync(FeatureFlags.WalkInCheckIn))
    return Result.Failure("WALK_IN_NOT_AVAILABLE");

// UI visibility
bool showWalkInButton = await featureFlags.IsEnabledAsync(FeatureFlags.WalkInCheckIn);

// A/B testing
var variant = await featureFlags.GetVariantAsync(FeatureFlags.NewCheckInFlow, userId);
if (variant == "new-flow")
    return new NewCheckInView();  // 50% of users
else
    return new ClassicCheckInView();  // 50% of users
```

---

# PHẦN 14: HEALTH MONITORING

## 14.1. Health Check Registry

```csharp
public interface IHealthMonitor
{
    Task<HealthReport> GetHealthReportAsync();
    Task<HealthCheckResult> CheckAsync(string checkName);
    IReadOnlyList<HealthCheckResult> LastResults { get; }
    event EventHandler<HealthChangedEventArgs> HealthChanged;
}

public class HealthReport
{
    public HealthStatus OverallStatus { get; set; }
    public Dictionary<string, HealthCheckResult> Checks { get; set; }
    public DateTime CheckedAtUtc { get; set; }
}

public enum HealthStatus { Healthy, Degraded, Unhealthy }

public class HealthCheckResult
{
    public string Name { get; set; }
    public HealthStatus Status { get; set; }
    public string Description { get; set; }
    public TimeSpan Duration { get; set; }
    public Exception Error { get; set; }
    public Dictionary<string, object> Data { get; set; }
}
```

## 14.2. Built-in Health Checks

```csharp
// Registered automatically
services.AddHealthCheck<DatabaseHealthCheck>("database");
services.AddHealthCheck<MigrationHealthCheck>("migrations");
services.AddHealthCheck<CacheHealthCheck>("cache");
services.AddHealthCheck<DiskSpaceHealthCheck>("disk-space");
services.AddHealthCheck<MemoryHealthCheck>("memory");
services.AddHealthCheck<EventBusHealthCheck>("event-bus");
services.AddHealthCheck<BackgroundJobHealthCheck>("background-jobs");

// Module-specific health checks (registered by each module)
services.AddHealthCheck<FrontOfficeHealthCheck>("front-office");
services.AddHealthCheck<NightAuditHealthCheck>("night-audit");
```

## 14.3. Health Report Example

```json
{
  "overallStatus": "Healthy",
  "checkedAt": "2026-07-20T08:30:00Z",
  "checks": {
    "database": { "status": "Healthy", "duration": "45ms",
      "data": { "server": "HOANGVUONGPC\\SQLEXPRESS", "database": "ASMILE_BO" } },
    "migrations": { "status": "Healthy", "duration": "12ms",
      "data": { "applied": 99, "pending": 0 } },
    "cache": { "status": "Healthy", "duration": "2ms",
      "data": { "items": 234, "size": "45MB" } },
    "disk-space": { "status": "Healthy", "duration": "1ms",
      "data": { "free": "120GB", "total": "500GB" } },
    "memory": { "status": "Healthy", "duration": "1ms",
      "data": { "used": "420MB", "limit": "800MB" } }
  }
}
```

---

# PHẦN 15: TELEMETRY

## 15.1. Telemetry Service

```csharp
public interface ITelemetryService
{
    // Counters
    void IncrementCounter(string name, int value = 1);
    void RecordGauge(string name, double value);

    // Timing
    IDisposable BeginTimedOperation(string name);
    void RecordDuration(string name, TimeSpan duration);

    // Events
    void TrackEvent(string name, Dictionary<string, string> properties = null);

    // Errors
    void TrackException(Exception ex, Dictionary<string, string> properties = null);

    // Module usage
    void TrackModuleOpen(string moduleId);
    void TrackModuleClose(string moduleId, TimeSpan duration);
    void TrackFeatureUsage(string featureKey);

    // Export
    Task<TelemetrySnapshot> GetSnapshotAsync();
}
```

## 15.2. Telemetry Metrics (Auto-Collected)

```yaml
Performance:
  - Module open duration (histogram)
  - Database query duration (histogram)
  - API call duration (histogram)
  - UI render duration (histogram)
  - Memory usage (gauge)
  - CPU usage (gauge)
  - GC collections (counter)

Business:
  - Check-ins per hour (counter)
  - Check-outs per hour (counter)
  - Payments received (counter)
  - Reservations created (counter)
  - Active guests (gauge)
  - Occupancy rate (gauge)

System:
  - Active sessions (gauge)
  - Open documents (gauge)
  - Cache hit ratio (gauge)
  - Event bus queue depth (gauge)
  - Background job queue depth (gauge)
  - Error rate (counter)
```

## 15.3. Slow Operation Detection

```csharp
// Automatic detection via interceptor
[DetectSlowOperation(WarningThresholdMs = 200, CriticalThresholdMs = 1000)]
public async Task<Result<HotelStayDto>> CheckInAsync(...)
{
    // If method takes > 200ms: log warning + telemetry
    // If method takes > 1000ms: log error + telemetry + alert
}
```

---

# PHẦN 16: DIAGNOSTICS

## 16.1. Diagnostic Console (Ctrl+Shift+D)

```
┌─ Diagnostic Console ──────────────────────────┐
│ ▸ memory                                      │
├───────────────────────────────────────────────┤
│ Memory: 420MB / 800MB (52%)                   │
│ GC Gen 0: 145 | Gen 1: 23 | Gen 2: 5         │
│ Cache Items: 234 | Cache Size: 45MB           │
│ Open Documents: 3                             │
│ Event Bus Queue: 0                            │
│ Background Jobs: 2 running, 15 queued         │
│ Active DB Connections: 5 (pool: 20)           │
└───────────────────────────────────────────────┘
```

## 16.2. Diagnostic Commands

```csharp
public interface IDiagnosticService
{
    // Memory
    MemorySnapshot GetMemorySnapshot();
    Task ForceGarbageCollectionAsync();

    // Cache
    CacheDiagnostic GetCacheDiagnostic();
    Task ClearCacheAsync(string category = null);

    // Database
    DbDiagnostic GetDatabaseDiagnostic();
    Task<DbQueryStats> GetSlowQueriesAsync(int topN = 10);

    // Threads
    ThreadDiagnostic GetThreadDiagnostic();

    // Performance overlay (FPS counter)
    void TogglePerformanceOverlay();
    bool IsPerformanceOverlayEnabled { get; }
}
```

---

# PHẦN 17: EXCEPTION HANDLING

## 17.1. Exception Strategy

```
Exception Thrown
  │
  ├── Is Recoverable?
  │   ├── Yes: retry with backoff (max 3 times)
  │   │   └── Still failing → escalate to user
  │   └── No: escalate immediately
  │
  ├── Is User-Facing?
  │   ├── Yes: show friendly message (Vietnamese)
  │   │   └── Example: "Không thể kết nối database. Vui lòng thử lại."
  │   └── No: log and suppress
  │
  ├── Severity
  │   ├── Fatal: terminate application (unrecoverable)
  │   ├── Error: log + notify + continue
  │   ├── Warning: log + continue
  │   └── Info: log only
  │
  └── Telemetry
      └── TrackException with stack trace, module, correlation ID
```

## 17.2. Exception Categories

```csharp
public class PlatformException : Exception
{
    public string ErrorCode { get; }          // "DB_CONNECTION_FAILED"
    public ExceptionSeverity Severity { get; }
    public bool IsRecoverable { get; }
    public bool IsUserFacing { get; }
    public string UserMessage { get; }        // Vietnamese message
    public Dictionary<string, string> Context { get; } // Module, CorrelationId...
}

public enum ExceptionSeverity
{
    Fatal,       // Terminate app
    Error,       // Log + notify
    Warning,     // Log only
    Info         // Log only, informational
}

// Subclass for specific categories
public class DatabaseException : PlatformException { }
public class NetworkException : PlatformException { }
public class ValidationException : PlatformException { }
public class ConcurrencyException : PlatformException { }
public class BusinessRuleException : PlatformException { }
```

---

# PHẦN 18: LOCALIZATION

## 18.1. Localization Architecture

```csharp
public interface ILocalizationService
{
    // String translation
    string Translate(string key, CultureInfo culture = null);
    string Translate(string key, params object[] args);

    // Current culture
    CultureInfo CurrentCulture { get; set; }
    CultureInfo CurrentUICulture { get; set; }
    IReadOnlyList<CultureInfo> SupportedCultures { get; }

    // Formatting
    string FormatCurrency(decimal amount, string currencyCode = "VND");
    string FormatDate(DateTime date, DateFormat format = DateFormat.Short);
    string FormatDateTime(DateTime dt);
    string FormatNumber(decimal number);

    // Timezone
    TimeZoneInfo CurrentTimeZone { get; set; }
    DateTime ToLocalTime(DateTime utcTime);
}

public enum DateFormat { Short, Long, Full, Relative }
```

## 18.2. Resource Files

```
Resources/
├── Strings.resx            ← Default (Vietnamese)
├── Strings.en-US.resx      ← English (US)
├── Strings.zh-CN.resx      ← Chinese (Simplified)
├── Strings.ja-JP.resx      ← Japanese
├── Strings.ko-KR.resx      ← Korean
└── Strings.ru-RU.resx      ← Russian

// Usage in code
string title = localization.Translate("RoomRack.Title");          // "Sơ Đồ Phòng"
string title = localization.Translate("RoomRack.Title", "en-US"); // "Room Rack"

// With parameters
string msg = localization.Translate("CheckIn.Success",
    "Nguyễn Văn A", "101");  // "Đã nhận phòng: Nguyễn Văn A - Phòng 101"
```

---

# PHẦN 19: OFFLINE SUPPORT

## 19.1. Offline Mode

```csharp
public interface IOfflineService
{
    bool IsOnline { get; }
    event EventHandler<ConnectivityChangedEventArgs> ConnectivityChanged;

    // Queue operations for later sync
    Task EnqueueAsync(IOfflineOperation operation);
    Task<IReadOnlyList<IOfflineOperation>> GetPendingOperationsAsync();

    // Sync
    Task<SyncResult> SyncAsync();
    DateTime? LastSyncAt { get; }

    // Conflict resolution
    ConflictResolutionStrategy DefaultStrategy { get; set; }
}

public enum ConflictResolutionStrategy
{
    ServerWins,
    ClientWins,
    LastWriteWins,
    ManualMerge
}

public class SyncResult
{
    public int SyncedCount { get; set; }
    public int ConflictCount { get; set; }
    public int FailedCount { get; set; }
    public List<SyncError> Errors { get; set; }
}
```

## 19.2. Offline Capability Matrix

| Module | Offline Capability | Notes |
|---|---|---|
| Room Rack | Read-only | Cannot check-in/out offline |
| Guest Profile | Read-only | Cannot create guest offline |
| Task List | Read-write | Tasks sync when online |
| Housekeeping | Read-write | Room status sync when online |
| Lost & Found | Read-write | Item registration syncs |
| Night Audit | **Not available** | Requires live data |
| Payment | **Not available** | Requires live DB |
| Folio | **Not available** | Requires live DB |

---

# PHẦN 20: FOLDER STRUCTURE

```
ASMILE.BO.Platform/
├── ASMILE.BO.Platform.Abstractions/       ← Interfaces ONLY
│   ├── IApplicationHost.cs
│   ├── IEventBus.cs
│   ├── IBackgroundJobScheduler.cs
│   ├── ICacheProvider.cs
│   ├── IConfigurationService.cs
│   ├── IAuditService.cs
│   ├── IValidationEngine.cs
│   ├── IPermissionService.cs
│   ├── IFeatureFlagService.cs
│   ├── IHealthMonitor.cs
│   ├── ITelemetryService.cs
│   ├── IDiagnosticService.cs
│   ├── ILocalizationService.cs
│   ├── IOfflineService.cs
│   ├── IPlugin.cs
│   └── IModule.cs
│
├── ASMILE.BO.Platform.Core/              ← Default implementations
│   ├── ApplicationHost.cs
│   ├── EventBus.cs
│   ├── BackgroundJobScheduler.cs
│   ├── MemoryCacheProvider.cs
│   ├── ConfigurationService.cs
│   ├── AuditService.cs
│   ├── ValidationEngine.cs
│   ├── PermissionService.cs
│   ├── FeatureFlagService.cs
│   ├── HealthMonitor.cs
│   ├── TelemetryService.cs
│   ├── DiagnosticService.cs
│   ├── LocalizationService.cs
│   ├── OfflineService.cs
│   ├── PluginLoader.cs
│   └── ExceptionHandler.cs
│
├── ASMILE.BO.Platform.Logging/
│   └── SerilogPlatformLogger.cs
│
├── ASMILE.BO.Platform.Configuration/
│   └── MultiSourceConfigurationProvider.cs
│
├── ASMILE.BO.Platform.Caching/
│   └── RedisCacheProvider.cs (future)
│
├── ASMILE.BO.Platform.Messaging/
│   └── RabbitMqMessageBus.cs (future)
│
└── ASMILE.BO.Platform.Offline/
    └── SqliteOfflineStore.cs
```

---

# PHẦN 21: INTERFACE DESIGN SUMMARY

```
Core Platform Interfaces (20 interfaces):

1.  IApplicationHost           — App lifecycle, startup/shutdown
2.  IEventBus                  — Publish/subscribe events
3.  IMessageBus                — Cross-service messaging (future)
4.  IBackgroundJobScheduler    — Enqueue/schedule/recurring jobs
5.  ICacheProvider             — Multi-level caching
6.  IConfigurationService      — Multi-source config with scopes
7.  IPlatformLogger            — Structured logging
8.  IAuditService              — Immutable audit trail
9.  IValidationEngine          — Rule-based validation
10. IPermissionService         — RBAC with data scope
11. IFeatureFlagService        — Feature flags + A/B testing
12. IHealthMonitor             — Health checks
13. ITelemetryService          — Metrics, counters, timing
14. IDiagnosticService         — Runtime diagnostics
15. ILocalizationService       — Multi-language, formatting
16. IOfflineService            — Offline queue + sync
17. IPlugin                    — Plugin contract
18. IModule                    — Module contract
19. ICrashRecoveryService      — Crash recovery
20. IConfigurationValidator    — Config validation
```

---

# PHẦN 22: IMPLEMENTATION ROADMAP

| Sprint | Deliverable | Duration |
|---|---|---|
| PLAT-001 | Platform.Abstractions (all 20 interfaces) | 3 ngày |
| PLAT-002 | ApplicationHost + Startup/Shutdown pipeline | 3 ngày |
| PLAT-003 | Configuration system + multi-source + validation | 3 ngày |
| PLAT-004 | Logging (Serilog) + structured logging | 2 ngày |
| PLAT-005 | EventBus (InMemory) + domain events + UI events | 3 ngày |
| PLAT-006 | BackgroundJobScheduler + recurring jobs | 3 ngày |
| PLAT-007 | Cache (MemoryCache) + TTL + eviction | 2 ngày |
| PLAT-008 | Audit pipeline + interceptors + batching | 3 ngày |
| PLAT-009 | Validation engine + built-in rules | 3 ngày |
| PLAT-010 | Permission engine (RBAC) + cache | 3 ngày |
| PLAT-011 | Feature flags + A/B testing | 2 ngày |
| PLAT-012 | Health monitoring + built-in checks | 2 ngày |
| PLAT-013 | Telemetry + metrics + slow operation detection | 2 ngày |
| PLAT-014 | Diagnostics console + memory/thread viewer | 2 ngày |
| PLAT-015 | Localization (.resx + formatting) | 2 ngày |
| PLAT-016 | Exception handler + recovery strategy | 2 ngày |
| PLAT-017 | Offline support + sync queue | 3 ngày |
| PLAT-018 | Plugin system + assembly loading + validation | 3 ngày |
| PLAT-019 | Integration testing + performance testing | 3 ngày |
| PLAT-020 | Migration: all modules adopt platform services | 5 ngày |

**Total: ~51 working days (10 weeks)**

---

# PHẦN 23: ARCHITECTURE DECISION RECORDS

### ADR-P001: InMemory EventBus over RabbitMQ for V1
**Decision:** Use InMemory EventBus for V1. Add RabbitMQ bridge in V2.
**Rationale:** ASMILE is a single-process WPF application. Distributed event bus adds complexity without benefit until we have multiple services.
**Alternatives:** RabbitMQ (overkill for single-process), Kafka (enterprise, not needed).

### ADR-P002: MemoryCache over Redis for V1
**Decision:** Use System.Runtime.Caching MemoryCache. Add Redis in V2.
**Rationale:** Single-machine deployment. Redis adds deployment complexity. MemoryCache is sufficient for hotel-scale data (thousands, not millions of records).

### ADR-P003: Serilog as the logging framework
**Decision:** Use Serilog with structured logging.
**Rationale:** Mature, widely used, supports file/console/DB/Seq sinks. Already integrated into the project.

### ADR-P004: RBAC over Claims-based for V1
**Decision:** Use simple RBAC (Role-Based Access Control). Migrate to claims in V2 if needed.
**Rationale:** Hotel staff roles are well-defined (Receptionist, Manager, Housekeeper). RBAC maps cleanly. Claims add complexity without benefit at this scale.

### ADR-P005: Feature flags as DB table, not config file
**Decision:** Store feature flags in database, not appsettings.
**Rationale:** Flags can be toggled at runtime without restart. Per-hotel, per-user targeting requires DB storage.
**Alternatives:** Config file (requires restart), external service (LaunchDarkly — overkill).

### ADR-P006: Offline support via SQLite local store
**Decision:** Use SQLite for offline data store. Sync via JSON queue.
**Rationale:** WPF app needs local storage when disconnected. SQLite is embedded, zero-config, and supports the same SQL queries as SQL Server.
**Alternatives:** LiteDB (less mature), file-based (no query support).

---

# PHẦN 24: PERFORMANCE STRATEGY

```yaml
Startup Performance:
  - Splash screen: < 500ms
  - DI container build: < 500ms
  - Module discovery: < 1000ms (lazy scan)
  - First module open: < 1000ms
  - Total startup: < 3 seconds

Runtime Performance:
  - UI thread: never blocked > 16ms (60fps)
  - DB queries: < 200ms (p95)
  - Cache hit ratio: > 85%
  - Memory: < 500MB (idle), < 800MB (heavy use)
  - GC pauses: < 50ms

Optimization Techniques:
  - Lazy module loading (Assembly.Load only when needed)
  - Cache frequently-used lookup data
  - Virtualize all large lists (DataGrid, ListBox)
  - Batching for audit writes (accumulate and flush)
  - Async all I/O (DB, file, network)
  - Pooled DbContext instances (EF Core context pooling)
  - Compiled LINQ queries for hot paths
  - Image decode at display resolution (not full size)
```

---

# PHẦN 25: SECURITY STRATEGY

```yaml
Authentication:
  - Windows Authentication (integrated)
  - Username/Password (fallback)
  - PBKDF2 password hashing
  - DPAPI for secret storage

Authorization:
  - RBAC (Role-Based Access Control)
  - Permission cache (5 min TTL)
  - Data scope (hotel-level isolation)
  - Feature-level permission checks

Data Protection:
  - Connection strings: DPAPI encrypted
  - Guest PII: encrypted at rest (future)
  - Passwords: never logged
  - Audit trail: immutable, tamper-evident

Network:
  - TLS for all external connections (future)
  - Certificate validation
  - API authentication tokens (future)

Secrets Management:
  - Development: appsettings.json
  - Production: Windows DPAPI
  - Future: Azure Key Vault / HashiCorp Vault
```

---

# PHẦN 26: CODING STANDARDS (Platform)

```yaml
Interface Design:
  - All platform services defined as interfaces in .Abstractions
  - One interface per file
  - Interface names: I{ServiceName}
  - Default implementations in .Core with same name minus "I"
  - Async methods return Task/Task<T>
  - CancellationToken as last parameter (optional, default = default)

Dependency Injection:
  - Services registered via extension methods: services.AddPlatformCore()
  - Module-specific services registered in module's RegisterServices()
  - No service locator pattern (no GetRequiredService outside composition root)
  - Constructor injection only (no property injection)

Threading:
  - Platform services are thread-safe
  - Use ConcurrentDictionary for shared state
  - Use Channel<T> for producer-consumer patterns
  - Never lock for > 1ms

Error Handling:
  - Platform exceptions inherit from PlatformException
  - Always include ErrorCode (machine-readable)
  - Always include UserMessage (Vietnamese) for user-facing errors
  - Log + telemetry for all exceptions

Naming:
  - Service interfaces: I{ServiceName} (IEventBus)
  - Service implementations: {ServiceName} (EventBus)
  - Extensions: {ServiceName}Extensions (EventBusExtensions)
  - Options: {ServiceName}Options (CacheOptions)
  - Results: {ServiceName}Result (ValidationResult)
```

---

*Enterprise Platform Core architecture complete. 26 sections. This is the kernel that every ASMILE module — Front Office, Back Office, Restaurant, Banquet, POS, Inventory, Accounting, CRM, HR, Mobile, API, AI — will run on.*
