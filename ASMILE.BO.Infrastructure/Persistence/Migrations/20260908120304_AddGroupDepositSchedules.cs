using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ASMILE.BO.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddGroupDepositSchedules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: "DepositCutoffDate",
                schema: "fo",
                table: "DatPhongDoan",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "DepositReleaseDate",
                schema: "fo",
                table: "DatPhongDoan",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "DepositRequired",
                schema: "fo",
                table: "DatPhongDoan",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "GuaranteeType",
                schema: "fo",
                table: "DatPhongDoan",
                type: "nvarchar(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "LichDatCocDoan",
                schema: "fo",
                columns: table => new
                {
                    GroupDepositScheduleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PropertyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GroupReservationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DueDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Percent = table.Column<decimal>(type: "decimal(9,2)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fo_LichDatCocDoan", x => x.GroupDepositScheduleId);
                    table.ForeignKey(
                        name: "FK_LichDatCocDoan_DatPhongDoan_GroupReservationId",
                        column: x => x.GroupReservationId,
                        principalSchema: "fo",
                        principalTable: "DatPhongDoan",
                        principalColumn: "GroupReservationId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_fo_LichDatCocDoan_CoSo_Doan_Han",
                schema: "fo",
                table: "LichDatCocDoan",
                columns: new[] { "PropertyId", "GroupReservationId", "DueDate" });

            migrationBuilder.CreateIndex(
                name: "IX_fo_LichDatCocDoan_CoSo_TrangThai",
                schema: "fo",
                table: "LichDatCocDoan",
                columns: new[] { "PropertyId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_LichDatCocDoan_GroupReservationId",
                schema: "fo",
                table: "LichDatCocDoan",
                column: "GroupReservationId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LichDatCocDoan",
                schema: "fo");

            migrationBuilder.DropColumn(
                name: "DepositCutoffDate",
                schema: "fo",
                table: "DatPhongDoan");

            migrationBuilder.DropColumn(
                name: "DepositReleaseDate",
                schema: "fo",
                table: "DatPhongDoan");

            migrationBuilder.DropColumn(
                name: "DepositRequired",
                schema: "fo",
                table: "DatPhongDoan");

            migrationBuilder.DropColumn(
                name: "GuaranteeType",
                schema: "fo",
                table: "DatPhongDoan");
        }
    }
}
