using ASMILE.BO.Core.Entities;

namespace ASMILE.BO.Application.Interfaces;

public interface IAccountingPeriodService
{
    Task<AccountingPeriod?> GetCurrentPeriodAsync(DateTime? date = null, CancellationToken cancellationToken = default);
    Task<AccountingPeriod?> GetPeriodByCodeAsync(string periodCode, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AccountingPeriod>> GetPeriodsAsync(int year, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<FiscalYear>> GetFiscalYearsAsync(CancellationToken cancellationToken = default);
    Task<FiscalYear> EnsureFiscalYearAsync(int fiscalYear, string? createdBy = null, CancellationToken cancellationToken = default);
    Task<AccountingPeriod> CreatePeriodAsync(string periodCode, DateTime fromDate, DateTime toDate, string? createdBy = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AccountingPeriod>> CreateFiscalYearPeriodsAsync(int fiscalYear, string? createdBy = null, CancellationToken cancellationToken = default);
    Task<AccountingPeriod> EnsurePeriodAsync(DateTime date, string? createdBy = null, CancellationToken cancellationToken = default);
    Task<AccountingPeriod> LockPeriodAsync(string periodCode, string lockedBy, CancellationToken cancellationToken = default);
    Task<PeriodLock> LockPeriodAsync(string periodCode, Guid? branchId, string lockedBy, string? reason = null, CancellationToken cancellationToken = default);
    Task<AccountingPeriod> UnlockPeriodAsync(string periodCode, string unlockedBy, string reason, CancellationToken cancellationToken = default);
    Task<PeriodLock> UnlockPeriodAsync(string periodCode, Guid? branchId, string unlockedBy, string reason, CancellationToken cancellationToken = default);
    Task<AccountingPeriod> ClosePeriodAsync(string periodCode, string closedBy, CancellationToken cancellationToken = default);
    Task<AccountingPeriod> ReopenPeriodAsync(string periodCode, string reopenedBy, string reason, CancellationToken cancellationToken = default);
    Task<bool> IsDateLockedAsync(DateTime date, CancellationToken cancellationToken = default);
    Task<bool> IsDateLockedAsync(DateTime date, Guid? branchId, CancellationToken cancellationToken = default);
    Task ValidatePostingDateAsync(DateTime postingDate, CancellationToken cancellationToken = default);
    Task ValidatePostingDateAsync(DateTime postingDate, Guid? branchId, CancellationToken cancellationToken = default);
}
