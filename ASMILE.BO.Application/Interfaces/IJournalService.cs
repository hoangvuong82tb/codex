using ASMILE.BO.Application.DTOs;
using ASMILE.BO.Core.Entities;

namespace ASMILE.BO.Application.Interfaces;

public interface IJournalService
{
    Task<JournalHeader?> GetJournalAsync(Guid journalId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<JournalHeader>> GetJournalsBySourceAsync(string sourceType, Guid sourceId, CancellationToken cancellationToken = default);
    Task<JournalHeader> CreateDraftAsync(JournalHeader journal, CancellationToken cancellationToken = default);
    Task<JournalHeader> PostDraftAsync(Guid journalId, string? postedBy = null, CancellationToken cancellationToken = default);
    Task<JournalHeader> ReverseAsync(Guid journalId, string reason, string? reversedBy = null, CancellationToken cancellationToken = default);
    Task<JournalValidationResult> ValidateAsync(JournalHeader journal, CancellationToken cancellationToken = default);
}
