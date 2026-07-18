using ASMILE.BO.Application.DTOs;
using ASMILE.BO.Core.Entities;

namespace ASMILE.BO.Application.Interfaces;

public interface IPostingEngine
{
    Task<PostingPreviewResult> PreviewPostingAsync(string sourceType, Guid sourceId, CancellationToken cancellationToken = default);
    Task<PostingResult> PostAsync(string sourceType, Guid sourceId, string? postedBy = null, CancellationToken cancellationToken = default);
    Task<PostingResult> ReverseAsync(Guid journalId, string reason, string? reversedBy = null, CancellationToken cancellationToken = default);
    Task<JournalValidationResult> ValidateJournalAsync(JournalHeader journal, CancellationToken cancellationToken = default);
}
