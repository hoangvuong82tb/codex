using Microsoft.EntityFrameworkCore.Migrations;

namespace ASMILE.BO.Infrastructure.Persistence.Migrations;

[Migration("20260629114500_AddParentCategoryToMenuCategory")]
public partial class AddParentCategoryToMenuCategory : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            IF COL_LENGTH('MenuCategories', 'ParentCategoryId') IS NULL
                ALTER TABLE [MenuCategories] ADD [ParentCategoryId] uniqueidentifier NULL;
            """);

        migrationBuilder.Sql(
            """
            IF NOT EXISTS (
                SELECT 1
                FROM sys.indexes
                WHERE name = 'IX_MenuCategories_ParentCategoryId'
                  AND object_id = OBJECT_ID('[dbo].[MenuCategories]')
            )
                CREATE INDEX [IX_MenuCategories_ParentCategoryId] ON [MenuCategories]([ParentCategoryId]);
            """);

        migrationBuilder.Sql(
            """
            IF NOT EXISTS (
                SELECT 1
                FROM sys.foreign_keys
                WHERE name = 'FK_MenuCategories_MenuCategories_ParentCategoryId'
            )
                ALTER TABLE [MenuCategories]
                ADD CONSTRAINT [FK_MenuCategories_MenuCategories_ParentCategoryId]
                FOREIGN KEY ([ParentCategoryId]) REFERENCES [MenuCategories]([Id]);
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            IF EXISTS (
                SELECT 1
                FROM sys.foreign_keys
                WHERE name = 'FK_MenuCategories_MenuCategories_ParentCategoryId'
            )
                ALTER TABLE [MenuCategories]
                DROP CONSTRAINT [FK_MenuCategories_MenuCategories_ParentCategoryId];
            """);

        migrationBuilder.Sql(
            """
            IF EXISTS (
                SELECT 1
                FROM sys.indexes
                WHERE name = 'IX_MenuCategories_ParentCategoryId'
                  AND object_id = OBJECT_ID('[dbo].[MenuCategories]')
            )
                DROP INDEX [IX_MenuCategories_ParentCategoryId] ON [MenuCategories];
            """);

        migrationBuilder.Sql(
            """
            IF COL_LENGTH('MenuCategories', 'ParentCategoryId') IS NOT NULL
                ALTER TABLE [MenuCategories] DROP COLUMN [ParentCategoryId];
            """);
    }
}
