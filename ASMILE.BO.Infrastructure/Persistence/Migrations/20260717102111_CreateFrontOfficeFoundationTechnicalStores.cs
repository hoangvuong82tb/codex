using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ASMILE.BO.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CreateFrontOfficeFoundationTechnicalStores : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "fo");

            migrationBuilder.CreateTable(
                name: "fo_hop_thu_den",
                schema: "fo",
                columns: table => new
                {
                    ten_bo_tieu_thu = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    ma_thong_diep = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ma_bam_noi_dung = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    nhan_luc_utc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    hoan_tat_luc_utc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    trang_thai = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    so_lan_thu_lai = table.Column<int>(type: "int", nullable: false),
                    ma_loi_cuoi = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    chu_so_huu_thue = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    het_han_thue_utc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    phien_ban_dong = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fo_hop_thu_den", x => new { x.ten_bo_tieu_thu, x.ma_thong_diep });
                });

            migrationBuilder.CreateTable(
                name: "fo_hop_thu_di",
                schema: "fo",
                columns: table => new
                {
                    ma_thong_diep = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ten_su_kien = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    phien_ban_schema = table.Column<int>(type: "int", nullable: false),
                    xay_ra_luc_utc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    xuat_ban_luc_utc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ma_co_so = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ngay_nghiep_vu = table.Column<DateOnly>(type: "date", nullable: true),
                    ma_tuong_quan = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ma_nguyen_nhan = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    loai_tong_hop = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    ma_tong_hop = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    phien_ban_tong_hop = table.Column<long>(type: "bigint", nullable: false),
                    khoa_phan_vung = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    do_nhay_cam = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    noi_dung = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ma_bam_noi_dung = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    trang_thai = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    so_lan_thu_lai = table.Column<int>(type: "int", nullable: false),
                    thu_lai_luc_utc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    chu_so_huu_thue = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    het_han_thue_utc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ma_loi_cuoi = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    tao_luc_utc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    phien_ban_dong = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fo_hop_thu_di", x => x.ma_thong_diep);
                });

            migrationBuilder.CreateTable(
                name: "fo_khoa_idempotency",
                schema: "fo",
                columns: table => new
                {
                    ma = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    khong_gian_ten = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    khoa = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    ma_lenh = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ma_bam_yeu_cau = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    trang_thai = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    loai_ket_qua = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    tham_chieu_ket_qua = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    tao_luc_utc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    hoan_tat_luc_utc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    het_han_luc_utc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    phan_loai_that_bai = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    phien_ban_dong = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fo_khoa_idempotency", x => x.ma);
                });

            migrationBuilder.CreateIndex(
                name: "IX_fo_hop_thu_di_het_han_thue",
                schema: "fo",
                table: "fo_hop_thu_di",
                column: "het_han_thue_utc");

            migrationBuilder.CreateIndex(
                name: "IX_fo_hop_thu_di_ma_bam",
                schema: "fo",
                table: "fo_hop_thu_di",
                column: "ma_bam_noi_dung");

            migrationBuilder.CreateIndex(
                name: "IX_fo_hop_thu_di_trang_thai_thu_lai",
                schema: "fo",
                table: "fo_hop_thu_di",
                columns: new[] { "trang_thai", "thu_lai_luc_utc" });

            migrationBuilder.CreateIndex(
                name: "IX_fo_khoa_idempotency_trang_thai_het_han",
                schema: "fo",
                table: "fo_khoa_idempotency",
                columns: new[] { "trang_thai", "het_han_luc_utc" });

            migrationBuilder.CreateIndex(
                name: "UQ_fo_khoa_idempotency_khong_gian_khoa",
                schema: "fo",
                table: "fo_khoa_idempotency",
                columns: new[] { "khong_gian_ten", "khoa" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "fo_hop_thu_den",
                schema: "fo");

            migrationBuilder.DropTable(
                name: "fo_hop_thu_di",
                schema: "fo");

            migrationBuilder.DropTable(
                name: "fo_khoa_idempotency",
                schema: "fo");
        }
    }
}
