using ASMILE.BO.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ASMILE.BO.Infrastructure.Persistence.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260913130000_AddFeedbackAndVipLoyaltyPersistence")]
public partial class AddFeedbackAndVipLoyaltyPersistence : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            IF SCHEMA_ID(N'fo') IS NULL
                EXEC(N'CREATE SCHEMA [fo]');

            IF OBJECT_ID(N'[fo].[PhanHoiKhach]', N'U') IS NULL
            BEGIN
                CREATE TABLE [fo].[PhanHoiKhach]
                (
                    [GuestFeedbackId] uniqueidentifier NOT NULL,
                    [PropertyId] uniqueidentifier NOT NULL,
                    [FeedbackNumber] nvarchar(60) NOT NULL,
                    [GuestId] uniqueidentifier NULL,
                    [ReservationId] uniqueidentifier NULL,
                    [HotelStayId] uniqueidentifier NULL,
                    [HotelRoomId] uniqueidentifier NULL,
                    [GuestName] nvarchar(200) NULL,
                    [RoomNumber] nvarchar(40) NULL,
                    [FeedbackType] nvarchar(80) NOT NULL,
                    [Category] nvarchar(80) NOT NULL,
                    [Priority] nvarchar(20) NOT NULL,
                    [Status] nvarchar(30) NOT NULL,
                    [Source] nvarchar(80) NULL,
                    [Rating] int NOT NULL,
                    [Subject] nvarchar(255) NULL,
                    [Description] nvarchar(4000) NOT NULL,
                    [AssignedDepartment] nvarchar(120) NULL,
                    [AssignedStaffId] uniqueidentifier NULL,
                    [CreatedAtUtc] datetime2 NOT NULL,
                    [AcknowledgedAtUtc] datetime2 NULL,
                    [ResolvedAtUtc] datetime2 NULL,
                    [ClosedAtUtc] datetime2 NULL,
                    [Resolution] nvarchar(4000) NULL,
                    [FollowUpRequired] bit NOT NULL,
                    [FollowUpDate] datetime2 NULL,
                    [CreatedBy] uniqueidentifier NULL,
                    [RowVersion] rowversion NULL,
                    CONSTRAINT [PK_fo_PhanHoiKhach] PRIMARY KEY ([GuestFeedbackId])
                );
            END;

            IF OBJECT_ID(N'[fo].[LichSuPhanHoiKhach]', N'U') IS NULL
            BEGIN
                CREATE TABLE [fo].[LichSuPhanHoiKhach]
                (
                    [GuestFeedbackHistoryId] uniqueidentifier NOT NULL,
                    [GuestFeedbackId] uniqueidentifier NOT NULL,
                    [PropertyId] uniqueidentifier NOT NULL,
                    [Action] nvarchar(500) NOT NULL,
                    [OccurredAtUtc] datetime2 NOT NULL,
                    CONSTRAINT [PK_fo_LichSuPhanHoiKhach] PRIMARY KEY ([GuestFeedbackHistoryId]),
                    CONSTRAINT [FK_fo_LichSuPhanHoiKhach_PhanHoiKhach] FOREIGN KEY ([GuestFeedbackId]) REFERENCES [fo].[PhanHoiKhach] ([GuestFeedbackId]) ON DELETE CASCADE
                );
            END;

            IF OBJECT_ID(N'[fo].[HoSoVIP]', N'U') IS NULL
            BEGIN
                CREATE TABLE [fo].[HoSoVIP]
                (
                    [VipProfileId] uniqueidentifier NOT NULL,
                    [PropertyId] uniqueidentifier NOT NULL,
                    [VipNumber] nvarchar(60) NOT NULL,
                    [GuestId] uniqueidentifier NULL,
                    [GuestName] nvarchar(200) NULL,
                    [Level] nvarchar(20) NOT NULL,
                    [Status] nvarchar(20) NOT NULL,
                    [PointsBalance] int NOT NULL,
                    [LifetimePoints] int NOT NULL,
                    [EnrollmentDate] datetime2 NOT NULL,
                    [ExpirationDate] datetime2 NULL,
                    [Preferences] nvarchar(2000) NULL,
                    [SpecialRequests] nvarchar(2000) NULL,
                    [Birthday] date NULL,
                    [Anniversary] date NULL,
                    [AssignedAccountManager] nvarchar(200) NULL,
                    [LastStayDate] date NULL,
                    [TotalStayCount] int NOT NULL,
                    [TotalRevenue] decimal(18,2) NOT NULL,
                    [Notes] nvarchar(2000) NULL,
                    [CreatedAtUtc] datetime2 NOT NULL,
                    [RowVersion] rowversion NULL,
                    CONSTRAINT [PK_fo_HoSoVIP] PRIMARY KEY ([VipProfileId])
                );
            END;

            IF OBJECT_ID(N'[fo].[GiaoDichLoyalty]', N'U') IS NULL
            BEGIN
                CREATE TABLE [fo].[GiaoDichLoyalty]
                (
                    [LoyaltyTransactionId] uniqueidentifier NOT NULL,
                    [VipProfileId] uniqueidentifier NOT NULL,
                    [PropertyId] uniqueidentifier NOT NULL,
                    [Type] nvarchar(20) NOT NULL,
                    [Points] int NOT NULL,
                    [Description] nvarchar(500) NOT NULL,
                    [OccurredAtUtc] datetime2 NOT NULL,
                    CONSTRAINT [PK_fo_GiaoDichLoyalty] PRIMARY KEY ([LoyaltyTransactionId]),
                    CONSTRAINT [FK_fo_GiaoDichLoyalty_HoSoVIP] FOREIGN KEY ([VipProfileId]) REFERENCES [fo].[HoSoVIP] ([VipProfileId]) ON DELETE CASCADE
                );
            END;

            IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UQ_fo_PhanHoiKhach_CoSo_SoPhanHoi' AND object_id = OBJECT_ID(N'[fo].[PhanHoiKhach]'))
                CREATE UNIQUE INDEX [UQ_fo_PhanHoiKhach_CoSo_SoPhanHoi] ON [fo].[PhanHoiKhach] ([PropertyId], [FeedbackNumber]);
            IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_fo_PhanHoiKhach_CoSo_TrangThai_ThoiGian' AND object_id = OBJECT_ID(N'[fo].[PhanHoiKhach]'))
                CREATE INDEX [IX_fo_PhanHoiKhach_CoSo_TrangThai_ThoiGian] ON [fo].[PhanHoiKhach] ([PropertyId], [Status], [CreatedAtUtc]);
            IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_fo_PhanHoiKhach_CoSo_Phong_ThoiGian' AND object_id = OBJECT_ID(N'[fo].[PhanHoiKhach]'))
                CREATE INDEX [IX_fo_PhanHoiKhach_CoSo_Phong_ThoiGian] ON [fo].[PhanHoiKhach] ([PropertyId], [RoomNumber], [CreatedAtUtc]);
            IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_fo_LichSuPhanHoiKhach_CoSo_PhanHoi_ThoiGian' AND object_id = OBJECT_ID(N'[fo].[LichSuPhanHoiKhach]'))
                CREATE INDEX [IX_fo_LichSuPhanHoiKhach_CoSo_PhanHoi_ThoiGian] ON [fo].[LichSuPhanHoiKhach] ([PropertyId], [GuestFeedbackId], [OccurredAtUtc]);
            IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UQ_fo_HoSoVIP_CoSo_MaVIP' AND object_id = OBJECT_ID(N'[fo].[HoSoVIP]'))
                CREATE UNIQUE INDEX [UQ_fo_HoSoVIP_CoSo_MaVIP] ON [fo].[HoSoVIP] ([PropertyId], [VipNumber]);
            IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_fo_HoSoVIP_CoSo_Hang_TrangThai' AND object_id = OBJECT_ID(N'[fo].[HoSoVIP]'))
                CREATE INDEX [IX_fo_HoSoVIP_CoSo_Hang_TrangThai] ON [fo].[HoSoVIP] ([PropertyId], [Level], [Status]);
            IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_fo_GiaoDichLoyalty_CoSo_VIP_ThoiGian' AND object_id = OBJECT_ID(N'[fo].[GiaoDichLoyalty]'))
                CREATE INDEX [IX_fo_GiaoDichLoyalty_CoSo_VIP_ThoiGian] ON [fo].[GiaoDichLoyalty] ([PropertyId], [VipProfileId], [OccurredAtUtc]);
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // This migration is intentionally non-destructive for production rollback.
        // Restore from the deployment backup if these tables must be removed.
    }
}
