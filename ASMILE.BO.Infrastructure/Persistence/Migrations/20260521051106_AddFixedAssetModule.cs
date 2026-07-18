using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ASMILE.BO.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddFixedAssetModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FixedAssetCategories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CategoryCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CategoryName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    AssetAccountCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    DepreciationAccountCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ExpenseAccountCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    UsefulLifeMonthsDefault = table.Column<int>(type: "int", nullable: false),
                    DepreciationMethodDefault = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, defaultValue: "StraightLine"),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FixedAssetCategories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FixedAssets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AssetCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    AssetName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    CategoryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SerialNumber = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    PurchaseDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    StartDepreciationDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    OriginalCost = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    SalvageValue = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    UsefulLifeMonths = table.Column<int>(type: "int", nullable: false),
                    DepreciationMethod = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, defaultValue: "StraightLine"),
                    AccumulatedDepreciation = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    RemainingValue = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    DepartmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    EmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    WarehouseId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SupplierId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    JournalHeaderId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FixedAssets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FixedAssets_FixedAssetCategories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "FixedAssetCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FixedAssets_JournalHeaders_JournalHeaderId",
                        column: x => x.JournalHeaderId,
                        principalTable: "JournalHeaders",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "FixedAssetAdjustments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FixedAssetId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AdjustmentDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AdjustmentType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    JournalHeaderId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FixedAssetAdjustments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FixedAssetAdjustments_FixedAssets_FixedAssetId",
                        column: x => x.FixedAssetId,
                        principalTable: "FixedAssets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FixedAssetAdjustments_JournalHeaders_JournalHeaderId",
                        column: x => x.JournalHeaderId,
                        principalTable: "JournalHeaders",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "FixedAssetDepreciations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FixedAssetId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DepreciationPeriod = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    DepreciationDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DepreciationAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    AccumulatedAfter = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    RemainingValueAfter = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    JournalHeaderId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PostedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FixedAssetDepreciations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FixedAssetDepreciations_FixedAssets_FixedAssetId",
                        column: x => x.FixedAssetId,
                        principalTable: "FixedAssets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FixedAssetDepreciations_JournalHeaders_JournalHeaderId",
                        column: x => x.JournalHeaderId,
                        principalTable: "JournalHeaders",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "FixedAssetDisposals",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FixedAssetId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DisposalDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DisposalType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    DisposalAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    GainLossAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    JournalHeaderId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FixedAssetDisposals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FixedAssetDisposals_FixedAssets_FixedAssetId",
                        column: x => x.FixedAssetId,
                        principalTable: "FixedAssets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FixedAssetDisposals_JournalHeaders_JournalHeaderId",
                        column: x => x.JournalHeaderId,
                        principalTable: "JournalHeaders",
                        principalColumn: "Id");
                });

            migrationBuilder.InsertData(
                table: "FixedAssetCategories",
                columns: new[] { "Id", "AssetAccountCode", "CategoryCode", "CategoryName", "CreatedAt", "DepreciationAccountCode", "DepreciationMethodDefault", "ExpenseAccountCode", "IsActive", "UpdatedAt", "UsefulLifeMonthsDefault" },
                values: new object[] { new Guid("9a71a150-2a6a-4e72-9b25-000000000001"), null, "TSCD_DEFAULT", "Tài sản cố định mặc định", new DateTime(2026, 5, 21, 0, 0, 0, 0, DateTimeKind.Utc), null, "StraightLine", null, true, new DateTime(2026, 5, 21, 0, 0, 0, 0, DateTimeKind.Utc), 60 });

            migrationBuilder.CreateIndex(
                name: "IX_FixedAssetAdjustments_AdjustmentDate",
                table: "FixedAssetAdjustments",
                column: "AdjustmentDate");

            migrationBuilder.CreateIndex(
                name: "IX_FixedAssetAdjustments_AdjustmentType",
                table: "FixedAssetAdjustments",
                column: "AdjustmentType");

            migrationBuilder.CreateIndex(
                name: "IX_FixedAssetAdjustments_FixedAssetId",
                table: "FixedAssetAdjustments",
                column: "FixedAssetId");

            migrationBuilder.CreateIndex(
                name: "IX_FixedAssetAdjustments_JournalHeaderId",
                table: "FixedAssetAdjustments",
                column: "JournalHeaderId");

            migrationBuilder.CreateIndex(
                name: "IX_FixedAssetCategories_CategoryCode",
                table: "FixedAssetCategories",
                column: "CategoryCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FixedAssetCategories_IsActive",
                table: "FixedAssetCategories",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_FixedAssetDepreciations_DepreciationPeriod",
                table: "FixedAssetDepreciations",
                column: "DepreciationPeriod");

            migrationBuilder.CreateIndex(
                name: "IX_FixedAssetDepreciations_FixedAssetId_DepreciationPeriod",
                table: "FixedAssetDepreciations",
                columns: new[] { "FixedAssetId", "DepreciationPeriod" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FixedAssetDepreciations_JournalHeaderId",
                table: "FixedAssetDepreciations",
                column: "JournalHeaderId");

            migrationBuilder.CreateIndex(
                name: "IX_FixedAssetDepreciations_Status",
                table: "FixedAssetDepreciations",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_FixedAssetDisposals_DisposalDate",
                table: "FixedAssetDisposals",
                column: "DisposalDate");

            migrationBuilder.CreateIndex(
                name: "IX_FixedAssetDisposals_DisposalType",
                table: "FixedAssetDisposals",
                column: "DisposalType");

            migrationBuilder.CreateIndex(
                name: "IX_FixedAssetDisposals_FixedAssetId",
                table: "FixedAssetDisposals",
                column: "FixedAssetId");

            migrationBuilder.CreateIndex(
                name: "IX_FixedAssetDisposals_JournalHeaderId",
                table: "FixedAssetDisposals",
                column: "JournalHeaderId");

            migrationBuilder.CreateIndex(
                name: "IX_FixedAssets_AssetCode",
                table: "FixedAssets",
                column: "AssetCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FixedAssets_CategoryId",
                table: "FixedAssets",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_FixedAssets_JournalHeaderId",
                table: "FixedAssets",
                column: "JournalHeaderId");

            migrationBuilder.CreateIndex(
                name: "IX_FixedAssets_PurchaseDate",
                table: "FixedAssets",
                column: "PurchaseDate");

            migrationBuilder.CreateIndex(
                name: "IX_FixedAssets_Status",
                table: "FixedAssets",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FixedAssetAdjustments");

            migrationBuilder.DropTable(
                name: "FixedAssetDepreciations");

            migrationBuilder.DropTable(
                name: "FixedAssetDisposals");

            migrationBuilder.DropTable(
                name: "FixedAssets");

            migrationBuilder.DropTable(
                name: "FixedAssetCategories");
        }
    }
}
