using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ASMILE.BO.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddBanquetMenuPackageLinePricingRule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PricingRule",
                table: "BanquetMenuPackageLines",
                type: "nvarchar(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE line
                SET line.PricingRule = pkg.PricingMode
                FROM BanquetMenuPackageLines AS line
                INNER JOIN BanquetMenuPackages AS pkg
                    ON pkg.Id = line.BanquetMenuPackageId
                WHERE line.PricingRule IS NULL OR LTRIM(RTRIM(line.PricingRule)) = ''
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PricingRule",
                table: "BanquetMenuPackageLines");
        }
    }
}
