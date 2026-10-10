using ASMILE.BO.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace ASMILE.BO.Infrastructure.Persistence.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260922100000_AddHousekeepingRoomStatusHistory")]
public sealed class AddHousekeepingRoomStatusHistory : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Additive, idempotent schema repair: existing installations keep their
        // current room-state projection and start recording immutable history
        // after this migration. No existing operational data is modified.
        migrationBuilder.Sql("""
            IF SCHEMA_ID(N'fo') IS NULL
                EXEC(N'CREATE SCHEMA [fo]');

            IF OBJECT_ID(N'[fo].[LichSuTrangThaiDonDep]', N'U') IS NULL
            BEGIN
                CREATE TABLE [fo].[LichSuTrangThaiDonDep]
                (
                    [RoomHousekeepingStatusHistoryId] uniqueidentifier NOT NULL,
                    [PropertyId] uniqueidentifier NOT NULL,
                    [HotelRoomId] uniqueidentifier NOT NULL,
                    [FromStatus] nvarchar(30) NULL,
                    [ToStatus] nvarchar(30) NOT NULL,
                    [Source] nvarchar(80) NOT NULL,
                    [HousekeepingTaskId] uniqueidentifier NULL,
                    [OccurredAtUtc] datetime2 NOT NULL,
                    CONSTRAINT [PK_fo_LichSuTrangThaiDonDep] PRIMARY KEY ([RoomHousekeepingStatusHistoryId]),
                    CONSTRAINT [FK_fo_LichSuTrangThaiDonDep_PhongKhachSan]
                        FOREIGN KEY ([HotelRoomId]) REFERENCES [fo].[PhongKhachSan] ([HotelRoomId]) ON DELETE NO ACTION
                );
            END;

            IF NOT EXISTS
            (
                SELECT 1
                FROM sys.indexes
                WHERE object_id = OBJECT_ID(N'[fo].[LichSuTrangThaiDonDep]')
                    AND name = N'IX_fo_LichSuTrangThaiDonDep_CoSo_Phong_ThoiGian'
            )
                CREATE INDEX [IX_fo_LichSuTrangThaiDonDep_CoSo_Phong_ThoiGian]
                    ON [fo].[LichSuTrangThaiDonDep] ([PropertyId], [HotelRoomId], [OccurredAtUtc]);

            -- Establish a non-destructive baseline for rooms that already had
            -- a current-state projection before history was introduced. Older
            -- state changes cannot be reconstructed and remain visibly outside
            -- the report's history coverage.
            IF OBJECT_ID(N'[fo].[TrangThaiDonDep]', N'U') IS NOT NULL
                INSERT INTO [fo].[LichSuTrangThaiDonDep]
                    ([RoomHousekeepingStatusHistoryId], [PropertyId], [HotelRoomId], [FromStatus], [ToStatus], [Source], [HousekeepingTaskId], [OccurredAtUtc])
                SELECT NEWID(), state.[PropertyId], state.[HotelRoomId], NULL, state.[Status], N'MigrationBaseline', NULL, state.[StatusChangedAtUtc]
                FROM [fo].[TrangThaiDonDep] state
                WHERE NOT EXISTS
                (
                    SELECT 1
                    FROM [fo].[LichSuTrangThaiDonDep] history
                    WHERE history.[PropertyId] = state.[PropertyId]
                        AND history.[HotelRoomId] = state.[HotelRoomId]
                        AND history.[Source] = N'MigrationBaseline'
                );
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Keep an immutable audit trail if a deployment is rolled back.
    }
}
