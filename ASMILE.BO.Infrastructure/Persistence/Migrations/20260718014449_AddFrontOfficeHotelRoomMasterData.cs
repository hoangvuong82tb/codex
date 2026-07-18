using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ASMILE.BO.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddFrontOfficeHotelRoomMasterData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LoaiPhongKhachSan",
                schema: "fo",
                columns: table => new
                {
                    HotelRoomTypeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PropertyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    StandardAdultCapacity = table.Column<int>(type: "int", nullable: false),
                    StandardChildCapacity = table.Column<int>(type: "int", nullable: false),
                    MaximumAdultCapacity = table.Column<int>(type: "int", nullable: false),
                    MaximumChildCapacity = table.Column<int>(type: "int", nullable: false),
                    DefaultOccupancy = table.Column<int>(type: "int", nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fo_LoaiPhongKhachSan", x => x.HotelRoomTypeId);
                });

            migrationBuilder.CreateTable(
                name: "PhongKhachSan",
                schema: "fo",
                columns: table => new
                {
                    HotelRoomId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PropertyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HotelRoomTypeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RoomNumber = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: true),
                    Floor = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    Building = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    Zone = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    OperationalStatus = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fo_PhongKhachSan", x => x.HotelRoomId);
                    table.ForeignKey(
                        name: "FK_PhongKhachSan_LoaiPhongKhachSan_HotelRoomTypeId",
                        column: x => x.HotelRoomTypeId,
                        principalSchema: "fo",
                        principalTable: "LoaiPhongKhachSan",
                        principalColumn: "HotelRoomTypeId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_fo_LoaiPhongKhachSan_CoSo_HoatDong",
                schema: "fo",
                table: "LoaiPhongKhachSan",
                columns: new[] { "PropertyId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_fo_LoaiPhongKhachSan_CoSo_ThuTu",
                schema: "fo",
                table: "LoaiPhongKhachSan",
                columns: new[] { "PropertyId", "DisplayOrder" });

            migrationBuilder.CreateIndex(
                name: "UQ_fo_LoaiPhongKhachSan_CoSo_Ma",
                schema: "fo",
                table: "LoaiPhongKhachSan",
                columns: new[] { "PropertyId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_fo_PhongKhachSan_CoSo_HoatDong",
                schema: "fo",
                table: "PhongKhachSan",
                columns: new[] { "PropertyId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_fo_PhongKhachSan_CoSo_LoaiPhong",
                schema: "fo",
                table: "PhongKhachSan",
                columns: new[] { "PropertyId", "HotelRoomTypeId" });

            migrationBuilder.CreateIndex(
                name: "IX_fo_PhongKhachSan_CoSo_TrangThai",
                schema: "fo",
                table: "PhongKhachSan",
                columns: new[] { "PropertyId", "OperationalStatus" });

            migrationBuilder.CreateIndex(
                name: "IX_PhongKhachSan_HotelRoomTypeId",
                schema: "fo",
                table: "PhongKhachSan",
                column: "HotelRoomTypeId");

            migrationBuilder.CreateIndex(
                name: "UQ_fo_PhongKhachSan_CoSo_SoPhong",
                schema: "fo",
                table: "PhongKhachSan",
                columns: new[] { "PropertyId", "RoomNumber" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PhongKhachSan",
                schema: "fo");

            migrationBuilder.DropTable(
                name: "LoaiPhongKhachSan",
                schema: "fo");
        }
    }
}
