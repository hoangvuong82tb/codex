using ASMILE.BO.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
#nullable enable

using Microsoft.EntityFrameworkCore.Migrations;

#nullable enable

namespace ASMILE.BO.Infrastructure.Persistence.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260910100000_RepairGroupReservationDepositPolicyFields")]
public partial class RepairGroupReservationDepositPolicyFields : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        AddColumnIfMissing(
            migrationBuilder,
            "DepositRequired",
            "bit NOT NULL CONSTRAINT [DF_fo_DatPhongDoan_DepositRequired] DEFAULT 0");
        AddColumnIfMissing(migrationBuilder, "GuaranteeType", "nvarchar(40) NULL");
        AddColumnIfMissing(migrationBuilder, "DepositCutoffDate", "date NULL");
        AddColumnIfMissing(migrationBuilder, "DepositReleaseDate", "date NULL");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        DropColumnIfPresent(migrationBuilder, "DepositReleaseDate", null);
        DropColumnIfPresent(migrationBuilder, "DepositCutoffDate", null);
        DropColumnIfPresent(migrationBuilder, "GuaranteeType", null);
        DropColumnIfPresent(
            migrationBuilder,
            "DepositRequired",
            "DF_fo_DatPhongDoan_DepositRequired");
    }

    private static void AddColumnIfMissing(MigrationBuilder migrationBuilder, string columnName, string definition)
    {
        migrationBuilder.Sql($"""
            IF OBJECT_ID(N'[fo].[DatPhongDoan]', N'U') IS NOT NULL
               AND COL_LENGTH(N'fo.DatPhongDoan', N'{columnName}') IS NULL
                ALTER TABLE [fo].[DatPhongDoan]
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
                      AND parent_object_id = OBJECT_ID(N'[fo].[DatPhongDoan]')
                )
                    ALTER TABLE [fo].[DatPhongDoan]
                    DROP CONSTRAINT [{defaultConstraintName}];
            """;

        migrationBuilder.Sql($"""
            IF OBJECT_ID(N'[fo].[DatPhongDoan]', N'U') IS NOT NULL
            BEGIN
                {dropConstraint}
                IF COL_LENGTH(N'fo.DatPhongDoan', N'{columnName}') IS NOT NULL
                    ALTER TABLE [fo].[DatPhongDoan] DROP COLUMN [{columnName}];
            END;
            """);
    }
}
