using ASMILE.BO.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ASMILE.BO.Infrastructure.Persistence.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260909130000_RepairVatIncludedFlagsSchema")]
public partial class RepairVatIncludedFlagsSchema : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        AddFlagIfMissing(migrationBuilder, "MenuItems", "DF_MenuItems_IsVatIncluded");
        AddFlagIfMissing(migrationBuilder, "RestaurantExtraServices", "DF_RestaurantExtraServices_IsVatIncluded");
        AddFlagIfMissing(migrationBuilder, "AlacarteOrderLines", "DF_AlacarteOrderLines_IsVatIncluded");
        AddFlagIfMissing(migrationBuilder, "RestaurantOrderExtraServices", "DF_RestaurantOrderExtraServices_IsVatIncluded");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        DropFlagIfPresent(migrationBuilder, "RestaurantOrderExtraServices", "DF_RestaurantOrderExtraServices_IsVatIncluded");
        DropFlagIfPresent(migrationBuilder, "AlacarteOrderLines", "DF_AlacarteOrderLines_IsVatIncluded");
        DropFlagIfPresent(migrationBuilder, "RestaurantExtraServices", "DF_RestaurantExtraServices_IsVatIncluded");
        DropFlagIfPresent(migrationBuilder, "MenuItems", "DF_MenuItems_IsVatIncluded");
    }

    private static void AddFlagIfMissing(MigrationBuilder migrationBuilder, string tableName, string defaultConstraintName)
    {
        migrationBuilder.Sql($"""
            IF OBJECT_ID(N'[dbo].[{tableName}]', N'U') IS NOT NULL
               AND COL_LENGTH(N'dbo.{tableName}', N'IsVatIncluded') IS NULL
                ALTER TABLE [dbo].[{tableName}]
                ADD [IsVatIncluded] bit NOT NULL
                    CONSTRAINT [{defaultConstraintName}] DEFAULT 1;
            """);
    }

    private static void DropFlagIfPresent(MigrationBuilder migrationBuilder, string tableName, string defaultConstraintName)
    {
        migrationBuilder.Sql($"""
            IF OBJECT_ID(N'[dbo].[{tableName}]', N'U') IS NOT NULL
            BEGIN
                IF EXISTS (
                    SELECT 1
                    FROM sys.default_constraints
                    WHERE name = N'{defaultConstraintName}'
                      AND parent_object_id = OBJECT_ID(N'[dbo].[{tableName}]')
                )
                    ALTER TABLE [dbo].[{tableName}]
                    DROP CONSTRAINT [{defaultConstraintName}];

                IF COL_LENGTH(N'dbo.{tableName}', N'IsVatIncluded') IS NOT NULL
                    ALTER TABLE [dbo].[{tableName}] DROP COLUMN [IsVatIncluded];
            END;
            """);
    }
}
