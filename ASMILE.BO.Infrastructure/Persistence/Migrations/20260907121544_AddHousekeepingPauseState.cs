using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ASMILE.BO.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddHousekeepingPauseState : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PauseReason",
                schema: "fo",
                table: "DonDep",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PausedAtUtc",
                schema: "fo",
                table: "DonDep",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ResumedAtUtc",
                schema: "fo",
                table: "DonDep",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TotalPauseMinutes",
                schema: "fo",
                table: "DonDep",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PauseReason",
                schema: "fo",
                table: "DonDep");

            migrationBuilder.DropColumn(
                name: "PausedAtUtc",
                schema: "fo",
                table: "DonDep");

            migrationBuilder.DropColumn(
                name: "ResumedAtUtc",
                schema: "fo",
                table: "DonDep");

            migrationBuilder.DropColumn(
                name: "TotalPauseMinutes",
                schema: "fo",
                table: "DonDep");
        }
    }
}
