using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ASMILE.BO.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEInvoiceAdjustmentPersistence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AdjustmentReason",
                table: "Invoices",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AdjustmentType",
                table: "Invoices",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "AdjustsInvoiceId",
                table: "Invoices",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_AdjustsInvoiceId",
                table: "Invoices",
                column: "AdjustsInvoiceId");

            migrationBuilder.AddForeignKey(
                name: "FK_Invoices_Invoices_AdjustsInvoiceId",
                table: "Invoices",
                column: "AdjustsInvoiceId",
                principalTable: "Invoices",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Invoices_Invoices_AdjustsInvoiceId",
                table: "Invoices");

            migrationBuilder.DropIndex(
                name: "IX_Invoices_AdjustsInvoiceId",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "AdjustmentReason",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "AdjustmentType",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "AdjustsInvoiceId",
                table: "Invoices");
        }
    }
}
