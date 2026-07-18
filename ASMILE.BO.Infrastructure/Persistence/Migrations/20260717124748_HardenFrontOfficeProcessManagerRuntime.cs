using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ASMILE.BO.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class HardenFrontOfficeProcessManagerRuntime : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DeadLetteredAtUtc",
                schema: "fo_process",
                table: "HenGioQuyTrinh",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastErrorCode",
                schema: "fo_process",
                table: "HenGioQuyTrinh",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "NextRetryAtUtc",
                schema: "fo_process",
                table: "HenGioQuyTrinh",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ApprovedByActorId",
                schema: "fo_process",
                table: "HanhDongThuCongQuyTrinh",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedByActorId",
                schema: "fo_process",
                table: "HanhDongThuCongQuyTrinh",
                type: "uniqueidentifier",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeadLetteredAtUtc",
                schema: "fo_process",
                table: "HenGioQuyTrinh");

            migrationBuilder.DropColumn(
                name: "LastErrorCode",
                schema: "fo_process",
                table: "HenGioQuyTrinh");

            migrationBuilder.DropColumn(
                name: "NextRetryAtUtc",
                schema: "fo_process",
                table: "HenGioQuyTrinh");

            migrationBuilder.DropColumn(
                name: "ApprovedByActorId",
                schema: "fo_process",
                table: "HanhDongThuCongQuyTrinh");

            migrationBuilder.DropColumn(
                name: "CreatedByActorId",
                schema: "fo_process",
                table: "HanhDongThuCongQuyTrinh");
        }
    }
}
