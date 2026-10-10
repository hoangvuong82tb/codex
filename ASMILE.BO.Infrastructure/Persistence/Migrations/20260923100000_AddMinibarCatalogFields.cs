using ASMILE.BO.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace ASMILE.BO.Infrastructure.Persistence.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260923100000_AddMinibarCatalogFields")]
public sealed class AddMinibarCatalogFields : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Additive and idempotent so existing installations keep all current
        // item data while gaining a real minibar catalog contract.
        migrationBuilder.Sql("""
            IF COL_LENGTH(N'dbo.Items', N'RetailPrice') IS NULL
                ALTER TABLE [dbo].[Items] ADD [RetailPrice] decimal(18,2) NOT NULL CONSTRAINT [DF_Items_RetailPrice] DEFAULT (0);
            IF COL_LENGTH(N'dbo.Items', N'VatRate') IS NULL
                ALTER TABLE [dbo].[Items] ADD [VatRate] decimal(9,4) NOT NULL CONSTRAINT [DF_Items_VatRate] DEFAULT (0);
            IF COL_LENGTH(N'dbo.Items', N'IsMinibarItem') IS NULL
                ALTER TABLE [dbo].[Items] ADD [IsMinibarItem] bit NOT NULL CONSTRAINT [DF_Items_IsMinibarItem] DEFAULT (0);
            IF COL_LENGTH(N'dbo.Items', N'MinibarCategory') IS NULL
                ALTER TABLE [dbo].[Items] ADD [MinibarCategory] nvarchar(100) NOT NULL CONSTRAINT [DF_Items_MinibarCategory] DEFAULT (N'');
            IF COL_LENGTH(N'dbo.Items', N'MinibarStandardQuantity') IS NULL
                ALTER TABLE [dbo].[Items] ADD [MinibarStandardQuantity] decimal(18,3) NOT NULL CONSTRAINT [DF_Items_MinibarStandardQuantity] DEFAULT (0);
            IF COL_LENGTH(N'dbo.Items', N'MinibarMinStock') IS NULL
                ALTER TABLE [dbo].[Items] ADD [MinibarMinStock] decimal(18,3) NOT NULL CONSTRAINT [DF_Items_MinibarMinStock] DEFAULT (0);
            IF COL_LENGTH(N'dbo.Items', N'MinibarMaxStock') IS NULL
                ALTER TABLE [dbo].[Items] ADD [MinibarMaxStock] decimal(18,3) NOT NULL CONSTRAINT [DF_Items_MinibarMaxStock] DEFAULT (0);
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Keep catalog data during rollback; fields are non-destructive and can
        // be removed only by an explicit data-migration decision.
    }
}
