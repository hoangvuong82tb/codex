using ASMILE.BO.Application.FrontOffice.Foundation.Cqrs;
using ASMILE.BO.Application.FrontOffice.Reservation;
using ASMILE.BO.Core.FrontOffice.Reservation;
using ASMILE.BO.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using ReservationAggregate = ASMILE.BO.Core.FrontOffice.Reservation.Reservation;

namespace ASMILE.BO.Infrastructure.FrontOffice.Reservation;

public sealed class EfReservationRepository(AppDbContext dbContext) : IReservationRepository, IOverbookingChecker
{
    public Task AddAsync(ReservationAggregate reservation, ReservationHistory history, CancellationToken cancellationToken = default) { dbContext.FrontOfficeReservations.Add(reservation); dbContext.FrontOfficeReservationHistory.Add(history); return Task.CompletedTask; }
    public Task<ReservationAggregate?> GetAsync(Guid id, Guid propertyId, CancellationToken cancellationToken = default) => dbContext.FrontOfficeReservations.SingleOrDefaultAsync(x => x.ReservationId == id && x.PropertyId == propertyId, cancellationToken);
    public Task AppendHistoryAsync(ReservationHistory history, CancellationToken cancellationToken = default) { dbContext.FrontOfficeReservationHistory.Add(history); return Task.CompletedTask; }
    public void ApplyExpectedRowVersion(ReservationAggregate reservation, byte[] expectedRowVersion) => dbContext.Entry(reservation).Property(x => x.RowVersion).OriginalValue = expectedRowVersion;
    public Task<ReservationDto?> GetDtoAsync(Guid id, Guid propertyId, CancellationToken cancellationToken = default) => dbContext.FrontOfficeReservations.AsNoTracking().Where(x => x.ReservationId == id && x.PropertyId == propertyId).Select(ToDto).SingleOrDefaultAsync(cancellationToken);
    public Task<ReservationDto?> GetByNumberAsync(Guid propertyId, string reservationNumber, CancellationToken cancellationToken = default) => dbContext.FrontOfficeReservations.AsNoTracking().Where(x => x.PropertyId == propertyId && x.ReservationNumber == reservationNumber).Select(ToDto).SingleOrDefaultAsync(cancellationToken);
    public async Task<PageResult<ReservationDto>> SearchAsync(Guid propertyId, ReservationSearchFilter f, PageRequest page, string? sort, CancellationToken cancellationToken = default)
    {
        var query = dbContext.FrontOfficeReservations.AsNoTracking().Where(x => x.PropertyId == propertyId);
        if (!string.IsNullOrWhiteSpace(f.Keyword)) query = query.Where(x => x.ReservationNumber.Contains(f.Keyword) || x.GuestNameSnapshot.Contains(f.Keyword));
        if (!string.IsNullOrWhiteSpace(f.Phone)) query = query.Where(x => x.GuestPhoneSnapshot != null && x.GuestPhoneSnapshot.Contains(f.Phone));
        if (f.Status is not null) query = query.Where(x => x.Status == f.Status);
        if (f.DateFrom is not null) query = query.Where(x => x.DepartureDate > f.DateFrom);
        if (f.DateTo is not null) query = query.Where(x => x.ArrivalDate < f.DateTo);
        if (f.RoomTypeId is not null) query = query.Where(x => x.RoomTypeId == f.RoomTypeId);
        if (f.RoomId is not null) query = query.Where(x => x.AssignedRoomId == f.RoomId);
        if (!string.IsNullOrWhiteSpace(f.Source)) query = query.Where(x => x.Source == f.Source);
        if (!string.IsNullOrWhiteSpace(f.MarketSegment)) query = query.Where(x => x.MarketSegment == f.MarketSegment);
        if (f.IsVip is not null) query = query.Where(x => x.IsVip == f.IsVip);
        if (!string.IsNullOrWhiteSpace(f.CompanyName)) query = query.Where(x => x.CompanyName != null && x.CompanyName.Contains(f.CompanyName));
        if (!string.IsNullOrWhiteSpace(f.TravelAgentName)) query = query.Where(x => x.TravelAgentName != null && x.TravelAgentName.Contains(f.TravelAgentName));
        query = sort?.ToLowerInvariant() switch { "number" => query.OrderBy(x => x.ReservationNumber), "arrival" => query.OrderBy(x => x.ArrivalDate), "departure" => query.OrderBy(x => x.DepartureDate), "guest" => query.OrderBy(x => x.GuestNameSnapshot), "status" => query.OrderBy(x => x.Status), _ => query.OrderByDescending(x => x.UpdatedAtUtc) };
        var total = await query.CountAsync(cancellationToken); var items = await query.Skip(page.Skip).Take(Math.Clamp(page.PageSize, 1, 200)).Select(ToDto).ToListAsync(cancellationToken);
        return new(items, total, page.Page, page.PageSize);
    }
    public async Task<PageResult<ReservationHistoryDto>> GetHistoryAsync(Guid reservationId, PageRequest page, CancellationToken cancellationToken = default)
    {
        var query = dbContext.FrontOfficeReservationHistory.AsNoTracking().Where(x => x.ReservationId == reservationId).OrderByDescending(x => x.OccurredAtUtc);
        var total = await query.CountAsync(cancellationToken);
        var items = await query.Skip(page.Skip).Take(Math.Clamp(page.PageSize, 1, 200)).Select(x => new ReservationHistoryDto(x.HistoryId, x.ReservationId, x.Action, x.FromStatus, x.ToStatus, x.OccurredAtUtc, x.ActorId, x.ReasonCode, x.SafeSummary)).ToListAsync(cancellationToken);
        return new(items, total, page.Page, page.PageSize);
    }
    public async Task<RoomConflictResult> CheckAsync(Guid propertyId, Guid roomId, DateOnly arrival, DateOnly departure, Guid? excludedReservationId, CancellationToken cancellationToken = default)
    {
        var conflict = await dbContext.FrontOfficeReservations.AsNoTracking().Where(x => x.PropertyId == propertyId && x.AssignedRoomId == roomId && (x.Status == ReservationStatus.Tentative || x.Status == ReservationStatus.Confirmed || x.Status == ReservationStatus.Guaranteed || x.Status == ReservationStatus.CheckedIn) && (excludedReservationId == null || x.ReservationId != excludedReservationId) && x.ArrivalDate < departure && x.DepartureDate > arrival).Select(x => (Guid?)x.ReservationId).FirstOrDefaultAsync(cancellationToken);
        return new(conflict is not null, conflict);
    }
    private static readonly System.Linq.Expressions.Expression<Func<ReservationAggregate, ReservationDto>> ToDto = x => new ReservationDto(x.ReservationId, x.ReservationNumber, x.PropertyId, x.GuestNameSnapshot, x.GuestPhoneSnapshot, x.GuestEmailSnapshot, x.ArrivalDate, x.DepartureDate, x.AdultCount, x.ChildCount, x.RoomTypeId, x.AssignedRoomId, x.RatePlanId, x.Status, x.Source, x.SpecialRequests, x.InternalNotes, x.CancellationReason, x.IsVip, x.MarketSegment, x.CompanyName, x.TravelAgentName, x.GuaranteedAtUtc, x.GuaranteeType, x.GuaranteeExpiryDate, x.DepositAmount, x.DepositDueDate, x.DepositStatus, x.CheckedInAtUtc, x.CompletedAtUtc, x.CreatedAtUtc, x.UpdatedAtUtc, x.RowVersion) { BookingSourceProfileId = x.BookingSourceProfileId, BookingChannelProfileId = x.BookingChannelProfileId, MarketSegmentProfileId = x.MarketSegmentProfileId, CompanyProfileId = x.CompanyProfileId, TravelAgentProfileId = x.TravelAgentProfileId, CommissionRuleProfileId = x.CommissionRuleProfileId };
    public async Task<bool> IsOverbookingLimitReachedAsync(Guid propertyId, Guid roomTypeId, DateOnly arrival, DateOnly departure, Guid? excludedReservationId, CancellationToken ct)
    {
        var count = await dbContext.FrontOfficeReservations.CountAsync(x => x.PropertyId == propertyId && x.RoomTypeId == roomTypeId && x.Status != ReservationStatus.Cancelled && x.Status != ReservationStatus.NoShow && x.Status != ReservationStatus.Completed && x.ArrivalDate < departure && x.DepartureDate > arrival && (excludedReservationId == null || x.ReservationId != excludedReservationId), ct);
        var roomCount = await dbContext.HotelRooms.CountAsync(x => x.PropertyId == propertyId && x.HotelRoomTypeId == roomTypeId && x.IsActive, ct);
        return roomCount > 0 && count >= roomCount;
    }
}
