using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ASMILE.BO.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddFrontOfficeMarketingData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TiepThi",
                schema: "fo",
                columns: table => new
                {
                    MarketingRecordId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PropertyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Section = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Code = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    GroupName = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Owner = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    Revenue = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fo_TiepThi", x => x.MarketingRecordId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_fo_TiepThi_CoSo_Nhom",
                schema: "fo",
                table: "TiepThi",
                columns: new[] { "PropertyId", "GroupName" });

            migrationBuilder.CreateIndex(
                name: "IX_fo_TiepThi_CoSo_PhanLoai_TrangThai",
                schema: "fo",
                table: "TiepThi",
                columns: new[] { "PropertyId", "Section", "Status" });

            migrationBuilder.CreateIndex(
                name: "UQ_fo_TiepThi_CoSo_PhanLoai_Ma",
                schema: "fo",
                table: "TiepThi",
                columns: new[] { "PropertyId", "Section", "Code" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TiepThi",
                schema: "fo");
        }
    }
}
