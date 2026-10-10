using ASMILE.BO.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ASMILE.BO.Infrastructure.Persistence.Migrations;

/// <summary>
/// Keeps notification comments and reactions in their own append-only table.
/// This prevents interaction data from overwriting the notification's source metadata.
/// </summary>
[DbContext(typeof(AppDbContext))]
[Migration("20260915133000_AddNotificationCommentPersistence")]
public partial class AddNotificationCommentPersistence : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "NotificationComments",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                NotificationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                Kind = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                CommentText = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                CreatedByName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_NotificationComments", x => x.Id);
                table.ForeignKey(
                    name: "FK_NotificationComments_UserNotifications_NotificationId",
                    column: x => x.NotificationId,
                    principalTable: "UserNotifications",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_NotificationComments_NotificationId",
            table: "NotificationComments",
            column: "NotificationId");

        migrationBuilder.CreateIndex(
            name: "IX_NotificationComments_NotificationId_UserId_CreatedAt",
            table: "NotificationComments",
            columns: new[] { "NotificationId", "UserId", "CreatedAt" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "NotificationComments");
    }
}
