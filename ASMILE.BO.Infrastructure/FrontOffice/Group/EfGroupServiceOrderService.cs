using ASMILE.BO.Application.FrontOffice.Foundation.Audit;
using ASMILE.BO.Application.FrontOffice.Foundation.Contexts;
using ASMILE.BO.Application.FrontOffice.Foundation.Persistence;
using ASMILE.BO.Application.FrontOffice.Group;
using ASMILE.BO.Application.FrontOffice.Folio;
using ASMILE.BO.Core.FrontOffice.Group;
using ASMILE.BO.Core.FrontOffice.Folio;
using ASMILE.BO.Core.FrontOffice.Foundation.Identifiers;
using ASMILE.BO.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ASMILE.BO.Infrastructure.FrontOffice.Group;

public sealed class EfGroupServiceOrderService(
    AppDbContext db,
    IFrontOfficePersistenceSession persistence,
    IFrontOfficeExecutionContextAccessor contextAccessor,
    IClock clock,
    IHotelFolioRepository folioRepository) : IGroupServiceOrderQueryService, IGroupServiceOrderCommandService
{
    public async Task<IReadOnlyList<GroupServiceOrderDto>> SearchAsync(
        Guid propertyId,
        Guid? groupReservationId,
        CancellationToken ct)
    {
        var query = db.FrontOfficeGroupServiceOrders
            .AsNoTracking()
            .Include(x => x.GroupReservation)
            .Where(x => x.PropertyId == propertyId);
        if (groupReservationId is Guid groupId)
            query = query.Where(x => x.GroupReservationId == groupId);

        var rows = await query
            .OrderBy(x => x.ServiceDate)
            .ThenBy(x => x.ServiceTime)
            .ThenBy(x => x.ServiceCode)
            .ToListAsync(ct);
        return rows.Select(Map).ToList();
    }

    public async Task<GroupServiceOrderDto> CreateAsync(
        CreateGroupServiceOrderRequest request,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        var context = RequireContext();
        var propertyId = context.Property.PropertyId.Value;
        var group = await db.FrontOfficeGroupReservations
            .SingleOrDefaultAsync(x => x.GroupReservationId == request.GroupReservationId && x.PropertyId == propertyId, ct)
            ?? throw new KeyNotFoundException("GROUP_NOT_FOUND");
        if (group.Status is GroupReservationStatus.Cancelled or GroupReservationStatus.Closed or GroupReservationStatus.CheckedOut)
            throw new InvalidOperationException("GROUP_SERVICE_GROUP_NOT_ACTIVE");

        var now = clock.UtcNow;
        var code = await ResolveCodeAsync(propertyId, now, ct);
        var order = GroupServiceOrder.Create(
            propertyId,
            group.GroupReservationId,
            code,
            request.ServiceName,
            request.Category,
            request.ServiceDate,
            request.ServiceTime,
            request.DurationMinutes,
            request.Quantity,
            request.UnitPrice,
            request.DiscountRate,
            request.TaxRate,
            request.ServiceChargeRate,
            request.ChargeTo,
            now,
            context.Actor.ActorId,
            request.SubCategory,
            request.Department,
            request.Provider,
            request.Location,
            request.GuestName,
            request.Remark);

        db.FrontOfficeGroupServiceOrders.Add(order);
        StageAudit("GroupServiceCreated", order, propertyId);
        await CommitAsync(ct, "GROUP_SERVICE_CREATE_FAILED");
        return Map(order);
    }

    public async Task<GroupServiceOrderDto> UpdateAsync(
        UpdateGroupServiceOrderRequest request,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        var context = RequireContext();
        var propertyId = context.Property.PropertyId.Value;
        var order = await db.FrontOfficeGroupServiceOrders
            .Include(x => x.GroupReservation)
            .SingleOrDefaultAsync(x => x.GroupServiceOrderId == request.GroupServiceOrderId && x.PropertyId == propertyId, ct)
            ?? throw new KeyNotFoundException("GROUP_SERVICE_NOT_FOUND");

        if (request.ExpectedRowVersion is not null
            && !(request.ExpectedRowVersion ?? Array.Empty<byte>()).SequenceEqual(order.RowVersion ?? Array.Empty<byte>()))
            throw new InvalidOperationException("GROUP_SERVICE_CONCURRENCY_CONFLICT");

        order.UpdateDetails(
            request.ServiceName,
            request.Category,
            request.ServiceDate,
            request.ServiceTime,
            request.DurationMinutes,
            request.Quantity,
            request.UnitPrice,
            request.DiscountRate,
            request.TaxRate,
            request.ServiceChargeRate,
            request.ChargeTo,
            clock.UtcNow,
            context.Actor.ActorId,
            request.SubCategory,
            request.Department,
            request.Provider,
            request.Location,
            request.GuestName,
            request.Remark);

        StageAudit("GroupServiceUpdated", order, propertyId);
        await CommitAsync(ct, "GROUP_SERVICE_UPDATE_FAILED");
        return Map(order);
    }

    public async Task<GroupServiceOrderDto> ChangeStatusAsync(
        ChangeGroupServiceOrderStatusRequest request,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        var context = RequireContext();
        var propertyId = context.Property.PropertyId.Value;
        var order = await db.FrontOfficeGroupServiceOrders
            .Include(x => x.GroupReservation)
            .SingleOrDefaultAsync(x => x.GroupServiceOrderId == request.GroupServiceOrderId && x.PropertyId == propertyId, ct)
            ?? throw new KeyNotFoundException("GROUP_SERVICE_NOT_FOUND");
        var now = clock.UtcNow;

        switch (request.Status)
        {
            case GroupServiceOrderStatus.Confirmed:
                order.Confirm(now, context.Actor.ActorId);
                break;
            case GroupServiceOrderStatus.InProgress:
                order.Start(now, context.Actor.ActorId);
                break;
            case GroupServiceOrderStatus.Completed:
                order.Complete(now, context.Actor.ActorId);
                break;
            case GroupServiceOrderStatus.Cancelled:
                order.Cancel(request.Reason ?? string.Empty, now, context.Actor.ActorId);
                break;
            default:
                throw new InvalidOperationException("GROUP_SERVICE_STATUS_TRANSITION_NOT_SUPPORTED");
        }

        StageAudit("GroupServiceStatusChanged", order, propertyId, request.Reason);
        await CommitAsync(ct, "GROUP_SERVICE_STATUS_CHANGE_FAILED");
        return Map(order);
    }

    public async Task<GroupServiceBatchStatusResult> ChangeBatchStatusAsync(
        BatchChangeGroupServiceOrderStatusRequest request,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        var context = RequireContext();
        var propertyId = context.Property.PropertyId.Value;
        var orderIds = request.GroupServiceOrderIds?.Where(x => x != Guid.Empty).Distinct().ToArray()
            ?? Array.Empty<Guid>();
        if (orderIds.Length == 0)
            throw new InvalidOperationException("GROUP_SERVICE_BATCH_EMPTY");

        var orders = await db.FrontOfficeGroupServiceOrders
            .Include(x => x.GroupReservation)
            .Where(x => x.PropertyId == propertyId && orderIds.Contains(x.GroupServiceOrderId))
            .ToListAsync(ct);
        if (orders.Count != orderIds.Length)
            throw new KeyNotFoundException("GROUP_SERVICE_NOT_FOUND");
        ValidateBatchTransition(orders, request.Status, request.Reason);

        var now = clock.UtcNow;
        foreach (var order in orders)
        {
            switch (request.Status)
            {
                case GroupServiceOrderStatus.Confirmed:
                    order.Confirm(now, context.Actor.ActorId);
                    break;
                case GroupServiceOrderStatus.Completed:
                    order.Complete(now, context.Actor.ActorId);
                    break;
                case GroupServiceOrderStatus.Cancelled:
                    order.Cancel(request.Reason!, now, context.Actor.ActorId);
                    break;
                default:
                    throw new InvalidOperationException("GROUP_SERVICE_STATUS_TRANSITION_NOT_SUPPORTED");
            }
            StageAudit("GroupServiceStatusChanged", order, propertyId, request.Reason ?? "Batch");
        }

        await CommitAsync(ct, "GROUP_SERVICE_BATCH_STATUS_CHANGE_FAILED");
        return new GroupServiceBatchStatusResult(
            orders.Select(x => x.GroupServiceOrderId).ToArray(), request.Status);
    }

    public async Task<GroupServiceOrderDto> PostToFolioAsync(
        PostGroupServiceOrderToFolioRequest request,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        var context = RequireContext();
        var propertyId = context.Property.PropertyId.Value;
        var order = await db.FrontOfficeGroupServiceOrders
            .Include(x => x.GroupReservation)
            .SingleOrDefaultAsync(x => x.GroupServiceOrderId == request.GroupServiceOrderId && x.PropertyId == propertyId, ct)
            ?? throw new KeyNotFoundException("GROUP_SERVICE_NOT_FOUND");

        if (order.Status != GroupServiceOrderStatus.Completed)
            throw new InvalidOperationException("GROUP_SERVICE_MUST_BE_COMPLETED_BEFORE_FOLIO");

        var alreadyPosted = await db.HotelFolioEntries.AnyAsync(
            x => x.PropertyId == propertyId
                && x.ReferenceType == "GroupServiceOrder"
                && x.ReferenceId == order.GroupServiceOrderId
                && x.ReversedByEntryId == null,
            ct);
        if (alreadyPosted)
            throw new InvalidOperationException("GROUP_SERVICE_ALREADY_POSTED");

        var targetFolioId = request.TargetFolioId ?? await ResolveSingleTargetFolioIdAsync(order, propertyId, ct);
        var folio = await folioRepository.GetAsync(propertyId, targetFolioId, ct)
            ?? throw new KeyNotFoundException("GROUP_SERVICE_FOLIO_NOT_FOUND");
        EnsureOpenFolio(folio.Status);
        var entry = folio.AddGroupServiceCharge(
            order.TotalAmount,
            $"Dịch vụ đoàn {order.ServiceCode} - {order.ServiceName}",
            order.GroupServiceOrderId,
            ResolveEntryType(order.Category),
            clock.UtcNow,
            context.Actor.ActorId,
            context.BusinessDate.BusinessDate);
        await folioRepository.AddEntryAsync(entry, ct);
        order.MarkPostedToFolio(clock.UtcNow, context.Actor.ActorId);
        StageAudit("GroupServicePostedToFolio", order, propertyId, $"FolioId={targetFolioId:D};EntryId={entry.HotelFolioEntryId:D}");
        await CommitAsync(ct, "GROUP_SERVICE_FOLIO_POST_FAILED");
        return Map(order);
    }

    public async Task<GroupServiceBatchPostResult> PostBatchToFolioAsync(
        PostGroupServiceOrdersToFolioRequest request,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        var context = RequireContext();
        var propertyId = context.Property.PropertyId.Value;
        var orderIds = request.GroupServiceOrderIds?.Where(x => x != Guid.Empty).Distinct().ToArray()
            ?? Array.Empty<Guid>();
        if (orderIds.Length == 0)
            throw new InvalidOperationException("GROUP_SERVICE_BATCH_EMPTY");
        if (request.TargetFolioId == Guid.Empty)
            throw new InvalidOperationException("GROUP_SERVICE_FOLIO_TARGET_REQUIRED");

        var orders = await db.FrontOfficeGroupServiceOrders
            .Include(x => x.GroupReservation)
            .Where(x => x.PropertyId == propertyId && orderIds.Contains(x.GroupServiceOrderId))
            .ToListAsync(ct);
        if (orders.Count != orderIds.Length)
            throw new KeyNotFoundException("GROUP_SERVICE_NOT_FOUND");
        if (orders.Any(x => x.Status != GroupServiceOrderStatus.Completed))
            throw new InvalidOperationException("GROUP_SERVICE_BATCH_MUST_BE_COMPLETED");

        var alreadyPostedIds = await db.HotelFolioEntries
            .Where(x => x.PropertyId == propertyId
                && x.ReferenceType == "GroupServiceOrder"
                && x.ReferenceId != null
                && orderIds.Contains(x.ReferenceId.Value)
                && x.ReversedByEntryId == null)
            .Select(x => x.ReferenceId!.Value)
            .Distinct()
            .ToListAsync(ct);
        if (alreadyPostedIds.Count > 0)
            throw new InvalidOperationException("GROUP_SERVICE_BATCH_ALREADY_POSTED");

        var folio = await folioRepository.GetAsync(propertyId, request.TargetFolioId, ct)
            ?? throw new KeyNotFoundException("GROUP_SERVICE_FOLIO_NOT_FOUND");
        EnsureOpenFolio(folio.Status);

        var now = clock.UtcNow;
        foreach (var order in orders.OrderBy(x => x.ServiceDate).ThenBy(x => x.ServiceTime).ThenBy(x => x.ServiceCode))
        {
            var entry = folio.AddGroupServiceCharge(
                order.TotalAmount,
                $"Dịch vụ đoàn {order.ServiceCode} - {order.ServiceName}",
                order.GroupServiceOrderId,
                ResolveEntryType(order.Category),
                now,
                context.Actor.ActorId,
                context.BusinessDate.BusinessDate);
            await folioRepository.AddEntryAsync(entry, ct);
            order.MarkPostedToFolio(now, context.Actor.ActorId);
            StageAudit("GroupServicePostedToFolio", order, propertyId, $"FolioId={request.TargetFolioId:D};EntryId={entry.HotelFolioEntryId:D};Batch=true");
        }

        await CommitAsync(ct, "GROUP_SERVICE_FOLIO_BATCH_POST_FAILED");
        return new GroupServiceBatchPostResult(
            request.TargetFolioId,
            orders.Select(x => x.GroupServiceOrderId).ToArray(),
            orders.Count,
            orders.Sum(x => x.TotalAmount));
    }

    private async Task<Guid> ResolveSingleTargetFolioIdAsync(GroupServiceOrder order, Guid propertyId, CancellationToken ct)
    {
        var folioIds = await db.FrontOfficeGroupRoomAssignments
            .Where(x => x.PropertyId == propertyId && x.GroupReservationId == order.GroupReservationId && x.ReservationId != null)
            .Join(db.HotelStays.Where(x => x.PropertyId == propertyId), assignment => assignment.ReservationId, stay => stay.ReservationId, (_, stay) => stay.HotelStayId)
            .Join(db.HotelFolios.Where(x => x.PropertyId == propertyId && x.Status == HotelFolioStatus.Open), stayId => stayId, folio => folio.HotelStayId, (_, folio) => folio.HotelFolioId)
            .Distinct()
            .ToListAsync(ct);

        return folioIds.Count switch
        {
            0 => throw new InvalidOperationException("GROUP_SERVICE_FOLIO_TARGET_NOT_FOUND"),
            1 => folioIds[0],
            _ => throw new InvalidOperationException("GROUP_SERVICE_FOLIO_TARGET_AMBIGUOUS")
        };
    }

    private static HotelFolioEntryType ResolveEntryType(string category)
    {
        var key = category.Trim().ToLowerInvariant();
        if (key.Contains("restaurant") || key.Contains("breakfast") || key.Contains("beverage") || key.Contains("banquet") || key.Contains("food"))
            return HotelFolioEntryType.Restaurant;
        if (key.Contains("laundry")) return HotelFolioEntryType.Laundry;
        if (key.Contains("spa")) return HotelFolioEntryType.Spa;
        if (key.Contains("meeting") || key.Contains("transport") || key.Contains("equipment") || key.Contains("other"))
            return HotelFolioEntryType.OtherCharge;
        return HotelFolioEntryType.ManualCharge;
    }

    private static void EnsureOpenFolio(HotelFolioStatus status)
    {
        if (status != HotelFolioStatus.Open)
            throw new InvalidOperationException("GROUP_SERVICE_FOLIO_NOT_OPEN");
    }

    private static void ValidateBatchTransition(
        IReadOnlyCollection<GroupServiceOrder> orders,
        GroupServiceOrderStatus status,
        string? reason)
    {
        switch (status)
        {
            case GroupServiceOrderStatus.Confirmed when orders.Any(x => x.Status != GroupServiceOrderStatus.Pending):
                throw new InvalidOperationException("GROUP_SERVICE_BATCH_CONFIRM_REQUIRES_PENDING");
            case GroupServiceOrderStatus.Completed when orders.Any(x => x.Status is not (GroupServiceOrderStatus.Confirmed or GroupServiceOrderStatus.InProgress)):
                throw new InvalidOperationException("GROUP_SERVICE_BATCH_COMPLETE_REQUIRES_ACTIVE");
            case GroupServiceOrderStatus.Cancelled when string.IsNullOrWhiteSpace(reason):
                throw new ArgumentException("GROUP_SERVICE_CANCEL_REASON_REQUIRED");
            case GroupServiceOrderStatus.Cancelled when orders.Any(x => x.Status is GroupServiceOrderStatus.Completed or GroupServiceOrderStatus.PostedToFolio):
                throw new InvalidOperationException("GROUP_SERVICE_STATUS_NOT_CANCELLABLE");
            case GroupServiceOrderStatus.Confirmed or GroupServiceOrderStatus.Completed or GroupServiceOrderStatus.Cancelled:
                return;
            default:
                throw new InvalidOperationException("GROUP_SERVICE_STATUS_TRANSITION_NOT_SUPPORTED");
        }
    }

    private FrontOfficeExecutionContext RequireContext()
        => contextAccessor.Current ?? throw new InvalidOperationException("FRONT_OFFICE_CONTEXT_REQUIRED");

    private async Task<string> ResolveCodeAsync(Guid propertyId, DateTime nowUtc, CancellationToken ct)
    {
        for (var attempt = 0; attempt < 3; attempt++)
        {
            var candidate = $"GSV-{nowUtc:yyyyMMddHHmmss}-{Uuid7.NewGuid():N}"[..42].ToUpperInvariant();
            if (!await db.FrontOfficeGroupServiceOrders.AnyAsync(x => x.PropertyId == propertyId && x.ServiceCode == candidate, ct))
                return candidate;
        }

        throw new InvalidOperationException("GROUP_SERVICE_CODE_GENERATION_FAILED");
    }

    private void StageAudit(string action, GroupServiceOrder order, Guid propertyId, string? reason = null)
    {
        persistence.StageAudit(new FrontOfficeAuditIntent(
            action,
            nameof(GroupServiceOrder),
            order.GroupServiceOrderId.ToString("D"),
            "Succeeded",
            ReasonCode: reason,
            Metadata: new Dictionary<string, string>
            {
                ["PropertyId"] = propertyId.ToString("D"),
                ["GroupReservationId"] = order.GroupReservationId.ToString("D"),
                ["ServiceCode"] = order.ServiceCode,
                ["Status"] = order.Status.ToString()
            }));
    }

    private async Task CommitAsync(CancellationToken ct, string errorCode)
    {
        var result = await persistence.CommitAsync(ct);
        if (result.IsFailure)
            throw new InvalidOperationException(result.Error?.Code ?? errorCode);
    }

    private static GroupServiceOrderDto Map(GroupServiceOrder order)
    {
        var group = order.GroupReservation ?? throw new InvalidOperationException("GROUP_SERVICE_GROUP_NOT_LOADED");
        return new GroupServiceOrderDto(
            order.GroupServiceOrderId,
            order.PropertyId,
            order.GroupReservationId,
            order.ServiceCode,
            order.ServiceName,
            order.Category,
            order.SubCategory,
            order.Department,
            order.Provider,
            order.Location,
            order.ServiceDate,
            order.ServiceTime,
            order.DurationMinutes,
            order.Quantity,
            order.UnitPrice,
            order.DiscountRate,
            order.TaxRate,
            order.ServiceChargeRate,
            order.SubTotal,
            order.DiscountAmount,
            order.TaxAmount,
            order.ServiceChargeAmount,
            order.TotalAmount,
            order.PaidAmount,
            order.Balance,
            order.Status,
            order.ChargeTo,
            group.GroupCode,
            group.GroupName,
            order.GuestName,
            order.Remark,
            order.CreatedAtUtc,
            order.UpdatedAtUtc,
            order.RowVersion);
    }
}
