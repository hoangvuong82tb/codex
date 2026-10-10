using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ASMILE.BO.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddFolioRoutingRules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "QuyTacChuyenFolio",
                schema: "fo",
                columns: table => new
                {
                    HotelFolioRoutingRuleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PropertyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SourceFolioId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TargetFolioId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    EntryType = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Percent = table.Column<decimal>(type: "decimal(9,4)", nullable: false),
                    Priority = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fo_QuyTacChuyenFolio", x => x.HotelFolioRoutingRuleId);
                    table.ForeignKey(
                        name: "FK_QuyTacChuyenFolio_SoFolio_SourceFolioId",
                        column: x => x.SourceFolioId,
                        principalSchema: "fo",
                        principalTable: "SoFolio",
                        principalColumn: "HotelFolioId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_QuyTacChuyenFolio_SoFolio_TargetFolioId",
                        column: x => x.TargetFolioId,
                        principalSchema: "fo",
                        principalTable: "SoFolio",
                        principalColumn: "HotelFolioId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_fo_QuyTacChuyenFolio_CoSo_Nguon_HieuLuc",
                schema: "fo",
                table: "QuyTacChuyenFolio",
                columns: new[] { "PropertyId", "SourceFolioId", "IsActive", "Priority" });

            migrationBuilder.CreateIndex(
                name: "IX_QuyTacChuyenFolio_SourceFolioId",
                schema: "fo",
                table: "QuyTacChuyenFolio",
                column: "SourceFolioId");

            migrationBuilder.CreateIndex(
                name: "IX_QuyTacChuyenFolio_TargetFolioId",
                schema: "fo",
                table: "QuyTacChuyenFolio",
                column: "TargetFolioId");

            migrationBuilder.CreateIndex(
                name: "UQ_fo_QuyTacChuyenFolio_CoSo_NguonDich_Loai_HieuLuc",
                schema: "fo",
                table: "QuyTacChuyenFolio",
                columns: new[] { "PropertyId", "SourceFolioId", "TargetFolioId", "EntryType", "IsActive" },
                unique: true,
                filter: "[IsActive] = 1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "QuyTacChuyenFolio",
                schema: "fo");
        }
    }
}
