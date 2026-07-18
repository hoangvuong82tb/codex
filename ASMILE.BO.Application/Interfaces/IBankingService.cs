using ASMILE.BO.Application.DTOs;

namespace ASMILE.BO.Application.Interfaces;

public interface IBankingService
{
    Task<IReadOnlyList<BankAccountDto>> GetAccountsAsync(CancellationToken cancellationToken = default);
    Task<BankAccountDto> CreateAccountAsync(BankAccountRequest request, CancellationToken cancellationToken = default);
    Task<BankTransactionDto> CreateReceiptAsync(BankTransactionRequest request, CancellationToken cancellationToken = default);
    Task<BankTransactionDto> CreatePaymentAsync(BankTransactionRequest request, CancellationToken cancellationToken = default);
    Task<BankTransactionDto> UpdateTransactionAsync(Guid transactionId, BankTransactionRequest request, CancellationToken cancellationToken = default);
    Task DeleteTransactionAsync(Guid transactionId, string? deletedBy = null, CancellationToken cancellationToken = default);
    Task<BankTransactionDto> ApproveTransactionAsync(Guid transactionId, string? approvedBy = null, CancellationToken cancellationToken = default);
    Task<BankTransactionDto> CancelApprovalAsync(Guid transactionId, string? cancelledBy = null, CancellationToken cancellationToken = default);
    Task<BankTransactionDto> PostTransactionAsync(Guid transactionId, string? postedBy = null, CancellationToken cancellationToken = default);
    Task<BankTransactionDto> ReverseTransactionAsync(Guid transactionId, string reason, string? reversedBy = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<BankTransactionDto>> GetTransactionsAsync(Guid? bankAccountId, DateTime? fromDate, DateTime? toDate, string? keyword = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<BankBookEntryDto>> GetBankBookAsync(Guid? bankAccountId, DateTime fromDate, DateTime toDate, CancellationToken cancellationToken = default);
    Task<BankSummaryDto> GetSummaryAsync(Guid? bankAccountId, DateTime? fromDate, DateTime? toDate, CancellationToken cancellationToken = default);
    Task<BankStatementDto> ImportStatementAsync(Guid bankAccountId, string csvFilePath, string? importedBy = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<BankStatementDto>> GetStatementsAsync(Guid? bankAccountId, int take = 50, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<BankReconciliationDto>> GetReconciliationsAsync(Guid? bankAccountId, int take = 50, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<BankReconciliationLineDto>> GetReconciliationLinesAsync(Guid reconciliationId, CancellationToken cancellationToken = default);
    Task<BankReconciliationDto> ReconcileAsync(Guid bankAccountId, Guid bankStatementId, DateTime reconciliationDate, string? createdBy = null, CancellationToken cancellationToken = default);
    Task<BankReconciliationDto> ConfirmReconciliationAsync(Guid reconciliationId, string? confirmedBy = null, CancellationToken cancellationToken = default);
}
