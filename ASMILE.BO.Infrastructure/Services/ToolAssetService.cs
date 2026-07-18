using System.Globalization;
using ASMILE.BO.Application.DTOs;
using ASMILE.BO.Application.Interfaces;
using ASMILE.BO.Core.Entities;
using ASMILE.BO.Core.Interfaces.Services;
using ASMILE.BO.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ASMILE.BO.Infrastructure.Services;

public sealed class ToolAssetService : IToolAssetService
{
    private const string StatusInStock = "InStock";
    private const string StatusInUse = "InUse";
    private const string StatusAllocating = "Allocating";
    private const string StatusFullyAllocated = "FullyAllocated";
    private const string StatusRecovered = "Recovered";
    private const string StatusDisposed = "Disposed";
    private const string StatusDraft = "Draft";
    private const string StatusPosted = "Posted";

    private readonly AppDbContext _dbContext;
    private readonly IJournalService _journalService;
    private readonly IAccountingPeriodService _accountingPeriodService;
    private readonly IPostingRuleService _postingRuleService;
    private readonly IAuditLogService? _auditLogService;

    public ToolAssetService(
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

    public async Task<IReadOnlyList<ToolAssetCategoryDto>> GetCategoriesAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.ToolAssetCategories.AsNoTracking()
            .OrderBy(x => x.CategoryCode)
            .Select(x => ToCategoryDto(x))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ToolAssetDto>> GetToolsAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.ToolAssets.AsNoTracking()
            .Include(x => x.Category)
            .OrderBy(x => x.ToolCode)
            .Select(x => ToToolDto(x))
            .ToListAsync(cancellationToken);
    }

    public async Task<ToolAssetDto> CreateToolAsync(CreateToolAssetRequest request, CancellationToken cancellationToken = default)
    {
        ValidateCreateToolRequest(request);
        var category = await _dbContext.ToolAssetCategories.FirstOrDefaultAsync(x => x.Id == request.CategoryId, cancellationToken)
            ?? throw new InvalidOperationException("Không tìm thấy nhóm công cụ dụng cụ.");
        if (!category.IsActive)
        {
            throw new InvalidOperationException("Nhóm công cụ dụng cụ đang tạm tắt.");
        }

        var code = request.ToolCode.Trim();
        var duplicated = await _dbContext.ToolAssets.AnyAsync(x => x.ToolCode == code, cancellationToken);
        if (duplicated)
        {
            throw new InvalidOperationException($"Mã công cụ dụng cụ đã tồn tại: {code}.");
        }

        var allocationMonths = request.AllocationMonths.GetValueOrDefault(category.DefaultAllocationMonths);
        if (allocationMonths < 0)
        {
            throw new InvalidOperationException("Số tháng phân bổ không được âm.");
        }

        var now = DateTime.UtcNow;
        var tool = new ToolAsset
        {
            Id = Guid.NewGuid(),
            ToolCode = code,
            ToolName = request.ToolName.Trim(),
            CategoryId = category.Id,
            PurchaseDate = request.PurchaseDate.Date,
            OriginalCost = request.OriginalCost,
            Quantity = request.Quantity,
            UnitCost = Math.Round(request.OriginalCost / request.Quantity, 2, MidpointRounding.AwayFromZero),
            AllocatedAmount = 0m,
            RemainingAmount = request.OriginalCost,
            AllocationMonths = allocationMonths,
            DepartmentId = request.DepartmentId,
            EmployeeId = request.EmployeeId,
            WarehouseId = request.WarehouseId,
            SupplierId = request.SupplierId,
            Status = StatusInStock,
            Notes = NormalizeOptional(request.Notes),
            CreatedAt = now,
            UpdatedAt = now
        };

        _dbContext.ToolAssets.Add(tool);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await LogAsync("ToolAsset.Create", $"Tạo công cụ dụng cụ {tool.ToolCode}.", request.CreatedBy, cancellationToken);
        tool.Category = category;
        return ToToolDto(tool);
    }

    public async Task<ToolIssueDto> IssueToolAsync(IssueToolRequest request, CancellationToken cancellationToken = default)
    {
        if (request.ToolAssetId == Guid.Empty)
        {
            throw new InvalidOperationException("Công cụ dụng cụ là bắt buộc.");
        }
        if (request.Quantity <= 0)
        {
            throw new InvalidOperationException("Số lượng xuất dùng phải lớn hơn 0.");
        }

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
        var tool = await _dbContext.ToolAssets.Include(x => x.Category)
            .FirstOrDefaultAsync(x => x.Id == request.ToolAssetId, cancellationToken)
            ?? throw new InvalidOperationException("Không tìm thấy công cụ dụng cụ.");
        if (tool.Status is StatusDisposed or StatusRecovered)
        {
            throw new InvalidOperationException("Công cụ dụng cụ đã thu hồi/thanh lý, không thể xuất dùng.");
        }
        if (request.Quantity > tool.Quantity)
        {
            throw new InvalidOperationException("Số lượng xuất dùng lớn hơn số lượng công cụ dụng cụ hiện có.");
        }

        var issueDate = request.IssueDate.Date;
        await _accountingPeriodService.ValidatePostingDateAsync(issueDate, cancellationToken);
        var user = NormalizeOptional(request.PostedBy) ?? "system";
        var amount = Math.Round(tool.UnitCost * request.Quantity, 2, MidpointRounding.AwayFromZero);
        if (request.Quantity == tool.Quantity)
        {
            amount = Math.Min(tool.RemainingAmount, amount);
        }

        var issue = new ToolIssue
        {
            Id = Guid.NewGuid(),
            IssueNo = await GenerateDocumentNoAsync("CCDC-XD", _dbContext.ToolIssues.Select(x => x.IssueNo), cancellationToken),
            IssueDate = issueDate,
            ToolAssetId = tool.Id,
            Quantity = request.Quantity,
            Amount = amount,
            DepartmentId = request.DepartmentId ?? tool.DepartmentId,
            EmployeeId = request.EmployeeId ?? tool.EmployeeId,
            Description = NormalizeOptional(request.Description),
            Status = StatusDraft,
            CreatedAt = DateTime.UtcNow
        };
        _dbContext.ToolIssues.Add(issue);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var toolAccount = await ResolveAccountAsync(tool.Category, tool.Category?.ToolAccountCode, "ToolAndSupply", cancellationToken);
        var debitAccount = tool.AllocationMonths > 1
            ? await ResolveAccountAsync(tool.Category, tool.Category?.PrepaidExpenseAccountCode, "PrepaidExpense", cancellationToken)
            : await ResolveAccountAsync(tool.Category, tool.Category?.ExpenseAccountCode, "Expense", cancellationToken);

        var journal = await CreateIssueJournalAsync(tool, issue, debitAccount, toolAccount, user, cancellationToken);
        issue.JournalHeaderId = journal.Id;
        issue.Status = StatusPosted;
        issue.PostedAt = DateTime.UtcNow;

        tool.StartAllocationDate ??= issueDate;
        tool.DepartmentId = issue.DepartmentId;
        tool.EmployeeId = issue.EmployeeId;
        tool.JournalHeaderId = journal.Id;
        if (tool.AllocationMonths > 1)
        {
            tool.Status = StatusAllocating;
            tool.RemainingAmount = amount;
            tool.AllocatedAmount = Math.Max(0m, tool.OriginalCost - amount);
            await BuildAllocationScheduleCoreAsync(tool, cancellationToken);
        }
        else
        {
            tool.AllocatedAmount = Math.Min(tool.OriginalCost, tool.AllocatedAmount + amount);
            tool.RemainingAmount = Math.Max(0m, tool.RemainingAmount - amount);
            tool.Status = tool.RemainingAmount <= 0 ? StatusFullyAllocated : StatusInUse;
        }

        tool.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        await LogAsync("ToolAsset.Issue", $"Xuất dùng CCDC {tool.ToolCode} theo phiếu {issue.IssueNo}.", user, cancellationToken);
        return ToIssueDto(issue);
    }

    public async Task<IReadOnlyList<ToolAllocationScheduleDto>> BuildAllocationScheduleAsync(Guid toolAssetId, CancellationToken cancellationToken = default)
    {
        var tool = await _dbContext.ToolAssets.Include(x => x.Category)
            .Include(x => x.AllocationSchedules)
            .FirstOrDefaultAsync(x => x.Id == toolAssetId, cancellationToken)
            ?? throw new InvalidOperationException("Không tìm thấy công cụ dụng cụ.");
        await BuildAllocationScheduleCoreAsync(tool, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return await _dbContext.ToolAllocationSchedules.AsNoTracking()
            .Where(x => x.ToolAssetId == toolAssetId)
            .OrderBy(x => x.AllocationPeriod)
            .Select(x => ToScheduleDto(x))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ToolAllocationScheduleDto>> GetAllocationSchedulesAsync(Guid? toolAssetId = null, int take = 300, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.ToolAllocationSchedules.AsNoTracking()
            .Include(x => x.ToolAsset)
            .AsQueryable();
        if (toolAssetId.HasValue)
        {
            query = query.Where(x => x.ToolAssetId == toolAssetId.Value);
        }

        return await query
            .OrderByDescending(x => x.AllocationDate)
            .ThenByDescending(x => x.AllocationPeriod)
            .Take(Math.Clamp(take, 1, 1000))
            .Select(x => ToScheduleDto(x))
            .ToListAsync(cancellationToken);
    }

    public async Task<ToolAllocationRunDto> RunAllocationAsync(AllocationRunRequest request, CancellationToken cancellationToken = default)
    {
        var periodCode = NormalizePeriodCode(request.AllocationPeriod);
        var allocationDate = request.AllocationDate?.Date ?? GetPeriodEndDate(periodCode);
        await _accountingPeriodService.ValidatePostingDateAsync(allocationDate, cancellationToken);
        if (request.ToolAssetId.HasValue)
        {
            await BuildAllocationScheduleAsync(request.ToolAssetId.Value, cancellationToken);
        }

        var run = new ToolAllocationRun
        {
            Id = Guid.NewGuid(),
            RunNo = await GenerateDocumentNoAsync("CCDC-PB", _dbContext.ToolAllocationRuns.Select(x => x.RunNo), cancellationToken),
            AllocationPeriod = periodCode,
            RunDate = allocationDate,
            Status = StatusDraft,
            CreatedAt = DateTime.UtcNow
        };
        _dbContext.ToolAllocationRuns.Add(run);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var query = _dbContext.ToolAllocationSchedules
            .Where(x => x.AllocationPeriod == periodCode && x.Status == StatusDraft);
        if (request.ToolAssetId.HasValue)
        {
            query = query.Where(x => x.ToolAssetId == request.ToolAssetId.Value);
        }

        var scheduleIds = await query.OrderBy(x => x.AllocationDate).Select(x => x.Id).ToListAsync(cancellationToken);
        decimal total = 0m;
        foreach (var scheduleId in scheduleIds)
        {
            var posted = await PostAllocationAsync(scheduleId, request.PostedBy, cancellationToken);
            total += posted.AllocationAmount;
        }

        run.TotalAmount = total;
        run.Status = StatusPosted;
        run.PostedAt = DateTime.UtcNow;
        run.Message = $"Đã phân bổ {scheduleIds.Count} dòng kỳ {periodCode}.";
        await _dbContext.SaveChangesAsync(cancellationToken);
        await LogAsync("ToolAsset.AllocationRun", run.Message, request.PostedBy, cancellationToken);
        return ToRunDto(run);
    }

    public async Task<ToolAllocationScheduleDto> PostAllocationAsync(Guid scheduleId, string? postedBy = null, CancellationToken cancellationToken = default)
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
        var schedule = await _dbContext.ToolAllocationSchedules
            .Include(x => x.ToolAsset)
            .ThenInclude(x => x!.Category)
            .FirstOrDefaultAsync(x => x.Id == scheduleId, cancellationToken)
            ?? throw new InvalidOperationException("Không tìm thấy dòng phân bổ CCDC.");
        if (schedule.Status != StatusDraft)
        {
            throw new InvalidOperationException("Chỉ được ghi sổ dòng phân bổ ở trạng thái nháp.");
        }

        var tool = schedule.ToolAsset ?? throw new InvalidOperationException("Không tìm thấy công cụ dụng cụ của dòng phân bổ.");
        await _accountingPeriodService.ValidatePostingDateAsync(schedule.AllocationDate, cancellationToken);
        var user = NormalizeOptional(postedBy) ?? "system";
        var expenseAccount = await ResolveAccountAsync(tool.Category, tool.Category?.ExpenseAccountCode, "Expense", cancellationToken);
        var prepaidAccount = await ResolveAccountAsync(tool.Category, tool.Category?.PrepaidExpenseAccountCode, "PrepaidExpense", cancellationToken);
        var journal = await CreateAllocationJournalAsync(tool, schedule, expenseAccount, prepaidAccount, user, cancellationToken);

        schedule.JournalHeaderId = journal.Id;
        schedule.Status = StatusPosted;
        schedule.PostedAt = DateTime.UtcNow;
        tool.AllocatedAmount = schedule.AccumulatedAfter;
        tool.RemainingAmount = schedule.RemainingAfter;
        tool.Status = tool.RemainingAmount <= 0 ? StatusFullyAllocated : StatusAllocating;
        tool.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        await LogAsync("ToolAsset.AllocationPost", $"Ghi sổ phân bổ CCDC {tool.ToolCode} kỳ {schedule.AllocationPeriod}.", user, cancellationToken);
        return ToScheduleDto(schedule);
    }

    public async Task<ToolAllocationScheduleDto> ReverseAllocationAsync(Guid scheduleId, string reason, string? reversedBy = null, CancellationToken cancellationToken = default)
    {
        var schedule = await _dbContext.ToolAllocationSchedules
            .Include(x => x.ToolAsset)
            .ThenInclude(x => x!.Category)
            .FirstOrDefaultAsync(x => x.Id == scheduleId, cancellationToken)
            ?? throw new InvalidOperationException("Không tìm thấy dòng phân bổ CCDC.");
        if (schedule.Status != StatusPosted)
        {
            throw new InvalidOperationException("Chỉ đảo được dòng phân bổ đã ghi sổ.");
        }

        var tool = schedule.ToolAsset ?? throw new InvalidOperationException("Không tìm thấy công cụ dụng cụ của dòng phân bổ.");
        var user = NormalizeOptional(reversedBy) ?? "system";
        if (schedule.JournalHeaderId.HasValue)
        {
            await _journalService.ReverseAsync(schedule.JournalHeaderId.Value, NormalizeOptional(reason) ?? "Reverse tool allocation", user, cancellationToken);
        }

        schedule.Status = "Reversed";
        tool.AllocatedAmount = Math.Max(0m, tool.AllocatedAmount - schedule.AllocationAmount);
        tool.RemainingAmount = Math.Min(tool.OriginalCost, tool.RemainingAmount + schedule.AllocationAmount);
        tool.Status = tool.RemainingAmount <= 0 ? StatusFullyAllocated : StatusAllocating;
        tool.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
        await LogAsync("ToolAsset.AllocationReverse", $"Đảo phân bổ CCDC {tool.ToolCode} kỳ {schedule.AllocationPeriod}.", user, cancellationToken);
        return ToScheduleDto(schedule);
    }

    public async Task<ToolSummaryDto> GetSummaryAsync(CancellationToken cancellationToken = default)
    {
        var tools = await _dbContext.ToolAssets.AsNoTracking().ToListAsync(cancellationToken);
        return new ToolSummaryDto
        {
            TotalToolCount = tools.Count,
            InUseCount = tools.Count(x => x.Status == StatusInUse),
            AllocatingCount = tools.Count(x => x.Status == StatusAllocating),
            FullyAllocatedCount = tools.Count(x => x.Status == StatusFullyAllocated),
            TotalOriginalCost = tools.Sum(x => x.OriginalCost),
            TotalAllocatedAmount = tools.Sum(x => x.AllocatedAmount),
            TotalRemainingAmount = tools.Sum(x => x.RemainingAmount)
        };
    }

    public async Task RecoverToolAsync(Guid toolAssetId, decimal quantity, string? description = null, CancellationToken cancellationToken = default)
    {
        if (quantity <= 0)
        {
            throw new InvalidOperationException("Số lượng thu hồi phải lớn hơn 0.");
        }

        var tool = await _dbContext.ToolAssets.FirstOrDefaultAsync(x => x.Id == toolAssetId, cancellationToken)
            ?? throw new InvalidOperationException("Không tìm thấy công cụ dụng cụ.");
        var recovery = new ToolRecovery
        {
            Id = Guid.NewGuid(),
            RecoveryNo = await GenerateDocumentNoAsync("CCDC-TH", _dbContext.ToolRecoveries.Select(x => x.RecoveryNo), cancellationToken),
            RecoveryDate = DateTime.Today,
            ToolAssetId = tool.Id,
            Quantity = quantity,
            Amount = Math.Round(tool.UnitCost * quantity, 2, MidpointRounding.AwayFromZero),
            Description = NormalizeOptional(description),
            Status = StatusPosted,
            CreatedAt = DateTime.UtcNow
        };
        tool.Status = StatusRecovered;
        tool.UpdatedAt = DateTime.UtcNow;
        _dbContext.ToolRecoveries.Add(recovery);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await LogAsync("ToolAsset.Recover", $"Thu hồi CCDC {tool.ToolCode}.", null, cancellationToken);
    }

    public async Task DisposeToolAsync(Guid toolAssetId, decimal quantity, string? reason = null, CancellationToken cancellationToken = default)
    {
        if (quantity <= 0)
        {
            throw new InvalidOperationException("Số lượng thanh lý phải lớn hơn 0.");
        }

        var tool = await _dbContext.ToolAssets.FirstOrDefaultAsync(x => x.Id == toolAssetId, cancellationToken)
            ?? throw new InvalidOperationException("Không tìm thấy công cụ dụng cụ.");
        var disposal = new ToolDisposal
        {
            Id = Guid.NewGuid(),
            DisposalNo = await GenerateDocumentNoAsync("CCDC-TL", _dbContext.ToolDisposals.Select(x => x.DisposalNo), cancellationToken),
            DisposalDate = DateTime.Today,
            ToolAssetId = tool.Id,
            Quantity = quantity,
            RemainingAmount = tool.RemainingAmount,
            Reason = NormalizeOptional(reason),
            Status = StatusDraft,
            CreatedAt = DateTime.UtcNow
        };
        tool.Status = StatusDisposed;
        tool.RemainingAmount = 0m;
        tool.UpdatedAt = DateTime.UtcNow;
        _dbContext.ToolDisposals.Add(disposal);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await LogAsync("ToolAsset.Dispose", $"Ghi nhận thanh lý CCDC {tool.ToolCode}; journal thanh lý sẽ hoàn thiện ở bước sau.", null, cancellationToken);
    }

    private async Task BuildAllocationScheduleCoreAsync(ToolAsset tool, CancellationToken cancellationToken)
    {
        if (tool.AllocationMonths <= 1 || tool.RemainingAmount <= 0)
        {
            return;
        }

        var existingPeriods = await _dbContext.ToolAllocationSchedules
            .Where(x => x.ToolAssetId == tool.Id)
            .Select(x => x.AllocationPeriod)
            .ToListAsync(cancellationToken);
        var existingSet = existingPeriods.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var startDate = (tool.StartAllocationDate ?? DateTime.Today).Date;
        var monthlyAmount = Math.Round(tool.RemainingAmount / tool.AllocationMonths, 2, MidpointRounding.AwayFromZero);
        var accumulated = tool.AllocatedAmount;
        var remaining = tool.RemainingAmount;
        for (var i = 0; i < tool.AllocationMonths && remaining > 0; i++)
        {
            var date = startDate.AddMonths(i);
            var period = date.ToString("yyyy-MM");
            var amount = i == tool.AllocationMonths - 1 ? remaining : Math.Min(monthlyAmount, remaining);
            accumulated += amount;
            remaining = Math.Max(0m, remaining - amount);
            if (existingSet.Contains(period))
            {
                continue;
            }

            _dbContext.ToolAllocationSchedules.Add(new ToolAllocationSchedule
            {
                Id = Guid.NewGuid(),
                ToolAssetId = tool.Id,
                AllocationPeriod = period,
                AllocationDate = new DateTime(date.Year, date.Month, DateTime.DaysInMonth(date.Year, date.Month)),
                AllocationAmount = amount,
                AccumulatedAfter = accumulated,
                RemainingAfter = remaining,
                Status = StatusDraft,
                CreatedAt = DateTime.UtcNow
            });
        }
    }

    private async Task<JournalHeader> CreateIssueJournalAsync(ToolAsset tool, ToolIssue issue, string debitAccountCode, string toolAccountCode, string user, CancellationToken cancellationToken)
    {
        var accountNames = await GetAccountNamesAsync([debitAccountCode, toolAccountCode], cancellationToken);
        var description = $"Xuất dùng CCDC {tool.ToolCode} theo phiếu {issue.IssueNo}";
        var journal = new JournalHeader
        {
            Id = Guid.NewGuid(),
            JournalDate = issue.IssueDate,
            PostingDate = issue.IssueDate,
            SourceType = "ToolIssue",
            SourceId = issue.Id,
            SourceNo = issue.IssueNo,
            Description = description,
            PeriodCode = issue.IssueDate.ToString("yyyy-MM"),
            CreatedBy = user,
            CreatedAt = DateTime.UtcNow,
            Lines = new List<JournalLine>
            {
                CreateJournalLine(1, debitAccountCode, ResolveAccountName(accountNames, debitAccountCode), issue.Amount, 0m, tool, issue, description),
                CreateJournalLine(2, toolAccountCode, ResolveAccountName(accountNames, toolAccountCode), 0m, issue.Amount, tool, issue, description)
            }
        };

        var draft = await _journalService.CreateDraftAsync(journal, cancellationToken);
        return await _journalService.PostDraftAsync(draft.Id, user, cancellationToken);
    }

    private async Task<JournalHeader> CreateAllocationJournalAsync(ToolAsset tool, ToolAllocationSchedule schedule, string expenseAccountCode, string prepaidAccountCode, string user, CancellationToken cancellationToken)
    {
        var accountNames = await GetAccountNamesAsync([expenseAccountCode, prepaidAccountCode], cancellationToken);
        var description = $"Phân bổ CCDC {tool.ToolCode} kỳ {schedule.AllocationPeriod}";
        var journal = new JournalHeader
        {
            Id = Guid.NewGuid(),
            JournalDate = schedule.AllocationDate,
            PostingDate = schedule.AllocationDate,
            SourceType = "ToolAllocation",
            SourceId = schedule.Id,
            SourceNo = $"{tool.ToolCode}-{schedule.AllocationPeriod}",
            Description = description,
            PeriodCode = schedule.AllocationDate.ToString("yyyy-MM"),
            CreatedBy = user,
            CreatedAt = DateTime.UtcNow,
            Lines = new List<JournalLine>
            {
                CreateJournalLine(1, expenseAccountCode, ResolveAccountName(accountNames, expenseAccountCode), schedule.AllocationAmount, 0m, tool, null, description),
                CreateJournalLine(2, prepaidAccountCode, ResolveAccountName(accountNames, prepaidAccountCode), 0m, schedule.AllocationAmount, tool, null, description)
            }
        };

        var draft = await _journalService.CreateDraftAsync(journal, cancellationToken);
        return await _journalService.PostDraftAsync(draft.Id, user, cancellationToken);
    }

    private static JournalLine CreateJournalLine(int lineNo, string accountCode, string accountName, decimal debit, decimal credit, ToolAsset tool, ToolIssue? issue, string description)
        => new()
        {
            Id = Guid.NewGuid(),
            LineNo = lineNo,
            AccountCode = accountCode,
            AccountName = accountName,
            DebitAmount = debit,
            CreditAmount = credit,
            CurrencyCode = "VND",
            ExchangeRate = 1m,
            WarehouseId = tool.WarehouseId,
            ItemId = tool.Id,
            DepartmentId = issue?.DepartmentId ?? tool.DepartmentId,
            EmployeeId = issue?.EmployeeId ?? tool.EmployeeId,
            SupplierId = tool.SupplierId,
            ObjectType = "ToolAsset",
            ObjectId = tool.Id,
            Description = description
        };

    private async Task<string> ResolveAccountAsync(ToolAssetCategory? category, string? categoryAccountCode, string roleCode, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(categoryAccountCode))
        {
            return categoryAccountCode.Trim();
        }

        var standardCode = await GetDefaultAccountingStandardCodeAsync(cancellationToken);
        var account = await _postingRuleService.ResolveAccountByRoleAsync(roleCode, standardCode, cancellationToken);
        if (string.IsNullOrWhiteSpace(account))
        {
            var categoryHint = category is null ? string.Empty : $" cho nhóm {category.CategoryCode}";
            throw new InvalidOperationException($"Chưa cấu hình AccountRoleMapping cho {roleCode}{categoryHint} theo chuẩn {standardCode}.");
        }

        return account;
    }

    private async Task<Dictionary<string, string>> GetAccountNamesAsync(IEnumerable<string> accountCodes, CancellationToken cancellationToken)
    {
        var distinctCodes = accountCodes.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()).Distinct().ToList();
        return await _dbContext.LedgerAccounts.AsNoTracking()
            .Where(x => distinctCodes.Contains(x.AccountCode))
            .ToDictionaryAsync(x => x.AccountCode, x => x.AccountName, cancellationToken);
    }

    private static string ResolveAccountName(IReadOnlyDictionary<string, string> accountNames, string accountCode)
        => accountNames.TryGetValue(accountCode, out var name) ? name : accountCode;

    private async Task<string> GetDefaultAccountingStandardCodeAsync(CancellationToken cancellationToken)
    {
        return await _dbContext.AccountingStandards.AsNoTracking()
            .Where(x => x.IsDefault && x.IsActive)
            .OrderBy(x => x.Code)
            .Select(x => x.Code)
            .FirstOrDefaultAsync(cancellationToken) ?? "TT99";
    }

    private async Task<string> GenerateDocumentNoAsync(string prefix, IQueryable<string> existingNumbers, CancellationToken cancellationToken)
    {
        var seed = DateTime.Now.ToString("yyyyMMdd-HHmmss");
        for (var i = 1; i <= 999; i++)
        {
            var candidate = $"{prefix}-{seed}-{i:000}";
            if (!await existingNumbers.AnyAsync(x => x == candidate, cancellationToken))
            {
                return candidate;
            }
        }

        throw new InvalidOperationException("Không thể sinh số chứng từ CCDC duy nhất.");
    }

    private static void ValidateCreateToolRequest(CreateToolAssetRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.ToolCode))
        {
            throw new InvalidOperationException("Mã công cụ dụng cụ là bắt buộc.");
        }
        if (string.IsNullOrWhiteSpace(request.ToolName))
        {
            throw new InvalidOperationException("Tên công cụ dụng cụ là bắt buộc.");
        }
        if (request.CategoryId == Guid.Empty)
        {
            throw new InvalidOperationException("Nhóm công cụ dụng cụ là bắt buộc.");
        }
        if (request.OriginalCost <= 0)
        {
            throw new InvalidOperationException("Nguyên giá công cụ dụng cụ phải lớn hơn 0.");
        }
        if (request.Quantity <= 0)
        {
            throw new InvalidOperationException("Số lượng công cụ dụng cụ phải lớn hơn 0.");
        }
    }

    private static string NormalizePeriodCode(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return DateTime.Today.ToString("yyyy-MM");
        }

        if (DateTime.TryParseExact(value.Trim(), ["yyyy-MM", "MM/yyyy"], CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
        {
            return parsed.ToString("yyyy-MM");
        }

        throw new InvalidOperationException("Kỳ phân bổ phải có định dạng yyyy-MM hoặc MM/yyyy.");
    }

    private static DateTime GetPeriodEndDate(string periodCode)
    {
        var parsed = DateTime.ParseExact(periodCode, "yyyy-MM", CultureInfo.InvariantCulture);
        return new DateTime(parsed.Year, parsed.Month, DateTime.DaysInMonth(parsed.Year, parsed.Month));
    }

    private static string? NormalizeOptional(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static ToolAssetCategoryDto ToCategoryDto(ToolAssetCategory category)
        => new()
        {
            Id = category.Id,
            CategoryCode = category.CategoryCode,
            CategoryName = category.CategoryName,
            ToolAccountCode = category.ToolAccountCode,
            PrepaidExpenseAccountCode = category.PrepaidExpenseAccountCode,
            ExpenseAccountCode = category.ExpenseAccountCode,
            DefaultAllocationMonths = category.DefaultAllocationMonths,
            IsActive = category.IsActive
        };

    private static ToolAssetDto ToToolDto(ToolAsset tool)
        => new()
        {
            Id = tool.Id,
            ToolCode = tool.ToolCode,
            ToolName = tool.ToolName,
            CategoryId = tool.CategoryId,
            CategoryCode = tool.Category?.CategoryCode,
            CategoryName = tool.Category?.CategoryName,
            PurchaseDate = tool.PurchaseDate,
            OriginalCost = tool.OriginalCost,
            Quantity = tool.Quantity,
            UnitCost = tool.UnitCost,
            AllocatedAmount = tool.AllocatedAmount,
            RemainingAmount = tool.RemainingAmount,
            AllocationMonths = tool.AllocationMonths,
            StartAllocationDate = tool.StartAllocationDate,
            JournalHeaderId = tool.JournalHeaderId,
            Status = tool.Status,
            Notes = tool.Notes
        };

    private static ToolIssueDto ToIssueDto(ToolIssue issue)
        => new()
        {
            Id = issue.Id,
            IssueNo = issue.IssueNo,
            IssueDate = issue.IssueDate,
            ToolAssetId = issue.ToolAssetId,
            ToolCode = issue.ToolAsset?.ToolCode,
            ToolName = issue.ToolAsset?.ToolName,
            Quantity = issue.Quantity,
            Amount = issue.Amount,
            JournalHeaderId = issue.JournalHeaderId,
            Status = issue.Status
        };

    private static ToolAllocationScheduleDto ToScheduleDto(ToolAllocationSchedule schedule)
        => new()
        {
            Id = schedule.Id,
            ToolAssetId = schedule.ToolAssetId,
            ToolCode = schedule.ToolAsset?.ToolCode,
            ToolName = schedule.ToolAsset?.ToolName,
            AllocationPeriod = schedule.AllocationPeriod,
            AllocationDate = schedule.AllocationDate,
            AllocationAmount = schedule.AllocationAmount,
            AccumulatedAfter = schedule.AccumulatedAfter,
            RemainingAfter = schedule.RemainingAfter,
            JournalHeaderId = schedule.JournalHeaderId,
            Status = schedule.Status
        };

    private static ToolAllocationRunDto ToRunDto(ToolAllocationRun run)
        => new()
        {
            Id = run.Id,
            RunNo = run.RunNo,
            AllocationPeriod = run.AllocationPeriod,
            RunDate = run.RunDate,
            TotalAmount = run.TotalAmount,
            Status = run.Status,
            Message = run.Message
        };

    private async Task LogAsync(string action, string detail, string? username, CancellationToken cancellationToken)
    {
        if (_auditLogService is not null)
        {
            await _auditLogService.LogAsync(action, detail, NormalizeOptional(username) ?? "system", cancellationToken);
        }
    }
}
