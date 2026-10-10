/*
  ASMILE-BO U-008: add the room housekeeping status extension.
  Narrow, idempotent, transactional DDL; does not touch EF migration history.
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
BEGIN TRANSACTION;

IF COL_LENGTH(N'fo.PhongKhachSan', N'HousekeepingStatus') IS NULL
    ALTER TABLE fo.PhongKhachSan ADD HousekeepingStatus NVARCHAR(30) NULL;

COMMIT TRANSACTION;
