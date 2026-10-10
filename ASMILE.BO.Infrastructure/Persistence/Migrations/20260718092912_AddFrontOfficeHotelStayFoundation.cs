using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ASMILE.BO.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddFrontOfficeHotelStayFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateSequence(
                name: "TrinhTuSoLuuTru",
                schema: "fo");

            migrationBuilder.CreateTable(
                name: "LuuTruKhachSan",
                schema: "fo",
                columns: table => new
                {
                    HotelStayId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PropertyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StayNumber = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    ReservationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ReservationNumberSnapshot = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    GuestId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    GuestNameSnapshot = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    GuestPhoneSnapshot = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    RoomId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RoomNumberSnapshot = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    RoomTypeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RoomTypeCodeSnapshot = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    ScheduledArrivalDate = table.Column<DateOnly>(type: "date", nullable: false),
                    ScheduledDepartureDate = table.Column<DateOnly>(type: "date", nullable: false),
                    ActualCheckInAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ActualCheckOutAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Adults = table.Column<int>(type: "int", nullable: false),
                    Children = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Source = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    InternalNotes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fo_LuuTruKhachSan", x => x.HotelStayId);
                    table.ForeignKey(
                        name: "FK_LuuTruKhachSan_DatPhong_ReservationId",
                        column: x => x.ReservationId,
                        principalSchema: "fo",
                        principalTable: "DatPhong",
                        principalColumn: "ReservationId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LuuTruKhachSan_LoaiPhongKhachSan_RoomTypeId",
                        column: x => x.RoomTypeId,
                        principalSchema: "fo",
                        principalTable: "LoaiPhongKhachSan",
                        principalColumn: "HotelRoomTypeId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LuuTruKhachSan_PhongKhachSan_RoomId",
                        column: x => x.RoomId,
                        principalSchema: "fo",
                        principalTable: "PhongKhachSan",
                        principalColumn: "HotelRoomId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LichSuLuuTruKhachSan",
                schema: "fo",
                columns: table => new
                {
                    HistoryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HotelStayId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PropertyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Action = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    FromStatus = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    ToStatus = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    ActorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    OccurredAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CorrelationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SafeMetadata = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fo_LichSuLuuTruKhachSan", x => x.HistoryId);
                    table.ForeignKey(
                        name: "FK_LichSuLuuTruKhachSan_LuuTruKhachSan_HotelStayId",
                        column: x => x.HotelStayId,
                        principalSchema: "fo",
                        principalTable: "LuuTruKhachSan",
                        principalColumn: "HotelStayId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_fo_LichSuLuuTruKhachSan_LuuTru_Luc",
                schema: "fo",
                table: "LichSuLuuTruKhachSan",
                columns: new[] { "HotelStayId", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_fo_LuuTruKhachSan_CapNhat",
                schema: "fo",
                table: "LuuTruKhachSan",
                column: "UpdatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_fo_LuuTruKhachSan_CoSo_NgayDen",
                schema: "fo",
                table: "LuuTruKhachSan",
                columns: new[] { "PropertyId", "ScheduledArrivalDate" });

            migrationBuilder.CreateIndex(
                name: "IX_fo_LuuTruKhachSan_CoSo_NgayDi",
                schema: "fo",
                table: "LuuTruKhachSan",
                columns: new[] { "PropertyId", "ScheduledDepartureDate" });

            migrationBuilder.CreateIndex(
                name: "IX_fo_LuuTruKhachSan_CoSo_Phong_TrangThai",
                schema: "fo",
                table: "LuuTruKhachSan",
                columns: new[] { "PropertyId", "RoomId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_fo_LuuTruKhachSan_CoSo_TrangThai",
                schema: "fo",
                table: "LuuTruKhachSan",
                columns: new[] { "PropertyId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_LuuTruKhachSan_RoomId",
                schema: "fo",
                table: "LuuTruKhachSan",
                column: "RoomId");

            migrationBuilder.CreateIndex(
                name: "IX_LuuTruKhachSan_RoomTypeId",
                schema: "fo",
                table: "LuuTruKhachSan",
                column: "RoomTypeId");

            migrationBuilder.CreateIndex(
                name: "UQ_fo_LuuTruKhachSan_DatPhong_HoatDong",
                schema: "fo",
                table: "LuuTruKhachSan",
                column: "ReservationId",
                unique: true,
                filter: "[ReservationId] IS NOT NULL AND [Status] IN ('PendingArrival', 'InHouse')");

            migrationBuilder.CreateIndex(
                name: "UQ_fo_LuuTruKhachSan_SoLuuTru",
                schema: "fo",
                table: "LuuTruKhachSan",
                column: "StayNumber",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LichSuLuuTruKhachSan",
                schema: "fo");

            migrationBuilder.DropTable(
                name: "LuuTruKhachSan",
                schema: "fo");

            migrationBuilder.DropSequence(
                name: "TrinhTuSoLuuTru",
                schema: "fo");
        }
    }
}
