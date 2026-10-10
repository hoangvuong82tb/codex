using ASMILE.BO.Application.FrontOffice.Foundation.Cqrs;
using ASMILE.BO.Application.FrontOffice.HotelMasterData;
using ASMILE.BO.Core.FrontOffice.HotelMasterData;
using ASMILE.BO.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ASMILE.BO.Infrastructure.FrontOffice.HotelMasterData;

public sealed class EfHotelStructureMasterDataRepository(AppDbContext dbContext) : IHotelStructureMasterDataRepository
{
    public Task AddBuildingAsync(HotelBuilding building, CancellationToken ct = default) { dbContext.HotelBuildings.Add(building); return Task.CompletedTask; }
    public Task AddFloorAsync(HotelFloor floor, CancellationToken ct = default) { dbContext.HotelFloors.Add(floor); return Task.CompletedTask; }
    public Task AddAreaAsync(HotelArea area, CancellationToken ct = default) { dbContext.HotelAreas.Add(area); return Task.CompletedTask; }
    public Task AddCatalogItemAsync(HotelReferenceCatalogItem item, CancellationToken ct = default) { dbContext.HotelReferenceCatalogItems.Add(item); return Task.CompletedTask; }
    public Task AddRoomStatusRuleAsync(HotelRoomStatusRule rule, CancellationToken ct = default) { dbContext.HotelRoomStatusRules.Add(rule); return Task.CompletedTask; }
    public Task AddPropertyProfileAsync(HotelPropertyProfile profile, CancellationToken ct = default) { dbContext.HotelPropertyProfiles.Add(profile); return Task.CompletedTask; }
    public Task AddStayPolicyAsync(HotelStayPolicy policy, CancellationToken ct = default) { dbContext.HotelStayPolicies.Add(policy); return Task.CompletedTask; }
    public Task AddCheckInOutRuleAsync(HotelCheckInOutRule rule, CancellationToken ct = default) { dbContext.HotelCheckInOutRules.Add(rule); return Task.CompletedTask; }
    public Task AddRoomMoveUpgradeRuleAsync(HotelRoomMoveUpgradeRule rule, CancellationToken ct = default) { dbContext.HotelRoomMoveUpgradeRules.Add(rule); return Task.CompletedTask; }
    public Task AddPropertyDocumentAsync(HotelPropertyDocument document, CancellationToken ct = default) { dbContext.HotelPropertyDocuments.Add(document); return Task.CompletedTask; }
    public Task AddPromotionPackageAsync(HotelPromotionPackage package, CancellationToken ct = default) { dbContext.HotelPromotionPackages.Add(package); return Task.CompletedTask; }
    public Task AddGroupPolicyAsync(HotelGroupPolicy policy, CancellationToken ct = default) { dbContext.HotelGroupPolicies.Add(policy); return Task.CompletedTask; }
    public Task AddCashierShiftPolicyAsync(HotelCashierShiftPolicy policy, CancellationToken ct = default) { dbContext.HotelCashierShiftPolicies.Add(policy); return Task.CompletedTask; }
    public Task AddNightAuditPolicyAsync(HotelNightAuditPolicy policy, CancellationToken ct = default) { dbContext.HotelNightAuditPolicies.Add(policy); return Task.CompletedTask; }
    public Task AddApprovalMatrixRuleAsync(HotelApprovalMatrixRule rule, CancellationToken ct = default) { dbContext.HotelApprovalMatrixRules.Add(rule); return Task.CompletedTask; }
    public Task AddFOPermissionPolicyAsync(HotelFOPermissionPolicy policy, CancellationToken ct = default) { dbContext.HotelFOPermissionPolicies.Add(policy); return Task.CompletedTask; }
    public Task AddFormPrinterProfileAsync(HotelFormPrinterProfile profile, CancellationToken ct = default) { dbContext.HotelFormPrinterProfiles.Add(profile); return Task.CompletedTask; }
    public Task AddMessagingIntegrationProfileAsync(HotelMessagingIntegrationProfile profile, CancellationToken ct = default) { dbContext.HotelMessagingIntegrationProfiles.Add(profile); return Task.CompletedTask; }
    public Task AddPostedCorrectionPolicyAsync(HotelPostedCorrectionPolicy policy, CancellationToken ct = default) { dbContext.HotelPostedCorrectionPolicies.Add(policy); return Task.CompletedTask; }
    public Task AddBusinessRuleProfileAsync(HotelBusinessRuleProfile profile, CancellationToken ct = default) { dbContext.HotelBusinessRuleProfiles.Add(profile); return Task.CompletedTask; }
    public Task AddTaxServiceChargeRuleAsync(HotelTaxServiceChargeRule rule, CancellationToken ct = default) { dbContext.HotelTaxServiceChargeRules.Add(rule); return Task.CompletedTask; }
    public Task AddPaymentMethodProfileAsync(HotelPaymentMethodProfile profile, CancellationToken ct = default) { dbContext.HotelPaymentMethodProfiles.Add(profile); return Task.CompletedTask; }
    public Task AddDepositPolicyAsync(HotelDepositPolicy policy, CancellationToken ct = default) { dbContext.HotelDepositPolicies.Add(policy); return Task.CompletedTask; }
    public Task AddDiscountPolicyAsync(HotelDiscountPolicy policy, CancellationToken ct = default) { dbContext.HotelDiscountPolicies.Add(policy); return Task.CompletedTask; }
    public Task AddChargeCodeProfileAsync(HotelChargeCodeProfile profile, CancellationToken ct = default) { dbContext.HotelChargeCodeProfiles.Add(profile); return Task.CompletedTask; }
    public Task AddFolioProfileAsync(HotelFolioProfile profile, CancellationToken ct = default) { dbContext.HotelFolioProfiles.Add(profile); return Task.CompletedTask; }
    public Task AddGuestTypeProfileAsync(HotelGuestTypeProfile profile, CancellationToken ct = default) { dbContext.HotelGuestTypeProfiles.Add(profile); return Task.CompletedTask; }
    public Task AddProfileTypeProfileAsync(HotelProfileTypeProfile profile, CancellationToken ct = default) { dbContext.HotelProfileTypeProfiles.Add(profile); return Task.CompletedTask; }
    public Task AddVipLevelProfileAsync(HotelVipLevelProfile profile, CancellationToken ct = default) { dbContext.HotelVipLevelProfiles.Add(profile); return Task.CompletedTask; }
    public Task AddGuestPreferenceProfileAsync(HotelGuestPreferenceProfile profile, CancellationToken ct = default) { dbContext.HotelGuestPreferenceProfiles.Add(profile); return Task.CompletedTask; }
    public Task AddGuestRequestProfileAsync(HotelGuestRequestProfile profile, CancellationToken ct = default) { dbContext.HotelGuestRequestProfiles.Add(profile); return Task.CompletedTask; }
    public Task AddReservationTypeProfileAsync(HotelReservationTypeProfile profile, CancellationToken ct = default) { dbContext.HotelReservationTypeProfiles.Add(profile); return Task.CompletedTask; }
    public Task AddBookingSourceProfileAsync(HotelBookingSourceProfile profile, CancellationToken ct = default) { dbContext.HotelBookingSourceProfiles.Add(profile); return Task.CompletedTask; }
    public Task AddBookingChannelProfileAsync(HotelBookingChannelProfile profile, CancellationToken ct = default) { dbContext.HotelBookingChannelProfiles.Add(profile); return Task.CompletedTask; }
    public Task AddMarketSegmentProfileAsync(HotelMarketSegmentProfile profile, CancellationToken ct = default) { dbContext.HotelMarketSegmentProfiles.Add(profile); return Task.CompletedTask; }
    public Task AddHousekeepingRuleProfileAsync(HotelHousekeepingRuleProfile profile, CancellationToken ct = default) { dbContext.HotelHousekeepingRuleProfiles.Add(profile); return Task.CompletedTask; }
    public Task AddWakeupRuleProfileAsync(HotelWakeupRuleProfile profile, CancellationToken ct = default) { dbContext.HotelWakeupRuleProfiles.Add(profile); return Task.CompletedTask; }
    public Task AddComplaintProfileAsync(HotelComplaintProfile profile, CancellationToken ct = default) { dbContext.HotelComplaintProfiles.Add(profile); return Task.CompletedTask; }

    public Task<HotelBuilding?> GetBuildingAsync(Guid propertyId, Guid id, CancellationToken ct = default) =>
        dbContext.HotelBuildings.SingleOrDefaultAsync(x => x.PropertyId == propertyId && x.HotelBuildingId == id, ct);

    public Task<HotelFloor?> GetFloorAsync(Guid propertyId, Guid id, CancellationToken ct = default) =>
        dbContext.HotelFloors.SingleOrDefaultAsync(x => x.PropertyId == propertyId && x.HotelFloorId == id, ct);

    public Task<HotelArea?> GetAreaAsync(Guid propertyId, Guid id, CancellationToken ct = default) =>
        dbContext.HotelAreas.SingleOrDefaultAsync(x => x.PropertyId == propertyId && x.HotelAreaId == id, ct);

    public Task<HotelReferenceCatalogItem?> GetCatalogItemAsync(Guid propertyId, Guid id, CancellationToken ct = default) =>
        dbContext.HotelReferenceCatalogItems.SingleOrDefaultAsync(x => x.PropertyId == propertyId && x.HotelReferenceCatalogItemId == id, ct);
    public Task<HotelRoomStatusRule?> GetRoomStatusRuleAsync(Guid propertyId, Guid id, CancellationToken ct = default) =>
        dbContext.HotelRoomStatusRules.SingleOrDefaultAsync(x => x.PropertyId == propertyId && x.HotelRoomStatusRuleId == id, ct);
    public async Task<IReadOnlyList<HotelRoomStatusRule>> GetActiveRoomStatusRulesAsync(Guid propertyId, string statusDomain, CancellationToken ct = default) =>
        await dbContext.HotelRoomStatusRules.AsNoTracking()
            .Where(x => x.PropertyId == propertyId && x.IsActive && x.StatusDomain == statusDomain)
            .OrderBy(x => x.DisplayOrder).ThenBy(x => x.Code)
            .ToListAsync(ct);
    public Task<HotelPropertyProfile?> GetPropertyProfileAsync(Guid propertyId, CancellationToken ct = default) =>
        dbContext.HotelPropertyProfiles.SingleOrDefaultAsync(x => x.PropertyId == propertyId, ct);
    public Task<HotelStayPolicy?> GetStayPolicyAsync(Guid propertyId, Guid id, CancellationToken ct = default) => dbContext.HotelStayPolicies.SingleOrDefaultAsync(x => x.PropertyId == propertyId && x.HotelStayPolicyId == id, ct);
    public Task<HotelCheckInOutRule?> GetCheckInOutRuleAsync(Guid propertyId, Guid id, CancellationToken ct = default) => dbContext.HotelCheckInOutRules.SingleOrDefaultAsync(x => x.PropertyId == propertyId && x.HotelCheckInOutRuleId == id, ct);
    public Task<HotelCheckInOutRule?> GetActiveCheckInOutRuleAsync(Guid propertyId, DateTime businessDate, CancellationToken ct = default) =>
        dbContext.HotelCheckInOutRules.AsNoTracking()
            .Where(x => x.PropertyId == propertyId && x.IsActive && x.EffectiveFrom <= businessDate.Date && x.EffectiveTo >= businessDate.Date)
            .OrderByDescending(x => x.EffectiveFrom).ThenBy(x => x.Code)
            .FirstOrDefaultAsync(ct);
    public Task<HotelWakeupRuleProfile?> GetActiveWakeupRuleAsync(Guid propertyId, HotelWakeupRuleKind kind, DateTime asOfUtc, CancellationToken ct = default) =>
        dbContext.HotelWakeupRuleProfiles.AsNoTracking()
            .Where(x => x.PropertyId == propertyId && x.Kind == kind && x.IsActive && x.EffectiveFrom <= asOfUtc.Date && x.EffectiveTo >= asOfUtc.Date)
            .OrderByDescending(x => x.EffectiveFrom).ThenBy(x => x.Code)
            .FirstOrDefaultAsync(ct);
    public Task<HotelComplaintProfile?> GetActiveComplaintProfileAsync(Guid propertyId, string category, DateTime asOfUtc, CancellationToken ct = default) =>
        dbContext.HotelComplaintProfiles.AsNoTracking()
            .Where(x => x.PropertyId == propertyId && x.Category == category && x.IsActive && x.EffectiveFrom <= asOfUtc.Date && x.EffectiveTo >= asOfUtc.Date)
            .OrderByDescending(x => x.EffectiveFrom).ThenByDescending(x => x.Severity).ThenBy(x => x.Code)
            .FirstOrDefaultAsync(ct);
    public Task<HotelRoomMoveUpgradeRule?> GetRoomMoveUpgradeRuleAsync(Guid propertyId, Guid id, CancellationToken ct = default) => dbContext.HotelRoomMoveUpgradeRules.SingleOrDefaultAsync(x => x.PropertyId == propertyId && x.HotelRoomMoveUpgradeRuleId == id, ct);
    public Task<HotelRoomMoveUpgradeRule?> GetActiveRoomMoveUpgradeRuleAsync(Guid propertyId, DateTime asOfUtc, CancellationToken ct = default) =>
        dbContext.HotelRoomMoveUpgradeRules.AsNoTracking()
            .Where(x => x.PropertyId == propertyId && x.IsActive && x.EffectiveFrom <= asOfUtc.Date && x.EffectiveTo >= asOfUtc.Date)
            .OrderByDescending(x => x.EffectiveFrom).ThenBy(x => x.Code)
            .FirstOrDefaultAsync(ct);
    public Task<HotelPropertyDocument?> GetPropertyDocumentAsync(Guid propertyId, Guid id, CancellationToken ct = default) => dbContext.HotelPropertyDocuments.SingleOrDefaultAsync(x => x.PropertyId == propertyId && x.HotelPropertyDocumentId == id, ct);
    public Task<HotelPromotionPackage?> GetPromotionPackageAsync(Guid propertyId, Guid id, CancellationToken ct = default) => dbContext.HotelPromotionPackages.SingleOrDefaultAsync(x => x.PropertyId == propertyId && x.HotelPromotionPackageId == id, ct);
    public Task<HotelGroupPolicy?> GetGroupPolicyAsync(Guid propertyId, Guid id, CancellationToken ct = default) => dbContext.HotelGroupPolicies.SingleOrDefaultAsync(x => x.PropertyId == propertyId && x.HotelGroupPolicyId == id, ct);
    public Task<HotelCashierShiftPolicy?> GetCashierShiftPolicyAsync(Guid propertyId, Guid id, CancellationToken ct = default) => dbContext.HotelCashierShiftPolicies.SingleOrDefaultAsync(x => x.PropertyId == propertyId && x.HotelCashierShiftPolicyId == id, ct);
    public Task<HotelNightAuditPolicy?> GetNightAuditPolicyAsync(Guid propertyId, Guid id, CancellationToken ct = default) => dbContext.HotelNightAuditPolicies.SingleOrDefaultAsync(x => x.PropertyId == propertyId && x.HotelNightAuditPolicyId == id, ct);
    public Task<HotelApprovalMatrixRule?> GetApprovalMatrixRuleAsync(Guid propertyId, Guid id, CancellationToken ct = default) => dbContext.HotelApprovalMatrixRules.SingleOrDefaultAsync(x => x.PropertyId == propertyId && x.HotelApprovalMatrixRuleId == id, ct);
    public Task<HotelFOPermissionPolicy?> GetFOPermissionPolicyAsync(Guid propertyId, Guid id, CancellationToken ct = default) => dbContext.HotelFOPermissionPolicies.SingleOrDefaultAsync(x => x.PropertyId == propertyId && x.HotelFOPermissionPolicyId == id, ct);
    public Task<HotelFormPrinterProfile?> GetFormPrinterProfileAsync(Guid propertyId, Guid id, CancellationToken ct = default) => dbContext.HotelFormPrinterProfiles.SingleOrDefaultAsync(x => x.PropertyId == propertyId && x.HotelFormPrinterProfileId == id, ct);
    public Task<HotelMessagingIntegrationProfile?> GetMessagingIntegrationProfileAsync(Guid propertyId, Guid id, CancellationToken ct = default) => dbContext.HotelMessagingIntegrationProfiles.SingleOrDefaultAsync(x => x.PropertyId == propertyId && x.HotelMessagingIntegrationProfileId == id, ct);
    public Task<HotelPostedCorrectionPolicy?> GetPostedCorrectionPolicyAsync(Guid propertyId, Guid id, CancellationToken ct = default) => dbContext.HotelPostedCorrectionPolicies.SingleOrDefaultAsync(x => x.PropertyId == propertyId && x.HotelPostedCorrectionPolicyId == id, ct);
    public Task<HotelBusinessRuleProfile?> GetBusinessRuleProfileAsync(Guid propertyId, Guid id, CancellationToken ct = default) => dbContext.HotelBusinessRuleProfiles.SingleOrDefaultAsync(x => x.PropertyId == propertyId && x.HotelBusinessRuleProfileId == id, ct);
    public Task<HotelTaxServiceChargeRule?> GetTaxServiceChargeRuleAsync(Guid propertyId, Guid id, CancellationToken ct = default) => dbContext.HotelTaxServiceChargeRules.SingleOrDefaultAsync(x => x.PropertyId == propertyId && x.HotelTaxServiceChargeRuleId == id, ct);
    public Task<HotelPaymentMethodProfile?> GetPaymentMethodProfileAsync(Guid propertyId, Guid id, CancellationToken ct = default) => dbContext.HotelPaymentMethodProfiles.SingleOrDefaultAsync(x => x.PropertyId == propertyId && x.HotelPaymentMethodProfileId == id, ct);
    public Task<HotelDepositPolicy?> GetDepositPolicyAsync(Guid propertyId, Guid id, CancellationToken ct = default) => dbContext.HotelDepositPolicies.SingleOrDefaultAsync(x => x.PropertyId == propertyId && x.HotelDepositPolicyId == id, ct);
    public Task<HotelDiscountPolicy?> GetDiscountPolicyAsync(Guid propertyId, Guid id, CancellationToken ct = default) => dbContext.HotelDiscountPolicies.SingleOrDefaultAsync(x => x.PropertyId == propertyId && x.HotelDiscountPolicyId == id, ct);
    public Task<HotelChargeCodeProfile?> GetChargeCodeProfileAsync(Guid propertyId, Guid id, CancellationToken ct = default) => dbContext.HotelChargeCodeProfiles.SingleOrDefaultAsync(x => x.PropertyId == propertyId && x.HotelChargeCodeProfileId == id, ct);
    public Task<HotelFolioProfile?> GetFolioProfileAsync(Guid propertyId, Guid id, CancellationToken ct = default) => dbContext.HotelFolioProfiles.SingleOrDefaultAsync(x => x.PropertyId == propertyId && x.HotelFolioProfileId == id, ct);
    public Task<HotelGuestTypeProfile?> GetGuestTypeProfileAsync(Guid propertyId, Guid id, CancellationToken ct = default) => dbContext.HotelGuestTypeProfiles.SingleOrDefaultAsync(x => x.PropertyId == propertyId && x.HotelGuestTypeProfileId == id, ct);
    public Task<HotelProfileTypeProfile?> GetProfileTypeProfileAsync(Guid propertyId, Guid id, CancellationToken ct = default) => dbContext.HotelProfileTypeProfiles.SingleOrDefaultAsync(x => x.PropertyId == propertyId && x.HotelProfileTypeProfileId == id, ct);
    public Task<HotelVipLevelProfile?> GetVipLevelProfileAsync(Guid propertyId, Guid id, CancellationToken ct = default) => dbContext.HotelVipLevelProfiles.SingleOrDefaultAsync(x => x.PropertyId == propertyId && x.HotelVipLevelProfileId == id, ct);
    public Task<HotelGuestPreferenceProfile?> GetGuestPreferenceProfileAsync(Guid propertyId, Guid id, CancellationToken ct = default) => dbContext.HotelGuestPreferenceProfiles.SingleOrDefaultAsync(x => x.PropertyId == propertyId && x.HotelGuestPreferenceProfileId == id, ct);
    public Task<HotelGuestRequestProfile?> GetGuestRequestProfileAsync(Guid propertyId, Guid id, CancellationToken ct = default) => dbContext.HotelGuestRequestProfiles.SingleOrDefaultAsync(x => x.PropertyId == propertyId && x.HotelGuestRequestProfileId == id, ct);
    public Task<HotelReservationTypeProfile?> GetReservationTypeProfileAsync(Guid propertyId, Guid id, CancellationToken ct = default) => dbContext.HotelReservationTypeProfiles.SingleOrDefaultAsync(x => x.PropertyId == propertyId && x.HotelReservationTypeProfileId == id, ct);
    public Task<HotelBookingSourceProfile?> GetBookingSourceProfileAsync(Guid propertyId, Guid id, CancellationToken ct = default) => dbContext.HotelBookingSourceProfiles.SingleOrDefaultAsync(x => x.PropertyId == propertyId && x.HotelBookingSourceProfileId == id, ct);
    public Task<HotelBookingChannelProfile?> GetBookingChannelProfileAsync(Guid propertyId, Guid id, CancellationToken ct = default) => dbContext.HotelBookingChannelProfiles.SingleOrDefaultAsync(x => x.PropertyId == propertyId && x.HotelBookingChannelProfileId == id, ct);
    public Task<HotelMarketSegmentProfile?> GetMarketSegmentProfileAsync(Guid propertyId, Guid id, CancellationToken ct = default) => dbContext.HotelMarketSegmentProfiles.SingleOrDefaultAsync(x => x.PropertyId == propertyId && x.HotelMarketSegmentProfileId == id, ct);
    public Task<HotelHousekeepingRuleProfile?> GetHousekeepingRuleProfileAsync(Guid propertyId, Guid id, CancellationToken ct = default) => dbContext.HotelHousekeepingRuleProfiles.SingleOrDefaultAsync(x => x.PropertyId == propertyId && x.HotelHousekeepingRuleProfileId == id, ct);
    public Task<HotelWakeupRuleProfile?> GetWakeupRuleProfileAsync(Guid propertyId, Guid id, CancellationToken ct = default) => dbContext.HotelWakeupRuleProfiles.SingleOrDefaultAsync(x => x.PropertyId == propertyId && x.HotelWakeupRuleProfileId == id, ct);
    public Task<HotelComplaintProfile?> GetComplaintProfileAsync(Guid propertyId, Guid id, CancellationToken ct = default) => dbContext.HotelComplaintProfiles.SingleOrDefaultAsync(x => x.PropertyId == propertyId && x.HotelComplaintProfileId == id, ct);

    public Task<bool> BuildingCodeExistsAsync(Guid propertyId, string code, Guid? excludingId, CancellationToken ct = default) =>
        dbContext.HotelBuildings.AnyAsync(x => x.PropertyId == propertyId && x.Code == code && (excludingId == null || x.HotelBuildingId != excludingId), ct);

    public Task<bool> FloorCodeExistsAsync(Guid propertyId, Guid buildingId, string code, Guid? excludingId, CancellationToken ct = default) =>
        dbContext.HotelFloors.AnyAsync(x => x.PropertyId == propertyId && x.HotelBuildingId == buildingId && x.Code == code && (excludingId == null || x.HotelFloorId != excludingId), ct);

    public Task<bool> BuildingExistsAsync(Guid propertyId, Guid buildingId, bool activeOnly, CancellationToken ct = default) =>
        dbContext.HotelBuildings.AnyAsync(x => x.PropertyId == propertyId && x.HotelBuildingId == buildingId && (!activeOnly || x.IsActive), ct);

    public Task<bool> AreaCodeExistsAsync(Guid propertyId, string code, Guid? excludingId, CancellationToken ct = default) =>
        dbContext.HotelAreas.AnyAsync(x => x.PropertyId == propertyId && x.Code == code && (excludingId == null || x.HotelAreaId != excludingId), ct);

    public Task<bool> FloorExistsAsync(Guid propertyId, Guid floorId, Guid? buildingId, bool activeOnly, CancellationToken ct = default) =>
        dbContext.HotelFloors.AnyAsync(x => x.PropertyId == propertyId && x.HotelFloorId == floorId && (buildingId == null || x.HotelBuildingId == buildingId) && (!activeOnly || x.IsActive), ct);

    public Task<bool> CatalogCodeExistsAsync(Guid propertyId, string catalogKey, string code, Guid? excludingId, CancellationToken ct = default) =>
        dbContext.HotelReferenceCatalogItems.AnyAsync(x => x.PropertyId == propertyId && x.CatalogKey == catalogKey && x.Code == code && (excludingId == null || x.HotelReferenceCatalogItemId != excludingId), ct);
    public Task<bool> RoomStatusRuleCodeExistsAsync(Guid propertyId, string code, Guid? excludingId, CancellationToken ct = default) =>
        dbContext.HotelRoomStatusRules.AnyAsync(x => x.PropertyId == propertyId && x.Code == code && (excludingId == null || x.HotelRoomStatusRuleId != excludingId), ct);

    public void UseExpectedRowVersion(object aggregate, byte[] expectedRowVersion) =>
        dbContext.Entry(aggregate).Property("RowVersion").OriginalValue = expectedRowVersion;

    public Task<HotelBuildingDto?> GetBuildingDtoAsync(Guid propertyId, Guid id, CancellationToken ct = default) =>
        dbContext.HotelBuildings.AsNoTracking().Where(x => x.PropertyId == propertyId && x.HotelBuildingId == id).Select(BuildingDto).SingleOrDefaultAsync(ct);

    public async Task<HotelFloorDto?> GetFloorDtoAsync(Guid propertyId, Guid id, CancellationToken ct = default)
    {
        var floor = await dbContext.HotelFloors.AsNoTracking()
            .SingleOrDefaultAsync(x => x.PropertyId == propertyId && x.HotelFloorId == id, ct);
        if (floor is null) return null;

        var building = await dbContext.HotelBuildings.AsNoTracking()
            .SingleOrDefaultAsync(x => x.PropertyId == propertyId && x.HotelBuildingId == floor.HotelBuildingId, ct);
        return building is null ? null : ToFloorDto(floor, building);
    }

    public async Task<HotelAreaDto?> GetAreaDtoAsync(Guid propertyId, Guid id, CancellationToken ct = default)
    {
        var area = await dbContext.HotelAreas.AsNoTracking()
            .SingleOrDefaultAsync(x => x.PropertyId == propertyId && x.HotelAreaId == id, ct);
        if (area is null) return null;

        var building = area.HotelBuildingId is { } buildingId
            ? await dbContext.HotelBuildings.AsNoTracking().SingleOrDefaultAsync(x => x.PropertyId == propertyId && x.HotelBuildingId == buildingId, ct)
            : null;
        var floor = area.HotelFloorId is { } floorId
            ? await dbContext.HotelFloors.AsNoTracking().SingleOrDefaultAsync(x => x.PropertyId == propertyId && x.HotelFloorId == floorId, ct)
            : null;
        return ToAreaDto(area, building, floor);
    }

    public Task<HotelReferenceCatalogItemDto?> GetCatalogItemDtoAsync(Guid propertyId, Guid id, CancellationToken ct = default) =>
        dbContext.HotelReferenceCatalogItems.AsNoTracking().Where(x => x.PropertyId == propertyId && x.HotelReferenceCatalogItemId == id).Select(CatalogItemDto).SingleOrDefaultAsync(ct);
    public Task<HotelRoomStatusRuleDto?> GetRoomStatusRuleDtoAsync(Guid propertyId, Guid id, CancellationToken ct = default) =>
        dbContext.HotelRoomStatusRules.AsNoTracking().Where(x => x.PropertyId == propertyId && x.HotelRoomStatusRuleId == id).Select(RoomStatusRuleDto).SingleOrDefaultAsync(ct);
    public Task<HotelPropertyProfileDto?> GetPropertyProfileDtoAsync(Guid propertyId, CancellationToken ct = default) =>
        dbContext.HotelPropertyProfiles.AsNoTracking().Where(x => x.PropertyId == propertyId).Select(PropertyProfileDto).SingleOrDefaultAsync(ct);
    public Task<HotelStayPolicyDto?> GetStayPolicyDtoAsync(Guid propertyId, Guid id, CancellationToken ct = default) => dbContext.HotelStayPolicies.AsNoTracking().Where(x => x.PropertyId == propertyId && x.HotelStayPolicyId == id).Select(StayPolicyDto).SingleOrDefaultAsync(ct);
    public Task<bool> StayPolicyCodeExistsAsync(Guid propertyId, string code, Guid? excludingId, CancellationToken ct = default) => dbContext.HotelStayPolicies.AnyAsync(x => x.PropertyId == propertyId && x.Code == code && (excludingId == null || x.HotelStayPolicyId != excludingId), ct);
    public Task<bool> CheckInOutRuleCodeExistsAsync(Guid propertyId, string code, Guid? excludingId, CancellationToken ct = default) => dbContext.HotelCheckInOutRules.AnyAsync(x => x.PropertyId == propertyId && x.Code == code && (excludingId == null || x.HotelCheckInOutRuleId != excludingId), ct);
    public Task<bool> RoomMoveUpgradeRuleCodeExistsAsync(Guid propertyId, string code, Guid? excludingId, CancellationToken ct = default) => dbContext.HotelRoomMoveUpgradeRules.AnyAsync(x => x.PropertyId == propertyId && x.Code == code && (excludingId == null || x.HotelRoomMoveUpgradeRuleId != excludingId), ct);
    public Task<bool> PropertyDocumentNumberExistsAsync(Guid propertyId, string documentType, string documentNumber, Guid? excludingId, CancellationToken ct = default) => dbContext.HotelPropertyDocuments.AnyAsync(x => x.PropertyId == propertyId && x.DocumentType == documentType && x.DocumentNumber == documentNumber && (excludingId == null || x.HotelPropertyDocumentId != excludingId), ct);
    public Task<bool> PromotionPackageCodeExistsAsync(Guid propertyId, string code, Guid? excludingId, CancellationToken ct = default) => dbContext.HotelPromotionPackages.AnyAsync(x => x.PropertyId == propertyId && x.Code == code && (excludingId == null || x.HotelPromotionPackageId != excludingId), ct);
    public Task<bool> GroupPolicyCodeExistsAsync(Guid propertyId, string code, Guid? excludingId, CancellationToken ct = default) => dbContext.HotelGroupPolicies.AnyAsync(x => x.PropertyId == propertyId && x.Code == code && (excludingId == null || x.HotelGroupPolicyId != excludingId), ct);
    public Task<bool> CashierShiftPolicyCodeExistsAsync(Guid propertyId, string code, Guid? excludingId, CancellationToken ct = default) => dbContext.HotelCashierShiftPolicies.AnyAsync(x => x.PropertyId == propertyId && x.Code == code && (excludingId == null || x.HotelCashierShiftPolicyId != excludingId), ct);
    public Task<bool> NightAuditPolicyCodeExistsAsync(Guid propertyId, string code, Guid? excludingId, CancellationToken ct = default) => dbContext.HotelNightAuditPolicies.AnyAsync(x => x.PropertyId == propertyId && x.Code == code && (excludingId == null || x.HotelNightAuditPolicyId != excludingId), ct);
    public Task<bool> ApprovalMatrixRuleCodeExistsAsync(Guid propertyId, string code, Guid? excludingId, CancellationToken ct = default) => dbContext.HotelApprovalMatrixRules.AnyAsync(x => x.PropertyId == propertyId && x.Code == code && (excludingId == null || x.HotelApprovalMatrixRuleId != excludingId), ct);
    public Task<bool> FOPermissionPolicyCodeExistsAsync(Guid propertyId, string code, Guid? excludingId, CancellationToken ct = default) => dbContext.HotelFOPermissionPolicies.AnyAsync(x => x.PropertyId == propertyId && x.Code == code && (excludingId == null || x.HotelFOPermissionPolicyId != excludingId), ct);
    public Task<bool> FormPrinterProfileCodeExistsAsync(Guid propertyId, string code, Guid? excludingId, CancellationToken ct = default) => dbContext.HotelFormPrinterProfiles.AnyAsync(x => x.PropertyId == propertyId && x.Code == code && (excludingId == null || x.HotelFormPrinterProfileId != excludingId), ct);
    public Task<bool> MessagingIntegrationProfileCodeExistsAsync(Guid propertyId, string code, Guid? excludingId, CancellationToken ct = default) => dbContext.HotelMessagingIntegrationProfiles.AnyAsync(x => x.PropertyId == propertyId && x.Code == code && (excludingId == null || x.HotelMessagingIntegrationProfileId != excludingId), ct);
    public Task<bool> PostedCorrectionPolicyCodeExistsAsync(Guid propertyId, string code, Guid? excludingId, CancellationToken ct = default) => dbContext.HotelPostedCorrectionPolicies.AnyAsync(x => x.PropertyId == propertyId && x.Code == code && (excludingId == null || x.HotelPostedCorrectionPolicyId != excludingId), ct);
    public Task<bool> BusinessRuleProfileCodeExistsAsync(Guid propertyId, string code, Guid? excludingId, CancellationToken ct = default) => dbContext.HotelBusinessRuleProfiles.AnyAsync(x => x.PropertyId == propertyId && x.Code == code && (excludingId == null || x.HotelBusinessRuleProfileId != excludingId), ct);
    public Task<bool> TaxServiceChargeRuleCodeExistsAsync(Guid propertyId, HotelTaxServiceChargeKind kind, string code, Guid? excludingId, CancellationToken ct = default) => dbContext.HotelTaxServiceChargeRules.AnyAsync(x => x.PropertyId == propertyId && x.Kind == kind && x.Code == code && (excludingId == null || x.HotelTaxServiceChargeRuleId != excludingId), ct);
    public Task<bool> PaymentMethodProfileCodeExistsAsync(Guid propertyId, string code, Guid? excludingId, CancellationToken ct = default) => dbContext.HotelPaymentMethodProfiles.AnyAsync(x => x.PropertyId == propertyId && x.Code == code && (excludingId == null || x.HotelPaymentMethodProfileId != excludingId), ct);
    public Task<bool> DepositPolicyCodeExistsAsync(Guid propertyId, string code, Guid? excludingId, CancellationToken ct = default) => dbContext.HotelDepositPolicies.AnyAsync(x => x.PropertyId == propertyId && x.Code == code && (excludingId == null || x.HotelDepositPolicyId != excludingId), ct);
    public Task<bool> DiscountPolicyCodeExistsAsync(Guid propertyId, string code, Guid? excludingId, CancellationToken ct = default) => dbContext.HotelDiscountPolicies.AnyAsync(x => x.PropertyId == propertyId && x.Code == code && (excludingId == null || x.HotelDiscountPolicyId != excludingId), ct);
    public Task<bool> ChargeCodeProfileCodeExistsAsync(Guid propertyId, string code, Guid? excludingId, CancellationToken ct = default) => dbContext.HotelChargeCodeProfiles.AnyAsync(x => x.PropertyId == propertyId && x.Code == code && (excludingId == null || x.HotelChargeCodeProfileId != excludingId), ct);
    public Task<bool> FolioProfileCodeExistsAsync(Guid propertyId, string code, Guid? excludingId, CancellationToken ct = default) => dbContext.HotelFolioProfiles.AnyAsync(x => x.PropertyId == propertyId && x.Code == code && (excludingId == null || x.HotelFolioProfileId != excludingId), ct);
    public Task<bool> GuestTypeProfileCodeExistsAsync(Guid propertyId, string code, Guid? excludingId, CancellationToken ct = default) => dbContext.HotelGuestTypeProfiles.AnyAsync(x => x.PropertyId == propertyId && x.Code == code && (excludingId == null || x.HotelGuestTypeProfileId != excludingId), ct);
    public Task<bool> ProfileTypeProfileCodeExistsAsync(Guid propertyId, string code, Guid? excludingId, CancellationToken ct = default) => dbContext.HotelProfileTypeProfiles.AnyAsync(x => x.PropertyId == propertyId && x.Code == code && (excludingId == null || x.HotelProfileTypeProfileId != excludingId), ct);
    public Task<bool> VipLevelProfileCodeExistsAsync(Guid propertyId, string code, Guid? excludingId, CancellationToken ct = default) => dbContext.HotelVipLevelProfiles.AnyAsync(x => x.PropertyId == propertyId && x.Code == code && (excludingId == null || x.HotelVipLevelProfileId != excludingId), ct);
    public Task<bool> GuestPreferenceProfileCodeExistsAsync(Guid propertyId, string code, Guid? excludingId, CancellationToken ct = default) => dbContext.HotelGuestPreferenceProfiles.AnyAsync(x => x.PropertyId == propertyId && x.Code == code && (excludingId == null || x.HotelGuestPreferenceProfileId != excludingId), ct);
    public Task<bool> GuestRequestProfileCodeExistsAsync(Guid propertyId, string code, Guid? excludingId, CancellationToken ct = default) => dbContext.HotelGuestRequestProfiles.AnyAsync(x => x.PropertyId == propertyId && x.Code == code && (excludingId == null || x.HotelGuestRequestProfileId != excludingId), ct);
    public Task<bool> ReservationTypeProfileCodeExistsAsync(Guid propertyId, string code, Guid? excludingId, CancellationToken ct = default) => dbContext.HotelReservationTypeProfiles.AnyAsync(x => x.PropertyId == propertyId && x.Code == code && (excludingId == null || x.HotelReservationTypeProfileId != excludingId), ct);
    public Task<bool> BookingSourceProfileCodeExistsAsync(Guid propertyId, string code, Guid? excludingId, CancellationToken ct = default) => dbContext.HotelBookingSourceProfiles.AnyAsync(x => x.PropertyId == propertyId && x.Code == code && (excludingId == null || x.HotelBookingSourceProfileId != excludingId), ct);
    public Task<bool> BookingChannelProfileCodeExistsAsync(Guid propertyId, string code, Guid? excludingId, CancellationToken ct = default) => dbContext.HotelBookingChannelProfiles.AnyAsync(x => x.PropertyId == propertyId && x.Code == code && (excludingId == null || x.HotelBookingChannelProfileId != excludingId), ct);
    public Task<bool> MarketSegmentProfileCodeExistsAsync(Guid propertyId, string code, Guid? excludingId, CancellationToken ct = default) => dbContext.HotelMarketSegmentProfiles.AnyAsync(x => x.PropertyId == propertyId && x.Code == code && (excludingId == null || x.HotelMarketSegmentProfileId != excludingId), ct);
    public Task<bool> HousekeepingRuleProfileCodeExistsAsync(Guid propertyId, string code, Guid? excludingId, CancellationToken ct = default) => dbContext.HotelHousekeepingRuleProfiles.AnyAsync(x => x.PropertyId == propertyId && x.Code == code && (excludingId == null || x.HotelHousekeepingRuleProfileId != excludingId), ct);
    public Task<bool> WakeupRuleProfileCodeExistsAsync(Guid propertyId, string code, Guid? excludingId, CancellationToken ct = default) => dbContext.HotelWakeupRuleProfiles.AnyAsync(x => x.PropertyId == propertyId && x.Code == code && (excludingId == null || x.HotelWakeupRuleProfileId != excludingId), ct);
    public Task<bool> ComplaintProfileCodeExistsAsync(Guid propertyId, string code, Guid? excludingId, CancellationToken ct = default) => dbContext.HotelComplaintProfiles.AnyAsync(x => x.PropertyId == propertyId && x.Code == code && (excludingId == null || x.HotelComplaintProfileId != excludingId), ct);

    public async Task<PageResult<HotelBuildingDto>> SearchBuildingsAsync(Guid propertyId, string? keyword, bool? isActive, PageRequest page, string? sort, CancellationToken ct = default)
    {
        var query = dbContext.HotelBuildings.AsNoTracking().Where(x => x.PropertyId == propertyId);
        if (!string.IsNullOrWhiteSpace(keyword)) query = query.Where(x => x.Code.Contains(keyword) || x.Name.Contains(keyword));
        if (isActive is not null) query = query.Where(x => x.IsActive == isActive);
        query = sort?.ToLowerInvariant() switch
        {
            "code" => query.OrderBy(x => x.Code),
            "name" => query.OrderBy(x => x.Name),
            "updated" => query.OrderByDescending(x => x.UpdatedAtUtc),
            _ => query.OrderBy(x => x.DisplayOrder).ThenBy(x => x.Code)
        };
        return await Page(query.Select(BuildingDto), page, ct);
    }

    public async Task<PageResult<HotelFloorDto>> SearchFloorsAsync(Guid propertyId, Guid? buildingId, string? keyword, bool? isActive, PageRequest page, string? sort, CancellationToken ct = default)
    {
        var floorsQuery = dbContext.HotelFloors.AsNoTracking().Where(x => x.PropertyId == propertyId);
        if (buildingId is not null) floorsQuery = floorsQuery.Where(x => x.HotelBuildingId == buildingId);
        if (isActive is not null) floorsQuery = floorsQuery.Where(x => x.IsActive == isActive);

        var floors = await floorsQuery.ToListAsync(ct);
        var buildingIds = floors.Select(x => x.HotelBuildingId).Distinct().ToArray();
        var buildings = await dbContext.HotelBuildings.AsNoTracking()
            .Where(x => x.PropertyId == propertyId && buildingIds.Contains(x.HotelBuildingId))
            .ToDictionaryAsync(x => x.HotelBuildingId, ct);
        var items = floors.Where(x => buildings.ContainsKey(x.HotelBuildingId))
            .Select(x => ToFloorDto(x, buildings[x.HotelBuildingId]))
            .Where(x => string.IsNullOrWhiteSpace(keyword)
                || x.Code.Contains(keyword, StringComparison.OrdinalIgnoreCase)
                || x.Name.Contains(keyword, StringComparison.OrdinalIgnoreCase)
                || x.BuildingCode.Contains(keyword, StringComparison.OrdinalIgnoreCase)
                || x.BuildingName.Contains(keyword, StringComparison.OrdinalIgnoreCase));

        items = sort?.ToLowerInvariant() switch
        {
            "level" => items.OrderBy(x => x.LevelNumber).ThenBy(x => x.Code),
            "building" => items.OrderBy(x => x.BuildingCode).ThenBy(x => x.LevelNumber),
            "updated" => items.OrderByDescending(x => x.UpdatedAtUtc),
            _ => items.OrderBy(x => x.BuildingCode).ThenBy(x => x.DisplayOrder).ThenBy(x => x.LevelNumber)
        };
        return PageInMemory(items, page);
    }

    public async Task<PageResult<HotelAreaDto>> SearchAreasAsync(Guid propertyId, Guid? buildingId, Guid? floorId, string? keyword, bool? isActive, PageRequest page, string? sort, CancellationToken ct = default)
    {
        var areasQuery = dbContext.HotelAreas.AsNoTracking().Where(x => x.PropertyId == propertyId);
        if (buildingId is not null) areasQuery = areasQuery.Where(x => x.HotelBuildingId == buildingId);
        if (floorId is not null) areasQuery = areasQuery.Where(x => x.HotelFloorId == floorId);
        if (isActive is not null) areasQuery = areasQuery.Where(x => x.IsActive == isActive);

        var areas = await areasQuery.ToListAsync(ct);
        var buildingIds = areas.Where(x => x.HotelBuildingId is not null).Select(x => x.HotelBuildingId!.Value).Distinct().ToArray();
        var floorIds = areas.Where(x => x.HotelFloorId is not null).Select(x => x.HotelFloorId!.Value).Distinct().ToArray();
        var buildings = await dbContext.HotelBuildings.AsNoTracking()
            .Where(x => x.PropertyId == propertyId && buildingIds.Contains(x.HotelBuildingId))
            .ToDictionaryAsync(x => x.HotelBuildingId, ct);
        var floors = await dbContext.HotelFloors.AsNoTracking()
            .Where(x => x.PropertyId == propertyId && floorIds.Contains(x.HotelFloorId))
            .ToDictionaryAsync(x => x.HotelFloorId, ct);
        var items = areas.Select(x => ToAreaDto(
                x,
                x.HotelBuildingId is { } areaBuildingId && buildings.TryGetValue(areaBuildingId, out var building) ? building : null,
                x.HotelFloorId is { } areaFloorId && floors.TryGetValue(areaFloorId, out var floor) ? floor : null))
            .Where(x => string.IsNullOrWhiteSpace(keyword)
                || x.Code.Contains(keyword, StringComparison.OrdinalIgnoreCase)
                || x.Name.Contains(keyword, StringComparison.OrdinalIgnoreCase)
                || (x.AreaType?.Contains(keyword, StringComparison.OrdinalIgnoreCase) ?? false)
                || (x.BuildingCode?.Contains(keyword, StringComparison.OrdinalIgnoreCase) ?? false)
                || (x.FloorCode?.Contains(keyword, StringComparison.OrdinalIgnoreCase) ?? false));

        items = sort?.ToLowerInvariant() switch
        {
            "name" => items.OrderBy(x => x.Name),
            "building" => items.OrderBy(x => x.BuildingCode).ThenBy(x => x.DisplayOrder),
            "updated" => items.OrderByDescending(x => x.UpdatedAtUtc),
            _ => items.OrderBy(x => x.DisplayOrder).ThenBy(x => x.Code)
        };
        return PageInMemory(items, page);
    }

    public async Task<PageResult<HotelReferenceCatalogItemDto>> SearchCatalogItemsAsync(Guid propertyId, string catalogKey, string? keyword, bool? isActive, PageRequest page, string? sort, CancellationToken ct = default)
    {
        var query = dbContext.HotelReferenceCatalogItems.AsNoTracking().Where(x => x.PropertyId == propertyId && x.CatalogKey == catalogKey);
        if (!string.IsNullOrWhiteSpace(keyword)) query = query.Where(x => x.Code.Contains(keyword) || x.Name.Contains(keyword) || (x.Description != null && x.Description.Contains(keyword)));
        if (isActive is not null) query = query.Where(x => x.IsActive == isActive);
        query = sort?.ToLowerInvariant() switch
        {
            "code" => query.OrderBy(x => x.Code),
            "name" => query.OrderBy(x => x.Name),
            "updated" => query.OrderByDescending(x => x.UpdatedAtUtc),
            _ => query.OrderBy(x => x.DisplayOrder).ThenBy(x => x.Code)
        };
        return await Page(query.Select(CatalogItemDto), page, ct);
    }

    public async Task<PageResult<HotelRoomStatusRuleDto>> SearchRoomStatusRulesAsync(Guid propertyId, string? domain, string? keyword, bool? isActive, PageRequest page, string? sort, CancellationToken ct = default)
    {
        var query = dbContext.HotelRoomStatusRules.AsNoTracking().Where(x => x.PropertyId == propertyId);
        if (!string.IsNullOrWhiteSpace(domain)) query = query.Where(x => x.StatusDomain == domain);
        if (!string.IsNullOrWhiteSpace(keyword)) query = query.Where(x => x.Code.Contains(keyword) || x.Name.Contains(keyword) || x.FromStatus.Contains(keyword) || x.ToStatus.Contains(keyword) || (x.Description != null && x.Description.Contains(keyword)));
        if (isActive is not null) query = query.Where(x => x.IsActive == isActive);
        query = sort?.ToLowerInvariant() switch
        {
            "code" => query.OrderBy(x => x.Code),
            "domain" => query.OrderBy(x => x.StatusDomain).ThenBy(x => x.DisplayOrder),
            "updated" => query.OrderByDescending(x => x.UpdatedAtUtc),
            _ => query.OrderBy(x => x.StatusDomain).ThenBy(x => x.DisplayOrder).ThenBy(x => x.Code)
        };
        return await Page(query.Select(RoomStatusRuleDto), page, ct);
    }

    public Task AddCompanyProfileAsync(HotelCompanyProfile profile, CancellationToken ct = default) => dbContext.HotelCompanyProfiles.AddAsync(profile, ct).AsTask();
    public Task<HotelCompanyProfile?> GetCompanyProfileAsync(Guid propertyId, Guid id, CancellationToken ct = default) => dbContext.HotelCompanyProfiles.SingleOrDefaultAsync(x => x.PropertyId == propertyId && x.HotelCompanyProfileId == id, ct);
    public Task<bool> CompanyProfileCodeExistsAsync(Guid propertyId, string code, Guid? excludingId, CancellationToken ct = default) => dbContext.HotelCompanyProfiles.AnyAsync(x => x.PropertyId == propertyId && x.Code == code.Trim().ToUpper() && (!excludingId.HasValue || x.HotelCompanyProfileId != excludingId.Value), ct);
    public Task<HotelCompanyProfileDto?> GetCompanyProfileDtoAsync(Guid propertyId, Guid id, CancellationToken ct = default) => dbContext.HotelCompanyProfiles.AsNoTracking().Where(x => x.PropertyId == propertyId && x.HotelCompanyProfileId == id).Select(CompanyProfileDto).SingleOrDefaultAsync(ct);
    public async Task<PageResult<HotelCompanyProfileDto>> SearchCompanyProfilesAsync(Guid propertyId, HotelCompanyKind? kind, string? keyword, bool? isActive, PageRequest page, string? sort, CancellationToken ct = default)
    { var query = dbContext.HotelCompanyProfiles.AsNoTracking().Where(x => x.PropertyId == propertyId); if (kind is not null) query = query.Where(x => x.Kind == kind); if (!string.IsNullOrWhiteSpace(keyword)) query = query.Where(x => x.Code.Contains(keyword) || x.Name.Contains(keyword) || (x.TaxCode != null && x.TaxCode.Contains(keyword)) || (x.ContactName != null && x.ContactName.Contains(keyword))); if (isActive is not null) query = query.Where(x => x.IsActive == isActive); query = sort?.ToLowerInvariant() switch { "kind" => query.OrderBy(x => x.Kind).ThenBy(x => x.Code), "updated" => query.OrderByDescending(x => x.UpdatedAtUtc), _ => query.OrderBy(x => x.Code) }; return await Page(query.Select(CompanyProfileDto), page, ct); }
    public Task AddTravelAgentProfileAsync(HotelTravelAgentProfile profile, CancellationToken ct = default) => dbContext.HotelTravelAgentProfiles.AddAsync(profile, ct).AsTask();
    public Task<HotelTravelAgentProfile?> GetTravelAgentProfileAsync(Guid propertyId, Guid id, CancellationToken ct = default) => dbContext.HotelTravelAgentProfiles.SingleOrDefaultAsync(x => x.PropertyId == propertyId && x.HotelTravelAgentProfileId == id, ct);
    public Task<bool> TravelAgentProfileCodeExistsAsync(Guid propertyId, string code, Guid? excludingId, CancellationToken ct = default) => dbContext.HotelTravelAgentProfiles.AnyAsync(x => x.PropertyId == propertyId && x.Code == code.Trim().ToUpper() && (!excludingId.HasValue || x.HotelTravelAgentProfileId != excludingId.Value), ct);
    public Task<HotelTravelAgentProfileDto?> GetTravelAgentProfileDtoAsync(Guid propertyId, Guid id, CancellationToken ct = default) => dbContext.HotelTravelAgentProfiles.AsNoTracking().Where(x => x.PropertyId == propertyId && x.HotelTravelAgentProfileId == id).Select(TravelAgentProfileDto).SingleOrDefaultAsync(ct);
    public async Task<PageResult<HotelTravelAgentProfileDto>> SearchTravelAgentProfilesAsync(Guid propertyId, HotelTravelAgentKind? kind, string? keyword, bool? isActive, PageRequest page, string? sort, CancellationToken ct = default)
    { var query = dbContext.HotelTravelAgentProfiles.AsNoTracking().Where(x => x.PropertyId == propertyId); if (kind is not null) query = query.Where(x => x.Kind == kind); if (!string.IsNullOrWhiteSpace(keyword)) query = query.Where(x => x.Code.Contains(keyword) || x.Name.Contains(keyword) || (x.IataCode != null && x.IataCode.Contains(keyword)) || (x.ContactName != null && x.ContactName.Contains(keyword))); if (isActive is not null) query = query.Where(x => x.IsActive == isActive); query = sort?.ToLowerInvariant() switch { "kind" => query.OrderBy(x => x.Kind).ThenBy(x => x.Code), "updated" => query.OrderByDescending(x => x.UpdatedAtUtc), _ => query.OrderBy(x => x.Code) }; return await Page(query.Select(TravelAgentProfileDto), page, ct); }
    public Task AddCommissionRuleProfileAsync(HotelCommissionRuleProfile profile, CancellationToken ct = default) => dbContext.HotelCommissionRuleProfiles.AddAsync(profile, ct).AsTask();
    public Task<HotelCommissionRuleProfile?> GetCommissionRuleProfileAsync(Guid propertyId, Guid id, CancellationToken ct = default) => dbContext.HotelCommissionRuleProfiles.SingleOrDefaultAsync(x => x.PropertyId == propertyId && x.HotelCommissionRuleProfileId == id, ct);
    public Task<bool> CommissionRuleProfileCodeExistsAsync(Guid propertyId, string code, Guid? excludingId, CancellationToken ct = default) => dbContext.HotelCommissionRuleProfiles.AnyAsync(x => x.PropertyId == propertyId && x.Code == code.Trim().ToUpper() && (!excludingId.HasValue || x.HotelCommissionRuleProfileId != excludingId.Value), ct);
    public Task<HotelCommissionRuleProfileDto?> GetCommissionRuleProfileDtoAsync(Guid propertyId, Guid id, CancellationToken ct = default) => dbContext.HotelCommissionRuleProfiles.AsNoTracking().Where(x => x.PropertyId == propertyId && x.HotelCommissionRuleProfileId == id).Select(CommissionRuleProfileDto).SingleOrDefaultAsync(ct);
    public async Task<PageResult<HotelCommissionRuleProfileDto>> SearchCommissionRuleProfilesAsync(Guid propertyId, HotelCommissionPartnerKind? partnerKind, string? keyword, bool? isActive, PageRequest page, string? sort, CancellationToken ct = default)
    { var query = dbContext.HotelCommissionRuleProfiles.AsNoTracking().Where(x => x.PropertyId == propertyId); if (partnerKind is not null) query = query.Where(x => x.PartnerKind == partnerKind); if (!string.IsNullOrWhiteSpace(keyword)) query = query.Where(x => x.Code.Contains(keyword) || x.Name.Contains(keyword) || (x.Description != null && x.Description.Contains(keyword))); if (isActive is not null) query = query.Where(x => x.IsActive == isActive); query = sort?.ToLowerInvariant() switch { "partner" => query.OrderBy(x => x.PartnerKind).ThenBy(x => x.Code), "updated" => query.OrderByDescending(x => x.UpdatedAtUtc), _ => query.OrderBy(x => x.Code) }; return await Page(query.Select(CommissionRuleProfileDto), page, ct); }

    private static HotelFloorDto ToFloorDto(HotelFloor floor, HotelBuilding building) => new(
        floor.HotelFloorId,
        floor.HotelBuildingId,
        building.Code,
        building.Name,
        floor.Code,
        floor.Name,
        floor.LevelNumber,
        floor.Description,
        floor.DisplayOrder,
        floor.IsActive,
        floor.UpdatedAtUtc,
        floor.RowVersion);

    private static HotelAreaDto ToAreaDto(HotelArea area, HotelBuilding? building, HotelFloor? floor) => new(
        area.HotelAreaId,
        area.HotelBuildingId,
        area.HotelFloorId,
        building?.Code,
        building?.Name,
        floor?.Code,
        floor?.Name,
        area.Code,
        area.Name,
        area.AreaType,
        area.Description,
        area.DisplayOrder,
        area.IsActive,
        area.UpdatedAtUtc,
        area.RowVersion);

    private static PageResult<T> PageInMemory<T>(IEnumerable<T> source, PageRequest page)
    {
        var items = source.ToList();
        var size = Math.Clamp(page.PageSize, 1, 200);
        return new PageResult<T>(items.Skip(page.Skip).Take(size).ToList(), items.Count, page.Page, page.PageSize);
    }

    private static readonly System.Linq.Expressions.Expression<Func<HotelBuilding, HotelBuildingDto>> BuildingDto = x => new HotelBuildingDto(x.HotelBuildingId, x.Code, x.Name, x.Description, x.DisplayOrder, x.IsActive, x.UpdatedAtUtc, x.RowVersion);
    private static readonly System.Linq.Expressions.Expression<Func<HotelReferenceCatalogItem, HotelReferenceCatalogItemDto>> CatalogItemDto = x => new HotelReferenceCatalogItemDto(x.HotelReferenceCatalogItemId, x.CatalogKey, x.Code, x.Name, x.Description, x.DisplayOrder, x.IsActive, x.UpdatedAtUtc, x.RowVersion);
    private static readonly System.Linq.Expressions.Expression<Func<HotelRoomStatusRule, HotelRoomStatusRuleDto>> RoomStatusRuleDto = x => new HotelRoomStatusRuleDto(x.HotelRoomStatusRuleId, x.Code, x.Name, x.StatusDomain, x.FromStatus, x.ToStatus, x.RequiredRole, x.RequiredEvent, x.RequiresApproval, x.Description, x.DisplayOrder, x.IsActive, x.UpdatedAtUtc, x.RowVersion);
    private static readonly System.Linq.Expressions.Expression<Func<HotelPropertyProfile, HotelPropertyProfileDto>> PropertyProfileDto = x => new HotelPropertyProfileDto(x.HotelPropertyProfileId, x.Code, x.Name, x.Address, x.Phone, x.Email, x.TaxCode, x.TimeZoneId, x.CurrencyCode, x.CheckInMinutes, x.CheckOutMinutes, x.BusinessDate, x.IsActive, x.UpdatedAtUtc, x.RowVersion);
    private static readonly System.Linq.Expressions.Expression<Func<HotelStayPolicy, HotelStayPolicyDto>> StayPolicyDto = x => new HotelStayPolicyDto(x.HotelStayPolicyId, x.Code, x.Name, x.EffectiveFrom, x.EffectiveTo, x.CancellationDeadlineHours, x.NoShowChargeNights, x.DepositPercent, x.MinimumNights, x.MaximumNights, x.ChildrenAllowed, x.PetsAllowed, x.Description, x.IsActive, x.UpdatedAtUtc, x.RowVersion);
    private static readonly System.Linq.Expressions.Expression<Func<HotelCheckInOutRule, HotelCheckInOutRuleDto>> CheckInOutRuleDto = x => new HotelCheckInOutRuleDto(x.HotelCheckInOutRuleId, x.Code, x.Name, x.EffectiveFrom, x.EffectiveTo, x.EarlyCheckInGraceMinutes, x.LateCheckOutGraceMinutes, x.EarlyCheckInApprovalRole, x.LateCheckOutApprovalRole, x.EarlyCheckInChargeCode, x.LateCheckOutChargeCode, x.RequireGuestDocument, x.RequirePaymentAuthorization, x.Description, x.IsActive, x.UpdatedAtUtc, x.RowVersion);
    private static readonly System.Linq.Expressions.Expression<Func<HotelRoomMoveUpgradeRule, HotelRoomMoveUpgradeRuleDto>> RoomMoveUpgradeRuleDto = x => new HotelRoomMoveUpgradeRuleDto(x.HotelRoomMoveUpgradeRuleId, x.Code, x.Name, x.EffectiveFrom, x.EffectiveTo, x.AllowRoomMove, x.AllowUpgrade, x.AllowDowngrade, x.RequireApproval, x.ApprovalRole, x.PriceDifferenceMode, x.MaxClassChangeLevels, x.RequireHousekeepingReady, x.Description, x.IsActive, x.UpdatedAtUtc, x.RowVersion);
    private static readonly System.Linq.Expressions.Expression<Func<HotelPropertyDocument, HotelPropertyDocumentDto>> PropertyDocumentDto = x => new HotelPropertyDocumentDto(x.HotelPropertyDocumentId, x.DocumentType, x.DocumentNumber, x.IssuedBy, x.IssuedDate, x.ExpiryDate, x.FileReference, x.Notes, x.IsRequired, x.IsActive, x.UpdatedAtUtc, x.RowVersion);
    private static readonly System.Linq.Expressions.Expression<Func<HotelPromotionPackage, HotelPromotionPackageDto>> PromotionPackageDto = x => new HotelPromotionPackageDto(x.HotelPromotionPackageId, x.Code, x.Name, x.OfferType, x.DiscountMode, x.DiscountValue, x.EffectiveFrom, x.EffectiveTo, x.BookingFrom, x.BookingTo, x.MinimumNights, x.RequiresPrepayment, x.IncludedServices, x.BlackoutDates, x.Description, x.IsActive, x.UpdatedAtUtc, x.RowVersion);
    private static readonly System.Linq.Expressions.Expression<Func<HotelGroupPolicy, HotelGroupPolicyDto>> GroupPolicyDto = x => new HotelGroupPolicyDto(x.HotelGroupPolicyId, x.Code, x.Name, x.EffectiveFrom, x.EffectiveTo, x.DefaultCutoffDays, x.RoomReleaseDaysBeforeArrival, x.DepositPercent, x.DepositDueDaysBeforeArrival, x.DepositRequired, x.AllowRoomingListImport, x.DefaultRouting, x.DefaultBillingInstruction, x.Description, x.IsActive, x.UpdatedAtUtc, x.RowVersion);
    private static readonly System.Linq.Expressions.Expression<Func<HotelCashierShiftPolicy, HotelCashierShiftPolicyDto>> CashierShiftPolicyDto = x => new HotelCashierShiftPolicyDto(x.HotelCashierShiftPolicyId, x.Code, x.Name, x.EffectiveFrom, x.EffectiveTo, x.RequireOpeningFloat, x.DefaultOpeningFloat, x.RequireSupervisorClose, x.VarianceApprovalThreshold, x.AllowCashTransfer, x.RequireHandoverNote, x.SupervisorRole, x.Description, x.IsActive, x.UpdatedAtUtc, x.RowVersion);
    private static readonly System.Linq.Expressions.Expression<Func<HotelNightAuditPolicy, HotelNightAuditPolicyDto>> NightAuditPolicyDto = x => new HotelNightAuditPolicyDto(x.HotelNightAuditPolicyId, x.Code, x.Name, x.EffectiveFrom, x.EffectiveTo, x.LockBusinessDateDuringAudit, x.RequireAllValidationsPassed, x.AllowResumeAfterFailure, x.RequireSupervisorApproval, x.MaximumRetries, x.PostRoomCharges, x.CloseEligibleFolios, x.AdvanceBusinessDateOnSuccess, x.SupervisorRole, x.Description, x.IsActive, x.UpdatedAtUtc, x.RowVersion);
    private static readonly System.Linq.Expressions.Expression<Func<HotelApprovalMatrixRule, HotelApprovalMatrixRuleDto>> ApprovalMatrixRuleDto = x => new HotelApprovalMatrixRuleDto(x.HotelApprovalMatrixRuleId, x.Code, x.Name, x.Domain, x.Action, x.EffectiveFrom, x.EffectiveTo, x.ThresholdAmount, x.MinimumApprovers, x.RequiredRole, x.RequireDifferentApprover, x.RequireReason, x.Description, x.IsActive, x.UpdatedAtUtc, x.RowVersion);
    private static readonly System.Linq.Expressions.Expression<Func<HotelFOPermissionPolicy, HotelFOPermissionPolicyDto>> FOPermissionPolicyDto = x => new HotelFOPermissionPolicyDto(x.HotelFOPermissionPolicyId, x.Code, x.Name, x.Module, x.Action, x.RequiredRole, x.DataScope, x.IsSensitive, x.EffectiveFrom, x.EffectiveTo, x.Description, x.IsActive, x.UpdatedAtUtc, x.RowVersion);
    private static readonly System.Linq.Expressions.Expression<Func<HotelFormPrinterProfile, HotelFormPrinterProfileDto>> FormPrinterProfileDto = x => new HotelFormPrinterProfileDto(x.HotelFormPrinterProfileId, x.Code, x.Name, x.DocumentType, x.TemplateReference, x.PrinterName, x.PaperSize, x.Copies, x.Duplex, x.EffectiveFrom, x.EffectiveTo, x.Description, x.IsActive, x.UpdatedAtUtc, x.RowVersion);
    private static readonly System.Linq.Expressions.Expression<Func<HotelMessagingIntegrationProfile, HotelMessagingIntegrationProfileDto>> MessagingIntegrationProfileDto = x => new HotelMessagingIntegrationProfileDto(x.HotelMessagingIntegrationProfileId, x.Code, x.Name, x.Channel, x.Provider, x.Endpoint, x.SecretReference, x.TemplateReference, x.MaxRetries, x.TimeoutSeconds, x.EffectiveFrom, x.EffectiveTo, x.Description, x.IsActive, x.UpdatedAtUtc, x.RowVersion);
    private static readonly System.Linq.Expressions.Expression<Func<HotelPostedCorrectionPolicy, HotelPostedCorrectionPolicyDto>> PostedCorrectionPolicyDto = x => new HotelPostedCorrectionPolicyDto(x.HotelPostedCorrectionPolicyId, x.Code, x.Name, x.Domain, x.MaximumAmount, x.RequireReason, x.RequireApproval, x.AllowedRoles, x.EffectiveFrom, x.EffectiveTo, x.Description, x.IsActive, x.UpdatedAtUtc, x.RowVersion);
    private static readonly System.Linq.Expressions.Expression<Func<HotelBusinessRuleProfile, HotelBusinessRuleProfileDto>> BusinessRuleProfileDto = x => new HotelBusinessRuleProfileDto(x.HotelBusinessRuleProfileId, x.Code, x.Name, x.Domain, x.ConditionReference, x.Priority, x.IsBlocking, x.IsExclusive, x.EffectiveFrom, x.EffectiveTo, x.Description, x.IsActive, x.UpdatedAtUtc, x.RowVersion);
    private static readonly System.Linq.Expressions.Expression<Func<HotelTaxServiceChargeRule, HotelTaxServiceChargeRuleDto>> TaxServiceChargeRuleDto = x => new HotelTaxServiceChargeRuleDto(x.HotelTaxServiceChargeRuleId, x.Kind, x.Code, x.Name, x.Rate, x.IsInclusive, x.ApplicableService, x.ApplicableRatePlan, x.RoundingMode, x.AccountingMapping, x.EffectiveFrom, x.EffectiveTo, x.Description, x.IsActive, x.UpdatedAtUtc, x.RowVersion);
    private static readonly System.Linq.Expressions.Expression<Func<HotelPaymentMethodProfile, HotelPaymentMethodProfileDto>> PaymentMethodProfileDto = x => new HotelPaymentMethodProfileDto(x.HotelPaymentMethodProfileId, x.Code, x.Name, x.Kind, x.CurrencyCode, x.BankAccount, x.CardTerminal, x.FeeRate, x.AccountingMapping, x.RequireReference, x.RequireApproval, x.IsActive, x.UpdatedAtUtc, x.RowVersion);
    private static readonly System.Linq.Expressions.Expression<Func<HotelDepositPolicy, HotelDepositPolicyDto>> DepositPolicyDto = x => new HotelDepositPolicyDto(x.HotelDepositPolicyId, x.Code, x.Name, x.DepositType, x.Required, x.Percentage, x.FixedAmount, x.DueDaysBeforeArrival, x.Refundable, x.Transferable, x.Forfeitable, x.AutoReminder, x.AutoApplyToFolio, x.EffectiveFrom, x.EffectiveTo, x.Description, x.IsActive, x.UpdatedAtUtc, x.RowVersion);
    private static readonly System.Linq.Expressions.Expression<Func<HotelDiscountPolicy, HotelDiscountPolicyDto>> DiscountPolicyDto = x => new HotelDiscountPolicyDto(x.HotelDiscountPolicyId, x.Code, x.Name, x.DiscountType, x.Percentage, x.FixedAmount, x.MaximumDiscount, x.AllowedRoles, x.ApprovalLevel, x.ReasonRequired, x.AccountingTreatment, x.EffectiveFrom, x.EffectiveTo, x.Description, x.IsActive, x.UpdatedAtUtc, x.RowVersion);
    private static readonly System.Linq.Expressions.Expression<Func<HotelChargeCodeProfile, HotelChargeCodeProfileDto>> ChargeCodeProfileDto = x => new HotelChargeCodeProfileDto(x.HotelChargeCodeProfileId, x.Code, x.Name, x.Category, x.Unit, x.Price, x.TaxCode, x.ServiceChargeCode, x.RevenueDepartment, x.RevenueAccount, x.TaxAccount, x.CostCenter, x.AllowManualPosting, x.AllowDiscount, x.AllowVoid, x.EffectiveFrom, x.EffectiveTo, x.Description, x.IsActive, x.UpdatedAtUtc, x.RowVersion);
    private static readonly System.Linq.Expressions.Expression<Func<HotelFolioProfile, HotelFolioProfileDto>> FolioProfileDto = x => new HotelFolioProfileDto(x.HotelFolioProfileId, x.Code, x.Name, x.Kind, x.AutoCreate, x.AutoRouting, x.AllowSplit, x.AllowMerge, x.AllowTransfer, x.ChargeRouting, x.PaymentRouting, x.EffectiveFrom, x.EffectiveTo, x.Description, x.IsActive, x.UpdatedAtUtc, x.RowVersion);
    private static readonly System.Linq.Expressions.Expression<Func<HotelGuestTypeProfile, HotelGuestTypeProfileDto>> GuestTypeProfileDto = x => new HotelGuestTypeProfileDto(x.HotelGuestTypeProfileId, x.Code, x.Name, x.Kind, x.RequiredFields, x.DefaultRateCode, x.DefaultPaymentMethodCode, x.DefaultMarketCode, x.DefaultSourceCode, x.EffectiveFrom, x.EffectiveTo, x.Description, x.IsActive, x.UpdatedAtUtc, x.RowVersion);
    private static readonly System.Linq.Expressions.Expression<Func<HotelProfileTypeProfile, HotelProfileTypeProfileDto>> ProfileTypeProfileDto = x => new HotelProfileTypeProfileDto(x.HotelProfileTypeProfileId, x.Code, x.Name, x.Kind, x.RequiredFields, x.DefaultRateCode, x.DefaultPaymentMethodCode, x.DefaultMarketCode, x.DefaultSourceCode, x.EffectiveFrom, x.EffectiveTo, x.Description, x.IsActive, x.UpdatedAtUtc, x.RowVersion);
    private static readonly System.Linq.Expressions.Expression<Func<HotelVipLevelProfile, HotelVipLevelProfileDto>> VipLevelProfileDto = x => new HotelVipLevelProfileDto(x.HotelVipLevelProfileId, x.Code, x.Name, x.LevelNumber, x.WelcomeBenefit, x.AmenityBenefit, x.AllowUpgrade, x.DiscountPercent, x.PriorityScore, x.NotificationChannels, x.EffectiveFrom, x.EffectiveTo, x.Description, x.IsActive, x.UpdatedAtUtc, x.RowVersion);
    private static readonly System.Linq.Expressions.Expression<Func<HotelGuestPreferenceProfile, HotelGuestPreferenceProfileDto>> GuestPreferenceProfileDto = x => new HotelGuestPreferenceProfileDto(x.HotelGuestPreferenceProfileId, x.Code, x.Name, x.Category, x.ValueKind, x.DefaultValue, x.Options, x.Scope, x.Priority, x.IsSafetyCritical, x.IsChargeable, x.EffectiveFrom, x.EffectiveTo, x.Description, x.IsActive, x.UpdatedAtUtc, x.RowVersion);
    private static readonly System.Linq.Expressions.Expression<Func<HotelGuestRequestProfile, HotelGuestRequestProfileDto>> GuestRequestProfileDto = x => new HotelGuestRequestProfileDto(x.HotelGuestRequestProfileId, x.Code, x.Name, x.RequestType, x.Priority, x.Department, x.SlaMinutes, x.AutoAssignment, x.NotificationChannels, x.EffectiveFrom, x.EffectiveTo, x.Description, x.IsActive, x.UpdatedAtUtc, x.RowVersion);
    private static readonly System.Linq.Expressions.Expression<Func<HotelReservationTypeProfile, HotelReservationTypeProfileDto>> ReservationTypeProfileDto = x => new HotelReservationTypeProfileDto(x.HotelReservationTypeProfileId, x.Code, x.Name, x.Kind, x.AutoConfirm, x.AutoCancel, x.DepositRequired, x.GuaranteeRequired, x.HoldMinutes, x.CutoffHours, x.MinimumStayNights, x.MaximumStayNights, x.AllowOverbooking, x.EffectiveFrom, x.EffectiveTo, x.Description, x.IsActive, x.UpdatedAtUtc, x.RowVersion);
    private static readonly System.Linq.Expressions.Expression<Func<HotelBookingSourceProfile, HotelBookingSourceProfileDto>> BookingSourceProfileDto = x => new HotelBookingSourceProfileDto(x.HotelBookingSourceProfileId, x.Code, x.Name, x.Kind, x.IsCommissionable, x.RequiresReference, x.DefaultMarketSegmentCode, x.TrackingCode, x.EffectiveFrom, x.EffectiveTo, x.Description, x.IsActive, x.UpdatedAtUtc, x.RowVersion);
    private static readonly System.Linq.Expressions.Expression<Func<HotelBookingChannelProfile, HotelBookingChannelProfileDto>> BookingChannelProfileDto = x => new HotelBookingChannelProfileDto(x.HotelBookingChannelProfileId, x.Code, x.Name, x.Kind, x.IsOnline, x.RequiresExternalConfirmation, x.ConnectorKey, x.EffectiveFrom, x.EffectiveTo, x.Description, x.IsActive, x.UpdatedAtUtc, x.RowVersion);
    private static readonly System.Linq.Expressions.Expression<Func<HotelMarketSegmentProfile, HotelMarketSegmentProfileDto>> MarketSegmentProfileDto = x => new HotelMarketSegmentProfileDto(x.HotelMarketSegmentProfileId, x.Code, x.Name, x.Kind, x.RevenueBucket, x.DefaultRateCode, x.ForecastCategory, x.EffectiveFrom, x.EffectiveTo, x.Description, x.IsActive, x.UpdatedAtUtc, x.RowVersion);
    private static readonly System.Linq.Expressions.Expression<Func<HotelHousekeepingRuleProfile, HotelHousekeepingRuleProfileDto>> HousekeepingRuleProfileDto = x => new HotelHousekeepingRuleProfileDto(x.HotelHousekeepingRuleProfileId, x.Code, x.Name, x.Kind, x.Priority, x.Department, x.SlaMinutes, x.RequiresInspection, x.AutoCreateTask, x.EffectiveFrom, x.EffectiveTo, x.Description, x.IsActive, x.UpdatedAtUtc, x.RowVersion);
    private static readonly System.Linq.Expressions.Expression<Func<HotelWakeupRuleProfile, HotelWakeupRuleProfileDto>> WakeupRuleProfileDto = x => new HotelWakeupRuleProfileDto(x.HotelWakeupRuleProfileId, x.Code, x.Name, x.Kind, x.DefaultLeadMinutes, x.EscalationMinutes, x.ConfirmationRequired, x.NotificationChannel, x.EffectiveFrom, x.EffectiveTo, x.Description, x.IsActive, x.UpdatedAtUtc, x.RowVersion);
    private static readonly System.Linq.Expressions.Expression<Func<HotelComplaintProfile, HotelComplaintProfileDto>> ComplaintProfileDto = x => new HotelComplaintProfileDto(x.HotelComplaintProfileId, x.Code, x.Name, x.Category, x.Severity, x.Department, x.SlaMinutes, x.EscalationMinutes, x.CompensationAllowed, x.EffectiveFrom, x.EffectiveTo, x.Description, x.IsActive, x.UpdatedAtUtc, x.RowVersion);
    private static readonly System.Linq.Expressions.Expression<Func<HotelCompanyProfile, HotelCompanyProfileDto>> CompanyProfileDto = x => new HotelCompanyProfileDto(x.HotelCompanyProfileId, x.Code, x.Name, x.Kind, x.TaxCode, x.BusinessLicense, x.ContactName, x.Phone, x.Email, x.Address, x.CurrencyCode, x.CreditLimit, x.CreditDays, x.RequireContract, x.RequirePurchaseOrder, x.EffectiveFrom, x.EffectiveTo, x.Description, x.IsActive, x.UpdatedAtUtc, x.RowVersion);
    private static readonly System.Linq.Expressions.Expression<Func<HotelTravelAgentProfile, HotelTravelAgentProfileDto>> TravelAgentProfileDto = x => new HotelTravelAgentProfileDto(x.HotelTravelAgentProfileId, x.Code, x.Name, x.Kind, x.IataCode, x.LicenseNumber, x.ContactName, x.Phone, x.Email, x.Address, x.CommissionRuleCode, x.DefaultMarketSegmentCode, x.PaymentTerms, x.CreditLimit, x.EffectiveFrom, x.EffectiveTo, x.Description, x.IsActive, x.UpdatedAtUtc, x.RowVersion);
    private static readonly System.Linq.Expressions.Expression<Func<HotelCommissionRuleProfile, HotelCommissionRuleProfileDto>> CommissionRuleProfileDto = x => new HotelCommissionRuleProfileDto(x.HotelCommissionRuleProfileId, x.Code, x.Name, x.PartnerKind, x.Basis, x.RatePercent, x.FixedAmount, x.CurrencyCode, x.IncludeTax, x.IncludeServiceCharge, x.MinimumAmount, x.MaximumAmount, x.ApprovalRequired, x.EffectiveFrom, x.EffectiveTo, x.Description, x.IsActive, x.UpdatedAtUtc, x.RowVersion);

    public async Task<PageResult<HotelStayPolicyDto>> SearchStayPoliciesAsync(Guid propertyId, string? keyword, bool? isActive, PageRequest page, string? sort, CancellationToken ct = default)
    { var query = dbContext.HotelStayPolicies.AsNoTracking().Where(x => x.PropertyId == propertyId); if (!string.IsNullOrWhiteSpace(keyword)) query = query.Where(x => x.Code.Contains(keyword) || x.Name.Contains(keyword) || (x.Description != null && x.Description.Contains(keyword))); if (isActive is not null) query = query.Where(x => x.IsActive == isActive); query = sort?.ToLowerInvariant() == "updated" ? query.OrderByDescending(x => x.UpdatedAtUtc) : query.OrderBy(x => x.EffectiveFrom).ThenBy(x => x.Code); return await Page(query.Select(StayPolicyDto), page, ct); }
    public Task<HotelCheckInOutRuleDto?> GetCheckInOutRuleDtoAsync(Guid propertyId, Guid id, CancellationToken ct = default) => dbContext.HotelCheckInOutRules.AsNoTracking().Where(x => x.PropertyId == propertyId && x.HotelCheckInOutRuleId == id).Select(CheckInOutRuleDto).SingleOrDefaultAsync(ct);
    public Task<PageResult<HotelCheckInOutRuleDto>> SearchCheckInOutRulesAsync(Guid propertyId, string? keyword, bool? isActive, PageRequest page, string? sort, CancellationToken ct = default)
    { var query = dbContext.HotelCheckInOutRules.AsNoTracking().Where(x => x.PropertyId == propertyId); if (!string.IsNullOrWhiteSpace(keyword)) query = query.Where(x => x.Code.Contains(keyword) || x.Name.Contains(keyword) || (x.Description != null && x.Description.Contains(keyword))); if (isActive is not null) query = query.Where(x => x.IsActive == isActive); query = sort?.ToLowerInvariant() == "updated" ? query.OrderByDescending(x => x.UpdatedAtUtc) : query.OrderBy(x => x.EffectiveFrom).ThenBy(x => x.Code); return Page(query.Select(CheckInOutRuleDto), page, ct); }
    public Task<HotelRoomMoveUpgradeRuleDto?> GetRoomMoveUpgradeRuleDtoAsync(Guid propertyId, Guid id, CancellationToken ct = default) => dbContext.HotelRoomMoveUpgradeRules.AsNoTracking().Where(x => x.PropertyId == propertyId && x.HotelRoomMoveUpgradeRuleId == id).Select(RoomMoveUpgradeRuleDto).SingleOrDefaultAsync(ct);
    public Task<PageResult<HotelRoomMoveUpgradeRuleDto>> SearchRoomMoveUpgradeRulesAsync(Guid propertyId, string? keyword, bool? isActive, PageRequest page, string? sort, CancellationToken ct = default)
    { var query = dbContext.HotelRoomMoveUpgradeRules.AsNoTracking().Where(x => x.PropertyId == propertyId); if (!string.IsNullOrWhiteSpace(keyword)) query = query.Where(x => x.Code.Contains(keyword) || x.Name.Contains(keyword) || (x.Description != null && x.Description.Contains(keyword))); if (isActive is not null) query = query.Where(x => x.IsActive == isActive); query = sort?.ToLowerInvariant() == "updated" ? query.OrderByDescending(x => x.UpdatedAtUtc) : query.OrderBy(x => x.EffectiveFrom).ThenBy(x => x.Code); return Page(query.Select(RoomMoveUpgradeRuleDto), page, ct); }
    public Task<HotelPropertyDocumentDto?> GetPropertyDocumentDtoAsync(Guid propertyId, Guid id, CancellationToken ct = default) => dbContext.HotelPropertyDocuments.AsNoTracking().Where(x => x.PropertyId == propertyId && x.HotelPropertyDocumentId == id).Select(PropertyDocumentDto).SingleOrDefaultAsync(ct);
    public Task<PageResult<HotelPropertyDocumentDto>> SearchPropertyDocumentsAsync(Guid propertyId, string? keyword, bool? isActive, PageRequest page, string? sort, CancellationToken ct = default)
    { var query = dbContext.HotelPropertyDocuments.AsNoTracking().Where(x => x.PropertyId == propertyId); if (!string.IsNullOrWhiteSpace(keyword)) query = query.Where(x => x.DocumentType.Contains(keyword) || x.DocumentNumber.Contains(keyword) || (x.IssuedBy != null && x.IssuedBy.Contains(keyword)) || (x.Notes != null && x.Notes.Contains(keyword))); if (isActive is not null) query = query.Where(x => x.IsActive == isActive); query = sort?.ToLowerInvariant() == "expiry" ? query.OrderBy(x => x.ExpiryDate).ThenBy(x => x.DocumentType) : sort?.ToLowerInvariant() == "updated" ? query.OrderByDescending(x => x.UpdatedAtUtc) : query.OrderBy(x => x.DocumentType).ThenBy(x => x.DocumentNumber); return Page(query.Select(PropertyDocumentDto), page, ct); }
    public Task<HotelPromotionPackageDto?> GetPromotionPackageDtoAsync(Guid propertyId, Guid id, CancellationToken ct = default) => dbContext.HotelPromotionPackages.AsNoTracking().Where(x => x.PropertyId == propertyId && x.HotelPromotionPackageId == id).Select(PromotionPackageDto).SingleOrDefaultAsync(ct);
    public Task<PageResult<HotelPromotionPackageDto>> SearchPromotionPackagesAsync(Guid propertyId, string? keyword, bool? isActive, PageRequest page, string? sort, CancellationToken ct = default)
    { var query = dbContext.HotelPromotionPackages.AsNoTracking().Where(x => x.PropertyId == propertyId); if (!string.IsNullOrWhiteSpace(keyword)) query = query.Where(x => x.Code.Contains(keyword) || x.Name.Contains(keyword) || x.OfferType.Contains(keyword) || (x.Description != null && x.Description.Contains(keyword))); if (isActive is not null) query = query.Where(x => x.IsActive == isActive); query = sort?.ToLowerInvariant() == "updated" ? query.OrderByDescending(x => x.UpdatedAtUtc) : sort?.ToLowerInvariant() == "discount" ? query.OrderByDescending(x => x.DiscountValue) : query.OrderBy(x => x.EffectiveFrom).ThenBy(x => x.Code); return Page(query.Select(PromotionPackageDto), page, ct); }
    public Task<HotelGroupPolicyDto?> GetGroupPolicyDtoAsync(Guid propertyId, Guid id, CancellationToken ct = default) => dbContext.HotelGroupPolicies.AsNoTracking().Where(x => x.PropertyId == propertyId && x.HotelGroupPolicyId == id).Select(GroupPolicyDto).SingleOrDefaultAsync(ct);
    public Task<PageResult<HotelGroupPolicyDto>> SearchGroupPoliciesAsync(Guid propertyId, string? keyword, bool? isActive, PageRequest page, string? sort, CancellationToken ct = default)
    { var query = dbContext.HotelGroupPolicies.AsNoTracking().Where(x => x.PropertyId == propertyId); if (!string.IsNullOrWhiteSpace(keyword)) query = query.Where(x => x.Code.Contains(keyword) || x.Name.Contains(keyword) || x.DefaultRouting.Contains(keyword) || (x.Description != null && x.Description.Contains(keyword))); if (isActive is not null) query = query.Where(x => x.IsActive == isActive); query = sort?.ToLowerInvariant() == "updated" ? query.OrderByDescending(x => x.UpdatedAtUtc) : query.OrderBy(x => x.EffectiveFrom).ThenBy(x => x.Code); return Page(query.Select(GroupPolicyDto), page, ct); }
    public Task<HotelCashierShiftPolicyDto?> GetCashierShiftPolicyDtoAsync(Guid propertyId, Guid id, CancellationToken ct = default) => dbContext.HotelCashierShiftPolicies.AsNoTracking().Where(x => x.PropertyId == propertyId && x.HotelCashierShiftPolicyId == id).Select(CashierShiftPolicyDto).SingleOrDefaultAsync(ct);
    public Task<PageResult<HotelCashierShiftPolicyDto>> SearchCashierShiftPoliciesAsync(Guid propertyId, string? keyword, bool? isActive, PageRequest page, string? sort, CancellationToken ct = default)
    { var query = dbContext.HotelCashierShiftPolicies.AsNoTracking().Where(x => x.PropertyId == propertyId); if (!string.IsNullOrWhiteSpace(keyword)) query = query.Where(x => x.Code.Contains(keyword) || x.Name.Contains(keyword) || (x.SupervisorRole != null && x.SupervisorRole.Contains(keyword)) || (x.Description != null && x.Description.Contains(keyword))); if (isActive is not null) query = query.Where(x => x.IsActive == isActive); query = sort?.ToLowerInvariant() == "updated" ? query.OrderByDescending(x => x.UpdatedAtUtc) : query.OrderBy(x => x.EffectiveFrom).ThenBy(x => x.Code); return Page(query.Select(CashierShiftPolicyDto), page, ct); }
    public Task<HotelNightAuditPolicyDto?> GetNightAuditPolicyDtoAsync(Guid propertyId, Guid id, CancellationToken ct = default) => dbContext.HotelNightAuditPolicies.AsNoTracking().Where(x => x.PropertyId == propertyId && x.HotelNightAuditPolicyId == id).Select(NightAuditPolicyDto).SingleOrDefaultAsync(ct);
    public Task<PageResult<HotelNightAuditPolicyDto>> SearchNightAuditPoliciesAsync(Guid propertyId, string? keyword, bool? isActive, PageRequest page, string? sort, CancellationToken ct = default)
    { var query = dbContext.HotelNightAuditPolicies.AsNoTracking().Where(x => x.PropertyId == propertyId); if (!string.IsNullOrWhiteSpace(keyword)) query = query.Where(x => x.Code.Contains(keyword) || x.Name.Contains(keyword) || (x.SupervisorRole != null && x.SupervisorRole.Contains(keyword)) || (x.Description != null && x.Description.Contains(keyword))); if (isActive is not null) query = query.Where(x => x.IsActive == isActive); query = sort?.ToLowerInvariant() == "updated" ? query.OrderByDescending(x => x.UpdatedAtUtc) : query.OrderBy(x => x.EffectiveFrom).ThenBy(x => x.Code); return Page(query.Select(NightAuditPolicyDto), page, ct); }
    public Task<HotelApprovalMatrixRuleDto?> GetApprovalMatrixRuleDtoAsync(Guid propertyId, Guid id, CancellationToken ct = default) => dbContext.HotelApprovalMatrixRules.AsNoTracking().Where(x => x.PropertyId == propertyId && x.HotelApprovalMatrixRuleId == id).Select(ApprovalMatrixRuleDto).SingleOrDefaultAsync(ct);
    public Task<PageResult<HotelApprovalMatrixRuleDto>> SearchApprovalMatrixRulesAsync(Guid propertyId, string? keyword, bool? isActive, PageRequest page, string? sort, CancellationToken ct = default)
    { var query = dbContext.HotelApprovalMatrixRules.AsNoTracking().Where(x => x.PropertyId == propertyId); if (!string.IsNullOrWhiteSpace(keyword)) query = query.Where(x => x.Code.Contains(keyword) || x.Name.Contains(keyword) || x.Domain.Contains(keyword) || x.Action.Contains(keyword) || x.RequiredRole.Contains(keyword)); if (isActive is not null) query = query.Where(x => x.IsActive == isActive); query = sort?.ToLowerInvariant() == "updated" ? query.OrderByDescending(x => x.UpdatedAtUtc) : query.OrderBy(x => x.Domain).ThenBy(x => x.Action).ThenBy(x => x.ThresholdAmount); return Page(query.Select(ApprovalMatrixRuleDto), page, ct); }
    public Task<HotelFOPermissionPolicyDto?> GetFOPermissionPolicyDtoAsync(Guid propertyId, Guid id, CancellationToken ct = default) => dbContext.HotelFOPermissionPolicies.AsNoTracking().Where(x => x.PropertyId == propertyId && x.HotelFOPermissionPolicyId == id).Select(FOPermissionPolicyDto).SingleOrDefaultAsync(ct);
    public Task<PageResult<HotelFOPermissionPolicyDto>> SearchFOPermissionPoliciesAsync(Guid propertyId, string? keyword, bool? isActive, PageRequest page, string? sort, CancellationToken ct = default)
    { var query = dbContext.HotelFOPermissionPolicies.AsNoTracking().Where(x => x.PropertyId == propertyId); if (!string.IsNullOrWhiteSpace(keyword)) query = query.Where(x => x.Code.Contains(keyword) || x.Name.Contains(keyword) || x.Module.Contains(keyword) || x.Action.Contains(keyword) || x.RequiredRole.Contains(keyword) || x.DataScope.Contains(keyword)); if (isActive is not null) query = query.Where(x => x.IsActive == isActive); query = sort?.ToLowerInvariant() == "updated" ? query.OrderByDescending(x => x.UpdatedAtUtc) : query.OrderBy(x => x.Module).ThenBy(x => x.Action).ThenBy(x => x.Code); return Page(query.Select(FOPermissionPolicyDto), page, ct); }
    public Task<HotelFormPrinterProfileDto?> GetFormPrinterProfileDtoAsync(Guid propertyId, Guid id, CancellationToken ct = default) => dbContext.HotelFormPrinterProfiles.AsNoTracking().Where(x => x.PropertyId == propertyId && x.HotelFormPrinterProfileId == id).Select(FormPrinterProfileDto).SingleOrDefaultAsync(ct);
    public Task<PageResult<HotelFormPrinterProfileDto>> SearchFormPrinterProfilesAsync(Guid propertyId, string? keyword, bool? isActive, PageRequest page, string? sort, CancellationToken ct = default)
    { var query = dbContext.HotelFormPrinterProfiles.AsNoTracking().Where(x => x.PropertyId == propertyId); if (!string.IsNullOrWhiteSpace(keyword)) query = query.Where(x => x.Code.Contains(keyword) || x.Name.Contains(keyword) || x.DocumentType.Contains(keyword) || x.TemplateReference.Contains(keyword) || x.PrinterName.Contains(keyword)); if (isActive is not null) query = query.Where(x => x.IsActive == isActive); query = sort?.ToLowerInvariant() == "updated" ? query.OrderByDescending(x => x.UpdatedAtUtc) : query.OrderBy(x => x.DocumentType).ThenBy(x => x.Code); return Page(query.Select(FormPrinterProfileDto), page, ct); }
    public Task<HotelMessagingIntegrationProfileDto?> GetMessagingIntegrationProfileDtoAsync(Guid propertyId, Guid id, CancellationToken ct = default) => dbContext.HotelMessagingIntegrationProfiles.AsNoTracking().Where(x => x.PropertyId == propertyId && x.HotelMessagingIntegrationProfileId == id).Select(MessagingIntegrationProfileDto).SingleOrDefaultAsync(ct);
    public Task<PageResult<HotelMessagingIntegrationProfileDto>> SearchMessagingIntegrationProfilesAsync(Guid propertyId, string? keyword, bool? isActive, PageRequest page, string? sort, CancellationToken ct = default)
    { var query = dbContext.HotelMessagingIntegrationProfiles.AsNoTracking().Where(x => x.PropertyId == propertyId); if (!string.IsNullOrWhiteSpace(keyword)) query = query.Where(x => x.Code.Contains(keyword) || x.Name.Contains(keyword) || x.Channel.Contains(keyword) || x.Provider.Contains(keyword) || x.Endpoint.Contains(keyword)); if (isActive is not null) query = query.Where(x => x.IsActive == isActive); query = sort?.ToLowerInvariant() == "updated" ? query.OrderByDescending(x => x.UpdatedAtUtc) : query.OrderBy(x => x.Channel).ThenBy(x => x.Code); return Page(query.Select(MessagingIntegrationProfileDto), page, ct); }
    public Task<HotelPostedCorrectionPolicyDto?> GetPostedCorrectionPolicyDtoAsync(Guid propertyId, Guid id, CancellationToken ct = default) => dbContext.HotelPostedCorrectionPolicies.AsNoTracking().Where(x => x.PropertyId == propertyId && x.HotelPostedCorrectionPolicyId == id).Select(PostedCorrectionPolicyDto).SingleOrDefaultAsync(ct);
    public Task<HotelPostedCorrectionPolicyDto?> GetActivePostedCorrectionPolicyAsync(Guid propertyId, string domain, DateTime asOfUtc, CancellationToken ct = default) => dbContext.HotelPostedCorrectionPolicies.AsNoTracking().Where(x => x.PropertyId == propertyId && x.IsActive && x.Domain == domain && x.EffectiveFrom <= asOfUtc.Date && x.EffectiveTo >= asOfUtc.Date).OrderByDescending(x => x.EffectiveFrom).ThenByDescending(x => x.UpdatedAtUtc).Select(PostedCorrectionPolicyDto).FirstOrDefaultAsync(ct);
    public Task<PageResult<HotelPostedCorrectionPolicyDto>> SearchPostedCorrectionPoliciesAsync(Guid propertyId, string? keyword, bool? isActive, PageRequest page, string? sort, CancellationToken ct = default)
    { var query = dbContext.HotelPostedCorrectionPolicies.AsNoTracking().Where(x => x.PropertyId == propertyId); if (!string.IsNullOrWhiteSpace(keyword)) query = query.Where(x => x.Code.Contains(keyword) || x.Name.Contains(keyword) || x.Domain.Contains(keyword) || x.AllowedRoles.Contains(keyword)); if (isActive is not null) query = query.Where(x => x.IsActive == isActive); query = sort?.ToLowerInvariant() == "updated" ? query.OrderByDescending(x => x.UpdatedAtUtc) : query.OrderBy(x => x.Domain).ThenBy(x => x.Code); return Page(query.Select(PostedCorrectionPolicyDto), page, ct); }
    public Task<HotelBusinessRuleProfileDto?> GetBusinessRuleProfileDtoAsync(Guid propertyId, Guid id, CancellationToken ct = default) => dbContext.HotelBusinessRuleProfiles.AsNoTracking().Where(x => x.PropertyId == propertyId && x.HotelBusinessRuleProfileId == id).Select(BusinessRuleProfileDto).SingleOrDefaultAsync(ct);
    public Task<PageResult<HotelBusinessRuleProfileDto>> SearchBusinessRuleProfilesAsync(Guid propertyId, string? keyword, bool? isActive, PageRequest page, string? sort, CancellationToken ct = default)
    { var query = dbContext.HotelBusinessRuleProfiles.AsNoTracking().Where(x => x.PropertyId == propertyId); if (!string.IsNullOrWhiteSpace(keyword)) query = query.Where(x => x.Code.Contains(keyword) || x.Name.Contains(keyword) || x.Domain.Contains(keyword) || x.ConditionReference.Contains(keyword)); if (isActive is not null) query = query.Where(x => x.IsActive == isActive); query = sort?.ToLowerInvariant() == "priority" ? query.OrderBy(x => x.Priority).ThenBy(x => x.Code) : sort?.ToLowerInvariant() == "updated" ? query.OrderByDescending(x => x.UpdatedAtUtc) : query.OrderBy(x => x.Domain).ThenBy(x => x.Code); return Page(query.Select(BusinessRuleProfileDto), page, ct); }
    public Task<HotelTaxServiceChargeRuleDto?> GetTaxServiceChargeRuleDtoAsync(Guid propertyId, Guid id, CancellationToken ct = default) => dbContext.HotelTaxServiceChargeRules.AsNoTracking().Where(x => x.PropertyId == propertyId && x.HotelTaxServiceChargeRuleId == id).Select(TaxServiceChargeRuleDto).SingleOrDefaultAsync(ct);
    public async Task<IReadOnlyList<HotelTaxServiceChargeRuleDto>> GetEffectiveTaxServiceChargeRulesAsync(Guid propertyId, DateOnly businessDate, string? applicableService = null, string? applicableRatePlan = null, CancellationToken ct = default)
    {
        var from = businessDate.ToDateTime(TimeOnly.MinValue);
        var to = businessDate.ToDateTime(TimeOnly.MaxValue);
        var candidates = await dbContext.HotelTaxServiceChargeRules
            .AsNoTracking()
            .Where(x => x.PropertyId == propertyId && x.IsActive
                && x.EffectiveFrom <= to && x.EffectiveTo >= from)
            .Select(TaxServiceChargeRuleDto)
            .ToListAsync(ct);

        var serviceKey = NormalizeRuleKey(applicableService);
        var ratePlanKey = NormalizeRuleKey(applicableRatePlan);
        return candidates
            .Where(x => MatchesRuleKey(x.ApplicableService, serviceKey)
                && MatchesRuleKey(x.ApplicableRatePlan, ratePlanKey))
            .OrderBy(x => IsWildcardRuleKey(x.ApplicableService) ? 1 : 0)
            .ThenBy(x => IsWildcardRuleKey(x.ApplicableRatePlan) ? 1 : 0)
            .ThenByDescending(x => x.EffectiveFrom)
            .ThenByDescending(x => x.UpdatedAtUtc)
            .ThenBy(x => x.Code)
            .ToArray();
    }
    public Task<PageResult<HotelTaxServiceChargeRuleDto>> SearchTaxServiceChargeRulesAsync(Guid propertyId, HotelTaxServiceChargeKind? kind, string? keyword, bool? isActive, PageRequest page, string? sort, CancellationToken ct = default)
    { var query = dbContext.HotelTaxServiceChargeRules.AsNoTracking().Where(x => x.PropertyId == propertyId); if (kind is not null) query = query.Where(x => x.Kind == kind); if (!string.IsNullOrWhiteSpace(keyword)) query = query.Where(x => x.Code.Contains(keyword) || x.Name.Contains(keyword) || x.ApplicableService.Contains(keyword) || (x.Description != null && x.Description.Contains(keyword))); if (isActive is not null) query = query.Where(x => x.IsActive == isActive); query = sort?.ToLowerInvariant() == "updated" ? query.OrderByDescending(x => x.UpdatedAtUtc) : query.OrderBy(x => x.Kind).ThenBy(x => x.EffectiveFrom).ThenBy(x => x.Code); return Page(query.Select(TaxServiceChargeRuleDto), page, ct); }
    public Task<HotelPaymentMethodProfileDto?> GetPaymentMethodProfileDtoAsync(Guid propertyId, Guid id, CancellationToken ct = default) => dbContext.HotelPaymentMethodProfiles.AsNoTracking().Where(x => x.PropertyId == propertyId && x.HotelPaymentMethodProfileId == id).Select(PaymentMethodProfileDto).SingleOrDefaultAsync(ct);
    public Task<PageResult<HotelPaymentMethodProfileDto>> SearchPaymentMethodProfilesAsync(Guid propertyId, HotelPaymentMethodKind? kind, string? keyword, bool? isActive, PageRequest page, string? sort, CancellationToken ct = default)
    { var query = dbContext.HotelPaymentMethodProfiles.AsNoTracking().Where(x => x.PropertyId == propertyId); if (kind is not null) query = query.Where(x => x.Kind == kind); if (!string.IsNullOrWhiteSpace(keyword)) query = query.Where(x => x.Code.Contains(keyword) || x.Name.Contains(keyword) || x.CurrencyCode.Contains(keyword) || (x.BankAccount != null && x.BankAccount.Contains(keyword)) || (x.CardTerminal != null && x.CardTerminal.Contains(keyword))); if (isActive is not null) query = query.Where(x => x.IsActive == isActive); query = sort?.ToLowerInvariant() == "updated" ? query.OrderByDescending(x => x.UpdatedAtUtc) : query.OrderBy(x => x.Kind).ThenBy(x => x.Code); return Page(query.Select(PaymentMethodProfileDto), page, ct); }
    public Task<HotelDepositPolicyDto?> GetDepositPolicyDtoAsync(Guid propertyId, Guid id, CancellationToken ct = default) => dbContext.HotelDepositPolicies.AsNoTracking().Where(x => x.PropertyId == propertyId && x.HotelDepositPolicyId == id).Select(DepositPolicyDto).SingleOrDefaultAsync(ct);
    public Task<PageResult<HotelDepositPolicyDto>> SearchDepositPoliciesAsync(Guid propertyId, string? keyword, bool? isActive, PageRequest page, string? sort, CancellationToken ct = default)
    { var query = dbContext.HotelDepositPolicies.AsNoTracking().Where(x => x.PropertyId == propertyId); if (!string.IsNullOrWhiteSpace(keyword)) query = query.Where(x => x.Code.Contains(keyword) || x.Name.Contains(keyword) || (x.Description != null && x.Description.Contains(keyword))); if (isActive is not null) query = query.Where(x => x.IsActive == isActive); query = sort?.ToLowerInvariant() == "updated" ? query.OrderByDescending(x => x.UpdatedAtUtc) : query.OrderBy(x => x.EffectiveFrom).ThenBy(x => x.Code); return Page(query.Select(DepositPolicyDto), page, ct); }
    public Task<HotelDiscountPolicyDto?> GetDiscountPolicyDtoAsync(Guid propertyId, Guid id, CancellationToken ct = default) => dbContext.HotelDiscountPolicies.AsNoTracking().Where(x => x.PropertyId == propertyId && x.HotelDiscountPolicyId == id).Select(DiscountPolicyDto).SingleOrDefaultAsync(ct);
    public Task<PageResult<HotelDiscountPolicyDto>> SearchDiscountPoliciesAsync(Guid propertyId, string? keyword, bool? isActive, PageRequest page, string? sort, CancellationToken ct = default)
    { var query = dbContext.HotelDiscountPolicies.AsNoTracking().Where(x => x.PropertyId == propertyId); if (!string.IsNullOrWhiteSpace(keyword)) query = query.Where(x => x.Code.Contains(keyword) || x.Name.Contains(keyword) || x.AllowedRoles.Contains(keyword) || (x.Description != null && x.Description.Contains(keyword))); if (isActive is not null) query = query.Where(x => x.IsActive == isActive); query = sort?.ToLowerInvariant() == "updated" ? query.OrderByDescending(x => x.UpdatedAtUtc) : query.OrderBy(x => x.EffectiveFrom).ThenBy(x => x.Code); return Page(query.Select(DiscountPolicyDto), page, ct); }
    public Task<HotelChargeCodeProfileDto?> GetChargeCodeProfileDtoAsync(Guid propertyId, Guid id, CancellationToken ct = default) => dbContext.HotelChargeCodeProfiles.AsNoTracking().Where(x => x.PropertyId == propertyId && x.HotelChargeCodeProfileId == id).Select(ChargeCodeProfileDto).SingleOrDefaultAsync(ct);
    public Task<PageResult<HotelChargeCodeProfileDto>> SearchChargeCodeProfilesAsync(Guid propertyId, string? category, string? keyword, bool? isActive, PageRequest page, string? sort, CancellationToken ct = default)
    { var query = dbContext.HotelChargeCodeProfiles.AsNoTracking().Where(x => x.PropertyId == propertyId); if (!string.IsNullOrWhiteSpace(category)) query = query.Where(x => x.Category == category); if (!string.IsNullOrWhiteSpace(keyword)) query = query.Where(x => x.Code.Contains(keyword) || x.Name.Contains(keyword) || x.Category.Contains(keyword) || (x.RevenueAccount != null && x.RevenueAccount.Contains(keyword)) || (x.TaxCode != null && x.TaxCode.Contains(keyword))); if (isActive is not null) query = query.Where(x => x.IsActive == isActive); query = sort?.ToLowerInvariant() == "updated" ? query.OrderByDescending(x => x.UpdatedAtUtc) : query.OrderBy(x => x.Category).ThenBy(x => x.Code); return Page(query.Select(ChargeCodeProfileDto), page, ct); }
    public Task<HotelFolioProfileDto?> GetFolioProfileDtoAsync(Guid propertyId, Guid id, CancellationToken ct = default) => dbContext.HotelFolioProfiles.AsNoTracking().Where(x => x.PropertyId == propertyId && x.HotelFolioProfileId == id).Select(FolioProfileDto).SingleOrDefaultAsync(ct);
    public Task<PageResult<HotelFolioProfileDto>> SearchFolioProfilesAsync(Guid propertyId, HotelFolioTypeKind? kind, string? keyword, bool? isActive, PageRequest page, string? sort, CancellationToken ct = default)
    { var query = dbContext.HotelFolioProfiles.AsNoTracking().Where(x => x.PropertyId == propertyId); if (kind is not null) query = query.Where(x => x.Kind == kind); if (!string.IsNullOrWhiteSpace(keyword)) query = query.Where(x => x.Code.Contains(keyword) || x.Name.Contains(keyword) || (x.Description != null && x.Description.Contains(keyword))); if (isActive is not null) query = query.Where(x => x.IsActive == isActive); query = sort?.ToLowerInvariant() == "updated" ? query.OrderByDescending(x => x.UpdatedAtUtc) : query.OrderBy(x => x.Kind).ThenBy(x => x.Code); return Page(query.Select(FolioProfileDto), page, ct); }
    public Task<HotelGuestTypeProfileDto?> GetGuestTypeProfileDtoAsync(Guid propertyId, Guid id, CancellationToken ct = default) => dbContext.HotelGuestTypeProfiles.AsNoTracking().Where(x => x.PropertyId == propertyId && x.HotelGuestTypeProfileId == id).Select(GuestTypeProfileDto).SingleOrDefaultAsync(ct);
    public Task<PageResult<HotelGuestTypeProfileDto>> SearchGuestTypeProfilesAsync(Guid propertyId, HotelGuestTypeKind? kind, string? keyword, bool? isActive, PageRequest page, string? sort, CancellationToken ct = default)
    { var query = dbContext.HotelGuestTypeProfiles.AsNoTracking().Where(x => x.PropertyId == propertyId); if (kind is not null) query = query.Where(x => x.Kind == kind); if (!string.IsNullOrWhiteSpace(keyword)) query = query.Where(x => x.Code.Contains(keyword) || x.Name.Contains(keyword) || (x.Description != null && x.Description.Contains(keyword)) || (x.RequiredFields != null && x.RequiredFields.Contains(keyword))); if (isActive is not null) query = query.Where(x => x.IsActive == isActive); query = sort?.ToLowerInvariant() == "updated" ? query.OrderByDescending(x => x.UpdatedAtUtc) : query.OrderBy(x => x.Kind).ThenBy(x => x.Code); return Page(query.Select(GuestTypeProfileDto), page, ct); }
    public Task<HotelProfileTypeProfileDto?> GetProfileTypeProfileDtoAsync(Guid propertyId, Guid id, CancellationToken ct = default) => dbContext.HotelProfileTypeProfiles.AsNoTracking().Where(x => x.PropertyId == propertyId && x.HotelProfileTypeProfileId == id).Select(ProfileTypeProfileDto).SingleOrDefaultAsync(ct);
    public Task<PageResult<HotelProfileTypeProfileDto>> SearchProfileTypeProfilesAsync(Guid propertyId, HotelProfileTypeKind? kind, string? keyword, bool? isActive, PageRequest page, string? sort, CancellationToken ct = default)
    { var query = dbContext.HotelProfileTypeProfiles.AsNoTracking().Where(x => x.PropertyId == propertyId); if (kind is not null) query = query.Where(x => x.Kind == kind); if (!string.IsNullOrWhiteSpace(keyword)) query = query.Where(x => x.Code.Contains(keyword) || x.Name.Contains(keyword) || (x.Description != null && x.Description.Contains(keyword)) || (x.RequiredFields != null && x.RequiredFields.Contains(keyword))); if (isActive is not null) query = query.Where(x => x.IsActive == isActive); query = sort?.ToLowerInvariant() == "updated" ? query.OrderByDescending(x => x.UpdatedAtUtc) : query.OrderBy(x => x.Kind).ThenBy(x => x.Code); return Page(query.Select(ProfileTypeProfileDto), page, ct); }
    public Task<HotelVipLevelProfileDto?> GetVipLevelProfileDtoAsync(Guid propertyId, Guid id, CancellationToken ct = default) => dbContext.HotelVipLevelProfiles.AsNoTracking().Where(x => x.PropertyId == propertyId && x.HotelVipLevelProfileId == id).Select(VipLevelProfileDto).SingleOrDefaultAsync(ct);
    public Task<PageResult<HotelVipLevelProfileDto>> SearchVipLevelProfilesAsync(Guid propertyId, string? keyword, bool? isActive, PageRequest page, string? sort, CancellationToken ct = default)
    { var query = dbContext.HotelVipLevelProfiles.AsNoTracking().Where(x => x.PropertyId == propertyId); if (!string.IsNullOrWhiteSpace(keyword)) query = query.Where(x => x.Code.Contains(keyword) || x.Name.Contains(keyword) || (x.WelcomeBenefit != null && x.WelcomeBenefit.Contains(keyword)) || (x.AmenityBenefit != null && x.AmenityBenefit.Contains(keyword))); if (isActive is not null) query = query.Where(x => x.IsActive == isActive); query = sort?.ToLowerInvariant() == "updated" ? query.OrderByDescending(x => x.UpdatedAtUtc) : query.OrderBy(x => x.LevelNumber).ThenBy(x => x.Code); return Page(query.Select(VipLevelProfileDto), page, ct); }
    public Task<HotelGuestPreferenceProfileDto?> GetGuestPreferenceProfileDtoAsync(Guid propertyId, Guid id, CancellationToken ct = default) => dbContext.HotelGuestPreferenceProfiles.AsNoTracking().Where(x => x.PropertyId == propertyId && x.HotelGuestPreferenceProfileId == id).Select(GuestPreferenceProfileDto).SingleOrDefaultAsync(ct);
    public Task<PageResult<HotelGuestPreferenceProfileDto>> SearchGuestPreferenceProfilesAsync(Guid propertyId, string? category, string? keyword, bool? isActive, PageRequest page, string? sort, CancellationToken ct = default)
    { var query = dbContext.HotelGuestPreferenceProfiles.AsNoTracking().Where(x => x.PropertyId == propertyId); if (!string.IsNullOrWhiteSpace(category)) query = query.Where(x => x.Category == category); if (!string.IsNullOrWhiteSpace(keyword)) query = query.Where(x => x.Code.Contains(keyword) || x.Name.Contains(keyword) || x.ValueKind.Contains(keyword) || (x.Options != null && x.Options.Contains(keyword)) || (x.Description != null && x.Description.Contains(keyword))); if (isActive is not null) query = query.Where(x => x.IsActive == isActive); query = sort?.ToLowerInvariant() == "priority" ? query.OrderBy(x => x.Priority).ThenBy(x => x.Code) : sort?.ToLowerInvariant() == "updated" ? query.OrderByDescending(x => x.UpdatedAtUtc) : query.OrderBy(x => x.Category).ThenBy(x => x.Code); return Page(query.Select(GuestPreferenceProfileDto), page, ct); }
    public Task<HotelGuestRequestProfileDto?> GetGuestRequestProfileDtoAsync(Guid propertyId, Guid id, CancellationToken ct = default) => dbContext.HotelGuestRequestProfiles.AsNoTracking().Where(x => x.PropertyId == propertyId && x.HotelGuestRequestProfileId == id).Select(GuestRequestProfileDto).SingleOrDefaultAsync(ct);
    public Task<PageResult<HotelGuestRequestProfileDto>> SearchGuestRequestProfilesAsync(Guid propertyId, string? requestType, string? keyword, bool? isActive, PageRequest page, string? sort, CancellationToken ct = default)
    { var query = dbContext.HotelGuestRequestProfiles.AsNoTracking().Where(x => x.PropertyId == propertyId); if (!string.IsNullOrWhiteSpace(requestType)) query = query.Where(x => x.RequestType == requestType); if (!string.IsNullOrWhiteSpace(keyword)) query = query.Where(x => x.Code.Contains(keyword) || x.Name.Contains(keyword) || x.Department.Contains(keyword) || (x.AutoAssignment != null && x.AutoAssignment.Contains(keyword)) || (x.Description != null && x.Description.Contains(keyword))); if (isActive is not null) query = query.Where(x => x.IsActive == isActive); query = sort?.ToLowerInvariant() == "sla" ? query.OrderBy(x => x.SlaMinutes).ThenBy(x => x.Priority) : sort?.ToLowerInvariant() == "updated" ? query.OrderByDescending(x => x.UpdatedAtUtc) : query.OrderBy(x => x.Priority).ThenBy(x => x.Code); return Page(query.Select(GuestRequestProfileDto), page, ct); }
    public Task<HotelReservationTypeProfileDto?> GetReservationTypeProfileDtoAsync(Guid propertyId, Guid id, CancellationToken ct = default) => dbContext.HotelReservationTypeProfiles.AsNoTracking().Where(x => x.PropertyId == propertyId && x.HotelReservationTypeProfileId == id).Select(ReservationTypeProfileDto).SingleOrDefaultAsync(ct);
    public Task<PageResult<HotelReservationTypeProfileDto>> SearchReservationTypeProfilesAsync(Guid propertyId, HotelReservationTypeKind? kind, string? keyword, bool? isActive, PageRequest page, string? sort, CancellationToken ct = default)
    { var query = dbContext.HotelReservationTypeProfiles.AsNoTracking().Where(x => x.PropertyId == propertyId); if (kind is not null) query = query.Where(x => x.Kind == kind); if (!string.IsNullOrWhiteSpace(keyword)) query = query.Where(x => x.Code.Contains(keyword) || x.Name.Contains(keyword) || (x.Description != null && x.Description.Contains(keyword))); if (isActive is not null) query = query.Where(x => x.IsActive == isActive); query = sort?.ToLowerInvariant() == "updated" ? query.OrderByDescending(x => x.UpdatedAtUtc) : query.OrderBy(x => x.Kind).ThenBy(x => x.Code); return Page(query.Select(ReservationTypeProfileDto), page, ct); }
    public Task<HotelBookingSourceProfileDto?> GetBookingSourceProfileDtoAsync(Guid propertyId, Guid id, CancellationToken ct = default) => dbContext.HotelBookingSourceProfiles.AsNoTracking().Where(x => x.PropertyId == propertyId && x.HotelBookingSourceProfileId == id).Select(BookingSourceProfileDto).SingleOrDefaultAsync(ct);
    public Task<PageResult<HotelBookingSourceProfileDto>> SearchBookingSourceProfilesAsync(Guid propertyId, HotelBookingSourceKind? kind, string? keyword, bool? isActive, PageRequest page, string? sort, CancellationToken ct = default)
    { var query = dbContext.HotelBookingSourceProfiles.AsNoTracking().Where(x => x.PropertyId == propertyId); if (kind is not null) query = query.Where(x => x.Kind == kind); if (!string.IsNullOrWhiteSpace(keyword)) query = query.Where(x => x.Code.Contains(keyword) || x.Name.Contains(keyword) || (x.TrackingCode != null && x.TrackingCode.Contains(keyword)) || (x.Description != null && x.Description.Contains(keyword))); if (isActive is not null) query = query.Where(x => x.IsActive == isActive); query = sort?.ToLowerInvariant() == "updated" ? query.OrderByDescending(x => x.UpdatedAtUtc) : query.OrderBy(x => x.Kind).ThenBy(x => x.Code); return Page(query.Select(BookingSourceProfileDto), page, ct); }
    public Task<HotelBookingChannelProfileDto?> GetBookingChannelProfileDtoAsync(Guid propertyId, Guid id, CancellationToken ct = default) => dbContext.HotelBookingChannelProfiles.AsNoTracking().Where(x => x.PropertyId == propertyId && x.HotelBookingChannelProfileId == id).Select(BookingChannelProfileDto).SingleOrDefaultAsync(ct);
    public Task<PageResult<HotelBookingChannelProfileDto>> SearchBookingChannelProfilesAsync(Guid propertyId, HotelBookingChannelKind? kind, string? keyword, bool? isActive, PageRequest page, string? sort, CancellationToken ct = default)
    { var query = dbContext.HotelBookingChannelProfiles.AsNoTracking().Where(x => x.PropertyId == propertyId); if (kind is not null) query = query.Where(x => x.Kind == kind); if (!string.IsNullOrWhiteSpace(keyword)) query = query.Where(x => x.Code.Contains(keyword) || x.Name.Contains(keyword) || (x.ConnectorKey != null && x.ConnectorKey.Contains(keyword)) || (x.Description != null && x.Description.Contains(keyword))); if (isActive is not null) query = query.Where(x => x.IsActive == isActive); query = sort?.ToLowerInvariant() == "updated" ? query.OrderByDescending(x => x.UpdatedAtUtc) : query.OrderBy(x => x.Kind).ThenBy(x => x.Code); return Page(query.Select(BookingChannelProfileDto), page, ct); }
    public Task<HotelMarketSegmentProfileDto?> GetMarketSegmentProfileDtoAsync(Guid propertyId, Guid id, CancellationToken ct = default) => dbContext.HotelMarketSegmentProfiles.AsNoTracking().Where(x => x.PropertyId == propertyId && x.HotelMarketSegmentProfileId == id).Select(MarketSegmentProfileDto).SingleOrDefaultAsync(ct);
    public Task<PageResult<HotelMarketSegmentProfileDto>> SearchMarketSegmentProfilesAsync(Guid propertyId, HotelMarketSegmentKind? kind, string? keyword, bool? isActive, PageRequest page, string? sort, CancellationToken ct = default)
    { var query = dbContext.HotelMarketSegmentProfiles.AsNoTracking().Where(x => x.PropertyId == propertyId); if (kind is not null) query = query.Where(x => x.Kind == kind); if (!string.IsNullOrWhiteSpace(keyword)) query = query.Where(x => x.Code.Contains(keyword) || x.Name.Contains(keyword) || x.RevenueBucket.Contains(keyword) || (x.DefaultRateCode != null && x.DefaultRateCode.Contains(keyword)) || (x.Description != null && x.Description.Contains(keyword))); if (isActive is not null) query = query.Where(x => x.IsActive == isActive); query = sort?.ToLowerInvariant() == "updated" ? query.OrderByDescending(x => x.UpdatedAtUtc) : query.OrderBy(x => x.Kind).ThenBy(x => x.Code); return Page(query.Select(MarketSegmentProfileDto), page, ct); }
    public Task<HotelHousekeepingRuleProfileDto?> GetHousekeepingRuleProfileDtoAsync(Guid propertyId, Guid id, CancellationToken ct = default) => dbContext.HotelHousekeepingRuleProfiles.AsNoTracking().Where(x => x.PropertyId == propertyId && x.HotelHousekeepingRuleProfileId == id).Select(HousekeepingRuleProfileDto).SingleOrDefaultAsync(ct);
    public Task<PageResult<HotelHousekeepingRuleProfileDto>> SearchHousekeepingRuleProfilesAsync(Guid propertyId, HotelHousekeepingRuleKind? kind, string? keyword, bool? isActive, PageRequest page, string? sort, CancellationToken ct = default)
    { var query = dbContext.HotelHousekeepingRuleProfiles.AsNoTracking().Where(x => x.PropertyId == propertyId); if (kind is not null) query = query.Where(x => x.Kind == kind); if (!string.IsNullOrWhiteSpace(keyword)) query = query.Where(x => x.Code.Contains(keyword) || x.Name.Contains(keyword) || x.Department.Contains(keyword) || (x.Description != null && x.Description.Contains(keyword))); if (isActive is not null) query = query.Where(x => x.IsActive == isActive); query = sort?.ToLowerInvariant() == "updated" ? query.OrderByDescending(x => x.UpdatedAtUtc) : query.OrderBy(x => x.Priority).ThenBy(x => x.Code); return Page(query.Select(HousekeepingRuleProfileDto), page, ct); }
    public Task<HotelWakeupRuleProfileDto?> GetWakeupRuleProfileDtoAsync(Guid propertyId, Guid id, CancellationToken ct = default) => dbContext.HotelWakeupRuleProfiles.AsNoTracking().Where(x => x.PropertyId == propertyId && x.HotelWakeupRuleProfileId == id).Select(WakeupRuleProfileDto).SingleOrDefaultAsync(ct);
    public Task<PageResult<HotelWakeupRuleProfileDto>> SearchWakeupRuleProfilesAsync(Guid propertyId, HotelWakeupRuleKind? kind, string? keyword, bool? isActive, PageRequest page, string? sort, CancellationToken ct = default)
    { var query = dbContext.HotelWakeupRuleProfiles.AsNoTracking().Where(x => x.PropertyId == propertyId); if (kind is not null) query = query.Where(x => x.Kind == kind); if (!string.IsNullOrWhiteSpace(keyword)) query = query.Where(x => x.Code.Contains(keyword) || x.Name.Contains(keyword) || x.NotificationChannel.Contains(keyword) || (x.Description != null && x.Description.Contains(keyword))); if (isActive is not null) query = query.Where(x => x.IsActive == isActive); query = sort?.ToLowerInvariant() == "updated" ? query.OrderByDescending(x => x.UpdatedAtUtc) : query.OrderBy(x => x.Kind).ThenBy(x => x.Code); return Page(query.Select(WakeupRuleProfileDto), page, ct); }
    public Task<HotelComplaintProfileDto?> GetComplaintProfileDtoAsync(Guid propertyId, Guid id, CancellationToken ct = default) => dbContext.HotelComplaintProfiles.AsNoTracking().Where(x => x.PropertyId == propertyId && x.HotelComplaintProfileId == id).Select(ComplaintProfileDto).SingleOrDefaultAsync(ct);
    public Task<PageResult<HotelComplaintProfileDto>> SearchComplaintProfilesAsync(Guid propertyId, HotelComplaintSeverity? severity, string? keyword, bool? isActive, PageRequest page, string? sort, CancellationToken ct = default)
    { var query = dbContext.HotelComplaintProfiles.AsNoTracking().Where(x => x.PropertyId == propertyId); if (severity is not null) query = query.Where(x => x.Severity == severity); if (!string.IsNullOrWhiteSpace(keyword)) query = query.Where(x => x.Code.Contains(keyword) || x.Name.Contains(keyword) || x.Category.Contains(keyword) || x.Department.Contains(keyword) || (x.Description != null && x.Description.Contains(keyword))); if (isActive is not null) query = query.Where(x => x.IsActive == isActive); query = sort?.ToLowerInvariant() == "updated" ? query.OrderByDescending(x => x.UpdatedAtUtc) : query.OrderBy(x => x.Severity).ThenBy(x => x.Code); return Page(query.Select(ComplaintProfileDto), page, ct); }

    private static async Task<PageResult<T>> Page<T>(IQueryable<T> query, PageRequest page, CancellationToken ct)
    {
        var total = await query.CountAsync(ct);
        var size = Math.Clamp(page.PageSize, 1, 200);
        var items = await query.Skip(page.Skip).Take(size).ToListAsync(ct);
        return new PageResult<T>(items, total, page.Page, page.PageSize);
    }

    private static bool MatchesRuleKey(string? configuredValue, string requestedKey)
    {
        if (IsWildcardRuleKey(configuredValue))
            return true;

        var configuredKey = NormalizeRuleKey(configuredValue);
        if (string.Equals(configuredKey, requestedKey, StringComparison.OrdinalIgnoreCase))
            return true;

        // The settings screens historically allowed both Vietnamese labels and
        // technical service keys. Keep those representations equivalent when
        // resolving a real rule from SQL Server.
        return requestedKey == "ROOM"
            && configuredKey is "PHÒNG" or "PHONG" or "ROOMS" or "ROOMCHARGE" or "ROOMCHARGES";
    }

    private static bool IsWildcardRuleKey(string? value)
    {
        var raw = value?.Trim();
        if (string.IsNullOrWhiteSpace(raw) || raw == "*")
            return true;

        var key = NormalizeRuleKey(raw);
        return key is "ALL" or "ALLSERVICES" or "TATCA" or "TATCADICHVU";
    }

    private static string NormalizeRuleKey(string? value)
        => string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : new string(value.Trim().ToUpperInvariant().Where(char.IsLetterOrDigit).ToArray());
}
