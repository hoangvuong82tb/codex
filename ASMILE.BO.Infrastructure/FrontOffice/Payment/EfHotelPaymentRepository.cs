using ASMILE.BO.Application.FrontOffice.Payment;
using ASMILE.BO.Core.FrontOffice.Folio;
using ASMILE.BO.Core.FrontOffice.Payment;
using ASMILE.BO.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ASMILE.BO.Infrastructure.FrontOffice.Payment;

public sealed class EfHotelPaymentRepository(AppDbContext db) : IHotelPaymentRepository
{
    public async Task<HotelPaymentDto?> GetDtoAsync(Guid propertyId, Guid paymentId, CancellationToken ct)
    {
        var p = await db.Set<HotelPayment>().AsNoTracking().FirstOrDefaultAsync(x => x.PropertyId == propertyId && x.HotelPaymentId == paymentId, ct);
        if (p is null) return null;
        var alloc = await db.Set<HotelPaymentAllocation>().Where(x => x.HotelPaymentId == paymentId).SumAsync(x => x.Amount, ct);
        return Map(p, alloc);
    }
    public async Task<IReadOnlyList<HotelPaymentDto>> GetForFolioAsync(Guid propertyId, Guid folioId, CancellationToken ct)
    {
        var payments = await db.Set<HotelPayment>().AsNoTracking().Where(x => x.PropertyId == propertyId && x.HotelFolioId == folioId).OrderByDescending(x => x.CreatedAtUtc).ToListAsync(ct);
        var result = new List<HotelPaymentDto>();
        foreach (var p in payments) { var alloc = await db.Set<HotelPaymentAllocation>().Where(x => x.HotelPaymentId == p.HotelPaymentId).SumAsync(x => x.Amount, ct); result.Add(Map(p, alloc)); }
        return result;
    }
    public async Task<IReadOnlyList<PaymentAllocationDto>> GetAllocationsAsync(Guid propertyId, Guid paymentId, CancellationToken ct) => await db.Set<HotelPaymentAllocation>().AsNoTracking().Where(x => x.PropertyId == propertyId && x.HotelPaymentId == paymentId).OrderBy(x => x.CreatedAtUtc).Select(x => new PaymentAllocationDto(x.HotelPaymentAllocationId, x.HotelPaymentId, x.ChargeFolioEntryId, x.ChargeDescription, x.Amount, x.CreatedAtUtc)).ToListAsync(ct);
    public async Task<OutstandingBalanceDto> GetOutstandingAsync(Guid propertyId, Guid folioId, CancellationToken ct)
    {
        var folioExists = await db.HotelFolios.AsNoTracking().AnyAsync(x => x.PropertyId == propertyId && x.HotelFolioId == folioId, ct);
        if (!folioExists) return new OutstandingBalanceDto(0, 0, 0, 0);
        // Folio entries use signed ledger amounts: debits are positive and payments/deposits are credits.
        // Keep every non-payment/deposit entry in the charge bucket so F&B, minibar, services,
        // refunds and manual adjustments are included in the real outstanding balance.
        var charges = await db.Set<HotelFolioEntry>()
            .Where(e => e.HotelFolioId == folioId
                && e.EntryType != HotelFolioEntryType.Payment
                && e.EntryType != HotelFolioEntryType.Deposit
                && !e.ReversedByEntryId.HasValue)
            .SumAsync(e => e.Amount, ct);
        var payments = await db.Set<HotelFolioEntry>().Where(e => e.HotelFolioId == folioId && e.EntryType == HotelFolioEntryType.Payment && !e.ReversedByEntryId.HasValue).SumAsync(e => e.Amount, ct);
        var deposits = await db.Set<HotelFolioEntry>().Where(e => e.HotelFolioId == folioId && (e.EntryType == HotelFolioEntryType.Deposit) && !e.ReversedByEntryId.HasValue).SumAsync(e => e.Amount, ct);
        return new OutstandingBalanceDto(charges, Math.Abs(payments), Math.Abs(deposits), charges + payments + deposits);
    }
    public async Task<IReadOnlyList<PaymentHistoryDto>> GetHistoryAsync(Guid propertyId, Guid paymentId, CancellationToken ct) =>
        await db.Set<PaymentHistory>().AsNoTracking()
            .Where(x => x.PropertyId == propertyId && x.HotelPaymentId == paymentId)
            .OrderByDescending(x => x.OccurredAtUtc)
            .Select(x => new PaymentHistoryDto(x.OccurredAtUtc, x.Action, x.Detail))
            .ToListAsync(ct);
    public async Task<DepositSummaryDto> GetDepositSummaryAsync(Guid propertyId, Guid folioId, CancellationToken ct)
    {
        var folioExists = await db.HotelFolios.AsNoTracking().AnyAsync(x => x.PropertyId == propertyId && x.HotelFolioId == folioId, ct);
        if (!folioExists) return new DepositSummaryDto(0, 0, 0);
        var total = await db.Set<HotelFolioEntry>().Where(e => e.HotelFolioId == folioId && e.EntryType == HotelFolioEntryType.Deposit && !e.ReversedByEntryId.HasValue).SumAsync(e => e.Amount, ct);
        return new DepositSummaryDto(Math.Abs(total), 0, Math.Abs(total));
    }
    public async Task<IReadOnlyList<SettlementSummaryDto>> GetSettlementSummaryAsync(Guid propertyId, DateOnly? from, DateOnly? to, CancellationToken ct)
    {
        var query = db.HotelFolios.AsNoTracking().Where(x => x.PropertyId == propertyId);
        if (from.HasValue) query = query.Where(x => x.CreatedAtUtc >= from.Value.ToDateTime(TimeOnly.MinValue));
        if (to.HasValue) query = query.Where(x => x.CreatedAtUtc <= to.Value.ToDateTime(TimeOnly.MaxValue));
        var folios = await query.OrderByDescending(x => x.CreatedAtUtc).Take(200).ToListAsync(ct);
        var result = new List<SettlementSummaryDto>();
        foreach (var f in folios)
        {
            var charges = await db.HotelFolioEntries
                .Where(e => e.HotelFolioId == f.HotelFolioId
                    && e.EntryType != HotelFolioEntryType.Payment
                    && e.EntryType != HotelFolioEntryType.Deposit
                    && !e.ReversedByEntryId.HasValue)
                .SumAsync(e => e.Amount, ct);
            var payments = await db.HotelFolioEntries.Where(e => e.HotelFolioId == f.HotelFolioId && e.EntryType == HotelFolioEntryType.Payment && !e.ReversedByEntryId.HasValue).SumAsync(e => e.Amount, ct);
            var deposits = await db.HotelFolioEntries.Where(e => e.HotelFolioId == f.HotelFolioId && e.EntryType == HotelFolioEntryType.Deposit && !e.ReversedByEntryId.HasValue).SumAsync(e => e.Amount, ct);
            var outstanding = charges + payments + deposits;
            var stay = await db.HotelStays.AsNoTracking().FirstOrDefaultAsync(s => s.HotelStayId == f.HotelStayId, ct);
            result.Add(new SettlementSummaryDto(f.HotelFolioId, f.FolioNumber, stay?.GuestNameSnapshot ?? "N/A", stay?.RoomNumberSnapshot ?? "N/A", charges, Math.Abs(payments), Math.Abs(deposits), outstanding, outstanding == 0));
        }
        return result;
    }
    public async Task<HotelPayment?> GetAsync(Guid propertyId, Guid paymentId, CancellationToken ct) => await db.Set<HotelPayment>().Include(x => x.Allocations).FirstOrDefaultAsync(x => x.PropertyId == propertyId && x.HotelPaymentId == paymentId, ct);
    // The application service stages payment, folio, audit and outbox records on
    // the same DbContext. The persistence session owns the transaction and the
    // single SaveChangesAsync call; saving here could commit a partial payment
    // before the related folio posting is known to be valid.
    public Task AddAsync(HotelPayment payment, CancellationToken ct)
    {
        db.Set<HotelPayment>().Add(payment);
        return Task.CompletedTask;
    }

    public Task AddAllocationAsync(HotelPaymentAllocation allocation, CancellationToken ct)
    {
        db.Set<HotelPaymentAllocation>().Add(allocation);
        return Task.CompletedTask;
    }
    public void ApplyExpectedRowVersion(HotelPayment payment, byte[] expectedRowVersion) => db.Entry(payment).Property(nameof(HotelPayment.RowVersion)).OriginalValue = expectedRowVersion;
    private static HotelPaymentDto Map(HotelPayment p, decimal alloc) => new(p.HotelPaymentId, p.PaymentNumber, p.HotelFolioId, p.PaymentMethod, p.Amount, p.RefundedAmount, alloc, p.Status, p.Currency, p.AuthorizationCode, p.ExternalReference, p.Notes, p.CreatedAtUtc, p.VoidedAtUtc, p.VoidReason, p.RefundedAtUtc, p.RefundReason, p.RowVersion, p.FolioEntryId);
}
