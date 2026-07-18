using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ASMILE.BO.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTt99ComplianceFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TT99AccountMappings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AccountCode = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    TT99Category = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    TT99ReportSection = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    TT99DeclarationGroup = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TT99AccountMappings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TT99ComplianceIssues",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RuleCode = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Severity = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Module = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Message = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    AffectedEntity = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    MetadataJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CorrelationId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    DetectedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TT99ComplianceIssues", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TT99ComplianceSnapshots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PeriodCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    SnapshotHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ComplianceScore = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    DeclarationStatus = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    SnapshotJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CorrelationId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TT99ComplianceSnapshots", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TT99DeclarationPeriods",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PeriodCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    FromDateUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ToDateUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TT99DeclarationPeriods", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TT99ExportManifests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ExportType = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    PeriodCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ExportedBy = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    ExportedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Hash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    RowCount = table.Column<int>(type: "int", nullable: false),
                    ComplianceVersion = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    CorrelationId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    MetadataJson = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TT99ExportManifests", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TT99ReportTemplates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ReportId = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TT99ReportTemplates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TT99ValidationRules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RuleCode = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    RuleName = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Severity = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Module = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    MetadataJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TT99ValidationRules", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TT99ReportLines",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TT99ReportTemplateId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LineCode = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    LineName = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    Formula = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    MappingJson = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TT99ReportLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TT99ReportLines_TT99ReportTemplates_TT99ReportTemplateId",
                        column: x => x.TT99ReportTemplateId,
                        principalTable: "TT99ReportTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "ReportTemplates",
                columns: new[] { "Id", "Category", "Code", "CompanyId", "CreatedAt", "DataSource", "Description", "IsActive", "IsSystem", "LayoutDefinition", "Name", "QueryDefinition", "UpdatedAt" },
                values: new object[,]
                {
                    { new Guid("4a9a7b4f-ac6e-4e91-9a11-000000000112"), "Compliance", "TT99_BALANCE_SHEET", null, new DateTime(2026, 5, 23, 0, 0, 0, 0, DateTimeKind.Utc), "FinancialStatement", "Bảng cân đối kế toán theo nền TT99 compliance.", true, true, null, "Bảng cân đối kế toán TT99", null, new DateTime(2026, 5, 23, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("4a9a7b4f-ac6e-4e91-9a11-000000000113"), "Compliance", "TT99_INCOME_STATEMENT", null, new DateTime(2026, 5, 23, 0, 0, 0, 0, DateTimeKind.Utc), "FinancialStatement", "Báo cáo kết quả kinh doanh theo nền TT99 compliance.", true, true, null, "Báo cáo kết quả kinh doanh TT99", null, new DateTime(2026, 5, 23, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("4a9a7b4f-ac6e-4e91-9a11-000000000114"), "Compliance", "TT99_CASH_FLOW", null, new DateTime(2026, 5, 23, 0, 0, 0, 0, DateTimeKind.Utc), "FinancialStatement", "Báo cáo lưu chuyển tiền tệ theo nền TT99 compliance.", true, true, null, "Báo cáo lưu chuyển tiền tệ TT99", null, new DateTime(2026, 5, 23, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("4a9a7b4f-ac6e-4e91-9a11-000000000115"), "Compliance", "TT99_ACCOUNT_MAPPING", null, new DateTime(2026, 5, 23, 0, 0, 0, 0, DateTimeKind.Utc), "TT99", "Đối chiếu mapping tài khoản theo TT99.", true, true, null, "Đối chiếu mapping tài khoản TT99", null, new DateTime(2026, 5, 23, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("4a9a7b4f-ac6e-4e91-9a11-000000000116"), "Compliance", "TT99_COMPLIANCE_ISSUES", null, new DateTime(2026, 5, 23, 0, 0, 0, 0, DateTimeKind.Utc), "TT99", "Danh sách issue tuân thủ TT99.", true, true, null, "Danh sách lỗi tuân thủ TT99", null, new DateTime(2026, 5, 23, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("4a9a7b4f-ac6e-4e91-9a11-000000000117"), "Compliance", "TT99_DECLARATION_SUMMARY", null, new DateTime(2026, 5, 23, 0, 0, 0, 0, DateTimeKind.Utc), "TT99", "Tổng hợp kỳ kê khai phục vụ nền TT99.", true, true, null, "Tổng hợp kỳ kê khai TT99", null, new DateTime(2026, 5, 23, 0, 0, 0, 0, DateTimeKind.Utc) }
                });

            migrationBuilder.CreateIndex(
                name: "IX_TT99AccountMappings_CompanyId_BranchId_AccountCode",
                table: "TT99AccountMappings",
                columns: new[] { "CompanyId", "BranchId", "AccountCode" },
                unique: true,
                filter: "[CompanyId] IS NOT NULL AND [BranchId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_TT99AccountMappings_CompanyId_BranchId_TT99Category",
                table: "TT99AccountMappings",
                columns: new[] { "CompanyId", "BranchId", "TT99Category" });

            migrationBuilder.CreateIndex(
                name: "IX_TT99ComplianceIssues_CompanyId_BranchId_DetectedAtUtc",
                table: "TT99ComplianceIssues",
                columns: new[] { "CompanyId", "BranchId", "DetectedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_TT99ComplianceIssues_RuleCode_Severity",
                table: "TT99ComplianceIssues",
                columns: new[] { "RuleCode", "Severity" });

            migrationBuilder.CreateIndex(
                name: "IX_TT99ComplianceSnapshots_CompanyId_BranchId_PeriodCode_CreatedAtUtc",
                table: "TT99ComplianceSnapshots",
                columns: new[] { "CompanyId", "BranchId", "PeriodCode", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_TT99DeclarationPeriods_CompanyId_BranchId_PeriodCode",
                table: "TT99DeclarationPeriods",
                columns: new[] { "CompanyId", "BranchId", "PeriodCode" },
                unique: true,
                filter: "[CompanyId] IS NOT NULL AND [BranchId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_TT99ExportManifests_CompanyId_BranchId_PeriodCode_ExportedAtUtc",
                table: "TT99ExportManifests",
                columns: new[] { "CompanyId", "BranchId", "PeriodCode", "ExportedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_TT99ReportLines_CompanyId_BranchId_LineCode",
                table: "TT99ReportLines",
                columns: new[] { "CompanyId", "BranchId", "LineCode" });

            migrationBuilder.CreateIndex(
                name: "IX_TT99ReportLines_TT99ReportTemplateId",
                table: "TT99ReportLines",
                column: "TT99ReportTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_TT99ReportTemplates_CompanyId_BranchId_ReportId",
                table: "TT99ReportTemplates",
                columns: new[] { "CompanyId", "BranchId", "ReportId" },
                unique: true,
                filter: "[CompanyId] IS NOT NULL AND [BranchId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_TT99ValidationRules_CompanyId_BranchId_RuleCode",
                table: "TT99ValidationRules",
                columns: new[] { "CompanyId", "BranchId", "RuleCode" },
                unique: true,
                filter: "[CompanyId] IS NOT NULL AND [BranchId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TT99AccountMappings");

            migrationBuilder.DropTable(
                name: "TT99ComplianceIssues");

            migrationBuilder.DropTable(
                name: "TT99ComplianceSnapshots");

            migrationBuilder.DropTable(
                name: "TT99DeclarationPeriods");

            migrationBuilder.DropTable(
                name: "TT99ExportManifests");

            migrationBuilder.DropTable(
                name: "TT99ReportLines");

            migrationBuilder.DropTable(
                name: "TT99ValidationRules");

            migrationBuilder.DropTable(
                name: "TT99ReportTemplates");

            migrationBuilder.DeleteData(
                table: "ReportTemplates",
                keyColumn: "Id",
                keyValue: new Guid("4a9a7b4f-ac6e-4e91-9a11-000000000112"));

            migrationBuilder.DeleteData(
                table: "ReportTemplates",
                keyColumn: "Id",
                keyValue: new Guid("4a9a7b4f-ac6e-4e91-9a11-000000000113"));

            migrationBuilder.DeleteData(
                table: "ReportTemplates",
                keyColumn: "Id",
                keyValue: new Guid("4a9a7b4f-ac6e-4e91-9a11-000000000114"));

            migrationBuilder.DeleteData(
                table: "ReportTemplates",
                keyColumn: "Id",
                keyValue: new Guid("4a9a7b4f-ac6e-4e91-9a11-000000000115"));

            migrationBuilder.DeleteData(
                table: "ReportTemplates",
                keyColumn: "Id",
                keyValue: new Guid("4a9a7b4f-ac6e-4e91-9a11-000000000116"));

            migrationBuilder.DeleteData(
                table: "ReportTemplates",
                keyColumn: "Id",
                keyValue: new Guid("4a9a7b4f-ac6e-4e91-9a11-000000000117"));
        }
    }
}
