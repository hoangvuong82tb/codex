using ASMILE.BO.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ASMILE.BO.Infrastructure.Persistence.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260909120000_RepairMenuCategoryParentCategorySchema")]
public partial class RepairMenuCategoryParentCategorySchema : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            IF COL_LENGTH('dbo.MenuCategories', 'ParentCategoryId') IS NULL
                ALTER TABLE [dbo].[MenuCategories] ADD [ParentCategoryId] uniqueidentifier NULL;
            """);

        migrationBuilder.Sql(
            """
            IF NOT EXISTS (
                SELECT 1
                FROM sys.indexes
                WHERE name = 'IX_MenuCategories_ParentCategoryId'
                  AND object_id = OBJECT_ID('[dbo].[MenuCategories]')
            )
                CREATE INDEX [IX_MenuCategories_ParentCategoryId]
                    ON [dbo].[MenuCategories]([ParentCategoryId]);
            """);

        migrationBuilder.Sql(
            """
            IF NOT EXISTS (
                SELECT 1
                FROM sys.foreign_keys
                WHERE name = 'FK_MenuCategories_MenuCategories_ParentCategoryId'
            )
                ALTER TABLE [dbo].[MenuCategories]
                ADD CONSTRAINT [FK_MenuCategories_MenuCategories_ParentCategoryId]
                FOREIGN KEY ([ParentCategoryId]) REFERENCES [dbo].[MenuCategories]([Id]);
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
                ALTER TABLE [dbo].[MenuCategories]
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
                DROP INDEX [IX_MenuCategories_ParentCategoryId]
                    ON [dbo].[MenuCategories];
            """);

        migrationBuilder.Sql(
            """
            IF COL_LENGTH('dbo.MenuCategories', 'ParentCategoryId') IS NOT NULL
                ALTER TABLE [dbo].[MenuCategories] DROP COLUMN [ParentCategoryId];
            """);
    }
}
