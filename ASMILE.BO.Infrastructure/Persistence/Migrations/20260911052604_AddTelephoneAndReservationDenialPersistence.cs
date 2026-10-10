using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ASMILE.BO.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTelephoneAndReservationDenialPersistence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DanhBaDienThoai",
                schema: "fo",
                columns: table => new
                {
                    ContactId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PropertyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    PhoneNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    RoomNumber = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    Category = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fo_DanhBaDienThoai", x => x.ContactId);
                });

            migrationBuilder.CreateTable(
                name: "DatPhongTuChoi",
                schema: "fo",
                columns: table => new
                {
                    DenialId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PropertyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DenialNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    GuestName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    GuestPhone = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ArrivalDate = table.Column<DateOnly>(type: "date", nullable: false),
                    DepartureDate = table.Column<DateOnly>(type: "date", nullable: false),
                    AdultCount = table.Column<int>(type: "int", nullable: false),
                    ChildCount = table.Column<int>(type: "int", nullable: false),
                    RoomTypeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Source = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    RecoveredReservationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ClosedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fo_DatPhongTuChoi", x => x.DenialId);
                });

            migrationBuilder.CreateTable(
                name: "NhatKyCuocGoi",
                schema: "fo",
                columns: table => new
                {
                    CallId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PropertyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Direction = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    PhoneNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    GuestName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    RoomNumber = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    Subject = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    StartedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DurationSeconds = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fo_NhatKyCuocGoi", x => x.CallId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_fo_DanhBaDienThoai_CoSo_HoatDong_Ten",
                schema: "fo",
                table: "DanhBaDienThoai",
                columns: new[] { "PropertyId", "IsActive", "DisplayName" });

            migrationBuilder.CreateIndex(
                name: "IX_fo_DanhBaDienThoai_CoSo_SoDienThoai",
                schema: "fo",
                table: "DanhBaDienThoai",
                columns: new[] { "PropertyId", "PhoneNumber" });

            migrationBuilder.CreateIndex(
                name: "IX_fo_DatPhongTuChoi_CoSo_TrangThai_ThoiGian",
                schema: "fo",
                table: "DatPhongTuChoi",
                columns: new[] { "PropertyId", "Status", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "UQ_fo_DatPhongTuChoi_CoSo_SoTuChoi",
                schema: "fo",
                table: "DatPhongTuChoi",
                columns: new[] { "PropertyId", "DenialNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_fo_NhatKyCuocGoi_CoSo_Huong_ThoiGian",
                schema: "fo",
                table: "NhatKyCuocGoi",
                columns: new[] { "PropertyId", "Direction", "StartedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_fo_NhatKyCuocGoi_CoSo_SoDienThoai",
                schema: "fo",
                table: "NhatKyCuocGoi",
                columns: new[] { "PropertyId", "PhoneNumber" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DanhBaDienThoai",
                schema: "fo");

            migrationBuilder.DropTable(
                name: "DatPhongTuChoi",
                schema: "fo");

            migrationBuilder.DropTable(
                name: "NhatKyCuocGoi",
                schema: "fo");
        }
    }
}
