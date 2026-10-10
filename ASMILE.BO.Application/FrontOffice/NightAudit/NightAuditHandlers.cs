using System.Text.Json;
using ASMILE.BO.Application.FrontOffice.Folio;
using ASMILE.BO.Application.FrontOffice.Foundation.Contexts;
using ASMILE.BO.Application.FrontOffice.Foundation.Cqrs;
using ASMILE.BO.Application.FrontOffice.Foundation.Persistence;
using ASMILE.BO.Application.FrontOffice.HotelMasterData;
using ASMILE.BO.Application.FrontOffice.Posting;
using ASMILE.BO.Application.FrontOffice.Rate;
using ASMILE.BO.Application.FrontOffice.Stay;
using ASMILE.BO.Application.Interfaces;
using ASMILE.BO.Core.FrontOffice.Folio;
using ASMILE.BO.Core.FrontOffice.Foundation.Identifiers;
using ASMILE.BO.Core.FrontOffice.HotelMasterData;
using ASMILE.BO.Core.FrontOffice.NightAudit;
using ASMILE.BO.Core.FrontOffice.Rate;
using ASMILE.BO.Core.FrontOffice.Reservation;
using ASMILE.BO.Core.FrontOffice.Stay;
using ASMILE.BO.Shared.Foundation;
using NightAuditAggregate = ASMILE.BO.Core.FrontOffice.NightAudit.NightAudit;

namespace ASMILE.BO.Application.FrontOffice.NightAudit;

// === DTOs ===
public sealed record NightAuditDto(Guid NightAuditId, string AuditNumber, DateOnly BusinessDate, NightAuditStatus Status, NightAuditStep CurrentStep, string? ErrorMessage, DateTime StartedAtUtc, DateTime? CompletedAtUtc, int CompletedSteps, int TotalSteps, byte[]? RowVersion);
public sealed record BusinessDateDto(Guid BusinessDateId, DateOnly BusinessDate, BusinessDateStatus Status, DateTime OpenedAtUtc, DateTime? ClosedAtUtc, byte[]? RowVersion);
public sealed record RevenueSnapshotDto(Guid RevenueSnapshotId, DateOnly BusinessDate, decimal RoomRevenue, decimal OtherRevenue, decimal TotalRevenue, decimal TotalTax, decimal TotalPayments, decimal TotalRefunds, decimal OutstandingBalance, int RoomsSold, int TotalRooms, decimal OccupancyRate, decimal ADR, decimal RevPAR, int Arrivals, int Departures, int NoShows, int Cancellations);
public sealed record NightAuditSummaryDto(NightAuditDto Audit, IReadOnlyList<NightAuditStepDto> Steps, RevenueSnapshotDto? Revenue, int ValidationFailures);

public sealed record NightAuditStepDto(NightAuditStep Step, NightAuditStepStatus Status, string? Detail, DateTime? CompletedAtUtc);

public sealed record NightAuditOccupancyMetrics(int RoomsSold, int TotalRooms, decimal OccupancyRate);

public static class NightAuditMetrics
{
    public static NightAuditOccupancyMetrics CalculateOccupancy(IEnumerable<RoomOccupancyDto> rooms)
    {
        ArgumentNullException.ThrowIfNull(rooms);
        var inventory = rooms as IReadOnlyCollection<RoomOccupancyDto> ?? rooms.ToArray();
        var totalRooms = inventory.Count;
        var roomsSold = inventory.Count(x => string.Equals(x.OccupancyStatus, "Occupied", StringComparison.OrdinalIgnoreCase));
        var occupancyRate = totalRooms == 0 ? 0 : (decimal)roomsSold / totalRooms * 100;
        return new NightAuditOccupancyMetrics(roomsSold, totalRooms, occupancyRate);
    }
}

// === Commands ===
public sealed record BeginNightAuditCommand(Guid PropertyId) : ICommand<NightAuditDto>;
public sealed record ExecuteNightAuditStepCommand(Guid NightAuditId, NightAuditStep Step, string? Detail = null) : ICommand<NightAuditDto>;
public sealed record FailNightAuditCommand(Guid NightAuditId, string Error) : ICommand<NightAuditDto>;
public sealed record CancelNightAuditCommand(Guid NightAuditId, string Reason) : ICommand<NightAuditDto>;
public sealed record ResumeNightAuditCommand(Guid NightAuditId) : ICommand<NightAuditDto>;
public sealed record GetNightAuditSummaryCommand(Guid NightAuditId) : ICommand<NightAuditSummaryDto>;

// === Queries ===
public sealed record GetCurrentNightAuditQuery(Guid PropertyId) : IQuery<NightAuditDto?>;
public sealed record GetCurrentBusinessDateQuery(Guid PropertyId) : IQuery<BusinessDateDto?>;
public sealed record GetRevenueSnapshotQuery(Guid PropertyId, DateOnly Date) : IQuery<RevenueSnapshotDto?>;

// === Repository ===
public interface INightAuditRepository
{
    Task<NightAuditAggregate?> GetAsync(Guid propertyId, Guid auditId, CancellationToken ct);
    Task AddAsync(NightAuditAggregate audit, CancellationToken ct);
    Task AddRevenueSnapshotAsync(RevenueSnapshot snapshot, CancellationToken ct);
    Task AddBusinessDateAsync(BusinessDateRecord record, CancellationToken ct);
    Task<NightAuditDto?> GetCurrentAsync(Guid propertyId, CancellationToken ct);
    Task<BusinessDateRecord?> GetBusinessDateAsync(Guid propertyId, DateOnly date, CancellationToken ct);
    Task<BusinessDateDto?> GetCurrentBusinessDateAsync(Guid propertyId, CancellationToken ct);
    Task<RevenueSnapshotDto?> GetRevenueSnapshotAsync(Guid propertyId, DateOnly date, CancellationToken ct);
    void ApplyExpectedRowVersion(NightAuditAggregate audit, byte[] rowVersion);
}

/// <summary>
/// Reads the canonical folio ledger for the Night Audit revenue snapshot.
/// Keeping this behind an application contract prevents the workflow from
/// silently presenting zeroes when financial data exists in the database.
/// </summary>
public sealed record NightAuditFinancialMetrics(
    decimal RoomRevenue,
    decimal OtherRevenue,
    decimal ServiceCharge,
    decimal TotalTax,
    decimal TotalPayments,
    decimal TotalRefunds,
    decimal OutstandingBalance,
    int RoomNightsSold);

public interface INightAuditFinancialReader
{
    Task<NightAuditFinancialMetrics> GetAsync(Guid propertyId, DateOnly businessDate, CancellationToken ct);
}

/// <summary>
/// Reads reservation outcomes for the business-date revenue snapshot.
/// The counts are scoped to the property's arrivals for that date so a
/// reservation from another property or another arrival day cannot leak into
/// Night Audit KPIs.
/// </summary>
public sealed record NightAuditReservationMetrics(int NoShows, int Cancellations);

public interface INightAuditReservationMetricsReader
{
    Task<NightAuditReservationMetrics> GetAsync(Guid propertyId, DateOnly businessDate, CancellationToken ct);
}

public interface INightAuditNumberGenerator { Task<string> GenerateAsync(Guid propertyId, DateOnly businessDate, CancellationToken ct = default); }

// =============================================================================
// APPLICATION SERVICE
// =============================================================================
public sealed class NightAuditApplicationService(
    INightAuditRepository repo, IHotelStayRepository stayRepo, IFrontOfficePersistenceSession persistence,
    IFrontOfficeExecutionContextAccessor ctx, IAuthorizationService auth, IClock clock,
    INightAuditNumberGenerator numberGen, IFrontOfficePostingEngine postingEngine, IPropertyLockManager lockManager,
    IHotelFolioRepository folioRepo, IRateRepository rateRepo, IHotelStayOccupancyReader occupancyReader,
    INightAuditFinancialReader financialReader, INightAuditReservationMetricsReader reservationMetricsReader,
    IHotelStructureMasterDataRepository structureRepo)
{
    private static readonly Error PD = Error.Authorization("NIGHT_AUDIT_PERMISSION_DENIED", "");
    private Error? Context(out Guid pid) { var c = ctx.Current; pid = c?.Property.PropertyId.Value ?? Guid.Empty; return c is null ? PD : null; }
    static Result<T> F<T>(Error e) => Result<T>.Failure(e);

    // =============================================================================
    // BEGIN
    // =============================================================================
    public async Task<Result<NightAuditDto>> BeginAsync(BeginNightAuditCommand c, CancellationToken ct)
    {
        if (!auth.HasPermission("FrontOffice.NightAudit.Execute")) return F<NightAuditDto>(PD);
        if (Context(out var pid) is { } e) return F<NightAuditDto>(e);
        if (c.PropertyId != pid)
            return F<NightAuditDto>(Error.Authorization("NIGHT_AUDIT_PROPERTY_SCOPE_MISMATCH", "Night Audit property is outside the current execution scope."));
        var existing = await repo.GetCurrentAsync(c.PropertyId, ct);
        if (existing is not null) return F<NightAuditDto>(Error.Business("NIGHT_AUDIT_ALREADY_RUNNING", "Another audit is in progress."));
        var cx = ctx.Current!;
        var date = cx.BusinessDate.BusinessDate;
        var number = await numberGen.GenerateAsync(c.PropertyId, date, ct);
        var audit = NightAuditAggregate.Begin(c.PropertyId, date, number, clock.UtcNow, cx.Actor.ActorId);
        await repo.AddAsync(audit, ct);
        await persistence.CommitAsync(ct);
        return Result<NightAuditDto>.Success(Map(audit));
    }

    // =============================================================================
    // EXECUTE STEP — Real pipeline logic
    // =============================================================================
    public async Task<Result<NightAuditDto>> ExecuteStepAsync(ExecuteNightAuditStepCommand c, CancellationToken ct)
    {
        if (!auth.HasPermission("FrontOffice.NightAudit.Execute")) return F<NightAuditDto>(PD);
        if (Context(out var pid) is { } e) return F<NightAuditDto>(e);
        var audit = await repo.GetAsync(pid, c.NightAuditId, ct);
        if (audit is null) return F<NightAuditDto>(Error.Business("NIGHT_AUDIT_NOT_FOUND", ""));
        if (audit.Status != NightAuditStatus.Running) return F<NightAuditDto>(Error.Business("NIGHT_AUDIT_NOT_RUNNING", ""));

        // Idempotent — skip already completed steps
        if (audit.Steps.Any(s => s.Step == c.Step && s.Status == NightAuditStepStatus.Completed))
            return Result<NightAuditDto>.Success(Map(audit));

        var now = clock.UtcNow;
        try
        {
            var detail = await ProcessStepAsync(audit, c.Step, now, ct);
            audit.CompleteStep(c.Step, detail ?? c.Detail, now);
            var commit = await persistence.CommitAsync(ct);
            if (commit.IsFailure) return F<NightAuditDto>(commit.Error!);
            return Result<NightAuditDto>.Success(Map(audit));
        }
        catch (Exception ex)
        {
            audit.FailStep(c.Step, ex.Message, now);
            await persistence.CommitAsync(ct);
            return F<NightAuditDto>(Error.System("NIGHT_AUDIT_STEP_FAILED", ex.Message));
        }
    }

    // =============================================================================
    // PIPELINE STEPS — REAL BUSINESS LOGIC
    // =============================================================================
    private async Task<string?> ProcessStepAsync(NightAuditAggregate audit, NightAuditStep step, DateTime nowUtc, CancellationToken ct)
    {
        var pid = audit.PropertyId;
        var date = audit.BusinessDate;

        return step switch
        {
            NightAuditStep.LockBusinessDate => await LockBusinessDateAsync(audit, nowUtc, ct),
            NightAuditStep.ValidateOpenStays => await ValidateOpenStaysAsync(audit, ct),
            NightAuditStep.ValidateOpenFolios => await ValidateOpenFoliosAsync(audit, ct),
            NightAuditStep.PostRoomCharges => await PostRoomChargesAsync(audit, nowUtc, ct),
            NightAuditStep.PostTaxes => await PostTaxesAsync(audit, ct),
            NightAuditStep.PostServiceCharges => await PostServiceChargesAsync(audit, ct),
            NightAuditStep.ApplyPackages => await ApplyPackagesAsync(audit, ct),
            NightAuditStep.SettlementValidation => await ValidateSettlementsAsync(audit, ct),
            NightAuditStep.UpdateExpectedDepartures => await UpdateExpectedDeparturesAsync(audit, nowUtc, ct),
            NightAuditStep.GenerateRevenueSnapshot => await GenerateSnapshotAsync(audit, nowUtc, ct),
            NightAuditStep.RollBusinessDate => await RollBusinessDateAsync(audit, nowUtc, ct),
            NightAuditStep.FinalizeAndUnlock => await FinalizeAsync(audit, ct),
            _ => null
        };
    }

    // === STEP 1: Lock Business Date ===
    private async Task<string?> LockBusinessDateAsync(NightAuditAggregate audit, DateTime nowUtc, CancellationToken ct)
    {
        var bd = await repo.GetBusinessDateAsync(audit.PropertyId, audit.BusinessDate, ct);
        if (bd is null)
        {
            bd = BusinessDateRecord.Open(audit.PropertyId, audit.BusinessDate, nowUtc, ctx.Current?.Actor.ActorId);
            bd.BeginClosing(audit.NightAuditId, nowUtc);
            await repo.AddBusinessDateAsync(bd, ct);
            return $"BusinessDate {audit.BusinessDate} created and locked for audit.";
        }
        if (bd.Status == BusinessDateStatus.Closed) return "BusinessDate already closed.";
        bd.BeginClosing(audit.NightAuditId, nowUtc);
        return $"BusinessDate {audit.BusinessDate} locked for audit.";
    }

    // === STEP 2: Validate Open Stays ===
    private async Task<string?> ValidateOpenStaysAsync(NightAuditAggregate audit, CancellationToken ct)
    {
        var inHouse = await stayRepo.GetCurrentInHouseAsync(audit.PropertyId, ct);
        foreach (var stay in inHouse)
        {
            if (stay.ScheduledDepartureDate <= audit.BusinessDate)
            {
                audit.AddValidation(new AuditValidationResult(false, "StayPastDeparture", $"Stay {stay.StayNumber} ({stay.GuestName}) in room {stay.RoomNumber} is past scheduled departure {stay.ScheduledDepartureDate}.", "Error"));
            }
            if (stay.ActualCheckInAtUtc is null)
            {
                audit.AddValidation(new AuditValidationResult(false, "StayNotCheckedIn", $"Stay {stay.StayNumber} ({stay.GuestName}) has no check-in time — may be stale.", "Warning"));
            }
        }
        audit.AddValidation(new AuditValidationResult(true, "OpenStaysCount", $"{inHouse.Count} in-house stays.", "Info"));
        return $"{inHouse.Count} in-house stays. {audit.ValidationFailures} validation failures.";
    }

    // === STEP 3: Validate Open Folios ===
    private async Task<string?> ValidateOpenFoliosAsync(NightAuditAggregate audit, CancellationToken ct)
    {
        var stays = await stayRepo.GetCurrentInHouseAsync(audit.PropertyId, ct);
        var validatedCount = 0;
        var missingCount = 0;
        var nonOpenCount = 0;
        decimal outstandingBalance = 0;
        foreach (var stay in stays)
        {
            var folio = await folioRepo.GetByStayAsync(audit.PropertyId, stay.HotelStayId, ct);
            if (folio is null)
            {
                missingCount++;
                audit.AddValidation(new AuditValidationResult(false, "MissingFolio", $"Stay {stay.StayNumber} has no folio.", "Error"));
                continue;
            }

            var ledger = await folioRepo.GetLedgerAsync(audit.PropertyId, folio.HotelFolioId, ct);
            if (ledger is null)
            {
                missingCount++;
                audit.AddValidation(new AuditValidationResult(false, "MissingFolioLedger", $"Folio {folio.FolioNumber} for stay {stay.StayNumber} has no ledger.", "Error"));
                continue;
            }

            validatedCount++;
            outstandingBalance += Math.Max(ledger.RunningBalance, 0);
            if (ledger.Status is HotelFolioStatus.Closed or HotelFolioStatus.Settled or HotelFolioStatus.Archived)
            {
                nonOpenCount++;
                audit.AddValidation(new AuditValidationResult(false, "InHouseFolioNotOpen", $"Folio {ledger.FolioNumber} for stay {stay.StayNumber} is {ledger.Status}.", "Error"));
            }
        }

        audit.AddValidation(new AuditValidationResult(
            missingCount == 0 && nonOpenCount == 0,
            "OpenFolios",
            $"Validated {validatedCount}/{stays.Count} in-house folios; outstanding balance: {outstandingBalance:N0} VND.",
            missingCount == 0 && nonOpenCount == 0 ? "Info" : "Error"));
        return $"{validatedCount}/{stays.Count} folios validated. {missingCount + nonOpenCount} exceptions.";
    }

    // === STEP 4: Post Room Charges (via PostingEngine and the active rate plan) ===
    private async Task<string?> PostRoomChargesAsync(NightAuditAggregate audit, DateTime nowUtc, CancellationToken ct)
    {
        // Lock property to prevent new postings during audit
        if (!await lockManager.LockAsync(audit.PropertyId, audit.NightAuditId, "Night Audit room charge posting", ct))
            throw new InvalidOperationException("Property could not be locked for Night Audit posting.");

        var stays = await stayRepo.GetCurrentInHouseAsync(audit.PropertyId, ct);
        var ratePlans = await rateRepo.GetAllAsync(
            audit.PropertyId,
            new RatePlanSearchFilter(audit.PropertyId, RatePlanStatus.Active, null, null),
            ct);
        int posted = 0;
        decimal totalRevenue = 0;

        foreach (var stay in stays)
        {
            var folio = await folioRepo.GetByStayAsync(audit.PropertyId, stay.HotelStayId, ct)
                ?? throw new InvalidOperationException($"Folio not found for stay {stay.StayNumber}.");
            if (folio.RowVersion is not { Length: 8 })
                throw new InvalidOperationException($"Folio row version is missing for stay {stay.StayNumber}.");

            var existingLedger = await folioRepo.GetLedgerAsync(audit.PropertyId, folio.HotelFolioId, ct);
            var auditDescription = $"Night Audit {audit.AuditNumber} room charge {audit.BusinessDate}";
            var existingCharge = existingLedger?.Entries.FirstOrDefault(x =>
                x.EntryType == HotelFolioEntryType.RoomCharge &&
                x.BusinessDate == audit.BusinessDate &&
                x.Description == auditDescription);
            if (existingCharge is not null)
            {
                audit.RecordRoomCharge(existingCharge.Amount);
                totalRevenue += existingCharge.Amount;
                posted++;
                continue;
            }

            var plan = ratePlans
                .Where(x => x.RoomTypeId == stay.RoomTypeId || x.RoomTypeId is null)
                .Where(x => DateOnly.FromDateTime(x.EffectiveFrom) <= audit.BusinessDate && DateOnly.FromDateTime(x.EffectiveTo) >= audit.BusinessDate)
                .OrderBy(x => x.RoomTypeId == stay.RoomTypeId ? 0 : 1)
                .ThenBy(x => x.Hierarchy)
                .ThenBy(x => x.PlanCode)
                .FirstOrDefault();
            if (plan is null)
                throw new InvalidOperationException($"No active rate plan for room type {stay.RoomTypeCode} on {audit.BusinessDate}.");

            var (roomRate, _) = plan.CalculatePrice(
                audit.BusinessDate,
                Math.Max(stay.Adults, 1),
                1,
                1,
                1);
            if (roomRate <= 0)
                throw new InvalidOperationException($"Rate plan {plan.PlanCode} returned an invalid room rate for stay {stay.StayNumber}.");

            var request = new PostingRequest(
                audit.PropertyId, folio.HotelFolioId, stay.HotelStayId, folio.RowVersion,
                HotelFolioEntryType.RoomCharge, roomRate,
                auditDescription,
                audit.BusinessDate, "NightAudit", audit.NightAuditId,
                TaxRate: 0, ServiceChargeRate: 0, RatePlanId: plan.RatePlanId
            );

            var result = await postingEngine.PostAsync(request, ct);
            if (!result.Success)
                throw new InvalidOperationException(result.Error ?? $"Room charge failed for stay {stay.StayNumber}.");

            audit.RecordRoomCharge(result.BaseAmount);
            totalRevenue += result.BaseAmount;
            posted++;
        }

        audit.AddValidation(new AuditValidationResult(true, "RoomChargesPosted", $"Posted {posted} room charges via PostingEngine, total: {totalRevenue:N0} VND.", "Info"));
        return $"{posted} room charges posted. Total revenue: {totalRevenue:N0} VND.";
    }

    // === STEP 5: Post Taxes ===
    private async Task<string?> PostTaxesAsync(NightAuditAggregate audit, CancellationToken ct)
    {
        var taxAmount = await PostDerivedChargesAsync(
            audit,
            HotelTaxServiceChargeKind.Tax,
            "tax",
            (current, amount) => current.RecordTax(amount),
            ct);
        audit.AddValidation(new AuditValidationResult(true, "TaxPosted", $"Tax posted: {taxAmount:N0} VND.", "Info"));
        return $"Tax: {taxAmount:N0} VND.";
    }

    // === STEP 6: Post Service Charges ===
    private async Task<string?> PostServiceChargesAsync(NightAuditAggregate audit, CancellationToken ct)
    {
        var scAmount = await PostDerivedChargesAsync(
            audit,
            HotelTaxServiceChargeKind.ServiceCharge,
            "service charge",
            (current, amount) => current.RecordServiceCharge(amount),
            ct);
        audit.AddValidation(new AuditValidationResult(true, "ServiceChargePosted", $"Service charge posted: {scAmount:N0} VND.", "Info"));
        return $"Service charge: {scAmount:N0} VND.";
    }

    private async Task<decimal> PostDerivedChargesAsync(
        NightAuditAggregate audit,
        HotelTaxServiceChargeKind kind,
        string label,
        Action<NightAuditAggregate, decimal> record,
        CancellationToken ct)
    {
        var entryType = kind == HotelTaxServiceChargeKind.Tax
            ? HotelFolioEntryType.Tax
            : HotelFolioEntryType.ServiceCharge;
        var stays = await stayRepo.GetCurrentInHouseAsync(audit.PropertyId, ct);
        var ratePlans = await rateRepo.GetAllAsync(
            audit.PropertyId,
            new RatePlanSearchFilter(audit.PropertyId, RatePlanStatus.Active, null, null),
            ct);
        var pending = new List<(Guid FolioId, Guid StayId, byte[]? RowVersion, decimal Amount, string Description, string StayNumber)>();
        foreach (var stay in stays)
        {
            var folio = await folioRepo.GetByStayAsync(audit.PropertyId, stay.HotelStayId, ct)
                ?? throw new InvalidOperationException($"Folio not found for stay {stay.StayNumber}.");
            if (folio.RowVersion is not { Length: 8 })
                throw new InvalidOperationException($"Folio row version is missing for stay {stay.StayNumber}.");

            var ledger = await folioRepo.GetLedgerAsync(audit.PropertyId, folio.HotelFolioId, ct);
            var sourceDescription = $"Night Audit {audit.AuditNumber} room charge {audit.BusinessDate}";
            var sourceAmount = ledger?.Entries
                .Where(x => x.EntryType == HotelFolioEntryType.RoomCharge && x.BusinessDate == audit.BusinessDate && x.Description == sourceDescription)
                .Select(x => x.Amount)
                .FirstOrDefault() ?? 0;
            if (sourceAmount <= 0) continue;

            var description = $"Night Audit {audit.AuditNumber} {label} {audit.BusinessDate}";
            var existing = ledger?.Entries.FirstOrDefault(x =>
                x.EntryType == entryType && x.BusinessDate == audit.BusinessDate && x.Description == description);
            if (existing is not null)
            {
                pending.Add((folio.HotelFolioId, stay.HotelStayId, null, existing.Amount, description, stay.StayNumber));
                continue;
            }

            var plan = ratePlans
                .Where(x => x.RoomTypeId == stay.RoomTypeId)
                .Where(x => DateOnly.FromDateTime(x.EffectiveFrom) <= audit.BusinessDate && DateOnly.FromDateTime(x.EffectiveTo) >= audit.BusinessDate)
                .OrderBy(x => x.Hierarchy)
                .ThenBy(x => x.PlanCode)
                .FirstOrDefault();
            if (plan is null)
                throw new InvalidOperationException($"No active rate plan for room type {stay.RoomTypeCode} on {audit.BusinessDate}.");

            var rules = await structureRepo.GetEffectiveTaxServiceChargeRulesAsync(
                audit.PropertyId,
                audit.BusinessDate,
                "ROOM",
                plan.PlanCode,
                ct);
            var rule = rules.FirstOrDefault(x => x.Kind == kind);
            if (rule is null)
            {
                var error = $"No active {label} rule for room charges, rate plan {plan.PlanCode}, on {audit.BusinessDate}; no posting generated.";
                audit.AddValidation(new AuditValidationResult(
                    false,
                    kind == HotelTaxServiceChargeKind.Tax ? "MissingTaxRule" : "MissingServiceChargeRule",
                    error,
                    "Error"));
                throw new InvalidOperationException(error);
            }

            var amount = CalculateDerivedAmount(sourceAmount, rule);
            if (amount <= 0)
            {
                var error = $"Active {label} rule {rule.Code} produced no positive amount; no posting generated.";
                audit.AddValidation(new AuditValidationResult(
                    false,
                    kind == HotelTaxServiceChargeKind.Tax ? "InvalidTaxRule" : "InvalidServiceChargeRule",
                    error,
                    "Error"));
                throw new InvalidOperationException(error);
            }

            pending.Add((folio.HotelFolioId, stay.HotelStayId, folio.RowVersion, amount, description, stay.StayNumber));
        }

        decimal total = 0;
        foreach (var candidate in pending)
        {
            var amount = candidate.Amount;
            if (candidate.RowVersion is not null)
            {
                var result = await postingEngine.PostAsync(new PostingRequest(
                    audit.PropertyId,
                    candidate.FolioId,
                    candidate.StayId,
                    candidate.RowVersion,
                    entryType,
                    amount,
                    candidate.Description,
                    audit.BusinessDate,
                    "NightAudit",
                    audit.NightAuditId), ct);
                if (!result.Success)
                    throw new InvalidOperationException(result.Error ?? $"{label} failed for stay {candidate.StayNumber}.");
                amount = result.BaseAmount;
            }

            record(audit, amount);
            total += amount;
        }

        return total;
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

    // === STEP 7: Apply Packages ===
    private async Task<string?> ApplyPackagesAsync(NightAuditAggregate audit, CancellationToken ct)
    {
        // Package rules are property-specific and there is no canonical rule
        // contract in the current schema. Do not fabricate package postings or
        // report a successful application when nothing was evaluated.
        audit.AddValidation(new AuditValidationResult(true, "PackagesNotConfigured", "No package rules are configured; no package postings were generated.", "Info"));
        return "No package rules configured.";
    }

    // === STEP 8: Settlement Validation ===
    private async Task<string?> ValidateSettlementsAsync(NightAuditAggregate audit, CancellationToken ct)
    {
        var stays = await stayRepo.GetCurrentInHouseAsync(audit.PropertyId, ct);
        var unsettled = stays.Count(s => s.Status == HotelStayStatus.CheckedOut || s.ActualCheckInAtUtc == null);
        if (unsettled > 0)
            audit.AddValidation(new AuditValidationResult(false, "UnsettledStays", $"{unsettled} stays require settlement attention.", "Warning"));
        audit.AddValidation(new AuditValidationResult(true, "SettlementCheck", $"Settlement validation complete for {stays.Count} stays.", "Info"));
        return $"Settlement validated. {unsettled} stays require attention.";
    }

    // === STEP 9: Update Expected Departures ===
    private async Task<string?> UpdateExpectedDeparturesAsync(NightAuditAggregate audit, DateTime nowUtc, CancellationToken ct)
    {
        var stays = await stayRepo.GetExpectedDeparturesAsync(audit.PropertyId, audit.BusinessDate, ct);
        audit.RecordStayUpdated(); // Mark departures processed
        audit.AddValidation(new AuditValidationResult(true, "DeparturesUpdated", $"{stays.Count} stays marked for expected departure today.", "Info"));
        return $"{stays.Count} expected departures processed.";
    }

    // === STEP 10: Generate Revenue Snapshot ===
    private async Task<string?> GenerateSnapshotAsync(NightAuditAggregate audit, DateTime nowUtc, CancellationToken ct)
    {
        var arrivals = await stayRepo.GetExpectedArrivalsAsync(audit.PropertyId, audit.BusinessDate, ct);
        var departures = await stayRepo.GetExpectedDeparturesAsync(audit.PropertyId, audit.BusinessDate, ct);
        var rooms = await occupancyReader.GetRoomOccupancyAsync(audit.PropertyId, ct);
        var occupancyMetrics = NightAuditMetrics.CalculateOccupancy(rooms);
        var totalRooms = occupancyMetrics.TotalRooms;
        var roomsSold = occupancyMetrics.RoomsSold;
        var occupancy = occupancyMetrics.OccupancyRate;
        var financial = await financialReader.GetAsync(audit.PropertyId, audit.BusinessDate, ct);
        var totalRevenue = financial.RoomRevenue + financial.OtherRevenue + financial.ServiceCharge;
        var adr = financial.RoomNightsSold > 0 ? financial.RoomRevenue / financial.RoomNightsSold : 0;
        var revpar = totalRooms > 0 ? financial.RoomRevenue / totalRooms : 0;
        var reservationMetrics = await reservationMetricsReader.GetAsync(audit.PropertyId, audit.BusinessDate, ct);

        var snapshot = RevenueSnapshot.Create(audit.PropertyId, audit.NightAuditId, audit.BusinessDate,
            financial.RoomRevenue, financial.OtherRevenue, totalRevenue, financial.TotalTax, financial.TotalPayments, financial.TotalRefunds, financial.OutstandingBalance,
            roomsSold, totalRooms, occupancy, adr, revpar,
            arrivals.Count, departures.Count, reservationMetrics.NoShows, reservationMetrics.Cancellations,
            JsonSerializer.Serialize(new
            {
                occupancy,
                adr,
                revpar,
                roomRevenue = financial.RoomRevenue,
                otherRevenue = financial.OtherRevenue,
                serviceCharge = financial.ServiceCharge,
                totalTax = financial.TotalTax,
                totalPayments = financial.TotalPayments,
                totalRefunds = financial.TotalRefunds,
                outstandingBalance = financial.OutstandingBalance,
                roomNightsSold = financial.RoomNightsSold
            }), nowUtc);
        await repo.AddRevenueSnapshotAsync(snapshot, ct);
        return $"Revenue snapshot created. Revenue: {totalRevenue:N0}, ADR: {adr:N0}, RevPAR: {revpar:N0}, Occupancy: {occupancy:F1}%.";
    }

    // === STEP 11: Roll Business Date ===
    private async Task<string?> RollBusinessDateAsync(NightAuditAggregate audit, DateTime nowUtc, CancellationToken ct)
    {
        var bd = await repo.GetBusinessDateAsync(audit.PropertyId, audit.BusinessDate, ct);
        if (bd is not null && bd.Status != BusinessDateStatus.Closed)
        {
            bd.MarkClosed(nowUtc, null);
            var nextDate = audit.BusinessDate.AddDays(1);
            var nextBd = BusinessDateRecord.Open(audit.PropertyId, nextDate, nowUtc, null);
            await repo.AddBusinessDateAsync(nextBd, ct);
            return $"BusinessDate rolled: {audit.BusinessDate} → {nextDate}.";
        }
        return "BusinessDate already closed.";
    }

    // === STEP 12: Finalize ===
    private async Task<string?> FinalizeAsync(NightAuditAggregate audit, CancellationToken ct)
    {
        if (!await lockManager.UnlockAsync(audit.PropertyId, ct))
            throw new InvalidOperationException("Could not unlock property after Night Audit.");
        var summary = audit.ToSummary();
        audit.AddValidation(new AuditValidationResult(true, "AuditComplete", $"Night audit {audit.AuditNumber} complete. {summary.RoomsCharged} rooms charged, {summary.TotalRoomRevenue:N0} VND revenue.", "Info"));
        return $"Night Audit {audit.AuditNumber} finalized. Revenue: {audit.RoomRevenue:N0} VND.";
    }

    // =============================================================================
    // RESUME
    // =============================================================================
    public async Task<Result<NightAuditDto>> ResumeAsync(ResumeNightAuditCommand c, CancellationToken ct)
    {
        if (!auth.HasPermission("FrontOffice.NightAudit.Execute")) return F<NightAuditDto>(PD);
        if (Context(out var pid) is { } e) return F<NightAuditDto>(e);
        var audit = await repo.GetAsync(pid, c.NightAuditId, ct);
        if (audit is null) return F<NightAuditDto>(Error.Business("NIGHT_AUDIT_NOT_FOUND", ""));
        if (!audit.CanResume()) return F<NightAuditDto>(Error.Business("NIGHT_AUDIT_CANNOT_RESUME", "Audit cannot be resumed."));
        var lastStep = audit.GetLastCompletedStep();
        audit.Resume(clock.UtcNow);
        await persistence.CommitAsync(ct);
        return Result<NightAuditDto>.Success(Map(audit));
    }

    // =============================================================================
    // FAIL / CANCEL
    // =============================================================================
    public async Task<Result<NightAuditDto>> FailAsync(FailNightAuditCommand c, CancellationToken ct)
    {
        if (!auth.HasPermission("FrontOffice.NightAudit.Execute")) return F<NightAuditDto>(PD);
        if (Context(out var pid) is { } e) return F<NightAuditDto>(e);
        var audit = await repo.GetAsync(pid, c.NightAuditId, ct);
        if (audit is null) return F<NightAuditDto>(Error.Business("NIGHT_AUDIT_NOT_FOUND", ""));
        audit.Fail(c.Error, clock.UtcNow);
        await lockManager.UnlockAsync(pid, ct);
        await persistence.CommitAsync(ct);
        return Result<NightAuditDto>.Success(Map(audit));
    }

    public async Task<Result<NightAuditDto>> CancelAsync(CancelNightAuditCommand c, CancellationToken ct)
    {
        if (!auth.HasPermission("FrontOffice.NightAudit.Cancel")) return F<NightAuditDto>(PD);
        if (Context(out var pid) is { } e) return F<NightAuditDto>(e);
        var audit = await repo.GetAsync(pid, c.NightAuditId, ct);
        if (audit is null) return F<NightAuditDto>(Error.Business("NIGHT_AUDIT_NOT_FOUND", ""));
        audit.Cancel(c.Reason, clock.UtcNow);
        await lockManager.UnlockAsync(pid, ct);
        await persistence.CommitAsync(ct);
        return Result<NightAuditDto>.Success(Map(audit));
    }

    // =============================================================================
    // HELPERS
    // =============================================================================
    private static NightAuditDto Map(NightAuditAggregate a) => new(a.NightAuditId, a.AuditNumber, a.BusinessDate, a.Status, a.CurrentStep, a.ErrorMessage, a.StartedAtUtc, a.CompletedAtUtc, a.Steps.Count(s => s.Status == NightAuditStepStatus.Completed), a.Steps.Count, a.RowVersion);
}
