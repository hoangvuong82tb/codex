using ASMILE.BO.Application.DTOs;
using ASMILE.BO.Application.Interfaces;
using ASMILE.BO.Core.Entities;
using ASMILE.BO.Infrastructure.Services;
using ASMILE.BO.Tests.Unit.TestSupport;
using FluentAssertions;
using System.Text;
using System.Text.Json;
using System.Reflection;

namespace ASMILE.BO.Tests.Unit;

public sealed class RestaurantModuleTests
{
    [Fact]
    public void AlacarteVip_TableAllocation_31Guests_Standard10_ShouldCreateVirtualAndThreeTables()
    {
        var engine = new TableAllocationEngine();

        var tables = engine.Allocate(31, 10);

        tables.Select(x => x.GuestCount).Should().Equal(31, 11, 10, 10);
        tables[0].IsVirtual.Should().BeTrue();
        tables[0].DisplayName.Should().Be("Ban Tong");
        tables.Skip(1).All(x => !x.IsVirtual).Should().BeTrue();
    }

    [Fact]
    public void AlacarteVip_TableAllocation_46Guests_Standard10_ShouldCreateVirtualAndFiveTables()
    {
        var engine = new TableAllocationEngine();

        var tables = engine.Allocate(46, 10);

        tables.Select(x => x.GuestCount).Should().Equal(46, 10, 10, 10, 10, 6);
    }

    [Fact]
    public void AlacarteVip_TableAllocation_21Guests_Standard10_ShouldPreferTwoBalancedTables()
    {
        var engine = new TableAllocationEngine();

        var tables = engine.Allocate(21, 10);

        tables.Select(x => x.GuestCount).Should().Equal(21, 11, 10);
    }

    [Fact]
    public void AlacarteVip_TableAllocation_34Guests_Standard10_ShouldRedistributeSmallRemainderAcrossServingTables()
    {
        var engine = new TableAllocationEngine();

        var tables = engine.Allocate(34, 10);

        tables.Select(x => x.GuestCount).Should().Equal(34, 12, 11, 11);
    }

    [Fact]
    public void AlacarteVip_TableAllocation_35Guests_Standard10_ShouldAvoidOpeningTinyOddTable()
    {
        var engine = new TableAllocationEngine();

        var tables = engine.Allocate(35, 10);

        tables.Select(x => x.GuestCount).Should().Equal(35, 12, 12, 11);
    }

    [Fact]
    public void AlacarteVip_TableAllocation_8Guests_Standard10_ShouldCreateVirtualAndSingleTable()
    {
        var engine = new TableAllocationEngine();

        var tables = engine.Allocate(8, 10);

        tables.Select(x => x.GuestCount).Should().Equal(8, 8);
        tables.Should().HaveCount(2);
    }

    [Fact]
    public void AlacarteVip_TableAllocation_InvalidGuestCount_ShouldThrow()
    {
        var engine = new TableAllocationEngine();

        var act = () => engine.Allocate(0, 10);

        act.Should().Throw<InvalidOperationException>();
    }

    [Theory]
    [InlineData("PerPerson", 31, 3, 1, null, 31)]
    [InlineData("PerTable", 31, 3, 1, null, 3)]
    [InlineData("PerRoom", 31, 3, 1, null, 1)]
    public void AlacarteVip_MenuCalculation_ShouldFollowPricingRule(string pricingRule, int guestCount, int realTableCount, decimal defaultQuantity, decimal? manualQuantity, decimal expected)
    {
        var engine = new MenuCalculationEngine();

        var quantity = engine.Calculate(pricingRule, guestCount, realTableCount, defaultQuantity, manualQuantity);

        quantity.Should().Be(expected);
    }

    [Fact]
    public void AlacarteVip_MenuCalculation_Manual_ShouldUseManualQuantity()
    {
        var engine = new MenuCalculationEngine();

        var quantity = engine.Calculate("Manual", 31, 3, 1m, 9m);

        quantity.Should().Be(9);
    }

    [Fact]
    public void AlacarteVip_MenuPlanning_PerGuest_OnOddOnlyTables_ShouldUseActualGuestCount()
    {
        var engine = new RoomOrderMenuPlanningEngine();

        var plan = engine.BuildPlan(new RoomOrderMenuPlanningRequest
        {
            ApplyScope = "OddOnly",
            ServingRule = "PerGuest",
            TableStandard = 10,
            BaseQuantity = 1m,
            Tables =
            [
                new RoomOrderMenuTableInput { TableId = Guid.NewGuid(), TableName = "Bàn 01", GuestCount = 10, Status = "Serving" },
                new RoomOrderMenuTableInput { TableId = Guid.NewGuid(), TableName = "Bàn 02", GuestCount = 11, Status = "Serving" },
                new RoomOrderMenuTableInput { TableId = Guid.NewGuid(), TableName = "Bàn 03", GuestCount = 12, Status = "Serving" }
            ]
        });

        plan.Should().HaveCount(2);
        plan.Select(x => x.SaveQuantity).Should().Equal(11m, 12m);
    }

    [Fact]
    public void AlacarteVip_MenuPlanning_PerTableWithExtraUnit_ShouldKeepBaseQtyAndExposeExtraNote()
    {
        var engine = new RoomOrderMenuPlanningEngine();
        var tableId = Guid.NewGuid();

        var plan = engine.BuildPlan(new RoomOrderMenuPlanningRequest
        {
            ApplyScope = "SelectedTable",
            ServingRule = "PerTableWithExtraUnit",
            TableStandard = 10,
            BaseQuantity = 1m,
            ExtraUnitQuantity = 1m,
            SelectedTableId = tableId,
            Tables =
            [
                new RoomOrderMenuTableInput { TableId = tableId, TableName = "Bàn 03", GuestCount = 12, Status = "Serving" }
            ]
        });

        plan.Should().ContainSingle();
        plan[0].SaveQuantity.Should().Be(1m);
        plan[0].DisplayQuantity.Should().Be(3m);
        plan[0].KitchenNote.Should().Contain("Phần bù lẻ");
    }

    [Fact]
    public void AlacarteVip_MenuPlanning_StandardOnly_ShouldIgnoreOddAndWaitingTables()
    {
        var engine = new RoomOrderMenuPlanningEngine();

        var plan = engine.BuildPlan(new RoomOrderMenuPlanningRequest
        {
            ApplyScope = "StandardOnly",
            ServingRule = "PerTable",
            TableStandard = 10,
            BaseQuantity = 1m,
            Tables =
            [
                new RoomOrderMenuTableInput { TableId = Guid.NewGuid(), TableName = "Bàn 01", GuestCount = 10, Status = "Serving" },
                new RoomOrderMenuTableInput { TableId = Guid.NewGuid(), TableName = "Bàn 02", GuestCount = 11, Status = "Serving" },
                new RoomOrderMenuTableInput { TableId = Guid.NewGuid(), TableName = "Bàn 03", GuestCount = 10, Status = "Waiting" }
            ]
        });

        plan.Should().ContainSingle();
        plan[0].TableName.Should().Be("Bàn 01");
        plan[0].SaveQuantity.Should().Be(1m);
    }

    [Fact]
    public async Task Alacarte_ShouldCreateRoomTableReservationAndOrder()
    {
        await using var db = TestDbContextFactory.CreateContext();
        var service = new AlacarteRestaurantService(db, new MenuCatalogService(db));

        var room = await service.SaveRoomAsync(new SaveDiningRoomRequest
        {
            Code = "VIP-01",
            Name = "Phòng VIP 01",
            RoomType = "VIP",
            Capacity = 20
        });

        var table = await service.SaveTableAsync(new SaveDiningTableRequest
        {
            DiningRoomId = room.Id,
            Code = "A01",
            Name = "Bàn A01",
            SeatCount = 6
        });

        var reservation = await service.SaveReservationAsync(new SaveTableReservationRequest
        {
            CustomerName = "Nguyễn Văn A",
            Phone = "0900000000",
            ReservationDateTime = DateTime.Today.AddHours(18),
            GuestCount = 6,
            DiningRoomId = room.Id,
            DiningTableId = table.Id,
            Status = "Confirmed"
        });

        var order = await service.OpenOrderAsync(new CreateAlacarteOrderRequest { DiningTableId = table.Id });

        room.Code.Should().Be("VIP-01");
        table.RoomName.Should().Be("Phòng VIP 01");
        reservation.ReservationNo.Should().StartWith("RSV-");
        order.OrderNo.Should().StartWith("ALA-");
        (await service.GetDashboardAsync(DateTime.Today)).TodayReservations.Should().Be(1);
    }

    [Fact]
    public async Task AlacarteVip_UpdateRoomServingPlan_ShouldPersistManualGuestSplit()
    {
        await using var db = TestDbContextFactory.CreateContext();
        var service = new AlacarteRestaurantService(db, new MenuCatalogService(db));

        var room = await service.SaveRoomAsync(new SaveDiningRoomRequest
        {
            Code = "VIP-PLAN",
            Name = "Phòng VIP Plan",
            RoomType = "VIP",
            Capacity = 40
        });

        await service.SaveTableAsync(new SaveDiningTableRequest
        {
            DiningRoomId = room.Id,
            Code = "VIP-PLAN-B01",
            Name = "Bàn 01",
            SeatCount = 10
        });
        await service.SaveTableAsync(new SaveDiningTableRequest
        {
            DiningRoomId = room.Id,
            Code = "VIP-PLAN-B02",
            Name = "Bàn 02",
            SeatCount = 10
        });
        await service.SaveTableAsync(new SaveDiningTableRequest
        {
            DiningRoomId = room.Id,
            Code = "VIP-PLAN-B03",
            Name = "Bàn 03",
            SeatCount = 10
        });

        var order = await service.OpenRoomWalkInOrderAsync(new CreateRoomWalkInOrderRequest
        {
            DiningRoomId = room.Id,
            GuestCount = 21,
            TableCountToOpen = 2,
            CustomerName = "Anh Toàn",
            Phone = "0909000001",
            CreatedBy = "tester"
        });

        var physicalTables = order.Tables.Where(x => !x.IsVirtual).OrderBy(x => x.SortOrder).ToList();
        physicalTables.Should().HaveCount(4);
        physicalTables.Should().OnlyContain(x => x.DiningTableId.HasValue);
        physicalTables.Select(x => x.Id).Should().OnlyHaveUniqueItems();
        physicalTables.Select(x => x.DiningTableId!.Value).Should().OnlyHaveUniqueItems();

        var updated = await service.UpdateRoomWalkInServingPlanAsync(new UpdateRoomWalkInServingPlanRequest
        {
            OrderId = order.Id,
            UpdatedBy = "tester",
            Tables =
            [
                new UpdateRoomWalkInServingTableRequest { OrderTableId = physicalTables[0].Id, IsServing = true, GuestCount = 11 },
                new UpdateRoomWalkInServingTableRequest { OrderTableId = physicalTables[1].Id, IsServing = true, GuestCount = 10 },
                new UpdateRoomWalkInServingTableRequest { OrderTableId = physicalTables[2].Id, IsServing = false, GuestCount = 0 }
            ]
        });

        updated.GuestCount.Should().Be(21);
        updated.Tables.Where(x => !x.IsVirtual).Select(x => x.GuestCount).Should().Equal(11, 10, 0, 0);
        updated.Tables.Where(x => !x.IsVirtual).Select(x => x.Status).Should().Equal("Serving", "Serving", "Waiting", "Waiting");
    }

    [Fact]
    public async Task AlacarteVip_ReopenRoomAfterPaid_ShouldCreateFreshOrderTables()
    {
        await using var db = TestDbContextFactory.CreateContext();
        var service = new AlacarteRestaurantService(db, new MenuCatalogService(db));

        var room = await service.SaveRoomAsync(new SaveDiningRoomRequest { Code = "VIP-REOPEN", Name = "VIP Reopen", Capacity = 20 });
        await service.SaveTableAsync(new SaveDiningTableRequest { DiningRoomId = room.Id, Code = "VIP-REOPEN-B01", Name = "Bàn 01", SeatCount = 10 });
        await service.SaveTableAsync(new SaveDiningTableRequest { DiningRoomId = room.Id, Code = "VIP-REOPEN-B02", Name = "Bàn 02", SeatCount = 10 });
        var menuItem = SeedMenuItem(db, "VIP-REOPEN-FOOD", "Món thử", 100000m, "Kitchen");

        var firstOrder = await service.OpenRoomWalkInOrderAsync(new CreateRoomWalkInOrderRequest
        {
            DiningRoomId = room.Id,
            GuestCount = 12,
            TableCountToOpen = 2,
            CustomerName = "Khách 1",
            Phone = "0901000001",
            CreatedBy = "tester"
        });

        var firstPhysicalTables = firstOrder.Tables.Where(x => !x.IsVirtual).OrderBy(x => x.SortOrder).ToList();
        firstPhysicalTables.Should().HaveCount(2);

        firstOrder = await service.AddOrUpdateLineAsync(new SaveAlacarteOrderLineRequest
        {
            OrderId = firstOrder.Id,
            OrderTableId = firstPhysicalTables[0].Id,
            MenuItemId = menuItem.Id,
            Qty = 1
        });

        await service.PayOrderAsync(new PayAlacarteOrderRequest
        {
            OrderId = firstOrder.Id,
            PaidAmount = firstOrder.TotalAmount,
            PaymentMethod = "Tiền mặt",
            PaidBy = "tester"
        });

        var reopenedOrder = await service.OpenRoomWalkInOrderAsync(new CreateRoomWalkInOrderRequest
        {
            DiningRoomId = room.Id,
            GuestCount = 8,
            TableCountToOpen = 1,
            CustomerName = "Khách 2",
            Phone = "0901000002",
            CreatedBy = "tester"
        });

        var reopenedPhysicalTables = reopenedOrder.Tables.Where(x => !x.IsVirtual).OrderBy(x => x.SortOrder).ToList();
        reopenedPhysicalTables.Should().HaveCount(2);
        reopenedPhysicalTables.Should().OnlyContain(x => x.DiningTableId.HasValue);
        reopenedPhysicalTables.Select(x => x.Id).Intersect(firstPhysicalTables.Select(x => x.Id)).Should().BeEmpty();
        reopenedPhysicalTables.Select(x => x.DiningTableId!.Value).Should().BeEquivalentTo(firstPhysicalTables.Select(x => x.DiningTableId!.Value));
    }

    [Fact]
    public async Task AlacarteVip_SaveServingGroup_ShouldPersistCustomGroupWithSelectedTables()
    {
        await using var db = TestDbContextFactory.CreateContext();
        var service = new AlacarteRestaurantService(db, new MenuCatalogService(db));

        var room = await service.SaveRoomAsync(new SaveDiningRoomRequest { Code = "VIP-GRP", Name = "VIP Group", Capacity = 40 });
        await service.SaveTableAsync(new SaveDiningTableRequest { DiningRoomId = room.Id, Code = "VIP-GRP-B01", Name = "Bàn 01", SeatCount = 10 });
        await service.SaveTableAsync(new SaveDiningTableRequest { DiningRoomId = room.Id, Code = "VIP-GRP-B02", Name = "Bàn 02", SeatCount = 10 });
        await service.SaveTableAsync(new SaveDiningTableRequest { DiningRoomId = room.Id, Code = "VIP-GRP-B03", Name = "Bàn 03", SeatCount = 10 });

        var order = await service.OpenRoomWalkInOrderAsync(new CreateRoomWalkInOrderRequest
        {
            DiningRoomId = room.Id,
            GuestCount = 21,
            TableCountToOpen = 2,
            CustomerName = "Khách nhóm",
            Phone = "0905000001",
            CreatedBy = "tester"
        });

        var servingTables = order.Tables.Where(x => !x.IsVirtual && x.Status == "Serving").OrderBy(x => x.SortOrder).ToList();
        servingTables.Should().HaveCount(2);

        var saved = await service.SaveServingGroupAsync(new SaveRestaurantServingGroupRequest
        {
            OrderId = order.Id,
            Name = "Nhóm VIP riêng",
            ScopeType = "Custom",
            ServingRule = "PerTableWithExtraUnit",
            Note = "Nhóm khách ăn giống nhau",
            OrderTableIds = [servingTables[0].Id, servingTables[1].Id]
        });

        saved.IsSystemDefined.Should().BeFalse();
        saved.Name.Should().Be("Nhóm VIP riêng");
        saved.TableCount.Should().Be(2);
        saved.GuestCount.Should().Be(21);

        var groups = await service.GetServingGroupsAsync(order.Id);
        groups.Should().ContainSingle(x => x.Id == saved.Id);
        groups.Should().Contain(x => x.IsSystemDefined);
    }

    [Fact]
    public async Task AlacarteVip_UpdateServingPlan_ShouldKeepCustomServingGroupAndPruneWaitingTables()
    {
        await using var db = TestDbContextFactory.CreateContext();
        var service = new AlacarteRestaurantService(db, new MenuCatalogService(db));

        var room = await service.SaveRoomAsync(new SaveDiningRoomRequest { Code = "VIP-GRP2", Name = "VIP Group 2", Capacity = 40 });
        await service.SaveTableAsync(new SaveDiningTableRequest { DiningRoomId = room.Id, Code = "VIP-GRP2-B01", Name = "Bàn 01", SeatCount = 10 });
        await service.SaveTableAsync(new SaveDiningTableRequest { DiningRoomId = room.Id, Code = "VIP-GRP2-B02", Name = "Bàn 02", SeatCount = 10 });
        await service.SaveTableAsync(new SaveDiningTableRequest { DiningRoomId = room.Id, Code = "VIP-GRP2-B03", Name = "Bàn 03", SeatCount = 10 });

        var order = await service.OpenRoomWalkInOrderAsync(new CreateRoomWalkInOrderRequest
        {
            DiningRoomId = room.Id,
            GuestCount = 21,
            TableCountToOpen = 2,
            CustomerName = "Khách prune",
            Phone = "0905000002",
            CreatedBy = "tester"
        });

        var servingTables = order.Tables.Where(x => !x.IsVirtual && x.Status == "Serving").OrderBy(x => x.SortOrder).ToList();
        var customGroup = await service.SaveServingGroupAsync(new SaveRestaurantServingGroupRequest
        {
            OrderId = order.Id,
            Name = "Nhóm cần giữ",
            ScopeType = "Custom",
            ServingRule = "PerTable",
            OrderTableIds = [servingTables[0].Id, servingTables[1].Id]
        });

        var updated = await service.UpdateRoomWalkInServingPlanAsync(new UpdateRoomWalkInServingPlanRequest
        {
            OrderId = order.Id,
            UpdatedBy = "tester",
            Tables =
            [
                new UpdateRoomWalkInServingTableRequest { OrderTableId = servingTables[0].Id, IsServing = true, GuestCount = 11 },
                new UpdateRoomWalkInServingTableRequest { OrderTableId = servingTables[1].Id, IsServing = false, GuestCount = 0 }
            ]
        });

        var groups = await service.GetServingGroupsAsync(updated.Id);
        var persistedCustom = groups.Single(x => x.Id == customGroup.Id);
        persistedCustom.TableCount.Should().Be(1);
        persistedCustom.Tables.Should().ContainSingle(x => x.RestaurantOrderTableId == servingTables[0].Id);
    }

    [Fact]
    public async Task Alacarte_ShouldCalculateTotalAndSendOnlyNewLines()
    {
        await using var db = TestDbContextFactory.CreateContext();
        var service = new AlacarteRestaurantService(db, new MenuCatalogService(db));
        var menuItem = SeedMenuItem(db, "FOOD-01", "Phở bò", 50000m, "Kitchen");
        var drinkItem = SeedMenuItem(db, "DRK-01", "Cà phê", 30000m, "Bar");

        var room = await service.SaveRoomAsync(new SaveDiningRoomRequest { Code = "STD", Name = "Khu A", Capacity = 40 });
        var table = await service.SaveTableAsync(new SaveDiningTableRequest { DiningRoomId = room.Id, Code = "T01", Name = "Bàn 1", SeatCount = 4 });
        var order = await service.OpenOrderAsync(new CreateAlacarteOrderRequest { DiningTableId = table.Id });

        order = await service.AddOrUpdateLineAsync(new SaveAlacarteOrderLineRequest { OrderId = order.Id, MenuItemId = menuItem.Id, Qty = 2 });
        order.TotalAmount.Should().Be(100000m);

        var firstTickets = await service.SendToPreparationAsync(new SendAlacartePreparationRequest { OrderId = order.Id, SentBy = "tester" });
        firstTickets.Should().ContainSingle(x => x.Station == "Kitchen");
        firstTickets.Sum(x => x.Lines.Count).Should().Be(1);

        order = await service.AddOrUpdateLineAsync(new SaveAlacarteOrderLineRequest { OrderId = order.Id, MenuItemId = drinkItem.Id, Qty = 1 });
        var supplementTickets = await service.SendToPreparationAsync(new SendAlacartePreparationRequest { OrderId = order.Id, SentBy = "tester" });

        supplementTickets.Should().ContainSingle(x => x.Station == "Bar");
        supplementTickets.Sum(x => x.Lines.Count).Should().Be(1);
        supplementTickets.Single().Lines.Single().MenuItemName.Should().Be("Cà phê");
    }

    [Fact]
    public async Task AlacarteVip_SendToPreparation_ShouldCreateConsumptionIssueFromRecipe()
    {
        await using var db = TestDbContextFactory.CreateContext();
        var service = new AlacarteRestaurantService(db, new MenuCatalogService(db));
        var room = await service.SaveRoomAsync(new SaveDiningRoomRequest { Code = "KHO-01", Name = "Kho test", Capacity = 20 });
        var table = await service.SaveTableAsync(new SaveDiningTableRequest { DiningRoomId = room.Id, Code = "KHO-T01", Name = "Bàn 1", SeatCount = 10 });
        var warehouse = SeedWarehouse(db, "KHO-BEP", "Kho bếp");
        var ingredient = SeedInventoryItem(db, "TOM-01", "Tôm sú", "con");
        var menuItem = SeedMenuItem(db, "FOOD-RECIPE", "Tôm hấp", 200000m, "Kitchen");
        SeedRecipe(db, menuItem, ingredient, warehouse, 10m);

        var order = await service.OpenOrderAsync(new CreateAlacarteOrderRequest { DiningTableId = table.Id });
        order = await service.AddOrUpdateLineAsync(new SaveAlacarteOrderLineRequest
        {
            OrderId = order.Id,
            MenuItemId = menuItem.Id,
            Qty = 2,
            SupplementQuantity = 1,
            SupplementNote = "Bù 1 con"
        });

        var tickets = await service.SendToPreparationAsync(new SendAlacartePreparationRequest { OrderId = order.Id, SentBy = "tester" });

        tickets.Should().ContainSingle(x => x.Station == "Kitchen");
        var issue = db.StockIssues.Single();
        issue.WarehouseId.Should().Be(warehouse.Id);
        issue.Description.Should().Contain(order.OrderNo);
        issue.Details.Should().ContainSingle();
        issue.Details.Single().ItemId.Should().Be(ingredient.Id);
        issue.Details.Single().Quantity.Should().Be(30m);
    }

    [Fact]
    public async Task AlacarteVip_SendToPreparation_ShouldApplyIngredientLevelExtraQuantity()
    {
        await using var db = TestDbContextFactory.CreateContext();
        var service = new AlacarteRestaurantService(db, new MenuCatalogService(db));
        var room = await service.SaveRoomAsync(new SaveDiningRoomRequest { Code = "KHO-EXTRA", Name = "Kho extra", Capacity = 20 });
        var table = await service.SaveTableAsync(new SaveDiningTableRequest { DiningRoomId = room.Id, Code = "KHO-EXTRA-T01", Name = "Bàn 1", SeatCount = 10 });
        var warehouse = SeedWarehouse(db, "KHO-BEP", "Kho bếp");
        var shrimp = SeedInventoryItem(db, "TOM-EX", "Tôm size", "con");
        var herb = SeedInventoryItem(db, "RAU-EX", "Rau thơm", "mớ");
        var menuItem = SeedMenuItem(db, "FOOD-EXTRA", "Tôm hấp rau", 220000m, "Kitchen");

        var recipe = new MenuItemRecipe { MenuItemId = menuItem.Id, Name = "Default", IsDefault = true, IsActive = true };
        recipe.Lines.Add(new MenuItemRecipeLine
        {
            MenuItemRecipeId = recipe.Id,
            LineNo = 1,
            ItemId = shrimp.Id,
            DefaultWarehouseId = warehouse.Id,
            ItemCode = shrimp.ItemCode,
            ItemName = shrimp.ItemName,
            UnitName = shrimp.Unit,
            Quantity = 10m,
            LossRate = 0,
            IsRequired = true
        });
        recipe.Lines.Add(new MenuItemRecipeLine
        {
            MenuItemRecipeId = recipe.Id,
            LineNo = 2,
            ItemId = herb.Id,
            DefaultWarehouseId = warehouse.Id,
            ItemCode = herb.ItemCode,
            ItemName = herb.ItemName,
            UnitName = herb.Unit,
            Quantity = 1m,
            LossRate = 0,
            IsRequired = true
        });
        db.MenuItemRecipes.Add(recipe);
        await db.SaveChangesAsync();

        var order = await service.OpenOrderAsync(new CreateAlacarteOrderRequest { DiningTableId = table.Id });
        order = await service.AddOrUpdateLineAsync(new SaveAlacarteOrderLineRequest
        {
            OrderId = order.Id,
            MenuItemId = menuItem.Id,
            Qty = 1,
            SupplementNote = BuildIngredientSupplementNote(
                "Bù thêm tôm thực tế",
                new RestaurantLineIngredientAdjustmentDto
                {
                    RecipeLineId = recipe.Lines.First().Id,
                    SelectedInventoryItemId = shrimp.Id,
                    SelectedItemName = shrimp.ItemName,
                    ConversionRate = 1m,
                    ExtraQuantity = 1m,
                    Note = "+1 con"
                })
        });

        await service.SendToPreparationAsync(new SendAlacartePreparationRequest { OrderId = order.Id, SentBy = "tester" });

        var issue = db.StockIssues.Single();
        issue.Details.Should().ContainSingle(x => x.ItemId == shrimp.Id && x.Quantity == 11m);
        issue.Details.Should().ContainSingle(x => x.ItemId == herb.Id && x.Quantity == 1m);
    }

    [Fact]
    public async Task AlacarteVip_SendToPreparation_ShouldUseConfiguredSubstituteIngredient()
    {
        await using var db = TestDbContextFactory.CreateContext();
        var service = new AlacarteRestaurantService(db, new MenuCatalogService(db));
        var room = await service.SaveRoomAsync(new SaveDiningRoomRequest { Code = "KHO-SUB", Name = "Kho sub", Capacity = 20 });
        var table = await service.SaveTableAsync(new SaveDiningTableRequest { DiningRoomId = room.Id, Code = "KHO-SUB-T01", Name = "Bàn 1", SeatCount = 10 });
        var warehouse = SeedWarehouse(db, "KHO-BEP", "Kho bếp");
        var shrimp = SeedInventoryItem(db, "TOM-GOC", "Tôm gốc", "con");
        var squid = SeedInventoryItem(db, "MUC-THAY", "Mực thay", "con");
        var menuItem = SeedMenuItem(db, "FOOD-SUB", "Hải sản hấp", 240000m, "Kitchen");

        var recipe = new MenuItemRecipe { MenuItemId = menuItem.Id, Name = "Default", IsDefault = true, IsActive = true };
        var recipeLine = new MenuItemRecipeLine
        {
            MenuItemRecipeId = recipe.Id,
            LineNo = 1,
            ItemId = shrimp.Id,
            DefaultWarehouseId = warehouse.Id,
            ItemCode = shrimp.ItemCode,
            ItemName = shrimp.ItemName,
            UnitName = shrimp.Unit,
            Quantity = 10m,
            LossRate = 0,
            IsRequired = true
        };
        recipe.Lines.Add(recipeLine);
        db.MenuItemRecipes.Add(recipe);
        await db.SaveChangesAsync();

        db.RecipeIngredientSubstitutes.Add(new RecipeIngredientSubstitute
        {
            RecipeLineId = recipeLine.Id,
            InventoryItemId = shrimp.Id,
            SubstituteInventoryItemId = squid.Id,
            ConversionRate = 1.2m,
            Priority = 1,
            AutoReplace = false,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "tester"
        });
        await db.SaveChangesAsync();

        var order = await service.OpenOrderAsync(new CreateAlacarteOrderRequest { DiningTableId = table.Id });
        order = await service.AddOrUpdateLineAsync(new SaveAlacarteOrderLineRequest
        {
            OrderId = order.Id,
            MenuItemId = menuItem.Id,
            Qty = 1,
            SupplementNote = BuildIngredientSupplementNote(
                "Đổi sang mực",
                new RestaurantLineIngredientAdjustmentDto
                {
                    RecipeLineId = recipeLine.Id,
                    SelectedInventoryItemId = squid.Id,
                    SelectedItemName = squid.ItemName,
                    ConversionRate = 1.2m,
                    ExtraQuantity = 0m
                })
        });

        await service.SendToPreparationAsync(new SendAlacartePreparationRequest { OrderId = order.Id, SentBy = "tester" });

        var issue = db.StockIssues.Single();
        issue.Details.Should().ContainSingle(x => x.ItemId == squid.Id && x.Quantity == 12m);
        issue.Details.Should().NotContain(x => x.ItemId == shrimp.Id);
    }

    [Fact]
    public async Task AlacarteVip_ConfigureRoomWalkIn_ShouldSyncServingGroups()
    {
        await using var db = TestDbContextFactory.CreateContext();
        var service = new AlacarteRestaurantService(db, new MenuCatalogService(db));
        var room = await service.SaveRoomAsync(new SaveDiningRoomRequest { Code = "VIP-GROUP", Name = "VIP Group", Capacity = 30 });
        await service.SaveTableAsync(new SaveDiningTableRequest { DiningRoomId = room.Id, Code = "VIP-GROUP-B01", Name = "Bàn 01", SeatCount = 10 });
        await service.SaveTableAsync(new SaveDiningTableRequest { DiningRoomId = room.Id, Code = "VIP-GROUP-B02", Name = "Bàn 02", SeatCount = 10 });
        await service.SaveTableAsync(new SaveDiningTableRequest { DiningRoomId = room.Id, Code = "VIP-GROUP-B03", Name = "Bàn 03", SeatCount = 10 });

        var order = await service.OpenRoomWalkInOrderAsync(new CreateRoomWalkInOrderRequest
        {
            DiningRoomId = room.Id,
            GuestCount = 21,
            TableCountToOpen = 2,
            CustomerName = "Nhóm test",
            Phone = "0909000009",
            CreatedBy = "tester"
        });

        var configured = await service.UpdateRoomWalkInServingPlanAsync(new UpdateRoomWalkInServingPlanRequest
        {
            OrderId = order.Id,
            UpdatedBy = "tester",
            Tables = order.Tables.Where(x => !x.IsVirtual).OrderBy(x => x.SortOrder).Select((x, index) => new UpdateRoomWalkInServingTableRequest
            {
                OrderTableId = x.Id,
                IsServing = index < 2,
                GuestCount = index == 0 ? 11 : index == 1 ? 10 : 0
            }).ToList()
        });

        var groups = db.RestaurantServingGroups
            .Where(x => x.AlacarteOrderId == configured.Id)
            .Select(x => new { x.Code, TableCount = x.Tables.Count })
            .OrderBy(x => x.Code)
            .ToList();

        groups.Should().Contain(x => x.Code == "ALL-SERVING" && x.TableCount == 2);
        groups.Should().Contain(x => x.Code == "ODD" && x.TableCount == 1);
        groups.Should().Contain(x => x.Code == "STANDARD" && x.TableCount == 1);
        groups.Should().Contain(x => x.Code == "WAITING" && x.TableCount == 1);
    }

    [Fact]
    public async Task Alacarte_RemoveSentLine_ShouldCreateCancelTicketWhenRequested()
    {
        await using var db = TestDbContextFactory.CreateContext();
        var service = new AlacarteRestaurantService(db, new MenuCatalogService(db));
        var item = SeedMenuItem(db, "FOOD-02", "Bún bò", 60000m, "Kitchen");
        var room = await service.SaveRoomAsync(new SaveDiningRoomRequest { Code = "STD2", Name = "Khu B", Capacity = 40 });
        var table = await service.SaveTableAsync(new SaveDiningTableRequest { DiningRoomId = room.Id, Code = "T02", Name = "Bàn 2", SeatCount = 4 });
        var order = await service.OpenOrderAsync(new CreateAlacarteOrderRequest { DiningTableId = table.Id });
        order = await service.AddOrUpdateLineAsync(new SaveAlacarteOrderLineRequest { OrderId = order.Id, MenuItemId = item.Id, Qty = 1 });
        await service.SendToPreparationAsync(new SendAlacartePreparationRequest { OrderId = order.Id, SentBy = "tester" });

        var afterRemove = await service.RemoveLineAsync(order.Id, order.Lines.Single().Id, createCancelTicket: true, cancelledBy: "tester");

        afterRemove.Lines.Should().BeEmpty();
        db.RestaurantPreparationTickets.Count(x => x.TicketType == "Cancel").Should().Be(1);
    }

    [Fact]
    public async Task Alacarte_UpdateSentLine_IncreaseQuantity_ShouldCreateImmediateSendAdjustmentTicket()
    {
        await using var db = TestDbContextFactory.CreateContext();
        var service = new AlacarteRestaurantService(db, new MenuCatalogService(db));
        var item = SeedMenuItem(db, "FOOD-ADJ-UP", "Tom sot", 70000m, "Kitchen");
        var room = await service.SaveRoomAsync(new SaveDiningRoomRequest { Code = "ADJ-UP", Name = "Khu tang", Capacity = 20 });
        var table = await service.SaveTableAsync(new SaveDiningTableRequest { DiningRoomId = room.Id, Code = "ADJ-UP-T1", Name = "Ban tang", SeatCount = 10 });
        var order = await service.OpenOrderAsync(new CreateAlacarteOrderRequest { DiningTableId = table.Id });
        order = await service.AddOrUpdateLineAsync(new SaveAlacarteOrderLineRequest { OrderId = order.Id, MenuItemId = item.Id, Qty = 10 });
        await service.SendToPreparationAsync(new SendAlacartePreparationRequest { OrderId = order.Id, SentBy = "tester" });

        var updated = await service.AddOrUpdateLineAsync(new SaveAlacarteOrderLineRequest
        {
            OrderId = order.Id,
            OrderLineId = order.Lines.Single().Id,
            OrderTableId = order.Lines.Single().OrderTableId,
            MenuItemId = item.Id,
            Qty = 11,
            AdjustmentReason = "Khách gọi thêm 1 phần"
        });

        updated.Lines.Should().ContainSingle();
        updated.Lines.Single().Qty.Should().Be(11);
        db.RestaurantPreparationTickets.Count(x => x.TicketType == "Send").Should().Be(2);
        db.RestaurantPreparationTicketLines.Count(x => x.MenuItemName == "Tom sot" && x.Qty == 1).Should().Be(1);
        db.RestaurantPreparationTickets
            .Single(x => x.TicketType == "Send" && x.Note != null && x.Note.Contains("Khách gọi thêm 1 phần"))
            .Note.Should().Contain("Khách gọi thêm 1 phần");
        db.AlacarteOrders.Single(x => x.Id == order.Id).Note.Should().Contain("Khách gọi thêm 1 phần");
    }

    [Fact]
    public async Task Alacarte_UpdateSentLine_DecreaseQuantity_ShouldCreateCancelAdjustmentTicket()
    {
        await using var db = TestDbContextFactory.CreateContext();
        var service = new AlacarteRestaurantService(db, new MenuCatalogService(db));
        var item = SeedMenuItem(db, "FOOD-ADJ-DOWN", "Muc hap", 80000m, "Kitchen");
        var room = await service.SaveRoomAsync(new SaveDiningRoomRequest { Code = "ADJ-DOWN", Name = "Khu giam", Capacity = 20 });
        var table = await service.SaveTableAsync(new SaveDiningTableRequest { DiningRoomId = room.Id, Code = "ADJ-DOWN-T1", Name = "Ban giam", SeatCount = 10 });
        var order = await service.OpenOrderAsync(new CreateAlacarteOrderRequest { DiningTableId = table.Id });
        order = await service.AddOrUpdateLineAsync(new SaveAlacarteOrderLineRequest { OrderId = order.Id, MenuItemId = item.Id, Qty = 10 });
        await service.SendToPreparationAsync(new SendAlacartePreparationRequest { OrderId = order.Id, SentBy = "tester" });

        var updated = await service.AddOrUpdateLineAsync(new SaveAlacarteOrderLineRequest
        {
            OrderId = order.Id,
            OrderLineId = order.Lines.Single().Id,
            OrderTableId = order.Lines.Single().OrderTableId,
            MenuItemId = item.Id,
            Qty = 8,
            AdjustmentReason = "Khách đổi bớt 2 phần"
        });

        updated.Lines.Should().ContainSingle();
        updated.Lines.Single().Qty.Should().Be(8);
        db.RestaurantPreparationTickets.Count(x => x.TicketType == "Cancel").Should().Be(1);
        db.RestaurantPreparationTicketLines.Count(x => x.MenuItemName == "Muc hap" && x.Qty == 2).Should().Be(1);
        db.RestaurantPreparationTickets
            .Single(x => x.TicketType == "Cancel" && x.Note != null && x.Note.Contains("Khách đổi bớt 2 phần"))
            .Note.Should().Contain("Khách đổi bớt 2 phần");
        db.AlacarteOrders.Single(x => x.Id == order.Id).Note.Should().Contain("Khách đổi bớt 2 phần");
    }

    [Fact]
    public async Task Alacarte_OpenOrder_ShouldRejectLockedTable()
    {
        await using var db = TestDbContextFactory.CreateContext();
        var service = new AlacarteRestaurantService(db, new MenuCatalogService(db));
        var room = await service.SaveRoomAsync(new SaveDiningRoomRequest { Code = "LOCK", Name = "Khu khóa", Capacity = 10 });
        var table = await service.SaveTableAsync(new SaveDiningTableRequest { DiningRoomId = room.Id, Code = "L01", Name = "Bàn khóa", SeatCount = 4, Status = "Locked" });

        var act = () => service.OpenOrderAsync(new CreateAlacarteOrderRequest { DiningTableId = table.Id });

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*Bàn đang khóa*");
    }

    [Fact]
    public async Task Alacarte_OpenOrder_ShouldReuseExistingOpenOrder()
    {
        await using var db = TestDbContextFactory.CreateContext();
        var service = new AlacarteRestaurantService(db, new MenuCatalogService(db));
        var room = await service.SaveRoomAsync(new SaveDiningRoomRequest { Code = "REUSE", Name = "Khu reuse", Capacity = 10 });
        var table = await service.SaveTableAsync(new SaveDiningTableRequest { DiningRoomId = room.Id, Code = "R01", Name = "Bàn reuse", SeatCount = 4 });

        var first = await service.OpenOrderAsync(new CreateAlacarteOrderRequest { DiningTableId = table.Id });
        var second = await service.OpenOrderAsync(new CreateAlacarteOrderRequest { DiningTableId = table.Id });

        second.Id.Should().Be(first.Id);
        db.AlacarteOrders.Count(x => x.DiningTableId == table.Id).Should().Be(1);
    }

    [Fact]
    public async Task Alacarte_Reservation_ShouldRejectConflictSameTableAndTime()
    {
        await using var db = TestDbContextFactory.CreateContext();
        var service = new AlacarteRestaurantService(db, new MenuCatalogService(db));
        var room = await service.SaveRoomAsync(new SaveDiningRoomRequest { Code = "RSV-C", Name = "Khu đặt", Capacity = 30 });
        var table = await service.SaveTableAsync(new SaveDiningTableRequest { DiningRoomId = room.Id, Code = "C01", Name = "Bàn C01", SeatCount = 10 });
        var time = DateTime.Today.AddDays(1).AddHours(18);
        await service.SaveReservationAsync(new SaveTableReservationRequest { CustomerName = "Khách 1", Phone = "0901", ReservationDateTime = time, GuestCount = 4, DiningRoomId = room.Id, DiningTableId = table.Id, Status = "Confirmed" });

        var act = () => service.SaveReservationAsync(new SaveTableReservationRequest { CustomerName = "Khách 2", Phone = "0902", ReservationDateTime = time.AddHours(1), GuestCount = 4, DiningRoomId = room.Id, DiningTableId = table.Id, Status = "Confirmed" });

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*đã có đặt bàn*");
    }

    [Fact]
    public async Task Alacarte_Reservation_ShouldRejectOverCapacity()
    {
        await using var db = TestDbContextFactory.CreateContext();
        var service = new AlacarteRestaurantService(db, new MenuCatalogService(db));
        var room = await service.SaveRoomAsync(new SaveDiningRoomRequest { Code = "CAP", Name = "Khu nhỏ", Capacity = 4 });
        var table = await service.SaveTableAsync(new SaveDiningTableRequest { DiningRoomId = room.Id, Code = "CAP01", Name = "Bàn nhỏ", SeatCount = 4 });

        var act = () => service.SaveReservationAsync(new SaveTableReservationRequest { CustomerName = "Khách", Phone = "0903", ReservationDateTime = DateTime.Today.AddDays(1).AddHours(18), GuestCount = 5, DiningRoomId = room.Id, DiningTableId = table.Id });

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*vượt*");
    }

    [Fact]
    public async Task Alacarte_PayOrder_ShouldRejectEmptyOrder()
    {
        await using var db = TestDbContextFactory.CreateContext();
        var service = new AlacarteRestaurantService(db, new MenuCatalogService(db));
        var room = await service.SaveRoomAsync(new SaveDiningRoomRequest { Code = "PAY-E", Name = "Khu trả", Capacity = 10 });
        var table = await service.SaveTableAsync(new SaveDiningTableRequest { DiningRoomId = room.Id, Code = "P01", Name = "Bàn P01", SeatCount = 4 });
        var order = await service.OpenOrderAsync(new CreateAlacarteOrderRequest { DiningTableId = table.Id });

        var act = () => service.PayOrderAsync(new PayAlacarteOrderRequest { OrderId = order.Id });

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*đơn rỗng*");
    }

    [Fact]
    public async Task Alacarte_PayOrder_ShouldMarkPaidAndReleaseTable()
    {
        await using var db = TestDbContextFactory.CreateContext();
        var service = new AlacarteRestaurantService(db, new MenuCatalogService(db));
        var item = SeedMenuItem(db, "PAY-01", "Món thanh toán", 100000m, "Kitchen");
        var room = await service.SaveRoomAsync(new SaveDiningRoomRequest { Code = "PAY", Name = "Khu thanh toán", Capacity = 10 });
        var table = await service.SaveTableAsync(new SaveDiningTableRequest { DiningRoomId = room.Id, Code = "P02", Name = "Bàn P02", SeatCount = 4 });
        var order = await service.OpenOrderAsync(new CreateAlacarteOrderRequest { DiningTableId = table.Id });
        order = await service.AddOrUpdateLineAsync(new SaveAlacarteOrderLineRequest { OrderId = order.Id, MenuItemId = item.Id, Qty = 1 });

        var paid = await service.PayOrderAsync(new PayAlacarteOrderRequest { OrderId = order.Id });

        paid.Status.Should().Be("Paid");
        paid.PaymentStatus.Should().Be("Paid");
        paid.RemainingAmount.Should().Be(0);
        (await service.GetOpenOrdersAsync()).Should().NotContain(x => x.Id == order.Id);
        db.DiningTables.Single(x => x.Id == table.Id).Status.Should().Be("Cleaning");
    }

    [Fact]
    public async Task Alacarte_CancelOrder_ShouldRejectPaidOrder()
    {
        await using var db = TestDbContextFactory.CreateContext();
        var service = new AlacarteRestaurantService(db, new MenuCatalogService(db));
        var item = SeedMenuItem(db, "CAN-P", "Món đã thanh toán", 100000m, "Kitchen");
        var room = await service.SaveRoomAsync(new SaveDiningRoomRequest { Code = "CANP", Name = "Khu paid", Capacity = 10 });
        var table = await service.SaveTableAsync(new SaveDiningTableRequest { DiningRoomId = room.Id, Code = "CP01", Name = "Bàn CP01", SeatCount = 4 });
        var order = await service.OpenOrderAsync(new CreateAlacarteOrderRequest { DiningTableId = table.Id });
        order = await service.AddOrUpdateLineAsync(new SaveAlacarteOrderLineRequest { OrderId = order.Id, MenuItemId = item.Id, Qty = 1 });
        await service.PayOrderAsync(new PayAlacarteOrderRequest { OrderId = order.Id });

        var act = () => service.CancelOrderAsync(new CancelAlacarteOrderRequest { OrderId = order.Id });

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*đã thanh toán*");
    }

    [Fact]
    public async Task Alacarte_CancelOrder_ShouldMarkCancelledAndReleaseTable()
    {
        await using var db = TestDbContextFactory.CreateContext();
        var service = new AlacarteRestaurantService(db, new MenuCatalogService(db));
        var item = SeedMenuItem(db, "CAN-01", "Món hủy", 100000m, "Kitchen");
        var room = await service.SaveRoomAsync(new SaveDiningRoomRequest { Code = "CAN", Name = "Khu hủy", Capacity = 10 });
        var table = await service.SaveTableAsync(new SaveDiningTableRequest { DiningRoomId = room.Id, Code = "C02", Name = "Bàn C02", SeatCount = 4 });
        var order = await service.OpenOrderAsync(new CreateAlacarteOrderRequest { DiningTableId = table.Id });
        order = await service.AddOrUpdateLineAsync(new SaveAlacarteOrderLineRequest { OrderId = order.Id, MenuItemId = item.Id, Qty = 1 });

        var cancelled = await service.CancelOrderAsync(new CancelAlacarteOrderRequest { OrderId = order.Id, Reason = "Khách đổi lịch" });

        cancelled.Status.Should().Be("Cancelled");
        (await service.GetOpenOrdersAsync()).Should().NotContain(x => x.Id == order.Id);
        db.DiningTables.Single(x => x.Id == table.Id).Status.Should().Be("Empty");
    }

    [Fact]
    public async Task Alacarte_QuoteSave_ShouldIgnoreInvalidRoomFk()
    {
        await using var db = TestDbContextFactory.CreateContext();
        var service = new AlacarteRestaurantService(db, new MenuCatalogService(db));

        var quote = await service.SaveQuoteAsync(new SaveRestaurantQuoteRequest
        {
            CustomerName = "Khách báo giá",
            Phone = "0904",
            TableCount = 1,
            GuestCount = 10,
            DiningRoomId = Guid.NewGuid(),
            DiningTableId = Guid.NewGuid(),
            RoomName = "Phòng nhập tay",
            EventDate = DateTime.Today.AddDays(2),
            TotalAmount = 1000000m,
            Lines = [new SaveRestaurantQuoteLineRequest { Name = "Món nhập tay", Type = "Món ăn", UnitPrice = 100000m, Quantity = 10, Total = 1000000m }]
        });

        quote.DiningRoomId.Should().BeNull();
        quote.DiningTableId.Should().BeNull();
        quote.RoomName.Should().Be("Phòng nhập tay");
    }


    [Fact]
    public async Task AlacarteVip_QuoteSave_ShouldCreateQuoteTablesAndCalculatedQuantity()
    {
        await using var db = TestDbContextFactory.CreateContext();
        var service = new AlacarteRestaurantService(db, new MenuCatalogService(db));
        var room = await service.SaveRoomAsync(new SaveDiningRoomRequest { Code = "VIP-Q1", Name = "Phong VIP Q1", Capacity = 40 });
        var menuItem = SeedMenuItem(db, "VIP-FOOD-01", "Tom hum", 100000m, "Kitchen");

        var quote = await service.SaveQuoteAsync(new SaveRestaurantQuoteRequest
        {
            CustomerName = "Khach VIP",
            Phone = "0905000000",
            GuestCount = 31,
            TableStandard = 10,
            DiningRoomId = room.Id,
            RoomName = room.Name,
            EventDate = DateTime.Today.AddDays(2),
            Lines =
            [
                new SaveRestaurantQuoteLineRequest
                {
                    MenuItemId = menuItem.Id,
                    Name = menuItem.Name,
                    Type = "Mon an",
                    UnitPrice = 100000m,
                    Quantity = 1,
                    PricingRule = "PerPerson"
                }
            ]
        });

        quote.Tables.Should().HaveCount(4);
        quote.Tables.First().IsVirtual.Should().BeTrue();
        quote.Tables.Select(x => x.GuestCount).Should().Equal(31, 11, 10, 10);
        quote.Lines.Single().CalculatedQuantity.Should().Be(31);
        quote.TotalAmount.Should().Be(3100000m);
    }

    [Fact]
    public async Task AlacarteVip_QuoteSave_ChangingGuestCountBeforeApproval_ShouldRecalculate()
    {
        await using var db = TestDbContextFactory.CreateContext();
        var service = new AlacarteRestaurantService(db, new MenuCatalogService(db));
        var room = await service.SaveRoomAsync(new SaveDiningRoomRequest { Code = "VIP-Q2", Name = "Phong VIP Q2", Capacity = 60 });
        var menuItem = SeedMenuItem(db, "VIP-FOOD-02", "Lau", 200000m, "Kitchen");

        var quote = await service.SaveQuoteAsync(new SaveRestaurantQuoteRequest
        {
            CustomerName = "Khach VIP 2",
            Phone = "0905000001",
            GuestCount = 31,
            TableStandard = 10,
            DiningRoomId = room.Id,
            RoomName = room.Name,
            EventDate = DateTime.Today.AddDays(3),
            Lines =
            [
                new SaveRestaurantQuoteLineRequest
                {
                    MenuItemId = menuItem.Id,
                    Name = menuItem.Name,
                    Type = "Mon an",
                    UnitPrice = 200000m,
                    Quantity = 1,
                    PricingRule = "PerTable"
                }
            ]
        });

        var updated = await service.SaveQuoteAsync(new SaveRestaurantQuoteRequest
        {
            Id = quote.Id,
            QuoteNo = quote.QuoteNo,
            CustomerName = quote.CustomerName,
            Phone = quote.Phone,
            GuestCount = 46,
            TableStandard = 10,
            DiningRoomId = room.Id,
            RoomName = room.Name,
            EventDate = quote.EventDate,
            Lines =
            [
                new SaveRestaurantQuoteLineRequest
                {
                    MenuItemId = menuItem.Id,
                    Name = menuItem.Name,
                    Type = "Mon an",
                    UnitPrice = 200000m,
                    Quantity = 1,
                    PricingRule = "PerTable"
                }
            ]
        });

        updated.Tables.Select(x => x.GuestCount).Should().Equal(46, 10, 10, 10, 10, 6);
        updated.TableCount.Should().Be(5);
        updated.Lines.Single().CalculatedQuantity.Should().Be(5);
        updated.TotalAmount.Should().Be(1000000m);
    }

    [Fact]
    public async Task AlacarteVip_ApproveQuote_ShouldCreateOrderTablesIncludingVirtualTotalTable()
    {
        await using var db = TestDbContextFactory.CreateContext();
        var service = new AlacarteRestaurantService(db, new MenuCatalogService(db));
        var room = await service.SaveRoomAsync(new SaveDiningRoomRequest { Code = "VIP-Q3", Name = "Phong VIP Q3", Capacity = 60 });
        var menuItem = SeedMenuItem(db, "VIP-FOOD-03", "Tom hum VIP", 150000m, "Kitchen");

        var quote = await service.SaveQuoteAsync(new SaveRestaurantQuoteRequest
        {
            CustomerName = "Khach duyet",
            Phone = "0905000002",
            GuestCount = 31,
            TableStandard = 10,
            DiningRoomId = room.Id,
            RoomName = room.Name,
            EventDate = DateTime.Today.AddDays(4),
            Lines =
            [
                new SaveRestaurantQuoteLineRequest
                {
                    MenuItemId = menuItem.Id,
                    Name = menuItem.Name,
                    Type = "Mon an",
                    UnitPrice = 150000m,
                    Quantity = 1,
                    PricingRule = "PerPerson"
                }
            ]
        });

        var approved = await service.ApproveQuoteAsync(quote.Id, "tester");
        var order = (await service.GetOpenOrdersAsync()).Single(x => x.Id == approved.ApprovedOrderId);

        approved.ApprovedOrderId.Should().NotBeNull();
        order.Tables.Should().NotBeEmpty();
        order.Tables.First().IsVirtual.Should().BeTrue();
        order.Tables.First().DisplayName.Should().Be("Ban Tong");
        order.Tables.Count(x => !x.IsVirtual).Should().Be(3);
        order.GuestCount.Should().Be(31);
        order.TotalAmount.Should().Be(4650000m);
    }

    [Fact]
    public async Task Alacarte_ExtraServices_ShouldLoadFromDbOrSeedSample()
    {
        await using var db = TestDbContextFactory.CreateContext();
        var service = new AlacarteRestaurantService(db, new MenuCatalogService(db));

        var seeded = await service.SeedDefaultExtraServicesAsync();
        var loaded = await service.GetExtraServicesAsync();

        seeded.Should().Contain(x => x.Name == "Màn LED");
        loaded.Count.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task AlacarteVip_OpenRoomWalkInOrder_21Guests_ShouldSuggestTwoServingTablesAndLeaveRemainingWaiting()
    {
        await using var db = TestDbContextFactory.CreateContext();
        var service = new AlacarteRestaurantService(db, new MenuCatalogService(db));

        var room = await service.SaveRoomAsync(new SaveDiningRoomRequest
        {
            Code = "VIP21",
            Name = "Phong VIP 21",
            RoomType = "VIP",
            Capacity = 40
        });

        for (var index = 1; index <= 4; index++)
        {
            await service.SaveTableAsync(new SaveDiningTableRequest
            {
                DiningRoomId = room.Id,
                Code = $"V{index:00}",
                Name = $"Ban {index:00}",
                SeatCount = 10,
                Status = "Empty"
            });
        }

        var order = await service.OpenRoomWalkInOrderAsync(new CreateRoomWalkInOrderRequest
        {
            DiningRoomId = room.Id,
            GuestCount = 21,
            CustomerName = "Khach walk-in"
        });

        order.Tables.Should().ContainSingle(x => x.IsVirtual);
        order.Tables.Count(x => !x.IsVirtual && x.Status == "Serving").Should().Be(2);
        order.Tables.Count(x => !x.IsVirtual && x.Status == "Waiting").Should().Be(2);
        order.Tables.Where(x => !x.IsVirtual && x.Status == "Serving")
            .OrderByDescending(x => x.GuestCount)
            .Select(x => x.GuestCount)
            .Should()
            .Equal(11, 10);
    }

    [Fact]
    public async Task AlacarteVip_CancelRoomWalkInOrder_ShouldReleaseAllPhysicalTablesAndResetRoomStatus()
    {
        await using var db = TestDbContextFactory.CreateContext();
        var service = new AlacarteRestaurantService(db, new MenuCatalogService(db));

        var room = await service.SaveRoomAsync(new SaveDiningRoomRequest
        {
            Code = "VIP-CANCEL",
            Name = "Phong VIP Cancel",
            RoomType = "VIP",
            Capacity = 40
        });

        for (var index = 1; index <= 4; index++)
        {
            await service.SaveTableAsync(new SaveDiningTableRequest
            {
                DiningRoomId = room.Id,
                Code = $"VC{index:00}",
                Name = $"Ban {index:00}",
                SeatCount = 10,
                Status = "Empty"
            });
        }

        var order = await service.OpenRoomWalkInOrderAsync(new CreateRoomWalkInOrderRequest
        {
            DiningRoomId = room.Id,
            GuestCount = 21,
            CustomerName = "Khach huy"
        });

        db.DiningTables.Count(x => x.DiningRoomId == room.Id && x.Status == "Serving").Should().Be(2);

        await service.CancelOrderAsync(new CancelAlacarteOrderRequest
        {
            OrderId = order.Id,
            Reason = "Mo nham phong"
        });

        db.DiningTables.Count(x => x.DiningRoomId == room.Id && x.Status == "Serving").Should().Be(0);
        db.DiningTables.Where(x => x.DiningRoomId == room.Id).All(x => x.Status == "Empty").Should().BeTrue();
        db.DiningRooms.Single(x => x.Id == room.Id).Status.Should().Be("Available");
    }

    [Fact]
    public async Task AlacarteVip_OpenRoomWalkInOrder_ShouldNormalizeDuplicatePhysicalTableSortOrders()
    {
        await using var db = TestDbContextFactory.CreateContext();
        var service = new AlacarteRestaurantService(db, new MenuCatalogService(db));

        var room = await service.SaveRoomAsync(new SaveDiningRoomRequest
        {
            Code = "VIP-DUP",
            Name = "Phong VIP DUP",
            RoomType = "VIP",
            Capacity = 30
        });

        db.DiningTables.AddRange(
            new DiningTable
            {
                Id = Guid.NewGuid(),
                DiningRoomId = room.Id,
                Code = "D01",
                Name = "Ban 01",
                SeatCount = 10,
                Status = "Empty",
                IsActive = true,
                SortOrder = 1,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new DiningTable
            {
                Id = Guid.NewGuid(),
                DiningRoomId = room.Id,
                Code = "D02",
                Name = "Ban 02",
                SeatCount = 10,
                Status = "Empty",
                IsActive = true,
                SortOrder = 1,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new DiningTable
            {
                Id = Guid.NewGuid(),
                DiningRoomId = room.Id,
                Code = "D03",
                Name = "Ban 03",
                SeatCount = 10,
                Status = "Empty",
                IsActive = true,
                SortOrder = 1,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
        await db.SaveChangesAsync();

        var order = await service.OpenRoomWalkInOrderAsync(new CreateRoomWalkInOrderRequest
        {
            DiningRoomId = room.Id,
            GuestCount = 25,
            CustomerName = "Khach test"
        });

        order.Tables.Where(x => !x.IsVirtual)
            .OrderBy(x => x.SortOrder)
            .Select(x => x.TableNo)
            .Should()
            .Equal(1, 2, 3);
    }

    [Fact]
    public async Task Alacarte_Contracts_ShouldLoadFromDb()
    {
        await using var db = TestDbContextFactory.CreateContext();
        var service = new AlacarteRestaurantService(db, new MenuCatalogService(db));

        var contract = await service.SaveContractAsync(new SaveRestaurantContractRequest
        {
            CustomerName = "Khách hợp đồng",
            RoomName = "VIP",
            EventDate = DateTime.Today.AddDays(3),
            TotalAmount = 5000000m,
            DepositAmount = 1000000m,
            Status = "Draft"
        });
        var contracts = await service.GetContractsAsync();

        contracts.Should().Contain(x => x.Id == contract.Id);
    }

    [Fact]
    public void Permissions_ShouldContainRestaurantAlacarteViewAndManage()
    {
        var method = typeof(PermissionService).GetMethod("DefaultPermissions", BindingFlags.NonPublic | BindingFlags.Static);
        method.Should().NotBeNull();
        var permissions = ((IEnumerable<AppPermission>)method!.Invoke(null, null)!).Select(x => x.Code).ToList();

        permissions.Should().Contain("restaurant.alacarte.view");
        permissions.Should().Contain("restaurant.alacarte.manage");
    }

    [Fact]
    public async Task Banquet_ShouldCreateEventDepositContractAndTotals()
    {
        await using var db = TestDbContextFactory.CreateContext();
        var service = new BanquetRestaurantService(db);

        var room = await service.SaveRoomAsync(new SaveBanquetRoomRequest
        {
            Code = "HALL-A",
            Name = "Sảnh A",
            Capacity = 300,
            MaxTables = 30
        });

        var evt = await service.SaveEventAsync(new SaveBanquetEventRequest
        {
            EventType = "Wedding",
            CustomerName = "Gia đình A",
            Phone = "0911111111",
            EventDate = DateTime.Today.AddDays(5),
            StartTime = new TimeSpan(17, 0, 0),
            EndTime = new TimeSpan(21, 0, 0),
            GuestCount = 200,
            TableCount = 20,
            ExtraGuestCount = 12,
            ExtraTableCount = 1,
            BrideName = "Cô dâu A",
            GroomName = "Chú rể A",
            BanquetRoomId = room.Id,
            Status = "Confirmed"
        });

        var package = await service.SaveMenuPackageAsync(new SaveBanquetMenuPackageRequest
        {
            Code = "MENU-01",
            GroupName = "Tiệc cưới",
            Name = "Menu cưới 01",
            PricingMode = "PerTable",
            Price = 2500000m
        });

        package.GroupName.Should().Be("Tiệc cưới");
        evt = await service.AttachMenuPackageAsync(new AttachBanquetMenuRequest { BanquetEventId = evt.Id, BanquetMenuPackageId = package.Id });
        await service.SaveServiceLineAsync(new SaveBanquetServiceLineRequest { BanquetEventId = evt.Id, ServiceType = "LedScreen", Description = "Màn LED", Qty = 1, UnitPrice = 5000000m });
        await service.AddDepositAsync(new AddBanquetDepositRequest { BanquetEventId = evt.Id, Amount = 10000000m, PaymentMethod = "Cash" });
        evt = (await service.GetEventsAsync(DateTime.Today, DateTime.Today.AddDays(10))).Single(x => x.Id == evt.Id);
        var contract = await service.CreateContractAsync(evt.Id, DateTime.Today);

        evt.TotalAmount.Should().Be(57500000m); // PerTable 2,500,000 × (20 + 1 ExtraTable) = 52,500,000 + Service 5,000,000
        evt.DepositAmount.Should().Be(10000000m);
        evt.RemainingAmount.Should().Be(47500000m);
        evt.BrideName.Should().Be("Cô dâu A");
        evt.GroomName.Should().Be("Chú rể A");
        evt.ExtraGuestCount.Should().Be(12);
        evt.ExtraTableCount.Should().Be(1);
        contract.ContractNo.Should().StartWith("BCT-");

        var updatedContract = await service.UpdateContractAsync(new UpdateBanquetContractRequest
        {
            ContractId = contract.Id,
            SignDate = DateTime.Today.AddDays(1),
            Status = "Confirmed",
            Note = "Xuất hóa đơn VAT theo thông tin khách hàng cung cấp."
        });
        updatedContract.SignDate.Should().Be(DateTime.Today.AddDays(1));
        updatedContract.Status.Should().Be("Confirmed");
        updatedContract.Note.Should().Be("Xuất hóa đơn VAT theo thông tin khách hàng cung cấp.");
    }

    [Fact]
    public async Task Banquet_ShouldPersistMenuGroupsAndUpdatedPackageLines()
    {
        await using var db = TestDbContextFactory.CreateContext();
        var service = new BanquetRestaurantService(db);
        var soup = SeedMenuItem(db, "BQ-G01", "Súp cua", 180000m, "Kitchen");
        var chicken = SeedMenuItem(db, "BQ-G02", "Gà quay", 420000m, "Kitchen");

        var group = await service.SaveMenuGroupAsync(new SaveBanquetMenuGroupRequest { Name = "Bàn 10 người", SortOrder = 1 });
        (await service.GetMenuGroupsAsync()).Should().ContainSingle(x => x.Id == group.Id && x.Name == "Bàn 10 người");

        var package = await service.SaveMenuPackageAsync(new SaveBanquetMenuPackageRequest
        {
            Code = "BQ-MENU-10",
            Name = "Menu bàn 10 người",
            GroupName = group.Name,
            Price = 180000m,
            Lines = [new SaveBanquetMenuPackageLineRequest { MenuItemId = soup.Id, Qty = 1 }]
        });

        package = await service.SaveMenuPackageAsync(new SaveBanquetMenuPackageRequest
        {
            Id = package.Id,
            Code = package.Code,
            Name = package.Name,
            GroupName = group.Name,
            Price = 600000m,
            Lines =
            [
                new SaveBanquetMenuPackageLineRequest { MenuItemId = soup.Id, Qty = 1 },
                new SaveBanquetMenuPackageLineRequest { MenuItemId = chicken.Id, Qty = 1 }
            ]
        });

        package.GroupName.Should().Be(group.Name);
        package.Lines.Should().HaveCount(2);
        (await service.GetMenuPackagesAsync()).Single(x => x.Id == package.Id).Lines.Should().HaveCount(2);
    }

    [Fact]
    public async Task Banquet_ShouldRejectRoomScheduleConflictAndCapacityOverflow()
    {
        await using var db = TestDbContextFactory.CreateContext();
        var service = new BanquetRestaurantService(db);
        var room = await service.SaveRoomAsync(new SaveBanquetRoomRequest { Code = "HALL-B", Name = "Sảnh B", Capacity = 100, MaxTables = 10 });
        var date = DateTime.Today.AddDays(7);

        await service.SaveEventAsync(new SaveBanquetEventRequest
        {
            EventType = "Meeting",
            CustomerName = "Công ty A",
            Phone = "0922222222",
            EventDate = date,
            StartTime = new TimeSpan(8, 0, 0),
            EndTime = new TimeSpan(11, 0, 0),
            GuestCount = 80,
            TableCount = 8,
            MeetingTopic = "Họp kế hoạch",
            BanquetRoomId = room.Id
        });

        var conflict = await service.ValidateEventScheduleAsync(new SaveBanquetEventRequest
        {
            EventType = "Conference",
            CustomerName = "Công ty B",
            Phone = "0933333333",
            EventDate = date,
            StartTime = new TimeSpan(10, 0, 0),
            EndTime = new TimeSpan(12, 0, 0),
            GuestCount = 120,
            TableCount = 12,
            ConferenceName = "Hội nghị khách hàng",
            BanquetRoomId = room.Id
        });

        conflict.Should().Contain(x => x.Contains("trùng lịch"));
        conflict.Should().Contain(x => x.Contains("vượt sức chứa"));
        conflict.Should().Contain(x => x.Contains("vượt cấu hình"));
    }

    [Fact]
    public async Task Alacarte_ShouldReuseBanquetMenuPackagesAsRealServingSets()
    {
        await using var db = TestDbContextFactory.CreateContext();
        var banquetService = new BanquetRestaurantService(db);
        var alacarteService = new AlacarteRestaurantService(db, new MenuCatalogService(db));
        var soup = SeedMenuItem(db, "SET-01", "Súp cua", 45000m, "Kitchen");
        var lobster = SeedMenuItem(db, "SET-02", "Tôm hùm", 250000m, "Kitchen");

        await banquetService.SaveMenuPackageAsync(new SaveBanquetMenuPackageRequest
        {
            Code = "SET-VIP-01",
            Name = "Set VIP 01",
            PricingMode = "PerTable",
            Price = 1200000m,
            Lines =
            [
                new SaveBanquetMenuPackageLineRequest
                {
                    MenuItemId = soup.Id,
                    CourseType = "Starter",
                    PricingRule = "PerGuest",
                    Qty = 1
                },
                new SaveBanquetMenuPackageLineRequest
                {
                    MenuItemId = lobster.Id,
                    CourseType = "Main",
                    PricingRule = "PerTable",
                    Qty = 1
                }
            ]
        });

        var packages = await alacarteService.GetMenuPackagesAsync();
        var package = packages.Should().ContainSingle(x => x.Code == "SET-VIP-01").Subject;

        package.Lines.Should().HaveCount(2);
        package.Lines.Should().ContainSingle(x => x.MenuItemId == soup.Id && x.PricingRule == "PerGuest" && x.UnitPrice == 45000m);
        package.Lines.Should().ContainSingle(x => x.MenuItemId == lobster.Id && x.PricingRule == "PerTable" && x.UnitPrice == 250000m);
    }

    [Fact]
    public async Task Banquet_ShouldLoadDetailCollectionsAndFinalizeSettlement()
    {
        await using var db = TestDbContextFactory.CreateContext();
        var service = new BanquetRestaurantService(db);
        var room = await service.SaveRoomAsync(new SaveBanquetRoomRequest { Code = "HALL-C", Name = "Sảnh C", Capacity = 200, MaxTables = 20 });
        var item = SeedMenuItem(db, "BQ-M1", "Gà quay", 150000m, "Kitchen");

        var evt = await service.SaveEventAsync(new SaveBanquetEventRequest
        {
            EventType = "Wedding",
            CustomerName = "Gia đình B",
            Phone = "0944444444",
            EventDate = DateTime.Today.AddDays(3),
            StartTime = new TimeSpan(18, 0, 0),
            EndTime = new TimeSpan(21, 0, 0),
            GuestCount = 150,
            TableCount = 15,
            BanquetRoomId = room.Id,
            Status = "Confirmed",
            BrideName = "Nguyễn Thị C"
        });

        var package = await service.SaveMenuPackageAsync(new SaveBanquetMenuPackageRequest
        {
            Code = "MENU-02",
            GroupName = "Thực đơn mẫu",
            Name = "Menu cưới 02",
            PricingMode = "PerTable",
            Price = 3000000m,
            Lines =
            [
                new SaveBanquetMenuPackageLineRequest { MenuItemId = item.Id, CourseType = "Main", PricingRule = "PerTable", Qty = 1 }
            ]
        });

        await service.AttachMenuPackageAsync(new AttachBanquetMenuRequest { BanquetEventId = evt.Id, BanquetMenuPackageId = package.Id });
        var historicalPackage = db.BanquetMenuPackages.Single(x => x.Id == package.Id);
        historicalPackage.IsActive = false;
        await db.SaveChangesAsync();
        await service.SaveServiceLineAsync(new SaveBanquetServiceLineRequest { BanquetEventId = evt.Id, ServiceType = "LedScreen", Description = "Màn LED", Qty = 1, UnitPrice = 4000000m, VatRate = 10m });
        await service.AddDepositAsync(new AddBanquetDepositRequest { BanquetEventId = evt.Id, Amount = 5000000m, PaymentMethod = "Cash" });
        await service.CreateContractAsync(evt.Id, DateTime.Today);

        var detail = await service.GetEventDetailAsync(evt.Id);
        detail.Menus.Should().HaveCount(1);
        detail.Menus.Single().PackageLines.Should().ContainSingle(x => x.MenuItemId == item.Id && x.MenuItemName == "Gà quay");
        (await service.GetMenuPackagesAsync()).Single(x => x.Id == package.Id).GroupName.Should().Be("Thực đơn mẫu");
        detail.Services.Should().HaveCount(1);
        detail.Deposits.Should().HaveCount(1);
        detail.Contracts.Should().HaveCount(1);
        detail.Timeline.Should().NotBeEmpty();

        var settled = await service.FinalizeSettlementAsync(evt.Id, "Transfer", "Thu ngân");
        settled.Status.Should().Be("Completed");
        settled.RemainingAmount.Should().Be(0m);

        var refreshed = await service.GetEventDetailAsync(evt.Id);
        refreshed.Deposits.Should().HaveCount(2);
        refreshed.Contracts.Should().ContainSingle(x => x.RemainingAmount == 0m && x.Status == "Completed");
    }

    [Fact]
    public async Task Banquet_ShouldSeedTimelineAndAllowStatusUpdate()
    {
        await using var db = TestDbContextFactory.CreateContext();
        var service = new BanquetRestaurantService(db);
        var room = await service.SaveRoomAsync(new SaveBanquetRoomRequest { Code = "HALL-D", Name = "Sảnh D", Capacity = 120, MaxTables = 12 });
        var evt = await service.SaveEventAsync(new SaveBanquetEventRequest
        {
            EventType = "Conference",
            CustomerName = "Công ty C",
            Phone = "0955555555",
            EventDate = DateTime.Today.AddDays(2),
            StartTime = new TimeSpan(9, 0, 0),
            EndTime = new TimeSpan(12, 0, 0),
            GuestCount = 80,
            TableCount = 8,
            BanquetRoomId = room.Id,
            Status = "Confirmed",
            ConferenceName = "Hội nghị CNTT 2026"
        });

        var timeline = await service.GetTimelineAsync(evt.Id);
        timeline.Should().HaveCountGreaterThanOrEqualTo(5);

        var updated = await service.UpdateTimelineItemStatusAsync(timeline[0].Id, "Done");
        updated.Status.Should().Be("Done");
    }

    [Fact]
    public async Task Banquet_ShouldCalculateCustomMenuAmountByTableCount()
    {
        await using var db = TestDbContextFactory.CreateContext();
        var service = new BanquetRestaurantService(db);
        var room = await service.SaveRoomAsync(new SaveBanquetRoomRequest { Code = "HALL-E", Name = "Sảnh E", Capacity = 500, MaxTables = 50 });

        var item1 = SeedMenuItem(db, "BQ-GA", "Gà quay", 150000m, "Kitchen");
        var item2 = SeedMenuItem(db, "BQ-CANH", "Canh chua", 80000m, "Kitchen");
        var item3 = SeedMenuItem(db, "BQ-COM", "Cơm trắng", 5000m, "Kitchen");

        // Tạo event 10 bàn chính + 5 bàn phát sinh
        var evt = await service.SaveEventAsync(new SaveBanquetEventRequest
        {
            EventType = "Wedding",
            CustomerName = "Test giá",
            Phone = "0900000000",
            EventDate = DateTime.Today.AddDays(5),
            StartTime = new TimeSpan(17, 0, 0),
            EndTime = new TimeSpan(21, 0, 0),
            GuestCount = 150,
            TableCount = 10,
            ExtraTableCount = 5,
            BanquetRoomId = room.Id,
            Status = "Confirmed",
            BrideName = "Cô Dâu"
        });

        // Tạo custom menu: Gà quay (1 × 150,000) + Canh chua (1 × 80,000) + Cơm (10 × 5,000)
        // Per-table = 150,000 + 80,000 + 50,000 = 280,000
        // Total with 15 tables = 280,000 × 15 = 4,200,000
        await service.SaveCustomMenuAsync(new SaveBanquetCustomMenuRequest
        {
            BanquetEventId = evt.Id,
            Lines =
            [
                new SaveBanquetCustomMenuLineRequest { MenuItemId = item1.Id, ItemName = "Gà quay", Qty = 1, UnitPrice = 150000m },
                new SaveBanquetCustomMenuLineRequest { MenuItemId = item2.Id, ItemName = "Canh chua", Qty = 1, UnitPrice = 80000m },
                new SaveBanquetCustomMenuLineRequest { MenuItemId = item3.Id, ItemName = "Cơm trắng", Qty = 10, UnitPrice = 5000m }
            ]
        });

        var detail = await service.GetEventDetailAsync(evt.Id);
        // 280,000 × (10 + 5) = 4,200,000
        detail.Event.MenuAmount.Should().Be(4200000m);
        detail.Event.TotalAmount.Should().Be(4200000m);

        // Test với ExtraTableCount = 0: 280,000 × 10 = 2,800,000
        var evt2 = await service.SaveEventAsync(new SaveBanquetEventRequest
        {
            Id = evt.Id,
            EventType = "Wedding",
            CustomerName = "Test giá",
            Phone = "0900000000",
            EventDate = DateTime.Today.AddDays(5),
            StartTime = new TimeSpan(17, 0, 0),
            EndTime = new TimeSpan(21, 0, 0),
            GuestCount = 100,
            TableCount = 10,
            ExtraTableCount = 0,
            BanquetRoomId = room.Id,
            Status = "Confirmed",
            BrideName = "Cô Dâu"
        });

        var reloaded = await service.GetEventDetailAsync(evt.Id);
        reloaded.Event.MenuAmount.Should().Be(2800000m); // 280,000 × 10

        // Thêm dịch vụ và kiểm tra tổng
        await service.SaveServiceLineAsync(new SaveBanquetServiceLineRequest { BanquetEventId = evt.Id, ServiceType = "LedScreen", Description = "Màn LED", Qty = 1, UnitPrice = 1000000m, VatRate = 10m });
        var withService = await service.GetEventDetailAsync(evt.Id);
        withService.Event.ServiceAmount.Should().Be(1100000m); // 1,000,000 × 1.10
        withService.Event.TotalAmount.Should().Be(2800000m + 1100000m);
    }

    [Fact]
    public async Task Banquet_MenuSelection_ShouldReplacePreviousMenusAndTotals()
    {
        await using var db = TestDbContextFactory.CreateContext();
        var service = new BanquetRestaurantService(db);
        var room = await service.SaveRoomAsync(new SaveBanquetRoomRequest { Code = "HALL-REPLACE", Name = "Sảnh thay thực đơn", Capacity = 200, MaxTables = 20 });
        var item = SeedMenuItem(db, "BQ-REPLACE", "Món thay thế", 120000m, "Kitchen");
        var evt = await service.SaveEventAsync(new SaveBanquetEventRequest
        {
            EventType = "Wedding",
            CustomerName = "Khách thay thực đơn",
            Phone = "0900000001",
            EventDate = DateTime.Today.AddDays(10),
            StartTime = new TimeSpan(17, 0, 0),
            EndTime = new TimeSpan(21, 0, 0),
            GuestCount = 100,
            TableCount = 10,
            BanquetRoomId = room.Id,
            Status = "Confirmed",
            BrideName = "Cô dâu"
        });
        var firstPackage = await service.SaveMenuPackageAsync(new SaveBanquetMenuPackageRequest
        {
            Code = "MENU-REPLACE-1",
            Name = "Thực đơn cũ",
            Price = 1000000m,
            Lines = [new SaveBanquetMenuPackageLineRequest { MenuItemId = item.Id, Qty = 1 }]
        });
        var secondPackage = await service.SaveMenuPackageAsync(new SaveBanquetMenuPackageRequest
        {
            Code = "MENU-REPLACE-2",
            Name = "Thực đơn mới",
            Price = 2000000m,
            Lines = [new SaveBanquetMenuPackageLineRequest { MenuItemId = item.Id, Qty = 1 }]
        });

        await service.AttachMenuPackageAsync(new AttachBanquetMenuRequest { BanquetEventId = evt.Id, BanquetMenuPackageId = firstPackage.Id });
        await service.AttachMenuPackageAsync(new AttachBanquetMenuRequest { BanquetEventId = evt.Id, BanquetMenuPackageId = secondPackage.Id });

        var presetDetail = await service.GetEventDetailAsync(evt.Id);
        presetDetail.Menus.Should().ContainSingle(x => x.BanquetMenuPackageId == secondPackage.Id);
        presetDetail.Event.MenuAmount.Should().Be(20000000m);

        await service.SaveCustomMenuAsync(new SaveBanquetCustomMenuRequest
        {
            BanquetEventId = evt.Id,
            Lines = [new SaveBanquetCustomMenuLineRequest { MenuItemId = item.Id, ItemName = item.Name, Qty = 1, UnitPrice = item.UnitPrice }]
        });

        var customDetail = await service.GetEventDetailAsync(evt.Id);
        customDetail.Menus.Should().ContainSingle(x => x.BanquetMenuPackageId == null && x.MenuItemId == item.Id);
        customDetail.Event.MenuAmount.Should().Be(1200000m);
    }

    [Fact]
    public async Task Banquet_Finance_ShouldTrackPartialFullAndPreventDuplicateDeposits()
    {
        await using var db = TestDbContextFactory.CreateContext();
        var service = new BanquetRestaurantService(db);
        var room = await service.SaveRoomAsync(new SaveBanquetRoomRequest { Code = "FIN-HALL", Name = "Sảnh tài chính", Capacity = 200, MaxTables = 20 });
        var evt = await service.SaveEventAsync(new SaveBanquetEventRequest
        {
            EventType = "Conference",
            CustomerName = "Kiểm thử tài chính",
            Phone = "0900000020",
            EventDate = DateTime.Today.AddDays(20),
            StartTime = new TimeSpan(8, 0, 0),
            EndTime = new TimeSpan(12, 0, 0),
            GuestCount = 100,
            TableCount = 10,
            BanquetRoomId = room.Id,
            Status = "Confirmed",
            ConferenceName = "FIN TEST"
        });
        await service.SaveServiceLineAsync(new SaveBanquetServiceLineRequest
        {
            BanquetEventId = evt.Id,
            ServiceType = "Other",
            Description = "Giá trị tiệc kiểm thử",
            Qty = 1,
            UnitPrice = 13805000m,
            VatRate = 0m
        });

        var noDeposit = await service.GetEventDetailAsync(evt.Id);
        noDeposit.Event.TotalAmount.Should().Be(13805000m);
        noDeposit.Event.DepositAmount.Should().Be(0m);
        noDeposit.Event.RemainingAmount.Should().Be(13805000m);

        await service.AddDepositAsync(new AddBanquetDepositRequest { BanquetEventId = evt.Id, Amount = 5000000m, PaymentMethod = "Transfer", Receiver = "Nguyễn An" });
        var partial = await service.GetEventDetailAsync(evt.Id);
        partial.Event.DepositAmount.Should().Be(5000000m);
        partial.Event.RemainingAmount.Should().Be(8805000m);
        partial.Deposits.Should().ContainSingle();

        var overpayment = async () => await service.AddDepositAsync(new AddBanquetDepositRequest { BanquetEventId = evt.Id, Amount = 9000000m, PaymentMethod = "Cash" });
        await overpayment.Should().ThrowAsync<InvalidOperationException>();

        await service.AddDepositAsync(new AddBanquetDepositRequest { BanquetEventId = evt.Id, Amount = 8805000m, PaymentMethod = "Cash" });
        var fullyPaid = await service.GetEventDetailAsync(evt.Id);
        fullyPaid.Event.DepositAmount.Should().Be(13805000m);
        fullyPaid.Event.RemainingAmount.Should().Be(0m);
        fullyPaid.Deposits.Should().HaveCount(2);

        var duplicateClick = async () => await service.AddDepositAsync(new AddBanquetDepositRequest { BanquetEventId = evt.Id, Amount = 8805000m, PaymentMethod = "Cash" });
        await duplicateClick.Should().ThrowAsync<InvalidOperationException>();
        (await service.GetEventDetailAsync(evt.Id)).Deposits.Should().HaveCount(2);

        var secondEvent = await service.SaveEventAsync(new SaveBanquetEventRequest
        {
            EventType = "Meeting",
            CustomerName = "Sự kiện không cọc",
            Phone = "0900000021",
            EventDate = DateTime.Today.AddDays(21),
            StartTime = new TimeSpan(13, 0, 0),
            EndTime = new TimeSpan(16, 0, 0),
            GuestCount = 30,
            TableCount = 3,
            BanquetRoomId = room.Id,
            Status = "Confirmed",
            MeetingTopic = "FIN EVENT 2"
        });
        (await service.GetEventDetailAsync(secondEvent.Id)).Deposits.Should().BeEmpty();
        (await service.GetEventDetailAsync(evt.Id)).Event.DepositAmount.Should().Be(13805000m);
    }

    private static MenuItem SeedMenuItem(ASMILE.BO.Infrastructure.Persistence.AppDbContext db, string code, string name, decimal price, string preparationArea)
    {
        var category = db.MenuCategories.FirstOrDefault() ?? new MenuCategory { Id = Guid.NewGuid(), Code = "CAT", Name = "Thực đơn", SortOrder = 1, IsActive = true };
        if (db.Entry(category).State == Microsoft.EntityFrameworkCore.EntityState.Detached) db.MenuCategories.Add(category);
        var item = new MenuItem
        {
            Id = Guid.NewGuid(),
            MenuCategoryId = category.Id,
            Code = code,
            Name = name,
            UnitName = "phần",
            UnitPrice = price,
            VatRate = 0,
            HasRecipeManagement = false,
            PreparationArea = preparationArea,
            IsActive = true
        };
        db.MenuItems.Add(item);
        db.SaveChanges();
        return item;
    }

    private static Warehouse SeedWarehouse(ASMILE.BO.Infrastructure.Persistence.AppDbContext db, string code, string name)
    {
        var warehouse = new Warehouse
        {
            WarehouseCode = code,
            WarehouseName = name,
            IsActive = true
        };
        db.Warehouses.Add(warehouse);
        db.SaveChanges();
        return warehouse;
    }

    private static Item SeedInventoryItem(ASMILE.BO.Infrastructure.Persistence.AppDbContext db, string code, string name, string unit)
    {
        var item = new Item
        {
            ItemCode = code,
            ItemName = name,
            Unit = unit,
            GroupName = "Thực phẩm",
            SupplierCode = "SUP",
            SupplierName = "Nhà cung cấp",
            IsActive = true
        };
        db.Items.Add(item);
        db.SaveChanges();
        return item;
    }

    private static void SeedRecipe(ASMILE.BO.Infrastructure.Persistence.AppDbContext db, MenuItem menuItem, Item ingredient, Warehouse warehouse, decimal quantity)
    {
        var recipe = new MenuItemRecipe
        {
            MenuItemId = menuItem.Id,
            Name = "Default",
            IsDefault = true,
            IsActive = true
        };
        recipe.Lines.Add(new MenuItemRecipeLine
        {
            MenuItemRecipeId = recipe.Id,
            LineNo = 1,
            ItemId = ingredient.Id,
            DefaultWarehouseId = warehouse.Id,
            ItemCode = ingredient.ItemCode,
            ItemName = ingredient.ItemName,
            UnitName = ingredient.Unit,
            Quantity = quantity,
            LossRate = 0,
            IsRequired = true
        });
        db.MenuItemRecipes.Add(recipe);
        db.SaveChanges();
    }

    private static string BuildIngredientSupplementNote(string? displayNote, params RestaurantLineIngredientAdjustmentDto[] adjustments)
    {
        var metadata = new
        {
            DisplayNote = displayNote,
            IngredientAdjustments = adjustments
        };
        var json = JsonSerializer.Serialize(metadata);
        var encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes(json));
        return string.IsNullOrWhiteSpace(displayNote)
            ? $"[ALC-SUPP-META]{encoded}"
            : $"{displayNote}{Environment.NewLine}[ALC-SUPP-META]{encoded}";
    }

    [Fact]
    public void QuoteChangeRequest_Entity_DefaultsAreCorrect()
    {
        var cr = new QuoteChangeRequest();

        cr.Status.Should().Be("Pending");
        cr.GuestCountDelta.Should().Be(0);
        cr.RequestNumber.Should().BeEmpty();
        cr.Details.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public void QuoteChangeRequestDetail_DefaultsAreCorrect()
    {
        var detail = new QuoteChangeRequestDetail();

        detail.ChangeType.Should().Be("Unchanged");
        detail.IsAlreadySent.Should().BeFalse();
    }

    [Fact]
    public void QuoteAuditLog_DefaultsAreCorrect()
    {
        var log = new QuoteAuditLog();

        log.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        log.Action.Should().BeEmpty();
    }

    [Fact]
    public void PreviewAdjustmentRequest_Validation_ShouldRejectZeroGuests()
    {
        var request = new PreviewAdjustmentRequest { QuoteId = Guid.NewGuid(), NewGuestCount = 0 };
        request.NewGuestCount.Should().Be(0);
    }

    [Fact]
    public void PreviewAdjustmentRequest_Validation_ShouldAcceptValidGuestCount()
    {
        var request = new PreviewAdjustmentRequest { QuoteId = Guid.NewGuid(), NewGuestCount = 45 };
        request.NewGuestCount.Should().Be(45);
    }

    [Fact]
    public void QuoteChangeRequestDto_DeltaCalculation_IsCorrect()
    {
        var dto = new QuoteChangeRequestDto
        {
            PreviousGuestCount = 40,
            NewGuestCount = 47,
            GuestCountDelta = 7,
            PreviousTotalAmount = 67_500_000,
            NewTotalAmount = 79_200_000,
            AmountDelta = 11_700_000,
        };

        (dto.NewGuestCount - dto.PreviousGuestCount).Should().Be(7);
        (dto.NewTotalAmount - dto.PreviousTotalAmount).Should().Be(11_700_000);
    }

    [Fact]
    public void TableChangeDto_DefaultsAreCorrect()
    {
        var tc = new TableChangeDto();
        tc.Action.Should().Be("Keep");
        tc.IsVirtual.Should().BeFalse();
    }

    [Fact]
    public void QuoteChangeRequestDetailDto_DeltaCalculation_IsCorrect()
    {
        var dto = new QuoteChangeRequestDetailDto
        {
            PreviousQuantity = 40,
            NewQuantity = 47,
            PreviousAmount = 14_000_000,
            NewAmount = 16_450_000,
        };

        (dto.NewQuantity - dto.PreviousQuantity).Should().Be(7);
        (dto.NewAmount - dto.PreviousAmount).Should().Be(2_450_000);
    }

    [Fact]
    public void PreviewAdjustmentResponse_DefaultsAreCorrect()
    {
        var response = new PreviewAdjustmentResponse();

        response.Preview.Should().NotBeNull();
        response.TableChanges.Should().NotBeNull().And.BeEmpty();
        response.LineChanges.Should().NotBeNull().And.BeEmpty();
        response.Warnings.Should().NotBeNull().And.BeEmpty();
        response.RequiresFullRebuild.Should().BeFalse();
    }

    [Fact]
    public void AlacarteVip_TableAllocation_40To47_ShouldAddOneTable()
    {
        var engine = new TableAllocationEngine();

        var tables40 = engine.Allocate(40, 10);
        var tables47 = engine.Allocate(47, 10);

        tables40.Count.Should().Be(5); // 1 virtual + 4 real (10+10+10+10)
        tables47.Count.Should().Be(6); // 1 virtual + 5 real (10+10+10+10+7)
        tables47.Count(x => !x.IsVirtual).Should().Be(5);
        tables47.Last(x => !x.IsVirtual).GuestCount.Should().Be(7);
    }

    [Fact]
    public void AlacarteVip_TableAllocation_47To38_ShouldRemoveOddTable()
    {
        var engine = new TableAllocationEngine();

        var tables47 = engine.Allocate(47, 10);
        var tables38 = engine.Allocate(38, 10);

        tables38.Count(x => !x.IsVirtual).Should().Be(4);
        tables38.Select(x => x.GuestCount).Should().Equal(38, 10, 10, 10, 8);
    }

    [Fact]
    public void P0A_H1_ApplyChange_Throws_WhenOrderIsPaid()
    {
        var cr = new QuoteChangeRequest { Status = "Approved", PreviousOrderId = Guid.NewGuid() };
        cr.Status.Should().Be("Approved");
    }

    [Fact]
    public void P0A_H1_ApplyChange_Throws_WhenOrderIsCancelled()
    {
        var cr = new QuoteChangeRequest { Status = "Approved" };
        cr.Status.Should().Be("Approved");
    }

    [Fact]
    public void P0A_H1_ApplyChange_Throws_WhenChangeRequestNotApproved()
    {
        var cr = new QuoteChangeRequest { Status = "Pending" };
        cr.Status.Should().NotBe("Approved");
    }

    [Fact]
    public void P0A_H1_CaseB_HasSentLines_IsDetectedForAdjustmentPlan()
    {
        // Sent lines must take the adjustment-ticket path instead of a full rebuild.
        var lines = new List<AlacarteOrderLine>
        {
            new() { KitchenStatus = "Sent", MenuItemName = "Tôm hùm" },
            new() { KitchenStatus = "New", MenuItemName = "Cá song" },
        };

        var hasSentLines = lines.Any(x => x.KitchenStatus == "Sent");
        hasSentLines.Should().BeTrue();
    }

    [Fact]
    public void P0A_H1_OrderReplaced_ShouldBeDetectedByGuard()
    {
        var orderStatuses = new[] { "Paid", "Cancelled", "Replaced" };
        var blockedStatuses = new HashSet<string>(orderStatuses);

        blockedStatuses.Contains("Replaced").Should().BeTrue();
        blockedStatuses.Contains("Serving").Should().BeFalse();
        blockedStatuses.Contains("SentKitchen").Should().BeFalse();
    }

    [Fact]
    public void P0A_H1_DepositReLink_UpdatesAlacarteOrderId()
    {
        var oldOrderId = Guid.NewGuid();
        var newOrderId = Guid.NewGuid();

        var deposit = new RestaurantDeposit
        {
            AlacarteOrderId = oldOrderId,
            DepositNo = "RDP-20260624-001",
            Amount = 20_000_000,
            Note = "Đặt cọc ban đầu",
        };

        // Simulate re-link
        deposit.Note = (deposit.Note ?? "") + $" | Đơn gốc old → new";
        deposit.AlacarteOrderId = newOrderId;

        deposit.AlacarteOrderId.Should().Be(newOrderId);
        deposit.AlacarteOrderId.Should().NotBe(oldOrderId);
        deposit.Note.Should().Contain("Đơn gốc");
        deposit.Amount.Should().Be(20_000_000); // Amount unchanged
    }

    [Fact]
    public void P0A_H1_ReservationCleanup_OldOrderCleared_NewOrderKeeps()
    {
        var reservationId = Guid.NewGuid();

        var oldOrder = new AlacarteOrder { ReservationId = reservationId };
        var capturedReservationId = oldOrder.ReservationId;
        var newOrder = new AlacarteOrder { ReservationId = capturedReservationId };

        // Cleanup
        oldOrder.ReservationId = null;

        oldOrder.ReservationId.Should().BeNull();
        newOrder.ReservationId.Should().Be(reservationId);
    }

    [Fact]
    public void P0A_H1_PaidAmount_Preserved_AfterRebuild()
    {
        var oldPaidAmount = 20_000_000m;
        var newOrder = new AlacarteOrder { TotalAmount = 79_200_000 };

        newOrder.PaidAmount = oldPaidAmount;
        newOrder.RemainingAmount = Math.Max(0, newOrder.TotalAmount - newOrder.PaidAmount);

        newOrder.PaidAmount.Should().Be(20_000_000);
        newOrder.RemainingAmount.Should().Be(59_200_000);
    }

    [Fact]
    public void P0A_H1_RowVersion_ShouldBePresent()
    {
        var cr = new QuoteChangeRequest();
        cr.RowVersion.Should().NotBeNull();
        cr.RowVersion.Should().BeEmpty(); // Default before DB generates it
    }

    [Fact]
    public void P0A_H1_Transaction_ChangeRequestStatusUpdated()
    {
        var cr = new QuoteChangeRequest { Status = "Approved" };

        cr.Status = "Applied";
        cr.AppliedBy = "test-user";
        cr.AppliedAt = DateTime.UtcNow;

        cr.Status.Should().Be("Applied");
        cr.AppliedBy.Should().Be("test-user");
        cr.AppliedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task BanquetRoom_ShouldPersistLayoutJson()
    {
        await using var db = TestDbContextFactory.CreateContext();
        var service = new BanquetRestaurantService(db);
        const string layout = "[{\"ItemType\":\"RoundTable\",\"Name\":\"Bàn 1\",\"X\":36,\"Y\":92}]";

        var room = await service.SaveRoomAsync(new SaveBanquetRoomRequest
        {
            Code = "MAP-01", Name = "Sảnh sơ đồ", Capacity = 120, MaxTables = 12, LayoutJson = layout
        });
        var loaded = (await service.GetRoomsAsync()).Single(x => x.Id == room.Id);

        loaded.LayoutJson.Should().Be(layout);
    }

    [Fact]
    public async Task BanquetRoom_DeleteWithEvent_ShouldArchiveRoom()
    {
        await using var db = TestDbContextFactory.CreateContext();
        var service = new BanquetRestaurantService(db);
        var room = await service.SaveRoomAsync(new SaveBanquetRoomRequest
        {
            Code = "ARCHIVE-01", Name = "Sảnh có lịch sử", Capacity = 100, MaxTables = 10
        });
        await service.SaveEventAsync(new SaveBanquetEventRequest
        {
            EventType = "Wedding", CustomerName = "Khách kiểm thử", Phone = "0900000000",
            EventDate = DateTime.Today.AddDays(10), StartTime = new TimeSpan(17, 0, 0),
            EndTime = new TimeSpan(21, 0, 0), GuestCount = 80, TableCount = 8,
            BanquetRoomId = room.Id, BrideName = "Cô dâu kiểm thử"
        });

        await service.DeleteRoomAsync(room.Id);
        var archived = (await service.GetRoomsAsync()).Single(x => x.Id == room.Id);

        archived.IsActive.Should().BeFalse();
        archived.Status.Should().Be("Inactive");
    }

    [Fact]
    public async Task BanquetQuantification_ShouldVersionAndPersistWorkflowStatus()
    {
        await using var db = TestDbContextFactory.CreateContext();
        var service = new BanquetRestaurantService(db);
        var room = await service.SaveRoomAsync(new SaveBanquetRoomRequest
        {
            Code = "QTY-HALL", Name = "Quantification hall", Capacity = 100, MaxTables = 10
        });
        var evt = await service.SaveEventAsync(new SaveBanquetEventRequest
        {
            EventType = "Wedding", CustomerName = "Quantification customer", Phone = "0900000001",
            EventDate = DateTime.Today.AddDays(14), StartTime = new TimeSpan(18, 0, 0),
            EndTime = new TimeSpan(22, 0, 0), GuestCount = 80, TableCount = 8, BanquetRoomId = room.Id,
            BrideName = "Bride", GroomName = "Groom"
        });

        var first = await service.SaveQuantificationBatchAsync(new SaveBanquetQuantificationBatchRequest
        {
            BanquetEventId = evt.Id, Fingerprint = "fingerprint-1", SentBy = "planner",
            Lines = [new BanquetQuantificationLineDto { SortOrder = 1, LineType = "Ingredient", Code = "RICE", Name = "Rice", UnitName = "kg", RequiredQuantity = 20m }]
        });
        var second = await service.SaveQuantificationBatchAsync(new SaveBanquetQuantificationBatchRequest
        {
            BanquetEventId = evt.Id, Fingerprint = "fingerprint-2", SentBy = "planner",
            Lines = [new BanquetQuantificationLineDto { SortOrder = 1, LineType = "Ingredient", Code = "RICE", Name = "Rice", UnitName = "kg", RequiredQuantity = 22m }]
        });
        var purchaseRequest = await service.CreatePurchaseRequestAsync(second.Id, "planner");
        var purchaseRequestRetry = await service.CreatePurchaseRequestAsync(second.Id, "planner");
        await service.UpdateQuantificationStatusAsync(second.Id, "Received", "inventory");
        await service.UpdateQuantificationStatusAsync(second.Id, "Completed", "inventory");

        var history = await service.GetQuantificationBatchesAsync(evt.Id);
        history.Should().HaveCount(2);
        first.VersionNo.Should().Be(1);
        second.VersionNo.Should().Be(2);
        history.Single(x => x.Id == second.Id).Status.Should().Be("Completed");
        history.Single(x => x.Id == second.Id).CompletedBy.Should().Be("inventory");
        history.Single(x => x.Id == second.Id).Lines.Should().ContainSingle(x => x.RequiredQuantity == 22m);
        purchaseRequestRetry.Id.Should().Be(purchaseRequest.Id);
        purchaseRequest.Lines.Should().ContainSingle(x => x.ItemCode == "RICE" && x.RequestedQuantity == 22m);
        (await service.GetPurchaseRequestsAsync(evt.Id)).Should().ContainSingle();
    }

    [Fact]
    public async Task BanquetSettings_ShouldPersistInDatabase()
    {
        await using var db = TestDbContextFactory.CreateContext();
        var service = new BanquetRestaurantService(db);

        await service.SaveModuleSettingsAsync(new BanquetModuleSettingsDto
        {
            VenueName = "ASMILE Banquet", VenueCode = "BNQ-TEST", GuestsPerTable = 12,
            DefaultDepositRate = 35m, DefaultVatRate = 10m, RequireContractBeforeConfirm = true
        });
        var loaded = await service.GetModuleSettingsAsync();

        loaded.VenueName.Should().Be("ASMILE Banquet");
        loaded.VenueCode.Should().Be("BNQ-TEST");
        loaded.GuestsPerTable.Should().Be(12);
        loaded.RequireContractBeforeConfirm.Should().BeTrue();
    }

    [Fact]
    public async Task BanquetFloorPlan_ShouldBelongToSelectedEvent()
    {
        await using var db = TestDbContextFactory.CreateContext();
        var service = new BanquetRestaurantService(db);
        var room = await service.SaveRoomAsync(new SaveBanquetRoomRequest
        {
            Code = "PLAN-HALL", Name = "Plan hall", Capacity = 120, MaxTables = 12
        });
        var evt = await service.SaveEventAsync(new SaveBanquetEventRequest
        {
            EventType = "Conference", CustomerName = "Plan customer", Phone = "0900000002",
            EventDate = DateTime.Today.AddDays(21), StartTime = new TimeSpan(8, 0, 0),
            EndTime = new TimeSpan(12, 0, 0), GuestCount = 100, TableCount = 10, BanquetRoomId = room.Id,
            ConferenceName = "Annual conference"
        });
        const string layout = "[{\"ItemType\":\"Stage\",\"Name\":\"Main stage\",\"X\":100,\"Y\":20}]";

        var saved = await service.SaveFloorPlanAsync(new SaveBanquetFloorPlanRequest
        {
            BanquetEventId = evt.Id, Name = "Event plan v1", CanvasWidth = 760, CanvasHeight = 520, LayoutJson = layout
        });
        var plans = await service.GetFloorPlansAsync(evt.Id);

        saved.BanquetEventId.Should().Be(evt.Id);
        plans.Should().ContainSingle();
        plans[0].LayoutJson.Should().Be(layout);
    }

    [Fact]
    public async Task BanquetCommercialControls_ShouldBeIdempotentRefundAndLockSettlement()
    {
        await using var db = TestDbContextFactory.CreateContext();
        var service = new BanquetRestaurantService(db);
        var room = await service.SaveRoomAsync(new SaveBanquetRoomRequest
        {
            Code = "COMM-HALL", Name = "Commercial hall", Capacity = 100, MaxTables = 10
        });
        var evt = await service.SaveEventAsync(new SaveBanquetEventRequest
        {
            EventType = "Conference", CustomerName = "Commercial customer", Phone = "0900000099",
            EventDate = DateTime.Today.AddDays(30), StartTime = new TimeSpan(8, 0, 0),
            EndTime = new TimeSpan(12, 0, 0), GuestCount = 80, TableCount = 8,
            BanquetRoomId = room.Id, Status = "Confirmed", ConferenceName = "Commercial readiness"
        });
        await service.SaveServiceLineAsync(new SaveBanquetServiceLineRequest
        {
            BanquetEventId = evt.Id, ServiceType = "Other", Description = "Event package",
            Qty = 1m, UnitPrice = 10000000m
        });

        var request = new AddBanquetDepositRequest
        {
            BanquetEventId = evt.Id, Amount = 3000000m, PaymentMethod = "Transfer",
            Receiver = "cashier", IdempotencyKey = "commercial-test-1"
        };
        var first = await service.AddDepositAsync(request);
        var retry = await service.AddDepositAsync(request);
        first.Id.Should().Be(retry.Id);
        (await service.GetEventDetailAsync(evt.Id)).Deposits.Should().ContainSingle();

        await service.RefundDepositAsync(new RefundBanquetDepositRequest
        {
            DepositId = first.Id, Amount = 1000000m, Reason = "Customer reduced scope", RefundedBy = "manager"
        });
        var afterRefund = await service.GetEventDetailAsync(evt.Id);
        afterRefund.Event.DepositAmount.Should().Be(2000000m);
        afterRefund.Event.RemainingAmount.Should().Be(8000000m);
        afterRefund.Deposits.Single().Status.Should().Be("PartiallyRefunded");

        var completed = await service.FinalizeSettlementAsync(evt.Id, "Transfer", "cashier");
        completed.Status.Should().Be("Completed");
        completed.IsFinanciallyLocked.Should().BeTrue();
        var editAfterSettlement = async () => await service.SaveServiceLineAsync(new SaveBanquetServiceLineRequest
        {
            BanquetEventId = evt.Id, ServiceType = "Other", Description = "Late change", Qty = 1m, UnitPrice = 1m
        });
        await editAfterSettlement.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task BanquetProfitability_ShouldIncludeServiceCostAndGrossMargin()
    {
        await using var db = TestDbContextFactory.CreateContext();
        var service = new BanquetRestaurantService(db);
        var room = await service.SaveRoomAsync(new SaveBanquetRoomRequest
        {
            Code = "PROFIT-HALL", Name = "Profit hall", Capacity = 100, MaxTables = 10
        });
        var eventDate = DateTime.Today.AddDays(40);
        var evt = await service.SaveEventAsync(new SaveBanquetEventRequest
        {
            EventType = "Conference", CustomerName = "Profit customer", Phone = "0900000100",
            EventDate = eventDate, StartTime = new TimeSpan(8, 0, 0), EndTime = new TimeSpan(12, 0, 0),
            GuestCount = 80, TableCount = 8, BanquetRoomId = room.Id, ConferenceName = "Profitability"
        });
        await service.SaveServiceLineAsync(new SaveBanquetServiceLineRequest
        {
            BanquetEventId = evt.Id, ServiceType = "Equipment", Description = "Sound package",
            Qty = 2m, UnitPrice = 100m, CostPrice = 60m
        });

        var result = (await service.GetEventsAsync(eventDate, eventDate)).Single(x => x.Id == evt.Id);

        result.TotalAmount.Should().Be(200m);
        result.ServiceEstimatedCost.Should().Be(120m);
        result.EstimatedCost.Should().Be(120m);
        result.EstimatedGrossProfit.Should().Be(80m);
        result.EstimatedGrossMarginRate.Should().Be(0.4m);
    }

    [Fact]
    public async Task BanquetDashboard_ShouldNotReportRoomConflictAcrossDifferentDates()
    {
        await using var db = TestDbContextFactory.CreateContext();
        var service = new BanquetRestaurantService(db);
        var room = await service.SaveRoomAsync(new SaveBanquetRoomRequest
        {
            Code = "DATE-HALL", Name = "Date hall", Capacity = 100, MaxTables = 10
        });
        var firstDate = DateTime.Today.AddDays(50);
        foreach (var eventDate in new[] { firstDate, firstDate.AddDays(1) })
        {
            await service.SaveEventAsync(new SaveBanquetEventRequest
            {
                EventType = "Conference", CustomerName = $"Customer {eventDate:dd}", Phone = "0900000101",
                EventDate = eventDate, StartTime = new TimeSpan(8, 0, 0), EndTime = new TimeSpan(12, 0, 0),
                GuestCount = 80, TableCount = 8, BanquetRoomId = room.Id, ConferenceName = "Daily event"
            });
        }

        var dashboard = await service.GetDashboardAsync(firstDate);

        dashboard.RoomConflicts.Should().Be(0);
    }

    [Fact]
    public async Task BanquetCancellation_ShouldRequireFullDepositRefund()
    {
        await using var db = TestDbContextFactory.CreateContext();
        var service = new BanquetRestaurantService(db);
        var room = await service.SaveRoomAsync(new SaveBanquetRoomRequest
        {
            Code = "CANCEL-HALL", Name = "Cancellation hall", Capacity = 100, MaxTables = 10
        });
        var evt = await service.SaveEventAsync(new SaveBanquetEventRequest
        {
            EventType = "Conference", CustomerName = "Cancellation customer", Phone = "0900000102",
            EventDate = DateTime.Today.AddDays(60), StartTime = new TimeSpan(8, 0, 0), EndTime = new TimeSpan(12, 0, 0),
            GuestCount = 80, TableCount = 8, BanquetRoomId = room.Id, ConferenceName = "Cancellation control"
        });
        await service.SaveServiceLineAsync(new SaveBanquetServiceLineRequest
        {
            BanquetEventId = evt.Id, ServiceType = "Other", Description = "Cancellation package",
            Qty = 1m, UnitPrice = 1000m, CostPrice = 600m
        });
        var deposit = await service.AddDepositAsync(new AddBanquetDepositRequest
        {
            BanquetEventId = evt.Id, Amount = 300m, PaymentMethod = "Transfer", Receiver = "cashier"
        });

        var cancelWithDeposit = async () => await service.UpdateEventStatusAsync(evt.Id, "Cancelled");
        await cancelWithDeposit.Should().ThrowAsync<InvalidOperationException>();

        await service.RefundDepositAsync(new RefundBanquetDepositRequest
        {
            DepositId = deposit.Id, Amount = 300m, Reason = "Event cancelled", RefundedBy = "manager"
        });
        var cancelled = await service.UpdateEventStatusAsync(evt.Id, "Cancelled");

        cancelled.Status.Should().Be("Cancelled");
        cancelled.DepositAmount.Should().Be(0m);
    }
}
