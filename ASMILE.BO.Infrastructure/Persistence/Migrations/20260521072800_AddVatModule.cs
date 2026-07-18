using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ASMILE.BO.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddVatModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TaxCodes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    TaxRate = table.Column<decimal>(type: "decimal(9,4)", precision: 9, scale: 4, nullable: false),
                    TaxType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    InputAccountCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    OutputAccountCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaxCodes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TaxDeclarationDrafts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DeclarationPeriod = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    TaxType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    InputVatAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    OutputVatAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    NetVatPayable = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaxDeclarationDrafts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "VatTransactions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TaxCodeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SourceType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    SourceId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    JournalHeaderId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    JournalLineId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    InvoiceNo = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    InvoiceDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PartnerType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    PartnerId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TaxBaseAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TaxRate = table.Column<decimal>(type: "decimal(9,4)", precision: 9, scale: 4, nullable: false),
                    TaxAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Direction = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    DeclarationPeriod = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VatTransactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VatTransactions_JournalHeaders_JournalHeaderId",
                        column: x => x.JournalHeaderId,
                        principalTable: "JournalHeaders",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_VatTransactions_JournalLines_JournalLineId",
                        column: x => x.JournalLineId,
                        principalTable: "JournalLines",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_VatTransactions_TaxCodes_TaxCodeId",
                        column: x => x.TaxCodeId,
                        principalTable: "TaxCodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "ReportTemplates",
                columns: new[] { "Id", "Category", "Code", "CreatedAt", "DataSource", "Description", "IsActive", "IsSystem", "LayoutDefinition", "Name", "QueryDefinition", "UpdatedAt" },
                values: new object[,]
                {
                    { new Guid("4a9a7b4f-ac6e-4e91-9a11-000000000007"), "Tax", "VAT_INPUT", new DateTime(2026, 5, 21, 0, 0, 0, 0, DateTimeKind.Utc), "VatTransaction", "Bảng kê thuế GTGT đầu vào từ giao dịch VAT persisted.", true, true, null, "Bảng kê VAT đầu vào", null, new DateTime(2026, 5, 21, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("4a9a7b4f-ac6e-4e91-9a11-000000000008"), "Tax", "VAT_OUTPUT", new DateTime(2026, 5, 21, 0, 0, 0, 0, DateTimeKind.Utc), "VatTransaction", "Bảng kê thuế GTGT đầu ra từ giao dịch VAT persisted.", true, true, null, "Bảng kê VAT đầu ra", null, new DateTime(2026, 5, 21, 0, 0, 0, 0, DateTimeKind.Utc) }
                });

            migrationBuilder.InsertData(
                table: "TaxCodes",
                columns: new[] { "Id", "Code", "CreatedAt", "InputAccountCode", "IsActive", "Name", "OutputAccountCode", "TaxRate", "TaxType", "UpdatedAt" },
                values: new object[,]
                {
                    { new Guid("b7ac6c11-2a40-4d60-9b25-000000000000"), "VAT0", new DateTime(2026, 5, 21, 0, 0, 0, 0, DateTimeKind.Utc), null, true, "Thuế GTGT 0%", null, 0m, "VATInput", new DateTime(2026, 5, 21, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("b7ac6c11-2a40-4d60-9b25-000000000005"), "VAT5", new DateTime(2026, 5, 21, 0, 0, 0, 0, DateTimeKind.Utc), null, true, "Thuế GTGT 5%", null, 5m, "VATInput", new DateTime(2026, 5, 21, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("b7ac6c11-2a40-4d60-9b25-000000000008"), "VAT8", new DateTime(2026, 5, 21, 0, 0, 0, 0, DateTimeKind.Utc), null, true, "Thuế GTGT 8%", null, 8m, "VATInput", new DateTime(2026, 5, 21, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("b7ac6c11-2a40-4d60-9b25-000000000010"), "VAT10", new DateTime(2026, 5, 21, 0, 0, 0, 0, DateTimeKind.Utc), null, true, "Thuế GTGT 10%", null, 10m, "VATInput", new DateTime(2026, 5, 21, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("b7ac6c11-2a40-4d60-9b25-000000000999"), "KCT", new DateTime(2026, 5, 21, 0, 0, 0, 0, DateTimeKind.Utc), null, true, "Không chịu thuế", null, 0m, "NonTaxable", new DateTime(2026, 5, 21, 0, 0, 0, 0, DateTimeKind.Utc) }
                });

            migrationBuilder.CreateIndex(
                name: "IX_TaxCodes_Code",
                table: "TaxCodes",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TaxCodes_IsActive",
                table: "TaxCodes",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_TaxCodes_TaxType",
                table: "TaxCodes",
                column: "TaxType");

            migrationBuilder.CreateIndex(
                name: "IX_TaxDeclarationDrafts_DeclarationPeriod_TaxType",
                table: "TaxDeclarationDrafts",
                columns: new[] { "DeclarationPeriod", "TaxType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TaxDeclarationDrafts_Status",
                table: "TaxDeclarationDrafts",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_VatTransactions_DeclarationPeriod",
                table: "VatTransactions",
                column: "DeclarationPeriod");

            migrationBuilder.CreateIndex(
                name: "IX_VatTransactions_Direction",
                table: "VatTransactions",
                column: "Direction");

            migrationBuilder.CreateIndex(
                name: "IX_VatTransactions_JournalHeaderId",
                table: "VatTransactions",
                column: "JournalHeaderId");

            migrationBuilder.CreateIndex(
                name: "IX_VatTransactions_JournalLineId",
                table: "VatTransactions",
                column: "JournalLineId",
                unique: true,
                filter: "[JournalLineId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_VatTransactions_SourceType_SourceId",
                table: "VatTransactions",
                columns: new[] { "SourceType", "SourceId" });

            migrationBuilder.CreateIndex(
                name: "IX_VatTransactions_Status",
                table: "VatTransactions",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_VatTransactions_TaxCodeId",
                table: "VatTransactions",
                column: "TaxCodeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TaxDeclarationDrafts");

            migrationBuilder.DropTable(
                name: "VatTransactions");

            migrationBuilder.DropTable(
                name: "TaxCodes");

            migrationBuilder.DeleteData(
                table: "ReportTemplates",
                keyColumn: "Id",
                keyValue: new Guid("4a9a7b4f-ac6e-4e91-9a11-000000000007"));

            migrationBuilder.DeleteData(
                table: "ReportTemplates",
                keyColumn: "Id",
                keyValue: new Guid("4a9a7b4f-ac6e-4e91-9a11-000000000008"));
        }
    }
}
