using ASMILE.BO.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ASMILE.BO.Infrastructure.Persistence.Migrations;

/// <summary>
/// Repairs a historical table-name drift: the EF model uses fo.PhongDoan,
/// while the original migration created fo.PhanPhongDoan. The repair is
/// intentionally idempotent so it is safe for databases that already contain
/// the corrected table.
/// </summary>
[DbContext(typeof(AppDbContext))]
[Migration("20260914170000_RepairGroupRoomAssignmentTableName")]
public partial class RepairGroupRoomAssignmentTableName : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            IF OBJECT_ID(N'[fo].[PhongDoan]', N'U') IS NOT NULL
               AND OBJECT_ID(N'[fo].[PhanPhongDoan]', N'U') IS NOT NULL
               AND EXISTS (SELECT 1 FROM [fo].[PhanPhongDoan])
                THROW 51000, 'GROUP_ASSIGNMENT_SCHEMA_CONFLICT: both fo.PhongDoan and fo.PhanPhongDoan exist and the legacy table contains data.', 1;

            IF OBJECT_ID(N'[fo].[PhongDoan]', N'U') IS NULL
               AND OBJECT_ID(N'[fo].[PhanPhongDoan]', N'U') IS NOT NULL
                EXEC sp_rename N'[fo].[PhanPhongDoan]', N'PhongDoan';

            IF OBJECT_ID(N'[fo].[PhongDoan]', N'U') IS NULL
            BEGIN
                CREATE TABLE [fo].[PhongDoan]
                (
                    [GroupRoomAssignmentId] uniqueidentifier NOT NULL,
                    [GroupReservationId] uniqueidentifier NOT NULL,
                    [PropertyId] uniqueidentifier NOT NULL,
                    [HotelRoomId] uniqueidentifier NOT NULL,
                    [RoomNumber] nvarchar(40) NOT NULL,
                    [RoomType] nvarchar(80) NOT NULL,
                    [GuestId] uniqueidentifier NULL,
                    [GuestName] nvarchar(200) NULL,
                    [IsVip] bit NOT NULL CONSTRAINT [DF_fo_PhongDoan_IsVip] DEFAULT (0),
                    [Adults] int NOT NULL,
                    [Children] int NOT NULL,
                    [IsCheckedIn] bit NOT NULL CONSTRAINT [DF_fo_PhongDoan_IsCheckedIn] DEFAULT (0),
                    [IsCheckedOut] bit NOT NULL CONSTRAINT [DF_fo_PhongDoan_IsCheckedOut] DEFAULT (0),
                    [ReservationId] uniqueidentifier NULL,
                    [CreatedAtUtc] datetime2 NOT NULL,
                    CONSTRAINT [PK_fo_PhongDoan] PRIMARY KEY ([GroupRoomAssignmentId]),
                    CONSTRAINT [FK_PhongDoan_DatPhongDoan_GroupReservationId]
                        FOREIGN KEY ([GroupReservationId]) REFERENCES [fo].[DatPhongDoan] ([GroupReservationId]) ON DELETE CASCADE
                );
            END;

            IF COL_LENGTH(N'fo.PhongDoan', N'RoomType') IS NOT NULL
            BEGIN
                ALTER TABLE [fo].[PhongDoan] ALTER COLUMN [RoomType] nvarchar(80) NOT NULL;
            END;

            IF EXISTS
            (
                SELECT 1
                FROM sys.foreign_keys
                WHERE parent_object_id = OBJECT_ID(N'[fo].[PhongDoan]')
                  AND name <> N'FK_PhongDoan_DatPhongDoan_GroupReservationId'
            )
            BEGIN
                DECLARE @legacyFk nvarchar(255);
                SELECT TOP (1) @legacyFk = QUOTENAME(name)
                FROM sys.foreign_keys
                WHERE parent_object_id = OBJECT_ID(N'[fo].[PhongDoan]')
                  AND name <> N'FK_PhongDoan_DatPhongDoan_GroupReservationId';
                EXEC (N'ALTER TABLE [fo].[PhongDoan] DROP CONSTRAINT ' + @legacyFk);
            END;

            IF NOT EXISTS
            (
                SELECT 1 FROM sys.foreign_keys
                WHERE parent_object_id = OBJECT_ID(N'[fo].[PhongDoan]')
                  AND name = N'FK_PhongDoan_DatPhongDoan_GroupReservationId'
            )
                ALTER TABLE [fo].[PhongDoan]
                ADD CONSTRAINT [FK_PhongDoan_DatPhongDoan_GroupReservationId]
                    FOREIGN KEY ([GroupReservationId]) REFERENCES [fo].[DatPhongDoan] ([GroupReservationId]) ON DELETE CASCADE;

            IF NOT EXISTS
            (
                SELECT 1 FROM sys.indexes
                WHERE object_id = OBJECT_ID(N'[fo].[PhongDoan]')
                  AND name = N'UQ_fo_PhongDoan_Doan_Phong'
            )
                CREATE UNIQUE INDEX [UQ_fo_PhongDoan_Doan_Phong]
                    ON [fo].[PhongDoan] ([GroupReservationId], [HotelRoomId]);
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            IF OBJECT_ID(N'[fo].[PhongDoan]', N'U') IS NOT NULL
               AND OBJECT_ID(N'[fo].[PhanPhongDoan]', N'U') IS NULL
                EXEC sp_rename N'[fo].[PhongDoan]', N'PhanPhongDoan';
            """);
    }
}
