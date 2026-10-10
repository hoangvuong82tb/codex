using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ASMILE.BO.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddLostFoundContactPersistence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ContactDate",
                table: "LostFoundItems",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContactMethod",
                table: "LostFoundItems",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContactNotes",
                table: "LostFoundItems",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContactResult",
                table: "LostFoundItems",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContactedBy",
                table: "LostFoundItems",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ContactDate",
                table: "LostFoundItems");

            migrationBuilder.DropColumn(
                name: "ContactMethod",
                table: "LostFoundItems");

            migrationBuilder.DropColumn(
                name: "ContactNotes",
                table: "LostFoundItems");

            migrationBuilder.DropColumn(
                name: "ContactResult",
                table: "LostFoundItems");

            migrationBuilder.DropColumn(
                name: "ContactedBy",
                table: "LostFoundItems");
        }
    }
}
