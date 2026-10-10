using ASMILE.BO.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ASMILE.BO.Infrastructure.Persistence.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260911150000_RepairDefaultTaxCodeSeed")]
public partial class RepairDefaultTaxCodeSeed : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Some deployed databases have the VAT table but missed the HasData
        // rows from the original VAT migration. Restore only missing codes.
        migrationBuilder.Sql("""
            IF OBJECT_ID(N'[TaxCodes]', N'U') IS NOT NULL
            BEGIN
                IF NOT EXISTS (SELECT 1 FROM [TaxCodes] WHERE [Code] = N'VAT0')
                    INSERT INTO [TaxCodes] ([Id], [Code], [Name], [TaxRate], [TaxType], [InputAccountCode], [OutputAccountCode], [IsActive], [CreatedAt], [UpdatedAt])
                    VALUES ('B7AC6C11-2A40-4D60-9B25-000000000000', N'VAT0', N'Thuế GTGT 0%', 0, N'VATInput', NULL, NULL, 1, SYSUTCDATETIME(), SYSUTCDATETIME());
                IF NOT EXISTS (SELECT 1 FROM [TaxCodes] WHERE [Code] = N'VAT5')
                    INSERT INTO [TaxCodes] ([Id], [Code], [Name], [TaxRate], [TaxType], [InputAccountCode], [OutputAccountCode], [IsActive], [CreatedAt], [UpdatedAt])
                    VALUES ('B7AC6C11-2A40-4D60-9B25-000000000005', N'VAT5', N'Thuế GTGT 5%', 5, N'VATInput', NULL, NULL, 1, SYSUTCDATETIME(), SYSUTCDATETIME());
                IF NOT EXISTS (SELECT 1 FROM [TaxCodes] WHERE [Code] = N'VAT8')
                    INSERT INTO [TaxCodes] ([Id], [Code], [Name], [TaxRate], [TaxType], [InputAccountCode], [OutputAccountCode], [IsActive], [CreatedAt], [UpdatedAt])
                    VALUES ('B7AC6C11-2A40-4D60-9B25-000000000008', N'VAT8', N'Thuế GTGT 8%', 8, N'VATInput', NULL, NULL, 1, SYSUTCDATETIME(), SYSUTCDATETIME());
                IF NOT EXISTS (SELECT 1 FROM [TaxCodes] WHERE [Code] = N'VAT10')
                    INSERT INTO [TaxCodes] ([Id], [Code], [Name], [TaxRate], [TaxType], [InputAccountCode], [OutputAccountCode], [IsActive], [CreatedAt], [UpdatedAt])
                    VALUES ('B7AC6C11-2A40-4D60-9B25-000000000010', N'VAT10', N'Thuế GTGT 10%', 10, N'VATInput', NULL, NULL, 1, SYSUTCDATETIME(), SYSUTCDATETIME());
            END;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Do not remove tax codes: they may already be referenced by VAT rows.
    }
}
