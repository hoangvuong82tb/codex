using System.Globalization;
using ASMILE.BO.Application.DTOs;
using ASMILE.BO.Application.Interfaces;
using ASMILE.BO.Core.Entities;
using ASMILE.BO.Core.Interfaces.Services;
using ASMILE.BO.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ASMILE.BO.Infrastructure.Services;

public sealed class BankingService : IBankingService
{
    private const string Draft = "Draft";
    private const string Approved = "Approved";
    private const string Posted = "Posted";
    private const string Reconciled = "Reconciled";
    private const string Reversed = "Reversed";
    private const string Confirmed = "Confirmed";
    private const string Imported = "Imported";
    private const string Matched = "Matched";
    private const string Unmatched = "Unmatched";
    private const string Receipt = "Receipt";
    private const string Payment = "Payment";

    private readonly AppDbContext _dbContext;
    private readonly IJournalService _journalService;
    private readonly IAccountingPeriodService _accountingPeriodService;
    private readonly IPostingRuleService _postingRuleService;
    private readonly IAuditLogService? _auditLogService;

    public BankingService(
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

    public async Task<IReadOnlyList<BankAccountDto>> GetAccountsAsync(CancellationToken cancellationToken = default)
        => await _dbContext.BankAccounts.AsNoTracking()
            .OrderByDescending(x => x.IsDefault)
            .ThenBy(x => x.BankCode)
            .ThenBy(x => x.AccountNumber)
            .Select(x => ToAccountDto(x))
            .ToListAsync(cancellationToken);

    public async Task<BankAccountDto> CreateAccountAsync(BankAccountRequest request, CancellationToken cancellationToken = default)
    {
        ValidateAccountRequest(request);
        var accountNumber = request.AccountNumber.Trim();
        if (await _dbContext.BankAccounts.AnyAsync(x => x.AccountNumber == accountNumber, cancellationToken))
        {
            throw new InvalidOperationException($"Số tài khoản ngân hàng đã tồn tại: {accountNumber}.");
        }

        if (request.IsDefault)
        {
            await ClearDefaultAccountsAsync(cancellationToken);
        }

        var now = DateTime.UtcNow;
        var account = new BankAccount
        {
            Id = Guid.NewGuid(),
            BankCode = request.BankCode.Trim(),
            BankName = request.BankName.Trim(),
            BranchName = NormalizeOptional(request.BranchName),
            AccountNumber = accountNumber,
            AccountName = request.AccountName.Trim(),
            CurrencyCode = NormalizeCurrency(request.CurrencyCode),
            AccountCode = NormalizeOptional(request.AccountCode),
            OpeningBalance = request.OpeningBalance,
            CurrentBalance = request.OpeningBalance,
            IsDefault = request.IsDefault,
            IsActive = request.IsActive,
            CreatedAt = now,
            UpdatedAt = now
        };

        _dbContext.BankAccounts.Add(account);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await LogAsync("BankAccount.Create", $"Tạo tài khoản ngân hàng {account.AccountNumber}.", "bank", cancellationToken);
        return ToAccountDto(account);
    }

    public Task<BankTransactionDto> CreateReceiptAsync(BankTransactionRequest request, CancellationToken cancellationToken = default)
        => CreateTransactionAsync(request, Receipt, cancellationToken);

    public Task<BankTransactionDto> CreatePaymentAsync(BankTransactionRequest request, CancellationToken cancellationToken = default)
        => CreateTransactionAsync(request, Payment, cancellationToken);

    public async Task<BankTransactionDto> UpdateTransactionAsync(Guid transactionId, BankTransactionRequest request, CancellationToken cancellationToken = default)
    {
        if (request.Amount <= 0)
        {
            throw new InvalidOperationException("Số tiền giao dịch ngân hàng phải lớn hơn 0.");
        }

        var tx = await _dbContext.BankTransactions.Include(x => x.BankAccount)
            .FirstOrDefaultAsync(x => x.Id == transactionId, cancellationToken)
            ?? throw new InvalidOperationException("Không tìm thấy giao dịch ngân hàng.");
        if (tx.Status != Draft)
        {
            throw new InvalidOperationException("Chỉ được sửa giao dịch ngân hàng ở trạng thái nháp.");
        }

        var account = await _dbContext.BankAccounts.FirstOrDefaultAsync(x => x.Id == request.BankAccountId, cancellationToken)
            ?? throw new InvalidOperationException("Không tìm thấy tài khoản ngân hàng.");
        if (!account.IsActive)
        {
            throw new InvalidOperationException("Tài khoản ngân hàng đang tạm tắt.");
        }

        var isReceipt = tx.TransactionType == Receipt;
        tx.BankAccountId = account.Id;
        tx.TransactionDate = request.TransactionDate.Date;
        tx.ValueDate = request.ValueDate?.Date;
        tx.SourceType = NormalizeOptional(request.SourceType);
        tx.SourceId = request.SourceId;
        tx.ReferenceNo = NormalizeOptional(request.ReferenceNo);
        tx.Description = NormalizeOptional(request.Description);
        tx.DebitAmount = isReceipt ? request.Amount : 0m;
        tx.CreditAmount = isReceipt ? 0m : request.Amount;
        tx.CounterAccountCode = NormalizeOptional(request.CounterAccountCode);
        tx.PartnerType = NormalizeOptional(request.PartnerType);
        tx.PartnerId = request.PartnerId;

        await _dbContext.SaveChangesAsync(cancellationToken);
        await LogAsync("BankTransaction.Update", $"Sửa giao dịch ngân hàng {tx.TransactionNo}.", request.CreatedBy ?? "system", cancellationToken);
        return ToTransactionDto(tx, account);
    }

    public async Task DeleteTransactionAsync(Guid transactionId, string? deletedBy = null, CancellationToken cancellationToken = default)
    {
        var tx = await _dbContext.BankTransactions.FirstOrDefaultAsync(x => x.Id == transactionId, cancellationToken)
            ?? throw new InvalidOperationException("Không tìm thấy giao dịch ngân hàng.");
        if (tx.Status != Draft)
        {
            throw new InvalidOperationException("Chỉ được xóa giao dịch ngân hàng ở trạng thái nháp.");
        }

        _dbContext.BankTransactions.Remove(tx);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await LogAsync("BankTransaction.Delete", $"Xóa giao dịch ngân hàng {tx.TransactionNo}.", deletedBy ?? "system", cancellationToken);
    }

    public async Task<BankTransactionDto> ApproveTransactionAsync(Guid transactionId, string? approvedBy = null, CancellationToken cancellationToken = default)
    {
        var tx = await _dbContext.BankTransactions.Include(x => x.BankAccount)
            .FirstOrDefaultAsync(x => x.Id == transactionId, cancellationToken)
            ?? throw new InvalidOperationException("Không tìm thấy giao dịch ngân hàng.");
        if (tx.Status != Draft)
        {
            throw new InvalidOperationException("Chỉ được duyệt giao dịch ngân hàng ở trạng thái nháp.");
        }

        await _accountingPeriodService.ValidatePostingDateAsync(tx.TransactionDate, cancellationToken);
        tx.Status = Approved;
        await _dbContext.SaveChangesAsync(cancellationToken);
        await LogAsync("BankTransaction.Approve", $"Duyệt giao dịch ngân hàng {tx.TransactionNo}.", approvedBy ?? "system", cancellationToken);
        return ToTransactionDto(tx);
    }

    public async Task<BankTransactionDto> CancelApprovalAsync(Guid transactionId, string? cancelledBy = null, CancellationToken cancellationToken = default)
    {
        var tx = await _dbContext.BankTransactions.Include(x => x.BankAccount)
            .FirstOrDefaultAsync(x => x.Id == transactionId, cancellationToken)
            ?? throw new InvalidOperationException("Không tìm thấy giao dịch ngân hàng.");
        if (tx.Status != Approved)
        {
            throw new InvalidOperationException("Chỉ được hủy duyệt giao dịch ngân hàng đã duyệt.");
        }

        tx.Status = Draft;
        await _dbContext.SaveChangesAsync(cancellationToken);
        await LogAsync("BankTransaction.CancelApproval", $"Hủy duyệt giao dịch ngân hàng {tx.TransactionNo}.", cancelledBy ?? "system", cancellationToken);
        return ToTransactionDto(tx);
    }

    public async Task<BankTransactionDto> PostTransactionAsync(Guid transactionId, string? postedBy = null, CancellationToken cancellationToken = default)
    {
        await using var dbTransaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
        var tx = await _dbContext.BankTransactions.Include(x => x.BankAccount)
            .FirstOrDefaultAsync(x => x.Id == transactionId, cancellationToken)
            ?? throw new InvalidOperationException("Không tìm thấy giao dịch ngân hàng.");
        if (tx.Status is not (Draft or Approved))
        {
            throw new InvalidOperationException("Chỉ được ghi sổ giao dịch ngân hàng ở trạng thái nháp hoặc đã duyệt.");
        }

        var bankAccount = tx.BankAccount ?? throw new InvalidOperationException("Không tìm thấy tài khoản ngân hàng.");
        if (!bankAccount.IsActive)
        {
            throw new InvalidOperationException("Tài khoản ngân hàng đang tạm tắt.");
        }

        await _accountingPeriodService.ValidatePostingDateAsync(tx.TransactionDate, cancellationToken);
        var bankLedgerAccount = await ResolveBankAccountAsync(bankAccount, cancellationToken);
        var counterAccount = ResolveCounterAccount(tx);
        if (tx.CreditAmount > 0 && bankAccount.CurrentBalance < tx.CreditAmount)
        {
            throw new InvalidOperationException("Số dư ngân hàng không đủ để chi tiền.");
        }

        var user = string.IsNullOrWhiteSpace(postedBy) ? tx.CreatedBy ?? "system" : postedBy.Trim();
        var journal = await CreateAndPostJournalAsync(tx, bankLedgerAccount, counterAccount, user, cancellationToken);

        bankAccount.CurrentBalance += tx.DebitAmount - tx.CreditAmount;
        bankAccount.UpdatedAt = DateTime.UtcNow;
        tx.JournalHeaderId = journal.Id;
        tx.Status = Posted;
        tx.PostedAt = DateTime.UtcNow;
        tx.PostedBy = user;
        tx.BalanceAfter = bankAccount.CurrentBalance;

        await _dbContext.SaveChangesAsync(cancellationToken);
        await dbTransaction.CommitAsync(cancellationToken);
        await LogAsync("BankTransaction.Post", $"Ghi sổ giao dịch ngân hàng {tx.TransactionNo}.", user, cancellationToken);
        return ToTransactionDto(tx);
    }

    public async Task<BankTransactionDto> ReverseTransactionAsync(Guid transactionId, string reason, string? reversedBy = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new InvalidOperationException("Lý do đảo giao dịch là bắt buộc.");
        }

        await using var dbTransaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
        var original = await _dbContext.BankTransactions.Include(x => x.BankAccount)
            .FirstOrDefaultAsync(x => x.Id == transactionId, cancellationToken)
            ?? throw new InvalidOperationException("Không tìm thấy giao dịch ngân hàng.");
        if (original.Status != Posted)
        {
            throw new InvalidOperationException("Chỉ được đảo giao dịch ngân hàng đã ghi sổ.");
        }

        var bankAccount = original.BankAccount ?? throw new InvalidOperationException("Không tìm thấy tài khoản ngân hàng.");
        await _accountingPeriodService.ValidatePostingDateAsync(DateTime.Today, cancellationToken);
        var user = string.IsNullOrWhiteSpace(reversedBy) ? "system" : reversedBy.Trim();
        JournalHeader? reversalJournal = null;
        if (original.JournalHeaderId.HasValue)
        {
            reversalJournal = await _journalService.ReverseAsync(original.JournalHeaderId.Value, reason.Trim(), user, cancellationToken);
        }

        var reversal = new BankTransaction
        {
            Id = Guid.NewGuid(),
            BankAccountId = original.BankAccountId,
            TransactionNo = await GenerateTransactionNoAsync("NH-D", cancellationToken),
            TransactionDate = DateTime.Today,
            ValueDate = DateTime.Today,
            TransactionType = "Adjustment",
            SourceType = "BankTransactionReversal",
            SourceId = original.Id,
            JournalHeaderId = reversalJournal?.Id,
            ReferenceNo = original.TransactionNo,
            Description = $"Đảo giao dịch {original.TransactionNo}: {reason.Trim()}",
            DebitAmount = original.CreditAmount,
            CreditAmount = original.DebitAmount,
            CounterAccountCode = original.CounterAccountCode,
            PartnerType = original.PartnerType,
            PartnerId = original.PartnerId,
            Status = Posted,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = user,
            PostedAt = DateTime.UtcNow,
            PostedBy = user
        };

        bankAccount.CurrentBalance += reversal.DebitAmount - reversal.CreditAmount;
        bankAccount.UpdatedAt = DateTime.UtcNow;
        reversal.BalanceAfter = bankAccount.CurrentBalance;
        original.Status = Reversed;
        _dbContext.BankTransactions.Add(reversal);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await dbTransaction.CommitAsync(cancellationToken);
        await LogAsync("BankTransaction.Reverse", $"Đảo giao dịch ngân hàng {original.TransactionNo}. Lý do: {reason.Trim()}", user, cancellationToken);
        return ToTransactionDto(reversal);
    }

    public async Task<IReadOnlyList<BankBookEntryDto>> GetBankBookAsync(Guid? bankAccountId, DateTime fromDate, DateTime toDate, CancellationToken cancellationToken = default)
    {
        var from = fromDate.Date;
        var to = toDate.Date;
        var accountQuery = _dbContext.BankAccounts.AsNoTracking().AsQueryable();
        if (bankAccountId.HasValue)
        {
            accountQuery = accountQuery.Where(x => x.Id == bankAccountId.Value);
        }

        var balance = (await accountQuery.Select(x => x.OpeningBalance).ToListAsync(cancellationToken)).Sum();
        var beforeQuery = _dbContext.BankTransactions.AsNoTracking().Where(x => (x.Status == Posted || x.Status == Reconciled) && x.TransactionDate < from);
        if (bankAccountId.HasValue)
        {
            beforeQuery = beforeQuery.Where(x => x.BankAccountId == bankAccountId.Value);
        }
        balance += (await beforeQuery.Select(x => new { x.DebitAmount, x.CreditAmount }).ToListAsync(cancellationToken)).Sum(x => x.DebitAmount - x.CreditAmount);

        var query = _dbContext.BankTransactions.AsNoTracking().Where(x => (x.Status == Posted || x.Status == Reconciled) && x.TransactionDate >= from && x.TransactionDate <= to);
        if (bankAccountId.HasValue)
        {
            query = query.Where(x => x.BankAccountId == bankAccountId.Value);
        }

        var transactions = await query.OrderBy(x => x.TransactionDate).ThenBy(x => x.CreatedAt).ThenBy(x => x.TransactionNo).ToListAsync(cancellationToken);
        var result = new List<BankBookEntryDto>(transactions.Count);
        foreach (var tx in transactions)
        {
            balance += tx.DebitAmount - tx.CreditAmount;
            result.Add(new BankBookEntryDto
            {
                Date = tx.TransactionDate,
                TransactionNo = tx.TransactionNo,
                ReferenceNo = tx.ReferenceNo,
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

    public async Task<IReadOnlyList<BankTransactionDto>> GetTransactionsAsync(Guid? bankAccountId, DateTime? fromDate, DateTime? toDate, string? keyword = null, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.BankTransactions
            .AsNoTracking()
            .Include(x => x.BankAccount)
            .AsQueryable();

        if (bankAccountId.HasValue)
        {
            query = query.Where(x => x.BankAccountId == bankAccountId.Value);
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
                || (x.ReferenceNo != null && x.ReferenceNo.Contains(value))
                || (x.Description != null && x.Description.Contains(value))
                || (x.CounterAccountCode != null && x.CounterAccountCode.Contains(value))
                || (x.SourceType != null && x.SourceType.Contains(value)));
        }

        return await query
            .OrderByDescending(x => x.TransactionDate)
            .ThenByDescending(x => x.CreatedAt)
            .Select(x => ToTransactionDto(x, x.BankAccount))
            .ToListAsync(cancellationToken);
    }

    public async Task<BankSummaryDto> GetSummaryAsync(Guid? bankAccountId, DateTime? fromDate, DateTime? toDate, CancellationToken cancellationToken = default)
    {
        var accountQuery = _dbContext.BankAccounts.AsNoTracking().AsQueryable();
        if (bankAccountId.HasValue)
        {
            accountQuery = accountQuery.Where(x => x.Id == bankAccountId.Value);
        }

        var query = _dbContext.BankTransactions.AsNoTracking().Where(x => x.Status == Posted || x.Status == Reconciled);
        if (bankAccountId.HasValue)
        {
            query = query.Where(x => x.BankAccountId == bankAccountId.Value);
        }
        if (fromDate.HasValue)
        {
            query = query.Where(x => x.TransactionDate >= fromDate.Value.Date);
        }
        if (toDate.HasValue)
        {
            query = query.Where(x => x.TransactionDate <= toDate.Value.Date);
        }

        var accountValues = await accountQuery.Select(x => new { x.OpeningBalance, x.CurrentBalance }).ToListAsync(cancellationToken);
        var transactionValues = await query.Select(x => new { x.DebitAmount, x.CreditAmount }).ToListAsync(cancellationToken);
        return new BankSummaryDto
        {
            OpeningBalance = accountValues.Sum(x => x.OpeningBalance),
            TotalReceipts = transactionValues.Sum(x => x.DebitAmount),
            TotalPayments = transactionValues.Sum(x => x.CreditAmount),
            ClosingBalance = accountValues.Sum(x => x.CurrentBalance),
            PostedTransactionCount = transactionValues.Count
        };
    }

    public async Task<BankStatementDto> ImportStatementAsync(Guid bankAccountId, string csvFilePath, string? importedBy = null, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(csvFilePath))
        {
            throw new FileNotFoundException("Không tìm thấy file sao kê CSV.", csvFilePath);
        }

        var account = await _dbContext.BankAccounts.FirstOrDefaultAsync(x => x.Id == bankAccountId, cancellationToken)
            ?? throw new InvalidOperationException("Không tìm thấy tài khoản ngân hàng.");
        var lines = new List<BankStatementLine>();
        foreach (var rawLine in await File.ReadAllLinesAsync(csvFilePath, cancellationToken))
        {
            if (string.IsNullOrWhiteSpace(rawLine) || rawLine.StartsWith("date", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var parts = SplitCsv(rawLine);
            if (parts.Length < 5 || !TryParseDate(parts[0], out var transactionDate))
            {
                continue;
            }

            lines.Add(new BankStatementLine
            {
                Id = Guid.NewGuid(),
                TransactionDate = transactionDate,
                ValueDate = TryParseDate(parts.ElementAtOrDefault(1), out var valueDate) ? valueDate : transactionDate,
                ReferenceNo = NormalizeOptional(parts.ElementAtOrDefault(2)),
                Description = NormalizeOptional(parts.ElementAtOrDefault(3)),
                DebitAmount = ParseDecimal(parts.ElementAtOrDefault(4)),
                CreditAmount = ParseDecimal(parts.ElementAtOrDefault(5)),
                Balance = ParseDecimal(parts.ElementAtOrDefault(6)),
                MatchStatus = Unmatched
            });
        }

        if (lines.Count == 0)
        {
            throw new InvalidOperationException("File sao kê không có dòng hợp lệ.");
        }

        var ordered = lines.OrderBy(x => x.TransactionDate).ThenBy(x => x.ReferenceNo).ToList();
        var first = ordered.First();
        var last = ordered.Last();
        var statement = new BankStatement
        {
            Id = Guid.NewGuid(),
            BankAccountId = account.Id,
            StatementNo = await GenerateStatementNoAsync(cancellationToken),
            FromDate = first.TransactionDate,
            ToDate = last.TransactionDate,
            OpeningBalance = first.Balance - first.DebitAmount + first.CreditAmount,
            ClosingBalance = last.Balance,
            ImportedAt = DateTime.UtcNow,
            ImportedBy = string.IsNullOrWhiteSpace(importedBy) ? "system" : importedBy.Trim(),
            Status = Imported,
            Lines = ordered
        };

        _dbContext.BankStatements.Add(statement);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await LogAsync("BankStatement.Import", $"Import sao kê {statement.StatementNo} cho tài khoản {account.AccountNumber}.", statement.ImportedBy ?? "system", cancellationToken);
        return ToStatementDto(statement);
    }

    public async Task<IReadOnlyList<BankStatementDto>> GetStatementsAsync(Guid? bankAccountId, int take = 50, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.BankStatements
            .AsNoTracking()
            .Include(x => x.Lines)
            .AsQueryable();

        if (bankAccountId.HasValue)
        {
            query = query.Where(x => x.BankAccountId == bankAccountId.Value);
        }

        return await query
            .OrderByDescending(x => x.ImportedAt)
            .Take(Math.Clamp(take, 1, 500))
            .Select(x => ToStatementDto(x))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<BankReconciliationDto>> GetReconciliationsAsync(Guid? bankAccountId, int take = 50, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.BankReconciliations.AsNoTracking().AsQueryable();
        if (bankAccountId.HasValue)
        {
            query = query.Where(x => x.BankAccountId == bankAccountId.Value);
        }

        return await query
            .OrderByDescending(x => x.ReconciliationDate)
            .ThenByDescending(x => x.CreatedAt)
            .Take(Math.Clamp(take, 1, 500))
            .Select(x => ToReconciliationDto(x))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<BankReconciliationLineDto>> GetReconciliationLinesAsync(Guid reconciliationId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.BankReconciliationLines
            .AsNoTracking()
            .Where(x => x.BankReconciliationId == reconciliationId)
            .GroupJoin(_dbContext.BankTransactions.AsNoTracking(), line => line.BankTransactionId, tx => tx.Id, (line, txs) => new { line, tx = txs.FirstOrDefault() })
            .GroupJoin(_dbContext.BankStatementLines.AsNoTracking(), x => x.line.BankStatementLineId, statement => statement.Id, (x, statements) => new { x.line, x.tx, statement = statements.FirstOrDefault() })
            .OrderBy(x => x.statement != null ? x.statement.TransactionDate : DateTime.MaxValue)
            .Select(x => new BankReconciliationLineDto
            {
                Id = x.line.Id,
                BankReconciliationId = x.line.BankReconciliationId,
                BankTransactionId = x.line.BankTransactionId,
                BankTransactionNo = x.tx != null ? x.tx.TransactionNo : null,
                BankStatementLineId = x.line.BankStatementLineId,
                StatementDate = x.statement != null ? x.statement.TransactionDate : null,
                ReferenceNo = x.statement != null ? x.statement.ReferenceNo : x.tx != null ? x.tx.ReferenceNo : null,
                Description = x.statement != null ? x.statement.Description : x.tx != null ? x.tx.Description : null,
                DebitAmount = x.statement != null ? x.statement.DebitAmount : x.tx != null ? x.tx.DebitAmount : 0m,
                CreditAmount = x.statement != null ? x.statement.CreditAmount : x.tx != null ? x.tx.CreditAmount : 0m,
                DifferenceAmount = x.line.DifferenceAmount,
                Note = x.line.Note
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<BankReconciliationDto> ReconcileAsync(Guid bankAccountId, Guid bankStatementId, DateTime reconciliationDate, string? createdBy = null, CancellationToken cancellationToken = default)
    {
        var statement = await _dbContext.BankStatements.Include(x => x.Lines)
            .FirstOrDefaultAsync(x => x.Id == bankStatementId && x.BankAccountId == bankAccountId, cancellationToken)
            ?? throw new InvalidOperationException("Không tìm thấy sao kê ngân hàng.");
        var transactions = await _dbContext.BankTransactions
            .Where(x => x.BankAccountId == bankAccountId && (x.Status == Posted || x.Status == Reconciled) && x.TransactionDate >= statement.FromDate.AddDays(-3) && x.TransactionDate <= statement.ToDate.AddDays(3))
            .ToListAsync(cancellationToken);

        var reconciliation = new BankReconciliation
        {
            Id = Guid.NewGuid(),
            BankAccountId = bankAccountId,
            ReconciliationNo = await GenerateReconciliationNoAsync(cancellationToken),
            ReconciliationDate = reconciliationDate.Date,
            BookBalance = transactions.Where(x => x.TransactionDate <= reconciliationDate.Date).Sum(x => x.DebitAmount - x.CreditAmount),
            StatementBalance = statement.ClosingBalance,
            Status = Draft,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = string.IsNullOrWhiteSpace(createdBy) ? "system" : createdBy.Trim()
        };
        reconciliation.Difference = reconciliation.StatementBalance - reconciliation.BookBalance;

        foreach (var line in statement.Lines.Where(x => x.MatchStatus == Unmatched).OrderBy(x => x.TransactionDate))
        {
            var candidate = transactions.FirstOrDefault(x =>
                (x.Status == Posted || x.Status == Reconciled) &&
                Math.Abs((x.TransactionDate.Date - line.TransactionDate.Date).TotalDays) <= 3 &&
                x.DebitAmount == line.DebitAmount &&
                x.CreditAmount == line.CreditAmount &&
                (string.IsNullOrWhiteSpace(line.ReferenceNo) || string.Equals(x.ReferenceNo, line.ReferenceNo, StringComparison.OrdinalIgnoreCase)));
            if (candidate is null)
            {
                reconciliation.Lines.Add(new BankReconciliationLine { Id = Guid.NewGuid(), BankStatementLineId = line.Id, DifferenceAmount = line.DebitAmount - line.CreditAmount, Note = "Chưa khớp" });
                continue;
            }

            line.MatchStatus = Matched;
            line.MatchedBankTransactionId = candidate.Id;
            candidate.Status = Reconciled;
            reconciliation.Lines.Add(new BankReconciliationLine { Id = Guid.NewGuid(), BankTransactionId = candidate.Id, BankStatementLineId = line.Id, DifferenceAmount = 0m, Note = "Khớp tự động" });
        }

        _dbContext.BankReconciliations.Add(reconciliation);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await LogAsync("BankReconciliation.Create", $"Tạo đối chiếu ngân hàng {reconciliation.ReconciliationNo}.", reconciliation.CreatedBy ?? "system", cancellationToken);
        return ToReconciliationDto(reconciliation);
    }

    public async Task<BankReconciliationDto> ConfirmReconciliationAsync(Guid reconciliationId, string? confirmedBy = null, CancellationToken cancellationToken = default)
    {
        var reconciliation = await _dbContext.BankReconciliations.FirstOrDefaultAsync(x => x.Id == reconciliationId, cancellationToken)
            ?? throw new InvalidOperationException("Không tìm thấy đối chiếu ngân hàng.");
        if (reconciliation.Status != Draft)
        {
            throw new InvalidOperationException("Chỉ được xác nhận đối chiếu ở trạng thái nháp.");
        }

        reconciliation.Status = Confirmed;
        reconciliation.ConfirmedAt = DateTime.UtcNow;
        reconciliation.ConfirmedBy = string.IsNullOrWhiteSpace(confirmedBy) ? "system" : confirmedBy.Trim();
        await _dbContext.SaveChangesAsync(cancellationToken);
        if (reconciliation.Difference != 0)
        {
            await LogAsync("BankReconciliation.AdjustmentPending", $"Đối chiếu {reconciliation.ReconciliationNo} còn chênh lệch {reconciliation.Difference:N2}; chưa tự tạo bút toán điều chỉnh ở Step 14.", reconciliation.ConfirmedBy, cancellationToken);
        }
        await LogAsync("BankReconciliation.Confirm", $"Xác nhận đối chiếu ngân hàng {reconciliation.ReconciliationNo}.", reconciliation.ConfirmedBy, cancellationToken);
        return ToReconciliationDto(reconciliation);
    }

    private async Task<BankTransactionDto> CreateTransactionAsync(BankTransactionRequest request, string transactionType, CancellationToken cancellationToken)
    {
        if (request.Amount <= 0)
        {
            throw new InvalidOperationException("Số tiền giao dịch ngân hàng phải lớn hơn 0.");
        }
        var account = await _dbContext.BankAccounts.FirstOrDefaultAsync(x => x.Id == request.BankAccountId, cancellationToken)
            ?? throw new InvalidOperationException("Không tìm thấy tài khoản ngân hàng.");
        if (!account.IsActive)
        {
            throw new InvalidOperationException("Tài khoản ngân hàng đang tạm tắt.");
        }

        var isReceipt = transactionType == Receipt;
        var tx = new BankTransaction
        {
            Id = Guid.NewGuid(),
            BankAccountId = account.Id,
            TransactionNo = await GenerateTransactionNoAsync(isReceipt ? "BC" : "BN", cancellationToken),
            TransactionDate = request.TransactionDate.Date,
            ValueDate = request.ValueDate?.Date,
            TransactionType = transactionType,
            SourceType = NormalizeOptional(request.SourceType),
            SourceId = request.SourceId,
            ReferenceNo = NormalizeOptional(request.ReferenceNo),
            Description = NormalizeOptional(request.Description),
            DebitAmount = isReceipt ? request.Amount : 0m,
            CreditAmount = isReceipt ? 0m : request.Amount,
            CounterAccountCode = NormalizeOptional(request.CounterAccountCode),
            PartnerType = NormalizeOptional(request.PartnerType),
            PartnerId = request.PartnerId,
            Status = Draft,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = string.IsNullOrWhiteSpace(request.CreatedBy) ? "system" : request.CreatedBy.Trim()
        };

        _dbContext.BankTransactions.Add(tx);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await LogAsync("BankTransaction.Create", $"Tạo giao dịch ngân hàng {tx.TransactionNo}.", tx.CreatedBy, cancellationToken);
        return ToTransactionDto(tx, account);
    }

    private async Task<JournalHeader> CreateAndPostJournalAsync(BankTransaction tx, string bankAccountCode, string counterAccountCode, string user, CancellationToken cancellationToken)
    {
        var accounts = await _dbContext.LedgerAccounts
            .Where(x => x.AccountCode == bankAccountCode || x.AccountCode == counterAccountCode)
            .ToDictionaryAsync(x => x.AccountCode, x => x.AccountName, cancellationToken);
        var bankAccountName = accounts.TryGetValue(bankAccountCode, out var bankName) ? bankName : bankAccountCode;
        var counterAccountName = accounts.TryGetValue(counterAccountCode, out var counterName) ? counterName : counterAccountCode;
        var isDebitBank = tx.DebitAmount > 0;
        var amount = isDebitBank ? tx.DebitAmount : tx.CreditAmount;
        var customerId = IsPartnerType(tx, "Customer") ? tx.PartnerId : null;
        var supplierId = IsPartnerType(tx, "Supplier") ? tx.PartnerId : null;
        var employeeId = IsPartnerType(tx, "Employee") ? tx.PartnerId : null;
        var currency = tx.BankAccount?.CurrencyCode ?? "VND";
        var journal = new JournalHeader
        {
            Id = Guid.NewGuid(),
            JournalNo = await GenerateJournalNoAsync(cancellationToken),
            JournalDate = tx.TransactionDate,
            PostingDate = tx.TransactionDate,
            SourceType = "BankTransaction",
            SourceId = tx.Id,
            SourceNo = tx.TransactionNo,
            Description = string.IsNullOrWhiteSpace(tx.Description) ? $"Ghi sổ giao dịch ngân hàng {tx.TransactionNo}" : tx.Description!,
            PeriodCode = tx.TransactionDate.ToString("yyyy-MM"),
            CreatedBy = user,
            CreatedAt = DateTime.UtcNow,
            Lines = new List<JournalLine>()
        };

        if (isDebitBank)
        {
            journal.Lines.Add(CreateJournalLine(1, bankAccountCode, bankAccountName, amount, 0m, currency, tx, customerId, supplierId, employeeId, $"Thu ngân hàng {tx.TransactionNo}"));
            journal.Lines.Add(CreateJournalLine(2, counterAccountCode, counterAccountName, 0m, amount, currency, tx, customerId, supplierId, employeeId, $"Đối ứng thu ngân hàng {tx.TransactionNo}"));
        }
        else
        {
            journal.Lines.Add(CreateJournalLine(1, counterAccountCode, counterAccountName, amount, 0m, currency, tx, customerId, supplierId, employeeId, $"Đối ứng chi ngân hàng {tx.TransactionNo}"));
            journal.Lines.Add(CreateJournalLine(2, bankAccountCode, bankAccountName, 0m, amount, currency, tx, customerId, supplierId, employeeId, $"Chi ngân hàng {tx.TransactionNo}"));
        }

        var draft = await _journalService.CreateDraftAsync(journal, cancellationToken);
        return await _journalService.PostDraftAsync(draft.Id, user, cancellationToken);
    }

    private static JournalLine CreateJournalLine(int lineNo, string accountCode, string accountName, decimal debit, decimal credit, string currency, BankTransaction tx, Guid? customerId, Guid? supplierId, Guid? employeeId, string description)
        => new()
        {
            Id = Guid.NewGuid(),
            LineNo = lineNo,
            AccountCode = accountCode,
            AccountName = accountName,
            DebitAmount = debit,
            CreditAmount = credit,
            CurrencyCode = currency,
            ExchangeRate = 1m,
            ObjectType = tx.PartnerType,
            ObjectId = tx.PartnerId,
            CustomerId = customerId,
            SupplierId = supplierId,
            EmployeeId = employeeId,
            Description = description
        };

    private async Task<string> ResolveBankAccountAsync(BankAccount account, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(account.AccountCode))
        {
            return account.AccountCode.Trim();
        }
        var standardCode = await _dbContext.AccountingStandards.Where(x => x.IsDefault && x.IsActive).Select(x => x.Code).FirstOrDefaultAsync(cancellationToken) ?? "TT99";
        var resolved = await _postingRuleService.ResolveAccountByRoleAsync("Bank", standardCode, cancellationToken);
        if (string.IsNullOrWhiteSpace(resolved))
        {
            throw new InvalidOperationException($"Chưa cấu hình AccountRoleMapping cho Bank theo chuẩn {standardCode}.");
        }
        return resolved;
    }

    private static string ResolveCounterAccount(BankTransaction tx)
    {
        if (string.IsNullOrWhiteSpace(tx.CounterAccountCode))
        {
            throw new InvalidOperationException("Tài khoản đối ứng là bắt buộc khi ghi sổ giao dịch ngân hàng.");
        }
        return tx.CounterAccountCode.Trim();
    }

    private static bool IsPartnerType(BankTransaction tx, string partnerType)
        => string.Equals(tx.PartnerType, partnerType, StringComparison.OrdinalIgnoreCase);

    private static string[] SplitCsv(string line)
    {
        var parts = new List<string>();
        var current = new List<char>();
        var quoted = false;
        for (var i = 0; i < line.Length; i++)
        {
            var ch = line[i];
            if (ch == '"')
            {
                if (quoted && i + 1 < line.Length && line[i + 1] == '"')
                {
                    current.Add('"');
                    i++;
                }
                else
                {
                    quoted = !quoted;
                }
                continue;
            }
            if (ch == ',' && !quoted)
            {
                parts.Add(new string(current.ToArray()).Trim());
                current.Clear();
            }
            else
            {
                current.Add(ch);
            }
        }
        parts.Add(new string(current.ToArray()).Trim());
        return parts.ToArray();
    }

    private static bool TryParseDate(string? value, out DateTime date)
        => DateTime.TryParse(value, CultureInfo.GetCultureInfo("vi-VN"), DateTimeStyles.None, out date)
            || DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out date);

    private static decimal ParseDecimal(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return 0m;
        }
        var normalized = value.Trim().Replace(" ", string.Empty);
        return decimal.TryParse(normalized, NumberStyles.Any, CultureInfo.GetCultureInfo("vi-VN"), out var vi)
            ? vi
            : decimal.TryParse(normalized, NumberStyles.Any, CultureInfo.InvariantCulture, out var inv) ? inv : 0m;
    }

    private async Task<string> GenerateTransactionNoAsync(string prefix, CancellationToken cancellationToken)
    {
        string value;
        do
        {
            value = $"{prefix}-{DateTime.UtcNow:yyyyMMdd-HHmmss}-{Random.Shared.Next(0, 9999):D4}";
        }
        while (await _dbContext.BankTransactions.AnyAsync(x => x.TransactionNo == value, cancellationToken));
        return value;
    }

    private async Task<string> GenerateStatementNoAsync(CancellationToken cancellationToken)
    {
        string value;
        do
        {
            value = $"SKNH-{DateTime.UtcNow:yyyyMMdd-HHmmss}-{Random.Shared.Next(0, 9999):D4}";
        }
        while (await _dbContext.BankStatements.AnyAsync(x => x.StatementNo == value, cancellationToken));
        return value;
    }

    private async Task<string> GenerateReconciliationNoAsync(CancellationToken cancellationToken)
    {
        string value;
        do
        {
            value = $"DCNH-{DateTime.UtcNow:yyyyMMdd-HHmmss}-{Random.Shared.Next(0, 9999):D4}";
        }
        while (await _dbContext.BankReconciliations.AnyAsync(x => x.ReconciliationNo == value, cancellationToken));
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

    private static void ValidateAccountRequest(BankAccountRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.BankCode)) throw new InvalidOperationException("Mã ngân hàng là bắt buộc.");
        if (string.IsNullOrWhiteSpace(request.BankName)) throw new InvalidOperationException("Tên ngân hàng là bắt buộc.");
        if (string.IsNullOrWhiteSpace(request.AccountNumber)) throw new InvalidOperationException("Số tài khoản là bắt buộc.");
        if (string.IsNullOrWhiteSpace(request.AccountName)) throw new InvalidOperationException("Tên tài khoản là bắt buộc.");
    }

    private async Task ClearDefaultAccountsAsync(CancellationToken cancellationToken)
    {
        var defaults = await _dbContext.BankAccounts.Where(x => x.IsDefault).ToListAsync(cancellationToken);
        foreach (var account in defaults)
        {
            account.IsDefault = false;
            account.UpdatedAt = DateTime.UtcNow;
        }
    }

    private static string NormalizeCurrency(string? currencyCode)
        => string.IsNullOrWhiteSpace(currencyCode) ? "VND" : currencyCode.Trim().ToUpperInvariant();

    private static string? NormalizeOptional(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static BankAccountDto ToAccountDto(BankAccount account) => new()
    {
        Id = account.Id,
        BankCode = account.BankCode,
        BankName = account.BankName,
        BranchName = account.BranchName,
        AccountNumber = account.AccountNumber,
        AccountName = account.AccountName,
        CurrencyCode = account.CurrencyCode,
        AccountCode = account.AccountCode,
        OpeningBalance = account.OpeningBalance,
        CurrentBalance = account.CurrentBalance,
        IsDefault = account.IsDefault,
        IsActive = account.IsActive
    };

    private static BankTransactionDto ToTransactionDto(BankTransaction tx, BankAccount? account = null)
    {
        account ??= tx.BankAccount;
        return new BankTransactionDto
        {
            Id = tx.Id,
            BankAccountId = tx.BankAccountId,
            AccountNumber = account?.AccountNumber ?? string.Empty,
            BankName = account?.BankName ?? string.Empty,
            TransactionNo = tx.TransactionNo,
            TransactionDate = tx.TransactionDate,
            ValueDate = tx.ValueDate,
            TransactionType = tx.TransactionType,
            SourceType = tx.SourceType,
            SourceId = tx.SourceId,
            JournalHeaderId = tx.JournalHeaderId,
            ReferenceNo = tx.ReferenceNo,
            Description = tx.Description,
            DebitAmount = tx.DebitAmount,
            CreditAmount = tx.CreditAmount,
            BalanceAfter = tx.BalanceAfter,
            CounterAccountCode = tx.CounterAccountCode,
            PartnerType = tx.PartnerType,
            PartnerId = tx.PartnerId,
            Status = tx.Status,
            CreatedAt = tx.CreatedAt,
            CreatedBy = tx.CreatedBy,
            PostedAt = tx.PostedAt,
            PostedBy = tx.PostedBy
        };
    }

    private static BankStatementDto ToStatementDto(BankStatement statement) => new()
    {
        Id = statement.Id,
        BankAccountId = statement.BankAccountId,
        StatementNo = statement.StatementNo,
        FromDate = statement.FromDate,
        ToDate = statement.ToDate,
        OpeningBalance = statement.OpeningBalance,
        ClosingBalance = statement.ClosingBalance,
        ImportedAt = statement.ImportedAt,
        ImportedBy = statement.ImportedBy,
        Status = statement.Status,
        Lines = statement.Lines.OrderBy(x => x.TransactionDate).Select(x => new BankStatementLineDto
        {
            Id = x.Id,
            TransactionDate = x.TransactionDate,
            ValueDate = x.ValueDate,
            ReferenceNo = x.ReferenceNo,
            Description = x.Description,
            DebitAmount = x.DebitAmount,
            CreditAmount = x.CreditAmount,
            Balance = x.Balance,
            MatchedBankTransactionId = x.MatchedBankTransactionId,
            MatchStatus = x.MatchStatus
        }).ToList()
    };

    private static BankReconciliationDto ToReconciliationDto(BankReconciliation reconciliation) => new()
    {
        Id = reconciliation.Id,
        BankAccountId = reconciliation.BankAccountId,
        ReconciliationNo = reconciliation.ReconciliationNo,
        ReconciliationDate = reconciliation.ReconciliationDate,
        BookBalance = reconciliation.BookBalance,
        StatementBalance = reconciliation.StatementBalance,
        Difference = reconciliation.Difference,
        Status = reconciliation.Status,
        CreatedAt = reconciliation.CreatedAt,
        CreatedBy = reconciliation.CreatedBy,
        ConfirmedAt = reconciliation.ConfirmedAt,
        ConfirmedBy = reconciliation.ConfirmedBy
    };

    private async Task LogAsync(string action, string detail, string username, CancellationToken cancellationToken)
    {
        if (_auditLogService is not null)
        {
            await _auditLogService.LogAsync(action, detail, username, cancellationToken);
        }
    }
}


