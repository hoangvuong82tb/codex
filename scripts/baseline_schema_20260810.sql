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
VALUES (1, N'HCKT', N'Hành chính - K? toán', CAST(1 AS bit)),
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

BEGIN TRANSACTION;
GO

CREATE TABLE [Customers] (
    [Id] int NOT NULL IDENTITY,
    [CustomerCode] nvarchar(50) NOT NULL,
    [CustomerName] nvarchar(255) NOT NULL,
    [Phone] nvarchar(30) NOT NULL,
    [BankAccountNo] nvarchar(80) NOT NULL,
    [Address] nvarchar(500) NOT NULL,
    [Note] nvarchar(500) NOT NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_Customers] PRIMARY KEY ([Id])
);
GO

IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Address', N'BankAccountNo', N'CustomerCode', N'CustomerName', N'IsActive', N'Note', N'Phone') AND [object_id] = OBJECT_ID(N'[Customers]'))
    SET IDENTITY_INSERT [Customers] ON;
INSERT INTO [Customers] ([Id], [Address], [BankAccountNo], [CustomerCode], [CustomerName], [IsActive], [Note], [Phone])
VALUES (1, N'', N'', N'KH-001', N'Khách hàng mặc định', CAST(1 AS bit), N'', N'');
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Address', N'BankAccountNo', N'CustomerCode', N'CustomerName', N'IsActive', N'Note', N'Phone') AND [object_id] = OBJECT_ID(N'[Customers]'))
    SET IDENTITY_INSERT [Customers] OFF;
GO

CREATE UNIQUE INDEX [IX_Customers_CustomerCode] ON [Customers] ([CustomerCode]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260424054559_AddCustomersModule', N'8.0.7');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

ALTER TABLE [Departments] ADD [Level] int NOT NULL DEFAULT 0;
GO

ALTER TABLE [Departments] ADD [Note] nvarchar(500) NOT NULL DEFAULT N'';
GO

ALTER TABLE [Departments] ADD [ParentDepartmentId] int NULL;
GO

CREATE TABLE [People] (
    [Id] int NOT NULL IDENTITY,
    [PersonCode] nvarchar(50) NOT NULL,
    [FullName] nvarchar(255) NOT NULL,
    [DepartmentId] int NOT NULL,
    [Position] nvarchar(120) NOT NULL,
    [Phone] nvarchar(30) NOT NULL,
    [Email] nvarchar(180) NOT NULL,
    [Note] nvarchar(500) NOT NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_People] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_People_Departments_DepartmentId] FOREIGN KEY ([DepartmentId]) REFERENCES [Departments] ([Id]) ON DELETE NO ACTION
);
GO

UPDATE [Departments] SET [DepartmentName] = N'Hành chính - Kế toán', [Level] = 1, [Note] = N'Khối hỗ trợ', [ParentDepartmentId] = NULL
WHERE [Id] = 1;
SELECT @@ROWCOUNT;

GO

UPDATE [Departments] SET [Level] = 1, [Note] = N'Khối vận hành', [ParentDepartmentId] = NULL
WHERE [Id] = 2;
SELECT @@ROWCOUNT;

GO

UPDATE [Departments] SET [DepartmentName] = N'Bếp', [Level] = 2, [Note] = N'Bộ phận trực thuộc nhà hàng', [ParentDepartmentId] = 2
WHERE [Id] = 3;
SELECT @@ROWCOUNT;

GO

UPDATE [Items] SET [GroupName] = N'Mặc định', [ItemName] = N'Hàng mẫu', [SupplierName] = N'Nhà cung cấp mặc định'
WHERE [Id] = 1;
SELECT @@ROWCOUNT;

GO

IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'CreatedAt', N'DepartmentId', N'Email', N'FullName', N'IsActive', N'Note', N'PersonCode', N'Phone', N'Position') AND [object_id] = OBJECT_ID(N'[People]'))
    SET IDENTITY_INSERT [People] ON;
INSERT INTO [People] ([Id], [CreatedAt], [DepartmentId], [Email], [FullName], [IsActive], [Note], [PersonCode], [Phone], [Position])
VALUES (1, '2026-01-15T00:00:00.0000000Z', 1, N'hoa@asmile.local', N'Nguyễn Minh Hòa', CAST(1 AS bit), N'Nhân sự nòng cốt', N'NS-001', N'0901234567', N'Kế toán trưởng'),
(2, '2026-02-02T00:00:00.0000000Z', 2, N'an@asmile.local', N'Trần Quốc An', CAST(1 AS bit), N'', N'NS-002', N'0912345678', N'Giám sát ca'),
(3, '2026-03-01T00:00:00.0000000Z', 3, N'ha@asmile.local', N'Phạm Thu Hà', CAST(0 AS bit), N'Đang thử việc', N'NS-003', N'0989123456', N'Bếp phó');
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'CreatedAt', N'DepartmentId', N'Email', N'FullName', N'IsActive', N'Note', N'PersonCode', N'Phone', N'Position') AND [object_id] = OBJECT_ID(N'[People]'))
    SET IDENTITY_INSERT [People] OFF;
GO

UPDATE [Suppliers] SET [SupplierName] = N'Nhà cung cấp mặc định'
WHERE [Id] = 1;
SELECT @@ROWCOUNT;

GO

UPDATE [Warehouses] SET [WarehouseName] = N'Kho tổng'
WHERE [Id] = 1;
SELECT @@ROWCOUNT;

GO

UPDATE [Warehouses] SET [WarehouseName] = N'Kho bếp'
WHERE [Id] = 2;
SELECT @@ROWCOUNT;

GO

CREATE INDEX [IX_Departments_ParentDepartmentId] ON [Departments] ([ParentDepartmentId]);
GO

CREATE INDEX [IX_People_DepartmentId] ON [People] ([DepartmentId]);
GO

CREATE UNIQUE INDEX [IX_People_PersonCode] ON [People] ([PersonCode]);
GO

CREATE INDEX [IX_People_Phone] ON [People] ([Phone]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260424072605_AddDepartmentPeopleModule', N'8.0.7');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

ALTER TABLE [Warehouses] ADD [DepartmentName] nvarchar(255) NOT NULL DEFAULT N'';
GO

ALTER TABLE [Warehouses] ADD [ResponsiblePersonName] nvarchar(255) NOT NULL DEFAULT N'';
GO

UPDATE [Warehouses] SET [DepartmentName] = N'Hành chính - Kế toán', [ResponsiblePersonName] = N'Nguyễn Minh Hòa'
WHERE [Id] = 1;
SELECT @@ROWCOUNT;

GO

UPDATE [Warehouses] SET [DepartmentName] = N'Bếp', [ResponsiblePersonName] = N'Phạm Thu Hà'
WHERE [Id] = 2;
SELECT @@ROWCOUNT;

GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260424084053_AddWarehouseDepartmentAndResponsible', N'8.0.7');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

ALTER TABLE [StockReceipts] ADD [IsApproved] bit NOT NULL DEFAULT CAST(0 AS bit);
GO

ALTER TABLE [StockReceiptDetails] ADD [LineNo] int NOT NULL DEFAULT 0;
GO

ALTER TABLE [StockReceiptDetails] ADD [Unit] nvarchar(50) NOT NULL DEFAULT N'';
GO

ALTER TABLE [StockReceiptDetails] ADD [WarehouseId] int NULL;
GO

ALTER TABLE [StockIssues] ADD [IsApproved] bit NOT NULL DEFAULT CAST(0 AS bit);
GO

ALTER TABLE [StockIssueDetails] ADD [LineNo] int NOT NULL DEFAULT 0;
GO

ALTER TABLE [StockIssueDetails] ADD [Unit] nvarchar(50) NOT NULL DEFAULT N'';
GO

ALTER TABLE [StockIssueDetails] ADD [WarehouseId] int NULL;
GO

CREATE TABLE [BusinessDocuments] (
    [Id] uniqueidentifier NOT NULL,
    [Code] nvarchar(64) NOT NULL,
    [Title] nvarchar(256) NOT NULL,
    [Module] int NOT NULL,
    [EntityType] int NOT NULL,
    [EntityId] uniqueidentifier NOT NULL,
    [DocumentType] int NOT NULL,
    [DocumentDate] datetime2 NOT NULL,
    [Amount] decimal(18,2) NOT NULL,
    [Currency] nvarchar(16) NOT NULL,
    [VirtualPath] nvarchar(512) NOT NULL,
    [PhysicalFilePath] nvarchar(1024) NOT NULL,
    [FileName] nvarchar(260) NOT NULL,
    [FileExtension] nvarchar(32) NOT NULL,
    [FileSize] bigint NOT NULL,
    [Status] int NOT NULL,
    [CreatedBy] nvarchar(128) NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_BusinessDocuments] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [DocumentLinks] (
    [Id] uniqueidentifier NOT NULL,
    [DocumentId] uniqueidentifier NOT NULL,
    [RelatedEntityType] nvarchar(64) NOT NULL,
    [RelatedEntityId] uniqueidentifier NOT NULL,
    [RelationType] nvarchar(64) NOT NULL,
    CONSTRAINT [PK_DocumentLinks] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [VirtualDrives] (
    [Id] uniqueidentifier NOT NULL,
    [Code] nvarchar(16) NOT NULL,
    [Name] nvarchar(64) NOT NULL,
    [DisplayName] nvarchar(128) NOT NULL,
    [Icon] nvarchar(256) NOT NULL,
    [SortOrder] int NOT NULL,
    [IsSystem] bit NOT NULL,
    [IsEnabled] bit NOT NULL,
    CONSTRAINT [PK_VirtualDrives] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [VirtualFolders] (
    [Id] uniqueidentifier NOT NULL,
    [DriveCode] nvarchar(16) NOT NULL,
    [ParentId] uniqueidentifier NULL,
    [Name] nvarchar(128) NOT NULL,
    [DisplayName] nvarchar(256) NOT NULL,
    [VirtualPath] nvarchar(512) NOT NULL,
    [FolderType] int NOT NULL,
    [EntityType] int NULL,
    [EntityId] uniqueidentifier NULL,
    [SortOrder] int NOT NULL,
    [IsSystem] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_VirtualFolders] PRIMARY KEY ([Id])
);
GO

IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Code', N'DisplayName', N'Icon', N'IsEnabled', N'IsSystem', N'Name', N'SortOrder') AND [object_id] = OBJECT_ID(N'[VirtualDrives]'))
    SET IDENTITY_INSERT [VirtualDrives] ON;
INSERT INTO [VirtualDrives] ([Id], [Code], [DisplayName], [Icon], [IsEnabled], [IsSystem], [Name], [SortOrder])
VALUES ('1b6a72e1-8a8e-4e63-8a4d-8f2e5c1f0001', N'C', N'C: SYSTEM', N'drive-system.ico', CAST(1 AS bit), CAST(1 AS bit), N'SYSTEM', 10),
('1b6a72e1-8a8e-4e63-8a4d-8f2e5c1f0002', N'D', N'D: ACCOUNTING', N'drive-accounting.ico', CAST(1 AS bit), CAST(1 AS bit), N'ACCOUNTING', 20),
('1b6a72e1-8a8e-4e63-8a4d-8f2e5c1f0003', N'E', N'E: SALES', N'drive-sales.ico', CAST(1 AS bit), CAST(1 AS bit), N'SALES', 30),
('1b6a72e1-8a8e-4e63-8a4d-8f2e5c1f0004', N'F', N'F: HR', N'drive-hr.ico', CAST(1 AS bit), CAST(1 AS bit), N'HR', 40),
('1b6a72e1-8a8e-4e63-8a4d-8f2e5c1f0005', N'G', N'G: LOGS', N'drive-logs.ico', CAST(1 AS bit), CAST(1 AS bit), N'LOGS', 50),
('1b6a72e1-8a8e-4e63-8a4d-8f2e5c1f0006', N'NET', N'Network', N'network.ico', CAST(1 AS bit), CAST(1 AS bit), N'NETWORK', 60);
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Code', N'DisplayName', N'Icon', N'IsEnabled', N'IsSystem', N'Name', N'SortOrder') AND [object_id] = OBJECT_ID(N'[VirtualDrives]'))
    SET IDENTITY_INSERT [VirtualDrives] OFF;
GO

CREATE INDEX [IX_Suppliers_SupplierName] ON [Suppliers] ([SupplierName]);
GO

CREATE INDEX [IX_Customers_CustomerName] ON [Customers] ([CustomerName]);
GO

CREATE INDEX [IX_BusinessDocuments_DocumentDate] ON [BusinessDocuments] ([DocumentDate]);
GO

CREATE INDEX [IX_BusinessDocuments_EntityType_EntityId] ON [BusinessDocuments] ([EntityType], [EntityId]);
GO

CREATE INDEX [IX_BusinessDocuments_VirtualPath] ON [BusinessDocuments] ([VirtualPath]);
GO

CREATE INDEX [IX_DocumentLinks_DocumentId] ON [DocumentLinks] ([DocumentId]);
GO

CREATE INDEX [IX_DocumentLinks_RelatedEntityType_RelatedEntityId] ON [DocumentLinks] ([RelatedEntityType], [RelatedEntityId]);
GO

CREATE UNIQUE INDEX [IX_VirtualDrives_Code] ON [VirtualDrives] ([Code]);
GO

CREATE UNIQUE INDEX [IX_VirtualFolders_VirtualPath] ON [VirtualFolders] ([VirtualPath]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260427034808_AddStockDetailManualColumns', N'8.0.7');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

ALTER TABLE [StockReceipts] ADD [ReceiverName] nvarchar(255) NOT NULL DEFAULT N'';
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260427054537_AddReceiptReceiverName', N'8.0.7');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

CREATE TABLE [LedgerAccounts] (
    [Id] uniqueidentifier NOT NULL,
    [AccountCode] nvarchar(50) NOT NULL,
    [AccountKind] nvarchar(10) NOT NULL,
    [AccountLevel] int NOT NULL,
    [AccountName] nvarchar(255) NOT NULL,
    [AccountNature] nvarchar(10) NOT NULL,
    [ParentAccountCode] nvarchar(50) NULL,
    [AccountType] nvarchar(40) NOT NULL,
    [AllowFund] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [CustomerDebt] bit NOT NULL,
    [FollowWarehouse] bit NOT NULL,
    [HasForeignCurrency] bit NOT NULL,
    [IsActive] bit NOT NULL,
    [IsCashAccount] bit NOT NULL,
    [ReportName] nvarchar(255) NOT NULL,
    [SortOrder] int NOT NULL,
    [SupplierDebt] bit NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_LedgerAccounts] PRIMARY KEY ([Id])
);
GO

CREATE UNIQUE INDEX [IX_LedgerAccounts_AccountCode] ON [LedgerAccounts] ([AccountCode]) WHERE [AccountCode] IS NOT NULL;
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260517090000_AddLedgerAccounts', N'8.0.7');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

CREATE TABLE [AccountingPeriods] (
    [Id] uniqueidentifier NOT NULL,
    [CompanyId] uniqueidentifier NULL,
    [PeriodCode] nvarchar(20) NOT NULL,
    [FromDate] datetime2 NOT NULL,
    [ToDate] datetime2 NOT NULL,
    [Status] int NOT NULL,
    [LockedAt] datetime2 NULL,
    [LockedBy] nvarchar(255) NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_AccountingPeriods] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [AccountingStandards] (
    [Id] uniqueidentifier NOT NULL,
    [Code] nvarchar(20) NOT NULL,
    [Name] nvarchar(255) NOT NULL,
    [EffectiveFrom] datetime2 NULL,
    [IsDefault] bit NOT NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_AccountingStandards] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [AccountRoleMappings] (
    [Id] uniqueidentifier NOT NULL,
    [AccountingStandardCode] nvarchar(20) NOT NULL,
    [RoleCode] nvarchar(50) NOT NULL,
    [AccountCode] nvarchar(50) NOT NULL,
    [IsDefault] bit NOT NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_AccountRoleMappings] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [AccountTagMappings] (
    [Id] uniqueidentifier NOT NULL,
    [AccountCode] nvarchar(50) NOT NULL,
    [TagCode] nvarchar(50) NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_AccountTagMappings] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [JournalHeaders] (
    [Id] uniqueidentifier NOT NULL,
    [JournalNo] nvarchar(50) NOT NULL,
    [JournalDate] datetime2 NOT NULL,
    [PostingDate] datetime2 NOT NULL,
    [SourceType] nvarchar(50) NOT NULL,
    [SourceId] uniqueidentifier NULL,
    [SourceNo] nvarchar(100) NOT NULL,
    [Description] nvarchar(1000) NOT NULL,
    [PeriodCode] nvarchar(20) NOT NULL,
    [Status] int NOT NULL,
    [TotalDebit] decimal(18,2) NOT NULL,
    [TotalCredit] decimal(18,2) NOT NULL,
    [Difference] decimal(18,2) NOT NULL,
    [CreatedBy] nvarchar(255) NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [PostedBy] nvarchar(255) NULL,
    [PostedAt] datetime2 NULL,
    [ReversedBy] nvarchar(255) NULL,
    [ReversedAt] datetime2 NULL,
    CONSTRAINT [PK_JournalHeaders] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [PostingBatches] (
    [Id] uniqueidentifier NOT NULL,
    [BatchNo] nvarchar(50) NOT NULL,
    [SourceType] nvarchar(50) NOT NULL,
    [SourceId] uniqueidentifier NULL,
    [Status] int NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [CreatedBy] nvarchar(255) NOT NULL,
    [Message] nvarchar(2000) NOT NULL,
    CONSTRAINT [PK_PostingBatches] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [PostingRules] (
    [Id] uniqueidentifier NOT NULL,
    [Code] nvarchar(100) NOT NULL,
    [Name] nvarchar(255) NOT NULL,
    [SourceType] nvarchar(50) NOT NULL,
    [AccountingStandardCode] nvarchar(20) NOT NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_PostingRules] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [JournalLines] (
    [Id] uniqueidentifier NOT NULL,
    [JournalHeaderId] uniqueidentifier NOT NULL,
    [LineNo] int NOT NULL,
    [AccountCode] nvarchar(50) NOT NULL,
    [AccountName] nvarchar(255) NOT NULL,
    [DebitAmount] decimal(18,2) NOT NULL,
    [CreditAmount] decimal(18,2) NOT NULL,
    [CurrencyCode] nvarchar(10) NOT NULL DEFAULT N'VND',
    [ExchangeRate] decimal(18,6) NOT NULL DEFAULT 1.0,
    [ObjectType] nvarchar(50) NULL,
    [ObjectId] uniqueidentifier NULL,
    [CustomerId] uniqueidentifier NULL,
    [SupplierId] uniqueidentifier NULL,
    [WarehouseId] uniqueidentifier NULL,
    [ItemId] uniqueidentifier NULL,
    [DepartmentId] uniqueidentifier NULL,
    [EmployeeId] uniqueidentifier NULL,
    [Description] nvarchar(1000) NULL,
    CONSTRAINT [PK_JournalLines] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_JournalLines_JournalHeaders_JournalHeaderId] FOREIGN KEY ([JournalHeaderId]) REFERENCES [JournalHeaders] ([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [PostingSources] (
    [Id] uniqueidentifier NOT NULL,
    [SourceType] nvarchar(50) NOT NULL,
    [SourceId] uniqueidentifier NOT NULL,
    [SourceNo] nvarchar(100) NOT NULL,
    [JournalHeaderId] uniqueidentifier NULL,
    [Status] nvarchar(50) NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_PostingSources] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_PostingSources_JournalHeaders_JournalHeaderId] FOREIGN KEY ([JournalHeaderId]) REFERENCES [JournalHeaders] ([Id]) ON DELETE NO ACTION
);
GO

CREATE TABLE [PostingRuleLines] (
    [Id] uniqueidentifier NOT NULL,
    [PostingRuleId] uniqueidentifier NOT NULL,
    [LineNo] int NOT NULL,
    [DebitCredit] int NOT NULL,
    [AccountRole] nvarchar(50) NOT NULL,
    [FixedAccountCode] nvarchar(50) NULL,
    [Formula] nvarchar(500) NULL,
    [DescriptionTemplate] nvarchar(500) NULL,
    CONSTRAINT [PK_PostingRuleLines] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_PostingRuleLines_PostingRules_PostingRuleId] FOREIGN KEY ([PostingRuleId]) REFERENCES [PostingRules] ([Id]) ON DELETE CASCADE
);
GO

IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Code', N'CreatedAt', N'EffectiveFrom', N'IsActive', N'IsDefault', N'Name', N'UpdatedAt') AND [object_id] = OBJECT_ID(N'[AccountingStandards]'))
    SET IDENTITY_INSERT [AccountingStandards] ON;
INSERT INTO [AccountingStandards] ([Id], [Code], [CreatedAt], [EffectiveFrom], [IsActive], [IsDefault], [Name], [UpdatedAt])
VALUES ('2e7ce9b2-4e4d-4f75-8e5e-000000000099', N'TT99', '2026-05-20T00:00:00.0000000Z', NULL, CAST(1 AS bit), CAST(1 AS bit), N'Thông tư 99', '2026-05-20T00:00:00.0000000Z'),
('2e7ce9b2-4e4d-4f75-8e5e-000000000200', N'TT200', '2026-05-20T00:00:00.0000000Z', NULL, CAST(1 AS bit), CAST(0 AS bit), N'Thông tư 200', '2026-05-20T00:00:00.0000000Z'),
('2e7ce9b2-4e4d-4f75-8e5e-000000000999', N'CUSTOM', '2026-05-20T00:00:00.0000000Z', NULL, CAST(1 AS bit), CAST(0 AS bit), N'Tùy chỉnh', '2026-05-20T00:00:00.0000000Z');
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Code', N'CreatedAt', N'EffectiveFrom', N'IsActive', N'IsDefault', N'Name', N'UpdatedAt') AND [object_id] = OBJECT_ID(N'[AccountingStandards]'))
    SET IDENTITY_INSERT [AccountingStandards] OFF;
GO

CREATE UNIQUE INDEX [IX_AccountingPeriods_CompanyId_PeriodCode] ON [AccountingPeriods] ([CompanyId], [PeriodCode]) WHERE [CompanyId] IS NOT NULL;
GO

CREATE INDEX [IX_AccountingPeriods_PeriodCode] ON [AccountingPeriods] ([PeriodCode]);
GO

CREATE UNIQUE INDEX [IX_AccountingStandards_Code] ON [AccountingStandards] ([Code]);
GO

CREATE UNIQUE INDEX [IX_AccountRoleMappings_AccountingStandardCode_RoleCode_AccountCode] ON [AccountRoleMappings] ([AccountingStandardCode], [RoleCode], [AccountCode]);
GO

CREATE INDEX [IX_AccountRoleMappings_AccountingStandardCode_RoleCode_IsDefault] ON [AccountRoleMappings] ([AccountingStandardCode], [RoleCode], [IsDefault]);
GO

CREATE UNIQUE INDEX [IX_AccountTagMappings_AccountCode_TagCode] ON [AccountTagMappings] ([AccountCode], [TagCode]);
GO

CREATE UNIQUE INDEX [IX_JournalHeaders_JournalNo] ON [JournalHeaders] ([JournalNo]);
GO

CREATE INDEX [IX_JournalHeaders_PeriodCode] ON [JournalHeaders] ([PeriodCode]);
GO

CREATE INDEX [IX_JournalHeaders_SourceType_SourceId] ON [JournalHeaders] ([SourceType], [SourceId]);
GO

CREATE INDEX [IX_JournalLines_AccountCode] ON [JournalLines] ([AccountCode]);
GO

CREATE INDEX [IX_JournalLines_JournalHeaderId] ON [JournalLines] ([JournalHeaderId]);
GO

CREATE UNIQUE INDEX [IX_PostingBatches_BatchNo] ON [PostingBatches] ([BatchNo]);
GO

CREATE INDEX [IX_PostingRuleLines_PostingRuleId] ON [PostingRuleLines] ([PostingRuleId]);
GO

CREATE UNIQUE INDEX [IX_PostingRules_Code] ON [PostingRules] ([Code]);
GO

CREATE INDEX [IX_PostingRules_SourceType_AccountingStandardCode] ON [PostingRules] ([SourceType], [AccountingStandardCode]);
GO

CREATE INDEX [IX_PostingSources_JournalHeaderId] ON [PostingSources] ([JournalHeaderId]);
GO

CREATE UNIQUE INDEX [IX_PostingSources_SourceType_SourceId] ON [PostingSources] ([SourceType], [SourceId]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260520051037_AddAccountingCoreEntities', N'8.0.7');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260520083954_FixBusinessDocumentAmountPrecision', N'8.0.7');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

ALTER TABLE [StockReceipts] ADD [JournalHeaderId] uniqueidentifier NULL;
GO

ALTER TABLE [StockReceipts] ADD [PostedAt] datetime2 NULL;
GO

ALTER TABLE [StockReceipts] ADD [PostedBy] nvarchar(255) NULL;
GO

ALTER TABLE [StockReceipts] ADD [PostingStatus] nvarchar(50) NULL;
GO

ALTER TABLE [StockIssues] ADD [JournalHeaderId] uniqueidentifier NULL;
GO

ALTER TABLE [StockIssues] ADD [PostedAt] datetime2 NULL;
GO

ALTER TABLE [StockIssues] ADD [PostedBy] nvarchar(255) NULL;
GO

ALTER TABLE [StockIssues] ADD [PostingStatus] nvarchar(50) NULL;
GO

IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'AccountCode', N'AccountingStandardCode', N'IsActive', N'IsDefault', N'RoleCode') AND [object_id] = OBJECT_ID(N'[AccountRoleMappings]'))
    SET IDENTITY_INSERT [AccountRoleMappings] ON;
INSERT INTO [AccountRoleMappings] ([Id], [AccountCode], [AccountingStandardCode], [IsActive], [IsDefault], [RoleCode])
VALUES ('6f6b8f41-9c2d-4d66-9b2f-000000000156', N'156', N'TT99', CAST(1 AS bit), CAST(1 AS bit), N'Inventory'),
('6f6b8f41-9c2d-4d66-9b2f-000000000331', N'331', N'TT99', CAST(1 AS bit), CAST(1 AS bit), N'Payable'),
('6f6b8f41-9c2d-4d66-9b2f-000000000632', N'632', N'TT99', CAST(1 AS bit), CAST(1 AS bit), N'COGS');
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'AccountCode', N'AccountingStandardCode', N'IsActive', N'IsDefault', N'RoleCode') AND [object_id] = OBJECT_ID(N'[AccountRoleMappings]'))
    SET IDENTITY_INSERT [AccountRoleMappings] OFF;
GO

CREATE INDEX [IX_StockReceipts_JournalHeaderId] ON [StockReceipts] ([JournalHeaderId]);
GO

CREATE INDEX [IX_StockIssues_JournalHeaderId] ON [StockIssues] ([JournalHeaderId]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260520093157_AddStockVoucherPosting', N'8.0.7');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

ALTER TABLE [LedgerAccounts] ADD [AccountCategory] nvarchar(50) NULL;
GO

ALTER TABLE [LedgerAccounts] ADD [AccountingStandardCode] nvarchar(20) NOT NULL DEFAULT N'TT99';
GO

ALTER TABLE [LedgerAccounts] ADD [AllowPosting] bit NOT NULL DEFAULT CAST(1 AS bit);
GO

ALTER TABLE [LedgerAccounts] ADD [IsDeletable] bit NOT NULL DEFAULT CAST(1 AS bit);
GO

ALTER TABLE [LedgerAccounts] ADD [IsEditable] bit NOT NULL DEFAULT CAST(1 AS bit);
GO

ALTER TABLE [LedgerAccounts] ADD [IsFinancialAccount] bit NOT NULL DEFAULT CAST(1 AS bit);
GO

ALTER TABLE [LedgerAccounts] ADD [IsManagementAccount] bit NOT NULL DEFAULT CAST(0 AS bit);
GO

ALTER TABLE [LedgerAccounts] ADD [IsSystemAccount] bit NOT NULL DEFAULT CAST(0 AS bit);
GO

ALTER TABLE [LedgerAccounts] ADD [NormalBalance] nvarchar(20) NOT NULL DEFAULT N'Debit';
GO

ALTER TABLE [LedgerAccounts] ADD [RequireCurrency] bit NOT NULL DEFAULT CAST(0 AS bit);
GO

ALTER TABLE [LedgerAccounts] ADD [RequireCustomer] bit NOT NULL DEFAULT CAST(0 AS bit);
GO

ALTER TABLE [LedgerAccounts] ADD [RequireDepartment] bit NOT NULL DEFAULT CAST(0 AS bit);
GO

ALTER TABLE [LedgerAccounts] ADD [RequireEmployee] bit NOT NULL DEFAULT CAST(0 AS bit);
GO

ALTER TABLE [LedgerAccounts] ADD [RequireItem] bit NOT NULL DEFAULT CAST(0 AS bit);
GO

ALTER TABLE [LedgerAccounts] ADD [RequireSupplier] bit NOT NULL DEFAULT CAST(0 AS bit);
GO

ALTER TABLE [LedgerAccounts] ADD [RequireWarehouse] bit NOT NULL DEFAULT CAST(0 AS bit);
GO

CREATE INDEX [IX_LedgerAccounts_AccountCategory] ON [LedgerAccounts] ([AccountCategory]);
GO

CREATE INDEX [IX_LedgerAccounts_AccountingStandardCode] ON [LedgerAccounts] ([AccountingStandardCode]);
GO

CREATE INDEX [IX_LedgerAccounts_AllowPosting] ON [LedgerAccounts] ([AllowPosting]);
GO

UPDATE [LedgerAccounts]
SET [AccountingStandardCode] = N'TT99'
WHERE [AccountingStandardCode] IS NULL OR LTRIM(RTRIM([AccountingStandardCode])) = N'';

UPDATE [LedgerAccounts]
SET [AccountCategory] = CASE
    WHEN [AccountCode] LIKE N'1%' THEN N'Asset'
    WHEN [AccountCode] LIKE N'2%' OR [AccountCode] LIKE N'3%' THEN N'Liability'
    WHEN [AccountCode] LIKE N'4%' THEN N'Equity'
    WHEN [AccountCode] LIKE N'5%' OR [AccountCode] LIKE N'7%' THEN N'Revenue'
    WHEN [AccountCode] LIKE N'6%' OR [AccountCode] LIKE N'8%' THEN N'Expense'
    ELSE N'Other'
END
WHERE [AccountCategory] IS NULL OR LTRIM(RTRIM([AccountCategory])) = N'';

UPDATE [LedgerAccounts]
SET [NormalBalance] = CASE
    WHEN [AccountCode] LIKE N'2%' OR [AccountCode] LIKE N'3%' OR [AccountCode] LIKE N'4%' OR [AccountCode] LIKE N'5%' OR [AccountCode] LIKE N'7%' THEN N'Credit'
    ELSE N'Debit'
END
WHERE [NormalBalance] IS NULL OR LTRIM(RTRIM([NormalBalance])) = N'';

UPDATE [LedgerAccounts]
SET [IsFinancialAccount] = CASE WHEN [AccountCode] LIKE N'9%' THEN CAST(0 AS bit) ELSE CAST(1 AS bit) END,
    [IsManagementAccount] = CASE WHEN [AccountCode] LIKE N'9%' THEN CAST(1 AS bit) ELSE CAST(0 AS bit) END;

UPDATE [LedgerAccounts]
SET [AllowPosting] = CASE WHEN [AccountKind] = N'H' THEN CAST(0 AS bit) ELSE CAST(1 AS bit) END,
    [IsDeletable] = CASE WHEN [AccountKind] = N'H' THEN CAST(0 AS bit) ELSE CAST(1 AS bit) END;

UPDATE [AccountRoleMappings]
SET [AccountCode] = N'1561'
WHERE [AccountingStandardCode] = N'TT99' AND [RoleCode] = N'Inventory' AND [AccountCode] = N'156'
    AND EXISTS (SELECT 1 FROM [LedgerAccounts] WHERE [AccountCode] = N'1561');

UPDATE [AccountRoleMappings]
SET [AccountCode] = N'3311'
WHERE [AccountingStandardCode] = N'TT99' AND [RoleCode] = N'Payable' AND [AccountCode] = N'331'
    AND EXISTS (SELECT 1 FROM [LedgerAccounts] WHERE [AccountCode] = N'3311');

UPDATE [AccountRoleMappings]
SET [AccountCode] = N'6321'
WHERE [AccountingStandardCode] = N'TT99' AND [RoleCode] = N'COGS' AND [AccountCode] = N'632'
    AND EXISTS (SELECT 1 FROM [LedgerAccounts] WHERE [AccountCode] = N'6321');
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260520145845_AddLedgerAccountMetadata', N'8.0.7');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

UPDATE [AccountRoleMappings] SET [AccountCode] = N'1561'
WHERE [Id] = '6f6b8f41-9c2d-4d66-9b2f-000000000156';
SELECT @@ROWCOUNT;

GO

UPDATE [AccountRoleMappings] SET [AccountCode] = N'3311'
WHERE [Id] = '6f6b8f41-9c2d-4d66-9b2f-000000000331';
SELECT @@ROWCOUNT;

GO

UPDATE [AccountRoleMappings] SET [AccountCode] = N'6321'
WHERE [Id] = '6f6b8f41-9c2d-4d66-9b2f-000000000632';
SELECT @@ROWCOUNT;

GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260520150643_AlignPostingMappingsToDetailAccounts', N'8.0.7');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

CREATE TABLE [AgingSnapshots] (
    [Id] uniqueidentifier NOT NULL,
    [SnapshotDate] datetime2 NOT NULL,
    [PartnerType] nvarchar(20) NOT NULL,
    [PartnerId] uniqueidentifier NOT NULL,
    [CurrentAmount] decimal(18,2) NOT NULL,
    [Overdue1To30] decimal(18,2) NOT NULL,
    [Overdue31To60] decimal(18,2) NOT NULL,
    [Overdue61To90] decimal(18,2) NOT NULL,
    [OverdueOver90] decimal(18,2) NOT NULL,
    [TotalAmount] decimal(18,2) NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_AgingSnapshots] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [DebtReconciliations] (
    [Id] uniqueidentifier NOT NULL,
    [ReconciliationNo] nvarchar(50) NOT NULL,
    [PartnerType] nvarchar(20) NOT NULL,
    [PartnerId] uniqueidentifier NOT NULL,
    [FromDate] datetime2 NOT NULL,
    [ToDate] datetime2 NOT NULL,
    [Status] nvarchar(30) NOT NULL,
    [TotalDebit] decimal(18,2) NOT NULL,
    [TotalCredit] decimal(18,2) NOT NULL,
    [Balance] decimal(18,2) NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [CreatedBy] nvarchar(255) NULL,
    [ConfirmedAt] datetime2 NULL,
    [ConfirmedBy] nvarchar(255) NULL,
    CONSTRAINT [PK_DebtReconciliations] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [PayableTransactions] (
    [Id] uniqueidentifier NOT NULL,
    [SupplierId] uniqueidentifier NOT NULL,
    [SourceType] nvarchar(50) NOT NULL,
    [SourceId] uniqueidentifier NULL,
    [JournalHeaderId] uniqueidentifier NULL,
    [JournalLineId] uniqueidentifier NULL,
    [DocumentNo] nvarchar(100) NOT NULL,
    [DocumentDate] datetime2 NOT NULL,
    [DueDate] datetime2 NULL,
    [Description] nvarchar(1000) NULL,
    [DebitAmount] decimal(18,2) NOT NULL,
    [CreditAmount] decimal(18,2) NOT NULL,
    [BalanceAmount] decimal(18,2) NOT NULL,
    [CurrencyCode] nvarchar(10) NOT NULL DEFAULT N'VND',
    [Status] nvarchar(30) NOT NULL DEFAULT N'Open',
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_PayableTransactions] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_PayableTransactions_JournalHeaders_JournalHeaderId] FOREIGN KEY ([JournalHeaderId]) REFERENCES [JournalHeaders] ([Id]),
    CONSTRAINT [FK_PayableTransactions_JournalLines_JournalLineId] FOREIGN KEY ([JournalLineId]) REFERENCES [JournalLines] ([Id])
);
GO

CREATE TABLE [PaymentAllocations] (
    [Id] uniqueidentifier NOT NULL,
    [PaymentSourceType] nvarchar(50) NOT NULL,
    [PaymentSourceId] uniqueidentifier NULL,
    [PaymentJournalHeaderId] uniqueidentifier NULL,
    [TargetType] nvarchar(20) NOT NULL,
    [TargetTransactionId] uniqueidentifier NOT NULL,
    [AllocatedAmount] decimal(18,2) NOT NULL,
    [AllocationDate] datetime2 NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [CreatedBy] nvarchar(255) NULL,
    CONSTRAINT [PK_PaymentAllocations] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_PaymentAllocations_JournalHeaders_PaymentJournalHeaderId] FOREIGN KEY ([PaymentJournalHeaderId]) REFERENCES [JournalHeaders] ([Id])
);
GO

CREATE TABLE [ReceivableTransactions] (
    [Id] uniqueidentifier NOT NULL,
    [CustomerId] uniqueidentifier NOT NULL,
    [SourceType] nvarchar(50) NOT NULL,
    [SourceId] uniqueidentifier NULL,
    [JournalHeaderId] uniqueidentifier NULL,
    [JournalLineId] uniqueidentifier NULL,
    [DocumentNo] nvarchar(100) NOT NULL,
    [DocumentDate] datetime2 NOT NULL,
    [DueDate] datetime2 NULL,
    [Description] nvarchar(1000) NULL,
    [DebitAmount] decimal(18,2) NOT NULL,
    [CreditAmount] decimal(18,2) NOT NULL,
    [BalanceAmount] decimal(18,2) NOT NULL,
    [CurrencyCode] nvarchar(10) NOT NULL DEFAULT N'VND',
    [Status] nvarchar(30) NOT NULL DEFAULT N'Open',
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_ReceivableTransactions] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ReceivableTransactions_JournalHeaders_JournalHeaderId] FOREIGN KEY ([JournalHeaderId]) REFERENCES [JournalHeaders] ([Id]),
    CONSTRAINT [FK_ReceivableTransactions_JournalLines_JournalLineId] FOREIGN KEY ([JournalLineId]) REFERENCES [JournalLines] ([Id])
);
GO

CREATE TABLE [DebtReconciliationLines] (
    [Id] uniqueidentifier NOT NULL,
    [DebtReconciliationId] uniqueidentifier NOT NULL,
    [SourceType] nvarchar(50) NOT NULL,
    [SourceId] uniqueidentifier NULL,
    [DocumentNo] nvarchar(100) NOT NULL,
    [DocumentDate] datetime2 NOT NULL,
    [DebitAmount] decimal(18,2) NOT NULL,
    [CreditAmount] decimal(18,2) NOT NULL,
    [BalanceAmount] decimal(18,2) NOT NULL,
    [Description] nvarchar(1000) NULL,
    CONSTRAINT [PK_DebtReconciliationLines] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_DebtReconciliationLines_DebtReconciliations_DebtReconciliationId] FOREIGN KEY ([DebtReconciliationId]) REFERENCES [DebtReconciliations] ([Id]) ON DELETE CASCADE
);
GO

IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'AccountCode', N'AccountingStandardCode', N'IsActive', N'IsDefault', N'RoleCode') AND [object_id] = OBJECT_ID(N'[AccountRoleMappings]'))
    SET IDENTITY_INSERT [AccountRoleMappings] ON;
INSERT INTO [AccountRoleMappings] ([Id], [AccountCode], [AccountingStandardCode], [IsActive], [IsDefault], [RoleCode])
VALUES ('6f6b8f41-9c2d-4d66-9b2f-000000000131', N'1311', N'TT99', CAST(1 AS bit), CAST(1 AS bit), N'Receivable');
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'AccountCode', N'AccountingStandardCode', N'IsActive', N'IsDefault', N'RoleCode') AND [object_id] = OBJECT_ID(N'[AccountRoleMappings]'))
    SET IDENTITY_INSERT [AccountRoleMappings] OFF;
GO

CREATE INDEX [IX_AgingSnapshots_PartnerType_PartnerId] ON [AgingSnapshots] ([PartnerType], [PartnerId]);
GO

CREATE INDEX [IX_AgingSnapshots_SnapshotDate] ON [AgingSnapshots] ([SnapshotDate]);
GO

CREATE INDEX [IX_DebtReconciliationLines_DebtReconciliationId] ON [DebtReconciliationLines] ([DebtReconciliationId]);
GO

CREATE INDEX [IX_DebtReconciliations_PartnerType_PartnerId] ON [DebtReconciliations] ([PartnerType], [PartnerId]);
GO

CREATE UNIQUE INDEX [IX_DebtReconciliations_ReconciliationNo] ON [DebtReconciliations] ([ReconciliationNo]);
GO

CREATE INDEX [IX_DebtReconciliations_Status] ON [DebtReconciliations] ([Status]);
GO

CREATE INDEX [IX_PayableTransactions_JournalHeaderId] ON [PayableTransactions] ([JournalHeaderId]);
GO

CREATE UNIQUE INDEX [IX_PayableTransactions_JournalLineId] ON [PayableTransactions] ([JournalLineId]) WHERE [JournalLineId] IS NOT NULL;
GO

CREATE INDEX [IX_PayableTransactions_SourceType_SourceId] ON [PayableTransactions] ([SourceType], [SourceId]);
GO

CREATE INDEX [IX_PayableTransactions_Status] ON [PayableTransactions] ([Status]);
GO

CREATE INDEX [IX_PayableTransactions_SupplierId] ON [PayableTransactions] ([SupplierId]);
GO

CREATE INDEX [IX_PaymentAllocations_PaymentJournalHeaderId] ON [PaymentAllocations] ([PaymentJournalHeaderId]);
GO

CREATE INDEX [IX_PaymentAllocations_TargetType_TargetTransactionId] ON [PaymentAllocations] ([TargetType], [TargetTransactionId]);
GO

CREATE INDEX [IX_ReceivableTransactions_CustomerId] ON [ReceivableTransactions] ([CustomerId]);
GO

CREATE INDEX [IX_ReceivableTransactions_JournalHeaderId] ON [ReceivableTransactions] ([JournalHeaderId]);
GO

CREATE UNIQUE INDEX [IX_ReceivableTransactions_JournalLineId] ON [ReceivableTransactions] ([JournalLineId]) WHERE [JournalLineId] IS NOT NULL;
GO

CREATE INDEX [IX_ReceivableTransactions_SourceType_SourceId] ON [ReceivableTransactions] ([SourceType], [SourceId]);
GO

CREATE INDEX [IX_ReceivableTransactions_Status] ON [ReceivableTransactions] ([Status]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260520153302_AddArApPersistedTransactions', N'8.0.7');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

CREATE TABLE [ImportSessions] (
    [Id] uniqueidentifier NOT NULL,
    [TemplateCode] nvarchar(100) NOT NULL,
    [FileName] nvarchar(255) NOT NULL,
    [OriginalFilePath] nvarchar(1000) NULL,
    [TempFilePath] nvarchar(1000) NULL,
    [TargetEntity] nvarchar(100) NOT NULL,
    [Status] nvarchar(30) NOT NULL,
    [TotalRows] int NOT NULL,
    [ValidRows] int NOT NULL,
    [WarningRows] int NOT NULL,
    [ErrorRows] int NOT NULL,
    [StartedAt] datetime2 NOT NULL,
    [CompletedAt] datetime2 NULL,
    [CreatedBy] nvarchar(255) NULL,
    [ErrorMessage] nvarchar(2000) NULL,
    CONSTRAINT [PK_ImportSessions] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [ImportTemplates] (
    [Id] uniqueidentifier NOT NULL,
    [Code] nvarchar(100) NOT NULL,
    [Name] nvarchar(255) NOT NULL,
    [TargetEntity] nvarchar(100) NOT NULL,
    [Description] nvarchar(1000) NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_ImportTemplates] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [ImportRows] (
    [Id] uniqueidentifier NOT NULL,
    [ImportSessionId] uniqueidentifier NOT NULL,
    [RowNumber] int NOT NULL,
    [RawJson] nvarchar(max) NOT NULL,
    [NormalizedJson] nvarchar(max) NULL,
    [Status] nvarchar(30) NOT NULL,
    [ErrorCount] int NOT NULL,
    [WarningCount] int NOT NULL,
    CONSTRAINT [PK_ImportRows] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ImportRows_ImportSessions_ImportSessionId] FOREIGN KEY ([ImportSessionId]) REFERENCES [ImportSessions] ([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [ImportCellErrors] (
    [Id] uniqueidentifier NOT NULL,
    [ImportRowId] uniqueidentifier NOT NULL,
    [ColumnName] nvarchar(100) NOT NULL,
    [CellAddress] nvarchar(20) NOT NULL,
    [Severity] nvarchar(20) NOT NULL,
    [ErrorCode] nvarchar(100) NOT NULL,
    [Message] nvarchar(500) NOT NULL,
    [SuggestedValue] nvarchar(500) NULL,
    CONSTRAINT [PK_ImportCellErrors] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ImportCellErrors_ImportRows_ImportRowId] FOREIGN KEY ([ImportRowId]) REFERENCES [ImportRows] ([Id]) ON DELETE CASCADE
);
GO

IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Code', N'CreatedAt', N'Description', N'IsActive', N'Name', N'TargetEntity', N'UpdatedAt') AND [object_id] = OBJECT_ID(N'[ImportTemplates]'))
    SET IDENTITY_INSERT [ImportTemplates] ON;
INSERT INTO [ImportTemplates] ([Id], [Code], [CreatedAt], [Description], [IsActive], [Name], [TargetEntity], [UpdatedAt])
VALUES ('0e8bc591-4f9e-4f26-9b25-000000000001', N'ITEMS_DEFAULT', '2026-05-20T00:00:00.0000000Z', N'Template chuẩn để nhập mã hàng, tên hàng, đơn vị tính, nhóm hàng và nhà cung cấp.', CAST(1 AS bit), N'Nhập danh mục hàng hóa', N'Items', '2026-05-20T00:00:00.0000000Z');
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Code', N'CreatedAt', N'Description', N'IsActive', N'Name', N'TargetEntity', N'UpdatedAt') AND [object_id] = OBJECT_ID(N'[ImportTemplates]'))
    SET IDENTITY_INSERT [ImportTemplates] OFF;
GO

CREATE INDEX [IX_ImportCellErrors_ImportRowId] ON [ImportCellErrors] ([ImportRowId]);
GO

CREATE INDEX [IX_ImportCellErrors_Severity] ON [ImportCellErrors] ([Severity]);
GO

CREATE INDEX [IX_ImportRows_ImportSessionId] ON [ImportRows] ([ImportSessionId]);
GO

CREATE INDEX [IX_ImportRows_Status] ON [ImportRows] ([Status]);
GO

CREATE INDEX [IX_ImportSessions_Status] ON [ImportSessions] ([Status]);
GO

CREATE INDEX [IX_ImportSessions_TargetEntity] ON [ImportSessions] ([TargetEntity]);
GO

CREATE INDEX [IX_ImportSessions_TemplateCode] ON [ImportSessions] ([TemplateCode]);
GO

CREATE UNIQUE INDEX [IX_ImportTemplates_Code] ON [ImportTemplates] ([Code]);
GO

CREATE INDEX [IX_ImportTemplates_TargetEntity] ON [ImportTemplates] ([TargetEntity]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260520155935_AddImportEngine', N'8.0.7');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

CREATE TABLE [InventoryCostLayers] (
    [Id] uniqueidentifier NOT NULL,
    [ItemId] int NOT NULL,
    [WarehouseId] int NOT NULL,
    [SourceType] nvarchar(50) NOT NULL,
    [SourceId] uniqueidentifier NULL,
    [SourceDetailId] uniqueidentifier NULL,
    [DocumentNo] nvarchar(100) NOT NULL,
    [DocumentDate] datetime2 NOT NULL,
    [QuantityIn] decimal(18,4) NOT NULL,
    [QuantityOut] decimal(18,4) NOT NULL,
    [UnitCost] decimal(18,6) NOT NULL,
    [TotalCost] decimal(18,2) NOT NULL,
    [RemainingQuantity] decimal(18,4) NOT NULL,
    [RemainingCost] decimal(18,2) NOT NULL,
    [CostMethod] nvarchar(30) NOT NULL,
    [Status] nvarchar(30) NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_InventoryCostLayers] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [InventoryCostTransactions] (
    [Id] uniqueidentifier NOT NULL,
    [ItemId] int NOT NULL,
    [WarehouseId] int NOT NULL,
    [SourceType] nvarchar(50) NOT NULL,
    [SourceId] uniqueidentifier NULL,
    [SourceDetailId] uniqueidentifier NULL,
    [DocumentNo] nvarchar(100) NOT NULL,
    [DocumentDate] datetime2 NOT NULL,
    [Quantity] decimal(18,4) NOT NULL,
    [UnitCost] decimal(18,6) NOT NULL,
    [TotalCost] decimal(18,2) NOT NULL,
    [CostMethod] nvarchar(30) NOT NULL,
    [JournalHeaderId] uniqueidentifier NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_InventoryCostTransactions] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_InventoryCostTransactions_JournalHeaders_JournalHeaderId] FOREIGN KEY ([JournalHeaderId]) REFERENCES [JournalHeaders] ([Id])
);
GO

CREATE INDEX [IX_InventoryCostLayers_DocumentDate] ON [InventoryCostLayers] ([DocumentDate]);
GO

CREATE INDEX [IX_InventoryCostLayers_ItemId_WarehouseId] ON [InventoryCostLayers] ([ItemId], [WarehouseId]);
GO

CREATE INDEX [IX_InventoryCostLayers_SourceDetailId] ON [InventoryCostLayers] ([SourceDetailId]);
GO

CREATE INDEX [IX_InventoryCostLayers_SourceType_SourceId] ON [InventoryCostLayers] ([SourceType], [SourceId]);
GO

CREATE INDEX [IX_InventoryCostLayers_Status] ON [InventoryCostLayers] ([Status]);
GO

CREATE INDEX [IX_InventoryCostTransactions_DocumentDate] ON [InventoryCostTransactions] ([DocumentDate]);
GO

CREATE INDEX [IX_InventoryCostTransactions_ItemId_WarehouseId] ON [InventoryCostTransactions] ([ItemId], [WarehouseId]);
GO

CREATE INDEX [IX_InventoryCostTransactions_JournalHeaderId] ON [InventoryCostTransactions] ([JournalHeaderId]);
GO

CREATE INDEX [IX_InventoryCostTransactions_SourceDetailId] ON [InventoryCostTransactions] ([SourceDetailId]);
GO

CREATE INDEX [IX_InventoryCostTransactions_SourceType_SourceId] ON [InventoryCostTransactions] ([SourceType], [SourceId]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260521020906_AddInventoryCosting', N'8.0.7');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

CREATE TABLE [ReportExecutions] (
    [Id] uniqueidentifier NOT NULL,
    [ReportCode] nvarchar(100) NOT NULL,
    [ParametersJson] nvarchar(max) NULL,
    [Status] nvarchar(30) NOT NULL,
    [StartedAt] datetime2 NOT NULL,
    [CompletedAt] datetime2 NULL,
    [RowCount] int NOT NULL,
    [ErrorMessage] nvarchar(2000) NULL,
    [ExecutedBy] nvarchar(255) NULL,
    CONSTRAINT [PK_ReportExecutions] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [ReportTemplates] (
    [Id] uniqueidentifier NOT NULL,
    [Code] nvarchar(100) NOT NULL,
    [Name] nvarchar(255) NOT NULL,
    [Category] nvarchar(100) NOT NULL,
    [Description] nvarchar(1000) NULL,
    [DataSource] nvarchar(100) NOT NULL,
    [QueryDefinition] nvarchar(max) NULL,
    [LayoutDefinition] nvarchar(max) NULL,
    [IsSystem] bit NOT NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_ReportTemplates] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [ReportDefinitions] (
    [Id] uniqueidentifier NOT NULL,
    [ReportTemplateId] uniqueidentifier NOT NULL,
    [VersionNo] int NOT NULL,
    [DefinitionJson] nvarchar(max) NOT NULL,
    [IsPublished] bit NOT NULL,
    [PublishedAt] datetime2 NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_ReportDefinitions] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ReportDefinitions_ReportTemplates_ReportTemplateId] FOREIGN KEY ([ReportTemplateId]) REFERENCES [ReportTemplates] ([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [ReportParameters] (
    [Id] uniqueidentifier NOT NULL,
    [ReportTemplateId] uniqueidentifier NOT NULL,
    [Name] nvarchar(100) NOT NULL,
    [DisplayName] nvarchar(255) NOT NULL,
    [DataType] nvarchar(50) NOT NULL,
    [DefaultValue] nvarchar(500) NULL,
    [IsRequired] bit NOT NULL,
    [SortOrder] int NOT NULL,
    CONSTRAINT [PK_ReportParameters] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ReportParameters_ReportTemplates_ReportTemplateId] FOREIGN KEY ([ReportTemplateId]) REFERENCES [ReportTemplates] ([Id]) ON DELETE CASCADE
);
GO

IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Category', N'Code', N'CreatedAt', N'DataSource', N'Description', N'IsActive', N'IsSystem', N'LayoutDefinition', N'Name', N'QueryDefinition', N'UpdatedAt') AND [object_id] = OBJECT_ID(N'[ReportTemplates]'))
    SET IDENTITY_INSERT [ReportTemplates] ON;
INSERT INTO [ReportTemplates] ([Id], [Category], [Code], [CreatedAt], [DataSource], [Description], [IsActive], [IsSystem], [LayoutDefinition], [Name], [QueryDefinition], [UpdatedAt])
VALUES ('4a9a7b4f-ac6e-4e91-9a11-000000000001', N'Accounting', N'GENERAL_LEDGER', '2026-05-21T00:00:00.0000000Z', N'Journal', N'Báo cáo sổ cái từ Journal persisted.', CAST(1 AS bit), CAST(1 AS bit), NULL, N'Sổ cái', NULL, '2026-05-21T00:00:00.0000000Z'),
('4a9a7b4f-ac6e-4e91-9a11-000000000002', N'Accounting', N'TRIAL_BALANCE', '2026-05-21T00:00:00.0000000Z', N'Journal', N'Tổng hợp phát sinh theo tài khoản từ Journal persisted.', CAST(1 AS bit), CAST(1 AS bit), NULL, N'Cân đối phát sinh', NULL, '2026-05-21T00:00:00.0000000Z'),
('4a9a7b4f-ac6e-4e91-9a11-000000000003', N'Inventory', N'INVENTORY_SUMMARY', '2026-05-21T00:00:00.0000000Z', N'StockBalance', N'Tổng hợp số lượng và giá trị tồn kho.', CAST(1 AS bit), CAST(1 AS bit), NULL, N'Tổng hợp tồn kho', NULL, '2026-05-21T00:00:00.0000000Z'),
('4a9a7b4f-ac6e-4e91-9a11-000000000004', N'Inventory', N'INVENTORY_MOVEMENT', '2026-05-21T00:00:00.0000000Z', N'StockReceipt,StockIssue,InventoryCostTransaction', N'Báo cáo phát sinh nhập xuất kho.', CAST(1 AS bit), CAST(1 AS bit), NULL, N'Nhập xuất tồn', NULL, '2026-05-21T00:00:00.0000000Z'),
('4a9a7b4f-ac6e-4e91-9a11-000000000005', N'Payable', N'AP_AGING', '2026-05-21T00:00:00.0000000Z', N'PayableTransaction', N'Báo cáo tuổi nợ nhà cung cấp từ công nợ persisted.', CAST(1 AS bit), CAST(1 AS bit), NULL, N'Tuổi nợ phải trả', NULL, '2026-05-21T00:00:00.0000000Z'),
('4a9a7b4f-ac6e-4e91-9a11-000000000006', N'Receivable', N'AR_AGING', '2026-05-21T00:00:00.0000000Z', N'ReceivableTransaction', N'Báo cáo tuổi nợ khách hàng từ công nợ persisted.', CAST(1 AS bit), CAST(1 AS bit), NULL, N'Tuổi nợ phải thu', NULL, '2026-05-21T00:00:00.0000000Z');
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Category', N'Code', N'CreatedAt', N'DataSource', N'Description', N'IsActive', N'IsSystem', N'LayoutDefinition', N'Name', N'QueryDefinition', N'UpdatedAt') AND [object_id] = OBJECT_ID(N'[ReportTemplates]'))
    SET IDENTITY_INSERT [ReportTemplates] OFF;
GO

CREATE INDEX [IX_ReportDefinitions_IsPublished] ON [ReportDefinitions] ([IsPublished]);
GO

CREATE INDEX [IX_ReportDefinitions_ReportTemplateId] ON [ReportDefinitions] ([ReportTemplateId]);
GO

CREATE INDEX [IX_ReportExecutions_ReportCode] ON [ReportExecutions] ([ReportCode]);
GO

CREATE INDEX [IX_ReportExecutions_Status] ON [ReportExecutions] ([Status]);
GO

CREATE INDEX [IX_ReportParameters_ReportTemplateId] ON [ReportParameters] ([ReportTemplateId]);
GO

CREATE INDEX [IX_ReportTemplates_Category] ON [ReportTemplates] ([Category]);
GO

CREATE UNIQUE INDEX [IX_ReportTemplates_Code] ON [ReportTemplates] ([Code]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260521023835_AddReportEngine', N'8.0.7');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

CREATE TABLE [AccountingStandardTemplates] (
    [Id] uniqueidentifier NOT NULL,
    [StandardCode] nvarchar(20) NOT NULL,
    [TemplateType] nvarchar(50) NOT NULL,
    [TemplateCode] nvarchar(100) NOT NULL,
    [Name] nvarchar(255) NOT NULL,
    [DefinitionJson] nvarchar(max) NOT NULL,
    [IsSystem] bit NOT NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_AccountingStandardTemplates] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [AccountTemplates] (
    [Id] uniqueidentifier NOT NULL,
    [StandardCode] nvarchar(20) NOT NULL,
    [AccountCode] nvarchar(50) NOT NULL,
    [AccountName] nvarchar(255) NOT NULL,
    [ParentCode] nvarchar(50) NULL,
    [AccountLevel] int NOT NULL,
    [AccountKind] nvarchar(10) NULL,
    [NormalBalance] nvarchar(20) NOT NULL,
    [AccountCategory] nvarchar(50) NOT NULL,
    [AllowPosting] bit NOT NULL,
    [IsSystemAccount] bit NOT NULL,
    [IsFinancialAccount] bit NOT NULL,
    [IsManagementAccount] bit NOT NULL,
    [RequireCustomer] bit NOT NULL,
    [RequireSupplier] bit NOT NULL,
    [RequireWarehouse] bit NOT NULL,
    [RequireItem] bit NOT NULL,
    [RequireDepartment] bit NOT NULL,
    [RequireEmployee] bit NOT NULL,
    [RequireCurrency] bit NOT NULL,
    [SortOrder] int NOT NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_AccountTemplates] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [CompanyAccountingProfiles] (
    [Id] uniqueidentifier NOT NULL,
    [CompanyId] uniqueidentifier NULL,
    [CompanyName] nvarchar(255) NULL,
    [AccountingStandardCode] nvarchar(20) NOT NULL,
    [FiscalYearStartMonth] int NOT NULL,
    [FiscalYearStartDay] int NOT NULL,
    [EffectiveFrom] datetime2 NOT NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_CompanyAccountingProfiles] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [FinancialStatementMappings] (
    [Id] uniqueidentifier NOT NULL,
    [StandardCode] nvarchar(20) NOT NULL,
    [ReportCode] nvarchar(100) NOT NULL,
    [LineCode] nvarchar(100) NOT NULL,
    [LineName] nvarchar(255) NOT NULL,
    [AccountFilter] nvarchar(500) NULL,
    [AccountRoleFilter] nvarchar(500) NULL,
    [Formula] nvarchar(1000) NULL,
    [SortOrder] int NOT NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_FinancialStatementMappings] PRIMARY KEY ([Id])
);
GO

IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'AccountCode', N'AccountingStandardCode', N'IsActive', N'IsDefault', N'RoleCode') AND [object_id] = OBJECT_ID(N'[AccountRoleMappings]'))
    SET IDENTITY_INSERT [AccountRoleMappings] ON;
INSERT INTO [AccountRoleMappings] ([Id], [AccountCode], [AccountingStandardCode], [IsActive], [IsDefault], [RoleCode])
VALUES ('084ef9b2-3d28-4053-2334-52af8a409ad3', N'1331', N'TT99', CAST(1 AS bit), CAST(1 AS bit), N'VATInput'),
('1261722a-f44e-f1eb-6c9a-fa4ae07f73c7', N'1111', N'TT99', CAST(1 AS bit), CAST(1 AS bit), N'Cash'),
('16daf76a-7d75-a8dd-a176-9463adc93891', N'5111', N'TT200', CAST(1 AS bit), CAST(1 AS bit), N'Revenue'),
('27e66cfb-8ba2-fbf5-b428-90b89d2e5747', N'1311', N'TT200', CAST(1 AS bit), CAST(1 AS bit), N'Receivable'),
('28c9d98c-13a5-310d-7d9e-8d2388cd22a0', N'1121', N'TT200', CAST(1 AS bit), CAST(1 AS bit), N'Bank'),
('2cdf8041-0a2b-e65a-5f3c-593773b875d0', N'1111', N'TT200', CAST(1 AS bit), CAST(1 AS bit), N'Cash'),
('366959af-6b03-5589-8f76-ec419b0d24ea', N'642', N'TT200', CAST(1 AS bit), CAST(1 AS bit), N'Expense'),
('5beefc39-6f8e-ed2e-5e17-d264b80288ac', N'3331', N'TT200', CAST(1 AS bit), CAST(1 AS bit), N'VATOutput'),
('6282a244-7706-9065-57a3-58040ccad70d', N'5111', N'TT99', CAST(1 AS bit), CAST(1 AS bit), N'Revenue'),
('6e3f936d-006d-0a10-e4bf-548f6b0bb914', N'1121', N'TT99', CAST(1 AS bit), CAST(1 AS bit), N'Bank'),
('7d20c5e4-5f3d-15be-6873-11cdb10e09b2', N'214', N'TT99', CAST(1 AS bit), CAST(1 AS bit), N'Depreciation'),
('92b1d03e-a22f-907d-9307-84270006ef9a', N'153', N'TT200', CAST(1 AS bit), CAST(1 AS bit), N'ToolAndSupply'),
('9a72737d-3b3f-e6d0-172e-ff048181a97a', N'1331', N'TT200', CAST(1 AS bit), CAST(1 AS bit), N'VATInput'),
('c0065adc-1d73-5356-d8f3-caf5a5f9d9ce', N'6321', N'TT200', CAST(1 AS bit), CAST(1 AS bit), N'COGS'),
('c07f23bf-aa79-ae63-9fca-6ca14eeb1143', N'211', N'TT99', CAST(1 AS bit), CAST(1 AS bit), N'FixedAsset'),
('c45d119c-1c87-9a46-9403-fa182f974f68', N'3311', N'TT200', CAST(1 AS bit), CAST(1 AS bit), N'Payable'),
('c7364a40-5c3e-1ce1-fb65-ab1000638819', N'3331', N'TT99', CAST(1 AS bit), CAST(1 AS bit), N'VATOutput'),
('d9361f9e-d7de-45e1-3e81-175bb0b335a7', N'211', N'TT200', CAST(1 AS bit), CAST(1 AS bit), N'FixedAsset'),
('ddc5cd68-8e22-8dda-b942-4496b1aec26d', N'642', N'TT99', CAST(1 AS bit), CAST(1 AS bit), N'Expense'),
('e71d40d6-6853-ba29-93c1-703f06b8d231', N'214', N'TT200', CAST(1 AS bit), CAST(1 AS bit), N'Depreciation'),
('f4b2d931-85a8-8d06-4cf3-c52ab5d6f126', N'153', N'TT99', CAST(1 AS bit), CAST(1 AS bit), N'ToolAndSupply'),
('f954661f-f4fb-dd00-5184-49748e7db9e0', N'1561', N'TT200', CAST(1 AS bit), CAST(1 AS bit), N'Inventory');
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'AccountCode', N'AccountingStandardCode', N'IsActive', N'IsDefault', N'RoleCode') AND [object_id] = OBJECT_ID(N'[AccountRoleMappings]'))
    SET IDENTITY_INSERT [AccountRoleMappings] OFF;
GO

IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'AccountCategory', N'AccountCode', N'AccountKind', N'AccountLevel', N'AccountName', N'AllowPosting', N'IsActive', N'IsFinancialAccount', N'IsManagementAccount', N'IsSystemAccount', N'NormalBalance', N'ParentCode', N'RequireCurrency', N'RequireCustomer', N'RequireDepartment', N'RequireEmployee', N'RequireItem', N'RequireSupplier', N'RequireWarehouse', N'SortOrder', N'StandardCode') AND [object_id] = OBJECT_ID(N'[AccountTemplates]'))
    SET IDENTITY_INSERT [AccountTemplates] ON;
INSERT INTO [AccountTemplates] ([Id], [AccountCategory], [AccountCode], [AccountKind], [AccountLevel], [AccountName], [AllowPosting], [IsActive], [IsFinancialAccount], [IsManagementAccount], [IsSystemAccount], [NormalBalance], [ParentCode], [RequireCurrency], [RequireCustomer], [RequireDepartment], [RequireEmployee], [RequireItem], [RequireSupplier], [RequireWarehouse], [SortOrder], [StandardCode])
VALUES ('025508de-311e-2366-3a4b-25f1c05fc28e', N'Asset', N'152', N'D', 1, N'Nguyên liệu, vật liệu', CAST(1 AS bit), CAST(1 AS bit), CAST(1 AS bit), CAST(0 AS bit), CAST(1 AS bit), N'Debit', NULL, CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), CAST(0 AS bit), CAST(1 AS bit), 90, N'TT99'),
('04ea1f08-03ec-5803-8e41-51dca9a3b1f4', N'Asset', N'156', N'H', 1, N'Hàng hóa', CAST(0 AS bit), CAST(1 AS bit), CAST(1 AS bit), CAST(0 AS bit), CAST(1 AS bit), N'Debit', NULL, CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), 110, N'TT200'),
('088e7865-cfc9-6b08-6e30-3ebfed221bf5', N'Asset', N'131', N'H', 1, N'Phải thu khách hàng', CAST(0 AS bit), CAST(1 AS bit), CAST(1 AS bit), CAST(0 AS bit), CAST(1 AS bit), N'Debit', NULL, CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), 50, N'TT200'),
('0c852960-1799-22cb-3d10-70cc91932d65', N'Asset', N'133', N'H', 1, N'Thuế GTGT được khấu trừ', CAST(0 AS bit), CAST(1 AS bit), CAST(1 AS bit), CAST(0 AS bit), CAST(1 AS bit), N'Debit', NULL, CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), 70, N'TT200'),
('10140228-000b-a5dc-2802-fbf40e5d0c0d', N'Revenue', N'511', N'H', 1, N'Doanh thu bán hàng và cung cấp dịch vụ', CAST(0 AS bit), CAST(1 AS bit), CAST(1 AS bit), CAST(0 AS bit), CAST(1 AS bit), N'Credit', NULL, CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), 240, N'TT200'),
('17f30f88-b727-097f-4908-7a6296c3350c', N'Revenue', N'711', N'D', 1, N'Thu nhập khác', CAST(1 AS bit), CAST(1 AS bit), CAST(1 AS bit), CAST(0 AS bit), CAST(1 AS bit), N'Credit', NULL, CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), 300, N'TT99'),
('183ed488-26ff-532a-fd15-5501096a5cdd', N'Liability', N'338', N'D', 1, N'Phải trả, phải nộp khác', CAST(1 AS bit), CAST(1 AS bit), CAST(1 AS bit), CAST(0 AS bit), CAST(1 AS bit), N'Credit', NULL, CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), 210, N'TT99'),
('19afa75d-ee3f-ce4f-b271-c15a154e6444', N'Asset', N'153', N'D', 1, N'Công cụ, dụng cụ', CAST(1 AS bit), CAST(1 AS bit), CAST(1 AS bit), CAST(0 AS bit), CAST(1 AS bit), N'Debit', NULL, CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), CAST(0 AS bit), CAST(1 AS bit), 100, N'TT200'),
('1fc890e2-bb7c-d661-57b1-2b26ad18a933', N'Liability', N'3331', N'D', 2, N'Thuế GTGT đầu ra', CAST(1 AS bit), CAST(1 AS bit), CAST(1 AS bit), CAST(0 AS bit), CAST(1 AS bit), N'Credit', N'333', CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), 190, N'TT99'),
('24601f3e-1e11-dca9-d483-d54643246bff', N'Asset', N'242', N'D', 1, N'Chi phí trả trước', CAST(1 AS bit), CAST(1 AS bit), CAST(1 AS bit), CAST(0 AS bit), CAST(1 AS bit), N'Debit', NULL, CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), 150, N'TT200'),
('284fd7c8-adb3-f313-51ec-db9f468f2a88', N'Liability', N'331', N'H', 1, N'Phải trả nhà cung cấp', CAST(0 AS bit), CAST(1 AS bit), CAST(1 AS bit), CAST(0 AS bit), CAST(1 AS bit), N'Credit', NULL, CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), 160, N'TT200'),
('29700cc1-b105-96bc-e8a0-4414b23b5d51', N'Asset', N'111', N'H', 1, N'Tiền mặt', CAST(0 AS bit), CAST(1 AS bit), CAST(1 AS bit), CAST(0 AS bit), CAST(1 AS bit), N'Debit', NULL, CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), 10, N'TT200'),
('2a067d52-22a5-b564-08b7-85235979a252', N'Expense', N'642', N'D', 1, N'Chi phí quản lý doanh nghiệp', CAST(1 AS bit), CAST(1 AS bit), CAST(1 AS bit), CAST(0 AS bit), CAST(1 AS bit), N'Debit', NULL, CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), 290, N'TT99'),
('2bf2e261-d9b9-3232-6df7-2d08c82e3c5d', N'Asset', N'1561', N'D', 2, N'Giá mua hàng hóa', CAST(1 AS bit), CAST(1 AS bit), CAST(1 AS bit), CAST(0 AS bit), CAST(1 AS bit), N'Debit', N'156', CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), CAST(0 AS bit), CAST(1 AS bit), 120, N'TT99'),
('2e258141-702b-2748-2a09-89fb0d3f4926', N'Asset', N'1331', N'D', 2, N'Thuế GTGT đầu vào', CAST(1 AS bit), CAST(1 AS bit), CAST(1 AS bit), CAST(0 AS bit), CAST(1 AS bit), N'Debit', N'133', CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), 80, N'TT200'),
('4b856cd9-274b-2340-cac8-97e0c5f9075b', N'Asset', N'1311', N'D', 2, N'Phải thu khách hàng chi tiết', CAST(1 AS bit), CAST(1 AS bit), CAST(1 AS bit), CAST(0 AS bit), CAST(1 AS bit), N'Debit', N'131', CAST(0 AS bit), CAST(1 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), 60, N'TT200'),
('56b93dbc-f7fb-2234-5f71-5a1935e1be78', N'Asset', N'1111', N'D', 2, N'Tiền Việt Nam', CAST(1 AS bit), CAST(1 AS bit), CAST(1 AS bit), CAST(0 AS bit), CAST(1 AS bit), N'Debit', N'111', CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), 20, N'TT99'),
('57ff43a6-2100-104c-db83-3a9d510dfca2', N'Asset', N'1311', N'D', 2, N'Phải thu khách hàng chi tiết', CAST(1 AS bit), CAST(1 AS bit), CAST(1 AS bit), CAST(0 AS bit), CAST(1 AS bit), N'Debit', N'131', CAST(0 AS bit), CAST(1 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), 60, N'TT99'),
('598eb0f9-2daf-5723-0843-2656bd6bb8cd', N'Expense', N'811', N'D', 1, N'Chi phí khác', CAST(1 AS bit), CAST(1 AS bit), CAST(1 AS bit), CAST(0 AS bit), CAST(1 AS bit), N'Debit', NULL, CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), 310, N'TT99'),
('5ba977fb-f630-4c2a-1f95-6e50b1e81b6f', N'Liability', N'334', N'D', 1, N'Phải trả người lao động', CAST(1 AS bit), CAST(1 AS bit), CAST(1 AS bit), CAST(0 AS bit), CAST(1 AS bit), N'Credit', NULL, CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), 200, N'TT200'),
('5f658316-534b-1dce-b9fa-30ae0b693d39', N'Asset', N'1561', N'D', 2, N'Giá mua hàng hóa', CAST(1 AS bit), CAST(1 AS bit), CAST(1 AS bit), CAST(0 AS bit), CAST(1 AS bit), N'Debit', N'156', CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), CAST(0 AS bit), CAST(1 AS bit), 120, N'TT200'),
('60edee36-2f10-e923-c4d4-220fa9fe1162', N'Asset', N'133', N'H', 1, N'Thuế GTGT được khấu trừ', CAST(0 AS bit), CAST(1 AS bit), CAST(1 AS bit), CAST(0 AS bit), CAST(1 AS bit), N'Debit', NULL, CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), 70, N'TT99'),
('6428a67a-c730-f210-9fb7-4e114fe73a56', N'Revenue', N'711', N'D', 1, N'Thu nhập khác', CAST(1 AS bit), CAST(1 AS bit), CAST(1 AS bit), CAST(0 AS bit), CAST(1 AS bit), N'Credit', NULL, CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), 300, N'TT200'),
('64b98cbc-dd18-1b65-4d64-b7ba3fe13206', N'Asset', N'211', N'D', 1, N'Tài sản cố định hữu hình', CAST(1 AS bit), CAST(1 AS bit), CAST(1 AS bit), CAST(0 AS bit), CAST(1 AS bit), N'Debit', NULL, CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), 130, N'TT200'),
('6745719e-2582-f8db-f594-4207102cfd68', N'Expense', N'642', N'D', 1, N'Chi phí quản lý doanh nghiệp', CAST(1 AS bit), CAST(1 AS bit), CAST(1 AS bit), CAST(0 AS bit), CAST(1 AS bit), N'Debit', NULL, CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), 290, N'TT200'),
('6a62361b-dfaa-003a-3797-eccd1967efa9', N'Asset', N'211', N'D', 1, N'Tài sản cố định hữu hình', CAST(1 AS bit), CAST(1 AS bit), CAST(1 AS bit), CAST(0 AS bit), CAST(1 AS bit), N'Debit', NULL, CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), 130, N'TT99'),
('6cf34201-ead0-6ea9-d155-836076276cc7', N'Asset', N'242', N'D', 1, N'Chi phí trả trước', CAST(1 AS bit), CAST(1 AS bit), CAST(1 AS bit), CAST(0 AS bit), CAST(1 AS bit), N'Debit', NULL, CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), 150, N'TT99'),
('7379f7a5-657d-030a-5caa-fa6f03a7248b', N'Other', N'911', N'D', 1, N'Xác định kết quả kinh doanh', CAST(1 AS bit), CAST(1 AS bit), CAST(0 AS bit), CAST(1 AS bit), CAST(1 AS bit), N'Both', NULL, CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), 320, N'TT99'),
('76b26f4a-6113-e4b2-fe6b-2d39ac30c0d7', N'Liability', N'333', N'H', 1, N'Thuế và các khoản phải nộp Nhà nước', CAST(0 AS bit), CAST(1 AS bit), CAST(1 AS bit), CAST(0 AS bit), CAST(1 AS bit), N'Credit', NULL, CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), 180, N'TT99'),
('77287d40-a16b-c7f2-f1c7-fedef57a5466', N'Revenue', N'5111', N'D', 2, N'Doanh thu bán hàng hóa', CAST(1 AS bit), CAST(1 AS bit), CAST(1 AS bit), CAST(0 AS bit), CAST(1 AS bit), N'Credit', N'511', CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), 250, N'TT200'),
('7b4d454d-3050-993d-d17c-5043e915a16a', N'Expense', N'811', N'D', 1, N'Chi phí khác', CAST(1 AS bit), CAST(1 AS bit), CAST(1 AS bit), CAST(0 AS bit), CAST(1 AS bit), N'Debit', NULL, CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), 310, N'TT200'),
('81ca3594-4e8b-47e8-ac11-bd46a2dedbf4', N'Expense', N'6321', N'D', 2, N'Giá vốn hàng hóa', CAST(1 AS bit), CAST(1 AS bit), CAST(1 AS bit), CAST(0 AS bit), CAST(1 AS bit), N'Debit', N'632', CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), 270, N'TT99'),
('84a75d6d-83df-a21e-e7b2-8f4b78e85267', N'Equity', N'411', N'D', 1, N'Vốn góp của chủ sở hữu', CAST(1 AS bit), CAST(1 AS bit), CAST(1 AS bit), CAST(0 AS bit), CAST(1 AS bit), N'Credit', NULL, CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), 220, N'TT200'),
('8688b1f2-28b3-6ad0-75ec-eb3abd093655', N'Liability', N'3331', N'D', 2, N'Thuế GTGT đầu ra', CAST(1 AS bit), CAST(1 AS bit), CAST(1 AS bit), CAST(0 AS bit), CAST(1 AS bit), N'Credit', N'333', CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), 190, N'TT200'),
('8a6562f9-2370-e388-3960-c8fc039c226b', N'Asset', N'214', N'D', 1, N'Hao mòn tài sản cố định', CAST(1 AS bit), CAST(1 AS bit), CAST(1 AS bit), CAST(0 AS bit), CAST(1 AS bit), N'Credit', NULL, CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), 140, N'TT200'),
('8d0ca8f7-a446-5995-5ae8-1292372bd232', N'Revenue', N'511', N'H', 1, N'Doanh thu bán hàng và cung cấp dịch vụ', CAST(0 AS bit), CAST(1 AS bit), CAST(1 AS bit), CAST(0 AS bit), CAST(1 AS bit), N'Credit', NULL, CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), 240, N'TT99'),
('8d486cd7-a2be-3ed6-c2ec-9899f035fc8a', N'Asset', N'156', N'H', 1, N'Hàng hóa', CAST(0 AS bit), CAST(1 AS bit), CAST(1 AS bit), CAST(0 AS bit), CAST(1 AS bit), N'Debit', NULL, CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), 110, N'TT99'),
('928bb1a2-ad74-159e-5f32-46f41acc2c82', N'Liability', N'3311', N'D', 2, N'Phải trả nhà cung cấp chi tiết', CAST(1 AS bit), CAST(1 AS bit), CAST(1 AS bit), CAST(0 AS bit), CAST(1 AS bit), N'Credit', N'331', CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), CAST(0 AS bit), 170, N'TT99'),
('98355a78-7e61-9d0b-8d01-aec60bbd27c1', N'Equity', N'411', N'D', 1, N'Vốn góp của chủ sở hữu', CAST(1 AS bit), CAST(1 AS bit), CAST(1 AS bit), CAST(0 AS bit), CAST(1 AS bit), N'Credit', NULL, CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), 220, N'TT99'),
('988a0a84-a6be-7e48-898f-672de0e7e3fe', N'Other', N'911', N'D', 1, N'Xác định kết quả kinh doanh', CAST(1 AS bit), CAST(1 AS bit), CAST(0 AS bit), CAST(1 AS bit), CAST(1 AS bit), N'Both', NULL, CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), 320, N'TT200'),
('9ba9a426-8856-7471-49fb-0fa286c0f46e', N'Asset', N'214', N'D', 1, N'Hao mòn tài sản cố định', CAST(1 AS bit), CAST(1 AS bit), CAST(1 AS bit), CAST(0 AS bit), CAST(1 AS bit), N'Credit', NULL, CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), 140, N'TT99'),
('9c7d8add-aa2e-66ea-23f4-0e38322f0e22', N'Expense', N'632', N'H', 1, N'Giá vốn hàng bán', CAST(0 AS bit), CAST(1 AS bit), CAST(1 AS bit), CAST(0 AS bit), CAST(1 AS bit), N'Debit', NULL, CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), 260, N'TT99');
INSERT INTO [AccountTemplates] ([Id], [AccountCategory], [AccountCode], [AccountKind], [AccountLevel], [AccountName], [AllowPosting], [IsActive], [IsFinancialAccount], [IsManagementAccount], [IsSystemAccount], [NormalBalance], [ParentCode], [RequireCurrency], [RequireCustomer], [RequireDepartment], [RequireEmployee], [RequireItem], [RequireSupplier], [RequireWarehouse], [SortOrder], [StandardCode])
VALUES ('9fd87791-3bc2-908b-0f62-8ea4430360a9', N'Expense', N'641', N'D', 1, N'Chi phí bán hàng', CAST(1 AS bit), CAST(1 AS bit), CAST(1 AS bit), CAST(0 AS bit), CAST(1 AS bit), N'Debit', NULL, CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), 280, N'TT200'),
('aa953e33-968a-411a-4607-0dc2dbc08977', N'Liability', N'3311', N'D', 2, N'Phải trả nhà cung cấp chi tiết', CAST(1 AS bit), CAST(1 AS bit), CAST(1 AS bit), CAST(0 AS bit), CAST(1 AS bit), N'Credit', N'331', CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), CAST(0 AS bit), 170, N'TT200'),
('ad132247-9cdc-40f3-cc4d-0ff60395df41', N'Asset', N'1331', N'D', 2, N'Thuế GTGT đầu vào', CAST(1 AS bit), CAST(1 AS bit), CAST(1 AS bit), CAST(0 AS bit), CAST(1 AS bit), N'Debit', N'133', CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), 80, N'TT99'),
('b23752de-4c21-28f5-dcd4-a1dea936ee5b', N'Asset', N'153', N'D', 1, N'Công cụ, dụng cụ', CAST(1 AS bit), CAST(1 AS bit), CAST(1 AS bit), CAST(0 AS bit), CAST(1 AS bit), N'Debit', NULL, CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), CAST(0 AS bit), CAST(1 AS bit), 100, N'TT99'),
('b4c397e6-0b94-e22c-9278-b72a7da3cd45', N'Asset', N'1121', N'D', 2, N'Tiền Việt Nam gửi ngân hàng', CAST(1 AS bit), CAST(1 AS bit), CAST(1 AS bit), CAST(0 AS bit), CAST(1 AS bit), N'Debit', N'112', CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), 40, N'TT200'),
('bacde5d0-d1ed-9aa5-b351-805e5ae8ef59', N'Expense', N'641', N'D', 1, N'Chi phí bán hàng', CAST(1 AS bit), CAST(1 AS bit), CAST(1 AS bit), CAST(0 AS bit), CAST(1 AS bit), N'Debit', NULL, CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), 280, N'TT99'),
('bcfb23b9-32ed-f6b2-27a6-9dfae33faa4d', N'Asset', N'1111', N'D', 2, N'Tiền Việt Nam', CAST(1 AS bit), CAST(1 AS bit), CAST(1 AS bit), CAST(0 AS bit), CAST(1 AS bit), N'Debit', N'111', CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), 20, N'TT200'),
('bd7fb5ae-ccdf-2936-9967-ebe55ac6e1e1', N'Expense', N'632', N'H', 1, N'Giá vốn hàng bán', CAST(0 AS bit), CAST(1 AS bit), CAST(1 AS bit), CAST(0 AS bit), CAST(1 AS bit), N'Debit', NULL, CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), 260, N'TT200'),
('c065d0fa-023d-b16a-4fb0-5271e5148f4a', N'Revenue', N'5111', N'D', 2, N'Doanh thu bán hàng hóa', CAST(1 AS bit), CAST(1 AS bit), CAST(1 AS bit), CAST(0 AS bit), CAST(1 AS bit), N'Credit', N'511', CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), 250, N'TT99'),
('c200f46d-d7f5-1ecb-a0cb-c93a3098e592', N'Asset', N'112', N'H', 1, N'Tiền gửi ngân hàng', CAST(0 AS bit), CAST(1 AS bit), CAST(1 AS bit), CAST(0 AS bit), CAST(1 AS bit), N'Debit', NULL, CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), 30, N'TT200'),
('c5891608-7f1f-7f34-0009-362fb8b789a4', N'Liability', N'331', N'H', 1, N'Phải trả nhà cung cấp', CAST(0 AS bit), CAST(1 AS bit), CAST(1 AS bit), CAST(0 AS bit), CAST(1 AS bit), N'Credit', NULL, CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), 160, N'TT99'),
('c6305502-1af6-c027-e1ee-4279f9d38ae4', N'Asset', N'152', N'D', 1, N'Nguyên liệu, vật liệu', CAST(1 AS bit), CAST(1 AS bit), CAST(1 AS bit), CAST(0 AS bit), CAST(1 AS bit), N'Debit', NULL, CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(1 AS bit), CAST(0 AS bit), CAST(1 AS bit), 90, N'TT200'),
('d0705825-f9fc-435f-5ae9-ed5f6bc34299', N'Asset', N'112', N'H', 1, N'Tiền gửi ngân hàng', CAST(0 AS bit), CAST(1 AS bit), CAST(1 AS bit), CAST(0 AS bit), CAST(1 AS bit), N'Debit', NULL, CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), 30, N'TT99'),
('d46ab24e-4675-33bd-a668-0ed75a7d769b', N'Equity', N'421', N'D', 1, N'Lợi nhuận sau thuế chưa phân phối', CAST(1 AS bit), CAST(1 AS bit), CAST(1 AS bit), CAST(0 AS bit), CAST(1 AS bit), N'Credit', NULL, CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), 230, N'TT99'),
('d559b674-eb66-7f73-688b-40c9c9f19576', N'Asset', N'1121', N'D', 2, N'Tiền Việt Nam gửi ngân hàng', CAST(1 AS bit), CAST(1 AS bit), CAST(1 AS bit), CAST(0 AS bit), CAST(1 AS bit), N'Debit', N'112', CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), 40, N'TT99'),
('d8b6a044-5ac0-7b7b-9c1d-b7a57b57def5', N'Expense', N'6321', N'D', 2, N'Giá vốn hàng hóa', CAST(1 AS bit), CAST(1 AS bit), CAST(1 AS bit), CAST(0 AS bit), CAST(1 AS bit), N'Debit', N'632', CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), 270, N'TT200'),
('dac90964-ec7f-11c1-a01e-ba10703bb21a', N'Liability', N'334', N'D', 1, N'Phải trả người lao động', CAST(1 AS bit), CAST(1 AS bit), CAST(1 AS bit), CAST(0 AS bit), CAST(1 AS bit), N'Credit', NULL, CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), 200, N'TT99'),
('dcca2214-0d4b-dc61-4823-acad129f8c9a', N'Equity', N'421', N'D', 1, N'Lợi nhuận sau thuế chưa phân phối', CAST(1 AS bit), CAST(1 AS bit), CAST(1 AS bit), CAST(0 AS bit), CAST(1 AS bit), N'Credit', NULL, CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), 230, N'TT200'),
('ea2d937e-3992-2636-729c-8694b562d023', N'Asset', N'111', N'H', 1, N'Tiền mặt', CAST(0 AS bit), CAST(1 AS bit), CAST(1 AS bit), CAST(0 AS bit), CAST(1 AS bit), N'Debit', NULL, CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), 10, N'TT99'),
('ebb3ec86-3f85-d42a-d7dd-0afadbb5a708', N'Liability', N'333', N'H', 1, N'Thuế và các khoản phải nộp Nhà nước', CAST(0 AS bit), CAST(1 AS bit), CAST(1 AS bit), CAST(0 AS bit), CAST(1 AS bit), N'Credit', NULL, CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), 180, N'TT200'),
('f0edc9bc-c303-4758-d9e5-ecd06ddbf5b8', N'Liability', N'338', N'D', 1, N'Phải trả, phải nộp khác', CAST(1 AS bit), CAST(1 AS bit), CAST(1 AS bit), CAST(0 AS bit), CAST(1 AS bit), N'Credit', NULL, CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), 210, N'TT200'),
('f6acf7bf-51a1-2a36-2eb3-d7e83b1fa71d', N'Asset', N'131', N'H', 1, N'Phải thu khách hàng', CAST(0 AS bit), CAST(1 AS bit), CAST(1 AS bit), CAST(0 AS bit), CAST(1 AS bit), N'Debit', NULL, CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), CAST(0 AS bit), 50, N'TT99');
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'AccountCategory', N'AccountCode', N'AccountKind', N'AccountLevel', N'AccountName', N'AllowPosting', N'IsActive', N'IsFinancialAccount', N'IsManagementAccount', N'IsSystemAccount', N'NormalBalance', N'ParentCode', N'RequireCurrency', N'RequireCustomer', N'RequireDepartment', N'RequireEmployee', N'RequireItem', N'RequireSupplier', N'RequireWarehouse', N'SortOrder', N'StandardCode') AND [object_id] = OBJECT_ID(N'[AccountTemplates]'))
    SET IDENTITY_INSERT [AccountTemplates] OFF;
GO

IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'CreatedAt', N'DefinitionJson', N'IsActive', N'IsSystem', N'Name', N'StandardCode', N'TemplateCode', N'TemplateType', N'UpdatedAt') AND [object_id] = OBJECT_ID(N'[AccountingStandardTemplates]'))
    SET IDENTITY_INSERT [AccountingStandardTemplates] ON;
INSERT INTO [AccountingStandardTemplates] ([Id], [CreatedAt], [DefinitionJson], [IsActive], [IsSystem], [Name], [StandardCode], [TemplateCode], [TemplateType], [UpdatedAt])
VALUES ('1386fead-9944-45b8-7d8e-7cf4f84ebacf', '2026-05-21T00:00:00.0000000Z', N'{"standardCode":"CUSTOM","type":"Account","version":1}', CAST(1 AS bit), CAST(1 AS bit), N'Mẫu hệ thống tài khoản', N'CUSTOM', N'CUSTOM_ACCOUNTS', N'Account', '2026-05-21T00:00:00.0000000Z'),
('26c6b1bc-febb-8b6a-f14c-7eb8884c19fc', '2026-05-21T00:00:00.0000000Z', N'{"standardCode":"TT200","type":"ReportMapping","version":1}', CAST(1 AS bit), CAST(1 AS bit), N'Mẫu mapping báo cáo tài chính', N'TT200', N'TT200_REPORT_MAPPING', N'ReportMapping', '2026-05-21T00:00:00.0000000Z'),
('2a77e81e-3caa-9dbf-d890-172559873b7d', '2026-05-21T00:00:00.0000000Z', N'{"standardCode":"CUSTOM","type":"ReportMapping","version":1}', CAST(1 AS bit), CAST(1 AS bit), N'Mẫu mapping báo cáo tài chính', N'CUSTOM', N'CUSTOM_REPORT_MAPPING', N'ReportMapping', '2026-05-21T00:00:00.0000000Z'),
('2d505991-250c-5966-7f53-a8abc157cd31', '2026-05-21T00:00:00.0000000Z', N'{"standardCode":"TT99","type":"Account","version":1}', CAST(1 AS bit), CAST(1 AS bit), N'Mẫu hệ thống tài khoản', N'TT99', N'TT99_ACCOUNTS', N'Account', '2026-05-21T00:00:00.0000000Z'),
('33f6661c-28e8-3228-9d2b-16940bbd7df0', '2026-05-21T00:00:00.0000000Z', N'{"standardCode":"TT200","type":"PostingRule","version":1}', CAST(1 AS bit), CAST(1 AS bit), N'Mẫu quy tắc hạch toán', N'TT200', N'TT200_POSTING_RULE', N'PostingRule', '2026-05-21T00:00:00.0000000Z'),
('661336a6-bfd3-3945-7e79-172502a03112', '2026-05-21T00:00:00.0000000Z', N'{"standardCode":"TT99","type":"PostingRule","version":1}', CAST(1 AS bit), CAST(1 AS bit), N'Mẫu quy tắc hạch toán', N'TT99', N'TT99_POSTING_RULE', N'PostingRule', '2026-05-21T00:00:00.0000000Z'),
('8225f128-462f-00f1-e610-101995fcae16', '2026-05-21T00:00:00.0000000Z', N'{"standardCode":"TT200","type":"Account","version":1}', CAST(1 AS bit), CAST(1 AS bit), N'Mẫu hệ thống tài khoản', N'TT200', N'TT200_ACCOUNTS', N'Account', '2026-05-21T00:00:00.0000000Z'),
('9c9a7c37-1237-1a8d-9998-f6107adf494a', '2026-05-21T00:00:00.0000000Z', N'{"standardCode":"TT99","type":"RoleMapping","version":1}', CAST(1 AS bit), CAST(1 AS bit), N'Mẫu mapping vai trò tài khoản', N'TT99', N'TT99_ROLE_MAPPING', N'RoleMapping', '2026-05-21T00:00:00.0000000Z'),
('d45c163e-bdcc-bdae-fc81-9d63cbed6fe8', '2026-05-21T00:00:00.0000000Z', N'{"standardCode":"TT99","type":"ReportMapping","version":1}', CAST(1 AS bit), CAST(1 AS bit), N'Mẫu mapping báo cáo tài chính', N'TT99', N'TT99_REPORT_MAPPING', N'ReportMapping', '2026-05-21T00:00:00.0000000Z'),
('dcb9b376-d9fc-eabb-a825-87e097c541d7', '2026-05-21T00:00:00.0000000Z', N'{"standardCode":"CUSTOM","type":"PostingRule","version":1}', CAST(1 AS bit), CAST(1 AS bit), N'Mẫu quy tắc hạch toán', N'CUSTOM', N'CUSTOM_POSTING_RULE', N'PostingRule', '2026-05-21T00:00:00.0000000Z'),
('e1b2b3d5-45b5-bb13-2930-668ac84fe4f0', '2026-05-21T00:00:00.0000000Z', N'{"standardCode":"TT200","type":"RoleMapping","version":1}', CAST(1 AS bit), CAST(1 AS bit), N'Mẫu mapping vai trò tài khoản', N'TT200', N'TT200_ROLE_MAPPING', N'RoleMapping', '2026-05-21T00:00:00.0000000Z'),
('f0d3245d-7789-1301-58e4-98a3ec36f4e8', '2026-05-21T00:00:00.0000000Z', N'{"standardCode":"CUSTOM","type":"RoleMapping","version":1}', CAST(1 AS bit), CAST(1 AS bit), N'Mẫu mapping vai trò tài khoản', N'CUSTOM', N'CUSTOM_ROLE_MAPPING', N'RoleMapping', '2026-05-21T00:00:00.0000000Z');
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'CreatedAt', N'DefinitionJson', N'IsActive', N'IsSystem', N'Name', N'StandardCode', N'TemplateCode', N'TemplateType', N'UpdatedAt') AND [object_id] = OBJECT_ID(N'[AccountingStandardTemplates]'))
    SET IDENTITY_INSERT [AccountingStandardTemplates] OFF;
GO

IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'AccountingStandardCode', N'CompanyId', N'CompanyName', N'CreatedAt', N'EffectiveFrom', N'FiscalYearStartDay', N'FiscalYearStartMonth', N'IsActive', N'UpdatedAt') AND [object_id] = OBJECT_ID(N'[CompanyAccountingProfiles]'))
    SET IDENTITY_INSERT [CompanyAccountingProfiles] ON;
INSERT INTO [CompanyAccountingProfiles] ([Id], [AccountingStandardCode], [CompanyId], [CompanyName], [CreatedAt], [EffectiveFrom], [FiscalYearStartDay], [FiscalYearStartMonth], [IsActive], [UpdatedAt])
VALUES ('9b2c65a2-1a31-4b6b-924c-000000000001', N'TT99', NULL, N'Default Company', '2026-05-21T00:00:00.0000000Z', '2026-01-01T00:00:00.0000000Z', 1, 1, CAST(1 AS bit), '2026-05-21T00:00:00.0000000Z');
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'AccountingStandardCode', N'CompanyId', N'CompanyName', N'CreatedAt', N'EffectiveFrom', N'FiscalYearStartDay', N'FiscalYearStartMonth', N'IsActive', N'UpdatedAt') AND [object_id] = OBJECT_ID(N'[CompanyAccountingProfiles]'))
    SET IDENTITY_INSERT [CompanyAccountingProfiles] OFF;
GO

IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'AccountFilter', N'AccountRoleFilter', N'Formula', N'IsActive', N'LineCode', N'LineName', N'ReportCode', N'SortOrder', N'StandardCode') AND [object_id] = OBJECT_ID(N'[FinancialStatementMappings]'))
    SET IDENTITY_INSERT [FinancialStatementMappings] ON;
INSERT INTO [FinancialStatementMappings] ([Id], [AccountFilter], [AccountRoleFilter], [Formula], [IsActive], [LineCode], [LineName], [ReportCode], [SortOrder], [StandardCode])
VALUES ('0182d2aa-0a17-791a-cb65-a65e5effab7a', NULL, NULL, N'GROSS_PROFIT - SELLING_EXPENSE - ADMIN_EXPENSE + OTHER_INCOME - OTHER_EXPENSE', CAST(1 AS bit), N'PROFIT_BEFORE_TAX', N'Lợi nhuận trước thuế', N'INCOME_STATEMENT', 80, N'TT99'),
('1009532d-729d-8d6e-4f9a-87f4bb281c74', N'111*,112*', N'Cash,Bank', NULL, CAST(1 AS bit), N'CASH_AND_CASH_EQUIVALENTS', N'Tiền và tương đương tiền', N'BALANCE_SHEET', 10, N'TT200'),
('15c78b04-6e79-d552-26a4-e73c1cf809dc', N'152*,153*,156*', N'Inventory,ToolAndSupply', NULL, CAST(1 AS bit), N'INVENTORY', N'Hàng tồn kho', N'BALANCE_SHEET', 30, N'TT200'),
('1838b8af-4691-772a-eca2-4397a2d490c1', NULL, NULL, N'REVENUE - COGS', CAST(1 AS bit), N'GROSS_PROFIT', N'Lợi nhuận gộp', N'INCOME_STATEMENT', 30, N'TT200'),
('21246897-c179-8a44-e21f-47b6ffdd43e5', N'711*', NULL, NULL, CAST(1 AS bit), N'OTHER_INCOME', N'Thu nhập khác', N'INCOME_STATEMENT', 60, N'TT99'),
('292432f7-5788-bec1-ce3f-cc814025844d', N'711*', NULL, NULL, CAST(1 AS bit), N'OTHER_INCOME', N'Thu nhập khác', N'INCOME_STATEMENT', 60, N'TT200'),
('37e6948c-6831-61c8-e055-7e3af262d316', N'811*', NULL, NULL, CAST(1 AS bit), N'OTHER_EXPENSE', N'Chi phí khác', N'INCOME_STATEMENT', 70, N'TT200'),
('47ed997b-5f57-b22d-5b54-8309c419f29a', N'152*,153*,156*', N'Inventory,ToolAndSupply', NULL, CAST(1 AS bit), N'INVENTORY', N'Hàng tồn kho', N'BALANCE_SHEET', 30, N'TT99'),
('5076eb2e-81f8-607a-5b50-5b8c1b736d2e', NULL, NULL, N'REVENUE - COGS', CAST(1 AS bit), N'GROSS_PROFIT', N'Lợi nhuận gộp', N'INCOME_STATEMENT', 30, N'TT99'),
('52fadcac-2053-01a8-eaf9-9ba6d77714b6', N'511*', N'Revenue', NULL, CAST(1 AS bit), N'REVENUE', N'Doanh thu', N'INCOME_STATEMENT', 10, N'TT99'),
('576f7773-3663-768b-d830-d8daf2730795', N'331*', N'Payable', NULL, CAST(1 AS bit), N'PAYABLES', N'Phải trả nhà cung cấp', N'BALANCE_SHEET', 50, N'TT99'),
('5f7d1620-a49e-71c0-971f-877e63950596', N'331*', N'Payable', NULL, CAST(1 AS bit), N'PAYABLES', N'Phải trả nhà cung cấp', N'BALANCE_SHEET', 50, N'TT200'),
('67a4801b-73cb-bd68-0f70-38712b9569d8', N'641*', NULL, NULL, CAST(1 AS bit), N'SELLING_EXPENSE', N'Chi phí bán hàng', N'INCOME_STATEMENT', 40, N'TT200'),
('681a72d7-516b-6fda-a9aa-faa59cc0bc4e', N'211*,214*', N'FixedAsset,Depreciation', NULL, CAST(1 AS bit), N'FIXED_ASSETS', N'Tài sản cố định', N'BALANCE_SHEET', 40, N'TT200'),
('87bee6eb-e9c5-98e0-e3c3-9580228dd031', N'811*', NULL, NULL, CAST(1 AS bit), N'OTHER_EXPENSE', N'Chi phí khác', N'INCOME_STATEMENT', 70, N'TT99'),
('8b9d58dd-6cac-a0a1-6911-c53349006327', N'333*', N'VATOutput', NULL, CAST(1 AS bit), N'TAX_PAYABLES', N'Thuế và các khoản phải nộp', N'BALANCE_SHEET', 60, N'TT99'),
('8e4e72ce-02d5-6fa7-0940-b8272443cc54', N'632*', N'COGS', NULL, CAST(1 AS bit), N'COGS', N'Giá vốn hàng bán', N'INCOME_STATEMENT', 20, N'TT200'),
('95543140-a7a5-97a2-aa3a-0fb8617e029e', N'511*', N'Revenue', NULL, CAST(1 AS bit), N'REVENUE', N'Doanh thu', N'INCOME_STATEMENT', 10, N'TT200'),
('964494b2-cedd-7c14-8d9e-5b6c74803097', N'411*,421*', NULL, NULL, CAST(1 AS bit), N'EQUITY', N'Vốn chủ sở hữu', N'BALANCE_SHEET', 70, N'TT200'),
('9bf102f0-400d-d6dd-61de-802b0559b105', N'642*', N'Expense', NULL, CAST(1 AS bit), N'ADMIN_EXPENSE', N'Chi phí quản lý doanh nghiệp', N'INCOME_STATEMENT', 50, N'TT99'),
('a8c61726-fe70-dfbf-a3ca-ea80cf36f2d4', N'211*,214*', N'FixedAsset,Depreciation', NULL, CAST(1 AS bit), N'FIXED_ASSETS', N'Tài sản cố định', N'BALANCE_SHEET', 40, N'TT99'),
('aed70647-f9d1-5a24-72f0-f9d955f00538', NULL, NULL, N'GROSS_PROFIT - SELLING_EXPENSE - ADMIN_EXPENSE + OTHER_INCOME - OTHER_EXPENSE', CAST(1 AS bit), N'PROFIT_BEFORE_TAX', N'Lợi nhuận trước thuế', N'INCOME_STATEMENT', 80, N'TT200'),
('b7188cbd-d96b-030b-ae96-22ead1485ce9', N'333*', N'VATOutput', NULL, CAST(1 AS bit), N'TAX_PAYABLES', N'Thuế và các khoản phải nộp', N'BALANCE_SHEET', 60, N'TT200'),
('c7f9d121-abca-c78a-23fd-441cd5af9d6e', N'642*', N'Expense', NULL, CAST(1 AS bit), N'ADMIN_EXPENSE', N'Chi phí quản lý doanh nghiệp', N'INCOME_STATEMENT', 50, N'TT200'),
('d16f401a-9c3e-60b9-5e93-8ec4b1f838ac', N'111*,112*', N'Cash,Bank', NULL, CAST(1 AS bit), N'CASH_AND_CASH_EQUIVALENTS', N'Tiền và tương đương tiền', N'BALANCE_SHEET', 10, N'TT99'),
('d3228ff9-4a20-e563-3fef-cd5472f0829f', N'632*', N'COGS', NULL, CAST(1 AS bit), N'COGS', N'Giá vốn hàng bán', N'INCOME_STATEMENT', 20, N'TT99'),
('d8f4f33e-6371-14fb-d0c2-1529ea42943a', N'131*', N'Receivable', NULL, CAST(1 AS bit), N'RECEIVABLES', N'Phải thu khách hàng', N'BALANCE_SHEET', 20, N'TT99'),
('dc32a6b1-e3d6-025d-c7da-e53e70974dd3', N'131*', N'Receivable', NULL, CAST(1 AS bit), N'RECEIVABLES', N'Phải thu khách hàng', N'BALANCE_SHEET', 20, N'TT200'),
('df4ca685-8a95-fa2e-bc7e-499eddc58e9b', N'411*,421*', NULL, NULL, CAST(1 AS bit), N'EQUITY', N'Vốn chủ sở hữu', N'BALANCE_SHEET', 70, N'TT99'),
('e9ee0684-7722-ab0b-637c-08f890529513', N'641*', NULL, NULL, CAST(1 AS bit), N'SELLING_EXPENSE', N'Chi phí bán hàng', N'INCOME_STATEMENT', 40, N'TT99');
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'AccountFilter', N'AccountRoleFilter', N'Formula', N'IsActive', N'LineCode', N'LineName', N'ReportCode', N'SortOrder', N'StandardCode') AND [object_id] = OBJECT_ID(N'[FinancialStatementMappings]'))
    SET IDENTITY_INSERT [FinancialStatementMappings] OFF;
GO

CREATE INDEX [IX_AccountingStandardTemplates_StandardCode] ON [AccountingStandardTemplates] ([StandardCode]);
GO

CREATE UNIQUE INDEX [IX_AccountingStandardTemplates_TemplateCode] ON [AccountingStandardTemplates] ([TemplateCode]);
GO

CREATE INDEX [IX_AccountingStandardTemplates_TemplateType] ON [AccountingStandardTemplates] ([TemplateType]);
GO

CREATE INDEX [IX_AccountTemplates_StandardCode] ON [AccountTemplates] ([StandardCode]);
GO

CREATE UNIQUE INDEX [IX_AccountTemplates_StandardCode_AccountCode] ON [AccountTemplates] ([StandardCode], [AccountCode]);
GO

CREATE INDEX [IX_CompanyAccountingProfiles_AccountingStandardCode] ON [CompanyAccountingProfiles] ([AccountingStandardCode]);
GO

CREATE INDEX [IX_CompanyAccountingProfiles_CompanyId] ON [CompanyAccountingProfiles] ([CompanyId]);
GO

CREATE INDEX [IX_FinancialStatementMappings_LineCode] ON [FinancialStatementMappings] ([LineCode]);
GO

CREATE INDEX [IX_FinancialStatementMappings_ReportCode] ON [FinancialStatementMappings] ([ReportCode]);
GO

CREATE INDEX [IX_FinancialStatementMappings_StandardCode] ON [FinancialStatementMappings] ([StandardCode]);
GO

CREATE UNIQUE INDEX [IX_FinancialStatementMappings_StandardCode_ReportCode_LineCode] ON [FinancialStatementMappings] ([StandardCode], [ReportCode], [LineCode]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260521031817_AddAccountingStandardEngine', N'8.0.7');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

CREATE TABLE [CashFunds] (
    [Id] uniqueidentifier NOT NULL,
    [FundCode] nvarchar(50) NOT NULL,
    [FundName] nvarchar(255) NOT NULL,
    [CurrencyCode] nvarchar(10) NOT NULL DEFAULT N'VND',
    [AccountCode] nvarchar(50) NULL,
    [WarehouseId] uniqueidentifier NULL,
    [DepartmentId] uniqueidentifier NULL,
    [ResponsiblePersonId] uniqueidentifier NULL,
    [OpeningBalance] decimal(18,2) NOT NULL,
    [CurrentBalance] decimal(18,2) NOT NULL,
    [IsDefault] bit NOT NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_CashFunds] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [CashCounts] (
    [Id] uniqueidentifier NOT NULL,
    [CashFundId] uniqueidentifier NOT NULL,
    [CountNo] nvarchar(50) NOT NULL,
    [CountDate] datetime2 NOT NULL,
    [BookBalance] decimal(18,2) NOT NULL,
    [ActualBalance] decimal(18,2) NOT NULL,
    [Difference] decimal(18,2) NOT NULL,
    [Status] nvarchar(30) NOT NULL,
    [Note] nvarchar(1000) NULL,
    [CreatedAt] datetime2 NOT NULL,
    [CreatedBy] nvarchar(255) NULL,
    [ConfirmedAt] datetime2 NULL,
    [ConfirmedBy] nvarchar(255) NULL,
    CONSTRAINT [PK_CashCounts] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_CashCounts_CashFunds_CashFundId] FOREIGN KEY ([CashFundId]) REFERENCES [CashFunds] ([Id]) ON DELETE NO ACTION
);
GO

CREATE TABLE [CashTransactions] (
    [Id] uniqueidentifier NOT NULL,
    [CashFundId] uniqueidentifier NOT NULL,
    [TransactionNo] nvarchar(50) NOT NULL,
    [TransactionDate] datetime2 NOT NULL,
    [TransactionType] nvarchar(30) NOT NULL,
    [SourceType] nvarchar(50) NULL,
    [SourceId] uniqueidentifier NULL,
    [JournalHeaderId] uniqueidentifier NULL,
    [PartnerType] nvarchar(30) NULL,
    [PartnerId] uniqueidentifier NULL,
    [CounterAccountCode] nvarchar(50) NULL,
    [Description] nvarchar(1000) NULL,
    [DebitAmount] decimal(18,2) NOT NULL,
    [CreditAmount] decimal(18,2) NOT NULL,
    [BalanceAfter] decimal(18,2) NOT NULL,
    [Status] nvarchar(30) NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [CreatedBy] nvarchar(255) NULL,
    [PostedAt] datetime2 NULL,
    [PostedBy] nvarchar(255) NULL,
    CONSTRAINT [PK_CashTransactions] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_CashTransactions_CashFunds_CashFundId] FOREIGN KEY ([CashFundId]) REFERENCES [CashFunds] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_CashTransactions_JournalHeaders_JournalHeaderId] FOREIGN KEY ([JournalHeaderId]) REFERENCES [JournalHeaders] ([Id])
);
GO

CREATE TABLE [CashCountLines] (
    [Id] uniqueidentifier NOT NULL,
    [CashCountId] uniqueidentifier NOT NULL,
    [Denomination] decimal(18,2) NOT NULL,
    [Quantity] int NOT NULL,
    [Amount] decimal(18,2) NOT NULL,
    CONSTRAINT [PK_CashCountLines] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_CashCountLines_CashCounts_CashCountId] FOREIGN KEY ([CashCountId]) REFERENCES [CashCounts] ([Id]) ON DELETE CASCADE
);
GO

IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'AccountCode', N'CreatedAt', N'CurrencyCode', N'CurrentBalance', N'DepartmentId', N'FundCode', N'FundName', N'IsActive', N'IsDefault', N'OpeningBalance', N'ResponsiblePersonId', N'UpdatedAt', N'WarehouseId') AND [object_id] = OBJECT_ID(N'[CashFunds]'))
    SET IDENTITY_INSERT [CashFunds] ON;
INSERT INTO [CashFunds] ([Id], [AccountCode], [CreatedAt], [CurrencyCode], [CurrentBalance], [DepartmentId], [FundCode], [FundName], [IsActive], [IsDefault], [OpeningBalance], [ResponsiblePersonId], [UpdatedAt], [WarehouseId])
VALUES ('7ac7ad91-6e6a-4b2c-9f9e-000000000001', N'1111', '2026-05-21T00:00:00.0000000Z', N'VND', 0.0, NULL, N'CASH_MAIN', N'Quỹ tiền mặt chính', CAST(1 AS bit), CAST(1 AS bit), 0.0, NULL, '2026-05-21T00:00:00.0000000Z', NULL);
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'AccountCode', N'CreatedAt', N'CurrencyCode', N'CurrentBalance', N'DepartmentId', N'FundCode', N'FundName', N'IsActive', N'IsDefault', N'OpeningBalance', N'ResponsiblePersonId', N'UpdatedAt', N'WarehouseId') AND [object_id] = OBJECT_ID(N'[CashFunds]'))
    SET IDENTITY_INSERT [CashFunds] OFF;
GO

CREATE INDEX [IX_CashCountLines_CashCountId] ON [CashCountLines] ([CashCountId]);
GO

CREATE INDEX [IX_CashCounts_CashFundId] ON [CashCounts] ([CashFundId]);
GO

CREATE INDEX [IX_CashCounts_CountDate] ON [CashCounts] ([CountDate]);
GO

CREATE UNIQUE INDEX [IX_CashCounts_CountNo] ON [CashCounts] ([CountNo]);
GO

CREATE INDEX [IX_CashCounts_Status] ON [CashCounts] ([Status]);
GO

CREATE UNIQUE INDEX [IX_CashFunds_FundCode] ON [CashFunds] ([FundCode]);
GO

CREATE INDEX [IX_CashFunds_IsDefault] ON [CashFunds] ([IsDefault]);
GO

CREATE INDEX [IX_CashTransactions_CashFundId] ON [CashTransactions] ([CashFundId]);
GO

CREATE INDEX [IX_CashTransactions_JournalHeaderId] ON [CashTransactions] ([JournalHeaderId]);
GO

CREATE INDEX [IX_CashTransactions_SourceType_SourceId] ON [CashTransactions] ([SourceType], [SourceId]);
GO

CREATE INDEX [IX_CashTransactions_Status] ON [CashTransactions] ([Status]);
GO

CREATE INDEX [IX_CashTransactions_TransactionDate] ON [CashTransactions] ([TransactionDate]);
GO

CREATE UNIQUE INDEX [IX_CashTransactions_TransactionNo] ON [CashTransactions] ([TransactionNo]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260521041101_AddCashFundModule', N'8.0.7');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

CREATE TABLE [BankAccounts] (
    [Id] uniqueidentifier NOT NULL,
    [BankCode] nvarchar(50) NOT NULL,
    [BankName] nvarchar(255) NOT NULL,
    [BranchName] nvarchar(255) NULL,
    [AccountNumber] nvarchar(100) NOT NULL,
    [AccountName] nvarchar(255) NOT NULL,
    [CurrencyCode] nvarchar(10) NOT NULL DEFAULT N'VND',
    [AccountCode] nvarchar(50) NULL,
    [OpeningBalance] decimal(18,2) NOT NULL,
    [CurrentBalance] decimal(18,2) NOT NULL,
    [IsDefault] bit NOT NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_BankAccounts] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [BankReconciliations] (
    [Id] uniqueidentifier NOT NULL,
    [BankAccountId] uniqueidentifier NOT NULL,
    [ReconciliationNo] nvarchar(50) NOT NULL,
    [ReconciliationDate] datetime2 NOT NULL,
    [BookBalance] decimal(18,2) NOT NULL,
    [StatementBalance] decimal(18,2) NOT NULL,
    [Difference] decimal(18,2) NOT NULL,
    [Status] nvarchar(30) NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [CreatedBy] nvarchar(255) NULL,
    [ConfirmedAt] datetime2 NULL,
    [ConfirmedBy] nvarchar(255) NULL,
    CONSTRAINT [PK_BankReconciliations] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [BankStatements] (
    [Id] uniqueidentifier NOT NULL,
    [BankAccountId] uniqueidentifier NOT NULL,
    [StatementNo] nvarchar(50) NOT NULL,
    [FromDate] datetime2 NOT NULL,
    [ToDate] datetime2 NOT NULL,
    [OpeningBalance] decimal(18,2) NOT NULL,
    [ClosingBalance] decimal(18,2) NOT NULL,
    [ImportedAt] datetime2 NOT NULL,
    [ImportedBy] nvarchar(255) NULL,
    [Status] nvarchar(30) NOT NULL,
    CONSTRAINT [PK_BankStatements] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_BankStatements_BankAccounts_BankAccountId] FOREIGN KEY ([BankAccountId]) REFERENCES [BankAccounts] ([Id]) ON DELETE NO ACTION
);
GO

CREATE TABLE [BankTransactions] (
    [Id] uniqueidentifier NOT NULL,
    [BankAccountId] uniqueidentifier NOT NULL,
    [TransactionNo] nvarchar(50) NOT NULL,
    [TransactionDate] datetime2 NOT NULL,
    [ValueDate] datetime2 NULL,
    [TransactionType] nvarchar(30) NOT NULL,
    [SourceType] nvarchar(50) NULL,
    [SourceId] uniqueidentifier NULL,
    [JournalHeaderId] uniqueidentifier NULL,
    [ReferenceNo] nvarchar(100) NULL,
    [Description] nvarchar(1000) NULL,
    [DebitAmount] decimal(18,2) NOT NULL,
    [CreditAmount] decimal(18,2) NOT NULL,
    [BalanceAfter] decimal(18,2) NOT NULL,
    [CounterAccountCode] nvarchar(50) NULL,
    [PartnerType] nvarchar(30) NULL,
    [PartnerId] uniqueidentifier NULL,
    [Status] nvarchar(30) NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [CreatedBy] nvarchar(255) NULL,
    [PostedAt] datetime2 NULL,
    [PostedBy] nvarchar(255) NULL,
    CONSTRAINT [PK_BankTransactions] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_BankTransactions_BankAccounts_BankAccountId] FOREIGN KEY ([BankAccountId]) REFERENCES [BankAccounts] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_BankTransactions_JournalHeaders_JournalHeaderId] FOREIGN KEY ([JournalHeaderId]) REFERENCES [JournalHeaders] ([Id])
);
GO

CREATE TABLE [BankStatementLines] (
    [Id] uniqueidentifier NOT NULL,
    [BankStatementId] uniqueidentifier NOT NULL,
    [TransactionDate] datetime2 NOT NULL,
    [ValueDate] datetime2 NULL,
    [ReferenceNo] nvarchar(100) NULL,
    [Description] nvarchar(1000) NULL,
    [DebitAmount] decimal(18,2) NOT NULL,
    [CreditAmount] decimal(18,2) NOT NULL,
    [Balance] decimal(18,2) NOT NULL,
    [MatchedBankTransactionId] uniqueidentifier NULL,
    [MatchStatus] nvarchar(30) NOT NULL,
    CONSTRAINT [PK_BankStatementLines] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_BankStatementLines_BankStatements_BankStatementId] FOREIGN KEY ([BankStatementId]) REFERENCES [BankStatements] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_BankStatementLines_BankTransactions_MatchedBankTransactionId] FOREIGN KEY ([MatchedBankTransactionId]) REFERENCES [BankTransactions] ([Id])
);
GO

CREATE TABLE [BankReconciliationLines] (
    [Id] uniqueidentifier NOT NULL,
    [BankReconciliationId] uniqueidentifier NOT NULL,
    [BankTransactionId] uniqueidentifier NULL,
    [BankStatementLineId] uniqueidentifier NULL,
    [DifferenceAmount] decimal(18,2) NOT NULL,
    [Note] nvarchar(1000) NULL,
    CONSTRAINT [PK_BankReconciliationLines] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_BankReconciliationLines_BankReconciliations_BankReconciliationId] FOREIGN KEY ([BankReconciliationId]) REFERENCES [BankReconciliations] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_BankReconciliationLines_BankStatementLines_BankStatementLineId] FOREIGN KEY ([BankStatementLineId]) REFERENCES [BankStatementLines] ([Id]),
    CONSTRAINT [FK_BankReconciliationLines_BankTransactions_BankTransactionId] FOREIGN KEY ([BankTransactionId]) REFERENCES [BankTransactions] ([Id])
);
GO

IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'AccountCode', N'AccountName', N'AccountNumber', N'BankCode', N'BankName', N'BranchName', N'CreatedAt', N'CurrencyCode', N'CurrentBalance', N'IsActive', N'IsDefault', N'OpeningBalance', N'UpdatedAt') AND [object_id] = OBJECT_ID(N'[BankAccounts]'))
    SET IDENTITY_INSERT [BankAccounts] ON;
INSERT INTO [BankAccounts] ([Id], [AccountCode], [AccountName], [AccountNumber], [BankCode], [BankName], [BranchName], [CreatedAt], [CurrencyCode], [CurrentBalance], [IsActive], [IsDefault], [OpeningBalance], [UpdatedAt])
VALUES ('8e4d05d8-0a7e-4e23-9f5a-000000000001', NULL, N'Tài khoản ngân hàng chính', N'MAIN_BANK', N'MAIN_BANK', N'Ngân hàng chính', NULL, '2026-05-21T00:00:00.0000000Z', N'VND', 0.0, CAST(1 AS bit), CAST(1 AS bit), 0.0, '2026-05-21T00:00:00.0000000Z');
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'AccountCode', N'AccountName', N'AccountNumber', N'BankCode', N'BankName', N'BranchName', N'CreatedAt', N'CurrencyCode', N'CurrentBalance', N'IsActive', N'IsDefault', N'OpeningBalance', N'UpdatedAt') AND [object_id] = OBJECT_ID(N'[BankAccounts]'))
    SET IDENTITY_INSERT [BankAccounts] OFF;
GO

CREATE UNIQUE INDEX [IX_BankAccounts_AccountNumber] ON [BankAccounts] ([AccountNumber]);
GO

CREATE INDEX [IX_BankAccounts_BankCode] ON [BankAccounts] ([BankCode]);
GO

CREATE INDEX [IX_BankAccounts_IsDefault] ON [BankAccounts] ([IsDefault]);
GO

CREATE INDEX [IX_BankReconciliationLines_BankReconciliationId] ON [BankReconciliationLines] ([BankReconciliationId]);
GO

CREATE INDEX [IX_BankReconciliationLines_BankStatementLineId] ON [BankReconciliationLines] ([BankStatementLineId]);
GO

CREATE INDEX [IX_BankReconciliationLines_BankTransactionId] ON [BankReconciliationLines] ([BankTransactionId]);
GO

CREATE INDEX [IX_BankReconciliations_BankAccountId] ON [BankReconciliations] ([BankAccountId]);
GO

CREATE INDEX [IX_BankReconciliations_ReconciliationDate] ON [BankReconciliations] ([ReconciliationDate]);
GO

CREATE UNIQUE INDEX [IX_BankReconciliations_ReconciliationNo] ON [BankReconciliations] ([ReconciliationNo]);
GO

CREATE INDEX [IX_BankReconciliations_Status] ON [BankReconciliations] ([Status]);
GO

CREATE INDEX [IX_BankStatementLines_BankStatementId] ON [BankStatementLines] ([BankStatementId]);
GO

CREATE INDEX [IX_BankStatementLines_MatchedBankTransactionId] ON [BankStatementLines] ([MatchedBankTransactionId]);
GO

CREATE INDEX [IX_BankStatementLines_MatchStatus] ON [BankStatementLines] ([MatchStatus]);
GO

CREATE INDEX [IX_BankStatementLines_TransactionDate] ON [BankStatementLines] ([TransactionDate]);
GO

CREATE INDEX [IX_BankStatements_BankAccountId] ON [BankStatements] ([BankAccountId]);
GO

CREATE INDEX [IX_BankStatements_FromDate_ToDate] ON [BankStatements] ([FromDate], [ToDate]);
GO

CREATE UNIQUE INDEX [IX_BankStatements_StatementNo] ON [BankStatements] ([StatementNo]);
GO

CREATE INDEX [IX_BankStatements_Status] ON [BankStatements] ([Status]);
GO

CREATE INDEX [IX_BankTransactions_BankAccountId] ON [BankTransactions] ([BankAccountId]);
GO

CREATE INDEX [IX_BankTransactions_JournalHeaderId] ON [BankTransactions] ([JournalHeaderId]);
GO

CREATE INDEX [IX_BankTransactions_SourceType_SourceId] ON [BankTransactions] ([SourceType], [SourceId]);
GO

CREATE INDEX [IX_BankTransactions_Status] ON [BankTransactions] ([Status]);
GO

CREATE INDEX [IX_BankTransactions_TransactionDate] ON [BankTransactions] ([TransactionDate]);
GO

CREATE UNIQUE INDEX [IX_BankTransactions_TransactionNo] ON [BankTransactions] ([TransactionNo]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260521043833_AddBankingModule', N'8.0.7');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

CREATE TABLE [FixedAssetCategories] (
    [Id] uniqueidentifier NOT NULL,
    [CategoryCode] nvarchar(50) NOT NULL,
    [CategoryName] nvarchar(255) NOT NULL,
    [AssetAccountCode] nvarchar(50) NULL,
    [DepreciationAccountCode] nvarchar(50) NULL,
    [ExpenseAccountCode] nvarchar(50) NULL,
    [UsefulLifeMonthsDefault] int NOT NULL,
    [DepreciationMethodDefault] nvarchar(50) NOT NULL DEFAULT N'StraightLine',
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_FixedAssetCategories] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [FixedAssets] (
    [Id] uniqueidentifier NOT NULL,
    [AssetCode] nvarchar(50) NOT NULL,
    [AssetName] nvarchar(255) NOT NULL,
    [CategoryId] uniqueidentifier NOT NULL,
    [SerialNumber] nvarchar(255) NULL,
    [PurchaseDate] datetime2 NOT NULL,
    [StartDepreciationDate] datetime2 NOT NULL,
    [OriginalCost] decimal(18,2) NOT NULL,
    [SalvageValue] decimal(18,2) NOT NULL,
    [UsefulLifeMonths] int NOT NULL,
    [DepreciationMethod] nvarchar(50) NOT NULL DEFAULT N'StraightLine',
    [AccumulatedDepreciation] decimal(18,2) NOT NULL,
    [RemainingValue] decimal(18,2) NOT NULL,
    [Quantity] decimal(18,2) NOT NULL,
    [DepartmentId] uniqueidentifier NULL,
    [EmployeeId] uniqueidentifier NULL,
    [WarehouseId] uniqueidentifier NULL,
    [SupplierId] uniqueidentifier NULL,
    [JournalHeaderId] uniqueidentifier NULL,
    [Status] nvarchar(30) NOT NULL,
    [Notes] nvarchar(1000) NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_FixedAssets] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_FixedAssets_FixedAssetCategories_CategoryId] FOREIGN KEY ([CategoryId]) REFERENCES [FixedAssetCategories] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_FixedAssets_JournalHeaders_JournalHeaderId] FOREIGN KEY ([JournalHeaderId]) REFERENCES [JournalHeaders] ([Id])
);
GO

CREATE TABLE [FixedAssetAdjustments] (
    [Id] uniqueidentifier NOT NULL,
    [FixedAssetId] uniqueidentifier NOT NULL,
    [AdjustmentDate] datetime2 NOT NULL,
    [AdjustmentType] nvarchar(30) NOT NULL,
    [Amount] decimal(18,2) NOT NULL,
    [Description] nvarchar(1000) NULL,
    [JournalHeaderId] uniqueidentifier NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_FixedAssetAdjustments] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_FixedAssetAdjustments_FixedAssets_FixedAssetId] FOREIGN KEY ([FixedAssetId]) REFERENCES [FixedAssets] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_FixedAssetAdjustments_JournalHeaders_JournalHeaderId] FOREIGN KEY ([JournalHeaderId]) REFERENCES [JournalHeaders] ([Id])
);
GO

CREATE TABLE [FixedAssetDepreciations] (
    [Id] uniqueidentifier NOT NULL,
    [FixedAssetId] uniqueidentifier NOT NULL,
    [DepreciationPeriod] nvarchar(20) NOT NULL,
    [DepreciationDate] datetime2 NOT NULL,
    [DepreciationAmount] decimal(18,2) NOT NULL,
    [AccumulatedAfter] decimal(18,2) NOT NULL,
    [RemainingValueAfter] decimal(18,2) NOT NULL,
    [JournalHeaderId] uniqueidentifier NULL,
    [Status] nvarchar(30) NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [PostedAt] datetime2 NULL,
    CONSTRAINT [PK_FixedAssetDepreciations] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_FixedAssetDepreciations_FixedAssets_FixedAssetId] FOREIGN KEY ([FixedAssetId]) REFERENCES [FixedAssets] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_FixedAssetDepreciations_JournalHeaders_JournalHeaderId] FOREIGN KEY ([JournalHeaderId]) REFERENCES [JournalHeaders] ([Id])
);
GO

CREATE TABLE [FixedAssetDisposals] (
    [Id] uniqueidentifier NOT NULL,
    [FixedAssetId] uniqueidentifier NOT NULL,
    [DisposalDate] datetime2 NOT NULL,
    [DisposalType] nvarchar(30) NOT NULL,
    [DisposalAmount] decimal(18,2) NOT NULL,
    [GainLossAmount] decimal(18,2) NOT NULL,
    [JournalHeaderId] uniqueidentifier NULL,
    [Notes] nvarchar(1000) NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_FixedAssetDisposals] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_FixedAssetDisposals_FixedAssets_FixedAssetId] FOREIGN KEY ([FixedAssetId]) REFERENCES [FixedAssets] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_FixedAssetDisposals_JournalHeaders_JournalHeaderId] FOREIGN KEY ([JournalHeaderId]) REFERENCES [JournalHeaders] ([Id])
);
GO

IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'AssetAccountCode', N'CategoryCode', N'CategoryName', N'CreatedAt', N'DepreciationAccountCode', N'DepreciationMethodDefault', N'ExpenseAccountCode', N'IsActive', N'UpdatedAt', N'UsefulLifeMonthsDefault') AND [object_id] = OBJECT_ID(N'[FixedAssetCategories]'))
    SET IDENTITY_INSERT [FixedAssetCategories] ON;
INSERT INTO [FixedAssetCategories] ([Id], [AssetAccountCode], [CategoryCode], [CategoryName], [CreatedAt], [DepreciationAccountCode], [DepreciationMethodDefault], [ExpenseAccountCode], [IsActive], [UpdatedAt], [UsefulLifeMonthsDefault])
VALUES ('9a71a150-2a6a-4e72-9b25-000000000001', NULL, N'TSCD_DEFAULT', N'Tài sản cố định mặc định', '2026-05-21T00:00:00.0000000Z', NULL, N'StraightLine', NULL, CAST(1 AS bit), '2026-05-21T00:00:00.0000000Z', 60);
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'AssetAccountCode', N'CategoryCode', N'CategoryName', N'CreatedAt', N'DepreciationAccountCode', N'DepreciationMethodDefault', N'ExpenseAccountCode', N'IsActive', N'UpdatedAt', N'UsefulLifeMonthsDefault') AND [object_id] = OBJECT_ID(N'[FixedAssetCategories]'))
    SET IDENTITY_INSERT [FixedAssetCategories] OFF;
GO

CREATE INDEX [IX_FixedAssetAdjustments_AdjustmentDate] ON [FixedAssetAdjustments] ([AdjustmentDate]);
GO

CREATE INDEX [IX_FixedAssetAdjustments_AdjustmentType] ON [FixedAssetAdjustments] ([AdjustmentType]);
GO

CREATE INDEX [IX_FixedAssetAdjustments_FixedAssetId] ON [FixedAssetAdjustments] ([FixedAssetId]);
GO

CREATE INDEX [IX_FixedAssetAdjustments_JournalHeaderId] ON [FixedAssetAdjustments] ([JournalHeaderId]);
GO

CREATE UNIQUE INDEX [IX_FixedAssetCategories_CategoryCode] ON [FixedAssetCategories] ([CategoryCode]);
GO

CREATE INDEX [IX_FixedAssetCategories_IsActive] ON [FixedAssetCategories] ([IsActive]);
GO

CREATE INDEX [IX_FixedAssetDepreciations_DepreciationPeriod] ON [FixedAssetDepreciations] ([DepreciationPeriod]);
GO

CREATE UNIQUE INDEX [IX_FixedAssetDepreciations_FixedAssetId_DepreciationPeriod] ON [FixedAssetDepreciations] ([FixedAssetId], [DepreciationPeriod]);
GO

CREATE INDEX [IX_FixedAssetDepreciations_JournalHeaderId] ON [FixedAssetDepreciations] ([JournalHeaderId]);
GO

CREATE INDEX [IX_FixedAssetDepreciations_Status] ON [FixedAssetDepreciations] ([Status]);
GO

CREATE INDEX [IX_FixedAssetDisposals_DisposalDate] ON [FixedAssetDisposals] ([DisposalDate]);
GO

CREATE INDEX [IX_FixedAssetDisposals_DisposalType] ON [FixedAssetDisposals] ([DisposalType]);
GO

CREATE INDEX [IX_FixedAssetDisposals_FixedAssetId] ON [FixedAssetDisposals] ([FixedAssetId]);
GO

CREATE INDEX [IX_FixedAssetDisposals_JournalHeaderId] ON [FixedAssetDisposals] ([JournalHeaderId]);
GO

CREATE UNIQUE INDEX [IX_FixedAssets_AssetCode] ON [FixedAssets] ([AssetCode]);
GO

CREATE INDEX [IX_FixedAssets_CategoryId] ON [FixedAssets] ([CategoryId]);
GO

CREATE INDEX [IX_FixedAssets_JournalHeaderId] ON [FixedAssets] ([JournalHeaderId]);
GO

CREATE INDEX [IX_FixedAssets_PurchaseDate] ON [FixedAssets] ([PurchaseDate]);
GO

CREATE INDEX [IX_FixedAssets_Status] ON [FixedAssets] ([Status]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260521051106_AddFixedAssetModule', N'8.0.7');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

CREATE TABLE [ToolAllocationRuns] (
    [Id] uniqueidentifier NOT NULL,
    [RunNo] nvarchar(50) NOT NULL,
    [AllocationPeriod] nvarchar(20) NOT NULL,
    [RunDate] datetime2 NOT NULL,
    [TotalAmount] decimal(18,2) NOT NULL,
    [Status] nvarchar(30) NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [PostedAt] datetime2 NULL,
    [Message] nvarchar(2000) NULL,
    CONSTRAINT [PK_ToolAllocationRuns] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [ToolAssetCategories] (
    [Id] uniqueidentifier NOT NULL,
    [CategoryCode] nvarchar(50) NOT NULL,
    [CategoryName] nvarchar(255) NOT NULL,
    [ToolAccountCode] nvarchar(50) NULL,
    [PrepaidExpenseAccountCode] nvarchar(50) NULL,
    [ExpenseAccountCode] nvarchar(50) NULL,
    [DefaultAllocationMonths] int NOT NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_ToolAssetCategories] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [ToolAssets] (
    [Id] uniqueidentifier NOT NULL,
    [ToolCode] nvarchar(50) NOT NULL,
    [ToolName] nvarchar(255) NOT NULL,
    [CategoryId] uniqueidentifier NOT NULL,
    [PurchaseDate] datetime2 NOT NULL,
    [OriginalCost] decimal(18,2) NOT NULL,
    [Quantity] decimal(18,2) NOT NULL,
    [UnitCost] decimal(18,2) NOT NULL,
    [AllocatedAmount] decimal(18,2) NOT NULL,
    [RemainingAmount] decimal(18,2) NOT NULL,
    [AllocationMonths] int NOT NULL,
    [StartAllocationDate] datetime2 NULL,
    [DepartmentId] uniqueidentifier NULL,
    [EmployeeId] uniqueidentifier NULL,
    [WarehouseId] uniqueidentifier NULL,
    [SupplierId] uniqueidentifier NULL,
    [JournalHeaderId] uniqueidentifier NULL,
    [Status] nvarchar(30) NOT NULL,
    [Notes] nvarchar(1000) NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_ToolAssets] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ToolAssets_JournalHeaders_JournalHeaderId] FOREIGN KEY ([JournalHeaderId]) REFERENCES [JournalHeaders] ([Id]),
    CONSTRAINT [FK_ToolAssets_ToolAssetCategories_CategoryId] FOREIGN KEY ([CategoryId]) REFERENCES [ToolAssetCategories] ([Id]) ON DELETE NO ACTION
);
GO

CREATE TABLE [ToolAllocationSchedules] (
    [Id] uniqueidentifier NOT NULL,
    [ToolAssetId] uniqueidentifier NOT NULL,
    [AllocationPeriod] nvarchar(20) NOT NULL,
    [AllocationDate] datetime2 NOT NULL,
    [AllocationAmount] decimal(18,2) NOT NULL,
    [AccumulatedAfter] decimal(18,2) NOT NULL,
    [RemainingAfter] decimal(18,2) NOT NULL,
    [JournalHeaderId] uniqueidentifier NULL,
    [Status] nvarchar(30) NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [PostedAt] datetime2 NULL,
    CONSTRAINT [PK_ToolAllocationSchedules] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ToolAllocationSchedules_JournalHeaders_JournalHeaderId] FOREIGN KEY ([JournalHeaderId]) REFERENCES [JournalHeaders] ([Id]),
    CONSTRAINT [FK_ToolAllocationSchedules_ToolAssets_ToolAssetId] FOREIGN KEY ([ToolAssetId]) REFERENCES [ToolAssets] ([Id]) ON DELETE NO ACTION
);
GO

CREATE TABLE [ToolDisposals] (
    [Id] uniqueidentifier NOT NULL,
    [DisposalNo] nvarchar(50) NOT NULL,
    [DisposalDate] datetime2 NOT NULL,
    [ToolAssetId] uniqueidentifier NOT NULL,
    [Quantity] decimal(18,2) NOT NULL,
    [RemainingAmount] decimal(18,2) NOT NULL,
    [Reason] nvarchar(1000) NULL,
    [JournalHeaderId] uniqueidentifier NULL,
    [Status] nvarchar(30) NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_ToolDisposals] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ToolDisposals_JournalHeaders_JournalHeaderId] FOREIGN KEY ([JournalHeaderId]) REFERENCES [JournalHeaders] ([Id]),
    CONSTRAINT [FK_ToolDisposals_ToolAssets_ToolAssetId] FOREIGN KEY ([ToolAssetId]) REFERENCES [ToolAssets] ([Id]) ON DELETE NO ACTION
);
GO

CREATE TABLE [ToolIssues] (
    [Id] uniqueidentifier NOT NULL,
    [IssueNo] nvarchar(50) NOT NULL,
    [IssueDate] datetime2 NOT NULL,
    [ToolAssetId] uniqueidentifier NOT NULL,
    [Quantity] decimal(18,2) NOT NULL,
    [Amount] decimal(18,2) NOT NULL,
    [DepartmentId] uniqueidentifier NULL,
    [EmployeeId] uniqueidentifier NULL,
    [Description] nvarchar(1000) NULL,
    [JournalHeaderId] uniqueidentifier NULL,
    [Status] nvarchar(30) NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [PostedAt] datetime2 NULL,
    CONSTRAINT [PK_ToolIssues] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ToolIssues_JournalHeaders_JournalHeaderId] FOREIGN KEY ([JournalHeaderId]) REFERENCES [JournalHeaders] ([Id]),
    CONSTRAINT [FK_ToolIssues_ToolAssets_ToolAssetId] FOREIGN KEY ([ToolAssetId]) REFERENCES [ToolAssets] ([Id]) ON DELETE NO ACTION
);
GO

CREATE TABLE [ToolRecoveries] (
    [Id] uniqueidentifier NOT NULL,
    [RecoveryNo] nvarchar(50) NOT NULL,
    [RecoveryDate] datetime2 NOT NULL,
    [ToolAssetId] uniqueidentifier NOT NULL,
    [Quantity] decimal(18,2) NOT NULL,
    [Amount] decimal(18,2) NOT NULL,
    [Description] nvarchar(1000) NULL,
    [Status] nvarchar(30) NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_ToolRecoveries] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ToolRecoveries_ToolAssets_ToolAssetId] FOREIGN KEY ([ToolAssetId]) REFERENCES [ToolAssets] ([Id]) ON DELETE NO ACTION
);
GO

IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'AccountCode', N'AccountingStandardCode', N'IsActive', N'IsDefault', N'RoleCode') AND [object_id] = OBJECT_ID(N'[AccountRoleMappings]'))
    SET IDENTITY_INSERT [AccountRoleMappings] ON;
INSERT INTO [AccountRoleMappings] ([Id], [AccountCode], [AccountingStandardCode], [IsActive], [IsDefault], [RoleCode])
VALUES ('c160e668-af9c-8039-3c5c-0b594f4afbb6', N'242', N'TT200', CAST(1 AS bit), CAST(1 AS bit), N'PrepaidExpense'),
('c77a7543-adc1-2faf-e7e4-b1b94fbacf92', N'242', N'TT99', CAST(1 AS bit), CAST(1 AS bit), N'PrepaidExpense');
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'AccountCode', N'AccountingStandardCode', N'IsActive', N'IsDefault', N'RoleCode') AND [object_id] = OBJECT_ID(N'[AccountRoleMappings]'))
    SET IDENTITY_INSERT [AccountRoleMappings] OFF;
GO

IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'CategoryCode', N'CategoryName', N'CreatedAt', N'DefaultAllocationMonths', N'ExpenseAccountCode', N'IsActive', N'PrepaidExpenseAccountCode', N'ToolAccountCode', N'UpdatedAt') AND [object_id] = OBJECT_ID(N'[ToolAssetCategories]'))
    SET IDENTITY_INSERT [ToolAssetCategories] ON;
INSERT INTO [ToolAssetCategories] ([Id], [CategoryCode], [CategoryName], [CreatedAt], [DefaultAllocationMonths], [ExpenseAccountCode], [IsActive], [PrepaidExpenseAccountCode], [ToolAccountCode], [UpdatedAt])
VALUES ('a82e9c51-1f1d-4d3f-9b25-000000000001', N'CCDC_DEFAULT', N'Công cụ dụng cụ mặc định', '2026-05-21T00:00:00.0000000Z', 12, NULL, CAST(1 AS bit), NULL, NULL, '2026-05-21T00:00:00.0000000Z');
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'CategoryCode', N'CategoryName', N'CreatedAt', N'DefaultAllocationMonths', N'ExpenseAccountCode', N'IsActive', N'PrepaidExpenseAccountCode', N'ToolAccountCode', N'UpdatedAt') AND [object_id] = OBJECT_ID(N'[ToolAssetCategories]'))
    SET IDENTITY_INSERT [ToolAssetCategories] OFF;
GO

CREATE INDEX [IX_ToolAllocationRuns_AllocationPeriod] ON [ToolAllocationRuns] ([AllocationPeriod]);
GO

CREATE UNIQUE INDEX [IX_ToolAllocationRuns_RunNo] ON [ToolAllocationRuns] ([RunNo]);
GO

CREATE INDEX [IX_ToolAllocationRuns_Status] ON [ToolAllocationRuns] ([Status]);
GO

CREATE INDEX [IX_ToolAllocationSchedules_AllocationPeriod] ON [ToolAllocationSchedules] ([AllocationPeriod]);
GO

CREATE INDEX [IX_ToolAllocationSchedules_JournalHeaderId] ON [ToolAllocationSchedules] ([JournalHeaderId]);
GO

CREATE INDEX [IX_ToolAllocationSchedules_Status] ON [ToolAllocationSchedules] ([Status]);
GO

CREATE UNIQUE INDEX [IX_ToolAllocationSchedules_ToolAssetId_AllocationPeriod] ON [ToolAllocationSchedules] ([ToolAssetId], [AllocationPeriod]);
GO

CREATE UNIQUE INDEX [IX_ToolAssetCategories_CategoryCode] ON [ToolAssetCategories] ([CategoryCode]);
GO

CREATE INDEX [IX_ToolAssetCategories_IsActive] ON [ToolAssetCategories] ([IsActive]);
GO

CREATE INDEX [IX_ToolAssets_CategoryId] ON [ToolAssets] ([CategoryId]);
GO

CREATE INDEX [IX_ToolAssets_JournalHeaderId] ON [ToolAssets] ([JournalHeaderId]);
GO

CREATE INDEX [IX_ToolAssets_PurchaseDate] ON [ToolAssets] ([PurchaseDate]);
GO

CREATE INDEX [IX_ToolAssets_Status] ON [ToolAssets] ([Status]);
GO

CREATE UNIQUE INDEX [IX_ToolAssets_ToolCode] ON [ToolAssets] ([ToolCode]);
GO

CREATE INDEX [IX_ToolDisposals_DisposalDate] ON [ToolDisposals] ([DisposalDate]);
GO

CREATE UNIQUE INDEX [IX_ToolDisposals_DisposalNo] ON [ToolDisposals] ([DisposalNo]);
GO

CREATE INDEX [IX_ToolDisposals_JournalHeaderId] ON [ToolDisposals] ([JournalHeaderId]);
GO

CREATE INDEX [IX_ToolDisposals_Status] ON [ToolDisposals] ([Status]);
GO

CREATE INDEX [IX_ToolDisposals_ToolAssetId] ON [ToolDisposals] ([ToolAssetId]);
GO

CREATE INDEX [IX_ToolIssues_IssueDate] ON [ToolIssues] ([IssueDate]);
GO

CREATE UNIQUE INDEX [IX_ToolIssues_IssueNo] ON [ToolIssues] ([IssueNo]);
GO

CREATE INDEX [IX_ToolIssues_JournalHeaderId] ON [ToolIssues] ([JournalHeaderId]);
GO

CREATE INDEX [IX_ToolIssues_Status] ON [ToolIssues] ([Status]);
GO

CREATE INDEX [IX_ToolIssues_ToolAssetId] ON [ToolIssues] ([ToolAssetId]);
GO

CREATE INDEX [IX_ToolRecoveries_RecoveryDate] ON [ToolRecoveries] ([RecoveryDate]);
GO

CREATE UNIQUE INDEX [IX_ToolRecoveries_RecoveryNo] ON [ToolRecoveries] ([RecoveryNo]);
GO

CREATE INDEX [IX_ToolRecoveries_Status] ON [ToolRecoveries] ([Status]);
GO

CREATE INDEX [IX_ToolRecoveries_ToolAssetId] ON [ToolRecoveries] ([ToolAssetId]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260521053544_AddToolAssetModule', N'8.0.7');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

CREATE TABLE [TaxCodes] (
    [Id] uniqueidentifier NOT NULL,
    [Code] nvarchar(50) NOT NULL,
    [Name] nvarchar(255) NOT NULL,
    [TaxRate] decimal(9,4) NOT NULL,
    [TaxType] nvarchar(30) NOT NULL,
    [InputAccountCode] nvarchar(50) NULL,
    [OutputAccountCode] nvarchar(50) NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_TaxCodes] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [TaxDeclarationDrafts] (
    [Id] uniqueidentifier NOT NULL,
    [DeclarationPeriod] nvarchar(20) NOT NULL,
    [TaxType] nvarchar(30) NOT NULL,
    [InputVatAmount] decimal(18,2) NOT NULL,
    [OutputVatAmount] decimal(18,2) NOT NULL,
    [NetVatPayable] decimal(18,2) NOT NULL,
    [Status] nvarchar(30) NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_TaxDeclarationDrafts] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [VatTransactions] (
    [Id] uniqueidentifier NOT NULL,
    [TaxCodeId] uniqueidentifier NOT NULL,
    [SourceType] nvarchar(50) NOT NULL,
    [SourceId] uniqueidentifier NULL,
    [JournalHeaderId] uniqueidentifier NULL,
    [JournalLineId] uniqueidentifier NULL,
    [InvoiceNo] nvarchar(100) NULL,
    [InvoiceDate] datetime2 NULL,
    [PartnerType] nvarchar(30) NULL,
    [PartnerId] uniqueidentifier NULL,
    [TaxBaseAmount] decimal(18,2) NOT NULL,
    [TaxRate] decimal(9,4) NOT NULL,
    [TaxAmount] decimal(18,2) NOT NULL,
    [Direction] nvarchar(20) NOT NULL,
    [DeclarationPeriod] nvarchar(20) NOT NULL,
    [Status] nvarchar(30) NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_VatTransactions] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_VatTransactions_JournalHeaders_JournalHeaderId] FOREIGN KEY ([JournalHeaderId]) REFERENCES [JournalHeaders] ([Id]),
    CONSTRAINT [FK_VatTransactions_JournalLines_JournalLineId] FOREIGN KEY ([JournalLineId]) REFERENCES [JournalLines] ([Id]),
    CONSTRAINT [FK_VatTransactions_TaxCodes_TaxCodeId] FOREIGN KEY ([TaxCodeId]) REFERENCES [TaxCodes] ([Id]) ON DELETE NO ACTION
);
GO

IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Category', N'Code', N'CreatedAt', N'DataSource', N'Description', N'IsActive', N'IsSystem', N'LayoutDefinition', N'Name', N'QueryDefinition', N'UpdatedAt') AND [object_id] = OBJECT_ID(N'[ReportTemplates]'))
    SET IDENTITY_INSERT [ReportTemplates] ON;
INSERT INTO [ReportTemplates] ([Id], [Category], [Code], [CreatedAt], [DataSource], [Description], [IsActive], [IsSystem], [LayoutDefinition], [Name], [QueryDefinition], [UpdatedAt])
VALUES ('4a9a7b4f-ac6e-4e91-9a11-000000000007', N'Tax', N'VAT_INPUT', '2026-05-21T00:00:00.0000000Z', N'VatTransaction', N'Bảng kê thuế GTGT đầu vào từ giao dịch VAT persisted.', CAST(1 AS bit), CAST(1 AS bit), NULL, N'Bảng kê VAT đầu vào', NULL, '2026-05-21T00:00:00.0000000Z'),
('4a9a7b4f-ac6e-4e91-9a11-000000000008', N'Tax', N'VAT_OUTPUT', '2026-05-21T00:00:00.0000000Z', N'VatTransaction', N'Bảng kê thuế GTGT đầu ra từ giao dịch VAT persisted.', CAST(1 AS bit), CAST(1 AS bit), NULL, N'Bảng kê VAT đầu ra', NULL, '2026-05-21T00:00:00.0000000Z');
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Category', N'Code', N'CreatedAt', N'DataSource', N'Description', N'IsActive', N'IsSystem', N'LayoutDefinition', N'Name', N'QueryDefinition', N'UpdatedAt') AND [object_id] = OBJECT_ID(N'[ReportTemplates]'))
    SET IDENTITY_INSERT [ReportTemplates] OFF;
GO

IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Code', N'CreatedAt', N'InputAccountCode', N'IsActive', N'Name', N'OutputAccountCode', N'TaxRate', N'TaxType', N'UpdatedAt') AND [object_id] = OBJECT_ID(N'[TaxCodes]'))
    SET IDENTITY_INSERT [TaxCodes] ON;
INSERT INTO [TaxCodes] ([Id], [Code], [CreatedAt], [InputAccountCode], [IsActive], [Name], [OutputAccountCode], [TaxRate], [TaxType], [UpdatedAt])
VALUES ('b7ac6c11-2a40-4d60-9b25-000000000000', N'VAT0', '2026-05-21T00:00:00.0000000Z', NULL, CAST(1 AS bit), N'Thuế GTGT 0%', NULL, 0.0, N'VATInput', '2026-05-21T00:00:00.0000000Z'),
('b7ac6c11-2a40-4d60-9b25-000000000005', N'VAT5', '2026-05-21T00:00:00.0000000Z', NULL, CAST(1 AS bit), N'Thuế GTGT 5%', NULL, 5.0, N'VATInput', '2026-05-21T00:00:00.0000000Z'),
('b7ac6c11-2a40-4d60-9b25-000000000008', N'VAT8', '2026-05-21T00:00:00.0000000Z', NULL, CAST(1 AS bit), N'Thuế GTGT 8%', NULL, 8.0, N'VATInput', '2026-05-21T00:00:00.0000000Z'),
('b7ac6c11-2a40-4d60-9b25-000000000010', N'VAT10', '2026-05-21T00:00:00.0000000Z', NULL, CAST(1 AS bit), N'Thuế GTGT 10%', NULL, 10.0, N'VATInput', '2026-05-21T00:00:00.0000000Z'),
('b7ac6c11-2a40-4d60-9b25-000000000999', N'KCT', '2026-05-21T00:00:00.0000000Z', NULL, CAST(1 AS bit), N'Không chịu thuế', NULL, 0.0, N'NonTaxable', '2026-05-21T00:00:00.0000000Z');
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Code', N'CreatedAt', N'InputAccountCode', N'IsActive', N'Name', N'OutputAccountCode', N'TaxRate', N'TaxType', N'UpdatedAt') AND [object_id] = OBJECT_ID(N'[TaxCodes]'))
    SET IDENTITY_INSERT [TaxCodes] OFF;
GO

CREATE UNIQUE INDEX [IX_TaxCodes_Code] ON [TaxCodes] ([Code]);
GO

CREATE INDEX [IX_TaxCodes_IsActive] ON [TaxCodes] ([IsActive]);
GO

CREATE INDEX [IX_TaxCodes_TaxType] ON [TaxCodes] ([TaxType]);
GO

CREATE UNIQUE INDEX [IX_TaxDeclarationDrafts_DeclarationPeriod_TaxType] ON [TaxDeclarationDrafts] ([DeclarationPeriod], [TaxType]);
GO

CREATE INDEX [IX_TaxDeclarationDrafts_Status] ON [TaxDeclarationDrafts] ([Status]);
GO

CREATE INDEX [IX_VatTransactions_DeclarationPeriod] ON [VatTransactions] ([DeclarationPeriod]);
GO

CREATE INDEX [IX_VatTransactions_Direction] ON [VatTransactions] ([Direction]);
GO

CREATE INDEX [IX_VatTransactions_JournalHeaderId] ON [VatTransactions] ([JournalHeaderId]);
GO

CREATE UNIQUE INDEX [IX_VatTransactions_JournalLineId] ON [VatTransactions] ([JournalLineId]) WHERE [JournalLineId] IS NOT NULL;
GO

CREATE INDEX [IX_VatTransactions_SourceType_SourceId] ON [VatTransactions] ([SourceType], [SourceId]);
GO

CREATE INDEX [IX_VatTransactions_Status] ON [VatTransactions] ([Status]);
GO

CREATE INDEX [IX_VatTransactions_TaxCodeId] ON [VatTransactions] ([TaxCodeId]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260521072800_AddVatModule', N'8.0.7');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

CREATE TABLE [AccountBalanceSnapshots] (
    [Id] uniqueidentifier NOT NULL,
    [FiscalYear] int NOT NULL,
    [Period] nvarchar(20) NOT NULL,
    [AccountCode] nvarchar(50) NOT NULL,
    [OpeningDebit] decimal(18,2) NOT NULL,
    [OpeningCredit] decimal(18,2) NOT NULL,
    [PeriodDebit] decimal(18,2) NOT NULL,
    [PeriodCredit] decimal(18,2) NOT NULL,
    [ClosingDebit] decimal(18,2) NOT NULL,
    [ClosingCredit] decimal(18,2) NOT NULL,
    [SnapshotDate] datetime2 NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_AccountBalanceSnapshots] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [ClosingEntries] (
    [Id] uniqueidentifier NOT NULL,
    [FiscalYear] int NOT NULL,
    [Period] nvarchar(20) NOT NULL,
    [ClosingDate] datetime2 NOT NULL,
    [JournalHeaderId] uniqueidentifier NULL,
    [Status] nvarchar(30) NOT NULL,
    [Notes] nvarchar(1000) NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_ClosingEntries] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ClosingEntries_JournalHeaders_JournalHeaderId] FOREIGN KEY ([JournalHeaderId]) REFERENCES [JournalHeaders] ([Id])
);
GO

CREATE TABLE [FinancialStatementExecutions] (
    [Id] uniqueidentifier NOT NULL,
    [StatementType] nvarchar(50) NOT NULL,
    [FiscalYear] int NOT NULL,
    [Period] nvarchar(20) NOT NULL,
    [GeneratedAt] datetime2 NOT NULL,
    [GeneratedBy] nvarchar(255) NULL,
    [Status] nvarchar(30) NOT NULL,
    [JsonData] nvarchar(max) NULL,
    CONSTRAINT [PK_FinancialStatementExecutions] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [ClosingEntryLines] (
    [Id] uniqueidentifier NOT NULL,
    [ClosingEntryId] uniqueidentifier NOT NULL,
    [AccountCode] nvarchar(50) NOT NULL,
    [DebitAmount] decimal(18,2) NOT NULL,
    [CreditAmount] decimal(18,2) NOT NULL,
    CONSTRAINT [PK_ClosingEntryLines] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ClosingEntryLines_ClosingEntries_ClosingEntryId] FOREIGN KEY ([ClosingEntryId]) REFERENCES [ClosingEntries] ([Id]) ON DELETE CASCADE
);
GO

IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'AccountCode', N'AccountingStandardCode', N'IsActive', N'IsDefault', N'RoleCode') AND [object_id] = OBJECT_ID(N'[AccountRoleMappings]'))
    SET IDENTITY_INSERT [AccountRoleMappings] ON;
INSERT INTO [AccountRoleMappings] ([Id], [AccountCode], [AccountingStandardCode], [IsActive], [IsDefault], [RoleCode])
VALUES ('37294164-9c91-b856-8d6f-3a7a8b36ce12', N'911', N'TT200', CAST(1 AS bit), CAST(1 AS bit), N'IncomeSummary'),
('88377036-c67d-1495-472b-f508e974f060', N'421', N'TT99', CAST(1 AS bit), CAST(1 AS bit), N'RetainedEarnings'),
('909f56ff-dc3c-dfb5-84b9-3d442da55cba', N'421', N'TT200', CAST(1 AS bit), CAST(1 AS bit), N'RetainedEarnings'),
('a5dde8ea-9625-82c4-7297-f675d224c6c2', N'911', N'TT99', CAST(1 AS bit), CAST(1 AS bit), N'IncomeSummary');
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'AccountCode', N'AccountingStandardCode', N'IsActive', N'IsDefault', N'RoleCode') AND [object_id] = OBJECT_ID(N'[AccountRoleMappings]'))
    SET IDENTITY_INSERT [AccountRoleMappings] OFF;
GO

IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Category', N'Code', N'CreatedAt', N'DataSource', N'Description', N'IsActive', N'IsSystem', N'LayoutDefinition', N'Name', N'QueryDefinition', N'UpdatedAt') AND [object_id] = OBJECT_ID(N'[ReportTemplates]'))
    SET IDENTITY_INSERT [ReportTemplates] ON;
INSERT INTO [ReportTemplates] ([Id], [Category], [Code], [CreatedAt], [DataSource], [Description], [IsActive], [IsSystem], [LayoutDefinition], [Name], [QueryDefinition], [UpdatedAt])
VALUES ('4a9a7b4f-ac6e-4e91-9a11-000000000009', N'Accounting', N'BALANCE_SHEET', '2026-05-21T00:00:00.0000000Z', N'FinancialStatement', N'Báo cáo tài sản, nợ phải trả và vốn chủ sở hữu từ Journal persisted.', CAST(1 AS bit), CAST(1 AS bit), NULL, N'Bảng cân đối kế toán', NULL, '2026-05-21T00:00:00.0000000Z'),
('4a9a7b4f-ac6e-4e91-9a11-000000000010', N'Accounting', N'INCOME_STATEMENT', '2026-05-21T00:00:00.0000000Z', N'FinancialStatement', N'Báo cáo doanh thu, chi phí và lợi nhuận từ Journal persisted.', CAST(1 AS bit), CAST(1 AS bit), NULL, N'Báo cáo kết quả kinh doanh', NULL, '2026-05-21T00:00:00.0000000Z'),
('4a9a7b4f-ac6e-4e91-9a11-000000000011', N'Accounting', N'CASH_FLOW', '2026-05-21T00:00:00.0000000Z', N'FinancialStatement', N'Skeleton báo cáo lưu chuyển tiền tệ từ tài khoản tiền và ngân hàng.', CAST(1 AS bit), CAST(1 AS bit), NULL, N'Báo cáo lưu chuyển tiền tệ', NULL, '2026-05-21T00:00:00.0000000Z');
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Category', N'Code', N'CreatedAt', N'DataSource', N'Description', N'IsActive', N'IsSystem', N'LayoutDefinition', N'Name', N'QueryDefinition', N'UpdatedAt') AND [object_id] = OBJECT_ID(N'[ReportTemplates]'))
    SET IDENTITY_INSERT [ReportTemplates] OFF;
GO

CREATE INDEX [IX_AccountBalanceSnapshots_AccountCode] ON [AccountBalanceSnapshots] ([AccountCode]);
GO

CREATE INDEX [IX_AccountBalanceSnapshots_FiscalYear] ON [AccountBalanceSnapshots] ([FiscalYear]);
GO

CREATE INDEX [IX_AccountBalanceSnapshots_Period] ON [AccountBalanceSnapshots] ([Period]);
GO

CREATE UNIQUE INDEX [IX_AccountBalanceSnapshots_Period_AccountCode] ON [AccountBalanceSnapshots] ([Period], [AccountCode]);
GO

CREATE INDEX [IX_ClosingEntries_FiscalYear] ON [ClosingEntries] ([FiscalYear]);
GO

CREATE INDEX [IX_ClosingEntries_JournalHeaderId] ON [ClosingEntries] ([JournalHeaderId]);
GO

CREATE INDEX [IX_ClosingEntries_Period] ON [ClosingEntries] ([Period]);
GO

CREATE INDEX [IX_ClosingEntries_Status] ON [ClosingEntries] ([Status]);
GO

CREATE INDEX [IX_ClosingEntryLines_AccountCode] ON [ClosingEntryLines] ([AccountCode]);
GO

CREATE INDEX [IX_ClosingEntryLines_ClosingEntryId] ON [ClosingEntryLines] ([ClosingEntryId]);
GO

CREATE INDEX [IX_FinancialStatementExecutions_FiscalYear] ON [FinancialStatementExecutions] ([FiscalYear]);
GO

CREATE INDEX [IX_FinancialStatementExecutions_Period] ON [FinancialStatementExecutions] ([Period]);
GO

CREATE INDEX [IX_FinancialStatementExecutions_StatementType] ON [FinancialStatementExecutions] ([StatementType]);
GO

CREATE INDEX [IX_FinancialStatementExecutions_Status] ON [FinancialStatementExecutions] ([Status]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260521080623_AddFinancialStatementEngine', N'8.0.7');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

ALTER TABLE [ToolAssets] ADD [RowVersion] rowversion NULL;
GO

ALTER TABLE [StockReceipts] ADD [RowVersion] rowversion NULL;
GO

ALTER TABLE [StockIssues] ADD [RowVersion] rowversion NULL;
GO

ALTER TABLE [JournalHeaders] ADD [RowVersion] rowversion NULL;
GO

ALTER TABLE [FixedAssets] ADD [RowVersion] rowversion NULL;
GO

ALTER TABLE [CashTransactions] ADD [RowVersion] rowversion NULL;
GO

ALTER TABLE [BankTransactions] ADD [RowVersion] rowversion NULL;
GO

ALTER TABLE [AuditLogs] ADD [CorrelationId] nvarchar(100) NULL;
GO

ALTER TABLE [AuditLogs] ADD [EntityId] nvarchar(100) NOT NULL DEFAULT N'';
GO

ALTER TABLE [AuditLogs] ADD [EntityName] nvarchar(100) NOT NULL DEFAULT N'';
GO

ALTER TABLE [AuditLogs] ADD [IpAddress] nvarchar(100) NULL;
GO

ALTER TABLE [AuditLogs] ADD [NewValuesJson] nvarchar(max) NULL;
GO

ALTER TABLE [AuditLogs] ADD [OldValuesJson] nvarchar(max) NULL;
GO

CREATE INDEX [IX_AuditLogs_Action] ON [AuditLogs] ([Action]);
GO

CREATE INDEX [IX_AuditLogs_EntityName_EntityId] ON [AuditLogs] ([EntityName], [EntityId]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260521093739_AddAuditTrailAndConcurrency', N'8.0.7');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

CREATE TABLE [AppRoles] (
    [Id] uniqueidentifier NOT NULL,
    [Code] nvarchar(80) NOT NULL,
    [Name] nvarchar(160) NOT NULL,
    [Description] nvarchar(500) NULL,
    [IsSystem] bit NOT NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_AppRoles] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [AppUserPermissions] (
    [Id] uniqueidentifier NOT NULL,
    [UserId] uniqueidentifier NOT NULL,
    [PermissionCode] nvarchar(120) NOT NULL,
    [IsGranted] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [CreatedBy] nvarchar(255) NULL,
    CONSTRAINT [PK_AppUserPermissions] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [UserWorkspacePreferences] (
    [Id] uniqueidentifier NOT NULL,
    [UserId] uniqueidentifier NOT NULL,
    [Category] nvarchar(80) NOT NULL,
    [PreferenceKey] nvarchar(120) NOT NULL,
    [ValueJson] nvarchar(max) NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_UserWorkspacePreferences] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [AppRolePermissions] (
    [Id] uniqueidentifier NOT NULL,
    [RoleId] uniqueidentifier NOT NULL,
    [PermissionCode] nvarchar(120) NOT NULL,
    CONSTRAINT [PK_AppRolePermissions] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_AppRolePermissions_AppRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [AppRoles] ([Id]) ON DELETE CASCADE
);
GO

CREATE UNIQUE INDEX [IX_AppRolePermissions_RoleId_PermissionCode] ON [AppRolePermissions] ([RoleId], [PermissionCode]);
GO

CREATE UNIQUE INDEX [IX_AppRoles_Code] ON [AppRoles] ([Code]);
GO

CREATE INDEX [IX_AppUserPermissions_UserId] ON [AppUserPermissions] ([UserId]);
GO

CREATE UNIQUE INDEX [IX_AppUserPermissions_UserId_PermissionCode] ON [AppUserPermissions] ([UserId], [PermissionCode]);
GO

CREATE INDEX [IX_UserWorkspacePreferences_UserId] ON [UserWorkspacePreferences] ([UserId]);
GO

CREATE UNIQUE INDEX [IX_UserWorkspacePreferences_UserId_Category_PreferenceKey] ON [UserWorkspacePreferences] ([UserId], [Category], [PreferenceKey]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260522131611_AddEnterpriseShellAndPermissions', N'8.0.7');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

ALTER TABLE [AppRoles] ADD [IsArchived] bit NOT NULL DEFAULT CAST(0 AS bit);
GO

CREATE TABLE [AppPermissions] (
    [Id] uniqueidentifier NOT NULL,
    [Code] nvarchar(120) NOT NULL,
    [Name] nvarchar(180) NOT NULL,
    [Group] nvarchar(80) NOT NULL,
    [Description] nvarchar(500) NULL,
    CONSTRAINT [PK_AppPermissions] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [AppUsers] (
    [Id] uniqueidentifier NOT NULL,
    [UserName] nvarchar(120) NOT NULL,
    [DisplayName] nvarchar(180) NOT NULL,
    [Email] nvarchar(255) NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    CONSTRAINT [PK_AppUsers] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [BackgroundJobExecutions] (
    [Id] uniqueidentifier NOT NULL,
    [JobName] nvarchar(160) NOT NULL,
    [JobType] nvarchar(80) NOT NULL,
    [Status] nvarchar(40) NOT NULL,
    [StartedAt] datetime2 NOT NULL,
    [CompletedAt] datetime2 NULL,
    [DurationMs] bigint NOT NULL,
    [Message] nvarchar(1000) NULL,
    [Error] nvarchar(4000) NULL,
    [RequestedBy] nvarchar(255) NULL,
    [CorrelationId] nvarchar(64) NOT NULL,
    [CancellationRequested] bit NOT NULL,
    CONSTRAINT [PK_BackgroundJobExecutions] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [AppUserRoles] (
    [Id] uniqueidentifier NOT NULL,
    [UserId] uniqueidentifier NOT NULL,
    [RoleId] uniqueidentifier NOT NULL,
    CONSTRAINT [PK_AppUserRoles] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_AppUserRoles_AppRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [AppRoles] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_AppUserRoles_AppUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AppUsers] ([Id]) ON DELETE CASCADE
);
GO

CREATE UNIQUE INDEX [IX_AppPermissions_Code] ON [AppPermissions] ([Code]);
GO

CREATE INDEX [IX_AppUserRoles_RoleId] ON [AppUserRoles] ([RoleId]);
GO

CREATE UNIQUE INDEX [IX_AppUserRoles_UserId_RoleId] ON [AppUserRoles] ([UserId], [RoleId]);
GO

CREATE UNIQUE INDEX [IX_AppUsers_UserName] ON [AppUsers] ([UserName]);
GO

CREATE INDEX [IX_BackgroundJobExecutions_JobName] ON [BackgroundJobExecutions] ([JobName]);
GO

CREATE INDEX [IX_BackgroundJobExecutions_StartedAt] ON [BackgroundJobExecutions] ([StartedAt]);
GO

CREATE INDEX [IX_BackgroundJobExecutions_Status] ON [BackgroundJobExecutions] ([Status]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260522133345_AddEnterpriseOperationsAndSecurityUi', N'8.0.7');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

DROP INDEX [IX_UserWorkspacePreferences_UserId_Category_PreferenceKey] ON [UserWorkspacePreferences];
GO

DROP INDEX [IX_AppUserPermissions_UserId_PermissionCode] ON [AppUserPermissions];
GO

ALTER TABLE [VatTransactions] ADD [BranchId] uniqueidentifier NULL;
GO

ALTER TABLE [VatTransactions] ADD [CompanyId] uniqueidentifier NULL;
GO

ALTER TABLE [UserWorkspacePreferences] ADD [BranchId] uniqueidentifier NULL;
GO

ALTER TABLE [UserWorkspacePreferences] ADD [CompanyId] uniqueidentifier NULL;
GO

ALTER TABLE [ToolRecoveries] ADD [BranchId] uniqueidentifier NULL;
GO

ALTER TABLE [ToolRecoveries] ADD [CompanyId] uniqueidentifier NULL;
GO

ALTER TABLE [ToolIssues] ADD [BranchId] uniqueidentifier NULL;
GO

ALTER TABLE [ToolIssues] ADD [CompanyId] uniqueidentifier NULL;
GO

ALTER TABLE [ToolDisposals] ADD [BranchId] uniqueidentifier NULL;
GO

ALTER TABLE [ToolDisposals] ADD [CompanyId] uniqueidentifier NULL;
GO

ALTER TABLE [ToolAssets] ADD [BranchId] uniqueidentifier NULL;
GO

ALTER TABLE [ToolAssets] ADD [CompanyId] uniqueidentifier NULL;
GO

ALTER TABLE [ToolAssetCategories] ADD [CompanyId] uniqueidentifier NULL;
GO

ALTER TABLE [ToolAllocationSchedules] ADD [BranchId] uniqueidentifier NULL;
GO

ALTER TABLE [ToolAllocationSchedules] ADD [CompanyId] uniqueidentifier NULL;
GO

ALTER TABLE [ToolAllocationRuns] ADD [BranchId] uniqueidentifier NULL;
GO

ALTER TABLE [ToolAllocationRuns] ADD [CompanyId] uniqueidentifier NULL;
GO

ALTER TABLE [TaxDeclarationDrafts] ADD [BranchId] uniqueidentifier NULL;
GO

ALTER TABLE [TaxDeclarationDrafts] ADD [CompanyId] uniqueidentifier NULL;
GO

ALTER TABLE [TaxCodes] ADD [CompanyId] uniqueidentifier NULL;
GO

ALTER TABLE [StockReceipts] ADD [BranchId] uniqueidentifier NULL;
GO

ALTER TABLE [StockReceipts] ADD [CompanyId] uniqueidentifier NULL;
GO

ALTER TABLE [StockReceiptDetails] ADD [BranchId] uniqueidentifier NULL;
GO

ALTER TABLE [StockReceiptDetails] ADD [CompanyId] uniqueidentifier NULL;
GO

ALTER TABLE [StockIssues] ADD [BranchId] uniqueidentifier NULL;
GO

ALTER TABLE [StockIssues] ADD [CompanyId] uniqueidentifier NULL;
GO

ALTER TABLE [StockIssueDetails] ADD [BranchId] uniqueidentifier NULL;
GO

ALTER TABLE [StockIssueDetails] ADD [CompanyId] uniqueidentifier NULL;
GO

ALTER TABLE [StockBalances] ADD [BranchId] uniqueidentifier NULL;
GO

ALTER TABLE [StockBalances] ADD [CompanyId] uniqueidentifier NULL;
GO

ALTER TABLE [ReportTemplates] ADD [CompanyId] uniqueidentifier NULL;
GO

ALTER TABLE [ReportParameters] ADD [CompanyId] uniqueidentifier NULL;
GO

ALTER TABLE [ReportExecutions] ADD [BranchId] uniqueidentifier NULL;
GO

ALTER TABLE [ReportExecutions] ADD [CompanyId] uniqueidentifier NULL;
GO

ALTER TABLE [ReportDefinitions] ADD [CompanyId] uniqueidentifier NULL;
GO

ALTER TABLE [ReceivableTransactions] ADD [BranchId] uniqueidentifier NULL;
GO

ALTER TABLE [ReceivableTransactions] ADD [CompanyId] uniqueidentifier NULL;
GO

ALTER TABLE [PostingSources] ADD [BranchId] uniqueidentifier NULL;
GO

ALTER TABLE [PostingSources] ADD [CompanyId] uniqueidentifier NULL;
GO

ALTER TABLE [PostingBatches] ADD [BranchId] uniqueidentifier NULL;
GO

ALTER TABLE [PostingBatches] ADD [CompanyId] uniqueidentifier NULL;
GO

ALTER TABLE [PaymentAllocations] ADD [BranchId] uniqueidentifier NULL;
GO

ALTER TABLE [PaymentAllocations] ADD [CompanyId] uniqueidentifier NULL;
GO

ALTER TABLE [PayableTransactions] ADD [BranchId] uniqueidentifier NULL;
GO

ALTER TABLE [PayableTransactions] ADD [CompanyId] uniqueidentifier NULL;
GO

ALTER TABLE [JournalLines] ADD [BranchId] uniqueidentifier NULL;
GO

ALTER TABLE [JournalLines] ADD [CompanyId] uniqueidentifier NULL;
GO

ALTER TABLE [JournalHeaders] ADD [BranchId] uniqueidentifier NULL;
GO

ALTER TABLE [JournalHeaders] ADD [CompanyId] uniqueidentifier NULL;
GO

ALTER TABLE [InventoryCostTransactions] ADD [BranchId] uniqueidentifier NULL;
GO

ALTER TABLE [InventoryCostTransactions] ADD [CompanyId] uniqueidentifier NULL;
GO

ALTER TABLE [InventoryCostLayers] ADD [BranchId] uniqueidentifier NULL;
GO

ALTER TABLE [InventoryCostLayers] ADD [CompanyId] uniqueidentifier NULL;
GO

ALTER TABLE [FixedAssets] ADD [BranchId] uniqueidentifier NULL;
GO

ALTER TABLE [FixedAssets] ADD [CompanyId] uniqueidentifier NULL;
GO

ALTER TABLE [FixedAssetDisposals] ADD [BranchId] uniqueidentifier NULL;
GO

ALTER TABLE [FixedAssetDisposals] ADD [CompanyId] uniqueidentifier NULL;
GO

ALTER TABLE [FixedAssetDepreciations] ADD [BranchId] uniqueidentifier NULL;
GO

ALTER TABLE [FixedAssetDepreciations] ADD [CompanyId] uniqueidentifier NULL;
GO

ALTER TABLE [FixedAssetCategories] ADD [CompanyId] uniqueidentifier NULL;
GO

ALTER TABLE [FixedAssetAdjustments] ADD [BranchId] uniqueidentifier NULL;
GO

ALTER TABLE [FixedAssetAdjustments] ADD [CompanyId] uniqueidentifier NULL;
GO

ALTER TABLE [FinancialStatementMappings] ADD [CompanyId] uniqueidentifier NULL;
GO

ALTER TABLE [FinancialStatementExecutions] ADD [BranchId] uniqueidentifier NULL;
GO

ALTER TABLE [FinancialStatementExecutions] ADD [CompanyId] uniqueidentifier NULL;
GO

ALTER TABLE [DebtReconciliations] ADD [BranchId] uniqueidentifier NULL;
GO

ALTER TABLE [DebtReconciliations] ADD [CompanyId] uniqueidentifier NULL;
GO

ALTER TABLE [DebtReconciliationLines] ADD [BranchId] uniqueidentifier NULL;
GO

ALTER TABLE [DebtReconciliationLines] ADD [CompanyId] uniqueidentifier NULL;
GO

ALTER TABLE [ClosingEntryLines] ADD [BranchId] uniqueidentifier NULL;
GO

ALTER TABLE [ClosingEntryLines] ADD [CompanyId] uniqueidentifier NULL;
GO

ALTER TABLE [ClosingEntries] ADD [BranchId] uniqueidentifier NULL;
GO

ALTER TABLE [ClosingEntries] ADD [CompanyId] uniqueidentifier NULL;
GO

ALTER TABLE [CashTransactions] ADD [BranchId] uniqueidentifier NULL;
GO

ALTER TABLE [CashTransactions] ADD [CompanyId] uniqueidentifier NULL;
GO

ALTER TABLE [CashFunds] ADD [BranchId] uniqueidentifier NULL;
GO

ALTER TABLE [CashFunds] ADD [CompanyId] uniqueidentifier NULL;
GO

ALTER TABLE [CashCounts] ADD [BranchId] uniqueidentifier NULL;
GO

ALTER TABLE [CashCounts] ADD [CompanyId] uniqueidentifier NULL;
GO

ALTER TABLE [CashCountLines] ADD [BranchId] uniqueidentifier NULL;
GO

ALTER TABLE [CashCountLines] ADD [CompanyId] uniqueidentifier NULL;
GO

ALTER TABLE [BankTransactions] ADD [BranchId] uniqueidentifier NULL;
GO

ALTER TABLE [BankTransactions] ADD [CompanyId] uniqueidentifier NULL;
GO

ALTER TABLE [BankStatements] ADD [BranchId] uniqueidentifier NULL;
GO

ALTER TABLE [BankStatements] ADD [CompanyId] uniqueidentifier NULL;
GO

ALTER TABLE [BankStatementLines] ADD [BranchId] uniqueidentifier NULL;
GO

ALTER TABLE [BankStatementLines] ADD [CompanyId] uniqueidentifier NULL;
GO

ALTER TABLE [BankReconciliations] ADD [BranchId] uniqueidentifier NULL;
GO

ALTER TABLE [BankReconciliations] ADD [CompanyId] uniqueidentifier NULL;
GO

ALTER TABLE [BankReconciliationLines] ADD [BranchId] uniqueidentifier NULL;
GO

ALTER TABLE [BankReconciliationLines] ADD [CompanyId] uniqueidentifier NULL;
GO

ALTER TABLE [BankAccounts] ADD [BranchId] uniqueidentifier NULL;
GO

ALTER TABLE [BankAccounts] ADD [CompanyId] uniqueidentifier NULL;
GO

ALTER TABLE [AuditLogs] ADD [BranchId] uniqueidentifier NULL;
GO

ALTER TABLE [AuditLogs] ADD [CompanyId] uniqueidentifier NULL;
GO

ALTER TABLE [AppUserPermissions] ADD [BranchId] uniqueidentifier NULL;
GO

ALTER TABLE [AppUserPermissions] ADD [CompanyId] uniqueidentifier NULL;
GO

ALTER TABLE [AgingSnapshots] ADD [BranchId] uniqueidentifier NULL;
GO

ALTER TABLE [AgingSnapshots] ADD [CompanyId] uniqueidentifier NULL;
GO

ALTER TABLE [AccountBalanceSnapshots] ADD [BranchId] uniqueidentifier NULL;
GO

ALTER TABLE [AccountBalanceSnapshots] ADD [CompanyId] uniqueidentifier NULL;
GO

CREATE TABLE [Companies] (
    [Id] uniqueidentifier NOT NULL,
    [Code] nvarchar(40) NOT NULL,
    [Name] nvarchar(255) NOT NULL,
    [TaxCode] nvarchar(50) NULL,
    [Address] nvarchar(500) NULL,
    [IsDefault] bit NOT NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_Companies] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [UserBranchAccesses] (
    [Id] uniqueidentifier NOT NULL,
    [UserId] uniqueidentifier NOT NULL,
    [CompanyId] uniqueidentifier NOT NULL,
    [BranchId] uniqueidentifier NOT NULL,
    [IsDefault] bit NOT NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_UserBranchAccesses] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [UserCompanyAccesses] (
    [Id] uniqueidentifier NOT NULL,
    [UserId] uniqueidentifier NOT NULL,
    [CompanyId] uniqueidentifier NOT NULL,
    [IsDefault] bit NOT NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_UserCompanyAccesses] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [Branches] (
    [Id] uniqueidentifier NOT NULL,
    [CompanyId] uniqueidentifier NOT NULL,
    [Code] nvarchar(40) NOT NULL,
    [Name] nvarchar(255) NOT NULL,
    [Address] nvarchar(500) NULL,
    [IsDefault] bit NOT NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_Branches] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Branches_Companies_CompanyId] FOREIGN KEY ([CompanyId]) REFERENCES [Companies] ([Id]) ON DELETE CASCADE
);
GO

UPDATE [BankAccounts] SET [BranchId] = NULL, [CompanyId] = NULL
WHERE [Id] = '8e4d05d8-0a7e-4e23-9f5a-000000000001';
SELECT @@ROWCOUNT;

GO

UPDATE [CashFunds] SET [BranchId] = NULL, [CompanyId] = NULL
WHERE [Id] = '7ac7ad91-6e6a-4b2c-9f9e-000000000001';
SELECT @@ROWCOUNT;

GO

IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Address', N'Code', N'CreatedAt', N'IsActive', N'IsDefault', N'Name', N'TaxCode', N'UpdatedAt') AND [object_id] = OBJECT_ID(N'[Companies]'))
    SET IDENTITY_INSERT [Companies] ON;
INSERT INTO [Companies] ([Id], [Address], [Code], [CreatedAt], [IsActive], [IsDefault], [Name], [TaxCode], [UpdatedAt])
VALUES ('11111111-1111-1111-1111-111111111111', NULL, N'DEFAULT', '2026-05-22T00:00:00.0000000Z', CAST(1 AS bit), CAST(1 AS bit), N'Default Company', NULL, '2026-05-22T00:00:00.0000000Z');
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Address', N'Code', N'CreatedAt', N'IsActive', N'IsDefault', N'Name', N'TaxCode', N'UpdatedAt') AND [object_id] = OBJECT_ID(N'[Companies]'))
    SET IDENTITY_INSERT [Companies] OFF;
GO

UPDATE [FinancialStatementMappings] SET [CompanyId] = NULL
WHERE [Id] = '0182d2aa-0a17-791a-cb65-a65e5effab7a';
SELECT @@ROWCOUNT;

GO

UPDATE [FinancialStatementMappings] SET [CompanyId] = NULL
WHERE [Id] = '1009532d-729d-8d6e-4f9a-87f4bb281c74';
SELECT @@ROWCOUNT;

GO

UPDATE [FinancialStatementMappings] SET [CompanyId] = NULL
WHERE [Id] = '15c78b04-6e79-d552-26a4-e73c1cf809dc';
SELECT @@ROWCOUNT;

GO

UPDATE [FinancialStatementMappings] SET [CompanyId] = NULL
WHERE [Id] = '1838b8af-4691-772a-eca2-4397a2d490c1';
SELECT @@ROWCOUNT;

GO

UPDATE [FinancialStatementMappings] SET [CompanyId] = NULL
WHERE [Id] = '21246897-c179-8a44-e21f-47b6ffdd43e5';
SELECT @@ROWCOUNT;

GO

UPDATE [FinancialStatementMappings] SET [CompanyId] = NULL
WHERE [Id] = '292432f7-5788-bec1-ce3f-cc814025844d';
SELECT @@ROWCOUNT;

GO

UPDATE [FinancialStatementMappings] SET [CompanyId] = NULL
WHERE [Id] = '37e6948c-6831-61c8-e055-7e3af262d316';
SELECT @@ROWCOUNT;

GO

UPDATE [FinancialStatementMappings] SET [CompanyId] = NULL
WHERE [Id] = '47ed997b-5f57-b22d-5b54-8309c419f29a';
SELECT @@ROWCOUNT;

GO

UPDATE [FinancialStatementMappings] SET [CompanyId] = NULL
WHERE [Id] = '5076eb2e-81f8-607a-5b50-5b8c1b736d2e';
SELECT @@ROWCOUNT;

GO

UPDATE [FinancialStatementMappings] SET [CompanyId] = NULL
WHERE [Id] = '52fadcac-2053-01a8-eaf9-9ba6d77714b6';
SELECT @@ROWCOUNT;

GO

UPDATE [FinancialStatementMappings] SET [CompanyId] = NULL
WHERE [Id] = '576f7773-3663-768b-d830-d8daf2730795';
SELECT @@ROWCOUNT;

GO

UPDATE [FinancialStatementMappings] SET [CompanyId] = NULL
WHERE [Id] = '5f7d1620-a49e-71c0-971f-877e63950596';
SELECT @@ROWCOUNT;

GO

UPDATE [FinancialStatementMappings] SET [CompanyId] = NULL
WHERE [Id] = '67a4801b-73cb-bd68-0f70-38712b9569d8';
SELECT @@ROWCOUNT;

GO

UPDATE [FinancialStatementMappings] SET [CompanyId] = NULL
WHERE [Id] = '681a72d7-516b-6fda-a9aa-faa59cc0bc4e';
SELECT @@ROWCOUNT;

GO

UPDATE [FinancialStatementMappings] SET [CompanyId] = NULL
WHERE [Id] = '87bee6eb-e9c5-98e0-e3c3-9580228dd031';
SELECT @@ROWCOUNT;

GO

UPDATE [FinancialStatementMappings] SET [CompanyId] = NULL
WHERE [Id] = '8b9d58dd-6cac-a0a1-6911-c53349006327';
SELECT @@ROWCOUNT;

GO

UPDATE [FinancialStatementMappings] SET [CompanyId] = NULL
WHERE [Id] = '8e4e72ce-02d5-6fa7-0940-b8272443cc54';
SELECT @@ROWCOUNT;

GO

UPDATE [FinancialStatementMappings] SET [CompanyId] = NULL
WHERE [Id] = '95543140-a7a5-97a2-aa3a-0fb8617e029e';
SELECT @@ROWCOUNT;

GO

UPDATE [FinancialStatementMappings] SET [CompanyId] = NULL
WHERE [Id] = '964494b2-cedd-7c14-8d9e-5b6c74803097';
SELECT @@ROWCOUNT;

GO

UPDATE [FinancialStatementMappings] SET [CompanyId] = NULL
WHERE [Id] = '9bf102f0-400d-d6dd-61de-802b0559b105';
SELECT @@ROWCOUNT;

GO

UPDATE [FinancialStatementMappings] SET [CompanyId] = NULL
WHERE [Id] = 'a8c61726-fe70-dfbf-a3ca-ea80cf36f2d4';
SELECT @@ROWCOUNT;

GO

UPDATE [FinancialStatementMappings] SET [CompanyId] = NULL
WHERE [Id] = 'aed70647-f9d1-5a24-72f0-f9d955f00538';
SELECT @@ROWCOUNT;

GO

UPDATE [FinancialStatementMappings] SET [CompanyId] = NULL
WHERE [Id] = 'b7188cbd-d96b-030b-ae96-22ead1485ce9';
SELECT @@ROWCOUNT;

GO

UPDATE [FinancialStatementMappings] SET [CompanyId] = NULL
WHERE [Id] = 'c7f9d121-abca-c78a-23fd-441cd5af9d6e';
SELECT @@ROWCOUNT;

GO

UPDATE [FinancialStatementMappings] SET [CompanyId] = NULL
WHERE [Id] = 'd16f401a-9c3e-60b9-5e93-8ec4b1f838ac';
SELECT @@ROWCOUNT;

GO

UPDATE [FinancialStatementMappings] SET [CompanyId] = NULL
WHERE [Id] = 'd3228ff9-4a20-e563-3fef-cd5472f0829f';
SELECT @@ROWCOUNT;

GO

UPDATE [FinancialStatementMappings] SET [CompanyId] = NULL
WHERE [Id] = 'd8f4f33e-6371-14fb-d0c2-1529ea42943a';
SELECT @@ROWCOUNT;

GO

UPDATE [FinancialStatementMappings] SET [CompanyId] = NULL
WHERE [Id] = 'dc32a6b1-e3d6-025d-c7da-e53e70974dd3';
SELECT @@ROWCOUNT;

GO

UPDATE [FinancialStatementMappings] SET [CompanyId] = NULL
WHERE [Id] = 'df4ca685-8a95-fa2e-bc7e-499eddc58e9b';
SELECT @@ROWCOUNT;

GO

UPDATE [FinancialStatementMappings] SET [CompanyId] = NULL
WHERE [Id] = 'e9ee0684-7722-ab0b-637c-08f890529513';
SELECT @@ROWCOUNT;

GO

UPDATE [FixedAssetCategories] SET [CompanyId] = NULL
WHERE [Id] = '9a71a150-2a6a-4e72-9b25-000000000001';
SELECT @@ROWCOUNT;

GO

UPDATE [ReportTemplates] SET [CompanyId] = NULL
WHERE [Id] = '4a9a7b4f-ac6e-4e91-9a11-000000000001';
SELECT @@ROWCOUNT;

GO

UPDATE [ReportTemplates] SET [CompanyId] = NULL
WHERE [Id] = '4a9a7b4f-ac6e-4e91-9a11-000000000002';
SELECT @@ROWCOUNT;

GO

UPDATE [ReportTemplates] SET [CompanyId] = NULL
WHERE [Id] = '4a9a7b4f-ac6e-4e91-9a11-000000000003';
SELECT @@ROWCOUNT;

GO

UPDATE [ReportTemplates] SET [CompanyId] = NULL
WHERE [Id] = '4a9a7b4f-ac6e-4e91-9a11-000000000004';
SELECT @@ROWCOUNT;

GO

UPDATE [ReportTemplates] SET [CompanyId] = NULL
WHERE [Id] = '4a9a7b4f-ac6e-4e91-9a11-000000000005';
SELECT @@ROWCOUNT;

GO

UPDATE [ReportTemplates] SET [CompanyId] = NULL
WHERE [Id] = '4a9a7b4f-ac6e-4e91-9a11-000000000006';
SELECT @@ROWCOUNT;

GO

UPDATE [ReportTemplates] SET [CompanyId] = NULL
WHERE [Id] = '4a9a7b4f-ac6e-4e91-9a11-000000000007';
SELECT @@ROWCOUNT;

GO

UPDATE [ReportTemplates] SET [CompanyId] = NULL
WHERE [Id] = '4a9a7b4f-ac6e-4e91-9a11-000000000008';
SELECT @@ROWCOUNT;

GO

UPDATE [ReportTemplates] SET [CompanyId] = NULL
WHERE [Id] = '4a9a7b4f-ac6e-4e91-9a11-000000000009';
SELECT @@ROWCOUNT;

GO

UPDATE [ReportTemplates] SET [CompanyId] = NULL
WHERE [Id] = '4a9a7b4f-ac6e-4e91-9a11-000000000010';
SELECT @@ROWCOUNT;

GO

UPDATE [ReportTemplates] SET [CompanyId] = NULL
WHERE [Id] = '4a9a7b4f-ac6e-4e91-9a11-000000000011';
SELECT @@ROWCOUNT;

GO

UPDATE [TaxCodes] SET [CompanyId] = NULL
WHERE [Id] = 'b7ac6c11-2a40-4d60-9b25-000000000000';
SELECT @@ROWCOUNT;

GO

UPDATE [TaxCodes] SET [CompanyId] = NULL
WHERE [Id] = 'b7ac6c11-2a40-4d60-9b25-000000000005';
SELECT @@ROWCOUNT;

GO

UPDATE [TaxCodes] SET [CompanyId] = NULL
WHERE [Id] = 'b7ac6c11-2a40-4d60-9b25-000000000008';
SELECT @@ROWCOUNT;

GO

UPDATE [TaxCodes] SET [CompanyId] = NULL
WHERE [Id] = 'b7ac6c11-2a40-4d60-9b25-000000000010';
SELECT @@ROWCOUNT;

GO

UPDATE [TaxCodes] SET [CompanyId] = NULL
WHERE [Id] = 'b7ac6c11-2a40-4d60-9b25-000000000999';
SELECT @@ROWCOUNT;

GO

UPDATE [ToolAssetCategories] SET [CompanyId] = NULL
WHERE [Id] = 'a82e9c51-1f1d-4d3f-9b25-000000000001';
SELECT @@ROWCOUNT;

GO

IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Address', N'Code', N'CompanyId', N'CreatedAt', N'IsActive', N'IsDefault', N'Name', N'UpdatedAt') AND [object_id] = OBJECT_ID(N'[Branches]'))
    SET IDENTITY_INSERT [Branches] ON;
INSERT INTO [Branches] ([Id], [Address], [Code], [CompanyId], [CreatedAt], [IsActive], [IsDefault], [Name], [UpdatedAt])
VALUES ('22222222-2222-2222-2222-222222222222', NULL, N'MAIN', '11111111-1111-1111-1111-111111111111', '2026-05-22T00:00:00.0000000Z', CAST(1 AS bit), CAST(1 AS bit), N'Main Branch', '2026-05-22T00:00:00.0000000Z');
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Address', N'Code', N'CompanyId', N'CreatedAt', N'IsActive', N'IsDefault', N'Name', N'UpdatedAt') AND [object_id] = OBJECT_ID(N'[Branches]'))
    SET IDENTITY_INSERT [Branches] OFF;
GO

CREATE UNIQUE INDEX [IX_UserWorkspacePreferences_UserId_Category_PreferenceKey_CompanyId_BranchId] ON [UserWorkspacePreferences] ([UserId], [Category], [PreferenceKey], [CompanyId], [BranchId]) WHERE [CompanyId] IS NOT NULL AND [BranchId] IS NOT NULL;
GO

CREATE UNIQUE INDEX [IX_AppUserPermissions_UserId_PermissionCode_CompanyId_BranchId] ON [AppUserPermissions] ([UserId], [PermissionCode], [CompanyId], [BranchId]) WHERE [CompanyId] IS NOT NULL AND [BranchId] IS NOT NULL;
GO

CREATE UNIQUE INDEX [IX_Branches_CompanyId_Code] ON [Branches] ([CompanyId], [Code]);
GO

CREATE UNIQUE INDEX [IX_Companies_Code] ON [Companies] ([Code]);
GO

CREATE INDEX [IX_UserBranchAccesses_CompanyId_BranchId] ON [UserBranchAccesses] ([CompanyId], [BranchId]);
GO

CREATE UNIQUE INDEX [IX_UserBranchAccesses_UserId_BranchId] ON [UserBranchAccesses] ([UserId], [BranchId]);
GO

CREATE INDEX [IX_UserCompanyAccesses_CompanyId] ON [UserCompanyAccesses] ([CompanyId]);
GO

CREATE UNIQUE INDEX [IX_UserCompanyAccesses_UserId_CompanyId] ON [UserCompanyAccesses] ([UserId], [CompanyId]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260522144345_AddMultiCompanyFoundation', N'8.0.7');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

ALTER TABLE [BackgroundJobExecutions] ADD [CompletedAtUtc] datetime2 NULL;
GO

ALTER TABLE [BackgroundJobExecutions] ADD [ErrorMessage] nvarchar(4000) NULL;
GO

ALTER TABLE [BackgroundJobExecutions] ADD [JobDefinitionId] uniqueidentifier NULL;
GO

ALTER TABLE [BackgroundJobExecutions] ADD [QueuedAtUtc] datetime2 NULL;
GO

ALTER TABLE [BackgroundJobExecutions] ADD [RetryAttempt] int NOT NULL DEFAULT 0;
GO

ALTER TABLE [BackgroundJobExecutions] ADD [StartedAtUtc] datetime2 NULL;
GO

ALTER TABLE [BackgroundJobExecutions] ADD [TriggerSource] nvarchar(40) NOT NULL DEFAULT N'';
GO

CREATE TABLE [AppDataPermissions] (
    [Id] uniqueidentifier NOT NULL,
    [CompanyId] uniqueidentifier NULL,
    [BranchId] uniqueidentifier NULL,
    [SubjectType] nvarchar(30) NOT NULL,
    [SubjectId] uniqueidentifier NOT NULL,
    [ScopeType] nvarchar(40) NOT NULL,
    [ScopeId] uniqueidentifier NOT NULL,
    [IsGranted] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [CreatedBy] nvarchar(255) NULL,
    CONSTRAINT [PK_AppDataPermissions] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [AppRoleInheritances] (
    [Id] uniqueidentifier NOT NULL,
    [RoleId] uniqueidentifier NOT NULL,
    [ParentRoleId] uniqueidentifier NOT NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [CreatedBy] nvarchar(255) NULL,
    CONSTRAINT [PK_AppRoleInheritances] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_AppRoleInheritances_AppRoles_ParentRoleId] FOREIGN KEY ([ParentRoleId]) REFERENCES [AppRoles] ([Id]),
    CONSTRAINT [FK_AppRoleInheritances_AppRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [AppRoles] ([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [BackgroundJobDefinitions] (
    [Id] uniqueidentifier NOT NULL,
    [Code] nvarchar(100) NOT NULL,
    [Name] nvarchar(180) NOT NULL,
    [Description] nvarchar(1000) NOT NULL,
    [IsEnabled] bit NOT NULL,
    [AllowConcurrentExecution] bit NOT NULL,
    [RetryCount] int NOT NULL,
    [TimeoutSeconds] int NOT NULL,
    [CreatedAtUtc] datetime2 NOT NULL,
    [UpdatedAtUtc] datetime2 NOT NULL,
    CONSTRAINT [PK_BackgroundJobDefinitions] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [BackgroundJobLogs] (
    [Id] uniqueidentifier NOT NULL,
    [ExecutionId] uniqueidentifier NOT NULL,
    [TimestampUtc] datetime2 NOT NULL,
    [Severity] nvarchar(30) NOT NULL,
    [Message] nvarchar(2000) NOT NULL,
    [MetadataJson] nvarchar(max) NULL,
    CONSTRAINT [PK_BackgroundJobLogs] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_BackgroundJobLogs_BackgroundJobExecutions_ExecutionId] FOREIGN KEY ([ExecutionId]) REFERENCES [BackgroundJobExecutions] ([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [CompanyBranchPermissions] (
    [Id] uniqueidentifier NOT NULL,
    [CompanyId] uniqueidentifier NULL,
    [BranchId] uniqueidentifier NULL,
    [UserId] uniqueidentifier NOT NULL,
    [PermissionCode] nvarchar(120) NOT NULL,
    [IsGranted] bit NOT NULL,
    [CreatedBy] nvarchar(255) NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_CompanyBranchPermissions] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [FiscalYears] (
    [Id] uniqueidentifier NOT NULL,
    [CompanyId] uniqueidentifier NULL,
    [Year] int NOT NULL,
    [FiscalYearCode] nvarchar(20) NOT NULL,
    [FromDate] datetime2 NOT NULL,
    [ToDate] datetime2 NOT NULL,
    [Status] nvarchar(30) NOT NULL,
    [ClosedAt] datetime2 NULL,
    [ClosedBy] nvarchar(255) NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_FiscalYears] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [InvoiceProviderConfigurations] (
    [Id] uniqueidentifier NOT NULL,
    [CompanyId] uniqueidentifier NULL,
    [BranchId] uniqueidentifier NULL,
    [ProviderCode] nvarchar(30) NOT NULL,
    [DisplayName] nvarchar(120) NOT NULL,
    [EndpointUrl] nvarchar(500) NOT NULL,
    [TenantCode] nvarchar(100) NULL,
    [Username] nvarchar(120) NULL,
    [ProtectedSecret] nvarchar(1000) NULL,
    [IsActive] bit NOT NULL,
    [IsDefault] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_InvoiceProviderConfigurations] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [Invoices] (
    [Id] uniqueidentifier NOT NULL,
    [CompanyId] uniqueidentifier NULL,
    [BranchId] uniqueidentifier NULL,
    [InvoiceNo] nvarchar(100) NOT NULL,
    [InvoiceSeries] nvarchar(50) NOT NULL,
    [TemplateCode] nvarchar(50) NOT NULL,
    [InvoiceDate] datetime2 NOT NULL,
    [BuyerName] nvarchar(255) NOT NULL,
    [BuyerTaxCode] nvarchar(50) NULL,
    [BuyerAddress] nvarchar(500) NULL,
    [CurrencyCode] nvarchar(10) NOT NULL DEFAULT N'VND',
    [ExchangeRate] decimal(18,6) NOT NULL,
    [SubtotalAmount] decimal(18,2) NOT NULL,
    [TaxAmount] decimal(18,2) NOT NULL,
    [TotalAmount] decimal(18,2) NOT NULL,
    [Status] nvarchar(30) NOT NULL,
    [ProviderCode] nvarchar(30) NOT NULL,
    [ProviderInvoiceId] nvarchar(120) NULL,
    [JournalHeaderId] uniqueidentifier NULL,
    [ReceivableTransactionId] uniqueidentifier NULL,
    [PayableTransactionId] uniqueidentifier NULL,
    [VatDeclarationDraftId] uniqueidentifier NULL,
    [XmlPayload] nvarchar(max) NULL,
    [PdfPreviewPath] nvarchar(max) NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    [IssuedAt] datetime2 NULL,
    CONSTRAINT [PK_Invoices] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Invoices_JournalHeaders_JournalHeaderId] FOREIGN KEY ([JournalHeaderId]) REFERENCES [JournalHeaders] ([Id]),
    CONSTRAINT [FK_Invoices_PayableTransactions_PayableTransactionId] FOREIGN KEY ([PayableTransactionId]) REFERENCES [PayableTransactions] ([Id]),
    CONSTRAINT [FK_Invoices_ReceivableTransactions_ReceivableTransactionId] FOREIGN KEY ([ReceivableTransactionId]) REFERENCES [ReceivableTransactions] ([Id]),
    CONSTRAINT [FK_Invoices_TaxDeclarationDrafts_VatDeclarationDraftId] FOREIGN KEY ([VatDeclarationDraftId]) REFERENCES [TaxDeclarationDrafts] ([Id])
);
GO

CREATE TABLE [JobDefinitions] (
    [Id] uniqueidentifier NOT NULL,
    [CompanyId] uniqueidentifier NULL,
    [BranchId] uniqueidentifier NULL,
    [JobCode] nvarchar(100) NOT NULL,
    [JobName] nvarchar(180) NOT NULL,
    [JobType] nvarchar(80) NOT NULL,
    [ScheduleType] nvarchar(30) NOT NULL,
    [IntervalMinutes] int NULL,
    [CronExpression] nvarchar(120) NULL,
    [IsActive] bit NOT NULL,
    [NextRunAt] datetime2 NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_JobDefinitions] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [PeriodLocks] (
    [Id] uniqueidentifier NOT NULL,
    [CompanyId] uniqueidentifier NULL,
    [BranchId] uniqueidentifier NULL,
    [PeriodCode] nvarchar(20) NOT NULL,
    [IsLocked] bit NOT NULL,
    [LockedBy] nvarchar(255) NOT NULL,
    [LockedAt] datetime2 NOT NULL,
    [LockReason] nvarchar(1000) NULL,
    [UnlockedBy] nvarchar(255) NULL,
    [UnlockedAt] datetime2 NULL,
    [UnlockReason] nvarchar(1000) NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_PeriodLocks] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [RecentSearches] (
    [Id] uniqueidentifier NOT NULL,
    [CompanyId] uniqueidentifier NULL,
    [BranchId] uniqueidentifier NULL,
    [UserId] uniqueidentifier NULL,
    [Query] nvarchar(255) NOT NULL,
    [SelectedEntityType] nvarchar(80) NULL,
    [SelectedEntityId] nvarchar(120) NULL,
    [SearchedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_RecentSearches] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [SearchIndexEntries] (
    [Id] uniqueidentifier NOT NULL,
    [CompanyId] uniqueidentifier NULL,
    [BranchId] uniqueidentifier NULL,
    [EntityType] nvarchar(80) NOT NULL,
    [EntityId] nvarchar(120) NOT NULL,
    [Title] nvarchar(255) NOT NULL,
    [Subtitle] nvarchar(500) NOT NULL,
    [Category] nvarchar(80) NOT NULL,
    [TargetType] nvarchar(40) NOT NULL,
    [TargetRoute] nvarchar(120) NOT NULL,
    [TargetGuid] uniqueidentifier NULL,
    [SourceType] nvarchar(80) NULL,
    [SearchText] nvarchar(2000) NOT NULL,
    [BaseScore] int NOT NULL,
    [IsActive] bit NOT NULL,
    [IndexedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_SearchIndexEntries] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [TaskInboxItems] (
    [Id] uniqueidentifier NOT NULL,
    [CompanyId] uniqueidentifier NULL,
    [BranchId] uniqueidentifier NULL,
    [Title] nvarchar(255) NOT NULL,
    [Description] nvarchar(4000) NOT NULL,
    [Status] nvarchar(40) NOT NULL,
    [Priority] nvarchar(30) NOT NULL,
    [AssignedToUserId] uniqueidentifier NULL,
    [CreatedByUserId] uniqueidentifier NULL,
    [SourceType] nvarchar(80) NULL,
    [SourceId] uniqueidentifier NULL,
    [TargetRoute] nvarchar(120) NULL,
    [DueAt] datetime2 NULL,
    [CompletedAt] datetime2 NULL,
    [ReopenedAt] datetime2 NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_TaskInboxItems] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [UserNotifications] (
    [Id] uniqueidentifier NOT NULL,
    [CompanyId] uniqueidentifier NULL,
    [BranchId] uniqueidentifier NULL,
    [UserId] uniqueidentifier NULL,
    [NotificationType] nvarchar(80) NOT NULL,
    [Severity] nvarchar(30) NOT NULL,
    [Title] nvarchar(255) NOT NULL,
    [Message] nvarchar(2000) NOT NULL,
    [SourceType] nvarchar(80) NULL,
    [SourceId] uniqueidentifier NULL,
    [TargetRoute] nvarchar(120) NULL,
    [MetadataJson] nvarchar(max) NULL,
    [IsRead] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [ReadAt] datetime2 NULL,
    CONSTRAINT [PK_UserNotifications] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [BackgroundJobSchedules] (
    [Id] uniqueidentifier NOT NULL,
    [JobDefinitionId] uniqueidentifier NOT NULL,
    [CronExpression] nvarchar(120) NULL,
    [IntervalMinutes] int NULL,
    [NextRunAtUtc] datetime2 NULL,
    [LastRunAtUtc] datetime2 NULL,
    [IsPaused] bit NOT NULL,
    CONSTRAINT [PK_BackgroundJobSchedules] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_BackgroundJobSchedules_BackgroundJobDefinitions_JobDefinitionId] FOREIGN KEY ([JobDefinitionId]) REFERENCES [BackgroundJobDefinitions] ([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [InvoiceIssueLogs] (
    [Id] uniqueidentifier NOT NULL,
    [CompanyId] uniqueidentifier NULL,
    [BranchId] uniqueidentifier NULL,
    [InvoiceId] uniqueidentifier NOT NULL,
    [ProviderCode] nvarchar(30) NOT NULL,
    [Status] nvarchar(30) NOT NULL,
    [AttemptNo] int NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [CompletedAt] datetime2 NULL,
    [RequestXml] nvarchar(max) NULL,
    [ResponsePayload] nvarchar(max) NULL,
    [ErrorMessage] nvarchar(2000) NULL,
    [CorrelationId] nvarchar(64) NOT NULL,
    CONSTRAINT [PK_InvoiceIssueLogs] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_InvoiceIssueLogs_Invoices_InvoiceId] FOREIGN KEY ([InvoiceId]) REFERENCES [Invoices] ([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [InvoiceLines] (
    [Id] uniqueidentifier NOT NULL,
    [CompanyId] uniqueidentifier NULL,
    [BranchId] uniqueidentifier NULL,
    [InvoiceId] uniqueidentifier NOT NULL,
    [LineNo] int NOT NULL,
    [ItemCode] nvarchar(100) NOT NULL,
    [ItemName] nvarchar(255) NOT NULL,
    [UnitName] nvarchar(50) NOT NULL,
    [Quantity] decimal(18,4) NOT NULL,
    [UnitPrice] decimal(18,6) NOT NULL,
    [LineAmount] decimal(18,2) NOT NULL,
    [TaxRate] decimal(9,4) NOT NULL,
    [TaxAmount] decimal(18,2) NOT NULL,
    [JournalAccountCode] nvarchar(50) NULL,
    CONSTRAINT [PK_InvoiceLines] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_InvoiceLines_Invoices_InvoiceId] FOREIGN KEY ([InvoiceId]) REFERENCES [Invoices] ([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [InvoiceTaxBreakdowns] (
    [Id] uniqueidentifier NOT NULL,
    [CompanyId] uniqueidentifier NULL,
    [BranchId] uniqueidentifier NULL,
    [InvoiceId] uniqueidentifier NOT NULL,
    [TaxRate] decimal(9,4) NOT NULL,
    [TaxBaseAmount] decimal(18,2) NOT NULL,
    [TaxAmount] decimal(18,2) NOT NULL,
    [TaxCode] nvarchar(50) NOT NULL,
    CONSTRAINT [PK_InvoiceTaxBreakdowns] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_InvoiceTaxBreakdowns_Invoices_InvoiceId] FOREIGN KEY ([InvoiceId]) REFERENCES [Invoices] ([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [JobExecutions] (
    [Id] uniqueidentifier NOT NULL,
    [CompanyId] uniqueidentifier NULL,
    [BranchId] uniqueidentifier NULL,
    [JobDefinitionId] uniqueidentifier NOT NULL,
    [JobCode] nvarchar(100) NOT NULL,
    [Status] nvarchar(40) NOT NULL,
    [QueuedAt] datetime2 NOT NULL,
    [StartedAt] datetime2 NULL,
    [CompletedAt] datetime2 NULL,
    [DurationMs] bigint NOT NULL,
    [CancellationRequested] bit NOT NULL,
    [TriggeredBy] nvarchar(255) NULL,
    [CorrelationId] nvarchar(64) NOT NULL,
    [ErrorMessage] nvarchar(4000) NULL,
    CONSTRAINT [PK_JobExecutions] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_JobExecutions_JobDefinitions_JobDefinitionId] FOREIGN KEY ([JobDefinitionId]) REFERENCES [JobDefinitions] ([Id])
);
GO

CREATE TABLE [TaskAttachmentMetadata] (
    [Id] uniqueidentifier NOT NULL,
    [CompanyId] uniqueidentifier NULL,
    [BranchId] uniqueidentifier NULL,
    [TaskInboxId] uniqueidentifier NOT NULL,
    [FileName] nvarchar(260) NOT NULL,
    [ContentType] nvarchar(120) NULL,
    [StorageProvider] nvarchar(40) NOT NULL,
    [StorageKey] nvarchar(500) NOT NULL,
    [SizeBytes] bigint NOT NULL,
    [CreatedByUserId] uniqueidentifier NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_TaskAttachmentMetadata] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_TaskAttachmentMetadata_TaskInboxItems_TaskInboxId] FOREIGN KEY ([TaskInboxId]) REFERENCES [TaskInboxItems] ([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [TaskComments] (
    [Id] uniqueidentifier NOT NULL,
    [CompanyId] uniqueidentifier NULL,
    [BranchId] uniqueidentifier NULL,
    [TaskInboxId] uniqueidentifier NOT NULL,
    [UserId] uniqueidentifier NULL,
    [CommentText] nvarchar(4000) NOT NULL,
    [MentionUserIdsJson] nvarchar(max) NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_TaskComments] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_TaskComments_TaskInboxItems_TaskInboxId] FOREIGN KEY ([TaskInboxId]) REFERENCES [TaskInboxItems] ([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [JobLogs] (
    [Id] uniqueidentifier NOT NULL,
    [CompanyId] uniqueidentifier NULL,
    [BranchId] uniqueidentifier NULL,
    [JobExecutionId] uniqueidentifier NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [Level] nvarchar(30) NOT NULL,
    [Message] nvarchar(2000) NOT NULL,
    CONSTRAINT [PK_JobLogs] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_JobLogs_JobExecutions_JobExecutionId] FOREIGN KEY ([JobExecutionId]) REFERENCES [JobExecutions] ([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [JobRetries] (
    [Id] uniqueidentifier NOT NULL,
    [CompanyId] uniqueidentifier NULL,
    [BranchId] uniqueidentifier NULL,
    [JobExecutionId] uniqueidentifier NOT NULL,
    [AttemptNo] int NOT NULL,
    [ScheduledAt] datetime2 NOT NULL,
    [ExecutedAt] datetime2 NULL,
    [Status] nvarchar(40) NOT NULL,
    [ErrorMessage] nvarchar(4000) NULL,
    CONSTRAINT [PK_JobRetries] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_JobRetries_JobExecutions_JobExecutionId] FOREIGN KEY ([JobExecutionId]) REFERENCES [JobExecutions] ([Id]) ON DELETE CASCADE
);
GO

CREATE INDEX [IX_VatTransactions_BranchId] ON [VatTransactions] ([BranchId]);
GO

CREATE INDEX [IX_VatTransactions_CompanyId] ON [VatTransactions] ([CompanyId]);
GO

CREATE INDEX [IX_VatTransactions_SourceType] ON [VatTransactions] ([SourceType]);
GO

CREATE INDEX [IX_StockReceipts_BranchId] ON [StockReceipts] ([BranchId]);
GO

CREATE INDEX [IX_StockReceipts_CompanyId] ON [StockReceipts] ([CompanyId]);
GO

CREATE INDEX [IX_StockReceipts_CompanyId_BranchId_PostingStatus_ReceiptDate] ON [StockReceipts] ([CompanyId], [BranchId], [PostingStatus], [ReceiptDate]);
GO

CREATE INDEX [IX_StockReceipts_PostingStatus] ON [StockReceipts] ([PostingStatus]);
GO

CREATE INDEX [IX_StockIssues_BranchId] ON [StockIssues] ([BranchId]);
GO

CREATE INDEX [IX_StockIssues_CompanyId] ON [StockIssues] ([CompanyId]);
GO

CREATE INDEX [IX_StockIssues_CompanyId_BranchId_PostingStatus_IssueDate] ON [StockIssues] ([CompanyId], [BranchId], [PostingStatus], [IssueDate]);
GO

CREATE INDEX [IX_StockIssues_PostingStatus] ON [StockIssues] ([PostingStatus]);
GO

CREATE INDEX [IX_ReportExecutions_BranchId] ON [ReportExecutions] ([BranchId]);
GO

CREATE INDEX [IX_ReportExecutions_CompanyId] ON [ReportExecutions] ([CompanyId]);
GO

CREATE INDEX [IX_ReportExecutions_CompanyId_BranchId_ReportCode_StartedAt] ON [ReportExecutions] ([CompanyId], [BranchId], [ReportCode], [StartedAt]);
GO

CREATE INDEX [IX_ReceivableTransactions_BranchId] ON [ReceivableTransactions] ([BranchId]);
GO

CREATE INDEX [IX_ReceivableTransactions_CompanyId] ON [ReceivableTransactions] ([CompanyId]);
GO

CREATE INDEX [IX_ReceivableTransactions_CompanyId_BranchId_Status_DocumentDate] ON [ReceivableTransactions] ([CompanyId], [BranchId], [Status], [DocumentDate]);
GO

CREATE INDEX [IX_ReceivableTransactions_SourceType] ON [ReceivableTransactions] ([SourceType]);
GO

CREATE INDEX [IX_PayableTransactions_BranchId] ON [PayableTransactions] ([BranchId]);
GO

CREATE INDEX [IX_PayableTransactions_CompanyId] ON [PayableTransactions] ([CompanyId]);
GO

CREATE INDEX [IX_PayableTransactions_CompanyId_BranchId_Status_DocumentDate] ON [PayableTransactions] ([CompanyId], [BranchId], [Status], [DocumentDate]);
GO

CREATE INDEX [IX_PayableTransactions_SourceType] ON [PayableTransactions] ([SourceType]);
GO

CREATE INDEX [IX_JournalLines_BranchId] ON [JournalLines] ([BranchId]);
GO

CREATE INDEX [IX_JournalLines_CompanyId] ON [JournalLines] ([CompanyId]);
GO

CREATE INDEX [IX_JournalLines_CompanyId_BranchId_AccountCode] ON [JournalLines] ([CompanyId], [BranchId], [AccountCode]);
GO

CREATE INDEX [IX_JournalHeaders_BranchId] ON [JournalHeaders] ([BranchId]);
GO

CREATE INDEX [IX_JournalHeaders_CompanyId] ON [JournalHeaders] ([CompanyId]);
GO

CREATE INDEX [IX_JournalHeaders_CompanyId_BranchId_Status_PostingDate] ON [JournalHeaders] ([CompanyId], [BranchId], [Status], [PostingDate]);
GO

CREATE INDEX [IX_JournalHeaders_JournalDate] ON [JournalHeaders] ([JournalDate]);
GO

CREATE INDEX [IX_JournalHeaders_SourceType] ON [JournalHeaders] ([SourceType]);
GO

CREATE INDEX [IX_JournalHeaders_Status] ON [JournalHeaders] ([Status]);
GO

CREATE INDEX [IX_InventoryCostTransactions_BranchId] ON [InventoryCostTransactions] ([BranchId]);
GO

CREATE INDEX [IX_InventoryCostTransactions_CompanyId] ON [InventoryCostTransactions] ([CompanyId]);
GO

CREATE INDEX [IX_InventoryCostTransactions_CompanyId_BranchId_DocumentDate] ON [InventoryCostTransactions] ([CompanyId], [BranchId], [DocumentDate]);
GO

CREATE INDEX [IX_InventoryCostTransactions_SourceType] ON [InventoryCostTransactions] ([SourceType]);
GO

CREATE INDEX [IX_InventoryCostLayers_BranchId] ON [InventoryCostLayers] ([BranchId]);
GO

CREATE INDEX [IX_InventoryCostLayers_CompanyId] ON [InventoryCostLayers] ([CompanyId]);
GO

CREATE INDEX [IX_InventoryCostLayers_CompanyId_BranchId_Status_DocumentDate] ON [InventoryCostLayers] ([CompanyId], [BranchId], [Status], [DocumentDate]);
GO

CREATE INDEX [IX_InventoryCostLayers_SourceType] ON [InventoryCostLayers] ([SourceType]);
GO

CREATE INDEX [IX_BackgroundJobExecutions_JobDefinitionId] ON [BackgroundJobExecutions] ([JobDefinitionId]);
GO

CREATE INDEX [IX_BackgroundJobExecutions_QueuedAtUtc] ON [BackgroundJobExecutions] ([QueuedAtUtc]);
GO

CREATE INDEX [IX_BackgroundJobExecutions_Status_StartedAtUtc] ON [BackgroundJobExecutions] ([Status], [StartedAtUtc]);
GO

CREATE INDEX [IX_AuditLogs_BranchId] ON [AuditLogs] ([BranchId]);
GO

CREATE INDEX [IX_AuditLogs_CompanyId] ON [AuditLogs] ([CompanyId]);
GO

CREATE INDEX [IX_AuditLogs_CompanyId_BranchId_CreatedAt] ON [AuditLogs] ([CompanyId], [BranchId], [CreatedAt]);
GO

CREATE INDEX [IX_AppDataPermissions_BranchId] ON [AppDataPermissions] ([BranchId]);
GO

CREATE INDEX [IX_AppDataPermissions_CompanyId] ON [AppDataPermissions] ([CompanyId]);
GO

CREATE INDEX [IX_AppDataPermissions_ScopeType_ScopeId] ON [AppDataPermissions] ([ScopeType], [ScopeId]);
GO

CREATE UNIQUE INDEX [IX_AppDataPermissions_SubjectType_SubjectId_ScopeType_ScopeId_CompanyId_BranchId] ON [AppDataPermissions] ([SubjectType], [SubjectId], [ScopeType], [ScopeId], [CompanyId], [BranchId]) WHERE [CompanyId] IS NOT NULL AND [BranchId] IS NOT NULL;
GO

CREATE INDEX [IX_AppRoleInheritances_ParentRoleId] ON [AppRoleInheritances] ([ParentRoleId]);
GO

CREATE UNIQUE INDEX [IX_AppRoleInheritances_RoleId_ParentRoleId] ON [AppRoleInheritances] ([RoleId], [ParentRoleId]);
GO

CREATE UNIQUE INDEX [IX_BackgroundJobDefinitions_Code] ON [BackgroundJobDefinitions] ([Code]);
GO

CREATE INDEX [IX_BackgroundJobLogs_ExecutionId] ON [BackgroundJobLogs] ([ExecutionId]);
GO

CREATE INDEX [IX_BackgroundJobLogs_TimestampUtc] ON [BackgroundJobLogs] ([TimestampUtc]);
GO

CREATE INDEX [IX_BackgroundJobSchedules_IsPaused_NextRunAtUtc] ON [BackgroundJobSchedules] ([IsPaused], [NextRunAtUtc]);
GO

CREATE UNIQUE INDEX [IX_BackgroundJobSchedules_JobDefinitionId] ON [BackgroundJobSchedules] ([JobDefinitionId]);
GO

CREATE INDEX [IX_CompanyBranchPermissions_CompanyId_BranchId] ON [CompanyBranchPermissions] ([CompanyId], [BranchId]);
GO

CREATE UNIQUE INDEX [IX_CompanyBranchPermissions_UserId_CompanyId_BranchId_PermissionCode] ON [CompanyBranchPermissions] ([UserId], [CompanyId], [BranchId], [PermissionCode]) WHERE [CompanyId] IS NOT NULL;
GO

CREATE UNIQUE INDEX [IX_FiscalYears_CompanyId_Year] ON [FiscalYears] ([CompanyId], [Year]) WHERE [CompanyId] IS NOT NULL;
GO

CREATE INDEX [IX_FiscalYears_FiscalYearCode] ON [FiscalYears] ([FiscalYearCode]);
GO

CREATE INDEX [IX_InvoiceIssueLogs_CorrelationId] ON [InvoiceIssueLogs] ([CorrelationId]);
GO

CREATE INDEX [IX_InvoiceIssueLogs_InvoiceId] ON [InvoiceIssueLogs] ([InvoiceId]);
GO

CREATE INDEX [IX_InvoiceIssueLogs_Status] ON [InvoiceIssueLogs] ([Status]);
GO

CREATE INDEX [IX_InvoiceLines_InvoiceId] ON [InvoiceLines] ([InvoiceId]);
GO

CREATE INDEX [IX_InvoiceLines_ItemCode] ON [InvoiceLines] ([ItemCode]);
GO

CREATE UNIQUE INDEX [IX_InvoiceProviderConfigurations_CompanyId_BranchId_ProviderCode] ON [InvoiceProviderConfigurations] ([CompanyId], [BranchId], [ProviderCode]) WHERE [CompanyId] IS NOT NULL AND [BranchId] IS NOT NULL;
GO

CREATE INDEX [IX_InvoiceProviderConfigurations_ProviderCode] ON [InvoiceProviderConfigurations] ([ProviderCode]);
GO

CREATE INDEX [IX_Invoices_CompanyId_BranchId_Status_InvoiceDate] ON [Invoices] ([CompanyId], [BranchId], [Status], [InvoiceDate]);
GO

CREATE UNIQUE INDEX [IX_Invoices_InvoiceNo] ON [Invoices] ([InvoiceNo]);
GO

CREATE INDEX [IX_Invoices_JournalHeaderId] ON [Invoices] ([JournalHeaderId]);
GO

CREATE INDEX [IX_Invoices_PayableTransactionId] ON [Invoices] ([PayableTransactionId]);
GO

CREATE INDEX [IX_Invoices_ProviderCode] ON [Invoices] ([ProviderCode]);
GO

CREATE INDEX [IX_Invoices_ReceivableTransactionId] ON [Invoices] ([ReceivableTransactionId]);
GO

CREATE INDEX [IX_Invoices_Status] ON [Invoices] ([Status]);
GO

CREATE INDEX [IX_Invoices_VatDeclarationDraftId] ON [Invoices] ([VatDeclarationDraftId]);
GO

CREATE INDEX [IX_InvoiceTaxBreakdowns_InvoiceId] ON [InvoiceTaxBreakdowns] ([InvoiceId]);
GO

CREATE INDEX [IX_JobDefinitions_IsActive] ON [JobDefinitions] ([IsActive]);
GO

CREATE UNIQUE INDEX [IX_JobDefinitions_JobCode] ON [JobDefinitions] ([JobCode]);
GO

CREATE INDEX [IX_JobDefinitions_NextRunAt] ON [JobDefinitions] ([NextRunAt]);
GO

CREATE INDEX [IX_JobExecutions_JobCode] ON [JobExecutions] ([JobCode]);
GO

CREATE INDEX [IX_JobExecutions_JobDefinitionId] ON [JobExecutions] ([JobDefinitionId]);
GO

CREATE INDEX [IX_JobExecutions_QueuedAt] ON [JobExecutions] ([QueuedAt]);
GO

CREATE INDEX [IX_JobExecutions_Status] ON [JobExecutions] ([Status]);
GO

CREATE INDEX [IX_JobLogs_CreatedAt] ON [JobLogs] ([CreatedAt]);
GO

CREATE INDEX [IX_JobLogs_JobExecutionId] ON [JobLogs] ([JobExecutionId]);
GO

CREATE INDEX [IX_JobRetries_JobExecutionId] ON [JobRetries] ([JobExecutionId]);
GO

CREATE INDEX [IX_JobRetries_Status] ON [JobRetries] ([Status]);
GO

CREATE INDEX [IX_PeriodLocks_BranchId] ON [PeriodLocks] ([BranchId]);
GO

CREATE UNIQUE INDEX [IX_PeriodLocks_CompanyId_BranchId_PeriodCode] ON [PeriodLocks] ([CompanyId], [BranchId], [PeriodCode]) WHERE [CompanyId] IS NOT NULL;
GO

CREATE INDEX [IX_PeriodLocks_CompanyId_PeriodCode_IsLocked] ON [PeriodLocks] ([CompanyId], [PeriodCode], [IsLocked]);
GO

CREATE INDEX [IX_RecentSearches_UserId_CompanyId_BranchId_SearchedAt] ON [RecentSearches] ([UserId], [CompanyId], [BranchId], [SearchedAt]);
GO

CREATE INDEX [IX_SearchIndexEntries_CompanyId_BranchId_Category] ON [SearchIndexEntries] ([CompanyId], [BranchId], [Category]);
GO

CREATE UNIQUE INDEX [IX_SearchIndexEntries_EntityType_EntityId_CompanyId_BranchId] ON [SearchIndexEntries] ([EntityType], [EntityId], [CompanyId], [BranchId]) WHERE [CompanyId] IS NOT NULL AND [BranchId] IS NOT NULL;
GO

CREATE INDEX [IX_SearchIndexEntries_IsActive] ON [SearchIndexEntries] ([IsActive]);
GO

CREATE INDEX [IX_SearchIndexEntries_TargetRoute] ON [SearchIndexEntries] ([TargetRoute]);
GO

CREATE INDEX [IX_TaskAttachmentMetadata_TaskInboxId] ON [TaskAttachmentMetadata] ([TaskInboxId]);
GO

CREATE INDEX [IX_TaskComments_TaskInboxId] ON [TaskComments] ([TaskInboxId]);
GO

CREATE INDEX [IX_TaskInboxItems_AssignedToUserId_Status_UpdatedAt] ON [TaskInboxItems] ([AssignedToUserId], [Status], [UpdatedAt]);
GO

CREATE INDEX [IX_TaskInboxItems_CompanyId_BranchId_Status] ON [TaskInboxItems] ([CompanyId], [BranchId], [Status]);
GO

CREATE INDEX [IX_TaskInboxItems_SourceType_SourceId] ON [TaskInboxItems] ([SourceType], [SourceId]);
GO

CREATE INDEX [IX_UserNotifications_CompanyId_BranchId_CreatedAt] ON [UserNotifications] ([CompanyId], [BranchId], [CreatedAt]);
GO

CREATE INDEX [IX_UserNotifications_NotificationType] ON [UserNotifications] ([NotificationType]);
GO

CREATE INDEX [IX_UserNotifications_UserId_IsRead_CreatedAt] ON [UserNotifications] ([UserId], [IsRead], [CreatedAt]);
GO

ALTER TABLE [BackgroundJobExecutions] ADD CONSTRAINT [FK_BackgroundJobExecutions_BackgroundJobDefinitions_JobDefinitionId] FOREIGN KEY ([JobDefinitionId]) REFERENCES [BackgroundJobDefinitions] ([Id]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260523042803_AddBackgroundSchedulerEngine', N'8.0.7');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

CREATE TABLE [AuditCorrelations] (
    [Id] uniqueidentifier NOT NULL,
    [CorrelationId] nvarchar(100) NOT NULL,
    [EntityType] nvarchar(120) NOT NULL,
    [EntityId] nvarchar(120) NOT NULL,
    [Module] nvarchar(120) NOT NULL,
    [Action] nvarchar(120) NOT NULL,
    [CreatedAtUtc] datetime2 NOT NULL,
    CONSTRAINT [PK_AuditCorrelations] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [AuditEntitySnapshots] (
    [Id] uniqueidentifier NOT NULL,
    [AuditLogId] int NOT NULL,
    [EntityType] nvarchar(120) NOT NULL,
    [EntityId] nvarchar(120) NOT NULL,
    [BeforeJson] nvarchar(max) NULL,
    [AfterJson] nvarchar(max) NULL,
    [DiffJson] nvarchar(max) NULL,
    [SnapshotHash] nvarchar(64) NOT NULL,
    CONSTRAINT [PK_AuditEntitySnapshots] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_AuditEntitySnapshots_AuditLogs_AuditLogId] FOREIGN KEY ([AuditLogId]) REFERENCES [AuditLogs] ([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [AuditExportHistory] (
    [Id] uniqueidentifier NOT NULL,
    [RequestedByUserId] uniqueidentifier NULL,
    [CompanyId] uniqueidentifier NULL,
    [BranchId] uniqueidentifier NULL,
    [ExportType] nvarchar(30) NOT NULL,
    [FilterJson] nvarchar(max) NULL,
    [FilePath] nvarchar(1000) NOT NULL,
    [CreatedAtUtc] datetime2 NOT NULL,
    CONSTRAINT [PK_AuditExportHistory] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [AuditReplayEntries] (
    [Id] uniqueidentifier NOT NULL,
    [CorrelationId] nvarchar(100) NOT NULL,
    [SequenceNo] int NOT NULL,
    [Route] nvarchar(180) NOT NULL,
    [Module] nvarchar(120) NOT NULL,
    [Action] nvarchar(120) NOT NULL,
    [EntityType] nvarchar(120) NOT NULL,
    [EntityId] nvarchar(120) NOT NULL,
    [MetadataJson] nvarchar(max) NULL,
    [TimestampUtc] datetime2 NOT NULL,
    CONSTRAINT [PK_AuditReplayEntries] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [AuditSessions] (
    [Id] uniqueidentifier NOT NULL,
    [CorrelationId] nvarchar(100) NOT NULL,
    [UserId] uniqueidentifier NULL,
    [CompanyId] uniqueidentifier NULL,
    [BranchId] uniqueidentifier NULL,
    [StartedAtUtc] datetime2 NOT NULL,
    [CompletedAtUtc] datetime2 NULL,
    [Source] nvarchar(120) NOT NULL,
    [ClientMachine] nvarchar(120) NULL,
    [AppVersion] nvarchar(60) NULL,
    [SessionMetadataJson] nvarchar(max) NULL,
    CONSTRAINT [PK_AuditSessions] PRIMARY KEY ([Id])
);
GO

CREATE INDEX [IX_AuditCorrelations_CorrelationId] ON [AuditCorrelations] ([CorrelationId]);
GO

CREATE INDEX [IX_AuditCorrelations_EntityType_EntityId] ON [AuditCorrelations] ([EntityType], [EntityId]);
GO

CREATE INDEX [IX_AuditCorrelations_Module_Action] ON [AuditCorrelations] ([Module], [Action]);
GO

CREATE INDEX [IX_AuditEntitySnapshots_AuditLogId] ON [AuditEntitySnapshots] ([AuditLogId]);
GO

CREATE INDEX [IX_AuditEntitySnapshots_EntityType_EntityId] ON [AuditEntitySnapshots] ([EntityType], [EntityId]);
GO

CREATE INDEX [IX_AuditExportHistory_CompanyId_BranchId_CreatedAtUtc] ON [AuditExportHistory] ([CompanyId], [BranchId], [CreatedAtUtc]);
GO

CREATE INDEX [IX_AuditExportHistory_CreatedAtUtc] ON [AuditExportHistory] ([CreatedAtUtc]);
GO

CREATE INDEX [IX_AuditReplayEntries_CorrelationId] ON [AuditReplayEntries] ([CorrelationId]);
GO

CREATE UNIQUE INDEX [IX_AuditReplayEntries_CorrelationId_SequenceNo] ON [AuditReplayEntries] ([CorrelationId], [SequenceNo]);
GO

CREATE INDEX [IX_AuditReplayEntries_EntityType_EntityId] ON [AuditReplayEntries] ([EntityType], [EntityId]);
GO

CREATE INDEX [IX_AuditReplayEntries_Module_Action] ON [AuditReplayEntries] ([Module], [Action]);
GO

CREATE INDEX [IX_AuditReplayEntries_TimestampUtc] ON [AuditReplayEntries] ([TimestampUtc]);
GO

CREATE INDEX [IX_AuditSessions_CompanyId_BranchId_StartedAtUtc] ON [AuditSessions] ([CompanyId], [BranchId], [StartedAtUtc]);
GO

CREATE UNIQUE INDEX [IX_AuditSessions_CorrelationId] ON [AuditSessions] ([CorrelationId]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260523053846_EnhanceEnterpriseAuditTrail', N'8.0.7');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

ALTER TABLE [AuditEntitySnapshots] DROP CONSTRAINT [FK_AuditEntitySnapshots_AuditLogs_AuditLogId];
GO

ALTER TABLE [AuditSessions] ADD [CompletedReason] nvarchar(200) NULL;
GO

ALTER TABLE [AuditSessions] ADD [DeviceId] nvarchar(120) NULL;
GO

ALTER TABLE [AuditSessions] ADD [DurationMs] bigint NOT NULL DEFAULT CAST(0 AS bigint);
GO

ALTER TABLE [AuditSessions] ADD [LastActivityAtUtc] datetime2 NOT NULL DEFAULT '0001-01-01T00:00:00.0000000';
GO

ALTER TABLE [AuditSessions] ADD [Platform] nvarchar(80) NULL;
GO

ALTER TABLE [AuditSessions] ADD [Status] nvarchar(40) NOT NULL DEFAULT N'Started';
GO

ALTER TABLE [AuditReplayEntries] ADD [AuditSessionId] uniqueidentifier NULL;
GO

ALTER TABLE [AuditReplayEntries] ADD [ParentCorrelationId] nvarchar(100) NULL;
GO

ALTER TABLE [AuditExportHistory] ADD [CorrelationCoverageJson] nvarchar(max) NULL;
GO

ALTER TABLE [AuditExportHistory] ADD [ExportHash] nvarchar(64) NULL;
GO

ALTER TABLE [AuditExportHistory] ADD [HashAlgorithm] nvarchar(20) NOT NULL DEFAULT N'SHA256';
GO

ALTER TABLE [AuditExportHistory] ADD [IntegrityManifestJson] nvarchar(max) NULL;
GO

ALTER TABLE [AuditExportHistory] ADD [RowCount] int NOT NULL DEFAULT 0;
GO

DECLARE @var0 sysname;
SELECT @var0 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[AuditEntitySnapshots]') AND [c].[name] = N'AuditLogId');
IF @var0 IS NOT NULL EXEC(N'ALTER TABLE [AuditEntitySnapshots] DROP CONSTRAINT [' + @var0 + '];');
ALTER TABLE [AuditEntitySnapshots] ALTER COLUMN [AuditLogId] int NULL;
GO

ALTER TABLE [AuditEntitySnapshots] ADD [BranchId] uniqueidentifier NULL;
GO

ALTER TABLE [AuditEntitySnapshots] ADD [CompanyId] uniqueidentifier NULL;
GO

ALTER TABLE [AuditEntitySnapshots] ADD [CompressedPayload] varbinary(max) NULL;
GO

ALTER TABLE [AuditEntitySnapshots] ADD [CompressionType] nvarchar(20) NOT NULL DEFAULT N'None';
GO

ALTER TABLE [AuditEntitySnapshots] ADD [HashAlgorithm] nvarchar(20) NOT NULL DEFAULT N'SHA256';
GO

ALTER TABLE [AuditEntitySnapshots] ADD [SnapshotSize] bigint NOT NULL DEFAULT CAST(0 AS bigint);
GO

ALTER TABLE [AuditCorrelations] ADD [AuditSessionId] uniqueidentifier NULL;
GO

ALTER TABLE [AuditCorrelations] ADD [ParentCorrelationId] nvarchar(100) NULL;
GO

UPDATE AuditSessions
SET LastActivityAtUtc = CASE WHEN CompletedAtUtc IS NULL THEN StartedAtUtc ELSE CompletedAtUtc END,
    Status = CASE WHEN CompletedAtUtc IS NULL THEN 'Active' ELSE 'Completed' END,
    DurationMs = CASE
        WHEN CompletedAtUtc IS NULL THEN 0
        ELSE DATEDIFF_BIG(MILLISECOND, StartedAtUtc, CompletedAtUtc)
    END
WHERE LastActivityAtUtc = '0001-01-01T00:00:00.0000000';
GO

UPDATE s
SET s.CompanyId = l.CompanyId,
    s.BranchId = l.BranchId,
    s.SnapshotSize = DATALENGTH(COALESCE(s.BeforeJson, N'')) + DATALENGTH(COALESCE(s.AfterJson, N'')) + DATALENGTH(COALESCE(s.DiffJson, N''))
FROM AuditEntitySnapshots s
INNER JOIN AuditLogs l ON s.AuditLogId = l.Id
WHERE s.CompanyId IS NULL;
GO

CREATE INDEX [IX_AuditSessions_Status_LastActivityAtUtc] ON [AuditSessions] ([Status], [LastActivityAtUtc]);
GO

CREATE INDEX [IX_AuditReplayEntries_AuditSessionId] ON [AuditReplayEntries] ([AuditSessionId]);
GO

CREATE INDEX [IX_AuditReplayEntries_ParentCorrelationId] ON [AuditReplayEntries] ([ParentCorrelationId]);
GO

CREATE INDEX [IX_AuditEntitySnapshots_CompanyId_BranchId_EntityType_EntityId] ON [AuditEntitySnapshots] ([CompanyId], [BranchId], [EntityType], [EntityId]);
GO

CREATE INDEX [IX_AuditCorrelations_AuditSessionId] ON [AuditCorrelations] ([AuditSessionId]);
GO

CREATE INDEX [IX_AuditCorrelations_ParentCorrelationId] ON [AuditCorrelations] ([ParentCorrelationId]);
GO

ALTER TABLE [AuditEntitySnapshots] ADD CONSTRAINT [FK_AuditEntitySnapshots_AuditLogs_AuditLogId] FOREIGN KEY ([AuditLogId]) REFERENCES [AuditLogs] ([Id]) ON DELETE SET NULL;
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260523062650_CompleteEnterpriseAuditLifecycle', N'8.0.7');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

ALTER TABLE [SearchIndexEntries] ADD [ArchivedAtUtc] datetime2 NULL;
GO

ALTER TABLE [SearchIndexEntries] ADD [IsArchived] bit NOT NULL DEFAULT CAST(0 AS bit);
GO

ALTER TABLE [RecentSearches] ADD [ArchivedAtUtc] datetime2 NULL;
GO

ALTER TABLE [RecentSearches] ADD [IsArchived] bit NOT NULL DEFAULT CAST(0 AS bit);
GO

CREATE TABLE [ArchiveBatches] (
    [Id] uniqueidentifier NOT NULL,
    [CompanyId] uniqueidentifier NULL,
    [BranchId] uniqueidentifier NULL,
    [Module] nvarchar(120) NOT NULL,
    [EntityName] nvarchar(120) NOT NULL,
    [Status] nvarchar(40) NOT NULL,
    [ItemCount] int NOT NULL,
    [PayloadSizeBytes] bigint NOT NULL,
    [Hash] nvarchar(64) NULL,
    [MetadataJson] nvarchar(max) NULL,
    [CreatedAtUtc] datetime2 NOT NULL,
    [ArchivedAtUtc] datetime2 NULL,
    [DeletedAtUtc] datetime2 NULL,
    [CreatedBy] nvarchar(120) NULL,
    [CorrelationId] nvarchar(100) NULL,
    CONSTRAINT [PK_ArchiveBatches] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [ArchiveExportManifests] (
    [Id] uniqueidentifier NOT NULL,
    [CompanyId] uniqueidentifier NULL,
    [BranchId] uniqueidentifier NULL,
    [Module] nvarchar(120) NOT NULL,
    [EntityName] nvarchar(120) NOT NULL,
    [Status] nvarchar(40) NOT NULL,
    [FilePath] nvarchar(1000) NULL,
    [Hash] nvarchar(64) NULL,
    [PayloadSizeBytes] bigint NOT NULL,
    [MetadataJson] nvarchar(max) NULL,
    [CreatedAtUtc] datetime2 NOT NULL,
    [ArchivedAtUtc] datetime2 NULL,
    [DeletedAtUtc] datetime2 NULL,
    [CreatedBy] nvarchar(120) NULL,
    [CorrelationId] nvarchar(100) NULL,
    CONSTRAINT [PK_ArchiveExportManifests] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [DataRetentionPolicies] (
    [Id] uniqueidentifier NOT NULL,
    [CompanyId] uniqueidentifier NULL,
    [BranchId] uniqueidentifier NULL,
    [Module] nvarchar(120) NOT NULL,
    [EntityName] nvarchar(120) NOT NULL,
    [RetentionDays] int NOT NULL,
    [ArchiveAfterDays] int NOT NULL,
    [DeleteAfterDays] int NOT NULL,
    [CompressArchivedPayload] bit NOT NULL,
    [EncryptArchive] bit NOT NULL,
    [IsEnabled] bit NOT NULL,
    [CreatedAtUtc] datetime2 NOT NULL,
    [CreatedBy] nvarchar(120) NULL,
    [CorrelationId] nvarchar(100) NULL,
    [MetadataJson] nvarchar(max) NULL,
    CONSTRAINT [PK_DataRetentionPolicies] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [RecoveryCheckpoints] (
    [Id] uniqueidentifier NOT NULL,
    [CompanyId] uniqueidentifier NULL,
    [BranchId] uniqueidentifier NULL,
    [Module] nvarchar(120) NOT NULL,
    [EntityName] nvarchar(120) NOT NULL,
    [Status] nvarchar(40) NOT NULL,
    [MigrationVersion] nvarchar(100) NULL,
    [SchedulerStateJson] nvarchar(max) NULL,
    [WorkspaceMetadataJson] nvarchar(max) NULL,
    [AuditSessionMetadataJson] nvarchar(max) NULL,
    [MetadataJson] nvarchar(max) NULL,
    [Hash] nvarchar(64) NULL,
    [PayloadSizeBytes] bigint NOT NULL,
    [CreatedAtUtc] datetime2 NOT NULL,
    [ArchivedAtUtc] datetime2 NULL,
    [DeletedAtUtc] datetime2 NULL,
    [CreatedBy] nvarchar(120) NULL,
    [CorrelationId] nvarchar(100) NULL,
    CONSTRAINT [PK_RecoveryCheckpoints] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [ArchiveBatchItems] (
    [Id] uniqueidentifier NOT NULL,
    [ArchiveBatchId] uniqueidentifier NOT NULL,
    [CompanyId] uniqueidentifier NULL,
    [BranchId] uniqueidentifier NULL,
    [Module] nvarchar(120) NOT NULL,
    [EntityName] nvarchar(120) NOT NULL,
    [EntityId] nvarchar(120) NOT NULL,
    [Status] nvarchar(40) NOT NULL,
    [PayloadSizeBytes] bigint NOT NULL,
    [Hash] nvarchar(64) NULL,
    [MetadataJson] nvarchar(max) NULL,
    [PayloadJson] nvarchar(max) NULL,
    [CompressedPayload] varbinary(max) NULL,
    [CompressionType] nvarchar(20) NOT NULL DEFAULT N'None',
    [ArchivedAtUtc] datetime2 NOT NULL,
    [DeletedAtUtc] datetime2 NULL,
    [CreatedBy] nvarchar(120) NULL,
    [CorrelationId] nvarchar(100) NULL,
    CONSTRAINT [PK_ArchiveBatchItems] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ArchiveBatchItems_ArchiveBatches_ArchiveBatchId] FOREIGN KEY ([ArchiveBatchId]) REFERENCES [ArchiveBatches] ([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [RecoveryOperations] (
    [Id] uniqueidentifier NOT NULL,
    [CompanyId] uniqueidentifier NULL,
    [BranchId] uniqueidentifier NULL,
    [RecoveryCheckpointId] uniqueidentifier NULL,
    [OperationType] nvarchar(120) NOT NULL,
    [Status] nvarchar(40) NOT NULL,
    [MetadataJson] nvarchar(max) NULL,
    [Hash] nvarchar(64) NULL,
    [PayloadSizeBytes] bigint NOT NULL,
    [CreatedAtUtc] datetime2 NOT NULL,
    [StartedAtUtc] datetime2 NULL,
    [CompletedAtUtc] datetime2 NULL,
    [CreatedBy] nvarchar(120) NULL,
    [CorrelationId] nvarchar(100) NULL,
    [ErrorMessage] nvarchar(2000) NULL,
    CONSTRAINT [PK_RecoveryOperations] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_RecoveryOperations_RecoveryCheckpoints_RecoveryCheckpointId] FOREIGN KEY ([RecoveryCheckpointId]) REFERENCES [RecoveryCheckpoints] ([Id]) ON DELETE SET NULL
);
GO

CREATE INDEX [IX_SearchIndexEntries_IsArchived] ON [SearchIndexEntries] ([IsArchived]);
GO

CREATE INDEX [IX_RecentSearches_IsArchived] ON [RecentSearches] ([IsArchived]);
GO

CREATE INDEX [IX_ArchiveBatches_CompanyId_BranchId_Module_EntityName_CreatedAtUtc] ON [ArchiveBatches] ([CompanyId], [BranchId], [Module], [EntityName], [CreatedAtUtc]);
GO

CREATE INDEX [IX_ArchiveBatches_Status] ON [ArchiveBatches] ([Status]);
GO

CREATE INDEX [IX_ArchiveBatchItems_ArchiveBatchId] ON [ArchiveBatchItems] ([ArchiveBatchId]);
GO

CREATE INDEX [IX_ArchiveBatchItems_CompanyId_BranchId_Module_EntityName] ON [ArchiveBatchItems] ([CompanyId], [BranchId], [Module], [EntityName]);
GO

CREATE INDEX [IX_ArchiveBatchItems_EntityName_EntityId] ON [ArchiveBatchItems] ([EntityName], [EntityId]);
GO

CREATE INDEX [IX_ArchiveExportManifests_CompanyId_BranchId_Module_CreatedAtUtc] ON [ArchiveExportManifests] ([CompanyId], [BranchId], [Module], [CreatedAtUtc]);
GO

CREATE INDEX [IX_ArchiveExportManifests_Status] ON [ArchiveExportManifests] ([Status]);
GO

CREATE UNIQUE INDEX [IX_DataRetentionPolicies_CompanyId_BranchId_Module_EntityName] ON [DataRetentionPolicies] ([CompanyId], [BranchId], [Module], [EntityName]) WHERE [CompanyId] IS NOT NULL AND [BranchId] IS NOT NULL;
GO

CREATE INDEX [IX_DataRetentionPolicies_IsEnabled] ON [DataRetentionPolicies] ([IsEnabled]);
GO

CREATE INDEX [IX_RecoveryCheckpoints_CompanyId_BranchId_Module_CreatedAtUtc] ON [RecoveryCheckpoints] ([CompanyId], [BranchId], [Module], [CreatedAtUtc]);
GO

CREATE INDEX [IX_RecoveryCheckpoints_Status] ON [RecoveryCheckpoints] ([Status]);
GO

CREATE INDEX [IX_RecoveryOperations_CompanyId_BranchId_OperationType_CreatedAtUtc] ON [RecoveryOperations] ([CompanyId], [BranchId], [OperationType], [CreatedAtUtc]);
GO

CREATE INDEX [IX_RecoveryOperations_RecoveryCheckpointId] ON [RecoveryOperations] ([RecoveryCheckpointId]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260523065326_AddEnterpriseRetentionAndArchival', N'8.0.7');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

CREATE TABLE [RecoveryRecommendations] (
    [Id] uniqueidentifier NOT NULL,
    [CompanyId] uniqueidentifier NULL,
    [BranchId] uniqueidentifier NULL,
    [CorrelationId] nvarchar(100) NULL,
    [SessionId] uniqueidentifier NULL,
    [Module] nvarchar(120) NOT NULL,
    [Category] nvarchar(120) NOT NULL,
    [Severity] nvarchar(30) NOT NULL,
    [RecommendationCode] nvarchar(120) NOT NULL,
    [SuggestedAction] nvarchar(500) NOT NULL,
    [Message] nvarchar(1000) NOT NULL,
    [ConfidenceScore] decimal(18,4) NOT NULL,
    [MetadataJson] nvarchar(max) NULL,
    [MachineName] nvarchar(120) NOT NULL,
    [UserName] nvarchar(120) NULL,
    [Status] nvarchar(40) NOT NULL,
    [RecordedAtUtc] datetime2 NOT NULL,
    [IsArchived] bit NOT NULL,
    [ArchivedAtUtc] datetime2 NULL,
    CONSTRAINT [PK_RecoveryRecommendations] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [RuntimeExceptionLogs] (
    [Id] uniqueidentifier NOT NULL,
    [CompanyId] uniqueidentifier NULL,
    [BranchId] uniqueidentifier NULL,
    [CorrelationId] nvarchar(100) NULL,
    [SessionId] uniqueidentifier NULL,
    [Module] nvarchar(120) NOT NULL,
    [Category] nvarchar(120) NOT NULL,
    [Severity] nvarchar(30) NOT NULL,
    [ExceptionType] nvarchar(200) NOT NULL,
    [StackHash] nvarchar(64) NOT NULL,
    [Message] nvarchar(2000) NOT NULL,
    [MetadataJson] nvarchar(max) NULL,
    [OccurrenceCount] int NOT NULL,
    [MachineName] nvarchar(120) NOT NULL,
    [UserName] nvarchar(120) NULL,
    [RecordedAtUtc] datetime2 NOT NULL,
    [IsArchived] bit NOT NULL,
    [ArchivedAtUtc] datetime2 NULL,
    CONSTRAINT [PK_RuntimeExceptionLogs] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [RuntimeMetricSnapshots] (
    [Id] uniqueidentifier NOT NULL,
    [CompanyId] uniqueidentifier NULL,
    [BranchId] uniqueidentifier NULL,
    [CorrelationId] nvarchar(100) NULL,
    [SessionId] uniqueidentifier NULL,
    [Module] nvarchar(120) NOT NULL,
    [Category] nvarchar(120) NOT NULL,
    [Severity] nvarchar(30) NOT NULL,
    [MetricName] nvarchar(120) NOT NULL,
    [MetricValue] float NOT NULL,
    [Unit] nvarchar(40) NULL,
    [Message] nvarchar(1000) NULL,
    [MetadataJson] nvarchar(max) NULL,
    [MachineName] nvarchar(120) NOT NULL,
    [UserName] nvarchar(120) NULL,
    [RecordedAtUtc] datetime2 NOT NULL,
    [IsArchived] bit NOT NULL,
    [ArchivedAtUtc] datetime2 NULL,
    CONSTRAINT [PK_RuntimeMetricSnapshots] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [SlowOperationLogs] (
    [Id] uniqueidentifier NOT NULL,
    [CompanyId] uniqueidentifier NULL,
    [BranchId] uniqueidentifier NULL,
    [CorrelationId] nvarchar(100) NULL,
    [SessionId] uniqueidentifier NULL,
    [Module] nvarchar(120) NOT NULL,
    [Category] nvarchar(120) NOT NULL,
    [Severity] nvarchar(30) NOT NULL,
    [OperationName] nvarchar(180) NOT NULL,
    [DurationMs] bigint NOT NULL,
    [Message] nvarchar(1000) NULL,
    [MetadataJson] nvarchar(max) NULL,
    [MachineName] nvarchar(120) NOT NULL,
    [UserName] nvarchar(120) NULL,
    [RecordedAtUtc] datetime2 NOT NULL,
    [IsArchived] bit NOT NULL,
    [ArchivedAtUtc] datetime2 NULL,
    CONSTRAINT [PK_SlowOperationLogs] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [SystemResourceSnapshots] (
    [Id] uniqueidentifier NOT NULL,
    [CompanyId] uniqueidentifier NULL,
    [BranchId] uniqueidentifier NULL,
    [CorrelationId] nvarchar(100) NULL,
    [SessionId] uniqueidentifier NULL,
    [Module] nvarchar(120) NOT NULL,
    [Category] nvarchar(120) NOT NULL,
    [Severity] nvarchar(30) NOT NULL,
    [CpuUsagePercent] float NOT NULL,
    [MemoryUsageBytes] bigint NOT NULL,
    [WorkingSetBytes] bigint NOT NULL,
    [ThreadCount] int NOT NULL,
    [Message] nvarchar(1000) NOT NULL,
    [MetadataJson] nvarchar(max) NULL,
    [MachineName] nvarchar(120) NOT NULL,
    [UserName] nvarchar(120) NULL,
    [RecordedAtUtc] datetime2 NOT NULL,
    [IsArchived] bit NOT NULL,
    [ArchivedAtUtc] datetime2 NULL,
    CONSTRAINT [PK_SystemResourceSnapshots] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [TelemetryEvents] (
    [Id] uniqueidentifier NOT NULL,
    [CompanyId] uniqueidentifier NULL,
    [BranchId] uniqueidentifier NULL,
    [CorrelationId] nvarchar(100) NULL,
    [SessionId] uniqueidentifier NULL,
    [Module] nvarchar(120) NOT NULL,
    [Category] nvarchar(120) NOT NULL,
    [Severity] nvarchar(30) NOT NULL,
    [EventType] nvarchar(60) NOT NULL,
    [EventName] nvarchar(180) NOT NULL,
    [Message] nvarchar(2000) NOT NULL,
    [MetadataJson] nvarchar(max) NULL,
    [SearchText] nvarchar(4000) NOT NULL,
    [MachineName] nvarchar(120) NOT NULL,
    [UserName] nvarchar(120) NULL,
    [RecordedAtUtc] datetime2 NOT NULL,
    [IsArchived] bit NOT NULL,
    [ArchivedAtUtc] datetime2 NULL,
    CONSTRAINT [PK_TelemetryEvents] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [UiResponsivenessSnapshots] (
    [Id] uniqueidentifier NOT NULL,
    [CompanyId] uniqueidentifier NULL,
    [BranchId] uniqueidentifier NULL,
    [CorrelationId] nvarchar(100) NULL,
    [SessionId] uniqueidentifier NULL,
    [Module] nvarchar(120) NOT NULL,
    [Category] nvarchar(120) NOT NULL,
    [Severity] nvarchar(30) NOT NULL,
    [OperationName] nvarchar(180) NOT NULL,
    [DurationMs] bigint NOT NULL,
    [IsFrozen] bit NOT NULL,
    [Message] nvarchar(1000) NOT NULL,
    [MetadataJson] nvarchar(max) NULL,
    [MachineName] nvarchar(120) NOT NULL,
    [UserName] nvarchar(120) NULL,
    [RecordedAtUtc] datetime2 NOT NULL,
    [IsArchived] bit NOT NULL,
    [ArchivedAtUtc] datetime2 NULL,
    CONSTRAINT [PK_UiResponsivenessSnapshots] PRIMARY KEY ([Id])
);
GO

CREATE INDEX [IX_RecoveryRecommendations_CompanyId_BranchId_Module_RecordedAtUtc] ON [RecoveryRecommendations] ([CompanyId], [BranchId], [Module], [RecordedAtUtc]);
GO

CREATE INDEX [IX_RecoveryRecommendations_CorrelationId] ON [RecoveryRecommendations] ([CorrelationId]);
GO

CREATE INDEX [IX_RecoveryRecommendations_RecommendationCode] ON [RecoveryRecommendations] ([RecommendationCode]);
GO

CREATE INDEX [IX_RecoveryRecommendations_RecordedAtUtc] ON [RecoveryRecommendations] ([RecordedAtUtc]);
GO

CREATE INDEX [IX_RecoveryRecommendations_SessionId] ON [RecoveryRecommendations] ([SessionId]);
GO

CREATE INDEX [IX_RuntimeExceptionLogs_CompanyId_BranchId_Module_RecordedAtUtc] ON [RuntimeExceptionLogs] ([CompanyId], [BranchId], [Module], [RecordedAtUtc]);
GO

CREATE INDEX [IX_RuntimeExceptionLogs_CorrelationId] ON [RuntimeExceptionLogs] ([CorrelationId]);
GO

CREATE INDEX [IX_RuntimeExceptionLogs_ExceptionType_StackHash] ON [RuntimeExceptionLogs] ([ExceptionType], [StackHash]);
GO

CREATE INDEX [IX_RuntimeExceptionLogs_IsArchived] ON [RuntimeExceptionLogs] ([IsArchived]);
GO

CREATE INDEX [IX_RuntimeExceptionLogs_RecordedAtUtc] ON [RuntimeExceptionLogs] ([RecordedAtUtc]);
GO

CREATE INDEX [IX_RuntimeExceptionLogs_SessionId] ON [RuntimeExceptionLogs] ([SessionId]);
GO

CREATE INDEX [IX_RuntimeMetricSnapshots_CompanyId_BranchId_Module_RecordedAtUtc] ON [RuntimeMetricSnapshots] ([CompanyId], [BranchId], [Module], [RecordedAtUtc]);
GO

CREATE INDEX [IX_RuntimeMetricSnapshots_CorrelationId] ON [RuntimeMetricSnapshots] ([CorrelationId]);
GO

CREATE INDEX [IX_RuntimeMetricSnapshots_IsArchived] ON [RuntimeMetricSnapshots] ([IsArchived]);
GO

CREATE INDEX [IX_RuntimeMetricSnapshots_RecordedAtUtc] ON [RuntimeMetricSnapshots] ([RecordedAtUtc]);
GO

CREATE INDEX [IX_RuntimeMetricSnapshots_SessionId] ON [RuntimeMetricSnapshots] ([SessionId]);
GO

CREATE INDEX [IX_SlowOperationLogs_CompanyId_BranchId_Module_RecordedAtUtc] ON [SlowOperationLogs] ([CompanyId], [BranchId], [Module], [RecordedAtUtc]);
GO

CREATE INDEX [IX_SlowOperationLogs_CorrelationId] ON [SlowOperationLogs] ([CorrelationId]);
GO

CREATE INDEX [IX_SlowOperationLogs_DurationMs] ON [SlowOperationLogs] ([DurationMs]);
GO

CREATE INDEX [IX_SlowOperationLogs_RecordedAtUtc] ON [SlowOperationLogs] ([RecordedAtUtc]);
GO

CREATE INDEX [IX_SlowOperationLogs_SessionId] ON [SlowOperationLogs] ([SessionId]);
GO

CREATE INDEX [IX_SystemResourceSnapshots_CompanyId_BranchId_Module_RecordedAtUtc] ON [SystemResourceSnapshots] ([CompanyId], [BranchId], [Module], [RecordedAtUtc]);
GO

CREATE INDEX [IX_SystemResourceSnapshots_CorrelationId] ON [SystemResourceSnapshots] ([CorrelationId]);
GO

CREATE INDEX [IX_SystemResourceSnapshots_RecordedAtUtc] ON [SystemResourceSnapshots] ([RecordedAtUtc]);
GO

CREATE INDEX [IX_SystemResourceSnapshots_SessionId] ON [SystemResourceSnapshots] ([SessionId]);
GO

CREATE INDEX [IX_TelemetryEvents_CompanyId_BranchId_Module_RecordedAtUtc] ON [TelemetryEvents] ([CompanyId], [BranchId], [Module], [RecordedAtUtc]);
GO

CREATE INDEX [IX_TelemetryEvents_CorrelationId] ON [TelemetryEvents] ([CorrelationId]);
GO

CREATE INDEX [IX_TelemetryEvents_IsArchived_RecordedAtUtc] ON [TelemetryEvents] ([IsArchived], [RecordedAtUtc]);
GO

CREATE INDEX [IX_TelemetryEvents_RecordedAtUtc] ON [TelemetryEvents] ([RecordedAtUtc]);
GO

CREATE INDEX [IX_TelemetryEvents_SessionId] ON [TelemetryEvents] ([SessionId]);
GO

CREATE INDEX [IX_UiResponsivenessSnapshots_CompanyId_BranchId_Module_RecordedAtUtc] ON [UiResponsivenessSnapshots] ([CompanyId], [BranchId], [Module], [RecordedAtUtc]);
GO

CREATE INDEX [IX_UiResponsivenessSnapshots_CorrelationId] ON [UiResponsivenessSnapshots] ([CorrelationId]);
GO

CREATE INDEX [IX_UiResponsivenessSnapshots_IsFrozen] ON [UiResponsivenessSnapshots] ([IsFrozen]);
GO

CREATE INDEX [IX_UiResponsivenessSnapshots_RecordedAtUtc] ON [UiResponsivenessSnapshots] ([RecordedAtUtc]);
GO

CREATE INDEX [IX_UiResponsivenessSnapshots_SessionId] ON [UiResponsivenessSnapshots] ([SessionId]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260523075514_AddEnterpriseTelemetryAndDiagnostics', N'8.0.7');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

CREATE TABLE [IdempotencyKeyEntries] (
    [Id] uniqueidentifier NOT NULL,
    [CompanyId] uniqueidentifier NULL,
    [BranchId] uniqueidentifier NULL,
    [CorrelationId] nvarchar(100) NOT NULL,
    [WorkflowType] nvarchar(120) NOT NULL,
    [WorkflowKey] nvarchar(160) NOT NULL,
    [IdempotencyKey] nvarchar(160) NOT NULL,
    [ExecutionHash] nvarchar(64) NOT NULL,
    [PayloadHash] nvarchar(64) NOT NULL,
    [Status] nvarchar(40) NOT NULL,
    [StartedAtUtc] datetime2 NOT NULL,
    [CompletedAtUtc] datetime2 NULL,
    [MetadataJson] nvarchar(max) NULL,
    CONSTRAINT [PK_IdempotencyKeyEntries] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [WorkflowExecutions] (
    [Id] uniqueidentifier NOT NULL,
    [CompanyId] uniqueidentifier NULL,
    [BranchId] uniqueidentifier NULL,
    [CorrelationId] nvarchar(100) NOT NULL,
    [WorkflowType] nvarchar(120) NOT NULL,
    [WorkflowKey] nvarchar(160) NOT NULL,
    [Status] nvarchar(40) NOT NULL,
    [RetryCount] int NOT NULL,
    [StartedAtUtc] datetime2 NOT NULL,
    [CompletedAtUtc] datetime2 NULL,
    [FailedAtUtc] datetime2 NULL,
    [LastHeartbeatUtc] datetime2 NOT NULL,
    [IdempotencyKey] nvarchar(160) NULL,
    [ExecutionHash] nvarchar(64) NOT NULL,
    [PayloadHash] nvarchar(64) NOT NULL,
    [RecoveryStateJson] nvarchar(max) NULL,
    [CompensationStateJson] nvarchar(max) NULL,
    [ExceptionType] nvarchar(250) NULL,
    [ErrorMessage] nvarchar(2000) NULL,
    [MachineName] nvarchar(120) NULL,
    [UserName] nvarchar(120) NULL,
    [MetadataJson] nvarchar(max) NULL,
    CONSTRAINT [PK_WorkflowExecutions] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [DistributedOperationMarkers] (
    [Id] uniqueidentifier NOT NULL,
    [WorkflowExecutionId] uniqueidentifier NOT NULL,
    [CompanyId] uniqueidentifier NULL,
    [BranchId] uniqueidentifier NULL,
    [CorrelationId] nvarchar(100) NOT NULL,
    [WorkflowType] nvarchar(120) NOT NULL,
    [WorkflowKey] nvarchar(160) NOT NULL,
    [StepName] nvarchar(120) NOT NULL,
    [Status] nvarchar(40) NOT NULL,
    [MachineName] nvarchar(120) NOT NULL,
    [UserName] nvarchar(120) NOT NULL,
    [MetadataJson] nvarchar(max) NULL,
    [StartedAtUtc] datetime2 NOT NULL,
    [CompletedAtUtc] datetime2 NULL,
    CONSTRAINT [PK_DistributedOperationMarkers] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_DistributedOperationMarkers_WorkflowExecutions_WorkflowExecutionId] FOREIGN KEY ([WorkflowExecutionId]) REFERENCES [WorkflowExecutions] ([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [WorkflowConsistencyViolations] (
    [Id] uniqueidentifier NOT NULL,
    [WorkflowExecutionId] uniqueidentifier NULL,
    [CompanyId] uniqueidentifier NULL,
    [BranchId] uniqueidentifier NULL,
    [CorrelationId] nvarchar(100) NOT NULL,
    [WorkflowType] nvarchar(120) NOT NULL,
    [WorkflowKey] nvarchar(160) NOT NULL,
    [Severity] nvarchar(30) NOT NULL,
    [Category] nvarchar(80) NOT NULL,
    [Recommendation] nvarchar(600) NOT NULL,
    [AffectedEntityModule] nvarchar(120) NOT NULL,
    [MetadataJson] nvarchar(max) NULL,
    [RecordedAtUtc] datetime2 NOT NULL,
    CONSTRAINT [PK_WorkflowConsistencyViolations] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_WorkflowConsistencyViolations_WorkflowExecutions_WorkflowExecutionId] FOREIGN KEY ([WorkflowExecutionId]) REFERENCES [WorkflowExecutions] ([Id]) ON DELETE SET NULL
);
GO

CREATE TABLE [WorkflowExecutionSteps] (
    [Id] uniqueidentifier NOT NULL,
    [WorkflowExecutionId] uniqueidentifier NOT NULL,
    [CompanyId] uniqueidentifier NULL,
    [BranchId] uniqueidentifier NULL,
    [CorrelationId] nvarchar(100) NOT NULL,
    [StepName] nvarchar(120) NOT NULL,
    [Status] nvarchar(40) NOT NULL,
    [RetryCount] int NOT NULL,
    [StartedAtUtc] datetime2 NOT NULL,
    [CompletedAtUtc] datetime2 NULL,
    [FailedAtUtc] datetime2 NULL,
    [LastHeartbeatUtc] datetime2 NOT NULL,
    [ExceptionType] nvarchar(250) NULL,
    [ErrorMessage] nvarchar(2000) NULL,
    [MetadataJson] nvarchar(max) NULL,
    CONSTRAINT [PK_WorkflowExecutionSteps] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_WorkflowExecutionSteps_WorkflowExecutions_WorkflowExecutionId] FOREIGN KEY ([WorkflowExecutionId]) REFERENCES [WorkflowExecutions] ([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [WorkflowCompensationActions] (
    [Id] uniqueidentifier NOT NULL,
    [WorkflowExecutionId] uniqueidentifier NOT NULL,
    [WorkflowExecutionStepId] uniqueidentifier NULL,
    [CompanyId] uniqueidentifier NULL,
    [BranchId] uniqueidentifier NULL,
    [CorrelationId] nvarchar(100) NOT NULL,
    [StepName] nvarchar(120) NOT NULL,
    [Status] nvarchar(40) NOT NULL,
    [RetryCount] int NOT NULL,
    [CompensationStateJson] nvarchar(max) NULL,
    [ExceptionType] nvarchar(250) NULL,
    [ErrorMessage] nvarchar(2000) NULL,
    [MetadataJson] nvarchar(max) NULL,
    [StartedAtUtc] datetime2 NOT NULL,
    [CompletedAtUtc] datetime2 NULL,
    [FailedAtUtc] datetime2 NULL,
    CONSTRAINT [PK_WorkflowCompensationActions] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_WorkflowCompensationActions_WorkflowExecutionSteps_WorkflowExecutionStepId] FOREIGN KEY ([WorkflowExecutionStepId]) REFERENCES [WorkflowExecutionSteps] ([Id]) ON DELETE SET NULL,
    CONSTRAINT [FK_WorkflowCompensationActions_WorkflowExecutions_WorkflowExecutionId] FOREIGN KEY ([WorkflowExecutionId]) REFERENCES [WorkflowExecutions] ([Id])
);
GO

CREATE TABLE [WorkflowRetryHistories] (
    [Id] uniqueidentifier NOT NULL,
    [WorkflowExecutionId] uniqueidentifier NOT NULL,
    [WorkflowExecutionStepId] uniqueidentifier NULL,
    [CompanyId] uniqueidentifier NULL,
    [BranchId] uniqueidentifier NULL,
    [CorrelationId] nvarchar(100) NOT NULL,
    [WorkflowType] nvarchar(120) NOT NULL,
    [StepName] nvarchar(120) NULL,
    [RetryCount] int NOT NULL,
    [Status] nvarchar(40) NOT NULL,
    [ExceptionType] nvarchar(250) NULL,
    [ErrorMessage] nvarchar(2000) NULL,
    [MetadataJson] nvarchar(max) NULL,
    [StartedAtUtc] datetime2 NOT NULL,
    [CompletedAtUtc] datetime2 NULL,
    CONSTRAINT [PK_WorkflowRetryHistories] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_WorkflowRetryHistories_WorkflowExecutionSteps_WorkflowExecutionStepId] FOREIGN KEY ([WorkflowExecutionStepId]) REFERENCES [WorkflowExecutionSteps] ([Id]) ON DELETE SET NULL,
    CONSTRAINT [FK_WorkflowRetryHistories_WorkflowExecutions_WorkflowExecutionId] FOREIGN KEY ([WorkflowExecutionId]) REFERENCES [WorkflowExecutions] ([Id])
);
GO

CREATE INDEX [IX_DistributedOperationMarkers_CompanyId_BranchId_WorkflowType_WorkflowKey_StepName] ON [DistributedOperationMarkers] ([CompanyId], [BranchId], [WorkflowType], [WorkflowKey], [StepName]);
GO

CREATE INDEX [IX_DistributedOperationMarkers_WorkflowExecutionId] ON [DistributedOperationMarkers] ([WorkflowExecutionId]);
GO

CREATE UNIQUE INDEX [IX_IdempotencyKeyEntries_CompanyId_BranchId_WorkflowType_WorkflowKey_IdempotencyKey] ON [IdempotencyKeyEntries] ([CompanyId], [BranchId], [WorkflowType], [WorkflowKey], [IdempotencyKey]) WHERE [CompanyId] IS NOT NULL AND [BranchId] IS NOT NULL;
GO

CREATE INDEX [IX_IdempotencyKeyEntries_WorkflowType_WorkflowKey_Status] ON [IdempotencyKeyEntries] ([WorkflowType], [WorkflowKey], [Status]);
GO

CREATE INDEX [IX_WorkflowCompensationActions_WorkflowExecutionId] ON [WorkflowCompensationActions] ([WorkflowExecutionId]);
GO

CREATE INDEX [IX_WorkflowCompensationActions_WorkflowExecutionId_StepName_Status] ON [WorkflowCompensationActions] ([WorkflowExecutionId], [StepName], [Status]);
GO

CREATE INDEX [IX_WorkflowCompensationActions_WorkflowExecutionStepId] ON [WorkflowCompensationActions] ([WorkflowExecutionStepId]);
GO

CREATE INDEX [IX_WorkflowConsistencyViolations_Category_Severity] ON [WorkflowConsistencyViolations] ([Category], [Severity]);
GO

CREATE INDEX [IX_WorkflowConsistencyViolations_CompanyId_BranchId_WorkflowType_RecordedAtUtc] ON [WorkflowConsistencyViolations] ([CompanyId], [BranchId], [WorkflowType], [RecordedAtUtc]);
GO

CREATE INDEX [IX_WorkflowConsistencyViolations_WorkflowExecutionId] ON [WorkflowConsistencyViolations] ([WorkflowExecutionId]);
GO

CREATE INDEX [IX_WorkflowExecutions_CompanyId_BranchId_WorkflowType_WorkflowKey_StartedAtUtc] ON [WorkflowExecutions] ([CompanyId], [BranchId], [WorkflowType], [WorkflowKey], [StartedAtUtc]);
GO

CREATE INDEX [IX_WorkflowExecutions_CorrelationId] ON [WorkflowExecutions] ([CorrelationId]);
GO

CREATE INDEX [IX_WorkflowExecutions_LastHeartbeatUtc] ON [WorkflowExecutions] ([LastHeartbeatUtc]);
GO

CREATE INDEX [IX_WorkflowExecutions_WorkflowType_WorkflowKey_Status] ON [WorkflowExecutions] ([WorkflowType], [WorkflowKey], [Status]);
GO

CREATE INDEX [IX_WorkflowExecutionSteps_LastHeartbeatUtc] ON [WorkflowExecutionSteps] ([LastHeartbeatUtc]);
GO

CREATE INDEX [IX_WorkflowExecutionSteps_WorkflowExecutionId] ON [WorkflowExecutionSteps] ([WorkflowExecutionId]);
GO

CREATE INDEX [IX_WorkflowExecutionSteps_WorkflowExecutionId_StepName_Status] ON [WorkflowExecutionSteps] ([WorkflowExecutionId], [StepName], [Status]);
GO

CREATE INDEX [IX_WorkflowRetryHistories_WorkflowExecutionId] ON [WorkflowRetryHistories] ([WorkflowExecutionId]);
GO

CREATE INDEX [IX_WorkflowRetryHistories_WorkflowExecutionStepId] ON [WorkflowRetryHistories] ([WorkflowExecutionStepId]);
GO

CREATE INDEX [IX_WorkflowRetryHistories_WorkflowType_RetryCount_StartedAtUtc] ON [WorkflowRetryHistories] ([WorkflowType], [RetryCount], [StartedAtUtc]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260523094333_AddWorkflowResilienceAndRecovery', N'8.0.7');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

ALTER TABLE [WorkflowExecutions] ADD [ParentWorkflowExecutionId] uniqueidentifier NULL;
GO

ALTER TABLE [SearchIndexEntries] ADD [MetadataJson] nvarchar(max) NULL;
GO

ALTER TABLE [RecentSearches] ADD [MetadataJson] nvarchar(max) NULL;
GO

CREATE TABLE [EnterpriseConsistencyViolations] (
    [Id] uniqueidentifier NOT NULL,
    [CompanyId] uniqueidentifier NULL,
    [BranchId] uniqueidentifier NULL,
    [Category] nvarchar(120) NOT NULL,
    [Severity] nvarchar(30) NOT NULL,
    [Module] nvarchar(120) NOT NULL,
    [EntityType] nvarchar(120) NOT NULL,
    [EntityId] nvarchar(120) NOT NULL,
    [Recommendation] nvarchar(600) NOT NULL,
    [CanAutoRecover] bit NOT NULL,
    [MetadataJson] nvarchar(max) NULL,
    [RecordedAtUtc] datetime2 NOT NULL,
    CONSTRAINT [PK_EnterpriseConsistencyViolations] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [WorkflowDeadLetters] (
    [Id] uniqueidentifier NOT NULL,
    [CompanyId] uniqueidentifier NULL,
    [BranchId] uniqueidentifier NULL,
    [WorkflowExecutionId] uniqueidentifier NULL,
    [CorrelationId] nvarchar(100) NOT NULL,
    [WorkflowType] nvarchar(120) NOT NULL,
    [WorkflowKey] nvarchar(160) NOT NULL,
    [DeadLetterReason] nvarchar(400) NOT NULL,
    [Status] nvarchar(40) NOT NULL,
    [PayloadJson] nvarchar(max) NULL,
    [ExceptionType] nvarchar(250) NULL,
    [ErrorMessage] nvarchar(2000) NULL,
    [MetadataJson] nvarchar(max) NULL,
    [DeadLetteredAtUtc] datetime2 NOT NULL,
    [RecoveredAtUtc] datetime2 NULL,
    CONSTRAINT [PK_WorkflowDeadLetters] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_WorkflowDeadLetters_WorkflowExecutions_WorkflowExecutionId] FOREIGN KEY ([WorkflowExecutionId]) REFERENCES [WorkflowExecutions] ([Id]) ON DELETE SET NULL
);
GO

CREATE TABLE [WorkflowDistributedLocks] (
    [Id] uniqueidentifier NOT NULL,
    [CompanyId] uniqueidentifier NULL,
    [BranchId] uniqueidentifier NULL,
    [WorkflowExecutionId] uniqueidentifier NULL,
    [CorrelationId] nvarchar(100) NOT NULL,
    [WorkflowType] nvarchar(120) NOT NULL,
    [WorkflowKey] nvarchar(160) NOT NULL,
    [LockScope] nvarchar(120) NOT NULL,
    [LockKey] nvarchar(200) NOT NULL,
    [Status] nvarchar(40) NOT NULL,
    [LockedAtUtc] datetime2 NOT NULL,
    [LastHeartbeatUtc] datetime2 NOT NULL,
    [ExpiresAtUtc] datetime2 NOT NULL,
    [HolderMachineName] nvarchar(120) NOT NULL,
    [HolderProcessId] int NOT NULL,
    [MetadataJson] nvarchar(max) NULL,
    CONSTRAINT [PK_WorkflowDistributedLocks] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_WorkflowDistributedLocks_WorkflowExecutions_WorkflowExecutionId] FOREIGN KEY ([WorkflowExecutionId]) REFERENCES [WorkflowExecutions] ([Id]) ON DELETE SET NULL
);
GO

CREATE TABLE [WorkflowExecutionMetrics] (
    [Id] uniqueidentifier NOT NULL,
    [CompanyId] uniqueidentifier NULL,
    [BranchId] uniqueidentifier NULL,
    [WorkflowExecutionId] uniqueidentifier NULL,
    [CorrelationId] nvarchar(100) NOT NULL,
    [WorkflowType] nvarchar(120) NOT NULL,
    [WorkflowKey] nvarchar(160) NOT NULL,
    [MetricName] nvarchar(120) NOT NULL,
    [Category] nvarchar(120) NOT NULL,
    [Severity] nvarchar(30) NOT NULL,
    [MetricValue] float NOT NULL,
    [DurationMs] bigint NOT NULL,
    [MetadataJson] nvarchar(max) NULL,
    [RecordedAtUtc] datetime2 NOT NULL,
    CONSTRAINT [PK_WorkflowExecutionMetrics] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_WorkflowExecutionMetrics_WorkflowExecutions_WorkflowExecutionId] FOREIGN KEY ([WorkflowExecutionId]) REFERENCES [WorkflowExecutions] ([Id]) ON DELETE SET NULL
);
GO

CREATE TABLE [WorkflowLeases] (
    [Id] uniqueidentifier NOT NULL,
    [CompanyId] uniqueidentifier NULL,
    [BranchId] uniqueidentifier NULL,
    [WorkflowExecutionId] uniqueidentifier NULL,
    [CorrelationId] nvarchar(100) NOT NULL,
    [WorkflowType] nvarchar(120) NOT NULL,
    [WorkflowKey] nvarchar(160) NOT NULL,
    [LeaseKey] nvarchar(200) NOT NULL,
    [Status] nvarchar(40) NOT NULL,
    [HolderMachineName] nvarchar(120) NOT NULL,
    [HolderProcessId] int NOT NULL,
    [StartedAtUtc] datetime2 NOT NULL,
    [LastHeartbeatUtc] datetime2 NOT NULL,
    [ExpiresAtUtc] datetime2 NOT NULL,
    [MetadataJson] nvarchar(max) NULL,
    CONSTRAINT [PK_WorkflowLeases] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_WorkflowLeases_WorkflowExecutions_WorkflowExecutionId] FOREIGN KEY ([WorkflowExecutionId]) REFERENCES [WorkflowExecutions] ([Id]) ON DELETE SET NULL
);
GO

CREATE TABLE [WorkflowRecoveryCheckpoints] (
    [Id] uniqueidentifier NOT NULL,
    [CompanyId] uniqueidentifier NULL,
    [BranchId] uniqueidentifier NULL,
    [WorkflowExecutionId] uniqueidentifier NULL,
    [ParentCheckpointId] uniqueidentifier NULL,
    [CorrelationId] nvarchar(100) NOT NULL,
    [WorkflowType] nvarchar(120) NOT NULL,
    [WorkflowKey] nvarchar(160) NOT NULL,
    [CheckpointName] nvarchar(120) NOT NULL,
    [Status] nvarchar(40) NOT NULL,
    [SnapshotJson] nvarchar(max) NOT NULL,
    [SnapshotHash] nvarchar(64) NOT NULL,
    [MetadataJson] nvarchar(max) NULL,
    [CreatedAtUtc] datetime2 NOT NULL,
    [RestoredAtUtc] datetime2 NULL,
    CONSTRAINT [PK_WorkflowRecoveryCheckpoints] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_WorkflowRecoveryCheckpoints_WorkflowExecutions_WorkflowExecutionId] FOREIGN KEY ([WorkflowExecutionId]) REFERENCES [WorkflowExecutions] ([Id]) ON DELETE SET NULL
);
GO

CREATE INDEX [IX_WorkflowExecutions_ParentWorkflowExecutionId] ON [WorkflowExecutions] ([ParentWorkflowExecutionId]);
GO

CREATE INDEX [IX_EnterpriseConsistencyViolations_CompanyId_BranchId_Category_RecordedAtUtc] ON [EnterpriseConsistencyViolations] ([CompanyId], [BranchId], [Category], [RecordedAtUtc]);
GO

CREATE INDEX [IX_EnterpriseConsistencyViolations_Module_EntityType] ON [EnterpriseConsistencyViolations] ([Module], [EntityType]);
GO

CREATE INDEX [IX_WorkflowDeadLetters_CompanyId_BranchId_WorkflowType_WorkflowKey_DeadLetteredAtUtc] ON [WorkflowDeadLetters] ([CompanyId], [BranchId], [WorkflowType], [WorkflowKey], [DeadLetteredAtUtc]);
GO

CREATE INDEX [IX_WorkflowDeadLetters_Status] ON [WorkflowDeadLetters] ([Status]);
GO

CREATE INDEX [IX_WorkflowDeadLetters_WorkflowExecutionId] ON [WorkflowDeadLetters] ([WorkflowExecutionId]);
GO

CREATE UNIQUE INDEX [IX_WorkflowDistributedLocks_CompanyId_BranchId_LockScope_LockKey] ON [WorkflowDistributedLocks] ([CompanyId], [BranchId], [LockScope], [LockKey]) WHERE [CompanyId] IS NOT NULL AND [BranchId] IS NOT NULL;
GO

CREATE INDEX [IX_WorkflowDistributedLocks_ExpiresAtUtc] ON [WorkflowDistributedLocks] ([ExpiresAtUtc]);
GO

CREATE INDEX [IX_WorkflowDistributedLocks_WorkflowExecutionId] ON [WorkflowDistributedLocks] ([WorkflowExecutionId]);
GO

CREATE INDEX [IX_WorkflowExecutionMetrics_CompanyId_BranchId_WorkflowType_RecordedAtUtc] ON [WorkflowExecutionMetrics] ([CompanyId], [BranchId], [WorkflowType], [RecordedAtUtc]);
GO

CREATE INDEX [IX_WorkflowExecutionMetrics_CorrelationId] ON [WorkflowExecutionMetrics] ([CorrelationId]);
GO

CREATE INDEX [IX_WorkflowExecutionMetrics_WorkflowExecutionId] ON [WorkflowExecutionMetrics] ([WorkflowExecutionId]);
GO

CREATE UNIQUE INDEX [IX_WorkflowLeases_CompanyId_BranchId_WorkflowType_WorkflowKey_LeaseKey] ON [WorkflowLeases] ([CompanyId], [BranchId], [WorkflowType], [WorkflowKey], [LeaseKey]) WHERE [CompanyId] IS NOT NULL AND [BranchId] IS NOT NULL;
GO

CREATE INDEX [IX_WorkflowLeases_ExpiresAtUtc] ON [WorkflowLeases] ([ExpiresAtUtc]);
GO

CREATE INDEX [IX_WorkflowLeases_WorkflowExecutionId] ON [WorkflowLeases] ([WorkflowExecutionId]);
GO

CREATE INDEX [IX_WorkflowRecoveryCheckpoints_CompanyId_BranchId_WorkflowType_WorkflowKey_CreatedAtUtc] ON [WorkflowRecoveryCheckpoints] ([CompanyId], [BranchId], [WorkflowType], [WorkflowKey], [CreatedAtUtc]);
GO

CREATE INDEX [IX_WorkflowRecoveryCheckpoints_Status] ON [WorkflowRecoveryCheckpoints] ([Status]);
GO

CREATE INDEX [IX_WorkflowRecoveryCheckpoints_WorkflowExecutionId] ON [WorkflowRecoveryCheckpoints] ([WorkflowExecutionId]);
GO

ALTER TABLE [WorkflowExecutions] ADD CONSTRAINT [FK_WorkflowExecutions_WorkflowExecutions_ParentWorkflowExecutionId] FOREIGN KEY ([ParentWorkflowExecutionId]) REFERENCES [WorkflowExecutions] ([Id]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260523103649_AddEnterpriseReliabilityAndDistributedCoordination', N'8.0.7');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

ALTER TABLE [UserCompanyAccesses] ADD [CreatedAtUtc] datetime2 NOT NULL DEFAULT '0001-01-01T00:00:00.0000000';
GO

ALTER TABLE [UserCompanyAccesses] ADD [IsArchived] bit NOT NULL DEFAULT CAST(0 AS bit);
GO

ALTER TABLE [UserCompanyAccesses] ADD [ModifiedAtUtc] datetime2 NOT NULL DEFAULT '0001-01-01T00:00:00.0000000';
GO

ALTER TABLE [UserCompanyAccesses] ADD [RowVersion] rowversion NULL;
GO

ALTER TABLE [UserBranchAccesses] ADD [CreatedAtUtc] datetime2 NOT NULL DEFAULT '0001-01-01T00:00:00.0000000';
GO

ALTER TABLE [UserBranchAccesses] ADD [IsArchived] bit NOT NULL DEFAULT CAST(0 AS bit);
GO

ALTER TABLE [UserBranchAccesses] ADD [ModifiedAtUtc] datetime2 NOT NULL DEFAULT '0001-01-01T00:00:00.0000000';
GO

ALTER TABLE [UserBranchAccesses] ADD [RowVersion] rowversion NULL;
GO

ALTER TABLE [Companies] ADD [AccountingStandardProfile] nvarchar(80) NULL;
GO

ALTER TABLE [Companies] ADD [CreatedAtUtc] datetime2 NOT NULL DEFAULT '0001-01-01T00:00:00.0000000';
GO

ALTER TABLE [Companies] ADD [DefaultCurrencyCode] nvarchar(12) NULL;
GO

ALTER TABLE [Companies] ADD [FiscalConfigJson] nvarchar(max) NULL;
GO

ALTER TABLE [Companies] ADD [IsArchived] bit NOT NULL DEFAULT CAST(0 AS bit);
GO

ALTER TABLE [Companies] ADD [Locale] nvarchar(20) NULL;
GO

ALTER TABLE [Companies] ADD [ModifiedAtUtc] datetime2 NOT NULL DEFAULT '0001-01-01T00:00:00.0000000';
GO

ALTER TABLE [Companies] ADD [RowVersion] rowversion NULL;
GO

ALTER TABLE [Companies] ADD [TimeZoneId] nvarchar(80) NULL;
GO

ALTER TABLE [Branches] ADD [CreatedAtUtc] datetime2 NOT NULL DEFAULT '0001-01-01T00:00:00.0000000';
GO

ALTER TABLE [Branches] ADD [IsArchived] bit NOT NULL DEFAULT CAST(0 AS bit);
GO

ALTER TABLE [Branches] ADD [ModifiedAtUtc] datetime2 NOT NULL DEFAULT '0001-01-01T00:00:00.0000000';
GO

ALTER TABLE [Branches] ADD [ParentBranchId] uniqueidentifier NULL;
GO

ALTER TABLE [Branches] ADD [RowVersion] rowversion NULL;
GO

CREATE TABLE [ActiveCompanySessions] (
    [Id] uniqueidentifier NOT NULL,
    [CompanyId] uniqueidentifier NULL,
    [BranchId] uniqueidentifier NULL,
    [UserId] uniqueidentifier NOT NULL,
    [SessionId] uniqueidentifier NULL,
    [Status] nvarchar(40) NOT NULL,
    [Source] nvarchar(120) NULL,
    [MachineName] nvarchar(120) NULL,
    [AppVersion] nvarchar(40) NULL,
    [SessionMetadataJson] nvarchar(max) NULL,
    [StartedAtUtc] datetime2 NOT NULL,
    [LastActivityAtUtc] datetime2 NOT NULL,
    [CompletedAtUtc] datetime2 NULL,
    [ExpiresAtUtc] datetime2 NULL,
    [IsActive] bit NOT NULL,
    [IsArchived] bit NOT NULL,
    [CreatedAtUtc] datetime2 NOT NULL,
    [ModifiedAtUtc] datetime2 NOT NULL,
    [RowVersion] rowversion NULL,
    CONSTRAINT [PK_ActiveCompanySessions] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ActiveCompanySessions_Branches_BranchId] FOREIGN KEY ([BranchId]) REFERENCES [Branches] ([Id]),
    CONSTRAINT [FK_ActiveCompanySessions_Companies_CompanyId] FOREIGN KEY ([CompanyId]) REFERENCES [Companies] ([Id])
);
GO

CREATE TABLE [BranchSettings] (
    [Id] uniqueidentifier NOT NULL,
    [CompanyId] uniqueidentifier NULL,
    [BranchId] uniqueidentifier NULL,
    [SettingKey] nvarchar(120) NOT NULL,
    [ValueJson] nvarchar(max) NULL,
    [MetadataJson] nvarchar(max) NULL,
    [IsArchived] bit NOT NULL,
    [CreatedAtUtc] datetime2 NOT NULL,
    [ModifiedAtUtc] datetime2 NOT NULL,
    [RowVersion] rowversion NULL,
    CONSTRAINT [PK_BranchSettings] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_BranchSettings_Branches_BranchId] FOREIGN KEY ([BranchId]) REFERENCES [Branches] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_BranchSettings_Companies_CompanyId] FOREIGN KEY ([CompanyId]) REFERENCES [Companies] ([Id])
);
GO

CREATE TABLE [CompanyFeatureFlags] (
    [Id] uniqueidentifier NOT NULL,
    [CompanyId] uniqueidentifier NULL,
    [FeatureKey] nvarchar(120) NOT NULL,
    [IsEnabled] bit NOT NULL,
    [ValueJson] nvarchar(max) NULL,
    [MetadataJson] nvarchar(max) NULL,
    [IsArchived] bit NOT NULL,
    [CreatedAtUtc] datetime2 NOT NULL,
    [ModifiedAtUtc] datetime2 NOT NULL,
    [RowVersion] rowversion NULL,
    CONSTRAINT [PK_CompanyFeatureFlags] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_CompanyFeatureFlags_Companies_CompanyId] FOREIGN KEY ([CompanyId]) REFERENCES [Companies] ([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [CompanyFiscalPeriods] (
    [Id] uniqueidentifier NOT NULL,
    [CompanyId] uniqueidentifier NULL,
    [FiscalYearCode] nvarchar(40) NOT NULL,
    [PeriodNo] int NOT NULL,
    [PeriodCode] nvarchar(40) NOT NULL,
    [StartDateUtc] datetime2 NOT NULL,
    [EndDateUtc] datetime2 NOT NULL,
    [IsOpen] bit NOT NULL,
    [IsClosed] bit NOT NULL,
    [IsArchived] bit NOT NULL,
    [CreatedAtUtc] datetime2 NOT NULL,
    [ModifiedAtUtc] datetime2 NOT NULL,
    [RowVersion] rowversion NULL,
    CONSTRAINT [PK_CompanyFiscalPeriods] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_CompanyFiscalPeriods_Companies_CompanyId] FOREIGN KEY ([CompanyId]) REFERENCES [Companies] ([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [CompanySettings] (
    [Id] uniqueidentifier NOT NULL,
    [CompanyId] uniqueidentifier NULL,
    [SettingKey] nvarchar(120) NOT NULL,
    [ValueJson] nvarchar(max) NULL,
    [MetadataJson] nvarchar(max) NULL,
    [IsArchived] bit NOT NULL,
    [CreatedAtUtc] datetime2 NOT NULL,
    [ModifiedAtUtc] datetime2 NOT NULL,
    [RowVersion] rowversion NULL,
    CONSTRAINT [PK_CompanySettings] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_CompanySettings_Companies_CompanyId] FOREIGN KEY ([CompanyId]) REFERENCES [Companies] ([Id]) ON DELETE CASCADE
);
GO

UPDATE [Branches] SET [CreatedAtUtc] = '2026-05-22T00:00:00.0000000Z', [IsArchived] = CAST(0 AS bit), [ModifiedAtUtc] = '2026-05-22T00:00:00.0000000Z', [ParentBranchId] = NULL
WHERE [Id] = '22222222-2222-2222-2222-222222222222';
SELECT @@ROWCOUNT;

GO

UPDATE [Companies] SET [AccountingStandardProfile] = NULL, [CreatedAtUtc] = '2026-05-22T00:00:00.0000000Z', [DefaultCurrencyCode] = NULL, [FiscalConfigJson] = NULL, [IsArchived] = CAST(0 AS bit), [Locale] = NULL, [ModifiedAtUtc] = '2026-05-22T00:00:00.0000000Z', [TimeZoneId] = NULL
WHERE [Id] = '11111111-1111-1111-1111-111111111111';
SELECT @@ROWCOUNT;

GO

CREATE INDEX [IX_UserCompanyAccesses_IsArchived] ON [UserCompanyAccesses] ([IsArchived]);
GO

CREATE INDEX [IX_UserBranchAccesses_IsArchived] ON [UserBranchAccesses] ([IsArchived]);
GO

CREATE INDEX [IX_Companies_IsArchived] ON [Companies] ([IsArchived]);
GO

CREATE INDEX [IX_Branches_IsArchived] ON [Branches] ([IsArchived]);
GO

CREATE INDEX [IX_Branches_ParentBranchId] ON [Branches] ([ParentBranchId]);
GO

CREATE INDEX [IX_ActiveCompanySessions_BranchId] ON [ActiveCompanySessions] ([BranchId]);
GO

CREATE INDEX [IX_ActiveCompanySessions_CompanyId_BranchId_IsActive] ON [ActiveCompanySessions] ([CompanyId], [BranchId], [IsActive]);
GO

CREATE INDEX [IX_ActiveCompanySessions_ExpiresAtUtc] ON [ActiveCompanySessions] ([ExpiresAtUtc]);
GO

CREATE INDEX [IX_ActiveCompanySessions_IsArchived] ON [ActiveCompanySessions] ([IsArchived]);
GO

CREATE INDEX [IX_ActiveCompanySessions_Status] ON [ActiveCompanySessions] ([Status]);
GO

CREATE UNIQUE INDEX [IX_ActiveCompanySessions_UserId_SessionId] ON [ActiveCompanySessions] ([UserId], [SessionId]) WHERE [SessionId] IS NOT NULL;
GO

CREATE INDEX [IX_BranchSettings_BranchId] ON [BranchSettings] ([BranchId]);
GO

CREATE UNIQUE INDEX [IX_BranchSettings_CompanyId_BranchId_SettingKey] ON [BranchSettings] ([CompanyId], [BranchId], [SettingKey]) WHERE [CompanyId] IS NOT NULL AND [BranchId] IS NOT NULL;
GO

CREATE INDEX [IX_BranchSettings_IsArchived] ON [BranchSettings] ([IsArchived]);
GO

CREATE UNIQUE INDEX [IX_CompanyFeatureFlags_CompanyId_FeatureKey] ON [CompanyFeatureFlags] ([CompanyId], [FeatureKey]) WHERE [CompanyId] IS NOT NULL;
GO

CREATE INDEX [IX_CompanyFeatureFlags_IsArchived] ON [CompanyFeatureFlags] ([IsArchived]);
GO

CREATE UNIQUE INDEX [IX_CompanyFiscalPeriods_CompanyId_FiscalYearCode_PeriodNo] ON [CompanyFiscalPeriods] ([CompanyId], [FiscalYearCode], [PeriodNo]) WHERE [CompanyId] IS NOT NULL;
GO

CREATE INDEX [IX_CompanyFiscalPeriods_CompanyId_PeriodCode] ON [CompanyFiscalPeriods] ([CompanyId], [PeriodCode]);
GO

CREATE INDEX [IX_CompanyFiscalPeriods_IsArchived] ON [CompanyFiscalPeriods] ([IsArchived]);
GO

CREATE UNIQUE INDEX [IX_CompanySettings_CompanyId_SettingKey] ON [CompanySettings] ([CompanyId], [SettingKey]) WHERE [CompanyId] IS NOT NULL;
GO

CREATE INDEX [IX_CompanySettings_IsArchived] ON [CompanySettings] ([IsArchived]);
GO

ALTER TABLE [Branches] ADD CONSTRAINT [FK_Branches_Branches_ParentBranchId] FOREIGN KEY ([ParentBranchId]) REFERENCES [Branches] ([Id]) ON DELETE NO ACTION;
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260523111335_AddEnterpriseMultiCompanyBranchHardening', N'8.0.7');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

CREATE TABLE [TT99AccountMappings] (
    [Id] uniqueidentifier NOT NULL,
    [CompanyId] uniqueidentifier NULL,
    [BranchId] uniqueidentifier NULL,
    [AccountCode] nvarchar(40) NOT NULL,
    [TT99Category] nvarchar(80) NOT NULL,
    [TT99ReportSection] nvarchar(80) NOT NULL,
    [TT99DeclarationGroup] nvarchar(80) NOT NULL,
    [IsActive] bit NOT NULL,
    [CreatedAtUtc] datetime2 NOT NULL,
    [UpdatedAtUtc] datetime2 NOT NULL,
    [RowVersion] rowversion NULL,
    CONSTRAINT [PK_TT99AccountMappings] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [TT99ComplianceIssues] (
    [Id] uniqueidentifier NOT NULL,
    [CompanyId] uniqueidentifier NULL,
    [BranchId] uniqueidentifier NULL,
    [RuleCode] nvarchar(120) NOT NULL,
    [Severity] nvarchar(20) NOT NULL,
    [Module] nvarchar(120) NOT NULL,
    [Message] nvarchar(2000) NOT NULL,
    [AffectedEntity] nvarchar(250) NULL,
    [MetadataJson] nvarchar(max) NULL,
    [CorrelationId] nvarchar(100) NULL,
    [DetectedAtUtc] datetime2 NOT NULL,
    CONSTRAINT [PK_TT99ComplianceIssues] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [TT99ComplianceSnapshots] (
    [Id] uniqueidentifier NOT NULL,
    [CompanyId] uniqueidentifier NULL,
    [BranchId] uniqueidentifier NULL,
    [PeriodCode] nvarchar(20) NOT NULL,
    [SnapshotHash] nvarchar(64) NOT NULL,
    [ComplianceScore] decimal(18,4) NOT NULL,
    [DeclarationStatus] nvarchar(40) NOT NULL,
    [SnapshotJson] nvarchar(max) NOT NULL,
    [CorrelationId] nvarchar(100) NULL,
    [CreatedAtUtc] datetime2 NOT NULL,
    CONSTRAINT [PK_TT99ComplianceSnapshots] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [TT99DeclarationPeriods] (
    [Id] uniqueidentifier NOT NULL,
    [CompanyId] uniqueidentifier NULL,
    [BranchId] uniqueidentifier NULL,
    [PeriodCode] nvarchar(20) NOT NULL,
    [FromDateUtc] datetime2 NOT NULL,
    [ToDateUtc] datetime2 NOT NULL,
    [Status] nvarchar(40) NOT NULL,
    [CreatedAtUtc] datetime2 NOT NULL,
    [UpdatedAtUtc] datetime2 NOT NULL,
    [RowVersion] rowversion NULL,
    CONSTRAINT [PK_TT99DeclarationPeriods] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [TT99ExportManifests] (
    [Id] uniqueidentifier NOT NULL,
    [CompanyId] uniqueidentifier NULL,
    [BranchId] uniqueidentifier NULL,
    [ExportType] nvarchar(60) NOT NULL,
    [PeriodCode] nvarchar(20) NOT NULL,
    [ExportedBy] nvarchar(120) NULL,
    [ExportedAtUtc] datetime2 NOT NULL,
    [Hash] nvarchar(64) NOT NULL,
    [RowCount] int NOT NULL,
    [ComplianceVersion] nvarchar(40) NOT NULL,
    [CorrelationId] nvarchar(100) NULL,
    [MetadataJson] nvarchar(max) NULL,
    CONSTRAINT [PK_TT99ExportManifests] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [TT99ReportTemplates] (
    [Id] uniqueidentifier NOT NULL,
    [CompanyId] uniqueidentifier NULL,
    [BranchId] uniqueidentifier NULL,
    [ReportId] nvarchar(80) NOT NULL,
    [Name] nvarchar(250) NOT NULL,
    [Description] nvarchar(1000) NOT NULL,
    [IsActive] bit NOT NULL,
    [CreatedAtUtc] datetime2 NOT NULL,
    [UpdatedAtUtc] datetime2 NOT NULL,
    [RowVersion] rowversion NULL,
    CONSTRAINT [PK_TT99ReportTemplates] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [TT99ValidationRules] (
    [Id] uniqueidentifier NOT NULL,
    [CompanyId] uniqueidentifier NULL,
    [BranchId] uniqueidentifier NULL,
    [RuleCode] nvarchar(120) NOT NULL,
    [RuleName] nvarchar(300) NOT NULL,
    [Severity] nvarchar(20) NOT NULL,
    [Module] nvarchar(120) NOT NULL,
    [MetadataJson] nvarchar(max) NULL,
    [IsEnabled] bit NOT NULL,
    [CreatedAtUtc] datetime2 NOT NULL,
    [UpdatedAtUtc] datetime2 NOT NULL,
    CONSTRAINT [PK_TT99ValidationRules] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [TT99ReportLines] (
    [Id] uniqueidentifier NOT NULL,
    [TT99ReportTemplateId] uniqueidentifier NOT NULL,
    [CompanyId] uniqueidentifier NULL,
    [BranchId] uniqueidentifier NULL,
    [LineCode] nvarchar(80) NOT NULL,
    [LineName] nvarchar(300) NOT NULL,
    [SortOrder] int NOT NULL,
    [Formula] nvarchar(2000) NULL,
    [MappingJson] nvarchar(max) NULL,
    CONSTRAINT [PK_TT99ReportLines] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_TT99ReportLines_TT99ReportTemplates_TT99ReportTemplateId] FOREIGN KEY ([TT99ReportTemplateId]) REFERENCES [TT99ReportTemplates] ([Id]) ON DELETE CASCADE
);
GO

IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Category', N'Code', N'CompanyId', N'CreatedAt', N'DataSource', N'Description', N'IsActive', N'IsSystem', N'LayoutDefinition', N'Name', N'QueryDefinition', N'UpdatedAt') AND [object_id] = OBJECT_ID(N'[ReportTemplates]'))
    SET IDENTITY_INSERT [ReportTemplates] ON;
INSERT INTO [ReportTemplates] ([Id], [Category], [Code], [CompanyId], [CreatedAt], [DataSource], [Description], [IsActive], [IsSystem], [LayoutDefinition], [Name], [QueryDefinition], [UpdatedAt])
VALUES ('4a9a7b4f-ac6e-4e91-9a11-000000000112', N'Compliance', N'TT99_BALANCE_SHEET', NULL, '2026-05-23T00:00:00.0000000Z', N'FinancialStatement', N'Bảng cân đối kế toán theo nền TT99 compliance.', CAST(1 AS bit), CAST(1 AS bit), NULL, N'Bảng cân đối kế toán TT99', NULL, '2026-05-23T00:00:00.0000000Z'),
('4a9a7b4f-ac6e-4e91-9a11-000000000113', N'Compliance', N'TT99_INCOME_STATEMENT', NULL, '2026-05-23T00:00:00.0000000Z', N'FinancialStatement', N'Báo cáo kết quả kinh doanh theo nền TT99 compliance.', CAST(1 AS bit), CAST(1 AS bit), NULL, N'Báo cáo kết quả kinh doanh TT99', NULL, '2026-05-23T00:00:00.0000000Z'),
('4a9a7b4f-ac6e-4e91-9a11-000000000114', N'Compliance', N'TT99_CASH_FLOW', NULL, '2026-05-23T00:00:00.0000000Z', N'FinancialStatement', N'Báo cáo lưu chuyển tiền tệ theo nền TT99 compliance.', CAST(1 AS bit), CAST(1 AS bit), NULL, N'Báo cáo lưu chuyển tiền tệ TT99', NULL, '2026-05-23T00:00:00.0000000Z'),
('4a9a7b4f-ac6e-4e91-9a11-000000000115', N'Compliance', N'TT99_ACCOUNT_MAPPING', NULL, '2026-05-23T00:00:00.0000000Z', N'TT99', N'Đối chiếu mapping tài khoản theo TT99.', CAST(1 AS bit), CAST(1 AS bit), NULL, N'Đối chiếu mapping tài khoản TT99', NULL, '2026-05-23T00:00:00.0000000Z'),
('4a9a7b4f-ac6e-4e91-9a11-000000000116', N'Compliance', N'TT99_COMPLIANCE_ISSUES', NULL, '2026-05-23T00:00:00.0000000Z', N'TT99', N'Danh sách issue tuân thủ TT99.', CAST(1 AS bit), CAST(1 AS bit), NULL, N'Danh sách lỗi tuân thủ TT99', NULL, '2026-05-23T00:00:00.0000000Z'),
('4a9a7b4f-ac6e-4e91-9a11-000000000117', N'Compliance', N'TT99_DECLARATION_SUMMARY', NULL, '2026-05-23T00:00:00.0000000Z', N'TT99', N'Tổng hợp kỳ kê khai phục vụ nền TT99.', CAST(1 AS bit), CAST(1 AS bit), NULL, N'Tổng hợp kỳ kê khai TT99', NULL, '2026-05-23T00:00:00.0000000Z');
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Category', N'Code', N'CompanyId', N'CreatedAt', N'DataSource', N'Description', N'IsActive', N'IsSystem', N'LayoutDefinition', N'Name', N'QueryDefinition', N'UpdatedAt') AND [object_id] = OBJECT_ID(N'[ReportTemplates]'))
    SET IDENTITY_INSERT [ReportTemplates] OFF;
GO

CREATE UNIQUE INDEX [IX_TT99AccountMappings_CompanyId_BranchId_AccountCode] ON [TT99AccountMappings] ([CompanyId], [BranchId], [AccountCode]) WHERE [CompanyId] IS NOT NULL AND [BranchId] IS NOT NULL;
GO

CREATE INDEX [IX_TT99AccountMappings_CompanyId_BranchId_TT99Category] ON [TT99AccountMappings] ([CompanyId], [BranchId], [TT99Category]);
GO

CREATE INDEX [IX_TT99ComplianceIssues_CompanyId_BranchId_DetectedAtUtc] ON [TT99ComplianceIssues] ([CompanyId], [BranchId], [DetectedAtUtc]);
GO

CREATE INDEX [IX_TT99ComplianceIssues_RuleCode_Severity] ON [TT99ComplianceIssues] ([RuleCode], [Severity]);
GO

CREATE INDEX [IX_TT99ComplianceSnapshots_CompanyId_BranchId_PeriodCode_CreatedAtUtc] ON [TT99ComplianceSnapshots] ([CompanyId], [BranchId], [PeriodCode], [CreatedAtUtc]);
GO

CREATE UNIQUE INDEX [IX_TT99DeclarationPeriods_CompanyId_BranchId_PeriodCode] ON [TT99DeclarationPeriods] ([CompanyId], [BranchId], [PeriodCode]) WHERE [CompanyId] IS NOT NULL AND [BranchId] IS NOT NULL;
GO

CREATE INDEX [IX_TT99ExportManifests_CompanyId_BranchId_PeriodCode_ExportedAtUtc] ON [TT99ExportManifests] ([CompanyId], [BranchId], [PeriodCode], [ExportedAtUtc]);
GO

CREATE INDEX [IX_TT99ReportLines_CompanyId_BranchId_LineCode] ON [TT99ReportLines] ([CompanyId], [BranchId], [LineCode]);
GO

CREATE INDEX [IX_TT99ReportLines_TT99ReportTemplateId] ON [TT99ReportLines] ([TT99ReportTemplateId]);
GO

CREATE UNIQUE INDEX [IX_TT99ReportTemplates_CompanyId_BranchId_ReportId] ON [TT99ReportTemplates] ([CompanyId], [BranchId], [ReportId]) WHERE [CompanyId] IS NOT NULL AND [BranchId] IS NOT NULL;
GO

CREATE UNIQUE INDEX [IX_TT99ValidationRules_CompanyId_BranchId_RuleCode] ON [TT99ValidationRules] ([CompanyId], [BranchId], [RuleCode]) WHERE [CompanyId] IS NOT NULL AND [BranchId] IS NOT NULL;
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260523145315_AddTt99ComplianceFoundation', N'8.0.7');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

CREATE TABLE [TT200AccountMappings] (
    [Id] uniqueidentifier NOT NULL,
    [CompanyId] uniqueidentifier NULL,
    [BranchId] uniqueidentifier NULL,
    [AccountCode] nvarchar(40) NOT NULL,
    [TT200Category] nvarchar(80) NOT NULL,
    [TT200ReportSection] nvarchar(80) NOT NULL,
    [TT200ClosingGroup] nvarchar(80) NOT NULL,
    [IsActive] bit NOT NULL,
    [CreatedAtUtc] datetime2 NOT NULL,
    [UpdatedAtUtc] datetime2 NOT NULL,
    [RowVersion] rowversion NULL,
    CONSTRAINT [PK_TT200AccountMappings] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [TT200ComplianceIssues] (
    [Id] uniqueidentifier NOT NULL,
    [CompanyId] uniqueidentifier NULL,
    [BranchId] uniqueidentifier NULL,
    [RuleCode] nvarchar(120) NOT NULL,
    [Severity] nvarchar(20) NOT NULL,
    [Module] nvarchar(120) NOT NULL,
    [Message] nvarchar(2000) NOT NULL,
    [AffectedEntity] nvarchar(250) NULL,
    [MetadataJson] nvarchar(max) NULL,
    [CorrelationId] nvarchar(100) NULL,
    [DetectedAtUtc] datetime2 NOT NULL,
    CONSTRAINT [PK_TT200ComplianceIssues] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [TT200ComplianceSnapshots] (
    [Id] uniqueidentifier NOT NULL,
    [CompanyId] uniqueidentifier NULL,
    [BranchId] uniqueidentifier NULL,
    [PeriodCode] nvarchar(20) NOT NULL,
    [SnapshotHash] nvarchar(64) NOT NULL,
    [ComplianceScore] decimal(18,4) NOT NULL,
    [DeclarationStatus] nvarchar(40) NOT NULL,
    [SnapshotJson] nvarchar(max) NOT NULL,
    [CorrelationId] nvarchar(100) NULL,
    [CreatedAtUtc] datetime2 NOT NULL,
    CONSTRAINT [PK_TT200ComplianceSnapshots] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [TT200DeclarationPeriods] (
    [Id] uniqueidentifier NOT NULL,
    [CompanyId] uniqueidentifier NULL,
    [BranchId] uniqueidentifier NULL,
    [PeriodCode] nvarchar(20) NOT NULL,
    [FromDateUtc] datetime2 NOT NULL,
    [ToDateUtc] datetime2 NOT NULL,
    [Status] nvarchar(40) NOT NULL,
    [CreatedAtUtc] datetime2 NOT NULL,
    [UpdatedAtUtc] datetime2 NOT NULL,
    [RowVersion] rowversion NULL,
    CONSTRAINT [PK_TT200DeclarationPeriods] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [TT200ExportManifests] (
    [Id] uniqueidentifier NOT NULL,
    [CompanyId] uniqueidentifier NULL,
    [BranchId] uniqueidentifier NULL,
    [ExportType] nvarchar(60) NOT NULL,
    [PeriodCode] nvarchar(20) NOT NULL,
    [ExportedBy] nvarchar(120) NULL,
    [ExportedAtUtc] datetime2 NOT NULL,
    [Hash] nvarchar(64) NOT NULL,
    [RowCount] int NOT NULL,
    [ComplianceVersion] nvarchar(40) NOT NULL,
    [CorrelationId] nvarchar(100) NULL,
    [MetadataJson] nvarchar(max) NULL,
    CONSTRAINT [PK_TT200ExportManifests] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [TT200ReportTemplates] (
    [Id] uniqueidentifier NOT NULL,
    [CompanyId] uniqueidentifier NULL,
    [BranchId] uniqueidentifier NULL,
    [ReportId] nvarchar(80) NOT NULL,
    [Name] nvarchar(250) NOT NULL,
    [Description] nvarchar(1000) NOT NULL,
    [IsActive] bit NOT NULL,
    [CreatedAtUtc] datetime2 NOT NULL,
    [UpdatedAtUtc] datetime2 NOT NULL,
    [RowVersion] rowversion NULL,
    CONSTRAINT [PK_TT200ReportTemplates] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [TT200ValidationRules] (
    [Id] uniqueidentifier NOT NULL,
    [CompanyId] uniqueidentifier NULL,
    [BranchId] uniqueidentifier NULL,
    [RuleCode] nvarchar(120) NOT NULL,
    [RuleName] nvarchar(300) NOT NULL,
    [Severity] nvarchar(20) NOT NULL,
    [Module] nvarchar(120) NOT NULL,
    [MetadataJson] nvarchar(max) NULL,
    [IsEnabled] bit NOT NULL,
    [CreatedAtUtc] datetime2 NOT NULL,
    [UpdatedAtUtc] datetime2 NOT NULL,
    CONSTRAINT [PK_TT200ValidationRules] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [TT200ReportLines] (
    [Id] uniqueidentifier NOT NULL,
    [TT200ReportTemplateId] uniqueidentifier NOT NULL,
    [CompanyId] uniqueidentifier NULL,
    [BranchId] uniqueidentifier NULL,
    [LineCode] nvarchar(80) NOT NULL,
    [LineName] nvarchar(300) NOT NULL,
    [SortOrder] int NOT NULL,
    [Formula] nvarchar(2000) NULL,
    [MappingJson] nvarchar(max) NULL,
    CONSTRAINT [PK_TT200ReportLines] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_TT200ReportLines_TT200ReportTemplates_TT200ReportTemplateId] FOREIGN KEY ([TT200ReportTemplateId]) REFERENCES [TT200ReportTemplates] ([Id]) ON DELETE CASCADE
);
GO

IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Category', N'Code', N'CompanyId', N'CreatedAt', N'DataSource', N'Description', N'IsActive', N'IsSystem', N'LayoutDefinition', N'Name', N'QueryDefinition', N'UpdatedAt') AND [object_id] = OBJECT_ID(N'[ReportTemplates]'))
    SET IDENTITY_INSERT [ReportTemplates] ON;
INSERT INTO [ReportTemplates] ([Id], [Category], [Code], [CompanyId], [CreatedAt], [DataSource], [Description], [IsActive], [IsSystem], [LayoutDefinition], [Name], [QueryDefinition], [UpdatedAt])
VALUES ('4a9a7b4f-ac6e-4e91-9a11-000000000212', N'Compliance', N'TT200_BALANCE_SHEET', NULL, '2026-05-23T00:00:00.0000000Z', N'FinancialStatement', N'Bảng cân đối kế toán theo nền TT200 compliance.', CAST(1 AS bit), CAST(1 AS bit), NULL, N'Bảng cân đối kế toán TT200', NULL, '2026-05-23T00:00:00.0000000Z'),
('4a9a7b4f-ac6e-4e91-9a11-000000000213', N'Compliance', N'TT200_INCOME_STATEMENT', NULL, '2026-05-23T00:00:00.0000000Z', N'FinancialStatement', N'Báo cáo kết quả kinh doanh theo nền TT200 compliance.', CAST(1 AS bit), CAST(1 AS bit), NULL, N'Báo cáo kết quả kinh doanh TT200', NULL, '2026-05-23T00:00:00.0000000Z'),
('4a9a7b4f-ac6e-4e91-9a11-000000000214', N'Compliance', N'TT200_CASH_FLOW', NULL, '2026-05-23T00:00:00.0000000Z', N'FinancialStatement', N'Báo cáo lưu chuyển tiền tệ theo nền TT200 compliance.', CAST(1 AS bit), CAST(1 AS bit), NULL, N'Báo cáo lưu chuyển tiền tệ TT200', NULL, '2026-05-23T00:00:00.0000000Z'),
('4a9a7b4f-ac6e-4e91-9a11-000000000215', N'Compliance', N'TT200_CLOSING_SUMMARY', NULL, '2026-05-23T00:00:00.0000000Z', N'TT200', N'Tổng hợp khóa sổ theo nền TT200 compliance.', CAST(1 AS bit), CAST(1 AS bit), NULL, N'Tổng hợp khóa sổ TT200', NULL, '2026-05-23T00:00:00.0000000Z'),
('4a9a7b4f-ac6e-4e91-9a11-000000000216', N'Compliance', N'TT200_ACCOUNT_MAPPING', NULL, '2026-05-23T00:00:00.0000000Z', N'TT200', N'Đối chiếu mapping tài khoản theo TT200.', CAST(1 AS bit), CAST(1 AS bit), NULL, N'Đối chiếu mapping tài khoản TT200', NULL, '2026-05-23T00:00:00.0000000Z'),
('4a9a7b4f-ac6e-4e91-9a11-000000000217', N'Compliance', N'TT200_COMPLIANCE_ISSUES', NULL, '2026-05-23T00:00:00.0000000Z', N'TT200', N'Danh sách issue tuân thủ TT200.', CAST(1 AS bit), CAST(1 AS bit), NULL, N'Danh sách lỗi tuân thủ TT200', NULL, '2026-05-23T00:00:00.0000000Z'),
('4a9a7b4f-ac6e-4e91-9a11-000000000218', N'Compliance', N'TT200_DECLARATION_SUMMARY', NULL, '2026-05-23T00:00:00.0000000Z', N'TT200', N'Tổng hợp kỳ kê khai phục vụ nền TT200.', CAST(1 AS bit), CAST(1 AS bit), NULL, N'Tổng hợp kỳ kê khai TT200', NULL, '2026-05-23T00:00:00.0000000Z');
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Category', N'Code', N'CompanyId', N'CreatedAt', N'DataSource', N'Description', N'IsActive', N'IsSystem', N'LayoutDefinition', N'Name', N'QueryDefinition', N'UpdatedAt') AND [object_id] = OBJECT_ID(N'[ReportTemplates]'))
    SET IDENTITY_INSERT [ReportTemplates] OFF;
GO

CREATE UNIQUE INDEX [IX_TT200AccountMappings_CompanyId_BranchId_AccountCode] ON [TT200AccountMappings] ([CompanyId], [BranchId], [AccountCode]) WHERE [CompanyId] IS NOT NULL AND [BranchId] IS NOT NULL;
GO

CREATE INDEX [IX_TT200AccountMappings_CompanyId_BranchId_TT200Category] ON [TT200AccountMappings] ([CompanyId], [BranchId], [TT200Category]);
GO

CREATE INDEX [IX_TT200ComplianceIssues_CompanyId_BranchId_DetectedAtUtc] ON [TT200ComplianceIssues] ([CompanyId], [BranchId], [DetectedAtUtc]);
GO

CREATE INDEX [IX_TT200ComplianceIssues_RuleCode_Severity] ON [TT200ComplianceIssues] ([RuleCode], [Severity]);
GO

CREATE INDEX [IX_TT200ComplianceSnapshots_CompanyId_BranchId_PeriodCode_CreatedAtUtc] ON [TT200ComplianceSnapshots] ([CompanyId], [BranchId], [PeriodCode], [CreatedAtUtc]);
GO

CREATE UNIQUE INDEX [IX_TT200DeclarationPeriods_CompanyId_BranchId_PeriodCode] ON [TT200DeclarationPeriods] ([CompanyId], [BranchId], [PeriodCode]) WHERE [CompanyId] IS NOT NULL AND [BranchId] IS NOT NULL;
GO

CREATE INDEX [IX_TT200ExportManifests_CompanyId_BranchId_PeriodCode_ExportedAtUtc] ON [TT200ExportManifests] ([CompanyId], [BranchId], [PeriodCode], [ExportedAtUtc]);
GO

CREATE INDEX [IX_TT200ReportLines_CompanyId_BranchId_LineCode] ON [TT200ReportLines] ([CompanyId], [BranchId], [LineCode]);
GO

CREATE INDEX [IX_TT200ReportLines_TT200ReportTemplateId] ON [TT200ReportLines] ([TT200ReportTemplateId]);
GO

CREATE UNIQUE INDEX [IX_TT200ReportTemplates_CompanyId_BranchId_ReportId] ON [TT200ReportTemplates] ([CompanyId], [BranchId], [ReportId]) WHERE [CompanyId] IS NOT NULL AND [BranchId] IS NOT NULL;
GO

CREATE UNIQUE INDEX [IX_TT200ValidationRules_CompanyId_BranchId_RuleCode] ON [TT200ValidationRules] ([CompanyId], [BranchId], [RuleCode]) WHERE [CompanyId] IS NOT NULL AND [BranchId] IS NOT NULL;
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260523153700_AddTt200ComplianceFoundation', N'8.0.7');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

ALTER TABLE [ActiveCompanySessions] DROP CONSTRAINT [FK_ActiveCompanySessions_Branches_BranchId];
GO

ALTER TABLE [ActiveCompanySessions] DROP CONSTRAINT [FK_ActiveCompanySessions_Companies_CompanyId];
GO

ALTER TABLE [BranchSettings] DROP CONSTRAINT [FK_BranchSettings_Companies_CompanyId];
GO

ALTER TABLE [WorkflowCompensationActions] DROP CONSTRAINT [FK_WorkflowCompensationActions_WorkflowExecutions_WorkflowExecutionId];
GO

ALTER TABLE [WorkflowExecutions] DROP CONSTRAINT [FK_WorkflowExecutions_WorkflowExecutions_ParentWorkflowExecutionId];
GO

ALTER TABLE [WorkflowRetryHistories] DROP CONSTRAINT [FK_WorkflowRetryHistories_WorkflowExecutions_WorkflowExecutionId];
GO

CREATE TABLE [ApprovalDelegations] (
    [Id] uniqueidentifier NOT NULL,
    [CompanyId] uniqueidentifier NULL,
    [BranchId] uniqueidentifier NULL,
    [AssignmentId] uniqueidentifier NOT NULL,
    [FromUserId] uniqueidentifier NOT NULL,
    [ToUserId] uniqueidentifier NOT NULL,
    [DelegatedAtUtc] datetime2 NOT NULL,
    [ExpiresAtUtc] datetime2 NULL,
    [Reason] nvarchar(500) NULL,
    [Status] nvarchar(40) NOT NULL,
    [MetadataJson] nvarchar(max) NULL,
    CONSTRAINT [PK_ApprovalDelegations] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [ApprovalEscalations] (
    [Id] uniqueidentifier NOT NULL,
    [CompanyId] uniqueidentifier NULL,
    [BranchId] uniqueidentifier NULL,
    [WorkflowId] uniqueidentifier NOT NULL,
    [StepId] uniqueidentifier NOT NULL,
    [AssignmentId] uniqueidentifier NULL,
    [EscalatedToUserId] uniqueidentifier NOT NULL,
    [Reason] nvarchar(400) NOT NULL,
    [EscalatedAtUtc] datetime2 NOT NULL,
    [IsResolved] bit NOT NULL,
    [ResolvedAtUtc] datetime2 NULL,
    [MetadataJson] nvarchar(max) NULL,
    CONSTRAINT [PK_ApprovalEscalations] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [ApprovalWorkflows] (
    [Id] uniqueidentifier NOT NULL,
    [CompanyId] uniqueidentifier NULL,
    [BranchId] uniqueidentifier NULL,
    [Module] nvarchar(120) NOT NULL,
    [EntityType] nvarchar(120) NOT NULL,
    [EntityId] nvarchar(160) NOT NULL,
    [Title] nvarchar(300) NOT NULL,
    [Status] nvarchar(40) NOT NULL,
    [CurrentLevel] int NOT NULL,
    [TotalLevels] int NOT NULL,
    [IsCompleted] bit NOT NULL,
    [IsRejected] bit NOT NULL,
    [SubmittedAtUtc] datetime2 NOT NULL,
    [CompletedAtUtc] datetime2 NULL,
    [LastActivityAtUtc] datetime2 NULL,
    [CorrelationId] nvarchar(100) NULL,
    [MetadataJson] nvarchar(max) NULL,
    [RowVersion] rowversion NULL,
    CONSTRAINT [PK_ApprovalWorkflows] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [ApprovalSnapshots] (
    [Id] uniqueidentifier NOT NULL,
    [CompanyId] uniqueidentifier NULL,
    [BranchId] uniqueidentifier NULL,
    [WorkflowId] uniqueidentifier NOT NULL,
    [CapturedAtUtc] datetime2 NOT NULL,
    [Status] nvarchar(40) NOT NULL,
    [CurrentLevel] int NOT NULL,
    [SnapshotJson] nvarchar(max) NOT NULL,
    [SnapshotHash] nvarchar(64) NOT NULL,
    CONSTRAINT [PK_ApprovalSnapshots] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ApprovalSnapshots_ApprovalWorkflows_WorkflowId] FOREIGN KEY ([WorkflowId]) REFERENCES [ApprovalWorkflows] ([Id])
);
GO

CREATE TABLE [ApprovalSteps] (
    [Id] uniqueidentifier NOT NULL,
    [CompanyId] uniqueidentifier NULL,
    [BranchId] uniqueidentifier NULL,
    [WorkflowId] uniqueidentifier NOT NULL,
    [Level] int NOT NULL,
    [StepName] nvarchar(180) NOT NULL,
    [Status] nvarchar(40) NOT NULL,
    [StartedAtUtc] datetime2 NULL,
    [CompletedAtUtc] datetime2 NULL,
    [DueAtUtc] datetime2 NULL,
    [Comment] nvarchar(1000) NULL,
    [MetadataJson] nvarchar(max) NULL,
    CONSTRAINT [PK_ApprovalSteps] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ApprovalSteps_ApprovalWorkflows_WorkflowId] FOREIGN KEY ([WorkflowId]) REFERENCES [ApprovalWorkflows] ([Id])
);
GO

CREATE TABLE [ApprovalAssignments] (
    [Id] uniqueidentifier NOT NULL,
    [CompanyId] uniqueidentifier NULL,
    [BranchId] uniqueidentifier NULL,
    [WorkflowId] uniqueidentifier NOT NULL,
    [StepId] uniqueidentifier NOT NULL,
    [AssignedUserId] uniqueidentifier NOT NULL,
    [IsPrimary] bit NOT NULL,
    [Status] nvarchar(40) NOT NULL,
    [AssignedAtUtc] datetime2 NOT NULL,
    [ActedAtUtc] datetime2 NULL,
    [ActionComment] nvarchar(1000) NULL,
    CONSTRAINT [PK_ApprovalAssignments] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ApprovalAssignments_ApprovalSteps_StepId] FOREIGN KEY ([StepId]) REFERENCES [ApprovalSteps] ([Id]),
    CONSTRAINT [FK_ApprovalAssignments_ApprovalWorkflows_WorkflowId] FOREIGN KEY ([WorkflowId]) REFERENCES [ApprovalWorkflows] ([Id])
);
GO

CREATE TABLE [ApprovalHistory] (
    [Id] uniqueidentifier NOT NULL,
    [CompanyId] uniqueidentifier NULL,
    [BranchId] uniqueidentifier NULL,
    [WorkflowId] uniqueidentifier NOT NULL,
    [StepId] uniqueidentifier NULL,
    [AssignmentId] uniqueidentifier NULL,
    [Action] nvarchar(80) NOT NULL,
    [ActorUserId] uniqueidentifier NULL,
    [OccurredAtUtc] datetime2 NOT NULL,
    [Comment] nvarchar(1000) NULL,
    [MetadataJson] nvarchar(max) NULL,
    CONSTRAINT [PK_ApprovalHistory] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ApprovalHistory_ApprovalSteps_StepId] FOREIGN KEY ([StepId]) REFERENCES [ApprovalSteps] ([Id]),
    CONSTRAINT [FK_ApprovalHistory_ApprovalWorkflows_WorkflowId] FOREIGN KEY ([WorkflowId]) REFERENCES [ApprovalWorkflows] ([Id])
);
GO

CREATE INDEX [IX_ApprovalAssignments_StepId_AssignedUserId] ON [ApprovalAssignments] ([StepId], [AssignedUserId]);
GO

CREATE INDEX [IX_ApprovalAssignments_WorkflowId] ON [ApprovalAssignments] ([WorkflowId]);
GO

CREATE INDEX [IX_ApprovalDelegations_AssignmentId_DelegatedAtUtc] ON [ApprovalDelegations] ([AssignmentId], [DelegatedAtUtc]);
GO

CREATE INDEX [IX_ApprovalEscalations_WorkflowId_StepId_EscalatedAtUtc] ON [ApprovalEscalations] ([WorkflowId], [StepId], [EscalatedAtUtc]);
GO

CREATE INDEX [IX_ApprovalHistory_StepId] ON [ApprovalHistory] ([StepId]);
GO

CREATE INDEX [IX_ApprovalHistory_WorkflowId_OccurredAtUtc] ON [ApprovalHistory] ([WorkflowId], [OccurredAtUtc]);
GO

CREATE INDEX [IX_ApprovalSnapshots_WorkflowId_CapturedAtUtc] ON [ApprovalSnapshots] ([WorkflowId], [CapturedAtUtc]);
GO

CREATE INDEX [IX_ApprovalSteps_WorkflowId_Level] ON [ApprovalSteps] ([WorkflowId], [Level]);
GO

CREATE INDEX [IX_ApprovalWorkflows_CompanyId_BranchId_Module_SubmittedAtUtc] ON [ApprovalWorkflows] ([CompanyId], [BranchId], [Module], [SubmittedAtUtc]);
GO

CREATE INDEX [IX_ApprovalWorkflows_CorrelationId] ON [ApprovalWorkflows] ([CorrelationId]);
GO

ALTER TABLE [ActiveCompanySessions] ADD CONSTRAINT [FK_ActiveCompanySessions_Branches_BranchId] FOREIGN KEY ([BranchId]) REFERENCES [Branches] ([Id]);
GO

ALTER TABLE [ActiveCompanySessions] ADD CONSTRAINT [FK_ActiveCompanySessions_Companies_CompanyId] FOREIGN KEY ([CompanyId]) REFERENCES [Companies] ([Id]);
GO

ALTER TABLE [BranchSettings] ADD CONSTRAINT [FK_BranchSettings_Companies_CompanyId] FOREIGN KEY ([CompanyId]) REFERENCES [Companies] ([Id]);
GO

ALTER TABLE [WorkflowCompensationActions] ADD CONSTRAINT [FK_WorkflowCompensationActions_WorkflowExecutions_WorkflowExecutionId] FOREIGN KEY ([WorkflowExecutionId]) REFERENCES [WorkflowExecutions] ([Id]);
GO

ALTER TABLE [WorkflowExecutions] ADD CONSTRAINT [FK_WorkflowExecutions_WorkflowExecutions_ParentWorkflowExecutionId] FOREIGN KEY ([ParentWorkflowExecutionId]) REFERENCES [WorkflowExecutions] ([Id]);
GO

ALTER TABLE [WorkflowRetryHistories] ADD CONSTRAINT [FK_WorkflowRetryHistories_WorkflowExecutions_WorkflowExecutionId] FOREIGN KEY ([WorkflowExecutionId]) REFERENCES [WorkflowExecutions] ([Id]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260524030741_AddEnterpriseApprovalWorkflowFoundation', N'8.0.7');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

CREATE TABLE [FinancialTemplates] (
    [Id] uniqueidentifier NOT NULL,
    [CompanyId] uniqueidentifier NULL,
    [BranchId] uniqueidentifier NULL,
    [StandardCode] nvarchar(20) NOT NULL,
    [TemplateCode] nvarchar(120) NOT NULL,
    [Name] nvarchar(250) NOT NULL,
    [StatementType] nvarchar(80) NOT NULL,
    [IsActive] bit NOT NULL,
    [CreatedAtUtc] datetime2 NOT NULL,
    [UpdatedAtUtc] datetime2 NOT NULL,
    [RowVersion] rowversion NULL,
    CONSTRAINT [PK_FinancialTemplates] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [FinancialTemplateVersions] (
    [Id] uniqueidentifier NOT NULL,
    [CompanyId] uniqueidentifier NULL,
    [BranchId] uniqueidentifier NULL,
    [FinancialTemplateId] uniqueidentifier NOT NULL,
    [VersionCode] nvarchar(60) NOT NULL,
    [IsActiveVersion] bit NOT NULL,
    [EffectiveFromUtc] datetime2 NOT NULL,
    [EffectiveToUtc] datetime2 NULL,
    [HierarchyJson] nvarchar(max) NULL,
    [MetadataJson] nvarchar(max) NULL,
    [CreatedAtUtc] datetime2 NOT NULL,
    CONSTRAINT [PK_FinancialTemplateVersions] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_FinancialTemplateVersions_FinancialTemplates_FinancialTemplateId] FOREIGN KEY ([FinancialTemplateId]) REFERENCES [FinancialTemplates] ([Id])
);
GO

CREATE TABLE [FinancialStatementFormulas] (
    [Id] uniqueidentifier NOT NULL,
    [CompanyId] uniqueidentifier NULL,
    [BranchId] uniqueidentifier NULL,
    [FinancialTemplateVersionId] uniqueidentifier NOT NULL,
    [LineCode] nvarchar(120) NOT NULL,
    [Expression] nvarchar(1200) NOT NULL,
    [SortOrder] int NOT NULL,
    [MetadataJson] nvarchar(max) NULL,
    CONSTRAINT [PK_FinancialStatementFormulas] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_FinancialStatementFormulas_FinancialTemplateVersions_FinancialTemplateVersionId] FOREIGN KEY ([FinancialTemplateVersionId]) REFERENCES [FinancialTemplateVersions] ([Id])
);
GO

CREATE TABLE [FinancialStatementSnapshots] (
    [Id] uniqueidentifier NOT NULL,
    [CompanyId] uniqueidentifier NULL,
    [BranchId] uniqueidentifier NULL,
    [FinancialTemplateVersionId] uniqueidentifier NOT NULL,
    [PeriodCode] nvarchar(20) NOT NULL,
    [CapturedAtUtc] datetime2 NOT NULL,
    [SnapshotJson] nvarchar(max) NOT NULL,
    [SnapshotHash] nvarchar(64) NOT NULL,
    [CorrelationId] nvarchar(100) NULL,
    CONSTRAINT [PK_FinancialStatementSnapshots] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_FinancialStatementSnapshots_FinancialTemplateVersions_FinancialTemplateVersionId] FOREIGN KEY ([FinancialTemplateVersionId]) REFERENCES [FinancialTemplateVersions] ([Id])
);
GO

CREATE INDEX [IX_FinancialStatementFormulas_FinancialTemplateVersionId_LineCode] ON [FinancialStatementFormulas] ([FinancialTemplateVersionId], [LineCode]);
GO

CREATE INDEX [IX_FinancialStatementSnapshots_FinancialTemplateVersionId_PeriodCode_CapturedAtUtc] ON [FinancialStatementSnapshots] ([FinancialTemplateVersionId], [PeriodCode], [CapturedAtUtc]);
GO

CREATE UNIQUE INDEX [IX_FinancialTemplates_CompanyId_BranchId_StandardCode_TemplateCode] ON [FinancialTemplates] ([CompanyId], [BranchId], [StandardCode], [TemplateCode]) WHERE [CompanyId] IS NOT NULL AND [BranchId] IS NOT NULL;
GO

CREATE UNIQUE INDEX [IX_FinancialTemplateVersions_FinancialTemplateId_VersionCode] ON [FinancialTemplateVersions] ([FinancialTemplateId], [VersionCode]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260524060743_AddAdvancedFinancialStatementMappingFoundation', N'8.0.7');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

CREATE TABLE [ComplianceArchives] (
    [Id] uniqueidentifier NOT NULL,
    [CompanyId] uniqueidentifier NULL,
    [BranchId] uniqueidentifier NULL,
    [CompliancePackageId] uniqueidentifier NOT NULL,
    [ArchivePath] nvarchar(1000) NOT NULL,
    [Hash] nvarchar(64) NOT NULL,
    [CreatedAtUtc] datetime2 NOT NULL,
    [IsVerified] bit NOT NULL,
    CONSTRAINT [PK_ComplianceArchives] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [ComplianceManifests] (
    [Id] uniqueidentifier NOT NULL,
    [CompanyId] uniqueidentifier NULL,
    [BranchId] uniqueidentifier NULL,
    [CompliancePackageId] uniqueidentifier NOT NULL,
    [ManifestType] nvarchar(50) NOT NULL,
    [FilePath] nvarchar(1000) NOT NULL,
    [Hash] nvarchar(64) NOT NULL,
    [RowCount] bigint NOT NULL,
    [CreatedAtUtc] datetime2 NOT NULL,
    CONSTRAINT [PK_ComplianceManifests] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [CompliancePackages] (
    [Id] uniqueidentifier NOT NULL,
    [CompanyId] uniqueidentifier NULL,
    [BranchId] uniqueidentifier NULL,
    [PackageCode] nvarchar(120) NOT NULL,
    [StandardCode] nvarchar(20) NOT NULL,
    [PeriodCode] nvarchar(20) NOT NULL,
    [Status] nvarchar(40) NOT NULL,
    [CreatedAtUtc] datetime2 NOT NULL,
    [CompletedAtUtc] datetime2 NULL,
    [CorrelationId] nvarchar(100) NULL,
    CONSTRAINT [PK_CompliancePackages] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [ComplianceSubmissions] (
    [Id] uniqueidentifier NOT NULL,
    [CompanyId] uniqueidentifier NULL,
    [BranchId] uniqueidentifier NULL,
    [CompliancePackageId] uniqueidentifier NOT NULL,
    [SubmissionChannel] nvarchar(80) NOT NULL,
    [Status] nvarchar(40) NOT NULL,
    [ReferenceNo] nvarchar(120) NULL,
    [CreatedAtUtc] datetime2 NOT NULL,
    [MetadataJson] nvarchar(max) NULL,
    CONSTRAINT [PK_ComplianceSubmissions] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [DataIntegrityCheckpoints] (
    [Id] uniqueidentifier NOT NULL,
    [CompanyId] uniqueidentifier NULL,
    [BranchId] uniqueidentifier NULL,
    [CheckpointCode] nvarchar(120) NOT NULL,
    [CreatedAtUtc] datetime2 NOT NULL,
    [SnapshotHash] nvarchar(64) NOT NULL,
    [SnapshotJson] nvarchar(max) NOT NULL,
    CONSTRAINT [PK_DataIntegrityCheckpoints] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [DataIntegrityScanResults] (
    [Id] uniqueidentifier NOT NULL,
    [CompanyId] uniqueidentifier NULL,
    [BranchId] uniqueidentifier NULL,
    [Module] nvarchar(80) NOT NULL,
    [Severity] nvarchar(30) NOT NULL,
    [Category] nvarchar(80) NOT NULL,
    [Message] nvarchar(1000) NOT NULL,
    [EntityType] nvarchar(120) NULL,
    [EntityId] nvarchar(120) NULL,
    [IsCorrupted] bit NOT NULL,
    [ScannedAtUtc] datetime2 NOT NULL,
    [MetadataJson] nvarchar(max) NULL,
    CONSTRAINT [PK_DataIntegrityScanResults] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [DataRepairOperations] (
    [Id] uniqueidentifier NOT NULL,
    [CompanyId] uniqueidentifier NULL,
    [BranchId] uniqueidentifier NULL,
    [OperationCode] nvarchar(120) NOT NULL,
    [Status] nvarchar(40) NOT NULL,
    [RequestedBy] nvarchar(120) NULL,
    [RequestedAtUtc] datetime2 NOT NULL,
    [CompletedAtUtc] datetime2 NULL,
    [ResultJson] nvarchar(max) NULL,
    [ErrorMessage] nvarchar(2000) NULL,
    [CorrelationId] nvarchar(100) NULL,
    CONSTRAINT [PK_DataRepairOperations] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [ExportArtifacts] (
    [Id] uniqueidentifier NOT NULL,
    [CompanyId] uniqueidentifier NULL,
    [BranchId] uniqueidentifier NULL,
    [ExportExecutionId] uniqueidentifier NOT NULL,
    [FilePath] nvarchar(1000) NOT NULL,
    [FileType] nvarchar(20) NOT NULL,
    [SizeBytes] bigint NOT NULL,
    [Hash] nvarchar(64) NOT NULL,
    [CreatedAtUtc] datetime2 NOT NULL,
    CONSTRAINT [PK_ExportArtifacts] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [ExportExecutions] (
    [Id] uniqueidentifier NOT NULL,
    [CompanyId] uniqueidentifier NULL,
    [BranchId] uniqueidentifier NULL,
    [ExportCode] nvarchar(120) NOT NULL,
    [ExportType] nvarchar(50) NOT NULL,
    [Status] nvarchar(40) NOT NULL,
    [RetryCount] int NOT NULL,
    [QueuedAtUtc] datetime2 NOT NULL,
    [StartedAtUtc] datetime2 NULL,
    [CompletedAtUtc] datetime2 NULL,
    [ErrorMessage] nvarchar(2000) NULL,
    [CorrelationId] nvarchar(100) NULL,
    CONSTRAINT [PK_ExportExecutions] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [ExportRetentionPolicies] (
    [Id] uniqueidentifier NOT NULL,
    [CompanyId] uniqueidentifier NULL,
    [BranchId] uniqueidentifier NULL,
    [ExportCode] nvarchar(120) NOT NULL,
    [RetentionDays] int NOT NULL,
    [IsActive] bit NOT NULL,
    [UpdatedAtUtc] datetime2 NOT NULL,
    CONSTRAINT [PK_ExportRetentionPolicies] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [ProductionCertificationReports] (
    [Id] uniqueidentifier NOT NULL,
    [CompanyId] uniqueidentifier NULL,
    [BranchId] uniqueidentifier NULL,
    [PhaseCode] nvarchar(40) NOT NULL,
    [Status] nvarchar(40) NOT NULL,
    [GeneratedAtUtc] datetime2 NOT NULL,
    [ReportJson] nvarchar(max) NOT NULL,
    CONSTRAINT [PK_ProductionCertificationReports] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [ReconciliationAnomalies] (
    [Id] uniqueidentifier NOT NULL,
    [CompanyId] uniqueidentifier NULL,
    [BranchId] uniqueidentifier NULL,
    [SessionId] uniqueidentifier NOT NULL,
    [Severity] nvarchar(30) NOT NULL,
    [Category] nvarchar(80) NOT NULL,
    [Message] nvarchar(1000) NOT NULL,
    [RelatedEntityType] nvarchar(120) NULL,
    [RelatedEntityId] nvarchar(120) NULL,
    [DetectedAtUtc] datetime2 NOT NULL,
    [MetadataJson] nvarchar(max) NULL,
    CONSTRAINT [PK_ReconciliationAnomalies] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [ReconciliationMatches] (
    [Id] uniqueidentifier NOT NULL,
    [CompanyId] uniqueidentifier NULL,
    [BranchId] uniqueidentifier NULL,
    [SessionId] uniqueidentifier NOT NULL,
    [SourceType] nvarchar(80) NOT NULL,
    [SourceId] nvarchar(120) NOT NULL,
    [TargetType] nvarchar(80) NOT NULL,
    [TargetId] nvarchar(120) NOT NULL,
    [MatchScore] decimal(18,6) NOT NULL,
    [IsFuzzy] bit NOT NULL,
    [Status] nvarchar(40) NOT NULL,
    [MetadataJson] nvarchar(max) NULL,
    CONSTRAINT [PK_ReconciliationMatches] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [ReconciliationRules] (
    [Id] uniqueidentifier NOT NULL,
    [CompanyId] uniqueidentifier NULL,
    [BranchId] uniqueidentifier NULL,
    [RuleCode] nvarchar(80) NOT NULL,
    [Name] nvarchar(180) NOT NULL,
    [Module] nvarchar(80) NOT NULL,
    [Priority] int NOT NULL,
    [IsFuzzy] bit NOT NULL,
    [IsActive] bit NOT NULL,
    [DefinitionJson] nvarchar(max) NOT NULL,
    CONSTRAINT [PK_ReconciliationRules] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [ReconciliationSessions] (
    [Id] uniqueidentifier NOT NULL,
    [CompanyId] uniqueidentifier NULL,
    [BranchId] uniqueidentifier NULL,
    [Module] nvarchar(80) NOT NULL,
    [SessionCode] nvarchar(80) NOT NULL,
    [Status] nvarchar(40) NOT NULL,
    [StartedAtUtc] datetime2 NOT NULL,
    [CompletedAtUtc] datetime2 NULL,
    [CorrelationId] nvarchar(100) NULL,
    [MetadataJson] nvarchar(max) NULL,
    CONSTRAINT [PK_ReconciliationSessions] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [ReconciliationSnapshots] (
    [Id] uniqueidentifier NOT NULL,
    [CompanyId] uniqueidentifier NULL,
    [BranchId] uniqueidentifier NULL,
    [SessionId] uniqueidentifier NOT NULL,
    [CapturedAtUtc] datetime2 NOT NULL,
    [SnapshotJson] nvarchar(max) NOT NULL,
    [SnapshotHash] nvarchar(64) NOT NULL,
    CONSTRAINT [PK_ReconciliationSnapshots] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [ReportSnapshots] (
    [Id] uniqueidentifier NOT NULL,
    [CompanyId] uniqueidentifier NULL,
    [BranchId] uniqueidentifier NULL,
    [ReportCode] nvarchar(120) NOT NULL,
    [VersionCode] nvarchar(60) NOT NULL,
    [PeriodCode] nvarchar(20) NOT NULL,
    [CapturedAtUtc] datetime2 NOT NULL,
    [SnapshotJson] nvarchar(max) NOT NULL,
    [SnapshotHash] nvarchar(64) NOT NULL,
    CONSTRAINT [PK_ReportSnapshots] PRIMARY KEY ([Id])
);
GO

CREATE INDEX [IX_ComplianceArchives_CompliancePackageId_CreatedAtUtc] ON [ComplianceArchives] ([CompliancePackageId], [CreatedAtUtc]);
GO

CREATE INDEX [IX_ComplianceManifests_CompliancePackageId_ManifestType] ON [ComplianceManifests] ([CompliancePackageId], [ManifestType]);
GO

CREATE UNIQUE INDEX [IX_CompliancePackages_CompanyId_BranchId_PackageCode] ON [CompliancePackages] ([CompanyId], [BranchId], [PackageCode]) WHERE [CompanyId] IS NOT NULL AND [BranchId] IS NOT NULL;
GO

CREATE INDEX [IX_ComplianceSubmissions_CompliancePackageId_CreatedAtUtc] ON [ComplianceSubmissions] ([CompliancePackageId], [CreatedAtUtc]);
GO

CREATE INDEX [IX_DataIntegrityCheckpoints_CompanyId_BranchId_CheckpointCode_CreatedAtUtc] ON [DataIntegrityCheckpoints] ([CompanyId], [BranchId], [CheckpointCode], [CreatedAtUtc]);
GO

CREATE INDEX [IX_DataIntegrityScanResults_CompanyId_BranchId_Module_ScannedAtUtc] ON [DataIntegrityScanResults] ([CompanyId], [BranchId], [Module], [ScannedAtUtc]);
GO

CREATE INDEX [IX_DataRepairOperations_CompanyId_BranchId_OperationCode_RequestedAtUtc] ON [DataRepairOperations] ([CompanyId], [BranchId], [OperationCode], [RequestedAtUtc]);
GO

CREATE INDEX [IX_ExportArtifacts_ExportExecutionId_CreatedAtUtc] ON [ExportArtifacts] ([ExportExecutionId], [CreatedAtUtc]);
GO

CREATE INDEX [IX_ExportExecutions_CompanyId_BranchId_ExportCode_QueuedAtUtc] ON [ExportExecutions] ([CompanyId], [BranchId], [ExportCode], [QueuedAtUtc]);
GO

CREATE UNIQUE INDEX [IX_ExportRetentionPolicies_CompanyId_BranchId_ExportCode] ON [ExportRetentionPolicies] ([CompanyId], [BranchId], [ExportCode]) WHERE [CompanyId] IS NOT NULL AND [BranchId] IS NOT NULL;
GO

CREATE INDEX [IX_ProductionCertificationReports_CompanyId_BranchId_PhaseCode_GeneratedAtUtc] ON [ProductionCertificationReports] ([CompanyId], [BranchId], [PhaseCode], [GeneratedAtUtc]);
GO

CREATE INDEX [IX_ReconciliationAnomalies_SessionId_Severity_DetectedAtUtc] ON [ReconciliationAnomalies] ([SessionId], [Severity], [DetectedAtUtc]);
GO

CREATE INDEX [IX_ReconciliationMatches_SessionId_SourceType_TargetType] ON [ReconciliationMatches] ([SessionId], [SourceType], [TargetType]);
GO

CREATE UNIQUE INDEX [IX_ReconciliationRules_CompanyId_BranchId_RuleCode] ON [ReconciliationRules] ([CompanyId], [BranchId], [RuleCode]) WHERE [CompanyId] IS NOT NULL AND [BranchId] IS NOT NULL;
GO

CREATE INDEX [IX_ReconciliationSessions_CompanyId_BranchId_Module_StartedAtUtc] ON [ReconciliationSessions] ([CompanyId], [BranchId], [Module], [StartedAtUtc]);
GO

CREATE INDEX [IX_ReconciliationSnapshots_SessionId_CapturedAtUtc] ON [ReconciliationSnapshots] ([SessionId], [CapturedAtUtc]);
GO

CREATE INDEX [IX_ReportSnapshots_CompanyId_BranchId_ReportCode_PeriodCode_CapturedAtUtc] ON [ReportSnapshots] ([CompanyId], [BranchId], [ReportCode], [PeriodCode], [CapturedAtUtc]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260524061631_AddPhase4ExecutionFoundationStep46To412', N'8.0.7');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

CREATE TABLE [TrainingDataRuns] (
    [Id] uniqueidentifier NOT NULL,
    [OperationType] nvarchar(60) NOT NULL,
    [SeedProfile] nvarchar(60) NULL,
    [StartedAtUtc] datetime2 NOT NULL,
    [FinishedAtUtc] datetime2 NULL,
    [Status] nvarchar(40) NOT NULL,
    [RequestedByUserId] uniqueidentifier NULL,
    [IsDryRun] bit NOT NULL,
    [SummaryJson] nvarchar(max) NULL,
    [ErrorMessage] nvarchar(2000) NULL,
    CONSTRAINT [PK_TrainingDataRuns] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [TrainingDataRunSteps] (
    [Id] uniqueidentifier NOT NULL,
    [TrainingDataRunId] uniqueidentifier NOT NULL,
    [StepOrder] int NOT NULL,
    [StepCode] nvarchar(80) NOT NULL,
    [StepName] nvarchar(255) NOT NULL,
    [Success] bit NOT NULL,
    [DeletedCount] int NOT NULL,
    [CreatedCount] int NOT NULL,
    [Message] nvarchar(2000) NOT NULL,
    [ErrorMessage] nvarchar(2000) NULL,
    [StartedAtUtc] datetime2 NOT NULL,
    [FinishedAtUtc] datetime2 NOT NULL,
    CONSTRAINT [PK_TrainingDataRunSteps] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_TrainingDataRunSteps_TrainingDataRuns_TrainingDataRunId] FOREIGN KEY ([TrainingDataRunId]) REFERENCES [TrainingDataRuns] ([Id]) ON DELETE CASCADE
);
GO

CREATE INDEX [IX_TrainingDataRuns_OperationType_StartedAtUtc] ON [TrainingDataRuns] ([OperationType], [StartedAtUtc]);
GO

CREATE INDEX [IX_TrainingDataRuns_StartedAtUtc] ON [TrainingDataRuns] ([StartedAtUtc]);
GO

CREATE UNIQUE INDEX [IX_TrainingDataRunSteps_TrainingDataRunId_StepOrder] ON [TrainingDataRunSteps] ([TrainingDataRunId], [StepOrder]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260525031433_AddTrainingDataRunHistory', N'8.0.7');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF COL_LENGTH('StockReceiptDetails', 'VatAmount') IS NULL
    ALTER TABLE [StockReceiptDetails] ADD [VatAmount] decimal(18,2) NOT NULL CONSTRAINT [DF_StockReceiptDetails_VatAmount] DEFAULT 0;
GO

IF COL_LENGTH('StockReceiptDetails', 'VatRate') IS NULL
    ALTER TABLE [StockReceiptDetails] ADD [VatRate] decimal(5,2) NOT NULL CONSTRAINT [DF_StockReceiptDetails_VatRate] DEFAULT 0;
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260527091500_AddStockReceiptDetailVatColumns', N'8.0.7');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF COL_LENGTH('StockIssueDetails', 'VatAmount') IS NOT NULL
    ALTER TABLE [StockIssueDetails] DROP COLUMN [VatAmount];
GO

IF COL_LENGTH('StockIssueDetails', 'VatRate') IS NOT NULL
    ALTER TABLE [StockIssueDetails] DROP COLUMN [VatRate];
GO

IF COL_LENGTH('TrainingDataRunSteps', 'AfterCount') IS NULL
    ALTER TABLE [TrainingDataRunSteps] ADD [AfterCount] int NOT NULL CONSTRAINT [DF_TrainingDataRunSteps_AfterCount] DEFAULT 0;
GO

IF COL_LENGTH('TrainingDataRunSteps', 'BeforeCount') IS NULL
    ALTER TABLE [TrainingDataRunSteps] ADD [BeforeCount] int NOT NULL CONSTRAINT [DF_TrainingDataRunSteps_BeforeCount] DEFAULT 0;
GO

IF COL_LENGTH('TrainingDataRunSteps', 'EntityName') IS NULL
    ALTER TABLE [TrainingDataRunSteps] ADD [EntityName] nvarchar(160) NULL;
GO

IF COL_LENGTH('TrainingDataRunSteps', 'Status') IS NULL
    ALTER TABLE [TrainingDataRunSteps] ADD [Status] nvarchar(40) NOT NULL CONSTRAINT [DF_TrainingDataRunSteps_Status] DEFAULT N'';
GO

IF COL_LENGTH('StockReceiptDetails', 'VatAmount') IS NULL
    ALTER TABLE [StockReceiptDetails] ADD [VatAmount] decimal(18,2) NOT NULL CONSTRAINT [DF_StockReceiptDetails_VatAmount] DEFAULT 0;
GO

IF COL_LENGTH('StockReceiptDetails', 'VatRate') IS NULL
    ALTER TABLE [StockReceiptDetails] ADD [VatRate] decimal(5,2) NOT NULL CONSTRAINT [DF_StockReceiptDetails_VatRate] DEFAULT 0;
GO

CREATE TABLE [BreakfastCafeOrders] (
    [Id] uniqueidentifier NOT NULL,
    [CompanyId] uniqueidentifier NULL,
    [BranchId] uniqueidentifier NULL,
    [OrderNumber] nvarchar(60) NOT NULL,
    [OrderDate] datetime2 NOT NULL,
    [Status] nvarchar(30) NOT NULL,
    [PaymentStatus] nvarchar(30) NOT NULL,
    [CustomerReference] nvarchar(255) NULL,
    [CustomerId] int NULL,
    [RoomReference] nvarchar(120) NULL,
    [SubtotalAmount] decimal(18,2) NOT NULL,
    [DiscountAmount] decimal(18,2) NOT NULL,
    [VatAmount] decimal(18,2) NOT NULL,
    [TotalAmount] decimal(18,2) NOT NULL,
    [PaidAmount] decimal(18,2) NOT NULL,
    [RemainingAmount] decimal(18,2) NOT NULL,
    [JournalHeaderId] uniqueidentifier NULL,
    [Note] nvarchar(1000) NULL,
    [CreatedBy] nvarchar(120) NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_BreakfastCafeOrders] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_BreakfastCafeOrders_Customers_CustomerId] FOREIGN KEY ([CustomerId]) REFERENCES [Customers] ([Id]) ON DELETE SET NULL
);
GO

CREATE TABLE [MenuCategories] (
    [Id] uniqueidentifier NOT NULL,
    [CompanyId] uniqueidentifier NULL,
    [BranchId] uniqueidentifier NULL,
    [Code] nvarchar(50) NOT NULL,
    [Name] nvarchar(255) NOT NULL,
    [SortOrder] int NOT NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_MenuCategories] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [BreakfastCafePayments] (
    [Id] uniqueidentifier NOT NULL,
    [CompanyId] uniqueidentifier NULL,
    [BranchId] uniqueidentifier NULL,
    [BreakfastCafeOrderId] uniqueidentifier NOT NULL,
    [PaymentDate] datetime2 NOT NULL,
    [PaymentMethod] nvarchar(40) NOT NULL,
    [Amount] decimal(18,2) NOT NULL,
    [ReferenceNo] nvarchar(120) NULL,
    [CashFundId] uniqueidentifier NULL,
    [BankAccountId] uniqueidentifier NULL,
    [RoomReference] nvarchar(120) NULL,
    [CustomerId] int NULL,
    [Status] nvarchar(30) NOT NULL,
    [CreatedBy] nvarchar(120) NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_BreakfastCafePayments] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_BreakfastCafePayments_BankAccounts_BankAccountId] FOREIGN KEY ([BankAccountId]) REFERENCES [BankAccounts] ([Id]) ON DELETE SET NULL,
    CONSTRAINT [FK_BreakfastCafePayments_BreakfastCafeOrders_BreakfastCafeOrderId] FOREIGN KEY ([BreakfastCafeOrderId]) REFERENCES [BreakfastCafeOrders] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_BreakfastCafePayments_CashFunds_CashFundId] FOREIGN KEY ([CashFundId]) REFERENCES [CashFunds] ([Id]) ON DELETE SET NULL,
    CONSTRAINT [FK_BreakfastCafePayments_Customers_CustomerId] FOREIGN KEY ([CustomerId]) REFERENCES [Customers] ([Id]) ON DELETE SET NULL
);
GO

CREATE TABLE [MenuItems] (
    [Id] uniqueidentifier NOT NULL,
    [CompanyId] uniqueidentifier NULL,
    [BranchId] uniqueidentifier NULL,
    [MenuCategoryId] uniqueidentifier NOT NULL,
    [ItemId] int NULL,
    [Code] nvarchar(50) NOT NULL,
    [Name] nvarchar(255) NOT NULL,
    [UnitName] nvarchar(30) NOT NULL,
    [UnitPrice] decimal(18,2) NOT NULL,
    [VatRate] decimal(9,4) NOT NULL,
    [IsCombo] bit NOT NULL,
    [IsHotelService] bit NOT NULL,
    [ImagePath] nvarchar(500) NULL,
    [ImageUrl] nvarchar(500) NULL,
    [SortOrder] int NOT NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_MenuItems] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_MenuItems_Items_ItemId] FOREIGN KEY ([ItemId]) REFERENCES [Items] ([Id]) ON DELETE SET NULL,
    CONSTRAINT [FK_MenuItems_MenuCategories_MenuCategoryId] FOREIGN KEY ([MenuCategoryId]) REFERENCES [MenuCategories] ([Id]) ON DELETE NO ACTION
);
GO

CREATE TABLE [BreakfastCafeOrderLines] (
    [Id] uniqueidentifier NOT NULL,
    [CompanyId] uniqueidentifier NULL,
    [BranchId] uniqueidentifier NULL,
    [BreakfastCafeOrderId] uniqueidentifier NOT NULL,
    [MenuItemId] uniqueidentifier NOT NULL,
    [LineNo] int NOT NULL,
    [MenuItemCode] nvarchar(50) NOT NULL,
    [MenuItemName] nvarchar(255) NOT NULL,
    [UnitName] nvarchar(30) NOT NULL,
    [Quantity] decimal(18,3) NOT NULL,
    [UnitPrice] decimal(18,2) NOT NULL,
    [LineDiscountAmount] decimal(18,2) NOT NULL,
    [VatRate] decimal(9,4) NOT NULL,
    [VatAmount] decimal(18,2) NOT NULL,
    [LineTotalAmount] decimal(18,2) NOT NULL,
    [Note] nvarchar(500) NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_BreakfastCafeOrderLines] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_BreakfastCafeOrderLines_BreakfastCafeOrders_BreakfastCafeOrderId] FOREIGN KEY ([BreakfastCafeOrderId]) REFERENCES [BreakfastCafeOrders] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_BreakfastCafeOrderLines_MenuItems_MenuItemId] FOREIGN KEY ([MenuItemId]) REFERENCES [MenuItems] ([Id]) ON DELETE NO ACTION
);
GO

CREATE TABLE [MenuItemRecipes] (
    [Id] uniqueidentifier NOT NULL,
    [CompanyId] uniqueidentifier NULL,
    [BranchId] uniqueidentifier NULL,
    [MenuItemId] uniqueidentifier NOT NULL,
    [Name] nvarchar(255) NOT NULL,
    [IsDefault] bit NOT NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_MenuItemRecipes] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_MenuItemRecipes_MenuItems_MenuItemId] FOREIGN KEY ([MenuItemId]) REFERENCES [MenuItems] ([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [MenuItemRecipeLines] (
    [Id] uniqueidentifier NOT NULL,
    [CompanyId] uniqueidentifier NULL,
    [BranchId] uniqueidentifier NULL,
    [MenuItemRecipeId] uniqueidentifier NOT NULL,
    [LineNo] int NOT NULL,
    [ItemId] int NULL,
    [ItemCode] nvarchar(50) NOT NULL,
    [ItemName] nvarchar(255) NOT NULL,
    [UnitName] nvarchar(30) NOT NULL,
    [Quantity] decimal(18,4) NOT NULL,
    [LossRate] decimal(9,4) NOT NULL,
    [Note] nvarchar(500) NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_MenuItemRecipeLines] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_MenuItemRecipeLines_Items_ItemId] FOREIGN KEY ([ItemId]) REFERENCES [Items] ([Id]) ON DELETE SET NULL,
    CONSTRAINT [FK_MenuItemRecipeLines_MenuItemRecipes_MenuItemRecipeId] FOREIGN KEY ([MenuItemRecipeId]) REFERENCES [MenuItemRecipes] ([Id]) ON DELETE CASCADE
);
GO

CREATE UNIQUE INDEX [IX_BreakfastCafeOrderLines_BreakfastCafeOrderId_LineNo] ON [BreakfastCafeOrderLines] ([BreakfastCafeOrderId], [LineNo]);
GO

CREATE INDEX [IX_BreakfastCafeOrderLines_MenuItemCode] ON [BreakfastCafeOrderLines] ([MenuItemCode]);
GO

CREATE INDEX [IX_BreakfastCafeOrderLines_MenuItemId] ON [BreakfastCafeOrderLines] ([MenuItemId]);
GO

CREATE INDEX [IX_BreakfastCafeOrders_CustomerId] ON [BreakfastCafeOrders] ([CustomerId]);
GO

CREATE INDEX [IX_BreakfastCafeOrders_OrderDate] ON [BreakfastCafeOrders] ([OrderDate]);
GO

CREATE UNIQUE INDEX [IX_BreakfastCafeOrders_OrderNumber] ON [BreakfastCafeOrders] ([OrderNumber]);
GO

CREATE INDEX [IX_BreakfastCafeOrders_PaymentStatus] ON [BreakfastCafeOrders] ([PaymentStatus]);
GO

CREATE INDEX [IX_BreakfastCafeOrders_Status] ON [BreakfastCafeOrders] ([Status]);
GO

CREATE INDEX [IX_BreakfastCafePayments_BankAccountId] ON [BreakfastCafePayments] ([BankAccountId]);
GO

CREATE INDEX [IX_BreakfastCafePayments_BreakfastCafeOrderId_PaymentDate] ON [BreakfastCafePayments] ([BreakfastCafeOrderId], [PaymentDate]);
GO

CREATE INDEX [IX_BreakfastCafePayments_CashFundId] ON [BreakfastCafePayments] ([CashFundId]);
GO

CREATE INDEX [IX_BreakfastCafePayments_CustomerId] ON [BreakfastCafePayments] ([CustomerId]);
GO

CREATE INDEX [IX_BreakfastCafePayments_PaymentMethod] ON [BreakfastCafePayments] ([PaymentMethod]);
GO

CREATE UNIQUE INDEX [IX_MenuCategories_Code] ON [MenuCategories] ([Code]);
GO

CREATE INDEX [IX_MenuItemRecipeLines_ItemId] ON [MenuItemRecipeLines] ([ItemId]);
GO

CREATE UNIQUE INDEX [IX_MenuItemRecipeLines_MenuItemRecipeId_LineNo] ON [MenuItemRecipeLines] ([MenuItemRecipeId], [LineNo]);
GO

CREATE INDEX [IX_MenuItemRecipes_MenuItemId_IsDefault] ON [MenuItemRecipes] ([MenuItemId], [IsDefault]);
GO

CREATE UNIQUE INDEX [IX_MenuItems_Code] ON [MenuItems] ([Code]);
GO

CREATE INDEX [IX_MenuItems_ItemId] ON [MenuItems] ([ItemId]);
GO

CREATE INDEX [IX_MenuItems_MenuCategoryId_SortOrder] ON [MenuItems] ([MenuCategoryId], [SortOrder]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260528095952_AddBreakfastCafeSalesModule', N'8.0.7');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF COL_LENGTH('MenuItems', 'HasRecipeManagement') IS NULL
    ALTER TABLE [MenuItems] ADD [HasRecipeManagement] bit NOT NULL CONSTRAINT [DF_MenuItems_HasRecipeManagement] DEFAULT 1;
GO

IF COL_LENGTH('MenuItems', 'Note') IS NULL
    ALTER TABLE [MenuItems] ADD [Note] nvarchar(1000) NULL;
GO

IF COL_LENGTH('MenuItemRecipeLines', 'ChildMenuItemId') IS NULL
    ALTER TABLE [MenuItemRecipeLines] ADD [ChildMenuItemId] uniqueidentifier NULL;
GO

IF COL_LENGTH('MenuItemRecipeLines', 'DefaultWarehouseId') IS NULL
    ALTER TABLE [MenuItemRecipeLines] ADD [DefaultWarehouseId] int NULL;
GO

IF COL_LENGTH('MenuItemRecipeLines', 'IsRequired') IS NULL
    ALTER TABLE [MenuItemRecipeLines] ADD [IsRequired] bit NOT NULL CONSTRAINT [DF_MenuItemRecipeLines_IsRequired] DEFAULT 1;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_MenuItems_IsActive' AND object_id = OBJECT_ID('MenuItems'))
    CREATE INDEX [IX_MenuItems_IsActive] ON [MenuItems]([IsActive]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_MenuItemRecipeLines_ChildMenuItemId' AND object_id = OBJECT_ID('MenuItemRecipeLines'))
    CREATE INDEX [IX_MenuItemRecipeLines_ChildMenuItemId] ON [MenuItemRecipeLines]([ChildMenuItemId]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_MenuItemRecipeLines_DefaultWarehouseId' AND object_id = OBJECT_ID('MenuItemRecipeLines'))
    CREATE INDEX [IX_MenuItemRecipeLines_DefaultWarehouseId] ON [MenuItemRecipeLines]([DefaultWarehouseId]);
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_MenuItemRecipeLines_MenuItems_ChildMenuItemId'
)
    ALTER TABLE [MenuItemRecipeLines] ADD CONSTRAINT [FK_MenuItemRecipeLines_MenuItems_ChildMenuItemId]
        FOREIGN KEY ([ChildMenuItemId]) REFERENCES [MenuItems]([Id]) ON DELETE NO ACTION;
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_MenuItemRecipeLines_Warehouses_DefaultWarehouseId'
)
    ALTER TABLE [MenuItemRecipeLines] ADD CONSTRAINT [FK_MenuItemRecipeLines_Warehouses_DefaultWarehouseId]
        FOREIGN KEY ([DefaultWarehouseId]) REFERENCES [Warehouses]([Id]) ON DELETE SET NULL;
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260528183000_AddMenuRecipeModule', N'8.0.7');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF COL_LENGTH('MenuItems', 'PreparationArea') IS NULL
    ALTER TABLE [MenuItems] ADD [PreparationArea] nvarchar(100) NULL;
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260529152901_AddPreparationAreaToMenuItem', N'8.0.7');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF COL_LENGTH('MenuItems', 'HasRecipeManagement') IS NULL
    ALTER TABLE [MenuItems] ADD [HasRecipeManagement] bit NOT NULL CONSTRAINT [DF_MenuItems_HasRecipeManagement] DEFAULT CAST(0 AS bit);
IF COL_LENGTH('MenuItems', 'Note') IS NULL
    ALTER TABLE [MenuItems] ADD [Note] nvarchar(1000) NULL;
IF COL_LENGTH('MenuItems', 'PreparationArea') IS NULL
    ALTER TABLE [MenuItems] ADD [PreparationArea] nvarchar(100) NULL;
IF COL_LENGTH('MenuItemRecipeLines', 'ChildMenuItemId') IS NULL
    ALTER TABLE [MenuItemRecipeLines] ADD [ChildMenuItemId] uniqueidentifier NULL;
IF COL_LENGTH('MenuItemRecipeLines', 'DefaultWarehouseId') IS NULL
    ALTER TABLE [MenuItemRecipeLines] ADD [DefaultWarehouseId] int NULL;
IF COL_LENGTH('MenuItemRecipeLines', 'IsRequired') IS NULL
    ALTER TABLE [MenuItemRecipeLines] ADD [IsRequired] bit NOT NULL CONSTRAINT [DF_MenuItemRecipeLines_IsRequired] DEFAULT CAST(0 AS bit);
GO

ALTER TABLE [BreakfastCafeOrders] ADD [AreaName] nvarchar(120) NULL;
GO

ALTER TABLE [BreakfastCafeOrders] ADD [KitchenSentAt] datetime2 NULL;
GO

ALTER TABLE [BreakfastCafeOrders] ADD [OrderType] nvarchar(30) NOT NULL DEFAULT N'';
GO

ALTER TABLE [BreakfastCafeOrders] ADD [PosTableId] uniqueidentifier NULL;
GO

ALTER TABLE [BreakfastCafeOrders] ADD [ServiceChargeAmount] decimal(18,2) NOT NULL DEFAULT 0.0;
GO

ALTER TABLE [BreakfastCafeOrders] ADD [ServiceStatus] nvarchar(30) NOT NULL DEFAULT N'';
GO

ALTER TABLE [BreakfastCafeOrders] ADD [TableCode] nvarchar(50) NULL;
GO

ALTER TABLE [BreakfastCafeOrders] ADD [TableName] nvarchar(120) NULL;
GO

ALTER TABLE [BreakfastCafeOrderLines] ADD [KitchenStatus] nvarchar(30) NOT NULL DEFAULT N'';
GO

CREATE TABLE [BreakfastCafeKitchenTickets] (
    [Id] uniqueidentifier NOT NULL,
    [CompanyId] uniqueidentifier NULL,
    [BranchId] uniqueidentifier NULL,
    [BreakfastCafeOrderId] uniqueidentifier NOT NULL,
    [TicketNumber] nvarchar(60) NOT NULL,
    [Station] nvarchar(50) NOT NULL,
    [Status] nvarchar(30) NOT NULL,
    [SentAt] datetime2 NOT NULL,
    [SentBy] nvarchar(120) NULL,
    [Note] nvarchar(500) NULL,
    CONSTRAINT [PK_BreakfastCafeKitchenTickets] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_BreakfastCafeKitchenTickets_BreakfastCafeOrders_BreakfastCafeOrderId] FOREIGN KEY ([BreakfastCafeOrderId]) REFERENCES [BreakfastCafeOrders] ([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [PosTables] (
    [Id] uniqueidentifier NOT NULL,
    [CompanyId] uniqueidentifier NULL,
    [BranchId] uniqueidentifier NULL,
    [AreaCode] nvarchar(50) NOT NULL,
    [AreaName] nvarchar(120) NOT NULL,
    [TableCode] nvarchar(50) NOT NULL,
    [TableName] nvarchar(120) NOT NULL,
    [OrderType] nvarchar(30) NOT NULL,
    [Status] nvarchar(30) NOT NULL,
    [SeatCount] int NOT NULL,
    [SortOrder] int NOT NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_PosTables] PRIMARY KEY ([Id])
);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_MenuItems_IsActive' AND object_id = OBJECT_ID('MenuItems'))
    CREATE INDEX [IX_MenuItems_IsActive] ON [MenuItems] ([IsActive]);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_MenuItemRecipeLines_ChildMenuItemId' AND object_id = OBJECT_ID('MenuItemRecipeLines'))
    CREATE INDEX [IX_MenuItemRecipeLines_ChildMenuItemId] ON [MenuItemRecipeLines] ([ChildMenuItemId]);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_MenuItemRecipeLines_DefaultWarehouseId' AND object_id = OBJECT_ID('MenuItemRecipeLines'))
    CREATE INDEX [IX_MenuItemRecipeLines_DefaultWarehouseId] ON [MenuItemRecipeLines] ([DefaultWarehouseId]);
GO

CREATE INDEX [IX_BreakfastCafeOrders_PosTableId] ON [BreakfastCafeOrders] ([PosTableId]);
GO

CREATE INDEX [IX_BreakfastCafeKitchenTickets_BreakfastCafeOrderId] ON [BreakfastCafeKitchenTickets] ([BreakfastCafeOrderId]);
GO

CREATE UNIQUE INDEX [IX_BreakfastCafeKitchenTickets_TicketNumber] ON [BreakfastCafeKitchenTickets] ([TicketNumber]);
GO

CREATE INDEX [IX_PosTables_AreaCode] ON [PosTables] ([AreaCode]);
GO

CREATE INDEX [IX_PosTables_Status] ON [PosTables] ([Status]);
GO

CREATE UNIQUE INDEX [IX_PosTables_TableCode] ON [PosTables] ([TableCode]);
GO

ALTER TABLE [BreakfastCafeOrders] ADD CONSTRAINT [FK_BreakfastCafeOrders_PosTables_PosTableId] FOREIGN KEY ([PosTableId]) REFERENCES [PosTables] ([Id]) ON DELETE SET NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_MenuItemRecipeLines_MenuItems_ChildMenuItemId')
    ALTER TABLE [MenuItemRecipeLines] ADD CONSTRAINT [FK_MenuItemRecipeLines_MenuItems_ChildMenuItemId]
    FOREIGN KEY ([ChildMenuItemId]) REFERENCES [MenuItems] ([Id]);
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_MenuItemRecipeLines_Warehouses_DefaultWarehouseId')
    ALTER TABLE [MenuItemRecipeLines] ADD CONSTRAINT [FK_MenuItemRecipeLines_Warehouses_DefaultWarehouseId]
    FOREIGN KEY ([DefaultWarehouseId]) REFERENCES [Warehouses] ([Id]) ON DELETE SET NULL;
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260607022536_AddBreakfastCafePosTablesAndKitchenTickets', N'8.0.7');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

CREATE TABLE [BanquetMenuPackages] (
    [Id] uniqueidentifier NOT NULL,
    [CompanyId] uniqueidentifier NULL,
    [BranchId] uniqueidentifier NULL,
    [Code] nvarchar(50) NOT NULL,
    [Name] nvarchar(255) NOT NULL,
    [PricingMode] nvarchar(40) NOT NULL,
    [Price] decimal(18,2) NOT NULL,
    [VatRate] decimal(9,4) NOT NULL,
    [IsActive] bit NOT NULL,
    [Note] nvarchar(1000) NULL,
    CONSTRAINT [PK_BanquetMenuPackages] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [BanquetRooms] (
    [Id] uniqueidentifier NOT NULL,
    [CompanyId] uniqueidentifier NULL,
    [BranchId] uniqueidentifier NULL,
    [Code] nvarchar(50) NOT NULL,
    [Name] nvarchar(255) NOT NULL,
    [Capacity] int NOT NULL,
    [MaxTables] int NOT NULL,
    [Description] nvarchar(1000) NULL,
    [Status] nvarchar(40) NOT NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_BanquetRooms] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [DiningRooms] (
    [Id] uniqueidentifier NOT NULL,
    [CompanyId] uniqueidentifier NULL,
    [BranchId] uniqueidentifier NULL,
    [Code] nvarchar(50) NOT NULL,
    [Name] nvarchar(255) NOT NULL,
    [RoomType] nvarchar(40) NOT NULL,
    [Capacity] int NOT NULL,
    [Description] nvarchar(1000) NULL,
    [Status] nvarchar(40) NOT NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_DiningRooms] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [RestaurantContracts] (
    [Id] uniqueidentifier NOT NULL,
    [CompanyId] uniqueidentifier NULL,
    [BranchId] uniqueidentifier NULL,
    [ContractNo] nvarchar(60) NOT NULL,
    [CustomerId] int NULL,
    [EventDescription] nvarchar(1000) NULL,
    [TotalAmount] decimal(18,2) NOT NULL,
    [DepositAmount] decimal(18,2) NOT NULL,
    [RemainingAmount] decimal(18,2) NOT NULL,
    [Status] nvarchar(40) NOT NULL,
    [SignedDate] datetime2 NULL,
    [Note] nvarchar(1000) NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_RestaurantContracts] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_RestaurantContracts_Customers_CustomerId] FOREIGN KEY ([CustomerId]) REFERENCES [Customers] ([Id]) ON DELETE SET NULL
);
GO

CREATE TABLE [RestaurantExtraServices] (
    [Id] uniqueidentifier NOT NULL,
    [CompanyId] uniqueidentifier NULL,
    [BranchId] uniqueidentifier NULL,
    [Code] nvarchar(50) NOT NULL,
    [Name] nvarchar(255) NOT NULL,
    [ServiceType] nvarchar(60) NOT NULL,
    [DefaultPrice] decimal(18,2) NOT NULL,
    [VatRate] decimal(9,4) NOT NULL,
    [IsActive] bit NOT NULL,
    [Note] nvarchar(1000) NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_RestaurantExtraServices] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [BanquetMenuPackageLines] (
    [Id] uniqueidentifier NOT NULL,
    [CompanyId] uniqueidentifier NULL,
    [BranchId] uniqueidentifier NULL,
    [BanquetMenuPackageId] uniqueidentifier NOT NULL,
    [MenuItemId] uniqueidentifier NOT NULL,
    [CourseType] nvarchar(40) NOT NULL,
    [LineNo] int NOT NULL,
    [Qty] decimal(18,3) NOT NULL,
    [Note] nvarchar(500) NULL,
    CONSTRAINT [PK_BanquetMenuPackageLines] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_BanquetMenuPackageLines_BanquetMenuPackages_BanquetMenuPackageId] FOREIGN KEY ([BanquetMenuPackageId]) REFERENCES [BanquetMenuPackages] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_BanquetMenuPackageLines_MenuItems_MenuItemId] FOREIGN KEY ([MenuItemId]) REFERENCES [MenuItems] ([Id]) ON DELETE NO ACTION
);
GO

CREATE TABLE [BanquetEvents] (
    [Id] uniqueidentifier NOT NULL,
    [CompanyId] uniqueidentifier NULL,
    [BranchId] uniqueidentifier NULL,
    [EventNo] nvarchar(60) NOT NULL,
    [EventType] nvarchar(60) NOT NULL,
    [CustomerId] int NULL,
    [CustomerName] nvarchar(255) NULL,
    [Phone] nvarchar(40) NOT NULL,
    [EventDate] datetime2 NOT NULL,
    [StartTime] time NOT NULL,
    [EndTime] time NOT NULL,
    [GuestCount] int NOT NULL,
    [TableCount] int NOT NULL,
    [BanquetRoomId] uniqueidentifier NULL,
    [Status] nvarchar(40) NOT NULL,
    [Note] nvarchar(1000) NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_BanquetEvents] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_BanquetEvents_BanquetRooms_BanquetRoomId] FOREIGN KEY ([BanquetRoomId]) REFERENCES [BanquetRooms] ([Id]) ON DELETE SET NULL,
    CONSTRAINT [FK_BanquetEvents_Customers_CustomerId] FOREIGN KEY ([CustomerId]) REFERENCES [Customers] ([Id]) ON DELETE SET NULL
);
GO

CREATE TABLE [DiningTables] (
    [Id] uniqueidentifier NOT NULL,
    [CompanyId] uniqueidentifier NULL,
    [BranchId] uniqueidentifier NULL,
    [DiningRoomId] uniqueidentifier NOT NULL,
    [Code] nvarchar(50) NOT NULL,
    [Name] nvarchar(255) NOT NULL,
    [SeatCount] int NOT NULL,
    [Status] nvarchar(40) NOT NULL,
    [Note] nvarchar(1000) NULL,
    [IsActive] bit NOT NULL,
    [SortOrder] int NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_DiningTables] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_DiningTables_DiningRooms_DiningRoomId] FOREIGN KEY ([DiningRoomId]) REFERENCES [DiningRooms] ([Id]) ON DELETE NO ACTION
);
GO

CREATE TABLE [BanquetChecklists] (
    [Id] uniqueidentifier NOT NULL,
    [CompanyId] uniqueidentifier NULL,
    [BranchId] uniqueidentifier NULL,
    [BanquetEventId] uniqueidentifier NOT NULL,
    [Title] nvarchar(255) NOT NULL,
    [IsDone] bit NOT NULL,
    [DoneAt] datetime2 NULL,
    [DoneBy] nvarchar(120) NULL,
    [SortOrder] int NOT NULL,
    [Note] nvarchar(1000) NULL,
    CONSTRAINT [PK_BanquetChecklists] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_BanquetChecklists_BanquetEvents_BanquetEventId] FOREIGN KEY ([BanquetEventId]) REFERENCES [BanquetEvents] ([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [BanquetContracts] (
    [Id] uniqueidentifier NOT NULL,
    [CompanyId] uniqueidentifier NULL,
    [BranchId] uniqueidentifier NULL,
    [ContractNo] nvarchar(60) NOT NULL,
    [BanquetEventId] uniqueidentifier NOT NULL,
    [SignDate] datetime2 NULL,
    [CustomerId] int NULL,
    [TotalAmount] decimal(18,2) NOT NULL,
    [DepositAmount] decimal(18,2) NOT NULL,
    [RemainingAmount] decimal(18,2) NOT NULL,
    [Status] nvarchar(40) NOT NULL,
    [Note] nvarchar(1000) NULL,
    CONSTRAINT [PK_BanquetContracts] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_BanquetContracts_BanquetEvents_BanquetEventId] FOREIGN KEY ([BanquetEventId]) REFERENCES [BanquetEvents] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_BanquetContracts_Customers_CustomerId] FOREIGN KEY ([CustomerId]) REFERENCES [Customers] ([Id]) ON DELETE SET NULL
);
GO

CREATE TABLE [BanquetEventMenus] (
    [Id] uniqueidentifier NOT NULL,
    [CompanyId] uniqueidentifier NULL,
    [BranchId] uniqueidentifier NULL,
    [BanquetEventId] uniqueidentifier NOT NULL,
    [BanquetMenuPackageId] uniqueidentifier NULL,
    [PricingMode] nvarchar(40) NOT NULL,
    [TableCount] int NOT NULL,
    [GuestCount] int NOT NULL,
    [UnitPrice] decimal(18,2) NOT NULL,
    [Amount] decimal(18,2) NOT NULL,
    CONSTRAINT [PK_BanquetEventMenus] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_BanquetEventMenus_BanquetEvents_BanquetEventId] FOREIGN KEY ([BanquetEventId]) REFERENCES [BanquetEvents] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_BanquetEventMenus_BanquetMenuPackages_BanquetMenuPackageId] FOREIGN KEY ([BanquetMenuPackageId]) REFERENCES [BanquetMenuPackages] ([Id]) ON DELETE SET NULL
);
GO

CREATE TABLE [BanquetFloorPlans] (
    [Id] uniqueidentifier NOT NULL,
    [CompanyId] uniqueidentifier NULL,
    [BranchId] uniqueidentifier NULL,
    [BanquetEventId] uniqueidentifier NOT NULL,
    [Name] nvarchar(255) NOT NULL,
    [CanvasWidth] float NOT NULL,
    [CanvasHeight] float NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_BanquetFloorPlans] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_BanquetFloorPlans_BanquetEvents_BanquetEventId] FOREIGN KEY ([BanquetEventId]) REFERENCES [BanquetEvents] ([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [BanquetServiceLines] (
    [Id] uniqueidentifier NOT NULL,
    [CompanyId] uniqueidentifier NULL,
    [BranchId] uniqueidentifier NULL,
    [BanquetEventId] uniqueidentifier NOT NULL,
    [ServiceType] nvarchar(60) NOT NULL,
    [Description] nvarchar(255) NOT NULL,
    [Qty] decimal(18,3) NOT NULL,
    [UnitPrice] decimal(18,2) NOT NULL,
    [VatRate] decimal(9,4) NOT NULL,
    [Amount] decimal(18,2) NOT NULL,
    [SupplierId] int NULL,
    [Note] nvarchar(1000) NULL,
    CONSTRAINT [PK_BanquetServiceLines] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_BanquetServiceLines_BanquetEvents_BanquetEventId] FOREIGN KEY ([BanquetEventId]) REFERENCES [BanquetEvents] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_BanquetServiceLines_Suppliers_SupplierId] FOREIGN KEY ([SupplierId]) REFERENCES [Suppliers] ([Id]) ON DELETE SET NULL
);
GO

CREATE TABLE [BanquetTimelines] (
    [Id] uniqueidentifier NOT NULL,
    [CompanyId] uniqueidentifier NULL,
    [BranchId] uniqueidentifier NULL,
    [BanquetEventId] uniqueidentifier NOT NULL,
    [StartTime] time NOT NULL,
    [Title] nvarchar(255) NOT NULL,
    [Description] nvarchar(1000) NULL,
    [ResponsiblePerson] nvarchar(120) NULL,
    [SortOrder] int NOT NULL,
    [Status] nvarchar(40) NOT NULL,
    CONSTRAINT [PK_BanquetTimelines] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_BanquetTimelines_BanquetEvents_BanquetEventId] FOREIGN KEY ([BanquetEventId]) REFERENCES [BanquetEvents] ([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [TableReservations] (
    [Id] uniqueidentifier NOT NULL,
    [CompanyId] uniqueidentifier NULL,
    [BranchId] uniqueidentifier NULL,
    [ReservationNo] nvarchar(60) NOT NULL,
    [CustomerId] int NULL,
    [CustomerName] nvarchar(255) NULL,
    [Phone] nvarchar(40) NOT NULL,
    [ReservationDateTime] datetime2 NOT NULL,
    [GuestCount] int NOT NULL,
    [DiningRoomId] uniqueidentifier NULL,
    [DiningTableId] uniqueidentifier NULL,
    [Status] nvarchar(40) NOT NULL,
    [Note] nvarchar(1000) NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_TableReservations] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_TableReservations_Customers_CustomerId] FOREIGN KEY ([CustomerId]) REFERENCES [Customers] ([Id]) ON DELETE SET NULL,
    CONSTRAINT [FK_TableReservations_DiningRooms_DiningRoomId] FOREIGN KEY ([DiningRoomId]) REFERENCES [DiningRooms] ([Id]),
    CONSTRAINT [FK_TableReservations_DiningTables_DiningTableId] FOREIGN KEY ([DiningTableId]) REFERENCES [DiningTables] ([Id])
);
GO

CREATE TABLE [BanquetDeposits] (
    [Id] uniqueidentifier NOT NULL,
    [CompanyId] uniqueidentifier NULL,
    [BranchId] uniqueidentifier NULL,
    [DepositNo] nvarchar(60) NOT NULL,
    [BanquetEventId] uniqueidentifier NOT NULL,
    [BanquetContractId] uniqueidentifier NULL,
    [DepositDate] datetime2 NOT NULL,
    [Amount] decimal(18,2) NOT NULL,
    [PaymentMethod] nvarchar(40) NOT NULL,
    [CashFundId] uniqueidentifier NULL,
    [BankAccountId] uniqueidentifier NULL,
    [Receiver] nvarchar(120) NULL,
    [Note] nvarchar(1000) NULL,
    [Status] nvarchar(40) NOT NULL,
    CONSTRAINT [PK_BanquetDeposits] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_BanquetDeposits_BankAccounts_BankAccountId] FOREIGN KEY ([BankAccountId]) REFERENCES [BankAccounts] ([Id]) ON DELETE SET NULL,
    CONSTRAINT [FK_BanquetDeposits_BanquetContracts_BanquetContractId] FOREIGN KEY ([BanquetContractId]) REFERENCES [BanquetContracts] ([Id]),
    CONSTRAINT [FK_BanquetDeposits_BanquetEvents_BanquetEventId] FOREIGN KEY ([BanquetEventId]) REFERENCES [BanquetEvents] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_BanquetDeposits_CashFunds_CashFundId] FOREIGN KEY ([CashFundId]) REFERENCES [CashFunds] ([Id]) ON DELETE SET NULL
);
GO

CREATE TABLE [BanquetFloorPlanTables] (
    [Id] uniqueidentifier NOT NULL,
    [CompanyId] uniqueidentifier NULL,
    [BranchId] uniqueidentifier NULL,
    [BanquetFloorPlanId] uniqueidentifier NOT NULL,
    [TableNo] nvarchar(40) NOT NULL,
    [TableType] nvarchar(40) NOT NULL,
    [SeatCount] int NOT NULL,
    [X] float NOT NULL,
    [Y] float NOT NULL,
    [Width] float NOT NULL,
    [Height] float NOT NULL,
    [Rotation] float NOT NULL,
    [Note] nvarchar(500) NULL,
    CONSTRAINT [PK_BanquetFloorPlanTables] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_BanquetFloorPlanTables_BanquetFloorPlans_BanquetFloorPlanId] FOREIGN KEY ([BanquetFloorPlanId]) REFERENCES [BanquetFloorPlans] ([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [AlacarteOrders] (
    [Id] uniqueidentifier NOT NULL,
    [CompanyId] uniqueidentifier NULL,
    [BranchId] uniqueidentifier NULL,
    [OrderNo] nvarchar(60) NOT NULL,
    [DiningTableId] uniqueidentifier NULL,
    [CustomerId] int NULL,
    [ReservationId] uniqueidentifier NULL,
    [OrderDate] datetime2 NOT NULL,
    [Status] nvarchar(40) NOT NULL,
    [PaymentStatus] nvarchar(40) NOT NULL,
    [SubtotalAmount] decimal(18,2) NOT NULL,
    [DiscountAmount] decimal(18,2) NOT NULL,
    [VatAmount] decimal(18,2) NOT NULL,
    [ServiceChargeAmount] decimal(18,2) NOT NULL,
    [TotalAmount] decimal(18,2) NOT NULL,
    [PaidAmount] decimal(18,2) NOT NULL,
    [RemainingAmount] decimal(18,2) NOT NULL,
    [KitchenSentAt] datetime2 NULL,
    [Note] nvarchar(1000) NULL,
    [CreatedBy] nvarchar(120) NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_AlacarteOrders] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_AlacarteOrders_Customers_CustomerId] FOREIGN KEY ([CustomerId]) REFERENCES [Customers] ([Id]) ON DELETE SET NULL,
    CONSTRAINT [FK_AlacarteOrders_DiningTables_DiningTableId] FOREIGN KEY ([DiningTableId]) REFERENCES [DiningTables] ([Id]) ON DELETE SET NULL,
    CONSTRAINT [FK_AlacarteOrders_TableReservations_ReservationId] FOREIGN KEY ([ReservationId]) REFERENCES [TableReservations] ([Id]) ON DELETE SET NULL
);
GO

CREATE TABLE [AlacarteOrderLines] (
    [Id] uniqueidentifier NOT NULL,
    [CompanyId] uniqueidentifier NULL,
    [BranchId] uniqueidentifier NULL,
    [AlacarteOrderId] uniqueidentifier NOT NULL,
    [MenuItemId] uniqueidentifier NOT NULL,
    [LineNo] int NOT NULL,
    [MenuItemCode] nvarchar(50) NOT NULL,
    [MenuItemName] nvarchar(255) NOT NULL,
    [UnitName] nvarchar(30) NOT NULL,
    [Qty] decimal(18,3) NOT NULL,
    [UnitPrice] decimal(18,2) NOT NULL,
    [VatRate] decimal(9,4) NOT NULL,
    [VatAmount] decimal(18,2) NOT NULL,
    [DiscountAmount] decimal(18,2) NOT NULL,
    [Amount] decimal(18,2) NOT NULL,
    [KitchenNote] nvarchar(500) NULL,
    [LineStatus] nvarchar(40) NOT NULL,
    [KitchenStatus] nvarchar(40) NOT NULL,
    [LineType] nvarchar(40) NOT NULL,
    [SentAt] datetime2 NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_AlacarteOrderLines] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_AlacarteOrderLines_AlacarteOrders_AlacarteOrderId] FOREIGN KEY ([AlacarteOrderId]) REFERENCES [AlacarteOrders] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_AlacarteOrderLines_MenuItems_MenuItemId] FOREIGN KEY ([MenuItemId]) REFERENCES [MenuItems] ([Id]) ON DELETE NO ACTION
);
GO

CREATE TABLE [RestaurantDeposits] (
    [Id] uniqueidentifier NOT NULL,
    [CompanyId] uniqueidentifier NULL,
    [BranchId] uniqueidentifier NULL,
    [DepositNo] nvarchar(60) NOT NULL,
    [AlacarteOrderId] uniqueidentifier NULL,
    [RestaurantContractId] uniqueidentifier NULL,
    [TableReservationId] uniqueidentifier NULL,
    [Date] datetime2 NOT NULL,
    [Amount] decimal(18,2) NOT NULL,
    [PaymentMethod] nvarchar(40) NOT NULL,
    [CashFundId] uniqueidentifier NULL,
    [BankAccountId] uniqueidentifier NULL,
    [Receiver] nvarchar(120) NULL,
    [Note] nvarchar(1000) NULL,
    [Status] nvarchar(40) NOT NULL,
    CONSTRAINT [PK_RestaurantDeposits] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_RestaurantDeposits_AlacarteOrders_AlacarteOrderId] FOREIGN KEY ([AlacarteOrderId]) REFERENCES [AlacarteOrders] ([Id]),
    CONSTRAINT [FK_RestaurantDeposits_BankAccounts_BankAccountId] FOREIGN KEY ([BankAccountId]) REFERENCES [BankAccounts] ([Id]) ON DELETE SET NULL,
    CONSTRAINT [FK_RestaurantDeposits_CashFunds_CashFundId] FOREIGN KEY ([CashFundId]) REFERENCES [CashFunds] ([Id]) ON DELETE SET NULL,
    CONSTRAINT [FK_RestaurantDeposits_RestaurantContracts_RestaurantContractId] FOREIGN KEY ([RestaurantContractId]) REFERENCES [RestaurantContracts] ([Id]),
    CONSTRAINT [FK_RestaurantDeposits_TableReservations_TableReservationId] FOREIGN KEY ([TableReservationId]) REFERENCES [TableReservations] ([Id]) ON DELETE SET NULL
);
GO

CREATE TABLE [RestaurantOrderExtraServices] (
    [Id] uniqueidentifier NOT NULL,
    [CompanyId] uniqueidentifier NULL,
    [BranchId] uniqueidentifier NULL,
    [AlacarteOrderId] uniqueidentifier NOT NULL,
    [RestaurantExtraServiceId] uniqueidentifier NULL,
    [Description] nvarchar(255) NOT NULL,
    [Qty] decimal(18,3) NOT NULL,
    [UnitPrice] decimal(18,2) NOT NULL,
    [VatRate] decimal(9,4) NOT NULL,
    [Amount] decimal(18,2) NOT NULL,
    CONSTRAINT [PK_RestaurantOrderExtraServices] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_RestaurantOrderExtraServices_AlacarteOrders_AlacarteOrderId] FOREIGN KEY ([AlacarteOrderId]) REFERENCES [AlacarteOrders] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_RestaurantOrderExtraServices_RestaurantExtraServices_RestaurantExtraServiceId] FOREIGN KEY ([RestaurantExtraServiceId]) REFERENCES [RestaurantExtraServices] ([Id]) ON DELETE SET NULL
);
GO

CREATE TABLE [RestaurantPreparationTickets] (
    [Id] uniqueidentifier NOT NULL,
    [CompanyId] uniqueidentifier NULL,
    [BranchId] uniqueidentifier NULL,
    [AlacarteOrderId] uniqueidentifier NOT NULL,
    [TicketNo] nvarchar(60) NOT NULL,
    [Station] nvarchar(50) NOT NULL,
    [TicketType] nvarchar(40) NOT NULL,
    [Status] nvarchar(40) NOT NULL,
    [SentAt] datetime2 NOT NULL,
    [SentBy] nvarchar(120) NULL,
    [Note] nvarchar(1000) NULL,
    CONSTRAINT [PK_RestaurantPreparationTickets] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_RestaurantPreparationTickets_AlacarteOrders_AlacarteOrderId] FOREIGN KEY ([AlacarteOrderId]) REFERENCES [AlacarteOrders] ([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [RestaurantPreparationTicketLines] (
    [Id] uniqueidentifier NOT NULL,
    [CompanyId] uniqueidentifier NULL,
    [BranchId] uniqueidentifier NULL,
    [RestaurantPreparationTicketId] uniqueidentifier NOT NULL,
    [AlacarteOrderLineId] uniqueidentifier NULL,
    [LineNo] int NOT NULL,
    [MenuItemName] nvarchar(255) NOT NULL,
    [Qty] decimal(18,3) NOT NULL,
    [KitchenNote] nvarchar(500) NULL,
    CONSTRAINT [PK_RestaurantPreparationTicketLines] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_RestaurantPreparationTicketLines_AlacarteOrderLines_AlacarteOrderLineId] FOREIGN KEY ([AlacarteOrderLineId]) REFERENCES [AlacarteOrderLines] ([Id]),
    CONSTRAINT [FK_RestaurantPreparationTicketLines_RestaurantPreparationTickets_RestaurantPreparationTicketId] FOREIGN KEY ([RestaurantPreparationTicketId]) REFERENCES [RestaurantPreparationTickets] ([Id]) ON DELETE CASCADE
);
GO

CREATE UNIQUE INDEX [IX_AlacarteOrderLines_AlacarteOrderId_LineNo] ON [AlacarteOrderLines] ([AlacarteOrderId], [LineNo]);
GO

CREATE INDEX [IX_AlacarteOrderLines_MenuItemId] ON [AlacarteOrderLines] ([MenuItemId]);
GO

CREATE INDEX [IX_AlacarteOrders_CustomerId] ON [AlacarteOrders] ([CustomerId]);
GO

CREATE INDEX [IX_AlacarteOrders_DiningTableId_Status] ON [AlacarteOrders] ([DiningTableId], [Status]);
GO

CREATE INDEX [IX_AlacarteOrders_OrderDate] ON [AlacarteOrders] ([OrderDate]);
GO

CREATE UNIQUE INDEX [IX_AlacarteOrders_OrderNo] ON [AlacarteOrders] ([OrderNo]);
GO

CREATE INDEX [IX_AlacarteOrders_ReservationId] ON [AlacarteOrders] ([ReservationId]);
GO

CREATE INDEX [IX_BanquetChecklists_BanquetEventId_SortOrder] ON [BanquetChecklists] ([BanquetEventId], [SortOrder]);
GO

CREATE INDEX [IX_BanquetContracts_BanquetEventId] ON [BanquetContracts] ([BanquetEventId]);
GO

CREATE UNIQUE INDEX [IX_BanquetContracts_ContractNo] ON [BanquetContracts] ([ContractNo]);
GO

CREATE INDEX [IX_BanquetContracts_CustomerId] ON [BanquetContracts] ([CustomerId]);
GO

CREATE INDEX [IX_BanquetDeposits_BankAccountId] ON [BanquetDeposits] ([BankAccountId]);
GO

CREATE INDEX [IX_BanquetDeposits_BanquetContractId] ON [BanquetDeposits] ([BanquetContractId]);
GO

CREATE INDEX [IX_BanquetDeposits_BanquetEventId] ON [BanquetDeposits] ([BanquetEventId]);
GO

CREATE INDEX [IX_BanquetDeposits_CashFundId] ON [BanquetDeposits] ([CashFundId]);
GO

CREATE UNIQUE INDEX [IX_BanquetDeposits_DepositNo] ON [BanquetDeposits] ([DepositNo]);
GO

CREATE INDEX [IX_BanquetEventMenus_BanquetEventId] ON [BanquetEventMenus] ([BanquetEventId]);
GO

CREATE INDEX [IX_BanquetEventMenus_BanquetMenuPackageId] ON [BanquetEventMenus] ([BanquetMenuPackageId]);
GO

CREATE INDEX [IX_BanquetEvents_BanquetRoomId_EventDate_StartTime_EndTime_Status] ON [BanquetEvents] ([BanquetRoomId], [EventDate], [StartTime], [EndTime], [Status]);
GO

CREATE INDEX [IX_BanquetEvents_CustomerId] ON [BanquetEvents] ([CustomerId]);
GO

CREATE UNIQUE INDEX [IX_BanquetEvents_EventNo] ON [BanquetEvents] ([EventNo]);
GO

CREATE INDEX [IX_BanquetFloorPlans_BanquetEventId] ON [BanquetFloorPlans] ([BanquetEventId]);
GO

CREATE UNIQUE INDEX [IX_BanquetFloorPlanTables_BanquetFloorPlanId_TableNo] ON [BanquetFloorPlanTables] ([BanquetFloorPlanId], [TableNo]);
GO

CREATE UNIQUE INDEX [IX_BanquetMenuPackageLines_BanquetMenuPackageId_LineNo] ON [BanquetMenuPackageLines] ([BanquetMenuPackageId], [LineNo]);
GO

CREATE INDEX [IX_BanquetMenuPackageLines_MenuItemId] ON [BanquetMenuPackageLines] ([MenuItemId]);
GO

CREATE UNIQUE INDEX [IX_BanquetMenuPackages_Code] ON [BanquetMenuPackages] ([Code]);
GO

CREATE UNIQUE INDEX [IX_BanquetRooms_Code] ON [BanquetRooms] ([Code]);
GO

CREATE INDEX [IX_BanquetRooms_Status] ON [BanquetRooms] ([Status]);
GO

CREATE INDEX [IX_BanquetServiceLines_BanquetEventId] ON [BanquetServiceLines] ([BanquetEventId]);
GO

CREATE INDEX [IX_BanquetServiceLines_SupplierId] ON [BanquetServiceLines] ([SupplierId]);
GO

CREATE INDEX [IX_BanquetTimelines_BanquetEventId_SortOrder] ON [BanquetTimelines] ([BanquetEventId], [SortOrder]);
GO

CREATE UNIQUE INDEX [IX_DiningRooms_Code] ON [DiningRooms] ([Code]);
GO

CREATE INDEX [IX_DiningRooms_Status] ON [DiningRooms] ([Status]);
GO

CREATE UNIQUE INDEX [IX_DiningTables_Code] ON [DiningTables] ([Code]);
GO

CREATE INDEX [IX_DiningTables_DiningRoomId_SortOrder] ON [DiningTables] ([DiningRoomId], [SortOrder]);
GO

CREATE INDEX [IX_DiningTables_Status] ON [DiningTables] ([Status]);
GO

CREATE UNIQUE INDEX [IX_RestaurantContracts_ContractNo] ON [RestaurantContracts] ([ContractNo]);
GO

CREATE INDEX [IX_RestaurantContracts_CustomerId] ON [RestaurantContracts] ([CustomerId]);
GO

CREATE INDEX [IX_RestaurantDeposits_AlacarteOrderId] ON [RestaurantDeposits] ([AlacarteOrderId]);
GO

CREATE INDEX [IX_RestaurantDeposits_BankAccountId] ON [RestaurantDeposits] ([BankAccountId]);
GO

CREATE INDEX [IX_RestaurantDeposits_CashFundId] ON [RestaurantDeposits] ([CashFundId]);
GO

CREATE UNIQUE INDEX [IX_RestaurantDeposits_DepositNo] ON [RestaurantDeposits] ([DepositNo]);
GO

CREATE INDEX [IX_RestaurantDeposits_RestaurantContractId] ON [RestaurantDeposits] ([RestaurantContractId]);
GO

CREATE INDEX [IX_RestaurantDeposits_TableReservationId] ON [RestaurantDeposits] ([TableReservationId]);
GO

CREATE UNIQUE INDEX [IX_RestaurantExtraServices_Code] ON [RestaurantExtraServices] ([Code]);
GO

CREATE INDEX [IX_RestaurantOrderExtraServices_AlacarteOrderId] ON [RestaurantOrderExtraServices] ([AlacarteOrderId]);
GO

CREATE INDEX [IX_RestaurantOrderExtraServices_RestaurantExtraServiceId] ON [RestaurantOrderExtraServices] ([RestaurantExtraServiceId]);
GO

CREATE INDEX [IX_RestaurantPreparationTicketLines_AlacarteOrderLineId] ON [RestaurantPreparationTicketLines] ([AlacarteOrderLineId]);
GO

CREATE UNIQUE INDEX [IX_RestaurantPreparationTicketLines_RestaurantPreparationTicketId_LineNo] ON [RestaurantPreparationTicketLines] ([RestaurantPreparationTicketId], [LineNo]);
GO

CREATE INDEX [IX_RestaurantPreparationTickets_AlacarteOrderId] ON [RestaurantPreparationTickets] ([AlacarteOrderId]);
GO

CREATE UNIQUE INDEX [IX_RestaurantPreparationTickets_TicketNo] ON [RestaurantPreparationTickets] ([TicketNo]);
GO

CREATE INDEX [IX_TableReservations_CustomerId] ON [TableReservations] ([CustomerId]);
GO

CREATE INDEX [IX_TableReservations_DiningRoomId] ON [TableReservations] ([DiningRoomId]);
GO

CREATE INDEX [IX_TableReservations_DiningTableId_ReservationDateTime_Status] ON [TableReservations] ([DiningTableId], [ReservationDateTime], [Status]);
GO

CREATE INDEX [IX_TableReservations_Phone] ON [TableReservations] ([Phone]);
GO

CREATE UNIQUE INDEX [IX_TableReservations_ReservationNo] ON [TableReservations] ([ReservationNo]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260614055304_AddRestaurantAlacarteAndBanquetModules', N'8.0.7');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF OBJECT_ID(N'[RecipeIngredientSubstitutes]', N'U') IS NULL
BEGIN
    CREATE TABLE [RecipeIngredientSubstitutes] (
        [Id] uniqueidentifier NOT NULL,
        [CompanyId] uniqueidentifier NULL,
        [BranchId] uniqueidentifier NULL,
        [RecipeLineId] uniqueidentifier NOT NULL,
        [InventoryItemId] int NOT NULL,
        [SubstituteInventoryItemId] int NOT NULL,
        [ConversionRate] decimal(18,4) NOT NULL,
        [Priority] int NOT NULL,
        [AutoReplace] bit NOT NULL,
        [Notes] nvarchar(500) NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(120) NULL,
        [ModifiedAt] datetime2 NULL,
        [ModifiedBy] nvarchar(120) NULL,
        [IsDeleted] bit NOT NULL,
        CONSTRAINT [PK_RecipeIngredientSubstitutes] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_RecipeIngredientSubstitutes_MenuItemRecipeLines_RecipeLineId]
            FOREIGN KEY ([RecipeLineId]) REFERENCES [MenuItemRecipeLines]([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_RecipeIngredientSubstitutes_Items_InventoryItemId]
            FOREIGN KEY ([InventoryItemId]) REFERENCES [Items]([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_RecipeIngredientSubstitutes_Items_SubstituteInventoryItemId]
            FOREIGN KEY ([SubstituteInventoryItemId]) REFERENCES [Items]([Id]) ON DELETE NO ACTION
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_RecipeIngredientSubstitutes_RecipeLineId' AND object_id = OBJECT_ID('RecipeIngredientSubstitutes'))
    CREATE INDEX [IX_RecipeIngredientSubstitutes_RecipeLineId] ON [RecipeIngredientSubstitutes]([RecipeLineId]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_RecipeIngredientSubstitutes_InventoryItemId' AND object_id = OBJECT_ID('RecipeIngredientSubstitutes'))
    CREATE INDEX [IX_RecipeIngredientSubstitutes_InventoryItemId] ON [RecipeIngredientSubstitutes]([InventoryItemId]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_RecipeIngredientSubstitutes_SubstituteInventoryItemId' AND object_id = OBJECT_ID('RecipeIngredientSubstitutes'))
    CREATE INDEX [IX_RecipeIngredientSubstitutes_SubstituteInventoryItemId] ON [RecipeIngredientSubstitutes]([SubstituteInventoryItemId]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_RecipeIngredientSubstitutes_RecipeLine_Substitute_IsDeleted' AND object_id = OBJECT_ID('RecipeIngredientSubstitutes'))
    CREATE INDEX [IX_RecipeIngredientSubstitutes_RecipeLine_Substitute_IsDeleted] ON [RecipeIngredientSubstitutes]([RecipeLineId], [SubstituteInventoryItemId], [IsDeleted]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260615090000_AddRecipeIngredientSubstitutes', N'8.0.7');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

CREATE TABLE [RestaurantQuotes] (
    [Id] uniqueidentifier NOT NULL,
    [CompanyId] uniqueidentifier NULL,
    [BranchId] uniqueidentifier NULL,
    [QuoteNo] nvarchar(60) NOT NULL,
    [CustomerName] nvarchar(255) NOT NULL,
    [Phone] nvarchar(40) NOT NULL,
    [TableCount] int NOT NULL,
    [DiningRoomId] uniqueidentifier NULL,
    [DiningTableId] uniqueidentifier NULL,
    [TableReservationId] uniqueidentifier NULL,
    [ApprovedOrderId] uniqueidentifier NULL,
    [DepositId] uniqueidentifier NULL,
    [DepositCashTransactionId] uniqueidentifier NULL,
    [DepositCashTransactionNo] nvarchar(60) NULL,
    [RoomName] nvarchar(255) NOT NULL,
    [EventDate] datetime2 NOT NULL,
    [StartTime] nvarchar(20) NOT NULL,
    [EndTime] nvarchar(20) NOT NULL,
    [TotalAmount] decimal(18,2) NOT NULL,
    [DepositAmount] decimal(18,2) NOT NULL,
    [PaymentDeadline] nvarchar(120) NOT NULL,
    [PaymentMethod] nvarchar(40) NOT NULL,
    [DiscountAmount] decimal(18,2) NOT NULL,
    [Promotion] nvarchar(1000) NOT NULL,
    [ValidityDays] int NOT NULL,
    [CancelDeadlineDays] int NOT NULL,
    [CancelFeePercent] decimal(9,4) NOT NULL,
    [BankAccount] nvarchar(255) NOT NULL,
    [Status] nvarchar(40) NOT NULL,
    [Note] nvarchar(1000) NOT NULL,
    [PreparedBy] nvarchar(120) NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_RestaurantQuotes] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_RestaurantQuotes_AlacarteOrders_ApprovedOrderId] FOREIGN KEY ([ApprovedOrderId]) REFERENCES [AlacarteOrders] ([Id]),
    CONSTRAINT [FK_RestaurantQuotes_CashTransactions_DepositCashTransactionId] FOREIGN KEY ([DepositCashTransactionId]) REFERENCES [CashTransactions] ([Id]) ON DELETE SET NULL,
    CONSTRAINT [FK_RestaurantQuotes_DiningRooms_DiningRoomId] FOREIGN KEY ([DiningRoomId]) REFERENCES [DiningRooms] ([Id]) ON DELETE SET NULL,
    CONSTRAINT [FK_RestaurantQuotes_DiningTables_DiningTableId] FOREIGN KEY ([DiningTableId]) REFERENCES [DiningTables] ([Id]) ON DELETE SET NULL,
    CONSTRAINT [FK_RestaurantQuotes_RestaurantDeposits_DepositId] FOREIGN KEY ([DepositId]) REFERENCES [RestaurantDeposits] ([Id]) ON DELETE SET NULL,
    CONSTRAINT [FK_RestaurantQuotes_TableReservations_TableReservationId] FOREIGN KEY ([TableReservationId]) REFERENCES [TableReservations] ([Id]) ON DELETE SET NULL
);
GO

CREATE TABLE [RestaurantQuoteLines] (
    [Id] uniqueidentifier NOT NULL,
    [CompanyId] uniqueidentifier NULL,
    [BranchId] uniqueidentifier NULL,
    [RestaurantQuoteId] uniqueidentifier NOT NULL,
    [MenuItemId] uniqueidentifier NULL,
    [LineNo] int NOT NULL,
    [Name] nvarchar(255) NOT NULL,
    [LineType] nvarchar(40) NOT NULL,
    [UnitPrice] decimal(18,2) NOT NULL,
    [Quantity] int NOT NULL,
    [Total] decimal(18,2) NOT NULL,
    CONSTRAINT [PK_RestaurantQuoteLines] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_RestaurantQuoteLines_MenuItems_MenuItemId] FOREIGN KEY ([MenuItemId]) REFERENCES [MenuItems] ([Id]) ON DELETE SET NULL,
    CONSTRAINT [FK_RestaurantQuoteLines_RestaurantQuotes_RestaurantQuoteId] FOREIGN KEY ([RestaurantQuoteId]) REFERENCES [RestaurantQuotes] ([Id]) ON DELETE CASCADE
);
GO

CREATE INDEX [IX_RestaurantQuoteLines_MenuItemId] ON [RestaurantQuoteLines] ([MenuItemId]);
GO

CREATE UNIQUE INDEX [IX_RestaurantQuoteLines_RestaurantQuoteId_LineNo] ON [RestaurantQuoteLines] ([RestaurantQuoteId], [LineNo]);
GO

CREATE INDEX [IX_RestaurantQuotes_ApprovedOrderId] ON [RestaurantQuotes] ([ApprovedOrderId]);
GO

CREATE INDEX [IX_RestaurantQuotes_DepositCashTransactionId] ON [RestaurantQuotes] ([DepositCashTransactionId]);
GO

CREATE INDEX [IX_RestaurantQuotes_DepositId] ON [RestaurantQuotes] ([DepositId]);
GO

CREATE INDEX [IX_RestaurantQuotes_DiningRoomId] ON [RestaurantQuotes] ([DiningRoomId]);
GO

CREATE INDEX [IX_RestaurantQuotes_DiningTableId] ON [RestaurantQuotes] ([DiningTableId]);
GO

CREATE UNIQUE INDEX [IX_RestaurantQuotes_QuoteNo] ON [RestaurantQuotes] ([QuoteNo]);
GO

CREATE INDEX [IX_RestaurantQuotes_Status_EventDate] ON [RestaurantQuotes] ([Status], [EventDate]);
GO

CREATE INDEX [IX_RestaurantQuotes_TableReservationId] ON [RestaurantQuotes] ([TableReservationId]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260617031602_AddRestaurantQuotes', N'8.0.7');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

ALTER TABLE [RestaurantQuotes] ADD [GuestCount] int NOT NULL DEFAULT 0;
GO

UPDATE RestaurantQuotes
SET GuestCount = CASE WHEN TableCount > 0 THEN TableCount * 10 ELSE 1 END
WHERE GuestCount <= 0
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260617043114_AddRestaurantQuoteGuestCount', N'8.0.7');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

ALTER TABLE [RestaurantQuotes] ADD [TableStandard] int NOT NULL DEFAULT 0;
GO

ALTER TABLE [RestaurantQuoteLines] ADD [CalculatedQuantity] decimal(18,3) NOT NULL DEFAULT 0.0;
GO

ALTER TABLE [RestaurantQuoteLines] ADD [PricingRule] nvarchar(30) NOT NULL DEFAULT N'';
GO

ALTER TABLE [AlacarteOrders] ADD [ActualGuestCount] int NOT NULL DEFAULT 0;
GO

ALTER TABLE [AlacarteOrders] ADD [DiningRoomId] uniqueidentifier NULL;
GO

ALTER TABLE [AlacarteOrders] ADD [OrderType] nvarchar(30) NOT NULL DEFAULT N'';
GO

ALTER TABLE [AlacarteOrders] ADD [RestaurantQuoteId] uniqueidentifier NULL;
GO

ALTER TABLE [AlacarteOrderLines] ADD [IsSupplement] bit NOT NULL DEFAULT CAST(0 AS bit);
GO

ALTER TABLE [AlacarteOrderLines] ADD [PricingRule] nvarchar(30) NOT NULL DEFAULT N'';
GO

ALTER TABLE [AlacarteOrderLines] ADD [RestaurantOrderTableId] uniqueidentifier NULL;
GO

CREATE TABLE [RestaurantQuoteTables] (
    [Id] uniqueidentifier NOT NULL,
    [CompanyId] uniqueidentifier NULL,
    [BranchId] uniqueidentifier NULL,
    [RestaurantQuoteId] uniqueidentifier NOT NULL,
    [TableNo] int NOT NULL,
    [DisplayName] nvarchar(120) NOT NULL,
    [GuestCount] int NOT NULL,
    [SortOrder] int NOT NULL,
    [IsVirtual] bit NOT NULL,
    [Note] nvarchar(500) NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_RestaurantQuoteTables] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_RestaurantQuoteTables_RestaurantQuotes_RestaurantQuoteId] FOREIGN KEY ([RestaurantQuoteId]) REFERENCES [RestaurantQuotes] ([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [RestaurantOrderTables] (
    [Id] uniqueidentifier NOT NULL,
    [CompanyId] uniqueidentifier NULL,
    [BranchId] uniqueidentifier NULL,
    [AlacarteOrderId] uniqueidentifier NOT NULL,
    [RestaurantQuoteTableId] uniqueidentifier NULL,
    [TableNo] int NOT NULL,
    [DisplayName] nvarchar(120) NOT NULL,
    [GuestCount] int NOT NULL,
    [SortOrder] int NOT NULL,
    [IsVirtual] bit NOT NULL,
    [Status] nvarchar(40) NOT NULL,
    [Note] nvarchar(500) NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_RestaurantOrderTables] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_RestaurantOrderTables_AlacarteOrders_AlacarteOrderId] FOREIGN KEY ([AlacarteOrderId]) REFERENCES [AlacarteOrders] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_RestaurantOrderTables_RestaurantQuoteTables_RestaurantQuoteTableId] FOREIGN KEY ([RestaurantQuoteTableId]) REFERENCES [RestaurantQuoteTables] ([Id]) ON DELETE SET NULL
);
GO

CREATE INDEX [IX_AlacarteOrders_DiningRoomId] ON [AlacarteOrders] ([DiningRoomId]);
GO

CREATE UNIQUE INDEX [IX_AlacarteOrders_RestaurantQuoteId] ON [AlacarteOrders] ([RestaurantQuoteId]) WHERE [RestaurantQuoteId] IS NOT NULL;
GO

CREATE INDEX [IX_AlacarteOrderLines_RestaurantOrderTableId] ON [AlacarteOrderLines] ([RestaurantOrderTableId]);
GO

CREATE INDEX [IX_RestaurantOrderTables_AlacarteOrderId_SortOrder] ON [RestaurantOrderTables] ([AlacarteOrderId], [SortOrder]);
GO

CREATE UNIQUE INDEX [IX_RestaurantOrderTables_AlacarteOrderId_TableNo] ON [RestaurantOrderTables] ([AlacarteOrderId], [TableNo]);
GO

CREATE INDEX [IX_RestaurantOrderTables_RestaurantQuoteTableId] ON [RestaurantOrderTables] ([RestaurantQuoteTableId]);
GO

CREATE INDEX [IX_RestaurantQuoteTables_RestaurantQuoteId_SortOrder] ON [RestaurantQuoteTables] ([RestaurantQuoteId], [SortOrder]);
GO

CREATE UNIQUE INDEX [IX_RestaurantQuoteTables_RestaurantQuoteId_TableNo] ON [RestaurantQuoteTables] ([RestaurantQuoteId], [TableNo]);
GO

ALTER TABLE [AlacarteOrderLines] ADD CONSTRAINT [FK_AlacarteOrderLines_RestaurantOrderTables_RestaurantOrderTableId] FOREIGN KEY ([RestaurantOrderTableId]) REFERENCES [RestaurantOrderTables] ([Id]);
GO

ALTER TABLE [AlacarteOrders] ADD CONSTRAINT [FK_AlacarteOrders_DiningRooms_DiningRoomId] FOREIGN KEY ([DiningRoomId]) REFERENCES [DiningRooms] ([Id]) ON DELETE SET NULL;
GO

ALTER TABLE [AlacarteOrders] ADD CONSTRAINT [FK_AlacarteOrders_RestaurantQuotes_RestaurantQuoteId] FOREIGN KEY ([RestaurantQuoteId]) REFERENCES [RestaurantQuotes] ([Id]) ON DELETE SET NULL;
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260618100620_AddAlacarteVipP0QuoteAndOrderTables', N'8.0.7');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

ALTER TABLE [RestaurantOrderTables] ADD [DiningTableId] uniqueidentifier NULL;
GO

ALTER TABLE [AlacarteOrderLines] ADD [SupplementNote] nvarchar(500) NULL;
GO

ALTER TABLE [AlacarteOrderLines] ADD [SupplementQuantity] decimal(18,3) NOT NULL DEFAULT 0.0;
GO

CREATE TABLE [RestaurantServingGroups] (
    [Id] uniqueidentifier NOT NULL,
    [CompanyId] uniqueidentifier NULL,
    [BranchId] uniqueidentifier NULL,
    [AlacarteOrderId] uniqueidentifier NOT NULL,
    [Code] nvarchar(60) NOT NULL,
    [Name] nvarchar(120) NOT NULL,
    [ScopeType] nvarchar(40) NOT NULL,
    [ServingRule] nvarchar(40) NOT NULL,
    [SortOrder] int NOT NULL,
    [Note] nvarchar(500) NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_RestaurantServingGroups] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_RestaurantServingGroups_AlacarteOrders_AlacarteOrderId] FOREIGN KEY ([AlacarteOrderId]) REFERENCES [AlacarteOrders] ([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [RestaurantServingGroupTables] (
    [Id] uniqueidentifier NOT NULL,
    [CompanyId] uniqueidentifier NULL,
    [BranchId] uniqueidentifier NULL,
    [RestaurantServingGroupId] uniqueidentifier NOT NULL,
    [RestaurantOrderTableId] uniqueidentifier NOT NULL,
    [DiningTableId] uniqueidentifier NULL,
    [GuestCount] int NOT NULL,
    [ExtraGuestCount] int NOT NULL,
    [IsServing] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_RestaurantServingGroupTables] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_RestaurantServingGroupTables_DiningTables_DiningTableId] FOREIGN KEY ([DiningTableId]) REFERENCES [DiningTables] ([Id]) ON DELETE SET NULL,
    CONSTRAINT [FK_RestaurantServingGroupTables_RestaurantOrderTables_RestaurantOrderTableId] FOREIGN KEY ([RestaurantOrderTableId]) REFERENCES [RestaurantOrderTables] ([Id]),
    CONSTRAINT [FK_RestaurantServingGroupTables_RestaurantServingGroups_RestaurantServingGroupId] FOREIGN KEY ([RestaurantServingGroupId]) REFERENCES [RestaurantServingGroups] ([Id]) ON DELETE CASCADE
);
GO

CREATE INDEX [IX_RestaurantOrderTables_DiningTableId] ON [RestaurantOrderTables] ([DiningTableId]);
GO

CREATE UNIQUE INDEX [IX_RestaurantServingGroups_AlacarteOrderId_Code] ON [RestaurantServingGroups] ([AlacarteOrderId], [Code]);
GO

CREATE INDEX [IX_RestaurantServingGroups_AlacarteOrderId_SortOrder] ON [RestaurantServingGroups] ([AlacarteOrderId], [SortOrder]);
GO

CREATE INDEX [IX_RestaurantServingGroupTables_DiningTableId] ON [RestaurantServingGroupTables] ([DiningTableId]);
GO

CREATE INDEX [IX_RestaurantServingGroupTables_RestaurantOrderTableId] ON [RestaurantServingGroupTables] ([RestaurantOrderTableId]);
GO

CREATE UNIQUE INDEX [IX_RestaurantServingGroupTables_RestaurantServingGroupId_RestaurantOrderTableId] ON [RestaurantServingGroupTables] ([RestaurantServingGroupId], [RestaurantOrderTableId]);
GO

ALTER TABLE [RestaurantOrderTables] ADD CONSTRAINT [FK_RestaurantOrderTables_DiningTables_DiningTableId] FOREIGN KEY ([DiningTableId]) REFERENCES [DiningTables] ([Id]) ON DELETE SET NULL;
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260620075759_AddRestaurantServingGroupsAndSupplements', N'8.0.7');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

ALTER TABLE [BanquetMenuPackageLines] ADD [PricingRule] nvarchar(40) NULL;
GO

UPDATE line
SET line.PricingRule = pkg.PricingMode
FROM BanquetMenuPackageLines AS line
INNER JOIN BanquetMenuPackages AS pkg
    ON pkg.Id = line.BanquetMenuPackageId
WHERE line.PricingRule IS NULL OR LTRIM(RTRIM(line.PricingRule)) = ''
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260624021045_AddBanquetMenuPackageLinePricingRule', N'8.0.7');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

CREATE TABLE [QuoteChangeRequests] (
    [Id] uniqueidentifier NOT NULL,
    [CompanyId] uniqueidentifier NULL,
    [BranchId] uniqueidentifier NULL,
    [QuoteId] uniqueidentifier NOT NULL,
    [RequestNumber] nvarchar(60) NOT NULL,
    [PreviousGuestCount] int NOT NULL,
    [NewGuestCount] int NOT NULL,
    [GuestCountDelta] int NOT NULL,
    [Reason] nvarchar(1000) NOT NULL,
    [Status] nvarchar(40) NOT NULL,
    [RequestedBy] nvarchar(200) NOT NULL,
    [RequestedAt] datetime2 NOT NULL,
    [ReviewedBy] nvarchar(200) NULL,
    [ReviewedAt] datetime2 NULL,
    [AppliedBy] nvarchar(200) NULL,
    [AppliedAt] datetime2 NULL,
    [PreviousTotalAmount] decimal(18,2) NOT NULL,
    [NewTotalAmount] decimal(18,2) NOT NULL,
    [AmountDelta] decimal(18,2) NOT NULL,
    [AdjustmentPlanJson] nvarchar(4000) NULL,
    [PreviousOrderId] uniqueidentifier NOT NULL,
    [NewOrderId] uniqueidentifier NULL,
    [Note] nvarchar(1000) NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_QuoteChangeRequests] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_QuoteChangeRequests_AlacarteOrders_NewOrderId] FOREIGN KEY ([NewOrderId]) REFERENCES [AlacarteOrders] ([Id]),
    CONSTRAINT [FK_QuoteChangeRequests_AlacarteOrders_PreviousOrderId] FOREIGN KEY ([PreviousOrderId]) REFERENCES [AlacarteOrders] ([Id]),
    CONSTRAINT [FK_QuoteChangeRequests_RestaurantQuotes_QuoteId] FOREIGN KEY ([QuoteId]) REFERENCES [RestaurantQuotes] ([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [QuoteAuditLogs] (
    [Id] uniqueidentifier NOT NULL,
    [QuoteId] uniqueidentifier NOT NULL,
    [Timestamp] datetime2 NOT NULL,
    [Action] nvarchar(60) NOT NULL,
    [ChangedBy] nvarchar(200) NOT NULL,
    [FieldName] nvarchar(200) NULL,
    [OldValue] nvarchar(500) NULL,
    [NewValue] nvarchar(500) NULL,
    [ChangeRequestId] uniqueidentifier NULL,
    [Note] nvarchar(1000) NULL,
    CONSTRAINT [PK_QuoteAuditLogs] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_QuoteAuditLogs_QuoteChangeRequests_ChangeRequestId] FOREIGN KEY ([ChangeRequestId]) REFERENCES [QuoteChangeRequests] ([Id]) ON DELETE SET NULL,
    CONSTRAINT [FK_QuoteAuditLogs_RestaurantQuotes_QuoteId] FOREIGN KEY ([QuoteId]) REFERENCES [RestaurantQuotes] ([Id])
);
GO

CREATE TABLE [QuoteChangeRequestDetails] (
    [Id] uniqueidentifier NOT NULL,
    [ChangeRequestId] uniqueidentifier NOT NULL,
    [QuoteLineId] uniqueidentifier NULL,
    [OrderLineId] uniqueidentifier NULL,
    [ChangeType] nvarchar(40) NOT NULL,
    [ItemName] nvarchar(500) NOT NULL,
    [PreviousQuantity] decimal(18,4) NOT NULL,
    [NewQuantity] decimal(18,4) NOT NULL,
    [PreviousAmount] decimal(18,2) NOT NULL,
    [NewAmount] decimal(18,2) NOT NULL,
    [OrderTableId] uniqueidentifier NULL,
    [IsAlreadySent] bit NOT NULL,
    [Note] nvarchar(500) NULL,
    CONSTRAINT [PK_QuoteChangeRequestDetails] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_QuoteChangeRequestDetails_QuoteChangeRequests_ChangeRequestId] FOREIGN KEY ([ChangeRequestId]) REFERENCES [QuoteChangeRequests] ([Id]) ON DELETE CASCADE
);
GO

CREATE INDEX [IX_QuoteAuditLogs_ChangeRequestId] ON [QuoteAuditLogs] ([ChangeRequestId]);
GO

CREATE INDEX [IX_QuoteAuditLogs_QuoteId] ON [QuoteAuditLogs] ([QuoteId]);
GO

CREATE INDEX [IX_QuoteAuditLogs_Timestamp] ON [QuoteAuditLogs] ([Timestamp]);
GO

CREATE INDEX [IX_QuoteChangeRequestDetails_ChangeRequestId] ON [QuoteChangeRequestDetails] ([ChangeRequestId]);
GO

CREATE INDEX [IX_QuoteChangeRequests_NewOrderId] ON [QuoteChangeRequests] ([NewOrderId]);
GO

CREATE INDEX [IX_QuoteChangeRequests_PreviousOrderId] ON [QuoteChangeRequests] ([PreviousOrderId]);
GO

CREATE INDEX [IX_QuoteChangeRequests_QuoteId] ON [QuoteChangeRequests] ([QuoteId]);
GO

CREATE UNIQUE INDEX [IX_QuoteChangeRequests_RequestNumber] ON [QuoteChangeRequests] ([RequestNumber]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260624082548_AddQuoteChangeRequest', N'8.0.7');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

ALTER TABLE [QuoteAuditLogs] DROP CONSTRAINT [FK_QuoteAuditLogs_RestaurantQuotes_QuoteId];
GO

ALTER TABLE [QuoteChangeRequests] ADD [RowVersion] rowversion NOT NULL;
GO

ALTER TABLE [QuoteAuditLogs] ADD CONSTRAINT [FK_QuoteAuditLogs_RestaurantQuotes_QuoteId] FOREIGN KEY ([QuoteId]) REFERENCES [RestaurantQuotes] ([Id]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260624091944_AddQuoteChangeRequestRowVersion', N'8.0.7');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

ALTER TABLE [BanquetContracts] DROP CONSTRAINT [FK_BanquetContracts_BanquetEvents_BanquetEventId];
GO

ALTER TABLE [BanquetDeposits] DROP CONSTRAINT [FK_BanquetDeposits_BanquetEvents_BanquetEventId];
GO

ALTER TABLE [BanquetEvents] ADD [BirthdayDate] datetime2 NULL;
GO

ALTER TABLE [BanquetEvents] ADD [BirthdayPersonName] nvarchar(255) NULL;
GO

ALTER TABLE [BanquetEvents] ADD [BrideName] nvarchar(255) NULL;
GO

ALTER TABLE [BanquetEvents] ADD [ConferenceName] nvarchar(255) NULL;
GO

ALTER TABLE [BanquetEvents] ADD [ExtraGuestCount] int NOT NULL DEFAULT 0;
GO

ALTER TABLE [BanquetEvents] ADD [ExtraTableCount] int NOT NULL DEFAULT 0;
GO

ALTER TABLE [BanquetEvents] ADD [GroomName] nvarchar(255) NULL;
GO

ALTER TABLE [BanquetEvents] ADD [MeetingTopic] nvarchar(500) NULL;
GO

ALTER TABLE [BanquetEvents] ADD [OtherEventName] nvarchar(255) NULL;
GO

ALTER TABLE [BanquetEvents] ADD [PartyTheme] nvarchar(500) NULL;
GO

ALTER TABLE [BanquetContracts] ADD CONSTRAINT [FK_BanquetContracts_BanquetEvents_BanquetEventId] FOREIGN KEY ([BanquetEventId]) REFERENCES [BanquetEvents] ([Id]) ON DELETE NO ACTION;
GO

ALTER TABLE [BanquetDeposits] ADD CONSTRAINT [FK_BanquetDeposits_BanquetEvents_BanquetEventId] FOREIGN KEY ([BanquetEventId]) REFERENCES [BanquetEvents] ([Id]) ON DELETE NO ACTION;
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260710033555_CompleteBanquetOverviewEventFields', N'8.0.7');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

ALTER TABLE [BanquetEventMenus] ADD [ItemName] nvarchar(255) NULL;
GO

ALTER TABLE [BanquetEventMenus] ADD [MenuItemId] uniqueidentifier NULL;
GO

ALTER TABLE [BanquetEventMenus] ADD [Note] nvarchar(1000) NULL;
GO

ALTER TABLE [BanquetEventMenus] ADD [Qty] decimal(18,3) NOT NULL DEFAULT 0.0;
GO

CREATE INDEX [IX_BanquetEventMenus_MenuItemId] ON [BanquetEventMenus] ([MenuItemId]);
GO

ALTER TABLE [BanquetEventMenus] ADD CONSTRAINT [FK_BanquetEventMenus_MenuItems_MenuItemId] FOREIGN KEY ([MenuItemId]) REFERENCES [MenuItems] ([Id]) ON DELETE NO ACTION;
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260710040633_AddBanquetEventMenuCustomColumns', N'8.0.7');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

CREATE TABLE [BanquetMenuGroups] (
    [Id] uniqueidentifier NOT NULL,
    [CompanyId] uniqueidentifier NULL,
    [BranchId] uniqueidentifier NULL,
    [Name] nvarchar(max) NOT NULL,
    [SortOrder] int NOT NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_BanquetMenuGroups] PRIMARY KEY ([Id])
);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260711043948_AddBanquetMenuGroups', N'8.0.7');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

ALTER TABLE [BanquetRooms] ADD [LayoutJson] nvarchar(max) NULL;
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260713032500_AddBanquetRoomLayout', N'8.0.7');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

ALTER TABLE [RestaurantExtraServices] ADD [UnitName] nvarchar(40) NOT NULL DEFAULT N'';
GO

ALTER TABLE [BanquetServiceLines] ADD [UnitName] nvarchar(40) NOT NULL DEFAULT N'';
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260715083035_AddBanquetServiceUnits', N'8.0.7');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

CREATE TABLE [BanquetModuleSettings] (
    [Id] uniqueidentifier NOT NULL,
    [CompanyId] uniqueidentifier NULL,
    [BranchId] uniqueidentifier NULL,
    [VenueName] nvarchar(255) NOT NULL,
    [VenueCode] nvarchar(60) NOT NULL,
    [Address] nvarchar(500) NOT NULL,
    [Phone] nvarchar(40) NOT NULL,
    [Email] nvarchar(255) NOT NULL,
    [Website] nvarchar(255) NOT NULL,
    [TaxCode] nvarchar(40) NOT NULL,
    [GuestsPerTable] int NOT NULL,
    [DefaultDepositRate] decimal(9,4) NOT NULL,
    [DefaultVatRate] decimal(9,4) NOT NULL,
    [HoldDays] int NOT NULL,
    [ReminderDays] int NOT NULL,
    [LunchStartTime] nvarchar(20) NOT NULL,
    [DinnerStartTime] nvarchar(20) NOT NULL,
    [AutoGenerateEventNo] bit NOT NULL,
    [WarnRoomConflict] bit NOT NULL,
    [RequireContractBeforeConfirm] bit NOT NULL,
    [EnableDepositReminder] bit NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_BanquetModuleSettings] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [BanquetQuantificationBatches] (
    [Id] uniqueidentifier NOT NULL,
    [CompanyId] uniqueidentifier NULL,
    [BranchId] uniqueidentifier NULL,
    [BanquetEventId] uniqueidentifier NOT NULL,
    [VersionNo] int NOT NULL,
    [Fingerprint] nvarchar(128) NOT NULL,
    [Status] nvarchar(40) NOT NULL,
    [SentAt] datetime2 NOT NULL,
    [SentBy] nvarchar(120) NULL,
    [ReceivedAt] datetime2 NULL,
    [ReceivedBy] nvarchar(120) NULL,
    [CompletedAt] datetime2 NULL,
    [CompletedBy] nvarchar(120) NULL,
    [Note] nvarchar(1000) NULL,
    CONSTRAINT [PK_BanquetQuantificationBatches] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_BanquetQuantificationBatches_BanquetEvents_BanquetEventId] FOREIGN KEY ([BanquetEventId]) REFERENCES [BanquetEvents] ([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [BanquetQuantificationLines] (
    [Id] uniqueidentifier NOT NULL,
    [CompanyId] uniqueidentifier NULL,
    [BranchId] uniqueidentifier NULL,
    [BanquetQuantificationBatchId] uniqueidentifier NOT NULL,
    [SortOrder] int NOT NULL,
    [LineType] nvarchar(40) NOT NULL,
    [Code] nvarchar(80) NOT NULL,
    [Name] nvarchar(255) NOT NULL,
    [UnitName] nvarchar(40) NOT NULL,
    [RequiredQuantity] decimal(18,3) NOT NULL,
    [EstimatedCost] decimal(18,2) NOT NULL,
    [SourceText] nvarchar(500) NULL,
    [Note] nvarchar(1000) NULL,
    CONSTRAINT [PK_BanquetQuantificationLines] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_BanquetQuantificationLines_BanquetQuantificationBatches_BanquetQuantificationBatchId] FOREIGN KEY ([BanquetQuantificationBatchId]) REFERENCES [BanquetQuantificationBatches] ([Id]) ON DELETE CASCADE
);
GO

CREATE UNIQUE INDEX [IX_BanquetModuleSettings_CompanyId_BranchId] ON [BanquetModuleSettings] ([CompanyId], [BranchId]) WHERE [CompanyId] IS NOT NULL AND [BranchId] IS NOT NULL;
GO

CREATE UNIQUE INDEX [IX_BanquetQuantificationBatches_BanquetEventId_VersionNo] ON [BanquetQuantificationBatches] ([BanquetEventId], [VersionNo]);
GO

CREATE INDEX [IX_BanquetQuantificationBatches_Status_SentAt] ON [BanquetQuantificationBatches] ([Status], [SentAt]);
GO

CREATE INDEX [IX_BanquetQuantificationLines_BanquetQuantificationBatchId_SortOrder] ON [BanquetQuantificationLines] ([BanquetQuantificationBatchId], [SortOrder]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260715083738_AddBanquetQuantificationAndSettings', N'8.0.7');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

ALTER TABLE [BanquetFloorPlans] ADD [LayoutJson] nvarchar(max) NULL;
GO

ALTER TABLE [BanquetDeposits] ADD [CashTransactionId] uniqueidentifier NULL;
GO

ALTER TABLE [BanquetDeposits] ADD [CashTransactionNo] nvarchar(60) NULL;
GO

CREATE INDEX [IX_BanquetDeposits_CashTransactionId] ON [BanquetDeposits] ([CashTransactionId]);
GO

ALTER TABLE [BanquetDeposits] ADD CONSTRAINT [FK_BanquetDeposits_CashTransactions_CashTransactionId] FOREIGN KEY ([CashTransactionId]) REFERENCES [CashTransactions] ([Id]) ON DELETE SET NULL;
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260715085020_AddBanquetFloorPlanAndDepositReceipt', N'8.0.7');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

DROP INDEX [IX_BanquetDeposits_BanquetEventId] ON [BanquetDeposits];
GO

ALTER TABLE [BanquetEvents] ADD [FinanciallyLockedAt] datetime2 NULL;
GO

ALTER TABLE [BanquetEvents] ADD [FinanciallyLockedBy] nvarchar(120) NULL;
GO

ALTER TABLE [BanquetEvents] ADD [IsFinanciallyLocked] bit NOT NULL DEFAULT CAST(0 AS bit);
GO

ALTER TABLE [BanquetEvents] ADD [RowVersion] rowversion NULL;
GO

ALTER TABLE [BanquetDeposits] ADD [IdempotencyKey] nvarchar(80) NULL;
GO

ALTER TABLE [BanquetDeposits] ADD [RefundAmount] decimal(18,2) NOT NULL DEFAULT 0.0;
GO

ALTER TABLE [BanquetDeposits] ADD [RefundCashTransactionId] uniqueidentifier NULL;
GO

ALTER TABLE [BanquetDeposits] ADD [RefundCashTransactionNo] nvarchar(60) NULL;
GO

ALTER TABLE [BanquetDeposits] ADD [RefundReason] nvarchar(1000) NULL;
GO

ALTER TABLE [BanquetDeposits] ADD [RefundedAt] datetime2 NULL;
GO

ALTER TABLE [BanquetDeposits] ADD [RefundedBy] nvarchar(120) NULL;
GO

ALTER TABLE [BanquetDeposits] ADD [RowVersion] rowversion NULL;
GO

ALTER TABLE [BanquetContracts] ADD [RowVersion] rowversion NULL;
GO

CREATE UNIQUE INDEX [IX_BanquetDeposits_BanquetEventId_IdempotencyKey] ON [BanquetDeposits] ([BanquetEventId], [IdempotencyKey]) WHERE [IdempotencyKey] IS NOT NULL;
GO

CREATE INDEX [IX_BanquetDeposits_RefundCashTransactionId] ON [BanquetDeposits] ([RefundCashTransactionId]);
GO

ALTER TABLE [BanquetDeposits] ADD CONSTRAINT [FK_BanquetDeposits_CashTransactions_RefundCashTransactionId] FOREIGN KEY ([RefundCashTransactionId]) REFERENCES [CashTransactions] ([Id]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260715100639_AddBanquetCommercialControls', N'8.0.7');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

CREATE TABLE [BanquetPurchaseRequests] (
    [Id] uniqueidentifier NOT NULL,
    [CompanyId] uniqueidentifier NULL,
    [BranchId] uniqueidentifier NULL,
    [RequestNo] nvarchar(60) NOT NULL,
    [BanquetEventId] uniqueidentifier NOT NULL,
    [BanquetQuantificationBatchId] uniqueidentifier NOT NULL,
    [RequiredDate] datetime2 NOT NULL,
    [Status] nvarchar(40) NOT NULL,
    [RequestedAt] datetime2 NOT NULL,
    [RequestedBy] nvarchar(120) NULL,
    [Note] nvarchar(1000) NULL,
    CONSTRAINT [PK_BanquetPurchaseRequests] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_BanquetPurchaseRequests_BanquetEvents_BanquetEventId] FOREIGN KEY ([BanquetEventId]) REFERENCES [BanquetEvents] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_BanquetPurchaseRequests_BanquetQuantificationBatches_BanquetQuantificationBatchId] FOREIGN KEY ([BanquetQuantificationBatchId]) REFERENCES [BanquetQuantificationBatches] ([Id]) ON DELETE NO ACTION
);
GO

CREATE TABLE [BanquetPurchaseRequestLines] (
    [Id] uniqueidentifier NOT NULL,
    [CompanyId] uniqueidentifier NULL,
    [BranchId] uniqueidentifier NULL,
    [BanquetPurchaseRequestId] uniqueidentifier NOT NULL,
    [SortOrder] int NOT NULL,
    [ItemCode] nvarchar(80) NOT NULL,
    [ItemName] nvarchar(255) NOT NULL,
    [UnitName] nvarchar(40) NOT NULL,
    [RequestedQuantity] decimal(18,3) NOT NULL,
    [EstimatedCost] decimal(18,2) NOT NULL,
    [Note] nvarchar(500) NULL,
    CONSTRAINT [PK_BanquetPurchaseRequestLines] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_BanquetPurchaseRequestLines_BanquetPurchaseRequests_BanquetPurchaseRequestId] FOREIGN KEY ([BanquetPurchaseRequestId]) REFERENCES [BanquetPurchaseRequests] ([Id]) ON DELETE CASCADE
);
GO

CREATE UNIQUE INDEX [IX_BanquetPurchaseRequestLines_BanquetPurchaseRequestId_SortOrder] ON [BanquetPurchaseRequestLines] ([BanquetPurchaseRequestId], [SortOrder]);
GO

CREATE INDEX [IX_BanquetPurchaseRequests_BanquetEventId] ON [BanquetPurchaseRequests] ([BanquetEventId]);
GO

CREATE UNIQUE INDEX [IX_BanquetPurchaseRequests_BanquetQuantificationBatchId] ON [BanquetPurchaseRequests] ([BanquetQuantificationBatchId]);
GO

CREATE UNIQUE INDEX [IX_BanquetPurchaseRequests_RequestNo] ON [BanquetPurchaseRequests] ([RequestNo]);
GO

CREATE INDEX [IX_BanquetPurchaseRequests_RequiredDate_Status] ON [BanquetPurchaseRequests] ([RequiredDate], [Status]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260715101214_AddBanquetPurchaseRequests', N'8.0.7');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

ALTER TABLE [BanquetServiceLines] ADD [CostAmount] decimal(18,2) NOT NULL DEFAULT 0.0;
GO

ALTER TABLE [BanquetServiceLines] ADD [CostPrice] decimal(18,2) NOT NULL DEFAULT 0.0;
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260716085141_AddBanquetServiceCosting', N'8.0.7');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF SCHEMA_ID(N'fo') IS NULL EXEC(N'CREATE SCHEMA [fo];');
GO

CREATE TABLE [fo].[fo_hop_thu_den] (
    [ten_bo_tieu_thu] nvarchar(160) NOT NULL,
    [ma_thong_diep] uniqueidentifier NOT NULL,
    [ma_bam_noi_dung] nvarchar(64) NOT NULL,
    [nhan_luc_utc] datetime2 NOT NULL,
    [hoan_tat_luc_utc] datetime2 NULL,
    [trang_thai] nvarchar(30) NOT NULL,
    [so_lan_thu_lai] int NOT NULL,
    [ma_loi_cuoi] nvarchar(100) NULL,
    [chu_so_huu_thue] nvarchar(120) NULL,
    [het_han_thue_utc] datetime2 NULL,
    [phien_ban_dong] rowversion NULL,
    CONSTRAINT [PK_fo_hop_thu_den] PRIMARY KEY ([ten_bo_tieu_thu], [ma_thong_diep])
);
GO

CREATE TABLE [fo].[fo_hop_thu_di] (
    [ma_thong_diep] uniqueidentifier NOT NULL,
    [ten_su_kien] nvarchar(200) NOT NULL,
    [phien_ban_schema] int NOT NULL,
    [xay_ra_luc_utc] datetime2 NOT NULL,
    [xuat_ban_luc_utc] datetime2 NULL,
    [ma_co_so] uniqueidentifier NULL,
    [ngay_nghiep_vu] date NULL,
    [ma_tuong_quan] uniqueidentifier NULL,
    [ma_nguyen_nhan] uniqueidentifier NULL,
    [loai_tong_hop] nvarchar(160) NOT NULL,
    [ma_tong_hop] nvarchar(100) NOT NULL,
    [phien_ban_tong_hop] bigint NOT NULL,
    [khoa_phan_vung] nvarchar(200) NOT NULL,
    [do_nhay_cam] nvarchar(30) NOT NULL,
    [noi_dung] nvarchar(max) NOT NULL,
    [ma_bam_noi_dung] nvarchar(64) NOT NULL,
    [trang_thai] nvarchar(30) NOT NULL,
    [so_lan_thu_lai] int NOT NULL,
    [thu_lai_luc_utc] datetime2 NULL,
    [chu_so_huu_thue] nvarchar(120) NULL,
    [het_han_thue_utc] datetime2 NULL,
    [ma_loi_cuoi] nvarchar(100) NULL,
    [tao_luc_utc] datetime2 NOT NULL,
    [phien_ban_dong] rowversion NULL,
    CONSTRAINT [PK_fo_hop_thu_di] PRIMARY KEY ([ma_thong_diep])
);
GO

CREATE TABLE [fo].[fo_khoa_idempotency] (
    [ma] uniqueidentifier NOT NULL,
    [khong_gian_ten] nvarchar(120) NOT NULL,
    [khoa] nvarchar(160) NOT NULL,
    [ma_lenh] uniqueidentifier NOT NULL,
    [ma_bam_yeu_cau] nvarchar(64) NOT NULL,
    [trang_thai] nvarchar(30) NOT NULL,
    [loai_ket_qua] nvarchar(100) NULL,
    [tham_chieu_ket_qua] nvarchar(500) NULL,
    [tao_luc_utc] datetime2 NOT NULL,
    [hoan_tat_luc_utc] datetime2 NULL,
    [het_han_luc_utc] datetime2 NOT NULL,
    [phan_loai_that_bai] nvarchar(80) NULL,
    [phien_ban_dong] rowversion NULL,
    CONSTRAINT [PK_fo_khoa_idempotency] PRIMARY KEY ([ma])
);
GO

CREATE INDEX [IX_fo_hop_thu_di_het_han_thue] ON [fo].[fo_hop_thu_di] ([het_han_thue_utc]);
GO

CREATE INDEX [IX_fo_hop_thu_di_ma_bam] ON [fo].[fo_hop_thu_di] ([ma_bam_noi_dung]);
GO

CREATE INDEX [IX_fo_hop_thu_di_trang_thai_thu_lai] ON [fo].[fo_hop_thu_di] ([trang_thai], [thu_lai_luc_utc]);
GO

CREATE INDEX [IX_fo_khoa_idempotency_trang_thai_het_han] ON [fo].[fo_khoa_idempotency] ([trang_thai], [het_han_luc_utc]);
GO

CREATE UNIQUE INDEX [UQ_fo_khoa_idempotency_khong_gian_khoa] ON [fo].[fo_khoa_idempotency] ([khong_gian_ten], [khoa]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260717102111_CreateFrontOfficeFoundationTechnicalStores', N'8.0.7');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF SCHEMA_ID(N'fo_process') IS NULL EXEC(N'CREATE SCHEMA [fo_process];');
GO

CREATE TABLE [fo_process].[QuyTrinhVanHanh] (
    [ProcessId] uniqueidentifier NOT NULL,
    [ProcessKey] nvarchar(200) NOT NULL,
    [ProcessType] nvarchar(120) NOT NULL,
    [ProcessVersion] int NOT NULL,
    [PropertyId] uniqueidentifier NOT NULL,
    [BusinessDate] date NOT NULL,
    [RootSubjectType] nvarchar(100) NOT NULL,
    [RootSubjectId] nvarchar(120) NOT NULL,
    [Status] nvarchar(40) NOT NULL,
    [CurrentState] nvarchar(80) NOT NULL,
    [CurrentStep] nvarchar(120) NULL,
    [StartedAtUtc] datetime2 NOT NULL,
    [UpdatedAtUtc] datetime2 NOT NULL,
    [CompletedAtUtc] datetime2 NULL,
    [ExpiresAtUtc] datetime2 NULL,
    [CorrelationId] uniqueidentifier NOT NULL,
    [CausationId] uniqueidentifier NULL,
    [InitiatingCommandId] uniqueidentifier NULL,
    [InitiatedByActorId] uniqueidentifier NULL,
    [RetryCount] int NOT NULL,
    [LastErrorCode] nvarchar(100) NULL,
    [LastErrorAtUtc] datetime2 NULL,
    [RequiresManualAction] bit NOT NULL,
    [CompensationStatus] nvarchar(40) NULL,
    [ResumeToken] nvarchar(160) NULL,
    [RowVersion] rowversion NULL,
    CONSTRAINT [PK_fo_process_QuyTrinhVanHanh] PRIMARY KEY ([ProcessId])
);
GO

CREATE TABLE [fo_process].[BuocQuyTrinh] (
    [StepId] uniqueidentifier NOT NULL,
    [ProcessId] uniqueidentifier NOT NULL,
    [StepName] nvarchar(120) NOT NULL,
    [StepVersion] int NOT NULL,
    [Sequence] int NOT NULL,
    [Status] nvarchar(40) NOT NULL,
    [StartedAtUtc] datetime2 NULL,
    [CompletedAtUtc] datetime2 NULL,
    [TimeoutAtUtc] datetime2 NULL,
    [RetryCount] int NOT NULL,
    [LastErrorCode] nvarchar(100) NULL,
    [ExpectedSignalType] nvarchar(120) NULL,
    [CommandReference] nvarchar(500) NULL,
    [ResultReference] nvarchar(500) NULL,
    [CompensationRequired] bit NOT NULL,
    [RowVersion] rowversion NULL,
    CONSTRAINT [PK_fo_process_BuocQuyTrinh] PRIMARY KEY ([StepId]),
    CONSTRAINT [FK_BuocQuyTrinh_QuyTrinhVanHanh_ProcessId] FOREIGN KEY ([ProcessId]) REFERENCES [fo_process].[QuyTrinhVanHanh] ([ProcessId]) ON DELETE NO ACTION
);
GO

CREATE TABLE [fo_process].[HanhDongThuCongQuyTrinh] (
    [ManualActionId] uniqueidentifier NOT NULL,
    [ProcessId] uniqueidentifier NOT NULL,
    [ActionType] nvarchar(100) NOT NULL,
    [Status] nvarchar(40) NOT NULL,
    [RequiredPermission] nvarchar(120) NOT NULL,
    [AssignedRole] nvarchar(120) NULL,
    [AssignedActorId] uniqueidentifier NULL,
    [ReasonRequired] bit NOT NULL,
    [ApprovalRequired] bit NOT NULL,
    [CreatedAtUtc] datetime2 NOT NULL,
    [DueAtUtc] datetime2 NULL,
    [CompletedAtUtc] datetime2 NULL,
    [CompletedByActorId] uniqueidentifier NULL,
    [ResultCode] nvarchar(100) NULL,
    [AuditReference] nvarchar(500) NULL,
    CONSTRAINT [PK_fo_process_HanhDongThuCongQuyTrinh] PRIMARY KEY ([ManualActionId]),
    CONSTRAINT [FK_HanhDongThuCongQuyTrinh_QuyTrinhVanHanh_ProcessId] FOREIGN KEY ([ProcessId]) REFERENCES [fo_process].[QuyTrinhVanHanh] ([ProcessId]) ON DELETE NO ACTION
);
GO

CREATE TABLE [fo_process].[LichSuQuyTrinh] (
    [HistoryId] uniqueidentifier NOT NULL,
    [ProcessId] uniqueidentifier NOT NULL,
    [PreviousState] nvarchar(80) NULL,
    [NewState] nvarchar(80) NOT NULL,
    [PreviousStep] nvarchar(120) NULL,
    [NewStep] nvarchar(120) NULL,
    [TransitionType] nvarchar(80) NOT NULL,
    [SignalId] uniqueidentifier NULL,
    [CommandId] uniqueidentifier NULL,
    [EventId] uniqueidentifier NULL,
    [ActorId] uniqueidentifier NULL,
    [OccurredAtUtc] datetime2 NOT NULL,
    [ErrorCode] nvarchar(100) NULL,
    [ReasonCode] nvarchar(100) NULL,
    [CorrelationId] uniqueidentifier NOT NULL,
    CONSTRAINT [PK_fo_process_LichSuQuyTrinh] PRIMARY KEY ([HistoryId]),
    CONSTRAINT [FK_LichSuQuyTrinh_QuyTrinhVanHanh_ProcessId] FOREIGN KEY ([ProcessId]) REFERENCES [fo_process].[QuyTrinhVanHanh] ([ProcessId]) ON DELETE NO ACTION
);
GO

CREATE TABLE [fo_process].[TinHieuQuyTrinh] (
    [SignalId] uniqueidentifier NOT NULL,
    [ProcessId] uniqueidentifier NOT NULL,
    [SignalType] nvarchar(120) NOT NULL,
    [MessageId] uniqueidentifier NULL,
    [PayloadHash] nvarchar(64) NOT NULL,
    [ReceivedAtUtc] datetime2 NOT NULL,
    [ProcessedAtUtc] datetime2 NULL,
    [Status] nvarchar(40) NOT NULL,
    [IsOutOfOrder] bit NOT NULL,
    [CorrelationId] uniqueidentifier NOT NULL,
    [CausationId] uniqueidentifier NULL,
    [SchemaVersion] int NOT NULL,
    [ErrorCode] nvarchar(100) NULL,
    CONSTRAINT [PK_fo_process_TinHieuQuyTrinh] PRIMARY KEY ([ProcessId], [SignalId]),
    CONSTRAINT [FK_TinHieuQuyTrinh_QuyTrinhVanHanh_ProcessId] FOREIGN KEY ([ProcessId]) REFERENCES [fo_process].[QuyTrinhVanHanh] ([ProcessId]) ON DELETE NO ACTION
);
GO

CREATE TABLE [fo_process].[BuTruQuyTrinh] (
    [CompensationId] uniqueidentifier NOT NULL,
    [ProcessId] uniqueidentifier NOT NULL,
    [StepId] uniqueidentifier NULL,
    [CompensationType] nvarchar(100) NOT NULL,
    [Status] nvarchar(40) NOT NULL,
    [RequestedAtUtc] datetime2 NOT NULL,
    [StartedAtUtc] datetime2 NULL,
    [CompletedAtUtc] datetime2 NULL,
    [CommandReference] nvarchar(500) NULL,
    [ErrorCode] nvarchar(100) NULL,
    [RetryCount] int NOT NULL,
    [ApprovalReference] nvarchar(500) NULL,
    [AuditReference] nvarchar(500) NULL,
    CONSTRAINT [PK_fo_process_BuTruQuyTrinh] PRIMARY KEY ([CompensationId]),
    CONSTRAINT [FK_BuTruQuyTrinh_BuocQuyTrinh_StepId] FOREIGN KEY ([StepId]) REFERENCES [fo_process].[BuocQuyTrinh] ([StepId]) ON DELETE NO ACTION,
    CONSTRAINT [FK_BuTruQuyTrinh_QuyTrinhVanHanh_ProcessId] FOREIGN KEY ([ProcessId]) REFERENCES [fo_process].[QuyTrinhVanHanh] ([ProcessId]) ON DELETE NO ACTION
);
GO

CREATE TABLE [fo_process].[HenGioQuyTrinh] (
    [TimeoutId] uniqueidentifier NOT NULL,
    [ProcessId] uniqueidentifier NOT NULL,
    [StepId] uniqueidentifier NULL,
    [TimeoutType] nvarchar(120) NOT NULL,
    [DueAtUtc] datetime2 NOT NULL,
    [Status] nvarchar(40) NOT NULL,
    [ClaimedBy] nvarchar(120) NULL,
    [LeaseExpiresAtUtc] datetime2 NULL,
    [AttemptCount] int NOT NULL,
    [FiredAtUtc] datetime2 NULL,
    [CancelledAtUtc] datetime2 NULL,
    [CorrelationId] uniqueidentifier NOT NULL,
    [RowVersion] rowversion NULL,
    CONSTRAINT [PK_fo_process_HenGioQuyTrinh] PRIMARY KEY ([TimeoutId]),
    CONSTRAINT [FK_HenGioQuyTrinh_BuocQuyTrinh_StepId] FOREIGN KEY ([StepId]) REFERENCES [fo_process].[BuocQuyTrinh] ([StepId]) ON DELETE NO ACTION,
    CONSTRAINT [FK_HenGioQuyTrinh_QuyTrinhVanHanh_ProcessId] FOREIGN KEY ([ProcessId]) REFERENCES [fo_process].[QuyTrinhVanHanh] ([ProcessId]) ON DELETE NO ACTION
);
GO

CREATE TABLE [fo_process].[LanThucHienBuocQuyTrinh] (
    [AttemptId] uniqueidentifier NOT NULL,
    [ProcessId] uniqueidentifier NOT NULL,
    [StepId] uniqueidentifier NOT NULL,
    [AttemptNumber] int NOT NULL,
    [StartedAtUtc] datetime2 NOT NULL,
    [CompletedAtUtc] datetime2 NULL,
    [Outcome] nvarchar(40) NOT NULL,
    [ErrorCode] nvarchar(100) NULL,
    [RetryClassification] nvarchar(60) NULL,
    [CorrelationId] uniqueidentifier NOT NULL,
    [CommandId] uniqueidentifier NULL,
    [MessageId] uniqueidentifier NULL,
    [WorkerId] nvarchar(120) NULL,
    [DurationMilliseconds] bigint NULL,
    [PayloadHash] nvarchar(64) NULL,
    [SanitizedMetadata] nvarchar(2000) NULL,
    CONSTRAINT [PK_fo_process_LanThucHienBuocQuyTrinh] PRIMARY KEY ([AttemptId]),
    CONSTRAINT [FK_LanThucHienBuocQuyTrinh_BuocQuyTrinh_StepId] FOREIGN KEY ([StepId]) REFERENCES [fo_process].[BuocQuyTrinh] ([StepId]) ON DELETE NO ACTION,
    CONSTRAINT [FK_LanThucHienBuocQuyTrinh_QuyTrinhVanHanh_ProcessId] FOREIGN KEY ([ProcessId]) REFERENCES [fo_process].[QuyTrinhVanHanh] ([ProcessId]) ON DELETE NO ACTION
);
GO

CREATE UNIQUE INDEX [UQ_fo_process_BuocQuyTrinh_Process_Sequence] ON [fo_process].[BuocQuyTrinh] ([ProcessId], [Sequence]);
GO

CREATE INDEX [IX_BuTruQuyTrinh_StepId] ON [fo_process].[BuTruQuyTrinh] ([StepId]);
GO

CREATE INDEX [IX_fo_process_BuTruQuyTrinh_Process_Status] ON [fo_process].[BuTruQuyTrinh] ([ProcessId], [Status]);
GO

CREATE INDEX [IX_fo_process_HanhDongThuCongQuyTrinh_Status_Due] ON [fo_process].[HanhDongThuCongQuyTrinh] ([Status], [DueAtUtc]);
GO

CREATE INDEX [IX_HanhDongThuCongQuyTrinh_ProcessId] ON [fo_process].[HanhDongThuCongQuyTrinh] ([ProcessId]);
GO

CREATE INDEX [IX_fo_process_HenGioQuyTrinh_Claim] ON [fo_process].[HenGioQuyTrinh] ([Status], [DueAtUtc], [LeaseExpiresAtUtc]);
GO

CREATE INDEX [IX_HenGioQuyTrinh_ProcessId] ON [fo_process].[HenGioQuyTrinh] ([ProcessId]);
GO

CREATE INDEX [IX_HenGioQuyTrinh_StepId] ON [fo_process].[HenGioQuyTrinh] ([StepId]);
GO

CREATE INDEX [IX_LanThucHienBuocQuyTrinh_ProcessId] ON [fo_process].[LanThucHienBuocQuyTrinh] ([ProcessId]);
GO

CREATE UNIQUE INDEX [UQ_fo_process_LanThucHienBuocQuyTrinh_Step_Attempt] ON [fo_process].[LanThucHienBuocQuyTrinh] ([StepId], [AttemptNumber]);
GO

CREATE INDEX [IX_fo_process_LichSuQuyTrinh_Process_Occurred] ON [fo_process].[LichSuQuyTrinh] ([ProcessId], [OccurredAtUtc]);
GO

CREATE INDEX [IX_fo_process_QuyTrinhVanHanh_Correlation] ON [fo_process].[QuyTrinhVanHanh] ([CorrelationId]);
GO

CREATE INDEX [IX_fo_process_QuyTrinhVanHanh_Status_Expiry] ON [fo_process].[QuyTrinhVanHanh] ([Status], [ExpiresAtUtc]);
GO

CREATE INDEX [IX_fo_process_QuyTrinhVanHanh_Subject] ON [fo_process].[QuyTrinhVanHanh] ([PropertyId], [RootSubjectType], [RootSubjectId]);
GO

CREATE UNIQUE INDEX [UQ_fo_process_QuyTrinhVanHanh_ProcessKey] ON [fo_process].[QuyTrinhVanHanh] ([ProcessKey]);
GO

CREATE INDEX [IX_fo_process_TinHieuQuyTrinh_Correlation] ON [fo_process].[TinHieuQuyTrinh] ([CorrelationId]);
GO

CREATE UNIQUE INDEX [UQ_fo_process_TinHieuQuyTrinh_Process_Signal] ON [fo_process].[TinHieuQuyTrinh] ([ProcessId], [SignalId]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260717105902_CreateFrontOfficeProcessManagerFoundation', N'8.0.7');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

ALTER TABLE [fo_process].[HenGioQuyTrinh] ADD [DeadLetteredAtUtc] datetime2 NULL;
GO

ALTER TABLE [fo_process].[HenGioQuyTrinh] ADD [LastErrorCode] nvarchar(max) NULL;
GO

ALTER TABLE [fo_process].[HenGioQuyTrinh] ADD [NextRetryAtUtc] datetime2 NULL;
GO

ALTER TABLE [fo_process].[HanhDongThuCongQuyTrinh] ADD [ApprovedByActorId] uniqueidentifier NULL;
GO

ALTER TABLE [fo_process].[HanhDongThuCongQuyTrinh] ADD [CreatedByActorId] uniqueidentifier NULL;
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260717124748_HardenFrontOfficeProcessManagerRuntime', N'8.0.7');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

CREATE TABLE [fo_process].[ThuDaChetQuyTrinh] (
    [DeadLetterId] uniqueidentifier NOT NULL,
    [TimeoutId] uniqueidentifier NOT NULL,
    [ProcessId] uniqueidentifier NOT NULL,
    [SignalId] uniqueidentifier NOT NULL,
    [Reason] nvarchar(200) NOT NULL,
    [ErrorCode] nvarchar(100) NOT NULL,
    [RetryCount] int NOT NULL,
    [OccurredAtUtc] datetime2 NOT NULL,
    [WorkerId] nvarchar(120) NOT NULL,
    [CorrelationId] uniqueidentifier NOT NULL,
    [BusinessDate] date NOT NULL,
    CONSTRAINT [PK_fo_process_ThuDaChetQuyTrinh] PRIMARY KEY ([DeadLetterId]),
    CONSTRAINT [FK_fo_process_ThuDaChetQuyTrinh_QuyTrinhVanHanh_ProcessId] FOREIGN KEY ([ProcessId]) REFERENCES [fo_process].[QuyTrinhVanHanh] ([ProcessId]) ON DELETE NO ACTION,
    CONSTRAINT [FK_fo_process_ThuDaChetQuyTrinh_HenGioQuyTrinh_TimeoutId] FOREIGN KEY ([TimeoutId]) REFERENCES [fo_process].[HenGioQuyTrinh] ([TimeoutId]) ON DELETE NO ACTION
);
GO

CREATE UNIQUE INDEX [UQ_fo_process_ThuDaChetQuyTrinh_Timeout] ON [fo_process].[ThuDaChetQuyTrinh] ([TimeoutId]) WHERE [TimeoutId] IS NOT NULL;
GO

CREATE INDEX [IX_fo_process_ThuDaChetQuyTrinh_Process_Occurred] ON [fo_process].[ThuDaChetQuyTrinh] ([ProcessId], [OccurredAtUtc]);
GO

CREATE INDEX [IX_fo_process_ThuDaChetQuyTrinh_BusinessDate_Occurred] ON [fo_process].[ThuDaChetQuyTrinh] ([BusinessDate], [OccurredAtUtc]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260717143000_AddFrontOfficeProcessWorkerDeadLetters', N'8.0.7');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

CREATE SEQUENCE [fo].[TrinhTuSoDatPhong] START WITH 1 INCREMENT BY 1 NO MINVALUE NO MAXVALUE NO CYCLE;
GO

CREATE TABLE [fo].[DatPhong] (
    [ReservationId] uniqueidentifier NOT NULL,
    [ReservationNumber] nvarchar(40) NOT NULL,
    [PropertyId] uniqueidentifier NOT NULL,
    [GuestId] uniqueidentifier NULL,
    [GuestNameSnapshot] nvarchar(200) NOT NULL,
    [GuestPhoneSnapshot] nvarchar(50) NULL,
    [GuestEmailSnapshot] nvarchar(200) NULL,
    [ArrivalDate] date NOT NULL,
    [DepartureDate] date NOT NULL,
    [AdultCount] int NOT NULL,
    [ChildCount] int NOT NULL,
    [RoomTypeId] uniqueidentifier NULL,
    [AssignedRoomId] uniqueidentifier NULL,
    [RatePlanId] uniqueidentifier NULL,
    [Source] nvarchar(80) NOT NULL,
    [Status] nvarchar(30) NOT NULL,
    [SpecialRequests] nvarchar(1000) NULL,
    [InternalNotes] nvarchar(1000) NULL,
    [CancellationReason] nvarchar(300) NULL,
    [CreatedAtUtc] datetime2 NOT NULL,
    [CreatedBy] uniqueidentifier NULL,
    [UpdatedAtUtc] datetime2 NOT NULL,
    [UpdatedBy] uniqueidentifier NULL,
    [ConfirmedAtUtc] datetime2 NULL,
    [CancelledAtUtc] datetime2 NULL,
    [NoShowAtUtc] datetime2 NULL,
    [RowVersion] rowversion NULL,
    CONSTRAINT [PK_fo_DatPhong] PRIMARY KEY ([ReservationId])
);
GO

CREATE TABLE [fo].[LichSuDatPhong] (
    [HistoryId] uniqueidentifier NOT NULL,
    [ReservationId] uniqueidentifier NOT NULL,
    [Action] nvarchar(80) NOT NULL,
    [FromStatus] nvarchar(30) NULL,
    [ToStatus] nvarchar(30) NOT NULL,
    [OccurredAtUtc] datetime2 NOT NULL,
    [ActorId] uniqueidentifier NULL,
    [CorrelationId] uniqueidentifier NOT NULL,
    [ReasonCode] nvarchar(100) NULL,
    [SafeSummary] nvarchar(300) NULL,
    CONSTRAINT [PK_fo_LichSuDatPhong] PRIMARY KEY ([HistoryId]),
    CONSTRAINT [FK_LichSuDatPhong_DatPhong_ReservationId] FOREIGN KEY ([ReservationId]) REFERENCES [fo].[DatPhong] ([ReservationId]) ON DELETE NO ACTION
);
GO

CREATE UNIQUE INDEX [UQ_fo_DatPhong_CoSo_SoDatPhong] ON [fo].[DatPhong] ([PropertyId], [ReservationNumber]);
GO

CREATE INDEX [IX_fo_DatPhong_CoSo_NgayO] ON [fo].[DatPhong] ([PropertyId], [ArrivalDate], [DepartureDate]);
GO

CREATE INDEX [IX_fo_DatPhong_CoSo_TrangThai] ON [fo].[DatPhong] ([PropertyId], [Status]);
GO

CREATE INDEX [IX_fo_DatPhong_Phong_NgayO] ON [fo].[DatPhong] ([PropertyId], [AssignedRoomId], [ArrivalDate], [DepartureDate]);
GO

CREATE INDEX [IX_fo_LichSuDatPhong_DatPhong_Luc] ON [fo].[LichSuDatPhong] ([ReservationId], [OccurredAtUtc]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260718011133_AddFrontOfficeReservation', N'8.0.7');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

CREATE TABLE [fo].[LoaiPhongKhachSan] (
    [HotelRoomTypeId] uniqueidentifier NOT NULL,
    [PropertyId] uniqueidentifier NOT NULL,
    [Code] nvarchar(40) NOT NULL,
    [Name] nvarchar(160) NOT NULL,
    [Description] nvarchar(1000) NULL,
    [StandardAdultCapacity] int NOT NULL,
    [StandardChildCapacity] int NOT NULL,
    [MaximumAdultCapacity] int NOT NULL,
    [MaximumChildCapacity] int NOT NULL,
    [DefaultOccupancy] int NOT NULL,
    [DisplayOrder] int NOT NULL,
    [IsActive] bit NOT NULL,
    [CreatedAtUtc] datetime2 NOT NULL,
    [CreatedBy] uniqueidentifier NULL,
    [UpdatedAtUtc] datetime2 NOT NULL,
    [UpdatedBy] uniqueidentifier NULL,
    [RowVersion] rowversion NULL,
    CONSTRAINT [PK_fo_LoaiPhongKhachSan] PRIMARY KEY ([HotelRoomTypeId])
);
GO

CREATE TABLE [fo].[PhongKhachSan] (
    [HotelRoomId] uniqueidentifier NOT NULL,
    [PropertyId] uniqueidentifier NOT NULL,
    [HotelRoomTypeId] uniqueidentifier NOT NULL,
    [RoomNumber] nvarchar(40) NOT NULL,
    [DisplayName] nvarchar(160) NULL,
    [Floor] nvarchar(40) NULL,
    [Building] nvarchar(80) NULL,
    [Zone] nvarchar(80) NULL,
    [Description] nvarchar(1000) NULL,
    [OperationalStatus] nvarchar(30) NOT NULL,
    [IsActive] bit NOT NULL,
    [CreatedAtUtc] datetime2 NOT NULL,
    [CreatedBy] uniqueidentifier NULL,
    [UpdatedAtUtc] datetime2 NOT NULL,
    [UpdatedBy] uniqueidentifier NULL,
    [RowVersion] rowversion NULL,
    CONSTRAINT [PK_fo_PhongKhachSan] PRIMARY KEY ([HotelRoomId]),
    CONSTRAINT [FK_PhongKhachSan_LoaiPhongKhachSan_HotelRoomTypeId] FOREIGN KEY ([HotelRoomTypeId]) REFERENCES [fo].[LoaiPhongKhachSan] ([HotelRoomTypeId]) ON DELETE NO ACTION
);
GO

CREATE INDEX [IX_fo_LoaiPhongKhachSan_CoSo_HoatDong] ON [fo].[LoaiPhongKhachSan] ([PropertyId], [IsActive]);
GO

CREATE INDEX [IX_fo_LoaiPhongKhachSan_CoSo_ThuTu] ON [fo].[LoaiPhongKhachSan] ([PropertyId], [DisplayOrder]);
GO

CREATE UNIQUE INDEX [UQ_fo_LoaiPhongKhachSan_CoSo_Ma] ON [fo].[LoaiPhongKhachSan] ([PropertyId], [Code]);
GO

CREATE INDEX [IX_fo_PhongKhachSan_CoSo_HoatDong] ON [fo].[PhongKhachSan] ([PropertyId], [IsActive]);
GO

CREATE INDEX [IX_fo_PhongKhachSan_CoSo_LoaiPhong] ON [fo].[PhongKhachSan] ([PropertyId], [HotelRoomTypeId]);
GO

CREATE INDEX [IX_fo_PhongKhachSan_CoSo_TrangThai] ON [fo].[PhongKhachSan] ([PropertyId], [OperationalStatus]);
GO

CREATE INDEX [IX_PhongKhachSan_HotelRoomTypeId] ON [fo].[PhongKhachSan] ([HotelRoomTypeId]);
GO

CREATE UNIQUE INDEX [UQ_fo_PhongKhachSan_CoSo_SoPhong] ON [fo].[PhongKhachSan] ([PropertyId], [RoomNumber]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260718014449_AddFrontOfficeHotelRoomMasterData', N'8.0.7');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

CREATE SEQUENCE [fo].[TrinhTuSoLuuTru] START WITH 1 INCREMENT BY 1 NO MINVALUE NO MAXVALUE NO CYCLE;
GO

CREATE TABLE [fo].[LuuTruKhachSan] (
    [HotelStayId] uniqueidentifier NOT NULL,
    [PropertyId] uniqueidentifier NOT NULL,
    [StayNumber] nvarchar(40) NOT NULL,
    [ReservationId] uniqueidentifier NULL,
    [ReservationNumberSnapshot] nvarchar(40) NULL,
    [GuestId] uniqueidentifier NULL,
    [GuestNameSnapshot] nvarchar(200) NOT NULL,
    [GuestPhoneSnapshot] nvarchar(50) NULL,
    [RoomId] uniqueidentifier NOT NULL,
    [RoomNumberSnapshot] nvarchar(40) NOT NULL,
    [RoomTypeId] uniqueidentifier NOT NULL,
    [RoomTypeCodeSnapshot] nvarchar(40) NOT NULL,
    [ScheduledArrivalDate] date NOT NULL,
    [ScheduledDepartureDate] date NOT NULL,
    [ActualCheckInAtUtc] datetime2 NULL,
    [ActualCheckOutAtUtc] datetime2 NULL,
    [Adults] int NOT NULL,
    [Children] int NOT NULL,
    [Status] nvarchar(30) NOT NULL,
    [Source] nvarchar(80) NOT NULL,
    [InternalNotes] nvarchar(1000) NULL,
    [CreatedAtUtc] datetime2 NOT NULL,
    [CreatedBy] uniqueidentifier NULL,
    [UpdatedAtUtc] datetime2 NOT NULL,
    [UpdatedBy] uniqueidentifier NULL,
    [RowVersion] rowversion NULL,
    CONSTRAINT [PK_fo_LuuTruKhachSan] PRIMARY KEY ([HotelStayId]),
    CONSTRAINT [FK_LuuTruKhachSan_DatPhong_ReservationId] FOREIGN KEY ([ReservationId]) REFERENCES [fo].[DatPhong] ([ReservationId]) ON DELETE NO ACTION,
    CONSTRAINT [FK_LuuTruKhachSan_LoaiPhongKhachSan_RoomTypeId] FOREIGN KEY ([RoomTypeId]) REFERENCES [fo].[LoaiPhongKhachSan] ([HotelRoomTypeId]) ON DELETE NO ACTION,
    CONSTRAINT [FK_LuuTruKhachSan_PhongKhachSan_RoomId] FOREIGN KEY ([RoomId]) REFERENCES [fo].[PhongKhachSan] ([HotelRoomId]) ON DELETE NO ACTION
);
GO

CREATE TABLE [fo].[LichSuLuuTruKhachSan] (
    [HistoryId] uniqueidentifier NOT NULL,
    [HotelStayId] uniqueidentifier NOT NULL,
    [PropertyId] uniqueidentifier NOT NULL,
    [Action] nvarchar(80) NOT NULL,
    [FromStatus] nvarchar(30) NULL,
    [ToStatus] nvarchar(30) NOT NULL,
    [ActorId] uniqueidentifier NULL,
    [OccurredAtUtc] datetime2 NOT NULL,
    [CorrelationId] uniqueidentifier NOT NULL,
    [SafeMetadata] nvarchar(300) NULL,
    CONSTRAINT [PK_fo_LichSuLuuTruKhachSan] PRIMARY KEY ([HistoryId]),
    CONSTRAINT [FK_LichSuLuuTruKhachSan_LuuTruKhachSan_HotelStayId] FOREIGN KEY ([HotelStayId]) REFERENCES [fo].[LuuTruKhachSan] ([HotelStayId]) ON DELETE NO ACTION
);
GO

CREATE INDEX [IX_fo_LichSuLuuTruKhachSan_LuuTru_Luc] ON [fo].[LichSuLuuTruKhachSan] ([HotelStayId], [OccurredAtUtc]);
GO

CREATE INDEX [IX_fo_LuuTruKhachSan_CapNhat] ON [fo].[LuuTruKhachSan] ([UpdatedAtUtc]);
GO

CREATE INDEX [IX_fo_LuuTruKhachSan_CoSo_NgayDen] ON [fo].[LuuTruKhachSan] ([PropertyId], [ScheduledArrivalDate]);
GO

CREATE INDEX [IX_fo_LuuTruKhachSan_CoSo_NgayDi] ON [fo].[LuuTruKhachSan] ([PropertyId], [ScheduledDepartureDate]);
GO

CREATE INDEX [IX_fo_LuuTruKhachSan_CoSo_Phong_TrangThai] ON [fo].[LuuTruKhachSan] ([PropertyId], [RoomId], [Status]);
GO

CREATE INDEX [IX_fo_LuuTruKhachSan_CoSo_TrangThai] ON [fo].[LuuTruKhachSan] ([PropertyId], [Status]);
GO

CREATE INDEX [IX_LuuTruKhachSan_RoomId] ON [fo].[LuuTruKhachSan] ([RoomId]);
GO

CREATE INDEX [IX_LuuTruKhachSan_RoomTypeId] ON [fo].[LuuTruKhachSan] ([RoomTypeId]);
GO

CREATE UNIQUE INDEX [UQ_fo_LuuTruKhachSan_DatPhong_HoatDong] ON [fo].[LuuTruKhachSan] ([ReservationId]) WHERE [ReservationId] IS NOT NULL AND [Status] IN ('PendingArrival', 'InHouse');
GO

CREATE UNIQUE INDEX [UQ_fo_LuuTruKhachSan_SoLuuTru] ON [fo].[LuuTruKhachSan] ([StayNumber]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260718092912_AddFrontOfficeHotelStayFoundation', N'8.0.7');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

DROP INDEX [IX_LuuTruKhachSan_RoomId] ON [fo].[LuuTruKhachSan];
GO

CREATE UNIQUE INDEX [UQ_fo_LuuTruKhachSan_Phong_InHouse] ON [fo].[LuuTruKhachSan] ([RoomId]) WHERE [Status] = 'InHouse';
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260718095823_AddFrontOfficeHotelStayCheckIn', N'8.0.7');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

CREATE SEQUENCE [fo].[TrinhTuSoFolio] START WITH 1 INCREMENT BY 1 NO MINVALUE NO MAXVALUE NO CYCLE;
GO

CREATE TABLE [fo].[SoFolio] (
    [HotelFolioId] uniqueidentifier NOT NULL,
    [PropertyId] uniqueidentifier NOT NULL,
    [HotelStayId] uniqueidentifier NOT NULL,
    [FolioNumber] nvarchar(40) NOT NULL,
    [Status] nvarchar(20) NOT NULL,
    [CreatedAtUtc] datetime2 NOT NULL,
    [CreatedBy] uniqueidentifier NULL,
    [ClosedAtUtc] datetime2 NULL,
    [ClosedBy] uniqueidentifier NULL,
    [RowVersion] rowversion NULL,
    CONSTRAINT [PK_fo_SoFolio] PRIMARY KEY ([HotelFolioId]),
    CONSTRAINT [FK_SoFolio_LuuTruKhachSan_HotelStayId] FOREIGN KEY ([HotelStayId]) REFERENCES [fo].[LuuTruKhachSan] ([HotelStayId]) ON DELETE NO ACTION
);
GO

CREATE TABLE [fo].[ButToanFolio] (
    [HotelFolioEntryId] uniqueidentifier NOT NULL,
    [HotelFolioId] uniqueidentifier NOT NULL,
    [PropertyId] uniqueidentifier NOT NULL,
    [SequenceNumber] int NOT NULL,
    [EntryType] nvarchar(30) NOT NULL,
    [Amount] decimal(18,2) NOT NULL,
    [Description] nvarchar(500) NOT NULL,
    [ReferenceType] nvarchar(80) NULL,
    [ReferenceId] uniqueidentifier NULL,
    [ReversesEntryId] uniqueidentifier NULL,
    [ReversedByEntryId] uniqueidentifier NULL,
    [CreatedAtUtc] datetime2 NOT NULL,
    [CreatedBy] uniqueidentifier NULL,
    CONSTRAINT [PK_fo_ButToanFolio] PRIMARY KEY ([HotelFolioEntryId]),
    CONSTRAINT [FK_ButToanFolio_SoFolio_HotelFolioId] FOREIGN KEY ([HotelFolioId]) REFERENCES [fo].[SoFolio] ([HotelFolioId]) ON DELETE NO ACTION
);
GO

CREATE INDEX [IX_fo_ButToanFolio_CoSo_Folio] ON [fo].[ButToanFolio] ([PropertyId], [HotelFolioId]);
GO

CREATE INDEX [IX_fo_ButToanFolio_Folio_ThoiGian] ON [fo].[ButToanFolio] ([HotelFolioId], [CreatedAtUtc]);
GO

CREATE UNIQUE INDEX [UQ_fo_ButToanFolio_Folio_Sequence] ON [fo].[ButToanFolio] ([HotelFolioId], [SequenceNumber]);
GO

CREATE INDEX [IX_fo_SoFolio_CoSo_TrangThai] ON [fo].[SoFolio] ([PropertyId], [Status]);
GO

CREATE INDEX [IX_SoFolio_HotelStayId] ON [fo].[SoFolio] ([HotelStayId]);
GO

CREATE UNIQUE INDEX [UQ_fo_SoFolio_CoSo_LuuTru] ON [fo].[SoFolio] ([PropertyId], [HotelStayId]);
GO

CREATE UNIQUE INDEX [UQ_fo_SoFolio_SoFolio] ON [fo].[SoFolio] ([FolioNumber]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260719053754_AddFrontOfficeHotelFolio', N'8.0.7');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

CREATE SEQUENCE [fo].[TrinhTuSoCa] START WITH 1 INCREMENT BY 1 NO MINVALUE NO MAXVALUE NO CYCLE;
GO

CREATE SEQUENCE [fo].[TrinhTuSoDanhThuc] START WITH 1 INCREMENT BY 1 NO MINVALUE NO MAXVALUE NO CYCLE;
GO

CREATE SEQUENCE [fo].[TrinhTuSoDoan] START WITH 1 INCREMENT BY 1 NO MINVALUE NO MAXVALUE NO CYCLE;
GO

CREATE SEQUENCE [fo].[TrinhTuSoDonDep] START WITH 1 INCREMENT BY 1 NO MINVALUE NO MAXVALUE NO CYCLE;
GO

CREATE SEQUENCE [fo].[TrinhTuSoGia] START WITH 1 INCREMENT BY 1 NO MINVALUE NO MAXVALUE NO CYCLE;
GO

CREATE SEQUENCE [fo].[TrinhTuSoHoaDon] START WITH 1 INCREMENT BY 1 NO MINVALUE NO MAXVALUE NO CYCLE;
GO

CREATE SEQUENCE [fo].[TrinhTuSoKhach] START WITH 1 INCREMENT BY 1 NO MINVALUE NO MAXVALUE NO CYCLE;
GO

CREATE SEQUENCE [fo].[TrinhTuSoKiemDem] START WITH 1 INCREMENT BY 1 NO MINVALUE NO MAXVALUE NO CYCLE;
GO

CREATE SEQUENCE [fo].[TrinhTuSoThanhToan] START WITH 1 INCREMENT BY 1 NO MINVALUE NO MAXVALUE NO CYCLE;
GO

CREATE SEQUENCE [fo].[TrinhTuSoThatLac] START WITH 1 INCREMENT BY 1 NO MINVALUE NO MAXVALUE NO CYCLE;
GO

CREATE SEQUENCE [fo].[TrinhTuSoYeuCau] START WITH 1 INCREMENT BY 1 NO MINVALUE NO MAXVALUE NO CYCLE;
GO

ALTER TABLE [fo].[DatPhong] ADD [CheckedInAtUtc] datetime2 NULL;
GO

ALTER TABLE [fo].[DatPhong] ADD [CompanyName] nvarchar(200) NULL;
GO

ALTER TABLE [fo].[DatPhong] ADD [CompletedAtUtc] datetime2 NULL;
GO

ALTER TABLE [fo].[DatPhong] ADD [GuaranteedAtUtc] datetime2 NULL;
GO

ALTER TABLE [fo].[DatPhong] ADD [IsVip] bit NOT NULL DEFAULT CAST(0 AS bit);
GO

ALTER TABLE [fo].[DatPhong] ADD [MarketSegment] nvarchar(100) NULL;
GO

ALTER TABLE [fo].[DatPhong] ADD [TravelAgentName] nvarchar(200) NULL;
GO

CREATE TABLE [fo].[CaLamViec] (
    [CashierShiftId] uniqueidentifier NOT NULL,
    [PropertyId] uniqueidentifier NOT NULL,
    [ShiftNumber] nvarchar(40) NOT NULL,
    [CashierId] uniqueidentifier NOT NULL,
    [Status] nvarchar(20) NOT NULL,
    [OpeningCash] decimal(18,2) NOT NULL,
    [ClosingCash] decimal(18,2) NOT NULL,
    [ActualCash] decimal(18,2) NULL,
    [BusinessDate] date NOT NULL,
    [OpenedAtUtc] datetime2 NOT NULL,
    [ClosedAtUtc] datetime2 NULL,
    [ClosedBy] uniqueidentifier NULL,
    [RowVersion] rowversion NULL,
    CONSTRAINT [PK_fo_CaLamViec] PRIMARY KEY ([CashierShiftId])
);
GO

CREATE TABLE [fo].[DanhThuc] (
    [WakeUpCallId] uniqueidentifier NOT NULL,
    [PropertyId] uniqueidentifier NOT NULL,
    [CallNumber] nvarchar(40) NOT NULL,
    [HotelStayId] uniqueidentifier NULL,
    [HotelRoomId] uniqueidentifier NULL,
    [RoomNumber] nvarchar(40) NULL,
    [GuestName] nvarchar(200) NULL,
    [WakeTime] datetime2 NOT NULL,
    [ActualCallTime] datetime2 NULL,
    [Status] nvarchar(20) NOT NULL,
    [Result] nvarchar(20) NULL,
    [IsRecurring] bit NOT NULL,
    [IsDaily] bit NOT NULL,
    [RetryCount] int NOT NULL,
    [Notes] nvarchar(500) NULL,
    [AssignedStaffId] uniqueidentifier NULL,
    [CreatedAtUtc] datetime2 NOT NULL,
    [CreatedBy] uniqueidentifier NULL,
    [RowVersion] rowversion NULL,
    CONSTRAINT [PK_fo_DanhThuc] PRIMARY KEY ([WakeUpCallId])
);
GO

CREATE TABLE [fo].[DatPhongDoan] (
    [GroupReservationId] uniqueidentifier NOT NULL,
    [PropertyId] uniqueidentifier NOT NULL,
    [GroupCode] nvarchar(40) NOT NULL,
    [GroupName] nvarchar(200) NOT NULL,
    [CompanyName] nvarchar(200) NULL,
    [AgencyName] nvarchar(200) NULL,
    [ContactPerson] nvarchar(200) NULL,
    [ContactPhone] nvarchar(50) NULL,
    [ContactEmail] nvarchar(200) NULL,
    [ArrivalDate] date NOT NULL,
    [DepartureDate] date NOT NULL,
    [ExpectedRooms] int NOT NULL,
    [ConfirmedRooms] int NOT NULL,
    [TotalAdults] int NOT NULL,
    [TotalChildren] int NOT NULL,
    [Status] nvarchar(20) NOT NULL,
    [Source] nvarchar(80) NULL,
    [SalesManager] nvarchar(200) NULL,
    [MarketSegment] nvarchar(100) NULL,
    [IsVip] bit NOT NULL,
    [Routing] nvarchar(30) NOT NULL,
    [SpecialRequests] nvarchar(1000) NULL,
    [Notes] nvarchar(1000) NULL,
    [TotalDeposit] decimal(18,2) NOT NULL,
    [TotalRevenue] decimal(18,2) NOT NULL,
    [OutstandingBalance] decimal(18,2) NOT NULL,
    [CreatedAtUtc] datetime2 NOT NULL,
    [CreatedBy] uniqueidentifier NULL,
    [UpdatedAtUtc] datetime2 NOT NULL,
    [UpdatedBy] uniqueidentifier NULL,
    [RowVersion] rowversion NULL,
    CONSTRAINT [PK_fo_DatPhongDoan] PRIMARY KEY ([GroupReservationId])
);
GO

CREATE TABLE [fo].[DonDep] (
    [HousekeepingTaskId] uniqueidentifier NOT NULL,
    [PropertyId] uniqueidentifier NOT NULL,
    [HotelRoomId] uniqueidentifier NOT NULL,
    [TaskNumber] nvarchar(40) NOT NULL,
    [TaskType] nvarchar(30) NOT NULL,
    [Status] nvarchar(20) NOT NULL,
    [Priority] nvarchar(20) NOT NULL,
    [AssignedHousekeeperId] uniqueidentifier NULL,
    [InspectorNotes] nvarchar(1000) NULL,
    [InspectionResult] nvarchar(20) NULL,
    [CreatedAtUtc] datetime2 NOT NULL,
    [CreatedBy] uniqueidentifier NULL,
    [StartedAtUtc] datetime2 NULL,
    [CompletedAtUtc] datetime2 NULL,
    [InspectedAtUtc] datetime2 NULL,
    [CancelledAtUtc] datetime2 NULL,
    [RowVersion] rowversion NULL,
    CONSTRAINT [PK_fo_DonDep] PRIMARY KEY ([HousekeepingTaskId])
);
GO

CREATE TABLE [fo].[HoaDon] (
    [HotelInvoiceId] uniqueidentifier NOT NULL,
    [PropertyId] uniqueidentifier NOT NULL,
    [HotelFolioId] uniqueidentifier NOT NULL,
    [HotelStayId] uniqueidentifier NOT NULL,
    [InvoiceNumber] nvarchar(40) NOT NULL,
    [Status] nvarchar(20) NOT NULL,
    [Subtotal] decimal(18,2) NOT NULL,
    [TaxAmount] decimal(18,2) NOT NULL,
    [TotalAmount] decimal(18,2) NOT NULL,
    [PaidAmount] decimal(18,2) NOT NULL,
    [OutstandingAmount] decimal(18,2) NOT NULL,
    [GuestName] nvarchar(200) NULL,
    [RoomNumber] nvarchar(40) NULL,
    [StayDates] nvarchar(200) NULL,
    [InvoiceDataJson] nvarchar(max) NOT NULL,
    [CreatedAtUtc] datetime2 NOT NULL,
    [CreatedBy] uniqueidentifier NULL,
    [PrintedAtUtc] datetime2 NULL,
    [RowVersion] rowversion NULL,
    CONSTRAINT [PK_fo_HoaDon] PRIMARY KEY ([HotelInvoiceId])
);
GO

CREATE TABLE [fo].[HoSoKhach] (
    [GuestProfileId] uniqueidentifier NOT NULL,
    [PropertyId] uniqueidentifier NOT NULL,
    [GuestNumber] nvarchar(40) NOT NULL,
    [FullName] nvarchar(200) NOT NULL,
    [Phone] nvarchar(50) NULL,
    [Email] nvarchar(200) NULL,
    [Address] nvarchar(500) NULL,
    [Nationality] nvarchar(80) NULL,
    [Country] nvarchar(80) NULL,
    [Gender] nvarchar(20) NULL,
    [DateOfBirth] date NULL,
    [EmergencyContact] nvarchar(200) NULL,
    [EmergencyPhone] nvarchar(50) NULL,
    [VipLevel] nvarchar(20) NOT NULL,
    [TotalStays] int NOT NULL,
    [TotalRevenue] decimal(18,2) NOT NULL,
    [LastStayDate] date NULL,
    [AverageSpend] decimal(18,2) NOT NULL,
    [IsBlacklisted] bit NOT NULL,
    [BlacklistReason] nvarchar(500) NULL,
    [BlacklistedBy] uniqueidentifier NULL,
    [BlacklistedAtUtc] datetime2 NULL,
    [Source] nvarchar(20) NOT NULL,
    [CompanyName] nvarchar(200) NULL,
    [Notes] nvarchar(1000) NULL,
    [CreatedAtUtc] datetime2 NOT NULL,
    [CreatedBy] uniqueidentifier NULL,
    [UpdatedAtUtc] datetime2 NOT NULL,
    [UpdatedBy] uniqueidentifier NULL,
    [RowVersion] rowversion NULL,
    CONSTRAINT [PK_fo_HoSoKhach] PRIMARY KEY ([GuestProfileId])
);
GO

CREATE TABLE [fo].[KeHoachGia] (
    [RatePlanId] uniqueidentifier NOT NULL,
    [PropertyId] uniqueidentifier NOT NULL,
    [PlanCode] nvarchar(40) NOT NULL,
    [PlanName] nvarchar(200) NOT NULL,
    [RoomTypeId] uniqueidentifier NULL,
    [BaseRate] decimal(18,2) NOT NULL,
    [Status] nvarchar(20) NOT NULL,
    [Hierarchy] int NOT NULL,
    [EffectiveFrom] datetime2 NOT NULL,
    [EffectiveTo] datetime2 NOT NULL,
    [AllowYieldManagement] bit NOT NULL,
    [YieldMinRate] decimal(18,2) NOT NULL,
    [YieldMaxRate] decimal(18,2) NOT NULL,
    [YieldThresholdOccupancy] int NOT NULL,
    [YieldAdjustmentPct] decimal(18,2) NOT NULL,
    [CreatedAtUtc] datetime2 NOT NULL,
    [CreatedBy] uniqueidentifier NULL,
    [RowVersion] rowversion NULL,
    CONSTRAINT [PK_fo_KeHoachGia] PRIMARY KEY ([RatePlanId])
);
GO

CREATE TABLE [fo].[KiemDem] (
    [NightAuditId] uniqueidentifier NOT NULL,
    [PropertyId] uniqueidentifier NOT NULL,
    [AuditNumber] nvarchar(40) NOT NULL,
    [BusinessDate] date NOT NULL,
    [Status] nvarchar(20) NOT NULL,
    [CurrentStep] int NOT NULL,
    [ErrorMessage] nvarchar(1000) NULL,
    [StartedAtUtc] datetime2 NOT NULL,
    [CompletedAtUtc] datetime2 NULL,
    [StartedBy] uniqueidentifier NULL,
    [RowVersion] rowversion NULL,
    CONSTRAINT [PK_fo_KiemDem] PRIMARY KEY ([NightAuditId])
);
GO

CREATE TABLE [LostFoundItems] (
    [LostFoundItemId] uniqueidentifier NOT NULL,
    [PropertyId] uniqueidentifier NOT NULL,
    [ItemNumber] nvarchar(max) NOT NULL,
    [ItemName] nvarchar(max) NOT NULL,
    [Category] int NOT NULL,
    [Description] nvarchar(max) NULL,
    [Color] nvarchar(max) NULL,
    [Brand] nvarchar(max) NULL,
    [SerialNumber] nvarchar(max) NULL,
    [FoundLocation] nvarchar(max) NULL,
    [FoundDateTime] datetime2 NOT NULL,
    [FoundByStaff] nvarchar(max) NULL,
    [StoredLocation] nvarchar(max) NULL,
    [Status] int NOT NULL,
    [OwnerGuestId] uniqueidentifier NULL,
    [OwnerGuestName] nvarchar(max) NULL,
    [ReservationId] uniqueidentifier NULL,
    [HotelStayId] uniqueidentifier NULL,
    [ClaimedDate] datetime2 NULL,
    [ReleasedBy] nvarchar(max) NULL,
    [ReleasedTo] nvarchar(max) NULL,
    [DisposalDate] datetime2 NULL,
    [PhotoPath] nvarchar(max) NULL,
    [Notes] nvarchar(max) NULL,
    [CreatedAtUtc] datetime2 NOT NULL,
    [CreatedBy] uniqueidentifier NULL,
    [RowVersion] varbinary(max) NULL,
    CONSTRAINT [PK_LostFoundItems] PRIMARY KEY ([LostFoundItemId])
);
GO

CREATE TABLE [fo].[NgayKinhDoanh] (
    [BusinessDateId] uniqueidentifier NOT NULL,
    [PropertyId] uniqueidentifier NOT NULL,
    [BusinessDate] date NOT NULL,
    [Status] nvarchar(20) NOT NULL,
    [OpenedAtUtc] datetime2 NOT NULL,
    [OpenedBy] uniqueidentifier NULL,
    [ClosedAtUtc] datetime2 NULL,
    [ClosedBy] uniqueidentifier NULL,
    [NightAuditId] uniqueidentifier NULL,
    [RowVersion] rowversion NULL,
    CONSTRAINT [PK_fo_NgayKinhDoanh] PRIMARY KEY ([BusinessDateId])
);
GO

CREATE TABLE [fo].[ThanhToan] (
    [HotelPaymentId] uniqueidentifier NOT NULL,
    [PropertyId] uniqueidentifier NOT NULL,
    [HotelFolioId] uniqueidentifier NOT NULL,
    [FolioEntryId] uniqueidentifier NULL,
    [RefundEntryId] uniqueidentifier NULL,
    [PaymentNumber] nvarchar(40) NOT NULL,
    [PaymentMethod] nvarchar(30) NOT NULL,
    [Amount] decimal(18,2) NOT NULL,
    [Status] nvarchar(20) NOT NULL,
    [ExternalReference] nvarchar(200) NULL,
    [Notes] nvarchar(500) NULL,
    [CreatedAtUtc] datetime2 NOT NULL,
    [CreatedBy] uniqueidentifier NULL,
    [VoidedAtUtc] datetime2 NULL,
    [VoidedBy] uniqueidentifier NULL,
    [VoidReason] nvarchar(500) NULL,
    [RefundedAtUtc] datetime2 NULL,
    [RefundedBy] uniqueidentifier NULL,
    [RefundReason] nvarchar(500) NULL,
    [RowVersion] rowversion NULL,
    CONSTRAINT [PK_fo_ThanhToan] PRIMARY KEY ([HotelPaymentId])
);
GO

CREATE TABLE [fo].[TrangThaiDonDep] (
    [HotelRoomId] uniqueidentifier NOT NULL,
    [PropertyId] uniqueidentifier NOT NULL,
    [Status] nvarchar(20) NOT NULL,
    [StatusChangedAtUtc] datetime2 NOT NULL,
    [RowVersion] rowversion NULL,
    CONSTRAINT [PK_fo_TrangThaiDonDep] PRIMARY KEY ([HotelRoomId])
);
GO

CREATE TABLE [fo].[YeuCauDichVu] (
    [ServiceRequestId] uniqueidentifier NOT NULL,
    [PropertyId] uniqueidentifier NOT NULL,
    [RequestNumber] nvarchar(40) NOT NULL,
    [HotelStayId] uniqueidentifier NULL,
    [HotelRoomId] uniqueidentifier NULL,
    [RoomNumber] nvarchar(40) NULL,
    [GuestName] nvarchar(200) NULL,
    [ServiceType] nvarchar(200) NOT NULL,
    [Category] nvarchar(20) NOT NULL,
    [Priority] nvarchar(20) NOT NULL,
    [Status] nvarchar(20) NOT NULL,
    [Description] nvarchar(1000) NULL,
    [AssignedDepartment] nvarchar(200) NULL,
    [AssignedStaffId] uniqueidentifier NULL,
    [EstimatedCost] decimal(18,2) NULL,
    [ActualCost] decimal(18,2) NULL,
    [ChargeType] nvarchar(20) NOT NULL,
    [FolioEntryId] uniqueidentifier NULL,
    [RequestedAtUtc] datetime2 NOT NULL,
    [CompletedAtUtc] datetime2 NULL,
    [RequestedBy] uniqueidentifier NULL,
    [RowVersion] rowversion NULL,
    CONSTRAINT [PK_fo_YeuCauDichVu] PRIMARY KEY ([ServiceRequestId])
);
GO

CREATE TABLE [fo].[BienDongTienMat] (
    [CashMovementId] uniqueidentifier NOT NULL,
    [CashierShiftId] uniqueidentifier NOT NULL,
    [PropertyId] uniqueidentifier NOT NULL,
    [Type] nvarchar(20) NOT NULL,
    [Amount] decimal(18,2) NOT NULL,
    [Description] nvarchar(500) NOT NULL,
    [PaymentId] uniqueidentifier NULL,
    [CreatedAtUtc] datetime2 NOT NULL,
    [CreatedBy] uniqueidentifier NULL,
    CONSTRAINT [PK_fo_BienDongTienMat] PRIMARY KEY ([CashMovementId]),
    CONSTRAINT [FK_BienDongTienMat_CaLamViec_CashierShiftId] FOREIGN KEY ([CashierShiftId]) REFERENCES [fo].[CaLamViec] ([CashierShiftId]) ON DELETE NO ACTION
);
GO

CREATE TABLE [fo].[LichSuDanhThuc] (
    [WakeUpCallHistoryId] uniqueidentifier NOT NULL,
    [WakeUpCallId] uniqueidentifier NOT NULL,
    [PropertyId] uniqueidentifier NOT NULL,
    [Action] nvarchar(200) NOT NULL,
    [OccurredAtUtc] datetime2 NOT NULL,
    CONSTRAINT [PK_fo_LichSuDanhThuc] PRIMARY KEY ([WakeUpCallHistoryId]),
    CONSTRAINT [FK_LichSuDanhThuc_DanhThuc_WakeUpCallId] FOREIGN KEY ([WakeUpCallId]) REFERENCES [fo].[DanhThuc] ([WakeUpCallId]) ON DELETE NO ACTION
);
GO

CREATE TABLE [fo].[PhanPhongDoan] (
    [GroupRoomAssignmentId] uniqueidentifier NOT NULL,
    [GroupReservationId] uniqueidentifier NOT NULL,
    [PropertyId] uniqueidentifier NOT NULL,
    [HotelRoomId] uniqueidentifier NOT NULL,
    [RoomNumber] nvarchar(40) NOT NULL,
    [RoomType] nvarchar(40) NOT NULL,
    [GuestId] uniqueidentifier NULL,
    [GuestName] nvarchar(200) NULL,
    [IsVip] bit NOT NULL,
    [Adults] int NOT NULL,
    [Children] int NOT NULL,
    [IsCheckedIn] bit NOT NULL,
    [IsCheckedOut] bit NOT NULL,
    [ReservationId] uniqueidentifier NULL,
    [CreatedAtUtc] datetime2 NOT NULL,
    CONSTRAINT [PK_fo_PhanPhongDoan] PRIMARY KEY ([GroupRoomAssignmentId]),
    CONSTRAINT [FK_PhanPhongDoan_DatPhongDoan_GroupReservationId] FOREIGN KEY ([GroupReservationId]) REFERENCES [fo].[DatPhongDoan] ([GroupReservationId]) ON DELETE NO ACTION
);
GO

CREATE TABLE [fo].[GiayToKhach] (
    [GuestDocumentId] uniqueidentifier NOT NULL,
    [GuestProfileId] uniqueidentifier NOT NULL,
    [PropertyId] uniqueidentifier NOT NULL,
    [DocumentType] nvarchar(20) NOT NULL,
    [DocumentNumber] nvarchar(100) NOT NULL,
    [IssuingCountry] nvarchar(80) NULL,
    [ExpiryDate] date NULL,
    [CreatedAtUtc] datetime2 NOT NULL,
    CONSTRAINT [PK_fo_GiayToKhach] PRIMARY KEY ([GuestDocumentId]),
    CONSTRAINT [FK_GiayToKhach_HoSoKhach_GuestProfileId] FOREIGN KEY ([GuestProfileId]) REFERENCES [fo].[HoSoKhach] ([GuestProfileId]) ON DELETE NO ACTION
);
GO

CREATE TABLE [fo].[SoThichKhach] (
    [GuestPreferenceId] uniqueidentifier NOT NULL,
    [GuestProfileId] uniqueidentifier NOT NULL,
    [PropertyId] uniqueidentifier NOT NULL,
    [Key] nvarchar(100) NOT NULL,
    [Value] nvarchar(500) NOT NULL,
    CONSTRAINT [PK_fo_SoThichKhach] PRIMARY KEY ([GuestPreferenceId]),
    CONSTRAINT [FK_SoThichKhach_HoSoKhach_GuestProfileId] FOREIGN KEY ([GuestProfileId]) REFERENCES [fo].[HoSoKhach] ([GuestProfileId]) ON DELETE NO ACTION
);
GO

CREATE TABLE [fo].[LichGia] (
    [RateCalendarEntryId] uniqueidentifier NOT NULL,
    [RatePlanId] uniqueidentifier NOT NULL,
    [PropertyId] uniqueidentifier NOT NULL,
    [Date] date NOT NULL,
    [Rate] decimal(18,2) NOT NULL,
    [UpdatedAtUtc] datetime2 NOT NULL,
    CONSTRAINT [PK_fo_LichGia] PRIMARY KEY ([RateCalendarEntryId]),
    CONSTRAINT [FK_LichGia_KeHoachGia_RatePlanId] FOREIGN KEY ([RatePlanId]) REFERENCES [fo].[KeHoachGia] ([RatePlanId]) ON DELETE NO ACTION
);
GO

CREATE TABLE [fo].[MuaGia] (
    [RateSeasonId] uniqueidentifier NOT NULL,
    [RatePlanId] uniqueidentifier NOT NULL,
    [PropertyId] uniqueidentifier NOT NULL,
    [Type] nvarchar(20) NOT NULL,
    [FromDate] datetime2 NOT NULL,
    [ToDate] datetime2 NOT NULL,
    [Multiplier] decimal(9,4) NOT NULL,
    [Description] nvarchar(500) NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_fo_MuaGia] PRIMARY KEY ([RateSeasonId]),
    CONSTRAINT [FK_MuaGia_KeHoachGia_RatePlanId] FOREIGN KEY ([RatePlanId]) REFERENCES [fo].[KeHoachGia] ([RatePlanId]) ON DELETE NO ACTION
);
GO

CREATE TABLE [fo].[QuyTacGia] (
    [RateRuleId] uniqueidentifier NOT NULL,
    [RatePlanId] uniqueidentifier NOT NULL,
    [PropertyId] uniqueidentifier NOT NULL,
    [Type] nvarchar(30) NOT NULL,
    [Value] decimal(9,4) NOT NULL,
    [Priority] int NOT NULL,
    [Exclusive] bit NOT NULL,
    [Combinable] bit NOT NULL,
    [IsActive] bit NOT NULL,
    [Description] nvarchar(500) NULL,
    [CreatedAtUtc] datetime2 NOT NULL,
    [ExpiryDate] datetime2 NULL,
    CONSTRAINT [PK_fo_QuyTacGia] PRIMARY KEY ([RateRuleId]),
    CONSTRAINT [FK_QuyTacGia_KeHoachGia_RatePlanId] FOREIGN KEY ([RatePlanId]) REFERENCES [fo].[KeHoachGia] ([RatePlanId]) ON DELETE NO ACTION
);
GO

CREATE TABLE [fo].[BuocKiemDem] (
    [NightAuditStepExecutionId] uniqueidentifier NOT NULL,
    [NightAuditId] uniqueidentifier NOT NULL,
    [PropertyId] uniqueidentifier NOT NULL,
    [Step] int NOT NULL,
    [Status] nvarchar(20) NOT NULL,
    [Detail] nvarchar(1000) NULL,
    [CompletedAtUtc] datetime2 NULL,
    CONSTRAINT [PK_fo_BuocKiemDem] PRIMARY KEY ([NightAuditStepExecutionId]),
    CONSTRAINT [FK_BuocKiemDem_KiemDem_NightAuditId] FOREIGN KEY ([NightAuditId]) REFERENCES [fo].[KiemDem] ([NightAuditId]) ON DELETE NO ACTION
);
GO

CREATE TABLE [fo].[DoanhThuNgay] (
    [RevenueSnapshotId] uniqueidentifier NOT NULL,
    [PropertyId] uniqueidentifier NOT NULL,
    [NightAuditId] uniqueidentifier NOT NULL,
    [BusinessDate] date NOT NULL,
    [RoomRevenue] decimal(18,2) NOT NULL,
    [OtherRevenue] decimal(18,2) NOT NULL,
    [TotalRevenue] decimal(18,2) NOT NULL,
    [TotalTax] decimal(18,2) NOT NULL,
    [TotalPayments] decimal(18,2) NOT NULL,
    [TotalRefunds] decimal(18,2) NOT NULL,
    [OutstandingBalance] decimal(18,2) NOT NULL,
    [RoomsSold] int NOT NULL,
    [TotalRooms] int NOT NULL,
    [OccupancyRate] decimal(9,4) NOT NULL,
    [ADR] decimal(18,2) NOT NULL,
    [RevPAR] decimal(18,2) NOT NULL,
    [Arrivals] int NOT NULL,
    [Departures] int NOT NULL,
    [NoShows] int NOT NULL,
    [Cancellations] int NOT NULL,
    [SnapshotJson] nvarchar(max) NOT NULL,
    [CreatedAtUtc] datetime2 NOT NULL,
    CONSTRAINT [PK_fo_DoanhThuNgay] PRIMARY KEY ([RevenueSnapshotId]),
    CONSTRAINT [FK_DoanhThuNgay_KiemDem_NightAuditId] FOREIGN KEY ([NightAuditId]) REFERENCES [fo].[KiemDem] ([NightAuditId]) ON DELETE NO ACTION
);
GO

CREATE TABLE [LostFoundHistories] (
    [LostFoundHistoryId] uniqueidentifier NOT NULL,
    [LostFoundItemId] uniqueidentifier NOT NULL,
    [PropertyId] uniqueidentifier NOT NULL,
    [Action] nvarchar(max) NOT NULL,
    [OccurredAtUtc] datetime2 NOT NULL,
    CONSTRAINT [PK_LostFoundHistories] PRIMARY KEY ([LostFoundHistoryId]),
    CONSTRAINT [FK_LostFoundHistories_LostFoundItems_LostFoundItemId] FOREIGN KEY ([LostFoundItemId]) REFERENCES [LostFoundItems] ([LostFoundItemId]) ON DELETE CASCADE
);
GO

CREATE TABLE [fo].[PhanBoThanhToan] (
    [HotelPaymentAllocationId] uniqueidentifier NOT NULL,
    [HotelPaymentId] uniqueidentifier NOT NULL,
    [PropertyId] uniqueidentifier NOT NULL,
    [HotelFolioId] uniqueidentifier NOT NULL,
    [ChargeFolioEntryId] uniqueidentifier NOT NULL,
    [ChargeDescription] nvarchar(500) NOT NULL,
    [Amount] decimal(18,2) NOT NULL,
    [CreatedAtUtc] datetime2 NOT NULL,
    CONSTRAINT [PK_fo_PhanBoThanhToan] PRIMARY KEY ([HotelPaymentAllocationId]),
    CONSTRAINT [FK_PhanBoThanhToan_ThanhToan_HotelPaymentId] FOREIGN KEY ([HotelPaymentId]) REFERENCES [fo].[ThanhToan] ([HotelPaymentId]) ON DELETE NO ACTION
);
GO

CREATE TABLE [fo].[LichSuYeuCau] (
    [ServiceHistoryId] uniqueidentifier NOT NULL,
    [ServiceRequestId] uniqueidentifier NOT NULL,
    [PropertyId] uniqueidentifier NOT NULL,
    [Action] nvarchar(200) NOT NULL,
    [Detail] nvarchar(500) NULL,
    [OccurredAtUtc] datetime2 NOT NULL,
    CONSTRAINT [PK_fo_LichSuYeuCau] PRIMARY KEY ([ServiceHistoryId]),
    CONSTRAINT [FK_LichSuYeuCau_YeuCauDichVu_ServiceRequestId] FOREIGN KEY ([ServiceRequestId]) REFERENCES [fo].[YeuCauDichVu] ([ServiceRequestId]) ON DELETE NO ACTION
);
GO

CREATE INDEX [IX_fo_DatPhong_CoSo_PhanKhuc] ON [fo].[DatPhong] ([PropertyId], [MarketSegment]);
GO

CREATE INDEX [IX_fo_BienDongTienMat_Ca_ThoiGian] ON [fo].[BienDongTienMat] ([CashierShiftId], [CreatedAtUtc]);
GO

CREATE UNIQUE INDEX [UQ_fo_BuocKiemDem_Audit_Step] ON [fo].[BuocKiemDem] ([NightAuditId], [Step]);
GO

CREATE INDEX [IX_fo_CaLamViec_CoSo_ThuNgan_TrangThai] ON [fo].[CaLamViec] ([PropertyId], [CashierId], [Status]);
GO

CREATE UNIQUE INDEX [UQ_fo_CaLamViec_SoCa] ON [fo].[CaLamViec] ([ShiftNumber]);
GO

CREATE INDEX [IX_fo_DanhThuc_CoSo_Gio_TrangThai] ON [fo].[DanhThuc] ([PropertyId], [WakeTime], [Status]);
GO

CREATE UNIQUE INDEX [UQ_fo_DanhThuc_SoDanhThuc] ON [fo].[DanhThuc] ([CallNumber]);
GO

CREATE INDEX [IX_fo_DatPhongDoan_CoSo_NgayDen] ON [fo].[DatPhongDoan] ([PropertyId], [ArrivalDate]);
GO

CREATE UNIQUE INDEX [UQ_fo_DatPhongDoan_MaDoan] ON [fo].[DatPhongDoan] ([GroupCode]);
GO

CREATE INDEX [IX_DoanhThuNgay_NightAuditId] ON [fo].[DoanhThuNgay] ([NightAuditId]);
GO

CREATE UNIQUE INDEX [UQ_fo_DoanhThuNgay_CoSo_Ngay] ON [fo].[DoanhThuNgay] ([PropertyId], [BusinessDate]);
GO

CREATE INDEX [IX_fo_DonDep_CoSo_Phong_TrangThai] ON [fo].[DonDep] ([PropertyId], [HotelRoomId], [Status]);
GO

CREATE UNIQUE INDEX [UQ_fo_DonDep_SoDonDep] ON [fo].[DonDep] ([TaskNumber]);
GO

CREATE INDEX [IX_fo_GiayToKhach_CoSo_Loai_So] ON [fo].[GiayToKhach] ([PropertyId], [DocumentType], [DocumentNumber]);
GO

CREATE INDEX [IX_GiayToKhach_GuestProfileId] ON [fo].[GiayToKhach] ([GuestProfileId]);
GO

CREATE INDEX [IX_fo_HoaDon_CoSo_Folio] ON [fo].[HoaDon] ([PropertyId], [HotelFolioId]);
GO

CREATE UNIQUE INDEX [UQ_fo_HoaDon_SoHoaDon] ON [fo].[HoaDon] ([InvoiceNumber]);
GO

CREATE INDEX [IX_fo_HoSoKhach_CoSo_Ten] ON [fo].[HoSoKhach] ([PropertyId], [FullName]);
GO

CREATE INDEX [IX_fo_HoSoKhach_Email] ON [fo].[HoSoKhach] ([Email]);
GO

CREATE INDEX [IX_fo_HoSoKhach_Phone] ON [fo].[HoSoKhach] ([Phone]);
GO

CREATE UNIQUE INDEX [UQ_fo_HoSoKhach_SoKhach] ON [fo].[HoSoKhach] ([GuestNumber]);
GO

CREATE UNIQUE INDEX [UQ_fo_KeHoachGia_MaKeHoach] ON [fo].[KeHoachGia] ([PlanCode]);
GO

CREATE INDEX [IX_fo_KiemDem_CoSo_Ngay] ON [fo].[KiemDem] ([PropertyId], [BusinessDate]);
GO

CREATE UNIQUE INDEX [UQ_fo_KiemDem_SoKiemDem] ON [fo].[KiemDem] ([AuditNumber]);
GO

CREATE UNIQUE INDEX [UQ_fo_LichGia_KeHoach_Ngay] ON [fo].[LichGia] ([RatePlanId], [Date]);
GO

CREATE INDEX [IX_fo_LichSuDanhThuc_Call_ThoiGian] ON [fo].[LichSuDanhThuc] ([WakeUpCallId], [OccurredAtUtc]);
GO

CREATE INDEX [IX_fo_LichSuYeuCau_YeuCau_ThoiGian] ON [fo].[LichSuYeuCau] ([ServiceRequestId], [OccurredAtUtc]);
GO

CREATE INDEX [IX_LostFoundHistories_LostFoundItemId] ON [LostFoundHistories] ([LostFoundItemId]);
GO

CREATE INDEX [IX_fo_MuaGia_KeHoach_ThoiGian] ON [fo].[MuaGia] ([RatePlanId], [FromDate], [ToDate]);
GO

CREATE UNIQUE INDEX [UQ_fo_NgayKinhDoanh_CoSo_Ngay] ON [fo].[NgayKinhDoanh] ([PropertyId], [BusinessDate]);
GO

CREATE INDEX [IX_fo_PhanBoThanhToan_CoSo_Folio] ON [fo].[PhanBoThanhToan] ([PropertyId], [HotelFolioId]);
GO

CREATE UNIQUE INDEX [UQ_fo_PhanBoThanhToan_Payment_Charge] ON [fo].[PhanBoThanhToan] ([HotelPaymentId], [ChargeFolioEntryId]);
GO

CREATE UNIQUE INDEX [UQ_fo_PhanPhongDoan_Doan_Phong] ON [fo].[PhanPhongDoan] ([GroupReservationId], [HotelRoomId]);
GO

CREATE INDEX [IX_fo_QuyTacGia_KeHoach_HoatDong] ON [fo].[QuyTacGia] ([RatePlanId], [IsActive]);
GO

CREATE UNIQUE INDEX [UQ_fo_SoThichKhach_Khach_Key] ON [fo].[SoThichKhach] ([GuestProfileId], [Key]);
GO

CREATE INDEX [IX_fo_ThanhToan_CoSo_Folio_ThoiGian] ON [fo].[ThanhToan] ([PropertyId], [HotelFolioId], [CreatedAtUtc]);
GO

CREATE INDEX [IX_fo_ThanhToan_CoSo_TrangThai] ON [fo].[ThanhToan] ([PropertyId], [Status]);
GO

CREATE UNIQUE INDEX [UQ_fo_ThanhToan_SoThanhToan] ON [fo].[ThanhToan] ([PaymentNumber]);
GO

CREATE INDEX [IX_fo_TrangThaiDonDep_CoSo_TrangThai] ON [fo].[TrangThaiDonDep] ([PropertyId], [Status]);
GO

CREATE INDEX [IX_fo_YeuCauDichVu_CoSo_TrangThai] ON [fo].[YeuCauDichVu] ([PropertyId], [Status]);
GO

CREATE UNIQUE INDEX [UQ_fo_YeuCauDichVu_SoYeuCau] ON [fo].[YeuCauDichVu] ([RequestNumber]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260720032319_AddReservationEnterpriseFields', N'8.0.7');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

CREATE SEQUENCE [fo].[TrinhTuSoLuuTru] START WITH 1 INCREMENT BY 1 NO MINVALUE NO MAXVALUE NO CYCLE;
GO

ALTER TABLE [fo].[DatPhong] ADD [CheckedInAtUtc] datetime2 NULL;
GO

ALTER TABLE [fo].[DatPhong] ADD [CompanyName] nvarchar(max) NULL;
GO

ALTER TABLE [fo].[DatPhong] ADD [CompletedAtUtc] datetime2 NULL;
GO

ALTER TABLE [fo].[DatPhong] ADD [DepositAmount] decimal(18,2) NOT NULL DEFAULT 0.0;
GO

ALTER TABLE [fo].[DatPhong] ADD [DepositDueDate] date NULL;
GO

ALTER TABLE [fo].[DatPhong] ADD [DepositStatus] nvarchar(30) NOT NULL DEFAULT N'';
GO

ALTER TABLE [fo].[DatPhong] ADD [GuaranteeExpiryDate] datetime2 NULL;
GO

ALTER TABLE [fo].[DatPhong] ADD [GuaranteeType] nvarchar(40) NOT NULL DEFAULT N'';
GO

ALTER TABLE [fo].[DatPhong] ADD [GuaranteedAtUtc] datetime2 NULL;
GO

ALTER TABLE [fo].[DatPhong] ADD [IsVip] bit NOT NULL DEFAULT CAST(0 AS bit);
GO

ALTER TABLE [fo].[DatPhong] ADD [MarketSegment] nvarchar(max) NULL;
GO

ALTER TABLE [fo].[DatPhong] ADD [TravelAgentName] nvarchar(max) NULL;
GO

CREATE TABLE [fo].[DatPhongDoan] (
    [GroupReservationId] uniqueidentifier NOT NULL,
    [PropertyId] uniqueidentifier NOT NULL,
    [GroupCode] nvarchar(40) NOT NULL,
    [GroupName] nvarchar(200) NOT NULL,
    [CompanyName] nvarchar(200) NULL,
    [AgencyName] nvarchar(200) NULL,
    [ContactPerson] nvarchar(200) NULL,
    [ContactPhone] nvarchar(50) NULL,
    [ContactEmail] nvarchar(200) NULL,
    [ArrivalDate] date NOT NULL,
    [DepartureDate] date NOT NULL,
    [ExpectedRooms] int NOT NULL,
    [ConfirmedRooms] int NOT NULL,
    [TotalAdults] int NOT NULL,
    [TotalChildren] int NOT NULL,
    [Status] nvarchar(30) NOT NULL,
    [Source] nvarchar(80) NULL,
    [SalesManager] nvarchar(200) NULL,
    [MarketSegment] nvarchar(80) NULL,
    [IsVip] bit NOT NULL,
    [Routing] nvarchar(30) NOT NULL,
    [SpecialRequests] nvarchar(1000) NULL,
    [Notes] nvarchar(1000) NULL,
    [TotalDeposit] decimal(18,2) NOT NULL,
    [TotalRevenue] decimal(18,2) NOT NULL,
    [OutstandingBalance] decimal(18,2) NOT NULL,
    [CreatedAtUtc] datetime2 NOT NULL,
    [CreatedBy] uniqueidentifier NULL,
    [UpdatedAtUtc] datetime2 NOT NULL,
    [UpdatedBy] uniqueidentifier NULL,
    [RowVersion] rowversion NULL,
    CONSTRAINT [PK_fo_DatPhongDoan] PRIMARY KEY ([GroupReservationId])
);
GO

CREATE TABLE [HotelFolios] (
    [HotelFolioId] uniqueidentifier NOT NULL,
    [PropertyId] uniqueidentifier NOT NULL,
    [HotelStayId] uniqueidentifier NOT NULL,
    [FolioNumber] nvarchar(max) NOT NULL,
    [Status] int NOT NULL,
    [CreatedAtUtc] datetime2 NOT NULL,
    [CreatedBy] uniqueidentifier NULL,
    [ClosedAtUtc] datetime2 NULL,
    [ClosedBy] uniqueidentifier NULL,
    [SettledAtUtc] datetime2 NULL,
    [SettledBy] uniqueidentifier NULL,
    [ArchivedAtUtc] datetime2 NULL,
    [RowVersion] varbinary(max) NULL,
    CONSTRAINT [PK_HotelFolios] PRIMARY KEY ([HotelFolioId])
);
GO

CREATE TABLE [fo].[KeHoachGia] (
    [RatePlanId] uniqueidentifier NOT NULL,
    [PropertyId] uniqueidentifier NOT NULL,
    [PlanCode] nvarchar(40) NOT NULL,
    [PlanName] nvarchar(160) NOT NULL,
    [RoomTypeId] uniqueidentifier NULL,
    [BaseRate] decimal(18,2) NOT NULL,
    [Status] nvarchar(30) NOT NULL,
    [Hierarchy] nvarchar(30) NOT NULL,
    [EffectiveFrom] datetime2 NOT NULL,
    [EffectiveTo] datetime2 NOT NULL,
    [AllowYieldManagement] bit NOT NULL,
    [YieldMinRate] decimal(18,2) NOT NULL,
    [YieldMaxRate] decimal(18,2) NOT NULL,
    [YieldThresholdOccupancy] int NOT NULL,
    [YieldAdjustmentPct] decimal(8,4) NOT NULL,
    [CreatedAtUtc] datetime2 NOT NULL,
    [CreatedBy] uniqueidentifier NULL,
    [RowVersion] rowversion NULL,
    CONSTRAINT [PK_fo_KeHoachGia] PRIMARY KEY ([RatePlanId])
);
GO

CREATE TABLE [fo].[LuuTruKhachSan] (
    [HotelStayId] uniqueidentifier NOT NULL,
    [PropertyId] uniqueidentifier NOT NULL,
    [StayNumber] nvarchar(40) NOT NULL,
    [ReservationId] uniqueidentifier NULL,
    [ReservationNumberSnapshot] nvarchar(40) NULL,
    [GuestId] uniqueidentifier NULL,
    [GuestNameSnapshot] nvarchar(200) NOT NULL,
    [GuestPhoneSnapshot] nvarchar(50) NULL,
    [RoomId] uniqueidentifier NOT NULL,
    [RoomNumberSnapshot] nvarchar(40) NOT NULL,
    [RoomTypeId] uniqueidentifier NOT NULL,
    [RoomTypeCodeSnapshot] nvarchar(40) NOT NULL,
    [ScheduledArrivalDate] date NOT NULL,
    [ScheduledDepartureDate] date NOT NULL,
    [ActualCheckInAtUtc] datetime2 NULL,
    [ActualCheckOutAtUtc] datetime2 NULL,
    [PreArrivalConfirmedAtUtc] datetime2 NULL,
    [LateCheckoutRequestedAtUtc] datetime2 NULL,
    [LateCheckoutUntilUtc] datetime2 NULL,
    [Adults] int NOT NULL,
    [Children] int NOT NULL,
    [Status] nvarchar(30) NOT NULL,
    [Source] nvarchar(80) NOT NULL,
    [InternalNotes] nvarchar(1000) NULL,
    [ExtensionCount] int NOT NULL,
    [OriginalDepartureDate] date NULL,
    [CreatedAtUtc] datetime2 NOT NULL,
    [CreatedBy] uniqueidentifier NULL,
    [UpdatedAtUtc] datetime2 NOT NULL,
    [UpdatedBy] uniqueidentifier NULL,
    [RowVersion] rowversion NULL,
    CONSTRAINT [PK_fo_LuuTruKhachSan] PRIMARY KEY ([HotelStayId]),
    CONSTRAINT [FK_LuuTruKhachSan_DatPhong_ReservationId] FOREIGN KEY ([ReservationId]) REFERENCES [fo].[DatPhong] ([ReservationId]) ON DELETE NO ACTION,
    CONSTRAINT [FK_LuuTruKhachSan_LoaiPhongKhachSan_RoomTypeId] FOREIGN KEY ([RoomTypeId]) REFERENCES [fo].[LoaiPhongKhachSan] ([HotelRoomTypeId]) ON DELETE NO ACTION,
    CONSTRAINT [FK_LuuTruKhachSan_PhongKhachSan_RoomId] FOREIGN KEY ([RoomId]) REFERENCES [fo].[PhongKhachSan] ([HotelRoomId]) ON DELETE NO ACTION
);
GO

CREATE TABLE [fo].[PhongDoan] (
    [GroupRoomAssignmentId] uniqueidentifier NOT NULL,
    [GroupReservationId] uniqueidentifier NOT NULL,
    [PropertyId] uniqueidentifier NOT NULL,
    [HotelRoomId] uniqueidentifier NOT NULL,
    [RoomNumber] nvarchar(40) NOT NULL,
    [RoomType] nvarchar(80) NOT NULL,
    [GuestId] uniqueidentifier NULL,
    [GuestName] nvarchar(200) NULL,
    [IsVip] bit NOT NULL,
    [Adults] int NOT NULL,
    [Children] int NOT NULL,
    [IsCheckedIn] bit NOT NULL,
    [IsCheckedOut] bit NOT NULL,
    [ReservationId] uniqueidentifier NULL,
    [CreatedAtUtc] datetime2 NOT NULL,
    [GroupReservationId1] uniqueidentifier NULL,
    CONSTRAINT [PK_fo_PhongDoan] PRIMARY KEY ([GroupRoomAssignmentId]),
    CONSTRAINT [FK_PhongDoan_DatPhongDoan_GroupReservationId] FOREIGN KEY ([GroupReservationId]) REFERENCES [fo].[DatPhongDoan] ([GroupReservationId]) ON DELETE CASCADE,
    CONSTRAINT [FK_PhongDoan_DatPhongDoan_GroupReservationId1] FOREIGN KEY ([GroupReservationId1]) REFERENCES [fo].[DatPhongDoan] ([GroupReservationId])
);
GO

CREATE TABLE [HotelFolioEntries] (
    [HotelFolioEntryId] uniqueidentifier NOT NULL,
    [HotelFolioId] uniqueidentifier NOT NULL,
    [PropertyId] uniqueidentifier NOT NULL,
    [SequenceNumber] int NOT NULL,
    [EntryType] int NOT NULL,
    [Amount] decimal(18,2) NOT NULL,
    [Description] nvarchar(max) NOT NULL,
    [ReferenceType] nvarchar(max) NULL,
    [ReferenceId] uniqueidentifier NULL,
    [BusinessDate] date NULL,
    [ReversesEntryId] uniqueidentifier NULL,
    [ReversedByEntryId] uniqueidentifier NULL,
    [CreatedAtUtc] datetime2 NOT NULL,
    [CreatedBy] uniqueidentifier NULL,
    CONSTRAINT [PK_HotelFolioEntries] PRIMARY KEY ([HotelFolioEntryId]),
    CONSTRAINT [FK_HotelFolioEntries_HotelFolios_HotelFolioId] FOREIGN KEY ([HotelFolioId]) REFERENCES [HotelFolios] ([HotelFolioId]) ON DELETE CASCADE
);
GO

CREATE TABLE [fo].[LichGia] (
    [RateCalendarEntryId] uniqueidentifier NOT NULL,
    [RatePlanId] uniqueidentifier NOT NULL,
    [PropertyId] uniqueidentifier NOT NULL,
    [Date] date NOT NULL,
    [Rate] decimal(18,2) NOT NULL,
    [UpdatedAtUtc] datetime2 NOT NULL,
    [RatePlanId1] uniqueidentifier NULL,
    CONSTRAINT [PK_fo_LichGia] PRIMARY KEY ([RateCalendarEntryId]),
    CONSTRAINT [FK_LichGia_KeHoachGia_RatePlanId] FOREIGN KEY ([RatePlanId]) REFERENCES [fo].[KeHoachGia] ([RatePlanId]) ON DELETE CASCADE,
    CONSTRAINT [FK_LichGia_KeHoachGia_RatePlanId1] FOREIGN KEY ([RatePlanId1]) REFERENCES [fo].[KeHoachGia] ([RatePlanId])
);
GO

CREATE TABLE [fo].[MuaGia] (
    [RateSeasonId] uniqueidentifier NOT NULL,
    [RatePlanId] uniqueidentifier NOT NULL,
    [PropertyId] uniqueidentifier NOT NULL,
    [Type] nvarchar(30) NOT NULL,
    [FromDate] datetime2 NOT NULL,
    [ToDate] datetime2 NOT NULL,
    [Multiplier] decimal(8,4) NOT NULL,
    [Description] nvarchar(500) NULL,
    [IsActive] bit NOT NULL,
    [RatePlanId1] uniqueidentifier NULL,
    CONSTRAINT [PK_fo_MuaGia] PRIMARY KEY ([RateSeasonId]),
    CONSTRAINT [FK_MuaGia_KeHoachGia_RatePlanId] FOREIGN KEY ([RatePlanId]) REFERENCES [fo].[KeHoachGia] ([RatePlanId]) ON DELETE CASCADE,
    CONSTRAINT [FK_MuaGia_KeHoachGia_RatePlanId1] FOREIGN KEY ([RatePlanId1]) REFERENCES [fo].[KeHoachGia] ([RatePlanId])
);
GO

CREATE TABLE [fo].[QuyTacGia] (
    [RateRuleId] uniqueidentifier NOT NULL,
    [RatePlanId] uniqueidentifier NOT NULL,
    [PropertyId] uniqueidentifier NOT NULL,
    [Type] nvarchar(50) NOT NULL,
    [Value] decimal(18,4) NOT NULL,
    [Priority] int NOT NULL,
    [Exclusive] bit NOT NULL,
    [Combinable] bit NOT NULL,
    [IsActive] bit NOT NULL,
    [Description] nvarchar(500) NULL,
    [CreatedAtUtc] datetime2 NOT NULL,
    [ExpiryDate] datetime2 NULL,
    [RatePlanId1] uniqueidentifier NULL,
    CONSTRAINT [PK_fo_QuyTacGia] PRIMARY KEY ([RateRuleId]),
    CONSTRAINT [FK_QuyTacGia_KeHoachGia_RatePlanId] FOREIGN KEY ([RatePlanId]) REFERENCES [fo].[KeHoachGia] ([RatePlanId]) ON DELETE CASCADE,
    CONSTRAINT [FK_QuyTacGia_KeHoachGia_RatePlanId1] FOREIGN KEY ([RatePlanId1]) REFERENCES [fo].[KeHoachGia] ([RatePlanId])
);
GO

CREATE TABLE [fo].[LichSuLuuTruKhachSan] (
    [HistoryId] uniqueidentifier NOT NULL,
    [HotelStayId] uniqueidentifier NOT NULL,
    [PropertyId] uniqueidentifier NOT NULL,
    [Action] nvarchar(80) NOT NULL,
    [FromStatus] nvarchar(30) NULL,
    [ToStatus] nvarchar(30) NOT NULL,
    [ActorId] uniqueidentifier NULL,
    [OccurredAtUtc] datetime2 NOT NULL,
    [CorrelationId] uniqueidentifier NOT NULL,
    [SafeMetadata] nvarchar(300) NULL,
    CONSTRAINT [PK_fo_LichSuLuuTruKhachSan] PRIMARY KEY ([HistoryId]),
    CONSTRAINT [FK_LichSuLuuTruKhachSan_LuuTruKhachSan_HotelStayId] FOREIGN KEY ([HotelStayId]) REFERENCES [fo].[LuuTruKhachSan] ([HotelStayId]) ON DELETE NO ACTION
);
GO

CREATE INDEX [IX_fo_DatPhongDoan_CoSo_NgayO] ON [fo].[DatPhongDoan] ([PropertyId], [ArrivalDate], [DepartureDate]);
GO

CREATE INDEX [IX_fo_DatPhongDoan_CoSo_TrangThai] ON [fo].[DatPhongDoan] ([PropertyId], [Status]);
GO

CREATE UNIQUE INDEX [UQ_fo_DatPhongDoan_CoSo_MaDoan] ON [fo].[DatPhongDoan] ([PropertyId], [GroupCode]);
GO

CREATE INDEX [IX_HotelFolioEntries_HotelFolioId] ON [HotelFolioEntries] ([HotelFolioId]);
GO

CREATE INDEX [IX_fo_KeHoachGia_CoSo_TrangThai] ON [fo].[KeHoachGia] ([PropertyId], [Status]);
GO

CREATE UNIQUE INDEX [UQ_fo_KeHoachGia_CoSo_Ma] ON [fo].[KeHoachGia] ([PropertyId], [PlanCode]);
GO

CREATE INDEX [IX_LichGia_RatePlanId1] ON [fo].[LichGia] ([RatePlanId1]);
GO

CREATE UNIQUE INDEX [UQ_fo_LichGia_KeHoach_Ngay] ON [fo].[LichGia] ([RatePlanId], [Date]);
GO

CREATE INDEX [IX_fo_LichSuLuuTruKhachSan_LuuTru_Luc] ON [fo].[LichSuLuuTruKhachSan] ([HotelStayId], [OccurredAtUtc]);
GO

CREATE INDEX [IX_fo_LuuTruKhachSan_CapNhat] ON [fo].[LuuTruKhachSan] ([UpdatedAtUtc]);
GO

CREATE INDEX [IX_fo_LuuTruKhachSan_CoSo_NgayDen] ON [fo].[LuuTruKhachSan] ([PropertyId], [ScheduledArrivalDate]);
GO

CREATE INDEX [IX_fo_LuuTruKhachSan_CoSo_NgayDi] ON [fo].[LuuTruKhachSan] ([PropertyId], [ScheduledDepartureDate]);
GO

CREATE INDEX [IX_fo_LuuTruKhachSan_CoSo_Phong_TrangThai] ON [fo].[LuuTruKhachSan] ([PropertyId], [RoomId], [Status]);
GO

CREATE INDEX [IX_fo_LuuTruKhachSan_CoSo_TrangThai] ON [fo].[LuuTruKhachSan] ([PropertyId], [Status]);
GO

CREATE INDEX [IX_LuuTruKhachSan_RoomId] ON [fo].[LuuTruKhachSan] ([RoomId]);
GO

CREATE INDEX [IX_LuuTruKhachSan_RoomTypeId] ON [fo].[LuuTruKhachSan] ([RoomTypeId]);
GO

CREATE UNIQUE INDEX [UQ_fo_LuuTruKhachSan_DatPhong_HoatDong] ON [fo].[LuuTruKhachSan] ([ReservationId]) WHERE [ReservationId] IS NOT NULL AND [Status] IN ('PendingArrival', 'InHouse');
GO

CREATE UNIQUE INDEX [UQ_fo_LuuTruKhachSan_SoLuuTru] ON [fo].[LuuTruKhachSan] ([StayNumber]);
GO

CREATE INDEX [IX_fo_MuaGia_KeHoach_Ngay] ON [fo].[MuaGia] ([RatePlanId], [FromDate], [ToDate]);
GO

CREATE INDEX [IX_MuaGia_RatePlanId1] ON [fo].[MuaGia] ([RatePlanId1]);
GO

CREATE INDEX [IX_PhongDoan_GroupReservationId1] ON [fo].[PhongDoan] ([GroupReservationId1]);
GO

CREATE UNIQUE INDEX [UQ_fo_PhongDoan_Doan_Phong] ON [fo].[PhongDoan] ([GroupReservationId], [HotelRoomId]);
GO

CREATE INDEX [IX_fo_QuyTacGia_KeHoach_UuTien] ON [fo].[QuyTacGia] ([RatePlanId], [Priority]);
GO

CREATE INDEX [IX_QuyTacGia_RatePlanId1] ON [fo].[QuyTacGia] ([RatePlanId1]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260721065248_AddRatePlanGroupReservationAndReservationGuaranteeDeposit', N'8.0.7');
GO

COMMIT;
GO

