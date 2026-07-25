using ASMILE.BO.Application.FrontOffice.Stay;
using ASMILE.BO.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace ASMILE.BO.Infrastructure.FrontOffice.Stay;

public sealed class SqlHotelStayNumberGenerator(AppDbContext dbContext) : IHotelStayNumberGenerator
{
    public async Task<string> GenerateAsync(Guid propertyId, DateOnly businessDate, CancellationToken cancellationToken = default)
    {
        if (!dbContext.Database.IsSqlServer()) throw new InvalidOperationException("Stay numbering requires SQL Server.");
        await dbContext.Database.OpenConnectionAsync(cancellationToken);
        try
        {
            await using var command = dbContext.Database.GetDbConnection().CreateCommand();
            command.CommandText = "SELECT NEXT VALUE FOR [fo].[TrinhTuSoLuuTru]";
            command.Transaction = dbContext.Database.CurrentTransaction?.GetDbTransaction();
            var value = Convert.ToInt64(await command.ExecuteScalarAsync(cancellationToken));
            return $"STY-{businessDate:yyyyMM}-{value:D6}";
        }
        finally { await dbContext.Database.CloseConnectionAsync(); }
    }
}
