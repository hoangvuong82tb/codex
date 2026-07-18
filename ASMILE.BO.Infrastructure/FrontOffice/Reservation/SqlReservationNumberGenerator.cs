using System.Data;
using ASMILE.BO.Application.FrontOffice.Reservation;
using ASMILE.BO.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace ASMILE.BO.Infrastructure.FrontOffice.Reservation;

public sealed class SqlReservationNumberGenerator(AppDbContext dbContext) : IReservationNumberGenerator
{
    public async Task<string> GenerateAsync(Guid propertyId, DateOnly businessDate, CancellationToken cancellationToken = default)
    {
        if (!dbContext.Database.IsSqlServer()) throw new InvalidOperationException("Reservation numbering requires SQL Server.");
        await dbContext.Database.OpenConnectionAsync(cancellationToken);
        try
        {
            await using var command = dbContext.Database.GetDbConnection().CreateCommand();
            command.CommandText = "SELECT NEXT VALUE FOR [fo].[TrinhTuSoDatPhong]";
            command.Transaction = dbContext.Database.CurrentTransaction?.GetDbTransaction();
            var value = Convert.ToInt64(await command.ExecuteScalarAsync(cancellationToken));
            return $"RSV-{businessDate:yyyyMM}-{value:D6}";
        }
        finally { await dbContext.Database.CloseConnectionAsync(); }
    }
}
