using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;

#nullable disable

namespace ASMILE.BO.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(AppDbContext))]
    [Migration("20260527091500_AddStockReceiptDetailVatColumns")]
    public partial class AddStockReceiptDetailVatColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF COL_LENGTH('StockReceiptDetails', 'VatAmount') IS NULL
                    ALTER TABLE [StockReceiptDetails] ADD [VatAmount] decimal(18,2) NOT NULL CONSTRAINT [DF_StockReceiptDetails_VatAmount] DEFAULT 0;
                """);

            migrationBuilder.Sql("""
                IF COL_LENGTH('StockReceiptDetails', 'VatRate') IS NULL
                    ALTER TABLE [StockReceiptDetails] ADD [VatRate] decimal(5,2) NOT NULL CONSTRAINT [DF_StockReceiptDetails_VatRate] DEFAULT 0;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "VatAmount",
                table: "StockReceiptDetails");

            migrationBuilder.DropColumn(
                name: "VatRate",
                table: "StockReceiptDetails");
        }
    }
}
