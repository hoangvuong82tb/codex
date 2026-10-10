/*
    Controlled EF history repair for the three Hotel/Front Office migrations
    whose physical schema is already reconciled in ASMILE_BO.

    This script does not execute migration DDL. It only records migrations
    after the target schema and critical contracts have been checked.
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;

IF DB_NAME() <> N'ASMILE_BO'
    THROW 51100, 'Wrong database. Expected ASMILE_BO.', 1;

IF OBJECT_ID(N'dbo.__EFMigrationsHistory', N'U') IS NULL
    THROW 51101, 'EF migration history table is missing.', 1;

DECLARE @m1 nvarchar(200) = N'20260720032319_AddReservationEnterpriseFields';
DECLARE @m2 nvarchar(200) = N'20260721065248_AddRatePlanGroupReservationAndReservationGuaranteeDeposit';
DECLARE @m3 nvarchar(200) = N'20260814231130_AddHousekeepingMinibarKeyCardContracts';
DECLARE @existing int =
(
    SELECT COUNT(*)
    FROM dbo.__EFMigrationsHistory
    WHERE MigrationId IN (@m1, @m2, @m3)
);

IF @existing NOT IN (0, 3)
    THROW 51102, 'Migration history is partially repaired; stop and inspect manually.', 1;

/* Critical tables and contracts covered by the reconciliation. */
IF OBJECT_ID(N'fo.DatPhong', N'U') IS NULL
   OR OBJECT_ID(N'fo.LuuTruKhachSan', N'U') IS NULL
   OR OBJECT_ID(N'fo.DatPhongDoan', N'U') IS NULL
   OR OBJECT_ID(N'fo.KeHoachGia', N'U') IS NULL
   OR OBJECT_ID(N'fo.DonDep', N'U') IS NULL
   OR OBJECT_ID(N'fo.PhongDoan', N'U') IS NULL
   OR OBJECT_ID(N'fo.KeyCardCredential', N'U') IS NULL
   OR OBJECT_ID(N'fo.KeyCardEvent', N'U') IS NULL
   OR OBJECT_ID(N'fo.MinibarSession', N'U') IS NULL
   OR OBJECT_ID(N'fo.MinibarConsumption', N'U') IS NULL
    THROW 51103, 'Critical Hotel tables are missing.', 1;

IF COL_LENGTH(N'fo.DatPhong', N'CheckedInAtUtc') IS NULL
   OR COL_LENGTH(N'fo.DatPhong', N'IsVip') IS NULL
   OR COL_LENGTH(N'fo.LuuTruKhachSan', N'ExtensionCount') IS NULL
   OR COL_LENGTH(N'fo.LuuTruKhachSan', N'IsVipService') IS NULL
   OR COL_LENGTH(N'fo.DonDep', N'ScheduledDate') IS NULL
    THROW 51104, 'Critical Hotel columns are missing.', 1;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'fo.DatPhongDoan') AND name = N'UQ_fo_DatPhongDoan_CoSo_MaDoan')
   OR NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'fo.KeHoachGia') AND name = N'UQ_fo_KeHoachGia_CoSo_Ma')
   OR NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'fo.KeyCardCredential') AND name = N'UQ_fo_KeyCardCredential_CardSerial')
   OR NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'fo.MinibarSession') AND name = N'IX_MinibarSession_PropertyId_StayId')
    THROW 51105, 'Critical Hotel indexes are missing.', 1;

DECLARE @tablesXml xml =
(
    SELECT s.name AS [schema], t.name AS [table]
    FROM sys.tables AS t
    JOIN sys.schemas AS s ON s.schema_id = t.schema_id
    WHERE t.is_ms_shipped = 0
    ORDER BY s.name, t.name
    FOR XML PATH(N't'), TYPE
);
DECLARE @tables nvarchar(max) = @tablesXml.value(N'.', N'nvarchar(max)');

DECLARE @columnsXml xml =
(
    SELECT s.name AS [schema], t.name AS [table], c.column_id, c.name AS [column], ty.name AS [type],
           c.max_length, c.precision, c.scale, c.is_nullable
    FROM sys.columns AS c
    JOIN sys.tables AS t ON t.object_id = c.object_id
    JOIN sys.schemas AS s ON s.schema_id = t.schema_id
    JOIN sys.types AS ty ON ty.user_type_id = c.user_type_id
    ORDER BY s.name, t.name, c.column_id
    FOR XML PATH(N'c'), TYPE
);
DECLARE @columns nvarchar(max) = @columnsXml.value(N'.', N'nvarchar(max)');

DECLARE @indexesXml xml =
(
    SELECT s.name AS [schema], t.name AS [table], i.name AS [index], i.is_unique, i.filter_definition,
           ic.key_ordinal, c.name AS [column]
    FROM sys.indexes AS i
    JOIN sys.tables AS t ON t.object_id = i.object_id
    JOIN sys.schemas AS s ON s.schema_id = t.schema_id
    JOIN sys.index_columns AS ic ON ic.object_id = i.object_id AND ic.index_id = i.index_id
    JOIN sys.columns AS c ON c.object_id = ic.object_id AND c.column_id = ic.column_id
    WHERE i.is_primary_key = 0 AND ic.key_ordinal > 0
    ORDER BY s.name, t.name, i.name, ic.key_ordinal
    FOR XML PATH(N'i'), TYPE
);
DECLARE @indexes nvarchar(max) = @indexesXml.value(N'.', N'nvarchar(max)');

DECLARE @foreignKeysXml xml =
(
    SELECT s.name AS [schema], OBJECT_NAME(fk.parent_object_id) AS [table], fk.name,
           OBJECT_NAME(fk.referenced_object_id) AS [referenced_table]
    FROM sys.foreign_keys AS fk
    JOIN sys.schemas AS s ON s.schema_id = OBJECTPROPERTY(fk.parent_object_id, N'SchemaId')
    ORDER BY s.name, [table], fk.name
    FOR XML PATH(N'f'), TYPE
);
DECLARE @foreignKeys nvarchar(max) = @foreignKeysXml.value(N'.', N'nvarchar(max)');

DECLARE @fingerprint nvarchar(64) = CONVERT(nvarchar(64), HASHBYTES(
    N'SHA2_256', COALESCE(@tables, N'') + COALESCE(@columns, N'') + COALESCE(@indexes, N'') + COALESCE(@foreignKeys, N'')), 2);

BEGIN TRANSACTION;

IF @existing = 0
BEGIN
    INSERT INTO dbo.__EFMigrationsHistory (MigrationId, ProductVersion)
    VALUES
        (@m1, N'8.0.7'),
        (@m2, N'8.0.7'),
        (@m3, N'8.0.7');
END;

COMMIT TRANSACTION;

SELECT [HistoryRepair] = CASE WHEN @existing = 0 THEN N'Inserted' ELSE N'AlreadyPresent' END,
       [SchemaFingerprintSha256] = @fingerprint,
       [Migration1] = @m1,
       [Migration2] = @m2,
       [Migration3] = @m3;
