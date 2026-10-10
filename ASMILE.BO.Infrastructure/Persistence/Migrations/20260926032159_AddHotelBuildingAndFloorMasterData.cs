using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ASMILE.BO.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddHotelBuildingAndFloorMasterData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ToaNhaKhachSan",
                schema: "fo",
                columns: table => new
                {
                    HotelBuildingId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PropertyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
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
                    table.PrimaryKey("PK_fo_ToaNhaKhachSan", x => x.HotelBuildingId);
                });

            migrationBuilder.CreateTable(
                name: "TangKhachSan",
                schema: "fo",
                columns: table => new
                {
                    HotelFloorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PropertyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HotelBuildingId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    LevelNumber = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
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
                    table.PrimaryKey("PK_fo_TangKhachSan", x => x.HotelFloorId);
                    table.ForeignKey(
                        name: "FK_TangKhachSan_ToaNhaKhachSan_HotelBuildingId",
                        column: x => x.HotelBuildingId,
                        principalSchema: "fo",
                        principalTable: "ToaNhaKhachSan",
                        principalColumn: "HotelBuildingId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_fo_TangKhachSan_CoSo_ToaNha_Tang",
                schema: "fo",
                table: "TangKhachSan",
                columns: new[] { "PropertyId", "HotelBuildingId", "LevelNumber" });

            migrationBuilder.CreateIndex(
                name: "IX_TangKhachSan_HotelBuildingId",
                schema: "fo",
                table: "TangKhachSan",
                column: "HotelBuildingId");

            migrationBuilder.CreateIndex(
                name: "UQ_fo_TangKhachSan_CoSo_ToaNha_Ma",
                schema: "fo",
                table: "TangKhachSan",
                columns: new[] { "PropertyId", "HotelBuildingId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_fo_ToaNhaKhachSan_CoSo_HoatDong",
                schema: "fo",
                table: "ToaNhaKhachSan",
                columns: new[] { "PropertyId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_fo_ToaNhaKhachSan_CoSo_ThuTu",
                schema: "fo",
                table: "ToaNhaKhachSan",
                columns: new[] { "PropertyId", "DisplayOrder" });

            migrationBuilder.CreateIndex(
                name: "UQ_fo_ToaNhaKhachSan_CoSo_Ma",
                schema: "fo",
                table: "ToaNhaKhachSan",
                columns: new[] { "PropertyId", "Code" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TangKhachSan",
                schema: "fo");

            migrationBuilder.DropTable(
                name: "ToaNhaKhachSan",
                schema: "fo");
        }
    }
}
