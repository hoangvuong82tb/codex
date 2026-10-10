using ASMILE.BO.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace ASMILE.BO.Infrastructure.Persistence.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260920220000_RepairNightAuditStepCounters")]
public sealed class RepairNightAuditStepCounters : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Repair pre-existing Night Audit tables as well as clean installations.
        // The earlier table-creation migration skipped these columns whenever
        // [fo].[KiemDem] already existed, leaving EF writes to fail at runtime.
        migrationBuilder.Sql("""
            IF OBJECT_ID(N'[fo].[KiemDem]', N'U') IS NOT NULL
            BEGIN
                IF COL_LENGTH(N'fo.KiemDem', N'RoomsCharged') IS NULL
                    ALTER TABLE [fo].[KiemDem] ADD [RoomsCharged] int NOT NULL CONSTRAINT [DF_fo_KiemDem_RoomsCharged] DEFAULT (0);
                IF COL_LENGTH(N'fo.KiemDem', N'RoomRevenue') IS NULL
                    ALTER TABLE [fo].[KiemDem] ADD [RoomRevenue] decimal(18,2) NOT NULL CONSTRAINT [DF_fo_KiemDem_RoomRevenue] DEFAULT (0);
                IF COL_LENGTH(N'fo.KiemDem', N'TaxPosted') IS NULL
                    ALTER TABLE [fo].[KiemDem] ADD [TaxPosted] decimal(18,2) NOT NULL CONSTRAINT [DF_fo_KiemDem_TaxPosted] DEFAULT (0);
                IF COL_LENGTH(N'fo.KiemDem', N'ServiceChargePosted') IS NULL
                    ALTER TABLE [fo].[KiemDem] ADD [ServiceChargePosted] decimal(18,2) NOT NULL CONSTRAINT [DF_fo_KiemDem_ServiceChargePosted] DEFAULT (0);
                IF COL_LENGTH(N'fo.KiemDem', N'FoliosClosed') IS NULL
                    ALTER TABLE [fo].[KiemDem] ADD [FoliosClosed] int NOT NULL CONSTRAINT [DF_fo_KiemDem_FoliosClosed] DEFAULT (0);
                IF COL_LENGTH(N'fo.KiemDem', N'StaysUpdated') IS NULL
                    ALTER TABLE [fo].[KiemDem] ADD [StaysUpdated] int NOT NULL CONSTRAINT [DF_fo_KiemDem_StaysUpdated] DEFAULT (0);
                IF COL_LENGTH(N'fo.KiemDem', N'ValidationFailures') IS NULL
                    ALTER TABLE [fo].[KiemDem] ADD [ValidationFailures] int NOT NULL CONSTRAINT [DF_fo_KiemDem_ValidationFailures] DEFAULT (0);
            END;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Keep the repair additive so rollback cannot discard audit progress.
    }
}
