using System.Security.Cryptography;
using System.Text;
using ASMILE.BO.Application.DTOs;
using ASMILE.BO.Application.Interfaces;
using ASMILE.BO.Core.Entities;
using ASMILE.BO.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ASMILE.BO.Infrastructure.Services;

public sealed class CompliancePackagingService : ICompliancePackagingService
{
    private readonly AppDbContext _db;
    private readonly ICurrentCompanyContext _companyContext;

    public CompliancePackagingService(AppDbContext db, ICurrentCompanyContext companyContext)
    {
        _db = db;
        _companyContext = companyContext;
    }

    public async Task<CompliancePackageDto> CreatePackageAsync(string standardCode, string periodCode, CancellationToken cancellationToken = default)
    {
        var package = new CompliancePackage
        {
            CompanyId = _companyContext.CompanyId,
            BranchId = _companyContext.BranchId,
            PackageCode = $"{standardCode}-{periodCode}-{DateTime.UtcNow:yyyyMMddHHmmss}",
            StandardCode = standardCode,
            PeriodCode = periodCode,
            Status = "Generating",
            CreatedAtUtc = DateTime.UtcNow
        };
        _db.CompliancePackages.Add(package);
        await _db.SaveChangesAsync(cancellationToken);
        return ToDto(package);
    }

    public async Task<CompliancePackageDto> MarkPackageCompletedAsync(Guid packageId, CancellationToken cancellationToken = default)
    {
        var package = await _db.CompliancePackages.FirstOrDefaultAsync(x => x.Id == packageId, cancellationToken)
            ?? throw new KeyNotFoundException("Compliance package not found.");
        package.Status = "Completed";
        package.CompletedAtUtc = DateTime.UtcNow;
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(package.PackageCode)));
        _db.ComplianceManifests.Add(new ComplianceManifest
        {
            CompanyId = package.CompanyId,
            BranchId = package.BranchId,
            CompliancePackageId = package.Id,
            ManifestType = "XML/PDF",
            FilePath = $"compliance/{package.PackageCode}.zip",
            Hash = hash,
            RowCount = 1
        });
        _db.ComplianceArchives.Add(new ComplianceArchive
        {
            CompanyId = package.CompanyId,
            BranchId = package.BranchId,
            CompliancePackageId = package.Id,
            ArchivePath = $"archive/{package.PackageCode}.zip",
            Hash = hash,
            IsVerified = true
        });
        await _db.SaveChangesAsync(cancellationToken);
        return ToDto(package);
    }

    public async Task<IReadOnlyList<CompliancePackageDto>> GetPackagesAsync(int take = 100, CancellationToken cancellationToken = default)
    {
        var normalizedTake = Math.Clamp(take, 1, 300);
        return await _db.CompliancePackages.AsNoTracking()
            .OrderByDescending(x => x.CreatedAtUtc)
            .Take(normalizedTake)
            .Select(x => new CompliancePackageDto
            {
                Id = x.Id,
                PackageCode = x.PackageCode,
                StandardCode = x.StandardCode,
                PeriodCode = x.PeriodCode,
                Status = x.Status
            })
            .ToListAsync(cancellationToken);
    }

    private static CompliancePackageDto ToDto(CompliancePackage package)
        => new()
        {
            Id = package.Id,
            PackageCode = package.PackageCode,
            StandardCode = package.StandardCode,
            PeriodCode = package.PeriodCode,
            Status = package.Status
        };
}
