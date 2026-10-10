using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ASMILE.BO.Infrastructure.Persistence.Migrations;

public partial class AddGroupRoomBlocks : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "RoomBlockDoan",
            schema: "fo",
            columns: table => new
            {
                GroupRoomBlockId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                PropertyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                GroupReservationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                HotelRoomTypeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                RequestedRooms = table.Column<int>(type: "int", nullable: false),
                BlockedRooms = table.Column<int>(type: "int", nullable: false),
                PickedUpRooms = table.Column<int>(type: "int", nullable: false),
                ArrivalDate = table.Column<DateOnly>(type: "date", nullable: false),
                DepartureDate = table.Column<DateOnly>(type: "date", nullable: false),
                CutoffDate = table.Column<DateOnly>(type: "date", nullable: true),
                Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                NegotiatedRate = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_fo_RoomBlockDoan", x => x.GroupRoomBlockId);
                table.ForeignKey("FK_fo_RoomBlockDoan_DatPhongDoan", x => x.GroupReservationId, principalSchema: "fo", principalTable: "DatPhongDoan", principalColumn: "GroupReservationId", onDelete: ReferentialAction.Cascade);
                table.ForeignKey("FK_fo_RoomBlockDoan_LoaiPhongKhachSan", x => x.HotelRoomTypeId, principalSchema: "fo", principalTable: "LoaiPhongKhachSan", principalColumn: "HotelRoomTypeId", onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex("IX_fo_RoomBlockDoan_CoSo_HanChot", "RoomBlockDoan", new[] { "PropertyId", "CutoffDate" }, "fo");
        migrationBuilder.CreateIndex("IX_fo_RoomBlockDoan_CoSo_TrangThai", "RoomBlockDoan", new[] { "PropertyId", "Status" }, "fo");
        migrationBuilder.CreateIndex("IX_fo_RoomBlockDoan_GroupReservationId", "RoomBlockDoan", "GroupReservationId", "fo");
        migrationBuilder.CreateIndex("IX_fo_RoomBlockDoan_HotelRoomTypeId", "RoomBlockDoan", "HotelRoomTypeId", "fo");
        migrationBuilder.CreateIndex("UQ_fo_RoomBlockDoan_CoSo_Doan_Loai_Ngay", "RoomBlockDoan", new[] { "PropertyId", "GroupReservationId", "HotelRoomTypeId", "ArrivalDate", "DepartureDate" }, "fo", unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
        => migrationBuilder.DropTable(name: "RoomBlockDoan", schema: "fo");
}
