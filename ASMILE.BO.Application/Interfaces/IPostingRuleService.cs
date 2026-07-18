using ASMILE.BO.Core.Entities;

namespace ASMILE.BO.Application.Interfaces;

public interface IPostingRuleService
{
    Task<PostingRule?> GetActiveRuleAsync(string sourceType, string accountingStandardCode, CancellationToken cancellationToken = default);
    Task<string?> ResolveAccountByRoleAsync(string roleCode, string accountingStandardCode, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AccountRoleMapping>> GetRoleMappingsAsync(string accountingStandardCode, CancellationToken cancellationToken = default);
}
