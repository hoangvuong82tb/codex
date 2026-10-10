using ASMILE.BO.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ASMILE.BO.Infrastructure.Persistence.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260913140000_AddPersistentAuthenticationSessions")]
public partial class AddPersistentAuthenticationSessions : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "LoginAudits",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                UserName = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                Success = table.Column<bool>(type: "bit", nullable: false),
                FailureReason = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                MachineName = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                IpAddress = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_LoginAudits", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "LoginSessions",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                TokenHash = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                MachineName = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                IpAddress = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                StartedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                EndedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                IsActive = table.Column<bool>(type: "bit", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_LoginSessions", x => x.Id);
                table.ForeignKey(
                    name: "FK_LoginSessions_AppUsers_UserId",
                    column: x => x.UserId,
                    principalTable: "AppUsers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_LoginAudits_CreatedAt",
            table: "LoginAudits",
            column: "CreatedAt");

        migrationBuilder.CreateIndex(
            name: "IX_LoginAudits_UserId_CreatedAt",
            table: "LoginAudits",
            columns: new[] { "UserId", "CreatedAt" });

        migrationBuilder.CreateIndex(
            name: "IX_LoginSessions_TokenHash",
            table: "LoginSessions",
            column: "TokenHash",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_LoginSessions_UserId_IsActive_ExpiresAt",
            table: "LoginSessions",
            columns: new[] { "UserId", "IsActive", "ExpiresAt" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "LoginSessions");
        migrationBuilder.DropTable(name: "LoginAudits");
    }
}
