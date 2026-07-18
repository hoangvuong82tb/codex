using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ASMILE.BO.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddBreakfastCafePosTablesAndKitchenTickets : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                IF COL_LENGTH('MenuItems', 'HasRecipeManagement') IS NULL
                    ALTER TABLE [MenuItems] ADD [HasRecipeManagement] bit NOT NULL CONSTRAINT [DF_MenuItems_HasRecipeManagement] DEFAULT CAST(0 AS bit);
                IF COL_LENGTH('MenuItems', 'Note') IS NULL
                    ALTER TABLE [MenuItems] ADD [Note] nvarchar(1000) NULL;
                IF COL_LENGTH('MenuItems', 'PreparationArea') IS NULL
                    ALTER TABLE [MenuItems] ADD [PreparationArea] nvarchar(100) NULL;
                IF COL_LENGTH('MenuItemRecipeLines', 'ChildMenuItemId') IS NULL
                    ALTER TABLE [MenuItemRecipeLines] ADD [ChildMenuItemId] uniqueidentifier NULL;
                IF COL_LENGTH('MenuItemRecipeLines', 'DefaultWarehouseId') IS NULL
                    ALTER TABLE [MenuItemRecipeLines] ADD [DefaultWarehouseId] int NULL;
                IF COL_LENGTH('MenuItemRecipeLines', 'IsRequired') IS NULL
                    ALTER TABLE [MenuItemRecipeLines] ADD [IsRequired] bit NOT NULL CONSTRAINT [DF_MenuItemRecipeLines_IsRequired] DEFAULT CAST(0 AS bit);
                """);

            migrationBuilder.AddColumn<string>(
                name: "AreaName",
                table: "BreakfastCafeOrders",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "KitchenSentAt",
                table: "BreakfastCafeOrders",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OrderType",
                table: "BreakfastCafeOrders",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "PosTableId",
                table: "BreakfastCafeOrders",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ServiceChargeAmount",
                table: "BreakfastCafeOrders",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "ServiceStatus",
                table: "BreakfastCafeOrders",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TableCode",
                table: "BreakfastCafeOrders",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TableName",
                table: "BreakfastCafeOrders",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "KitchenStatus",
                table: "BreakfastCafeOrderLines",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "BreakfastCafeKitchenTickets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BreakfastCafeOrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TicketNumber = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    Station = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    SentAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SentBy = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BreakfastCafeKitchenTickets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BreakfastCafeKitchenTickets_BreakfastCafeOrders_BreakfastCafeOrderId",
                        column: x => x.BreakfastCafeOrderId,
                        principalTable: "BreakfastCafeOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PosTables",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AreaCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    AreaName = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    TableCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    TableName = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    OrderType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    SeatCount = table.Column<int>(type: "int", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PosTables", x => x.Id);
                });

            migrationBuilder.Sql(
                """
                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_MenuItems_IsActive' AND object_id = OBJECT_ID('MenuItems'))
                    CREATE INDEX [IX_MenuItems_IsActive] ON [MenuItems] ([IsActive]);
                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_MenuItemRecipeLines_ChildMenuItemId' AND object_id = OBJECT_ID('MenuItemRecipeLines'))
                    CREATE INDEX [IX_MenuItemRecipeLines_ChildMenuItemId] ON [MenuItemRecipeLines] ([ChildMenuItemId]);
                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_MenuItemRecipeLines_DefaultWarehouseId' AND object_id = OBJECT_ID('MenuItemRecipeLines'))
                    CREATE INDEX [IX_MenuItemRecipeLines_DefaultWarehouseId] ON [MenuItemRecipeLines] ([DefaultWarehouseId]);
                """);

            migrationBuilder.CreateIndex(
                name: "IX_BreakfastCafeOrders_PosTableId",
                table: "BreakfastCafeOrders",
                column: "PosTableId");

            migrationBuilder.CreateIndex(
                name: "IX_BreakfastCafeKitchenTickets_BreakfastCafeOrderId",
                table: "BreakfastCafeKitchenTickets",
                column: "BreakfastCafeOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_BreakfastCafeKitchenTickets_TicketNumber",
                table: "BreakfastCafeKitchenTickets",
                column: "TicketNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PosTables_AreaCode",
                table: "PosTables",
                column: "AreaCode");

            migrationBuilder.CreateIndex(
                name: "IX_PosTables_Status",
                table: "PosTables",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_PosTables_TableCode",
                table: "PosTables",
                column: "TableCode",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_BreakfastCafeOrders_PosTables_PosTableId",
                table: "BreakfastCafeOrders",
                column: "PosTableId",
                principalTable: "PosTables",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.Sql(
                """
                IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_MenuItemRecipeLines_MenuItems_ChildMenuItemId')
                    ALTER TABLE [MenuItemRecipeLines] ADD CONSTRAINT [FK_MenuItemRecipeLines_MenuItems_ChildMenuItemId]
                    FOREIGN KEY ([ChildMenuItemId]) REFERENCES [MenuItems] ([Id]);
                IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_MenuItemRecipeLines_Warehouses_DefaultWarehouseId')
                    ALTER TABLE [MenuItemRecipeLines] ADD CONSTRAINT [FK_MenuItemRecipeLines_Warehouses_DefaultWarehouseId]
                    FOREIGN KEY ([DefaultWarehouseId]) REFERENCES [Warehouses] ([Id]) ON DELETE SET NULL;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BreakfastCafeOrders_PosTables_PosTableId",
                table: "BreakfastCafeOrders");

            migrationBuilder.DropTable(
                name: "BreakfastCafeKitchenTickets");

            migrationBuilder.DropTable(
                name: "PosTables");

            migrationBuilder.DropIndex(
                name: "IX_BreakfastCafeOrders_PosTableId",
                table: "BreakfastCafeOrders");

            migrationBuilder.DropColumn(
                name: "AreaName",
                table: "BreakfastCafeOrders");

            migrationBuilder.DropColumn(
                name: "KitchenSentAt",
                table: "BreakfastCafeOrders");

            migrationBuilder.DropColumn(
                name: "OrderType",
                table: "BreakfastCafeOrders");

            migrationBuilder.DropColumn(
                name: "PosTableId",
                table: "BreakfastCafeOrders");

            migrationBuilder.DropColumn(
                name: "ServiceChargeAmount",
                table: "BreakfastCafeOrders");

            migrationBuilder.DropColumn(
                name: "ServiceStatus",
                table: "BreakfastCafeOrders");

            migrationBuilder.DropColumn(
                name: "TableCode",
                table: "BreakfastCafeOrders");

            migrationBuilder.DropColumn(
                name: "TableName",
                table: "BreakfastCafeOrders");

            migrationBuilder.DropColumn(
                name: "KitchenStatus",
                table: "BreakfastCafeOrderLines");
        }
    }
}
