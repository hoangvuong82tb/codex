using ASMILE.BO.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ASMILE.BO.Infrastructure.Persistence.Migrations;

/// <summary>
/// Persists acknowledgement and dismissal per user so broadcast notifications
/// do not leak one user's workflow state to another user.
/// </summary>
[DbContext(typeof(AppDbContext))]
[Migration("20260915140000_AddNotificationUserState")]
public partial class AddNotificationUserState : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "NotificationUserStates",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                NotificationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                IsAcknowledged = table.Column<bool>(type: "bit", nullable: false),
                AcknowledgedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                IsDismissed = table.Column<bool>(type: "bit", nullable: false),
                DismissedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_NotificationUserStates", x => x.Id);
                table.ForeignKey(
                    name: "FK_NotificationUserStates_UserNotifications_NotificationId",
                    column: x => x.NotificationId,
                    principalTable: "UserNotifications",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_NotificationUserStates_NotificationId_UserId",
            table: "NotificationUserStates",
            columns: new[] { "NotificationId", "UserId" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_NotificationUserStates_UserId_IsDismissed_UpdatedAt",
            table: "NotificationUserStates",
            columns: new[] { "UserId", "IsDismissed", "UpdatedAt" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "NotificationUserStates");
    }
}
