using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ASMILE.BO.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase3Step35SchedulerEngine : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "JobDefinitions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    JobCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    JobName = table.Column<string>(type: "nvarchar(180)", maxLength: 180, nullable: false),
                    JobType = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    ScheduleType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    IntervalMinutes = table.Column<int>(type: "int", nullable: true),
                    CronExpression = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    NextRunAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table => table.PrimaryKey("PK_JobDefinitions", x => x.Id));

            migrationBuilder.CreateTable(
                name: "JobExecutions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    JobDefinitionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    JobCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    QueuedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DurationMs = table.Column<long>(type: "bigint", nullable: false),
                    CancellationRequested = table.Column<bool>(type: "bit", nullable: false),
                    TriggeredBy = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    CorrelationId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ErrorMessage = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JobExecutions", x => x.Id);
                    table.ForeignKey(name: "FK_JobExecutions_JobDefinitions_JobDefinitionId", column: x => x.JobDefinitionId, principalTable: "JobDefinitions", principalColumn: "Id", onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "JobLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    JobExecutionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Level = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Message = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JobLogs", x => x.Id);
                    table.ForeignKey(name: "FK_JobLogs_JobExecutions_JobExecutionId", column: x => x.JobExecutionId, principalTable: "JobExecutions", principalColumn: "Id", onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "JobRetries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    JobExecutionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AttemptNo = table.Column<int>(type: "int", nullable: false),
                    ScheduledAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExecutedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    ErrorMessage = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JobRetries", x => x.Id);
                    table.ForeignKey(name: "FK_JobRetries_JobExecutions_JobExecutionId", column: x => x.JobExecutionId, principalTable: "JobExecutions", principalColumn: "Id", onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(name: "IX_JobDefinitions_JobCode", table: "JobDefinitions", column: "JobCode", unique: true);
            migrationBuilder.CreateIndex(name: "IX_JobDefinitions_NextRunAt", table: "JobDefinitions", column: "NextRunAt");
            migrationBuilder.CreateIndex(name: "IX_JobDefinitions_IsActive", table: "JobDefinitions", column: "IsActive");
            migrationBuilder.CreateIndex(name: "IX_JobExecutions_JobDefinitionId", table: "JobExecutions", column: "JobDefinitionId");
            migrationBuilder.CreateIndex(name: "IX_JobExecutions_JobCode", table: "JobExecutions", column: "JobCode");
            migrationBuilder.CreateIndex(name: "IX_JobExecutions_Status", table: "JobExecutions", column: "Status");
            migrationBuilder.CreateIndex(name: "IX_JobExecutions_QueuedAt", table: "JobExecutions", column: "QueuedAt");
            migrationBuilder.CreateIndex(name: "IX_JobLogs_JobExecutionId", table: "JobLogs", column: "JobExecutionId");
            migrationBuilder.CreateIndex(name: "IX_JobLogs_CreatedAt", table: "JobLogs", column: "CreatedAt");
            migrationBuilder.CreateIndex(name: "IX_JobRetries_JobExecutionId", table: "JobRetries", column: "JobExecutionId");
            migrationBuilder.CreateIndex(name: "IX_JobRetries_Status", table: "JobRetries", column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "JobLogs");
            migrationBuilder.DropTable(name: "JobRetries");
            migrationBuilder.DropTable(name: "JobExecutions");
            migrationBuilder.DropTable(name: "JobDefinitions");
        }
    }
}
