using ASMILE.BO.Application.FrontOffice.Foundation.Cqrs;
using ASMILE.BO.Application.FrontOffice.Guest;
using ASMILE.BO.Core.FrontOffice.Guest;
using ASMILE.BO.Core.FrontOffice.Loyalty;
using ASMILE.BO.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ASMILE.BO.Infrastructure.FrontOffice.Guest;

/// <summary>
/// EF-backed guest master repository.  The repository deliberately keeps
/// unsupported consent/history aggregates read-only until their tables are
/// introduced, rather than inventing persistence in the UI layer.
/// </summary>
public sealed class EfGuestProfileRepository(AppDbContext db) : IGuestProfileRepository
{
    public Task<GuestProfile?> GetAsync(Guid propertyId, Guid guestId, CancellationToken ct) =>
        db.GuestProfiles
            .Include(x => x.Documents)
            .Include(x => x.Preferences)
            .SingleOrDefaultAsync(x => x.PropertyId == propertyId && x.GuestProfileId == guestId, ct);

    public Task<GuestProfileDto?> GetDtoAsync(Guid propertyId, Guid guestId, CancellationToken ct) =>
        db.GuestProfiles.AsNoTracking()
            .Where(x => x.PropertyId == propertyId && x.GuestProfileId == guestId)
            .Select(x => new GuestProfileDto(x.GuestProfileId, x.GuestNumber, x.FullName, x.Phone, x.Email, x.VipLevel, x.TotalStays, x.TotalRevenue, x.CreatedAtUtc, x.IsBlacklisted, x.Nationality, x.CompanyName, x.Notes, x.Gender, x.DateOfBirth))
            .SingleOrDefaultAsync(ct);

    public async Task<PageResult<GuestProfileDto>> SearchAsync(Guid propertyId, GuestSearchFilter filter, PageRequest page, CancellationToken ct)
    {
        var query = db.GuestProfiles.AsNoTracking().Where(x => x.PropertyId == propertyId);
        if (!string.IsNullOrWhiteSpace(filter.Keyword))
        {
            var keyword = filter.Keyword.Trim();
            query = query.Where(x => x.GuestNumber.Contains(keyword) || x.FullName.Contains(keyword) || (x.Phone != null && x.Phone.Contains(keyword)) || (x.Email != null && x.Email.Contains(keyword)));
        }
        if (filter.MinVipLevel.HasValue) query = query.Where(x => x.VipLevel >= filter.MinVipLevel.Value);
        if (filter.IsBlacklisted.HasValue) query = query.Where(x => x.IsBlacklisted == filter.IsBlacklisted.Value);
        if (!string.IsNullOrWhiteSpace(filter.Nationality)) query = query.Where(x => x.Nationality == filter.Nationality);
        if (!string.IsNullOrWhiteSpace(filter.Company)) query = query.Where(x => x.CompanyName != null && x.CompanyName.Contains(filter.Company));
        if (filter.LastStayBefore.HasValue) query = query.Where(x => x.LastStayDate < filter.LastStayBefore.Value);
        if (filter.LastStayAfter.HasValue) query = query.Where(x => x.LastStayDate > filter.LastStayAfter.Value);
        if (filter.MinRevenue.HasValue) query = query.Where(x => x.TotalRevenue >= filter.MinRevenue.Value);
        // Consent is not yet represented by a persisted aggregate in Phase 2B6.
        var total = await query.CountAsync(ct);
        var size = Math.Clamp(page.PageSize, 1, 200);
        var items = await query.OrderBy(x => x.FullName).ThenBy(x => x.GuestNumber).Skip(page.Skip).Take(size)
            .Select(x => new GuestProfileDto(x.GuestProfileId, x.GuestNumber, x.FullName, x.Phone, x.Email, x.VipLevel, x.TotalStays, x.TotalRevenue, x.CreatedAtUtc, x.IsBlacklisted, x.Nationality, x.CompanyName, x.Notes, x.Gender, x.DateOfBirth))
            .ToListAsync(ct);
        return new(items, total, page.Page, page.PageSize);
    }

    public async Task AddAsync(GuestProfile guest, CancellationToken ct)
    {
        db.GuestProfiles.Add(guest);
        await db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(GuestProfile guest, CancellationToken ct)
    {
        db.GuestProfiles.Attach(guest);
        db.Entry(guest).State = EntityState.Modified;
        if (guest.RowVersion is not null) db.Entry(guest).Property(x => x.RowVersion).OriginalValue = guest.RowVersion;
        await db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<DuplicateCandidate>> FindDuplicatesAsync(Guid propertyId, DuplicateDetectionCriteria criteria, CancellationToken ct)
    {
        var candidates = await db.GuestProfiles
            .AsNoTracking()
            .Include(x => x.Documents)
            .Where(x => x.PropertyId == propertyId)
            .ToListAsync(ct);
        var name = Normalize(criteria.FullName);
        var phone = Normalize(criteria.Phone);
        var email = Normalize(criteria.Email);
        var passportNumber = Normalize(criteria.PassportNumber);
        var nationalId = Normalize(criteria.NationalId);
        var result = new List<DuplicateCandidate>();
        foreach (var candidate in candidates)
        {
            var score = 0m;
            var reasons = new List<string>();
            if (!string.IsNullOrEmpty(name) && Normalize(candidate.FullName) == name) { score += 0.55m; reasons.Add("FullName"); }
            if (!string.IsNullOrEmpty(phone) && Normalize(candidate.Phone) == phone) { score += 0.25m; reasons.Add("Phone"); }
            if (!string.IsNullOrEmpty(email) && Normalize(candidate.Email) == email) { score += 0.20m; reasons.Add("Email"); }
            if (candidate.Documents.Any(document =>
                    (!string.IsNullOrEmpty(passportNumber) && Normalize(document.DocumentNumber) == passportNumber) ||
                    (!string.IsNullOrEmpty(nationalId) && Normalize(document.DocumentNumber) == nationalId)))
            {
                score = Math.Max(score, 0.80m);
                reasons.Add("Document");
            }
            if (score >= 0.55m) result.Add(new(candidate.GuestProfileId, candidate.GuestNumber, candidate.FullName, Math.Min(1m, score), string.Join(" + ", reasons)));
        }
        return result.OrderByDescending(x => x.Confidence).ThenBy(x => x.ExistingFullName).ToList();
    }

    public async Task<GuestMergeResult> MergeAsync(GuestMergeRequest request, CancellationToken ct)
    {
        if (request.SourceGuestId == request.TargetGuestId) return new(false, request.TargetGuestId, [], [], "GUEST_MERGE_SAME_PROFILE");
        var source = await db.GuestProfiles.Include(x => x.Documents).Include(x => x.Preferences).SingleOrDefaultAsync(x => x.GuestProfileId == request.SourceGuestId, ct);
        var target = await db.GuestProfiles.Include(x => x.Documents).Include(x => x.Preferences).SingleOrDefaultAsync(x => x.GuestProfileId == request.TargetGuestId, ct);
        if (source is null || target is null) return new(false, request.TargetGuestId, [], [], "GUEST_MERGE_PROFILE_NOT_FOUND");
        if (source.PropertyId != target.PropertyId) return new(false, request.TargetGuestId, [], [], "GUEST_MERGE_PROPERTY_MISMATCH");

        target.UpdateProfile(target.Phone ?? source.Phone, target.Email ?? source.Email, target.Address ?? source.Address, target.Nationality ?? source.Nationality, target.Gender ?? source.Gender, target.DateOfBirth ?? source.DateOfBirth, DateTime.UtcNow, null);
        if (source.VipLevel > target.VipLevel) target.SetVip(source.VipLevel, DateTime.UtcNow, null);
        var documents = await db.GuestDocuments.Where(x => x.PropertyId == source.PropertyId && x.GuestProfileId == source.GuestProfileId).ToListAsync(ct);
        var preferences = await db.GuestPreferences.Where(x => x.PropertyId == source.PropertyId && x.GuestProfileId == source.GuestProfileId).ToListAsync(ct);
        foreach (var document in documents) db.Entry(document).Property(nameof(GuestDocument.GuestProfileId)).CurrentValue = target.GuestProfileId;
        foreach (var preference in preferences) db.Entry(preference).Property(nameof(GuestPreference.GuestProfileId)).CurrentValue = target.GuestProfileId;
        await db.SaveChangesAsync(ct);
        return new(true, target.GuestProfileId, [$"Documents: {documents.Count}", $"Preferences: {preferences.Count}"], ["Source profile retained for audit; dependent records were reassigned."], null);
    }

    public async Task<PiiMaskingResult> MaskPiiAsync(Guid guestId, Guid requestedBy, CancellationToken ct)
    {
        var guest = await db.GuestProfiles.SingleOrDefaultAsync(x => x.GuestProfileId == guestId, ct);
        if (guest is null) return new(guestId, [], DateTime.UtcNow, requestedBy);
        var masked = new[] { nameof(GuestProfile.Phone), nameof(GuestProfile.Email), nameof(GuestProfile.Address), nameof(GuestProfile.EmergencyContact), nameof(GuestProfile.EmergencyPhone) };
        foreach (var field in masked) db.Entry(guest).Property(field).CurrentValue = null;
        await db.SaveChangesAsync(ct);
        return new(guestId, masked, DateTime.UtcNow, requestedBy);
    }

    public Task<DataSubjectRequest> SubmitPrivacyRequestAsync(DataSubjectRequest request, CancellationToken ct) => Task.FromResult(request);

    public async Task<IReadOnlyList<GuestHistoryEntry>> GetHistoryAsync(GuestHistoryQuery query, CancellationToken ct)
    {
        var guest = await db.GuestProfiles.AsNoTracking().SingleOrDefaultAsync(x => x.GuestProfileId == query.GuestId, ct);
        if (guest is null) return [];
        var entry = new GuestHistoryEntry(guest.GuestProfileId, "ProfileCreated", "Guest", guest.CreatedAtUtc, "Guest profile created", null, guest.GuestProfileId, "GuestProfile", null, null);
        if (query.EventType is not null && !entry.EventType.Equals(query.EventType, StringComparison.OrdinalIgnoreCase)) return [];
        if (query.Module is not null && !entry.Module.Equals(query.Module, StringComparison.OrdinalIgnoreCase)) return [];
        if (query.From.HasValue && entry.OccurredAtUtc < query.From.Value) return [];
        if (query.To.HasValue && entry.OccurredAtUtc > query.To.Value) return [];
        return [entry];
    }

    public async Task<GuestDashboardDto> GetDashboardAsync(Guid propertyId, Guid guestId, CancellationToken ct)
    {
        var guest = await GetAsync(propertyId, guestId, ct) ?? throw new KeyNotFoundException("GUEST_NOT_FOUND");
        var stays = await db.HotelStays.AsNoTracking().Where(x => x.PropertyId == propertyId && x.GuestId == guestId).OrderByDescending(x => x.ScheduledArrivalDate).Take(10).ToListAsync(ct);
        var preferences = guest.Preferences.Select(x => new TypedPreference(Enum.TryParse<PreferenceKey>(x.Key, true, out var key) ? key : PreferenceKey.SpecialRequests, x.Value, 0, true, guest.UpdatedAtUtc)).ToList();
        return new(guest.GuestProfileId, guest.GuestNumber, guest.FullName, guest.Phone, guest.Email, guest.VipLevel, MembershipLevel.Standard, guest.TotalStays, guest.TotalRevenue, guest.AverageSpend, guest.LastStayDate, 0, guest.IsBlacklisted, false, preferences, await GetHistoryAsync(new(guestId, null, null, null, null), ct), stays.Select(x => new StaySummaryDto(x.HotelStayId, x.StayNumber, x.ScheduledArrivalDate, x.ScheduledDepartureDate, x.RoomNumberSnapshot, 0, x.Status)).ToList());
    }

    public async Task<VipDashboardDto> GetVipDashboardAsync(Guid propertyId, CancellationToken ct)
    {
        var guests = await db.GuestProfiles.AsNoTracking().Where(x => x.PropertyId == propertyId && x.VipLevel != VipLevel.Normal).ToListAsync(ct);
        static VipGuestDto Map(GuestProfile x) => new(x.GuestProfileId, x.GuestNumber, x.FullName, x.VipLevel, x.TotalStays, x.TotalRevenue, x.LastStayDate?.ToDateTime(TimeOnly.MinValue));
        return new(guests.Count, 0, guests.Count(x => x.VipLevel == VipLevel.Platinum), guests.Count(x => x.VipLevel == VipLevel.Gold), guests.Count(x => x.VipLevel == VipLevel.Silver), guests.Count(x => x.VipLevel == VipLevel.Bronze), guests.Sum(x => x.TotalRevenue), guests.Count == 0 ? 0 : guests.Average(x => x.AverageSpend), guests.OrderByDescending(x => x.TotalRevenue).Take(10).Select(Map).ToList(), guests.OrderByDescending(x => x.UpdatedAtUtc).Take(10).Select(Map).ToList());
    }

    private static string Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim().ToUpperInvariant();
}
