SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    IF SCHEMA_ID(N'fo') IS NULL
        THROW 51000, 'Schema fo does not exist.', 1;

    IF OBJECT_ID(N'fo.DatPhong', N'U') IS NULL
        THROW 51001, 'Table fo.DatPhong does not exist.', 1;

    IF OBJECT_ID(N'fo.LuuTruKhachSan', N'U') IS NULL
        THROW 51002, 'Table fo.LuuTruKhachSan does not exist.', 1;

    IF OBJECT_ID(N'fo.HoSoKhach', N'U') IS NULL
        THROW 51003, 'Table fo.HoSoKhach does not exist.', 1;

    IF COL_LENGTH(N'fo.DatPhong', N'GuestId') IS NULL
        ALTER TABLE [fo].[DatPhong] ADD [GuestId] uniqueidentifier NULL;

    IF COL_LENGTH(N'fo.LuuTruKhachSan', N'GuestId') IS NULL
        ALTER TABLE [fo].[LuuTruKhachSan] ADD [GuestId] uniqueidentifier NULL;

    /*
       The current pilot rows were seeded with placeholder GuestId values while
       the canonical guest table is still empty. Preserve every reservation and
       stay (including all snapshot fields), but clear only those demonstrably
       orphaned placeholder links before adding the FK. If a real guest row is
       already present, fail closed instead of silently rewriting a mixed dataset.
    */
    DECLARE @guestProfileCount int = (SELECT COUNT(*) FROM [fo].[HoSoKhach]);
    DECLARE @reservationOrphans int = (
        SELECT COUNT(*)
        FROM [fo].[DatPhong] AS r
        WHERE r.GuestId IS NOT NULL
          AND NOT EXISTS (SELECT 1 FROM [fo].[HoSoKhach] AS g WHERE g.GuestProfileId = r.GuestId)
    );
    DECLARE @stayOrphans int = (
        SELECT COUNT(*)
        FROM [fo].[LuuTruKhachSan] AS s
        WHERE s.GuestId IS NOT NULL
          AND NOT EXISTS (SELECT 1 FROM [fo].[HoSoKhach] AS g WHERE g.GuestProfileId = s.GuestId)
    );

    IF (@guestProfileCount = 0 AND (@reservationOrphans > 0 OR @stayOrphans > 0))
    BEGIN
        UPDATE [fo].[DatPhong]
        SET GuestId = NULL
        WHERE GuestId IS NOT NULL;

        UPDATE [fo].[LuuTruKhachSan]
        SET GuestId = NULL
        WHERE GuestId IS NOT NULL;
    END
    ELSE IF (@reservationOrphans > 0 OR @stayOrphans > 0)
        THROW 51008, 'Orphan GuestId values exist alongside canonical guest profiles; manual reconciliation is required.', 1;

    DECLARE @fkName sysname;
    DECLARE @refSchema sysname;
    DECLARE @refTable sysname;
    DECLARE @refColumn sysname;

    SELECT TOP (1)
        @fkName = fk.name,
        @refSchema = OBJECT_SCHEMA_NAME(fk.referenced_object_id),
        @refTable = OBJECT_NAME(fk.referenced_object_id),
        @refColumn = COL_NAME(fkc.referenced_object_id, fkc.referenced_column_id)
    FROM sys.foreign_keys AS fk
    INNER JOIN sys.foreign_key_columns AS fkc ON fkc.constraint_object_id = fk.object_id
    WHERE fk.parent_object_id = OBJECT_ID(N'fo.DatPhong')
      AND COL_NAME(fkc.parent_object_id, fkc.parent_column_id) = N'GuestId';

    IF @fkName IS NOT NULL
    BEGIN
        IF @refSchema <> N'fo' OR @refTable <> N'HoSoKhach' OR @refColumn <> N'GuestProfileId'
            THROW 51004, 'fo.DatPhong.GuestId has an unexpected foreign key target.', 1;
    END
    ELSE
    BEGIN
        ALTER TABLE [fo].[DatPhong]
            ADD CONSTRAINT [FK_fo_DatPhong_HoSoKhach]
            FOREIGN KEY ([GuestId]) REFERENCES [fo].[HoSoKhach] ([GuestProfileId])
            ON DELETE NO ACTION;
    END;

    SET @fkName = NULL;
    SET @refSchema = NULL;
    SET @refTable = NULL;
    SET @refColumn = NULL;

    SELECT TOP (1)
        @fkName = fk.name,
        @refSchema = OBJECT_SCHEMA_NAME(fk.referenced_object_id),
        @refTable = OBJECT_NAME(fk.referenced_object_id),
        @refColumn = COL_NAME(fkc.referenced_object_id, fkc.referenced_column_id)
    FROM sys.foreign_keys AS fk
    INNER JOIN sys.foreign_key_columns AS fkc ON fkc.constraint_object_id = fk.object_id
    WHERE fk.parent_object_id = OBJECT_ID(N'fo.LuuTruKhachSan')
      AND COL_NAME(fkc.parent_object_id, fkc.parent_column_id) = N'GuestId';

    IF @fkName IS NOT NULL
    BEGIN
        IF @refSchema <> N'fo' OR @refTable <> N'HoSoKhach' OR @refColumn <> N'GuestProfileId'
            THROW 51005, 'fo.LuuTruKhachSan.GuestId has an unexpected foreign key target.', 1;
    END
    ELSE
    BEGIN
        ALTER TABLE [fo].[LuuTruKhachSan]
            ADD CONSTRAINT [FK_fo_LuuTruKhachSan_HoSoKhach]
            FOREIGN KEY ([GuestId]) REFERENCES [fo].[HoSoKhach] ([GuestProfileId])
            ON DELETE NO ACTION;
    END;

    IF EXISTS (
        SELECT 1
        FROM [fo].[DatPhong] AS r
        WHERE r.GuestId IS NOT NULL
          AND NOT EXISTS (SELECT 1 FROM [fo].[HoSoKhach] AS g WHERE g.GuestProfileId = r.GuestId)
    )
        THROW 51006, 'Orphan GuestId values exist in fo.DatPhong.', 1;

    IF EXISTS (
        SELECT 1
        FROM [fo].[LuuTruKhachSan] AS s
        WHERE s.GuestId IS NOT NULL
          AND NOT EXISTS (SELECT 1 FROM [fo].[HoSoKhach] AS g WHERE g.GuestProfileId = s.GuestId)
    )
        THROW 51007, 'Orphan GuestId values exist in fo.LuuTruKhachSan.', 1;

    COMMIT TRANSACTION;

    SELECT
        N'U006_U007_POST' AS CheckName,
        (SELECT COUNT(*) FROM sys.foreign_keys WHERE name = N'FK_fo_DatPhong_HoSoKhach') AS ReservationGuestFk,
        (SELECT COUNT(*) FROM sys.foreign_keys WHERE name = N'FK_fo_LuuTruKhachSan_HoSoKhach') AS StayGuestFk,
        (SELECT COUNT(*) FROM [fo].[DatPhong] WHERE GuestId IS NOT NULL) AS ReservationGuestLinks,
        (SELECT COUNT(*) FROM [fo].[LuuTruKhachSan] WHERE GuestId IS NOT NULL) AS StayGuestLinks;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0
        ROLLBACK TRANSACTION;
    THROW;
END CATCH;
