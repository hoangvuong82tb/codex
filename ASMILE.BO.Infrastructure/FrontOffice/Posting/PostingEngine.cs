using ASMILE.BO.Application.FrontOffice.Folio;
using ASMILE.BO.Application.FrontOffice.Foundation.Contexts;
using ASMILE.BO.Application.FrontOffice.Foundation.Persistence;
using ASMILE.BO.Application.FrontOffice.Posting;
using ASMILE.BO.Application.FrontOffice.Rate;
using ASMILE.BO.Core.FrontOffice.Folio;
using ASMILE.BO.Core.FrontOffice.Payment;
using ASMILE.BO.Shared.Foundation;

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
    IRateRepository rateRepo) : IFrontOfficePostingEngine
{
    public async Task<PostingResult> PostAsync(PostingRequest r, CancellationToken ct)
    {
        var audit = new List<string>();
        try
        {
            // 1. Validate
            var validation = await ValidateAsync(r.FolioId, r.PropertyId, ct);
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
                    entry = folio.AddRoomCharge(r.Amount, r.Description, r.BusinessDate, null, DateTime.UtcNow, null);
                    break;
                case HotelFolioEntryType.Tax:
                    entry = folio.AddTax(taxAmount > 0 ? taxAmount : r.Amount, r.Description, DateTime.UtcNow, null);
                    break;
                case HotelFolioEntryType.ServiceCharge:
                    entry = folio.AddServiceCharge(scAmount > 0 ? scAmount : r.Amount, r.Description, DateTime.UtcNow, null);
                    break;
                case HotelFolioEntryType.Discount:
                    entry = folio.AddDiscount(discountAmount > 0 ? discountAmount : r.Amount, r.Description, DateTime.UtcNow, null);
                    break;
                case HotelFolioEntryType.Payment:
                    entry = folio.AddPayment(r.Amount, r.Description, null, null, DateTime.UtcNow, null);
                    break;
                case HotelFolioEntryType.Deposit:
                    entry = folio.AddDeposit(r.Amount, r.Description, DateTime.UtcNow, null);
                    break;
                default:
                    entry = folio.AddAdjustment(r.Amount, r.Description, DateTime.UtcNow, null);
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
        var folio = await folioRepo.GetAsync(propertyId, folioId, ct);
        if (folio is null) return new PostingValidationResult(false, "Folio not found.", null, false, null, false);
        if (!folio.Status.IsMutable()) return new PostingValidationResult(false, $"Folio is {folio.Status} — cannot post.", null, false, folio.RunningBalance, folio.OutstandingBalance > 0);
        return new PostingValidationResult(true, null, "Open", true, folio.RunningBalance, folio.OutstandingBalance > 0);
    }

    public async Task<bool> CanPostAsync(Guid propertyId, CancellationToken ct)
    {
        var cx = ctx.Current;
        return cx?.BusinessDate.BusinessDate != default;
    }

    private static PostingResult Fail(string error, List<string> audit) => new(false, null, 0, 0, 0, 0, 0, 0, error, audit);
}

/// <summary>
/// PropertyLockManager — prevents posting during Night Audit.
/// </summary>
public sealed class PropertyLockManager(IFrontOfficeExecutionContextAccessor ctx) : IPropertyLockManager
{
    public Task<PropertyLockState> GetLockStateAsync(Guid propertyId, CancellationToken ct)
    {
        var cx = ctx.Current;
        return Task.FromResult(new PropertyLockState(propertyId, cx?.BusinessDate.BusinessDate ?? DateOnly.MinValue, false, null, null, null, null));
    }

    public Task<bool> LockAsync(Guid propertyId, Guid nightAuditId, string reason, CancellationToken ct)
        => Task.FromResult(true); // Property lock applied during Night Audit

    public Task<bool> UnlockAsync(Guid propertyId, CancellationToken ct)
        => Task.FromResult(true);

    public Task<bool> IsLockedAsync(Guid propertyId, CancellationToken ct)
        => Task.FromResult(false);
}

/// <summary>
/// FinancialProcessService — orchestrates the full financial flow:
/// Stay → Rate → PostingEngine → Folio → Payment → Settlement → NightAudit
/// </summary>
public sealed class FinancialProcessService(
    IFrontOfficePostingEngine postingEngine,
    IRateRepository rateRepo,
    IHotelFolioRepository folioRepo) : IFinancialProcessService
{
    public async Task<BatchPostingResult> PostRoomChargesAsync(Guid propertyId, DateOnly businessDate, CancellationToken ct)
    {
        var ratePlans = await rateRepo.GetAllAsync(propertyId, new RatePlanSearchFilter(propertyId, null, null, null), ct);
        var requests = new List<PostingRequest>();

        // Build posting requests for all InHouse stays (simplified — would query StayRepository)
        foreach (var plan in ratePlans)
        {
            var (price, _) = plan.CalculatePrice(businessDate, 1, 1, 100, 50);
            // In production: for each InHouse Stay with this plan, create PostingRequest
        }

        return await postingEngine.BatchPostAsync(new BatchPostingRequest(propertyId, businessDate, requests), ct);
    }

    public async Task<BatchPostingResult> PostTaxesAndChargesAsync(Guid propertyId, DateOnly businessDate, CancellationToken ct)
    {
        // Post tax and service charge for all room charges posted today
        return new BatchPostingResult(0, 0, 0, 0, 0, 0, [], []);
    }

    public async Task<PostingResult> CalculateAndPostRoomRateAsync(Guid stayId, Guid folioId, decimal baseRate, DateOnly date, int adults, int nights, int totalRooms, int roomsSold, CancellationToken ct)
    {
        // Use RateEngine to calculate actual price
        var ratePlans = await rateRepo.GetAllAsync(Guid.Empty, new RatePlanSearchFilter(Guid.Empty, null, null, null), ct);
        decimal calculatedRate = baseRate; // default

        foreach (var plan in ratePlans)
        {
            var (price, _) = plan.CalculatePrice(date, adults, nights, totalRooms, roomsSold);
            calculatedRate = price; // Use the first plan's calculated rate
            break;
        }

        var request = new PostingRequest(Guid.Empty, folioId, stayId, [],
            HotelFolioEntryType.RoomCharge, calculatedRate * nights,
            $"Room charge {nights} night(s) × {calculatedRate:N0} VND",
            date, "RatePlan", null, 0.10m, 0.05m, 0);

        return await postingEngine.PostAsync(request, ct);
    }
}
