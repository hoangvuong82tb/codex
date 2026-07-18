using ASMILE.BO.App.Services;
using ASMILE.BO.App.ViewModels;
using ASMILE.BO.App.ViewModels.Auth;
using ASMILE.BO.App.Views.Auth;
using ASMILE.BO.Application.Interfaces;
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
        services.AddSingleton<IModuleSettingsService, ModuleSettingsService>();
        services.AddSingleton<ISettingsValidationService, ModuleSettingsValidationService>();
        services.AddSingleton<IToastNotificationService, ToastNotificationService>();

        services.AddTransient<DashboardViewModel>();
        services.AddTransient<ItemsViewModel>();
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
        services.AddTransient<OperationsCenterViewModel>();
        services.AddTransient<FiscalPeriodManagementViewModel>();
        services.AddTransient<NotificationTaskCenterViewModel>();
        services.AddTransient<TrainingDataViewModel>();
        services.AddTransient<MenuRecipeViewModel>();
        services.AddTransient<BreakfastCafeSalesViewModel>();
        services.AddTransient<BreakfastCafeSettingsViewModel>();
        services.AddTransient<AlacarteRestaurantViewModel>();
        services.AddScoped<BanquetRestaurantViewModel>();
        services.AddTransient<IReservationUiService, ReservationUiService>();
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
