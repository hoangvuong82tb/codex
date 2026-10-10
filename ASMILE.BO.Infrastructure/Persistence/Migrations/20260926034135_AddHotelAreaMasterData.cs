using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ASMILE.BO.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddHotelAreaMasterData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "KhuVucKhachSan",
                schema: "fo",
                columns: table => new
                {
                    HotelAreaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PropertyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HotelBuildingId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    HotelFloorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Code = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    AreaType = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
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
                    table.PrimaryKey("PK_fo_KhuVucKhachSan", x => x.HotelAreaId);
                    table.ForeignKey(
                        name: "FK_KhuVucKhachSan_TangKhachSan_HotelFloorId",
                        column: x => x.HotelFloorId,
                        principalSchema: "fo",
                        principalTable: "TangKhachSan",
                        principalColumn: "HotelFloorId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_KhuVucKhachSan_ToaNhaKhachSan_HotelBuildingId",
                        column: x => x.HotelBuildingId,
                        principalSchema: "fo",
                        principalTable: "ToaNhaKhachSan",
                        principalColumn: "HotelBuildingId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_fo_KhuVucKhachSan_CoSo_ViTri",
                schema: "fo",
                table: "KhuVucKhachSan",
                columns: new[] { "PropertyId", "HotelBuildingId", "HotelFloorId" });

            migrationBuilder.CreateIndex(
                name: "IX_KhuVucKhachSan_HotelBuildingId",
                schema: "fo",
                table: "KhuVucKhachSan",
                column: "HotelBuildingId");

            migrationBuilder.CreateIndex(
                name: "IX_KhuVucKhachSan_HotelFloorId",
                schema: "fo",
                table: "KhuVucKhachSan",
                column: "HotelFloorId");

            migrationBuilder.CreateIndex(
                name: "UQ_fo_KhuVucKhachSan_CoSo_Ma",
                schema: "fo",
                table: "KhuVucKhachSan",
                columns: new[] { "PropertyId", "Code" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "KhuVucKhachSan",
                schema: "fo");
        }
    }
}
