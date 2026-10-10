using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ASMILE.BO.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCashReconciliation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DoiSoatTienMat",
                schema: "fo",
                columns: table => new
                {
                    CashReconciliationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PropertyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CashierShiftId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ShiftNumber = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    ExpectedCash = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ActualCash = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Variance = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    ReasonCode = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    ReasonDetail = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ReviewedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ReviewedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ApprovedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ApprovedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fo_DoiSoatTienMat", x => x.CashReconciliationId);
                    table.ForeignKey(
                        name: "FK_DoiSoatTienMat_CaLamViec_CashierShiftId",
                        column: x => x.CashierShiftId,
                        principalSchema: "fo",
                        principalTable: "CaLamViec",
                        principalColumn: "CashierShiftId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DoiSoatTienMat_CashierShiftId",
                schema: "fo",
                table: "DoiSoatTienMat",
                column: "CashierShiftId");

            migrationBuilder.CreateIndex(
                name: "IX_fo_DoiSoatTienMat_CoSo_TrangThai",
                schema: "fo",
                table: "DoiSoatTienMat",
                columns: new[] { "PropertyId", "Status" });

            migrationBuilder.CreateIndex(
                name: "UQ_fo_DoiSoatTienMat_CoSo_Ca",
                schema: "fo",
                table: "DoiSoatTienMat",
                columns: new[] { "PropertyId", "CashierShiftId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DoiSoatTienMat",
                schema: "fo");
        }
    }
}
