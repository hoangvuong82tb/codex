using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ASMILE.BO.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMultiCompanyFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_UserWorkspacePreferences_UserId_Category_PreferenceKey",
                table: "UserWorkspacePreferences");

            migrationBuilder.DropIndex(
                name: "IX_AppUserPermissions_UserId_PermissionCode",
                table: "AppUserPermissions");

            migrationBuilder.AddColumn<Guid>(
                name: "BranchId",
                table: "VatTransactions",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CompanyId",
                table: "VatTransactions",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "BranchId",
                table: "UserWorkspacePreferences",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CompanyId",
                table: "UserWorkspacePreferences",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "BranchId",
                table: "ToolRecoveries",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CompanyId",
                table: "ToolRecoveries",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "BranchId",
                table: "ToolIssues",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CompanyId",
                table: "ToolIssues",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "BranchId",
                table: "ToolDisposals",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CompanyId",
                table: "ToolDisposals",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "BranchId",
                table: "ToolAssets",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CompanyId",
                table: "ToolAssets",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CompanyId",
                table: "ToolAssetCategories",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "BranchId",
                table: "ToolAllocationSchedules",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CompanyId",
                table: "ToolAllocationSchedules",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "BranchId",
                table: "ToolAllocationRuns",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CompanyId",
                table: "ToolAllocationRuns",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "BranchId",
                table: "TaxDeclarationDrafts",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CompanyId",
                table: "TaxDeclarationDrafts",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CompanyId",
                table: "TaxCodes",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "BranchId",
                table: "StockReceipts",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CompanyId",
                table: "StockReceipts",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "BranchId",
                table: "StockReceiptDetails",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CompanyId",
                table: "StockReceiptDetails",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "BranchId",
                table: "StockIssues",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CompanyId",
                table: "StockIssues",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "BranchId",
                table: "StockIssueDetails",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CompanyId",
                table: "StockIssueDetails",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "BranchId",
                table: "StockBalances",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CompanyId",
                table: "StockBalances",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CompanyId",
                table: "ReportTemplates",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CompanyId",
                table: "ReportParameters",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "BranchId",
                table: "ReportExecutions",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CompanyId",
                table: "ReportExecutions",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CompanyId",
                table: "ReportDefinitions",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "BranchId",
                table: "ReceivableTransactions",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CompanyId",
                table: "ReceivableTransactions",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "BranchId",
                table: "PostingSources",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CompanyId",
                table: "PostingSources",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "BranchId",
                table: "PostingBatches",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CompanyId",
                table: "PostingBatches",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "BranchId",
                table: "PaymentAllocations",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CompanyId",
                table: "PaymentAllocations",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "BranchId",
                table: "PayableTransactions",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CompanyId",
                table: "PayableTransactions",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "BranchId",
                table: "JournalLines",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CompanyId",
                table: "JournalLines",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "BranchId",
                table: "JournalHeaders",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CompanyId",
                table: "JournalHeaders",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "BranchId",
                table: "InventoryCostTransactions",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CompanyId",
                table: "InventoryCostTransactions",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "BranchId",
                table: "InventoryCostLayers",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CompanyId",
                table: "InventoryCostLayers",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "BranchId",
                table: "FixedAssets",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CompanyId",
                table: "FixedAssets",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "BranchId",
                table: "FixedAssetDisposals",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CompanyId",
                table: "FixedAssetDisposals",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "BranchId",
                table: "FixedAssetDepreciations",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CompanyId",
                table: "FixedAssetDepreciations",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CompanyId",
                table: "FixedAssetCategories",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "BranchId",
                table: "FixedAssetAdjustments",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CompanyId",
                table: "FixedAssetAdjustments",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CompanyId",
                table: "FinancialStatementMappings",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "BranchId",
                table: "FinancialStatementExecutions",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CompanyId",
                table: "FinancialStatementExecutions",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "BranchId",
                table: "DebtReconciliations",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CompanyId",
                table: "DebtReconciliations",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "BranchId",
                table: "DebtReconciliationLines",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CompanyId",
                table: "DebtReconciliationLines",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "BranchId",
                table: "ClosingEntryLines",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CompanyId",
                table: "ClosingEntryLines",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "BranchId",
                table: "ClosingEntries",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CompanyId",
                table: "ClosingEntries",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "BranchId",
                table: "CashTransactions",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CompanyId",
                table: "CashTransactions",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "BranchId",
                table: "CashFunds",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CompanyId",
                table: "CashFunds",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "BranchId",
                table: "CashCounts",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CompanyId",
                table: "CashCounts",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "BranchId",
                table: "CashCountLines",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CompanyId",
                table: "CashCountLines",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "BranchId",
                table: "BankTransactions",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CompanyId",
                table: "BankTransactions",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "BranchId",
                table: "BankStatements",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CompanyId",
                table: "BankStatements",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "BranchId",
                table: "BankStatementLines",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CompanyId",
                table: "BankStatementLines",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "BranchId",
                table: "BankReconciliations",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CompanyId",
                table: "BankReconciliations",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "BranchId",
                table: "BankReconciliationLines",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CompanyId",
                table: "BankReconciliationLines",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "BranchId",
                table: "BankAccounts",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CompanyId",
                table: "BankAccounts",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "BranchId",
                table: "AuditLogs",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CompanyId",
                table: "AuditLogs",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "BranchId",
                table: "AppUserPermissions",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CompanyId",
                table: "AppUserPermissions",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "BranchId",
                table: "AgingSnapshots",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CompanyId",
                table: "AgingSnapshots",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "BranchId",
                table: "AccountBalanceSnapshots",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CompanyId",
                table: "AccountBalanceSnapshots",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Companies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    TaxCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Address = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Companies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserBranchAccesses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserBranchAccesses", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserCompanyAccesses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserCompanyAccesses", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Branches",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Address = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Branches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Branches_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.UpdateData(
                table: "BankAccounts",
                keyColumn: "Id",
                keyValue: new Guid("8e4d05d8-0a7e-4e23-9f5a-000000000001"),
                columns: new[] { "BranchId", "CompanyId" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "CashFunds",
                keyColumn: "Id",
                keyValue: new Guid("7ac7ad91-6e6a-4b2c-9f9e-000000000001"),
                columns: new[] { "BranchId", "CompanyId" },
                values: new object[] { null, null });

            migrationBuilder.InsertData(
                table: "Companies",
                columns: new[] { "Id", "Address", "Code", "CreatedAt", "IsActive", "IsDefault", "Name", "TaxCode", "UpdatedAt" },
                values: new object[] { new Guid("11111111-1111-1111-1111-111111111111"), null, "DEFAULT", new DateTime(2026, 5, 22, 0, 0, 0, 0, DateTimeKind.Utc), true, true, "Default Company", null, new DateTime(2026, 5, 22, 0, 0, 0, 0, DateTimeKind.Utc) });

            migrationBuilder.UpdateData(
                table: "FinancialStatementMappings",
                keyColumn: "Id",
                keyValue: new Guid("0182d2aa-0a17-791a-cb65-a65e5effab7a"),
                column: "CompanyId",
                value: null);

            migrationBuilder.UpdateData(
                table: "FinancialStatementMappings",
                keyColumn: "Id",
                keyValue: new Guid("1009532d-729d-8d6e-4f9a-87f4bb281c74"),
                column: "CompanyId",
                value: null);

            migrationBuilder.UpdateData(
                table: "FinancialStatementMappings",
                keyColumn: "Id",
                keyValue: new Guid("15c78b04-6e79-d552-26a4-e73c1cf809dc"),
                column: "CompanyId",
                value: null);

            migrationBuilder.UpdateData(
                table: "FinancialStatementMappings",
                keyColumn: "Id",
                keyValue: new Guid("1838b8af-4691-772a-eca2-4397a2d490c1"),
                column: "CompanyId",
                value: null);

            migrationBuilder.UpdateData(
                table: "FinancialStatementMappings",
                keyColumn: "Id",
                keyValue: new Guid("21246897-c179-8a44-e21f-47b6ffdd43e5"),
                column: "CompanyId",
                value: null);

            migrationBuilder.UpdateData(
                table: "FinancialStatementMappings",
                keyColumn: "Id",
                keyValue: new Guid("292432f7-5788-bec1-ce3f-cc814025844d"),
                column: "CompanyId",
                value: null);

            migrationBuilder.UpdateData(
                table: "FinancialStatementMappings",
                keyColumn: "Id",
                keyValue: new Guid("37e6948c-6831-61c8-e055-7e3af262d316"),
                column: "CompanyId",
                value: null);

            migrationBuilder.UpdateData(
                table: "FinancialStatementMappings",
                keyColumn: "Id",
                keyValue: new Guid("47ed997b-5f57-b22d-5b54-8309c419f29a"),
                column: "CompanyId",
                value: null);

            migrationBuilder.UpdateData(
                table: "FinancialStatementMappings",
                keyColumn: "Id",
                keyValue: new Guid("5076eb2e-81f8-607a-5b50-5b8c1b736d2e"),
                column: "CompanyId",
                value: null);

            migrationBuilder.UpdateData(
                table: "FinancialStatementMappings",
                keyColumn: "Id",
                keyValue: new Guid("52fadcac-2053-01a8-eaf9-9ba6d77714b6"),
                column: "CompanyId",
                value: null);

            migrationBuilder.UpdateData(
                table: "FinancialStatementMappings",
                keyColumn: "Id",
                keyValue: new Guid("576f7773-3663-768b-d830-d8daf2730795"),
                column: "CompanyId",
                value: null);

            migrationBuilder.UpdateData(
                table: "FinancialStatementMappings",
                keyColumn: "Id",
                keyValue: new Guid("5f7d1620-a49e-71c0-971f-877e63950596"),
                column: "CompanyId",
                value: null);

            migrationBuilder.UpdateData(
                table: "FinancialStatementMappings",
                keyColumn: "Id",
                keyValue: new Guid("67a4801b-73cb-bd68-0f70-38712b9569d8"),
                column: "CompanyId",
                value: null);

            migrationBuilder.UpdateData(
                table: "FinancialStatementMappings",
                keyColumn: "Id",
                keyValue: new Guid("681a72d7-516b-6fda-a9aa-faa59cc0bc4e"),
                column: "CompanyId",
                value: null);

            migrationBuilder.UpdateData(
                table: "FinancialStatementMappings",
                keyColumn: "Id",
                keyValue: new Guid("87bee6eb-e9c5-98e0-e3c3-9580228dd031"),
                column: "CompanyId",
                value: null);

            migrationBuilder.UpdateData(
                table: "FinancialStatementMappings",
                keyColumn: "Id",
                keyValue: new Guid("8b9d58dd-6cac-a0a1-6911-c53349006327"),
                column: "CompanyId",
                value: null);

            migrationBuilder.UpdateData(
                table: "FinancialStatementMappings",
                keyColumn: "Id",
                keyValue: new Guid("8e4e72ce-02d5-6fa7-0940-b8272443cc54"),
                column: "CompanyId",
                value: null);

            migrationBuilder.UpdateData(
                table: "FinancialStatementMappings",
                keyColumn: "Id",
                keyValue: new Guid("95543140-a7a5-97a2-aa3a-0fb8617e029e"),
                column: "CompanyId",
                value: null);

            migrationBuilder.UpdateData(
                table: "FinancialStatementMappings",
                keyColumn: "Id",
                keyValue: new Guid("964494b2-cedd-7c14-8d9e-5b6c74803097"),
                column: "CompanyId",
                value: null);

            migrationBuilder.UpdateData(
                table: "FinancialStatementMappings",
                keyColumn: "Id",
                keyValue: new Guid("9bf102f0-400d-d6dd-61de-802b0559b105"),
                column: "CompanyId",
                value: null);

            migrationBuilder.UpdateData(
                table: "FinancialStatementMappings",
                keyColumn: "Id",
                keyValue: new Guid("a8c61726-fe70-dfbf-a3ca-ea80cf36f2d4"),
                column: "CompanyId",
                value: null);

            migrationBuilder.UpdateData(
                table: "FinancialStatementMappings",
                keyColumn: "Id",
                keyValue: new Guid("aed70647-f9d1-5a24-72f0-f9d955f00538"),
                column: "CompanyId",
                value: null);

            migrationBuilder.UpdateData(
                table: "FinancialStatementMappings",
                keyColumn: "Id",
                keyValue: new Guid("b7188cbd-d96b-030b-ae96-22ead1485ce9"),
                column: "CompanyId",
                value: null);

            migrationBuilder.UpdateData(
                table: "FinancialStatementMappings",
                keyColumn: "Id",
                keyValue: new Guid("c7f9d121-abca-c78a-23fd-441cd5af9d6e"),
                column: "CompanyId",
                value: null);

            migrationBuilder.UpdateData(
                table: "FinancialStatementMappings",
                keyColumn: "Id",
                keyValue: new Guid("d16f401a-9c3e-60b9-5e93-8ec4b1f838ac"),
                column: "CompanyId",
                value: null);

            migrationBuilder.UpdateData(
                table: "FinancialStatementMappings",
                keyColumn: "Id",
                keyValue: new Guid("d3228ff9-4a20-e563-3fef-cd5472f0829f"),
                column: "CompanyId",
                value: null);

            migrationBuilder.UpdateData(
                table: "FinancialStatementMappings",
                keyColumn: "Id",
                keyValue: new Guid("d8f4f33e-6371-14fb-d0c2-1529ea42943a"),
                column: "CompanyId",
                value: null);

            migrationBuilder.UpdateData(
                table: "FinancialStatementMappings",
                keyColumn: "Id",
                keyValue: new Guid("dc32a6b1-e3d6-025d-c7da-e53e70974dd3"),
                column: "CompanyId",
                value: null);

            migrationBuilder.UpdateData(
                table: "FinancialStatementMappings",
                keyColumn: "Id",
                keyValue: new Guid("df4ca685-8a95-fa2e-bc7e-499eddc58e9b"),
                column: "CompanyId",
                value: null);

            migrationBuilder.UpdateData(
                table: "FinancialStatementMappings",
                keyColumn: "Id",
                keyValue: new Guid("e9ee0684-7722-ab0b-637c-08f890529513"),
                column: "CompanyId",
                value: null);

            migrationBuilder.UpdateData(
                table: "FixedAssetCategories",
                keyColumn: "Id",
                keyValue: new Guid("9a71a150-2a6a-4e72-9b25-000000000001"),
                column: "CompanyId",
                value: null);

            migrationBuilder.UpdateData(
                table: "ReportTemplates",
                keyColumn: "Id",
                keyValue: new Guid("4a9a7b4f-ac6e-4e91-9a11-000000000001"),
                column: "CompanyId",
                value: null);

            migrationBuilder.UpdateData(
                table: "ReportTemplates",
                keyColumn: "Id",
                keyValue: new Guid("4a9a7b4f-ac6e-4e91-9a11-000000000002"),
                column: "CompanyId",
                value: null);

            migrationBuilder.UpdateData(
                table: "ReportTemplates",
                keyColumn: "Id",
                keyValue: new Guid("4a9a7b4f-ac6e-4e91-9a11-000000000003"),
                column: "CompanyId",
                value: null);

            migrationBuilder.UpdateData(
                table: "ReportTemplates",
                keyColumn: "Id",
                keyValue: new Guid("4a9a7b4f-ac6e-4e91-9a11-000000000004"),
                column: "CompanyId",
                value: null);

            migrationBuilder.UpdateData(
                table: "ReportTemplates",
                keyColumn: "Id",
                keyValue: new Guid("4a9a7b4f-ac6e-4e91-9a11-000000000005"),
                column: "CompanyId",
                value: null);

            migrationBuilder.UpdateData(
                table: "ReportTemplates",
                keyColumn: "Id",
                keyValue: new Guid("4a9a7b4f-ac6e-4e91-9a11-000000000006"),
                column: "CompanyId",
                value: null);

            migrationBuilder.UpdateData(
                table: "ReportTemplates",
                keyColumn: "Id",
                keyValue: new Guid("4a9a7b4f-ac6e-4e91-9a11-000000000007"),
                column: "CompanyId",
                value: null);

            migrationBuilder.UpdateData(
                table: "ReportTemplates",
                keyColumn: "Id",
                keyValue: new Guid("4a9a7b4f-ac6e-4e91-9a11-000000000008"),
                column: "CompanyId",
                value: null);

            migrationBuilder.UpdateData(
                table: "ReportTemplates",
                keyColumn: "Id",
                keyValue: new Guid("4a9a7b4f-ac6e-4e91-9a11-000000000009"),
                column: "CompanyId",
                value: null);

            migrationBuilder.UpdateData(
                table: "ReportTemplates",
                keyColumn: "Id",
                keyValue: new Guid("4a9a7b4f-ac6e-4e91-9a11-000000000010"),
                column: "CompanyId",
                value: null);

            migrationBuilder.UpdateData(
                table: "ReportTemplates",
                keyColumn: "Id",
                keyValue: new Guid("4a9a7b4f-ac6e-4e91-9a11-000000000011"),
                column: "CompanyId",
                value: null);

            migrationBuilder.UpdateData(
                table: "TaxCodes",
                keyColumn: "Id",
                keyValue: new Guid("b7ac6c11-2a40-4d60-9b25-000000000000"),
                column: "CompanyId",
                value: null);

            migrationBuilder.UpdateData(
                table: "TaxCodes",
                keyColumn: "Id",
                keyValue: new Guid("b7ac6c11-2a40-4d60-9b25-000000000005"),
                column: "CompanyId",
                value: null);

            migrationBuilder.UpdateData(
                table: "TaxCodes",
                keyColumn: "Id",
                keyValue: new Guid("b7ac6c11-2a40-4d60-9b25-000000000008"),
                column: "CompanyId",
                value: null);

            migrationBuilder.UpdateData(
                table: "TaxCodes",
                keyColumn: "Id",
                keyValue: new Guid("b7ac6c11-2a40-4d60-9b25-000000000010"),
                column: "CompanyId",
                value: null);

            migrationBuilder.UpdateData(
                table: "TaxCodes",
                keyColumn: "Id",
                keyValue: new Guid("b7ac6c11-2a40-4d60-9b25-000000000999"),
                column: "CompanyId",
                value: null);

            migrationBuilder.UpdateData(
                table: "ToolAssetCategories",
                keyColumn: "Id",
                keyValue: new Guid("a82e9c51-1f1d-4d3f-9b25-000000000001"),
                column: "CompanyId",
                value: null);

            migrationBuilder.InsertData(
                table: "Branches",
                columns: new[] { "Id", "Address", "Code", "CompanyId", "CreatedAt", "IsActive", "IsDefault", "Name", "UpdatedAt" },
                values: new object[] { new Guid("22222222-2222-2222-2222-222222222222"), null, "MAIN", new Guid("11111111-1111-1111-1111-111111111111"), new DateTime(2026, 5, 22, 0, 0, 0, 0, DateTimeKind.Utc), true, true, "Main Branch", new DateTime(2026, 5, 22, 0, 0, 0, 0, DateTimeKind.Utc) });

            migrationBuilder.CreateIndex(
                name: "IX_UserWorkspacePreferences_UserId_Category_PreferenceKey_CompanyId_BranchId",
                table: "UserWorkspacePreferences",
                columns: new[] { "UserId", "Category", "PreferenceKey", "CompanyId", "BranchId" },
                unique: true,
                filter: "[CompanyId] IS NOT NULL AND [BranchId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AppUserPermissions_UserId_PermissionCode_CompanyId_BranchId",
                table: "AppUserPermissions",
                columns: new[] { "UserId", "PermissionCode", "CompanyId", "BranchId" },
                unique: true,
                filter: "[CompanyId] IS NOT NULL AND [BranchId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Branches_CompanyId_Code",
                table: "Branches",
                columns: new[] { "CompanyId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Companies_Code",
                table: "Companies",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserBranchAccesses_CompanyId_BranchId",
                table: "UserBranchAccesses",
                columns: new[] { "CompanyId", "BranchId" });

            migrationBuilder.CreateIndex(
                name: "IX_UserBranchAccesses_UserId_BranchId",
                table: "UserBranchAccesses",
                columns: new[] { "UserId", "BranchId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserCompanyAccesses_CompanyId",
                table: "UserCompanyAccesses",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_UserCompanyAccesses_UserId_CompanyId",
                table: "UserCompanyAccesses",
                columns: new[] { "UserId", "CompanyId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Branches");

            migrationBuilder.DropTable(
                name: "UserBranchAccesses");

            migrationBuilder.DropTable(
                name: "UserCompanyAccesses");

            migrationBuilder.DropTable(
                name: "Companies");

            migrationBuilder.DropIndex(
                name: "IX_UserWorkspacePreferences_UserId_Category_PreferenceKey_CompanyId_BranchId",
                table: "UserWorkspacePreferences");

            migrationBuilder.DropIndex(
                name: "IX_AppUserPermissions_UserId_PermissionCode_CompanyId_BranchId",
                table: "AppUserPermissions");

            migrationBuilder.DropColumn(
                name: "BranchId",
                table: "VatTransactions");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "VatTransactions");

            migrationBuilder.DropColumn(
                name: "BranchId",
                table: "UserWorkspacePreferences");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "UserWorkspacePreferences");

            migrationBuilder.DropColumn(
                name: "BranchId",
                table: "ToolRecoveries");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "ToolRecoveries");

            migrationBuilder.DropColumn(
                name: "BranchId",
                table: "ToolIssues");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "ToolIssues");

            migrationBuilder.DropColumn(
                name: "BranchId",
                table: "ToolDisposals");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "ToolDisposals");

            migrationBuilder.DropColumn(
                name: "BranchId",
                table: "ToolAssets");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "ToolAssets");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "ToolAssetCategories");

            migrationBuilder.DropColumn(
                name: "BranchId",
                table: "ToolAllocationSchedules");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "ToolAllocationSchedules");

            migrationBuilder.DropColumn(
                name: "BranchId",
                table: "ToolAllocationRuns");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "ToolAllocationRuns");

            migrationBuilder.DropColumn(
                name: "BranchId",
                table: "TaxDeclarationDrafts");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "TaxDeclarationDrafts");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "TaxCodes");

            migrationBuilder.DropColumn(
                name: "BranchId",
                table: "StockReceipts");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "StockReceipts");

            migrationBuilder.DropColumn(
                name: "BranchId",
                table: "StockReceiptDetails");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "StockReceiptDetails");

            migrationBuilder.DropColumn(
                name: "BranchId",
                table: "StockIssues");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "StockIssues");

            migrationBuilder.DropColumn(
                name: "BranchId",
                table: "StockIssueDetails");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "StockIssueDetails");

            migrationBuilder.DropColumn(
                name: "BranchId",
                table: "StockBalances");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "StockBalances");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "ReportTemplates");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "ReportParameters");

            migrationBuilder.DropColumn(
                name: "BranchId",
                table: "ReportExecutions");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "ReportExecutions");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "ReportDefinitions");

            migrationBuilder.DropColumn(
                name: "BranchId",
                table: "ReceivableTransactions");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "ReceivableTransactions");

            migrationBuilder.DropColumn(
                name: "BranchId",
                table: "PostingSources");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "PostingSources");

            migrationBuilder.DropColumn(
                name: "BranchId",
                table: "PostingBatches");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "PostingBatches");

            migrationBuilder.DropColumn(
                name: "BranchId",
                table: "PaymentAllocations");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "PaymentAllocations");

            migrationBuilder.DropColumn(
                name: "BranchId",
                table: "PayableTransactions");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "PayableTransactions");

            migrationBuilder.DropColumn(
                name: "BranchId",
                table: "JournalLines");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "JournalLines");

            migrationBuilder.DropColumn(
                name: "BranchId",
                table: "JournalHeaders");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "JournalHeaders");

            migrationBuilder.DropColumn(
                name: "BranchId",
                table: "InventoryCostTransactions");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "InventoryCostTransactions");

            migrationBuilder.DropColumn(
                name: "BranchId",
                table: "InventoryCostLayers");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "InventoryCostLayers");

            migrationBuilder.DropColumn(
                name: "BranchId",
                table: "FixedAssets");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "FixedAssets");

            migrationBuilder.DropColumn(
                name: "BranchId",
                table: "FixedAssetDisposals");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "FixedAssetDisposals");

            migrationBuilder.DropColumn(
                name: "BranchId",
                table: "FixedAssetDepreciations");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "FixedAssetDepreciations");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "FixedAssetCategories");

            migrationBuilder.DropColumn(
                name: "BranchId",
                table: "FixedAssetAdjustments");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "FixedAssetAdjustments");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "FinancialStatementMappings");

            migrationBuilder.DropColumn(
                name: "BranchId",
                table: "FinancialStatementExecutions");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "FinancialStatementExecutions");

            migrationBuilder.DropColumn(
                name: "BranchId",
                table: "DebtReconciliations");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "DebtReconciliations");

            migrationBuilder.DropColumn(
                name: "BranchId",
                table: "DebtReconciliationLines");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "DebtReconciliationLines");

            migrationBuilder.DropColumn(
                name: "BranchId",
                table: "ClosingEntryLines");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "ClosingEntryLines");

            migrationBuilder.DropColumn(
                name: "BranchId",
                table: "ClosingEntries");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "ClosingEntries");

            migrationBuilder.DropColumn(
                name: "BranchId",
                table: "CashTransactions");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "CashTransactions");

            migrationBuilder.DropColumn(
                name: "BranchId",
                table: "CashFunds");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "CashFunds");

            migrationBuilder.DropColumn(
                name: "BranchId",
                table: "CashCounts");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "CashCounts");

            migrationBuilder.DropColumn(
                name: "BranchId",
                table: "CashCountLines");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "CashCountLines");

            migrationBuilder.DropColumn(
                name: "BranchId",
                table: "BankTransactions");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "BankTransactions");

            migrationBuilder.DropColumn(
                name: "BranchId",
                table: "BankStatements");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "BankStatements");

            migrationBuilder.DropColumn(
                name: "BranchId",
                table: "BankStatementLines");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "BankStatementLines");

            migrationBuilder.DropColumn(
                name: "BranchId",
                table: "BankReconciliations");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "BankReconciliations");

            migrationBuilder.DropColumn(
                name: "BranchId",
                table: "BankReconciliationLines");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "BankReconciliationLines");

            migrationBuilder.DropColumn(
                name: "BranchId",
                table: "BankAccounts");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "BankAccounts");

            migrationBuilder.DropColumn(
                name: "BranchId",
                table: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "BranchId",
                table: "AppUserPermissions");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "AppUserPermissions");

            migrationBuilder.DropColumn(
                name: "BranchId",
                table: "AgingSnapshots");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "AgingSnapshots");

            migrationBuilder.DropColumn(
                name: "BranchId",
                table: "AccountBalanceSnapshots");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "AccountBalanceSnapshots");

            migrationBuilder.CreateIndex(
                name: "IX_UserWorkspacePreferences_UserId_Category_PreferenceKey",
                table: "UserWorkspacePreferences",
                columns: new[] { "UserId", "Category", "PreferenceKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppUserPermissions_UserId_PermissionCode",
                table: "AppUserPermissions",
                columns: new[] { "UserId", "PermissionCode" },
                unique: true);
        }
    }
}
