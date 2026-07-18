using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ASMILE.BO.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEnterpriseRetentionAndArchival : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ArchivedAtUtc",
                table: "SearchIndexEntries",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsArchived",
                table: "SearchIndexEntries",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "ArchivedAtUtc",
                table: "RecentSearches",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsArchived",
                table: "RecentSearches",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "ArchiveBatches",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Module = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    EntityName = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    ItemCount = table.Column<int>(type: "int", nullable: false),
                    PayloadSizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    Hash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    MetadataJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ArchivedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    CorrelationId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ArchiveBatches", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ArchiveExportManifests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Module = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    EntityName = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    FilePath = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Hash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    PayloadSizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    MetadataJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ArchivedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    CorrelationId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ArchiveExportManifests", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DataRetentionPolicies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Module = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    EntityName = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    RetentionDays = table.Column<int>(type: "int", nullable: false),
                    ArchiveAfterDays = table.Column<int>(type: "int", nullable: false),
                    DeleteAfterDays = table.Column<int>(type: "int", nullable: false),
                    CompressArchivedPayload = table.Column<bool>(type: "bit", nullable: false),
                    EncryptArchive = table.Column<bool>(type: "bit", nullable: false),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    CorrelationId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    MetadataJson = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DataRetentionPolicies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RecoveryCheckpoints",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Module = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    EntityName = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    MigrationVersion = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    SchedulerStateJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    WorkspaceMetadataJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AuditSessionMetadataJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MetadataJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Hash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    PayloadSizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ArchivedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    CorrelationId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecoveryCheckpoints", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ArchiveBatchItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ArchiveBatchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Module = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    EntityName = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    EntityId = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    PayloadSizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    Hash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    MetadataJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PayloadJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CompressedPayload = table.Column<byte[]>(type: "varbinary(max)", nullable: true),
                    CompressionType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "None"),
                    ArchivedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    CorrelationId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ArchiveBatchItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ArchiveBatchItems_ArchiveBatches_ArchiveBatchId",
                        column: x => x.ArchiveBatchId,
                        principalTable: "ArchiveBatches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RecoveryOperations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RecoveryCheckpointId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    OperationType = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    MetadataJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Hash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    PayloadSizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    StartedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    CorrelationId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ErrorMessage = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecoveryOperations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RecoveryOperations_RecoveryCheckpoints_RecoveryCheckpointId",
                        column: x => x.RecoveryCheckpointId,
                        principalTable: "RecoveryCheckpoints",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SearchIndexEntries_IsArchived",
                table: "SearchIndexEntries",
                column: "IsArchived");

            migrationBuilder.CreateIndex(
                name: "IX_RecentSearches_IsArchived",
                table: "RecentSearches",
                column: "IsArchived");

            migrationBuilder.CreateIndex(
                name: "IX_ArchiveBatches_CompanyId_BranchId_Module_EntityName_CreatedAtUtc",
                table: "ArchiveBatches",
                columns: new[] { "CompanyId", "BranchId", "Module", "EntityName", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ArchiveBatches_Status",
                table: "ArchiveBatches",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ArchiveBatchItems_ArchiveBatchId",
                table: "ArchiveBatchItems",
                column: "ArchiveBatchId");

            migrationBuilder.CreateIndex(
                name: "IX_ArchiveBatchItems_CompanyId_BranchId_Module_EntityName",
                table: "ArchiveBatchItems",
                columns: new[] { "CompanyId", "BranchId", "Module", "EntityName" });

            migrationBuilder.CreateIndex(
                name: "IX_ArchiveBatchItems_EntityName_EntityId",
                table: "ArchiveBatchItems",
                columns: new[] { "EntityName", "EntityId" });

            migrationBuilder.CreateIndex(
                name: "IX_ArchiveExportManifests_CompanyId_BranchId_Module_CreatedAtUtc",
                table: "ArchiveExportManifests",
                columns: new[] { "CompanyId", "BranchId", "Module", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ArchiveExportManifests_Status",
                table: "ArchiveExportManifests",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_DataRetentionPolicies_CompanyId_BranchId_Module_EntityName",
                table: "DataRetentionPolicies",
                columns: new[] { "CompanyId", "BranchId", "Module", "EntityName" },
                unique: true,
                filter: "[CompanyId] IS NOT NULL AND [BranchId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_DataRetentionPolicies_IsEnabled",
                table: "DataRetentionPolicies",
                column: "IsEnabled");

            migrationBuilder.CreateIndex(
                name: "IX_RecoveryCheckpoints_CompanyId_BranchId_Module_CreatedAtUtc",
                table: "RecoveryCheckpoints",
                columns: new[] { "CompanyId", "BranchId", "Module", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_RecoveryCheckpoints_Status",
                table: "RecoveryCheckpoints",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_RecoveryOperations_CompanyId_BranchId_OperationType_CreatedAtUtc",
                table: "RecoveryOperations",
                columns: new[] { "CompanyId", "BranchId", "OperationType", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_RecoveryOperations_RecoveryCheckpointId",
                table: "RecoveryOperations",
                column: "RecoveryCheckpointId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ArchiveBatchItems");

            migrationBuilder.DropTable(
                name: "ArchiveExportManifests");

            migrationBuilder.DropTable(
                name: "DataRetentionPolicies");

            migrationBuilder.DropTable(
                name: "RecoveryOperations");

            migrationBuilder.DropTable(
                name: "ArchiveBatches");

            migrationBuilder.DropTable(
                name: "RecoveryCheckpoints");

            migrationBuilder.DropIndex(
                name: "IX_SearchIndexEntries_IsArchived",
                table: "SearchIndexEntries");

            migrationBuilder.DropIndex(
                name: "IX_RecentSearches_IsArchived",
                table: "RecentSearches");

            migrationBuilder.DropColumn(
                name: "ArchivedAtUtc",
                table: "SearchIndexEntries");

            migrationBuilder.DropColumn(
                name: "IsArchived",
                table: "SearchIndexEntries");

            migrationBuilder.DropColumn(
                name: "ArchivedAtUtc",
                table: "RecentSearches");

            migrationBuilder.DropColumn(
                name: "IsArchived",
                table: "RecentSearches");
        }
    }
}
