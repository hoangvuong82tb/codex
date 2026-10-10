using ASMILE.BO.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ASMILE.BO.Infrastructure.Persistence.Migrations;

/// <summary>
/// Adds durable credential and account-lock state without replaying unrelated
/// model-snapshot drift into the production database.
/// </summary>
[DbContext(typeof(AppDbContext))]
[Migration("20260913103000_AddPersistentUserCredentials")]
public partial class AddPersistentUserCredentials : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "PasswordHash",
            table: "AppUsers",
            type: "nvarchar(512)",
            maxLength: 512,
            nullable: false,
            defaultValue: string.Empty);

        migrationBuilder.AddColumn<bool>(
            name: "IsLocked",
            table: "AppUsers",
            type: "bit",
            nullable: false,
            defaultValue: false);

        migrationBuilder.AddColumn<bool>(
            name: "MustChangePassword",
            table: "AppUsers",
            type: "bit",
            nullable: false,
            defaultValue: true);

        migrationBuilder.AddColumn<int>(
            name: "FailedLoginCount",
            table: "AppUsers",
            type: "int",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<DateTime>(
            name: "LastLoginAt",
            table: "AppUsers",
            type: "datetime2",
            nullable: true);

        migrationBuilder.AddColumn<DateTime>(
            name: "LastPasswordChangedAt",
            table: "AppUsers",
            type: "datetime2",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "Notes",
            table: "AppUsers",
            type: "nvarchar(1000)",
            maxLength: 1000,
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "PasswordHash", table: "AppUsers");
        migrationBuilder.DropColumn(name: "IsLocked", table: "AppUsers");
        migrationBuilder.DropColumn(name: "MustChangePassword", table: "AppUsers");
        migrationBuilder.DropColumn(name: "FailedLoginCount", table: "AppUsers");
        migrationBuilder.DropColumn(name: "LastLoginAt", table: "AppUsers");
        migrationBuilder.DropColumn(name: "LastPasswordChangedAt", table: "AppUsers");
        migrationBuilder.DropColumn(name: "Notes", table: "AppUsers");
    }
}
