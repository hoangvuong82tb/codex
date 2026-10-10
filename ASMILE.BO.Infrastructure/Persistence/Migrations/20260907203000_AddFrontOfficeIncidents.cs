using System;
using ASMILE.BO.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ASMILE.BO.Infrastructure.Persistence.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260907203000_AddFrontOfficeIncidents")]
public partial class AddFrontOfficeIncidents : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "SuCo",
            schema: "fo",
            columns: table => new
            {
                IncidentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                PropertyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                IncidentNumber = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                OccurredAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                Title = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                Category = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                Priority = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                RoomReference = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                GuestReference = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                ReservationReference = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                Department = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                ReporterName = table.Column<string>(type: "nvarchar(180)", maxLength: 180, nullable: false),
                AssignedTo = table.Column<string>(type: "nvarchar(180)", maxLength: 180, nullable: true),
                Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                Impact = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                Description = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                RootCause = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                TemporaryAction = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                CorrectiveAction = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                PreventiveAction = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                IsVip = table.Column<bool>(type: "bit", nullable: false),
                CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                ResolvedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                ClosedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_fo_SuCo", x => x.IncidentId);
            });

        migrationBuilder.CreateIndex(
            name: "UQ_fo_SuCo_CoSo_MaSC",
            schema: "fo",
            table: "SuCo",
            columns: new[] { "PropertyId", "IncidentNumber" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_fo_SuCo_CoSo_ThoiGian",
            schema: "fo",
            table: "SuCo",
            columns: new[] { "PropertyId", "OccurredAtUtc" });

        migrationBuilder.CreateIndex(
            name: "IX_fo_SuCo_CoSo_TrangThai_UuTien",
            schema: "fo",
            table: "SuCo",
            columns: new[] { "PropertyId", "Status", "Priority" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "SuCo", schema: "fo");
    }
}
