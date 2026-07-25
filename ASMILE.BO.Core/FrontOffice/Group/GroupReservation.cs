using ASMILE.BO.Core.FrontOffice.Foundation.Identifiers;

namespace ASMILE.BO.Core.FrontOffice.Group;

public enum GroupReservationStatus { Draft, Tentative, Confirmed, CheckedIn, CheckedOut, Cancelled, Closed }
public enum ChargeRouting { CompanyPaysAll, GuestPaysExtras, SplitPercentage, SplitFixed, Custom }

public sealed class GroupReservation
{
    private readonly List<GroupRoomAssignment> _roomingList = [];
    private GroupReservation() { }

    public Guid GroupReservationId { get; private set; }
    public Guid PropertyId { get; private set; }
    public string GroupCode { get; private set; } = string.Empty;
    public string GroupName { get; private set; } = string.Empty;
    public string? CompanyName { get; private set; }
    public string? AgencyName { get; private set; }
    public string? ContactPerson { get; private set; }
    public string? ContactPhone { get; private set; }
    public string? ContactEmail { get; private set; }
    public DateOnly ArrivalDate { get; private set; }
    public DateOnly DepartureDate { get; private set; }
    public int ExpectedRooms { get; private set; }
    public int ConfirmedRooms { get; private set; }
    public int TotalAdults { get; private set; }
    public int TotalChildren { get; private set; }
    public GroupReservationStatus Status { get; private set; }
    public string? Source { get; private set; }
    public string? SalesManager { get; private set; }
    public string? MarketSegment { get; private set; }
    public bool IsVip { get; private set; }
    public ChargeRouting Routing { get; private set; }
    public string? SpecialRequests { get; private set; }
    public string? Notes { get; private set; }
    public decimal TotalDeposit { get; private set; }
    public decimal TotalRevenue { get; private set; }
    public decimal OutstandingBalance { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public Guid? CreatedBy { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }
    public Guid? UpdatedBy { get; private set; }
    public byte[]? RowVersion { get; private set; }
    public IReadOnlyList<GroupRoomAssignment> RoomingList => _roomingList;

    public static GroupReservation Create(Guid propertyId, string code, string name, DateOnly arrival, DateOnly departure, int expectedRooms, int adults, int children, DateTime nowUtc, ActorId? actorId)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("GROUP_NAME_REQUIRED");
        if (departure <= arrival) throw new ArgumentException("GROUP_INVALID_DATES");
        return new GroupReservation { GroupReservationId = Uuid7.NewGuid(), PropertyId = propertyId, GroupCode = code, GroupName = name, ArrivalDate = arrival, DepartureDate = departure, ExpectedRooms = expectedRooms, TotalAdults = adults, TotalChildren = children, Status = GroupReservationStatus.Draft, CreatedAtUtc = nowUtc, UpdatedAtUtc = nowUtc, CreatedBy = actorId?.Value, UpdatedBy = actorId?.Value };
    }

    public GroupRoomAssignment AddRoom(Guid roomId, string roomNumber, string roomType, string? guestName, int adults, int children, DateTime nowUtc)
    {
        if (_roomingList.Any(r => r.HotelRoomId == roomId)) throw new InvalidOperationException("GROUP_ROOM_ALREADY_ASSIGNED");
        var assignment = GroupRoomAssignment.Create(GroupReservationId, PropertyId, roomId, roomNumber, roomType, guestName, adults, children, nowUtc);
        _roomingList.Add(assignment);
        ConfirmedRooms = _roomingList.Count;
        return assignment;
    }

    public void RemoveRoom(Guid roomId) { var r = _roomingList.FirstOrDefault(x => x.HotelRoomId == roomId); if (r is not null) { _roomingList.Remove(r); ConfirmedRooms = _roomingList.Count; } }

    public void Confirm(DateTime nowUtc) { RequireStatus(GroupReservationStatus.Draft, GroupReservationStatus.Tentative); Status = GroupReservationStatus.Confirmed; Touch(nowUtc); }
    public void Cancel(string reason, DateTime nowUtc) { Status = GroupReservationStatus.Cancelled; Touch(nowUtc); }
    public void Close(DateTime nowUtc) { Status = GroupReservationStatus.Closed; Touch(nowUtc); }
    private void Touch(DateTime nowUtc) => UpdatedAtUtc = nowUtc;
    private void RequireStatus(params GroupReservationStatus[] allowed) { if (!allowed.Contains(Status)) throw new InvalidOperationException($"GROUP_STATUS_{Status}"); }
}

public sealed class GroupRoomAssignment
{
    private GroupRoomAssignment() { }
    public Guid GroupRoomAssignmentId { get; private set; }
    public Guid GroupReservationId { get; private set; }
    public Guid PropertyId { get; private set; }
    public Guid HotelRoomId { get; private set; }
    public string RoomNumber { get; private set; } = string.Empty;
    public string RoomType { get; private set; } = string.Empty;
    public Guid? GuestId { get; private set; }
    public string? GuestName { get; private set; }
    public bool IsVip { get; private set; }
    public int Adults { get; private set; }
    public int Children { get; private set; }
    public bool IsCheckedIn { get; private set; }
    public bool IsCheckedOut { get; private set; }
    public Guid? ReservationId { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    public static GroupRoomAssignment Create(Guid groupId, Guid propertyId, Guid roomId, string roomNumber, string roomType, string? guestName, int adults, int children, DateTime nowUtc) => new()
    { GroupRoomAssignmentId = Uuid7.NewGuid(), GroupReservationId = groupId, PropertyId = propertyId, HotelRoomId = roomId, RoomNumber = roomNumber, RoomType = roomType, GuestName = guestName, Adults = adults, Children = children, CreatedAtUtc = nowUtc };
}
