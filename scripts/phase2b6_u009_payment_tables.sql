/*
   Phase 2B.6 / U-009 - Hotel payment persistence
   Scope: create the three payment tables only when none exists.
   This script is intentionally fail-closed for partial/unknown schemas.
*/
SET XACT_ABORT ON;
SET NOCOUNT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    IF SCHEMA_ID(N'fo') IS NULL
        THROW 51009, 'U009 requires schema fo.', 1;

    IF OBJECT_ID(N'fo.SoFolio', N'U') IS NULL
        THROW 51009, 'U009 requires fo.SoFolio.', 1;

    IF COL_LENGTH(N'fo.SoFolio', N'HotelFolioId') IS NULL
        THROW 51009, 'U009 requires fo.SoFolio.HotelFolioId.', 1;

    IF OBJECT_ID(N'fo.ButToanFolio', N'U') IS NULL
        THROW 51009, 'U009 requires fo.ButToanFolio.', 1;

    IF COL_LENGTH(N'fo.ButToanFolio', N'HotelFolioEntryId') IS NULL
        THROW 51009, 'U009 requires fo.ButToanFolio.HotelFolioEntryId.', 1;

    DECLARE @ExistingCount int =
        (SELECT COUNT(*)
         FROM (VALUES
             (OBJECT_ID(N'fo.ThanhToan', N'U')),
             (OBJECT_ID(N'fo.PhanBoThanhToan', N'U')),
             (OBJECT_ID(N'fo.LichSuThanhToan', N'U'))
         ) AS ExistingTables(ObjectId)
         WHERE ObjectId IS NOT NULL);

    IF @ExistingCount BETWEEN 1 AND 2
        THROW 51009, 'U009 found a partial payment schema; refusing to mutate it.', 1;

    IF @ExistingCount = 0
    BEGIN
        CREATE TABLE fo.ThanhToan (
            HotelPaymentId UNIQUEIDENTIFIER NOT NULL,
            PropertyId UNIQUEIDENTIFIER NOT NULL,
            HotelFolioId UNIQUEIDENTIFIER NOT NULL,
            FolioEntryId UNIQUEIDENTIFIER NULL,
            RefundEntryId UNIQUEIDENTIFIER NULL,
            PaymentNumber NVARCHAR(40) NOT NULL,
            PaymentMethod NVARCHAR(30) NOT NULL,
            Amount DECIMAL(18,2) NOT NULL,
            RefundedAmount DECIMAL(18,2) NOT NULL CONSTRAINT DF_fo_ThanhToan_RefundedAmount DEFAULT 0,
            Status NVARCHAR(30) NOT NULL,
            Currency NVARCHAR(10) NOT NULL CONSTRAINT DF_fo_ThanhToan_Currency DEFAULT N'VND',
            ExchangeRateToVnd DECIMAL(18,6) NOT NULL CONSTRAINT DF_fo_ThanhToan_ExchangeRate DEFAULT 1,
            ExternalReference NVARCHAR(200) NULL,
            AuthorizationCode NVARCHAR(100) NULL,
            Notes NVARCHAR(500) NULL,
            CreatedAtUtc DATETIME2 NOT NULL,
            CreatedBy UNIQUEIDENTIFIER NULL,
            VoidedAtUtc DATETIME2 NULL,
            VoidedBy UNIQUEIDENTIFIER NULL,
            VoidReason NVARCHAR(500) NULL,
            RefundedAtUtc DATETIME2 NULL,
            RefundedBy UNIQUEIDENTIFIER NULL,
            RefundReason NVARCHAR(500) NULL,
            RowVersion ROWVERSION NOT NULL,
            CONSTRAINT PK_fo_ThanhToan PRIMARY KEY (HotelPaymentId),
            CONSTRAINT UQ_fo_ThanhToan_SoThanhToan UNIQUE (PaymentNumber),
            CONSTRAINT FK_fo_ThanhToan_SoFolio FOREIGN KEY (HotelFolioId)
                REFERENCES fo.SoFolio(HotelFolioId)
        );

        CREATE TABLE fo.PhanBoThanhToan (
            HotelPaymentAllocationId UNIQUEIDENTIFIER NOT NULL,
            HotelPaymentId UNIQUEIDENTIFIER NOT NULL,
            PropertyId UNIQUEIDENTIFIER NOT NULL,
            HotelFolioId UNIQUEIDENTIFIER NOT NULL,
            ChargeFolioEntryId UNIQUEIDENTIFIER NOT NULL,
            ChargeDescription NVARCHAR(300) NOT NULL,
            Amount DECIMAL(18,2) NOT NULL,
            CreatedAtUtc DATETIME2 NOT NULL,
            CONSTRAINT PK_fo_PhanBoThanhToan PRIMARY KEY (HotelPaymentAllocationId),
            CONSTRAINT FK_fo_PhanBoThanhToan_ThanhToan FOREIGN KEY (HotelPaymentId)
                REFERENCES fo.ThanhToan(HotelPaymentId),
            CONSTRAINT FK_fo_PhanBoThanhToan_ButToanFolio FOREIGN KEY (ChargeFolioEntryId)
                REFERENCES fo.ButToanFolio(HotelFolioEntryId)
        );

        CREATE TABLE fo.LichSuThanhToan (
            PaymentHistoryId UNIQUEIDENTIFIER NOT NULL,
            HotelPaymentId UNIQUEIDENTIFIER NOT NULL,
            PropertyId UNIQUEIDENTIFIER NOT NULL,
            Action NVARCHAR(100) NOT NULL,
            Detail NVARCHAR(500) NULL,
            OccurredAtUtc DATETIME2 NOT NULL,
            ActorId UNIQUEIDENTIFIER NULL,
            CONSTRAINT PK_fo_LichSuThanhToan PRIMARY KEY (PaymentHistoryId),
            CONSTRAINT FK_fo_LichSuThanhToan_ThanhToan FOREIGN KEY (HotelPaymentId)
                REFERENCES fo.ThanhToan(HotelPaymentId)
        );
    END;

    COMMIT TRANSACTION;
    SELECT N'U009_OK' AS Result, @ExistingCount AS ExistingTableCount;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
