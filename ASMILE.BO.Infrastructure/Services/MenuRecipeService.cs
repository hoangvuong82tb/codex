using ASMILE.BO.Application.DTOs;
using ASMILE.BO.Application.Interfaces;
using ASMILE.BO.Core.Entities;
using ASMILE.BO.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ASMILE.BO.Infrastructure.Services;

public sealed class MenuRecipeService : IMenuRecipeService
{
    private readonly AppDbContext _dbContext;

    public MenuRecipeService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<MenuRecipeDto>> GetRecipesByMenuItemAsync(Guid menuItemId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.MenuItemRecipes.AsNoTracking()
            .Include(x => x.Lines.OrderBy(l => l.LineNo))
            .Where(x => x.MenuItemId == menuItemId && x.IsActive)
            .OrderByDescending(x => x.IsDefault).ThenByDescending(x => x.UpdatedAt).ThenBy(x => x.Name)
            .Select(MapRecipeDto())
            .ToListAsync(cancellationToken);
    }

    public async Task<MenuRecipeDto?> GetRecipeAsync(Guid menuItemId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.MenuItemRecipes.AsNoTracking()
            .Include(x => x.Lines.OrderBy(l => l.LineNo))
            .Where(x => x.MenuItemId == menuItemId && x.IsActive)
            .OrderByDescending(x => x.IsDefault).ThenByDescending(x => x.UpdatedAt).ThenBy(x => x.Name)
            .Select(MapRecipeDto())
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<MenuRecipeDto> SaveRecipeAsync(SaveMenuRecipeRequest request, CancellationToken cancellationToken = default)
    {
        if (!await _dbContext.MenuItems.AnyAsync(x => x.Id == request.MenuItemId, cancellationToken))
        {
            throw new InvalidOperationException("Món không tồn tại.");
        }

        ValidateRequest(request);
        var now = DateTime.UtcNow;
        MenuItemRecipe recipe;
        if (request.RecipeId.HasValue)
        {
            recipe = await _dbContext.MenuItemRecipes.Include(x => x.Lines)
                .FirstOrDefaultAsync(x => x.Id == request.RecipeId.Value, cancellationToken)
                ?? throw new InvalidOperationException("Định lượng không tồn tại.");
            if (recipe.MenuItemId != request.MenuItemId)
            {
                throw new InvalidOperationException("Định lượng không thuộc món đang chọn.");
            }
        }
        else
        {
            var existingRecipe = await _dbContext.MenuItemRecipes.Include(x => x.Lines)
                .Where(x => x.MenuItemId == request.MenuItemId && x.IsActive && x.IsDefault == request.IsDefault)
                .OrderByDescending(x => x.UpdatedAt)
                .FirstOrDefaultAsync(cancellationToken);

            if (existingRecipe is null)
            {
                recipe = new MenuItemRecipe
                {
                    Id = Guid.NewGuid(),
                    MenuItemId = request.MenuItemId,
                    CreatedAt = now
                };
                _dbContext.MenuItemRecipes.Add(recipe);
            }
            else
            {
                recipe = existingRecipe;
            }
        }

        recipe.Name = string.IsNullOrWhiteSpace(request.Name) ? "Default" : request.Name.Trim();
        recipe.IsDefault = request.IsDefault;
        recipe.IsActive = true;
        recipe.UpdatedAt = now;

        if (recipe.IsDefault)
        {
            var otherDefaults = await _dbContext.MenuItemRecipes
                .Where(x => x.MenuItemId == recipe.MenuItemId && x.Id != recipe.Id && x.IsDefault)
                .ToListAsync(cancellationToken);

            foreach (var otherDefault in otherDefaults)
            {
                otherDefault.IsDefault = false;
                otherDefault.UpdatedAt = now;
            }
        }

        var persistedLines = recipe.Lines.Where(x => x.Id != Guid.Empty).ToList();
        if (persistedLines.Count > 0)
        {
            var tempLineNo = -1;
            foreach (var persistedLine in persistedLines.OrderBy(x => x.LineNo))
            {
                persistedLine.LineNo = tempLineNo--;
            }

            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        var existing = recipe.Lines.ToDictionary(x => x.Id);
        var existingByLineNo = recipe.Lines
            .GroupBy(x => Math.Abs(x.LineNo))
            .ToDictionary(x => x.Key, x => x.First());
        var keepIds = new HashSet<Guid>();
        var lineNo = 1;
        foreach (var line in request.Lines.OrderBy(x => x.LineNo))
        {
            MenuItemRecipeLine row;
            if (line.Id != Guid.Empty && existing.TryGetValue(line.Id, out var existingLine))
            {
                row = existingLine;
            }
            else if (line.Id == Guid.Empty && existingByLineNo.TryGetValue(line.LineNo, out var existingLineNo))
            {
                row = existingLineNo;
            }
            else
            {
                row = new MenuItemRecipeLine
                {
                    Id = Guid.NewGuid(),
                    MenuItemRecipeId = recipe.Id,
                    CreatedAt = now
                };
                recipe.Lines.Add(row);
                _dbContext.MenuItemRecipeLines.Add(row);
            }

            row.LineNo = lineNo++;
            row.ItemId = line.ItemId;
            row.ChildMenuItemId = line.ChildMenuItemId;
            row.DefaultWarehouseId = line.DefaultWarehouseId;
            row.ItemCode = line.ItemCode?.Trim() ?? string.Empty;
            row.ItemName = line.ItemName?.Trim() ?? string.Empty;
            row.UnitName = line.UnitName?.Trim() ?? string.Empty;
            row.Quantity = line.Quantity;
            row.LossRate = line.LossRate;
            row.IsRequired = line.IsRequired;
            row.Note = string.IsNullOrWhiteSpace(line.Note) ? null : line.Note.Trim();
            row.UpdatedAt = now;
            keepIds.Add(row.Id);
        }

        foreach (var remove in recipe.Lines.Where(x => !keepIds.Contains(x.Id)).ToList())
        {
            _dbContext.MenuItemRecipeLines.Remove(remove);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return await GetRecipeByIdAsync(recipe.Id, cancellationToken)
            ?? throw new InvalidOperationException("Không thể tải định lượng đã lưu.");
    }

    public async Task<IReadOnlyList<string>> ValidateRecipeAsync(Guid menuItemId, CancellationToken cancellationToken = default)
    {
        var warnings = new List<string>();
        var preview = await PreviewCostAsync(menuItemId, null, cancellationToken);
        warnings.AddRange(preview.Warnings);
        if (preview.Lines.Count == 0)
        {
            warnings.Add("Món chưa có định lượng.");
        }

        return warnings.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
    }

    public async Task<IReadOnlyList<SubstituteIngredientDto>> GetSubstitutesAsync(Guid recipeLineId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.RecipeIngredientSubstitutes.AsNoTracking()
            .Include(x => x.InventoryItem)
            .Include(x => x.SubstituteInventoryItem)
            .Where(x => x.RecipeLineId == recipeLineId && !x.IsDeleted)
            .OrderBy(x => x.Priority)
            .ThenBy(x => x.SubstituteInventoryItem!.ItemName)
            .Select(x => new SubstituteIngredientDto
            {
                Id = x.Id,
                RecipeLineId = x.RecipeLineId,
                InventoryItemId = x.InventoryItemId,
                InventoryItemCode = x.InventoryItem != null ? x.InventoryItem.ItemCode : string.Empty,
                InventoryItemName = x.InventoryItem != null ? x.InventoryItem.ItemName : string.Empty,
                SubstituteInventoryItemId = x.SubstituteInventoryItemId,
                SubstituteItemCode = x.SubstituteInventoryItem != null ? x.SubstituteInventoryItem.ItemCode : string.Empty,
                SubstituteItemName = x.SubstituteInventoryItem != null ? x.SubstituteInventoryItem.ItemName : string.Empty,
                SubstituteUnitName = x.SubstituteInventoryItem != null ? x.SubstituteInventoryItem.Unit : string.Empty,
                ConversionRate = x.ConversionRate,
                Priority = x.Priority,
                AutoReplace = x.AutoReplace,
                Notes = x.Notes,
                CreatedAt = x.CreatedAt
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<SubstituteIngredientDto> CreateSubstituteAsync(CreateSubstituteIngredientRequest request, CancellationToken cancellationToken = default)
    {
        var errors = await ValidateSubstituteAsync(request, cancellationToken);
        if (errors.Count > 0)
        {
            throw new InvalidOperationException(string.Join(Environment.NewLine, errors));
        }

        var line = await _dbContext.MenuItemRecipeLines.AsNoTracking()
            .FirstAsync(x => x.Id == request.RecipeLineId, cancellationToken);
        var now = DateTime.UtcNow;
        var row = new RecipeIngredientSubstitute
        {
            Id = Guid.NewGuid(),
            CompanyId = line.CompanyId,
            BranchId = line.BranchId,
            RecipeLineId = request.RecipeLineId,
            InventoryItemId = line.ItemId!.Value,
            SubstituteInventoryItemId = request.SubstituteInventoryItemId,
            ConversionRate = request.ConversionRate,
            Priority = request.Priority,
            AutoReplace = request.AutoReplace,
            Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim(),
            CreatedAt = now,
            CreatedBy = string.IsNullOrWhiteSpace(request.UserName) ? "system" : request.UserName.Trim()
        };

        _dbContext.RecipeIngredientSubstitutes.Add(row);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return (await GetSubstitutesAsync(request.RecipeLineId, cancellationToken)).First(x => x.Id == row.Id);
    }

    public async Task<SubstituteIngredientDto> UpdateSubstituteAsync(UpdateSubstituteIngredientRequest request, CancellationToken cancellationToken = default)
    {
        if (request.ConversionRate <= 0)
        {
            throw new InvalidOperationException("Tỷ lệ quy đổi phải lớn hơn 0.");
        }

        if (request.Priority < 1)
        {
            throw new InvalidOperationException("Ưu tiên phải lớn hơn hoặc bằng 1.");
        }

        var row = await _dbContext.RecipeIngredientSubstitutes
            .FirstOrDefaultAsync(x => x.Id == request.Id && !x.IsDeleted, cancellationToken)
            ?? throw new InvalidOperationException("Nguyên liệu thay thế không tồn tại.");

        row.ConversionRate = request.ConversionRate;
        row.Priority = request.Priority;
        row.AutoReplace = request.AutoReplace;
        row.Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim();
        row.ModifiedAt = DateTime.UtcNow;
        row.ModifiedBy = string.IsNullOrWhiteSpace(request.UserName) ? "system" : request.UserName.Trim();
        await _dbContext.SaveChangesAsync(cancellationToken);
        return (await GetSubstitutesAsync(row.RecipeLineId, cancellationToken)).First(x => x.Id == row.Id);
    }

    public async Task DeleteSubstituteAsync(Guid substituteId, string? userName = null, CancellationToken cancellationToken = default)
    {
        var row = await _dbContext.RecipeIngredientSubstitutes
            .FirstOrDefaultAsync(x => x.Id == substituteId && !x.IsDeleted, cancellationToken);
        if (row is null)
        {
            return;
        }

        row.IsDeleted = true;
        row.ModifiedAt = DateTime.UtcNow;
        row.ModifiedBy = string.IsNullOrWhiteSpace(userName) ? "system" : userName.Trim();
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<string>> ValidateSubstituteAsync(CreateSubstituteIngredientRequest request, CancellationToken cancellationToken = default)
    {
        var errors = new List<string>();
        var line = await _dbContext.MenuItemRecipeLines.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == request.RecipeLineId, cancellationToken);
        if (line is null)
        {
            errors.Add("Dòng định lượng không tồn tại.");
            return errors;
        }

        if (line.ItemId is null)
        {
            errors.Add("Chỉ hỗ trợ nguyên liệu kho, không hỗ trợ dòng tham chiếu món.");
            return errors;
        }

        if (request.SubstituteInventoryItemId <= 0
            || !await _dbContext.Items.AsNoTracking().AnyAsync(x => x.Id == request.SubstituteInventoryItemId && x.IsActive, cancellationToken))
        {
            errors.Add("Nguyên liệu thay thế không tồn tại hoặc đã ngưng sử dụng.");
        }

        if (request.SubstituteInventoryItemId == line.ItemId.Value)
        {
            errors.Add("Không được chọn chính nguyên liệu đang định lượng làm nguyên liệu thay thế.");
        }

        if (request.ConversionRate <= 0)
        {
            errors.Add("Tỷ lệ quy đổi phải lớn hơn 0.");
        }

        if (request.Priority < 1)
        {
            errors.Add("Ưu tiên phải lớn hơn hoặc bằng 1.");
        }

        var isDuplicate = await _dbContext.RecipeIngredientSubstitutes.AsNoTracking()
            .AnyAsync(x => x.RecipeLineId == request.RecipeLineId
                           && x.SubstituteInventoryItemId == request.SubstituteInventoryItemId
                           && !x.IsDeleted, cancellationToken);
        if (isDuplicate)
        {
            errors.Add("Nguyên liệu thay thế này đã tồn tại trong dòng định lượng.");
        }

        return errors;
    }

    public async Task<MenuCostPreviewDto> PreviewCostAsync(Guid menuItemId, Guid? warehouseId = null, CancellationToken cancellationToken = default)
    {
        var menu = await _dbContext.MenuItems.AsNoTracking().FirstOrDefaultAsync(x => x.Id == menuItemId, cancellationToken)
            ?? throw new InvalidOperationException("Món không tồn tại.");
        var recipe = await _dbContext.MenuItemRecipes.AsNoTracking()
            .Include(x => x.Lines.OrderBy(l => l.LineNo))
            .Where(x => x.MenuItemId == menuItemId && x.IsActive)
            .OrderByDescending(x => x.IsDefault).ThenByDescending(x => x.UpdatedAt).ThenBy(x => x.Name)
            .FirstOrDefaultAsync(cancellationToken);

        if (recipe is null)
        {
            return new MenuCostPreviewDto
            {
                MenuItemId = menu.Id,
                MenuItemCode = menu.Code,
                MenuItemName = menu.Name,
                SellingPrice = menu.UnitPrice,
                VatRate = menu.VatRate,
                EstimatedCost = 0,
                MarginAmount = menu.UnitPrice,
                MarginRate = menu.UnitPrice > 0 ? 1m : 0m,
                Warnings = ["Món chưa có định lượng."]
            };
        }

        var itemIds = recipe.Lines.Where(x => x.ItemId.HasValue).Select(x => x.ItemId!.Value).Distinct().ToArray();
        var items = await _dbContext.Items.AsNoTracking()
            .Where(x => itemIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, cancellationToken);

        var stockRows = await _dbContext.StockBalances.AsNoTracking()
            .Where(x => itemIds.Contains(x.ItemId))
            .Select(x => new { x.ItemId, x.WarehouseId, x.ClosingQty, x.AvgPrice })
            .ToListAsync(cancellationToken);

        var stockByItemWarehouse = stockRows
            .GroupBy(x => (x.ItemId, x.WarehouseId))
            .ToDictionary(
                x => x.Key,
                x => new
                {
                    Qty = x.Sum(v => v.ClosingQty),
                    AvgCost = x.Where(v => v.AvgPrice > 0).Select(v => v.AvgPrice).FirstOrDefault()
                });

        var stockByItem = stockRows
            .GroupBy(x => x.ItemId)
            .ToDictionary(
                x => x.Key,
                x => new
                {
                    Qty = x.Sum(v => v.ClosingQty),
                    AvgCost = x.Where(v => v.AvgPrice > 0).Select(v => v.AvgPrice).FirstOrDefault()
                });

        var costRows = await _dbContext.InventoryCostLayers.AsNoTracking()
            .Where(x => itemIds.Contains(x.ItemId) && x.UnitCost > 0)
            .OrderByDescending(x => x.DocumentDate)
            .ThenByDescending(x => x.CreatedAt)
            .Select(x => new { x.ItemId, x.WarehouseId, x.UnitCost })
            .ToListAsync(cancellationToken);

        var costByItemWarehouse = costRows
            .GroupBy(x => (x.ItemId, x.WarehouseId))
            .ToDictionary(x => x.Key, x => x.First().UnitCost);

        var costByItem = costRows
            .GroupBy(x => x.ItemId)
            .ToDictionary(x => x.Key, x => x.First().UnitCost);

        var lines = new List<MenuCostPreviewLineDto>();
        var warnings = new List<string>();
        foreach (var line in recipe.Lines.OrderBy(x => x.LineNo))
        {
            if (line.ItemId is null && line.ChildMenuItemId is null)
            {
                warnings.Add($"Dòng {line.LineNo} thiếu nguyên vật liệu.");
                continue;
            }

            if (line.ItemId is null && line.ChildMenuItemId.HasValue)
            {
                lines.Add(new MenuCostPreviewLineDto
                {
                    LineNo = line.LineNo,
                    ChildMenuItemId = line.ChildMenuItemId,
                    ItemCode = "MENU_REF",
                    ItemName = "Món thành phần (skeleton)",
                    Quantity = line.Quantity,
                    LossRate = line.LossRate,
                    EffectiveQuantity = line.Quantity * (1 + line.LossRate),
                    Warning = "Dòng tham chiếu món thành phần chưa tính giá vốn tự động.",
                    MissingCost = true
                });
                warnings.Add($"Dòng {line.LineNo} là tham chiếu món thành phần, chưa có cost tự động.");
                continue;
            }

            var itemId = line.ItemId!.Value;
            items.TryGetValue(itemId, out var item);
            var hasWarehouse = line.DefaultWarehouseId.HasValue;
            var lineWarehouseId = line.DefaultWarehouseId.GetValueOrDefault();
            var stockInfo = hasWarehouse && stockByItemWarehouse.TryGetValue((itemId, lineWarehouseId), out var warehouseStock)
                ? warehouseStock
                : stockByItem.GetValueOrDefault(itemId);
            var unitCost = hasWarehouse && costByItemWarehouse.TryGetValue((itemId, lineWarehouseId), out var warehouseLayerCost)
                ? warehouseLayerCost
                : costByItem.TryGetValue(itemId, out var layerCost) && layerCost > 0
                    ? layerCost
                    : stockInfo?.AvgCost ?? 0m;
            var effectiveQty = line.Quantity * (1 + line.LossRate);
            var lineCost = Math.Round(effectiveQty * unitCost, 2, MidpointRounding.AwayFromZero);
            var missingCost = unitCost <= 0;
            var insufficient = (stockInfo?.Qty ?? 0) < effectiveQty;
            string? warning = null;
            if (missingCost)
            {
                warning = "Nguyên liệu chưa có giá vốn.";
                warnings.Add($"{item?.ItemName ?? line.ItemName} chưa có giá vốn.");
            }
            if (insufficient)
            {
                warning = "Tồn kho không đủ theo định mức.";
                warnings.Add($"{item?.ItemName ?? line.ItemName} không đủ tồn kho.");
            }

            lines.Add(new MenuCostPreviewLineDto
            {
                LineNo = line.LineNo,
                ItemId = itemId,
                ItemCode = item?.ItemCode ?? line.ItemCode,
                ItemName = item?.ItemName ?? line.ItemName,
                Quantity = line.Quantity,
                LossRate = line.LossRate,
                EffectiveQuantity = effectiveQty,
                UnitCost = unitCost,
                LineCost = lineCost,
                AvailableQuantity = stockInfo?.Qty ?? 0m,
                MissingCost = missingCost,
                InsufficientStock = insufficient,
                Warning = warning
            });
        }

        var total = lines.Sum(x => x.LineCost);
        var marginAmount = menu.UnitPrice - total;
        if (menu.UnitPrice < total)
        {
            warnings.Add("Giá bán thấp hơn giá vốn.");
        }

        return new MenuCostPreviewDto
        {
            MenuItemId = menu.Id,
            MenuItemCode = menu.Code,
            MenuItemName = menu.Name,
            SellingPrice = menu.UnitPrice,
            VatRate = menu.VatRate,
            EstimatedCost = total,
            MarginAmount = marginAmount,
            MarginRate = menu.UnitPrice == 0 ? 0 : marginAmount / menu.UnitPrice,
            Lines = lines,
            Warnings = warnings.Distinct(StringComparer.OrdinalIgnoreCase).ToArray()
        };
    }

    private async Task<MenuRecipeDto?> GetRecipeByIdAsync(Guid recipeId, CancellationToken cancellationToken)
    {
        return await _dbContext.MenuItemRecipes.AsNoTracking()
            .Include(x => x.Lines.OrderBy(l => l.LineNo))
            .Where(x => x.Id == recipeId && x.IsActive)
            .Select(MapRecipeDto())
            .FirstOrDefaultAsync(cancellationToken);
    }

    private static void ValidateRequest(SaveMenuRecipeRequest request)
    {
        foreach (var line in request.Lines)
        {
            if (line.Quantity <= 0)
            {
                throw new InvalidOperationException("Số lượng định mức phải lớn hơn 0.");
            }

            if (line.LossRate < 0 || line.LossRate > 1)
            {
                throw new InvalidOperationException("Tỷ lệ hao hụt phải trong khoảng 0..1.");
            }

            var hasItem = line.ItemId.HasValue;
            var hasChild = line.ChildMenuItemId.HasValue;
            if (hasItem == hasChild)
            {
                throw new InvalidOperationException("Mỗi dòng phải có đúng một trong ItemId hoặc ChildMenuItemId.");
            }
        }
    }

    private static System.Linq.Expressions.Expression<Func<MenuItemRecipe, MenuRecipeDto>> MapRecipeDto()
        => x => new MenuRecipeDto
        {
            Id = x.Id,
            MenuItemId = x.MenuItemId,
            Name = x.Name,
            IsDefault = x.IsDefault,
            Lines = x.Lines.Select(l => new MenuRecipeLineDto
            {
                Id = l.Id,
                LineNo = l.LineNo,
                ItemId = l.ItemId,
                ChildMenuItemId = l.ChildMenuItemId,
                DefaultWarehouseId = l.DefaultWarehouseId,
                ItemCode = l.ItemCode,
                ItemName = l.ItemName,
                UnitName = l.UnitName,
                Quantity = l.Quantity,
                LossRate = l.LossRate,
                IsRequired = l.IsRequired,
                Note = l.Note
            }).ToList()
        };
}
