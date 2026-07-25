using ASMILE.BO.Application.FrontOffice.Guest;
using ASMILE.BO.Core.FrontOffice.Guest;

namespace ASMILE.BO.Infrastructure.FrontOffice.Guest;

/// <summary>
/// PreferenceEngine — matches guest preferences against room attributes.
/// </summary>
public static class PreferenceEngine
{
    public static PreferenceMatchResult MatchPreferences(GuestProfile guest, IReadOnlyDictionary<PreferenceKey, bool> roomAttributes)
    {
        var matches = new List<(PreferenceKey, bool, string?)>();
        int totalMatched = 0, totalPrefs = 0;

        foreach (var pref in guest.Preferences)
        {
            if (!Enum.TryParse<PreferenceKey>(pref.Key, true, out var key)) continue;
            totalPrefs++;

            bool? matchResult = key switch
            {
                PreferenceKey.Smoking => roomAttributes.GetValueOrDefault(PreferenceKey.Smoking, false),
                PreferenceKey.NonSmoking => !roomAttributes.GetValueOrDefault(PreferenceKey.Smoking, false),
                PreferenceKey.HighFloor => roomAttributes.GetValueOrDefault(PreferenceKey.HighFloor, false),
                PreferenceKey.LowFloor => roomAttributes.GetValueOrDefault(PreferenceKey.LowFloor, false),
                PreferenceKey.NearElevator => roomAttributes.GetValueOrDefault(PreferenceKey.NearElevator, false),
                PreferenceKey.AwayElevator => !roomAttributes.GetValueOrDefault(PreferenceKey.NearElevator, false),
                PreferenceKey.King => roomAttributes.GetValueOrDefault(PreferenceKey.King, false),
                PreferenceKey.Twin => roomAttributes.GetValueOrDefault(PreferenceKey.Twin, false),
                PreferenceKey.Queen => roomAttributes.GetValueOrDefault(PreferenceKey.Queen, false),
                PreferenceKey.Double => roomAttributes.GetValueOrDefault(PreferenceKey.Double, false),
                PreferenceKey.ExtraBed => roomAttributes.GetValueOrDefault(PreferenceKey.ExtraBed, false),
                PreferenceKey.BabyCot => roomAttributes.GetValueOrDefault(PreferenceKey.BabyCot, false),
                PreferenceKey.Connecting => roomAttributes.GetValueOrDefault(PreferenceKey.Connecting, false),
                PreferenceKey.Quiet => roomAttributes.GetValueOrDefault(PreferenceKey.Quiet, false),
                PreferenceKey.Corner => roomAttributes.GetValueOrDefault(PreferenceKey.Corner, false),
                PreferenceKey.Balcony => roomAttributes.GetValueOrDefault(PreferenceKey.Balcony, false),
                PreferenceKey.Accessibility => roomAttributes.GetValueOrDefault(PreferenceKey.Accessibility, false),
                _ => null
            };

            if (matchResult.HasValue)
            {
                if (matchResult.Value) totalMatched++;
                matches.Add((key, matchResult.Value, matchResult.Value ? null : $"Room does not match {key}"));
            }
        }

        return new PreferenceMatchResult(guest.GuestProfileId, matches, totalMatched, totalPrefs,
            totalPrefs > 0 ? (decimal)totalMatched / totalPrefs * 100 : 0);
    }
}

/// <summary>
/// GuestMergeEngine — detects duplicates and merges guests.
/// </summary>
public static class GuestMergeEngine
{
    public static IReadOnlyList<DuplicateCandidate> DetectDuplicates(IReadOnlyList<GuestProfile> existing, DuplicateDetectionCriteria criteria)
    {
        var candidates = new List<DuplicateCandidate>();

        foreach (var guest in existing)
        {
            var score = 0m;
            var reasons = new List<string>();

            if (!string.IsNullOrWhiteSpace(criteria.FullName) && guest.FullName.Equals(criteria.FullName, StringComparison.OrdinalIgnoreCase))
            { score += 40; reasons.Add("NameMatch"); }
            else if (!string.IsNullOrWhiteSpace(criteria.FullName) && guest.FullName.Contains(criteria.FullName, StringComparison.OrdinalIgnoreCase))
            { score += 20; reasons.Add("PartialNameMatch"); }

            if (!string.IsNullOrWhiteSpace(criteria.Phone) && guest.Phone == criteria.Phone)
            { score += 30; reasons.Add("PhoneMatch"); }

            if (!string.IsNullOrWhiteSpace(criteria.Email) && guest.Email?.Equals(criteria.Email, StringComparison.OrdinalIgnoreCase) == true)
            { score += 30; reasons.Add("EmailMatch"); }

            var passport = guest.Documents.FirstOrDefault(d => d.DocumentType == GuestDocumentType.Passport);
            if (!string.IsNullOrWhiteSpace(criteria.PassportNumber) && passport?.DocumentNumber == criteria.PassportNumber)
            { score += 50; reasons.Add("PassportMatch"); }

            var nationalId = guest.Documents.FirstOrDefault(d => d.DocumentType == GuestDocumentType.NationalId);
            if (!string.IsNullOrWhiteSpace(criteria.NationalId) && nationalId?.DocumentNumber == criteria.NationalId)
            { score += 50; reasons.Add("NationalIdMatch"); }

            if (score >= 30)
                candidates.Add(new DuplicateCandidate(guest.GuestProfileId, guest.GuestNumber, guest.FullName, score, string.Join(", ", reasons)));
        }

        return candidates.OrderByDescending(c => c.Confidence).ToList();
    }

    public static GuestMergeResult ValidateMerge(GuestMergeRequest request, GuestProfile? source, GuestProfile? target)
    {
        if (source is null) return new GuestMergeResult(false, request.TargetGuestId, [], [], "Source guest not found.");
        if (target is null) return new GuestMergeResult(false, request.TargetGuestId, [], [], "Target guest not found.");
        if (source.GuestProfileId == target.GuestProfileId) return new GuestMergeResult(false, request.TargetGuestId, [], [], "Cannot merge guest into itself.");
        if (source.PropertyId != target.PropertyId) return new GuestMergeResult(false, request.TargetGuestId, [], [], "Guests belong to different properties.");

        var transferred = new List<string>();
        if (source.Preferences.Count > 0) transferred.Add($"Preferences({source.Preferences.Count})");
        if (source.Documents.Count > 0) transferred.Add($"Documents({source.Documents.Count})");

        return new GuestMergeResult(true, request.TargetGuestId, transferred, [], null);
    }
}

/// <summary>
/// GuestPrivacyEngine — handles GDPR requests and PII masking.
/// </summary>
public static class GuestPrivacyEngine
{
    public static bool RequiresConsentRenewal(DateTime? consentGivenAtUtc, int renewalPeriodDays = 365)
    {
        if (consentGivenAtUtc is null) return true;
        return DateTime.UtcNow > consentGivenAtUtc.Value.AddDays(renewalPeriodDays);
    }

    public static IReadOnlyList<string> GetPiiFields() => ["FullName", "Phone", "Email", "Address", "DateOfBirth", "Nationality", "Country", "Documents"];

    public static PiiMaskingResult MaskPii(GuestProfile guest, Guid maskedBy)
    {
        var masked = new List<string>();
        // In production: actually nullify/anonymize fields, audit the change
        masked.AddRange(GetPiiFields());
        return new PiiMaskingResult(guest.GuestProfileId, masked, DateTime.UtcNow, maskedBy);
    }
}
