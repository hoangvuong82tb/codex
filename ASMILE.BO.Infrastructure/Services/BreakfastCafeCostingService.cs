using ASMILE.BO.Application.DTOs;
using ASMILE.BO.Application.Interfaces;
using ASMILE.BO.Core.Entities;
using ASMILE.BO.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ASMILE.BO.Infrastructure.Services;

public sealed class BreakfastCafeCostingService : IBreakfastCafeCostingService
{
    private readonly AppDbContext _dbContext;
    private readonly IInventoryAppService _inventoryService;

    public BreakfastCafeCostingService(AppDbContext dbContext, IInventoryAppService inventoryService)
    {
        _dbContext = dbContext;
        _inventoryService = inventoryService;
    }

    public async Task ConsumeRecipeAsync(Guid orderId, string? postedBy = null, CancellationToken cancellationToken = default)
    {
        var order = await _dbContext.BreakfastCafeOrders.AsNoTracking()
            .Include(x => x.Lines)
            .FirstOrDefaultAsync(x => x.Id == orderId, cancellationToken)
            ?? throw new InvalidOperationException("Không tìm thấy order POS để xuất kho định lượng.");

        if (!string.Equals(order.PaymentStatus, "Paid", StringComparison.OrdinalIgnoreCase)
            || !string.Equals(order.Status, "Paid", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        string issuePrefix = $"POS-{order.OrderNumber}";
        bool alreadyConsumed = await _dbContext.StockIssues.AsNoTracking()
            .AnyAsync(x => x.IssueNo.StartsWith(issuePrefix), cancellationToken);
        if (alreadyConsumed)
        {
            return;
        }

        List<RequiredRecipeLine> requiredRows = await BuildRequiredRowsAsync(order, cancellationToken);
        if (requiredRows.Count == 0)
        {
            return;
        }

        await EnsureFallbackCostLayersAsync(requiredRows, order.OrderDate.Date, cancellationToken);

        int lineNo = 1;
        foreach (var warehouseGroup in requiredRows.GroupBy(x => x.WarehouseId))
        {
            var detailRows = warehouseGroup
                .GroupBy(x => new { x.ItemId, x.Unit })
                .Select(group =>
                {
                    decimal quantity = group.Sum(x => x.Quantity);
                    decimal unitPrice = group.Where(x => x.UnitPrice > 0m).Select(x => x.UnitPrice).DefaultIfEmpty(0m).Average();
                    return new StockTransactionDetailDto
                    {
                        LineNo = lineNo++,
                        ItemId = group.Key.ItemId,
                        Unit = group.Key.Unit,
                        WarehouseId = warehouseGroup.Key,
                        Quantity = quantity,
                        UnitPrice = unitPrice,
                        Amount = Math.Round(quantity * unitPrice, 2, MidpointRounding.AwayFromZero)
                    };
                })
                .ToList();

            if (detailRows.Count == 0)
            {
                continue;
            }

            var issue = await _inventoryService.SaveIssueAsync(new StockIssueDto
            {
                IssueNo = $"{issuePrefix}-{warehouseGroup.Key}",
                IssueDate = order.OrderDate.Date,
                Description = $"Xuất kho định lượng POS {order.OrderNumber}",
                WarehouseId = warehouseGroup.Key,
                DepartmentCode = "POS",
                DebitAccountCode = string.Empty,
                CreditAccountCode = string.Empty,
                Details = detailRows
            }, cancellationToken);

            await _inventoryService.ApproveIssueAndPostAsync(issue.Id, postedBy ?? "pos", cancellationToken);
        }
    }

    private async Task<List<RequiredRecipeLine>> BuildRequiredRowsAsync(BreakfastCafeOrder order, CancellationToken cancellationToken)
    {
        var orderQuantities = order.Lines
            .GroupBy(x => x.MenuItemId)
            .ToDictionary(x => x.Key, x => x.Sum(line => line.Quantity));
        if (orderQuantities.Count == 0)
        {
            return new List<RequiredRecipeLine>();
        }

        Guid[] menuItemIds = orderQuantities.Keys.ToArray();
        List<MenuItemRecipe> recipes = await _dbContext.MenuItemRecipes.AsNoTracking()
            .Include(x => x.Lines)
            .Where(x => menuItemIds.Contains(x.MenuItemId) && x.IsActive && x.IsDefault)
            .ToListAsync(cancellationToken);

        int[] itemIds = recipes.SelectMany(x => x.Lines)
            .Where(x => x.ItemId.HasValue)
            .Select(x => x.ItemId!.Value)
            .Distinct()
            .ToArray();
        Dictionary<int, Item> items = await _dbContext.Items.AsNoTracking()
            .Where(x => itemIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, cancellationToken);
        int[] warehouseIds = recipes.SelectMany(x => x.Lines)
            .Where(x => x.DefaultWarehouseId.HasValue)
            .Select(x => x.DefaultWarehouseId!.Value)
            .Distinct()
            .ToArray();
        Dictionary<string, StockBalance> balances = await _dbContext.StockBalances.AsNoTracking()
            .Where(x => warehouseIds.Contains(x.WarehouseId) && itemIds.Contains(x.ItemId))
            .ToDictionaryAsync(x => $"{x.WarehouseId}_{x.ItemId}", cancellationToken);

        var rows = new List<RequiredRecipeLine>();
        foreach (MenuItemRecipe recipe in recipes)
        {
            if (!orderQuantities.TryGetValue(recipe.MenuItemId, out decimal orderQuantity))
            {
                continue;
            }

            foreach (MenuItemRecipeLine line in recipe.Lines.Where(x => x.ItemId.HasValue))
            {
                if (!line.DefaultWarehouseId.HasValue)
                {
                    throw new InvalidOperationException($"Dòng định lượng {line.ItemName} chưa chọn kho tiêu hao.");
                }

                int itemId = line.ItemId!.Value;
                int warehouseId = line.DefaultWarehouseId.Value;
                balances.TryGetValue($"{warehouseId}_{itemId}", out StockBalance? balance);
                items.TryGetValue(itemId, out Item? item);
                decimal quantity = line.Quantity * (1 + line.LossRate) * orderQuantity;
                rows.Add(new RequiredRecipeLine(
                    warehouseId,
                    itemId,
                    string.IsNullOrWhiteSpace(line.UnitName) ? item?.Unit ?? string.Empty : line.UnitName,
                    quantity,
                    balance?.AvgPrice ?? 0m));
            }
        }

        return rows;
    }

    private async Task EnsureFallbackCostLayersAsync(IReadOnlyCollection<RequiredRecipeLine> requiredRows, DateTime documentDate, CancellationToken cancellationToken)
    {
        var pairKeys = requiredRows
            .Select(x => $"{x.WarehouseId}_{x.ItemId}")
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        if (pairKeys.Length == 0)
        {
            return;
        }

        int[] warehouseIds = requiredRows.Select(x => x.WarehouseId).Distinct().ToArray();
        int[] itemIds = requiredRows.Select(x => x.ItemId).Distinct().ToArray();

        var openLayerKeys = await _dbContext.InventoryCostLayers.AsNoTracking()
            .Where(x => warehouseIds.Contains(x.WarehouseId)
                && itemIds.Contains(x.ItemId)
                && x.DocumentDate <= documentDate
                && x.Status == "Open"
                && x.RemainingQuantity > 0m)
            .Select(x => new { x.WarehouseId, x.ItemId })
            .ToListAsync(cancellationToken);
        var openLayerLookup = openLayerKeys
            .Select(x => $"{x.WarehouseId}_{x.ItemId}")
            .ToHashSet(StringComparer.Ordinal);

        string[] missingKeys = pairKeys
            .Where(x => !openLayerLookup.Contains(x))
            .ToArray();
        if (missingKeys.Length == 0)
        {
            return;
        }

        var balances = await _dbContext.StockBalances
            .Where(x => warehouseIds.Contains(x.WarehouseId) && itemIds.Contains(x.ItemId))
            .ToListAsync(cancellationToken);
        DateTime now = DateTime.UtcNow;

        foreach (var balance in balances)
        {
            string key = $"{balance.WarehouseId}_{balance.ItemId}";
            if (!missingKeys.Contains(key, StringComparer.Ordinal))
            {
                continue;
            }

            if (balance.ClosingQty <= 0m || balance.AvgPrice <= 0m)
            {
                continue;
            }

            decimal totalCost = balance.ClosingAmount > 0m
                ? balance.ClosingAmount
                : Math.Round(balance.ClosingQty * balance.AvgPrice, 2, MidpointRounding.AwayFromZero);

            _dbContext.InventoryCostLayers.Add(new InventoryCostLayer
            {
                Id = Guid.NewGuid(),
                CompanyId = balance.CompanyId,
                BranchId = balance.BranchId,
                ItemId = balance.ItemId,
                WarehouseId = balance.WarehouseId,
                SourceType = "OpeningBalance",
                SourceId = null,
                SourceDetailId = null,
                DocumentNo = $"AUTO-OPEN-{balance.WarehouseId}-{balance.ItemId}",
                DocumentDate = documentDate,
                QuantityIn = balance.ClosingQty,
                QuantityOut = 0m,
                UnitCost = Math.Round(balance.AvgPrice, 6, MidpointRounding.AwayFromZero),
                TotalCost = totalCost,
                RemainingQuantity = balance.ClosingQty,
                RemainingCost = totalCost,
                CostMethod = "MovingAverage",
                Status = "Open",
                CreatedAt = now,
                UpdatedAt = now
            });
        }

        if (_dbContext.ChangeTracker.HasChanges())
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    private sealed record RequiredRecipeLine(int WarehouseId, int ItemId, string Unit, decimal Quantity, decimal UnitPrice);
}
