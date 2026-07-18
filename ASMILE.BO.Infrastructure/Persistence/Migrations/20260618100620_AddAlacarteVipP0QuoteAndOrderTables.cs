using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ASMILE.BO.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAlacarteVipP0QuoteAndOrderTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TableStandard",
                table: "RestaurantQuotes",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "CalculatedQuantity",
                table: "RestaurantQuoteLines",
                type: "decimal(18,3)",
                precision: 18,
                scale: 3,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "PricingRule",
                table: "RestaurantQuoteLines",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "ActualGuestCount",
                table: "AlacarteOrders",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "DiningRoomId",
                table: "AlacarteOrders",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OrderType",
                table: "AlacarteOrders",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "RestaurantQuoteId",
                table: "AlacarteOrders",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsSupplement",
                table: "AlacarteOrderLines",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "PricingRule",
                table: "AlacarteOrderLines",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "RestaurantOrderTableId",
                table: "AlacarteOrderLines",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "RestaurantQuoteTables",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RestaurantQuoteId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TableNo = table.Column<int>(type: "int", nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    GuestCount = table.Column<int>(type: "int", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsVirtual = table.Column<bool>(type: "bit", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RestaurantQuoteTables", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RestaurantQuoteTables_RestaurantQuotes_RestaurantQuoteId",
                        column: x => x.RestaurantQuoteId,
                        principalTable: "RestaurantQuotes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RestaurantOrderTables",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AlacarteOrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RestaurantQuoteTableId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TableNo = table.Column<int>(type: "int", nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    GuestCount = table.Column<int>(type: "int", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsVirtual = table.Column<bool>(type: "bit", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RestaurantOrderTables", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RestaurantOrderTables_AlacarteOrders_AlacarteOrderId",
                        column: x => x.AlacarteOrderId,
                        principalTable: "AlacarteOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RestaurantOrderTables_RestaurantQuoteTables_RestaurantQuoteTableId",
                        column: x => x.RestaurantQuoteTableId,
                        principalTable: "RestaurantQuoteTables",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AlacarteOrders_DiningRoomId",
                table: "AlacarteOrders",
                column: "DiningRoomId");

            migrationBuilder.CreateIndex(
                name: "IX_AlacarteOrders_RestaurantQuoteId",
                table: "AlacarteOrders",
                column: "RestaurantQuoteId",
                unique: true,
                filter: "[RestaurantQuoteId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AlacarteOrderLines_RestaurantOrderTableId",
                table: "AlacarteOrderLines",
                column: "RestaurantOrderTableId");

            migrationBuilder.CreateIndex(
                name: "IX_RestaurantOrderTables_AlacarteOrderId_SortOrder",
                table: "RestaurantOrderTables",
                columns: new[] { "AlacarteOrderId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_RestaurantOrderTables_AlacarteOrderId_TableNo",
                table: "RestaurantOrderTables",
                columns: new[] { "AlacarteOrderId", "TableNo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RestaurantOrderTables_RestaurantQuoteTableId",
                table: "RestaurantOrderTables",
                column: "RestaurantQuoteTableId");

            migrationBuilder.CreateIndex(
                name: "IX_RestaurantQuoteTables_RestaurantQuoteId_SortOrder",
                table: "RestaurantQuoteTables",
                columns: new[] { "RestaurantQuoteId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_RestaurantQuoteTables_RestaurantQuoteId_TableNo",
                table: "RestaurantQuoteTables",
                columns: new[] { "RestaurantQuoteId", "TableNo" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_AlacarteOrderLines_RestaurantOrderTables_RestaurantOrderTableId",
                table: "AlacarteOrderLines",
                column: "RestaurantOrderTableId",
                principalTable: "RestaurantOrderTables",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_AlacarteOrders_DiningRooms_DiningRoomId",
                table: "AlacarteOrders",
                column: "DiningRoomId",
                principalTable: "DiningRooms",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_AlacarteOrders_RestaurantQuotes_RestaurantQuoteId",
                table: "AlacarteOrders",
                column: "RestaurantQuoteId",
                principalTable: "RestaurantQuotes",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AlacarteOrderLines_RestaurantOrderTables_RestaurantOrderTableId",
                table: "AlacarteOrderLines");

            migrationBuilder.DropForeignKey(
                name: "FK_AlacarteOrders_DiningRooms_DiningRoomId",
                table: "AlacarteOrders");

            migrationBuilder.DropForeignKey(
                name: "FK_AlacarteOrders_RestaurantQuotes_RestaurantQuoteId",
                table: "AlacarteOrders");

            migrationBuilder.DropTable(
                name: "RestaurantOrderTables");

            migrationBuilder.DropTable(
                name: "RestaurantQuoteTables");

            migrationBuilder.DropIndex(
                name: "IX_AlacarteOrders_DiningRoomId",
                table: "AlacarteOrders");

            migrationBuilder.DropIndex(
                name: "IX_AlacarteOrders_RestaurantQuoteId",
                table: "AlacarteOrders");

            migrationBuilder.DropIndex(
                name: "IX_AlacarteOrderLines_RestaurantOrderTableId",
                table: "AlacarteOrderLines");

            migrationBuilder.DropColumn(
                name: "TableStandard",
                table: "RestaurantQuotes");

            migrationBuilder.DropColumn(
                name: "CalculatedQuantity",
                table: "RestaurantQuoteLines");

            migrationBuilder.DropColumn(
                name: "PricingRule",
                table: "RestaurantQuoteLines");

            migrationBuilder.DropColumn(
                name: "ActualGuestCount",
                table: "AlacarteOrders");

            migrationBuilder.DropColumn(
                name: "DiningRoomId",
                table: "AlacarteOrders");

            migrationBuilder.DropColumn(
                name: "OrderType",
                table: "AlacarteOrders");

            migrationBuilder.DropColumn(
                name: "RestaurantQuoteId",
                table: "AlacarteOrders");

            migrationBuilder.DropColumn(
                name: "IsSupplement",
                table: "AlacarteOrderLines");

            migrationBuilder.DropColumn(
                name: "PricingRule",
                table: "AlacarteOrderLines");

            migrationBuilder.DropColumn(
                name: "RestaurantOrderTableId",
                table: "AlacarteOrderLines");
        }
    }
}
