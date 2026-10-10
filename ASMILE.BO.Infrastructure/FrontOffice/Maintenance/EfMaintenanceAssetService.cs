using ASMILE.BO.Application.FrontOffice.Maintenance;
using ASMILE.BO.Application.FrontOffice.Foundation.Audit;
using ASMILE.BO.Application.FrontOffice.Foundation.Contexts;
using ASMILE.BO.Application.FrontOffice.Foundation.Persistence;
using ASMILE.BO.Core.FrontOffice.Maintenance;
using ASMILE.BO.Infrastructure.FrontOffice.Foundation;
using ASMILE.BO.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ASMILE.BO.Infrastructure.FrontOffice.Maintenance;

/// <summary>
/// Real persistence adapter for operational maintenance assets.
/// It intentionally does not use the accounting FixedAsset tables.
/// </summary>
public sealed class EfMaintenanceAssetService(
    AppDbContext db,
    IFrontOfficePersistenceSession persistence,
    IFrontOfficeExecutionContextAccessor contextAccessor,
    IClock clock) : IAssetService
{
    public async Task<IReadOnlyList<AssetDto>> GetAssetsByRoomAsync(Guid roomId, CancellationToken ct)
    {
        var propertyId = RequireContext().Property.PropertyId.Value;
        if (roomId == Guid.Empty) return [];

        var rows = await db.MaintenanceAssets.AsNoTracking()
            .Where(x => x.PropertyId == propertyId && x.RoomId == roomId && x.IsActive)
            .OrderBy(x => x.AssetCode)
            .ToListAsync(ct);
        return rows.Select(Map).ToList();
    }

    public async Task<IReadOnlyList<AssetHealthDto>> GetHealthAsync(Guid propertyId, AssetCategory? category, CancellationToken ct)
    {
        RequireContext(propertyId);
        var query = db.MaintenanceAssets.AsNoTracking()
            .Where(x => x.PropertyId == propertyId && x.IsActive);
        if (category is AssetCategory requested)
            query = query.Where(x => x.Category == requested.ToString());

        var now = clock.UtcNow;
        var rows = await query.ToListAsync(ct);
        rows = rows.OrderBy(x => x.ConditionScore).ThenBy(x => x.AssetCode).ToList();
        return rows.Select(x => MapHealth(x, now)).ToList();
    }

    public async Task<AssetDashboard> GetDashboardAsync(Guid propertyId, CancellationToken ct)
    {
        RequireContext(propertyId);
        var now = clock.UtcNow;
        var assets = await db.MaintenanceAssets.AsNoTracking()
            .Where(x => x.PropertyId == propertyId)
            .ToListAsync(ct);
        assets = assets.OrderBy(x => x.ConditionScore).ThenBy(x => x.AssetCode).ToList();
        var openOrders = await db.MaintenanceWorkOrders.AsNoTracking()
            .Where(x => x.PropertyId == propertyId && x.Status != nameof(WorkOrderStatus.Closed) && x.Status != nameof(WorkOrderStatus.Cancelled))
            .Select(x => new { x.RoomId, x.Category })
            .ToListAsync(ct);

        var active = assets.Where(x => x.IsActive).ToList();
        var health = active.Select(x => MapHealth(x, now)).ToList();
        var withOpenOrders = active.Count(asset => asset.RoomId is Guid roomId && openOrders.Any(order => order.RoomId == roomId && order.Category == asset.Category));
        var due = active.Where(x => IsDue(x, now)).Select(Map).Take(20).ToList();
        var underWarranty = active.Count(x => x.WarrantyExpiry is DateTime expiry && expiry.Date >= now.Date);
        var expiredWarranty = active.Count(x => x.WarrantyExpiry is DateTime expiry && expiry.Date < now.Date);

        return new AssetDashboard(
            assets.Count,
            active.Count,
            underWarranty,
            expiredWarranty,
            withOpenOrders,
            active.Count == 0 ? 0m : decimal.Round(active.Average(x => x.ConditionScore), 2),
            health.Where(x => x.AtRisk).Take(10).ToList(),
            due);
    }

    public async Task RecordFailureAsync(Guid assetId, string description, CancellationToken ct)
    {
        var context = RequireContext();
        var propertyId = context.Property.PropertyId.Value;
        var asset = await db.MaintenanceAssets
            .Include(x => x.Failures)
            .SingleOrDefaultAsync(x => x.PropertyId == propertyId && x.AssetId == assetId, ct)
            ?? throw new KeyNotFoundException("MAINTENANCE_ASSET_NOT_FOUND");

        var failure = asset.RecordFailure(description, clock.UtcNow, context.Actor.ActorId?.Value);
        // The failure has a client-generated GUID and is reached through an already
        // tracked aggregate. Mark it explicitly so EF persists the append-only
        // history row instead of inferring an incorrect state from the graph.
        db.Entry(failure).State = EntityState.Added;
        StageAudit("MaintenanceAssetFailureRecorded", asset, failure.FailureId, propertyId);
        var result = await persistence.CommitAsync(ct);
        if (result.IsFailure)
            throw new InvalidOperationException(result.Error?.Code ?? "MAINTENANCE_ASSET_FAILURE_COMMIT_FAILED");
    }

    private FrontOfficeExecutionContext RequireContext(Guid? propertyId = null)
    {
        var context = contextAccessor.Current ?? throw new InvalidOperationException("FRONT_OFFICE_CONTEXT_REQUIRED");
        if (propertyId is Guid requested && requested != context.Property.PropertyId.Value)
            throw new InvalidOperationException("MAINTENANCE_PROPERTY_CONTEXT_MISMATCH");
        return context;
    }

    private void StageAudit(string action, MaintenanceAsset asset, Guid subjectId, Guid propertyId)
        => persistence.StageAudit(new FrontOfficeAuditIntent(
            action,
            nameof(MaintenanceAsset),
            asset.AssetId.ToString("D"),
            "Succeeded",
            Metadata: new Dictionary<string, string>
            {
                ["PropertyId"] = propertyId.ToString("D"),
                ["AssetCode"] = asset.AssetCode,
                ["SubjectId"] = subjectId.ToString("D"),
                ["FailureCount"] = asset.FailureCount.ToString(System.Globalization.CultureInfo.InvariantCulture),
                ["ConditionScore"] = asset.ConditionScore.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture)
            }));

    private static bool IsDue(MaintenanceAsset asset, DateTime nowUtc)
        => asset.NextMaintenanceDueUtc is DateTime due && due <= nowUtc || asset.ConditionScore < 70m;

    private static AssetDto Map(MaintenanceAsset asset)
        => new(
            asset.AssetId,
            asset.AssetCode,
            asset.Name,
            ParseCategory(asset.Category),
            asset.Location,
            asset.RoomId,
            asset.SerialNumber,
            asset.Model,
            asset.Vendor,
            asset.WarrantyExpiry,
            asset.InstalledAtUtc,
            asset.TotalRepairs,
            asset.RuntimeHours,
            asset.FailureCount,
            asset.ConditionScore,
            asset.LastMaintenanceAtUtc,
            asset.NextMaintenanceDueUtc,
            asset.IsActive);

    private static AssetHealthDto MapHealth(MaintenanceAsset asset, DateTime nowUtc)
        => new(
            asset.AssetId,
            asset.AssetCode,
            asset.Name,
            ParseCategory(asset.Category),
            asset.ConditionScore,
            asset.RuntimeHours,
            asset.FailureCount,
            decimal.Round(Math.Clamp(asset.ConditionScore / 100m, 0m, 1m), 2),
            IsDue(asset, nowUtc),
            asset.ConditionScore < 50m || asset.FailureCount >= 3);

    private static AssetCategory ParseCategory(string value)
        => Enum.TryParse<AssetCategory>(value, true, out var parsed)
            ? parsed
            : throw new InvalidOperationException("MAINTENANCE_ASSET_CATEGORY_INVALID");
}
