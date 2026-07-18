using ASMILE.BO.Application.DTOs;
using ASMILE.BO.Application.Interfaces;
using ASMILE.BO.Core.Accounting;
using ASMILE.BO.Core.Entities;
using ASMILE.BO.Core.Interfaces.Repositories;
using ASMILE.BO.Core.Interfaces.Services;
using ASMILE.BO.Shared.Exceptions;

namespace ASMILE.BO.Application.Services;

public sealed class InventoryAppService : IInventoryAppService
{
    private readonly IStockReceiptRepository _receiptRepository;
    private readonly IStockIssueRepository _issueRepository;
    private readonly IStockBalanceRepository _balanceRepository;
    private readonly IItemRepository _itemRepository;
    private readonly IWarehouseRepository _warehouseRepository;
    private readonly ISupplierRepository _supplierRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditLogService _auditLogService;
    private readonly IPostingEngine _postingEngine;
    private readonly IAccountingPeriodService _accountingPeriodService;
    private readonly IInventoryCostingService _inventoryCostingService;

    public InventoryAppService(
        IStockReceiptRepository receiptRepository,
        IStockIssueRepository issueRepository,
        IStockBalanceRepository balanceRepository,
        IItemRepository itemRepository,
        IWarehouseRepository warehouseRepository,
        ISupplierRepository supplierRepository,
        IUnitOfWork unitOfWork,
        IAuditLogService auditLogService,
        IPostingEngine postingEngine,
        IAccountingPeriodService accountingPeriodService,
        IInventoryCostingService inventoryCostingService)
    {
        _receiptRepository = receiptRepository;
        _issueRepository = issueRepository;
        _balanceRepository = balanceRepository;
        _itemRepository = itemRepository;
        _warehouseRepository = warehouseRepository;
        _supplierRepository = supplierRepository;
        _unitOfWork = unitOfWork;
        _auditLogService = auditLogService;
        _postingEngine = postingEngine;
        _accountingPeriodService = accountingPeriodService;
        _inventoryCostingService = inventoryCostingService;
    }

    public async Task<IReadOnlyList<StockReceiptDto>> SearchReceiptsAsync(DateTime? fromDate, DateTime? toDate, string keyword, int? warehouseId, CancellationToken cancellationToken = default)
    {
        var rows = await _receiptRepository.SearchAsync(fromDate, toDate, keyword ?? string.Empty, warehouseId, cancellationToken);
        var items = (await _itemRepository.GetAllAsync(cancellationToken)).ToDictionary(x => x.Id);
        var warehouses = (await _warehouseRepository.GetAllAsync(cancellationToken)).ToDictionary(x => x.Id, x => x.WarehouseCode);
        return rows.Select(x => MapReceipt(x, items, warehouses)).ToList();
    }

    public async Task<IReadOnlyList<StockIssueDto>> SearchIssuesAsync(DateTime? fromDate, DateTime? toDate, string keyword, int? warehouseId, CancellationToken cancellationToken = default)
    {
        var rows = await _issueRepository.SearchAsync(fromDate, toDate, keyword ?? string.Empty, warehouseId, cancellationToken);
        var items = (await _itemRepository.GetAllAsync(cancellationToken)).ToDictionary(x => x.Id);
        var warehouses = (await _warehouseRepository.GetAllAsync(cancellationToken)).ToDictionary(x => x.Id, x => x.WarehouseCode);
        return rows.Select(x => MapIssue(x, items, warehouses)).ToList();
    }

    public async Task<StockReceiptDto> SaveReceiptAsync(StockReceiptDto dto, CancellationToken cancellationToken = default)
    {
        if (dto.WarehouseId <= 0)
        {
            var firstDetailWarehouse = dto.Details
                .Select(x => x.WarehouseId.GetValueOrDefault())
                .FirstOrDefault(x => x > 0);
            if (firstDetailWarehouse > 0)
            {
                dto.WarehouseId = firstDetailWarehouse;
            }
        }
        else
        {
            foreach (var detail in dto.Details.Where(x => !x.WarehouseId.HasValue || x.WarehouseId <= 0))
            {
                detail.WarehouseId = dto.WarehouseId;
            }
        }

        ValidateReceipt(dto);
        await _accountingPeriodService.ValidatePostingDateAsync(dto.ReceiptDate, cancellationToken);

        var duplicate = await _receiptRepository.FindByNoAsync(dto.ReceiptNo.Trim(), cancellationToken);
        if (duplicate is not null && duplicate.Id != dto.Id)
        {
            throw new AppValidationException("Số phiếu nhập đã tồn tại.");
        }

        StockReceipt entity;
        if (dto.Id == 0)
        {
            entity = ToReceiptEntity(dto);
            await _receiptRepository.AddAsync(entity, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _auditLogService.LogAsync("CREATE_RECEIPT", entity.ReceiptNo, cancellationToken: cancellationToken);
        }
        else
        {
            entity = await _receiptRepository.GetAggregateByIdAsync(dto.Id, cancellationToken)
                ?? throw new AppValidationException("Không tìm thấy phiếu nhập.");
            await EnsureReceiptEditableAsync(entity, cancellationToken);
            ApplyReceipt(entity, dto);
            _receiptRepository.Update(entity);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _auditLogService.LogAsync("UPDATE_RECEIPT", entity.ReceiptNo, cancellationToken: cancellationToken);
        }

        await RebuildBalancesAsync(cancellationToken);
        dto.Id = entity.Id;
        dto.TotalAmount = entity.TotalAmount;
        return dto;
    }

    public async Task<StockIssueDto> SaveIssueAsync(StockIssueDto dto, CancellationToken cancellationToken = default)
    {
        ValidateIssue(dto);
        await _accountingPeriodService.ValidatePostingDateAsync(dto.IssueDate, cancellationToken);
        await EnsureIssueStockValidAsync(dto, cancellationToken);

        var duplicate = await _issueRepository.FindByNoAsync(dto.IssueNo.Trim(), cancellationToken);
        if (duplicate is not null && duplicate.Id != dto.Id)
        {
            throw new AppValidationException("Số phiếu xuất đã tồn tại.");
        }

        StockIssue entity;
        if (dto.Id == 0)
        {
            entity = ToIssueEntity(dto);
            await _issueRepository.AddAsync(entity, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _auditLogService.LogAsync("CREATE_ISSUE", entity.IssueNo, cancellationToken: cancellationToken);
        }
        else
        {
            entity = await _issueRepository.GetAggregateByIdAsync(dto.Id, cancellationToken)
                ?? throw new AppValidationException("Không tìm thấy phiếu xuất.");
            await EnsureIssueEditableAsync(entity, cancellationToken);
            ApplyIssue(entity, dto);
            _issueRepository.Update(entity);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _auditLogService.LogAsync("UPDATE_ISSUE", entity.IssueNo, cancellationToken: cancellationToken);
        }

        await RebuildBalancesAsync(cancellationToken);
        dto.Id = entity.Id;
        dto.TotalAmount = entity.TotalAmount;
        return dto;
    }

    public async Task ApproveReceiptAsync(int id, CancellationToken cancellationToken = default)
    {
        await ApproveReceiptAndPostAsync(id, null, cancellationToken);
    }

    public async Task ApproveIssueAsync(int id, CancellationToken cancellationToken = default)
    {
        await ApproveIssueAndPostAsync(id, null, cancellationToken);
    }

    public async Task ApproveReceiptAndPostAsync(int id, string? postedBy = null, CancellationToken cancellationToken = default)
    {
        var entity = await _receiptRepository.GetAggregateByIdAsync(id, cancellationToken)
            ?? throw new AppValidationException("Không tìm thấy phiếu nhập.");
        await _accountingPeriodService.ValidatePostingDateAsync(entity.ReceiptDate, cancellationToken);

        if (entity.IsApproved)
        {
            return;
        }

        await _unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            entity.IsApproved = true;
            _receiptRepository.Update(entity);
            await _unitOfWork.SaveChangesAsync(ct);

            var costing = await _inventoryCostingService.ApplyReceiptCostAsync(PostingSourceIds.FromInt32(entity.Id), ct);
            if (!costing.Success)
            {
                throw new AppValidationException(string.Join(Environment.NewLine, costing.Errors));
            }

            var posting = await _postingEngine.PostAsync("StockReceipt", PostingSourceIds.FromInt32(entity.Id), postedBy, ct);
            if (!posting.Success)
            {
                throw new AppValidationException(posting.Message);
            }

            entity.PostedAt = DateTime.UtcNow;
            entity.PostedBy = postedBy ?? "system";
            entity.JournalHeaderId = posting.JournalId;
            entity.PostingStatus = "Posted";
            _receiptRepository.Update(entity);
            await _unitOfWork.SaveChangesAsync(ct);
            await _auditLogService.LogAsync("APPROVE_RECEIPT", entity.ReceiptNo, cancellationToken: ct);
        }, cancellationToken);
    }

    public async Task ApproveIssueAndPostAsync(int id, string? postedBy = null, CancellationToken cancellationToken = default)
    {
        var entity = await _issueRepository.GetAggregateByIdAsync(id, cancellationToken)
            ?? throw new AppValidationException("Không tìm thấy phiếu xuất.");
        await _accountingPeriodService.ValidatePostingDateAsync(entity.IssueDate, cancellationToken);

        if (entity.IsApproved)
        {
            return;
        }

        await _unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            entity.IsApproved = true;
            _issueRepository.Update(entity);
            await _unitOfWork.SaveChangesAsync(ct);

            var costing = await _inventoryCostingService.ApplyIssueCostAsync(PostingSourceIds.FromInt32(entity.Id), ct);
            if (!costing.Success)
            {
                throw new AppValidationException(string.Join(Environment.NewLine, costing.Errors));
            }

            var posting = await _postingEngine.PostAsync("StockIssue", PostingSourceIds.FromInt32(entity.Id), postedBy, ct);
            if (!posting.Success)
            {
                throw new AppValidationException(posting.Message);
            }

            entity.PostedAt = DateTime.UtcNow;
            entity.PostedBy = postedBy ?? "system";
            entity.JournalHeaderId = posting.JournalId;
            entity.PostingStatus = "Posted";
            _issueRepository.Update(entity);
            await _unitOfWork.SaveChangesAsync(ct);
            await _auditLogService.LogAsync("APPROVE_ISSUE", entity.IssueNo, cancellationToken: ct);
        }, cancellationToken);
    }

    public async Task UnapproveReceiptAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _receiptRepository.GetAggregateByIdAsync(id, cancellationToken)
            ?? throw new AppValidationException("Không tìm thấy phiếu nhập.");

        await EnsureReceiptEditableAsync(entity, cancellationToken);
        if (!entity.IsApproved)
        {
            return;
        }

        entity.IsApproved = false;
        _receiptRepository.Update(entity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _auditLogService.LogAsync("UNAPPROVE_RECEIPT", entity.ReceiptNo, cancellationToken: cancellationToken);
    }

    public async Task UnapproveIssueAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _issueRepository.GetAggregateByIdAsync(id, cancellationToken)
            ?? throw new AppValidationException("Không tìm thấy phiếu xuất.");
        await EnsureIssueEditableAsync(entity, cancellationToken);

        if (!entity.IsApproved)
        {
            return;
        }

        entity.IsApproved = false;
        _issueRepository.Update(entity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _auditLogService.LogAsync("UNAPPROVE_ISSUE", entity.IssueNo, cancellationToken: cancellationToken);
    }

    public async Task ReverseReceiptAsync(int id, string reason, string? reversedBy = null, CancellationToken cancellationToken = default)
    {
        var entity = await _receiptRepository.GetAggregateByIdAsync(id, cancellationToken)
            ?? throw new AppValidationException("Không tìm thấy phiếu nhập.");

        if (!entity.JournalHeaderId.HasValue || !string.Equals(entity.PostingStatus, "Posted", StringComparison.OrdinalIgnoreCase))
        {
            throw new AppValidationException("Chỉ được đảo phiếu nhập đã ghi sổ.");
        }

        await _unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            var reversal = await _postingEngine.ReverseAsync(
                entity.JournalHeaderId.Value,
                string.IsNullOrWhiteSpace(reason) ? "Reverse stock receipt" : reason.Trim(),
                reversedBy,
                ct);

            if (!reversal.Success)
            {
                throw new AppValidationException(reversal.Message);
            }

            entity.IsApproved = false;
            entity.PostingStatus = "Reversed";
            entity.PostedBy = reversedBy ?? entity.PostedBy;
            _receiptRepository.Update(entity);
            await _unitOfWork.SaveChangesAsync(ct);
            await RebuildBalancesAsync(ct);
            await _auditLogService.LogAsync("REVERSE_RECEIPT", entity.ReceiptNo, cancellationToken: ct);
        }, cancellationToken);
    }

    public async Task ReverseIssueAsync(int id, string reason, string? reversedBy = null, CancellationToken cancellationToken = default)
    {
        var entity = await _issueRepository.GetAggregateByIdAsync(id, cancellationToken)
            ?? throw new AppValidationException("Không tìm thấy phiếu xuất.");

        if (!entity.JournalHeaderId.HasValue || !string.Equals(entity.PostingStatus, "Posted", StringComparison.OrdinalIgnoreCase))
        {
            throw new AppValidationException("Chỉ được đảo phiếu xuất đã ghi sổ.");
        }

        await _unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            var reversal = await _postingEngine.ReverseAsync(
                entity.JournalHeaderId.Value,
                string.IsNullOrWhiteSpace(reason) ? "Reverse stock issue" : reason.Trim(),
                reversedBy,
                ct);

            if (!reversal.Success)
            {
                throw new AppValidationException(reversal.Message);
            }

            entity.IsApproved = false;
            entity.PostingStatus = "Reversed";
            entity.PostedBy = reversedBy ?? entity.PostedBy;
            _issueRepository.Update(entity);
            await _unitOfWork.SaveChangesAsync(ct);
            await RebuildBalancesAsync(ct);
            await _auditLogService.LogAsync("REVERSE_ISSUE", entity.IssueNo, cancellationToken: ct);
        }, cancellationToken);
    }

    public async Task DeleteReceiptAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _receiptRepository.GetAggregateByIdAsync(id, cancellationToken)
            ?? throw new AppValidationException("Không tìm thấy phiếu nhập.");
        await EnsureReceiptEditableAsync(entity, cancellationToken);
        _receiptRepository.Delete(entity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await RebuildBalancesAsync(cancellationToken);
        await _auditLogService.LogAsync("DELETE_RECEIPT", entity.ReceiptNo, cancellationToken: cancellationToken);
    }

    public async Task DeleteIssueAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _issueRepository.GetAggregateByIdAsync(id, cancellationToken)
            ?? throw new AppValidationException("Không tìm thấy phiếu xuất.");
        await EnsureIssueEditableAsync(entity, cancellationToken);
        _issueRepository.Delete(entity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await RebuildBalancesAsync(cancellationToken);
        await _auditLogService.LogAsync("DELETE_ISSUE", entity.IssueNo, cancellationToken: cancellationToken);
    }


    private async Task EnsureReceiptEditableAsync(StockReceipt entity, CancellationToken cancellationToken)
    {
        await _accountingPeriodService.ValidatePostingDateAsync(entity.ReceiptDate, cancellationToken);
        if (entity.JournalHeaderId.HasValue || string.Equals(entity.PostingStatus, "Posted", StringComparison.OrdinalIgnoreCase))
        {
            throw new AppValidationException("Phiếu nhập đã ghi sổ, không thể sửa hoặc xóa trực tiếp.");
        }
    }

    private async Task EnsureIssueEditableAsync(StockIssue entity, CancellationToken cancellationToken)
    {
        await _accountingPeriodService.ValidatePostingDateAsync(entity.IssueDate, cancellationToken);
        if (entity.JournalHeaderId.HasValue || string.Equals(entity.PostingStatus, "Posted", StringComparison.OrdinalIgnoreCase))
        {
            throw new AppValidationException("Phiếu xuất đã ghi sổ, không thể sửa hoặc xóa trực tiếp.");
        }
    }

    public async Task<IReadOnlyList<StockBalanceDto>> GetWarehouseStockAsync(int warehouseId, CancellationToken cancellationToken = default)
    {
        var rows = await _balanceRepository.GetByWarehouseAsync(warehouseId, cancellationToken);
        var items = (await _itemRepository.GetAllAsync(cancellationToken)).ToDictionary(x => x.Id);

        return rows.OrderBy(x => items.TryGetValue(x.ItemId, out var i) ? i.ItemCode : string.Empty)
            .Select(x => new StockBalanceDto
            {
                ItemId = x.ItemId,
                ItemCode = items.TryGetValue(x.ItemId, out var item) ? item.ItemCode : string.Empty,
                ItemName = items.TryGetValue(x.ItemId, out item) ? item.ItemName : string.Empty,
                Unit = items.TryGetValue(x.ItemId, out item) ? item.Unit : string.Empty,
                OpeningQty = x.OpeningQty,
                InQty = x.InQty,
                OutQty = x.OutQty,
                ClosingQty = x.ClosingQty,
                AvgPrice = x.AvgPrice,
                ClosingAmount = x.ClosingAmount
            }).ToList();
    }

    public async Task<IReadOnlyList<InventoryStockInsightDto>> GetItemStockInsightsAsync(int itemId, CancellationToken cancellationToken = default)
    {
        var result = await BuildItemStockInsightsAsync(itemId, cancellationToken);
        if (result.Count == 0)
        {
            await RebuildBalancesAsync(cancellationToken);
            result = await BuildItemStockInsightsAsync(itemId, cancellationToken);
        }

        return result;
    }

    public async Task<IReadOnlyList<InventoryStockInsightDto>> GetWarehouseStockInsightsAsync(int warehouseId, CancellationToken cancellationToken = default)
    {
        var rows = await _balanceRepository.GetByWarehouseAsync(warehouseId, cancellationToken);
        if (rows.Count == 0)
        {
            await RebuildBalancesAsync(cancellationToken);
            rows = await _balanceRepository.GetByWarehouseAsync(warehouseId, cancellationToken);
        }
        var items = (await _itemRepository.GetAllAsync(cancellationToken)).ToDictionary(x => x.Id);
        var warehouses = (await _warehouseRepository.GetAllAsync(cancellationToken)).ToDictionary(x => x.Id);
        var priceHistory = await GetImportPriceHistoryAsync(cancellationToken);
        var result = new List<InventoryStockInsightDto>();

        foreach (var row in rows.OrderBy(x => items.TryGetValue(x.ItemId, out var item) ? item.ItemCode : string.Empty))
        {
            var key = BuildWarehouseItemKey(warehouseId, row.ItemId);
            priceHistory.TryGetValue(key, out var history);
            history ??= [];
            var currentPrice = history.Count > 0 ? history[^1] : 0m;
            var previousPrice = history.Count > 1 ? history[^2] : currentPrice;
            var item = items.TryGetValue(row.ItemId, out var mappedItem) ? mappedItem : null;

            result.Add(new InventoryStockInsightDto
            {
                ItemId = row.ItemId,
                ItemCode = item?.ItemCode ?? string.Empty,
                ItemName = item?.ItemName ?? string.Empty,
                Unit = item?.Unit ?? string.Empty,
                WarehouseId = warehouseId,
                WarehouseCode = warehouses.TryGetValue(warehouseId, out var warehouse) ? warehouse.WarehouseCode : string.Empty,
                WarehouseName = warehouses.TryGetValue(warehouseId, out warehouse) ? warehouse.WarehouseName : string.Empty,
                OpeningQty = row.OpeningQty,
                OpeningAmount = row.OpeningAmount,
                InQty = row.InQty,
                InAmount = row.InAmount,
                OutQty = row.OutQty,
                OutAmount = row.OutAmount,
                ClosingQty = row.ClosingQty,
                PreviousImportPrice = previousPrice,
                CurrentImportPrice = currentPrice,
                AveragePrice = row.AvgPrice,
                ClosingAmount = row.ClosingAmount
            });
        }

        return result;
    }

    public async Task RebuildBalancesAsync(CancellationToken cancellationToken = default)
    {
        var receiptHeaders = await _receiptRepository.SearchAsync(null, null, string.Empty, null, cancellationToken);
        var issueHeaders = await _issueRepository.SearchAsync(null, null, string.Empty, null, cancellationToken);

        var receipts = new List<StockReceipt>();
        foreach (var h in receiptHeaders)
        {
            var aggregate = await _receiptRepository.GetAggregateByIdAsync(h.Id, cancellationToken);
            if (aggregate is not null) receipts.Add(aggregate);
        }

        var issues = new List<StockIssue>();
        foreach (var h in issueHeaders)
        {
            var aggregate = await _issueRepository.GetAggregateByIdAsync(h.Id, cancellationToken);
            if (aggregate is not null) issues.Add(aggregate);
        }

        await _balanceRepository.RemoveAllAsync(cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var balanceMap = new Dictionary<string, StockBalance>();
        StockBalance GetBalance(int warehouseId, int itemId)
        {
            var key = $"{warehouseId}_{itemId}";
            if (balanceMap.TryGetValue(key, out var b)) return b;
            b = new StockBalance { WarehouseId = warehouseId, ItemId = itemId };
            balanceMap[key] = b;
            return b;
        }

        var activeReceipts = receipts.Where(x => !string.Equals(x.PostingStatus, "Reversed", StringComparison.OrdinalIgnoreCase)).ToList();
        var activeIssues = issues.Where(x => !string.Equals(x.PostingStatus, "Reversed", StringComparison.OrdinalIgnoreCase)).ToList();

        var events = activeReceipts.Select(x => new { Kind = "IN", x.ReceiptDate, Id = x.Id })
            .Concat(activeIssues.Select(x => new { Kind = "OUT", ReceiptDate = x.IssueDate, Id = x.Id }))
            .OrderBy(x => x.ReceiptDate)
            .ThenBy(x => x.Kind)
            .ThenBy(x => x.Id)
            .ToList();

        foreach (var ev in events)
        {
            if (ev.Kind == "IN")
            {
                var receipt = activeReceipts.First(x => x.Id == ev.Id);
                foreach (var d in receipt.Details)
                {
                    var warehouseId = d.WarehouseId.GetValueOrDefault(receipt.WarehouseId);
                    var b = GetBalance(warehouseId, d.ItemId);
                    d.Amount = d.Quantity * d.UnitPrice;
                    b.InQty += d.Quantity;
                    b.InAmount += d.Amount;
                    b.ClosingQty += d.Quantity;
                    b.ClosingAmount += d.Amount;
                    b.AvgPrice = b.ClosingQty == 0 ? 0 : b.ClosingAmount / b.ClosingQty;
                }
                receipt.TotalAmount = receipt.Details.Sum(x => x.Amount);
            }
            else
            {
                var issue = activeIssues.First(x => x.Id == ev.Id);
                foreach (var d in issue.Details)
                {
                    var sourceWarehouseId = d.WarehouseId.GetValueOrDefault(issue.WarehouseId);
                    var sourceBalance = GetBalance(sourceWarehouseId, d.ItemId);
                    d.UnitPrice = sourceBalance.AvgPrice;
                    d.Amount = d.Quantity * d.UnitPrice;

                    sourceBalance.OutQty += d.Quantity;
                    sourceBalance.OutAmount += d.Amount;
                    sourceBalance.ClosingQty = Math.Max(0, sourceBalance.ClosingQty - d.Quantity);
                    sourceBalance.ClosingAmount = Math.Max(0, sourceBalance.ClosingAmount - d.Amount);
                    sourceBalance.AvgPrice = sourceBalance.ClosingQty == 0 ? 0 : sourceBalance.ClosingAmount / sourceBalance.ClosingQty;

                    // Chuyển kho: đồng thời ghi tăng vào kho nhận để kho tiêu hao (hoặc kho đích) có số liệu tồn.
                    if (issue.ToWarehouseId.HasValue && issue.ToWarehouseId.Value > 0 && issue.ToWarehouseId.Value != sourceWarehouseId)
                    {
                        var targetWarehouseId = issue.ToWarehouseId.Value;
                        var targetBalance = GetBalance(targetWarehouseId, d.ItemId);
                        targetBalance.InQty += d.Quantity;
                        targetBalance.InAmount += d.Amount;
                        targetBalance.ClosingQty += d.Quantity;
                        targetBalance.ClosingAmount += d.Amount;
                        targetBalance.AvgPrice = targetBalance.ClosingQty == 0 ? 0 : targetBalance.ClosingAmount / targetBalance.ClosingQty;
                    }
                }
                issue.TotalAmount = issue.Details.Sum(x => x.Amount);
            }
        }

        foreach (var b in balanceMap.Values)
        {
            await _balanceRepository.AddAsync(b, cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<DashboardDto> GetDashboardAsync(CancellationToken cancellationToken = default)
    {
        var monthFrom = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        var monthTo = monthFrom.AddMonths(1).AddDays(-1);

        var warehouses = await _warehouseRepository.GetAllAsync(cancellationToken);
        var inventoryValue = 0m;
        foreach (var w in warehouses)
        {
            inventoryValue += (await _balanceRepository.GetByWarehouseAsync(w.Id, cancellationToken)).Sum(x => x.ClosingAmount);
        }

        return new DashboardDto
        {
            TotalItems = (await _itemRepository.GetAllAsync(cancellationToken)).Count,
            TotalSuppliers = (await _supplierRepository.GetAllAsync(cancellationToken)).Count,
            TotalWarehouses = warehouses.Count,
            ReceiptsThisMonth = (await _receiptRepository.SearchAsync(monthFrom, monthTo, string.Empty, null, cancellationToken)).Count,
            IssuesThisMonth = (await _issueRepository.SearchAsync(monthFrom, monthTo, string.Empty, null, cancellationToken)).Count,
            InventoryValue = inventoryValue
        };
    }

    private async Task EnsureIssueStockValidAsync(StockIssueDto dto, CancellationToken cancellationToken)
    {
        var requested = dto.Details.GroupBy(x => x.ItemId).ToDictionary(g => g.Key, g => g.Sum(x => x.Quantity));
        var currentBalances = (await _balanceRepository.GetByWarehouseAsync(dto.WarehouseId, cancellationToken))
            .ToDictionary(x => x.ItemId, x => x.ClosingQty);

        var oldIssue = dto.Id > 0 ? await _issueRepository.GetAggregateByIdAsync(dto.Id, cancellationToken) : null;
        var returning = oldIssue?.Details.GroupBy(x => x.ItemId).ToDictionary(g => g.Key, g => g.Sum(x => x.Quantity))
                       ?? new Dictionary<int, decimal>();

        foreach (var pair in requested)
        {
            var available = currentBalances.GetValueOrDefault(pair.Key, 0m) + returning.GetValueOrDefault(pair.Key, 0m);
            if (pair.Value > available)
            {
                throw new AppValidationException($"Số lượng xuất vượt tồn khả dụng (ItemId={pair.Key}, tồn={available:#,##0.####}, yêu cầu={pair.Value:#,##0.####}).");
            }
        }
    }

    private async Task<Dictionary<string, List<decimal>>> GetImportPriceHistoryAsync(CancellationToken cancellationToken)
    {
        var headers = await _receiptRepository.SearchAsync(null, null, string.Empty, null, cancellationToken);
        var orderedHeaders = headers
            .OrderBy(x => x.ReceiptDate)
            .ThenBy(x => x.Id)
            .ToList();

        var map = new Dictionary<string, List<decimal>>(StringComparer.OrdinalIgnoreCase);
        foreach (var header in orderedHeaders)
        {
            var aggregate = await _receiptRepository.GetAggregateByIdAsync(header.Id, cancellationToken);
            if (aggregate is null)
            {
                continue;
            }

            var fallbackWarehouseId = aggregate.WarehouseId;
            foreach (var detail in aggregate.Details.OrderBy(x => x.LineNo))
            {
                var warehouseId = detail.WarehouseId.GetValueOrDefault(fallbackWarehouseId);
                if (warehouseId <= 0 || detail.ItemId <= 0)
                {
                    continue;
                }

                var key = BuildWarehouseItemKey(warehouseId, detail.ItemId);
                if (!map.TryGetValue(key, out var values))
                {
                    values = new List<decimal>();
                    map[key] = values;
                }

                values.Add(detail.UnitPrice);
            }
        }

        return map;
    }

    private static string BuildWarehouseItemKey(int warehouseId, int itemId) => $"{warehouseId}_{itemId}";

    private async Task<List<InventoryStockInsightDto>> BuildItemStockInsightsAsync(int itemId, CancellationToken cancellationToken)
    {
        var items = (await _itemRepository.GetAllAsync(cancellationToken)).ToDictionary(x => x.Id);
        var warehouses = await _warehouseRepository.GetAllAsync(cancellationToken);
        var balancesByWarehouse = new Dictionary<int, StockBalance?>();
        foreach (var warehouse in warehouses)
        {
            balancesByWarehouse[warehouse.Id] = await _balanceRepository.GetByWarehouseAndItemAsync(warehouse.Id, itemId, cancellationToken);
        }

        var priceHistory = await GetImportPriceHistoryAsync(cancellationToken);
        var result = new List<InventoryStockInsightDto>();
        foreach (var warehouse in warehouses.OrderBy(x => x.WarehouseCode))
        {
            var balance = balancesByWarehouse[warehouse.Id];
            var key = BuildWarehouseItemKey(warehouse.Id, itemId);
            priceHistory.TryGetValue(key, out var history);
            history ??= [];
            var currentPrice = history.Count > 0 ? history[^1] : 0m;
            var previousPrice = history.Count > 1 ? history[^2] : currentPrice;

            var closingQty = balance?.ClosingQty ?? 0m;
            if (closingQty <= 0m && history.Count == 0)
            {
                continue;
            }

            result.Add(new InventoryStockInsightDto
            {
                ItemId = itemId,
                ItemCode = items.TryGetValue(itemId, out var item) ? item.ItemCode : string.Empty,
                ItemName = items.TryGetValue(itemId, out item) ? item.ItemName : string.Empty,
                Unit = items.TryGetValue(itemId, out item) ? item.Unit : string.Empty,
                WarehouseId = warehouse.Id,
                WarehouseCode = warehouse.WarehouseCode,
                WarehouseName = warehouse.WarehouseName,
                OpeningQty = balance?.OpeningQty ?? 0m,
                OpeningAmount = balance?.OpeningAmount ?? 0m,
                InQty = balance?.InQty ?? 0m,
                InAmount = balance?.InAmount ?? 0m,
                OutQty = balance?.OutQty ?? 0m,
                OutAmount = balance?.OutAmount ?? 0m,
                ClosingQty = closingQty,
                PreviousImportPrice = previousPrice,
                CurrentImportPrice = currentPrice,
                AveragePrice = balance?.AvgPrice ?? 0m,
                ClosingAmount = balance?.ClosingAmount ?? 0m
            });
        }

        return result;
    }

    private static void ValidateReceipt(StockReceiptDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.ReceiptNo)) throw new AppValidationException("Số phiếu nhập không được rỗng.");
        if (dto.Details.Count == 0) throw new AppValidationException("Phiếu nhập phải có ít nhất 1 dòng.");
        if (dto.WarehouseId <= 0 || dto.Details.Any(x => !x.WarehouseId.HasValue || x.WarehouseId <= 0))
            throw new AppValidationException("Vui lòng chọn kho nhập cho các dòng chi tiết.");
        if (dto.Details.Any(x => x.ItemId <= 0 || x.Quantity <= 0 || x.UnitPrice < 0)) throw new AppValidationException("Dòng chi tiết phiếu nhập không hợp lệ.");
    }

    private static void ValidateIssue(StockIssueDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.IssueNo)) throw new AppValidationException("Số phiếu xuất không được rỗng.");
        if (dto.WarehouseId <= 0) throw new AppValidationException("Vui lòng chọn kho.");
        if (string.IsNullOrWhiteSpace(dto.DepartmentCode)) throw new AppValidationException("Vui lòng nhập mã bộ phận.");
        if (dto.Details.Count == 0) throw new AppValidationException("Phiếu xuất phải có ít nhất 1 dòng.");
        if (dto.Details.Any(x => x.ItemId <= 0 || x.Quantity <= 0)) throw new AppValidationException("Dòng chi tiết phiếu xuất không hợp lệ.");
    }

    private static StockReceipt ToReceiptEntity(StockReceiptDto dto)
    {
        var entity = new StockReceipt();
        ApplyReceipt(entity, dto);
        return entity;
    }

    private static void ApplyReceipt(StockReceipt entity, StockReceiptDto dto)
    {
        entity.ReceiptNo = dto.ReceiptNo.Trim();
        entity.ReceiptDate = dto.ReceiptDate;
        entity.SupplierCode = dto.SupplierCode.Trim();
        entity.SupplierName = dto.SupplierName.Trim();
        entity.ReceiverName = dto.ReceiverName.Trim();
        entity.Description = dto.Description.Trim();
        entity.WarehouseId = dto.WarehouseId;
        entity.DebitAccountCode = dto.DebitAccountCode.Trim();
        entity.CreditAccountCode = dto.CreditAccountCode.Trim();
        entity.IsApproved = dto.IsApproved;
        entity.Details = dto.Details.Select(d => new StockReceiptDetail
        {
            LineNo = d.LineNo,
            ItemId = d.ItemId,
            Unit = d.Unit,
            WarehouseId = d.WarehouseId,
            Quantity = d.Quantity,
            UnitPrice = d.UnitPrice,
            Amount = d.Quantity * d.UnitPrice,
            VatRate = d.VatRate,
            VatAmount = Math.Round(d.Quantity * d.UnitPrice * d.VatRate / 100m, 2, MidpointRounding.AwayFromZero)
        }).ToList();
        entity.TotalAmount = entity.Details.Sum(x => x.Amount);
    }

    private static StockIssue ToIssueEntity(StockIssueDto dto)
    {
        var entity = new StockIssue();
        ApplyIssue(entity, dto);
        return entity;
    }

    private static void ApplyIssue(StockIssue entity, StockIssueDto dto)
    {
        entity.IssueNo = dto.IssueNo.Trim();
        entity.IssueDate = dto.IssueDate;
        entity.Description = dto.Description.Trim();
        entity.WarehouseId = dto.WarehouseId;
        entity.ToWarehouseId = dto.ToWarehouseId;
        entity.DepartmentCode = dto.DepartmentCode.Trim();
        entity.DebitAccountCode = dto.DebitAccountCode.Trim();
        entity.CreditAccountCode = dto.CreditAccountCode.Trim();
        entity.IsApproved = dto.IsApproved;
        entity.Details = dto.Details.Select(d => new StockIssueDetail
        {
            LineNo = d.LineNo,
            ItemId = d.ItemId,
            Unit = d.Unit,
            WarehouseId = d.WarehouseId,
            Quantity = d.Quantity,
            UnitPrice = d.UnitPrice,
            Amount = d.Amount
        }).ToList();
        entity.TotalAmount = entity.Details.Sum(x => x.Amount);
    }

    private static StockReceiptDto MapReceipt(
        StockReceipt entity,
        IReadOnlyDictionary<int, Item> items,
        IReadOnlyDictionary<int, string> warehouses) => new()
    {
        Id = entity.Id,
        ReceiptNo = entity.ReceiptNo,
        ReceiptDate = entity.ReceiptDate,
        SupplierCode = entity.SupplierCode,
        SupplierName = entity.SupplierName,
        ReceiverName = entity.ReceiverName,
        Description = entity.Description,
        WarehouseId = entity.WarehouseId,
        DebitAccountCode = entity.DebitAccountCode,
        CreditAccountCode = entity.CreditAccountCode,
        TotalAmount = entity.TotalAmount,
        IsApproved = entity.IsApproved,
        PostedAt = entity.PostedAt,
        PostedBy = entity.PostedBy,
        JournalHeaderId = entity.JournalHeaderId,
        PostingStatus = entity.PostingStatus,
        Details = entity.Details.Select(d => new StockTransactionDetailDto
        {
            Id = d.Id,
            LineNo = d.LineNo,
            ItemId = d.ItemId,
            ItemCode = items.TryGetValue(d.ItemId, out var item) ? item.ItemCode : string.Empty,
            ItemName = items.TryGetValue(d.ItemId, out item) ? item.ItemName : string.Empty,
            Unit = d.Unit,
            WarehouseId = d.WarehouseId,
            WarehouseCode = d.WarehouseId.HasValue && warehouses.TryGetValue(d.WarehouseId.Value, out var warehouseCode)
                ? warehouseCode
                : (warehouses.TryGetValue(entity.WarehouseId, out var headerWarehouseCode) ? headerWarehouseCode : string.Empty),
            Quantity = d.Quantity,
            UnitPrice = d.UnitPrice,
            Amount = d.Amount,
            VatRate = d.VatRate,
            VatAmount = d.VatAmount
        }).ToList()
    };

    private static StockIssueDto MapIssue(StockIssue entity) => new()
    {
        Id = entity.Id,
        IssueNo = entity.IssueNo,
        IssueDate = entity.IssueDate,
        Description = entity.Description,
        WarehouseId = entity.WarehouseId,
        ToWarehouseId = entity.ToWarehouseId,
        DepartmentCode = entity.DepartmentCode,
        DebitAccountCode = entity.DebitAccountCode,
        CreditAccountCode = entity.CreditAccountCode,
        TotalAmount = entity.TotalAmount,
        IsApproved = entity.IsApproved,
        PostedAt = entity.PostedAt,
        PostedBy = entity.PostedBy,
        JournalHeaderId = entity.JournalHeaderId,
        PostingStatus = entity.PostingStatus,
        Details = entity.Details.Select(d => new StockTransactionDetailDto
        {
            Id = d.Id,
            LineNo = d.LineNo,
            ItemId = d.ItemId,
            Unit = d.Unit,
            WarehouseId = d.WarehouseId,
            Quantity = d.Quantity,
            UnitPrice = d.UnitPrice,
            Amount = d.Amount
        }).ToList()
    };

    private static StockIssueDto MapIssue(StockIssue entity, IReadOnlyDictionary<int, Item> items, IReadOnlyDictionary<int, string> warehouses) => new()
    {
        Id = entity.Id,
        IssueNo = entity.IssueNo,
        IssueDate = entity.IssueDate,
        Description = entity.Description,
        WarehouseId = entity.WarehouseId,
        WarehouseCode = warehouses.TryGetValue(entity.WarehouseId, out var warehouseCode) ? warehouseCode : string.Empty,
        ToWarehouseId = entity.ToWarehouseId,
        ToWarehouseCode = entity.ToWarehouseId.HasValue && warehouses.TryGetValue(entity.ToWarehouseId.Value, out var toWarehouseCode)
            ? toWarehouseCode
            : string.Empty,
        DepartmentCode = entity.DepartmentCode,
        DebitAccountCode = entity.DebitAccountCode,
        CreditAccountCode = entity.CreditAccountCode,
        TotalAmount = entity.TotalAmount,
        IsApproved = entity.IsApproved,
        PostedAt = entity.PostedAt,
        PostedBy = entity.PostedBy,
        JournalHeaderId = entity.JournalHeaderId,
        PostingStatus = entity.PostingStatus,
        Details = entity.Details.Select(d => new StockTransactionDetailDto
        {
            Id = d.Id,
            LineNo = d.LineNo,
            ItemId = d.ItemId,
            ItemCode = items.TryGetValue(d.ItemId, out var item) ? item.ItemCode : string.Empty,
            ItemName = items.TryGetValue(d.ItemId, out item) ? item.ItemName : string.Empty,
            Unit = !string.IsNullOrWhiteSpace(d.Unit)
                ? d.Unit
                : (items.TryGetValue(d.ItemId, out var itemUnit) ? itemUnit.Unit : string.Empty),
            WarehouseId = d.WarehouseId,
            WarehouseCode = d.WarehouseId.HasValue && warehouses.TryGetValue(d.WarehouseId.Value, out var detailWarehouseCode)
                ? detailWarehouseCode
                : (warehouses.TryGetValue(entity.WarehouseId, out var headerWarehouseCode) ? headerWarehouseCode : string.Empty),
            Quantity = d.Quantity,
            UnitPrice = d.UnitPrice,
            Amount = d.Amount
        }).ToList()
    };
}

