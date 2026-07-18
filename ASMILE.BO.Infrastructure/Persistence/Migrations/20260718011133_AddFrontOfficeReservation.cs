using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ASMILE.BO.Infrastructure.Persistence.Migrations
{
    public partial class AddFrontOfficeReservation : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateSequence<long>(name: "TrinhTuSoDatPhong", schema: "fo", startValue: 1L);
            migrationBuilder.CreateTable(name: "DatPhong", schema: "fo", columns: table => new
            {
                ReservationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false), ReservationNumber = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false), PropertyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false), GuestId = table.Column<Guid>(type: "uniqueidentifier", nullable: true), GuestNameSnapshot = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false), GuestPhoneSnapshot = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true), GuestEmailSnapshot = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true), ArrivalDate = table.Column<DateOnly>(type: "date", nullable: false), DepartureDate = table.Column<DateOnly>(type: "date", nullable: false), AdultCount = table.Column<int>(type: "int", nullable: false), ChildCount = table.Column<int>(type: "int", nullable: false), RoomTypeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true), AssignedRoomId = table.Column<Guid>(type: "uniqueidentifier", nullable: true), RatePlanId = table.Column<Guid>(type: "uniqueidentifier", nullable: true), Source = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false), Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false), SpecialRequests = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true), InternalNotes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true), CancellationReason = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true), CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false), CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true), UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false), UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true), ConfirmedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true), CancelledAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true), NoShowAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true), RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
            }, constraints: table => table.PrimaryKey("PK_fo_DatPhong", x => x.ReservationId));
            migrationBuilder.CreateTable(name: "LichSuDatPhong", schema: "fo", columns: table => new
            {
                HistoryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false), ReservationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false), Action = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false), FromStatus = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true), ToStatus = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false), OccurredAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false), ActorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true), CorrelationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false), ReasonCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true), SafeSummary = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true)
            }, constraints: table => { table.PrimaryKey("PK_fo_LichSuDatPhong", x => x.HistoryId); table.ForeignKey(name: "FK_LichSuDatPhong_DatPhong_ReservationId", column: x => x.ReservationId, principalSchema: "fo", principalTable: "DatPhong", principalColumn: "ReservationId", onDelete: ReferentialAction.Restrict); });
            migrationBuilder.CreateIndex("UQ_fo_DatPhong_CoSo_SoDatPhong", "DatPhong", new[] { "PropertyId", "ReservationNumber" }, schema: "fo", unique: true);
            migrationBuilder.CreateIndex("IX_fo_DatPhong_CoSo_NgayO", "DatPhong", new[] { "PropertyId", "ArrivalDate", "DepartureDate" }, schema: "fo");
            migrationBuilder.CreateIndex("IX_fo_DatPhong_CoSo_TrangThai", "DatPhong", new[] { "PropertyId", "Status" }, schema: "fo");
            migrationBuilder.CreateIndex("IX_fo_DatPhong_Phong_NgayO", "DatPhong", new[] { "PropertyId", "AssignedRoomId", "ArrivalDate", "DepartureDate" }, schema: "fo");
            migrationBuilder.CreateIndex("IX_fo_LichSuDatPhong_DatPhong_Luc", "LichSuDatPhong", new[] { "ReservationId", "OccurredAtUtc" }, schema: "fo");
        }
        protected override void Down(MigrationBuilder migrationBuilder) { migrationBuilder.DropTable("LichSuDatPhong", "fo"); migrationBuilder.DropTable("DatPhong", "fo"); migrationBuilder.DropSequence("TrinhTuSoDatPhong", "fo"); }
    }
}
