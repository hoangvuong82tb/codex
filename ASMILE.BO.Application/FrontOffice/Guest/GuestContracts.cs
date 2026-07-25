using ASMILE.BO.Application.FrontOffice.Foundation.Cqrs;
using ASMILE.BO.Core.FrontOffice.Guest;
using ASMILE.BO.Core.FrontOffice.Loyalty;
using ASMILE.BO.Core.FrontOffice.Stay;

namespace ASMILE.BO.Application.FrontOffice.Guest;

// =============================================================================
// TYPED PREFERENCE ENGINE
// =============================================================================

/// <summary>Typed guest preference — replaces string key-value pairs.</summary>
public enum PreferenceKey
{
    Smoking, NonSmoking, HighFloor, LowFloor, NearElevator, AwayElevator,
    Twin, King, Queen, Double, Single, ExtraBed, BabyCot,
    Newspaper, AirportPickup, LateCheckout, EarlyCheckin,
    FavoriteDrink, FavoriteFood, SpecialRequests,
    Accessibility, Religion, Diet, Allergies,
    NoisePreference, TemperaturePreference, PillowType,
    Connecting, Quiet, Corner, Balcony, View,
    MarketingConsent, DataProcessingConsent, ThirdPartyConsent
}

public sealed record TypedPreference(PreferenceKey Key, string Value, int Priority, bool IsActive, DateTime LastUpdatedUtc);

/// <summary>Preference match result for room allocation scoring.</summary>
public sealed record PreferenceMatchResult(Guid GuestId, IReadOnlyList<(PreferenceKey Key, bool Matched, string? Detail)> Matches, int TotalMatched, int TotalPreferences, decimal MatchPercent);

// =============================================================================
// GUEST MERGE
// =============================================================================

/// <summary>Duplicate detection criteria.</summary>
public sealed record DuplicateDetectionCriteria(string? FullName, string? Phone, string? Email, string? PassportNumber, string? NationalId);

/// <summary>Duplicate candidate with confidence score.</summary>
public sealed record DuplicateCandidate(Guid ExistingGuestId, string ExistingGuestNumber, string ExistingFullName, decimal Confidence, string MatchReason);

/// <summary>Merge request — merge source guest into target guest.</summary>
public sealed record GuestMergeRequest(Guid SourceGuestId, Guid TargetGuestId, string Reason, bool KeepSourceHistory, Guid RequestedBy);

/// <summary>Merge result with audit trail.</summary>
public sealed record GuestMergeResult(bool Success, Guid TargetGuestId, IReadOnlyList<string> TransferredItems, IReadOnlyList<string> Warnings, string? Error);

// =============================================================================
// GUEST PRIVACY (GDPR)
// =============================================================================

/// <summary>Consent types.</summary>
public enum ConsentType { Marketing, DataProcessing, ThirdParty, EmailNewsletter, SmsPromo, PushNotification, LocationTracking }

/// <summary>Consent status.</summary>
public enum ConsentStatus { Granted, Denied, Withdrawn, Expired, PendingRenewal }

/// <summary>Privacy request types.</summary>
public enum PrivacyRequestType { DeleteAccount, ExportData, RestrictProcessing, RectifyData, AccessData, WithdrawConsent }

/// <summary>Data subject request (GDPR).</summary>
public sealed record DataSubjectRequest(Guid RequestId, Guid GuestId, PrivacyRequestType RequestType, DateTime RequestedAtUtc, DateTime? CompletedAtUtc, string? Result, Guid? ProcessedBy);

/// <summary>PII masking result.</summary>
public sealed record PiiMaskingResult(Guid GuestId, IReadOnlyList<string> MaskedFields, DateTime MaskedAtUtc, Guid? MaskedBy);

// =============================================================================
// GUEST HISTORY (UNIFIED)
// =============================================================================

/// <summary>Unified guest history entry — aggregated from multiple modules.</summary>
public sealed record GuestHistoryEntry(
    Guid GuestId, string EventType, string Module, DateTime OccurredAtUtc,
    string Description, Guid? CorrelationId, Guid? ReferenceId,
    string? ReferenceType, decimal? Amount, IReadOnlyDictionary<string, string>? Metadata
);

/// <summary>Guest history query.</summary>
public sealed record GuestHistoryQuery(Guid GuestId, string? EventType, string? Module, DateTime? From, DateTime? To, int Page = 1, int PageSize = 50);

// =============================================================================
// GUEST DASHBOARD
// =============================================================================

/// <summary>Guest 360 dashboard.</summary>
public sealed record GuestDashboardDto(
    Guid GuestId, string GuestNumber, string FullName, string? Phone, string? Email,
    VipLevel VipLevel, MembershipLevel LoyaltyLevel, int TotalStays, decimal TotalRevenue,
    decimal AverageSpend, DateOnly? LastStayDate, int LoyaltyPoints,
    bool IsBlacklisted, bool HasConsentMarketing,
    IReadOnlyList<TypedPreference> TopPreferences,
    IReadOnlyList<GuestHistoryEntry> RecentHistory,
    IReadOnlyList<StaySummaryDto> RecentStays
);

public sealed record StaySummaryDto(Guid StayId, string StayNumber, DateOnly ArrivalDate, DateOnly DepartureDate, string RoomNumber, decimal FolioTotal, HotelStayStatus Status);

/// <summary>Guest search filter.</summary>
public sealed record GuestSearchFilter(string? Keyword, VipLevel? MinVipLevel, bool? IsBlacklisted, bool? HasConsentMarketing, string? Nationality, string? Company, DateOnly? LastStayBefore, DateOnly? LastStayAfter, decimal? MinRevenue);

/// <summary>VIP dashboard.</summary>
public sealed record VipDashboardDto(
    int TotalVip, int Diamond, int Platinum, int Gold, int Silver, int Bronze,
    decimal TotalVipRevenue, decimal AverageVipSpend,
    IReadOnlyList<VipGuestDto> TopSpenders,
    IReadOnlyList<VipGuestDto> RecentUpgrades
);

public sealed record VipGuestDto(Guid GuestId, string GuestNumber, string FullName, VipLevel VipLevel, int TotalStays, decimal TotalRevenue, DateTime? LastStayDate);

/// <summary>Loyalty earn/redeem rules.</summary>
public sealed record LoyaltyEarnRule(string RuleId, string Description, string Category, decimal EarnRate, decimal? MinAmount, decimal? MaxPerStay, bool IsActive);
public sealed record LoyaltyRedeemRule(string RuleId, string Description, int PointsRequired, string Benefit, decimal? MonetaryValue, bool IsActive);

// =============================================================================
// REPOSITORY INTERFACES
// =============================================================================

public interface IGuestProfileRepository
{
    Task<GuestProfile?> GetAsync(Guid propertyId, Guid guestId, CancellationToken ct);
    Task<GuestProfileDto?> GetDtoAsync(Guid propertyId, Guid guestId, CancellationToken ct);
    Task<PageResult<GuestProfileDto>> SearchAsync(Guid propertyId, GuestSearchFilter filter, PageRequest page, CancellationToken ct);
    Task AddAsync(GuestProfile guest, CancellationToken ct);
    Task UpdateAsync(GuestProfile guest, CancellationToken ct);
    Task<IReadOnlyList<DuplicateCandidate>> FindDuplicatesAsync(Guid propertyId, DuplicateDetectionCriteria criteria, CancellationToken ct);
    Task<GuestMergeResult> MergeAsync(GuestMergeRequest request, CancellationToken ct);
    Task<PiiMaskingResult> MaskPiiAsync(Guid guestId, Guid requestedBy, CancellationToken ct);
    Task<DataSubjectRequest> SubmitPrivacyRequestAsync(DataSubjectRequest request, CancellationToken ct);
    Task<IReadOnlyList<GuestHistoryEntry>> GetHistoryAsync(GuestHistoryQuery query, CancellationToken ct);
    Task<GuestDashboardDto> GetDashboardAsync(Guid propertyId, Guid guestId, CancellationToken ct);
    Task<VipDashboardDto> GetVipDashboardAsync(Guid propertyId, CancellationToken ct);
}

public sealed record GuestProfileDto(Guid GuestProfileId, string GuestNumber, string FullName, string? Phone, string? Email, VipLevel VipLevel, int TotalStays, decimal TotalRevenue, DateTime CreatedAtUtc, bool IsBlacklisted, string? Nationality, string? CompanyName);
