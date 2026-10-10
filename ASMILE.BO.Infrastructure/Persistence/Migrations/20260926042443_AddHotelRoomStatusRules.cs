using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ASMILE.BO.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddHotelRoomStatusRules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "QuyTacTrangThaiPhong",
                schema: "fo",
                columns: table => new
                {
                    HotelRoomStatusRuleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PropertyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    StatusDomain = table.Column<string>(type: "nvarchar(12)", maxLength: 12, nullable: false),
                    FromStatus = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    ToStatus = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    RequiredRole = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    RequiredEvent = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    RequiresApproval = table.Column<bool>(type: "bit", nullable: false),
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
                    table.PrimaryKey("PK_fo_QuyTacTrangThaiPhong", x => x.HotelRoomStatusRuleId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_fo_QuyTacTrangThaiPhong_CoSo_LinhVuc_HoatDong",
                schema: "fo",
                table: "QuyTacTrangThaiPhong",
                columns: new[] { "PropertyId", "StatusDomain", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "UQ_fo_QuyTacTrangThaiPhong_CoSo_Ma",
                schema: "fo",
                table: "QuyTacTrangThaiPhong",
                columns: new[] { "PropertyId", "Code" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "QuyTacTrangThaiPhong",
                schema: "fo");
        }
    }
}
