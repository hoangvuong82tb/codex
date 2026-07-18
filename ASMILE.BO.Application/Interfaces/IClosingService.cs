using ASMILE.BO.Application.DTOs;

namespace ASMILE.BO.Application.Interfaces;

public interface IClosingService
{
    Task<ClosingEntryDto> PreviewClosingAsync(string period, CancellationToken cancellationToken = default);
    Task<ClosingEntryDto> ClosePeriodAsync(string period, string? closedBy = null, string? notes = null, CancellationToken cancellationToken = default);
    Task<ClosingEntryDto> ReverseClosingAsync(Guid closingEntryId, string reason, string? reversedBy = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ClosingEntryDto>> GetClosingEntriesAsync(string? period = null, int take = 100, CancellationToken cancellationToken = default);
}
