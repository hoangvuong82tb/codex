using ASMILE.BO.Application.DTOs;

namespace ASMILE.BO.Application.Interfaces;

public interface ITT99ComplianceService
{
    Task<IReadOnlyList<TT99AccountMappingDto>> GetAccountMappingsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TT99ComplianceIssueDto>> ValidateComplianceAsync(string? periodCode = null, CancellationToken cancellationToken = default);
    Task<TT99ComplianceSnapshotDto> CreateSnapshotAsync(string periodCode, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TT99ComplianceSnapshotDto>> GetSnapshotsAsync(int take = 50, CancellationToken cancellationToken = default);
    Task<TT99ExportManifestDto> CreateExportManifestAsync(string exportType, string periodCode, int rowCount, string payloadHash, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TT99ExportManifestDto>> GetExportManifestsAsync(int take = 50, CancellationToken cancellationToken = default);
    Task<TT99ValidationSummaryDto> GetValidationSummaryAsync(string? periodCode = null, CancellationToken cancellationToken = default);
}
