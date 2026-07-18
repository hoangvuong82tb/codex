using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ASMILE.BO.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddQuoteChangeRequest : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "QuoteChangeRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    QuoteId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequestNumber = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    PreviousGuestCount = table.Column<int>(type: "int", nullable: false),
                    NewGuestCount = table.Column<int>(type: "int", nullable: false),
                    GuestCountDelta = table.Column<int>(type: "int", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    RequestedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    RequestedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReviewedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ReviewedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AppliedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    AppliedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PreviousTotalAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    NewTotalAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    AmountDelta = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    AdjustmentPlanJson = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    PreviousOrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NewOrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Note = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuoteChangeRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QuoteChangeRequests_AlacarteOrders_NewOrderId",
                        column: x => x.NewOrderId,
                        principalTable: "AlacarteOrders",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_QuoteChangeRequests_AlacarteOrders_PreviousOrderId",
                        column: x => x.PreviousOrderId,
                        principalTable: "AlacarteOrders",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_QuoteChangeRequests_RestaurantQuotes_QuoteId",
                        column: x => x.QuoteId,
                        principalTable: "RestaurantQuotes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "QuoteAuditLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    QuoteId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Action = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    ChangedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    FieldName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    OldValue = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    NewValue = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ChangeRequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Note = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuoteAuditLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QuoteAuditLogs_QuoteChangeRequests_ChangeRequestId",
                        column: x => x.ChangeRequestId,
                        principalTable: "QuoteChangeRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_QuoteAuditLogs_RestaurantQuotes_QuoteId",
                        column: x => x.QuoteId,
                        principalTable: "RestaurantQuotes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.NoAction);
                });

            migrationBuilder.CreateTable(
                name: "QuoteChangeRequestDetails",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ChangeRequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    QuoteLineId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    OrderLineId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ChangeType = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    ItemName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    PreviousQuantity = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    NewQuantity = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    PreviousAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    NewAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    OrderTableId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsAlreadySent = table.Column<bool>(type: "bit", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuoteChangeRequestDetails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QuoteChangeRequestDetails_QuoteChangeRequests_ChangeRequestId",
                        column: x => x.ChangeRequestId,
                        principalTable: "QuoteChangeRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_QuoteAuditLogs_ChangeRequestId",
                table: "QuoteAuditLogs",
                column: "ChangeRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_QuoteAuditLogs_QuoteId",
                table: "QuoteAuditLogs",
                column: "QuoteId");

            migrationBuilder.CreateIndex(
                name: "IX_QuoteAuditLogs_Timestamp",
                table: "QuoteAuditLogs",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_QuoteChangeRequestDetails_ChangeRequestId",
                table: "QuoteChangeRequestDetails",
                column: "ChangeRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_QuoteChangeRequests_NewOrderId",
                table: "QuoteChangeRequests",
                column: "NewOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_QuoteChangeRequests_PreviousOrderId",
                table: "QuoteChangeRequests",
                column: "PreviousOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_QuoteChangeRequests_QuoteId",
                table: "QuoteChangeRequests",
                column: "QuoteId");

            migrationBuilder.CreateIndex(
                name: "IX_QuoteChangeRequests_RequestNumber",
                table: "QuoteChangeRequests",
                column: "RequestNumber",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "QuoteAuditLogs");

            migrationBuilder.DropTable(
                name: "QuoteChangeRequestDetails");

            migrationBuilder.DropTable(
                name: "QuoteChangeRequests");
        }
    }
}
