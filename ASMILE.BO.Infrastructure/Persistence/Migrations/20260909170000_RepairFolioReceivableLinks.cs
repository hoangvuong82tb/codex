using ASMILE.BO.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ASMILE.BO.Infrastructure.Persistence.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260909170000_RepairFolioReceivableLinks")]
public partial class RepairFolioReceivableLinks : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        AddColumnIfMissing(migrationBuilder, "Invoices", "FolioId");
        AddColumnIfMissing(migrationBuilder, "ReceivableTransactions", "FolioId");
        AddColumnIfMissing(migrationBuilder, "ReceivableTransactions", "PropertyId");

        CreateIndexIfMissing(migrationBuilder, "Invoices", "IX_Invoices_FolioId", "[FolioId]");
        CreateIndexIfMissing(migrationBuilder, "ReceivableTransactions", "IX_ReceivableTransactions_FolioId", "[FolioId]");
        CreateIndexIfMissing(migrationBuilder, "ReceivableTransactions", "IX_ReceivableTransactions_PropertyId", "[PropertyId]");
        CreateIndexIfMissing(migrationBuilder, "ReceivableTransactions", "IX_ReceivableTransactions_PropertyId_FolioId_DocumentDate", "[PropertyId], [FolioId], [DocumentDate]");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        DropIndexIfPresent(migrationBuilder, "ReceivableTransactions", "IX_ReceivableTransactions_PropertyId_FolioId_DocumentDate");
        DropIndexIfPresent(migrationBuilder, "ReceivableTransactions", "IX_ReceivableTransactions_PropertyId");
        DropIndexIfPresent(migrationBuilder, "ReceivableTransactions", "IX_ReceivableTransactions_FolioId");
        DropIndexIfPresent(migrationBuilder, "Invoices", "IX_Invoices_FolioId");
        DropColumnIfPresent(migrationBuilder, "ReceivableTransactions", "PropertyId");
        DropColumnIfPresent(migrationBuilder, "ReceivableTransactions", "FolioId");
        DropColumnIfPresent(migrationBuilder, "Invoices", "FolioId");
    }

    private static void AddColumnIfMissing(MigrationBuilder migrationBuilder, string tableName, string columnName)
    {
        migrationBuilder.Sql($"""
            IF OBJECT_ID(N'[dbo].[{tableName}]', N'U') IS NOT NULL
               AND COL_LENGTH(N'dbo.{tableName}', N'{columnName}') IS NULL
                ALTER TABLE [dbo].[{tableName}] ADD [{columnName}] uniqueidentifier NULL;
            """);
    }

    private static void CreateIndexIfMissing(MigrationBuilder migrationBuilder, string tableName, string indexName, string columns)
    {
        migrationBuilder.Sql($"""
            IF OBJECT_ID(N'[dbo].[{tableName}]', N'U') IS NOT NULL
               AND NOT EXISTS (
                    SELECT 1
                    FROM sys.indexes
                    WHERE name = N'{indexName}'
                      AND object_id = OBJECT_ID(N'[dbo].[{tableName}]')
               )
                CREATE INDEX [{indexName}] ON [dbo].[{tableName}]({columns});
            """);
    }

    private static void DropIndexIfPresent(MigrationBuilder migrationBuilder, string tableName, string indexName)
    {
        migrationBuilder.Sql($"""
            IF OBJECT_ID(N'[dbo].[{tableName}]', N'U') IS NOT NULL
               AND EXISTS (
                    SELECT 1
                    FROM sys.indexes
                    WHERE name = N'{indexName}'
                      AND object_id = OBJECT_ID(N'[dbo].[{tableName}]')
               )
                DROP INDEX [{indexName}] ON [dbo].[{tableName}];
            """);
    }

    private static void DropColumnIfPresent(MigrationBuilder migrationBuilder, string tableName, string columnName)
    {
        migrationBuilder.Sql($"""
            IF OBJECT_ID(N'[dbo].[{tableName}]', N'U') IS NOT NULL
               AND COL_LENGTH(N'dbo.{tableName}', N'{columnName}') IS NOT NULL
                ALTER TABLE [dbo].[{tableName}] DROP COLUMN [{columnName}];
            """);
    }
}
