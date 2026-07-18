using ASMILE.BO.Application.DTOs;

namespace ASMILE.BO.Application.Interfaces;

public interface ITT200ComplianceService
{
    Task<IReadOnlyList<TT200AccountMappingDto>> GetAccountMappingsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TT200ComplianceIssueDto>> ValidateComplianceAsync(string? periodCode = null, CancellationToken cancellationToken = default);
    Task<TT200ComplianceSnapshotDto> CreateSnapshotAsync(string periodCode, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TT200ComplianceSnapshotDto>> GetSnapshotsAsync(int take = 50, CancellationToken cancellationToken = default);
    Task<TT200ExportManifestDto> CreateExportManifestAsync(string exportType, string periodCode, int rowCount, string payloadHash, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TT200ExportManifestDto>> GetExportManifestsAsync(int take = 50, CancellationToken cancellationToken = default);
    Task<TT200ValidationSummaryDto> GetValidationSummaryAsync(string? periodCode = null, CancellationToken cancellationToken = default);
}
