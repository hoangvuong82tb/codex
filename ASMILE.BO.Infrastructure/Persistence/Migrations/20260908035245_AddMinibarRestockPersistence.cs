using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ASMILE.BO.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMinibarRestockPersistence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MinibarRestockTransaction",
                schema: "fo",
                columns: table => new
                {
                    TransactionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PropertyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HotelRoomId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ItemCode = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    ItemName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,3)", nullable: false),
                    Warehouse = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    OccurredAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ActorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fo_MinibarRestockTransaction", x => x.TransactionId);
                    table.ForeignKey(
                        name: "FK_MinibarRestockTransaction_PhongKhachSan_HotelRoomId",
                        column: x => x.HotelRoomId,
                        principalSchema: "fo",
                        principalTable: "PhongKhachSan",
                        principalColumn: "HotelRoomId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MinibarRestockTransaction_HotelRoomId",
                schema: "fo",
                table: "MinibarRestockTransaction",
                column: "HotelRoomId");

            migrationBuilder.CreateIndex(
                name: "IX_MinibarRestockTransaction_PropertyId_HotelRoomId_OccurredAtUtc",
                schema: "fo",
                table: "MinibarRestockTransaction",
                columns: new[] { "PropertyId", "HotelRoomId", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_MinibarRestockTransaction_PropertyId_ItemCode",
                schema: "fo",
                table: "MinibarRestockTransaction",
                columns: new[] { "PropertyId", "ItemCode" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MinibarRestockTransaction",
                schema: "fo");
        }
    }
}
