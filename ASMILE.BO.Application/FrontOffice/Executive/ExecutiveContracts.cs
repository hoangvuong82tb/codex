namespace ASMILE.BO.Application.FrontOffice.Executive;

// =============================================================================
// WIDGET-BASED DASHBOARD
// =============================================================================

public enum WidgetSize { Small, Medium, Large, Full }
public enum WidgetType { KPI, Chart, Table, Alert, Trend, Gauge, Forecast }
public enum KpiTrend { Up, Down, Flat, Unknown }
public enum AlertLevel { Info, Warning, Critical, Emergency }

public sealed record DashboardWidget(
    string WidgetId, string Title, WidgetType Type, WidgetSize Size,
    int Row, int Col, string? DataEndpoint, bool IsCollapsible,
    bool IsRefreshable, int RefreshIntervalSeconds
);

public sealed record KpiWidget(
    string KpiId, string Label, string Value, string Format, string? SubValue,
    KpiTrend Trend, decimal? ChangePct, string? DrillDownRoute, string? Color
);

public sealed record ChartWidget(
    string ChartId, string Title, string ChartType, // "Bar", "Line", "Pie", "Gauge", "Heatmap"
    IReadOnlyList<ChartSeries> Series, string? XAxisLabel, string? YAxisLabel
);

public sealed record ChartSeries(string Name, IReadOnlyList<decimal> Data, IReadOnlyList<string> Labels, string? Color);

public sealed record TableWidget<T>(
    string TableId, string Title, IReadOnlyList<string> Columns,
    IReadOnlyList<T> Rows, int TotalRows, bool HasPagination, string? DrillDownRoute
);

public sealed record AlertWidget(
    string AlertId, AlertLevel Level, string Title, string Message,
    string Module, DateTime FiredAtUtc, string? ActionRoute, bool IsAcknowledged
);

// =============================================================================
// EXECUTIVE KPIs
// =============================================================================

public sealed record ExecutiveKpiDto(
    // Occupancy & Revenue
    decimal OccupancyPct, decimal OccupancyPctYesterday, KpiTrend OccupancyTrend,
    decimal ADR, decimal ADRYesterday, KpiTrend ADRTrend,
    decimal RevPAR, decimal RevPARYesterday, KpiTrend RevPARTrend,
    // Bookings
    int ArrivalsToday, int DeparturesToday, int InHouse,
    int ReservationsCreated, int Cancellations, int NoShows,
    // Revenue
    decimal TodayRevenue, decimal MonthToDateRevenue, decimal YearToDateRevenue,
    decimal RoomRevenue, decimal FbRevenue, decimal OtherRevenue,
    // Forecast
    decimal ForecastOccupancy7d, decimal ForecastRevenue7d, decimal ForecastRevPAR7d
);

public sealed record OperationalKpiDto(
    int TotalRooms, int VacantClean, int VacantDirty, int Occupied, int OutOfOrder,
    decimal RoomReadinessPct, decimal HousekeepingSlaPct,
    int OpenWorkOrders, decimal MaintenanceSlaPct,
    decimal CashVarianceToday, int NightAuditStatus,
    int OnlineLocks, int OfflineLocks, decimal DoorHealthPct,
    int ActiveChannels, decimal ChannelHealthPct, int ConciergeOpenRequests
);

public sealed record FinancialKpiDto(
    decimal TotalRevenue, decimal TotalCharges, decimal TotalPayments,
    decimal TotalDeposits, decimal TotalRefunds, decimal OutstandingBalance,
    decimal CashPosition, decimal PaymentMixCash, decimal PaymentMixCard,
    decimal PaymentMixTransfer, int FoliosOpen, int FoliosSettled,
    decimal DepositLiability, decimal RefundLiability
);

public sealed record ForecastDto(
    decimal OccupancyForecast7, decimal OccupancyForecast30, decimal OccupancyForecast90,
    decimal RevenueForecast7, decimal RevenueForecast30, decimal RevenueForecast90,
    decimal PickupForecast7, decimal PickupForecast30,
    int RoomsToCleanTomorrow, int ExpectedArrivalsTomorrow, int ExpectedDeparturesTomorrow
);

// =============================================================================
// EXECUTIVE COCKPIT (5 Dashboards)
// =============================================================================

public sealed record GmDashboardDto(
    ExecutiveKpiDto KPIs, IReadOnlyList<KpiWidget> RevenueKPIs,
    IReadOnlyList<ChartWidget> OccupancyChart, IReadOnlyList<ChartWidget> RevenueChart,
    IReadOnlyList<AlertWidget> CriticalAlerts,
    ForecastDto Forecast
);

public sealed record FrontOfficeDashboardDto(
    IReadOnlyList<KpiWidget> ReservationKPIs, IReadOnlyList<KpiWidget> CheckInKPIs,
    IReadOnlyList<ChartWidget> OccupancyTimeline, IReadOnlyList<ChartWidget> RoomStatusPie,
    IReadOnlyList<AlertWidget> ArrivalAlerts, IReadOnlyList<AlertWidget> DepartureAlerts,
    int Arrivals, int Departures, int InHouse, int VipInHouse
);

public sealed record OperationsDashboardDto(
    OperationalKpiDto KPIs,
    IReadOnlyList<ChartWidget> HousekeepingStatus, IReadOnlyList<ChartWidget> MaintenanceByCategory,
    IReadOnlyList<AlertWidget> SlaBreaches, IReadOnlyList<AlertWidget> OfflineDevices,
    int RoomsReady, int RoomsCleaning, int RoomsDirty
);

public sealed record FinanceDashboardDto(
    FinancialKpiDto KPIs,
    IReadOnlyList<ChartWidget> RevenueByDay, IReadOnlyList<ChartWidget> PaymentMixPie,
    IReadOnlyList<ChartWidget> OutstandingByChannel,
    IReadOnlyList<AlertWidget> CashVariances, IReadOnlyList<AlertWidget> FolioAlerts,
    decimal TodayRevenue, decimal TodayPayments, decimal TodayRefunds
);

public sealed record EngineeringDashboardDto(
    IReadOnlyList<KpiWidget> LockHealthKPIs, IReadOnlyList<KpiWidget> MaintenanceKPIs,
    IReadOnlyList<ChartWidget> WorkOrdersByStatus, IReadOnlyList<ChartWidget> AssetConditionGauge,
    IReadOnlyList<AlertWidget> CriticalAlerts, IReadOnlyList<AlertWidget> LockAlerts,
    IReadOnlyList<AlertWidget> MaintenanceAlerts
);

// =============================================================================
// ALERT CENTER
// =============================================================================

public sealed record AlertCenterDto(
    int TotalAlerts, int Critical, int Warning, int Info,
    IReadOnlyList<AlertWidget> Unacknowledged, DateTime LastUpdatedUtc
);

// =============================================================================
// DASHBOARD LAYOUT
// =============================================================================

public sealed record DashboardLayout(
    string LayoutId, string Name, string Role, // "GM", "FrontOffice", "Operations", "Finance", "Engineering"
    IReadOnlyList<DashboardWidget> Widgets, bool IsDefault
);

// =============================================================================
// CONTRACTS (Read-Only CQRS)
// =============================================================================

public interface IExecutiveQueryService
{
    Task<ExecutiveKpiDto> GetExecutiveKpisAsync(Guid propertyId, CancellationToken ct);
    Task<OperationalKpiDto> GetOperationalKpisAsync(Guid propertyId, CancellationToken ct);
    Task<FinancialKpiDto> GetFinancialKpisAsync(Guid propertyId, CancellationToken ct);
    Task<ForecastDto> GetForecastAsync(Guid propertyId, CancellationToken ct);
}

public interface IDashboardService
{
    Task<GmDashboardDto> GetGmDashboardAsync(Guid propertyId, CancellationToken ct);
    Task<FrontOfficeDashboardDto> GetFrontOfficeDashboardAsync(Guid propertyId, CancellationToken ct);
    Task<OperationsDashboardDto> GetOperationsDashboardAsync(Guid propertyId, CancellationToken ct);
    Task<FinanceDashboardDto> GetFinanceDashboardAsync(Guid propertyId, CancellationToken ct);
    Task<EngineeringDashboardDto> GetEngineeringDashboardAsync(Guid propertyId, CancellationToken ct);
    Task<DashboardLayout> GetLayoutAsync(string role, CancellationToken ct);
}

public interface IAlertCenterService
{
    Task<AlertCenterDto> GetAlertsAsync(Guid propertyId, CancellationToken ct);
    Task AcknowledgeAlertAsync(string alertId, Guid actorId, CancellationToken ct);
    Task<IReadOnlyList<AlertWidget>> GetCriticalAlertsAsync(Guid propertyId, CancellationToken ct);
}
