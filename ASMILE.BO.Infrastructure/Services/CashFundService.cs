using ASMILE.BO.Application.DTOs;
using ASMILE.BO.Application.Interfaces;
using ASMILE.BO.Core.Entities;
using ASMILE.BO.Core.Interfaces.Services;
using ASMILE.BO.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ASMILE.BO.Infrastructure.Services;

public sealed class CashFundService : ICashFundService
{
    private const string StatusDraft = "Draft";
    private const string StatusApproved = "Approved";
    private const string StatusPosted = "Posted";
    private const string StatusReversed = "Reversed";
    private const string StatusConfirmed = "Confirmed";
    private const string TypeReceipt = "Receipt";
    private const string TypePayment = "Payment";
    private const string TypeAdjustment = "Adjustment";

    private readonly AppDbContext _dbContext;
    private readonly IJournalService _journalService;
    private readonly IAccountingPeriodService _accountingPeriodService;
    private readonly IPostingRuleService _postingRuleService;
    private readonly IAuditLogService? _auditLogService;

    public CashFundService(
        AppDbContext dbContext,
        IJournalService journalService,
        IAccountingPeriodService accountingPeriodService,
        IPostingRuleService postingRuleService,
        IAuditLogService? auditLogService = null)
    {
        _dbContext = dbContext;
        _journalService = journalService;
        _accountingPeriodService = accountingPeriodService;
        _postingRuleService = postingRuleService;
        _auditLogService = auditLogService;
    }

    public async Task<IReadOnlyList<CashFundDto>> GetFundsAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.CashFunds
            .AsNoTracking()
            .OrderByDescending(x => x.IsDefault)
            .ThenBy(x => x.FundCode)
            .Select(x => ToFundDto(x))
            .ToListAsync(cancellationToken);
    }

    public async Task<CashFundDto> CreateFundAsync(CashFundRequest request, CancellationToken cancellationToken = default)
    {
        ValidateFundRequest(request);
        var code = request.FundCode.Trim();
        var exists = await _dbContext.CashFunds.AnyAsync(x => x.FundCode == code, cancellationToken);
        if (exists)
        {
            throw new InvalidOperationException($"Mã quỹ đã tồn tại: {code}.");
        }

        var now = DateTime.UtcNow;
        if (request.IsDefault)
        {
            await ClearDefaultFundsAsync(cancellationToken);
        }

        var fund = new CashFund
        {
            Id = Guid.NewGuid(),
            FundCode = code,
            FundName = request.FundName.Trim(),
            CurrencyCode = NormalizeCurrency(request.CurrencyCode),
            AccountCode = NormalizeOptional(request.AccountCode),
            WarehouseId = request.WarehouseId,
            DepartmentId = request.DepartmentId,
            ResponsiblePersonId = request.ResponsiblePersonId,
            OpeningBalance = request.OpeningBalance,
            CurrentBalance = request.OpeningBalance,
            IsDefault = request.IsDefault,
            IsActive = request.IsActive,
            CreatedAt = now,
            UpdatedAt = now
        };

        _dbContext.CashFunds.Add(fund);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await LogAsync("CashFund.Create", $"Tạo quỹ tiền mặt {fund.FundCode}.", "cash", cancellationToken);
        return ToFundDto(fund);
    }

    public async Task<CashFundDto> UpdateFundAsync(Guid fundId, CashFundRequest request, CancellationToken cancellationToken = default)
    {
        ValidateFundRequest(request);
        var fund = await _dbContext.CashFunds.FirstOrDefaultAsync(x => x.Id == fundId, cancellationToken)
            ?? throw new InvalidOperationException("Không tìm thấy quỹ tiền mặt.");

        var code = request.FundCode.Trim();
        var duplicated = await _dbContext.CashFunds.AnyAsync(x => x.Id != fundId && x.FundCode == code, cancellationToken);
        if (duplicated)
        {
            throw new InvalidOperationException($"Mã quỹ đã tồn tại: {code}.");
        }

        if (request.IsDefault)
        {
            await ClearDefaultFundsAsync(cancellationToken, fundId);
        }

        fund.FundCode = code;
        fund.FundName = request.FundName.Trim();
        fund.CurrencyCode = NormalizeCurrency(request.CurrencyCode);
        fund.AccountCode = NormalizeOptional(request.AccountCode);
        fund.WarehouseId = request.WarehouseId;
        fund.DepartmentId = request.DepartmentId;
        fund.ResponsiblePersonId = request.ResponsiblePersonId;
        fund.IsDefault = request.IsDefault;
        fund.IsActive = request.IsActive;
        fund.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);
        await LogAsync("CashFund.Update", $"Cập nhật quỹ tiền mặt {fund.FundCode}.", "cash", cancellationToken);
        return ToFundDto(fund);
    }

    public async Task<CashSummaryDto> GetSummaryAsync(Guid? fundId, DateTime? fromDate, DateTime? toDate, CancellationToken cancellationToken = default)
    {
        var from = fromDate?.Date;
        var to = toDate?.Date;
        var fundQuery = _dbContext.CashFunds.AsNoTracking().AsQueryable();
        if (fundId.HasValue)
        {
            fundQuery = fundQuery.Where(x => x.Id == fundId.Value);
        }

        var opening = (await fundQuery.Select(x => x.OpeningBalance).ToListAsync(cancellationToken)).Sum();
        var txQuery = _dbContext.CashTransactions.AsNoTracking().Where(x => x.Status == StatusPosted);
        if (fundId.HasValue)
        {
            txQuery = txQuery.Where(x => x.CashFundId == fundId.Value);
        }
        if (from.HasValue)
        {
            txQuery = txQuery.Where(x => x.TransactionDate >= from.Value);
        }
        if (to.HasValue)
        {
            txQuery = txQuery.Where(x => x.TransactionDate <= to.Value);
        }

        var receiptValues = await txQuery.Select(x => new { x.DebitAmount, x.CreditAmount }).ToListAsync(cancellationToken);
        var receipts = receiptValues.Sum(x => x.DebitAmount);
        var payments = receiptValues.Sum(x => x.CreditAmount);
        var count = await txQuery.CountAsync(cancellationToken);
        var closing = (await fundQuery.Select(x => x.CurrentBalance).ToListAsync(cancellationToken)).Sum();
        return new CashSummaryDto
        {
            OpeningBalance = opening,
            TotalReceipts = receipts,
            TotalPayments = payments,
            ClosingBalance = closing,
            PostedTransactionCount = count
        };
    }

    public async Task<IReadOnlyList<CashBookEntryDto>> GetCashBookAsync(Guid? fundId, DateTime fromDate, DateTime toDate, CancellationToken cancellationToken = default)
    {
        var from = fromDate.Date;
        var to = toDate.Date;
        var fundQuery = _dbContext.CashFunds.AsNoTracking().AsQueryable();
        if (fundId.HasValue)
        {
            fundQuery = fundQuery.Where(x => x.Id == fundId.Value);
        }

        var opening = (await fundQuery.Select(x => x.OpeningBalance).ToListAsync(cancellationToken)).Sum();
        var beforeQuery = _dbContext.CashTransactions.AsNoTracking()
            .Where(x => x.Status == StatusPosted && x.TransactionDate < from);
        if (fundId.HasValue)
        {
            beforeQuery = beforeQuery.Where(x => x.CashFundId == fundId.Value);
        }
        opening += (await beforeQuery.Select(x => new { x.DebitAmount, x.CreditAmount }).ToListAsync(cancellationToken)).Sum(x => x.DebitAmount - x.CreditAmount);

        var query = _dbContext.CashTransactions.AsNoTracking()
            .Where(x => x.Status == StatusPosted && x.TransactionDate >= from && x.TransactionDate <= to);
        if (fundId.HasValue)
        {
            query = query.Where(x => x.CashFundId == fundId.Value);
        }

        var transactions = await query
            .OrderBy(x => x.TransactionDate)
            .ThenBy(x => x.CreatedAt)
            .ThenBy(x => x.TransactionNo)
            .ToListAsync(cancellationToken);

        var balance = opening;
        var result = new List<CashBookEntryDto>(transactions.Count);
        foreach (var tx in transactions)
        {
            balance += tx.DebitAmount - tx.CreditAmount;
            result.Add(new CashBookEntryDto
            {
                Date = tx.TransactionDate,
                TransactionNo = tx.TransactionNo,
                Description = tx.Description,
                Debit = tx.DebitAmount,
                Credit = tx.CreditAmount,
                Balance = balance,
                SourceType = tx.SourceType,
                Status = tx.Status
            });
        }

        return result;
    }

    public async Task<IReadOnlyList<CashTransactionDto>> GetTransactionsAsync(Guid? fundId, DateTime? fromDate, DateTime? toDate, string? keyword = null, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.CashTransactions
            .AsNoTracking()
            .Include(x => x.CashFund)
            .AsQueryable();

        if (fundId.HasValue)
        {
            query = query.Where(x => x.CashFundId == fundId.Value);
        }

        if (fromDate.HasValue)
        {
            query = query.Where(x => x.TransactionDate >= fromDate.Value.Date);
        }

        if (toDate.HasValue)
        {
            query = query.Where(x => x.TransactionDate <= toDate.Value.Date);
        }

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var value = keyword.Trim();
            query = query.Where(x => x.TransactionNo.Contains(value)
                || (x.Description != null && x.Description.Contains(value))
                || (x.CounterAccountCode != null && x.CounterAccountCode.Contains(value))
                || (x.SourceType != null && x.SourceType.Contains(value)));
        }

        return await query
            .OrderByDescending(x => x.TransactionDate)
            .ThenByDescending(x => x.CreatedAt)
            .Select(x => ToTransactionDto(x, x.CashFund))
            .ToListAsync(cancellationToken);
    }

    public Task<CashTransactionDto> CreateReceiptAsync(CashTransactionRequest request, CancellationToken cancellationToken = default)
        => CreateTransactionAsync(request, TypeReceipt, cancellationToken);

    public Task<CashTransactionDto> CreatePaymentAsync(CashTransactionRequest request, CancellationToken cancellationToken = default)
        => CreateTransactionAsync(request, TypePayment, cancellationToken);

    public async Task<CashTransactionDto> UpdateTransactionAsync(Guid transactionId, CashTransactionRequest request, CancellationToken cancellationToken = default)
    {
        if (request.Amount <= 0)
        {
            throw new InvalidOperationException("Số tiền giao dịch quỹ phải lớn hơn 0.");
        }

        var tx = await _dbContext.CashTransactions
            .Include(x => x.CashFund)
            .FirstOrDefaultAsync(x => x.Id == transactionId, cancellationToken)
            ?? throw new InvalidOperationException("Không tìm thấy giao dịch quỹ.");
        if (tx.Status != StatusDraft)
        {
            throw new InvalidOperationException("Chỉ được sửa giao dịch quỹ ở trạng thái nháp.");
        }

        var fund = await _dbContext.CashFunds.FirstOrDefaultAsync(x => x.Id == request.FundId, cancellationToken)
            ?? throw new InvalidOperationException("Không tìm thấy quỹ tiền mặt.");
        if (!fund.IsActive)
        {
            throw new InvalidOperationException("Quỹ tiền mặt đang tạm tắt.");
        }

        var isReceipt = tx.TransactionType == TypeReceipt;
        tx.CashFundId = fund.Id;
        tx.TransactionDate = request.TransactionDate.Date;
        tx.SourceType = NormalizeOptional(request.SourceType);
        tx.SourceId = request.SourceId;
        tx.PartnerType = NormalizeOptional(request.PartnerType);
        tx.PartnerId = request.PartnerId;
        tx.CounterAccountCode = NormalizeOptional(request.CounterAccountCode);
        tx.Description = NormalizeOptional(request.Description);
        tx.DebitAmount = isReceipt ? request.Amount : 0m;
        tx.CreditAmount = isReceipt ? 0m : request.Amount;

        await _dbContext.SaveChangesAsync(cancellationToken);
        await LogAsync("CashTransaction.Update", $"Sửa giao dịch quỹ {tx.TransactionNo}.", request.CreatedBy ?? "system", cancellationToken);
        return ToTransactionDto(tx, fund);
    }

    public async Task DeleteTransactionAsync(Guid transactionId, string? deletedBy = null, CancellationToken cancellationToken = default)
    {
        var tx = await _dbContext.CashTransactions.FirstOrDefaultAsync(x => x.Id == transactionId, cancellationToken)
            ?? throw new InvalidOperationException("Không tìm thấy giao dịch quỹ.");
        if (tx.Status != StatusDraft)
        {
            throw new InvalidOperationException("Chỉ được xóa giao dịch quỹ ở trạng thái nháp.");
        }

        _dbContext.CashTransactions.Remove(tx);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await LogAsync("CashTransaction.Delete", $"Xóa giao dịch quỹ {tx.TransactionNo}.", deletedBy ?? "system", cancellationToken);
    }

    public async Task<CashTransactionDto> ApproveTransactionAsync(Guid transactionId, string? approvedBy = null, CancellationToken cancellationToken = default)
    {
        var tx = await _dbContext.CashTransactions
            .Include(x => x.CashFund)
            .FirstOrDefaultAsync(x => x.Id == transactionId, cancellationToken)
            ?? throw new InvalidOperationException("Không tìm thấy giao dịch quỹ.");
        if (tx.Status != StatusDraft)
        {
            throw new InvalidOperationException("Chỉ được duyệt giao dịch quỹ ở trạng thái nháp.");
        }

        await _accountingPeriodService.ValidatePostingDateAsync(tx.TransactionDate, cancellationToken);
        tx.Status = StatusApproved;

        // Retry once on transient concurrency conflicts
        var retry = 0;
        while (true)
        {
            try
            {
                await _dbContext.SaveChangesAsync(cancellationToken);
                break;
            }
            catch (DbUpdateConcurrencyException) when (++retry < 2)
            {
                await _dbContext.Entry(tx).ReloadAsync(cancellationToken);
                tx.Status = StatusApproved;
            }
        }

        await LogAsync("CashTransaction.Approve", $"Duyệt giao dịch quỹ {tx.TransactionNo}.", approvedBy ?? "system", cancellationToken);
        return ToTransactionDto(tx);
    }

    public async Task<CashTransactionDto> CancelApprovalAsync(Guid transactionId, string? cancelledBy = null, CancellationToken cancellationToken = default)
    {
        var tx = await _dbContext.CashTransactions
            .Include(x => x.CashFund)
            .FirstOrDefaultAsync(x => x.Id == transactionId, cancellationToken)
            ?? throw new InvalidOperationException("Không tìm thấy giao dịch quỹ.");
        if (tx.Status != StatusApproved)
        {
            throw new InvalidOperationException("Chỉ được hủy duyệt giao dịch quỹ đã duyệt.");
        }

        tx.Status = StatusDraft;
        await _dbContext.SaveChangesAsync(cancellationToken);
        await LogAsync("CashTransaction.CancelApproval", $"Hủy duyệt giao dịch quỹ {tx.TransactionNo}.", cancelledBy ?? "system", cancellationToken);
        return ToTransactionDto(tx);
    }

    public async Task<CashTransactionDto> PostTransactionAsync(Guid transactionId, string? postedBy = null, CancellationToken cancellationToken = default)
    {
        var tx = await _dbContext.CashTransactions
            .Include(x => x.CashFund)
            .FirstOrDefaultAsync(x => x.Id == transactionId, cancellationToken)
            ?? throw new InvalidOperationException("Không tìm thấy giao dịch quỹ.");

        if (tx.Status is not (StatusDraft or StatusApproved))
        {
            throw new InvalidOperationException("Chỉ được ghi sổ giao dịch quỹ ở trạng thái nháp hoặc đã duyệt.");
        }

        var fund = tx.CashFund ?? throw new InvalidOperationException("Không tìm thấy quỹ tiền mặt.");
        if (!fund.IsActive)
        {
            throw new InvalidOperationException("Quỹ tiền mặt đang tạm tắt.");
        }

        await _accountingPeriodService.ValidatePostingDateAsync(tx.TransactionDate, cancellationToken);
        var cashAccount = await ResolveCashAccountAsync(fund, cancellationToken);
        var counterAccount = await ResolveCounterAccountAsync(tx, cancellationToken);
        if (tx.CreditAmount > 0 && fund.CurrentBalance < tx.CreditAmount)
        {
            throw new InvalidOperationException("Số dư quỹ không đủ để chi tiền.");
        }

        var user = string.IsNullOrWhiteSpace(postedBy) ? tx.CreatedBy ?? "system" : postedBy.Trim();
        var journal = await CreateAndPostJournalAsync(tx, cashAccount, counterAccount, user, cancellationToken);

        fund.CurrentBalance += tx.DebitAmount - tx.CreditAmount;
        fund.UpdatedAt = DateTime.UtcNow;
        tx.JournalHeaderId = journal.Id;
        tx.Status = StatusPosted;
        tx.PostedAt = DateTime.UtcNow;
        tx.PostedBy = user;
        tx.BalanceAfter = fund.CurrentBalance;

        await _dbContext.SaveChangesAsync(cancellationToken);
        await LogAsync("CashTransaction.Post", $"Ghi sổ giao dịch quỹ {tx.TransactionNo}.", user, cancellationToken);
        return ToTransactionDto(tx);
    }

    public async Task<CashTransactionDto> ReverseTransactionAsync(Guid transactionId, string reason, string? reversedBy = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new InvalidOperationException("Lý do đảo giao dịch là bắt buộc.");
        }

        await using var dbTransaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
        var original = await _dbContext.CashTransactions
            .Include(x => x.CashFund)
            .FirstOrDefaultAsync(x => x.Id == transactionId, cancellationToken)
            ?? throw new InvalidOperationException("Không tìm thấy giao dịch quỹ.");

        if (original.Status != StatusPosted)
        {
            throw new InvalidOperationException("Chỉ được đảo giao dịch quỹ đã ghi sổ.");
        }

        var fund = original.CashFund ?? throw new InvalidOperationException("Không tìm thấy quỹ tiền mặt.");
        await _accountingPeriodService.ValidatePostingDateAsync(DateTime.Today, cancellationToken);
        var user = string.IsNullOrWhiteSpace(reversedBy) ? "system" : reversedBy.Trim();
        JournalHeader? reversalJournal = null;
        if (original.JournalHeaderId.HasValue)
        {
            reversalJournal = await _journalService.ReverseAsync(original.JournalHeaderId.Value, reason.Trim(), user, cancellationToken);
        }

        var reversal = new CashTransaction
        {
            Id = Guid.NewGuid(),
            CashFundId = original.CashFundId,
            TransactionNo = await GenerateTransactionNoAsync("CQD", cancellationToken),
            TransactionDate = DateTime.Today,
            TransactionType = TypeAdjustment,
            SourceType = "CashTransactionReversal",
            SourceId = original.Id,
            JournalHeaderId = reversalJournal?.Id,
            PartnerType = original.PartnerType,
            PartnerId = original.PartnerId,
            Description = $"Đảo giao dịch {original.TransactionNo}: {reason.Trim()}",
            DebitAmount = original.CreditAmount,
            CreditAmount = original.DebitAmount,
            Status = StatusPosted,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = user,
            PostedAt = DateTime.UtcNow,
            PostedBy = user
        };

        fund.CurrentBalance += reversal.DebitAmount - reversal.CreditAmount;
        fund.UpdatedAt = DateTime.UtcNow;
        reversal.BalanceAfter = fund.CurrentBalance;
        original.Status = StatusReversed;
        _dbContext.CashTransactions.Add(reversal);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await dbTransaction.CommitAsync(cancellationToken);
        await LogAsync("CashTransaction.Reverse", $"Đảo giao dịch quỹ {original.TransactionNo}. Lý do: {reason.Trim()}", user, cancellationToken);
        return ToTransactionDto(reversal);
    }

    public async Task<CashCountDto> CreateCashCountAsync(CashCountRequest request, CancellationToken cancellationToken = default)
    {
        var fund = await _dbContext.CashFunds.FirstOrDefaultAsync(x => x.Id == request.CashFundId, cancellationToken)
            ?? throw new InvalidOperationException("Không tìm thấy quỹ tiền mặt.");
        if (!fund.IsActive)
        {
            throw new InvalidOperationException("Quỹ tiền mặt đang tạm tắt.");
        }

        var lines = request.Lines.Select(x => new CashCountLine
        {
            Id = Guid.NewGuid(),
            Denomination = x.Denomination,
            Quantity = x.Quantity,
            Amount = x.Amount != 0 ? x.Amount : x.Denomination * x.Quantity
        }).ToList();
        var actual = lines.Sum(x => x.Amount);
        var now = DateTime.UtcNow;
        var count = new CashCount
        {
            Id = Guid.NewGuid(),
            CashFundId = fund.Id,
            CountNo = await GenerateCountNoAsync(cancellationToken),
            CountDate = request.CountDate.Date,
            BookBalance = fund.CurrentBalance,
            ActualBalance = actual,
            Difference = actual - fund.CurrentBalance,
            Status = StatusDraft,
            Note = NormalizeOptional(request.Note),
            CreatedAt = now,
            CreatedBy = string.IsNullOrWhiteSpace(request.CreatedBy) ? "system" : request.CreatedBy.Trim(),
            Lines = lines
        };

        _dbContext.CashCounts.Add(count);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await LogAsync("CashCount.Create", $"Tạo kiểm kê quỹ {count.CountNo}.", count.CreatedBy, cancellationToken);
        return ToCashCountDto(count);
    }

    public async Task<CashCountDto> ConfirmCashCountAsync(Guid cashCountId, string? confirmedBy = null, string? counterAccountCode = null, CancellationToken cancellationToken = default)
    {
        var count = await _dbContext.CashCounts
            .Include(x => x.Lines)
            .Include(x => x.CashFund)
            .FirstOrDefaultAsync(x => x.Id == cashCountId, cancellationToken)
            ?? throw new InvalidOperationException("Không tìm thấy phiếu kiểm kê quỹ.");

        if (count.Status != StatusDraft)
        {
            throw new InvalidOperationException("Chỉ được xác nhận phiếu kiểm kê quỹ ở trạng thái nháp.");
        }

        await _accountingPeriodService.ValidatePostingDateAsync(count.CountDate, cancellationToken);
        var user = string.IsNullOrWhiteSpace(confirmedBy) ? "system" : confirmedBy.Trim();
        count.Status = StatusConfirmed;
        count.ConfirmedAt = DateTime.UtcNow;
        count.ConfirmedBy = user;
        await _dbContext.SaveChangesAsync(cancellationToken);

        if (count.Difference != 0 && !string.IsNullOrWhiteSpace(counterAccountCode))
        {
            var request = new CashTransactionRequest
            {
                FundId = count.CashFundId,
                TransactionDate = count.CountDate,
                Amount = Math.Abs(count.Difference),
                CounterAccountCode = counterAccountCode,
                Description = count.Difference > 0
                    ? $"Điều chỉnh thừa quỹ theo kiểm kê {count.CountNo}"
                    : $"Điều chỉnh thiếu quỹ theo kiểm kê {count.CountNo}",
                SourceType = "CashCount",
                SourceId = count.Id,
                CreatedBy = user
            };
            var adjustment = count.Difference > 0
                ? await CreateReceiptAsync(request, cancellationToken)
                : await CreatePaymentAsync(request, cancellationToken);
            await PostTransactionAsync(adjustment.Id, user, cancellationToken);
        }
        else if (count.Difference != 0)
        {
            await LogAsync("CashCount.AdjustmentPending", $"Kiểm kê {count.CountNo} có chênh lệch nhưng chưa có tài khoản đối ứng để tự điều chỉnh.", user, cancellationToken);
        }

        await LogAsync("CashCount.Confirm", $"Xác nhận kiểm kê quỹ {count.CountNo}.", user, cancellationToken);
        return ToCashCountDto(count);
    }

    private async Task<CashTransactionDto> CreateTransactionAsync(CashTransactionRequest request, string transactionType, CancellationToken cancellationToken)
    {
        if (request.Amount <= 0)
        {
            throw new InvalidOperationException("Số tiền giao dịch quỹ phải lớn hơn 0.");
        }

        var fund = await _dbContext.CashFunds.FirstOrDefaultAsync(x => x.Id == request.FundId, cancellationToken)
            ?? throw new InvalidOperationException("Không tìm thấy quỹ tiền mặt.");
        if (!fund.IsActive)
        {
            throw new InvalidOperationException("Quỹ tiền mặt đang tạm tắt.");
        }

        var isReceipt = transactionType == TypeReceipt;
        var tx = new CashTransaction
        {
            Id = Guid.NewGuid(),
            CashFundId = fund.Id,
            TransactionNo = await GenerateTransactionNoAsync(isReceipt ? "PTQ" : "PCQ", cancellationToken),
            TransactionDate = request.TransactionDate.Date,
            TransactionType = transactionType,
            SourceType = NormalizeOptional(request.SourceType),
            SourceId = request.SourceId,
            PartnerType = NormalizeOptional(request.PartnerType),
            PartnerId = request.PartnerId,
            CounterAccountCode = NormalizeOptional(request.CounterAccountCode),
            Description = NormalizeOptional(request.Description),
            DebitAmount = isReceipt ? request.Amount : 0m,
            CreditAmount = isReceipt ? 0m : request.Amount,
            Status = StatusDraft,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = string.IsNullOrWhiteSpace(request.CreatedBy) ? "system" : request.CreatedBy.Trim()
        };

        // Store the counter account in SourceType is avoided; callers must pass it again when posting if needed.
        _dbContext.CashTransactions.Add(tx);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await LogAsync("CashTransaction.Create", $"Tạo giao dịch quỹ {tx.TransactionNo}.", tx.CreatedBy, cancellationToken);
        return ToTransactionDto(tx, fund);
    }

    private async Task<JournalHeader> CreateAndPostJournalAsync(CashTransaction tx, string cashAccountCode, string counterAccountCode, string user, CancellationToken cancellationToken)
    {
        var accounts = await _dbContext.LedgerAccounts
            .Where(x => x.AccountCode == cashAccountCode || x.AccountCode == counterAccountCode)
            .ToDictionaryAsync(x => x.AccountCode, x => x.AccountName, cancellationToken);
        var cashAccountName = accounts.TryGetValue(cashAccountCode, out var cashName) ? cashName : cashAccountCode;
        var counterAccountName = accounts.TryGetValue(counterAccountCode, out var counterName) ? counterName : counterAccountCode;
        var isDebitCash = tx.DebitAmount > 0;
        var amount = isDebitCash ? tx.DebitAmount : tx.CreditAmount;
        var customerId = IsPartnerType(tx, "Customer") ? tx.PartnerId : null;
        var supplierId = IsPartnerType(tx, "Supplier") ? tx.PartnerId : null;
        var employeeId = IsPartnerType(tx, "Employee") ? tx.PartnerId : null;
        var journal = new JournalHeader
        {
            Id = Guid.NewGuid(),
            JournalNo = await GenerateJournalNoAsync(cancellationToken),
            JournalDate = tx.TransactionDate,
            PostingDate = tx.TransactionDate,
            SourceType = "CashTransaction",
            SourceId = tx.Id,
            SourceNo = tx.TransactionNo,
            Description = string.IsNullOrWhiteSpace(tx.Description) ? $"Ghi sổ giao dịch quỹ {tx.TransactionNo}" : tx.Description!,
            PeriodCode = tx.TransactionDate.ToString("yyyy-MM"),
            CreatedBy = user,
            CreatedAt = DateTime.UtcNow,
            Lines = new List<JournalLine>()
        };

        if (isDebitCash)
        {
            journal.Lines.Add(new JournalLine
            {
                Id = Guid.NewGuid(),
                LineNo = 1,
                AccountCode = cashAccountCode,
                AccountName = cashAccountName,
                DebitAmount = amount,
                CurrencyCode = tx.CashFund?.CurrencyCode ?? "VND",
                ExchangeRate = 1m,
                ObjectType = tx.PartnerType,
                ObjectId = tx.PartnerId,
                CustomerId = customerId,
                SupplierId = supplierId,
                EmployeeId = employeeId,
                Description = $"Thu tiền quỹ {tx.TransactionNo}"
            });
            journal.Lines.Add(new JournalLine
            {
                Id = Guid.NewGuid(),
                LineNo = 2,
                AccountCode = counterAccountCode,
                AccountName = counterAccountName,
                CreditAmount = amount,
                CurrencyCode = tx.CashFund?.CurrencyCode ?? "VND",
                ExchangeRate = 1m,
                ObjectType = tx.PartnerType,
                ObjectId = tx.PartnerId,
                CustomerId = customerId,
                SupplierId = supplierId,
                EmployeeId = employeeId,
                Description = $"Đối ứng thu tiền quỹ {tx.TransactionNo}"
            });
        }
        else
        {
            journal.Lines.Add(new JournalLine
            {
                Id = Guid.NewGuid(),
                LineNo = 1,
                AccountCode = counterAccountCode,
                AccountName = counterAccountName,
                DebitAmount = amount,
                CurrencyCode = tx.CashFund?.CurrencyCode ?? "VND",
                ExchangeRate = 1m,
                ObjectType = tx.PartnerType,
                ObjectId = tx.PartnerId,
                CustomerId = customerId,
                SupplierId = supplierId,
                EmployeeId = employeeId,
                Description = $"Đối ứng chi tiền quỹ {tx.TransactionNo}"
            });
            journal.Lines.Add(new JournalLine
            {
                Id = Guid.NewGuid(),
                LineNo = 2,
                AccountCode = cashAccountCode,
                AccountName = cashAccountName,
                CreditAmount = amount,
                CurrencyCode = tx.CashFund?.CurrencyCode ?? "VND",
                ExchangeRate = 1m,
                ObjectType = tx.PartnerType,
                ObjectId = tx.PartnerId,
                CustomerId = customerId,
                SupplierId = supplierId,
                EmployeeId = employeeId,
                Description = $"Chi tiền quỹ {tx.TransactionNo}"
            });
        }

        var draft = await _journalService.CreateDraftAsync(journal, cancellationToken);
        return await _journalService.PostDraftAsync(draft.Id, user, cancellationToken);
    }

    private async Task<string> ResolveCashAccountAsync(CashFund fund, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(fund.AccountCode))
        {
            return fund.AccountCode.Trim();
        }

        var standardCode = await GetDefaultAccountingStandardCodeAsync(cancellationToken);
        var account = await _postingRuleService.ResolveAccountByRoleAsync("Cash", standardCode, cancellationToken);
        if (string.IsNullOrWhiteSpace(account))
        {
            throw new InvalidOperationException($"Chưa cấu hình AccountRoleMapping cho Cash theo chuẩn {standardCode}.");
        }

        return account;
    }

    private static bool IsPartnerType(CashTransaction tx, string partnerType)
        => string.Equals(tx.PartnerType, partnerType, StringComparison.OrdinalIgnoreCase);

    private async Task<string> ResolveCounterAccountAsync(CashTransaction tx, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(tx.CounterAccountCode))
        {
            return tx.CounterAccountCode.Trim();
        }

        // Auto-resolve from source context or transaction type
        var standardCode = await GetDefaultAccountingStandardCodeAsync(cancellationToken);
        if (tx.SourceType == "BreakfastCafeOrder")
        {
            var account = await _postingRuleService.ResolveAccountByRoleAsync("Revenue", standardCode, cancellationToken);
            if (!string.IsNullOrWhiteSpace(account))
                return account;
        }

        // Fallback: Receipt → Revenue, Payment → Expense
        var role = tx.TransactionType == TypeReceipt ? "Revenue" : "Expense";
        var resolved = await _postingRuleService.ResolveAccountByRoleAsync(role, standardCode, cancellationToken);
        if (string.IsNullOrWhiteSpace(resolved))
        {
            throw new InvalidOperationException($"Không xác định được tài khoản đối ứng cho giao dịch {tx.TransactionNo}. Vui lòng gán CounterAccountCode.");
        }

        return resolved;
    }

    private async Task<string> GetDefaultAccountingStandardCodeAsync(CancellationToken cancellationToken)
    {
        return await _dbContext.AccountingStandards
            .Where(x => x.IsDefault && x.IsActive)
            .OrderBy(x => x.Code)
            .Select(x => x.Code)
            .FirstOrDefaultAsync(cancellationToken) ?? "TT99";
    }

    private async Task<string> GenerateTransactionNoAsync(string prefix, CancellationToken cancellationToken)
    {
        string value;
        do
        {
            value = $"{prefix}-{DateTime.UtcNow:yyyyMMdd-HHmmss}-{Random.Shared.Next(0, 9999):D4}";
        }
        while (await _dbContext.CashTransactions.AnyAsync(x => x.TransactionNo == value, cancellationToken));

        return value;
    }

    private async Task<string> GenerateCountNoAsync(CancellationToken cancellationToken)
    {
        string value;
        do
        {
            value = $"KKQ-{DateTime.UtcNow:yyyyMMdd-HHmmss}-{Random.Shared.Next(0, 9999):D4}";
        }
        while (await _dbContext.CashCounts.AnyAsync(x => x.CountNo == value, cancellationToken));

        return value;
    }

    private async Task<string> GenerateJournalNoAsync(CancellationToken cancellationToken)
    {
        string value;
        do
        {
            value = $"JV-{DateTime.UtcNow:yyyyMMdd-HHmmss}-{Random.Shared.Next(0, 99999):D5}";
        }
        while (await _dbContext.JournalHeaders.AnyAsync(x => x.JournalNo == value, cancellationToken));

        return value;
    }

    private static void ValidateFundRequest(CashFundRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.FundCode))
        {
            throw new InvalidOperationException("Mã quỹ là bắt buộc.");
        }
        if (string.IsNullOrWhiteSpace(request.FundName))
        {
            throw new InvalidOperationException("Tên quỹ là bắt buộc.");
        }
    }

    private async Task ClearDefaultFundsAsync(CancellationToken cancellationToken, Guid? exceptFundId = null)
    {
        var defaults = await _dbContext.CashFunds
            .Where(x => x.IsDefault && (!exceptFundId.HasValue || x.Id != exceptFundId.Value))
            .ToListAsync(cancellationToken);
        foreach (var fund in defaults)
        {
            fund.IsDefault = false;
            fund.UpdatedAt = DateTime.UtcNow;
        }
    }

    private static string NormalizeCurrency(string? currencyCode)
        => string.IsNullOrWhiteSpace(currencyCode) ? "VND" : currencyCode.Trim().ToUpperInvariant();

    private static string? NormalizeOptional(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static CashFundDto ToFundDto(CashFund fund) => new()
    {
        Id = fund.Id,
        FundCode = fund.FundCode,
        FundName = fund.FundName,
        CurrencyCode = fund.CurrencyCode,
        AccountCode = fund.AccountCode,
        WarehouseId = fund.WarehouseId,
        DepartmentId = fund.DepartmentId,
        ResponsiblePersonId = fund.ResponsiblePersonId,
        OpeningBalance = fund.OpeningBalance,
        CurrentBalance = fund.CurrentBalance,
        IsDefault = fund.IsDefault,
        IsActive = fund.IsActive
    };

    private static CashTransactionDto ToTransactionDto(CashTransaction tx, CashFund? fund = null)
    {
        fund ??= tx.CashFund;
        return new CashTransactionDto
        {
            Id = tx.Id,
            CashFundId = tx.CashFundId,
            FundCode = fund?.FundCode ?? string.Empty,
            FundName = fund?.FundName ?? string.Empty,
            TransactionNo = tx.TransactionNo,
            TransactionDate = tx.TransactionDate,
            TransactionType = tx.TransactionType,
            SourceType = tx.SourceType,
            SourceId = tx.SourceId,
            JournalHeaderId = tx.JournalHeaderId,
            PartnerType = tx.PartnerType,
            PartnerId = tx.PartnerId,
            CounterAccountCode = tx.CounterAccountCode,
            Description = tx.Description,
            DebitAmount = tx.DebitAmount,
            CreditAmount = tx.CreditAmount,
            BalanceAfter = tx.BalanceAfter,
            Status = tx.Status,
            CreatedAt = tx.CreatedAt,
            CreatedBy = tx.CreatedBy,
            PostedAt = tx.PostedAt,
            PostedBy = tx.PostedBy
        };
    }

    private static CashCountDto ToCashCountDto(CashCount count) => new()
    {
        Id = count.Id,
        CashFundId = count.CashFundId,
        CountNo = count.CountNo,
        CountDate = count.CountDate,
        BookBalance = count.BookBalance,
        ActualBalance = count.ActualBalance,
        Difference = count.Difference,
        Status = count.Status,
        Note = count.Note,
        CreatedAt = count.CreatedAt,
        CreatedBy = count.CreatedBy,
        ConfirmedAt = count.ConfirmedAt,
        ConfirmedBy = count.ConfirmedBy,
        Lines = count.Lines
            .OrderBy(x => x.Denomination)
            .Select(x => new CashCountLineDto
            {
                Id = x.Id,
                Denomination = x.Denomination,
                Quantity = x.Quantity,
                Amount = x.Amount
            })
            .ToList()
    };

    private async Task LogAsync(string action, string detail, string username, CancellationToken cancellationToken)
    {
        if (_auditLogService is not null)
        {
            await _auditLogService.LogAsync(action, detail, username, cancellationToken);
        }
    }
}




