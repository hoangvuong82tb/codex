using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ASMILE.BO.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddFrontOfficeShiftLogEntries : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "NhatKyCa",
                schema: "fo",
                columns: table => new
                {
                    ShiftLogEntryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PropertyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OccurredAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ShiftName = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Priority = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    RoomReference = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    GuestReference = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    ReservationReference = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    AssignedTo = table.Column<string>(type: "nvarchar(180)", maxLength: 180, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    IsVip = table.Column<bool>(type: "bit", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fo_NhatKyCa", x => x.ShiftLogEntryId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_fo_NhatKyCa_CoSo_Nhom",
                schema: "fo",
                table: "NhatKyCa",
                columns: new[] { "PropertyId", "Category" });

            migrationBuilder.CreateIndex(
                name: "IX_fo_NhatKyCa_CoSo_ThoiGian",
                schema: "fo",
                table: "NhatKyCa",
                columns: new[] { "PropertyId", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_fo_NhatKyCa_CoSo_TrangThai_UuTien",
                schema: "fo",
                table: "NhatKyCa",
                columns: new[] { "PropertyId", "Status", "Priority" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NhatKyCa",
                schema: "fo");
        }
    }
}
