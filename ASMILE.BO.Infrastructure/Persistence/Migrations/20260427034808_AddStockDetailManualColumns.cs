using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ASMILE.BO.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddStockDetailManualColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsApproved",
                table: "StockReceipts",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "LineNo",
                table: "StockReceiptDetails",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Unit",
                table: "StockReceiptDetails",
                type: "nvarchar(50)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "WarehouseId",
                table: "StockReceiptDetails",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsApproved",
                table: "StockIssues",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "LineNo",
                table: "StockIssueDetails",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Unit",
                table: "StockIssueDetails",
                type: "nvarchar(50)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "WarehouseId",
                table: "StockIssueDetails",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "BusinessDocuments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Module = table.Column<int>(type: "int", nullable: false),
                    EntityType = table.Column<int>(type: "int", nullable: false),
                    EntityId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DocumentType = table.Column<int>(type: "int", nullable: false),
                    DocumentDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Currency = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    VirtualPath = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    PhysicalFilePath = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(260)", maxLength: 260, nullable: false),
                    FileExtension = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    FileSize = table.Column<long>(type: "bigint", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BusinessDocuments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DocumentLinks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DocumentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RelatedEntityType = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    RelatedEntityId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RelationType = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentLinks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "VirtualDrives",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Icon = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsSystem = table.Column<bool>(type: "bit", nullable: false),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VirtualDrives", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "VirtualFolders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DriveCode = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    ParentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    VirtualPath = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    FolderType = table.Column<int>(type: "int", nullable: false),
                    EntityType = table.Column<int>(type: "int", nullable: true),
                    EntityId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsSystem = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VirtualFolders", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "VirtualDrives",
                columns: new[] { "Id", "Code", "DisplayName", "Icon", "IsEnabled", "IsSystem", "Name", "SortOrder" },
                values: new object[,]
                {
                    { new Guid("1b6a72e1-8a8e-4e63-8a4d-8f2e5c1f0001"), "C", "C: SYSTEM", "drive-system.ico", true, true, "SYSTEM", 10 },
                    { new Guid("1b6a72e1-8a8e-4e63-8a4d-8f2e5c1f0002"), "D", "D: ACCOUNTING", "drive-accounting.ico", true, true, "ACCOUNTING", 20 },
                    { new Guid("1b6a72e1-8a8e-4e63-8a4d-8f2e5c1f0003"), "E", "E: SALES", "drive-sales.ico", true, true, "SALES", 30 },
                    { new Guid("1b6a72e1-8a8e-4e63-8a4d-8f2e5c1f0004"), "F", "F: HR", "drive-hr.ico", true, true, "HR", 40 },
                    { new Guid("1b6a72e1-8a8e-4e63-8a4d-8f2e5c1f0005"), "G", "G: LOGS", "drive-logs.ico", true, true, "LOGS", 50 },
                    { new Guid("1b6a72e1-8a8e-4e63-8a4d-8f2e5c1f0006"), "NET", "Network", "network.ico", true, true, "NETWORK", 60 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Suppliers_SupplierName",
                table: "Suppliers",
                column: "SupplierName");

            migrationBuilder.CreateIndex(
                name: "IX_Customers_CustomerName",
                table: "Customers",
                column: "CustomerName");

            migrationBuilder.CreateIndex(
                name: "IX_BusinessDocuments_DocumentDate",
                table: "BusinessDocuments",
                column: "DocumentDate");

            migrationBuilder.CreateIndex(
                name: "IX_BusinessDocuments_EntityType_EntityId",
                table: "BusinessDocuments",
                columns: new[] { "EntityType", "EntityId" });

            migrationBuilder.CreateIndex(
                name: "IX_BusinessDocuments_VirtualPath",
                table: "BusinessDocuments",
                column: "VirtualPath");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentLinks_DocumentId",
                table: "DocumentLinks",
                column: "DocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentLinks_RelatedEntityType_RelatedEntityId",
                table: "DocumentLinks",
                columns: new[] { "RelatedEntityType", "RelatedEntityId" });

            migrationBuilder.CreateIndex(
                name: "IX_VirtualDrives_Code",
                table: "VirtualDrives",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VirtualFolders_VirtualPath",
                table: "VirtualFolders",
                column: "VirtualPath",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BusinessDocuments");

            migrationBuilder.DropTable(
                name: "DocumentLinks");

            migrationBuilder.DropTable(
                name: "VirtualDrives");

            migrationBuilder.DropTable(
                name: "VirtualFolders");

            migrationBuilder.DropIndex(
                name: "IX_Suppliers_SupplierName",
                table: "Suppliers");

            migrationBuilder.DropIndex(
                name: "IX_Customers_CustomerName",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "IsApproved",
                table: "StockReceipts");

            migrationBuilder.DropColumn(
                name: "LineNo",
                table: "StockReceiptDetails");

            migrationBuilder.DropColumn(
                name: "Unit",
                table: "StockReceiptDetails");

            migrationBuilder.DropColumn(
                name: "WarehouseId",
                table: "StockReceiptDetails");

            migrationBuilder.DropColumn(
                name: "IsApproved",
                table: "StockIssues");

            migrationBuilder.DropColumn(
                name: "LineNo",
                table: "StockIssueDetails");

            migrationBuilder.DropColumn(
                name: "Unit",
                table: "StockIssueDetails");

            migrationBuilder.DropColumn(
                name: "WarehouseId",
                table: "StockIssueDetails");
        }
    }
}
