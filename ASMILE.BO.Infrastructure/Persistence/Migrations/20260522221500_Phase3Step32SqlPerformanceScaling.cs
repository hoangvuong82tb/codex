using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ASMILE.BO.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase3Step32SqlPerformanceScaling : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(name: "IX_AuditLogs_BranchId", table: "AuditLogs", column: "BranchId");
            migrationBuilder.CreateIndex(name: "IX_AuditLogs_CompanyId", table: "AuditLogs", column: "CompanyId");
            migrationBuilder.CreateIndex(name: "IX_AuditLogs_CompanyId_BranchId_CreatedAt", table: "AuditLogs", columns: new[] { "CompanyId", "BranchId", "CreatedAt" });

            migrationBuilder.CreateIndex(name: "IX_StockReceipts_BranchId", table: "StockReceipts", column: "BranchId");
            migrationBuilder.CreateIndex(name: "IX_StockReceipts_CompanyId", table: "StockReceipts", column: "CompanyId");
            migrationBuilder.CreateIndex(name: "IX_StockReceipts_PostingStatus", table: "StockReceipts", column: "PostingStatus");
            migrationBuilder.CreateIndex(name: "IX_StockReceipts_CompanyId_BranchId_PostingStatus_ReceiptDate", table: "StockReceipts", columns: new[] { "CompanyId", "BranchId", "PostingStatus", "ReceiptDate" });

            migrationBuilder.CreateIndex(name: "IX_StockIssues_BranchId", table: "StockIssues", column: "BranchId");
            migrationBuilder.CreateIndex(name: "IX_StockIssues_CompanyId", table: "StockIssues", column: "CompanyId");
            migrationBuilder.CreateIndex(name: "IX_StockIssues_PostingStatus", table: "StockIssues", column: "PostingStatus");
            migrationBuilder.CreateIndex(name: "IX_StockIssues_CompanyId_BranchId_PostingStatus_IssueDate", table: "StockIssues", columns: new[] { "CompanyId", "BranchId", "PostingStatus", "IssueDate" });

            migrationBuilder.CreateIndex(name: "IX_JournalHeaders_BranchId", table: "JournalHeaders", column: "BranchId");
            migrationBuilder.CreateIndex(name: "IX_JournalHeaders_CompanyId", table: "JournalHeaders", column: "CompanyId");
            migrationBuilder.CreateIndex(name: "IX_JournalHeaders_JournalDate", table: "JournalHeaders", column: "JournalDate");
            migrationBuilder.CreateIndex(name: "IX_JournalHeaders_SourceType", table: "JournalHeaders", column: "SourceType");
            migrationBuilder.CreateIndex(name: "IX_JournalHeaders_Status", table: "JournalHeaders", column: "Status");
            migrationBuilder.CreateIndex(name: "IX_JournalHeaders_CompanyId_BranchId_Status_PostingDate", table: "JournalHeaders", columns: new[] { "CompanyId", "BranchId", "Status", "PostingDate" });

            migrationBuilder.CreateIndex(name: "IX_JournalLines_BranchId", table: "JournalLines", column: "BranchId");
            migrationBuilder.CreateIndex(name: "IX_JournalLines_CompanyId", table: "JournalLines", column: "CompanyId");
            migrationBuilder.CreateIndex(name: "IX_JournalLines_CompanyId_BranchId_AccountCode", table: "JournalLines", columns: new[] { "CompanyId", "BranchId", "AccountCode" });

            migrationBuilder.CreateIndex(name: "IX_ReceivableTransactions_BranchId", table: "ReceivableTransactions", column: "BranchId");
            migrationBuilder.CreateIndex(name: "IX_ReceivableTransactions_CompanyId", table: "ReceivableTransactions", column: "CompanyId");
            migrationBuilder.CreateIndex(name: "IX_ReceivableTransactions_SourceType", table: "ReceivableTransactions", column: "SourceType");
            migrationBuilder.CreateIndex(name: "IX_ReceivableTransactions_CompanyId_BranchId_Status_DocumentDate", table: "ReceivableTransactions", columns: new[] { "CompanyId", "BranchId", "Status", "DocumentDate" });

            migrationBuilder.CreateIndex(name: "IX_PayableTransactions_BranchId", table: "PayableTransactions", column: "BranchId");
            migrationBuilder.CreateIndex(name: "IX_PayableTransactions_CompanyId", table: "PayableTransactions", column: "CompanyId");
            migrationBuilder.CreateIndex(name: "IX_PayableTransactions_SourceType", table: "PayableTransactions", column: "SourceType");
            migrationBuilder.CreateIndex(name: "IX_PayableTransactions_CompanyId_BranchId_Status_DocumentDate", table: "PayableTransactions", columns: new[] { "CompanyId", "BranchId", "Status", "DocumentDate" });

            migrationBuilder.CreateIndex(name: "IX_InventoryCostLayers_BranchId", table: "InventoryCostLayers", column: "BranchId");
            migrationBuilder.CreateIndex(name: "IX_InventoryCostLayers_CompanyId", table: "InventoryCostLayers", column: "CompanyId");
            migrationBuilder.CreateIndex(name: "IX_InventoryCostLayers_SourceType", table: "InventoryCostLayers", column: "SourceType");
            migrationBuilder.CreateIndex(name: "IX_InventoryCostLayers_CompanyId_BranchId_Status_DocumentDate", table: "InventoryCostLayers", columns: new[] { "CompanyId", "BranchId", "Status", "DocumentDate" });

            migrationBuilder.CreateIndex(name: "IX_InventoryCostTransactions_BranchId", table: "InventoryCostTransactions", column: "BranchId");
            migrationBuilder.CreateIndex(name: "IX_InventoryCostTransactions_CompanyId", table: "InventoryCostTransactions", column: "CompanyId");
            migrationBuilder.CreateIndex(name: "IX_InventoryCostTransactions_SourceType", table: "InventoryCostTransactions", column: "SourceType");
            migrationBuilder.CreateIndex(name: "IX_InventoryCostTransactions_CompanyId_BranchId_DocumentDate", table: "InventoryCostTransactions", columns: new[] { "CompanyId", "BranchId", "DocumentDate" });

            migrationBuilder.CreateIndex(name: "IX_ReportExecutions_BranchId", table: "ReportExecutions", column: "BranchId");
            migrationBuilder.CreateIndex(name: "IX_ReportExecutions_CompanyId", table: "ReportExecutions", column: "CompanyId");
            migrationBuilder.CreateIndex(name: "IX_ReportExecutions_CompanyId_BranchId_ReportCode_StartedAt", table: "ReportExecutions", columns: new[] { "CompanyId", "BranchId", "ReportCode", "StartedAt" });

            migrationBuilder.CreateIndex(name: "IX_VatTransactions_BranchId", table: "VatTransactions", column: "BranchId");
            migrationBuilder.CreateIndex(name: "IX_VatTransactions_CompanyId", table: "VatTransactions", column: "CompanyId");
            migrationBuilder.CreateIndex(name: "IX_VatTransactions_SourceType", table: "VatTransactions", column: "SourceType");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(name: "IX_VatTransactions_SourceType", table: "VatTransactions");
            migrationBuilder.DropIndex(name: "IX_VatTransactions_CompanyId", table: "VatTransactions");
            migrationBuilder.DropIndex(name: "IX_VatTransactions_BranchId", table: "VatTransactions");

            migrationBuilder.DropIndex(name: "IX_ReportExecutions_CompanyId_BranchId_ReportCode_StartedAt", table: "ReportExecutions");
            migrationBuilder.DropIndex(name: "IX_ReportExecutions_CompanyId", table: "ReportExecutions");
            migrationBuilder.DropIndex(name: "IX_ReportExecutions_BranchId", table: "ReportExecutions");

            migrationBuilder.DropIndex(name: "IX_InventoryCostTransactions_CompanyId_BranchId_DocumentDate", table: "InventoryCostTransactions");
            migrationBuilder.DropIndex(name: "IX_InventoryCostTransactions_SourceType", table: "InventoryCostTransactions");
            migrationBuilder.DropIndex(name: "IX_InventoryCostTransactions_CompanyId", table: "InventoryCostTransactions");
            migrationBuilder.DropIndex(name: "IX_InventoryCostTransactions_BranchId", table: "InventoryCostTransactions");

            migrationBuilder.DropIndex(name: "IX_InventoryCostLayers_CompanyId_BranchId_Status_DocumentDate", table: "InventoryCostLayers");
            migrationBuilder.DropIndex(name: "IX_InventoryCostLayers_SourceType", table: "InventoryCostLayers");
            migrationBuilder.DropIndex(name: "IX_InventoryCostLayers_CompanyId", table: "InventoryCostLayers");
            migrationBuilder.DropIndex(name: "IX_InventoryCostLayers_BranchId", table: "InventoryCostLayers");

            migrationBuilder.DropIndex(name: "IX_PayableTransactions_CompanyId_BranchId_Status_DocumentDate", table: "PayableTransactions");
            migrationBuilder.DropIndex(name: "IX_PayableTransactions_SourceType", table: "PayableTransactions");
            migrationBuilder.DropIndex(name: "IX_PayableTransactions_CompanyId", table: "PayableTransactions");
            migrationBuilder.DropIndex(name: "IX_PayableTransactions_BranchId", table: "PayableTransactions");

            migrationBuilder.DropIndex(name: "IX_ReceivableTransactions_CompanyId_BranchId_Status_DocumentDate", table: "ReceivableTransactions");
            migrationBuilder.DropIndex(name: "IX_ReceivableTransactions_SourceType", table: "ReceivableTransactions");
            migrationBuilder.DropIndex(name: "IX_ReceivableTransactions_CompanyId", table: "ReceivableTransactions");
            migrationBuilder.DropIndex(name: "IX_ReceivableTransactions_BranchId", table: "ReceivableTransactions");

            migrationBuilder.DropIndex(name: "IX_JournalLines_CompanyId_BranchId_AccountCode", table: "JournalLines");
            migrationBuilder.DropIndex(name: "IX_JournalLines_CompanyId", table: "JournalLines");
            migrationBuilder.DropIndex(name: "IX_JournalLines_BranchId", table: "JournalLines");

            migrationBuilder.DropIndex(name: "IX_JournalHeaders_CompanyId_BranchId_Status_PostingDate", table: "JournalHeaders");
            migrationBuilder.DropIndex(name: "IX_JournalHeaders_Status", table: "JournalHeaders");
            migrationBuilder.DropIndex(name: "IX_JournalHeaders_SourceType", table: "JournalHeaders");
            migrationBuilder.DropIndex(name: "IX_JournalHeaders_JournalDate", table: "JournalHeaders");
            migrationBuilder.DropIndex(name: "IX_JournalHeaders_CompanyId", table: "JournalHeaders");
            migrationBuilder.DropIndex(name: "IX_JournalHeaders_BranchId", table: "JournalHeaders");

            migrationBuilder.DropIndex(name: "IX_StockIssues_CompanyId_BranchId_PostingStatus_IssueDate", table: "StockIssues");
            migrationBuilder.DropIndex(name: "IX_StockIssues_PostingStatus", table: "StockIssues");
            migrationBuilder.DropIndex(name: "IX_StockIssues_CompanyId", table: "StockIssues");
            migrationBuilder.DropIndex(name: "IX_StockIssues_BranchId", table: "StockIssues");

            migrationBuilder.DropIndex(name: "IX_StockReceipts_CompanyId_BranchId_PostingStatus_ReceiptDate", table: "StockReceipts");
            migrationBuilder.DropIndex(name: "IX_StockReceipts_PostingStatus", table: "StockReceipts");
            migrationBuilder.DropIndex(name: "IX_StockReceipts_CompanyId", table: "StockReceipts");
            migrationBuilder.DropIndex(name: "IX_StockReceipts_BranchId", table: "StockReceipts");

            migrationBuilder.DropIndex(name: "IX_AuditLogs_CompanyId_BranchId_CreatedAt", table: "AuditLogs");
            migrationBuilder.DropIndex(name: "IX_AuditLogs_CompanyId", table: "AuditLogs");
            migrationBuilder.DropIndex(name: "IX_AuditLogs_BranchId", table: "AuditLogs");
        }
    }
}
