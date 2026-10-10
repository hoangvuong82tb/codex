using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using ASMILE.BO.Infrastructure.Persistence;

#nullable disable

namespace ASMILE.BO.Infrastructure.Persistence.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260909110000_AddTrainingDataMarkersPersistence")]
public partial class AddTrainingDataMarkersPersistence : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "TrainingDataMarkers",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                TrainingDataRunId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Profile = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                EntityName = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                RecordId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_TrainingDataMarkers", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_TrainingDataMarkers_Profile_EntityName",
            table: "TrainingDataMarkers",
            columns: new[] { "Profile", "EntityName" });

        migrationBuilder.CreateIndex(
            name: "IX_TrainingDataMarkers_TrainingDataRunId_EntityName_RecordId",
            table: "TrainingDataMarkers",
            columns: new[] { "TrainingDataRunId", "EntityName", "RecordId" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "TrainingDataMarkers");
    }
}
