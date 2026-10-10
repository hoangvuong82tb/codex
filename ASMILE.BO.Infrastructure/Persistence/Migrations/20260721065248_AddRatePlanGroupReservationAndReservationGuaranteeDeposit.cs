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
            // The previous migration (20260720032319) already creates the shared
            // group, rate-plan, stay, and folio tables. This migration only adds
            // the reservation guarantee/deposit fields introduced here.
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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
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
        }
    }
}
