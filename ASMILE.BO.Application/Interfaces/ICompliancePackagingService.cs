using ASMILE.BO.Application.DTOs;

namespace ASMILE.BO.Application.Interfaces;

public interface ICompliancePackagingService
{
    Task<CompliancePackageDto> CreatePackageAsync(string standardCode, string periodCode, CancellationToken cancellationToken = default);
    Task<CompliancePackageDto> MarkPackageCompletedAsync(Guid packageId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CompliancePackageDto>> GetPackagesAsync(int take = 100, CancellationToken cancellationToken = default);
}
