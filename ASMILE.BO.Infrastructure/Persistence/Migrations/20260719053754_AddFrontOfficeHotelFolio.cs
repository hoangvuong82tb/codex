using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ASMILE.BO.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddFrontOfficeHotelFolio : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateSequence(
                name: "TrinhTuSoFolio",
                schema: "fo");

            migrationBuilder.CreateTable(
                name: "SoFolio",
                schema: "fo",
                columns: table => new
                {
                    HotelFolioId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PropertyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HotelStayId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FolioNumber = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ClosedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ClosedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fo_SoFolio", x => x.HotelFolioId);
                    table.ForeignKey(
                        name: "FK_SoFolio_LuuTruKhachSan_HotelStayId",
                        column: x => x.HotelStayId,
                        principalSchema: "fo",
                        principalTable: "LuuTruKhachSan",
                        principalColumn: "HotelStayId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ButToanFolio",
                schema: "fo",
                columns: table => new
                {
                    HotelFolioEntryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HotelFolioId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PropertyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SequenceNumber = table.Column<int>(type: "int", nullable: false),
                    EntryType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ReferenceType = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    ReferenceId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ReversesEntryId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ReversedByEntryId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fo_ButToanFolio", x => x.HotelFolioEntryId);
                    table.ForeignKey(
                        name: "FK_ButToanFolio_SoFolio_HotelFolioId",
                        column: x => x.HotelFolioId,
                        principalSchema: "fo",
                        principalTable: "SoFolio",
                        principalColumn: "HotelFolioId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_fo_ButToanFolio_CoSo_Folio",
                schema: "fo",
                table: "ButToanFolio",
                columns: new[] { "PropertyId", "HotelFolioId" });

            migrationBuilder.CreateIndex(
                name: "IX_fo_ButToanFolio_Folio_ThoiGian",
                schema: "fo",
                table: "ButToanFolio",
                columns: new[] { "HotelFolioId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "UQ_fo_ButToanFolio_Folio_Sequence",
                schema: "fo",
                table: "ButToanFolio",
                columns: new[] { "HotelFolioId", "SequenceNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_fo_SoFolio_CoSo_TrangThai",
                schema: "fo",
                table: "SoFolio",
                columns: new[] { "PropertyId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_SoFolio_HotelStayId",
                schema: "fo",
                table: "SoFolio",
                column: "HotelStayId");

            migrationBuilder.CreateIndex(
                name: "UQ_fo_SoFolio_CoSo_LuuTru",
                schema: "fo",
                table: "SoFolio",
                columns: new[] { "PropertyId", "HotelStayId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_fo_SoFolio_SoFolio",
                schema: "fo",
                table: "SoFolio",
                column: "FolioNumber",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ButToanFolio",
                schema: "fo");

            migrationBuilder.DropTable(
                name: "SoFolio",
                schema: "fo");

            migrationBuilder.DropSequence(
                name: "TrinhTuSoFolio",
                schema: "fo");
        }
    }
}
