using ASMILE.BO.Application.DTOs;
using ASMILE.BO.Core.Entities;
using ASMILE.BO.Infrastructure.Services;
using ASMILE.BO.Tests.Unit.TestSupport;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace ASMILE.BO.Tests.Unit;

public sealed class MenuRecipeModuleTests
{
    [Fact]
    public async Task CreateOrUpdateMenuItemAsync_ShouldCreateMenuItem()
    {
        await using var db = TestDbContextFactory.CreateContext();
        var catalog = new MenuCatalogService(db);
        var category = (await catalog.GetCategoriesAsync()).First();

        var saved = await catalog.CreateOrUpdateMenuItemAsync(new CreateMenuItemRequest
        {
            MenuCategoryId = category.Id,
            Code = "UT_MENU_01",
            Name = "Unit test menu",
            UnitName = "phần",
            UnitPrice = 100000,
            VatRate = 0.08m,
            HasRecipeManagement = true
        });

        saved.Code.Should().Be("UT_MENU_01");
        (await db.MenuItems.CountAsync(x => x.Code == "UT_MENU_01")).Should().Be(1);
    }

    [Fact]
    public async Task SaveRecipeAsync_ShouldRejectLineQuantityLessOrEqualZero()
    {
        await using var db = TestDbContextFactory.CreateContext();
        var catalog = new MenuCatalogService(db);
        var recipeService = new MenuRecipeService(db);
        var category = (await catalog.GetCategoriesAsync()).First();
        var menu = await catalog.CreateOrUpdateMenuItemAsync(new CreateMenuItemRequest
        {
            MenuCategoryId = category.Id,
            Code = "UT_MENU_02",
            Name = "Unit test menu 2",
            UnitName = "phần",
            UnitPrice = 50000,
            VatRate = 0.08m,
            HasRecipeManagement = true
        });

        var act = () => recipeService.SaveRecipeAsync(new SaveMenuRecipeRequest
        {
            MenuItemId = menu.Id,
            Lines =
            [
                new MenuRecipeLineDto
                {
                    LineNo = 1,
                    ItemId = 1,
                    Quantity = 0,
                    LossRate = 0
                }
            ]
        });

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task SaveRecipeAsync_WithoutRecipeId_ShouldReuseExistingDefaultRecipe()
    {
        await using var db = TestDbContextFactory.CreateContext();
        var catalog = new MenuCatalogService(db);
        var recipeService = new MenuRecipeService(db);
        var category = (await catalog.GetCategoriesAsync()).First();
        db.Items.AddRange(
            new Item { ItemCode = "UT_RECIPE_ITEM_A", ItemName = "Ingredient A", Unit = "kg", GroupName = "UT", IsActive = true },
            new Item { ItemCode = "UT_RECIPE_ITEM_B", ItemName = "Ingredient B", Unit = "kg", GroupName = "UT", IsActive = true });
        await db.SaveChangesAsync();
        var itemA = await db.Items.FirstAsync(x => x.ItemCode == "UT_RECIPE_ITEM_A");
        var itemB = await db.Items.FirstAsync(x => x.ItemCode == "UT_RECIPE_ITEM_B");

        var menu = await catalog.CreateOrUpdateMenuItemAsync(new CreateMenuItemRequest
        {
            MenuCategoryId = category.Id,
            Code = "UT_MENU_REUSE_RECIPE",
            Name = "Unit test reuse recipe",
            UnitName = "phan",
            UnitPrice = 70000,
            VatRate = 0.08m,
            HasRecipeManagement = true
        });

        var firstSave = await recipeService.SaveRecipeAsync(new SaveMenuRecipeRequest
        {
            MenuItemId = menu.Id,
            Lines =
            [
                new MenuRecipeLineDto
                {
                    LineNo = 1,
                    ItemId = itemA.Id,
                    ItemCode = itemA.ItemCode,
                    ItemName = itemA.ItemName,
                    UnitName = itemA.Unit,
                    Quantity = 1,
                    LossRate = 0
                }
            ]
        });

        var secondSave = await recipeService.SaveRecipeAsync(new SaveMenuRecipeRequest
        {
            MenuItemId = menu.Id,
            Lines =
            [
                new MenuRecipeLineDto
                {
                    LineNo = 1,
                    ItemId = itemB.Id,
                    ItemCode = itemB.ItemCode,
                    ItemName = itemB.ItemName,
                    UnitName = itemB.Unit,
                    Quantity = 2,
                    LossRate = 0
                }
            ]
        });

        secondSave.Id.Should().Be(firstSave.Id);
        secondSave.Lines.Should().ContainSingle(x => x.ItemId == itemB.Id && x.Quantity == 2);
        (await db.MenuItemRecipes.CountAsync(x => x.MenuItemId == menu.Id && x.IsActive)).Should().Be(1);
        (await db.MenuItemRecipes.CountAsync(x => x.MenuItemId == menu.Id && x.IsDefault)).Should().Be(1);
    }

    [Fact]
    public async Task SaveRecipeAsync_ShouldRemoveRecipeLineAndReindex()
    {
        await using var db = TestDbContextFactory.CreateContext();
        var catalog = new MenuCatalogService(db);
        var recipeService = new MenuRecipeService(db);
        var category = (await catalog.GetCategoriesAsync()).First();
        db.Items.AddRange(
            new Item { ItemCode = "UT_DELETE_ITEM_A", ItemName = "Delete ingredient A", Unit = "kg", GroupName = "UT", IsActive = true },
            new Item { ItemCode = "UT_DELETE_ITEM_B", ItemName = "Delete ingredient B", Unit = "kg", GroupName = "UT", IsActive = true });
        await db.SaveChangesAsync();
        var itemA = await db.Items.FirstAsync(x => x.ItemCode == "UT_DELETE_ITEM_A");
        var itemB = await db.Items.FirstAsync(x => x.ItemCode == "UT_DELETE_ITEM_B");

        var menu = await catalog.CreateOrUpdateMenuItemAsync(new CreateMenuItemRequest
        {
            MenuCategoryId = category.Id,
            Code = "UT_MENU_DELETE_RECIPE_LINE",
            Name = "Unit test delete recipe line",
            UnitName = "phan",
            UnitPrice = 70000,
            VatRate = 0.08m,
            HasRecipeManagement = true
        });

        var firstSave = await recipeService.SaveRecipeAsync(new SaveMenuRecipeRequest
        {
            MenuItemId = menu.Id,
            Lines =
            [
                new MenuRecipeLineDto { LineNo = 1, ItemId = itemA.Id, ItemCode = itemA.ItemCode, ItemName = itemA.ItemName, UnitName = itemA.Unit, Quantity = 1, LossRate = 0 },
                new MenuRecipeLineDto { LineNo = 2, ItemId = itemB.Id, ItemCode = itemB.ItemCode, ItemName = itemB.ItemName, UnitName = itemB.Unit, Quantity = 2, LossRate = 0 }
            ]
        });

        var keptLine = firstSave.Lines.Single(x => x.ItemId == itemB.Id);
        var secondSave = await recipeService.SaveRecipeAsync(new SaveMenuRecipeRequest
        {
            RecipeId = firstSave.Id,
            MenuItemId = menu.Id,
            Lines =
            [
                new MenuRecipeLineDto
                {
                    Id = keptLine.Id,
                    LineNo = 1,
                    ItemId = keptLine.ItemId,
                    ItemCode = keptLine.ItemCode,
                    ItemName = keptLine.ItemName,
                    UnitName = keptLine.UnitName,
                    Quantity = keptLine.Quantity,
                    LossRate = keptLine.LossRate
                }
            ]
        });

        secondSave.Lines.Should().ContainSingle();
        secondSave.Lines[0].ItemId.Should().Be(itemB.Id);
        secondSave.Lines[0].LineNo.Should().Be(1);
        (await db.MenuItemRecipeLines.CountAsync(x => x.MenuItemRecipeId == firstSave.Id)).Should().Be(1);
    }

    [Fact]
    public async Task CreateSubstituteAsync_ShouldRejectSelfAndDuplicate_ThenAllowAfterSoftDelete()
    {
        await using var db = TestDbContextFactory.CreateContext();
        var catalog = new MenuCatalogService(db);
        var recipeService = new MenuRecipeService(db);
        var category = (await catalog.GetCategoriesAsync()).First();
        db.Items.AddRange(
            new Item { ItemCode = "UT_SUB_MAIN", ItemName = "Main ingredient", Unit = "kg", GroupName = "UT", IsActive = true },
            new Item { ItemCode = "UT_SUB_ALT", ItemName = "Substitute ingredient", Unit = "kg", GroupName = "UT", IsActive = true });
        await db.SaveChangesAsync();
        var main = await db.Items.FirstAsync(x => x.ItemCode == "UT_SUB_MAIN");
        var substitute = await db.Items.FirstAsync(x => x.ItemCode == "UT_SUB_ALT");

        var menu = await catalog.CreateOrUpdateMenuItemAsync(new CreateMenuItemRequest
        {
            MenuCategoryId = category.Id,
            Code = "UT_MENU_SUBSTITUTE",
            Name = "Unit test substitute",
            UnitName = "phan",
            UnitPrice = 70000,
            VatRate = 0.08m,
            HasRecipeManagement = true
        });

        var recipe = await recipeService.SaveRecipeAsync(new SaveMenuRecipeRequest
        {
            MenuItemId = menu.Id,
            Lines =
            [
                new MenuRecipeLineDto
                {
                    LineNo = 1,
                    ItemId = main.Id,
                    ItemCode = main.ItemCode,
                    ItemName = main.ItemName,
                    UnitName = main.Unit,
                    Quantity = 1,
                    LossRate = 0
                }
            ]
        });
        var lineId = recipe.Lines.Single().Id;

        var self = () => recipeService.CreateSubstituteAsync(new CreateSubstituteIngredientRequest
        {
            RecipeLineId = lineId,
            SubstituteInventoryItemId = main.Id,
            ConversionRate = 1m,
            Priority = 1,
            AutoReplace = true
        });
        await self.Should().ThrowAsync<InvalidOperationException>();

        var created = await recipeService.CreateSubstituteAsync(new CreateSubstituteIngredientRequest
        {
            RecipeLineId = lineId,
            SubstituteInventoryItemId = substitute.Id,
            ConversionRate = 1.25m,
            Priority = 2,
            AutoReplace = true,
            Notes = "Use when main is out of stock"
        });

        created.SubstituteInventoryItemId.Should().Be(substitute.Id);
        created.ConversionRate.Should().Be(1.25m);
        (await recipeService.GetSubstitutesAsync(lineId)).Should().ContainSingle();

        var duplicate = () => recipeService.CreateSubstituteAsync(new CreateSubstituteIngredientRequest
        {
            RecipeLineId = lineId,
            SubstituteInventoryItemId = substitute.Id,
            ConversionRate = 1m,
            Priority = 3,
            AutoReplace = false
        });
        await duplicate.Should().ThrowAsync<InvalidOperationException>();

        await recipeService.DeleteSubstituteAsync(created.Id, "unit-test");
        (await recipeService.GetSubstitutesAsync(lineId)).Should().BeEmpty();
        (await db.RecipeIngredientSubstitutes.CountAsync(x => x.Id == created.Id && x.IsDeleted)).Should().Be(1);

        var recreated = await recipeService.CreateSubstituteAsync(new CreateSubstituteIngredientRequest
        {
            RecipeLineId = lineId,
            SubstituteInventoryItemId = substitute.Id,
            ConversionRate = 1m,
            Priority = 1,
            AutoReplace = true
        });
        recreated.Id.Should().NotBe(created.Id);
        (await recipeService.GetSubstitutesAsync(lineId)).Should().ContainSingle();
    }

    [Fact]
    public async Task GetIngredientsLookupAsync_ShouldOnlyUseConsumptionWarehouses()
    {
        await using var db = TestDbContextFactory.CreateContext();
        var catalog = new MenuCatalogService(db);
        var normalWarehouse = new Warehouse { WarehouseCode = "UT_NORMAL_WH", WarehouseName = "Normal warehouse", IsActive = true, IsConsumptionWarehouse = false };
        var consumptionWarehouse = new Warehouse { WarehouseCode = "UT_CONSUME_WH", WarehouseName = "Consumption warehouse", IsActive = true, IsConsumptionWarehouse = true };
        var itemA = new Item { ItemCode = "UT_CONSUME_ITEM", ItemName = "Consumption item", Unit = "kg", GroupName = "UT", IsActive = true };
        var itemB = new Item { ItemCode = "UT_NORMAL_ITEM", ItemName = "Normal item", Unit = "kg", GroupName = "UT", IsActive = true };
        db.Warehouses.AddRange(normalWarehouse, consumptionWarehouse);
        db.Items.AddRange(itemA, itemB);
        await db.SaveChangesAsync();

        db.StockBalances.AddRange(
            new StockBalance { ItemId = itemA.Id, WarehouseId = consumptionWarehouse.Id, ClosingQty = 5, AvgPrice = 10 },
            new StockBalance { ItemId = itemB.Id, WarehouseId = normalWarehouse.Id, ClosingQty = 5, AvgPrice = 10 });
        await db.SaveChangesAsync();

        var lookup = await catalog.GetIngredientsLookupAsync();

        lookup.Warehouses.Should().ContainSingle(x => x.WarehouseId == consumptionWarehouse.Id);
        lookup.Items.Should().Contain(x => x.ItemId == itemA.Id);
        lookup.Items.Should().Contain(x => x.ItemId == itemB.Id);
    }

    [Fact]
    public async Task PreviewCostAsync_ShouldWarnWhenConsumptionStockIsInsufficient()
    {
        await using var db = TestDbContextFactory.CreateContext();
        var catalog = new MenuCatalogService(db);
        var recipeService = new MenuRecipeService(db);
        var category = (await catalog.GetCategoriesAsync()).First();
        var warehouse = new Warehouse { WarehouseCode = "UT_WARN_WH", WarehouseName = "Warning warehouse", IsActive = true, IsConsumptionWarehouse = true };
        var item = new Item { ItemCode = "UT_WARN_ITEM", ItemName = "Warning item", Unit = "kg", GroupName = "UT", IsActive = true };
        db.Warehouses.Add(warehouse);
        db.Items.Add(item);
        await db.SaveChangesAsync();
        db.StockBalances.Add(new StockBalance { ItemId = item.Id, WarehouseId = warehouse.Id, ClosingQty = 1, AvgPrice = 10 });
        await db.SaveChangesAsync();

        var menu = await catalog.CreateOrUpdateMenuItemAsync(new CreateMenuItemRequest
        {
            MenuCategoryId = category.Id,
            Code = "UT_MENU_STOCK_WARN",
            Name = "Unit test stock warning",
            UnitName = "phan",
            UnitPrice = 100000,
            VatRate = 0.08m,
            HasRecipeManagement = true
        });

        await recipeService.SaveRecipeAsync(new SaveMenuRecipeRequest
        {
            MenuItemId = menu.Id,
            Lines =
            [
                new MenuRecipeLineDto
                {
                    LineNo = 1,
                    ItemId = item.Id,
                    DefaultWarehouseId = warehouse.Id,
                    ItemCode = item.ItemCode,
                    ItemName = item.ItemName,
                    UnitName = item.Unit,
                    Quantity = 2,
                    LossRate = 0
                }
            ]
        });

        var preview = await recipeService.PreviewCostAsync(menu.Id);

        preview.Lines.Should().ContainSingle();
        preview.Lines[0].InsufficientStock.Should().BeTrue();
        preview.Warnings.Should().Contain(x => x.Contains("không đủ tồn kho", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task PreviewCostAsync_ShouldWarnOutOfStockWhenIngredientHasNoConsumptionStock()
    {
        await using var db = TestDbContextFactory.CreateContext();
        var catalog = new MenuCatalogService(db);
        var recipeService = new MenuRecipeService(db);
        var category = (await catalog.GetCategoriesAsync()).First();
        var warehouse = new Warehouse { WarehouseCode = "UT_EMPTY_WH", WarehouseName = "Empty warehouse", IsActive = true, IsConsumptionWarehouse = true };
        var item = new Item { ItemCode = "UT_EMPTY_ITEM", ItemName = "Empty item", Unit = "kg", GroupName = "UT", IsActive = true };
        db.Warehouses.Add(warehouse);
        db.Items.Add(item);
        await db.SaveChangesAsync();

        var lookup = await catalog.GetIngredientsLookupAsync();
        lookup.Items.Should().Contain(x => x.ItemId == item.Id);

        var menu = await catalog.CreateOrUpdateMenuItemAsync(new CreateMenuItemRequest
        {
            MenuCategoryId = category.Id,
            Code = "UT_MENU_EMPTY_STOCK",
            Name = "Unit test empty stock",
            UnitName = "phan",
            UnitPrice = 100000,
            VatRate = 0.08m,
            HasRecipeManagement = true
        });

        await recipeService.SaveRecipeAsync(new SaveMenuRecipeRequest
        {
            MenuItemId = menu.Id,
            Lines =
            [
                new MenuRecipeLineDto
                {
                    LineNo = 1,
                    ItemId = item.Id,
                    DefaultWarehouseId = warehouse.Id,
                    ItemCode = item.ItemCode,
                    ItemName = item.ItemName,
                    UnitName = item.Unit,
                    Quantity = 1,
                    LossRate = 0
                }
            ]
        });

        var preview = await recipeService.PreviewCostAsync(menu.Id);

        preview.Lines.Should().ContainSingle();
        preview.Lines[0].AvailableQuantity.Should().Be(0);
        preview.Lines[0].InsufficientStock.Should().BeTrue();
        preview.Warnings.Should().NotBeEmpty();
    }

    [Fact]
    public async Task PreviewCostAsync_ShouldWarnWhenCostMissing()
    {
        await using var db = TestDbContextFactory.CreateContext();
        var catalog = new MenuCatalogService(db);
        var recipeService = new MenuRecipeService(db);
        var category = (await catalog.GetCategoriesAsync()).First();
        db.Items.Add(new ASMILE.BO.Core.Entities.Item { ItemCode = "UT_ITEM_01", ItemName = "Missing cost item", Unit = "kg", GroupName = "UT", IsActive = true });
        await db.SaveChangesAsync();
        var itemId = await db.Items.Where(x => x.ItemCode == "UT_ITEM_01").Select(x => x.Id).FirstAsync();

        var menu = await catalog.CreateOrUpdateMenuItemAsync(new CreateMenuItemRequest
        {
            MenuCategoryId = category.Id,
            Code = "UT_MENU_03",
            Name = "Unit test menu 3",
            UnitName = "phần",
            UnitPrice = 40000,
            VatRate = 0.08m,
            HasRecipeManagement = true
        });

        await recipeService.SaveRecipeAsync(new SaveMenuRecipeRequest
        {
            MenuItemId = menu.Id,
            Lines =
            [
                new MenuRecipeLineDto
                {
                    LineNo = 1,
                    ItemId = itemId,
                    ItemCode = "UT_ITEM_01",
                    ItemName = "Missing cost item",
                    UnitName = "kg",
                    Quantity = 1,
                    LossRate = 0
                }
            ]
        });

        var preview = await recipeService.PreviewCostAsync(menu.Id);

        preview.Warnings.Should().Contain(x => x.Contains("chưa có giá vốn", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task DeleteOrDeactivateMenuItemAsync_WithRecipe_ShouldSoftDeactivate()
    {
        await using var db = TestDbContextFactory.CreateContext();
        var catalog = new MenuCatalogService(db);
        var recipeService = new MenuRecipeService(db);
        var category = (await catalog.GetCategoriesAsync()).First();
        db.Items.Add(new ASMILE.BO.Core.Entities.Item { ItemCode = "UT_ITEM_02", ItemName = "Ingredient", Unit = "kg", GroupName = "UT", IsActive = true });
        await db.SaveChangesAsync();
        var itemId = await db.Items.Where(x => x.ItemCode == "UT_ITEM_02").Select(x => x.Id).FirstAsync();
        var menu = await catalog.CreateOrUpdateMenuItemAsync(new CreateMenuItemRequest
        {
            MenuCategoryId = category.Id,
            Code = "UT_MENU_04",
            Name = "Unit test menu 4",
            UnitName = "phần",
            UnitPrice = 60000,
            VatRate = 0.08m,
            HasRecipeManagement = true
        });

        await recipeService.SaveRecipeAsync(new SaveMenuRecipeRequest
        {
            MenuItemId = menu.Id,
            Lines =
            [
                new MenuRecipeLineDto
                {
                    LineNo = 1,
                    ItemId = itemId,
                    ItemCode = "UT_ITEM_02",
                    ItemName = "Ingredient",
                    UnitName = "kg",
                    Quantity = 1,
                    LossRate = 0
                }
            ]
        });

        await catalog.DeleteOrDeactivateMenuItemAsync(menu.Id, softDeleteOnly: true);

        var entity = await db.MenuItems.FirstAsync(x => x.Id == menu.Id);
        entity.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task PreviewCostAsync_ShouldUseSelectedWarehouseLatestCostLayer()
    {
        await using var db = TestDbContextFactory.CreateContext();
        var catalog = new MenuCatalogService(db);
        var recipeService = new MenuRecipeService(db);
        var category = (await catalog.GetCategoriesAsync()).First();

        var warehouseA = new Warehouse { WarehouseCode = "WHA", WarehouseName = "Warehouse A", IsActive = true };
        var warehouseB = new Warehouse { WarehouseCode = "WHB", WarehouseName = "Warehouse B", IsActive = true };
        var item = new Item { ItemCode = "UT_ITEM_COST", ItemName = "Costed item", Unit = "kg", GroupName = "UT", IsActive = true };
        db.Warehouses.AddRange(warehouseA, warehouseB);
        db.Items.Add(item);
        await db.SaveChangesAsync();

        db.InventoryCostLayers.AddRange(
            new InventoryCostLayer
            {
                Id = Guid.NewGuid(),
                ItemId = item.Id,
                WarehouseId = warehouseA.Id,
                UnitCost = 90_000,
                DocumentDate = new DateTime(2026, 1, 1),
                CreatedAt = new DateTime(2026, 1, 1),
                UpdatedAt = new DateTime(2026, 1, 1),
                DocumentNo = "OLD"
            },
            new InventoryCostLayer
            {
                Id = Guid.NewGuid(),
                ItemId = item.Id,
                WarehouseId = warehouseA.Id,
                UnitCost = 30_000,
                DocumentDate = new DateTime(2026, 2, 1),
                CreatedAt = new DateTime(2026, 2, 1),
                UpdatedAt = new DateTime(2026, 2, 1),
                DocumentNo = "NEW"
            },
            new InventoryCostLayer
            {
                Id = Guid.NewGuid(),
                ItemId = item.Id,
                WarehouseId = warehouseB.Id,
                UnitCost = 120_000,
                DocumentDate = new DateTime(2026, 3, 1),
                CreatedAt = new DateTime(2026, 3, 1),
                UpdatedAt = new DateTime(2026, 3, 1),
                DocumentNo = "OTHER"
            });
        await db.SaveChangesAsync();

        var menu = await catalog.CreateOrUpdateMenuItemAsync(new CreateMenuItemRequest
        {
            MenuCategoryId = category.Id,
            Code = "UT_MENU_COST",
            Name = "Unit test menu cost",
            UnitName = "phần",
            UnitPrice = 100_000,
            VatRate = 0.08m,
            HasRecipeManagement = true
        });

        await recipeService.SaveRecipeAsync(new SaveMenuRecipeRequest
        {
            MenuItemId = menu.Id,
            Lines =
            [
                new MenuRecipeLineDto
                {
                    LineNo = 1,
                    ItemId = item.Id,
                    DefaultWarehouseId = warehouseA.Id,
                    ItemCode = item.ItemCode,
                    ItemName = item.ItemName,
                    UnitName = item.Unit,
                    Quantity = 2,
                    LossRate = 0.1m
                }
            ]
        });

        var preview = await recipeService.PreviewCostAsync(menu.Id);

        preview.Lines.Should().ContainSingle();
        preview.Lines[0].UnitCost.Should().Be(30_000);
        preview.Lines[0].LineCost.Should().Be(66_000);
    }
}
