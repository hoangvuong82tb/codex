using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ASMILE.BO.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddQuoteChangeRequestRowVersion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_QuoteAuditLogs_RestaurantQuotes_QuoteId",
                table: "QuoteAuditLogs");

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "QuoteChangeRequests",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddForeignKey(
                name: "FK_QuoteAuditLogs_RestaurantQuotes_QuoteId",
                table: "QuoteAuditLogs",
                column: "QuoteId",
                principalTable: "RestaurantQuotes",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_QuoteAuditLogs_RestaurantQuotes_QuoteId",
                table: "QuoteAuditLogs");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "QuoteChangeRequests");

            migrationBuilder.AddForeignKey(
                name: "FK_QuoteAuditLogs_RestaurantQuotes_QuoteId",
                table: "QuoteAuditLogs",
                column: "QuoteId",
                principalTable: "RestaurantQuotes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
