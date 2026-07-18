using ASMILE.BO.Application.Interfaces;
using ASMILE.BO.Core.Entities;
using ASMILE.BO.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ASMILE.BO.Infrastructure.Services;

public sealed class PostingRuleService : IPostingRuleService
{
    private readonly AppDbContext _dbContext;

    public PostingRuleService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PostingRule?> GetActiveRuleAsync(string sourceType, string accountingStandardCode, CancellationToken cancellationToken = default)
    {
        return await _dbContext.PostingRules
            .Include(x => x.Lines.OrderBy(line => line.LineNo))
            .FirstOrDefaultAsync(x =>
                x.IsActive &&
                x.SourceType == sourceType &&
                x.AccountingStandardCode == accountingStandardCode,
                cancellationToken);
    }

    public async Task<string?> ResolveAccountByRoleAsync(string roleCode, string accountingStandardCode, CancellationToken cancellationToken = default)
    {
        return await _dbContext.AccountRoleMappings
            .Where(x =>
                x.IsActive &&
                x.IsDefault &&
                x.RoleCode == roleCode &&
                x.AccountingStandardCode == accountingStandardCode)
            .OrderBy(x => x.AccountCode)
            .Select(x => x.AccountCode)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AccountRoleMapping>> GetRoleMappingsAsync(string accountingStandardCode, CancellationToken cancellationToken = default)
    {
        return await _dbContext.AccountRoleMappings
            .Where(x => x.IsActive && x.AccountingStandardCode == accountingStandardCode)
            .OrderBy(x => x.RoleCode)
            .ThenBy(x => x.AccountCode)
            .ToListAsync(cancellationToken);
    }
}
