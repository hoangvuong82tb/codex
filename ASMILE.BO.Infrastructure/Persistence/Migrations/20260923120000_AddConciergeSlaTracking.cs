using ASMILE.BO.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ASMILE.BO.Infrastructure.Persistence.Migrations;

/// <summary>
/// Persists Concierge SLA deadlines and first-response timestamps so SLA cards
/// and reports are calculated from real service-request history.
/// </summary>
[DbContext(typeof(AppDbContext))]
[Migration("20260923120000_AddConciergeSlaTracking")]
public partial class AddConciergeSlaTracking : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("IF COL_LENGTH(N'[fo].[YeuCauDichVu]', N'SlaDeadlineAtUtc') IS NULL ALTER TABLE [fo].[YeuCauDichVu] ADD [SlaDeadlineAtUtc] datetime2 NULL;");
        migrationBuilder.Sql("IF COL_LENGTH(N'[fo].[YeuCauDichVu]', N'FirstResponseAtUtc') IS NULL ALTER TABLE [fo].[YeuCauDichVu] ADD [FirstResponseAtUtc] datetime2 NULL;");
        migrationBuilder.Sql("IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_fo_YeuCauDichVu_SlaDeadline' AND object_id = OBJECT_ID(N'[fo].[YeuCauDichVu]')) CREATE INDEX [IX_fo_YeuCauDichVu_SlaDeadline] ON [fo].[YeuCauDichVu] ([PropertyId], [SlaDeadlineAtUtc], [Status]);");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_fo_YeuCauDichVu_SlaDeadline' AND object_id = OBJECT_ID(N'[fo].[YeuCauDichVu]')) DROP INDEX [IX_fo_YeuCauDichVu_SlaDeadline] ON [fo].[YeuCauDichVu];");
        migrationBuilder.Sql("IF COL_LENGTH(N'[fo].[YeuCauDichVu]', N'FirstResponseAtUtc') IS NOT NULL ALTER TABLE [fo].[YeuCauDichVu] DROP COLUMN [FirstResponseAtUtc];");
        migrationBuilder.Sql("IF COL_LENGTH(N'[fo].[YeuCauDichVu]', N'SlaDeadlineAtUtc') IS NOT NULL ALTER TABLE [fo].[YeuCauDichVu] DROP COLUMN [SlaDeadlineAtUtc];");
    }
}
