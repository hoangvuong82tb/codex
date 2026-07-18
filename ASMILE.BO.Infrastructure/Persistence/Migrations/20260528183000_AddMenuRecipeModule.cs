using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ASMILE.BO.Infrastructure.Persistence.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260528183000_AddMenuRecipeModule")]
    public partial class AddMenuRecipeModule : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                IF COL_LENGTH('MenuItems', 'HasRecipeManagement') IS NULL
                    ALTER TABLE [MenuItems] ADD [HasRecipeManagement] bit NOT NULL CONSTRAINT [DF_MenuItems_HasRecipeManagement] DEFAULT 1;
                """);

            migrationBuilder.Sql(
                """
                IF COL_LENGTH('MenuItems', 'Note') IS NULL
                    ALTER TABLE [MenuItems] ADD [Note] nvarchar(1000) NULL;
                """);

            migrationBuilder.Sql(
                """
                IF COL_LENGTH('MenuItemRecipeLines', 'ChildMenuItemId') IS NULL
                    ALTER TABLE [MenuItemRecipeLines] ADD [ChildMenuItemId] uniqueidentifier NULL;
                """);

            migrationBuilder.Sql(
                """
                IF COL_LENGTH('MenuItemRecipeLines', 'DefaultWarehouseId') IS NULL
                    ALTER TABLE [MenuItemRecipeLines] ADD [DefaultWarehouseId] int NULL;
                """);

            migrationBuilder.Sql(
                """
                IF COL_LENGTH('MenuItemRecipeLines', 'IsRequired') IS NULL
                    ALTER TABLE [MenuItemRecipeLines] ADD [IsRequired] bit NOT NULL CONSTRAINT [DF_MenuItemRecipeLines_IsRequired] DEFAULT 1;
                """);

            migrationBuilder.Sql(
                """
                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_MenuItems_IsActive' AND object_id = OBJECT_ID('MenuItems'))
                    CREATE INDEX [IX_MenuItems_IsActive] ON [MenuItems]([IsActive]);
                """);

            migrationBuilder.Sql(
                """
                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_MenuItemRecipeLines_ChildMenuItemId' AND object_id = OBJECT_ID('MenuItemRecipeLines'))
                    CREATE INDEX [IX_MenuItemRecipeLines_ChildMenuItemId] ON [MenuItemRecipeLines]([ChildMenuItemId]);
                """);

            migrationBuilder.Sql(
                """
                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_MenuItemRecipeLines_DefaultWarehouseId' AND object_id = OBJECT_ID('MenuItemRecipeLines'))
                    CREATE INDEX [IX_MenuItemRecipeLines_DefaultWarehouseId] ON [MenuItemRecipeLines]([DefaultWarehouseId]);
                """);

            migrationBuilder.Sql(
                """
                IF NOT EXISTS (
                    SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_MenuItemRecipeLines_MenuItems_ChildMenuItemId'
                )
                    ALTER TABLE [MenuItemRecipeLines] ADD CONSTRAINT [FK_MenuItemRecipeLines_MenuItems_ChildMenuItemId]
                        FOREIGN KEY ([ChildMenuItemId]) REFERENCES [MenuItems]([Id]) ON DELETE NO ACTION;
                """);

            migrationBuilder.Sql(
                """
                IF NOT EXISTS (
                    SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_MenuItemRecipeLines_Warehouses_DefaultWarehouseId'
                )
                    ALTER TABLE [MenuItemRecipeLines] ADD CONSTRAINT [FK_MenuItemRecipeLines_Warehouses_DefaultWarehouseId]
                        FOREIGN KEY ([DefaultWarehouseId]) REFERENCES [Warehouses]([Id]) ON DELETE SET NULL;
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MenuItemRecipeLines_MenuItems_ChildMenuItemId",
                table: "MenuItemRecipeLines");

            migrationBuilder.DropForeignKey(
                name: "FK_MenuItemRecipeLines_Warehouses_DefaultWarehouseId",
                table: "MenuItemRecipeLines");

            migrationBuilder.DropIndex(
                name: "IX_MenuItems_IsActive",
                table: "MenuItems");

            migrationBuilder.DropIndex(
                name: "IX_MenuItemRecipeLines_ChildMenuItemId",
                table: "MenuItemRecipeLines");

            migrationBuilder.DropIndex(
                name: "IX_MenuItemRecipeLines_DefaultWarehouseId",
                table: "MenuItemRecipeLines");

            migrationBuilder.DropIndex(
                name: "IX_MenuItemRecipeLines_ItemId",
                table: "MenuItemRecipeLines");

            migrationBuilder.DropColumn(
                name: "HasRecipeManagement",
                table: "MenuItems");

            migrationBuilder.DropColumn(
                name: "Note",
                table: "MenuItems");

            migrationBuilder.DropColumn(
                name: "ChildMenuItemId",
                table: "MenuItemRecipeLines");

            migrationBuilder.DropColumn(
                name: "DefaultWarehouseId",
                table: "MenuItemRecipeLines");

            migrationBuilder.DropColumn(
                name: "IsRequired",
                table: "MenuItemRecipeLines");
        }
    }
}
