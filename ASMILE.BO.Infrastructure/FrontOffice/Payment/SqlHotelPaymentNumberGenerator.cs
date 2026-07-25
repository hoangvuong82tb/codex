using ASMILE.BO.Application.FrontOffice.Payment;
using ASMILE.BO.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace ASMILE.BO.Infrastructure.FrontOffice.Payment;

public sealed class SqlHotelPaymentNumberGenerator(AppDbContext dbContext) : IHotelPaymentNumberGenerator
{
    public async Task<string> GenerateAsync(Guid propertyId, DateOnly businessDate, CancellationToken cancellationToken = default)
    {
        if (!dbContext.Database.IsSqlServer()) throw new InvalidOperationException("Payment numbering requires SQL Server.");
        await dbContext.Database.OpenConnectionAsync(cancellationToken);
        try
        {
            await using var command = dbContext.Database.GetDbConnection().CreateCommand();
            command.CommandText = "SELECT NEXT VALUE FOR [fo].[TrinhTuSoThanhToan]";
            command.Transaction = dbContext.Database.CurrentTransaction?.GetDbTransaction();
            var value = Convert.ToInt64(await command.ExecuteScalarAsync(cancellationToken));
            return $"PMT-{businessDate:yyyyMM}-{value:D6}";
        }
        finally { await dbContext.Database.CloseConnectionAsync(); }
    }
}
