using ASMILE.BO.Application.FrontOffice.Folio;
using ASMILE.BO.Application.FrontOffice.Foundation.Contexts;
using ASMILE.BO.Application.FrontOffice.Foundation.Cqrs;
using ASMILE.BO.Application.FrontOffice.Foundation.Persistence;
using ASMILE.BO.Application.FrontOffice.Stay;
using ASMILE.BO.Application.Interfaces;
using ASMILE.BO.Core.FrontOffice.Folio;
using ASMILE.BO.Core.FrontOffice.Foundation.DomainEvents;
using ASMILE.BO.Core.FrontOffice.Foundation.Identifiers;
using ASMILE.BO.Core.FrontOffice.Payment;
using ASMILE.BO.Shared.Foundation;
using FluentAssertions;

namespace ASMILE.BO.Tests.Unit;

public sealed class HotelFolioTests
{
    [Fact]
    public void FOLIO_DOM_U_001_create_folio_opens_in_open_status()
    {
        var folio = HotelFolio.Create(Guid.NewGuid(), Guid.NewGuid(), "FOL-001", DateTime.UtcNow, null);
        folio.Status.Should().Be(HotelFolioStatus.Open);
        folio.FolioNumber.Should().Be("FOL-001");
        folio.RunningBalance.Should().Be(0);
    }

    [Fact]
    public void FOLIO_DOM_U_002_add_room_charge_increases_balance()
    {
        var folio = HotelFolio.Create(Guid.NewGuid(), Guid.NewGuid(), "FOL-002", DateTime.UtcNow, null);
        folio.AddRoomCharge(500000, "Nightly rate 2026-07-18", new DateOnly(2026, 7, 18), null, DateTime.UtcNow, null);
        folio.RunningBalance.Should().Be(500000);
        folio.TotalCharges.Should().Be(500000);
        folio.TotalPayments.Should().Be(0);
        folio.Entries.Should().ContainSingle(x => x.EntryType == HotelFolioEntryType.RoomCharge);
    }

    [Fact]
    public void FOLIO_DOM_U_003_payment_reduces_balance()
    {
        var folio = HotelFolio.Create(Guid.NewGuid(), Guid.NewGuid(), "FOL-003", DateTime.UtcNow, null);
        folio.AddRoomCharge(500000, "Room", new DateOnly(2026, 7, 18), null, DateTime.UtcNow, null);
        folio.AddPayment(300000, "Partial payment", HotelPaymentMethod.Cash, null, DateTime.UtcNow, null);
        folio.RunningBalance.Should().Be(200000);
        folio.TotalPayments.Should().Be(300000);
    }

    [Fact]
    public void FOLIO_DOM_U_004_tax_and_service_charge()
    {
        var folio = HotelFolio.Create(Guid.NewGuid(), Guid.NewGuid(), "FOL-004", DateTime.UtcNow, null);
        folio.AddRoomCharge(500000, "Room", new DateOnly(2026, 7, 18), null, DateTime.UtcNow, null);
        folio.AddTax(50000, "VAT 10%", DateTime.UtcNow, null);
        folio.AddServiceCharge(25000, "Service 5%", DateTime.UtcNow, null);
        folio.RunningBalance.Should().Be(575000);
    }

    [Fact]
    public void FOLIO_DOM_U_005_discount_reduces_balance()
    {
        var folio = HotelFolio.Create(Guid.NewGuid(), Guid.NewGuid(), "FOL-005", DateTime.UtcNow, null);
        folio.AddRoomCharge(500000, "Room", new DateOnly(2026, 7, 18), null, DateTime.UtcNow, null);
        folio.AddDiscount(100000, "Loyalty discount 20%", DateTime.UtcNow, null);
        folio.RunningBalance.Should().Be(400000);
    }

    [Fact]
    public void FOLIO_DOM_U_006_correction_reverses_original()
    {
        var folio = HotelFolio.Create(Guid.NewGuid(), Guid.NewGuid(), "FOL-006", DateTime.UtcNow, null);
        var charge = folio.AddRoomCharge(500000, "Room", new DateOnly(2026, 7, 18), null, DateTime.UtcNow, null);
        folio.AddCorrection(-500000, "Wrong rate - correcting", charge.HotelFolioEntryId, DateTime.UtcNow, null);
        folio.RunningBalance.Should().Be(0);
    }

    [Fact]
    public void FOLIO_DOM_U_007_close_and_settle_requires_open()
    {
        var folio = HotelFolio.Create(Guid.NewGuid(), Guid.NewGuid(), "FOL-007", DateTime.UtcNow, null);
        folio.AddRoomCharge(500000, "Room", new DateOnly(2026, 7, 18), null, DateTime.UtcNow, null);
        folio.AddPayment(500000, "Full settlement", HotelPaymentMethod.Card, null, DateTime.UtcNow, null);
        folio.Close(DateTime.UtcNow, null);
        folio.Status.Should().Be(HotelFolioStatus.Closed);
        folio.Settle(DateTime.UtcNow, null);
        folio.Status.Should().Be(HotelFolioStatus.Settled);
    }

    [Fact]
    public void FOLIO_DOM_U_008_closed_folio_cannot_post()
    {
        var folio = HotelFolio.Create(Guid.NewGuid(), Guid.NewGuid(), "FOL-008", DateTime.UtcNow, null);
        folio.Close(DateTime.UtcNow, null);
        Action post = () => folio.AddRoomCharge(100, "Attempt post", new DateOnly(2026, 7, 18), null, DateTime.UtcNow, null);
        post.Should().Throw<InvalidOperationException>().WithMessage("FOLIO_NOT_MUTABLE*");
    }

    [Fact]
    public void FOLIO_DOM_U_009_transfer_between_folios()
    {
        var from = HotelFolio.Create(Guid.NewGuid(), Guid.NewGuid(), "FOL-009A", DateTime.UtcNow, null);
        var to = HotelFolio.Create(Guid.NewGuid(), Guid.NewGuid(), "FOL-009B", DateTime.UtcNow, null);
        from.AddRoomCharge(500000, "Room", new DateOnly(2026, 7, 18), null, DateTime.UtcNow, null);
        from.AddManualCredit(200000, "Transfer to FOL-009B", DateTime.UtcNow, null);
        to.AddRoomCharge(200000, "Transferred from FOL-009A", new DateOnly(2026, 7, 18), null, DateTime.UtcNow, null);
        from.RunningBalance.Should().Be(300000);
        to.RunningBalance.Should().Be(200000);
    }

    [Fact]
    public void FOLIO_DOM_U_010_revenue_category_creation()
    {
        var cat = RevenueCategory.Create(Guid.NewGuid(), "ROOM", "Room Revenue", RevenueBucket.Rooms, 10, false);
        cat.Code.Should().Be("ROOM");
        cat.Bucket.Should().Be(RevenueBucket.Rooms);
        cat.TaxRate.Should().Be(10);
        cat.IsTaxInclusive.Should().BeFalse();
    }

    [Fact]
    public void FOLIO_DOM_U_011_tax_rule_calculates_correctly()
    {
        var rule = TaxRule.Create(Guid.NewGuid(), TaxRuleType.Vat, "VAT 10%", 10, RevenueBucket.Rooms);
        rule.Calculate(500000).Should().Be(50000);
    }

    [Fact]
    public void FOLIO_DOM_U_012_tax_rule_respects_min_max()
    {
        var rule = TaxRule.Create(Guid.NewGuid(), TaxRuleType.CityTax, "City Tax", 5, RevenueBucket.Rooms, min: 10000, max: 50000);
        rule.Calculate(100000).Should().Be(10000);
        rule.Calculate(2000000).Should().Be(50000);
    }
}
