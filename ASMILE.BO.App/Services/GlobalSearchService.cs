using ASMILE.BO.Application.DTOs;
using ASMILE.BO.Application.Interfaces;
using ASMILE.BO.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ASMILE.BO.App.Services;

public sealed class GlobalSearchService : IGlobalSearchService
{
    private const int MaxResults = 30;
    private readonly INavigationService _navigationService;
    private readonly IServiceScopeFactory _scopeFactory;

    public GlobalSearchService(INavigationService navigationService, IServiceScopeFactory scopeFactory)
    {
        _navigationService = navigationService;
        _scopeFactory = scopeFactory;
    }

    public async Task<IReadOnlyList<GlobalSearchResultDto>> SearchAsync(string query, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return [];
        }

        var term = query.Trim();
        var results = new List<GlobalSearchResultDto>();
        results.AddRange(SearchModules(term));

        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        results.AddRange(await SearchReportsAsync(dbContext, term, cancellationToken));
        results.AddRange(await SearchJournalsAsync(dbContext, term, cancellationToken));
        results.AddRange(await SearchVouchersAsync(dbContext, term, cancellationToken));
        results.AddRange(await SearchPartnersAsync(dbContext, term, cancellationToken));
        results.AddRange(await SearchAssetsAsync(dbContext, term, cancellationToken));

        return results
            .GroupBy(x => $"{x.TargetType}:{x.TargetRoute}:{x.Id}", StringComparer.OrdinalIgnoreCase)
            .Select(x => x.First())
            .Take(MaxResults)
            .ToArray();
    }

    private IEnumerable<GlobalSearchResultDto> SearchModules(string term)
    {
        return _navigationService.GetModuleCatalog()
            .Where(x => x.Title.Contains(term, StringComparison.OrdinalIgnoreCase)
                        || x.Subtitle.Contains(term, StringComparison.OrdinalIgnoreCase)
                        || x.Key.Contains(term, StringComparison.OrdinalIgnoreCase))
            .Take(8)
            .Select(x => new GlobalSearchResultDto
            {
                Id = x.Key,
                Title = x.Title,
                Subtitle = x.Subtitle,
                Category = "Module",
                TargetType = "module",
                TargetRoute = x.Key
            });
    }

    private static async Task<IReadOnlyList<GlobalSearchResultDto>> SearchReportsAsync(AppDbContext dbContext, string term, CancellationToken cancellationToken)
    {
        var rows = await dbContext.ReportTemplates
            .AsNoTracking()
            .Where(x => x.IsActive && (x.Code.Contains(term) || x.Name.Contains(term)))
            .OrderBy(x => x.Code)
            .Take(6)
            .Select(x => new { x.Code, x.Name, x.Category })
            .ToListAsync(cancellationToken);

        return rows.Select(x => new GlobalSearchResultDto
        {
            Id = x.Code,
            Title = x.Name,
            Subtitle = x.Code,
            Category = $"Report/{x.Category}",
            TargetType = "report",
            TargetRoute = x.Code
        }).ToArray();
    }

    private static async Task<IReadOnlyList<GlobalSearchResultDto>> SearchJournalsAsync(AppDbContext dbContext, string term, CancellationToken cancellationToken)
    {
        var rows = await dbContext.JournalHeaders
            .AsNoTracking()
            .Where(x => x.JournalNo.Contains(term) || x.SourceNo.Contains(term) || x.Description.Contains(term))
            .OrderByDescending(x => x.JournalDate)
            .Take(6)
            .Select(x => new { x.Id, x.JournalNo, x.SourceType, x.SourceNo, x.Status })
            .ToListAsync(cancellationToken);

        return rows.Select(x => new GlobalSearchResultDto
        {
            Id = x.Id.ToString(),
            Title = x.JournalNo,
            Subtitle = $"{x.SourceType} {x.SourceNo} - {x.Status}",
            Category = "Journal",
            TargetType = "journal",
            TargetRoute = "general-ledger",
            TargetId = x.Id
        }).ToArray();
    }

    private static async Task<IReadOnlyList<GlobalSearchResultDto>> SearchVouchersAsync(AppDbContext dbContext, string term, CancellationToken cancellationToken)
    {
        var receiptRows = await dbContext.StockReceipts
            .AsNoTracking()
            .Where(x => x.ReceiptNo.Contains(term))
            .OrderByDescending(x => x.ReceiptDate)
            .Take(4)
            .Select(x => new { x.Id, Number = x.ReceiptNo, Date = x.ReceiptDate, x.PostingStatus })
            .ToListAsync(cancellationToken);

        var issueRows = await dbContext.StockIssues
            .AsNoTracking()
            .Where(x => x.IssueNo.Contains(term))
            .OrderByDescending(x => x.IssueDate)
            .Take(4)
            .Select(x => new { x.Id, Number = x.IssueNo, Date = x.IssueDate, x.PostingStatus })
            .ToListAsync(cancellationToken);

        var cashRows = await dbContext.CashTransactions
            .AsNoTracking()
            .Where(x => x.TransactionNo.Contains(term))
            .OrderByDescending(x => x.TransactionDate)
            .Take(4)
            .Select(x => new { x.Id, Number = x.TransactionNo, Date = x.TransactionDate, x.Status })
            .ToListAsync(cancellationToken);

        var bankRows = await dbContext.BankTransactions
            .AsNoTracking()
            .Where(x => x.TransactionNo.Contains(term) || (x.ReferenceNo != null && x.ReferenceNo.Contains(term)))
            .OrderByDescending(x => x.TransactionDate)
            .Take(4)
            .Select(x => new { x.Id, Number = x.TransactionNo, Date = x.TransactionDate, x.Status })
            .ToListAsync(cancellationToken);

        return receiptRows.Select(x => VoucherResult(x.Id.ToString(), x.Number, "StockReceipt", "receipts", x.Date, x.PostingStatus))
            .Concat(issueRows.Select(x => VoucherResult(x.Id.ToString(), x.Number, "StockIssue", "issues", x.Date, x.PostingStatus)))
            .Concat(cashRows.Select(x => VoucherResult(x.Id.ToString(), x.Number, "CashTransaction", "cash-bank", x.Date, x.Status, x.Id)))
            .Concat(bankRows.Select(x => VoucherResult(x.Id.ToString(), x.Number, "BankTransaction", "cash-bank", x.Date, x.Status, x.Id)))
            .ToArray();
    }

    private static async Task<IReadOnlyList<GlobalSearchResultDto>> SearchPartnersAsync(AppDbContext dbContext, string term, CancellationToken cancellationToken)
    {
        var customers = await dbContext.Customers
            .AsNoTracking()
            .Where(x => x.CustomerCode.Contains(term) || x.CustomerName.Contains(term) || x.Phone.Contains(term))
            .OrderBy(x => x.CustomerCode)
            .Take(4)
            .Select(x => new { x.Id, x.CustomerCode, x.CustomerName, x.Phone })
            .ToListAsync(cancellationToken);

        var suppliers = await dbContext.Suppliers
            .AsNoTracking()
            .Where(x => x.SupplierCode.Contains(term) || x.SupplierName.Contains(term) || x.Phone.Contains(term))
            .OrderBy(x => x.SupplierCode)
            .Take(4)
            .Select(x => new { x.Id, x.SupplierCode, x.SupplierName, x.Phone })
            .ToListAsync(cancellationToken);

        return customers.Select(x => new GlobalSearchResultDto
        {
            Id = x.Id.ToString(),
            Title = $"{x.CustomerCode} - {x.CustomerName}",
            Subtitle = x.Phone,
            Category = "Customer",
            TargetType = "module",
            TargetRoute = "customers"
        }).Concat(suppliers.Select(x => new GlobalSearchResultDto
        {
            Id = x.Id.ToString(),
            Title = $"{x.SupplierCode} - {x.SupplierName}",
            Subtitle = x.Phone,
            Category = "Supplier",
            TargetType = "module",
            TargetRoute = "suppliers"
        })).ToArray();
    }

    private static async Task<IReadOnlyList<GlobalSearchResultDto>> SearchAssetsAsync(AppDbContext dbContext, string term, CancellationToken cancellationToken)
    {
        var fixedAssets = await dbContext.FixedAssets
            .AsNoTracking()
            .Where(x => x.AssetCode.Contains(term) || x.AssetName.Contains(term))
            .OrderBy(x => x.AssetCode)
            .Take(4)
            .Select(x => new { x.Id, x.AssetCode, x.AssetName, x.Status })
            .ToListAsync(cancellationToken);

        var toolAssets = await dbContext.ToolAssets
            .AsNoTracking()
            .Where(x => x.ToolCode.Contains(term) || x.ToolName.Contains(term))
            .OrderBy(x => x.ToolCode)
            .Take(4)
            .Select(x => new { x.Id, x.ToolCode, x.ToolName, x.Status })
            .ToListAsync(cancellationToken);

        return fixedAssets.Select(x => VoucherResult(x.Id.ToString(), $"{x.AssetCode} - {x.AssetName}", "FixedAsset", "asset-management", null, x.Status, x.Id))
            .Concat(toolAssets.Select(x => VoucherResult(x.Id.ToString(), $"{x.ToolCode} - {x.ToolName}", "ToolAsset", "asset-management", null, x.Status, x.Id)))
            .ToArray();
    }

    private static GlobalSearchResultDto VoucherResult(string id, string number, string sourceType, string route, DateTime? date, string? status, Guid? targetId = null)
    {
        return new GlobalSearchResultDto
        {
            Id = id,
            Title = number,
            Subtitle = date.HasValue ? $"{date:dd/MM/yyyy} - {status}" : status ?? string.Empty,
            Category = sourceType,
            TargetType = "voucher",
            TargetRoute = route,
            TargetId = targetId,
            SourceType = sourceType
        };
    }
}
