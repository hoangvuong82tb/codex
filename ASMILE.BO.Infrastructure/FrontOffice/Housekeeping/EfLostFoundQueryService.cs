using ASMILE.BO.Application.FrontOffice.Housekeeping;
using ASMILE.BO.Core.FrontOffice.LostFound;
using ASMILE.BO.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ASMILE.BO.Infrastructure.FrontOffice.Housekeeping;

public sealed class EfLostFoundQueryService(AppDbContext db) : ILostFoundQueryService
{
    public async Task<LostFoundQueryResult> SearchAsync(
        LostFoundSearchFilter filter,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = db.LostFoundItems
            .AsNoTracking()
            .Where(item => item.PropertyId == filter.PropertyId);

        if (!string.IsNullOrWhiteSpace(filter.Keyword))
        {
            var keyword = filter.Keyword.Trim();
            query = query.Where(item =>
                item.ItemNumber.Contains(keyword) ||
                item.ItemName.Contains(keyword) ||
                (item.Description != null && item.Description.Contains(keyword)) ||
                (item.OwnerGuestName != null && item.OwnerGuestName.Contains(keyword)) ||
                (item.FoundLocation != null && item.FoundLocation.Contains(keyword)));
        }

        if (filter.Status is { } status)
        {
            var domainStatus = status switch
            {
                LostFoundItemStatus.Found => LostFoundStatus.Found,
                LostFoundItemStatus.Stored => LostFoundStatus.Stored,
                LostFoundItemStatus.Claimed => LostFoundStatus.Claimed,
                LostFoundItemStatus.Returned => LostFoundStatus.Returned,
                LostFoundItemStatus.Donated => LostFoundStatus.Disposed,
                LostFoundItemStatus.Unclaimed => LostFoundStatus.Archived,
                LostFoundItemStatus.Contacted => LostFoundStatus.Contacted,
                _ => LostFoundStatus.Found
            };
            query = query.Where(item => item.Status == domainStatus);
        }

        if (filter.FoundFrom is { } foundFrom)
            query = query.Where(item => item.FoundDateTime >= foundFrom.Date);
        if (filter.FoundTo is { } foundTo)
        {
            var exclusiveTo = foundTo.Date.AddDays(1);
            query = query.Where(item => item.FoundDateTime < exclusiveTo);
        }

        if (!string.IsNullOrWhiteSpace(filter.Location))
        {
            var location = filter.Location.Trim();
            query = query.Where(item =>
                (item.FoundLocation != null && item.FoundLocation.Contains(location)) ||
                (item.StoredLocation != null && item.StoredLocation.Contains(location)));
        }

        if (!string.IsNullOrWhiteSpace(filter.Category) &&
            TryParseCategory(filter.Category, out var category))
        {
            query = query.Where(item => item.Category == category);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var dashboard = await BuildDashboardAsync(
            db.LostFoundItems.AsNoTracking().Where(item => item.PropertyId == filter.PropertyId),
            cancellationToken);

        var safePage = Math.Max(1, page);
        var safePageSize = Math.Clamp(pageSize, 1, 200);
        var entities = await query
            .OrderByDescending(item => item.FoundDateTime)
            .ThenBy(item => item.ItemNumber)
            .Skip((safePage - 1) * safePageSize)
            .Take(safePageSize)
            .ToListAsync(cancellationToken);

        var items = entities.Select(item => new LostFoundItemReadDto(
                item.LostFoundItemId,
                item.ItemNumber,
                item.ItemName,
                item.Status,
                item.Category.ToString(),
                item.Description,
                item.Color,
                item.Brand,
                item.SerialNumber,
                item.FoundLocation,
                item.FoundDateTime,
                item.FoundByStaff,
                item.StoredLocation,
                item.OwnerGuestName,
                item.OwnerGuestId,
                item.ReservationId,
                item.HotelStayId,
                item.ClaimedDate,
                item.ContactDate,
                item.ContactedBy,
                item.ContactMethod,
                item.ContactResult,
                item.ContactNotes,
                item.ReleasedBy,
                item.ReleasedTo,
                item.DisposalDate,
                item.PhotoPath,
                item.Notes,
                item.CreatedAtUtc,
                Array.Empty<LostFoundHistoryReadDto>())).ToList();

        if (items.Count > 0)
        {
            var ids = items.Select(item => item.LostFoundItemId).ToArray();
            var histories = await db.LostFoundHistories
                .AsNoTracking()
                .Where(history => ids.Contains(history.LostFoundItemId))
                .OrderBy(history => history.OccurredAtUtc)
                .Select(history => new
                {
                    history.LostFoundItemId,
                    Dto = new LostFoundHistoryReadDto(
                        history.LostFoundHistoryId,
                        history.Action,
                        history.OccurredAtUtc)
                })
                .ToListAsync(cancellationToken);

            var historyByItem = histories
                .GroupBy(history => history.LostFoundItemId)
                .ToDictionary(group => group.Key, group => (IReadOnlyList<LostFoundHistoryReadDto>)group.Select(x => x.Dto).ToList());
            items = items.Select(item => item with
            {
                History = historyByItem.TryGetValue(item.LostFoundItemId, out var history)
                    ? history
                    : Array.Empty<LostFoundHistoryReadDto>()
            }).ToList();
        }

        return new LostFoundQueryResult(items, dashboard, totalCount);
    }

    private static async Task<LostFoundDashboardDto> BuildDashboardAsync(
        IQueryable<LostFoundItem> query,
        CancellationToken cancellationToken)
    {
        var counts = await query
            .GroupBy(item => 1)
            .Select(group => new
            {
                Total = group.Count(),
                Stored = group.Count(item => item.Status == LostFoundStatus.Stored),
                Claimed = group.Count(item => item.Status == LostFoundStatus.Matched || item.Status == LostFoundStatus.Claimed),
                Returned = group.Count(item => item.Status == LostFoundStatus.Returned),
                Disposed = group.Count(item => item.Status == LostFoundStatus.Disposed),
                Unclaimed = group.Count(item => item.Status == LostFoundStatus.Archived)
            })
            .SingleOrDefaultAsync(cancellationToken);

        var total = counts?.Total ?? 0;
        var claimed = counts?.Claimed ?? 0;
        return new LostFoundDashboardDto(
            total,
            counts?.Stored ?? 0,
            claimed,
            counts?.Returned ?? 0,
            counts?.Disposed ?? 0,
            counts?.Unclaimed ?? 0,
            total == 0 ? 0 : (decimal)claimed * 100m / total);
    }

    private static bool TryParseCategory(string value, out LostFoundCategory category)
    {
        if (Enum.TryParse(value, true, out category))
            return true;

        var normalized = value.Trim().ToLowerInvariant();
        category = normalized switch
        {
            "điện tử" or "dien tu" => LostFoundCategory.Electronics,
            "trang sức" or "trang suc" => LostFoundCategory.Jewelry,
            "giấy tờ" or "giay to" => LostFoundCategory.Documents,
            "tiền" or "tien" => LostFoundCategory.Cash,
            "quần áo" or "quan ao" => LostFoundCategory.Clothing,
            "hành lý" or "hanh ly" => LostFoundCategory.Luggage,
            "chìa khóa" or "chia khoa" => LostFoundCategory.Keys,
            "y tế" or "y te" => LostFoundCategory.Medical,
            _ => LostFoundCategory.Others
        };
        return normalized is "điện tử" or "dien tu" or "trang sức" or "trang suc"
            or "giấy tờ" or "giay to" or "tiền" or "tien" or "quần áo" or "quan ao"
            or "hành lý" or "hanh ly" or "chìa khóa" or "chia khoa" or "y tế" or "y te";
    }
}
