using ASMILE.BO.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ASMILE.BO.Infrastructure.Persistence.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260909140000_RepairHotelStayOperationalFields")]
public partial class RepairHotelStayOperationalFields : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        AddColumnIfMissing(migrationBuilder, "AreRoomPreferencesConfirmed", "bit NOT NULL CONSTRAINT [DF_LuuTruKhachSan_AreRoomPreferencesConfirmed] DEFAULT 0");
        AddColumnIfMissing(migrationBuilder, "AreSpecialRequestsConfirmed", "bit NOT NULL CONSTRAINT [DF_LuuTruKhachSan_AreSpecialRequestsConfirmed] DEFAULT 0");
        AddColumnIfMissing(migrationBuilder, "DepartureNotes", "nvarchar(max) NULL");
        AddColumnIfMissing(migrationBuilder, "DepartureReason", "nvarchar(max) NULL");
        AddColumnIfMissing(migrationBuilder, "Eta", "datetime2 NULL");
        AddColumnIfMissing(migrationBuilder, "ExtensionCount", "int NOT NULL CONSTRAINT [DF_LuuTruKhachSan_ExtensionCount] DEFAULT 0");
        AddColumnIfMissing(migrationBuilder, "HasOutstandingBalance", "bit NOT NULL CONSTRAINT [DF_LuuTruKhachSan_HasOutstandingBalance] DEFAULT 0");
        AddColumnIfMissing(migrationBuilder, "HasPendingCharges", "bit NOT NULL CONSTRAINT [DF_LuuTruKhachSan_HasPendingCharges] DEFAULT 0");
        AddColumnIfMissing(migrationBuilder, "IsDepartureApproved", "bit NOT NULL CONSTRAINT [DF_LuuTruKhachSan_IsDepartureApproved] DEFAULT 0");
        AddColumnIfMissing(migrationBuilder, "IsDoNotDisturb", "bit NOT NULL CONSTRAINT [DF_LuuTruKhachSan_IsDoNotDisturb] DEFAULT 0");
        AddColumnIfMissing(migrationBuilder, "IsHouseUse", "bit NOT NULL CONSTRAINT [DF_LuuTruKhachSan_IsHouseUse] DEFAULT 0");
        AddColumnIfMissing(migrationBuilder, "IsIdentityVerified", "bit NOT NULL CONSTRAINT [DF_LuuTruKhachSan_IsIdentityVerified] DEFAULT 0");
        AddColumnIfMissing(migrationBuilder, "IsVipService", "bit NOT NULL CONSTRAINT [DF_LuuTruKhachSan_IsVipService] DEFAULT 0");
        AddColumnIfMissing(migrationBuilder, "LateCheckoutRequestedAtUtc", "datetime2 NULL");
        AddColumnIfMissing(migrationBuilder, "LateCheckoutUntilUtc", "datetime2 NULL");
        AddColumnIfMissing(migrationBuilder, "OriginalDepartureDate", "date NULL");
        AddColumnIfMissing(migrationBuilder, "PreArrivalConfirmedAtUtc", "datetime2 NULL");
        AddColumnIfMissing(migrationBuilder, "PreCheckInCompletedAtUtc", "datetime2 NULL");
        AddColumnIfMissing(migrationBuilder, "SpecialRequests", "nvarchar(max) NULL");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        DropColumnIfPresent(migrationBuilder, "SpecialRequests", "DF_LuuTruKhachSan_SpecialRequests");
        DropColumnIfPresent(migrationBuilder, "PreCheckInCompletedAtUtc", "DF_LuuTruKhachSan_PreCheckInCompletedAtUtc");
        DropColumnIfPresent(migrationBuilder, "PreArrivalConfirmedAtUtc", "DF_LuuTruKhachSan_PreArrivalConfirmedAtUtc");
        DropColumnIfPresent(migrationBuilder, "OriginalDepartureDate", "DF_LuuTruKhachSan_OriginalDepartureDate");
        DropColumnIfPresent(migrationBuilder, "LateCheckoutUntilUtc", "DF_LuuTruKhachSan_LateCheckoutUntilUtc");
        DropColumnIfPresent(migrationBuilder, "LateCheckoutRequestedAtUtc", "DF_LuuTruKhachSan_LateCheckoutRequestedAtUtc");
        DropColumnIfPresent(migrationBuilder, "IsVipService", "DF_LuuTruKhachSan_IsVipService");
        DropColumnIfPresent(migrationBuilder, "IsIdentityVerified", "DF_LuuTruKhachSan_IsIdentityVerified");
        DropColumnIfPresent(migrationBuilder, "IsHouseUse", "DF_LuuTruKhachSan_IsHouseUse");
        DropColumnIfPresent(migrationBuilder, "IsDoNotDisturb", "DF_LuuTruKhachSan_IsDoNotDisturb");
        DropColumnIfPresent(migrationBuilder, "IsDepartureApproved", "DF_LuuTruKhachSan_IsDepartureApproved");
        DropColumnIfPresent(migrationBuilder, "HasPendingCharges", "DF_LuuTruKhachSan_HasPendingCharges");
        DropColumnIfPresent(migrationBuilder, "HasOutstandingBalance", "DF_LuuTruKhachSan_HasOutstandingBalance");
        DropColumnIfPresent(migrationBuilder, "ExtensionCount", "DF_LuuTruKhachSan_ExtensionCount");
        DropColumnIfPresent(migrationBuilder, "Eta", "DF_LuuTruKhachSan_Eta");
        DropColumnIfPresent(migrationBuilder, "DepartureReason", "DF_LuuTruKhachSan_DepartureReason");
        DropColumnIfPresent(migrationBuilder, "DepartureNotes", "DF_LuuTruKhachSan_DepartureNotes");
        DropColumnIfPresent(migrationBuilder, "AreSpecialRequestsConfirmed", "DF_LuuTruKhachSan_AreSpecialRequestsConfirmed");
        DropColumnIfPresent(migrationBuilder, "AreRoomPreferencesConfirmed", "DF_LuuTruKhachSan_AreRoomPreferencesConfirmed");
    }

    private static void AddColumnIfMissing(MigrationBuilder migrationBuilder, string columnName, string definition)
    {
        migrationBuilder.Sql($"""
            IF OBJECT_ID(N'[fo].[LuuTruKhachSan]', N'U') IS NOT NULL
               AND COL_LENGTH(N'fo.LuuTruKhachSan', N'{columnName}') IS NULL
                ALTER TABLE [fo].[LuuTruKhachSan]
                ADD [{columnName}] {definition};
            """);
    }

    private static void DropColumnIfPresent(MigrationBuilder migrationBuilder, string columnName, string defaultConstraintName)
    {
        migrationBuilder.Sql($"""
            IF OBJECT_ID(N'[fo].[LuuTruKhachSan]', N'U') IS NOT NULL
            BEGIN
                IF EXISTS (
                    SELECT 1
                    FROM sys.default_constraints
                    WHERE name = N'{defaultConstraintName}'
                      AND parent_object_id = OBJECT_ID(N'[fo].[LuuTruKhachSan]')
                )
                    ALTER TABLE [fo].[LuuTruKhachSan]
                    DROP CONSTRAINT [{defaultConstraintName}];

                IF COL_LENGTH(N'fo.LuuTruKhachSan', N'{columnName}') IS NOT NULL
                    ALTER TABLE [fo].[LuuTruKhachSan] DROP COLUMN [{columnName}];
            END;
            """);
    }
}
