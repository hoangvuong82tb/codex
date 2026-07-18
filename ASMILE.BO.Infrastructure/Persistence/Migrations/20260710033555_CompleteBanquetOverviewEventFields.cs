using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ASMILE.BO.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CompleteBanquetOverviewEventFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BanquetContracts_BanquetEvents_BanquetEventId",
                table: "BanquetContracts");

            migrationBuilder.DropForeignKey(
                name: "FK_BanquetDeposits_BanquetEvents_BanquetEventId",
                table: "BanquetDeposits");

            migrationBuilder.AddColumn<DateTime>(
                name: "BirthdayDate",
                table: "BanquetEvents",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BirthdayPersonName",
                table: "BanquetEvents",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BrideName",
                table: "BanquetEvents",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ConferenceName",
                table: "BanquetEvents",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ExtraGuestCount",
                table: "BanquetEvents",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ExtraTableCount",
                table: "BanquetEvents",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "GroomName",
                table: "BanquetEvents",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MeetingTopic",
                table: "BanquetEvents",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OtherEventName",
                table: "BanquetEvents",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PartyTheme",
                table: "BanquetEvents",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_BanquetContracts_BanquetEvents_BanquetEventId",
                table: "BanquetContracts",
                column: "BanquetEventId",
                principalTable: "BanquetEvents",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_BanquetDeposits_BanquetEvents_BanquetEventId",
                table: "BanquetDeposits",
                column: "BanquetEventId",
                principalTable: "BanquetEvents",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BanquetContracts_BanquetEvents_BanquetEventId",
                table: "BanquetContracts");

            migrationBuilder.DropForeignKey(
                name: "FK_BanquetDeposits_BanquetEvents_BanquetEventId",
                table: "BanquetDeposits");

            migrationBuilder.DropColumn(
                name: "BirthdayDate",
                table: "BanquetEvents");

            migrationBuilder.DropColumn(
                name: "BirthdayPersonName",
                table: "BanquetEvents");

            migrationBuilder.DropColumn(
                name: "BrideName",
                table: "BanquetEvents");

            migrationBuilder.DropColumn(
                name: "ConferenceName",
                table: "BanquetEvents");

            migrationBuilder.DropColumn(
                name: "ExtraGuestCount",
                table: "BanquetEvents");

            migrationBuilder.DropColumn(
                name: "ExtraTableCount",
                table: "BanquetEvents");

            migrationBuilder.DropColumn(
                name: "GroomName",
                table: "BanquetEvents");

            migrationBuilder.DropColumn(
                name: "MeetingTopic",
                table: "BanquetEvents");

            migrationBuilder.DropColumn(
                name: "OtherEventName",
                table: "BanquetEvents");

            migrationBuilder.DropColumn(
                name: "PartyTheme",
                table: "BanquetEvents");

            migrationBuilder.AddForeignKey(
                name: "FK_BanquetContracts_BanquetEvents_BanquetEventId",
                table: "BanquetContracts",
                column: "BanquetEventId",
                principalTable: "BanquetEvents",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_BanquetDeposits_BanquetEvents_BanquetEventId",
                table: "BanquetDeposits",
                column: "BanquetEventId",
                principalTable: "BanquetEvents",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
