using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ASMILE.BO.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase3Step34EInvoiceFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "InvoiceProviderConfigurations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ProviderCode = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    EndpointUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    TenantCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Username = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    ProtectedSecret = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table => table.PrimaryKey("PK_InvoiceProviderConfigurations", x => x.Id));

            migrationBuilder.CreateTable(
                name: "Invoices",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    InvoiceNo = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    InvoiceSeries = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    TemplateCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    InvoiceDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    BuyerName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    BuyerTaxCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    BuyerAddress = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CurrencyCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false, defaultValue: "VND"),
                    ExchangeRate = table.Column<decimal>(type: "decimal(18,6)", nullable: false),
                    SubtotalAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TaxAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    ProviderCode = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    ProviderInvoiceId = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    JournalHeaderId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ReceivableTransactionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PayableTransactionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    VatDeclarationDraftId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    XmlPayload = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PdfPreviewPath = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IssuedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Invoices", x => x.Id);
                    table.ForeignKey(name: "FK_Invoices_JournalHeaders_JournalHeaderId", column: x => x.JournalHeaderId, principalTable: "JournalHeaders", principalColumn: "Id");
                    table.ForeignKey(name: "FK_Invoices_PayableTransactions_PayableTransactionId", column: x => x.PayableTransactionId, principalTable: "PayableTransactions", principalColumn: "Id");
                    table.ForeignKey(name: "FK_Invoices_ReceivableTransactions_ReceivableTransactionId", column: x => x.ReceivableTransactionId, principalTable: "ReceivableTransactions", principalColumn: "Id");
                    table.ForeignKey(name: "FK_Invoices_TaxDeclarationDrafts_VatDeclarationDraftId", column: x => x.VatDeclarationDraftId, principalTable: "TaxDeclarationDrafts", principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "InvoiceLines",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    InvoiceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LineNo = table.Column<int>(type: "int", nullable: false),
                    ItemCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ItemName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    UnitName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,6)", nullable: false),
                    LineAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TaxRate = table.Column<decimal>(type: "decimal(9,4)", nullable: false),
                    TaxAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    JournalAccountCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InvoiceLines", x => x.Id);
                    table.ForeignKey(name: "FK_InvoiceLines_Invoices_InvoiceId", column: x => x.InvoiceId, principalTable: "Invoices", principalColumn: "Id", onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "InvoiceTaxBreakdowns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    InvoiceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TaxRate = table.Column<decimal>(type: "decimal(9,4)", nullable: false),
                    TaxBaseAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TaxAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TaxCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InvoiceTaxBreakdowns", x => x.Id);
                    table.ForeignKey(name: "FK_InvoiceTaxBreakdowns_Invoices_InvoiceId", column: x => x.InvoiceId, principalTable: "Invoices", principalColumn: "Id", onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "InvoiceIssueLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    InvoiceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProviderCode = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    AttemptNo = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RequestXml = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ResponsePayload = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ErrorMessage = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CorrelationId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InvoiceIssueLogs", x => x.Id);
                    table.ForeignKey(name: "FK_InvoiceIssueLogs_Invoices_InvoiceId", column: x => x.InvoiceId, principalTable: "Invoices", principalColumn: "Id", onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(name: "IX_InvoiceProviderConfigurations_ProviderCode", table: "InvoiceProviderConfigurations", column: "ProviderCode");
            migrationBuilder.CreateIndex(name: "IX_InvoiceProviderConfigurations_CompanyId_BranchId_ProviderCode", table: "InvoiceProviderConfigurations", columns: new[] { "CompanyId", "BranchId", "ProviderCode" }, unique: true, filter: "[CompanyId] IS NOT NULL AND [BranchId] IS NOT NULL");
            migrationBuilder.CreateIndex(name: "IX_Invoices_InvoiceNo", table: "Invoices", column: "InvoiceNo", unique: true);
            migrationBuilder.CreateIndex(name: "IX_Invoices_Status", table: "Invoices", column: "Status");
            migrationBuilder.CreateIndex(name: "IX_Invoices_ProviderCode", table: "Invoices", column: "ProviderCode");
            migrationBuilder.CreateIndex(name: "IX_Invoices_CompanyId_BranchId_Status_InvoiceDate", table: "Invoices", columns: new[] { "CompanyId", "BranchId", "Status", "InvoiceDate" });
            migrationBuilder.CreateIndex(name: "IX_Invoices_JournalHeaderId", table: "Invoices", column: "JournalHeaderId");
            migrationBuilder.CreateIndex(name: "IX_Invoices_PayableTransactionId", table: "Invoices", column: "PayableTransactionId");
            migrationBuilder.CreateIndex(name: "IX_Invoices_ReceivableTransactionId", table: "Invoices", column: "ReceivableTransactionId");
            migrationBuilder.CreateIndex(name: "IX_Invoices_VatDeclarationDraftId", table: "Invoices", column: "VatDeclarationDraftId");
            migrationBuilder.CreateIndex(name: "IX_InvoiceLines_InvoiceId", table: "InvoiceLines", column: "InvoiceId");
            migrationBuilder.CreateIndex(name: "IX_InvoiceLines_ItemCode", table: "InvoiceLines", column: "ItemCode");
            migrationBuilder.CreateIndex(name: "IX_InvoiceTaxBreakdowns_InvoiceId", table: "InvoiceTaxBreakdowns", column: "InvoiceId");
            migrationBuilder.CreateIndex(name: "IX_InvoiceIssueLogs_InvoiceId", table: "InvoiceIssueLogs", column: "InvoiceId");
            migrationBuilder.CreateIndex(name: "IX_InvoiceIssueLogs_Status", table: "InvoiceIssueLogs", column: "Status");
            migrationBuilder.CreateIndex(name: "IX_InvoiceIssueLogs_CorrelationId", table: "InvoiceIssueLogs", column: "CorrelationId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "InvoiceIssueLogs");
            migrationBuilder.DropTable(name: "InvoiceLines");
            migrationBuilder.DropTable(name: "InvoiceProviderConfigurations");
            migrationBuilder.DropTable(name: "InvoiceTaxBreakdowns");
            migrationBuilder.DropTable(name: "Invoices");
        }
    }
}
