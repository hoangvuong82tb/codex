using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ASMILE.BO.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddReservationEnterpriseFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateSequence(
                name: "TrinhTuSoCa",
                schema: "fo");

            migrationBuilder.CreateSequence(
                name: "TrinhTuSoDanhThuc",
                schema: "fo");

            migrationBuilder.CreateSequence(
                name: "TrinhTuSoDoan",
                schema: "fo");

            migrationBuilder.CreateSequence(
                name: "TrinhTuSoDonDep",
                schema: "fo");

            migrationBuilder.CreateSequence(
                name: "TrinhTuSoGia",
                schema: "fo");

            migrationBuilder.CreateSequence(
                name: "TrinhTuSoHoaDon",
                schema: "fo");

            migrationBuilder.CreateSequence(
                name: "TrinhTuSoKhach",
                schema: "fo");

            migrationBuilder.CreateSequence(
                name: "TrinhTuSoKiemDem",
                schema: "fo");

            migrationBuilder.CreateSequence(
                name: "TrinhTuSoThanhToan",
                schema: "fo");

            migrationBuilder.CreateSequence(
                name: "TrinhTuSoThatLac",
                schema: "fo");

            migrationBuilder.CreateSequence(
                name: "TrinhTuSoYeuCau",
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
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CompletedAtUtc",
                schema: "fo",
                table: "DatPhong",
                type: "datetime2",
                nullable: true);

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
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TravelAgentName",
                schema: "fo",
                table: "DatPhong",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CaLamViec",
                schema: "fo",
                columns: table => new
                {
                    CashierShiftId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PropertyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ShiftNumber = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    CashierId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    OpeningCash = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ClosingCash = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ActualCash = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    BusinessDate = table.Column<DateOnly>(type: "date", nullable: false),
                    OpenedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ClosedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ClosedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fo_CaLamViec", x => x.CashierShiftId);
                });

            migrationBuilder.CreateTable(
                name: "DanhThuc",
                schema: "fo",
                columns: table => new
                {
                    WakeUpCallId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PropertyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CallNumber = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    HotelStayId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    HotelRoomId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RoomNumber = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    GuestName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    WakeTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ActualCallTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Result = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    IsRecurring = table.Column<bool>(type: "bit", nullable: false),
                    IsDaily = table.Column<bool>(type: "bit", nullable: false),
                    RetryCount = table.Column<int>(type: "int", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    AssignedStaffId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fo_DanhThuc", x => x.WakeUpCallId);
                });

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
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Source = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    SalesManager = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    MarketSegment = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsVip = table.Column<bool>(type: "bit", nullable: false),
                    Routing = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    SpecialRequests = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    TotalDeposit = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalRevenue = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    OutstandingBalance = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
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
                name: "DonDep",
                schema: "fo",
                columns: table => new
                {
                    HousekeepingTaskId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PropertyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HotelRoomId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TaskNumber = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    TaskType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Priority = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    AssignedHousekeeperId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    InspectorNotes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    InspectionResult = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    StartedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    InspectedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CancelledAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fo_DonDep", x => x.HousekeepingTaskId);
                });

            migrationBuilder.CreateTable(
                name: "HoaDon",
                schema: "fo",
                columns: table => new
                {
                    HotelInvoiceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PropertyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HotelFolioId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HotelStayId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InvoiceNumber = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Subtotal = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TaxAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    PaidAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    OutstandingAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    GuestName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    RoomNumber = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    StayDates = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    InvoiceDataJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PrintedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fo_HoaDon", x => x.HotelInvoiceId);
                });

            migrationBuilder.CreateTable(
                name: "HoSoKhach",
                schema: "fo",
                columns: table => new
                {
                    GuestProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PropertyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GuestNumber = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Phone = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Address = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Nationality = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    Country = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    Gender = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    DateOfBirth = table.Column<DateOnly>(type: "date", nullable: true),
                    EmergencyContact = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    EmergencyPhone = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    VipLevel = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    TotalStays = table.Column<int>(type: "int", nullable: false),
                    TotalRevenue = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    LastStayDate = table.Column<DateOnly>(type: "date", nullable: true),
                    AverageSpend = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    IsBlacklisted = table.Column<bool>(type: "bit", nullable: false),
                    BlacklistReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    BlacklistedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BlacklistedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Source = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CompanyName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fo_HoSoKhach", x => x.GuestProfileId);
                });

            migrationBuilder.CreateTable(
                name: "KeHoachGia",
                schema: "fo",
                columns: table => new
                {
                    RatePlanId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PropertyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlanCode = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    PlanName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    RoomTypeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BaseRate = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Hierarchy = table.Column<int>(type: "int", nullable: false),
                    EffectiveFrom = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EffectiveTo = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AllowYieldManagement = table.Column<bool>(type: "bit", nullable: false),
                    YieldMinRate = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    YieldMaxRate = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    YieldThresholdOccupancy = table.Column<int>(type: "int", nullable: false),
                    YieldAdjustmentPct = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fo_KeHoachGia", x => x.RatePlanId);
                });

            migrationBuilder.CreateTable(
                name: "KiemDem",
                schema: "fo",
                columns: table => new
                {
                    NightAuditId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PropertyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AuditNumber = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    BusinessDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CurrentStep = table.Column<int>(type: "int", nullable: false),
                    ErrorMessage = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    StartedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    StartedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fo_KiemDem", x => x.NightAuditId);
                });

            migrationBuilder.CreateTable(
                name: "LostFoundItems",
                columns: table => new
                {
                    LostFoundItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PropertyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ItemNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ItemName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Category = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Color = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Brand = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SerialNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FoundLocation = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FoundDateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FoundByStaff = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    StoredLocation = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    OwnerGuestId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    OwnerGuestName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ReservationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    HotelStayId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ClaimedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReleasedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ReleasedTo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DisposalDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PhotoPath = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "varbinary(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LostFoundItems", x => x.LostFoundItemId);
                });

            migrationBuilder.CreateTable(
                name: "NgayKinhDoanh",
                schema: "fo",
                columns: table => new
                {
                    BusinessDateId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PropertyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BusinessDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    OpenedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    OpenedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ClosedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ClosedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    NightAuditId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fo_NgayKinhDoanh", x => x.BusinessDateId);
                });

            migrationBuilder.CreateTable(
                name: "ThanhToan",
                schema: "fo",
                columns: table => new
                {
                    HotelPaymentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PropertyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HotelFolioId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FolioEntryId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RefundEntryId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PaymentNumber = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    PaymentMethod = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ExternalReference = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    VoidedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    VoidedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    VoidReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    RefundedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RefundedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RefundReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fo_ThanhToan", x => x.HotelPaymentId);
                });

            migrationBuilder.CreateTable(
                name: "TrangThaiDonDep",
                schema: "fo",
                columns: table => new
                {
                    HotelRoomId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PropertyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    StatusChangedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fo_TrangThaiDonDep", x => x.HotelRoomId);
                });

            migrationBuilder.CreateTable(
                name: "YeuCauDichVu",
                schema: "fo",
                columns: table => new
                {
                    ServiceRequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PropertyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequestNumber = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    HotelStayId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    HotelRoomId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RoomNumber = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    GuestName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ServiceType = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Priority = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    AssignedDepartment = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    AssignedStaffId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    EstimatedCost = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    ActualCost = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    ChargeType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    FolioEntryId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RequestedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RequestedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fo_YeuCauDichVu", x => x.ServiceRequestId);
                });

            migrationBuilder.CreateTable(
                name: "BienDongTienMat",
                schema: "fo",
                columns: table => new
                {
                    CashMovementId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CashierShiftId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PropertyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    PaymentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fo_BienDongTienMat", x => x.CashMovementId);
                    table.ForeignKey(
                        name: "FK_BienDongTienMat_CaLamViec_CashierShiftId",
                        column: x => x.CashierShiftId,
                        principalSchema: "fo",
                        principalTable: "CaLamViec",
                        principalColumn: "CashierShiftId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LichSuDanhThuc",
                schema: "fo",
                columns: table => new
                {
                    WakeUpCallHistoryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WakeUpCallId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PropertyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Action = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    OccurredAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fo_LichSuDanhThuc", x => x.WakeUpCallHistoryId);
                    table.ForeignKey(
                        name: "FK_LichSuDanhThuc_DanhThuc_WakeUpCallId",
                        column: x => x.WakeUpCallId,
                        principalSchema: "fo",
                        principalTable: "DanhThuc",
                        principalColumn: "WakeUpCallId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PhanPhongDoan",
                schema: "fo",
                columns: table => new
                {
                    GroupRoomAssignmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GroupReservationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PropertyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HotelRoomId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RoomNumber = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    RoomType = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    GuestId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    GuestName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    IsVip = table.Column<bool>(type: "bit", nullable: false),
                    Adults = table.Column<int>(type: "int", nullable: false),
                    Children = table.Column<int>(type: "int", nullable: false),
                    IsCheckedIn = table.Column<bool>(type: "bit", nullable: false),
                    IsCheckedOut = table.Column<bool>(type: "bit", nullable: false),
                    ReservationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fo_PhanPhongDoan", x => x.GroupRoomAssignmentId);
                    table.ForeignKey(
                        name: "FK_PhanPhongDoan_DatPhongDoan_GroupReservationId",
                        column: x => x.GroupReservationId,
                        principalSchema: "fo",
                        principalTable: "DatPhongDoan",
                        principalColumn: "GroupReservationId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "GiayToKhach",
                schema: "fo",
                columns: table => new
                {
                    GuestDocumentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GuestProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PropertyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DocumentType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    DocumentNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IssuingCountry = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    ExpiryDate = table.Column<DateOnly>(type: "date", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fo_GiayToKhach", x => x.GuestDocumentId);
                    table.ForeignKey(
                        name: "FK_GiayToKhach_HoSoKhach_GuestProfileId",
                        column: x => x.GuestProfileId,
                        principalSchema: "fo",
                        principalTable: "HoSoKhach",
                        principalColumn: "GuestProfileId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SoThichKhach",
                schema: "fo",
                columns: table => new
                {
                    GuestPreferenceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GuestProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PropertyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Key = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Value = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fo_SoThichKhach", x => x.GuestPreferenceId);
                    table.ForeignKey(
                        name: "FK_SoThichKhach_HoSoKhach_GuestProfileId",
                        column: x => x.GuestProfileId,
                        principalSchema: "fo",
                        principalTable: "HoSoKhach",
                        principalColumn: "GuestProfileId",
                        onDelete: ReferentialAction.Restrict);
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
                    Rate = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
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
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MuaGia",
                schema: "fo",
                columns: table => new
                {
                    RateSeasonId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RatePlanId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PropertyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    FromDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ToDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Multiplier = table.Column<decimal>(type: "decimal(9,4)", precision: 9, scale: 4, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
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
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "QuyTacGia",
                schema: "fo",
                columns: table => new
                {
                    RateRuleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RatePlanId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PropertyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Value = table.Column<decimal>(type: "decimal(9,4)", precision: 9, scale: 4, nullable: false),
                    Priority = table.Column<int>(type: "int", nullable: false),
                    Exclusive = table.Column<bool>(type: "bit", nullable: false),
                    Combinable = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiryDate = table.Column<DateTime>(type: "datetime2", nullable: true)
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
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BuocKiemDem",
                schema: "fo",
                columns: table => new
                {
                    NightAuditStepExecutionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NightAuditId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PropertyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Step = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Detail = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CompletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fo_BuocKiemDem", x => x.NightAuditStepExecutionId);
                    table.ForeignKey(
                        name: "FK_BuocKiemDem_KiemDem_NightAuditId",
                        column: x => x.NightAuditId,
                        principalSchema: "fo",
                        principalTable: "KiemDem",
                        principalColumn: "NightAuditId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DoanhThuNgay",
                schema: "fo",
                columns: table => new
                {
                    RevenueSnapshotId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PropertyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NightAuditId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BusinessDate = table.Column<DateOnly>(type: "date", nullable: false),
                    RoomRevenue = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    OtherRevenue = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalRevenue = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalTax = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalPayments = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalRefunds = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    OutstandingBalance = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    RoomsSold = table.Column<int>(type: "int", nullable: false),
                    TotalRooms = table.Column<int>(type: "int", nullable: false),
                    OccupancyRate = table.Column<decimal>(type: "decimal(9,4)", precision: 9, scale: 4, nullable: false),
                    ADR = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    RevPAR = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Arrivals = table.Column<int>(type: "int", nullable: false),
                    Departures = table.Column<int>(type: "int", nullable: false),
                    NoShows = table.Column<int>(type: "int", nullable: false),
                    Cancellations = table.Column<int>(type: "int", nullable: false),
                    SnapshotJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fo_DoanhThuNgay", x => x.RevenueSnapshotId);
                    table.ForeignKey(
                        name: "FK_DoanhThuNgay_KiemDem_NightAuditId",
                        column: x => x.NightAuditId,
                        principalSchema: "fo",
                        principalTable: "KiemDem",
                        principalColumn: "NightAuditId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LostFoundHistories",
                columns: table => new
                {
                    LostFoundHistoryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LostFoundItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PropertyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Action = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OccurredAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LostFoundHistories", x => x.LostFoundHistoryId);
                    table.ForeignKey(
                        name: "FK_LostFoundHistories_LostFoundItems_LostFoundItemId",
                        column: x => x.LostFoundItemId,
                        principalTable: "LostFoundItems",
                        principalColumn: "LostFoundItemId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PhanBoThanhToan",
                schema: "fo",
                columns: table => new
                {
                    HotelPaymentAllocationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HotelPaymentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PropertyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HotelFolioId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ChargeFolioEntryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ChargeDescription = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fo_PhanBoThanhToan", x => x.HotelPaymentAllocationId);
                    table.ForeignKey(
                        name: "FK_PhanBoThanhToan_ThanhToan_HotelPaymentId",
                        column: x => x.HotelPaymentId,
                        principalSchema: "fo",
                        principalTable: "ThanhToan",
                        principalColumn: "HotelPaymentId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LichSuYeuCau",
                schema: "fo",
                columns: table => new
                {
                    ServiceHistoryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ServiceRequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PropertyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Action = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Detail = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    OccurredAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fo_LichSuYeuCau", x => x.ServiceHistoryId);
                    table.ForeignKey(
                        name: "FK_LichSuYeuCau_YeuCauDichVu_ServiceRequestId",
                        column: x => x.ServiceRequestId,
                        principalSchema: "fo",
                        principalTable: "YeuCauDichVu",
                        principalColumn: "ServiceRequestId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_fo_DatPhong_CoSo_PhanKhuc",
                schema: "fo",
                table: "DatPhong",
                columns: new[] { "PropertyId", "MarketSegment" });

            migrationBuilder.CreateIndex(
                name: "IX_fo_BienDongTienMat_Ca_ThoiGian",
                schema: "fo",
                table: "BienDongTienMat",
                columns: new[] { "CashierShiftId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "UQ_fo_BuocKiemDem_Audit_Step",
                schema: "fo",
                table: "BuocKiemDem",
                columns: new[] { "NightAuditId", "Step" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_fo_CaLamViec_CoSo_ThuNgan_TrangThai",
                schema: "fo",
                table: "CaLamViec",
                columns: new[] { "PropertyId", "CashierId", "Status" });

            migrationBuilder.CreateIndex(
                name: "UQ_fo_CaLamViec_SoCa",
                schema: "fo",
                table: "CaLamViec",
                column: "ShiftNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_fo_DanhThuc_CoSo_Gio_TrangThai",
                schema: "fo",
                table: "DanhThuc",
                columns: new[] { "PropertyId", "WakeTime", "Status" });

            migrationBuilder.CreateIndex(
                name: "UQ_fo_DanhThuc_SoDanhThuc",
                schema: "fo",
                table: "DanhThuc",
                column: "CallNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_fo_DatPhongDoan_CoSo_NgayDen",
                schema: "fo",
                table: "DatPhongDoan",
                columns: new[] { "PropertyId", "ArrivalDate" });

            migrationBuilder.CreateIndex(
                name: "UQ_fo_DatPhongDoan_MaDoan",
                schema: "fo",
                table: "DatPhongDoan",
                column: "GroupCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DoanhThuNgay_NightAuditId",
                schema: "fo",
                table: "DoanhThuNgay",
                column: "NightAuditId");

            migrationBuilder.CreateIndex(
                name: "UQ_fo_DoanhThuNgay_CoSo_Ngay",
                schema: "fo",
                table: "DoanhThuNgay",
                columns: new[] { "PropertyId", "BusinessDate" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_fo_DonDep_CoSo_Phong_TrangThai",
                schema: "fo",
                table: "DonDep",
                columns: new[] { "PropertyId", "HotelRoomId", "Status" });

            migrationBuilder.CreateIndex(
                name: "UQ_fo_DonDep_SoDonDep",
                schema: "fo",
                table: "DonDep",
                column: "TaskNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_fo_GiayToKhach_CoSo_Loai_So",
                schema: "fo",
                table: "GiayToKhach",
                columns: new[] { "PropertyId", "DocumentType", "DocumentNumber" });

            migrationBuilder.CreateIndex(
                name: "IX_GiayToKhach_GuestProfileId",
                schema: "fo",
                table: "GiayToKhach",
                column: "GuestProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_fo_HoaDon_CoSo_Folio",
                schema: "fo",
                table: "HoaDon",
                columns: new[] { "PropertyId", "HotelFolioId" });

            migrationBuilder.CreateIndex(
                name: "UQ_fo_HoaDon_SoHoaDon",
                schema: "fo",
                table: "HoaDon",
                column: "InvoiceNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_fo_HoSoKhach_CoSo_Ten",
                schema: "fo",
                table: "HoSoKhach",
                columns: new[] { "PropertyId", "FullName" });

            migrationBuilder.CreateIndex(
                name: "IX_fo_HoSoKhach_Email",
                schema: "fo",
                table: "HoSoKhach",
                column: "Email");

            migrationBuilder.CreateIndex(
                name: "IX_fo_HoSoKhach_Phone",
                schema: "fo",
                table: "HoSoKhach",
                column: "Phone");

            migrationBuilder.CreateIndex(
                name: "UQ_fo_HoSoKhach_SoKhach",
                schema: "fo",
                table: "HoSoKhach",
                column: "GuestNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_fo_KeHoachGia_MaKeHoach",
                schema: "fo",
                table: "KeHoachGia",
                column: "PlanCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_fo_KiemDem_CoSo_Ngay",
                schema: "fo",
                table: "KiemDem",
                columns: new[] { "PropertyId", "BusinessDate" });

            migrationBuilder.CreateIndex(
                name: "UQ_fo_KiemDem_SoKiemDem",
                schema: "fo",
                table: "KiemDem",
                column: "AuditNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_fo_LichGia_KeHoach_Ngay",
                schema: "fo",
                table: "LichGia",
                columns: new[] { "RatePlanId", "Date" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_fo_LichSuDanhThuc_Call_ThoiGian",
                schema: "fo",
                table: "LichSuDanhThuc",
                columns: new[] { "WakeUpCallId", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_fo_LichSuYeuCau_YeuCau_ThoiGian",
                schema: "fo",
                table: "LichSuYeuCau",
                columns: new[] { "ServiceRequestId", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_LostFoundHistories_LostFoundItemId",
                table: "LostFoundHistories",
                column: "LostFoundItemId");

            migrationBuilder.CreateIndex(
                name: "IX_fo_MuaGia_KeHoach_ThoiGian",
                schema: "fo",
                table: "MuaGia",
                columns: new[] { "RatePlanId", "FromDate", "ToDate" });

            migrationBuilder.CreateIndex(
                name: "UQ_fo_NgayKinhDoanh_CoSo_Ngay",
                schema: "fo",
                table: "NgayKinhDoanh",
                columns: new[] { "PropertyId", "BusinessDate" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_fo_PhanBoThanhToan_CoSo_Folio",
                schema: "fo",
                table: "PhanBoThanhToan",
                columns: new[] { "PropertyId", "HotelFolioId" });

            migrationBuilder.CreateIndex(
                name: "UQ_fo_PhanBoThanhToan_Payment_Charge",
                schema: "fo",
                table: "PhanBoThanhToan",
                columns: new[] { "HotelPaymentId", "ChargeFolioEntryId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_fo_PhanPhongDoan_Doan_Phong",
                schema: "fo",
                table: "PhanPhongDoan",
                columns: new[] { "GroupReservationId", "HotelRoomId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_fo_QuyTacGia_KeHoach_HoatDong",
                schema: "fo",
                table: "QuyTacGia",
                columns: new[] { "RatePlanId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "UQ_fo_SoThichKhach_Khach_Key",
                schema: "fo",
                table: "SoThichKhach",
                columns: new[] { "GuestProfileId", "Key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_fo_ThanhToan_CoSo_Folio_ThoiGian",
                schema: "fo",
                table: "ThanhToan",
                columns: new[] { "PropertyId", "HotelFolioId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_fo_ThanhToan_CoSo_TrangThai",
                schema: "fo",
                table: "ThanhToan",
                columns: new[] { "PropertyId", "Status" });

            migrationBuilder.CreateIndex(
                name: "UQ_fo_ThanhToan_SoThanhToan",
                schema: "fo",
                table: "ThanhToan",
                column: "PaymentNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_fo_TrangThaiDonDep_CoSo_TrangThai",
                schema: "fo",
                table: "TrangThaiDonDep",
                columns: new[] { "PropertyId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_fo_YeuCauDichVu_CoSo_TrangThai",
                schema: "fo",
                table: "YeuCauDichVu",
                columns: new[] { "PropertyId", "Status" });

            migrationBuilder.CreateIndex(
                name: "UQ_fo_YeuCauDichVu_SoYeuCau",
                schema: "fo",
                table: "YeuCauDichVu",
                column: "RequestNumber",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BienDongTienMat",
                schema: "fo");

            migrationBuilder.DropTable(
                name: "BuocKiemDem",
                schema: "fo");

            migrationBuilder.DropTable(
                name: "DoanhThuNgay",
                schema: "fo");

            migrationBuilder.DropTable(
                name: "DonDep",
                schema: "fo");

            migrationBuilder.DropTable(
                name: "GiayToKhach",
                schema: "fo");

            migrationBuilder.DropTable(
                name: "HoaDon",
                schema: "fo");

            migrationBuilder.DropTable(
                name: "LichGia",
                schema: "fo");

            migrationBuilder.DropTable(
                name: "LichSuDanhThuc",
                schema: "fo");

            migrationBuilder.DropTable(
                name: "LichSuYeuCau",
                schema: "fo");

            migrationBuilder.DropTable(
                name: "LostFoundHistories");

            migrationBuilder.DropTable(
                name: "MuaGia",
                schema: "fo");

            migrationBuilder.DropTable(
                name: "NgayKinhDoanh",
                schema: "fo");

            migrationBuilder.DropTable(
                name: "PhanBoThanhToan",
                schema: "fo");

            migrationBuilder.DropTable(
                name: "PhanPhongDoan",
                schema: "fo");

            migrationBuilder.DropTable(
                name: "QuyTacGia",
                schema: "fo");

            migrationBuilder.DropTable(
                name: "SoThichKhach",
                schema: "fo");

            migrationBuilder.DropTable(
                name: "TrangThaiDonDep",
                schema: "fo");

            migrationBuilder.DropTable(
                name: "CaLamViec",
                schema: "fo");

            migrationBuilder.DropTable(
                name: "KiemDem",
                schema: "fo");

            migrationBuilder.DropTable(
                name: "DanhThuc",
                schema: "fo");

            migrationBuilder.DropTable(
                name: "YeuCauDichVu",
                schema: "fo");

            migrationBuilder.DropTable(
                name: "LostFoundItems");

            migrationBuilder.DropTable(
                name: "ThanhToan",
                schema: "fo");

            migrationBuilder.DropTable(
                name: "DatPhongDoan",
                schema: "fo");

            migrationBuilder.DropTable(
                name: "KeHoachGia",
                schema: "fo");

            migrationBuilder.DropTable(
                name: "HoSoKhach",
                schema: "fo");

            migrationBuilder.DropIndex(
                name: "IX_fo_DatPhong_CoSo_PhanKhuc",
                schema: "fo",
                table: "DatPhong");

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
                name: "TrinhTuSoCa",
                schema: "fo");

            migrationBuilder.DropSequence(
                name: "TrinhTuSoDanhThuc",
                schema: "fo");

            migrationBuilder.DropSequence(
                name: "TrinhTuSoDoan",
                schema: "fo");

            migrationBuilder.DropSequence(
                name: "TrinhTuSoDonDep",
                schema: "fo");

            migrationBuilder.DropSequence(
                name: "TrinhTuSoGia",
                schema: "fo");

            migrationBuilder.DropSequence(
                name: "TrinhTuSoHoaDon",
                schema: "fo");

            migrationBuilder.DropSequence(
                name: "TrinhTuSoKhach",
                schema: "fo");

            migrationBuilder.DropSequence(
                name: "TrinhTuSoKiemDem",
                schema: "fo");

            migrationBuilder.DropSequence(
                name: "TrinhTuSoThanhToan",
                schema: "fo");

            migrationBuilder.DropSequence(
                name: "TrinhTuSoThatLac",
                schema: "fo");

            migrationBuilder.DropSequence(
                name: "TrinhTuSoYeuCau",
                schema: "fo");
        }
    }
}
