# ASMILE ERP — ENTERPRISE ANALYTICS PLATFORM

## Kiến trúc Business Intelligence cho toàn bộ hệ sinh thái ASMILE

**Phiên bản:** 1.0
**Ngày:** 2026-07-20
**Loại:** Enterprise Analytics & BI Platform Architecture
**Mục tiêu chất lượng:** Microsoft Power BI · SAP BW/4HANA · Oracle Analytics Cloud · Snowflake · Google Looker · Microsoft Fabric

---

# PHẦN 1: ANALYTICS PLATFORM OVERVIEW

## 1.1. Analytics Layer Position

```
┌─────────────────────────────────────────────────────────────┐
│                   CONSUMPTION LAYER                          │
│  WPF Dashboards │ Power BI │ Excel │ Mobile │ AI Assistant  │
│  PDF Reports │ Email Reports │ Scheduled Reports            │
└─────────────────────────────────────────────────────────────┘
                              │
┌─────────────────────────────┼───────────────────────────────┐
│                   ANALYTICS API LAYER                        │
│  REST API │ OData │ SQL Views │ Power BI Connector          │
└─────────────────────────────────────────────────────────────┘
                              │
┌─────────────────────────────┼───────────────────────────────┐
│                   ANALYTICS ENGINE LAYER  ← THIS DOCUMENT   │
│  ┌──────────┐ ┌──────────┐ ┌──────────┐ ┌───────────────┐  │
│  │Reporting │ │Dashboard │ │  Metric  │ │   Semantic    │  │
│  │ Engine   │ │ Engine   │ │  Engine  │ │    Layer      │  │
│  └──────────┘ └──────────┘ └──────────┘ └───────────────┘  │
│  ┌──────────┐ ┌──────────┐ ┌──────────┐ ┌───────────────┐  │
│  │Snapshot  │ │Historical│ │ Forecast │ │    Trend      │  │
│  │ Engine   │ │ Engine   │ │ Engine   │ │   Engine      │  │
│  └──────────┘ └──────────┘ └──────────┘ └───────────────┘  │
│  ┌──────────┐ ┌──────────┐ ┌──────────┐                   │
│  │  OLAP    │ │   AI     │ │ Material │                   │
│  │  Engine  │ │ Dataset  │ │  Views   │                   │
│  └──────────┘ └──────────┘ └──────────┘                   │
└─────────────────────────────────────────────────────────────┘
                              │
┌─────────────────────────────┼───────────────────────────────┐
│                   DATA WAREHOUSE LAYER                       │
│  Fact Tables │ Dimension Tables │ Aggregate Tables          │
└─────────────────────────────────────────────────────────────┘
                              │
┌─────────────────────────────┼───────────────────────────────┐
│                   OPERATIONAL DATA LAYER                     │
│  Front Office DB │ POS DB │ Inventory DB │ Accounting DB    │
└─────────────────────────────────────────────────────────────┘
```

## 1.2. Core Design Principles

```yaml
PRINCIPLE 1: ANALYTICS NEVER BLOCKS OPERATIONS
  Analytics queries run on dedicated warehouse, NOT operational DB.
  Zero impact on check-in, payment, or reservation performance.

PRINCIPLE 2: SNAPSHOT > LIVE QUERY
  Pre-compute metrics at known points (night audit, hourly).
  Live queries only for "since last snapshot" delta.
  This is how SAP BW and Oracle Analytics work.

PRINCIPLE 3: SEMANTIC LAYER IS THE SINGLE SOURCE OF TRUTH
  "Doanh thu hôm nay" is a semantic concept, not a SQL query.
  AI, dashboards, reports all use the same semantic definitions.
  If the formula changes, everything updates.

PRINCIPLE 4: TIME TRAVEL IS BUILT-IN
  Every snapshot is immutable and timestamped.
  "What was occupancy on July 19 at 2:00 PM?" — answerable.
  Night Audit snapshots are never overwritten.

PRINCIPLE 5: AI-READY FROM DAY ONE
  All datasets are structured for ML consumption.
  Feature engineering is documented, not hidden in code.
  Training data is versioned alongside model versions.
```

---

# PHẦN 2: DATA WAREHOUSE — STAR SCHEMA

## 2.1. Fact Tables

```sql
-- ===== CORE FACT TABLES =====

-- FactReservation: One row per reservation per snapshot
CREATE TABLE analytics.FactReservation (
    FactReservationId BIGINT IDENTITY PRIMARY KEY,
    ReservationId UNIQUEIDENTIFIER NOT NULL,
    DateKey INT NOT NULL REFERENCES analytics.DimDate(DateKey),
    PropertyKey INT NOT NULL REFERENCES analytics.DimProperty(PropertyKey),
    RoomTypeKey INT NOT NULL REFERENCES analytics.DimRoomType(RoomTypeKey),
    GuestKey INT REFERENCES analytics.DimGuest(GuestKey),
    SourceKey INT REFERENCES analytics.DimSource(SourceKey),
    MarketKey INT REFERENCES analytics.DimMarketSegment(MarketKey),
    ArrivalDate DATE NOT NULL,
    DepartureDate DATE NOT NULL,
    Nights AS DATEDIFF(DAY, ArrivalDate, DepartureDate),
    Adults INT NOT NULL,
    Children INT NOT NULL,
    TotalGuests AS Adults + Children,
    Status NVARCHAR(30) NOT NULL,
    RoomRate DECIMAL(18,2),
    TotalRevenue DECIMAL(18,2),
    DepositAmount DECIMAL(18,2),
    IsVip BIT NOT NULL DEFAULT 0,
    IsGroup BIT NOT NULL DEFAULT 0,
    BookingLeadDays INT,           -- Days between booking and arrival
    SnapshotDateTime DATETIME2 NOT NULL,
    IsCurrent BIT NOT NULL DEFAULT 1
);

-- FactStay: One row per stay per snapshot
CREATE TABLE analytics.FactStay (
    FactStayId BIGINT IDENTITY PRIMARY KEY,
    HotelStayId UNIQUEIDENTIFIER NOT NULL,
    DateKey INT NOT NULL REFERENCES analytics.DimDate(DateKey),
    PropertyKey INT NOT NULL,
    RoomKey INT NOT NULL REFERENCES analytics.DimRoom(RoomKey),
    GuestKey INT REFERENCES analytics.DimGuest(GuestKey),
    ReservationId UNIQUEIDENTIFIER NULL,
    ArrivalDate DATE NOT NULL,
    DepartureDate DATE NOT NULL,
    ActualCheckInAtUtc DATETIME2,
    ActualCheckOutAtUtc DATETIME2,
    LengthOfStay AS DATEDIFF(DAY, ArrivalDate, DepartureDate),
    Status NVARCHAR(30) NOT NULL,
    Adults INT NOT NULL,
    Children INT NOT NULL,
    IsVip BIT NOT NULL DEFAULT 0,
    SnapshotDateTime DATETIME2 NOT NULL,
    IsCurrent BIT NOT NULL DEFAULT 1
);

-- FactRevenue: One row per revenue transaction
CREATE TABLE analytics.FactRevenue (
    FactRevenueId BIGINT IDENTITY PRIMARY KEY,
    DateKey INT NOT NULL REFERENCES analytics.DimDate(DateKey),
    PropertyKey INT NOT NULL,
    RoomKey INT REFERENCES analytics.DimRoom(RoomKey),
    GuestKey INT REFERENCES analytics.DimGuest(GuestKey),
    RevenueType NVARCHAR(30) NOT NULL,  -- RoomCharge, Payment, Refund, Deposit, F&B, Laundry, Spa, Other
    Amount DECIMAL(18,2) NOT NULL,
    IsPosted BIT NOT NULL DEFAULT 1,
    TransactionDateTime DATETIME2 NOT NULL,
    SourceModule NVARCHAR(50) NOT NULL,  -- FrontOffice, POS, Banquet, Inventory
    SnapshotDateTime DATETIME2 NOT NULL
);

-- FactNightAudit: One row per completed night audit
CREATE TABLE analytics.FactNightAudit (
    FactNightAuditId BIGINT IDENTITY PRIMARY KEY,
    DateKey INT NOT NULL REFERENCES analytics.DimDate(DateKey),
    PropertyKey INT NOT NULL,
    NightAuditId UNIQUEIDENTIFIER NOT NULL,
    AuditNumber NVARCHAR(40) NOT NULL,
    StartedAtUtc DATETIME2 NOT NULL,
    CompletedAtUtc DATETIME2 NOT NULL,
    DurationMinutes INT NOT NULL,
    Status NVARCHAR(20) NOT NULL,
    RoomsSold INT NOT NULL,
    TotalRooms INT NOT NULL,
    OccupancyPct DECIMAL(9,4) NOT NULL,
    ADR DECIMAL(18,2) NOT NULL,
    RevPAR DECIMAL(18,2) NOT NULL,
    RoomRevenue DECIMAL(18,2) NOT NULL,
    OtherRevenue DECIMAL(18,2) NOT NULL,
    TotalRevenue DECIMAL(18,2) NOT NULL,
    Arrivals INT NOT NULL,
    Departures INT NOT NULL,
    NoShows INT NOT NULL,
    Cancellations INT NOT NULL,
    SnapshotDateTime DATETIME2 NOT NULL
);

-- FactPayment: One row per payment
CREATE TABLE analytics.FactPayment (
    FactPaymentId BIGINT IDENTITY PRIMARY KEY,
    DateKey INT NOT NULL REFERENCES analytics.DimDate(DateKey),
    PropertyKey INT NOT NULL,
    PaymentId UNIQUEIDENTIFIER NOT NULL,
    PaymentMethod NVARCHAR(30) NOT NULL,
    Amount DECIMAL(18,2) NOT NULL,
    Status NVARCHAR(20) NOT NULL,
    TransactionDateTime DATETIME2 NOT NULL
);

-- FactHousekeeping: One row per completed task
CREATE TABLE analytics.FactHousekeeping (
    FactHousekeepingId BIGINT IDENTITY PRIMARY KEY,
    DateKey INT NOT NULL REFERENCES analytics.DimDate(DateKey),
    PropertyKey INT NOT NULL,
    RoomKey INT REFERENCES analytics.DimRoom(RoomKey),
    TaskType NVARCHAR(30) NOT NULL,
    Priority NVARCHAR(20) NOT NULL,
    AssignedStaffId UNIQUEIDENTIFIER,
    StartedAtUtc DATETIME2,
    CompletedAtUtc DATETIME2,
    DurationMinutes AS DATEDIFF(MINUTE, StartedAtUtc, CompletedAtUtc),
    Status NVARCHAR(20) NOT NULL,
    InspectionResult NVARCHAR(20)
);
```

## 2.2. Dimension Tables

```sql
-- ===== CONFORMED DIMENSIONS =====

-- DimDate: The most important dimension in any data warehouse
CREATE TABLE analytics.DimDate (
    DateKey INT PRIMARY KEY,               -- 20260719
    FullDate DATE NOT NULL,
    DayOfWeek TINYINT NOT NULL,             -- 1=Monday, 7=Sunday
    DayName NVARCHAR(20) NOT NULL,          -- 'Monday', 'Thứ Hai'
    DayOfMonth TINYINT NOT NULL,
    DayOfYear SMALLINT NOT NULL,
    WeekOfYear TINYINT NOT NULL,
    MonthName NVARCHAR(20) NOT NULL,
    MonthOfYear TINYINT NOT NULL,
    Quarter TINYINT NOT NULL,
    Year SMALLINT NOT NULL,
    IsWeekend BIT NOT NULL,
    IsHoliday BIT NOT NULL DEFAULT 0,
    HolidayName NVARCHAR(100),
    SeasonName NVARCHAR(20),                -- Low, High, Peak, Shoulder
    FiscalYear SMALLINT,
    FiscalQuarter TINYINT,
    FirstDayOfMonth DATE NOT NULL,
    LastDayOfMonth DATE NOT NULL
);

-- DimRoom
CREATE TABLE analytics.DimRoom (
    RoomKey INT IDENTITY PRIMARY KEY,
    HotelRoomId UNIQUEIDENTIFIER NOT NULL,
    RoomNumber NVARCHAR(40) NOT NULL,
    RoomTypeName NVARCHAR(200),
    Floor NVARCHAR(40),
    Building NVARCHAR(80),
    Zone NVARCHAR(80),
    MaxAdults INT,
    MaxChildren INT,
    IsActive BIT NOT NULL,
    EffectiveFrom DATETIME2 NOT NULL,
    EffectiveTo DATETIME2,
    IsCurrent BIT NOT NULL DEFAULT 1
);

-- DimGuest
CREATE TABLE analytics.DimGuest (
    GuestKey INT IDENTITY PRIMARY KEY,
    GuestId UNIQUEIDENTIFIER,
    GuestNumber NVARCHAR(40),
    FullName NVARCHAR(200),
    Nationality NVARCHAR(80),
    Country NVARCHAR(80),
    VipLevel NVARCHAR(20),
    IsBlacklisted BIT,
    EnrollmentDate DATETIME2,
    EffectiveFrom DATETIME2 NOT NULL,
    IsCurrent BIT NOT NULL DEFAULT 1
);

-- DimProperty
CREATE TABLE analytics.DimProperty (
    PropertyKey INT IDENTITY PRIMARY KEY,
    PropertyId UNIQUEIDENTIFIER NOT NULL,
    PropertyCode NVARCHAR(20) NOT NULL,
    PropertyName NVARCHAR(200) NOT NULL,
    City NVARCHAR(100),
    Country NVARCHAR(80),
    TotalRooms INT,
    StarRating TINYINT,
    IsActive BIT NOT NULL DEFAULT 1
);

-- DimRatePlan
CREATE TABLE analytics.DimRatePlan (
    RatePlanKey INT IDENTITY PRIMARY KEY,
    RatePlanId UNIQUEIDENTIFIER NOT NULL,
    PlanCode NVARCHAR(40) NOT NULL,
    PlanName NVARCHAR(200) NOT NULL,
    BaseRate DECIMAL(18,2),
    Hierarchy NVARCHAR(20),
    IsActive BIT NOT NULL DEFAULT 1
);

-- DimEmployee
CREATE TABLE analytics.DimEmployee (
    EmployeeKey INT IDENTITY PRIMARY KEY,
    EmployeeId UNIQUEIDENTIFIER NOT NULL,
    FullName NVARCHAR(200) NOT NULL,
    Department NVARCHAR(200),
    Position NVARCHAR(200),
    ShiftType NVARCHAR(20),
    HireDate DATE,
    IsActive BIT NOT NULL DEFAULT 1
);

-- DimSource
CREATE TABLE analytics.DimSource (
    SourceKey INT IDENTITY PRIMARY KEY,
    SourceCode NVARCHAR(40) NOT NULL,
    SourceName NVARCHAR(200) NOT NULL,
    SourceType NVARCHAR(40)
);

-- DimMarketSegment
CREATE TABLE analytics.DimMarketSegment (
    MarketKey INT IDENTITY PRIMARY KEY,
    SegmentCode NVARCHAR(40) NOT NULL,
    SegmentName NVARCHAR(200) NOT NULL
);

-- DimCompany
CREATE TABLE analytics.DimCompany (
    CompanyKey INT IDENTITY PRIMARY KEY,
    CompanyId UNIQUEIDENTIFIER NOT NULL,
    CompanyCode NVARCHAR(40) NOT NULL,
    CompanyName NVARCHAR(200) NOT NULL
);

-- DimTravelAgent
CREATE TABLE analytics.DimTravelAgent (
    AgentKey INT IDENTITY PRIMARY KEY,
    AgentId UNIQUEIDENTIFIER NOT NULL,
    AgentCode NVARCHAR(40) NOT NULL,
    AgentName NVARCHAR(200) NOT NULL,
    CommissionPct DECIMAL(9,4)
);
```

---

# PHẦN 3: METRIC ENGINE

## 3.1. Metric Catalog

```csharp
/// <summary>
/// MetricCatalog is the single source of truth for ALL business metrics.
/// Every dashboard, report, and AI model uses these definitions.
/// If a metric formula changes, it changes in ONE place.
/// </summary>
public interface IMetricCatalog
{
    IMetricDefinition GetMetric(string metricCode);
    IReadOnlyList<IMetricDefinition> GetAllMetrics();
    IReadOnlyList<IMetricDefinition> GetMetricsByCategory(string category);
    void RegisterMetric(IMetricDefinition metric);
}

public interface IMetricDefinition
{
    string MetricCode { get; }           // "OCCUPANCY_PCT"
    string DisplayName { get; }           // "Công suất phòng"
    string DisplayNameEn { get; }         // "Occupancy Rate"
    string Category { get; }              // "Revenue", "Operations", "Housekeeping"
    string Unit { get; }                  // "%", "VND", "rooms", "guests"
    string Formula { get; }               // Human-readable: "RoomsSold / TotalRooms * 100"
    string[] DependsOn { get; }           // ["RoomsSold", "TotalRooms"]
    IMetricCalculator Calculator { get; }  // Implementation
    int CacheMinutes { get; }             // How long to cache
    string[] RequiredPermissions { get; }
}

public interface IMetricCalculator
{
    Task<MetricResult> CalculateAsync(
        MetricContext context,
        CancellationToken ct = default);
}

public class MetricContext
{
    public Guid PropertyId { get; set; }
    public DateOnly? BusinessDate { get; set; }
    public DateOnly? FromDate { get; set; }
    public DateOnly? ToDate { get; set; }
    public string Granularity { get; set; }  // "daily", "monthly", "yearly"
    public Dictionary<string, object> Parameters { get; set; }
}

public class MetricResult
{
    public string MetricCode { get; set; }
    public decimal Value { get; set; }
    public string FormattedValue { get; set; }  // "85.5%" or "125,000,000 VND"
    public decimal? PreviousValue { get; set; }  // For comparison
    public decimal? ChangePct { get; set; }      // % change from previous
    public string Trend { get; set; }            // "up", "down", "flat"
    public DateTime CalculatedAt { get; set; }
    public Dictionary<string, decimal> Components { get; set; }  // Breakdown
}
```

## 3.2. KPI Registry (Complete Catalog)

```csharp
public static class AsmileMetrics
{
    // ===== OCCUPANCY =====
    public static readonly IMetricDefinition OccupancyPct = new MetricDefinition(
        "OCCUPANCY_PCT", "Công suất phòng", "Occupancy Rate",
        "Operations", "%",
        "RoomsSold / TotalRooms × 100",
        new[] { "RoomsSold", "TotalRooms" });

    public static readonly IMetricDefinition ADR = new MetricDefinition(
        "ADR", "Giá phòng trung bình", "Average Daily Rate",
        "Revenue", "VND",
        "RoomRevenue / RoomsSold",
        new[] { "RoomRevenue", "RoomsSold" });

    public static readonly IMetricDefinition RevPAR = new MetricDefinition(
        "REVPAR", "Doanh thu trên phòng sẵn có", "Revenue Per Available Room",
        "Revenue", "VND",
        "RoomRevenue / TotalRooms",
        new[] { "RoomRevenue", "TotalRooms" });

    // ===== OPERATIONS =====
    public static readonly IMetricDefinition Arrivals = new MetricDefinition(
        "ARRIVALS", "Khách đến", "Arrivals", "Operations", "guests",
        "COUNT(Stay WHERE ArrivalDate = @Date)", null);

    public static readonly IMetricDefinition Departures = new MetricDefinition(
        "DEPARTURES", "Khách đi", "Departures", "Operations", "guests",
        "COUNT(Stay WHERE DepartureDate = @Date)", null);

    public static readonly IMetricDefinition InHouse = new MetricDefinition(
        "IN_HOUSE", "Khách đang ở", "In-House Guests", "Operations", "guests",
        "COUNT(Stay WHERE Status = 'InHouse')", null);

    public static readonly IMetricDefinition AvgLengthOfStay = new MetricDefinition(
        "AVG_LOS", "Thời gian ở trung bình", "Avg Length of Stay",
        "Operations", "days",
        "AVG(DepartureDate - ArrivalDate)", null);

    // ===== REVENUE =====
    public static readonly IMetricDefinition TotalRevenue = new MetricDefinition(
        "TOTAL_REVENUE", "Tổng doanh thu", "Total Revenue",
        "Revenue", "VND",
        "SUM(RoomRevenue + OtherRevenue)", null);

    public static readonly IMetricDefinition RevenuePerGuest = new MetricDefinition(
        "REV_PER_GUEST", "Doanh thu trên khách", "Revenue Per Guest",
        "Revenue", "VND",
        "TotalRevenue / TotalGuests", null);

    // ===== HOUSEKEEPING =====
    public static readonly IMetricDefinition DirtyRooms = new MetricDefinition(
        "DIRTY_ROOMS", "Phòng bẩn", "Dirty Rooms",
        "Housekeeping", "rooms",
        "COUNT(Room WHERE Status IN ('VacantDirty','OccupiedDirty'))", null);

    public static readonly IMetricDefinition AvgCleaningTime = new MetricDefinition(
        "AVG_CLEANING_TIME", "Thời gian dọn trung bình", "Avg Cleaning Time",
        "Housekeeping", "minutes",
        "AVG(DurationMinutes)", null);

    // ===== FINANCIAL =====
    public static readonly IMetricDefinition OutstandingBalance = new MetricDefinition(
        "OUTSTANDING", "Công nợ", "Outstanding Balance",
        "Finance", "VND",
        "SUM(FolioBalance WHERE Status = 'Open')", null);

    public static readonly IMetricDefinition DepositBalance = new MetricDefinition(
        "DEPOSIT_BALANCE", "Tiền đặt cọc", "Deposit Balance",
        "Finance", "VND",
        "SUM(DepositAmount WHERE Status = 'Active')", null);

    // ===== GUEST =====
    public static readonly IMetricDefinition RepeatGuestRate = new MetricDefinition(
        "REPEAT_GUEST", "Tỷ lệ khách quay lại", "Repeat Guest Rate",
        "Guest", "%",
        "RepeatGuests / TotalGuests × 100", null);

    public static readonly IMetricDefinition VipRatio = new MetricDefinition(
        "VIP_RATIO", "Tỷ lệ khách VIP", "VIP Ratio",
        "Guest", "%",
        "VIPGuests / TotalGuests × 100", null);

    public static readonly IMetricDefinition GuestSatisfaction = new MetricDefinition(
        "GUEST_SATISFACTION", "Điểm hài lòng", "Guest Satisfaction",
        "Guest", "stars",
        "AVG(Rating) FROM Feedback", null);
}
```

---

# PHẦN 4: REPORTING ENGINE

## 4.1. Report Definition

```csharp
public interface IReportDefinition
{
    string ReportCode { get; }              // "DAILY_MANAGER_REPORT"
    string ReportName { get; }              // "Báo cáo quản lý hàng ngày"
    string Category { get; }                // "Operations", "Finance", "Housekeeping"
    ReportOutputType[] SupportedOutputs { get; }  // PDF, Excel, CSV, Print
    IReadOnlyList<IReportParameter> Parameters { get; }
    IReadOnlyList<IReportSection> Sections { get; }
    IReportRenderer Renderer { get; }
}

public interface IReportSection
{
    string SectionName { get; }
    IReadOnlyList<string> MetricCodes { get; }     // Metrics to include
    IReadOnlyList<IReportChart> Charts { get; }    // Visualizations
    string TemplateKey { get; }                    // Layout template
}

public enum ReportOutputType
{
    PDF, Excel, CSV, Word, HTML, Print, Email
}

public interface IReportParameter
{
    string ParameterName { get; }
    Type ParameterType { get; }
    bool IsRequired { get; }
    object DefaultValue { get; }
    string[] AllowedValues { get; }         // For dropdowns
}
```

## 4.2. Report Catalog

```csharp
public static class AsmileReports
{
    // ===== OPERATIONS =====
    public const string DailyManagerReport = "DAILY_MANAGER_REPORT";
    public const string ArrivalList = "ARRIVAL_LIST";
    public const string DepartureList = "DEPARTURE_LIST";
    public const string InHouseList = "IN_HOUSE_LIST";
    public const string RoomStatusReport = "ROOM_STATUS_REPORT";
    public const string HousekeepingReport = "HOUSEKEEPING_REPORT";
    public const string WakeUpCallReport = "WAKE_UP_CALL_REPORT";

    // ===== FINANCE =====
    public const string RevenueReport = "REVENUE_REPORT";
    public const string NightAuditReport = "NIGHT_AUDIT_REPORT";
    public const string CashierReport = "CASHIER_REPORT";
    public const string PaymentReport = "PAYMENT_REPORT";
    public const string DepositReport = "DEPOSIT_REPORT";
    public const string OutstandingReport = "OUTSTANDING_REPORT";
    public const string TaxReport = "TAX_REPORT";

    // ===== GUEST =====
    public const string GuestHistoryReport = "GUEST_HISTORY_REPORT";
    public const string VipReport = "VIP_REPORT";
    public const string GuestFeedbackReport = "GUEST_FEEDBACK_REPORT";

    // ===== COMMERCIAL =====
    public const string OccupancyForecast = "OCCUPANCY_FORECAST";
    public const string RevenueForecast = "REVENUE_FORECAST";
    public const string RateReport = "RATE_REPORT";
    public const string MarketSegmentReport = "MARKET_SEGMENT_REPORT";
    public const string AgencyProductionReport = "AGENCY_PRODUCTION_REPORT";
    public const string CompanyReport = "COMPANY_REPORT";
}
```

## 4.3. Report Scheduler

```csharp
public interface IReportScheduler
{
    Task<string> ScheduleReportAsync(string reportCode,
        Dictionary<string, object> parameters,
        ReportOutputType outputType,
        string cronExpression,
        string[] emailRecipients = null);

    Task CancelScheduledReportAsync(string scheduleId);
    Task<IReadOnlyList<ScheduledReport>> GetScheduledReportsAsync();
}

public class ScheduledReport
{
    public string ScheduleId { get; set; }
    public string ReportCode { get; set; }
    public string CronExpression { get; set; }    // "0 8 * * *" = every day at 8 AM
    public ReportOutputType OutputType { get; set; }
    public string[] EmailRecipients { get; set; }
    public DateTime? LastRunAt { get; set; }
    public DateTime? NextRunAt { get; set; }
    public string LastRunStatus { get; set; }     // Success, Failed

    // Example schedules:
    // "0 7 * * *"     → Daily Manager Report at 7:00 AM
    // "0 2 * * *"     → Night Audit Report at 2:00 AM
    // "0 8 * * 1"     → Weekly Revenue Report every Monday 8:00 AM
    // "0 8 1 * *"     → Monthly Financial Report on 1st at 8:00 AM
}
```

---

# PHẦN 5: DASHBOARD ENGINE

## 5.1. Dashboard Definition

```csharp
public interface IDashboardDefinition
{
    string DashboardCode { get; }
    string DashboardName { get; }
    string Category { get; }
    IReadOnlyList<IDashboardWidget> Widgets { get; }
    IDashboardLayout Layout { get; }
    int RefreshIntervalSeconds { get; }      // 0 = no auto-refresh
}

public interface IDashboardWidget
{
    string WidgetId { get; }
    string WidgetType { get; }              // "KpiCard", "Chart", "Table", "Timeline"
    string Title { get; }
    int Column { get; }                     // Grid column (0-based)
    int Row { get; }                        // Grid row (0-based)
    int ColumnSpan { get; }                 // 1, 2, 3, 4
    int RowSpan { get; }
    IReadOnlyList<string> MetricCodes { get; }
    Dictionary<string, object> Configuration { get; }
}

public interface IDashboardLayout
{
    int Columns { get; }                    // 1-4 columns
    bool IsResponsive { get; }
    string LayoutJson { get; }
}
```

## 5.2. Widget Types

```yaml
KpiCard:
  - Single metric with large number
  - Comparison indicator (▲ 12% vs yesterday)
  - Trend sparkline (optional)
  - Color: green (up), red (down), gray (flat)

Chart:
  - Bar chart, Line chart, Pie chart, Donut chart
  - Multi-series support
  - Time axis or category axis
  - Drill-down capability

Table:
  - Paginated data grid
  - Sortable columns
  - Export button
  - Row limit: 100

Timeline:
  - Chronological event feed
  - Icons per event type
  - Relative time display ("5 phút trước")

Heatmap:
  - Room occupancy by floor
  - Color intensity = value

Gauge:
  - Semi-circular gauge
  - Target line indicator
  - Example: Occupancy 85% (target: 80%)
```

## 5.3. Dashboard Catalog

```csharp
public static class AsmileDashboards
{
    public const string ExecutiveDashboard = "EXECUTIVE";
    public const string FrontOfficeDashboard = "FRONT_OFFICE";
    public const string RevenueDashboard = "REVENUE";
    public const string HousekeepingDashboard = "HOUSEKEEPING";
    public const string NightAuditDashboard = "NIGHT_AUDIT";
    public const string CashierDashboard = "CASHIER";
    public const string ReservationDashboard = "RESERVATION";
    public const string GroupDashboard = "GROUP";
    public const string GuestDashboard = "GUEST";

    // Widget sizes: (Column, Row, ColSpan, RowSpan)
    // Grid is 4 columns × N rows
    // KPI cards: 1×1, Charts: 2×2, Tables: 4×3
}
```

---

# PHẦN 6: SEMANTIC LAYER

## 6.1. Semantic Layer Architecture

```
User / AI asks:
  "Doanh thu hôm nay là bao nhiêu?"
         │
         ▼
┌─────────────────────────────────────────────┐
│          SEMANTIC LAYER                      │
│                                              │
│  ┌──────────────────────────────────────┐   │
│  │ Vocabulary Registry                  │   │
│  │ "Doanh thu" → TOTAL_REVENUE          │   │
│  │ "hôm nay" → BusinessDate = TODAY     │   │
│  │ "phòng" → RoomRevenue               │   │
│  │ "công suất" → OCCUPANCY_PCT          │   │
│  │ "khách VIP" → Guest.VipLevel != 0    │   │
│  └──────────────────────────────────────┘   │
│                                              │
│  ┌──────────────────────────────────────┐   │
│  │ Query Builder                        │   │
│  │ Intent → MetricCodes → Parameters    │   │
│  │ → MetricEngine.Calculate()           │   │
│  └──────────────────────────────────────┘   │
└─────────────────────────────────────────────┘
         │
         ▼
   MetricEngine.Calculate("TOTAL_REVENUE", { Date: TODAY })
         │
         ▼
   Result: "125,000,000 VND"
```

## 6.2. Vocabulary Registry

```csharp
public interface IVocabularyRegistry
{
    void RegisterTerm(VocabularyTerm term);
    VocabularyTerm ResolveTerm(string naturalLanguageTerm, string language = "vi");
    IReadOnlyList<VocabularyTerm> SearchTerms(string query, string language = "vi");
}

public class VocabularyTerm
{
    public string TermCode { get; set; }         // "TOTAL_REVENUE"
    public string TermVi { get; set; }            // "Doanh thu", "Tổng doanh thu"
    public string TermEn { get; set; }            // "Revenue", "Total Revenue"
    public string[] Synonyms { get; set; }        // ["doanh số", "thu nhập", "tiền"]
    public string MapsToMetricCode { get; set; }  // Metric code
    public string Category { get; set; }          // "Finance"
    public string[] ExampleQuestions { get; set; }
}

// ===== REGISTERED VOCABULARY =====
public static class AsmileVocabulary
{
    public static readonly VocabularyTerm Revenue = new()
    {
        TermCode = "TOTAL_REVENUE",
        TermVi = "Doanh thu",
        TermEn = "Revenue",
        Synonyms = new[] { "doanh số", "thu nhập", "tiền thu", "doanh thu phòng" },
        MapsToMetricCode = "TOTAL_REVENUE",
        Category = "Finance",
        ExampleQuestions = new[] {
            "Doanh thu hôm nay?",
            "Doanh thu tháng này?",
            "So sánh doanh thu với tháng trước?"
        }
    };

    public static readonly VocabularyTerm Occupancy = new()
    {
        TermCode = "OCCUPANCY_PCT",
        TermVi = "Công suất",
        TermEn = "Occupancy",
        Synonyms = new[] { "tỷ lệ lấp đầy", "số phòng đang ở", "phòng có khách" },
        MapsToMetricCode = "OCCUPANCY_PCT",
        Category = "Operations"
    };
}
```

---

# PHẦN 7: SNAPSHOT ENGINE

## 7.1. Snapshot Architecture

```csharp
/// <summary>
/// Snapshots are immutable point-in-time captures of metrics.
/// Night Audit creates the authoritative daily snapshot.
/// Hourly snapshots provide intraday granularity.
/// </summary>
public interface ISnapshotEngine
{
    // Create snapshot
    Task<SnapshotResult> CreateSnapshotAsync(
        SnapshotType type,
        Guid propertyId,
        DateOnly businessDate,
        CancellationToken ct = default);

    // Read snapshot
    Task<T> GetSnapshotAsync<T>(
        SnapshotType type,
        Guid propertyId,
        DateOnly businessDate,
        CancellationToken ct = default) where T : ISnapshot;

    // Compare snapshots
    Task<SnapshotComparison> CompareSnapshotsAsync(
        SnapshotType type,
        Guid propertyId,
        DateOnly date1,
        DateOnly date2,
        CancellationToken ct = default);
}

public enum SnapshotType
{
    NightAudit,         // Authoritative — created by Night Audit
    Hourly,             // Intraday — created by background job
    Monthly,            // Month-end — created on 1st of next month
    Yearly,             // Year-end — created on Jan 1
    Revenue,            // Revenue-specific
    Occupancy,          // Occupancy-specific
    Housekeeping,       // Housekeeping status
    Forecast            // Forecast snapshot (for accuracy comparison)
}

public interface ISnapshot
{
    Guid SnapshotId { get; }
    SnapshotType Type { get; }
    Guid PropertyId { get; }
    DateOnly BusinessDate { get; }
    DateTime CreatedAtUtc { get; }
    bool IsAuthoritative { get; }    // Night Audit snapshots are authoritative
    string CreatedBy { get; }        // "NightAudit", "HourlyJob", "Manual"
}

public class SnapshotComparison
{
    public decimal Value1 { get; set; }
    public decimal Value2 { get; set; }
    public decimal AbsoluteChange { get; set; }
    public decimal PercentageChange { get; set; }
    public string Trend { get; set; }            // "up", "down", "flat"
    public string ComparisonLabel { get; set; }  // "vs yesterday", "vs last week"
}
```

## 7.2. Snapshot Schedule

```yaml
Night Audit (Daily, Authoritative):
  Time: 2:00 AM (configurable)
  Creates:
    - RevenueSnapshot (immutable)
    - OccupancySnapshot (immutable)
    - HousekeepingSnapshot
    - NightAuditReport

Hourly (Background Job):
  Time: Every hour at :05
  Creates:
    - HourlyRevenueSnapshot
    - HourlyOccupancySnapshot
    - Used for "live" dashboards (last snapshot + delta)

Monthly (Month-End Close):
  Time: 1st of month at 2:00 AM
  Creates:
    - MonthlyRevenueSnapshot
    - MonthlyOccupancySnapshot
    - MonthlyFinancialReport

Yearly (Year-End Close):
  Time: Jan 1 at 2:00 AM
  Creates:
    - YearlyRevenueSnapshot
    - YearlyOccupancySnapshot
```

---

# PHẦN 8: FORECAST ENGINE

## 8.1. Forecast Architecture

```csharp
public interface IForecastEngine
{
    Task<ForecastResult> GenerateForecastAsync(
        ForecastRequest request,
        CancellationToken ct = default);

    Task<decimal> GetForecastAccuracyAsync(
        string metricCode,
        Guid propertyId,
        DateOnly forecastDate,
        CancellationToken ct = default);
}

public class ForecastRequest
{
    public string MetricCode { get; set; }       // "OCCUPANCY_PCT"
    public Guid PropertyId { get; set; }
    public DateOnly ForecastDate { get; set; }   // Single date or...
    public DateOnly? ForecastFrom { get; set; }  // ...date range
    public DateOnly? ForecastTo { get; set; }
    public int HistoricalDays { get; set; } = 90; // Lookback window
    public ForecastMethod Method { get; set; } = ForecastMethod.Auto;
}

public enum ForecastMethod
{
    Auto,               // System selects best method
    MovingAverage,      // Simple moving average
    ExponentialSmoothing, // Weighted recent data
    LinearRegression,   // Trend line
    SeasonalNaive,      // Same day last year
    LastYearComparison, // Compare with same date last year
    BookingPace,        // Based on current booking pace
    AI                  // Machine Learning model
}

public class ForecastResult
{
    public string MetricCode { get; set; }
    public decimal ForecastedValue { get; set; }
    public decimal? LowerBound { get; set; }     // 95% confidence
    public decimal? UpperBound { get; set; }
    public decimal? Accuracy { get; set; }       // Historical MAPE
    public ForecastMethod MethodUsed { get; set; }
    public string[] Factors { get; set; }        // What influenced this forecast
    public DateTime GeneratedAtUtc { get; set; }
}
```

## 8.2. Forecast Metrics

```yaml
Occupancy Forecast:
  Method: BookingPace + HistoricalPattern
  Factors: Current bookings, historical occupancy, events, season
  Horizon: 90 days
  Confidence: High (1-7 days), Medium (8-30 days), Low (31-90 days)

Revenue Forecast:
  Method: ExponentialSmoothing
  Factors: Occupancy forecast × ADR forecast
  Horizon: 90 days

Demand Forecast:
  Method: BookingPace
  Factors: Pickup rate, lead time, cancellation rate
  Horizon: 365 days

Staff Forecast:
  Method: OccupancyForecast / RoomsPerStaff
  Factors: Occupancy forecast, housekeeping productivity
  Horizon: 30 days
```

---

# PHẦN 9: AI DATASET LAYER

## 9.1. Dataset Architecture

```csharp
/// <summary>
/// AI Dataset Layer provides ML-ready datasets.
/// AI NEVER queries operational database directly.
/// All AI data comes through this layer.
/// </summary>
public interface IAIDatasetService
{
    Task<AIDataset> GenerateDatasetAsync(
        string datasetCode,
        AIDatasetParameters parameters,
        CancellationToken ct = default);

    Task<IReadOnlyList<AIDataset>> GetAvailableDatasetsAsync();
    Task<AIDataset> GetDatasetVersionAsync(
        string datasetCode, int version, CancellationToken ct = default);
}

public class AIDataset
{
    public string DatasetCode { get; set; }
    public int Version { get; set; }
    public string Description { get; set; }
    public DateTime GeneratedAtUtc { get; set; }
    public int RowCount { get; set; }
    public int FeatureCount { get; set; }
    public string[] FeatureNames { get; set; }
    public string TargetColumn { get; set; }
    public string DataFormat { get; set; }       // "CSV", "JSON", "Parquet"
    public byte[] Data { get; set; }
    public Dictionary<string, string> Metadata { get; set; }
}

public class AIDatasetParameters
{
    public Guid PropertyId { get; set; }
    public DateOnly? FromDate { get; set; }
    public DateOnly? ToDate { get; set; }
    public string[] IncludeFeatures { get; set; }
    public string[] ExcludeFeatures { get; set; }
    public bool IncludePII { get; set; } = false; // Default: NO PII
    public int? MaxRows { get; set; }
}
```

## 9.2. AI Dataset Catalog

```yaml
AIReservationDataset:
  Purpose: Predict booking patterns, no-show probability
  Features: LeadTime, ArrivalDayOfWeek, LengthOfStay, Adults, Children,
            RoomType, RatePlan, MarketSegment, Source, VIP, Season, ADR
  Target: IsNoShow, IsCancelled, TotalRevenue
  Rows: ~100K+ (all historical reservations)
  Refresh: Weekly

AIOccupancyDataset:
  Purpose: Occupancy prediction, overbooking optimization
  Features: Date, DayOfWeek, Month, Season, IsHoliday, IsWeekend,
            RoomsAvailable, RoomsBooked, BookingPace, CancellationRate
  Target: FinalOccupancy
  Rows: ~10K+ (daily snapshots)
  Refresh: Daily (after night audit)

AIRevenueDataset:
  Purpose: Revenue forecasting, pricing optimization
  Features: Occupancy, ADR, RevPAR, Season, DayOfWeek, IsHoliday,
            CompetitorRate (if available), Events, MarketingSpend
  Target: TotalRevenue, RoomRevenue, OtherRevenue
  Rows: ~10K+ (daily snapshots)
  Refresh: Daily

AIGuestDataset:
  Purpose: Guest segmentation, churn prediction, lifetime value
  Features: TotalStays, TotalRevenue, AvgSpend, LastStayDate,
            StayFrequency, PreferredRoomType, VipLevel, Nationality,
            BookingChannel, CancellationRate, NoShowRate
  Target: LifetimeValue, ChurnProbability, UpgradePropensity
  Rows: ~50K+ (guest profiles)
  Refresh: Weekly

AIRecommendationDataset:
  Purpose: Room upgrade recommendations, upsell targeting
  Features: GuestSegment, CurrentRoom, AvailableUpgrades,
            UpgradePrice, LengthOfStay, Occasion, LoyaltyLevel
  Target: UpgradeAcceptProbability
  Refresh: Daily
```

---

# PHẦN 10: OLAP ENGINE

## 10.1. Cube Definition

```csharp
public interface IOlapCube
{
    string CubeName { get; }
    IReadOnlyList<IOlapDimension> Dimensions { get; }
    IReadOnlyList<IOlapMeasure> Measures { get; }
    Task<OlapResult> QueryAsync(OlapQuery query, CancellationToken ct = default);
}

public interface IOlapDimension
{
    string Name { get; }
    IReadOnlyList<IOlapHierarchy> Hierarchies { get; }
}

public interface IOlapHierarchy
{
    string Name { get; }
    string[] Levels { get; }     // ["All", "Year", "Quarter", "Month", "Day"]
}

public interface IOlapMeasure
{
    string Name { get; }
    string AggregationFunction { get; }  // "SUM", "AVG", "COUNT", "MIN", "MAX"
    string Format { get; }              // "N0", "P1", "C2"
}

public class OlapQuery
{
    public string CubeName { get; set; }
    public string[] RowDimensions { get; set; }     // ["Date.Year", "Date.Month"]
    public string[] ColumnDimensions { get; set; }  // ["Property.City"]
    public string[] FilterDimensions { get; set; }  // ["Date.Year = 2026"]
    public string[] Measures { get; set; }          // ["Revenue", "Occupancy"]
}
```

## 10.2. Cubes

```yaml
Cube: RevenueCube
  Dimensions: Date (Year→Quarter→Month→Day), Property, RoomType, MarketSegment
  Measures: TotalRevenue (SUM), RoomRevenue (SUM), ADR (AVG), RevPAR (AVG)

Cube: OccupancyCube
  Dimensions: Date, Property, RoomType, Season
  Measures: RoomsSold (SUM), TotalRooms (SUM), OccupancyPct (AVG)

Cube: GuestCube
  Dimensions: Date, Nationality, VipLevel, Source, MarketSegment
  Measures: Arrivals (COUNT), Departures (COUNT), AvgStay (AVG), RepeatRate (AVG)

Cube: HousekeepingCube
  Dimensions: Date, Property, Shift, Employee
  Measures: TasksCompleted (COUNT), AvgDuration (AVG), InspectionPassRate (AVG)
```

---

# PHẦN 11: MATERIALIZED VIEW STRATEGY

```sql
-- Materialized views are pre-computed aggregations
-- Refreshed after each Night Audit snapshot

-- MV_DailyRevenue: Aggregated by day
CREATE VIEW analytics.MV_DailyRevenue
WITH SCHEMABINDING AS
SELECT
    d.DateKey,
    d.FullDate,
    r.PropertyKey,
    SUM(r.Amount) AS TotalRevenue,
    SUM(CASE WHEN r.RevenueType = 'RoomCharge' THEN r.Amount ELSE 0 END) AS RoomRevenue,
    SUM(CASE WHEN r.RevenueType = 'Payment' THEN r.Amount ELSE 0 END) AS TotalPayments,
    SUM(CASE WHEN r.RevenueType = 'Refund' THEN r.Amount ELSE 0 END) AS TotalRefunds,
    COUNT_BIG(*) AS TransactionCount
FROM analytics.FactRevenue r
JOIN analytics.DimDate d ON r.DateKey = d.DateKey
GROUP BY d.DateKey, d.FullDate, r.PropertyKey;

-- MV_MonthlyOccupancy: Aggregated by month
CREATE VIEW analytics.MV_MonthlyOccupancy
WITH SCHEMABINDING AS
SELECT
    d.Year, d.MonthOfYear, d.MonthName,
    s.PropertyKey,
    AVG(s.OccupancyPct) AS AvgOccupancy,
    AVG(s.ADR) AS AvgADR,
    AVG(s.RevPAR) AS AvgRevPAR,
    SUM(s.RoomRevenue) AS TotalRoomRevenue,
    SUM(s.Arrivals) AS TotalArrivals,
    SUM(s.Departures) AS TotalDepartures,
    COUNT_BIG(*) AS DayCount
FROM analytics.FactNightAudit s
JOIN analytics.DimDate d ON s.DateKey = d.DateKey
GROUP BY d.Year, d.MonthOfYear, d.MonthName, s.PropertyKey;
```

---

# PHẦN 12: PERFORMANCE STRATEGY

```yaml
Data Warehouse Performance:
  - Columnstore indexes on all Fact tables (SQL Server)
  - Partition Fact tables by DateKey (monthly partitions)
  - Materialized Views for common aggregations
  - Incremental refresh: only load delta since last ETL run
  - ETL runs after Night Audit completes (2:00 AM - 3:00 AM)

Query Performance:
  - Metric results cached with TTL:
      Real-time: 30 seconds
      Hourly: 5 minutes
      Daily: 15 minutes
      Monthly: 1 hour
  - Pre-compute top 20 metrics on Night Audit completion
  - Dashboard queries: < 500ms (p95)
  - Report generation: < 30 seconds (background)

Storage:
  - Fact tables: partition by month, archive after 3 years
  - Snapshots: keep 2 years online, archive older
  - ETL logs: keep 90 days
  - Dataset generations: keep latest 3 versions only

Scaling:
  - Single SQL Server for hotel scale (< 1000 rooms)
  - Azure SQL / Managed Instance for multi-property
  - Future: Synapse Analytics for enterprise scale (100+ properties)
```

---

# PHẦN 13: SECURITY

```yaml
Row-Level Security:
  - Finance metrics: Manager and above
  - Guest PII: never in analytics tables
  - Employee performance: Department head only
  - Forecast data: Manager and above

Column-Level Security:
  - GuestName: masked in exports unless permission
  - Revenue: masked for non-Manager roles
  - Profit margins: Director and above

Dashboard Permission:
  - Executive: Director, GM
  - Revenue: Manager, Finance
  - Operations: Front Office Manager, Supervisor
  - Housekeeping: Housekeeping Manager, Supervisor
  - Cashier: Cashier, Front Office Manager

Audit:
  - All analytics queries logged (who, what, when)
  - Exports logged with recipient
  - Scheduled reports: recipient list audited
```

---

# PHẦN 14: FOLDER STRUCTURE

```
ASMILE.BO.Analytics/
├── ASMILE.BO.Analytics.Abstractions/
│   ├── IMetricCatalog.cs
│   ├── IMetricDefinition.cs
│   ├── IMetricCalculator.cs
│   ├── IReportDefinition.cs
│   ├── IReportScheduler.cs
│   ├── IDashboardDefinition.cs
│   ├── ISnapshotEngine.cs
│   ├── IForecastEngine.cs
│   ├── ITrendEngine.cs
│   ├── IOlapCube.cs
│   ├── IAIDatasetService.cs
│   ├── IVocabularyRegistry.cs
│   └── IAnalyticsApi.cs
│
├── ASMILE.BO.Analytics.Engine/
│   ├── MetricEngine/
│   │   ├── MetricCatalog.cs
│   │   ├── MetricCalculator.cs
│   │   └── AsmileMetrics.cs
│   ├── ReportingEngine/
│   │   ├── ReportRenderer.cs
│   │   ├── ReportScheduler.cs
│   │   └── AsmileReports.cs
│   ├── DashboardEngine/
│   │   └── DashboardService.cs
│   ├── SnapshotEngine/
│   │   └── SnapshotService.cs
│   ├── ForecastEngine/
│   │   └── ForecastService.cs
│   ├── TrendEngine/
│   │   └── TrendService.cs
│   ├── OlapEngine/
│   │   └── OlapService.cs
│   ├── SemanticLayer/
│   │   ├── VocabularyRegistry.cs
│   │   └── AsmileVocabulary.cs
│   └── ETL/
│       └── EtlPipeline.cs
│
├── ASMILE.BO.Analytics.DataWarehouse/
│   ├── Facts/
│   │   ├── FactReservation.cs
│   │   ├── FactStay.cs
│   │   ├── FactRevenue.cs
│   │   ├── FactPayment.cs
│   │   ├── FactNightAudit.cs
│   │   └── FactHousekeeping.cs
│   ├── Dimensions/
│   │   ├── DimDate.cs
│   │   ├── DimRoom.cs
│   │   ├── DimGuest.cs
│   │   ├── DimProperty.cs
│   │   └── ...
│   ├── MaterializedViews/
│   │   ├── MV_DailyRevenue.sql
│   │   └── MV_MonthlyOccupancy.sql
│   └── ETL/
│       ├── EtlReservation.sql
│       ├── EtlRevenue.sql
│       └── EtlNightAudit.sql
│
├── ASMILE.BO.Analytics.AI/
│   ├── Datasets/
│   │   ├── AIReservationDataset.cs
│   │   ├── AIOccupancyDataset.cs
│   │   ├── AIRevenueDataset.cs
│   │   └── AIGuestDataset.cs
│   └── Features/
│       ├── FeatureEngineering.cs
│       └── DatasetVersioning.cs
│
└── ASMILE.BO.Analytics.API/
    ├── AnalyticsController.cs
    ├── MetricController.cs
    ├── DashboardController.cs
    └── PowerBIConnector.cs
```

---

# PHẦN 15: IMPLEMENTATION ROADMAP

| Sprint | Deliverable | Duration |
|---|---|---|
| BI-001 | DimDate + DimProperty + DimRoom + DimGuest tables | 2 ngày |
| BI-002 | FactStay + FactRevenue + FactNightAudit tables | 3 ngày |
| BI-003 | ETL pipeline: Night Audit → Fact tables | 3 ngày |
| BI-004 | Metric Engine + KPI Catalog (20 metrics) | 3 ngày |
| BI-005 | Snapshot Engine (daily, hourly) | 2 ngày |
| BI-006 | Reporting Engine (PDF, Excel, CSV) + 6 core reports | 4 ngày |
| BI-007 | Report Scheduler + Email delivery | 2 ngày |
| BI-008 | Dashboard Engine + Executive Dashboard widgets | 3 ngày |
| BI-009 | Semantic Layer + Vocabulary Registry | 2 ngày |
| BI-010 | Historical Engine (YoY, MoM, Moving Avg) | 2 ngày |
| BI-011 | Forecast Engine (5 methods) | 3 ngày |
| BI-012 | Trend Engine + Anomaly Detection | 2 ngày |
| BI-013 | OLAP Engine + 4 cubes | 3 ngày |
| BI-014 | AI Dataset Layer + 5 datasets | 3 ngày |
| BI-015 | Materialized Views + Performance tuning | 2 ngày |
| BI-016 | Analytics API (REST) | 2 ngày |
| BI-017 | Power BI Connector + Gateway | 2 ngày |
| BI-018 | Security (RLS, Column Security, Audit) | 2 ngày |
| BI-019 | Integration testing + ETL validation | 3 ngày |
| BI-020 | Production deployment + Monitoring | 2 ngày |

**Total: ~47 working days (9.5 weeks)**

---

# PHẦN 16: ARCHITECTURE DECISION RECORDS

### ADR-BI001: Star Schema over Snowflake Schema
**Decision:** Use Star Schema (denormalized dimensions) for the analytics warehouse.
**Rationale:** Star Schema is simpler to query, performs better for OLAP workloads, and is the standard for Power BI and Excel connectivity. Snowflake adds complexity without benefit at hotel data scale.

### ADR-BI002: Night Audit as ETL Trigger
**Decision:** ETL pipeline runs immediately after Night Audit completes. No streaming/realtime ETL.
**Rationale:** Night Audit provides a consistent point-in-time for all metrics. Delta queries handle "since last night audit" for live dashboards.

### ADR-BI003: Semantic Layer as the single source of truth
**Decision:** All metrics, KPIs, and business terms are defined in the Semantic Layer. No ad-hoc SQL in reports or dashboards.
**Rationale:** Ensures consistency across all consumers (WPF, Power BI, Excel, AI). Changes propagate automatically.

### ADR-BI004: AI Datasets as versioned snapshots
**Decision:** AI training datasets are versioned and immutable. AI models reference dataset versions, not live data.
**Rationale:** Reproducibility is critical for ML. Model accuracy can only be measured against known dataset versions.

### ADR-BI005: Incremental ETL, not full refresh
**Decision:** ETL processes only the delta since the last run (typically 1 day of data).
**Rationale:** Full refresh is unnecessary at hotel data volumes (millions, not billions of rows). Incremental is faster and less resource-intensive.

---

# PHẦN 17: CODING STANDARDS (Analytics)

```yaml
Metric Definitions:
  - Every metric has: code, name (vi), name (en), formula, unit, dependencies
  - Metric codes use UPPER_SNAKE_CASE
  - Formulas stored as human-readable strings (not code)
  - Dependencies explicitly listed (for dependency graph)

ETL:
  - ETL jobs are idempotent (can re-run safely)
  - Every ETL job logs: rows processed, duration, errors
  - ETL failures trigger alert (email + notification center)
  - ETL runs in transaction: all or nothing per table

Reports:
  - Report templates are XAML/HTML, not hardcoded
  - Parameters are typed, validated, documented
  - Reports use Semantic Layer metrics, not raw SQL
  - Export includes metadata: generated at, by whom, parameters

Dashboards:
  - Widgets reference metric codes, not SQL
  - Layout is responsive (reflows on window resize)
  - Auto-refresh interval is configurable per dashboard
  - Permission check on every widget

AI Datasets:
  - Datasets are versioned (v1, v2, v3)
  - Feature engineering is documented per version
  - PII is excluded by default (explicit opt-in required)
  - Dataset quality metrics recorded (completeness, balance, drift)
```

---

*Enterprise Analytics Platform architecture complete. 17 sections. This is the single source of truth for all ASMILE business intelligence, reporting, dashboards, metrics, forecasting, and AI data.*
