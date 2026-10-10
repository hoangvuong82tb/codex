using ASMILE.BO.Application.FrontOffice.Foundation.Audit;
using ASMILE.BO.Application.FrontOffice.Foundation.Contexts;
using ASMILE.BO.Application.FrontOffice.Foundation.Persistence;
using ASMILE.BO.Application.FrontOffice.Folio;
using ASMILE.BO.Application.FrontOffice.HotelMasterData;
using ASMILE.BO.Application.FrontOffice.Rate;
using ASMILE.BO.Application.FrontOffice.Reservation;
using ASMILE.BO.Application.Interfaces;
using ASMILE.BO.Core.FrontOffice.Folio;
using ASMILE.BO.Core.FrontOffice.HotelMasterData;
using ASMILE.BO.Core.FrontOffice.Stay;
using ASMILE.BO.Shared.Foundation;

namespace ASMILE.BO.Application.FrontOffice.Stay;

/// <summary>
/// Single-transaction workflow for the combined room move/extension screen.
/// The existing Stay and Folio application services intentionally commit one
/// aggregate at a time; this service is the boundary used when both aggregates
/// must succeed or fail together.
/// </summary>
public sealed class StayAdjustmentApplicationService(
    IHotelStayRepository stayRepository,
    IHotelRoomMasterDataRepository rooms,
    IRoomReservationConflictChecker reservationConflicts,
    IHotelFolioRepository folioRepository,
    IHotelFolioNumberGenerator folioNumberGenerator,
    IFrontOfficePersistenceSession persistence,
    IFrontOfficeExecutionContextAccessor contextAccessor,
    IAuthorizationService authorization,
    IClock clock)
{
    public async Task<Result<StayAdjustmentResult>> AdjustAsync(AdjustStayCommand command, CancellationToken ct)
    {
        if (!authorization.HasPermission(HotelStayPermissions.Update))
            return Fail<StayAdjustmentResult>(Error.Authorization("STAY_PERMISSION_DENIED", "Permission denied."));
        if (command.FolioAmount != 0 && !authorization.HasPermission("FrontOffice.Folio.AddCharge"))
            return Fail<StayAdjustmentResult>(Error.Authorization("FOLIO_PERMISSION_DENIED", "Folio add charge permission is required."));

        var ctx = contextAccessor.Current;
        if (ctx is null)
            return Fail<StayAdjustmentResult>(Error.Authorization("STAY_PERMISSION_DENIED", "Front Office context is required."));
        if (command.ExpectedStayRowVersion is not { Length: 8 })
            return Fail<StayAdjustmentResult>(Error.Validation("STAY_ROWVERSION_REQUIRED", "Row version required."));
        if (command.TargetRoomId == Guid.Empty)
            return Fail<StayAdjustmentResult>(Error.Validation("STAY_ROOM_REQUIRED", "Target room is required."));
        if (string.IsNullOrWhiteSpace(command.Reason))
            return Fail<StayAdjustmentResult>(Error.Validation("STAY_ADJUSTMENT_REASON_REQUIRED", "Adjustment reason is required."));

        var propertyId = ctx.Property.PropertyId.Value;
        var stay = await stayRepository.GetAsync(propertyId, command.HotelStayId, ct);
        if (stay is null)
            return Fail<StayAdjustmentResult>(Error.Business("STAY_NOT_FOUND", "Stay not found."));
        if (!stay.Status.IsOccupying())
            return Fail<StayAdjustmentResult>(Error.Business("STAY_NOT_OCCUPYING", "Stay is not occupying a room."));
        if (command.NewDepartureDate <= stay.ScheduledDepartureDate)
            return Fail<StayAdjustmentResult>(Error.Validation("STAY_INVALID_DEPARTURE", "New departure must be after the current departure."));

        var isRoomMove = command.TargetRoomId != stay.RoomId;
        var targetRoom = await rooms.GetRoomDtoAsync(propertyId, command.TargetRoomId, ct);
        if (targetRoom is null || !targetRoom.IsActive || (isRoomMove && targetRoom.OperationalStatus != HotelRoomOperationalStatus.Available))
            return Fail<StayAdjustmentResult>(Error.Business("STAY_ROOM_NOT_ASSIGNABLE", "Target room is not available."));
        if (isRoomMove && await stayRepository.ExistsInHouseForRoomAsync(propertyId, command.TargetRoomId, null, ct))
            return Fail<StayAdjustmentResult>(Error.Business("STAY_ROOM_ALREADY_OCCUPIED", "Target room is already occupied."));

        var conflictStart = !isRoomMove
            ? stay.ScheduledDepartureDate
            : ctx.BusinessDate.BusinessDate;
        var roomConflict = await reservationConflicts.CheckAsync(
            propertyId,
            command.TargetRoomId,
            conflictStart,
            command.NewDepartureDate,
            stay.ReservationId,
            ct);
        if (roomConflict.HasConflict)
            return Fail<StayAdjustmentResult>(Error.Business(
                "STAY_ROOM_RESERVATION_CONFLICT",
                "The target room has an overlapping reservation during the adjusted stay."));

        HotelFolioDto? folioDto = null;
        HotelFolio? folio = null;
        if (command.FolioAmount != 0)
        {
            folioDto = await folioRepository.GetByStayAsync(propertyId, stay.HotelStayId, ct);
            if (folioDto is null)
            {
                var number = await folioNumberGenerator.GenerateAsync(propertyId, ctx.BusinessDate.BusinessDate, ct);
                folio = HotelFolio.Create(propertyId, stay.HotelStayId, number, clock.UtcNow, ctx.Actor.ActorId);
                await folioRepository.AddAsync(folio, ct);
            }
            else
            {
                folio = await folioRepository.GetAsync(propertyId, folioDto.HotelFolioId, ct);
                if (folio is null)
                    return Fail<StayAdjustmentResult>(Error.Business("FOLIO_NOT_FOUND", "Stay Folio not found."));
                if (folioDto.RowVersion is not { Length: 8 })
                    return Fail<StayAdjustmentResult>(Error.Validation("FOLIO_ROWVERSION_REQUIRED", "Folio row version required."));
                folioRepository.ApplyExpectedRowVersion(folio, folioDto.RowVersion);
            }
        }

        stayRepository.ApplyExpectedRowVersion(stay, command.ExpectedStayRowVersion);
        var now = clock.UtcNow;
        try
        {
            var roomType = await rooms.GetRoomTypeDtoAsync(propertyId, targetRoom.HotelRoomTypeId, ct);
            if (command.TargetRoomId != stay.RoomId)
            {
                stay.MoveRoom(
                    command.TargetRoomId,
                    targetRoom.RoomNumber,
                    targetRoom.HotelRoomTypeId,
                    command.Reason,
                    now,
                    ctx.Actor.ActorId,
                    roomType?.DisplayOrder ?? 0,
                    0);
                await stayRepository.AppendHistoryAsync(
                    HotelStayHistory.Record(stay, "StayRoomMoved", now, ctx.Actor.ActorId?.Value,
                        ctx.Correlation.CorrelationId.Value,
                        $"ToRoom={targetRoom.RoomNumber};Reason={command.Reason}"), ct);
            }

            stay.ExtendStay(command.NewDepartureDate, now, ctx.Actor.ActorId);
            await stayRepository.AppendHistoryAsync(
                HotelStayHistory.Record(stay, "StayExtended", now, ctx.Actor.ActorId?.Value,
                    ctx.Correlation.CorrelationId.Value,
                    $"NewDeparture={command.NewDepartureDate}"), ct);

            HotelFolioEntry? entry = null;
            if (folio is not null && command.FolioAmount > 0)
            {
                entry = folio.AddRoomCharge(
                    command.FolioAmount,
                    $"Điều chỉnh lưu trú {stay.StayNumber} · phòng {targetRoom.RoomNumber}",
                    ctx.BusinessDate.BusinessDate,
                    command.RatePlanId,
                    now,
                    ctx.Actor.ActorId);
            }
            else if (folio is not null && command.FolioAmount < 0)
            {
                entry = folio.AddDiscount(
                    Math.Abs(command.FolioAmount),
                    $"Miễn/giảm chênh lệch điều chỉnh lưu trú {stay.StayNumber}",
                    now,
                    ctx.Actor.ActorId,
                    ctx.BusinessDate.BusinessDate);
            }

            if (entry is not null)
                await folioRepository.AddEntryAsync(entry, ct);

            foreach (var domainEvent in stay.DomainEvents)
                persistence.StageDomainEvent(domainEvent);
            persistence.StageAudit(new FrontOfficeAuditIntent(
                "StayAdjustedWithFolio",
                nameof(HotelStay),
                stay.HotelStayId.ToString("D"),
                "Succeeded",
                Metadata: new Dictionary<string, string>
                {
                    ["PropertyId"] = propertyId.ToString("D"),
                    ["StayNumber"] = stay.StayNumber,
                    ["TargetRoom"] = targetRoom.RoomNumber,
                    ["FolioAmount"] = command.FolioAmount.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    ["ActorId"] = ctx.Actor.ActorId?.Value.ToString("D") ?? string.Empty,
                    ["CorrelationId"] = ctx.Correlation.CorrelationId.Value.ToString("D")
                }));

            var commit = await persistence.CommitAsync(ct);
            if (commit.IsFailure)
                return Fail<StayAdjustmentResult>(commit.Error!.Category == ErrorCategory.Concurrency
                    ? Error.Concurrency("STAY_ADJUSTMENT_CONCURRENCY", "Stay or Folio was changed by another user.")
                    : commit.Error);

            stay.ClearDomainEvents();
            var updatedStay = await stayRepository.GetDtoAsync(propertyId, stay.HotelStayId, ct);
            var updatedFolio = folio is null ? null : await folioRepository.GetDtoAsync(propertyId, folio.HotelFolioId, ct);
            return updatedStay is null || (folio is not null && updatedFolio is null)
                ? Fail<StayAdjustmentResult>(Error.System("STAY_ADJUSTMENT_READ_FAILED", "Could not read the adjustment after save."))
                : Result<StayAdjustmentResult>.Success(new StayAdjustmentResult(updatedStay, updatedFolio, entry is null ? null : MapEntry(entry)));
        }
        catch (InvalidOperationException ex)
        {
            return Fail<StayAdjustmentResult>(Error.Business("STAY_ADJUSTMENT_INVALID_STATE", ex.Message));
        }
        catch (ArgumentException ex)
        {
            return Fail<StayAdjustmentResult>(Error.Validation("STAY_ADJUSTMENT_INVALID_INPUT", ex.Message));
        }
    }

    private static Result<T> Fail<T>(Error? error) => Result<T>.Failure(error!);

    private static HotelFolioEntryDto MapEntry(HotelFolioEntry entry) => new(
        entry.HotelFolioEntryId,
        entry.SequenceNumber,
        entry.EntryType,
        entry.Amount,
        entry.Description,
        entry.ReferenceType,
        entry.ReferenceId,
        entry.BusinessDate,
        entry.ReversesEntryId,
        entry.ReversedByEntryId,
        entry.CreatedAtUtc,
        entry.CreatedBy);
}
