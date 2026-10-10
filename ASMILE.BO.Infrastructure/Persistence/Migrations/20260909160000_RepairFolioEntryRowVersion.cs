using ASMILE.BO.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ASMILE.BO.Infrastructure.Persistence.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260909160000_RepairFolioEntryRowVersion")]
public partial class RepairFolioEntryRowVersion : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            IF OBJECT_ID(N'[fo].[ButToanFolio]', N'U') IS NOT NULL
               AND COL_LENGTH(N'fo.ButToanFolio', N'RowVersion') IS NULL
                ALTER TABLE [fo].[ButToanFolio] ADD [RowVersion] rowversion;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            IF OBJECT_ID(N'[fo].[ButToanFolio]', N'U') IS NOT NULL
               AND COL_LENGTH(N'fo.ButToanFolio', N'RowVersion') IS NOT NULL
                ALTER TABLE [fo].[ButToanFolio] DROP COLUMN [RowVersion];
            """);
    }
}
