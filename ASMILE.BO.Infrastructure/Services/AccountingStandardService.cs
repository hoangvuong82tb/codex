using ASMILE.BO.Application.Interfaces;
using ASMILE.BO.Core.Entities;
using ASMILE.BO.Core.Interfaces.Services;
using ASMILE.BO.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ASMILE.BO.Infrastructure.Services;

public sealed class AccountingStandardService : IAccountingStandardService
{
    private readonly AppDbContext _dbContext;
    private readonly IAuditLogService? _auditLogService;

    public AccountingStandardService(AppDbContext dbContext, IAuditLogService? auditLogService = null)
    {
        _dbContext = dbContext;
        _auditLogService = auditLogService;
    }

    public async Task<IReadOnlyList<AccountingStandard>> GetStandardsAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.AccountingStandards
            .AsNoTracking()
            .Where(x => x.IsActive)
            .OrderByDescending(x => x.IsDefault)
            .ThenBy(x => x.Code)
            .ToListAsync(cancellationToken);
    }

    public async Task<AccountingStandard?> GetDefaultStandardAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.AccountingStandards
            .AsNoTracking()
            .Where(x => x.IsActive)
            .OrderByDescending(x => x.IsDefault)
            .ThenBy(x => x.Code)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<AccountingStandard> SetDefaultStandardAsync(string standardCode, string? updatedBy = null, CancellationToken cancellationToken = default)
    {
        var standard = await _dbContext.AccountingStandards.FirstOrDefaultAsync(x => x.Code == standardCode && x.IsActive, cancellationToken)
            ?? throw new InvalidOperationException($"Không tìm thấy chuẩn kế toán {standardCode}.");

        var now = DateTime.UtcNow;
        var standards = await _dbContext.AccountingStandards.Where(x => x.IsActive).ToListAsync(cancellationToken);
        foreach (var item in standards)
        {
            item.IsDefault = item.Code == standard.Code;
            item.UpdatedAt = now;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        await LogAsync("AccountingStandard.SetDefault", $"Đặt chuẩn kế toán mặc định: {standard.Code}.", updatedBy, cancellationToken);
        return standard;
    }

    public async Task<IReadOnlyList<AccountTemplate>> GetAccountTemplatesAsync(string standardCode, CancellationToken cancellationToken = default)
    {
        return await _dbContext.AccountTemplates
            .AsNoTracking()
            .Where(x => x.StandardCode == standardCode && x.IsActive)
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.AccountCode)
            .ToListAsync(cancellationToken);
    }

    public async Task SeedStandardTemplatesAsync(CancellationToken cancellationToken = default)
    {
        await UpsertStandardsAsync(cancellationToken);
        await AddMissingAsync(_dbContext.AccountingStandardTemplates, AccountingStandardSeedData.StandardTemplates, x => x.TemplateCode, cancellationToken);
        await AddMissingAsync(_dbContext.AccountTemplates, AccountingStandardSeedData.AccountTemplates, x => $"{x.StandardCode}:{x.AccountCode}", cancellationToken);
        await AddMissingAsync(_dbContext.FinancialStatementMappings, AccountingStandardSeedData.FinancialMappings, x => $"{x.StandardCode}:{x.ReportCode}:{x.LineCode}", cancellationToken);
        await AddMissingRoleMappingsAsync(cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task ApplyStandardToCompanyAsync(string standardCode, Guid? companyId = null, bool overwrite = false, string? appliedBy = null, CancellationToken cancellationToken = default)
    {
        await SeedStandardTemplatesAsync(cancellationToken);
        var standard = await _dbContext.AccountingStandards.FirstOrDefaultAsync(x => x.Code == standardCode && x.IsActive, cancellationToken)
            ?? throw new InvalidOperationException($"Không tìm thấy chuẩn kế toán {standardCode}.");

        var templates = await _dbContext.AccountTemplates
            .Where(x => x.StandardCode == standard.Code && x.IsActive)
            .OrderBy(x => x.SortOrder)
            .ToListAsync(cancellationToken);

        var existingAccounts = await _dbContext.LedgerAccounts.ToDictionaryAsync(x => x.AccountCode, StringComparer.OrdinalIgnoreCase, cancellationToken);
        var now = DateTime.UtcNow;

        foreach (var template in templates)
        {
            if (existingAccounts.TryGetValue(template.AccountCode, out var account))
            {
                if (!overwrite)
                {
                    continue;
                }

                ApplyTemplate(account, template, now);
            }
            else
            {
                _dbContext.LedgerAccounts.Add(CreateLedgerAccount(template, now));
            }
        }

        var profile = await _dbContext.CompanyAccountingProfiles
            .FirstOrDefaultAsync(x => x.CompanyId == companyId && x.IsActive, cancellationToken);
        if (profile is null)
        {
            profile = new CompanyAccountingProfile
            {
                Id = Guid.NewGuid(),
                CompanyId = companyId,
                CompanyName = companyId.HasValue ? null : "Default Company",
                AccountingStandardCode = standard.Code,
                FiscalYearStartMonth = 1,
                FiscalYearStartDay = 1,
                EffectiveFrom = new DateTime(DateTime.Today.Year, 1, 1),
                IsActive = true,
                CreatedAt = now,
                UpdatedAt = now
            };
            _dbContext.CompanyAccountingProfiles.Add(profile);
        }
        else
        {
            profile.AccountingStandardCode = standard.Code;
            profile.UpdatedAt = now;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        await LogAsync("AccountingStandard.Apply", $"Áp dụng chuẩn {standard.Code} cho hồ sơ {(companyId?.ToString() ?? "mặc định")}. Overwrite={overwrite}.", appliedBy, cancellationToken);
    }

    public async Task<IReadOnlyList<FinancialStatementMapping>> GetFinancialMappingsAsync(string standardCode, string reportCode, CancellationToken cancellationToken = default)
    {
        return await _dbContext.FinancialStatementMappings
            .AsNoTracking()
            .Where(x => x.StandardCode == standardCode && x.ReportCode == reportCode && x.IsActive)
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.LineCode)
            .ToListAsync(cancellationToken);
    }

    public async Task<CompanyAccountingProfile> EnsureDefaultProfileAsync(CancellationToken cancellationToken = default)
    {
        await SeedStandardTemplatesAsync(cancellationToken);
        var profile = await _dbContext.CompanyAccountingProfiles.FirstOrDefaultAsync(x => x.CompanyId == null && x.IsActive, cancellationToken);
        if (profile is not null)
        {
            return profile;
        }

        var standard = await _dbContext.AccountingStandards
            .Where(x => x.IsActive)
            .OrderByDescending(x => x.IsDefault)
            .ThenBy(x => x.Code)
            .FirstAsync(cancellationToken);

        profile = new CompanyAccountingProfile
        {
            Id = Guid.NewGuid(),
            CompanyId = null,
            CompanyName = "Default Company",
            AccountingStandardCode = standard.Code,
            FiscalYearStartMonth = 1,
            FiscalYearStartDay = 1,
            EffectiveFrom = new DateTime(DateTime.Today.Year, 1, 1),
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _dbContext.CompanyAccountingProfiles.Add(profile);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return profile;
    }

    private async Task UpsertStandardsAsync(CancellationToken cancellationToken)
    {
        var existing = await _dbContext.AccountingStandards.ToDictionaryAsync(x => x.Code, StringComparer.OrdinalIgnoreCase, cancellationToken);
        foreach (var seed in AccountingStandardSeedData.Standards)
        {
            if (existing.TryGetValue(seed.Code, out var standard))
            {
                standard.Name = seed.Name;
                standard.IsActive = true;
                if (seed.Code == "TT99")
                {
                    standard.IsDefault = true;
                }
                else if (standard.IsDefault && existing.ContainsKey("TT99"))
                {
                    standard.IsDefault = false;
                }
                standard.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                _dbContext.AccountingStandards.Add(new AccountingStandard
                {
                    Id = seed.Id,
                    Code = seed.Code,
                    Name = seed.Name,
                    EffectiveFrom = seed.EffectiveFrom,
                    IsDefault = seed.IsDefault,
                    IsActive = seed.IsActive,
                    CreatedAt = seed.CreatedAt,
                    UpdatedAt = seed.UpdatedAt
                });
            }
        }
    }

    private async Task AddMissingRoleMappingsAsync(CancellationToken cancellationToken)
    {
        var existingKeys = await _dbContext.AccountRoleMappings
            .Select(x => x.AccountingStandardCode + ":" + x.RoleCode + ":" + x.AccountCode)
            .ToListAsync(cancellationToken);
        var existing = existingKeys.ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var mapping in AccountingStandardSeedData.RoleMappings)
        {
            var key = $"{mapping.AccountingStandardCode}:{mapping.RoleCode}:{mapping.AccountCode}";
            if (existing.Contains(key))
            {
                continue;
            }

            _dbContext.AccountRoleMappings.Add(new AccountRoleMapping
            {
                Id = mapping.Id,
                AccountingStandardCode = mapping.AccountingStandardCode,
                RoleCode = mapping.RoleCode,
                AccountCode = mapping.AccountCode,
                IsDefault = mapping.IsDefault,
                IsActive = mapping.IsActive
            });
        }
    }

    private async Task AddMissingAsync<TEntity>(DbSet<TEntity> dbSet, IReadOnlyList<TEntity> seeds, Func<TEntity, string> keySelector, CancellationToken cancellationToken)
        where TEntity : class
    {
        var existing = (await dbSet.AsNoTracking().ToListAsync(cancellationToken))
            .Select(keySelector)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var seed in seeds)
        {
            if (!existing.Contains(keySelector(seed)))
            {
                dbSet.Add(seed);
            }
        }
    }

    private static LedgerAccount CreateLedgerAccount(AccountTemplate template, DateTime now)
    {
        var account = new LedgerAccount
        {
            Id = Guid.NewGuid(),
            AccountCode = template.AccountCode,
            CreatedAt = now,
            UpdatedAt = now,
            IsActive = template.IsActive
        };
        ApplyTemplate(account, template, now);
        return account;
    }

    private static void ApplyTemplate(LedgerAccount account, AccountTemplate template, DateTime now)
    {
        account.SortOrder = template.SortOrder;
        account.AccountName = template.AccountName;
        account.ReportName = template.AccountName;
        account.ParentAccountCode = template.ParentCode;
        account.AccountLevel = template.AccountLevel;
        account.AccountKind = template.AccountKind ?? (template.AllowPosting ? "D" : "H");
        account.AccountNature = template.NormalBalance switch
        {
            "Credit" => "C",
            "Both" => "B",
            _ => "D"
        };
        account.AccountType = template.AccountCategory;
        account.AccountingStandardCode = template.StandardCode;
        account.AllowPosting = template.AllowPosting;
        account.IsSystemAccount = template.IsSystemAccount;
        account.IsEditable = !template.IsSystemAccount;
        account.IsDeletable = !template.IsSystemAccount;
        account.NormalBalance = template.NormalBalance;
        account.AccountCategory = template.AccountCategory;
        account.RequireCustomer = template.RequireCustomer;
        account.RequireSupplier = template.RequireSupplier;
        account.RequireWarehouse = template.RequireWarehouse;
        account.RequireItem = template.RequireItem;
        account.RequireDepartment = template.RequireDepartment;
        account.RequireEmployee = template.RequireEmployee;
        account.RequireCurrency = template.RequireCurrency;
        account.IsFinancialAccount = template.IsFinancialAccount;
        account.IsManagementAccount = template.IsManagementAccount;
        account.IsActive = template.IsActive;
        account.UpdatedAt = now;
    }

    private async Task LogAsync(string action, string detail, string? username, CancellationToken cancellationToken)
    {
        if (_auditLogService is not null)
        {
            await _auditLogService.LogAsync(action, detail, string.IsNullOrWhiteSpace(username) ? "system" : username, cancellationToken);
        }
    }
}
