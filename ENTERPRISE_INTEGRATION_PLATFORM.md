# ASMILE ERP — ENTERPRISE INTEGRATION PLATFORM

## Kiến trúc tích hợp cho toàn bộ hệ sinh thái ASMILE

**Phiên bản:** 1.0 · **Ngày:** 2026-07-20
**Mục tiêu:** Azure Integration Services · MuleSoft Anypoint · Apache Camel · Kong Gateway · Apigee

---

# PHẦN 1: INTEGRATION ARCHITECTURE OVERVIEW

## 1.1. Integration Hub Position

```
┌─────────────────────────────────────────────────────────────┐
│                   EXTERNAL SYSTEMS                           │
│  OTAs │ Payment Gateways │ Door Locks │ SMS │ Email │ IoT   │
│  POS │ CRM │ Accounting │ HRM │ Channel Manager │ Partners  │
└─────────────────────────────────────────────────────────────┘
                              │
┌─────────────────────────────┼───────────────────────────────┐
│              INTEGRATION HUB (THIS DOCUMENT)                 │
│  ┌──────────┐ ┌──────────┐ ┌──────────┐ ┌──────────────┐  │
│  │   API    │ │Connector │ │ Message  │ │  Webhook     │  │
│  │ Gateway  │ │Framework │ │   Bus    │ │   Engine     │  │
│  └──────────┘ └──────────┘ └──────────┘ └──────────────┘  │
│  ┌──────────┐ ┌──────────┐ ┌──────────┐ ┌──────────────┐  │
│  │ Payment  │ │  Device  │ │Notify Hub│ │ Import/Export│  │
│  │ Gateway  │ │ Gateway  │ │          │ │   Engine     │  │
│  └──────────┘ └──────────┘ └──────────┘ └──────────────┘  │
└─────────────────────────────────────────────────────────────┘
                              │
┌─────────────────────────────┼───────────────────────────────┐
│                   ASMILE BUSINESS MODULES                    │
│  Front Office │ POS │ Inventory │ Accounting │ CRM │ HR     │
└─────────────────────────────────────────────────────────────┘
```

## 1.2. Design Principles

```yaml
1. NEVER DIRECT CONNECTION: Business modules không kết nối trực tiếp
   với hệ thống bên ngoài. Mọi kết nối qua Integration Hub.

2. ADAPTER PATTERN: Mỗi đối tác có một Adapter riêng.
   Business module chỉ gọi interface chung (IPaymentGateway),
   không biết đang dùng VNPay hay MoMo.

3. RESILIENCE BY DEFAULT: Circuit breaker, retry, timeout
   trên mọi kết nối ra bên ngoài. Không external system nào
   được phép làm sập ASMILE.

4. IDEMPOTENCY: Mọi message có idempotency key.
   Có thể retry an toàn. Không double-charge, không double-booking.

5. OBSERVABLE: Mọi integration call đều được trace, log, metric.
   Dashboard hiển thị real-time status của tất cả connectors.
```

---

# PHẦN 2: API GATEWAY

## 2.1. Gateway Architecture

```csharp
public interface IApiGateway
{
    Task<ApiResponse> RouteAsync(ApiRequest request, CancellationToken ct = default);
    Task<HealthStatus> GetServiceHealthAsync(string serviceId);
}

public class ApiRequest
{
    public string Method { get; set; }           // GET, POST, PUT, DELETE
    public string Path { get; set; }             // /api/v2/reservations
    public string ApiVersion { get; set; }       // v1, v2, latest
    public Dictionary<string, string> Headers { get; set; }
    public object Body { get; set; }
    public string ClientId { get; set; }
    public string ClientSecret { get; set; }
    public string IdempotencyKey { get; set; }
}

public class ApiResponse
{
    public int StatusCode { get; set; }
    public object Body { get; set; }
    public string RequestId { get; set; }
    public TimeSpan Latency { get; set; }
    public string RoutedToService { get; set; }
}

// Gateway Pipeline:
// Request → Auth (OAuth2/JWT/API Key) → Rate Limit → Route →
//   Transform → Call Service → Cache → Response → Log/Metric
```

## 2.2. Authentication Methods

```yaml
Internal Clients (WPF App, Internal Services):
  Method: JWT Bearer Token
  Issuer: ASMILE Identity Service
  Expiry: 8 hours (configurable)
  Refresh: via refresh token

External Partners (OTAs, Payment Gateways):
  Method: OAuth2 Client Credentials
  Scope: per-partner, per-endpoint
  Rate Limit: per partner contract

Webhook Consumers (3rd party receiving our events):
  Method: HMAC-SHA256 Signature
  Secret: per webhook subscription
  Replay Protection: timestamp + nonce

Public API (future Marketplace):
  Method: API Key + Secret
  Rate Limit: per subscription tier
  Quota: daily/monthly request limits
```

## 2.3. Rate Limiting

```csharp
public interface IRateLimiter
{
    Task<bool> TryAcquireAsync(string clientId, string endpoint, CancellationToken ct);
    Task<RateLimitStatus> GetStatusAsync(string clientId);
}

public class RateLimitPolicy
{
    public string ClientId { get; set; }
    public string Endpoint { get; set; }
    public int MaxRequestsPerSecond { get; set; }
    public int MaxRequestsPerMinute { get; set; }
    public int MaxRequestsPerHour { get; set; }
    public int MaxRequestsPerDay { get; set; }

    // Example OTA policies:
    // Booking.com:  100 req/min, 10,000 req/day
    // Agoda:        50 req/min,  5,000 req/day
    // Internal WPF: 500 req/min, unlimited
}

// Rate limit response headers:
// X-RateLimit-Limit: 100
// X-RateLimit-Remaining: 87
// X-RateLimit-Reset: 1623456789
```

---

# PHẦN 3: CONNECTOR FRAMEWORK

## 3.1. Adapter Pattern

```csharp
/// <summary>
/// Every external system gets its own Adapter.
/// Business modules call the INTERFACE, never the Adapter directly.
/// Adding a new OTA or Payment Gateway = new Adapter, ZERO business code changes.
/// </summary>

// ===== CHANNEL MANAGER (OTA) CONNECTOR =====
public interface IChannelManagerConnector
{
    Task<List<OtaReservation>> FetchReservationsAsync(DateTime from, DateTime to);
    Task PushAvailabilityAsync(List<RoomAvailability> availability);
    Task PushRatesAsync(List<RoomRate> rates);
    Task PushRestrictionsAsync(List<RateRestriction> restrictions);
    Task<OtaSyncStatus> GetSyncStatusAsync();
}

// Booking.com Adapter
public class BookingComConnector : IChannelManagerConnector
{
    public async Task<List<OtaReservation>> FetchReservationsAsync(DateTime from, DateTime to)
    {
        // Call Booking.com XML API
        // Transform Booking.com XML → ASMILE OtaReservation
        // Handle pagination (Booking.com returns max 1000 per page)
        // Handle rate limits (429 → wait + retry)
    }
}

// Agoda Adapter
public class AgodaConnector : IChannelManagerConnector
{
    // Agoda uses JSON REST API, different auth, different rate limits
    // But same interface → business code unchanged
}

// ===== PAYMENT GATEWAY CONNECTOR =====
public interface IPaymentGateway
{
    Task<PaymentResult> CreatePaymentAsync(PaymentRequest request);
    Task<PaymentResult> QueryPaymentAsync(string transactionId);
    Task<RefundResult> RefundAsync(string transactionId, decimal amount, string reason);
    Task<ReconciliationReport> GetReconciliationAsync(DateTime date);
}

// VNPay Adapter
public class VNPayConnector : IPaymentGateway { /* VNPay-specific implementation */ }

// MoMo Adapter
public class MoMoConnector : IPaymentGateway { /* MoMo-specific implementation */ }

// Stripe Adapter
public class StripeConnector : IPaymentGateway { /* Stripe-specific implementation */ }

// ===== SMS GATEWAY CONNECTOR =====
public interface ISmsGateway
{
    Task<SmsResult> SendAsync(string phone, string message, string senderId = null);
    Task<SmsStatus> GetStatusAsync(string messageId);
}

public class TwilioSmsConnector : ISmsGateway { }
public class VntimeSmsConnector : ISmsGateway { }
public class EsmsConnector : ISmsGateway { }

// ===== EMAIL GATEWAY CONNECTOR =====
public interface IEmailGateway
{
    Task<EmailResult> SendAsync(EmailMessage message);
}

public class SendGridConnector : IEmailGateway { }
public class SmtpConnector : IEmailGateway { }
```

## 3.2. OTA Connector Registry

```yaml
connectors:
  - id: "booking.com"
    type: channel_manager
    protocol: XML over HTTPS
    auth: OAuth1.0a (legacy) + API Key
    endpoints:
      reservations: "https://supply-xml.booking.com/hotels/reservations"
      availability: "https://supply-xml.booking.com/hotels/availability"
      rates: "https://supply-xml.booking.com/hotels/roomrates"
    rate_limit: { rpm: 100, daily: 10000 }
    retry: { max: 3, backoff: exponential, base_ms: 1000 }

  - id: "agoda"
    type: channel_manager
    protocol: JSON REST over HTTPS
    auth: API Key + Secret (HMAC)
    endpoints:
      reservations: "https://affiliateapi.agoda.com/v3/reservations"
    rate_limit: { rpm: 50, daily: 5000 }

  - id: "expedia"
    type: channel_manager
    protocol: JSON REST
    auth: API Key + Secret
    endpoints:
      reservations: "https://services.expediapartnercentral.com/reservations"

  - id: "traveloka"
    type: channel_manager
    protocol: JSON REST
    auth: API Key
    endpoints:
      reservations: "https://api.traveloka.com/v2/partner/reservations"

  - id: "trip.com"
    type: channel_manager
    protocol: JSON REST
    auth: API Key + IP Whitelist

  - id: "airbnb"
    type: channel_manager
    protocol: JSON REST
    auth: OAuth2
    endpoints:
      reservations: "https://api.airbnb.com/v2/reservations"

  - id: "google_hotel"
    type: channel_manager
    protocol: JSON REST
    auth: OAuth2 (Google Service Account)
```

---

# PHẦN 4: MESSAGE BUS

## 4.1. Message Bus Architecture

```csharp
public interface IMessageBus
{
    // Publish
    Task PublishAsync<T>(string topic, T message, MessageOptions options = null);

    // Subscribe
    IDisposable Subscribe<T>(string topic, string subscriptionId,
        Func<T, Task> handler, MessageOptions options = null);

    // Request/Reply
    Task<TResponse> RequestAsync<TRequest, TResponse>(string queue,
        TRequest request, TimeSpan timeout);
}

public class MessageOptions
{
    public string MessageId { get; set; }
    public string CorrelationId { get; set; }
    public string IdempotencyKey { get; set; }
    public TimeSpan? TimeToLive { get; set; }    // Message expires after TTL
    public int Priority { get; set; }             // 0=normal, 9=highest
    public string ReplyTo { get; set; }
    public Dictionary<string, string> Headers { get; set; }
}

// ===== DEAD LETTER QUEUE =====
// After max retries, failed messages go to DLQ
// DLQ messages are:
// 1. Logged with full context
// 2. Alerted to operations team
// 3. Available for manual replay via admin UI
```

## 4.2. Topics & Queues

```yaml
topics:
  reservation:
    - reservation.created      ← Front Office → OTA (push)
    - reservation.modified     ← Front Office → OTA (push)
    - reservation.cancelled    ← Front Office → OTA (push)
    - reservation.imported     ← OTA → Front Office (import)

  availability:
    - availability.changed     ← Housekeeping + Check-in/out → OTA (push)

  rate:
    - rate.changed             ← Rate Engine → OTA (push)

  payment:
    - payment.created          ← Payment Engine → Accounting (post)
    - payment.refunded         ← Payment Engine → Accounting (post)
    - payment.reconciled       ← Payment Gateway → Payment Engine

  notification:
    - sms.send                 ← Any module → SMS Gateway
    - email.send               ← Any module → Email Gateway
    - push.send                ← Any module → Push Notification

  audit:
    - audit.entry              ← All modules → Audit Service

queues:
  dlq:                          # Dead Letter Queue
    - dlq.ota.failed            # Failed OTA sync
    - dlq.payment.failed        # Failed payment
    - dlq.email.failed          # Failed email
```

---

# PHẦN 5: CIRCUIT BREAKER & RESILIENCE

## 5.1. Circuit Breaker Pattern

```csharp
public enum CircuitState { Closed, Open, HalfOpen }

public interface ICircuitBreaker
{
    Task<T> ExecuteAsync<T>(Func<Task<T>> action, string serviceId);
    CircuitState GetState(string serviceId);
    Task ResetAsync(string serviceId);
}

// Circuit Breaker States:
// CLOSED (normal): Requests pass through
//   ↓ (failure threshold exceeded: 5 failures in 60s)
// OPEN (blocking): Requests fail immediately without calling external service
//   ↓ (timeout: 30s)
// HALF-OPEN (testing): Limited requests pass through to test if service recovered
//   ↓ (success) → CLOSED
//   ↓ (failure) → OPEN (reset timer)

// Configuration per external service:
public class CircuitBreakerConfig
{
    public int FailureThreshold { get; set; } = 5;      // Failures before opening
    public TimeSpan FailureWindow { get; set; } = 60s;   // Window to count failures
    public TimeSpan OpenTimeout { get; set; } = 30s;    // Time before half-open
    public int HalfOpenMaxRequests { get; set; } = 3;   // Test requests in half-open
    public TimeSpan RequestTimeout { get; set; } = 10s;  // Individual request timeout
}
```

## 5.2. Retry Policy

```csharp
public class RetryPolicy
{
    public int MaxRetries { get; set; } = 3;
    public TimeSpan InitialDelay { get; set; } = 1_000ms;
    public RetryBackoff Backoff { get; set; } = RetryBackoff.Exponential;
    public double BackoffMultiplier { get; set; } = 2.0;
    public TimeSpan MaxDelay { get; set; } = 60_000ms;

    public bool ShouldRetry(Exception ex) => ex switch
    {
        TimeoutException => true,
        HttpRequestException { StatusCode: >= 500 } => true,     // Server errors
        HttpRequestException { StatusCode: 429 } => true,        // Rate limit
        HttpRequestException { StatusCode: < 500 } => false,     // Client errors (4xx)
        _ => false
    };
}

public enum RetryBackoff { Fixed, Linear, Exponential }

// Exponential backoff example:
// Attempt 1: wait 1s → fail
// Attempt 2: wait 2s → fail
// Attempt 3: wait 4s → fail
// → Circuit Breaker opens
```

---

# PHẦN 6: PAYMENT GATEWAY HUB

## 6.1. Unified Payment Interface

```csharp
public interface IPaymentGatewayHub
{
    Task<PaymentGatewayResult> ProcessPaymentAsync(UnifiedPaymentRequest request);
    Task<PaymentGatewayResult> QueryAsync(string gatewayTransactionId);
    Task<RefundResult> RefundAsync(string gatewayTransactionId, decimal amount, string reason);
    Task<List<GatewayTransaction>> GetReconciliationAsync(DateTime date);
    IReadOnlyList<IPaymentGateway> GetAvailableGateways();
    Task<bool> IsGatewayHealthyAsync(string gatewayId);
}

public class UnifiedPaymentRequest
{
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "VND";
    public string PaymentMethod { get; set; }    // "VNPay", "MoMo", "ZaloPay", "Stripe"
    public string Description { get; set; }
    public string OrderReference { get; set; }   // ASMILE Folio/Payment ID
    public string ReturnUrl { get; set; }        // For redirect-based payments
    public string IpAddress { get; set; }
    public string Language { get; set; } = "vn";
    public Dictionary<string, string> Metadata { get; set; }
}

// Business module calls:
// await _paymentHub.ProcessPaymentAsync(new UnifiedPaymentRequest { ... });
// Payment Hub routes to VNPay/MoMo/Stripe based on PaymentMethod
// Business module doesn't know which gateway is used
```

## 6.2. Reconciliation Engine

```csharp
public interface IReconciliationEngine
{
    Task<ReconciliationResult> ReconcileAsync(DateTime date);
    Task<List<ReconciliationDiscrepancy>> GetDiscrepanciesAsync(DateTime date);
}

public class ReconciliationResult
{
    public string GatewayId { get; set; }
    public DateTime Date { get; set; }
    public int AsmileTransactionCount { get; set; }
    public int GatewayTransactionCount { get; set; }
    public int MatchedCount { get; set; }
    public List<ReconciliationDiscrepancy> Discrepancies { get; set; }

    // Discrepancy types:
    // - IN_ASMILE_NOT_GATEWAY: Transaction in ASMILE but not in gateway
    // - IN_GATEWAY_NOT_ASMILE: Transaction in gateway but not in ASMILE
    // - AMOUNT_MISMATCH: Same transaction, different amounts
    // - STATUS_MISMATCH: Same transaction, different status
}
```

---

# PHẦN 7: DEVICE GATEWAY

## 7.1. Device Abstraction

```csharp
public interface IDeviceGateway
{
    Task<DeviceResult> ExecuteAsync(string deviceId, DeviceCommand command);
    Task<DeviceStatus> GetStatusAsync(string deviceId);
    IReadOnlyList<IDeviceDriver> GetRegisteredDrivers();
}

public interface IDeviceDriver
{
    string DriverId { get; }         // "assa-abloy-door-lock"
    string DeviceType { get; }       // "DoorLock", "KeyCard", "PassportScanner"
    Task<DeviceResult> ExecuteAsync(DeviceCommand command);
    Task<bool> IsDeviceOnlineAsync(string deviceId);
}

// ===== DEVICE DRIVERS =====

// Door Lock (Assa Abloy VingCard)
public class AssaAbloyDoorLockDriver : IDeviceDriver
{
    public string DriverId => "assa-abloy-door-lock";
    public string DeviceType => "DoorLock";

    // Commands: ENCODE_KEY, REVOKE_KEY, READ_AUDIT_TRAIL, UNLOCK, LOCK
}

// Passport Scanner (Access-IS)
public class PassportScannerDriver : IDeviceDriver
{
    public string DriverId => "access-is-passport-scanner";
    public string DeviceType => "PassportScanner";

    // Reads MRZ (Machine Readable Zone) from passport/ID card
    // Returns: Name, DOB, Nationality, Document Number, Expiry
}

// Face Recognition (Hikvision)
public class FaceRecognitionDriver : IDeviceDriver
{
    public string DriverId => "hikvision-face-recognition";
    public string DeviceType => "Camera";

    // Matches face against guest photo database
    // Returns: Match score, Guest ID
}

// POS Printer (Epson TM-T88)
public class PosPrinterDriver : IDeviceDriver
{
    public string DriverId => "epson-tm-t88-printer";
    public string DeviceType => "Printer";

    // Commands: PRINT_RECEIPT, PRINT_KITCHEN_TICKET, PRINT_REGISTRATION
}
```

---

# PHẦN 8: NOTIFICATION HUB

## 8.1. Unified Notification Interface

```csharp
public interface INotificationHub
{
    Task<NotificationResult> SendAsync(UnifiedNotification notification);
    Task<NotificationStatus> GetStatusAsync(string notificationId);
}

public class UnifiedNotification
{
    public string Channel { get; set; }      // "sms", "email", "zalo", "push", "whatsapp"
    public string Recipient { get; set; }    // Phone, email, Zalo ID
    public string TemplateCode { get; set; } // "CHECKIN_CONFIRMATION", "BALANCE_REMINDER"
    public Dictionary<string, string> Variables { get; set; }
    public int Priority { get; set; }        // 0=normal, 9=urgent
    public string IdempotencyKey { get; set; }
}

// Business module calls:
// await _notificationHub.SendAsync(new UnifiedNotification {
//     Channel = "sms",
//     Recipient = "0901000001",
//     TemplateCode = "CHECKIN_CONFIRMATION",
//     Variables = { ["guest_name"] = "Nguyen Van A", ["room"] = "101" }
// });

// Notification Hub routes to Twilio/VNTime/eSMS based on Channel + cost optimization
```

## 8.2. Notification Templates

```yaml
templates:
  CHECKIN_CONFIRMATION:
    sms: "Chao mung {{guest_name}} den ASMILE! Phong {{room}} da san sang. Chuc quy khach co ky nghi thu gian."
    email: "<h2>Check-in Confirmed</h2><p>Room {{room}} is ready.</p>"

  BALANCE_REMINDER:
    sms: "Quy khach {{guest_name}}, so du tai khoan cua quy khach la {{balance}} VND. Vui long thanh toan truoc khi tra phong."

  VIP_ARRIVAL_ALERT:
    push: "VIP {{guest_name}} arriving at {{time}}. Prepare room {{room}} with VIP amenities."
    sms: "VIP Arrival Alert: {{guest_name}}, {{room}}, {{time}} - Staff please acknowledge."

  WAKE_UP_CONFIRMATION:
    sms: "Wake-up call confirmed for {{time}}. Have a great day, {{guest_name}}!"

  INVOICE_READY:
    email: "<h2>Your Invoice is Ready</h2><p>Invoice {{invoice_no}} for {{amount}} VND</p>"
```

---

# PHẦN 9: IMPORT / EXPORT ENGINE

## 9.1. Unified Interface

```csharp
public interface IImportExportEngine
{
    Task<ImportResult> ImportAsync(ImportRequest request);
    Task<ExportResult> ExportAsync(ExportRequest request);
}

public class ImportRequest
{
    public string Format { get; set; }           // "Excel", "CSV", "XML", "JSON"
    public Stream Data { get; set; }
    public string TargetEntity { get; set; }     // "Reservation", "Guest", "RoomRate"
    public ImportMode Mode { get; set; }          // ValidateOnly, Import, ImportWithPreview
    public Dictionary<string, string> ColumnMapping { get; set; }
    public string IdempotencyKey { get; set; }
}

public enum ImportMode { ValidateOnly, Import, ImportWithPreview }

public class ImportResult
{
    public int TotalRows { get; set; }
    public int SuccessRows { get; set; }
    public int ErrorRows { get; set; }
    public List<ImportError> Errors { get; set; }
    public Stream ErrorFile { get; set; }        // Original file with error column
}

public class ExportRequest
{
    public string Format { get; set; }
    public string ReportCode { get; set; }       // From Reporting Engine
    public Dictionary<string, object> Parameters { get; set; }
    public ExportDestination Destination { get; set; }
}

public enum ExportDestination { Download, Email, SaveToDisk, Print, FTP }
```

---

# PHẦN 10: WEBHOOK ENGINE

```csharp
public interface IWebhookEngine
{
    // Outbound: ASMILE → external system
    Task<string> RegisterOutboundWebhookAsync(WebhookSubscription subscription);
    Task FireOutboundAsync(string eventType, object payload);

    // Inbound: external system → ASMILE
    Task<WebhookResult> ReceiveInboundAsync(string sourceId, HttpRequest request);
}

public class WebhookSubscription
{
    public string SubscriptionId { get; set; }
    public string EventType { get; set; }        // "reservation.created"
    public string TargetUrl { get; set; }        // https://partner.com/webhook
    public string Secret { get; set; }           // For HMAC signature
    public string[] Headers { get; set; }
    public int MaxRetries { get; set; } = 5;
    public bool IsActive { get; set; }
}

// Outbound webhook delivery:
// 1. Serialize payload to JSON
// 2. Sign with HMAC-SHA256(secret, payload)
// 3. POST to TargetUrl with signature in header
// 4. If non-2xx response → retry with exponential backoff
// 5. After MaxRetries → disable webhook + alert

// Inbound webhook signature verification:
// 1. Read X-Webhook-Signature header
// 2. Compute HMAC-SHA256(secret, rawBody)
// 3. Compare signatures (constant-time comparison)
// 4. If mismatch → 401 Unauthorized
// 5. Check X-Webhook-Timestamp (reject if > 5 min old)
```

---

# PHẦN 11: FOLDER STRUCTURE

```
ASMILE.BO.Integration/
├── ASMILE.BO.Integration.Abstractions/
│   ├── IApiGateway.cs
│   ├── IMessageBus.cs
│   ├── IChannelManagerConnector.cs
│   ├── IPaymentGateway.cs
│   ├── ISmsGateway.cs
│   ├── IEmailGateway.cs
│   ├── IDeviceDriver.cs
│   ├── INotificationHub.cs
│   ├── IImportExportEngine.cs
│   ├── IWebhookEngine.cs
│   ├── ICircuitBreaker.cs
│   └── IReconciliationEngine.cs
│
├── ASMILE.BO.Integration.Gateway/
│   ├── ApiGateway.cs
│   ├── RateLimiter.cs
│   └── Auth/
│       ├── JwtAuthHandler.cs
│       ├── ApiKeyAuthHandler.cs
│       └── OAuth2Handler.cs
│
├── ASMILE.BO.Integration.Connectors/
│   ├── OTA/
│   │   ├── BookingComConnector.cs
│   │   ├── AgodaConnector.cs
│   │   └── ExpediaConnector.cs
│   ├── Payment/
│   │   ├── VNPayConnector.cs
│   │   ├── MoMoConnector.cs
│   │   └── StripeConnector.cs
│   ├── SMS/
│   │   ├── TwilioConnector.cs
│   │   └── EsmsConnector.cs
│   └── Email/
│       └── SendGridConnector.cs
│
├── ASMILE.BO.Integration.MessageBus/
│   ├── MessageBus.cs
│   ├── DeadLetterQueue.cs
│   └── RabbitMqTransport.cs (future)
│
├── ASMILE.BO.Integration.Resilience/
│   ├── CircuitBreaker.cs
│   └── RetryPolicy.cs
│
├── ASMILE.BO.Integration.Devices/
│   ├── DeviceGateway.cs
│   └── Drivers/
│       ├── AssaAbloyDoorLock.cs
│       ├── PassportScanner.cs
│       └── PosPrinter.cs
│
├── ASMILE.BO.Integration.Notifications/
│   ├── NotificationHub.cs
│   └── Templates/
│
├── ASMILE.BO.Integration.ImportExport/
│   ├── ImportEngine.cs
│   ├── ExportEngine.cs
│   └── ExcelHandler.cs
│
└── ASMILE.BO.Integration.Webhook/
    ├── WebhookEngine.cs
    └── SignatureValidator.cs
```

---

# PHẦN 12: IMPLEMENTATION ROADMAP

| Sprint | Deliverable | Duration |
|---|---|---|
| INT-001 | Integration Abstractions (all interfaces) | 2 ngày |
| INT-002 | API Gateway + Auth (JWT, API Key, OAuth2) + Rate Limiter | 3 ngày |
| INT-003 | Message Bus + Dead Letter Queue | 3 ngày |
| INT-004 | Circuit Breaker + Retry Policy + Resilience | 2 ngày |
| INT-005 | Payment Gateway Hub + VNPay connector | 3 ngày |
| INT-006 | MoMo + ZaloPay connectors | 2 ngày |
| INT-007 | Channel Manager: Booking.com connector | 3 ngày |
| INT-008 | Channel Manager: Agoda + Expedia connectors | 3 ngày |
| INT-009 | Notification Hub: SMS + Email + Zalo | 3 ngày |
| INT-010 | Device Gateway: Door Lock + Passport Scanner | 3 ngày |
| INT-011 | Import/Export Engine (Excel, CSV, JSON) | 2 ngày |
| INT-012 | Webhook Engine (inbound + outbound) | 2 ngày |
| INT-013 | Reconciliation Engine (Payment) | 2 ngày |
| INT-014 | Integration Dashboard + Monitoring | 2 ngày |
| INT-015 | Integration testing + End-to-end validation | 3 ngày |

**Total: ~38 working days (7.5 weeks)**

---

# PHẦN 13: ADRs

### ADR-INT001: Adapter Pattern over Direct Integration
**Decision:** Every external system gets an Adapter implementing a common interface. Business modules never call external APIs directly.
**Rationale:** Adding a new OTA or payment gateway must not require business code changes. Adapters encapsulate protocol differences, auth, rate limits, and retry logic.

### ADR-INT002: Idempotency Keys on All External Calls
**Decision:** Every outbound message (payment, OTA sync, SMS) carries an idempotency key. Receivers deduplicate by key.
**Rationale:** Network failures happen. Without idempotency, retries cause double-charges and double-bookings. Hotel PMS cannot tolerate duplicate financial transactions.

### ADR-INT003: Circuit Breaker on All External Dependencies
**Decision:** Every connector is wrapped in a circuit breaker. External service failures cannot cascade into ASMILE.
**Rationale:** OTAs and payment gateways can be slow or unavailable. The hotel must continue operating (check-in, check-out, folio) even if Booking.com is down.

### ADR-INT004: Message Bus for Async Integration
**Decision:** Cross-system communication (OTA sync, notifications, audit) uses async messaging. Synchronous only for payment confirmation.
**Rationale:** Async messaging decouples systems. OTA availability push doesn't block check-in. Failed messages go to DLQ for retry.

---

# PHẦN 14: SECURITY

```yaml
API Security:
  - All external endpoints require authentication
  - JWT for internal clients (8h expiry, refresh token)
  - API Key + Secret for partner integrations
  - OAuth2 Client Credentials for OTA connectors
  - mTLS for payment gateway connectors
  - All traffic over HTTPS (TLS 1.2+)

Secrets Management:
  - Development: appsettings.json (never committed)
  - Production: Windows DPAPI (single machine)
  - Cloud: Azure Key Vault / HashiCorp Vault

Data Protection:
  - Payment data: PCI-DSS compliant storage (tokenize card numbers)
  - Guest PII: encrypted at rest, masked in logs
  - API keys: never logged, never in source code
  - Webhook secrets: rotated every 90 days

Audit:
  - All API calls logged (who, what, when, result)
  - All payment transactions traced end-to-end
  - All OTA sync operations logged with status
  - Integration audit dashboard for security team
```

---

*Enterprise Integration Platform architecture complete. 14 sections. This is the single Integration Hub for all ASMILE external connectivity — OTAs, Payment Gateways, SMS, Email, Door Locks, and Partner APIs.*
