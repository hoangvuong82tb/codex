using ASMILE.BO.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ASMILE.BO.Infrastructure.Persistence.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260914171000_AddHousekeepingScheduledDate")]
public partial class AddHousekeepingScheduledDate : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            IF OBJECT_ID(N'[fo].[DonDep]', N'U') IS NULL
                THROW 51001, 'HOUSEKEEPING_TABLE_MISSING', 1;

            IF COL_LENGTH(N'fo.DonDep', N'ScheduledDate') IS NULL
            BEGIN
                ALTER TABLE [fo].[DonDep]
                    ADD [ScheduledDate] datetime2 NOT NULL
                        CONSTRAINT [DF_fo_DonDep_ScheduledDate] DEFAULT ('19000101');

                EXEC(N'UPDATE [fo].[DonDep]
                    SET [ScheduledDate] = [CreatedAtUtc]
                    WHERE [ScheduledDate] = CONVERT(datetime2, ''19000101'');');
            END;

            IF NOT EXISTS
            (
                SELECT 1
                FROM sys.indexes
                WHERE object_id = OBJECT_ID(N'[fo].[DonDep]')
                  AND name = N'IX_fo_DonDep_CoSo_NgayLich'
            )
            BEGIN
                EXEC(N'CREATE INDEX [IX_fo_DonDep_CoSo_NgayLich]
                    ON [fo].[DonDep] ([PropertyId], [ScheduledDate]);');
            END;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            IF OBJECT_ID(N'[fo].[DonDep]', N'U') IS NOT NULL
            BEGIN
                IF EXISTS
                (
                    SELECT 1
                    FROM sys.indexes
                    WHERE object_id = OBJECT_ID(N'[fo].[DonDep]')
                      AND name = N'IX_fo_DonDep_CoSo_NgayLich'
                )
                    DROP INDEX [IX_fo_DonDep_CoSo_NgayLich] ON [fo].[DonDep];

                IF COL_LENGTH(N'fo.DonDep', N'ScheduledDate') IS NOT NULL
                    ALTER TABLE [fo].[DonDep] DROP CONSTRAINT [DF_fo_DonDep_ScheduledDate];

                IF COL_LENGTH(N'fo.DonDep', N'ScheduledDate') IS NOT NULL
                    ALTER TABLE [fo].[DonDep] DROP COLUMN [ScheduledDate];
            END;
            """);
    }
}
