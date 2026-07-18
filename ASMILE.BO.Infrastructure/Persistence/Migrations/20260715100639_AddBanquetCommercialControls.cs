using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ASMILE.BO.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddBanquetCommercialControls : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_BanquetDeposits_BanquetEventId",
                table: "BanquetDeposits");

            migrationBuilder.AddColumn<DateTime>(
                name: "FinanciallyLockedAt",
                table: "BanquetEvents",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FinanciallyLockedBy",
                table: "BanquetEvents",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsFinanciallyLocked",
                table: "BanquetEvents",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "BanquetEvents",
                type: "rowversion",
                rowVersion: true,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IdempotencyKey",
                table: "BanquetDeposits",
                type: "nvarchar(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "RefundAmount",
                table: "BanquetDeposits",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<Guid>(
                name: "RefundCashTransactionId",
                table: "BanquetDeposits",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RefundCashTransactionNo",
                table: "BanquetDeposits",
                type: "nvarchar(60)",
                maxLength: 60,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RefundReason",
                table: "BanquetDeposits",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RefundedAt",
                table: "BanquetDeposits",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RefundedBy",
                table: "BanquetDeposits",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "BanquetDeposits",
                type: "rowversion",
                rowVersion: true,
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "BanquetContracts",
                type: "rowversion",
                rowVersion: true,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_BanquetDeposits_BanquetEventId_IdempotencyKey",
                table: "BanquetDeposits",
                columns: new[] { "BanquetEventId", "IdempotencyKey" },
                unique: true,
                filter: "[IdempotencyKey] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_BanquetDeposits_RefundCashTransactionId",
                table: "BanquetDeposits",
                column: "RefundCashTransactionId");

            migrationBuilder.AddForeignKey(
                name: "FK_BanquetDeposits_CashTransactions_RefundCashTransactionId",
                table: "BanquetDeposits",
                column: "RefundCashTransactionId",
                principalTable: "CashTransactions",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BanquetDeposits_CashTransactions_RefundCashTransactionId",
                table: "BanquetDeposits");

            migrationBuilder.DropIndex(
                name: "IX_BanquetDeposits_BanquetEventId_IdempotencyKey",
                table: "BanquetDeposits");

            migrationBuilder.DropIndex(
                name: "IX_BanquetDeposits_RefundCashTransactionId",
                table: "BanquetDeposits");

            migrationBuilder.DropColumn(
                name: "FinanciallyLockedAt",
                table: "BanquetEvents");

            migrationBuilder.DropColumn(
                name: "FinanciallyLockedBy",
                table: "BanquetEvents");

            migrationBuilder.DropColumn(
                name: "IsFinanciallyLocked",
                table: "BanquetEvents");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "BanquetEvents");

            migrationBuilder.DropColumn(
                name: "IdempotencyKey",
                table: "BanquetDeposits");

            migrationBuilder.DropColumn(
                name: "RefundAmount",
                table: "BanquetDeposits");

            migrationBuilder.DropColumn(
                name: "RefundCashTransactionId",
                table: "BanquetDeposits");

            migrationBuilder.DropColumn(
                name: "RefundCashTransactionNo",
                table: "BanquetDeposits");

            migrationBuilder.DropColumn(
                name: "RefundReason",
                table: "BanquetDeposits");

            migrationBuilder.DropColumn(
                name: "RefundedAt",
                table: "BanquetDeposits");

            migrationBuilder.DropColumn(
                name: "RefundedBy",
                table: "BanquetDeposits");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "BanquetDeposits");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "BanquetContracts");

            migrationBuilder.CreateIndex(
                name: "IX_BanquetDeposits_BanquetEventId",
                table: "BanquetDeposits",
                column: "BanquetEventId");
        }
    }
}
