namespace ASMILE.BO.Core.FrontOffice.Housekeeping;

public sealed class MinibarSession
{
    private readonly List<MinibarConsumption> _consumptions = [];
    private MinibarSession() { }

    public Guid SessionId { get; private set; }
    public Guid PropertyId { get; private set; }
    public Guid StayId { get; private set; }
    public Guid HotelRoomId { get; private set; }
    public string Status { get; private set; } = "Open";
    public Guid? CheckedBy { get; private set; }
    public DateTime? CheckedAtUtc { get; private set; }
    public DateTime? PostedAtUtc { get; private set; }
    public Guid? FolioEntryId { get; private set; }
    public byte[]? RowVersion { get; private set; }
    public IReadOnlyCollection<MinibarConsumption> Consumptions => _consumptions;

    public static MinibarSession Create(Guid propertyId, Guid stayId, Guid roomId) => new()
    {
        SessionId = Guid.NewGuid(),
        PropertyId = propertyId,
        StayId = stayId,
        HotelRoomId = roomId,
        Status = "Open"
    };

    public void Check(Guid checkedBy, DateTime checkedAtUtc)
    {
        if (Status == "Posted")
            throw new InvalidOperationException("MINIBAR_SESSION_ALREADY_POSTED");
        CheckedBy = checkedBy;
        CheckedAtUtc = checkedAtUtc;
        Status = "Checked";
    }

    public void Post(Guid folioEntryId, DateTime postedAtUtc)
    {
        if (Status != "Checked")
            throw new InvalidOperationException("MINIBAR_SESSION_NOT_CHECKED");
        if (folioEntryId == Guid.Empty)
            throw new ArgumentException("Folio entry is required.", nameof(folioEntryId));
        FolioEntryId = folioEntryId;
        PostedAtUtc = postedAtUtc;
        Status = "Posted";
    }

    public void Unpost(DateTime occurredAtUtc)
    {
        if (Status != "Posted")
            throw new InvalidOperationException("MINIBAR_SESSION_NOT_POSTED");
        FolioEntryId = null;
        PostedAtUtc = null;
        Status = "Checked";
    }
}

public sealed class MinibarConsumption
{
    private MinibarConsumption() { }

    public Guid ConsumptionId { get; private set; }
    public Guid SessionId { get; private set; }
    public Guid PropertyId { get; private set; }
    public string ItemCode { get; private set; } = string.Empty;
    public string ItemName { get; private set; } = string.Empty;
    public decimal Quantity { get; private set; }
    public decimal UnitPrice { get; private set; }
    public decimal LineAmount { get; private set; }
    public decimal VatRate { get; private set; }
    public decimal VatAmount { get; private set; }
    public decimal GrossAmount { get; private set; }

    public static MinibarConsumption Create(Guid propertyId, Guid sessionId, string itemCode, string itemName, decimal quantity, decimal unitPrice, decimal vatRate = 0)
    {
        if (vatRate is < 0 or > 100) throw new ArgumentOutOfRangeException(nameof(vatRate));
        var lineAmount = quantity * unitPrice;
        var vatAmount = Math.Round(lineAmount * vatRate / 100m, 2, MidpointRounding.AwayFromZero);
        return new()
        {
            ConsumptionId = Guid.NewGuid(), PropertyId = propertyId, SessionId = sessionId,
            ItemCode = itemCode, ItemName = itemName, Quantity = quantity, UnitPrice = unitPrice,
            LineAmount = lineAmount, VatRate = vatRate, VatAmount = vatAmount, GrossAmount = lineAmount + vatAmount
        };
    }
}

/// <summary>Append-only record of minibar replenishment performed for a room.</summary>
public sealed class MinibarRestockTransaction
{
    private MinibarRestockTransaction() { }

    public Guid TransactionId { get; private set; }
    public Guid PropertyId { get; private set; }
    public Guid HotelRoomId { get; private set; }
    public string ItemCode { get; private set; } = string.Empty;
    public string ItemName { get; private set; } = string.Empty;
    public decimal Quantity { get; private set; }
    public string Warehouse { get; private set; } = string.Empty;
    public DateTime OccurredAtUtc { get; private set; }
    public Guid ActorId { get; private set; }
    public string? Note { get; private set; }

    public static MinibarRestockTransaction Create(Guid transactionId, Guid propertyId, Guid roomId,
        string itemCode, string itemName, decimal quantity, string warehouse, DateTime occurredAtUtc,
        Guid actorId, string? note)
    {
        if (transactionId == Guid.Empty || propertyId == Guid.Empty || roomId == Guid.Empty || actorId == Guid.Empty)
            throw new ArgumentException("Transaction, property, room and actor ids are required.");
        if (string.IsNullOrWhiteSpace(itemCode) || string.IsNullOrWhiteSpace(itemName))
            throw new ArgumentException("Minibar item code and name are required.");
        if (quantity <= 0) throw new ArgumentOutOfRangeException(nameof(quantity));
        if (string.IsNullOrWhiteSpace(warehouse)) throw new ArgumentException("Warehouse is required.", nameof(warehouse));

        return new()
        {
            TransactionId = transactionId,
            PropertyId = propertyId,
            HotelRoomId = roomId,
            ItemCode = itemCode.Trim().ToUpperInvariant(),
            ItemName = itemName.Trim(),
            Quantity = quantity,
            Warehouse = warehouse.Trim(),
            OccurredAtUtc = occurredAtUtc,
            ActorId = actorId,
            Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim()
        };
    }
}

/// <summary>Persisted minibar inventory count for a room at a point in time.</summary>
public sealed class MinibarInventoryAudit
{
    private readonly List<MinibarInventoryLine> _lines = [];
    private MinibarInventoryAudit() { }

    public Guid InventoryId { get; private set; }
    public Guid PropertyId { get; private set; }
    public Guid HotelRoomId { get; private set; }
    public Guid? StayId { get; private set; }
    public DateTime CountedAtUtc { get; private set; }
    public Guid CountedBy { get; private set; }
    public string? Note { get; private set; }
    public IReadOnlyCollection<MinibarInventoryLine> Lines => _lines;

    public static MinibarInventoryAudit Create(Guid inventoryId, Guid propertyId, Guid roomId,
        Guid? stayId, DateTime countedAtUtc, Guid countedBy, string? note)
    {
        if (inventoryId == Guid.Empty || propertyId == Guid.Empty || roomId == Guid.Empty || countedBy == Guid.Empty)
            throw new ArgumentException("Inventory, property, room and actor ids are required.");

        return new()
        {
            InventoryId = inventoryId,
            PropertyId = propertyId,
            HotelRoomId = roomId,
            StayId = stayId,
            CountedAtUtc = countedAtUtc,
            CountedBy = countedBy,
            Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim()
        };
    }

    public void AddLine(string itemCode, string itemName, decimal quantity, decimal unitPrice)
    {
        if (string.IsNullOrWhiteSpace(itemCode) || string.IsNullOrWhiteSpace(itemName))
            throw new ArgumentException("Minibar inventory item code and name are required.");
        if (quantity < 0) throw new ArgumentOutOfRangeException(nameof(quantity));
        if (unitPrice < 0) throw new ArgumentOutOfRangeException(nameof(unitPrice));

        _lines.Add(MinibarInventoryLine.Create(
            Guid.NewGuid(), InventoryId, PropertyId, itemCode, itemName, quantity, unitPrice));
    }
}

public sealed class MinibarInventoryLine
{
    private MinibarInventoryLine() { }

    public Guid InventoryLineId { get; private set; }
    public Guid InventoryId { get; private set; }
    public Guid PropertyId { get; private set; }
    public string ItemCode { get; private set; } = string.Empty;
    public string ItemName { get; private set; } = string.Empty;
    public decimal Quantity { get; private set; }
    public decimal UnitPrice { get; private set; }

    public static MinibarInventoryLine Create(Guid lineId, Guid inventoryId, Guid propertyId,
        string itemCode, string itemName, decimal quantity, decimal unitPrice) => new()
    {
        InventoryLineId = lineId,
        InventoryId = inventoryId,
        PropertyId = propertyId,
        ItemCode = itemCode.Trim().ToUpperInvariant(),
        ItemName = itemName.Trim(),
        Quantity = quantity,
        UnitPrice = unitPrice
    };
}
