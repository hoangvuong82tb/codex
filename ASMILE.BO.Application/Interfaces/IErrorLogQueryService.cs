namespace ASMILE.BO.Application.Interfaces;

public sealed record ErrorLogEntryDto(DateTime Timestamp, string Level, string Message);

public sealed record ErrorLogSnapshotDto(
    bool IsConfigured,
    bool IsAvailable,
    string Status,
    IReadOnlyList<ErrorLogEntryDto> Entries);

public interface IErrorLogQueryService
{
    Task<ErrorLogSnapshotDto> GetRecentErrorsAsync(int maxEntries = 100, CancellationToken cancellationToken = default);
}
