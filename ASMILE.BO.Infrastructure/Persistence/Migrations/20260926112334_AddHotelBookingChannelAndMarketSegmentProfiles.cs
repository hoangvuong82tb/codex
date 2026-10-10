using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ASMILE.BO.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddHotelBookingChannelAndMarketSegmentProfiles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "KenhDatPhongFO",
                schema: "fo",
                columns: table => new
                {
                    HotelBookingChannelProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PropertyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    Kind = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    IsOnline = table.Column<bool>(type: "bit", nullable: false),
                    RequiresExternalConfirmation = table.Column<bool>(type: "bit", nullable: false),
                    ConnectorKey = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
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
                    table.PrimaryKey("PK_fo_KenhDatPhongFO", x => x.HotelBookingChannelProfileId);
                });

            migrationBuilder.CreateTable(
                name: "PhanKhucThiTruongFO",
                schema: "fo",
                columns: table => new
                {
                    HotelMarketSegmentProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PropertyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    Kind = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    RevenueBucket = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DefaultRateCode = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    ForecastCategory = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
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
                    table.PrimaryKey("PK_fo_PhanKhucThiTruongFO", x => x.HotelMarketSegmentProfileId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_fo_KenhDatPhongFO_CoSo_Loai",
                schema: "fo",
                table: "KenhDatPhongFO",
                columns: new[] { "PropertyId", "Kind", "EffectiveFrom", "EffectiveTo", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "UQ_fo_KenhDatPhongFO_CoSo_Ma",
                schema: "fo",
                table: "KenhDatPhongFO",
                columns: new[] { "PropertyId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_fo_PhanKhucThiTruongFO_CoSo_Loai",
                schema: "fo",
                table: "PhanKhucThiTruongFO",
                columns: new[] { "PropertyId", "Kind", "EffectiveFrom", "EffectiveTo", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "UQ_fo_PhanKhucThiTruongFO_CoSo_Ma",
                schema: "fo",
                table: "PhanKhucThiTruongFO",
                columns: new[] { "PropertyId", "Code" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "KenhDatPhongFO",
                schema: "fo");

            migrationBuilder.DropTable(
                name: "PhanKhucThiTruongFO",
                schema: "fo");
        }
    }
}
