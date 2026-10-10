using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ASMILE.BO.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddHotelChargeCodeProfiles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ChargeCodeFO",
                schema: "fo",
                columns: table => new
                {
                    HotelChargeCodeProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PropertyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Unit = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Price = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TaxCode = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    ServiceChargeCode = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    RevenueDepartment = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    RevenueAccount = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    TaxAccount = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    CostCenter = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    AllowManualPosting = table.Column<bool>(type: "bit", nullable: false),
                    AllowDiscount = table.Column<bool>(type: "bit", nullable: false),
                    AllowVoid = table.Column<bool>(type: "bit", nullable: false),
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
                    table.PrimaryKey("PK_fo_ChargeCodeFO", x => x.HotelChargeCodeProfileId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_fo_ChargeCodeFO_CoSo_Nhom",
                schema: "fo",
                table: "ChargeCodeFO",
                columns: new[] { "PropertyId", "Category", "EffectiveFrom", "EffectiveTo", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "UQ_fo_ChargeCodeFO_CoSo_Ma",
                schema: "fo",
                table: "ChargeCodeFO",
                columns: new[] { "PropertyId", "Code" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChargeCodeFO",
                schema: "fo");
        }
    }
}
