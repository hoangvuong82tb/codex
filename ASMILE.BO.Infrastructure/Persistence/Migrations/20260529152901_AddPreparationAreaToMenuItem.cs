using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ASMILE.BO.Infrastructure.Persistence.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260529152901_AddPreparationAreaToMenuItem")]
    public partial class AddPreparationAreaToMenuItem : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                IF COL_LENGTH('MenuItems', 'PreparationArea') IS NULL
                    ALTER TABLE [MenuItems] ADD [PreparationArea] nvarchar(100) NULL;
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                IF COL_LENGTH('MenuItems', 'PreparationArea') IS NOT NULL
                    ALTER TABLE [MenuItems] DROP COLUMN [PreparationArea];
                """);
        }
    }
}