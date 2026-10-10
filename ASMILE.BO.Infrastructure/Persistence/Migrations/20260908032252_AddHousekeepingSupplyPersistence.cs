using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ASMILE.BO.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddHousekeepingSupplyPersistence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GiaoDichDoVai",
                schema: "fo",
                columns: table => new
                {
                    TransactionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PropertyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HotelRoomId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LinenCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Action = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    OccurredAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ActorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fo_GiaoDichDoVai", x => x.TransactionId);
                    table.ForeignKey(
                        name: "FK_GiaoDichDoVai_PhongKhachSan_HotelRoomId",
                        column: x => x.HotelRoomId,
                        principalSchema: "fo",
                        principalTable: "PhongKhachSan",
                        principalColumn: "HotelRoomId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LoaiDoVai",
                schema: "fo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PropertyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    DefaultParLevel = table.Column<int>(type: "int", nullable: false),
                    MaxWashCycles = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fo_LoaiDoVai", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LoaiTienNghi",
                schema: "fo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PropertyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    DefaultParLevel = table.Column<int>(type: "int", nullable: false),
                    MinAlertThreshold = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fo_LoaiTienNghi", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TonTienNghiPhong",
                schema: "fo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PropertyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HotelRoomId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AmenityCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CurrentStock = table.Column<int>(type: "int", nullable: false),
                    ParLevel = table.Column<int>(type: "int", nullable: false),
                    ConsumptionLast30Days = table.Column<int>(type: "int", nullable: false),
                    LastRefillUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fo_TonTienNghiPhong", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TonTienNghiPhong_PhongKhachSan_HotelRoomId",
                        column: x => x.HotelRoomId,
                        principalSchema: "fo",
                        principalTable: "PhongKhachSan",
                        principalColumn: "HotelRoomId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "YeuCauCapBuTienNghi",
                schema: "fo",
                columns: table => new
                {
                    RequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PropertyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HotelRoomId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequestedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    RequestedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fo_YeuCauCapBuTienNghi", x => x.RequestId);
                    table.ForeignKey(
                        name: "FK_YeuCauCapBuTienNghi_PhongKhachSan_HotelRoomId",
                        column: x => x.HotelRoomId,
                        principalSchema: "fo",
                        principalTable: "PhongKhachSan",
                        principalColumn: "HotelRoomId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ChiTietYeuCauCapBuTienNghi",
                schema: "fo",
                columns: table => new
                {
                    LineId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AmenityCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fo_ChiTietYeuCauCapBuTienNghi", x => x.LineId);
                    table.ForeignKey(
                        name: "FK_ChiTietYeuCauCapBuTienNghi_YeuCauCapBuTienNghi_RequestId",
                        column: x => x.RequestId,
                        principalSchema: "fo",
                        principalTable: "YeuCauCapBuTienNghi",
                        principalColumn: "RequestId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "UQ_fo_ChiTietYeuCauCapBuTienNghi_YeuCau_Ma",
                schema: "fo",
                table: "ChiTietYeuCauCapBuTienNghi",
                columns: new[] { "RequestId", "AmenityCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_fo_GiaoDichDoVai_CoSo_Phong_Ma_ThoiGian",
                schema: "fo",
                table: "GiaoDichDoVai",
                columns: new[] { "PropertyId", "HotelRoomId", "LinenCode", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_GiaoDichDoVai_HotelRoomId",
                schema: "fo",
                table: "GiaoDichDoVai",
                column: "HotelRoomId");

            migrationBuilder.CreateIndex(
                name: "IX_fo_LoaiDoVai_CoSo_HoatDong",
                schema: "fo",
                table: "LoaiDoVai",
                columns: new[] { "PropertyId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "UQ_fo_LoaiDoVai_CoSo_Ma",
                schema: "fo",
                table: "LoaiDoVai",
                columns: new[] { "PropertyId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_fo_LoaiTienNghi_CoSo_HoatDong",
                schema: "fo",
                table: "LoaiTienNghi",
                columns: new[] { "PropertyId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "UQ_fo_LoaiTienNghi_CoSo_Ma",
                schema: "fo",
                table: "LoaiTienNghi",
                columns: new[] { "PropertyId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TonTienNghiPhong_HotelRoomId",
                schema: "fo",
                table: "TonTienNghiPhong",
                column: "HotelRoomId");

            migrationBuilder.CreateIndex(
                name: "UQ_fo_TonTienNghiPhong_CoSo_Phong_Ma",
                schema: "fo",
                table: "TonTienNghiPhong",
                columns: new[] { "PropertyId", "HotelRoomId", "AmenityCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_fo_YeuCauCapBuTienNghi_CoSo_Phong_ThoiGian",
                schema: "fo",
                table: "YeuCauCapBuTienNghi",
                columns: new[] { "PropertyId", "HotelRoomId", "RequestedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_YeuCauCapBuTienNghi_HotelRoomId",
                schema: "fo",
                table: "YeuCauCapBuTienNghi",
                column: "HotelRoomId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChiTietYeuCauCapBuTienNghi",
                schema: "fo");

            migrationBuilder.DropTable(
                name: "GiaoDichDoVai",
                schema: "fo");

            migrationBuilder.DropTable(
                name: "LoaiDoVai",
                schema: "fo");

            migrationBuilder.DropTable(
                name: "LoaiTienNghi",
                schema: "fo");

            migrationBuilder.DropTable(
                name: "TonTienNghiPhong",
                schema: "fo");

            migrationBuilder.DropTable(
                name: "YeuCauCapBuTienNghi",
                schema: "fo");
        }
    }
}
