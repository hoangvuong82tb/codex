using ASMILE.BO.Application.FrontOffice.Group;
using ASMILE.BO.Application.FrontOffice.Foundation.Contexts;
using ASMILE.BO.Core.FrontOffice.Folio;
using ASMILE.BO.Core.FrontOffice.Group;
using ASMILE.BO.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ASMILE.BO.Infrastructure.FrontOffice.Group;

/// <summary>
/// Group billing read model and routing command backed by the persisted group
/// reservation, group deposit, service-order and folio tables.
/// </summary>
public sealed class EfGroupBillingService(
    AppDbContext db,
    IFrontOfficeExecutionContextAccessor contextAccessor) : IGroupBillingService
{
    public async Task<GroupBillingSetup> ConfigureBillingAsync(GroupBillingSetup setup, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var propertyId = RequirePropertyId();
        var group = await db.FrontOfficeGroupReservations
            .SingleOrDefaultAsync(x => x.GroupReservationId == setup.GroupId && x.PropertyId == propertyId, ct);
        if (group is null)
            throw new KeyNotFoundException($"Group '{setup.GroupId}' was not found.");

        group.ConfigureBillingRouting(
            setup.UseMasterFolio,
            setup.DefaultRouting,
            setup.AllowIndividualFolios,
            setup.SplitPercentage,
            setup.MasterFolioId,
            DateTime.UtcNow,
            actorId: null);
        await db.SaveChangesAsync(ct);
        return setup with { DepositRequired = group.DepositRequired };
    }

    public async Task<BillingSummaryDto> GetBillingSummaryAsync(Guid groupId, CancellationToken ct)
    {
        var snapshot = await LoadGroupAsync(groupId, ct);
        var facts = await LoadBillingFactsAsync(snapshot, ct);
        var charges = facts.TotalCharges;
        var payments = facts.TotalPayments;
        var groupDeposits = facts.HasGroupDepositLedger
            ? facts.GroupDepositBalance
            : Math.Max(snapshot.TotalDeposit, 0m);
        var deposits = groupDeposits + facts.FolioDeposits;
        var outstanding = Math.Max(charges - payments - deposits, 0m);
        return new BillingSummaryDto(
            snapshot.GroupReservationId,
            snapshot.GroupName,
            charges,
            payments,
            deposits,
            outstanding,
            0,
            facts.FolioCount,
            outstanding <= 0.01m);
    }

    public async Task<GroupRevenueDashboardDto> GetRevenueDashboardAsync(Guid groupId, CancellationToken ct)
    {
        var snapshot = await LoadGroupAsync(groupId, ct);
        var facts = await LoadBillingFactsAsync(snapshot, ct);
        var roomRevenue = facts.RoomRevenue;
        var fbRevenue = facts.FoodAndBeverageRevenue;
        var otherRevenue = facts.OtherRevenue;
        var total = roomRevenue + fbRevenue + otherRevenue;
        if (total <= 0m && snapshot.TotalRevenue > 0m)
        {
            // Preserve a previously persisted group snapshot when no child
            // ledger/service rows exist yet; this is still database data,
            // never a fabricated fixture.
            total = snapshot.TotalRevenue;
            otherRevenue = total;
        }

        var roomCount = Math.Max(snapshot.ConfirmedRooms > 0 ? snapshot.ConfirmedRooms : snapshot.ExpectedRooms, 1);
        var nights = Math.Max(snapshot.DepartureDate.DayNumber - snapshot.ArrivalDate.DayNumber, 1);
        return new GroupRevenueDashboardDto(
            snapshot.GroupReservationId,
            snapshot.GroupName,
            roomRevenue,
            fbRevenue,
            otherRevenue,
            total,
            Math.Round(total / roomCount, 2),
            Math.Round(total / (roomCount * nights), 2),
            0m);
    }

    private async Task<GroupSnapshot> LoadGroupAsync(Guid groupId, CancellationToken ct)
    {
        var propertyId = RequirePropertyId();
        return await db.FrontOfficeGroupReservations
            .AsNoTracking()
            .Where(x => x.GroupReservationId == groupId && x.PropertyId == propertyId)
            .Select(x => new GroupSnapshot(
                x.GroupReservationId,
                x.GroupName,
                x.PropertyId,
                x.ArrivalDate,
                x.DepartureDate,
                x.ExpectedRooms,
                x.ConfirmedRooms,
                x.TotalDeposit,
                x.TotalRevenue,
                x.OutstandingBalance))
            .SingleOrDefaultAsync(ct)
            ?? throw new KeyNotFoundException($"Group '{groupId}' was not found.");
    }

    private async Task<BillingFacts> LoadBillingFactsAsync(GroupSnapshot group, CancellationToken ct)
    {
        var serviceRows = await db.FrontOfficeGroupServiceOrders
            .AsNoTracking()
            .Where(x => x.PropertyId == group.PropertyId && x.GroupReservationId == group.GroupReservationId && x.Status != GroupServiceOrderStatus.Cancelled && x.Status != GroupServiceOrderStatus.PostedToFolio)
            .Select(x => new ServiceFact(x.Category, x.Quantity, x.UnitPrice, x.DiscountRate, x.TaxRate, x.ServiceChargeRate, x.PaidAmount))
            .ToListAsync(ct);

        var groupDepositRows = await db.FrontOfficeGroupDeposits
            .AsNoTracking()
            .Where(x => x.PropertyId == group.PropertyId && x.GroupReservationId == group.GroupReservationId)
            .Select(x => new { x.Amount, x.Status })
            .ToListAsync(ct);

        var reservationIds = await db.FrontOfficeGroupRoomAssignments
            .AsNoTracking()
            .Where(x => x.PropertyId == group.PropertyId && x.GroupReservationId == group.GroupReservationId && x.ReservationId.HasValue)
            .Select(x => x.ReservationId!.Value)
            .ToArrayAsync(ct);

        var folioRows = reservationIds.Length == 0
            ? new List<FolioFact>()
            : await db.HotelFolioEntries
                .AsNoTracking()
                .Join(db.HotelFolios.AsNoTracking(), entry => entry.HotelFolioId, folio => folio.HotelFolioId, (entry, folio) => new { entry, folio })
                .Join(db.HotelStays.AsNoTracking(), row => row.folio.HotelStayId, stay => stay.HotelStayId, (row, stay) => new { row.entry, row.folio, stay })
                .Where(x => x.entry.PropertyId == group.PropertyId && x.stay.ReservationId.HasValue && reservationIds.Contains(x.stay.ReservationId.Value) && !x.entry.ReversedByEntryId.HasValue)
                .Select(x => new FolioFact(x.folio.HotelFolioId, x.entry.EntryType, x.entry.Amount))
                .ToListAsync(ct);

        var serviceCharges = serviceRows.Sum(x => x.Amount);
        var servicePayments = serviceRows.Sum(x => Math.Max(x.PaidAmount, 0m));
        // Folio entries are signed. Only Payment is a payment and only Deposit
        // is a deposit; a generic negative entry can be a discount/manual credit
        // and must remain part of the net charge ledger. Treating every credit
        // as payment double-counts deposits and hides real adjustments.
        var folioCharges = folioRows
            .Where(x => x.EntryType != HotelFolioEntryType.Payment
                && x.EntryType != HotelFolioEntryType.Deposit
                && x.EntryType != HotelFolioEntryType.Refund
                && x.EntryType != HotelFolioEntryType.ClosingBalance)
            .Sum(x => x.Amount);
        var folioPayments = folioRows
            .Where(x => x.EntryType == HotelFolioEntryType.Payment && x.Amount < 0m)
            .Sum(x => -x.Amount);
        var folioRefunds = folioRows
            .Where(x => x.EntryType == HotelFolioEntryType.Refund && x.Amount > 0m)
            .Sum(x => x.Amount);
        var folioDeposits = folioRows.Where(x => x.EntryType == HotelFolioEntryType.Deposit && x.Amount < 0m).Sum(x => -x.Amount);
        var roomRevenue = folioRows.Where(x => x.EntryType == HotelFolioEntryType.RoomCharge && x.Amount > 0m).Sum(x => x.Amount);
        var foodAndBeverageRevenue = serviceRows.Where(x => IsFoodAndBeverage(x.Category)).Sum(x => x.Amount)
            + folioRows.Where(x => x.EntryType == HotelFolioEntryType.Restaurant && x.Amount > 0m).Sum(x => x.Amount);
        var otherRevenue = serviceCharges - foodAndBeverageRevenue
            + folioRows.Where(x => x.Amount > 0m && x.EntryType != HotelFolioEntryType.RoomCharge && x.EntryType != HotelFolioEntryType.Restaurant && x.EntryType != HotelFolioEntryType.Refund && x.EntryType != HotelFolioEntryType.ClosingBalance).Sum(x => x.Amount);

        return new BillingFacts(
            serviceCharges + folioCharges,
            servicePayments + folioPayments - folioRefunds,
            roomRevenue,
            foodAndBeverageRevenue,
            Math.Max(otherRevenue, 0m),
            folioDeposits,
            folioRows.Select(x => x.FolioId).Distinct().Count(),
            groupDepositRows.Count > 0,
            groupDepositRows.Where(x => x.Status == GroupDepositStatus.Posted).Sum(x => x.Amount));
    }

    private Guid RequirePropertyId()
        => contextAccessor.Current?.Property.PropertyId.Value
            ?? throw new InvalidOperationException("FRONT_OFFICE_CONTEXT_REQUIRED");

    private static bool IsFoodAndBeverage(string category)
        => category.Contains("f&b", StringComparison.OrdinalIgnoreCase)
            || category.Contains("food", StringComparison.OrdinalIgnoreCase)
            || category.Contains("beverage", StringComparison.OrdinalIgnoreCase)
            || category.Contains("restaurant", StringComparison.OrdinalIgnoreCase)
            || category.Contains("breakfast", StringComparison.OrdinalIgnoreCase)
            || category.Contains("cafe", StringComparison.OrdinalIgnoreCase);

    private sealed record GroupSnapshot(
        Guid GroupReservationId,
        string GroupName,
        Guid PropertyId,
        DateOnly ArrivalDate,
        DateOnly DepartureDate,
        int ExpectedRooms,
        int ConfirmedRooms,
        decimal TotalDeposit,
        decimal TotalRevenue,
        decimal OutstandingBalance);

    private sealed record ServiceFact(
        string Category,
        int Quantity,
        decimal UnitPrice,
        decimal DiscountRate,
        decimal TaxRate,
        decimal ServiceChargeRate,
        decimal PaidAmount)
    {
        public decimal Amount
        {
            get
            {
                var subtotal = Quantity * UnitPrice;
                var discount = subtotal * DiscountRate;
                return subtotal - discount + (subtotal - discount) * TaxRate + (subtotal - discount) * ServiceChargeRate;
            }
        }
    }
    private sealed record FolioFact(Guid FolioId, HotelFolioEntryType EntryType, decimal Amount);
    private sealed record BillingFacts(
        decimal TotalCharges,
        decimal TotalPayments,
        decimal RoomRevenue,
        decimal FoodAndBeverageRevenue,
        decimal OtherRevenue,
        decimal FolioDeposits,
        int FolioCount,
        bool HasGroupDepositLedger,
        decimal GroupDepositBalance);
}
