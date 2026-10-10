using System;
using ASMILE.BO.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ASMILE.BO.Infrastructure.Persistence.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260907221500_AddHotelExchangeRates")]
public partial class AddHotelExchangeRates : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "TyGia", schema: "fo",
            columns: table => new
            {
                HotelExchangeRateId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                PropertyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                CurrencyCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                CurrencyName = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                Symbol = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                BuyRate = table.Column<decimal>(type: "decimal(18,6)", nullable: false),
                SellRate = table.Column<decimal>(type: "decimal(18,6)", nullable: false),
                HotelRate = table.Column<decimal>(type: "decimal(18,6)", nullable: false),
                EffectiveFromUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                EffectiveToUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                IsDefault = table.Column<bool>(type: "bit", nullable: false),
                Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
            }, constraints: table => { table.PrimaryKey("PK_fo_TyGia", x => x.HotelExchangeRateId); });

        migrationBuilder.CreateIndex(name: "UQ_fo_TyGia_CoSo_Ma_TuNgay", schema: "fo", table: "TyGia", columns: new[] { "PropertyId", "CurrencyCode", "EffectiveFromUtc" }, unique: true);
        migrationBuilder.CreateIndex(name: "IX_fo_TyGia_CoSo_TrangThai", schema: "fo", table: "TyGia", columns: new[] { "PropertyId", "Status" });
        migrationBuilder.CreateIndex(name: "IX_fo_TyGia_CoSo_HieuLuc", schema: "fo", table: "TyGia", columns: new[] { "PropertyId", "EffectiveFromUtc", "EffectiveToUtc" });
    }

    protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.DropTable(name: "TyGia", schema: "fo");
}
