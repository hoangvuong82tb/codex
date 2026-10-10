using ASMILE.BO.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ASMILE.BO.Infrastructure.Persistence.Migrations;

/// <summary>
/// Repairs a database whose migration history contains AddReservationEnterpriseFields
/// but whose Night Audit objects were omitted from a partial restore.
/// </summary>
[DbContext(typeof(AppDbContext))]
[Migration("20260915110000_RepairNightAuditTables")]
public partial class RepairNightAuditTables : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM sys.sequences WHERE name = N'TrinhTuSoKiemDem' AND schema_id = SCHEMA_ID(N'fo'))
    EXEC(N'CREATE SEQUENCE [fo].[TrinhTuSoKiemDem] AS bigint START WITH 1 INCREMENT BY 1');

IF OBJECT_ID(N'[fo].[KiemDem]', N'U') IS NULL
BEGIN
    CREATE TABLE [fo].[KiemDem]
    (
        [NightAuditId] uniqueidentifier NOT NULL,
        [PropertyId] uniqueidentifier NOT NULL,
        [AuditNumber] nvarchar(40) NOT NULL,
        [BusinessDate] date NOT NULL,
        [Status] nvarchar(20) NOT NULL,
        [CurrentStep] int NOT NULL,
        [ErrorMessage] nvarchar(1000) NULL,
        [RoomsCharged] int NOT NULL CONSTRAINT [DF_fo_KiemDem_RoomsCharged] DEFAULT (0),
        [RoomRevenue] decimal(18,2) NOT NULL CONSTRAINT [DF_fo_KiemDem_RoomRevenue] DEFAULT (0),
        [TaxPosted] decimal(18,2) NOT NULL CONSTRAINT [DF_fo_KiemDem_TaxPosted] DEFAULT (0),
        [ServiceChargePosted] decimal(18,2) NOT NULL CONSTRAINT [DF_fo_KiemDem_ServiceChargePosted] DEFAULT (0),
        [FoliosClosed] int NOT NULL CONSTRAINT [DF_fo_KiemDem_FoliosClosed] DEFAULT (0),
        [StaysUpdated] int NOT NULL CONSTRAINT [DF_fo_KiemDem_StaysUpdated] DEFAULT (0),
        [ValidationFailures] int NOT NULL CONSTRAINT [DF_fo_KiemDem_ValidationFailures] DEFAULT (0),
        [StartedAtUtc] datetime2 NOT NULL,
        [CompletedAtUtc] datetime2 NULL,
        [StartedBy] uniqueidentifier NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_fo_KiemDem] PRIMARY KEY ([NightAuditId])
    );
    CREATE UNIQUE INDEX [UQ_fo_KiemDem_SoKiemDem] ON [fo].[KiemDem] ([AuditNumber]);
    CREATE INDEX [IX_fo_KiemDem_CoSo_Ngay] ON [fo].[KiemDem] ([PropertyId], [BusinessDate]);
END;

IF OBJECT_ID(N'[fo].[BuocKiemDem]', N'U') IS NULL
BEGIN
    CREATE TABLE [fo].[BuocKiemDem]
    (
        [NightAuditStepExecutionId] uniqueidentifier NOT NULL,
        [NightAuditId] uniqueidentifier NOT NULL,
        [PropertyId] uniqueidentifier NOT NULL,
        [Step] int NOT NULL,
        [Status] nvarchar(20) NOT NULL,
        [Detail] nvarchar(1000) NULL,
        [CompletedAtUtc] datetime2 NULL,
        CONSTRAINT [PK_fo_BuocKiemDem] PRIMARY KEY ([NightAuditStepExecutionId]),
        CONSTRAINT [FK_BuocKiemDem_KiemDem_NightAuditId]
            FOREIGN KEY ([NightAuditId]) REFERENCES [fo].[KiemDem] ([NightAuditId]) ON DELETE NO ACTION
    );
    CREATE UNIQUE INDEX [UQ_fo_BuocKiemDem_Audit_Step] ON [fo].[BuocKiemDem] ([NightAuditId], [Step]);
END;

IF OBJECT_ID(N'[fo].[DoanhThuNgay]', N'U') IS NULL
BEGIN
    CREATE TABLE [fo].[DoanhThuNgay]
    (
        [RevenueSnapshotId] uniqueidentifier NOT NULL,
        [PropertyId] uniqueidentifier NOT NULL,
        [NightAuditId] uniqueidentifier NOT NULL,
        [BusinessDate] date NOT NULL,
        [RoomRevenue] decimal(18,2) NOT NULL,
        [OtherRevenue] decimal(18,2) NOT NULL,
        [TotalRevenue] decimal(18,2) NOT NULL,
        [TotalTax] decimal(18,2) NOT NULL,
        [TotalPayments] decimal(18,2) NOT NULL,
        [TotalRefunds] decimal(18,2) NOT NULL,
        [OutstandingBalance] decimal(18,2) NOT NULL,
        [RoomsSold] int NOT NULL,
        [TotalRooms] int NOT NULL,
        [OccupancyRate] decimal(9,4) NOT NULL,
        [ADR] decimal(18,2) NOT NULL,
        [RevPAR] decimal(18,2) NOT NULL,
        [Arrivals] int NOT NULL,
        [Departures] int NOT NULL,
        [NoShows] int NOT NULL,
        [Cancellations] int NOT NULL,
        [SnapshotJson] nvarchar(max) NOT NULL,
        [CreatedAtUtc] datetime2 NOT NULL,
        CONSTRAINT [PK_fo_DoanhThuNgay] PRIMARY KEY ([RevenueSnapshotId]),
        CONSTRAINT [FK_DoanhThuNgay_KiemDem_NightAuditId]
            FOREIGN KEY ([NightAuditId]) REFERENCES [fo].[KiemDem] ([NightAuditId]) ON DELETE NO ACTION
    );
    CREATE INDEX [IX_fo_DoanhThuNgay_KiemDem] ON [fo].[DoanhThuNgay] ([NightAuditId]);
    CREATE UNIQUE INDEX [UQ_fo_DoanhThuNgay_CoSo_Ngay] ON [fo].[DoanhThuNgay] ([PropertyId], [BusinessDate]);
END;");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(@"
IF OBJECT_ID(N'[fo].[DoanhThuNgay]', N'U') IS NOT NULL DROP TABLE [fo].[DoanhThuNgay];
IF OBJECT_ID(N'[fo].[BuocKiemDem]', N'U') IS NOT NULL DROP TABLE [fo].[BuocKiemDem];
IF OBJECT_ID(N'[fo].[KiemDem]', N'U') IS NOT NULL DROP TABLE [fo].[KiemDem];
IF EXISTS (SELECT 1 FROM sys.sequences WHERE name = N'TrinhTuSoKiemDem' AND schema_id = SCHEMA_ID(N'fo'))
    DROP SEQUENCE [fo].[TrinhTuSoKiemDem];");
    }
}
