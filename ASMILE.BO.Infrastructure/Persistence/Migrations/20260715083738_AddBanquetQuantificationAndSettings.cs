using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ASMILE.BO.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddBanquetQuantificationAndSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BanquetModuleSettings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    VenueName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    VenueCode = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    Address = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Phone = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Website = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    TaxCode = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    GuestsPerTable = table.Column<int>(type: "int", nullable: false),
                    DefaultDepositRate = table.Column<decimal>(type: "decimal(9,4)", precision: 9, scale: 4, nullable: false),
                    DefaultVatRate = table.Column<decimal>(type: "decimal(9,4)", precision: 9, scale: 4, nullable: false),
                    HoldDays = table.Column<int>(type: "int", nullable: false),
                    ReminderDays = table.Column<int>(type: "int", nullable: false),
                    LunchStartTime = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    DinnerStartTime = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    AutoGenerateEventNo = table.Column<bool>(type: "bit", nullable: false),
                    WarnRoomConflict = table.Column<bool>(type: "bit", nullable: false),
                    RequireContractBeforeConfirm = table.Column<bool>(type: "bit", nullable: false),
                    EnableDepositReminder = table.Column<bool>(type: "bit", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BanquetModuleSettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BanquetQuantificationBatches",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BanquetEventId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VersionNo = table.Column<int>(type: "int", nullable: false),
                    Fingerprint = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    SentAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SentBy = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    ReceivedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReceivedBy = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedBy = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    Note = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BanquetQuantificationBatches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BanquetQuantificationBatches_BanquetEvents_BanquetEventId",
                        column: x => x.BanquetEventId,
                        principalTable: "BanquetEvents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BanquetQuantificationLines",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BanquetQuantificationBatchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    LineType = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Code = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    UnitName = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    RequiredQuantity = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    EstimatedCost = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    SourceText = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Note = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BanquetQuantificationLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BanquetQuantificationLines_BanquetQuantificationBatches_BanquetQuantificationBatchId",
                        column: x => x.BanquetQuantificationBatchId,
                        principalTable: "BanquetQuantificationBatches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BanquetModuleSettings_CompanyId_BranchId",
                table: "BanquetModuleSettings",
                columns: new[] { "CompanyId", "BranchId" },
                unique: true,
                filter: "[CompanyId] IS NOT NULL AND [BranchId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_BanquetQuantificationBatches_BanquetEventId_VersionNo",
                table: "BanquetQuantificationBatches",
                columns: new[] { "BanquetEventId", "VersionNo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BanquetQuantificationBatches_Status_SentAt",
                table: "BanquetQuantificationBatches",
                columns: new[] { "Status", "SentAt" });

            migrationBuilder.CreateIndex(
                name: "IX_BanquetQuantificationLines_BanquetQuantificationBatchId_SortOrder",
                table: "BanquetQuantificationLines",
                columns: new[] { "BanquetQuantificationBatchId", "SortOrder" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BanquetModuleSettings");

            migrationBuilder.DropTable(
                name: "BanquetQuantificationLines");

            migrationBuilder.DropTable(
                name: "BanquetQuantificationBatches");
        }
    }
}
