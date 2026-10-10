using ASMILE.BO.Application.DTOs;
using ASMILE.BO.Application.Interfaces;
using ASMILE.BO.Application.Interfaces.Settings;
using ASMILE.BO.Core.Accounting;
using ASMILE.BO.Core.Entities;
using ASMILE.BO.Core.Entities.Settings;
using ASMILE.BO.Core.Enums;
using ASMILE.BO.Core.Interfaces.Services;
using ASMILE.BO.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ASMILE.BO.Infrastructure.Services;

public sealed class InventoryCostingService : IInventoryCostingService
{
    private const string StockReceiptSourceType = "StockReceipt";
    private const string StockIssueSourceType = "StockIssue";
    private const string MovingAverageMethod = nameof(InventoryCostMethod.MovingAverage);

    private readonly AppDbContext _dbContext;
    private readonly IAuditLogService _auditLogService;
    private readonly ISettingsService? _settingsService;

    public InventoryCostingService(AppDbContext dbContext, IAuditLogService auditLogService)
        : this(dbContext, auditLogService, null)
    {
    }

    public InventoryCostingService(AppDbContext dbContext, IAuditLogService auditLogService, ISettingsService? settingsService)
    {
        _dbContext = dbContext;
        _auditLogService = auditLogService;
        _settingsService = settingsService;
    }

    public async Task<InventoryCostResult> ApplyReceiptCostAsync(Guid stockReceiptId, CancellationToken cancellationToken = default)
    {
        var receiptId = DecodeIntSourceId(stockReceiptId, StockReceiptSourceType);
        var receipt = await _dbContext.StockReceipts
            .Include(x => x.Details)
            .FirstOrDefaultAsync(x => x.Id == receiptId, cancellationToken);

        if (receipt is null)
        {
            return InventoryCostResult.Failed("Không tìm thấy phiếu nhập để tính giá vốn.");
        }

        if (receipt.Details.Count == 0)
        {
            return InventoryCostResult.Failed("Phiếu nhập chưa có chi tiết để tạo cost layer.");
        }

        var results = new List<InventoryCostLineResult>();
        var now = DateTime.UtcNow;

        foreach (var detail in receipt.Details.OrderBy(x => x.LineNo))
        {
            if (detail.Quantity <= 0)
            {
                return InventoryCostResult.Failed($"Số lượng nhập phải lớn hơn 0 ở dòng {detail.LineNo}.");
            }

            var warehouseId = detail.WarehouseId ?? receipt.WarehouseId;
            if (detail.ItemId <= 0 || warehouseId <= 0)
            {
                return InventoryCostResult.Failed($"Dòng {detail.LineNo} thiếu hàng hóa hoặc kho.");
            }

            var sourceDetailId = PostingSourceIds.FromInt32(detail.Id);
            var exists = await _dbContext.InventoryCostLayers.AnyAsync(x =>
                x.SourceType == StockReceiptSourceType && x.SourceDetailId == sourceDetailId, cancellationToken);
            if (exists)
            {
                continue;
            }

            detail.Amount = detail.Amount > 0 ? detail.Amount : Math.Round(detail.Quantity * detail.UnitPrice, 2, MidpointRounding.AwayFromZero);
            var unitCost = detail.Quantity == 0 ? 0 : Math.Round(detail.Amount / detail.Quantity, 6, MidpointRounding.AwayFromZero);

            _dbContext.InventoryCostLayers.Add(new InventoryCostLayer
            {
                Id = Guid.NewGuid(),
                ItemId = detail.ItemId,
                WarehouseId = warehouseId,
                SourceType = StockReceiptSourceType,
                SourceId = stockReceiptId,
                SourceDetailId = sourceDetailId,
                DocumentNo = receipt.ReceiptNo,
                DocumentDate = receipt.ReceiptDate.Date,
                QuantityIn = detail.Quantity,
                QuantityOut = 0,
                UnitCost = unitCost,
                TotalCost = detail.Amount,
                RemainingQuantity = detail.Quantity,
                RemainingCost = detail.Amount,
                CostMethod = MovingAverageMethod,
                Status = "Open",
                CreatedAt = now,
                UpdatedAt = now
            });

            results.Add(new InventoryCostLineResult
            {
                ItemId = detail.ItemId,
                WarehouseId = warehouseId,
                SourceDetailId = sourceDetailId,
                Quantity = detail.Quantity,
                UnitCost = unitCost,
                TotalCost = detail.Amount,
                CostMethod = MovingAverageMethod
            });
        }

        receipt.TotalAmount = receipt.Details.Sum(x => x.Amount);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await _auditLogService.LogAsync("InventoryCostReceipt", $"Áp dụng giá vốn phiếu nhập {receipt.ReceiptNo}", "inventory", cancellationToken);
        return InventoryCostResult.Succeeded(results);
    }

    public Task<InventoryCostResult> CalculateIssueCostAsync(Guid stockIssueId, CancellationToken cancellationToken = default)
        => CalculateIssueCostInternalAsync(stockIssueId, apply: false, cancellationToken);

    public Task<InventoryCostResult> ApplyIssueCostAsync(Guid stockIssueId, CancellationToken cancellationToken = default)
        => CalculateIssueCostInternalAsync(stockIssueId, apply: true, cancellationToken);

    public async Task RecalculateCostAsync(Guid itemId, Guid? warehouseId, DateTime fromDate, CancellationToken cancellationToken = default)
    {
        var decodedItemId = DecodeIntSourceId(itemId, "Item");
        int? decodedWarehouseId = null;
        if (warehouseId.HasValue)
        {
            decodedWarehouseId = DecodeIntSourceId(warehouseId.Value, "Warehouse");
        }

        // Recalculate đầy đủ sẽ cần replay chứng từ theo kỳ. Step 10 chỉ đặt nền và xóa cost phát sinh sau ngày yêu cầu để tính lại thủ công sau.
        var layers = _dbContext.InventoryCostLayers.Where(x => x.ItemId == decodedItemId && x.DocumentDate >= fromDate.Date);
        var transactions = _dbContext.InventoryCostTransactions.Where(x => x.ItemId == decodedItemId && x.DocumentDate >= fromDate.Date);
        if (decodedWarehouseId.HasValue)
        {
            layers = layers.Where(x => x.WarehouseId == decodedWarehouseId.Value);
            transactions = transactions.Where(x => x.WarehouseId == decodedWarehouseId.Value);
        }

        _dbContext.InventoryCostTransactions.RemoveRange(await transactions.ToListAsync(cancellationToken));
        foreach (var layer in await layers.ToListAsync(cancellationToken))
        {
            layer.QuantityOut = 0;
            layer.RemainingQuantity = layer.QuantityIn;
            layer.RemainingCost = layer.TotalCost;
            layer.Status = "Open";
            layer.UpdatedAt = DateTime.UtcNow;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        await _auditLogService.LogAsync("InventoryCostRecalculate", $"Đặt lại giá vốn ItemId={decodedItemId}; WarehouseId={decodedWarehouseId}; From={fromDate:yyyy-MM-dd}", "inventory", cancellationToken);
    }

    public async Task<decimal> GetMovingAverageCostAsync(Guid itemId, Guid warehouseId, DateTime asOfDate, CancellationToken cancellationToken = default)
    {
        var decodedItemId = DecodeIntSourceId(itemId, "Item");
        var decodedWarehouseId = DecodeIntSourceId(warehouseId, "Warehouse");
        var layers = await GetOpenLayersAsync(decodedItemId, decodedWarehouseId, asOfDate.Date, cancellationToken);
        var totalQty = layers.Sum(x => x.RemainingQuantity);
        if (totalQty <= 0)
        {
            return 0;
        }

        return Math.Round(layers.Sum(x => x.RemainingCost) / totalQty, 6, MidpointRounding.AwayFromZero);
    }

    private async Task<InventoryCostResult> CalculateIssueCostInternalAsync(Guid stockIssueId, bool apply, CancellationToken cancellationToken)
    {
        var issueId = DecodeIntSourceId(stockIssueId, StockIssueSourceType);
        var issue = await _dbContext.StockIssues
            .Include(x => x.Details)
            .FirstOrDefaultAsync(x => x.Id == issueId, cancellationToken);

        if (issue is null)
        {
            return InventoryCostResult.Failed("Không tìm thấy phiếu xuất để tính giá vốn.");
        }

        if (issue.Details.Count == 0)
        {
            return InventoryCostResult.Failed("Phiếu xuất chưa có chi tiết để tính giá vốn.");
        }

        var configuredCostMethod = await GetConfiguredCostMethodAsync();
        if (configuredCostMethod is null)
        {
            return InventoryCostResult.Failed(
                "Phương pháp tính giá vốn đã lưu chưa được hỗ trợ. Chọn Bình quân tức thời hoặc FIFO trong Cài đặt module Kho.");
        }

        var results = new List<InventoryCostLineResult>();
        var warnings = new List<string>();
        var now = DateTime.UtcNow;

        foreach (var detail in issue.Details.OrderBy(x => x.LineNo))
        {
            if (detail.Quantity <= 0)
            {
                return InventoryCostResult.Failed($"Số lượng xuất phải lớn hơn 0 ở dòng {detail.LineNo}.");
            }

            var warehouseId = detail.WarehouseId ?? issue.WarehouseId;
            if (detail.ItemId <= 0 || warehouseId <= 0)
            {
                return InventoryCostResult.Failed($"Dòng {detail.LineNo} thiếu hàng hóa hoặc kho.");
            }

            var sourceDetailId = PostingSourceIds.FromInt32(detail.Id);
            var existingTransaction = await _dbContext.InventoryCostTransactions
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.SourceType == StockIssueSourceType && x.SourceDetailId == sourceDetailId, cancellationToken);
            if (existingTransaction is not null)
            {
                results.Add(new InventoryCostLineResult
                {
                    ItemId = existingTransaction.ItemId,
                    WarehouseId = existingTransaction.WarehouseId,
                    SourceDetailId = existingTransaction.SourceDetailId,
                    Quantity = existingTransaction.Quantity,
                    UnitCost = existingTransaction.UnitCost,
                    TotalCost = existingTransaction.TotalCost,
                    CostMethod = existingTransaction.CostMethod
                });
                continue;
            }

            var layers = await GetOpenLayersAsync(detail.ItemId, warehouseId, issue.IssueDate.Date, cancellationToken);
            var availableQty = layers.Sum(x => x.RemainingQuantity);
            decimal unitCost;
            decimal totalCost;

            if (availableQty >= detail.Quantity && availableQty > 0)
            {
                if (configuredCostMethod == nameof(InventoryCostMethod.FIFO))
                {
                    totalCost = CalculateFifoCost(layers, detail.Quantity);
                    unitCost = Math.Round(totalCost / detail.Quantity, 6, MidpointRounding.AwayFromZero);
                    if (apply)
                    {
                        ConsumeFifoLayers(layers, detail.Quantity, now);
                    }
                }
                else
                {
                    unitCost = Math.Round(layers.Sum(x => x.RemainingCost) / availableQty, 6, MidpointRounding.AwayFromZero);
                    totalCost = Math.Round(detail.Quantity * unitCost, 2, MidpointRounding.AwayFromZero);

                    if (apply)
                    {
                        ConsumeLayers(layers, detail.Quantity, totalCost, now);
                    }
                }
            }
            else
            {
                return InventoryCostResult.Failed($"Không đủ tồn giá vốn cho hàng hóa ItemId={detail.ItemId}, WarehouseId={warehouseId}. Tồn giá vốn hiện có {availableQty:n4}, số lượng xuất {detail.Quantity:n4}.");
            }

            if (apply)
            {
                detail.UnitPrice = Math.Round(unitCost, 2, MidpointRounding.AwayFromZero);
                detail.Amount = totalCost;
                _dbContext.InventoryCostTransactions.Add(new InventoryCostTransaction
                {
                    Id = Guid.NewGuid(),
                    ItemId = detail.ItemId,
                    WarehouseId = warehouseId,
                    SourceType = StockIssueSourceType,
                    SourceId = stockIssueId,
                    SourceDetailId = sourceDetailId,
                    DocumentNo = issue.IssueNo,
                    DocumentDate = issue.IssueDate.Date,
                    Quantity = detail.Quantity,
                    UnitCost = unitCost,
                    TotalCost = totalCost,
                    CostMethod = configuredCostMethod,
                    CreatedAt = now
                });
            }

            results.Add(new InventoryCostLineResult
            {
                ItemId = detail.ItemId,
                WarehouseId = warehouseId,
                SourceDetailId = sourceDetailId,
                Quantity = detail.Quantity,
                UnitCost = unitCost,
                TotalCost = totalCost,
                CostMethod = configuredCostMethod
            });
        }

        if (apply)
        {
            issue.TotalAmount = issue.Details.Sum(x => x.Amount);
            await _dbContext.SaveChangesAsync(cancellationToken);
            await _auditLogService.LogAsync("InventoryCostIssue", $"Áp dụng giá vốn phiếu xuất {issue.IssueNo}", "inventory", cancellationToken);
        }

        return InventoryCostResult.Succeeded(results, warnings);
    }

    private async Task<List<InventoryCostLayer>> GetOpenLayersAsync(int itemId, int warehouseId, DateTime asOfDate, CancellationToken cancellationToken)
    {
        return await _dbContext.InventoryCostLayers
            .Where(x => x.ItemId == itemId
                && x.WarehouseId == warehouseId
                && x.DocumentDate <= asOfDate
                && x.Status == "Open"
                && x.RemainingQuantity > 0)
            .OrderBy(x => x.DocumentDate)
            .ThenBy(x => x.CreatedAt)
            .ThenBy(x => x.Id)
            .ToListAsync(cancellationToken);
    }

    private async Task<string?> GetConfiguredCostMethodAsync()
    {
        if (_settingsService is null)
        {
            return MovingAverageMethod;
        }

        var configured = await _settingsService.GetValueAsync(
            "Module.inventory.costMethod",
            SettingScope.App,
            "inventory");
        var value = configured?.Value?.Trim();
        if (string.IsNullOrWhiteSpace(value)
            || string.Equals(value, "Bình quân tức thời", StringComparison.OrdinalIgnoreCase)
            || string.Equals(value, MovingAverageMethod, StringComparison.OrdinalIgnoreCase))
        {
            return MovingAverageMethod;
        }

        return string.Equals(value, "FIFO", StringComparison.OrdinalIgnoreCase)
            ? nameof(InventoryCostMethod.FIFO)
            : null;
    }

    private static decimal CalculateFifoCost(IReadOnlyList<InventoryCostLayer> layers, decimal quantity)
    {
        var remainingQuantity = quantity;
        var totalCost = 0m;
        foreach (var layer in layers)
        {
            if (remainingQuantity <= 0)
            {
                break;
            }

            var takeQuantity = Math.Min(layer.RemainingQuantity, remainingQuantity);
            var allocatedCost = takeQuantity == layer.RemainingQuantity
                ? layer.RemainingCost
                : Math.Round(layer.RemainingCost * (takeQuantity / layer.RemainingQuantity), 2, MidpointRounding.AwayFromZero);
            totalCost += allocatedCost;
            remainingQuantity -= takeQuantity;
        }

        return Math.Round(totalCost, 2, MidpointRounding.AwayFromZero);
    }

    private static void ConsumeFifoLayers(IReadOnlyList<InventoryCostLayer> layers, decimal quantity, DateTime now)
    {
        var remainingQuantity = quantity;
        foreach (var layer in layers)
        {
            if (remainingQuantity <= 0)
            {
                break;
            }

            var takeQuantity = Math.Min(layer.RemainingQuantity, remainingQuantity);
            var allocatedCost = takeQuantity == layer.RemainingQuantity
                ? layer.RemainingCost
                : Math.Round(layer.RemainingCost * (takeQuantity / layer.RemainingQuantity), 2, MidpointRounding.AwayFromZero);
            layer.QuantityOut += takeQuantity;
            layer.RemainingQuantity -= takeQuantity;
            layer.RemainingCost = Math.Max(0, layer.RemainingCost - allocatedCost);
            layer.Status = layer.RemainingQuantity <= 0 ? "Consumed" : "Open";
            layer.UpdatedAt = now;
            remainingQuantity -= takeQuantity;
        }
    }

    private static void ConsumeLayers(IReadOnlyList<InventoryCostLayer> layers, decimal quantity, decimal totalCost, DateTime now)
    {
        var remainingQty = quantity;
        var remainingCostToAllocate = totalCost;

        for (var i = 0; i < layers.Count && remainingQty > 0; i++)
        {
            var layer = layers[i];
            var takeQty = Math.Min(layer.RemainingQuantity, remainingQty);
            var allocatedCost = i == layers.Count - 1 || takeQty == remainingQty
                ? remainingCostToAllocate
                : Math.Round(totalCost * (takeQty / quantity), 2, MidpointRounding.AwayFromZero);

            layer.QuantityOut += takeQty;
            layer.RemainingQuantity -= takeQty;
            layer.RemainingCost = Math.Max(0, layer.RemainingCost - allocatedCost);
            layer.Status = layer.RemainingQuantity <= 0 ? "Consumed" : "Open";
            layer.UpdatedAt = now;

            remainingQty -= takeQty;
            remainingCostToAllocate -= allocatedCost;
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
}

