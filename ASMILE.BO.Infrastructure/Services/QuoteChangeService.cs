using ASMILE.BO.Application.DTOs;
using ASMILE.BO.Application.Interfaces;
using ASMILE.BO.Core.Entities;
using ASMILE.BO.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ASMILE.BO.Infrastructure.Services;

public sealed class QuoteChangeService : IQuoteChangeService
{
    private readonly AppDbContext _dbContext;
    private readonly IAlacarteRestaurantService _alacarteService;
    private readonly ITableAllocationEngine _tableAllocationEngine;
    private readonly IMenuCalculationEngine _menuCalculationEngine;

    public QuoteChangeService(
        AppDbContext dbContext,
        IAlacarteRestaurantService alacarteService,
        ITableAllocationEngine tableAllocationEngine,
        IMenuCalculationEngine menuCalculationEngine)
    {
        _dbContext = dbContext;
        _alacarteService = alacarteService;
        _tableAllocationEngine = tableAllocationEngine;
        _menuCalculationEngine = menuCalculationEngine;
    }

    public async Task<PreviewAdjustmentResponse> PreviewAdjustmentAsync(PreviewAdjustmentRequest request, CancellationToken cancellationToken = default)
    {
        var quote = await _dbContext.RestaurantQuotes
            .Include(x => x.Lines)
            .Include(x => x.QuoteTables)
            .FirstOrDefaultAsync(x => x.Id == request.QuoteId, cancellationToken)
            ?? throw new InvalidOperationException("Không tìm thấy báo giá.");

        if (!quote.ApprovedOrderId.HasValue)
            throw new InvalidOperationException("Báo giá chưa được duyệt.");

        var order = await _dbContext.AlacarteOrders
            .Include(x => x.Lines)
            .Include(x => x.OrderTables)
            .FirstOrDefaultAsync(x => x.Id == quote.ApprovedOrderId.Value, cancellationToken)
            ?? throw new InvalidOperationException("Không tìm thấy đơn mở.");

        var previousGuestCount = quote.GuestCount;
        var newGuestCount = request.NewGuestCount;
        if (newGuestCount <= 0)
            throw new InvalidOperationException("Số khách phải lớn hơn 0.");

        var tableStandard = quote.QuoteTables
            .Where(x => !x.IsVirtual)
            .Select(x => x.GuestCount)
            .DefaultIfEmpty(10)
            .Max();

        var previousAllocation = _tableAllocationEngine.Allocate(previousGuestCount, tableStandard);
        var newAllocation = _tableAllocationEngine.Allocate(newGuestCount, tableStandard);
        var realTableCount = newAllocation.Count(x => !x.IsVirtual);

        var lineChanges = new List<QuoteChangeRequestDetailDto>();
        var warnings = new List<string>();
        var hasSentLines = order.Lines.Any(x => x.KitchenStatus == "Sent");

        foreach (var quoteLine in quote.Lines)
        {
            var pricingRule = NormalizePricingRule(quoteLine.PricingRule, IsServiceLine(quoteLine));
            var previousCalc = _menuCalculationEngine.Calculate(pricingRule, previousGuestCount,
                previousAllocation.Count(x => !x.IsVirtual), quoteLine.Quantity);
            var newCalc = _menuCalculationEngine.Calculate(pricingRule, newGuestCount,
                realTableCount, quoteLine.Quantity);

            if (previousCalc == newCalc)
                continue;

            var orderLine = order.Lines.FirstOrDefault(x => x.MenuItemId == quoteLine.MenuItemId);
            var isSent = orderLine?.KitchenStatus == "Sent";

            var detail = new QuoteChangeRequestDetailDto
            {
                ChangeType = "QuantityChanged",
                ItemName = quoteLine.Name,
                PreviousQuantity = previousCalc,
                NewQuantity = newCalc,
                QuantityDelta = newCalc - previousCalc,
                PreviousAmount = previousCalc * quoteLine.UnitPrice,
                NewAmount = newCalc * quoteLine.UnitPrice,
                AmountDelta = (newCalc - previousCalc) * quoteLine.UnitPrice,
                IsAlreadySent = isSent,
            };

            if (isSent && pricingRule != "PerRoom")
            {
                detail.Warning = $"Món '{detail.ItemName}' đã gửi bếp ({detail.PreviousQuantity}). Cần tạo Adjustment Ticket riêng.";
                warnings.Add(detail.Warning);
            }

            lineChanges.Add(detail);
        }

        var tableChanges = new List<TableChangeDto>();
        var prevTableNames = previousAllocation.Select(x => x.DisplayName).ToHashSet();
        var newTableNames = newAllocation.Select(x => x.DisplayName).ToHashSet();

        foreach (var table in newAllocation)
        {
            if (!prevTableNames.Contains(table.DisplayName))
                tableChanges.Add(new TableChangeDto { Action = "Add", TableDisplayName = table.DisplayName, GuestCount = table.GuestCount, IsVirtual = table.IsVirtual });
            else
                tableChanges.Add(new TableChangeDto { Action = "Keep", TableDisplayName = table.DisplayName, GuestCount = table.GuestCount, IsVirtual = table.IsVirtual });
        }
        foreach (var table in previousAllocation)
        {
            if (!newTableNames.Contains(table.DisplayName))
                tableChanges.Add(new TableChangeDto { Action = "Remove", TableDisplayName = table.DisplayName, GuestCount = table.GuestCount, IsVirtual = table.IsVirtual });
        }

        var previousTotal = quote.TotalAmount;
        var totalDelta = lineChanges.Sum(x => x.AmountDelta);
        var newTotal = previousTotal + totalDelta;
        var requiresFullRebuild = !hasSentLines;

        var response = new PreviewAdjustmentResponse
        {
            Preview = new QuoteChangeRequestDto
            {
                QuoteId = quote.Id,
                PreviousGuestCount = previousGuestCount,
                NewGuestCount = newGuestCount,
                GuestCountDelta = newGuestCount - previousGuestCount,
                PreviousTotalAmount = previousTotal,
                NewTotalAmount = newTotal,
                AmountDelta = totalDelta,
                OrderStatus = order.Status,
                HasSentLines = hasSentLines,
                CanApply = order.Status != "Paid" && order.Status != "Cancelled",
                Details = lineChanges,
            },
            TableChanges = tableChanges,
            LineChanges = lineChanges,
            TotalDelta = totalDelta,
            Warnings = warnings,
            RequiresFullRebuild = requiresFullRebuild,
        };

        return response;
    }

    public async Task<QuoteChangeRequestDto> RequestChangeAsync(RequestChangeRequest req, CancellationToken cancellationToken = default)
    {
        var quote = await _dbContext.RestaurantQuotes
            .FirstOrDefaultAsync(x => x.Id == req.QuoteId, cancellationToken)
            ?? throw new InvalidOperationException("Không tìm thấy báo giá.");

        if (!quote.ApprovedOrderId.HasValue)
            throw new InvalidOperationException("Báo giá chưa được duyệt.");

        if (await _dbContext.QuoteChangeRequests.AnyAsync(x => x.QuoteId == req.QuoteId && x.Status == "Pending", cancellationToken))
            throw new InvalidOperationException("Đã có yêu cầu sửa đang chờ duyệt cho báo giá này.");

        var prevGuestCount = quote.GuestCount;
        var newGuestCount = req.NewGuestCount;
        if (newGuestCount <= 0)
            throw new InvalidOperationException("Số khách phải lớn hơn 0.");

        var changeRequest = new QuoteChangeRequest
        {
            QuoteId = quote.Id,
            RequestNumber = $"CR-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..4].ToUpperInvariant()}",
            PreviousGuestCount = prevGuestCount,
            NewGuestCount = newGuestCount,
            GuestCountDelta = newGuestCount - prevGuestCount,
            Reason = req.Reason,
            Status = "Pending",
            RequestedBy = req.RequestedBy,
            RequestedAt = DateTime.UtcNow,
            PreviousTotalAmount = quote.TotalAmount,
            PreviousOrderId = quote.ApprovedOrderId!.Value,
        };

        _dbContext.QuoteChangeRequests.Add(changeRequest);

        _dbContext.QuoteAuditLogs.Add(new QuoteAuditLog
        {
            QuoteId = quote.Id,
            Action = "ChangeRequested",
            ChangedBy = req.RequestedBy,
            FieldName = "GuestCount",
            OldValue = prevGuestCount.ToString(),
            NewValue = newGuestCount.ToString(),
            ChangeRequestId = changeRequest.Id,
            Note = req.Reason,
        });

        await _dbContext.SaveChangesAsync(cancellationToken);
        return MapToDto(changeRequest);
    }

    public async Task<QuoteChangeRequestDto> ApproveChangeRequestAsync(ReviewChangeRequest req, CancellationToken cancellationToken = default)
    {
        var cr = await _dbContext.QuoteChangeRequests
            .FirstOrDefaultAsync(x => x.Id == req.ChangeRequestId, cancellationToken)
            ?? throw new InvalidOperationException("Không tìm thấy yêu cầu sửa.");

        if (cr.Status != "Pending")
            throw new InvalidOperationException($"Yêu cầu sửa đang ở trạng thái '{cr.Status}', không thể duyệt.");

        cr.Status = "Approved";
        cr.ReviewedBy = req.ReviewedBy;
        cr.ReviewedAt = DateTime.UtcNow;

        _dbContext.QuoteAuditLogs.Add(new QuoteAuditLog
        {
            QuoteId = cr.QuoteId,
            Action = "ChangeApproved",
            ChangedBy = req.ReviewedBy,
            ChangeRequestId = cr.Id,
            Note = $"Duyệt yêu cầu sửa: {cr.PreviousGuestCount} → {cr.NewGuestCount} khách",
        });

        await _dbContext.SaveChangesAsync(cancellationToken);
        return MapToDto(cr);
    }

    public async Task<QuoteChangeRequestDto> RejectChangeRequestAsync(ReviewChangeRequest req, CancellationToken cancellationToken = default)
    {
        var cr = await _dbContext.QuoteChangeRequests
            .FirstOrDefaultAsync(x => x.Id == req.ChangeRequestId, cancellationToken)
            ?? throw new InvalidOperationException("Không tìm thấy yêu cầu sửa.");

        if (cr.Status != "Pending")
            throw new InvalidOperationException($"Yêu cầu sửa đang ở trạng thái '{cr.Status}', không thể từ chối.");

        cr.Status = "Rejected";
        cr.ReviewedBy = req.ReviewedBy;
        cr.ReviewedAt = DateTime.UtcNow;

        _dbContext.QuoteAuditLogs.Add(new QuoteAuditLog
        {
            QuoteId = cr.QuoteId,
            Action = "ChangeRejected",
            ChangedBy = req.ReviewedBy,
            ChangeRequestId = cr.Id,
            Note = $"Từ chối yêu cầu sửa: {cr.PreviousGuestCount} → {cr.NewGuestCount} khách",
        });

        await _dbContext.SaveChangesAsync(cancellationToken);
        return MapToDto(cr);
    }

    public async Task<QuoteChangeRequestDto> ApplyChangeAsync(ApplyChangeRequest req, CancellationToken cancellationToken = default)
    {
        var strategy = _dbContext.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            using var tx = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                var cr = await _dbContext.QuoteChangeRequests
                    .FirstOrDefaultAsync(x => x.Id == req.ChangeRequestId, cancellationToken)
                    ?? throw new InvalidOperationException("Không tìm thấy yêu cầu sửa.");

                if (cr.Status != "Approved")
                    throw new InvalidOperationException($"Yêu cầu sửa phải được duyệt trước khi áp dụng (hiện tại: '{cr.Status}').");

                var quote = await _dbContext.RestaurantQuotes
                    .Include(x => x.Lines)
                    .Include(x => x.QuoteTables)
                    .FirstOrDefaultAsync(x => x.Id == cr.QuoteId, cancellationToken)
                    ?? throw new InvalidOperationException("Không tìm thấy báo giá.");

                var order = await _dbContext.AlacarteOrders
                    .Include(x => x.Lines)
                    .Include(x => x.OrderTables)
                    .Include(x => x.ExtraServices)
                    .FirstOrDefaultAsync(x => x.Id == cr.PreviousOrderId, cancellationToken)
                    ?? throw new InvalidOperationException("Không tìm thấy đơn mở.");

                if (order.Status == "Paid" || order.Status == "Cancelled")
                    throw new InvalidOperationException($"Đơn mở đã '{order.Status}', không thể áp dụng thay đổi.");

                var hasSentLines = order.Lines.Any(x => x.KitchenStatus == "Sent");

                if (!hasSentLines)
                {
                    await ApplyFullRebuildAsync(quote, order, cr, req.AppliedBy, cancellationToken);
                }
                else
                {
                    await ApplyAdjustmentPlanAsync(quote, order, cr, req.AppliedBy, cancellationToken);
                }

                cr.Status = "Applied";
                cr.AppliedBy = req.AppliedBy;
                cr.AppliedAt = DateTime.UtcNow;

                _dbContext.QuoteAuditLogs.Add(new QuoteAuditLog
                {
                    QuoteId = cr.QuoteId,
                    Action = "ChangeApplied",
                    ChangedBy = req.AppliedBy,
                    ChangeRequestId = cr.Id,
                    FieldName = "GuestCount",
                    OldValue = cr.PreviousGuestCount.ToString(),
                    NewValue = cr.NewGuestCount.ToString(),
                    Note = $"Đã áp dụng thay đổi. Chênh lệch tiền: {cr.AmountDelta:N0} đ",
                });

                await _dbContext.SaveChangesAsync(cancellationToken);
                await tx.CommitAsync(cancellationToken);
                return MapToDto(cr);
            }
            catch (DbUpdateConcurrencyException)
            {
                await tx.RollbackAsync(cancellationToken);
                throw new InvalidOperationException(
                    "Yêu cầu sửa đã được xử lý bởi người khác. Vui lòng tải lại trang.");
            }
            catch (NotSupportedException) { throw; }
            catch (InvalidOperationException) { throw; }
            catch
            {
                await tx.RollbackAsync(cancellationToken);
                throw;
            }
        });
    }

    private async Task ApplyFullRebuildAsync(RestaurantQuote quote, AlacarteOrder oldOrder, QuoteChangeRequest cr, string appliedBy, CancellationToken ct)
    {
        var reservationId = oldOrder.ReservationId;
        oldOrder.Status = "Replaced";
        oldOrder.ReservationId = null;
        oldOrder.UpdatedAt = DateTime.UtcNow;

        var tableStandard = quote.QuoteTables
            .Where(x => !x.IsVirtual)
            .Select(x => x.GuestCount)
            .DefaultIfEmpty(10)
            .Max();

        quote.GuestCount = cr.NewGuestCount;
        quote.UpdatedAt = DateTime.UtcNow;

        _dbContext.RestaurantQuoteTables.RemoveRange(quote.QuoteTables);

        var allocation = _tableAllocationEngine.Allocate(cr.NewGuestCount, tableStandard);
        var newQuoteTables = new List<RestaurantQuoteTable>();
        foreach (var alloc in allocation)
        {
            newQuoteTables.Add(new RestaurantQuoteTable
            {
                RestaurantQuoteId = quote.Id,
                TableNo = alloc.TableNo,
                DisplayName = alloc.DisplayName,
                GuestCount = alloc.GuestCount,
                IsVirtual = alloc.IsVirtual,
                Note = alloc.IsVirtual ? "Tổng hợp thanh toán" : null,
                SortOrder = alloc.TableNo,
            });
        }
        await _dbContext.RestaurantQuoteTables.AddRangeAsync(newQuoteTables, ct);

        foreach (var quoteLine in quote.Lines)
        {
            var pricingRule = NormalizePricingRule(quoteLine.PricingRule, IsServiceLine(quoteLine));
            var realTableCount = allocation.Count(x => !x.IsVirtual);
            quoteLine.CalculatedQuantity = _menuCalculationEngine.Calculate(pricingRule, cr.NewGuestCount, realTableCount, quoteLine.Quantity);
        }

        var newOrder = new AlacarteOrder
        {
            RestaurantQuoteId = quote.Id,
            DiningRoomId = quote.DiningRoomId,
            DiningTableId = oldOrder.DiningTableId,
            ReservationId = reservationId,
            ActualGuestCount = cr.NewGuestCount,
            OrderType = "Advance",
            Status = "Serving",
            PaymentStatus = quote.DepositAmount > 0 ? "Deposit" : "Unpaid",
            OrderDate = oldOrder.OrderDate,
            Note = $"Từ báo giá {quote.QuoteNo} (đã sửa: {cr.PreviousGuestCount} → {cr.NewGuestCount} khách)",
            CreatedBy = appliedBy,
        };

        foreach (var alloc in allocation)
        {
            var qt = newQuoteTables.FirstOrDefault(x => x.TableNo == alloc.TableNo);
            newOrder.OrderTables.Add(new RestaurantOrderTable
            {
                TableNo = alloc.TableNo,
                DisplayName = alloc.DisplayName,
                GuestCount = alloc.GuestCount,
                IsVirtual = alloc.IsVirtual,
                Note = alloc.IsVirtual ? "Tổng hợp thanh toán" : null,
                SortOrder = alloc.TableNo,
                Status = "Serving",
                RestaurantQuoteTableId = qt?.Id,
            });
        }

        foreach (var quoteLine in quote.Lines)
        {
            if (IsServiceLine(quoteLine))
            {
                newOrder.ExtraServices.Add(new RestaurantOrderExtraService
                {
                    RestaurantExtraServiceId = quoteLine.MenuItemId,
                    Description = quoteLine.Name,
                    Qty = quoteLine.CalculatedQuantity > 0 ? quoteLine.CalculatedQuantity : 1,
                    UnitPrice = quoteLine.UnitPrice,
                    Amount = (quoteLine.CalculatedQuantity > 0 ? quoteLine.CalculatedQuantity : 1) * quoteLine.UnitPrice,
                });
                continue;
            }

            var pricingRule = NormalizePricingRule(quoteLine.PricingRule, false);
            var realTables = newOrder.OrderTables.Where(x => !x.IsVirtual).ToList();

            if (pricingRule == "PerPerson" || pricingRule == "PerTable")
            {
                foreach (var servingTable in realTables)
                {
                    var qty = pricingRule == "PerPerson"
                        ? Math.Max(1, servingTable.GuestCount) * Math.Max(1, quoteLine.Quantity)
                        : Math.Max(1, quoteLine.Quantity);
                    AddOrderLine(newOrder, quoteLine, servingTable, qty);
                }
            }
            else
            {
                var summaryTable = newOrder.OrderTables.FirstOrDefault(x => x.IsVirtual);
                var qty = quoteLine.CalculatedQuantity > 0 ? quoteLine.CalculatedQuantity : Math.Max(1, quoteLine.Quantity);
                AddOrderLine(newOrder, quoteLine, summaryTable, qty);
            }
        }

        if (oldOrder.PaidAmount > 0)
        {
            newOrder.PaidAmount = oldOrder.PaidAmount;
            newOrder.RemainingAmount = Math.Max(0, newOrder.TotalAmount - newOrder.PaidAmount);
        }

        RecalculateOrder(newOrder);
        quote.TotalAmount = newOrder.TotalAmount;

        // Re-link deposits from old order to new order
        var deposits = await _dbContext.RestaurantDeposits
            .Where(x => x.AlacarteOrderId == oldOrder.Id)
            .ToListAsync(ct);
        foreach (var d in deposits)
        {
            _dbContext.QuoteAuditLogs.Add(new QuoteAuditLog
            {
                QuoteId = quote.Id,
                Action = "DepositReLinked",
                ChangedBy = appliedBy,
                FieldName = "AlacarteOrderId",
                OldValue = $"{oldOrder.OrderNo} ({oldOrder.Id})",
                NewValue = $"{newOrder.OrderNo} ({newOrder.Id})",
                ChangeRequestId = cr.Id,
                Note = $"Deposit {d.DepositNo} ({d.Amount:N0}đ) chuyển từ đơn cũ sang đơn mới",
            });
            d.AlacarteOrderId = newOrder.Id;
            d.Note = (d.Note ?? "") + $" | Đơn gốc {oldOrder.OrderNo} → {newOrder.OrderNo}";
        }

        // Reservation cleanup: new order keeps it, old order cleared
        if (newOrder.ReservationId.HasValue)
        {
            _dbContext.QuoteAuditLogs.Add(new QuoteAuditLog
            {
                QuoteId = quote.Id,
                Action = "ReservationReLinked",
                ChangedBy = appliedBy,
                FieldName = "ReservationId",
                OldValue = $"Order {oldOrder.OrderNo}",
                NewValue = $"Order {newOrder.OrderNo}",
                ChangeRequestId = cr.Id,
                Note = "Reservation chuyển từ đơn cũ sang đơn mới",
            });
        }

        _dbContext.AlacarteOrders.Add(newOrder);

        cr.NewTotalAmount = quote.TotalAmount;
        cr.AmountDelta = quote.TotalAmount - cr.PreviousTotalAmount;
        cr.NewOrderId = newOrder.Id;

        quote.ApprovedOrderId = newOrder.Id;
    }

    private async Task ApplyAdjustmentPlanAsync(
        RestaurantQuote quote,
        AlacarteOrder order,
        QuoteChangeRequest cr,
        string appliedBy,
        CancellationToken ct)
    {
        var tableStandard = quote.QuoteTables
            .Where(x => !x.IsVirtual)
            .Select(x => x.GuestCount)
            .DefaultIfEmpty(10)
            .Max();

        var allocation = _tableAllocationEngine.Allocate(cr.NewGuestCount, tableStandard);
        var currentRealTables = order.OrderTables.Where(x => !x.IsVirtual).ToList();
        var newRealTables = allocation.Where(x => !x.IsVirtual).ToList();
        var currentQuoteTables = quote.QuoteTables
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Id)
            .ToList();
        var synchronizedQuoteTables = new List<RestaurantQuoteTable>(allocation.Count);

        for (var i = 0; i < allocation.Count; i++)
        {
            var alloc = allocation[i];
            if (i < currentQuoteTables.Count)
            {
                var quoteTable = currentQuoteTables[i];
                quoteTable.TableNo = alloc.TableNo;
                quoteTable.DisplayName = alloc.DisplayName;
                quoteTable.GuestCount = alloc.GuestCount;
                quoteTable.IsVirtual = alloc.IsVirtual;
                quoteTable.Note = alloc.IsVirtual ? "Tổng hợp thanh toán" : null;
                quoteTable.SortOrder = alloc.TableNo;
                quoteTable.UpdatedAt = DateTime.UtcNow;
                synchronizedQuoteTables.Add(quoteTable);
            }
            else
            {
                var quoteTable = new RestaurantQuoteTable
                {
                    RestaurantQuoteId = quote.Id,
                    TableNo = alloc.TableNo,
                    DisplayName = alloc.DisplayName,
                    GuestCount = alloc.GuestCount,
                    IsVirtual = alloc.IsVirtual,
                    Note = alloc.IsVirtual ? "Tổng hợp thanh toán" : null,
                    SortOrder = alloc.TableNo,
                };
                quote.QuoteTables.Add(quoteTable);
                _dbContext.RestaurantQuoteTables.Add(quoteTable);
                synchronizedQuoteTables.Add(quoteTable);
            }
        }

        foreach (var removedQuoteTable in currentQuoteTables.Skip(allocation.Count))
        {
            foreach (var orderTable in order.OrderTables.Where(x => x.RestaurantQuoteTableId == removedQuoteTable.Id))
                orderTable.RestaurantQuoteTableId = null;
            _dbContext.RestaurantQuoteTables.Remove(removedQuoteTable);
        }

        if (newRealTables.Count > currentRealTables.Count)
        {
            for (int i = currentRealTables.Count; i < newRealTables.Count; i++)
            {
                var newAlloc = newRealTables[i];
                order.OrderTables.Add(new RestaurantOrderTable
                {
                    TableNo = newAlloc.TableNo,
                    DisplayName = newAlloc.DisplayName,
                    GuestCount = newAlloc.GuestCount,
                    IsVirtual = false,
                    SortOrder = newAlloc.TableNo,
                    Status = "Serving",
                });
            }
        }
        else if (newRealTables.Count < currentRealTables.Count)
        {
            for (int i = newRealTables.Count; i < currentRealTables.Count; i++)
            {
                currentRealTables[i].Status = "Removed";
                currentRealTables[i].UpdatedAt = DateTime.UtcNow;
            }
        }

        for (int i = 0; i < Math.Min(currentRealTables.Count, newRealTables.Count); i++)
        {
            currentRealTables[i].GuestCount = newRealTables[i].GuestCount;
            currentRealTables[i].RestaurantQuoteTableId = synchronizedQuoteTables
                .FirstOrDefault(x => !x.IsVirtual && x.SortOrder == newRealTables[i].TableNo)?.Id;
        }

        foreach (var orderTable in order.OrderTables.Where(x => x.IsVirtual))
        {
            orderTable.RestaurantQuoteTableId = synchronizedQuoteTables
                .FirstOrDefault(x => x.IsVirtual)?.Id;
        }

        foreach (var quoteLine in quote.Lines)
        {
            if (IsServiceLine(quoteLine))
                continue;

            var pricingRule = NormalizePricingRule(quoteLine.PricingRule, false);
            if (pricingRule == "PerRoom")
                continue;

            var realTables = order.OrderTables.Where(x => !x.IsVirtual && x.Status != "Removed").ToList();
            var existingLines = order.Lines.Where(x => x.MenuItemId == quoteLine.MenuItemId).ToList();

            foreach (var table in realTables)
            {
                var existingLine = existingLines.FirstOrDefault(x => x.RestaurantOrderTableId == table.Id);
                var newQty = pricingRule == "PerPerson"
                    ? Math.Max(1, table.GuestCount) * Math.Max(1, quoteLine.Quantity)
                    : Math.Max(1, quoteLine.Quantity);

                if (existingLine != null)
                {
                    if (existingLine.KitchenStatus == "Sent")
                    {
                        if (newQty != existingLine.Qty + existingLine.SupplementQuantity)
                        {
                            await _alacarteService.AddOrUpdateLineAsync(
                                new SaveAlacarteOrderLineRequest
                                {
                                    OrderId = order.Id,
                                    OrderLineId = existingLine.Id,
                                    OrderTableId = existingLine.RestaurantOrderTableId,
                                    MenuItemId = existingLine.MenuItemId,
                                    Qty = newQty,
                                    SupplementQuantity = 0,
                                    KitchenNote = existingLine.KitchenNote,
                                    SupplementNote = existingLine.SupplementNote,
                                    AdjustmentReason = cr.Reason,
                                    AdjustmentBy = appliedBy,
                                },
                                ct);
                        }
                    }
                    else
                    {
                        existingLine.Qty = newQty;
                        RecalculateLine(existingLine);
                    }
                }
                else if (newQty > 0)
                {
                    AddOrderLine(order, quoteLine, table, newQty);
                }
            }

            foreach (var removed in currentRealTables.Skip(realTables.Count))
            {
                var line = existingLines.FirstOrDefault(x => x.RestaurantOrderTableId == removed.Id);
                if (line != null)
                {
                    if (line.KitchenStatus == "Sent")
                    {
                        await _alacarteService.RemoveLineAsync(
                            order.Id,
                            line.Id,
                            createCancelTicket: true,
                            cancelledBy: appliedBy,
                            cancellationToken: ct);
                    }
                    else
                    {
                        order.Lines.Remove(line);
                    }
                }
            }
        }

        RecalculateOrder(order);
        quote.GuestCount = cr.NewGuestCount;
        var realTableCount = allocation.Count(x => !x.IsVirtual);
        foreach (var quoteLine in quote.Lines)
        {
            var pricingRule = NormalizePricingRule(quoteLine.PricingRule, IsServiceLine(quoteLine));
            quoteLine.CalculatedQuantity = _menuCalculationEngine.Calculate(
                pricingRule,
                cr.NewGuestCount,
                realTableCount,
                quoteLine.Quantity);
        }
        quote.TotalAmount = order.TotalAmount;
        quote.UpdatedAt = DateTime.UtcNow;

        cr.NewOrderId = order.Id;
        cr.NewTotalAmount = order.TotalAmount;
        cr.AmountDelta = order.TotalAmount - cr.PreviousTotalAmount;
    }

    public async Task<IReadOnlyList<QuoteChangeRequestDto>> GetChangeHistoryAsync(Guid quoteId, CancellationToken cancellationToken = default)
    {
        var list = await _dbContext.QuoteChangeRequests
            .Include(x => x.Details)
            .Where(x => x.QuoteId == quoteId)
            .OrderByDescending(x => x.RequestedAt)
            .ToListAsync(cancellationToken);
        return list.Select(MapToDto).ToList();
    }

    public async Task<IReadOnlyList<QuoteAuditLogDto>> GetAuditLogsAsync(Guid quoteId, CancellationToken cancellationToken = default)
    {
        var logs = await _dbContext.QuoteAuditLogs
            .Where(x => x.QuoteId == quoteId)
            .OrderByDescending(x => x.Timestamp)
            .ToListAsync(cancellationToken);
        return logs.Select(x => new QuoteAuditLogDto
        {
            Id = x.Id,
            QuoteId = x.QuoteId,
            Timestamp = x.Timestamp,
            Action = x.Action,
            ChangedBy = x.ChangedBy,
            FieldName = x.FieldName,
            OldValue = x.OldValue,
            NewValue = x.NewValue,
            Note = x.Note,
        }).ToList();
    }

    private static QuoteChangeRequestDto MapToDto(QuoteChangeRequest cr)
    {
        return new QuoteChangeRequestDto
        {
            Id = cr.Id,
            QuoteId = cr.QuoteId,
            RequestNumber = cr.RequestNumber,
            PreviousGuestCount = cr.PreviousGuestCount,
            NewGuestCount = cr.NewGuestCount,
            GuestCountDelta = cr.GuestCountDelta,
            Reason = cr.Reason,
            Status = cr.Status,
            RequestedBy = cr.RequestedBy,
            RequestedAt = cr.RequestedAt,
            ReviewedBy = cr.ReviewedBy,
            ReviewedAt = cr.ReviewedAt,
            AppliedBy = cr.AppliedBy,
            AppliedAt = cr.AppliedAt,
            PreviousTotalAmount = cr.PreviousTotalAmount,
            NewTotalAmount = cr.NewTotalAmount,
            AmountDelta = cr.AmountDelta,
            AdjustmentPlanJson = cr.AdjustmentPlanJson,
            PreviousOrderId = cr.PreviousOrderId,
            NewOrderId = cr.NewOrderId,
            Note = cr.Note,
            Details = cr.Details.Select(d => new QuoteChangeRequestDetailDto
            {
                Id = d.Id,
                ChangeType = d.ChangeType,
                ItemName = d.ItemName,
                PreviousQuantity = d.PreviousQuantity,
                NewQuantity = d.NewQuantity,
                QuantityDelta = d.NewQuantity - d.PreviousQuantity,
                PreviousAmount = d.PreviousAmount,
                NewAmount = d.NewAmount,
                AmountDelta = d.NewAmount - d.PreviousAmount,
                IsAlreadySent = d.IsAlreadySent,
                Note = d.Note,
            }).ToList(),
        };
    }

    private static string NormalizePricingRule(string? pricingRule, bool isServiceLine)
    {
        if (string.Equals(pricingRule, "PerPerson", StringComparison.OrdinalIgnoreCase)) return "PerPerson";
        if (string.Equals(pricingRule, "PerTable", StringComparison.OrdinalIgnoreCase)) return "PerTable";
        if (string.Equals(pricingRule, "PerRoom", StringComparison.OrdinalIgnoreCase)) return "PerRoom";
        return isServiceLine ? "PerRoom" : "Manual";
    }

    private static bool IsServiceLine(RestaurantQuoteLine line)
    {
        var lt = line.LineType ?? string.Empty;
        return lt.Contains("Dịch vụ", StringComparison.OrdinalIgnoreCase)
               || lt.Contains("Service", StringComparison.OrdinalIgnoreCase)
               || lt.Contains("Dich vu", StringComparison.OrdinalIgnoreCase);
    }

    private static void AddOrderLine(AlacarteOrder order, RestaurantQuoteLine quoteLine, RestaurantOrderTable? table, decimal qty)
    {
        order.Lines.Add(new AlacarteOrderLine
        {
            RestaurantOrderTableId = table?.Id,
            MenuItemId = quoteLine.MenuItemId ?? Guid.Empty,
            MenuItemName = quoteLine.Name,
            Qty = qty,
            UnitPrice = quoteLine.UnitPrice,
            PricingRule = quoteLine.PricingRule ?? "Manual",
            LineType = "Normal",
            KitchenStatus = "New",
            LineStatus = "Ordered",
            LineNo = order.Lines.Count + 1,
        });
    }

    private static void RecalculateLine(AlacarteOrderLine line)
    {
        var baseAmount = line.Qty * line.UnitPrice - line.DiscountAmount;
        line.VatAmount = Math.Round(baseAmount * line.VatRate / 100m, 2);
        line.Amount = baseAmount + line.VatAmount;
    }

    private static void RecalculateOrder(AlacarteOrder order)
    {
        var serviceAmount = order.ExtraServices.Sum(x => x.Amount);
        order.SubtotalAmount = order.Lines.Where(x => x.LineStatus != "Cancelled").Sum(x => x.Qty * x.UnitPrice) + serviceAmount;
        order.VatAmount = order.Lines.Where(x => x.LineStatus != "Cancelled").Sum(x => x.VatAmount);
        order.TotalAmount = order.Lines.Where(x => x.LineStatus != "Cancelled").Sum(x => x.Amount) + serviceAmount - order.DiscountAmount + order.ServiceChargeAmount;
        order.RemainingAmount = Math.Max(0, order.TotalAmount - order.PaidAmount);
    }
}
