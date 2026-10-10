using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ASMILE.BO.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddHotelRoomMoveUpgradeRules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "QuyTacDoiNangHangPhong",
                schema: "fo",
                columns: table => new
                {
                    HotelRoomMoveUpgradeRuleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PropertyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    EffectiveFrom = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EffectiveTo = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AllowRoomMove = table.Column<bool>(type: "bit", nullable: false),
                    AllowUpgrade = table.Column<bool>(type: "bit", nullable: false),
                    AllowDowngrade = table.Column<bool>(type: "bit", nullable: false),
                    RequireApproval = table.Column<bool>(type: "bit", nullable: false),
                    ApprovalRole = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    PriceDifferenceMode = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    MaxClassChangeLevels = table.Column<int>(type: "int", nullable: false),
                    RequireHousekeepingReady = table.Column<bool>(type: "bit", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fo_QuyTacDoiNangHangPhong", x => x.HotelRoomMoveUpgradeRuleId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_fo_QuyTacDoiNangHangPhong_CoSo_HieuLuc",
                schema: "fo",
                table: "QuyTacDoiNangHangPhong",
                columns: new[] { "PropertyId", "EffectiveFrom", "EffectiveTo", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "UQ_fo_QuyTacDoiNangHangPhong_CoSo_Ma",
                schema: "fo",
                table: "QuyTacDoiNangHangPhong",
                columns: new[] { "PropertyId", "Code" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "QuyTacDoiNangHangPhong",
                schema: "fo");
        }
    }
}
