using ASMILE.BO.Application.FrontOffice.Foundation.Contexts;
using ASMILE.BO.Application.FrontOffice.Foundation.Cqrs;
using ASMILE.BO.Application.FrontOffice.Foundation.Persistence;
using ASMILE.BO.Application.FrontOffice.Stay;
using ASMILE.BO.Application.Interfaces;
using ASMILE.BO.Core.FrontOffice.Folio;
using ASMILE.BO.Core.FrontOffice.Foundation.Identifiers;
using ASMILE.BO.Core.FrontOffice.Payment;
using ASMILE.BO.Shared.Foundation;

namespace ASMILE.BO.Application.FrontOffice.Folio;

public sealed class HotelFolioApplicationService(
    IHotelFolioRepository repository,
    IHotelFolioNumberGenerator numberGenerator,
    IHotelStayRepository stayRepository,
    IFrontOfficePersistenceSession persistence,
    IFrontOfficeExecutionContextAccessor contextAccessor,
    IAuthorizationService authorization,
    IClock clock)
{
    private static Error Perm(string action) => Error.Authorization("FOLIO_PERMISSION_DENIED", $"Folio {action} permission is required.");
    private Error? Context(out FrontOfficeExecutionContext ctx, out Guid pid) { ctx = contextAccessor.Current!; pid = ctx?.Property.PropertyId.Value ?? Guid.Empty; return ctx is null ? Error.Authorization("FOLIO_PERMISSION_DENIED", "A trusted Front Office property context is required.") : null; }

    // =============================================================================
    // CREATE
    // =============================================================================
    public async Task<Result<HotelFolioDto>> CreateAsync(CreateFolioCommand c, CancellationToken ct)
    {
        if (!authorization.HasPermission("FrontOffice.Folio.Create")) return Fail<HotelFolioDto>(Perm("create"));
        if (Context(out var ctx, out var pid) is { } e) return Fail<HotelFolioDto>(e);
        var stay = await stayRepository.GetDtoAsync(pid, c.HotelStayId, ct);
        if (stay is null) return Fail<HotelFolioDto>(Error.Business("FOLIO_STAY_NOT_FOUND", "Stay not found."));
        var existing = await repository.GetByStayAsync(pid, c.HotelStayId, ct);
        if (existing is not null) return Result<HotelFolioDto>.Success(existing);
        var now = clock.UtcNow;
        var number = await numberGenerator.GenerateAsync(pid, ctx.BusinessDate.BusinessDate, ct);
        var folio = HotelFolio.Create(pid, c.HotelStayId, number, now, ctx.Actor.ActorId);
        await repository.AddAsync(folio, ct);
        await persistence.CommitAsync(ct);
        return await repository.GetDtoAsync(pid, folio.HotelFolioId, ct) is { } dto ? Result<HotelFolioDto>.Success(dto) : Fail<HotelFolioDto>(Error.System("FOLIO_READ_FAILED", "Read after create failed."));
    }

    // =============================================================================
    // CHARGE POSTINGS
    // =============================================================================
    public Task<Result<HotelFolioEntryDto>> AddRoomChargeAsync(AddFolioRoomChargeCommand c, CancellationToken ct) => AddEntry(c.HotelFolioId, c.ExpectedRowVersion, ct, "FrontOffice.Folio.AddCharge", (f, actor) => f.AddRoomCharge(c.Amount, c.Description, c.BusinessDate, c.RatePlanId, clock.UtcNow, actor));
    public Task<Result<HotelFolioEntryDto>> AddRestaurantChargeAsync(AddFolioRestaurantChargeCommand c, CancellationToken ct) => AddEntry(c.HotelFolioId, c.ExpectedRowVersion, ct, "FrontOffice.Folio.AddCharge", (f, actor) => f.AddRestaurantCharge(c.Amount, c.Description, c.ReferenceId, clock.UtcNow, actor));
    public Task<Result<HotelFolioEntryDto>> AddTaxAsync(AddFolioTaxCommand c, CancellationToken ct) => AddEntry(c.HotelFolioId, c.ExpectedRowVersion, ct, "FrontOffice.Folio.AddCharge", (f, actor) => f.AddTax(c.Amount, c.Description, clock.UtcNow, actor));
    public Task<Result<HotelFolioEntryDto>> AddServiceChargeAsync(AddFolioServiceChargeCommand c, CancellationToken ct) => AddEntry(c.HotelFolioId, c.ExpectedRowVersion, ct, "FrontOffice.Folio.AddCharge", (f, actor) => f.AddServiceCharge(c.Amount, c.Description, clock.UtcNow, actor));
    public Task<Result<HotelFolioEntryDto>> AddDiscountAsync(AddFolioDiscountCommand c, CancellationToken ct) => AddEntry(c.HotelFolioId, c.ExpectedRowVersion, ct, "FrontOffice.Folio.AddCharge", (f, actor) => f.AddDiscount(c.Amount, c.Description, clock.UtcNow, actor));
    public Task<Result<HotelFolioEntryDto>> AddPaymentAsync(AddFolioPaymentCommand c, CancellationToken ct) => AddEntry(c.HotelFolioId, c.ExpectedRowVersion, ct, "FrontOffice.Folio.AddPayment", (f, actor) => f.AddPayment(c.Amount, c.Description, c.PaymentMethod is not null ? Enum.TryParse<HotelPaymentMethod>(c.PaymentMethod, out var m) ? m : null : null, c.ExternalReference, clock.UtcNow, actor));
    public Task<Result<HotelFolioEntryDto>> AddDepositAsync(AddFolioDepositCommand c, CancellationToken ct) => AddEntry(c.HotelFolioId, c.ExpectedRowVersion, ct, "FrontOffice.Folio.AddDeposit", (f, actor) => f.AddDeposit(c.Amount, c.Description, clock.UtcNow, actor));
    public Task<Result<HotelFolioEntryDto>> AddAdjustmentAsync(AddFolioAdjustmentCommand c, CancellationToken ct) => AddEntry(c.HotelFolioId, c.ExpectedRowVersion, ct, "FrontOffice.Folio.AddCharge", (f, actor) => f.AddAdjustment(c.Amount, c.Description, clock.UtcNow, actor));
    public Task<Result<HotelFolioEntryDto>> AddCorrectionAsync(AddFolioCorrectionCommand c, CancellationToken ct) => AddEntry(c.HotelFolioId, c.ExpectedRowVersion, ct, "FrontOffice.Folio.AddCharge", (f, actor) => f.AddCorrection(c.Amount, c.Description, c.OriginalEntryId, clock.UtcNow, actor));
    public Task<Result<HotelFolioEntryDto>> AddManualChargeAsync(AddFolioManualChargeCommand c, CancellationToken ct) => AddEntry(c.HotelFolioId, c.ExpectedRowVersion, ct, "FrontOffice.Folio.AddCharge", (f, actor) => f.AddManualCharge(c.Amount, c.Description, clock.UtcNow, actor));
    public Task<Result<HotelFolioEntryDto>> AddManualCreditAsync(AddFolioManualCreditCommand c, CancellationToken ct) => AddEntry(c.HotelFolioId, c.ExpectedRowVersion, ct, "FrontOffice.Folio.AddCharge", (f, actor) => f.AddManualCredit(c.Amount, c.Description, clock.UtcNow, actor));
    public Task<Result<HotelFolioEntryDto>> AddRefundAsync(AddFolioRefundCommand c, CancellationToken ct) => AddEntry(c.HotelFolioId, c.ExpectedRowVersion, ct, "FrontOffice.Folio.AddPayment", (f, actor) => f.AddRefund(c.Amount, c.Description, clock.UtcNow, actor));

    // =============================================================================
    // TRANSFER
    // =============================================================================
    public async Task<Result<HotelFolioEntryDto>> TransferAsync(AddFolioTransferCommand c, CancellationToken ct)
    {
        if (!authorization.HasPermission("FrontOffice.Folio.AddCharge")) return Fail<HotelFolioEntryDto>(Perm("transfer"));
        if (Context(out var ctx, out var pid) is { } e) return Fail<HotelFolioEntryDto>(e);
        if (c.ExpectedRowVersion is not { Length: 8 }) return Fail<HotelFolioEntryDto>(Error.Validation("FOLIO_ROWVERSION_REQUIRED", "Row version required."));
        var fromFolio = await repository.GetAsync(pid, c.FromFolioId, ct);
        if (fromFolio is null) return Fail<HotelFolioEntryDto>(Error.Business("FOLIO_NOT_FOUND", "Source folio not found."));
        var toFolio = await repository.GetAsync(pid, c.ToFolioId, ct);
        if (toFolio is null) return Fail<HotelFolioEntryDto>(Error.Business("FOLIO_NOT_FOUND", "Target folio not found."));
        repository.ApplyExpectedRowVersion(fromFolio, c.ExpectedRowVersion);
        try
        {
            var now = clock.UtcNow;
            var credit = fromFolio.AddAdjustment(-c.Amount, $"Transfer to {toFolio.FolioNumber}: {c.Description}", now, ctx.Actor.ActorId);
            var debit = toFolio.AddAdjustment(c.Amount, $"Transfer from {fromFolio.FolioNumber}: {c.Description}", now, ctx.Actor.ActorId);
            await repository.AddEntryAsync(credit, ct);
            await repository.AddEntryAsync(debit, ct);
            var commit = await persistence.CommitAsync(ct);
            if (commit.IsFailure) return Fail<HotelFolioEntryDto>(commit.Error!);
            return Result<HotelFolioEntryDto>.Success(MapEntry(credit));
        }
        catch (InvalidOperationException ex) { return Fail<HotelFolioEntryDto>(Error.Business("FOLIO_TRANSFER_FAILED", ex.Message)); }
    }

    // =============================================================================
    // REVERSAL
    // =============================================================================
    public async Task<Result<HotelFolioEntryDto>> ReverseEntryAsync(ReverseFolioEntryCommand c, CancellationToken ct)
    {
        if (!authorization.HasPermission("FrontOffice.Folio.Reverse")) return Fail<HotelFolioEntryDto>(Perm("reverse"));
        if (Context(out var ctx, out var pid) is { } e) return Fail<HotelFolioEntryDto>(e);
        if (c.ExpectedRowVersion is not { Length: 8 }) return Fail<HotelFolioEntryDto>(Error.Validation("FOLIO_ROWVERSION_REQUIRED", "Row version required."));
        var folio = await repository.GetAsync(pid, c.HotelFolioId, ct);
        if (folio is null) return Fail<HotelFolioEntryDto>(Error.Business("FOLIO_NOT_FOUND", "Folio not found."));
        var entry = folio.Entries.SingleOrDefault(e => e.HotelFolioEntryId == c.EntryId);
        if (entry is null) return Fail<HotelFolioEntryDto>(Error.Business("FOLIO_ENTRY_NOT_FOUND", "Entry not found."));
        if (entry.ReversedByEntryId.HasValue) return Fail<HotelFolioEntryDto>(Error.Business("FOLIO_ENTRY_ALREADY_REVERSED", "Already reversed."));
        repository.ApplyExpectedRowVersion(folio, c.ExpectedRowVersion);
        var now = clock.UtcNow;
        var reversal = folio.AddCorrection(-entry.Amount, $"Reversal: {c.Reason} (Entry #{entry.SequenceNumber})", entry.HotelFolioEntryId, now, ctx.Actor.ActorId);
        await repository.AddEntryAsync(reversal, ct);
        await repository.UpdateEntryReversalAsync(entry, reversal, ct);
        var commit = await persistence.CommitAsync(ct);
        if (commit.IsFailure) return Fail<HotelFolioEntryDto>(Error.Concurrency("FOLIO_CONCURRENCY_CONFLICT", "Concurrent modification."));
        return Result<HotelFolioEntryDto>.Success(MapEntry(reversal));
    }

    // =============================================================================
    // STATE TRANSITIONS
    // =============================================================================
    public async Task<Result<HotelFolioDto>> SuspendAsync(SuspendFolioCommand c, CancellationToken ct) => await MutateFolio(c.HotelFolioId, c.ExpectedRowVersion, "FrontOffice.Folio.Manage", (f, ctx) => f.Suspend(clock.UtcNow, ctx.Actor.ActorId), ct);
    public async Task<Result<HotelFolioDto>> ResumeAsync(ResumeFolioCommand c, CancellationToken ct) => await MutateFolio(c.HotelFolioId, c.ExpectedRowVersion, "FrontOffice.Folio.Manage", (f, ctx) => f.Resume(clock.UtcNow, ctx.Actor.ActorId), ct);
    public async Task<Result<HotelFolioDto>> CloseAsync(CloseFolioCommand c, CancellationToken ct) => await MutateFolio(c.HotelFolioId, c.ExpectedRowVersion, "FrontOffice.Folio.Manage", (f, ctx) => f.Close(clock.UtcNow, ctx.Actor.ActorId), ct);
    public async Task<Result<HotelFolioDto>> SettleAsync(SettleFolioCommand c, CancellationToken ct) => await MutateFolio(c.HotelFolioId, c.ExpectedRowVersion, "FrontOffice.Folio.Manage", (f, ctx) => f.Settle(clock.UtcNow, ctx.Actor.ActorId), ct);
    public async Task<Result<HotelFolioDto>> ReopenAsync(ReopenFolioCommand c, CancellationToken ct) => await MutateFolio(c.HotelFolioId, c.ExpectedRowVersion, "FrontOffice.Folio.Manage", (f, ctx) => f.Reopen(clock.UtcNow, ctx.Actor.ActorId), ct);

    // =============================================================================
    // SEARCH
    // =============================================================================
    public async Task<Result<PageResult<FolioLedgerDto>>> SearchAsync(SearchFoliosQuery q, CancellationToken ct)
    {
        if (!authorization.HasPermission("FrontOffice.Folio.View")) return Result<PageResult<FolioLedgerDto>>.Failure(Perm("view"));
        return Result<PageResult<FolioLedgerDto>>.Success(await repository.SearchAsync(q.Filter, q.Page, ct));
    }

    // =============================================================================
    // HELPERS
    // =============================================================================
    private static Result<T> Fail<T>(Error? e) => Result<T>.Failure(e!);

    private async Task<Result<HotelFolioEntryDto>> AddEntry(Guid folioId, byte[] rowVersion, CancellationToken ct, string permission, Func<HotelFolio, ActorId?, HotelFolioEntry> add)
    {
        if (!authorization.HasPermission(permission)) return Fail<HotelFolioEntryDto>(Perm("add entry"));
        if (Context(out var ctx, out var pid) is { } e) return Fail<HotelFolioEntryDto>(e);
        if (rowVersion is not { Length: 8 }) return Fail<HotelFolioEntryDto>(Error.Validation("FOLIO_ROWVERSION_REQUIRED", "Row version required."));
        var folio = await repository.GetAsync(pid, folioId, ct);
        if (folio is null) return Fail<HotelFolioEntryDto>(Error.Business("FOLIO_NOT_FOUND", "Folio not found."));
        repository.ApplyExpectedRowVersion(folio, rowVersion);
        try
        {
            var entry = add(folio, ctx.Actor.ActorId);
            await repository.AddEntryAsync(entry, ct);
            var commit = await persistence.CommitAsync(ct);
            if (commit.IsFailure) return Fail<HotelFolioEntryDto>(Error.Concurrency("FOLIO_CONCURRENCY_CONFLICT", "Concurrent modification."));
            return Result<HotelFolioEntryDto>.Success(MapEntry(entry));
        }
        catch (InvalidOperationException ex) { return Fail<HotelFolioEntryDto>(Error.Business("FOLIO_INVALID_STATE", ex.Message)); }
        catch (ArgumentException ex) { return Fail<HotelFolioEntryDto>(Error.Validation("FOLIO_INVALID_INPUT", ex.Message)); }
    }

    private async Task<Result<HotelFolioDto>> MutateFolio(Guid folioId, byte[] rowVersion, string permission, Action<HotelFolio, FrontOfficeExecutionContext> mutate, CancellationToken ct)
    {
        if (!authorization.HasPermission(permission)) return Fail<HotelFolioDto>(Perm("manage"));
        if (Context(out var ctx, out var pid) is { } e) return Fail<HotelFolioDto>(e);
        if (rowVersion is not { Length: 8 }) return Fail<HotelFolioDto>(Error.Validation("FOLIO_ROWVERSION_REQUIRED", "Row version required."));
        var folio = await repository.GetAsync(pid, folioId, ct);
        if (folio is null) return Fail<HotelFolioDto>(Error.Business("FOLIO_NOT_FOUND", "Folio not found."));
        repository.ApplyExpectedRowVersion(folio, rowVersion);
        try
        {
            mutate(folio, ctx);
            var commit = await persistence.CommitAsync(ct);
            if (commit.IsFailure) return Fail<HotelFolioDto>(Error.Concurrency("FOLIO_CONCURRENCY_CONFLICT", "Concurrent modification."));
            return await repository.GetDtoAsync(pid, folio.HotelFolioId, ct) is { } dto ? Result<HotelFolioDto>.Success(dto) : Fail<HotelFolioDto>(Error.System("FOLIO_READ_FAILED", "Read after mutate failed."));
        }
        catch (InvalidOperationException ex) { return Fail<HotelFolioDto>(Error.Business("FOLIO_INVALID_STATE", ex.Message)); }
    }

    private static HotelFolioEntryDto MapEntry(HotelFolioEntry e) => new(e.HotelFolioEntryId, e.SequenceNumber, e.EntryType, e.Amount, e.Description, e.ReferenceType, e.ReferenceId, e.BusinessDate, e.ReversesEntryId, e.ReversedByEntryId, e.CreatedAtUtc, e.CreatedBy);
}
