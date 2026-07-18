using ASMILE.BO.Application.Interfaces;
using ASMILE.BO.Core.Entities;
using ASMILE.BO.Core.Enums;
using ASMILE.BO.Core.Interfaces.Services;
using ASMILE.BO.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ASMILE.BO.Infrastructure.Services;

public sealed class AccountingPeriodService : IAccountingPeriodService
{
    private readonly AppDbContext _dbContext;
    private readonly IAuditLogService? _auditLogService;
    private readonly ICurrentCompanyContext? _companyContext;
    private readonly ICurrentUserService? _currentUserService;

    public AccountingPeriodService(
        AppDbContext dbContext,
        IAuditLogService? auditLogService = null,
        ICurrentCompanyContext? companyContext = null,
        ICurrentUserService? currentUserService = null)
    {
        _dbContext = dbContext;
        _auditLogService = auditLogService;
        _companyContext = companyContext;
        _currentUserService = currentUserService;
    }

    public async Task<AccountingPeriod?> GetCurrentPeriodAsync(DateTime? date = null, CancellationToken cancellationToken = default)
    {
        var targetDate = (date ?? DateTime.Today).Date;
        return await GetPeriodByCodeAsync(ToPeriodCode(targetDate), cancellationToken);
    }

    public async Task<AccountingPeriod?> GetPeriodByCodeAsync(string periodCode, CancellationToken cancellationToken = default)
    {
        var normalized = NormalizePeriodCode(periodCode);
        return await _dbContext.AccountingPeriods.FirstOrDefaultAsync(x => x.PeriodCode == normalized, cancellationToken);
    }

    public async Task<IReadOnlyList<AccountingPeriod>> GetPeriodsAsync(int year, CancellationToken cancellationToken = default)
    {
        var prefix = $"{year:D4}-";
        return await _dbContext.AccountingPeriods
            .AsNoTracking()
            .Where(x => x.PeriodCode.StartsWith(prefix))
            .OrderBy(x => x.PeriodCode)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<FiscalYear>> GetFiscalYearsAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.FiscalYears
            .AsNoTracking()
            .OrderByDescending(x => x.Year)
            .ToListAsync(cancellationToken);
    }

    public async Task<FiscalYear> EnsureFiscalYearAsync(int fiscalYear, string? createdBy = null, CancellationToken cancellationToken = default)
    {
        var existing = await _dbContext.FiscalYears.FirstOrDefaultAsync(x => x.Year == fiscalYear, cancellationToken);
        if (existing is not null)
        {
            return existing;
        }

        var now = DateTime.UtcNow;
        var entity = new FiscalYear
        {
            Id = Guid.NewGuid(),
            Year = fiscalYear,
            FiscalYearCode = fiscalYear.ToString("D4"),
            FromDate = new DateTime(fiscalYear, 1, 1),
            ToDate = new DateTime(fiscalYear, 12, 31),
            Status = "Open",
            CreatedAt = now,
            UpdatedAt = now
        };

        _dbContext.FiscalYears.Add(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await LogAsync("FiscalYear.Create", $"Create fiscal year {entity.FiscalYearCode}.", createdBy, cancellationToken);
        return entity;
    }

    public async Task<AccountingPeriod> CreatePeriodAsync(string periodCode, DateTime fromDate, DateTime toDate, string? createdBy = null, CancellationToken cancellationToken = default)
    {
        var normalized = NormalizePeriodCode(periodCode);
        if (toDate.Date < fromDate.Date)
        {
            throw new InvalidOperationException("Period end date cannot be earlier than start date.");
        }

        if (await GetPeriodByCodeAsync(normalized, cancellationToken) is not null)
        {
            throw new InvalidOperationException($"Accounting period {normalized} already exists.");
        }

        var now = DateTime.UtcNow;
        var period = new AccountingPeriod
        {
            Id = Guid.NewGuid(),
            PeriodCode = normalized,
            FromDate = fromDate.Date,
            ToDate = toDate.Date,
            Status = AccountingPeriodStatus.Open,
            CreatedAt = now,
            UpdatedAt = now
        };

        _dbContext.AccountingPeriods.Add(period);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await LogAsync("AccountingPeriod.Create", $"Create accounting period {period.PeriodCode}.", createdBy, cancellationToken);
        return period;
    }

    public async Task<IReadOnlyList<AccountingPeriod>> CreateFiscalYearPeriodsAsync(int fiscalYear, string? createdBy = null, CancellationToken cancellationToken = default)
    {
        await EnsureFiscalYearAsync(fiscalYear, createdBy, cancellationToken);
        var periods = new List<AccountingPeriod>(12);
        for (var month = 1; month <= 12; month++)
        {
            var fromDate = new DateTime(fiscalYear, month, 1);
            var periodCode = ToPeriodCode(fromDate);
            var existing = await GetPeriodByCodeAsync(periodCode, cancellationToken);
            periods.Add(existing ?? await CreatePeriodAsync(periodCode, fromDate, fromDate.AddMonths(1).AddDays(-1), createdBy, cancellationToken));
        }

        return periods;
    }

    public async Task<AccountingPeriod> EnsurePeriodAsync(DateTime date, string? createdBy = null, CancellationToken cancellationToken = default)
    {
        var targetDate = date.Date;
        var periodCode = ToPeriodCode(targetDate);
        var existing = await GetPeriodByCodeAsync(periodCode, cancellationToken);
        if (existing is not null)
        {
            return existing;
        }

        var fromDate = new DateTime(targetDate.Year, targetDate.Month, 1);
        return await CreatePeriodAsync(periodCode, fromDate, fromDate.AddMonths(1).AddDays(-1), createdBy, cancellationToken);
    }

    public async Task<AccountingPeriod> LockPeriodAsync(string periodCode, string lockedBy, CancellationToken cancellationToken = default)
    {
        var period = await GetRequiredPeriodAsync(periodCode, cancellationToken);
        if (period.Status != AccountingPeriodStatus.Open)
        {
            throw new InvalidOperationException($"Only open periods can be locked. Period {period.PeriodCode} is {period.Status}.");
        }

        var user = NormalizeUser(lockedBy);
        period.Status = AccountingPeriodStatus.Locked;
        period.LockedAt = DateTime.UtcNow;
        period.LockedBy = user;
        period.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
        await UpsertPeriodLockAsync(period.PeriodCode, null, user, null, true, cancellationToken);
        await LogAsync("AccountingPeriod.Lock", $"Lock accounting period {period.PeriodCode}.", user, cancellationToken);
        return period;
    }

    public async Task<PeriodLock> LockPeriodAsync(string periodCode, Guid? branchId, string lockedBy, string? reason = null, CancellationToken cancellationToken = default)
    {
        var normalized = NormalizePeriodCode(periodCode);
        await EnsurePeriodFromCodeAsync(normalized, lockedBy, cancellationToken);
        var user = NormalizeUser(lockedBy);
        var periodLock = await UpsertPeriodLockAsync(normalized, branchId, user, reason, true, cancellationToken);
        await LogAsync("PeriodLock.Lock", $"Lock period {normalized}{FormatBranch(branchId)}.", user, cancellationToken);
        return periodLock;
    }

    public async Task<AccountingPeriod> UnlockPeriodAsync(string periodCode, string unlockedBy, string reason, CancellationToken cancellationToken = default)
    {
        var period = await GetRequiredPeriodAsync(periodCode, cancellationToken);
        if (period.Status != AccountingPeriodStatus.Locked)
        {
            throw new InvalidOperationException($"Only locked periods can be unlocked. Period {period.PeriodCode} is {period.Status}.");
        }

        var user = NormalizeUser(unlockedBy);
        period.Status = AccountingPeriodStatus.Open;
        period.LockedAt = null;
        period.LockedBy = null;
        period.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
        await UpsertPeriodLockAsync(period.PeriodCode, null, user, reason, false, cancellationToken);
        await LogAsync("AccountingPeriod.Unlock", $"Unlock accounting period {period.PeriodCode}. Reason: {reason}", user, cancellationToken);
        return period;
    }

    public async Task<PeriodLock> UnlockPeriodAsync(string periodCode, Guid? branchId, string unlockedBy, string reason, CancellationToken cancellationToken = default)
    {
        var normalized = NormalizePeriodCode(periodCode);
        var user = NormalizeUser(unlockedBy);
        var periodLock = await UpsertPeriodLockAsync(normalized, branchId, user, reason, false, cancellationToken);
        await LogAsync("PeriodLock.Unlock", $"Unlock period {normalized}{FormatBranch(branchId)}. Reason: {reason}", user, cancellationToken);
        return periodLock;
    }

    public async Task<AccountingPeriod> ClosePeriodAsync(string periodCode, string closedBy, CancellationToken cancellationToken = default)
    {
        var period = await GetRequiredPeriodAsync(periodCode, cancellationToken);
        if (period.Status == AccountingPeriodStatus.Closed)
        {
            throw new InvalidOperationException($"Accounting period {period.PeriodCode} is already closed.");
        }

        var user = NormalizeUser(closedBy);
        period.Status = AccountingPeriodStatus.Closed;
        period.LockedAt ??= DateTime.UtcNow;
        period.LockedBy = user;
        period.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
        await UpsertPeriodLockAsync(period.PeriodCode, null, user, "Period closed.", true, cancellationToken);
        await LogAsync("AccountingPeriod.Close", $"Close accounting period {period.PeriodCode}.", user, cancellationToken);
        return period;
    }

    public async Task<AccountingPeriod> ReopenPeriodAsync(string periodCode, string reopenedBy, string reason, CancellationToken cancellationToken = default)
    {
        var period = await GetRequiredPeriodAsync(periodCode, cancellationToken);
        if (period.Status != AccountingPeriodStatus.Closed)
        {
            throw new InvalidOperationException($"Only closed periods can be reopened. Period {period.PeriodCode} is {period.Status}.");
        }

        var user = NormalizeUser(reopenedBy);
        period.Status = AccountingPeriodStatus.Open;
        period.LockedAt = null;
        period.LockedBy = null;
        period.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
        await UpsertPeriodLockAsync(period.PeriodCode, null, user, reason, false, cancellationToken);
        await LogAsync("AccountingPeriod.Reopen", $"Reopen accounting period {period.PeriodCode}. Reason: {reason}", user, cancellationToken);
        return period;
    }

    public async Task<bool> IsDateLockedAsync(DateTime date, CancellationToken cancellationToken = default)
        => await IsDateLockedAsync(date, _companyContext?.BranchId, cancellationToken);

    public async Task<bool> IsDateLockedAsync(DateTime date, Guid? branchId, CancellationToken cancellationToken = default)
    {
        var period = await EnsurePeriodAsync(date, null, cancellationToken);
        if (period.Status is AccountingPeriodStatus.Locked or AccountingPeriodStatus.Closed)
        {
            return !HasPeriodLockOverride();
        }

        var locked = await HasActivePeriodLockAsync(period.PeriodCode, branchId, cancellationToken);
        return locked && !HasPeriodLockOverride();
    }

    public async Task ValidatePostingDateAsync(DateTime postingDate, CancellationToken cancellationToken = default)
        => await ValidatePostingDateAsync(postingDate, _companyContext?.BranchId, cancellationToken);

    public async Task ValidatePostingDateAsync(DateTime postingDate, Guid? branchId, CancellationToken cancellationToken = default)
    {
        var period = await EnsurePeriodAsync(postingDate, null, cancellationToken);
        if (await IsDateLockedAsync(postingDate, branchId, cancellationToken))
        {
            throw new InvalidOperationException($"Accounting period {period.PeriodCode} is locked; posting is not allowed.");
        }
    }

    private async Task<AccountingPeriod> EnsurePeriodFromCodeAsync(string periodCode, string? createdBy, CancellationToken cancellationToken)
    {
        var normalized = NormalizePeriodCode(periodCode);
        var year = int.Parse(normalized[..4]);
        var month = int.Parse(normalized[5..7]);
        return await EnsurePeriodAsync(new DateTime(year, month, 1), createdBy, cancellationToken);
    }

    private async Task<AccountingPeriod> GetRequiredPeriodAsync(string periodCode, CancellationToken cancellationToken)
    {
        var normalized = NormalizePeriodCode(periodCode);
        return await GetPeriodByCodeAsync(normalized, cancellationToken)
            ?? throw new InvalidOperationException($"Accounting period {normalized} was not found.");
    }

    private async Task<PeriodLock> UpsertPeriodLockAsync(string periodCode, Guid? branchId, string user, string? reason, bool isLocked, CancellationToken cancellationToken)
    {
        var normalized = NormalizePeriodCode(periodCode);
        var companyId = _companyContext?.CompanyId ?? _dbContext.CurrentCompanyId;
        var existing = await _dbContext.PeriodLocks
            .FirstOrDefaultAsync(x => x.CompanyId == companyId && x.BranchId == branchId && x.PeriodCode == normalized, cancellationToken);
        var now = DateTime.UtcNow;

        if (existing is null)
        {
            existing = new PeriodLock
            {
                Id = Guid.NewGuid(),
                CompanyId = companyId,
                BranchId = branchId,
                PeriodCode = normalized,
                LockedAt = now,
                LockedBy = user,
                CreatedAt = now
            };
            _dbContext.PeriodLocks.Add(existing);
        }

        existing.IsLocked = isLocked;
        existing.UpdatedAt = now;
        if (isLocked)
        {
            existing.LockedAt = now;
            existing.LockedBy = user;
            existing.LockReason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
            existing.UnlockedAt = null;
            existing.UnlockedBy = null;
            existing.UnlockReason = null;
        }
        else
        {
            existing.UnlockedAt = now;
            existing.UnlockedBy = user;
            existing.UnlockReason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return existing;
    }

    private async Task<bool> HasActivePeriodLockAsync(string periodCode, Guid? branchId, CancellationToken cancellationToken)
    {
        var normalized = NormalizePeriodCode(periodCode);
        return await _dbContext.PeriodLocks
            .AsNoTracking()
            .Where(x => x.PeriodCode == normalized && x.IsLocked)
            .Where(x => x.BranchId == null || (branchId.HasValue && x.BranchId == branchId))
            .AnyAsync(cancellationToken);
    }

    private bool HasPeriodLockOverride()
    {
        var roles = _currentUserService?.CurrentUser?.Roles ?? [];
        return roles.Any(role =>
            string.Equals(role, "Administrator", StringComparison.OrdinalIgnoreCase)
            || string.Equals(role, "AccountingManager", StringComparison.OrdinalIgnoreCase)
            || string.Equals(role, "Accounting Manager", StringComparison.OrdinalIgnoreCase));
    }

    private static string NormalizePeriodCode(string periodCode)
    {
        if (DateTime.TryParse($"{periodCode?.Trim()}-01", out var parsed))
        {
            return ToPeriodCode(parsed);
        }

        throw new InvalidOperationException("PeriodCode must use yyyy-MM format.");
    }

    private static string NormalizeUser(string? user) => string.IsNullOrWhiteSpace(user) ? "system" : user.Trim();

    private static string ToPeriodCode(DateTime date) => date.ToString("yyyy-MM");

    private static string FormatBranch(Guid? branchId) => branchId.HasValue ? $" for branch {branchId.Value:N}" : string.Empty;

    private async Task LogAsync(string action, string detail, string? username, CancellationToken cancellationToken)
    {
        if (_auditLogService is not null)
        {
            await _auditLogService.LogAsync(action, detail, NormalizeUser(username), cancellationToken);
        }
    }
}
