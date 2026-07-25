using ASMILE.BO.Application.FrontOffice.Cashier;
using ASMILE.BO.Infrastructure.FrontOffice.Cashier;
using FluentAssertions;

namespace ASMILE.BO.Tests.Unit;

public sealed class CashierTests
{
    [Fact] public void CSH_001_open_drawer() { new CashDrawerService().OpenDrawerAsync(Guid.NewGuid(), Guid.NewGuid(), 3000000, default).Result.Status.Should().Be(DrawerStatus.Open); }
    [Fact] public void CSH_002_close_drawer_returns_closed() { new CashDrawerService().CloseDrawerAsync(Guid.NewGuid(), 5200000, default).Result.Status.Should().Be(DrawerStatus.Closed); }
    [Fact] public void CSH_003_get_drawer_has_variance() { new CashDrawerService().GetDrawerAsync(Guid.NewGuid(), default).Result.Variance.Should().NotBe(0); }
    [Fact] public void CSH_004_open_shift() { var s = new CashShiftService().OpenShiftAsync(Guid.NewGuid(), Guid.NewGuid(), 3000000, default).Result; s.Status.Should().Be(Core.FrontOffice.Cashier.CashierShiftStatus.Open); s.OpeningCash.Should().Be(3000000); }
    [Fact] public void CSH_005_close_shift() { new CashShiftService().CloseShiftAsync(Guid.NewGuid(), 4800000, "Counting error", default).Result.Status.Should().Be(Core.FrontOffice.Cashier.CashierShiftStatus.Closed); }
    [Fact] public void CSH_006_handover_has_variance() { new CashShiftService().HandOverAsync(Guid.NewGuid(), Guid.NewGuid(), 5100000, "Counted", default).Result.Variance.Should().NotBe(0); }
    [Fact] public void CSH_007_reconcile_detects_variance() { new CashReconciliationService().ReconcileAsync(Guid.NewGuid(), 4800000, default).Result.Status.Should().Be(ReconciliationStatus.VarianceDetected); }
    [Fact] public void CSH_008_approve_variance() { new CashReconciliationService().ApproveVarianceAsync(Guid.NewGuid(), Guid.NewGuid(), "Approved by manager", default).Result.Status.Should().Be(ReconciliationStatus.Approved); }
    [Fact] public void CSH_009_cashier_dashboard() { new CashierDashboardService().GetCashierDashboardAsync(Guid.NewGuid(), default).Result.TransactionsToday.Should().BeGreaterThan(0); }
    [Fact] public void CSH_010_supervisor_dashboard() { new CashierDashboardService().GetSupervisorDashboardAsync(Guid.NewGuid(), default).Result.OpenDrawerCount.Should().BeGreaterThan(0); }
    [Fact] public void CSH_011_treasury_dashboard() { new CashierDashboardService().GetTreasuryDashboardAsync(Guid.NewGuid(), default).Result.TotalCashOnHand.Should().BeGreaterThan(0); }
    [Fact] public void CSH_012_events_unique() { typeof(CashierEvents).GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static).Select(f => f.GetValue(null)).Should().OnlyHaveUniqueItems(); }
}
