using ASMILE.BO.Application.FrontOffice.Guest;
using ASMILE.BO.Core.FrontOffice.Foundation.Identifiers;
using ASMILE.BO.Core.FrontOffice.Guest;
using ASMILE.BO.Core.FrontOffice.Loyalty;
using ASMILE.BO.Infrastructure.FrontOffice.Guest;
using FluentAssertions;

namespace ASMILE.BO.Tests.Unit;

public sealed class GuestPlatformTests
{
    // =============================================================================
    // PREFERENCE ENGINE
    // =============================================================================
    [Fact]
    public void GUEST_001_preference_match_smoking()
    {
        var guest = CreateGuest();
        guest.AddPreference("Smoking", "Yes");
        var attributes = new Dictionary<PreferenceKey, bool> { [PreferenceKey.Smoking] = true };

        var result = PreferenceEngine.MatchPreferences(guest, attributes);

        result.MatchPercent.Should().Be(100m);
        result.TotalMatched.Should().Be(1);
    }

    [Fact]
    public void GUEST_002_preference_mismatch_non_smoking()
    {
        var guest = CreateGuest();
        guest.AddPreference("NonSmoking", "Yes");
        var attributes = new Dictionary<PreferenceKey, bool> { [PreferenceKey.Smoking] = true };

        var result = PreferenceEngine.MatchPreferences(guest, attributes);

        result.MatchPercent.Should().Be(0m);
        result.TotalMatched.Should().Be(0);
    }

    [Fact]
    public void GUEST_003_preference_mixed_match()
    {
        var guest = CreateGuest();
        guest.AddPreference("HighFloor", "Yes");
        guest.AddPreference("King", "Yes");
        guest.AddPreference("Smoking", "Yes");
        var attributes = new Dictionary<PreferenceKey, bool> { [PreferenceKey.HighFloor] = true, [PreferenceKey.King] = false, [PreferenceKey.Smoking] = true };

        var result = PreferenceEngine.MatchPreferences(guest, attributes);

        result.TotalMatched.Should().Be(2);
        result.TotalPreferences.Should().Be(3);
        result.MatchPercent.Should().BeApproximately(66.67m, 0.1m);
    }

    [Fact]
    public void GUEST_004_preference_unknown_key_skipped()
    {
        var guest = CreateGuest();
        guest.AddPreference("UnknownPreference", "value");
        var attributes = new Dictionary<PreferenceKey, bool>();

        var result = PreferenceEngine.MatchPreferences(guest, attributes);

        result.TotalPreferences.Should().Be(0); // Unknown key = not counted
    }

    // =============================================================================
    // MERGE ENGINE
    // =============================================================================
    [Fact]
    public void GUEST_005_duplicate_detection_by_name_and_phone()
    {
        var existing = new[] { CreateGuest("John Doe", "123456789") };
        var criteria = new DuplicateDetectionCriteria("John Doe", "123456789", null, null, null);

        var duplicates = GuestMergeEngine.DetectDuplicates(existing, criteria);

        duplicates.Should().HaveCount(1);
        duplicates[0].Confidence.Should().Be(70m); // 40 + 30
    }

    [Fact]
    public void GUEST_006_duplicate_not_detected_with_low_confidence()
    {
        var existing = new[] { CreateGuest("Jane Smith", "999") };
        var criteria = new DuplicateDetectionCriteria("John Doe", "123", "diff@email.com", null, null);

        var duplicates = GuestMergeEngine.DetectDuplicates(existing, criteria);

        duplicates.Should().HaveCount(0);
    }

    [Fact]
    public void GUEST_007_cannot_merge_same_guest()
    {
        var source = CreateGuest("Test", "111");
        var request = new GuestMergeRequest(source.GuestProfileId, source.GuestProfileId, "Test", true, Guid.NewGuid());

        var result = GuestMergeEngine.ValidateMerge(request, source, source);

        result.Success.Should().BeFalse();
        result.Error.Should().Contain("itself");
    }

    [Fact]
    public void GUEST_008_valid_merge_detects_transferrable_items()
    {
        var propId = Guid.NewGuid();
        var source = GuestProfile.Create(propId, $"GST-{Guid.NewGuid():N}"[..10], "Source", "111", null, GuestSource.Direct, DateTime.UtcNow, null);
        source.AddPreference("HighFloor", "Yes");
        source.AddDocument(GuestDocumentType.Passport, "P123", "US", null, DateTime.UtcNow);
        var target = GuestProfile.Create(propId, $"GST-{Guid.NewGuid():N}"[..10], "Target", "222", null, GuestSource.Direct, DateTime.UtcNow, null);
        var request = new GuestMergeRequest(source.GuestProfileId, target.GuestProfileId, "Duplicate", true, Guid.NewGuid());

        var result = GuestMergeEngine.ValidateMerge(request, source, target);

        result.Success.Should().BeTrue();
        result.TransferredItems.Should().Contain(i => i.Contains("Preferences"));
        result.TransferredItems.Should().Contain(i => i.Contains("Documents"));
    }

    // =============================================================================
    // PRIVACY ENGINE
    // =============================================================================
    [Fact]
    public void GUEST_009_pii_fields_include_personal_data()
    {
        var fields = GuestPrivacyEngine.GetPiiFields();
        fields.Should().Contain("FullName");
        fields.Should().Contain("Email");
        fields.Should().Contain("Phone");
        fields.Should().Contain("Documents");
    }

    [Fact]
    public void GUEST_010_consent_renewal_required_after_365_days()
    {
        var oneYearAgo = DateTime.UtcNow.AddDays(-400);
        GuestPrivacyEngine.RequiresConsentRenewal(oneYearAgo).Should().BeTrue();
    }

    [Fact]
    public void GUEST_011_consent_still_valid_within_period()
    {
        var recently = DateTime.UtcNow.AddDays(-100);
        GuestPrivacyEngine.RequiresConsentRenewal(recently).Should().BeFalse();
    }

    [Fact]
    public void GUEST_012_pii_masking_returns_all_fields()
    {
        var guest = CreateGuest("Test Guest", "12345");
        var result = GuestPrivacyEngine.MaskPii(guest, Guid.NewGuid());
        result.MaskedFields.Should().NotBeEmpty();
        result.GuestId.Should().Be(guest.GuestProfileId);
    }

    // =============================================================================
    // LOYALTY
    // =============================================================================
    [Fact]
    public void GUEST_013_vip_upgrade_on_threshold()
    {
        var profile = CreateVip();
        profile.EarnPoints(5000, "Stay bonus", DateTime.UtcNow);
        profile.Level.Should().Be(MembershipLevel.Silver); // 5000 → Silver
    }

    [Fact]
    public void GUEST_014_vip_points_not_enough_for_upgrade()
    {
        var profile = CreateVip();
        profile.EarnPoints(1000, "Small bonus", DateTime.UtcNow);
        profile.Level.Should().Be(MembershipLevel.Standard); // < 5000, stay standard
    }

    [Fact]
    public void GUEST_015_loyalty_earn_rule_validation()
    {
        var rule = new LoyaltyEarnRule("R001", "Room spend 10%", "Room", 0.10m, null, 50000m, true);
        rule.EarnRate.Should().Be(0.10m);
        rule.MaxPerStay.Should().Be(50000m);
        rule.IsActive.Should().BeTrue();
    }

    // =============================================================================
    // HELPERS
    // =============================================================================
    private static GuestProfile CreateGuest(string name = "Test Guest", string phone = "000", Guid? propId = null)
    {
        return GuestProfile.Create(propId ?? Guid.NewGuid(), $"GST-{Guid.NewGuid():N}"[..10], name, phone, null, GuestSource.Direct, DateTime.UtcNow, null);
    }

    private static VipProfile CreateVip()
    {
        return VipProfile.Enroll(Guid.NewGuid(), $"VIP-{Guid.NewGuid():N}"[..10], Guid.NewGuid(), "VIP Guest", MembershipLevel.Standard, DateTime.UtcNow, null);
    }
}
