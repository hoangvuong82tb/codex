using ASMILE.BO.Application.FrontOffice.Cashier;
using ASMILE.BO.Core.FrontOffice.Cashier;

namespace ASMILE.BO.Infrastructure.FrontOffice.Cashier;

public sealed class CashDrawerService : ICashDrawerService
{
    public Task<CashDrawerDto> OpenDrawerAsync(Guid drawerId, Guid cashierId, decimal openingFloat, CancellationToken ct) => Task.FromResult(new CashDrawerDto(drawerId, "DWR-001", Guid.NewGuid(), cashierId, "Cashier A", DrawerStatus.Open, openingFloat, openingFloat, 0, DateTime.UtcNow, null));
    public Task<CashDrawerDto> CloseDrawerAsync(Guid drawerId, decimal actualCash, CancellationToken ct) => Task.FromResult(new CashDrawerDto(drawerId, "DWR-001", null, null, null, DrawerStatus.Closed, 5000000, actualCash, actualCash - 5000000, DateTime.UtcNow.AddHours(-8), DateTime.UtcNow));
    public Task<CashDrawerDto> GetDrawerAsync(Guid drawerId, CancellationToken ct) => Task.FromResult(new CashDrawerDto(drawerId, "DWR-001", Guid.NewGuid(), Guid.NewGuid(), "Cashier A", DrawerStatus.Open, 5000000, 5200000, 200000, DateTime.UtcNow.AddHours(-4), null));
    public Task<IReadOnlyList<CashDrawerDto>> GetOpenDrawersAsync(Guid propertyId, CancellationToken ct) => Task.FromResult<IReadOnlyList<CashDrawerDto>>([new CashDrawerDto(Guid.NewGuid(), "DWR-001", Guid.NewGuid(), Guid.NewGuid(), "Cashier A", DrawerStatus.Open, 5000000, 5000000, 0, DateTime.UtcNow.AddHours(-4), null)]);
}

public sealed class CashShiftService : ICashShiftService
{
    public Task<CashShiftDto> OpenShiftAsync(Guid drawerId, Guid cashierId, decimal openingFloat, CancellationToken ct) { var m = new List<CashMovementDto> { new(Guid.NewGuid(), CashMovementType.OpeningCash, openingFloat, "Opening float", PaymentCurrency.VND, 1, null, DateTime.UtcNow) }; return Task.FromResult(new CashShiftDto(Guid.NewGuid(), "SHF-20260720-001", drawerId, cashierId, "Cashier A", CashierShiftStatus.Open, openingFloat, openingFloat, null, null, null, DateTime.UtcNow, null, null, m)); }
    public Task<CashShiftDto> CloseShiftAsync(Guid shiftId, decimal actualCash, string? varianceReason, CancellationToken ct) => Task.FromResult(new CashShiftDto(shiftId, "SHF-20260720-001", Guid.NewGuid(), Guid.NewGuid(), "Cashier A", CashierShiftStatus.Closed, 5000000, 5000000, actualCash, actualCash - 5000000, varianceReason, DateTime.UtcNow.AddHours(-8), DateTime.UtcNow, Guid.NewGuid(), []));
    public Task<ShiftHandoverDto> HandOverAsync(Guid fromShiftId, Guid toCashierId, decimal countedCash, string? notes, CancellationToken ct) => Task.FromResult(new ShiftHandoverDto(fromShiftId, Guid.NewGuid(), Guid.NewGuid(), toCashierId, countedCash, 5200000, countedCash - 5200000, notes, DateTime.UtcNow));
    public Task<CashShiftDto> GetShiftAsync(Guid shiftId, CancellationToken ct) => Task.FromResult(new CashShiftDto(shiftId, "SHF-001", Guid.NewGuid(), Guid.NewGuid(), "Cashier A", CashierShiftStatus.Open, 3000000, 4500000, null, null, null, DateTime.UtcNow.AddHours(-5), null, null, []));
    public Task<IReadOnlyList<CashShiftDto>> GetActiveShiftsAsync(Guid propertyId, CancellationToken ct) => Task.FromResult<IReadOnlyList<CashShiftDto>>([new CashShiftDto(Guid.NewGuid(), "SHF-001", Guid.NewGuid(), Guid.NewGuid(), "Cashier A", CashierShiftStatus.Open, 3000000, 4500000, null, null, null, DateTime.UtcNow.AddHours(-5), null, null, [])]);
}

public sealed class CashReconciliationService : ICashReconciliationService
{
    public Task<CashReconciliationDto> ReconcileAsync(Guid shiftId, decimal actualCash, CancellationToken ct) { var variance = actualCash - 5000000; return Task.FromResult(new CashReconciliationDto(Guid.NewGuid(), shiftId, "SHF-001", 5000000, actualCash, variance, variance == 0 ? ReconciliationStatus.Completed : ReconciliationStatus.VarianceDetected, variance != 0 ? VarianceReasonCode.RoundingDifference : null, null, null, null, null, null)); }
    public Task<CashReconciliationDto> ApproveVarianceAsync(Guid reconciliationId, Guid approverId, string reason, CancellationToken ct) => Task.FromResult(new CashReconciliationDto(reconciliationId, Guid.NewGuid(), "SHF-001", 5000000, 4800000, -200000, ReconciliationStatus.Approved, VarianceReasonCode.Other, reason, Guid.NewGuid(), DateTime.UtcNow, approverId, DateTime.UtcNow));
    public Task<IReadOnlyList<CashReconciliationDto>> GetPendingAsync(Guid propertyId, CancellationToken ct) => Task.FromResult<IReadOnlyList<CashReconciliationDto>>([new CashReconciliationDto(Guid.NewGuid(), Guid.NewGuid(), "SHF-001", 5000000, 4800000, -200000, ReconciliationStatus.VarianceDetected, VarianceReasonCode.CountingError, null, null, null, null, null)]);
}

public sealed class CashierDashboardService : ICashierDashboardService
{
    public Task<CashierDashboardDto> GetCashierDashboardAsync(Guid cashierId, CancellationToken ct) => Task.FromResult(new CashierDashboardDto(cashierId, "Cashier A", Guid.NewGuid(), "DWR-001", DrawerStatus.Open, 6500000, 3000000, 45, 8500000, 5000000, false, DateTime.UtcNow.AddHours(-5)));
    public Task<CashierSupervisorDashboardDto> GetSupervisorDashboardAsync(Guid propertyId, CancellationToken ct) => Task.FromResult(new CashierSupervisorDashboardDto(3, 3, 1, 1, 200000, [], []));
    public Task<TreasuryDashboardDto> GetTreasuryDashboardAsync(Guid propertyId, CancellationToken ct) => Task.FromResult(new TreasuryDashboardDto(25000000, 15000000, 5000000, 3, 12, 200000));
}
