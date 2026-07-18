using ASMILE.BO.Application.DTOs;

namespace ASMILE.BO.Application.Interfaces;

public interface IFinancialStatementMappingService
{
    Task<IReadOnlyList<FinancialTemplateDto>> GetTemplatesAsync(string? standardCode = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<FinancialTemplateVersionDto>> GetTemplateVersionsAsync(Guid templateId, CancellationToken cancellationToken = default);
    Task<FinancialTemplateVersionDto> ActivateVersionAsync(Guid versionId, CancellationToken cancellationToken = default);
    Task<FinancialStatementSnapshotDto> CaptureSnapshotAsync(Guid templateVersionId, string periodCode, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<FinancialStatementSnapshotDto>> GetSnapshotsAsync(Guid templateVersionId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<string>> ValidateMappingAsync(Guid templateVersionId, CancellationToken cancellationToken = default);
}
