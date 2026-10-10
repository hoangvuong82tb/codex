using ASMILE.BO.Application.Interfaces;
using ASMILE.BO.Core.Entities;
using ASMILE.BO.Core.Enums;
using ASMILE.BO.Core.Interfaces.Services;
using ASMILE.BO.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ASMILE.BO.Infrastructure.Services;

public sealed class DebtTransactionService : IDebtTransactionService
{
    private const string ReceivableRole = "Receivable";
    private const string PayableRole = "Payable";
    private const string ReceivableTarget = "Receivable";
    private const string PayableTarget = "Payable";
    private const int DefaultDueDays = 30;

    private readonly AppDbContext _dbContext;
    private readonly IAuditLogService? _auditLogService;

    public DebtTransactionService(AppDbContext dbContext, IAuditLogService? auditLogService = null)
    {
        _dbContext = dbContext;
        _auditLogService = auditLogService;
    }

    public async Task SyncFromJournalAsync(Guid journalHeaderId, CancellationToken cancellationToken = default)
    {
        var journal = await _dbContext.JournalHeaders
            .Include(x => x.Lines)
            .FirstOrDefaultAsync(x => x.Id == journalHeaderId, cancellationToken);

        if (journal is null)
        {
            throw new InvalidOperationException($"Journal not found: {journalHeaderId}.");
        }

        await SyncFromJournalAsync(journal, cancellationToken);
    }

    public async Task SyncFromJournalAsync(JournalHeader journal, CancellationToken cancellationToken = default)
    {
        if (journal.Status != JournalStatus.Posted)
        {
            return;
        }

        if (journal.Lines.Count == 0)
        {
            await _dbContext.Entry(journal).Collection(x => x.Lines).LoadAsync(cancellationToken);
        }

        var roleByAccount = await BuildRoleMapAsync(cancellationToken);
        var now = DateTime.UtcNow;
        var created = false;

        foreach (var line in journal.Lines.OrderBy(x => x.LineNo))
        {
            if (!roleByAccount.TryGetValue(line.AccountCode, out var role))
            {
                continue;
            }

            if (string.Equals(role, ReceivableRole, StringComparison.OrdinalIgnoreCase))
            {
                created |= await SyncReceivableLineAsync(journal, line, now, cancellationToken);
                continue;
            }

            if (string.Equals(role, PayableRole, StringComparison.OrdinalIgnoreCase))
            {
                created |= await SyncPayableLineAsync(journal, line, now, cancellationToken);
            }
        }

        if (created)
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
            await LogAsync("Debt.SyncFromJournal", $"Synced AR/AP transactions from journal {journal.JournalNo}.", journal.PostedBy ?? journal.CreatedBy, cancellationToken);
        }
    }

    public async Task<IReadOnlyList<ReceivableTransaction>> GetReceivablesAsync(Guid? customerId = null, DateTime? fromDate = null, DateTime? toDate = null, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.ReceivableTransactions.AsNoTracking().AsQueryable();
        if (customerId.HasValue)
        {
            query = query.Where(x => x.CustomerId == customerId.Value);
        }

        if (fromDate.HasValue)
        {
            query = query.Where(x => x.DocumentDate.Date >= fromDate.Value.Date);
        }

        if (toDate.HasValue)
        {
            query = query.Where(x => x.DocumentDate.Date <= toDate.Value.Date);
        }

        return await query
            .OrderByDescending(x => x.DocumentDate)
            .ThenByDescending(x => x.DocumentNo)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<PayableTransaction>> GetPayablesAsync(Guid? supplierId = null, DateTime? fromDate = null, DateTime? toDate = null, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.PayableTransactions.AsNoTracking().AsQueryable();
        if (supplierId.HasValue)
        {
            query = query.Where(x => x.SupplierId == supplierId.Value);
        }

        if (fromDate.HasValue)
        {
            query = query.Where(x => x.DocumentDate.Date >= fromDate.Value.Date);
        }

        if (toDate.HasValue)
        {
            query = query.Where(x => x.DocumentDate.Date <= toDate.Value.Date);
        }

        return await query
            .OrderByDescending(x => x.DocumentDate)
            .ThenByDescending(x => x.DocumentNo)
            .ToListAsync(cancellationToken);
    }

    public async Task<PaymentAllocation> AllocatePaymentAsync(string paymentSourceType, Guid? paymentSourceId, Guid? paymentJournalHeaderId, string targetType, Guid targetTransactionId, decimal allocatedAmount, DateTime allocationDate, string? createdBy = null, CancellationToken cancellationToken = default)
    {
        if (allocatedAmount <= 0)
        {
            throw new InvalidOperationException("Số tiền phân bổ phải lớn hơn 0.");
        }

        if (string.Equals(paymentSourceType, ReceivableTarget, StringComparison.OrdinalIgnoreCase))
        {
            if (!paymentSourceId.HasValue)
            {
                throw new InvalidOperationException("Phân bổ phải có giao dịch thu nguồn.");
            }

            var source = await _dbContext.ReceivableTransactions.FirstOrDefaultAsync(x => x.Id == paymentSourceId.Value, cancellationToken)
                ?? throw new InvalidOperationException("Không tìm thấy giao dịch thu nguồn.");

            if (source.CreditAmount <= 0)
            {
                throw new InvalidOperationException("Giao dịch nguồn không phải khoản thu có thể phân bổ.");
            }

            if (source.Id == targetTransactionId)
            {
                throw new InvalidOperationException("Không thể phân bổ giao dịch thu vào chính nó.");
            }

            var allocatedFromSource = await _dbContext.PaymentAllocations
                .Where(x => x.PaymentSourceType == ReceivableTarget && x.PaymentSourceId == source.Id)
                .SumAsync(x => (decimal?)x.AllocatedAmount, cancellationToken) ?? 0m;
            var availableFromSource = Math.Max(0, source.CreditAmount - allocatedFromSource);
            if (allocatedAmount > availableFromSource)
            {
                throw new InvalidOperationException($"Số tiền phân bổ vượt số dư chưa phân bổ của khoản thu ({availableFromSource:N2}).");
            }

            if (string.Equals(targetType, ReceivableTarget, StringComparison.OrdinalIgnoreCase))
            {
                var target = await _dbContext.ReceivableTransactions.FirstOrDefaultAsync(x => x.Id == targetTransactionId, cancellationToken)
                    ?? throw new InvalidOperationException("Không tìm thấy giao dịch phải thu cần phân bổ.");
                if (target.CustomerId != source.CustomerId)
                {
                    throw new InvalidOperationException("Khoản thu và khoản công nợ phải cùng khách hàng.");
                }

                var targetBalance = Math.Max(0, target.BalanceAmount);
                if (allocatedAmount > targetBalance)
                {
                    throw new InvalidOperationException($"Số tiền phân bổ vượt số dư công nợ còn lại ({targetBalance:N2}).");
                }
            }
        }

        var now = DateTime.UtcNow;
        var allocation = new PaymentAllocation
        {
            Id = Guid.NewGuid(),
            PaymentSourceType = paymentSourceType,
            PaymentSourceId = paymentSourceId,
            PaymentJournalHeaderId = paymentJournalHeaderId,
            TargetType = targetType,
            TargetTransactionId = targetTransactionId,
            AllocatedAmount = allocatedAmount,
            AllocationDate = allocationDate,
            CreatedAt = now,
            CreatedBy = createdBy
        };

        if (string.Equals(targetType, ReceivableTarget, StringComparison.OrdinalIgnoreCase))
        {
            var transaction = await _dbContext.ReceivableTransactions.FirstOrDefaultAsync(x => x.Id == targetTransactionId, cancellationToken)
                ?? throw new InvalidOperationException("Không tìm thấy giao dịch phải thu cần phân bổ.");
            transaction.BalanceAmount = Math.Max(0, transaction.BalanceAmount - allocatedAmount);
            transaction.Status = ResolveDebtStatus(transaction.BalanceAmount, transaction.DebitAmount, transaction.CreditAmount);
            transaction.UpdatedAt = now;
        }
        else if (string.Equals(targetType, PayableTarget, StringComparison.OrdinalIgnoreCase))
        {
            var transaction = await _dbContext.PayableTransactions.FirstOrDefaultAsync(x => x.Id == targetTransactionId, cancellationToken)
                ?? throw new InvalidOperationException("Không tìm thấy giao dịch phải trả cần phân bổ.");
            transaction.BalanceAmount = Math.Max(0, transaction.BalanceAmount - allocatedAmount);
            transaction.Status = ResolveDebtStatus(transaction.BalanceAmount, transaction.CreditAmount, transaction.DebitAmount);
            transaction.UpdatedAt = now;
        }
        else
        {
            throw new InvalidOperationException($"Loại công nợ không hợp lệ: {targetType}.");
        }

        _dbContext.PaymentAllocations.Add(allocation);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await LogAsync("Debt.AllocatePayment", $"Allocated {allocatedAmount:N2} to {targetType} transaction {targetTransactionId}.", createdBy ?? "system", cancellationToken);
        return allocation;
    }

    public async Task<int> AutoAllocatePaymentAsync(Guid paymentSourceId, string? createdBy = null, CancellationToken cancellationToken = default)
    {
        var source = await _dbContext.ReceivableTransactions
            .FirstOrDefaultAsync(x => x.Id == paymentSourceId, cancellationToken)
            ?? throw new InvalidOperationException("Không tìm thấy giao dịch thu nguồn.");
        if (source.CreditAmount <= 0 || string.Equals(source.Status, "Cancelled", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Giao dịch nguồn không còn khả dụng để tự động phân bổ.");

        var allocated = await _dbContext.PaymentAllocations
            .Where(x => x.PaymentSourceType == ReceivableTarget && x.PaymentSourceId == paymentSourceId)
            .SumAsync(x => (decimal?)x.AllocatedAmount, cancellationToken) ?? 0m;
        var remaining = Math.Max(0, source.CreditAmount - allocated);
        if (remaining <= 0) return 0;

        var targets = await _dbContext.ReceivableTransactions
            .Where(x => x.CustomerId == source.CustomerId && x.DebitAmount > 0 && x.BalanceAmount > 0)
            .OrderBy(x => x.DocumentDate)
            .ThenBy(x => x.DocumentNo)
            .ToListAsync(cancellationToken);

        var created = new List<PaymentAllocation>();
        foreach (var target in targets)
        {
            if (remaining <= 0) break;
            if (target.Id == source.Id) continue;

            var amount = Math.Min(remaining, target.BalanceAmount);
            if (amount <= 0) continue;

            created.Add(new PaymentAllocation
            {
                Id = Guid.NewGuid(),
                PaymentSourceType = ReceivableTarget,
                PaymentSourceId = paymentSourceId,
                TargetType = ReceivableTarget,
                TargetTransactionId = target.Id,
                AllocatedAmount = amount,
                AllocationDate = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = createdBy
            });
            target.BalanceAmount = Math.Max(0, target.BalanceAmount - amount);
            target.Status = ResolveDebtStatus(target.BalanceAmount, target.DebitAmount, target.CreditAmount);
            target.UpdatedAt = DateTime.UtcNow;
            remaining -= amount;
        }

        if (created.Count == 0) return 0;
        _dbContext.PaymentAllocations.AddRange(created);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await LogAsync("Debt.AutoAllocatePayment", $"Created {created.Count} allocations for payment {paymentSourceId}.", createdBy ?? "system", cancellationToken);
        return created.Count;
    }

    public async Task<int> UndoPaymentAllocationsAsync(Guid paymentSourceId, string? createdBy = null, CancellationToken cancellationToken = default)
    {
        var allocations = await _dbContext.PaymentAllocations
            .Where(x => x.PaymentSourceType == ReceivableTarget && x.PaymentSourceId == paymentSourceId)
            .ToListAsync(cancellationToken);
        if (allocations.Count == 0) return 0;

        var targetIds = allocations
            .Where(x => x.TargetType == ReceivableTarget)
            .Select(x => x.TargetTransactionId)
            .Distinct()
            .ToList();
        var targets = await _dbContext.ReceivableTransactions
            .Where(x => targetIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, cancellationToken);
        var missingTarget = targetIds.FirstOrDefault(x => !targets.ContainsKey(x));
        if (missingTarget != Guid.Empty)
        {
            throw new InvalidOperationException($"Không tìm thấy công nợ đích của allocation {missingTarget}.");
        }

        foreach (var group in allocations.Where(x => x.TargetType == ReceivableTarget).GroupBy(x => x.TargetTransactionId))
        {
            var target = targets[group.Key];
            target.BalanceAmount += group.Sum(x => x.AllocatedAmount);
            target.Status = ResolveDebtStatus(target.BalanceAmount, target.DebitAmount, target.CreditAmount);
            target.UpdatedAt = DateTime.UtcNow;
        }

        _dbContext.PaymentAllocations.RemoveRange(allocations);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await LogAsync("Debt.UndoPaymentAllocations", $"Removed {allocations.Count} allocations for payment {paymentSourceId}.", createdBy ?? "system", cancellationToken);
        return allocations.Count;
    }

    public async Task RecalculateBalancesAsync(string targetType, Guid? partnerId = null, CancellationToken cancellationToken = default)
    {
        if (string.Equals(targetType, ReceivableTarget, StringComparison.OrdinalIgnoreCase))
        {
            var query = _dbContext.ReceivableTransactions.AsQueryable();
            if (partnerId.HasValue)
            {
                query = query.Where(x => x.CustomerId == partnerId.Value);
            }

            var transactions = await query.ToListAsync(cancellationToken);
            var allocations = await _dbContext.PaymentAllocations
                .Where(x => x.TargetType == ReceivableTarget && transactions.Select(t => t.Id).Contains(x.TargetTransactionId))
                .GroupBy(x => x.TargetTransactionId)
                .Select(x => new { TransactionId = x.Key, Amount = x.Sum(a => a.AllocatedAmount) })
                .ToDictionaryAsync(x => x.TransactionId, x => x.Amount, cancellationToken);

            foreach (var transaction in transactions)
            {
                var baseBalance = Math.Max(0, transaction.DebitAmount - transaction.CreditAmount);
                transaction.BalanceAmount = Math.Max(0, baseBalance - allocations.GetValueOrDefault(transaction.Id));
                transaction.Status = ResolveDebtStatus(transaction.BalanceAmount, transaction.DebitAmount, transaction.CreditAmount);
                transaction.UpdatedAt = DateTime.UtcNow;
            }
        }
        else if (string.Equals(targetType, PayableTarget, StringComparison.OrdinalIgnoreCase))
        {
            var query = _dbContext.PayableTransactions.AsQueryable();
            if (partnerId.HasValue)
            {
                query = query.Where(x => x.SupplierId == partnerId.Value);
            }

            var transactions = await query.ToListAsync(cancellationToken);
            var allocations = await _dbContext.PaymentAllocations
                .Where(x => x.TargetType == PayableTarget && transactions.Select(t => t.Id).Contains(x.TargetTransactionId))
                .GroupBy(x => x.TargetTransactionId)
                .Select(x => new { TransactionId = x.Key, Amount = x.Sum(a => a.AllocatedAmount) })
                .ToDictionaryAsync(x => x.TransactionId, x => x.Amount, cancellationToken);

            foreach (var transaction in transactions)
            {
                var baseBalance = Math.Max(0, transaction.CreditAmount - transaction.DebitAmount);
                transaction.BalanceAmount = Math.Max(0, baseBalance - allocations.GetValueOrDefault(transaction.Id));
                transaction.Status = ResolveDebtStatus(transaction.BalanceAmount, transaction.CreditAmount, transaction.DebitAmount);
                transaction.UpdatedAt = DateTime.UtcNow;
            }
        }
        else
        {
            throw new InvalidOperationException($"Loại công nợ không hợp lệ: {targetType}.");
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AgingSnapshot>> BuildAgingSnapshotAsync(DateTime snapshotDate, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var snapshots = new List<AgingSnapshot>();

        var receivables = await _dbContext.ReceivableTransactions
            .AsNoTracking()
            .Where(x => x.BalanceAmount > 0 && x.Status != "Cancelled")
            .ToListAsync(cancellationToken);
        snapshots.AddRange(BuildSnapshots(receivables, snapshotDate.Date, "Customer", now, x => x.CustomerId));

        var payables = await _dbContext.PayableTransactions
            .AsNoTracking()
            .Where(x => x.BalanceAmount > 0 && x.Status != "Cancelled")
            .ToListAsync(cancellationToken);
        snapshots.AddRange(BuildSnapshots(payables, snapshotDate.Date, "Supplier", now, x => x.SupplierId));

        _dbContext.AgingSnapshots.AddRange(snapshots);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return snapshots;
    }

    private async Task<bool> SyncReceivableLineAsync(JournalHeader journal, JournalLine line, DateTime now, CancellationToken cancellationToken)
    {
        if (line.CustomerId is null)
        {
            await LogAsync("Debt.SyncSkipped", $"Skipped receivable line {line.Id} from journal {journal.JournalNo}: missing customer.", journal.PostedBy ?? journal.CreatedBy, cancellationToken);
            return false;
        }

        if (await _dbContext.ReceivableTransactions.AnyAsync(x => x.JournalLineId == line.Id, cancellationToken))
        {
            return false;
        }

        var balance = line.DebitAmount - line.CreditAmount;
        _dbContext.ReceivableTransactions.Add(new ReceivableTransaction
        {
            Id = Guid.NewGuid(),
            CustomerId = line.CustomerId.Value,
            SourceType = journal.SourceType,
            SourceId = journal.SourceId,
            JournalHeaderId = journal.Id,
            JournalLineId = line.Id,
            DocumentNo = string.IsNullOrWhiteSpace(journal.SourceNo) ? journal.JournalNo : journal.SourceNo,
            DocumentDate = journal.JournalDate.Date,
            DueDate = journal.JournalDate.Date.AddDays(DefaultDueDays),
            Description = line.Description ?? journal.Description,
            DebitAmount = line.DebitAmount,
            CreditAmount = line.CreditAmount,
            BalanceAmount = balance,
            CurrencyCode = string.IsNullOrWhiteSpace(line.CurrencyCode) ? "VND" : line.CurrencyCode,
            Status = ResolveDebtStatus(balance, line.DebitAmount, line.CreditAmount),
            CreatedAt = now,
            UpdatedAt = now
        });
        return true;
    }

    private async Task<bool> SyncPayableLineAsync(JournalHeader journal, JournalLine line, DateTime now, CancellationToken cancellationToken)
    {
        if (line.SupplierId is null)
        {
            await LogAsync("Debt.SyncSkipped", $"Skipped payable line {line.Id} from journal {journal.JournalNo}: missing supplier.", journal.PostedBy ?? journal.CreatedBy, cancellationToken);
            return false;
        }

        if (await _dbContext.PayableTransactions.AnyAsync(x => x.JournalLineId == line.Id, cancellationToken))
        {
            return false;
        }

        var balance = line.CreditAmount - line.DebitAmount;
        _dbContext.PayableTransactions.Add(new PayableTransaction
        {
            Id = Guid.NewGuid(),
            SupplierId = line.SupplierId.Value,
            SourceType = journal.SourceType,
            SourceId = journal.SourceId,
            JournalHeaderId = journal.Id,
            JournalLineId = line.Id,
            DocumentNo = string.IsNullOrWhiteSpace(journal.SourceNo) ? journal.JournalNo : journal.SourceNo,
            DocumentDate = journal.JournalDate.Date,
            DueDate = journal.JournalDate.Date.AddDays(DefaultDueDays),
            Description = line.Description ?? journal.Description,
            DebitAmount = line.DebitAmount,
            CreditAmount = line.CreditAmount,
            BalanceAmount = balance,
            CurrencyCode = string.IsNullOrWhiteSpace(line.CurrencyCode) ? "VND" : line.CurrencyCode,
            Status = ResolveDebtStatus(balance, line.CreditAmount, line.DebitAmount),
            CreatedAt = now,
            UpdatedAt = now
        });
        return true;
    }

    private async Task<Dictionary<string, string>> BuildRoleMapAsync(CancellationToken cancellationToken)
    {
        return await _dbContext.AccountRoleMappings
            .AsNoTracking()
            .Where(x => x.IsActive && (x.RoleCode == ReceivableRole || x.RoleCode == PayableRole))
            .OrderByDescending(x => x.IsDefault)
            .GroupBy(x => x.AccountCode)
            .Select(x => new { AccountCode = x.Key, RoleCode = x.First().RoleCode })
            .ToDictionaryAsync(x => x.AccountCode, x => x.RoleCode, StringComparer.OrdinalIgnoreCase, cancellationToken);
    }

    private static string ResolveDebtStatus(decimal balanceAmount, decimal increaseAmount, decimal decreaseAmount)
    {
        if (balanceAmount <= 0)
        {
            return "Paid";
        }

        return decreaseAmount > 0 && increaseAmount > 0 ? "PartiallyPaid" : "Open";
    }

    private static IEnumerable<AgingSnapshot> BuildSnapshots<T>(IEnumerable<T> rows, DateTime snapshotDate, string partnerType, DateTime now, Func<T, Guid> partnerSelector)
        where T : class
    {
        return rows
            .GroupBy(partnerSelector)
            .Select(group =>
            {
                var snapshot = new AgingSnapshot
                {
                    Id = Guid.NewGuid(),
                    SnapshotDate = snapshotDate,
                    PartnerType = partnerType,
                    PartnerId = group.Key,
                    CreatedAt = now
                };

                foreach (var row in group)
                {
                    var (dueDate, balance) = row switch
                    {
                        ReceivableTransaction receivable => (receivable.DueDate ?? receivable.DocumentDate, receivable.BalanceAmount),
                        PayableTransaction payable => (payable.DueDate ?? payable.DocumentDate, payable.BalanceAmount),
                        _ => (snapshotDate, 0m)
                    };

                    var days = (snapshotDate - dueDate.Date).Days;
                    if (days <= 0)
                    {
                        snapshot.CurrentAmount += balance;
                    }
                    else if (days <= 30)
                    {
                        snapshot.Overdue1To30 += balance;
                    }
                    else if (days <= 60)
                    {
                        snapshot.Overdue31To60 += balance;
                    }
                    else if (days <= 90)
                    {
                        snapshot.Overdue61To90 += balance;
                    }
                    else
                    {
                        snapshot.OverdueOver90 += balance;
                    }
                }

                snapshot.TotalAmount = snapshot.CurrentAmount + snapshot.Overdue1To30 + snapshot.Overdue31To60 + snapshot.Overdue61To90 + snapshot.OverdueOver90;
                return snapshot;
            });
    }

    private async Task LogAsync(string action, string detail, string username, CancellationToken cancellationToken)
    {
        if (_auditLogService is not null)
        {
            await _auditLogService.LogAsync(action, detail, username, cancellationToken);
        }
    }
}
