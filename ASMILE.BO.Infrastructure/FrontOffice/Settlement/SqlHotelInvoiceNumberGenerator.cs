using ASMILE.BO.Application.FrontOffice.Settlement;
using ASMILE.BO.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace ASMILE.BO.Infrastructure.FrontOffice.Settlement;

public sealed class SqlHotelInvoiceNumberGenerator(AppDbContext dbContext) : IHotelInvoiceNumberGenerator
{
    public async Task<string> GenerateAsync(Guid propertyId, DateOnly businessDate, CancellationToken ct = default)
    {
        if (!dbContext.Database.IsSqlServer()) throw new InvalidOperationException("Invoice numbering requires SQL Server.");
        await dbContext.Database.OpenConnectionAsync(ct);
        try
        {
            await using var cmd = dbContext.Database.GetDbConnection().CreateCommand();
            cmd.CommandText = "SELECT NEXT VALUE FOR [fo].[TrinhTuSoHoaDon]";
            cmd.Transaction = dbContext.Database.CurrentTransaction?.GetDbTransaction();
            return $"INV-{businessDate:yyyyMM}-{Convert.ToInt64(await cmd.ExecuteScalarAsync(ct)):D6}";
        }
        finally { await dbContext.Database.CloseConnectionAsync(); }
    }
}
