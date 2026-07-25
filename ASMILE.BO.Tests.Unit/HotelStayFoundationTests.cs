using ASMILE.BO.Application.FrontOffice.Foundation.Audit;
using ASMILE.BO.Application.FrontOffice.Foundation.Contexts;
using ASMILE.BO.Application.FrontOffice.Foundation.Cqrs;
using ASMILE.BO.Application.FrontOffice.Foundation.Persistence;
using ASMILE.BO.Application.FrontOffice.HotelMasterData;
using ASMILE.BO.Application.FrontOffice.Reservation;
using ASMILE.BO.Application.FrontOffice.Stay;
using ASMILE.BO.Application.Interfaces;
using ASMILE.BO.Core.FrontOffice.Foundation.DomainEvents;
using ASMILE.BO.Core.FrontOffice.Foundation.Identifiers;
using ASMILE.BO.Core.FrontOffice.HotelMasterData;
using ASMILE.BO.Core.FrontOffice.Reservation;
using ASMILE.BO.Core.FrontOffice.Stay;
using ASMILE.BO.Core.Security;
using ASMILE.BO.Infrastructure.FrontOffice.Foundation;
using ASMILE.BO.Shared.Foundation;
using FluentAssertions;
using ReservationAggregate = ASMILE.BO.Core.FrontOffice.Reservation.Reservation;

namespace ASMILE.BO.Tests.Unit;

public sealed class HotelStayFoundationTests
{
    [Fact]
    public void STAY_DOM_U_001_to_007_create_pending_arrival_and_enforce_required_invariants()
    {
        var stay = ValidStay();
        stay.Status.Should().Be(HotelStayStatus.PendingArrival);
        stay.PropertyId.Should().NotBeEmpty();
        stay.RowVersion.Should().BeNull();
        stay.DomainEvents.Should().ContainSingle(x => x.EventName == "StayPrepared");
        Action invalidDates = () => HotelStayPeriod.Create(new DateOnly(2026, 7, 18), new DateOnly(2026, 7, 18));
        Action invalidAdults = () => HotelStayGuestCount.Create(0, 0);
        Action invalidChildren = () => HotelStayGuestCount.Create(1, -1);
        Action noGuest = () => HotelStay.Prepare("STY-X", PropertyId.New(), Guid.NewGuid(), "RSV-X", Guid.NewGuid(), " ", null, Guid.NewGuid(), "101", Guid.NewGuid(), "STD", new(new(2026, 7, 18), new(2026, 7, 19)), new(1, 0), "Reservation", DateTime.UtcNow, null);
        Action noRoom = () => HotelStay.Prepare("STY-X", PropertyId.New(), Guid.NewGuid(), "RSV-X", Guid.NewGuid(), "Guest", null, Guid.Empty, "101", Guid.NewGuid(), "STD", new(new(2026, 7, 18), new(2026, 7, 19)), new(1, 0), "Reservation", DateTime.UtcNow, null);
        invalidDates.Should().Throw<ArgumentException>().WithMessage("STAY_INVALID_DATE_RANGE"); invalidAdults.Should().Throw<ArgumentException>().WithMessage("STAY_INVALID_GUEST_COUNT"); invalidChildren.Should().Throw<ArgumentException>().WithMessage("STAY_INVALID_GUEST_COUNT"); noGuest.Should().Throw<ArgumentException>().WithMessage("STAY_REQUIRED_FIELD"); noRoom.Should().Throw<ArgumentException>().WithMessage("STAY_ROOM_REQUIRED");
    }

    [Fact]
    public void STAY_DOM_U_008_to_010_transitions_are_guarded_property_is_immutable_and_rowversion_has_no_setter()
    {
        var stay = ValidStay();
        Action checkoutBeforeCheckin = () => stay.MarkCheckedOut(DateTime.UtcNow, null);
        checkoutBeforeCheckin.Should().Throw<InvalidOperationException>().WithMessage("STAY_INVALID_STATE*");
        stay.MarkInHouse(DateTime.UtcNow, null); stay.MarkCheckedOut(DateTime.UtcNow.AddDays(1), null);
        stay.Status.Should().Be(HotelStayStatus.CheckedOut);
        typeof(HotelStay).GetProperty(nameof(HotelStay.PropertyId))!.SetMethod!.IsPublic.Should().BeFalse();
        typeof(HotelStay).GetProperty(nameof(HotelStay.RowVersion))!.SetMethod!.IsPublic.Should().BeFalse();
    }

    [Fact]
    public async Task STAY_APP_U_001_valid_confirmed_reservation_prepares_stay_with_trusted_context_and_clock()
    {
        var setup = Setup.Valid();
        using var _ = setup.Accessor.BeginScope(setup.Context);
        var result = await setup.Service.PrepareFromReservationAsync(new PrepareStayFromReservationCommand(setup.Reservation.ReservationId), default);
        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be(HotelStayStatus.PendingArrival);
        result.Value.StayNumber.Should().Be("STY-202607-000001");
        setup.Persistence.Events.Should().ContainSingle(x => x.EventName == "StayPrepared");
        setup.Persistence.Audits.Should().ContainSingle(x => x.Action == "StayPrepared");
        setup.NumberGenerator.Calls.Should().Be(1);
    }

    [Theory]
    [InlineData("missing-reservation", "STAY_RESERVATION_NOT_FOUND")]
    [InlineData("wrong-property", "STAY_RESERVATION_NOT_FOUND")]
    [InlineData("ineligible", "STAY_RESERVATION_NOT_ELIGIBLE")]
    [InlineData("no-room", "STAY_ROOM_REQUIRED")]
    [InlineData("missing-room", "STAY_ROOM_NOT_FOUND")]
    [InlineData("inactive-room", "STAY_ROOM_NOT_ASSIGNABLE")]
    [InlineData("out-of-order", "STAY_ROOM_NOT_ASSIGNABLE")]
    [InlineData("out-of-service", "STAY_ROOM_NOT_ASSIGNABLE")]
    [InlineData("type-mismatch", "STAY_ROOM_TYPE_MISMATCH")]
    [InlineData("duplicate", "STAY_ALREADY_EXISTS_FOR_RESERVATION")]
    public async Task STAY_APP_U_002_to_011_invalid_preparation_has_machine_readable_error_and_no_side_effects(string scenario, string errorCode)
    {
        var setup = Setup.Valid(); setup.Apply(scenario);
        using var _ = setup.Accessor.BeginScope(setup.Context);
        var result = await setup.Service.PrepareFromReservationAsync(new PrepareStayFromReservationCommand(setup.Reservation.ReservationId), default);
        result.IsFailure.Should().BeTrue(); result.Error!.Code.Should().Be(errorCode);
        setup.Stays.AddCalls.Should().Be(0); setup.Persistence.Events.Should().BeEmpty(); setup.Persistence.Audits.Should().BeEmpty();
    }

    [Fact]
    public async Task STAY_APP_U_012_permission_is_checked_before_mutation()
    {
        var setup = Setup.Valid(allowed: false);
        using var _ = setup.Accessor.BeginScope(setup.Context);
        var result = await setup.Service.PrepareFromReservationAsync(new PrepareStayFromReservationCommand(setup.Reservation.ReservationId), default);
        result.Error!.Code.Should().Be("STAY_PERMISSION_DENIED"); setup.Stays.AddCalls.Should().Be(0);
    }

    [Fact]
    public async Task STAY_APP_U_013_to_015_queries_use_property_context_and_business_date_without_client_property()
    {
        var setup = Setup.Valid();
        setup.Stays.Arrivals = [new HotelStayDto(Guid.NewGuid(), "STY", setup.Context.Property.PropertyId.Value, null, null, null, "Guest", Guid.NewGuid(), "101", Guid.NewGuid(), "STD", setup.Context.BusinessDate.BusinessDate, setup.Context.BusinessDate.BusinessDate.AddDays(1), null, null, 1, 0, HotelStayStatus.PendingArrival, "Reservation", 0, DateTime.UtcNow, DateTime.UtcNow, [])];
        using var _ = setup.Accessor.BeginScope(setup.Context);
        var result = await new GetTodayExpectedArrivalsQueryHandler(setup.Stays, setup.Accessor, new Authorization(true)).HandleAsync(new GetTodayExpectedArrivalsQuery(), default);
        result.IsSuccess.Should().BeTrue(); setup.Stays.LastBusinessDate.Should().Be(setup.Context.BusinessDate.BusinessDate);
    }

    [Fact]
    public void CHECKIN_DOM_U_001_to_010_pending_arrival_checkin_preserves_snapshots_and_raises_event()
    {
        var stay = ValidStay();
        var propertyId = stay.PropertyId;
        var roomId = stay.RoomId;
        var reservationId = stay.ReservationId;
        var roomNumber = stay.RoomNumberSnapshot;
        var reservationNumber = stay.ReservationNumberSnapshot;
        var checkedInAt = new DateTime(2026, 7, 18, 10, 0, 0, DateTimeKind.Utc);

        stay.CheckIn(checkedInAt, ActorId.New());

        stay.Status.Should().Be(HotelStayStatus.InHouse);
        stay.ActualCheckInAtUtc.Should().Be(checkedInAt);
        stay.DomainEvents.Should().ContainSingle(x => x.EventName == "StayCheckedIn");
        stay.PropertyId.Should().Be(propertyId);
        stay.RoomId.Should().Be(roomId);
        stay.ReservationId.Should().Be(reservationId);
        stay.RoomNumberSnapshot.Should().Be(roomNumber);
        stay.ReservationNumberSnapshot.Should().Be(reservationNumber);

        Action twice = () => stay.CheckIn(checkedInAt, ActorId.New());
        twice.Should().Throw<InvalidOperationException>().WithMessage("STAY_INVALID_STATE*");

        var cancelled = ValidStay(); cancelled.CancelPreparation(checkedInAt, null);
        Action cancelledCheckIn = () => cancelled.CheckIn(checkedInAt, null);
        cancelledCheckIn.Should().Throw<InvalidOperationException>().WithMessage("STAY_INVALID_STATE*");
        var checkedOut = ValidStay(); checkedOut.CheckIn(checkedInAt, null); checkedOut.MarkCheckedOut(checkedInAt.AddDays(1), null);
        Action checkedOutCheckIn = () => checkedOut.CheckIn(checkedInAt, null);
        checkedOutCheckIn.Should().Throw<InvalidOperationException>().WithMessage("STAY_INVALID_STATE*");
    }

    [Theory]
    [InlineData("missing", "STAY_NOT_FOUND")]
    [InlineData("missing-rowversion", "STAY_ROWVERSION_REQUIRED")]
    [InlineData("invalid-rowversion", "STAY_ROWVERSION_INVALID")]
    [InlineData("not-pending", "STAY_NOT_PENDING_ARRIVAL")]
    [InlineData("missing-room", "STAY_ROOM_REQUIRED")]
    [InlineData("inactive-room", "STAY_ROOM_NOT_ASSIGNABLE")]
    [InlineData("out-of-order", "STAY_ROOM_NOT_ASSIGNABLE")]
    [InlineData("out-of-service", "STAY_ROOM_NOT_ASSIGNABLE")]
    [InlineData("type-mismatch", "STAY_ROOM_TYPE_MISMATCH")]
    [InlineData("occupied", "STAY_ROOM_ALREADY_OCCUPIED")]
    public async Task CHECKIN_APP_U_002_to_019_rejections_are_side_effect_free(string scenario, string errorCode)
    {
        var setup = Setup.Valid();
        setup.ApplyCheckIn(scenario);
        using var _ = setup.Accessor.BeginScope(setup.Context);
        var result = await setup.Service.CheckInAsync(new CheckInHotelStayCommand(setup.CheckInStay.HotelStayId, scenario == "missing-rowversion" ? [] : scenario == "invalid-rowversion" ? [1] : new byte[8]), default);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be(errorCode);
        setup.Stays.Histories.Should().BeEmpty();
        setup.Persistence.Events.Should().BeEmpty();
        setup.Persistence.Audits.Should().BeEmpty();
        setup.Persistence.Commits.Should().Be(0);
    }

    [Fact]
    public async Task CHECKIN_APP_U_001_to_020_valid_checkin_uses_clock_and_commits_once()
    {
        var setup = Setup.Valid();
        using var _ = setup.Accessor.BeginScope(setup.Context);
        var result = await setup.Service.CheckInAsync(new CheckInHotelStayCommand(setup.CheckInStay.HotelStayId, new byte[8]), default);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be(HotelStayStatus.InHouse);
        result.Value.ActualCheckInAtUtc.Should().Be(setup.Clock.UtcNow);
        setup.Stays.Histories.Should().ContainSingle(x => x.Action == "StayCheckedIn");
        setup.Persistence.Events.Should().ContainSingle(x => x.EventName == "StayCheckedIn");
        setup.Persistence.Audits.Should().ContainSingle(x => x.Action == "StayCheckedIn");
        setup.Persistence.Commits.Should().Be(1);
    }

    [Fact]
    public async Task PRECHECKIN_APP_U_001_valid_pre_check_in_completes_with_all_fields()
    {
        var setup = Setup.Valid();
        using var _ = setup.Accessor.BeginScope(setup.Context);
        var eta = new DateTime(2026, 7, 18, 14, 0, 0, DateTimeKind.Utc);
        var result = await setup.Service.PreCheckInAsync(new PreCheckInHotelStayCommand(setup.CheckInStay.HotelStayId, new byte[8], eta, true, true, true), default);
        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be(HotelStayStatus.PreArrival);
        setup.Stays.Aggregate!.IsIdentityVerified.Should().BeTrue();
        setup.Stays.Aggregate.AreSpecialRequestsConfirmed.Should().BeTrue();
        setup.Stays.Aggregate.AreRoomPreferencesConfirmed.Should().BeTrue();
        setup.Stays.Aggregate.Eta.Should().Be(eta);
        setup.Stays.Aggregate.PreCheckInCompletedAtUtc.Should().Be(setup.Clock.UtcNow);
        setup.Persistence.Events.Should().ContainSingle(x => x.EventName == "StayPreCheckInCompleted");
        setup.Persistence.Audits.Should().ContainSingle(x => x.Action == "StayPreCheckInCompleted");
        setup.Persistence.Commits.Should().Be(1);
    }

    [Theory]
    [InlineData("not-pending", "STAY_INVALID_STATE")]
    [InlineData("missing-rowversion", "STAY_ROWVERSION_REQUIRED")]
    public async Task PRECHECKIN_APP_U_002_to_003_rejections(string scenario, string errorCode)
    {
        var setup = Setup.Valid();
        if (scenario == "not-pending") setup.ApplyCheckIn(scenario);
        using var _ = setup.Accessor.BeginScope(setup.Context);
        var token = scenario == "missing-rowversion" ? [] : new byte[8];
        var result = await setup.Service.PreCheckInAsync(new PreCheckInHotelStayCommand(setup.CheckInStay.HotelStayId, token), default);
        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be(errorCode);
        setup.Persistence.Events.Should().BeEmpty();
        setup.Persistence.Audits.Should().BeEmpty();
    }

    [Fact]
    public void PRECHECKIN_DOM_U_001_identity_not_verified_throws()
    {
        var stay = ValidStay();
        Action act = () => stay.PreCheckIn(DateTime.UtcNow, null, identityVerified: false);
        act.Should().Throw<ArgumentException>().WithMessage("PRE_CHECKIN_IDENTITY_REQUIRED");
    }

    [Fact]
    public void PRECHECKIN_DOM_U_002_pre_check_in_from_pending_arrival_transitions_to_pre_arrival()
    {
        var stay = ValidStay();
        stay.PreCheckIn(DateTime.UtcNow, ActorId.New(), new DateTime(2026, 7, 18, 15, 0, 0, DateTimeKind.Utc));
        stay.Status.Should().Be(HotelStayStatus.PreArrival);
        stay.IsIdentityVerified.Should().BeTrue();
        stay.PreCheckInCompletedAtUtc.Should().NotBeNull();
        stay.DomainEvents.Should().ContainSingle(x => x.EventName == "StayPreCheckInCompleted");
    }

    [Fact]
    public void STAY_DOM_U_011_dnd_toggle_requires_inhouse_status()
    {
        var stay = ValidStay();
        Action act = () => stay.ToggleDoNotDisturb(true, DateTime.UtcNow, null);
        act.Should().Throw<InvalidOperationException>().WithMessage("STAY_INVALID_STATE*");
        stay.CheckIn(DateTime.UtcNow, ActorId.New());
        stay.ToggleDoNotDisturb(true, DateTime.UtcNow, null);
        stay.IsDoNotDisturb.Should().BeTrue();
    }

    [Fact]
    public void STAY_DOM_U_012_wake_up_call_requested_adds_to_collection()
    {
        var stay = ValidStay();
        var call = stay.RequestWakeUpCall(new DateTime(2026, 7, 19, 6, 0, 0, DateTimeKind.Utc), "Wake the guest", DateTime.UtcNow, null);
        stay.WakeUpCalls.Should().ContainSingle().Which.CallNumber.Should().Be(1);
        stay.WakeUpCalls.Single().RequestedAtUtc.Hour.Should().Be(6);
    }

    [Fact]
    public void STAY_DOM_U_013_complaint_requires_category_and_description()
    {
        var stay = ValidStay();
        Action noCategory = () => stay.LogComplaint("", "desc", DateTime.UtcNow, null);
        noCategory.Should().Throw<ArgumentException>().WithMessage("COMPLAINT_CATEGORY_REQUIRED");
        Action noDesc = () => stay.LogComplaint("cat", "", DateTime.UtcNow, null);
        noDesc.Should().Throw<ArgumentException>().WithMessage("COMPLAINT_DESCRIPTION_REQUIRED");
        var complaint = stay.LogComplaint("Noise", "Too loud", DateTime.UtcNow, null);
        stay.Complaints.Should().ContainSingle().Which.Category.Should().Be("Noise");
    }

    [Fact]
    public void STAY_DOM_U_014_tags_are_added_and_removed()
    {
        var stay = ValidStay();
        stay.AddTag("VIP", DateTime.UtcNow, null);
        stay.Tags.Should().Contain("VIP");
        stay.AddTag("VIP", DateTime.UtcNow, null);
        stay.Tags.Should().HaveCount(1);
        stay.RemoveTag("VIP", DateTime.UtcNow, null);
        stay.Tags.Should().BeEmpty();
    }

    [Fact]
    public void STAY_DOM_U_015_house_use_and_vip_service_flags()
    {
        var stay = ValidStay();
        stay.MarkHouseUse(true, DateTime.UtcNow, null);
        stay.IsHouseUse.Should().BeTrue();
        stay.MarkVipService(true, DateTime.UtcNow, null);
        stay.IsVipService.Should().BeTrue();
    }

    [Fact]
    public async Task INHOUSE_APP_U_001_wake_up_call_and_complaint_are_audited()
    {
        var setup = Setup.Valid();
        using var _ = setup.Accessor.BeginScope(setup.Context);
        await setup.Service.RequestWakeUpCallAsync(new RequestWakeUpCallCommand(setup.CheckInStay.HotelStayId, new DateTime(2026, 7, 19, 6, 0, 0, DateTimeKind.Utc), null, new byte[8]), default);
        setup.Persistence.Events.Should().ContainSingle(x => x.EventName == "StayWakeUpCallRequested");
        setup.Persistence.Audits.Should().ContainSingle(x => x.Action == "StayWakeUpCallRequested");
    }

    [Fact]
    public async Task INHOUSE_APP_U_002_complaint_is_logged_with_history()
    {
        var setup = Setup.Valid();
        using var _ = setup.Accessor.BeginScope(setup.Context);
        await setup.Service.LogComplaintAsync(new LogComplaintCommand(setup.CheckInStay.HotelStayId, "Noise", "Too loud at night", new byte[8]), default);
        setup.Stays.Aggregate!.Complaints.Should().ContainSingle(x => x.Category == "Noise");
        setup.Persistence.Commits.Should().Be(1);
    }

    // =============================================================================
    // ROOM MOVE TESTS (Phase 2.3)
    // =============================================================================

    [Fact]
    public void ROOMMOVE_DOM_U_001_same_room_is_rejected()
    {
        var stay = ValidStay(); stay.CheckIn(DateTime.UtcNow, ActorId.New());
        Action same = () => stay.MoveRoom(stay.RoomId, stay.RoomNumberSnapshot, stay.RoomTypeId, "test", DateTime.UtcNow, null);
        same.Should().Throw<ArgumentException>().WithMessage("ROOM_MOVE_SAME_ROOM");
    }

    [Fact]
    public void ROOMMOVE_DOM_U_002_empty_target_or_reason_rejected()
    {
        var stay = ValidStay(); stay.CheckIn(DateTime.UtcNow, ActorId.New());
        Action emptyTarget = () => stay.MoveRoom(Guid.Empty, "102", Guid.NewGuid(), "reason", DateTime.UtcNow, null);
        emptyTarget.Should().Throw<ArgumentException>().WithMessage("ROOM_MOVE_TARGET_REQUIRED");
        Action emptyReason = () => stay.MoveRoom(Guid.NewGuid(), "102", Guid.NewGuid(), "", DateTime.UtcNow, null);
        emptyReason.Should().Throw<ArgumentException>().WithMessage("ROOM_MOVE_REASON_REQUIRED");
    }

    [Fact]
    public void ROOMMOVE_DOM_U_003_not_occupying_is_rejected()
    {
        var stay = ValidStay();
        Action act = () => stay.MoveRoom(Guid.NewGuid(), "102", Guid.NewGuid(), "reason", DateTime.UtcNow, null);
        act.Should().Throw<InvalidOperationException>().WithMessage("STAY_INVALID_STATE*");
    }

    [Fact]
    public void ROOMMOVE_DOM_U_004_upgrade_detected_by_display_order()
    {
        var stay = ValidStay(); stay.CheckIn(DateTime.UtcNow, ActorId.New());
        stay.MoveRoom(Guid.NewGuid(), "201", Guid.NewGuid(), "Guest requested upgrade", DateTime.UtcNow, null, toRoomTypeDisplayOrder: 1, fromRoomTypeDisplayOrder: 5);
        stay.RoomMoves.Should().ContainSingle().Which.MoveType.Should().Be(RoomMoveType.Upgrade);
    }

    [Fact]
    public void ROOMMOVE_DOM_U_005_downgrade_detected_by_display_order()
    {
        var stay = ValidStay(); stay.CheckIn(DateTime.UtcNow, ActorId.New());
        stay.MoveRoom(Guid.NewGuid(), "301", Guid.NewGuid(), "Guest requested downgrade", DateTime.UtcNow, null, toRoomTypeDisplayOrder: 9, fromRoomTypeDisplayOrder: 3);
        stay.RoomMoves.Should().ContainSingle().Which.MoveType.Should().Be(RoomMoveType.Downgrade);
    }

    [Fact]
    public void ROOMMOVE_DOM_U_006_reason_keywords_detect_move_type()
    {
        var stay = ValidStay(); stay.CheckIn(DateTime.UtcNow, ActorId.New());
        stay.MoveRoom(Guid.NewGuid(), "M01", Guid.NewGuid(), "Maintenance required - AC broken", DateTime.UtcNow, null);
        stay.RoomMoves.Should().ContainSingle().Which.MoveType.Should().Be(RoomMoveType.Maintenance);
    }

    [Fact]
    public void ROOMMOVE_DOM_U_007_emergency_move_detected()
    {
        var stay = ValidStay(); stay.CheckIn(DateTime.UtcNow, ActorId.New());
        stay.MoveRoom(Guid.NewGuid(), "E01", Guid.NewGuid(), "Emergency - water leak", DateTime.UtcNow, null);
        stay.RoomMoves.Should().ContainSingle().Which.MoveType.Should().Be(RoomMoveType.Emergency);
    }

    [Fact]
    public void ROOMMOVE_DOM_U_008_vip_move_detected()
    {
        var stay = ValidStay(); stay.CheckIn(DateTime.UtcNow, ActorId.New());
        stay.MoveRoom(Guid.NewGuid(), "VIP1", Guid.NewGuid(), "VIP upgrade - presidential suite", DateTime.UtcNow, null);
        stay.RoomMoves.Should().ContainSingle().Which.MoveType.Should().Be(RoomMoveType.Vip);
    }

    [Fact]
    public void ROOMMOVE_DOM_U_009_out_of_order_move_detected()
    {
        var stay = ValidStay(); stay.CheckIn(DateTime.UtcNow, ActorId.New());
        stay.MoveRoom(Guid.NewGuid(), "R01", Guid.NewGuid(), "Room out of order - OOO - heating failure", DateTime.UtcNow, null);
        stay.RoomMoves.Should().ContainSingle().Which.MoveType.Should().Be(RoomMoveType.OutOfOrder);
    }

    [Fact]
    public void ROOMMOVE_DOM_U_010_multiple_moves_accumulate()
    {
        var stay = ValidStay(); stay.CheckIn(DateTime.UtcNow, ActorId.New());
        stay.MoveRoom(Guid.NewGuid(), "201", Guid.NewGuid(), "First move", DateTime.UtcNow, null);
        stay.MoveRoom(Guid.NewGuid(), "301", Guid.NewGuid(), "Second move - Maintenance", DateTime.UtcNow, null);
        stay.RoomMoves.Should().HaveCount(2);
        stay.RoomMoves.First().MoveType.Should().Be(RoomMoveType.SameType);
        stay.RoomMoves.Last().MoveType.Should().Be(RoomMoveType.Maintenance);
    }

    [Fact]
    public async Task ROOMMOVE_APP_U_001_valid_move_commits_with_history()
    {
        var setup = Setup.Valid();
        using var _ = setup.Accessor.BeginScope(setup.Context);
        await setup.Service.CheckInAsync(new CheckInHotelStayCommand(setup.CheckInStay.HotelStayId, new byte[8]), default);
        setup.CheckInStay.ClearDomainEvents(); setup.Persistence.Events.Clear(); setup.Persistence.Audits.Clear();
        var toRoomId = Guid.NewGuid();
        setup.Rooms.Room = new HotelRoomDto(toRoomId, "201", null, setup.CheckInStay.RoomTypeId, "STD", "Standard", null, null, null, HotelRoomOperationalStatus.Available, true, setup.Clock.UtcNow, []);
        setup.Rooms.RoomType = new HotelRoomTypeDto(setup.CheckInStay.RoomTypeId, "STD", "Standard", null, 2, 1, 3, 2, 2, 1, true, setup.Clock.UtcNow, []);
        var result = await setup.Service.MoveRoomAsync(new MoveRoomCommand(setup.CheckInStay.HotelStayId, toRoomId, "Upgrade to better room", new byte[8]), default);
        result.IsSuccess.Should().BeTrue();
        setup.Stays.Aggregate!.RoomMoves.Should().ContainSingle();
        setup.Persistence.Commits.Should().BeGreaterThan(1);
    }

    [Fact]
    public async Task ROOMMOVE_APP_U_002_unavailable_room_is_rejected()
    {
        var setup = Setup.Valid();
        using var _ = setup.Accessor.BeginScope(setup.Context);
        await setup.Service.CheckInAsync(new CheckInHotelStayCommand(setup.CheckInStay.HotelStayId, new byte[8]), default);
        var result = await setup.Service.MoveRoomAsync(new MoveRoomCommand(setup.CheckInStay.HotelStayId, Guid.NewGuid(), "reason", new byte[8]), default);
        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("STAY_ROOM_NOT_ASSIGNABLE");
    }

    // =============================================================================
    // DEPARTURE TESTS (Phase 2.4)
    // =============================================================================

    [Fact]
    public void DEPARTURE_DOM_U_001_checkout_records_departure_reason_and_notes()
    {
        var stay = ValidStay(); stay.CheckIn(DateTime.UtcNow, ActorId.New());
        stay.MarkCheckedOut(DateTime.UtcNow.AddDays(1), null, DepartureType.Normal, "Business trip completed", "Guest satisfied", false, false);
        stay.Status.Should().Be(HotelStayStatus.CheckedOut);
        stay.DepartureReason.Should().Be("Business trip completed");
        stay.DepartureNotes.Should().Be("Guest satisfied");
    }

    [Fact]
    public void DEPARTURE_DOM_U_002_early_departure_detected_by_date()
    {
        var stay = ValidStay(); stay.CheckIn(DateTime.UtcNow, ActorId.New());
        stay.MarkEarlyDeparture(DateTime.UtcNow.AddHours(4), null, "Emergency at home");
        stay.Status.Should().Be(HotelStayStatus.CheckedOut);
        stay.DepartureReason.Should().Be("Emergency at home");
        stay.DomainEvents.Should().ContainSingle(x => x.EventName == "StayEarlyDeparture");
    }

    [Fact]
    public void DEPARTURE_DOM_U_003_not_occupying_cannot_checkout()
    {
        var stay = ValidStay();
        Action act = () => stay.MarkCheckedOut(DateTime.UtcNow, null);
        act.Should().Throw<InvalidOperationException>().WithMessage("STAY_INVALID_STATE*");
    }

    [Fact]
    public void DEPARTURE_DOM_U_004_expected_departure_then_checkout()
    {
        var stay = ValidStay(); stay.CheckIn(DateTime.UtcNow, ActorId.New());
        stay.MarkExpectedDeparture(DateTime.UtcNow.AddDays(1));
        stay.Status.Should().Be(HotelStayStatus.ExpectedDeparture);
        stay.MarkCheckedOut(DateTime.UtcNow.AddDays(1), null, DepartureType.Normal, "Scheduled departure", null, false, false);
        stay.Status.Should().Be(HotelStayStatus.CheckedOut);
    }

    [Fact]
    public void DEPARTURE_DOM_U_005_pending_charges_and_balance_flags()
    {
        var stay = ValidStay(); stay.CheckIn(DateTime.UtcNow, ActorId.New());
        stay.MarkCheckedOut(DateTime.UtcNow.AddDays(1), null, DepartureType.Normal, null, null, hasPendingCharges: true, hasOutstandingBalance: true);
        stay.HasPendingCharges.Should().BeTrue();
        stay.HasOutstandingBalance.Should().BeTrue();
    }

    [Fact]
    public void DEPARTURE_DOM_U_006_departure_approval()
    {
        var stay = ValidStay(); stay.CheckIn(DateTime.UtcNow, ActorId.New());
        stay.ApproveDeparture(DateTime.UtcNow, null);
        stay.IsDepartureApproved.Should().BeTrue();
        stay.DomainEvents.Should().ContainSingle(x => x.EventName == "StayDepartureApproved");
    }

    [Fact]
    public void DEPARTURE_DOM_U_007_late_checkout_request()
    {
        var stay = ValidStay(); stay.CheckIn(DateTime.UtcNow, ActorId.New());
        var lateUntil = new DateTime(2026, 7, 19, 14, 0, 0, DateTimeKind.Utc);
        stay.RequestLateCheckout(lateUntil, DateTime.UtcNow, null);
        stay.LateCheckoutUntilUtc.Should().Be(lateUntil);
        stay.DomainEvents.Should().ContainSingle(x => x.EventName == "StayLateCheckoutRequested");
    }

    [Fact]
    public async Task DEPARTURE_APP_U_001_valid_checkout_records_all_fields()
    {
        var setup = Setup.Valid();
        using var _ = setup.Accessor.BeginScope(setup.Context);
        await setup.Service.CheckInAsync(new CheckInHotelStayCommand(setup.CheckInStay.HotelStayId, new byte[8]), default);
        setup.CheckInStay.ClearDomainEvents(); setup.Persistence.Events.Clear(); setup.Persistence.Audits.Clear();
        var checkoutTime = setup.Clock.UtcNow.AddDays(1);
        var result = await setup.Service.CheckOutAsync(new CheckOutHotelStayCommand(setup.CheckInStay.HotelStayId, new byte[8], checkoutTime, "Trip complete", "All good", false, false), default);
        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be(HotelStayStatus.CheckedOut);
        setup.Stays.Aggregate!.DepartureReason.Should().Be("Trip complete");
        setup.Persistence.Events.Should().ContainSingle(x => x.EventName == "StayCheckedOut");
    }

    [Fact]
    public async Task DEPARTURE_APP_U_002_not_occupying_cannot_checkout()
    {
        var setup = Setup.Valid();
        using var _ = setup.Accessor.BeginScope(setup.Context);
        var result = await setup.Service.CheckOutAsync(new CheckOutHotelStayCommand(setup.CheckInStay.HotelStayId, new byte[8]), default);
        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("STAY_NOT_OCCUPYING");
    }

    private static HotelStay ValidStay() => HotelStay.Prepare("STY-202607-000001", PropertyId.New(), Guid.NewGuid(), "RSV-202607-000001", Guid.NewGuid(), "Guest", null, Guid.NewGuid(), "101", Guid.NewGuid(), "STD", HotelStayPeriod.Create(new(2026, 7, 18), new(2026, 7, 19)), HotelStayGuestCount.Create(1, 0), "Reservation", new DateTime(2026, 7, 18, 0, 0, 0, DateTimeKind.Utc), ActorId.New());

    private sealed class Setup
    {
        private Setup(bool allowed)
        {
            var property = PropertyId.New(); Context = new(new(ActorId.New(), new HashSet<string>(), "test", "test", false), new(property, "Asia/Ho_Chi_Minh", "VND"), new(property, new DateOnly(2026, 7, 18), "test"), new(CorrelationId.New(), null, CommandId.New(), null));
            Accessor = new(); Reservation = ReservationAggregate.Create("RSV-202607-000001", property, Guid.NewGuid(), "Guest", StayPeriod.Create(new(2026, 7, 18), new(2026, 7, 20)), GuestCount.Create(2, 1), Guid.NewGuid(), "Reservation", Clock.UtcNow, Context.Actor.ActorId); Reservation.AssignRoom(Guid.NewGuid(), Clock.UtcNow, Context.Actor.ActorId); Reservation.MarkTentative(Clock.UtcNow, Context.Actor.ActorId); Reservation.Confirm(Clock.UtcNow, Context.Actor.ActorId);
            Reservations = new ReservationRepository(Reservation); CheckInStay = HotelStay.Prepare("STY-202607-000001", property, Reservation.ReservationId, Reservation.ReservationNumber, Reservation.GuestId, Reservation.GuestNameSnapshot, Reservation.GuestPhoneSnapshot, Reservation.AssignedRoomId!.Value, "101", Reservation.RoomTypeId!.Value, "STD", HotelStayPeriod.Create(Reservation.ArrivalDate, Reservation.DepartureDate), HotelStayGuestCount.Create(Reservation.AdultCount, Reservation.ChildCount), Reservation.Source, Clock.UtcNow, Context.Actor.ActorId); CheckInStay.ClearDomainEvents(); Stays = new StayRepository { Aggregate = CheckInStay }; Rooms = new RoomRepository(new HotelRoomDto(Reservation.AssignedRoomId!.Value, "101", null, Reservation.RoomTypeId!.Value, "STD", "Standard", null, null, null, HotelRoomOperationalStatus.Available, true, Clock.UtcNow, [])); Persistence = new Persistence(); NumberGenerator = new NumberGenerator(); Service = new(Stays, NumberGenerator, Reservations, Rooms, Persistence, Accessor, new Authorization(allowed), Clock);
        }
        public static Setup Valid(bool allowed = true) => new(allowed);
        public FrontOfficeExecutionContext Context { get; } public FrontOfficeExecutionContextAccessor Accessor { get; } public FixedClock Clock { get; } = new(); public ReservationAggregate Reservation { get; } public HotelStay CheckInStay { get; } public ReservationRepository Reservations { get; } public StayRepository Stays { get; } public RoomRepository Rooms { get; } public Persistence Persistence { get; } public NumberGenerator NumberGenerator { get; } public HotelStayApplicationService Service { get; }
        public void Apply(string scenario)
        {
            switch (scenario)
            {
                case "missing-reservation": Reservations.Reservation = null; break;
                case "wrong-property": Reservations.PropertyOverride = Guid.NewGuid(); break;
                case "ineligible": Reservations.Reservation = ReservationAggregate.Create("RSV-X", Context.Property.PropertyId, Guid.NewGuid(), "Guest", StayPeriod.Create(new(2026, 7, 18), new(2026, 7, 20)), GuestCount.Create(1, 0), Reservation.RoomTypeId, "Reservation", Clock.UtcNow, Context.Actor.ActorId); break;
                case "no-room": Reservations.Reservation = CreateConfirmedWithoutRoom(); break;
                case "missing-room": Rooms.Room = null; break;
                case "inactive-room": Rooms.Room = Rooms.Room! with { IsActive = false }; break;
                case "out-of-order": Rooms.Room = Rooms.Room! with { OperationalStatus = HotelRoomOperationalStatus.OutOfOrder }; break;
                case "out-of-service": Rooms.Room = Rooms.Room! with { OperationalStatus = HotelRoomOperationalStatus.OutOfService }; break;
                case "type-mismatch": Rooms.Room = Rooms.Room! with { HotelRoomTypeId = Guid.NewGuid() }; break;
                case "duplicate": Stays.Duplicate = true; break;
            }
        }
        private ReservationAggregate CreateConfirmedWithoutRoom()
        { var r = ReservationAggregate.Create("RSV-X", Context.Property.PropertyId, Guid.NewGuid(), "Guest", StayPeriod.Create(new(2026, 7, 18), new(2026, 7, 20)), GuestCount.Create(1, 0), Reservation.RoomTypeId, "Reservation", Clock.UtcNow, Context.Actor.ActorId); r.MarkTentative(Clock.UtcNow, Context.Actor.ActorId); r.Confirm(Clock.UtcNow, Context.Actor.ActorId); return r; }
        public void ApplyCheckIn(string scenario)
        {
            switch (scenario)
            {
                case "missing": Stays.Aggregate = null; break;
                case "not-pending": CheckInStay.CancelPreparation(Clock.UtcNow, null); break;
                case "missing-room": typeof(HotelStay).GetProperty(nameof(HotelStay.RoomId))!.SetValue(CheckInStay, Guid.Empty); break;
                case "inactive-room": Rooms.Room = Rooms.Room! with { IsActive = false }; break;
                case "out-of-order": Rooms.Room = Rooms.Room! with { OperationalStatus = HotelRoomOperationalStatus.OutOfOrder }; break;
                case "out-of-service": Rooms.Room = Rooms.Room! with { OperationalStatus = HotelRoomOperationalStatus.OutOfService }; break;
                case "type-mismatch": Rooms.Room = Rooms.Room! with { HotelRoomTypeId = Guid.NewGuid() }; break;
                case "occupied": Stays.InHouseForRoom = true; break;
            }
        }
    }

    private sealed class FixedClock : IClock { public DateTime UtcNow => new(2026, 7, 18, 10, 0, 0, DateTimeKind.Utc); }
    private sealed class Authorization(bool allowed) : IAuthorizationService { public bool HasPermission(string permissionCode) => allowed; public void Demand(string permissionCode) { } public Task<bool> CanAccessAsync(string permissionCode) => Task.FromResult(allowed); public bool HasPermission(Permission permission) => allowed; public void Demand(Permission permission) { } public Task<bool> ConfirmDangerousActionAsync(string title, string message) => Task.FromResult(allowed); }
    private sealed class NumberGenerator : IHotelStayNumberGenerator { public int Calls { get; private set; } public Task<string> GenerateAsync(Guid propertyId, DateOnly businessDate, CancellationToken cancellationToken = default) { Calls++; return Task.FromResult($"STY-{businessDate:yyyyMM}-000001"); } }
    private sealed class Persistence : IFrontOfficePersistenceSession { public List<IFrontOfficeDomainEvent> Events { get; } = []; public List<FrontOfficeAuditIntent> Audits { get; } = []; public int Commits { get; private set; } public void StageDomainEvent(IFrontOfficeDomainEvent domainEvent) => Events.Add(domainEvent); public void StageAudit(FrontOfficeAuditIntent intent) => Audits.Add(intent); public Task<Result<int>> CommitAsync(CancellationToken cancellationToken = default) { Commits++; return Task.FromResult(Result<int>.Success(1)); } }
    private sealed class ReservationRepository(ReservationAggregate? reservation) : IReservationRepository
    { public ReservationAggregate? Reservation { get; set; } = reservation; public Guid? PropertyOverride { get; set; } public Task<ReservationAggregate?> GetAsync(Guid id, Guid propertyId, CancellationToken cancellationToken = default) => Task.FromResult(PropertyOverride is null ? Reservation : null); public Task AddAsync(ReservationAggregate reservation, ReservationHistory history, CancellationToken cancellationToken = default) => Task.CompletedTask; public Task AppendHistoryAsync(ReservationHistory history, CancellationToken cancellationToken = default) => Task.CompletedTask; public void ApplyExpectedRowVersion(ReservationAggregate reservation, byte[] expectedRowVersion) { } public Task<ReservationDto?> GetDtoAsync(Guid id, Guid propertyId, CancellationToken cancellationToken = default) => Task.FromResult<ReservationDto?>(null); public Task<ReservationDto?> GetByNumberAsync(Guid propertyId, string reservationNumber, CancellationToken cancellationToken = default) => Task.FromResult<ReservationDto?>(null); public Task<PageResult<ReservationDto>> SearchAsync(Guid propertyId, string? keyword, ReservationStatus? status, DateOnly? dateFrom, DateOnly? dateTo, PageRequest page, string? sort, CancellationToken cancellationToken = default) => Task.FromResult(new PageResult<ReservationDto>([], 0, page.Page, page.PageSize)); public Task<PageResult<ReservationDto>> SearchAsync(Guid propertyId, ReservationSearchFilter filter, PageRequest page, string? sort, CancellationToken ct = default) => Task.FromResult(new PageResult<ReservationDto>([], 0, page.Page, page.PageSize)); public Task<PageResult<ReservationHistoryDto>> GetHistoryAsync(Guid reservationId, PageRequest page, CancellationToken ct = default) => Task.FromResult(new PageResult<ReservationHistoryDto>([], 0, page.Page, page.PageSize)); public Task<RoomConflictResult> CheckAsync(Guid propertyId, Guid roomId, DateOnly arrival, DateOnly departure, Guid? excludedReservationId, CancellationToken cancellationToken = default) => Task.FromResult(new RoomConflictResult(false, null)); }
    private sealed class StayRepository : IHotelStayRepository
    { public int AddCalls { get; private set; } public bool Duplicate { get; set; } public bool InHouseForRoom { get; set; } public HotelStay? Aggregate { get; set; } public List<HotelStayHistory> Histories { get; } = []; public IReadOnlyList<HotelStayDto> Arrivals { get; set; } = []; public DateOnly? LastBusinessDate { get; private set; } public Task AddAsync(HotelStay stay, HotelStayHistory history, CancellationToken cancellationToken = default) { AddCalls++; Aggregate = stay; Histories.Add(history); return Task.CompletedTask; } public Task<HotelStay?> GetAsync(Guid propertyId, Guid stayId, CancellationToken cancellationToken = default) => Task.FromResult(Aggregate?.PropertyId == propertyId && Aggregate.HotelStayId == stayId ? Aggregate : null); public Task<HotelStayDto?> GetDtoAsync(Guid propertyId, Guid stayId, CancellationToken cancellationToken = default) => Task.FromResult(Aggregate?.PropertyId == propertyId && Aggregate.HotelStayId == stayId ? ToDto(Aggregate) : null); public Task<HotelStayDto?> GetByNumberAsync(Guid propertyId, string stayNumber, CancellationToken cancellationToken = default) => Task.FromResult<HotelStayDto?>(null); public Task<PageResult<HotelStayDto>> SearchAsync(Guid propertyId, string? keyword, HotelStayStatus? status, DateOnly? dateFrom, DateOnly? dateTo, PageRequest page, string? sort, CancellationToken cancellationToken = default) => Task.FromResult(new PageResult<HotelStayDto>([], 0, page.Page, page.PageSize)); public Task<IReadOnlyList<HotelStayDto>> GetCurrentInHouseAsync(Guid propertyId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<HotelStayDto>>([]); public Task<IReadOnlyList<HotelStayDto>> GetExpectedArrivalsAsync(Guid propertyId, DateOnly businessDate, CancellationToken cancellationToken = default) { LastBusinessDate = businessDate; return Task.FromResult(Arrivals); } public Task<IReadOnlyList<HotelStayDto>> GetExpectedDeparturesAsync(Guid propertyId, DateOnly businessDate, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<HotelStayDto>>([]); public Task<bool> ExistsActiveForReservationAsync(Guid propertyId, Guid reservationId, CancellationToken cancellationToken = default) => Task.FromResult(Duplicate); public Task<bool> ExistsInHouseForRoomAsync(Guid propertyId, Guid roomId, Guid? excludingStayId = null, CancellationToken cancellationToken = default) => Task.FromResult(InHouseForRoom); public Task<IReadOnlyList<StayTimelineEntryDto>> GetTimelineAsync(Guid propertyId, Guid stayId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<StayTimelineEntryDto>>([]); public Task AppendHistoryAsync(HotelStayHistory history, CancellationToken cancellationToken = default) { Histories.Add(history); return Task.CompletedTask; } public void ApplyExpectedRowVersion(HotelStay stay, byte[] expectedRowVersion) { } private static HotelStayDto ToDto(HotelStay stay) => new(stay.HotelStayId, stay.StayNumber, stay.PropertyId, stay.ReservationId, stay.ReservationNumberSnapshot, stay.GuestId, stay.GuestNameSnapshot, stay.RoomId, stay.RoomNumberSnapshot, stay.RoomTypeId, stay.RoomTypeCodeSnapshot, stay.ScheduledArrivalDate, stay.ScheduledDepartureDate, stay.ActualCheckInAtUtc, stay.ActualCheckOutAtUtc, stay.Adults, stay.Children, stay.Status, stay.Source, stay.ExtensionCount, stay.CreatedAtUtc, stay.UpdatedAtUtc, []); }
    private sealed class RoomRepository(HotelRoomDto? room) : IHotelRoomMasterDataRepository
    { public HotelRoomDto? Room { get; set; } = room; public HotelRoomTypeDto? RoomType { get; set; } public Task AddRoomTypeAsync(HotelRoomType roomType, CancellationToken ct = default) => Task.CompletedTask; public Task AddRoomAsync(HotelRoom room, CancellationToken ct = default) => Task.CompletedTask; public Task<HotelRoomType?> GetRoomTypeAsync(Guid propertyId, Guid id, CancellationToken ct = default) => Task.FromResult<HotelRoomType?>(null); public Task<HotelRoom?> GetRoomAsync(Guid propertyId, Guid id, CancellationToken ct = default) => Task.FromResult<HotelRoom?>(null); public Task<bool> RoomTypeCodeExistsAsync(Guid propertyId, string code, Guid? excludingId, CancellationToken ct = default) => Task.FromResult(false); public Task<bool> RoomNumberExistsAsync(Guid propertyId, string roomNumber, Guid? excludingId, CancellationToken ct = default) => Task.FromResult(false); public void UseExpectedRowVersion(object aggregate, byte[] expectedRowVersion) { } public Task<HotelRoomTypeDto?> GetRoomTypeDtoAsync(Guid propertyId, Guid id, CancellationToken ct = default) => Task.FromResult(RoomType?.HotelRoomTypeId == id ? RoomType : null); public Task<HotelRoomTypeDto?> GetRoomTypeByCodeAsync(Guid propertyId, string code, CancellationToken ct = default) => Task.FromResult<HotelRoomTypeDto?>(null); public Task<PageResult<HotelRoomTypeDto>> SearchRoomTypesAsync(Guid propertyId, string? keyword, bool? isActive, PageRequest page, string? sort, CancellationToken ct = default) => Task.FromResult(new PageResult<HotelRoomTypeDto>([], 0, page.Page, page.PageSize)); public Task<HotelRoomDto?> GetRoomDtoAsync(Guid propertyId, Guid id, CancellationToken ct = default) => Task.FromResult(Room?.HotelRoomId == id ? Room : null); public Task<HotelRoomDto?> GetRoomByNumberAsync(Guid propertyId, string number, CancellationToken ct = default) => Task.FromResult<HotelRoomDto?>(null); public Task<PageResult<HotelRoomDto>> SearchRoomsAsync(Guid propertyId, string? keyword, Guid? roomTypeId, string? floor, string? building, HotelRoomOperationalStatus? status, bool? isActive, PageRequest page, string? sort, bool assignableOnly, CancellationToken ct = default) => Task.FromResult(new PageResult<HotelRoomDto>([], 0, page.Page, page.PageSize)); public Task<IReadOnlyList<HotelRoomTypeDto>> GetActiveRoomTypesAsync(Guid propertyId, CancellationToken ct = default) => Task.FromResult<IReadOnlyList<HotelRoomTypeDto>>([]); public Task<IReadOnlyList<HotelRoomDto>> GetAssignableRoomsAsync(Guid propertyId, Guid? roomTypeId, CancellationToken ct = default) => Task.FromResult<IReadOnlyList<HotelRoomDto>>([]); }
}
