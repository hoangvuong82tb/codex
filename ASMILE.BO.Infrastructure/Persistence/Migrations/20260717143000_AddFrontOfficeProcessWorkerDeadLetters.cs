using System;
using ASMILE.BO.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ASMILE.BO.Infrastructure.Persistence.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260717143000_AddFrontOfficeProcessWorkerDeadLetters")]
    public partial class AddFrontOfficeProcessWorkerDeadLetters : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ThuDaChetQuyTrinh",
                schema: "fo_process",
                columns: table => new
                {
                    DeadLetterId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TimeoutId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProcessId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SignalId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ErrorCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    RetryCount = table.Column<int>(type: "int", nullable: false),
                    OccurredAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    WorkerId = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    CorrelationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BusinessDate = table.Column<DateOnly>(type: "date", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fo_process_ThuDaChetQuyTrinh", x => x.DeadLetterId);
                    table.ForeignKey(name: "FK_fo_process_ThuDaChetQuyTrinh_QuyTrinhVanHanh_ProcessId", column: x => x.ProcessId, principalSchema: "fo_process", principalTable: "QuyTrinhVanHanh", principalColumn: "ProcessId", onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(name: "FK_fo_process_ThuDaChetQuyTrinh_HenGioQuyTrinh_TimeoutId", column: x => x.TimeoutId, principalSchema: "fo_process", principalTable: "HenGioQuyTrinh", principalColumn: "TimeoutId", onDelete: ReferentialAction.Restrict);
                });
            migrationBuilder.CreateIndex(name: "UQ_fo_process_ThuDaChetQuyTrinh_Timeout", schema: "fo_process", table: "ThuDaChetQuyTrinh", column: "TimeoutId", unique: true);
            migrationBuilder.CreateIndex(name: "IX_fo_process_ThuDaChetQuyTrinh_Process_Occurred", schema: "fo_process", table: "ThuDaChetQuyTrinh", columns: new[] { "ProcessId", "OccurredAtUtc" });
            migrationBuilder.CreateIndex(name: "IX_fo_process_ThuDaChetQuyTrinh_BusinessDate_Occurred", schema: "fo_process", table: "ThuDaChetQuyTrinh", columns: new[] { "BusinessDate", "OccurredAtUtc" });
        }

        protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.DropTable(name: "ThuDaChetQuyTrinh", schema: "fo_process");
    }
}
