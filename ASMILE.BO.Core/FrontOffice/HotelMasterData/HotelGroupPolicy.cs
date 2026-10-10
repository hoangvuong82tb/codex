using ASMILE.BO.Core.FrontOffice.Foundation.Identifiers;

namespace ASMILE.BO.Core.FrontOffice.HotelMasterData;

/// <summary>Property-scoped defaults for group/allotment commercial workflows.</summary>
public sealed class HotelGroupPolicy
{
    private HotelGroupPolicy() { }
    public Guid HotelGroupPolicyId { get; private set; }
    public Guid PropertyId { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public DateTime EffectiveFrom { get; private set; }
    public DateTime EffectiveTo { get; private set; }
    public int DefaultCutoffDays { get; private set; }
    public int RoomReleaseDaysBeforeArrival { get; private set; }
    public decimal DepositPercent { get; private set; }
    public int DepositDueDaysBeforeArrival { get; private set; }
    public bool DepositRequired { get; private set; }
    public bool AllowRoomingListImport { get; private set; }
    public string DefaultRouting { get; private set; } = string.Empty;
    public string? DefaultBillingInstruction { get; private set; }
    public string? Description { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public Guid? CreatedBy { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }
    public Guid? UpdatedBy { get; private set; }
    public byte[]? RowVersion { get; private set; }
    public static HotelGroupPolicy Create(PropertyId propertyId, string code, string name, DateTime effectiveFrom, DateTime effectiveTo, int defaultCutoffDays, int roomReleaseDaysBeforeArrival, decimal depositPercent, int depositDueDaysBeforeArrival, bool depositRequired, bool allowRoomingListImport, string defaultRouting, string? defaultBillingInstruction, string? description, DateTime nowUtc, ActorId? actorId)
    { Validate(code, name, effectiveFrom, effectiveTo, defaultCutoffDays, roomReleaseDaysBeforeArrival, depositPercent, depositDueDaysBeforeArrival, defaultRouting); return new HotelGroupPolicy { HotelGroupPolicyId = Uuid7.NewGuid(), PropertyId = propertyId.Value, Code = code.Trim(), Name = name.Trim(), EffectiveFrom = effectiveFrom.Date, EffectiveTo = effectiveTo.Date, DefaultCutoffDays = defaultCutoffDays, RoomReleaseDaysBeforeArrival = roomReleaseDaysBeforeArrival, DepositPercent = depositPercent, DepositDueDaysBeforeArrival = depositDueDaysBeforeArrival, DepositRequired = depositRequired, AllowRoomingListImport = allowRoomingListImport, DefaultRouting = defaultRouting.Trim(), DefaultBillingInstruction = Normalize(defaultBillingInstruction), Description = Normalize(description), IsActive = true, CreatedAtUtc = nowUtc, UpdatedAtUtc = nowUtc, CreatedBy = actorId?.Value, UpdatedBy = actorId?.Value }; }
    public void Update(string code, string name, DateTime effectiveFrom, DateTime effectiveTo, int defaultCutoffDays, int roomReleaseDaysBeforeArrival, decimal depositPercent, int depositDueDaysBeforeArrival, bool depositRequired, bool allowRoomingListImport, string defaultRouting, string? defaultBillingInstruction, string? description, DateTime nowUtc, ActorId? actorId)
    { Validate(code, name, effectiveFrom, effectiveTo, defaultCutoffDays, roomReleaseDaysBeforeArrival, depositPercent, depositDueDaysBeforeArrival, defaultRouting); Code = code.Trim(); Name = name.Trim(); EffectiveFrom = effectiveFrom.Date; EffectiveTo = effectiveTo.Date; DefaultCutoffDays = defaultCutoffDays; RoomReleaseDaysBeforeArrival = roomReleaseDaysBeforeArrival; DepositPercent = depositPercent; DepositDueDaysBeforeArrival = depositDueDaysBeforeArrival; DepositRequired = depositRequired; AllowRoomingListImport = allowRoomingListImport; DefaultRouting = defaultRouting.Trim(); DefaultBillingInstruction = Normalize(defaultBillingInstruction); Description = Normalize(description); Touch(nowUtc, actorId); }
    public void Activate(DateTime nowUtc, ActorId? actorId) { IsActive = true; Touch(nowUtc, actorId); }
    public void Deactivate(DateTime nowUtc, ActorId? actorId) { IsActive = false; Touch(nowUtc, actorId); }
    private void Touch(DateTime nowUtc, ActorId? actorId) { UpdatedAtUtc = nowUtc; UpdatedBy = actorId?.Value; }
    private static void Validate(string code, string name, DateTime from, DateTime to, int cutoff, int release, decimal deposit, int dueDays, string routing)
    { if (string.IsNullOrWhiteSpace(code)) throw new ArgumentException("HOTEL_GROUP_POLICY_CODE_REQUIRED"); if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("HOTEL_GROUP_POLICY_NAME_REQUIRED"); if (to.Date < from.Date) throw new ArgumentException("HOTEL_GROUP_POLICY_EFFECTIVE_RANGE_INVALID"); if (cutoff < 0 || release < 0 || dueDays < 0 || deposit is < 0 or > 100) throw new ArgumentException("HOTEL_GROUP_POLICY_VALUES_INVALID"); if (string.IsNullOrWhiteSpace(routing)) throw new ArgumentException("HOTEL_GROUP_POLICY_ROUTING_REQUIRED"); }
    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
