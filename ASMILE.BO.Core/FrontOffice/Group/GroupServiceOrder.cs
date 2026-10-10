using ASMILE.BO.Core.FrontOffice.Foundation.Identifiers;

namespace ASMILE.BO.Core.FrontOffice.Group;

public enum GroupServiceOrderStatus
{
    Pending,
    Confirmed,
    InProgress,
    Completed,
    Cancelled,
    PostedToFolio
}

/// <summary>
/// Persisted operational order for a service delivered to a group.
/// Folio posting remains a separate operation; this aggregate records the
/// service commitment and its commercial values.
/// </summary>
public sealed class GroupServiceOrder
{
    private GroupServiceOrder() { }

    public Guid GroupServiceOrderId { get; private set; }
    public Guid PropertyId { get; private set; }
    public Guid GroupReservationId { get; private set; }
    public GroupReservation? GroupReservation { get; private set; }
    public string ServiceCode { get; private set; } = string.Empty;
    public string ServiceName { get; private set; } = string.Empty;
    public string Category { get; private set; } = string.Empty;
    public string? SubCategory { get; private set; }
    public string? Department { get; private set; }
    public string? Provider { get; private set; }
    public string? Location { get; private set; }
    public DateOnly ServiceDate { get; private set; }
    public TimeOnly ServiceTime { get; private set; }
    public int DurationMinutes { get; private set; }
    public int Quantity { get; private set; }
    public decimal UnitPrice { get; private set; }
    public decimal DiscountRate { get; private set; }
    public decimal TaxRate { get; private set; }
    public decimal ServiceChargeRate { get; private set; }
    public decimal PaidAmount { get; private set; }
    public GroupServiceOrderStatus Status { get; private set; }
    public string ChargeTo { get; private set; } = "MasterFolio";
    public string? GuestName { get; private set; }
    public string? Remark { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public Guid? CreatedBy { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }
    public Guid? UpdatedBy { get; private set; }
    public byte[]? RowVersion { get; private set; }

    public decimal SubTotal => Quantity * UnitPrice;
    public decimal DiscountAmount => SubTotal * DiscountRate;
    public decimal TaxAmount => (SubTotal - DiscountAmount) * TaxRate;
    public decimal ServiceChargeAmount => (SubTotal - DiscountAmount) * ServiceChargeRate;
    public decimal TotalAmount => SubTotal - DiscountAmount + TaxAmount + ServiceChargeAmount;
    public decimal Balance => TotalAmount - PaidAmount;

    public static GroupServiceOrder Create(
        Guid propertyId, Guid groupReservationId, string serviceCode,
        string serviceName, string category, DateOnly serviceDate,
        TimeOnly serviceTime, int durationMinutes, int quantity, decimal unitPrice,
        decimal discountRate, decimal taxRate, decimal serviceChargeRate,
        string chargeTo, DateTime nowUtc, ActorId? actorId,
        string? subCategory = null, string? department = null,
        string? provider = null, string? location = null,
        string? guestName = null, string? remark = null)
    {
        if (propertyId == Guid.Empty) throw new ArgumentException("GROUP_SERVICE_PROPERTY_REQUIRED");
        if (groupReservationId == Guid.Empty) throw new ArgumentException("GROUP_SERVICE_GROUP_REQUIRED");
        if (string.IsNullOrWhiteSpace(serviceCode)) throw new ArgumentException("GROUP_SERVICE_CODE_REQUIRED");
        if (string.IsNullOrWhiteSpace(serviceName)) throw new ArgumentException("GROUP_SERVICE_NAME_REQUIRED");
        if (string.IsNullOrWhiteSpace(category)) throw new ArgumentException("GROUP_SERVICE_CATEGORY_REQUIRED");
        if (serviceDate == default) throw new ArgumentException("GROUP_SERVICE_DATE_REQUIRED");
        if (durationMinutes < 0) throw new ArgumentException("GROUP_SERVICE_DURATION_INVALID");
        if (quantity <= 0) throw new ArgumentException("GROUP_SERVICE_QUANTITY_INVALID");
        if (unitPrice < 0) throw new ArgumentException("GROUP_SERVICE_UNIT_PRICE_INVALID");
        ValidateRate(discountRate, "GROUP_SERVICE_DISCOUNT_INVALID");
        ValidateRate(taxRate, "GROUP_SERVICE_TAX_INVALID");
        ValidateRate(serviceChargeRate, "GROUP_SERVICE_CHARGE_INVALID");
        if (string.IsNullOrWhiteSpace(chargeTo)) throw new ArgumentException("GROUP_SERVICE_CHARGE_TO_REQUIRED");

        return new GroupServiceOrder
        {
            GroupServiceOrderId = Uuid7.NewGuid(), PropertyId = propertyId,
            GroupReservationId = groupReservationId, ServiceCode = serviceCode.Trim(),
            ServiceName = serviceName.Trim(), Category = category.Trim(),
            SubCategory = Normalize(subCategory), Department = Normalize(department),
            Provider = Normalize(provider), Location = Normalize(location),
            ServiceDate = serviceDate, ServiceTime = serviceTime,
            DurationMinutes = durationMinutes, Quantity = quantity, UnitPrice = unitPrice,
            DiscountRate = discountRate, TaxRate = taxRate,
            ServiceChargeRate = serviceChargeRate, Status = GroupServiceOrderStatus.Pending,
            ChargeTo = chargeTo.Trim(), GuestName = Normalize(guestName),
            Remark = Normalize(remark), CreatedAtUtc = nowUtc,
            CreatedBy = actorId?.Value, UpdatedAtUtc = nowUtc, UpdatedBy = actorId?.Value
        };
    }

    public void UpdateDetails(
        string serviceName, string category, DateOnly serviceDate, TimeOnly serviceTime,
        int durationMinutes, int quantity, decimal unitPrice, decimal discountRate,
        decimal taxRate, decimal serviceChargeRate, string chargeTo, DateTime nowUtc,
        ActorId? actorId, string? subCategory = null, string? department = null,
        string? provider = null, string? location = null, string? guestName = null,
        string? remark = null)
    {
        if (Status is not GroupServiceOrderStatus.Pending)
            throw new InvalidOperationException("GROUP_SERVICE_EDIT_REQUIRES_PENDING");
        if (PaidAmount != 0m)
            throw new InvalidOperationException("GROUP_SERVICE_EDIT_HAS_PAYMENT");
        if (string.IsNullOrWhiteSpace(serviceName)) throw new ArgumentException("GROUP_SERVICE_NAME_REQUIRED");
        if (string.IsNullOrWhiteSpace(category)) throw new ArgumentException("GROUP_SERVICE_CATEGORY_REQUIRED");
        if (serviceDate == default) throw new ArgumentException("GROUP_SERVICE_DATE_REQUIRED");
        if (durationMinutes < 0) throw new ArgumentException("GROUP_SERVICE_DURATION_INVALID");
        if (quantity <= 0) throw new ArgumentException("GROUP_SERVICE_QUANTITY_INVALID");
        if (unitPrice < 0) throw new ArgumentException("GROUP_SERVICE_UNIT_PRICE_INVALID");
        ValidateRate(discountRate, "GROUP_SERVICE_DISCOUNT_INVALID");
        ValidateRate(taxRate, "GROUP_SERVICE_TAX_INVALID");
        ValidateRate(serviceChargeRate, "GROUP_SERVICE_CHARGE_INVALID");
        if (string.IsNullOrWhiteSpace(chargeTo)) throw new ArgumentException("GROUP_SERVICE_CHARGE_TO_REQUIRED");

        ServiceName = serviceName.Trim();
        Category = category.Trim();
        SubCategory = Normalize(subCategory);
        Department = Normalize(department);
        Provider = Normalize(provider);
        Location = Normalize(location);
        ServiceDate = serviceDate;
        ServiceTime = serviceTime;
        DurationMinutes = durationMinutes;
        Quantity = quantity;
        UnitPrice = unitPrice;
        DiscountRate = discountRate;
        TaxRate = taxRate;
        ServiceChargeRate = serviceChargeRate;
        ChargeTo = chargeTo.Trim();
        GuestName = Normalize(guestName);
        Remark = Normalize(remark);
        Touch(nowUtc, actorId);
    }

    public void Confirm(DateTime nowUtc, ActorId? actorId)
    {
        RequireStatus(GroupServiceOrderStatus.Pending);
        Status = GroupServiceOrderStatus.Confirmed;
        Touch(nowUtc, actorId);
    }

    public void Start(DateTime nowUtc, ActorId? actorId)
    {
        RequireStatus(GroupServiceOrderStatus.Confirmed);
        Status = GroupServiceOrderStatus.InProgress;
        Touch(nowUtc, actorId);
    }

    public void Complete(DateTime nowUtc, ActorId? actorId)
    {
        RequireStatus(GroupServiceOrderStatus.Confirmed, GroupServiceOrderStatus.InProgress);
        Status = GroupServiceOrderStatus.Completed;
        Touch(nowUtc, actorId);
    }

    public void MarkPostedToFolio(DateTime nowUtc, ActorId? actorId)
    {
        RequireStatus(GroupServiceOrderStatus.Completed);
        Status = GroupServiceOrderStatus.PostedToFolio;
        Touch(nowUtc, actorId);
    }

    public void Cancel(string reason, DateTime nowUtc, ActorId? actorId)
    {
        if (string.IsNullOrWhiteSpace(reason)) throw new ArgumentException("GROUP_SERVICE_CANCEL_REASON_REQUIRED");
        if (Status is GroupServiceOrderStatus.Completed or GroupServiceOrderStatus.PostedToFolio)
            throw new InvalidOperationException("GROUP_SERVICE_STATUS_NOT_CANCELLABLE");
        Status = GroupServiceOrderStatus.Cancelled;
        Remark = string.IsNullOrWhiteSpace(Remark) ? $"Hủy: {reason.Trim()}" : $"{Remark}\nHủy: {reason.Trim()}";
        Touch(nowUtc, actorId);
    }

    private void RequireStatus(params GroupServiceOrderStatus[] allowed)
    {
        if (!allowed.Contains(Status))
            throw new InvalidOperationException($"GROUP_SERVICE_STATUS_{Status.ToString().ToUpperInvariant()}");
    }

    private void Touch(DateTime nowUtc, ActorId? actorId)
    {
        UpdatedAtUtc = nowUtc;
        UpdatedBy = actorId?.Value;
    }

    private static void ValidateRate(decimal value, string error)
    {
        if (value < 0m || value > 1m) throw new ArgumentException(error);
    }

    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
