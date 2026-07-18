using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ASMILE.BO.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAdvancedFinancialStatementMappingFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FinancialTemplates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    StandardCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    TemplateCode = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    StatementType = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinancialTemplates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FinancialTemplateVersions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    FinancialTemplateId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VersionCode = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    IsActiveVersion = table.Column<bool>(type: "bit", nullable: false),
                    EffectiveFromUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EffectiveToUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    HierarchyJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MetadataJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinancialTemplateVersions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FinancialTemplateVersions_FinancialTemplates_FinancialTemplateId",
                        column: x => x.FinancialTemplateId,
                        principalTable: "FinancialTemplates",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "FinancialStatementFormulas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    FinancialTemplateVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LineCode = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Expression = table.Column<string>(type: "nvarchar(1200)", maxLength: 1200, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    MetadataJson = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinancialStatementFormulas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FinancialStatementFormulas_FinancialTemplateVersions_FinancialTemplateVersionId",
                        column: x => x.FinancialTemplateVersionId,
                        principalTable: "FinancialTemplateVersions",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "FinancialStatementSnapshots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    FinancialTemplateVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PeriodCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CapturedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SnapshotJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SnapshotHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    CorrelationId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinancialStatementSnapshots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FinancialStatementSnapshots_FinancialTemplateVersions_FinancialTemplateVersionId",
                        column: x => x.FinancialTemplateVersionId,
                        principalTable: "FinancialTemplateVersions",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_FinancialStatementFormulas_FinancialTemplateVersionId_LineCode",
                table: "FinancialStatementFormulas",
                columns: new[] { "FinancialTemplateVersionId", "LineCode" });

            migrationBuilder.CreateIndex(
                name: "IX_FinancialStatementSnapshots_FinancialTemplateVersionId_PeriodCode_CapturedAtUtc",
                table: "FinancialStatementSnapshots",
                columns: new[] { "FinancialTemplateVersionId", "PeriodCode", "CapturedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_FinancialTemplates_CompanyId_BranchId_StandardCode_TemplateCode",
                table: "FinancialTemplates",
                columns: new[] { "CompanyId", "BranchId", "StandardCode", "TemplateCode" },
                unique: true,
                filter: "[CompanyId] IS NOT NULL AND [BranchId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_FinancialTemplateVersions_FinancialTemplateId_VersionCode",
                table: "FinancialTemplateVersions",
                columns: new[] { "FinancialTemplateId", "VersionCode" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FinancialStatementFormulas");

            migrationBuilder.DropTable(
                name: "FinancialStatementSnapshots");

            migrationBuilder.DropTable(
                name: "FinancialTemplateVersions");

            migrationBuilder.DropTable(
                name: "FinancialTemplates");
        }
    }
}
