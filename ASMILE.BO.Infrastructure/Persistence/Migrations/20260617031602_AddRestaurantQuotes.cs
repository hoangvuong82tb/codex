using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ASMILE.BO.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRestaurantQuotes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RestaurantQuotes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    QuoteNo = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    CustomerName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Phone = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    TableCount = table.Column<int>(type: "int", nullable: false),
                    DiningRoomId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DiningTableId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TableReservationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ApprovedOrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DepositId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DepositCashTransactionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DepositCashTransactionNo = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: true),
                    RoomName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    EventDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    StartTime = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    EndTime = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    DepositAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    PaymentDeadline = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    PaymentMethod = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    DiscountAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Promotion = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    ValidityDays = table.Column<int>(type: "int", nullable: false),
                    CancelDeadlineDays = table.Column<int>(type: "int", nullable: false),
                    CancelFeePercent = table.Column<decimal>(type: "decimal(9,4)", precision: 9, scale: 4, nullable: false),
                    BankAccount = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Note = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    PreparedBy = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RestaurantQuotes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RestaurantQuotes_AlacarteOrders_ApprovedOrderId",
                        column: x => x.ApprovedOrderId,
                        principalTable: "AlacarteOrders",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_RestaurantQuotes_CashTransactions_DepositCashTransactionId",
                        column: x => x.DepositCashTransactionId,
                        principalTable: "CashTransactions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_RestaurantQuotes_DiningRooms_DiningRoomId",
                        column: x => x.DiningRoomId,
                        principalTable: "DiningRooms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_RestaurantQuotes_DiningTables_DiningTableId",
                        column: x => x.DiningTableId,
                        principalTable: "DiningTables",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_RestaurantQuotes_RestaurantDeposits_DepositId",
                        column: x => x.DepositId,
                        principalTable: "RestaurantDeposits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_RestaurantQuotes_TableReservations_TableReservationId",
                        column: x => x.TableReservationId,
                        principalTable: "TableReservations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "RestaurantQuoteLines",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RestaurantQuoteId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MenuItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LineNo = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    LineType = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    Total = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RestaurantQuoteLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RestaurantQuoteLines_MenuItems_MenuItemId",
                        column: x => x.MenuItemId,
                        principalTable: "MenuItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_RestaurantQuoteLines_RestaurantQuotes_RestaurantQuoteId",
                        column: x => x.RestaurantQuoteId,
                        principalTable: "RestaurantQuotes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RestaurantQuoteLines_MenuItemId",
                table: "RestaurantQuoteLines",
                column: "MenuItemId");

            migrationBuilder.CreateIndex(
                name: "IX_RestaurantQuoteLines_RestaurantQuoteId_LineNo",
                table: "RestaurantQuoteLines",
                columns: new[] { "RestaurantQuoteId", "LineNo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RestaurantQuotes_ApprovedOrderId",
                table: "RestaurantQuotes",
                column: "ApprovedOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_RestaurantQuotes_DepositCashTransactionId",
                table: "RestaurantQuotes",
                column: "DepositCashTransactionId");

            migrationBuilder.CreateIndex(
                name: "IX_RestaurantQuotes_DepositId",
                table: "RestaurantQuotes",
                column: "DepositId");

            migrationBuilder.CreateIndex(
                name: "IX_RestaurantQuotes_DiningRoomId",
                table: "RestaurantQuotes",
                column: "DiningRoomId");

            migrationBuilder.CreateIndex(
                name: "IX_RestaurantQuotes_DiningTableId",
                table: "RestaurantQuotes",
                column: "DiningTableId");

            migrationBuilder.CreateIndex(
                name: "IX_RestaurantQuotes_QuoteNo",
                table: "RestaurantQuotes",
                column: "QuoteNo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RestaurantQuotes_Status_EventDate",
                table: "RestaurantQuotes",
                columns: new[] { "Status", "EventDate" });

            migrationBuilder.CreateIndex(
                name: "IX_RestaurantQuotes_TableReservationId",
                table: "RestaurantQuotes",
                column: "TableReservationId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RestaurantQuoteLines");

            migrationBuilder.DropTable(
                name: "RestaurantQuotes");
        }
    }
}
