using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ASMILE.BO.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddStockVoucherPosting : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "JournalHeaderId",
                table: "StockReceipts",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PostedAt",
                table: "StockReceipts",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PostedBy",
                table: "StockReceipts",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PostingStatus",
                table: "StockReceipts",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "JournalHeaderId",
                table: "StockIssues",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PostedAt",
                table: "StockIssues",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PostedBy",
                table: "StockIssues",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PostingStatus",
                table: "StockIssues",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.InsertData(
                table: "AccountRoleMappings",
                columns: new[] { "Id", "AccountCode", "AccountingStandardCode", "IsActive", "IsDefault", "RoleCode" },
                values: new object[,]
                {
                    { new Guid("6f6b8f41-9c2d-4d66-9b2f-000000000156"), "156", "TT99", true, true, "Inventory" },
                    { new Guid("6f6b8f41-9c2d-4d66-9b2f-000000000331"), "331", "TT99", true, true, "Payable" },
                    { new Guid("6f6b8f41-9c2d-4d66-9b2f-000000000632"), "632", "TT99", true, true, "COGS" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_StockReceipts_JournalHeaderId",
                table: "StockReceipts",
                column: "JournalHeaderId");

            migrationBuilder.CreateIndex(
                name: "IX_StockIssues_JournalHeaderId",
                table: "StockIssues",
                column: "JournalHeaderId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_StockReceipts_JournalHeaderId",
                table: "StockReceipts");

            migrationBuilder.DropIndex(
                name: "IX_StockIssues_JournalHeaderId",
                table: "StockIssues");

            migrationBuilder.DeleteData(
                table: "AccountRoleMappings",
                keyColumn: "Id",
                keyValue: new Guid("6f6b8f41-9c2d-4d66-9b2f-000000000156"));

            migrationBuilder.DeleteData(
                table: "AccountRoleMappings",
                keyColumn: "Id",
                keyValue: new Guid("6f6b8f41-9c2d-4d66-9b2f-000000000331"));

            migrationBuilder.DeleteData(
                table: "AccountRoleMappings",
                keyColumn: "Id",
                keyValue: new Guid("6f6b8f41-9c2d-4d66-9b2f-000000000632"));

            migrationBuilder.DropColumn(
                name: "JournalHeaderId",
                table: "StockReceipts");

            migrationBuilder.DropColumn(
                name: "PostedAt",
                table: "StockReceipts");

            migrationBuilder.DropColumn(
                name: "PostedBy",
                table: "StockReceipts");

            migrationBuilder.DropColumn(
                name: "PostingStatus",
                table: "StockReceipts");

            migrationBuilder.DropColumn(
                name: "JournalHeaderId",
                table: "StockIssues");

            migrationBuilder.DropColumn(
                name: "PostedAt",
                table: "StockIssues");

            migrationBuilder.DropColumn(
                name: "PostedBy",
                table: "StockIssues");

            migrationBuilder.DropColumn(
                name: "PostingStatus",
                table: "StockIssues");
        }
    }
}
