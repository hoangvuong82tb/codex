using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ASMILE.BO.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CreateFrontOfficeProcessManagerFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "fo_process");

            migrationBuilder.CreateTable(
                name: "QuyTrinhVanHanh",
                schema: "fo_process",
                columns: table => new
                {
                    ProcessId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProcessKey = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ProcessType = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    ProcessVersion = table.Column<int>(type: "int", nullable: false),
                    PropertyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BusinessDate = table.Column<DateOnly>(type: "date", nullable: false),
                    RootSubjectType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    RootSubjectId = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    CurrentState = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    CurrentStep = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    StartedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ExpiresAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CorrelationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CausationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    InitiatingCommandId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    InitiatedByActorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RetryCount = table.Column<int>(type: "int", nullable: false),
                    LastErrorCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    LastErrorAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RequiresManualAction = table.Column<bool>(type: "bit", nullable: false),
                    CompensationStatus = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    ResumeToken = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fo_process_QuyTrinhVanHanh", x => x.ProcessId);
                });

            migrationBuilder.CreateTable(
                name: "BuocQuyTrinh",
                schema: "fo_process",
                columns: table => new
                {
                    StepId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProcessId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StepName = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    StepVersion = table.Column<int>(type: "int", nullable: false),
                    Sequence = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    StartedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TimeoutAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RetryCount = table.Column<int>(type: "int", nullable: false),
                    LastErrorCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ExpectedSignalType = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    CommandReference = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ResultReference = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CompensationRequired = table.Column<bool>(type: "bit", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fo_process_BuocQuyTrinh", x => x.StepId);
                    table.ForeignKey(
                        name: "FK_BuocQuyTrinh_QuyTrinhVanHanh_ProcessId",
                        column: x => x.ProcessId,
                        principalSchema: "fo_process",
                        principalTable: "QuyTrinhVanHanh",
                        principalColumn: "ProcessId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "HanhDongThuCongQuyTrinh",
                schema: "fo_process",
                columns: table => new
                {
                    ManualActionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProcessId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ActionType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    RequiredPermission = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    AssignedRole = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    AssignedActorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ReasonRequired = table.Column<bool>(type: "bit", nullable: false),
                    ApprovalRequired = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DueAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedByActorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ResultCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    AuditReference = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fo_process_HanhDongThuCongQuyTrinh", x => x.ManualActionId);
                    table.ForeignKey(
                        name: "FK_HanhDongThuCongQuyTrinh_QuyTrinhVanHanh_ProcessId",
                        column: x => x.ProcessId,
                        principalSchema: "fo_process",
                        principalTable: "QuyTrinhVanHanh",
                        principalColumn: "ProcessId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LichSuQuyTrinh",
                schema: "fo_process",
                columns: table => new
                {
                    HistoryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProcessId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PreviousState = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    NewState = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    PreviousStep = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    NewStep = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    TransitionType = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    SignalId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CommandId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    EventId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ActorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    OccurredAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ErrorCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ReasonCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CorrelationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fo_process_LichSuQuyTrinh", x => x.HistoryId);
                    table.ForeignKey(
                        name: "FK_LichSuQuyTrinh_QuyTrinhVanHanh_ProcessId",
                        column: x => x.ProcessId,
                        principalSchema: "fo_process",
                        principalTable: "QuyTrinhVanHanh",
                        principalColumn: "ProcessId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TinHieuQuyTrinh",
                schema: "fo_process",
                columns: table => new
                {
                    SignalId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProcessId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SignalType = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    MessageId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PayloadHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ReceivedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ProcessedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    IsOutOfOrder = table.Column<bool>(type: "bit", nullable: false),
                    CorrelationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CausationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SchemaVersion = table.Column<int>(type: "int", nullable: false),
                    ErrorCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fo_process_TinHieuQuyTrinh", x => new { x.ProcessId, x.SignalId });
                    table.ForeignKey(
                        name: "FK_TinHieuQuyTrinh_QuyTrinhVanHanh_ProcessId",
                        column: x => x.ProcessId,
                        principalSchema: "fo_process",
                        principalTable: "QuyTrinhVanHanh",
                        principalColumn: "ProcessId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BuTruQuyTrinh",
                schema: "fo_process",
                columns: table => new
                {
                    CompensationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProcessId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StepId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CompensationType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    RequestedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    StartedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CommandReference = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ErrorCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RetryCount = table.Column<int>(type: "int", nullable: false),
                    ApprovalReference = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    AuditReference = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fo_process_BuTruQuyTrinh", x => x.CompensationId);
                    table.ForeignKey(
                        name: "FK_BuTruQuyTrinh_BuocQuyTrinh_StepId",
                        column: x => x.StepId,
                        principalSchema: "fo_process",
                        principalTable: "BuocQuyTrinh",
                        principalColumn: "StepId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BuTruQuyTrinh_QuyTrinhVanHanh_ProcessId",
                        column: x => x.ProcessId,
                        principalSchema: "fo_process",
                        principalTable: "QuyTrinhVanHanh",
                        principalColumn: "ProcessId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "HenGioQuyTrinh",
                schema: "fo_process",
                columns: table => new
                {
                    TimeoutId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProcessId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StepId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TimeoutType = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    DueAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    ClaimedBy = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    LeaseExpiresAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AttemptCount = table.Column<int>(type: "int", nullable: false),
                    FiredAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CancelledAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CorrelationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fo_process_HenGioQuyTrinh", x => x.TimeoutId);
                    table.ForeignKey(
                        name: "FK_HenGioQuyTrinh_BuocQuyTrinh_StepId",
                        column: x => x.StepId,
                        principalSchema: "fo_process",
                        principalTable: "BuocQuyTrinh",
                        principalColumn: "StepId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_HenGioQuyTrinh_QuyTrinhVanHanh_ProcessId",
                        column: x => x.ProcessId,
                        principalSchema: "fo_process",
                        principalTable: "QuyTrinhVanHanh",
                        principalColumn: "ProcessId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LanThucHienBuocQuyTrinh",
                schema: "fo_process",
                columns: table => new
                {
                    AttemptId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProcessId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StepId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AttemptNumber = table.Column<int>(type: "int", nullable: false),
                    StartedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Outcome = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    ErrorCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RetryClassification = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: true),
                    CorrelationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CommandId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    MessageId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    WorkerId = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    DurationMilliseconds = table.Column<long>(type: "bigint", nullable: true),
                    PayloadHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    SanitizedMetadata = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fo_process_LanThucHienBuocQuyTrinh", x => x.AttemptId);
                    table.ForeignKey(
                        name: "FK_LanThucHienBuocQuyTrinh_BuocQuyTrinh_StepId",
                        column: x => x.StepId,
                        principalSchema: "fo_process",
                        principalTable: "BuocQuyTrinh",
                        principalColumn: "StepId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LanThucHienBuocQuyTrinh_QuyTrinhVanHanh_ProcessId",
                        column: x => x.ProcessId,
                        principalSchema: "fo_process",
                        principalTable: "QuyTrinhVanHanh",
                        principalColumn: "ProcessId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "UQ_fo_process_BuocQuyTrinh_Process_Sequence",
                schema: "fo_process",
                table: "BuocQuyTrinh",
                columns: new[] { "ProcessId", "Sequence" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BuTruQuyTrinh_StepId",
                schema: "fo_process",
                table: "BuTruQuyTrinh",
                column: "StepId");

            migrationBuilder.CreateIndex(
                name: "IX_fo_process_BuTruQuyTrinh_Process_Status",
                schema: "fo_process",
                table: "BuTruQuyTrinh",
                columns: new[] { "ProcessId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_fo_process_HanhDongThuCongQuyTrinh_Status_Due",
                schema: "fo_process",
                table: "HanhDongThuCongQuyTrinh",
                columns: new[] { "Status", "DueAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_HanhDongThuCongQuyTrinh_ProcessId",
                schema: "fo_process",
                table: "HanhDongThuCongQuyTrinh",
                column: "ProcessId");

            migrationBuilder.CreateIndex(
                name: "IX_fo_process_HenGioQuyTrinh_Claim",
                schema: "fo_process",
                table: "HenGioQuyTrinh",
                columns: new[] { "Status", "DueAtUtc", "LeaseExpiresAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_HenGioQuyTrinh_ProcessId",
                schema: "fo_process",
                table: "HenGioQuyTrinh",
                column: "ProcessId");

            migrationBuilder.CreateIndex(
                name: "IX_HenGioQuyTrinh_StepId",
                schema: "fo_process",
                table: "HenGioQuyTrinh",
                column: "StepId");

            migrationBuilder.CreateIndex(
                name: "IX_LanThucHienBuocQuyTrinh_ProcessId",
                schema: "fo_process",
                table: "LanThucHienBuocQuyTrinh",
                column: "ProcessId");

            migrationBuilder.CreateIndex(
                name: "UQ_fo_process_LanThucHienBuocQuyTrinh_Step_Attempt",
                schema: "fo_process",
                table: "LanThucHienBuocQuyTrinh",
                columns: new[] { "StepId", "AttemptNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_fo_process_LichSuQuyTrinh_Process_Occurred",
                schema: "fo_process",
                table: "LichSuQuyTrinh",
                columns: new[] { "ProcessId", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_fo_process_QuyTrinhVanHanh_Correlation",
                schema: "fo_process",
                table: "QuyTrinhVanHanh",
                column: "CorrelationId");

            migrationBuilder.CreateIndex(
                name: "IX_fo_process_QuyTrinhVanHanh_Status_Expiry",
                schema: "fo_process",
                table: "QuyTrinhVanHanh",
                columns: new[] { "Status", "ExpiresAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_fo_process_QuyTrinhVanHanh_Subject",
                schema: "fo_process",
                table: "QuyTrinhVanHanh",
                columns: new[] { "PropertyId", "RootSubjectType", "RootSubjectId" });

            migrationBuilder.CreateIndex(
                name: "UQ_fo_process_QuyTrinhVanHanh_ProcessKey",
                schema: "fo_process",
                table: "QuyTrinhVanHanh",
                column: "ProcessKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_fo_process_TinHieuQuyTrinh_Correlation",
                schema: "fo_process",
                table: "TinHieuQuyTrinh",
                column: "CorrelationId");

            migrationBuilder.CreateIndex(
                name: "UQ_fo_process_TinHieuQuyTrinh_Process_Signal",
                schema: "fo_process",
                table: "TinHieuQuyTrinh",
                columns: new[] { "ProcessId", "SignalId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BuTruQuyTrinh",
                schema: "fo_process");

            migrationBuilder.DropTable(
                name: "HanhDongThuCongQuyTrinh",
                schema: "fo_process");

            migrationBuilder.DropTable(
                name: "HenGioQuyTrinh",
                schema: "fo_process");

            migrationBuilder.DropTable(
                name: "LanThucHienBuocQuyTrinh",
                schema: "fo_process");

            migrationBuilder.DropTable(
                name: "LichSuQuyTrinh",
                schema: "fo_process");

            migrationBuilder.DropTable(
                name: "TinHieuQuyTrinh",
                schema: "fo_process");

            migrationBuilder.DropTable(
                name: "BuocQuyTrinh",
                schema: "fo_process");

            migrationBuilder.DropTable(
                name: "QuyTrinhVanHanh",
                schema: "fo_process");
        }
    }
}
