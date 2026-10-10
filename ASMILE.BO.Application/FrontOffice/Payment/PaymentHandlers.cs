using ASMILE.BO.Application.FrontOffice.Folio;
using ASMILE.BO.Application.FrontOffice.Foundation.Contexts;
using ASMILE.BO.Application.FrontOffice.Foundation.Persistence;
using ASMILE.BO.Application.Interfaces;
using ASMILE.BO.Core.FrontOffice.Foundation.Identifiers;
using ASMILE.BO.Core.FrontOffice.Payment;
using ASMILE.BO.Shared.Foundation;

namespace ASMILE.BO.Application.FrontOffice.Payment;

public sealed class HotelPaymentApplicationService(
    IHotelPaymentRepository paymentRepo,
    IHotelFolioRepository folioRepo,
    IHotelPaymentNumberGenerator numberGenerator,
    IFrontOfficePersistenceSession persistence,
    IFrontOfficeExecutionContextAccessor ctx,
    IAuthorizationService auth,
    IClock clock)
{
    private static Error Denied(string a) => Error.Authorization("PAYMENT_PERMISSION_DENIED", $"{a} permission required.");
    private Error? Context(out FrontOfficeExecutionContext cx, out Guid pid) { cx = ctx.Current!; pid = cx?.Property.PropertyId.Value ?? Guid.Empty; return cx is null ? Error.Authorization("PAYMENT_PERMISSION_DENIED", "FO context required.") : null; }
    static Result<T> F<T>(Error? e) => Result<T>.Failure(e!);
    static bool ValidToken(byte[]? t, out Error? e) { if (t is not { Length: 8 }) { e = Error.Validation("ROWVERSION_REQUIRED", ""); return false; } e = null; return true; }

    // =============================================================================
    // RECEIVE PAYMENT (Payment Engine → Folio Posting)
    // =============================================================================
    public async Task<Result<HotelPaymentDto>> ReceiveAsync(ReceivePaymentCommand c, CancellationToken ct)
    {
        if (!auth.HasPermission("FrontOffice.Payment.Receive")) return F<HotelPaymentDto>(Denied("Receive"));
        if (Context(out var cx, out var pid) is { } e) return F<HotelPaymentDto>(e);
        if (c.Amount <= 0) return F<HotelPaymentDto>(Error.Validation("PAYMENT_AMOUNT_POSITIVE", ""));
        if (!ValidToken(c.FolioExpectedRowVersion, out var te)) return F<HotelPaymentDto>(te);
        var folio = await folioRepo.GetAsync(pid, c.HotelFolioId, ct);
        if (folio is null) return F<HotelPaymentDto>(Error.Business("FOLIO_NOT_FOUND", ""));
        folioRepo.ApplyExpectedRowVersion(folio, c.FolioExpectedRowVersion);
        var now = clock.UtcNow;
        var number = await numberGenerator.GenerateAsync(pid, cx.BusinessDate.BusinessDate, ct);
        var payment = HotelPayment.Create(pid, c.HotelFolioId, number, c.Method, c.Amount, c.Currency, c.ExchangeRate, c.ExternalReference, c.Notes, now, cx.Actor.ActorId);
        try
        {
            // Payment Engine creates the folio posting — single source of truth
            var entry = folio.AddPayment(c.Amount, $"Payment {c.Method}{(c.ExternalReference != null ? $" ({c.ExternalReference})" : "")}", c.Method, c.ExternalReference, now, cx.Actor.ActorId, cx.BusinessDate.BusinessDate);
            payment.LinkFolioEntry(entry.HotelFolioEntryId);
            await paymentRepo.AddAsync(payment, ct); await folioRepo.AddEntryAsync(entry, ct);
            var commit = await persistence.CommitAsync(ct);
            if (commit.IsFailure) return F<HotelPaymentDto>(commit.Error!.Category == ErrorCategory.Concurrency ? Error.Concurrency("PAYMENT_CONCURRENCY_CONFLICT", "") : commit.Error!);
            return Result<HotelPaymentDto>.Success(Map(payment));
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("FOLIO")) { return F<HotelPaymentDto>(Error.Business("FOLIO_NOT_OPEN", ex.Message)); }
        catch (ArgumentException ex) { return F<HotelPaymentDto>(Error.Validation("PAYMENT_INVALID", ex.Message)); }
    }

    // =============================================================================
    // SPLIT TENDER
    // =============================================================================
    public async Task<Result<IReadOnlyList<HotelPaymentDto>>> SplitTenderAsync(SplitTenderCommand c, CancellationToken ct)
    {
        if (!auth.HasPermission("FrontOffice.Payment.Receive")) return F<IReadOnlyList<HotelPaymentDto>>(Denied("Receive"));
        if (Context(out var cx, out var pid) is { } e) return F<IReadOnlyList<HotelPaymentDto>>(e);
        if (!ValidToken(c.FolioExpectedRowVersion, out var te)) return F<IReadOnlyList<HotelPaymentDto>>(te);
        if (c.Lines.Count == 0) return F<IReadOnlyList<HotelPaymentDto>>(Error.Validation("SPLIT_TENDER_EMPTY", ""));
        var folio = await folioRepo.GetAsync(pid, c.HotelFolioId, ct);
        if (folio is null) return F<IReadOnlyList<HotelPaymentDto>>(Error.Business("FOLIO_NOT_FOUND", ""));
        folioRepo.ApplyExpectedRowVersion(folio, c.FolioExpectedRowVersion);
        var now = clock.UtcNow;
        var payments = new List<HotelPaymentDto>();
        try
        {
            foreach (var line in c.Lines)
            {
                var number = await numberGenerator.GenerateAsync(pid, cx.BusinessDate.BusinessDate, ct);
                var payment = HotelPayment.Create(pid, c.HotelFolioId, number, line.Method, line.Amount, PaymentCurrency.VND, 1, line.ExternalReference, line.Notes, now, cx.Actor.ActorId);
                var entry = folio.AddPayment(line.Amount, $"Payment {line.Method}{(line.ExternalReference != null ? $" ({line.ExternalReference})" : "")}", line.Method, line.ExternalReference, now, cx.Actor.ActorId, cx.BusinessDate.BusinessDate);
                payment.LinkFolioEntry(entry.HotelFolioEntryId);
                await paymentRepo.AddAsync(payment, ct); await folioRepo.AddEntryAsync(entry, ct);
                payments.Add(Map(payment));
            }
            var commit = await persistence.CommitAsync(ct);
            if (commit.IsFailure) return F<IReadOnlyList<HotelPaymentDto>>(commit.Error!);
            return Result<IReadOnlyList<HotelPaymentDto>>.Success(payments);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("FOLIO")) { return F<IReadOnlyList<HotelPaymentDto>>(Error.Business("FOLIO_NOT_OPEN", ex.Message)); }
    }

    // =============================================================================
    // AUTHORIZE → CAPTURE → COMPLETE
    // =============================================================================
    public async Task<Result<HotelPaymentDto>> AuthorizeAsync(AuthorizePaymentCommand c, CancellationToken ct)
        => await Mutate(c.HotelPaymentId, c.ExpectedRowVersion, "FrontOffice.Payment.Authorize", (p, actor) => p.Authorize(c.AuthorizationCode, clock.UtcNow, actor), ct);

    public async Task<Result<HotelPaymentDto>> CaptureAsync(CapturePaymentCommand c, CancellationToken ct)
        => await Mutate(c.HotelPaymentId, c.ExpectedRowVersion, "FrontOffice.Payment.Capture", (p, actor) => p.Capture(clock.UtcNow, actor), ct);

    public async Task<Result<HotelPaymentDto>> CompleteAsync(CompletePaymentCommand c, CancellationToken ct)
        => await Mutate(c.HotelPaymentId, c.ExpectedRowVersion, "FrontOffice.Payment.Complete", (p, actor) => p.Complete(clock.UtcNow, actor), ct);

    public async Task<Result<HotelPaymentDto>> CancelAsync(CancelPaymentCommand c, CancellationToken ct)
        => await Mutate(c.HotelPaymentId, c.ExpectedRowVersion, "FrontOffice.Payment.Cancel", (p, actor) => p.Cancel(c.Reason, clock.UtcNow, actor), ct);

    // =============================================================================
    // ALLOCATE
    // =============================================================================
    public async Task<Result<PaymentAllocationDto>> AllocateAsync(AllocatePaymentCommand c, CancellationToken ct)
    {
        if (!auth.HasPermission("FrontOffice.Payment.Allocate")) return F<PaymentAllocationDto>(Denied("Allocate"));
        if (Context(out var cx, out var pid) is { } e) return F<PaymentAllocationDto>(e);
        if (!ValidToken(c.ExpectedRowVersion, out var te)) return F<PaymentAllocationDto>(te);
        var payment = await paymentRepo.GetAsync(pid, c.HotelPaymentId, ct);
        if (payment is null) return F<PaymentAllocationDto>(Error.Business("PAYMENT_NOT_FOUND", ""));
        paymentRepo.ApplyExpectedRowVersion(payment, c.ExpectedRowVersion);
        try
        {
            var allocation = payment.AllocateToCharge(c.ChargeFolioEntryId, c.ChargeDescription, c.Amount, clock.UtcNow);
            await paymentRepo.AddAllocationAsync(allocation, ct);
            var commit = await persistence.CommitAsync(ct);
            if (commit.IsFailure) return F<PaymentAllocationDto>(commit.Error!);
            return Result<PaymentAllocationDto>.Success(new PaymentAllocationDto(allocation.HotelPaymentAllocationId, allocation.HotelPaymentId, allocation.ChargeFolioEntryId, allocation.ChargeDescription, allocation.Amount, allocation.CreatedAtUtc));
        }
        catch (InvalidOperationException ex) { return F<PaymentAllocationDto>(Error.Business("PAYMENT_ALLOCATION_FAILED", ex.Message)); }
    }

    public async Task<Result<IReadOnlyList<PaymentAllocationDto>>> AllocateBatchAsync(AllocatePaymentBatchCommand c, CancellationToken ct)
    {
        if (!auth.HasPermission("FrontOffice.Payment.Allocate")) return F<IReadOnlyList<PaymentAllocationDto>>(Denied("Allocate"));
        if (Context(out var cx, out var pid) is { } e) return F<IReadOnlyList<PaymentAllocationDto>>(e);
        if (!ValidToken(c.ExpectedRowVersion, out var te)) return F<IReadOnlyList<PaymentAllocationDto>>(te);
        if (c.Lines.Count == 0) return F<IReadOnlyList<PaymentAllocationDto>>(Error.Validation("ALLOCATION_BATCH_EMPTY", ""));
        if (c.Lines.Any(x => x.Amount <= 0)) return F<IReadOnlyList<PaymentAllocationDto>>(Error.Validation("ALLOCATION_AMOUNT_POSITIVE", ""));
        if (c.Lines.Select(x => x.ChargeFolioEntryId).Distinct().Count() != c.Lines.Count)
            return F<IReadOnlyList<PaymentAllocationDto>>(Error.Validation("ALLOCATION_CHARGE_DUPLICATE", ""));

        var payment = await paymentRepo.GetAsync(pid, c.HotelPaymentId, ct);
        if (payment is null) return F<IReadOnlyList<PaymentAllocationDto>>(Error.Business("PAYMENT_NOT_FOUND", ""));
        paymentRepo.ApplyExpectedRowVersion(payment, c.ExpectedRowVersion);
        var total = c.Lines.Sum(x => x.Amount);
        if (payment.UnallocatedAmount < total)
            return F<IReadOnlyList<PaymentAllocationDto>>(Error.Business("ALLOCATION_EXCEEDS_UNALLOCATED", ""));

        try
        {
            // Validate the complete batch before mutating the aggregate so one bad
            // line cannot leave an in-memory payment partially allocated.
            var now = clock.UtcNow;
            var allocations = new List<HotelPaymentAllocation>(c.Lines.Count);
            foreach (var line in c.Lines)
                allocations.Add(payment.AllocateToCharge(line.ChargeFolioEntryId, line.ChargeDescription, line.Amount, now));

            foreach (var allocation in allocations)
                await paymentRepo.AddAllocationAsync(allocation, ct);

            var commit = await persistence.CommitAsync(ct);
            if (commit.IsFailure) return F<IReadOnlyList<PaymentAllocationDto>>(commit.Error!);
            return Result<IReadOnlyList<PaymentAllocationDto>>.Success(
                allocations.Select(a => new PaymentAllocationDto(a.HotelPaymentAllocationId, a.HotelPaymentId, a.ChargeFolioEntryId, a.ChargeDescription, a.Amount, a.CreatedAtUtc)).ToList());
        }
        catch (InvalidOperationException ex) { return F<IReadOnlyList<PaymentAllocationDto>>(Error.Business("PAYMENT_ALLOCATION_FAILED", ex.Message)); }
        catch (ArgumentException ex) { return F<IReadOnlyList<PaymentAllocationDto>>(Error.Validation("PAYMENT_ALLOCATION_INVALID", ex.Message)); }
    }

    // =============================================================================
    // REFUND (Partial or Full)
    // =============================================================================
    public async Task<Result<HotelPaymentDto>> RefundAsync(RefundPaymentCommand c, CancellationToken ct)
    {
        if (!auth.HasPermission("FrontOffice.Payment.Refund")) return F<HotelPaymentDto>(Denied("Refund"));
        if (Context(out var cx, out var pid) is { } e) return F<HotelPaymentDto>(e);
        if (!ValidToken(c.ExpectedRowVersion, out var te)) return F<HotelPaymentDto>(te);
        var payment = await paymentRepo.GetAsync(pid, c.HotelPaymentId, ct);
        if (payment is null) return F<HotelPaymentDto>(Error.Business("PAYMENT_NOT_FOUND", ""));
        paymentRepo.ApplyExpectedRowVersion(payment, c.ExpectedRowVersion);
        var amount = c.Amount ?? payment.Amount - payment.RefundedAmount;
        var folio = await folioRepo.GetAsync(pid, payment.HotelFolioId, ct);
        if (folio is null) return F<HotelPaymentDto>(Error.Business("FOLIO_NOT_FOUND", ""));
        try
        {
            var now = clock.UtcNow;
            var refundEntry = folio.AddRefund(amount, $"Refund Payment #{payment.PaymentNumber}: {c.Reason}", now, cx.Actor.ActorId, cx.BusinessDate.BusinessDate);
            payment.Refund(amount, refundEntry.HotelFolioEntryId, c.Reason, now, cx.Actor.ActorId);
            await folioRepo.AddEntryAsync(refundEntry, ct);
            var commit = await persistence.CommitAsync(ct);
            if (commit.IsFailure) return F<HotelPaymentDto>(commit.Error!);
            return Result<HotelPaymentDto>.Success(Map(payment));
        }
        catch (InvalidOperationException ex) { return F<HotelPaymentDto>(Error.Business("PAYMENT_REFUND_FAILED", ex.Message)); }
    }

    // =============================================================================
    // VOID
    // =============================================================================
    public async Task<Result<HotelPaymentDto>> VoidAsync(VoidPaymentCommand c, CancellationToken ct)
    {
        if (!auth.HasPermission("FrontOffice.Payment.Void")) return F<HotelPaymentDto>(Denied("Void"));
        if (Context(out var cx, out var pid) is { } e) return F<HotelPaymentDto>(e);
        if (!ValidToken(c.ExpectedRowVersion, out var te)) return F<HotelPaymentDto>(te);
        var payment = await paymentRepo.GetAsync(pid, c.HotelPaymentId, ct);
        if (payment is null) return F<HotelPaymentDto>(Error.Business("PAYMENT_NOT_FOUND", ""));
        paymentRepo.ApplyExpectedRowVersion(payment, c.ExpectedRowVersion);
        var folio = await folioRepo.GetAsync(pid, payment.HotelFolioId, ct);
        if (folio is null) return F<HotelPaymentDto>(Error.Business("FOLIO_NOT_FOUND", ""));
        try
        {
            var now = clock.UtcNow;
            var voidEntry = folio.AddCorrection(payment.Amount, $"Void Payment #{payment.PaymentNumber}: {c.Reason}", payment.FolioEntryId, now, cx.Actor.ActorId, cx.BusinessDate.BusinessDate);
            payment.Void(voidEntry.HotelFolioEntryId, c.Reason, now, cx.Actor.ActorId);
            await folioRepo.AddEntryAsync(voidEntry, ct);
            var commit = await persistence.CommitAsync(ct);
            if (commit.IsFailure) return F<HotelPaymentDto>(commit.Error!);
            return Result<HotelPaymentDto>.Success(Map(payment));
        }
        catch (InvalidOperationException ex) { return F<HotelPaymentDto>(Error.Business("PAYMENT_VOID_FAILED", ex.Message)); }
    }

    // =============================================================================
    // HELPERS
    // =============================================================================
    private async Task<Result<HotelPaymentDto>> Mutate(Guid paymentId, byte[] rowVersion, string permission, Action<HotelPayment, ActorId?> mutate, CancellationToken ct)
    {
        if (!auth.HasPermission(permission)) return F<HotelPaymentDto>(Denied(permission));
        if (Context(out var cx, out var pid) is { } e) return F<HotelPaymentDto>(e);
        if (!ValidToken(rowVersion, out var te)) return F<HotelPaymentDto>(te);
        var payment = await paymentRepo.GetAsync(pid, paymentId, ct);
        if (payment is null) return F<HotelPaymentDto>(Error.Business("PAYMENT_NOT_FOUND", ""));
        paymentRepo.ApplyExpectedRowVersion(payment, rowVersion);
        try
        {
            mutate(payment, cx.Actor.ActorId);
            var commit = await persistence.CommitAsync(ct);
            if (commit.IsFailure) return F<HotelPaymentDto>(commit.Error!);
            return Result<HotelPaymentDto>.Success(Map(payment));
        }
        catch (InvalidOperationException ex) { return F<HotelPaymentDto>(Error.Business("PAYMENT_INVALID_STATE", ex.Message)); }
    }

    private static HotelPaymentDto Map(HotelPayment p) => new(p.HotelPaymentId, p.PaymentNumber, p.HotelFolioId, p.PaymentMethod, p.Amount, p.RefundedAmount, p.AllocatedAmount, p.Status, p.Currency, p.AuthorizationCode, p.ExternalReference, p.Notes, p.CreatedAtUtc, p.VoidedAtUtc, p.VoidReason, p.RefundedAtUtc, p.RefundReason, p.RowVersion);
}
