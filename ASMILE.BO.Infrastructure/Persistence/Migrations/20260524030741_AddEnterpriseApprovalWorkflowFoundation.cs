using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ASMILE.BO.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEnterpriseApprovalWorkflowFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ActiveCompanySessions_Branches_BranchId",
                table: "ActiveCompanySessions");

            migrationBuilder.DropForeignKey(
                name: "FK_ActiveCompanySessions_Companies_CompanyId",
                table: "ActiveCompanySessions");

            migrationBuilder.DropForeignKey(
                name: "FK_BranchSettings_Companies_CompanyId",
                table: "BranchSettings");

            migrationBuilder.DropForeignKey(
                name: "FK_WorkflowCompensationActions_WorkflowExecutions_WorkflowExecutionId",
                table: "WorkflowCompensationActions");

            migrationBuilder.DropForeignKey(
                name: "FK_WorkflowExecutions_WorkflowExecutions_ParentWorkflowExecutionId",
                table: "WorkflowExecutions");

            migrationBuilder.DropForeignKey(
                name: "FK_WorkflowRetryHistories_WorkflowExecutions_WorkflowExecutionId",
                table: "WorkflowRetryHistories");

            migrationBuilder.CreateTable(
                name: "ApprovalDelegations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AssignmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FromUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ToUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DelegatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiresAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    MetadataJson = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApprovalDelegations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ApprovalEscalations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    WorkflowId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StepId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AssignmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    EscalatedToUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    EscalatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsResolved = table.Column<bool>(type: "bit", nullable: false),
                    ResolvedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    MetadataJson = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApprovalEscalations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ApprovalWorkflows",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Module = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    EntityType = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    EntityId = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    CurrentLevel = table.Column<int>(type: "int", nullable: false),
                    TotalLevels = table.Column<int>(type: "int", nullable: false),
                    IsCompleted = table.Column<bool>(type: "bit", nullable: false),
                    IsRejected = table.Column<bool>(type: "bit", nullable: false),
                    SubmittedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastActivityAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CorrelationId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    MetadataJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApprovalWorkflows", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ApprovalSnapshots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    WorkflowId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CapturedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    CurrentLevel = table.Column<int>(type: "int", nullable: false),
                    SnapshotJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SnapshotHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApprovalSnapshots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ApprovalSnapshots_ApprovalWorkflows_WorkflowId",
                        column: x => x.WorkflowId,
                        principalTable: "ApprovalWorkflows",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ApprovalSteps",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    WorkflowId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Level = table.Column<int>(type: "int", nullable: false),
                    StepName = table.Column<string>(type: "nvarchar(180)", maxLength: 180, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    StartedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DueAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Comment = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    MetadataJson = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApprovalSteps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ApprovalSteps_ApprovalWorkflows_WorkflowId",
                        column: x => x.WorkflowId,
                        principalTable: "ApprovalWorkflows",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ApprovalAssignments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    WorkflowId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StepId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AssignedUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsPrimary = table.Column<bool>(type: "bit", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    AssignedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ActedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ActionComment = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApprovalAssignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ApprovalAssignments_ApprovalSteps_StepId",
                        column: x => x.StepId,
                        principalTable: "ApprovalSteps",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ApprovalAssignments_ApprovalWorkflows_WorkflowId",
                        column: x => x.WorkflowId,
                        principalTable: "ApprovalWorkflows",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ApprovalHistory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    WorkflowId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StepId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AssignmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Action = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    ActorUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    OccurredAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Comment = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    MetadataJson = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApprovalHistory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ApprovalHistory_ApprovalSteps_StepId",
                        column: x => x.StepId,
                        principalTable: "ApprovalSteps",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ApprovalHistory_ApprovalWorkflows_WorkflowId",
                        column: x => x.WorkflowId,
                        principalTable: "ApprovalWorkflows",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalAssignments_StepId_AssignedUserId",
                table: "ApprovalAssignments",
                columns: new[] { "StepId", "AssignedUserId" });

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalAssignments_WorkflowId",
                table: "ApprovalAssignments",
                column: "WorkflowId");

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalDelegations_AssignmentId_DelegatedAtUtc",
                table: "ApprovalDelegations",
                columns: new[] { "AssignmentId", "DelegatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalEscalations_WorkflowId_StepId_EscalatedAtUtc",
                table: "ApprovalEscalations",
                columns: new[] { "WorkflowId", "StepId", "EscalatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalHistory_StepId",
                table: "ApprovalHistory",
                column: "StepId");

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalHistory_WorkflowId_OccurredAtUtc",
                table: "ApprovalHistory",
                columns: new[] { "WorkflowId", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalSnapshots_WorkflowId_CapturedAtUtc",
                table: "ApprovalSnapshots",
                columns: new[] { "WorkflowId", "CapturedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalSteps_WorkflowId_Level",
                table: "ApprovalSteps",
                columns: new[] { "WorkflowId", "Level" });

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalWorkflows_CompanyId_BranchId_Module_SubmittedAtUtc",
                table: "ApprovalWorkflows",
                columns: new[] { "CompanyId", "BranchId", "Module", "SubmittedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalWorkflows_CorrelationId",
                table: "ApprovalWorkflows",
                column: "CorrelationId");

            migrationBuilder.AddForeignKey(
                name: "FK_ActiveCompanySessions_Branches_BranchId",
                table: "ActiveCompanySessions",
                column: "BranchId",
                principalTable: "Branches",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ActiveCompanySessions_Companies_CompanyId",
                table: "ActiveCompanySessions",
                column: "CompanyId",
                principalTable: "Companies",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_BranchSettings_Companies_CompanyId",
                table: "BranchSettings",
                column: "CompanyId",
                principalTable: "Companies",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_WorkflowCompensationActions_WorkflowExecutions_WorkflowExecutionId",
                table: "WorkflowCompensationActions",
                column: "WorkflowExecutionId",
                principalTable: "WorkflowExecutions",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_WorkflowExecutions_WorkflowExecutions_ParentWorkflowExecutionId",
                table: "WorkflowExecutions",
                column: "ParentWorkflowExecutionId",
                principalTable: "WorkflowExecutions",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_WorkflowRetryHistories_WorkflowExecutions_WorkflowExecutionId",
                table: "WorkflowRetryHistories",
                column: "WorkflowExecutionId",
                principalTable: "WorkflowExecutions",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ActiveCompanySessions_Branches_BranchId",
                table: "ActiveCompanySessions");

            migrationBuilder.DropForeignKey(
                name: "FK_ActiveCompanySessions_Companies_CompanyId",
                table: "ActiveCompanySessions");

            migrationBuilder.DropForeignKey(
                name: "FK_BranchSettings_Companies_CompanyId",
                table: "BranchSettings");

            migrationBuilder.DropForeignKey(
                name: "FK_WorkflowCompensationActions_WorkflowExecutions_WorkflowExecutionId",
                table: "WorkflowCompensationActions");

            migrationBuilder.DropForeignKey(
                name: "FK_WorkflowExecutions_WorkflowExecutions_ParentWorkflowExecutionId",
                table: "WorkflowExecutions");

            migrationBuilder.DropForeignKey(
                name: "FK_WorkflowRetryHistories_WorkflowExecutions_WorkflowExecutionId",
                table: "WorkflowRetryHistories");

            migrationBuilder.DropTable(
                name: "ApprovalAssignments");

            migrationBuilder.DropTable(
                name: "ApprovalDelegations");

            migrationBuilder.DropTable(
                name: "ApprovalEscalations");

            migrationBuilder.DropTable(
                name: "ApprovalHistory");

            migrationBuilder.DropTable(
                name: "ApprovalSnapshots");

            migrationBuilder.DropTable(
                name: "ApprovalSteps");

            migrationBuilder.DropTable(
                name: "ApprovalWorkflows");

            migrationBuilder.AddForeignKey(
                name: "FK_ActiveCompanySessions_Branches_BranchId",
                table: "ActiveCompanySessions",
                column: "BranchId",
                principalTable: "Branches",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_ActiveCompanySessions_Companies_CompanyId",
                table: "ActiveCompanySessions",
                column: "CompanyId",
                principalTable: "Companies",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_BranchSettings_Companies_CompanyId",
                table: "BranchSettings",
                column: "CompanyId",
                principalTable: "Companies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_WorkflowCompensationActions_WorkflowExecutions_WorkflowExecutionId",
                table: "WorkflowCompensationActions",
                column: "WorkflowExecutionId",
                principalTable: "WorkflowExecutions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_WorkflowExecutions_WorkflowExecutions_ParentWorkflowExecutionId",
                table: "WorkflowExecutions",
                column: "ParentWorkflowExecutionId",
                principalTable: "WorkflowExecutions",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_WorkflowRetryHistories_WorkflowExecutions_WorkflowExecutionId",
                table: "WorkflowRetryHistories",
                column: "WorkflowExecutionId",
                principalTable: "WorkflowExecutions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
