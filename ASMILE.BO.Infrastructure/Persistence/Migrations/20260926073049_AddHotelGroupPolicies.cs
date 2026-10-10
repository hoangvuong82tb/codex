using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ASMILE.BO.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddHotelGroupPolicies : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ChinhSachDoanKhachSan",
                schema: "fo",
                columns: table => new
                {
                    HotelGroupPolicyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PropertyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    EffectiveFrom = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EffectiveTo = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DefaultCutoffDays = table.Column<int>(type: "int", nullable: false),
                    RoomReleaseDaysBeforeArrival = table.Column<int>(type: "int", nullable: false),
                    DepositPercent = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    DepositDueDaysBeforeArrival = table.Column<int>(type: "int", nullable: false),
                    DepositRequired = table.Column<bool>(type: "bit", nullable: false),
                    AllowRoomingListImport = table.Column<bool>(type: "bit", nullable: false),
                    DefaultRouting = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    DefaultBillingInstruction = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
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
                    table.PrimaryKey("PK_fo_ChinhSachDoanKhachSan", x => x.HotelGroupPolicyId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_fo_ChinhSachDoanKhachSan_CoSo_HieuLuc",
                schema: "fo",
                table: "ChinhSachDoanKhachSan",
                columns: new[] { "PropertyId", "EffectiveFrom", "EffectiveTo", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "UQ_fo_ChinhSachDoanKhachSan_CoSo_Ma",
                schema: "fo",
                table: "ChinhSachDoanKhachSan",
                columns: new[] { "PropertyId", "Code" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChinhSachDoanKhachSan",
                schema: "fo");
        }
    }
}
