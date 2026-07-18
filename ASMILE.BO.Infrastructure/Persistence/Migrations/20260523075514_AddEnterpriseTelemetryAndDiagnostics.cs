using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ASMILE.BO.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEnterpriseTelemetryAndDiagnostics : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RecoveryRecommendations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CorrelationId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    SessionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Module = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Severity = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    RecommendationCode = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    SuggestedAction = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Message = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    ConfidenceScore = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    MetadataJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MachineName = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    UserName = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    RecordedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsArchived = table.Column<bool>(type: "bit", nullable: false),
                    ArchivedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecoveryRecommendations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RuntimeExceptionLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CorrelationId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    SessionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Module = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Severity = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    ExceptionType = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    StackHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Message = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    MetadataJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OccurrenceCount = table.Column<int>(type: "int", nullable: false),
                    MachineName = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    UserName = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    RecordedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsArchived = table.Column<bool>(type: "bit", nullable: false),
                    ArchivedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RuntimeExceptionLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RuntimeMetricSnapshots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CorrelationId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    SessionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Module = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Severity = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    MetricName = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    MetricValue = table.Column<double>(type: "float", nullable: false),
                    Unit = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    Message = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    MetadataJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MachineName = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    UserName = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    RecordedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsArchived = table.Column<bool>(type: "bit", nullable: false),
                    ArchivedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RuntimeMetricSnapshots", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SlowOperationLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CorrelationId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    SessionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Module = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Severity = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    OperationName = table.Column<string>(type: "nvarchar(180)", maxLength: 180, nullable: false),
                    DurationMs = table.Column<long>(type: "bigint", nullable: false),
                    Message = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    MetadataJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MachineName = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    UserName = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    RecordedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsArchived = table.Column<bool>(type: "bit", nullable: false),
                    ArchivedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SlowOperationLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SystemResourceSnapshots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CorrelationId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    SessionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Module = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Severity = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    CpuUsagePercent = table.Column<double>(type: "float", nullable: false),
                    MemoryUsageBytes = table.Column<long>(type: "bigint", nullable: false),
                    WorkingSetBytes = table.Column<long>(type: "bigint", nullable: false),
                    ThreadCount = table.Column<int>(type: "int", nullable: false),
                    Message = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    MetadataJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MachineName = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    UserName = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    RecordedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsArchived = table.Column<bool>(type: "bit", nullable: false),
                    ArchivedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SystemResourceSnapshots", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TelemetryEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CorrelationId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    SessionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Module = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Severity = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    EventType = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    EventName = table.Column<string>(type: "nvarchar(180)", maxLength: 180, nullable: false),
                    Message = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    MetadataJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SearchText = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    MachineName = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    UserName = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    RecordedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsArchived = table.Column<bool>(type: "bit", nullable: false),
                    ArchivedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TelemetryEvents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UiResponsivenessSnapshots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CorrelationId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    SessionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Module = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Severity = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    OperationName = table.Column<string>(type: "nvarchar(180)", maxLength: 180, nullable: false),
                    DurationMs = table.Column<long>(type: "bigint", nullable: false),
                    IsFrozen = table.Column<bool>(type: "bit", nullable: false),
                    Message = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    MetadataJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MachineName = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    UserName = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    RecordedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsArchived = table.Column<bool>(type: "bit", nullable: false),
                    ArchivedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UiResponsivenessSnapshots", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RecoveryRecommendations_CompanyId_BranchId_Module_RecordedAtUtc",
                table: "RecoveryRecommendations",
                columns: new[] { "CompanyId", "BranchId", "Module", "RecordedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_RecoveryRecommendations_CorrelationId",
                table: "RecoveryRecommendations",
                column: "CorrelationId");

            migrationBuilder.CreateIndex(
                name: "IX_RecoveryRecommendations_RecommendationCode",
                table: "RecoveryRecommendations",
                column: "RecommendationCode");

            migrationBuilder.CreateIndex(
                name: "IX_RecoveryRecommendations_RecordedAtUtc",
                table: "RecoveryRecommendations",
                column: "RecordedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_RecoveryRecommendations_SessionId",
                table: "RecoveryRecommendations",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_RuntimeExceptionLogs_CompanyId_BranchId_Module_RecordedAtUtc",
                table: "RuntimeExceptionLogs",
                columns: new[] { "CompanyId", "BranchId", "Module", "RecordedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_RuntimeExceptionLogs_CorrelationId",
                table: "RuntimeExceptionLogs",
                column: "CorrelationId");

            migrationBuilder.CreateIndex(
                name: "IX_RuntimeExceptionLogs_ExceptionType_StackHash",
                table: "RuntimeExceptionLogs",
                columns: new[] { "ExceptionType", "StackHash" });

            migrationBuilder.CreateIndex(
                name: "IX_RuntimeExceptionLogs_IsArchived",
                table: "RuntimeExceptionLogs",
                column: "IsArchived");

            migrationBuilder.CreateIndex(
                name: "IX_RuntimeExceptionLogs_RecordedAtUtc",
                table: "RuntimeExceptionLogs",
                column: "RecordedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_RuntimeExceptionLogs_SessionId",
                table: "RuntimeExceptionLogs",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_RuntimeMetricSnapshots_CompanyId_BranchId_Module_RecordedAtUtc",
                table: "RuntimeMetricSnapshots",
                columns: new[] { "CompanyId", "BranchId", "Module", "RecordedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_RuntimeMetricSnapshots_CorrelationId",
                table: "RuntimeMetricSnapshots",
                column: "CorrelationId");

            migrationBuilder.CreateIndex(
                name: "IX_RuntimeMetricSnapshots_IsArchived",
                table: "RuntimeMetricSnapshots",
                column: "IsArchived");

            migrationBuilder.CreateIndex(
                name: "IX_RuntimeMetricSnapshots_RecordedAtUtc",
                table: "RuntimeMetricSnapshots",
                column: "RecordedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_RuntimeMetricSnapshots_SessionId",
                table: "RuntimeMetricSnapshots",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_SlowOperationLogs_CompanyId_BranchId_Module_RecordedAtUtc",
                table: "SlowOperationLogs",
                columns: new[] { "CompanyId", "BranchId", "Module", "RecordedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_SlowOperationLogs_CorrelationId",
                table: "SlowOperationLogs",
                column: "CorrelationId");

            migrationBuilder.CreateIndex(
                name: "IX_SlowOperationLogs_DurationMs",
                table: "SlowOperationLogs",
                column: "DurationMs");

            migrationBuilder.CreateIndex(
                name: "IX_SlowOperationLogs_RecordedAtUtc",
                table: "SlowOperationLogs",
                column: "RecordedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_SlowOperationLogs_SessionId",
                table: "SlowOperationLogs",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_SystemResourceSnapshots_CompanyId_BranchId_Module_RecordedAtUtc",
                table: "SystemResourceSnapshots",
                columns: new[] { "CompanyId", "BranchId", "Module", "RecordedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_SystemResourceSnapshots_CorrelationId",
                table: "SystemResourceSnapshots",
                column: "CorrelationId");

            migrationBuilder.CreateIndex(
                name: "IX_SystemResourceSnapshots_RecordedAtUtc",
                table: "SystemResourceSnapshots",
                column: "RecordedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_SystemResourceSnapshots_SessionId",
                table: "SystemResourceSnapshots",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_TelemetryEvents_CompanyId_BranchId_Module_RecordedAtUtc",
                table: "TelemetryEvents",
                columns: new[] { "CompanyId", "BranchId", "Module", "RecordedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_TelemetryEvents_CorrelationId",
                table: "TelemetryEvents",
                column: "CorrelationId");

            migrationBuilder.CreateIndex(
                name: "IX_TelemetryEvents_IsArchived_RecordedAtUtc",
                table: "TelemetryEvents",
                columns: new[] { "IsArchived", "RecordedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_TelemetryEvents_RecordedAtUtc",
                table: "TelemetryEvents",
                column: "RecordedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_TelemetryEvents_SessionId",
                table: "TelemetryEvents",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_UiResponsivenessSnapshots_CompanyId_BranchId_Module_RecordedAtUtc",
                table: "UiResponsivenessSnapshots",
                columns: new[] { "CompanyId", "BranchId", "Module", "RecordedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_UiResponsivenessSnapshots_CorrelationId",
                table: "UiResponsivenessSnapshots",
                column: "CorrelationId");

            migrationBuilder.CreateIndex(
                name: "IX_UiResponsivenessSnapshots_IsFrozen",
                table: "UiResponsivenessSnapshots",
                column: "IsFrozen");

            migrationBuilder.CreateIndex(
                name: "IX_UiResponsivenessSnapshots_RecordedAtUtc",
                table: "UiResponsivenessSnapshots",
                column: "RecordedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_UiResponsivenessSnapshots_SessionId",
                table: "UiResponsivenessSnapshots",
                column: "SessionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RecoveryRecommendations");

            migrationBuilder.DropTable(
                name: "RuntimeExceptionLogs");

            migrationBuilder.DropTable(
                name: "RuntimeMetricSnapshots");

            migrationBuilder.DropTable(
                name: "SlowOperationLogs");

            migrationBuilder.DropTable(
                name: "SystemResourceSnapshots");

            migrationBuilder.DropTable(
                name: "TelemetryEvents");

            migrationBuilder.DropTable(
                name: "UiResponsivenessSnapshots");
        }
    }
}
