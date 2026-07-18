using ASMILE.BO.Core.Entities;

namespace ASMILE.BO.Application.Interfaces;

public interface IDebtTransactionService
{
    Task SyncFromJournalAsync(Guid journalHeaderId, CancellationToken cancellationToken = default);
    Task SyncFromJournalAsync(JournalHeader journal, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ReceivableTransaction>> GetReceivablesAsync(Guid? customerId = null, DateTime? fromDate = null, DateTime? toDate = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PayableTransaction>> GetPayablesAsync(Guid? supplierId = null, DateTime? fromDate = null, DateTime? toDate = null, CancellationToken cancellationToken = default);
    Task<PaymentAllocation> AllocatePaymentAsync(string paymentSourceType, Guid? paymentSourceId, Guid? paymentJournalHeaderId, string targetType, Guid targetTransactionId, decimal allocatedAmount, DateTime allocationDate, string? createdBy = null, CancellationToken cancellationToken = default);
    Task RecalculateBalancesAsync(string targetType, Guid? partnerId = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AgingSnapshot>> BuildAgingSnapshotAsync(DateTime snapshotDate, CancellationToken cancellationToken = default);
}
