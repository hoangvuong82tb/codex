using ASMILE.BO.Application.DTOs;
using ASMILE.BO.Application.Interfaces;
using ASMILE.BO.Core.Entities;
using ASMILE.BO.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace ASMILE.BO.Infrastructure.Services;

public sealed class MenuCatalogService : IMenuCatalogService
{
    private readonly AppDbContext _dbContext;

    public MenuCatalogService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<MenuCategoryDto>> GetCategoriesAsync(CancellationToken cancellationToken = default)
    {
        await EnsureDefaultsAsync(cancellationToken);
        List<MenuCategoryDto> categories;
        try
        {
            categories = await _dbContext.MenuCategories.AsNoTracking()
                .Where(x => x.IsActive)
                .OrderBy(x => x.SortOrder).ThenBy(x => x.Name)
                .Select(x => new MenuCategoryDto
                {
                    Id = x.Id,
                    ParentCategoryId = x.ParentCategoryId,
                    ParentCategoryName = _dbContext.MenuCategories
                        .Where(parent => parent.Id == x.ParentCategoryId)
                        .Select(parent => parent.Name)
                        .FirstOrDefault(),
                    Code = x.Code,
                    Name = x.Name,
                    SortOrder = x.SortOrder
                })
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex) when (IsParentCategorySchemaMissing(ex))
        {
            categories = await _dbContext.MenuCategories.AsNoTracking()
                .Where(x => x.IsActive)
                .OrderBy(x => x.SortOrder).ThenBy(x => x.Name)
                .Select(x => new MenuCategoryDto
                {
                    Id = x.Id,
                    Code = x.Code,
                    Name = x.Name,
                    SortOrder = x.SortOrder
                })
                .ToListAsync(cancellationToken);
        }

        return OrderCategoriesHierarchically(categories);
    }

    public async Task<MenuCategoryDto> CreateOrUpdateCategoryAsync(CreateMenuCategoryRequest request, CancellationToken cancellationToken = default)
    {
        var code = request.Code.Trim().ToLowerInvariant();
        var name = request.Name.Trim();
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new InvalidOperationException("Mã nhóm món không được để trống.");
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new InvalidOperationException("Tên nhóm món không được để trống.");
        }

        var duplicated = await _dbContext.MenuCategories.AnyAsync(
            x => x.Code == code && (!request.Id.HasValue || x.Id != request.Id.Value),
            cancellationToken);
        if (duplicated)
        {
            throw new InvalidOperationException($"Mã nhóm món '{code}' đã tồn tại.");
        }

        if (request.ParentCategoryId.HasValue)
        {
            var parentExists = await _dbContext.MenuCategories.AnyAsync(
                x => x.Id == request.ParentCategoryId.Value,
                cancellationToken);
            if (!parentExists)
            {
                throw new InvalidOperationException("Nhóm cha không tồn tại.");
            }
        }

        var now = DateTime.UtcNow;
        MenuCategory entity;
        if (request.Id.HasValue)
        {
            entity = await _dbContext.MenuCategories.FirstOrDefaultAsync(x => x.Id == request.Id.Value, cancellationToken)
                ?? throw new InvalidOperationException("Nhóm món không tồn tại.");
        }
        else
        {
            entity = new MenuCategory
            {
                Id = Guid.NewGuid(),
                CreatedAt = now
            };
            _dbContext.MenuCategories.Add(entity);
        }

        if (request.ParentCategoryId == entity.Id)
        {
            throw new InvalidOperationException("Nhóm món không thể chọn chính nó làm nhóm cha.");
        }

        if (request.Id.HasValue && request.ParentCategoryId.HasValue)
        {
            await ValidateNoCategoryCycleAsync(entity.Id, request.ParentCategoryId.Value, cancellationToken);
        }

        entity.Code = code;
        entity.Name = name;
        entity.ParentCategoryId = request.ParentCategoryId;
        entity.SortOrder = request.SortOrder;
        entity.IsActive = request.IsActive;
        entity.UpdatedAt = now;

        await _dbContext.SaveChangesAsync(cancellationToken);
        return new MenuCategoryDto
        {
            Id = entity.Id,
            ParentCategoryId = entity.ParentCategoryId,
            ParentCategoryName = entity.ParentCategoryId.HasValue
                ? await _dbContext.MenuCategories.AsNoTracking()
                    .Where(x => x.Id == entity.ParentCategoryId.Value)
                    .Select(x => x.Name)
                    .FirstOrDefaultAsync(cancellationToken)
                : null,
            Code = entity.Code,
            Name = entity.Name,
            SortOrder = entity.SortOrder
        };
    }

    public async Task<IReadOnlyList<MenuItemDto>> GetMenuItemsAsync(
        Guid? categoryId = null,
        string? keyword = null,
        bool includeInactive = false,
        CancellationToken cancellationToken = default)
    {
        await EnsureDefaultsAsync(cancellationToken);
        var query = _dbContext.MenuItems.AsNoTracking().Include(x => x.MenuCategory).AsQueryable();
        if (!includeInactive)
        {
            query = query.Where(x => x.IsActive);
        }

        if (categoryId.HasValue)
        {
            query = query.Where(x => x.MenuCategoryId == categoryId.Value);
        }

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var term = keyword.Trim();
            query = query.Where(x => x.Code.Contains(term) || x.Name.Contains(term));
        }

        return await query
            .OrderBy(x => x.MenuCategory!.SortOrder).ThenBy(x => x.SortOrder).ThenBy(x => x.Name)
            .Select(MapMenuItemDto())
            .ToListAsync(cancellationToken);
    }

    public async Task<MenuItemDto?> GetMenuItemDetailAsync(Guid menuItemId, CancellationToken cancellationToken = default)
    {
        await EnsureDefaultsAsync(cancellationToken);
        return await _dbContext.MenuItems.AsNoTracking()
            .Include(x => x.MenuCategory)
            .Where(x => x.Id == menuItemId)
            .Select(MapMenuItemDto())
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<MenuItemDto> CreateOrUpdateMenuItemAsync(CreateMenuItemRequest request, CancellationToken cancellationToken = default)
    {
        await EnsureDefaultsAsync(cancellationToken);
        var code = request.Code.Trim().ToUpperInvariant();
        var name = request.Name.Trim();
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new InvalidOperationException("Mã món không được để trống.");
        }
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new InvalidOperationException("Tên món không được để trống.");
        }

        if (request.VatRate < 0 || request.VatRate > 1)
        {
            throw new InvalidOperationException("VAT rate phải trong khoảng 0..1.");
        }

        if (request.UnitPrice < 0)
        {
            throw new InvalidOperationException("Giá bán không được âm.");
        }

        if (!await _dbContext.MenuCategories.AnyAsync(x => x.Id == request.MenuCategoryId, cancellationToken))
        {
            throw new InvalidOperationException("Nhóm món không tồn tại.");
        }

        var duplicated = await _dbContext.MenuItems.AnyAsync(
            x => x.Code == code && (!request.Id.HasValue || x.Id != request.Id.Value),
            cancellationToken);
        if (duplicated)
        {
            throw new InvalidOperationException($"Mã món '{code}' đã tồn tại.");
        }

        var now = DateTime.UtcNow;
        MenuItem entity;
        if (request.Id.HasValue)
        {
            entity = await _dbContext.MenuItems.FirstOrDefaultAsync(x => x.Id == request.Id.Value, cancellationToken)
                ?? throw new InvalidOperationException("Món không tồn tại.");
        }
        else
        {
            entity = new MenuItem
            {
                Id = Guid.NewGuid(),
                CreatedAt = now
            };
            _dbContext.MenuItems.Add(entity);
        }

        entity.MenuCategoryId = request.MenuCategoryId;
        entity.Code = code;
        entity.Name = name;
        entity.UnitName = string.IsNullOrWhiteSpace(request.UnitName) ? "phần" : request.UnitName.Trim();
        entity.UnitPrice = request.UnitPrice;
        entity.VatRate = request.VatRate;
        entity.IsVatIncluded = request.IsVatIncluded;
        entity.HasRecipeManagement = request.HasRecipeManagement;
        entity.IsCombo = request.IsCombo;
        entity.IsHotelService = request.IsHotelService;
        entity.IsActive = request.IsActive;
        entity.SortOrder = request.SortOrder;
        entity.ItemId = request.ItemId;
        entity.Note = string.IsNullOrWhiteSpace(request.Note) ? null : request.Note.Trim();
        entity.PreparationArea = string.IsNullOrWhiteSpace(request.PreparationArea) ? null : request.PreparationArea.Trim();
        entity.ImagePath = request.ImagePath;
        entity.UpdatedAt = now;

        await _dbContext.SaveChangesAsync(cancellationToken);
        return await GetMenuItemDetailAsync(entity.Id, cancellationToken) ?? throw new InvalidOperationException("Không thể tải món vừa lưu.");
    }

    public async Task DeleteOrDeactivateCategoryAsync(Guid categoryId, bool softDeleteOnly = true, CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.MenuCategories.FirstOrDefaultAsync(x => x.Id == categoryId, cancellationToken)
            ?? throw new InvalidOperationException("Nhóm món không tồn tại.");
        var hasItems = await _dbContext.MenuItems.AnyAsync(x => x.MenuCategoryId == categoryId, cancellationToken);
        var hasChildren = await _dbContext.MenuCategories.AnyAsync(x => x.ParentCategoryId == categoryId, cancellationToken);

        if (softDeleteOnly || hasItems || hasChildren)
        {
            entity.IsActive = false;
            entity.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            _dbContext.MenuCategories.Remove(entity);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteOrDeactivateMenuItemAsync(Guid menuItemId, bool softDeleteOnly = true, CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.MenuItems.FirstOrDefaultAsync(x => x.Id == menuItemId, cancellationToken)
            ?? throw new InvalidOperationException("Món không tồn tại.");
        var hasRecipe = await _dbContext.MenuItemRecipes.AnyAsync(x => x.MenuItemId == menuItemId, cancellationToken);

        if (softDeleteOnly || hasRecipe)
        {
            entity.IsActive = false;
            entity.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            _dbContext.MenuItems.Remove(entity);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IngredientsLookupDto> GetIngredientsLookupAsync(int? warehouseId = null, CancellationToken cancellationToken = default)
    {
        var warehouses = await _dbContext.Warehouses.AsNoTracking()
            .Where(x => x.IsActive && x.IsConsumptionWarehouse && (!warehouseId.HasValue || x.Id == warehouseId.Value))
            .OrderBy(x => x.WarehouseCode)
            .Select(x => new WarehouseLookupDto
            {
                WarehouseId = x.Id,
                WarehouseCode = x.WarehouseCode,
                WarehouseName = x.WarehouseName,
                IsConsumptionWarehouse = x.IsConsumptionWarehouse
            })
            .ToListAsync(cancellationToken);
        var consumptionWarehouseIds = warehouses.Select(x => x.WarehouseId).ToArray();

        var items = await _dbContext.Items.AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.ItemCode)
            .Select(x => new
            {
                x.Id,
                x.ItemCode,
                x.ItemName,
                x.Unit
            })
            .ToListAsync(cancellationToken);

        var stockBalances = await _dbContext.StockBalances.AsNoTracking()
            .Where(x => consumptionWarehouseIds.Contains(x.WarehouseId))
            .GroupBy(x => x.ItemId)
            .Select(g => new
            {
                ItemId = g.Key,
                AvailableQty = g.Sum(x => x.ClosingQty),
                AvgCost = g.Where(x => x.AvgPrice > 0).Select(x => (decimal?)x.AvgPrice).FirstOrDefault() ?? 0m
            })
            .ToListAsync(cancellationToken);

        var costs = await _dbContext.InventoryCostLayers.AsNoTracking()
            .Where(x => x.UnitCost > 0 && consumptionWarehouseIds.Contains(x.WarehouseId))
            .OrderByDescending(x => x.DocumentDate)
            .ThenByDescending(x => x.CreatedAt)
            .Select(x => new { x.ItemId, x.UnitCost })
            .ToListAsync(cancellationToken);

        var stockMap = stockBalances.ToDictionary(x => x.ItemId, x => x);
        var costMap = costs.GroupBy(x => x.ItemId).ToDictionary(x => x.Key, x => x.First().UnitCost);
        return new IngredientsLookupDto
        {
            Warehouses = warehouses,
            Items = items.Select(x =>
            {
                stockMap.TryGetValue(x.Id, out var stock);
                var referenceCost = costMap.TryGetValue(x.Id, out var unitCost) && unitCost > 0
                    ? unitCost
                    : stock?.AvgCost ?? 0m;
                return new IngredientLookupDto
                {
                    ItemId = x.Id,
                    ItemCode = x.ItemCode,
                    ItemName = x.ItemName,
                    UnitName = x.Unit,
                    ReferenceUnitCost = referenceCost,
                    AvailableQuantity = stock?.AvailableQty ?? 0m
                };
            }).ToList()
        };
    }

    public async Task EnsureDefaultsAsync(CancellationToken cancellationToken = default)
    {
        if (await _dbContext.MenuCategories.AnyAsync(cancellationToken))
        {
            return;
        }

        var now = DateTime.UtcNow;
        var categories = new[]
        {
            new MenuCategory { Code = "breakfast", Name = "Đồ ăn sáng", SortOrder = 1, CreatedAt = now, UpdatedAt = now },
            new MenuCategory { Code = "coffee", Name = "Cà phê", SortOrder = 2, CreatedAt = now, UpdatedAt = now },
            new MenuCategory { Code = "tea-drink", Name = "Trà & nước giải khát", SortOrder = 3, CreatedAt = now, UpdatedAt = now },
            new MenuCategory { Code = "combo", Name = "Combo", SortOrder = 4, CreatedAt = now, UpdatedAt = now },
            new MenuCategory { Code = "hotel-service", Name = "Dịch vụ khách sạn", SortOrder = 5, CreatedAt = now, UpdatedAt = now },
            new MenuCategory { Code = "others", Name = "Món khác", SortOrder = 6, CreatedAt = now, UpdatedAt = now }
        };
        _dbContext.MenuCategories.AddRange(categories);

        MenuCategory Category(string code) => categories.First(x => x.Code == code);
        MenuItem New(string code, string name, decimal price, string cat, int sort, bool combo = false, bool hotel = false) => new()
        {
            Code = code,
            Name = name,
            UnitName = "phần",
            UnitPrice = price,
            VatRate = 0.08m,
            IsVatIncluded = true,
            SortOrder = sort,
            IsCombo = combo,
            IsHotelService = hotel,
            HasRecipeManagement = true,
            IsActive = true,
            MenuCategoryId = Category(cat).Id,
            CreatedAt = now,
            UpdatedAt = now
        };

        _dbContext.MenuItems.AddRange(
            New("BM_TRUNG", "Bánh mì trứng", 20000, "breakfast", 1),
            New("BM_BO", "Bánh mì bò", 30000, "breakfast", 2),
            New("PHO_BO", "Phở bò", 45000, "breakfast", 3),
            New("BUN_GA", "Bún gà", 40000, "breakfast", 4),
            New("XOI", "Xôi", 25000, "breakfast", 5),
            New("CF_DEN", "Cà phê đen", 20000, "coffee", 1),
            New("CF_SUA", "Cà phê sữa", 25000, "coffee", 2),
            New("BAC_XIU", "Bạc xỉu", 25000, "coffee", 3),
            New("CAPPU", "Cappuccino", 35000, "coffee", 4),
            New("LATTE", "Latte", 35000, "coffee", 5),
            New("TRA_DAO", "Trà đào", 25000, "tea-drink", 1),
            New("NUOC_CAM", "Nước cam", 30000, "tea-drink", 2),
            New("COMBO_BM_CF", "Combo bánh mì + cà phê", 36000, "combo", 1, combo: true)
        );

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private static Expression<Func<MenuItem, MenuItemDto>> MapMenuItemDto()
        => x => new MenuItemDto
        {
            Id = x.Id,
            MenuCategoryId = x.MenuCategoryId,
            CategoryName = x.MenuCategory!.Name,
            Code = x.Code,
            Name = x.Name,
            UnitName = x.UnitName,
            UnitPrice = x.UnitPrice,
            VatRate = x.VatRate,
            IsVatIncluded = x.IsVatIncluded,
            HasRecipeManagement = x.HasRecipeManagement,
            HasRecipe = x.Recipes.Any(r => r.IsActive),
            IsCombo = x.IsCombo,
            IsHotelService = x.IsHotelService,
            IsActive = x.IsActive,
            Note = x.Note,
            ImagePath = x.ImagePath,
            PreparationArea = x.PreparationArea,
            ImageUrl = x.ImageUrl
        };

    private async Task ValidateNoCategoryCycleAsync(Guid categoryId, Guid parentCategoryId, CancellationToken cancellationToken)
    {
        var currentParentId = parentCategoryId;
        while (true)
        {
            if (currentParentId == categoryId)
            {
                throw new InvalidOperationException("Không thể chọn nhóm con làm nhóm cha của chính nhóm này.");
            }

            var nextParentId = await _dbContext.MenuCategories.AsNoTracking()
                .Where(x => x.Id == currentParentId)
                .Select(x => x.ParentCategoryId)
                .FirstOrDefaultAsync(cancellationToken);

            if (!nextParentId.HasValue)
            {
                return;
            }

            currentParentId = nextParentId.Value;
        }
    }

    private static IReadOnlyList<MenuCategoryDto> OrderCategoriesHierarchically(IReadOnlyList<MenuCategoryDto> categories)
    {
        if (categories.Count <= 1)
        {
            return categories;
        }

        var byParent = categories
            .GroupBy(x => x.ParentCategoryId)
            .Select(group => (
                ParentId: group.Key,
                Children: group.OrderBy(x => x.SortOrder).ThenBy(x => x.Name, StringComparer.CurrentCultureIgnoreCase).ToList()
            ))
            .ToList();

        var ordered = new List<MenuCategoryDto>(categories.Count);
        AppendChildren(null, byParent, ordered);

        foreach (var category in categories)
        {
            if (ordered.All(x => x.Id != category.Id))
            {
                ordered.Add(category);
            }
        }

        return ordered;
    }

    private static void AppendChildren(
        Guid? parentId,
        IReadOnlyList<(Guid? ParentId, List<MenuCategoryDto> Children)> byParent,
        ICollection<MenuCategoryDto> ordered)
    {
        if (!byParent.Any(x => x.ParentId == parentId))
        {
            return;
        }

        var match = byParent.First(x => x.ParentId == parentId);

        foreach (var child in match.Children)
        {
            ordered.Add(child);
            AppendChildren(child.Id, byParent, ordered);
        }
    }

    private static bool IsParentCategorySchemaMissing(Exception ex)
    {
        var message = ex.GetBaseException().Message;
        return message.Contains("ParentCategoryId", StringComparison.OrdinalIgnoreCase)
               && (message.Contains("Invalid column name", StringComparison.OrdinalIgnoreCase)
                   || message.Contains("Invalid column", StringComparison.OrdinalIgnoreCase));
    }
}
