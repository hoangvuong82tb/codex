using ASMILE.BO.Application.DTOs;
using ASMILE.BO.Core.Entities;

namespace ASMILE.BO.Application.Interfaces;

public interface IImportEngine
{
    Task<ImportPreviewResult> PreviewAsync(ImportPreviewRequest request, CancellationToken cancellationToken = default);
    Task<ImportPreviewResult> ValidateAsync(Guid importSessionId, CancellationToken cancellationToken = default);
    Task<ImportCommitResult> CommitAsync(Guid importSessionId, bool allowWarnings = false, CancellationToken cancellationToken = default);
    Task<ImportSession?> GetSessionAsync(Guid importSessionId, CancellationToken cancellationToken = default);
    Task<byte[]> ExportErrorsAsync(Guid importSessionId, CancellationToken cancellationToken = default);
}

public interface IImportTemplateHandler
{
    string TargetEntity { get; }
    IReadOnlyList<string> RequiredColumns { get; }
    IReadOnlyList<string> KeyColumns { get; }
    Task ValidateRowAsync(ImportRow row, IReadOnlyDictionary<string, string> values, IReadOnlyList<ImportRow> allRows, CancellationToken cancellationToken = default);
    Task<int> CommitAsync(ImportSession session, IReadOnlyList<ImportRow> rows, CancellationToken cancellationToken = default);
}
