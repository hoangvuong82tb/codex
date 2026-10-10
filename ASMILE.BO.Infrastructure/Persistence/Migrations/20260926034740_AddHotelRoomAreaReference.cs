using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ASMILE.BO.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddHotelRoomAreaReference : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "HotelAreaId",
                schema: "fo",
                table: "PhongKhachSan",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_fo_PhongKhachSan_CoSo_KhuVuc",
                schema: "fo",
                table: "PhongKhachSan",
                columns: new[] { "PropertyId", "HotelAreaId" });

            migrationBuilder.CreateIndex(
                name: "IX_PhongKhachSan_HotelAreaId",
                schema: "fo",
                table: "PhongKhachSan",
                column: "HotelAreaId");

            migrationBuilder.AddForeignKey(
                name: "FK_PhongKhachSan_KhuVucKhachSan_HotelAreaId",
                schema: "fo",
                table: "PhongKhachSan",
                column: "HotelAreaId",
                principalSchema: "fo",
                principalTable: "KhuVucKhachSan",
                principalColumn: "HotelAreaId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PhongKhachSan_KhuVucKhachSan_HotelAreaId",
                schema: "fo",
                table: "PhongKhachSan");

            migrationBuilder.DropIndex(
                name: "IX_fo_PhongKhachSan_CoSo_KhuVuc",
                schema: "fo",
                table: "PhongKhachSan");

            migrationBuilder.DropIndex(
                name: "IX_PhongKhachSan_HotelAreaId",
                schema: "fo",
                table: "PhongKhachSan");

            migrationBuilder.DropColumn(
                name: "HotelAreaId",
                schema: "fo",
                table: "PhongKhachSan");
        }
    }
}
