using ASMILE.BO.Application.FrontOffice.Foundation.Cqrs;
using ASMILE.BO.Application.FrontOffice.Reservation;
using ASMILE.BO.Core.FrontOffice.Reservation;
using ASMILE.BO.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using ReservationAggregate = ASMILE.BO.Core.FrontOffice.Reservation.Reservation;

namespace ASMILE.BO.Infrastructure.FrontOffice.Reservation;

public sealed class EfReservationRepository(AppDbContext dbContext) : IReservationRepository
{
    public Task AddAsync(ReservationAggregate reservation, ReservationHistory history, CancellationToken cancellationToken = default) { dbContext.FrontOfficeReservations.Add(reservation); dbContext.FrontOfficeReservationHistory.Add(history); return Task.CompletedTask; }
    public Task<ReservationAggregate?> GetAsync(Guid id, Guid propertyId, CancellationToken cancellationToken = default) => dbContext.FrontOfficeReservations.SingleOrDefaultAsync(x => x.ReservationId == id && x.PropertyId == propertyId, cancellationToken);
    public Task AppendHistoryAsync(ReservationHistory history, CancellationToken cancellationToken = default) { dbContext.FrontOfficeReservationHistory.Add(history); return Task.CompletedTask; }
    public void ApplyExpectedRowVersion(ReservationAggregate reservation, byte[] expectedRowVersion) => dbContext.Entry(reservation).Property(x => x.RowVersion).OriginalValue = expectedRowVersion;
    public Task<ReservationDto?> GetDtoAsync(Guid id, Guid propertyId, CancellationToken cancellationToken = default) => dbContext.FrontOfficeReservations.AsNoTracking().Where(x => x.ReservationId == id && x.PropertyId == propertyId).Select(ToDto).SingleOrDefaultAsync(cancellationToken);
    public Task<ReservationDto?> GetByNumberAsync(Guid propertyId, string reservationNumber, CancellationToken cancellationToken = default) => dbContext.FrontOfficeReservations.AsNoTracking().Where(x => x.PropertyId == propertyId && x.ReservationNumber == reservationNumber).Select(ToDto).SingleOrDefaultAsync(cancellationToken);
    public async Task<PageResult<ReservationDto>> SearchAsync(Guid propertyId, string? keyword, ReservationStatus? status, DateOnly? dateFrom, DateOnly? dateTo, PageRequest page, string? sort, CancellationToken cancellationToken = default)
    {
        var query = dbContext.FrontOfficeReservations.AsNoTracking().Where(x => x.PropertyId == propertyId);
        if (!string.IsNullOrWhiteSpace(keyword)) query = query.Where(x => x.ReservationNumber.Contains(keyword) || x.GuestNameSnapshot.Contains(keyword));
        if (status is not null) query = query.Where(x => x.Status == status);
        if (dateFrom is not null) query = query.Where(x => x.DepartureDate > dateFrom);
        if (dateTo is not null) query = query.Where(x => x.ArrivalDate < dateTo);
        query = sort?.ToLowerInvariant() switch { "number" => query.OrderBy(x => x.ReservationNumber), "arrival" => query.OrderBy(x => x.ArrivalDate), _ => query.OrderByDescending(x => x.UpdatedAtUtc) };
        var total = await query.CountAsync(cancellationToken); var items = await query.Skip(page.Skip).Take(Math.Clamp(page.PageSize, 1, 200)).Select(ToDto).ToListAsync(cancellationToken);
        return new(items, total, page.Page, page.PageSize);
    }
    public async Task<RoomConflictResult> CheckAsync(Guid propertyId, Guid roomId, DateOnly arrival, DateOnly departure, Guid? excludedReservationId, CancellationToken cancellationToken = default)
    {
        var conflict = await dbContext.FrontOfficeReservations.AsNoTracking().Where(x => x.PropertyId == propertyId && x.AssignedRoomId == roomId && (x.Status == ReservationStatus.Tentative || x.Status == ReservationStatus.Confirmed) && (excludedReservationId == null || x.ReservationId != excludedReservationId) && x.ArrivalDate < departure && x.DepartureDate > arrival).Select(x => (Guid?)x.ReservationId).FirstOrDefaultAsync(cancellationToken);
        return new(conflict is not null, conflict);
    }
    private static readonly System.Linq.Expressions.Expression<Func<ReservationAggregate, ReservationDto>> ToDto = x => new ReservationDto(x.ReservationId, x.ReservationNumber, x.PropertyId, x.GuestNameSnapshot, x.ArrivalDate, x.DepartureDate, x.AdultCount, x.ChildCount, x.RoomTypeId, x.AssignedRoomId, x.Status, x.Source, x.UpdatedAtUtc, x.RowVersion);
}
