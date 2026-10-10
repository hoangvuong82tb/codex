using ASMILE.BO.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ASMILE.BO.Infrastructure.Persistence.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20261007190000_ClearLegacyAppModuleSeedVersion")]
public sealed class ClearLegacyAppModuleSeedVersion : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            UPDATE [AppModules]
            SET [Version] = NULL
            WHERE [Version] = N'1.0.0'
              AND [AppKey] IN (
                  N'dashboard', N'virtual-file-center', N'inventory', N'inventory-movement',
                  N'warehouse', N'import-voucher', N'export-voucher', N'receivable', N'payable',
                  N'general-ledger', N'report-center', N'customers', N'suppliers', N'hr',
                  N'settings', N'pos-client', N'fo-client', N'accounting-client', N'warehouse-client'
              );
            """);
    }

    // Deliberately do not restore the fabricated seed value during rollback.
    // The migration corrects data; restoring 1.0.0 would reintroduce the defect.
    protected override void Down(MigrationBuilder migrationBuilder)
    {
    }
}
