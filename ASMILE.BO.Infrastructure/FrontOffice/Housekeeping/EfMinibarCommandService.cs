using ASMILE.BO.Application.FrontOffice.Housekeeping;
using ASMILE.BO.Application.DTOs;
using ASMILE.BO.Application.Interfaces;
using ASMILE.BO.Core.Entities;
using ASMILE.BO.Core.FrontOffice.Folio;
using ASMILE.BO.Core.FrontOffice.Housekeeping;
using ASMILE.BO.Core.Interfaces.Repositories;
using ASMILE.BO.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ASMILE.BO.Infrastructure.FrontOffice.Housekeeping;

public sealed class EfMinibarCommandService : IMinibarCommandService
{
    private readonly AppDbContext _db;
    private readonly IInventoryAppService? _inventoryService;
    private readonly IUnitOfWork? _unitOfWork;

    public EfMinibarCommandService(
        AppDbContext db,
        IInventoryAppService? inventoryService = null,
        IUnitOfWork? unitOfWork = null)
    {
        _db = db;
        _inventoryService = inventoryService;
        _unitOfWork = unitOfWork;
    }

    public async Task CheckAsync(CheckMinibarCommand command, CancellationToken cancellationToken = default)
    {
        ValidateScope(command.PropertyId, command.StayId, command.RoomId, command.ActorId);

        var stayExists = await _db.HotelStays.AsNoTracking().AnyAsync(
            x => x.PropertyId == command.PropertyId && x.HotelStayId == command.StayId && x.RoomId == command.RoomId,
            cancellationToken);
        if (!stayExists)
            throw new InvalidOperationException("MINIBAR_STAY_NOT_FOUND");

        var roomExists = await _db.HotelRooms.AsNoTracking().AnyAsync(
            x => x.PropertyId == command.PropertyId && x.HotelRoomId == command.RoomId,
            cancellationToken);
        if (!roomExists)
            throw new InvalidOperationException("MINIBAR_ROOM_NOT_FOUND");

        var minibarProfiles = await _db.HotelMinibarItemProfiles.AsNoTracking()
            .Where(x => x.PropertyId == command.PropertyId && x.IsActive)
            .ToListAsync(cancellationToken);
        var itemVatRates = minibarProfiles.ToDictionary(x => x.ItemCode, x => x.VatRate, StringComparer.OrdinalIgnoreCase);

        MinibarSession? session = null;
        if (command.SessionId != Guid.Empty)
        {
            session = await _db.MinibarSessions
                .Include(x => x.Consumptions)
                .SingleOrDefaultAsync(x => x.SessionId == command.SessionId && x.PropertyId == command.PropertyId, cancellationToken);
            if (session is null)
                throw new InvalidOperationException("MINIBAR_SESSION_NOT_FOUND");
            if (session.StayId != command.StayId || session.HotelRoomId != command.RoomId)
                throw new InvalidOperationException("MINIBAR_SESSION_SCOPE_MISMATCH");
            _db.MinibarConsumptions.RemoveRange(session.Consumptions);
        }
        else
        {
            // A room/stay has one active minibar check at a time. Reuse an
            // existing open/checked session when the UI has not yet received
            // its persistence id (for example, the first refresh after
            // arrival) instead of creating duplicate sessions.
            session = await _db.MinibarSessions
                .Include(x => x.Consumptions)
                .Where(x => x.PropertyId == command.PropertyId
                    && x.StayId == command.StayId
                    && x.HotelRoomId == command.RoomId
                    && x.Status != "Posted")
                .OrderByDescending(x => x.CheckedAtUtc)
                .FirstOrDefaultAsync(cancellationToken);

            if (session is null)
            {
                var postedSessionExists = await _db.MinibarSessions.AsNoTracking().AnyAsync(
                    x => x.PropertyId == command.PropertyId
                        && x.StayId == command.StayId
                        && x.HotelRoomId == command.RoomId
                        && x.Status == "Posted",
                    cancellationToken);
                if (postedSessionExists)
                    throw new InvalidOperationException("MINIBAR_SESSION_ALREADY_POSTED");

                session = MinibarSession.Create(command.PropertyId, command.StayId, command.RoomId);
                _db.MinibarSessions.Add(session);
            }
            else
            {
                _db.MinibarConsumptions.RemoveRange(session.Consumptions);
            }
        }

        foreach (var line in command.Items.Where(x => x.Quantity > 0))
        {
            var vatRate = itemVatRates.TryGetValue(line.ItemCode, out var catalogVatRate)
                ? catalogVatRate
                : line.VatRate;
            _db.MinibarConsumptions.Add(MinibarConsumption.Create(
                command.PropertyId, session.SessionId, line.ItemCode, line.ItemName, line.Quantity, line.UnitPrice, vatRate));
        }

        session.Check(command.ActorId!.Value, command.CheckedAtUtc);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task PostAsync(PostMinibarCommand command, CancellationToken cancellationToken = default)
    {
        ValidateScope(command.PropertyId, command.SessionId, command.ActorId);

        var session = await _db.MinibarSessions
            .Include(x => x.Consumptions)
            .SingleOrDefaultAsync(
            x => x.SessionId == command.SessionId && x.PropertyId == command.PropertyId, cancellationToken)
            ?? throw new InvalidOperationException("MINIBAR_SESSION_NOT_FOUND");

        var folioEntryId = command.FolioEntryId;
        if (folioEntryId != Guid.Empty)
        {
            var entry = await _db.HotelFolioEntries
                .Include(x => x.HotelFolio)
                .SingleOrDefaultAsync(x => x.HotelFolioEntryId == folioEntryId && x.PropertyId == command.PropertyId, cancellationToken)
                ?? throw new InvalidOperationException("MINIBAR_FOLIO_ENTRY_NOT_FOUND");

            if (entry.EntryType != HotelFolioEntryType.MiniBar || entry.HotelFolio is null || entry.HotelFolio.HotelStayId != session.StayId)
                throw new InvalidOperationException("MINIBAR_FOLIO_ENTRY_INVALID");
        }
        else
        {
            var amount = session.Consumptions.Sum(x => x.LineAmount);
            if (amount <= 0)
                throw new InvalidOperationException("MINIBAR_NO_CHARGE");

            var folio = await _db.HotelFolios
                .Include(x => x.Entries)
                .SingleOrDefaultAsync(x => x.PropertyId == command.PropertyId && x.HotelStayId == session.StayId, cancellationToken)
                ?? throw new InvalidOperationException("MINIBAR_FOLIO_NOT_FOUND");

            var existing = folio.Entries.FirstOrDefault(x =>
                x.EntryType == HotelFolioEntryType.MiniBar
                && x.ReferenceType == "MiniBarConsumption"
                && x.ReferenceId == session.SessionId
                && !x.ReversedByEntryId.HasValue);
            if (existing is not null)
            {
                folioEntryId = existing.HotelFolioEntryId;
            }
            else
            {
                var created = folio.AddMiniBarCharge(
                    amount,
                    $"Minibar — phiên {session.SessionId:N}",
                    session.SessionId,
                    command.PostedAtUtc,
                    command.ActorId.HasValue
                        ? new ASMILE.BO.Core.FrontOffice.Foundation.Identifiers.ActorId(command.ActorId.Value)
                        : null,
                    DateOnly.FromDateTime(command.PostedAtUtc.ToLocalTime()));
                _db.HotelFolioEntries.Add(created);
                folioEntryId = created.HotelFolioEntryId;

                var vatAmount = session.Consumptions.Sum(x => x.VatAmount);
                if (vatAmount > 0)
                {
                    _db.HotelFolioEntries.Add(folio.AddTax(
                        vatAmount,
                        $"VAT Minibar — phiên {session.SessionId:N}",
                        command.PostedAtUtc,
                        command.ActorId.HasValue
                            ? new ASMILE.BO.Core.FrontOffice.Foundation.Identifiers.ActorId(command.ActorId.Value)
                            : null,
                        DateOnly.FromDateTime(command.PostedAtUtc.ToLocalTime())));
                }
            }
        }

        session.Post(folioEntryId, command.PostedAtUtc);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task UnpostAsync(UnpostMinibarCommand command, CancellationToken cancellationToken = default)
    {
        ValidateScope(command.PropertyId, command.SessionId, command.ActorId);

        var session = await _db.MinibarSessions.SingleOrDefaultAsync(
            x => x.SessionId == command.SessionId && x.PropertyId == command.PropertyId, cancellationToken)
            ?? throw new InvalidOperationException("MINIBAR_SESSION_NOT_FOUND");

        session.Unpost(command.OccurredAtUtc);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task RestockAsync(RestockMinibarCommand command, CancellationToken cancellationToken = default)
    {
        if (_unitOfWork is null)
        {
            await RestockCoreAsync(command, cancellationToken);
            return;
        }

        await _unitOfWork.ExecuteInTransactionAsync(
            ct => RestockCoreAsync(command, ct),
            cancellationToken);
    }

    private async Task RestockCoreAsync(RestockMinibarCommand command, CancellationToken cancellationToken)
    {
        if (command.PropertyId == Guid.Empty || command.TransactionId == Guid.Empty || command.RoomId == Guid.Empty || command.ActorId == Guid.Empty)
            throw new InvalidOperationException("MINIBAR_RESTOCK_SCOPE_REQUIRED");
        if (command.Quantity <= 0)
            throw new InvalidOperationException("MINIBAR_RESTOCK_QUANTITY_INVALID");
        if (await _db.MinibarRestockTransactions.AnyAsync(x => x.TransactionId == command.TransactionId, cancellationToken))
            return;

        var warehouse = await _db.Warehouses.AsNoTracking().SingleOrDefaultAsync(
            x => x.IsActive && (x.WarehouseCode == command.Warehouse || x.WarehouseName == command.Warehouse),
            cancellationToken);
        if (warehouse is null)
            throw new InvalidOperationException("MINIBAR_WAREHOUSE_NOT_FOUND");

        var profile = await _db.HotelMinibarItemProfiles.AsNoTracking().SingleOrDefaultAsync(
            x => x.PropertyId == command.PropertyId && x.IsActive && x.ItemCode == command.ItemCode,
            cancellationToken);
        Item? item;
        if (profile is not null)
        {
            item = await _db.Items.AsNoTracking().SingleOrDefaultAsync(x => x.Id == profile.ItemId && x.IsActive, cancellationToken);
        }
        else if (await _db.HotelMinibarItemProfiles.AsNoTracking().AnyAsync(x => x.PropertyId == command.PropertyId, cancellationToken))
        {
            throw new InvalidOperationException("MINIBAR_ITEM_NOT_CONFIGURED_FOR_PROPERTY");
        }
        else
        {
            // Compatibility path for a property that has not migrated any
            // minibar profile yet. Once one profile exists, the property is
            // fully governed by HangMinibarFO and no cross-property fallback
            // is allowed.
            item = await _db.Items.AsNoTracking().SingleOrDefaultAsync(
                x => x.IsActive && x.IsMinibarItem && x.ItemCode == command.ItemCode,
                cancellationToken);
        }
        if (item is null) throw new InvalidOperationException("MINIBAR_ITEM_NOT_FOUND");

        var balance = await _db.StockBalances.AsNoTracking().SingleOrDefaultAsync(
            x => x.WarehouseId == warehouse.Id && x.ItemId == item.Id,
            cancellationToken);
        if (balance is null || balance.ClosingQty < command.Quantity)
            throw new InvalidOperationException("MINIBAR_STOCK_INSUFFICIENT");

        var roomExists = await _db.HotelRooms.AsNoTracking().AnyAsync(
            x => x.PropertyId == command.PropertyId && x.HotelRoomId == command.RoomId && x.IsActive,
            cancellationToken);
        if (!roomExists)
            throw new InvalidOperationException("MINIBAR_ROOM_NOT_FOUND");

        var note = command.Note;
        if (_inventoryService is not null)
        {
            var issue = await _inventoryService.SaveIssueAsync(new StockIssueDto
            {
                IssueNo = $"MINI-{DateTime.UtcNow:yyyyMMddHHmmssfff}-{Guid.NewGuid():N}",
                IssueDate = command.OccurredAtUtc.ToLocalTime().Date,
                Description = $"Cấp bù minibar phòng {command.RoomId:N}",
                WarehouseId = warehouse.Id,
                WarehouseCode = warehouse.WarehouseCode,
                DepartmentCode = "MINI",
                DebitAccountCode = "632",
                CreditAccountCode = "152",
                Details =
                [
                    new StockTransactionDetailDto
                    {
                        LineNo = 1,
                        ItemId = item.Id,
                        ItemCode = item.ItemCode,
                        ItemName = item.ItemName,
                        Unit = item.Unit,
                        WarehouseId = warehouse.Id,
                        WarehouseCode = warehouse.WarehouseCode,
                        Quantity = command.Quantity,
                        UnitPrice = balance.AvgPrice,
                        Amount = Math.Round(command.Quantity * balance.AvgPrice, 2, MidpointRounding.AwayFromZero)
                    }
                ]
            }, cancellationToken);
            await _inventoryService.ApproveIssueAndPostAsync(issue.Id, command.ActorId.ToString(), cancellationToken);
            note = string.IsNullOrWhiteSpace(note)
                ? $"StockIssue:{issue.IssueNo}"
                : $"{note.Trim()} | StockIssue:{issue.IssueNo}";
        }

        _db.MinibarRestockTransactions.Add(MinibarRestockTransaction.Create(
            command.TransactionId, command.PropertyId, command.RoomId, command.ItemCode,
            command.ItemName, command.Quantity, command.Warehouse, command.OccurredAtUtc,
            command.ActorId, note));
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task InventoryAsync(InventoryMinibarCommand command, CancellationToken cancellationToken = default)
    {
        if (command.PropertyId == Guid.Empty || command.InventoryId == Guid.Empty || command.RoomId == Guid.Empty || command.ActorId == Guid.Empty)
            throw new InvalidOperationException("MINIBAR_INVENTORY_SCOPE_REQUIRED");
        if (command.Items is null || command.Items.Count == 0)
            throw new InvalidOperationException("MINIBAR_INVENTORY_LINES_REQUIRED");
        if (await _db.MinibarInventoryAudits.AnyAsync(x => x.InventoryId == command.InventoryId, cancellationToken))
            return;

        var roomExists = await _db.HotelRooms.AsNoTracking().AnyAsync(
            x => x.PropertyId == command.PropertyId && x.HotelRoomId == command.RoomId && x.IsActive,
            cancellationToken);
        if (!roomExists)
            throw new InvalidOperationException("MINIBAR_ROOM_NOT_FOUND");

        if (command.StayId.HasValue)
        {
            var stayExists = await _db.HotelStays.AsNoTracking().AnyAsync(
                x => x.PropertyId == command.PropertyId && x.HotelStayId == command.StayId.Value && x.RoomId == command.RoomId,
                cancellationToken);
            if (!stayExists)
                throw new InvalidOperationException("MINIBAR_STAY_NOT_FOUND");
        }

        var audit = MinibarInventoryAudit.Create(
            command.InventoryId, command.PropertyId, command.RoomId, command.StayId,
            command.CountedAtUtc, command.ActorId, command.Note);
        foreach (var item in command.Items)
        {
            if (item.Quantity < 0 || item.UnitPrice < 0)
                throw new InvalidOperationException("MINIBAR_INVENTORY_QUANTITY_INVALID");
            audit.AddLine(item.ItemCode, item.ItemName, item.Quantity, item.UnitPrice);
        }

        _db.MinibarInventoryAudits.Add(audit);
        await _db.SaveChangesAsync(cancellationToken);
    }

    private static void ValidateScope(Guid propertyId, Guid sessionOrStayId, Guid roomOrEntryId, Guid? actorId)
    {
        if (propertyId == Guid.Empty || sessionOrStayId == Guid.Empty || roomOrEntryId == Guid.Empty || !actorId.HasValue || actorId.Value == Guid.Empty)
            throw new InvalidOperationException("MINIBAR_COMMAND_SCOPE_REQUIRED");
    }

    private static void ValidateScope(Guid propertyId, Guid sessionId, Guid? actorId)
    {
        if (propertyId == Guid.Empty || sessionId == Guid.Empty || !actorId.HasValue || actorId.Value == Guid.Empty)
            throw new InvalidOperationException("MINIBAR_COMMAND_SCOPE_REQUIRED");
    }
}
