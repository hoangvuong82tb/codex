using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ASMILE.BO.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddBreakfastCafeSalesModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                IF COL_LENGTH('StockIssueDetails', 'VatAmount') IS NOT NULL
                    ALTER TABLE [StockIssueDetails] DROP COLUMN [VatAmount];
                """);

            migrationBuilder.Sql(
                """
                IF COL_LENGTH('StockIssueDetails', 'VatRate') IS NOT NULL
                    ALTER TABLE [StockIssueDetails] DROP COLUMN [VatRate];
                """);

            migrationBuilder.Sql(
                """
                IF COL_LENGTH('TrainingDataRunSteps', 'AfterCount') IS NULL
                    ALTER TABLE [TrainingDataRunSteps] ADD [AfterCount] int NOT NULL CONSTRAINT [DF_TrainingDataRunSteps_AfterCount] DEFAULT 0;
                """);

            migrationBuilder.Sql(
                """
                IF COL_LENGTH('TrainingDataRunSteps', 'BeforeCount') IS NULL
                    ALTER TABLE [TrainingDataRunSteps] ADD [BeforeCount] int NOT NULL CONSTRAINT [DF_TrainingDataRunSteps_BeforeCount] DEFAULT 0;
                """);

            migrationBuilder.Sql(
                """
                IF COL_LENGTH('TrainingDataRunSteps', 'EntityName') IS NULL
                    ALTER TABLE [TrainingDataRunSteps] ADD [EntityName] nvarchar(160) NULL;
                """);

            migrationBuilder.Sql(
                """
                IF COL_LENGTH('TrainingDataRunSteps', 'Status') IS NULL
                    ALTER TABLE [TrainingDataRunSteps] ADD [Status] nvarchar(40) NOT NULL CONSTRAINT [DF_TrainingDataRunSteps_Status] DEFAULT N'';
                """);

            migrationBuilder.Sql(
                """
                IF COL_LENGTH('StockReceiptDetails', 'VatAmount') IS NULL
                    ALTER TABLE [StockReceiptDetails] ADD [VatAmount] decimal(18,2) NOT NULL CONSTRAINT [DF_StockReceiptDetails_VatAmount] DEFAULT 0;
                """);

            migrationBuilder.Sql(
                """
                IF COL_LENGTH('StockReceiptDetails', 'VatRate') IS NULL
                    ALTER TABLE [StockReceiptDetails] ADD [VatRate] decimal(5,2) NOT NULL CONSTRAINT [DF_StockReceiptDetails_VatRate] DEFAULT 0;
                """);

            migrationBuilder.CreateTable(
                name: "BreakfastCafeOrders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    OrderNumber = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    OrderDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    PaymentStatus = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    CustomerReference = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    CustomerId = table.Column<int>(type: "int", nullable: true),
                    RoomReference = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    SubtotalAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    DiscountAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    VatAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    PaidAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    RemainingAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    JournalHeaderId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Note = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BreakfastCafeOrders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BreakfastCafeOrders_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "MenuCategories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MenuCategories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BreakfastCafePayments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BreakfastCafeOrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PaymentDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PaymentMethod = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ReferenceNo = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    CashFundId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BankAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RoomReference = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    CustomerId = table.Column<int>(type: "int", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BreakfastCafePayments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BreakfastCafePayments_BankAccounts_BankAccountId",
                        column: x => x.BankAccountId,
                        principalTable: "BankAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_BreakfastCafePayments_BreakfastCafeOrders_BreakfastCafeOrderId",
                        column: x => x.BreakfastCafeOrderId,
                        principalTable: "BreakfastCafeOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BreakfastCafePayments_CashFunds_CashFundId",
                        column: x => x.CashFundId,
                        principalTable: "CashFunds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_BreakfastCafePayments_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "MenuItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    MenuCategoryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ItemId = table.Column<int>(type: "int", nullable: true),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    UnitName = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    VatRate = table.Column<decimal>(type: "decimal(9,4)", precision: 9, scale: 4, nullable: false),
                    IsCombo = table.Column<bool>(type: "bit", nullable: false),
                    IsHotelService = table.Column<bool>(type: "bit", nullable: false),
                    ImagePath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ImageUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MenuItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MenuItems_Items_ItemId",
                        column: x => x.ItemId,
                        principalTable: "Items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_MenuItems_MenuCategories_MenuCategoryId",
                        column: x => x.MenuCategoryId,
                        principalTable: "MenuCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BreakfastCafeOrderLines",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BreakfastCafeOrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MenuItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LineNo = table.Column<int>(type: "int", nullable: false),
                    MenuItemCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    MenuItemName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    UnitName = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    LineDiscountAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    VatRate = table.Column<decimal>(type: "decimal(9,4)", precision: 9, scale: 4, nullable: false),
                    VatAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    LineTotalAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BreakfastCafeOrderLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BreakfastCafeOrderLines_BreakfastCafeOrders_BreakfastCafeOrderId",
                        column: x => x.BreakfastCafeOrderId,
                        principalTable: "BreakfastCafeOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BreakfastCafeOrderLines_MenuItems_MenuItemId",
                        column: x => x.MenuItemId,
                        principalTable: "MenuItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MenuItemRecipes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    MenuItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MenuItemRecipes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MenuItemRecipes_MenuItems_MenuItemId",
                        column: x => x.MenuItemId,
                        principalTable: "MenuItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MenuItemRecipeLines",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    MenuItemRecipeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LineNo = table.Column<int>(type: "int", nullable: false),
                    ItemId = table.Column<int>(type: "int", nullable: true),
                    ItemCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ItemName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    UnitName = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    LossRate = table.Column<decimal>(type: "decimal(9,4)", precision: 9, scale: 4, nullable: false),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MenuItemRecipeLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MenuItemRecipeLines_Items_ItemId",
                        column: x => x.ItemId,
                        principalTable: "Items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_MenuItemRecipeLines_MenuItemRecipes_MenuItemRecipeId",
                        column: x => x.MenuItemRecipeId,
                        principalTable: "MenuItemRecipes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BreakfastCafeOrderLines_BreakfastCafeOrderId_LineNo",
                table: "BreakfastCafeOrderLines",
                columns: new[] { "BreakfastCafeOrderId", "LineNo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BreakfastCafeOrderLines_MenuItemCode",
                table: "BreakfastCafeOrderLines",
                column: "MenuItemCode");

            migrationBuilder.CreateIndex(
                name: "IX_BreakfastCafeOrderLines_MenuItemId",
                table: "BreakfastCafeOrderLines",
                column: "MenuItemId");

            migrationBuilder.CreateIndex(
                name: "IX_BreakfastCafeOrders_CustomerId",
                table: "BreakfastCafeOrders",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_BreakfastCafeOrders_OrderDate",
                table: "BreakfastCafeOrders",
                column: "OrderDate");

            migrationBuilder.CreateIndex(
                name: "IX_BreakfastCafeOrders_OrderNumber",
                table: "BreakfastCafeOrders",
                column: "OrderNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BreakfastCafeOrders_PaymentStatus",
                table: "BreakfastCafeOrders",
                column: "PaymentStatus");

            migrationBuilder.CreateIndex(
                name: "IX_BreakfastCafeOrders_Status",
                table: "BreakfastCafeOrders",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_BreakfastCafePayments_BankAccountId",
                table: "BreakfastCafePayments",
                column: "BankAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_BreakfastCafePayments_BreakfastCafeOrderId_PaymentDate",
                table: "BreakfastCafePayments",
                columns: new[] { "BreakfastCafeOrderId", "PaymentDate" });

            migrationBuilder.CreateIndex(
                name: "IX_BreakfastCafePayments_CashFundId",
                table: "BreakfastCafePayments",
                column: "CashFundId");

            migrationBuilder.CreateIndex(
                name: "IX_BreakfastCafePayments_CustomerId",
                table: "BreakfastCafePayments",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_BreakfastCafePayments_PaymentMethod",
                table: "BreakfastCafePayments",
                column: "PaymentMethod");

            migrationBuilder.CreateIndex(
                name: "IX_MenuCategories_Code",
                table: "MenuCategories",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MenuItemRecipeLines_ItemId",
                table: "MenuItemRecipeLines",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_MenuItemRecipeLines_MenuItemRecipeId_LineNo",
                table: "MenuItemRecipeLines",
                columns: new[] { "MenuItemRecipeId", "LineNo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MenuItemRecipes_MenuItemId_IsDefault",
                table: "MenuItemRecipes",
                columns: new[] { "MenuItemId", "IsDefault" });

            migrationBuilder.CreateIndex(
                name: "IX_MenuItems_Code",
                table: "MenuItems",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MenuItems_ItemId",
                table: "MenuItems",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_MenuItems_MenuCategoryId_SortOrder",
                table: "MenuItems",
                columns: new[] { "MenuCategoryId", "SortOrder" });

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BreakfastCafeOrderLines");

            migrationBuilder.DropTable(
                name: "BreakfastCafePayments");

            migrationBuilder.DropTable(
                name: "MenuItemRecipeLines");

            migrationBuilder.DropTable(
                name: "BreakfastCafeOrders");

            migrationBuilder.DropTable(
                name: "MenuItemRecipes");

            migrationBuilder.DropTable(
                name: "MenuItems");

            migrationBuilder.DropTable(
                name: "MenuCategories");

            migrationBuilder.DropColumn(
                name: "AfterCount",
                table: "TrainingDataRunSteps");

            migrationBuilder.DropColumn(
                name: "BeforeCount",
                table: "TrainingDataRunSteps");

            migrationBuilder.DropColumn(
                name: "EntityName",
                table: "TrainingDataRunSteps");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "TrainingDataRunSteps");

            migrationBuilder.DropColumn(
                name: "VatAmount",
                table: "StockReceiptDetails");

            migrationBuilder.DropColumn(
                name: "VatRate",
                table: "StockReceiptDetails");

            migrationBuilder.Sql(
                """
                IF COL_LENGTH('StockIssueDetails', 'VatAmount') IS NULL
                    ALTER TABLE [StockIssueDetails] ADD [VatAmount] decimal(18,2) NOT NULL CONSTRAINT [DF_StockIssueDetails_VatAmount] DEFAULT 0;
                """);

            migrationBuilder.Sql(
                """
                IF COL_LENGTH('StockIssueDetails', 'VatRate') IS NULL
                    ALTER TABLE [StockIssueDetails] ADD [VatRate] decimal(5,2) NOT NULL CONSTRAINT [DF_StockIssueDetails_VatRate] DEFAULT 0;
                """);
        }
    }
}
