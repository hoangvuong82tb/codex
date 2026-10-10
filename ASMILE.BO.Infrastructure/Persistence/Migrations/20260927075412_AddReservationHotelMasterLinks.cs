using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ASMILE.BO.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddReservationHotelMasterLinks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "QuyTacHoaHongFO",
                schema: "fo",
                columns: table => new
                {
                    HotelCommissionRuleProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PropertyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    PartnerKind = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Basis = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    RatePercent = table.Column<decimal>(type: "decimal(9,4)", precision: 9, scale: 4, nullable: false),
                    FixedAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CurrencyCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    IncludeTax = table.Column<bool>(type: "bit", nullable: false),
                    IncludeServiceCharge = table.Column<bool>(type: "bit", nullable: false),
                    MinimumAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    MaximumAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    ApprovalRequired = table.Column<bool>(type: "bit", nullable: false),
                    EffectiveFrom = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EffectiveTo = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table => table.PrimaryKey("PK_fo_QuyTacHoaHongFO", x => x.HotelCommissionRuleProfileId));

            migrationBuilder.CreateTable(
                name: "DoiTacCongTyFO",
                schema: "fo",
                columns: table => new
                {
                    HotelCompanyProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PropertyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Kind = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    TaxCode = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    BusinessLicense = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    ContactName = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: true),
                    Phone = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(320)", maxLength: 320, nullable: true),
                    Address = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CurrencyCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    CreditLimit = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CreditDays = table.Column<int>(type: "int", nullable: false),
                    RequireContract = table.Column<bool>(type: "bit", nullable: false),
                    RequirePurchaseOrder = table.Column<bool>(type: "bit", nullable: false),
                    EffectiveFrom = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EffectiveTo = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table => table.PrimaryKey("PK_fo_DoiTacCongTyFO", x => x.HotelCompanyProfileId));

            migrationBuilder.CreateTable(
                name: "DaiLyDuLichFO",
                schema: "fo",
                columns: table => new
                {
                    HotelTravelAgentProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PropertyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Kind = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    IataCode = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    LicenseNumber = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    ContactName = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: true),
                    Phone = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(320)", maxLength: 320, nullable: true),
                    Address = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CommissionRuleCode = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    DefaultMarketSegmentCode = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    PaymentTerms = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    CreditLimit = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    EffectiveFrom = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EffectiveTo = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table => table.PrimaryKey("PK_fo_DaiLyDuLichFO", x => x.HotelTravelAgentProfileId));

            migrationBuilder.CreateIndex("UQ_fo_QuyTacHoaHongFO_CoSo_Ma", "QuyTacHoaHongFO", "Code", "fo", unique: true);
            migrationBuilder.CreateIndex("IX_fo_QuyTacHoaHongFO_CoSo_DoiTac", "QuyTacHoaHongFO", new[] { "PropertyId", "PartnerKind", "EffectiveFrom", "EffectiveTo", "IsActive" }, "fo");
            migrationBuilder.CreateIndex("UQ_fo_DoiTacCongTyFO_CoSo_Ma", "DoiTacCongTyFO", new[] { "PropertyId", "Code" }, "fo", unique: true);
            migrationBuilder.CreateIndex("IX_fo_DoiTacCongTyFO_CoSo_Loai", "DoiTacCongTyFO", new[] { "PropertyId", "Kind", "EffectiveFrom", "EffectiveTo", "IsActive" }, "fo");
            migrationBuilder.CreateIndex("UQ_fo_DaiLyDuLichFO_CoSo_Ma", "DaiLyDuLichFO", new[] { "PropertyId", "Code" }, "fo", unique: true);
            migrationBuilder.CreateIndex("IX_fo_DaiLyDuLichFO_CoSo_Loai", "DaiLyDuLichFO", new[] { "PropertyId", "Kind", "EffectiveFrom", "EffectiveTo", "IsActive" }, "fo");

            // The previous reservation migration indexed the legacy text
            // column MarketSegment. SQL Server does not allow ALTER COLUMN
            // while that index still depends on it. Remove the legacy index
            // before narrowing the column and replace it with the new
            // profile-based index below.
            migrationBuilder.DropIndex(
                name: "IX_fo_DatPhong_CoSo_PhanKhuc",
                schema: "fo",
                table: "DatPhong");

            migrationBuilder.AlterColumn<string>(
                name: "TravelAgentName",
                schema: "fo",
                table: "DatPhong",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "MarketSegment",
                schema: "fo",
                table: "DatPhong",
                type: "nvarchar(80)",
                maxLength: 80,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "CompanyName",
                schema: "fo",
                table: "DatPhong",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "BookingChannelProfileId",
                schema: "fo",
                table: "DatPhong",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "BookingSourceProfileId",
                schema: "fo",
                table: "DatPhong",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CommissionRuleProfileId",
                schema: "fo",
                table: "DatPhong",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CompanyProfileId",
                schema: "fo",
                table: "DatPhong",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "MarketSegmentProfileId",
                schema: "fo",
                table: "DatPhong",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "TravelAgentProfileId",
                schema: "fo",
                table: "DatPhong",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_fo_DatPhong_CoSo_DoiTac",
                schema: "fo",
                table: "DatPhong",
                columns: new[] { "PropertyId", "CompanyProfileId", "TravelAgentProfileId" });

            migrationBuilder.CreateIndex(
                name: "IX_fo_DatPhong_CoSo_Nguon_Kenh_PhanKhuc",
                schema: "fo",
                table: "DatPhong",
                columns: new[] { "PropertyId", "BookingSourceProfileId", "BookingChannelProfileId", "MarketSegmentProfileId" });

            migrationBuilder.AddForeignKey(name: "FK_fo_DatPhong_NguonDatPhongFO", schema: "fo", table: "DatPhong", column: "BookingSourceProfileId", principalSchema: "fo", principalTable: "NguonDatPhongFO", principalColumn: "HotelBookingSourceProfileId", onDelete: ReferentialAction.Restrict);
            migrationBuilder.AddForeignKey(name: "FK_fo_DatPhong_KenhDatPhongFO", schema: "fo", table: "DatPhong", column: "BookingChannelProfileId", principalSchema: "fo", principalTable: "KenhDatPhongFO", principalColumn: "HotelBookingChannelProfileId", onDelete: ReferentialAction.Restrict);
            migrationBuilder.AddForeignKey(name: "FK_fo_DatPhong_PhanKhucThiTruongFO", schema: "fo", table: "DatPhong", column: "MarketSegmentProfileId", principalSchema: "fo", principalTable: "PhanKhucThiTruongFO", principalColumn: "HotelMarketSegmentProfileId", onDelete: ReferentialAction.Restrict);
            migrationBuilder.AddForeignKey(name: "FK_fo_DatPhong_DoiTacCongTyFO", schema: "fo", table: "DatPhong", column: "CompanyProfileId", principalSchema: "fo", principalTable: "DoiTacCongTyFO", principalColumn: "HotelCompanyProfileId", onDelete: ReferentialAction.Restrict);
            migrationBuilder.AddForeignKey(name: "FK_fo_DatPhong_DaiLyDuLichFO", schema: "fo", table: "DatPhong", column: "TravelAgentProfileId", principalSchema: "fo", principalTable: "DaiLyDuLichFO", principalColumn: "HotelTravelAgentProfileId", onDelete: ReferentialAction.Restrict);
            migrationBuilder.AddForeignKey(name: "FK_fo_DatPhong_QuyTacHoaHongFO", schema: "fo", table: "DatPhong", column: "CommissionRuleProfileId", principalSchema: "fo", principalTable: "QuyTacHoaHongFO", principalColumn: "HotelCommissionRuleProfileId", onDelete: ReferentialAction.Restrict);

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(name: "FK_fo_DatPhong_NguonDatPhongFO", schema: "fo", table: "DatPhong");
            migrationBuilder.DropForeignKey(name: "FK_fo_DatPhong_KenhDatPhongFO", schema: "fo", table: "DatPhong");
            migrationBuilder.DropForeignKey(name: "FK_fo_DatPhong_PhanKhucThiTruongFO", schema: "fo", table: "DatPhong");
            migrationBuilder.DropForeignKey(name: "FK_fo_DatPhong_DoiTacCongTyFO", schema: "fo", table: "DatPhong");
            migrationBuilder.DropForeignKey(name: "FK_fo_DatPhong_DaiLyDuLichFO", schema: "fo", table: "DatPhong");
            migrationBuilder.DropForeignKey(name: "FK_fo_DatPhong_QuyTacHoaHongFO", schema: "fo", table: "DatPhong");

            migrationBuilder.DropIndex(
                name: "IX_fo_DatPhong_CoSo_DoiTac",
                schema: "fo",
                table: "DatPhong");

            migrationBuilder.DropIndex(
                name: "IX_fo_DatPhong_CoSo_Nguon_Kenh_PhanKhuc",
                schema: "fo",
                table: "DatPhong");

            migrationBuilder.CreateIndex(
                name: "IX_fo_DatPhong_CoSo_PhanKhuc",
                schema: "fo",
                table: "DatPhong",
                columns: new[] { "PropertyId", "MarketSegment" });

            migrationBuilder.DropColumn(
                name: "BookingChannelProfileId",
                schema: "fo",
                table: "DatPhong");

            migrationBuilder.DropColumn(
                name: "BookingSourceProfileId",
                schema: "fo",
                table: "DatPhong");

            migrationBuilder.DropColumn(
                name: "CommissionRuleProfileId",
                schema: "fo",
                table: "DatPhong");

            migrationBuilder.DropColumn(
                name: "CompanyProfileId",
                schema: "fo",
                table: "DatPhong");

            migrationBuilder.DropColumn(
                name: "MarketSegmentProfileId",
                schema: "fo",
                table: "DatPhong");

            migrationBuilder.DropColumn(
                name: "TravelAgentProfileId",
                schema: "fo",
                table: "DatPhong");

            migrationBuilder.AlterColumn<string>(
                name: "TravelAgentName",
                schema: "fo",
                table: "DatPhong",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "MarketSegment",
                schema: "fo",
                table: "DatPhong",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(80)",
                oldMaxLength: 80,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "CompanyName",
                schema: "fo",
                table: "DatPhong",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.DropTable("QuyTacHoaHongFO", "fo");
            migrationBuilder.DropTable("DoiTacCongTyFO", "fo");
            migrationBuilder.DropTable("DaiLyDuLichFO", "fo");
        }
    }
}
