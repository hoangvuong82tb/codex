using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ASMILE.BO.Infrastructure.Persistence.Migrations;

public partial class AddGroupServiceOrders : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "DichVuDoan",
            schema: "fo",
            columns: table => new
            {
                GroupServiceOrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                PropertyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                GroupReservationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                ServiceCode = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                ServiceName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                Category = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                SubCategory = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                Department = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                Provider = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: true),
                Location = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: true),
                ServiceDate = table.Column<DateOnly>(type: "date", nullable: false),
                ServiceTime = table.Column<TimeOnly>(type: "time", nullable: false),
                DurationMinutes = table.Column<int>(type: "int", nullable: false),
                Quantity = table.Column<int>(type: "int", nullable: false),
                UnitPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                DiscountRate = table.Column<decimal>(type: "decimal(9,4)", nullable: false),
                TaxRate = table.Column<decimal>(type: "decimal(9,4)", nullable: false),
                ServiceChargeRate = table.Column<decimal>(type: "decimal(9,4)", nullable: false),
                PaidAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                ChargeTo = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                GuestName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                Remark = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_fo_DichVuDoan", x => x.GroupServiceOrderId);
                table.ForeignKey(
                    name: "FK_DichVuDoan_DatPhongDoan_GroupReservationId",
                    column: x => x.GroupReservationId,
                    principalSchema: "fo",
                    principalTable: "DatPhongDoan",
                    principalColumn: "GroupReservationId",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "IX_DichVuDoan_GroupReservationId",
            schema: "fo",
            table: "DichVuDoan",
            column: "GroupReservationId");

        migrationBuilder.CreateIndex(
            name: "IX_fo_DichVuDoan_CoSo_Doan_Ngay",
            schema: "fo",
            table: "DichVuDoan",
            columns: new[] { "PropertyId", "GroupReservationId", "ServiceDate" });

        migrationBuilder.CreateIndex(
            name: "IX_fo_DichVuDoan_CoSo_TrangThai",
            schema: "fo",
            table: "DichVuDoan",
            columns: new[] { "PropertyId", "Status" });

        migrationBuilder.CreateIndex(
            name: "UQ_fo_DichVuDoan_CoSo_MaDV",
            schema: "fo",
            table: "DichVuDoan",
            columns: new[] { "PropertyId", "ServiceCode" },
            unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "DichVuDoan",
            schema: "fo");
    }
}

