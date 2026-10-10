using System.Text.Json;
using ASMILE.BO.Application.FrontOffice.Foundation.Audit;
using ASMILE.BO.Application.FrontOffice.Foundation.Contexts;
using ASMILE.BO.Application.FrontOffice.Foundation.Persistence;
using ASMILE.BO.Application.FrontOffice.Group;
using ASMILE.BO.Application.FrontOffice.Settlement;
using ASMILE.BO.Core.FrontOffice.Folio;
using ASMILE.BO.Core.FrontOffice.Group;
using ASMILE.BO.Core.FrontOffice.Housekeeping;
using ASMILE.BO.Core.FrontOffice.Settlement;
using ASMILE.BO.Core.FrontOffice.Stay;
using ASMILE.BO.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ASMILE.BO.Infrastructure.FrontOffice.Group;

public sealed class EfGroupCheckOutCommandService(
    AppDbContext db,
    IFrontOfficePersistenceSession persistence,
    IFrontOfficeExecutionContextAccessor contextAccessor,
    IClock clock,
    IHotelInvoiceNumberGenerator invoiceNumberGenerator) : IGroupCheckOutCommandService
{
    public async Task<GroupCheckOutResult> CheckOutAsync(
        Guid groupId,
        IReadOnlyCollection<Guid> assignmentIds,
        DateTime checkOutAtUtc,
        CancellationToken ct)
        => await CheckOutCoreAsync(groupId, assignmentIds, checkOutAtUtc, issueInvoices: false, ct);

    public async Task<GroupCheckOutResult> CheckOutAndIssueInvoicesAsync(
        Guid groupId,
        IReadOnlyCollection<Guid> assignmentIds,
        DateTime checkOutAtUtc,
        CancellationToken ct)
        => await CheckOutCoreAsync(groupId, assignmentIds, checkOutAtUtc, issueInvoices: true, ct);

    private async Task<GroupCheckOutResult> CheckOutCoreAsync(
        Guid groupId,
        IReadOnlyCollection<Guid> assignmentIds,
        DateTime checkOutAtUtc,
        bool issueInvoices,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(assignmentIds);
        var context = contextAccessor.Current
            ?? throw new InvalidOperationException("FRONT_OFFICE_CONTEXT_REQUIRED");
        var propertyId = context.Property.PropertyId.Value;

        if (assignmentIds.Count == 0)
            throw new InvalidOperationException("GROUP_ROOM_ASSIGNMENTS_REQUIRED");
        if (checkOutAtUtc > clock.UtcNow.AddMinutes(5))
            throw new ArgumentException("GROUP_INVALID_CHECKOUT_TIME");

        var group = await db.FrontOfficeGroupReservations
            .Include(x => x.RoomingList)
            .SingleOrDefaultAsync(x => x.GroupReservationId == groupId && x.PropertyId == propertyId, ct)
            ?? throw new KeyNotFoundException("GROUP_NOT_FOUND");

        if (group.Status is not GroupReservationStatus.CheckedIn)
            throw new InvalidOperationException("GROUP_STATUS_NOT_CHECKOUTABLE");

        var requestedIds = assignmentIds.Distinct().ToHashSet();
        if (requestedIds.Count != assignmentIds.Count)
            throw new InvalidOperationException("GROUP_ROOM_ASSIGNMENT_DUPLICATE");

        var selected = group.RoomingList
            .Where(x => requestedIds.Contains(x.GroupRoomAssignmentId))
            .ToList();
        if (selected.Count != requestedIds.Count)
            throw new KeyNotFoundException("GROUP_ROOM_ASSIGNMENT_NOT_FOUND");
        if (selected.Any(x => x.IsCheckedOut))
            throw new InvalidOperationException("GROUP_ROOM_ALREADY_CHECKED_OUT");
        if (selected.Any(x => !x.IsCheckedIn))
            throw new InvalidOperationException("GROUP_ROOM_NOT_CHECKED_IN");

        var reservationIds = selected
            .Select(x => x.ReservationId)
            .ToList();
        if (reservationIds.Any(x => !x.HasValue || x.Value == Guid.Empty))
            throw new InvalidOperationException("GROUP_STAY_REQUIRED");

        var distinctReservationIds = reservationIds
            .Select(x => x!.Value)
            .Distinct()
            .ToHashSet();
        var stays = await db.HotelStays
            .Where(x => x.PropertyId == propertyId
                        && x.ReservationId.HasValue
                        && distinctReservationIds.Contains(x.ReservationId.Value))
            .ToListAsync(ct);
        if (stays.Count != distinctReservationIds.Count)
            throw new InvalidOperationException("GROUP_STAY_NOT_FOUND");

        var stayIds = stays.Select(x => x.HotelStayId).ToHashSet();
        var folios = await db.HotelFolios
            .Where(x => x.PropertyId == propertyId && stayIds.Contains(x.HotelStayId))
            .Select(x => new FolioSnapshot(x.HotelFolioId, x.HotelStayId, x.FolioNumber))
            .ToListAsync(ct);
        if (folios.Select(x => x.HotelStayId).Distinct().Count() != stayIds.Count)
            throw new InvalidOperationException("GROUP_FOLIO_REQUIRED");

        var folioIds = folios.Select(x => x.HotelFolioId).ToHashSet();
        var balancesByFolio = await db.HotelFolioEntries
            .Where(x => folioIds.Contains(x.HotelFolioId))
            .GroupBy(x => x.HotelFolioId)
            .Select(x => new { FolioId = x.Key, Balance = x.Sum(entry => entry.Amount) })
            .ToDictionaryAsync(x => x.FolioId, x => x.Balance, ct);
        var outstandingByStay = folios
            .GroupBy(x => x.HotelStayId)
            .ToDictionary(
                x => x.Key,
                x => x.Sum(folio => balancesByFolio.GetValueOrDefault(folio.HotelFolioId)) > 0m);

        var invoicesGenerated = 0;
        if (issueInvoices)
        {
            var existingInvoiceFolioIds = (await db.Set<HotelInvoice>()
                .Where(x => x.PropertyId == propertyId && folioIds.Contains(x.HotelFolioId))
                .Select(x => x.HotelFolioId)
                .ToListAsync(ct))
                .ToHashSet();
            var invoiceEntries = await db.HotelFolioEntries
                .Where(x => folioIds.Contains(x.HotelFolioId))
                .OrderBy(x => x.HotelFolioId)
                .ThenBy(x => x.CreatedAtUtc)
                .ToListAsync(ct);
            var entriesByFolio = invoiceEntries
                .GroupBy(x => x.HotelFolioId)
                .ToDictionary(x => x.Key, x => x.ToList());
            var staysById = stays.ToDictionary(x => x.HotelStayId);

            foreach (var folio in folios.Where(x => !existingInvoiceFolioIds.Contains(x.HotelFolioId)))
            {
                var entries = entriesByFolio.TryGetValue(folio.HotelFolioId, out var rows)
                    ? rows
                    : [];
                var subtotal = entries
                    .Where(x => IsInvoiceSubtotalEntry(x.EntryType))
                    .Sum(x => x.Amount);
                var tax = entries
                    .Where(x => x.EntryType == HotelFolioEntryType.Tax)
                    .Sum(x => x.Amount);
                var payments = Math.Abs(entries
                    .Where(x => x.EntryType == HotelFolioEntryType.Payment)
                    .Sum(x => x.Amount));
                var deposits = Math.Abs(entries
                    .Where(x => x.EntryType == HotelFolioEntryType.Deposit)
                    .Sum(x => x.Amount));
                var refunds = entries
                    .Where(x => x.EntryType == HotelFolioEntryType.Refund)
                    .Sum(x => x.Amount);
                var total = subtotal + tax;
                var paid = payments + deposits - refunds;
                var outstanding = entries.Sum(x => x.Amount);
                var stay = staysById[folio.HotelStayId];
                var invoiceNumber = await invoiceNumberGenerator.GenerateAsync(
                    propertyId,
                    context.BusinessDate.BusinessDate,
                    ct);
                var invoiceData = JsonSerializer.Serialize(new
                {
                    source = "GroupCheckout",
                    folio.FolioNumber,
                    stay.StayNumber,
                    entries = entries.Select(x => new
                    {
                        x.HotelFolioEntryId,
                        type = x.EntryType.ToString(),
                        x.Description,
                        x.Amount,
                        x.CreatedAtUtc
                    })
                });

                db.Set<HotelInvoice>().Add(HotelInvoice.Generate(
                    propertyId,
                    folio.HotelFolioId,
                    folio.HotelStayId,
                    invoiceNumber,
                    subtotal,
                    tax,
                    total,
                    paid,
                    outstanding,
                    stay.GuestNameSnapshot,
                    stay.RoomNumberSnapshot,
                    $"{stay.ScheduledArrivalDate:yyyy-MM-dd} - {stay.ScheduledDepartureDate:yyyy-MM-dd}",
                    invoiceData,
                    checkOutAtUtc,
                    context.Actor.ActorId));
                invoicesGenerated++;
            }
        }

        var housekeepingTasksCreated = 0;
        var selectedRoomIds = selected
            .Select(x => x.HotelRoomId)
            .Distinct()
            .ToHashSet();
        if (selectedRoomIds.Count > 0)
        {
            var activeHousekeepingRoomIds = (await db.HousekeepingTasks
                    .Where(x => x.PropertyId == propertyId
                        && selectedRoomIds.Contains(x.HotelRoomId)
                        && x.Status != HousekeepingTaskStatus.Completed
                        && x.Status != HousekeepingTaskStatus.Cancelled)
                    .Select(x => x.HotelRoomId)
                    .ToListAsync(ct))
                .ToHashSet();
            var departureTasks = selectedRoomIds
                .Where(roomId => !activeHousekeepingRoomIds.Contains(roomId))
                .Select(roomId => HousekeepingTask.Create(
                    propertyId,
                    roomId,
                    $"HK-GRP-{Guid.NewGuid():N}"[..34],
                    HousekeepingTaskType.DepartureClean,
                    RoomPriority.Normal,
                    checkOutAtUtc,
                    context.Actor.ActorId))
                .ToList();
            if (departureTasks.Count > 0)
            {
                db.HousekeepingTasks.AddRange(departureTasks);
                housekeepingTasksCreated = departureTasks.Count;
            }
        }

        foreach (var stay in stays)
        {
            stay.MarkCheckedOut(
                checkOutAtUtc,
                context.Actor.ActorId,
                hasOutstandingBalance: outstandingByStay.GetValueOrDefault(stay.HotelStayId));
            db.HotelStayHistories.Add(HotelStayHistory.RecordCheckedOut(
                stay,
                checkOutAtUtc,
                context.Actor.ActorId?.Value,
                context.Correlation.CorrelationId.Value));
            foreach (var domainEvent in stay.DomainEvents)
                persistence.StageDomainEvent(domainEvent);
        }

        foreach (var assignment in selected)
            assignment.MarkCheckedOut();

        var remaining = group.RoomingList.Any(x => !x.IsCheckedOut);
        if (!remaining)
            group.MarkCheckedOut(checkOutAtUtc);

        persistence.StageAudit(new FrontOfficeAuditIntent(
            "GroupCheckedOut",
            nameof(GroupReservation),
            group.GroupReservationId.ToString("D"),
            "Succeeded",
            Metadata: new Dictionary<string, string>
            {
                ["PropertyId"] = propertyId.ToString("D"),
                ["GroupCode"] = group.GroupCode,
                ["AssignmentCount"] = selected.Count.ToString(),
                ["GuestCount"] = selected.Sum(x => x.Adults + x.Children).ToString(),
                ["StayCount"] = stays.Count.ToString(),
                ["OutstandingStayCount"] = outstandingByStay.Count(x => x.Value).ToString(),
                ["InvoicesGenerated"] = invoicesGenerated.ToString(),
                ["HousekeepingTasksCreated"] = housekeepingTasksCreated.ToString(),
                ["GroupClosed"] = (!remaining).ToString()
            }));

        var saved = await persistence.CommitAsync(ct);
        if (saved.IsFailure)
            throw new InvalidOperationException(saved.Error?.Code ?? "GROUP_CHECKOUT_FAILED");

        return new GroupCheckOutResult(
            group.GroupReservationId,
            group.GroupCode,
            group.Status,
            selected.Count,
            selected.Sum(x => x.Adults + x.Children),
            checkOutAtUtc,
            invoicesGenerated,
            housekeepingTasksCreated);
    }

    private static bool IsInvoiceSubtotalEntry(HotelFolioEntryType entryType) => entryType is
        HotelFolioEntryType.RoomCharge or HotelFolioEntryType.Restaurant or HotelFolioEntryType.Laundry or
        HotelFolioEntryType.MiniBar or HotelFolioEntryType.Spa or HotelFolioEntryType.Telephone or
        HotelFolioEntryType.OtherCharge or HotelFolioEntryType.ServiceCharge or HotelFolioEntryType.Discount or
        HotelFolioEntryType.Package or HotelFolioEntryType.Adjustment or HotelFolioEntryType.Correction or
        HotelFolioEntryType.Transfer or HotelFolioEntryType.ManualCharge or HotelFolioEntryType.ManualCredit or
        HotelFolioEntryType.OpeningBalance;

    private sealed record FolioSnapshot(Guid HotelFolioId, Guid HotelStayId, string FolioNumber);
}
