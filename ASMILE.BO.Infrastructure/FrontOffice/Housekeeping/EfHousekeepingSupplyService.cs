using ASMILE.BO.Application.FrontOffice.Foundation.Contexts;
using ASMILE.BO.Application.FrontOffice.Housekeeping;
using ASMILE.BO.Core.FrontOffice.Housekeeping;
using ASMILE.BO.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ASMILE.BO.Infrastructure.FrontOffice.Housekeeping;

/// <summary>
/// Real EF-backed linen and amenity operations. An empty result means that no
/// corresponding persisted rows exist; this service never creates sample data.
/// </summary>
public sealed class EfHousekeepingSupplyService(
    AppDbContext db,
    IFrontOfficeExecutionContextAccessor contextAccessor,
    IClock clock) : ILinenService, IAmenityService
{
    public async Task<IReadOnlyList<LinenType>> GetLinenTypesAsync(CancellationToken ct)
    {
        var propertyId = CurrentPropertyIdOrEmpty();
        if (propertyId == Guid.Empty) return [];

        return await db.HousekeepingLinenTypes.AsNoTracking()
            .Where(x => x.PropertyId == propertyId && x.IsActive)
            .OrderBy(x => x.Category).ThenBy(x => x.Code)
            .Select(x => new LinenType(x.Code, x.Name, x.Category, x.DefaultParLevel, x.MaxWashCycles))
            .ToListAsync(ct);
    }

    async Task<IReadOnlyList<LinenInventory>> ILinenService.GetInventoryAsync(Guid propertyId, string? floor, CancellationToken ct)
    {
        EnsureRequestedProperty(propertyId);
        var rooms = await db.HotelRooms.AsNoTracking()
            .Where(x => x.PropertyId == propertyId && x.IsActive && (string.IsNullOrWhiteSpace(floor) || x.Floor == floor))
            .Select(x => new { x.HotelRoomId, x.RoomNumber })
            .ToListAsync(ct);
        if (rooms.Count == 0) return [];

        var roomIds = rooms.Select(x => x.HotelRoomId).ToList();
        var roomNumbers = rooms.ToDictionary(x => x.HotelRoomId, x => x.RoomNumber);

        var rows = await db.HousekeepingLinenTransactions.AsNoTracking()
            .Where(x => x.PropertyId == propertyId && roomIds.Contains(x.HotelRoomId))
            .OrderBy(x => x.OccurredAtUtc)
            .ToListAsync(ct);

        return rows.GroupBy(x => new { x.HotelRoomId, x.LinenCode })
            .Select(group =>
            {
                var restocked = group.Where(x => x.Action == nameof(LinenAction.Restocked))
                    .OrderByDescending(x => x.OccurredAtUtc)
                    .Select(x => (DateTime?)x.OccurredAtUtc)
                    .FirstOrDefault();
                var quantity = group.Sum(x => MovementDelta(x.Action, x.Quantity));
                var washCycles = group.Where(x => x.Action == nameof(LinenAction.Washed)).Sum(x => x.Quantity);
                var damaged = group.Where(x => x.Action == nameof(LinenAction.Damaged)).Sum(x => x.Quantity);
                return new LinenInventory(group.Key.HotelRoomId, group.Key.LinenCode, quantity, washCycles, damaged, restocked,
                    roomNumbers.GetValueOrDefault(group.Key.HotelRoomId));
            })
            .OrderBy(x => x.RoomId).ThenBy(x => x.LinenCode)
            .ToList();
    }

    public async Task RecordLinenTransactionAsync(LinenTransaction tx, CancellationToken ct)
    {
        var propertyId = RequireCurrentProperty();
        if (tx.RoomId == Guid.Empty || tx.TransactionId == Guid.Empty || tx.Quantity <= 0)
            throw new ArgumentException("A valid room, transaction id and positive quantity are required.", nameof(tx));

        var roomExists = await db.HotelRooms.AnyAsync(x => x.HotelRoomId == tx.RoomId && x.PropertyId == propertyId && x.IsActive, ct);
        if (!roomExists) throw new InvalidOperationException("HK_ROOM_NOT_FOUND_IN_PROPERTY");

        var code = tx.LinenCode.Trim().ToUpperInvariant();
        if (!Enum.IsDefined(tx.Action))
            throw new InvalidOperationException("HK_LINEN_ACTION_INVALID");
        var typeExists = await db.HousekeepingLinenTypes.AnyAsync(x => x.PropertyId == propertyId && x.Code == code && x.IsActive, ct);
        if (!typeExists) throw new InvalidOperationException("HK_LINEN_TYPE_NOT_FOUND");

        if (await db.HousekeepingLinenTransactions.AnyAsync(x => x.TransactionId == tx.TransactionId, ct)) return;

        if (tx.Action is LinenAction.Issued or LinenAction.Damaged or LinenAction.Discarded)
        {
            var movements = await db.HousekeepingLinenTransactions.AsNoTracking()
                .Where(x => x.PropertyId == propertyId && x.HotelRoomId == tx.RoomId && x.LinenCode == code)
                .Select(x => new { x.Action, x.Quantity })
                .ToListAsync(ct);
            var available = movements.Sum(x => MovementDelta(x.Action, x.Quantity));
            if (tx.Quantity > available)
                throw new InvalidOperationException("HK_LINEN_STOCK_INSUFFICIENT");
        }

        db.HousekeepingLinenTransactions.Add(HousekeepingLinenTransaction.Create(
            tx.TransactionId,
            propertyId,
            tx.RoomId,
            code,
            tx.Action.ToString(),
            tx.Quantity,
            tx.OccurredAtUtc,
            tx.ActorId));
        await db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<AmenityType>> GetAmenityTypesAsync(CancellationToken ct)
    {
        var propertyId = CurrentPropertyIdOrEmpty();
        if (propertyId == Guid.Empty) return [];

        return await db.HousekeepingAmenityTypes.AsNoTracking()
            .Where(x => x.PropertyId == propertyId && x.IsActive)
            .OrderBy(x => x.Category).ThenBy(x => x.Code)
            .Select(x => new AmenityType(x.Code, x.Name, x.Category, x.DefaultParLevel, x.MinAlertThreshold))
            .ToListAsync(ct);
    }

    async Task<IReadOnlyList<AmenityInventory>> IAmenityService.GetInventoryAsync(Guid propertyId, string? floor, CancellationToken ct)
    {
        EnsureRequestedProperty(propertyId);
        var roomRows = await db.HotelRooms.AsNoTracking()
            .Where(x => x.PropertyId == propertyId && x.IsActive && (string.IsNullOrWhiteSpace(floor) || x.Floor == floor))
            .Select(x => new { x.HotelRoomId, x.RoomNumber })
            .ToListAsync(ct);
        if (roomRows.Count == 0) return [];
        var roomIds = roomRows.Select(x => x.HotelRoomId).ToList();
        var roomNumbers = roomRows.ToDictionary(x => x.HotelRoomId, x => x.RoomNumber);
        var balanceRows = await db.HousekeepingAmenityBalances.AsNoTracking()
            .Where(x => x.PropertyId == propertyId && roomIds.Contains(x.HotelRoomId))
            .Join(db.HousekeepingAmenityTypes.AsNoTracking().Where(x => x.PropertyId == propertyId && x.IsActive),
                balance => new { balance.PropertyId, balance.AmenityCode },
                type => new { type.PropertyId, AmenityCode = type.Code },
                (balance, type) => new
                {
                    balance.HotelRoomId,
                    balance.AmenityCode,
                    balance.CurrentStock,
                    balance.ParLevel,
                    balance.ConsumptionLast30Days,
                    balance.LastRefillUtc
                })
            .ToListAsync(ct);
        return balanceRows
            .Select(x => new AmenityInventory(x.HotelRoomId, x.AmenityCode, x.CurrentStock, x.ParLevel,
                x.ConsumptionLast30Days, x.LastRefillUtc, roomNumbers.GetValueOrDefault(x.HotelRoomId)))
            .OrderBy(x => x.RoomId).ThenBy(x => x.AmenityCode)
            .ToList();
    }

    public async Task<AmenityRefillRequest> CreateRefillRequestAsync(Guid roomId, CancellationToken ct)
    {
        var propertyId = RequireCurrentProperty();
        var roomExists = await db.HotelRooms.AnyAsync(x => x.HotelRoomId == roomId && x.PropertyId == propertyId && x.IsActive, ct);
        if (!roomExists) throw new InvalidOperationException("HK_ROOM_NOT_FOUND_IN_PROPERTY");

        var types = await db.HousekeepingAmenityTypes.AsNoTracking()
            .Where(x => x.PropertyId == propertyId && x.IsActive)
            .ToListAsync(ct);
        var balances = await db.HousekeepingAmenityBalances.AsNoTracking()
            .Where(x => x.PropertyId == propertyId && x.HotelRoomId == roomId)
            .ToListAsync(ct);
        var items = types.Join(balances,
                type => type.Code,
                balance => balance.AmenityCode,
                (type, balance) => (type.Code, Quantity: Math.Max(type.DefaultParLevel - balance.CurrentStock, 0), balance.CurrentStock, type.MinAlertThreshold))
            .Where(x => x.CurrentStock <= x.MinAlertThreshold && x.Quantity > 0)
            .Select(x => (x.Code, x.Quantity))
            .ToList();
        var requestedAt = clock.UtcNow;
        if (items.Count == 0) return new AmenityRefillRequest(roomId, [], requestedAt);

        var request = HousekeepingAmenityRefillRequest.Create(propertyId, roomId, requestedAt, contextAccessor.Current?.Actor.ActorId?.Value);
        db.HousekeepingAmenityRefillRequests.Add(request);
        db.HousekeepingAmenityRefillLines.AddRange(items.Select(item => HousekeepingAmenityRefillLine.Create(request.RequestId, item.Code, item.Quantity)));
        await db.SaveChangesAsync(ct);
        return new AmenityRefillRequest(roomId, items, requestedAt);
    }

    private Guid RequireCurrentProperty()
    {
        var propertyId = CurrentPropertyIdOrEmpty();
        if (propertyId == Guid.Empty) throw new InvalidOperationException("FO_PROPERTY_CONTEXT_REQUIRED");
        return propertyId;
    }

    private Guid CurrentPropertyIdOrEmpty() => contextAccessor.Current?.Property.PropertyId.Value ?? Guid.Empty;

    private void EnsureRequestedProperty(Guid propertyId)
    {
        if (propertyId == Guid.Empty) throw new ArgumentException("Property id is required.", nameof(propertyId));
        var current = CurrentPropertyIdOrEmpty();
        if (current != Guid.Empty && current != propertyId) throw new InvalidOperationException("FO_PROPERTY_SCOPE_MISMATCH");
    }

    private static int MovementDelta(string action, int quantity)
        => action switch
        {
            nameof(LinenAction.Issued) or nameof(LinenAction.Damaged) or nameof(LinenAction.Discarded) => -quantity,
            nameof(LinenAction.Returned) or nameof(LinenAction.Restocked) or nameof(LinenAction.Washed) => quantity,
            _ => throw new InvalidOperationException("HK_LINEN_ACTION_INVALID")
        };
}
