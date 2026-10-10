using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ASMILE.BO.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddConciergePersistence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CuocGoiBaoThuc",
                schema: "fo",
                columns: table => new
                {
                    CallId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PropertyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CallNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    StayId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RoomId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RoomNumber = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    GuestName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    WakeTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ActualCallTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    RetryCount = table.Column<int>(type: "int", nullable: false),
                    IsRecurring = table.Column<bool>(type: "bit", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    AssignedStaffId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fo_CuocGoiBaoThuc", x => x.CallId);
                });

            migrationBuilder.CreateTable(
                name: "DatXeConcierge",
                schema: "fo",
                columns: table => new
                {
                    BookingId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PropertyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BookingNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Type = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    GuestId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    GuestName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    StayId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RoomNumber = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    DriverName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    VehicleNumber = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    VehicleType = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    ScheduledAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PickupLocation = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    DropoffLocation = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    FlightNumber = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    PassengerCount = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Cost = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fo_DatXeConcierge", x => x.BookingId);
                });

            migrationBuilder.CreateTable(
                name: "HanhLyConcierge",
                schema: "fo",
                columns: table => new
                {
                    ItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PropertyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TagNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    GuestId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    GuestName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    StayId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RoomNumber = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    Pieces = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    StorageLocation = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    StoredAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DeliveredAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CollectedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    HandledBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    HandlerName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fo_HanhLyConcierge", x => x.ItemId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_fo_CuocGoiBaoThuc_CoSo_TrangThai_ThoiGian",
                schema: "fo",
                table: "CuocGoiBaoThuc",
                columns: new[] { "PropertyId", "Status", "WakeTime" });

            migrationBuilder.CreateIndex(
                name: "UQ_fo_CuocGoiBaoThuc_CoSo_SoGoi",
                schema: "fo",
                table: "CuocGoiBaoThuc",
                columns: new[] { "PropertyId", "CallNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_fo_DatXeConcierge_CoSo_TrangThai_ThoiGian",
                schema: "fo",
                table: "DatXeConcierge",
                columns: new[] { "PropertyId", "Status", "ScheduledAtUtc" });

            migrationBuilder.CreateIndex(
                name: "UQ_fo_DatXeConcierge_CoSo_SoDat",
                schema: "fo",
                table: "DatXeConcierge",
                columns: new[] { "PropertyId", "BookingNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_fo_HanhLyConcierge_CoSo_Khach",
                schema: "fo",
                table: "HanhLyConcierge",
                columns: new[] { "PropertyId", "GuestId" });

            migrationBuilder.CreateIndex(
                name: "IX_fo_HanhLyConcierge_CoSo_TrangThai_ThoiGian",
                schema: "fo",
                table: "HanhLyConcierge",
                columns: new[] { "PropertyId", "Status", "StoredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "UQ_fo_HanhLyConcierge_CoSo_MaThe",
                schema: "fo",
                table: "HanhLyConcierge",
                columns: new[] { "PropertyId", "TagNumber" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CuocGoiBaoThuc",
                schema: "fo");

            migrationBuilder.DropTable(
                name: "DatXeConcierge",
                schema: "fo");

            migrationBuilder.DropTable(
                name: "HanhLyConcierge",
                schema: "fo");
        }
    }
}
