using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ASMILE.BO.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMaintenanceWorkOrders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CongViecBaoTri",
                schema: "fo",
                columns: table => new
                {
                    WorkOrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrderNumber = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    PropertyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RoomId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RoomNumber = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    Type = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Priority = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    AssignedEngineerId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AssignedEngineerName = table.Column<string>(type: "nvarchar(180)", maxLength: 180, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    StartedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    VerifiedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    VerifiedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    EstimatedMinutes = table.Column<int>(type: "int", nullable: false),
                    ActualMinutes = table.Column<int>(type: "int", nullable: true),
                    Resolution = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fo_CongViecBaoTri", x => x.WorkOrderId);
                });

            migrationBuilder.CreateTable(
                name: "NhatKyCongViecBaoTri",
                schema: "fo",
                columns: table => new
                {
                    NoteId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WorkOrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Action = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Detail = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ActorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fo_NhatKyCongViecBaoTri", x => x.NoteId);
                    table.ForeignKey(
                        name: "FK_NhatKyCongViecBaoTri_CongViecBaoTri_WorkOrderId",
                        column: x => x.WorkOrderId,
                        principalSchema: "fo",
                        principalTable: "CongViecBaoTri",
                        principalColumn: "WorkOrderId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_fo_CongViecBaoTri_CoSo_Phong_TrangThai",
                schema: "fo",
                table: "CongViecBaoTri",
                columns: new[] { "PropertyId", "RoomId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_fo_CongViecBaoTri_CoSo_TrangThai_UuTien",
                schema: "fo",
                table: "CongViecBaoTri",
                columns: new[] { "PropertyId", "Status", "Priority" });

            migrationBuilder.CreateIndex(
                name: "UQ_fo_CongViecBaoTri_CoSo_SoLenh",
                schema: "fo",
                table: "CongViecBaoTri",
                columns: new[] { "PropertyId", "OrderNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_fo_NhatKyCongViecBaoTri_Lenh_ThoiGian",
                schema: "fo",
                table: "NhatKyCongViecBaoTri",
                columns: new[] { "WorkOrderId", "AtUtc" });

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NhatKyCongViecBaoTri",
                schema: "fo");

            migrationBuilder.DropTable(
                name: "CongViecBaoTri",
                schema: "fo");
        }
    }
}
