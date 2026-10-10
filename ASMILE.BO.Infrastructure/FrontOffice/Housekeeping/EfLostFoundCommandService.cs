using ASMILE.BO.Application.FrontOffice.Housekeeping;
using ASMILE.BO.Core.FrontOffice.Foundation.Identifiers;
using ASMILE.BO.Core.FrontOffice.LostFound;
using ASMILE.BO.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ASMILE.BO.Infrastructure.FrontOffice.Housekeeping;

/// <summary>
/// Persists Lost &amp; Found registration, maintenance, and operational transitions.
/// The UI must use this boundary for every record loaded from EF.
/// </summary>
public sealed class EfLostFoundCommandService(AppDbContext db) : ILostFoundCommandService
{
    public async Task<RegisterLostFoundItemResult> RegisterAsync(
        RegisterLostFoundItemCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command.PropertyId == Guid.Empty)
            throw new ArgumentException("Property identifier is required.", nameof(command));
        if (string.IsNullOrWhiteSpace(command.ItemName))
            throw new ArgumentException("Item name is required.", nameof(command));

        var itemNumber = string.IsNullOrWhiteSpace(command.ItemNumber)
            ? $"LF{DateTime.UtcNow:yyyyMMddHHmmssfff}"
            : command.ItemNumber.Trim();
        if (await db.LostFoundItems.AnyAsync(
                item => item.PropertyId == command.PropertyId && item.ItemNumber == itemNumber,
                cancellationToken))
            throw new InvalidOperationException($"Lost & Found item number {itemNumber} already exists.");

        var item = LostFoundItem.Register(
            command.PropertyId,
            itemNumber,
            command.ItemName.Trim(),
            command.Category,
            command.Description,
            command.FoundLocation,
            command.FoundDateTime == default ? DateTime.UtcNow : command.FoundDateTime,
            command.FoundByStaff,
            command.StoredLocation,
            command.Color,
            command.Brand,
            command.SerialNumber,
            command.Notes,
            DateTime.UtcNow,
            command.ActorId is { } actorId && actorId != Guid.Empty ? ActorId.From(actorId) : null);

        db.LostFoundItems.Add(item);
        await db.SaveChangesAsync(cancellationToken);
        return new RegisterLostFoundItemResult(item.LostFoundItemId, item.ItemNumber);
    }

    public Task UpdateAsync(UpdateLostFoundItemCommand command, CancellationToken cancellationToken = default) =>
        ExecuteAsync(command.ItemId, command.PropertyId, cancellationToken, (item, now) =>
            item.UpdateDetails(
                command.ItemName,
                command.Category,
                command.Description,
                command.Color,
                command.Brand,
                command.SerialNumber,
                command.Notes,
                now));

    public Task ContactAsync(ContactLostFoundItemCommand command, CancellationToken cancellationToken = default) =>
        ExecuteAsync(command.ItemId, command.PropertyId, cancellationToken, (item, now) =>
            item.RecordContact(
                command.ContactedBy,
                command.ContactDate,
                command.ContactMethod,
                command.ContactResult,
                command.ContactNotes,
                now));

    public async Task DeleteAsync(DeleteLostFoundItemCommand command, CancellationToken cancellationToken = default)
    {
        if (command.ItemId == Guid.Empty || command.PropertyId == Guid.Empty)
            throw new ArgumentException("Item and property identifiers are required.");

        var item = await db.LostFoundItems
            .SingleOrDefaultAsync(candidate => candidate.LostFoundItemId == command.ItemId, cancellationToken);
        if (item is null)
            throw new InvalidOperationException($"Lost & Found item {command.ItemId} was not found.");
        if (item.PropertyId != command.PropertyId)
            throw new InvalidOperationException("The Lost & Found item belongs to another property.");
        if (item.Status is not LostFoundStatus.Found)
            throw new InvalidOperationException($"Only newly found items can be deleted; current status is {item.Status}.");

        db.LostFoundItems.Remove(item);
        await db.SaveChangesAsync(cancellationToken);
    }

    public Task StoreAsync(StoreLostFoundItemCommand command, CancellationToken cancellationToken = default) =>
        ExecuteAsync(command.ItemId, command.PropertyId, cancellationToken, (item, now) =>
        {
            if (string.IsNullOrWhiteSpace(command.Location))
                throw new ArgumentException("Storage location is required.", nameof(command));
            if (item.Status is LostFoundStatus.Returned or LostFoundStatus.Disposed or LostFoundStatus.Archived)
                throw new InvalidOperationException($"Item {item.ItemNumber} cannot be stored from status {item.Status}.");
            item.Store(command.Location.Trim(), command.OccurredAtUtc == default ? now : command.OccurredAtUtc);
        });

    public Task ReturnAsync(ReturnLostFoundItemCommand command, CancellationToken cancellationToken = default) =>
        ExecuteAsync(command.ItemId, command.PropertyId, cancellationToken, (item, now) =>
        {
            if (string.IsNullOrWhiteSpace(command.ReleasedTo))
                throw new ArgumentException("Recipient is required.", nameof(command));
            if (item.Status is LostFoundStatus.Returned or LostFoundStatus.Disposed or LostFoundStatus.Archived)
                throw new InvalidOperationException($"Item {item.ItemNumber} cannot be returned from status {item.Status}.");
            item.ReturnItem(
                string.IsNullOrWhiteSpace(command.ReleasedBy) ? "Front Office" : command.ReleasedBy.Trim(),
                command.ReleasedTo.Trim(),
                command.OccurredAtUtc == default ? now : command.OccurredAtUtc);
        });

    public Task DisposeAsync(DisposeLostFoundItemCommand command, CancellationToken cancellationToken = default) =>
        ExecuteAsync(command.ItemId, command.PropertyId, cancellationToken, (item, now) =>
        {
            if (string.IsNullOrWhiteSpace(command.Reason))
                throw new ArgumentException("Disposal reason is required.", nameof(command));
            if (item.Status is LostFoundStatus.Returned or LostFoundStatus.Disposed or LostFoundStatus.Archived)
                throw new InvalidOperationException($"Item {item.ItemNumber} cannot be disposed from status {item.Status}.");
            item.Dispose(command.OccurredAtUtc == default ? now : command.OccurredAtUtc, command.Reason.Trim());
        });

    private async Task ExecuteAsync(
        Guid itemId,
        Guid propertyId,
        CancellationToken cancellationToken,
        Action<LostFoundItem, DateTime> transition)
    {
        if (itemId == Guid.Empty || propertyId == Guid.Empty)
            throw new ArgumentException("Item and property identifiers are required.");

        var item = await db.LostFoundItems
            .SingleOrDefaultAsync(candidate => candidate.LostFoundItemId == itemId, cancellationToken);
        if (item is null)
            throw new InvalidOperationException($"Lost & Found item {itemId} was not found.");
        if (item.PropertyId != propertyId)
            throw new InvalidOperationException("The Lost & Found item belongs to another property.");

        var existingHistoryIds = (await db.LostFoundHistories
            .Where(history => history.LostFoundItemId == item.LostFoundItemId)
            .Select(history => history.LostFoundHistoryId)
            .ToListAsync(cancellationToken)).ToHashSet();

        var now = DateTime.UtcNow;
        transition(item, now);

        foreach (var history in item.History.Where(history => !existingHistoryIds.Contains(history.LostFoundHistoryId)))
            db.LostFoundHistories.Add(history);

        await db.SaveChangesAsync(cancellationToken);
    }
}
