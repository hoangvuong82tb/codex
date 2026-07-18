using ASMILE.BO.Application.DTOs;

namespace ASMILE.BO.Application.Interfaces;

public interface IGeneralLedgerService
{
    Task<IReadOnlyList<LedgerAccountDto>> GetAccountsAsync(CancellationToken cancellationToken = default);
    Task SaveAccountsAsync(IReadOnlyList<LedgerAccountDto> accounts, CancellationToken cancellationToken = default);
    Task DeleteAccountAsync(string accountCode, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<LedgerEntryDto>> GetLedgerEntriesAsync(LedgerFilterDto filter, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<LedgerBalanceDto>> GetLedgerBalancesAsync(LedgerFilterDto filter, CancellationToken cancellationToken = default);
    Task CreateManualEntryAsync(CreateLedgerEntryRequest request, CancellationToken cancellationToken = default);
    Task PostFromDocumentAsync(LedgerDocumentType documentType, string documentId, CancellationToken cancellationToken = default);
    Task PrintLedgerAsync(string accountCode, DateTime? fromDate, DateTime? toDate, CancellationToken cancellationToken = default);
    Task ExportLedgerAsync(LedgerFilterDto filter, CancellationToken cancellationToken = default);
}
