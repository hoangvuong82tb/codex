using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ASMILE.BO.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddGroupDeposits : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DatCocDoan",
                schema: "fo",
                columns: table => new
                {
                    GroupDepositId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PropertyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GroupReservationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CurrencyCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    PaymentMethod = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    ExternalReference = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    BusinessDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ReversedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReversedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ReversalReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fo_DatCocDoan", x => x.GroupDepositId);
                    table.ForeignKey(
                        name: "FK_DatCocDoan_DatPhongDoan_GroupReservationId",
                        column: x => x.GroupReservationId,
                        principalSchema: "fo",
                        principalTable: "DatPhongDoan",
                        principalColumn: "GroupReservationId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DatCocDoan_GroupReservationId",
                schema: "fo",
                table: "DatCocDoan",
                column: "GroupReservationId");

            migrationBuilder.CreateIndex(
                name: "IX_fo_DatCocDoan_CoSo_Doan_Ngay",
                schema: "fo",
                table: "DatCocDoan",
                columns: new[] { "PropertyId", "GroupReservationId", "BusinessDate" });

            migrationBuilder.CreateIndex(
                name: "IX_fo_DatCocDoan_CoSo_TrangThai",
                schema: "fo",
                table: "DatCocDoan",
                columns: new[] { "PropertyId", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DatCocDoan",
                schema: "fo");
        }
    }
}
