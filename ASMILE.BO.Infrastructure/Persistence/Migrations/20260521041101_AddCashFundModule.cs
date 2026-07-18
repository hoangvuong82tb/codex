using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ASMILE.BO.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCashFundModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CashFunds",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FundCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    FundName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    CurrencyCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false, defaultValue: "VND"),
                    AccountCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    WarehouseId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DepartmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ResponsiblePersonId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    OpeningBalance = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CurrentBalance = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CashFunds", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CashCounts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CashFundId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CountNo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CountDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    BookBalance = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ActualBalance = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Difference = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Note = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    ConfirmedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ConfirmedBy = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CashCounts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CashCounts_CashFunds_CashFundId",
                        column: x => x.CashFundId,
                        principalTable: "CashFunds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CashTransactions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CashFundId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TransactionNo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    TransactionDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TransactionType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    SourceType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    SourceId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    JournalHeaderId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PartnerType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    PartnerId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CounterAccountCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    DebitAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CreditAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    BalanceAfter = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    PostedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PostedBy = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CashTransactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CashTransactions_CashFunds_CashFundId",
                        column: x => x.CashFundId,
                        principalTable: "CashFunds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CashTransactions_JournalHeaders_JournalHeaderId",
                        column: x => x.JournalHeaderId,
                        principalTable: "JournalHeaders",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "CashCountLines",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CashCountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Denomination = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CashCountLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CashCountLines_CashCounts_CashCountId",
                        column: x => x.CashCountId,
                        principalTable: "CashCounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "CashFunds",
                columns: new[] { "Id", "AccountCode", "CreatedAt", "CurrencyCode", "CurrentBalance", "DepartmentId", "FundCode", "FundName", "IsActive", "IsDefault", "OpeningBalance", "ResponsiblePersonId", "UpdatedAt", "WarehouseId" },
                values: new object[] { new Guid("7ac7ad91-6e6a-4b2c-9f9e-000000000001"), "1111", new DateTime(2026, 5, 21, 0, 0, 0, 0, DateTimeKind.Utc), "VND", 0m, null, "CASH_MAIN", "Quỹ tiền mặt chính", true, true, 0m, null, new DateTime(2026, 5, 21, 0, 0, 0, 0, DateTimeKind.Utc), null });

            migrationBuilder.CreateIndex(
                name: "IX_CashCountLines_CashCountId",
                table: "CashCountLines",
                column: "CashCountId");

            migrationBuilder.CreateIndex(
                name: "IX_CashCounts_CashFundId",
                table: "CashCounts",
                column: "CashFundId");

            migrationBuilder.CreateIndex(
                name: "IX_CashCounts_CountDate",
                table: "CashCounts",
                column: "CountDate");

            migrationBuilder.CreateIndex(
                name: "IX_CashCounts_CountNo",
                table: "CashCounts",
                column: "CountNo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CashCounts_Status",
                table: "CashCounts",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_CashFunds_FundCode",
                table: "CashFunds",
                column: "FundCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CashFunds_IsDefault",
                table: "CashFunds",
                column: "IsDefault");

            migrationBuilder.CreateIndex(
                name: "IX_CashTransactions_CashFundId",
                table: "CashTransactions",
                column: "CashFundId");

            migrationBuilder.CreateIndex(
                name: "IX_CashTransactions_JournalHeaderId",
                table: "CashTransactions",
                column: "JournalHeaderId");

            migrationBuilder.CreateIndex(
                name: "IX_CashTransactions_SourceType_SourceId",
                table: "CashTransactions",
                columns: new[] { "SourceType", "SourceId" });

            migrationBuilder.CreateIndex(
                name: "IX_CashTransactions_Status",
                table: "CashTransactions",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_CashTransactions_TransactionDate",
                table: "CashTransactions",
                column: "TransactionDate");

            migrationBuilder.CreateIndex(
                name: "IX_CashTransactions_TransactionNo",
                table: "CashTransactions",
                column: "TransactionNo",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CashCountLines");

            migrationBuilder.DropTable(
                name: "CashTransactions");

            migrationBuilder.DropTable(
                name: "CashCounts");

            migrationBuilder.DropTable(
                name: "CashFunds");
        }
    }
}
