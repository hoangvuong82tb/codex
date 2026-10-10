using ASMILE.BO.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ASMILE.BO.Infrastructure.Persistence.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260920210000_AddMinibarSessionPersistence")]
public partial class AddMinibarSessionPersistence : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Minibar inventory and restock tables were migrated, but the session
        // and consumption entities already present in the EF model were never
        // created by a migration. Keep this repair safe for both clean installs
        // and databases where an operator has already created the tables.
        migrationBuilder.Sql("""
            IF SCHEMA_ID(N'fo') IS NULL
                EXEC(N'CREATE SCHEMA [fo]');

            IF OBJECT_ID(N'[fo].[MinibarSession]', N'U') IS NULL
            BEGIN
                CREATE TABLE [fo].[MinibarSession]
                (
                    [SessionId] uniqueidentifier NOT NULL,
                    [PropertyId] uniqueidentifier NOT NULL,
                    [StayId] uniqueidentifier NOT NULL,
                    [HotelRoomId] uniqueidentifier NOT NULL,
                    [Status] nvarchar(30) NOT NULL,
                    [CheckedAtUtc] datetime2 NULL,
                    [CheckedBy] uniqueidentifier NULL,
                    [FolioEntryId] uniqueidentifier NULL,
                    [PostedAtUtc] datetime2 NULL,
                    [RowVersion] rowversion NOT NULL,
                    CONSTRAINT [PK_fo_MinibarSession] PRIMARY KEY ([SessionId]),
                    CONSTRAINT [FK_MinibarSession_PhongKhachSan_HotelRoomId]
                        FOREIGN KEY ([HotelRoomId]) REFERENCES [fo].[PhongKhachSan] ([HotelRoomId]) ON DELETE NO ACTION,
                    CONSTRAINT [FK_MinibarSession_LuuTruKhachSan_StayId]
                        FOREIGN KEY ([StayId]) REFERENCES [fo].[LuuTruKhachSan] ([HotelStayId]) ON DELETE NO ACTION
                );
            END;

            IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'[fo].[MinibarSession]') AND name = N'IX_MinibarSession_HotelRoomId')
                CREATE INDEX [IX_MinibarSession_HotelRoomId] ON [fo].[MinibarSession] ([HotelRoomId]);
            IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'[fo].[MinibarSession]') AND name = N'IX_MinibarSession_StayId')
                CREATE INDEX [IX_MinibarSession_StayId] ON [fo].[MinibarSession] ([StayId]);
            IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'[fo].[MinibarSession]') AND name = N'IX_MinibarSession_PropertyId_StayId')
                CREATE UNIQUE INDEX [IX_MinibarSession_PropertyId_StayId] ON [fo].[MinibarSession] ([PropertyId], [StayId]);

            IF OBJECT_ID(N'[fo].[MinibarConsumption]', N'U') IS NULL
            BEGIN
                CREATE TABLE [fo].[MinibarConsumption]
                (
                    [ConsumptionId] uniqueidentifier NOT NULL,
                    [PropertyId] uniqueidentifier NOT NULL,
                    [SessionId] uniqueidentifier NOT NULL,
                    [ItemCode] nvarchar(80) NOT NULL,
                    [ItemName] nvarchar(200) NOT NULL,
                    [Quantity] decimal(18,3) NOT NULL CONSTRAINT [DF_fo_MinibarConsumption_Quantity] DEFAULT (1),
                    [UnitPrice] decimal(18,2) NOT NULL,
                    [LineAmount] decimal(18,2) NOT NULL,
                    CONSTRAINT [PK_fo_MinibarConsumption] PRIMARY KEY ([ConsumptionId]),
                    CONSTRAINT [FK_MinibarConsumption_MinibarSession_SessionId]
                        FOREIGN KEY ([SessionId]) REFERENCES [fo].[MinibarSession] ([SessionId]) ON DELETE CASCADE
                );
            END;

            IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'[fo].[MinibarConsumption]') AND name = N'IX_MinibarConsumption_SessionId')
                CREATE INDEX [IX_MinibarConsumption_SessionId] ON [fo].[MinibarConsumption] ([SessionId]);
            IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'[fo].[MinibarConsumption]') AND name = N'IX_MinibarConsumption_PropertyId_SessionId')
                CREATE INDEX [IX_MinibarConsumption_PropertyId_SessionId] ON [fo].[MinibarConsumption] ([PropertyId], [SessionId]);

            IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE parent_object_id = OBJECT_ID(N'[fo].[MinibarSession]') AND name = N'FK_MinibarSession_PhongKhachSan_HotelRoomId')
                ALTER TABLE [fo].[MinibarSession] ADD CONSTRAINT [FK_MinibarSession_PhongKhachSan_HotelRoomId]
                    FOREIGN KEY ([HotelRoomId]) REFERENCES [fo].[PhongKhachSan] ([HotelRoomId]) ON DELETE NO ACTION;
            IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE parent_object_id = OBJECT_ID(N'[fo].[MinibarSession]') AND name = N'FK_MinibarSession_LuuTruKhachSan_StayId')
                ALTER TABLE [fo].[MinibarSession] ADD CONSTRAINT [FK_MinibarSession_LuuTruKhachSan_StayId]
                    FOREIGN KEY ([StayId]) REFERENCES [fo].[LuuTruKhachSan] ([HotelStayId]) ON DELETE NO ACTION;
            IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE parent_object_id = OBJECT_ID(N'[fo].[MinibarConsumption]') AND name = N'FK_MinibarConsumption_MinibarSession_SessionId')
                ALTER TABLE [fo].[MinibarConsumption] ADD CONSTRAINT [FK_MinibarConsumption_MinibarSession_SessionId]
                    FOREIGN KEY ([SessionId]) REFERENCES [fo].[MinibarSession] ([SessionId]) ON DELETE CASCADE;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Do not drop operational minibar records when rolling back a schema repair.
    }
}
