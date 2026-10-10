using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ASMILE.BO.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AlignCommercialModelSnapshot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // This migration closes a historical snapshot drift. Several of these
            // columns may already exist because the feature migrations are guarded
            // SQL migrations; keep this migration safe for both fresh and upgraded DBs.
            migrationBuilder.Sql("""
                IF COL_LENGTH(N'[fo].[YeuCauDichVu]', N'FirstResponseAtUtc') IS NULL
                    ALTER TABLE [fo].[YeuCauDichVu] ADD [FirstResponseAtUtc] datetime2 NULL;

                IF COL_LENGTH(N'dbo.Items', N'IsMinibarItem') IS NULL
                    ALTER TABLE [dbo].[Items] ADD [IsMinibarItem] bit NOT NULL CONSTRAINT [DF_Align_Items_IsMinibarItem] DEFAULT (0);
                IF COL_LENGTH(N'dbo.Items', N'MinibarCategory') IS NULL
                    ALTER TABLE [dbo].[Items] ADD [MinibarCategory] nvarchar(100) NOT NULL CONSTRAINT [DF_Align_Items_MinibarCategory] DEFAULT (N'');
                IF COL_LENGTH(N'dbo.Items', N'MinibarMaxStock') IS NULL
                    ALTER TABLE [dbo].[Items] ADD [MinibarMaxStock] decimal(18,3) NOT NULL CONSTRAINT [DF_Align_Items_MinibarMaxStock] DEFAULT (0);
                IF COL_LENGTH(N'dbo.Items', N'MinibarMinStock') IS NULL
                    ALTER TABLE [dbo].[Items] ADD [MinibarMinStock] decimal(18,3) NOT NULL CONSTRAINT [DF_Align_Items_MinibarMinStock] DEFAULT (0);
                IF COL_LENGTH(N'dbo.Items', N'MinibarStandardQuantity') IS NULL
                    ALTER TABLE [dbo].[Items] ADD [MinibarStandardQuantity] decimal(18,3) NOT NULL CONSTRAINT [DF_Align_Items_MinibarStandardQuantity] DEFAULT (0);
                IF COL_LENGTH(N'dbo.Items', N'RetailPrice') IS NULL
                    ALTER TABLE [dbo].[Items] ADD [RetailPrice] decimal(18,2) NOT NULL CONSTRAINT [DF_Align_Items_RetailPrice] DEFAULT (0);
                IF COL_LENGTH(N'dbo.Items', N'VatRate') IS NULL
                    ALTER TABLE [dbo].[Items] ADD [VatRate] decimal(9,4) NOT NULL CONSTRAINT [DF_Align_Items_VatRate] DEFAULT (0);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF COL_LENGTH(N'[fo].[YeuCauDichVu]', N'FirstResponseAtUtc') IS NOT NULL
                    ALTER TABLE [fo].[YeuCauDichVu] DROP COLUMN [FirstResponseAtUtc];
                """);
        }
    }
}
