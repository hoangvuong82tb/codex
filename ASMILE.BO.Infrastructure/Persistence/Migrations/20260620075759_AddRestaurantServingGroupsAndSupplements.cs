using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ASMILE.BO.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRestaurantServingGroupsAndSupplements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "DiningTableId",
                table: "RestaurantOrderTables",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SupplementNote",
                table: "AlacarteOrderLines",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "SupplementQuantity",
                table: "AlacarteOrderLines",
                type: "decimal(18,3)",
                precision: 18,
                scale: 3,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "RestaurantServingGroups",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AlacarteOrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    ScopeType = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    ServingRule = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RestaurantServingGroups", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RestaurantServingGroups_AlacarteOrders_AlacarteOrderId",
                        column: x => x.AlacarteOrderId,
                        principalTable: "AlacarteOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RestaurantServingGroupTables",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RestaurantServingGroupId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RestaurantOrderTableId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DiningTableId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    GuestCount = table.Column<int>(type: "int", nullable: false),
                    ExtraGuestCount = table.Column<int>(type: "int", nullable: false),
                    IsServing = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RestaurantServingGroupTables", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RestaurantServingGroupTables_DiningTables_DiningTableId",
                        column: x => x.DiningTableId,
                        principalTable: "DiningTables",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_RestaurantServingGroupTables_RestaurantOrderTables_RestaurantOrderTableId",
                        column: x => x.RestaurantOrderTableId,
                        principalTable: "RestaurantOrderTables",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_RestaurantServingGroupTables_RestaurantServingGroups_RestaurantServingGroupId",
                        column: x => x.RestaurantServingGroupId,
                        principalTable: "RestaurantServingGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RestaurantOrderTables_DiningTableId",
                table: "RestaurantOrderTables",
                column: "DiningTableId");

            migrationBuilder.CreateIndex(
                name: "IX_RestaurantServingGroups_AlacarteOrderId_Code",
                table: "RestaurantServingGroups",
                columns: new[] { "AlacarteOrderId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RestaurantServingGroups_AlacarteOrderId_SortOrder",
                table: "RestaurantServingGroups",
                columns: new[] { "AlacarteOrderId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_RestaurantServingGroupTables_DiningTableId",
                table: "RestaurantServingGroupTables",
                column: "DiningTableId");

            migrationBuilder.CreateIndex(
                name: "IX_RestaurantServingGroupTables_RestaurantOrderTableId",
                table: "RestaurantServingGroupTables",
                column: "RestaurantOrderTableId");

            migrationBuilder.CreateIndex(
                name: "IX_RestaurantServingGroupTables_RestaurantServingGroupId_RestaurantOrderTableId",
                table: "RestaurantServingGroupTables",
                columns: new[] { "RestaurantServingGroupId", "RestaurantOrderTableId" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_RestaurantOrderTables_DiningTables_DiningTableId",
                table: "RestaurantOrderTables",
                column: "DiningTableId",
                principalTable: "DiningTables",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RestaurantOrderTables_DiningTables_DiningTableId",
                table: "RestaurantOrderTables");

            migrationBuilder.DropTable(
                name: "RestaurantServingGroupTables");

            migrationBuilder.DropTable(
                name: "RestaurantServingGroups");

            migrationBuilder.DropIndex(
                name: "IX_RestaurantOrderTables_DiningTableId",
                table: "RestaurantOrderTables");

            migrationBuilder.DropColumn(
                name: "DiningTableId",
                table: "RestaurantOrderTables");

            migrationBuilder.DropColumn(
                name: "SupplementNote",
                table: "AlacarteOrderLines");

            migrationBuilder.DropColumn(
                name: "SupplementQuantity",
                table: "AlacarteOrderLines");
        }
    }
}
