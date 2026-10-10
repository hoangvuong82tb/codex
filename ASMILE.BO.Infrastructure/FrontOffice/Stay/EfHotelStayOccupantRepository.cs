using ASMILE.BO.Application.FrontOffice.Stay;
using ASMILE.BO.Core.FrontOffice.Stay;
using ASMILE.BO.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ASMILE.BO.Infrastructure.FrontOffice.Stay;

public sealed class EfHotelStayOccupantRepository(AppDbContext db) : IHotelStayOccupantRepository
{
    public async Task<IReadOnlyList<StayOccupantDto>> GetActiveAsync(Guid propertyId, Guid hotelStayId, CancellationToken cancellationToken = default)
        => await (from occupant in db.HotelStayOccupants.AsNoTracking()
                  join guest in db.GuestProfiles.AsNoTracking() on occupant.GuestProfileId equals guest.GuestProfileId
                  where occupant.PropertyId == propertyId
                        && occupant.HotelStayId == hotelStayId
                        && occupant.IsActive
                        && guest.PropertyId == propertyId
                  orderby occupant.AddedAtUtc, occupant.GuestNameSnapshot
                  select new StayOccupantDto(
                      occupant.OccupantId,
                      occupant.PropertyId,
                      occupant.HotelStayId,
                      occupant.GuestProfileId,
                      guest.GuestNumber,
                      occupant.GuestNameSnapshot,
                      occupant.GuestPhoneSnapshot,
                      occupant.OccupantType,
                      occupant.AddedAtUtc,
                      occupant.IsActive,
                      occupant.RowVersion)).ToListAsync(cancellationToken);

    public Task<bool> ExistsActiveAsync(Guid propertyId, Guid hotelStayId, Guid guestProfileId, CancellationToken cancellationToken = default)
        => db.HotelStayOccupants.AsNoTracking().AnyAsync(x => x.PropertyId == propertyId
            && x.HotelStayId == hotelStayId
            && x.GuestProfileId == guestProfileId
            && x.IsActive, cancellationToken);

    public Task<HotelStayOccupant?> GetAsync(Guid propertyId, Guid hotelStayId, Guid occupantId, CancellationToken cancellationToken = default)
        => db.HotelStayOccupants.SingleOrDefaultAsync(x => x.PropertyId == propertyId
            && x.HotelStayId == hotelStayId
            && x.OccupantId == occupantId, cancellationToken);

    public Task AddAsync(HotelStayOccupant occupant, CancellationToken cancellationToken = default)
    {
        db.HotelStayOccupants.Add(occupant);
        return Task.CompletedTask;
    }

    public void ApplyExpectedRowVersion(HotelStayOccupant occupant, byte[] expectedRowVersion)
        => db.Entry(occupant).Property(x => x.RowVersion).OriginalValue = expectedRowVersion;

    public Task<StayOccupantDto?> GetDtoAsync(Guid propertyId, Guid occupantId, CancellationToken cancellationToken = default)
        => (from occupant in db.HotelStayOccupants.AsNoTracking()
            join guest in db.GuestProfiles.AsNoTracking() on occupant.GuestProfileId equals guest.GuestProfileId
            where occupant.PropertyId == propertyId && occupant.OccupantId == occupantId && guest.PropertyId == propertyId
            select new StayOccupantDto(
                occupant.OccupantId,
                occupant.PropertyId,
                occupant.HotelStayId,
                occupant.GuestProfileId,
                guest.GuestNumber,
                occupant.GuestNameSnapshot,
                occupant.GuestPhoneSnapshot,
                occupant.OccupantType,
                occupant.AddedAtUtc,
                occupant.IsActive,
                occupant.RowVersion)).SingleOrDefaultAsync(cancellationToken);
}
