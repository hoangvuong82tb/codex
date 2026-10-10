using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ASMILE.BO.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddHotelNightAuditPolicies : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ChinhSachNightAudit",
                schema: "fo",
                columns: table => new
                {
                    HotelNightAuditPolicyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PropertyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    EffectiveFrom = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EffectiveTo = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LockBusinessDateDuringAudit = table.Column<bool>(type: "bit", nullable: false),
                    RequireAllValidationsPassed = table.Column<bool>(type: "bit", nullable: false),
                    AllowResumeAfterFailure = table.Column<bool>(type: "bit", nullable: false),
                    RequireSupervisorApproval = table.Column<bool>(type: "bit", nullable: false),
                    MaximumRetries = table.Column<int>(type: "int", nullable: false),
                    PostRoomCharges = table.Column<bool>(type: "bit", nullable: false),
                    CloseEligibleFolios = table.Column<bool>(type: "bit", nullable: false),
                    AdvanceBusinessDateOnSuccess = table.Column<bool>(type: "bit", nullable: false),
                    SupervisorRole = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
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
                    table.PrimaryKey("PK_fo_ChinhSachNightAudit", x => x.HotelNightAuditPolicyId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_fo_ChinhSachNightAudit_CoSo_HieuLuc",
                schema: "fo",
                table: "ChinhSachNightAudit",
                columns: new[] { "PropertyId", "EffectiveFrom", "EffectiveTo", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "UQ_fo_ChinhSachNightAudit_CoSo_Ma",
                schema: "fo",
                table: "ChinhSachNightAudit",
                columns: new[] { "PropertyId", "Code" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChinhSachNightAudit",
                schema: "fo");
        }
    }
}
