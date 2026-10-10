using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ASMILE.BO.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddHotelPromotionPackages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "KhuyenMaiGoiDichVu",
                schema: "fo",
                columns: table => new
                {
                    HotelPromotionPackageId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PropertyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    OfferType = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    DiscountMode = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    DiscountValue = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    EffectiveFrom = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EffectiveTo = table.Column<DateTime>(type: "datetime2", nullable: false),
                    BookingFrom = table.Column<DateTime>(type: "datetime2", nullable: true),
                    BookingTo = table.Column<DateTime>(type: "datetime2", nullable: true),
                    MinimumNights = table.Column<int>(type: "int", nullable: false),
                    RequiresPrepayment = table.Column<bool>(type: "bit", nullable: false),
                    IncludedServices = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    BlackoutDates = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
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
                    table.PrimaryKey("PK_fo_KhuyenMaiGoiDichVu", x => x.HotelPromotionPackageId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_fo_KhuyenMaiGoiDichVu_CoSo_HieuLuc",
                schema: "fo",
                table: "KhuyenMaiGoiDichVu",
                columns: new[] { "PropertyId", "EffectiveFrom", "EffectiveTo", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "UQ_fo_KhuyenMaiGoiDichVu_CoSo_Ma",
                schema: "fo",
                table: "KhuyenMaiGoiDichVu",
                columns: new[] { "PropertyId", "Code" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "KhuyenMaiGoiDichVu",
                schema: "fo");
        }
    }
}
