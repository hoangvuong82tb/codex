/*
    Hotel/Front Office schema reconciliation - 2026-09-07

    Purpose
    -------
    Bring the physical database in line with the current EF model for tables
    that are absent from the target database. This is deliberately not an EF
    migration replay: the target database contains tables created by older
    migration branches and the local migration source/history is incomplete.

    Safety
    ------
    - Run only after a verified COPY_ONLY backup.
    - Run against ASMILE_BO explicitly; this script fails closed otherwise.
    - Idempotent for the 14 currently-missing model tables.
    - Does not write __EFMigrationsHistory. History repair is a separate,
      evidence-based operation after schema verification.
*/

SET NOCOUNT ON;
SET XACT_ABORT ON;

IF DB_NAME() <> N'ASMILE_BO'
    THROW 51000, 'Wrong database. Expected ASMILE_BO.', 1;

IF SCHEMA_ID(N'fo') IS NULL
    THROW 51001, 'Required schema fo is missing.', 1;

IF OBJECT_ID(N'fo.PhongKhachSan', N'U') IS NULL
    THROW 51002, 'Dependency fo.PhongKhachSan is missing.', 1;

IF OBJECT_ID(N'fo.HoSoKhach', N'U') IS NULL
    THROW 51003, 'Dependency fo.HoSoKhach is missing.', 1;

IF OBJECT_ID(N'fo.LuuTruKhachSan', N'U') IS NULL
    THROW 51004, 'Dependency fo.LuuTruKhachSan is missing.', 1;

IF OBJECT_ID(N'fo.DatPhongDoan', N'U') IS NULL
    THROW 51005, 'Dependency fo.DatPhongDoan is missing.', 1;

BEGIN TRANSACTION;

/* Core settings tables currently present in the EF model but absent in DB. */
IF OBJECT_ID(N'dbo.AppModules', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[AppModules]
    (
        [Id] uniqueidentifier NOT NULL,
        [AllowClientAccess] bit NOT NULL,
        [AppKey] nvarchar(max) NOT NULL,
        [Description] nvarchar(max) NULL,
        [Icon] nvarchar(max) NULL,
        [IsEnabled] bit NOT NULL,
        [Name] nvarchar(max) NOT NULL,
        [PinToTaskbar] bit NOT NULL,
        [RequiredPermission] nvarchar(max) NULL,
        [ShowOnDesktop] bit NOT NULL,
        [SortOrder] int NOT NULL,
        [Version] nvarchar(max) NULL,
        CONSTRAINT [PK_AppModules] PRIMARY KEY ([Id])
    );
END;

IF OBJECT_ID(N'dbo.SettingChangeLogs', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[SettingChangeLogs]
    (
        [Id] uniqueidentifier NOT NULL,
        [ChangedAt] datetime2 NOT NULL,
        [ChangedBy] nvarchar(max) NOT NULL,
        [NewValue] nvarchar(max) NULL,
        [OldValue] nvarchar(max) NULL,
        [Reason] nvarchar(max) NULL,
        [Scope] int NOT NULL,
        [ScopeTarget] nvarchar(max) NULL,
        [SettingKey] nvarchar(max) NOT NULL,
        CONSTRAINT [PK_SettingChangeLogs] PRIMARY KEY ([Id])
    );
END;

IF OBJECT_ID(N'dbo.SettingDefinitions', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[SettingDefinitions]
    (
        [Id] uniqueidentifier NOT NULL,
        [Category] nvarchar(max) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [DefaultValue] nvarchar(max) NULL,
        [Description] nvarchar(max) NULL,
        [DisplayName] nvarchar(max) NOT NULL,
        [Group] nvarchar(max) NOT NULL,
        [IsReadOnly] bit NOT NULL,
        [IsRequired] bit NOT NULL,
        [IsSensitive] bit NOT NULL,
        [Key] nvarchar(max) NOT NULL,
        [RequiredPermission] nvarchar(max) NULL,
        [RequiresRestart] bit NOT NULL,
        [Scope] int NOT NULL,
        [SortOrder] int NOT NULL,
        [ValueType] int NOT NULL,
        CONSTRAINT [PK_SettingDefinitions] PRIMARY KEY ([Id])
    );
END;

IF OBJECT_ID(N'dbo.SettingValues', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[SettingValues]
    (
        [Id] uniqueidentifier NOT NULL,
        [DefinitionKey] nvarchar(max) NOT NULL,
        [IsEncrypted] bit NOT NULL,
        [Scope] int NOT NULL,
        [ScopeTarget] nvarchar(max) NULL,
        [UpdatedAt] datetime2 NOT NULL,
        [UpdatedBy] nvarchar(max) NULL,
        [Value] nvarchar(max) NULL,
        CONSTRAINT [PK_SettingValues] PRIMARY KEY ([Id])
    );
END;

/* Housekeeping tables. */
IF OBJECT_ID(N'fo.DonDep', N'U') IS NULL
BEGIN
    CREATE TABLE [fo].[DonDep]
    (
        [HousekeepingTaskId] uniqueidentifier NOT NULL,
        [PropertyId] uniqueidentifier NOT NULL,
        [HotelRoomId] uniqueidentifier NOT NULL,
        [TaskNumber] nvarchar(40) NOT NULL,
        [TaskType] nvarchar(50) NOT NULL,
        [Status] nvarchar(30) NOT NULL,
        [Priority] nvarchar(30) NOT NULL,
        [AssignedHousekeeperId] uniqueidentifier NULL,
        [InspectorNotes] nvarchar(500) NULL,
        [InspectionResult] nvarchar(30) NULL,
        [CreatedAtUtc] datetime2 NOT NULL,
        [CreatedBy] uniqueidentifier NULL,
        [StartedAtUtc] datetime2 NULL,
        [CompletedAtUtc] datetime2 NULL,
        [InspectedAtUtc] datetime2 NULL,
        [CancelledAtUtc] datetime2 NULL,
        [ScheduledDate] datetime2 NOT NULL CONSTRAINT [DF_fo_DonDep_ScheduledDate] DEFAULT (CONVERT(date, SYSUTCDATETIME())),
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_fo_DonDep] PRIMARY KEY ([HousekeepingTaskId])
    );
END;

IF OBJECT_ID(N'fo.TrangThaiDonDep', N'U') IS NULL
BEGIN
    CREATE TABLE [fo].[TrangThaiDonDep]
    (
        [HotelRoomId] uniqueidentifier NOT NULL,
        [PropertyId] uniqueidentifier NOT NULL,
        [Status] nvarchar(30) NOT NULL,
        [StatusChangedAtUtc] datetime2 NOT NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_fo_TrangThaiDonDep] PRIMARY KEY ([HotelRoomId])
    );
END;

IF OBJECT_ID(N'fo.RoomInspection', N'U') IS NULL
BEGIN
    CREATE TABLE [fo].[RoomInspection]
    (
        [InspectionId] uniqueidentifier NOT NULL,
        [HousekeepingTaskId] uniqueidentifier NOT NULL,
        [PropertyId] uniqueidentifier NOT NULL,
        [InspectorId] uniqueidentifier NULL,
        [Status] nvarchar(30) NOT NULL,
        [Result] nvarchar(30) NULL,
        [ChecklistData] nvarchar(max) NULL,
        [InspectedAtUtc] datetime2 NULL,
        [Notes] nvarchar(500) NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_fo_RoomInspection] PRIMARY KEY ([InspectionId])
    );
END;

/* Group rooming list table; legacy PhanPhongDoan is not the current model. */
IF OBJECT_ID(N'fo.PhongDoan', N'U') IS NULL
BEGIN
    CREATE TABLE [fo].[PhongDoan]
    (
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
        CONSTRAINT [PK_fo_PhongDoan] PRIMARY KEY ([GroupRoomAssignmentId])
    );
END;

/* Stay/key-card and minibar contracts. */
IF OBJECT_ID(N'fo.KeyCardCredential', N'U') IS NULL
BEGIN
    CREATE TABLE [fo].[KeyCardCredential]
    (
        [CredentialId] uniqueidentifier NOT NULL,
        [PropertyId] uniqueidentifier NOT NULL,
        [CardSerial] nvarchar(80) NOT NULL,
        [Status] nvarchar(30) NOT NULL,
        [StayId] uniqueidentifier NOT NULL,
        [GuestId] uniqueidentifier NULL,
        [HotelRoomId] uniqueidentifier NULL,
        [IssuedAtUtc] datetime2 NOT NULL,
        [IssuedBy] uniqueidentifier NULL,
        [ExpiresAtUtc] datetime2 NULL,
        [DisabledAtUtc] datetime2 NULL,
        [DisabledBy] uniqueidentifier NULL,
        [DisableReason] nvarchar(500) NULL,
        [ReturnedAtUtc] datetime2 NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_fo_KeyCardCredential] PRIMARY KEY ([CredentialId])
    );
END;

IF OBJECT_ID(N'fo.MinibarSession', N'U') IS NULL
BEGIN
    CREATE TABLE [fo].[MinibarSession]
    (
        [SessionId] uniqueidentifier NOT NULL,
        [PropertyId] uniqueidentifier NOT NULL,
        [StayId] uniqueidentifier NOT NULL,
        [HotelRoomId] uniqueidentifier NOT NULL,
        [Status] nvarchar(30) NOT NULL,
        [CheckedBy] uniqueidentifier NULL,
        [CheckedAtUtc] datetime2 NULL,
        [PostedAtUtc] datetime2 NULL,
        [FolioEntryId] uniqueidentifier NULL,
        [RowVersion] rowversion NULL,
        CONSTRAINT [PK_fo_MinibarSession] PRIMARY KEY ([SessionId])
    );
END;

IF OBJECT_ID(N'fo.KeyCardEvent', N'U') IS NULL
BEGIN
    CREATE TABLE [fo].[KeyCardEvent]
    (
        [EventId] uniqueidentifier NOT NULL,
        [CredentialId] uniqueidentifier NOT NULL,
        [PropertyId] uniqueidentifier NOT NULL,
        [EventType] nvarchar(40) NOT NULL,
        [OccurredAtUtc] datetime2 NOT NULL,
        [DoorId] nvarchar(80) NULL,
        [AccessGranted] bit NOT NULL,
        [Notes] nvarchar(500) NULL,
        CONSTRAINT [PK_fo_KeyCardEvent] PRIMARY KEY ([EventId])
    );
END;

IF OBJECT_ID(N'fo.MinibarConsumption', N'U') IS NULL
BEGIN
    CREATE TABLE [fo].[MinibarConsumption]
    (
        [ConsumptionId] uniqueidentifier NOT NULL,
        [SessionId] uniqueidentifier NOT NULL,
        [PropertyId] uniqueidentifier NOT NULL,
        [ItemCode] nvarchar(80) NOT NULL,
        [ItemName] nvarchar(200) NOT NULL,
        [Quantity] decimal(18,3) NOT NULL CONSTRAINT [DF_fo_MinibarConsumption_Quantity] DEFAULT (1.0),
        [UnitPrice] decimal(18,2) NOT NULL,
        [LineAmount] decimal(18,2) NOT NULL,
        CONSTRAINT [PK_fo_MinibarConsumption] PRIMARY KEY ([ConsumptionId])
    );
END;

/* Reservation/stay fields added by the current model after the legacy tables
   had already been created. */
IF COL_LENGTH(N'fo.DatPhong', N'CheckedInAtUtc') IS NULL
    ALTER TABLE [fo].[DatPhong] ADD [CheckedInAtUtc] datetime2 NULL;
IF COL_LENGTH(N'fo.DatPhong', N'CompanyName') IS NULL
    ALTER TABLE [fo].[DatPhong] ADD [CompanyName] nvarchar(max) NULL;
IF COL_LENGTH(N'fo.DatPhong', N'CompletedAtUtc') IS NULL
    ALTER TABLE [fo].[DatPhong] ADD [CompletedAtUtc] datetime2 NULL;
IF COL_LENGTH(N'fo.DatPhong', N'DepositDueDate') IS NULL
    ALTER TABLE [fo].[DatPhong] ADD [DepositDueDate] date NULL;
IF COL_LENGTH(N'fo.DatPhong', N'GuaranteedAtUtc') IS NULL
    ALTER TABLE [fo].[DatPhong] ADD [GuaranteedAtUtc] datetime2 NULL;
IF COL_LENGTH(N'fo.DatPhong', N'GuaranteeExpiryDate') IS NULL
    ALTER TABLE [fo].[DatPhong] ADD [GuaranteeExpiryDate] datetime2 NULL;
IF COL_LENGTH(N'fo.DatPhong', N'IsVip') IS NULL
    ALTER TABLE [fo].[DatPhong] ADD [IsVip] bit NOT NULL CONSTRAINT [DF_fo_DatPhong_IsVip] DEFAULT (0);
IF COL_LENGTH(N'fo.DatPhong', N'MarketSegment') IS NULL
    ALTER TABLE [fo].[DatPhong] ADD [MarketSegment] nvarchar(max) NULL;
IF COL_LENGTH(N'fo.DatPhong', N'TravelAgentName') IS NULL
    ALTER TABLE [fo].[DatPhong] ADD [TravelAgentName] nvarchar(max) NULL;

IF COL_LENGTH(N'fo.LuuTruKhachSan', N'AreRoomPreferencesConfirmed') IS NULL
    ALTER TABLE [fo].[LuuTruKhachSan] ADD [AreRoomPreferencesConfirmed] bit NOT NULL CONSTRAINT [DF_fo_LuuTruKhachSan_AreRoomPreferencesConfirmed] DEFAULT (0);
IF COL_LENGTH(N'fo.LuuTruKhachSan', N'AreSpecialRequestsConfirmed') IS NULL
    ALTER TABLE [fo].[LuuTruKhachSan] ADD [AreSpecialRequestsConfirmed] bit NOT NULL CONSTRAINT [DF_fo_LuuTruKhachSan_AreSpecialRequestsConfirmed] DEFAULT (0);
IF COL_LENGTH(N'fo.LuuTruKhachSan', N'DepartureNotes') IS NULL
    ALTER TABLE [fo].[LuuTruKhachSan] ADD [DepartureNotes] nvarchar(max) NULL;
IF COL_LENGTH(N'fo.LuuTruKhachSan', N'DepartureReason') IS NULL
    ALTER TABLE [fo].[LuuTruKhachSan] ADD [DepartureReason] nvarchar(max) NULL;
IF COL_LENGTH(N'fo.LuuTruKhachSan', N'Eta') IS NULL
    ALTER TABLE [fo].[LuuTruKhachSan] ADD [Eta] datetime2 NULL;
IF COL_LENGTH(N'fo.LuuTruKhachSan', N'ExtensionCount') IS NULL
    ALTER TABLE [fo].[LuuTruKhachSan] ADD [ExtensionCount] int NOT NULL CONSTRAINT [DF_fo_LuuTruKhachSan_ExtensionCount] DEFAULT (0);
IF COL_LENGTH(N'fo.LuuTruKhachSan', N'HasOutstandingBalance') IS NULL
    ALTER TABLE [fo].[LuuTruKhachSan] ADD [HasOutstandingBalance] bit NOT NULL CONSTRAINT [DF_fo_LuuTruKhachSan_HasOutstandingBalance] DEFAULT (0);
IF COL_LENGTH(N'fo.LuuTruKhachSan', N'HasPendingCharges') IS NULL
    ALTER TABLE [fo].[LuuTruKhachSan] ADD [HasPendingCharges] bit NOT NULL CONSTRAINT [DF_fo_LuuTruKhachSan_HasPendingCharges] DEFAULT (0);
IF COL_LENGTH(N'fo.LuuTruKhachSan', N'IsDepartureApproved') IS NULL
    ALTER TABLE [fo].[LuuTruKhachSan] ADD [IsDepartureApproved] bit NOT NULL CONSTRAINT [DF_fo_LuuTruKhachSan_IsDepartureApproved] DEFAULT (0);
IF COL_LENGTH(N'fo.LuuTruKhachSan', N'IsDoNotDisturb') IS NULL
    ALTER TABLE [fo].[LuuTruKhachSan] ADD [IsDoNotDisturb] bit NOT NULL CONSTRAINT [DF_fo_LuuTruKhachSan_IsDoNotDisturb] DEFAULT (0);
IF COL_LENGTH(N'fo.LuuTruKhachSan', N'IsHouseUse') IS NULL
    ALTER TABLE [fo].[LuuTruKhachSan] ADD [IsHouseUse] bit NOT NULL CONSTRAINT [DF_fo_LuuTruKhachSan_IsHouseUse] DEFAULT (0);
IF COL_LENGTH(N'fo.LuuTruKhachSan', N'IsIdentityVerified') IS NULL
    ALTER TABLE [fo].[LuuTruKhachSan] ADD [IsIdentityVerified] bit NOT NULL CONSTRAINT [DF_fo_LuuTruKhachSan_IsIdentityVerified] DEFAULT (0);
IF COL_LENGTH(N'fo.LuuTruKhachSan', N'IsVipService') IS NULL
    ALTER TABLE [fo].[LuuTruKhachSan] ADD [IsVipService] bit NOT NULL CONSTRAINT [DF_fo_LuuTruKhachSan_IsVipService] DEFAULT (0);
IF COL_LENGTH(N'fo.LuuTruKhachSan', N'LateCheckoutRequestedAtUtc') IS NULL
    ALTER TABLE [fo].[LuuTruKhachSan] ADD [LateCheckoutRequestedAtUtc] datetime2 NULL;
IF COL_LENGTH(N'fo.LuuTruKhachSan', N'LateCheckoutUntilUtc') IS NULL
    ALTER TABLE [fo].[LuuTruKhachSan] ADD [LateCheckoutUntilUtc] datetime2 NULL;
IF COL_LENGTH(N'fo.LuuTruKhachSan', N'OriginalDepartureDate') IS NULL
    ALTER TABLE [fo].[LuuTruKhachSan] ADD [OriginalDepartureDate] date NULL;
IF COL_LENGTH(N'fo.LuuTruKhachSan', N'PreArrivalConfirmedAtUtc') IS NULL
    ALTER TABLE [fo].[LuuTruKhachSan] ADD [PreArrivalConfirmedAtUtc] datetime2 NULL;
IF COL_LENGTH(N'fo.LuuTruKhachSan', N'PreCheckInCompletedAtUtc') IS NULL
    ALTER TABLE [fo].[LuuTruKhachSan] ADD [PreCheckInCompletedAtUtc] datetime2 NULL;
IF COL_LENGTH(N'fo.LuuTruKhachSan', N'SpecialRequests') IS NULL
    ALTER TABLE [fo].[LuuTruKhachSan] ADD [SpecialRequests] nvarchar(max) NULL;

/* Existing legacy rows may contain NULLs in fields that are required by the
   current model. Normalize them before tightening the column contract. */
UPDATE [fo].[DatPhong] SET [DepositAmount] = 0 WHERE [DepositAmount] IS NULL;
UPDATE [fo].[DatPhong] SET [DepositStatus] = N'' WHERE [DepositStatus] IS NULL;
UPDATE [fo].[DatPhong] SET [GuaranteeType] = N'' WHERE [GuaranteeType] IS NULL;
ALTER TABLE [fo].[DatPhong] ALTER COLUMN [DepositAmount] decimal(18,2) NOT NULL;
ALTER TABLE [fo].[DatPhong] ALTER COLUMN [DepositStatus] nvarchar(30) NOT NULL;
ALTER TABLE [fo].[DatPhong] ALTER COLUMN [GuaranteeType] nvarchar(40) NOT NULL;

/* Existing rate/group tables were created by an older migration branch with
   wider/narrower definitions than the current model snapshot. */
IF EXISTS (SELECT 1 FROM [fo].[DatPhongDoan] WHERE LEN([Status]) > 30 OR LEN([MarketSegment]) > 80)
    THROW 51010, 'DatPhongDoan contains values exceeding the current model length.', 1;
IF EXISTS (SELECT 1 FROM [fo].[KeHoachGia] WHERE LEN([Status]) > 30 OR LEN([Hierarchy]) > 30 OR LEN([PlanName]) > 160)
    THROW 51011, 'KeHoachGia contains values exceeding the current model length.', 1;
IF EXISTS (SELECT 1 FROM [fo].[MuaGia] WHERE LEN([Type]) > 30)
    THROW 51012, 'MuaGia contains values exceeding the current model length.', 1;
IF EXISTS (SELECT 1 FROM [fo].[QuyTacGia] WHERE LEN([Type]) > 50)
    THROW 51013, 'QuyTacGia contains values exceeding the current model length.', 1;
IF EXISTS (SELECT 1 FROM [fo].[KeHoachGia] WHERE ABS([YieldAdjustmentPct]) >= 10000)
    THROW 51014, 'KeHoachGia YieldAdjustmentPct does not fit decimal(8,4).', 1;
IF EXISTS (SELECT 1 FROM [fo].[MuaGia] WHERE ABS([Multiplier]) >= 10000)
    THROW 51015, 'MuaGia Multiplier does not fit decimal(8,4).', 1;

UPDATE [fo].[DatPhongDoan] SET [Status] = N'' WHERE [Status] IS NULL;
UPDATE [fo].[KeHoachGia] SET [Status] = N'', [Hierarchy] = N'', [PlanName] = N'' WHERE [Status] IS NULL OR [Hierarchy] IS NULL OR [PlanName] IS NULL;
UPDATE [fo].[MuaGia] SET [Type] = N'' WHERE [Type] IS NULL;
UPDATE [fo].[QuyTacGia] SET [Type] = N'' WHERE [Type] IS NULL;
UPDATE [fo].[KeHoachGia] SET [YieldAdjustmentPct] = 0 WHERE [YieldAdjustmentPct] IS NULL;
UPDATE [fo].[MuaGia] SET [Multiplier] = 0 WHERE [Multiplier] IS NULL;
UPDATE [fo].[QuyTacGia] SET [Value] = 0 WHERE [Value] IS NULL;

ALTER TABLE [dbo].[MenuItems] ALTER COLUMN [PreparationArea] nvarchar(max) NULL;
ALTER TABLE [fo].[DatPhongDoan] ALTER COLUMN [Status] nvarchar(30) NOT NULL;
ALTER TABLE [fo].[DatPhongDoan] ALTER COLUMN [MarketSegment] nvarchar(80) NULL;
ALTER TABLE [fo].[KeHoachGia] ALTER COLUMN [Status] nvarchar(30) NOT NULL;
ALTER TABLE [fo].[KeHoachGia] ALTER COLUMN [Hierarchy] nvarchar(30) NOT NULL;
ALTER TABLE [fo].[KeHoachGia] ALTER COLUMN [PlanName] nvarchar(160) NOT NULL;
ALTER TABLE [fo].[KeHoachGia] ALTER COLUMN [YieldAdjustmentPct] decimal(8,4) NOT NULL;
ALTER TABLE [fo].[MuaGia] ALTER COLUMN [Type] nvarchar(30) NOT NULL;
ALTER TABLE [fo].[MuaGia] ALTER COLUMN [Multiplier] decimal(8,4) NOT NULL;
ALTER TABLE [fo].[QuyTacGia] ALTER COLUMN [Type] nvarchar(50) NOT NULL;
ALTER TABLE [fo].[QuyTacGia] ALTER COLUMN [Value] decimal(18,4) NOT NULL;

/* Normalize indexes that affect property isolation and operational queries. */
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'fo.DatPhong') AND name = N'IX_DatPhong_GuestId')
    CREATE INDEX [IX_DatPhong_GuestId] ON [fo].[DatPhong] ([GuestId]);

IF EXISTS (SELECT 1 FROM sys.key_constraints WHERE parent_object_id = OBJECT_ID(N'fo.DatPhongDoan') AND name = N'UQ__DatPhong__3B97438046112FD8')
    ALTER TABLE [fo].[DatPhongDoan] DROP CONSTRAINT [UQ__DatPhong__3B97438046112FD8];
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'fo.DatPhongDoan') AND name = N'UQ_fo_DatPhongDoan_CoSo_MaDoan')
    CREATE UNIQUE INDEX [UQ_fo_DatPhongDoan_CoSo_MaDoan] ON [fo].[DatPhongDoan] ([PropertyId], [GroupCode]);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'fo.DatPhongDoan') AND name = N'IX_fo_DatPhongDoan_CoSo_TrangThai')
    CREATE INDEX [IX_fo_DatPhongDoan_CoSo_TrangThai] ON [fo].[DatPhongDoan] ([PropertyId], [Status]);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'fo.DatPhongDoan') AND name = N'IX_fo_DatPhongDoan_CoSo_NgayO')
    CREATE INDEX [IX_fo_DatPhongDoan_CoSo_NgayO] ON [fo].[DatPhongDoan] ([PropertyId], [ArrivalDate], [DepartureDate]);

IF EXISTS (SELECT 1 FROM sys.key_constraints WHERE parent_object_id = OBJECT_ID(N'fo.KeHoachGia') AND name = N'UQ__KeHoachG__DDC8069BCA38066B')
    ALTER TABLE [fo].[KeHoachGia] DROP CONSTRAINT [UQ__KeHoachG__DDC8069BCA38066B];
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'fo.KeHoachGia') AND name = N'UQ_fo_KeHoachGia_CoSo_Ma')
    CREATE UNIQUE INDEX [UQ_fo_KeHoachGia_CoSo_Ma] ON [fo].[KeHoachGia] ([PropertyId], [PlanCode]);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'fo.KeHoachGia') AND name = N'IX_fo_KeHoachGia_CoSo_TrangThai')
    CREATE INDEX [IX_fo_KeHoachGia_CoSo_TrangThai] ON [fo].[KeHoachGia] ([PropertyId], [Status]);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'fo.MuaGia') AND name = N'IX_fo_MuaGia_KeHoach_Ngay')
    CREATE INDEX [IX_fo_MuaGia_KeHoach_Ngay] ON [fo].[MuaGia] ([RatePlanId], [FromDate], [ToDate]);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'fo.QuyTacGia') AND name = N'IX_fo_QuyTacGia_KeHoach_UuTien')
    CREATE INDEX [IX_fo_QuyTacGia_KeHoach_UuTien] ON [fo].[QuyTacGia] ([RatePlanId], [Priority]);

/* Guest, stay and payment query indexes from the current EF model. */
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'fo.GiayToKhach') AND name = N'IX_GiayToKhach_GuestProfileId')
    CREATE INDEX [IX_GiayToKhach_GuestProfileId] ON [fo].[GiayToKhach] ([GuestProfileId]);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'fo.GiayToKhach') AND name = N'IX_GiayToKhach_PropertyId_GuestProfileId')
    CREATE INDEX [IX_GiayToKhach_PropertyId_GuestProfileId] ON [fo].[GiayToKhach] ([PropertyId], [GuestProfileId]);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'fo.SoThichKhach') AND name = N'IX_SoThichKhach_GuestProfileId')
    CREATE INDEX [IX_SoThichKhach_GuestProfileId] ON [fo].[SoThichKhach] ([GuestProfileId]);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'fo.SoThichKhach') AND name = N'IX_SoThichKhach_PropertyId_GuestProfileId')
    CREATE INDEX [IX_SoThichKhach_PropertyId_GuestProfileId] ON [fo].[SoThichKhach] ([PropertyId], [GuestProfileId]);

IF EXISTS (SELECT 1 FROM sys.key_constraints WHERE parent_object_id = OBJECT_ID(N'fo.HoSoKhach') AND name = N'UQ_fo_HoSoKhach_SoKhach')
    ALTER TABLE [fo].[HoSoKhach] DROP CONSTRAINT [UQ_fo_HoSoKhach_SoKhach];
ELSE IF EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'fo.HoSoKhach') AND name = N'UQ_fo_HoSoKhach_SoKhach')
    DROP INDEX [UQ_fo_HoSoKhach_SoKhach] ON [fo].[HoSoKhach];
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'fo.HoSoKhach') AND name = N'IX_HoSoKhach_PropertyId_GuestNumber')
    CREATE UNIQUE INDEX [IX_HoSoKhach_PropertyId_GuestNumber] ON [fo].[HoSoKhach] ([PropertyId], [GuestNumber]);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'fo.ThanhToan') AND name = N'IX_ThanhToan_HotelFolioId')
    CREATE INDEX [IX_ThanhToan_HotelFolioId] ON [fo].[ThanhToan] ([HotelFolioId]);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'fo.PhanBoThanhToan') AND name = N'IX_PhanBoThanhToan_ChargeFolioEntryId')
    CREATE INDEX [IX_PhanBoThanhToan_ChargeFolioEntryId] ON [fo].[PhanBoThanhToan] ([ChargeFolioEntryId]);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'fo.PhanBoThanhToan') AND name = N'IX_PhanBoThanhToan_HotelPaymentId')
    CREATE INDEX [IX_PhanBoThanhToan_HotelPaymentId] ON [fo].[PhanBoThanhToan] ([HotelPaymentId]);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'fo.LichSuThanhToan') AND name = N'IX_LichSuThanhToan_HotelPaymentId')
    CREATE INDEX [IX_LichSuThanhToan_HotelPaymentId] ON [fo].[LichSuThanhToan] ([HotelPaymentId]);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'fo.LuuTruKhachSan') AND name = N'IX_LuuTruKhachSan_GuestId')
    CREATE INDEX [IX_LuuTruKhachSan_GuestId] ON [fo].[LuuTruKhachSan] ([GuestId]);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'fo.LuuTruKhachSan') AND name = N'IX_LuuTruKhachSan_RoomId')
    CREATE INDEX [IX_LuuTruKhachSan_RoomId] ON [fo].[LuuTruKhachSan] ([RoomId]);

/* Lost & Found uses dbo tables in the current model; fo.DoThatLac is legacy. */
IF OBJECT_ID(N'dbo.LostFoundItems', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[LostFoundItems]
    (
        [LostFoundItemId] uniqueidentifier NOT NULL,
        [PropertyId] uniqueidentifier NOT NULL,
        [ItemNumber] nvarchar(50) NOT NULL,
        [ItemName] nvarchar(200) NOT NULL,
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
END;

IF OBJECT_ID(N'dbo.LostFoundHistories', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[LostFoundHistories]
    (
        [LostFoundHistoryId] uniqueidentifier NOT NULL,
        [LostFoundItemId] uniqueidentifier NOT NULL,
        [PropertyId] uniqueidentifier NOT NULL,
        [Action] nvarchar(500) NOT NULL,
        [OccurredAtUtc] datetime2 NOT NULL,
        CONSTRAINT [PK_LostFoundHistories] PRIMARY KEY ([LostFoundHistoryId])
    );
END;

/* Foreign keys are added only when absent, so the script is rerunnable. */
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_DonDep_PhongKhachSan_HotelRoomId')
    ALTER TABLE [fo].[DonDep] ADD CONSTRAINT [FK_DonDep_PhongKhachSan_HotelRoomId]
        FOREIGN KEY ([HotelRoomId]) REFERENCES [fo].[PhongKhachSan] ([HotelRoomId]);

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_PhongDoan_DatPhongDoan_GroupReservationId')
    ALTER TABLE [fo].[PhongDoan] ADD CONSTRAINT [FK_PhongDoan_DatPhongDoan_GroupReservationId]
        FOREIGN KEY ([GroupReservationId]) REFERENCES [fo].[DatPhongDoan] ([GroupReservationId]) ON DELETE CASCADE;

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_RoomInspection_DonDep_HousekeepingTaskId')
    ALTER TABLE [fo].[RoomInspection] ADD CONSTRAINT [FK_RoomInspection_DonDep_HousekeepingTaskId]
        FOREIGN KEY ([HousekeepingTaskId]) REFERENCES [fo].[DonDep] ([HousekeepingTaskId]) ON DELETE CASCADE;

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_KeyCardCredential_HoSoKhach_GuestId')
    ALTER TABLE [fo].[KeyCardCredential] ADD CONSTRAINT [FK_KeyCardCredential_HoSoKhach_GuestId]
        FOREIGN KEY ([GuestId]) REFERENCES [fo].[HoSoKhach] ([GuestProfileId]);

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_KeyCardCredential_LuuTruKhachSan_StayId')
    ALTER TABLE [fo].[KeyCardCredential] ADD CONSTRAINT [FK_KeyCardCredential_LuuTruKhachSan_StayId]
        FOREIGN KEY ([StayId]) REFERENCES [fo].[LuuTruKhachSan] ([HotelStayId]);

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_KeyCardCredential_PhongKhachSan_HotelRoomId')
    ALTER TABLE [fo].[KeyCardCredential] ADD CONSTRAINT [FK_KeyCardCredential_PhongKhachSan_HotelRoomId]
        FOREIGN KEY ([HotelRoomId]) REFERENCES [fo].[PhongKhachSan] ([HotelRoomId]);

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_MinibarSession_LuuTruKhachSan_StayId')
    ALTER TABLE [fo].[MinibarSession] ADD CONSTRAINT [FK_MinibarSession_LuuTruKhachSan_StayId]
        FOREIGN KEY ([StayId]) REFERENCES [fo].[LuuTruKhachSan] ([HotelStayId]);

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_MinibarSession_PhongKhachSan_HotelRoomId')
    ALTER TABLE [fo].[MinibarSession] ADD CONSTRAINT [FK_MinibarSession_PhongKhachSan_HotelRoomId]
        FOREIGN KEY ([HotelRoomId]) REFERENCES [fo].[PhongKhachSan] ([HotelRoomId]);

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_KeyCardEvent_KeyCardCredential_CredentialId')
    ALTER TABLE [fo].[KeyCardEvent] ADD CONSTRAINT [FK_KeyCardEvent_KeyCardCredential_CredentialId]
        FOREIGN KEY ([CredentialId]) REFERENCES [fo].[KeyCardCredential] ([CredentialId]) ON DELETE CASCADE;

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_MinibarConsumption_MinibarSession_SessionId')
    ALTER TABLE [fo].[MinibarConsumption] ADD CONSTRAINT [FK_MinibarConsumption_MinibarSession_SessionId]
        FOREIGN KEY ([SessionId]) REFERENCES [fo].[MinibarSession] ([SessionId]) ON DELETE CASCADE;

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_TrangThaiDonDep_PhongKhachSan_HotelRoomId')
    ALTER TABLE [fo].[TrangThaiDonDep] ADD CONSTRAINT [FK_TrangThaiDonDep_PhongKhachSan_HotelRoomId]
        FOREIGN KEY ([HotelRoomId]) REFERENCES [fo].[PhongKhachSan] ([HotelRoomId]);

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_LostFoundHistories_LostFoundItems_LostFoundItemId')
    ALTER TABLE [dbo].[LostFoundHistories] ADD CONSTRAINT [FK_LostFoundHistories_LostFoundItems_LostFoundItemId]
        FOREIGN KEY ([LostFoundItemId]) REFERENCES [dbo].[LostFoundItems] ([LostFoundItemId]) ON DELETE CASCADE;

/* Current-model indexes missing with the tables. */
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_DonDep_HotelRoomId' AND object_id = OBJECT_ID(N'fo.DonDep'))
    CREATE INDEX [IX_DonDep_HotelRoomId] ON [fo].[DonDep] ([HotelRoomId]);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_DonDep_PropertyId_ScheduledDate' AND object_id = OBJECT_ID(N'fo.DonDep'))
    CREATE INDEX [IX_DonDep_PropertyId_ScheduledDate] ON [fo].[DonDep] ([PropertyId], [ScheduledDate]);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_RoomInspection_HousekeepingTaskId' AND object_id = OBJECT_ID(N'fo.RoomInspection'))
    CREATE INDEX [IX_RoomInspection_HousekeepingTaskId] ON [fo].[RoomInspection] ([HousekeepingTaskId]);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UQ_RoomInspection_PropertyId_HousekeepingTaskId' AND object_id = OBJECT_ID(N'fo.RoomInspection'))
    CREATE UNIQUE INDEX [UQ_RoomInspection_PropertyId_HousekeepingTaskId] ON [fo].[RoomInspection] ([PropertyId], [HousekeepingTaskId]);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UQ_fo_PhongDoan_Doan_Phong' AND object_id = OBJECT_ID(N'fo.PhongDoan'))
    CREATE UNIQUE INDEX [UQ_fo_PhongDoan_Doan_Phong] ON [fo].[PhongDoan] ([GroupReservationId], [HotelRoomId]);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UQ_fo_KeyCardCredential_CardSerial' AND object_id = OBJECT_ID(N'fo.KeyCardCredential'))
    CREATE UNIQUE INDEX [UQ_fo_KeyCardCredential_CardSerial] ON [fo].[KeyCardCredential] ([CardSerial]);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_KeyCardCredential_GuestId' AND object_id = OBJECT_ID(N'fo.KeyCardCredential'))
    CREATE INDEX [IX_KeyCardCredential_GuestId] ON [fo].[KeyCardCredential] ([GuestId]);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_KeyCardCredential_HotelRoomId' AND object_id = OBJECT_ID(N'fo.KeyCardCredential'))
    CREATE INDEX [IX_KeyCardCredential_HotelRoomId] ON [fo].[KeyCardCredential] ([HotelRoomId]);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_KeyCardCredential_StayId' AND object_id = OBJECT_ID(N'fo.KeyCardCredential'))
    CREATE INDEX [IX_KeyCardCredential_StayId] ON [fo].[KeyCardCredential] ([StayId]);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_KeyCardCredential_PropertyId_StayId' AND object_id = OBJECT_ID(N'fo.KeyCardCredential'))
    CREATE INDEX [IX_KeyCardCredential_PropertyId_StayId] ON [fo].[KeyCardCredential] ([PropertyId], [StayId]);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_KeyCardEvent_CredentialId' AND object_id = OBJECT_ID(N'fo.KeyCardEvent'))
    CREATE INDEX [IX_KeyCardEvent_CredentialId] ON [fo].[KeyCardEvent] ([CredentialId]);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_KeyCardEvent_PropertyId_CredentialId_OccurredAtUtc' AND object_id = OBJECT_ID(N'fo.KeyCardEvent'))
    CREATE INDEX [IX_KeyCardEvent_PropertyId_CredentialId_OccurredAtUtc] ON [fo].[KeyCardEvent] ([PropertyId], [CredentialId], [OccurredAtUtc]);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_MinibarSession_HotelRoomId' AND object_id = OBJECT_ID(N'fo.MinibarSession'))
    CREATE INDEX [IX_MinibarSession_HotelRoomId] ON [fo].[MinibarSession] ([HotelRoomId]);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_MinibarSession_StayId' AND object_id = OBJECT_ID(N'fo.MinibarSession'))
    CREATE INDEX [IX_MinibarSession_StayId] ON [fo].[MinibarSession] ([StayId]);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_MinibarSession_PropertyId_StayId' AND object_id = OBJECT_ID(N'fo.MinibarSession'))
    CREATE UNIQUE INDEX [IX_MinibarSession_PropertyId_StayId] ON [fo].[MinibarSession] ([PropertyId], [StayId]);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_MinibarConsumption_SessionId' AND object_id = OBJECT_ID(N'fo.MinibarConsumption'))
    CREATE INDEX [IX_MinibarConsumption_SessionId] ON [fo].[MinibarConsumption] ([SessionId]);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_MinibarConsumption_PropertyId_SessionId' AND object_id = OBJECT_ID(N'fo.MinibarConsumption'))
    CREATE INDEX [IX_MinibarConsumption_PropertyId_SessionId] ON [fo].[MinibarConsumption] ([PropertyId], [SessionId]);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_LostFoundHistories_LostFoundItemId_OccurredAtUtc' AND object_id = OBJECT_ID(N'dbo.LostFoundHistories'))
    CREATE INDEX [IX_LostFoundHistories_LostFoundItemId_OccurredAtUtc] ON [dbo].[LostFoundHistories] ([LostFoundItemId], [OccurredAtUtc]);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_LostFoundItems_PropertyId_ItemNumber' AND object_id = OBJECT_ID(N'dbo.LostFoundItems'))
    CREATE INDEX [IX_LostFoundItems_PropertyId_ItemNumber] ON [dbo].[LostFoundItems] ([PropertyId], [ItemNumber]);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_LostFoundItems_PropertyId_Status_FoundDateTime' AND object_id = OBJECT_ID(N'dbo.LostFoundItems'))
    CREATE INDEX [IX_LostFoundItems_PropertyId_Status_FoundDateTime] ON [dbo].[LostFoundItems] ([PropertyId], [Status], [FoundDateTime]);

COMMIT TRANSACTION;

/* Intentionally no INSERT into __EFMigrationsHistory. Verify first. */
SELECT [SchemaName] = SCHEMA_NAME(t.schema_id), [TableName] = t.name
FROM sys.tables AS t
WHERE (t.schema_id = SCHEMA_ID(N'fo') AND t.name IN
       (N'DonDep', N'TrangThaiDonDep', N'PhongDoan', N'RoomInspection', N'KeyCardCredential', N'KeyCardEvent', N'MinibarSession', N'MinibarConsumption'))
   OR (t.schema_id = SCHEMA_ID(N'dbo') AND t.name IN
       (N'AppModules', N'SettingChangeLogs', N'SettingDefinitions', N'SettingValues', N'LostFoundItems', N'LostFoundHistories'))
ORDER BY [SchemaName], [TableName];
