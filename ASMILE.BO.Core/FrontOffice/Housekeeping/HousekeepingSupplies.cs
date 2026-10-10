namespace ASMILE.BO.Core.FrontOffice.Housekeeping;

/// <summary>Master data and transactions for real housekeeping linen operations.</summary>
public sealed class HousekeepingLinenType
{
    private HousekeepingLinenType() { }

    public int Id { get; private set; }
    public Guid PropertyId { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string Category { get; private set; } = string.Empty;
    public int DefaultParLevel { get; private set; }
    public int MaxWashCycles { get; private set; }
    public bool IsActive { get; private set; }
    public byte[]? RowVersion { get; private set; }

    public static HousekeepingLinenType Create(Guid propertyId, string code, string name, string category, int defaultParLevel, int maxWashCycles)
        => new()
        {
            PropertyId = propertyId,
            Code = Normalize(code),
            Name = Require(name, nameof(name)),
            Category = Require(category, nameof(category)),
            DefaultParLevel = RequireNonNegative(defaultParLevel, nameof(defaultParLevel)),
            MaxWashCycles = RequireNonNegative(maxWashCycles, nameof(maxWashCycles)),
            IsActive = true
        };

    public void Update(string code, string name, string category, int defaultParLevel, int maxWashCycles)
    {
        Code = Normalize(code);
        Name = Require(name, nameof(name));
        Category = Require(category, nameof(category));
        DefaultParLevel = RequireNonNegative(defaultParLevel, nameof(defaultParLevel));
        MaxWashCycles = RequireNonNegative(maxWashCycles, nameof(maxWashCycles));
    }

    public void SetActive(bool isActive) => IsActive = isActive;

    private static string Normalize(string value) => Require(value, nameof(value)).ToUpperInvariant();
    private static string Require(string value, string name) => string.IsNullOrWhiteSpace(value) ? throw new ArgumentException("Value is required.", name) : value.Trim();
    private static int RequireNonNegative(int value, string name) => value < 0 ? throw new ArgumentOutOfRangeException(name) : value;
}

/// <summary>Append-only linen movement ledger. Quantity is always positive.</summary>
public sealed class HousekeepingLinenTransaction
{
    private HousekeepingLinenTransaction() { }

    public Guid TransactionId { get; private set; }
    public Guid PropertyId { get; private set; }
    public Guid HotelRoomId { get; private set; }
    public string LinenCode { get; private set; } = string.Empty;
    public string Action { get; private set; } = string.Empty;
    public int Quantity { get; private set; }
    public DateTime OccurredAtUtc { get; private set; }
    public Guid? ActorId { get; private set; }
    public string? Note { get; private set; }

    public static HousekeepingLinenTransaction Create(Guid transactionId, Guid propertyId, Guid roomId, string linenCode, string action, int quantity, DateTime occurredAtUtc, Guid? actorId)
    {
        if (transactionId == Guid.Empty) throw new ArgumentException("Transaction id is required.", nameof(transactionId));
        if (propertyId == Guid.Empty) throw new ArgumentException("Property id is required.", nameof(propertyId));
        if (roomId == Guid.Empty) throw new ArgumentException("Room id is required.", nameof(roomId));
        if (quantity <= 0) throw new ArgumentOutOfRangeException(nameof(quantity));
        return new()
        {
            TransactionId = transactionId,
            PropertyId = propertyId,
            HotelRoomId = roomId,
            LinenCode = linenCode.Trim().ToUpperInvariant(),
            Action = action.Trim(),
            Quantity = quantity,
            OccurredAtUtc = occurredAtUtc,
            ActorId = actorId
        };
    }
}

/// <summary>Master data for room amenity replenishment.</summary>
public sealed class HousekeepingAmenityType
{
    private HousekeepingAmenityType() { }

    public int Id { get; private set; }
    public Guid PropertyId { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string Category { get; private set; } = string.Empty;
    public int DefaultParLevel { get; private set; }
    public int MinAlertThreshold { get; private set; }
    public bool IsActive { get; private set; }
    public byte[]? RowVersion { get; private set; }

    public static HousekeepingAmenityType Create(Guid propertyId, string code, string name, string category, int defaultParLevel, int minAlertThreshold)
        => new()
        {
            PropertyId = propertyId,
            Code = Normalize(code),
            Name = Require(name, nameof(name)),
            Category = Require(category, nameof(category)),
            DefaultParLevel = RequireNonNegative(defaultParLevel, nameof(defaultParLevel)),
            MinAlertThreshold = RequireNonNegative(minAlertThreshold, nameof(minAlertThreshold)),
            IsActive = true
        };

    public void Update(string code, string name, string category, int defaultParLevel, int minAlertThreshold)
    {
        Code = Normalize(code);
        Name = Require(name, nameof(name));
        Category = Require(category, nameof(category));
        DefaultParLevel = RequireNonNegative(defaultParLevel, nameof(defaultParLevel));
        MinAlertThreshold = RequireNonNegative(minAlertThreshold, nameof(minAlertThreshold));
    }

    public void SetActive(bool isActive) => IsActive = isActive;

    private static string Normalize(string value) => Require(value, nameof(value)).ToUpperInvariant();
    private static string Require(string value, string name) => string.IsNullOrWhiteSpace(value) ? throw new ArgumentException("Value is required.", name) : value.Trim();
    private static int RequireNonNegative(int value, string name) => value < 0 ? throw new ArgumentOutOfRangeException(name) : value;
}

/// <summary>Current persisted amenity balance for a room.</summary>
public sealed class HousekeepingAmenityBalance
{
    private HousekeepingAmenityBalance() { }

    public int Id { get; private set; }
    public Guid PropertyId { get; private set; }
    public Guid HotelRoomId { get; private set; }
    public string AmenityCode { get; private set; } = string.Empty;
    public int CurrentStock { get; private set; }
    public int ParLevel { get; private set; }
    public int ConsumptionLast30Days { get; private set; }
    public DateTime? LastRefillUtc { get; private set; }
    public byte[]? RowVersion { get; private set; }

    public static HousekeepingAmenityBalance Create(Guid propertyId, Guid roomId, string amenityCode, int currentStock, int parLevel, int consumptionLast30Days, DateTime? lastRefillUtc)
    {
        if (propertyId == Guid.Empty) throw new ArgumentException("Property id is required.", nameof(propertyId));
        if (roomId == Guid.Empty) throw new ArgumentException("Room id is required.", nameof(roomId));
        if (currentStock < 0 || parLevel < 0 || consumptionLast30Days < 0) throw new ArgumentOutOfRangeException(nameof(currentStock));
        return new()
        {
            PropertyId = propertyId,
            HotelRoomId = roomId,
            AmenityCode = amenityCode.Trim().ToUpperInvariant(),
            CurrentStock = currentStock,
            ParLevel = parLevel,
            ConsumptionLast30Days = consumptionLast30Days,
            LastRefillUtc = lastRefillUtc
        };
    }
}

public sealed class HousekeepingAmenityRefillRequest
{
    private HousekeepingAmenityRefillRequest() { }

    public Guid RequestId { get; private set; }
    public Guid PropertyId { get; private set; }
    public Guid HotelRoomId { get; private set; }
    public DateTime RequestedAtUtc { get; private set; }
    public string Status { get; private set; } = "Open";
    public Guid? RequestedBy { get; private set; }

    public static HousekeepingAmenityRefillRequest Create(Guid propertyId, Guid roomId, DateTime requestedAtUtc, Guid? requestedBy)
        => new()
        {
            RequestId = Guid.NewGuid(),
            PropertyId = propertyId,
            HotelRoomId = roomId,
            RequestedAtUtc = requestedAtUtc,
            RequestedBy = requestedBy,
            Status = "Open"
        };
}

public sealed class HousekeepingAmenityRefillLine
{
    private HousekeepingAmenityRefillLine() { }

    public Guid LineId { get; private set; }
    public Guid RequestId { get; private set; }
    public string AmenityCode { get; private set; } = string.Empty;
    public int Quantity { get; private set; }

    public static HousekeepingAmenityRefillLine Create(Guid requestId, string amenityCode, int quantity)
    {
        if (requestId == Guid.Empty) throw new ArgumentException("Request id is required.", nameof(requestId));
        if (quantity <= 0) throw new ArgumentOutOfRangeException(nameof(quantity));
        return new()
        {
            LineId = Guid.NewGuid(),
            RequestId = requestId,
            AmenityCode = amenityCode.Trim().ToUpperInvariant(),
            Quantity = quantity
        };
    }
}
