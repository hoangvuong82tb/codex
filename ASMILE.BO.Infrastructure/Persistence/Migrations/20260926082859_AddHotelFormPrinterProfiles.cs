using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ASMILE.BO.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddHotelFormPrinterProfiles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MauBieuMayInFO",
                schema: "fo",
                columns: table => new
                {
                    HotelFormPrinterProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PropertyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    DocumentType = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    TemplateReference = table.Column<string>(type: "nvarchar(260)", maxLength: 260, nullable: false),
                    PrinterName = table.Column<string>(type: "nvarchar(260)", maxLength: 260, nullable: false),
                    PaperSize = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Copies = table.Column<int>(type: "int", nullable: false),
                    Duplex = table.Column<bool>(type: "bit", nullable: false),
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
                    table.PrimaryKey("PK_fo_MauBieuMayInFO", x => x.HotelFormPrinterProfileId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_fo_MauBieuMayInFO_CoSo_NghiepVu",
                schema: "fo",
                table: "MauBieuMayInFO",
                columns: new[] { "PropertyId", "DocumentType", "EffectiveFrom", "EffectiveTo", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "UQ_fo_MauBieuMayInFO_CoSo_Ma",
                schema: "fo",
                table: "MauBieuMayInFO",
                columns: new[] { "PropertyId", "Code" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MauBieuMayInFO",
                schema: "fo");
        }
    }
}
