using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ASMILE.BO.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEnterpriseReliabilityAndDistributedCoordination : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ParentWorkflowExecutionId",
                table: "WorkflowExecutions",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MetadataJson",
                table: "SearchIndexEntries",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MetadataJson",
                table: "RecentSearches",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "EnterpriseConsistencyViolations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Category = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Severity = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Module = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    EntityType = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    EntityId = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Recommendation = table.Column<string>(type: "nvarchar(600)", maxLength: 600, nullable: false),
                    CanAutoRecover = table.Column<bool>(type: "bit", nullable: false),
                    MetadataJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RecordedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EnterpriseConsistencyViolations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WorkflowDeadLetters",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    WorkflowExecutionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CorrelationId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    WorkflowType = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    WorkflowKey = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    DeadLetterReason = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    PayloadJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ExceptionType = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    ErrorMessage = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    MetadataJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DeadLetteredAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RecoveredAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkflowDeadLetters", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkflowDeadLetters_WorkflowExecutions_WorkflowExecutionId",
                        column: x => x.WorkflowExecutionId,
                        principalTable: "WorkflowExecutions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "WorkflowDistributedLocks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    WorkflowExecutionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CorrelationId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    WorkflowType = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    WorkflowKey = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    LockScope = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    LockKey = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    LockedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastHeartbeatUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiresAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    HolderMachineName = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    HolderProcessId = table.Column<int>(type: "int", nullable: false),
                    MetadataJson = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkflowDistributedLocks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkflowDistributedLocks_WorkflowExecutions_WorkflowExecutionId",
                        column: x => x.WorkflowExecutionId,
                        principalTable: "WorkflowExecutions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "WorkflowExecutionMetrics",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    WorkflowExecutionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CorrelationId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    WorkflowType = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    WorkflowKey = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    MetricName = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Severity = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    MetricValue = table.Column<double>(type: "float", nullable: false),
                    DurationMs = table.Column<long>(type: "bigint", nullable: false),
                    MetadataJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RecordedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkflowExecutionMetrics", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkflowExecutionMetrics_WorkflowExecutions_WorkflowExecutionId",
                        column: x => x.WorkflowExecutionId,
                        principalTable: "WorkflowExecutions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "WorkflowLeases",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    WorkflowExecutionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CorrelationId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    WorkflowType = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    WorkflowKey = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    LeaseKey = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    HolderMachineName = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    HolderProcessId = table.Column<int>(type: "int", nullable: false),
                    StartedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastHeartbeatUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiresAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    MetadataJson = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkflowLeases", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkflowLeases_WorkflowExecutions_WorkflowExecutionId",
                        column: x => x.WorkflowExecutionId,
                        principalTable: "WorkflowExecutions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "WorkflowRecoveryCheckpoints",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    WorkflowExecutionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ParentCheckpointId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CorrelationId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    WorkflowType = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    WorkflowKey = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    CheckpointName = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    SnapshotJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SnapshotHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    MetadataJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RestoredAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkflowRecoveryCheckpoints", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkflowRecoveryCheckpoints_WorkflowExecutions_WorkflowExecutionId",
                        column: x => x.WorkflowExecutionId,
                        principalTable: "WorkflowExecutions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowExecutions_ParentWorkflowExecutionId",
                table: "WorkflowExecutions",
                column: "ParentWorkflowExecutionId");

            migrationBuilder.CreateIndex(
                name: "IX_EnterpriseConsistencyViolations_CompanyId_BranchId_Category_RecordedAtUtc",
                table: "EnterpriseConsistencyViolations",
                columns: new[] { "CompanyId", "BranchId", "Category", "RecordedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_EnterpriseConsistencyViolations_Module_EntityType",
                table: "EnterpriseConsistencyViolations",
                columns: new[] { "Module", "EntityType" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowDeadLetters_CompanyId_BranchId_WorkflowType_WorkflowKey_DeadLetteredAtUtc",
                table: "WorkflowDeadLetters",
                columns: new[] { "CompanyId", "BranchId", "WorkflowType", "WorkflowKey", "DeadLetteredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowDeadLetters_Status",
                table: "WorkflowDeadLetters",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowDeadLetters_WorkflowExecutionId",
                table: "WorkflowDeadLetters",
                column: "WorkflowExecutionId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowDistributedLocks_CompanyId_BranchId_LockScope_LockKey",
                table: "WorkflowDistributedLocks",
                columns: new[] { "CompanyId", "BranchId", "LockScope", "LockKey" },
                unique: true,
                filter: "[CompanyId] IS NOT NULL AND [BranchId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowDistributedLocks_ExpiresAtUtc",
                table: "WorkflowDistributedLocks",
                column: "ExpiresAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowDistributedLocks_WorkflowExecutionId",
                table: "WorkflowDistributedLocks",
                column: "WorkflowExecutionId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowExecutionMetrics_CompanyId_BranchId_WorkflowType_RecordedAtUtc",
                table: "WorkflowExecutionMetrics",
                columns: new[] { "CompanyId", "BranchId", "WorkflowType", "RecordedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowExecutionMetrics_CorrelationId",
                table: "WorkflowExecutionMetrics",
                column: "CorrelationId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowExecutionMetrics_WorkflowExecutionId",
                table: "WorkflowExecutionMetrics",
                column: "WorkflowExecutionId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowLeases_CompanyId_BranchId_WorkflowType_WorkflowKey_LeaseKey",
                table: "WorkflowLeases",
                columns: new[] { "CompanyId", "BranchId", "WorkflowType", "WorkflowKey", "LeaseKey" },
                unique: true,
                filter: "[CompanyId] IS NOT NULL AND [BranchId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowLeases_ExpiresAtUtc",
                table: "WorkflowLeases",
                column: "ExpiresAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowLeases_WorkflowExecutionId",
                table: "WorkflowLeases",
                column: "WorkflowExecutionId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowRecoveryCheckpoints_CompanyId_BranchId_WorkflowType_WorkflowKey_CreatedAtUtc",
                table: "WorkflowRecoveryCheckpoints",
                columns: new[] { "CompanyId", "BranchId", "WorkflowType", "WorkflowKey", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowRecoveryCheckpoints_Status",
                table: "WorkflowRecoveryCheckpoints",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowRecoveryCheckpoints_WorkflowExecutionId",
                table: "WorkflowRecoveryCheckpoints",
                column: "WorkflowExecutionId");

            migrationBuilder.AddForeignKey(
                name: "FK_WorkflowExecutions_WorkflowExecutions_ParentWorkflowExecutionId",
                table: "WorkflowExecutions",
                column: "ParentWorkflowExecutionId",
                principalTable: "WorkflowExecutions",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_WorkflowExecutions_WorkflowExecutions_ParentWorkflowExecutionId",
                table: "WorkflowExecutions");

            migrationBuilder.DropTable(
                name: "EnterpriseConsistencyViolations");

            migrationBuilder.DropTable(
                name: "WorkflowDeadLetters");

            migrationBuilder.DropTable(
                name: "WorkflowDistributedLocks");

            migrationBuilder.DropTable(
                name: "WorkflowExecutionMetrics");

            migrationBuilder.DropTable(
                name: "WorkflowLeases");

            migrationBuilder.DropTable(
                name: "WorkflowRecoveryCheckpoints");

            migrationBuilder.DropIndex(
                name: "IX_WorkflowExecutions_ParentWorkflowExecutionId",
                table: "WorkflowExecutions");

            migrationBuilder.DropColumn(
                name: "ParentWorkflowExecutionId",
                table: "WorkflowExecutions");

            migrationBuilder.DropColumn(
                name: "MetadataJson",
                table: "SearchIndexEntries");

            migrationBuilder.DropColumn(
                name: "MetadataJson",
                table: "RecentSearches");
        }
    }
}
