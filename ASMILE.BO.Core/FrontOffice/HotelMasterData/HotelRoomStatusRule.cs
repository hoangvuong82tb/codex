using ASMILE.BO.Core.FrontOffice.Foundation.Identifiers;

namespace ASMILE.BO.Core.FrontOffice.HotelMasterData;

/// <summary>
/// Property-scoped rule for room status transitions.  The operational workflow
/// still owns the execution; this aggregate owns the hotel's configurable
/// policy instead of scattering status rules through screens or seed data.
/// </summary>
public sealed class HotelRoomStatusRule
{
    private static readonly HashSet<string> Domains = ["FO", "HK", "OOS"];
    private HotelRoomStatusRule() { }

    public Guid HotelRoomStatusRuleId { get; private set; }
    public Guid PropertyId { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string StatusDomain { get; private set; } = "HK";
    public string FromStatus { get; private set; } = string.Empty;
    public string ToStatus { get; private set; } = string.Empty;
    public string? RequiredRole { get; private set; }
    public string? RequiredEvent { get; private set; }
    public bool RequiresApproval { get; private set; }
    public string? Description { get; private set; }
    public int DisplayOrder { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public Guid? CreatedBy { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }
    public Guid? UpdatedBy { get; private set; }
    public byte[]? RowVersion { get; private set; }

    public static HotelRoomStatusRule Create(PropertyId propertyId, string code, string name, string statusDomain,
        string fromStatus, string toStatus, string? requiredRole, string? requiredEvent,
        bool requiresApproval, string? description, int displayOrder, DateTime nowUtc, ActorId? actorId)
    {
        Validate(code, name, statusDomain, fromStatus, toStatus);
        return new HotelRoomStatusRule
        {
            HotelRoomStatusRuleId = Uuid7.NewGuid(), PropertyId = propertyId.Value,
            Code = code.Trim(), Name = name.Trim(), StatusDomain = statusDomain.Trim().ToUpperInvariant(),
            FromStatus = fromStatus.Trim(), ToStatus = toStatus.Trim(), RequiredRole = Normalize(requiredRole),
            RequiredEvent = Normalize(requiredEvent), RequiresApproval = requiresApproval, Description = Normalize(description),
            DisplayOrder = displayOrder, IsActive = true, CreatedAtUtc = nowUtc, UpdatedAtUtc = nowUtc,
            CreatedBy = actorId?.Value, UpdatedBy = actorId?.Value
        };
    }

    public void Update(string code, string name, string statusDomain, string fromStatus, string toStatus,
        string? requiredRole, string? requiredEvent, bool requiresApproval, string? description,
        int displayOrder, DateTime nowUtc, ActorId? actorId)
    {
        Validate(code, name, statusDomain, fromStatus, toStatus);
        Code = code.Trim(); Name = name.Trim(); StatusDomain = statusDomain.Trim().ToUpperInvariant();
        FromStatus = fromStatus.Trim(); ToStatus = toStatus.Trim(); RequiredRole = Normalize(requiredRole);
        RequiredEvent = Normalize(requiredEvent); RequiresApproval = requiresApproval; Description = Normalize(description);
        DisplayOrder = displayOrder; Touch(nowUtc, actorId);
    }

    public void Activate(DateTime nowUtc, ActorId? actorId) { IsActive = true; Touch(nowUtc, actorId); }
    public void Deactivate(DateTime nowUtc, ActorId? actorId) { IsActive = false; Touch(nowUtc, actorId); }

    private void Touch(DateTime nowUtc, ActorId? actorId) { UpdatedAtUtc = nowUtc; UpdatedBy = actorId?.Value; }
    private static void Validate(string code, string name, string domain, string from, string to)
    {
        if (string.IsNullOrWhiteSpace(code)) throw new ArgumentException("HOTEL_ROOM_STATUS_RULE_CODE_REQUIRED");
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("HOTEL_ROOM_STATUS_RULE_NAME_REQUIRED");
        if (!Domains.Contains(domain.Trim().ToUpperInvariant())) throw new ArgumentException("HOTEL_ROOM_STATUS_RULE_DOMAIN_INVALID");
        if (string.IsNullOrWhiteSpace(from)) throw new ArgumentException("HOTEL_ROOM_STATUS_RULE_FROM_REQUIRED");
        if (string.IsNullOrWhiteSpace(to)) throw new ArgumentException("HOTEL_ROOM_STATUS_RULE_TO_REQUIRED");
        if (string.Equals(from.Trim(), to.Trim(), StringComparison.OrdinalIgnoreCase)) throw new ArgumentException("HOTEL_ROOM_STATUS_RULE_SAME_STATUS");
    }
    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
