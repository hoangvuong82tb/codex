IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
GO

CREATE TABLE [AppSettings] (
    [Id] int NOT NULL IDENTITY,
    [Key] nvarchar(100) NOT NULL,
    [Value] nvarchar(2000) NOT NULL,
    CONSTRAINT [PK_AppSettings] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [AuditLogs] (
    [Id] int NOT NULL IDENTITY,
    [CreatedAt] datetime2 NOT NULL,
    [Username] nvarchar(100) NOT NULL,
    [Action] nvarchar(200) NOT NULL,
    [Detail] nvarchar(4000) NOT NULL,
    CONSTRAINT [PK_AuditLogs] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [Departments] (
    [Id] int NOT NULL IDENTITY,
    [DepartmentCode] nvarchar(50) NOT NULL,
    [DepartmentName] nvarchar(255) NOT NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_Departments] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [Items] (
    [Id] int NOT NULL IDENTITY,
    [ItemCode] nvarchar(50) NOT NULL,
    [ItemName] nvarchar(255) NOT NULL,
    [Unit] nvarchar(50) NOT NULL,
    [GroupName] nvarchar(100) NOT NULL,
    [SupplierCode] nvarchar(50) NOT NULL,
    [SupplierName] nvarchar(255) NOT NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_Items] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [StockBalances] (
    [Id] int NOT NULL IDENTITY,
    [WarehouseId] int NOT NULL,
    [ItemId] int NOT NULL,
    [OpeningQty] decimal(18,4) NOT NULL,
    [OpeningAmount] decimal(18,2) NOT NULL,
    [InQty] decimal(18,4) NOT NULL,
    [InAmount] decimal(18,2) NOT NULL,
    [OutQty] decimal(18,4) NOT NULL,
    [OutAmount] decimal(18,2) NOT NULL,
    [ClosingQty] decimal(18,4) NOT NULL,
    [ClosingAmount] decimal(18,2) NOT NULL,
    [AvgPrice] decimal(18,2) NOT NULL,
    CONSTRAINT [PK_StockBalances] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [StockIssues] (
    [Id] int NOT NULL IDENTITY,
    [IssueNo] nvarchar(60) NOT NULL,
    [IssueDate] datetime2 NOT NULL,
    [Description] nvarchar(500) NOT NULL,
    [WarehouseId] int NOT NULL,
    [ToWarehouseId] int NULL,
    [DepartmentCode] nvarchar(50) NOT NULL,
    [DebitAccountCode] nvarchar(50) NOT NULL,
    [CreditAccountCode] nvarchar(50) NOT NULL,
    [TotalAmount] decimal(18,2) NOT NULL,
    CONSTRAINT [PK_StockIssues] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [StockReceipts] (
    [Id] int NOT NULL IDENTITY,
    [ReceiptNo] nvarchar(60) NOT NULL,
    [ReceiptDate] datetime2 NOT NULL,
    [SupplierCode] nvarchar(50) NOT NULL,
    [SupplierName] nvarchar(255) NOT NULL,
    [Description] nvarchar(500) NOT NULL,
    [WarehouseId] int NOT NULL,
    [DebitAccountCode] nvarchar(50) NOT NULL,
    [CreditAccountCode] nvarchar(50) NOT NULL,
    [TotalAmount] decimal(18,2) NOT NULL,
    CONSTRAINT [PK_StockReceipts] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [Suppliers] (
    [Id] int NOT NULL IDENTITY,
    [SupplierCode] nvarchar(50) NOT NULL,
    [SupplierName] nvarchar(255) NOT NULL,
    [Phone] nvarchar(30) NOT NULL,
    [BankAccountNo] nvarchar(80) NOT NULL,
    [Address] nvarchar(500) NOT NULL,
    [Note] nvarchar(500) NOT NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_Suppliers] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [Warehouses] (
    [Id] int NOT NULL IDENTITY,
    [WarehouseCode] nvarchar(50) NOT NULL,
    [WarehouseName] nvarchar(255) NOT NULL,
    [Note] nvarchar(500) NOT NULL,
    [ParentWarehouseId] int NULL,
    [IsConsumptionWarehouse] bit NOT NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_Warehouses] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [StockIssueDetails] (
    [Id] int NOT NULL IDENTITY,
    [IssueId] int NOT NULL,
    [ItemId] int NOT NULL,
    [Quantity] decimal(18,4) NOT NULL,
    [UnitPrice] decimal(18,2) NOT NULL,
    [Amount] decimal(18,2) NOT NULL,
    CONSTRAINT [PK_StockIssueDetails] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_StockIssueDetails_StockIssues_IssueId] FOREIGN KEY ([IssueId]) REFERENCES [StockIssues] ([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [StockReceiptDetails] (
    [Id] int NOT NULL IDENTITY,
    [ReceiptId] int NOT NULL,
    [ItemId] int NOT NULL,
    [Quantity] decimal(18,4) NOT NULL,
    [UnitPrice] decimal(18,2) NOT NULL,
    [Amount] decimal(18,2) NOT NULL,
    CONSTRAINT [PK_StockReceiptDetails] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_StockReceiptDetails_StockReceipts_ReceiptId] FOREIGN KEY ([ReceiptId]) REFERENCES [StockReceipts] ([Id]) ON DELETE CASCADE
);
GO

IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'DepartmentCode', N'DepartmentName', N'IsActive') AND [object_id] = OBJECT_ID(N'[Departments]'))
    SET IDENTITY_INSERT [Departments] ON;
INSERT INTO [Departments] ([Id], [DepartmentCode], [DepartmentName], [IsActive])
VALUES (1, N'HCKT', N'Hành chính - Kế toán', CAST(1 AS bit)),
(2, N'NHAHANG', N'Nhà hàng', CAST(1 AS bit)),
(3, N'BEP', N'Bếp', CAST(1 AS bit));
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'DepartmentCode', N'DepartmentName', N'IsActive') AND [object_id] = OBJECT_ID(N'[Departments]'))
    SET IDENTITY_INSERT [Departments] OFF;
GO

IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'GroupName', N'IsActive', N'ItemCode', N'ItemName', N'SupplierCode', N'SupplierName', N'Unit') AND [object_id] = OBJECT_ID(N'[Items]'))
    SET IDENTITY_INSERT [Items] ON;
INSERT INTO [Items] ([Id], [GroupName], [IsActive], [ItemCode], [ItemName], [SupplierCode], [SupplierName], [Unit])
VALUES (1, N'Mặc định', CAST(1 AS bit), N'HH-001', N'Hàng mẫu', N'NCC-001', N'Nhà cung cấp mặc định', N'Cái');
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'GroupName', N'IsActive', N'ItemCode', N'ItemName', N'SupplierCode', N'SupplierName', N'Unit') AND [object_id] = OBJECT_ID(N'[Items]'))
    SET IDENTITY_INSERT [Items] OFF;
GO

IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Address', N'BankAccountNo', N'IsActive', N'Note', N'Phone', N'SupplierCode', N'SupplierName') AND [object_id] = OBJECT_ID(N'[Suppliers]'))
    SET IDENTITY_INSERT [Suppliers] ON;
INSERT INTO [Suppliers] ([Id], [Address], [BankAccountNo], [IsActive], [Note], [Phone], [SupplierCode], [SupplierName])
VALUES (1, N'', N'', CAST(1 AS bit), N'', N'', N'NCC-001', N'Nhà cung cấp mặc định');
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Address', N'BankAccountNo', N'IsActive', N'Note', N'Phone', N'SupplierCode', N'SupplierName') AND [object_id] = OBJECT_ID(N'[Suppliers]'))
    SET IDENTITY_INSERT [Suppliers] OFF;
GO

IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'IsActive', N'IsConsumptionWarehouse', N'Note', N'ParentWarehouseId', N'WarehouseCode', N'WarehouseName') AND [object_id] = OBJECT_ID(N'[Warehouses]'))
    SET IDENTITY_INSERT [Warehouses] ON;
INSERT INTO [Warehouses] ([Id], [IsActive], [IsConsumptionWarehouse], [Note], [ParentWarehouseId], [WarehouseCode], [WarehouseName])
VALUES (1, CAST(1 AS bit), CAST(0 AS bit), N'', NULL, N'KHO-TONG', N'Kho tổng'),
(2, CAST(1 AS bit), CAST(1 AS bit), N'', 1, N'KHO-BEP', N'Kho bếp');
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'IsActive', N'IsConsumptionWarehouse', N'Note', N'ParentWarehouseId', N'WarehouseCode', N'WarehouseName') AND [object_id] = OBJECT_ID(N'[Warehouses]'))
    SET IDENTITY_INSERT [Warehouses] OFF;
GO

CREATE UNIQUE INDEX [IX_AppSettings_Key] ON [AppSettings] ([Key]);
GO

CREATE INDEX [IX_AuditLogs_CreatedAt] ON [AuditLogs] ([CreatedAt]);
GO

CREATE UNIQUE INDEX [IX_Departments_DepartmentCode] ON [Departments] ([DepartmentCode]);
GO

CREATE UNIQUE INDEX [IX_Items_ItemCode] ON [Items] ([ItemCode]);
GO

CREATE UNIQUE INDEX [IX_Items_ItemName] ON [Items] ([ItemName]);
GO

CREATE UNIQUE INDEX [IX_StockBalances_WarehouseId_ItemId] ON [StockBalances] ([WarehouseId], [ItemId]);
GO

CREATE INDEX [IX_StockIssueDetails_IssueId] ON [StockIssueDetails] ([IssueId]);
GO

CREATE INDEX [IX_StockIssues_IssueDate] ON [StockIssues] ([IssueDate]);
GO

CREATE UNIQUE INDEX [IX_StockIssues_IssueNo] ON [StockIssues] ([IssueNo]);
GO

CREATE INDEX [IX_StockReceiptDetails_ReceiptId] ON [StockReceiptDetails] ([ReceiptId]);
GO

CREATE INDEX [IX_StockReceipts_ReceiptDate] ON [StockReceipts] ([ReceiptDate]);
GO

CREATE UNIQUE INDEX [IX_StockReceipts_ReceiptNo] ON [StockReceipts] ([ReceiptNo]);
GO

CREATE UNIQUE INDEX [IX_Suppliers_SupplierCode] ON [Suppliers] ([SupplierCode]);
GO

CREATE INDEX [IX_Warehouses_ParentWarehouseId] ON [Warehouses] ([ParentWarehouseId]);
GO

CREATE UNIQUE INDEX [IX_Warehouses_WarehouseCode] ON [Warehouses] ([WarehouseCode]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260421153713_InitialCreate', N'8.0.7');
GO

COMMIT;
GO

