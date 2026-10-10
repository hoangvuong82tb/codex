using ASMILE.BO.Application.FrontOffice.Foundation.Contexts;
using ASMILE.BO.Application.FrontOffice.Foundation.Cqrs;
using ASMILE.BO.Application.FrontOffice.Foundation.Persistence;
using ASMILE.BO.Application.FrontOffice.Stay;
using ASMILE.BO.Application.FrontOffice.HotelMasterData;
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
    IClock clock,
    IHotelStructureMasterDataRepository structureRepository)
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
    public Task<Result<HotelFolioEntryDto>> AddRoomChargeAsync(AddFolioRoomChargeCommand c, CancellationToken ct) => AddEntry(c.HotelFolioId, c.ExpectedRowVersion, ct, "FrontOffice.Folio.AddCharge", (f, actor, _) => f.AddRoomCharge(c.Amount, c.Description, c.BusinessDate, c.RatePlanId, clock.UtcNow, actor));
    public Task<Result<HotelFolioEntryDto>> AddRestaurantChargeAsync(AddFolioRestaurantChargeCommand c, CancellationToken ct) => AddEntry(c.HotelFolioId, c.ExpectedRowVersion, ct, "FrontOffice.Folio.AddCharge", (f, actor, businessDate) => f.AddRestaurantCharge(c.Amount, c.Description, c.ReferenceId, clock.UtcNow, actor, businessDate));
    public Task<Result<HotelFolioEntryDto>> AddTaxAsync(AddFolioTaxCommand c, CancellationToken ct) => AddEntry(c.HotelFolioId, c.ExpectedRowVersion, ct, "FrontOffice.Folio.AddCharge", (f, actor, businessDate) => f.AddTax(c.Amount, c.Description, clock.UtcNow, actor, businessDate));
    public Task<Result<HotelFolioEntryDto>> AddServiceChargeAsync(AddFolioServiceChargeCommand c, CancellationToken ct) => AddEntry(c.HotelFolioId, c.ExpectedRowVersion, ct, "FrontOffice.Folio.AddCharge", (f, actor, businessDate) => f.AddServiceCharge(c.Amount, c.Description, clock.UtcNow, actor, businessDate));
    public Task<Result<HotelFolioEntryDto>> AddDiscountAsync(AddFolioDiscountCommand c, CancellationToken ct) => AddEntry(c.HotelFolioId, c.ExpectedRowVersion, ct, "FrontOffice.Folio.AddCharge", (f, actor, businessDate) => f.AddDiscount(c.Amount, c.Description, clock.UtcNow, actor, businessDate));
    public Task<Result<HotelFolioEntryDto>> AddPaymentAsync(AddFolioPaymentCommand c, CancellationToken ct) => AddEntry(c.HotelFolioId, c.ExpectedRowVersion, ct, "FrontOffice.Folio.AddPayment", (f, actor, businessDate) => f.AddPayment(c.Amount, c.Description, c.PaymentMethod is not null ? Enum.TryParse<HotelPaymentMethod>(c.PaymentMethod, out var m) ? m : null : null, c.ExternalReference, clock.UtcNow, actor, businessDate));
    public Task<Result<HotelFolioEntryDto>> AddDepositAsync(AddFolioDepositCommand c, CancellationToken ct) => AddEntry(c.HotelFolioId, c.ExpectedRowVersion, ct, "FrontOffice.Folio.AddDeposit", (f, actor, businessDate) => f.AddDeposit(c.Amount, c.Description, clock.UtcNow, actor, businessDate));
    public Task<Result<HotelFolioEntryDto>> AddAdjustmentAsync(AddFolioAdjustmentCommand c, CancellationToken ct) => AddEntry(c.HotelFolioId, c.ExpectedRowVersion, ct, "FrontOffice.Folio.AddCharge", (f, actor, businessDate) => f.AddAdjustment(c.Amount, c.Description, clock.UtcNow, actor, businessDate));
    public async Task<Result<HotelFolioEntryDto>> AddCorrectionAsync(AddFolioCorrectionCommand c, CancellationToken ct)
    {
        if (!authorization.HasPermission("FrontOffice.Folio.AddCharge")) return Fail<HotelFolioEntryDto>(Perm("add correction"));
        if (Context(out var ctx, out var pid) is { } e) return Fail<HotelFolioEntryDto>(e);
        var policy = await structureRepository.GetActivePostedCorrectionPolicyAsync(pid, "Folio", clock.UtcNow, ct);
        if (policy is not null)
        {
            if (policy.RequireReason && string.IsNullOrWhiteSpace(c.Description)) return Fail<HotelFolioEntryDto>(Error.Validation("FOLIO_CORRECTION_REASON_REQUIRED", "A correction reason is required by the active policy."));
            if (policy.MaximumAmount > 0 && Math.Abs(c.Amount) > policy.MaximumAmount) return Fail<HotelFolioEntryDto>(Error.Business("FOLIO_CORRECTION_LIMIT_EXCEEDED", "The correction amount exceeds the active policy limit."));
            var allowedRoles = policy.AllowedRoles.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            if (allowedRoles.Length > 0 && !ctx.Actor.Roles.Any(role => allowedRoles.Contains(role, StringComparer.OrdinalIgnoreCase))) return Fail<HotelFolioEntryDto>(Error.Authorization("FOLIO_CORRECTION_ROLE_NOT_ALLOWED", "The current role is not allowed to correct posted entries."));
            if (policy.RequireApproval && !authorization.HasPermission("FrontOffice.Folio.CorrectionApprove")) return Fail<HotelFolioEntryDto>(Error.Authorization("FOLIO_CORRECTION_APPROVAL_REQUIRED", "An approval permission is required by the active correction policy."));
        }
        return await AddEntry(c.HotelFolioId, c.ExpectedRowVersion, ct, "FrontOffice.Folio.AddCharge", (f, actor, businessDate) => f.AddCorrection(c.Amount, c.Description, c.OriginalEntryId, clock.UtcNow, actor, businessDate));
    }
    public Task<Result<HotelFolioEntryDto>> AddManualChargeAsync(AddFolioManualChargeCommand c, CancellationToken ct) => AddEntry(c.HotelFolioId, c.ExpectedRowVersion, ct, "FrontOffice.Folio.AddCharge", (f, actor, businessDate) => f.AddManualCharge(c.Amount, c.Description, clock.UtcNow, actor, businessDate));
    public Task<Result<HotelFolioEntryDto>> AddManualCreditAsync(AddFolioManualCreditCommand c, CancellationToken ct) => AddEntry(c.HotelFolioId, c.ExpectedRowVersion, ct, "FrontOffice.Folio.AddCharge", (f, actor, businessDate) => f.AddManualCredit(c.Amount, c.Description, clock.UtcNow, actor, businessDate));
    public Task<Result<HotelFolioEntryDto>> AddRefundAsync(AddFolioRefundCommand c, CancellationToken ct) => AddEntry(c.HotelFolioId, c.ExpectedRowVersion, ct, "FrontOffice.Folio.AddPayment", (f, actor, businessDate) => f.AddRefund(c.Amount, c.Description, clock.UtcNow, actor, businessDate));

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
            var credit = fromFolio.AddTransfer(-c.Amount, $"Transfer to {toFolio.FolioNumber}: {c.Description}", null, now, ctx.Actor.ActorId, ctx.BusinessDate.BusinessDate);
            var debit = toFolio.AddTransfer(c.Amount, $"Transfer from {fromFolio.FolioNumber}: {c.Description}", null, now, ctx.Actor.ActorId, ctx.BusinessDate.BusinessDate);
            await repository.AddEntryAsync(credit, ct);
            await repository.AddEntryAsync(debit, ct);
            var commit = await persistence.CommitAsync(ct);
            if (commit.IsFailure) return Fail<HotelFolioEntryDto>(commit.Error!);
            return Result<HotelFolioEntryDto>.Success(MapEntry(credit));
        }
        catch (InvalidOperationException ex) { return Fail<HotelFolioEntryDto>(Error.Business("FOLIO_TRANSFER_FAILED", ex.Message)); }
    }

    // =============================================================================
    // SPLIT / MERGE
    // =============================================================================
    public async Task<Result<FolioTransferResult>> SplitAsync(SplitFolioCommand c, CancellationToken ct)
    {
        if (!authorization.HasPermission("FrontOffice.Folio.AddCharge")) return Fail<FolioTransferResult>(Perm("split"));
        if (Context(out var ctx, out var pid) is { } e) return Fail<FolioTransferResult>(e);
        if (c.SourceFolioId == c.TargetFolioId) return Fail<FolioTransferResult>(Error.Validation("FOLIO_SPLIT_SAME_TARGET", "Source and target Folio must be different."));
        if (c.Amount <= 0) return Fail<FolioTransferResult>(Error.Validation("FOLIO_SPLIT_AMOUNT_INVALID", "Split amount must be greater than zero."));
        if (c.ExpectedSourceRowVersion is not { Length: 8 } || c.ExpectedTargetRowVersion is not { Length: 8 }) return Fail<FolioTransferResult>(Error.Validation("FOLIO_ROWVERSION_REQUIRED", "Source and target Folio row versions are required."));

        var source = await repository.GetAsync(pid, c.SourceFolioId, ct);
        var target = await repository.GetAsync(pid, c.TargetFolioId, ct);
        if (source is null || target is null) return Fail<FolioTransferResult>(Error.Business("FOLIO_NOT_FOUND", "Source or target Folio not found."));
        if (source.RunningBalance < c.Amount) return Fail<FolioTransferResult>(Error.Business("FOLIO_SPLIT_BALANCE_EXCEEDED", "Split amount exceeds the source Folio balance."));
        var allocations = c.Allocations?.ToList();
        if (allocations is { Count: 0 }) return Fail<FolioTransferResult>(Error.Validation("FOLIO_SPLIT_ALLOCATION_REQUIRED", "At least one source entry allocation is required when allocation mode is used."));
        if (allocations is not null)
        {
            if (allocations.Any(x => x.SourceEntryId == Guid.Empty || x.Amount <= 0))
                return Fail<FolioTransferResult>(Error.Validation("FOLIO_SPLIT_ALLOCATION_INVALID", "Every split allocation must reference an entry and have a positive amount."));
            if (allocations.Select(x => x.SourceEntryId).Distinct().Count() != allocations.Count)
                return Fail<FolioTransferResult>(Error.Validation("FOLIO_SPLIT_ALLOCATION_DUPLICATE", "A source entry can appear only once in a split."));
            if (allocations.Sum(x => x.Amount) != c.Amount)
                return Fail<FolioTransferResult>(Error.Validation("FOLIO_SPLIT_ALLOCATION_TOTAL_MISMATCH", "Allocation total must equal the split amount."));

            foreach (var allocation in allocations)
            {
                var original = source.Entries.SingleOrDefault(x => x.HotelFolioEntryId == allocation.SourceEntryId);
                if (original is null) return Fail<FolioTransferResult>(Error.Business("FOLIO_SPLIT_ENTRY_NOT_FOUND", "A selected source entry was not found in the source Folio."));
                if (original.Amount <= 0 || original.ReversedByEntryId.HasValue || original.EntryType == HotelFolioEntryType.Transfer)
                    return Fail<FolioTransferResult>(Error.Business("FOLIO_SPLIT_ENTRY_NOT_SPLITTABLE", $"Source entry {original.SequenceNumber} cannot be split."));
                var alreadyTransferred = source.Entries
                    .Where(x => x.EntryType == HotelFolioEntryType.Transfer && x.ReferenceId == original.HotelFolioEntryId && x.Amount < 0)
                    .Sum(x => -x.Amount);
                if (alreadyTransferred + allocation.Amount > original.Amount)
                    return Fail<FolioTransferResult>(Error.Business("FOLIO_SPLIT_ENTRY_AMOUNT_EXCEEDED", $"Split amount exceeds the remaining amount of source entry {original.SequenceNumber}."));
            }
        }
        repository.ApplyExpectedRowVersion(source, c.ExpectedSourceRowVersion);
        repository.ApplyExpectedRowVersion(target, c.ExpectedTargetRowVersion);
        try
        {
            var now = clock.UtcNow;
            var transferResults = new List<FolioTransferAllocationResult>();
            if (allocations is null)
            {
                var sourceEntry = source.AddTransfer(-c.Amount, $"Split to {target.FolioNumber}: {c.Description}", null, now, ctx.Actor.ActorId, ctx.BusinessDate.BusinessDate);
                var targetEntry = target.AddTransfer(c.Amount, $"Split from {source.FolioNumber}: {c.Description}", null, now, ctx.Actor.ActorId, ctx.BusinessDate.BusinessDate);
                await repository.AddEntryAsync(sourceEntry, ct);
                await repository.AddEntryAsync(targetEntry, ct);
                transferResults.Add(new FolioTransferAllocationResult(Guid.Empty, c.Amount, MapEntry(sourceEntry), MapEntry(targetEntry)));
            }
            else
            {
                foreach (var allocation in allocations)
                {
                    var sourceEntry = source.AddTransfer(-allocation.Amount, $"Split khoản {allocation.SourceEntryId:N} to {target.FolioNumber}: {c.Description}", allocation.SourceEntryId, now, ctx.Actor.ActorId, ctx.BusinessDate.BusinessDate);
                    var targetEntry = target.AddTransfer(allocation.Amount, $"Split khoản {allocation.SourceEntryId:N} from {source.FolioNumber}: {c.Description}", allocation.SourceEntryId, now, ctx.Actor.ActorId, ctx.BusinessDate.BusinessDate);
                    await repository.AddEntryAsync(sourceEntry, ct);
                    await repository.AddEntryAsync(targetEntry, ct);
                    transferResults.Add(new FolioTransferAllocationResult(allocation.SourceEntryId, allocation.Amount, MapEntry(sourceEntry), MapEntry(targetEntry)));
                }
            }
            var commit = await persistence.CommitAsync(ct);
            if (commit.IsFailure) return Fail<FolioTransferResult>(Error.Concurrency("FOLIO_SPLIT_CONCURRENCY_CONFLICT", "Concurrent Folio modification."));
            var first = transferResults[0];
            return Result<FolioTransferResult>.Success(new FolioTransferResult(source.HotelFolioId, target.HotelFolioId, c.Amount, first.SourceEntry, first.TargetEntry, transferResults));
        }
        catch (InvalidOperationException ex) { return Fail<FolioTransferResult>(Error.Business("FOLIO_SPLIT_FAILED", ex.Message)); }
    }

    public async Task<Result<FolioMergeResult>> MergeAsync(MergeFoliosCommand c, CancellationToken ct)
    {
        if (!authorization.HasPermission("FrontOffice.Folio.Manage")) return Fail<FolioMergeResult>(Perm("merge"));
        if (Context(out var ctx, out var pid) is { } e) return Fail<FolioMergeResult>(e);
        if (c.Sources is null || c.Sources.Count == 0) return Fail<FolioMergeResult>(Error.Validation("FOLIO_MERGE_SOURCE_REQUIRED", "At least one source Folio is required."));
        if (c.Sources.Any(x => x.FolioId == c.TargetFolioId)) return Fail<FolioMergeResult>(Error.Validation("FOLIO_MERGE_TARGET_IN_SOURCE", "Target Folio cannot also be a source."));
        if (c.Sources.Select(x => x.FolioId).Distinct().Count() != c.Sources.Count) return Fail<FolioMergeResult>(Error.Validation("FOLIO_MERGE_DUPLICATE_SOURCE", "Source Folios must be unique."));
        if (c.ExpectedTargetRowVersion is not { Length: 8 }) return Fail<FolioMergeResult>(Error.Validation("FOLIO_ROWVERSION_REQUIRED", "Target Folio row version required."));
        if (c.Rules is not null && !SupportedMergeRules(c.Rules, out var unsupportedRule))
            return Fail<FolioMergeResult>(Error.Business("FOLIO_MERGE_RULE_NOT_SUPPORTED", $"Merge rule is not supported by the persisted ledger: {unsupportedRule}."));

        var target = await repository.GetAsync(pid, c.TargetFolioId, ct);
        if (target is null) return Fail<FolioMergeResult>(Error.Business("FOLIO_NOT_FOUND", "Target Folio not found."));
        var sources = new List<HotelFolio>();
        foreach (var item in c.Sources)
        {
            if (item.ExpectedRowVersion is not { Length: 8 }) return Fail<FolioMergeResult>(Error.Validation("FOLIO_ROWVERSION_REQUIRED", "Every source Folio row version is required."));
            var source = await repository.GetAsync(pid, item.FolioId, ct);
            if (source is null) return Fail<FolioMergeResult>(Error.Business("FOLIO_NOT_FOUND", "A source Folio was not found."));
            if (source.RunningBalance <= 0) return Fail<FolioMergeResult>(Error.Business("FOLIO_MERGE_SOURCE_BALANCE_INVALID", $"Source Folio {source.FolioNumber} has no positive balance to merge."));
            repository.ApplyExpectedRowVersion(source, item.ExpectedRowVersion);
            sources.Add(source);
        }
        repository.ApplyExpectedRowVersion(target, c.ExpectedTargetRowVersion);

        try
        {
            var now = clock.UtcNow;
            var transfers = new List<FolioTransferResult>(sources.Count);
            foreach (var source in sources)
            {
                var amount = source.RunningBalance;
                if (c.Rules is null)
                {
                    var sourceEntry = source.AddTransfer(-amount, $"Merge to {target.FolioNumber}: {c.Description}", null, now, ctx.Actor.ActorId, ctx.BusinessDate.BusinessDate);
                    var targetEntry = target.AddTransfer(amount, $"Merge from {source.FolioNumber}: {c.Description}", null, now, ctx.Actor.ActorId, ctx.BusinessDate.BusinessDate);
                    await repository.AddEntryAsync(sourceEntry, ct);
                    await repository.AddEntryAsync(targetEntry, ct);
                    transfers.Add(new FolioTransferResult(source.HotelFolioId, target.HotelFolioId, amount, MapEntry(sourceEntry), MapEntry(targetEntry)));
                }
                else
                {
                    var moves = new List<FolioTransferAllocationResult>();
                    foreach (var original in source.Entries.Where(x => x.EntryType != HotelFolioEntryType.ClosingBalance).ToList())
                    {
                        var sourceEntry = source.AddTransfer(-original.Amount, $"Merge khoản {original.HotelFolioEntryId:N} to {target.FolioNumber}: {c.Description}", original.HotelFolioEntryId, now, ctx.Actor.ActorId, ctx.BusinessDate.BusinessDate);
                        var targetEntry = target.AddTransfer(original.Amount, $"Merge khoản {original.HotelFolioEntryId:N} from {source.FolioNumber}: {c.Description}", original.HotelFolioEntryId, now, ctx.Actor.ActorId, ctx.BusinessDate.BusinessDate);
                        await repository.AddEntryAsync(sourceEntry, ct);
                        await repository.AddEntryAsync(targetEntry, ct);
                        moves.Add(new FolioTransferAllocationResult(original.HotelFolioEntryId, original.Amount, MapEntry(sourceEntry), MapEntry(targetEntry)));
                    }
                    if (moves.Count == 0) return Fail<FolioMergeResult>(Error.Business("FOLIO_MERGE_LEDGER_EMPTY", $"Source Folio {source.FolioNumber} has no mergeable ledger entries."));
                    transfers.Add(new FolioTransferResult(source.HotelFolioId, target.HotelFolioId, amount, moves[0].SourceEntry, moves[0].TargetEntry, moves));
                }
                source.Close(now, ctx.Actor.ActorId);
            }

            var commit = await persistence.CommitAsync(ct);
            if (commit.IsFailure) return Fail<FolioMergeResult>(Error.Concurrency("FOLIO_MERGE_CONCURRENCY_CONFLICT", "Concurrent Folio modification."));
            return Result<FolioMergeResult>.Success(new FolioMergeResult(target.HotelFolioId, transfers.Sum(x => x.Amount), transfers));
        }
        catch (InvalidOperationException ex) { return Fail<FolioMergeResult>(Error.Business("FOLIO_MERGE_FAILED", ex.Message)); }
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
        var reversal = folio.AddCorrection(-entry.Amount, $"Reversal: {c.Reason} (Entry #{entry.SequenceNumber})", entry.HotelFolioEntryId, now, ctx.Actor.ActorId, ctx.BusinessDate.BusinessDate);
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

    private static bool SupportedMergeRules(FolioMergeRules rules, out string unsupported)
    {
        var checks = new (string Name, string Value, string[] Supported)[] {
            ("transaction", rules.Transaction, new[] { "Keep", "Giữ nguyên ID", "Tạo merge reference" }),
            ("payer", rules.Payer, new[] { "Keep", "Giữ payer từng khoản" }),
            ("deposit", rules.Deposit, new[] { "Merge", "Gộp toàn bộ deposit", "Giữ theo nguồn" }),
            ("payment", rules.Payment, new[] { "Merge", "Gộp payment history", "Giữ receipt reference", "Giữ payment method" }),
            ("refund", rules.Refund, new[] { "Merge", "Gộp refund history", "Giữ refund reference" }),
            ("currency", rules.Currency, new[] { "Keep", "Giữ currency gốc" }),
            ("tax/package", rules.TaxPackage, new[] { "Keep", "Giữ nguyên từng khoản", "Giữ quan hệ gốc" }),
            ("document", rules.Document, new[] { "KeepRef", "Giữ reference nguồn" }),
            ("source post", rules.SourcePost, new[] { "MarkMerged", "Đánh dấu đã gộp", "Đóng Folio nguồn" })
        };
        foreach (var check in checks)
        {
            if (!check.Supported.Contains(check.Value, StringComparer.Ordinal))
            {
                unsupported = $"{check.Name} = '{check.Value}'";
                return false;
            }
        }
        unsupported = string.Empty;
        return true;
    }

    private async Task<Result<HotelFolioEntryDto>> AddEntry(Guid folioId, byte[] rowVersion, CancellationToken ct, string permission, Func<HotelFolio, ActorId?, DateOnly, HotelFolioEntry> add)
    {
        if (!authorization.HasPermission(permission)) return Fail<HotelFolioEntryDto>(Perm("add entry"));
        if (Context(out var ctx, out var pid) is { } e) return Fail<HotelFolioEntryDto>(e);
        if (rowVersion is not { Length: 8 }) return Fail<HotelFolioEntryDto>(Error.Validation("FOLIO_ROWVERSION_REQUIRED", "Row version required."));
        var folio = await repository.GetAsync(pid, folioId, ct);
        if (folio is null) return Fail<HotelFolioEntryDto>(Error.Business("FOLIO_NOT_FOUND", "Folio not found."));
        repository.ApplyExpectedRowVersion(folio, rowVersion);
        try
        {
            var entry = add(folio, ctx.Actor.ActorId, ctx.BusinessDate.BusinessDate);
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
