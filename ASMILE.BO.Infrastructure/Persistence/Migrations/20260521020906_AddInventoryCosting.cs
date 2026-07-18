using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ASMILE.BO.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddInventoryCosting : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "InventoryCostLayers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ItemId = table.Column<int>(type: "int", nullable: false),
                    WarehouseId = table.Column<int>(type: "int", nullable: false),
                    SourceType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    SourceId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SourceDetailId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DocumentNo = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DocumentDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    QuantityIn = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    QuantityOut = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    UnitCost = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    TotalCost = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    RemainingQuantity = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    RemainingCost = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CostMethod = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryCostLayers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "InventoryCostTransactions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ItemId = table.Column<int>(type: "int", nullable: false),
                    WarehouseId = table.Column<int>(type: "int", nullable: false),
                    SourceType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    SourceId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SourceDetailId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DocumentNo = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DocumentDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    UnitCost = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    TotalCost = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CostMethod = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    JournalHeaderId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryCostTransactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InventoryCostTransactions_JournalHeaders_JournalHeaderId",
                        column: x => x.JournalHeaderId,
                        principalTable: "JournalHeaders",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryCostLayers_DocumentDate",
                table: "InventoryCostLayers",
                column: "DocumentDate");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryCostLayers_ItemId_WarehouseId",
                table: "InventoryCostLayers",
                columns: new[] { "ItemId", "WarehouseId" });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryCostLayers_SourceDetailId",
                table: "InventoryCostLayers",
                column: "SourceDetailId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryCostLayers_SourceType_SourceId",
                table: "InventoryCostLayers",
                columns: new[] { "SourceType", "SourceId" });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryCostLayers_Status",
                table: "InventoryCostLayers",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryCostTransactions_DocumentDate",
                table: "InventoryCostTransactions",
                column: "DocumentDate");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryCostTransactions_ItemId_WarehouseId",
                table: "InventoryCostTransactions",
                columns: new[] { "ItemId", "WarehouseId" });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryCostTransactions_JournalHeaderId",
                table: "InventoryCostTransactions",
                column: "JournalHeaderId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryCostTransactions_SourceDetailId",
                table: "InventoryCostTransactions",
                column: "SourceDetailId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryCostTransactions_SourceType_SourceId",
                table: "InventoryCostTransactions",
                columns: new[] { "SourceType", "SourceId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InventoryCostLayers");

            migrationBuilder.DropTable(
                name: "InventoryCostTransactions");
        }
    }
}
