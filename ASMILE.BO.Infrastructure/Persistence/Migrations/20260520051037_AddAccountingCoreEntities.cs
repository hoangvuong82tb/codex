using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ASMILE.BO.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAccountingCoreEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AccountingPeriods",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PeriodCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    FromDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ToDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    LockedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LockedBy = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccountingPeriods", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AccountingStandards",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    EffectiveFrom = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccountingStandards", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AccountRoleMappings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AccountingStandardCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    RoleCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    AccountCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccountRoleMappings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AccountTagMappings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AccountCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    TagCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccountTagMappings", x => x.Id);
                });


            migrationBuilder.CreateTable(
                name: "JournalHeaders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    JournalNo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    JournalDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PostingDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SourceType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    SourceId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SourceNo = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    PeriodCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    TotalDebit = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalCredit = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Difference = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PostedBy = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    PostedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReversedBy = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    ReversedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JournalHeaders", x => x.Id);
                });


            migrationBuilder.CreateTable(
                name: "PostingBatches",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BatchNo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    SourceType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    SourceId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Message = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PostingBatches", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PostingRules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    SourceType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    AccountingStandardCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PostingRules", x => x.Id);
                });




            migrationBuilder.CreateTable(
                name: "JournalLines",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    JournalHeaderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LineNo = table.Column<int>(type: "int", nullable: false),
                    AccountCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    AccountName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    DebitAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CreditAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CurrencyCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false, defaultValue: "VND"),
                    ExchangeRate = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false, defaultValue: 1m),
                    ObjectType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ObjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CustomerId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SupplierId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    WarehouseId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DepartmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    EmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JournalLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_JournalLines_JournalHeaders_JournalHeaderId",
                        column: x => x.JournalHeaderId,
                        principalTable: "JournalHeaders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PostingSources",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SourceType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    SourceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SourceNo = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    JournalHeaderId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PostingSources", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PostingSources_JournalHeaders_JournalHeaderId",
                        column: x => x.JournalHeaderId,
                        principalTable: "JournalHeaders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PostingRuleLines",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PostingRuleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LineNo = table.Column<int>(type: "int", nullable: false),
                    DebitCredit = table.Column<int>(type: "int", nullable: false),
                    AccountRole = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    FixedAccountCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Formula = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DescriptionTemplate = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PostingRuleLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PostingRuleLines_PostingRules_PostingRuleId",
                        column: x => x.PostingRuleId,
                        principalTable: "PostingRules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "AccountingStandards",
                columns: new[] { "Id", "Code", "CreatedAt", "EffectiveFrom", "IsActive", "IsDefault", "Name", "UpdatedAt" },
                values: new object[,]
                {
                    { new Guid("2e7ce9b2-4e4d-4f75-8e5e-000000000099"), "TT99", new DateTime(2026, 5, 20, 0, 0, 0, 0, DateTimeKind.Utc), null, true, true, "Thông tư 99", new DateTime(2026, 5, 20, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("2e7ce9b2-4e4d-4f75-8e5e-000000000200"), "TT200", new DateTime(2026, 5, 20, 0, 0, 0, 0, DateTimeKind.Utc), null, true, false, "Thông tư 200", new DateTime(2026, 5, 20, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("2e7ce9b2-4e4d-4f75-8e5e-000000000999"), "CUSTOM", new DateTime(2026, 5, 20, 0, 0, 0, 0, DateTimeKind.Utc), null, true, false, "Tùy chỉnh", new DateTime(2026, 5, 20, 0, 0, 0, 0, DateTimeKind.Utc) }
                });

            migrationBuilder.CreateIndex(
                name: "IX_AccountingPeriods_CompanyId_PeriodCode",
                table: "AccountingPeriods",
                columns: new[] { "CompanyId", "PeriodCode" },
                unique: true,
                filter: "[CompanyId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AccountingPeriods_PeriodCode",
                table: "AccountingPeriods",
                column: "PeriodCode");

            migrationBuilder.CreateIndex(
                name: "IX_AccountingStandards_Code",
                table: "AccountingStandards",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AccountRoleMappings_AccountingStandardCode_RoleCode_AccountCode",
                table: "AccountRoleMappings",
                columns: new[] { "AccountingStandardCode", "RoleCode", "AccountCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AccountRoleMappings_AccountingStandardCode_RoleCode_IsDefault",
                table: "AccountRoleMappings",
                columns: new[] { "AccountingStandardCode", "RoleCode", "IsDefault" });

            migrationBuilder.CreateIndex(
                name: "IX_AccountTagMappings_AccountCode_TagCode",
                table: "AccountTagMappings",
                columns: new[] { "AccountCode", "TagCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_JournalHeaders_JournalNo",
                table: "JournalHeaders",
                column: "JournalNo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_JournalHeaders_PeriodCode",
                table: "JournalHeaders",
                column: "PeriodCode");

            migrationBuilder.CreateIndex(
                name: "IX_JournalHeaders_SourceType_SourceId",
                table: "JournalHeaders",
                columns: new[] { "SourceType", "SourceId" });

            migrationBuilder.CreateIndex(
                name: "IX_JournalLines_AccountCode",
                table: "JournalLines",
                column: "AccountCode");

            migrationBuilder.CreateIndex(
                name: "IX_JournalLines_JournalHeaderId",
                table: "JournalLines",
                column: "JournalHeaderId");

            migrationBuilder.CreateIndex(
                name: "IX_PostingBatches_BatchNo",
                table: "PostingBatches",
                column: "BatchNo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PostingRuleLines_PostingRuleId",
                table: "PostingRuleLines",
                column: "PostingRuleId");

            migrationBuilder.CreateIndex(
                name: "IX_PostingRules_Code",
                table: "PostingRules",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PostingRules_SourceType_AccountingStandardCode",
                table: "PostingRules",
                columns: new[] { "SourceType", "AccountingStandardCode" });

            migrationBuilder.CreateIndex(
                name: "IX_PostingSources_JournalHeaderId",
                table: "PostingSources",
                column: "JournalHeaderId");

            migrationBuilder.CreateIndex(
                name: "IX_PostingSources_SourceType_SourceId",
                table: "PostingSources",
                columns: new[] { "SourceType", "SourceId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AccountingPeriods");

            migrationBuilder.DropTable(
                name: "AccountingStandards");

            migrationBuilder.DropTable(
                name: "AccountRoleMappings");

            migrationBuilder.DropTable(
                name: "AccountTagMappings");


            migrationBuilder.DropTable(
                name: "JournalLines");


            migrationBuilder.DropTable(
                name: "PostingBatches");

            migrationBuilder.DropTable(
                name: "PostingRuleLines");

            migrationBuilder.DropTable(
                name: "PostingSources");




            migrationBuilder.DropTable(
                name: "PostingRules");

            migrationBuilder.DropTable(
                name: "JournalHeaders");
        }
    }
}
