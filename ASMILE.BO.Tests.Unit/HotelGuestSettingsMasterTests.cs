using ASMILE.BO.Core.FrontOffice.Foundation.Identifiers;
using ASMILE.BO.Core.FrontOffice.HotelMasterData;
using FluentAssertions;

namespace ASMILE.BO.Tests.Unit;

public sealed class HotelGuestSettingsMasterTests
{
    [Fact]
    public void Guest_preference_keeps_safety_and_scope_metadata_in_its_own_master()
    {
        var item = HotelGuestPreferenceProfile.Create(new PropertyId(Guid.NewGuid()), "allergy_gluten", "Không gluten", "Safety", "Choice", "No", "Yes,No", "Guest", 10, true, false, DateTime.Today, DateTime.Today.AddYears(1), null, DateTime.UtcNow, null);

        item.Code.Should().Be("ALLERGY_GLUTEN");
        item.IsSafetyCritical.Should().BeTrue();
        item.Scope.Should().Be("Guest");
        item.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Guest_request_rejects_negative_sla_and_keeps_operational_defaults()
    {
        var invalid = () => HotelGuestRequestProfile.Create(new PropertyId(Guid.NewGuid()), "EXTRA_TOWEL", "Extra towel", "Extra Towel", 10, "Housekeeping", -1, "HK", "InApp", DateTime.Today, DateTime.Today.AddYears(1), null, DateTime.UtcNow, null);

        invalid.Should().Throw<ArgumentException>().WithMessage("HOTEL_GUEST_REQUEST_SLA_INVALID");
    }

    [Fact]
    public void Reservation_type_keeps_stay_and_guarantee_rules_in_its_own_master()
    {
        var item = HotelReservationTypeProfile.Create(new PropertyId(Guid.NewGuid()), "CORP", "Corporate", HotelReservationTypeKind.Corporate, true, false, false, true, 60, 48, 1, 30, false, DateTime.Today, DateTime.Today.AddYears(1), null, DateTime.UtcNow, null);

        item.Code.Should().Be("CORP");
        item.GuaranteeRequired.Should().BeTrue();
        item.HoldMinutes.Should().Be(60);
        item.MaximumStayNights.Should().Be(30);
    }

    [Fact]
    public void Booking_source_keeps_tracking_and_commission_rules_in_its_own_master()
    {
        var item = HotelBookingSourceProfile.Create(new PropertyId(Guid.NewGuid()), "OTA_BK", "Booking.com", HotelBookingSourceKind.Ota, true, true, "OTA", "BOOKING", DateTime.Today, DateTime.Today.AddYears(1), null, DateTime.UtcNow, null);

        item.Code.Should().Be("OTA_BK");
        item.Kind.Should().Be(HotelBookingSourceKind.Ota);
        item.IsCommissionable.Should().BeTrue();
        item.RequiresReference.Should().BeTrue();
        item.DefaultMarketSegmentCode.Should().Be("OTA");
    }

    [Fact]
    public void Booking_channel_keeps_connector_and_external_confirmation_rules_in_its_own_master()
    {
        var item = HotelBookingChannelProfile.Create(new PropertyId(Guid.NewGuid()), "BOOKING_ENGINE", "Booking Engine", HotelBookingChannelKind.BookingEngine, true, true, "BE_MAIN", DateTime.Today, DateTime.Today.AddYears(1), null, DateTime.UtcNow, null);

        item.Code.Should().Be("BOOKING_ENGINE");
        item.IsOnline.Should().BeTrue();
        item.RequiresExternalConfirmation.Should().BeTrue();
        item.ConnectorKey.Should().Be("BE_MAIN");
    }

    [Fact]
    public void Market_segment_requires_a_reporting_bucket_and_keeps_rate_reference_separate()
    {
        var item = HotelMarketSegmentProfile.Create(new PropertyId(Guid.NewGuid()), "CORP", "Corporate", HotelMarketSegmentKind.Corporate, "Room Revenue", "CORP_RACK", "Corporate", DateTime.Today, DateTime.Today.AddYears(1), null, DateTime.UtcNow, null);

        item.Code.Should().Be("CORP");
        item.RevenueBucket.Should().Be("Room Revenue");
        item.DefaultRateCode.Should().Be("CORP_RACK");
    }

    [Fact]
    public void Housekeeping_rule_keeps_sla_and_task_creation_policy()
    {
        var item = HotelHousekeepingRuleProfile.Create(new PropertyId(Guid.NewGuid()), "TURN_DOWN", "Turn-down", HotelHousekeepingRuleKind.TurnDown, 5, "Housekeeping", 30, true, true, DateTime.Today, DateTime.Today.AddYears(1), null, DateTime.UtcNow, null);

        item.SlaMinutes.Should().Be(30);
        item.RequiresInspection.Should().BeTrue();
        item.AutoCreateTask.Should().BeTrue();
    }

    [Fact]
    public void Wakeup_rule_keeps_confirmation_and_escalation_policy()
    {
        var item = HotelWakeupRuleProfile.Create(new PropertyId(Guid.NewGuid()), "VIP_WAKEUP", "VIP wake-up", HotelWakeupRuleKind.Scheduled, 5, 10, true, "Phone", DateTime.Today, DateTime.Today.AddYears(1), null, DateTime.UtcNow, null);

        item.ConfirmationRequired.Should().BeTrue();
        item.EscalationMinutes.Should().Be(10);
        item.NotificationChannel.Should().Be("Phone");
    }

    [Fact]
    public void Complaint_profile_requires_category_and_keeps_compensation_policy()
    {
        var item = HotelComplaintProfile.Create(new PropertyId(Guid.NewGuid()), "ROOM_NOISE", "Room noise", "Room", HotelComplaintSeverity.High, "Front Office", 30, 60, true, DateTime.Today, DateTime.Today.AddYears(1), null, DateTime.UtcNow, null);

        item.Severity.Should().Be(HotelComplaintSeverity.High);
        item.Category.Should().Be("Room");
        item.CompensationAllowed.Should().BeTrue();
    }

    [Fact]
    public void Company_profile_keeps_credit_and_contract_controls_in_its_own_master()
    {
        var item = HotelCompanyProfile.Create(new PropertyId(Guid.NewGuid()), "ACME", "ACME Travel", HotelCompanyKind.Corporate, "0101234567", "BL-01", "Nguyen Van A", "0900000000", "a@acme.test", "Ha Noi", "VND", 100000000m, 30, true, true, DateTime.Today, DateTime.Today.AddYears(1), null, DateTime.UtcNow, null);

        item.Code.Should().Be("ACME");
        item.CreditLimit.Should().Be(100000000m);
        item.RequireContract.Should().BeTrue();
        item.RequirePurchaseOrder.Should().BeTrue();
    }

    [Fact]
    public void Travel_agent_profile_keeps_iata_and_commission_reference()
    {
        var item = HotelTravelAgentProfile.Create(new PropertyId(Guid.NewGuid()), "TA001", "Agent One", HotelTravelAgentKind.TravelAgency, "IATA-01", "LIC-01", "Tran Thi B", "0911111111", "b@agent.test", "Da Nang", "TA_10", "LEISURE", "Net 30", 50000000m, DateTime.Today, DateTime.Today.AddYears(1), null, DateTime.UtcNow, null);

        item.IataCode.Should().Be("IATA-01");
        item.CommissionRuleCode.Should().Be("TA_10");
        item.DefaultMarketSegmentCode.Should().Be("LEISURE");
    }

    [Fact]
    public void Commission_rule_rejects_invalid_range_and_keeps_basis()
    {
        var invalid = () => HotelCommissionRuleProfile.Create(new PropertyId(Guid.NewGuid()), "TA_10", "Travel agent 10%", HotelCommissionPartnerKind.TravelAgent, HotelCommissionBasis.RoomAndTax, 10m, 0m, "VND", true, true, null, null, true, DateTime.Today.AddDays(1), DateTime.Today, null, DateTime.UtcNow, null);

        invalid.Should().Throw<ArgumentException>().WithMessage("HOTEL_COMMISSION_EFFECTIVE_RANGE_INVALID");
    }
}
