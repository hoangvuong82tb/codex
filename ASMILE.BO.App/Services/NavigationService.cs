using ASMILE.BO.Application.Interfaces;
using ASMILE.BO.App.Models;
using ASMILE.BO.App.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace ASMILE.BO.App.Services;

public sealed class NavigationService : INavigationService
{
    private readonly IWindowManagerService _windowManagerService;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IAuditCorrelationService _auditCorrelationService;
    private readonly IFrontOfficePropertyContextHost _frontOfficePropertyContextHost;
    private readonly List<NavigationHistoryItem> _history = [];
    private int _historyIndex = -1;

    public NavigationService(
        IWindowManagerService windowManagerService,
        IServiceScopeFactory scopeFactory,
        IAuditCorrelationService auditCorrelationService,
        IFrontOfficePropertyContextHost frontOfficePropertyContextHost)
    {
        _windowManagerService = windowManagerService;
        _scopeFactory = scopeFactory;
        _auditCorrelationService = auditCorrelationService;
        _frontOfficePropertyContextHost = frontOfficePropertyContextHost;
    }

    public event Action<string>? Navigated;

    public void Navigate(string key)
    {
        Navigated?.Invoke(key);
    }

    public IReadOnlyList<AppModuleInfo> GetModuleCatalog()
    {
        var iconBase = @"D:\ASMILE-BO\Windows_Icon\Icons\applications\";
        var logoBase = @"D:\ASMILE-BO\Windows_Icon\Icons\logo\";

        return
        [
            new AppModuleInfo("dashboard", "Tổng quan", "\uE80F", "Bảng điều hành", "#4C6FFF", true, true, iconBase + "start2.ico"),
            new AppModuleInfo("virtual-file-center", "Virtual File Center", "\uE8B7", "My Computer dữ liệu ảo", "#3A7BFF", true, true, logoBase + "u2.png"),
            new AppModuleInfo("inventory", "Quản lý xuất nhập tồn", "\uE8D1", "Kho vận", "#2D9CDB", true, true, iconBase + "warehouse.ico"),
            new AppModuleInfo("ar", "Công nợ phải thu", "\uEAFD", "Tài chính", "#2FB67A", true, true, iconBase + "finance.ico"),
            new AppModuleInfo("ap", "Công nợ phải trả", "\uF0D6", "Tài chính", "#F2994A", true, true, iconBase + "pricing.ico"),
            new AppModuleInfo("cash-bank", "Quỹ & Ngân hàng", "\uE8C7", "Sổ quỹ và ngân hàng", "#2563EB", true, true, iconBase + "commerce.ico"),
            new AppModuleInfo("asset-management", "TSCĐ & CCDC", "\uE8A5", "Tài sản và công cụ dụng cụ", "#475569", true, true, iconBase + "operations.ico"),
            new AppModuleInfo("tax-financial", "Thuế & BCTC", "\uE9D2", "Thuế và báo cáo tài chính", "#0F766E", true, true, iconBase + "insights.ico"),
            new AppModuleInfo("general-ledger", "Sổ cái", "\uE9D9", "Kế toán tổng hợp", "#4B6BFB", true, true, iconBase + "insights3.ico"),
            new AppModuleInfo("customers", "Quản lý khách hàng", "\uE77B", "Danh mục", "#56CCF2", true, true, iconBase + "people.ico"),
            new AppModuleInfo("suppliers", "Quản lý nhà cung cấp", "\uE716", "Danh mục", "#6FCF97", true, true, iconBase + "supply.ico"),
            new AppModuleInfo("items", "Quản lý hàng hóa", "\uE14C", "Danh mục", "#5B8DEF", true, true, iconBase + "package.ico"),
            new AppModuleInfo("warehouses", "Quản lý kho", "\uE8D1", "Danh mục", "#3F8CFF", true, true, iconBase + "store3.ico"),
            new AppModuleInfo("departments", "Bộ phận & Nhân sự", "\uE902", "Tổ chức vận hành", "#8B80F9", true, true, iconBase + "lists.ico"),
            new AppModuleInfo("payment-receipt", "Phiếu thu", "\uE8C7", "Nghiệp vụ thu tiền", "#2FB67A", true, true, iconBase + "commerce.ico"),
            new AppModuleInfo("payment-voucher", "Phiếu chi", "\uE8C8", "Nghiệp vụ chi tiền", "#EB5757", true, true, iconBase + "operations.ico"),
            new AppModuleInfo("receipts", "Phiếu nhập", "\uE8A7", "Nghiệp vụ chứng từ", "#27AE60", true, true, iconBase + "import.ico"),
            new AppModuleInfo("issues", "Phiếu xuất", "\uE8A8", "Nghiệp vụ chứng từ", "#F2994A", true, true, iconBase + "order.ico"),
            new AppModuleInfo("report-center", "Trung tâm báo cáo", "\uE9D2", "ReportEngine persisted, chạy báo cáo và xuất Excel/PDF/CSV", "#2563EB", true, true, iconBase + "insights.ico"),
            new AppModuleInfo("stock-report", "Báo cáo tồn kho", "\uE9D2", "Báo cáo", "#2D9CDB", true, false, iconBase + "insights.ico"),
            new AppModuleInfo("debt-report", "Báo cáo công nợ", "\uE9D9", "Báo cáo", "#EB5757", true, false, iconBase + "insights3.ico"),
            new AppModuleInfo("settings", "Cài đặt hệ thống", "\uE713", "Thiết lập", "#9AA5B1", true, true, iconBase + "settings.ico"),
            new AppModuleInfo("security-admin", "Quản trị bảo mật", "\uE72E", "Vai trò và phân quyền", "#34495E", true, true, iconBase + "protection.ico"),
            new AppModuleInfo("workspace-settings", "Thiết lập không gian làm việc", "\uE713", "Tùy biến workspace", "#64748B", true, false, iconBase + "settings.ico"),
            new AppModuleInfo("audit-center", "Trung tâm kiểm tra hệ thống", "\uE9D9", "Nhật ký và truy vết vận hành", "#7C3AED", true, true, iconBase + "insights3.ico"),
            new AppModuleInfo("operations-center", "Trung tâm vận hành", "\uE895", "Jobs, hạ tầng và giám sát", "#0F766E", true, true, iconBase + "operations.ico"),
            new AppModuleInfo("fiscal-periods", "Kỳ kế toán", "\uE787", "Năm tài chính và khóa kỳ", "#1D4ED8", true, true, iconBase + "calendar.ico"),
            new AppModuleInfo("notification-center", "Trung tâm thông báo", "\uE7F4", "Thông báo và công việc", "#DC2626", true, true, iconBase + "notifications.ico"),
            new AppModuleInfo("authorization", "Phân quyền người dùng", "\uE72E", "Bảo mật", "#34495E", true, false, iconBase + "protection.ico"),
            new AppModuleInfo("training-data", "Dữ liệu mẫu", "\uE8B8", "Onboarding, reset và seed demo", "#7C3AED", true, true, iconBase + "data.ico"),
            new AppModuleInfo("menu-recipe", "Món ăn & Định lượng", "\uE8D2", "Quản lý món và định lượng nguyên vật liệu", "#0EA5E9", true, true, iconBase + "commerce.ico"),
            new AppModuleInfo("restaurant-alacarte", "Nhà hàng À La Carte", "\uE7BF", "Quản lý phòng ăn, bàn, đặt bàn, gọi món và thanh toán", "#0EA5E9", true, true, iconBase + "commerce.ico"),
            new AppModuleInfo("restaurant-banquet", "Nhà hàng Tiệc", "\uE8D2", "Quản lý phòng tiệc, sự kiện, hợp đồng, đặt cọc và checklist", "#0F766E", true, true, iconBase + "calendar.ico"),
            new AppModuleInfo("front-office-reservations", "Quan ly khach san", "\uE787", "Chon phan he de bat dau lam viec", "#2563EB", true, true, iconBase + "calendar.ico"),
            new AppModuleInfo("breakfast-cafe-sales", "Bán đồ ăn sáng & cà phê", "\uEADF", "Bán nhanh cho nhà hàng/khách sạn/café", "#2563EB", true, true, iconBase + "commerce.ico"),
            new AppModuleInfo("breakfast-cafe-settings", "Cài đặt Breakfast & Coffee", "\uE713", "Cấu hình POS nhà hàng và quầy cà phê", "#2563EB", true, false, iconBase + "settings.ico")
        ];
    }

    public string GetPermissionCode(string route)
    {
        return NormalizeRoute(route) switch
        {
            "dashboard" => "dashboard.view",
            "virtual-file-center" => "virtual-file-center.view",
            "inventory" or "inventory-movement" or "warehouses" or "receipts" or "issues" => "inventory.view",
            "ar" or "receivable" => "receivable.view",
            "ap" or "payable" => "payable.view",
            "cash-bank" or "cash" => "cash.view",
            "bank" => "bank.view",
            "asset-management" or "fixed-assets" or "tool-assets" => "asset.view",
            "tax-financial" or "tax" => "tax.view",
            "financial-statements" => "financial.view",
            "general-ledger" => "general-ledger.view",
            "customers" or "suppliers" or "items" => "master-data.view",
            "departments" => "hr.view",
            "payment-receipt" or "payment-voucher" => "cash.view",
            "report-center" or "stock-report" or "debt-report" => "reports.view",
            "settings" => "settings.view",
            "security-admin" or "authorization" => "security.admin",
            "workspace-settings" => "settings.view",
            "audit-center" => "audit.view",
            "operations-center" => "operations.view",
            "fiscal-periods" => "general-ledger.view",
            "notification-center" => "dashboard.view",
            "training-data" => "training-data.admin",
            "menu-recipe" => "menu-recipe.view",
            "restaurant-alacarte" => "restaurant.alacarte.view",
            "restaurant-banquet" => "restaurant.banquet.view",
            "front-office-reservations" => "FrontOffice.Reservation.View",
            "breakfast-cafe-sales" => "breakfast-cafe.view",
            "breakfast-cafe-settings" => "breakfast-cafe-settings.view",
            _ => "dashboard.view"
        };
    }

    public Task<NavigationResult> OpenModuleAsync(string route, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return OpenTargetAsync(new NavigationRequest
        {
            Target = new NavigationTarget { TargetType = "module", Route = NormalizeRoute(route) }
        }, cancellationToken);
    }

    public Task<NavigationResult> OpenJournalAsync(Guid journalId, CancellationToken cancellationToken = default)
    {
        return OpenTargetAsync(new NavigationRequest
        {
            Target = new NavigationTarget { TargetType = "journal", Route = "general-ledger", EntityId = journalId, DisplayTitle = "Chi tiết nhật ký" },
            Context = new NavigationContext { Breadcrumb = $"Nhật ký > {journalId}" }
        }, cancellationToken);
    }

    public Task<NavigationResult> OpenVoucherAsync(string sourceType, Guid sourceId, CancellationToken cancellationToken = default)
    {
        var route = ResolveSourceRoute(sourceType);
        return OpenTargetAsync(new NavigationRequest
        {
            Target = new NavigationTarget { TargetType = "voucher", Route = route, EntityId = sourceId, SourceType = sourceType, DisplayTitle = $"{sourceType} {sourceId}" },
            Context = new NavigationContext { Breadcrumb = $"{sourceType} > {sourceId}" }
        }, cancellationToken);
    }

    public Task<NavigationResult> OpenReportAsync(string reportCode, CancellationToken cancellationToken = default)
    {
        var route = ResolveReportRoute(reportCode);
        return OpenTargetAsync(new NavigationRequest
        {
            Target = new NavigationTarget { TargetType = "report", Route = route, SourceType = "Report", DisplayTitle = reportCode },
            Context = new NavigationContext { SourceReport = reportCode, Breadcrumb = $"Báo cáo > {reportCode}" }
        }, cancellationToken);
    }

    public Task<NavigationResult> OpenInNewTabAsync(NavigationRequest request, CancellationToken cancellationToken = default)
    {
        request.OpenInNewTab = true;
        return OpenTargetAsync(request, cancellationToken);
    }

    public Task<NavigationResult> GoBackAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (_historyIndex <= 0)
        {
            return Task.FromResult(new NavigationResult { Success = false, Message = "Không còn lịch sử phía trước." });
        }

        _historyIndex--;
        return OpenTargetAsync(new NavigationRequest { Target = _history[_historyIndex].Target, Context = _history[_historyIndex].Context }, cancellationToken, false);
    }

    public Task<NavigationResult> GoForwardAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (_historyIndex >= _history.Count - 1)
        {
            return Task.FromResult(new NavigationResult { Success = false, Message = "Không còn lịch sử phía sau." });
        }

        _historyIndex++;
        return OpenTargetAsync(new NavigationRequest { Target = _history[_historyIndex].Target, Context = _history[_historyIndex].Context }, cancellationToken, false);
    }

    public async Task<IReadOnlyList<NavigationHistoryItem>> RestoreWorkspaceAsync(CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var workspace = scope.ServiceProvider.GetRequiredService<IWorkspaceService>();
        var state = await workspace.LoadWorkspaceAsync(cancellationToken);
        return state.LastOpenedTabs
            .Select(x => new NavigationHistoryItem { Target = new NavigationTarget { TargetType = "module", Route = x } })
            .ToArray();
    }

    private async Task<NavigationResult> OpenTargetAsync(NavigationRequest request, CancellationToken cancellationToken, bool recordHistory = true)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var route = NormalizeRoute(request.Target.Route);
        if (route == "front-office-reservations" && !_frontOfficePropertyContextHost.HasCurrentProperty)
        {
            return new NavigationResult { Success = false, Route = route, Message = "Vui long chon chi nhanh truoc khi mo module Dat phong." };
        }
        var module = GetModuleCatalog().FirstOrDefault(x => string.Equals(x.Key, route, StringComparison.OrdinalIgnoreCase));
        if (module is null)
        {
            module = new AppModuleInfo($"drilldown-{request.Target.TargetType}-{request.Target.EntityId}", request.Target.DisplayTitle ?? "Xem dữ liệu liên quan", "\uE8A5", "Màn hình truy vết", "#64748B", false, false);
            _windowManagerService.OpenOrActivate(module, () => new ModulePlaceholderViewModel(module.Title, BuildFallbackDescription(request)));
        }
        else
        {
            IServiceScope? moduleScope = null;
            AppWindowViewModel window;
            try
            {
                window = _windowManagerService.OpenOrActivate(module, () =>
                {
                    moduleScope = _scopeFactory.CreateScope();
                    try
                    {
                        return ResolveModuleViewModel(module.Key, request, moduleScope.ServiceProvider);
                    }
                    catch
                    {
                        moduleScope.Dispose();
                        moduleScope = null;
                        throw;
                    }
                });
            }
            catch (Exception ex)
            {
                // Keep the UI message generic, but preserve route and scope evidence for diagnosis.
                // The DI exception itself contains the requested ViewModel type and resolution chain.
                Log.Error(ex,
                    "Navigation module activation failed. Route={Route}; ModuleKey={ModuleKey}; ModuleScopeCreated={ModuleScopeCreated}; Resolver=ScopedServiceProvider",
                    route,
                    module.Key,
                    moduleScope is not null);
                throw;
            }

            // A module ViewModel can depend on scoped infrastructure services. The scope must
            // live exactly as long as its desktop window, not merely for the navigation call.
            if (moduleScope is not null)
            {
                window.ServiceScope = moduleScope;
            }
        }
        Navigate(route);

        if (recordHistory)
        {
            AddHistory(request, _auditCorrelationService.EnsureCorrelationId(request.Context.CorrelationId));
        }

        using var scope = _scopeFactory.CreateScope();
        var workspace = scope.ServiceProvider.GetRequiredService<IWorkspaceService>();
        await workspace.AddRecentModuleAsync(module.Key, cancellationToken);
        if (!string.IsNullOrWhiteSpace(request.Context.SourceReport))
        {
            await workspace.AddRecentReportAsync(request.Context.SourceReport, cancellationToken);
        }

        if (request.Target.EntityId.HasValue)
        {
            await workspace.AddRecentDrilldownAsync($"{request.Target.TargetType}:{request.Target.SourceType}:{request.Target.EntityId}", cancellationToken);
        }

        try
        {
            var audit = scope.ServiceProvider.GetRequiredService<IAuditService>();
            await audit.LogAsync(
                "Navigation",
                request.Target.EntityId?.ToString() ?? module.Key,
                "Open",
                null,
                new
                {
                    module = module.Key,
                    route,
                    request.Target.TargetType,
                    request.Target.EntityId,
                    request.Target.SourceType,
                    request.Context.SourceModule,
                    request.Context.SourceReport,
                    request.Context.SourceRowKey,
                    request.Context.Breadcrumb
                },
                correlationId: _auditCorrelationService.EnsureCorrelationId(request.Context.CorrelationId),
                cancellationToken: cancellationToken);
        }
        catch
        {
        }

        return new NavigationResult { Success = true, Route = module.Key, Title = module.Title };
    }

    private static object ResolveModuleViewModel(string route, NavigationRequest request, IServiceProvider serviceProvider)
    {
        return NormalizeRoute(route) switch
        {
            "dashboard" => serviceProvider.GetRequiredService<DashboardViewModel>(),
            "virtual-file-center" => serviceProvider.GetRequiredService<VirtualExplorerViewModel>(),
            "inventory" or "inventory-movement" => serviceProvider.GetRequiredService<InventoryMovementViewModel>(),
            "ar" or "receivable" => serviceProvider.GetRequiredService<ReceivableViewModel>(),
            "ap" or "payable" => serviceProvider.GetRequiredService<PayableViewModel>(),
            "cash-bank" or "cash" or "bank" => serviceProvider.GetRequiredService<CashBankViewModel>(),
            "asset-management" or "fixed-assets" or "tool-assets" => serviceProvider.GetRequiredService<AssetManagementViewModel>(),
            "tax-financial" or "tax" or "financial-statements" => serviceProvider.GetRequiredService<TaxFinancialReportingViewModel>(),
            "general-ledger" => serviceProvider.GetRequiredService<GeneralLedgerViewModel>(),
            "items" => serviceProvider.GetRequiredService<ItemsViewModel>(),
            "customers" => serviceProvider.GetRequiredService<CustomerPageViewModel>(),
            "suppliers" => serviceProvider.GetRequiredService<SupplierPageViewModel>(),
            "departments" => serviceProvider.GetRequiredService<DepartmentPeoplePageViewModel>(),
            "warehouses" => serviceProvider.GetRequiredService<WarehousesViewModel>(),
            "receipts" => serviceProvider.GetRequiredService<StockReceiptsViewModel>(),
            "issues" => serviceProvider.GetRequiredService<StockIssuesViewModel>(),
            "payment-receipt" => serviceProvider.GetRequiredService<PaymentReceiptsViewModel>(),
            "payment-voucher" => serviceProvider.GetRequiredService<PaymentVouchersViewModel>(),
            "report-center" or "stock-report" or "debt-report" => serviceProvider.GetRequiredService<ReportCenterViewModel>(),
            "settings" => serviceProvider.GetRequiredService<SettingsViewModel>(),
            "security-admin" or "authorization" => serviceProvider.GetRequiredService<SecurityAdminViewModel>(),
            "workspace-settings" => serviceProvider.GetRequiredService<WorkspaceSettingsViewModel>(),
            "audit-center" => serviceProvider.GetRequiredService<AuditCenterViewModel>(),
            "operations-center" => serviceProvider.GetRequiredService<OperationsCenterViewModel>(),
            "fiscal-periods" => serviceProvider.GetRequiredService<FiscalPeriodManagementViewModel>(),
            "notification-center" => serviceProvider.GetRequiredService<NotificationTaskCenterViewModel>(),
            "training-data" => serviceProvider.GetRequiredService<TrainingDataViewModel>(),
            "menu-recipe" => serviceProvider.GetRequiredService<MenuRecipeViewModel>(),
            "restaurant-alacarte" => serviceProvider.GetRequiredService<AlacarteRestaurantViewModel>(),
            "restaurant-banquet" => serviceProvider.GetRequiredService<BanquetRestaurantViewModel>(),
            "front-office-reservations" => serviceProvider.GetRequiredService<FrontOfficeHubViewModel>(),
            "breakfast-cafe-sales" => serviceProvider.GetRequiredService<BreakfastCafeSalesViewModel>(),
            "breakfast-cafe-settings" => serviceProvider.GetRequiredService<BreakfastCafeSettingsViewModel>(),
            _ => new ModulePlaceholderViewModel(request.Target.DisplayTitle ?? route, BuildFallbackDescription(request))
        };
    }

    private void AddHistory(NavigationRequest request, string correlationId)
    {
        if (_historyIndex < _history.Count - 1)
        {
            _history.RemoveRange(_historyIndex + 1, _history.Count - _historyIndex - 1);
        }

        request.Context.CorrelationId ??= correlationId;
        _history.Add(new NavigationHistoryItem { Target = request.Target, Context = request.Context, CorrelationId = correlationId });
        _historyIndex = _history.Count - 1;
    }

    private static string ResolveSourceRoute(string sourceType)
    {
        return sourceType.ToLowerInvariant() switch
        {
            "journal" or "journalheader" => "general-ledger",
            "stockreceipt" or "inventoryreceipt" or "receipt" => "receipts",
            "stockissue" or "inventoryissue" or "issue" => "issues",
            "cashtransaction" or "cashvoucher" => "cash-bank",
            "banktransaction" or "bankstatement" or "bankreconciliation" => "cash-bank",
            "fixedasset" or "toolasset" or "depreciation" or "allocation" => "asset-management",
            "vatransaction" or "taxdeclaration" => "tax-financial",
            _ => string.Empty
        };
    }

    private static string ResolveReportRoute(string reportCode)
    {
        if (reportCode.StartsWith("VAT_", StringComparison.OrdinalIgnoreCase)
            || reportCode.Contains("BALANCE", StringComparison.OrdinalIgnoreCase)
            || reportCode.Contains("INCOME", StringComparison.OrdinalIgnoreCase)
            || reportCode.Contains("TRIAL", StringComparison.OrdinalIgnoreCase)
            || reportCode.Contains("LEDGER", StringComparison.OrdinalIgnoreCase))
        {
            return "tax-financial";
        }

        if (reportCode.Contains("STOCK", StringComparison.OrdinalIgnoreCase)
            || reportCode.Contains("INVENTORY", StringComparison.OrdinalIgnoreCase))
        {
            return "stock-report";
        }

        return "debt-report";
    }

    private static string NormalizeRoute(string route)
    {
        return string.IsNullOrWhiteSpace(route) ? string.Empty : route.Trim().ToLowerInvariant();
    }

    private static string BuildFallbackDescription(NavigationRequest request)
    {
        return $"Không tìm thấy màn hình chuyên biệt cho {request.Target.TargetType}. Dữ liệu liên quan: {request.Target.SourceType ?? request.Target.Route} {request.Target.EntityId}";
    }
}
