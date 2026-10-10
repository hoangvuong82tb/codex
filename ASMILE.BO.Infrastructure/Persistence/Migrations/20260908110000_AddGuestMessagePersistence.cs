using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ASMILE.BO.Infrastructure.Persistence.Migrations;

/// <summary>
/// Adds the persistence store for Message Center without recreating the older
/// service-request tables already introduced by AddReservationEnterpriseFields.
/// </summary>
public partial class AddGuestMessagePersistence : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            IF SCHEMA_ID(N'fo') IS NULL
                EXEC(N'CREATE SCHEMA [fo]');

            IF OBJECT_ID(N'[fo].[TinNhanKhach]', N'U') IS NULL
            BEGIN
                CREATE TABLE [fo].[TinNhanKhach]
                (
                    [GuestMessageId] uniqueidentifier NOT NULL,
                    [PropertyId] uniqueidentifier NOT NULL,
                    [MessageNumber] nvarchar(40) NOT NULL,
                    [GuestId] uniqueidentifier NULL,
                    [ReservationId] uniqueidentifier NULL,
                    [HotelStayId] uniqueidentifier NULL,
                    [HotelRoomId] uniqueidentifier NULL,
                    [GuestName] nvarchar(200) NULL,
                    [RoomNumber] nvarchar(40) NULL,
                    [MessageType] nvarchar(120) NOT NULL,
                    [Channel] nvarchar(40) NOT NULL,
                    [Priority] nvarchar(20) NOT NULL,
                    [Status] nvarchar(20) NOT NULL,
                    [Subject] nvarchar(300) NULL,
                    [Content] nvarchar(4000) NOT NULL,
                    [Sender] nvarchar(200) NULL,
                    [CreatedAtUtc] datetime2 NOT NULL,
                    [DeliveredAtUtc] datetime2 NULL,
                    [ReadAtUtc] datetime2 NULL,
                    [CompletedAtUtc] datetime2 NULL,
                    [RequiresResponse] bit NOT NULL,
                    [ResponseContent] nvarchar(4000) NULL,
                    [AssignedStaff] nvarchar(200) NULL,
                    [Department] nvarchar(120) NULL,
                    [CreatedBy] uniqueidentifier NULL,
                    [RowVersion] rowversion NULL,
                    CONSTRAINT [PK_fo_TinNhanKhach] PRIMARY KEY ([GuestMessageId])
                );
            END;

            IF OBJECT_ID(N'[fo].[LichSuTinNhanKhach]', N'U') IS NULL
            BEGIN
                CREATE TABLE [fo].[LichSuTinNhanKhach]
                (
                    [MessageHistoryId] uniqueidentifier NOT NULL,
                    [GuestMessageId] uniqueidentifier NOT NULL,
                    [PropertyId] uniqueidentifier NOT NULL,
                    [Action] nvarchar(40) NOT NULL,
                    [OccurredAtUtc] datetime2 NOT NULL,
                    CONSTRAINT [PK_fo_LichSuTinNhanKhach] PRIMARY KEY ([MessageHistoryId]),
                    CONSTRAINT [FK_LichSuTinNhanKhach_TinNhanKhach_GuestMessageId]
                        FOREIGN KEY ([GuestMessageId]) REFERENCES [fo].[TinNhanKhach] ([GuestMessageId]) ON DELETE CASCADE
                );
            END;

            IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_fo_TinNhanKhach_CoSo_TrangThai_ThoiGian' AND object_id = OBJECT_ID(N'[fo].[TinNhanKhach]'))
                CREATE INDEX [IX_fo_TinNhanKhach_CoSo_TrangThai_ThoiGian] ON [fo].[TinNhanKhach] ([PropertyId], [Status], [CreatedAtUtc]);

            IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UQ_fo_TinNhanKhach_CoSo_SoTin' AND object_id = OBJECT_ID(N'[fo].[TinNhanKhach]'))
                CREATE UNIQUE INDEX [UQ_fo_TinNhanKhach_CoSo_SoTin] ON [fo].[TinNhanKhach] ([PropertyId], [MessageNumber]);

            IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_fo_LichSuTinNhanKhach_TinNhan_ThoiGian' AND object_id = OBJECT_ID(N'[fo].[LichSuTinNhanKhach]'))
                CREATE INDEX [IX_fo_LichSuTinNhanKhach_TinNhan_ThoiGian] ON [fo].[LichSuTinNhanKhach] ([GuestMessageId], [OccurredAtUtc]);
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // This is an additive persistence migration. Do not delete messages on rollback.
    }
}
