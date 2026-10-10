using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ASMILE.BO.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkforceMetadataPersistence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "WorkforceBuilding",
                table: "People",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "WorkforceFloor",
                table: "People",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "WorkforceShift",
                table: "People",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "WorkforceStatus",
                table: "People",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.UpdateData(
                table: "People",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "WorkforceBuilding", "WorkforceFloor", "WorkforceShift", "WorkforceStatus" },
                values: new object[] { "", "", "Chưa phân ca", "Chưa xác định" });

            migrationBuilder.UpdateData(
                table: "People",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "WorkforceBuilding", "WorkforceFloor", "WorkforceShift", "WorkforceStatus" },
                values: new object[] { "", "", "Chưa phân ca", "Chưa xác định" });

            migrationBuilder.UpdateData(
                table: "People",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "WorkforceBuilding", "WorkforceFloor", "WorkforceShift", "WorkforceStatus" },
                values: new object[] { "", "", "Chưa phân ca", "Chưa xác định" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "WorkforceBuilding",
                table: "People");

            migrationBuilder.DropColumn(
                name: "WorkforceFloor",
                table: "People");

            migrationBuilder.DropColumn(
                name: "WorkforceShift",
                table: "People");

            migrationBuilder.DropColumn(
                name: "WorkforceStatus",
                table: "People");
        }
    }
}
