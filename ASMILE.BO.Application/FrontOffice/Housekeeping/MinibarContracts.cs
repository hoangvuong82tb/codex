namespace ASMILE.BO.Application.FrontOffice.Housekeeping;

public sealed record MinibarItemSnapshot(
    string Name,
    string Unit,
    int StandardQty,
    int RemainingQty,
    int RestockQty,
    decimal UnitPrice,
    string? Category,
    string? ItemCode = null,
    decimal VatRate = 0,
    decimal VatAmount = 0);

public sealed record MinibarInventoryItemSnapshot(
    string ItemCode,
    string ItemName,
    decimal Quantity,
    decimal UnitPrice);

public sealed record MinibarRoomSnapshot(
    Guid RoomId,
    Guid PropertyId,
    string RoomNumber,
    string? RoomType,
    string? Floor,
    string? Building,
    string? GuestName,
    string? ReservationNumber,
    string? FolioNumber,
    DateTime? ArrivalDate,
    DateTime? DepartureDate,
    bool IsVip,
    string? Housekeeper,
    string? Supervisor,
    DateTime? InspectionTime,
    string Status,
    int ItemCount,
    decimal ConsumptionAmount,
    decimal RestockAmount,
    bool IsPosted,
    string? PostTarget,
    string? Remark,
    IReadOnlyList<MinibarItemSnapshot> Items,
    Guid? SessionId = null,
    Guid? StayId = null,
    Guid? FolioEntryId = null,
    decimal RestockQuantity = 0,
    DateTime? LastInventoryAtUtc = null,
    int InventoryLineCount = 0,
    IReadOnlyList<MinibarInventoryItemSnapshot>? InventoryItems = null,
    decimal VatAmount = 0);

public interface IMinibarQueryService
{
    Task<IReadOnlyList<MinibarRoomSnapshot>> QueryAsync(Guid propertyId, CancellationToken ct = default);
}

public sealed record MinibarCheckLine(
    string ItemCode,
    string ItemName,
    decimal Quantity,
    decimal UnitPrice,
    decimal VatRate = 0);

public sealed record CheckMinibarCommand(
    Guid SessionId,
    Guid PropertyId,
    Guid StayId,
    Guid RoomId,
    Guid? ActorId,
    DateTime CheckedAtUtc,
    IReadOnlyList<MinibarCheckLine> Items);

public sealed record PostMinibarCommand(
    Guid SessionId,
    Guid PropertyId,
    Guid FolioEntryId,
    Guid? ActorId,
    DateTime PostedAtUtc);

public sealed record UnpostMinibarCommand(
    Guid SessionId,
    Guid PropertyId,
    Guid? ActorId,
    DateTime OccurredAtUtc);

public sealed record RestockMinibarCommand(
    Guid TransactionId,
    Guid PropertyId,
    Guid RoomId,
    string ItemCode,
    string ItemName,
    decimal Quantity,
    string Warehouse,
    Guid ActorId,
    DateTime OccurredAtUtc,
    string? Note = null);

public sealed record MinibarInventoryLineInput(
    string ItemCode,
    string ItemName,
    decimal Quantity,
    decimal UnitPrice);

public sealed record InventoryMinibarCommand(
    Guid InventoryId,
    Guid PropertyId,
    Guid RoomId,
    Guid? StayId,
    Guid ActorId,
    DateTime CountedAtUtc,
    IReadOnlyList<MinibarInventoryLineInput> Items,
    string? Note = null);

public interface IMinibarCommandService
{
    Task CheckAsync(CheckMinibarCommand command, CancellationToken ct = default);
    Task PostAsync(PostMinibarCommand command, CancellationToken ct = default);
    Task UnpostAsync(UnpostMinibarCommand command, CancellationToken ct = default);
    Task RestockAsync(RestockMinibarCommand command, CancellationToken ct = default);
    Task InventoryAsync(InventoryMinibarCommand command, CancellationToken ct = default);
}
