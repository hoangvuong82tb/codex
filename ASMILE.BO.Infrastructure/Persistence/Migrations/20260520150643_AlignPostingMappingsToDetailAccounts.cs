using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ASMILE.BO.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AlignPostingMappingsToDetailAccounts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "AccountRoleMappings",
                keyColumn: "Id",
                keyValue: new Guid("6f6b8f41-9c2d-4d66-9b2f-000000000156"),
                column: "AccountCode",
                value: "1561");

            migrationBuilder.UpdateData(
                table: "AccountRoleMappings",
                keyColumn: "Id",
                keyValue: new Guid("6f6b8f41-9c2d-4d66-9b2f-000000000331"),
                column: "AccountCode",
                value: "3311");

            migrationBuilder.UpdateData(
                table: "AccountRoleMappings",
                keyColumn: "Id",
                keyValue: new Guid("6f6b8f41-9c2d-4d66-9b2f-000000000632"),
                column: "AccountCode",
                value: "6321");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "AccountRoleMappings",
                keyColumn: "Id",
                keyValue: new Guid("6f6b8f41-9c2d-4d66-9b2f-000000000156"),
                column: "AccountCode",
                value: "156");

            migrationBuilder.UpdateData(
                table: "AccountRoleMappings",
                keyColumn: "Id",
                keyValue: new Guid("6f6b8f41-9c2d-4d66-9b2f-000000000331"),
                column: "AccountCode",
                value: "331");

            migrationBuilder.UpdateData(
                table: "AccountRoleMappings",
                keyColumn: "Id",
                keyValue: new Guid("6f6b8f41-9c2d-4d66-9b2f-000000000632"),
                column: "AccountCode",
                value: "632");
        }
    }
}
