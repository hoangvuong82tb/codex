using ASMILE.BO.Application.DTOs;
using ASMILE.BO.Application.Interfaces;
using ASMILE.BO.Core.Accounting;
using ASMILE.BO.Core.Entities;
using ASMILE.BO.Core.Enums;
using ASMILE.BO.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ASMILE.BO.Infrastructure.Services;

public sealed class PostingEngine : IPostingEngine
{
    private const string ManualJournalSourceType = "ManualJournal";
    private const string StockReceiptSourceType = "StockReceipt";
    private const string StockIssueSourceType = "StockIssue";

    private readonly AppDbContext _dbContext;
    private readonly IJournalService _journalService;
    private readonly IPostingRuleService _postingRuleService;
    private readonly IAccountingPeriodService _accountingPeriodService;

    public PostingEngine(AppDbContext dbContext, IJournalService journalService, IPostingRuleService postingRuleService, IAccountingPeriodService accountingPeriodService)
    {
        _dbContext = dbContext;
        _journalService = journalService;
        _postingRuleService = postingRuleService;
        _accountingPeriodService = accountingPeriodService;
    }

    public async Task<PostingPreviewResult> PreviewPostingAsync(string sourceType, Guid sourceId, CancellationToken cancellationToken = default)
    {
        if (sourceType == ManualJournalSourceType)
        {
            var journal = await _journalService.GetJournalAsync(sourceId, cancellationToken);
            if (journal is null)
            {
                return FailedPreview(sourceType, sourceId, "JOURNAL_NOT_FOUND", "Manual journal draft was not found.");
            }

            var validation = await _journalService.ValidateAsync(journal, cancellationToken);
            return ToPreviewResult(sourceType, sourceId, journal, validation, validation.IsValid ? "Manual journal is ready to post." : "Manual journal is invalid.");
        }

        if (sourceType == StockReceiptSourceType || sourceType == StockIssueSourceType)
        {
            try
            {
                var journal = sourceType == StockReceiptSourceType
                    ? await BuildStockReceiptJournalAsync(sourceId, "system", cancellationToken)
                    : await BuildStockIssueJournalAsync(sourceId, "system", cancellationToken);
                var validation = await _journalService.ValidateAsync(journal, cancellationToken);
                return ToPreviewResult(sourceType, sourceId, journal, validation, validation.IsValid ? "Posting preview is ready." : "Posting preview is invalid.");
            }
            catch (InvalidOperationException ex)
            {
                return FailedPreview(sourceType, sourceId, "PREVIEW_FAILED", ex.Message);
            }
        }

        return FailedPreview(sourceType, sourceId, "SOURCE_NOT_SUPPORTED", $"Posting source is not fully supported yet: {sourceType}.");
    }

    public async Task<PostingResult> PostAsync(string sourceType, Guid sourceId, string? postedBy = null, CancellationToken cancellationToken = default)
    {
        if (sourceType == ManualJournalSourceType)
        {
            try
            {
                var journal = await _journalService.PostDraftAsync(sourceId, postedBy, cancellationToken);
                return PostingResult.Succeeded(journal, $"Posted journal {journal.JournalNo}.");
            }
            catch (InvalidOperationException ex)
            {
                return PostingResult.Failed(new PostingError { Code = "POST_FAILED", Message = ex.Message });
            }
        }

        if (sourceType == StockReceiptSourceType || sourceType == StockIssueSourceType)
        {
            try
            {
                await EnsureNotPostedAsync(sourceType, sourceId, cancellationToken);
                var journal = sourceType == StockReceiptSourceType
                    ? await BuildStockReceiptJournalAsync(sourceId, postedBy ?? "system", cancellationToken)
                    : await BuildStockIssueJournalAsync(sourceId, postedBy ?? "system", cancellationToken);

                await _accountingPeriodService.ValidatePostingDateAsync(journal.PostingDate, cancellationToken);

                var draft = await _journalService.CreateDraftAsync(journal, cancellationToken);
                var posted = await _journalService.PostDraftAsync(draft.Id, postedBy, cancellationToken);
                if (sourceType == StockIssueSourceType)
                {
                    await LinkCostTransactionsToJournalAsync(sourceId, posted.Id, cancellationToken);
                }
                await AddPostingBatchAsync(sourceType, sourceId, PostingBatchStatus.Posted, postedBy, $"Posted journal {posted.JournalNo}.", cancellationToken);
                return PostingResult.Succeeded(posted, $"Posted journal {posted.JournalNo}.");
            }
            catch (InvalidOperationException ex)
            {
                await AddPostingBatchAsync(sourceType, sourceId, PostingBatchStatus.Failed, postedBy, ex.Message, cancellationToken);
                return PostingResult.Failed(new PostingError { Code = "POST_FAILED", Message = ex.Message });
            }
        }

        var message = $"Posting source is not fully supported yet: {sourceType}.";
        await AddPostingBatchAsync(sourceType, sourceId, PostingBatchStatus.Failed, postedBy, message, cancellationToken);
        return PostingResult.Failed(new PostingError { Code = "SOURCE_NOT_SUPPORTED", Message = message });
    }

    public async Task<PostingResult> ReverseAsync(Guid journalId, string reason, string? reversedBy = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var journal = await _journalService.ReverseAsync(journalId, reason, reversedBy, cancellationToken);
            return PostingResult.Succeeded(journal, $"Created reversal journal {journal.JournalNo}.");
        }
        catch (InvalidOperationException ex)
        {
            return PostingResult.Failed(new PostingError { Code = "REVERSE_FAILED", Message = ex.Message });
        }
    }

    public Task<JournalValidationResult> ValidateJournalAsync(JournalHeader journal, CancellationToken cancellationToken = default)
        => _journalService.ValidateAsync(journal, cancellationToken);

    private async Task<JournalHeader> BuildStockReceiptJournalAsync(Guid sourceId, string createdBy, CancellationToken cancellationToken)
    {
        var receiptId = DecodeIntSourceId(sourceId, StockReceiptSourceType);
        var receipt = await _dbContext.StockReceipts
            .Include(x => x.Details)
            .FirstOrDefaultAsync(x => x.Id == receiptId, cancellationToken)
            ?? throw new InvalidOperationException("Không tìm thấy phiếu nhập.");

        if (receipt.JournalHeaderId.HasValue || string.Equals(receipt.PostingStatus, "Posted", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Chứng từ đã được ghi sổ.");
        }

        if (receipt.Details.Count == 0)
        {
            throw new InvalidOperationException("Phiếu nhập chưa có chi tiết.");
        }

        var total = receipt.Details.Sum(x => x.Amount);
        if (total <= 0)
        {
            throw new InvalidOperationException("Tổng tiền phiếu nhập phải lớn hơn 0.");
        }

        var standardCode = await GetAccountingStandardCodeAsync(cancellationToken);
        var inventoryAccount = await ResolveAccountAsync("Inventory", standardCode, cancellationToken);
        var payableAccount = await ResolveAccountAsync("Payable", standardCode, cancellationToken);
        var postingDate = receipt.ReceiptDate == default ? DateTime.Today : receipt.ReceiptDate.Date;

        var lines = receipt.Details
            .Where(x => x.Amount > 0)
            .OrderBy(x => x.LineNo)
            .Select((detail, index) => new JournalLine
            {
                LineNo = index + 1,
                AccountCode = inventoryAccount.AccountCode,
                AccountName = inventoryAccount.AccountName,
                DebitAmount = detail.Amount,
                CreditAmount = 0,
                WarehouseId = (detail.WarehouseId ?? receipt.WarehouseId) > 0 ? PostingSourceIds.FromInt32(detail.WarehouseId ?? receipt.WarehouseId) : null,
                ItemId = detail.ItemId > 0 ? PostingSourceIds.FromInt32(detail.ItemId) : null,
                Description = $"Nhập kho theo phiếu {receipt.ReceiptNo}"
            })
            .ToList();

        lines.Add(new JournalLine
        {
            LineNo = lines.Count + 1,
            AccountCode = payableAccount.AccountCode,
            AccountName = payableAccount.AccountName,
            DebitAmount = 0,
            CreditAmount = total,
            SupplierId = await ResolveSupplierGuidAsync(receipt.SupplierCode, cancellationToken),
            Description = $"Công nợ nhà cung cấp theo phiếu {receipt.ReceiptNo}"
        });

        return new JournalHeader
        {
            Id = Guid.NewGuid(),
            JournalDate = postingDate,
            PostingDate = postingDate,
            SourceType = StockReceiptSourceType,
            SourceId = sourceId,
            SourceNo = receipt.ReceiptNo,
            Description = $"Ghi sổ phiếu nhập {receipt.ReceiptNo}",
            PeriodCode = postingDate.ToString("yyyy-MM"),
            Status = JournalStatus.Draft,
            CreatedBy = createdBy,
            CreatedAt = DateTime.UtcNow,
            Lines = lines
        };
    }

    private async Task<JournalHeader> BuildStockIssueJournalAsync(Guid sourceId, string createdBy, CancellationToken cancellationToken)
    {
        var issueId = DecodeIntSourceId(sourceId, StockIssueSourceType);
        var issue = await _dbContext.StockIssues
            .Include(x => x.Details)
            .FirstOrDefaultAsync(x => x.Id == issueId, cancellationToken)
            ?? throw new InvalidOperationException("Không tìm thấy phiếu xuất.");

        if (issue.JournalHeaderId.HasValue || string.Equals(issue.PostingStatus, "Posted", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Chứng từ đã được ghi sổ.");
        }

        if (issue.Details.Count == 0)
        {
            throw new InvalidOperationException("Phiếu xuất chưa có chi tiết.");
        }

        var costByDetailId = await _dbContext.InventoryCostTransactions
            .AsNoTracking()
            .Where(x => x.SourceType == StockIssueSourceType && x.SourceId == sourceId && x.SourceDetailId.HasValue)
            .ToDictionaryAsync(x => x.SourceDetailId!.Value, x => x.TotalCost, cancellationToken);

        var totalCost = costByDetailId.Count > 0
            ? costByDetailId.Values.Sum()
            : issue.Details.Sum(x => x.Amount > 0 ? x.Amount : x.Quantity * x.UnitPrice);
        if (totalCost <= 0)
        {
            throw new InvalidOperationException("Tổng giá vốn phiếu xuất phải lớn hơn 0.");
        }

        var standardCode = await GetAccountingStandardCodeAsync(cancellationToken);
        var cogsAccount = await ResolveAccountAsync("COGS", standardCode, cancellationToken);
        var inventoryAccount = await ResolveAccountAsync("Inventory", standardCode, cancellationToken);
        var postingDate = issue.IssueDate == default ? DateTime.Today : issue.IssueDate.Date;

        var lines = new List<JournalLine>();
        foreach (var detail in issue.Details.OrderBy(x => x.LineNo))
        {
            var sourceDetailId = detail.Id > 0 ? PostingSourceIds.FromInt32(detail.Id) : (Guid?)null;
            var lineAmount = sourceDetailId.HasValue && costByDetailId.TryGetValue(sourceDetailId.Value, out var persistedCost)
                ? persistedCost
                : detail.Amount > 0 ? detail.Amount : detail.Quantity * detail.UnitPrice;
            if (lineAmount <= 0)
            {
                continue;
            }

            var warehouseId = (detail.WarehouseId ?? issue.WarehouseId) > 0 ? PostingSourceIds.FromInt32(detail.WarehouseId ?? issue.WarehouseId) : (Guid?)null;
            var itemId = detail.ItemId > 0 ? PostingSourceIds.FromInt32(detail.ItemId) : (Guid?)null;

            lines.Add(new JournalLine
            {
                LineNo = lines.Count + 1,
                AccountCode = cogsAccount.AccountCode,
                AccountName = cogsAccount.AccountName,
                DebitAmount = lineAmount,
                CreditAmount = 0,
                WarehouseId = warehouseId,
                ItemId = itemId,
                Description = $"Giá vốn theo phiếu {issue.IssueNo}"
            });

            lines.Add(new JournalLine
            {
                LineNo = lines.Count + 1,
                AccountCode = inventoryAccount.AccountCode,
                AccountName = inventoryAccount.AccountName,
                DebitAmount = 0,
                CreditAmount = lineAmount,
                WarehouseId = warehouseId,
                ItemId = itemId,
                Description = $"Xuất kho theo phiếu {issue.IssueNo}"
            });
        }

        return new JournalHeader
        {
            Id = Guid.NewGuid(),
            JournalDate = postingDate,
            PostingDate = postingDate,
            SourceType = StockIssueSourceType,
            SourceId = sourceId,
            SourceNo = issue.IssueNo,
            Description = $"Ghi sổ phiếu xuất {issue.IssueNo}",
            PeriodCode = postingDate.ToString("yyyy-MM"),
            Status = JournalStatus.Draft,
            CreatedBy = createdBy,
            CreatedAt = DateTime.UtcNow,
            Lines = lines
        };
    }

    private async Task EnsureNotPostedAsync(string sourceType, Guid sourceId, CancellationToken cancellationToken)
    {
        var exists = await _dbContext.JournalHeaders.AnyAsync(x =>
            x.SourceType == sourceType
            && x.SourceId == sourceId
            && (x.Status == JournalStatus.Posted || x.Status == JournalStatus.Reversed), cancellationToken);
        if (exists)
        {
            throw new InvalidOperationException("Chứng từ đã được ghi sổ.");
        }
    }

    private async Task<(string AccountCode, string AccountName)> ResolveAccountAsync(string roleCode, string accountingStandardCode, CancellationToken cancellationToken)
    {
        var accountCode = await _postingRuleService.ResolveAccountByRoleAsync(roleCode, accountingStandardCode, cancellationToken);
        if (string.IsNullOrWhiteSpace(accountCode))
        {
            throw new InvalidOperationException($"Chưa cấu hình tài khoản cho vai trò {roleCode} theo chuẩn {accountingStandardCode}.");
        }

        var account = await _dbContext.LedgerAccounts.AsNoTracking().FirstOrDefaultAsync(x => x.AccountCode == accountCode, cancellationToken);
        return (accountCode, account?.AccountName ?? accountCode);
    }

    private async Task<string> GetAccountingStandardCodeAsync(CancellationToken cancellationToken)
    {
        var standard = await _dbContext.AccountingStandards.AsNoTracking()
            .Where(x => x.IsActive)
            .OrderByDescending(x => x.IsDefault)
            .ThenBy(x => x.Code)
            .Select(x => x.Code)
            .FirstOrDefaultAsync(cancellationToken);
        return string.IsNullOrWhiteSpace(standard) ? "TT99" : standard;
    }

    private async Task<Guid?> ResolveSupplierGuidAsync(string supplierCode, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(supplierCode))
        {
            return null;
        }

        var supplierId = await _dbContext.Suppliers.AsNoTracking()
            .Where(x => x.SupplierCode == supplierCode)
            .Select(x => (int?)x.Id)
            .FirstOrDefaultAsync(cancellationToken);
        return supplierId.HasValue && supplierId.Value > 0 ? PostingSourceIds.FromInt32(supplierId.Value) : null;
    }

    private async Task AddPostingBatchAsync(string sourceType, Guid sourceId, PostingBatchStatus status, string? user, string message, CancellationToken cancellationToken)
    {
        _dbContext.PostingBatches.Add(new PostingBatch
        {
            Id = Guid.NewGuid(),
            BatchNo = GenerateBatchNo(),
            SourceType = sourceType,
            SourceId = sourceId,
            Status = status,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = user ?? "system",
            Message = message
        });
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task LinkCostTransactionsToJournalAsync(Guid sourceId, Guid journalHeaderId, CancellationToken cancellationToken)
    {
        var transactions = await _dbContext.InventoryCostTransactions
            .Where(x => x.SourceType == StockIssueSourceType && x.SourceId == sourceId && x.JournalHeaderId == null)
            .ToListAsync(cancellationToken);

        foreach (var transaction in transactions)
        {
            transaction.JournalHeaderId = journalHeaderId;
        }

        if (transactions.Count > 0)
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    private static int DecodeIntSourceId(Guid sourceId, string sourceType)
    {
        if (!PostingSourceIds.TryToInt32(sourceId, out var id))
        {
            throw new InvalidOperationException($"SourceId không hợp lệ cho {sourceType}.");
        }

        return id;
    }

    private static PostingPreviewResult ToPreviewResult(string sourceType, Guid sourceId, JournalHeader journal, JournalValidationResult validation, string message)
        => new()
        {
            Success = validation.IsValid,
            SourceType = sourceType,
            SourceId = sourceId,
            Message = message,
            TotalDebit = journal.Lines.Sum(x => x.DebitAmount),
            TotalCredit = journal.Lines.Sum(x => x.CreditAmount),
            Difference = journal.Lines.Sum(x => x.DebitAmount) - journal.Lines.Sum(x => x.CreditAmount),
            Lines = journal.Lines.OrderBy(x => x.LineNo).Select(ToPreview).ToList(),
            Errors = validation.Errors
        };

    private static PostingPreviewResult FailedPreview(string sourceType, Guid sourceId, string code, string message)
        => new()
        {
            Success = false,
            SourceType = sourceType,
            SourceId = sourceId,
            Message = message,
            Errors = new[] { new PostingError { Code = code, Message = message } }
        };

    private static PostingLinePreview ToPreview(JournalLine line)
        => new()
        {
            LineNo = line.LineNo,
            AccountCode = line.AccountCode,
            AccountName = line.AccountName,
            DebitAmount = line.DebitAmount,
            CreditAmount = line.CreditAmount,
            Description = line.Description ?? string.Empty
        };

    private static string GenerateBatchNo()
        => $"PB-{DateTime.UtcNow:yyyyMMdd-HHmmss}-{Random.Shared.Next(0, 99999):D5}";
}
