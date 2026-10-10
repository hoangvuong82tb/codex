using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ASMILE.BO.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddHotelProfileTypeProfiles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LoaiHoSoFO",
                schema: "fo",
                columns: table => new
                {
                    HotelProfileTypeProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PropertyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    Kind = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    RequiredFields = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DefaultRateCode = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    DefaultPaymentMethodCode = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    DefaultMarketCode = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    DefaultSourceCode = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
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
                    table.PrimaryKey("PK_fo_LoaiHoSoFO", x => x.HotelProfileTypeProfileId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_fo_LoaiHoSoFO_CoSo_Loai",
                schema: "fo",
                table: "LoaiHoSoFO",
                columns: new[] { "PropertyId", "Kind", "EffectiveFrom", "EffectiveTo", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "UQ_fo_LoaiHoSoFO_CoSo_Ma",
                schema: "fo",
                table: "LoaiHoSoFO",
                columns: new[] { "PropertyId", "Code" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LoaiHoSoFO",
                schema: "fo");
        }
    }
}
