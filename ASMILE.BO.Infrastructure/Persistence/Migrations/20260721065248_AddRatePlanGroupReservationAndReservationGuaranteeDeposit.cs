using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ASMILE.BO.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRatePlanGroupReservationAndReservationGuaranteeDeposit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateSequence(
                name: "TrinhTuSoLuuTru",
                schema: "fo");

            migrationBuilder.AddColumn<DateTime>(
                name: "CheckedInAtUtc",
                schema: "fo",
                table: "DatPhong",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CompanyName",
                schema: "fo",
                table: "DatPhong",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CompletedAtUtc",
                schema: "fo",
                table: "DatPhong",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "DepositAmount",
                schema: "fo",
                table: "DatPhong",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<DateOnly>(
                name: "DepositDueDate",
                schema: "fo",
                table: "DatPhong",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DepositStatus",
                schema: "fo",
                table: "DatPhong",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "GuaranteeExpiryDate",
                schema: "fo",
                table: "DatPhong",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GuaranteeType",
                schema: "fo",
                table: "DatPhong",
                type: "nvarchar(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "GuaranteedAtUtc",
                schema: "fo",
                table: "DatPhong",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsVip",
                schema: "fo",
                table: "DatPhong",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "MarketSegment",
                schema: "fo",
                table: "DatPhong",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TravelAgentName",
                schema: "fo",
                table: "DatPhong",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "DatPhongDoan",
                schema: "fo",
                columns: table => new
                {
                    GroupReservationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PropertyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GroupCode = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    GroupName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CompanyName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    AgencyName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ContactPerson = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ContactPhone = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ContactEmail = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ArrivalDate = table.Column<DateOnly>(type: "date", nullable: false),
                    DepartureDate = table.Column<DateOnly>(type: "date", nullable: false),
                    ExpectedRooms = table.Column<int>(type: "int", nullable: false),
                    ConfirmedRooms = table.Column<int>(type: "int", nullable: false),
                    TotalAdults = table.Column<int>(type: "int", nullable: false),
                    TotalChildren = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Source = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    SalesManager = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    MarketSegment = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    IsVip = table.Column<bool>(type: "bit", nullable: false),
                    Routing = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    SpecialRequests = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    TotalDeposit = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalRevenue = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    OutstandingBalance = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fo_DatPhongDoan", x => x.GroupReservationId);
                });

            migrationBuilder.CreateTable(
                name: "HotelFolios",
                columns: table => new
                {
                    HotelFolioId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PropertyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HotelStayId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FolioNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ClosedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ClosedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SettledAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SettledBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ArchivedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "varbinary(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HotelFolios", x => x.HotelFolioId);
                });

            migrationBuilder.CreateTable(
                name: "KeHoachGia",
                schema: "fo",
                columns: table => new
                {
                    RatePlanId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PropertyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlanCode = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    PlanName = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    RoomTypeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BaseRate = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Hierarchy = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    EffectiveFrom = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EffectiveTo = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AllowYieldManagement = table.Column<bool>(type: "bit", nullable: false),
                    YieldMinRate = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    YieldMaxRate = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    YieldThresholdOccupancy = table.Column<int>(type: "int", nullable: false),
                    YieldAdjustmentPct = table.Column<decimal>(type: "decimal(8,4)", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fo_KeHoachGia", x => x.RatePlanId);
                });

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
                    PreArrivalConfirmedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LateCheckoutRequestedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LateCheckoutUntilUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Adults = table.Column<int>(type: "int", nullable: false),
                    Children = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Source = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    InternalNotes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ExtensionCount = table.Column<int>(type: "int", nullable: false),
                    OriginalDepartureDate = table.Column<DateOnly>(type: "date", nullable: true),
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
                name: "PhongDoan",
                schema: "fo",
                columns: table => new
                {
                    GroupRoomAssignmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GroupReservationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PropertyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HotelRoomId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RoomNumber = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    RoomType = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    GuestId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    GuestName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    IsVip = table.Column<bool>(type: "bit", nullable: false),
                    Adults = table.Column<int>(type: "int", nullable: false),
                    Children = table.Column<int>(type: "int", nullable: false),
                    IsCheckedIn = table.Column<bool>(type: "bit", nullable: false),
                    IsCheckedOut = table.Column<bool>(type: "bit", nullable: false),
                    ReservationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    GroupReservationId1 = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fo_PhongDoan", x => x.GroupRoomAssignmentId);
                    table.ForeignKey(
                        name: "FK_PhongDoan_DatPhongDoan_GroupReservationId",
                        column: x => x.GroupReservationId,
                        principalSchema: "fo",
                        principalTable: "DatPhongDoan",
                        principalColumn: "GroupReservationId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PhongDoan_DatPhongDoan_GroupReservationId1",
                        column: x => x.GroupReservationId1,
                        principalSchema: "fo",
                        principalTable: "DatPhongDoan",
                        principalColumn: "GroupReservationId");
                });

            migrationBuilder.CreateTable(
                name: "HotelFolioEntries",
                columns: table => new
                {
                    HotelFolioEntryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HotelFolioId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PropertyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SequenceNumber = table.Column<int>(type: "int", nullable: false),
                    EntryType = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ReferenceType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ReferenceId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BusinessDate = table.Column<DateOnly>(type: "date", nullable: true),
                    ReversesEntryId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ReversedByEntryId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HotelFolioEntries", x => x.HotelFolioEntryId);
                    table.ForeignKey(
                        name: "FK_HotelFolioEntries_HotelFolios_HotelFolioId",
                        column: x => x.HotelFolioId,
                        principalTable: "HotelFolios",
                        principalColumn: "HotelFolioId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LichGia",
                schema: "fo",
                columns: table => new
                {
                    RateCalendarEntryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RatePlanId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PropertyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    Rate = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RatePlanId1 = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fo_LichGia", x => x.RateCalendarEntryId);
                    table.ForeignKey(
                        name: "FK_LichGia_KeHoachGia_RatePlanId",
                        column: x => x.RatePlanId,
                        principalSchema: "fo",
                        principalTable: "KeHoachGia",
                        principalColumn: "RatePlanId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LichGia_KeHoachGia_RatePlanId1",
                        column: x => x.RatePlanId1,
                        principalSchema: "fo",
                        principalTable: "KeHoachGia",
                        principalColumn: "RatePlanId");
                });

            migrationBuilder.CreateTable(
                name: "MuaGia",
                schema: "fo",
                columns: table => new
                {
                    RateSeasonId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RatePlanId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PropertyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    FromDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ToDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Multiplier = table.Column<decimal>(type: "decimal(8,4)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    RatePlanId1 = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fo_MuaGia", x => x.RateSeasonId);
                    table.ForeignKey(
                        name: "FK_MuaGia_KeHoachGia_RatePlanId",
                        column: x => x.RatePlanId,
                        principalSchema: "fo",
                        principalTable: "KeHoachGia",
                        principalColumn: "RatePlanId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MuaGia_KeHoachGia_RatePlanId1",
                        column: x => x.RatePlanId1,
                        principalSchema: "fo",
                        principalTable: "KeHoachGia",
                        principalColumn: "RatePlanId");
                });

            migrationBuilder.CreateTable(
                name: "QuyTacGia",
                schema: "fo",
                columns: table => new
                {
                    RateRuleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RatePlanId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PropertyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Value = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    Priority = table.Column<int>(type: "int", nullable: false),
                    Exclusive = table.Column<bool>(type: "bit", nullable: false),
                    Combinable = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiryDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RatePlanId1 = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fo_QuyTacGia", x => x.RateRuleId);
                    table.ForeignKey(
                        name: "FK_QuyTacGia_KeHoachGia_RatePlanId",
                        column: x => x.RatePlanId,
                        principalSchema: "fo",
                        principalTable: "KeHoachGia",
                        principalColumn: "RatePlanId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_QuyTacGia_KeHoachGia_RatePlanId1",
                        column: x => x.RatePlanId1,
                        principalSchema: "fo",
                        principalTable: "KeHoachGia",
                        principalColumn: "RatePlanId");
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
                name: "IX_fo_DatPhongDoan_CoSo_NgayO",
                schema: "fo",
                table: "DatPhongDoan",
                columns: new[] { "PropertyId", "ArrivalDate", "DepartureDate" });

            migrationBuilder.CreateIndex(
                name: "IX_fo_DatPhongDoan_CoSo_TrangThai",
                schema: "fo",
                table: "DatPhongDoan",
                columns: new[] { "PropertyId", "Status" });

            migrationBuilder.CreateIndex(
                name: "UQ_fo_DatPhongDoan_CoSo_MaDoan",
                schema: "fo",
                table: "DatPhongDoan",
                columns: new[] { "PropertyId", "GroupCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_HotelFolioEntries_HotelFolioId",
                table: "HotelFolioEntries",
                column: "HotelFolioId");

            migrationBuilder.CreateIndex(
                name: "IX_fo_KeHoachGia_CoSo_TrangThai",
                schema: "fo",
                table: "KeHoachGia",
                columns: new[] { "PropertyId", "Status" });

            migrationBuilder.CreateIndex(
                name: "UQ_fo_KeHoachGia_CoSo_Ma",
                schema: "fo",
                table: "KeHoachGia",
                columns: new[] { "PropertyId", "PlanCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LichGia_RatePlanId1",
                schema: "fo",
                table: "LichGia",
                column: "RatePlanId1");

            migrationBuilder.CreateIndex(
                name: "UQ_fo_LichGia_KeHoach_Ngay",
                schema: "fo",
                table: "LichGia",
                columns: new[] { "RatePlanId", "Date" },
                unique: true);

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

            migrationBuilder.CreateIndex(
                name: "IX_fo_MuaGia_KeHoach_Ngay",
                schema: "fo",
                table: "MuaGia",
                columns: new[] { "RatePlanId", "FromDate", "ToDate" });

            migrationBuilder.CreateIndex(
                name: "IX_MuaGia_RatePlanId1",
                schema: "fo",
                table: "MuaGia",
                column: "RatePlanId1");

            migrationBuilder.CreateIndex(
                name: "IX_PhongDoan_GroupReservationId1",
                schema: "fo",
                table: "PhongDoan",
                column: "GroupReservationId1");

            migrationBuilder.CreateIndex(
                name: "UQ_fo_PhongDoan_Doan_Phong",
                schema: "fo",
                table: "PhongDoan",
                columns: new[] { "GroupReservationId", "HotelRoomId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_fo_QuyTacGia_KeHoach_UuTien",
                schema: "fo",
                table: "QuyTacGia",
                columns: new[] { "RatePlanId", "Priority" });

            migrationBuilder.CreateIndex(
                name: "IX_QuyTacGia_RatePlanId1",
                schema: "fo",
                table: "QuyTacGia",
                column: "RatePlanId1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HotelFolioEntries");

            migrationBuilder.DropTable(
                name: "LichGia",
                schema: "fo");

            migrationBuilder.DropTable(
                name: "LichSuLuuTruKhachSan",
                schema: "fo");

            migrationBuilder.DropTable(
                name: "MuaGia",
                schema: "fo");

            migrationBuilder.DropTable(
                name: "PhongDoan",
                schema: "fo");

            migrationBuilder.DropTable(
                name: "QuyTacGia",
                schema: "fo");

            migrationBuilder.DropTable(
                name: "HotelFolios");

            migrationBuilder.DropTable(
                name: "LuuTruKhachSan",
                schema: "fo");

            migrationBuilder.DropTable(
                name: "DatPhongDoan",
                schema: "fo");

            migrationBuilder.DropTable(
                name: "KeHoachGia",
                schema: "fo");

            migrationBuilder.DropColumn(
                name: "CheckedInAtUtc",
                schema: "fo",
                table: "DatPhong");

            migrationBuilder.DropColumn(
                name: "CompanyName",
                schema: "fo",
                table: "DatPhong");

            migrationBuilder.DropColumn(
                name: "CompletedAtUtc",
                schema: "fo",
                table: "DatPhong");

            migrationBuilder.DropColumn(
                name: "DepositAmount",
                schema: "fo",
                table: "DatPhong");

            migrationBuilder.DropColumn(
                name: "DepositDueDate",
                schema: "fo",
                table: "DatPhong");

            migrationBuilder.DropColumn(
                name: "DepositStatus",
                schema: "fo",
                table: "DatPhong");

            migrationBuilder.DropColumn(
                name: "GuaranteeExpiryDate",
                schema: "fo",
                table: "DatPhong");

            migrationBuilder.DropColumn(
                name: "GuaranteeType",
                schema: "fo",
                table: "DatPhong");

            migrationBuilder.DropColumn(
                name: "GuaranteedAtUtc",
                schema: "fo",
                table: "DatPhong");

            migrationBuilder.DropColumn(
                name: "IsVip",
                schema: "fo",
                table: "DatPhong");

            migrationBuilder.DropColumn(
                name: "MarketSegment",
                schema: "fo",
                table: "DatPhong");

            migrationBuilder.DropColumn(
                name: "TravelAgentName",
                schema: "fo",
                table: "DatPhong");

            migrationBuilder.DropSequence(
                name: "TrinhTuSoLuuTru",
                schema: "fo");
        }
    }
}
