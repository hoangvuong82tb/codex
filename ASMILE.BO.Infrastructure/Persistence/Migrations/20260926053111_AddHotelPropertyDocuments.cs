using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ASMILE.BO.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddHotelPropertyDocuments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "HoSoGiayPhepKhachSan",
                schema: "fo",
                columns: table => new
                {
                    HotelPropertyDocumentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PropertyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DocumentType = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    DocumentNumber = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    IssuedBy = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: true),
                    IssuedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiryDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FileReference = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    IsRequired = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fo_HoSoGiayPhepKhachSan", x => x.HotelPropertyDocumentId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_fo_HoSoGiayPhepKhachSan_CoSo_HetHan",
                schema: "fo",
                table: "HoSoGiayPhepKhachSan",
                columns: new[] { "PropertyId", "ExpiryDate", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "UQ_fo_HoSoGiayPhepKhachSan_CoSo_Loai_So",
                schema: "fo",
                table: "HoSoGiayPhepKhachSan",
                columns: new[] { "PropertyId", "DocumentType", "DocumentNumber" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HoSoGiayPhepKhachSan",
                schema: "fo");
        }
    }
}
