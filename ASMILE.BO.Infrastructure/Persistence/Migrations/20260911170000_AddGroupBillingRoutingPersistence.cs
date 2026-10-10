using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using ASMILE.BO.Infrastructure.Persistence;

#nullable disable

namespace ASMILE.BO.Infrastructure.Persistence.Migrations;

[Migration("20260911170000_AddGroupBillingRoutingPersistence")]
[DbContext(typeof(AppDbContext))]
public partial class AddGroupBillingRoutingPersistence : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<bool>(name: "UseMasterFolio", schema: "fo", table: "DatPhongDoan", nullable: false, defaultValue: false);
        migrationBuilder.AddColumn<bool>(name: "AllowIndividualFolios", schema: "fo", table: "DatPhongDoan", nullable: false, defaultValue: false);
        migrationBuilder.AddColumn<decimal>(name: "SplitPercentage", schema: "fo", table: "DatPhongDoan", type: "decimal(5,2)", nullable: true);
        migrationBuilder.AddColumn<Guid>(name: "MasterFolioId", schema: "fo", table: "DatPhongDoan", type: "uniqueidentifier", nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "UseMasterFolio", schema: "fo", table: "DatPhongDoan");
        migrationBuilder.DropColumn(name: "AllowIndividualFolios", schema: "fo", table: "DatPhongDoan");
        migrationBuilder.DropColumn(name: "SplitPercentage", schema: "fo", table: "DatPhongDoan");
        migrationBuilder.DropColumn(name: "MasterFolioId", schema: "fo", table: "DatPhongDoan");
    }
}
