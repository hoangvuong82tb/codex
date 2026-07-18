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
            connectionString = "Server=(localdb)\\MSSQLLocalDB;Database=ASMILE_BO_DesignTime;Integrated Security=True;TrustServerCertificate=True;Connect Timeout=1;";
        }

        builder.UseSqlServer(connectionString);
        return new AppDbContext(builder.Options);
    }
}
