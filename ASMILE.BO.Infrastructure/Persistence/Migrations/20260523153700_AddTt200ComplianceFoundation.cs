using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ASMILE.BO.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTt200ComplianceFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TT200AccountMappings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AccountCode = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    TT200Category = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    TT200ReportSection = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    TT200ClosingGroup = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TT200AccountMappings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TT200ComplianceIssues",
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
                    table.PrimaryKey("PK_TT200ComplianceIssues", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TT200ComplianceSnapshots",
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
                    table.PrimaryKey("PK_TT200ComplianceSnapshots", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TT200DeclarationPeriods",
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
                    table.PrimaryKey("PK_TT200DeclarationPeriods", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TT200ExportManifests",
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
                    table.PrimaryKey("PK_TT200ExportManifests", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TT200ReportTemplates",
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
                    table.PrimaryKey("PK_TT200ReportTemplates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TT200ValidationRules",
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
                    table.PrimaryKey("PK_TT200ValidationRules", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TT200ReportLines",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TT200ReportTemplateId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
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
                    table.PrimaryKey("PK_TT200ReportLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TT200ReportLines_TT200ReportTemplates_TT200ReportTemplateId",
                        column: x => x.TT200ReportTemplateId,
                        principalTable: "TT200ReportTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "ReportTemplates",
                columns: new[] { "Id", "Category", "Code", "CompanyId", "CreatedAt", "DataSource", "Description", "IsActive", "IsSystem", "LayoutDefinition", "Name", "QueryDefinition", "UpdatedAt" },
                values: new object[,]
                {
                    { new Guid("4a9a7b4f-ac6e-4e91-9a11-000000000212"), "Compliance", "TT200_BALANCE_SHEET", null, new DateTime(2026, 5, 23, 0, 0, 0, 0, DateTimeKind.Utc), "FinancialStatement", "Bảng cân đối kế toán theo nền TT200 compliance.", true, true, null, "Bảng cân đối kế toán TT200", null, new DateTime(2026, 5, 23, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("4a9a7b4f-ac6e-4e91-9a11-000000000213"), "Compliance", "TT200_INCOME_STATEMENT", null, new DateTime(2026, 5, 23, 0, 0, 0, 0, DateTimeKind.Utc), "FinancialStatement", "Báo cáo kết quả kinh doanh theo nền TT200 compliance.", true, true, null, "Báo cáo kết quả kinh doanh TT200", null, new DateTime(2026, 5, 23, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("4a9a7b4f-ac6e-4e91-9a11-000000000214"), "Compliance", "TT200_CASH_FLOW", null, new DateTime(2026, 5, 23, 0, 0, 0, 0, DateTimeKind.Utc), "FinancialStatement", "Báo cáo lưu chuyển tiền tệ theo nền TT200 compliance.", true, true, null, "Báo cáo lưu chuyển tiền tệ TT200", null, new DateTime(2026, 5, 23, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("4a9a7b4f-ac6e-4e91-9a11-000000000215"), "Compliance", "TT200_CLOSING_SUMMARY", null, new DateTime(2026, 5, 23, 0, 0, 0, 0, DateTimeKind.Utc), "TT200", "Tổng hợp khóa sổ theo nền TT200 compliance.", true, true, null, "Tổng hợp khóa sổ TT200", null, new DateTime(2026, 5, 23, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("4a9a7b4f-ac6e-4e91-9a11-000000000216"), "Compliance", "TT200_ACCOUNT_MAPPING", null, new DateTime(2026, 5, 23, 0, 0, 0, 0, DateTimeKind.Utc), "TT200", "Đối chiếu mapping tài khoản theo TT200.", true, true, null, "Đối chiếu mapping tài khoản TT200", null, new DateTime(2026, 5, 23, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("4a9a7b4f-ac6e-4e91-9a11-000000000217"), "Compliance", "TT200_COMPLIANCE_ISSUES", null, new DateTime(2026, 5, 23, 0, 0, 0, 0, DateTimeKind.Utc), "TT200", "Danh sách issue tuân thủ TT200.", true, true, null, "Danh sách lỗi tuân thủ TT200", null, new DateTime(2026, 5, 23, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("4a9a7b4f-ac6e-4e91-9a11-000000000218"), "Compliance", "TT200_DECLARATION_SUMMARY", null, new DateTime(2026, 5, 23, 0, 0, 0, 0, DateTimeKind.Utc), "TT200", "Tổng hợp kỳ kê khai phục vụ nền TT200.", true, true, null, "Tổng hợp kỳ kê khai TT200", null, new DateTime(2026, 5, 23, 0, 0, 0, 0, DateTimeKind.Utc) }
                });

            migrationBuilder.CreateIndex(
                name: "IX_TT200AccountMappings_CompanyId_BranchId_AccountCode",
                table: "TT200AccountMappings",
                columns: new[] { "CompanyId", "BranchId", "AccountCode" },
                unique: true,
                filter: "[CompanyId] IS NOT NULL AND [BranchId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_TT200AccountMappings_CompanyId_BranchId_TT200Category",
                table: "TT200AccountMappings",
                columns: new[] { "CompanyId", "BranchId", "TT200Category" });

            migrationBuilder.CreateIndex(
                name: "IX_TT200ComplianceIssues_CompanyId_BranchId_DetectedAtUtc",
                table: "TT200ComplianceIssues",
                columns: new[] { "CompanyId", "BranchId", "DetectedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_TT200ComplianceIssues_RuleCode_Severity",
                table: "TT200ComplianceIssues",
                columns: new[] { "RuleCode", "Severity" });

            migrationBuilder.CreateIndex(
                name: "IX_TT200ComplianceSnapshots_CompanyId_BranchId_PeriodCode_CreatedAtUtc",
                table: "TT200ComplianceSnapshots",
                columns: new[] { "CompanyId", "BranchId", "PeriodCode", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_TT200DeclarationPeriods_CompanyId_BranchId_PeriodCode",
                table: "TT200DeclarationPeriods",
                columns: new[] { "CompanyId", "BranchId", "PeriodCode" },
                unique: true,
                filter: "[CompanyId] IS NOT NULL AND [BranchId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_TT200ExportManifests_CompanyId_BranchId_PeriodCode_ExportedAtUtc",
                table: "TT200ExportManifests",
                columns: new[] { "CompanyId", "BranchId", "PeriodCode", "ExportedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_TT200ReportLines_CompanyId_BranchId_LineCode",
                table: "TT200ReportLines",
                columns: new[] { "CompanyId", "BranchId", "LineCode" });

            migrationBuilder.CreateIndex(
                name: "IX_TT200ReportLines_TT200ReportTemplateId",
                table: "TT200ReportLines",
                column: "TT200ReportTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_TT200ReportTemplates_CompanyId_BranchId_ReportId",
                table: "TT200ReportTemplates",
                columns: new[] { "CompanyId", "BranchId", "ReportId" },
                unique: true,
                filter: "[CompanyId] IS NOT NULL AND [BranchId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_TT200ValidationRules_CompanyId_BranchId_RuleCode",
                table: "TT200ValidationRules",
                columns: new[] { "CompanyId", "BranchId", "RuleCode" },
                unique: true,
                filter: "[CompanyId] IS NOT NULL AND [BranchId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TT200AccountMappings");

            migrationBuilder.DropTable(
                name: "TT200ComplianceIssues");

            migrationBuilder.DropTable(
                name: "TT200ComplianceSnapshots");

            migrationBuilder.DropTable(
                name: "TT200DeclarationPeriods");

            migrationBuilder.DropTable(
                name: "TT200ExportManifests");

            migrationBuilder.DropTable(
                name: "TT200ReportLines");

            migrationBuilder.DropTable(
                name: "TT200ValidationRules");

            migrationBuilder.DropTable(
                name: "TT200ReportTemplates");

            migrationBuilder.DeleteData(
                table: "ReportTemplates",
                keyColumn: "Id",
                keyValue: new Guid("4a9a7b4f-ac6e-4e91-9a11-000000000212"));

            migrationBuilder.DeleteData(
                table: "ReportTemplates",
                keyColumn: "Id",
                keyValue: new Guid("4a9a7b4f-ac6e-4e91-9a11-000000000213"));

            migrationBuilder.DeleteData(
                table: "ReportTemplates",
                keyColumn: "Id",
                keyValue: new Guid("4a9a7b4f-ac6e-4e91-9a11-000000000214"));

            migrationBuilder.DeleteData(
                table: "ReportTemplates",
                keyColumn: "Id",
                keyValue: new Guid("4a9a7b4f-ac6e-4e91-9a11-000000000215"));

            migrationBuilder.DeleteData(
                table: "ReportTemplates",
                keyColumn: "Id",
                keyValue: new Guid("4a9a7b4f-ac6e-4e91-9a11-000000000216"));

            migrationBuilder.DeleteData(
                table: "ReportTemplates",
                keyColumn: "Id",
                keyValue: new Guid("4a9a7b4f-ac6e-4e91-9a11-000000000217"));

            migrationBuilder.DeleteData(
                table: "ReportTemplates",
                keyColumn: "Id",
                keyValue: new Guid("4a9a7b4f-ac6e-4e91-9a11-000000000218"));
        }
    }
}
