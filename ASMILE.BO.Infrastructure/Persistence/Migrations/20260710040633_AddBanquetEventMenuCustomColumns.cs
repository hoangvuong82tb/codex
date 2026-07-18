using System;
using ASMILE.BO.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ASMILE.BO.Infrastructure.Persistence.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260710040633_AddBanquetEventMenuCustomColumns")]
public partial class AddBanquetEventMenuCustomColumns : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(name: "ItemName", table: "BanquetEventMenus", type: "nvarchar(255)", maxLength: 255, nullable: true);
        migrationBuilder.AddColumn<Guid>(name: "MenuItemId", table: "BanquetEventMenus", type: "uniqueidentifier", nullable: true);
        migrationBuilder.AddColumn<string>(name: "Note", table: "BanquetEventMenus", type: "nvarchar(1000)", maxLength: 1000, nullable: true);
        migrationBuilder.AddColumn<decimal>(name: "Qty", table: "BanquetEventMenus", type: "decimal(18,3)", precision: 18, scale: 3, nullable: false, defaultValue: 0m);
        migrationBuilder.CreateIndex(name: "IX_BanquetEventMenus_MenuItemId", table: "BanquetEventMenus", column: "MenuItemId");
        migrationBuilder.AddForeignKey(name: "FK_BanquetEventMenus_MenuItems_MenuItemId", table: "BanquetEventMenus", column: "MenuItemId", principalTable: "MenuItems", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(name: "FK_BanquetEventMenus_MenuItems_MenuItemId", table: "BanquetEventMenus");
        migrationBuilder.DropIndex(name: "IX_BanquetEventMenus_MenuItemId", table: "BanquetEventMenus");
        migrationBuilder.DropColumn(name: "ItemName", table: "BanquetEventMenus");
        migrationBuilder.DropColumn(name: "MenuItemId", table: "BanquetEventMenus");
        migrationBuilder.DropColumn(name: "Note", table: "BanquetEventMenus");
        migrationBuilder.DropColumn(name: "Qty", table: "BanquetEventMenus");
    }
}
