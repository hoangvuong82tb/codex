using System;
using ASMILE.BO.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ASMILE.BO.Infrastructure.Persistence.Migrations;

/// <summary>
/// Adds the persisted inspection record that was already present in the EF
/// model snapshot but had no corresponding schema operation in the migration
/// history.
/// </summary>
[DbContext(typeof(AppDbContext))]
[Migration("20260907130000_AddRoomInspectionTable")]
public partial class AddRoomInspectionTable : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "RoomInspection",
            schema: "fo",
            columns: table => new
            {
                InspectionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                HousekeepingTaskId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                PropertyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                InspectorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                Result = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                ChecklistData = table.Column<string>(type: "nvarchar(max)", nullable: true),
                InspectedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_fo_RoomInspection", x => x.InspectionId);
                table.ForeignKey(
                    name: "FK_RoomInspection_DonDep_HousekeepingTaskId",
                    column: x => x.HousekeepingTaskId,
                    principalSchema: "fo",
                    principalTable: "DonDep",
                    principalColumn: "HousekeepingTaskId",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_RoomInspection_HousekeepingTaskId",
            schema: "fo",
            table: "RoomInspection",
            column: "HousekeepingTaskId");

        migrationBuilder.CreateIndex(
            name: "IX_fo_RoomInspection_CoSo_DonDep",
            schema: "fo",
            table: "RoomInspection",
            columns: new[] { "PropertyId", "HousekeepingTaskId" },
            unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "RoomInspection",
            schema: "fo");
    }
}
