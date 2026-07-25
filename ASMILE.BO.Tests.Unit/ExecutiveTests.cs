using ASMILE.BO.Application.FrontOffice.Executive;
using ASMILE.BO.Infrastructure.FrontOffice.Executive;
using FluentAssertions;

namespace ASMILE.BO.Tests.Unit;

public sealed class ExecutiveTests
{
    [Fact] public void EXEC_001_executive_kpis_have_occupancy() => new ExecutiveQueryService().GetExecutiveKpisAsync(Guid.NewGuid(), default).Result.OccupancyPct.Should().BeGreaterThan(0);
    [Fact] public void EXEC_002_executive_kpis_have_adr() => new ExecutiveQueryService().GetExecutiveKpisAsync(Guid.NewGuid(), default).Result.ADR.Should().BeGreaterThan(0);
    [Fact] public void EXEC_003_executive_kpis_have_revpar() => new ExecutiveQueryService().GetExecutiveKpisAsync(Guid.NewGuid(), default).Result.RevPAR.Should().BeGreaterThan(0);
    [Fact] public void EXEC_004_operational_kpis_room_readiness() => new ExecutiveQueryService().GetOperationalKpisAsync(Guid.NewGuid(), default).Result.RoomReadinessPct.Should().BeGreaterThan(0);
    [Fact] public void EXEC_005_financial_kpis_outstanding() => new ExecutiveQueryService().GetFinancialKpisAsync(Guid.NewGuid(), default).Result.OutstandingBalance.Should().BeGreaterThan(0);
    [Fact] public void EXEC_006_forecast_has_7day_projection() => new ExecutiveQueryService().GetForecastAsync(Guid.NewGuid(), default).Result.OccupancyForecast7.Should().BeGreaterThan(0);
    [Fact] public void EXEC_007_gm_dashboard_has_revenue_kpis() => new DashboardService().GetGmDashboardAsync(Guid.NewGuid(), default).Result.RevenueKPIs.Should().NotBeEmpty();
    [Fact] public void EXEC_008_front_office_dashboard_arrivals() => new DashboardService().GetFrontOfficeDashboardAsync(Guid.NewGuid(), default).Result.Arrivals.Should().BeGreaterThan(0);
    [Fact] public void EXEC_009_operations_dashboard_rooms_ready() => new DashboardService().GetOperationsDashboardAsync(Guid.NewGuid(), default).Result.RoomsReady.Should().BeGreaterThan(0);
    [Fact] public void EXEC_010_finance_dashboard_revenue() => new DashboardService().GetFinanceDashboardAsync(Guid.NewGuid(), default).Result.TodayRevenue.Should().BeGreaterThan(0);
    [Fact] public void EXEC_011_engineering_dashboard_lock_kpis() => new DashboardService().GetEngineeringDashboardAsync(Guid.NewGuid(), default).Result.LockHealthKPIs.Should().NotBeEmpty();
    [Fact] public void EXEC_012_dashboard_layout_has_widgets() => new DashboardService().GetLayoutAsync("GM", default).Result.Widgets.Should().NotBeEmpty();
    [Fact] public void EXEC_013_alert_center_has_alerts() => new AlertCenterService().GetAlertsAsync(Guid.NewGuid(), default).Result.TotalAlerts.Should().BeGreaterThan(0);
    [Fact] public void EXEC_014_critical_alerts_non_empty() => new AlertCenterService().GetCriticalAlertsAsync(Guid.NewGuid(), default).Result.Should().NotBeEmpty();
}
