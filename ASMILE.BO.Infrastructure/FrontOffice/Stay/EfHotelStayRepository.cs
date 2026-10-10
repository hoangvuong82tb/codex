using System.Linq.Expressions;
using ASMILE.BO.Application.FrontOffice.Foundation.Cqrs;
using ASMILE.BO.Application.FrontOffice.Stay;
using ASMILE.BO.Core.FrontOffice.Stay;
using ASMILE.BO.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using HotelStayAggregate = ASMILE.BO.Core.FrontOffice.Stay.HotelStay;

namespace ASMILE.BO.Infrastructure.FrontOffice.Stay;

public sealed class EfHotelStayRepository(AppDbContext dbContext) : IHotelStayRepository, IHotelStayOccupancyReader
{
    public Task AddAsync(HotelStayAggregate stay, HotelStayHistory history, CancellationToken cancellationToken = default)
    {
        dbContext.HotelStays.Add(stay);
        dbContext.HotelStayHistories.Add(history);
        return Task.CompletedTask;
    }

    public Task<HotelStayAggregate?> GetAsync(Guid propertyId, Guid stayId, CancellationToken cancellationToken = default) => dbContext.HotelStays.SingleOrDefaultAsync(x => x.PropertyId == propertyId && x.HotelStayId == stayId, cancellationToken);
    public Task<HotelStayAggregate?> GetActiveForReservationAsync(Guid propertyId, Guid reservationId, CancellationToken cancellationToken = default)
        => dbContext.HotelStays
            .Where(x => x.PropertyId == propertyId && x.ReservationId == reservationId
                && (x.Status == HotelStayStatus.PendingArrival
                    || x.Status == HotelStayStatus.PreArrival
                    || x.Status == HotelStayStatus.CheckedIn
                    || x.Status == HotelStayStatus.InHouse
                    || x.Status == HotelStayStatus.RoomMoved
                    || x.Status == HotelStayStatus.Extended
                    || x.Status == HotelStayStatus.ExpectedDeparture))
            .OrderByDescending(x => x.UpdatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);
    public Task<HotelStayDto?> GetDtoAsync(Guid propertyId, Guid stayId, CancellationToken cancellationToken = default) => dbContext.HotelStays.AsNoTracking().Where(x => x.PropertyId == propertyId && x.HotelStayId == stayId).Select(ToDto).SingleOrDefaultAsync(cancellationToken);
    public Task<HotelStayDto?> GetByNumberAsync(Guid propertyId, string stayNumber, CancellationToken cancellationToken = default) => dbContext.HotelStays.AsNoTracking().Where(x => x.PropertyId == propertyId && x.StayNumber == stayNumber).Select(ToDto).SingleOrDefaultAsync(cancellationToken);

    public async Task<PageResult<HotelStayDto>> SearchAsync(Guid propertyId, string? keyword, HotelStayStatus? status, DateOnly? dateFrom, DateOnly? dateTo, PageRequest page, string? sort, CancellationToken cancellationToken = default)
    {
        var query = dbContext.HotelStays.AsNoTracking().Where(x => x.PropertyId == propertyId);
        if (!string.IsNullOrWhiteSpace(keyword)) query = query.Where(x => x.StayNumber.Contains(keyword) || x.GuestNameSnapshot.Contains(keyword) || x.RoomNumberSnapshot.Contains(keyword));
        if (status is not null) query = query.Where(x => x.Status == status);
        if (dateFrom is not null) query = query.Where(x => x.ScheduledDepartureDate > dateFrom);
        if (dateTo is not null) query = query.Where(x => x.ScheduledArrivalDate < dateTo);
        query = sort?.ToLowerInvariant() switch
        {
            "number" => query.OrderBy(x => x.StayNumber),
            "arrival" => query.OrderBy(x => x.ScheduledArrivalDate).ThenBy(x => x.StayNumber),
            "departure" => query.OrderBy(x => x.ScheduledDepartureDate).ThenBy(x => x.StayNumber),
            _ => query.OrderByDescending(x => x.UpdatedAtUtc)
        };
        var total = await query.CountAsync(cancellationToken);
        var size = Math.Clamp(page.PageSize, 1, 200);
        var items = await query.Skip(page.Skip).Take(size).Select(ToDto).ToListAsync(cancellationToken);
        return new PageResult<HotelStayDto>(items, total, page.Page, page.PageSize);
    }

    public async Task<IReadOnlyList<HotelStayDto>> GetCurrentInHouseAsync(Guid propertyId, CancellationToken cancellationToken = default) => await dbContext.HotelStays.AsNoTracking().Where(x => x.PropertyId == propertyId && (x.Status == HotelStayStatus.CheckedIn || x.Status == HotelStayStatus.InHouse || x.Status == HotelStayStatus.RoomMoved || x.Status == HotelStayStatus.Extended || x.Status == HotelStayStatus.ExpectedDeparture)).OrderBy(x => x.RoomNumberSnapshot).Select(ToDto).ToListAsync(cancellationToken);
    public async Task<IReadOnlyList<HotelStayDto>> GetExpectedArrivalsAsync(Guid propertyId, DateOnly businessDate, CancellationToken cancellationToken = default) => await dbContext.HotelStays.AsNoTracking().Where(x => x.PropertyId == propertyId && x.Status == HotelStayStatus.PendingArrival && x.ScheduledArrivalDate == businessDate).OrderBy(x => x.RoomNumberSnapshot).Select(ToDto).ToListAsync(cancellationToken);
    public async Task<IReadOnlyList<HotelStayDto>> GetExpectedDeparturesAsync(Guid propertyId, DateOnly businessDate, CancellationToken cancellationToken = default) => await dbContext.HotelStays.AsNoTracking().Where(x => x.PropertyId == propertyId && x.Status == HotelStayStatus.InHouse && x.ScheduledDepartureDate == businessDate).OrderBy(x => x.RoomNumberSnapshot).Select(ToDto).ToListAsync(cancellationToken);
    public Task<bool> ExistsActiveForReservationAsync(Guid propertyId, Guid reservationId, CancellationToken cancellationToken = default)
        => dbContext.HotelStays.AsNoTracking().AnyAsync(x =>
            x.PropertyId == propertyId
            && x.ReservationId == reservationId
            && (x.Status == HotelStayStatus.PendingArrival
                || x.Status == HotelStayStatus.PreArrival
                || x.Status == HotelStayStatus.CheckedIn
                || x.Status == HotelStayStatus.InHouse
                || x.Status == HotelStayStatus.RoomMoved
                || x.Status == HotelStayStatus.Extended
                || x.Status == HotelStayStatus.ExpectedDeparture), cancellationToken);

    public Task<bool> ExistsInHouseForRoomAsync(Guid propertyId, Guid roomId, Guid? excludingStayId = null, CancellationToken cancellationToken = default)
        => dbContext.HotelStays.AsNoTracking().AnyAsync(x =>
            x.PropertyId == propertyId
            && x.RoomId == roomId
            && (x.Status == HotelStayStatus.CheckedIn
                || x.Status == HotelStayStatus.InHouse
                || x.Status == HotelStayStatus.RoomMoved
                || x.Status == HotelStayStatus.Extended
                || x.Status == HotelStayStatus.ExpectedDeparture)
            && (excludingStayId == null || x.HotelStayId != excludingStayId), cancellationToken);
    public Task AppendHistoryAsync(HotelStayHistory history, CancellationToken cancellationToken = default) { dbContext.HotelStayHistories.Add(history); return Task.CompletedTask; }
    public void ApplyExpectedRowVersion(HotelStayAggregate stay, byte[] expectedRowVersion) => dbContext.Entry(stay).Property(x => x.RowVersion).OriginalValue = expectedRowVersion;

    public async Task<IReadOnlyList<StayTimelineEntryDto>> GetTimelineAsync(Guid propertyId, Guid stayId, CancellationToken cancellationToken = default)
        => await dbContext.HotelStayHistories.AsNoTracking()
            .Where(h => h.PropertyId == propertyId && h.HotelStayId == stayId)
            .OrderBy(h => h.OccurredAtUtc)
            .Select(h => new StayTimelineEntryDto(h.OccurredAtUtc, h.Action, h.FromStatus, h.ToStatus, h.ActorId, h.SafeMetadata))
            .ToListAsync(cancellationToken);
    public async Task<IReadOnlyList<RoomOccupancyDto>> GetRoomOccupancyAsync(Guid propertyId, CancellationToken cancellationToken = default) => await (from room in dbContext.HotelRooms.AsNoTracking()
        join type in dbContext.HotelRoomTypes.AsNoTracking() on room.HotelRoomTypeId equals type.HotelRoomTypeId
        join stay in dbContext.HotelStays.AsNoTracking().Where(x => x.PropertyId == propertyId && x.Status == HotelStayStatus.InHouse) on room.HotelRoomId equals stay.RoomId into stays
        from stay in stays.DefaultIfEmpty()
        where room.PropertyId == propertyId
        orderby room.RoomNumber
        select new RoomOccupancyDto(room.HotelRoomId, room.RoomNumber, type.Code, room.OperationalStatus.ToString(), stay == null ? "Vacant" : "Occupied", stay == null ? null : stay.StayNumber)).ToListAsync(cancellationToken);

    private static readonly Expression<Func<HotelStayAggregate, HotelStayDto>> ToDto = x => new HotelStayDto(x.HotelStayId, x.StayNumber, x.PropertyId, x.ReservationId, x.ReservationNumberSnapshot, x.GuestId, x.GuestNameSnapshot, x.RoomId, x.RoomNumberSnapshot, x.RoomTypeId, x.RoomTypeCodeSnapshot, x.ScheduledArrivalDate, x.ScheduledDepartureDate, x.ActualCheckInAtUtc, x.ActualCheckOutAtUtc, x.Adults, x.Children, x.Status, x.Source, x.ExtensionCount, x.CreatedAtUtc, x.UpdatedAtUtc, x.RowVersion, x.IsDepartureApproved, x.GuestPhoneSnapshot, x.SpecialRequests, x.InternalNotes);
}
