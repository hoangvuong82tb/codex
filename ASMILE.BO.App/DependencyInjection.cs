using ASMILE.BO.App.Services;
using ASMILE.BO.App.ViewModels;
using ASMILE.BO.App.ViewModels.Auth;
using ASMILE.BO.App.Views.Auth;
using ASMILE.BO.Application.Interfaces;
using ASMILE.BO.Application.FrontOffice.Foundation.Contexts;
using ASMILE.BO.Application.FrontOffice.Foundation.Persistence;
using ASMILE.BO.Application.FrontOffice.Housekeeping;
using ASMILE.BO.Application.FrontOffice.TaskShift;
using ASMILE.BO.Application.FrontOffice.Maintenance;
using ASMILE.BO.Application.FrontOffice.Concierge;
using ASMILE.BO.Application.FrontOffice.Message;
using ASMILE.BO.Application.FrontOffice.Group;
using ASMILE.BO.Application.FrontOffice.Guest;
using ASMILE.BO.Application.FrontOffice.Folio;
using ASMILE.BO.Application.FrontOffice.Payment;
using ASMILE.BO.Application.FrontOffice.Settlement;
using ASMILE.BO.Application.FrontOffice.HotelMasterData;
using ASMILE.BO.Application.FrontOffice.Availability;
using ASMILE.BO.Application.FrontOffice.Reservation;
using ASMILE.BO.Application.FrontOffice.Rate;
using ASMILE.BO.Application.FrontOffice.Feedback;
using ASMILE.BO.Application.FrontOffice.Loyalty;
using ASMILE.BO.Application.FrontOffice.Stay;
using Microsoft.Extensions.DependencyInjection;

namespace ASMILE.BO.App;

public static class DependencyInjection
{
    public static void AddPresentation(IServiceCollection services)
    {
        services.AddSingleton<INavigationService, NavigationService>();
        services.AddSingleton<IAppNavigationService, AppNavigationService>();
        services.AddSingleton<IGlobalSearchService, GlobalSearchService>();
        services.AddSingleton<INotificationService, NotificationService>();
        services.AddSingleton<IDialogService, DialogService>();
        services.AddSingleton<ISecurityPromptService, SecurityPromptService>();
        services.AddSingleton<ISupplierImportExportService, SupplierImportExportService>();
        services.AddSingleton<ISupplierImportValidator, SupplierImportValidator>();
        services.AddSingleton<ICustomerImportExportService, CustomerImportExportService>();
        services.AddSingleton<ICustomerImportValidator, CustomerImportValidator>();
        services.AddSingleton<IWarehouseImportExportService, WarehouseImportExportService>();
        services.AddSingleton<IItemImportExportService, ItemImportExportService>();
        services.AddSingleton<IDepartmentPeopleImportExportService, DepartmentPeopleImportExportService>();
        services.AddSingleton<IWindowManagerService, WindowManagerService>();
        services.AddSingleton<IFrontOfficePropertyContextHost, FrontOfficePropertyContextHost>();
        services.AddSingleton<IShellStateService, ShellStateService>();
        services.AddSingleton<IDesktopLayoutService, DesktopLayoutService>();
        services.AddScoped<IModuleSettingsService, ModuleSettingsService>();
        services.AddSingleton<ISettingsValidationService, ModuleSettingsValidationService>();
        services.AddSingleton<IToastNotificationService, ToastNotificationService>();

        services.AddTransient<DashboardViewModel>();
        services.AddTransient<ItemsViewModel>();
        services.AddTransient<HotelMinibarConfigurationViewModel>();
        services.AddTransient<CustomersViewModel>();
        services.AddTransient<CustomerPageViewModel>();
        services.AddTransient<SupplierPageViewModel>();
        services.AddTransient<SuppliersViewModel>();
        services.AddTransient<DepartmentPeoplePageViewModel>();
        services.AddTransient<WarehousesViewModel>();
        services.AddTransient<StockReceiptsViewModel>();
        services.AddTransient<StockIssuesViewModel>();
        services.AddTransient<PaymentReceiptsViewModel>();
        services.AddTransient<PaymentVouchersViewModel>();
        services.AddTransient<ReceivableViewModel>();
        services.AddTransient<ArLedgerViewModel>();
        services.AddTransient<ArCustomerViewModel>();
        services.AddTransient<ArCompanyViewModel>();
        services.AddTransient<ArInvoiceViewModel>();
        services.AddTransient<ArReconcileViewModel>();
        services.AddTransient<ArPaymentViewModel>();
        services.AddTransient<ArAgingViewModel>();
        services.AddTransient<GroupReservationViewModel>();
        services.AddTransient<GroupServiceViewModel>(sp =>
            new GroupServiceViewModel(
                sp.GetRequiredService<IGroupServiceOrderQueryService>(),
                sp.GetRequiredService<IGroupServiceOrderCommandService>(),
                sp.GetRequiredService<IFrontOfficeExecutionContextAccessor>(),
                sp.GetRequiredService<IHotelFolioRepository>(),
                sp.GetRequiredService<IGroupReservationQueryService>(),
                sp.GetRequiredService<IEmailDispatchService>()));
        services.AddTransient<ConciergeViewModel>(sp =>
            new ConciergeViewModel(
                sp.GetRequiredService<IServiceRequestService>(),
                sp.GetRequiredService<IFrontOfficeExecutionContextAccessor>()));
        services.AddTransient<ServiceRequestViewModel>(sp =>
            new ServiceRequestViewModel(
                sp.GetRequiredService<IServiceRequestService>(),
                sp.GetRequiredService<IFrontOfficeExecutionContextAccessor>()));
        services.AddTransient<GuestInquiryViewModel>(sp =>
            new GuestInquiryViewModel(
                sp.GetRequiredService<IGuestProfileRepository>(),
                sp.GetRequiredService<IHotelStayRepository>(),
                sp.GetRequiredService<IHotelFolioRepository>(),
                sp.GetRequiredService<IHotelPaymentRepository>(),
                sp.GetRequiredService<IHotelInvoiceRepository>(),
                sp.GetRequiredService<IReservationRepository>(),
                sp.GetRequiredService<IFrontOfficeExecutionContextAccessor>()));
        services.AddTransient<HousekeepingRoomStatusViewModel>(sp =>
            new HousekeepingRoomStatusViewModel(
                sp.GetRequiredService<IHousekeepingBoardService>(),
                sp.GetRequiredService<IFrontOfficeExecutionContextAccessor>(),
                sp.GetRequiredService<IHousekeepingWorkflowService>(),
                sp.GetRequiredService<IReservationRepository>(),
                sp.GetRequiredService<IAuditService>(),
                sp,
                sp.GetService<IHotelRoomMasterDataRepository>()));
        services.AddTransient<RoomInspectionViewModel>(sp =>
            new RoomInspectionViewModel(
                sp.GetRequiredService<IHousekeepingBoardService>(),
                sp.GetRequiredService<IFrontOfficeExecutionContextAccessor>(),
                sp.GetRequiredService<IHousekeepingWorkflowService>(),
                sp.GetRequiredService<IWorkOrderService>(),
                sp.GetService<IHotelRoomMasterDataRepository>(),
                sp.GetService<ILostFoundQueryService>(),
                sp.GetService<IHotelStayRepository>(),
                sp.GetService<IReservationRepository>()));
        services.AddTransient<CleaningInProgressViewModel>(sp =>
            new CleaningInProgressViewModel(
                sp.GetRequiredService<IHousekeepingBoardService>(),
                sp.GetRequiredService<IFrontOfficeExecutionContextAccessor>(),
                sp.GetRequiredService<IHousekeepingWorkflowService>(),
                sp.GetRequiredService<ICurrentUserService>(),
                sp.GetService<IHotelRoomMasterDataRepository>(),
                sp.GetService<IHotelStayRepository>(),
                sp.GetService<IReservationRepository>()));
        services.AddTransient<CleanRoomViewModel>(sp =>
            new CleanRoomViewModel(
                sp.GetRequiredService<IHousekeepingBoardService>(),
                sp.GetRequiredService<IFrontOfficeExecutionContextAccessor>(),
                sp.GetRequiredService<IHousekeepingWorkflowService>(),
                sp.GetRequiredService<ICurrentUserService>(),
                sp.GetService<IHotelRoomMasterDataRepository>(),
                sp.GetService<IHotelStayRepository>(),
                sp.GetService<IReservationRepository>()));
        services.AddTransient<DirtyRoomViewModel>(sp =>
            new DirtyRoomViewModel(
                sp.GetRequiredService<IHousekeepingBoardService>(),
                sp.GetRequiredService<IFrontOfficeExecutionContextAccessor>(),
                sp.GetRequiredService<IHousekeepingWorkflowService>(),
                sp.GetRequiredService<ICurrentUserService>(),
                sp.GetRequiredService<IHotelStayRepository>(),
                sp.GetRequiredService<IReservationRepository>(),
                sp.GetRequiredService<IWorkOrderService>(),
                sp,
                sp.GetService<IHotelRoomMasterDataRepository>()));
        services.AddTransient<LostFoundViewModel>(sp =>
    new LostFoundViewModel(
        sp.GetRequiredService<ILostFoundQueryService>(),
        sp.GetRequiredService<IFrontOfficeExecutionContextAccessor>(),
        sp.GetRequiredService<ILostFoundCommandService>()));
        services.AddTransient<MinibarViewModel>(sp =>
            new MinibarViewModel(
                sp.GetRequiredService<IMinibarQueryService>(),
                sp.GetRequiredService<IFrontOfficeExecutionContextAccessor>(),
                sp.GetRequiredService<IMinibarCommandService>()));
        services.AddTransient<WorkforceViewModel>(sp =>
            new WorkforceViewModel(
                sp.GetRequiredService<IWorkforceQueryService>(),
                sp.GetRequiredService<IWorkforceCommandService>(),
                sp.GetRequiredService<IHousekeepingBoardService>(),
                sp.GetRequiredService<IHousekeepingWorkflowService>(),
                sp.GetRequiredService<IFrontOfficeExecutionContextAccessor>()));
        services.AddTransient<TaskShiftViewModel>(sp =>
            new TaskShiftViewModel(sp.GetRequiredService<ITaskShiftService>()));
        services.AddTransient<HousekeepingSupplyViewModel>(sp =>
            new HousekeepingSupplyViewModel(
                sp.GetRequiredService<ILinenService>(),
                sp.GetRequiredService<IAmenityService>(),
                sp.GetRequiredService<IFrontOfficeExecutionContextAccessor>()));
        services.AddTransient<HotelHousekeepingSupplyCatalogViewModel>();
        services.AddTransient<PayableViewModel>();
        services.AddTransient<CashBankViewModel>();
        services.AddTransient<AssetManagementViewModel>();
        services.AddTransient<TaxFinancialReportingViewModel>();
        services.AddTransient<InventoryMovementViewModel>();
        services.AddTransient<GeneralLedgerViewModel>();
        services.AddTransient<ReportCenterViewModel>();
        services.AddTransient<SettingsViewModel>();
        services.AddTransient<SecurityAdminViewModel>();
        services.AddTransient<WorkspaceSettingsViewModel>();
        services.AddTransient<AuditCenterViewModel>();
        services.AddTransient<AuditTimelineViewModel>();
        services.AddTransient<OperationsCenterViewModel>();
        services.AddTransient<FiscalPeriodManagementViewModel>();
        services.AddTransient<NotificationCenterViewModel>(sp =>
            new NotificationCenterViewModel(sp.GetRequiredService<INotificationTaskService>()));
        services.AddTransient<NotificationTaskCenterViewModel>();
        services.AddTransient<GuestFeedbackViewModel>();
        services.AddTransient<VipLoyaltyViewModel>();
        services.AddTransient<MessageCenterViewModel>(sp => new MessageCenterViewModel(
            sp.GetRequiredService<IGuestMessageService>(),
            sp.GetRequiredService<IFrontOfficeExecutionContextAccessor>()));
        services.AddTransient<TelephoneCenterViewModel>();
        services.AddTransient<DeniedReservationViewModel>();
        services.AddTransient<WakeUpCallViewModel>(sp => new WakeUpCallViewModel(
            sp.GetRequiredService<IWakeUpCallService>(),
            sp.GetRequiredService<IFrontOfficeExecutionContextAccessor>(),
            sp.GetRequiredService<IHotelRoomMasterDataRepository>()));
        services.AddTransient<TrainingDataViewModel>();
        services.AddTransient<MenuRecipeViewModel>();
        services.AddTransient<BreakfastCafeSalesViewModel>();
        services.AddTransient<AlacarteRestaurantViewModel>();
        services.AddScoped<BanquetRestaurantViewModel>();
        services.AddTransient<IReservationUiService, ReservationUiService>();
        services.AddTransient<IReservationHubPermissionReader>(sp => (IReservationHubPermissionReader)sp.GetRequiredService<IReservationUiService>());
        services.AddTransient<IReceptionUiService, ReceptionUiService>();
        services.AddTransient<IReceptionHubPermissionReader>(sp => (IReceptionHubPermissionReader)sp.GetRequiredService<IReceptionUiService>());
        services.AddTransient<ReservationHubViewModel>();
        services.AddTransient<FutureAvailabilityViewModel>();
        services.AddTransient<ReceptionHubViewModel>();
        services.AddSingleton<IReservationWorkspaceModalService, ReservationWorkspaceModalService>();
        services.AddSingleton<IReceptionWorkspaceModalService, ReceptionWorkspaceModalService>();
        services.AddTransient<ReceptionReadOnlyWorkspaceViewModel>();
        services.AddTransient<ICheckInStayModalService, CheckInStayModalService>();
        services.AddTransient<ICheckOutStayModalService, CheckOutStayModalService>();
        services.AddTransient<CheckInStayModalViewModel>();
        services.AddTransient<CheckOutStayModalViewModel>();
        services.AddTransient<ASMILE.BO.App.Views.LockedRoomWindow>();
        services.AddSingleton<ILockedRoomWindowService, LockedRoomWindowService>();
        services.AddTransient<InHouseWorkspaceViewModel>();
        services.AddTransient<RateEngineViewModel>(sp =>
            new RateEngineViewModel(
                sp.GetRequiredService<IRateRepository>(),
                sp.GetRequiredService<IFrontOfficeExecutionContextAccessor>(),
                sp.GetRequiredService<IHotelRoomMasterDataRepository>(),
                sp.GetRequiredService<IHotelStayRepository>(),
                sp.GetRequiredService<IFrontOfficePersistenceSession>()));
        services.AddTransient<HotelExchangeRateViewModel>(sp =>
            new HotelExchangeRateViewModel(
                sp.GetRequiredService<IExchangeRateQueryService>(),
                sp.GetRequiredService<IExchangeRateCommandService>(),
                sp.GetRequiredService<IFrontOfficeExecutionContextAccessor>()));
        services.AddTransient<RoomTypeMasterViewModel>(sp =>
            new RoomTypeMasterViewModel(
                sp.GetService<IHotelRoomMasterDataRepository>(),
                sp.GetService<IFrontOfficeExecutionContextAccessor>(),
                sp.GetService<CreateHotelRoomTypeCommandHandler>(),
                sp.GetService<UpdateHotelRoomTypeCommandHandler>(),
                sp.GetService<ActivateHotelRoomTypeCommandHandler>(),
                sp.GetService<DeactivateHotelRoomTypeCommandHandler>()));
        services.AddTransient<HotelBuildingMasterViewModel>(sp =>
            new HotelBuildingMasterViewModel(
                sp.GetService<IHotelStructureMasterDataRepository>(),
                sp.GetService<IFrontOfficeExecutionContextAccessor>(),
                sp.GetService<CreateHotelBuildingCommandHandler>(),
                sp.GetService<UpdateHotelBuildingCommandHandler>(),
                sp.GetService<ActivateHotelBuildingCommandHandler>(),
                sp.GetService<DeactivateHotelBuildingCommandHandler>()));
        services.AddTransient<HotelFloorMasterViewModel>(sp =>
            new HotelFloorMasterViewModel(
                sp.GetService<IHotelStructureMasterDataRepository>(),
                sp.GetService<IFrontOfficeExecutionContextAccessor>(),
                sp.GetService<CreateHotelFloorCommandHandler>(),
                sp.GetService<UpdateHotelFloorCommandHandler>(),
                sp.GetService<ActivateHotelFloorCommandHandler>(),
                sp.GetService<DeactivateHotelFloorCommandHandler>()));
        services.AddTransient<HotelAreaMasterViewModel>(sp =>
            new HotelAreaMasterViewModel(
                sp.GetService<IHotelStructureMasterDataRepository>(),
                sp.GetService<IFrontOfficeExecutionContextAccessor>(),
                sp.GetService<CreateHotelAreaCommandHandler>(),
                sp.GetService<UpdateHotelAreaCommandHandler>(),
                sp.GetService<ActivateHotelAreaCommandHandler>(),
                sp.GetService<DeactivateHotelAreaCommandHandler>()));
        services.AddTransient<HotelReferenceCatalogViewModel>(sp =>
            new HotelReferenceCatalogViewModel(
                sp.GetService<IHotelStructureMasterDataRepository>(),
                sp.GetService<IFrontOfficeExecutionContextAccessor>(),
                sp.GetService<CreateHotelReferenceCatalogItemCommandHandler>(),
                sp.GetService<UpdateHotelReferenceCatalogItemCommandHandler>(),
                sp.GetService<ActivateHotelReferenceCatalogItemCommandHandler>(),
                sp.GetService<DeactivateHotelReferenceCatalogItemCommandHandler>()));
        services.AddTransient<HotelRoomStatusRuleViewModel>(sp =>
            new HotelRoomStatusRuleViewModel(
                sp.GetService<IHotelStructureMasterDataRepository>(),
                sp.GetService<IFrontOfficeExecutionContextAccessor>(),
                sp.GetService<CreateHotelRoomStatusRuleCommandHandler>(),
                sp.GetService<UpdateHotelRoomStatusRuleCommandHandler>(),
                sp.GetService<ActivateHotelRoomStatusRuleCommandHandler>(),
                sp.GetService<DeactivateHotelRoomStatusRuleCommandHandler>()));
        services.AddTransient<HotelPropertyProfileViewModel>(sp =>
            new HotelPropertyProfileViewModel(
                sp.GetService<IHotelStructureMasterDataRepository>(),
                sp.GetService<IFrontOfficeExecutionContextAccessor>(),
                sp.GetService<CreateHotelPropertyProfileCommandHandler>(),
                sp.GetService<UpdateHotelPropertyProfileCommandHandler>(),
                sp.GetService<ActivateHotelPropertyProfileCommandHandler>(),
                sp.GetService<DeactivateHotelPropertyProfileCommandHandler>()));
        services.AddTransient<HotelStayPolicyViewModel>(sp =>
            new HotelStayPolicyViewModel(
                sp.GetService<IHotelStructureMasterDataRepository>(),
                sp.GetService<IFrontOfficeExecutionContextAccessor>(),
                sp.GetService<CreateHotelStayPolicyCommandHandler>(),
                sp.GetService<UpdateHotelStayPolicyCommandHandler>(),
                sp.GetService<ActivateHotelStayPolicyCommandHandler>(),
                sp.GetService<DeactivateHotelStayPolicyCommandHandler>()));
        services.AddTransient<HotelCheckInOutRuleViewModel>(sp =>
            new HotelCheckInOutRuleViewModel(
                sp.GetService<IHotelStructureMasterDataRepository>(),
                sp.GetService<IFrontOfficeExecutionContextAccessor>(),
                sp.GetService<CreateHotelCheckInOutRuleCommandHandler>(),
                sp.GetService<UpdateHotelCheckInOutRuleCommandHandler>(),
                sp.GetService<ActivateHotelCheckInOutRuleCommandHandler>(),
                sp.GetService<DeactivateHotelCheckInOutRuleCommandHandler>()));
        services.AddTransient<HotelRoomMoveUpgradeRuleViewModel>(sp =>
            new HotelRoomMoveUpgradeRuleViewModel(
                sp.GetService<IHotelStructureMasterDataRepository>(),
                sp.GetService<IFrontOfficeExecutionContextAccessor>(),
                sp.GetService<CreateHotelRoomMoveUpgradeRuleCommandHandler>(),
                sp.GetService<UpdateHotelRoomMoveUpgradeRuleCommandHandler>(),
                sp.GetService<ActivateHotelRoomMoveUpgradeRuleCommandHandler>(),
                sp.GetService<DeactivateHotelRoomMoveUpgradeRuleCommandHandler>()));
        services.AddTransient<HotelPropertyDocumentViewModel>(sp =>
            new HotelPropertyDocumentViewModel(
                sp.GetService<IHotelStructureMasterDataRepository>(),
                sp.GetService<IFrontOfficeExecutionContextAccessor>(),
                sp.GetService<CreateHotelPropertyDocumentCommandHandler>(),
                sp.GetService<UpdateHotelPropertyDocumentCommandHandler>(),
                sp.GetService<ActivateHotelPropertyDocumentCommandHandler>(),
                sp.GetService<DeactivateHotelPropertyDocumentCommandHandler>()));
        services.AddTransient<HotelPromotionPackageViewModel>(sp =>
            new HotelPromotionPackageViewModel(
                sp.GetService<IHotelStructureMasterDataRepository>(),
                sp.GetService<IFrontOfficeExecutionContextAccessor>(),
                sp.GetService<CreateHotelPromotionPackageCommandHandler>(),
                sp.GetService<UpdateHotelPromotionPackageCommandHandler>(),
                sp.GetService<ActivateHotelPromotionPackageCommandHandler>(),
                sp.GetService<DeactivateHotelPromotionPackageCommandHandler>()));
        services.AddTransient<HotelGroupPolicyViewModel>(sp =>
            new HotelGroupPolicyViewModel(
                sp.GetService<IHotelStructureMasterDataRepository>(),
                sp.GetService<IFrontOfficeExecutionContextAccessor>(),
                sp.GetService<CreateHotelGroupPolicyCommandHandler>(),
                sp.GetService<UpdateHotelGroupPolicyCommandHandler>(),
                sp.GetService<ActivateHotelGroupPolicyCommandHandler>(),
                sp.GetService<DeactivateHotelGroupPolicyCommandHandler>()));
        services.AddTransient<HotelCashierShiftPolicyViewModel>(sp =>
            new HotelCashierShiftPolicyViewModel(
                sp.GetService<IHotelStructureMasterDataRepository>(),
                sp.GetService<IFrontOfficeExecutionContextAccessor>(),
                sp.GetService<CreateHotelCashierShiftPolicyCommandHandler>(),
                sp.GetService<UpdateHotelCashierShiftPolicyCommandHandler>(),
                sp.GetService<ActivateHotelCashierShiftPolicyCommandHandler>(),
                sp.GetService<DeactivateHotelCashierShiftPolicyCommandHandler>()));
        services.AddTransient<HotelNightAuditPolicyViewModel>(sp =>
            new HotelNightAuditPolicyViewModel(
                sp.GetService<IHotelStructureMasterDataRepository>(),
                sp.GetService<IFrontOfficeExecutionContextAccessor>(),
                sp.GetService<CreateHotelNightAuditPolicyCommandHandler>(),
                sp.GetService<UpdateHotelNightAuditPolicyCommandHandler>(),
                sp.GetService<ActivateHotelNightAuditPolicyCommandHandler>(),
                sp.GetService<DeactivateHotelNightAuditPolicyCommandHandler>()));
        services.AddTransient<HotelApprovalMatrixViewModel>(sp =>
            new HotelApprovalMatrixViewModel(
                sp.GetService<IHotelStructureMasterDataRepository>(),
                sp.GetService<IFrontOfficeExecutionContextAccessor>(),
                sp.GetService<CreateHotelApprovalMatrixRuleCommandHandler>(),
                sp.GetService<UpdateHotelApprovalMatrixRuleCommandHandler>(),
                sp.GetService<ActivateHotelApprovalMatrixRuleCommandHandler>(),
                sp.GetService<DeactivateHotelApprovalMatrixRuleCommandHandler>()));
        services.AddTransient<HotelFOPermissionPolicyViewModel>(sp =>
            new HotelFOPermissionPolicyViewModel(
                sp.GetService<IHotelStructureMasterDataRepository>(),
                sp.GetService<IFrontOfficeExecutionContextAccessor>(),
                sp.GetService<CreateHotelFOPermissionPolicyCommandHandler>(),
                sp.GetService<UpdateHotelFOPermissionPolicyCommandHandler>(),
                sp.GetService<ActivateHotelFOPermissionPolicyCommandHandler>(),
                sp.GetService<DeactivateHotelFOPermissionPolicyCommandHandler>()));
        services.AddTransient<HotelFormPrinterProfileViewModel>(sp =>
            new HotelFormPrinterProfileViewModel(
                sp.GetService<IHotelStructureMasterDataRepository>(),
                sp.GetService<IFrontOfficeExecutionContextAccessor>(),
                sp.GetService<CreateHotelFormPrinterProfileCommandHandler>(),
                sp.GetService<UpdateHotelFormPrinterProfileCommandHandler>(),
                sp.GetService<ActivateHotelFormPrinterProfileCommandHandler>(),
                sp.GetService<DeactivateHotelFormPrinterProfileCommandHandler>()));
        services.AddTransient<VfcRetentionPolicyViewModel>();
        services.AddTransient<ExportPipelineSettingsViewModel>();
        services.AddTransient<HotelMessagingIntegrationProfileViewModel>(sp =>
            new HotelMessagingIntegrationProfileViewModel(
                sp.GetService<IHotelStructureMasterDataRepository>(),
                sp.GetService<IFrontOfficeExecutionContextAccessor>(),
                sp.GetService<CreateHotelMessagingIntegrationProfileCommandHandler>(),
                sp.GetService<UpdateHotelMessagingIntegrationProfileCommandHandler>(),
                sp.GetService<ActivateHotelMessagingIntegrationProfileCommandHandler>(),
                sp.GetService<DeactivateHotelMessagingIntegrationProfileCommandHandler>()));
        services.AddTransient<HotelPostedCorrectionPolicyViewModel>(sp =>
            new HotelPostedCorrectionPolicyViewModel(
                sp.GetService<IHotelStructureMasterDataRepository>(),
                sp.GetService<IFrontOfficeExecutionContextAccessor>(),
                sp.GetService<CreateHotelPostedCorrectionPolicyCommandHandler>(),
                sp.GetService<UpdateHotelPostedCorrectionPolicyCommandHandler>(),
                sp.GetService<ActivateHotelPostedCorrectionPolicyCommandHandler>(),
                sp.GetService<DeactivateHotelPostedCorrectionPolicyCommandHandler>()));
        services.AddTransient<HotelBusinessRuleProfileViewModel>(sp =>
            new HotelBusinessRuleProfileViewModel(
                sp.GetService<IHotelStructureMasterDataRepository>(),
                sp.GetService<IFrontOfficeExecutionContextAccessor>(),
                sp.GetService<CreateHotelBusinessRuleProfileCommandHandler>(),
                sp.GetService<UpdateHotelBusinessRuleProfileCommandHandler>(),
                sp.GetService<ActivateHotelBusinessRuleProfileCommandHandler>(),
                sp.GetService<DeactivateHotelBusinessRuleProfileCommandHandler>()));
        services.AddTransient<HotelTaxServiceChargeRuleViewModel>(sp =>
            new HotelTaxServiceChargeRuleViewModel(
                sp.GetService<IHotelStructureMasterDataRepository>(),
                sp.GetService<IFrontOfficeExecutionContextAccessor>(),
                sp.GetService<CreateHotelTaxServiceChargeRuleCommandHandler>(),
                sp.GetService<UpdateHotelTaxServiceChargeRuleCommandHandler>(),
                sp.GetService<ActivateHotelTaxServiceChargeRuleCommandHandler>(),
                sp.GetService<DeactivateHotelTaxServiceChargeRuleCommandHandler>()));
        services.AddTransient<HotelPaymentMethodProfileViewModel>(sp =>
            new HotelPaymentMethodProfileViewModel(
                sp.GetService<IHotelStructureMasterDataRepository>(),
                sp.GetService<IFrontOfficeExecutionContextAccessor>(),
                sp.GetService<CreateHotelPaymentMethodProfileCommandHandler>(),
                sp.GetService<UpdateHotelPaymentMethodProfileCommandHandler>(),
                sp.GetService<ActivateHotelPaymentMethodProfileCommandHandler>(),
                sp.GetService<DeactivateHotelPaymentMethodProfileCommandHandler>()));
        services.AddTransient<HotelDepositPolicyViewModel>(sp => new HotelDepositPolicyViewModel(
            sp.GetService<IHotelStructureMasterDataRepository>(), sp.GetService<IFrontOfficeExecutionContextAccessor>(),
            sp.GetService<CreateHotelDepositPolicyCommandHandler>(), sp.GetService<UpdateHotelDepositPolicyCommandHandler>(),
            sp.GetService<ActivateHotelDepositPolicyCommandHandler>(), sp.GetService<DeactivateHotelDepositPolicyCommandHandler>()));
        services.AddTransient<HotelDiscountPolicyViewModel>(sp => new HotelDiscountPolicyViewModel(
            sp.GetService<IHotelStructureMasterDataRepository>(), sp.GetService<IFrontOfficeExecutionContextAccessor>(),
            sp.GetService<CreateHotelDiscountPolicyCommandHandler>(), sp.GetService<UpdateHotelDiscountPolicyCommandHandler>(),
            sp.GetService<ActivateHotelDiscountPolicyCommandHandler>(), sp.GetService<DeactivateHotelDiscountPolicyCommandHandler>()));
        services.AddTransient<HotelChargeCodeProfileViewModel>(sp => new HotelChargeCodeProfileViewModel(
            sp.GetService<IHotelStructureMasterDataRepository>(), sp.GetService<IFrontOfficeExecutionContextAccessor>(),
            sp.GetService<CreateHotelChargeCodeProfileCommandHandler>(), sp.GetService<UpdateHotelChargeCodeProfileCommandHandler>(),
            sp.GetService<ActivateHotelChargeCodeProfileCommandHandler>(), sp.GetService<DeactivateHotelChargeCodeProfileCommandHandler>()));
        services.AddTransient<HotelFolioProfileViewModel>(sp => new HotelFolioProfileViewModel(
            sp.GetService<IHotelStructureMasterDataRepository>(), sp.GetService<IFrontOfficeExecutionContextAccessor>(),
            sp.GetService<CreateHotelFolioProfileCommandHandler>(), sp.GetService<UpdateHotelFolioProfileCommandHandler>(),
            sp.GetService<ActivateHotelFolioProfileCommandHandler>(), sp.GetService<DeactivateHotelFolioProfileCommandHandler>()));
        services.AddTransient<HotelGuestTypeProfileViewModel>(sp => new HotelGuestTypeProfileViewModel(
            sp.GetService<IHotelStructureMasterDataRepository>(), sp.GetService<IFrontOfficeExecutionContextAccessor>(),
            sp.GetService<CreateHotelGuestTypeProfileCommandHandler>(), sp.GetService<UpdateHotelGuestTypeProfileCommandHandler>(),
            sp.GetService<ActivateHotelGuestTypeProfileCommandHandler>(), sp.GetService<DeactivateHotelGuestTypeProfileCommandHandler>()));
        services.AddTransient<HotelProfileTypeProfileViewModel>(sp => new HotelProfileTypeProfileViewModel(
            sp.GetService<IHotelStructureMasterDataRepository>(), sp.GetService<IFrontOfficeExecutionContextAccessor>(),
            sp.GetService<CreateHotelProfileTypeProfileCommandHandler>(), sp.GetService<UpdateHotelProfileTypeProfileCommandHandler>(),
            sp.GetService<ActivateHotelProfileTypeProfileCommandHandler>(), sp.GetService<DeactivateHotelProfileTypeProfileCommandHandler>()));
        services.AddTransient<HotelVipLevelProfileViewModel>(sp => new HotelVipLevelProfileViewModel(
            sp.GetService<IHotelStructureMasterDataRepository>(), sp.GetService<IFrontOfficeExecutionContextAccessor>(),
            sp.GetService<CreateHotelVipLevelProfileCommandHandler>(), sp.GetService<UpdateHotelVipLevelProfileCommandHandler>(),
            sp.GetService<ActivateHotelVipLevelProfileCommandHandler>(), sp.GetService<DeactivateHotelVipLevelProfileCommandHandler>()));
        services.AddTransient<HotelGuestPreferenceProfileViewModel>(sp => new HotelGuestPreferenceProfileViewModel(
            sp.GetService<IHotelStructureMasterDataRepository>(), sp.GetService<IFrontOfficeExecutionContextAccessor>(),
            sp.GetService<CreateHotelGuestPreferenceProfileCommandHandler>(), sp.GetService<UpdateHotelGuestPreferenceProfileCommandHandler>(),
            sp.GetService<ActivateHotelGuestPreferenceProfileCommandHandler>(), sp.GetService<DeactivateHotelGuestPreferenceProfileCommandHandler>()));
        services.AddTransient<HotelGuestRequestProfileViewModel>(sp => new HotelGuestRequestProfileViewModel(
            sp.GetService<IHotelStructureMasterDataRepository>(), sp.GetService<IFrontOfficeExecutionContextAccessor>(),
            sp.GetService<CreateHotelGuestRequestProfileCommandHandler>(), sp.GetService<UpdateHotelGuestRequestProfileCommandHandler>(),
            sp.GetService<ActivateHotelGuestRequestProfileCommandHandler>(), sp.GetService<DeactivateHotelGuestRequestProfileCommandHandler>()));
        services.AddTransient<HotelReservationTypeProfileViewModel>(sp => new HotelReservationTypeProfileViewModel(
            sp.GetService<IHotelStructureMasterDataRepository>(), sp.GetService<IFrontOfficeExecutionContextAccessor>(),
            sp.GetService<CreateHotelReservationTypeProfileCommandHandler>(), sp.GetService<UpdateHotelReservationTypeProfileCommandHandler>(),
            sp.GetService<ActivateHotelReservationTypeProfileCommandHandler>(), sp.GetService<DeactivateHotelReservationTypeProfileCommandHandler>()));
        services.AddTransient<HotelBookingSourceProfileViewModel>(sp => new HotelBookingSourceProfileViewModel(
            sp.GetService<IHotelStructureMasterDataRepository>(), sp.GetService<IFrontOfficeExecutionContextAccessor>(),
            sp.GetService<CreateHotelBookingSourceProfileCommandHandler>(), sp.GetService<UpdateHotelBookingSourceProfileCommandHandler>(),
            sp.GetService<ActivateHotelBookingSourceProfileCommandHandler>(), sp.GetService<DeactivateHotelBookingSourceProfileCommandHandler>()));
        services.AddTransient<HotelBookingChannelProfileViewModel>(sp => new HotelBookingChannelProfileViewModel(
            sp.GetService<IHotelStructureMasterDataRepository>(), sp.GetService<IFrontOfficeExecutionContextAccessor>(),
            sp.GetService<CreateHotelBookingChannelProfileCommandHandler>(), sp.GetService<UpdateHotelBookingChannelProfileCommandHandler>(),
            sp.GetService<ActivateHotelBookingChannelProfileCommandHandler>(), sp.GetService<DeactivateHotelBookingChannelProfileCommandHandler>()));
        services.AddTransient<HotelMarketSegmentProfileViewModel>(sp => new HotelMarketSegmentProfileViewModel(
            sp.GetService<IHotelStructureMasterDataRepository>(), sp.GetService<IFrontOfficeExecutionContextAccessor>(),
            sp.GetService<CreateHotelMarketSegmentProfileCommandHandler>(), sp.GetService<UpdateHotelMarketSegmentProfileCommandHandler>(),
            sp.GetService<ActivateHotelMarketSegmentProfileCommandHandler>(), sp.GetService<DeactivateHotelMarketSegmentProfileCommandHandler>()));
        services.AddTransient<HotelHousekeepingRuleProfileViewModel>(sp => new HotelHousekeepingRuleProfileViewModel(
            sp.GetService<IHotelStructureMasterDataRepository>(), sp.GetService<IFrontOfficeExecutionContextAccessor>(),
            sp.GetService<CreateHotelHousekeepingRuleProfileCommandHandler>(), sp.GetService<UpdateHotelHousekeepingRuleProfileCommandHandler>(),
            sp.GetService<ActivateHotelHousekeepingRuleProfileCommandHandler>(), sp.GetService<DeactivateHotelHousekeepingRuleProfileCommandHandler>()));
        services.AddTransient<HotelWakeupRuleProfileViewModel>(sp => new HotelWakeupRuleProfileViewModel(
            sp.GetService<IHotelStructureMasterDataRepository>(), sp.GetService<IFrontOfficeExecutionContextAccessor>(),
            sp.GetService<CreateHotelWakeupRuleProfileCommandHandler>(), sp.GetService<UpdateHotelWakeupRuleProfileCommandHandler>(),
            sp.GetService<ActivateHotelWakeupRuleProfileCommandHandler>(), sp.GetService<DeactivateHotelWakeupRuleProfileCommandHandler>()));
        services.AddTransient<HotelComplaintProfileViewModel>(sp => new HotelComplaintProfileViewModel(
            sp.GetService<IHotelStructureMasterDataRepository>(), sp.GetService<IFrontOfficeExecutionContextAccessor>(),
            sp.GetService<CreateHotelComplaintProfileCommandHandler>(), sp.GetService<UpdateHotelComplaintProfileCommandHandler>(),
            sp.GetService<ActivateHotelComplaintProfileCommandHandler>(), sp.GetService<DeactivateHotelComplaintProfileCommandHandler>()));
        services.AddTransient<HotelCompanyProfileViewModel>(sp => new HotelCompanyProfileViewModel(
            sp.GetService<IHotelStructureMasterDataRepository>(), sp.GetService<IFrontOfficeExecutionContextAccessor>(),
            sp.GetService<CreateHotelCompanyProfileCommandHandler>(), sp.GetService<UpdateHotelCompanyProfileCommandHandler>(), sp.GetService<ActivateHotelCompanyProfileCommandHandler>(), sp.GetService<DeactivateHotelCompanyProfileCommandHandler>(),
            sp.GetService<CreateHotelTravelAgentProfileCommandHandler>(), sp.GetService<UpdateHotelTravelAgentProfileCommandHandler>(), sp.GetService<ActivateHotelTravelAgentProfileCommandHandler>(), sp.GetService<DeactivateHotelTravelAgentProfileCommandHandler>(),
            sp.GetService<CreateHotelCommissionRuleProfileCommandHandler>(), sp.GetService<UpdateHotelCommissionRuleProfileCommandHandler>(), sp.GetService<ActivateHotelCommissionRuleProfileCommandHandler>(), sp.GetService<DeactivateHotelCommissionRuleProfileCommandHandler>()));
        services.AddTransient<HotelTravelAgentProfileViewModel>(sp => new HotelTravelAgentProfileViewModel(
            sp.GetService<IHotelStructureMasterDataRepository>(), sp.GetService<IFrontOfficeExecutionContextAccessor>(),
            sp.GetService<CreateHotelCompanyProfileCommandHandler>(), sp.GetService<UpdateHotelCompanyProfileCommandHandler>(), sp.GetService<ActivateHotelCompanyProfileCommandHandler>(), sp.GetService<DeactivateHotelCompanyProfileCommandHandler>(),
            sp.GetService<CreateHotelTravelAgentProfileCommandHandler>(), sp.GetService<UpdateHotelTravelAgentProfileCommandHandler>(), sp.GetService<ActivateHotelTravelAgentProfileCommandHandler>(), sp.GetService<DeactivateHotelTravelAgentProfileCommandHandler>(),
            sp.GetService<CreateHotelCommissionRuleProfileCommandHandler>(), sp.GetService<UpdateHotelCommissionRuleProfileCommandHandler>(), sp.GetService<ActivateHotelCommissionRuleProfileCommandHandler>(), sp.GetService<DeactivateHotelCommissionRuleProfileCommandHandler>()));
        services.AddTransient<HotelCommissionRuleProfileViewModel>(sp => new HotelCommissionRuleProfileViewModel(
            sp.GetService<IHotelStructureMasterDataRepository>(), sp.GetService<IFrontOfficeExecutionContextAccessor>(),
            sp.GetService<CreateHotelCompanyProfileCommandHandler>(), sp.GetService<UpdateHotelCompanyProfileCommandHandler>(), sp.GetService<ActivateHotelCompanyProfileCommandHandler>(), sp.GetService<DeactivateHotelCompanyProfileCommandHandler>(),
            sp.GetService<CreateHotelTravelAgentProfileCommandHandler>(), sp.GetService<UpdateHotelTravelAgentProfileCommandHandler>(), sp.GetService<ActivateHotelTravelAgentProfileCommandHandler>(), sp.GetService<DeactivateHotelTravelAgentProfileCommandHandler>(),
            sp.GetService<CreateHotelCommissionRuleProfileCommandHandler>(), sp.GetService<UpdateHotelCommissionRuleProfileCommandHandler>(), sp.GetService<ActivateHotelCommissionRuleProfileCommandHandler>(), sp.GetService<DeactivateHotelCommissionRuleProfileCommandHandler>()));
        services.AddTransient<HotelRoomConfigurationViewModel>(sp =>
            new HotelRoomConfigurationViewModel(
                sp.GetService<IHotelRoomMasterDataRepository>(),
                sp.GetService<IHotelStructureMasterDataRepository>(),
                sp.GetService<IFrontOfficeExecutionContextAccessor>(),
                sp.GetService<CreateHotelRoomCommandHandler>(),
                sp.GetService<UpdateHotelRoomCommandHandler>(),
                sp.GetService<ActivateHotelRoomCommandHandler>(),
                sp.GetService<DeactivateHotelRoomCommandHandler>()));
        services.AddTransient<RoomMasterViewModel>(sp =>
            new RoomMasterViewModel(
                sp.GetService<IHotelRoomMasterDataRepository>(),
                sp.GetService<IFrontOfficeExecutionContextAccessor>(),
                sp.GetService<CreateHotelRoomCommandHandler>(),
                sp.GetService<UpdateHotelRoomCommandHandler>(),
                sp.GetService<HotelRoomMasterDataQueryHandlers>(),
                sp.GetService<ChangeHotelRoomOperationalStatusCommandHandler>(),
                sp.GetService<IAuditService>(),
                sp.GetService<IHotelStayRepository>(),
                sp.GetService<IReservationRepository>(),
                sp,
                sp.GetService<IHousekeepingWorkflowService>(),
                sp.GetService<IHousekeepingBoardService>()));
        services.AddTransient<FrontOfficeHubViewModel>();
        services.AddTransient<ModuleHubViewModel>();
        services.AddTransient<ReservationWorkspaceViewModel>();
        services.AddTransient<VirtualExplorerViewModel>();
        services.AddTransient<DesktopWorkspaceViewModel>();
        services.AddTransient<StartMenuViewModel>();
        services.AddTransient<TaskbarViewModel>();
        services.AddTransient<LoginViewModel>();
        services.AddTransient<LoginView>();

        services.AddScoped<DesktopShellViewModel>();
        services.AddScoped<MainWindow>();
    }
}
