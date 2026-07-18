using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ASMILE.BO.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTrainingDataRunHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TrainingDataRuns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OperationType = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    SeedProfile = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: true),
                    StartedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FinishedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    RequestedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDryRun = table.Column<bool>(type: "bit", nullable: false),
                    SummaryJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ErrorMessage = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrainingDataRuns", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TrainingDataRunSteps",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TrainingDataRunId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StepOrder = table.Column<int>(type: "int", nullable: false),
                    StepCode = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    StepName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Success = table.Column<bool>(type: "bit", nullable: false),
                    DeletedCount = table.Column<int>(type: "int", nullable: false),
                    CreatedCount = table.Column<int>(type: "int", nullable: false),
                    Message = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    ErrorMessage = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    StartedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FinishedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrainingDataRunSteps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TrainingDataRunSteps_TrainingDataRuns_TrainingDataRunId",
                        column: x => x.TrainingDataRunId,
                        principalTable: "TrainingDataRuns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TrainingDataRuns_OperationType_StartedAtUtc",
                table: "TrainingDataRuns",
                columns: new[] { "OperationType", "StartedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_TrainingDataRuns_StartedAtUtc",
                table: "TrainingDataRuns",
                column: "StartedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingDataRunSteps_TrainingDataRunId_StepOrder",
                table: "TrainingDataRunSteps",
                columns: new[] { "TrainingDataRunId", "StepOrder" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TrainingDataRunSteps");

            migrationBuilder.DropTable(
                name: "TrainingDataRuns");
        }
    }
}
