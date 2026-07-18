using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ASMILE.BO.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddFiscalPeriodAndBranchEngine : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FiscalYears",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Year = table.Column<int>(type: "int", nullable: false),
                    FiscalYearCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    FromDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ToDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    ClosedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ClosedBy = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table => table.PrimaryKey("PK_FiscalYears", x => x.Id));

            migrationBuilder.CreateTable(
                name: "PeriodLocks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PeriodCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    IsLocked = table.Column<bool>(type: "bit", nullable: false),
                    LockedBy = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    LockedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LockReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    UnlockedBy = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    UnlockedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UnlockReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table => table.PrimaryKey("PK_PeriodLocks", x => x.Id));

            migrationBuilder.CreateTable(
                name: "CompanyBranchPermissions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PermissionCode = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    IsGranted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table => table.PrimaryKey("PK_CompanyBranchPermissions", x => x.Id));

            migrationBuilder.CreateIndex(name: "IX_FiscalYears_FiscalYearCode", table: "FiscalYears", column: "FiscalYearCode");
            migrationBuilder.CreateIndex(name: "IX_FiscalYears_CompanyId_Year", table: "FiscalYears", columns: new[] { "CompanyId", "Year" }, unique: true, filter: "[CompanyId] IS NOT NULL");
            migrationBuilder.CreateIndex(name: "IX_PeriodLocks_BranchId", table: "PeriodLocks", column: "BranchId");
            migrationBuilder.CreateIndex(name: "IX_PeriodLocks_CompanyId_BranchId_PeriodCode", table: "PeriodLocks", columns: new[] { "CompanyId", "BranchId", "PeriodCode" }, unique: true, filter: "[CompanyId] IS NOT NULL");
            migrationBuilder.CreateIndex(name: "IX_PeriodLocks_CompanyId_PeriodCode_IsLocked", table: "PeriodLocks", columns: new[] { "CompanyId", "PeriodCode", "IsLocked" });
            migrationBuilder.CreateIndex(name: "IX_CompanyBranchPermissions_CompanyId_BranchId", table: "CompanyBranchPermissions", columns: new[] { "CompanyId", "BranchId" });
            migrationBuilder.CreateIndex(name: "IX_CompanyBranchPermissions_UserId_CompanyId_BranchId_PermissionCode", table: "CompanyBranchPermissions", columns: new[] { "UserId", "CompanyId", "BranchId", "PermissionCode" }, unique: true, filter: "[CompanyId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "CompanyBranchPermissions");
            migrationBuilder.DropTable(name: "PeriodLocks");
            migrationBuilder.DropTable(name: "FiscalYears");
        }
    }
}
