using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ASMILE.BO.Application.DTOs;
using ASMILE.BO.Application.Interfaces;
using ASMILE.BO.Core.Entities;
using ASMILE.BO.Core.Enums;
using ASMILE.BO.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ASMILE.BO.Infrastructure.Services;

public sealed class BreakfastCafeSalesService : IBreakfastCafeSalesService
{
	private const string SourceType = "BreakfastCafeOrder";

	private readonly AppDbContext _dbContext;

	private readonly IMenuCatalogService _menuCatalogService;

	private readonly IJournalService _journalService;

	private readonly IPostingRuleService _postingRuleService;

	private readonly IAccountingPeriodService _accountingPeriodService;

	private readonly ICashFundService _cashFundService;

	private readonly IBankingService _bankingService;

	private readonly IBreakfastCafeCostingService _costingService;

	public BreakfastCafeSalesService(AppDbContext dbContext, IMenuCatalogService menuCatalogService, IJournalService journalService, IPostingRuleService postingRuleService, IAccountingPeriodService accountingPeriodService, ICashFundService cashFundService, IBankingService bankingService, IBreakfastCafeCostingService costingService)
	{
		_dbContext = dbContext;
		_menuCatalogService = menuCatalogService;
		_journalService = journalService;
		_postingRuleService = postingRuleService;
		_accountingPeriodService = accountingPeriodService;
		_cashFundService = cashFundService;
		_bankingService = bankingService;
		_costingService = costingService;
	}

	public Task<IReadOnlyList<MenuItemDto>> GetMenuAsync(CancellationToken cancellationToken = default(CancellationToken))
	{
		return GetMenuWithDefaultsAsync(cancellationToken);
	}

	private async Task<IReadOnlyList<MenuItemDto>> GetMenuWithDefaultsAsync(CancellationToken cancellationToken)
	{
		await _menuCatalogService.EnsureDefaultsAsync(cancellationToken);
		await EnsureMenuPreparationAreasAsync(cancellationToken);
		List<MenuItemDto> menu = (await _menuCatalogService.GetMenuItemsAsync(null, null, includeInactive: false, cancellationToken)).ToList();
		await ApplyMenuAvailabilityWarningsAsync(menu, cancellationToken);
		return menu;
	}

	public async Task<IReadOnlyList<PosTableDto>> GetTablesAsync(CancellationToken cancellationToken = default(CancellationToken))
	{
		await EnsurePosDefaultsAsync(cancellationToken);
		List<PosTable> tables = await (from x in _dbContext.PosTables.AsNoTracking()
			where x.IsActive
			orderby x.SortOrder, x.TableCode
			select x).ToListAsync(cancellationToken);
		Guid[] tableIds = tables.Select((PosTable x) => x.Id).ToArray();
		Dictionary<Guid, BreakfastCafeOrder> openMap = (from x in await (from x in _dbContext.BreakfastCafeOrders.AsNoTracking()
				where x.PosTableId.HasValue && Enumerable.Contains(tableIds, x.PosTableId.Value) && x.Status != "Paid" && x.Status != "Cancelled"
				orderby x.CreatedAt descending
				select x).ToListAsync(cancellationToken)
			group x by x.PosTableId!.Value).ToDictionary((IGrouping<Guid, BreakfastCafeOrder> x) => x.Key, (IGrouping<Guid, BreakfastCafeOrder> x) => x.First());
		return tables.Select(delegate(PosTable table)
		{
			openMap.TryGetValue(table.Id, out var value);
			return ToTableDto(table, value);
		}).ToList();
	}

	public async Task<BreakfastCafeOrderDto> OpenOrderForTableAsync(Guid posTableId, string? createdBy = null, CancellationToken cancellationToken = default(CancellationToken))
	{
		await EnsurePosDefaultsAsync(cancellationToken);
		PosTable table = (await _dbContext.PosTables.FirstOrDefaultAsync((PosTable x) => x.Id == posTableId && x.IsActive, cancellationToken)) ?? throw new InvalidOperationException("Không tìm thấy bàn/phòng.");
		BreakfastCafeOrder? openOrder = await (from x in _dbContext.BreakfastCafeOrders.Include((BreakfastCafeOrder x) => x.Lines).Include((BreakfastCafeOrder x) => x.Payments)
			where x.PosTableId == posTableId && x.Status != "Paid" && x.Status != "Cancelled"
			orderby x.CreatedAt descending
			select x).FirstOrDefaultAsync(cancellationToken);
		if (openOrder != null)
		{
			return ToDto(openOrder);
		}
		return await CreateOrderAsync(new CreateBreakfastCafeOrderRequest
		{
			PosTableId = table.Id,
			OrderType = table.OrderType,
			AreaName = table.AreaName,
			TableCode = table.TableCode,
			TableName = table.TableName,
			RoomReference = ((table.OrderType == "RoomService") ? table.TableName : null),
			CreatedBy = createdBy
		}, cancellationToken);
	}

	public async Task<BreakfastCafeOrderDto> CreateOrderAsync(CreateBreakfastCafeOrderRequest request, CancellationToken cancellationToken = default(CancellationToken))
	{
		await _menuCatalogService.EnsureDefaultsAsync(cancellationToken);
		await EnsurePosDefaultsAsync(cancellationToken);
		DateTime now = DateTime.UtcNow;
		DateTime orderDate = (request.OrderDate ?? DateTime.Now);
		PosTable? table = null;
		if (request.PosTableId.HasValue)
		{
			table = await _dbContext.PosTables.FirstOrDefaultAsync((PosTable x) => x.Id == request.PosTableId.Value, cancellationToken);
		}
		BreakfastCafeOrder breakfastCafeOrder = new BreakfastCafeOrder
		{
			Id = Guid.NewGuid()
		};
		PosTable? posTable = table;
		breakfastCafeOrder.PosTableId = ((posTable != null) ? new Guid?(posTable.Id) : request.PosTableId);
		BreakfastCafeOrder breakfastCafeOrder2 = breakfastCafeOrder;
		breakfastCafeOrder2.OrderNumber = await GenerateOrderNumberAsync(orderDate, cancellationToken);
		breakfastCafeOrder.OrderDate = orderDate;
		breakfastCafeOrder.Status = "Draft";
		breakfastCafeOrder.PaymentStatus = "Unpaid";
		breakfastCafeOrder.OrderType = table?.OrderType ?? NormalizeOrderType(request.OrderType);
		breakfastCafeOrder.ServiceStatus = "New";
		breakfastCafeOrder.AreaName = table?.AreaName ?? request.AreaName?.Trim();
		breakfastCafeOrder.TableCode = table?.TableCode ?? request.TableCode?.Trim();
		breakfastCafeOrder.TableName = table?.TableName ?? request.TableName?.Trim();
		breakfastCafeOrder.CustomerId = request.CustomerId;
		breakfastCafeOrder.CustomerReference = request.CustomerReference?.Trim();
		breakfastCafeOrder.RoomReference = request.RoomReference?.Trim();
		breakfastCafeOrder.Note = request.Note?.Trim();
		breakfastCafeOrder.SubtotalAmount = 0m;
		breakfastCafeOrder.DiscountAmount = 0m;
		breakfastCafeOrder.VatAmount = 0m;
		breakfastCafeOrder.TotalAmount = 0m;
		breakfastCafeOrder.PaidAmount = 0m;
		breakfastCafeOrder.RemainingAmount = 0m;
		breakfastCafeOrder.CreatedBy = request.CreatedBy?.Trim() ?? "system";
		breakfastCafeOrder.CreatedAt = now;
		breakfastCafeOrder.UpdatedAt = now;
		BreakfastCafeOrder order = breakfastCafeOrder;
		_dbContext.BreakfastCafeOrders.Add(order);
		if (table != null)
		{
			table.Status = "Serving";
			table.UpdatedAt = now;
		}
		await _dbContext.SaveChangesAsync(cancellationToken);
		return (await GetOrderDetailAsync(order.Id, cancellationToken)) ?? throw new InvalidOperationException("Cannot load order.");
	}

	public async Task<BreakfastCafeOrderDto> AddLineAsync(UpdateBreakfastCafeOrderLineRequest request, CancellationToken cancellationToken = default(CancellationToken))
	{
		BreakfastCafeOrder order = await GetOrderEntityAsync(request.OrderId, cancellationToken);
		EnsureEditable(order);
		EnsureNotCancelled(order);
		if (request.Quantity <= 0m)
		{
			throw new InvalidOperationException("Số lượng phải lớn hơn 0.");
		}
		bool hadKitchenTicket = order.KitchenTickets.Any((BreakfastCafeKitchenTicket x) => string.Equals(x.Status, "Sent", StringComparison.OrdinalIgnoreCase));
		MenuItem menuItem = (await _dbContext.MenuItems.AsNoTracking().FirstOrDefaultAsync((MenuItem x) => x.Id == request.MenuItemId && x.IsActive, cancellationToken)) ?? throw new InvalidOperationException("Không tìm thấy món.");
		decimal requestedTotalQuantity = order.Lines.Where(x => x.MenuItemId == menuItem.Id).Sum(x => x.Quantity) + request.Quantity;
		string? availabilityWarning = await GetMenuItemAvailabilityWarningAsync(menuItem.Id, requestedTotalQuantity, menuItem.HasRecipeManagement, cancellationToken);
		if (!string.IsNullOrWhiteSpace(availabilityWarning))
		{
			throw new InvalidOperationException("Không thể thêm món: " + availabilityWarning + ".");
		}
		BreakfastCafeOrderLine? existing = order.Lines.FirstOrDefault((BreakfastCafeOrderLine x) => x.MenuItemId == menuItem.Id && string.IsNullOrWhiteSpace(x.Note));
		if (existing != null && !string.Equals(existing.KitchenStatus, "Sent", StringComparison.OrdinalIgnoreCase))
		{
			existing.Quantity += request.Quantity;
			existing.UpdatedAt = DateTime.UtcNow;
			Recalculate(order);
			await _dbContext.SaveChangesAsync(cancellationToken);
			return (await GetOrderDetailAsync(order.Id, cancellationToken)) ?? throw new InvalidOperationException("Cannot load order.");
		}
		DateTime now = DateTime.UtcNow;
		int lineNo = ((order.Lines.Count == 0) ? 1 : (order.Lines.Max((BreakfastCafeOrderLine x) => x.LineNo) + 1));
		BreakfastCafeOrderLine line = new BreakfastCafeOrderLine
		{
			Id = Guid.NewGuid(),
			BreakfastCafeOrderId = order.Id,
			CompanyId = order.CompanyId,
			BranchId = order.BranchId,
			MenuItemId = menuItem.Id,
			LineNo = lineNo,
			MenuItemCode = menuItem.Code,
			MenuItemName = menuItem.Name,
			UnitName = menuItem.UnitName,
			Quantity = request.Quantity,
			UnitPrice = menuItem.UnitPrice,
			LineDiscountAmount = 0m,
			VatRate = menuItem.VatRate,
			KitchenStatus = "New",
			Note = hadKitchenTicket
				? string.IsNullOrWhiteSpace(request.Note) ? "Món thêm" : $"Món thêm - {request.Note.Trim()}"
				: string.Equals(existing?.KitchenStatus, "Sent", StringComparison.OrdinalIgnoreCase)
				? string.IsNullOrWhiteSpace(request.Note) ? "Món thêm" : $"Món thêm - {request.Note.Trim()}"
				: request.Note?.Trim(),
			CreatedAt = now,
			UpdatedAt = now
		};
		_dbContext.BreakfastCafeOrderLines.Add(line);
		if (!order.Lines.Any((BreakfastCafeOrderLine x) => x.Id == line.Id))
		{
			order.Lines.Add(line);
		}
		Recalculate(order);
		await _dbContext.SaveChangesAsync(cancellationToken);
		return (await GetOrderDetailAsync(order.Id, cancellationToken)) ?? throw new InvalidOperationException("Cannot load order.");
	}

	public async Task<BreakfastCafeOrderDto> UpdateLineQuantityAsync(UpdateBreakfastCafeOrderLineRequest request, CancellationToken cancellationToken = default(CancellationToken))
	{
		if (!request.OrderLineId.HasValue)
		{
			throw new InvalidOperationException("Thiếu order line.");
		}
		if (request.Quantity <= 0m)
		{
			throw new InvalidOperationException("Số lượng phải lớn hơn 0.");
		}
		BreakfastCafeOrder order = await GetOrderEntityAsync(request.OrderId, cancellationToken);
		EnsureEditable(order);
		EnsureNotCancelled(order);
		BreakfastCafeOrderLine line = order.Lines.FirstOrDefault((BreakfastCafeOrderLine x) => x.Id == request.OrderLineId.Value) ?? throw new InvalidOperationException("Không tìm thấy dòng món.");
		MenuItem menuItem = await _dbContext.MenuItems.AsNoTracking()
			.FirstOrDefaultAsync(x => x.Id == line.MenuItemId && x.IsActive, cancellationToken)
			?? throw new InvalidOperationException("Không tìm thấy món.");
		if (string.Equals(line.KitchenStatus, "Sent", StringComparison.OrdinalIgnoreCase) && request.Quantity > line.Quantity)
		{
			return await AddLineAsync(new UpdateBreakfastCafeOrderLineRequest
			{
				OrderId = request.OrderId,
				MenuItemId = line.MenuItemId,
				Quantity = request.Quantity - line.Quantity,
				Note = string.IsNullOrWhiteSpace(request.Note) ? "Món thêm" : request.Note
			}, cancellationToken);
		}
		decimal requestedTotalQuantity = order.Lines
			.Where(x => x.MenuItemId == line.MenuItemId && x.Id != line.Id)
			.Sum(x => x.Quantity) + request.Quantity;
		string? availabilityWarning = await GetMenuItemAvailabilityWarningAsync(menuItem.Id, requestedTotalQuantity, menuItem.HasRecipeManagement, cancellationToken);
		if (!string.IsNullOrWhiteSpace(availabilityWarning))
		{
			throw new InvalidOperationException("Không thể cập nhật số lượng: " + availabilityWarning + ".");
		}
		line.Quantity = request.Quantity;
		if (request.Note != null)
		{
			line.Note = (string.IsNullOrWhiteSpace(request.Note) ? null : request.Note.Trim());
		}
		line.UpdatedAt = DateTime.UtcNow;
		Recalculate(order);
		await _dbContext.SaveChangesAsync(cancellationToken);
		return (await GetOrderDetailAsync(order.Id, cancellationToken)) ?? throw new InvalidOperationException("Cannot load order.");
	}

	public async Task<BreakfastCafeOrderDto> RemoveLineAsync(Guid orderId, Guid orderLineId, CancellationToken cancellationToken = default(CancellationToken))
	{
		BreakfastCafeOrder order = await GetOrderEntityAsync(orderId, cancellationToken);
		EnsureEditable(order);
		EnsureNotCancelled(order);
		BreakfastCafeOrderLine? line = order.Lines.FirstOrDefault((BreakfastCafeOrderLine x) => x.Id == orderLineId);
		if (line == null)
		{
			return (await GetOrderDetailAsync(order.Id, cancellationToken)) ?? throw new InvalidOperationException("Cannot load order.");
		}
		DateTime now = DateTime.UtcNow;
		bool hadKitchenTicket = order.KitchenTickets.Any((BreakfastCafeKitchenTicket x) => string.Equals(x.Status, "Sent", StringComparison.OrdinalIgnoreCase));
		if (hadKitchenTicket || string.Equals(line.KitchenStatus, "Sent", StringComparison.OrdinalIgnoreCase))
		{
			string station = ResolvePreparationStation(line.MenuItem);
			order.KitchenTickets.Add(new BreakfastCafeKitchenTicket
			{
				Id = Guid.NewGuid(),
				BreakfastCafeOrderId = order.Id,
				TicketNumber = await GenerateCancelTicketNumberAsync(now.Date, cancellationToken),
				Station = station,
				Status = "Cancelled",
				SentAt = now,
				SentBy = "pos-ui",
				Note = $"Hủy món: {line.MenuItemName} x {line.Quantity:N1}"
			});
		}
		order.Lines.Remove(line);
		_dbContext.BreakfastCafeOrderLines.Remove(line);
		Recalculate(order);
		await _dbContext.SaveChangesAsync(cancellationToken);
		return (await GetOrderDetailAsync(order.Id, cancellationToken)) ?? throw new InvalidOperationException("Cannot load order.");
	}

	public async Task<BreakfastCafeOrderDto> UpdateLineNoteAsync(Guid orderId, Guid orderLineId, string? note, CancellationToken cancellationToken = default(CancellationToken))
	{
		BreakfastCafeOrder order = await GetOrderEntityAsync(orderId, cancellationToken);
		EnsureEditable(order);
		EnsureNotCancelled(order);
		EnsureNotCancelled(order);
		BreakfastCafeOrderLine line = order.Lines.FirstOrDefault((BreakfastCafeOrderLine x) => x.Id == orderLineId) ?? throw new InvalidOperationException("Không tìm thấy dòng món.");
		line.Note = (string.IsNullOrWhiteSpace(note) ? null : note.Trim());
		line.UpdatedAt = DateTime.UtcNow;
		await _dbContext.SaveChangesAsync(cancellationToken);
		return (await GetOrderDetailAsync(order.Id, cancellationToken)) ?? throw new InvalidOperationException("Cannot load order.");
	}

	public async Task<BreakfastCafeOrderDto> ApplyDiscountAsync(Guid orderId, decimal discountAmount, CancellationToken cancellationToken = default(CancellationToken))
	{
		BreakfastCafeOrder order = await GetOrderEntityAsync(orderId, cancellationToken);
		EnsureEditable(order);
		EnsureNotCancelled(order);
		EnsureNotCancelled(order);
		if (discountAmount < 0m)
		{
			throw new InvalidOperationException("Giảm giá không được âm.");
		}
		if (order.PaidAmount > 0m)
		{
			throw new InvalidOperationException("Không thể áp dụng giảm giá sau khi đã thanh toán một phần.");
		}
		decimal subtotal = order.Lines.Sum((BreakfastCafeOrderLine x) => x.Quantity * x.UnitPrice);
		if (discountAmount > subtotal)
		{
			throw new InvalidOperationException("Giảm giá vượt quá tạm tính.");
		}
		order.DiscountAmount = discountAmount;
		Recalculate(order);
		await _dbContext.SaveChangesAsync(cancellationToken);
		return (await GetOrderDetailAsync(order.Id, cancellationToken)) ?? throw new InvalidOperationException("Cannot load order.");
	}

	public async Task<List<PosKitchenTicketDto>> SendToKitchenAsync(SendBreakfastCafeKitchenRequest request, CancellationToken cancellationToken = default(CancellationToken))
	{
		BreakfastCafeOrder order = await GetOrderEntityAsync(request.OrderId, cancellationToken);
		EnsureEditable(order);
		EnsureNotCancelled(order);
		if (order.Lines.Count == 0)
		{
			throw new InvalidOperationException("Đơn chưa có món để gửi bếp/bar.");
		}
		DateTime now = DateTime.UtcNow;
		bool hadKitchenTicket = order.KitchenTickets.Any((BreakfastCafeKitchenTicket x) => string.Equals(x.Status, "Sent", StringComparison.OrdinalIgnoreCase));
		List<BreakfastCafeOrderLine> unsentLines = order.Lines
			.Where(x => IsPendingKitchenLine(x, order.KitchenSentAt, hadKitchenTicket))
			.ToList();
		if (unsentLines.Count == 0)
		{
			throw new InvalidOperationException("Tất cả món đã được gửi bếp/bar.");
		}
		if (hadKitchenTicket && unsentLines.Count == order.Lines.Count)
		{
			throw new InvalidOperationException("Phiếu này đã được gửi rồi. Chỉ khi thêm món mới thì mới gửi tiếp.");
		}
		await EnsureRecipeStockAvailableAsync(order, cancellationToken);
		List<BreakfastCafeOrderLine> kitchenLines = unsentLines.Where((BreakfastCafeOrderLine x) => IsKitchenPreparationArea(x.MenuItem?.PreparationArea)).ToList();
		List<BreakfastCafeOrderLine> barLines = unsentLines.Where((BreakfastCafeOrderLine x) => IsBarPreparationArea(x.MenuItem?.PreparationArea)).ToList();
		List<BreakfastCafeOrderLine> commonLines = unsentLines.Except(kitchenLines).Except(barLines).ToList();
		List<PosKitchenTicketDto> tickets = new List<PosKitchenTicketDto>();
		List<BreakfastCafeOrderLine> allSentLines = new List<BreakfastCafeOrderLine>();
		int ticketSequence = await GetNextKitchenTicketSequenceAsync(now.Date, cancellationToken);
		string NextTicketNumber()
		{
			return $"KOT-{now:yyyyMMdd}-{ticketSequence++:0000}";
		}
		if (kitchenLines.Count > 0 || commonLines.Count > 0)
		{
			PosKitchenTicketDto kitchenTicket = CreateTicket(order, "Kitchen", NextTicketNumber(), now, request);
			MarkLinesSent(kitchenLines.Concat(commonLines), now);
			tickets.Add(kitchenTicket);
			allSentLines.AddRange(kitchenLines);
			allSentLines.AddRange(commonLines);
		}
		if (barLines.Count > 0)
		{
			PosKitchenTicketDto barTicket = CreateTicket(order, "Bar", NextTicketNumber(), now, request);
			MarkLinesSent(barLines, now);
			tickets.Add(barTicket);
			allSentLines.AddRange(barLines);
		}
		if (tickets.Count == 0)
		{
			PosKitchenTicketDto ticket = CreateTicket(order, "KitchenBar", NextTicketNumber(), now, request);
			MarkLinesSent(unsentLines, now);
			tickets.Add(ticket);
			allSentLines.AddRange(unsentLines);
		}
		order.Status = "SentKitchen";
		order.ServiceStatus = "SentKitchen";
		order.KitchenSentAt = now;
		order.UpdatedAt = now;
		if (order.PosTableId.HasValue)
		{
			PosTable? table = await _dbContext.PosTables.FirstOrDefaultAsync((PosTable x) => x.Id == order.PosTableId.Value, cancellationToken);
			if (table != null)
			{
				table.Status = "Serving";
				table.UpdatedAt = now;
			}
		}
		await _dbContext.SaveChangesAsync(cancellationToken);
		return tickets;
	}

	private PosKitchenTicketDto CreateTicket(BreakfastCafeOrder order, string station, string ticketNumber, DateTime now, SendBreakfastCafeKitchenRequest request)
	{
		BreakfastCafeKitchenTicket breakfastCafeKitchenTicket = new BreakfastCafeKitchenTicket
		{
			Id = Guid.NewGuid(),
			BreakfastCafeOrderId = order.Id,
			TicketNumber = ticketNumber,
			Station = station,
			Status = "Sent",
			SentAt = now,
			SentBy = request.SentBy?.Trim(),
			Note = request.Note?.Trim()
		};
		order.KitchenTickets.Add(breakfastCafeKitchenTicket);
		return new PosKitchenTicketDto
		{
			Id = breakfastCafeKitchenTicket.Id,
			BreakfastCafeOrderId = order.Id,
			TicketNumber = breakfastCafeKitchenTicket.TicketNumber,
			Station = breakfastCafeKitchenTicket.Station,
			Status = breakfastCafeKitchenTicket.Status,
			SentAt = breakfastCafeKitchenTicket.SentAt
		};
	}

	private static string ResolvePreparationStation(MenuItem? menuItem)
	{
		if (IsBarPreparationArea(menuItem?.PreparationArea))
		{
			return "Bar";
		}

		return "Kitchen";
	}

	private static bool IsBarPreparationArea(string? preparationArea)
	{
		var value = (preparationArea ?? string.Empty).Trim();
		return value.Equals("Bar", StringComparison.OrdinalIgnoreCase)
			|| value.StartsWith("BAR", StringComparison.OrdinalIgnoreCase)
			|| value.Contains("bar", StringComparison.OrdinalIgnoreCase);
	}

	private static bool IsKitchenPreparationArea(string? preparationArea)
	{
		var value = (preparationArea ?? string.Empty).Trim();
		return string.IsNullOrWhiteSpace(value)
			|| value.Equals("Kitchen", StringComparison.OrdinalIgnoreCase)
			|| value.StartsWith("KITCHEN", StringComparison.OrdinalIgnoreCase)
			|| value.Contains("bếp", StringComparison.OrdinalIgnoreCase);
	}

	private async Task<string> GenerateCancelTicketNumberAsync(DateTime date, CancellationToken cancellationToken)
	{
		int sequence = await GetNextKitchenTicketSequenceAsync(date, cancellationToken);
		return $"KCN-{date:yyyyMMdd}-{sequence:0000}";
	}

	private static void MarkLinesSent(IEnumerable<BreakfastCafeOrderLine> lines, DateTime now)
	{
		foreach (BreakfastCafeOrderLine line in lines)
		{
			line.KitchenStatus = "Sent";
			line.UpdatedAt = now;
		}
	}

	private static bool IsPendingKitchenLine(BreakfastCafeOrderLine line, DateTime? kitchenSentAt, bool hadKitchenTicket)
	{
		if (string.Equals(line.KitchenStatus, "Sent", StringComparison.OrdinalIgnoreCase))
		{
			return false;
		}

		if (!hadKitchenTicket || !kitchenSentAt.HasValue)
		{
			return true;
		}

		if (line.CreatedAt > kitchenSentAt.Value || line.UpdatedAt > kitchenSentAt.Value)
		{
			return true;
		}

		return !string.IsNullOrWhiteSpace(line.Note) && line.Note.Contains("Món thêm", StringComparison.OrdinalIgnoreCase);
	}

	public async Task<BreakfastCafeOrderDto> PayOrderAsync(PayBreakfastCafeOrderRequest request, CancellationToken cancellationToken = default(CancellationToken))
	{
		BreakfastCafeOrder order = await GetOrderEntityAsync(request.OrderId, cancellationToken);
		EnsureEditable(order);
		EnsureNotCancelled(order);
		if (order.Lines.Count == 0)
		{
			throw new InvalidOperationException("Không thể thanh toán order rỗng.");
		}
		DateTime paymentDate = (request.PaymentDate ?? DateTime.Today).Date;
		await _accountingPeriodService.ValidatePostingDateAsync(paymentDate, cancellationToken);
		if (request.Amount <= 0m)
		{
			throw new InvalidOperationException("Số tiền thanh toán phải lớn hơn 0.");
		}
		if (request.Amount > order.RemainingAmount)
		{
			throw new InvalidOperationException("Số tiền thanh toán vượt số còn lại.");
		}
		string method = request.PaymentMethod.Trim();
		string text = method;
		bool flag = ((text == "RoomCharge" || text == "CustomerDebt") ? true : false);
		if (flag && !request.CustomerId.HasValue && string.IsNullOrWhiteSpace(request.RoomReference))
		{
			throw new InvalidOperationException("RoomCharge/CustomerDebt bắt buộc có tham chiếu khách hoặc phòng.");
		}
		var newPaidAmount = order.PaidAmount + request.Amount;
		var newRemaining = Math.Max(0m, order.TotalAmount - newPaidAmount);
		var newPaymentStatus = (newRemaining == 0m) ? "Paid" : "PartiallyPaid";
		if (newPaymentStatus == "Paid")
		{
			await EnsureRecipeStockAvailableAsync(order, cancellationToken);
		}
		await CreatePaymentFlowAsync(order, request, method, cancellationToken);

		var newOrderStatus = (newPaymentStatus == "Paid") ? "Paid" : "Serving";
		var newServiceStatus = (newPaymentStatus == "Paid") ? "Paid" : "AwaitingPayment";
		var now = DateTime.UtcNow;

		// Use raw SQL to avoid EF ChangeTracker concurrency issues
		if (newPaymentStatus == "Paid" && order.PosTableId.HasValue)
			await _dbContext.Database.ExecuteSqlRawAsync("UPDATE PosTables SET Status = 'Empty', UpdatedAt = {0} WHERE Id = {1}", now, order.PosTableId.Value);
		await _dbContext.Database.ExecuteSqlRawAsync("UPDATE BreakfastCafeOrders SET PaidAmount = {0}, RemainingAmount = {1}, PaymentStatus = {2}, Status = {3}, ServiceStatus = {4}, UpdatedAt = {5} WHERE Id = {6}", newPaidAmount, newRemaining, newPaymentStatus, newOrderStatus, newServiceStatus, now, order.Id);
		string paymentStatus = method is "Cash" or "BankTransfer" or "CardPOS" ? "Draft" : "Posted";
		await _dbContext.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO BreakfastCafePayments (Id, BreakfastCafeOrderId, PaymentDate, PaymentMethod, Amount, ReferenceNo, CashFundId, BankAccountId, CustomerId, RoomReference, Status, CreatedBy, CreatedAt) VALUES ({Guid.NewGuid()}, {order.Id}, {paymentDate}, {method}, {request.Amount}, {request.ReferenceNo?.Trim()}, {request.CashFundId}, {request.BankAccountId}, {request.CustomerId}, {request.RoomReference?.Trim()}, {paymentStatus}, {request.CreatedBy?.Trim() ?? "system"}, {now})");

		if (newPaymentStatus == "Paid")
		{
			await _costingService.ConsumeRecipeAsync(order.Id, request.CreatedBy, cancellationToken);
		}
		return (await GetOrderDetailAsync(order.Id, cancellationToken)) ?? throw new InvalidOperationException("Cannot load order.");
	}


	public async Task<BreakfastCafeOrderDto> CancelOrderAsync(Guid orderId, string? reason, CancellationToken cancellationToken = default(CancellationToken))
	{
		BreakfastCafeOrder order = await GetOrderEntityAsync(orderId, cancellationToken);
		if (order.Status == "Paid")
		{
			throw new InvalidOperationException("Order đã thanh toán, không thể hủy trực tiếp.");
		}
		if (order.PaidAmount > 0m)
		{
			throw new InvalidOperationException("Order đã thanh toán một phần, không thể hủy. Vui lòng hoàn tiền trước.");
		}
		order.Status = "Cancelled";
		order.ServiceStatus = "Cancelled";
		order.Note = (string.IsNullOrWhiteSpace(reason) ? order.Note : reason.Trim());
		order.UpdatedAt = DateTime.UtcNow;
		foreach (BreakfastCafeKitchenTicket ticket in order.KitchenTickets.Where((BreakfastCafeKitchenTicket t) => t.Status == "Sent"))
		{
			ticket.Status = "Cancelled";
			ticket.Note = (ticket.Note + " | Hủy: " + reason).Trim();
		}
		if (order.PosTableId.HasValue)
		{
			PosTable? table = await _dbContext.PosTables.FirstOrDefaultAsync((PosTable x) => x.Id == order.PosTableId.Value, cancellationToken);
			if (table != null)
			{
				table.Status = "Empty";
				table.UpdatedAt = DateTime.UtcNow;
			}
		}
		await _dbContext.SaveChangesAsync(cancellationToken);
		return (await GetOrderDetailAsync(order.Id, cancellationToken)) ?? throw new InvalidOperationException("Cannot load order.");
	}

	public async Task<BreakfastCafeOrderDto> UpdateOrderNoteAsync(Guid orderId, string? note, CancellationToken cancellationToken = default(CancellationToken))
	{
		BreakfastCafeOrder order = await GetOrderEntityAsync(orderId, cancellationToken);
		EnsureEditable(order);
		EnsureNotCancelled(order);
		order.Note = note?.Trim();
		order.UpdatedAt = DateTime.UtcNow;
		await _dbContext.SaveChangesAsync(cancellationToken);
		return ToDto(order);
	}

	public async Task<BreakfastCafeOrderDto> ChangeTableAsync(Guid orderId, Guid newPosTableId, string? createdBy = null, CancellationToken cancellationToken = default(CancellationToken))
	{
		BreakfastCafeOrder order = await GetOrderEntityAsync(orderId, cancellationToken);
		EnsureEditable(order);
		EnsureNotCancelled(order);
		if (order.PosTableId.HasValue)
		{
			PosTable? oldTable = await _dbContext.PosTables.FirstOrDefaultAsync((PosTable x) => x.Id == order.PosTableId.Value, cancellationToken);
			if (oldTable != null)
			{
				oldTable.Status = "Empty";
				oldTable.UpdatedAt = DateTime.UtcNow;
			}
		}
		PosTable newTable = (await _dbContext.PosTables.FirstOrDefaultAsync((PosTable x) => x.Id == newPosTableId, cancellationToken)) ?? throw new InvalidOperationException("Không tìm thấy bàn.");
		if (await _dbContext.BreakfastCafeOrders.AnyAsync((BreakfastCafeOrder x) => x.PosTableId == newPosTableId && x.Status != "Paid" && x.Status != "Cancelled", cancellationToken))
		{
			throw new InvalidOperationException("Bàn đích đã có đơn đang mở, không thể chuyển.");
		}
		order.PosTableId = newTable.Id;
		order.TableCode = newTable.TableCode;
		order.TableName = newTable.TableName;
		order.AreaName = newTable.AreaName;
		order.OrderType = newTable.OrderType;
		order.RoomReference = ((newTable.OrderType == "RoomService") ? newTable.TableName : null);
		order.UpdatedAt = DateTime.UtcNow;
		newTable.Status = "Serving";
		newTable.UpdatedAt = DateTime.UtcNow;
		await _dbContext.SaveChangesAsync(cancellationToken);
		return ToDto(order);
	}

	public async Task<BreakfastCafeOrderDto> MergeOrdersAsync(Guid targetOrderId, Guid sourceOrderId, string? createdBy = null, CancellationToken cancellationToken = default(CancellationToken))
	{
		if (targetOrderId == sourceOrderId)
		{
			throw new InvalidOperationException("Không thể gộp cùng một đơn.");
		}

		BreakfastCafeOrder targetOrder = await GetOrderEntityAsync(targetOrderId, cancellationToken);
		BreakfastCafeOrder sourceOrder = await GetOrderEntityAsync(sourceOrderId, cancellationToken);
		EnsureEditable(targetOrder);
		EnsureEditable(sourceOrder);
		EnsureNotCancelled(targetOrder);
		EnsureNotCancelled(sourceOrder);

		int nextLineNo = (targetOrder.Lines.Count == 0) ? 1 : (targetOrder.Lines.Max((BreakfastCafeOrderLine x) => x.LineNo) + 1);
		foreach (BreakfastCafeOrderLine line in sourceOrder.Lines.OrderBy((BreakfastCafeOrderLine x) => x.LineNo).ToList())
		{
			sourceOrder.Lines.Remove(line);
			line.BreakfastCafeOrderId = targetOrder.Id;
			line.Order = targetOrder;
			line.LineNo = nextLineNo++;
			line.UpdatedAt = DateTime.UtcNow;
			targetOrder.Lines.Add(line);
		}

		foreach (BreakfastCafePayment payment in sourceOrder.Payments.OrderBy((BreakfastCafePayment x) => x.PaymentDate).ToList())
		{
			sourceOrder.Payments.Remove(payment);
			payment.BreakfastCafeOrderId = targetOrder.Id;
			payment.Order = targetOrder;
			targetOrder.Payments.Add(payment);
		}

		foreach (BreakfastCafeKitchenTicket ticket in sourceOrder.KitchenTickets.OrderBy((BreakfastCafeKitchenTicket x) => x.SentAt).ToList())
		{
			sourceOrder.KitchenTickets.Remove(ticket);
			ticket.BreakfastCafeOrderId = targetOrder.Id;
			ticket.Order = targetOrder;
			ticket.Note = AppendMergeNote(ticket.Note, sourceOrder);
			targetOrder.KitchenTickets.Add(ticket);
		}

		targetOrder.PaidAmount = targetOrder.Payments.Sum((BreakfastCafePayment x) => x.Amount);
		if (sourceOrder.KitchenSentAt.HasValue && (!targetOrder.KitchenSentAt.HasValue || sourceOrder.KitchenSentAt.Value < targetOrder.KitchenSentAt.Value))
		{
			targetOrder.KitchenSentAt = sourceOrder.KitchenSentAt;
		}

		targetOrder.Note = MergeOrderNotes(targetOrder.Note, sourceOrder);
		Recalculate(targetOrder);
		if (targetOrder.PaidAmount >= targetOrder.TotalAmount && targetOrder.TotalAmount > 0m)
		{
			targetOrder.PaymentStatus = "Paid";
			targetOrder.Status = "Paid";
			targetOrder.ServiceStatus = "Paid";
		}
		else if (targetOrder.PaidAmount > 0m)
		{
			targetOrder.PaymentStatus = "Partial";
			targetOrder.Status = "Open";
		}

		sourceOrder.SubtotalAmount = 0m;
		sourceOrder.DiscountAmount = 0m;
		sourceOrder.VatAmount = 0m;
		sourceOrder.ServiceChargeAmount = 0m;
		sourceOrder.TotalAmount = 0m;
		sourceOrder.PaidAmount = 0m;
		sourceOrder.RemainingAmount = 0m;
		sourceOrder.KitchenSentAt = null;
		sourceOrder.Status = "Cancelled";
		sourceOrder.PaymentStatus = "Cancelled";
		sourceOrder.ServiceStatus = "Cancelled";
		sourceOrder.Note = $"Gộp vào {targetOrder.OrderNumber}" + (string.IsNullOrWhiteSpace(sourceOrder.Note) ? string.Empty : $" | {sourceOrder.Note}");
		sourceOrder.UpdatedAt = DateTime.UtcNow;

		if (sourceOrder.PosTableId.HasValue)
		{
			PosTable? sourceTable = await _dbContext.PosTables.FirstOrDefaultAsync((PosTable x) => x.Id == sourceOrder.PosTableId.Value, cancellationToken);
			if (sourceTable != null)
			{
				sourceTable.Status = "Empty";
				sourceTable.UpdatedAt = DateTime.UtcNow;
			}
		}

		await _dbContext.SaveChangesAsync(cancellationToken);
		return (await GetOrderDetailAsync(targetOrder.Id, cancellationToken)) ?? throw new InvalidOperationException("Cannot load merged order.");
	}

	public async Task<IReadOnlyList<BreakfastCafeOrderDto>> GetOpenOrdersAsync(CancellationToken cancellationToken = default(CancellationToken))
	{
		return (await (from x in _dbContext.BreakfastCafeOrders.AsNoTracking().Include((BreakfastCafeOrder x) => x.Lines)
			where x.Status != "Paid" && x.Status != "Cancelled"
			orderby x.CreatedAt
			select x).ToListAsync(cancellationToken)).Select(ToDtoHeader).ToList();
	}

	public async Task<IReadOnlyList<BreakfastCafeOrderDto>> GetTodayOrdersAsync(DateTime? businessDate = null, CancellationToken cancellationToken = default(CancellationToken))
	{
		DateTime date = (businessDate ?? DateTime.Today).Date;
		DateTime next = date.AddDays(1.0);
		return (await (from x in _dbContext.BreakfastCafeOrders.AsNoTracking()
			where x.OrderDate >= date && x.OrderDate < next
			orderby x.CreatedAt descending
			select x).ToListAsync(cancellationToken)).Select(ToDtoHeader).ToList();
	}

		public async Task<IReadOnlyList<BreakfastCafeOrderDto>> GetOrdersByDateRangeAsync(DateTime fromDate, DateTime toDate, CancellationToken cancellationToken = default)
		{
			var from = fromDate.Date;
			var to = toDate.Date.AddDays(1);
			return (await _dbContext.BreakfastCafeOrders.AsNoTracking()
				.Where(x => x.OrderDate >= from && x.OrderDate < to)
				.OrderByDescending(x => x.CreatedAt)
				.ToListAsync(cancellationToken))
				.Select(ToDtoHeader).ToList();
		}

	public async Task<BreakfastCafeSalesSummaryDto> GetSalesSummaryAsync(DateTime? businessDate = null, CancellationToken cancellationToken = default(CancellationToken))
	{
		DateTime date = (businessDate ?? DateTime.Today).Date;
		DateTime next = date.AddDays(1.0);
		List<BreakfastCafeOrder> orders = await (from x in _dbContext.BreakfastCafeOrders.AsNoTracking()
			where x.OrderDate >= date && x.OrderDate < next && x.Status == "Paid"
			select x).ToListAsync(cancellationToken);
		List<BreakfastCafeOrderLine> lines = await (from x in _dbContext.BreakfastCafeOrderLines.AsNoTracking()
			where x.Order != null && x.Order.OrderDate >= date && x.Order.OrderDate < next && x.Order.Status != "Cancelled"
			select x).Include((BreakfastCafeOrderLine x) => x.Order).ToListAsync(cancellationToken);
		List<BreakfastCafePayment> payments = await (from x in _dbContext.BreakfastCafePayments.AsNoTracking()
			where x.PaymentDate >= date && x.PaymentDate < next
			select x).ToListAsync(cancellationToken);
		var postedCostIssues = await _dbContext.StockIssues.AsNoTracking()
			.Where(x => x.IssueDate >= date
				&& x.IssueDate < next
				&& x.IsApproved
				&& x.IssueNo.StartsWith("POS-"))
			.Select(x => new { x.IssueNo, x.TotalAmount })
			.ToListAsync(cancellationToken);
		decimal estimatedCost = orders.Sum(order => postedCostIssues
			.Where(issue => issue.IssueNo.StartsWith($"POS-{order.OrderNumber}-", StringComparison.Ordinal))
			.Sum(issue => issue.TotalAmount));
		return new BreakfastCafeSalesSummaryDto
		{
			SummaryDate = date,
			RevenueAmount = orders.Sum((BreakfastCafeOrder x) => x.TotalAmount),
			VatOutputAmount = orders.Sum((BreakfastCafeOrder x) => x.VatAmount),
			EstimatedCostAmount = estimatedCost,
			OrderCount = orders.Count,
			TopSellingItems = (from x in lines
				group x by new { x.MenuItemCode, x.MenuItemName } into g
				select new BreakfastCafeTopItemDto
				{
					MenuItemCode = g.Key.MenuItemCode,
					MenuItemName = g.Key.MenuItemName,
					Quantity = g.Sum((BreakfastCafeOrderLine x) => x.Quantity),
					RevenueAmount = g.Sum((BreakfastCafeOrderLine x) => x.LineTotalAmount)
				} into x
				orderby x.Quantity descending
				select x).Take(5).ToList(),
			PaymentSummaries = (from x in payments
				group x by x.PaymentMethod into g
				select new BreakfastCafePaymentSummaryDto
				{
					PaymentMethod = g.Key,
					Amount = g.Sum((BreakfastCafePayment x) => x.Amount)
				} into x
				orderby x.Amount descending
				select x).ToList()
		};
	}

	public async Task<BreakfastCafeOrderDto?> GetOrderDetailAsync(Guid orderId, CancellationToken cancellationToken = default(CancellationToken))
	{
		BreakfastCafeOrder? order = await _dbContext.BreakfastCafeOrders.AsNoTracking().Include((BreakfastCafeOrder x) => x.Lines.OrderBy((BreakfastCafeOrderLine l) => l.LineNo)).ThenInclude((BreakfastCafeOrderLine x) => x.MenuItem)
			.Include((BreakfastCafeOrder x) => x.Payments.OrderByDescending((BreakfastCafePayment p) => p.PaymentDate))
			.Include((BreakfastCafeOrder x) => x.KitchenTickets)
			.FirstOrDefaultAsync((BreakfastCafeOrder x) => x.Id == orderId, cancellationToken);
		return (order == null) ? null : ToDto(order);
	}

	private async Task<BreakfastCafeOrder> GetOrderEntityAsync(Guid orderId, CancellationToken cancellationToken)
	{
		return (await _dbContext.BreakfastCafeOrders.Include((BreakfastCafeOrder x) => x.Lines).ThenInclude((BreakfastCafeOrderLine x) => x.MenuItem).Include((BreakfastCafeOrder x) => x.Payments)
			.Include((BreakfastCafeOrder x) => x.KitchenTickets)
			.FirstOrDefaultAsync((BreakfastCafeOrder x) => x.Id == orderId, cancellationToken)) ?? throw new InvalidOperationException("Không tìm thấy order.");
	}

	private static void EnsureEditable(BreakfastCafeOrder order)
	{
		if (order.Status == "Paid")
		{
			throw new InvalidOperationException("Order đã thanh toán, không thể chỉnh sửa.");
		}
		if (order.PaidAmount > 0m)
		{
			throw new InvalidOperationException("Order đã thanh toán một phần, không thể chỉnh sửa. Vui lòng hoàn tất thanh toán hoặc hoàn tiền.");
		}
	}

	private static void EnsureNotCancelled(BreakfastCafeOrder order)
	{
		if (order.Status == "Cancelled")
		{
			throw new InvalidOperationException("Order đã hủy, không thể chỉnh sửa.");
		}
	}

	private static void Recalculate(BreakfastCafeOrder order)
	{
		foreach (BreakfastCafeOrderLine line in order.Lines)
		{
			decimal num = line.Quantity * line.UnitPrice;
			if (line.LineDiscountAmount < 0m)
			{
				line.LineDiscountAmount = 0m;
			}
			if (line.LineDiscountAmount > num)
			{
				line.LineDiscountAmount = num;
			}
			decimal num2 = num - line.LineDiscountAmount;
			line.VatAmount = Math.Round(num2 * line.VatRate, 2, MidpointRounding.AwayFromZero);
			line.LineTotalAmount = num2 + line.VatAmount;
		}
		order.SubtotalAmount = order.Lines.Sum((BreakfastCafeOrderLine x) => x.Quantity * x.UnitPrice);
		if (order.DiscountAmount < 0m)
		{
			order.DiscountAmount = 0m;
		}
		if (order.DiscountAmount > order.SubtotalAmount)
		{
			order.DiscountAmount = order.SubtotalAmount;
		}
		decimal num3 = order.Lines.Sum((BreakfastCafeOrderLine x) => x.VatAmount);
		if (order.SubtotalAmount > 0m && order.DiscountAmount > 0m)
		{
			decimal num4 = (order.SubtotalAmount - order.DiscountAmount) / order.SubtotalAmount;
			order.VatAmount = Math.Round(num3 * num4, 2, MidpointRounding.AwayFromZero);
		}
		else
		{
			order.VatAmount = num3;
		}
		order.ServiceChargeAmount = ((order.OrderType == "RoomService") ? Math.Round((order.SubtotalAmount - order.DiscountAmount) * 0.05m, 2, MidpointRounding.AwayFromZero) : 0m);
		order.TotalAmount = Math.Round(order.SubtotalAmount - order.DiscountAmount + order.VatAmount + order.ServiceChargeAmount, 2, MidpointRounding.AwayFromZero);
		if (order.PaidAmount > order.TotalAmount)
		{
			order.PaidAmount = order.TotalAmount;
		}
		order.RemainingAmount = Math.Max(0m, order.TotalAmount - order.PaidAmount);
		order.UpdatedAt = DateTime.UtcNow;
	}

	private static string AppendMergeNote(string? note, BreakfastCafeOrder sourceOrder)
	{
		string sourceLabel = string.IsNullOrWhiteSpace(sourceOrder.TableName) ? sourceOrder.OrderNumber : sourceOrder.TableName!;
		string mergeText = "Gộp từ " + sourceLabel;
		if (string.IsNullOrWhiteSpace(note))
		{
			return mergeText;
		}

		return note.Contains(mergeText, StringComparison.OrdinalIgnoreCase) ? note : (note + " | " + mergeText);
	}

	private static string? MergeOrderNotes(string? targetNote, BreakfastCafeOrder sourceOrder)
	{
		string sourceLabel = string.IsNullOrWhiteSpace(sourceOrder.TableName) ? sourceOrder.OrderNumber : sourceOrder.TableName!;
		string sourceNote = string.IsNullOrWhiteSpace(sourceOrder.Note) ? $"Gộp từ {sourceLabel}" : $"Gộp từ {sourceLabel}: {sourceOrder.Note}";
		if (string.IsNullOrWhiteSpace(targetNote))
		{
			return sourceNote;
		}

		return targetNote.Contains(sourceNote, StringComparison.OrdinalIgnoreCase) ? targetNote : $"{targetNote} | {sourceNote}";
	}

	private async Task<JournalHeader> BuildAndPostJournalAsync(BreakfastCafeOrder order, string method, PayBreakfastCafeOrderRequest request, DateTime paymentDate, CancellationToken cancellationToken)
	{
		string standardCode = await ResolveAccountingStandardAsync(cancellationToken);
		if (1 == 0)
		{
		}
		string text = method switch
		{
			"Cash" => "Cash", 
			"BankTransfer" => "Bank", 
			"CardPOS" => "Bank", 
			"RoomCharge" => "Receivable", 
			"CustomerDebt" => "Receivable", 
			_ => "Cash", 
		};
		if (1 == 0)
		{
		}
		string cashOrBankRole = text;
		(string AccountCode, string AccountName) debitAccount = await ResolveAccountByRolesAsync(standardCode, cashOrBankRole, "Cash", "Bank", "Receivable");
		(string AccountCode, string AccountName) revenueAccount = await ResolveAccountByRolesAsync(standardCode, "SalesRevenue", "Revenue", "ServiceRevenue");
		(string AccountCode, string AccountName) vatAccount = await ResolveAccountByRolesAsync(standardCode, "OutputVAT", "VatOutput", "TaxOutput");
		JournalHeader journalHeader = new JournalHeader
		{
			Id = Guid.NewGuid()
		};
		JournalHeader journalHeader2 = journalHeader;
		journalHeader2.JournalNo = await GenerateJournalNoAsync(cancellationToken);
		journalHeader.JournalDate = paymentDate;
		journalHeader.PostingDate = paymentDate;
		journalHeader.SourceType = "BreakfastCafeOrder";
		journalHeader.SourceId = order.Id;
		journalHeader.SourceNo = order.OrderNumber;
		journalHeader.Description = "Thu tiền bán đồ ăn sáng/cà phê " + order.OrderNumber;
		journalHeader.PeriodCode = paymentDate.ToString("yyyy-MM");
		journalHeader.Status = JournalStatus.Draft;
		journalHeader.CreatedBy = request.CreatedBy?.Trim() ?? "system";
		journalHeader.CreatedAt = DateTime.UtcNow;
		JournalHeader journal = journalHeader;
		decimal netRevenue = Math.Max(0m, order.TotalAmount - order.VatAmount);
		journal.Lines.Add(new JournalLine
		{
			Id = Guid.NewGuid(),
			LineNo = 1,
			AccountCode = debitAccount.AccountCode,
			AccountName = debitAccount.AccountName,
			DebitAmount = request.Amount,
			CreditAmount = 0m
		});
		journal.Lines.Add(new JournalLine
		{
			Id = Guid.NewGuid(),
			LineNo = 2,
			AccountCode = revenueAccount.AccountCode,
			AccountName = revenueAccount.AccountName,
			DebitAmount = 0m,
			CreditAmount = Math.Min(netRevenue, request.Amount)
		});
		if (order.VatAmount > 0m)
		{
			journal.Lines.Add(new JournalLine
			{
				Id = Guid.NewGuid(),
				LineNo = 3,
				AccountCode = vatAccount.AccountCode,
				AccountName = vatAccount.AccountName,
				DebitAmount = 0m,
				CreditAmount = Math.Min(order.VatAmount, Math.Max(0m, request.Amount - Math.Min(netRevenue, request.Amount)))
			});
		}
		JournalHeader journalDraft = await _journalService.CreateDraftAsync(journal, cancellationToken);
		return await _journalService.PostDraftAsync(journalDraft.Id, request.CreatedBy, cancellationToken);
	}

	private async Task CreatePaymentFlowAsync(BreakfastCafeOrder order, PayBreakfastCafeOrderRequest request, string method, CancellationToken cancellationToken)
	{
		Guid sourceId = order.Id;
		if (method == "Cash")
		{
			Guid? guid = request.CashFundId ?? (await _cashFundService.GetFundsAsync(cancellationToken)).FirstOrDefault((CashFundDto x) => x.IsDefault)?.Id;
			Guid? fundId = guid;
			if (!fundId.HasValue)
			{
				throw new InvalidOperationException("Không có quỹ tiền mặt mặc định.");
			}
			CashTransactionDto cash = await _cashFundService.CreateReceiptAsync(new CashTransactionRequest
			{
				FundId = fundId.Value,
				TransactionDate = (request.PaymentDate ?? DateTime.Today),
				Amount = request.Amount,
				SourceType = "BreakfastCafeOrder",
				SourceId = sourceId,
				Description = "Thu tiền " + order.OrderNumber,
				CreatedBy = request.CreatedBy
			}, cancellationToken);
			// POS cashier only creates the cash receipt draft.
			// Fund/accounting users approve and post after handover.
				// Receipt stays Draft — posted later by fund manager
		}
		else if ((method == "BankTransfer" || method == "CardPOS") ? true : false)
		{
			Guid? guid2 = request.BankAccountId ?? (await _bankingService.GetAccountsAsync(cancellationToken)).FirstOrDefault((BankAccountDto x) => x.IsDefault)?.Id;
			Guid? bankId = guid2;
			if (!bankId.HasValue)
			{
				throw new InvalidOperationException("Không có tài khoản ngân hàng mặc định.");
			}
			string standardCode = await ResolveAccountingStandardAsync(cancellationToken);
			(string AccountCode, string AccountName) revenueAccount = await ResolveAccountByRolesAsync(standardCode, "SalesRevenue", "Revenue", "ServiceRevenue");
			BankTransactionDto bank = await _bankingService.CreateReceiptAsync(new BankTransactionRequest
			{
				BankAccountId = bankId.Value,
				TransactionDate = (request.PaymentDate ?? DateTime.Today),
				Amount = request.Amount,
				SourceType = "BreakfastCafeOrder",
				SourceId = sourceId,
				Description = "Thu tiền " + order.OrderNumber,
				ReferenceNo = request.ReferenceNo,
				CounterAccountCode = revenueAccount.AccountCode,
				CreatedBy = request.CreatedBy
			}, cancellationToken);
			// POS cashier only creates the bank receipt draft.
			// Fund/accounting users approve and post after reconciliation.
				// Bank receipt stays Draft — posted later by fund manager
		}
		else if (method == "RoomCharge" || method == "CustomerDebt")
		{
			await BuildAndPostJournalAsync(order, method, request, (request.PaymentDate ?? DateTime.Today).Date, cancellationToken);
		}
	}

	private async Task<string> GenerateOrderNumberAsync(DateTime businessDate, CancellationToken cancellationToken)
	{
		string prefix = $"ORD-{businessDate:yyyyMMdd}-";
		string? latest = await (from x in _dbContext.BreakfastCafeOrders.AsNoTracking()
			where x.OrderNumber.StartsWith(prefix)
			orderby x.OrderNumber descending
			select x.OrderNumber).FirstOrDefaultAsync(cancellationToken);
		int next = 1;
		int num2;
		int n = default(int);
		if (!string.IsNullOrWhiteSpace(latest))
		{
			int length = latest.Length;
			int num = length - 4;
			num2 = (int.TryParse(latest.Substring(num, length - num), out n) ? 1 : 0);
		}
		else
		{
			num2 = 0;
		}
		if (num2 != 0)
		{
			next = n + 1;
		}
		return $"{prefix}{next:0000}";
	}

	private async Task<string> GenerateKitchenTicketNumberAsync(DateTime businessDate, CancellationToken cancellationToken)
	{
		string prefix = $"KOT-{businessDate:yyyyMMdd}-";
		string? latest = await (from x in _dbContext.BreakfastCafeKitchenTickets.AsNoTracking()
			where x.TicketNumber.StartsWith(prefix)
			orderby x.TicketNumber descending
			select x.TicketNumber).FirstOrDefaultAsync(cancellationToken);
		int next = 1;
		int num2;
		int n = default(int);
		if (!string.IsNullOrWhiteSpace(latest))
		{
			int length = latest.Length;
			int num = length - 4;
			num2 = (int.TryParse(latest.Substring(num, length - num), out n) ? 1 : 0);
		}
		else
		{
			num2 = 0;
		}
		if (num2 != 0)
		{
			next = n + 1;
		}
		return $"{prefix}{next:0000}";
	}

	private async Task<int> GetNextKitchenTicketSequenceAsync(DateTime businessDate, CancellationToken cancellationToken)
	{
		string prefix = $"KOT-{businessDate:yyyyMMdd}-";
		string? latest = await (from x in _dbContext.BreakfastCafeKitchenTickets.AsNoTracking()
			where x.TicketNumber.StartsWith(prefix)
			orderby x.TicketNumber descending
			select x.TicketNumber).FirstOrDefaultAsync(cancellationToken);
		if (!string.IsNullOrWhiteSpace(latest)
			&& latest.Length >= prefix.Length + 4
			&& int.TryParse(latest.Substring(latest.Length - 4), out int current))
		{
			return current + 1;
		}
		return 1;
	}

	private async Task EnsurePosDefaultsAsync(CancellationToken cancellationToken)
	{
		DateTime now = DateTime.UtcNow;
		PosTable[] defaults = new PosTable[24]
		{
			New("floor-1", "Tầng 1", "A01", "A01", "DineIn", 1),
			New("floor-1", "Tầng 1", "A02", "A02", "DineIn", 2, 4),
			New("floor-1", "Tầng 1", "A03", "A03", "DineIn", 3, 4),
			New("floor-1", "Tầng 1", "A04", "A04", "DineIn", 4, 4),
			New("floor-1", "Tầng 1", "A05", "A05", "DineIn", 5, 4),
			New("floor-1", "Tầng 1", "A06", "A06", "DineIn", 6, 4),
			New("floor-1", "Tầng 1", "A07", "A07", "DineIn", 7, 4),
			New("floor-1", "Tầng 1", "A08", "A08", "DineIn", 8, 4),
			New("floor-2", "Tầng 2", "B01", "B01", "DineIn", 21, 4),
			New("floor-2", "Tầng 2", "B02", "B02", "DineIn", 22, 4),
			New("floor-2", "Tầng 2", "B03", "B03", "DineIn", 23, 4),
			New("floor-2", "Tầng 2", "B04", "B04", "DineIn", 24, 4),
			New("floor-2", "Tầng 2", "B05", "B05", "DineIn", 25, 4),
			New("floor-2", "Tầng 2", "B06", "B06", "DineIn", 26, 4),
			New("garden", "Sân vườn", "G01", "Sân vườn 1", "DineIn", 41, 4),
			New("garden", "Sân vườn", "G02", "Sân vườn 2", "DineIn", 42, 4),
			New("garden", "Sân vườn", "G03", "Sân vườn 3", "DineIn", 43, 4),
			New("garden", "Sân vườn", "G04", "Sân vườn 4", "DineIn", 44, 4),
			New("takeaway", "Mang đi", "TAKEAWAY", "Mang đi", "TakeAway", 61, 1),
			New("hotel", "Khách sạn", "R101", "Phòng 101", "RoomService", 81),
			New("hotel", "Khách sạn", "R102", "Phòng 102", "RoomService", 82),
			New("hotel", "Khách sạn", "R103", "Phòng 103", "RoomService", 83),
			New("hotel", "Khách sạn", "R104", "Phòng 104", "RoomService", 84),
			New("hotel", "Khách sạn", "R105", "Phòng 105", "RoomService", 85)
		};
		HashSet<string> existingSet = (await _dbContext.PosTables.Select((PosTable x) => x.TableCode).ToListAsync(cancellationToken)).ToHashSet<string>(StringComparer.OrdinalIgnoreCase);
		_dbContext.PosTables.AddRange(defaults.Where((PosTable x) => !existingSet.Contains(x.TableCode)));
		await _dbContext.SaveChangesAsync(cancellationToken);
		await EnsureMenuPreparationAreasAsync(cancellationToken);
		PosTable New(string areaCode, string areaName, string code, string name, string orderType, int sort, int seats = 2)
		{
			return new PosTable
			{
				Id = Guid.NewGuid(),
				AreaCode = areaCode,
				AreaName = areaName,
				TableCode = code,
				TableName = name,
				OrderType = orderType,
				Status = "Empty",
				SeatCount = seats,
				SortOrder = sort,
				IsActive = true,
				CreatedAt = now,
				UpdatedAt = now
			};
		}
	}

	private async Task EnsureMenuPreparationAreasAsync(CancellationToken cancellationToken)
	{
		List<MenuItem> missing = await _dbContext.MenuItems
			.Include(x => x.MenuCategory)
			.Where(x => x.IsActive && (x.PreparationArea == null || x.PreparationArea == string.Empty))
			.ToListAsync(cancellationToken);
		if (missing.Count == 0)
		{
			return;
		}

		DateTime now = DateTime.UtcNow;
		foreach (MenuItem item in missing)
		{
			item.PreparationArea = IsBarMenuItem(item) ? "Bar" : "Kitchen";
			item.UpdatedAt = now;
		}

		await _dbContext.SaveChangesAsync(cancellationToken);
	}

	private static bool IsBarMenuItem(MenuItem item)
	{
		string code = item.Code.ToUpperInvariant();
		string categoryCode = item.MenuCategory?.Code?.ToLowerInvariant() ?? string.Empty;
		return categoryCode == "coffee"
			|| categoryCode == "tea-drink"
			|| code.StartsWith("CF_", StringComparison.Ordinal)
			|| code.StartsWith("TRA_", StringComparison.Ordinal)
			|| code.StartsWith("NUOC_", StringComparison.Ordinal)
			|| code.Contains("LATTE", StringComparison.Ordinal)
			|| code.Contains("CAPPU", StringComparison.Ordinal)
			|| code.Contains("BAC_XIU", StringComparison.Ordinal);
	}

	private async Task ApplyMenuAvailabilityWarningsAsync(List<MenuItemDto> menu, CancellationToken cancellationToken)
	{
		if (menu.Count == 0)
		{
			return;
		}

		Guid[] menuItemIds = menu.Select(x => x.Id).ToArray();
		List<MenuItemRecipe> recipes = await _dbContext.MenuItemRecipes.AsNoTracking()
			.Include(x => x.Lines)
			.Where(x => menuItemIds.Contains(x.MenuItemId) && x.IsActive && x.IsDefault)
			.ToListAsync(cancellationToken);
		Dictionary<Guid, MenuItemRecipe> recipeMap = recipes.ToDictionary(x => x.MenuItemId);
		int[] warehouseIds = recipes.SelectMany(x => x.Lines)
			.Where(x => x.DefaultWarehouseId.HasValue)
			.Select(x => x.DefaultWarehouseId!.Value)
			.Distinct()
			.ToArray();
		int[] itemIds = recipes.SelectMany(x => x.Lines)
			.Where(x => x.ItemId.HasValue)
			.Select(x => x.ItemId!.Value)
			.Distinct()
			.ToArray();
		Dictionary<int, Warehouse> warehouses = await _dbContext.Warehouses.AsNoTracking()
			.Where(x => warehouseIds.Contains(x.Id))
			.ToDictionaryAsync(x => x.Id, cancellationToken);
		Dictionary<string, StockBalance> balances = await _dbContext.StockBalances.AsNoTracking()
			.Where(x => warehouseIds.Contains(x.WarehouseId) && itemIds.Contains(x.ItemId))
			.ToDictionaryAsync(x => $"{x.WarehouseId}_{x.ItemId}", cancellationToken);
		List<InventoryCostLayer> costRows = await _dbContext.InventoryCostLayers.AsNoTracking()
			.Where(x => itemIds.Contains(x.ItemId) && x.UnitCost > 0)
			.OrderByDescending(x => x.DocumentDate)
			.ThenByDescending(x => x.CreatedAt)
			.ToListAsync(cancellationToken);
		Dictionary<string, decimal> costByItemWarehouse = costRows
			.GroupBy(x => $"{x.WarehouseId}_{x.ItemId}")
			.ToDictionary(x => x.Key, x => x.First().UnitCost);
		Dictionary<int, decimal> costByItem = costRows
			.GroupBy(x => x.ItemId)
			.ToDictionary(x => x.Key, x => x.First().UnitCost);

		foreach (MenuItemDto item in menu)
		{
			if (!item.HasRecipeManagement)
			{
				continue;
			}

			if (!recipeMap.TryGetValue(item.Id, out MenuItemRecipe? recipe) || recipe.Lines.Count == 0)
			{
				item.HasAvailabilityWarning = true;
				item.IsOutOfStock = true;
				item.StockWarning = "Chưa định lượng";
				continue;
			}

			bool hasOutOfStock = false;
			bool hasInsufficient = false;
			bool hasInvalidWarehouse = false;
			bool hasMissingCost = false;
			foreach (MenuItemRecipeLine line in recipe.Lines.Where(x => x.ItemId.HasValue))
			{
				if (!line.DefaultWarehouseId.HasValue
					|| !warehouses.TryGetValue(line.DefaultWarehouseId.Value, out Warehouse? warehouse)
					|| !warehouse.IsActive
					|| !warehouse.IsConsumptionWarehouse)
				{
					hasInvalidWarehouse = true;
					continue;
				}

				decimal required = line.Quantity * (1 + line.LossRate);
				balances.TryGetValue($"{line.DefaultWarehouseId.Value}_{line.ItemId!.Value}", out StockBalance? balance);
				decimal available = balance?.ClosingQty ?? 0m;
				decimal unitCost = costByItemWarehouse.TryGetValue($"{line.DefaultWarehouseId.Value}_{line.ItemId.Value}", out decimal warehouseCost)
					? warehouseCost
					: costByItem.TryGetValue(line.ItemId.Value, out decimal latestCost)
						? latestCost
						: balance?.AvgPrice ?? 0m;
				if (unitCost <= 0m)
				{
					hasMissingCost = true;
				}
				if (available <= 0m)
				{
					hasOutOfStock = true;
				}
				else if (required > available)
				{
					hasInsufficient = true;
				}
			}

			item.HasAvailabilityWarning = hasInvalidWarehouse || hasOutOfStock || hasInsufficient || hasMissingCost;
			item.IsOutOfStock = item.HasAvailabilityWarning;
			item.IsLowStock = false;
			item.StockWarning = hasInvalidWarehouse ? "Thiếu kho tiêu hao"
				: hasOutOfStock ? "Hết hàng"
				: hasInsufficient ? "Tồn kho không đủ"
				: hasMissingCost ? "Thiếu giá vốn"
				: null;
		}
	}

	private async Task<string?> GetMenuItemAvailabilityWarningAsync(Guid menuItemId, decimal quantity, bool hasRecipeManagement, CancellationToken cancellationToken)
	{
		if (!hasRecipeManagement)
		{
			return null;
		}

		MenuItemRecipe? recipe = await _dbContext.MenuItemRecipes.AsNoTracking()
			.Include(x => x.Lines)
			.FirstOrDefaultAsync(x => x.MenuItemId == menuItemId && x.IsActive && x.IsDefault, cancellationToken);
		if (recipe == null || recipe.Lines.Count == 0)
		{
			return "Chưa định lượng";
		}

		int[] warehouseIds = recipe.Lines
			.Where(x => x.DefaultWarehouseId.HasValue)
			.Select(x => x.DefaultWarehouseId!.Value)
			.Distinct()
			.ToArray();
		int[] itemIds = recipe.Lines
			.Where(x => x.ItemId.HasValue)
			.Select(x => x.ItemId!.Value)
			.Distinct()
			.ToArray();
		Dictionary<int, Warehouse> warehouses = await _dbContext.Warehouses.AsNoTracking()
			.Where(x => warehouseIds.Contains(x.Id))
			.ToDictionaryAsync(x => x.Id, cancellationToken);
		Dictionary<string, StockBalance> balances = await _dbContext.StockBalances.AsNoTracking()
			.Where(x => warehouseIds.Contains(x.WarehouseId) && itemIds.Contains(x.ItemId))
			.ToDictionaryAsync(x => $"{x.WarehouseId}_{x.ItemId}", cancellationToken);
		List<InventoryCostLayer> costRows = await _dbContext.InventoryCostLayers.AsNoTracking()
			.Where(x => itemIds.Contains(x.ItemId) && x.UnitCost > 0)
			.OrderByDescending(x => x.DocumentDate)
			.ThenByDescending(x => x.CreatedAt)
			.ToListAsync(cancellationToken);
		Dictionary<string, decimal> costByItemWarehouse = costRows
			.GroupBy(x => $"{x.WarehouseId}_{x.ItemId}")
			.ToDictionary(x => x.Key, x => x.First().UnitCost);
		Dictionary<int, decimal> costByItem = costRows
			.GroupBy(x => x.ItemId)
			.ToDictionary(x => x.Key, x => x.First().UnitCost);

		foreach (MenuItemRecipeLine line in recipe.Lines.Where(x => x.ItemId.HasValue))
		{
			if (!line.DefaultWarehouseId.HasValue
				|| !warehouses.TryGetValue(line.DefaultWarehouseId.Value, out Warehouse? warehouse)
				|| !warehouse.IsActive
				|| !warehouse.IsConsumptionWarehouse)
			{
				return "Thiếu kho tiêu hao";
			}

			decimal required = line.Quantity * (1 + line.LossRate) * quantity;
			balances.TryGetValue($"{line.DefaultWarehouseId.Value}_{line.ItemId!.Value}", out StockBalance? balance);
			decimal available = balance?.ClosingQty ?? 0m;
			decimal unitCost = costByItemWarehouse.TryGetValue($"{line.DefaultWarehouseId.Value}_{line.ItemId.Value}", out decimal warehouseCost)
				? warehouseCost
				: costByItem.TryGetValue(line.ItemId.Value, out decimal latestCost)
					? latestCost
					: balance?.AvgPrice ?? 0m;
			if (unitCost <= 0m)
			{
				return "Thiếu giá vốn";
			}
			if (available <= 0m)
			{
				return "Hết hàng";
			}
			if (required > available)
			{
				return "Tồn kho không đủ";
			}
		}

		return null;
	}

	private async Task EnsureRecipeStockAvailableAsync(BreakfastCafeOrder order, CancellationToken cancellationToken)
	{
		List<RequiredStockRow> rows = await BuildRequiredStockRowsAsync(order, cancellationToken);
		if (rows.Count == 0)
		{
			return;
		}

		int[] warehouseIds = rows.Select(x => x.WarehouseId).Distinct().ToArray();
		int[] itemIds = rows.Select(x => x.ItemId).Distinct().ToArray();
		Dictionary<int, Warehouse> warehouses = await _dbContext.Warehouses.AsNoTracking()
			.Where(x => warehouseIds.Contains(x.Id))
			.ToDictionaryAsync(x => x.Id, cancellationToken);
		List<string> invalidWarehouses = rows
			.Where(x => !warehouses.TryGetValue(x.WarehouseId, out Warehouse? warehouse) || !warehouse.IsConsumptionWarehouse || !warehouse.IsActive)
			.Select(x => $"{x.ItemName} - kho {x.WarehouseId}")
			.Distinct()
			.ToList();
		if (invalidWarehouses.Count > 0)
		{
			throw new InvalidOperationException("Định lượng POS chỉ được dùng kho tiêu hao đang hoạt động: " + string.Join("; ", invalidWarehouses) + ".");
		}

		Dictionary<string, StockBalance> balances = await _dbContext.StockBalances.AsNoTracking()
			.Where(x => warehouseIds.Contains(x.WarehouseId) && itemIds.Contains(x.ItemId))
			.ToDictionaryAsync(x => $"{x.WarehouseId}_{x.ItemId}", cancellationToken);
		List<string> shortages = new List<string>();
		foreach (RequiredStockRow row in rows)
		{
			balances.TryGetValue($"{row.WarehouseId}_{row.ItemId}", out StockBalance? balance);
			decimal available = balance?.ClosingQty ?? 0m;
			if (row.RequiredQuantity > available)
			{
				string warehouseCode = warehouses.TryGetValue(row.WarehouseId, out Warehouse? warehouse) ? warehouse.WarehouseCode : row.WarehouseId.ToString();
				shortages.Add($"{row.ItemName} tại {warehouseCode}: tồn {available:#,##0.####}, cần {row.RequiredQuantity:#,##0.####}");
			}
		}
		if (shortages.Count > 0)
		{
			throw new InvalidOperationException("Không đủ tồn kho tiêu hao: " + string.Join("; ", shortages) + ".");
		}
	}

	private async Task<List<RequiredStockRow>> BuildRequiredStockRowsAsync(BreakfastCafeOrder order, CancellationToken cancellationToken)
	{
		var orderQuantities = order.Lines
			.GroupBy(x => x.MenuItemId)
			.ToDictionary(x => x.Key, x => x.Sum(line => line.Quantity));
		if (orderQuantities.Count == 0)
		{
			return new List<RequiredStockRow>();
		}

		Guid[] menuItemIds = orderQuantities.Keys.ToArray();
		List<MenuItemRecipe> recipes = await _dbContext.MenuItemRecipes.AsNoTracking()
			.Include(x => x.Lines)
			.Where(x => menuItemIds.Contains(x.MenuItemId) && x.IsActive && x.IsDefault)
			.ToListAsync(cancellationToken);
		List<RequiredStockRow> rows = new List<RequiredStockRow>();
		foreach (MenuItemRecipe recipe in recipes)
		{
			if (!orderQuantities.TryGetValue(recipe.MenuItemId, out decimal orderQuantity))
			{
				continue;
			}
			foreach (MenuItemRecipeLine line in recipe.Lines.Where(x => x.ItemId.HasValue))
			{
				if (!line.DefaultWarehouseId.HasValue)
				{
					throw new InvalidOperationException($"Dòng định lượng {line.ItemName} chưa chọn kho tiêu hao.");
				}
				decimal required = line.Quantity * (1 + line.LossRate) * orderQuantity;
				rows.Add(new RequiredStockRow(line.DefaultWarehouseId.Value, line.ItemId!.Value, line.ItemName, line.UnitName, required));
			}
		}

		return rows
			.GroupBy(x => new { x.WarehouseId, x.ItemId, x.ItemName, x.UnitName })
			.Select(x => new RequiredStockRow(x.Key.WarehouseId, x.Key.ItemId, x.Key.ItemName, x.Key.UnitName, x.Sum(row => row.RequiredQuantity)))
			.ToList();
	}

	private static string NormalizeOrderType(string? orderType)
	{
		return string.IsNullOrWhiteSpace(orderType) ? "DineIn" : orderType.Trim();
	}

	private async Task<string> GenerateJournalNoAsync(CancellationToken cancellationToken)
	{
		string prefix = $"NK{DateTime.Today:yyyyMMdd}";
		string? latest = await (from x in _dbContext.JournalHeaders.AsNoTracking()
			where x.JournalNo.StartsWith(prefix)
			orderby x.JournalNo descending
			select x.JournalNo).FirstOrDefaultAsync(cancellationToken);
		int next = 1;
		int num2;
		int n = default(int);
		if (!string.IsNullOrWhiteSpace(latest))
		{
			int length = latest.Length;
			int num = length - 4;
			num2 = (int.TryParse(latest.Substring(num, length - num), out n) ? 1 : 0);
		}
		else
		{
			num2 = 0;
		}
		if (num2 != 0)
		{
			next = n + 1;
		}
		return $"{prefix}{next:0000}";
	}

	private async Task<string> ResolveAccountingStandardAsync(CancellationToken cancellationToken)
	{
		return (await (from x in _dbContext.AccountingStandards.AsNoTracking()
			where x.IsActive
			orderby x.IsDefault descending
			select x.Code).FirstOrDefaultAsync(cancellationToken)) ?? "TT99";
	}

	private async Task<(string AccountCode, string AccountName)> ResolveAccountByRolesAsync(string standardCode, params string[] roles)
	{
		foreach (string role in roles)
		{
			string? accountCode = await _postingRuleService.ResolveAccountByRoleAsync(role, standardCode);
			if (!string.IsNullOrWhiteSpace(accountCode))
			{
				return new ValueTuple<string, string>(item2: ((await _dbContext.LedgerAccounts.AsNoTracking().FirstOrDefaultAsync((LedgerAccount x) => x.AccountCode == accountCode)) ?? new LedgerAccount
				{
					AccountCode = accountCode,
					AccountName = accountCode
				}).AccountName, item1: accountCode);
			}
		}
		throw new InvalidOperationException("Thiếu AccountRoleMapping cho các role: " + string.Join(", ", roles) + ".");
	}

	private static BreakfastCafeOrderDto ToDtoHeader(BreakfastCafeOrder x)
	{
		return new BreakfastCafeOrderDto
		{
			Id = x.Id,
			PosTableId = x.PosTableId,
			OrderNumber = x.OrderNumber,
			OrderDate = x.OrderDate,
			UpdatedAt = x.UpdatedAt,
			Status = x.Status,
			PaymentStatus = x.PaymentStatus,
			OrderType = x.OrderType,
			ServiceStatus = x.ServiceStatus,
			ServiceStatusText = ToServiceStatusText(x.ServiceStatus),
			AreaName = x.AreaName,
			TableCode = x.TableCode,
			TableName = x.TableName,
			CustomerId = x.CustomerId,
			CustomerReference = x.CustomerReference,
			RoomReference = x.RoomReference,
			SubtotalAmount = x.SubtotalAmount,
			DiscountAmount = x.DiscountAmount,
			VatAmount = x.VatAmount,
			ServiceChargeAmount = x.ServiceChargeAmount,
			TotalAmount = x.TotalAmount,
			PaidAmount = x.PaidAmount,
			RemainingAmount = x.RemainingAmount,
			KitchenSentAt = x.KitchenSentAt,
			KitchenTicketCount = x.KitchenTickets?.Count((BreakfastCafeKitchenTicket t) => string.Equals(t.Status, "Sent", StringComparison.OrdinalIgnoreCase)) ?? 0,
			KitchenHistoryCount = x.KitchenTickets?.Count ?? 0,
			LineCount = (x.Lines?.Count ?? 0),
			Note = x.Note
		};
	}

	private static BreakfastCafeOrderDto ToDto(BreakfastCafeOrder x)
	{
		BreakfastCafeOrderDto breakfastCafeOrderDto = ToDtoHeader(x);
		breakfastCafeOrderDto.Lines = (from l in x.Lines
			orderby l.LineNo
			select new BreakfastCafeOrderLineDto
			{
				Id = l.Id,
					LineNo = l.LineNo,
				MenuItemId = l.MenuItemId,
				MenuItemCode = l.MenuItemCode,
				MenuItemName = l.MenuItemName,
				UnitName = l.UnitName,
				Quantity = l.Quantity,
				UnitPrice = l.UnitPrice,
				LineDiscountAmount = l.LineDiscountAmount,
				VatRate = l.VatRate,
				VatAmount = l.VatAmount,
				LineTotalAmount = l.LineTotalAmount,
				KitchenStatus = l.KitchenStatus,
				Note = l.Note,
				CreatedAt = l.CreatedAt,
				UpdatedAt = l.UpdatedAt,
				ImagePath = l.MenuItem?.ImagePath,
				ImageUrl = l.MenuItem?.ImageUrl
			}).ToList();
		breakfastCafeOrderDto.Payments = (from p in x.Payments
			orderby p.PaymentDate descending
			select new BreakfastCafePaymentDto
			{
				Id = p.Id,
				PaymentDate = p.PaymentDate,
				CreatedAt = p.CreatedAt,
				PaymentMethod = p.PaymentMethod,
				Amount = p.Amount,
				ReferenceNo = p.ReferenceNo,
				Status = p.Status
			}).ToList();
		return breakfastCafeOrderDto;
	}

	private static PosTableDto ToTableDto(PosTable table, BreakfastCafeOrder? order)
	{
		string status = ((order == null) ? table.Status : ((order.PaymentStatus == "Paid") ? "Empty" : ((order.ServiceStatus == "AwaitingPayment") ? "AwaitingPayment" : ((order.Status == "SentKitchen") ? "Serving" : "Serving"))));
		return new PosTableDto
		{
			Id = table.Id,
			AreaCode = table.AreaCode,
			AreaName = table.AreaName,
			TableCode = table.TableCode,
			TableName = table.TableName,
			OrderType = table.OrderType,
			Status = status,
			StatusText = ToTableStatusText(status),
			SeatCount = table.SeatCount,
			SortOrder = table.SortOrder,
			OpenOrderId = order?.Id,
			OpenOrderNumber = order?.OrderNumber,
			OpenOrderTotal = (order?.TotalAmount ?? 0m),
			OpenedAt = order?.CreatedAt
		};
	}

	private static string ToTableStatusText(string status)
	{
		if (1 == 0)
		{
		}
		string result = status switch
		{
			"Serving" => "Đang phục vụ", 
			"Reserved" => "Đã đặt", 
			"AwaitingPayment" => "Chờ thanh toán", 
			_ => "Trống", 
		};
		if (1 == 0)
		{
		}
		return result;
	}

	public async Task<PosTableDto> SaveTableAsync(PosTableDto dto, CancellationToken cancellationToken = default(CancellationToken))
	{
		PosTable table;
		if (dto.Id == Guid.Empty)
		{
			table = new PosTable
			{
				Id = Guid.NewGuid(),
				CreatedAt = DateTime.UtcNow,
				UpdatedAt = DateTime.UtcNow
			};
			_dbContext.PosTables.Add(table);
		}
		else
		{
			table = (await _dbContext.PosTables.FirstOrDefaultAsync((PosTable x) => x.Id == dto.Id, cancellationToken)) ?? throw new InvalidOperationException("Không tìm thấy bàn.");
		}
		table.AreaCode = (dto.AreaCode ?? "default").Trim();
		table.AreaName = (dto.AreaName ?? "Mặc định").Trim();
		table.TableCode = (dto.TableCode ?? "").Trim();
		table.TableName = (dto.TableName ?? dto.TableCode ?? "").Trim();
		table.OrderType = (dto.OrderType ?? "DineIn").Trim();
		table.SeatCount = ((dto.SeatCount > 0) ? dto.SeatCount : 2);
		table.SortOrder = dto.SortOrder;
		table.IsActive = true;
		table.UpdatedAt = DateTime.UtcNow;
		await _dbContext.SaveChangesAsync(cancellationToken);
		return ToTableDto(table, null);
	}

	public async Task DeleteTableAsync(Guid tableId, CancellationToken cancellationToken = default(CancellationToken))
	{
		PosTable table = (await _dbContext.PosTables.FirstOrDefaultAsync((PosTable x) => x.Id == tableId, cancellationToken)) ?? throw new InvalidOperationException("Không tìm thấy bàn.");
		if (await _dbContext.BreakfastCafeOrders.AnyAsync((BreakfastCafeOrder x) => x.PosTableId == tableId && x.Status != "Paid" && x.Status != "Cancelled", cancellationToken))
		{
			throw new InvalidOperationException("Bàn đang có đơn mở, không thể xóa.");
		}
		table.IsActive = false;
		table.UpdatedAt = DateTime.UtcNow;
		await _dbContext.SaveChangesAsync(cancellationToken);
	}

	private static string ToServiceStatusText(string status)
	{
		if (1 == 0)
		{
		}
		string result = status switch
		{
			"New" => "Mới", 
			"SentKitchen" => "Đã gửi bếp", 
			"Serving" => "Đang phục vụ", 
			"AwaitingPayment" => "Chờ thanh toán", 
			"Paid" => "Đã thanh toán", 
			"Cancelled" => "Đã hủy", 
			_ => status, 
		};
		if (1 == 0)
		{
		}
		return result;
	}

	private sealed record RequiredStockRow(int WarehouseId, int ItemId, string ItemName, string UnitName, decimal RequiredQuantity);
}
