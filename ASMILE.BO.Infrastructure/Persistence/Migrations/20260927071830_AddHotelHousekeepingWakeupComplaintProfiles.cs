using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ASMILE.BO.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddHotelHousekeepingWakeupComplaintProfiles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DanhMucKhieuNaiFO",
                schema: "fo",
                columns: table => new
                {
                    HotelComplaintProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PropertyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Severity = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Department = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SlaMinutes = table.Column<int>(type: "int", nullable: false),
                    EscalationMinutes = table.Column<int>(type: "int", nullable: false),
                    CompensationAllowed = table.Column<bool>(type: "bit", nullable: false),
                    EffectiveFrom = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EffectiveTo = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fo_DanhMucKhieuNaiFO", x => x.HotelComplaintProfileId);
                });

            migrationBuilder.CreateTable(
                name: "QuyTacHousekeepingFO",
                schema: "fo",
                columns: table => new
                {
                    HotelHousekeepingRuleProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PropertyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    Kind = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Priority = table.Column<int>(type: "int", nullable: false),
                    Department = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SlaMinutes = table.Column<int>(type: "int", nullable: false),
                    RequiresInspection = table.Column<bool>(type: "bit", nullable: false),
                    AutoCreateTask = table.Column<bool>(type: "bit", nullable: false),
                    EffectiveFrom = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EffectiveTo = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fo_QuyTacHousekeepingFO", x => x.HotelHousekeepingRuleProfileId);
                });

            migrationBuilder.CreateTable(
                name: "QuyTacWakeupFO",
                schema: "fo",
                columns: table => new
                {
                    HotelWakeupRuleProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PropertyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    Kind = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    DefaultLeadMinutes = table.Column<int>(type: "int", nullable: false),
                    EscalationMinutes = table.Column<int>(type: "int", nullable: false),
                    ConfirmationRequired = table.Column<bool>(type: "bit", nullable: false),
                    NotificationChannel = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    EffectiveFrom = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EffectiveTo = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fo_QuyTacWakeupFO", x => x.HotelWakeupRuleProfileId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_fo_DanhMucKhieuNaiFO_CoSo_Muc",
                schema: "fo",
                table: "DanhMucKhieuNaiFO",
                columns: new[] { "PropertyId", "Severity", "EffectiveFrom", "EffectiveTo", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "UQ_fo_DanhMucKhieuNaiFO_CoSo_Ma",
                schema: "fo",
                table: "DanhMucKhieuNaiFO",
                columns: new[] { "PropertyId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_fo_QuyTacHousekeepingFO_CoSo_Loai",
                schema: "fo",
                table: "QuyTacHousekeepingFO",
                columns: new[] { "PropertyId", "Kind", "EffectiveFrom", "EffectiveTo", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "UQ_fo_QuyTacHousekeepingFO_CoSo_Ma",
                schema: "fo",
                table: "QuyTacHousekeepingFO",
                columns: new[] { "PropertyId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_fo_QuyTacWakeupFO_CoSo_Loai",
                schema: "fo",
                table: "QuyTacWakeupFO",
                columns: new[] { "PropertyId", "Kind", "EffectiveFrom", "EffectiveTo", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "UQ_fo_QuyTacWakeupFO_CoSo_Ma",
                schema: "fo",
                table: "QuyTacWakeupFO",
                columns: new[] { "PropertyId", "Code" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DanhMucKhieuNaiFO",
                schema: "fo");

            migrationBuilder.DropTable(
                name: "QuyTacHousekeepingFO",
                schema: "fo");

            migrationBuilder.DropTable(
                name: "QuyTacWakeupFO",
                schema: "fo");
        }
    }
}
