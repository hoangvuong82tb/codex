using ASMILE.BO.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ASMILE.BO.Infrastructure.Persistence.Migrations;

/// <summary>
/// Adds VAT snapshots to minibar consumption lines without rewriting existing data.
/// </summary>
[DbContext(typeof(AppDbContext))]
[Migration("20260923110000_AddMinibarVatFields")]
public partial class AddMinibarVatFields : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("IF COL_LENGTH(N'[fo].[MinibarConsumption]', N'VatRate') IS NULL ALTER TABLE [fo].[MinibarConsumption] ADD [VatRate] decimal(9,4) NOT NULL CONSTRAINT [DF_fo_MinibarConsumption_VatRate] DEFAULT (0);");
        migrationBuilder.Sql("IF COL_LENGTH(N'[fo].[MinibarConsumption]', N'VatAmount') IS NULL ALTER TABLE [fo].[MinibarConsumption] ADD [VatAmount] decimal(18,2) NOT NULL CONSTRAINT [DF_fo_MinibarConsumption_VatAmount] DEFAULT (0);");
        migrationBuilder.Sql("IF COL_LENGTH(N'[fo].[MinibarConsumption]', N'GrossAmount') IS NULL ALTER TABLE [fo].[MinibarConsumption] ADD [GrossAmount] decimal(18,2) NOT NULL CONSTRAINT [DF_fo_MinibarConsumption_GrossAmount] DEFAULT (0);");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Intentionally non-destructive: VAT snapshots must remain recoverable.
    }
}
