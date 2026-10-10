using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ASMILE.BO.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddHotelTaxServiceChargeRules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "QuyTacThuePhiDichVuFO",
                schema: "fo",
                columns: table => new
                {
                    HotelTaxServiceChargeRuleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PropertyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Kind = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Code = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    Rate = table.Column<decimal>(type: "decimal(9,4)", precision: 9, scale: 4, nullable: false),
                    IsInclusive = table.Column<bool>(type: "bit", nullable: false),
                    ApplicableService = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    ApplicableRatePlan = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    RoundingMode = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    AccountingMapping = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: true),
                    EffectiveFrom = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EffectiveTo = table.Column<DateTime>(type: "datetime2", nullable: false),
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
                    table.PrimaryKey("PK_fo_QuyTacThuePhiDichVuFO", x => x.HotelTaxServiceChargeRuleId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_fo_QuyTacThuePhiDichVuFO_CoSo_HieuLuc",
                schema: "fo",
                table: "QuyTacThuePhiDichVuFO",
                columns: new[] { "PropertyId", "Kind", "EffectiveFrom", "EffectiveTo", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "UQ_fo_QuyTacThuePhiDichVuFO_CoSo_Loai_Ma",
                schema: "fo",
                table: "QuyTacThuePhiDichVuFO",
                columns: new[] { "PropertyId", "Kind", "Code" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "QuyTacThuePhiDichVuFO",
                schema: "fo");
        }
    }
}
