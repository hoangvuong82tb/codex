using ASMILE.BO.Application.DTOs;

namespace ASMILE.BO.Application.Interfaces;

public interface ICashFundService
{
    Task<IReadOnlyList<CashFundDto>> GetFundsAsync(CancellationToken cancellationToken = default);
    Task<CashFundDto> CreateFundAsync(CashFundRequest request, CancellationToken cancellationToken = default);
    Task<CashFundDto> UpdateFundAsync(Guid fundId, CashFundRequest request, CancellationToken cancellationToken = default);
    Task<CashSummaryDto> GetSummaryAsync(Guid? fundId, DateTime? fromDate, DateTime? toDate, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CashTransactionDto>> GetTransactionsAsync(Guid? fundId, DateTime? fromDate, DateTime? toDate, string? keyword = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CashBookEntryDto>> GetCashBookAsync(Guid? fundId, DateTime fromDate, DateTime toDate, CancellationToken cancellationToken = default);
    Task<CashTransactionDto> CreateReceiptAsync(CashTransactionRequest request, CancellationToken cancellationToken = default);
    Task<CashTransactionDto> CreatePaymentAsync(CashTransactionRequest request, CancellationToken cancellationToken = default);
    Task<CashTransactionDto> UpdateTransactionAsync(Guid transactionId, CashTransactionRequest request, CancellationToken cancellationToken = default);
    Task DeleteTransactionAsync(Guid transactionId, string? deletedBy = null, CancellationToken cancellationToken = default);
    Task<CashTransactionDto> ApproveTransactionAsync(Guid transactionId, string? approvedBy = null, CancellationToken cancellationToken = default);
    Task<CashTransactionDto> CancelApprovalAsync(Guid transactionId, string? cancelledBy = null, CancellationToken cancellationToken = default);
    Task<CashTransactionDto> PostTransactionAsync(Guid transactionId, string? postedBy = null, CancellationToken cancellationToken = default);
    Task<CashTransactionDto> ReverseTransactionAsync(Guid transactionId, string reason, string? reversedBy = null, CancellationToken cancellationToken = default);
    Task<CashCountDto> CreateCashCountAsync(CashCountRequest request, CancellationToken cancellationToken = default);
    Task<CashCountDto> ConfirmCashCountAsync(Guid cashCountId, string? confirmedBy = null, string? counterAccountCode = null, CancellationToken cancellationToken = default);
}
