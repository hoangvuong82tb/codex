using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using ASMILE.BO.Infrastructure.Persistence;

#nullable disable

namespace ASMILE.BO.Infrastructure.Persistence.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260913093000_AddBusinessDocumentPaymentMethod")]
public partial class AddBusinessDocumentPaymentMethod : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "PaymentMethod",
            schema: "dbo",
            table: "BusinessDocuments",
            type: "nvarchar(32)",
            maxLength: 32,
            nullable: false,
            defaultValue: "Chưa xác định");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "PaymentMethod",
            schema: "dbo",
            table: "BusinessDocuments");
    }
}
