using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ASMILE.BO.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddBanquetFloorPlanAndDepositReceipt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LayoutJson",
                table: "BanquetFloorPlans",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CashTransactionId",
                table: "BanquetDeposits",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CashTransactionNo",
                table: "BanquetDeposits",
                type: "nvarchar(60)",
                maxLength: 60,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_BanquetDeposits_CashTransactionId",
                table: "BanquetDeposits",
                column: "CashTransactionId");

            migrationBuilder.AddForeignKey(
                name: "FK_BanquetDeposits_CashTransactions_CashTransactionId",
                table: "BanquetDeposits",
                column: "CashTransactionId",
                principalTable: "CashTransactions",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BanquetDeposits_CashTransactions_CashTransactionId",
                table: "BanquetDeposits");

            migrationBuilder.DropIndex(
                name: "IX_BanquetDeposits_CashTransactionId",
                table: "BanquetDeposits");

            migrationBuilder.DropColumn(
                name: "LayoutJson",
                table: "BanquetFloorPlans");

            migrationBuilder.DropColumn(
                name: "CashTransactionId",
                table: "BanquetDeposits");

            migrationBuilder.DropColumn(
                name: "CashTransactionNo",
                table: "BanquetDeposits");
        }
    }
}
