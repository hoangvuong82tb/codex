using ASMILE.BO.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ASMILE.BO.Infrastructure.Persistence.Migrations;

/// <summary>
/// Repairs the payment table created by the original Front Office migration.
/// The EF model has always required these fields, but older databases could
/// reach the payment screen without them and fail at insert/query time.
/// </summary>
[DbContext(typeof(AppDbContext))]
[Migration("20260915160000_RepairHotelPaymentFields")]
public partial class RepairHotelPaymentFields : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            IF OBJECT_ID(N'[fo].[ThanhToan]', N'U') IS NOT NULL
            BEGIN
                IF COL_LENGTH(N'fo.ThanhToan', N'AuthorizationCode') IS NULL
                    ALTER TABLE [fo].[ThanhToan] ADD [AuthorizationCode] nvarchar(100) NULL;

                IF COL_LENGTH(N'fo.ThanhToan', N'Currency') IS NULL
                    ALTER TABLE [fo].[ThanhToan] ADD [Currency] nvarchar(10) NOT NULL CONSTRAINT [DF_fo_ThanhToan_Currency] DEFAULT N'VND';

                IF COL_LENGTH(N'fo.ThanhToan', N'ExchangeRateToVnd') IS NULL
                    ALTER TABLE [fo].[ThanhToan] ADD [ExchangeRateToVnd] decimal(18,6) NOT NULL CONSTRAINT [DF_fo_ThanhToan_ExchangeRateToVnd] DEFAULT (1);

                IF COL_LENGTH(N'fo.ThanhToan', N'RefundedAmount') IS NULL
                    ALTER TABLE [fo].[ThanhToan] ADD [RefundedAmount] decimal(18,2) NOT NULL CONSTRAINT [DF_fo_ThanhToan_RefundedAmount] DEFAULT (0);
            END
            """);

        // Some databases have the payment aggregate tables but are missing
        // the audit history table that is part of the current model.
        migrationBuilder.Sql(
            """
            IF OBJECT_ID(N'[fo].[ThanhToan]', N'U') IS NOT NULL
               AND OBJECT_ID(N'[fo].[LichSuThanhToan]', N'U') IS NULL
            BEGIN
                CREATE TABLE [fo].[LichSuThanhToan]
                (
                    [PaymentHistoryId] uniqueidentifier NOT NULL,
                    [HotelPaymentId] uniqueidentifier NOT NULL,
                    [PropertyId] uniqueidentifier NOT NULL,
                    [Action] nvarchar(100) NOT NULL,
                    [Detail] nvarchar(500) NULL,
                    [ActorId] uniqueidentifier NULL,
                    [OccurredAtUtc] datetime2 NOT NULL,
                    CONSTRAINT [PK_fo_LichSuThanhToan] PRIMARY KEY ([PaymentHistoryId]),
                    CONSTRAINT [FK_fo_LichSuThanhToan_ThanhToan_HotelPaymentId]
                        FOREIGN KEY ([HotelPaymentId]) REFERENCES [fo].[ThanhToan] ([HotelPaymentId])
                        ON DELETE NO ACTION
                );
                CREATE INDEX [IX_fo_LichSuThanhToan_HotelPaymentId]
                    ON [fo].[LichSuThanhToan] ([HotelPaymentId]);
            END
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            IF OBJECT_ID(N'[fo].[ThanhToan]', N'U') IS NOT NULL
            BEGIN
                IF OBJECT_ID(N'[fo].[DF_fo_ThanhToan_RefundedAmount]', N'D') IS NOT NULL
                    ALTER TABLE [fo].[ThanhToan] DROP CONSTRAINT [DF_fo_ThanhToan_RefundedAmount];
                IF COL_LENGTH(N'fo.ThanhToan', N'RefundedAmount') IS NOT NULL
                    ALTER TABLE [fo].[ThanhToan] DROP COLUMN [RefundedAmount];

                IF OBJECT_ID(N'[fo].[DF_fo_ThanhToan_ExchangeRateToVnd]', N'D') IS NOT NULL
                    ALTER TABLE [fo].[ThanhToan] DROP CONSTRAINT [DF_fo_ThanhToan_ExchangeRateToVnd];
                IF COL_LENGTH(N'fo.ThanhToan', N'ExchangeRateToVnd') IS NOT NULL
                    ALTER TABLE [fo].[ThanhToan] DROP COLUMN [ExchangeRateToVnd];

                IF OBJECT_ID(N'[fo].[DF_fo_ThanhToan_Currency]', N'D') IS NOT NULL
                    ALTER TABLE [fo].[ThanhToan] DROP CONSTRAINT [DF_fo_ThanhToan_Currency];
                IF COL_LENGTH(N'fo.ThanhToan', N'Currency') IS NOT NULL
                    ALTER TABLE [fo].[ThanhToan] DROP COLUMN [Currency];

                IF COL_LENGTH(N'fo.ThanhToan', N'AuthorizationCode') IS NOT NULL
                    ALTER TABLE [fo].[ThanhToan] DROP COLUMN [AuthorizationCode];
            END
            """);
    }
}
