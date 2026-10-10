using ASMILE.BO.Core.Entities;
using ASMILE.BO.Core.Interfaces.Repositories;
using ASMILE.BO.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ASMILE.BO.Infrastructure.Repositories;

public sealed class CustomerRepository : EfRepository<Customer>, ICustomerRepository
{
    public CustomerRepository(AppDbContext dbContext) : base(dbContext)
    {
    }

    public async Task<IReadOnlyList<Customer>> SearchAsync(string keyword, CancellationToken cancellationToken = default)
    {
        var q = DbSet.AsNoTracking();
        keyword = keyword?.Trim() ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            q = q.Where(x => x.CustomerCode.Contains(keyword) || x.CustomerName.Contains(keyword) || x.Phone.Contains(keyword));
        }

        return await q.OrderBy(x => x.CustomerCode).ToListAsync(cancellationToken);
    }

    public Task<bool> ExistsCodeAsync(string customerCode, int excludingId = 0, CancellationToken cancellationToken = default)
        => DbSet.AnyAsync(x => x.CustomerCode == customerCode && x.Id != excludingId, cancellationToken);

    public async Task<IReadOnlyList<string>> GetCodesStartingWithAsync(string prefix, CancellationToken cancellationToken = default)
        => await DbSet.AsNoTracking()
            .Where(x => x.CustomerCode.StartsWith(prefix))
            .Select(x => x.CustomerCode)
            .ToListAsync(cancellationToken);

    public Task<bool> ExistsPhoneAsync(string phone, int excludingId = 0, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(phone)) return Task.FromResult(false);

        var normalizedPhone = NormalizePhone(phone);
        var alternatePhone = normalizedPhone.StartsWith('0') && normalizedPhone.Length > 1
            ? $"84{normalizedPhone[1..]}"
            : normalizedPhone;
        return DbSet.AnyAsync(x => x.Id != excludingId
            && (x.Phone.Replace(" ", string.Empty)
                .Replace("-", string.Empty)
                .Replace("(", string.Empty)
                .Replace(")", string.Empty)
                .Replace("+", string.Empty)
                .Replace(".", string.Empty)
                .Replace("/", string.Empty)
                == normalizedPhone
                || x.Phone.Replace(" ", string.Empty)
                    .Replace("-", string.Empty)
                    .Replace("(", string.Empty)
                    .Replace(")", string.Empty)
                    .Replace("+", string.Empty)
                    .Replace(".", string.Empty)
                    .Replace("/", string.Empty)
                    == alternatePhone),
            cancellationToken);
    }

    public Task<bool> ExistsTaxCodeAsync(string taxCode, int excludingId = 0, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(taxCode)) return Task.FromResult(false);

        var normalizedTaxCode = taxCode.Trim().ToUpperInvariant();
        return DbSet.AnyAsync(x => x.Id != excludingId
            && x.TaxCode.Trim().ToUpper() == normalizedTaxCode,
            cancellationToken);
    }

    private static string NormalizePhone(string phone)
    {
        var normalized = phone.Trim()
            .Replace(" ", string.Empty, StringComparison.Ordinal)
            .Replace("-", string.Empty, StringComparison.Ordinal)
            .Replace("(", string.Empty, StringComparison.Ordinal)
            .Replace(")", string.Empty, StringComparison.Ordinal)
            .Replace("+", string.Empty, StringComparison.Ordinal)
            .Replace(".", string.Empty, StringComparison.Ordinal)
            .Replace("/", string.Empty, StringComparison.Ordinal);
        return normalized.StartsWith("84", StringComparison.Ordinal) && normalized.Length > 2
            ? $"0{normalized[2..]}"
            : normalized;
    }
}
