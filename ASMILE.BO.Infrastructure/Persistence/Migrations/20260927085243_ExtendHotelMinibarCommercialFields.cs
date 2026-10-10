using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ASMILE.BO.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ExtendHotelMinibarCommercialFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AutoPostToFolio",
                schema: "fo",
                table: "HangMinibarFO",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "ReplenishmentLeadMinutes",
                schema: "fo",
                table: "HangMinibarFO",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "RequiresApproval",
                schema: "fo",
                table: "HangMinibarFO",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AutoPostToFolio",
                schema: "fo",
                table: "HangMinibarFO");

            migrationBuilder.DropColumn(
                name: "ReplenishmentLeadMinutes",
                schema: "fo",
                table: "HangMinibarFO");

            migrationBuilder.DropColumn(
                name: "RequiresApproval",
                schema: "fo",
                table: "HangMinibarFO");
        }
    }
}
