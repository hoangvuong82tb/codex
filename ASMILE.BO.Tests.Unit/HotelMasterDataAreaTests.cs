using ASMILE.BO.Core.FrontOffice.Foundation.Identifiers;
using ASMILE.BO.Core.FrontOffice.HotelMasterData;
using FluentAssertions;

namespace ASMILE.BO.Tests.Unit;

public sealed class HotelMasterDataAreaTests
{
    [Fact]
    public void Area_can_be_created_as_an_independent_property_master()
    {
        var propertyId = new PropertyId(Guid.NewGuid());

        var area = HotelArea.Create(propertyId, null, null, "LOBBY", "Sảnh chính", "Public", "Khu vực đón khách", 1, DateTime.UtcNow, null);

        area.PropertyId.Should().Be(propertyId.Value);
        area.Code.Should().Be("LOBBY");
        area.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Area_rejects_a_floor_without_a_building_and_room_keeps_area_id()
    {
        var propertyId = new PropertyId(Guid.NewGuid());
        var floorId = Guid.NewGuid();

        var act = () => HotelArea.Create(propertyId, null, floorId, "A", "Area", null, null, 1, DateTime.UtcNow, null);

        act.Should().Throw<ArgumentException>().WithMessage("HOTEL_AREA_BUILDING_REQUIRED_FOR_FLOOR");

        var roomTypeId = Guid.NewGuid();
        var roomAreaId = Guid.NewGuid();
        var room = HotelRoom.Create(propertyId, roomTypeId, "101", null, "F1", "B1", "AREA-1", null, DateTime.UtcNow, null, hotelAreaId: roomAreaId);

        room.HotelAreaId.Should().Be(roomAreaId);
    }

    [Fact]
    public void Reference_catalog_item_is_property_scoped_and_can_be_deactivated()
    {
        var item = HotelReferenceCatalogItem.Create(new PropertyId(Guid.NewGuid()), "booking-source", "DIRECT", "Trực tiếp", "Đặt tại quầy", 1, DateTime.UtcNow, null);

        item.CatalogKey.Should().Be("booking-source");
        item.IsActive.Should().BeTrue();

        item.Deactivate(DateTime.UtcNow, null);

        item.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Tax_and_service_charge_rules_keep_finance_settings_independent()
    {
        var rule = HotelTaxServiceChargeRule.Create(
            new PropertyId(Guid.NewGuid()),
            HotelTaxServiceChargeKind.Tax,
            "vat10",
            "VAT 10%",
            10m,
            false,
            "RoomCharge;Minibar",
            "BAR",
            "NearestDong",
            "BO:VAT_OUTPUT",
            new DateTime(2026, 9, 1),
            new DateTime(2026, 12, 31),
            "Thuế đầu ra",
            DateTime.UtcNow,
            null);

        rule.Code.Should().Be("VAT10");
        rule.Rate.Should().Be(10m);
        rule.IsInclusive.Should().BeFalse();
        rule.AccountingMapping.Should().Be("BO:VAT_OUTPUT");
        rule.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Tax_and_service_charge_rules_reject_invalid_rate_or_effective_range()
    {
        var invalidRate = () => HotelTaxServiceChargeRule.Create(
            new PropertyId(Guid.NewGuid()),
            HotelTaxServiceChargeKind.ServiceCharge,
            "SC",
            "Service charge",
            100.01m,
            true,
            "RoomCharge",
            null,
            "NearestDong",
            null,
            DateTime.Today,
            DateTime.Today.AddDays(1),
            null,
            DateTime.UtcNow,
            null);

        invalidRate.Should().Throw<ArgumentException>().WithMessage("HOTEL_TAX_SERVICE_CHARGE_RATE_INVALID");

        var invalidRange = () => HotelTaxServiceChargeRule.Create(
            new PropertyId(Guid.NewGuid()),
            HotelTaxServiceChargeKind.Tax,
            "VAT",
            "VAT",
            10m,
            false,
            "RoomCharge",
            null,
            "NearestDong",
            null,
            DateTime.Today.AddDays(1),
            DateTime.Today,
            null,
            DateTime.UtcNow,
            null);

        invalidRange.Should().Throw<ArgumentException>().WithMessage("HOTEL_TAX_SERVICE_CHARGE_EFFECTIVE_RANGE_INVALID");
    }

    [Fact]
    public void Payment_method_profile_keeps_collection_and_settlement_controls()
    {
        var profile = HotelPaymentMethodProfile.Create(new PropertyId(Guid.NewGuid()), "card-vnd", "Thẻ nội địa", HotelPaymentMethodKind.Card, "VND", "1122334455", "POS-01", 1.5m, "BO:CASH_CARD", true, true, DateTime.UtcNow, null);

        profile.Code.Should().Be("CARD-VND");
        profile.Kind.Should().Be(HotelPaymentMethodKind.Card);
        profile.FeeRate.Should().Be(1.5m);
        profile.RequireReference.Should().BeTrue();
        profile.RequireApproval.Should().BeTrue();
    }

    [Fact]
    public void Payment_method_profile_rejects_invalid_currency_or_fee()
    {
        var invalidCurrency = () => HotelPaymentMethodProfile.Create(new PropertyId(Guid.NewGuid()), "CASH", "Tiền mặt", HotelPaymentMethodKind.Cash, "V", null, null, 0m, null, false, false, DateTime.UtcNow, null);
        invalidCurrency.Should().Throw<ArgumentException>().WithMessage("HOTEL_PAYMENT_METHOD_CURRENCY_INVALID");

        var invalidFee = () => HotelPaymentMethodProfile.Create(new PropertyId(Guid.NewGuid()), "CARD", "Thẻ", HotelPaymentMethodKind.Card, "VND", null, null, 101m, null, false, false, DateTime.UtcNow, null);
        invalidFee.Should().Throw<ArgumentException>().WithMessage("HOTEL_PAYMENT_METHOD_FEE_INVALID");
    }

    [Fact]
    public void Deposit_policy_keeps_due_date_and_refund_controls()
    {
        var policy = HotelDepositPolicy.Create(new PropertyId(Guid.NewGuid()), "DEP30", "Cọc 30%", HotelDepositType.Percentage, true, 30m, 0m, 7, true, false, true, true, true, new DateTime(2026, 9, 1), new DateTime(2026, 12, 31), null, DateTime.UtcNow, null);

        policy.DepositType.Should().Be(HotelDepositType.Percentage);
        policy.Percentage.Should().Be(30m);
        policy.DueDaysBeforeArrival.Should().Be(7);
        policy.Refundable.Should().BeTrue();
        policy.AutoApplyToFolio.Should().BeTrue();
    }

    [Fact]
    public void Discount_policy_validates_roles_and_approval_level()
    {
        var policy = HotelDiscountPolicy.Create(new PropertyId(Guid.NewGuid()), "VIP10", "VIP 10%", HotelDiscountType.Percentage, 10m, 0m, 1_000_000m, "FO_SUPERVISOR", 1, true, "BO:DISCOUNT", new DateTime(2026, 9, 1), new DateTime(2026, 12, 31), null, DateTime.UtcNow, null);

        policy.Percentage.Should().Be(10m);
        policy.AllowedRoles.Should().Be("FO_SUPERVISOR");
        policy.ApprovalLevel.Should().Be(1);
        policy.ReasonRequired.Should().BeTrue();

        var invalid = () => HotelDiscountPolicy.Create(new PropertyId(Guid.NewGuid()), "BAD", "Sai", HotelDiscountType.Percentage, 10m, 0m, 0m, "", 1, true, null, DateTime.Today, DateTime.Today.AddDays(1), null, DateTime.UtcNow, null);
        invalid.Should().Throw<ArgumentException>().WithMessage("HOTEL_DISCOUNT_ALLOWED_ROLES_REQUIRED");
    }

    [Fact]
    public void Charge_code_profile_keeps_price_tax_and_bo_mapping_controls()
    {
        var charge = HotelChargeCodeProfile.Create(new PropertyId(Guid.NewGuid()), "room", "Tiền phòng", "Room", "Night", 1_000_000m, "VAT10", "SC5", "FO-ROOM", "5111", "3331", "CC-FO", false, true, true, new DateTime(2026, 9, 1), new DateTime(2026, 12, 31), null, DateTime.UtcNow, null);

        charge.Code.Should().Be("ROOM");
        charge.Price.Should().Be(1_000_000m);
        charge.TaxCode.Should().Be("VAT10");
        charge.RevenueAccount.Should().Be("5111");
        charge.AllowDiscount.Should().BeTrue();
    }

    [Fact]
    public void Room_status_rule_is_independent_and_validates_transition_policy()
    {
        var rule = HotelRoomStatusRule.Create(new PropertyId(Guid.NewGuid()), "HK-CLEAN", "Bắt đầu dọn phòng", "HK", "VacantDirty", "CleaningInProgress", "Housekeeper", "CleaningStarted", false, "Luồng dọn phòng chuẩn", 1, DateTime.UtcNow, null);

        rule.StatusDomain.Should().Be("HK");
        rule.FromStatus.Should().Be("VacantDirty");
        rule.ToStatus.Should().Be("CleaningInProgress");
        rule.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Room_status_rule_rejects_same_source_and_target_status()
    {
        var act = () => HotelRoomStatusRule.Create(new PropertyId(Guid.NewGuid()), "HK-SAME", "Không hợp lệ", "HK", "VacantDirty", "VacantDirty", null, null, false, null, 1, DateTime.UtcNow, null);

        act.Should().Throw<ArgumentException>().WithMessage("HOTEL_ROOM_STATUS_RULE_SAME_STATUS");
    }

    [Fact]
    public void Property_profile_keeps_business_date_and_operating_times_in_property_scope()
    {
        var propertyId = new PropertyId(Guid.NewGuid());
        var profile = HotelPropertyProfile.Create(propertyId, "H01", "Khách sạn mẫu", "Hà Nội", null, null, null, "Asia/Ho_Chi_Minh", "VND", 14 * 60, 12 * 60, new DateTime(2026, 9, 26), DateTime.UtcNow, null);

        profile.PropertyId.Should().Be(propertyId.Value);
        profile.CheckInMinutes.Should().Be(840);
        profile.CheckOutMinutes.Should().Be(720);
        profile.BusinessDate.Should().Be(new DateTime(2026, 9, 26));
    }

    [Fact]
    public void Stay_policy_validates_effective_dates_and_commercial_limits()
    {
        var policy = HotelStayPolicy.Create(new PropertyId(Guid.NewGuid()), "STD", "Chính sách chuẩn", new DateTime(2026, 9, 1), new DateTime(2026, 12, 31), 24, 1, 30m, 1, 14, true, false, null, DateTime.UtcNow, null);

        policy.DepositPercent.Should().Be(30m);
        policy.CancellationDeadlineHours.Should().Be(24);
        policy.ChildrenAllowed.Should().BeTrue();
    }

    [Fact]
    public void Check_in_out_rule_keeps_early_late_and_approval_settings_in_property_scope()
    {
        var rule = HotelCheckInOutRule.Create(new PropertyId(Guid.NewGuid()), "STD", "Quy tắc chuẩn", new DateTime(2026, 9, 1), new DateTime(2026, 12, 31), 120, 60, "FO_SUPERVISOR", "FO_SUPERVISOR", "EARLY", "LATE", true, true, "Cần xác nhận giấy tờ và thanh toán", DateTime.UtcNow, null);

        rule.EarlyCheckInGraceMinutes.Should().Be(120);
        rule.LateCheckOutGraceMinutes.Should().Be(60);
        rule.EarlyCheckInApprovalRole.Should().Be("FO_SUPERVISOR");
        rule.RequirePaymentAuthorization.Should().BeTrue();
    }

    [Fact]
    public void Check_in_out_rule_rejects_negative_grace_minutes()
    {
        var act = () => HotelCheckInOutRule.Create(new PropertyId(Guid.NewGuid()), "BAD", "Không hợp lệ", DateTime.Today, DateTime.Today.AddDays(1), -1, 0, null, null, null, null, false, false, null, DateTime.UtcNow, null);

        act.Should().Throw<ArgumentException>().WithMessage("HOTEL_CHECKIN_OUT_RULE_VALUES_INVALID");
    }

    [Fact]
    public void Room_move_upgrade_rule_keeps_move_upgrade_and_pricing_policy()
    {
        var rule = HotelRoomMoveUpgradeRule.Create(new PropertyId(Guid.NewGuid()), "STD", "Đổi phòng chuẩn", new DateTime(2026, 9, 1), new DateTime(2026, 12, 31), true, true, false, true, "FO_SUPERVISOR", "ChargeDifference", 2, true, null, DateTime.UtcNow, null);

        rule.AllowRoomMove.Should().BeTrue();
        rule.AllowUpgrade.Should().BeTrue();
        rule.AllowDowngrade.Should().BeFalse();
        rule.MaxClassChangeLevels.Should().Be(2);
        rule.RequireHousekeepingReady.Should().BeTrue();
    }

    [Fact]
    public void Room_move_upgrade_rule_rejects_invalid_effective_range()
    {
        var act = () => HotelRoomMoveUpgradeRule.Create(new PropertyId(Guid.NewGuid()), "BAD", "Không hợp lệ", new DateTime(2026, 10, 1), new DateTime(2026, 9, 1), true, true, false, false, null, null, 1, false, null, DateTime.UtcNow, null);

        act.Should().Throw<ArgumentException>().WithMessage("HOTEL_ROOM_MOVE_RULE_EFFECTIVE_RANGE_INVALID");
    }

    [Fact]
    public void Property_document_keeps_legal_metadata_and_required_flag()
    {
        var document = HotelPropertyDocument.Create(new PropertyId(Guid.NewGuid()), "Giấy phép lưu trú", "GP-2026-001", "Sở Du lịch", new DateTime(2026, 1, 1), new DateTime(2027, 1, 1), "docs/gp-2026-001.pdf", "Bản scan đã kiểm tra", true, DateTime.UtcNow, null);

        document.DocumentType.Should().Be("Giấy phép lưu trú");
        document.DocumentNumber.Should().Be("GP-2026-001");
        document.IsRequired.Should().BeTrue();
        document.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Property_document_rejects_expiry_before_issue_date()
    {
        var act = () => HotelPropertyDocument.Create(new PropertyId(Guid.NewGuid()), "Giấy phép", "BAD-001", null, new DateTime(2026, 10, 1), new DateTime(2026, 9, 1), null, null, false, DateTime.UtcNow, null);

        act.Should().Throw<ArgumentException>().WithMessage("HOTEL_PROPERTY_DOCUMENT_DATE_RANGE_INVALID");
    }

    [Fact]
    public void Promotion_package_keeps_booking_window_and_commercial_conditions()
    {
        var package = HotelPromotionPackage.Create(new PropertyId(Guid.NewGuid()), "WKD", "Weekend package", "Package", "Percent", 15m, new DateTime(2026, 9, 1), new DateTime(2026, 12, 31), new DateTime(2026, 8, 1), new DateTime(2026, 12, 20), 2, true, "Breakfast; airport transfer", "25/12/2026", null, DateTime.UtcNow, null);

        package.DiscountValue.Should().Be(15m);
        package.MinimumNights.Should().Be(2);
        package.RequiresPrepayment.Should().BeTrue();
        package.IncludedServices.Should().Contain("Breakfast");
    }

    [Fact]
    public void Promotion_package_rejects_percentage_above_one_hundred()
    {
        var act = () => HotelPromotionPackage.Create(new PropertyId(Guid.NewGuid()), "BAD", "Không hợp lệ", "Promotion", "Percent", 101m, DateTime.Today, DateTime.Today.AddDays(1), null, null, 1, false, null, null, null, DateTime.UtcNow, null);

        act.Should().Throw<ArgumentException>().WithMessage("HOTEL_PROMOTION_PACKAGE_VALUES_INVALID");
    }

    [Fact]
    public void Group_policy_keeps_cutoff_deposit_routing_and_rooming_list_defaults()
    {
        var policy = HotelGroupPolicy.Create(new PropertyId(Guid.NewGuid()), "CORP", "Đoàn công ty", new DateTime(2026, 9, 1), new DateTime(2026, 12, 31), 7, 3, 30m, 14, true, true, "MasterFolio", "Phòng vào master folio; dịch vụ thanh toán riêng", null, DateTime.UtcNow, null);

        policy.DefaultCutoffDays.Should().Be(7);
        policy.RoomReleaseDaysBeforeArrival.Should().Be(3);
        policy.DepositPercent.Should().Be(30m);
        policy.DefaultRouting.Should().Be("MasterFolio");
        policy.AllowRoomingListImport.Should().BeTrue();
    }

    [Fact]
    public void Group_policy_rejects_invalid_deposit_percent()
    {
        var act = () => HotelGroupPolicy.Create(new PropertyId(Guid.NewGuid()), "BAD", "Không hợp lệ", DateTime.Today, DateTime.Today.AddDays(1), 0, 0, 101m, 0, true, false, "MasterFolio", null, null, DateTime.UtcNow, null);

        act.Should().Throw<ArgumentException>().WithMessage("HOTEL_GROUP_POLICY_VALUES_INVALID");
    }

    [Fact]
    public void Cashier_shift_policy_keeps_float_closing_transfer_and_handover_controls()
    {
        var policy = HotelCashierShiftPolicy.Create(new PropertyId(Guid.NewGuid()), "FO-STD", "Ca lễ tân chuẩn", new DateTime(2026, 9, 1), new DateTime(2026, 12, 31), true, 2_000_000m, true, 100_000m, true, true, "FO_SUPERVISOR", "Bàn giao bắt buộc cuối ca", DateTime.UtcNow, null);

        policy.RequireOpeningFloat.Should().BeTrue();
        policy.DefaultOpeningFloat.Should().Be(2_000_000m);
        policy.RequireSupervisorClose.Should().BeTrue();
        policy.VarianceApprovalThreshold.Should().Be(100_000m);
        policy.AllowCashTransfer.Should().BeTrue();
        policy.RequireHandoverNote.Should().BeTrue();
    }

    [Fact]
    public void Cashier_shift_policy_rejects_negative_float_or_variance_threshold()
    {
        var act = () => HotelCashierShiftPolicy.Create(new PropertyId(Guid.NewGuid()), "BAD", "Không hợp lệ", DateTime.Today, DateTime.Today.AddDays(1), true, -1m, true, 0m, true, true, null, null, DateTime.UtcNow, null);

        act.Should().Throw<ArgumentException>().WithMessage("HOTEL_CASHIER_SHIFT_POLICY_VALUES_INVALID");
    }

    [Fact]
    public void Night_audit_policy_keeps_lock_validation_retry_and_business_date_controls()
    {
        var policy = HotelNightAuditPolicy.Create(new PropertyId(Guid.NewGuid()), "NIGHT-STD", "Night audit chuẩn", new DateTime(2026, 9, 1), new DateTime(2026, 12, 31), true, true, true, false, 3, true, true, true, "FO_SUPERVISOR", null, DateTime.UtcNow, null);

        policy.LockBusinessDateDuringAudit.Should().BeTrue();
        policy.RequireAllValidationsPassed.Should().BeTrue();
        policy.AllowResumeAfterFailure.Should().BeTrue();
        policy.MaximumRetries.Should().Be(3);
        policy.AdvanceBusinessDateOnSuccess.Should().BeTrue();
    }

    [Fact]
    public void Night_audit_policy_rejects_retry_count_outside_operational_range()
    {
        var act = () => HotelNightAuditPolicy.Create(new PropertyId(Guid.NewGuid()), "BAD", "Không hợp lệ", DateTime.Today, DateTime.Today.AddDays(1), true, true, true, false, 21, true, true, true, null, null, DateTime.UtcNow, null);

        act.Should().Throw<ArgumentException>().WithMessage("HOTEL_NIGHT_AUDIT_POLICY_RETRIES_INVALID");
    }

    [Fact]
    public void Approval_matrix_rule_keeps_threshold_role_and_segregation_controls()
    {
        var rule = HotelApprovalMatrixRule.Create(new PropertyId(Guid.NewGuid()), "REFUND-100", "Refund trên 100 nghìn", "Folio", "Refund", new DateTime(2026, 9, 1), new DateTime(2026, 12, 31), 100_000m, 1, "FO_SUPERVISOR", true, true, null, DateTime.UtcNow, null);

        rule.Domain.Should().Be("Folio");
        rule.ThresholdAmount.Should().Be(100_000m);
        rule.RequiredRole.Should().Be("FO_SUPERVISOR");
        rule.RequireDifferentApprover.Should().BeTrue();
        rule.RequireReason.Should().BeTrue();
    }

    [Fact]
    public void Approval_matrix_rule_rejects_zero_approvers()
    {
        var act = () => HotelApprovalMatrixRule.Create(new PropertyId(Guid.NewGuid()), "BAD", "Không hợp lệ", "Folio", "Void", DateTime.Today, DateTime.Today.AddDays(1), 0m, 0, "FO_SUPERVISOR", true, true, null, DateTime.UtcNow, null);

        act.Should().Throw<ArgumentException>().WithMessage("HOTEL_APPROVAL_MATRIX_RULE_VALUES_INVALID");
    }

    [Fact]
    public void Fo_permission_policy_keeps_module_action_scope_and_sensitive_flag()
    {
        var policy = HotelFOPermissionPolicy.Create(new PropertyId(Guid.NewGuid()), "RES-CANCEL", "Hủy đặt phòng", "Reservation", "Cancel", "FO_SUPERVISOR", "Property", true, new DateTime(2026, 9, 1), new DateTime(2026, 12, 31), null, DateTime.UtcNow, null);

        policy.Module.Should().Be("Reservation");
        policy.Action.Should().Be("Cancel");
        policy.RequiredRole.Should().Be("FO_SUPERVISOR");
        policy.DataScope.Should().Be("Property");
        policy.IsSensitive.Should().BeTrue();
    }

    [Fact]
    public void Fo_permission_policy_rejects_missing_scope()
    {
        var act = () => HotelFOPermissionPolicy.Create(new PropertyId(Guid.NewGuid()), "BAD", "Không hợp lệ", "Reservation", "View", "FO_AGENT", "", false, DateTime.Today, DateTime.Today.AddDays(1), null, DateTime.UtcNow, null);

        act.Should().Throw<ArgumentException>().WithMessage("HOTEL_FO_PERMISSION_POLICY_FIELDS_REQUIRED");
    }

    [Fact]
    public void Form_printer_profile_keeps_document_template_printer_and_copy_settings()
    {
        var profile = HotelFormPrinterProfile.Create(new PropertyId(Guid.NewGuid()), "RES-CONF", "Xác nhận đặt phòng", "ReservationConfirmation", "templates/reservation-confirmation.frx", "FO-Receipt", "A4", 2, true, new DateTime(2026, 9, 1), new DateTime(2026, 12, 31), null, DateTime.UtcNow, null);

        profile.DocumentType.Should().Be("ReservationConfirmation");
        profile.TemplateReference.Should().Contain("reservation-confirmation");
        profile.PrinterName.Should().Be("FO-Receipt");
        profile.Copies.Should().Be(2);
        profile.Duplex.Should().BeTrue();
    }

    [Fact]
    public void Form_printer_profile_rejects_zero_copies()
    {
        var act = () => HotelFormPrinterProfile.Create(new PropertyId(Guid.NewGuid()), "BAD", "Không hợp lệ", "Receipt", "receipt.frx", "Default", "A4", 0, false, DateTime.Today, DateTime.Today.AddDays(1), null, DateTime.UtcNow, null);

        act.Should().Throw<ArgumentException>().WithMessage("HOTEL_FORM_PRINTER_PROFILE_COPIES_INVALID");
    }

    [Fact]
    public void Messaging_integration_profile_keeps_channel_endpoint_and_safe_secret_reference()
    {
        var profile = HotelMessagingIntegrationProfile.Create(new PropertyId(Guid.NewGuid()), "FO-EMAIL", "Email xác nhận", "Email", "SMTP", "smtp://mail.internal", "secret://fo/email", "reservation-confirmed", 3, 30, new DateTime(2026, 9, 1), new DateTime(2026, 12, 31), null, DateTime.UtcNow, null);

        profile.Channel.Should().Be("Email");
        profile.Endpoint.Should().Be("smtp://mail.internal");
        profile.SecretReference.Should().Be("secret://fo/email");
        profile.MaxRetries.Should().Be(3);
        profile.TimeoutSeconds.Should().Be(30);
        profile.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Messaging_integration_profile_rejects_timeout_outside_operational_range()
    {
        var act = () => HotelMessagingIntegrationProfile.Create(new PropertyId(Guid.NewGuid()), "BAD", "Không hợp lệ", "API", "REST", "https://example.test", "secret://fo/api", null, 3, 601, DateTime.Today, DateTime.Today.AddDays(1), null, DateTime.UtcNow, null);

        act.Should().Throw<ArgumentException>().WithMessage("HOTEL_MESSAGING_INTEGRATION_PROFILE_VALUES_INVALID");
    }

    [Fact]
    public void Posted_correction_policy_keeps_amount_reason_approval_and_roles()
    {
        var policy = HotelPostedCorrectionPolicy.Create(new PropertyId(Guid.NewGuid()), "FOLIO-CORR", "Điều chỉnh folio", "Folio", 5000000, true, true, "FO_SUPERVISOR,FO_MANAGER", new DateTime(2026, 9, 1), new DateTime(2026, 12, 31), null, DateTime.UtcNow, null);

        policy.MaximumAmount.Should().Be(5000000);
        policy.RequireReason.Should().BeTrue();
        policy.RequireApproval.Should().BeTrue();
        policy.AllowedRoles.Should().Contain("FO_MANAGER");
    }

    [Fact]
    public void Posted_correction_policy_rejects_negative_amount()
    {
        var act = () => HotelPostedCorrectionPolicy.Create(new PropertyId(Guid.NewGuid()), "BAD", "Không hợp lệ", "Folio", -1, true, true, "FO_MANAGER", DateTime.Today, DateTime.Today.AddDays(1), null, DateTime.UtcNow, null);

        act.Should().Throw<ArgumentException>().WithMessage("HOTEL_POSTED_CORRECTION_POLICY_AMOUNT_INVALID");
    }

    [Fact]
    public void Business_rule_profile_keeps_declarative_condition_priority_and_modes()
    {
        var rule = HotelBusinessRuleProfile.Create(new PropertyId(Guid.NewGuid()), "NO-SHOW", "Tính phí no-show", "Reservation", "reservation.status == 'NoShow'", 10, true, true, new DateTime(2026, 9, 1), new DateTime(2026, 12, 31), null, DateTime.UtcNow, null);

        rule.ConditionReference.Should().Contain("reservation.status");
        rule.Priority.Should().Be(10);
        rule.IsBlocking.Should().BeTrue();
        rule.IsExclusive.Should().BeTrue();
    }

    [Fact]
    public void Business_rule_profile_rejects_priority_outside_safe_range()
    {
        var act = () => HotelBusinessRuleProfile.Create(new PropertyId(Guid.NewGuid()), "BAD", "Không hợp lệ", "Reservation", "ref://condition", 10001, false, false, DateTime.Today, DateTime.Today.AddDays(1), null, DateTime.UtcNow, null);

        act.Should().Throw<ArgumentException>().WithMessage("HOTEL_BUSINESS_RULE_PROFILE_PRIORITY_INVALID");
    }

    [Fact]
    public void Folio_profile_keeps_type_routing_and_split_controls_independent()
    {
        var profile = HotelFolioProfile.Create(new PropertyId(Guid.NewGuid()), "MASTER", "Master folio", HotelFolioTypeKind.Master, true, true, true, true, true, "ROOM,FB", "CASH,CARD", new DateTime(2026, 9, 1), new DateTime(2026, 12, 31), null, DateTime.UtcNow, null);

        profile.Kind.Should().Be(HotelFolioTypeKind.Master);
        profile.AutoRouting.Should().BeTrue();
        profile.AllowSplit.Should().BeTrue();
        profile.ChargeRouting.Should().Be("ROOM,FB");
        profile.PaymentRouting.Should().Be("CASH,CARD");
    }

    [Fact]
    public void Folio_profile_rejects_invalid_effective_range()
    {
        var act = () => HotelFolioProfile.Create(new PropertyId(Guid.NewGuid()), "BAD", "Không hợp lệ", HotelFolioTypeKind.Guest, true, false, true, true, true, null, null, new DateTime(2026, 12, 31), new DateTime(2026, 9, 1), null, DateTime.UtcNow, null);

        act.Should().Throw<ArgumentException>().WithMessage("HOTEL_FOLIO_EFFECTIVE_RANGE_INVALID");
    }

    [Fact]
    public void Guest_type_profile_keeps_defaults_and_required_fields_independent()
    {
        var profile = HotelGuestTypeProfile.Create(new PropertyId(Guid.NewGuid()), "CORP", "Corporate", HotelGuestTypeKind.Corporate, "FullName,Company,TaxCode", "CORP-RATE", "CREDIT", "CORP", "DIRECT", new DateTime(2026, 9, 1), new DateTime(2026, 12, 31), null, DateTime.UtcNow, null);

        profile.Kind.Should().Be(HotelGuestTypeKind.Corporate);
        profile.RequiredFields.Should().Contain("TaxCode");
        profile.DefaultRateCode.Should().Be("CORP-RATE");
        profile.DefaultPaymentMethodCode.Should().Be("CREDIT");
    }

    [Fact]
    public void Guest_type_profile_rejects_invalid_kind_or_effective_range()
    {
        var invalidKind = () => HotelGuestTypeProfile.Create(new PropertyId(Guid.NewGuid()), "BAD", "Sai", (HotelGuestTypeKind)999, null, null, null, null, null, DateTime.Today, DateTime.Today.AddDays(1), null, DateTime.UtcNow, null);
        var invalidRange = () => HotelGuestTypeProfile.Create(new PropertyId(Guid.NewGuid()), "BAD2", "Sai", HotelGuestTypeKind.Individual, null, null, null, null, null, DateTime.Today.AddDays(1), DateTime.Today, null, DateTime.UtcNow, null);

        invalidKind.Should().Throw<ArgumentException>().WithMessage("HOTEL_GUEST_TYPE_KIND_INVALID");
        invalidRange.Should().Throw<ArgumentException>().WithMessage("HOTEL_GUEST_TYPE_EFFECTIVE_RANGE_INVALID");
    }

    [Fact]
    public void Profile_type_profile_keeps_partner_defaults_independent()
    {
        var profile = HotelProfileTypeProfile.Create(new PropertyId(Guid.NewGuid()), "OTA", "Online Travel Agency", HotelProfileTypeKind.Ota, "Name,Email,Contract", "OTA-RATE", "OTA", "OTA", "BOOKING.COM", new DateTime(2026, 9, 1), new DateTime(2026, 12, 31), null, DateTime.UtcNow, null);

        profile.Kind.Should().Be(HotelProfileTypeKind.Ota);
        profile.RequiredFields.Should().Contain("Contract");
        profile.DefaultSourceCode.Should().Be("BOOKING.COM");
    }

    [Fact]
    public void Profile_type_profile_rejects_invalid_kind()
    {
        var act = () => HotelProfileTypeProfile.Create(new PropertyId(Guid.NewGuid()), "BAD", "Sai", (HotelProfileTypeKind)999, null, null, null, null, null, DateTime.Today, DateTime.Today.AddDays(1), null, DateTime.UtcNow, null);

        act.Should().Throw<ArgumentException>().WithMessage("HOTEL_PROFILE_TYPE_KIND_INVALID");
    }

    [Fact]
    public void Vip_level_profile_keeps_benefits_discount_priority_and_notification_controls()
    {
        var profile = HotelVipLevelProfile.Create(new PropertyId(Guid.NewGuid()), "VIP3", "Diamond", 3, "Welcome drink", "Fruit basket", true, 15m, 300, "Email,SMS", new DateTime(2026, 9, 1), new DateTime(2026, 12, 31), null, DateTime.UtcNow, null);

        profile.LevelNumber.Should().Be(3);
        profile.AmenityBenefit.Should().Be("Fruit basket");
        profile.DiscountPercent.Should().Be(15m);
        profile.PriorityScore.Should().Be(300);
        profile.NotificationChannels.Should().Contain("SMS");
    }

    [Fact]
    public void Vip_level_profile_rejects_discount_outside_percentage_range()
    {
        var act = () => HotelVipLevelProfile.Create(new PropertyId(Guid.NewGuid()), "BAD", "Sai", 1, null, null, false, 101m, 0, null, DateTime.Today, DateTime.Today.AddDays(1), null, DateTime.UtcNow, null);

        act.Should().Throw<ArgumentException>().WithMessage("HOTEL_VIP_LEVEL_DISCOUNT_INVALID");
    }
}
