-- Seed Ingredients using auto-generated IDs
IF NOT EXISTS (SELECT 1 FROM Items WHERE ItemCode = 'BANHMI')
BEGIN
    INSERT INTO Items (ItemCode, ItemName, Unit, IsActive)
    VALUES (N'BANHMI', N'Bánh mì', N'cái', 1);
END
GO
IF NOT EXISTS (SELECT 1 FROM Items WHERE ItemCode = 'THITBO')
BEGIN
    INSERT INTO Items (ItemCode, ItemName, Unit, IsActive)
    VALUES (N'THITBO', N'Thịt bò', N'gram', 1);
END
GO
IF NOT EXISTS (SELECT 1 FROM Items WHERE ItemCode = 'TRUNGGA')
BEGIN
    INSERT INTO Items (ItemCode, ItemName, Unit, IsActive)
    VALUES (N'TRUNGGA', N'Trứng gà', N'quả', 1);
END
GO
IF NOT EXISTS (SELECT 1 FROM Items WHERE ItemCode = 'RAU')
BEGIN
    INSERT INTO Items (ItemCode, ItemName, Unit, IsActive)
    VALUES (N'RAU', N'Rau sống', N'gram', 1);
END
GO
IF NOT EXISTS (SELECT 1 FROM Items WHERE ItemCode = 'CACHUA')
BEGIN
    INSERT INTO Items (ItemCode, ItemName, Unit, IsActive)
    VALUES (N'CACHUA', N'Cà chua', N'gram', 1);
END
GO
IF NOT EXISTS (SELECT 1 FROM Items WHERE ItemCode = 'CAPHEBOT')
BEGIN
    INSERT INTO Items (ItemCode, ItemName, Unit, IsActive)
    VALUES (N'CAPHEBOT', N'Cà phê bột', N'gram', 1);
END
GO
IF NOT EXISTS (SELECT 1 FROM Items WHERE ItemCode = 'SUADAC')
BEGIN
    INSERT INTO Items (ItemCode, ItemName, Unit, IsActive)
    VALUES (N'SUADAC', N'Sữa đặc', N'ml', 1);
END
GO
IF NOT EXISTS (SELECT 1 FROM Items WHERE ItemCode = 'DUONG')
BEGIN
    INSERT INTO Items (ItemCode, ItemName, Unit, IsActive)
    VALUES (N'DUONG', N'Đường', N'gram', 1);
END
GO
IF NOT EXISTS (SELECT 1 FROM Items WHERE ItemCode = 'DA')
BEGIN
    INSERT INTO Items (ItemCode, ItemName, Unit, IsActive)
    VALUES (N'DA', N'Đá viên', N'gram', 1);
END
GO
IF NOT EXISTS (SELECT 1 FROM Items WHERE ItemCode = 'BANHPHO')
BEGIN
    INSERT INTO Items (ItemCode, ItemName, Unit, IsActive)
    VALUES (N'BANHPHO', N'Bánh phở khô', N'gram', 1);
END
GO

-- Recipes using code lookups with dynamic ItemId
-- 1. Bánh mì bò recipe
DECLARE @menuId UNIQUEIDENTIFIER = (SELECT Id FROM MenuItems WHERE Code = 'BM_BO');
IF @menuId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM MenuItemRecipes WHERE MenuItemId = @menuId)
BEGIN
    DECLARE @rid UNIQUEIDENTIFIER = NEWID();
    INSERT INTO MenuItemRecipes (Id, MenuItemId, Name, IsDefault, IsActive, CreatedAt, UpdatedAt)
    VALUES (@rid, @menuId, N'Default', 1, 1, GETUTCDATE(), GETUTCDATE());
    INSERT INTO MenuItemRecipeLines (Id, MenuItemRecipeId, LineNo, ItemId, ItemCode, ItemName, UnitName, Quantity, LossRate, IsRequired, CreatedAt, UpdatedAt)
    SELECT NEWID(), @rid, 1, Id, 'BANHMI', N'Bánh mì', N'cái', 1.0, 0.0, 1, GETUTCDATE(), GETUTCDATE() FROM Items WHERE ItemCode = 'BANHMI';
    INSERT INTO MenuItemRecipeLines (Id, MenuItemRecipeId, LineNo, ItemId, ItemCode, ItemName, UnitName, Quantity, LossRate, IsRequired, CreatedAt, UpdatedAt)
    SELECT NEWID(), @rid, 2, Id, 'THITBO', N'Thịt bò', N'gram', 80.0, 0.05, 1, GETUTCDATE(), GETUTCDATE() FROM Items WHERE ItemCode = 'THITBO';
    INSERT INTO MenuItemRecipeLines (Id, MenuItemRecipeId, LineNo, ItemId, ItemCode, ItemName, UnitName, Quantity, LossRate, IsRequired, CreatedAt, UpdatedAt)
    SELECT NEWID(), @rid, 3, Id, 'RAU', N'Rau sống', N'gram', 30.0, 0.10, 1, GETUTCDATE(), GETUTCDATE() FROM Items WHERE ItemCode = 'RAU';
    INSERT INTO MenuItemRecipeLines (Id, MenuItemRecipeId, LineNo, ItemId, ItemCode, ItemName, UnitName, Quantity, LossRate, IsRequired, CreatedAt, UpdatedAt)
    SELECT NEWID(), @rid, 4, Id, 'CACHUA', N'Cà chua', N'gram', 30.0, 0.05, 0, GETUTCDATE(), GETUTCDATE() FROM Items WHERE ItemCode = 'CACHUA';
END
GO

-- 2. Cà phê sữa recipe
DECLARE @m2 UNIQUEIDENTIFIER = (SELECT Id FROM MenuItems WHERE Code = 'CF_SUA');
IF @m2 IS NOT NULL AND NOT EXISTS (SELECT 1 FROM MenuItemRecipes WHERE MenuItemId = @m2)
BEGIN
    DECLARE @r2 UNIQUEIDENTIFIER = NEWID();
    INSERT INTO MenuItemRecipes (Id, MenuItemId, Name, IsDefault, IsActive, CreatedAt, UpdatedAt)
    VALUES (@r2, @m2, N'Default', 1, 1, GETUTCDATE(), GETUTCDATE());
    INSERT INTO MenuItemRecipeLines (Id, MenuItemRecipeId, LineNo, ItemId, ItemCode, ItemName, UnitName, Quantity, LossRate, IsRequired, CreatedAt, UpdatedAt)
    SELECT NEWID(), @r2, 1, Id, 'CAPHEBOT', N'Cà phê bột', N'gram', 20.0, 0.02, 1, GETUTCDATE(), GETUTCDATE() FROM Items WHERE ItemCode = 'CAPHEBOT';
    INSERT INTO MenuItemRecipeLines (Id, MenuItemRecipeId, LineNo, ItemId, ItemCode, ItemName, UnitName, Quantity, LossRate, IsRequired, CreatedAt, UpdatedAt)
    SELECT NEWID(), @r2, 2, Id, 'SUADAC', N'Sữa đặc', N'ml', 30.0, 0.02, 1, GETUTCDATE(), GETUTCDATE() FROM Items WHERE ItemCode = 'SUADAC';
    INSERT INTO MenuItemRecipeLines (Id, MenuItemRecipeId, LineNo, ItemId, ItemCode, ItemName, UnitName, Quantity, LossRate, IsRequired, CreatedAt, UpdatedAt)
    SELECT NEWID(), @r2, 3, Id, 'DUONG', N'Đường', N'gram', 5.0, 0.0, 0, GETUTCDATE(), GETUTCDATE() FROM Items WHERE ItemCode = 'DUONG';
    INSERT INTO MenuItemRecipeLines (Id, MenuItemRecipeId, LineNo, ItemId, ItemCode, ItemName, UnitName, Quantity, LossRate, IsRequired, CreatedAt, UpdatedAt)
    SELECT NEWID(), @r2, 4, Id, 'DA', N'Đá viên', N'gram', 100.0, 0.05, 1, GETUTCDATE(), GETUTCDATE() FROM Items WHERE ItemCode = 'DA';
END
GO

-- 3. Phở bò recipe
DECLARE @m3 UNIQUEIDENTIFIER = (SELECT Id FROM MenuItems WHERE Code = 'PHO_BO');
IF @m3 IS NOT NULL AND NOT EXISTS (SELECT 1 FROM MenuItemRecipes WHERE MenuItemId = @m3)
BEGIN
    DECLARE @r3 UNIQUEIDENTIFIER = NEWID();
    INSERT INTO MenuItemRecipes (Id, MenuItemId, Name, IsDefault, IsActive, CreatedAt, UpdatedAt)
    VALUES (@r3, @m3, N'Default', 1, 1, GETUTCDATE(), GETUTCDATE());
    INSERT INTO MenuItemRecipeLines (Id, MenuItemRecipeId, LineNo, ItemId, ItemCode, ItemName, UnitName, Quantity, LossRate, IsRequired, CreatedAt, UpdatedAt)
    SELECT NEWID(), @r3, 1, Id, 'BANHPHO', N'Bánh phở khô', N'gram', 200.0, 0.03, 1, GETUTCDATE(), GETUTCDATE() FROM Items WHERE ItemCode = 'BANHPHO';
    INSERT INTO MenuItemRecipeLines (Id, MenuItemRecipeId, LineNo, ItemId, ItemCode, ItemName, UnitName, Quantity, LossRate, IsRequired, CreatedAt, UpdatedAt)
    SELECT NEWID(), @r3, 2, Id, 'THITBO', N'Thịt bò', N'gram', 80.0, 0.05, 1, GETUTCDATE(), GETUTCDATE() FROM Items WHERE ItemCode = 'THITBO';
    INSERT INTO MenuItemRecipeLines (Id, MenuItemRecipeId, LineNo, ItemId, ItemCode, ItemName, UnitName, Quantity, LossRate, IsRequired, CreatedAt, UpdatedAt)
    SELECT NEWID(), @r3, 3, Id, 'RAU', N'Rau sống', N'gram', 30.0, 0.10, 1, GETUTCDATE(), GETUTCDATE() FROM Items WHERE ItemCode = 'RAU';
END
GO
