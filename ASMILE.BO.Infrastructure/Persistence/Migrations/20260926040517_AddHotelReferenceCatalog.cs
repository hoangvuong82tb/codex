using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ASMILE.BO.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddHotelReferenceCatalog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DanhMucThamChieuKhachSan",
                schema: "fo",
                columns: table => new
                {
                    HotelReferenceCatalogItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PropertyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CatalogKey = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Code = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fo_DanhMucThamChieuKhachSan", x => x.HotelReferenceCatalogItemId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_fo_DanhMucThamChieuKhachSan_CoSo_Nhom_HoatDong",
                schema: "fo",
                table: "DanhMucThamChieuKhachSan",
                columns: new[] { "PropertyId", "CatalogKey", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "UQ_fo_DanhMucThamChieuKhachSan_CoSo_Nhom_Ma",
                schema: "fo",
                table: "DanhMucThamChieuKhachSan",
                columns: new[] { "PropertyId", "CatalogKey", "Code" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DanhMucThamChieuKhachSan",
                schema: "fo");
        }
    }
}
