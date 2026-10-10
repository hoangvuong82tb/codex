using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ASMILE.BO.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTaskShiftPersistence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TaskAssignment",
                schema: "fo",
                columns: table => new
                {
                    TaskAssignmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PropertyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TaskNumber = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    TaskType = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    SourceModule = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Priority = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Department = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    AssignedShift = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    AssignedStaffId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AssignedStaffName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DueTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    StartedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EstimatedMinutes = table.Column<int>(type: "int", nullable: true),
                    ActualMinutes = table.Column<int>(type: "int", nullable: true),
                    RelatedEntityId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fo_TaskAssignment", x => x.TaskAssignmentId);
                });

            migrationBuilder.CreateTable(
                name: "TaskHistory",
                schema: "fo",
                columns: table => new
                {
                    TaskHistoryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TaskAssignmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PropertyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Action = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    OccurredAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fo_TaskHistory", x => x.TaskHistoryId);
                    table.ForeignKey(
                        name: "FK_TaskHistory_TaskAssignment_TaskAssignmentId",
                        column: x => x.TaskAssignmentId,
                        principalSchema: "fo",
                        principalTable: "TaskAssignment",
                        principalColumn: "TaskAssignmentId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_fo_TaskAssignment_CoSo_NhanSu_Ca",
                schema: "fo",
                table: "TaskAssignment",
                columns: new[] { "PropertyId", "AssignedStaffId", "AssignedShift" });

            migrationBuilder.CreateIndex(
                name: "IX_fo_TaskAssignment_CoSo_TrangThai_Han",
                schema: "fo",
                table: "TaskAssignment",
                columns: new[] { "PropertyId", "Status", "DueTime" });

            migrationBuilder.CreateIndex(
                name: "UQ_fo_TaskAssignment_CoSo_SoTask",
                schema: "fo",
                table: "TaskAssignment",
                columns: new[] { "PropertyId", "TaskNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_fo_TaskHistory_CoSo_Task_ThoiGian",
                schema: "fo",
                table: "TaskHistory",
                columns: new[] { "PropertyId", "TaskAssignmentId", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_TaskHistory_TaskAssignmentId",
                schema: "fo",
                table: "TaskHistory",
                column: "TaskAssignmentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TaskHistory",
                schema: "fo");

            migrationBuilder.DropTable(
                name: "TaskAssignment",
                schema: "fo");
        }
    }
}
