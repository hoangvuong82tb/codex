using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ASMILE.BO.Infrastructure.Persistence;

public sealed class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var builder = new DbContextOptionsBuilder<AppDbContext>();
        var connectionString = Environment.GetEnvironmentVariable("ASMILE_BO_DESIGNTIME_CONNECTION");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Design-time database connection is not configured. Set ASMILE_BO_DESIGNTIME_CONNECTION explicitly before running EF commands; refusing to target an implicit database.");
        }

        builder.UseSqlServer(connectionString);
        return new AppDbContext(builder.Options);
    }
}
