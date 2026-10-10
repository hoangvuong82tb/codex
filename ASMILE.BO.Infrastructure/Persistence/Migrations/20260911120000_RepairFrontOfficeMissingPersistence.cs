using ASMILE.BO.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ASMILE.BO.Infrastructure.Persistence.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260911120000_RepairFrontOfficeMissingPersistence")]
public partial class RepairFrontOfficeMissingPersistence : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // The model already contains these fields, but older databases can have
        // KeyCardCredential without the later door-lock metadata columns.
        migrationBuilder.Sql("""
            IF OBJECT_ID(N'[fo].[KeyCardCredential]', N'U') IS NOT NULL
            BEGIN
                IF COL_LENGTH(N'fo.KeyCardCredential', N'AccessLevel') IS NULL
                    ALTER TABLE [fo].[KeyCardCredential] ADD [AccessLevel] nvarchar(30) NOT NULL CONSTRAINT [DF_fo_KeyCardCredential_AccessLevel] DEFAULT N'Guest' WITH VALUES;
                IF COL_LENGTH(N'fo.KeyCardCredential', N'EncoderId') IS NULL
                    ALTER TABLE [fo].[KeyCardCredential] ADD [EncoderId] int NOT NULL CONSTRAINT [DF_fo_KeyCardCredential_EncoderId] DEFAULT 0 WITH VALUES;
                IF COL_LENGTH(N'fo.KeyCardCredential', N'IsMaster') IS NULL
                    ALTER TABLE [fo].[KeyCardCredential] ADD [IsMaster] bit NOT NULL CONSTRAINT [DF_fo_KeyCardCredential_IsMaster] DEFAULT 0 WITH VALUES;
            END;
            """);

        // CaLamViec is present in some deployed databases while its movement
        // table was skipped by an earlier partial migration.
        migrationBuilder.Sql("""
            IF OBJECT_ID(N'[fo].[CaLamViec]', N'U') IS NOT NULL
               AND OBJECT_ID(N'[fo].[BienDongTienMat]', N'U') IS NULL
            BEGIN
                CREATE TABLE [fo].[BienDongTienMat]
                (
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
                    CONSTRAINT [FK_BienDongTienMat_CaLamViec_CashierShiftId]
                        FOREIGN KEY ([CashierShiftId]) REFERENCES [fo].[CaLamViec] ([CashierShiftId])
                        ON DELETE NO ACTION
                );
                CREATE INDEX [IX_fo_BienDongTienMat_Ca_ThoiGian]
                    ON [fo].[BienDongTienMat] ([CashierShiftId], [CreatedAtUtc]);
            END;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Keep the repair reversible without dropping data that may have been
        // written after the repair was applied.
        migrationBuilder.Sql("""
            IF OBJECT_ID(N'[fo].[BienDongTienMat]', N'U') IS NOT NULL
            BEGIN
                DROP INDEX [IX_fo_BienDongTienMat_Ca_ThoiGian] ON [fo].[BienDongTienMat];
                ALTER TABLE [fo].[BienDongTienMat] DROP CONSTRAINT [FK_BienDongTienMat_CaLamViec_CashierShiftId];
                DROP TABLE [fo].[BienDongTienMat];
            END;
            """);
    }
}
