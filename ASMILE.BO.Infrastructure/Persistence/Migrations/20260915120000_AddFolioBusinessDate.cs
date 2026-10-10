using ASMILE.BO.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ASMILE.BO.Infrastructure.Persistence.Migrations;

/// <summary>
/// Repairs the folio ledger schema so Night Audit can group postings by the
/// canonical hotel business date. Existing rows remain valid because the
/// column is nullable and the reader falls back to CreatedAtUtc for legacy rows.
/// </summary>
[DbContext(typeof(AppDbContext))]
[Migration("20260915120000_AddFolioBusinessDate")]
public partial class AddFolioBusinessDate : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            IF OBJECT_ID(N'[fo].[ButToanFolio]', N'U') IS NOT NULL
               AND COL_LENGTH(N'fo.ButToanFolio', N'BusinessDate') IS NULL
                ALTER TABLE [fo].[ButToanFolio]
                ADD [BusinessDate] date NULL;
            """);

        migrationBuilder.Sql(
            """
            IF OBJECT_ID(N'[fo].[ButToanFolio]', N'U') IS NOT NULL
               AND COL_LENGTH(N'fo.ButToanFolio', N'BusinessDate') IS NOT NULL
               AND NOT EXISTS (
                    SELECT 1
                    FROM sys.indexes
                    WHERE name = N'IX_fo_ButToanFolio_Property_BusinessDate'
                      AND object_id = OBJECT_ID(N'[fo].[ButToanFolio]')
               )
                CREATE INDEX [IX_fo_ButToanFolio_Property_BusinessDate]
                    ON [fo].[ButToanFolio]([PropertyId], [BusinessDate]);
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            IF OBJECT_ID(N'[fo].[ButToanFolio]', N'U') IS NOT NULL
            BEGIN
                IF EXISTS (
                    SELECT 1
                    FROM sys.indexes
                    WHERE name = N'IX_fo_ButToanFolio_Property_BusinessDate'
                      AND object_id = OBJECT_ID(N'[fo].[ButToanFolio]')
                )
                    DROP INDEX [IX_fo_ButToanFolio_Property_BusinessDate]
                        ON [fo].[ButToanFolio];

                IF COL_LENGTH(N'fo.ButToanFolio', N'BusinessDate') IS NOT NULL
                    ALTER TABLE [fo].[ButToanFolio] DROP COLUMN [BusinessDate];
            END;
            """);
    }
}
