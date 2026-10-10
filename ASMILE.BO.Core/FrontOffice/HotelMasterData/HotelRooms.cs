using ASMILE.BO.Core.FrontOffice.Foundation.Identifiers;

namespace ASMILE.BO.Core.FrontOffice.HotelMasterData;

public enum HotelRoomOperationalStatus { Available, OutOfOrder, OutOfService }

public sealed class HotelBuilding
{
    private HotelBuilding() { }
    public Guid HotelBuildingId { get; private set; }
    public Guid PropertyId { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public int DisplayOrder { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public Guid? CreatedBy { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }
    public Guid? UpdatedBy { get; private set; }
    public byte[]? RowVersion { get; private set; }

    public static HotelBuilding Create(PropertyId propertyId, string code, string name, string? description, int displayOrder, DateTime nowUtc, ActorId? actorId)
    {
        Validate(code, name);
        return new HotelBuilding
        {
            HotelBuildingId = Uuid7.NewGuid(), PropertyId = propertyId.Value, Code = code.Trim(), Name = name.Trim(),
            Description = Normalize(description), DisplayOrder = displayOrder, IsActive = true,
            CreatedAtUtc = nowUtc, UpdatedAtUtc = nowUtc, CreatedBy = actorId?.Value, UpdatedBy = actorId?.Value
        };
    }

    public void Update(string code, string name, string? description, int displayOrder, DateTime nowUtc, ActorId? actorId)
    {
        Validate(code, name);
        Code = code.Trim(); Name = name.Trim(); Description = Normalize(description); DisplayOrder = displayOrder;
        Touch(nowUtc, actorId);
    }

    public void Activate(DateTime nowUtc, ActorId? actorId) { IsActive = true; Touch(nowUtc, actorId); }
    public void Deactivate(DateTime nowUtc, ActorId? actorId) { IsActive = false; Touch(nowUtc, actorId); }
    private void Touch(DateTime nowUtc, ActorId? actorId) { UpdatedAtUtc = nowUtc; UpdatedBy = actorId?.Value; }
    private static void Validate(string code, string name)
    {
        if (string.IsNullOrWhiteSpace(code)) throw new ArgumentException("HOTEL_BUILDING_CODE_REQUIRED");
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("HOTEL_BUILDING_NAME_REQUIRED");
    }
    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

public sealed class HotelFloor
{
    private HotelFloor() { }
    public Guid HotelFloorId { get; private set; }
    public Guid PropertyId { get; private set; }
    public Guid HotelBuildingId { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public int LevelNumber { get; private set; }
    public string? Description { get; private set; }
    public int DisplayOrder { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public Guid? CreatedBy { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }
    public Guid? UpdatedBy { get; private set; }
    public byte[]? RowVersion { get; private set; }

    public static HotelFloor Create(PropertyId propertyId, Guid buildingId, string code, string name, int levelNumber, string? description, int displayOrder, DateTime nowUtc, ActorId? actorId)
    {
        Validate(buildingId, code, name, levelNumber);
        return new HotelFloor
        {
            HotelFloorId = Uuid7.NewGuid(), PropertyId = propertyId.Value, HotelBuildingId = buildingId,
            Code = code.Trim(), Name = name.Trim(), LevelNumber = levelNumber, Description = Normalize(description),
            DisplayOrder = displayOrder, IsActive = true, CreatedAtUtc = nowUtc, UpdatedAtUtc = nowUtc,
            CreatedBy = actorId?.Value, UpdatedBy = actorId?.Value
        };
    }

    public void Update(Guid buildingId, string code, string name, int levelNumber, string? description, int displayOrder, DateTime nowUtc, ActorId? actorId)
    {
        Validate(buildingId, code, name, levelNumber);
        HotelBuildingId = buildingId; Code = code.Trim(); Name = name.Trim(); LevelNumber = levelNumber;
        Description = Normalize(description); DisplayOrder = displayOrder; Touch(nowUtc, actorId);
    }

    public void Activate(DateTime nowUtc, ActorId? actorId) { IsActive = true; Touch(nowUtc, actorId); }
    public void Deactivate(DateTime nowUtc, ActorId? actorId) { IsActive = false; Touch(nowUtc, actorId); }
    private void Touch(DateTime nowUtc, ActorId? actorId) { UpdatedAtUtc = nowUtc; UpdatedBy = actorId?.Value; }
    private static void Validate(Guid buildingId, string code, string name, int levelNumber)
    {
        if (buildingId == Guid.Empty) throw new ArgumentException("HOTEL_BUILDING_NOT_FOUND");
        if (string.IsNullOrWhiteSpace(code)) throw new ArgumentException("HOTEL_FLOOR_CODE_REQUIRED");
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("HOTEL_FLOOR_NAME_REQUIRED");
        if (levelNumber < -5 || levelNumber > 200) throw new ArgumentException("HOTEL_FLOOR_LEVEL_INVALID");
    }
    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

public sealed class HotelArea
{
    private HotelArea() { }
    public Guid HotelAreaId { get; private set; }
    public Guid PropertyId { get; private set; }
    public Guid? HotelBuildingId { get; private set; }
    public Guid? HotelFloorId { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string? AreaType { get; private set; }
    public string? Description { get; private set; }
    public int DisplayOrder { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public Guid? CreatedBy { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }
    public Guid? UpdatedBy { get; private set; }
    public byte[]? RowVersion { get; private set; }

    public static HotelArea Create(PropertyId propertyId, Guid? buildingId, Guid? floorId, string code, string name, string? areaType, string? description, int displayOrder, DateTime nowUtc, ActorId? actorId)
    {
        Validate(buildingId, floorId, code, name);
        return new HotelArea
        {
            HotelAreaId = Uuid7.NewGuid(), PropertyId = propertyId.Value, HotelBuildingId = buildingId, HotelFloorId = floorId,
            Code = code.Trim(), Name = name.Trim(), AreaType = Normalize(areaType), Description = Normalize(description),
            DisplayOrder = displayOrder, IsActive = true, CreatedAtUtc = nowUtc, UpdatedAtUtc = nowUtc,
            CreatedBy = actorId?.Value, UpdatedBy = actorId?.Value
        };
    }

    public void Update(Guid? buildingId, Guid? floorId, string code, string name, string? areaType, string? description, int displayOrder, DateTime nowUtc, ActorId? actorId)
    {
        Validate(buildingId, floorId, code, name);
        HotelBuildingId = buildingId; HotelFloorId = floorId; Code = code.Trim(); Name = name.Trim();
        AreaType = Normalize(areaType); Description = Normalize(description); DisplayOrder = displayOrder; Touch(nowUtc, actorId);
    }

    public void Activate(DateTime nowUtc, ActorId? actorId) { IsActive = true; Touch(nowUtc, actorId); }
    public void Deactivate(DateTime nowUtc, ActorId? actorId) { IsActive = false; Touch(nowUtc, actorId); }
    private void Touch(DateTime nowUtc, ActorId? actorId) { UpdatedAtUtc = nowUtc; UpdatedBy = actorId?.Value; }
    private static void Validate(Guid? buildingId, Guid? floorId, string code, string name)
    {
        if (floorId is not null && buildingId is null) throw new ArgumentException("HOTEL_AREA_BUILDING_REQUIRED_FOR_FLOOR");
        if (string.IsNullOrWhiteSpace(code)) throw new ArgumentException("HOTEL_AREA_CODE_REQUIRED");
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("HOTEL_AREA_NAME_REQUIRED");
    }
    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

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
    public Guid? HotelAreaId { get; private set; }
    public string? Description { get; private set; }
    public string? RoomCondition { get; private set; }
    public string? View { get; private set; }
    public string? BedType { get; private set; }
    public bool IsSmoking { get; private set; }
    public bool IsConnecting { get; private set; }
    public HotelRoomOperationalStatus OperationalStatus { get; private set; }
    public DateTime? OperationalStatusChangedAtUtc { get; private set; }
    public DateTime? OperationalStatusExpiresAtUtc { get; private set; }
    public string? OperationalStatusReason { get; private set; }
    public string? HousekeepingStatus { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public Guid? CreatedBy { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }
    public Guid? UpdatedBy { get; private set; }
    public byte[]? RowVersion { get; private set; }
    public bool IsAssignable => IsActive && OperationalStatus == HotelRoomOperationalStatus.Available;

    public static HotelRoom Create(PropertyId propertyId, Guid hotelRoomTypeId, string roomNumber, string? displayName, string? floor, string? building, string? zone, string? description, DateTime nowUtc, ActorId? actorId, string? roomCondition = null, string? view = null, string? bedType = null, bool isSmoking = false, bool isConnecting = false, Guid? hotelAreaId = null)
    {
        if (hotelRoomTypeId == Guid.Empty) throw new ArgumentException("HOTEL_ROOM_TYPE_NOT_FOUND");
        if (string.IsNullOrWhiteSpace(roomNumber)) throw new ArgumentException("HOTEL_ROOM_NUMBER_REQUIRED");
        return new HotelRoom { HotelRoomId = Uuid7.NewGuid(), PropertyId = propertyId.Value, HotelRoomTypeId = hotelRoomTypeId, RoomNumber = roomNumber.Trim(), DisplayName = Normalize(displayName), Floor = Normalize(floor), Building = Normalize(building), Zone = Normalize(zone), HotelAreaId = hotelAreaId, Description = Normalize(description), RoomCondition = Normalize(roomCondition), View = Normalize(view), BedType = Normalize(bedType), IsSmoking = isSmoking, IsConnecting = isConnecting, OperationalStatus = HotelRoomOperationalStatus.Available, IsActive = true, CreatedAtUtc = nowUtc, UpdatedAtUtc = nowUtc, CreatedBy = actorId?.Value, UpdatedBy = actorId?.Value };
    }
    public void Update(string roomNumber, string? displayName, string? floor, string? building, string? zone, string? description, DateTime nowUtc, ActorId? actorId, string? roomCondition = null, string? view = null, string? bedType = null, bool isSmoking = false, bool isConnecting = false, Guid? hotelAreaId = null)
    { if (string.IsNullOrWhiteSpace(roomNumber)) throw new ArgumentException("HOTEL_ROOM_NUMBER_REQUIRED"); RoomNumber = roomNumber.Trim(); DisplayName = Normalize(displayName); Floor = Normalize(floor); Building = Normalize(building); Zone = Normalize(zone); HotelAreaId = hotelAreaId; Description = Normalize(description); RoomCondition = Normalize(roomCondition); View = Normalize(view); BedType = Normalize(bedType); IsSmoking = isSmoking; IsConnecting = isConnecting; Touch(nowUtc, actorId); }
    public void ChangeRoomType(Guid roomTypeId, DateTime nowUtc, ActorId? actorId) { if (roomTypeId == Guid.Empty) throw new ArgumentException("HOTEL_ROOM_TYPE_NOT_FOUND"); HotelRoomTypeId = roomTypeId; Touch(nowUtc, actorId); }
    public void ChangeOperationalStatus(HotelRoomOperationalStatus status, DateTime nowUtc, ActorId? actorId, string? reason = null, DateTime? expiresAtUtc = null)
    {
        if (status is (HotelRoomOperationalStatus.OutOfOrder or HotelRoomOperationalStatus.OutOfService) && expiresAtUtc is not null && expiresAtUtc <= nowUtc)
            throw new ArgumentException("HOTEL_ROOM_LOCK_EXPIRY_NOT_LATER");
        OperationalStatus = status;
        OperationalStatusChangedAtUtc = nowUtc;
        OperationalStatusExpiresAtUtc = status == HotelRoomOperationalStatus.Available ? null : expiresAtUtc;
        OperationalStatusReason = status == HotelRoomOperationalStatus.Available ? null : Normalize(reason);
        Touch(nowUtc, actorId);
    }
    public void ExtendOperationalLock(DateTime newExpiresAtUtc, string reason, DateTime nowUtc, ActorId? actorId)
    {
        if (OperationalStatus is not (HotelRoomOperationalStatus.OutOfOrder or HotelRoomOperationalStatus.OutOfService))
            throw new InvalidOperationException("HOTEL_ROOM_NOT_LOCKED");
        if (newExpiresAtUtc <= nowUtc || (OperationalStatusExpiresAtUtc is not null && newExpiresAtUtc <= OperationalStatusExpiresAtUtc.Value))
            throw new ArgumentException("HOTEL_ROOM_LOCK_EXPIRY_NOT_LATER");
        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("HOTEL_ROOM_LOCK_REASON_REQUIRED");
        OperationalStatusExpiresAtUtc = newExpiresAtUtc;
        OperationalStatusReason = Normalize(reason);
        Touch(nowUtc, actorId);
    }
    public void Activate(DateTime nowUtc, ActorId? actorId) { IsActive = true; Touch(nowUtc, actorId); }
    public void Deactivate(DateTime nowUtc, ActorId? actorId) { IsActive = false; Touch(nowUtc, actorId); }
    private void Touch(DateTime nowUtc, ActorId? actorId) { UpdatedAtUtc = nowUtc; UpdatedBy = actorId?.Value; }
    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
