using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ASMILE.BO.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddLedgerAccountMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AccountCategory",
                table: "LedgerAccounts",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AccountingStandardCode",
                table: "LedgerAccounts",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "TT99");

            migrationBuilder.AddColumn<bool>(
                name: "AllowPosting",
                table: "LedgerAccounts",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeletable",
                table: "LedgerAccounts",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsEditable",
                table: "LedgerAccounts",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsFinancialAccount",
                table: "LedgerAccounts",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsManagementAccount",
                table: "LedgerAccounts",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsSystemAccount",
                table: "LedgerAccounts",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "NormalBalance",
                table: "LedgerAccounts",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Debit");

            migrationBuilder.AddColumn<bool>(
                name: "RequireCurrency",
                table: "LedgerAccounts",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "RequireCustomer",
                table: "LedgerAccounts",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "RequireDepartment",
                table: "LedgerAccounts",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "RequireEmployee",
                table: "LedgerAccounts",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "RequireItem",
                table: "LedgerAccounts",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "RequireSupplier",
                table: "LedgerAccounts",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "RequireWarehouse",
                table: "LedgerAccounts",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_LedgerAccounts_AccountCategory",
                table: "LedgerAccounts",
                column: "AccountCategory");

            migrationBuilder.CreateIndex(
                name: "IX_LedgerAccounts_AccountingStandardCode",
                table: "LedgerAccounts",
                column: "AccountingStandardCode");

            migrationBuilder.CreateIndex(
                name: "IX_LedgerAccounts_AllowPosting",
                table: "LedgerAccounts",
                column: "AllowPosting");

            migrationBuilder.Sql("""
UPDATE [LedgerAccounts]
SET [AccountingStandardCode] = N'TT99'
WHERE [AccountingStandardCode] IS NULL OR LTRIM(RTRIM([AccountingStandardCode])) = N'';

UPDATE [LedgerAccounts]
SET [AccountCategory] = CASE
    WHEN [AccountCode] LIKE N'1%' THEN N'Asset'
    WHEN [AccountCode] LIKE N'2%' OR [AccountCode] LIKE N'3%' THEN N'Liability'
    WHEN [AccountCode] LIKE N'4%' THEN N'Equity'
    WHEN [AccountCode] LIKE N'5%' OR [AccountCode] LIKE N'7%' THEN N'Revenue'
    WHEN [AccountCode] LIKE N'6%' OR [AccountCode] LIKE N'8%' THEN N'Expense'
    ELSE N'Other'
END
WHERE [AccountCategory] IS NULL OR LTRIM(RTRIM([AccountCategory])) = N'';

UPDATE [LedgerAccounts]
SET [NormalBalance] = CASE
    WHEN [AccountCode] LIKE N'2%' OR [AccountCode] LIKE N'3%' OR [AccountCode] LIKE N'4%' OR [AccountCode] LIKE N'5%' OR [AccountCode] LIKE N'7%' THEN N'Credit'
    ELSE N'Debit'
END
WHERE [NormalBalance] IS NULL OR LTRIM(RTRIM([NormalBalance])) = N'';

UPDATE [LedgerAccounts]
SET [IsFinancialAccount] = CASE WHEN [AccountCode] LIKE N'9%' THEN CAST(0 AS bit) ELSE CAST(1 AS bit) END,
    [IsManagementAccount] = CASE WHEN [AccountCode] LIKE N'9%' THEN CAST(1 AS bit) ELSE CAST(0 AS bit) END;

UPDATE [LedgerAccounts]
SET [AllowPosting] = CASE WHEN [AccountKind] = N'H' THEN CAST(0 AS bit) ELSE CAST(1 AS bit) END,
    [IsDeletable] = CASE WHEN [AccountKind] = N'H' THEN CAST(0 AS bit) ELSE CAST(1 AS bit) END;

UPDATE [AccountRoleMappings]
SET [AccountCode] = N'1561'
WHERE [AccountingStandardCode] = N'TT99' AND [RoleCode] = N'Inventory' AND [AccountCode] = N'156'
    AND EXISTS (SELECT 1 FROM [LedgerAccounts] WHERE [AccountCode] = N'1561');

UPDATE [AccountRoleMappings]
SET [AccountCode] = N'3311'
WHERE [AccountingStandardCode] = N'TT99' AND [RoleCode] = N'Payable' AND [AccountCode] = N'331'
    AND EXISTS (SELECT 1 FROM [LedgerAccounts] WHERE [AccountCode] = N'3311');

UPDATE [AccountRoleMappings]
SET [AccountCode] = N'6321'
WHERE [AccountingStandardCode] = N'TT99' AND [RoleCode] = N'COGS' AND [AccountCode] = N'632'
    AND EXISTS (SELECT 1 FROM [LedgerAccounts] WHERE [AccountCode] = N'6321');
""");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_LedgerAccounts_AccountCategory",
                table: "LedgerAccounts");

            migrationBuilder.DropIndex(
                name: "IX_LedgerAccounts_AccountingStandardCode",
                table: "LedgerAccounts");

            migrationBuilder.DropIndex(
                name: "IX_LedgerAccounts_AllowPosting",
                table: "LedgerAccounts");

            migrationBuilder.DropColumn(
                name: "AccountCategory",
                table: "LedgerAccounts");

            migrationBuilder.DropColumn(
                name: "AccountingStandardCode",
                table: "LedgerAccounts");

            migrationBuilder.DropColumn(
                name: "AllowPosting",
                table: "LedgerAccounts");

            migrationBuilder.DropColumn(
                name: "IsDeletable",
                table: "LedgerAccounts");

            migrationBuilder.DropColumn(
                name: "IsEditable",
                table: "LedgerAccounts");

            migrationBuilder.DropColumn(
                name: "IsFinancialAccount",
                table: "LedgerAccounts");

            migrationBuilder.DropColumn(
                name: "IsManagementAccount",
                table: "LedgerAccounts");

            migrationBuilder.DropColumn(
                name: "IsSystemAccount",
                table: "LedgerAccounts");

            migrationBuilder.DropColumn(
                name: "NormalBalance",
                table: "LedgerAccounts");

            migrationBuilder.DropColumn(
                name: "RequireCurrency",
                table: "LedgerAccounts");

            migrationBuilder.DropColumn(
                name: "RequireCustomer",
                table: "LedgerAccounts");

            migrationBuilder.DropColumn(
                name: "RequireDepartment",
                table: "LedgerAccounts");

            migrationBuilder.DropColumn(
                name: "RequireEmployee",
                table: "LedgerAccounts");

            migrationBuilder.DropColumn(
                name: "RequireItem",
                table: "LedgerAccounts");

            migrationBuilder.DropColumn(
                name: "RequireSupplier",
                table: "LedgerAccounts");

            migrationBuilder.DropColumn(
                name: "RequireWarehouse",
                table: "LedgerAccounts");
        }
    }
}
