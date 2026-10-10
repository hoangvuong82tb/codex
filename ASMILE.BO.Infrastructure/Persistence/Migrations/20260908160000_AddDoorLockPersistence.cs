using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ASMILE.BO.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDoorLockPersistence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SuKienKhoaCua",
                schema: "fo",
                columns: table => new
                {
                    EventId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PropertyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DeviceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RoomNumber = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    Type = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    KeyCode = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    AccessLevel = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    Granted = table.Column<bool>(type: "bit", nullable: false),
                    OccurredAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Detail = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fo_SuKienKhoaCua", x => x.EventId);
                });

            migrationBuilder.CreateTable(
                name: "ThietBiKhoaCua",
                schema: "fo",
                columns: table => new
                {
                    DeviceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PropertyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DeviceCode = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    RoomNumber = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    RoomId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Vendor = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Model = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    FirmwareVersion = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    BatteryLevel = table.Column<int>(type: "int", nullable: false),
                    LastSeenAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastBatteryChangeAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TotalAccesses = table.Column<int>(type: "int", nullable: false),
                    FailedAttempts = table.Column<int>(type: "int", nullable: false),
                    IsSynced = table.Column<bool>(type: "bit", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fo_ThietBiKhoaCua", x => x.DeviceId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_fo_SuKienKhoaCua_CoSo_KetQua_ThoiGian",
                schema: "fo",
                table: "SuKienKhoaCua",
                columns: new[] { "PropertyId", "Granted", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_fo_SuKienKhoaCua_CoSo_ThietBi_ThoiGian",
                schema: "fo",
                table: "SuKienKhoaCua",
                columns: new[] { "PropertyId", "DeviceId", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_fo_ThietBiKhoaCua_CoSo_Phong_TrangThai",
                schema: "fo",
                table: "ThietBiKhoaCua",
                columns: new[] { "PropertyId", "RoomId", "Status" });

            migrationBuilder.CreateIndex(
                name: "UQ_fo_ThietBiKhoaCua_CoSo_MaThietBi",
                schema: "fo",
                table: "ThietBiKhoaCua",
                columns: new[] { "PropertyId", "DeviceCode" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SuKienKhoaCua",
                schema: "fo");

            migrationBuilder.DropTable(
                name: "ThietBiKhoaCua",
                schema: "fo");
        }
    }
}
