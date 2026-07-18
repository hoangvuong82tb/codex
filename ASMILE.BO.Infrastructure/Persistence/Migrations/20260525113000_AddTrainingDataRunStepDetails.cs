using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ASMILE.BO.Infrastructure.Persistence.Migrations
{
    public partial class AddTrainingDataRunStepDetails : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "EntityName",
                table: "TrainingDataRunSteps",
                type: "nvarchar(160)",
                maxLength: 160,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "BeforeCount",
                table: "TrainingDataRunSteps",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "AfterCount",
                table: "TrainingDataRunSteps",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "TrainingDataRunSteps",
                type: "nvarchar(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EntityName",
                table: "TrainingDataRunSteps");

            migrationBuilder.DropColumn(
                name: "BeforeCount",
                table: "TrainingDataRunSteps");

            migrationBuilder.DropColumn(
                name: "AfterCount",
                table: "TrainingDataRunSteps");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "TrainingDataRunSteps");
        }
    }
}
