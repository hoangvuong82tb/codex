using ASMILE.BO.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ASMILE.BO.Infrastructure.Persistence.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260909150000_RepairFolioIdempotencySchema")]
public partial class RepairFolioIdempotencySchema : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            IF OBJECT_ID(N'[fo].[ButToanFolio]', N'U') IS NOT NULL
               AND COL_LENGTH(N'fo.ButToanFolio', N'IdempotencyKey') IS NULL
                ALTER TABLE [fo].[ButToanFolio]
                ADD [IdempotencyKey] nvarchar(100) NULL;
            """);

        migrationBuilder.Sql(
            """
            IF OBJECT_ID(N'[fo].[ButToanFolio]', N'U') IS NOT NULL
               AND COL_LENGTH(N'fo.ButToanFolio', N'IdempotencyKey') IS NOT NULL
               AND NOT EXISTS (
                    SELECT 1
                    FROM sys.indexes
                    WHERE name = N'UQ_fo_ButToanFolio_Idempotency'
                      AND object_id = OBJECT_ID(N'[fo].[ButToanFolio]')
               )
                CREATE UNIQUE INDEX [UQ_fo_ButToanFolio_Idempotency]
                    ON [fo].[ButToanFolio]([IdempotencyKey])
                    WHERE [IdempotencyKey] IS NOT NULL;
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
                    WHERE name = N'UQ_fo_ButToanFolio_Idempotency'
                      AND object_id = OBJECT_ID(N'[fo].[ButToanFolio]')
                )
                    DROP INDEX [UQ_fo_ButToanFolio_Idempotency]
                        ON [fo].[ButToanFolio];

                IF COL_LENGTH(N'fo.ButToanFolio', N'IdempotencyKey') IS NOT NULL
                    ALTER TABLE [fo].[ButToanFolio] DROP COLUMN [IdempotencyKey];
            END;
            """);
    }
}
