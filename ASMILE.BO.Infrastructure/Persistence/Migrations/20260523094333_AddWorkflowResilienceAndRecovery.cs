using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ASMILE.BO.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkflowResilienceAndRecovery : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "IdempotencyKeyEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CorrelationId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    WorkflowType = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    WorkflowKey = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    IdempotencyKey = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    ExecutionHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    PayloadHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    StartedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    MetadataJson = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IdempotencyKeyEntries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WorkflowExecutions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CorrelationId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    WorkflowType = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    WorkflowKey = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    RetryCount = table.Column<int>(type: "int", nullable: false),
                    StartedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FailedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastHeartbeatUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IdempotencyKey = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: true),
                    ExecutionHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    PayloadHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    RecoveryStateJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CompensationStateJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ExceptionType = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    ErrorMessage = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    MachineName = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    UserName = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    MetadataJson = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkflowExecutions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DistributedOperationMarkers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WorkflowExecutionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CorrelationId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    WorkflowType = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    WorkflowKey = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    StepName = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    MachineName = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    UserName = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    MetadataJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    StartedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DistributedOperationMarkers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DistributedOperationMarkers_WorkflowExecutions_WorkflowExecutionId",
                        column: x => x.WorkflowExecutionId,
                        principalTable: "WorkflowExecutions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WorkflowConsistencyViolations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WorkflowExecutionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CorrelationId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    WorkflowType = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    WorkflowKey = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    Severity = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Recommendation = table.Column<string>(type: "nvarchar(600)", maxLength: 600, nullable: false),
                    AffectedEntityModule = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    MetadataJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RecordedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkflowConsistencyViolations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkflowConsistencyViolations_WorkflowExecutions_WorkflowExecutionId",
                        column: x => x.WorkflowExecutionId,
                        principalTable: "WorkflowExecutions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "WorkflowExecutionSteps",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WorkflowExecutionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CorrelationId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    StepName = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    RetryCount = table.Column<int>(type: "int", nullable: false),
                    StartedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FailedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastHeartbeatUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExceptionType = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    ErrorMessage = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    MetadataJson = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkflowExecutionSteps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkflowExecutionSteps_WorkflowExecutions_WorkflowExecutionId",
                        column: x => x.WorkflowExecutionId,
                        principalTable: "WorkflowExecutions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WorkflowCompensationActions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WorkflowExecutionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WorkflowExecutionStepId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CorrelationId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    StepName = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    RetryCount = table.Column<int>(type: "int", nullable: false),
                    CompensationStateJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ExceptionType = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    ErrorMessage = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    MetadataJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    StartedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FailedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkflowCompensationActions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkflowCompensationActions_WorkflowExecutionSteps_WorkflowExecutionStepId",
                        column: x => x.WorkflowExecutionStepId,
                        principalTable: "WorkflowExecutionSteps",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_WorkflowCompensationActions_WorkflowExecutions_WorkflowExecutionId",
                        column: x => x.WorkflowExecutionId,
                        principalTable: "WorkflowExecutions",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "WorkflowRetryHistories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WorkflowExecutionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WorkflowExecutionStepId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CorrelationId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    WorkflowType = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    StepName = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    RetryCount = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    ExceptionType = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    ErrorMessage = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    MetadataJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    StartedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkflowRetryHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkflowRetryHistories_WorkflowExecutionSteps_WorkflowExecutionStepId",
                        column: x => x.WorkflowExecutionStepId,
                        principalTable: "WorkflowExecutionSteps",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_WorkflowRetryHistories_WorkflowExecutions_WorkflowExecutionId",
                        column: x => x.WorkflowExecutionId,
                        principalTable: "WorkflowExecutions",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_DistributedOperationMarkers_CompanyId_BranchId_WorkflowType_WorkflowKey_StepName",
                table: "DistributedOperationMarkers",
                columns: new[] { "CompanyId", "BranchId", "WorkflowType", "WorkflowKey", "StepName" });

            migrationBuilder.CreateIndex(
                name: "IX_DistributedOperationMarkers_WorkflowExecutionId",
                table: "DistributedOperationMarkers",
                column: "WorkflowExecutionId");

            migrationBuilder.CreateIndex(
                name: "IX_IdempotencyKeyEntries_CompanyId_BranchId_WorkflowType_WorkflowKey_IdempotencyKey",
                table: "IdempotencyKeyEntries",
                columns: new[] { "CompanyId", "BranchId", "WorkflowType", "WorkflowKey", "IdempotencyKey" },
                unique: true,
                filter: "[CompanyId] IS NOT NULL AND [BranchId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_IdempotencyKeyEntries_WorkflowType_WorkflowKey_Status",
                table: "IdempotencyKeyEntries",
                columns: new[] { "WorkflowType", "WorkflowKey", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowCompensationActions_WorkflowExecutionId",
                table: "WorkflowCompensationActions",
                column: "WorkflowExecutionId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowCompensationActions_WorkflowExecutionId_StepName_Status",
                table: "WorkflowCompensationActions",
                columns: new[] { "WorkflowExecutionId", "StepName", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowCompensationActions_WorkflowExecutionStepId",
                table: "WorkflowCompensationActions",
                column: "WorkflowExecutionStepId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowConsistencyViolations_Category_Severity",
                table: "WorkflowConsistencyViolations",
                columns: new[] { "Category", "Severity" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowConsistencyViolations_CompanyId_BranchId_WorkflowType_RecordedAtUtc",
                table: "WorkflowConsistencyViolations",
                columns: new[] { "CompanyId", "BranchId", "WorkflowType", "RecordedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowConsistencyViolations_WorkflowExecutionId",
                table: "WorkflowConsistencyViolations",
                column: "WorkflowExecutionId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowExecutions_CompanyId_BranchId_WorkflowType_WorkflowKey_StartedAtUtc",
                table: "WorkflowExecutions",
                columns: new[] { "CompanyId", "BranchId", "WorkflowType", "WorkflowKey", "StartedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowExecutions_CorrelationId",
                table: "WorkflowExecutions",
                column: "CorrelationId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowExecutions_LastHeartbeatUtc",
                table: "WorkflowExecutions",
                column: "LastHeartbeatUtc");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowExecutions_WorkflowType_WorkflowKey_Status",
                table: "WorkflowExecutions",
                columns: new[] { "WorkflowType", "WorkflowKey", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowExecutionSteps_LastHeartbeatUtc",
                table: "WorkflowExecutionSteps",
                column: "LastHeartbeatUtc");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowExecutionSteps_WorkflowExecutionId",
                table: "WorkflowExecutionSteps",
                column: "WorkflowExecutionId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowExecutionSteps_WorkflowExecutionId_StepName_Status",
                table: "WorkflowExecutionSteps",
                columns: new[] { "WorkflowExecutionId", "StepName", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowRetryHistories_WorkflowExecutionId",
                table: "WorkflowRetryHistories",
                column: "WorkflowExecutionId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowRetryHistories_WorkflowExecutionStepId",
                table: "WorkflowRetryHistories",
                column: "WorkflowExecutionStepId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowRetryHistories_WorkflowType_RetryCount_StartedAtUtc",
                table: "WorkflowRetryHistories",
                columns: new[] { "WorkflowType", "RetryCount", "StartedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DistributedOperationMarkers");

            migrationBuilder.DropTable(
                name: "IdempotencyKeyEntries");

            migrationBuilder.DropTable(
                name: "WorkflowCompensationActions");

            migrationBuilder.DropTable(
                name: "WorkflowConsistencyViolations");

            migrationBuilder.DropTable(
                name: "WorkflowRetryHistories");

            migrationBuilder.DropTable(
                name: "WorkflowExecutionSteps");

            migrationBuilder.DropTable(
                name: "WorkflowExecutions");
        }
    }
}
