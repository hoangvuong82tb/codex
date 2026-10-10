using ASMILE.BO.Application.FrontOffice.Housekeeping;
using ASMILE.BO.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ASMILE.BO.Infrastructure.FrontOffice.Housekeeping;

public sealed class EfMinibarQueryService(AppDbContext db) : IMinibarQueryService
{
    public async Task<IReadOnlyList<MinibarRoomSnapshot>> QueryAsync(Guid propertyId, CancellationToken ct = default)
    {
        if (propertyId == Guid.Empty)
            return [];

        var rooms = await db.HotelRooms.AsNoTracking()
            .Where(x => x.PropertyId == propertyId && x.IsActive)
            .OrderBy(x => x.Floor).ThenBy(x => x.RoomNumber)
            .ToListAsync(ct);
        var stays = await db.HotelStays.AsNoTracking()
            .Where(x => x.PropertyId == propertyId && x.RoomId != Guid.Empty)
            .OrderByDescending(x => x.ScheduledArrivalDate)
            .ToListAsync(ct);
        var sessions = await db.MinibarSessions.AsNoTracking()
            .Include(x => x.Consumptions)
            .Where(x => x.PropertyId == propertyId)
            .OrderByDescending(x => x.CheckedAtUtc)
            .ToListAsync(ct);
        var restockTransactions = await db.MinibarRestockTransactions.AsNoTracking()
            .Where(x => x.PropertyId == propertyId)
            .ToListAsync(ct);
        var inventories = await db.MinibarInventoryAudits.AsNoTracking()
            .Include(x => x.Lines)
            .Where(x => x.PropertyId == propertyId)
            .OrderByDescending(x => x.CountedAtUtc)
            .ToListAsync(ct);
        var minibarProfiles = await db.HotelMinibarItemProfiles.AsNoTracking()
            .Where(x => x.PropertyId == propertyId && x.IsActive)
            .OrderBy(x => x.Category).ThenBy(x => x.ItemCode)
            .ToListAsync(ct);
        var minibarCatalog = minibarProfiles.Count > 0
            ? minibarProfiles.Select(x => new MinibarCatalogRow(x.ItemCode, x.ItemName, x.Unit, x.StandardQuantity, x.RetailPrice, x.Category, x.VatRate)).ToList()
            : (await db.Items.AsNoTracking().Where(x => x.IsActive && x.IsMinibarItem).OrderBy(x => x.MinibarCategory).ThenBy(x => x.ItemCode).ToListAsync(ct))
                .Select(x => new MinibarCatalogRow(x.ItemCode, x.ItemName, x.Unit, x.MinibarStandardQuantity, x.RetailPrice, x.MinibarCategory, x.VatRate)).ToList();
        var catalogByCode = minibarCatalog.ToDictionary(x => x.ItemCode, StringComparer.OrdinalIgnoreCase);
        var restocksByRoom = restockTransactions
            .GroupBy(x => x.HotelRoomId)
            .ToDictionary(
                group => group.Key,
                group => new
                {
                    Quantity = group.Sum(x => x.Quantity),
                    Amount = group.Sum(x => catalogByCode.TryGetValue(x.ItemCode, out var item)
                        ? x.Quantity * Math.Max(0m, item.RetailPrice)
                        : 0m)
                });

        var latestStays = stays.GroupBy(x => x.RoomId).ToDictionary(x => x.Key, x => x.First());
        var latestSessions = sessions.GroupBy(x => x.HotelRoomId).ToDictionary(x => x.Key, x => x.First());
        var latestInventories = inventories.GroupBy(x => x.HotelRoomId).ToDictionary(x => x.Key, x => x.First());
        // The minibar board is room-based. A room without a persisted check
        // session must still be visible as "Unchecked" so the real data path
        // does not silently hide inventory that has not been checked yet.
        var result = new List<MinibarRoomSnapshot>(rooms.Count);

        foreach (var room in rooms)
        {
            latestSessions.TryGetValue(room.HotelRoomId, out var session);
            latestStays.TryGetValue(room.HotelRoomId, out var stay);
            restocksByRoom.TryGetValue(room.HotelRoomId, out var restock);
            latestInventories.TryGetValue(room.HotelRoomId, out var inventory);
            var items = session is not null
                ? session.Consumptions.Select(x =>
                {
                    catalogByCode.TryGetValue(x.ItemCode, out var master);
                    var standard = master is null ? 0 : (int)Math.Max(0, master.StandardQuantity);
                    var unitPrice = master is not null && master.RetailPrice > 0 ? master.RetailPrice : x.UnitPrice;
                    var vatRate = master?.VatRate ?? x.VatRate;
                    var vatAmount = x.VatAmount > 0 ? x.VatAmount : Math.Round(x.LineAmount * vatRate / 100m, 2, MidpointRounding.AwayFromZero);
                    return new MinibarItemSnapshot(
                        master?.ItemName ?? x.ItemName,
                        master?.Unit ?? string.Empty,
                        standard,
                        Math.Max(0, standard - (int)x.Quantity),
                        0,
                        unitPrice,
                        master?.Category,
                        x.ItemCode,
                        vatRate,
                        vatAmount);
                }).ToArray()
                : minibarCatalog.Select(x => new MinibarItemSnapshot(
                    x.ItemName,
                    x.Unit,
                    (int)Math.Max(0, x.StandardQuantity),
                    (int)Math.Max(0, x.StandardQuantity),
                    0,
                    x.RetailPrice,
                    x.Category,
                    x.ItemCode,
                    x.VatRate,
                    0)).ToArray();
            var consumption = session?.Consumptions.Sum(x => x.LineAmount) ?? 0m;
            var vatAmount = session?.Consumptions.Sum(x => x.VatAmount) ?? 0m;
            var status = session?.Status switch
            {
                "Posted" => "Posted",
                "Checked" when consumption > 0 => "Consumption",
                "Checked" => "NoConsumption",
                _ => "Unchecked"
            };
            DateTime? arrival = stay is null
                ? null
                : stay.ScheduledArrivalDate.ToDateTime(TimeOnly.MinValue);
            DateTime? departure = stay is null
                ? null
                : stay.ScheduledDepartureDate.ToDateTime(TimeOnly.MinValue);

            result.Add(new MinibarRoomSnapshot(
                room.HotelRoomId, propertyId, room.RoomNumber, room.Description, room.Floor?.ToString(), room.Building,
                stay?.GuestNameSnapshot, stay?.ReservationNumberSnapshot, null, arrival, departure,
                stay?.IsVipService ?? false, null, null, session?.CheckedAtUtc, status, items.Length, consumption, restock?.Amount ?? 0m,
                session?.PostedAtUtc.HasValue ?? false, null, null, items,
                session?.SessionId, stay?.HotelStayId, session?.FolioEntryId, restock?.Quantity ?? 0m,
                inventory?.CountedAtUtc, inventory?.Lines.Count ?? 0,
                inventory?.Lines.Select(line => new MinibarInventoryItemSnapshot(
                    line.ItemCode, line.ItemName, line.Quantity, line.UnitPrice)).ToArray(),
                vatAmount));
        }

        return result;
    }

    private sealed record MinibarCatalogRow(string ItemCode, string ItemName, string Unit, decimal StandardQuantity, decimal RetailPrice, string Category, decimal VatRate);
}
