using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ASMILE.BO.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddFinancialStatementEngine : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AccountBalanceSnapshots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FiscalYear = table.Column<int>(type: "int", nullable: false),
                    Period = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    AccountCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    OpeningDebit = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    OpeningCredit = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    PeriodDebit = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    PeriodCredit = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ClosingDebit = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ClosingCredit = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    SnapshotDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccountBalanceSnapshots", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ClosingEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FiscalYear = table.Column<int>(type: "int", nullable: false),
                    Period = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ClosingDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    JournalHeaderId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClosingEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClosingEntries_JournalHeaders_JournalHeaderId",
                        column: x => x.JournalHeaderId,
                        principalTable: "JournalHeaders",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "FinancialStatementExecutions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StatementType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    FiscalYear = table.Column<int>(type: "int", nullable: false),
                    Period = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    GeneratedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    GeneratedBy = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    JsonData = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinancialStatementExecutions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ClosingEntryLines",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClosingEntryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AccountCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    DebitAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CreditAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClosingEntryLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClosingEntryLines_ClosingEntries_ClosingEntryId",
                        column: x => x.ClosingEntryId,
                        principalTable: "ClosingEntries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "AccountRoleMappings",
                columns: new[] { "Id", "AccountCode", "AccountingStandardCode", "IsActive", "IsDefault", "RoleCode" },
                values: new object[,]
                {
                    { new Guid("37294164-9c91-b856-8d6f-3a7a8b36ce12"), "911", "TT200", true, true, "IncomeSummary" },
                    { new Guid("88377036-c67d-1495-472b-f508e974f060"), "421", "TT99", true, true, "RetainedEarnings" },
                    { new Guid("909f56ff-dc3c-dfb5-84b9-3d442da55cba"), "421", "TT200", true, true, "RetainedEarnings" },
                    { new Guid("a5dde8ea-9625-82c4-7297-f675d224c6c2"), "911", "TT99", true, true, "IncomeSummary" }
                });

            migrationBuilder.InsertData(
                table: "ReportTemplates",
                columns: new[] { "Id", "Category", "Code", "CreatedAt", "DataSource", "Description", "IsActive", "IsSystem", "LayoutDefinition", "Name", "QueryDefinition", "UpdatedAt" },
                values: new object[,]
                {
                    { new Guid("4a9a7b4f-ac6e-4e91-9a11-000000000009"), "Accounting", "BALANCE_SHEET", new DateTime(2026, 5, 21, 0, 0, 0, 0, DateTimeKind.Utc), "FinancialStatement", "Báo cáo tài sản, nợ phải trả và vốn chủ sở hữu từ Journal persisted.", true, true, null, "Bảng cân đối kế toán", null, new DateTime(2026, 5, 21, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("4a9a7b4f-ac6e-4e91-9a11-000000000010"), "Accounting", "INCOME_STATEMENT", new DateTime(2026, 5, 21, 0, 0, 0, 0, DateTimeKind.Utc), "FinancialStatement", "Báo cáo doanh thu, chi phí và lợi nhuận từ Journal persisted.", true, true, null, "Báo cáo kết quả kinh doanh", null, new DateTime(2026, 5, 21, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("4a9a7b4f-ac6e-4e91-9a11-000000000011"), "Accounting", "CASH_FLOW", new DateTime(2026, 5, 21, 0, 0, 0, 0, DateTimeKind.Utc), "FinancialStatement", "Skeleton báo cáo lưu chuyển tiền tệ từ tài khoản tiền và ngân hàng.", true, true, null, "Báo cáo lưu chuyển tiền tệ", null, new DateTime(2026, 5, 21, 0, 0, 0, 0, DateTimeKind.Utc) }
                });

            migrationBuilder.CreateIndex(
                name: "IX_AccountBalanceSnapshots_AccountCode",
                table: "AccountBalanceSnapshots",
                column: "AccountCode");

            migrationBuilder.CreateIndex(
                name: "IX_AccountBalanceSnapshots_FiscalYear",
                table: "AccountBalanceSnapshots",
                column: "FiscalYear");

            migrationBuilder.CreateIndex(
                name: "IX_AccountBalanceSnapshots_Period",
                table: "AccountBalanceSnapshots",
                column: "Period");

            migrationBuilder.CreateIndex(
                name: "IX_AccountBalanceSnapshots_Period_AccountCode",
                table: "AccountBalanceSnapshots",
                columns: new[] { "Period", "AccountCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ClosingEntries_FiscalYear",
                table: "ClosingEntries",
                column: "FiscalYear");

            migrationBuilder.CreateIndex(
                name: "IX_ClosingEntries_JournalHeaderId",
                table: "ClosingEntries",
                column: "JournalHeaderId");

            migrationBuilder.CreateIndex(
                name: "IX_ClosingEntries_Period",
                table: "ClosingEntries",
                column: "Period");

            migrationBuilder.CreateIndex(
                name: "IX_ClosingEntries_Status",
                table: "ClosingEntries",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ClosingEntryLines_AccountCode",
                table: "ClosingEntryLines",
                column: "AccountCode");

            migrationBuilder.CreateIndex(
                name: "IX_ClosingEntryLines_ClosingEntryId",
                table: "ClosingEntryLines",
                column: "ClosingEntryId");

            migrationBuilder.CreateIndex(
                name: "IX_FinancialStatementExecutions_FiscalYear",
                table: "FinancialStatementExecutions",
                column: "FiscalYear");

            migrationBuilder.CreateIndex(
                name: "IX_FinancialStatementExecutions_Period",
                table: "FinancialStatementExecutions",
                column: "Period");

            migrationBuilder.CreateIndex(
                name: "IX_FinancialStatementExecutions_StatementType",
                table: "FinancialStatementExecutions",
                column: "StatementType");

            migrationBuilder.CreateIndex(
                name: "IX_FinancialStatementExecutions_Status",
                table: "FinancialStatementExecutions",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AccountBalanceSnapshots");

            migrationBuilder.DropTable(
                name: "ClosingEntryLines");

            migrationBuilder.DropTable(
                name: "FinancialStatementExecutions");

            migrationBuilder.DropTable(
                name: "ClosingEntries");

            migrationBuilder.DeleteData(
                table: "AccountRoleMappings",
                keyColumn: "Id",
                keyValue: new Guid("37294164-9c91-b856-8d6f-3a7a8b36ce12"));

            migrationBuilder.DeleteData(
                table: "AccountRoleMappings",
                keyColumn: "Id",
                keyValue: new Guid("88377036-c67d-1495-472b-f508e974f060"));

            migrationBuilder.DeleteData(
                table: "AccountRoleMappings",
                keyColumn: "Id",
                keyValue: new Guid("909f56ff-dc3c-dfb5-84b9-3d442da55cba"));

            migrationBuilder.DeleteData(
                table: "AccountRoleMappings",
                keyColumn: "Id",
                keyValue: new Guid("a5dde8ea-9625-82c4-7297-f675d224c6c2"));

            migrationBuilder.DeleteData(
                table: "ReportTemplates",
                keyColumn: "Id",
                keyValue: new Guid("4a9a7b4f-ac6e-4e91-9a11-000000000009"));

            migrationBuilder.DeleteData(
                table: "ReportTemplates",
                keyColumn: "Id",
                keyValue: new Guid("4a9a7b4f-ac6e-4e91-9a11-000000000010"));

            migrationBuilder.DeleteData(
                table: "ReportTemplates",
                keyColumn: "Id",
                keyValue: new Guid("4a9a7b4f-ac6e-4e91-9a11-000000000011"));
        }
    }
}
