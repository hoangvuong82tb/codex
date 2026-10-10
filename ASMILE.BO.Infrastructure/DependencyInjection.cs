using ASMILE.BO.Application.Interfaces;
using ASMILE.BO.Core.Interfaces.Repositories;
using ASMILE.BO.Core.Interfaces.Services;
using ASMILE.BO.Infrastructure.Options;
using ASMILE.BO.Infrastructure.Persistence;
using ASMILE.BO.Infrastructure.Repositories;
using ASMILE.BO.Infrastructure.Security;
using ASMILE.BO.Infrastructure.Security.Store;
using ASMILE.BO.Infrastructure.Services;
using ASMILE.BO.Application.Interfaces.Settings;
using ASMILE.BO.Infrastructure.SettingsEngine;
using ASMILE.BO.Infrastructure.FrontOffice.Foundation;
using ASMILE.BO.Application.FrontOffice.Foundation.Audit;
using ASMILE.BO.Application.FrontOffice.Foundation.Contexts;
using ASMILE.BO.Application.FrontOffice.Foundation.Events;
using ASMILE.BO.Application.FrontOffice.Foundation.Persistence;
using ASMILE.BO.Application.FrontOffice.ProcessManager;
using ASMILE.BO.Application.FrontOffice.Reservation;
using ASMILE.BO.Application.FrontOffice.HotelMasterData;
using ASMILE.BO.Application.FrontOffice.Stay;
using ASMILE.BO.Application.FrontOffice.Folio;
using SettlementApplication = ASMILE.BO.Application.FrontOffice.Settlement;
using ASMILE.BO.Application.FrontOffice.Guest;
using ASMILE.BO.Application.FrontOffice.Payment;
using ASMILE.BO.Application.FrontOffice.Group;
using ASMILE.BO.Application.FrontOffice.Housekeeping;
using ASMILE.BO.Application.FrontOffice.TaskShift;
using ASMILE.BO.Application.FrontOffice.NightAudit;
using ASMILE.BO.Application.FrontOffice.Posting;
using ASMILE.BO.Application.FrontOffice.Rate;
using ASMILE.BO.Application.FrontOffice.DoorLock;
using ASMILE.BO.Application.FrontOffice.Reporting;
using ASMILE.BO.Application.FrontOffice.Cashier;
using ASMILE.BO.Application.FrontOffice.Incident;
using ASMILE.BO.Application.FrontOffice.ShiftLog;
using ASMILE.BO.Application.FrontOffice.Marketing;
using ASMILE.BO.Application.FrontOffice.Concierge;
using ASMILE.BO.Application.FrontOffice.Message;
using ASMILE.BO.Application.FrontOffice.Maintenance;
using ASMILE.BO.Application.FrontOffice.Availability;
using ASMILE.BO.Application.FrontOffice.Feedback;
using ASMILE.BO.Application.FrontOffice.Loyalty;
using ASMILE.BO.Infrastructure.FrontOffice.ProcessManager;
using ASMILE.BO.Infrastructure.FrontOffice.Reservation;
using ASMILE.BO.Infrastructure.FrontOffice.Stay;
using ASMILE.BO.Infrastructure.FrontOffice.Folio;
using SettlementInfrastructure = ASMILE.BO.Infrastructure.FrontOffice.Settlement;
using ASMILE.BO.Infrastructure.FrontOffice.HotelMasterData;
using ASMILE.BO.Infrastructure.FrontOffice.Guest;
using ASMILE.BO.Infrastructure.FrontOffice.Payment;
using ASMILE.BO.Infrastructure.FrontOffice.Group;
using ASMILE.BO.Infrastructure.FrontOffice.Housekeeping;
using ASMILE.BO.Infrastructure.FrontOffice.Maintenance;
using ASMILE.BO.Infrastructure.FrontOffice.Availability;
using ASMILE.BO.Infrastructure.FrontOffice.Feedback;
using ASMILE.BO.Infrastructure.FrontOffice.Loyalty;
using ASMILE.BO.Infrastructure.FrontOffice.TaskShift;
using ASMILE.BO.Infrastructure.FrontOffice.Message;
using ASMILE.BO.Infrastructure.FrontOffice.NightAudit;
using ASMILE.BO.Infrastructure.FrontOffice.Posting;
using ASMILE.BO.Infrastructure.FrontOffice.Rate;
using ASMILE.BO.Infrastructure.FrontOffice.DoorLock;
using ASMILE.BO.Infrastructure.FrontOffice.Reporting;
using ASMILE.BO.Infrastructure.FrontOffice.Cashier;
using ASMILE.BO.Infrastructure.FrontOffice.Incident;
using ASMILE.BO.Infrastructure.FrontOffice.ShiftLog;
using ASMILE.BO.Infrastructure.FrontOffice.Marketing;
using ASMILE.BO.Infrastructure.FrontOffice.Concierge;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace ASMILE.BO.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<DatabaseOptions>(configuration.GetSection(DatabaseOptions.SectionName));
        services.Configure<ServiceSupervisorOptions>(configuration.GetSection(ServiceSupervisorOptions.SectionName));
        services.Configure<TrainingDataOptions>(configuration.GetSection(TrainingDataOptions.SectionName));
        services.Configure<EmailOptions>(configuration.GetSection(EmailOptions.SectionName));
        services.AddOptions<AccountingOptions>().Bind(configuration.GetSection(AccountingOptions.SectionName)).Validate(x => ValidateOptions(x), "Accounting configuration is invalid.");
        services.AddOptions<InventoryOptions>().Bind(configuration.GetSection(InventoryOptions.SectionName)).Validate(x => ValidateOptions(x), "Inventory configuration is invalid.");
        services.AddOptions<SecurityOptions>().Bind(configuration.GetSection(SecurityOptions.SectionName)).Validate(x => ValidateOptions(x), "Security configuration is invalid.");

        services.AddSingleton<InMemorySecurityStore>();
        services.AddSingleton<ISecretProtector>(_ => OperatingSystem.IsWindows()
            ? new DpapiSecretProtector()
            : new PlainTextSecretProtector());
        services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();
        services.AddSingleton<ICurrentUserService, CurrentUserService>();
        services.AddSingleton<IAuditCorrelationService, AuditCorrelationService>();
        services.AddSingleton<IPathSecurityService, PathSecurityService>();

        services.AddScoped<IAuthorizationService, AuthorizationService>();
        services.AddScoped<ISecurityAuditService, SecurityAuditService>();
        services.AddScoped<IAuthenticationService, AuthenticationService>();
        services.AddScoped<IUserAccountService, UserAccountService>();
        services.AddScoped<IRoleService, RoleService>();
        services.AddScoped<ISecuritySeedService, SecuritySeedService>();

        // Production deployments must be able to supply the connection string
        // outside the shipped configuration file (for example via a secret
        // manager or the ASMILE_BO_CONNECTION_STRING environment variable).
        var configuredConnectionString = configuration.GetValue<string>("Database:UserConnectionOverride")
            ?? Environment.GetEnvironmentVariable("ASMILE_BO_CONNECTION_STRING")
            ?? configuration.GetSection(DatabaseOptions.SectionName).GetValue<string>("ConnectionString")
            ?? throw new InvalidOperationException("Database:ConnectionString is missing.");
        ISecretProtector secretProtector = OperatingSystem.IsWindows()
            ? new DpapiSecretProtector()
            : new PlainTextSecretProtector();
        var connectionString = secretProtector.IsProtected(configuredConnectionString)
            ? secretProtector.Unprotect(configuredConnectionString)
            : configuredConnectionString;
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Database connection is not configured. Set ASMILE_BO_CONNECTION_STRING or Database:ConnectionString in a deployment-only configuration file.");
        }

        // The settings screen may show safe metadata about the active DB
        // connection, but must never retain or display the connection secret.
        services.AddSingleton(RuntimeDatabaseSettings.FromConnectionString(connectionString));
        services.AddSingleton<IDatabaseConnectionConfigurationService>(
            new DatabaseConnectionConfigurationService(connectionString, secretProtector));
        services.AddSingleton(new RuntimeLoggingSettings(configuration));

        services.AddSingleton<ICompanyContextAccessor, CompanyContextAccessor>();
        services.AddSingleton<ICurrentCompanyContext, CurrentCompanyContext>();
        services.AddSingleton<RuntimeDatabaseCommandTimeout>();
        services.AddSingleton<DatabaseCommandTimeoutInterceptor>();
        // Keep the scoped DbContext registration for the regular application
        // services, and also expose a scoped factory for read-heavy surfaces
        // that may be opened while another scoped query is still running.
        // Virtual File Center uses the factory so its navigation cannot share
        // an active DbDataReader with the rest of the shell.
        services.AddDbContext<AppDbContext>((sp, opt) =>
            opt.UseSqlServer(connectionString)
                .AddInterceptors(sp.GetRequiredService<DatabaseCommandTimeoutInterceptor>()));
        services.AddScoped<IDbContextFactory<AppDbContext>>(sp =>
            new ScopedAppDbContextFactory(
                sp.GetRequiredService<DbContextOptions<AppDbContext>>(),
                sp.GetRequiredService<ICurrentCompanyContext>()));

        services.AddSingleton<IFrontOfficeExecutionContextAccessor, FrontOfficeExecutionContextAccessor>();
        services.AddSingleton<IClock, SystemClock>();
        services.AddScoped<IIntegrationEventMapper, PassthroughIntegrationEventMapper>();
        services.AddScoped<IOutboxMessageFactory, CanonicalOutboxMessageFactory>();
        services.AddScoped<IEmailDispatchService, EmailDispatchService>();
        services.AddHostedService<EmailDispatchHostedService>();
        services.AddScoped<FrontOfficeAuditWriter>();
        services.AddScoped<IFrontOfficeAuditWriter>(sp => sp.GetRequiredService<FrontOfficeAuditWriter>());
        services.AddScoped<IFrontOfficePersistenceSession, EfFrontOfficePersistenceSession>();
        services.AddScoped<IInboxStore, EfInboxStore>();
        services.AddScoped<IIdempotencyStore, EfIdempotencyStore>();
        services.AddScoped<EfProcessManagerStore>();
        services.AddScoped<IProcessRepository>(sp => sp.GetRequiredService<EfProcessManagerStore>());
        services.AddScoped<IProcessMonitorQueries>(sp => sp.GetRequiredService<EfProcessManagerStore>());
        services.AddSingleton<IProcessFaultInjector, NoOpProcessFaultInjector>();
        services.AddSingleton(new ProcessWorkerOptions());
        services.AddSingleton<ProcessWorkerFailureClassifier>();
        services.AddSingleton<IProcessWorkerMetrics, NoOpProcessWorkerMetrics>();
        services.AddSingleton<IProcessTimeoutWorkExecutor, DurableProcessTimeoutWorkExecutor>();
        services.AddScoped<ProcessTimeoutDispatcher>();
        services.AddScoped<ProcessTransitionEngine>();
        services.AddScoped<ProcessSignalHandler>();
        services.AddScoped<EfReservationRepository>();
        services.AddScoped<IReservationRepository>(sp => sp.GetRequiredService<EfReservationRepository>());
        services.AddScoped<IRoomReservationConflictChecker>(sp => sp.GetRequiredService<EfReservationRepository>());
        services.AddScoped<IReservationNumberGenerator, SqlReservationNumberGenerator>();
        services.AddScoped<EfHotelRoomMasterDataRepository>();
        services.AddScoped<IHotelRoomMasterDataRepository>(sp => sp.GetRequiredService<EfHotelRoomMasterDataRepository>());
        services.AddScoped<IHotelReservationReferenceReader>(sp => sp.GetRequiredService<EfHotelRoomMasterDataRepository>());
        services.AddScoped<EfHotelStructureMasterDataRepository>();
        services.AddScoped<IHotelStructureMasterDataRepository>(sp => sp.GetRequiredService<EfHotelStructureMasterDataRepository>());
        services.AddScoped<IHotelMinibarMasterDataService, EfHotelMinibarMasterDataService>();
        services.AddScoped<EfHotelStayRepository>();
        services.AddScoped<IHotelStayRepository>(sp => sp.GetRequiredService<EfHotelStayRepository>());
        services.AddScoped<IHotelStayOccupancyReader>(sp => sp.GetRequiredService<EfHotelStayRepository>());
        services.AddScoped<IHotelStayOccupantRepository, EfHotelStayOccupantRepository>();
        services.AddScoped<IHotelStayNumberGenerator, SqlHotelStayNumberGenerator>();
        services.AddScoped<HotelStayApplicationService>();
        services.AddScoped<StayAdjustmentApplicationService>();
        services.AddScoped<PrepareStayFromReservationCommandHandler>(); services.AddScoped<CheckInHotelStayCommandHandler>(); services.AddScoped<CheckInReservationCommandHandler>(); services.AddScoped<CheckOutHotelStayCommandHandler>(); services.AddScoped<ApproveDepartureCommandHandler>(); services.AddScoped<ConfirmPreArrivalCommandHandler>(); services.AddScoped<PreCheckInHotelStayCommandHandler>(); services.AddScoped<ExtendStayCommandHandler>(); services.AddScoped<MoveRoomCommandHandler>(); services.AddScoped<CompleteStayCommandHandler>(); services.AddScoped<ReopenStayCommandHandler>(); services.AddScoped<CancelStayCommandHandler>(); services.AddScoped<ToggleDoNotDisturbCommandHandler>(); services.AddScoped<RequestWakeUpCallCommandHandler>(); services.AddScoped<LogComplaintCommandHandler>(); services.AddScoped<AddStayTagCommandHandler>(); services.AddScoped<RemoveStayTagCommandHandler>(); services.AddScoped<MarkVipServiceCommandHandler>();
        services.AddScoped<GetStayByIdQueryHandler>(); services.AddScoped<GetStayByNumberQueryHandler>(); services.AddScoped<SearchStaysQueryHandler>(); services.AddScoped<GetCurrentInHouseStaysQueryHandler>(); services.AddScoped<GetTodayExpectedArrivalsQueryHandler>(); services.AddScoped<GetTodayExpectedDeparturesQueryHandler>(); services.AddScoped<GetStayTimelineQueryHandler>();
        services.AddScoped<HotelRoomMasterDataApplicationService>();
        services.AddScoped<HotelStructureMasterDataApplicationService>();
        services.AddScoped<IAvailabilityEngine, AvailabilityEngine>();
        services.AddScoped<IHousekeepingBoardService, EfHousekeepingBoardService>();
        services.AddScoped<IHousekeepingWorkflowService, EfHousekeepingWorkflowService>();
        services.AddScoped<EfHousekeepingSupplyService>();
        services.AddScoped<ILinenService>(sp => sp.GetRequiredService<EfHousekeepingSupplyService>());
        services.AddScoped<IAmenityService>(sp => sp.GetRequiredService<EfHousekeepingSupplyService>());
        services.AddScoped<IHousekeepingSupplyMasterDataService, EfHousekeepingSupplyMasterDataService>();
        services.AddScoped<ILostFoundQueryService, EfLostFoundQueryService>();
        services.AddScoped<ILostFoundCommandService, EfLostFoundCommandService>();
        services.AddScoped<IGuestFeedbackService, EfGuestFeedbackService>();
        services.AddScoped<IVipLoyaltyService, EfVipLoyaltyService>();
        services.AddScoped<IMinibarQueryService, EfMinibarQueryService>();
        services.AddScoped<IMinibarCommandService, EfMinibarCommandService>();
        services.AddScoped<IWorkforceQueryService, EfWorkforceQueryService>();
        services.AddScoped<IWorkforceCommandService, EfWorkforceCommandService>();
        services.AddScoped<ITaskShiftService, EfTaskShiftService>();
        services.AddScoped<CreateHotelRoomTypeCommandHandler>(); services.AddScoped<UpdateHotelRoomTypeCommandHandler>(); services.AddScoped<ActivateHotelRoomTypeCommandHandler>(); services.AddScoped<DeactivateHotelRoomTypeCommandHandler>();
        services.AddScoped<CreateHotelBuildingCommandHandler>(); services.AddScoped<UpdateHotelBuildingCommandHandler>(); services.AddScoped<ActivateHotelBuildingCommandHandler>(); services.AddScoped<DeactivateHotelBuildingCommandHandler>();
        services.AddScoped<CreateHotelFloorCommandHandler>(); services.AddScoped<UpdateHotelFloorCommandHandler>(); services.AddScoped<ActivateHotelFloorCommandHandler>(); services.AddScoped<DeactivateHotelFloorCommandHandler>();
        services.AddScoped<CreateHotelAreaCommandHandler>(); services.AddScoped<UpdateHotelAreaCommandHandler>(); services.AddScoped<ActivateHotelAreaCommandHandler>(); services.AddScoped<DeactivateHotelAreaCommandHandler>();
        services.AddScoped<CreateHotelReferenceCatalogItemCommandHandler>(); services.AddScoped<UpdateHotelReferenceCatalogItemCommandHandler>(); services.AddScoped<ActivateHotelReferenceCatalogItemCommandHandler>(); services.AddScoped<DeactivateHotelReferenceCatalogItemCommandHandler>();
        services.AddScoped<CreateHotelRoomStatusRuleCommandHandler>(); services.AddScoped<UpdateHotelRoomStatusRuleCommandHandler>(); services.AddScoped<ActivateHotelRoomStatusRuleCommandHandler>(); services.AddScoped<DeactivateHotelRoomStatusRuleCommandHandler>();
        services.AddScoped<CreateHotelPropertyProfileCommandHandler>(); services.AddScoped<UpdateHotelPropertyProfileCommandHandler>(); services.AddScoped<ActivateHotelPropertyProfileCommandHandler>(); services.AddScoped<DeactivateHotelPropertyProfileCommandHandler>();
        services.AddScoped<CreateHotelStayPolicyCommandHandler>(); services.AddScoped<UpdateHotelStayPolicyCommandHandler>(); services.AddScoped<ActivateHotelStayPolicyCommandHandler>(); services.AddScoped<DeactivateHotelStayPolicyCommandHandler>();
        services.AddScoped<CreateHotelCheckInOutRuleCommandHandler>(); services.AddScoped<UpdateHotelCheckInOutRuleCommandHandler>(); services.AddScoped<ActivateHotelCheckInOutRuleCommandHandler>(); services.AddScoped<DeactivateHotelCheckInOutRuleCommandHandler>();
        services.AddScoped<CreateHotelRoomMoveUpgradeRuleCommandHandler>(); services.AddScoped<UpdateHotelRoomMoveUpgradeRuleCommandHandler>(); services.AddScoped<ActivateHotelRoomMoveUpgradeRuleCommandHandler>(); services.AddScoped<DeactivateHotelRoomMoveUpgradeRuleCommandHandler>();
        services.AddScoped<CreateHotelPropertyDocumentCommandHandler>(); services.AddScoped<UpdateHotelPropertyDocumentCommandHandler>(); services.AddScoped<ActivateHotelPropertyDocumentCommandHandler>(); services.AddScoped<DeactivateHotelPropertyDocumentCommandHandler>();
        services.AddScoped<CreateHotelPromotionPackageCommandHandler>(); services.AddScoped<UpdateHotelPromotionPackageCommandHandler>(); services.AddScoped<ActivateHotelPromotionPackageCommandHandler>(); services.AddScoped<DeactivateHotelPromotionPackageCommandHandler>();
        services.AddScoped<CreateHotelGroupPolicyCommandHandler>(); services.AddScoped<UpdateHotelGroupPolicyCommandHandler>(); services.AddScoped<ActivateHotelGroupPolicyCommandHandler>(); services.AddScoped<DeactivateHotelGroupPolicyCommandHandler>();
        services.AddScoped<CreateHotelCashierShiftPolicyCommandHandler>(); services.AddScoped<UpdateHotelCashierShiftPolicyCommandHandler>(); services.AddScoped<ActivateHotelCashierShiftPolicyCommandHandler>(); services.AddScoped<DeactivateHotelCashierShiftPolicyCommandHandler>();
        services.AddScoped<CreateHotelNightAuditPolicyCommandHandler>(); services.AddScoped<UpdateHotelNightAuditPolicyCommandHandler>(); services.AddScoped<ActivateHotelNightAuditPolicyCommandHandler>(); services.AddScoped<DeactivateHotelNightAuditPolicyCommandHandler>();
        services.AddScoped<CreateHotelApprovalMatrixRuleCommandHandler>(); services.AddScoped<UpdateHotelApprovalMatrixRuleCommandHandler>(); services.AddScoped<ActivateHotelApprovalMatrixRuleCommandHandler>(); services.AddScoped<DeactivateHotelApprovalMatrixRuleCommandHandler>();
        services.AddScoped<CreateHotelFOPermissionPolicyCommandHandler>(); services.AddScoped<UpdateHotelFOPermissionPolicyCommandHandler>(); services.AddScoped<ActivateHotelFOPermissionPolicyCommandHandler>(); services.AddScoped<DeactivateHotelFOPermissionPolicyCommandHandler>();
        services.AddScoped<CreateHotelFormPrinterProfileCommandHandler>(); services.AddScoped<UpdateHotelFormPrinterProfileCommandHandler>(); services.AddScoped<ActivateHotelFormPrinterProfileCommandHandler>(); services.AddScoped<DeactivateHotelFormPrinterProfileCommandHandler>();
        services.AddScoped<CreateHotelMessagingIntegrationProfileCommandHandler>(); services.AddScoped<UpdateHotelMessagingIntegrationProfileCommandHandler>(); services.AddScoped<ActivateHotelMessagingIntegrationProfileCommandHandler>(); services.AddScoped<DeactivateHotelMessagingIntegrationProfileCommandHandler>();
        services.AddScoped<CreateHotelPostedCorrectionPolicyCommandHandler>(); services.AddScoped<UpdateHotelPostedCorrectionPolicyCommandHandler>(); services.AddScoped<ActivateHotelPostedCorrectionPolicyCommandHandler>(); services.AddScoped<DeactivateHotelPostedCorrectionPolicyCommandHandler>();
        services.AddScoped<CreateHotelBusinessRuleProfileCommandHandler>(); services.AddScoped<UpdateHotelBusinessRuleProfileCommandHandler>(); services.AddScoped<ActivateHotelBusinessRuleProfileCommandHandler>(); services.AddScoped<DeactivateHotelBusinessRuleProfileCommandHandler>();
        services.AddScoped<CreateHotelTaxServiceChargeRuleCommandHandler>(); services.AddScoped<UpdateHotelTaxServiceChargeRuleCommandHandler>(); services.AddScoped<ActivateHotelTaxServiceChargeRuleCommandHandler>(); services.AddScoped<DeactivateHotelTaxServiceChargeRuleCommandHandler>();
        services.AddScoped<CreateHotelPaymentMethodProfileCommandHandler>(); services.AddScoped<UpdateHotelPaymentMethodProfileCommandHandler>(); services.AddScoped<ActivateHotelPaymentMethodProfileCommandHandler>(); services.AddScoped<DeactivateHotelPaymentMethodProfileCommandHandler>();
        services.AddScoped<CreateHotelDepositPolicyCommandHandler>(); services.AddScoped<UpdateHotelDepositPolicyCommandHandler>(); services.AddScoped<ActivateHotelDepositPolicyCommandHandler>(); services.AddScoped<DeactivateHotelDepositPolicyCommandHandler>();
        services.AddScoped<CreateHotelDiscountPolicyCommandHandler>(); services.AddScoped<UpdateHotelDiscountPolicyCommandHandler>(); services.AddScoped<ActivateHotelDiscountPolicyCommandHandler>(); services.AddScoped<DeactivateHotelDiscountPolicyCommandHandler>();
        services.AddScoped<CreateHotelChargeCodeProfileCommandHandler>(); services.AddScoped<UpdateHotelChargeCodeProfileCommandHandler>(); services.AddScoped<ActivateHotelChargeCodeProfileCommandHandler>(); services.AddScoped<DeactivateHotelChargeCodeProfileCommandHandler>();
        services.AddScoped<CreateHotelFolioProfileCommandHandler>(); services.AddScoped<UpdateHotelFolioProfileCommandHandler>(); services.AddScoped<ActivateHotelFolioProfileCommandHandler>(); services.AddScoped<DeactivateHotelFolioProfileCommandHandler>();
        services.AddScoped<CreateHotelGuestTypeProfileCommandHandler>(); services.AddScoped<UpdateHotelGuestTypeProfileCommandHandler>(); services.AddScoped<ActivateHotelGuestTypeProfileCommandHandler>(); services.AddScoped<DeactivateHotelGuestTypeProfileCommandHandler>();
        services.AddScoped<CreateHotelGuestPreferenceProfileCommandHandler>(); services.AddScoped<UpdateHotelGuestPreferenceProfileCommandHandler>(); services.AddScoped<ActivateHotelGuestPreferenceProfileCommandHandler>(); services.AddScoped<DeactivateHotelGuestPreferenceProfileCommandHandler>();
        services.AddScoped<CreateHotelGuestRequestProfileCommandHandler>(); services.AddScoped<UpdateHotelGuestRequestProfileCommandHandler>(); services.AddScoped<ActivateHotelGuestRequestProfileCommandHandler>(); services.AddScoped<DeactivateHotelGuestRequestProfileCommandHandler>();
        services.AddScoped<CreateHotelReservationTypeProfileCommandHandler>(); services.AddScoped<UpdateHotelReservationTypeProfileCommandHandler>(); services.AddScoped<ActivateHotelReservationTypeProfileCommandHandler>(); services.AddScoped<DeactivateHotelReservationTypeProfileCommandHandler>();
        services.AddScoped<CreateHotelBookingSourceProfileCommandHandler>(); services.AddScoped<UpdateHotelBookingSourceProfileCommandHandler>(); services.AddScoped<ActivateHotelBookingSourceProfileCommandHandler>(); services.AddScoped<DeactivateHotelBookingSourceProfileCommandHandler>();
        services.AddScoped<CreateHotelBookingChannelProfileCommandHandler>(); services.AddScoped<UpdateHotelBookingChannelProfileCommandHandler>(); services.AddScoped<ActivateHotelBookingChannelProfileCommandHandler>(); services.AddScoped<DeactivateHotelBookingChannelProfileCommandHandler>();
        services.AddScoped<CreateHotelMarketSegmentProfileCommandHandler>(); services.AddScoped<UpdateHotelMarketSegmentProfileCommandHandler>(); services.AddScoped<ActivateHotelMarketSegmentProfileCommandHandler>(); services.AddScoped<DeactivateHotelMarketSegmentProfileCommandHandler>();
        services.AddScoped<CreateHotelHousekeepingRuleProfileCommandHandler>(); services.AddScoped<UpdateHotelHousekeepingRuleProfileCommandHandler>(); services.AddScoped<ActivateHotelHousekeepingRuleProfileCommandHandler>(); services.AddScoped<DeactivateHotelHousekeepingRuleProfileCommandHandler>();
        services.AddScoped<CreateHotelWakeupRuleProfileCommandHandler>(); services.AddScoped<UpdateHotelWakeupRuleProfileCommandHandler>(); services.AddScoped<ActivateHotelWakeupRuleProfileCommandHandler>(); services.AddScoped<DeactivateHotelWakeupRuleProfileCommandHandler>();
        services.AddScoped<CreateHotelComplaintProfileCommandHandler>(); services.AddScoped<UpdateHotelComplaintProfileCommandHandler>(); services.AddScoped<ActivateHotelComplaintProfileCommandHandler>(); services.AddScoped<DeactivateHotelComplaintProfileCommandHandler>();
        services.AddScoped<CreateHotelCompanyProfileCommandHandler>(); services.AddScoped<UpdateHotelCompanyProfileCommandHandler>(); services.AddScoped<ActivateHotelCompanyProfileCommandHandler>(); services.AddScoped<DeactivateHotelCompanyProfileCommandHandler>();
        services.AddScoped<CreateHotelTravelAgentProfileCommandHandler>(); services.AddScoped<UpdateHotelTravelAgentProfileCommandHandler>(); services.AddScoped<ActivateHotelTravelAgentProfileCommandHandler>(); services.AddScoped<DeactivateHotelTravelAgentProfileCommandHandler>();
        services.AddScoped<CreateHotelCommissionRuleProfileCommandHandler>(); services.AddScoped<UpdateHotelCommissionRuleProfileCommandHandler>(); services.AddScoped<ActivateHotelCommissionRuleProfileCommandHandler>(); services.AddScoped<DeactivateHotelCommissionRuleProfileCommandHandler>();
        services.AddScoped<CreateHotelProfileTypeProfileCommandHandler>(); services.AddScoped<UpdateHotelProfileTypeProfileCommandHandler>(); services.AddScoped<ActivateHotelProfileTypeProfileCommandHandler>(); services.AddScoped<DeactivateHotelProfileTypeProfileCommandHandler>();
        services.AddScoped<CreateHotelVipLevelProfileCommandHandler>(); services.AddScoped<UpdateHotelVipLevelProfileCommandHandler>(); services.AddScoped<ActivateHotelVipLevelProfileCommandHandler>(); services.AddScoped<DeactivateHotelVipLevelProfileCommandHandler>();
        services.AddScoped<CreateHotelRoomCommandHandler>(); services.AddScoped<UpdateHotelRoomCommandHandler>(); services.AddScoped<ChangeHotelRoomTypeCommandHandler>(); services.AddScoped<ChangeHotelRoomOperationalStatusCommandHandler>(); services.AddScoped<ExtendHotelRoomLockCommandHandler>(); services.AddScoped<ActivateHotelRoomCommandHandler>(); services.AddScoped<DeactivateHotelRoomCommandHandler>();
        services.AddScoped<HotelRoomMasterDataQueryHandlers>();
        services.AddScoped<GetHotelRoomTypeByIdQueryHandler>(); services.AddScoped<GetHotelRoomTypeByCodeQueryHandler>(); services.AddScoped<SearchHotelRoomTypesQueryHandler>(); services.AddScoped<GetActiveHotelRoomTypesQueryHandler>();
        services.AddScoped<GetHotelRoomByIdQueryHandler>(); services.AddScoped<GetHotelRoomByNumberQueryHandler>(); services.AddScoped<SearchHotelRoomsQueryHandler>(); services.AddScoped<GetAssignableHotelRoomsQueryHandler>();
        services.AddScoped<ReservationApplicationService>();
        services.AddScoped<CreateReservationCommandHandler>(); services.AddScoped<UpdateReservationDetailsCommandHandler>(); services.AddScoped<ChangeReservationStayCommandHandler>(); services.AddScoped<ChangeReservationGuestCountCommandHandler>(); services.AddScoped<AssignReservationRoomCommandHandler>(); services.AddScoped<UnassignReservationRoomCommandHandler>(); services.AddScoped<MarkReservationTentativeCommandHandler>(); services.AddScoped<ConfirmReservationCommandHandler>(); services.AddScoped<CancelReservationCommandHandler>(); services.AddScoped<MarkReservationNoShowCommandHandler>(); services.AddScoped<GuaranteeReservationCommandHandler>(); services.AddScoped<RecordReservationDepositCommandHandler>();
        services.AddScoped<IOverbookingChecker>(sp => sp.GetRequiredService<EfReservationRepository>());
        services.AddScoped<GetReservationByIdQueryHandler>(); services.AddScoped<GetReservationByNumberQueryHandler>(); services.AddScoped<SearchReservationsQueryHandler>(); services.AddScoped<CheckRoomReservationConflictQueryHandler>();
        services.AddHostedService<ProcessTimeoutHostedService>();

        // === Financial Engine (M3) ===
        services.AddScoped<EfHotelFolioRepository>();
        services.AddScoped<IHotelFolioRepository>(sp => sp.GetRequiredService<EfHotelFolioRepository>());
        services.AddScoped<IHotelFolioNumberGenerator, SqlHotelFolioNumberGenerator>();
        services.AddScoped<HotelFolioApplicationService>();
        services.AddScoped<EfFolioRoutingService>();
        services.AddScoped<IFolioRoutingQueryService>(sp => sp.GetRequiredService<EfFolioRoutingService>());
        services.AddScoped<IFolioRoutingCommandService>(sp => sp.GetRequiredService<EfFolioRoutingService>());
        services.AddScoped<CreateFolioCommandHandler>(); services.AddScoped<AddFolioRoomChargeCommandHandler>(); services.AddScoped<AddFolioRestaurantChargeCommandHandler>(); services.AddScoped<AddFolioTaxCommandHandler>(); services.AddScoped<AddFolioServiceChargeCommandHandler>(); services.AddScoped<AddFolioDiscountCommandHandler>(); services.AddScoped<AddFolioPaymentCommandHandler>(); services.AddScoped<AddFolioDepositCommandHandler>(); services.AddScoped<AddFolioAdjustmentCommandHandler>(); services.AddScoped<AddFolioCorrectionCommandHandler>(); services.AddScoped<AddFolioManualChargeCommandHandler>(); services.AddScoped<AddFolioManualCreditCommandHandler>(); services.AddScoped<AddFolioRefundCommandHandler>(); services.AddScoped<AddFolioTransferCommandHandler>(); services.AddScoped<SplitFolioCommandHandler>(); services.AddScoped<MergeFoliosCommandHandler>(); services.AddScoped<ReverseFolioEntryCommandHandler>();
        services.AddScoped<SuspendFolioCommandHandler>(); services.AddScoped<ResumeFolioCommandHandler>(); services.AddScoped<CloseFolioCommandHandler>(); services.AddScoped<SettleFolioCommandHandler>(); services.AddScoped<ReopenFolioCommandHandler>();
        services.AddScoped<GetFolioQueryHandler>(); services.AddScoped<GetFolioByStayQueryHandler>(); services.AddScoped<GetFolioLedgerQueryHandler>(); services.AddScoped<SearchFoliosQueryHandler>();

        // === Settlement and hotel invoice application layer ===
        services.AddScoped<SettlementInfrastructure.EfHotelInvoiceRepository>();
        services.AddScoped<SettlementApplication.IHotelInvoiceRepository>(sp => sp.GetRequiredService<SettlementInfrastructure.EfHotelInvoiceRepository>());
        services.AddScoped<SettlementApplication.IHotelInvoiceNumberGenerator, SettlementInfrastructure.SqlHotelInvoiceNumberGenerator>();
        services.AddScoped<SettlementApplication.SettlementApplicationService>();
        services.AddScoped<SettlementApplication.SettleFolioCommandHandler>();
        services.AddScoped<SettlementApplication.CloseFolioCommandHandler>();
        services.AddScoped<SettlementApplication.GenerateInvoiceCommandHandler>();
        services.AddScoped<SettlementApplication.PrintInvoiceCommandHandler>();
        services.AddScoped<SettlementApplication.GetInvoiceQueryHandler>();
        services.AddScoped<SettlementApplication.GetInvoiceByFolioQueryHandler>();

        // === Guest and payment repositories (Phase 2B/6 real-data seam) ===
        services.AddScoped<EfGuestProfileRepository>();
        services.AddScoped<IGuestProfileRepository>(sp => sp.GetRequiredService<EfGuestProfileRepository>());
        services.AddScoped<EfHotelPaymentRepository>();
        services.AddScoped<IHotelPaymentRepository>(sp => sp.GetRequiredService<EfHotelPaymentRepository>());
        services.AddScoped<IHotelPaymentNumberGenerator, SqlHotelPaymentNumberGenerator>();
        // === Hotel payment application layer ===
        // CashierPaymentWindow, CreateReceiptVoucherWindow and RefundManagementWindow
        // resolve this service directly.  Without the registration they deliberately
        // fail closed even though the EF repositories and payment domain are available.
        services.AddScoped<HotelPaymentApplicationService>();
        services.AddScoped<ReceivePaymentCommandHandler>();
        services.AddScoped<SplitTenderCommandHandler>();
        services.AddScoped<AuthorizePaymentCommandHandler>();
        services.AddScoped<CapturePaymentCommandHandler>();
        services.AddScoped<CompletePaymentCommandHandler>();
        services.AddScoped<CancelPaymentCommandHandler>();
        services.AddScoped<AllocatePaymentCommandHandler>();
        services.AddScoped<AllocatePaymentBatchCommandHandler>();
        services.AddScoped<RefundPaymentCommandHandler>();
        services.AddScoped<VoidPaymentCommandHandler>();
        services.AddScoped<GetPaymentQueryHandler>();
        services.AddScoped<GetPaymentsForFolioQueryHandler>();
        services.AddScoped<GetAllocationsForPaymentQueryHandler>();
        services.AddScoped<GetPaymentHistoryQueryHandler>();
        services.AddScoped<GetOutstandingBalanceQueryHandler>();
        services.AddScoped<GetDepositSummaryQueryHandler>();
        services.AddScoped<GetSettlementSummaryQueryHandler>();
        services.AddScoped<ICashShiftService, EfCashShiftService>();
        services.AddScoped<ICashReconciliationService, EfCashReconciliationService>();
        services.AddScoped<ICashierDashboardService, EfCashierDashboardService>();
        services.AddScoped<EfIncidentService>();
        services.AddScoped<IIncidentQueryService>(sp => sp.GetRequiredService<EfIncidentService>());
        services.AddScoped<IIncidentCommandService>(sp => sp.GetRequiredService<EfIncidentService>());
        services.AddScoped<EfShiftLogEntryService>();
        services.AddScoped<IShiftLogEntryQueryService>(sp => sp.GetRequiredService<EfShiftLogEntryService>());
        services.AddScoped<IShiftLogEntryCommandService>(sp => sp.GetRequiredService<EfShiftLogEntryService>());
        services.AddScoped<EfRateRepository>();
        services.AddScoped<IRateRepository>(sp => sp.GetRequiredService<EfRateRepository>());
        services.AddScoped<EfExchangeRateService>();
        services.AddScoped<IExchangeRateQueryService>(sp => sp.GetRequiredService<EfExchangeRateService>());
        services.AddScoped<IExchangeRateCommandService>(sp => sp.GetRequiredService<EfExchangeRateService>());
        services.AddScoped<EfMarketingService>();
        services.AddScoped<IMarketingQueryService>(sp => sp.GetRequiredService<EfMarketingService>());
        services.AddScoped<IMarketingCommandService>(sp => sp.GetRequiredService<EfMarketingService>());
        services.AddScoped<IKeyCardService, EfKeyCardService>();
        services.AddScoped<IReportingQueryService, EfReportingQueryService>();
        services.AddScoped<IFolioBusinessDateBackfillService, EfFolioBusinessDateBackfillService>();
        services.AddScoped<IFrontOfficePostingEngine, FrontOfficePostingEngine>();
        services.AddScoped<IFinancialProcessService, FinancialProcessService>();
        services.AddScoped<IPropertyLockManager, PropertyLockManager>();
        services.AddScoped<EfNightAuditRepository>();
        services.AddScoped<INightAuditRepository>(sp => sp.GetRequiredService<EfNightAuditRepository>());
        services.AddScoped<INightAuditFinancialReader, EfNightAuditFinancialReader>();
        services.AddScoped<INightAuditReservationMetricsReader, EfNightAuditReservationMetricsReader>();
        services.AddScoped<INightAuditNumberGenerator, SqlNightAuditNumberGenerator>();
        services.AddScoped<NightAuditApplicationService>();
        services.AddScoped<BeginNightAuditCommandHandler>();
        services.AddScoped<ExecuteNightAuditStepCommandHandler>();
        services.AddScoped<ResumeNightAuditCommandHandler>();
        services.AddScoped<FailNightAuditCommandHandler>();
        services.AddScoped<CancelNightAuditCommandHandler>();
        services.AddScoped<GetCurrentNightAuditQueryHandler>();
        services.AddScoped<GetCurrentBusinessDateQueryHandler>();
        services.AddScoped<GetRevenueSnapshotQueryHandler>();
        services.AddScoped<IRoomingListService, EfRoomingListService>();
        services.AddScoped<IGroupCheckOutCommandService, EfGroupCheckOutCommandService>();
        services.AddScoped<IRoomBlockService, EfRoomBlockService>();
        services.AddScoped<IGroupBillingService, EfGroupBillingService>();
        services.AddScoped<IGroupDepositService, EfGroupDepositService>();
        services.AddScoped<IGroupDashboardService, EfGroupDashboardService>();
        services.AddScoped<IGroupReservationQueryService, EfGroupDashboardService>();
        services.AddScoped<IGroupReservationCommandService, EfGroupReservationCommandService>();
        services.AddScoped<IGroupCheckInCommandService, EfGroupCheckInCommandService>();
        services.AddScoped<IGroupRoomAssignmentCommandService, EfGroupRoomAssignmentCommandService>();
        services.AddScoped<EfGroupServiceOrderService>();
        services.AddScoped<IGroupServiceOrderQueryService>(sp => sp.GetRequiredService<EfGroupServiceOrderService>());
        services.AddScoped<IGroupServiceOrderCommandService>(sp => sp.GetRequiredService<EfGroupServiceOrderService>());
        services.AddScoped<IWorkOrderService, EfWorkOrderService>();
        services.AddScoped<IAssetService, EfMaintenanceAssetService>();
        services.AddScoped<IPreventiveService, EfPreventiveMaintenanceService>();
        services.AddScoped<IDoorLockService, EfDoorLockService>();
        services.AddScoped<IDoorEventService, EfDoorEventService>();
        services.AddScoped<IDoorLockDashboardService, EfDoorLockDashboardService>();
        services.AddScoped<IServiceRequestService, EfServiceRequestService>();
        services.AddScoped<IWakeUpCallService, EfWakeUpCallService>();
        services.AddScoped<ILuggageService, EfLuggageService>();
        services.AddScoped<ITransportationService, EfTransportationService>();
        services.AddScoped<IConciergeDashboardService, EfConciergeDashboardService>();
        services.AddScoped<IGuestMessageService, EfGuestMessageService>();
        services.AddScoped<ITelephoneService, EfTelephoneService>();
        services.AddScoped<IReservationDenialService, EfReservationDenialService>();

        services.AddScoped<IUnitOfWork, EfUnitOfWork>();
        services.AddScoped<IItemRepository, ItemRepository>();
        services.AddScoped<ICustomerRepository, CustomerRepository>();
        services.AddScoped<ISupplierRepository, SupplierRepository>();
        services.AddScoped<IWarehouseRepository, WarehouseRepository>();
        services.AddScoped<IDepartmentRepository, DepartmentRepository>();
        services.AddScoped<IPersonRepository, PersonRepository>();
        services.AddScoped<IStockReceiptRepository, StockReceiptRepository>();
        services.AddScoped<IStockIssueRepository, StockIssueRepository>();
        services.AddScoped<IStockBalanceRepository, StockBalanceRepository>();

        services.AddScoped<IVirtualExplorerRepository, VirtualExplorerRepository>();
        services.AddScoped<IClientRegistryService, ClientRegistryService>();
        services.AddScoped<IBusinessDocumentRepository, BusinessDocumentRepository>();

        services.AddScoped<IAuditLogService, AuditLogService>();
        services.AddScoped<IAuditService, AuditService>();
        services.AddScoped<IAuditSessionService, AuditSessionService>();
        services.AddScoped<IAuditReplayService, AuditReplayService>();
        services.AddSingleton<RuntimeTelemetryService>();
        services.AddSingleton<IRuntimeTelemetryService>(sp => sp.GetRequiredService<RuntimeTelemetryService>());
        services.AddSingleton<IHostedService>(sp => sp.GetRequiredService<RuntimeTelemetryService>());
        services.AddScoped<IFailureRecoveryService, FailureRecoveryService>();
        services.AddScoped<IArchiveService, ArchiveService>();
        services.AddScoped<IRecoveryService, RecoveryService>();
        services.AddScoped<IReconciliationEngine, ReconciliationEngine>();
        services.AddScoped<IExportPipelineService, ExportPipelineService>();
        services.AddScoped<IDataIntegrityService, DataIntegrityService>();
        services.AddScoped<ICompliancePackagingService, CompliancePackagingService>();
        services.AddScoped<ITT99ComplianceService, TT99ComplianceService>();
        services.AddScoped<ITT200ComplianceService, TT200ComplianceService>();
        services.AddScoped<IApprovalWorkflowService, ApprovalWorkflowService>();
        services.AddScoped<IWorkflowResilienceService, WorkflowResilienceService>();
        services.AddScoped<IEnterpriseConsistencyValidatorService, EnterpriseConsistencyValidatorService>();
        services.AddScoped<IDatabaseMigrationService, DatabaseMigrationService>();
        services.AddScoped<DatabaseMigrationService>(sp => (DatabaseMigrationService)sp.GetRequiredService<IDatabaseMigrationService>());
        services.AddScoped<IInfrastructureHealthService, InfrastructureHealthService>();
        services.AddScoped<ISchedulerService, SchedulerService>();
        services.AddScoped<IBackgroundSchedulerService, BackgroundSchedulerService>();
        services.AddHostedService<BackgroundSchedulerHostedService>();
        services.AddScoped<INotificationTaskService, NotificationTaskService>();
        services.AddHostedService<NotificationConditionHostedService>();
        services.AddScoped<ISqlPerformanceScalingService, SqlPerformanceScalingService>();
        services.AddScoped<ISystemSettingsService, EfSystemSettingsService>();
        services.AddScoped<IDatabaseAdminService, DatabaseAdminService>();
        services.AddScoped<IBackupService>(sp => new SqlServerBackupService(
            sp.GetRequiredService<AppDbContext>(),
            sp.GetRequiredService<IOptions<DatabaseOptions>>(),
            sp.GetRequiredService<IPathSecurityService>(),
            sp.GetRequiredService<ICurrentUserService>(),
            sp.GetRequiredService<ISettingsService>()));
        services.AddScoped<IFileBackupService, FileBackupService>();
        services.AddScoped<IServiceStatusService, WindowsServiceStatusService>();
        services.AddScoped<IWindowsFirewallService, WindowsFirewallService>();
        services.AddSingleton<IWindowsStartupAppsService, WindowsStartupAppsService>();
        services.AddSingleton<IWindowsSettingsLauncher, WindowsSettingsLauncher>();
        services.AddSingleton<ILicenseService, SignedLicenseService>();
        services.AddSingleton<IUpdateManifestService, SignedUpdateManifestService>();
        services.AddSingleton(_ => new HttpClient(new HttpClientHandler { AllowAutoRedirect = false })
        {
            Timeout = TimeSpan.FromHours(1)
        });
        services.AddScoped<IUpdatePackageStagingService, UpdatePackageStagingService>();
        services.AddScoped<ISecuritySettingsService, EfSecuritySettingsService>();
        services.AddScoped<IPrinterSettingsService, WindowsPrinterSettingsService>();
        services.AddScoped<IReceivableService, EfReceivableService>();
        services.AddScoped<IPayableService, EfPayableService>();
        services.AddScoped<IInventoryMovementService, EfInventoryMovementService>();
        services.AddScoped<IGeneralLedgerService, EfGeneralLedgerService>();
        services.AddScoped<IAccountingPeriodService, AccountingPeriodService>();
        services.AddScoped<IDebtTransactionService, DebtTransactionService>();
        services.AddScoped<IInventoryCostingService, InventoryCostingService>();
        services.AddScoped<IImportEngine, ImportEngine>();
        services.AddScoped<IImportTemplateHandler, ItemImportTemplateHandler>();
        services.AddScoped<IImportTemplateHandler, BankStatementImportTemplateHandler>();
        services.AddScoped<IReportEngine, ReportEngine>();
        services.AddScoped<IAccountingStandardService, AccountingStandardService>();
        services.AddScoped<ICashFundService, CashFundService>();
        services.AddScoped<IBankingService, BankingService>();
        services.AddScoped<IFixedAssetService, FixedAssetService>();
        services.AddScoped<IToolAssetService, ToolAssetService>();
        services.AddScoped<ITaxService, TaxService>();
        services.AddScoped<IEInvoiceService, EInvoiceService>();
        services.AddScoped<IFinancialStatementMappingService, FinancialStatementMappingService>();
        services.AddScoped<IEInvoiceXmlGenerator, EInvoiceXmlGenerator>();
        // Do not register the local test provider in production.  Until a real
        // provider adapter and its credentials are configured, issuing an
        // e-invoice must fail closed instead of persisting a fabricated
        // provider invoice number/status.
        services.AddScoped<IEInvoiceProvider>(_ => new UnavailableEInvoiceProvider("VIETTEL"));
        services.AddScoped<IEInvoiceProvider>(_ => new UnavailableEInvoiceProvider("VNPT"));
        services.AddScoped<IEInvoiceProvider>(_ => new UnavailableEInvoiceProvider("MISA"));
        services.AddScoped<IFinancialStatementService, FinancialStatementService>();
        services.AddScoped<IClosingService, ClosingService>();
        services.AddScoped<IPostingRuleService, PostingRuleService>();
        services.AddScoped<IJournalService, JournalService>();
        services.AddScoped<IPostingEngine, PostingEngine>();
        services.AddScoped<IPermissionService, PermissionService>();
        services.AddScoped<IWorkspaceService, WorkspaceService>();
        services.AddScoped<IEnterpriseSearchService, EnterpriseSearchService>();
        // Settings pages can load and save while another settings/runtime
        // provider is querying the shell context. Use isolated factory
        // contexts for the settings repositories instead of sharing the
        // scoped AppDbContext and triggering EF's second-operation error.
        services.AddScoped<ISettingsRepository>(sp =>
            new EfSettingsRepository(sp.GetRequiredService<IDbContextFactory<AppDbContext>>()));
        services.AddScoped<IAppModuleRepository>(sp =>
            new EfAppModuleRepository(sp.GetRequiredService<IDbContextFactory<AppDbContext>>()));
        services.AddScoped<ISettingChangeLogRepository>(sp =>
            new EfSettingChangeLogRepository(sp.GetRequiredService<IDbContextFactory<AppDbContext>>()));
        services.AddSingleton<ISettingsRegistry, SettingsRegistry>();
        services.AddSingleton<ISettingsValidator, SettingsValidator>();
        services.AddScoped<ISettingsAuditService, SettingsAuditService>();
        services.AddSingleton<ISettingsChangeNotifier, SettingsChangeNotifier>();
        services.AddScoped<ISettingsService, SettingsService>();
        services.AddSingleton<IErrorLogQueryService, ConfiguredErrorLogQueryService>();
        services.AddScoped<IAppModuleService, AppModuleService>();
        services.AddScoped<ITrainingDataService, TrainingDataService>();
        services.AddScoped<ITrainingDataCleaner, TrainingDataCleaner>();
        services.AddScoped<ITrainingDataSeeder, TrainingDataSeeder>();
        services.AddScoped<ITrainingDataValidator, TrainingDataValidator>();
        services.AddScoped<IMenuCatalogService, MenuCatalogService>();
        services.AddScoped<IMenuRecipeService, MenuRecipeService>();
        services.AddSingleton<ITableAllocationEngine, TableAllocationEngine>();
        services.AddSingleton<IMenuCalculationEngine, MenuCalculationEngine>();
        services.AddSingleton<IRoomOrderMenuPlanningEngine, RoomOrderMenuPlanningEngine>();
        services.AddScoped<IBreakfastCafeCostingService, BreakfastCafeCostingService>();
        services.AddScoped<IBreakfastCafeSalesService, BreakfastCafeSalesService>();
        services.AddScoped<IAlacarteRestaurantService, AlacarteRestaurantService>();
        services.AddScoped<IBanquetRestaurantService, BanquetRestaurantService>();
        services.AddScoped<IQuoteChangeService, QuoteChangeService>();

        return services;
    }

    private static bool ValidateOptions<TOptions>(TOptions options)
    {
        switch (options)
        {
            case AccountingOptions accounting:
                accounting.Validate();
                return true;
            case InventoryOptions inventory:
                inventory.Validate();
                return true;
            case SecurityOptions security:
                security.Validate();
                return true;
            default:
                return true;
        }
    }
}
