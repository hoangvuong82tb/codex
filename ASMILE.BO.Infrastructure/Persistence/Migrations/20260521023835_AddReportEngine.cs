using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ASMILE.BO.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddReportEngine : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ReportExecutions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReportCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ParametersJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    StartedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowCount = table.Column<int>(type: "int", nullable: false),
                    ErrorMessage = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ExecutedBy = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReportExecutions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ReportTemplates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    DataSource = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    QueryDefinition = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LayoutDefinition = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsSystem = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReportTemplates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ReportDefinitions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReportTemplateId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VersionNo = table.Column<int>(type: "int", nullable: false),
                    DefinitionJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsPublished = table.Column<bool>(type: "bit", nullable: false),
                    PublishedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReportDefinitions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReportDefinitions_ReportTemplates_ReportTemplateId",
                        column: x => x.ReportTemplateId,
                        principalTable: "ReportTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ReportParameters",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReportTemplateId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    DataType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    DefaultValue = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsRequired = table.Column<bool>(type: "bit", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReportParameters", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReportParameters_ReportTemplates_ReportTemplateId",
                        column: x => x.ReportTemplateId,
                        principalTable: "ReportTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "ReportTemplates",
                columns: new[] { "Id", "Category", "Code", "CreatedAt", "DataSource", "Description", "IsActive", "IsSystem", "LayoutDefinition", "Name", "QueryDefinition", "UpdatedAt" },
                values: new object[,]
                {
                    { new Guid("4a9a7b4f-ac6e-4e91-9a11-000000000001"), "Accounting", "GENERAL_LEDGER", new DateTime(2026, 5, 21, 0, 0, 0, 0, DateTimeKind.Utc), "Journal", "Báo cáo sổ cái từ Journal persisted.", true, true, null, "Sổ cái", null, new DateTime(2026, 5, 21, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("4a9a7b4f-ac6e-4e91-9a11-000000000002"), "Accounting", "TRIAL_BALANCE", new DateTime(2026, 5, 21, 0, 0, 0, 0, DateTimeKind.Utc), "Journal", "Tổng hợp phát sinh theo tài khoản từ Journal persisted.", true, true, null, "Cân đối phát sinh", null, new DateTime(2026, 5, 21, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("4a9a7b4f-ac6e-4e91-9a11-000000000003"), "Inventory", "INVENTORY_SUMMARY", new DateTime(2026, 5, 21, 0, 0, 0, 0, DateTimeKind.Utc), "StockBalance", "Tổng hợp số lượng và giá trị tồn kho.", true, true, null, "Tổng hợp tồn kho", null, new DateTime(2026, 5, 21, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("4a9a7b4f-ac6e-4e91-9a11-000000000004"), "Inventory", "INVENTORY_MOVEMENT", new DateTime(2026, 5, 21, 0, 0, 0, 0, DateTimeKind.Utc), "StockReceipt,StockIssue,InventoryCostTransaction", "Báo cáo phát sinh nhập xuất kho.", true, true, null, "Nhập xuất tồn", null, new DateTime(2026, 5, 21, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("4a9a7b4f-ac6e-4e91-9a11-000000000005"), "Payable", "AP_AGING", new DateTime(2026, 5, 21, 0, 0, 0, 0, DateTimeKind.Utc), "PayableTransaction", "Báo cáo tuổi nợ nhà cung cấp từ công nợ persisted.", true, true, null, "Tuổi nợ phải trả", null, new DateTime(2026, 5, 21, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("4a9a7b4f-ac6e-4e91-9a11-000000000006"), "Receivable", "AR_AGING", new DateTime(2026, 5, 21, 0, 0, 0, 0, DateTimeKind.Utc), "ReceivableTransaction", "Báo cáo tuổi nợ khách hàng từ công nợ persisted.", true, true, null, "Tuổi nợ phải thu", null, new DateTime(2026, 5, 21, 0, 0, 0, 0, DateTimeKind.Utc) }
                });

            migrationBuilder.CreateIndex(
                name: "IX_ReportDefinitions_IsPublished",
                table: "ReportDefinitions",
                column: "IsPublished");

            migrationBuilder.CreateIndex(
                name: "IX_ReportDefinitions_ReportTemplateId",
                table: "ReportDefinitions",
                column: "ReportTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_ReportExecutions_ReportCode",
                table: "ReportExecutions",
                column: "ReportCode");

            migrationBuilder.CreateIndex(
                name: "IX_ReportExecutions_Status",
                table: "ReportExecutions",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ReportParameters_ReportTemplateId",
                table: "ReportParameters",
                column: "ReportTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_ReportTemplates_Category",
                table: "ReportTemplates",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_ReportTemplates_Code",
                table: "ReportTemplates",
                column: "Code",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ReportDefinitions");

            migrationBuilder.DropTable(
                name: "ReportExecutions");

            migrationBuilder.DropTable(
                name: "ReportParameters");

            migrationBuilder.DropTable(
                name: "ReportTemplates");
        }
    }
}
