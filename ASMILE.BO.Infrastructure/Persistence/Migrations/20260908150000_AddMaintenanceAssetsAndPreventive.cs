using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ASMILE.BO.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMaintenanceAssetsAndPreventive : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "KeHoachBaoTriDinhKy",
                schema: "fo",
                columns: table => new
                {
                    PlanId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PropertyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PlanName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    AssetId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IntervalDays = table.Column<int>(type: "int", nullable: false),
                    EstimatedMinutes = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fo_KeHoachBaoTriDinhKy", x => x.PlanId);
                });

            migrationBuilder.CreateTable(
                name: "TaiSanBaoTri",
                schema: "fo",
                columns: table => new
                {
                    AssetId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PropertyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AssetCode = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Location = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    RoomId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SerialNumber = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    Model = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    Vendor = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: true),
                    WarrantyExpiry = table.Column<DateTime>(type: "datetime2", nullable: true),
                    InstalledAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TotalRepairs = table.Column<int>(type: "int", nullable: false),
                    RuntimeHours = table.Column<int>(type: "int", nullable: false),
                    FailureCount = table.Column<int>(type: "int", nullable: false),
                    ConditionScore = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    LastMaintenanceAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NextMaintenanceDueUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fo_TaiSanBaoTri", x => x.AssetId);
                });

            migrationBuilder.CreateTable(
                name: "ChiTietKeHoachBaoTri",
                schema: "fo",
                columns: table => new
                {
                    ChecklistItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlanId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    IsMandatory = table.Column<bool>(type: "bit", nullable: false),
                    Reference = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fo_ChiTietKeHoachBaoTri", x => x.ChecklistItemId);
                    table.ForeignKey(
                        name: "FK_ChiTietKeHoachBaoTri_KeHoachBaoTriDinhKy_PlanId",
                        column: x => x.PlanId,
                        principalSchema: "fo",
                        principalTable: "KeHoachBaoTriDinhKy",
                        principalColumn: "PlanId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LichBaoTriDinhKy",
                schema: "fo",
                columns: table => new
                {
                    ScheduleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PropertyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlanId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AssetId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RoomId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ScheduledAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    WorkOrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fo_LichBaoTriDinhKy", x => x.ScheduleId);
                    table.ForeignKey(
                        name: "FK_LichBaoTriDinhKy_KeHoachBaoTriDinhKy_PlanId",
                        column: x => x.PlanId,
                        principalSchema: "fo",
                        principalTable: "KeHoachBaoTriDinhKy",
                        principalColumn: "PlanId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LichSuHongHocBaoTri",
                schema: "fo",
                columns: table => new
                {
                    FailureId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AssetId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    ReportedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReportedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fo_LichSuHongHocBaoTri", x => x.FailureId);
                    table.ForeignKey(
                        name: "FK_LichSuHongHocBaoTri_TaiSanBaoTri_AssetId",
                        column: x => x.AssetId,
                        principalSchema: "fo",
                        principalTable: "TaiSanBaoTri",
                        principalColumn: "AssetId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "UQ_fo_ChiTietKeHoachBaoTri_KeHoach_Ma",
                schema: "fo",
                table: "ChiTietKeHoachBaoTri",
                columns: new[] { "PlanId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_fo_KeHoachBaoTriDinhKy_CoSo_Loai_HoatDong",
                schema: "fo",
                table: "KeHoachBaoTriDinhKy",
                columns: new[] { "PropertyId", "Category", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_fo_LichBaoTriDinhKy_CoSo_Lich",
                schema: "fo",
                table: "LichBaoTriDinhKy",
                columns: new[] { "PropertyId", "ScheduledAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_fo_LichBaoTriDinhKy_CoSo_TaiSan_HoanTat",
                schema: "fo",
                table: "LichBaoTriDinhKy",
                columns: new[] { "PropertyId", "AssetId", "CompletedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_LichBaoTriDinhKy_PlanId",
                schema: "fo",
                table: "LichBaoTriDinhKy",
                column: "PlanId");

            migrationBuilder.CreateIndex(
                name: "IX_fo_LichSuHongHocBaoTri_TaiSan_ThoiGian",
                schema: "fo",
                table: "LichSuHongHocBaoTri",
                columns: new[] { "AssetId", "ReportedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_fo_TaiSanBaoTri_CoSo_Phong_HoatDong",
                schema: "fo",
                table: "TaiSanBaoTri",
                columns: new[] { "PropertyId", "RoomId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "UQ_fo_TaiSanBaoTri_CoSo_MaTaiSan",
                schema: "fo",
                table: "TaiSanBaoTri",
                columns: new[] { "PropertyId", "AssetCode" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChiTietKeHoachBaoTri",
                schema: "fo");

            migrationBuilder.DropTable(
                name: "LichBaoTriDinhKy",
                schema: "fo");

            migrationBuilder.DropTable(
                name: "LichSuHongHocBaoTri",
                schema: "fo");

            migrationBuilder.DropTable(
                name: "KeHoachBaoTriDinhKy",
                schema: "fo");

            migrationBuilder.DropTable(
                name: "TaiSanBaoTri",
                schema: "fo");
        }
    }
}
