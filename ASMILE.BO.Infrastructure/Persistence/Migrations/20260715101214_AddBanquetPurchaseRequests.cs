using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ASMILE.BO.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddBanquetPurchaseRequests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BanquetPurchaseRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RequestNo = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    BanquetEventId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BanquetQuantificationBatchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequiredDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    RequestedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RequestedBy = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    Note = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BanquetPurchaseRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BanquetPurchaseRequests_BanquetEvents_BanquetEventId",
                        column: x => x.BanquetEventId,
                        principalTable: "BanquetEvents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BanquetPurchaseRequests_BanquetQuantificationBatches_BanquetQuantificationBatchId",
                        column: x => x.BanquetQuantificationBatchId,
                        principalTable: "BanquetQuantificationBatches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BanquetPurchaseRequestLines",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BanquetPurchaseRequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    ItemCode = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    ItemName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    UnitName = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    RequestedQuantity = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    EstimatedCost = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BanquetPurchaseRequestLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BanquetPurchaseRequestLines_BanquetPurchaseRequests_BanquetPurchaseRequestId",
                        column: x => x.BanquetPurchaseRequestId,
                        principalTable: "BanquetPurchaseRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BanquetPurchaseRequestLines_BanquetPurchaseRequestId_SortOrder",
                table: "BanquetPurchaseRequestLines",
                columns: new[] { "BanquetPurchaseRequestId", "SortOrder" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BanquetPurchaseRequests_BanquetEventId",
                table: "BanquetPurchaseRequests",
                column: "BanquetEventId");

            migrationBuilder.CreateIndex(
                name: "IX_BanquetPurchaseRequests_BanquetQuantificationBatchId",
                table: "BanquetPurchaseRequests",
                column: "BanquetQuantificationBatchId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BanquetPurchaseRequests_RequestNo",
                table: "BanquetPurchaseRequests",
                column: "RequestNo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BanquetPurchaseRequests_RequiredDate_Status",
                table: "BanquetPurchaseRequests",
                columns: new[] { "RequiredDate", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BanquetPurchaseRequestLines");

            migrationBuilder.DropTable(
                name: "BanquetPurchaseRequests");
        }
    }
}
