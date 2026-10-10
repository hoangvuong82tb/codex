using ASMILE.BO.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ASMILE.BO.Infrastructure.Persistence.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260920190000_AddKeyCardCredentialPersistence")]
public partial class AddKeyCardCredentialPersistence : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // The earlier door-lock migration creates the legacy device/event tables,
        // while AppDbContext also persists stay key-card credentials and events.
        // Repair both clean installs and databases that already contain a partial
        // credential table without replacing any issued-card data.
        migrationBuilder.Sql("""
            IF SCHEMA_ID(N'fo') IS NULL
                EXEC(N'CREATE SCHEMA [fo]');

            IF OBJECT_ID(N'[fo].[KeyCardCredential]', N'U') IS NULL
            BEGIN
                CREATE TABLE [fo].[KeyCardCredential]
                (
                    [CredentialId] uniqueidentifier NOT NULL,
                    [AccessLevel] nvarchar(30) NOT NULL,
                    [CardSerial] nvarchar(80) NOT NULL,
                    [DisableReason] nvarchar(500) NULL,
                    [DisabledAtUtc] datetime2 NULL,
                    [DisabledBy] uniqueidentifier NULL,
                    [EncoderId] int NOT NULL,
                    [ExpiresAtUtc] datetime2 NULL,
                    [GuestId] uniqueidentifier NULL,
                    [HotelRoomId] uniqueidentifier NULL,
                    [IsMaster] bit NOT NULL,
                    [IssuedAtUtc] datetime2 NOT NULL,
                    [IssuedBy] uniqueidentifier NULL,
                    [PropertyId] uniqueidentifier NOT NULL,
                    [ReturnedAtUtc] datetime2 NULL,
                    [RowVersion] rowversion NULL,
                    [Status] nvarchar(30) NOT NULL,
                    [StayId] uniqueidentifier NOT NULL,
                    CONSTRAINT [PK_fo_KeyCardCredential] PRIMARY KEY ([CredentialId]),
                    CONSTRAINT [FK_KeyCardCredential_HoSoKhach_GuestId]
                        FOREIGN KEY ([GuestId]) REFERENCES [fo].[HoSoKhach] ([GuestProfileId]) ON DELETE NO ACTION,
                    CONSTRAINT [FK_KeyCardCredential_PhongKhachSan_HotelRoomId]
                        FOREIGN KEY ([HotelRoomId]) REFERENCES [fo].[PhongKhachSan] ([HotelRoomId]) ON DELETE NO ACTION,
                    CONSTRAINT [FK_KeyCardCredential_LuuTruKhachSan_StayId]
                        FOREIGN KEY ([StayId]) REFERENCES [fo].[LuuTruKhachSan] ([HotelStayId]) ON DELETE NO ACTION
                );
            END;

            IF COL_LENGTH(N'fo.KeyCardCredential', N'AccessLevel') IS NULL
                ALTER TABLE [fo].[KeyCardCredential] ADD [AccessLevel] nvarchar(30) NOT NULL CONSTRAINT [DF_fo_KeyCardCredential_AccessLevel_Repair] DEFAULT N'Guest' WITH VALUES;
            IF COL_LENGTH(N'fo.KeyCardCredential', N'EncoderId') IS NULL
                ALTER TABLE [fo].[KeyCardCredential] ADD [EncoderId] int NOT NULL CONSTRAINT [DF_fo_KeyCardCredential_EncoderId_Repair] DEFAULT 0 WITH VALUES;
            IF COL_LENGTH(N'fo.KeyCardCredential', N'IsMaster') IS NULL
                ALTER TABLE [fo].[KeyCardCredential] ADD [IsMaster] bit NOT NULL CONSTRAINT [DF_fo_KeyCardCredential_IsMaster_Repair] DEFAULT 0 WITH VALUES;

            IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'[fo].[KeyCardCredential]') AND name = N'UQ_fo_KeyCardCredential_CardSerial')
                CREATE UNIQUE INDEX [UQ_fo_KeyCardCredential_CardSerial] ON [fo].[KeyCardCredential] ([CardSerial]);
            IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'[fo].[KeyCardCredential]') AND name = N'IX_KeyCardCredential_GuestId')
                CREATE INDEX [IX_KeyCardCredential_GuestId] ON [fo].[KeyCardCredential] ([GuestId]);
            IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'[fo].[KeyCardCredential]') AND name = N'IX_KeyCardCredential_HotelRoomId')
                CREATE INDEX [IX_KeyCardCredential_HotelRoomId] ON [fo].[KeyCardCredential] ([HotelRoomId]);
            IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'[fo].[KeyCardCredential]') AND name = N'IX_KeyCardCredential_StayId')
                CREATE INDEX [IX_KeyCardCredential_StayId] ON [fo].[KeyCardCredential] ([StayId]);
            IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'[fo].[KeyCardCredential]') AND name = N'IX_KeyCardCredential_PropertyId_StayId')
                CREATE INDEX [IX_KeyCardCredential_PropertyId_StayId] ON [fo].[KeyCardCredential] ([PropertyId], [StayId]);

            IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE parent_object_id = OBJECT_ID(N'[fo].[KeyCardCredential]') AND name = N'FK_KeyCardCredential_HoSoKhach_GuestId')
                ALTER TABLE [fo].[KeyCardCredential] ADD CONSTRAINT [FK_KeyCardCredential_HoSoKhach_GuestId]
                    FOREIGN KEY ([GuestId]) REFERENCES [fo].[HoSoKhach] ([GuestProfileId]) ON DELETE NO ACTION;
            IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE parent_object_id = OBJECT_ID(N'[fo].[KeyCardCredential]') AND name = N'FK_KeyCardCredential_PhongKhachSan_HotelRoomId')
                ALTER TABLE [fo].[KeyCardCredential] ADD CONSTRAINT [FK_KeyCardCredential_PhongKhachSan_HotelRoomId]
                    FOREIGN KEY ([HotelRoomId]) REFERENCES [fo].[PhongKhachSan] ([HotelRoomId]) ON DELETE NO ACTION;
            IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE parent_object_id = OBJECT_ID(N'[fo].[KeyCardCredential]') AND name = N'FK_KeyCardCredential_LuuTruKhachSan_StayId')
                ALTER TABLE [fo].[KeyCardCredential] ADD CONSTRAINT [FK_KeyCardCredential_LuuTruKhachSan_StayId]
                    FOREIGN KEY ([StayId]) REFERENCES [fo].[LuuTruKhachSan] ([HotelStayId]) ON DELETE NO ACTION;

            IF OBJECT_ID(N'[fo].[KeyCardEvent]', N'U') IS NULL
            BEGIN
                CREATE TABLE [fo].[KeyCardEvent]
                (
                    [EventId] uniqueidentifier NOT NULL,
                    [AccessGranted] bit NOT NULL,
                    [CredentialId] uniqueidentifier NOT NULL,
                    [DoorId] nvarchar(80) NULL,
                    [EventType] nvarchar(40) NOT NULL,
                    [Notes] nvarchar(500) NULL,
                    [OccurredAtUtc] datetime2 NOT NULL,
                    [PropertyId] uniqueidentifier NOT NULL,
                    CONSTRAINT [PK_fo_KeyCardEvent] PRIMARY KEY ([EventId]),
                    CONSTRAINT [FK_KeyCardEvent_KeyCardCredential_CredentialId]
                        FOREIGN KEY ([CredentialId]) REFERENCES [fo].[KeyCardCredential] ([CredentialId]) ON DELETE CASCADE
                );
            END;

            IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'[fo].[KeyCardEvent]') AND name = N'IX_KeyCardEvent_CredentialId')
                CREATE INDEX [IX_KeyCardEvent_CredentialId] ON [fo].[KeyCardEvent] ([CredentialId]);
            IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'[fo].[KeyCardEvent]') AND name = N'IX_KeyCardEvent_PropertyId_CredentialId_OccurredAtUtc')
                CREATE INDEX [IX_KeyCardEvent_PropertyId_CredentialId_OccurredAtUtc] ON [fo].[KeyCardEvent] ([PropertyId], [CredentialId], [OccurredAtUtc]);
            IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE parent_object_id = OBJECT_ID(N'[fo].[KeyCardEvent]') AND name = N'FK_KeyCardEvent_KeyCardCredential_CredentialId')
                ALTER TABLE [fo].[KeyCardEvent] ADD CONSTRAINT [FK_KeyCardEvent_KeyCardCredential_CredentialId]
                    FOREIGN KEY ([CredentialId]) REFERENCES [fo].[KeyCardCredential] ([CredentialId]) ON DELETE CASCADE;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Do not drop issued credentials or access history when rolling back a repair.
    }
}
