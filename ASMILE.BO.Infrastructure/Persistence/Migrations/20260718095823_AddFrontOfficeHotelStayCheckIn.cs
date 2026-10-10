using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ASMILE.BO.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddFrontOfficeHotelStayCheckIn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_LuuTruKhachSan_RoomId",
                schema: "fo",
                table: "LuuTruKhachSan");

            migrationBuilder.CreateIndex(
                name: "UQ_fo_LuuTruKhachSan_Phong_InHouse",
                schema: "fo",
                table: "LuuTruKhachSan",
                column: "RoomId",
                unique: true,
                filter: "[Status] = 'InHouse'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UQ_fo_LuuTruKhachSan_Phong_InHouse",
                schema: "fo",
                table: "LuuTruKhachSan");

            migrationBuilder.CreateIndex(
                name: "IX_LuuTruKhachSan_RoomId",
                schema: "fo",
                table: "LuuTruKhachSan",
                column: "RoomId");
        }
    }
}
