using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ASMILE.BO.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddHotelStayOccupants : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "KhachDiCungLuuTru",
                schema: "fo",
                columns: table => new
                {
                    OccupantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PropertyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HotelStayId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GuestProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GuestNameSnapshot = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    GuestPhoneSnapshot = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    OccupantType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    AddedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AddedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    RemovedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RemovedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fo_KhachDiCungLuuTru", x => x.OccupantId);
                    table.ForeignKey(
                        name: "FK_KhachDiCungLuuTru_HoSoKhach_GuestProfileId",
                        column: x => x.GuestProfileId,
                        principalSchema: "fo",
                        principalTable: "HoSoKhach",
                        principalColumn: "GuestProfileId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_KhachDiCungLuuTru_LuuTruKhachSan_HotelStayId",
                        column: x => x.HotelStayId,
                        principalSchema: "fo",
                        principalTable: "LuuTruKhachSan",
                        principalColumn: "HotelStayId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_fo_KhachDiCungLuuTru_CoSo_LuuTru_HoatDong",
                schema: "fo",
                table: "KhachDiCungLuuTru",
                columns: new[] { "PropertyId", "HotelStayId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_KhachDiCungLuuTru_GuestProfileId",
                schema: "fo",
                table: "KhachDiCungLuuTru",
                column: "GuestProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_KhachDiCungLuuTru_HotelStayId",
                schema: "fo",
                table: "KhachDiCungLuuTru",
                column: "HotelStayId");

            migrationBuilder.CreateIndex(
                name: "UQ_fo_KhachDiCungLuuTru_CoSo_LuuTru_Khach_HoatDong",
                schema: "fo",
                table: "KhachDiCungLuuTru",
                columns: new[] { "PropertyId", "HotelStayId", "GuestProfileId", "IsActive" },
                unique: true,
                filter: "[IsActive] = 1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "KhachDiCungLuuTru",
                schema: "fo");
        }
    }
}
