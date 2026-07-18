using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ASMILE.BO.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPhase4ExecutionFoundationStep46To412 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ComplianceArchives",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CompliancePackageId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ArchivePath = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Hash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsVerified = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ComplianceArchives", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ComplianceManifests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CompliancePackageId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ManifestType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    FilePath = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Hash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    RowCount = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ComplianceManifests", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CompliancePackages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PackageCode = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    StandardCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    PeriodCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CorrelationId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompliancePackages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ComplianceSubmissions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CompliancePackageId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SubmissionChannel = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    ReferenceNo = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    MetadataJson = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ComplianceSubmissions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DataIntegrityCheckpoints",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CheckpointCode = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SnapshotHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    SnapshotJson = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DataIntegrityCheckpoints", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DataIntegrityScanResults",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Module = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Severity = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Message = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    EntityType = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    EntityId = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    IsCorrupted = table.Column<bool>(type: "bit", nullable: false),
                    ScannedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    MetadataJson = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DataIntegrityScanResults", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DataRepairOperations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    OperationCode = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    RequestedBy = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    RequestedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ResultJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ErrorMessage = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CorrelationId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DataRepairOperations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ExportArtifacts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ExportExecutionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FilePath = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    FileType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    SizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    Hash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExportArtifacts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ExportExecutions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ExportCode = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    ExportType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    RetryCount = table.Column<int>(type: "int", nullable: false),
                    QueuedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    StartedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ErrorMessage = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CorrelationId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExportExecutions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ExportRetentionPolicies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ExportCode = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    RetentionDays = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExportRetentionPolicies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProductionCertificationReports",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PhaseCode = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    GeneratedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReportJson = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductionCertificationReports", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ReconciliationAnomalies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SessionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Severity = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Message = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    RelatedEntityType = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    RelatedEntityId = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    DetectedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    MetadataJson = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReconciliationAnomalies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ReconciliationMatches",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SessionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SourceType = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    SourceId = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    TargetType = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    TargetId = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    MatchScore = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    IsFuzzy = table.Column<bool>(type: "bit", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    MetadataJson = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReconciliationMatches", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ReconciliationRules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RuleCode = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(180)", maxLength: 180, nullable: false),
                    Module = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Priority = table.Column<int>(type: "int", nullable: false),
                    IsFuzzy = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    DefinitionJson = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReconciliationRules", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ReconciliationSessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Module = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    SessionCode = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    StartedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CorrelationId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    MetadataJson = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReconciliationSessions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ReconciliationSnapshots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SessionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CapturedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SnapshotJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SnapshotHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReconciliationSnapshots", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ReportSnapshots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ReportCode = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    VersionCode = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    PeriodCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CapturedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SnapshotJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SnapshotHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReportSnapshots", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ComplianceArchives_CompliancePackageId_CreatedAtUtc",
                table: "ComplianceArchives",
                columns: new[] { "CompliancePackageId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ComplianceManifests_CompliancePackageId_ManifestType",
                table: "ComplianceManifests",
                columns: new[] { "CompliancePackageId", "ManifestType" });

            migrationBuilder.CreateIndex(
                name: "IX_CompliancePackages_CompanyId_BranchId_PackageCode",
                table: "CompliancePackages",
                columns: new[] { "CompanyId", "BranchId", "PackageCode" },
                unique: true,
                filter: "[CompanyId] IS NOT NULL AND [BranchId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ComplianceSubmissions_CompliancePackageId_CreatedAtUtc",
                table: "ComplianceSubmissions",
                columns: new[] { "CompliancePackageId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_DataIntegrityCheckpoints_CompanyId_BranchId_CheckpointCode_CreatedAtUtc",
                table: "DataIntegrityCheckpoints",
                columns: new[] { "CompanyId", "BranchId", "CheckpointCode", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_DataIntegrityScanResults_CompanyId_BranchId_Module_ScannedAtUtc",
                table: "DataIntegrityScanResults",
                columns: new[] { "CompanyId", "BranchId", "Module", "ScannedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_DataRepairOperations_CompanyId_BranchId_OperationCode_RequestedAtUtc",
                table: "DataRepairOperations",
                columns: new[] { "CompanyId", "BranchId", "OperationCode", "RequestedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ExportArtifacts_ExportExecutionId_CreatedAtUtc",
                table: "ExportArtifacts",
                columns: new[] { "ExportExecutionId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ExportExecutions_CompanyId_BranchId_ExportCode_QueuedAtUtc",
                table: "ExportExecutions",
                columns: new[] { "CompanyId", "BranchId", "ExportCode", "QueuedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ExportRetentionPolicies_CompanyId_BranchId_ExportCode",
                table: "ExportRetentionPolicies",
                columns: new[] { "CompanyId", "BranchId", "ExportCode" },
                unique: true,
                filter: "[CompanyId] IS NOT NULL AND [BranchId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionCertificationReports_CompanyId_BranchId_PhaseCode_GeneratedAtUtc",
                table: "ProductionCertificationReports",
                columns: new[] { "CompanyId", "BranchId", "PhaseCode", "GeneratedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ReconciliationAnomalies_SessionId_Severity_DetectedAtUtc",
                table: "ReconciliationAnomalies",
                columns: new[] { "SessionId", "Severity", "DetectedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ReconciliationMatches_SessionId_SourceType_TargetType",
                table: "ReconciliationMatches",
                columns: new[] { "SessionId", "SourceType", "TargetType" });

            migrationBuilder.CreateIndex(
                name: "IX_ReconciliationRules_CompanyId_BranchId_RuleCode",
                table: "ReconciliationRules",
                columns: new[] { "CompanyId", "BranchId", "RuleCode" },
                unique: true,
                filter: "[CompanyId] IS NOT NULL AND [BranchId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ReconciliationSessions_CompanyId_BranchId_Module_StartedAtUtc",
                table: "ReconciliationSessions",
                columns: new[] { "CompanyId", "BranchId", "Module", "StartedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ReconciliationSnapshots_SessionId_CapturedAtUtc",
                table: "ReconciliationSnapshots",
                columns: new[] { "SessionId", "CapturedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ReportSnapshots_CompanyId_BranchId_ReportCode_PeriodCode_CapturedAtUtc",
                table: "ReportSnapshots",
                columns: new[] { "CompanyId", "BranchId", "ReportCode", "PeriodCode", "CapturedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ComplianceArchives");

            migrationBuilder.DropTable(
                name: "ComplianceManifests");

            migrationBuilder.DropTable(
                name: "CompliancePackages");

            migrationBuilder.DropTable(
                name: "ComplianceSubmissions");

            migrationBuilder.DropTable(
                name: "DataIntegrityCheckpoints");

            migrationBuilder.DropTable(
                name: "DataIntegrityScanResults");

            migrationBuilder.DropTable(
                name: "DataRepairOperations");

            migrationBuilder.DropTable(
                name: "ExportArtifacts");

            migrationBuilder.DropTable(
                name: "ExportExecutions");

            migrationBuilder.DropTable(
                name: "ExportRetentionPolicies");

            migrationBuilder.DropTable(
                name: "ProductionCertificationReports");

            migrationBuilder.DropTable(
                name: "ReconciliationAnomalies");

            migrationBuilder.DropTable(
                name: "ReconciliationMatches");

            migrationBuilder.DropTable(
                name: "ReconciliationRules");

            migrationBuilder.DropTable(
                name: "ReconciliationSessions");

            migrationBuilder.DropTable(
                name: "ReconciliationSnapshots");

            migrationBuilder.DropTable(
                name: "ReportSnapshots");
        }
    }
}
