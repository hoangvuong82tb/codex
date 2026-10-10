namespace ASMILE.BO.Core.FrontOffice.Maintenance;

/// <summary>
/// A physical asset operated by the hotel maintenance team. This is deliberately
/// separate from the accounting FixedAsset aggregate: accounting owns value and
/// depreciation, while this aggregate owns condition, failures and service dates.
/// </summary>
public sealed class MaintenanceAsset
{
    private MaintenanceAsset() { }

    public Guid AssetId { get; private set; }
    public Guid PropertyId { get; private set; }
    public string AssetCode { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string Category { get; private set; } = string.Empty;
    public string? Location { get; private set; }
    public Guid? RoomId { get; private set; }
    public string? SerialNumber { get; private set; }
    public string? Model { get; private set; }
    public string? Vendor { get; private set; }
    public DateTime? WarrantyExpiry { get; private set; }
    public DateTime? InstalledAtUtc { get; private set; }
    public int TotalRepairs { get; private set; }
    public int RuntimeHours { get; private set; }
    public int FailureCount { get; private set; }
    public decimal ConditionScore { get; private set; }
    public DateTime? LastMaintenanceAtUtc { get; private set; }
    public DateTime? NextMaintenanceDueUtc { get; private set; }
    public bool IsActive { get; private set; }
    public byte[]? RowVersion { get; private set; }
    public ICollection<MaintenanceAssetFailure> Failures { get; private set; } = new List<MaintenanceAssetFailure>();

    public static MaintenanceAsset Create(
        Guid propertyId,
        string assetCode,
        string name,
        string category,
        string? location,
        Guid? roomId,
        string? serialNumber,
        string? model,
        string? vendor,
        DateTime? warrantyExpiry,
        DateTime? installedAtUtc,
        int runtimeHours,
        decimal conditionScore,
        DateTime? lastMaintenanceAtUtc,
        DateTime? nextMaintenanceDueUtc,
        DateTime nowUtc)
    {
        if (propertyId == Guid.Empty) throw new ArgumentException("MAINTENANCE_ASSET_PROPERTY_REQUIRED");
        if (string.IsNullOrWhiteSpace(assetCode)) throw new ArgumentException("MAINTENANCE_ASSET_CODE_REQUIRED");
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("MAINTENANCE_ASSET_NAME_REQUIRED");
        if (string.IsNullOrWhiteSpace(category)) throw new ArgumentException("MAINTENANCE_ASSET_CATEGORY_REQUIRED");
        if (runtimeHours < 0) throw new ArgumentException("MAINTENANCE_ASSET_RUNTIME_INVALID");
        if (conditionScore is < 0m or > 100m) throw new ArgumentException("MAINTENANCE_ASSET_CONDITION_INVALID");

        return new MaintenanceAsset
        {
            AssetId = Foundation.Identifiers.Uuid7.NewGuid(),
            PropertyId = propertyId,
            AssetCode = assetCode.Trim(),
            Name = name.Trim(),
            Category = category.Trim(),
            Location = Normalize(location),
            RoomId = roomId,
            SerialNumber = Normalize(serialNumber),
            Model = Normalize(model),
            Vendor = Normalize(vendor),
            WarrantyExpiry = warrantyExpiry,
            InstalledAtUtc = installedAtUtc,
            RuntimeHours = runtimeHours,
            ConditionScore = conditionScore,
            LastMaintenanceAtUtc = lastMaintenanceAtUtc,
            NextMaintenanceDueUtc = nextMaintenanceDueUtc,
            IsActive = true
        };
    }

    public MaintenanceAssetFailure RecordFailure(string description, DateTime nowUtc, Guid? actorId)
    {
        if (string.IsNullOrWhiteSpace(description)) throw new ArgumentException("MAINTENANCE_ASSET_FAILURE_DESCRIPTION_REQUIRED");
        if (!IsActive) throw new InvalidOperationException("MAINTENANCE_ASSET_INACTIVE");

        FailureCount++;
        TotalRepairs++;
        ConditionScore = Math.Max(0m, ConditionScore - 10m);
        var failure = MaintenanceAssetFailure.Create(AssetId, description, nowUtc, actorId);
        Failures.Add(failure);
        return failure;
    }

    private static string? Normalize(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

public sealed class MaintenanceAssetFailure
{
    private MaintenanceAssetFailure() { }

    public Guid FailureId { get; private set; }
    public Guid AssetId { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public DateTime ReportedAtUtc { get; private set; }
    public Guid? ReportedBy { get; private set; }

    public static MaintenanceAssetFailure Create(Guid assetId, string description, DateTime reportedAtUtc, Guid? reportedBy)
    {
        if (assetId == Guid.Empty) throw new ArgumentException("MAINTENANCE_ASSET_REQUIRED");
        if (string.IsNullOrWhiteSpace(description)) throw new ArgumentException("MAINTENANCE_ASSET_FAILURE_DESCRIPTION_REQUIRED");

        return new MaintenanceAssetFailure
        {
            FailureId = Foundation.Identifiers.Uuid7.NewGuid(),
            AssetId = assetId,
            Description = description.Trim(),
            ReportedAtUtc = reportedAtUtc,
            ReportedBy = reportedBy
        };
    }
}

public sealed class PreventiveMaintenancePlan
{
    private PreventiveMaintenancePlan() { }

    public Guid PlanId { get; private set; }
    public Guid? PropertyId { get; private set; }
    public string PlanName { get; private set; } = string.Empty;
    public string Category { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public Guid? AssetId { get; private set; }
    public int IntervalDays { get; private set; }
    public int EstimatedMinutes { get; private set; }
    public bool IsActive { get; private set; }
    public ICollection<PreventiveChecklistItem> Checklist { get; private set; } = new List<PreventiveChecklistItem>();

    public static PreventiveMaintenancePlan Create(
        Guid? propertyId,
        string planName,
        string category,
        string description,
        Guid? assetId,
        int intervalDays,
        int estimatedMinutes)
    {
        if (propertyId is Guid property && property == Guid.Empty) throw new ArgumentException("MAINTENANCE_PLAN_PROPERTY_INVALID");
        if (string.IsNullOrWhiteSpace(planName)) throw new ArgumentException("MAINTENANCE_PLAN_NAME_REQUIRED");
        if (string.IsNullOrWhiteSpace(category)) throw new ArgumentException("MAINTENANCE_PLAN_CATEGORY_REQUIRED");
        if (intervalDays <= 0) throw new ArgumentException("MAINTENANCE_PLAN_INTERVAL_INVALID");
        if (estimatedMinutes < 0) throw new ArgumentException("MAINTENANCE_PLAN_ESTIMATE_INVALID");

        return new PreventiveMaintenancePlan
        {
            PlanId = Foundation.Identifiers.Uuid7.NewGuid(),
            PropertyId = propertyId,
            PlanName = planName.Trim(),
            Category = category.Trim(),
            Description = description?.Trim() ?? string.Empty,
            AssetId = assetId,
            IntervalDays = intervalDays,
            EstimatedMinutes = estimatedMinutes,
            IsActive = true
        };
    }

    public PreventiveChecklistItem AddChecklistItem(string code, string description, string category, bool isMandatory, string? reference)
    {
        var item = PreventiveChecklistItem.Create(PlanId, code, description, category, isMandatory, reference);
        Checklist.Add(item);
        return item;
    }
}

public sealed class PreventiveChecklistItem
{
    private PreventiveChecklistItem() { }

    public Guid ChecklistItemId { get; private set; }
    public Guid PlanId { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public string Category { get; private set; } = string.Empty;
    public bool IsMandatory { get; private set; }
    public string? Reference { get; private set; }

    public static PreventiveChecklistItem Create(Guid planId, string code, string description, string category, bool isMandatory, string? reference)
    {
        if (planId == Guid.Empty) throw new ArgumentException("MAINTENANCE_PLAN_REQUIRED");
        if (string.IsNullOrWhiteSpace(code)) throw new ArgumentException("MAINTENANCE_CHECKLIST_CODE_REQUIRED");
        if (string.IsNullOrWhiteSpace(description)) throw new ArgumentException("MAINTENANCE_CHECKLIST_DESCRIPTION_REQUIRED");
        if (string.IsNullOrWhiteSpace(category)) throw new ArgumentException("MAINTENANCE_CHECKLIST_CATEGORY_REQUIRED");

        return new PreventiveChecklistItem
        {
            ChecklistItemId = Foundation.Identifiers.Uuid7.NewGuid(),
            PlanId = planId,
            Code = code.Trim(),
            Description = description.Trim(),
            Category = category.Trim(),
            IsMandatory = isMandatory,
            Reference = string.IsNullOrWhiteSpace(reference) ? null : reference.Trim()
        };
    }
}

public sealed class PreventiveMaintenanceSchedule
{
    private PreventiveMaintenanceSchedule() { }

    public Guid ScheduleId { get; private set; }
    public Guid PropertyId { get; private set; }
    public Guid PlanId { get; private set; }
    public Guid? AssetId { get; private set; }
    public Guid? RoomId { get; private set; }
    public DateTime ScheduledAtUtc { get; private set; }
    public DateTime? CompletedAtUtc { get; private set; }
    public Guid? WorkOrderId { get; private set; }
    public byte[]? RowVersion { get; private set; }

    public static PreventiveMaintenanceSchedule Create(
        Guid propertyId,
        Guid planId,
        Guid? assetId,
        Guid? roomId,
        DateTime scheduledAtUtc)
    {
        if (propertyId == Guid.Empty) throw new ArgumentException("MAINTENANCE_SCHEDULE_PROPERTY_REQUIRED");
        if (planId == Guid.Empty) throw new ArgumentException("MAINTENANCE_PLAN_REQUIRED");
        if (assetId is null && roomId is null) throw new ArgumentException("MAINTENANCE_SCHEDULE_TARGET_REQUIRED");

        return new PreventiveMaintenanceSchedule
        {
            ScheduleId = Foundation.Identifiers.Uuid7.NewGuid(),
            PropertyId = propertyId,
            PlanId = planId,
            AssetId = assetId,
            RoomId = roomId,
            ScheduledAtUtc = scheduledAtUtc
        };
    }
}
