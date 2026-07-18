using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ASMILE.BO.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class EnhanceEnterpriseAuditTrail : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AuditCorrelations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CorrelationId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    EntityType = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    EntityId = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Module = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Action = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditCorrelations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AuditEntitySnapshots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AuditLogId = table.Column<int>(type: "int", nullable: false),
                    EntityType = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    EntityId = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    BeforeJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AfterJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DiffJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SnapshotHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditEntitySnapshots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AuditEntitySnapshots_AuditLogs_AuditLogId",
                        column: x => x.AuditLogId,
                        principalTable: "AuditLogs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AuditExportHistory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequestedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ExportType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    FilterJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FilePath = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditExportHistory", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AuditReplayEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CorrelationId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SequenceNo = table.Column<int>(type: "int", nullable: false),
                    Route = table.Column<string>(type: "nvarchar(180)", maxLength: 180, nullable: false),
                    Module = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Action = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    EntityType = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    EntityId = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    MetadataJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TimestampUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditReplayEntries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AuditSessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CorrelationId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    StartedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Source = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    ClientMachine = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    AppVersion = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: true),
                    SessionMetadataJson = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditSessions", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AuditCorrelations_CorrelationId",
                table: "AuditCorrelations",
                column: "CorrelationId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditCorrelations_EntityType_EntityId",
                table: "AuditCorrelations",
                columns: new[] { "EntityType", "EntityId" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditCorrelations_Module_Action",
                table: "AuditCorrelations",
                columns: new[] { "Module", "Action" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditEntitySnapshots_AuditLogId",
                table: "AuditEntitySnapshots",
                column: "AuditLogId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditEntitySnapshots_EntityType_EntityId",
                table: "AuditEntitySnapshots",
                columns: new[] { "EntityType", "EntityId" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditExportHistory_CompanyId_BranchId_CreatedAtUtc",
                table: "AuditExportHistory",
                columns: new[] { "CompanyId", "BranchId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditExportHistory_CreatedAtUtc",
                table: "AuditExportHistory",
                column: "CreatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_AuditReplayEntries_CorrelationId",
                table: "AuditReplayEntries",
                column: "CorrelationId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditReplayEntries_CorrelationId_SequenceNo",
                table: "AuditReplayEntries",
                columns: new[] { "CorrelationId", "SequenceNo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AuditReplayEntries_EntityType_EntityId",
                table: "AuditReplayEntries",
                columns: new[] { "EntityType", "EntityId" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditReplayEntries_Module_Action",
                table: "AuditReplayEntries",
                columns: new[] { "Module", "Action" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditReplayEntries_TimestampUtc",
                table: "AuditReplayEntries",
                column: "TimestampUtc");

            migrationBuilder.CreateIndex(
                name: "IX_AuditSessions_CompanyId_BranchId_StartedAtUtc",
                table: "AuditSessions",
                columns: new[] { "CompanyId", "BranchId", "StartedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditSessions_CorrelationId",
                table: "AuditSessions",
                column: "CorrelationId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditCorrelations");

            migrationBuilder.DropTable(
                name: "AuditEntitySnapshots");

            migrationBuilder.DropTable(
                name: "AuditExportHistory");

            migrationBuilder.DropTable(
                name: "AuditReplayEntries");

            migrationBuilder.DropTable(
                name: "AuditSessions");
        }
    }
}
