using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ASMILE.BO.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMinibarInventoryPersistence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MinibarInventoryAudit",
                schema: "fo",
                columns: table => new
                {
                    InventoryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PropertyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HotelRoomId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StayId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CountedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CountedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fo_MinibarInventoryAudit", x => x.InventoryId);
                    table.ForeignKey(
                        name: "FK_MinibarInventoryAudit_PhongKhachSan_HotelRoomId",
                        column: x => x.HotelRoomId,
                        principalSchema: "fo",
                        principalTable: "PhongKhachSan",
                        principalColumn: "HotelRoomId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MinibarInventoryLine",
                schema: "fo",
                columns: table => new
                {
                    InventoryLineId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InventoryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PropertyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ItemCode = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    ItemName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,3)", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fo_MinibarInventoryLine", x => x.InventoryLineId);
                    table.ForeignKey(
                        name: "FK_MinibarInventoryLine_MinibarInventoryAudit_InventoryId",
                        column: x => x.InventoryId,
                        principalSchema: "fo",
                        principalTable: "MinibarInventoryAudit",
                        principalColumn: "InventoryId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MinibarInventoryAudit_HotelRoomId",
                schema: "fo",
                table: "MinibarInventoryAudit",
                column: "HotelRoomId");

            migrationBuilder.CreateIndex(
                name: "IX_MinibarInventoryAudit_PropertyId_HotelRoomId_CountedAtUtc",
                schema: "fo",
                table: "MinibarInventoryAudit",
                columns: new[] { "PropertyId", "HotelRoomId", "CountedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_MinibarInventoryLine_InventoryId",
                schema: "fo",
                table: "MinibarInventoryLine",
                column: "InventoryId");

            migrationBuilder.CreateIndex(
                name: "IX_MinibarInventoryLine_PropertyId_InventoryId",
                schema: "fo",
                table: "MinibarInventoryLine",
                columns: new[] { "PropertyId", "InventoryId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MinibarInventoryLine",
                schema: "fo");

            migrationBuilder.DropTable(
                name: "MinibarInventoryAudit",
                schema: "fo");
        }
    }
}
