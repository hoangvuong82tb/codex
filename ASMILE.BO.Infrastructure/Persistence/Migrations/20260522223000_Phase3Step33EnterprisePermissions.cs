using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ASMILE.BO.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase3Step33EnterprisePermissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AppDataPermissions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SubjectType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    SubjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ScopeType = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    ScopeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsGranted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppDataPermissions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AppRoleInheritances",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RoleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ParentRoleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppRoleInheritances", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppRoleInheritances_AppRoles_ParentRoleId",
                        column: x => x.ParentRoleId,
                        principalTable: "AppRoles",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_AppRoleInheritances_AppRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AppRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(name: "IX_AppDataPermissions_BranchId", table: "AppDataPermissions", column: "BranchId");
            migrationBuilder.CreateIndex(name: "IX_AppDataPermissions_CompanyId", table: "AppDataPermissions", column: "CompanyId");
            migrationBuilder.CreateIndex(name: "IX_AppDataPermissions_ScopeType_ScopeId", table: "AppDataPermissions", columns: new[] { "ScopeType", "ScopeId" });
            migrationBuilder.CreateIndex(
                name: "IX_AppDataPermissions_SubjectType_SubjectId_ScopeType_ScopeId_CompanyId_BranchId",
                table: "AppDataPermissions",
                columns: new[] { "SubjectType", "SubjectId", "ScopeType", "ScopeId", "CompanyId", "BranchId" },
                unique: true,
                filter: "[CompanyId] IS NOT NULL AND [BranchId] IS NOT NULL");

            migrationBuilder.CreateIndex(name: "IX_AppRoleInheritances_ParentRoleId", table: "AppRoleInheritances", column: "ParentRoleId");
            migrationBuilder.CreateIndex(
                name: "IX_AppRoleInheritances_RoleId_ParentRoleId",
                table: "AppRoleInheritances",
                columns: new[] { "RoleId", "ParentRoleId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "AppDataPermissions");
            migrationBuilder.DropTable(name: "AppRoleInheritances");
        }
    }
}
