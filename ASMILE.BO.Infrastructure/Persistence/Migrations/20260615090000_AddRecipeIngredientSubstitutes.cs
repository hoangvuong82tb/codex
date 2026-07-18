using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ASMILE.BO.Infrastructure.Persistence.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260615090000_AddRecipeIngredientSubstitutes")]
    public partial class AddRecipeIngredientSubstitutes : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                IF OBJECT_ID(N'[RecipeIngredientSubstitutes]', N'U') IS NULL
                BEGIN
                    CREATE TABLE [RecipeIngredientSubstitutes] (
                        [Id] uniqueidentifier NOT NULL,
                        [CompanyId] uniqueidentifier NULL,
                        [BranchId] uniqueidentifier NULL,
                        [RecipeLineId] uniqueidentifier NOT NULL,
                        [InventoryItemId] int NOT NULL,
                        [SubstituteInventoryItemId] int NOT NULL,
                        [ConversionRate] decimal(18,4) NOT NULL,
                        [Priority] int NOT NULL,
                        [AutoReplace] bit NOT NULL,
                        [Notes] nvarchar(500) NULL,
                        [CreatedAt] datetime2 NOT NULL,
                        [CreatedBy] nvarchar(120) NULL,
                        [ModifiedAt] datetime2 NULL,
                        [ModifiedBy] nvarchar(120) NULL,
                        [IsDeleted] bit NOT NULL,
                        CONSTRAINT [PK_RecipeIngredientSubstitutes] PRIMARY KEY ([Id]),
                        CONSTRAINT [FK_RecipeIngredientSubstitutes_MenuItemRecipeLines_RecipeLineId]
                            FOREIGN KEY ([RecipeLineId]) REFERENCES [MenuItemRecipeLines]([Id]) ON DELETE CASCADE,
                        CONSTRAINT [FK_RecipeIngredientSubstitutes_Items_InventoryItemId]
                            FOREIGN KEY ([InventoryItemId]) REFERENCES [Items]([Id]) ON DELETE NO ACTION,
                        CONSTRAINT [FK_RecipeIngredientSubstitutes_Items_SubstituteInventoryItemId]
                            FOREIGN KEY ([SubstituteInventoryItemId]) REFERENCES [Items]([Id]) ON DELETE NO ACTION
                    );
                END
                """);

            migrationBuilder.Sql(
                """
                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_RecipeIngredientSubstitutes_RecipeLineId' AND object_id = OBJECT_ID('RecipeIngredientSubstitutes'))
                    CREATE INDEX [IX_RecipeIngredientSubstitutes_RecipeLineId] ON [RecipeIngredientSubstitutes]([RecipeLineId]);
                """);

            migrationBuilder.Sql(
                """
                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_RecipeIngredientSubstitutes_InventoryItemId' AND object_id = OBJECT_ID('RecipeIngredientSubstitutes'))
                    CREATE INDEX [IX_RecipeIngredientSubstitutes_InventoryItemId] ON [RecipeIngredientSubstitutes]([InventoryItemId]);
                """);

            migrationBuilder.Sql(
                """
                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_RecipeIngredientSubstitutes_SubstituteInventoryItemId' AND object_id = OBJECT_ID('RecipeIngredientSubstitutes'))
                    CREATE INDEX [IX_RecipeIngredientSubstitutes_SubstituteInventoryItemId] ON [RecipeIngredientSubstitutes]([SubstituteInventoryItemId]);
                """);

            migrationBuilder.Sql(
                """
                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_RecipeIngredientSubstitutes_RecipeLine_Substitute_IsDeleted' AND object_id = OBJECT_ID('RecipeIngredientSubstitutes'))
                    CREATE INDEX [IX_RecipeIngredientSubstitutes_RecipeLine_Substitute_IsDeleted] ON [RecipeIngredientSubstitutes]([RecipeLineId], [SubstituteInventoryItemId], [IsDeleted]);
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                IF OBJECT_ID(N'[RecipeIngredientSubstitutes]', N'U') IS NOT NULL
                    DROP TABLE [RecipeIngredientSubstitutes];
                """);
        }
    }
}
