using ASMILE.BO.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
#nullable enable

using Microsoft.EntityFrameworkCore.Migrations;

#nullable enable

namespace ASMILE.BO.Infrastructure.Persistence.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260910110000_RepairHotelRoomOperationalAndPhysicalFields")]
public partial class RepairHotelRoomOperationalAndPhysicalFields : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        AddColumnIfMissing(migrationBuilder, "OperationalStatusChangedAtUtc", "datetime2 NULL");
        AddColumnIfMissing(migrationBuilder, "OperationalStatusExpiresAtUtc", "datetime2 NULL");
        AddColumnIfMissing(migrationBuilder, "OperationalStatusReason", "nvarchar(500) NULL");
        AddColumnIfMissing(migrationBuilder, "BedType", "nvarchar(40) NULL");
        AddColumnIfMissing(migrationBuilder, "IsConnecting", "bit NOT NULL CONSTRAINT [DF_fo_PhongKhachSan_IsConnecting] DEFAULT 0");
        AddColumnIfMissing(migrationBuilder, "IsSmoking", "bit NOT NULL CONSTRAINT [DF_fo_PhongKhachSan_IsSmoking] DEFAULT 0");
        AddColumnIfMissing(migrationBuilder, "RoomCondition", "nvarchar(30) NULL");
        AddColumnIfMissing(migrationBuilder, "View", "nvarchar(80) NULL");

        migrationBuilder.Sql("""
            IF OBJECT_ID(N'[fo].[PhongKhachSan]', N'U') IS NOT NULL
               AND NOT EXISTS (
                    SELECT 1
                    FROM sys.indexes
                    WHERE name = N'IX_fo_PhongKhachSan_CoSo_TrangThai_HetHan'
                      AND object_id = OBJECT_ID(N'[fo].[PhongKhachSan]')
               )
                CREATE INDEX [IX_fo_PhongKhachSan_CoSo_TrangThai_HetHan]
                ON [fo].[PhongKhachSan] ([PropertyId], [OperationalStatus], [OperationalStatusExpiresAtUtc]);
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            IF OBJECT_ID(N'[fo].[PhongKhachSan]', N'U') IS NOT NULL
               AND EXISTS (
                    SELECT 1
                    FROM sys.indexes
                    WHERE name = N'IX_fo_PhongKhachSan_CoSo_TrangThai_HetHan'
                      AND object_id = OBJECT_ID(N'[fo].[PhongKhachSan]')
               )
                DROP INDEX [IX_fo_PhongKhachSan_CoSo_TrangThai_HetHan] ON [fo].[PhongKhachSan];
            """);

        DropColumnIfPresent(migrationBuilder, "View", null);
        DropColumnIfPresent(migrationBuilder, "RoomCondition", null);
        DropColumnIfPresent(migrationBuilder, "IsSmoking", "DF_fo_PhongKhachSan_IsSmoking");
        DropColumnIfPresent(migrationBuilder, "IsConnecting", "DF_fo_PhongKhachSan_IsConnecting");
        DropColumnIfPresent(migrationBuilder, "BedType", null);
        DropColumnIfPresent(migrationBuilder, "OperationalStatusReason", null);
        DropColumnIfPresent(migrationBuilder, "OperationalStatusExpiresAtUtc", null);
        DropColumnIfPresent(migrationBuilder, "OperationalStatusChangedAtUtc", null);
    }

    private static void AddColumnIfMissing(MigrationBuilder migrationBuilder, string columnName, string definition)
    {
        migrationBuilder.Sql($"""
            IF OBJECT_ID(N'[fo].[PhongKhachSan]', N'U') IS NOT NULL
               AND COL_LENGTH(N'fo.PhongKhachSan', N'{columnName}') IS NULL
                ALTER TABLE [fo].[PhongKhachSan]
                ADD [{columnName}] {definition};
            """);
    }

    private static void DropColumnIfPresent(MigrationBuilder migrationBuilder, string columnName, string? defaultConstraintName)
    {
        var dropConstraint = defaultConstraintName is null
            ? string.Empty
            : $"""
                IF EXISTS (
                    SELECT 1
                    FROM sys.default_constraints
                    WHERE name = N'{defaultConstraintName}'
                      AND parent_object_id = OBJECT_ID(N'[fo].[PhongKhachSan]')
                )
                    ALTER TABLE [fo].[PhongKhachSan]
                    DROP CONSTRAINT [{defaultConstraintName}];
            """;

        migrationBuilder.Sql($"""
            IF OBJECT_ID(N'[fo].[PhongKhachSan]', N'U') IS NOT NULL
            BEGIN
                {dropConstraint}
                IF COL_LENGTH(N'fo.PhongKhachSan', N'{columnName}') IS NOT NULL
                    ALTER TABLE [fo].[PhongKhachSan] DROP COLUMN [{columnName}];
            END;
            """);
    }
}
