using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ASMILE.BO.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddHotelPaymentMethodProfiles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PhuongThucThanhToanFO",
                schema: "fo",
                columns: table => new
                {
                    HotelPaymentMethodProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PropertyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    Kind = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    CurrencyCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    BankAccount = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: true),
                    CardTerminal = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    FeeRate = table.Column<decimal>(type: "decimal(9,4)", precision: 9, scale: 4, nullable: false),
                    AccountingMapping = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: true),
                    RequireReference = table.Column<bool>(type: "bit", nullable: false),
                    RequireApproval = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fo_PhuongThucThanhToanFO", x => x.HotelPaymentMethodProfileId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_fo_PhuongThucThanhToanFO_CoSo_Loai",
                schema: "fo",
                table: "PhuongThucThanhToanFO",
                columns: new[] { "PropertyId", "Kind", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "UQ_fo_PhuongThucThanhToanFO_CoSo_Ma",
                schema: "fo",
                table: "PhuongThucThanhToanFO",
                columns: new[] { "PropertyId", "Code" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PhuongThucThanhToanFO",
                schema: "fo");
        }
    }
}
