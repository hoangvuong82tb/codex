using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ASMILE.BO.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddToolAssetModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ToolAllocationRuns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RunNo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    AllocationPeriod = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    RunDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PostedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Message = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ToolAllocationRuns", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ToolAssetCategories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CategoryCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CategoryName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    ToolAccountCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    PrepaidExpenseAccountCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ExpenseAccountCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    DefaultAllocationMonths = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ToolAssetCategories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ToolAssets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ToolCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ToolName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    CategoryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PurchaseDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    OriginalCost = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    UnitCost = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    AllocatedAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    RemainingAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    AllocationMonths = table.Column<int>(type: "int", nullable: false),
                    StartAllocationDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DepartmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    EmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    WarehouseId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SupplierId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    JournalHeaderId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ToolAssets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ToolAssets_JournalHeaders_JournalHeaderId",
                        column: x => x.JournalHeaderId,
                        principalTable: "JournalHeaders",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ToolAssets_ToolAssetCategories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "ToolAssetCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ToolAllocationSchedules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ToolAssetId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AllocationPeriod = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    AllocationDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AllocationAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    AccumulatedAfter = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    RemainingAfter = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    JournalHeaderId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PostedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ToolAllocationSchedules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ToolAllocationSchedules_JournalHeaders_JournalHeaderId",
                        column: x => x.JournalHeaderId,
                        principalTable: "JournalHeaders",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ToolAllocationSchedules_ToolAssets_ToolAssetId",
                        column: x => x.ToolAssetId,
                        principalTable: "ToolAssets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ToolDisposals",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DisposalNo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    DisposalDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ToolAssetId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    RemainingAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    JournalHeaderId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ToolDisposals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ToolDisposals_JournalHeaders_JournalHeaderId",
                        column: x => x.JournalHeaderId,
                        principalTable: "JournalHeaders",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ToolDisposals_ToolAssets_ToolAssetId",
                        column: x => x.ToolAssetId,
                        principalTable: "ToolAssets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ToolIssues",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IssueNo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    IssueDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ToolAssetId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    DepartmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    EmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    JournalHeaderId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PostedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ToolIssues", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ToolIssues_JournalHeaders_JournalHeaderId",
                        column: x => x.JournalHeaderId,
                        principalTable: "JournalHeaders",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ToolIssues_ToolAssets_ToolAssetId",
                        column: x => x.ToolAssetId,
                        principalTable: "ToolAssets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ToolRecoveries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RecoveryNo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    RecoveryDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ToolAssetId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ToolRecoveries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ToolRecoveries_ToolAssets_ToolAssetId",
                        column: x => x.ToolAssetId,
                        principalTable: "ToolAssets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "AccountRoleMappings",
                columns: new[] { "Id", "AccountCode", "AccountingStandardCode", "IsActive", "IsDefault", "RoleCode" },
                values: new object[,]
                {
                    { new Guid("c160e668-af9c-8039-3c5c-0b594f4afbb6"), "242", "TT200", true, true, "PrepaidExpense" },
                    { new Guid("c77a7543-adc1-2faf-e7e4-b1b94fbacf92"), "242", "TT99", true, true, "PrepaidExpense" }
                });

            migrationBuilder.InsertData(
                table: "ToolAssetCategories",
                columns: new[] { "Id", "CategoryCode", "CategoryName", "CreatedAt", "DefaultAllocationMonths", "ExpenseAccountCode", "IsActive", "PrepaidExpenseAccountCode", "ToolAccountCode", "UpdatedAt" },
                values: new object[] { new Guid("a82e9c51-1f1d-4d3f-9b25-000000000001"), "CCDC_DEFAULT", "Công cụ dụng cụ mặc định", new DateTime(2026, 5, 21, 0, 0, 0, 0, DateTimeKind.Utc), 12, null, true, null, null, new DateTime(2026, 5, 21, 0, 0, 0, 0, DateTimeKind.Utc) });

            migrationBuilder.CreateIndex(
                name: "IX_ToolAllocationRuns_AllocationPeriod",
                table: "ToolAllocationRuns",
                column: "AllocationPeriod");

            migrationBuilder.CreateIndex(
                name: "IX_ToolAllocationRuns_RunNo",
                table: "ToolAllocationRuns",
                column: "RunNo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ToolAllocationRuns_Status",
                table: "ToolAllocationRuns",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ToolAllocationSchedules_AllocationPeriod",
                table: "ToolAllocationSchedules",
                column: "AllocationPeriod");

            migrationBuilder.CreateIndex(
                name: "IX_ToolAllocationSchedules_JournalHeaderId",
                table: "ToolAllocationSchedules",
                column: "JournalHeaderId");

            migrationBuilder.CreateIndex(
                name: "IX_ToolAllocationSchedules_Status",
                table: "ToolAllocationSchedules",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ToolAllocationSchedules_ToolAssetId_AllocationPeriod",
                table: "ToolAllocationSchedules",
                columns: new[] { "ToolAssetId", "AllocationPeriod" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ToolAssetCategories_CategoryCode",
                table: "ToolAssetCategories",
                column: "CategoryCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ToolAssetCategories_IsActive",
                table: "ToolAssetCategories",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_ToolAssets_CategoryId",
                table: "ToolAssets",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_ToolAssets_JournalHeaderId",
                table: "ToolAssets",
                column: "JournalHeaderId");

            migrationBuilder.CreateIndex(
                name: "IX_ToolAssets_PurchaseDate",
                table: "ToolAssets",
                column: "PurchaseDate");

            migrationBuilder.CreateIndex(
                name: "IX_ToolAssets_Status",
                table: "ToolAssets",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ToolAssets_ToolCode",
                table: "ToolAssets",
                column: "ToolCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ToolDisposals_DisposalDate",
                table: "ToolDisposals",
                column: "DisposalDate");

            migrationBuilder.CreateIndex(
                name: "IX_ToolDisposals_DisposalNo",
                table: "ToolDisposals",
                column: "DisposalNo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ToolDisposals_JournalHeaderId",
                table: "ToolDisposals",
                column: "JournalHeaderId");

            migrationBuilder.CreateIndex(
                name: "IX_ToolDisposals_Status",
                table: "ToolDisposals",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ToolDisposals_ToolAssetId",
                table: "ToolDisposals",
                column: "ToolAssetId");

            migrationBuilder.CreateIndex(
                name: "IX_ToolIssues_IssueDate",
                table: "ToolIssues",
                column: "IssueDate");

            migrationBuilder.CreateIndex(
                name: "IX_ToolIssues_IssueNo",
                table: "ToolIssues",
                column: "IssueNo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ToolIssues_JournalHeaderId",
                table: "ToolIssues",
                column: "JournalHeaderId");

            migrationBuilder.CreateIndex(
                name: "IX_ToolIssues_Status",
                table: "ToolIssues",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ToolIssues_ToolAssetId",
                table: "ToolIssues",
                column: "ToolAssetId");

            migrationBuilder.CreateIndex(
                name: "IX_ToolRecoveries_RecoveryDate",
                table: "ToolRecoveries",
                column: "RecoveryDate");

            migrationBuilder.CreateIndex(
                name: "IX_ToolRecoveries_RecoveryNo",
                table: "ToolRecoveries",
                column: "RecoveryNo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ToolRecoveries_Status",
                table: "ToolRecoveries",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ToolRecoveries_ToolAssetId",
                table: "ToolRecoveries",
                column: "ToolAssetId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ToolAllocationRuns");

            migrationBuilder.DropTable(
                name: "ToolAllocationSchedules");

            migrationBuilder.DropTable(
                name: "ToolDisposals");

            migrationBuilder.DropTable(
                name: "ToolIssues");

            migrationBuilder.DropTable(
                name: "ToolRecoveries");

            migrationBuilder.DropTable(
                name: "ToolAssets");

            migrationBuilder.DropTable(
                name: "ToolAssetCategories");

            migrationBuilder.DeleteData(
                table: "AccountRoleMappings",
                keyColumn: "Id",
                keyValue: new Guid("c160e668-af9c-8039-3c5c-0b594f4afbb6"));

            migrationBuilder.DeleteData(
                table: "AccountRoleMappings",
                keyColumn: "Id",
                keyValue: new Guid("c77a7543-adc1-2faf-e7e4-b1b94fbacf92"));
        }
    }
}
