using ASMILE.BO.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ASMILE.BO.Infrastructure.Persistence.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260517090000_AddLedgerAccounts")]
public class AddLedgerAccounts : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "LedgerAccounts",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                AccountCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                AccountKind = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                AccountLevel = table.Column<int>(type: "int", nullable: false),
                AccountName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                AccountNature = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                ParentAccountCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                AccountType = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                AllowFund = table.Column<bool>(type: "bit", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                CustomerDebt = table.Column<bool>(type: "bit", nullable: false),
                FollowWarehouse = table.Column<bool>(type: "bit", nullable: false),
                HasForeignCurrency = table.Column<bool>(type: "bit", nullable: false),
                IsActive = table.Column<bool>(type: "bit", nullable: false),
                IsCashAccount = table.Column<bool>(type: "bit", nullable: false),
                ReportName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                SortOrder = table.Column<int>(type: "int", nullable: false),
                SupplierDebt = table.Column<bool>(type: "bit", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_LedgerAccounts", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_LedgerAccounts_AccountCode",
            table: "LedgerAccounts",
            column: "AccountCode",
            unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "LedgerAccounts");
    }
}
