using ASMILE.BO.Application.FrontOffice.Foundation.Cqrs;
using ASMILE.BO.Core.FrontOffice.HotelMasterData;

namespace ASMILE.BO.Application.FrontOffice.HotelMasterData;

public sealed record HotelRoomTypeDto(Guid HotelRoomTypeId, string Code, string Name, string? Description, int StandardAdultCapacity, int StandardChildCapacity, int MaximumAdultCapacity, int MaximumChildCapacity, int DefaultOccupancy, int DisplayOrder, bool IsActive, DateTime UpdatedAtUtc, byte[]? RowVersion);
public sealed record HotelRoomDto(Guid HotelRoomId, string RoomNumber, string? DisplayName, Guid HotelRoomTypeId, string HotelRoomTypeCode, string HotelRoomTypeName, string? Floor, string? Building, string? Zone, HotelRoomOperationalStatus OperationalStatus, bool IsActive, DateTime UpdatedAtUtc, byte[]? RowVersion);
public sealed record HotelRoomTypeReference(Guid HotelRoomTypeId, bool IsActive);
public sealed record HotelRoomReference(Guid HotelRoomId, Guid HotelRoomTypeId, bool IsActive, bool HotelRoomTypeIsActive, HotelRoomOperationalStatus OperationalStatus)
{ public bool IsAssignable => IsActive && HotelRoomTypeIsActive && OperationalStatus == HotelRoomOperationalStatus.Available; }

public interface IHotelRoomMasterDataRepository
{
    Task AddRoomTypeAsync(HotelRoomType roomType, CancellationToken ct = default);
    Task AddRoomAsync(HotelRoom room, CancellationToken ct = default);
    Task<HotelRoomType?> GetRoomTypeAsync(Guid propertyId, Guid id, CancellationToken ct = default);
    Task<HotelRoom?> GetRoomAsync(Guid propertyId, Guid id, CancellationToken ct = default);
    Task<bool> RoomTypeCodeExistsAsync(Guid propertyId, string code, Guid? excludingId, CancellationToken ct = default);
    Task<bool> RoomNumberExistsAsync(Guid propertyId, string roomNumber, Guid? excludingId, CancellationToken ct = default);
    void UseExpectedRowVersion(object aggregate, byte[] expectedRowVersion);
    Task<HotelRoomTypeDto?> GetRoomTypeDtoAsync(Guid propertyId, Guid id, CancellationToken ct = default);
    Task<HotelRoomTypeDto?> GetRoomTypeByCodeAsync(Guid propertyId, string code, CancellationToken ct = default);
    Task<PageResult<HotelRoomTypeDto>> SearchRoomTypesAsync(Guid propertyId, string? keyword, bool? isActive, PageRequest page, string? sort, CancellationToken ct = default);
    Task<HotelRoomDto?> GetRoomDtoAsync(Guid propertyId, Guid id, CancellationToken ct = default);
    Task<HotelRoomDto?> GetRoomByNumberAsync(Guid propertyId, string number, CancellationToken ct = default);
    Task<PageResult<HotelRoomDto>> SearchRoomsAsync(Guid propertyId, string? keyword, Guid? roomTypeId, string? floor, string? building, HotelRoomOperationalStatus? status, bool? isActive, PageRequest page, string? sort, bool assignableOnly, CancellationToken ct = default);
}

public interface IHotelReservationReferenceReader
{
    Task<HotelRoomTypeReference?> GetRoomTypeReferenceAsync(Guid propertyId, Guid roomTypeId, CancellationToken ct = default);
    Task<HotelRoomReference?> GetRoomReferenceAsync(Guid propertyId, Guid roomId, CancellationToken ct = default);
    Task<bool> HotelRoomMatchesRoomTypeAsync(Guid propertyId, Guid roomId, Guid roomTypeId, CancellationToken ct = default);
}

public sealed record CreateHotelRoomTypeCommand(string Code, string Name, string? Description, int StandardAdultCapacity, int StandardChildCapacity, int MaximumAdultCapacity, int MaximumChildCapacity, int DefaultOccupancy, int DisplayOrder) : ICommand<HotelRoomTypeDto>;
public sealed record UpdateHotelRoomTypeCommand(Guid HotelRoomTypeId, string Code, string Name, string? Description, int StandardAdultCapacity, int StandardChildCapacity, int MaximumAdultCapacity, int MaximumChildCapacity, int DefaultOccupancy, int DisplayOrder, byte[] ExpectedRowVersion) : ICommand<HotelRoomTypeDto>;
public sealed record ActivateHotelRoomTypeCommand(Guid HotelRoomTypeId, byte[] ExpectedRowVersion) : ICommand<HotelRoomTypeDto>;
public sealed record DeactivateHotelRoomTypeCommand(Guid HotelRoomTypeId, byte[] ExpectedRowVersion) : ICommand<HotelRoomTypeDto>;
public sealed record CreateHotelRoomCommand(Guid HotelRoomTypeId, string RoomNumber, string? DisplayName, string? Floor, string? Building, string? Zone, string? Description) : ICommand<HotelRoomDto>;
public sealed record UpdateHotelRoomCommand(Guid HotelRoomId, string RoomNumber, string? DisplayName, string? Floor, string? Building, string? Zone, string? Description, byte[] ExpectedRowVersion) : ICommand<HotelRoomDto>;
public sealed record ChangeHotelRoomTypeCommand(Guid HotelRoomId, Guid HotelRoomTypeId, byte[] ExpectedRowVersion) : ICommand<HotelRoomDto>;
public sealed record ChangeHotelRoomOperationalStatusCommand(Guid HotelRoomId, HotelRoomOperationalStatus OperationalStatus, byte[] ExpectedRowVersion) : ICommand<HotelRoomDto>;
public sealed record ActivateHotelRoomCommand(Guid HotelRoomId, byte[] ExpectedRowVersion) : ICommand<HotelRoomDto>;
public sealed record DeactivateHotelRoomCommand(Guid HotelRoomId, byte[] ExpectedRowVersion) : ICommand<HotelRoomDto>;
public sealed record GetHotelRoomTypeByIdQuery(Guid HotelRoomTypeId) : IQuery<HotelRoomTypeDto>;
public sealed record GetHotelRoomTypeByCodeQuery(string Code) : IQuery<HotelRoomTypeDto>;
public sealed record SearchHotelRoomTypesQuery(string? Keyword, bool? IsActive, PageRequest Page, string? Sort) : IQuery<PageResult<HotelRoomTypeDto>>;
public sealed record GetActiveHotelRoomTypesQuery(PageRequest Page, string? Sort) : IQuery<PageResult<HotelRoomTypeDto>>;
public sealed record GetHotelRoomByIdQuery(Guid HotelRoomId) : IQuery<HotelRoomDto>;
public sealed record GetHotelRoomByNumberQuery(string RoomNumber) : IQuery<HotelRoomDto>;
public sealed record SearchHotelRoomsQuery(string? Keyword, Guid? HotelRoomTypeId, string? Floor, string? Building, HotelRoomOperationalStatus? OperationalStatus, bool? IsActive, PageRequest Page, string? Sort) : IQuery<PageResult<HotelRoomDto>>;
public sealed record GetAssignableHotelRoomsQuery(PageRequest Page, string? Sort) : IQuery<PageResult<HotelRoomDto>>;
