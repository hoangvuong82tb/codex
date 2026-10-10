using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ASMILE.BO.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddHotelDepositAndDiscountPolicies : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ChinhSachChietKhauFO",
                schema: "fo",
                columns: table => new
                {
                    HotelDiscountPolicyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PropertyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    DiscountType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Percentage = table.Column<decimal>(type: "decimal(9,4)", precision: 9, scale: 4, nullable: false),
                    FixedAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    MaximumDiscount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    AllowedRoles = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ApprovalLevel = table.Column<int>(type: "int", nullable: false),
                    ReasonRequired = table.Column<bool>(type: "bit", nullable: false),
                    AccountingTreatment = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: true),
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
                    table.PrimaryKey("PK_fo_ChinhSachChietKhauFO", x => x.HotelDiscountPolicyId);
                });

            migrationBuilder.CreateTable(
                name: "ChinhSachDatCocFO",
                schema: "fo",
                columns: table => new
                {
                    HotelDepositPolicyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PropertyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    DepositType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Required = table.Column<bool>(type: "bit", nullable: false),
                    Percentage = table.Column<decimal>(type: "decimal(9,4)", precision: 9, scale: 4, nullable: false),
                    FixedAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    DueDaysBeforeArrival = table.Column<int>(type: "int", nullable: false),
                    Refundable = table.Column<bool>(type: "bit", nullable: false),
                    Transferable = table.Column<bool>(type: "bit", nullable: false),
                    Forfeitable = table.Column<bool>(type: "bit", nullable: false),
                    AutoReminder = table.Column<bool>(type: "bit", nullable: false),
                    AutoApplyToFolio = table.Column<bool>(type: "bit", nullable: false),
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
                    table.PrimaryKey("PK_fo_ChinhSachDatCocFO", x => x.HotelDepositPolicyId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_fo_ChinhSachChietKhauFO_CoSo_Loai",
                schema: "fo",
                table: "ChinhSachChietKhauFO",
                columns: new[] { "PropertyId", "DiscountType", "EffectiveFrom", "EffectiveTo", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "UQ_fo_ChinhSachChietKhauFO_CoSo_Ma",
                schema: "fo",
                table: "ChinhSachChietKhauFO",
                columns: new[] { "PropertyId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_fo_ChinhSachDatCocFO_CoSo_Loai",
                schema: "fo",
                table: "ChinhSachDatCocFO",
                columns: new[] { "PropertyId", "DepositType", "EffectiveFrom", "EffectiveTo", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "UQ_fo_ChinhSachDatCocFO_CoSo_Ma",
                schema: "fo",
                table: "ChinhSachDatCocFO",
                columns: new[] { "PropertyId", "Code" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChinhSachChietKhauFO",
                schema: "fo");

            migrationBuilder.DropTable(
                name: "ChinhSachDatCocFO",
                schema: "fo");
        }
    }
}
