using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ASMILE.BO.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CompleteEnterpriseAuditLifecycle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AuditEntitySnapshots_AuditLogs_AuditLogId",
                table: "AuditEntitySnapshots");

            migrationBuilder.AddColumn<string>(
                name: "CompletedReason",
                table: "AuditSessions",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeviceId",
                table: "AuditSessions",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "DurationMs",
                table: "AuditSessions",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastActivityAtUtc",
                table: "AuditSessions",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "Platform",
                table: "AuditSessions",
                type: "nvarchar(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "AuditSessions",
                type: "nvarchar(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "Started");

            migrationBuilder.AddColumn<Guid>(
                name: "AuditSessionId",
                table: "AuditReplayEntries",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ParentCorrelationId",
                table: "AuditReplayEntries",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CorrelationCoverageJson",
                table: "AuditExportHistory",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExportHash",
                table: "AuditExportHistory",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HashAlgorithm",
                table: "AuditExportHistory",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "SHA256");

            migrationBuilder.AddColumn<string>(
                name: "IntegrityManifestJson",
                table: "AuditExportHistory",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RowCount",
                table: "AuditExportHistory",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<int>(
                name: "AuditLogId",
                table: "AuditEntitySnapshots",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<Guid>(
                name: "BranchId",
                table: "AuditEntitySnapshots",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CompanyId",
                table: "AuditEntitySnapshots",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "CompressedPayload",
                table: "AuditEntitySnapshots",
                type: "varbinary(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CompressionType",
                table: "AuditEntitySnapshots",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "None");

            migrationBuilder.AddColumn<string>(
                name: "HashAlgorithm",
                table: "AuditEntitySnapshots",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "SHA256");

            migrationBuilder.AddColumn<long>(
                name: "SnapshotSize",
                table: "AuditEntitySnapshots",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<Guid>(
                name: "AuditSessionId",
                table: "AuditCorrelations",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ParentCorrelationId",
                table: "AuditCorrelations",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE AuditSessions
                SET LastActivityAtUtc = CASE WHEN CompletedAtUtc IS NULL THEN StartedAtUtc ELSE CompletedAtUtc END,
                    Status = CASE WHEN CompletedAtUtc IS NULL THEN 'Active' ELSE 'Completed' END,
                    DurationMs = CASE
                        WHEN CompletedAtUtc IS NULL THEN 0
                        ELSE DATEDIFF_BIG(MILLISECOND, StartedAtUtc, CompletedAtUtc)
                    END
                WHERE LastActivityAtUtc = '0001-01-01T00:00:00.0000000';
                """);

            migrationBuilder.Sql("""
                UPDATE s
                SET s.CompanyId = l.CompanyId,
                    s.BranchId = l.BranchId,
                    s.SnapshotSize = DATALENGTH(COALESCE(s.BeforeJson, N'')) + DATALENGTH(COALESCE(s.AfterJson, N'')) + DATALENGTH(COALESCE(s.DiffJson, N''))
                FROM AuditEntitySnapshots s
                INNER JOIN AuditLogs l ON s.AuditLogId = l.Id
                WHERE s.CompanyId IS NULL;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_AuditSessions_Status_LastActivityAtUtc",
                table: "AuditSessions",
                columns: new[] { "Status", "LastActivityAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditReplayEntries_AuditSessionId",
                table: "AuditReplayEntries",
                column: "AuditSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditReplayEntries_ParentCorrelationId",
                table: "AuditReplayEntries",
                column: "ParentCorrelationId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditEntitySnapshots_CompanyId_BranchId_EntityType_EntityId",
                table: "AuditEntitySnapshots",
                columns: new[] { "CompanyId", "BranchId", "EntityType", "EntityId" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditCorrelations_AuditSessionId",
                table: "AuditCorrelations",
                column: "AuditSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditCorrelations_ParentCorrelationId",
                table: "AuditCorrelations",
                column: "ParentCorrelationId");

            migrationBuilder.AddForeignKey(
                name: "FK_AuditEntitySnapshots_AuditLogs_AuditLogId",
                table: "AuditEntitySnapshots",
                column: "AuditLogId",
                principalTable: "AuditLogs",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AuditEntitySnapshots_AuditLogs_AuditLogId",
                table: "AuditEntitySnapshots");

            migrationBuilder.DropIndex(
                name: "IX_AuditSessions_Status_LastActivityAtUtc",
                table: "AuditSessions");

            migrationBuilder.DropIndex(
                name: "IX_AuditReplayEntries_AuditSessionId",
                table: "AuditReplayEntries");

            migrationBuilder.DropIndex(
                name: "IX_AuditReplayEntries_ParentCorrelationId",
                table: "AuditReplayEntries");

            migrationBuilder.DropIndex(
                name: "IX_AuditEntitySnapshots_CompanyId_BranchId_EntityType_EntityId",
                table: "AuditEntitySnapshots");

            migrationBuilder.DropIndex(
                name: "IX_AuditCorrelations_AuditSessionId",
                table: "AuditCorrelations");

            migrationBuilder.DropIndex(
                name: "IX_AuditCorrelations_ParentCorrelationId",
                table: "AuditCorrelations");

            migrationBuilder.DropColumn(
                name: "CompletedReason",
                table: "AuditSessions");

            migrationBuilder.DropColumn(
                name: "DeviceId",
                table: "AuditSessions");

            migrationBuilder.DropColumn(
                name: "DurationMs",
                table: "AuditSessions");

            migrationBuilder.DropColumn(
                name: "LastActivityAtUtc",
                table: "AuditSessions");

            migrationBuilder.DropColumn(
                name: "Platform",
                table: "AuditSessions");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "AuditSessions");

            migrationBuilder.DropColumn(
                name: "AuditSessionId",
                table: "AuditReplayEntries");

            migrationBuilder.DropColumn(
                name: "ParentCorrelationId",
                table: "AuditReplayEntries");

            migrationBuilder.DropColumn(
                name: "CorrelationCoverageJson",
                table: "AuditExportHistory");

            migrationBuilder.DropColumn(
                name: "ExportHash",
                table: "AuditExportHistory");

            migrationBuilder.DropColumn(
                name: "HashAlgorithm",
                table: "AuditExportHistory");

            migrationBuilder.DropColumn(
                name: "IntegrityManifestJson",
                table: "AuditExportHistory");

            migrationBuilder.DropColumn(
                name: "RowCount",
                table: "AuditExportHistory");

            migrationBuilder.DropColumn(
                name: "BranchId",
                table: "AuditEntitySnapshots");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "AuditEntitySnapshots");

            migrationBuilder.DropColumn(
                name: "CompressedPayload",
                table: "AuditEntitySnapshots");

            migrationBuilder.DropColumn(
                name: "CompressionType",
                table: "AuditEntitySnapshots");

            migrationBuilder.DropColumn(
                name: "HashAlgorithm",
                table: "AuditEntitySnapshots");

            migrationBuilder.DropColumn(
                name: "SnapshotSize",
                table: "AuditEntitySnapshots");

            migrationBuilder.DropColumn(
                name: "AuditSessionId",
                table: "AuditCorrelations");

            migrationBuilder.DropColumn(
                name: "ParentCorrelationId",
                table: "AuditCorrelations");

            migrationBuilder.AlterColumn<int>(
                name: "AuditLogId",
                table: "AuditEntitySnapshots",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_AuditEntitySnapshots_AuditLogs_AuditLogId",
                table: "AuditEntitySnapshots",
                column: "AuditLogId",
                principalTable: "AuditLogs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
