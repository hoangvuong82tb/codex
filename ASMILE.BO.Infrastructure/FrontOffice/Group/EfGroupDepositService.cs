using ASMILE.BO.Application.FrontOffice.Foundation.Audit;
using ASMILE.BO.Application.FrontOffice.Foundation.Contexts;
using ASMILE.BO.Application.FrontOffice.Foundation.Persistence;
using ASMILE.BO.Application.FrontOffice.Group;
using ASMILE.BO.Core.FrontOffice.Group;
using ASMILE.BO.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ASMILE.BO.Infrastructure.FrontOffice.Group;

public sealed class EfGroupDepositService(
    AppDbContext db,
    IFrontOfficePersistenceSession persistence,
    IFrontOfficeExecutionContextAccessor contextAccessor,
    IClock clock) : IGroupDepositService
{
    public async Task<IReadOnlyList<GroupDepositDto>> GetAsync(
        Guid propertyId,
        Guid groupReservationId,
        CancellationToken ct)
    {
        var rows = await db.FrontOfficeGroupDeposits
            .AsNoTracking()
            .Include(x => x.GroupReservation)
            .Where(x => x.PropertyId == propertyId && x.GroupReservationId == groupReservationId)
            .OrderByDescending(x => x.BusinessDate)
            .ThenByDescending(x => x.CreatedAtUtc)
            .ToListAsync(ct);
        return rows.Select(Map).ToList();
    }

    public async Task<GroupDepositDto> RecordAsync(RecordGroupDepositRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        var context = RequireContext();
        var propertyId = context.Property.PropertyId.Value;
        var group = await db.FrontOfficeGroupReservations
            .SingleOrDefaultAsync(x => x.GroupReservationId == request.GroupReservationId && x.PropertyId == propertyId, ct)
            ?? throw new KeyNotFoundException("GROUP_NOT_FOUND");

        var now = clock.UtcNow;
        group.RecordDeposit(request.Amount, now, context.Actor.ActorId);
        var deposit = GroupDeposit.Create(
            propertyId,
            group.GroupReservationId,
            request.Amount,
            request.CurrencyCode,
            request.PaymentMethod,
            request.BusinessDate,
            request.ExternalReference,
            request.Note,
            now,
            context.Actor.ActorId);
        db.FrontOfficeGroupDeposits.Add(deposit);
        StageAudit("GroupDepositRecorded", deposit, group);
        await CommitAsync(ct, "GROUP_DEPOSIT_RECORD_FAILED");
        return Map(deposit, group);
    }

    public async Task<GroupDepositDto> ReverseAsync(ReverseGroupDepositRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        var context = RequireContext();
        var propertyId = context.Property.PropertyId.Value;
        var deposit = await db.FrontOfficeGroupDeposits
            .Include(x => x.GroupReservation)
            .SingleOrDefaultAsync(x => x.GroupDepositId == request.GroupDepositId && x.PropertyId == propertyId, ct)
            ?? throw new KeyNotFoundException("GROUP_DEPOSIT_NOT_FOUND");
        if (request.ExpectedRowVersion is not { Length: > 0 })
            throw new InvalidOperationException("GROUP_DEPOSIT_ROW_VERSION_REQUIRED");

        db.Entry(deposit).Property(x => x.RowVersion).OriginalValue = request.ExpectedRowVersion;
        var group = deposit.GroupReservation ?? throw new InvalidOperationException("GROUP_DEPOSIT_GROUP_NOT_LOADED");
        var now = clock.UtcNow;
        group.ReverseDeposit(deposit.Amount, now, context.Actor.ActorId);
        deposit.Reverse(request.Reason, now, context.Actor.ActorId);
        StageAudit("GroupDepositReversed", deposit, group, request.Reason);
        await CommitAsync(ct, "GROUP_DEPOSIT_REVERSE_FAILED");
        return Map(deposit, group);
    }

    private FrontOfficeExecutionContext RequireContext()
        => contextAccessor.Current ?? throw new InvalidOperationException("FRONT_OFFICE_CONTEXT_REQUIRED");

    private void StageAudit(string action, GroupDeposit deposit, GroupReservation group, string? reason = null)
    {
        persistence.StageAudit(new FrontOfficeAuditIntent(
            action,
            nameof(GroupDeposit),
            deposit.GroupDepositId.ToString("D"),
            "Succeeded",
            ReasonCode: reason,
            Metadata: new Dictionary<string, string>
            {
                ["PropertyId"] = deposit.PropertyId.ToString("D"),
                ["GroupReservationId"] = group.GroupReservationId.ToString("D"),
                ["Amount"] = deposit.Amount.ToString(System.Globalization.CultureInfo.InvariantCulture),
                ["CurrencyCode"] = deposit.CurrencyCode,
                ["PaymentMethod"] = deposit.PaymentMethod,
                ["Status"] = deposit.Status.ToString()
            }));
    }

    private async Task CommitAsync(CancellationToken ct, string errorCode)
    {
        var result = await persistence.CommitAsync(ct);
        if (result.IsFailure)
            throw new InvalidOperationException(result.Error?.Code ?? errorCode);
    }

    private static GroupDepositDto Map(GroupDeposit deposit)
        => Map(deposit, deposit.GroupReservation ?? throw new InvalidOperationException("GROUP_DEPOSIT_GROUP_NOT_LOADED"));

    private static GroupDepositDto Map(GroupDeposit deposit, GroupReservation group)
        => new(
            deposit.GroupDepositId,
            deposit.PropertyId,
            deposit.GroupReservationId,
            group.GroupCode,
            group.GroupName,
            deposit.Amount,
            deposit.CurrencyCode,
            deposit.PaymentMethod,
            deposit.ExternalReference,
            deposit.BusinessDate,
            deposit.Note,
            deposit.Status,
            deposit.CreatedAtUtc,
            deposit.ReversedAtUtc,
            deposit.ReversalReason,
            deposit.RowVersion);
}
