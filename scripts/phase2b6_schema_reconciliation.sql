/*
  ASMILE-BO Phase 2B.6 schema reconciliation.
  Scope is deliberately limited to the approved U-001/U-002 changes.
  Idempotent and transactional; does not touch EF migration history.
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
BEGIN TRANSACTION;

IF COL_LENGTH(N'fo.DatPhong', N'GuaranteeType') IS NULL
    ALTER TABLE fo.DatPhong ADD GuaranteeType NVARCHAR(40) NULL;
IF COL_LENGTH(N'fo.DatPhong', N'DepositStatus') IS NULL
    ALTER TABLE fo.DatPhong ADD DepositStatus NVARCHAR(30) NULL;
IF COL_LENGTH(N'fo.DatPhong', N'DepositAmount') IS NULL
    ALTER TABLE fo.DatPhong ADD DepositAmount DECIMAL(18,2) NULL;

IF COL_LENGTH(N'fo.ButToanFolio', N'RowVersion') IS NULL
    ALTER TABLE fo.ButToanFolio ADD RowVersion ROWVERSION NOT NULL;
IF COL_LENGTH(N'fo.ButToanFolio', N'IdempotencyKey') IS NULL
    ALTER TABLE fo.ButToanFolio ADD IdempotencyKey NVARCHAR(100) NULL;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'fo.ButToanFolio') AND name = N'UQ_fo_ButToanFolio_Idempotency')
    EXEC(N'CREATE UNIQUE INDEX UQ_fo_ButToanFolio_Idempotency ON fo.ButToanFolio(IdempotencyKey) WHERE IdempotencyKey IS NOT NULL;');

COMMIT TRANSACTION;
