using ASMILE.BO.Core.FrontOffice.Foundation.Identifiers;

namespace ASMILE.BO.Core.FrontOffice.HotelMasterData;

public sealed class HotelGuestRequestProfile
{
    private HotelGuestRequestProfile() { }
    public Guid HotelGuestRequestProfileId { get; private set; }
    public Guid PropertyId { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string RequestType { get; private set; } = string.Empty;
    public int Priority { get; private set; }
    public string Department { get; private set; } = string.Empty;
    public int SlaMinutes { get; private set; }
    public string? AutoAssignment { get; private set; }
    public string? NotificationChannels { get; private set; }
    public DateTime EffectiveFrom { get; private set; }
    public DateTime EffectiveTo { get; private set; }
    public string? Description { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public Guid? CreatedBy { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }
    public Guid? UpdatedBy { get; private set; }
    public byte[]? RowVersion { get; private set; }

    public static HotelGuestRequestProfile Create(PropertyId propertyId, string code, string name, string requestType, int priority, string department, int slaMinutes, string? autoAssignment, string? notificationChannels, DateTime from, DateTime to, string? description, DateTime nowUtc, ActorId? actorId)
    { Validate(code, name, requestType, priority, department, slaMinutes, from, to); return new() { HotelGuestRequestProfileId = Uuid7.NewGuid(), PropertyId = propertyId.Value, Code = CodeValue(code), Name = name.Trim(), RequestType = requestType.Trim(), Priority = priority, Department = department.Trim(), SlaMinutes = slaMinutes, AutoAssignment = Normalize(autoAssignment), NotificationChannels = Normalize(notificationChannels), EffectiveFrom = from.Date, EffectiveTo = to.Date, Description = Normalize(description), IsActive = true, CreatedAtUtc = nowUtc, CreatedBy = actorId?.Value, UpdatedAtUtc = nowUtc, UpdatedBy = actorId?.Value }; }
    public void Update(string code, string name, string requestType, int priority, string department, int slaMinutes, string? autoAssignment, string? notificationChannels, DateTime from, DateTime to, string? description, DateTime nowUtc, ActorId? actorId)
    { Validate(code, name, requestType, priority, department, slaMinutes, from, to); Code = CodeValue(code); Name = name.Trim(); RequestType = requestType.Trim(); Priority = priority; Department = department.Trim(); SlaMinutes = slaMinutes; AutoAssignment = Normalize(autoAssignment); NotificationChannels = Normalize(notificationChannels); EffectiveFrom = from.Date; EffectiveTo = to.Date; Description = Normalize(description); Touch(nowUtc, actorId); }
    public void Activate(DateTime nowUtc, ActorId? actorId) { IsActive = true; Touch(nowUtc, actorId); }
    public void Deactivate(DateTime nowUtc, ActorId? actorId) { IsActive = false; Touch(nowUtc, actorId); }
    private static void Validate(string code, string name, string requestType, int priority, string department, int slaMinutes, DateTime from, DateTime to) { if (string.IsNullOrWhiteSpace(code)) throw new ArgumentException("HOTEL_GUEST_REQUEST_CODE_REQUIRED"); if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("HOTEL_GUEST_REQUEST_NAME_REQUIRED"); if (string.IsNullOrWhiteSpace(requestType)) throw new ArgumentException("HOTEL_GUEST_REQUEST_TYPE_REQUIRED"); if (priority < 0) throw new ArgumentException("HOTEL_GUEST_REQUEST_PRIORITY_INVALID"); if (string.IsNullOrWhiteSpace(department)) throw new ArgumentException("HOTEL_GUEST_REQUEST_DEPARTMENT_REQUIRED"); if (slaMinutes < 0) throw new ArgumentException("HOTEL_GUEST_REQUEST_SLA_INVALID"); if (to.Date < from.Date) throw new ArgumentException("HOTEL_GUEST_REQUEST_EFFECTIVE_RANGE_INVALID"); }
    private static string CodeValue(string value) => value.Trim().ToUpperInvariant(); private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim(); private void Touch(DateTime nowUtc, ActorId? actorId) { UpdatedAtUtc = nowUtc; UpdatedBy = actorId?.Value; }
}
