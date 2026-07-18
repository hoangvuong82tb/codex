using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ASMILE.BO.Infrastructure.Persistence.Migrations
{
    public partial class AddEnterpriseSearchIndex : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SearchIndexEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    EntityType = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    EntityId = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Subtitle = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    TargetType = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    TargetRoute = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    TargetGuid = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SourceType = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    SearchText = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    BaseScore = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IndexedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table => table.PrimaryKey("PK_SearchIndexEntries", x => x.Id));

            migrationBuilder.CreateTable(
                name: "RecentSearches",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Query = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    SelectedEntityType = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    SelectedEntityId = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    SearchedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table => table.PrimaryKey("PK_RecentSearches", x => x.Id));

            migrationBuilder.CreateIndex(name: "IX_SearchIndexEntries_EntityType_EntityId_CompanyId_BranchId", table: "SearchIndexEntries", columns: new[] { "EntityType", "EntityId", "CompanyId", "BranchId" }, unique: true, filter: "[CompanyId] IS NOT NULL");
            migrationBuilder.CreateIndex(name: "IX_SearchIndexEntries_CompanyId_BranchId_Category", table: "SearchIndexEntries", columns: new[] { "CompanyId", "BranchId", "Category" });
            migrationBuilder.CreateIndex(name: "IX_SearchIndexEntries_TargetRoute", table: "SearchIndexEntries", column: "TargetRoute");
            migrationBuilder.CreateIndex(name: "IX_SearchIndexEntries_IsActive", table: "SearchIndexEntries", column: "IsActive");
            migrationBuilder.CreateIndex(name: "IX_RecentSearches_UserId_CompanyId_BranchId_SearchedAt", table: "RecentSearches", columns: new[] { "UserId", "CompanyId", "BranchId", "SearchedAt" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "RecentSearches");
            migrationBuilder.DropTable(name: "SearchIndexEntries");
        }
    }
}
