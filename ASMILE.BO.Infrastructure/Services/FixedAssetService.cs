using System.Globalization;
using ASMILE.BO.Application.DTOs;
using ASMILE.BO.Application.Interfaces;
using ASMILE.BO.Core.Entities;
using ASMILE.BO.Core.Interfaces.Services;
using ASMILE.BO.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ASMILE.BO.Infrastructure.Services;

public sealed class FixedAssetService : IFixedAssetService
{
    private const string StatusDraft = "Draft";
    private const string StatusActive = "Active";
    private const string StatusDisposed = "Disposed";
    private const string StatusFullyDepreciated = "FullyDepreciated";
    private const string DepreciationStatusDraft = "Draft";
    private const string DepreciationStatusPosted = "Posted";
    private const string StraightLine = "StraightLine";

    private readonly AppDbContext _dbContext;
    private readonly IJournalService _journalService;
    private readonly IAccountingPeriodService _accountingPeriodService;
    private readonly IPostingRuleService _postingRuleService;
    private readonly IAuditLogService? _auditLogService;

    public FixedAssetService(
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

    public async Task<IReadOnlyList<FixedAssetCategoryDto>> GetCategoriesAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.FixedAssetCategories.AsNoTracking()
            .OrderBy(x => x.CategoryCode)
            .Select(x => ToCategoryDto(x))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<FixedAssetDto>> GetAssetsAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.FixedAssets.AsNoTracking()
            .Include(x => x.Category)
            .OrderBy(x => x.AssetCode)
            .Select(x => ToAssetDto(x))
            .ToListAsync(cancellationToken);
    }

    public async Task<FixedAssetDto> CreateAssetAsync(CreateFixedAssetRequest request, CancellationToken cancellationToken = default)
    {
        ValidateCreateAssetRequest(request);
        var category = await _dbContext.FixedAssetCategories.FirstOrDefaultAsync(x => x.Id == request.CategoryId, cancellationToken)
            ?? throw new InvalidOperationException("Không tìm thấy nhóm tài sản cố định.");
        if (!category.IsActive)
        {
            throw new InvalidOperationException("Nhóm tài sản cố định đang tạm tắt.");
        }

        var code = request.AssetCode.Trim();
        var duplicated = await _dbContext.FixedAssets.AnyAsync(x => x.AssetCode == code, cancellationToken);
        if (duplicated)
        {
            throw new InvalidOperationException($"Mã tài sản cố định đã tồn tại: {code}.");
        }

        var usefulLife = request.UsefulLifeMonths.GetValueOrDefault(category.UsefulLifeMonthsDefault);
        if (usefulLife <= 0)
        {
            throw new InvalidOperationException("Thời gian khấu hao phải lớn hơn 0 tháng.");
        }

        var now = DateTime.UtcNow;
        var asset = new FixedAsset
        {
            Id = Guid.NewGuid(),
            AssetCode = code,
            AssetName = request.AssetName.Trim(),
            CategoryId = category.Id,
            SerialNumber = NormalizeOptional(request.SerialNumber),
            PurchaseDate = request.PurchaseDate.Date,
            StartDepreciationDate = request.StartDepreciationDate.Date,
            OriginalCost = request.OriginalCost,
            SalvageValue = request.SalvageValue,
            UsefulLifeMonths = usefulLife,
            DepreciationMethod = NormalizeOptional(request.DepreciationMethod) ?? category.DepreciationMethodDefault,
            AccumulatedDepreciation = 0m,
            RemainingValue = request.OriginalCost - request.SalvageValue,
            Quantity = request.Quantity <= 0 ? 1 : request.Quantity,
            DepartmentId = request.DepartmentId,
            EmployeeId = request.EmployeeId,
            WarehouseId = request.WarehouseId,
            SupplierId = request.SupplierId,
            Status = StatusDraft,
            Notes = NormalizeOptional(request.Notes),
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        };

        _dbContext.FixedAssets.Add(asset);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await LogAsync("FixedAsset.Create", $"Tạo tài sản cố định {asset.AssetCode}.", request.CreatedBy, cancellationToken);

        if (request.ActivateImmediately)
        {
            return await ActivateAssetAsync(asset.Id, request.CounterAccountCode, request.CreatedBy, cancellationToken);
        }

        asset.Category = category;
        return ToAssetDto(asset);
    }

    public async Task<FixedAssetDto> ActivateAssetAsync(Guid assetId, string? counterAccountCode = null, string? activatedBy = null, CancellationToken cancellationToken = default)
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
        var asset = await _dbContext.FixedAssets.Include(x => x.Category)
            .FirstOrDefaultAsync(x => x.Id == assetId, cancellationToken)
            ?? throw new InvalidOperationException("Không tìm thấy tài sản cố định.");
        if (asset.Status is StatusDisposed)
        {
            throw new InvalidOperationException("Tài sản cố định đã thanh lý/ghi giảm, không thể kích hoạt.");
        }

        await _accountingPeriodService.ValidatePostingDateAsync(asset.PurchaseDate, cancellationToken);
        var user = NormalizeOptional(activatedBy) ?? "system";
        if (!asset.JournalHeaderId.HasValue && !string.IsNullOrWhiteSpace(counterAccountCode))
        {
            var assetAccount = await ResolveAccountAsync(asset.Category, asset.Category?.AssetAccountCode, "FixedAsset", cancellationToken);
            var journal = await CreateActivationJournalAsync(asset, assetAccount, counterAccountCode.Trim(), user, cancellationToken);
            asset.JournalHeaderId = journal.Id;
        }

        asset.Status = StatusActive;
        asset.IsActive = true;
        asset.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        await LogAsync("FixedAsset.Activate", $"Kích hoạt tài sản cố định {asset.AssetCode}.", user, cancellationToken);
        return ToAssetDto(asset);
    }

    public async Task<FixedAssetDto> ReverseAssetAsync(Guid assetId, string reason, string? reversedBy = null, CancellationToken cancellationToken = default)
    {
        var asset = await _dbContext.FixedAssets.Include(x => x.Category)
            .FirstOrDefaultAsync(x => x.Id == assetId, cancellationToken)
            ?? throw new InvalidOperationException("Không tìm thấy tài sản cố định.");

        var hasPostedDepreciation = await _dbContext.FixedAssetDepreciations
            .AnyAsync(x => x.FixedAssetId == asset.Id && x.Status == DepreciationStatusPosted, cancellationToken);
        if (hasPostedDepreciation)
        {
            throw new InvalidOperationException("TSCĐ đã có khấu hao ghi sổ, cần đảo khấu hao trước khi đảo ghi tăng.");
        }

        var user = NormalizeOptional(reversedBy) ?? "system";
        if (asset.JournalHeaderId.HasValue)
        {
            await _journalService.ReverseAsync(asset.JournalHeaderId.Value, NormalizeOptional(reason) ?? "Reverse fixed asset", user, cancellationToken);
            asset.JournalHeaderId = null;
        }

        asset.Status = StatusDraft;
        asset.IsActive = true;
        asset.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
        await LogAsync("FixedAsset.Reverse", $"Đảo ghi tăng TSCĐ {asset.AssetCode}.", user, cancellationToken);
        return ToAssetDto(asset);
    }

    public async Task<FixedAssetAdjustmentDto> AdjustAssetAsync(AdjustFixedAssetRequest request, CancellationToken cancellationToken = default)
    {
        if (request.FixedAssetId == Guid.Empty)
        {
            throw new InvalidOperationException("TSCĐ là bắt buộc.");
        }
        if (request.Amount <= 0)
        {
            throw new InvalidOperationException("Giá trị điều chỉnh phải lớn hơn 0.");
        }

        var adjustmentType = NormalizeOptional(request.AdjustmentType) ?? "Increase";
        if (!string.Equals(adjustmentType, "Increase", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(adjustmentType, "Decrease", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Loại điều chỉnh chỉ hỗ trợ Increase hoặc Decrease.");
        }

        await _accountingPeriodService.ValidatePostingDateAsync(request.AdjustmentDate.Date, cancellationToken);
        var asset = await _dbContext.FixedAssets.Include(x => x.Category)
            .FirstOrDefaultAsync(x => x.Id == request.FixedAssetId, cancellationToken)
            ?? throw new InvalidOperationException("Không tìm thấy tài sản cố định.");
        if (asset.Status is StatusDraft or StatusDisposed)
        {
            throw new InvalidOperationException("Chỉ điều chỉnh TSCĐ đang hoạt động.");
        }

        var normalizedType = string.Equals(adjustmentType, "Decrease", StringComparison.OrdinalIgnoreCase) ? "Decrease" : "Increase";
        if (normalizedType == "Decrease" && request.Amount > asset.RemainingValue)
        {
            throw new InvalidOperationException("Giá trị ghi giảm không được lớn hơn giá trị còn lại.");
        }

        var adjustment = new FixedAssetAdjustment
        {
            Id = Guid.NewGuid(),
            FixedAssetId = asset.Id,
            AdjustmentDate = request.AdjustmentDate.Date,
            AdjustmentType = normalizedType,
            Amount = request.Amount,
            Description = NormalizeOptional(request.Description),
            CreatedAt = DateTime.UtcNow
        };

        if (normalizedType == "Increase")
        {
            asset.OriginalCost += request.Amount;
            asset.RemainingValue += request.Amount;
        }
        else
        {
            asset.OriginalCost = Math.Max(asset.SalvageValue + asset.AccumulatedDepreciation, asset.OriginalCost - request.Amount);
            asset.RemainingValue = Math.Max(0m, asset.RemainingValue - request.Amount);
        }

        asset.UpdatedAt = DateTime.UtcNow;
        _dbContext.FixedAssetAdjustments.Add(adjustment);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await LogAsync("FixedAsset.Adjust", $"{normalizedType} TSCĐ {asset.AssetCode}: {request.Amount:N0}.", request.AdjustedBy, cancellationToken);
        adjustment.FixedAsset = asset;
        return ToAdjustmentDto(adjustment);
    }

    public async Task<IReadOnlyList<FixedAssetDepreciationDto>> RunDepreciationAsync(DepreciationRunRequest request, CancellationToken cancellationToken = default)
    {
        var periodCode = NormalizePeriodCode(request.PeriodCode);
        var depreciationDate = request.DepreciationDate?.Date ?? GetPeriodEndDate(periodCode);
        await _accountingPeriodService.ValidatePostingDateAsync(depreciationDate, cancellationToken);

        var query = _dbContext.FixedAssets.Include(x => x.Category)
            .Where(x => x.Status == StatusActive && x.IsActive && x.DepreciationMethod == StraightLine);
        if (request.FixedAssetId.HasValue)
        {
            query = query.Where(x => x.Id == request.FixedAssetId.Value);
        }

        var assets = await query.OrderBy(x => x.AssetCode).ToListAsync(cancellationToken);
        var existingAssetIds = await _dbContext.FixedAssetDepreciations
            .Where(x => x.DepreciationPeriod == periodCode)
            .Select(x => x.FixedAssetId)
            .ToListAsync(cancellationToken);
        var existingSet = existingAssetIds.ToHashSet();
        var created = new List<FixedAssetDepreciation>();
        foreach (var asset in assets)
        {
            if (existingSet.Contains(asset.Id) || depreciationDate < asset.StartDepreciationDate.Date)
            {
                continue;
            }

            var amount = CalculateStraightLineDepreciation(asset);
            if (amount <= 0)
            {
                continue;
            }

            var accumulatedAfter = asset.AccumulatedDepreciation + amount;
            var remainingAfter = Math.Max(0m, asset.OriginalCost - asset.SalvageValue - accumulatedAfter);
            var depreciation = new FixedAssetDepreciation
            {
                Id = Guid.NewGuid(),
                FixedAssetId = asset.Id,
                DepreciationPeriod = periodCode,
                DepreciationDate = depreciationDate,
                DepreciationAmount = amount,
                AccumulatedAfter = accumulatedAfter,
                RemainingValueAfter = remainingAfter,
                Status = DepreciationStatusDraft,
                CreatedAt = DateTime.UtcNow
            };
            _dbContext.FixedAssetDepreciations.Add(depreciation);
            created.Add(depreciation);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        if (request.PostImmediately)
        {
            var posted = new List<FixedAssetDepreciationDto>(created.Count);
            foreach (var depreciation in created)
            {
                posted.Add(await PostDepreciationAsync(depreciation.Id, request.CreatedBy, cancellationToken));
            }

            return posted;
        }

        await LogAsync("FixedAsset.DepreciationRun", $"Tạo {created.Count} dòng khấu hao kỳ {periodCode}.", request.CreatedBy, cancellationToken);
        return created.Select(x => ToDepreciationDto(x)).ToList();
    }

    public async Task<FixedAssetDepreciationDto> PostDepreciationAsync(Guid depreciationId, string? postedBy = null, CancellationToken cancellationToken = default)
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
        var depreciation = await _dbContext.FixedAssetDepreciations
            .Include(x => x.FixedAsset)
            .ThenInclude(x => x!.Category)
            .FirstOrDefaultAsync(x => x.Id == depreciationId, cancellationToken)
            ?? throw new InvalidOperationException("Không tìm thấy dòng khấu hao tài sản cố định.");
        if (depreciation.Status != DepreciationStatusDraft)
        {
            throw new InvalidOperationException("Chỉ được ghi sổ dòng khấu hao ở trạng thái nháp.");
        }

        var asset = depreciation.FixedAsset ?? throw new InvalidOperationException("Không tìm thấy tài sản cố định của dòng khấu hao.");
        await _accountingPeriodService.ValidatePostingDateAsync(depreciation.DepreciationDate, cancellationToken);
        var user = NormalizeOptional(postedBy) ?? "system";
        var expenseAccount = await ResolveAccountAsync(asset.Category, asset.Category?.ExpenseAccountCode, "Expense", cancellationToken);
        var depreciationAccount = await ResolveAccountAsync(asset.Category, asset.Category?.DepreciationAccountCode, "Depreciation", cancellationToken);
        var journal = await CreateDepreciationJournalAsync(asset, depreciation, expenseAccount, depreciationAccount, user, cancellationToken);

        asset.AccumulatedDepreciation = depreciation.AccumulatedAfter;
        asset.RemainingValue = depreciation.RemainingValueAfter;
        asset.UpdatedAt = DateTime.UtcNow;
        if (asset.RemainingValue <= 0)
        {
            asset.Status = StatusFullyDepreciated;
        }

        depreciation.JournalHeaderId = journal.Id;
        depreciation.Status = DepreciationStatusPosted;
        depreciation.PostedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        await LogAsync("FixedAsset.DepreciationPost", $"Ghi sổ khấu hao TSCĐ {asset.AssetCode} kỳ {depreciation.DepreciationPeriod}.", user, cancellationToken);
        return ToDepreciationDto(depreciation);
    }

    public async Task<FixedAssetDepreciationDto> ReverseDepreciationAsync(Guid depreciationId, string reason, string? reversedBy = null, CancellationToken cancellationToken = default)
    {
        var depreciation = await _dbContext.FixedAssetDepreciations
            .Include(x => x.FixedAsset)
            .ThenInclude(x => x!.Category)
            .FirstOrDefaultAsync(x => x.Id == depreciationId, cancellationToken)
            ?? throw new InvalidOperationException("Không tìm thấy dòng khấu hao tài sản cố định.");
        if (depreciation.Status != DepreciationStatusPosted)
        {
            throw new InvalidOperationException("Chỉ đảo được dòng khấu hao đã ghi sổ.");
        }

        var asset = depreciation.FixedAsset ?? throw new InvalidOperationException("Không tìm thấy tài sản cố định của dòng khấu hao.");
        var user = NormalizeOptional(reversedBy) ?? "system";
        if (depreciation.JournalHeaderId.HasValue)
        {
            await _journalService.ReverseAsync(depreciation.JournalHeaderId.Value, NormalizeOptional(reason) ?? "Reverse depreciation", user, cancellationToken);
        }

        asset.AccumulatedDepreciation = Math.Max(0m, asset.AccumulatedDepreciation - depreciation.DepreciationAmount);
        asset.RemainingValue = Math.Max(0m, asset.OriginalCost - asset.SalvageValue - asset.AccumulatedDepreciation);
        if (asset.Status == StatusFullyDepreciated && asset.RemainingValue > 0)
        {
            asset.Status = StatusActive;
        }

        asset.UpdatedAt = DateTime.UtcNow;
        depreciation.Status = "Reversed";
        await _dbContext.SaveChangesAsync(cancellationToken);
        await LogAsync("FixedAsset.DepreciationReverse", $"Đảo khấu hao TSCĐ {asset.AssetCode} kỳ {depreciation.DepreciationPeriod}.", user, cancellationToken);
        return ToDepreciationDto(depreciation);
    }

    public async Task<FixedAssetDto> DisposeAssetAsync(DisposeFixedAssetRequest request, CancellationToken cancellationToken = default)
    {
        if (request.DisposalAmount < 0)
        {
            throw new InvalidOperationException("Giá trị thanh lý/ghi giảm không được âm.");
        }

        await _accountingPeriodService.ValidatePostingDateAsync(request.DisposalDate.Date, cancellationToken);
        var asset = await _dbContext.FixedAssets.Include(x => x.Category)
            .FirstOrDefaultAsync(x => x.Id == request.FixedAssetId, cancellationToken)
            ?? throw new InvalidOperationException("Không tìm thấy tài sản cố định.");
        if (asset.Status == StatusDisposed)
        {
            throw new InvalidOperationException("Tài sản cố định đã được thanh lý/ghi giảm.");
        }

        var disposal = new FixedAssetDisposal
        {
            Id = Guid.NewGuid(),
            FixedAssetId = asset.Id,
            DisposalDate = request.DisposalDate.Date,
            DisposalType = NormalizeOptional(request.DisposalType) ?? "Liquidation",
            DisposalAmount = request.DisposalAmount,
            GainLossAmount = request.DisposalAmount - asset.RemainingValue,
            Notes = NormalizeOptional(request.Notes),
            CreatedAt = DateTime.UtcNow
        };

        asset.Status = StatusDisposed;
        asset.IsActive = false;
        asset.UpdatedAt = DateTime.UtcNow;
        _dbContext.FixedAssetDisposals.Add(disposal);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await LogAsync("FixedAsset.Dispose", $"Ghi giảm tài sản cố định {asset.AssetCode}.", request.DisposedBy, cancellationToken);
        return ToAssetDto(asset);
    }

    public async Task<FixedAssetSummaryDto> GetSummaryAsync(CancellationToken cancellationToken = default)
    {
        var assets = await _dbContext.FixedAssets.AsNoTracking().ToListAsync(cancellationToken);
        return new FixedAssetSummaryDto
        {
            TotalOriginalCost = assets.Where(x => x.Status != StatusDisposed).Sum(x => x.OriginalCost),
            TotalAccumulatedDepreciation = assets.Where(x => x.Status != StatusDisposed).Sum(x => x.AccumulatedDepreciation),
            TotalRemainingValue = assets.Where(x => x.Status != StatusDisposed).Sum(x => x.RemainingValue),
            ActiveAssetCount = assets.Count(x => x.Status == StatusActive || x.Status == StatusFullyDepreciated),
            DraftAssetCount = assets.Count(x => x.Status == StatusDraft),
            DisposedAssetCount = assets.Count(x => x.Status == StatusDisposed)
        };
    }

    public async Task<IReadOnlyList<FixedAssetScheduleDto>> GetDepreciationScheduleAsync(Guid assetId, CancellationToken cancellationToken = default)
    {
        var asset = await _dbContext.FixedAssets.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == assetId, cancellationToken)
            ?? throw new InvalidOperationException("Không tìm thấy tài sản cố định.");
        var persisted = await _dbContext.FixedAssetDepreciations.AsNoTracking()
            .Where(x => x.FixedAssetId == assetId)
            .OrderBy(x => x.DepreciationPeriod)
            .Select(x => new FixedAssetScheduleDto
            {
                Period = x.DepreciationPeriod,
                DepreciationDate = x.DepreciationDate,
                DepreciationAmount = x.DepreciationAmount,
                AccumulatedAfter = x.AccumulatedAfter,
                RemainingValueAfter = x.RemainingValueAfter,
                Status = x.Status
            })
            .ToListAsync(cancellationToken);
        if (persisted.Count > 0)
        {
            return persisted;
        }

        var monthly = CalculateStraightLineDepreciation(asset);
        var result = new List<FixedAssetScheduleDto>();
        var accumulated = 0m;
        var remaining = asset.OriginalCost - asset.SalvageValue;
        for (var i = 0; i < asset.UsefulLifeMonths && remaining > 0; i++)
        {
            var date = asset.StartDepreciationDate.AddMonths(i);
            var amount = Math.Min(monthly, remaining);
            accumulated += amount;
            remaining = Math.Max(0m, remaining - amount);
            result.Add(new FixedAssetScheduleDto
            {
                Period = date.ToString("yyyy-MM"),
                DepreciationDate = new DateTime(date.Year, date.Month, DateTime.DaysInMonth(date.Year, date.Month)),
                DepreciationAmount = amount,
                AccumulatedAfter = accumulated,
                RemainingValueAfter = remaining,
                Status = "Planned"
            });
        }

        return result;
    }

    public async Task<IReadOnlyList<FixedAssetDepreciationDto>> GetDepreciationsAsync(Guid? assetId = null, int take = 200, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.FixedAssetDepreciations.AsNoTracking()
            .Include(x => x.FixedAsset)
            .AsQueryable();
        if (assetId.HasValue)
        {
            query = query.Where(x => x.FixedAssetId == assetId.Value);
        }

        return await query
            .OrderByDescending(x => x.DepreciationDate)
            .ThenByDescending(x => x.DepreciationPeriod)
            .Take(Math.Clamp(take, 1, 1000))
            .Select(x => ToDepreciationDto(x))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<FixedAssetAdjustmentDto>> GetAdjustmentsAsync(Guid? assetId = null, int take = 100, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.FixedAssetAdjustments.AsNoTracking()
            .Include(x => x.FixedAsset)
            .AsQueryable();
        if (assetId.HasValue)
        {
            query = query.Where(x => x.FixedAssetId == assetId.Value);
        }

        return await query
            .OrderByDescending(x => x.AdjustmentDate)
            .Take(Math.Clamp(take, 1, 1000))
            .Select(x => ToAdjustmentDto(x))
            .ToListAsync(cancellationToken);
    }

    private async Task<JournalHeader> CreateActivationJournalAsync(FixedAsset asset, string assetAccountCode, string counterAccountCode, string user, CancellationToken cancellationToken)
    {
        var accountNames = await GetAccountNamesAsync([assetAccountCode, counterAccountCode], cancellationToken);
        var journal = new JournalHeader
        {
            Id = Guid.NewGuid(),
            JournalDate = asset.PurchaseDate,
            PostingDate = asset.PurchaseDate,
            SourceType = "FixedAsset",
            SourceId = asset.Id,
            SourceNo = asset.AssetCode,
            Description = $"Ghi tăng TSCĐ {asset.AssetCode}",
            PeriodCode = asset.PurchaseDate.ToString("yyyy-MM"),
            CreatedBy = user,
            CreatedAt = DateTime.UtcNow,
            Lines = new List<JournalLine>
            {
                CreateJournalLine(1, assetAccountCode, ResolveAccountName(accountNames, assetAccountCode), asset.OriginalCost, 0m, asset, $"Ghi tăng TSCĐ {asset.AssetCode}"),
                CreateJournalLine(2, counterAccountCode, ResolveAccountName(accountNames, counterAccountCode), 0m, asset.OriginalCost, asset, $"Đối ứng ghi tăng TSCĐ {asset.AssetCode}")
            }
        };

        var draft = await _journalService.CreateDraftAsync(journal, cancellationToken);
        return await _journalService.PostDraftAsync(draft.Id, user, cancellationToken);
    }

    private async Task<JournalHeader> CreateDepreciationJournalAsync(FixedAsset asset, FixedAssetDepreciation depreciation, string expenseAccountCode, string depreciationAccountCode, string user, CancellationToken cancellationToken)
    {
        var accountNames = await GetAccountNamesAsync([expenseAccountCode, depreciationAccountCode], cancellationToken);
        var description = $"Trích khấu hao TSCĐ {asset.AssetCode} kỳ {depreciation.DepreciationPeriod}";
        var journal = new JournalHeader
        {
            Id = Guid.NewGuid(),
            JournalDate = depreciation.DepreciationDate,
            PostingDate = depreciation.DepreciationDate,
            SourceType = "FixedAssetDepreciation",
            SourceId = depreciation.Id,
            SourceNo = $"{asset.AssetCode}-{depreciation.DepreciationPeriod}",
            Description = description,
            PeriodCode = depreciation.DepreciationDate.ToString("yyyy-MM"),
            CreatedBy = user,
            CreatedAt = DateTime.UtcNow,
            Lines = new List<JournalLine>
            {
                CreateJournalLine(1, expenseAccountCode, ResolveAccountName(accountNames, expenseAccountCode), depreciation.DepreciationAmount, 0m, asset, description),
                CreateJournalLine(2, depreciationAccountCode, ResolveAccountName(accountNames, depreciationAccountCode), 0m, depreciation.DepreciationAmount, asset, description)
            }
        };

        var draft = await _journalService.CreateDraftAsync(journal, cancellationToken);
        return await _journalService.PostDraftAsync(draft.Id, user, cancellationToken);
    }

    private static JournalLine CreateJournalLine(int lineNo, string accountCode, string accountName, decimal debit, decimal credit, FixedAsset asset, string description)
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
            WarehouseId = asset.WarehouseId,
            DepartmentId = asset.DepartmentId,
            EmployeeId = asset.EmployeeId,
            SupplierId = asset.SupplierId,
            ObjectType = "FixedAsset",
            ObjectId = asset.Id,
            Description = description
        };

    private async Task<string> ResolveAccountAsync(FixedAssetCategory? category, string? categoryAccountCode, string roleCode, CancellationToken cancellationToken)
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

    private static decimal CalculateStraightLineDepreciation(FixedAsset asset)
    {
        if (asset.UsefulLifeMonths <= 0)
        {
            return 0m;
        }

        var depreciableAmount = Math.Max(0m, asset.OriginalCost - asset.SalvageValue);
        var remaining = Math.Max(0m, depreciableAmount - asset.AccumulatedDepreciation);
        var monthly = Math.Round(depreciableAmount / asset.UsefulLifeMonths, 2, MidpointRounding.AwayFromZero);
        return Math.Min(monthly, remaining);
    }

    private static void ValidateCreateAssetRequest(CreateFixedAssetRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.AssetCode))
        {
            throw new InvalidOperationException("Mã tài sản cố định là bắt buộc.");
        }
        if (string.IsNullOrWhiteSpace(request.AssetName))
        {
            throw new InvalidOperationException("Tên tài sản cố định là bắt buộc.");
        }
        if (request.CategoryId == Guid.Empty)
        {
            throw new InvalidOperationException("Nhóm tài sản cố định là bắt buộc.");
        }
        if (request.OriginalCost <= 0)
        {
            throw new InvalidOperationException("Nguyên giá tài sản cố định phải lớn hơn 0.");
        }
        if (request.SalvageValue < 0 || request.SalvageValue >= request.OriginalCost)
        {
            throw new InvalidOperationException("Giá trị thu hồi phải lớn hơn hoặc bằng 0 và nhỏ hơn nguyên giá.");
        }
        if (request.StartDepreciationDate.Date < request.PurchaseDate.Date)
        {
            throw new InvalidOperationException("Ngày bắt đầu khấu hao không được trước ngày mua.");
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

        throw new InvalidOperationException("Kỳ khấu hao phải có định dạng yyyy-MM hoặc MM/yyyy.");
    }

    private static DateTime GetPeriodEndDate(string periodCode)
    {
        var parsed = DateTime.ParseExact(periodCode, "yyyy-MM", CultureInfo.InvariantCulture);
        return new DateTime(parsed.Year, parsed.Month, DateTime.DaysInMonth(parsed.Year, parsed.Month));
    }

    private static string? NormalizeOptional(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static FixedAssetCategoryDto ToCategoryDto(FixedAssetCategory category)
        => new()
        {
            Id = category.Id,
            CategoryCode = category.CategoryCode,
            CategoryName = category.CategoryName,
            AssetAccountCode = category.AssetAccountCode,
            DepreciationAccountCode = category.DepreciationAccountCode,
            ExpenseAccountCode = category.ExpenseAccountCode,
            UsefulLifeMonthsDefault = category.UsefulLifeMonthsDefault,
            DepreciationMethodDefault = category.DepreciationMethodDefault,
            IsActive = category.IsActive
        };

    private static FixedAssetDto ToAssetDto(FixedAsset asset)
        => new()
        {
            Id = asset.Id,
            AssetCode = asset.AssetCode,
            AssetName = asset.AssetName,
            CategoryId = asset.CategoryId,
            CategoryCode = asset.Category?.CategoryCode,
            CategoryName = asset.Category?.CategoryName,
            SerialNumber = asset.SerialNumber,
            PurchaseDate = asset.PurchaseDate,
            StartDepreciationDate = asset.StartDepreciationDate,
            OriginalCost = asset.OriginalCost,
            SalvageValue = asset.SalvageValue,
            UsefulLifeMonths = asset.UsefulLifeMonths,
            DepreciationMethod = asset.DepreciationMethod,
            AccumulatedDepreciation = asset.AccumulatedDepreciation,
            RemainingValue = asset.RemainingValue,
            Quantity = asset.Quantity,
            JournalHeaderId = asset.JournalHeaderId,
            Status = asset.Status,
            Notes = asset.Notes,
            IsActive = asset.IsActive
        };

    private static FixedAssetDepreciationDto ToDepreciationDto(FixedAssetDepreciation depreciation)
        => new()
        {
            Id = depreciation.Id,
            FixedAssetId = depreciation.FixedAssetId,
            AssetCode = depreciation.FixedAsset?.AssetCode,
            AssetName = depreciation.FixedAsset?.AssetName,
            DepreciationPeriod = depreciation.DepreciationPeriod,
            DepreciationDate = depreciation.DepreciationDate,
            DepreciationAmount = depreciation.DepreciationAmount,
            AccumulatedAfter = depreciation.AccumulatedAfter,
            RemainingValueAfter = depreciation.RemainingValueAfter,
            JournalHeaderId = depreciation.JournalHeaderId,
            Status = depreciation.Status
        };

    private static FixedAssetAdjustmentDto ToAdjustmentDto(FixedAssetAdjustment adjustment)
        => new()
        {
            Id = adjustment.Id,
            FixedAssetId = adjustment.FixedAssetId,
            AssetCode = adjustment.FixedAsset?.AssetCode,
            AssetName = adjustment.FixedAsset?.AssetName,
            AdjustmentDate = adjustment.AdjustmentDate,
            AdjustmentType = adjustment.AdjustmentType,
            Amount = adjustment.Amount,
            Description = adjustment.Description,
            JournalHeaderId = adjustment.JournalHeaderId
        };

    private async Task LogAsync(string action, string detail, string? username, CancellationToken cancellationToken)
    {
        if (_auditLogService is not null)
        {
            await _auditLogService.LogAsync(action, detail, NormalizeOptional(username) ?? "system", cancellationToken);
        }
    }
}
