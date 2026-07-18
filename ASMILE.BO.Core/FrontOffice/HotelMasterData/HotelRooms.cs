using ASMILE.BO.Core.FrontOffice.Foundation.Identifiers;

namespace ASMILE.BO.Core.FrontOffice.HotelMasterData;

public enum HotelRoomOperationalStatus { Available, OutOfOrder, OutOfService }

public sealed class HotelRoomType
{
    private HotelRoomType() { }
    public Guid HotelRoomTypeId { get; private set; }
    public Guid PropertyId { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public int StandardAdultCapacity { get; private set; }
    public int StandardChildCapacity { get; private set; }
    public int MaximumAdultCapacity { get; private set; }
    public int MaximumChildCapacity { get; private set; }
    public int DefaultOccupancy { get; private set; }
    public int DisplayOrder { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public Guid? CreatedBy { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }
    public Guid? UpdatedBy { get; private set; }
    public byte[]? RowVersion { get; private set; }

    public static HotelRoomType Create(PropertyId propertyId, string code, string name, string? description, int standardAdultCapacity, int standardChildCapacity, int maximumAdultCapacity, int maximumChildCapacity, int defaultOccupancy, int displayOrder, DateTime nowUtc, ActorId? actorId)
    {
        Validate(code, name, standardAdultCapacity, standardChildCapacity, maximumAdultCapacity, maximumChildCapacity, defaultOccupancy);
        return new HotelRoomType { HotelRoomTypeId = Uuid7.NewGuid(), PropertyId = propertyId.Value, Code = code.Trim(), Name = name.Trim(), Description = Normalize(description), StandardAdultCapacity = standardAdultCapacity, StandardChildCapacity = standardChildCapacity, MaximumAdultCapacity = maximumAdultCapacity, MaximumChildCapacity = maximumChildCapacity, DefaultOccupancy = defaultOccupancy, DisplayOrder = displayOrder, IsActive = true, CreatedAtUtc = nowUtc, UpdatedAtUtc = nowUtc, CreatedBy = actorId?.Value, UpdatedBy = actorId?.Value };
    }
    public void Update(string code, string name, string? description, int standardAdultCapacity, int standardChildCapacity, int maximumAdultCapacity, int maximumChildCapacity, int defaultOccupancy, int displayOrder, DateTime nowUtc, ActorId? actorId)
    {
        Validate(code, name, standardAdultCapacity, standardChildCapacity, maximumAdultCapacity, maximumChildCapacity, defaultOccupancy);
        Code = code.Trim(); Name = name.Trim(); Description = Normalize(description); StandardAdultCapacity = standardAdultCapacity; StandardChildCapacity = standardChildCapacity; MaximumAdultCapacity = maximumAdultCapacity; MaximumChildCapacity = maximumChildCapacity; DefaultOccupancy = defaultOccupancy; DisplayOrder = displayOrder; Touch(nowUtc, actorId);
    }
    public void Activate(DateTime nowUtc, ActorId? actorId) { IsActive = true; Touch(nowUtc, actorId); }
    public void Deactivate(DateTime nowUtc, ActorId? actorId) { IsActive = false; Touch(nowUtc, actorId); }
    private void Touch(DateTime nowUtc, ActorId? actorId) { UpdatedAtUtc = nowUtc; UpdatedBy = actorId?.Value; }
    private static void Validate(string code, string name, int standardAdults, int standardChildren, int maximumAdults, int maximumChildren, int defaultOccupancy)
    {
        if (string.IsNullOrWhiteSpace(code)) throw new ArgumentException("HOTEL_ROOM_TYPE_CODE_REQUIRED");
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("HOTEL_ROOM_TYPE_NAME_REQUIRED");
        if (standardAdults < 1 || standardChildren < 0 || maximumAdults < standardAdults || maximumChildren < standardChildren || defaultOccupancy < 1 || defaultOccupancy > maximumAdults + maximumChildren) throw new ArgumentException("HOTEL_ROOM_TYPE_INVALID_CAPACITY");
    }
    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

public sealed class HotelRoom
{
    private HotelRoom() { }
    public Guid HotelRoomId { get; private set; }
    public Guid PropertyId { get; private set; }
    public Guid HotelRoomTypeId { get; private set; }
    public string RoomNumber { get; private set; } = string.Empty;
    public string? DisplayName { get; private set; }
    public string? Floor { get; private set; }
    public string? Building { get; private set; }
    public string? Zone { get; private set; }
    public string? Description { get; private set; }
    public HotelRoomOperationalStatus OperationalStatus { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public Guid? CreatedBy { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }
    public Guid? UpdatedBy { get; private set; }
    public byte[]? RowVersion { get; private set; }
    public bool IsAssignable => IsActive && OperationalStatus == HotelRoomOperationalStatus.Available;

    public static HotelRoom Create(PropertyId propertyId, Guid hotelRoomTypeId, string roomNumber, string? displayName, string? floor, string? building, string? zone, string? description, DateTime nowUtc, ActorId? actorId)
    {
        if (hotelRoomTypeId == Guid.Empty) throw new ArgumentException("HOTEL_ROOM_TYPE_NOT_FOUND");
        if (string.IsNullOrWhiteSpace(roomNumber)) throw new ArgumentException("HOTEL_ROOM_NUMBER_REQUIRED");
        return new HotelRoom { HotelRoomId = Uuid7.NewGuid(), PropertyId = propertyId.Value, HotelRoomTypeId = hotelRoomTypeId, RoomNumber = roomNumber.Trim(), DisplayName = Normalize(displayName), Floor = Normalize(floor), Building = Normalize(building), Zone = Normalize(zone), Description = Normalize(description), OperationalStatus = HotelRoomOperationalStatus.Available, IsActive = true, CreatedAtUtc = nowUtc, UpdatedAtUtc = nowUtc, CreatedBy = actorId?.Value, UpdatedBy = actorId?.Value };
    }
    public void Update(string roomNumber, string? displayName, string? floor, string? building, string? zone, string? description, DateTime nowUtc, ActorId? actorId)
    { if (string.IsNullOrWhiteSpace(roomNumber)) throw new ArgumentException("HOTEL_ROOM_NUMBER_REQUIRED"); RoomNumber = roomNumber.Trim(); DisplayName = Normalize(displayName); Floor = Normalize(floor); Building = Normalize(building); Zone = Normalize(zone); Description = Normalize(description); Touch(nowUtc, actorId); }
    public void ChangeRoomType(Guid roomTypeId, DateTime nowUtc, ActorId? actorId) { if (roomTypeId == Guid.Empty) throw new ArgumentException("HOTEL_ROOM_TYPE_NOT_FOUND"); HotelRoomTypeId = roomTypeId; Touch(nowUtc, actorId); }
    public void ChangeOperationalStatus(HotelRoomOperationalStatus status, DateTime nowUtc, ActorId? actorId) { OperationalStatus = status; Touch(nowUtc, actorId); }
    public void Activate(DateTime nowUtc, ActorId? actorId) { IsActive = true; Touch(nowUtc, actorId); }
    public void Deactivate(DateTime nowUtc, ActorId? actorId) { IsActive = false; Touch(nowUtc, actorId); }
    private void Touch(DateTime nowUtc, ActorId? actorId) { UpdatedAtUtc = nowUtc; UpdatedBy = actorId?.Value; }
    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
