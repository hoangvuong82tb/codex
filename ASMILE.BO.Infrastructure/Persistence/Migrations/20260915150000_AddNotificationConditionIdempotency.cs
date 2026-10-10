using ASMILE.BO.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ASMILE.BO.Infrastructure.Persistence.Migrations;

/// <summary>
/// Prevents periodic real-condition producers from emitting duplicate alerts
/// when more than one application instance scans the same task.
/// </summary>
[DbContext(typeof(AppDbContext))]
[Migration("20260915150000_AddNotificationConditionIdempotency")]
public partial class AddNotificationConditionIdempotency : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "IdempotencyKey",
            table: "UserNotifications",
            type: "nvarchar(160)",
            maxLength: 160,
            nullable: true);

        migrationBuilder.CreateIndex(
            name: "UQ_UserNotifications_IdempotencyKey",
            table: "UserNotifications",
            column: "IdempotencyKey",
            unique: true,
            filter: "[IdempotencyKey] IS NOT NULL");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "UQ_UserNotifications_IdempotencyKey",
            table: "UserNotifications");

        migrationBuilder.DropColumn(
            name: "IdempotencyKey",
            table: "UserNotifications");
    }
}
