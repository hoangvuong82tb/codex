using System.Globalization;
using System.Data;
using ASMILE.BO.Application.FrontOffice.Availability;
using ASMILE.BO.Application.FrontOffice.Foundation.Audit;
using ASMILE.BO.Application.FrontOffice.Foundation.Contexts;
using ASMILE.BO.Application.FrontOffice.Foundation.Persistence;
using ASMILE.BO.Application.FrontOffice.Group;
using ASMILE.BO.Core.FrontOffice.Availability;
using ASMILE.BO.Core.FrontOffice.Group;
using ASMILE.BO.Core.FrontOffice.Foundation.Identifiers;
using ASMILE.BO.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ASMILE.BO.Infrastructure.FrontOffice.Group;

public sealed class EfGroupReservationCommandService(
    AppDbContext db,
    IFrontOfficePersistenceSession persistence,
    IFrontOfficeExecutionContextAccessor contextAccessor,
    IClock clock,
    IAvailabilityEngine availabilityEngine) : IGroupReservationCommandService
{
    public async Task<CreatedGroupDto> CreateAsync(CreateGroupRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        var context = contextAccessor.Current
            ?? throw new InvalidOperationException("FRONT_OFFICE_CONTEXT_REQUIRED");
        var propertyId = context.Property.PropertyId.Value;
        if (propertyId != request.PropertyId)
            throw new InvalidOperationException("FRONT_OFFICE_PROPERTY_MISMATCH");

        var code = await ResolveCodeAsync(request.GroupCode, propertyId, ct);
        var now = clock.UtcNow;
        var group = GroupReservation.Create(
            propertyId,
            code,
            request.GroupName,
            request.ArrivalDate,
            request.DepartureDate,
            request.ExpectedRooms,
            request.TotalAdults,
            request.TotalChildren,
            now,
            context.Actor.ActorId);
        group.UpdateDetails(
            request.CompanyName,
            request.AgencyName,
            request.ContactPerson,
            request.ContactPhone,
            request.ContactEmail,
            request.Source,
            request.SalesManager,
            request.MarketSegment,
            request.IsVip,
            request.Routing,
            request.SpecialRequests,
            request.Notes,
            now,
            context.Actor.ActorId);

        db.FrontOfficeGroupReservations.Add(group);
        await AddServiceDraftsAsync(group, request.ServiceDrafts, now, context.Actor.ActorId, ct);
        await AddRoomBlockDraftsAsync(group, request.RoomBlockDrafts, now, context.Actor.ActorId, ct);
        persistence.StageAudit(new FrontOfficeAuditIntent(
            "GroupCreated",
            nameof(GroupReservation),
            group.GroupReservationId.ToString("D"),
            "Succeeded",
            Metadata: new Dictionary<string, string>
            {
                ["PropertyId"] = propertyId.ToString("D"),
                ["GroupCode"] = group.GroupCode,
                ["GroupName"] = group.GroupName,
                ["Status"] = group.Status.ToString()
            }));

        var saved = await persistence.CommitAsync(ct);
        if (saved.IsFailure)
            throw new InvalidOperationException(saved.Error?.Code ?? "GROUP_CREATE_FAILED");

        return new CreatedGroupDto(
            group.GroupReservationId,
            group.PropertyId,
            group.GroupCode,
            group.GroupName,
            group.Status,
            group.CreatedAtUtc);
    }

    public async Task<ConfiguredGroupBookingDto> ConfigureBookingAsync(
        ConfigureGroupBookingRequest request,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.RoomBlockDrafts is null || request.RoomBlockDrafts.Count == 0)
            throw new ArgumentException("GROUP_ROOM_BLOCK_REQUIRED");

        var context = contextAccessor.Current
            ?? throw new InvalidOperationException("FRONT_OFFICE_CONTEXT_REQUIRED");
        var propertyId = context.Property.PropertyId.Value;
        var group = await db.FrontOfficeGroupReservations
            .Include(x => x.RoomBlocks)
            .SingleOrDefaultAsync(
                x => x.GroupReservationId == request.GroupReservationId && x.PropertyId == propertyId,
                ct)
            ?? throw new KeyNotFoundException("GROUP_NOT_FOUND");

        if (group.Status is not (GroupReservationStatus.Draft or GroupReservationStatus.Tentative))
            throw new InvalidOperationException("GROUP_BOOKING_STATUS_NOT_CONFIGURABLE");
        if (group.RoomBlocks.Count != 0)
            throw new InvalidOperationException("GROUP_ROOM_BLOCKS_ALREADY_CONFIGURED");

        var now = clock.UtcNow;
        var depositPolicy = request.DepositPolicy;
        var schedules = (depositPolicy?.Schedules ?? Array.Empty<GroupDepositScheduleDraft>())
            .Select(x => GroupDepositSchedule.Create(
                propertyId,
                group.GroupReservationId,
                x.DueDate,
                x.Percent,
                now,
                context.Actor.ActorId))
            .ToArray();
        group.ConfigureDepositPolicy(
            depositPolicy?.DepositRequired ?? false,
            depositPolicy?.GuaranteeType,
            depositPolicy?.CutoffDate,
            depositPolicy?.ReleaseDate,
            schedules,
            now,
            context.Actor.ActorId);
        foreach (var schedule in schedules)
            db.FrontOfficeGroupDepositSchedules.Add(schedule);

        await using var confirmationTransaction = request.Confirm && db.Database.IsRelational()
            ? await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct)
            : null;
        var blocks = await AddRoomBlockDraftsAsync(group, request.RoomBlockDrafts, now, context.Actor.ActorId, ct);

        if (request.Confirm)
        {
            await EnsureRoomBlocksAvailableAsync(group, blocks, context.BusinessDate.BusinessDate, ct);
            group.Confirm(now);
            foreach (var block in blocks)
            {
                block.Confirm(now, context.Actor.ActorId);
                persistence.StageAudit(new FrontOfficeAuditIntent(
                    "GroupRoomBlockConfirmed",
                    nameof(GroupRoomBlock),
                    block.GroupRoomBlockId.ToString("D"),
                    "Succeeded",
                    Metadata: new Dictionary<string, string>
                    {
                        ["PropertyId"] = propertyId.ToString("D"),
                        ["GroupReservationId"] = group.GroupReservationId.ToString("D"),
                        ["ConfirmedWithBookingConfiguration"] = "true"
                    }));
            }
        }

        persistence.StageAudit(new FrontOfficeAuditIntent(
            request.Confirm ? "GroupBookingConfirmed" : "GroupBookingSaved",
            nameof(GroupReservation),
            group.GroupReservationId.ToString("D"),
            "Succeeded",
            Metadata: new Dictionary<string, string>
            {
                ["PropertyId"] = propertyId.ToString("D"),
                ["GroupCode"] = group.GroupCode,
                ["RoomBlockCount"] = blocks.Count.ToString(CultureInfo.InvariantCulture),
                ["Status"] = group.Status.ToString()
            }));

        var saved = await persistence.CommitAsync(ct);
        if (saved.IsFailure)
            throw new InvalidOperationException(saved.Error?.Code ?? "GROUP_BOOKING_CONFIGURATION_FAILED");
        if (confirmationTransaction is not null)
            await confirmationTransaction.CommitAsync(ct);

        return new ConfiguredGroupBookingDto(
            group.GroupReservationId,
            group.PropertyId,
            group.GroupCode,
            group.Status,
            blocks.Count,
            now);
    }

    private async Task<List<GroupRoomBlock>> AddRoomBlockDraftsAsync(
        GroupReservation group,
        IReadOnlyCollection<CreateGroupRoomBlockDraft>? drafts,
        DateTime nowUtc,
        ActorId? actorId,
        CancellationToken ct)
    {
        if (drafts is null || drafts.Count == 0) return [];
        if (drafts.Sum(x => x.Rooms) != group.ExpectedRooms)
            throw new ArgumentException("GROUP_ROOM_BLOCK_TOTAL_MISMATCH");

        var blocks = new List<GroupRoomBlock>(drafts.Count);
        foreach (var draft in drafts)
        {
            if (draft.ArrivalDate != group.ArrivalDate || draft.DepartureDate != group.DepartureDate)
                throw new ArgumentException("GROUP_ROOM_BLOCK_DATES_MUST_MATCH_GROUP");

            var roomType = await db.HotelRoomTypes
                .SingleOrDefaultAsync(x => x.HotelRoomTypeId == draft.RoomTypeId && x.PropertyId == group.PropertyId, ct)
                ?? throw new KeyNotFoundException("HOTEL_ROOM_TYPE_NOT_FOUND");
            if (!roomType.IsActive)
                throw new InvalidOperationException("HOTEL_ROOM_TYPE_INACTIVE");

            var block = GroupRoomBlock.Create(
                group.PropertyId,
                group.GroupReservationId,
                roomType.HotelRoomTypeId,
                draft.Rooms,
                draft.ArrivalDate,
                draft.DepartureDate,
                draft.CutoffDate,
                draft.NegotiatedRate,
                nowUtc,
                actorId);
            db.FrontOfficeGroupRoomBlocks.Add(block);
            blocks.Add(block);
            persistence.StageAudit(new FrontOfficeAuditIntent(
                "GroupRoomBlockCreated",
                nameof(GroupRoomBlock),
                block.GroupRoomBlockId.ToString("D"),
                "Succeeded",
                Metadata: new Dictionary<string, string>
                {
                    ["PropertyId"] = group.PropertyId.ToString("D"),
                    ["GroupReservationId"] = group.GroupReservationId.ToString("D"),
                    ["HotelRoomTypeId"] = roomType.HotelRoomTypeId.ToString("D"),
                    ["Rooms"] = block.BlockedRooms.ToString(CultureInfo.InvariantCulture),
                    ["CreatedWithGroup"] = "true"
                }));
        }

        return blocks;
    }

    private async Task AddServiceDraftsAsync(
        GroupReservation group,
        IReadOnlyCollection<CreateGroupServiceDraft>? drafts,
        DateTime nowUtc,
        ActorId? actorId,
        CancellationToken ct)
    {
        if (drafts is null || drafts.Count == 0) return;

        foreach (var draft in drafts)
        {
            var serviceCode = await ResolveServiceCodeAsync(group.PropertyId, nowUtc, ct);
            var order = GroupServiceOrder.Create(
                group.PropertyId,
                group.GroupReservationId,
                serviceCode,
                draft.ServiceName,
                draft.Category,
                draft.ServiceDate,
                draft.ServiceTime,
                draft.DurationMinutes,
                draft.Quantity,
                draft.UnitPrice,
                draft.DiscountRate,
                draft.TaxRate,
                draft.ServiceChargeRate,
                draft.ChargeTo,
                nowUtc,
                actorId,
                draft.SubCategory,
                draft.Department,
                draft.Provider,
                draft.Location,
                draft.GuestName,
                draft.Remark);
            db.FrontOfficeGroupServiceOrders.Add(order);
            persistence.StageAudit(new FrontOfficeAuditIntent(
                "GroupServiceCreated",
                nameof(GroupServiceOrder),
                order.GroupServiceOrderId.ToString("D"),
                "Succeeded",
                Metadata: new Dictionary<string, string>
                {
                    ["PropertyId"] = group.PropertyId.ToString("D"),
                    ["GroupReservationId"] = group.GroupReservationId.ToString("D"),
                    ["ServiceCode"] = order.ServiceCode,
                    ["Status"] = order.Status.ToString(),
                    ["CreatedWithGroup"] = "true"
                }));
        }
    }

    public Task<GroupStatusChangeDto> ConfirmAsync(Guid groupId, CancellationToken ct)
        => ChangeStatusAsync(groupId, GroupReservationStatus.Confirmed, null, ct);

    public Task<GroupStatusChangeDto> CancelAsync(Guid groupId, string reason, CancellationToken ct)
        => ChangeStatusAsync(groupId, GroupReservationStatus.Cancelled, reason, ct);

    public Task<GroupStatusChangeDto> CloseAsync(Guid groupId, CancellationToken ct)
        => ChangeStatusAsync(groupId, GroupReservationStatus.Closed, null, ct);

    public async Task DeleteDraftAsync(Guid groupId, CancellationToken ct)
    {
        var context = contextAccessor.Current
            ?? throw new InvalidOperationException("FRONT_OFFICE_CONTEXT_REQUIRED");
        var propertyId = context.Property.PropertyId.Value;
        var group = await db.FrontOfficeGroupReservations
            .SingleOrDefaultAsync(x => x.GroupReservationId == groupId && x.PropertyId == propertyId, ct)
            ?? throw new KeyNotFoundException("GROUP_NOT_FOUND");

        if (group.Status != GroupReservationStatus.Draft)
            throw new InvalidOperationException("GROUP_DRAFT_DELETE_STATUS_NOT_ALLOWED");

        if (await db.FrontOfficeGroupServiceOrders.AnyAsync(x => x.GroupReservationId == groupId, ct))
            throw new InvalidOperationException("GROUP_DRAFT_HAS_SERVICE_ORDERS");

        db.FrontOfficeGroupReservations.Remove(group);
        persistence.StageAudit(new FrontOfficeAuditIntent(
            "GroupDraftDeleted",
            nameof(GroupReservation),
            group.GroupReservationId.ToString("D"),
            "Succeeded",
            Metadata: new Dictionary<string, string>
            {
                ["PropertyId"] = propertyId.ToString("D"),
                ["GroupCode"] = group.GroupCode,
                ["Status"] = group.Status.ToString()
            }));

        var saved = await persistence.CommitAsync(ct);
        if (saved.IsFailure)
            throw new InvalidOperationException(saved.Error?.Code ?? "GROUP_DRAFT_DELETE_FAILED");
    }

    private async Task<GroupStatusChangeDto> ChangeStatusAsync(
        Guid groupId,
        GroupReservationStatus target,
        string? reason,
        CancellationToken ct)
    {
        var context = contextAccessor.Current
            ?? throw new InvalidOperationException("FRONT_OFFICE_CONTEXT_REQUIRED");
        var propertyId = context.Property.PropertyId.Value;
        var group = await db.FrontOfficeGroupReservations
            .Include(x => x.RoomBlocks)
            .SingleOrDefaultAsync(x => x.GroupReservationId == groupId && x.PropertyId == propertyId, ct)
            ?? throw new KeyNotFoundException("GROUP_NOT_FOUND");
        var now = clock.UtcNow;
        await using var confirmationTransaction = target == GroupReservationStatus.Confirmed && db.Database.IsRelational()
            ? await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct)
            : null;
        if (target == GroupReservationStatus.Confirmed)
        {
            await EnsureRoomBlocksAvailableAsync(group, group.RoomBlocks, context.BusinessDate.BusinessDate, ct);
            group.Confirm(now);
            foreach (var block in group.RoomBlocks)
            {
                block.Confirm(now, context.Actor.ActorId);
                persistence.StageAudit(new FrontOfficeAuditIntent(
                    "GroupRoomBlockConfirmed", nameof(GroupRoomBlock), block.GroupRoomBlockId.ToString("D"), "Succeeded",
                    Metadata: new Dictionary<string, string>
                    {
                        ["PropertyId"] = propertyId.ToString("D"),
                        ["GroupReservationId"] = group.GroupReservationId.ToString("D")
                    }));
            }
        }
        else if (target == GroupReservationStatus.Cancelled)
        {
            group.Cancel(reason ?? string.Empty, now);
            foreach (var block in group.RoomBlocks)
            {
                var releasable = block.BlockedRooms - block.PickedUpRooms;
                if (releasable <= 0) continue;
                block.Release(releasable, now, context.Actor.ActorId);
                persistence.StageAudit(new FrontOfficeAuditIntent(
                    "GroupRoomBlockReleased", nameof(GroupRoomBlock), block.GroupRoomBlockId.ToString("D"), "Succeeded",
                    ReasonCode: reason,
                    Metadata: new Dictionary<string, string>
                    {
                        ["PropertyId"] = propertyId.ToString("D"),
                        ["GroupReservationId"] = group.GroupReservationId.ToString("D"),
                        ["ReleasedWithGroupCancellation"] = "true"
                    }));
            }
        }
        else if (target == GroupReservationStatus.Closed)
        {
            if (group.Status is not (GroupReservationStatus.CheckedOut or GroupReservationStatus.Cancelled))
                throw new InvalidOperationException("GROUP_CLOSE_STATUS_NOT_ALLOWED");
            group.Close(now);
        }
        else
            throw new ArgumentOutOfRangeException(nameof(target));

        persistence.StageAudit(new FrontOfficeAuditIntent(
            target switch
            {
                GroupReservationStatus.Confirmed => "GroupConfirmed",
                GroupReservationStatus.Cancelled => "GroupCancelled",
                GroupReservationStatus.Closed => "GroupClosed",
                _ => "GroupStatusChanged"
            },
            nameof(GroupReservation),
            group.GroupReservationId.ToString("D"),
            "Succeeded",
            ReasonCode: reason,
            Metadata: new Dictionary<string, string>
            {
                ["PropertyId"] = propertyId.ToString("D"),
                ["GroupCode"] = group.GroupCode,
                ["Status"] = group.Status.ToString()
            }));
        var saved = await persistence.CommitAsync(ct);
        if (saved.IsFailure)
            throw new InvalidOperationException(saved.Error?.Code ?? "GROUP_STATUS_CHANGE_FAILED");

        return new GroupStatusChangeDto(
            group.GroupReservationId,
            group.PropertyId,
            group.GroupCode,
            group.Status,
            group.UpdatedAtUtc);
    }

    private async Task EnsureRoomBlocksAvailableAsync(
        GroupReservation group,
        IEnumerable<GroupRoomBlock> blocks,
        DateOnly businessDate,
        CancellationToken ct)
    {
        foreach (var block in blocks)
        {
            var roomsToProtect = block.BlockedRooms - block.PickedUpRooms;
            if (roomsToProtect <= 0) continue;

            var result = await availabilityEngine.CheckAvailabilityAsync(
                new AvailabilityRequest(
                    group.PropertyId,
                    businessDate,
                    block.ArrivalDate,
                    block.DepartureDate,
                    block.HotelRoomTypeId,
                    group.TotalAdults,
                    group.TotalChildren,
                    roomsToProtect,
                    group.GroupReservationId),
                ct);
            if (!result.IsAvailable)
                throw new InvalidOperationException(
                    $"GROUP_ROOM_BLOCK_NOT_AVAILABLE:{block.HotelRoomTypeId}:{result.UnavailabilityReason ?? "RESERVATION_NO_AVAILABILITY"}");
        }
    }

    private async Task<string> ResolveCodeAsync(string? requestedCode, Guid propertyId, CancellationToken ct)
    {
        var normalized = requestedCode?.Trim();
        if (!string.IsNullOrWhiteSpace(normalized))
        {
            if (normalized.Length > 40)
                throw new ArgumentException("GROUP_CODE_TOO_LONG");
            if (await db.FrontOfficeGroupReservations.AnyAsync(x => x.PropertyId == propertyId && x.GroupCode == normalized, ct))
                throw new InvalidOperationException("GROUP_CODE_ALREADY_EXISTS");
            return normalized;
        }

        for (var attempt = 0; attempt < 3; attempt++)
        {
            var candidate = $"GRP-{clock.UtcNow:yyyyMMdd}-{Uuid7.NewGuid():N}"[..23].ToUpperInvariant();
            if (!await db.FrontOfficeGroupReservations.AnyAsync(x => x.PropertyId == propertyId && x.GroupCode == candidate, ct))
                return candidate;
        }

        throw new InvalidOperationException("GROUP_CODE_GENERATION_FAILED");
    }

    private async Task<string> ResolveServiceCodeAsync(Guid propertyId, DateTime nowUtc, CancellationToken ct)
    {
        for (var attempt = 0; attempt < 3; attempt++)
        {
            var candidate = $"GSV-{nowUtc:yyyyMMddHHmmss}-{Uuid7.NewGuid():N}"[..42].ToUpperInvariant();
            if (!await db.FrontOfficeGroupServiceOrders.AnyAsync(x => x.PropertyId == propertyId && x.ServiceCode == candidate, ct))
                return candidate;
        }

        throw new InvalidOperationException("GROUP_SERVICE_CODE_GENERATION_FAILED");
    }
}
