using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ASMILE.BO.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddHotelRoomPhysicalAttributes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BedType",
                schema: "fo",
                table: "PhongKhachSan",
                type: "nvarchar(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsConnecting",
                schema: "fo",
                table: "PhongKhachSan",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsSmoking",
                schema: "fo",
                table: "PhongKhachSan",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "RoomCondition",
                schema: "fo",
                table: "PhongKhachSan",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "View",
                schema: "fo",
                table: "PhongKhachSan",
                type: "nvarchar(80)",
                maxLength: 80,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BedType",
                schema: "fo",
                table: "PhongKhachSan");

            migrationBuilder.DropColumn(
                name: "IsConnecting",
                schema: "fo",
                table: "PhongKhachSan");

            migrationBuilder.DropColumn(
                name: "IsSmoking",
                schema: "fo",
                table: "PhongKhachSan");

            migrationBuilder.DropColumn(
                name: "RoomCondition",
                schema: "fo",
                table: "PhongKhachSan");

            migrationBuilder.DropColumn(
                name: "View",
                schema: "fo",
                table: "PhongKhachSan");
        }
    }
}
