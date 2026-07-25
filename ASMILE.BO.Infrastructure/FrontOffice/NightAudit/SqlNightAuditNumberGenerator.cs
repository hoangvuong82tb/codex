using ASMILE.BO.Application.FrontOffice.NightAudit;
using ASMILE.BO.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace ASMILE.BO.Infrastructure.FrontOffice.NightAudit;

public sealed class SqlNightAuditNumberGenerator(AppDbContext dbContext) : INightAuditNumberGenerator
{
    public async Task<string> GenerateAsync(Guid propertyId, DateOnly businessDate, CancellationToken ct = default)
    {
        if (!dbContext.Database.IsSqlServer()) throw new InvalidOperationException("Night audit numbering requires SQL Server.");
        await dbContext.Database.OpenConnectionAsync(ct);
        try { await using var cmd = dbContext.Database.GetDbConnection().CreateCommand(); cmd.CommandText = "SELECT NEXT VALUE FOR [fo].[TrinhTuSoKiemDem]"; cmd.Transaction = dbContext.Database.CurrentTransaction?.GetDbTransaction(); return $"NA-{businessDate:yyyyMM}-{Convert.ToInt64(await cmd.ExecuteScalarAsync(ct)):D6}"; }
        finally { await dbContext.Database.CloseConnectionAsync(); }
    }
}
