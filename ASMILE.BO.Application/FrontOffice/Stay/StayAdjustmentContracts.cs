using ASMILE.BO.Application.FrontOffice.Foundation.Cqrs;
using ASMILE.BO.Application.FrontOffice.Folio;
using ASMILE.BO.Shared.Foundation;

namespace ASMILE.BO.Application.FrontOffice.Stay;

/// <summary>
/// Atomically changes an in-house stay and posts the resulting room adjustment
/// to the stay Folio.  A positive amount is a room charge; a negative amount is
/// recorded as a discount.  Zero is valid for a waived/no-difference policy.
/// </summary>
public sealed record AdjustStayCommand(
    Guid HotelStayId,
    byte[] ExpectedStayRowVersion,
    Guid TargetRoomId,
    DateOnly NewDepartureDate,
    decimal FolioAmount,
    string Reason,
    Guid? RatePlanId = null) : ICommand<StayAdjustmentResult>;

public sealed record StayAdjustmentResult(
    HotelStayDto Stay,
    HotelFolioDto? Folio,
    HotelFolioEntryDto? FolioEntry);
