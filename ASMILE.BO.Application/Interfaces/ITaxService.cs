using ASMILE.BO.Application.DTOs;
using ASMILE.BO.Core.Entities;

namespace ASMILE.BO.Application.Interfaces;

public interface ITaxService
{
    Task<IReadOnlyList<TaxCodeDto>> GetTaxCodesAsync(CancellationToken cancellationToken = default);
    Task<TaxCodeDto> CreateTaxCodeAsync(CreateTaxCodeRequest request, CancellationToken cancellationToken = default);
    Task<VatTransactionDto> CreateVatTransactionAsync(CreateVatTransactionRequest request, CancellationToken cancellationToken = default);
    Task SyncVatFromJournalAsync(Guid journalHeaderId, CancellationToken cancellationToken = default);
    Task SyncVatFromJournalAsync(JournalHeader journal, CancellationToken cancellationToken = default);
    Task<TaxDeclarationDraftDto> BuildVatDeclarationDraftAsync(string declarationPeriod, CancellationToken cancellationToken = default);
    Task<TaxDeclarationDraftDto> MarkDeclaredAsync(Guid declarationDraftId, string? declaredBy = null, CancellationToken cancellationToken = default);
    Task<TaxDeclarationDraftDto> ReopenDeclarationAsync(Guid declarationDraftId, string reason, string? reopenedBy = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TaxDeclarationDraftDto>> GetDeclarationsAsync(string? declarationPeriod = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<VatTransactionDto>> GetVatTransactionsAsync(string? declarationPeriod = null, string? direction = null, CancellationToken cancellationToken = default);
}
