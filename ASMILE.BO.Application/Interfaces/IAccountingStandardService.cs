using ASMILE.BO.Core.Entities;

namespace ASMILE.BO.Application.Interfaces;

public interface IAccountingStandardService
{
    Task<IReadOnlyList<AccountingStandard>> GetStandardsAsync(CancellationToken cancellationToken = default);
    Task<AccountingStandard?> GetDefaultStandardAsync(CancellationToken cancellationToken = default);
    Task<AccountingStandard> SetDefaultStandardAsync(string standardCode, string? updatedBy = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AccountTemplate>> GetAccountTemplatesAsync(string standardCode, CancellationToken cancellationToken = default);
    Task SeedStandardTemplatesAsync(CancellationToken cancellationToken = default);
    Task ApplyStandardToCompanyAsync(string standardCode, Guid? companyId = null, bool overwrite = false, string? appliedBy = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<FinancialStatementMapping>> GetFinancialMappingsAsync(string standardCode, string reportCode, CancellationToken cancellationToken = default);
    Task<CompanyAccountingProfile> EnsureDefaultProfileAsync(CancellationToken cancellationToken = default);
}
