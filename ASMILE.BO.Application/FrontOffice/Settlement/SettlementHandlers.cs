using System.Linq;
using System.Text.Json;
using ASMILE.BO.Application.FrontOffice.Folio;
using ASMILE.BO.Application.FrontOffice.Foundation.Contexts;
using ASMILE.BO.Application.FrontOffice.Foundation.Persistence;
using ASMILE.BO.Application.FrontOffice.Payment;
using ASMILE.BO.Application.FrontOffice.Stay;
using ASMILE.BO.Application.Interfaces;
using ASMILE.BO.Core.FrontOffice.Folio;
using ASMILE.BO.Core.FrontOffice.Settlement;
using ASMILE.BO.Shared.Foundation;

namespace ASMILE.BO.Application.FrontOffice.Settlement;

public sealed class SettlementApplicationService(
    IHotelFolioRepository folioRepo,
    IHotelInvoiceRepository invoiceRepo,
    IHotelInvoiceNumberGenerator invoiceNumberGen,
    IHotelStayRepository stayRepo,
    IFrontOfficePersistenceSession persistence,
    IFrontOfficeExecutionContextAccessor ctx,
    IAuthorizationService auth,
    IClock clock)
{
    private static readonly Error PD = Error.Authorization("SETTLEMENT_PERMISSION_DENIED", "Settlement permission required.");
    private static readonly Error CR = Error.Authorization("SETTLEMENT_PERMISSION_DENIED", "Trusted FO property context required.");

    public async Task<Result<SettlementResultDto>> SettleAsync(SettleFolioCommand c, CancellationToken ct)
    {
        if (!auth.HasPermission("FrontOffice.Settlement.Settle")) return Result<SettlementResultDto>.Failure(PD);
        var cx = ctx.Current; if (cx is null) return Result<SettlementResultDto>.Failure(CR);
        if (c.FolioExpectedRowVersion is not { Length: 8 }) return Result<SettlementResultDto>.Failure(Error.Validation("FOLIO_ROWVERSION_REQUIRED", ""));
        var pid = cx.Property.PropertyId.Value;
        var folio = await folioRepo.GetAsync(pid, c.HotelFolioId, ct);
        if (folio is null) return Result<SettlementResultDto>.Failure(Error.Business("FOLIO_NOT_FOUND", ""));
        if (folio.Status != HotelFolioStatus.Open) return Result<SettlementResultDto>.Failure(Error.Business("FOLIO_NOT_OPEN", "Folio must be open to settle."));
        folioRepo.ApplyExpectedRowVersion(folio, c.FolioExpectedRowVersion);

        var now = clock.UtcNow;
        var running = folio.RunningBalance;
        var outstandingBefore = running;
        var discount = c.DiscountAmount > 0 ? c.DiscountAmount : 0m;
        var tax = c.TaxAmount > 0 ? c.TaxAmount : 0m;
        var projectedOutstanding = running - discount + tax;
        if (projectedOutstanding != 0 && !c.ManagerOverride)
            return Result<SettlementResultDto>.Failure(Error.Business("SETTLEMENT_OUTSTANDING_NOT_ZERO", $"Outstanding balance is {projectedOutstanding:N0}. Manager override required."));

        try
        {
            if (discount > 0) folio.AddDiscount(discount, "Settlement discount", now, cx.Actor.ActorId, cx.BusinessDate.BusinessDate);
            if (tax > 0) folio.AddTax(tax, "Settlement tax", now, cx.Actor.ActorId, cx.BusinessDate.BusinessDate);

            var outstandingAfter = folio.RunningBalance;

            var commit = await persistence.CommitAsync(ct);
            if (commit.IsFailure) return Result<SettlementResultDto>.Failure(commit.Error!);

            return Result<SettlementResultDto>.Success(new SettlementResultDto(true, outstandingAfter == 0 ? "Settlement complete. Folio is balanced." : $"Settlement completed with manager override. Outstanding: {outstandingAfter:N0}", null, outstandingBefore, outstandingAfter));
        }
        catch (InvalidOperationException ex) { return Result<SettlementResultDto>.Failure(Error.Business("SETTLEMENT_FAILED", ex.Message)); }
    }

    public async Task<Result<HotelFolioDto>> CloseFolioAsync(CloseFolioCommand c, CancellationToken ct)
    {
        if (!auth.HasPermission("FrontOffice.Settlement.Close")) return Result<HotelFolioDto>.Failure(PD);
        var cx = ctx.Current; if (cx is null) return Result<HotelFolioDto>.Failure(CR);
        if (c.FolioExpectedRowVersion is not { Length: 8 }) return Result<HotelFolioDto>.Failure(Error.Validation("FOLIO_ROWVERSION_REQUIRED", ""));
        var pid = cx.Property.PropertyId.Value;
        var folio = await folioRepo.GetAsync(pid, c.HotelFolioId, ct);
        if (folio is null) return Result<HotelFolioDto>.Failure(Error.Business("FOLIO_NOT_FOUND", ""));
        if (folio.Status != HotelFolioStatus.Open) return Result<HotelFolioDto>.Failure(Error.Business("FOLIO_NOT_OPEN", "Folio must be open to close."));
        folioRepo.ApplyExpectedRowVersion(folio, c.FolioExpectedRowVersion);
        try
        {
            var now = clock.UtcNow;
            folio.Close(now, cx.Actor.ActorId);
            var commit = await persistence.CommitAsync(ct);
            if (commit.IsFailure) return Result<HotelFolioDto>.Failure(commit.Error!);
            return await folioRepo.GetDtoAsync(pid, folio.HotelFolioId, ct) is { } dto ? Result<HotelFolioDto>.Success(dto) : Result<HotelFolioDto>.Failure(Error.System("FOLIO_READ_FAILED", ""));
        }
        catch (InvalidOperationException ex) { return Result<HotelFolioDto>.Failure(Error.Business("FOLIO_CLOSE_FAILED", ex.Message)); }
    }

    public async Task<Result<HotelInvoiceDto>> GenerateInvoiceAsync(GenerateInvoiceCommand c, CancellationToken ct)
    {
        if (!auth.HasPermission("FrontOffice.Settlement.Invoice")) return Result<HotelInvoiceDto>.Failure(PD);
        var cx = ctx.Current; if (cx is null) return Result<HotelInvoiceDto>.Failure(CR);
        var pid = cx.Property.PropertyId.Value;
        var ledger = await folioRepo.GetLedgerAsync(pid, c.HotelFolioId, ct);
        if (ledger is null) return Result<HotelInvoiceDto>.Failure(Error.Business("FOLIO_NOT_FOUND", ""));
        if (c.FolioExpectedRowVersion is not { Length: 8 } || ledger.RowVersion is not { Length: 8 } || !ledger.RowVersion.SequenceEqual(c.FolioExpectedRowVersion))
            return Result<HotelInvoiceDto>.Failure(Error.Concurrency("INVOICE_CONCURRENCY_CONFLICT", "Folio đã thay đổi, vui lòng tải lại trước khi phát hành hóa đơn."));
        var existing = await invoiceRepo.GetByFolioAsync(pid, c.HotelFolioId, ct);
        if (existing is not null) return Result<HotelInvoiceDto>.Failure(Error.Business("INVOICE_ALREADY_EXISTS", $"Folio đã có hóa đơn {existing.InvoiceNumber}."));
        var stay = await stayRepo.GetDtoAsync(pid, ledger.HotelStayId, ct);
        var now = clock.UtcNow;
        var number = await invoiceNumberGen.GenerateAsync(pid, cx.BusinessDate.BusinessDate, ct);

        var entriesById = ledger.Entries.ToDictionary(e => e.HotelFolioEntryId);
        var voidedFinancialReversalIds = ledger.Entries
            .Where(e => e.EntryType == HotelFolioEntryType.Correction
                && e.ReversesEntryId is Guid reversedEntryId
                && entriesById.TryGetValue(reversedEntryId, out var reversedEntry)
                && (reversedEntry.EntryType is HotelFolioEntryType.Payment
                    or HotelFolioEntryType.Deposit
                    or HotelFolioEntryType.Refund))
            .Select(e => e.HotelFolioEntryId)
            .ToHashSet();
        var subtotal = ledger.Entries
            .Where(e => IsInvoiceSubtotalEntry(e.EntryType)
                && !voidedFinancialReversalIds.Contains(e.HotelFolioEntryId))
            .Sum(e => e.Amount);
        var payments = Math.Abs(ledger.Entries
            .Where(e => e.EntryType == HotelFolioEntryType.Payment && e.ReversedByEntryId is null)
            .Sum(e => e.Amount));
        var deposits = Math.Abs(ledger.Entries
            .Where(e => e.EntryType == HotelFolioEntryType.Deposit && e.ReversedByEntryId is null)
            .Sum(e => e.Amount));
        var refunds = ledger.Entries
            .Where(e => e.EntryType == HotelFolioEntryType.Refund && e.ReversedByEntryId is null)
            .Sum(e => e.Amount);
        var tax = ledger.Entries.Where(e => e.EntryType == HotelFolioEntryType.Tax).Sum(e => e.Amount);
        var total = subtotal + tax;
        var paid = payments + deposits - refunds;
        var outstanding = ledger.RunningBalance;

        var data = JsonSerializer.Serialize(new { subtotal, tax, total, paid, outstanding, entries = ledger.Entries.Select(e => new { e.SequenceNumber, e.EntryType, e.Amount, e.Description }) });
        var invoice = HotelInvoice.Generate(pid, ledger.HotelFolioId, ledger.HotelStayId, number, subtotal, tax, total, paid, outstanding, stay?.GuestName ?? ledger.GuestName, stay?.RoomNumber ?? ledger.RoomNumber, $"{ledger.GuestName} • {ledger.RoomNumber} • {ledger.StayNumber}", data, now, cx.Actor.ActorId);

        await invoiceRepo.AddAsync(invoice, ct);
        var commit = await persistence.CommitAsync(ct);
        if (commit.IsFailure) return Result<HotelInvoiceDto>.Failure(commit.Error!);
        return Result<HotelInvoiceDto>.Success(MapInvoice(invoice));
    }

    private static bool IsInvoiceSubtotalEntry(HotelFolioEntryType entryType) => entryType is
        HotelFolioEntryType.RoomCharge or HotelFolioEntryType.Restaurant or HotelFolioEntryType.Laundry or
        HotelFolioEntryType.MiniBar or HotelFolioEntryType.Spa or HotelFolioEntryType.Telephone or
        HotelFolioEntryType.OtherCharge or HotelFolioEntryType.ServiceCharge or HotelFolioEntryType.Discount or
        HotelFolioEntryType.Package or HotelFolioEntryType.Adjustment or HotelFolioEntryType.Correction or
        HotelFolioEntryType.Transfer or HotelFolioEntryType.ManualCharge or HotelFolioEntryType.ManualCredit or
        HotelFolioEntryType.OpeningBalance;

    public async Task<Result<HotelInvoiceDto>> PrintAsync(PrintInvoiceCommand c, CancellationToken ct)
    {
        var cx = ctx.Current; if (cx is null) return Result<HotelInvoiceDto>.Failure(CR);
        var pid = cx.Property.PropertyId.Value;
        var invoice = await invoiceRepo.GetAsync(pid, c.HotelInvoiceId, ct);
        if (invoice is null) return Result<HotelInvoiceDto>.Failure(Error.Business("INVOICE_NOT_FOUND", ""));
        invoice.MarkPrinted(clock.UtcNow);
        await persistence.CommitAsync(ct);
        return Result<HotelInvoiceDto>.Success(MapInvoice(invoice));
    }

    private static HotelInvoiceDto MapInvoice(HotelInvoice i) => new(i.HotelInvoiceId, i.InvoiceNumber, i.HotelFolioId, i.HotelStayId, i.Status, i.Subtotal, i.TaxAmount, i.TotalAmount, i.PaidAmount, i.OutstandingAmount, i.GuestName, i.RoomNumber, i.StayDates, i.CreatedAtUtc, i.PrintedAtUtc, i.RowVersion);
}
