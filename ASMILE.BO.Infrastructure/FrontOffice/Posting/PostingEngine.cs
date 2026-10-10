using ASMILE.BO.Application.FrontOffice.Folio;
using ASMILE.BO.Application.FrontOffice.Foundation.Contexts;
using ASMILE.BO.Application.FrontOffice.Foundation.Persistence;
using ASMILE.BO.Application.FrontOffice.HotelMasterData;
using ASMILE.BO.Application.FrontOffice.Posting;
using ASMILE.BO.Application.FrontOffice.Rate;
using ASMILE.BO.Application.FrontOffice.Stay;
using ASMILE.BO.Core.FrontOffice.Folio;
using ASMILE.BO.Core.FrontOffice.HotelMasterData;
using ASMILE.BO.Core.FrontOffice.NightAudit;
using ASMILE.BO.Core.FrontOffice.Payment;
using ASMILE.BO.Core.FrontOffice.Rate;
using ASMILE.BO.Shared.Foundation;
using ASMILE.BO.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ASMILE.BO.Infrastructure.FrontOffice.Posting;

/// <summary>
/// PostingEngine — SINGLE ENTRY POINT for all financial postings.
/// No module posts directly to Folio. All postings go through this engine.
/// Validates BusinessDate, Folio status, calculates tax/service charge/discount.
/// </summary>
public sealed class FrontOfficePostingEngine(
    IHotelFolioRepository folioRepo,
    IFrontOfficePersistenceSession persistence,
    IFrontOfficeExecutionContextAccessor ctx,
    IRateRepository rateRepo,
    IPropertyLockManager propertyLockManager) : IFrontOfficePostingEngine
{
    public async Task<PostingResult> PostAsync(PostingRequest r, CancellationToken ct)
    {
        // Retain the rate repository in the public constructor for compatibility
        // with existing composition roots; rate selection belongs to the
        // FinancialProcessService orchestration layer.
        _ = rateRepo;
        var audit = new List<string>();
        try
        {
            // 1. Validate
            var validation = await ValidateForPostingAsync(
                r.FolioId,
                r.PropertyId,
                r.ReferenceType == "NightAudit" ? r.ReferenceId : null,
                ct);
            if (!validation.IsValid) return Fail(validation.Error!, audit);

            // 2. Calculate tax, service charge, discount
            var taxAmount = r.TaxRate > 0 ? r.Amount * r.TaxRate : 0;
            var scAmount = r.ServiceChargeRate > 0 ? r.Amount * r.ServiceChargeRate : 0;
            var discountAmount = r.DiscountRate > 0 ? r.Amount * r.DiscountRate : 0;
            var netAmount = r.Amount + taxAmount + scAmount - discountAmount;
            audit.Add($"Posting: {r.EntryType} — Base: {r.Amount:N0}, Tax ({r.TaxRate:P0}): {taxAmount:N0}, SC ({r.ServiceChargeRate:P0}): {scAmount:N0}, Discount ({r.DiscountRate:P0}): {discountAmount:N0} → Net: {netAmount:N0}");

            // 3. Create Folio Entry
            var folio = await folioRepo.GetAsync(r.PropertyId, r.FolioId, ct);
            if (folio is null) return Fail("Folio not found.", audit);
            folioRepo.ApplyExpectedRowVersion(folio, r.FolioRowVersion);

            HotelFolioEntry entry;
            switch (r.EntryType)
            {
                case HotelFolioEntryType.RoomCharge:
                    entry = folio.AddRoomCharge(r.Amount, r.Description, r.BusinessDate, r.RatePlanId, DateTime.UtcNow, null);
                    break;
                case HotelFolioEntryType.Tax:
                    entry = folio.AddTax(taxAmount > 0 ? taxAmount : r.Amount, r.Description, DateTime.UtcNow, null, r.BusinessDate);
                    break;
                case HotelFolioEntryType.ServiceCharge:
                    entry = folio.AddServiceCharge(scAmount > 0 ? scAmount : r.Amount, r.Description, DateTime.UtcNow, null, r.BusinessDate);
                    break;
                case HotelFolioEntryType.Discount:
                    entry = folio.AddDiscount(discountAmount > 0 ? discountAmount : r.Amount, r.Description, DateTime.UtcNow, null, r.BusinessDate);
                    break;
                case HotelFolioEntryType.Payment:
                    entry = folio.AddPayment(r.Amount, r.Description, null, null, DateTime.UtcNow, null, r.BusinessDate);
                    break;
                case HotelFolioEntryType.Deposit:
                    entry = folio.AddDeposit(r.Amount, r.Description, DateTime.UtcNow, null, r.BusinessDate);
                    break;
                default:
                    entry = folio.AddAdjustment(r.Amount, r.Description, DateTime.UtcNow, null, r.BusinessDate);
                    break;
            }

            await folioRepo.AddEntryAsync(entry, ct);
            await persistence.CommitAsync(ct);
            audit.Add($"Folio entry #{entry.SequenceNumber} created: {entry.EntryType} {entry.Amount:N0} VND.");

            return new PostingResult(true, entry.HotelFolioEntryId, entry.SequenceNumber, r.Amount, taxAmount, scAmount, discountAmount, netAmount, null, audit);
        }
        catch (InvalidOperationException ex) { return Fail(ex.Message, audit); }
        catch (ArgumentException ex) { return Fail(ex.Message, audit); }
    }

    public async Task<BatchPostingResult> BatchPostAsync(BatchPostingRequest request, CancellationToken ct)
    {
        var results = new List<PostingResult>();
        var warnings = new List<string>();
        int posted = 0, failed = 0;
        decimal totalAmount = 0, totalTax = 0, totalSC = 0;

        foreach (var posting in request.Postings)
        {
            var result = await PostAsync(posting, ct);
            results.Add(result);
            if (result.Success) { posted++; totalAmount += result.BaseAmount; totalTax += result.TaxAmount; totalSC += result.ServiceChargeAmount; }
            else { failed++; warnings.Add($"Posting failed: {result.Error}"); }
        }

        return new BatchPostingResult(request.Postings.Count, posted, failed, totalAmount, totalTax, totalSC, results, warnings);
    }

    public async Task<PostingValidationResult> ValidateAsync(Guid folioId, Guid propertyId, CancellationToken ct)
    {
        return await ValidateForPostingAsync(folioId, propertyId, null, ct);
    }

    private async Task<PostingValidationResult> ValidateForPostingAsync(
        Guid folioId,
        Guid propertyId,
        Guid? requestingNightAuditId,
        CancellationToken ct)
    {
        var lockState = await propertyLockManager.GetLockStateAsync(propertyId, ct);
        if (lockState.IsLocked && lockState.NightAuditId != requestingNightAuditId)
        {
            return new PostingValidationResult(
                false,
                "Property is locked — Night Audit is in progress.",
                "Closing",
                false,
                null,
                false);
        }

        var folio = await folioRepo.GetAsync(propertyId, folioId, ct);
        if (folio is null) return new PostingValidationResult(false, "Folio not found.", null, false, null, false);
        if (!folio.Status.IsMutable()) return new PostingValidationResult(false, $"Folio is {folio.Status} — cannot post.", null, false, folio.RunningBalance, folio.OutstandingBalance > 0);
        return new PostingValidationResult(true, null, "Open", true, folio.RunningBalance, folio.OutstandingBalance > 0);
    }

    public async Task<bool> CanPostAsync(Guid propertyId, CancellationToken ct)
    {
        var cx = ctx.Current;
        if (cx?.BusinessDate.BusinessDate == default)
            return false;

        return !await propertyLockManager.IsLockedAsync(propertyId, ct);
    }

    private static PostingResult Fail(string error, List<string> audit) => new(false, null, 0, 0, 0, 0, 0, 0, error, audit);
}

/// <summary>
/// PropertyLockManager — prevents posting during Night Audit.
/// </summary>
public sealed class PropertyLockManager(
    AppDbContext db,
    IFrontOfficeExecutionContextAccessor ctx) : IPropertyLockManager
{
    public async Task<PropertyLockState> GetLockStateAsync(Guid propertyId, CancellationToken ct)
    {
        var record = await FindCurrentBusinessDateAsync(propertyId, asNoTracking: true, ct);
        var businessDate = record?.BusinessDate ?? ctx.Current?.BusinessDate.BusinessDate ?? DateOnly.MinValue;
        var isLocked = record?.Status == BusinessDateStatus.Closing;
        return new PropertyLockState(
            propertyId,
            businessDate,
            isLocked,
            isLocked ? record?.NightAuditId : null,
            null,
            null,
            isLocked ? "Night Audit" : null);
    }

    public async Task<bool> LockAsync(Guid propertyId, Guid nightAuditId, string reason, CancellationToken ct)
    {
        var record = await FindCurrentBusinessDateAsync(propertyId, asNoTracking: false, ct);
        if (record is null)
            return false;

        if (record.Status == BusinessDateStatus.Closing)
            return record.NightAuditId == nightAuditId;

        if (record.Status != BusinessDateStatus.Open)
            return false;

        record.BeginClosing(nightAuditId, DateTime.UtcNow);
        await db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> UnlockAsync(Guid propertyId, CancellationToken ct)
    {
        var record = await FindCurrentBusinessDateAsync(propertyId, asNoTracking: false, ct);
        if (record is null || record.Status != BusinessDateStatus.Closing)
            return true;

        record.Reopen();
        await db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> IsLockedAsync(Guid propertyId, CancellationToken ct)
    {
        var record = await FindCurrentBusinessDateAsync(propertyId, asNoTracking: true, ct);
        return record?.Status == BusinessDateStatus.Closing;
    }

    private async Task<BusinessDateRecord?> FindCurrentBusinessDateAsync(Guid propertyId, bool asNoTracking, CancellationToken ct)
    {
        var query = db.Set<BusinessDateRecord>()
            .Where(x => x.PropertyId == propertyId);
        var currentDate = ctx.Current?.BusinessDate.BusinessDate;
        if (currentDate is not null && currentDate.Value != DateOnly.MinValue)
            query = query.Where(x => x.BusinessDate == currentDate.Value);

        if (asNoTracking)
            query = query.AsNoTracking();

        return await query
            .OrderByDescending(x => x.BusinessDate)
            .FirstOrDefaultAsync(ct);
    }
}

/// <summary>
/// FinancialProcessService — orchestrates the full financial flow:
/// Stay → Rate → PostingEngine → Folio → Payment → Settlement → NightAudit
/// </summary>
public sealed class FinancialProcessService(
    IFrontOfficePostingEngine postingEngine,
    IRateRepository rateRepo,
    IHotelStayRepository stayRepo,
    IHotelFolioRepository folioRepo,
    IHotelStructureMasterDataRepository masterDataRepo,
    IFrontOfficeExecutionContextAccessor executionContext) : IFinancialProcessService
{
    public async Task<BatchPostingResult> PostRoomChargesAsync(Guid propertyId, DateOnly businessDate, CancellationToken ct)
    {
        var stays = await stayRepo.GetCurrentInHouseAsync(propertyId, ct);
        var ratePlans = await rateRepo.GetAllAsync(propertyId, new RatePlanSearchFilter(propertyId, RatePlanStatus.Active, null, null), ct);
        var requests = new List<PostingRequest>();
        var description = $"Financial process room charge {businessDate}";

        foreach (var stay in stays.Where(x => x.ScheduledArrivalDate <= businessDate && x.ScheduledDepartureDate > businessDate))
        {
            var folio = await folioRepo.GetByStayAsync(propertyId, stay.HotelStayId, ct)
                ?? throw new InvalidOperationException($"Folio not found for stay {stay.StayNumber}.");
            if (folio.RowVersion is not { Length: 8 })
                throw new InvalidOperationException($"Folio row version is missing for stay {stay.StayNumber}.");

            var ledger = await folioRepo.GetLedgerAsync(propertyId, folio.HotelFolioId, ct);
            if (ledger?.Entries.Any(x => x.EntryType == HotelFolioEntryType.RoomCharge
                && x.BusinessDate == businessDate && x.Description == description) == true)
                continue;

            var plan = SelectRatePlan(ratePlans, stay.RoomTypeId, businessDate)
                ?? throw new InvalidOperationException($"No active rate plan for room type {stay.RoomTypeCode} on {businessDate}.");
            var (price, _) = plan.CalculatePrice(businessDate, Math.Max(stay.Adults, 1), 1, 0, 0);
            if (price <= 0)
                throw new InvalidOperationException($"Rate plan {plan.PlanCode} returned an invalid room rate for stay {stay.StayNumber}.");

            requests.Add(new PostingRequest(
                propertyId, folio.HotelFolioId, stay.HotelStayId, folio.RowVersion,
                HotelFolioEntryType.RoomCharge, price, description, businessDate,
                "FinancialProcess", null, RatePlanId: plan.RatePlanId));
        }

        return await postingEngine.BatchPostAsync(new BatchPostingRequest(propertyId, businessDate, requests), ct);
    }

    public async Task<BatchPostingResult> PostTaxesAndChargesAsync(Guid propertyId, DateOnly businessDate, CancellationToken ct)
    {
        var stays = await stayRepo.GetCurrentInHouseAsync(propertyId, ct);
        var results = new List<PostingResult>();
        var warnings = new List<string>();
        decimal totalAmount = 0, totalTax = 0, totalServiceCharge = 0;

        foreach (var stay in stays.Where(x => x.ScheduledArrivalDate <= businessDate && x.ScheduledDepartureDate > businessDate))
        {
            var folio = await folioRepo.GetByStayAsync(propertyId, stay.HotelStayId, ct)
                ?? throw new InvalidOperationException($"Folio not found for stay {stay.StayNumber}.");
            var ledger = await folioRepo.GetLedgerAsync(propertyId, folio.HotelFolioId, ct);
            var roomCharge = ledger?.Entries
                .Where(x => x.EntryType == HotelFolioEntryType.RoomCharge && x.BusinessDate == businessDate)
                .Sum(x => x.Amount) ?? 0;
            if (roomCharge <= 0) continue;

            var roomChargeEntry = ledger?.Entries
                .Where(x => x.EntryType == HotelFolioEntryType.RoomCharge && x.BusinessDate == businessDate)
                .OrderBy(x => x.SequenceNumber)
                .FirstOrDefault();
            var ratePlanCode = roomChargeEntry?.ReferenceId is { } ratePlanId
                ? (await rateRepo.GetAsync(ratePlanId, propertyId, ct))?.PlanCode
                : null;
            var rules = await masterDataRepo.GetEffectiveTaxServiceChargeRulesAsync(
                propertyId, businessDate, "ROOM", ratePlanCode, ct);

            var taxRule = rules.FirstOrDefault(x => x.Kind == HotelTaxServiceChargeKind.Tax);
            var serviceChargeRule = rules.FirstOrDefault(x => x.Kind == HotelTaxServiceChargeKind.ServiceCharge);
            if (taxRule is null)
                warnings.Add($"No active tax rule for room charges at property {propertyId} on {businessDate}.");
            else
                await PostDerivedAsync(HotelFolioEntryType.Tax, taxRule, "tax", roomCharge);

            if (serviceChargeRule is null)
                warnings.Add($"No active service-charge rule for room charges at property {propertyId} on {businessDate}.");
            else
                await PostDerivedAsync(HotelFolioEntryType.ServiceCharge, serviceChargeRule, "service charge", roomCharge);

            async Task PostDerivedAsync(HotelFolioEntryType entryType, HotelTaxServiceChargeRuleDto rule, string label, decimal sourceAmount)
            {
                var description = $"Financial process {label} {businessDate}";
                if (ledger?.Entries.Any(x => x.EntryType == entryType
                    && x.BusinessDate == businessDate && x.Description == description) == true)
                    return;

                // Refresh after every posting so two entries on the same folio do not
                // reuse a stale optimistic-concurrency token.
                var currentFolio = await folioRepo.GetByStayAsync(propertyId, stay.HotelStayId, ct)
                    ?? throw new InvalidOperationException($"Folio not found for stay {stay.StayNumber}.");
                if (currentFolio.RowVersion is not { Length: 8 })
                    throw new InvalidOperationException($"Folio row version is missing for stay {stay.StayNumber}.");

                var amount = CalculateDerivedAmount(sourceAmount, rule);
                if (amount <= 0)
                {
                    warnings.Add($"Skipped zero-value {label} for {stay.StayNumber}: rule {rule.Code}.");
                    return;
                }
                var result = await postingEngine.PostAsync(new PostingRequest(
                    propertyId, currentFolio.HotelFolioId, stay.HotelStayId, currentFolio.RowVersion,
                    entryType, amount, description, businessDate, "FinancialProcess", null), ct);
                results.Add(result);
                if (!result.Success)
                {
                    warnings.Add($"Posting {label} failed for {stay.StayNumber}: {result.Error}");
                    return;
                }

                totalAmount += result.BaseAmount;
                if (entryType == HotelFolioEntryType.Tax)
                    totalTax += result.BaseAmount;
                else if (entryType == HotelFolioEntryType.ServiceCharge)
                    totalServiceCharge += result.BaseAmount;
            }
        }

        return new BatchPostingResult(results.Count, results.Count(x => x.Success), results.Count(x => !x.Success), totalAmount, totalTax, totalServiceCharge, results, warnings);
    }

    private static decimal CalculateDerivedAmount(decimal sourceAmount, HotelTaxServiceChargeRuleDto rule)
    {
        var rawAmount = rule.IsInclusive && rule.Rate > 0
            ? sourceAmount * rule.Rate / (100m + rule.Rate)
            : sourceAmount * rule.Rate / 100m;

        return rule.RoundingMode.Trim().ToUpperInvariant() switch
        {
            "FLOOR" => Math.Floor(rawAmount),
            "CEILING" or "CEIL" => Math.Ceiling(rawAmount),
            "NONE" or "NOROUNDING" or "EXACT" => rawAmount,
            _ => Math.Round(rawAmount, 0, MidpointRounding.AwayFromZero)
        };
    }

    public async Task<PostingResult> CalculateAndPostRoomRateAsync(Guid stayId, Guid folioId, decimal baseRate, DateOnly date, int adults, int nights, int totalRooms, int roomsSold, CancellationToken ct)
    {
        // Kept in the contract for backwards compatibility. The persisted
        // rate plan is now the only source of truth for the amount.
        _ = baseRate;
        var propertyId = executionContext.Current?.Property.PropertyId.Value;
        if (propertyId is not Guid currentPropertyId || currentPropertyId == Guid.Empty)
            return new PostingResult(false, null, 0, 0, 0, 0, 0, 0, "Property context is required.", []);

        var stay = await stayRepo.GetDtoAsync(currentPropertyId, stayId, ct);
        if (stay is null)
            return new PostingResult(false, null, 0, 0, 0, 0, 0, 0, "Stay not found.", []);

        // Use the stay's property scope. Guid.Empty must never silently fall
        // back to a UI-provided base rate and post an ungoverned amount.
        var ratePlans = await rateRepo.GetAllAsync(currentPropertyId, new RatePlanSearchFilter(currentPropertyId, RatePlanStatus.Active, null, null), ct);
        var plan = SelectRatePlan(ratePlans, stay.RoomTypeId, date);
        if (plan is null)
            return new PostingResult(
                false, null, 0, 0, 0, 0, 0, 0,
                $"No active rate plan for room type {stay.RoomTypeCode} on {date}. Configure a real rate plan before posting room charges.",
                []);

        if (date < DateOnly.FromDateTime(plan.EffectiveFrom)
            || date.AddDays(Math.Max(nights, 1) - 1) > DateOnly.FromDateTime(plan.EffectiveTo))
        {
            return new PostingResult(
                false, null, 0, 0, 0, 0, 0, 0,
                $"Rate plan {plan.PlanCode} does not cover the complete stay period {date:yyyy-MM-dd} + {nights} night(s).",
                []);
        }

        var (calculatedTotal, _) = plan.CalculateStayPrice(date, adults, nights, totalRooms, roomsSold);

        var folio = await folioRepo.GetByStayAsync(currentPropertyId, stayId, ct);
        if (folio is null || folio.HotelFolioId != folioId || folio.RowVersion is not { Length: 8 })
            return new PostingResult(false, null, 0, 0, 0, 0, 0, 0, "Folio not found or row version is missing.", []);
        if (calculatedTotal <= 0 || nights <= 0)
            return new PostingResult(false, null, 0, 0, 0, 0, 0, 0, "Room rate and nights must be greater than zero.", []);

        var request = new PostingRequest(currentPropertyId, folioId, stayId, folio.RowVersion,
            HotelFolioEntryType.RoomCharge, calculatedTotal,
            $"Room charge {nights} night(s) × rate plan {plan.PlanCode} total {calculatedTotal:N0} VND",
            date, "RatePlan", plan.RatePlanId, 0, 0, 0, RatePlanId: plan.RatePlanId);

        return await postingEngine.PostAsync(request, ct);
    }

    private static RatePlan? SelectRatePlan(IReadOnlyList<RatePlan> plans, Guid roomTypeId, DateOnly date)
        => plans
            // A plan scoped to the exact room type wins over a property-wide
            // plan. Property-wide BAR plans are valid fallback configuration,
            // but only after an exact room-type plan has been considered.
            .Where(x => x.RoomTypeId == roomTypeId || x.RoomTypeId is null)
            .Where(x => DateOnly.FromDateTime(x.EffectiveFrom) <= date && DateOnly.FromDateTime(x.EffectiveTo) >= date)
            .OrderBy(x => x.RoomTypeId == roomTypeId ? 0 : 1)
            .ThenBy(x => x.Hierarchy)
            .ThenBy(x => x.PlanCode)
            .FirstOrDefault();
}
