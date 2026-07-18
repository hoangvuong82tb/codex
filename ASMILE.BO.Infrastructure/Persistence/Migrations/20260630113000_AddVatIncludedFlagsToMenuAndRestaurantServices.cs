using Microsoft.EntityFrameworkCore.Migrations;

namespace ASMILE.BO.Infrastructure.Persistence.Migrations;

[Migration("20260630113000_AddVatIncludedFlagsToMenuAndRestaurantServices")]
public partial class AddVatIncludedFlagsToMenuAndRestaurantServices : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            IF COL_LENGTH('MenuItems', 'IsVatIncluded') IS NULL
                ALTER TABLE [MenuItems] ADD [IsVatIncluded] bit NOT NULL CONSTRAINT [DF_MenuItems_IsVatIncluded] DEFAULT 1;
            """);

        migrationBuilder.Sql(
            """
            IF COL_LENGTH('RestaurantExtraServices', 'IsVatIncluded') IS NULL
                ALTER TABLE [RestaurantExtraServices] ADD [IsVatIncluded] bit NOT NULL CONSTRAINT [DF_RestaurantExtraServices_IsVatIncluded] DEFAULT 1;
            """);

        migrationBuilder.Sql(
            """
            IF COL_LENGTH('AlacarteOrderLines', 'IsVatIncluded') IS NULL
                ALTER TABLE [AlacarteOrderLines] ADD [IsVatIncluded] bit NOT NULL CONSTRAINT [DF_AlacarteOrderLines_IsVatIncluded] DEFAULT 1;
            """);

        migrationBuilder.Sql(
            """
            IF COL_LENGTH('RestaurantOrderExtraServices', 'IsVatIncluded') IS NULL
                ALTER TABLE [RestaurantOrderExtraServices] ADD [IsVatIncluded] bit NOT NULL CONSTRAINT [DF_RestaurantOrderExtraServices_IsVatIncluded] DEFAULT 1;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            IF EXISTS (SELECT 1 FROM sys.default_constraints WHERE name = 'DF_RestaurantOrderExtraServices_IsVatIncluded')
                ALTER TABLE [RestaurantOrderExtraServices] DROP CONSTRAINT [DF_RestaurantOrderExtraServices_IsVatIncluded];
            IF COL_LENGTH('RestaurantOrderExtraServices', 'IsVatIncluded') IS NOT NULL
                ALTER TABLE [RestaurantOrderExtraServices] DROP COLUMN [IsVatIncluded];
            """);

        migrationBuilder.Sql(
            """
            IF EXISTS (SELECT 1 FROM sys.default_constraints WHERE name = 'DF_AlacarteOrderLines_IsVatIncluded')
                ALTER TABLE [AlacarteOrderLines] DROP CONSTRAINT [DF_AlacarteOrderLines_IsVatIncluded];
            IF COL_LENGTH('AlacarteOrderLines', 'IsVatIncluded') IS NOT NULL
                ALTER TABLE [AlacarteOrderLines] DROP COLUMN [IsVatIncluded];
            """);

        migrationBuilder.Sql(
            """
            IF EXISTS (SELECT 1 FROM sys.default_constraints WHERE name = 'DF_RestaurantExtraServices_IsVatIncluded')
                ALTER TABLE [RestaurantExtraServices] DROP CONSTRAINT [DF_RestaurantExtraServices_IsVatIncluded];
            IF COL_LENGTH('RestaurantExtraServices', 'IsVatIncluded') IS NOT NULL
                ALTER TABLE [RestaurantExtraServices] DROP COLUMN [IsVatIncluded];
            """);

        migrationBuilder.Sql(
            """
            IF EXISTS (SELECT 1 FROM sys.default_constraints WHERE name = 'DF_MenuItems_IsVatIncluded')
                ALTER TABLE [MenuItems] DROP CONSTRAINT [DF_MenuItems_IsVatIncluded];
            IF COL_LENGTH('MenuItems', 'IsVatIncluded') IS NOT NULL
                ALTER TABLE [MenuItems] DROP COLUMN [IsVatIncluded];
            """);
    }
}
