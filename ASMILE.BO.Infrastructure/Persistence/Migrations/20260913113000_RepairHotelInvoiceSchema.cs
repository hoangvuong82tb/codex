using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using ASMILE.BO.Infrastructure.Persistence;

#nullable disable

namespace ASMILE.BO.Infrastructure.Persistence.Migrations;

/// <summary>
/// Repairs installations where the historical migration was recorded as applied
/// but the Front Office invoice table was not restored with the database backup.
/// The SQL is deliberately idempotent so it also repairs partially restored tables.
/// </summary>
[DbContext(typeof(AppDbContext))]
[Migration("20260913113000_RepairHotelInvoiceSchema")]
public partial class RepairHotelInvoiceSchema : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            IF SCHEMA_ID(N'fo') IS NULL
                EXEC(N'CREATE SCHEMA [fo]');

            IF OBJECT_ID(N'[fo].[HoaDon]', N'U') IS NULL
            BEGIN
                CREATE TABLE [fo].[HoaDon]
                (
                    [HotelInvoiceId] uniqueidentifier NOT NULL,
                    [PropertyId] uniqueidentifier NOT NULL,
                    [HotelFolioId] uniqueidentifier NOT NULL,
                    [HotelStayId] uniqueidentifier NOT NULL,
                    [InvoiceNumber] nvarchar(40) NOT NULL,
                    [Status] nvarchar(20) NOT NULL,
                    [Subtotal] decimal(18,2) NOT NULL,
                    [TaxAmount] decimal(18,2) NOT NULL,
                    [TotalAmount] decimal(18,2) NOT NULL,
                    [PaidAmount] decimal(18,2) NOT NULL,
                    [OutstandingAmount] decimal(18,2) NOT NULL,
                    [GuestName] nvarchar(200) NULL,
                    [RoomNumber] nvarchar(40) NULL,
                    [StayDates] nvarchar(200) NULL,
                    [InvoiceDataJson] nvarchar(max) NOT NULL,
                    [CreatedAtUtc] datetime2 NOT NULL,
                    [CreatedBy] uniqueidentifier NULL,
                    [PrintedAtUtc] datetime2 NULL,
                    [RowVersion] rowversion NULL,
                    CONSTRAINT [PK_fo_HoaDon] PRIMARY KEY ([HotelInvoiceId])
                );
            END;

            IF COL_LENGTH(N'fo.HoaDon', N'PropertyId') IS NULL
                ALTER TABLE [fo].[HoaDon] ADD [PropertyId] uniqueidentifier NULL;
            IF COL_LENGTH(N'fo.HoaDon', N'HotelFolioId') IS NULL
                ALTER TABLE [fo].[HoaDon] ADD [HotelFolioId] uniqueidentifier NULL;
            IF COL_LENGTH(N'fo.HoaDon', N'HotelStayId') IS NULL
                ALTER TABLE [fo].[HoaDon] ADD [HotelStayId] uniqueidentifier NULL;
            IF COL_LENGTH(N'fo.HoaDon', N'InvoiceNumber') IS NULL
                ALTER TABLE [fo].[HoaDon] ADD [InvoiceNumber] nvarchar(40) NULL;
            IF COL_LENGTH(N'fo.HoaDon', N'Status') IS NULL
                ALTER TABLE [fo].[HoaDon] ADD [Status] nvarchar(20) NULL;
            IF COL_LENGTH(N'fo.HoaDon', N'Subtotal') IS NULL
                ALTER TABLE [fo].[HoaDon] ADD [Subtotal] decimal(18,2) NULL;
            IF COL_LENGTH(N'fo.HoaDon', N'TaxAmount') IS NULL
                ALTER TABLE [fo].[HoaDon] ADD [TaxAmount] decimal(18,2) NULL;
            IF COL_LENGTH(N'fo.HoaDon', N'TotalAmount') IS NULL
                ALTER TABLE [fo].[HoaDon] ADD [TotalAmount] decimal(18,2) NULL;
            IF COL_LENGTH(N'fo.HoaDon', N'PaidAmount') IS NULL
                ALTER TABLE [fo].[HoaDon] ADD [PaidAmount] decimal(18,2) NULL;
            IF COL_LENGTH(N'fo.HoaDon', N'OutstandingAmount') IS NULL
                ALTER TABLE [fo].[HoaDon] ADD [OutstandingAmount] decimal(18,2) NULL;
            IF COL_LENGTH(N'fo.HoaDon', N'GuestName') IS NULL
                ALTER TABLE [fo].[HoaDon] ADD [GuestName] nvarchar(200) NULL;
            IF COL_LENGTH(N'fo.HoaDon', N'RoomNumber') IS NULL
                ALTER TABLE [fo].[HoaDon] ADD [RoomNumber] nvarchar(40) NULL;
            IF COL_LENGTH(N'fo.HoaDon', N'StayDates') IS NULL
                ALTER TABLE [fo].[HoaDon] ADD [StayDates] nvarchar(200) NULL;
            IF COL_LENGTH(N'fo.HoaDon', N'InvoiceDataJson') IS NULL
                ALTER TABLE [fo].[HoaDon] ADD [InvoiceDataJson] nvarchar(max) NULL;
            IF COL_LENGTH(N'fo.HoaDon', N'CreatedAtUtc') IS NULL
                ALTER TABLE [fo].[HoaDon] ADD [CreatedAtUtc] datetime2 NULL;
            IF COL_LENGTH(N'fo.HoaDon', N'CreatedBy') IS NULL
                ALTER TABLE [fo].[HoaDon] ADD [CreatedBy] uniqueidentifier NULL;
            IF COL_LENGTH(N'fo.HoaDon', N'PrintedAtUtc') IS NULL
                ALTER TABLE [fo].[HoaDon] ADD [PrintedAtUtc] datetime2 NULL;
            IF COL_LENGTH(N'fo.HoaDon', N'RowVersion') IS NULL
                ALTER TABLE [fo].[HoaDon] ADD [RowVersion] rowversion;

            IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UQ_fo_HoaDon_SoHoaDon' AND object_id = OBJECT_ID(N'[fo].[HoaDon]'))
                CREATE UNIQUE INDEX [UQ_fo_HoaDon_SoHoaDon] ON [fo].[HoaDon] ([InvoiceNumber]) WHERE [InvoiceNumber] IS NOT NULL;
            IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_fo_HoaDon_CoSo_Folio' AND object_id = OBJECT_ID(N'[fo].[HoaDon]'))
                CREATE INDEX [IX_fo_HoaDon_CoSo_Folio] ON [fo].[HoaDon] ([PropertyId], [HotelFolioId]);
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // This is a repair migration.  Never drop an existing production invoice
        // table during rollback; a rollback must be handled by a backup restore.
    }
}
