using ASMILE.BO.Application.FrontOffice.Foundation.Cqrs;
using ASMILE.BO.Application.FrontOffice.HotelMasterData;
using ASMILE.BO.Core.FrontOffice.HotelMasterData;
using ASMILE.BO.Core.FrontOffice.Housekeeping;
using ASMILE.BO.Core.FrontOffice.Stay;
using ASMILE.BO.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ASMILE.BO.Infrastructure.FrontOffice.HotelMasterData;

public sealed class EfHotelRoomMasterDataRepository(AppDbContext dbContext) : IHotelRoomMasterDataRepository, IHotelReservationReferenceReader
{
    public Task AddRoomTypeAsync(HotelRoomType roomType, CancellationToken ct = default) { dbContext.HotelRoomTypes.Add(roomType); return Task.CompletedTask; }
    public Task AddRoomAsync(HotelRoom room, CancellationToken ct = default) { dbContext.HotelRooms.Add(room); return Task.CompletedTask; }
    public Task<HotelRoomType?> GetRoomTypeAsync(Guid propertyId, Guid id, CancellationToken ct = default) => dbContext.HotelRoomTypes.SingleOrDefaultAsync(x => x.PropertyId == propertyId && x.HotelRoomTypeId == id, ct);
    public Task<HotelRoom?> GetRoomAsync(Guid propertyId, Guid id, CancellationToken ct = default) => dbContext.HotelRooms.SingleOrDefaultAsync(x => x.PropertyId == propertyId && x.HotelRoomId == id, ct);
    public Task<bool> RoomTypeCodeExistsAsync(Guid propertyId, string code, Guid? excludingId, CancellationToken ct = default) => dbContext.HotelRoomTypes.AnyAsync(x => x.PropertyId == propertyId && x.Code == code && (excludingId == null || x.HotelRoomTypeId != excludingId), ct);
    public Task<bool> RoomNumberExistsAsync(Guid propertyId, string roomNumber, Guid? excludingId, CancellationToken ct = default) => dbContext.HotelRooms.AnyAsync(x => x.PropertyId == propertyId && x.RoomNumber == roomNumber && (excludingId == null || x.HotelRoomId != excludingId), ct);
    public void UseExpectedRowVersion(object aggregate, byte[] expectedRowVersion) => dbContext.Entry(aggregate).Property("RowVersion").OriginalValue = expectedRowVersion;
    public Task<HotelRoomTypeDto?> GetRoomTypeDtoAsync(Guid propertyId, Guid id, CancellationToken ct = default) => dbContext.HotelRoomTypes.AsNoTracking().Where(x => x.HotelRoomTypeId == id && x.PropertyId == propertyId).Select(TypeDto).SingleOrDefaultAsync(ct);
    public Task<HotelRoomTypeDto?> GetRoomTypeByCodeAsync(Guid propertyId, string code, CancellationToken ct = default) => dbContext.HotelRoomTypes.AsNoTracking().Where(x => x.PropertyId == propertyId && x.Code == code).Select(TypeDto).SingleOrDefaultAsync(ct);
    public async Task<PageResult<HotelRoomTypeDto>> SearchRoomTypesAsync(Guid propertyId, string? keyword, bool? isActive, PageRequest page, string? sort, CancellationToken ct = default)
    { var q = dbContext.HotelRoomTypes.AsNoTracking().Where(x => x.PropertyId == propertyId); if (!string.IsNullOrWhiteSpace(keyword)) q = q.Where(x => x.Code.Contains(keyword) || x.Name.Contains(keyword)); if (isActive is not null) q = q.Where(x => x.IsActive == isActive); q = sort?.ToLowerInvariant() switch { "code" => q.OrderBy(x => x.Code), "name" => q.OrderBy(x => x.Name), "updated" => q.OrderByDescending(x => x.UpdatedAtUtc), _ => q.OrderBy(x => x.DisplayOrder).ThenBy(x => x.Code) }; return await Page(q.Select(TypeDto), page, ct); }
    public Task<HotelRoomDto?> GetRoomDtoAsync(Guid propertyId, Guid id, CancellationToken ct = default) => RoomDtos(dbContext.HotelRooms.AsNoTracking().Where(x => x.HotelRoomId == id && x.PropertyId == propertyId)).SingleOrDefaultAsync(ct);
    public Task<HotelRoomDto?> GetRoomByNumberAsync(Guid propertyId, string number, CancellationToken ct = default) => RoomDtos(dbContext.HotelRooms.AsNoTracking().Where(x => x.PropertyId == propertyId && x.RoomNumber == number)).SingleOrDefaultAsync(ct);
    public async Task<PageResult<HotelRoomDto>> SearchRoomsAsync(Guid propertyId, string? keyword, Guid? roomTypeId, string? floor, string? building, HotelRoomOperationalStatus? status, bool? isActive, PageRequest page, string? sort, bool assignableOnly, CancellationToken ct = default, HousekeepingRoomStatus? housekeepingStatus = null, string? frontOfficeStatus = null, string? roomClass = null, string? bedType = null, string? view = null, string? roomCondition = null, bool smokingOnly = false, bool connectingOnly = false)
    {
        var q = dbContext.HotelRooms.AsNoTracking().Where(x => x.PropertyId == propertyId);
        if (!string.IsNullOrWhiteSpace(keyword)) q = q.Where(x => x.RoomNumber.Contains(keyword) || (x.DisplayName != null && x.DisplayName.Contains(keyword)));
        if (roomTypeId is not null) q = q.Where(x => x.HotelRoomTypeId == roomTypeId);
        if (!string.IsNullOrWhiteSpace(floor)) q = q.Where(x => x.Floor == floor);
        if (!string.IsNullOrWhiteSpace(building)) q = q.Where(x => x.Building == building);
        if (status is not null) q = q.Where(x => x.OperationalStatus == status);
        if (isActive is not null) q = q.Where(x => x.IsActive == isActive);
        if (!string.IsNullOrWhiteSpace(roomClass)) q = q.Where(x => dbContext.HotelRoomTypes.Any(t => t.HotelRoomTypeId == x.HotelRoomTypeId && t.Name == roomClass));
        if (!string.IsNullOrWhiteSpace(bedType)) q = q.Where(x => x.BedType == bedType);
        if (!string.IsNullOrWhiteSpace(view)) q = q.Where(x => x.View == view);
        if (!string.IsNullOrWhiteSpace(roomCondition)) q = q.Where(x => x.RoomCondition == roomCondition);
        if (smokingOnly) q = q.Where(x => x.IsSmoking);
        if (connectingOnly) q = q.Where(x => x.IsConnecting);
        if (housekeepingStatus is not null)
        {
            var statusText = housekeepingStatus.Value.ToString();
            q = q.Where(room =>
                dbContext.RoomHousekeepingStates.Any(state =>
                    state.PropertyId == propertyId &&
                    state.HotelRoomId == room.HotelRoomId &&
                    state.Status == housekeepingStatus.Value)
                || (!dbContext.RoomHousekeepingStates.Any(state =>
                        state.PropertyId == propertyId && state.HotelRoomId == room.HotelRoomId)
                    && room.HousekeepingStatus == statusText));
        }
        if (frontOfficeStatus is "OC" or "OD")
        {
            q = q.Where(room => dbContext.HotelStays.Any(stay =>
                stay.PropertyId == propertyId &&
                stay.RoomId == room.HotelRoomId &&
                stay.Status == HotelStayStatus.InHouse));
        }
        else if (frontOfficeStatus is "VC" or "VD")
        {
            q = q.Where(room => !dbContext.HotelStays.Any(stay =>
                stay.PropertyId == propertyId &&
                stay.RoomId == room.HotelRoomId &&
                stay.Status == HotelStayStatus.InHouse));
        }
        if (frontOfficeStatus is "VD" or "OD")
        {
            q = q.Where(room =>
                dbContext.RoomHousekeepingStates.Any(state =>
                    state.PropertyId == propertyId &&
                    state.HotelRoomId == room.HotelRoomId &&
                    (state.Status == HousekeepingRoomStatus.VacantDirty || state.Status == HousekeepingRoomStatus.OccupiedDirty))
                || (!dbContext.RoomHousekeepingStates.Any(state =>
                        state.PropertyId == propertyId && state.HotelRoomId == room.HotelRoomId)
                    && room.HousekeepingStatus != null
                    && (room.HousekeepingStatus == "VacantDirty" || room.HousekeepingStatus == "OccupiedDirty" || room.HousekeepingStatus == "Dirty")));
        }
        else if (frontOfficeStatus is "VC" or "OC")
        {
            q = q.Where(room =>
                !dbContext.RoomHousekeepingStates.Any(state =>
                    state.PropertyId == propertyId &&
                    state.HotelRoomId == room.HotelRoomId &&
                    (state.Status == HousekeepingRoomStatus.VacantDirty || state.Status == HousekeepingRoomStatus.OccupiedDirty))
                && (!dbContext.RoomHousekeepingStates.Any(state =>
                        state.PropertyId == propertyId && state.HotelRoomId == room.HotelRoomId)
                    || room.HousekeepingStatus == null
                    || (room.HousekeepingStatus != "VacantDirty" && room.HousekeepingStatus != "OccupiedDirty" && room.HousekeepingStatus != "Dirty")));
        }
        if (assignableOnly) q = q.Where(x => x.IsActive && x.OperationalStatus == HotelRoomOperationalStatus.Available && dbContext.HotelRoomTypes.Any(t => t.HotelRoomTypeId == x.HotelRoomTypeId && t.PropertyId == propertyId && t.IsActive));
        q = sort?.ToLowerInvariant() switch { "number" => q.OrderBy(x => x.RoomNumber), "floor" => q.OrderBy(x => x.Floor).ThenBy(x => x.RoomNumber), "updated" => q.OrderByDescending(x => x.UpdatedAtUtc), _ => q.OrderBy(x => x.RoomNumber) };
        return await Page(RoomDtos(q), page, ct);
    }
    public Task<HotelRoomTypeReference?> GetRoomTypeReferenceAsync(Guid propertyId, Guid roomTypeId, CancellationToken ct = default) => dbContext.HotelRoomTypes.AsNoTracking().Where(x => x.PropertyId == propertyId && x.HotelRoomTypeId == roomTypeId).Select(x => new HotelRoomTypeReference(x.HotelRoomTypeId, x.IsActive)).SingleOrDefaultAsync(ct);
    public Task<HotelRoomReference?> GetRoomReferenceAsync(Guid propertyId, Guid roomId, CancellationToken ct = default) => dbContext.HotelRooms.AsNoTracking().Where(x => x.PropertyId == propertyId && x.HotelRoomId == roomId).Join(dbContext.HotelRoomTypes.AsNoTracking(), room => room.HotelRoomTypeId, type => type.HotelRoomTypeId, (room, type) => new HotelRoomReference(room.HotelRoomId, room.HotelRoomTypeId, room.IsActive, type.IsActive, room.OperationalStatus)).SingleOrDefaultAsync(ct);
    public Task<bool> HotelRoomMatchesRoomTypeAsync(Guid propertyId, Guid roomId, Guid roomTypeId, CancellationToken ct = default) => dbContext.HotelRooms.AsNoTracking().AnyAsync(x => x.PropertyId == propertyId && x.HotelRoomId == roomId && x.HotelRoomTypeId == roomTypeId, ct);
    public Task<bool> IsActiveBookingSourceAsync(Guid propertyId, Guid id, CancellationToken ct = default) => dbContext.HotelBookingSourceProfiles.AsNoTracking().AnyAsync(x => x.PropertyId == propertyId && x.HotelBookingSourceProfileId == id && x.IsActive, ct);
    public Task<bool> IsActiveBookingChannelAsync(Guid propertyId, Guid id, CancellationToken ct = default) => dbContext.HotelBookingChannelProfiles.AsNoTracking().AnyAsync(x => x.PropertyId == propertyId && x.HotelBookingChannelProfileId == id && x.IsActive, ct);
    public Task<bool> IsActiveMarketSegmentAsync(Guid propertyId, Guid id, CancellationToken ct = default) => dbContext.HotelMarketSegmentProfiles.AsNoTracking().AnyAsync(x => x.PropertyId == propertyId && x.HotelMarketSegmentProfileId == id && x.IsActive, ct);
    public Task<bool> IsActiveCompanyAsync(Guid propertyId, Guid id, CancellationToken ct = default) => dbContext.HotelCompanyProfiles.AsNoTracking().AnyAsync(x => x.PropertyId == propertyId && x.HotelCompanyProfileId == id && x.IsActive, ct);
    public Task<bool> IsActiveTravelAgentAsync(Guid propertyId, Guid id, CancellationToken ct = default) => dbContext.HotelTravelAgentProfiles.AsNoTracking().AnyAsync(x => x.PropertyId == propertyId && x.HotelTravelAgentProfileId == id && x.IsActive, ct);
    public Task<bool> IsActiveCommissionRuleAsync(Guid propertyId, Guid id, CancellationToken ct = default) => dbContext.HotelCommissionRuleProfiles.AsNoTracking().AnyAsync(x => x.PropertyId == propertyId && x.HotelCommissionRuleProfileId == id && x.IsActive, ct);
    private static readonly System.Linq.Expressions.Expression<Func<HotelRoomType, HotelRoomTypeDto>> TypeDto = x => new HotelRoomTypeDto(x.HotelRoomTypeId, x.Code, x.Name, x.Description, x.StandardAdultCapacity, x.StandardChildCapacity, x.MaximumAdultCapacity, x.MaximumChildCapacity, x.DefaultOccupancy, x.DisplayOrder, x.IsActive, x.UpdatedAtUtc, x.RowVersion);
    private IQueryable<HotelRoomDto> RoomDtos(IQueryable<HotelRoom> rooms) => rooms.Join(dbContext.HotelRoomTypes.AsNoTracking(), room => room.HotelRoomTypeId, type => type.HotelRoomTypeId, (room, type) => new HotelRoomDto(room.HotelRoomId, room.RoomNumber, room.DisplayName, room.HotelRoomTypeId, type.Code, type.Name, room.Floor, room.Building, room.Zone, room.OperationalStatus, room.IsActive, room.UpdatedAtUtc, room.RowVersion, room.OperationalStatusChangedAtUtc, room.OperationalStatusExpiresAtUtc, room.OperationalStatusReason, room.RoomCondition, room.View, room.BedType, room.IsSmoking, room.IsConnecting, room.HotelAreaId, room.HousekeepingStatus));
    private static async Task<PageResult<T>> Page<T>(IQueryable<T> query, PageRequest page, CancellationToken ct) { var total = await query.CountAsync(ct); var size = Math.Clamp(page.PageSize, 1, 200); var items = await query.Skip(page.Skip).Take(size).ToListAsync(ct); return new PageResult<T>(items, total, page.Page, page.PageSize); }
}
