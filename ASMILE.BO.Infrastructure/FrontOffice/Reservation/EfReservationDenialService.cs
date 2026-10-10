using ASMILE.BO.Application.FrontOffice.Reservation;
using ASMILE.BO.Application.FrontOffice.Foundation.Contexts;
using ASMILE.BO.Core.FrontOffice.Reservation;
using ASMILE.BO.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ASMILE.BO.Infrastructure.FrontOffice.Reservation;

public sealed class EfReservationDenialService(AppDbContext db, IClock clock) : IReservationDenialService
{
    public async Task<IReadOnlyList<ReservationDenialDto>> GetAllAsync(Guid propertyId, string? keyword, ReservationDenialStatus? status, CancellationToken ct)
    {
        var query = db.ReservationDenials.AsNoTracking().Where(x => x.PropertyId == propertyId);
        if (status is not null) query = query.Where(x => x.Status == status.Value);
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var value = keyword.Trim();
            query = query.Where(x => x.DenialNumber.Contains(value) || x.GuestName.Contains(value) || (x.GuestPhone != null && x.GuestPhone.Contains(value)));
        }
        return await query.OrderByDescending(x => x.CreatedAtUtc).Select(MapExpression).ToListAsync(ct);
    }

    public async Task<ReservationDenialDto> CreateAsync(Guid propertyId, string guestName, string? guestPhone, DateOnly arrivalDate, DateOnly departureDate, int adultCount, int childCount, Guid? roomTypeId, string source, ReservationDenialReason reason, string? notes, CancellationToken ct)
    {
        var denial = ReservationDenial.Create(propertyId, $"DEN-{DateTime.UtcNow:yyyyMMddHHmmss}-{Random.Shared.Next(100, 999)}", guestName, guestPhone, arrivalDate, departureDate, adultCount, childCount, roomTypeId, source, reason, notes, clock.UtcNow);
        db.ReservationDenials.Add(denial);
        await db.SaveChangesAsync(ct);
        return Map(denial);
    }

    public async Task<ReservationDenialDto> CloseAsync(Guid propertyId, Guid denialId, CancellationToken ct)
    {
        var denial = await db.ReservationDenials.SingleOrDefaultAsync(x => x.PropertyId == propertyId && x.DenialId == denialId, ct)
            ?? throw new KeyNotFoundException("RESERVATION_DENIAL_NOT_FOUND");
        denial.Close(clock.UtcNow);
        await db.SaveChangesAsync(ct);
        return Map(denial);
    }

    private static ReservationDenialDto Map(ReservationDenial x) => new(x.DenialId, x.DenialNumber, x.GuestName, x.GuestPhone, x.ArrivalDate, x.DepartureDate, x.AdultCount, x.ChildCount, x.RoomTypeId, x.Source, x.Reason, x.Status, x.Notes, x.RecoveredReservationId, x.CreatedAtUtc, x.ClosedAtUtc, x.RowVersion);
    private static readonly System.Linq.Expressions.Expression<Func<ReservationDenial, ReservationDenialDto>> MapExpression = x => new(x.DenialId, x.DenialNumber, x.GuestName, x.GuestPhone, x.ArrivalDate, x.DepartureDate, x.AdultCount, x.ChildCount, x.RoomTypeId, x.Source, x.Reason, x.Status, x.Notes, x.RecoveredReservationId, x.CreatedAtUtc, x.ClosedAtUtc, x.RowVersion);
}
