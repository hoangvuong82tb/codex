using ASMILE.BO.Application.DTOs;
using ASMILE.BO.Application.Interfaces;
using ASMILE.BO.Core.Entities;
using ASMILE.BO.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Text.Json;

namespace ASMILE.BO.Infrastructure.Services;

public sealed class AlacarteRestaurantService : IAlacarteRestaurantService
{
    private const string SupplementMetadataMarker = "[ALC-SUPP-META]";
    private const string ServiceWindowMetadataMarker = "[ALC-SERVICE-WINDOW]";
    private static readonly string[] SystemServingGroupCodes = ["ALL-SERVING", "STANDARD", "ODD", "WAITING"];
    private readonly AppDbContext _dbContext;
    private readonly IMenuCatalogService _menuCatalogService;
    private readonly ICashFundService? _cashFundService;
    private readonly ITableAllocationEngine _tableAllocationEngine;
    private readonly IMenuCalculationEngine _menuCalculationEngine;

    public AlacarteRestaurantService(
        AppDbContext dbContext,
        IMenuCatalogService menuCatalogService,
        ICashFundService? cashFundService = null,
        ITableAllocationEngine? tableAllocationEngine = null,
        IMenuCalculationEngine? menuCalculationEngine = null)
    {
        _dbContext = dbContext;
        _menuCatalogService = menuCatalogService;
        _cashFundService = cashFundService;
        _tableAllocationEngine = tableAllocationEngine ?? new TableAllocationEngine();
        _menuCalculationEngine = menuCalculationEngine ?? new MenuCalculationEngine();
    }

    public async Task<AlacarteDashboardDto> GetDashboardAsync(DateTime? businessDate = null, CancellationToken cancellationToken = default)
    {
        var date = (businessDate ?? DateTime.Today).Date;
        var next = date.AddDays(1);
        await ReconcileReservationStatusesAsync(cancellationToken);

        var rooms = await _dbContext.DiningRooms.AsNoTracking()
            .Include(x => x.Tables)
            .Where(x => x.IsActive)
            .OrderBy(x => x.Code)
            .ToListAsync(cancellationToken);

        var openOrders = await _dbContext.AlacarteOrders.AsNoTracking()
            .Include(x => x.Table).ThenInclude(x => x!.Room)
            .Include(x => x.Room)
            .Include(x => x.Quote)
            .Include(x => x.Reservation).ThenInclude(x => x!.Room)
            .Include(x => x.Reservation).ThenInclude(x => x!.Table)
            .Include(x => x.OrderTables)
            .Include(x => x.Lines)
            .Include(x => x.ExtraServices)
            .Include(x => x.PreparationTickets)
            .Where(x => x.Status != "Paid" && x.Status != "Cancelled" && x.Status != "Replaced")
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);

        var reservations = await _dbContext.TableReservations.AsNoTracking()
            .Include(x => x.Room)
            .Include(x => x.Table)
            .Where(x => x.ReservationDateTime >= date && x.ReservationDateTime < next)
            .OrderBy(x => x.ReservationDateTime)
            .ToListAsync(cancellationToken);

        var quotes = await _dbContext.RestaurantQuotes.AsNoTracking()
            .Include(x => x.Lines)
            .Include(x => x.QuoteTables)
            .Where(x => x.EventDate >= date && x.EventDate < next.AddDays(1))
            .ToListAsync(cancellationToken);

        var paidOrdersToday = await _dbContext.AlacarteOrders.AsNoTracking()
            .Include(x => x.Table).ThenInclude(x => x!.Room)
            .Include(x => x.Room)
            .Include(x => x.Reservation).ThenInclude(x => x!.Room)
            .Include(x => x.Lines)
            .Include(x => x.ExtraServices)
            .Where(x => x.OrderDate >= date && x.OrderDate < next && x.PaymentStatus == "Paid")
            .ToListAsync(cancellationToken);

        var depositsToday = await _dbContext.RestaurantDeposits.AsNoTracking()
            .Where(x => x.Date >= date && x.Date < next && x.Status == "Confirmed")
            .ToListAsync(cancellationToken);

        var quoteLinks = await _dbContext.RestaurantQuotes.AsNoTracking()
            .Where(x => x.TableReservationId.HasValue && reservations.Select(r => r.Id).Contains(x.TableReservationId.Value))
            .Select(x => new DashboardReservationQuoteLink(x.TableReservationId!.Value, x.Id, x.QuoteNo, x.ApprovedOrderId))
            .ToListAsync(cancellationToken);

        var roomStatuses = BuildRoomStatuses(rooms, openOrders, paidOrdersToday);
        var servingBoard = BuildServingBoard(openOrders);
        var timeline = BuildTimeline(reservations, quoteLinks);
        var revenueChart = BuildRevenueChart(paidOrdersToday);
        var quickReports = BuildQuickReports(date, openOrders, paidOrdersToday, quotes);
        var alerts = BuildAlerts(reservations, openOrders, quotes, depositsToday, quoteLinks);
        var activities = BuildActivities(reservations, openOrders, depositsToday, quoteLinks);

        var activeRoomIds = openOrders
            .SelectMany(GetOrderRoomIds)
            .Distinct()
            .ToList();
        var servingTableIds = openOrders
            .SelectMany(GetServingTableIds)
            .Distinct()
            .ToList();
        var currentGuests = openOrders
            .Where(IsServingOrder)
            .Sum(x => ResolveGuestCount(x));

        return new AlacarteDashboardDto
        {
            Kpis = new AlacarteDashboardKpisDto
            {
                ActiveRooms = activeRoomIds.Count,
                ServingTables = servingTableIds.Count,
                CurrentGuests = currentGuests,
                TodayRevenue = paidOrdersToday.Sum(x => x.TotalAmount),
                TodayDeposits = depositsToday.Sum(x => x.Amount),
                TodayReservations = reservations.Count(x => !string.Equals(x.Status, "Cancelled", StringComparison.OrdinalIgnoreCase)),
                OpenOrders = openOrders.Count,
                PendingQuotes = quotes.Count(x => !x.ApprovedOrderId.HasValue),
                ExpectedGuests = reservations.Count == 0
                    ? openOrders.Sum(ResolveGuestCount)
                    : reservations.Where(x => x.Status is "Confirmed" or "CheckedIn" or "Draft").Sum(x => x.GuestCount)
            },
            RoomStatuses = roomStatuses,
            ServingBoard = servingBoard,
            TodayTimeline = timeline,
            RevenueChart = revenueChart,
            QuickReports = quickReports,
            Alerts = alerts,
            RecentActivities = activities
        };
    }

    public async Task<IReadOnlyList<DiningRoomDto>> GetRoomsAsync(CancellationToken cancellationToken = default)
    {
        var rooms = await _dbContext.DiningRooms.AsNoTracking()
            .Include(x => x.Tables)
            .OrderBy(x => x.Code)
            .ToListAsync(cancellationToken);
        var activeRoomIds = await _dbContext.AlacarteOrders.AsNoTracking()
            .Where(x => x.Status != "Paid" && x.Status != "Cancelled" && x.DiningRoomId.HasValue)
            .Select(x => x.DiningRoomId!.Value)
            .Distinct()
            .ToListAsync(cancellationToken);
        var servingRoomIds = await _dbContext.RestaurantOrderTables.AsNoTracking()
            .Where(x => !x.IsVirtual
                        && x.Status == "Serving"
                        && x.Order != null
                        && x.Order.DiningRoomId.HasValue
                        && x.Order.Status != "Paid"
                        && x.Order.Status != "Cancelled")
            .Select(x => x.Order!.DiningRoomId!.Value)
            .Distinct()
            .ToListAsync(cancellationToken);

        var activeRoomSet = activeRoomIds.ToHashSet();
        var servingRoomSet = servingRoomIds.ToHashSet();
        return rooms.Select(x =>
        {
            var dto = ToDto(x);
            dto.Status = servingRoomSet.Contains(x.Id)
                ? "Occupied"
                : activeRoomSet.Contains(x.Id) ? "Reserved" : "Available";
            return dto;
        }).ToList();
    }

    public async Task<DiningRoomDto> SaveRoomAsync(SaveDiningRoomRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Code)) throw new InvalidOperationException("MÃ£ phÃ²ng Äƒn khÃ´ng Ä‘Æ°á»£c Ä‘á»ƒ trá»‘ng.");
        if (string.IsNullOrWhiteSpace(request.Name)) throw new InvalidOperationException("TÃªn phÃ²ng Äƒn khÃ´ng Ä‘Æ°á»£c Ä‘á»ƒ trá»‘ng.");
        if (request.Capacity <= 0) throw new InvalidOperationException("Sá»©c chá»©a pháº£i lá»›n hÆ¡n 0.");
        var code = request.Code.Trim();
        var exists = await _dbContext.DiningRooms.AnyAsync(x => x.Code == code && (!request.Id.HasValue || x.Id != request.Id.Value), cancellationToken);
        if (exists) throw new InvalidOperationException("MÃ£ phÃ²ng Äƒn Ä‘Ã£ tá»“n táº¡i.");

        var now = DateTime.UtcNow;
        var room = request.Id.HasValue
            ? await _dbContext.DiningRooms.FirstOrDefaultAsync(x => x.Id == request.Id.Value, cancellationToken) ?? throw new InvalidOperationException("KhÃ´ng tÃ¬m tháº¥y phÃ²ng Äƒn.")
            : new DiningRoom { Id = Guid.NewGuid(), CreatedAt = now };
        room.Code = code;
        room.Name = request.Name.Trim();
        room.RoomType = Normalize(request.RoomType, "Standard");
        room.Capacity = request.Capacity;
        room.Description = TrimOrNull(request.Description);
        room.Status = Normalize(request.Status, "Available");
        room.IsActive = request.IsActive;
        room.UpdatedAt = now;
        if (!request.Id.HasValue) _dbContext.DiningRooms.Add(room);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return ToDto(room);
    }

    public async Task DeleteRoomAsync(Guid roomId, CancellationToken cancellationToken = default)
    {
        var room = await _dbContext.DiningRooms
            .Include(x => x.Tables)
            .FirstOrDefaultAsync(x => x.Id == roomId, cancellationToken)
            ?? throw new InvalidOperationException("KhÃ´ng tÃ¬m tháº¥y phÃ²ng Äƒn.");

        var hasTables = room.Tables.Any(x => x.IsActive);
        if (hasTables)
        {
            throw new InvalidOperationException("PhÃ²ng Äƒn váº«n cÃ²n bÃ n, vui lÃ²ng xÃ³a hoáº·c chuyá»ƒn bÃ n trÆ°á»›c.");
        }

        var hasReservations = await _dbContext.TableReservations.AnyAsync(x => x.DiningRoomId == roomId, cancellationToken);
        if (hasReservations)
        {
            throw new InvalidOperationException("PhÃ²ng Äƒn Ä‘Ã£ phÃ¡t sinh Ä‘áº·t bÃ n, khÃ´ng thá»ƒ xÃ³a.");
        }

        var hasOrders = await _dbContext.AlacarteOrders.AnyAsync(x => x.DiningRoomId == roomId, cancellationToken);
        if (hasOrders)
        {
            throw new InvalidOperationException("PhÃ²ng Äƒn Ä‘Ã£ phÃ¡t sinh Ä‘Æ¡n hÃ ng, khÃ´ng thá»ƒ xÃ³a.");
        }

        _dbContext.DiningRooms.Remove(room);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<DiningTableDto>> GetTablesAsync(CancellationToken cancellationToken = default)
        => await _dbContext.DiningTables.AsNoTracking()
            .Include(x => x.Room)
            .OrderBy(x => x.Room!.Code).ThenBy(x => x.SortOrder).ThenBy(x => x.Code)
            .Select(x => ToDto(x))
            .ToListAsync(cancellationToken);

    public async Task<DiningTableDto> SaveTableAsync(SaveDiningTableRequest request, CancellationToken cancellationToken = default)
    {
        if (!await _dbContext.DiningRooms.AnyAsync(x => x.Id == request.DiningRoomId, cancellationToken)) throw new InvalidOperationException("KhÃ´ng tÃ¬m tháº¥y phÃ²ng Äƒn.");
        if (request.SeatCount <= 0) throw new InvalidOperationException("Sá»‘ gháº¿ pháº£i lá»›n hÆ¡n 0.");
        var code = request.Code.Trim();
        if (string.IsNullOrWhiteSpace(code)) throw new InvalidOperationException("MÃ£ bÃ n khÃ´ng Ä‘Æ°á»£c Ä‘á»ƒ trá»‘ng.");
        var exists = await _dbContext.DiningTables.AnyAsync(x => x.Code == code && (!request.Id.HasValue || x.Id != request.Id.Value), cancellationToken);
        if (exists) throw new InvalidOperationException("MÃ£ bÃ n Äƒn Ä‘Ã£ tá»“n táº¡i.");

        var now = DateTime.UtcNow;
        var table = request.Id.HasValue
            ? await _dbContext.DiningTables.FirstOrDefaultAsync(x => x.Id == request.Id.Value, cancellationToken) ?? throw new InvalidOperationException("KhÃ´ng tÃ¬m tháº¥y bÃ n Äƒn.")
            : new DiningTable { Id = Guid.NewGuid(), CreatedAt = now };
        table.DiningRoomId = request.DiningRoomId;
        table.Code = code;
        table.Name = request.Name.Trim();
        table.SeatCount = request.SeatCount;
        table.Status = Normalize(request.Status, "Empty");
        table.Note = TrimOrNull(request.Note);
        table.IsActive = request.IsActive;
        table.SortOrder = request.SortOrder;
        table.UpdatedAt = now;
        if (!request.Id.HasValue) _dbContext.DiningTables.Add(table);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return ToDto(await _dbContext.DiningTables.AsNoTracking().Include(x => x.Room).FirstAsync(x => x.Id == table.Id, cancellationToken));
    }

    public async Task DeleteTableAsync(Guid tableId, CancellationToken cancellationToken = default)
    {
        var table = await _dbContext.DiningTables.FirstOrDefaultAsync(x => x.Id == tableId, cancellationToken)
            ?? throw new InvalidOperationException("KhÃ´ng tÃ¬m tháº¥y bÃ n Äƒn.");

        var hasReservations = await _dbContext.TableReservations.AnyAsync(x => x.DiningTableId == tableId, cancellationToken);
        if (hasReservations)
        {
            throw new InvalidOperationException("BÃ n Äƒn Ä‘Ã£ phÃ¡t sinh Ä‘áº·t bÃ n, khÃ´ng thá»ƒ xÃ³a.");
        }

        var hasOrders = await _dbContext.AlacarteOrders.AnyAsync(x => x.DiningTableId == tableId, cancellationToken);
        if (hasOrders)
        {
            throw new InvalidOperationException("BÃ n Äƒn Ä‘Ã£ phÃ¡t sinh Ä‘Æ¡n hÃ ng, khÃ´ng thá»ƒ xÃ³a.");
        }

        _dbContext.DiningTables.Remove(table);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<string>> ValidateReservationAsync(SaveTableReservationRequest request, CancellationToken cancellationToken = default)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(request.CustomerName)) errors.Add("TÃªn khÃ¡ch hÃ ng khÃ´ng Ä‘Æ°á»£c Ä‘á»ƒ trá»‘ng.");
        if (string.IsNullOrWhiteSpace(request.Phone)) errors.Add("Sá»‘ Ä‘iá»‡n thoáº¡i khÃ´ng Ä‘Æ°á»£c Ä‘á»ƒ trá»‘ng.");
        if (request.GuestCount <= 0) errors.Add("Sá»‘ khÃ¡ch pháº£i lá»›n hÆ¡n 0.");
        if (request.ReservationDateTime == default) errors.Add("Thiáº¿u ngÃ y giá» Ä‘áº·t bÃ n.");

        DiningRoom? room = null;
        if (request.DiningRoomId.HasValue)
        {
            room = await _dbContext.DiningRooms.AsNoTracking().FirstOrDefaultAsync(x => x.Id == request.DiningRoomId.Value, cancellationToken);
            if (room is null) errors.Add("KhÃ´ng tÃ¬m tháº¥y phÃ²ng Äƒn.");
            else if (request.GuestCount > room.Capacity) errors.Add($"Sá»‘ khÃ¡ch vÆ°á»£t sá»©c chá»©a phÃ²ng {room.Name} ({room.Capacity} khÃ¡ch).");
        }

        DiningTable? table = null;
        if (request.DiningTableId.HasValue)
        {
            table = await _dbContext.DiningTables.AsNoTracking().Include(x => x.Room).FirstOrDefaultAsync(x => x.Id == request.DiningTableId.Value, cancellationToken);
            if (table is null)
            {
                errors.Add("KhÃ´ng tÃ¬m tháº¥y bÃ n Äƒn.");
            }
            else
            {
                if (table.Status == "Locked") errors.Add($"B\u00e0n {table.Name} \u0111ang kh\u00f3a, kh\u00f4ng th\u1ec3 \u0111\u1eb7t b\u00e0n.");
                if (table.Status == "Serving") errors.Add($"B\u00e0n {table.Name} \u0111ang ph\u1ee5c v\u1ee5, kh\u00f4ng th\u1ec3 \u0111\u1eb7t b\u00e0n.");
                if (request.GuestCount > table.SeatCount) errors.Add($"S\u1ed1 kh\u00e1ch v\u01b0\u1ee3t s\u1ed1 gh\u1ebf b\u00e0n {table.Name} ({table.SeatCount} gh\u1ebf).");
            }
        }

        if (request.DiningTableId.HasValue && request.ReservationDateTime != default)
        {
            var from = request.ReservationDateTime.AddHours(-2);
            var to = request.ReservationDateTime.AddHours(2);
            var conflict = await _dbContext.TableReservations.AsNoTracking()
                .Include(x => x.Table)
                .Where(x => x.DiningTableId == request.DiningTableId.Value
                            && (!request.Id.HasValue || x.Id != request.Id.Value)
                            && x.ReservationDateTime >= from
                            && x.ReservationDateTime <= to
                            && x.Status != "Cancelled"
                            && x.Status != "Completed"
                            && x.Status != "NoShow")
                .OrderBy(x => x.ReservationDateTime)
                .FirstOrDefaultAsync(cancellationToken);
            if (conflict is not null)
            {
                errors.Add($"B\u00e0n {conflict.Table?.Name ?? table?.Name ?? string.Empty} \u0111\u00e3 c\u00f3 \u0111\u1eb7t b\u00e0n l\u00fac {conflict.ReservationDateTime:dd/MM/yyyy HH:mm}.");
            }
        }

        return errors;
    }

    public async Task<TableReservationDto> SaveReservationAsync(SaveTableReservationRequest request, CancellationToken cancellationToken = default)
    {
        var validationErrors = await ValidateReservationAsync(request, cancellationToken);
        if (validationErrors.Count > 0) throw new InvalidOperationException(string.Join(Environment.NewLine, validationErrors));
        if (request.ReservationDateTime == default) throw new InvalidOperationException("Thiáº¿u ngÃ y giá» Ä‘áº·t bÃ n.");
        if (request.GuestCount <= 0) throw new InvalidOperationException("Sá»‘ khÃ¡ch pháº£i lá»›n hÆ¡n 0.");
        var now = DateTime.UtcNow;
        var reservation = request.Id.HasValue
            ? await _dbContext.TableReservations.FirstOrDefaultAsync(x => x.Id == request.Id.Value, cancellationToken) ?? throw new InvalidOperationException("KhÃ´ng tÃ¬m tháº¥y Ä‘áº·t bÃ n.")
            : new TableReservation { Id = Guid.NewGuid(), ReservationNo = await GenerateReservationNoAsync(request.ReservationDateTime, cancellationToken), CreatedAt = now };
        reservation.CustomerId = request.CustomerId;
        reservation.CustomerName = TrimOrNull(request.CustomerName);
        reservation.Phone = request.Phone.Trim();
        reservation.ReservationDateTime = request.ReservationDateTime;
        reservation.GuestCount = request.GuestCount;
        reservation.DiningRoomId = request.DiningRoomId;
        reservation.DiningTableId = request.DiningTableId;
        reservation.Status = Normalize(request.Status, "Draft");
        reservation.Note = TrimOrNull(request.Note);
        reservation.UpdatedAt = now;
        if (!request.Id.HasValue) _dbContext.TableReservations.Add(reservation);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return await LoadReservationDtoAsync(reservation.Id, cancellationToken);
    }

    public async Task<IReadOnlyList<TableReservationDto>> GetReservationsAsync(DateTime? date = null, CancellationToken cancellationToken = default)
    {
        await ReconcileReservationStatusesAsync(cancellationToken);

        var query = _dbContext.TableReservations.AsNoTracking().Include(x => x.Room).Include(x => x.Table).AsQueryable();
        if (date.HasValue)
        {
            var from = date.Value.Date;
            var to = from.AddDays(1);
            query = query.Where(x => x.ReservationDateTime >= from && x.ReservationDateTime < to);
        }

        var reservations = await query.OrderBy(x => x.ReservationDateTime).Select(x => ToDto(x)).ToListAsync(cancellationToken);
        if (reservations.Count == 0)
        {
            return reservations;
        }

        var reservationIds = reservations.Select(x => x.Id).ToList();
        var quoteLinks = await _dbContext.RestaurantQuotes.AsNoTracking()
            .Where(x => x.TableReservationId.HasValue && reservationIds.Contains(x.TableReservationId.Value))
            .Select(x => new
            {
                ReservationId = x.TableReservationId!.Value,
                QuoteId = (Guid?)x.Id,
                x.QuoteNo,
                x.ApprovedOrderId
            })
            .ToListAsync(cancellationToken);
        var orderLinks = await _dbContext.AlacarteOrders.AsNoTracking()
            .Where(x => x.ReservationId.HasValue && reservationIds.Contains(x.ReservationId.Value))
            .Select(x => new
            {
                ReservationId = x.ReservationId!.Value,
                x.RestaurantQuoteId,
                ApprovedOrderId = (Guid?)x.Id
            })
            .ToListAsync(cancellationToken);

        foreach (var reservation in reservations)
        {
            var quoteLink = quoteLinks.FirstOrDefault(x => x.ReservationId == reservation.Id);
            var orderLink = orderLinks.FirstOrDefault(x => x.ReservationId == reservation.Id);
            reservation.QuoteId = quoteLink?.QuoteId ?? orderLink?.RestaurantQuoteId;
            reservation.QuoteNo = quoteLink?.QuoteNo;
            reservation.ApprovedOrderId = quoteLink?.ApprovedOrderId ?? orderLink?.ApprovedOrderId;
        }

        return reservations;
    }

    public async Task<AlacarteOrderDto> OpenOrderAsync(CreateAlacarteOrderRequest request, CancellationToken cancellationToken = default)
    {
        var table = request.DiningTableId.HasValue
            ? await _dbContext.DiningTables.Include(x => x.Room).FirstOrDefaultAsync(x => x.Id == request.DiningTableId.Value, cancellationToken)
            : null;
        var room = request.DiningRoomId.HasValue
            ? await _dbContext.DiningRooms.FirstOrDefaultAsync(x => x.Id == request.DiningRoomId.Value, cancellationToken)
            : table?.Room;
        if (request.DiningTableId.HasValue && table is null) throw new InvalidOperationException("KhÃ´ng tÃ¬m tháº¥y bÃ n Äƒn.");
        if (table?.Status == "Locked") throw new InvalidOperationException("B\u00e0n \u0111ang kh\u00f3a, kh\u00f4ng th\u1ec3 m\u1edf \u0111\u01a1n.");
        if (request.DiningTableId.HasValue)
        {
            var current = await _dbContext.AlacarteOrders
                .FirstOrDefaultAsync(x => x.DiningTableId == request.DiningTableId && x.Status != "Paid" && x.Status != "Cancelled", cancellationToken);
            if (current is not null) return await LoadOrderDtoAsync(current.Id, cancellationToken);
        }

        var now = DateTime.UtcNow;
        var order = new AlacarteOrder
        {
            Id = Guid.NewGuid(),
            DiningTableId = request.DiningTableId,
            DiningRoomId = request.DiningRoomId ?? table?.DiningRoomId,
            CustomerId = request.CustomerId,
            ReservationId = request.ReservationId,
            OrderNo = await GenerateOrderNoAsync(DateTime.Today, cancellationToken),
            OrderDate = DateTime.Now,
            ActualGuestCount = Math.Max(0, request.GuestCount),
            Note = TrimOrNull(request.Note),
            CreatedBy = request.CreatedBy?.Trim() ?? "system",
            CreatedAt = now,
            UpdatedAt = now
        };
        _dbContext.AlacarteOrders.Add(order);
        if (table is not null)
        {
            table.Status = "Serving";
            table.Room!.Status = "Occupied";
            table.UpdatedAt = now;
            table.Room.UpdatedAt = now;
        }
        else if (room is not null)
        {
            room.Status = "Occupied";
            room.UpdatedAt = now;
        }
        await _dbContext.SaveChangesAsync(cancellationToken);
        return await LoadOrderDtoAsync(order.Id, cancellationToken);
    }

    public async Task<AlacarteOrderDto> OpenRoomWalkInOrderAsync(CreateRoomWalkInOrderRequest request, CancellationToken cancellationToken = default)
    {
        if (request.DiningRoomId == Guid.Empty) throw new InvalidOperationException("Thiáº¿u phÃ²ng Äƒn.");

        var room = await _dbContext.DiningRooms.FirstOrDefaultAsync(x => x.Id == request.DiningRoomId, cancellationToken)
            ?? throw new InvalidOperationException("KhÃ´ng tÃ¬m tháº¥y phÃ²ng Äƒn.");
        await EnsureRoomPhysicalTablesAsync(room, cancellationToken);

        var existing = await _dbContext.AlacarteOrders.AsNoTracking()
            .FirstOrDefaultAsync(x => x.DiningRoomId == request.DiningRoomId && x.Status != "Paid" && x.Status != "Cancelled", cancellationToken);
        if (existing is not null)
        {
            if (request.GuestCount > 0 || request.TableCountToOpen > 0 || !string.IsNullOrWhiteSpace(request.CustomerName) || !string.IsNullOrWhiteSpace(request.Phone))
            {
                return await ConfigureRoomWalkInOrderAsync(new ConfigureRoomWalkInOrderRequest
                {
                    OrderId = existing.Id,
                    GuestCount = request.GuestCount,
                    TableCountToOpen = request.TableCountToOpen,
                    StartTime = request.StartTime,
                    EndTime = request.EndTime,
                    CustomerName = request.CustomerName,
                    Phone = request.Phone,
                    Note = request.Note,
                    UpdatedBy = request.CreatedBy
                }, cancellationToken);
            }
            return await LoadOrderDtoAsync(existing.Id, cancellationToken);
        }
        var now = DateTime.UtcNow;
        var tables = await EnsureRoomPhysicalTablesAsync(room, cancellationToken);

        var order = new AlacarteOrder
        {
            Id = Guid.NewGuid(),
            DiningRoomId = room.Id,
            OrderNo = await GenerateOrderNoAsync(DateTime.Today, cancellationToken),
            OrderDate = DateTime.Now,
            OrderType = "WalkIn",
            ActualGuestCount = 0,
            Status = "Draft",
            PaymentStatus = "Unpaid",
            Note = TrimOrNull(request.Note),
            CreatedBy = string.IsNullOrWhiteSpace(request.CreatedBy) ? "system" : request.CreatedBy.Trim(),
            CreatedAt = now,
            UpdatedAt = now
        };
        _dbContext.AlacarteOrders.Add(order);

        var summaryTable = new RestaurantOrderTable
        {
            Id = Guid.NewGuid(),
            AlacarteOrderId = order.Id,
            TableNo = 0,
            DisplayName = "Ban Tong",
            GuestCount = 0,
            SortOrder = 0,
            IsVirtual = true,
            Status = "Draft",
            Note = "Tong hop thanh toan",
            CreatedAt = now,
            UpdatedAt = now
        };
        order.OrderTables.Add(summaryTable);

        for (var index = 0; index < tables.Count; index++)
        {
            var table = tables[index];
            var tableNo = index + 1;
            var orderTable = new RestaurantOrderTable
            {
                Id = Guid.NewGuid(),
                AlacarteOrderId = order.Id,
                DiningTableId = table.Id,
                TableNo = tableNo,
                DisplayName = Normalize(table.Name, $"Ban {tableNo:00}"),
                GuestCount = 0,
                SortOrder = tableNo,
                IsVirtual = false,
                Status = "Waiting",
                Note = table.Code,
                CreatedAt = now,
                UpdatedAt = now
            };
            order.OrderTables.Add(orderTable);
        }

        room.Status = await RoomHasActiveTablesAsync(room.Id, cancellationToken) ? "Occupied" : "Available";
        room.UpdatedAt = now;
        await _dbContext.SaveChangesAsync(cancellationToken);

        if (request.GuestCount > 0 || request.TableCountToOpen > 0 || !string.IsNullOrWhiteSpace(request.CustomerName) || !string.IsNullOrWhiteSpace(request.Phone))
        {
            return await ConfigureRoomWalkInOrderAsync(new ConfigureRoomWalkInOrderRequest
            {
                OrderId = order.Id,
                GuestCount = request.GuestCount,
                TableCountToOpen = request.TableCountToOpen,
                StartTime = request.StartTime,
                EndTime = request.EndTime,
                CustomerName = request.CustomerName,
                Phone = request.Phone,
                Note = request.Note,
                UpdatedBy = request.CreatedBy
            }, cancellationToken);
        }

        return await LoadOrderDtoAsync(order.Id, cancellationToken);
    }

    public async Task<AlacarteOrderDto> ConfigureRoomWalkInOrderAsync(ConfigureRoomWalkInOrderRequest request, CancellationToken cancellationToken = default)
    {
        if (request.GuestCount <= 0) throw new InvalidOperationException("Sá»‘ khÃ¡ch pháº£i lá»›n hÆ¡n 0.");
        var normalizedStartTime = Normalize(string.IsNullOrWhiteSpace(request.StartTime) ? null : request.StartTime, DateTime.Now.ToString("HH:mm"));
        if (!TimeSpan.TryParse(normalizedStartTime, out var startTime))
        {
            throw new InvalidOperationException("Giờ bắt đầu chưa đúng định dạng HH:mm.");
        }

        var defaultEndTime = TimeSpan.FromMinutes(Math.Min(startTime.TotalMinutes + 120, (24 * 60) - 1));
        var normalizedEndTime = Normalize(
            string.IsNullOrWhiteSpace(request.EndTime) ? null : request.EndTime,
            defaultEndTime.ToString(@"hh\:mm"));

        if (!TimeSpan.TryParse(normalizedEndTime, out var endTime))
        {
            throw new InvalidOperationException("Giờ kết thúc chưa đúng định dạng HH:mm.");
        }

        if (startTime >= endTime)
        {
            throw new InvalidOperationException("Giờ kết thúc phải lớn hơn giờ bắt đầu.");
        }

        var order = await LoadOrderEntityAsync(request.OrderId, cancellationToken);
        EnsureOrderEditable(order);
        if (!string.Equals(order.OrderType, "WalkIn", StringComparison.OrdinalIgnoreCase))
        {
            var canUpgradeToWalkIn = !order.RestaurantQuoteId.HasValue
                                     && order.DiningRoomId.HasValue
                                     && (string.IsNullOrWhiteSpace(order.OrderType)
                                         || string.Equals(order.OrderType, "Basic", StringComparison.OrdinalIgnoreCase)
                                         || string.Equals(order.OrderType, "Draft", StringComparison.OrdinalIgnoreCase));
            if (canUpgradeToWalkIn)
            {
                order.OrderType = "WalkIn";
            }
        }
        if (!string.Equals(order.OrderType, "WalkIn", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("ÄÆ¡n hiá»‡n táº¡i khÃ´ng pháº£i luá»“ng má»Ÿ phÃ²ng Äƒn ngay.");
        }

        var roomId = order.DiningRoomId ?? order.Reservation?.DiningRoomId
            ?? throw new InvalidOperationException("ÄÆ¡n chÆ°a gáº¯n phÃ²ng.");
        var room = await _dbContext.DiningRooms.FirstOrDefaultAsync(x => x.Id == roomId, cancellationToken)
            ?? throw new InvalidOperationException("KhÃ´ng tÃ¬m tháº¥y phÃ²ng Äƒn.");
        var roomTables = await EnsureRoomPhysicalTablesAsync(room, cancellationToken);
        var physicalOrderTables = order.OrderTables.Where(x => !x.IsVirtual).OrderBy(x => x.SortOrder).ToList();
        if (physicalOrderTables.Count == 0)
        {
            for (var index = 0; index < roomTables.Count; index++)
            {
                var table = roomTables[index];
                var tableNo = index + 1;
                var orderTable = new RestaurantOrderTable
                {
                    Id = Guid.NewGuid(),
                    AlacarteOrderId = order.Id,
                    DiningTableId = table.Id,
                    TableNo = tableNo,
                    DisplayName = Normalize(table.Name, $"Ban {tableNo:00}"),
                    GuestCount = 0,
                    SortOrder = tableNo,
                    IsVirtual = false,
                    Status = "Waiting",
                    Note = table.Code,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                order.OrderTables.Add(orderTable);
                physicalOrderTables.Add(orderTable);
            }
        }

        var capacityPerTable = Math.Max(1, roomTables.Where(x => x.SeatCount > 0).Select(x => x.SeatCount).DefaultIfEmpty(10).Max());
        var suggestedAllocations = _tableAllocationEngine.Allocate(request.GuestCount, capacityPerTable)
            .Where(x => !x.IsVirtual)
            .Select(x => Math.Max(1, x.GuestCount))
            .ToList();
        var suggestedCount = Math.Max(1, suggestedAllocations.Count);
        var tableCountToOpen = Math.Max(1, request.TableCountToOpen <= 0 ? suggestedCount : request.TableCountToOpen);
        tableCountToOpen = Math.Min(tableCountToOpen, physicalOrderTables.Count);

        var allocations = request.TableCountToOpen <= 0 && tableCountToOpen == suggestedCount
            ? suggestedAllocations
            : BuildServingGuestAllocations(request.GuestCount, tableCountToOpen, capacityPerTable);
        var now = DateTime.UtcNow;

        var tableEntities = await _dbContext.DiningTables
            .Where(x => x.DiningRoomId == room.Id)
            .ToDictionaryAsync(x => x.Id, cancellationToken);

        for (var index = 0; index < physicalOrderTables.Count; index++)
        {
            var orderTable = physicalOrderTables[index];
            if (index < allocations.Count)
            {
                orderTable.GuestCount = allocations[index];
                orderTable.Status = "Serving";
            }
            else
            {
                orderTable.GuestCount = 0;
                orderTable.Status = "Waiting";
            }

            orderTable.UpdatedAt = now;
            if (orderTable.DiningTableId.HasValue && tableEntities.TryGetValue(orderTable.DiningTableId.Value, out var diningTable))
            {
                diningTable.Status = string.Equals(orderTable.Status, "Serving", StringComparison.OrdinalIgnoreCase) ? "Serving" : "Empty";
                diningTable.UpdatedAt = now;
            }
        }

        // Auto-copy items to newly activated tables (tables that were Waiting → now Serving, with no items)
        var sourceTable = physicalOrderTables.FirstOrDefault(x =>
            string.Equals(x.Status, "Serving", StringComparison.OrdinalIgnoreCase)
            && order.Lines.Any(l => l.RestaurantOrderTableId == x.Id && l.LineStatus != "Cancelled"));
        var newlyActivated = physicalOrderTables.Where(x =>
            string.Equals(x.Status, "Serving", StringComparison.OrdinalIgnoreCase)
            && !order.Lines.Any(l => l.RestaurantOrderTableId == x.Id && l.LineStatus != "Cancelled"))
            .ToList();

        if (sourceTable is not null && newlyActivated.Count > 0)
        {
            var sourceLines = order.Lines
                .Where(l => l.RestaurantOrderTableId == sourceTable.Id
                            && l.LineStatus != "Cancelled"
                            && l.LineType != "Adjustment")
                .ToList();

            foreach (var targetTable in newlyActivated)
            {
                foreach (var source in sourceLines)
                {
                    var pricingRule = source.PricingRule ?? "Manual";
                    if (string.Equals(pricingRule, "PerRoom", StringComparison.OrdinalIgnoreCase))
                        continue;

                    var qty = source.Qty;
                    if (string.Equals(pricingRule, "PerPerson", StringComparison.OrdinalIgnoreCase))
                        qty = Math.Max(1, targetTable.GuestCount);

                    order.Lines.Add(new AlacarteOrderLine
                    {
                        Id = Guid.NewGuid(),
                        AlacarteOrderId = order.Id,
                        RestaurantOrderTableId = targetTable.Id,
                        MenuItemId = source.MenuItemId,
                        LineNo = order.Lines.Count == 0 ? 1 : order.Lines.Max(l => l.LineNo) + 1,
                        MenuItemCode = source.MenuItemCode,
                        MenuItemName = source.MenuItemName,
                        UnitName = source.UnitName,
                        Qty = qty,
                        UnitPrice = source.UnitPrice,
                        VatRate = source.VatRate,
                        PricingRule = pricingRule,
                        LineType = "Normal",
                        KitchenStatus = "New",
                        LineStatus = "Ordered",
                        CreatedAt = now,
                        UpdatedAt = now
                    });
                }
            }
        }

        // Cancel items on deactivated tables (guest count decreased)
        var deactivatedTables = physicalOrderTables.Where(x =>
            !string.Equals(x.Status, "Serving", StringComparison.OrdinalIgnoreCase)
            && order.Lines.Any(l => l.RestaurantOrderTableId == x.Id && l.LineStatus != "Cancelled"))
            .ToList();

        foreach (var dt in deactivatedTables)
        {
            foreach (var line in order.Lines.Where(l => l.RestaurantOrderTableId == dt.Id && l.LineStatus != "Cancelled"))
            {
                line.LineStatus = "Cancelled";
                line.UpdatedAt = now;
            }
        }

        var summaryTable = order.OrderTables.FirstOrDefault(x => x.IsVirtual);
        if (summaryTable is not null)
        {
            summaryTable.GuestCount = request.GuestCount;
            summaryTable.Status = "Serving";
            summaryTable.UpdatedAt = now;
        }

        var firstServingTable = physicalOrderTables.FirstOrDefault(x => string.Equals(x.Status, "Serving", StringComparison.OrdinalIgnoreCase));
        order.DiningTableId = firstServingTable?.DiningTableId;
        order.ActualGuestCount = request.GuestCount;
        order.Status = "Serving";
        order.Note = SetServiceWindowMetadata(TrimOrNull(request.Note), normalizedStartTime, normalizedEndTime);
        order.UpdatedAt = now;

        if (order.ReservationId.HasValue)
        {
            var reservation = await _dbContext.TableReservations.FirstOrDefaultAsync(x => x.Id == order.ReservationId.Value, cancellationToken);
            if (reservation is not null)
            {
                reservation.CustomerName = TrimOrNull(request.CustomerName);
                reservation.Phone = request.Phone?.Trim() ?? string.Empty;
                reservation.ReservationDateTime = DateTime.Today.Add(startTime);
                reservation.GuestCount = request.GuestCount;
                reservation.DiningRoomId = room.Id;
                reservation.DiningTableId = firstServingTable?.DiningTableId;
                reservation.Status = "Confirmed";
                reservation.Note = order.Note;
                reservation.UpdatedAt = now;
            }
        }
        else
        {
            var reservation = new TableReservation
            {
                Id = Guid.NewGuid(),
                ReservationNo = await GenerateReservationNoAsync(DateTime.Today, cancellationToken),
                CustomerName = TrimOrNull(request.CustomerName),
                Phone = request.Phone?.Trim() ?? string.Empty,
                ReservationDateTime = DateTime.Today.Add(startTime),
                GuestCount = request.GuestCount,
                DiningRoomId = room.Id,
                DiningTableId = firstServingTable?.DiningTableId,
                Status = "Confirmed",
                Note = order.Note,
                CreatedAt = now,
                UpdatedAt = now
            };
            _dbContext.TableReservations.Add(reservation);
            order.ReservationId = reservation.Id;
        }

        room.Status = "Occupied";
        room.UpdatedAt = now;
        Recalculate(order);
        await SyncServingGroupsAsync(order, roomTables, capacityPerTable, now, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return await LoadOrderDtoAsync(order.Id, cancellationToken);
    }

    public async Task<AlacarteOrderDto> UpdateRoomWalkInServingPlanAsync(UpdateRoomWalkInServingPlanRequest request, CancellationToken cancellationToken = default)
    {
        if (request.OrderId == Guid.Empty) throw new InvalidOperationException("Thiáº¿u Ä‘Æ¡n cáº§n cáº­p nháº­t cáº¥u hÃ¬nh bÃ n.");
        if (request.Tables is null || request.Tables.Count == 0) throw new InvalidOperationException("ChÆ°a cÃ³ cáº¥u hÃ¬nh bÃ n Ä‘á»ƒ cáº­p nháº­t.");

        var order = await LoadOrderEntityAsync(request.OrderId, cancellationToken);
        EnsureOrderEditable(order);
        if (!string.Equals(order.OrderType, "WalkIn", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Chá»‰ há»— trá»£ chá»‰nh tay cáº¥u hÃ¬nh cho Ä‘Æ¡n má»Ÿ phÃ²ng dÃ¹ng ngay.");
        }

        var roomId = order.DiningRoomId ?? order.Reservation?.DiningRoomId
            ?? throw new InvalidOperationException("ÄÆ¡n chÆ°a gáº¯n phÃ²ng.");
        var room = await _dbContext.DiningRooms
            .Include(x => x.Tables)
            .FirstOrDefaultAsync(x => x.Id == roomId, cancellationToken)
            ?? throw new InvalidOperationException("KhÃ´ng tÃ¬m tháº¥y phÃ²ng Äƒn.");
        await EnsureRoomPhysicalTablesAsync(room, cancellationToken);

        var physicalOrderTables = order.OrderTables
            .Where(x => !x.IsVirtual)
            .OrderBy(x => x.SortOrder)
            .ToList();
        if (physicalOrderTables.Count == 0) throw new InvalidOperationException("ÄÆ¡n chÆ°a cÃ³ bÃ n váº­t lÃ½ Ä‘á»ƒ cáº­p nháº­t.");

        var requestLookup = request.Tables
            .GroupBy(x => x.OrderTableId)
            .ToDictionary(x => x.Key, x => x.First());
        var diningTableLookup = room.Tables.ToDictionary(x => x.Id);
        var orderTableLookup = physicalOrderTables.ToDictionary(x => x.Id);
        var standardSeats = Math.Max(1, room.Tables.Where(x => x.SeatCount > 0).Select(x => x.SeatCount).DefaultIfEmpty(10).Max());
        var softCapacityPerTable = standardSeats + 2;

        var servingRows = request.Tables
            .Where(x => x.IsServing && x.GuestCount > 0)
            .ToList();
        if (servingRows.Count == 0) throw new InvalidOperationException("Cáº§n Ã­t nháº¥t 1 bÃ n Ä‘ang phá»¥c vá»¥.");

        var totalGuests = servingRows.Sum(x => Math.Max(0, x.GuestCount));
        if (totalGuests <= 0) throw new InvalidOperationException("Tá»•ng sá»‘ khÃ¡ch phá»¥c vá»¥ pháº£i lá»›n hÆ¡n 0.");

        foreach (var row in servingRows)
        {
            if (!orderTableLookup.TryGetValue(row.OrderTableId, out var orderTable)
                || !orderTable.DiningTableId.HasValue
                || !diningTableLookup.TryGetValue(orderTable.DiningTableId.Value, out var diningTable))
            {
                throw new InvalidOperationException("CÃ³ bÃ n cáº¥u hÃ¬nh khÃ´ng thuá»™c phÃ²ng hiá»‡n táº¡i.");
            }

            var maxSeats = Math.Max(softCapacityPerTable, diningTable.SeatCount + 2);
            if (row.GuestCount > maxSeats)
            {
                throw new InvalidOperationException($"BÃ n {diningTable.Name} vÆ°á»£t sá»©c chá»©a má»m cho phÃ©p ({maxSeats} khÃ¡ch).");
            }
        }

        var now = DateTime.UtcNow;
        RestaurantOrderTable? firstServingTable = null;
        foreach (var orderTable in physicalOrderTables)
        {
            if (!requestLookup.TryGetValue(orderTable.Id, out var row))
            {
                row = new UpdateRoomWalkInServingTableRequest
                {
                    OrderTableId = orderTable.Id,
                    IsServing = false,
                    GuestCount = 0
                };
            }

            var guestCount = row.IsServing ? Math.Max(0, row.GuestCount) : 0;
            orderTable.GuestCount = guestCount;
            orderTable.Status = row.IsServing && guestCount > 0 ? "Serving" : "Waiting";
            orderTable.UpdatedAt = now;

            if (firstServingTable is null && string.Equals(orderTable.Status, "Serving", StringComparison.OrdinalIgnoreCase))
            {
                firstServingTable = orderTable;
            }

            if (orderTable.DiningTableId.HasValue && diningTableLookup.TryGetValue(orderTable.DiningTableId.Value, out var diningTable))
            {
                diningTable.Status = string.Equals(orderTable.Status, "Serving", StringComparison.OrdinalIgnoreCase) ? "Serving" : "Empty";
                diningTable.UpdatedAt = now;
            }
        }

        var summaryTable = order.OrderTables.FirstOrDefault(x => x.IsVirtual);
        if (summaryTable is not null)
        {
            summaryTable.GuestCount = totalGuests;
            summaryTable.Status = "Serving";
            summaryTable.UpdatedAt = now;
        }

        order.DiningTableId = firstServingTable?.DiningTableId;
        order.ActualGuestCount = totalGuests;
        order.Status = "Serving";
        order.UpdatedAt = now;

        if (order.ReservationId.HasValue)
        {
            var reservation = await _dbContext.TableReservations.FirstOrDefaultAsync(x => x.Id == order.ReservationId.Value, cancellationToken);
            if (reservation is not null)
            {
                reservation.GuestCount = totalGuests;
                reservation.DiningRoomId = room.Id;
                reservation.DiningTableId = firstServingTable?.DiningTableId;
                reservation.UpdatedAt = now;
            }
        }

        room.Status = "Occupied";
        room.UpdatedAt = now;
        await SyncServingGroupsAsync(order, room.Tables.ToList(), standardSeats, now, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return await LoadOrderDtoAsync(order.Id, cancellationToken);
    }

    public async Task<AlacarteOrderDto> AddOrUpdateLineAsync(SaveAlacarteOrderLineRequest request, CancellationToken cancellationToken = default)
    {
        if (request.Qty <= 0) throw new InvalidOperationException("Sá»‘ lÆ°á»£ng pháº£i lá»›n hÆ¡n 0.");
        var order = await LoadOrderEntityAsync(request.OrderId, cancellationToken);
        EnsureOrderEditable(order);
        var menuItem = await _dbContext.MenuItems.AsNoTracking().FirstOrDefaultAsync(x => x.Id == request.MenuItemId && x.IsActive, cancellationToken)
            ?? throw new InvalidOperationException("KhÃ´ng tÃ¬m tháº¥y mÃ³n.");
        var now = DateTime.UtcNow;
        var line = request.OrderLineId.HasValue
            ? order.Lines.FirstOrDefault(x => x.Id == request.OrderLineId.Value)
            : order.Lines.FirstOrDefault(x => x.MenuItemId == request.MenuItemId
                                              && x.RestaurantOrderTableId == request.OrderTableId
                                              && x.KitchenStatus != "Sent"
                                              && x.LineStatus != "Cancelled");
        if (!request.OrderLineId.HasValue && line is not null)
        {
            request.Qty += line.Qty;
            request.SupplementQuantity += line.SupplementQuantity;
        }
        if (line is null)
        {
            line = new AlacarteOrderLine
            {
                Id = Guid.NewGuid(),
                AlacarteOrderId = order.Id,
                RestaurantOrderTableId = request.OrderTableId,
                MenuItemId = menuItem.Id,
                LineNo = order.Lines.Count == 0 ? 1 : order.Lines.Max(x => x.LineNo) + 1,
                MenuItemCode = menuItem.Code,
                MenuItemName = menuItem.Name,
                UnitName = menuItem.UnitName,
                UnitPrice = menuItem.UnitPrice,
                VatRate = menuItem.VatRate,
                SupplementQuantity = 0,
                KitchenStatus = "New",
                LineStatus = "Ordered",
                LineType = order.PreparationTickets.Any(x => x.Status == "Sent") ? "Supplemental" : "Normal",
                CreatedAt = now
            };
            order.Lines.Add(line);
            _dbContext.AlacarteOrderLines.Add(line);
        }
        else if (string.Equals(line.KitchenStatus, "Sent", StringComparison.OrdinalIgnoreCase))
        {
            await ApplySentLineAdjustmentAsync(order, line, request, now, cancellationToken);
            Recalculate(order);
            await _dbContext.SaveChangesAsync(cancellationToken);
            return await LoadOrderDtoAsync(order.Id, cancellationToken);
        }

        line.Qty = request.Qty;
        line.SupplementQuantity = Math.Max(0, request.SupplementQuantity);
        line.KitchenNote = TrimOrNull(request.KitchenNote);
        line.SupplementNote = NormalizeSupplementNote(request.SupplementNote);
        line.UpdatedAt = now;
        Recalculate(line);
        Recalculate(order);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return await LoadOrderDtoAsync(order.Id, cancellationToken);
    }

    public async Task<AlacarteOrderDto> AddExtraServiceToOrderAsync(AddAlacarteOrderExtraServiceRequest request, CancellationToken cancellationToken = default)
    {
        if (request.Qty <= 0) throw new InvalidOperationException("Sá»‘ lÆ°á»£ng dá»‹ch vá»¥ pháº£i lá»›n hÆ¡n 0.");
        var order = await LoadOrderEntityAsync(request.OrderId, cancellationToken);
        EnsureOrderEditable(order);
        var service = await _dbContext.RestaurantExtraServices.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == request.RestaurantExtraServiceId && x.IsActive, cancellationToken)
            ?? throw new InvalidOperationException("KhÃ´ng tÃ¬m tháº¥y dá»‹ch vá»¥ bá»• sung.");

        var now = DateTime.UtcNow;
        var line = order.ExtraServices.FirstOrDefault(x => x.RestaurantExtraServiceId == service.Id);
        if (line is null)
        {
            line = new RestaurantOrderExtraService
            {
                Id = Guid.NewGuid(),
                AlacarteOrderId = order.Id,
                RestaurantExtraServiceId = service.Id,
                Description = service.Name,
                UnitPrice = service.DefaultPrice,
                VatRate = service.VatRate
            };
            order.ExtraServices.Add(line);
            _dbContext.RestaurantOrderExtraServices.Add(line);
        }

        line.Qty += request.Qty;
        var baseAmount = line.Qty * line.UnitPrice;
        line.Amount = Math.Round(baseAmount + baseAmount * line.VatRate / 100m, 2);
        order.UpdatedAt = now;
        Recalculate(order);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return await LoadOrderDtoAsync(order.Id, cancellationToken);
    }

    public async Task<AlacarteOrderDto> RemoveLineAsync(Guid orderId, Guid orderLineId, bool createCancelTicket, string? cancelledBy = null, CancellationToken cancellationToken = default)
    {
        var order = await LoadOrderEntityAsync(orderId, cancellationToken);
        EnsureOrderEditable(order);
        var line = order.Lines.FirstOrDefault(x => x.Id == orderLineId);
        if (line is null) return await LoadOrderDtoAsync(order.Id, cancellationToken);
        if (createCancelTicket && (line.KitchenStatus == "Sent" || order.PreparationTickets.Any(x => x.Status == "Sent")))
        {
            var ticket = await CreatePreparationTicketAsync(order, [line], "Cancel", cancelledBy, cancellationToken);
            ticket.Note = $"Há»§y mÃ³n: {line.MenuItemName}";
            line.KitchenStatus = "Cancelled";
            line.LineStatus = "Cancelled";
        }

        order.Lines.Remove(line);
        _dbContext.AlacarteOrderLines.Remove(line);
        Recalculate(order);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return await LoadOrderDtoAsync(order.Id, cancellationToken);
    }

    public async Task<AlacarteOrderDto> PayOrderAsync(PayAlacarteOrderRequest request, CancellationToken cancellationToken = default)
    {
        var order = await LoadOrderEntityAsync(request.OrderId, cancellationToken);
        if (order.Status == "Paid") throw new InvalidOperationException("\u0110\u01a1n \u0111\u00e3 thanh to\u00e1n.");
        if (order.Status == "Cancelled") throw new InvalidOperationException("\u0110\u01a1n \u0111\u00e3 h\u1ee7y, kh\u00f4ng th\u1ec3 thanh to\u00e1n.");
        var hasBillableLines = order.Lines.Any(x => x.LineStatus != "Cancelled") || order.ExtraServices.Any();
        if (!hasBillableLines) throw new InvalidOperationException("Kh\u00f4ng th\u1ec3 thanh to\u00e1n \u0111\u01a1n r\u1ed7ng.");

        Recalculate(order);
        var paidAmount = request.PaidAmount ?? order.RemainingAmount;
        if (paidAmount < order.RemainingAmount) throw new InvalidOperationException("S\u1ed1 ti\u1ec1n thanh to\u00e1n ch\u01b0a \u0111\u1ee7 \u0111\u1ec3 \u0111\u00f3ng \u0111\u01a1n.");

        var now = DateTime.UtcNow;
        order.PaidAmount = order.TotalAmount;
        order.RemainingAmount = 0m;
        order.PaymentStatus = "Paid";
        order.Status = "Paid";
        order.UpdatedAt = now;
        if (order.Reservation is not null)
        {
            order.Reservation.Status = "Completed";
            order.Reservation.UpdatedAt = now;
        }
        order.Note = AppendNote(order.Note, $"Thanh to\u00e1n b\u1edfi {Normalize(request.PaidBy, "system")} ({Normalize(request.PaymentMethod, "Ti\u1ec1n m\u1eb7t")})");
        await ReleaseOrderTablesAsync(order, now, cancellationToken, useCleaningForServingTables: true);

        await _dbContext.SaveChangesAsync(cancellationToken);
        return await LoadOrderDtoAsync(order.Id, cancellationToken);
    }

    public async Task<AlacarteOrderDto> CancelOrderAsync(CancelAlacarteOrderRequest request, CancellationToken cancellationToken = default)
    {
        var order = await LoadOrderEntityAsync(request.OrderId, cancellationToken);
        if (order.Status == "Paid") throw new InvalidOperationException("\u0110\u01a1n \u0111\u00e3 thanh to\u00e1n, kh\u00f4ng th\u1ec3 h\u1ee7y.");
        if (order.Status == "Cancelled") return await LoadOrderDtoAsync(order.Id, cancellationToken);

        var now = DateTime.UtcNow;
        var sentLines = order.Lines.Where(x => x.KitchenStatus == "Sent" && x.LineStatus != "Cancelled").ToList();
        if (sentLines.Count > 0)
        {
            await CreatePreparationTicketAsync(order, sentLines, "Cancel", request.CancelledBy, cancellationToken, note: Normalize(request.Reason, "H\u1ee7y \u0111\u01a1n"));
            foreach (var line in sentLines)
            {
                line.KitchenStatus = "Cancelled";
                line.LineStatus = "Cancelled";
                line.UpdatedAt = now;
            }
        }

        order.Status = "Cancelled";
        order.PaymentStatus = "Cancelled";
        order.RemainingAmount = 0m;
        if (order.Reservation is not null)
        {
            order.Reservation.Status = "Cancelled";
            order.Reservation.UpdatedAt = now;
        }
        order.Note = AppendNote(order.Note, $"H\u1ee7y \u0111\u01a1n: {Normalize(request.Reason, "Kh\u00e1ch h\u1ee7y \u0111\u01a1n")}");
        order.UpdatedAt = now;
        await ReleaseOrderTablesAsync(order, now, cancellationToken, useCleaningForServingTables: false);

        await _dbContext.SaveChangesAsync(cancellationToken);
        return await LoadOrderDtoAsync(order.Id, cancellationToken);
    }

    public async Task<IReadOnlyList<RestaurantPreparationTicketDto>> SendToPreparationAsync(SendAlacartePreparationRequest request, CancellationToken cancellationToken = default)
    {
        var order = await LoadOrderEntityAsync(request.OrderId, cancellationToken);
        EnsureOrderEditable(order);
        var pending = order.Lines.Where(x => x.KitchenStatus != "Sent" && x.LineStatus != "Cancelled").OrderBy(x => x.LineNo).ToList();
        if (pending.Count == 0) throw new InvalidOperationException("T\u1ea5t c\u1ea3 m\u00f3n \u0111\u00e3 \u0111\u01b0\u1ee3c g\u1eedi b\u1ebfp/bar.");
        await _menuCatalogService.EnsureDefaultsAsync(cancellationToken);
        var kitchen = pending.Where(x => !IsBar(x.MenuItem?.PreparationArea)).ToList();
        var bar = pending.Where(x => IsBar(x.MenuItem?.PreparationArea)).ToList();
        var tickets = new List<RestaurantPreparationTicket>();
        if (kitchen.Count > 0) tickets.Add(await CreatePreparationTicketAsync(order, kitchen, "Send", request.SentBy, cancellationToken, "Kitchen", request.Note));
        if (bar.Count > 0) tickets.Add(await CreatePreparationTicketAsync(order, bar, "Send", request.SentBy, cancellationToken, "Bar", request.Note));
        var now = DateTime.UtcNow;
        foreach (var line in pending)
        {
            line.KitchenStatus = "Sent";
            line.SentAt = now;
            line.UpdatedAt = now;
        }
        await PromoteOrderToServingIfDueAsync(order, now, cancellationToken);
        if (!string.Equals(order.Status, "Serving", StringComparison.OrdinalIgnoreCase))
        {
            order.Status = "SentKitchen";
        }
        order.KitchenSentAt = now;
        order.UpdatedAt = now;
        await CreateConsumptionIssuesAsync(order, pending, now, request.SentBy, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return tickets.Select(ToDto).ToList();
    }

    public async Task<IReadOnlyList<RestaurantServingGroupDto>> GetServingGroupsAsync(Guid orderId, CancellationToken cancellationToken = default)
        => await _dbContext.RestaurantServingGroups
            .AsNoTracking()
            .Include(x => x.Tables)
            .ThenInclude(x => x.OrderTable)
            .Where(x => x.AlacarteOrderId == orderId)
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Name)
            .Select(x => ToDto(x))
            .ToListAsync(cancellationToken);

    public async Task<RestaurantServingGroupDto> SaveServingGroupAsync(SaveRestaurantServingGroupRequest request, CancellationToken cancellationToken = default)
    {
        if (request.OrderId == Guid.Empty)
        {
            throw new InvalidOperationException("Thiáº¿u Ä‘Æ¡n Ã€ La Carte Ä‘á»ƒ lÆ°u nhÃ³m phá»¥c vá»¥.");
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new InvalidOperationException("TÃªn nhÃ³m phá»¥c vá»¥ khÃ´ng Ä‘Æ°á»£c Ä‘á»ƒ trá»‘ng.");
        }

        var order = await _dbContext.AlacarteOrders
            .Include(x => x.OrderTables)
            .Include(x => x.ServingGroups)
            .ThenInclude(x => x.Tables)
            .FirstOrDefaultAsync(x => x.Id == request.OrderId, cancellationToken)
            ?? throw new InvalidOperationException("KhÃ´ng tÃ¬m tháº¥y Ä‘Æ¡n Ã€ La Carte.");

        var selectedTableIds = request.OrderTableIds
            .Where(x => x != Guid.Empty)
            .Distinct()
            .ToList();
        if (selectedTableIds.Count == 0)
        {
            throw new InvalidOperationException("Vui lÃ²ng chá»n Ã­t nháº¥t 1 bÃ n Ä‘á»ƒ táº¡o nhÃ³m phá»¥c vá»¥.");
        }

        var validTables = order.OrderTables
            .Where(x => !x.IsVirtual && string.Equals(x.Status, "Serving", StringComparison.OrdinalIgnoreCase))
            .Where(x => selectedTableIds.Contains(x.Id))
            .OrderBy(x => x.SortOrder)
            .ToList();
        if (validTables.Count == 0)
        {
            throw new InvalidOperationException("NhÃ³m phá»¥c vá»¥ chá»‰ Ä‘Æ°á»£c gÃ¡n cho cÃ¡c bÃ n Ä‘ang phá»¥c vá»¥.");
        }

        var diningTableIds = validTables
            .Where(x => x.DiningTableId.HasValue)
            .Select(x => x.DiningTableId!.Value)
            .Distinct()
            .ToList();

        var seatLookup = diningTableIds.Count == 0
            ? new Dictionary<Guid, int>()
            : await _dbContext.DiningTables
                .AsNoTracking()
                .Where(x => diningTableIds.Contains(x.Id))
                .ToDictionaryAsync(x => x.Id, x => Math.Max(1, x.SeatCount), cancellationToken);

        var now = DateTime.UtcNow;
        RestaurantServingGroup entity;
        if (request.Id.HasValue)
        {
            entity = order.ServingGroups.FirstOrDefault(x => x.Id == request.Id.Value)
                ?? throw new InvalidOperationException("KhÃ´ng tÃ¬m tháº¥y nhÃ³m phá»¥c vá»¥ cáº§n sá»­a.");
            if (IsSystemDefinedServingGroup(entity))
            {
                throw new InvalidOperationException("NhÃ³m phá»¥c vá»¥ há»‡ thá»‘ng khÃ´ng thá»ƒ sá»­a trá»±c tiáº¿p.");
            }

            _dbContext.RestaurantServingGroupTables.RemoveRange(entity.Tables);
            entity.Tables.Clear();
        }
        else
        {
            entity = new RestaurantServingGroup
            {
                Id = Guid.NewGuid(),
                AlacarteOrderId = order.Id,
                CreatedAt = now
            };
            _dbContext.RestaurantServingGroups.Add(entity);
        }

        entity.Code = request.Id.HasValue
            ? entity.Code
            : (string.IsNullOrWhiteSpace(request.Code) ? BuildCustomServingGroupCode(order.ServingGroups) : request.Code.Trim());
        entity.Name = request.Name.Trim();
        entity.ScopeType = string.IsNullOrWhiteSpace(request.ScopeType) ? "Custom" : request.ScopeType.Trim();
        entity.ServingRule = string.IsNullOrWhiteSpace(request.ServingRule) ? "PerTable" : request.ServingRule.Trim();
        entity.Note = TrimOrNull(request.Note);
        entity.SortOrder = request.Id.HasValue
            ? entity.SortOrder <= 0 ? GetNextCustomServingGroupSortOrder(order.ServingGroups) : entity.SortOrder
            : GetNextCustomServingGroupSortOrder(order.ServingGroups);
        entity.UpdatedAt = now;

        foreach (var table in validTables)
        {
            var standardSeats = table.DiningTableId.HasValue && seatLookup.TryGetValue(table.DiningTableId.Value, out var mappedSeats)
                ? mappedSeats
                : Math.Max(1, table.GuestCount);
            entity.Tables.Add(new RestaurantServingGroupTable
            {
                Id = Guid.NewGuid(),
                RestaurantServingGroupId = entity.Id,
                RestaurantOrderTableId = table.Id,
                DiningTableId = table.DiningTableId,
                GuestCount = Math.Max(0, table.GuestCount),
                ExtraGuestCount = Math.Max(0, table.GuestCount - Math.Max(1, standardSeats)),
                IsServing = true,
                CreatedAt = now,
                UpdatedAt = now
            });
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        var persisted = await _dbContext.RestaurantServingGroups
            .AsNoTracking()
            .Include(x => x.Tables)
            .ThenInclude(x => x.OrderTable)
            .FirstAsync(x => x.Id == entity.Id, cancellationToken);
        return ToDto(persisted);
    }

    public async Task DeleteServingGroupAsync(Guid servingGroupId, CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.RestaurantServingGroups
            .Include(x => x.Tables)
            .FirstOrDefaultAsync(x => x.Id == servingGroupId, cancellationToken)
            ?? throw new InvalidOperationException("KhÃ´ng tÃ¬m tháº¥y nhÃ³m phá»¥c vá»¥ cáº§n xÃ³a.");

        if (IsSystemDefinedServingGroup(entity))
        {
            throw new InvalidOperationException("NhÃ³m phá»¥c vá»¥ há»‡ thá»‘ng khÃ´ng thá»ƒ xÃ³a.");
        }

        _dbContext.RestaurantServingGroupTables.RemoveRange(entity.Tables);
        _dbContext.RestaurantServingGroups.Remove(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<RestaurantDepositDto> AddDepositAsync(AddRestaurantDepositRequest request, CancellationToken cancellationToken = default)
    {
        if (request.Amount <= 0) throw new InvalidOperationException("Sá»‘ tiá»n cá»c pháº£i lá»›n hÆ¡n 0.");
        if (request.AlacarteOrderId.HasValue)
        {
            var order = await _dbContext.AlacarteOrders.AsNoTracking().Include(x => x.Deposits).FirstOrDefaultAsync(x => x.Id == request.AlacarteOrderId.Value, cancellationToken)
                ?? throw new InvalidOperationException("KhÃ´ng tÃ¬m tháº¥y Ä‘Æ¡n Ä‘á»ƒ nháº­n cá»c.");
            var deposited = order.Deposits.Where(x => x.Status == "Confirmed").Sum(x => x.Amount);
            if (deposited + request.Amount > order.TotalAmount) throw new InvalidOperationException("Tá»•ng tiá»n cá»c khÃ´ng Ä‘Æ°á»£c vÆ°á»£t tá»•ng pháº£i thu cá»§a Ä‘Æ¡n.");
        }

        if (request.RestaurantContractId.HasValue)
        {
            var contract = await _dbContext.RestaurantContracts.AsNoTracking().Include(x => x.Deposits).FirstOrDefaultAsync(x => x.Id == request.RestaurantContractId.Value, cancellationToken)
                ?? throw new InvalidOperationException("KhÃ´ng tÃ¬m tháº¥y há»£p Ä‘á»“ng Ä‘á»ƒ nháº­n cá»c.");
            var deposited = contract.Deposits.Where(x => x.Status == "Confirmed").Sum(x => x.Amount);
            if (deposited + request.Amount > contract.TotalAmount) throw new InvalidOperationException("Tá»•ng tiá»n cá»c khÃ´ng Ä‘Æ°á»£c vÆ°á»£t tá»•ng giÃ¡ trá»‹ há»£p Ä‘á»“ng.");
        }

        var date = request.Date ?? DateTime.Today;
        var deposit = new RestaurantDeposit
        {
            Id = Guid.NewGuid(),
            DepositNo = await GenerateDepositNoAsync(date, cancellationToken),
            AlacarteOrderId = request.AlacarteOrderId,
            RestaurantContractId = request.RestaurantContractId,
            TableReservationId = request.TableReservationId,
            Date = date,
            Amount = request.Amount,
            PaymentMethod = request.PaymentMethod.Trim(),
            CashFundId = request.CashFundId,
            BankAccountId = request.BankAccountId,
            Receiver = TrimOrNull(request.Receiver),
            Note = TrimOrNull(request.Note),
            Status = "Confirmed"
        };
        _dbContext.RestaurantDeposits.Add(deposit);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return ToDto(deposit);
    }

    public async Task<IReadOnlyList<AlacarteOrderDto>> GetOpenOrdersAsync(CancellationToken cancellationToken = default)
        => await _dbContext.AlacarteOrders.AsNoTracking().Include(x => x.Table).ThenInclude(x => x!.Room).Include(x => x.Room)
            .Include(x => x.OrderTables)
            .Include(x => x.ServingGroups).ThenInclude(x => x.Tables).ThenInclude(x => x.OrderTable)
            .Include(x => x.Lines)
            .Include(x => x.ExtraServices)
            .Include(x => x.Reservation).ThenInclude(x => x!.Room)
            .Include(x => x.Reservation).ThenInclude(x => x!.Table)
            .Where(x => x.Status != "Paid" && x.Status != "Cancelled" && x.Status != "Replaced")
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => ToDto(x))
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<RestaurantExtraServiceDto>> GetExtraServicesAsync(CancellationToken cancellationToken = default)
        => await _dbContext.RestaurantExtraServices.AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.ServiceType).ThenBy(x => x.Name)
            .Select(x => ToDto(x))
            .ToListAsync(cancellationToken);

    public async Task<RestaurantExtraServiceDto> SaveExtraServiceAsync(SaveRestaurantExtraServiceRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Code)) throw new InvalidOperationException("MÃ£ dá»‹ch vá»¥ khÃ´ng Ä‘Æ°á»£c Ä‘á»ƒ trá»‘ng.");
        if (string.IsNullOrWhiteSpace(request.Name)) throw new InvalidOperationException("TÃªn dá»‹ch vá»¥ khÃ´ng Ä‘Æ°á»£c Ä‘á»ƒ trá»‘ng.");
        if (request.DefaultPrice < 0) throw new InvalidOperationException("ÄÆ¡n giÃ¡ dá»‹ch vá»¥ khÃ´ng Ä‘Æ°á»£c Ã¢m.");
        if (request.VatRate < 0) throw new InvalidOperationException("VAT dá»‹ch vá»¥ khÃ´ng Ä‘Æ°á»£c Ã¢m.");

        var code = request.Code.Trim();
        var exists = await _dbContext.RestaurantExtraServices.AnyAsync(x => x.Code == code && (!request.Id.HasValue || x.Id != request.Id.Value), cancellationToken);
        if (exists) throw new InvalidOperationException("MÃ£ dá»‹ch vá»¥ Ä‘Ã£ tá»“n táº¡i.");

        var now = DateTime.UtcNow;
        var service = request.Id.HasValue
            ? await _dbContext.RestaurantExtraServices.FirstOrDefaultAsync(x => x.Id == request.Id.Value, cancellationToken) ?? throw new InvalidOperationException("KhÃ´ng tÃ¬m tháº¥y dá»‹ch vá»¥ bá»• sung.")
            : new RestaurantExtraService { Id = Guid.NewGuid(), CreatedAt = now };
        service.Code = code;
        service.Name = request.Name.Trim();
        service.ServiceType = Normalize(request.ServiceType, "Other");
        service.UnitName = Normalize(request.UnitName, "lần");
        service.DefaultPrice = request.DefaultPrice;
        service.VatRate = request.VatRate;
        service.IsVatIncluded = request.IsVatIncluded;
        service.IsActive = request.IsActive;
        service.Note = TrimOrNull(request.Note);
        service.ImagePath = TrimOrNull(request.ImagePath);
        service.ImageUrl = TrimOrNull(request.ImageUrl);
        service.UpdatedAt = now;
        if (!request.Id.HasValue) _dbContext.RestaurantExtraServices.Add(service);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return ToDto(service);
    }

    public async Task<IReadOnlyList<RestaurantExtraServiceDto>> SeedDefaultExtraServicesAsync(CancellationToken cancellationToken = default)
    {
        var defaults = new (string Code, string Name, string Type, decimal Price)[]
        {
            ("LED", "M\u00e0n LED", "Equipment", 1500000m),
            ("MC", "MC", "Staff", 1200000m),
            ("SINGER", "Ca s\u0129", "Staff", 2500000m),
            ("SOUND", "\u00c2m thanh", "Equipment", 1000000m),
            ("LIGHT", "\u00c1nh s\u00e1ng", "Equipment", 1000000m),
            ("DECOR", "Trang tr\u00ed", "Decor", 2000000m),
            ("FLOWER", "Hoa t\u01b0\u01a1i", "Decor", 800000m),
            ("PROJECTOR", "M\u00e1y chi\u1ebfu", "Equipment", 500000m),
            ("VIPROOM", "Ph\u00ed ph\u00f2ng VIP", "Fee", 500000m),
            ("SERVICECHARGE", "Ph\u00ed ph\u1ee5c v\u1ee5", "Fee", 0m),
            ("PHOTOBOOTH", "Photobooth", "Other", 1800000m)
        };

        var existingCodes = await _dbContext.RestaurantExtraServices.Select(x => x.Code).ToListAsync(cancellationToken);
        foreach (var item in defaults.Where(x => !existingCodes.Contains(x.Code)))
        {
            _dbContext.RestaurantExtraServices.Add(new RestaurantExtraService
            {
                Id = Guid.NewGuid(),
                Code = item.Code,
                Name = item.Name,
                ServiceType = item.Type,
                DefaultPrice = item.Price,
                VatRate = 0,
                IsVatIncluded = true,
                IsActive = true,
                ImagePath = null,
                ImageUrl = null,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return await GetExtraServicesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<RestaurantContractDto>> GetContractsAsync(CancellationToken cancellationToken = default)
        => await _dbContext.RestaurantContracts.AsNoTracking()
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => ToDto(x))
            .ToListAsync(cancellationToken);

    public async Task<RestaurantContractDto> SaveContractAsync(SaveRestaurantContractRequest request, CancellationToken cancellationToken = default)
    {
        if (request.TotalAmount < 0) throw new InvalidOperationException("Tá»•ng giÃ¡ trá»‹ há»£p Ä‘á»“ng khÃ´ng Ä‘Æ°á»£c Ã¢m.");
        if (request.DepositAmount < 0) throw new InvalidOperationException("Tiá»n cá»c khÃ´ng Ä‘Æ°á»£c Ã¢m.");
        if (request.DepositAmount > request.TotalAmount) throw new InvalidOperationException("Tiá»n cá»c khÃ´ng Ä‘Æ°á»£c vÆ°á»£t tá»•ng giÃ¡ trá»‹ há»£p Ä‘á»“ng.");

        var now = DateTime.UtcNow;
        var contract = request.Id.HasValue
            ? await _dbContext.RestaurantContracts.FirstOrDefaultAsync(x => x.Id == request.Id.Value, cancellationToken) ?? throw new InvalidOperationException("KhÃ´ng tÃ¬m tháº¥y há»£p Ä‘á»“ng.")
            : new RestaurantContract
            {
                Id = Guid.NewGuid(),
                ContractNo = string.IsNullOrWhiteSpace(request.ContractNo) ? await GenerateContractNoAsync(DateTime.Today, cancellationToken) : request.ContractNo.Trim(),
                CreatedAt = now
            };
        contract.CustomerId = request.CustomerId;
        contract.EventDescription = Normalize(request.EventDescription, $"{request.CustomerName} - {request.RoomName} - {request.EventDate:dd/MM/yyyy}");
        contract.TotalAmount = request.TotalAmount;
        contract.DepositAmount = request.DepositAmount;
        contract.RemainingAmount = Math.Max(0, request.TotalAmount - request.DepositAmount);
        contract.Status = Normalize(request.Status, "Draft");
        contract.SignedDate = request.SignedDate;
        contract.Note = TrimOrNull(request.Note);
        contract.UpdatedAt = now;
        if (!request.Id.HasValue) _dbContext.RestaurantContracts.Add(contract);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return ToDto(contract);
    }

    public async Task<RestaurantContractDto> CreateContractFromQuoteAsync(Guid quoteId, string? createdBy = null, CancellationToken cancellationToken = default)
    {
        var quote = await _dbContext.RestaurantQuotes.AsNoTracking().FirstOrDefaultAsync(x => x.Id == quoteId, cancellationToken)
            ?? throw new InvalidOperationException("KhÃ´ng tÃ¬m tháº¥y bÃ¡o giÃ¡.");
        return await SaveContractAsync(new SaveRestaurantContractRequest
        {
            CustomerName = quote.CustomerName,
            Phone = quote.Phone,
            EventDate = quote.EventDate,
            RoomName = quote.RoomName,
            EventDescription = $"BÃ¡o giÃ¡ {quote.QuoteNo} - {quote.CustomerName} - {quote.RoomName} - {quote.EventDate:dd/MM/yyyy}",
            TotalAmount = quote.TotalAmount,
            DepositAmount = quote.DepositAmount,
            Status = quote.ApprovedOrderId.HasValue ? "Signed" : "Draft",
            SignedDate = quote.ApprovedOrderId.HasValue ? DateTime.Today : null,
            Note = $"Táº¡o tá»« bÃ¡o giÃ¡ {quote.QuoteNo} bá»Ÿi {Normalize(createdBy, "system")}"
        }, cancellationToken);
    }

    public async Task<IReadOnlyList<RestaurantQuoteDto>> GetQuotesAsync(CancellationToken cancellationToken = default)
        => await _dbContext.RestaurantQuotes.AsNoTracking()
            .Include(x => x.QuoteTables)
            .Include(x => x.Lines)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => ToDto(x))
            .ToListAsync(cancellationToken);

    public async Task<RestaurantQuoteDto> SaveQuoteAsync(SaveRestaurantQuoteRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.CustomerName)) throw new InvalidOperationException("Ten khach hang khong duoc de trong.");
        if (request.GuestCount <= 0) throw new InvalidOperationException("Tong so khach phai lon hon 0.");
        if (request.EventDate == default) throw new InvalidOperationException("Thieu ngay bao gia.");
        if (request.Lines.Count == 0) throw new InvalidOperationException("Bao gia phai co mon an hoac dich vu.");
        if (request.DepositAmount < 0) throw new InvalidOperationException("Tien coc khong duoc am.");

        var now = DateTime.UtcNow;
        var quote = request.Id.HasValue
            ? await _dbContext.RestaurantQuotes
                .Include(x => x.QuoteTables)
                .Include(x => x.Lines)
                .FirstOrDefaultAsync(x => x.Id == request.Id.Value, cancellationToken)
                ?? throw new InvalidOperationException("Khong tim thay bao gia.")
            : new RestaurantQuote
            {
                Id = Guid.NewGuid(),
                QuoteNo = string.IsNullOrWhiteSpace(request.QuoteNo) ? await GenerateQuoteNoAsync(DateTime.Today, cancellationToken) : request.QuoteNo.Trim(),
                CreatedAt = now
            };

        if (quote.ApprovedOrderId.HasValue)
        {
            throw new InvalidOperationException("Bao gia da duyet, khong duoc sua noi dung mon va dich vu.");
        }

        Guid? resolvedRoomId = null;
        Guid? resolvedTableId = null;
        var resolvedRoomName = request.RoomName;
        if (request.DiningRoomId.HasValue)
        {
            var room = await _dbContext.DiningRooms.AsNoTracking().FirstOrDefaultAsync(x => x.Id == request.DiningRoomId.Value, cancellationToken);
            if (room is not null)
            {
                resolvedRoomId = room.Id;
                if (string.IsNullOrWhiteSpace(resolvedRoomName)) resolvedRoomName = room.Name;
            }
        }

        if (request.DiningTableId.HasValue)
        {
            var table = await _dbContext.DiningTables.AsNoTracking().Include(x => x.Room).FirstOrDefaultAsync(x => x.Id == request.DiningTableId.Value, cancellationToken);
            if (table is not null)
            {
                resolvedTableId = table.Id;
                resolvedRoomId ??= table.DiningRoomId;
                if (string.IsNullOrWhiteSpace(resolvedRoomName)) resolvedRoomName = table.Room?.Name;
            }
        }

        if (resolvedRoomId is null && string.IsNullOrWhiteSpace(resolvedRoomName))
        {
            throw new InvalidOperationException("Bao gia VIP bat buoc phai chon phong.");
        }

        var tableStandard = request.TableStandard <= 0 ? 10 : request.TableStandard;
        var allocations = _tableAllocationEngine.Allocate(request.GuestCount, tableStandard);
        var realTableCount = allocations.Count(x => !x.IsVirtual);

        quote.CustomerName = request.CustomerName.Trim();
        quote.Phone = request.Phone.Trim();
        quote.TableCount = realTableCount;
        quote.GuestCount = request.GuestCount;
        quote.TableStandard = tableStandard;
        quote.DiningRoomId = resolvedRoomId;
        quote.DiningTableId = resolvedTableId;
        quote.RoomName = Normalize(resolvedRoomName, "Phong VIP");
        quote.EventDate = request.EventDate.Date;
        quote.StartTime = Normalize(request.StartTime, "17:00");
        quote.EndTime = Normalize(request.EndTime, "21:00");
        quote.DepositAmount = request.DepositAmount;
        quote.PaymentDeadline = request.PaymentDeadline.Trim();
        quote.PaymentMethod = Normalize(request.PaymentMethod, "Chuyen khoan");
        quote.DiscountAmount = request.DiscountAmount;
        quote.Promotion = request.Promotion.Trim();
        quote.ValidityDays = request.ValidityDays <= 0 ? 7 : request.ValidityDays;
        quote.CancelDeadlineDays = request.CancelDeadlineDays <= 0 ? 3 : request.CancelDeadlineDays;
        quote.CancelFeePercent = request.CancelFeePercent <= 0 ? 30 : request.CancelFeePercent;
        quote.BankAccount = request.BankAccount.Trim();
        quote.Status = string.IsNullOrWhiteSpace(request.Status) ? "Nhap" : request.Status.Trim();
        quote.Note = request.Note.Trim();
        quote.PreparedBy = request.PreparedBy.Trim();
        quote.UpdatedAt = now;

        var removedQuoteChildren = false;
        if (quote.QuoteTables.Count > 0)
        {
            _dbContext.RestaurantQuoteTables.RemoveRange(quote.QuoteTables);
            quote.QuoteTables.Clear();
            removedQuoteChildren = true;
        }

        foreach (var allocation in allocations)
        {
            var quoteTable = new RestaurantQuoteTable
            {
                Id = Guid.NewGuid(),
                RestaurantQuoteId = quote.Id,
                TableNo = allocation.TableNo,
                DisplayName = allocation.DisplayName,
                GuestCount = allocation.GuestCount,
                SortOrder = allocation.SortOrder,
                IsVirtual = allocation.IsVirtual,
                Note = allocation.Note,
                CreatedAt = now,
                UpdatedAt = now
            };
            quote.QuoteTables.Add(quoteTable);
            _dbContext.RestaurantQuoteTables.Add(quoteTable);
        }

        if (quote.Lines.Count > 0)
        {
            _dbContext.RestaurantQuoteLines.RemoveRange(quote.Lines);
            quote.Lines.Clear();
            removedQuoteChildren = true;
        }

        if (request.Id.HasValue && removedQuoteChildren)
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        var lineNo = 1;
        decimal totalAmount = 0m;
        foreach (var lineRequest in request.Lines)
        {
            if (lineRequest.Quantity <= 0 || string.IsNullOrWhiteSpace(lineRequest.Name))
            {
                continue;
            }

            var pricingRule = NormalizePricingRule(lineRequest.PricingRule, IsQuoteServiceLine(lineRequest.Type));
            var calculatedQuantity = _menuCalculationEngine.Calculate(pricingRule, request.GuestCount, realTableCount, lineRequest.Quantity);
            var total = Math.Round(calculatedQuantity * lineRequest.UnitPrice, 2);
            totalAmount += total;

            var quoteLine = new RestaurantQuoteLine
            {
                Id = Guid.NewGuid(),
                RestaurantQuoteId = quote.Id,
                MenuItemId = lineRequest.MenuItemId,
                LineNo = lineNo++,
                Name = lineRequest.Name.Trim(),
                LineType = Normalize(lineRequest.Type, "Mon an"),
                UnitPrice = lineRequest.UnitPrice,
                Quantity = lineRequest.Quantity,
                PricingRule = pricingRule,
                CalculatedQuantity = calculatedQuantity,
                Total = total
            };
            quote.Lines.Add(quoteLine);
            _dbContext.RestaurantQuoteLines.Add(quoteLine);
        }

        if (quote.Lines.Count == 0) throw new InvalidOperationException("Bao gia phai co dong hop le.");

        quote.TotalAmount = Math.Max(0, totalAmount - quote.DiscountAmount);
        if (quote.DepositAmount > quote.TotalAmount) throw new InvalidOperationException("Tien coc khong duoc vuot tong phai thu.");

        if (!request.Id.HasValue) _dbContext.RestaurantQuotes.Add(quote);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return await LoadQuoteDtoAsync(quote.Id, cancellationToken);
    }

    public async Task<RestaurantQuoteDto> ApproveQuoteAsync(Guid quoteId, string? approvedBy = null, CancellationToken cancellationToken = default)
    {
        await using var tx = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var quote = await _dbContext.RestaurantQuotes
                .Include(x => x.QuoteTables)
                .Include(x => x.Lines)
                .FirstOrDefaultAsync(x => x.Id == quoteId, cancellationToken)
                ?? throw new InvalidOperationException("Khong tim thay bao gia.");
            if (quote.ApprovedOrderId.HasValue) return await LoadQuoteDtoAsync(quote.Id, cancellationToken);
            if (quote.QuoteTables.Count == 0) throw new InvalidOperationException("Bao gia chua co ban phuc vu duoc sinh.");

            var now = DateTime.UtcNow;
            var existingOrder = await _dbContext.AlacarteOrders
                .Include(x => x.OrderTables)
                .Include(x => x.Table).ThenInclude(x => x!.Room)
                .Include(x => x.Room)
                .FirstOrDefaultAsync(x => x.RestaurantQuoteId == quote.Id, cancellationToken);
            if (existingOrder is not null && quote.ApprovedOrderId != existingOrder.Id)
            {
                quote.ApprovedOrderId = existingOrder.Id;
            }

            var reservationId = quote.TableReservationId ?? existingOrder?.ReservationId;
            TableReservation? reservation = null;
            if (reservationId.HasValue)
            {
                reservation = await _dbContext.TableReservations
                    .FirstOrDefaultAsync(x => x.Id == reservationId.Value, cancellationToken);
            }

            if (reservation is null)
            {
                reservation = new TableReservation
                {
                    Id = Guid.NewGuid(),
                    CompanyId = quote.CompanyId,
                    BranchId = quote.BranchId,
                    ReservationNo = await GenerateReservationNoAsync(quote.EventDate, cancellationToken),
                    CustomerName = quote.CustomerName,
                    Phone = quote.Phone,
                    ReservationDateTime = BuildReservationDateTime(quote.EventDate, quote.StartTime),
                    GuestCount = quote.GuestCount,
                    DiningRoomId = quote.DiningRoomId,
                    DiningTableId = quote.DiningTableId,
                    Status = "Confirmed",
                    Note = $"Tu bao gia {quote.QuoteNo}",
                    CreatedAt = now,
                    UpdatedAt = now
                };
                _dbContext.TableReservations.Add(reservation);
            }

            quote.TableReservationId = reservation.Id;

            var order = existingOrder;
            var scheduledStart = BuildReservationDateTime(quote.EventDate, quote.StartTime);
            var shouldStartServing = scheduledStart <= DateTime.Now;

            if (order is null)
            {
                order = await CreateOrderForQuoteAsync(quote, reservation.Id, approvedBy, cancellationToken);
            }
            else
            {
                order.ReservationId = reservation.Id;
                order.DiningRoomId ??= quote.DiningRoomId;
                order.DiningTableId ??= quote.DiningTableId;
                order.Status = ResolveApprovedQuoteOrderStatus(scheduledStart);
                order.PaymentStatus = quote.DepositAmount > 0 ? "Deposit" : (string.IsNullOrWhiteSpace(order.PaymentStatus) ? "Unpaid" : order.PaymentStatus);
                order.UpdatedAt = now;
                foreach (var orderTable in order.OrderTables)
                {
                    orderTable.Status = orderTable.IsVirtual
                        ? "Serving"
                        : shouldStartServing ? "Serving" : "Waiting";
                    orderTable.UpdatedAt = now;
                }
                if (order.Table is not null)
                {
                    order.Table.Status = shouldStartServing ? "Serving" : "Reserved";
                    order.Table.UpdatedAt = now;
                    if (order.Table.Room is not null)
                    {
                        order.Table.Room.Status = shouldStartServing ? "Occupied" : "Reserved";
                        order.Table.Room.UpdatedAt = now;
                    }
                }
                if (order.Room is not null)
                {
                    order.Room.Status = shouldStartServing ? "Occupied" : "Reserved";
                    order.Room.UpdatedAt = now;
                }
            }

            quote.ApprovedOrderId = order.Id;
            quote.Status = "Da duyet";
            quote.UpdatedAt = now;

            if (quote.DepositAmount > 0)
            {
                if (_cashFundService is null) throw new InvalidOperationException("Chua cau hinh dich vu Quy de tao phieu thu coc.");
                var funds = await _cashFundService.GetFundsAsync(cancellationToken);
                var fund = funds.FirstOrDefault(x => x.IsDefault && x.IsActive) ?? funds.FirstOrDefault(x => x.IsActive);
                if (fund is null) throw new InvalidOperationException("Chua co quy tien mat dang hoat dong de tao phieu thu coc.");

                var deposit = quote.DepositId.HasValue
                    ? await _dbContext.RestaurantDeposits.FirstOrDefaultAsync(x => x.Id == quote.DepositId.Value, cancellationToken)
                    : await _dbContext.RestaurantDeposits
                        .FirstOrDefaultAsync(x => x.AlacarteOrderId == order.Id, cancellationToken);

                if (deposit is null)
                {
                    deposit = new RestaurantDeposit
                    {
                        Id = Guid.NewGuid(),
                        DepositNo = await GenerateDepositNoAsync(DateTime.Today, cancellationToken),
                        AlacarteOrderId = order.Id,
                        TableReservationId = reservation.Id,
                        Date = DateTime.Today,
                        Amount = quote.DepositAmount,
                        PaymentMethod = quote.PaymentMethod,
                        CashFundId = fund.Id,
                        Receiver = approvedBy,
                        Note = $"Coc bao gia {quote.QuoteNo}",
                        Status = "Confirmed"
                    };
                    _dbContext.RestaurantDeposits.Add(deposit);
                }
                else
                {
                    deposit.AlacarteOrderId = order.Id;
                    deposit.TableReservationId = reservation.Id;
                    deposit.Amount = quote.DepositAmount;
                    deposit.PaymentMethod = quote.PaymentMethod;
                    deposit.CashFundId = fund.Id;
                    deposit.Receiver = approvedBy;
                    deposit.Status = "Confirmed";
                }

                quote.DepositId = deposit.Id;

                var existingCash = quote.DepositCashTransactionId.HasValue
                    ? await _dbContext.CashTransactions.FirstOrDefaultAsync(x => x.Id == quote.DepositCashTransactionId.Value, cancellationToken)
                    : await _dbContext.CashTransactions.FirstOrDefaultAsync(
                        x => x.SourceType == "RestaurantQuoteDeposit" && x.SourceId == quote.Id,
                        cancellationToken);

                if (existingCash is null)
                {
                    var cash = await _cashFundService.CreateReceiptAsync(new CashTransactionRequest
                    {
                        FundId = fund.Id,
                        TransactionDate = DateTime.Today,
                        Amount = quote.DepositAmount,
                        CounterAccountCode = "131",
                        PartnerType = "Customer",
                        SourceType = "RestaurantQuoteDeposit",
                        SourceId = quote.Id,
                        Description = $"Thu coc bao gia {quote.QuoteNo} - {quote.CustomerName}",
                        CreatedBy = approvedBy ?? "system"
                    }, cancellationToken);
                    quote.DepositCashTransactionId = cash.Id;
                    quote.DepositCashTransactionNo = cash.TransactionNo;
                }
                else
                {
                    quote.DepositCashTransactionId = existingCash.Id;
                    quote.DepositCashTransactionNo = existingCash.TransactionNo;
                }

                order.PaidAmount = quote.DepositAmount;
                Recalculate(order);
            }

            await _dbContext.SaveChangesAsync(cancellationToken);
            await tx.CommitAsync(cancellationToken);
            return await LoadQuoteDtoAsync(quote.Id, cancellationToken);
        }
        catch (DbUpdateException ex)
        {
            await tx.RollbackAsync(cancellationToken);
            _dbContext.ChangeTracker.Clear();
            throw new InvalidOperationException($"Loi duyet bao gia: {ex.InnerException?.Message ?? ex.Message}", ex);
        }
        catch
        {
            await tx.RollbackAsync(cancellationToken);
            _dbContext.ChangeTracker.Clear();
            throw;
        }
    }

    public async Task<IReadOnlyList<BanquetMenuPackageDto>> GetMenuPackagesAsync(CancellationToken cancellationToken = default)
        => await _dbContext.BanquetMenuPackages
            .AsNoTracking()
            .Include(x => x.Lines)
            .ThenInclude(x => x.MenuItem)
            .Where(x => x.IsActive)
            .OrderBy(x => x.Code)
            .Select(x => ToBanquetMenuPackageDto(x))
            .ToListAsync(cancellationToken);

    public async Task DeleteQuoteAsync(Guid quoteId, CancellationToken cancellationToken = default)
    {
        var quote = await _dbContext.RestaurantQuotes
            .Include(x => x.QuoteTables)
            .Include(x => x.Lines)
            .FirstOrDefaultAsync(x => x.Id == quoteId, cancellationToken)
            ?? throw new InvalidOperationException("Khong tim thay bao gia.");
        if (quote.ApprovedOrderId.HasValue || quote.DepositId.HasValue || quote.DepositCashTransactionId.HasValue)
        {
            throw new InvalidOperationException("Bao gia da phat sinh duyet/thanh toan, vui long luu tru thay vi xoa.");
        }

        _dbContext.RestaurantQuoteTables.RemoveRange(quote.QuoteTables);
        _dbContext.RestaurantQuoteLines.RemoveRange(quote.Lines);
        _dbContext.RestaurantQuotes.Remove(quote);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<AlacarteOrderDto> CopyMenuFromTableAsync(Guid orderId, Guid sourceOrderTableId, Guid targetOrderTableId, bool scaleByGuestCount = true, CancellationToken cancellationToken = default)
    {
        var order = await LoadOrderEntityAsync(orderId, cancellationToken);
        EnsureOrderEditable(order);

        var sourceTable = order.OrderTables.FirstOrDefault(x => x.Id == sourceOrderTableId)
            ?? throw new InvalidOperationException("Không tìm thấy bàn nguồn.");
        var targetTable = order.OrderTables.FirstOrDefault(x => x.Id == targetOrderTableId)
            ?? throw new InvalidOperationException("Không tìm thấy bàn đích.");
        if (targetTable.IsVirtual)
            throw new InvalidOperationException("Không thể copy món vào Bàn Tổng.");

        var sourceLines = order.Lines
            .Where(x => x.RestaurantOrderTableId == sourceOrderTableId
                        && x.LineStatus != "Cancelled"
                        && x.LineType != "Adjustment")
            .ToList();

        if (sourceLines.Count == 0)
            return await LoadOrderDtoAsync(order.Id, cancellationToken);

        var now = DateTime.UtcNow;
        var targetGuestCount = Math.Max(1, targetTable.GuestCount);
        var sourceGuestCount = Math.Max(1, sourceTable.GuestCount);
        var lineNo = order.Lines.Count == 0 ? 1 : order.Lines.Max(x => x.LineNo) + 1;
        var copiedCount = 0;

        foreach (var source in sourceLines)
        {
            var pricingRule = source.PricingRule ?? "Manual";
            if (string.Equals(pricingRule, "PerRoom", StringComparison.OrdinalIgnoreCase))
                continue;

            var qty = source.Qty;
            if (scaleByGuestCount && string.Equals(pricingRule, "PerPerson", StringComparison.OrdinalIgnoreCase))
            {
                qty = Math.Max(1, targetGuestCount);
            }

            var existing = order.Lines.FirstOrDefault(x =>
                x.RestaurantOrderTableId == targetOrderTableId
                && x.MenuItemId == source.MenuItemId
                && x.LineStatus != "Cancelled"
                && x.KitchenStatus != "Sent");

            if (existing is not null)
            {
                existing.Qty += qty;
                Recalculate(existing);
                existing.UpdatedAt = now;
            }
            else
            {
                var newLine = new AlacarteOrderLine
                {
                    Id = Guid.NewGuid(),
                    AlacarteOrderId = order.Id,
                    RestaurantOrderTableId = targetOrderTableId,
                    MenuItemId = source.MenuItemId,
                    LineNo = lineNo++,
                    MenuItemCode = source.MenuItemCode,
                    MenuItemName = source.MenuItemName,
                    UnitName = source.UnitName,
                    Qty = qty,
                    UnitPrice = source.UnitPrice,
                    VatRate = source.VatRate,
                    PricingRule = pricingRule,
                    LineType = "Normal",
                    KitchenStatus = "New",
                    LineStatus = "Ordered",
                    CreatedAt = now,
                    UpdatedAt = now
                };
                Recalculate(newLine);
                order.Lines.Add(newLine);
                _dbContext.AlacarteOrderLines.Add(newLine);
            }
            copiedCount++;
        }

        Recalculate(order);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return await LoadOrderDtoAsync(order.Id, cancellationToken);
    }

    private async Task<RestaurantPreparationTicket> CreatePreparationTicketAsync(AlacarteOrder order, IReadOnlyList<AlacarteOrderLine> lines, string ticketType, string? sentBy, CancellationToken cancellationToken, string? station = null, string? note = null)
    {
        var now = DateTime.UtcNow;
        var resolvedStation = station ?? (lines.Any(x => IsBar(x.MenuItem?.PreparationArea)) ? "Bar" : "Kitchen");
        var ticket = new RestaurantPreparationTicket
        {
            Id = Guid.NewGuid(),
            AlacarteOrderId = order.Id,
            TicketNo = await GenerateTicketNoAsync(now.Date, cancellationToken, ticketType == "Cancel" ? "ACN" : "AKT"),
            Station = resolvedStation,
            TicketType = ticketType,
            Status = ticketType == "Cancel" ? "Cancelled" : "Sent",
            SentAt = now,
            SentBy = TrimOrNull(sentBy),
            Note = TrimOrNull(note)
        };
        var lineNo = 1;
        foreach (var line in lines)
        {
            ticket.Lines.Add(new RestaurantPreparationTicketLine
            {
                Id = Guid.NewGuid(),
                RestaurantPreparationTicketId = ticket.Id,
                AlacarteOrderLineId = line.Id,
                LineNo = lineNo++,
                MenuItemName = line.MenuItemName,
                Qty = line.Qty + line.SupplementQuantity,
                KitchenNote = TrimOrNull(string.Join(" | ", new[] { line.KitchenNote, ExtractSupplementDisplayNote(line.SupplementNote) }.Where(x => !string.IsNullOrWhiteSpace(x))))
            });
        }
        order.PreparationTickets.Add(ticket);
        _dbContext.RestaurantPreparationTickets.Add(ticket);
        return ticket;
    }

    private async Task<RestaurantPreparationTicket> CreatePreparationTicketAsync(
        AlacarteOrder order,
        IReadOnlyList<PreparationTicketDraftLine> lines,
        string ticketType,
        string? sentBy,
        CancellationToken cancellationToken,
        string? station = null,
        string? note = null)
    {
        var now = DateTime.UtcNow;
        var resolvedStation = station ?? (lines.Any(x => string.Equals(x.Station, "Bar", StringComparison.OrdinalIgnoreCase)) ? "Bar" : "Kitchen");
        var ticket = new RestaurantPreparationTicket
        {
            Id = Guid.NewGuid(),
            AlacarteOrderId = order.Id,
            TicketNo = await GenerateTicketNoAsync(now.Date, cancellationToken, ticketType == "Cancel" ? "ACN" : "AKT"),
            Station = resolvedStation,
            TicketType = ticketType,
            Status = ticketType == "Cancel" ? "Cancelled" : "Sent",
            SentAt = now,
            SentBy = TrimOrNull(sentBy),
            Note = TrimOrNull(note)
        };

        var lineNo = 1;
        foreach (var line in lines.Where(x => x.Qty > 0))
        {
            ticket.Lines.Add(new RestaurantPreparationTicketLine
            {
                Id = Guid.NewGuid(),
                RestaurantPreparationTicketId = ticket.Id,
                AlacarteOrderLineId = line.OrderLineId,
                LineNo = lineNo++,
                MenuItemName = line.MenuItemName,
                Qty = line.Qty,
                KitchenNote = TrimOrNull(line.KitchenNote)
            });
        }

        order.PreparationTickets.Add(ticket);
        _dbContext.RestaurantPreparationTickets.Add(ticket);
        return ticket;
    }

    private async Task ApplySentLineAdjustmentAsync(
        AlacarteOrder order,
        AlacarteOrderLine line,
        SaveAlacarteOrderLineRequest request,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var normalizedKitchenNote = TrimOrNull(request.KitchenNote);
        var normalizedSupplementNote = NormalizeSupplementNote(request.SupplementNote);
        var requestedQty = Math.Max(0, request.Qty);
        var requestedSupplementQuantity = Math.Max(0, request.SupplementQuantity);
        var currentEffectiveQuantity = Math.Max(0, line.Qty + line.SupplementQuantity);
        var requestedEffectiveQuantity = Math.Max(0, requestedQty + requestedSupplementQuantity);
        var station = IsBar(line.MenuItem?.PreparationArea) ? "Bar" : "Kitchen";
        var sentBy = TrimOrNull(request.AdjustmentBy)
            ?? (string.IsNullOrWhiteSpace(order.CreatedBy) ? "system" : order.CreatedBy);
        var adjustmentReason = TrimOrNull(request.AdjustmentReason);

        if (requestedEffectiveQuantity > currentEffectiveQuantity)
        {
            var increaseQuantity = requestedEffectiveQuantity - currentEffectiveQuantity;
            await CreatePreparationTicketAsync(
                order,
                [
                    new PreparationTicketDraftLine
                    {
                        OrderLineId = line.Id,
                        MenuItemName = line.MenuItemName,
                        Qty = increaseQuantity,
                        Station = station,
                        KitchenNote = BuildAdjustmentKitchenNote(
                            line.MenuItemName,
                            currentEffectiveQuantity,
                            requestedEffectiveQuantity,
                            normalizedKitchenNote,
                            normalizedSupplementNote,
                            adjustmentReason,
                            isDecrease: false)
                    }
                ],
                "Send",
                sentBy,
                cancellationToken,
                station,
                BuildAdjustmentTicketTitle(line.MenuItemName, adjustmentReason, isDecrease: false));

            await CreateConsumptionIssuesAsync(
                order,
                [
                    BuildAdjustmentShadowLine(line, increaseQuantity, normalizedSupplementNote)
                ],
                now,
                sentBy,
                cancellationToken);
        }
        else if (requestedEffectiveQuantity < currentEffectiveQuantity)
        {
            var decreaseQuantity = currentEffectiveQuantity - requestedEffectiveQuantity;
            await CreatePreparationTicketAsync(
                order,
                [
                    new PreparationTicketDraftLine
                    {
                        OrderLineId = line.Id,
                        MenuItemName = line.MenuItemName,
                        Qty = decreaseQuantity,
                        Station = station,
                        KitchenNote = BuildAdjustmentKitchenNote(
                            line.MenuItemName,
                            currentEffectiveQuantity,
                            requestedEffectiveQuantity,
                            normalizedKitchenNote,
                            normalizedSupplementNote,
                            adjustmentReason,
                            isDecrease: true)
                    }
                ],
                "Cancel",
                sentBy,
                cancellationToken,
                station,
                BuildAdjustmentTicketTitle(line.MenuItemName, adjustmentReason, isDecrease: true));
        }

        line.Qty = requestedQty;
        line.SupplementQuantity = requestedSupplementQuantity;
        line.KitchenNote = normalizedKitchenNote;
        line.SupplementNote = normalizedSupplementNote;
        line.UpdatedAt = now;
        order.Note = AppendNote(order.Note, BuildAdjustmentAuditNote(line.MenuItemName, currentEffectiveQuantity, requestedEffectiveQuantity, adjustmentReason, sentBy));
        order.UpdatedAt = now;
        Recalculate(line);
    }

    private static AlacarteOrderLine BuildAdjustmentShadowLine(AlacarteOrderLine sourceLine, decimal effectiveQuantityDelta, string? normalizedSupplementNote)
        => new()
        {
            Id = Guid.NewGuid(),
            AlacarteOrderId = sourceLine.AlacarteOrderId,
            RestaurantOrderTableId = sourceLine.RestaurantOrderTableId,
            MenuItemId = sourceLine.MenuItemId,
            MenuItem = sourceLine.MenuItem,
            MenuItemCode = sourceLine.MenuItemCode,
            MenuItemName = sourceLine.MenuItemName,
            UnitName = sourceLine.UnitName,
            Qty = effectiveQuantityDelta,
            SupplementQuantity = 0,
            UnitPrice = sourceLine.UnitPrice,
            VatRate = sourceLine.VatRate,
            KitchenNote = sourceLine.KitchenNote,
            SupplementNote = normalizedSupplementNote,
            LineStatus = "Ordered",
            KitchenStatus = "Sent",
            LineType = "Adjustment",
            PricingRule = sourceLine.PricingRule,
            IsSupplement = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

    private static string BuildAdjustmentKitchenNote(
        string menuItemName,
        decimal currentQuantity,
        decimal requestedQuantity,
        string? kitchenNote,
        string? supplementNote,
        string? adjustmentReason,
        bool isDecrease)
    {
        var directionText = isDecrease ? "\u0110i\u1ec1u ch\u1ec9nh gi\u1ea3m" : "\u0110i\u1ec1u ch\u1ec9nh t\u0103ng";
        var baseText = $"{directionText}: {menuItemName} t\u1eeb x{currentQuantity:0.##} th\u00e0nh x{requestedQuantity:0.##}.";
        return string.Join(" | ", new[]
        {
            baseText,
            string.IsNullOrWhiteSpace(adjustmentReason) ? null : $"L\u00fd do: {adjustmentReason}",
            kitchenNote,
            ExtractSupplementDisplayNote(supplementNote)
        }.Where(x => !string.IsNullOrWhiteSpace(x)));
    }

    private static string BuildAdjustmentTicketTitle(string menuItemName, string? adjustmentReason, bool isDecrease)
    {
        var prefix = isDecrease ? "\u0110i\u1ec1u ch\u1ec9nh gi\u1ea3m m\u00f3n" : "\u0110i\u1ec1u ch\u1ec9nh t\u0103ng m\u00f3n";
        return string.IsNullOrWhiteSpace(adjustmentReason)
            ? $"{prefix}: {menuItemName}"
            : $"{prefix}: {menuItemName} | L\u00fd do: {adjustmentReason}";
    }

    private static string BuildAdjustmentAuditNote(string menuItemName, decimal currentQuantity, decimal requestedQuantity, string? adjustmentReason, string updatedBy)
    {
        var baseText = $"S\u1eeda m\u00f3n \u0111\u00e3 g\u1eedi b\u1ebfp/bar: {menuItemName} x{currentQuantity:0.##} -> x{requestedQuantity:0.##} b\u1edfi {updatedBy}.";
        return string.IsNullOrWhiteSpace(adjustmentReason)
            ? baseText
            : $"{baseText} L\u00fd do: {adjustmentReason}";
    }

    private async Task<AlacarteOrder> LoadOrderEntityAsync(Guid id, CancellationToken cancellationToken)
        => await _dbContext.AlacarteOrders.Include(x => x.Table).ThenInclude(x => x!.Room)
            .Include(x => x.Room)
            .Include(x => x.Quote)
            .Include(x => x.Reservation).ThenInclude(x => x!.Room)
            .Include(x => x.Reservation).ThenInclude(x => x!.Table)
            .Include(x => x.OrderTables)
            .Include(x => x.ServingGroups).ThenInclude(x => x.Tables).ThenInclude(x => x.OrderTable)
            .Include(x => x.Lines).ThenInclude(x => x.MenuItem)
            .Include(x => x.ExtraServices)
            .Include(x => x.PreparationTickets).ThenInclude(x => x.Lines)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken) ?? throw new InvalidOperationException("Khong tim thay order A La Carte.");

    private async Task<AlacarteOrderDto> LoadOrderDtoAsync(Guid id, CancellationToken cancellationToken)
        => ToDto(await _dbContext.AlacarteOrders.AsNoTracking().Include(x => x.Table).ThenInclude(x => x!.Room)
            .Include(x => x.Room)
            .Include(x => x.Quote)
            .Include(x => x.Reservation).ThenInclude(x => x!.Room)
            .Include(x => x.Reservation).ThenInclude(x => x!.Table)
            .Include(x => x.OrderTables.OrderBy(t => t.SortOrder))
            .Include(x => x.ServingGroups).ThenInclude(x => x.Tables).ThenInclude(x => x.OrderTable)
            .Include(x => x.Lines.OrderBy(l => l.LineNo)).ThenInclude(x => x.MenuItem)
            .Include(x => x.ExtraServices)
            .FirstAsync(x => x.Id == id, cancellationToken));

    private async Task<TableReservationDto> LoadReservationDtoAsync(Guid id, CancellationToken cancellationToken)
        => ToDto(await _dbContext.TableReservations.AsNoTracking().Include(x => x.Room).Include(x => x.Table).FirstAsync(x => x.Id == id, cancellationToken));

    private async Task<RestaurantQuoteDto> LoadQuoteDtoAsync(Guid id, CancellationToken cancellationToken)
        => ToDto(await _dbContext.RestaurantQuotes.AsNoTracking()
            .Include(x => x.QuoteTables.OrderBy(t => t.SortOrder))
            .Include(x => x.Lines.OrderBy(l => l.LineNo))
            .FirstAsync(x => x.Id == id, cancellationToken));

    private async Task<AlacarteOrder> CreateOrderForQuoteAsync(RestaurantQuote quote, Guid reservationId, string? approvedBy, CancellationToken cancellationToken)
    {
        var table = quote.DiningTableId.HasValue
            ? await _dbContext.DiningTables.Include(x => x.Room).FirstOrDefaultAsync(x => x.Id == quote.DiningTableId.Value, cancellationToken)
            : null;
        var room = quote.DiningRoomId.HasValue
            ? await _dbContext.DiningRooms.FirstOrDefaultAsync(x => x.Id == quote.DiningRoomId.Value, cancellationToken)
            : table?.Room;
        var now = DateTime.UtcNow;
        var scheduledStart = BuildReservationDateTime(quote.EventDate, quote.StartTime);
        var shouldStartServing = scheduledStart <= DateTime.Now;
        var order = new AlacarteOrder
        {
            Id = Guid.NewGuid(),
            DiningTableId = quote.DiningTableId,
            DiningRoomId = quote.DiningRoomId ?? table?.DiningRoomId,
            RestaurantQuoteId = quote.Id,
            ReservationId = reservationId,
            OrderNo = await GenerateOrderNoAsync(DateTime.Today, cancellationToken),
            OrderDate = DateTime.Now,
            OrderType = "Advance",
            ActualGuestCount = quote.GuestCount,
            Status = ResolveApprovedQuoteOrderStatus(scheduledStart),
            PaymentStatus = quote.DepositAmount > 0 ? "Deposit" : "Unpaid",
            DiscountAmount = quote.DiscountAmount,
            Note = $"Tu bao gia {quote.QuoteNo}",
            CreatedBy = string.IsNullOrWhiteSpace(approvedBy) ? "system" : approvedBy.Trim(),
            CreatedAt = now,
            UpdatedAt = now
        };
        _dbContext.AlacarteOrders.Add(order);

        if (table is not null)
        {
            table.Status = shouldStartServing ? "Serving" : "Reserved";
            table.UpdatedAt = now;
        }

        if (room is not null)
        {
            room.Status = shouldStartServing ? "Occupied" : "Reserved";
            room.UpdatedAt = now;
        }

        foreach (var quoteTable in quote.QuoteTables.OrderBy(x => x.SortOrder))
        {
            var orderTable = new RestaurantOrderTable
            {
                Id = Guid.NewGuid(),
                AlacarteOrderId = order.Id,
                RestaurantQuoteTableId = quoteTable.Id,
                TableNo = quoteTable.TableNo,
                DisplayName = quoteTable.DisplayName,
                GuestCount = quoteTable.GuestCount,
                SortOrder = quoteTable.SortOrder,
                IsVirtual = quoteTable.IsVirtual,
                Status = quoteTable.IsVirtual || shouldStartServing ? "Serving" : "Waiting",
                Note = quoteTable.Note,
                CreatedAt = now,
                UpdatedAt = now
            };
            order.OrderTables.Add(orderTable);
        }

        var virtualTable = order.OrderTables.FirstOrDefault(x => x.IsVirtual);
        var realTables = order.OrderTables.Where(x => !x.IsVirtual).OrderBy(x => x.SortOrder).ToList();

        var lineNo = 1;
        foreach (var quoteLine in quote.Lines.OrderBy(x => x.LineNo))
        {
            if (IsQuoteServiceLine(quoteLine.LineType))
            {
                order.ExtraServices.Add(new RestaurantOrderExtraService
                {
                    Id = Guid.NewGuid(),
                    AlacarteOrderId = order.Id,
                    Description = quoteLine.Name,
                    Qty = quoteLine.CalculatedQuantity > 0 ? quoteLine.CalculatedQuantity : Math.Max(1, quoteLine.Quantity),
                    UnitPrice = quoteLine.UnitPrice,
                    Amount = quoteLine.Total,
                    VatRate = 0
                });
                continue;
            }

            var menuItem = quoteLine.MenuItemId.HasValue
                ? await _dbContext.MenuItems.AsNoTracking().FirstOrDefaultAsync(x => x.Id == quoteLine.MenuItemId.Value, cancellationToken)
                : await _dbContext.MenuItems.AsNoTracking().FirstOrDefaultAsync(x => x.Name == quoteLine.Name, cancellationToken);
            if (menuItem is null)
            {
                throw new InvalidOperationException($"Khong tim thay mon an '{quoteLine.Name}' de tao don mo tu bao gia.");
            }

            var pricingRule = NormalizePricingRule(quoteLine.PricingRule, false);
            if (string.Equals(pricingRule, "PerPerson", StringComparison.OrdinalIgnoreCase))
            {
                foreach (var servingTable in realTables)
                {
                    var qty = Math.Max(1, servingTable.GuestCount) * Math.Max(1, quoteLine.Quantity);
                    AddOrderLine(order, servingTable, menuItem, quote, pricingRule, qty, ref lineNo, now);
                }
                continue;
            }

            if (string.Equals(pricingRule, "PerTable", StringComparison.OrdinalIgnoreCase))
            {
                foreach (var servingTable in realTables)
                {
                    AddOrderLine(order, servingTable, menuItem, quote, pricingRule, Math.Max(1, quoteLine.Quantity), ref lineNo, now);
                }
                continue;
            }

            AddOrderLine(order, virtualTable, menuItem, quote, pricingRule, quoteLine.CalculatedQuantity > 0 ? quoteLine.CalculatedQuantity : Math.Max(1, quoteLine.Quantity), ref lineNo, now);
        }

        Recalculate(order);
        return order;
    }

    private void AddOrderLine(AlacarteOrder order, RestaurantOrderTable? orderTable, MenuItem menuItem, RestaurantQuote quote, string pricingRule, decimal qty, ref int lineNo, DateTime now)
    {
        var line = new AlacarteOrderLine
        {
            Id = Guid.NewGuid(),
            AlacarteOrderId = order.Id,
            RestaurantOrderTableId = orderTable?.Id,
            MenuItemId = menuItem.Id,
            LineNo = lineNo++,
            MenuItemCode = menuItem.Code,
            MenuItemName = menuItem.Name,
            UnitName = menuItem.UnitName,
            Qty = qty,
            UnitPrice = menuItem.UnitPrice,
            VatRate = menuItem.VatRate,
            KitchenStatus = "New",
            LineStatus = "Ordered",
            LineType = "Normal",
            PricingRule = pricingRule,
            IsSupplement = false,
            KitchenNote = $"Tu bao gia {quote.QuoteNo}",
            CreatedAt = now,
            UpdatedAt = now
        };
        Recalculate(line);
        order.Lines.Add(line);
        _dbContext.AlacarteOrderLines.Add(line);
    }

    private async Task<List<DiningTable>> EnsureRoomPhysicalTablesAsync(DiningRoom room, CancellationToken cancellationToken)
    {
        var tables = await _dbContext.DiningTables
            .Where(x => x.DiningRoomId == room.Id)
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Code)
            .ToListAsync(cancellationToken);

        var standardSeatCount = 10;
        var expectedTableCount = Math.Max(1, (int)Math.Ceiling(Math.Max(1, room.Capacity) / (double)standardSeatCount));
        if (tables.Count == 1 && expectedTableCount > 1 && tables[0].SeatCount >= room.Capacity)
        {
            tables[0].SeatCount = standardSeatCount;
            tables[0].SortOrder = tables[0].SortOrder <= 0 ? 1 : tables[0].SortOrder;
            tables[0].UpdatedAt = DateTime.UtcNow;
        }

        NormalizeRoomTableSortOrders(tables);

        if (tables.Count >= expectedTableCount)
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
            return tables.OrderBy(x => x.SortOrder).ThenBy(x => x.Code).ToList();
        }

        var existingCodes = tables.Select(x => x.Code).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var codePrefix = new string((room.Code ?? room.Name ?? "PHONG").Where(char.IsLetterOrDigit).ToArray()).ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(codePrefix))
        {
            codePrefix = "PHONG";
        }

        var now = DateTime.UtcNow;
        for (var index = tables.Count + 1; index <= expectedTableCount; index++)
        {
            var code = $"{codePrefix}-B{index:00}";
            while (existingCodes.Contains(code))
            {
                code = $"{codePrefix}-B{index:00}-{Guid.NewGuid():N}"[..Math.Min(24, $"{codePrefix}-B{index:00}-{Guid.NewGuid():N}".Length)];
            }

            existingCodes.Add(code);
            var table = new DiningTable
            {
                Id = Guid.NewGuid(),
                DiningRoomId = room.Id,
                Code = code,
                Name = $"BÃ n {index:00}",
                SeatCount = standardSeatCount,
                Status = "Empty",
                IsActive = true,
                SortOrder = index,
                CreatedAt = now,
                UpdatedAt = now
            };
            tables.Add(table);
            _dbContext.DiningTables.Add(table);
        }

        NormalizeRoomTableSortOrders(tables);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return tables.OrderBy(x => x.SortOrder).ThenBy(x => x.Code).ToList();
    }

    private async Task SyncServingGroupsAsync(AlacarteOrder order, IReadOnlyList<DiningTable> diningTables, int standardSeats, DateTime now, CancellationToken cancellationToken)
    {
        var existingGroups = await _dbContext.RestaurantServingGroups
            .Include(x => x.Tables)
            .Where(x => x.AlacarteOrderId == order.Id)
            .ToListAsync(cancellationToken);
        var customGroups = existingGroups.Where(x => !IsSystemDefinedServingGroup(x)).ToList();
        var systemGroups = existingGroups.Where(IsSystemDefinedServingGroup).ToList();
        if (systemGroups.Count > 0)
        {
            _dbContext.RestaurantServingGroupTables.RemoveRange(systemGroups.SelectMany(x => x.Tables));
            _dbContext.RestaurantServingGroups.RemoveRange(systemGroups);
        }

        if (customGroups.Count > 0)
        {
            var activeServingTableLookup = order.OrderTables
                .Where(x => !x.IsVirtual && string.Equals(x.Status, "Serving", StringComparison.OrdinalIgnoreCase))
                .ToDictionary(x => x.Id);
            foreach (var customGroup in customGroups)
            {
                var staleEntries = customGroup.Tables
                    .Where(x => !activeServingTableLookup.ContainsKey(x.RestaurantOrderTableId))
                    .ToList();
                if (staleEntries.Count > 0)
                {
                    _dbContext.RestaurantServingGroupTables.RemoveRange(staleEntries);
                    foreach (var stale in staleEntries)
                    {
                        customGroup.Tables.Remove(stale);
                    }
                }

                if (customGroup.Tables.Count == 0)
                {
                    _dbContext.RestaurantServingGroups.Remove(customGroup);
                    continue;
                }

                foreach (var tableEntry in customGroup.Tables)
                {
                    if (!activeServingTableLookup.TryGetValue(tableEntry.RestaurantOrderTableId, out var activeOrderTable))
                    {
                        continue;
                    }

                    tableEntry.GuestCount = Math.Max(0, activeOrderTable.GuestCount);
                    tableEntry.IsServing = true;
                    tableEntry.UpdatedAt = now;
                }

                customGroup.UpdatedAt = now;
            }
        }

        var diningTableLookup = diningTables.ToDictionary(x => x.Id);
        var physicalOrderTables = order.OrderTables
            .Where(x => !x.IsVirtual)
            .OrderBy(x => x.SortOrder)
            .ToList();

        var servingTables = physicalOrderTables
            .Where(x => string.Equals(x.Status, "Serving", StringComparison.OrdinalIgnoreCase))
            .ToList();
        var waitingTables = physicalOrderTables
            .Where(x => !string.Equals(x.Status, "Serving", StringComparison.OrdinalIgnoreCase))
            .ToList();
        var standardServingTables = servingTables.Where(x => x.GuestCount == standardSeats).ToList();
        var oddServingTables = servingTables.Where(x => x.GuestCount != standardSeats).ToList();

        var groups = new List<(string Code, string Name, string ScopeType, string ServingRule, string? Note, List<RestaurantOrderTable> Tables)>
        {
            ("ALL-SERVING", "Nhóm phục vụ tổng", "Serving", "PerTable", $"Tự động đồng bộ từ cấu hình phòng: {servingTables.Count} bàn đang phục vụ.", servingTables),
            ("STANDARD", "Nhóm bàn chuẩn", "StandardOnly", "PerTable", $"Bàn phục vụ đúng chuẩn {standardSeats} khách.", standardServingTables),
            ("ODD", "Nhóm bàn lẻ", "OddOnly", "PerTableWithExtraUnit", "Bàn vượt hoặc thiếu chuẩn, có thể cần phần bù định lượng.", oddServingTables),
            ("WAITING", "Bàn chờ", "Waiting", "Manual", "Bàn vật lý đã sẵn sàng nhưng chưa mở phục vụ.", waitingTables)
        };

        var sortOrder = 1;
        foreach (var groupDefinition in groups.Where(x => x.Tables.Count > 0))
        {
            var group = new RestaurantServingGroup
            {
                Id = Guid.NewGuid(),
                AlacarteOrderId = order.Id,
                Code = groupDefinition.Code,
                Name = groupDefinition.Name,
                ScopeType = groupDefinition.ScopeType,
                ServingRule = groupDefinition.ServingRule,
                SortOrder = sortOrder++,
                Note = groupDefinition.Note,
                CreatedAt = now,
                UpdatedAt = now
            };

            foreach (var orderTable in groupDefinition.Tables)
            {
                var baseSeatCount = orderTable.DiningTableId.HasValue && diningTableLookup.TryGetValue(orderTable.DiningTableId.Value, out var diningTable)
                    ? Math.Max(1, diningTable.SeatCount)
                    : standardSeats;
                group.Tables.Add(new RestaurantServingGroupTable
                {
                    Id = Guid.NewGuid(),
                    RestaurantServingGroupId = group.Id,
                    RestaurantOrderTableId = orderTable.Id,
                    DiningTableId = orderTable.DiningTableId,
                    GuestCount = Math.Max(0, orderTable.GuestCount),
                    ExtraGuestCount = Math.Max(0, orderTable.GuestCount - baseSeatCount),
                    IsServing = string.Equals(orderTable.Status, "Serving", StringComparison.OrdinalIgnoreCase),
                    CreatedAt = now,
                    UpdatedAt = now
                });
            }

            _dbContext.RestaurantServingGroups.Add(group);
        }
    }

    private static bool IsSystemDefinedServingGroup(RestaurantServingGroup group)
        => SystemServingGroupCodes.Contains(group.Code, StringComparer.OrdinalIgnoreCase)
           || (!string.Equals(group.ScopeType, "Custom", StringComparison.OrdinalIgnoreCase)
               && !string.Equals(group.ScopeType, "ManualGroup", StringComparison.OrdinalIgnoreCase));

    private static int GetNextCustomServingGroupSortOrder(IEnumerable<RestaurantServingGroup> groups)
        => Math.Max(100, groups.Where(x => !IsSystemDefinedServingGroup(x)).Select(x => x.SortOrder).DefaultIfEmpty(99).Max() + 1);

    private static string BuildCustomServingGroupCode(IEnumerable<RestaurantServingGroup> groups)
    {
        var nextIndex = groups
            .Where(x => !IsSystemDefinedServingGroup(x))
            .Select(x =>
            {
                var suffix = x.Code?.Split('-', StringSplitOptions.RemoveEmptyEntries).LastOrDefault();
                return int.TryParse(suffix, out var parsed) ? parsed : 0;
            })
            .DefaultIfEmpty()
            .Max() + 1;
        return $"CUSTOM-{nextIndex:00}";
    }

    private async Task ReconcileReservationStatusesAsync(CancellationToken cancellationToken)
    {
        var staleReservations = await _dbContext.TableReservations
            .Where(x => x.Status != "Completed" && x.Status != "Cancelled" && x.Status != "NoShow")
            .Join(
                _dbContext.AlacarteOrders,
                reservation => reservation.Id,
                order => order.ReservationId ?? Guid.Empty,
                (reservation, order) => new { Reservation = reservation, Order = order })
            .Where(x => x.Order.Status == "Paid" || x.Order.Status == "Cancelled")
            .ToListAsync(cancellationToken);

        if (staleReservations.Count == 0)
        {
            return;
        }

        var now = DateTime.UtcNow;
        foreach (var item in staleReservations)
        {
            item.Reservation.Status = item.Order.Status == "Paid" ? "Completed" : "Cancelled";
            item.Reservation.UpdatedAt = now;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task CreateConsumptionIssuesAsync(AlacarteOrder order, IReadOnlyList<AlacarteOrderLine> lines, DateTime now, string? sentBy, CancellationToken cancellationToken)
    {
        var menuItemIds = lines.Select(x => x.MenuItemId).Distinct().ToList();
        var recipes = await _dbContext.MenuItemRecipes
            .AsNoTracking()
            .Include(x => x.Lines)
            .Where(x => menuItemIds.Contains(x.MenuItemId) && x.IsDefault && x.IsActive)
            .ToDictionaryAsync(x => x.MenuItemId, cancellationToken);
        if (recipes.Count == 0)
        {
            return;
        }

        var warehouses = await _dbContext.Warehouses
            .AsNoTracking()
            .Where(x => x.IsActive)
            .ToListAsync(cancellationToken);
        if (warehouses.Count == 0)
        {
            return;
        }

        Warehouse? ResolveDefaultWarehouse(string station, int? warehouseId)
        {
            if (warehouseId.HasValue)
            {
                var exact = warehouses.FirstOrDefault(x => x.Id == warehouseId.Value);
                if (exact is not null) return exact;
            }

            var preferredCode = string.Equals(station, "Bar", StringComparison.OrdinalIgnoreCase) ? "KHO-BAR" : "KHO-BEP";
            return warehouses.FirstOrDefault(x => string.Equals(x.WarehouseCode, preferredCode, StringComparison.OrdinalIgnoreCase))
                   ?? warehouses.FirstOrDefault();
        }

        var supplementMetadataByLineId = lines.ToDictionary(
            x => x.Id,
            x => ParseSupplementMetadata(x.SupplementNote));
        var substituteItemIds = supplementMetadataByLineId.Values
            .Where(x => x is not null)
            .SelectMany(x => x!.IngredientAdjustments)
            .Where(x => x.SelectedInventoryItemId.HasValue)
            .Select(x => x.SelectedInventoryItemId!.Value)
            .Distinct()
            .ToList();
        var substituteItems = substituteItemIds.Count == 0
            ? new Dictionary<int, Item>()
            : await _dbContext.Items
                .AsNoTracking()
                .Where(x => substituteItemIds.Contains(x.Id))
                .ToDictionaryAsync(x => x.Id, cancellationToken);

        var issueGroups = new Dictionary<(string Station, int WarehouseId), List<(AlacarteOrderLine Line, MenuItemRecipeLine RecipeLine, int ItemId, string UnitName, decimal Quantity, Warehouse Warehouse)>>();
        foreach (var line in lines)
        {
            if (!recipes.TryGetValue(line.MenuItemId, out var recipe))
            {
                continue;
            }

            var station = IsBar(line.MenuItem?.PreparationArea) ? "Bar" : "Kitchen";
            supplementMetadataByLineId.TryGetValue(line.Id, out var supplementMetadata);
            foreach (var recipeLine in recipe.Lines.Where(x => x.ItemId.HasValue && x.Quantity > 0))
            {
                var warehouse = ResolveDefaultWarehouse(station, recipeLine.DefaultWarehouseId);
                if (warehouse is null)
                {
                    continue;
                }

                var effectiveQuantity = Math.Max(0, line.Qty + line.SupplementQuantity);
                if (effectiveQuantity <= 0)
                {
                    continue;
                }

                var componentQuantity = effectiveQuantity * recipeLine.Quantity * (1 + Math.Max(0, recipeLine.LossRate));
                var adjustment = supplementMetadata?.IngredientAdjustments.FirstOrDefault(x => x.RecipeLineId == recipeLine.Id);
                var selectedItemId = recipeLine.ItemId!.Value;
                var selectedUnitName = recipeLine.UnitName;
                var conversionRate = 1m;
                if (adjustment?.SelectedInventoryItemId is int substituteItemId
                    && substituteItems.TryGetValue(substituteItemId, out var substituteItem))
                {
                    selectedItemId = substituteItem.Id;
                    selectedUnitName = string.IsNullOrWhiteSpace(substituteItem.Unit) ? recipeLine.UnitName : substituteItem.Unit;
                    conversionRate = Math.Max(0.0001m, adjustment.ConversionRate <= 0 ? 1m : adjustment.ConversionRate);
                }

                componentQuantity = componentQuantity * conversionRate + Math.Max(0, adjustment?.ExtraQuantity ?? 0);
                if (componentQuantity <= 0)
                {
                    continue;
                }

                var key = (station, warehouse.Id);
                if (!issueGroups.TryGetValue(key, out var entries))
                {
                    entries = [];
                    issueGroups[key] = entries;
                }

                entries.Add((line, recipeLine, selectedItemId, selectedUnitName, componentQuantity, warehouse));
            }
        }

        foreach (var issueGroup in issueGroups)
        {
            var issue = new StockIssue
            {
                IssueNo = await GenerateRestaurantStockIssueNoAsync(now.Date, issueGroup.Key.Station, cancellationToken),
                IssueDate = now,
                Description = $"Xuáº¥t tiÃªu hao A La Carte {order.OrderNo} - {issueGroup.Key.Station}",
                WarehouseId = issueGroup.Key.WarehouseId,
                DepartmentCode = "NH",
                DebitAccountCode = "632",
                CreditAccountCode = "156",
                PostingStatus = "Draft",
                IsApproved = false,
                PostedBy = string.IsNullOrWhiteSpace(sentBy) ? "system" : sentBy.Trim()
            };

            var detailLineNo = 1;
            foreach (var recipeGroup in issueGroup.Value
                         .GroupBy(x => new { x.ItemId, x.UnitName, x.Warehouse.Id, x.Line.MenuItemName, SupplementNote = ExtractSupplementDisplayNote(x.Line.SupplementNote) }))
            {
                var sample = recipeGroup.First();
                var quantity = recipeGroup.Sum(x => x.Quantity);
                issue.Details.Add(new StockIssueDetail
                {
                    LineNo = detailLineNo++,
                    ItemId = sample.ItemId,
                    Unit = sample.UnitName,
                    WarehouseId = sample.Warehouse.Id,
                    Quantity = quantity,
                    UnitPrice = 0,
                    Amount = 0
                });
            }

            issue.TotalAmount = 0;
            _dbContext.StockIssues.Add(issue);
        }
    }

    private static string? NormalizeSupplementNote(string? supplementNote)
    {
        if (string.IsNullOrWhiteSpace(supplementNote))
        {
            return null;
        }

        var trimmed = supplementNote.Trim();
        if (!trimmed.Contains(SupplementMetadataMarker, StringComparison.Ordinal))
        {
            return trimmed;
        }

        var metadata = ParseSupplementMetadata(trimmed);
        if (metadata is null)
        {
            return ExtractSupplementDisplayNote(trimmed);
        }

        metadata.DisplayNote = ExtractSupplementDisplayNote(trimmed);
        return SerializeSupplementMetadata(metadata);
    }

    private static string? ExtractSupplementDisplayNote(string? supplementNote)
    {
        if (string.IsNullOrWhiteSpace(supplementNote))
        {
            return null;
        }

        var markerIndex = supplementNote.IndexOf(SupplementMetadataMarker, StringComparison.Ordinal);
        var displayNote = markerIndex >= 0
            ? supplementNote[..markerIndex]
            : supplementNote;
        displayNote = displayNote.Trim();
        return string.IsNullOrWhiteSpace(displayNote) ? null : displayNote;
    }

    private static RestaurantLineSupplementMetadata? ParseSupplementMetadata(string? supplementNote)
    {
        if (string.IsNullOrWhiteSpace(supplementNote))
        {
            return null;
        }

        var markerIndex = supplementNote.IndexOf(SupplementMetadataMarker, StringComparison.Ordinal);
        if (markerIndex < 0)
        {
            return null;
        }

        var payload = supplementNote[(markerIndex + SupplementMetadataMarker.Length)..].Trim();
        if (string.IsNullOrWhiteSpace(payload))
        {
            return null;
        }

        try
        {
            var json = Encoding.UTF8.GetString(Convert.FromBase64String(payload));
            var metadata = JsonSerializer.Deserialize<RestaurantLineSupplementMetadata>(json);
            if (metadata is null)
            {
                return null;
            }

            metadata.DisplayNote = ExtractSupplementDisplayNote(supplementNote);
            return metadata;
        }
        catch
        {
            return null;
        }
    }

    private static string SerializeSupplementMetadata(RestaurantLineSupplementMetadata metadata)
    {
        metadata.IngredientAdjustments = metadata.IngredientAdjustments
            .Where(x => x.RecipeLineId != Guid.Empty
                        && (x.SelectedInventoryItemId.HasValue || x.ExtraQuantity > 0 || !string.IsNullOrWhiteSpace(x.Note)))
            .ToList();
        var json = JsonSerializer.Serialize(metadata);
        var encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes(json));
        var display = string.IsNullOrWhiteSpace(metadata.DisplayNote) ? string.Empty : metadata.DisplayNote.Trim();
        return string.IsNullOrWhiteSpace(display)
            ? $"{SupplementMetadataMarker}{encoded}"
            : $"{display}{Environment.NewLine}{SupplementMetadataMarker}{encoded}";
    }

    private static void NormalizeRoomTableSortOrders(IReadOnlyList<DiningTable> tables)
    {
        for (var index = 0; index < tables.Count; index++)
        {
            var expectedSortOrder = index + 1;
            if (tables[index].SortOrder != expectedSortOrder)
            {
                tables[index].SortOrder = expectedSortOrder;
                tables[index].UpdatedAt = DateTime.UtcNow;
            }
        }
    }

    private static List<int> BuildServingGuestAllocations(int guestCount, int tableCount, int seatPerTable)
    {
        var allocations = new List<int>();
        var remainingGuests = Math.Max(1, guestCount);
        var remainingTables = Math.Max(1, tableCount);
        var maxSeats = Math.Max(1, seatPerTable);
        var softMaxSeats = maxSeats + 2;

        while (remainingTables > 0)
        {
            var suggested = (int)Math.Ceiling(remainingGuests / (double)remainingTables);
            var allocated = Math.Min(softMaxSeats, Math.Max(1, suggested));
            allocations.Add(allocated);
            remainingGuests = Math.Max(0, remainingGuests - allocated);
            remainingTables--;
        }

        return allocations;
    }

    private static string NormalizePricingRule(string? pricingRule, bool isServiceLine)
    {
        if (string.Equals(pricingRule, "PerPerson", StringComparison.OrdinalIgnoreCase)) return "PerPerson";
        if (string.Equals(pricingRule, "PerTable", StringComparison.OrdinalIgnoreCase)) return "PerTable";
        if (string.Equals(pricingRule, "PerRoom", StringComparison.OrdinalIgnoreCase)) return "PerRoom";
        return isServiceLine ? "PerRoom" : "Manual";
    }

    private Task<string> GenerateReservationNoAsync(DateTime date, CancellationToken cancellationToken)
        => GenerateDocumentNoAsync(
            "RSV",
            date,
            _dbContext.TableReservations.Select(x => x.ReservationNo),
            cancellationToken);

    private Task<string> GenerateOrderNoAsync(DateTime date, CancellationToken cancellationToken)
        => GenerateDocumentNoAsync(
            "ALA",
            date,
            _dbContext.AlacarteOrders.Select(x => x.OrderNo),
            cancellationToken);

    private Task<string> GenerateQuoteNoAsync(DateTime date, CancellationToken cancellationToken)
        => GenerateDocumentNoAsync(
            "BG",
            date,
            _dbContext.RestaurantQuotes.Select(x => x.QuoteNo),
            cancellationToken);

    private async Task<string> GenerateTicketNoAsync(DateTime date, CancellationToken cancellationToken, string prefix)
        => $"{prefix}-{date:yyyyMMdd}-{await _dbContext.RestaurantPreparationTickets.CountAsync(x => x.SentAt.Date == date.Date, cancellationToken) + 1:0000}";

    private async Task<string> GenerateRestaurantStockIssueNoAsync(DateTime date, string station, CancellationToken cancellationToken)
    {
        var stationCode = string.Equals(station, "Bar", StringComparison.OrdinalIgnoreCase) ? "BAR" : "BEP";
        var prefix = $"PX-ALC-{stationCode}-{date:yyyyMMdd}";
        var persistedCount = await _dbContext.StockIssues.CountAsync(x => x.IssueDate.Date == date.Date && x.IssueNo.StartsWith(prefix), cancellationToken);
        var pendingCount = _dbContext.ChangeTracker.Entries<StockIssue>()
            .Count(x => x.State == EntityState.Added && x.Entity.IssueNo.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
        return $"{prefix}-{persistedCount + pendingCount + 1:0000}";
    }

    private async Task<string> GenerateDepositNoAsync(DateTime date, CancellationToken cancellationToken)
        => $"RDP-{date:yyyyMMdd}-{await _dbContext.RestaurantDeposits.CountAsync(x => x.Date.Date == date.Date, cancellationToken) + 1:0000}";

    private async Task<string> GenerateContractNoAsync(DateTime date, CancellationToken cancellationToken)
        => $"HD-ALC-{date:yyyyMMdd}-{await _dbContext.RestaurantContracts.CountAsync(x => x.CreatedAt.Date == date.Date, cancellationToken) + 1:0000}";

    private async Task<bool> RoomHasActiveTablesAsync(Guid roomId, CancellationToken cancellationToken)
        => await _dbContext.DiningTables.AsNoTracking().AnyAsync(x => x.DiningRoomId == roomId && (x.Status == "Serving" || x.Status == "Reserved"), cancellationToken);

    private async Task ReleaseOrderTablesAsync(AlacarteOrder order, DateTime now, CancellationToken cancellationToken, bool useCleaningForServingTables)
    {
        var physicalOrderTables = order.OrderTables
            .Where(x => !x.IsVirtual && x.DiningTableId.HasValue)
            .ToList();

        if (physicalOrderTables.Count == 0 && order.Table is not null)
        {
            var hasFutureReservation = await _dbContext.TableReservations.AsNoTracking()
                .AnyAsync(x => x.DiningTableId == order.Table.Id
                               && x.ReservationDateTime >= DateTime.Now
                               && x.Status != "Cancelled"
                               && x.Status != "Completed"
                               && x.Status != "NoShow", cancellationToken);

            order.Table.Status = useCleaningForServingTables ? "Cleaning" : hasFutureReservation ? "Reserved" : "Empty";
            order.Table.UpdatedAt = now;
            order.DiningTableId = null;
            order.ActualGuestCount = 0;

            if (order.Table.Room is not null)
            {
                var hasOtherActiveTables = await _dbContext.DiningTables.AsNoTracking()
                    .AnyAsync(x => x.DiningRoomId == order.Table.DiningRoomId
                                   && x.Id != order.Table.Id
                                   && (x.Status == "Serving" || x.Status == "Reserved"), cancellationToken);
                order.Table.Room.Status = hasOtherActiveTables ? "Occupied" : "Available";
                order.Table.Room.UpdatedAt = now;
            }

            return;
        }

        var physicalTableIds = physicalOrderTables
            .Select(x => x.DiningTableId!.Value)
            .Distinct()
            .ToList();

        var diningTableLookup = physicalTableIds.Count == 0
            ? new Dictionary<Guid, DiningTable>()
            : await _dbContext.DiningTables
                .Include(x => x.Room)
                .Where(x => physicalTableIds.Contains(x.Id))
                .ToDictionaryAsync(x => x.Id, cancellationToken);

        var reservedTableIds = physicalTableIds.Count == 0
            ? new HashSet<Guid>()
            : (await _dbContext.TableReservations.AsNoTracking()
                .Where(x => x.DiningTableId.HasValue
                            && physicalTableIds.Contains(x.DiningTableId.Value)
                            && x.ReservationDateTime >= DateTime.Now
                            && x.Status != "Cancelled"
                            && x.Status != "Completed"
                            && x.Status != "NoShow")
                .Select(x => x.DiningTableId!.Value)
                .Distinct()
                .ToListAsync(cancellationToken))
                .ToHashSet();

        foreach (var orderTable in physicalOrderTables)
        {
            var wasServing = string.Equals(orderTable.Status, "Serving", StringComparison.OrdinalIgnoreCase);
            orderTable.Status = order.Status == "Paid" ? "Completed" : "Cancelled";
            orderTable.GuestCount = 0;
            orderTable.UpdatedAt = now;

            if (!orderTable.DiningTableId.HasValue || !diningTableLookup.TryGetValue(orderTable.DiningTableId.Value, out var diningTable))
            {
                continue;
            }

            diningTable.Status = useCleaningForServingTables && wasServing
                ? "Cleaning"
                : reservedTableIds.Contains(diningTable.Id) ? "Reserved" : "Empty";
            diningTable.UpdatedAt = now;
        }

        var summaryTable = order.OrderTables.FirstOrDefault(x => x.IsVirtual);
        if (summaryTable is not null)
        {
            summaryTable.Status = order.Status == "Paid" ? "Completed" : "Cancelled";
            summaryTable.GuestCount = 0;
            summaryTable.UpdatedAt = now;
        }

        order.DiningTableId = null;
        order.ActualGuestCount = 0;

        var roomId = order.DiningRoomId
            ?? order.Room?.Id
            ?? diningTableLookup.Values.Select(x => x.DiningRoomId).FirstOrDefault();

        if (roomId != Guid.Empty)
        {
            var room = order.Room;
            if (room is null || room.Id != roomId)
            {
                room = await _dbContext.DiningRooms.FirstOrDefaultAsync(x => x.Id == roomId, cancellationToken);
            }

            if (room is not null)
            {
                var localActiveTables = diningTableLookup.Values
                    .Any(x => x.DiningRoomId == room.Id
                              && (x.Status == "Serving" || x.Status == "Reserved"));
                var persistedOtherActiveTables = await _dbContext.DiningTables.AsNoTracking()
                    .AnyAsync(x => x.DiningRoomId == room.Id
                                   && !physicalTableIds.Contains(x.Id)
                                   && (x.Status == "Serving" || x.Status == "Reserved"), cancellationToken);
                room.Status = localActiveTables || persistedOtherActiveTables ? "Occupied" : "Available";
                room.UpdatedAt = now;
            }
        }
    }

    private static DateTime BuildReservationDateTime(DateTime date, string startTime)
        => TimeSpan.TryParse(startTime, out var time) ? date.Date.Add(time) : date.Date;

    private static string ResolveApprovedQuoteOrderStatus(DateTime scheduledStart)
        => scheduledStart <= DateTime.Now ? "Serving" : "Confirmed";

    private static void EnsureOrderEditable(AlacarteOrder order)
    {
        if (order.Status is "Paid" or "Cancelled" or "Replaced") throw new InvalidOperationException("Order đã đóng, không thể chỉnh sửa.");
    }

    private static void Recalculate(AlacarteOrderLine line)
    {
        var baseAmount = line.Qty * line.UnitPrice - line.DiscountAmount;
        line.VatAmount = Math.Round(baseAmount * line.VatRate / 100m, 2);
        line.Amount = baseAmount + line.VatAmount;
    }

    private static void Recalculate(AlacarteOrder order)
    {
        var serviceAmount = order.ExtraServices.Sum(x => x.Amount);
        order.SubtotalAmount = order.Lines.Where(x => x.LineStatus != "Cancelled").Sum(x => x.Qty * x.UnitPrice) + serviceAmount;
        order.VatAmount = order.Lines.Where(x => x.LineStatus != "Cancelled").Sum(x => x.VatAmount);
        order.TotalAmount = order.Lines.Where(x => x.LineStatus != "Cancelled").Sum(x => x.Amount) + serviceAmount - order.DiscountAmount + order.ServiceChargeAmount;
        order.RemainingAmount = Math.Max(0, order.TotalAmount - order.PaidAmount);
    }

    private static bool IsBar(string? area) => !string.IsNullOrWhiteSpace(area) && area.Contains("bar", StringComparison.OrdinalIgnoreCase);
    private static bool IsQuoteServiceLine(string? lineType)
    {
        if (string.IsNullOrWhiteSpace(lineType))
        {
            return false;
        }

        return string.Equals(lineType, "Dịch vụ", StringComparison.OrdinalIgnoreCase)
               || string.Equals(lineType, "Dich vu", StringComparison.OrdinalIgnoreCase)
               || string.Equals(lineType, "Dá»‹ch vá»¥", StringComparison.OrdinalIgnoreCase)
               || string.Equals(lineType, "Service", StringComparison.OrdinalIgnoreCase);
    }

    private static async Task<string> GenerateDocumentNoAsync(
        string prefix,
        DateTime date,
        IQueryable<string> source,
        CancellationToken cancellationToken)
    {
        var dayPrefix = $"{prefix}-{date:yyyyMMdd}-";
        var existingNumbers = await source
            .Where(x => x.StartsWith(dayPrefix))
            .ToListAsync(cancellationToken);

        var maxSequence = 0;
        foreach (var existingNumber in existingNumbers)
        {
            if (existingNumber.Length <= dayPrefix.Length)
            {
                continue;
            }

            var suffix = existingNumber[dayPrefix.Length..];
            if (int.TryParse(suffix, out var parsedSequence) && parsedSequence > maxSequence)
            {
                maxSequence = parsedSequence;
            }
        }

        return $"{dayPrefix}{maxSequence + 1:0000}";
    }

    private static IReadOnlyList<AlacarteDashboardRoomStatusDto> BuildRoomStatuses(
        IReadOnlyList<DiningRoom> rooms,
        IReadOnlyList<AlacarteOrder> openOrders,
        IReadOnlyList<AlacarteOrder> paidOrdersToday)
    {
        return rooms.Select(room =>
        {
            var roomOrderSet = openOrders
                .Where(order => GetOrderRoomIds(order).Contains(room.Id))
                .ToList();
            var servingTables = roomOrderSet
                .SelectMany(GetServingTableIds)
                .Distinct()
                .Count();
            var totalTables = room.Tables.Count(t => t.IsActive);
            var currentGuests = roomOrderSet
                .Where(IsServingOrder)
                .Sum(ResolveGuestCount);
            var revenueToday = paidOrdersToday
                .Where(order => GetOrderRoomIds(order).Contains(room.Id))
                .Sum(x => x.TotalAmount);
            var status = servingTables > 0
                ? "Serving"
                : roomOrderSet.Count > 0
                    ? "Reserved"
                    : "Available";

            return new AlacarteDashboardRoomStatusDto
            {
                RoomId = room.Id,
                RoomCode = room.Code,
                RoomName = room.Name,
                AreaName = NormalizeLegacyVietnameseText(room.RoomType) ?? room.RoomType,
                TotalTables = totalTables,
                ServingTables = servingTables,
                CurrentGuests = currentGuests,
                RevenueToday = revenueToday,
                Status = status,
                OccupancyPercent = totalTables <= 0 ? 0 : Math.Round(servingTables * 100d / totalTables, 1)
            };
        }).ToList();
    }

    private static IReadOnlyList<AlacarteDashboardServingCardDto> BuildServingBoard(IReadOnlyList<AlacarteOrder> openOrders)
    {
        return openOrders
            .Where(IsServingOrder)
            .OrderByDescending(GetServingPriority)
            .ThenBy(x => ResolveServingMinutes(x))
            .Select(order =>
            {
                var guestCount = ResolveGuestCount(order);
                var itemCount = order.Lines.Count(x => x.LineStatus != "Cancelled") + order.ExtraServices.Count;
                var warningLevel = ResolveServingWarningLevel(order);
                return new AlacarteDashboardServingCardDto
                {
                    OrderId = order.Id,
                    ReservationId = order.ReservationId,
                    QuoteId = order.RestaurantQuoteId,
                    OrderNo = order.OrderNo,
                    RoomName = ResolveOrderRoomName(order),
                    TableLabel = BuildOrderTableLabel(order),
                    CustomerName = order.Reservation?.CustomerName ?? order.Quote?.CustomerName ?? "Khách lẻ",
                    GuestCount = guestCount,
                    ServingMinutes = ResolveServingMinutes(order),
                    ItemCount = itemCount,
                    TotalAmount = order.TotalAmount,
                    Status = "Đang phục vụ",
                    WarningLevel = warningLevel,
                    Note = BuildServingNote(order)
                };
            })
            .ToList();
    }

    private static IReadOnlyList<AlacarteDashboardTimelineItemDto> BuildTimeline(
        IReadOnlyList<TableReservation> reservations,
        IReadOnlyList<DashboardReservationQuoteLink> quoteLinks)
    {
        var now = DateTime.Now;
        return reservations
            .Select(reservation =>
            {
                var quoteLink = quoteLinks.FirstOrDefault(x => x.ReservationId == reservation.Id);
                return new AlacarteDashboardTimelineItemDto
                {
                    ReservationId = reservation.Id,
                    QuoteId = quoteLink?.QuoteId,
                    ApprovedOrderId = quoteLink?.ApprovedOrderId,
                    ReservationNo = reservation.ReservationNo,
                    CustomerName = reservation.CustomerName ?? "Khách đặt",
                    RoomName = reservation.Room?.Name ?? string.Empty,
                    TableName = reservation.Table?.Name ?? string.Empty,
                    GuestCount = reservation.GuestCount,
                    ReservationDateTime = reservation.ReservationDateTime,
                    Status = reservation.Status,
                    TimelineState = ResolveTimelineState(reservation, now)
                };
            })
            .OrderBy(x => x.ReservationDateTime)
            .ToList();
    }

    private static IReadOnlyList<AlacarteDashboardChartPointDto> BuildRevenueChart(IReadOnlyList<AlacarteOrder> paidOrdersToday)
    {
        var labels = new[] { "08h", "10h", "12h", "14h", "16h", "18h", "20h", "22h" };
        return labels.Select(label =>
        {
            var hour = int.Parse(label[..2]);
            var nextHour = hour + 2;
            var total = paidOrdersToday
                .Where(x => x.OrderDate.Hour >= hour && x.OrderDate.Hour < nextHour)
                .Sum(x => x.TotalAmount);
            return new AlacarteDashboardChartPointDto
            {
                Label = label,
                Value = total
            };
        }).ToList();
    }

    private static AlacarteDashboardQuickReportsDto BuildQuickReports(
        DateTime businessDate,
        IReadOnlyList<AlacarteOrder> openOrders,
        IReadOnlyList<AlacarteOrder> paidOrdersToday,
        IReadOnlyList<RestaurantQuote> quotes)
    {
        var dayOrders = openOrders
            .Where(x => x.OrderDate.Date == businessDate.Date)
            .Concat(paidOrdersToday)
            .GroupBy(x => x.Id)
            .Select(g => g.First())
            .ToList();

        var topDishes = dayOrders
            .SelectMany(x => x.Lines)
            .Where(x => x.LineStatus != "Cancelled" && !IsQuoteServiceLine(x.LineType))
            .GroupBy(x => x.MenuItemName)
            .Select(g => new AlacarteDashboardTopItemDto
            {
                Name = g.Key,
                Quantity = g.Sum(x => x.Qty),
                Revenue = g.Sum(x => x.Amount)
            })
            .OrderByDescending(x => x.Quantity)
            .ThenByDescending(x => x.Revenue)
            .Take(5)
            .ToList();

        var topServices = dayOrders
            .SelectMany(x => x.ExtraServices)
            .GroupBy(x => x.Description)
            .Select(g => new AlacarteDashboardTopItemDto
            {
                Name = g.Key,
                Quantity = g.Sum(x => x.Qty),
                Revenue = g.Sum(x => x.Amount)
            })
            .OrderByDescending(x => x.Revenue)
            .ThenByDescending(x => x.Quantity)
            .Take(5)
            .ToList();

        var topCustomers = quotes
            .Where(x => x.EventDate.Date == businessDate.Date)
            .GroupBy(x => new { x.CustomerName, x.RoomName })
            .Select(g => new AlacarteDashboardTopCustomerDto
            {
                CustomerName = g.Key.CustomerName,
                RoomName = g.Key.RoomName,
                BookingCount = g.Count(),
                Revenue = g.Sum(x => x.TotalAmount)
            })
            .OrderByDescending(x => x.Revenue)
            .ThenByDescending(x => x.BookingCount)
            .Take(5)
            .ToList();

        var topRooms = quotes
            .Where(x => x.EventDate.Date == businessDate.Date)
            .GroupBy(x => x.RoomName)
            .Select(g => new AlacarteDashboardTopRoomDto
            {
                RoomName = g.Key,
                BookingCount = g.Count(),
                Revenue = g.Sum(x => x.TotalAmount)
            })
            .OrderByDescending(x => x.Revenue)
            .ThenByDescending(x => x.BookingCount)
            .Take(5)
            .ToList();

        return new AlacarteDashboardQuickReportsDto
        {
            TopDishes = topDishes,
            TopServices = topServices,
            TopCustomers = topCustomers,
            TopRooms = topRooms
        };
    }

    private static IReadOnlyList<AlacarteDashboardAlertDto> BuildAlerts(
        IReadOnlyList<TableReservation> reservations,
        IReadOnlyList<AlacarteOrder> openOrders,
        IReadOnlyList<RestaurantQuote> quotes,
        IReadOnlyList<RestaurantDeposit> depositsToday,
        IReadOnlyList<DashboardReservationQuoteLink> quoteLinks)
    {
        var now = DateTime.Now;
        var alerts = new List<AlacarteDashboardAlertDto>();

        alerts.AddRange(reservations
            .Where(x => x.Status == "Confirmed" && x.ReservationDateTime >= now && x.ReservationDateTime <= now.AddMinutes(30))
            .Take(4)
            .Select(reservation => new AlacarteDashboardAlertDto
            {
                Title = $"Bàn sắp đến giờ: {reservation.CustomerName}",
                Subtitle = $"{reservation.ReservationDateTime:HH:mm} · {reservation.Room?.Name ?? "Chưa xếp phòng"} · {reservation.GuestCount} khách",
                Severity = "warning",
                TargetType = "reservation",
                ReservationId = reservation.Id,
                QuoteId = quoteLinks.FirstOrDefault(x => x.ReservationId == reservation.Id)?.QuoteId,
                Timestamp = reservation.ReservationDateTime
            }));

        alerts.AddRange(reservations
            .Where(x => x.Status is "Confirmed" or "CheckedIn" && IsReservationOverdue(x, now))
            .Take(3)
            .Select(reservation => new AlacarteDashboardAlertDto
            {
                Title = $"Khách quá giờ: {reservation.CustomerName}",
                Subtitle = $"{reservation.ReservationDateTime:HH:mm} · {reservation.Room?.Name ?? "Chưa xếp phòng"}",
                Severity = "danger",
                TargetType = "reservation",
                ReservationId = reservation.Id,
                QuoteId = quoteLinks.FirstOrDefault(x => x.ReservationId == reservation.Id)?.QuoteId,
                Timestamp = reservation.ReservationDateTime
            }));

        alerts.AddRange(openOrders
            .Where(order => ResolveServingWarningLevel(order) is "danger" or "warning")
            .Take(4)
            .Select(order => new AlacarteDashboardAlertDto
            {
                Title = ResolveServingWarningLevel(order) == "danger"
                    ? $"Khách chờ món lâu: {ResolveOrderRoomName(order)}"
                    : $"Bàn sắp quá giờ: {ResolveOrderRoomName(order)}",
                Subtitle = $"{BuildOrderTableLabel(order)} · {ResolveServingMinutes(order)} phút · {ResolveGuestCount(order)} khách",
                Severity = ResolveServingWarningLevel(order),
                TargetType = "order",
                OrderId = order.Id,
                QuoteId = order.RestaurantQuoteId,
                Timestamp = order.KitchenSentAt ?? order.OrderDate
            }));

        alerts.AddRange(quotes
            .Where(x => x.EventDate.Date == DateTime.Today && x.DepositAmount > 0 && !x.DepositId.HasValue)
            .Take(2)
            .Select(quote => new AlacarteDashboardAlertDto
            {
                Title = $"Thiếu đặt cọc: {quote.CustomerName}",
                Subtitle = $"{quote.RoomName} · cần cọc {quote.DepositAmount:N0} đ",
                Severity = "info",
                TargetType = "quote",
                QuoteId = quote.Id,
                Timestamp = BuildReservationDateTime(quote.EventDate, quote.StartTime)
            }));

        if (alerts.Count == 0)
        {
            alerts.Add(new AlacarteDashboardAlertDto
            {
                Title = "Không có cảnh báo vận hành",
                Subtitle = "Các booking và đơn trong ngày đang ở trạng thái ổn định.",
                Severity = "muted",
                TargetType = string.Empty,
                Timestamp = now
            });
        }

        return alerts
            .OrderByDescending(x => GetAlertSeverityScore(x.Severity))
            .ThenBy(x => x.Timestamp ?? DateTime.MaxValue)
            .Take(8)
            .ToList();
    }

    private static IReadOnlyList<AlacarteDashboardActivityDto> BuildActivities(
        IReadOnlyList<TableReservation> reservations,
        IReadOnlyList<AlacarteOrder> openOrders,
        IReadOnlyList<RestaurantDeposit> depositsToday,
        IReadOnlyList<DashboardReservationQuoteLink> quoteLinks)
    {
        var activities = new List<AlacarteDashboardActivityDto>();

        activities.AddRange(reservations.Select(reservation => new AlacarteDashboardActivityDto
        {
            Title = $"Tạo lịch đặt {reservation.CustomerName}",
            Description = $"{reservation.ReservationDateTime:HH:mm} · {reservation.Room?.Name ?? "Chưa xếp phòng"}",
            ActivityType = "reservation",
            OccurredAt = reservation.CreatedAt,
            TargetType = "reservation",
            ReservationId = reservation.Id,
            QuoteId = quoteLinks.FirstOrDefault(x => x.ReservationId == reservation.Id)?.QuoteId
        }));

        activities.AddRange(openOrders.Select(order => new AlacarteDashboardActivityDto
        {
            Title = $"Mở đơn {order.OrderNo}",
            Description = $"{ResolveOrderRoomName(order)} · {ResolveGuestCount(order)} khách",
            ActivityType = "order",
            OccurredAt = order.CreatedAt,
            TargetType = "order",
            OrderId = order.Id,
            QuoteId = order.RestaurantQuoteId
        }));

        activities.AddRange(openOrders
            .SelectMany(order => order.PreparationTickets.Select(ticket => new AlacarteDashboardActivityDto
            {
                Title = $"{ticket.Station} nhận phiếu {ticket.TicketNo}",
                Description = $"{ResolveOrderRoomName(order)} · {ticket.Lines.Count} dòng món",
                ActivityType = "preparation",
                OccurredAt = ticket.SentAt,
                TargetType = "order",
                OrderId = order.Id,
                QuoteId = order.RestaurantQuoteId
            })));

        activities.AddRange(depositsToday.Select(deposit => new AlacarteDashboardActivityDto
        {
            Title = $"Nhận đặt cọc {deposit.DepositNo}",
            Description = $"{deposit.Amount:N0} đ · {deposit.PaymentMethod}",
            ActivityType = "deposit",
            OccurredAt = deposit.Date,
            TargetType = "quote"
        }));

        activities.AddRange(openOrders
            .Where(x => x.PaymentStatus == "Paid")
            .Select(order => new AlacarteDashboardActivityDto
            {
                Title = $"Thanh toán {order.OrderNo}",
                Description = $"{ResolveOrderRoomName(order)} · {order.TotalAmount:N0} đ",
                ActivityType = "payment",
                OccurredAt = order.UpdatedAt,
                TargetType = "order",
                OrderId = order.Id,
                QuoteId = order.RestaurantQuoteId
            }));

        return activities
            .OrderByDescending(x => x.OccurredAt)
            .Take(8)
            .ToList();
    }

    private static string ResolveTimelineState(TableReservation reservation, DateTime now)
    {
        if (string.Equals(reservation.Status, "Cancelled", StringComparison.OrdinalIgnoreCase))
        {
            return "Cancelled";
        }

        if (string.Equals(reservation.Status, "CheckedIn", StringComparison.OrdinalIgnoreCase)
            || string.Equals(reservation.Status, "Completed", StringComparison.OrdinalIgnoreCase))
        {
            return "Arrived";
        }

        if (IsReservationOverdue(reservation, now))
        {
            return "Overdue";
        }

        return "Upcoming";
    }

    private static bool IsReservationOverdue(TableReservation reservation, DateTime now)
    {
        var dueAt = ResolveReservationDueDateTime(reservation);
        return dueAt.HasValue && now > dueAt.Value.AddMinutes(15);
    }

    private static DateTime? ResolveReservationDueDateTime(TableReservation reservation)
    {
        var endTime = ExtractServiceWindowEndTime(reservation.Note);
        if (!string.IsNullOrWhiteSpace(endTime) && TimeSpan.TryParse(endTime, out var end))
        {
            return reservation.ReservationDateTime.Date.Add(end);
        }

        return reservation.ReservationDateTime;
    }

    private static int ResolveServingMinutes(AlacarteOrder order)
    {
        var start = ResolveOrderArrivalDateTime(order) ?? order.OrderDate;
        return Math.Max(0, (int)Math.Round((DateTime.Now - start).TotalMinutes));
    }

    private static string ResolveServingWarningLevel(AlacarteOrder order)
    {
        var pendingMinutes = order.KitchenSentAt.HasValue
            ? (DateTime.Now - order.KitchenSentAt.Value).TotalMinutes
            : 0;
        if (pendingMinutes >= 20)
        {
            return "danger";
        }

        var servingMinutes = ResolveServingMinutes(order);
        if (servingMinutes >= 120)
        {
            return "warning";
        }

        return "normal";
    }

    private static int GetServingPriority(AlacarteOrder order)
        => ResolveServingWarningLevel(order) switch
        {
            "danger" => 0,
            "warning" => 1,
            _ => 2
        };

    private static string BuildServingNote(AlacarteOrder order)
    {
        if (order.KitchenSentAt.HasValue)
        {
            var waitMinutes = Math.Max(0, (int)Math.Round((DateTime.Now - order.KitchenSentAt.Value).TotalMinutes));
            return $"Chờ món {waitMinutes} phút";
        }

        return $"Mở lúc {order.OrderDate:HH:mm}";
    }

    private static string BuildOrderTableLabel(AlacarteOrder order)
    {
        var tables = order.OrderTables
            .Where(x => !x.IsVirtual)
            .OrderBy(x => x.SortOrder)
            .Select(x => NormalizeLegacyVietnameseText(x.DisplayName) ?? x.DisplayName)
            .ToList();
        if (tables.Count > 0)
        {
            return string.Join(", ", tables.Take(3));
        }

        return NormalizeLegacyVietnameseText(order.Table?.Name)
               ?? NormalizeLegacyVietnameseText(order.Reservation?.Table?.Name)
               ?? "Bàn tổng";
    }

    private static string ResolveOrderRoomName(AlacarteOrder order)
        => NormalizeLegacyVietnameseText(order.Room?.Name)
           ?? NormalizeLegacyVietnameseText(order.Table?.Room?.Name)
           ?? NormalizeLegacyVietnameseText(order.Reservation?.Room?.Name)
           ?? NormalizeLegacyVietnameseText(order.Quote?.RoomName)
           ?? "Chưa xác định";

    private static int ResolveGuestCount(AlacarteOrder order)
        => order.ActualGuestCount > 0
            ? order.ActualGuestCount
            : order.OrderTables.Where(x => !x.IsVirtual).Sum(x => Math.Max(0, x.GuestCount)) > 0
                ? order.OrderTables.Where(x => !x.IsVirtual).Sum(x => Math.Max(0, x.GuestCount))
                : order.Reservation?.GuestCount ?? 0;

    private static bool IsServingOrder(AlacarteOrder order)
        => string.Equals(order.Status, "Serving", StringComparison.OrdinalIgnoreCase)
           || order.OrderTables.Any(x => !x.IsVirtual && string.Equals(x.Status, "Serving", StringComparison.OrdinalIgnoreCase));

    private static IEnumerable<Guid> GetOrderRoomIds(AlacarteOrder order)
    {
        if (order.DiningRoomId.HasValue)
        {
            yield return order.DiningRoomId.Value;
        }
        else if (order.Room?.Id is Guid roomId)
        {
            yield return roomId;
        }
        else if (order.Table?.DiningRoomId is Guid tableRoomId)
        {
            yield return tableRoomId;
        }
        else if (order.Reservation?.DiningRoomId is Guid reservationRoomId)
        {
            yield return reservationRoomId;
        }
    }

    private static IEnumerable<Guid> GetServingTableIds(AlacarteOrder order)
        => order.OrderTables
            .Where(x => !x.IsVirtual && x.DiningTableId.HasValue && string.Equals(x.Status, "Serving", StringComparison.OrdinalIgnoreCase))
            .Select(x => x.DiningTableId!.Value)
            .Concat(order.DiningTableId.HasValue && IsServingOrder(order) ? new[] { order.DiningTableId.Value } : Array.Empty<Guid>());

    private static int GetAlertSeverityScore(string severity)
        => severity switch
        {
            "danger" => 0,
            "warning" => 1,
            "info" => 2,
            _ => 3
        };

    private sealed record DashboardReservationQuoteLink(Guid ReservationId, Guid QuoteId, string QuoteNo, Guid? ApprovedOrderId);

    private static string Normalize(string? value, string fallback) => string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
    private static string? TrimOrNull(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static string AppendNote(string? current, string note)
        => string.IsNullOrWhiteSpace(current) ? note : $"{current}{Environment.NewLine}{note}";
    private static string? SetServiceWindowMetadata(string? note, string startTime, string endTime)
    {
        var sanitized = RemoveServiceWindowMetadata(note);
        var metadata = $"{ServiceWindowMetadataMarker} {startTime}-{endTime}";
        return string.IsNullOrWhiteSpace(sanitized) ? metadata : $"{sanitized}{Environment.NewLine}{metadata}";
    }

    private static string? RemoveServiceWindowMetadata(string? note)
    {
        if (string.IsNullOrWhiteSpace(note))
        {
            return note;
        }

        var lines = note
            .Split([Environment.NewLine, "\n"], StringSplitOptions.None)
            .Where(line => !line.Trim().StartsWith(ServiceWindowMetadataMarker, StringComparison.OrdinalIgnoreCase))
            .ToList();
        var cleaned = string.Join(Environment.NewLine, lines).Trim();
        return string.IsNullOrWhiteSpace(cleaned) ? null : cleaned;
    }

    private static string? ExtractServiceWindowEndTime(string? note)
    {
        if (string.IsNullOrWhiteSpace(note))
        {
            return null;
        }

        var line = note
            .Split([Environment.NewLine, "\n"], StringSplitOptions.RemoveEmptyEntries)
            .Select(x => x.Trim())
            .FirstOrDefault(x => x.StartsWith(ServiceWindowMetadataMarker, StringComparison.OrdinalIgnoreCase));
        if (string.IsNullOrWhiteSpace(line))
        {
            return null;
        }

        var payload = line[ServiceWindowMetadataMarker.Length..].Trim();
        var parts = payload.Split('-', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return parts.Length == 2 ? parts[1] : null;
    }

    private static string? ExtractSourceQuoteNo(string? note)
    {
        const string prefix = "Tá»« bÃ¡o giÃ¡ ";
        if (string.IsNullOrWhiteSpace(note) || !note.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return note[prefix.Length..].Trim();
    }

    private static DiningRoomDto ToDto(DiningRoom x) => new() { Id = x.Id, Code = x.Code, Name = x.Name, RoomType = x.RoomType, Capacity = x.Capacity, Description = x.Description, Status = x.Status, IsActive = x.IsActive, TableCount = x.Tables.Count };
    private static DiningTableDto ToDto(DiningTable x) => new() { Id = x.Id, DiningRoomId = x.DiningRoomId, RoomName = x.Room?.Name ?? string.Empty, Code = x.Code, Name = x.Name, SeatCount = x.SeatCount, Status = x.Status, Note = x.Note, IsActive = x.IsActive, SortOrder = x.SortOrder };
    private static TableReservationDto ToDto(TableReservation x) => new() { Id = x.Id, ReservationNo = x.ReservationNo, CustomerId = x.CustomerId, CustomerName = x.CustomerName, Phone = x.Phone, ReservationDateTime = x.ReservationDateTime, GuestCount = x.GuestCount, DiningRoomId = x.DiningRoomId, RoomName = x.Room?.Name, DiningTableId = x.DiningTableId, TableName = x.Table?.Name, Status = x.Status, Note = x.Note };
    private static AlacarteOrderDto ToDto(AlacarteOrder x)
    {
        var arrivalDateTime = ResolveOrderArrivalDateTime(x);
        return new AlacarteOrderDto
        {
            Id = x.Id,
            OrderNo = x.OrderNo,
            DiningTableId = x.DiningTableId ?? x.Reservation?.DiningTableId,
            DiningRoomId = x.DiningRoomId ?? x.Reservation?.DiningRoomId,
            RestaurantQuoteId = x.RestaurantQuoteId,
            TableName = x.Table?.Name ?? x.Reservation?.Table?.Name,
            RoomName = x.Room?.Name ?? x.Table?.Room?.Name ?? x.Reservation?.Room?.Name,
            CustomerId = x.CustomerId,
            CustomerName = x.Reservation?.CustomerName,
            CustomerPhone = x.Reservation?.Phone,
            ReservationId = x.ReservationId,
            ReservationNo = x.Reservation?.ReservationNo,
            GuestCount = x.ActualGuestCount > 0 ? x.ActualGuestCount : (x.Reservation?.GuestCount ?? 0),
            OrderType = string.IsNullOrWhiteSpace(x.OrderType) ? "Basic" : x.OrderType,
            SourceQuoteNo = x.Quote?.QuoteNo ?? ExtractSourceQuoteNo(x.Note),
            OrderDate = x.OrderDate,
            ArrivalDateTime = arrivalDateTime,
            StartTime = ResolveOrderStartTime(x, arrivalDateTime),
            EndTime = ResolveOrderEndTime(x),
            Status = x.Status,
            PaymentStatus = x.PaymentStatus,
            SubtotalAmount = x.SubtotalAmount,
            DiscountAmount = x.DiscountAmount,
            VatAmount = x.VatAmount,
            ServiceChargeAmount = x.ServiceChargeAmount,
            TotalAmount = x.TotalAmount,
            PaidAmount = x.PaidAmount,
            RemainingAmount = x.RemainingAmount,
            KitchenSentAt = x.KitchenSentAt,
            Note = RemoveServiceWindowMetadata(x.Note),
            Tables = x.OrderTables.OrderBy(t => t.SortOrder).Select(ToDto).ToList(),
            ServingGroups = x.ServingGroups.OrderBy(g => g.SortOrder).ThenBy(g => g.Name).Select(ToDto).ToList(),
            Lines = x.Lines.OrderBy(l => l.LineNo).Select(ToDto).Concat(x.ExtraServices.OrderBy(s => s.Description).Select(ToDto)).ToList(),
            PreparationTickets = x.PreparationTickets.OrderByDescending(t => t.SentAt).ThenByDescending(t => t.TicketNo).Select(ToDto).ToList()
        };
    }

    private static DateTime? ResolveOrderArrivalDateTime(AlacarteOrder order)
    {
        var reservation = order.Reservation;
        if (reservation is not null && reservation.ReservationDateTime != default)
        {
            return reservation.ReservationDateTime;
        }

        if (order.Quote is not null)
        {
            return BuildReservationDateTime(order.Quote.EventDate, order.Quote.StartTime);
        }

        return null;
    }

    private static string ResolveOrderStartTime(AlacarteOrder order, DateTime? arrivalDateTime)
    {
        if (!string.IsNullOrWhiteSpace(order.Quote?.StartTime))
        {
            return order.Quote.StartTime;
        }

        return arrivalDateTime?.ToString("HH:mm") ?? string.Empty;
    }

    private static string ResolveOrderEndTime(AlacarteOrder order)
        => !string.IsNullOrWhiteSpace(order.Quote?.EndTime)
            ? order.Quote.EndTime
            : ExtractServiceWindowEndTime(order.Note) ?? string.Empty;

    private async Task PromoteOrderToServingIfDueAsync(AlacarteOrder order, DateTime nowUtc, CancellationToken cancellationToken)
    {
        var localNow = DateTime.Now;
        var scheduledStart = ResolveOrderArrivalDateTime(order);
        if (!scheduledStart.HasValue || scheduledStart.Value > localNow)
        {
            return;
        }

        order.Status = "Serving";

        var diningTableIds = order.OrderTables
            .Where(x => !x.IsVirtual && x.DiningTableId.HasValue)
            .Select(x => x.DiningTableId!.Value)
            .Distinct()
            .ToList();

        if (diningTableIds.Count > 0)
        {
            var diningTables = await _dbContext.DiningTables
                .Include(x => x.Room)
                .Where(x => diningTableIds.Contains(x.Id))
                .ToListAsync(cancellationToken);

            foreach (var diningTable in diningTables)
            {
                diningTable.Status = "Serving";
                diningTable.UpdatedAt = nowUtc;
                if (diningTable.Room is not null)
                {
                    diningTable.Room.Status = "Occupied";
                    diningTable.Room.UpdatedAt = nowUtc;
                }
            }
        }

        foreach (var orderTable in order.OrderTables.Where(x => !x.IsVirtual))
        {
            orderTable.Status = "Serving";
            orderTable.UpdatedAt = nowUtc;
        }

        if (order.Table is not null)
        {
            order.Table.Status = "Serving";
            order.Table.UpdatedAt = nowUtc;
            if (order.Table.Room is not null)
            {
                order.Table.Room.Status = "Occupied";
                order.Table.Room.UpdatedAt = nowUtc;
            }
        }

        if (order.Room is not null)
        {
            order.Room.Status = "Occupied";
            order.Room.UpdatedAt = nowUtc;
        }
    }

    private static AlacarteOrderLineDto ToDto(AlacarteOrderLine x) => new()
    {
        Id = x.Id,
        OrderTableId = x.RestaurantOrderTableId,
        LineNo = x.LineNo,
        MenuItemId = x.MenuItemId,
        MenuItemCode = x.MenuItemCode,
        MenuItemName = x.MenuItemName,
        UnitName = x.UnitName,
        Qty = x.Qty,
        UnitPrice = x.UnitPrice,
        VatRate = x.VatRate,
        VatAmount = x.VatAmount,
        DiscountAmount = x.DiscountAmount,
        Amount = x.Amount,
        SupplementQuantity = x.SupplementQuantity,
        KitchenNote = x.KitchenNote,
        SupplementNote = ExtractSupplementDisplayNote(x.SupplementNote),
        SupplementNoteRaw = x.SupplementNote,
        LineStatus = x.LineStatus,
        KitchenStatus = x.KitchenStatus,
        LineType = x.LineType,
        PricingRule = x.PricingRule,
        IsSupplement = x.IsSupplement
    };

    private static AlacarteOrderLineDto ToDto(RestaurantOrderExtraService x) => new()
    {
        Id = x.Id,
        LineNo = 9000,
        MenuItemId = Guid.Empty,
        MenuItemCode = "SERVICE",
        MenuItemName = x.Description,
        UnitName = "dich vu",
        Qty = x.Qty,
        UnitPrice = x.UnitPrice,
        VatRate = x.VatRate,
        VatAmount = 0,
        DiscountAmount = 0,
        Amount = x.Amount,
        LineStatus = "Ordered",
        KitchenStatus = "NotRequired",
        LineType = "Dich vu",
        PricingRule = "PerRoom",
        IsSupplement = false
    };

    private static RestaurantPreparationTicketDto ToDto(RestaurantPreparationTicket x) => new() { Id = x.Id, AlacarteOrderId = x.AlacarteOrderId, TicketNo = x.TicketNo, Station = x.Station, TicketType = x.TicketType, Status = x.Status, SentAt = x.SentAt, Lines = x.Lines.OrderBy(l => l.LineNo).Select(l => new RestaurantPreparationTicketLineDto { MenuItemName = l.MenuItemName, Qty = l.Qty, SupplementQuantity = 0, KitchenNote = ExtractSupplementDisplayNote(l.KitchenNote), SupplementNote = l.KitchenNote }).ToList() };
    private static RestaurantDepositDto ToDto(RestaurantDeposit x) => new() { Id = x.Id, DepositNo = x.DepositNo, Date = x.Date, Amount = x.Amount, PaymentMethod = x.PaymentMethod, Receiver = x.Receiver, Note = x.Note, Status = x.Status };
    private static RestaurantOrderTableDto ToDto(RestaurantOrderTable x) => new() { Id = x.Id, DiningTableId = x.DiningTableId, QuoteTableId = x.RestaurantQuoteTableId, TableNo = x.TableNo, DisplayName = x.DisplayName, GuestCount = x.GuestCount, SortOrder = x.SortOrder, IsVirtual = x.IsVirtual, Status = x.Status, Note = x.Note };
    private static RestaurantServingGroupDto ToDto(RestaurantServingGroup x) => new()
    {
        Id = x.Id,
        AlacarteOrderId = x.AlacarteOrderId,
        Code = x.Code,
        Name = NormalizeLegacyVietnameseText(x.Name) ?? x.Name,
        ScopeType = x.ScopeType,
        ServingRule = x.ServingRule,
        SortOrder = x.SortOrder,
        Note = NormalizeLegacyVietnameseText(x.Note),
        IsSystemDefined = IsSystemDefinedServingGroup(x),
        TableCount = x.Tables.Count,
        GuestCount = x.Tables.Sum(t => Math.Max(0, t.GuestCount)),
        Tables = x.Tables
            .OrderBy(t => t.OrderTable?.SortOrder ?? int.MaxValue)
            .ThenBy(t => t.OrderTable?.DisplayName)
            .Select(ToDto)
            .ToList()
    };
    private static RestaurantServingGroupTableDto ToDto(RestaurantServingGroupTable x) => new()
    {
        Id = x.Id,
        RestaurantOrderTableId = x.RestaurantOrderTableId,
        DiningTableId = x.DiningTableId,
        TableName = NormalizeLegacyVietnameseText(x.OrderTable?.DisplayName) ?? "Bàn",
        GuestCount = x.GuestCount,
        ExtraGuestCount = x.ExtraGuestCount,
        IsServing = x.IsServing
    };
    private static RestaurantQuoteTableDto ToDto(RestaurantQuoteTable x) => new() { Id = x.Id, TableNo = x.TableNo, DisplayName = x.DisplayName, GuestCount = x.GuestCount, SortOrder = x.SortOrder, IsVirtual = x.IsVirtual, Note = x.Note };

    private static string? NormalizeLegacyVietnameseText(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        if (!LooksLikeLegacyMojibake(value))
        {
            return value;
        }

        try
        {
            var latin1Bytes = Encoding.GetEncoding(1252).GetBytes(value);
            var utf8Value = Encoding.UTF8.GetString(latin1Bytes);
            if (!string.IsNullOrWhiteSpace(utf8Value) && !LooksLikeLegacyMojibake(utf8Value))
            {
                return utf8Value;
            }
        }
        catch
        {
            // Ignore fallback conversion failures and return original text.
        }

        return value;
    }

    private static bool LooksLikeLegacyMojibake(string value)
        => value.Contains("Ã", StringComparison.Ordinal)
           || value.Contains("Ä", StringComparison.Ordinal)
           || value.Contains("Æ", StringComparison.Ordinal)
           || value.Contains("á»", StringComparison.Ordinal)
           || value.Contains("áº", StringComparison.Ordinal)
           || value.Contains("Tá»", StringComparison.Ordinal)
           || value.Contains("Há»", StringComparison.Ordinal)
           || value.Contains("Ä", StringComparison.Ordinal);

    private static RestaurantExtraServiceDto ToDto(RestaurantExtraService x) => new() { Id = x.Id, Code = x.Code, Name = x.Name, ServiceType = x.ServiceType, UnitName = Normalize(x.UnitName, "lần"), DefaultPrice = x.DefaultPrice, VatRate = x.VatRate, IsVatIncluded = x.IsVatIncluded, IsActive = x.IsActive, Note = x.Note, ImagePath = x.ImagePath, ImageUrl = x.ImageUrl };
    private static RestaurantContractDto ToDto(RestaurantContract x) => new() { Id = x.Id, ContractNo = x.ContractNo, CustomerId = x.CustomerId, EventDescription = x.EventDescription, TotalAmount = x.TotalAmount, DepositAmount = x.DepositAmount, RemainingAmount = x.RemainingAmount, Status = x.Status, SignedDate = x.SignedDate, Note = x.Note };
    private static RestaurantQuoteDto ToDto(RestaurantQuote x) => new()
    {
        Id = x.Id,
        QuoteNo = x.QuoteNo,
        CustomerName = x.CustomerName,
        Phone = x.Phone,
        TableCount = x.TableCount,
        GuestCount = x.GuestCount <= 0 ? Math.Max(1, x.TableCount * 10) : x.GuestCount,
        TableStandard = x.TableStandard <= 0 ? 10 : x.TableStandard,
        DiningRoomId = x.DiningRoomId,
        DiningTableId = x.DiningTableId,
        TableReservationId = x.TableReservationId,
        ApprovedOrderId = x.ApprovedOrderId,
        DepositId = x.DepositId,
        DepositCashTransactionId = x.DepositCashTransactionId,
        DepositCashTransactionNo = x.DepositCashTransactionNo,
        RoomName = x.RoomName,
        EventDate = x.EventDate,
        StartTime = x.StartTime,
        EndTime = x.EndTime,
        TotalAmount = x.TotalAmount,
        DepositAmount = x.DepositAmount,
        PaymentDeadline = x.PaymentDeadline,
        PaymentMethod = x.PaymentMethod,
        DiscountAmount = x.DiscountAmount,
        Promotion = x.Promotion,
        ValidityDays = x.ValidityDays,
        CancelDeadlineDays = x.CancelDeadlineDays,
        CancelFeePercent = x.CancelFeePercent,
        BankAccount = x.BankAccount,
        Status = x.Status,
        Note = x.Note,
        PreparedBy = x.PreparedBy,
        CreatedAt = x.CreatedAt,
        Tables = x.QuoteTables.OrderBy(t => t.SortOrder).Select(ToDto).ToList(),
        Lines = x.Lines.OrderBy(l => l.LineNo).Select(ToDto).ToList()
    };

    private static RestaurantQuoteLineDto ToDto(RestaurantQuoteLine x) => new()
    {
        Id = x.Id,
        MenuItemId = x.MenuItemId,
        LineNo = x.LineNo,
        Name = x.Name,
        Type = x.LineType,
        UnitPrice = x.UnitPrice,
        Quantity = x.Quantity,
        PricingRule = x.PricingRule,
        CalculatedQuantity = x.CalculatedQuantity,
        Total = x.Total
    };

    private static BanquetMenuPackageDto ToBanquetMenuPackageDto(BanquetMenuPackage x) => new()
    {
        Id = x.Id,
        Code = x.Code,
        Name = x.Name,
        GroupName = x.Note,
        PricingMode = x.PricingMode,
        Price = x.Price,
        VatRate = x.VatRate,
        IsActive = x.IsActive,
        LineCount = x.Lines.Count,
        Lines = x.Lines
            .OrderBy(l => l.LineNo)
            .Select(l => new BanquetMenuPackageLineDto
            {
                Id = l.Id,
                MenuItemId = l.MenuItemId,
                LineNo = l.LineNo,
                CourseType = l.CourseType,
                PricingRule = string.IsNullOrWhiteSpace(l.PricingRule) ? x.PricingMode : l.PricingRule!,
                Qty = l.Qty,
                MenuItemCode = l.MenuItem?.Code ?? string.Empty,
                MenuItemName = l.MenuItem?.Name ?? string.Empty,
                UnitName = l.MenuItem?.UnitName ?? string.Empty,
                UnitPrice = l.MenuItem?.UnitPrice ?? 0m,
                Note = l.Note
            })
            .ToList()
    };

    private sealed class RestaurantLineSupplementMetadata
    {
        public string? DisplayNote { get; set; }
        public List<RestaurantLineIngredientAdjustmentDto> IngredientAdjustments { get; set; } = [];
    }

    private sealed class PreparationTicketDraftLine
    {
        public Guid? OrderLineId { get; init; }
        public string MenuItemName { get; init; } = string.Empty;
        public decimal Qty { get; init; }
        public string Station { get; init; } = "Kitchen";
        public string? KitchenNote { get; init; }
    }

}

public sealed class BanquetRestaurantService : IBanquetRestaurantService
{
    private readonly AppDbContext _dbContext;
    private readonly ICashFundService? _cashFundService;
    private readonly IAuditService? _auditService;
    private readonly ICurrentUserService? _currentUserService;

    public BanquetRestaurantService(AppDbContext dbContext, ICashFundService? cashFundService = null, IAuditService? auditService = null, ICurrentUserService? currentUserService = null)
    {
        _dbContext = dbContext;
        _cashFundService = cashFundService;
        _auditService = auditService;
        _currentUserService = currentUserService;
    }

    public async Task<BanquetDashboardDto> GetDashboardAsync(DateTime? businessDate = null, CancellationToken cancellationToken = default)
    {
        var date = (businessDate ?? DateTime.Today).Date;
        var next = date.AddDays(1);
        var events = await _dbContext.BanquetEvents.AsNoTracking().Where(x => x.EventDate >= date && x.EventDate < next && x.Status != "Cancelled").ToListAsync(cancellationToken);
        return new BanquetDashboardDto
        {
            TodayEvents = events.Count,
            ConfirmedEvents = events.Count(x => x.Status is "Confirmed" or "Deposited"),
            PreparingEvents = events.Count(x => x.Status == "Preparing"),
            TodayDeposits = await _dbContext.BanquetDeposits.Where(x => x.DepositDate >= date && x.DepositDate < next && x.Status == "Confirmed").SumAsync(x => x.Amount, cancellationToken),
            ExpectedGuests = events.Sum(x => x.GuestCount),
            RoomConflicts = await CountRoomConflictsAsync(date, next, cancellationToken)
        };
    }

    public async Task<IReadOnlyList<BanquetRoomDto>> GetRoomsAsync(CancellationToken cancellationToken = default)
        => await _dbContext.BanquetRooms.AsNoTracking().OrderBy(x => x.Code).Select(x => ToDto(x)).ToListAsync(cancellationToken);

    public async Task<BanquetRoomDto> SaveRoomAsync(SaveBanquetRoomRequest request, CancellationToken cancellationToken = default)
    {
        EnsureCanManageBanquet();
        if (request.Capacity <= 0) throw new InvalidOperationException("Sức chứa phòng tiệc phải lớn hơn 0.");
        if (request.MaxTables <= 0) throw new InvalidOperationException("Số bàn tối đa phải lớn hơn 0.");
        var code = request.Code.Trim();
        if (string.IsNullOrWhiteSpace(code)) throw new InvalidOperationException("Mã phòng tiệc không được để trống.");
        if (await _dbContext.BanquetRooms.AnyAsync(x => x.Code == code && (!request.Id.HasValue || x.Id != request.Id.Value), cancellationToken)) throw new InvalidOperationException("Mã phòng tiệc đã tồn tại.");
        var now = DateTime.UtcNow;
        var room = request.Id.HasValue
            ? await _dbContext.BanquetRooms.FirstOrDefaultAsync(x => x.Id == request.Id.Value, cancellationToken) ?? throw new InvalidOperationException("Không tìm thấy phòng tiệc.")
            : new BanquetRoom { Id = Guid.NewGuid(), CreatedAt = now };
        room.Code = code;
        room.Name = request.Name.Trim();
        room.Capacity = request.Capacity;
        room.MaxTables = request.MaxTables;
        room.Description = TrimOrNull(request.Description);
        room.LayoutJson = TrimOrNull(request.LayoutJson);
        room.Status = Normalize(request.Status, "Available");
        room.IsActive = request.IsActive;
        room.UpdatedAt = now;
        if (!request.Id.HasValue) _dbContext.BanquetRooms.Add(room);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return ToDto(room);
    }

    public async Task DeleteRoomAsync(Guid roomId, CancellationToken cancellationToken = default)
    {
        EnsureCanManageBanquet();
        var room = await _dbContext.BanquetRooms.FirstOrDefaultAsync(x => x.Id == roomId, cancellationToken)
            ?? throw new InvalidOperationException("Không tìm thấy phòng tiệc.");
        if (await _dbContext.BanquetEvents.AnyAsync(x => x.BanquetRoomId == roomId, cancellationToken))
        {
            room.IsActive = false;
            room.Status = "Inactive";
            room.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            _dbContext.BanquetRooms.Remove(room);
        }
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<BanquetEventDto>> GetEventsAsync(DateTime? fromDate = null, DateTime? toDate = null, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.BanquetEvents.AsNoTracking().AsQueryable();
        if (fromDate.HasValue) query = query.Where(x => x.EventDate >= fromDate.Value.Date);
        if (toDate.HasValue) query = query.Where(x => x.EventDate < toDate.Value.Date.AddDays(1));
        var events = await query
            .OrderBy(x => x.EventDate)
            .ThenBy(x => x.StartTime)
            .Select(x => new
            {
                Event = x,
                RoomName = x.Room != null ? x.Room.Name : null,
                CustomMenuPerTableAmount = x.Menus.Where(m => m.PricingMode == "Custom").Sum(m => m.Amount),
                PresetMenuAmount = x.Menus.Where(m => m.PricingMode != "Custom").Sum(m => m.Amount),
                ServiceAmount = x.Services.Sum(s => s.Amount),
                ServiceEstimatedCost = x.Services.Sum(s => s.CostAmount),
                IngredientEstimatedCost = x.PurchaseRequests
                    .OrderByDescending(p => p.RequestedAt)
                    .Select(p => p.Lines.Sum(l => l.EstimatedCost))
                    .FirstOrDefault(),
                DepositAmount = x.Deposits
                    .Where(d => d.Status == "Confirmed" || d.Status == "PartiallyRefunded" || d.Status == "Refunded")
                    .Sum(d => d.Amount - d.RefundAmount)
            })
            .ToListAsync(cancellationToken);

        return events.Select(x =>
        {
            var menuAmount = x.CustomMenuPerTableAmount * (x.Event.TableCount + x.Event.ExtraTableCount) + x.PresetMenuAmount;
            var total = menuAmount + x.ServiceAmount;
            return new BanquetEventDto
            {
                Id = x.Event.Id, EventNo = x.Event.EventNo, EventType = x.Event.EventType,
                CustomerId = x.Event.CustomerId, CustomerName = x.Event.CustomerName, Phone = x.Event.Phone,
                EventDate = x.Event.EventDate, StartTime = x.Event.StartTime, EndTime = x.Event.EndTime,
                GuestCount = x.Event.GuestCount, TableCount = x.Event.TableCount,
                ExtraGuestCount = x.Event.ExtraGuestCount, ExtraTableCount = x.Event.ExtraTableCount,
                BrideName = x.Event.BrideName, GroomName = x.Event.GroomName,
                ConferenceName = x.Event.ConferenceName, BirthdayPersonName = x.Event.BirthdayPersonName,
                BirthdayDate = x.Event.BirthdayDate, MeetingTopic = x.Event.MeetingTopic,
                PartyTheme = x.Event.PartyTheme, OtherEventName = x.Event.OtherEventName,
                BanquetRoomId = x.Event.BanquetRoomId, RoomName = x.RoomName,
                Status = x.Event.Status, IsFinanciallyLocked = x.Event.IsFinanciallyLocked,
                FinanciallyLockedAt = x.Event.FinanciallyLockedAt,
                FinanciallyLockedBy = x.Event.FinanciallyLockedBy,
                RowVersion = x.Event.RowVersion, Note = x.Event.Note,
                MenuAmount = menuAmount, ServiceAmount = x.ServiceAmount,
                DepositAmount = x.DepositAmount, TotalAmount = total,
                RemainingAmount = Math.Max(0m, total - x.DepositAmount),
                IngredientEstimatedCost = x.IngredientEstimatedCost,
                ServiceEstimatedCost = x.ServiceEstimatedCost
            };
        }).ToList();
    }

    public async Task<BanquetEventDetailDto> GetEventDetailAsync(Guid banquetEventId, CancellationToken cancellationToken = default)
    {
        var entity = await LoadEventDetailAsync(banquetEventId, cancellationToken);
        return ToDetailDto(entity);
    }

    public async Task<BanquetEventDto> SaveEventAsync(SaveBanquetEventRequest request, CancellationToken cancellationToken = default)
    {
        EnsureCanManageBanquet();
        var warnings = await ValidateEventScheduleAsync(request, cancellationToken);
        if (warnings.Count > 0) throw new InvalidOperationException(string.Join(" ", warnings));
        var now = DateTime.UtcNow;
        var entity = request.Id.HasValue
            ? await _dbContext.BanquetEvents.Include(x => x.ChecklistItems).FirstOrDefaultAsync(x => x.Id == request.Id.Value, cancellationToken) ?? throw new InvalidOperationException("Không tìm thấy sự kiện tiệc.")
            : new BanquetEvent { Id = Guid.NewGuid(), EventNo = await GenerateEventNoAsync(request.EventDate, cancellationToken), CreatedAt = now };
        if (entity.IsFinanciallyLocked)
            throw new InvalidOperationException("Tiệc đã khóa quyết toán. Hãy mở khóa bằng nghiệp vụ điều chỉnh trước khi sửa.");
        if (request.Id.HasValue && request.RowVersion is { Length: > 0 })
            _dbContext.Entry(entity).Property(x => x.RowVersion).OriginalValue = request.RowVersion;
        var targetStatus = Normalize(request.Status, "Draft");
        if (request.Id.HasValue)
            ValidateEventStatusTransition(entity.Status, targetStatus);
        if (targetStatus == "Confirmed")
        {
            var settings = await _dbContext.BanquetModuleSettings.AsNoTracking().FirstOrDefaultAsync(cancellationToken);
            var hasContract = request.Id.HasValue && await _dbContext.BanquetContracts.AnyAsync(x => x.BanquetEventId == entity.Id, cancellationToken);
            if (settings?.RequireContractBeforeConfirm == true && !hasContract)
                throw new InvalidOperationException("Cần tạo hợp đồng trước khi xác nhận tiệc.");
        }
        entity.EventType = Normalize(request.EventType, "Other");
        entity.CustomerId = request.CustomerId;
        entity.CustomerName = TrimOrNull(request.CustomerName);
        entity.Phone = request.Phone.Trim();
        entity.EventDate = request.EventDate.Date;
        entity.StartTime = request.StartTime;
        entity.EndTime = request.EndTime;
        entity.GuestCount = request.GuestCount;
        entity.TableCount = request.TableCount;
        entity.ExtraGuestCount = request.ExtraGuestCount;
        entity.ExtraTableCount = request.ExtraTableCount;
        entity.BrideName = TrimOrNull(request.BrideName);
        entity.GroomName = TrimOrNull(request.GroomName);
        entity.ConferenceName = TrimOrNull(request.ConferenceName);
        entity.BirthdayPersonName = TrimOrNull(request.BirthdayPersonName);
        entity.BirthdayDate = request.BirthdayDate?.Date;
        entity.MeetingTopic = TrimOrNull(request.MeetingTopic);
        entity.PartyTheme = TrimOrNull(request.PartyTheme);
        entity.OtherEventName = TrimOrNull(request.OtherEventName);
        entity.BanquetRoomId = request.BanquetRoomId;
        entity.Status = targetStatus;
        entity.Note = TrimOrNull(request.Note);
        entity.UpdatedAt = now;
        if (!request.Id.HasValue)
        {
            AddDefaultChecklist(entity);
            AddDefaultTimeline(entity);
            _dbContext.BanquetEvents.Add(entity);
        }
        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new InvalidOperationException("Dữ liệu tiệc vừa được người khác cập nhật. Vui lòng tải lại trước khi lưu.");
        }
        await SyncContractsAsync(entity.Id, cancellationToken);
        return ToDto(await LoadEventAsync(entity.Id, cancellationToken));
    }

    public async Task<BanquetEventDto> UpdateEventStatusAsync(Guid banquetEventId, string status, CancellationToken cancellationToken = default)
    {
        EnsureCanManageBanquet();
        var entity = await _dbContext.BanquetEvents.FirstOrDefaultAsync(x => x.Id == banquetEventId, cancellationToken)
            ?? throw new InvalidOperationException("Không tìm thấy sự kiện tiệc.");
        var previousStatus = entity.Status;
        var target = Normalize(status, entity.Status);
        ValidateEventStatusTransition(entity.Status, target);
        if (target == "Confirmed")
        {
            var settings = await _dbContext.BanquetModuleSettings.AsNoTracking().FirstOrDefaultAsync(cancellationToken);
            if (settings?.RequireContractBeforeConfirm == true && !await _dbContext.BanquetContracts.AnyAsync(x => x.BanquetEventId == banquetEventId, cancellationToken))
                throw new InvalidOperationException("Cần tạo hợp đồng trước khi xác nhận tiệc.");
        }
        if (target == "Cancelled")
        {
            var activeDeposit = await _dbContext.BanquetDeposits
                .Where(x => x.BanquetEventId == banquetEventId &&
                            (x.Status == "Confirmed" || x.Status == "PartiallyRefunded" || x.Status == "Refunded"))
                .SumAsync(x => x.Amount - x.RefundAmount, cancellationToken);
            if (activeDeposit > 0m)
                throw new InvalidOperationException($"Tiệc còn {activeDeposit:N0} đ tiền cọc. Hãy hoàn hoặc xử lý hết tiền cọc trước khi hủy.");
        }
        entity.Status = target;
        entity.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
        if (_auditService is not null)
            await _auditService.LogAsync("BanquetEvent", entity.Id.ToString(), "StatusChanged", new { Status = previousStatus }, new { Status = target }, cancellationToken: cancellationToken);
        await SyncContractsAsync(entity.Id, cancellationToken);
        return ToDto(await LoadEventAsync(entity.Id, cancellationToken));
    }

    public async Task<BanquetEventDto> FinalizeSettlementAsync(Guid banquetEventId, string paymentMethod, string? receiver = null, string? note = null, CancellationToken cancellationToken = default)
    {
        EnsureCanManageBanquet();
        var entity = await LoadEventDetailAsync(banquetEventId, cancellationToken);
        if (entity.IsFinanciallyLocked && entity.Status == "Completed")
            return ToDto(entity);
        if (entity.Status == "Cancelled")
            throw new InvalidOperationException("Không thể quyết toán sự kiện đã hủy.");
        if (string.IsNullOrWhiteSpace(paymentMethod))
            throw new InvalidOperationException("Vui lòng chọn phương thức thanh toán khi quyết toán.");
        var dto = ToDto(entity);
        var remaining = dto.RemainingAmount;
        if (remaining > 0m)
        {
            Guid? cashFundId = null;
            if (string.Equals(paymentMethod, "Cash", StringComparison.OrdinalIgnoreCase) && _cashFundService is not null)
            {
                cashFundId = await ResolveDefaultCashFundIdAsync(cancellationToken);
            }
            await AddDepositAsync(new AddBanquetDepositRequest
            {
                BanquetEventId = banquetEventId,
                BanquetContractId = entity.Contracts.OrderByDescending(x => x.SignDate).ThenByDescending(x => x.ContractNo).FirstOrDefault()?.Id,
                DepositDate = DateTime.Today,
                Amount = remaining,
                PaymentMethod = Normalize(paymentMethod, "Cash"),
                CashFundId = cashFundId,
                Receiver = TrimOrNull(receiver),
                Note = TrimOrNull(note) ?? "Quyết toán phần còn lại",
                IdempotencyKey = $"settlement:{banquetEventId:N}"
            });
        }

        entity.Status = "Completed";
        entity.IsFinanciallyLocked = true;
        entity.FinanciallyLockedAt = DateTime.UtcNow;
        entity.FinanciallyLockedBy = TrimOrNull(receiver);
        entity.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
        await SyncContractsAsync(entity.Id, cancellationToken);
        return ToDto(await LoadEventAsync(entity.Id, cancellationToken));
    }

    public async Task<IReadOnlyList<string>> ValidateEventScheduleAsync(SaveBanquetEventRequest request, CancellationToken cancellationToken = default)
    {
        var warnings = new List<string>();
        if (request.ExtraGuestCount < 0) warnings.Add("Số khách phát sinh không được âm.");
        if (request.ExtraTableCount < 0) warnings.Add("Số bàn phát sinh không được âm.");
        switch (Normalize(request.EventType, "Other"))
        {
            case "Wedding" when string.IsNullOrWhiteSpace(request.BrideName) && string.IsNullOrWhiteSpace(request.GroomName):
                warnings.Add("Tiệc cưới cần nhập cô dâu hoặc chú rể.");
                break;
            case "Conference" when string.IsNullOrWhiteSpace(request.ConferenceName):
                warnings.Add("Nhập tên hội nghị.");
                break;
            case "Birthday" when string.IsNullOrWhiteSpace(request.BirthdayPersonName):
                warnings.Add("Nhập tên người sinh nhật.");
                break;
            case "Meeting" when string.IsNullOrWhiteSpace(request.MeetingTopic):
                warnings.Add("Nhập chủ đề họp mặt.");
                break;
            case "Other" when string.IsNullOrWhiteSpace(request.OtherEventName):
                warnings.Add("Nhập tên sự kiện.");
                break;
        }
        if (request.EventDate == default) warnings.Add("Thi\u1ebfu ng\u00e0y t\u1ed5 ch\u1ee9c.");
        if (request.StartTime >= request.EndTime) warnings.Add("Gi\u1edd b\u1eaft \u0111\u1ea7u ph\u1ea3i nh\u1ecf h\u01a1n gi\u1edd k\u1ebft th\u00fac.");
        if (request.GuestCount <= 0) warnings.Add("S\u1ed1 kh\u00e1ch ph\u1ea3i l\u1edbn h\u01a1n 0.");
        if (request.TableCount <= 0) warnings.Add("S\u1ed1 b\u00e0n ph\u1ea3i l\u1edbn h\u01a1n 0.");
        if (request.BanquetRoomId.HasValue)
        {
            var room = await _dbContext.BanquetRooms.AsNoTracking().FirstOrDefaultAsync(x => x.Id == request.BanquetRoomId.Value, cancellationToken);
            if (room is null) warnings.Add("Kh\u00f4ng t\u00ecm th\u1ea5y ph\u00f2ng ti\u1ec7c.");
            else
            {
                if (request.GuestCount > room.Capacity) warnings.Add("S\u1ed1 kh\u00e1ch v\u01b0\u1ee3t s\u1ee9c ch\u1ee9a ph\u00f2ng.");
                if (request.TableCount > room.MaxTables) warnings.Add("S\u1ed1 b\u00e0n v\u01b0\u1ee3t c\u1ea5u h\u00ecnh ph\u00f2ng.");
            }

            var conflict = await _dbContext.BanquetEvents.AsNoTracking().AnyAsync(x =>
                x.BanquetRoomId == request.BanquetRoomId &&
                x.EventDate == request.EventDate.Date &&
                x.Status != "Cancelled" &&
                (!request.Id.HasValue || x.Id != request.Id.Value) &&
                request.StartTime < x.EndTime && request.EndTime > x.StartTime, cancellationToken);
            if (conflict) warnings.Add("Ph\u00f2ng ti\u1ec7c b\u1ecb tr\u00f9ng l\u1ecbch ho\u1eb7c tr\u00f9ng gi\u1edd.");
        }
        return warnings;
    }

    public async Task<BanquetMenuPackageDto> SaveMenuPackageAsync(SaveBanquetMenuPackageRequest request, CancellationToken cancellationToken = default)
    {
        EnsureCanManageBanquet();
        var code = request.Code.Trim();
        if (string.IsNullOrWhiteSpace(code)) throw new InvalidOperationException("Mã thực đơn tiệc không được để trống.");
        if (request.Price < 0) throw new InvalidOperationException("Giá thực đơn không được âm.");
        if (await _dbContext.BanquetMenuPackages.AnyAsync(x => x.Code == code && (!request.Id.HasValue || x.Id != request.Id.Value), cancellationToken)) throw new InvalidOperationException("Mã thực đơn tiệc đã tồn tại.");
        var package = request.Id.HasValue
            ? await _dbContext.BanquetMenuPackages.Include(x => x.Lines).FirstOrDefaultAsync(x => x.Id == request.Id.Value, cancellationToken) ?? throw new InvalidOperationException("Không tìm thấy thực đơn tiệc.")
            : new BanquetMenuPackage { Id = Guid.NewGuid() };
        package.Code = code;
        package.Name = request.Name.Trim();
        package.Note = TrimOrNull(request.GroupName);
        package.PricingMode = Normalize(request.PricingMode, "PerTable");
        package.Price = request.Price;
        package.VatRate = request.VatRate;
        package.IsActive = request.IsActive;
        if (request.Id.HasValue && package.Lines.Count > 0)
        {
            _dbContext.BanquetMenuPackageLines.RemoveRange(package.Lines.ToArray());
            package.Lines.Clear();
        }
        var newLines = new List<BanquetMenuPackageLine>();
        var lineNo = 1;
        foreach (var line in request.Lines)
        {
            newLines.Add(new BanquetMenuPackageLine
            {
                Id = Guid.NewGuid(),
                BanquetMenuPackageId = package.Id,
                MenuItemId = line.MenuItemId,
                CourseType = Normalize(line.CourseType, "Main"),
                PricingRule = ValidatePricingRule(string.IsNullOrWhiteSpace(line.PricingRule) ? package.PricingMode : line.PricingRule),
                Qty = line.Qty,
                LineNo = lineNo++,
                Note = TrimOrNull(line.Note)
            });
        }
        if (!request.Id.HasValue) _dbContext.BanquetMenuPackages.Add(package);
        if (newLines.Count > 0) _dbContext.BanquetMenuPackageLines.AddRange(newLines);
        await _dbContext.SaveChangesAsync(cancellationToken);
        var saved = await _dbContext.BanquetMenuPackages.AsNoTracking()
            .Include(x => x.Lines)
            .ThenInclude(x => x.MenuItem)
            .SingleAsync(x => x.Id == package.Id, cancellationToken);
        return ToDto(saved);
    }

    public async Task<IReadOnlyList<BanquetMenuPackageDto>> GetMenuPackagesAsync(CancellationToken cancellationToken = default)
        => await _dbContext.BanquetMenuPackages
            .AsNoTracking()
            .Include(x => x.Lines)
            .ThenInclude(x => x.MenuItem)
            .OrderBy(x => x.Code)
            .Select(x => ToDto(x))
            .ToListAsync(cancellationToken);

    public async Task<BanquetMenuGroupDto> SaveMenuGroupAsync(SaveBanquetMenuGroupRequest request, CancellationToken cancellationToken = default)
    {
        EnsureCanManageBanquet();
        var name = request.Name.Trim();
        if (string.IsNullOrWhiteSpace(name)) throw new InvalidOperationException("Tên nhóm thực đơn không được để trống.");
        if (await _dbContext.BanquetMenuGroups.AnyAsync(x => x.Name == name && (!request.Id.HasValue || x.Id != request.Id.Value), cancellationToken))
            throw new InvalidOperationException("Nhóm thực đơn đã tồn tại.");

        var entity = request.Id.HasValue
            ? await _dbContext.BanquetMenuGroups.FirstOrDefaultAsync(x => x.Id == request.Id.Value, cancellationToken)
                ?? throw new InvalidOperationException("Không tìm thấy nhóm thực đơn.")
            : new BanquetMenuGroup { Id = Guid.NewGuid() };
        entity.Name = name;
        entity.SortOrder = request.SortOrder;
        entity.IsActive = request.IsActive;
        if (!request.Id.HasValue) _dbContext.BanquetMenuGroups.Add(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return new BanquetMenuGroupDto { Id = entity.Id, Name = entity.Name, SortOrder = entity.SortOrder, IsActive = entity.IsActive };
    }

    public async Task<IReadOnlyList<BanquetMenuGroupDto>> GetMenuGroupsAsync(CancellationToken cancellationToken = default)
        => await _dbContext.BanquetMenuGroups.AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Name)
            .Select(x => new BanquetMenuGroupDto { Id = x.Id, Name = x.Name, SortOrder = x.SortOrder, IsActive = x.IsActive })
            .ToListAsync(cancellationToken);

    public async Task<BanquetEventDto> AttachMenuPackageAsync(AttachBanquetMenuRequest request, CancellationToken cancellationToken = default)
    {
        EnsureCanManageBanquet();
        var entity = await _dbContext.BanquetEvents.Include(x => x.Menus).FirstOrDefaultAsync(x => x.Id == request.BanquetEventId, cancellationToken) ?? throw new InvalidOperationException("Không tìm thấy sự kiện tiệc.");
        EnsureFinanciallyEditable(entity);
        var package = await _dbContext.BanquetMenuPackages.AsNoTracking().FirstOrDefaultAsync(x => x.Id == request.BanquetMenuPackageId, cancellationToken) ?? throw new InvalidOperationException("Không tìm thấy thực đơn tiệc.");
        _dbContext.BanquetEventMenus.RemoveRange(entity.Menus.ToArray());
        var amount = package.PricingMode == "PerGuest" ? package.Price * entity.GuestCount : package.Price * (entity.TableCount + entity.ExtraTableCount);
        var eventMenu = new BanquetEventMenu { Id = Guid.NewGuid(), BanquetEventId = entity.Id, BanquetMenuPackageId = package.Id, PricingMode = package.PricingMode, GuestCount = entity.GuestCount, TableCount = entity.TableCount + entity.ExtraTableCount, UnitPrice = package.Price, Amount = amount };
        entity.Menus.Add(eventMenu);
        _dbContext.BanquetEventMenus.Add(eventMenu);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await SyncContractsAsync(entity.Id, cancellationToken);
        return ToDto(await LoadEventAsync(entity.Id, cancellationToken));
    }

    public async Task<BanquetEventDto> SaveCustomMenuAsync(SaveBanquetCustomMenuRequest request, CancellationToken cancellationToken = default)
    {
        EnsureCanManageBanquet();
        if (request.BanquetEventId == Guid.Empty) throw new InvalidOperationException("Thiếu sự kiện tiệc.");
        if (request.Lines.Any(x => x.MenuItemId == Guid.Empty || x.Qty <= 0 || x.UnitPrice < 0 || string.IsNullOrWhiteSpace(x.ItemName))) throw new InvalidOperationException("Dòng món tùy chỉnh không hợp lệ.");
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
        var banquetEvent = await _dbContext.BanquetEvents.FirstOrDefaultAsync(x => x.Id == request.BanquetEventId, cancellationToken)
            ?? throw new InvalidOperationException("Không tìm thấy sự kiện tiệc.");
        EnsureFinanciallyEditable(banquetEvent);
        var existing = await _dbContext.BanquetEventMenus.Where(x => x.BanquetEventId == request.BanquetEventId).ToListAsync(cancellationToken);
        _dbContext.BanquetEventMenus.RemoveRange(existing);
        foreach (var line in request.Lines.GroupBy(x => x.MenuItemId).Select(g => new { Line = g.First(), Qty = g.Sum(x => x.Qty) }))
        {
            _dbContext.BanquetEventMenus.Add(new BanquetEventMenu { Id = Guid.NewGuid(), BanquetEventId = request.BanquetEventId, MenuItemId = line.Line.MenuItemId, ItemName = TrimOrNull(line.Line.ItemName), Qty = line.Qty, UnitPrice = line.Line.UnitPrice, Amount = line.Qty * line.Line.UnitPrice, PricingMode = "Custom", Note = TrimOrNull(line.Line.Note) });
        }
        await _dbContext.SaveChangesAsync(cancellationToken);
        await SyncContractsAsync(request.BanquetEventId, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return ToDto(await LoadEventAsync(request.BanquetEventId, cancellationToken));
    }

    public async Task DetachMenuPackageAsync(Guid banquetEventMenuId, CancellationToken cancellationToken = default)
    {
        EnsureCanManageBanquet();
        var menu = await _dbContext.BanquetEventMenus.FirstOrDefaultAsync(x => x.Id == banquetEventMenuId, cancellationToken)
            ?? throw new InvalidOperationException("Không tìm thấy thực đơn tiệc đã gắn.");
        var eventId = menu.BanquetEventId;
        await EnsureFinanciallyEditableAsync(eventId, cancellationToken);
        _dbContext.BanquetEventMenus.Remove(menu);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await SyncContractsAsync(eventId, cancellationToken);
    }

    public async Task<BanquetServiceLineDto> SaveServiceLineAsync(SaveBanquetServiceLineRequest request, CancellationToken cancellationToken = default)
    {
        EnsureCanManageBanquet();
        if (request.Qty <= 0) throw new InvalidOperationException("Số lượng dịch vụ phải lớn hơn 0.");
        if (request.UnitPrice < 0 || request.CostPrice < 0) throw new InvalidOperationException("Đơn giá bán và giá vốn dịch vụ không được âm.");
        if (string.IsNullOrWhiteSpace(request.Description)) throw new InvalidOperationException("Tên dịch vụ không được để trống.");
        await EnsureFinanciallyEditableAsync(request.BanquetEventId, cancellationToken);
        var line = request.Id.HasValue
            ? await _dbContext.BanquetServiceLines.FirstOrDefaultAsync(x => x.Id == request.Id.Value, cancellationToken) ?? throw new InvalidOperationException("Không tìm thấy dòng dịch vụ.")
            : new BanquetServiceLine { Id = Guid.NewGuid(), BanquetEventId = request.BanquetEventId };
        line.ServiceType = Normalize(request.ServiceType, "Other");
        line.Description = request.Description.Trim();
        line.UnitName = Normalize(request.UnitName, "lần");
        line.Qty = request.Qty;
        line.UnitPrice = request.UnitPrice;
        line.CostPrice = request.CostPrice;
        line.VatRate = request.VatRate;
        line.Amount = Math.Round(request.Qty * request.UnitPrice * (1 + request.VatRate / 100m), 2);
        line.CostAmount = Math.Round(request.Qty * request.CostPrice, 2);
        line.SupplierId = request.SupplierId;
        line.Note = TrimOrNull(request.Note);
        if (!request.Id.HasValue) _dbContext.BanquetServiceLines.Add(line);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await SyncContractsAsync(request.BanquetEventId, cancellationToken);
        return ToDto(line);
    }

    public async Task DeleteServiceLineAsync(Guid banquetServiceLineId, CancellationToken cancellationToken = default)
    {
        EnsureCanManageBanquet();
        var line = await _dbContext.BanquetServiceLines.FirstOrDefaultAsync(x => x.Id == banquetServiceLineId, cancellationToken)
            ?? throw new InvalidOperationException("Không tìm thấy dòng dịch vụ.");
        var eventId = line.BanquetEventId;
        await EnsureFinanciallyEditableAsync(eventId, cancellationToken);
        _dbContext.BanquetServiceLines.Remove(line);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await SyncContractsAsync(eventId, cancellationToken);
    }

    public async Task<BanquetDepositDto> AddDepositAsync(AddBanquetDepositRequest request, CancellationToken cancellationToken = default)
    {
        EnsureCanManageBanquet();
        if (request.Amount <= 0) throw new InvalidOperationException("Số tiền cọc phải lớn hơn 0.");
        var idempotencyKey = TrimOrNull(request.IdempotencyKey);
        if (idempotencyKey is not null)
        {
            var existingDeposit = await _dbContext.BanquetDeposits.AsNoTracking()
                .FirstOrDefaultAsync(x => x.BanquetEventId == request.BanquetEventId && x.IdempotencyKey == idempotencyKey, cancellationToken);
            if (existingDeposit is not null) return ToDto(existingDeposit);
        }
        var eventSummary = await LoadEventAsync(request.BanquetEventId, cancellationToken);
        if (eventSummary is null) throw new InvalidOperationException("Không tìm thấy sự kiện tiệc.");
        var eventDto = ToDto(eventSummary);
        EnsureFinanciallyEditable(eventSummary);
        if (eventDto.Status == "Cancelled") throw new InvalidOperationException("Không thể ghi nhận cọc cho sự kiện đã hủy.");
        if (request.Amount > eventDto.RemainingAmount) throw new InvalidOperationException("Số tiền cọc không được vượt số tiền còn phải thu.");
        if (string.IsNullOrWhiteSpace(request.PaymentMethod)) throw new InvalidOperationException("Vui lòng chọn phương thức thanh toán.");
        var date = request.DepositDate ?? DateTime.Now;
        var cashFundId = request.CashFundId;
        if (string.Equals(request.PaymentMethod, "Cash", StringComparison.OrdinalIgnoreCase) && _cashFundService is not null && !cashFundId.HasValue)
            cashFundId = await ResolveDefaultCashFundIdAsync(cancellationToken);
        var deposit = new BanquetDeposit { Id = Guid.NewGuid(), BanquetEventId = request.BanquetEventId, BanquetContractId = request.BanquetContractId, DepositNo = await GenerateDepositNoAsync(date, cancellationToken), DepositDate = date, Amount = request.Amount, PaymentMethod = request.PaymentMethod.Trim(), CashFundId = cashFundId, BankAccountId = request.BankAccountId, Receiver = TrimOrNull(request.Receiver), Note = TrimOrNull(request.Note), IdempotencyKey = idempotencyKey, Status = "Confirmed" };
        _dbContext.BanquetDeposits.Add(deposit);
        var evt = await _dbContext.BanquetEvents.FirstAsync(x => x.Id == request.BanquetEventId, cancellationToken);
        if (evt.Status == "Confirmed" || evt.Status == "Draft")
        {
            evt.Status = "Deposited";
            evt.UpdatedAt = DateTime.UtcNow;
        }
        await _dbContext.SaveChangesAsync(cancellationToken);
        if (_cashFundService is not null && cashFundId.HasValue && string.Equals(request.PaymentMethod, "Cash", StringComparison.OrdinalIgnoreCase))
        {
            var receipt = await _cashFundService.CreateReceiptAsync(new CashTransactionRequest
            {
                FundId = cashFundId.Value,
                TransactionDate = date,
                Amount = request.Amount,
                Description = $"Thu tiền cọc tiệc {eventDto.EventNo} - {eventDto.CustomerName}",
                PartnerType = "Customer",
                SourceType = "BanquetDeposit",
                SourceId = deposit.Id,
                CreatedBy = request.Receiver
            }, cancellationToken);
            deposit.CashTransactionId = receipt.Id;
            deposit.CashTransactionNo = receipt.TransactionNo;
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        await SyncContractsAsync(request.BanquetEventId, cancellationToken);
        if (_auditService is not null)
            await _auditService.LogAsync("BanquetDeposit", deposit.Id.ToString(), "Created", null, new { deposit.DepositNo, deposit.Amount, deposit.PaymentMethod, deposit.CashTransactionNo }, request.Receiver, cancellationToken: cancellationToken);
        return ToDto(deposit);
    }

    public async Task<BanquetDepositDto> RefundDepositAsync(RefundBanquetDepositRequest request, CancellationToken cancellationToken = default)
    {
        EnsureCanManageBanquet();
        if (request.Amount <= 0m) throw new InvalidOperationException("Số tiền hoàn phải lớn hơn 0.");
        if (string.IsNullOrWhiteSpace(request.Reason)) throw new InvalidOperationException("Vui lòng nhập lý do hoàn cọc.");

        var deposit = await _dbContext.BanquetDeposits
            .FirstOrDefaultAsync(x => x.Id == request.DepositId, cancellationToken)
            ?? throw new InvalidOperationException("Không tìm thấy phiếu đặt cọc.");
        if (request.RowVersion is { Length: > 0 })
            _dbContext.Entry(deposit).Property(x => x.RowVersion).OriginalValue = request.RowVersion;
        var refundable = deposit.Amount - deposit.RefundAmount;
        if (request.Amount > refundable)
            throw new InvalidOperationException($"Số tiền hoàn không được vượt quá {refundable:N0} đ.");

        CashTransactionDto? payment = null;
        if (_cashFundService is not null && deposit.CashFundId.HasValue && string.Equals(deposit.PaymentMethod, "Cash", StringComparison.OrdinalIgnoreCase))
        {
            payment = await _cashFundService.CreatePaymentAsync(new CashTransactionRequest
            {
                FundId = deposit.CashFundId.Value,
                TransactionDate = DateTime.Now,
                Amount = request.Amount,
                Description = $"Hoàn cọc phiếu {deposit.DepositNo}: {request.Reason.Trim()}",
                PartnerType = "Customer",
                SourceType = "BanquetDepositRefund",
                SourceId = deposit.Id,
                CreatedBy = request.RefundedBy
            }, cancellationToken);
        }

        deposit.RefundAmount += request.Amount;
        deposit.RefundedAt = DateTime.Now;
        deposit.RefundedBy = TrimOrNull(request.RefundedBy);
        deposit.RefundReason = request.Reason.Trim();
        deposit.RefundCashTransactionId = payment?.Id;
        deposit.RefundCashTransactionNo = payment?.TransactionNo;
        deposit.Status = deposit.RefundAmount >= deposit.Amount ? "Refunded" : "PartiallyRefunded";
        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new InvalidOperationException("Phiếu đặt cọc vừa được người khác cập nhật. Vui lòng tải lại trước khi hoàn tiền.");
        }
        await SyncContractsAsync(deposit.BanquetEventId, cancellationToken);
        if (_auditService is not null)
            await _auditService.LogAsync("BanquetDeposit", deposit.Id.ToString(), "Refunded", null, new { request.Amount, deposit.RefundAmount, deposit.Status, deposit.RefundReason, deposit.RefundCashTransactionNo }, request.RefundedBy, cancellationToken: cancellationToken);
        return ToDto(deposit);
    }

    public async Task<BanquetContractDto> CreateContractAsync(Guid banquetEventId, DateTime? signDate = null, CancellationToken cancellationToken = default)
    {
        EnsureCanManageBanquet();
        var entity = await LoadEventDetailAsync(banquetEventId, cancellationToken);
        var dto = ToDto(entity);
        var contract = entity.Contracts
            .OrderByDescending(x => x.SignDate)
            .ThenByDescending(x => x.ContractNo)
            .FirstOrDefault();
        if (contract is null)
        {
            contract = new BanquetContract
            {
                Id = Guid.NewGuid(),
                BanquetEventId = entity.Id,
                ContractNo = await GenerateContractNoAsync(DateTime.Today, cancellationToken)
            };
            _dbContext.BanquetContracts.Add(contract);
        }

        contract.SignDate = signDate ?? DateTime.Today;
        contract.CustomerId = entity.CustomerId;
        contract.TotalAmount = dto.TotalAmount;
        contract.DepositAmount = dto.DepositAmount;
        contract.RemainingAmount = dto.RemainingAmount;
        contract.Status = dto.RemainingAmount <= 0m ? "Completed" : "Confirmed";
        await _dbContext.SaveChangesAsync(cancellationToken);
        return ToDto(contract);
    }

    public async Task<BanquetContractDto> UpdateContractAsync(UpdateBanquetContractRequest request, CancellationToken cancellationToken = default)
    {
        EnsureCanManageBanquet();
        var contract = await _dbContext.BanquetContracts
            .FirstOrDefaultAsync(x => x.Id == request.ContractId, cancellationToken)
            ?? throw new InvalidOperationException("Không tìm thấy hợp đồng cần cập nhật.");

        contract.SignDate = request.SignDate ?? contract.SignDate ?? DateTime.Today;
        contract.Status = string.IsNullOrWhiteSpace(request.Status) ? "Confirmed" : request.Status.Trim();
        contract.Note = TrimOrNull(request.Note);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return ToDto(contract);
    }

    public async Task<IReadOnlyList<BanquetChecklistDto>> GetChecklistAsync(Guid banquetEventId, CancellationToken cancellationToken = default)
        => await _dbContext.BanquetChecklists.AsNoTracking().Where(x => x.BanquetEventId == banquetEventId).OrderBy(x => x.SortOrder).Select(x => ToDto(x)).ToListAsync(cancellationToken);

    public async Task<BanquetChecklistDto> SaveChecklistItemAsync(SaveBanquetChecklistItemRequest request, CancellationToken cancellationToken = default)
    {
        EnsureCanManageBanquet();
        if (!await _dbContext.BanquetEvents.AnyAsync(x => x.Id == request.BanquetEventId, cancellationToken)) throw new InvalidOperationException("Không tìm thấy sự kiện tiệc.");
        if (string.IsNullOrWhiteSpace(request.Title)) throw new InvalidOperationException("Tiêu đề checklist không được để trống.");

        var item = request.Id.HasValue
            ? await _dbContext.BanquetChecklists.FirstOrDefaultAsync(x => x.Id == request.Id.Value, cancellationToken) ?? throw new InvalidOperationException("Không tìm thấy checklist.")
            : new BanquetChecklist { Id = Guid.NewGuid(), BanquetEventId = request.BanquetEventId };

        item.Title = request.Title.Trim();
        item.SortOrder = request.SortOrder <= 0
            ? await _dbContext.BanquetChecklists.Where(x => x.BanquetEventId == request.BanquetEventId).CountAsync(cancellationToken) + 1
            : request.SortOrder;
        item.Note = TrimOrNull(request.Note);

        if (!request.Id.HasValue)
        {
            _dbContext.BanquetChecklists.Add(item);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return ToDto(item);
    }

    public async Task<BanquetChecklistDto> ToggleChecklistAsync(Guid checklistId, bool isDone, string? doneBy = null, CancellationToken cancellationToken = default)
    {
        EnsureCanManageBanquet();
        var item = await _dbContext.BanquetChecklists.FirstOrDefaultAsync(x => x.Id == checklistId, cancellationToken) ?? throw new InvalidOperationException("Không tìm thấy checklist.");
        item.IsDone = isDone;
        item.DoneAt = isDone ? DateTime.UtcNow : null;
        item.DoneBy = isDone ? TrimOrNull(doneBy) : null;
        await _dbContext.SaveChangesAsync(cancellationToken);
        return ToDto(item);
    }

    public async Task DeleteChecklistItemAsync(Guid checklistId, CancellationToken cancellationToken = default)
    {
        EnsureCanManageBanquet();
        var item = await _dbContext.BanquetChecklists.FirstOrDefaultAsync(x => x.Id == checklistId, cancellationToken)
            ?? throw new InvalidOperationException("Không tìm thấy checklist.");
        _dbContext.BanquetChecklists.Remove(item);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<BanquetTimelineItemDto>> GetTimelineAsync(Guid banquetEventId, CancellationToken cancellationToken = default)
        => await _dbContext.BanquetTimelines.AsNoTracking()
            .Where(x => x.BanquetEventId == banquetEventId)
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.StartTime)
            .Select(x => ToDto(x))
            .ToListAsync(cancellationToken);

    public async Task<BanquetTimelineItemDto> SaveTimelineItemAsync(SaveBanquetTimelineItemRequest request, CancellationToken cancellationToken = default)
    {
        EnsureCanManageBanquet();
        if (string.IsNullOrWhiteSpace(request.Title)) throw new InvalidOperationException("Tiêu đề timeline không được để trống.");
        var item = request.Id.HasValue
            ? await _dbContext.BanquetTimelines.FirstOrDefaultAsync(x => x.Id == request.Id.Value, cancellationToken) ?? throw new InvalidOperationException("Không tìm thấy timeline.")
            : new BanquetTimeline { Id = Guid.NewGuid(), BanquetEventId = request.BanquetEventId };
        item.StartTime = request.StartTime;
        item.Title = request.Title.Trim();
        item.Description = TrimOrNull(request.Description);
        item.ResponsiblePerson = TrimOrNull(request.ResponsiblePerson);
        item.SortOrder = request.SortOrder;
        item.Status = Normalize(request.Status, "Planned");
        if (!request.Id.HasValue) _dbContext.BanquetTimelines.Add(item);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return ToDto(item);
    }

    public async Task<BanquetTimelineItemDto> UpdateTimelineItemStatusAsync(Guid timelineItemId, string status, CancellationToken cancellationToken = default)
    {
        EnsureCanManageBanquet();
        var item = await _dbContext.BanquetTimelines.FirstOrDefaultAsync(x => x.Id == timelineItemId, cancellationToken)
            ?? throw new InvalidOperationException("Không tìm thấy timeline.");
        item.Status = Normalize(status, item.Status);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return ToDto(item);
    }

    public async Task DeleteTimelineItemAsync(Guid timelineItemId, CancellationToken cancellationToken = default)
    {
        EnsureCanManageBanquet();
        var item = await _dbContext.BanquetTimelines.FirstOrDefaultAsync(x => x.Id == timelineItemId, cancellationToken)
            ?? throw new InvalidOperationException("Không tìm thấy timeline.");
        _dbContext.BanquetTimelines.Remove(item);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<BanquetQuantificationBatchDto>> GetQuantificationBatchesAsync(Guid? banquetEventId = null, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.BanquetQuantificationBatches.AsNoTracking().Include(x => x.Lines).AsQueryable();
        if (banquetEventId.HasValue) query = query.Where(x => x.BanquetEventId == banquetEventId.Value);
        return await query.OrderByDescending(x => x.SentAt).ThenByDescending(x => x.VersionNo)
            .Select(x => new BanquetQuantificationBatchDto
            {
                Id = x.Id,
                BanquetEventId = x.BanquetEventId,
                VersionNo = x.VersionNo,
                Fingerprint = x.Fingerprint,
                Status = x.Status,
                SentAt = x.SentAt,
                SentBy = x.SentBy,
                ReceivedAt = x.ReceivedAt,
                ReceivedBy = x.ReceivedBy,
                CompletedAt = x.CompletedAt,
                CompletedBy = x.CompletedBy,
                Note = x.Note,
                Lines = x.Lines.OrderBy(l => l.SortOrder).Select(l => new BanquetQuantificationLineDto
                {
                    SortOrder = l.SortOrder,
                    LineType = l.LineType,
                    Code = l.Code,
                    Name = l.Name,
                    UnitName = l.UnitName,
                    RequiredQuantity = l.RequiredQuantity,
                    EstimatedCost = l.EstimatedCost,
                    SourceText = l.SourceText,
                    Note = l.Note
                }).ToArray()
            }).ToListAsync(cancellationToken);
    }

    public async Task<BanquetQuantificationBatchDto> SaveQuantificationBatchAsync(SaveBanquetQuantificationBatchRequest request, CancellationToken cancellationToken = default)
    {
        EnsureCanManageBanquet();
        if (request.BanquetEventId == Guid.Empty) throw new InvalidOperationException("Thiếu sự kiện cần gửi định lượng.");
        if (string.IsNullOrWhiteSpace(request.Fingerprint)) throw new InvalidOperationException("Thiếu dấu vết phiên bản định lượng.");
        if (!await _dbContext.BanquetEvents.AnyAsync(x => x.Id == request.BanquetEventId, cancellationToken))
            throw new InvalidOperationException("Không tìm thấy sự kiện tiệc.");

        var version = (await _dbContext.BanquetQuantificationBatches
            .Where(x => x.BanquetEventId == request.BanquetEventId)
            .MaxAsync(x => (int?)x.VersionNo, cancellationToken) ?? 0) + 1;
        var entity = new BanquetQuantificationBatch
        {
            Id = Guid.NewGuid(),
            BanquetEventId = request.BanquetEventId,
            VersionNo = version,
            Fingerprint = request.Fingerprint.Trim(),
            Status = "Sent",
            SentAt = DateTime.UtcNow,
            SentBy = TrimOrNull(request.SentBy),
            Note = TrimOrNull(request.Note)
        };
        var order = 1;
        foreach (var line in request.Lines)
        {
            entity.Lines.Add(new BanquetQuantificationLine
            {
                Id = Guid.NewGuid(),
                SortOrder = order++,
                LineType = Normalize(line.LineType, "Ingredient"),
                Code = line.Code?.Trim() ?? string.Empty,
                Name = line.Name?.Trim() ?? string.Empty,
                UnitName = line.UnitName?.Trim() ?? string.Empty,
                RequiredQuantity = line.RequiredQuantity,
                EstimatedCost = line.EstimatedCost,
                SourceText = TrimOrNull(line.SourceText),
                Note = TrimOrNull(line.Note)
            });
        }
        _dbContext.BanquetQuantificationBatches.Add(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);
        if (_auditService is not null)
            await _auditService.LogAsync("BanquetQuantificationBatch", entity.Id.ToString(), "Sent", null, new { entity.BanquetEventId, entity.VersionNo, LineCount = entity.Lines.Count }, request.SentBy, cancellationToken: cancellationToken);
        return (await GetQuantificationBatchesAsync(request.BanquetEventId, cancellationToken)).First(x => x.Id == entity.Id);
    }

    public async Task<BanquetQuantificationBatchDto> UpdateQuantificationStatusAsync(Guid batchId, string status, string? handledBy = null, CancellationToken cancellationToken = default)
    {
        EnsureCanManageBanquet();
        var normalized = Normalize(status, "Received");
        if (normalized is not ("Received" or "Completed")) throw new InvalidOperationException("Trạng thái định lượng không hợp lệ.");
        var entity = await _dbContext.BanquetQuantificationBatches.FirstOrDefaultAsync(x => x.Id == batchId, cancellationToken)
            ?? throw new InvalidOperationException("Không tìm thấy phiên bản định lượng.");
        var previousStatus = entity.Status;
        entity.Status = normalized;
        if (normalized == "Received")
        {
            entity.ReceivedAt = DateTime.UtcNow;
            entity.ReceivedBy = TrimOrNull(handledBy);
        }
        else
        {
            entity.CompletedAt = DateTime.UtcNow;
            entity.CompletedBy = TrimOrNull(handledBy);
        }
        await _dbContext.SaveChangesAsync(cancellationToken);
        if (_auditService is not null)
            await _auditService.LogAsync("BanquetQuantificationBatch", entity.Id.ToString(), "StatusChanged", new { Status = previousStatus }, new { Status = normalized }, handledBy, cancellationToken: cancellationToken);
        return (await GetQuantificationBatchesAsync(entity.BanquetEventId, cancellationToken)).First(x => x.Id == entity.Id);
    }

    public async Task<IReadOnlyList<BanquetPurchaseRequestDto>> GetPurchaseRequestsAsync(Guid? banquetEventId = null, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.BanquetPurchaseRequests.AsNoTracking().Include(x => x.Lines).AsQueryable();
        if (banquetEventId.HasValue) query = query.Where(x => x.BanquetEventId == banquetEventId.Value);
        var entities = await query.OrderByDescending(x => x.RequestedAt).ToListAsync(cancellationToken);
        return entities.Select(ToDto).ToList();
    }

    public async Task<BanquetPurchaseRequestDto> CreatePurchaseRequestAsync(Guid quantificationBatchId, string? requestedBy = null, CancellationToken cancellationToken = default)
    {
        EnsureCanManageBanquet();
        var existing = await _dbContext.BanquetPurchaseRequests.AsNoTracking().Include(x => x.Lines)
            .FirstOrDefaultAsync(x => x.BanquetQuantificationBatchId == quantificationBatchId, cancellationToken);
        if (existing is not null) return ToDto(existing);

        var batch = await _dbContext.BanquetQuantificationBatches.AsNoTracking()
            .Include(x => x.Event)
            .Include(x => x.Lines)
            .FirstOrDefaultAsync(x => x.Id == quantificationBatchId, cancellationToken)
            ?? throw new InvalidOperationException("Không tìm thấy phiên bản định lượng.");
        var ingredientLines = batch.Lines
            .Where(x => x.LineType == "Ingredient" && x.RequiredQuantity > 0m)
            .OrderBy(x => x.SortOrder)
            .ToArray();
        if (ingredientLines.Length == 0)
            throw new InvalidOperationException("Phiên bản định lượng chưa có nguyên liệu để tạo yêu cầu mua hàng.");

        var now = DateTime.UtcNow;
        var sequence = await _dbContext.BanquetPurchaseRequests.CountAsync(x => x.RequestedAt.Date == now.Date, cancellationToken) + 1;
        var entity = new BanquetPurchaseRequest
        {
            Id = Guid.NewGuid(),
            RequestNo = $"BPR-{now:yyyyMMdd}-{sequence:0000}",
            BanquetEventId = batch.BanquetEventId,
            BanquetQuantificationBatchId = batch.Id,
            RequiredDate = (batch.Event?.EventDate.Date.AddDays(-1) ?? DateTime.Today),
            Status = "Requested",
            RequestedAt = now,
            RequestedBy = TrimOrNull(requestedBy),
            Note = $"Tự động tạo từ định lượng lần {batch.VersionNo}."
        };
        var order = 1;
        foreach (var line in ingredientLines)
        {
            entity.Lines.Add(new BanquetPurchaseRequestLine
            {
                Id = Guid.NewGuid(), SortOrder = order++, ItemCode = line.Code, ItemName = line.Name,
                UnitName = line.UnitName, RequestedQuantity = line.RequiredQuantity,
                EstimatedCost = line.EstimatedCost, Note = line.Note
            });
        }
        _dbContext.BanquetPurchaseRequests.Add(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);
        if (_auditService is not null)
            await _auditService.LogAsync("BanquetPurchaseRequest", entity.Id.ToString(), "Created", null, new { entity.RequestNo, entity.BanquetEventId, entity.BanquetQuantificationBatchId, LineCount = entity.Lines.Count }, requestedBy, cancellationToken: cancellationToken);
        return ToDto(entity);
    }

    public async Task<BanquetPurchaseRequestDto> UpdatePurchaseRequestStatusAsync(Guid requestId, string status, string? handledBy = null, CancellationToken cancellationToken = default)
    {
        EnsureCanManageBanquet();
        var target = Normalize(status, "Requested");
        if (target is not ("Requested" or "Approved" or "Ordered" or "Received" or "Cancelled"))
            throw new InvalidOperationException("Trạng thái yêu cầu mua hàng không hợp lệ.");
        var entity = await _dbContext.BanquetPurchaseRequests.Include(x => x.Lines)
            .FirstOrDefaultAsync(x => x.Id == requestId, cancellationToken)
            ?? throw new InvalidOperationException("Không tìm thấy yêu cầu mua hàng.");
        var previous = entity.Status;
        entity.Status = target;
        await _dbContext.SaveChangesAsync(cancellationToken);
        if (_auditService is not null)
            await _auditService.LogAsync("BanquetPurchaseRequest", entity.Id.ToString(), "StatusChanged", new { Status = previous }, new { Status = target }, handledBy, cancellationToken: cancellationToken);
        return ToDto(entity);
    }

    public async Task<BanquetModuleSettingsDto> GetModuleSettingsAsync(CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.BanquetModuleSettings.AsNoTracking().FirstOrDefaultAsync(cancellationToken);
        return entity is null ? new BanquetModuleSettingsDto() : ToDto(entity);
    }

    public async Task<BanquetModuleSettingsDto> SaveModuleSettingsAsync(BanquetModuleSettingsDto request, CancellationToken cancellationToken = default)
    {
        EnsureCanManageBanquet();
        if (string.IsNullOrWhiteSpace(request.VenueName)) throw new InvalidOperationException("Tên đơn vị không được để trống.");
        var entity = await _dbContext.BanquetModuleSettings.FirstOrDefaultAsync(cancellationToken) ?? new BanquetModuleSetting();
        entity.VenueName = request.VenueName.Trim();
        entity.VenueCode = request.VenueCode.Trim();
        entity.Address = request.Address.Trim();
        entity.Phone = request.Phone.Trim();
        entity.Email = request.Email.Trim();
        entity.Website = request.Website.Trim();
        entity.TaxCode = request.TaxCode.Trim();
        entity.GuestsPerTable = request.GuestsPerTable;
        entity.DefaultDepositRate = request.DefaultDepositRate;
        entity.DefaultVatRate = request.DefaultVatRate;
        entity.HoldDays = request.HoldDays;
        entity.ReminderDays = request.ReminderDays;
        entity.LunchStartTime = request.LunchStartTime.Trim();
        entity.DinnerStartTime = request.DinnerStartTime.Trim();
        entity.AutoGenerateEventNo = request.AutoGenerateEventNo;
        entity.WarnRoomConflict = request.WarnRoomConflict;
        entity.RequireContractBeforeConfirm = request.RequireContractBeforeConfirm;
        entity.EnableDepositReminder = request.EnableDepositReminder;
        entity.UpdatedAt = DateTime.UtcNow;
        if (entity.Id == Guid.Empty) entity.Id = Guid.NewGuid();
        if (_dbContext.Entry(entity).State == EntityState.Detached) _dbContext.BanquetModuleSettings.Add(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);
        if (_auditService is not null)
            await _auditService.LogAsync("BanquetModuleSetting", entity.Id.ToString(), "Saved", null, request, cancellationToken: cancellationToken);
        return ToDto(entity);
    }

    private static BanquetModuleSettingsDto ToDto(BanquetModuleSetting x) => new()
    {
        VenueName = x.VenueName,
        VenueCode = x.VenueCode,
        Address = x.Address,
        Phone = x.Phone,
        Email = x.Email,
        Website = x.Website,
        TaxCode = x.TaxCode,
        GuestsPerTable = x.GuestsPerTable,
        DefaultDepositRate = x.DefaultDepositRate,
        DefaultVatRate = x.DefaultVatRate,
        HoldDays = x.HoldDays,
        ReminderDays = x.ReminderDays,
        LunchStartTime = x.LunchStartTime,
        DinnerStartTime = x.DinnerStartTime,
        AutoGenerateEventNo = x.AutoGenerateEventNo,
        WarnRoomConflict = x.WarnRoomConflict,
        RequireContractBeforeConfirm = x.RequireContractBeforeConfirm,
        EnableDepositReminder = x.EnableDepositReminder
    };

    public async Task<IReadOnlyList<BanquetFloorPlanDto>> GetFloorPlansAsync(Guid banquetEventId, CancellationToken cancellationToken = default)
        => await _dbContext.BanquetFloorPlans.AsNoTracking()
            .Where(x => x.BanquetEventId == banquetEventId)
            .OrderByDescending(x => x.UpdatedAt)
            .Select(x => new BanquetFloorPlanDto
            {
                Id = x.Id, BanquetEventId = x.BanquetEventId, Name = x.Name,
                CanvasWidth = x.CanvasWidth, CanvasHeight = x.CanvasHeight,
                LayoutJson = x.LayoutJson, UpdatedAt = x.UpdatedAt
            }).ToListAsync(cancellationToken);

    public async Task<BanquetFloorPlanDto> SaveFloorPlanAsync(SaveBanquetFloorPlanRequest request, CancellationToken cancellationToken = default)
    {
        EnsureCanManageBanquet();
        if (request.BanquetEventId == Guid.Empty) throw new InvalidOperationException("Chọn tiệc trước khi lưu sơ đồ.");
        if (!await _dbContext.BanquetEvents.AnyAsync(x => x.Id == request.BanquetEventId, cancellationToken))
            throw new InvalidOperationException("Không tìm thấy sự kiện tiệc.");
        var entity = request.Id.HasValue
            ? await _dbContext.BanquetFloorPlans.FirstOrDefaultAsync(x => x.Id == request.Id.Value, cancellationToken)
                ?? throw new InvalidOperationException("Không tìm thấy sơ đồ tiệc.")
            : new BanquetFloorPlan { Id = Guid.NewGuid(), BanquetEventId = request.BanquetEventId };
        entity.Name = string.IsNullOrWhiteSpace(request.Name) ? "Sơ đồ chốt" : request.Name.Trim();
        entity.CanvasWidth = Math.Max(320, request.CanvasWidth);
        entity.CanvasHeight = Math.Max(240, request.CanvasHeight);
        entity.LayoutJson = request.LayoutJson;
        entity.UpdatedAt = DateTime.UtcNow;
        if (!request.Id.HasValue) _dbContext.BanquetFloorPlans.Add(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);
        if (_auditService is not null)
            await _auditService.LogAsync("BanquetFloorPlan", entity.Id.ToString(), request.Id.HasValue ? "Updated" : "Created", null, new { entity.BanquetEventId, entity.Name, entity.CanvasWidth, entity.CanvasHeight }, cancellationToken: cancellationToken);
        return new BanquetFloorPlanDto
        {
            Id = entity.Id, BanquetEventId = entity.BanquetEventId, Name = entity.Name,
            CanvasWidth = entity.CanvasWidth, CanvasHeight = entity.CanvasHeight,
            LayoutJson = entity.LayoutJson, UpdatedAt = entity.UpdatedAt
        };
    }

    private async Task<BanquetEvent> LoadEventAsync(Guid id, CancellationToken cancellationToken)
        => await _dbContext.BanquetEvents
            .AsSplitQuery()
            .Include(x => x.Room)
            .Include(x => x.Menus)
            .Include(x => x.Services)
            .Include(x => x.Deposits)
            .Include(x => x.Contracts)
            .Include(x => x.PurchaseRequests).ThenInclude(x => x.Lines)
            .FirstAsync(x => x.Id == id, cancellationToken);

    private async Task<BanquetEvent> LoadEventDetailAsync(Guid id, CancellationToken cancellationToken)
        => await _dbContext.BanquetEvents
            .AsSplitQuery()
            .Include(x => x.Room)
            .Include(x => x.Menus).ThenInclude(x => x.Package).ThenInclude(x => x!.Lines).ThenInclude(x => x.MenuItem)
            .Include(x => x.Menus).ThenInclude(x => x.MenuItem)
            .Include(x => x.Services)
            .Include(x => x.Deposits)
            .Include(x => x.Contracts)
            .Include(x => x.ChecklistItems)
            .Include(x => x.TimelineItems)
            .Include(x => x.PurchaseRequests).ThenInclude(x => x.Lines)
            .FirstAsync(x => x.Id == id, cancellationToken);

    private async Task<int> CountRoomConflictsAsync(DateTime from, DateTime to, CancellationToken cancellationToken)
    {
        var events = await _dbContext.BanquetEvents.AsNoTracking().Where(x => x.EventDate >= from && x.EventDate < to && x.BanquetRoomId.HasValue && x.Status != "Cancelled").ToListAsync(cancellationToken);
        return events.Count(a => events.Any(b => a.Id != b.Id && a.EventDate == b.EventDate && a.BanquetRoomId == b.BanquetRoomId && a.StartTime < b.EndTime && a.EndTime > b.StartTime));
    }

    private static void AddDefaultChecklist(BanquetEvent entity)
    {
        string[] titles = ["Đã chốt thực đơn", "Đã nhận cọc", "Đã xác nhận khách", "Đã chuẩn bị phòng", "Đã chuẩn bị âm thanh", "Đã chuẩn bị màn LED", "Đã chuẩn bị nhân sự", "Đã chuẩn bị sân khấu", "Đã hoàn tất thanh toán"];
        for (var i = 0; i < titles.Length; i++) entity.ChecklistItems.Add(new BanquetChecklist { Id = Guid.NewGuid(), BanquetEventId = entity.Id, Title = titles[i], SortOrder = i + 1 });
    }

    private static void AddDefaultTimeline(BanquetEvent entity)
    {
        var start = entity.StartTime;
        var end = entity.EndTime;
        var items = new[]
        {
            new { Start = start.Add(TimeSpan.FromMinutes(-30)), Title = "Đón khách", Description = "Lễ tân và host đón khách tại sảnh." },
            new { Start = start, Title = "Khai tiệc", Description = "MC giới thiệu chương trình và khai mạc." },
            new { Start = start.Add(TimeSpan.FromMinutes(20)), Title = "Phục vụ món khai vị", Description = "Bếp và phục vụ ra món đầu tiên." },
            new { Start = start.Add(TimeSpan.FromMinutes(60)), Title = "Nghi thức chính", Description = "Thực hiện nghi thức chính của buổi tiệc." },
            new { Start = end, Title = "Kết thúc tiệc", Description = "Tổng kết chương trình và tiễn khách." }
        };

        for (var i = 0; i < items.Length; i++)
        {
            entity.TimelineItems.Add(new BanquetTimeline
            {
                Id = Guid.NewGuid(),
                BanquetEventId = entity.Id,
                StartTime = items[i].Start,
                Title = items[i].Title,
                Description = items[i].Description,
                SortOrder = i + 1,
                Status = "Planned"
            });
        }
    }

    private async Task SyncContractsAsync(Guid banquetEventId, CancellationToken cancellationToken)
    {
        var entity = await _dbContext.BanquetEvents
            .AsSplitQuery()
            .Include(x => x.Menus)
            .Include(x => x.Services)
            .Include(x => x.Deposits)
            .Include(x => x.Contracts)
            .FirstOrDefaultAsync(x => x.Id == banquetEventId, cancellationToken);
        if (entity is null || entity.Contracts.Count == 0)
        {
            return;
        }

        var dto = ToDto(entity);
        foreach (var contract in entity.Contracts)
        {
            contract.TotalAmount = dto.TotalAmount;
            contract.DepositAmount = dto.DepositAmount;
            contract.RemainingAmount = dto.RemainingAmount;
            contract.Status = dto.RemainingAmount <= 0m ? "Completed" : (entity.Status == "Cancelled" ? "Cancelled" : "Confirmed");
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<string> GenerateEventNoAsync(DateTime date, CancellationToken cancellationToken) => $"BQT-{date:yyyyMMdd}-{await _dbContext.BanquetEvents.CountAsync(x => x.EventDate == date.Date, cancellationToken) + 1:0000}";
    private async Task<string> GenerateDepositNoAsync(DateTime date, CancellationToken cancellationToken) => $"BDP-{date:yyyyMMdd}-{await _dbContext.BanquetDeposits.CountAsync(x => x.DepositDate.Date == date.Date, cancellationToken) + 1:0000}";
    private async Task<string> GenerateContractNoAsync(DateTime date, CancellationToken cancellationToken) => $"BCT-{date:yyyyMMdd}-{await _dbContext.BanquetContracts.CountAsync(x => x.SignDate != null && x.SignDate.Value.Date == date.Date, cancellationToken) + 1:0000}";
    private async Task<Guid> ResolveDefaultCashFundIdAsync(CancellationToken cancellationToken)
    {
        var fundId = await _dbContext.CashFunds.AsNoTracking()
            .Where(x => x.IsActive)
            .OrderByDescending(x => x.IsDefault)
            .ThenBy(x => x.FundCode)
            .Select(x => (Guid?)x.Id)
            .FirstOrDefaultAsync(cancellationToken);
        return fundId ?? throw new InvalidOperationException("Chưa cấu hình quỹ tiền mặt mặc định. Hãy tạo hoặc kích hoạt quỹ trước khi thu tiền mặt.");
    }
    private static string Normalize(string? value, string fallback) => string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
    private static string? TrimOrNull(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static string ValidatePricingRule(string? value)
    {
        if (string.Equals(value, "PerGuest", StringComparison.OrdinalIgnoreCase)) return "PerGuest";
        if (string.Equals(value, "PerPerson", StringComparison.OrdinalIgnoreCase)) return "PerPerson";
        if (string.Equals(value, "PerTable", StringComparison.OrdinalIgnoreCase)) return "PerTable";
        if (string.Equals(value, "PerRoom", StringComparison.OrdinalIgnoreCase)) return "PerRoom";
        return "Manual";
    }

    private static BanquetRoomDto ToDto(BanquetRoom x) => new() { Id = x.Id, Code = x.Code, Name = x.Name, Capacity = x.Capacity, MaxTables = x.MaxTables, Description = x.Description, LayoutJson = x.LayoutJson, Status = x.Status, IsActive = x.IsActive };
    private static BanquetEventDto ToDto(BanquetEvent x)
    {
        var menu = x.Menus.Sum(m => m.PricingMode == "Custom"
            ? m.Amount * (x.TableCount + x.ExtraTableCount)
            : m.Amount);
        var menuMain = x.Menus.Sum(m => m.PricingMode == "Custom"
            ? m.Amount * x.TableCount
            : m.PricingMode == "PerGuest" ? m.Amount : m.UnitPrice * x.TableCount);
        var service = x.Services.Sum(s => s.Amount);
        var serviceBase = x.Services.Sum(s => s.Qty * s.UnitPrice);
        var serviceEstimatedCost = x.Services.Sum(s => s.CostAmount);
        var ingredientEstimatedCost = x.PurchaseRequests
            .OrderByDescending(p => p.RequestedAt)
            .FirstOrDefault()?.Lines.Sum(l => l.EstimatedCost) ?? 0m;
        var deposits = x.Deposits
            .Where(d => d.Status is "Confirmed" or "PartiallyRefunded" or "Refunded")
            .Sum(d => Math.Max(0m, d.Amount - d.RefundAmount));
        var total = menu + service;
        return new BanquetEventDto { Id = x.Id, EventNo = x.EventNo, EventType = x.EventType, CustomerId = x.CustomerId, CustomerName = x.CustomerName, Phone = x.Phone, EventDate = x.EventDate, StartTime = x.StartTime, EndTime = x.EndTime, GuestCount = x.GuestCount, TableCount = x.TableCount, ExtraGuestCount = x.ExtraGuestCount, ExtraTableCount = x.ExtraTableCount, BrideName = x.BrideName, GroomName = x.GroomName, ConferenceName = x.ConferenceName, BirthdayPersonName = x.BirthdayPersonName, BirthdayDate = x.BirthdayDate, MeetingTopic = x.MeetingTopic, PartyTheme = x.PartyTheme, OtherEventName = x.OtherEventName, BanquetRoomId = x.BanquetRoomId, RoomName = x.Room?.Name, Status = x.Status, IsFinanciallyLocked = x.IsFinanciallyLocked, FinanciallyLockedAt = x.FinanciallyLockedAt, FinanciallyLockedBy = x.FinanciallyLockedBy, RowVersion = x.RowVersion, Note = x.Note, MenuAmount = menu, MenuMainAmount = menuMain, MenuExtraAmount = Math.Max(0m, menu - menuMain), ServiceAmount = service, ServiceBaseAmount = serviceBase, VatAmount = Math.Max(0m, service - serviceBase), DepositAmount = deposits, TotalAmount = total, RemainingAmount = Math.Max(0, total - deposits), IngredientEstimatedCost = ingredientEstimatedCost, ServiceEstimatedCost = serviceEstimatedCost };
    }
    private static BanquetMenuPackageDto ToDto(BanquetMenuPackage x) => new()
    {
        Id = x.Id,
        Code = x.Code,
        Name = x.Name,
        GroupName = x.Note,
        PricingMode = x.PricingMode,
        Price = x.Price,
        VatRate = x.VatRate,
        IsActive = x.IsActive,
        LineCount = x.Lines.Count,
        Lines = x.Lines
            .OrderBy(l => l.LineNo)
            .Select(l => new BanquetMenuPackageLineDto
            {
                Id = l.Id,
                MenuItemId = l.MenuItemId,
                LineNo = l.LineNo,
                CourseType = l.CourseType,
                PricingRule = string.IsNullOrWhiteSpace(l.PricingRule) ? x.PricingMode : l.PricingRule!,
                Qty = l.Qty,
                MenuItemCode = l.MenuItem?.Code ?? string.Empty,
                MenuItemName = l.MenuItem?.Name ?? string.Empty,
                UnitName = l.MenuItem?.UnitName ?? string.Empty,
                UnitPrice = l.MenuItem?.UnitPrice ?? 0m,
                Note = l.Note
            })
            .ToList()
    };
    private static BanquetServiceLineDto ToDto(BanquetServiceLine x) => new() { Id = x.Id, BanquetEventId = x.BanquetEventId, ServiceType = x.ServiceType, Description = x.Description, UnitName = Normalize(x.UnitName, "lần"), Qty = x.Qty, UnitPrice = x.UnitPrice, VatRate = x.VatRate, Amount = x.Amount, CostPrice = x.CostPrice, CostAmount = x.CostAmount };
    private static BanquetDepositDto ToDto(BanquetDeposit x) => new() { Id = x.Id, DepositNo = x.DepositNo, BanquetEventId = x.BanquetEventId, BanquetContractId = x.BanquetContractId, DepositDate = x.DepositDate, Amount = x.Amount, PaymentMethod = x.PaymentMethod, CashTransactionId = x.CashTransactionId, CashTransactionNo = x.CashTransactionNo, RefundAmount = x.RefundAmount, RefundedAt = x.RefundedAt, RefundedBy = x.RefundedBy, RefundReason = x.RefundReason, RefundCashTransactionNo = x.RefundCashTransactionNo, RowVersion = x.RowVersion, Receiver = x.Receiver, Note = x.Note, Status = x.Status };

    private static void ValidateEventStatusTransition(string current, string target)
    {
        if (string.Equals(current, target, StringComparison.OrdinalIgnoreCase)) return;
        var allowed = current switch
        {
            "Draft" => new[] { "Hold", "Confirmed", "Cancelled" },
            "Hold" => new[] { "Draft", "Confirmed", "Cancelled" },
            "Confirmed" => new[] { "Deposited", "Preparing", "Cancelled" },
            "Deposited" => new[] { "Preparing", "Cancelled" },
            "Preparing" => new[] { "Completed", "Cancelled" },
            "Completed" => Array.Empty<string>(),
            "Cancelled" => Array.Empty<string>(),
            _ => new[] { "Draft", "Cancelled" }
        };
        if (!allowed.Contains(target, StringComparer.OrdinalIgnoreCase))
            throw new InvalidOperationException($"Không thể chuyển trạng thái từ {current} sang {target}.");
    }
    private static BanquetContractDto ToDto(BanquetContract x) => new() { Id = x.Id, ContractNo = x.ContractNo, BanquetEventId = x.BanquetEventId, SignDate = x.SignDate, TotalAmount = x.TotalAmount, DepositAmount = x.DepositAmount, RemainingAmount = x.RemainingAmount, Status = x.Status, Note = NormalizeLegacyVietnameseText(x.Note), RowVersion = x.RowVersion };
    private static BanquetPurchaseRequestDto ToDto(BanquetPurchaseRequest x) => new()
    {
        Id = x.Id, RequestNo = x.RequestNo, BanquetEventId = x.BanquetEventId,
        BanquetQuantificationBatchId = x.BanquetQuantificationBatchId, RequiredDate = x.RequiredDate,
        Status = x.Status, RequestedAt = x.RequestedAt, RequestedBy = x.RequestedBy, Note = x.Note,
        EstimatedTotal = x.Lines.Sum(l => l.EstimatedCost),
        Lines = x.Lines.OrderBy(l => l.SortOrder).Select(l => new BanquetPurchaseRequestLineDto
        {
            SortOrder = l.SortOrder, ItemCode = l.ItemCode, ItemName = l.ItemName,
            UnitName = l.UnitName, RequestedQuantity = l.RequestedQuantity,
            EstimatedCost = l.EstimatedCost, Note = l.Note
        }).ToList()
    };

    private static void EnsureFinanciallyEditable(BanquetEvent entity)
    {
        if (entity.IsFinanciallyLocked || entity.Status == "Completed")
            throw new InvalidOperationException("Tiệc đã khóa quyết toán. Không thể thay đổi thực đơn, dịch vụ hoặc tài chính.");
    }

    private void EnsureCanManageBanquet()
    {
        if (_currentUserService?.IsAuthenticated == true && !_currentUserService.HasPermission("restaurant.banquet.manage"))
            throw new UnauthorizedAccessException("Bạn không có quyền quản lý Nhà hàng tiệc.");
    }

    private async Task EnsureFinanciallyEditableAsync(Guid banquetEventId, CancellationToken cancellationToken)
    {
        var entity = await _dbContext.BanquetEvents.FirstOrDefaultAsync(x => x.Id == banquetEventId, cancellationToken)
            ?? throw new InvalidOperationException("Không tìm thấy sự kiện tiệc.");
        EnsureFinanciallyEditable(entity);
    }
    private static BanquetChecklistDto ToDto(BanquetChecklist x) => new() { Id = x.Id, BanquetEventId = x.BanquetEventId, Title = NormalizeLegacyVietnameseText(x.Title) ?? string.Empty, IsDone = x.IsDone, DoneAt = x.DoneAt, DoneBy = NormalizeLegacyVietnameseText(x.DoneBy), SortOrder = x.SortOrder, Note = NormalizeLegacyVietnameseText(x.Note) };
    private static BanquetEventMenuDto ToDto(BanquetEventMenu x) => new()
    {
        Id = x.Id,
        BanquetEventId = x.BanquetEventId,
        BanquetMenuPackageId = x.BanquetMenuPackageId,
        MenuItemId = x.MenuItemId,
        ItemName = x.ItemName ?? x.MenuItem?.Name,
        Qty = x.Qty,
        Note = x.Note,
        PackageCode = x.Package?.Code,
        PackageName = x.Package?.Name,
        PricingMode = x.PricingMode,
        TableCount = x.TableCount,
        GuestCount = x.GuestCount,
        UnitPrice = x.UnitPrice,
        Amount = x.Amount,
        PackageLines = x.Package is null ? Array.Empty<BanquetMenuPackageLineDto>() : ToDto(x.Package).Lines
    };
    private static BanquetTimelineItemDto ToDto(BanquetTimeline x) => new()
    {
        Id = x.Id,
        BanquetEventId = x.BanquetEventId,
        StartTime = x.StartTime,
        Title = NormalizeLegacyVietnameseText(x.Title) ?? string.Empty,
        Description = NormalizeLegacyVietnameseText(x.Description),
        ResponsiblePerson = NormalizeLegacyVietnameseText(x.ResponsiblePerson),
        SortOrder = x.SortOrder,
        Status = x.Status
    };
    private static BanquetEventDetailDto ToDetailDto(BanquetEvent x) => new()
    {
        Event = ToDto(x),
        Menus = x.Menus.OrderByDescending(m => m.Amount).ThenBy(m => m.PricingMode).Select(ToDto).ToList(),
        Services = x.Services.OrderByDescending(s => s.Amount).ThenBy(s => s.Description).Select(ToDto).ToList(),
        Deposits = x.Deposits.OrderByDescending(d => d.DepositDate).ThenByDescending(d => d.DepositNo).Select(ToDto).ToList(),
        Contracts = x.Contracts.OrderByDescending(c => c.SignDate).ThenByDescending(c => c.ContractNo).Select(ToDto).ToList(),
        Checklist = x.ChecklistItems.OrderBy(c => c.SortOrder).Select(ToDto).ToList(),
        Timeline = x.TimelineItems.OrderBy(t => t.SortOrder).ThenBy(t => t.StartTime).Select(ToDto).ToList()
    };

    private static string? NormalizeLegacyVietnameseText(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        if (!LooksLikeLegacyMojibake(value))
        {
            return value;
        }

        try
        {
            var latin1Bytes = Encoding.GetEncoding("ISO-8859-1").GetBytes(value);
            var utf8Value = Encoding.UTF8.GetString(latin1Bytes);
            if (!string.IsNullOrWhiteSpace(utf8Value) && !LooksLikeLegacyMojibake(utf8Value))
            {
                return utf8Value;
            }
        }
        catch
        {
        }

        return value;
    }

    private static bool LooksLikeLegacyMojibake(string value)
    {
        return value.Contains('Ã')
               || value.Contains('Ä')
               || value.Contains('Å')
               || value.Contains('Æ')
               || value.Contains('Ð')
               || value.Contains('¡')
               || value.Contains('¢')
               || value.Contains('£')
               || value.Contains('¤')
               || value.Contains('¥')
               || value.Contains('¦')
               || value.Contains('§');
    }

}
