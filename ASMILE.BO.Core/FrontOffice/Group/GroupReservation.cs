using ASMILE.BO.Core.FrontOffice.Foundation.Identifiers;

namespace ASMILE.BO.Core.FrontOffice.Group;

public enum GroupReservationStatus { Draft, Tentative, Confirmed, CheckedIn, CheckedOut, Cancelled, Closed }
public enum ChargeRouting { CompanyPaysAll, GuestPaysExtras, SplitPercentage, SplitFixed, Custom }

public sealed class GroupReservation
{
    private readonly List<GroupRoomAssignment> _roomingList = [];
    private readonly List<GroupRoomBlock> _roomBlocks = [];
    private readonly List<GroupDepositSchedule> _depositSchedules = [];
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
    public bool DepositRequired { get; private set; }
    public bool UseMasterFolio { get; private set; }
    public bool AllowIndividualFolios { get; private set; }
    public decimal? SplitPercentage { get; private set; }
    public Guid? MasterFolioId { get; private set; }
    public string? GuaranteeType { get; private set; }
    public DateOnly? DepositCutoffDate { get; private set; }
    public DateOnly? DepositReleaseDate { get; private set; }
    public decimal TotalDeposit { get; private set; }
    public decimal TotalRevenue { get; private set; }
    public decimal OutstandingBalance { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public Guid? CreatedBy { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }
    public Guid? UpdatedBy { get; private set; }
    public byte[]? RowVersion { get; private set; }
    public IReadOnlyList<GroupRoomAssignment> RoomingList => _roomingList;
    public IReadOnlyList<GroupRoomBlock> RoomBlocks => _roomBlocks;
    public IReadOnlyList<GroupDepositSchedule> DepositSchedules => _depositSchedules;

    public static GroupReservation Create(Guid propertyId, string code, string name, DateOnly arrival, DateOnly departure, int expectedRooms, int adults, int children, DateTime nowUtc, ActorId? actorId)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("GROUP_NAME_REQUIRED");
        if (departure <= arrival) throw new ArgumentException("GROUP_INVALID_DATES");
        return new GroupReservation { GroupReservationId = Uuid7.NewGuid(), PropertyId = propertyId, GroupCode = code, GroupName = name, ArrivalDate = arrival, DepartureDate = departure, ExpectedRooms = expectedRooms, TotalAdults = adults, TotalChildren = children, Status = GroupReservationStatus.Draft, CreatedAtUtc = nowUtc, UpdatedAtUtc = nowUtc, CreatedBy = actorId?.Value, UpdatedBy = actorId?.Value };
    }

    public void UpdateDetails(
        string? companyName, string? agencyName, string? contactPerson,
        string? contactPhone, string? contactEmail, string? source,
        string? salesManager, string? marketSegment, bool isVip,
        ChargeRouting routing, string? specialRequests, string? notes,
        DateTime nowUtc, ActorId? actorId)
    {
        CompanyName = Normalize(companyName);
        AgencyName = Normalize(agencyName);
        ContactPerson = Normalize(contactPerson);
        ContactPhone = Normalize(contactPhone);
        ContactEmail = Normalize(contactEmail);
        Source = Normalize(source);
        SalesManager = Normalize(salesManager);
        MarketSegment = Normalize(marketSegment);
        IsVip = isVip;
        Routing = routing;
        SpecialRequests = Normalize(specialRequests);
        Notes = Normalize(notes);
        UpdatedAtUtc = nowUtc;
        UpdatedBy = actorId?.Value;
    }

    public void ConfigureDepositPolicy(
        bool depositRequired,
        string? guaranteeType,
        DateOnly? cutoffDate,
        DateOnly? releaseDate,
        IReadOnlyCollection<GroupDepositSchedule>? schedules,
        DateTime nowUtc,
        ActorId? actorId)
    {
        var items = schedules?.ToList() ?? [];
        if (!depositRequired && items.Count > 0)
            throw new ArgumentException("GROUP_DEPOSIT_SCHEDULE_NOT_ALLOWED");
        if (depositRequired && items.Count == 0)
            throw new ArgumentException("GROUP_DEPOSIT_SCHEDULE_REQUIRED");
        if (depositRequired && string.IsNullOrWhiteSpace(guaranteeType))
            throw new ArgumentException("GROUP_GUARANTEE_TYPE_REQUIRED");
        if (cutoffDate is not null && cutoffDate > ArrivalDate)
            throw new ArgumentException("GROUP_DEPOSIT_CUTOFF_INVALID");
        if (releaseDate is not null && releaseDate < ArrivalDate)
            throw new ArgumentException("GROUP_DEPOSIT_RELEASE_INVALID");
        if (items.Any(x => x.PropertyId != PropertyId || x.GroupReservationId != GroupReservationId))
            throw new ArgumentException("GROUP_DEPOSIT_SCHEDULE_OWNER_MISMATCH");
        if (depositRequired && items.Sum(x => x.Percent) != 100m)
            throw new ArgumentException("GROUP_DEPOSIT_SCHEDULE_PERCENT_TOTAL_INVALID");

        DepositRequired = depositRequired;
        GuaranteeType = Normalize(guaranteeType);
        DepositCutoffDate = cutoffDate;
        DepositReleaseDate = releaseDate;
        _depositSchedules.Clear();
        _depositSchedules.AddRange(items);
        Touch(nowUtc);
        UpdatedBy = actorId?.Value;
    }

    public void ConfigureBillingRouting(
        bool useMasterFolio,
        ChargeRouting routing,
        bool allowIndividualFolios,
        decimal? splitPercentage,
        Guid? masterFolioId,
        DateTime nowUtc,
        ActorId? actorId)
    {
        if (routing == ChargeRouting.SplitPercentage && splitPercentage is not (>= 0m and <= 100m))
            throw new ArgumentException("GROUP_BILLING_SPLIT_PERCENTAGE_INVALID");
        if (routing != ChargeRouting.SplitPercentage)
            splitPercentage = null;
        if (masterFolioId == Guid.Empty)
            masterFolioId = null;

        UseMasterFolio = useMasterFolio;
        Routing = routing;
        AllowIndividualFolios = allowIndividualFolios;
        SplitPercentage = splitPercentage;
        MasterFolioId = masterFolioId;
        Touch(nowUtc);
        UpdatedBy = actorId?.Value;
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
    public void MarkCheckedIn(DateTime nowUtc)
    {
        RequireStatus(GroupReservationStatus.Confirmed, GroupReservationStatus.CheckedIn);
        Status = GroupReservationStatus.CheckedIn;
        Touch(nowUtc);
    }

    public void MarkCheckedOut(DateTime nowUtc)
    {
        RequireStatus(GroupReservationStatus.CheckedIn, GroupReservationStatus.CheckedOut);
        Status = GroupReservationStatus.CheckedOut;
        Touch(nowUtc);
    }

    public void Cancel(string reason, DateTime nowUtc)
    {
        if (string.IsNullOrWhiteSpace(reason)) throw new ArgumentException("GROUP_CANCEL_REASON_REQUIRED", nameof(reason));
        RequireStatus(GroupReservationStatus.Draft, GroupReservationStatus.Tentative, GroupReservationStatus.Confirmed);
        Status = GroupReservationStatus.Cancelled;
        Notes = Normalize(string.IsNullOrWhiteSpace(Notes) ? $"Hủy: {reason}" : $"{Notes}\nHủy: {reason}");
        Touch(nowUtc);
    }
    public void RecordDeposit(decimal amount, DateTime nowUtc, ActorId? actorId)
    {
        if (amount <= 0m) throw new ArgumentException("GROUP_DEPOSIT_AMOUNT_REQUIRED", nameof(amount));
        if (Status is GroupReservationStatus.Cancelled or GroupReservationStatus.Closed or GroupReservationStatus.CheckedOut)
            throw new InvalidOperationException("GROUP_DEPOSIT_GROUP_NOT_ACTIVE");
        var normalizedAmount = decimal.Round(amount, 2, MidpointRounding.AwayFromZero);
        TotalDeposit = checked(TotalDeposit + normalizedAmount);
        Touch(nowUtc);
        UpdatedBy = actorId?.Value;
    }

    public void ReverseDeposit(decimal amount, DateTime nowUtc, ActorId? actorId)
    {
        if (amount <= 0m) throw new ArgumentException("GROUP_DEPOSIT_AMOUNT_REQUIRED", nameof(amount));
        var normalizedAmount = decimal.Round(amount, 2, MidpointRounding.AwayFromZero);
        if (normalizedAmount > TotalDeposit) throw new InvalidOperationException("GROUP_DEPOSIT_REVERSE_EXCEEDS_TOTAL");
        TotalDeposit -= normalizedAmount;
        Touch(nowUtc);
        UpdatedBy = actorId?.Value;
    }
    public void Close(DateTime nowUtc)
    {
        RequireStatus(GroupReservationStatus.CheckedOut, GroupReservationStatus.Cancelled);
        Status = GroupReservationStatus.Closed;
        Touch(nowUtc);
    }
    private void Touch(DateTime nowUtc) => UpdatedAtUtc = nowUtc;
    private void RequireStatus(params GroupReservationStatus[] allowed) { if (!allowed.Contains(Status)) throw new InvalidOperationException($"GROUP_STATUS_{Status}"); }
    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

public enum GroupDepositStatus { Posted, Reversed }

public enum GroupDepositScheduleStatus { Pending, Paid, PartiallyPaid, Overdue, Cancelled }

public sealed class GroupDeposit
{
    private GroupDeposit() { }

    public Guid GroupDepositId { get; private set; }
    public Guid PropertyId { get; private set; }
    public Guid GroupReservationId { get; private set; }
    public GroupReservation? GroupReservation { get; private set; }
    public decimal Amount { get; private set; }
    public string CurrencyCode { get; private set; } = "VND";
    public string PaymentMethod { get; private set; } = string.Empty;
    public string? ExternalReference { get; private set; }
    public DateOnly BusinessDate { get; private set; }
    public string? Note { get; private set; }
    public GroupDepositStatus Status { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public Guid? CreatedBy { get; private set; }
    public DateTime? ReversedAtUtc { get; private set; }
    public Guid? ReversedBy { get; private set; }
    public string? ReversalReason { get; private set; }
    public byte[]? RowVersion { get; private set; }

    public static GroupDeposit Create(
        Guid propertyId,
        Guid groupReservationId,
        decimal amount,
        string currencyCode,
        string paymentMethod,
        DateOnly businessDate,
        string? externalReference,
        string? note,
        DateTime nowUtc,
        ActorId? actorId)
    {
        if (propertyId == Guid.Empty) throw new ArgumentException("PROPERTY_REQUIRED", nameof(propertyId));
        if (groupReservationId == Guid.Empty) throw new ArgumentException("GROUP_REQUIRED", nameof(groupReservationId));
        if (amount <= 0m) throw new ArgumentException("GROUP_DEPOSIT_AMOUNT_REQUIRED", nameof(amount));
        if (string.IsNullOrWhiteSpace(currencyCode)) throw new ArgumentException("GROUP_DEPOSIT_CURRENCY_REQUIRED", nameof(currencyCode));
        if (string.IsNullOrWhiteSpace(paymentMethod)) throw new ArgumentException("GROUP_DEPOSIT_PAYMENT_METHOD_REQUIRED", nameof(paymentMethod));

        return new GroupDeposit
        {
            GroupDepositId = Uuid7.NewGuid(),
            PropertyId = propertyId,
            GroupReservationId = groupReservationId,
            Amount = decimal.Round(amount, 2, MidpointRounding.AwayFromZero),
            CurrencyCode = currencyCode.Trim().ToUpperInvariant(),
            PaymentMethod = paymentMethod.Trim(),
            ExternalReference = Normalize(externalReference),
            BusinessDate = businessDate,
            Note = Normalize(note),
            Status = GroupDepositStatus.Posted,
            CreatedAtUtc = nowUtc,
            CreatedBy = actorId?.Value
        };
    }

    public void Reverse(string reason, DateTime nowUtc, ActorId? actorId)
    {
        if (Status != GroupDepositStatus.Posted)
            throw new InvalidOperationException("GROUP_DEPOSIT_ALREADY_REVERSED");
        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("GROUP_DEPOSIT_REVERSAL_REASON_REQUIRED", nameof(reason));
        Status = GroupDepositStatus.Reversed;
        ReversedAtUtc = nowUtc;
        ReversedBy = actorId?.Value;
        ReversalReason = reason.Trim();
    }

    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

public sealed class GroupDepositSchedule
{
    private GroupDepositSchedule() { }

    public Guid GroupDepositScheduleId { get; private set; }
    public Guid PropertyId { get; private set; }
    public Guid GroupReservationId { get; private set; }
    public GroupReservation? GroupReservation { get; private set; }
    public DateOnly DueDate { get; private set; }
    public decimal Percent { get; private set; }
    public GroupDepositScheduleStatus Status { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public Guid? CreatedBy { get; private set; }
    public byte[]? RowVersion { get; private set; }

    public static GroupDepositSchedule Create(
        Guid propertyId,
        Guid groupReservationId,
        DateOnly dueDate,
        decimal percent,
        DateTime nowUtc,
        ActorId? actorId)
    {
        if (propertyId == Guid.Empty) throw new ArgumentException("PROPERTY_REQUIRED", nameof(propertyId));
        if (groupReservationId == Guid.Empty) throw new ArgumentException("GROUP_REQUIRED", nameof(groupReservationId));
        if (percent <= 0m || percent > 100m) throw new ArgumentException("GROUP_DEPOSIT_SCHEDULE_PERCENT_INVALID", nameof(percent));

        return new GroupDepositSchedule
        {
            GroupDepositScheduleId = Uuid7.NewGuid(),
            PropertyId = propertyId,
            GroupReservationId = groupReservationId,
            DueDate = dueDate,
            Percent = decimal.Round(percent, 2, MidpointRounding.AwayFromZero),
            Status = GroupDepositScheduleStatus.Pending,
            CreatedAtUtc = nowUtc,
            CreatedBy = actorId?.Value
        };
    }
}

public enum GroupRoomBlockStatus { Tentative, Confirmed, Released, Expired }

/// <summary>
/// A persisted room-block commitment for one group and one real room type.
/// It is deliberately separate from room assignments: a block reserves a
/// quantity for planning, while assignments later bind individual rooms.
/// </summary>
public sealed class GroupRoomBlock
{
    private GroupRoomBlock() { }

    public Guid GroupRoomBlockId { get; private set; }
    public Guid PropertyId { get; private set; }
    public Guid GroupReservationId { get; private set; }
    public GroupReservation? GroupReservation { get; private set; }
    public Guid HotelRoomTypeId { get; private set; }
    public int RequestedRooms { get; private set; }
    public int BlockedRooms { get; private set; }
    public int PickedUpRooms { get; private set; }
    public DateOnly ArrivalDate { get; private set; }
    public DateOnly DepartureDate { get; private set; }
    public DateOnly? CutoffDate { get; private set; }
    public GroupRoomBlockStatus Status { get; private set; }
    public decimal? NegotiatedRate { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public Guid? CreatedBy { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }
    public Guid? UpdatedBy { get; private set; }
    public byte[]? RowVersion { get; private set; }

    public static GroupRoomBlock Create(
        Guid propertyId,
        Guid groupReservationId,
        Guid hotelRoomTypeId,
        int rooms,
        DateOnly arrivalDate,
        DateOnly departureDate,
        DateOnly? cutoffDate,
        decimal? negotiatedRate,
        DateTime nowUtc,
        ActorId? actorId)
    {
        Validate(propertyId, groupReservationId, hotelRoomTypeId, rooms, arrivalDate, departureDate, cutoffDate, negotiatedRate);
        return new GroupRoomBlock
        {
            GroupRoomBlockId = Uuid7.NewGuid(),
            PropertyId = propertyId,
            GroupReservationId = groupReservationId,
            HotelRoomTypeId = hotelRoomTypeId,
            RequestedRooms = rooms,
            BlockedRooms = rooms,
            PickedUpRooms = 0,
            ArrivalDate = arrivalDate,
            DepartureDate = departureDate,
            CutoffDate = cutoffDate,
            Status = GroupRoomBlockStatus.Tentative,
            NegotiatedRate = negotiatedRate,
            CreatedAtUtc = nowUtc,
            UpdatedAtUtc = nowUtc,
            CreatedBy = actorId?.Value,
            UpdatedBy = actorId?.Value
        };
    }

    public void Modify(int? additionalRooms, DateOnly? newCutoffDate, DateTime nowUtc, ActorId? actorId)
    {
        if (Status is GroupRoomBlockStatus.Released or GroupRoomBlockStatus.Expired)
            throw new InvalidOperationException("GROUP_ROOM_BLOCK_NOT_MODIFIABLE");
        if (additionalRooms is < 0)
            throw new ArgumentException("GROUP_ROOM_BLOCK_ADDITIONAL_ROOMS_INVALID", nameof(additionalRooms));
        if (newCutoffDate is not null && newCutoffDate > ArrivalDate)
            throw new ArgumentException("GROUP_ROOM_BLOCK_CUTOFF_INVALID", nameof(newCutoffDate));

        var additional = additionalRooms ?? 0;
        RequestedRooms = checked(RequestedRooms + additional);
        BlockedRooms = checked(BlockedRooms + additional);
        CutoffDate = newCutoffDate ?? CutoffDate;
        Touch(nowUtc, actorId);
    }

    public void Release(int count, DateTime nowUtc, ActorId? actorId)
    {
        if (count <= 0) throw new ArgumentException("GROUP_ROOM_BLOCK_RELEASE_COUNT_INVALID", nameof(count));
        if (Status is GroupRoomBlockStatus.Released or GroupRoomBlockStatus.Expired)
            throw new InvalidOperationException("GROUP_ROOM_BLOCK_NOT_RELEASABLE");
        if (count > BlockedRooms - PickedUpRooms)
            throw new InvalidOperationException("GROUP_ROOM_BLOCK_RELEASE_EXCEEDS_AVAILABLE");

        BlockedRooms -= count;
        if (BlockedRooms == 0)
            Status = GroupRoomBlockStatus.Released;
        Touch(nowUtc, actorId);
    }

    public void Confirm(DateTime nowUtc, ActorId? actorId)
    {
        if (Status is GroupRoomBlockStatus.Released or GroupRoomBlockStatus.Expired)
            throw new InvalidOperationException("GROUP_ROOM_BLOCK_NOT_CONFIRMABLE");
        Status = GroupRoomBlockStatus.Confirmed;
        Touch(nowUtc, actorId);
    }

    public void RecordPickup(int pickedUpRooms, DateTime nowUtc, ActorId? actorId)
    {
        if (pickedUpRooms < 0 || pickedUpRooms > BlockedRooms)
            throw new ArgumentOutOfRangeException(nameof(pickedUpRooms));
        PickedUpRooms = pickedUpRooms;
        Touch(nowUtc, actorId);
    }

    private void Touch(DateTime nowUtc, ActorId? actorId)
    {
        UpdatedAtUtc = nowUtc;
        UpdatedBy = actorId?.Value;
    }

    private static void Validate(
        Guid propertyId,
        Guid groupReservationId,
        Guid hotelRoomTypeId,
        int rooms,
        DateOnly arrivalDate,
        DateOnly departureDate,
        DateOnly? cutoffDate,
        decimal? negotiatedRate)
    {
        if (propertyId == Guid.Empty) throw new ArgumentException("PROPERTY_REQUIRED", nameof(propertyId));
        if (groupReservationId == Guid.Empty) throw new ArgumentException("GROUP_REQUIRED", nameof(groupReservationId));
        if (hotelRoomTypeId == Guid.Empty) throw new ArgumentException("ROOM_TYPE_REQUIRED", nameof(hotelRoomTypeId));
        if (rooms <= 0) throw new ArgumentException("GROUP_ROOM_BLOCK_ROOMS_REQUIRED", nameof(rooms));
        if (departureDate <= arrivalDate) throw new ArgumentException("GROUP_ROOM_BLOCK_INVALID_DATES", nameof(departureDate));
        if (cutoffDate is not null && cutoffDate > arrivalDate) throw new ArgumentException("GROUP_ROOM_BLOCK_CUTOFF_INVALID", nameof(cutoffDate));
        if (negotiatedRate is < 0) throw new ArgumentException("GROUP_ROOM_BLOCK_RATE_INVALID", nameof(negotiatedRate));
    }
}

public sealed class GroupRoomAssignment
{
    private GroupRoomAssignment() { }
    public Guid GroupRoomAssignmentId { get; private set; }
    public Guid GroupReservationId { get; private set; }
    public GroupReservation? GroupReservation { get; private set; }
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

    public void MarkCheckedIn()
    {
        if (IsCheckedOut) throw new InvalidOperationException("GROUP_ROOM_ALREADY_CHECKED_OUT");
        IsCheckedIn = true;
    }

    public void MarkCheckedOut()
    {
        if (!IsCheckedIn) throw new InvalidOperationException("GROUP_ROOM_NOT_CHECKED_IN");
        IsCheckedOut = true;
    }

    public void AssignGuest(Guid guestId, string guestName)
    {
        if (guestId == Guid.Empty) throw new ArgumentException("GROUP_GUEST_REQUIRED", nameof(guestId));
        if (string.IsNullOrWhiteSpace(guestName)) throw new ArgumentException("GROUP_GUEST_NAME_REQUIRED", nameof(guestName));
        GuestId = guestId;
        GuestName = guestName.Trim();
    }

    public void UpdateGuest(Guid? guestId, string? guestName)
    {
        if (guestId == Guid.Empty) guestId = null;
        if (guestId is not null && string.IsNullOrWhiteSpace(guestName))
            throw new ArgumentException("GROUP_GUEST_NAME_REQUIRED", nameof(guestName));

        GuestId = guestId;
        GuestName = string.IsNullOrWhiteSpace(guestName) ? null : guestName.Trim();
    }

    public void UpdateOccupancy(int adults, int children)
    {
        if (adults < 0 || children < 0 || adults + children == 0)
            throw new ArgumentException("GROUP_OCCUPANCY_REQUIRED");

        Adults = adults;
        Children = children;
    }

    public void SetVip(bool isVip) => IsVip = isVip;

    public void LinkReservation(Guid reservationId)
    {
        if (reservationId == Guid.Empty) throw new ArgumentException("GROUP_RESERVATION_REQUIRED", nameof(reservationId));
        if (ReservationId is not null && ReservationId != reservationId)
            throw new InvalidOperationException("GROUP_RESERVATION_ALREADY_LINKED");
        ReservationId = reservationId;
    }
}
