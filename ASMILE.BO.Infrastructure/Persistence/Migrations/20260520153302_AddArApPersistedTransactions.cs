using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ASMILE.BO.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddArApPersistedTransactions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AgingSnapshots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SnapshotDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PartnerType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    PartnerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CurrentAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Overdue1To30 = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Overdue31To60 = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Overdue61To90 = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    OverdueOver90 = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AgingSnapshots", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DebtReconciliations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReconciliationNo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PartnerType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    PartnerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FromDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ToDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    TotalDebit = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalCredit = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Balance = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    ConfirmedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ConfirmedBy = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DebtReconciliations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PayableTransactions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SupplierId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SourceType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    SourceId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    JournalHeaderId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    JournalLineId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DocumentNo = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DocumentDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DueDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    DebitAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CreditAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    BalanceAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CurrencyCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false, defaultValue: "VND"),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false, defaultValue: "Open"),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PayableTransactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PayableTransactions_JournalHeaders_JournalHeaderId",
                        column: x => x.JournalHeaderId,
                        principalTable: "JournalHeaders",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PayableTransactions_JournalLines_JournalLineId",
                        column: x => x.JournalLineId,
                        principalTable: "JournalLines",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "PaymentAllocations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PaymentSourceType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PaymentSourceId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PaymentJournalHeaderId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TargetType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    TargetTransactionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AllocatedAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    AllocationDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentAllocations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PaymentAllocations_JournalHeaders_PaymentJournalHeaderId",
                        column: x => x.PaymentJournalHeaderId,
                        principalTable: "JournalHeaders",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ReceivableTransactions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SourceType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    SourceId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    JournalHeaderId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    JournalLineId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DocumentNo = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DocumentDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DueDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    DebitAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CreditAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    BalanceAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CurrencyCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false, defaultValue: "VND"),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false, defaultValue: "Open"),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReceivableTransactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReceivableTransactions_JournalHeaders_JournalHeaderId",
                        column: x => x.JournalHeaderId,
                        principalTable: "JournalHeaders",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ReceivableTransactions_JournalLines_JournalLineId",
                        column: x => x.JournalLineId,
                        principalTable: "JournalLines",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "DebtReconciliationLines",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DebtReconciliationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SourceType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    SourceId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DocumentNo = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DocumentDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DebitAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CreditAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    BalanceAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DebtReconciliationLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DebtReconciliationLines_DebtReconciliations_DebtReconciliationId",
                        column: x => x.DebtReconciliationId,
                        principalTable: "DebtReconciliations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "AccountRoleMappings",
                columns: new[] { "Id", "AccountCode", "AccountingStandardCode", "IsActive", "IsDefault", "RoleCode" },
                values: new object[] { new Guid("6f6b8f41-9c2d-4d66-9b2f-000000000131"), "1311", "TT99", true, true, "Receivable" });

            migrationBuilder.CreateIndex(
                name: "IX_AgingSnapshots_PartnerType_PartnerId",
                table: "AgingSnapshots",
                columns: new[] { "PartnerType", "PartnerId" });

            migrationBuilder.CreateIndex(
                name: "IX_AgingSnapshots_SnapshotDate",
                table: "AgingSnapshots",
                column: "SnapshotDate");

            migrationBuilder.CreateIndex(
                name: "IX_DebtReconciliationLines_DebtReconciliationId",
                table: "DebtReconciliationLines",
                column: "DebtReconciliationId");

            migrationBuilder.CreateIndex(
                name: "IX_DebtReconciliations_PartnerType_PartnerId",
                table: "DebtReconciliations",
                columns: new[] { "PartnerType", "PartnerId" });

            migrationBuilder.CreateIndex(
                name: "IX_DebtReconciliations_ReconciliationNo",
                table: "DebtReconciliations",
                column: "ReconciliationNo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DebtReconciliations_Status",
                table: "DebtReconciliations",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_PayableTransactions_JournalHeaderId",
                table: "PayableTransactions",
                column: "JournalHeaderId");

            migrationBuilder.CreateIndex(
                name: "IX_PayableTransactions_JournalLineId",
                table: "PayableTransactions",
                column: "JournalLineId",
                unique: true,
                filter: "[JournalLineId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_PayableTransactions_SourceType_SourceId",
                table: "PayableTransactions",
                columns: new[] { "SourceType", "SourceId" });

            migrationBuilder.CreateIndex(
                name: "IX_PayableTransactions_Status",
                table: "PayableTransactions",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_PayableTransactions_SupplierId",
                table: "PayableTransactions",
                column: "SupplierId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentAllocations_PaymentJournalHeaderId",
                table: "PaymentAllocations",
                column: "PaymentJournalHeaderId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentAllocations_TargetType_TargetTransactionId",
                table: "PaymentAllocations",
                columns: new[] { "TargetType", "TargetTransactionId" });

            migrationBuilder.CreateIndex(
                name: "IX_ReceivableTransactions_CustomerId",
                table: "ReceivableTransactions",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_ReceivableTransactions_JournalHeaderId",
                table: "ReceivableTransactions",
                column: "JournalHeaderId");

            migrationBuilder.CreateIndex(
                name: "IX_ReceivableTransactions_JournalLineId",
                table: "ReceivableTransactions",
                column: "JournalLineId",
                unique: true,
                filter: "[JournalLineId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ReceivableTransactions_SourceType_SourceId",
                table: "ReceivableTransactions",
                columns: new[] { "SourceType", "SourceId" });

            migrationBuilder.CreateIndex(
                name: "IX_ReceivableTransactions_Status",
                table: "ReceivableTransactions",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AgingSnapshots");

            migrationBuilder.DropTable(
                name: "DebtReconciliationLines");

            migrationBuilder.DropTable(
                name: "PayableTransactions");

            migrationBuilder.DropTable(
                name: "PaymentAllocations");

            migrationBuilder.DropTable(
                name: "ReceivableTransactions");

            migrationBuilder.DropTable(
                name: "DebtReconciliations");

            migrationBuilder.DeleteData(
                table: "AccountRoleMappings",
                keyColumn: "Id",
                keyValue: new Guid("6f6b8f41-9c2d-4d66-9b2f-000000000131"));
        }
    }
}
