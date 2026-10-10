using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ASMILE.BO.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddHotelRoomOperationalLockDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "OperationalStatusChangedAtUtc",
                schema: "fo",
                table: "PhongKhachSan",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "OperationalStatusExpiresAtUtc",
                schema: "fo",
                table: "PhongKhachSan",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OperationalStatusReason",
                schema: "fo",
                table: "PhongKhachSan",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_fo_PhongKhachSan_CoSo_TrangThai_HetHan",
                schema: "fo",
                table: "PhongKhachSan",
                columns: new[] { "PropertyId", "OperationalStatus", "OperationalStatusExpiresAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_fo_PhongKhachSan_CoSo_TrangThai_HetHan",
                schema: "fo",
                table: "PhongKhachSan");

            migrationBuilder.DropColumn(
                name: "OperationalStatusChangedAtUtc",
                schema: "fo",
                table: "PhongKhachSan");

            migrationBuilder.DropColumn(
                name: "OperationalStatusExpiresAtUtc",
                schema: "fo",
                table: "PhongKhachSan");

            migrationBuilder.DropColumn(
                name: "OperationalStatusReason",
                schema: "fo",
                table: "PhongKhachSan");
        }
    }
}
