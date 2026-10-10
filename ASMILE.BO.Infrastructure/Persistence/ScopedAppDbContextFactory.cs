using ASMILE.BO.Application.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ASMILE.BO.Infrastructure.Persistence;

/// <summary>
/// Creates an isolated context from the current scope's options.
/// The default EF factory is singleton-oriented, while this application keeps
/// DbContextOptions scoped; this adapter preserves that lifetime contract.
/// </summary>
public sealed class ScopedAppDbContextFactory(
    DbContextOptions<AppDbContext> options,
    ICurrentCompanyContext companyContext)
    : IDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext() => new(options, companyContext);

    public Task<AppDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(new AppDbContext(options, companyContext));
    }
}
