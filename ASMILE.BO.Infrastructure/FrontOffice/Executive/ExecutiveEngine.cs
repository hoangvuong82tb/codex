using ASMILE.BO.Application.FrontOffice.Executive;

namespace ASMILE.BO.Infrastructure.FrontOffice.Executive;

public sealed class ExecutiveQueryService : IExecutiveQueryService
{
    public Task<ExecutiveKpiDto> GetExecutiveKpisAsync(Guid propertyId, CancellationToken ct) => Task.FromResult(new ExecutiveKpiDto(78m, 82m, KpiTrend.Up, 1850000m, 1780000m, KpiTrend.Up, 1443000m, 1460000m, KpiTrend.Flat, 28, 22, 85, 45, 3, 2, 85000000m, 1800000000m, 22000000000m, 65000000m, 15000000m, 5000000m, 75m, 280000000m, 1450000m));
    public Task<OperationalKpiDto> GetOperationalKpisAsync(Guid propertyId, CancellationToken ct) => Task.FromResult(new OperationalKpiDto(120, 70, 10, 30, 5, 88m, 95m, 12, 92m, 200000m, 0, 115, 3, 96m, 3, 98m, 12));
    public Task<FinancialKpiDto> GetFinancialKpisAsync(Guid propertyId, CancellationToken ct) => Task.FromResult(new FinancialKpiDto(85000000m, 95000000m, 65000000m, 20000000m, 3000000m, 15000000m, 25000000m, 35m, 45m, 20m, 85, 120, 20000000m, 3000000m));
    public Task<ForecastDto> GetForecastAsync(Guid propertyId, CancellationToken ct) => Task.FromResult(new ForecastDto(78m, 82m, 85m, 280000000m, 1200000000m, 3800000000m, 65m, 40m, 25, 30, 18));
}

public sealed class DashboardService : IDashboardService
{
    public Task<GmDashboardDto> GetGmDashboardAsync(Guid propertyId, CancellationToken ct)
    {
        var kpis = new ExecutiveKpiDto(78m, 82m, KpiTrend.Up, 1850000m, 1780000m, KpiTrend.Up, 1443000m, 1460000m, KpiTrend.Flat, 28, 22, 85, 45, 3, 2, 85000000m, 1800000000m, 22000000000m, 65000000m, 15000000m, 5000000m, 75m, 280000000m, 1450000m);
        var revKpis = new List<KpiWidget> { new("REV-TODAY", "Revenue Today", "85,000,000", "VND", "15M above target", KpiTrend.Up, 8.5m, "/finance/revenue", "#16A34A") };
        return Task.FromResult(new GmDashboardDto(kpis, revKpis, [], [], [], new ForecastDto(78m, 82m, 85m, 280000000m, 1200000000m, 3800000000m, 65m, 40m, 25, 30, 18)));
    }

    public Task<FrontOfficeDashboardDto> GetFrontOfficeDashboardAsync(Guid propertyId, CancellationToken ct)
    {
        var resKpis = new List<KpiWidget> { new("RES-ARR", "Arrivals Today", "28", "#", null, KpiTrend.Up, 12m, "/fo/arrivals", "#2563EB") };
        return Task.FromResult(new FrontOfficeDashboardDto(resKpis, resKpis, [], [], [], [], 28, 22, 85, 5));
    }

    public Task<OperationsDashboardDto> GetOperationsDashboardAsync(Guid propertyId, CancellationToken ct)
    {
        var opKpis = new OperationalKpiDto(120, 70, 10, 30, 5, 88m, 95m, 12, 92m, 200000m, 0, 115, 3, 96m, 3, 98m, 12);
        return Task.FromResult(new OperationsDashboardDto(opKpis, [], [], [], [], 70, 10, 5));
    }

    public Task<FinanceDashboardDto> GetFinanceDashboardAsync(Guid propertyId, CancellationToken ct)
    {
        var finKpis = new FinancialKpiDto(85000000m, 95000000m, 65000000m, 20000000m, 3000000m, 15000000m, 25000000m, 35m, 45m, 20m, 85, 120, 20000000m, 3000000m);
        return Task.FromResult(new FinanceDashboardDto(finKpis, [], [], [], [], [], 85000000m, 65000000m, 3000000m));
    }

    public Task<EngineeringDashboardDto> GetEngineeringDashboardAsync(Guid propertyId, CancellationToken ct)
    {
        var lockKpis = new List<KpiWidget> { new("LOCK-ONLINE", "Locks Online", "115/120", "96%", null, KpiTrend.Up, 1m, "/engineering/locks", "#16A34A") };
        return Task.FromResult(new EngineeringDashboardDto(lockKpis, lockKpis, [], [], [], [], []));
    }

    public Task<DashboardLayout> GetLayoutAsync(string role, CancellationToken ct)
    {
        var widgets = new List<DashboardWidget> { new("W-001", "Revenue Today", WidgetType.KPI, WidgetSize.Small, 0, 0, null, false, true, 30), new("W-002", "Occupancy Trend", WidgetType.Chart, WidgetSize.Large, 1, 0, null, true, true, 60) };
        return Task.FromResult(new DashboardLayout("GM-LAYOUT", "GM Cockpit", role, widgets, true));
    }
}

public sealed class AlertCenterService : IAlertCenterService
{
    public Task<AlertCenterDto> GetAlertsAsync(Guid propertyId, CancellationToken ct) => Task.FromResult(new AlertCenterDto(12, 2, 5, 5, [new AlertWidget("A-001", AlertLevel.Critical, "Night Audit Failed", "Night audit at step 7", "NightAudit", DateTime.UtcNow.AddMinutes(-15), "/nightaudit/resume", false)], DateTime.UtcNow));
    public Task AcknowledgeAlertAsync(string alertId, Guid actorId, CancellationToken ct) => Task.CompletedTask;
    public Task<IReadOnlyList<AlertWidget>> GetCriticalAlertsAsync(Guid propertyId, CancellationToken ct) => Task.FromResult<IReadOnlyList<AlertWidget>>([new AlertWidget("A-001", AlertLevel.Critical, "Door Offline", "Room 305 lock offline for 2h", "DoorLock", DateTime.UtcNow.AddHours(-2), "/engineering/locks/305", false)]);
}
