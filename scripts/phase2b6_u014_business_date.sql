/*
  Phase 2B.6 / U-014
  Canonical business date owned by the existing Night Audit domain model.
  Safe adaptation: fo.NgayKinhDoanh matches BusinessDateRecord; no duplicate fo.BusinessDate table.
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    IF SCHEMA_ID(N'fo') IS NULL
        EXEC(N'CREATE SCHEMA [fo]');

    IF OBJECT_ID(N'[fo].[NgayKinhDoanh]', N'U') IS NULL
    BEGIN
        CREATE TABLE [fo].[NgayKinhDoanh]
        (
            [BusinessDateId] UNIQUEIDENTIFIER NOT NULL
                CONSTRAINT [DF_fo_NgayKinhDoanh_Id] DEFAULT NEWSEQUENTIALID(),
            [PropertyId] UNIQUEIDENTIFIER NOT NULL,
            [BusinessDate] DATE NOT NULL,
            [Status] NVARCHAR(20) NOT NULL
                CONSTRAINT [DF_fo_NgayKinhDoanh_Status] DEFAULT N'Open',
            [OpenedAtUtc] DATETIME2 NOT NULL
                CONSTRAINT [DF_fo_NgayKinhDoanh_OpenedAtUtc] DEFAULT SYSUTCDATETIME(),
            [OpenedBy] UNIQUEIDENTIFIER NULL,
            [ClosedAtUtc] DATETIME2 NULL,
            [ClosedBy] UNIQUEIDENTIFIER NULL,
            [NightAuditId] UNIQUEIDENTIFIER NULL,
            [RowVersion] ROWVERSION NOT NULL,
            CONSTRAINT [PK_fo_NgayKinhDoanh] PRIMARY KEY ([BusinessDateId]),
            CONSTRAINT [CK_fo_NgayKinhDoanh_Status]
                CHECK ([Status] IN (N'Open', N'Closing', N'Closed', N'Archived'))
        );
    END;

    IF NOT EXISTS
    (
        SELECT 1
        FROM sys.indexes
        WHERE object_id = OBJECT_ID(N'[fo].[NgayKinhDoanh]')
          AND name = N'UQ_fo_NgayKinhDoanh_CoSo_Ngay'
    )
    BEGIN
        CREATE UNIQUE INDEX [UQ_fo_NgayKinhDoanh_CoSo_Ngay]
            ON [fo].[NgayKinhDoanh] ([PropertyId], [BusinessDate]);
    END;

    COMMIT TRANSACTION;

    SELECT N'U014_POST' AS [Check],
           COUNT(*) AS [BusinessDateRows]
    FROM [fo].[NgayKinhDoanh];
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
