using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ASMILE.BO.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRestaurantAlacarteAndBanquetModules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BanquetMenuPackages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    PricingMode = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Price = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    VatRate = table.Column<decimal>(type: "decimal(9,4)", precision: 9, scale: 4, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BanquetMenuPackages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BanquetRooms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Capacity = table.Column<int>(type: "int", nullable: false),
                    MaxTables = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BanquetRooms", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DiningRooms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    RoomType = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Capacity = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DiningRooms", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RestaurantContracts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ContractNo = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    CustomerId = table.Column<int>(type: "int", nullable: true),
                    EventDescription = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    DepositAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    RemainingAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    SignedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Note = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RestaurantContracts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RestaurantContracts_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "RestaurantExtraServices",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    ServiceType = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    DefaultPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    VatRate = table.Column<decimal>(type: "decimal(9,4)", precision: 9, scale: 4, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RestaurantExtraServices", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BanquetMenuPackageLines",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BanquetMenuPackageId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MenuItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CourseType = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    LineNo = table.Column<int>(type: "int", nullable: false),
                    Qty = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BanquetMenuPackageLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BanquetMenuPackageLines_BanquetMenuPackages_BanquetMenuPackageId",
                        column: x => x.BanquetMenuPackageId,
                        principalTable: "BanquetMenuPackages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BanquetMenuPackageLines_MenuItems_MenuItemId",
                        column: x => x.MenuItemId,
                        principalTable: "MenuItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BanquetEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    EventNo = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    EventType = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    CustomerId = table.Column<int>(type: "int", nullable: true),
                    CustomerName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    Phone = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    EventDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    StartTime = table.Column<TimeSpan>(type: "time", nullable: false),
                    EndTime = table.Column<TimeSpan>(type: "time", nullable: false),
                    GuestCount = table.Column<int>(type: "int", nullable: false),
                    TableCount = table.Column<int>(type: "int", nullable: false),
                    BanquetRoomId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Note = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BanquetEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BanquetEvents_BanquetRooms_BanquetRoomId",
                        column: x => x.BanquetRoomId,
                        principalTable: "BanquetRooms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_BanquetEvents_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "DiningTables",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DiningRoomId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    SeatCount = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Note = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DiningTables", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DiningTables_DiningRooms_DiningRoomId",
                        column: x => x.DiningRoomId,
                        principalTable: "DiningRooms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BanquetChecklists",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BanquetEventId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    IsDone = table.Column<bool>(type: "bit", nullable: false),
                    DoneAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DoneBy = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BanquetChecklists", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BanquetChecklists_BanquetEvents_BanquetEventId",
                        column: x => x.BanquetEventId,
                        principalTable: "BanquetEvents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BanquetContracts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ContractNo = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    BanquetEventId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SignDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CustomerId = table.Column<int>(type: "int", nullable: true),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    DepositAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    RemainingAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Note = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BanquetContracts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BanquetContracts_BanquetEvents_BanquetEventId",
                        column: x => x.BanquetEventId,
                        principalTable: "BanquetEvents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BanquetContracts_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "BanquetEventMenus",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BanquetEventId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BanquetMenuPackageId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PricingMode = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    TableCount = table.Column<int>(type: "int", nullable: false),
                    GuestCount = table.Column<int>(type: "int", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BanquetEventMenus", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BanquetEventMenus_BanquetEvents_BanquetEventId",
                        column: x => x.BanquetEventId,
                        principalTable: "BanquetEvents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BanquetEventMenus_BanquetMenuPackages_BanquetMenuPackageId",
                        column: x => x.BanquetMenuPackageId,
                        principalTable: "BanquetMenuPackages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "BanquetFloorPlans",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BanquetEventId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    CanvasWidth = table.Column<double>(type: "float", nullable: false),
                    CanvasHeight = table.Column<double>(type: "float", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BanquetFloorPlans", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BanquetFloorPlans_BanquetEvents_BanquetEventId",
                        column: x => x.BanquetEventId,
                        principalTable: "BanquetEvents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BanquetServiceLines",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BanquetEventId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ServiceType = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Qty = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    VatRate = table.Column<decimal>(type: "decimal(9,4)", precision: 9, scale: 4, nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    SupplierId = table.Column<int>(type: "int", nullable: true),
                    Note = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BanquetServiceLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BanquetServiceLines_BanquetEvents_BanquetEventId",
                        column: x => x.BanquetEventId,
                        principalTable: "BanquetEvents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BanquetServiceLines_Suppliers_SupplierId",
                        column: x => x.SupplierId,
                        principalTable: "Suppliers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "BanquetTimelines",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BanquetEventId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StartTime = table.Column<TimeSpan>(type: "time", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ResponsiblePerson = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BanquetTimelines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BanquetTimelines_BanquetEvents_BanquetEventId",
                        column: x => x.BanquetEventId,
                        principalTable: "BanquetEvents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TableReservations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ReservationNo = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    CustomerId = table.Column<int>(type: "int", nullable: true),
                    CustomerName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    Phone = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    ReservationDateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    GuestCount = table.Column<int>(type: "int", nullable: false),
                    DiningRoomId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DiningTableId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Note = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TableReservations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TableReservations_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_TableReservations_DiningRooms_DiningRoomId",
                        column: x => x.DiningRoomId,
                        principalTable: "DiningRooms",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TableReservations_DiningTables_DiningTableId",
                        column: x => x.DiningTableId,
                        principalTable: "DiningTables",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "BanquetDeposits",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DepositNo = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    BanquetEventId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BanquetContractId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DepositDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    PaymentMethod = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    CashFundId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BankAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Receiver = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    Note = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BanquetDeposits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BanquetDeposits_BankAccounts_BankAccountId",
                        column: x => x.BankAccountId,
                        principalTable: "BankAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_BanquetDeposits_BanquetContracts_BanquetContractId",
                        column: x => x.BanquetContractId,
                        principalTable: "BanquetContracts",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_BanquetDeposits_BanquetEvents_BanquetEventId",
                        column: x => x.BanquetEventId,
                        principalTable: "BanquetEvents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BanquetDeposits_CashFunds_CashFundId",
                        column: x => x.CashFundId,
                        principalTable: "CashFunds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "BanquetFloorPlanTables",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BanquetFloorPlanId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TableNo = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    TableType = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    SeatCount = table.Column<int>(type: "int", nullable: false),
                    X = table.Column<double>(type: "float", nullable: false),
                    Y = table.Column<double>(type: "float", nullable: false),
                    Width = table.Column<double>(type: "float", nullable: false),
                    Height = table.Column<double>(type: "float", nullable: false),
                    Rotation = table.Column<double>(type: "float", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BanquetFloorPlanTables", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BanquetFloorPlanTables_BanquetFloorPlans_BanquetFloorPlanId",
                        column: x => x.BanquetFloorPlanId,
                        principalTable: "BanquetFloorPlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AlacarteOrders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    OrderNo = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    DiningTableId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CustomerId = table.Column<int>(type: "int", nullable: true),
                    ReservationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    OrderDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    PaymentStatus = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    SubtotalAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    DiscountAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    VatAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ServiceChargeAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    PaidAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    RemainingAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    KitchenSentAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Note = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AlacarteOrders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AlacarteOrders_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_AlacarteOrders_DiningTables_DiningTableId",
                        column: x => x.DiningTableId,
                        principalTable: "DiningTables",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_AlacarteOrders_TableReservations_ReservationId",
                        column: x => x.ReservationId,
                        principalTable: "TableReservations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "AlacarteOrderLines",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AlacarteOrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MenuItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LineNo = table.Column<int>(type: "int", nullable: false),
                    MenuItemCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    MenuItemName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    UnitName = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Qty = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    VatRate = table.Column<decimal>(type: "decimal(9,4)", precision: 9, scale: 4, nullable: false),
                    VatAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    DiscountAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    KitchenNote = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    LineStatus = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    KitchenStatus = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    LineType = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    SentAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AlacarteOrderLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AlacarteOrderLines_AlacarteOrders_AlacarteOrderId",
                        column: x => x.AlacarteOrderId,
                        principalTable: "AlacarteOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AlacarteOrderLines_MenuItems_MenuItemId",
                        column: x => x.MenuItemId,
                        principalTable: "MenuItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RestaurantDeposits",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DepositNo = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    AlacarteOrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RestaurantContractId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TableReservationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    PaymentMethod = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    CashFundId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BankAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Receiver = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    Note = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RestaurantDeposits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RestaurantDeposits_AlacarteOrders_AlacarteOrderId",
                        column: x => x.AlacarteOrderId,
                        principalTable: "AlacarteOrders",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_RestaurantDeposits_BankAccounts_BankAccountId",
                        column: x => x.BankAccountId,
                        principalTable: "BankAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_RestaurantDeposits_CashFunds_CashFundId",
                        column: x => x.CashFundId,
                        principalTable: "CashFunds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_RestaurantDeposits_RestaurantContracts_RestaurantContractId",
                        column: x => x.RestaurantContractId,
                        principalTable: "RestaurantContracts",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_RestaurantDeposits_TableReservations_TableReservationId",
                        column: x => x.TableReservationId,
                        principalTable: "TableReservations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "RestaurantOrderExtraServices",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AlacarteOrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RestaurantExtraServiceId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Qty = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    VatRate = table.Column<decimal>(type: "decimal(9,4)", precision: 9, scale: 4, nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RestaurantOrderExtraServices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RestaurantOrderExtraServices_AlacarteOrders_AlacarteOrderId",
                        column: x => x.AlacarteOrderId,
                        principalTable: "AlacarteOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RestaurantOrderExtraServices_RestaurantExtraServices_RestaurantExtraServiceId",
                        column: x => x.RestaurantExtraServiceId,
                        principalTable: "RestaurantExtraServices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "RestaurantPreparationTickets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AlacarteOrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TicketNo = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    Station = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    TicketType = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    SentAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SentBy = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    Note = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RestaurantPreparationTickets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RestaurantPreparationTickets_AlacarteOrders_AlacarteOrderId",
                        column: x => x.AlacarteOrderId,
                        principalTable: "AlacarteOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RestaurantPreparationTicketLines",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RestaurantPreparationTicketId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AlacarteOrderLineId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LineNo = table.Column<int>(type: "int", nullable: false),
                    MenuItemName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Qty = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    KitchenNote = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RestaurantPreparationTicketLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RestaurantPreparationTicketLines_AlacarteOrderLines_AlacarteOrderLineId",
                        column: x => x.AlacarteOrderLineId,
                        principalTable: "AlacarteOrderLines",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_RestaurantPreparationTicketLines_RestaurantPreparationTickets_RestaurantPreparationTicketId",
                        column: x => x.RestaurantPreparationTicketId,
                        principalTable: "RestaurantPreparationTickets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AlacarteOrderLines_AlacarteOrderId_LineNo",
                table: "AlacarteOrderLines",
                columns: new[] { "AlacarteOrderId", "LineNo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AlacarteOrderLines_MenuItemId",
                table: "AlacarteOrderLines",
                column: "MenuItemId");

            migrationBuilder.CreateIndex(
                name: "IX_AlacarteOrders_CustomerId",
                table: "AlacarteOrders",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_AlacarteOrders_DiningTableId_Status",
                table: "AlacarteOrders",
                columns: new[] { "DiningTableId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_AlacarteOrders_OrderDate",
                table: "AlacarteOrders",
                column: "OrderDate");

            migrationBuilder.CreateIndex(
                name: "IX_AlacarteOrders_OrderNo",
                table: "AlacarteOrders",
                column: "OrderNo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AlacarteOrders_ReservationId",
                table: "AlacarteOrders",
                column: "ReservationId");

            migrationBuilder.CreateIndex(
                name: "IX_BanquetChecklists_BanquetEventId_SortOrder",
                table: "BanquetChecklists",
                columns: new[] { "BanquetEventId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_BanquetContracts_BanquetEventId",
                table: "BanquetContracts",
                column: "BanquetEventId");

            migrationBuilder.CreateIndex(
                name: "IX_BanquetContracts_ContractNo",
                table: "BanquetContracts",
                column: "ContractNo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BanquetContracts_CustomerId",
                table: "BanquetContracts",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_BanquetDeposits_BankAccountId",
                table: "BanquetDeposits",
                column: "BankAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_BanquetDeposits_BanquetContractId",
                table: "BanquetDeposits",
                column: "BanquetContractId");

            migrationBuilder.CreateIndex(
                name: "IX_BanquetDeposits_BanquetEventId",
                table: "BanquetDeposits",
                column: "BanquetEventId");

            migrationBuilder.CreateIndex(
                name: "IX_BanquetDeposits_CashFundId",
                table: "BanquetDeposits",
                column: "CashFundId");

            migrationBuilder.CreateIndex(
                name: "IX_BanquetDeposits_DepositNo",
                table: "BanquetDeposits",
                column: "DepositNo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BanquetEventMenus_BanquetEventId",
                table: "BanquetEventMenus",
                column: "BanquetEventId");

            migrationBuilder.CreateIndex(
                name: "IX_BanquetEventMenus_BanquetMenuPackageId",
                table: "BanquetEventMenus",
                column: "BanquetMenuPackageId");

            migrationBuilder.CreateIndex(
                name: "IX_BanquetEvents_BanquetRoomId_EventDate_StartTime_EndTime_Status",
                table: "BanquetEvents",
                columns: new[] { "BanquetRoomId", "EventDate", "StartTime", "EndTime", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_BanquetEvents_CustomerId",
                table: "BanquetEvents",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_BanquetEvents_EventNo",
                table: "BanquetEvents",
                column: "EventNo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BanquetFloorPlans_BanquetEventId",
                table: "BanquetFloorPlans",
                column: "BanquetEventId");

            migrationBuilder.CreateIndex(
                name: "IX_BanquetFloorPlanTables_BanquetFloorPlanId_TableNo",
                table: "BanquetFloorPlanTables",
                columns: new[] { "BanquetFloorPlanId", "TableNo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BanquetMenuPackageLines_BanquetMenuPackageId_LineNo",
                table: "BanquetMenuPackageLines",
                columns: new[] { "BanquetMenuPackageId", "LineNo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BanquetMenuPackageLines_MenuItemId",
                table: "BanquetMenuPackageLines",
                column: "MenuItemId");

            migrationBuilder.CreateIndex(
                name: "IX_BanquetMenuPackages_Code",
                table: "BanquetMenuPackages",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BanquetRooms_Code",
                table: "BanquetRooms",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BanquetRooms_Status",
                table: "BanquetRooms",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_BanquetServiceLines_BanquetEventId",
                table: "BanquetServiceLines",
                column: "BanquetEventId");

            migrationBuilder.CreateIndex(
                name: "IX_BanquetServiceLines_SupplierId",
                table: "BanquetServiceLines",
                column: "SupplierId");

            migrationBuilder.CreateIndex(
                name: "IX_BanquetTimelines_BanquetEventId_SortOrder",
                table: "BanquetTimelines",
                columns: new[] { "BanquetEventId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_DiningRooms_Code",
                table: "DiningRooms",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DiningRooms_Status",
                table: "DiningRooms",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_DiningTables_Code",
                table: "DiningTables",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DiningTables_DiningRoomId_SortOrder",
                table: "DiningTables",
                columns: new[] { "DiningRoomId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_DiningTables_Status",
                table: "DiningTables",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_RestaurantContracts_ContractNo",
                table: "RestaurantContracts",
                column: "ContractNo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RestaurantContracts_CustomerId",
                table: "RestaurantContracts",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_RestaurantDeposits_AlacarteOrderId",
                table: "RestaurantDeposits",
                column: "AlacarteOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_RestaurantDeposits_BankAccountId",
                table: "RestaurantDeposits",
                column: "BankAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_RestaurantDeposits_CashFundId",
                table: "RestaurantDeposits",
                column: "CashFundId");

            migrationBuilder.CreateIndex(
                name: "IX_RestaurantDeposits_DepositNo",
                table: "RestaurantDeposits",
                column: "DepositNo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RestaurantDeposits_RestaurantContractId",
                table: "RestaurantDeposits",
                column: "RestaurantContractId");

            migrationBuilder.CreateIndex(
                name: "IX_RestaurantDeposits_TableReservationId",
                table: "RestaurantDeposits",
                column: "TableReservationId");

            migrationBuilder.CreateIndex(
                name: "IX_RestaurantExtraServices_Code",
                table: "RestaurantExtraServices",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RestaurantOrderExtraServices_AlacarteOrderId",
                table: "RestaurantOrderExtraServices",
                column: "AlacarteOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_RestaurantOrderExtraServices_RestaurantExtraServiceId",
                table: "RestaurantOrderExtraServices",
                column: "RestaurantExtraServiceId");

            migrationBuilder.CreateIndex(
                name: "IX_RestaurantPreparationTicketLines_AlacarteOrderLineId",
                table: "RestaurantPreparationTicketLines",
                column: "AlacarteOrderLineId");

            migrationBuilder.CreateIndex(
                name: "IX_RestaurantPreparationTicketLines_RestaurantPreparationTicketId_LineNo",
                table: "RestaurantPreparationTicketLines",
                columns: new[] { "RestaurantPreparationTicketId", "LineNo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RestaurantPreparationTickets_AlacarteOrderId",
                table: "RestaurantPreparationTickets",
                column: "AlacarteOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_RestaurantPreparationTickets_TicketNo",
                table: "RestaurantPreparationTickets",
                column: "TicketNo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TableReservations_CustomerId",
                table: "TableReservations",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_TableReservations_DiningRoomId",
                table: "TableReservations",
                column: "DiningRoomId");

            migrationBuilder.CreateIndex(
                name: "IX_TableReservations_DiningTableId_ReservationDateTime_Status",
                table: "TableReservations",
                columns: new[] { "DiningTableId", "ReservationDateTime", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_TableReservations_Phone",
                table: "TableReservations",
                column: "Phone");

            migrationBuilder.CreateIndex(
                name: "IX_TableReservations_ReservationNo",
                table: "TableReservations",
                column: "ReservationNo",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BanquetChecklists");

            migrationBuilder.DropTable(
                name: "BanquetDeposits");

            migrationBuilder.DropTable(
                name: "BanquetEventMenus");

            migrationBuilder.DropTable(
                name: "BanquetFloorPlanTables");

            migrationBuilder.DropTable(
                name: "BanquetMenuPackageLines");

            migrationBuilder.DropTable(
                name: "BanquetServiceLines");

            migrationBuilder.DropTable(
                name: "BanquetTimelines");

            migrationBuilder.DropTable(
                name: "RestaurantDeposits");

            migrationBuilder.DropTable(
                name: "RestaurantOrderExtraServices");

            migrationBuilder.DropTable(
                name: "RestaurantPreparationTicketLines");

            migrationBuilder.DropTable(
                name: "BanquetContracts");

            migrationBuilder.DropTable(
                name: "BanquetFloorPlans");

            migrationBuilder.DropTable(
                name: "BanquetMenuPackages");

            migrationBuilder.DropTable(
                name: "RestaurantContracts");

            migrationBuilder.DropTable(
                name: "RestaurantExtraServices");

            migrationBuilder.DropTable(
                name: "AlacarteOrderLines");

            migrationBuilder.DropTable(
                name: "RestaurantPreparationTickets");

            migrationBuilder.DropTable(
                name: "BanquetEvents");

            migrationBuilder.DropTable(
                name: "AlacarteOrders");

            migrationBuilder.DropTable(
                name: "BanquetRooms");

            migrationBuilder.DropTable(
                name: "TableReservations");

            migrationBuilder.DropTable(
                name: "DiningTables");

            migrationBuilder.DropTable(
                name: "DiningRooms");
        }
    }
}
