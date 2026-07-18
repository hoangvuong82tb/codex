using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ASMILE.BO.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEnterpriseMultiCompanyBranchHardening : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAtUtc",
                table: "UserCompanyAccesses",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<bool>(
                name: "IsArchived",
                table: "UserCompanyAccesses",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "ModifiedAtUtc",
                table: "UserCompanyAccesses",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "UserCompanyAccesses",
                type: "rowversion",
                rowVersion: true,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAtUtc",
                table: "UserBranchAccesses",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<bool>(
                name: "IsArchived",
                table: "UserBranchAccesses",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "ModifiedAtUtc",
                table: "UserBranchAccesses",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "UserBranchAccesses",
                type: "rowversion",
                rowVersion: true,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AccountingStandardProfile",
                table: "Companies",
                type: "nvarchar(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAtUtc",
                table: "Companies",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "DefaultCurrencyCode",
                table: "Companies",
                type: "nvarchar(12)",
                maxLength: 12,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FiscalConfigJson",
                table: "Companies",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsArchived",
                table: "Companies",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Locale",
                table: "Companies",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ModifiedAtUtc",
                table: "Companies",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Companies",
                type: "rowversion",
                rowVersion: true,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TimeZoneId",
                table: "Companies",
                type: "nvarchar(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAtUtc",
                table: "Branches",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<bool>(
                name: "IsArchived",
                table: "Branches",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "ModifiedAtUtc",
                table: "Branches",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<Guid>(
                name: "ParentBranchId",
                table: "Branches",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Branches",
                type: "rowversion",
                rowVersion: true,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ActiveCompanySessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SessionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Source = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    MachineName = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    AppVersion = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    SessionMetadataJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    StartedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastActivityAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ExpiresAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsArchived = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActiveCompanySessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ActiveCompanySessions_Branches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "Branches",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ActiveCompanySessions_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "BranchSettings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SettingKey = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    ValueJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MetadataJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsArchived = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BranchSettings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BranchSettings_Branches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "Branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BranchSettings_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "CompanyFeatureFlags",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    FeatureKey = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false),
                    ValueJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MetadataJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsArchived = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompanyFeatureFlags", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CompanyFeatureFlags_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CompanyFiscalPeriods",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    FiscalYearCode = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    PeriodNo = table.Column<int>(type: "int", nullable: false),
                    PeriodCode = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    StartDateUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDateUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsOpen = table.Column<bool>(type: "bit", nullable: false),
                    IsClosed = table.Column<bool>(type: "bit", nullable: false),
                    IsArchived = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompanyFiscalPeriods", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CompanyFiscalPeriods_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CompanySettings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SettingKey = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    ValueJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MetadataJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsArchived = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompanySettings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CompanySettings_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.UpdateData(
                table: "Branches",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"),
                columns: new[] { "CreatedAtUtc", "IsArchived", "ModifiedAtUtc", "ParentBranchId" },
                values: new object[] { new DateTime(2026, 5, 22, 0, 0, 0, 0, DateTimeKind.Utc), false, new DateTime(2026, 5, 22, 0, 0, 0, 0, DateTimeKind.Utc), null });

            migrationBuilder.UpdateData(
                table: "Companies",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                columns: new[] { "AccountingStandardProfile", "CreatedAtUtc", "DefaultCurrencyCode", "FiscalConfigJson", "IsArchived", "Locale", "ModifiedAtUtc", "TimeZoneId" },
                values: new object[] { null, new DateTime(2026, 5, 22, 0, 0, 0, 0, DateTimeKind.Utc), null, null, false, null, new DateTime(2026, 5, 22, 0, 0, 0, 0, DateTimeKind.Utc), null });

            migrationBuilder.CreateIndex(
                name: "IX_UserCompanyAccesses_IsArchived",
                table: "UserCompanyAccesses",
                column: "IsArchived");

            migrationBuilder.CreateIndex(
                name: "IX_UserBranchAccesses_IsArchived",
                table: "UserBranchAccesses",
                column: "IsArchived");

            migrationBuilder.CreateIndex(
                name: "IX_Companies_IsArchived",
                table: "Companies",
                column: "IsArchived");

            migrationBuilder.CreateIndex(
                name: "IX_Branches_IsArchived",
                table: "Branches",
                column: "IsArchived");

            migrationBuilder.CreateIndex(
                name: "IX_Branches_ParentBranchId",
                table: "Branches",
                column: "ParentBranchId");

            migrationBuilder.CreateIndex(
                name: "IX_ActiveCompanySessions_BranchId",
                table: "ActiveCompanySessions",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_ActiveCompanySessions_CompanyId_BranchId_IsActive",
                table: "ActiveCompanySessions",
                columns: new[] { "CompanyId", "BranchId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_ActiveCompanySessions_ExpiresAtUtc",
                table: "ActiveCompanySessions",
                column: "ExpiresAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_ActiveCompanySessions_IsArchived",
                table: "ActiveCompanySessions",
                column: "IsArchived");

            migrationBuilder.CreateIndex(
                name: "IX_ActiveCompanySessions_Status",
                table: "ActiveCompanySessions",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ActiveCompanySessions_UserId_SessionId",
                table: "ActiveCompanySessions",
                columns: new[] { "UserId", "SessionId" },
                unique: true,
                filter: "[SessionId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_BranchSettings_BranchId",
                table: "BranchSettings",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_BranchSettings_CompanyId_BranchId_SettingKey",
                table: "BranchSettings",
                columns: new[] { "CompanyId", "BranchId", "SettingKey" },
                unique: true,
                filter: "[CompanyId] IS NOT NULL AND [BranchId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_BranchSettings_IsArchived",
                table: "BranchSettings",
                column: "IsArchived");

            migrationBuilder.CreateIndex(
                name: "IX_CompanyFeatureFlags_CompanyId_FeatureKey",
                table: "CompanyFeatureFlags",
                columns: new[] { "CompanyId", "FeatureKey" },
                unique: true,
                filter: "[CompanyId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_CompanyFeatureFlags_IsArchived",
                table: "CompanyFeatureFlags",
                column: "IsArchived");

            migrationBuilder.CreateIndex(
                name: "IX_CompanyFiscalPeriods_CompanyId_FiscalYearCode_PeriodNo",
                table: "CompanyFiscalPeriods",
                columns: new[] { "CompanyId", "FiscalYearCode", "PeriodNo" },
                unique: true,
                filter: "[CompanyId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_CompanyFiscalPeriods_CompanyId_PeriodCode",
                table: "CompanyFiscalPeriods",
                columns: new[] { "CompanyId", "PeriodCode" });

            migrationBuilder.CreateIndex(
                name: "IX_CompanyFiscalPeriods_IsArchived",
                table: "CompanyFiscalPeriods",
                column: "IsArchived");

            migrationBuilder.CreateIndex(
                name: "IX_CompanySettings_CompanyId_SettingKey",
                table: "CompanySettings",
                columns: new[] { "CompanyId", "SettingKey" },
                unique: true,
                filter: "[CompanyId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_CompanySettings_IsArchived",
                table: "CompanySettings",
                column: "IsArchived");

            migrationBuilder.AddForeignKey(
                name: "FK_Branches_Branches_ParentBranchId",
                table: "Branches",
                column: "ParentBranchId",
                principalTable: "Branches",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Branches_Branches_ParentBranchId",
                table: "Branches");

            migrationBuilder.DropTable(
                name: "ActiveCompanySessions");

            migrationBuilder.DropTable(
                name: "BranchSettings");

            migrationBuilder.DropTable(
                name: "CompanyFeatureFlags");

            migrationBuilder.DropTable(
                name: "CompanyFiscalPeriods");

            migrationBuilder.DropTable(
                name: "CompanySettings");

            migrationBuilder.DropIndex(
                name: "IX_UserCompanyAccesses_IsArchived",
                table: "UserCompanyAccesses");

            migrationBuilder.DropIndex(
                name: "IX_UserBranchAccesses_IsArchived",
                table: "UserBranchAccesses");

            migrationBuilder.DropIndex(
                name: "IX_Companies_IsArchived",
                table: "Companies");

            migrationBuilder.DropIndex(
                name: "IX_Branches_IsArchived",
                table: "Branches");

            migrationBuilder.DropIndex(
                name: "IX_Branches_ParentBranchId",
                table: "Branches");

            migrationBuilder.DropColumn(
                name: "CreatedAtUtc",
                table: "UserCompanyAccesses");

            migrationBuilder.DropColumn(
                name: "IsArchived",
                table: "UserCompanyAccesses");

            migrationBuilder.DropColumn(
                name: "ModifiedAtUtc",
                table: "UserCompanyAccesses");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "UserCompanyAccesses");

            migrationBuilder.DropColumn(
                name: "CreatedAtUtc",
                table: "UserBranchAccesses");

            migrationBuilder.DropColumn(
                name: "IsArchived",
                table: "UserBranchAccesses");

            migrationBuilder.DropColumn(
                name: "ModifiedAtUtc",
                table: "UserBranchAccesses");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "UserBranchAccesses");

            migrationBuilder.DropColumn(
                name: "AccountingStandardProfile",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "CreatedAtUtc",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "DefaultCurrencyCode",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "FiscalConfigJson",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "IsArchived",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "Locale",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "ModifiedAtUtc",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "TimeZoneId",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "CreatedAtUtc",
                table: "Branches");

            migrationBuilder.DropColumn(
                name: "IsArchived",
                table: "Branches");

            migrationBuilder.DropColumn(
                name: "ModifiedAtUtc",
                table: "Branches");

            migrationBuilder.DropColumn(
                name: "ParentBranchId",
                table: "Branches");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Branches");
        }
    }
}
