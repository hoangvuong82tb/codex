using System.Collections.ObjectModel;
using System.Windows.Media;
using ASMILE.BO.App.Models;
using ASMILE.BO.App.Services;
using ASMILE.BO.App.Views;
using ASMILE.BO.Application.Interfaces;
using ASMILE.BO.Application.FrontOffice.Concierge;
using ASMILE.BO.Application.FrontOffice.Foundation.Contexts;
using ASMILE.BO.Application.FrontOffice.Housekeeping;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;

namespace ASMILE.BO.App.ViewModels;

/// <summary>Enterprise entry point for Front Office. It routes to vertical hubs and owns no workflow logic.</summary>
public sealed partial class FrontOfficeHubViewModel : ViewModelBase, IDisposable
{
    private readonly IReservationHubPermissionReader permissions;
    private readonly IFrontOfficePropertyContextHost propertyContextHost;
    private readonly INavigationService navigationService;
    private readonly ICurrentUserService currentUserService;
    private readonly IServiceProvider? serviceProvider;
    private readonly IWindowManagerService? windowManagerService;
    private ReservationHubViewModel? reservationHub;
    private ReceptionHubViewModel? receptionHub;

    public FrontOfficeHubViewModel(
        IReservationHubPermissionReader permissions,
        IFrontOfficePropertyContextHost propertyContextHost,
        INavigationService navigationService,
        ICurrentUserService currentUserService,
        IServiceProvider? serviceProvider,
        IWindowManagerService? windowManagerService = null)
    {
        this.permissions = permissions;
        this.propertyContextHost = propertyContextHost;
        this.navigationService = navigationService;
        this.currentUserService = currentUserService;
        this.serviceProvider = serviceProvider;
        this.windowManagerService = windowManagerService;
        propertyContextHost.PropertyChanged += OnPropertyContextChanged;
        BuildCards();
    }

    public ObservableCollection<FrontOfficeHubCard> Cards { get; } = new();

    [ObservableProperty] private string filterText = string.Empty;
    [ObservableProperty] private string errorMessage = string.Empty;
    [ObservableProperty] private bool isComingSoonDialogOpen;
    [ObservableProperty] private string comingSoonTitle = string.Empty;
    [ObservableProperty] private object? activeWorkspace;

    public bool HasPropertyContext => propertyContextHost.HasCurrentProperty && permissions.HasContext;
    private bool CanOpenFrontOffice => HasPropertyContext && permissions.CanView;
    private string FrontOfficeDisabledReason => "Yêu cầu quyền xem Front Office và chi nhánh đang hoạt động.";
    public string CurrentPropertyDisplay => propertyContextHost.CurrentProperty?.DisplayName ?? "Chưa chọn chi nhánh";
    public string BusinessDateDisplay => DateTime.Today.ToString("dddd, dd/MM/yyyy", new System.Globalization.CultureInfo("vi-VN"));
    public string CurrentUserDisplay => currentUserService.CurrentUser?.DisplayName ?? "Administrator";
    public string VersionDisplay => "ASMILE-BO 1.0.0";
    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);
    public bool IsMainHubOpen => ActiveWorkspace is null;
    public bool IsReservationHubOpen => ActiveWorkspace is ReservationHubViewModel;
    public bool HasActiveWorkspace => ActiveWorkspace is not null;

    [RelayCommand]
    private void Refresh()
    {
        ErrorMessage = string.Empty;
        BuildCards();
    }

    [RelayCommand]
    private void CloseComingSoon() => IsComingSoonDialogOpen = false;

    [RelayCommand]
    private Task BackAsync()
    {
        if (ActiveWorkspace is not null)
        {
            ActiveWorkspace = null;
            SetWindowTitle("Quản lý khách sạn");
        }

        return Task.CompletedTask;
    }

    private async Task OpenReservationAsync()
    {
        if (!HasPropertyContext || !permissions.CanView)
        {
            ErrorMessage = "Vui lòng chọn chi nhánh Front Office và bảo đảm quyền xem Đặt phòng trước khi tiếp tục.";
            return;
        }

        ErrorMessage = string.Empty;
        if (serviceProvider?.GetService(typeof(ReservationHubViewModel)) is not ReservationHubViewModel workspace)
        {
            var result = await navigationService.OpenModuleAsync("front-office-reservations");
            if (!result.Success)
            {
                ErrorMessage = result.Message ?? "Không thể mở Đặt phòng.";
            }
            return;
        }

        reservationHub ??= workspace;
        reservationHub.ReturnToParentAsync = ReturnToMainHubAsync;
        ActiveWorkspace = reservationHub;
        SetWindowTitle("Đặt phòng");
    }

    private Task OpenReceptionAsync()
    {
        if (!HasPropertyContext || !permissions.CanView)
        {
            ErrorMessage = "Vui lòng chọn chi nhánh Front Office và bảo đảm quyền xem Đặt phòng trước khi tiếp tục.";
            return Task.CompletedTask;
        }

        if (serviceProvider?.GetService(typeof(ReceptionHubViewModel)) is not ReceptionHubViewModel workspace)
        {
            ErrorMessage = "Không thể mở Lễ tân.";
            return Task.CompletedTask;
        }

        ErrorMessage = string.Empty;
        receptionHub ??= workspace;
        receptionHub.ReturnToParentAsync = ReturnToMainHubAsync;
        ActiveWorkspace = receptionHub;
        SetWindowTitle("Lễ tân");
        return Task.CompletedTask;
    }

    private Task ReturnToMainHubAsync()
    {
        ActiveWorkspace = null;
        SetWindowTitle("Front Office");
        return Task.CompletedTask;
    }

    private Task OpenFolioWorkspaceAsync()
    {
        if (!HasPropertyContext)
        {
            ErrorMessage = "Vui lòng chọn chi nhánh Front Office trước khi mở Folio.";
            return Task.CompletedTask;
        }

        OpenCashierWindow<FolioWorkspaceWindow>();
        return Task.CompletedTask;
    }

    private Task OpenCashierHubAsync()
    {
        if (!HasPropertyContext)
        {
            ErrorMessage = "Vui lòng chọn chi nhánh Front Office trước khi mở Thu ngân.";
            return Task.CompletedTask;
        }

        var hub = new ModuleHubViewModel();
        ConfigureHubCards(hub, "Thu ngân");
        hub.ReturnToParentAsync = ReturnToMainHubAsync;
        ActiveWorkspace = hub;
        SetWindowTitle("Thu ngân");
        return Task.CompletedTask;
    }

    private async Task OpenHotelSetupAsync()
    {
        if (!HasPropertyContext || !permissions.CanView)
        {
            ErrorMessage = "Vui lòng chọn chi nhánh Front Office và bảo đảm quyền xem trước khi thiết lập dữ liệu khách sạn.";
            return;
        }

        ErrorMessage = string.Empty;
        var result = await navigationService.OpenModuleAsync("hotel-settings");
        if (!result.Success)
        {
            ErrorMessage = result.Message ?? "Không thể mở trung tâm cấu hình khách sạn.";
        }
    }

    private Task OpenKeyManagementAsync()
    {
        ActiveWorkspace = new KeyManagementWorkspaceView();
        SetWindowTitle("Quản lý chìa khóa");
        return Task.CompletedTask;
    }

    private void OpenFrontOfficeLog()
    {
        // Nhật ký FO là workspace tích hợp của Front Office; các màn hình con
        // được mở qua provider thật và không đi qua cửa sổ menu thu gọn cũ.
        ActiveWorkspace = new FrontOfficeLogWorkspaceView();
        SetWindowTitle("Nhật ký FO");
    }

    private void SetWindowTitle(string title)
    {
        var window = windowManagerService?.OpenWindows.FirstOrDefault(x => ReferenceEquals(x.ContentViewModel, this));
        if (window is not null)
        {
            window.Module.Title = title;
        }
    }

    private void OpenModuleHubOrShowComingSoon(string title)
    {
        var hub = new ModuleHubViewModel();
        ConfigureHubCards(hub, title);
        if (hub.Cards.Count > 0)
        {
            hub.ReturnToParentAsync = ReturnToMainHubAsync;
            ActiveWorkspace = hub;
            SetWindowTitle(title);
        }
        else
        {
            ComingSoonTitle = title;
            IsComingSoonDialogOpen = true;
        }
    }

    private void ShowUnavailable(string title, string description)
    {
        ComingSoonTitle = $"{title}: {description}";
        IsComingSoonDialogOpen = true;
    }

    private void OpenTelephoneView<TView, TViewModel>(string title)
        where TView : System.Windows.Controls.UserControl, new()
        where TViewModel : class
    {
        var tag = $"telephone:{typeof(TViewModel).FullName}";
        var existing = System.Windows.Application.Current.Windows
            .OfType<System.Windows.Window>()
            .FirstOrDefault(window => Equals(window.Tag, tag) && window.IsLoaded);
        if (existing is not null)
        {
            if (existing.WindowState == System.Windows.WindowState.Minimized)
                existing.WindowState = System.Windows.WindowState.Normal;
            existing.Activate();
            existing.Focus();
            return;
        }

        IServiceScope? scope = null;
        TViewModel? viewModel;
        try
        {
            scope = serviceProvider?.CreateScope();
            viewModel = scope?.ServiceProvider.GetService<TViewModel>();
        }
        catch (Exception ex)
        {
            scope?.Dispose();
            ShowUnavailable(title, $"không khởi tạo được workspace: {ex.Message}");
            return;
        }

        if (viewModel is null || scope is null)
        {
            scope?.Dispose();
            ShowUnavailable(title, "chưa được đăng ký trong DI.");
            return;
        }

        var window = new System.Windows.Window
        {
            Title = title,
            Tag = tag,
            Content = new TView { DataContext = viewModel },
            WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen,
            Width = 1220,
            Height = 780,
            MinWidth = 980,
            MinHeight = 620,
            ResizeMode = System.Windows.ResizeMode.CanResize,
            ShowInTaskbar = false
        };
        window.Closed += (_, _) => scope.Dispose();
        SetVisibleOwner(window);
        try
        {
            window.Show();
        }
        catch
        {
            scope.Dispose();
            throw;
        }
    }

    private void ConfigureHubCards(ModuleHubViewModel hub, string hubId)
    {
        switch (hubId)
        {
            case "Thu ngân":
                hub.Title = "Thu ngân"; hub.Subtitle = "Thu tiền Folio, giao dịch và quản lý tài chính khách";
                hub.Cards.Add(CreateCard("payment", "Thu tiền Folio", "", "#4A8EFF", "#1765F2", () => OpenCashierWindow<CashierPaymentWindow>()));
                hub.Cards.Add(CreateCard("deposit", "Tiền đặt cọc", "", "#FFAD39", "#ED7900", () => OpenCashierWindow<DepositManagementWindow>()));
                hub.Cards.Add(CreateCard("refund", "Hoàn tiền", "", "#B158FF", "#7D2BE1", () => OpenCashierWindow<RefundManagementWindow>()));
                hub.Cards.Add(CreateCard("invoice", "Hóa đơn", "", "#4ED86E", "#18B33F", () => OpenCashierWindow<InvoiceManagementWindow>()));
                // Keep the approved full-shell layouts.  Their parameterless
                // constructors now hydrate the shell from the real cashier VMs.
                hub.Cards.Add(CreateCard("receipt", "Phiếu thu", "", "#4A91FF", "#1E6CF1", () => OpenCashierWindow(() => new ReceiptVoucherWindow())));
                hub.Cards.Add(CreateCard("voucher", "Phiếu chi", "", "#2CC7D8", "#00A7BA", () => OpenCashierWindow(() => new CashierVoucherWindow())));
                hub.Cards.Add(CreateCard("transactions", "Giao dịch", "", "#3E83F3", "#195FCE", () => OpenCashierWindow<TransactionCenterWindow>()));
                hub.Cards.Add(CreateCard("folio", "Folio", "", "#9853F0", "#6F28D8", () => OpenCashierWindow<FolioWorkspaceWindow>()));
                hub.Cards.Add(CreateCard("split-folio", "Tách Folio", "", "#4ED86E", "#18B33F", () => OpenCashierWindow<SplitFolioWindow>()));
                hub.Cards.Add(CreateCard("merge-folio", "Gộp Folio", "", "#4A91FF", "#1E6CF1", () => OpenCashierWindow<MergeFolioWindow>()));
                break;
            case "Thiết lập khách sạn":
                hub.Title = "Cấu hình khách sạn";
                hub.Subtitle = "Mở trung tâm cấu hình tập trung cho dữ liệu nền và quy tắc vận hành";
                hub.Cards.Add(CreateCard(
                    "hotel-settings",
                    "Mở trung tâm cấu hình",
                    "",
                    "#4A8EFF",
                    "#1765F2",
                    () => _ = OpenHotelSetupAsync()));
                break;
            case "Đoàn":
                hub.Title = "Đoàn"; hub.Subtitle = "Quản lý đặt phòng đoàn và hồ sơ khách đoàn";
                hub.Cards.Add(CreateCard("group-create", "Tạo đoàn", "", "#4A8EFF", "#1765F2", OpenCreateGroupWindow));
                hub.Cards.Add(CreateCard("group-list", "Danh sách đoàn", "", "#31C9B8", "#00A58F", OpenGroupReservationWindow));
                hub.Cards.Add(CreateCard("group-booking", "Đặt phòng đoàn", "", "#FFAD39", "#ED7900", OpenGroupBookingWizardWindow));
                hub.Cards.Add(CreateCard("group-assign", "Phân phòng đoàn", "", "#B158FF", "#7D2BE1", OpenGroupRoomAssignmentWindow));
                hub.Cards.Add(CreateCard("rooming-list", "Rooming List", "", "#4ED86E", "#18B33F", OpenGroupRoomingListWindow));
                hub.Cards.Add(CreateCard("group-checkin", "Check-in đoàn", "", "#4A91FF", "#1E6CF1", OpenGroupCheckInWindow));
                hub.Cards.Add(CreateCard("group-checkout", "Check-out đoàn", "", "#2CC7D8", "#00A7BA", OpenGroupCheckoutWindow));
                hub.Cards.Add(CreateCard("group-guests", "Khách trong đoàn", "", "#3E83F3", "#195FCE", OpenGroupGuestManagementWindow));
                hub.Cards.Add(CreateCard("group-services", "Dịch vụ đoàn", "", "#FF5A94", "#E32F70", OpenGroupServiceWindow));
                break;
            case "Nhà buồng":
                hub.Title = "Nhà buồng"; hub.Subtitle = "Theo dõi tình trạng phòng và nghiệp vụ buồng phòng";
                hub.Cards.Add(CreateCard("hk-status", "Tình trạng buồng", "", "#4A8EFF", "#1765F2", OpenHousekeepingRoomStatusWindow));
                hub.Cards.Add(CreateCard("hk-list", "Tra cứu phòng", "", "#31C9B8", "#00A58F", OpenRoomMasterWindow));
                hub.Cards.Add(CreateCard("hk-clean", "Phòng sạch", "", "#4ED86E", "#18B33F", OpenCleanRoomWindow));
                hub.Cards.Add(CreateCard("hk-dirty", "Phòng bẩn", "", "#FFAD39", "#ED7900", OpenDirtyRoomWindow));
                hub.Cards.Add(CreateCard("hk-cleaning", "Đang dọn", "", "#B158FF", "#7D2BE1", OpenCleaningInProgressWindow));
                hub.Cards.Add(CreateCard("hk-inspect", "Kiểm tra phòng", "", "#4A91FF", "#1E6CF1", OpenRoomInspectionWindow));
                hub.Cards.Add(CreateCard("hk-lost-found", "Lost & Found", "", "#2CC7D8", "#00A7BA", OpenLostFoundWindow));
                hub.Cards.Add(CreateCard("hk-minibar", "Minibar", "", "#3E83F3", "#195FCE", OpenMinibarWindow));
                hub.Cards.Add(CreateCard("hk-supplies", "Đồ vải & tiện nghi", "", "#0EA5A8", "#087F83", OpenHousekeepingSupplyWindow));
                hub.Cards.Add(CreateCard("hk-assign", "Nhân công", "", "#7C3AED", "#6D28D9", OpenWorkforceWindow));
                hub.Cards.Add(CreateCard("hk-task-shift", "Công việc & ca", "", "#2563EB", "#1D4ED8", OpenTaskShiftWindow));
                break;
            case "Tổng đài":
                hub.Title = "Tổng đài"; hub.Subtitle = "Quản lý cuộc gọi, lời nhắn và yêu cầu của khách";
                hub.Cards.Add(CreateCard("tel-incoming", "Cuộc gọi đến", "", "#4A8EFF", "#1765F2", () => OpenTelephoneCenter(TelephoneWorkspaceMode.Incoming, "Cuộc gọi đến")));
                hub.Cards.Add(CreateCard("tel-outgoing", "Cuộc gọi đi", "", "#31C9B8", "#00A58F", () => OpenTelephoneCenter(TelephoneWorkspaceMode.Outgoing, "Cuộc gọi đi")));
                hub.Cards.Add(CreateCard("tel-log", "Nhật ký cuộc gọi", "", "#FFAD39", "#ED7900", () => OpenTelephoneCenter(TelephoneWorkspaceMode.Log, "Nhật ký cuộc gọi")));
                hub.Cards.Add(CreateCard("tel-wakeup", "Đánh thức", "", "#B158FF", "#7D2BE1", () => OpenTelephoneView<WakeUpCallView, WakeUpCallViewModel>("Đánh thức")));
                hub.Cards.Add(CreateCard("tel-messages", "Lời nhắn", "", "#4ED86E", "#18B33F", () => OpenTelephoneView<MessageCenterView, MessageCenterViewModel>("Lời nhắn")));
                hub.Cards.Add(CreateCard("tel-contacts", "Danh bạ", "", "#2CC7D8", "#00A7BA", () => OpenTelephoneCenter(TelephoneWorkspaceMode.Contacts, "Danh bạ")));
                hub.Cards.Add(CreateCard("tel-requests", "Yêu cầu khách", "", "#3E83F3", "#195FCE", () => OpenTelephoneView<ServiceRequestView, ServiceRequestViewModel>("Yêu cầu khách")));
                hub.Cards.Add(CreateCard("tel-concierge", "Concierge", "", "#7C3AED", "#6D28D9", () => OpenTelephoneView<ConciergeView, ConciergeViewModel>("Concierge")));
                break;
            case "Kế toán phải thu":
                hub.Title = "Kế toán phải thu"; hub.Subtitle = "Theo dõi công nợ và tài khoản phải thu";
                hub.Cards.Add(CreateCard("ar-debt", "Công nợ", "", "#4A8EFF", "#1765F2", OpenArLedgerWindow));
                hub.Cards.Add(CreateCard("ar-customers", "Khách hàng công nợ", "", "#31C9B8", "#00A58F", OpenArCustomerWindow));
                hub.Cards.Add(CreateCard("ar-companies", "Doanh nghiệp", "", "#FFAD39", "#ED7900", OpenArCompanyWindow));
                hub.Cards.Add(CreateCard("ar-invoices", "Hóa đơn", "", "#B158FF", "#7D2BE1", OpenArInvoiceWindow));
                hub.Cards.Add(CreateCard("ar-reconcile", "Đối chiếu", "", "#4ED86E", "#18B33F", OpenArReconcileWindow));
                hub.Cards.Add(CreateCard("ar-payment", "Thu công nợ", "", "#4A91FF", "#1E6CF1", OpenArPaymentWindow));
                hub.Cards.Add(CreateCard("ar-aging", "Tuổi nợ", "", "#2CC7D8", "#00A7BA", OpenArAgingWindow));
                break;
            case "Tìm kiếm toàn hệ thống":
                hub.Title = "Tìm kiếm toàn hệ thống"; hub.Subtitle = "Tra cứu khách, phòng, đặt phòng, lưu trú và giao dịch";
                hub.Cards.Add(CreateCard("inq-guest", "Tra cứu khách", "", "#4A8EFF", "#1765F2", OpenGuestInquiryWindow));
                hub.Cards.Add(CreateCard("inq-booking", "Tra cứu đặt phòng", "", "#31C9B8", "#00A58F", OpenReservationSearchWindow));
                hub.Cards.Add(CreateCard("inq-room", "Tra cứu phòng", "", "#FFAD39", "#ED7900", OpenRoomMasterWindow));
                // Reuse the Reception stay-profile workspace so lookup, filters, detail tabs, and pager stay consistent.
                hub.Cards.Add(CreateCard("inq-stay", "Tra cứu lưu trú", "", "#B158FF", "#7D2BE1", OpenStayProfileWindow));
                hub.Cards.Add(CreateCard("inq-transactions", "Tra cứu giao dịch", "", "#3E83F3", "#195FCE", () => OpenCashierWindow<TransactionCenterWindow>()));
                hub.Cards.Add(CreateCard("inq-log", "Nhật ký FO", "", "#2661D9", "#1046B7", OpenFrontOfficeLog));
                break;
            case "Marketing":
                hub.Title = "Marketing"; hub.Subtitle = "Quản lý nguồn khách, thị trường và chương trình tiếp thị";
                hub.Cards.Add(CreateCard("mkt-source", "Nguồn khách", "", "#4A8EFF", "#1765F2", () => OpenMarketingCenter("source")));
                hub.Cards.Add(CreateCard("mkt-segment", "Phân khúc", "", "#31C9B8", "#00A58F", () => OpenMarketingCenter("segment")));
                hub.Cards.Add(CreateCard("mkt-agency", "Đại lý", "", "#B158FF", "#7D2BE1", () => OpenMarketingCenter("agency")));
                hub.Cards.Add(CreateCard("mkt-members", "Thành viên", "", "#4A91FF", "#1E6CF1", () => OpenMarketingCenter("member")));
                hub.Cards.Add(CreateCard("mkt-promo", "Khuyến mại", "", "#FF5A94", "#E32F70", () => OpenMarketingCenter("promotion")));
                break;
            case "Báo cáo":
                hub.Title = "Báo cáo"; hub.Subtitle = "Báo cáo vận hành, công suất phòng và doanh thu từ dữ liệu thật";
                hub.Cards.Add(CreateCard("rpt-operations", "Vận hành & công suất", "", "#4A8EFF", "#1765F2", () => OpenReportCenter("Operations", "Báo cáo")));
                hub.Cards.Add(CreateCard("rpt-finance", "Doanh thu & thu ngân", "", "#7C3AED", "#6D28D9", () => OpenReportCenter("Finance", "Báo cáo")));
                hub.Cards.Add(CreateCard("rpt-folio-gaps", "Kiểm soát Folio", "", "#2CC7D8", "#00A7BA", () => OpenReportCenter("FolioGaps", "Báo cáo")));
                break;
            case "Night Audit":
                hub.Title = "Night Audit"; hub.Subtitle = "Kiểm tra và đóng ngày kinh doanh khách sạn";
                hub.Cards.Add(CreateCard("na-check", "Kiểm tra", "", "#4A8EFF", "#1765F2", () => OpenNightAuditCenter("check")));
                hub.Cards.Add(CreateCard("na-run", "Chạy Night Audit", "", "#4ED86E", "#18B33F", () => OpenNightAuditCenter("run")));
                hub.Cards.Add(CreateCard("na-close", "Đóng ngày", "", "#4A91FF", "#1E6CF1", () => OpenNightAuditCenter("close")));
                break;
        }
    }

    private static ModuleHubCard CreateCard(string id, string title, string glyph, string c1, string c2, Action? action = null) => new()
    {
        Id = id, Title = title, IconGlyph = glyph,
        IconColor = new LinearGradientBrush((Color)ColorConverter.ConvertFromString(c1), (Color)ColorConverter.ConvertFromString(c2), 45),
        Command = action is not null ? new RelayCommand(action) : new RelayCommand(() => { })
    };

    private static void OpenGroupBookingWizardWindow()
    {
        // The former GroupBookingWizardWindow is a legacy in-memory prototype:
        // it can display and mutate room/rate/deposit state without persisting
        // anything. Route the production card through the persisted group
        // creation wizard instead, so operators never enter a mock workflow.
        var existing = System.Windows.Application.Current.Windows
            .OfType<CreateGroupWindow>()
            .FirstOrDefault(window => window.IsLoaded);
        if (existing is not null)
        {
            if (existing.WindowState == System.Windows.WindowState.Minimized)
                existing.WindowState = System.Windows.WindowState.Normal;
            existing.Activate();
            existing.Focus();
            return;
        }

        new CreateGroupWindow
        {
            Owner = GetVisibleOwner(),
            WindowStartupLocation = System.Windows.WindowStartupLocation.CenterOwner
        }.Show();
    }

    private static void OpenGroupRoomAssignmentWindow()
    {
        var existing = System.Windows.Application.Current.Windows
            .OfType<GroupRoomAssignmentWindow>()
            .FirstOrDefault(window => window.IsLoaded);
        if (existing is not null)
        {
            if (existing.WindowState == System.Windows.WindowState.Minimized)
                existing.WindowState = System.Windows.WindowState.Normal;
            existing.Activate();
            existing.Focus();
            return;
        }

        new GroupRoomAssignmentWindow
        {
            Owner = GetVisibleOwner(),
            WindowStartupLocation = System.Windows.WindowStartupLocation.CenterOwner
        }.Show();
    }

    private static void OpenGroupCheckInWindow()
    {
        var existing = System.Windows.Application.Current.Windows
            .OfType<GroupCheckInWindow>()
            .FirstOrDefault(window => window.IsLoaded);
        if (existing is not null)
        {
            if (existing.WindowState == System.Windows.WindowState.Minimized)
                existing.WindowState = System.Windows.WindowState.Normal;
            existing.Activate();
            existing.Focus();
            return;
        }

        var window = new GroupCheckInWindow();
        SetVisibleOwner(window);
        window.Show();
    }

    private static void OpenGroupCheckoutWindow()
    {
        var existing = System.Windows.Application.Current.Windows
            .OfType<GroupCheckoutWindow>()
            .FirstOrDefault(window => window.IsLoaded);
        if (existing is not null)
        {
            if (existing.WindowState == System.Windows.WindowState.Minimized)
                existing.WindowState = System.Windows.WindowState.Normal;
            existing.Activate();
            existing.Focus();
            return;
        }

        var window = new GroupCheckoutWindow();
        SetVisibleOwner(window);
        window.Show();
    }

    private static void OpenCreateGroupWindow()
    {
        var existing = System.Windows.Application.Current.Windows
            .OfType<CreateGroupWindow>()
            .FirstOrDefault(window => window.IsLoaded);
        if (existing is not null)
        {
            if (existing.WindowState == System.Windows.WindowState.Minimized)
                existing.WindowState = System.Windows.WindowState.Normal;
            existing.Activate();
            existing.Focus();
            return;
        }

        new CreateGroupWindow
        {
            Owner = GetVisibleOwner(),
            WindowStartupLocation = System.Windows.WindowStartupLocation.CenterOwner
        }.Show();
    }

    private static void OpenGroupGuestManagementWindow()
    {
        var existing = System.Windows.Application.Current.Windows
            .OfType<GroupGuestManagementWindow>()
            .FirstOrDefault(window => window.IsLoaded);
        if (existing is not null)
        {
            if (existing.WindowState == System.Windows.WindowState.Minimized)
                existing.WindowState = System.Windows.WindowState.Normal;
            existing.Activate();
            existing.Focus();
            return;
        }

        new GroupGuestManagementWindow
        {
            Owner = GetVisibleOwner(),
            WindowStartupLocation = System.Windows.WindowStartupLocation.CenterOwner
        }.Show();
    }

    private static void OpenGroupRoomingListWindow()
    {
        var existing = System.Windows.Application.Current.Windows
            .OfType<GroupRoomingListWindow>()
            .FirstOrDefault(window => window.IsLoaded);
        if (existing is not null)
        {
            if (existing.WindowState == System.Windows.WindowState.Minimized)
                existing.WindowState = System.Windows.WindowState.Normal;
            existing.Activate();
            existing.Focus();
            return;
        }

        new GroupRoomingListWindow
        {
            Owner = GetVisibleOwner(),
            WindowStartupLocation = System.Windows.WindowStartupLocation.CenterOwner
        }.Show();
    }

    private static void OpenGroupServiceWindow()
    {
        var existing = System.Windows.Application.Current.Windows
            .OfType<GroupServiceWindow>()
            .FirstOrDefault(window => window.IsLoaded);
        if (existing is not null)
        {
            if (existing.WindowState == System.Windows.WindowState.Minimized)
                existing.WindowState = System.Windows.WindowState.Normal;
            existing.Activate();
            existing.Focus();
            return;
        }

        new GroupServiceWindow
        {
            Owner = GetVisibleOwner(),
            WindowStartupLocation = System.Windows.WindowStartupLocation.CenterOwner
        }.Show();
    }

    private static void OpenHousekeepingRoomStatusWindow()
    {
        var existing = System.Windows.Application.Current.Windows
            .OfType<HousekeepingRoomStatusWindow>()
            .FirstOrDefault(window => window.IsLoaded);
        if (existing is not null)
        {
            if (existing.WindowState == System.Windows.WindowState.Minimized)
                existing.WindowState = System.Windows.WindowState.Normal;
            existing.Activate();
            existing.Focus();
            return;
        }

        new HousekeepingRoomStatusWindow
        {
            Owner = GetVisibleOwner(),
            WindowStartupLocation = System.Windows.WindowStartupLocation.CenterOwner
        }.Show();
    }

    private static void OpenHousekeepingSupplyWindow()
    {
        OpenCashierWindow<HousekeepingSupplyWindow>();
    }

    private static void OpenRoomMasterWindow()
    {
        var existing = System.Windows.Application.Current.Windows
            .OfType<RoomMasterWindow>()
            .FirstOrDefault(window => window.IsLoaded);
        if (existing is not null)
        {
            if (existing.WindowState == System.Windows.WindowState.Minimized)
                existing.WindowState = System.Windows.WindowState.Normal;
            existing.Activate();
            existing.Focus();
            return;
        }

        new RoomMasterWindow
        {
            Owner = GetVisibleOwner(),
            WindowStartupLocation = System.Windows.WindowStartupLocation.CenterOwner
        }.Show();
    }

    private void OpenRateEngineWindow()
        => OpenTelephoneView<RateEngineView, RateEngineViewModel>("Khung giá & kiểm tra giá");

    private void OpenItemMasterWindow()
    {
        const string tag = "hotel-setup:minibar-items";
        var existing = System.Windows.Application.Current.Windows
            .OfType<System.Windows.Window>()
            .FirstOrDefault(window => Equals(window.Tag, tag) && window.IsLoaded);
        if (existing is not null)
        {
            if (existing.WindowState == System.Windows.WindowState.Minimized)
                existing.WindowState = System.Windows.WindowState.Normal;
            existing.Activate();
            existing.Focus();
            return;
        }

        IServiceScope? scope = null;
        ItemsViewModel? viewModel;
        try
        {
            scope = serviceProvider?.CreateScope();
            viewModel = scope?.ServiceProvider.GetService<ItemsViewModel>();
        }
        catch (Exception ex)
        {
            scope?.Dispose();
            ShowUnavailable("Danh mục hàng minibar", $"không khởi tạo được workspace: {ex.Message}");
            return;
        }

        if (viewModel is null || scope is null)
        {
            scope?.Dispose();
            ShowUnavailable("Danh mục hàng minibar", "chưa được đăng ký trong DI.");
            return;
        }

        var window = new System.Windows.Window
        {
            Title = "Danh mục hàng minibar",
            Tag = tag,
            Content = new ItemsView { DataContext = viewModel },
            WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen,
            Width = 1220,
            Height = 780,
            MinWidth = 980,
            MinHeight = 620,
            ResizeMode = System.Windows.ResizeMode.CanResize,
            ShowInTaskbar = false
        };
        window.Closed += (_, _) => scope.Dispose();
        SetVisibleOwner(window);
        try
        {
            window.Show();
        }
        catch
        {
            scope.Dispose();
            throw;
        }
    }

    /// <summary>
    /// Opens the existing reservation-search workspace from the inquiry hub.
    /// This keeps inquiry and reservation operations on the same query, detail,
    /// and common pager implementation instead of maintaining a duplicate screen.
    /// </summary>
    private void OpenReservationSearchWindow()
    {
        var existing = System.Windows.Application.Current.Windows
            .OfType<SearchReservationWindow>()
            .FirstOrDefault(window => window.IsLoaded);
        if (existing is not null)
        {
            existing.EditAction ??= reservation => OpenReservationEditorAsync(existing.Workspace, existing, reservation);
            if (existing.WindowState == System.Windows.WindowState.Minimized)
                existing.WindowState = System.Windows.WindowState.Normal;
            existing.Activate();
            existing.Focus();
            return;
        }

        IServiceScope? scope = null;
        try
        {
            scope = serviceProvider?.CreateScope();
            var workspace = scope?.ServiceProvider.GetService<ReservationWorkspaceViewModel>();
            if (workspace is null || scope is null)
            {
                scope?.Dispose();
                ErrorMessage = "Không thể khởi tạo màn hình tra cứu đặt phòng.";
                return;
            }

            ErrorMessage = string.Empty;
            var window = new SearchReservationWindow(workspace)
            {
                Owner = GetVisibleOwner(),
                WindowStartupLocation = System.Windows.WindowStartupLocation.CenterOwner
            };
            window.EditAction = reservation => OpenReservationEditorAsync(workspace, window, reservation);
            window.Closed += (_, _) => scope.Dispose();
            window.Show();
        }
        catch (Exception ex)
        {
            scope?.Dispose();
            ErrorMessage = $"Không thể mở tra cứu đặt phòng: {ex.Message}";
        }
    }

    private static async Task OpenReservationEditorAsync(
        ReservationWorkspaceViewModel workspace,
        SearchReservationWindow owner,
        ASMILE.BO.Application.FrontOffice.Reservation.ReservationDto reservation)
    {
        try
        {
            if (!await workspace.OpenForEditAsync(reservation.ReservationId))
            {
                throw new InvalidOperationException(string.IsNullOrWhiteSpace(workspace.ErrorMessage)
                    ? "Không tải được đặt phòng đã chọn để sửa."
                    : workspace.ErrorMessage);
            }

            CreateReservationPopup? popup = null;
            var modal = new ReservationModalWorkspaceViewModel(workspace, ReservationModalKind.Edit, () => popup?.Close());
            popup = new CreateReservationPopup(modal)
            {
                Owner = owner,
                WindowStartupLocation = System.Windows.WindowStartupLocation.CenterOwner
            };
            popup.Wizard.SetEditMode(reservation.ReservationId, reservation.ReservationNumber);
            popup.ShowDialog();

            await workspace.RefreshWithStatusAsync(workspace.SelectedStatus, reservation.ReservationId);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Không thể mở màn hình sửa đặt phòng: {ex.Message}", ex);
        }
    }

    private static void OpenCleanRoomWindow()
    {
        var existing = System.Windows.Application.Current.Windows
            .OfType<CleanRoomWindow>()
            .FirstOrDefault(window => window.IsLoaded);
        if (existing is not null)
        {
            if (existing.WindowState == System.Windows.WindowState.Minimized)
                existing.WindowState = System.Windows.WindowState.Normal;
            existing.Activate();
            existing.Focus();
            return;
        }

        new CleanRoomWindow
        {
            Owner = GetVisibleOwner(),
            WindowStartupLocation = System.Windows.WindowStartupLocation.CenterOwner
        }.Show();
    }

    private static void OpenDirtyRoomWindow()
    {
        var existing = System.Windows.Application.Current.Windows
            .OfType<DirtyRoomWindow>()
            .FirstOrDefault(window => window.IsLoaded);
        if (existing is not null)
        {
            if (existing.WindowState == System.Windows.WindowState.Minimized)
                existing.WindowState = System.Windows.WindowState.Normal;
            existing.Activate();
            existing.Focus();
            return;
        }

        new DirtyRoomWindow
        {
            Owner = GetVisibleOwner(),
            WindowStartupLocation = System.Windows.WindowStartupLocation.CenterOwner
        }.Show();
    }

    private static void OpenCleaningInProgressWindow()
    {
        var existing = System.Windows.Application.Current.Windows
            .OfType<CleaningInProgressWindow>()
            .FirstOrDefault(window => window.IsLoaded);
        if (existing is not null)
        {
            if (existing.WindowState == System.Windows.WindowState.Minimized)
                existing.WindowState = System.Windows.WindowState.Normal;
            existing.Activate();
            existing.Focus();
            return;
        }

        new CleaningInProgressWindow
        {
            Owner = GetVisibleOwner(),
            WindowStartupLocation = System.Windows.WindowStartupLocation.CenterOwner
        }.Show();
    }

    private static void OpenRoomInspectionWindow()
    {
        var existing = System.Windows.Application.Current.Windows
            .OfType<RoomInspectionWindow>()
            .FirstOrDefault(window => window.IsLoaded);
        if (existing is not null)
        {
            if (existing.WindowState == System.Windows.WindowState.Minimized)
                existing.WindowState = System.Windows.WindowState.Normal;
            existing.Activate();
            existing.Focus();
            return;
        }

        new RoomInspectionWindow
        {
            Owner = GetVisibleOwner(),
            WindowStartupLocation = System.Windows.WindowStartupLocation.CenterOwner
        }.Show();
    }

    private static void OpenLostFoundWindow()
    {
        var existing = System.Windows.Application.Current.Windows
            .OfType<LostFoundWindow>()
            .FirstOrDefault(window => window.IsLoaded);
        if (existing is not null)
        {
            if (existing.WindowState == System.Windows.WindowState.Minimized)
                existing.WindowState = System.Windows.WindowState.Normal;
            existing.Activate();
            existing.Focus();
            return;
        }

        new LostFoundWindow
        {
            Owner = GetVisibleOwner(),
            WindowStartupLocation = System.Windows.WindowStartupLocation.CenterOwner
        }.Show();
    }

    private static void OpenMinibarWindow()
    {
        var existing = System.Windows.Application.Current.Windows
            .OfType<MinibarWindow>()
            .FirstOrDefault(window => window.IsLoaded);
        if (existing is not null)
        {
            if (existing.WindowState == System.Windows.WindowState.Minimized)
                existing.WindowState = System.Windows.WindowState.Normal;
            existing.Activate();
            existing.Focus();
            return;
        }

        new MinibarWindow
        {
            Owner = GetVisibleOwner(),
            WindowStartupLocation = System.Windows.WindowStartupLocation.CenterOwner
        }.Show();
    }

    private static void OpenArLedgerWindow()
    {
        var existing = System.Windows.Application.Current.Windows
            .OfType<ArLedgerWindow>()
            .FirstOrDefault(window => window.IsLoaded);
        if (existing is not null)
        {
            if (existing.WindowState == System.Windows.WindowState.Minimized)
                existing.WindowState = System.Windows.WindowState.Normal;
            existing.Activate();
            existing.Focus();
            return;
        }

        new ArLedgerWindow
        {
            Owner = GetVisibleOwner(),
            WindowStartupLocation = System.Windows.WindowStartupLocation.CenterOwner
        }.Show();
    }

    private static void OpenArCustomerWindow()
    {
        var existing = System.Windows.Application.Current.Windows
            .OfType<ArCustomerWindow>()
            .FirstOrDefault(window => window.IsLoaded);
        if (existing is not null)
        {
            if (existing.WindowState == System.Windows.WindowState.Minimized)
                existing.WindowState = System.Windows.WindowState.Normal;
            existing.Activate();
            existing.Focus();
            return;
        }

        new ArCustomerWindow
        {
            Owner = GetVisibleOwner(),
            WindowStartupLocation = System.Windows.WindowStartupLocation.CenterOwner
        }.Show();
    }

    private static void OpenArCompanyWindow()
    {
        var existing = System.Windows.Application.Current.Windows
            .OfType<ArCompanyWindow>()
            .FirstOrDefault(window => window.IsLoaded);
        if (existing is not null)
        {
            if (existing.WindowState == System.Windows.WindowState.Minimized)
                existing.WindowState = System.Windows.WindowState.Normal;
            existing.Activate();
            existing.Focus();
            return;
        }

        new ArCompanyWindow
        {
            Owner = GetVisibleOwner(),
            WindowStartupLocation = System.Windows.WindowStartupLocation.CenterOwner
        }.Show();
    }

    private static void OpenArInvoiceWindow()
    {
        var existing = System.Windows.Application.Current.Windows
            .OfType<ArInvoiceWindow>()
            .FirstOrDefault(window => window.IsLoaded);
        if (existing is not null)
        {
            if (existing.WindowState == System.Windows.WindowState.Minimized)
                existing.WindowState = System.Windows.WindowState.Normal;
            existing.Activate();
            existing.Focus();
            return;
        }

        new ArInvoiceWindow
        {
            Owner = GetVisibleOwner(),
            WindowStartupLocation = System.Windows.WindowStartupLocation.CenterOwner
        }.Show();
    }

    private static void OpenArReconcileWindow()
    {
        var existing = System.Windows.Application.Current.Windows
            .OfType<ArReconcileWindow>()
            .FirstOrDefault(window => window.IsLoaded);
        if (existing is not null)
        {
            if (existing.WindowState == System.Windows.WindowState.Minimized)
                existing.WindowState = System.Windows.WindowState.Normal;
            existing.Activate();
            existing.Focus();
            return;
        }

        new ArReconcileWindow
        {
            Owner = GetVisibleOwner(),
            WindowStartupLocation = System.Windows.WindowStartupLocation.CenterOwner
        }.Show();
    }

    private static void OpenArPaymentWindow()
    {
        var existing = System.Windows.Application.Current.Windows
            .OfType<ArPaymentWindow>()
            .FirstOrDefault(window => window.IsLoaded);
        if (existing is not null)
        {
            if (existing.WindowState == System.Windows.WindowState.Minimized)
                existing.WindowState = System.Windows.WindowState.Normal;
            existing.Activate();
            existing.Focus();
            return;
        }

        new ArPaymentWindow
        {
            Owner = GetVisibleOwner(),
            WindowStartupLocation = System.Windows.WindowStartupLocation.CenterOwner
        }.Show();
    }

    private static void OpenArAgingWindow()
    {
        var existing = System.Windows.Application.Current.Windows
            .OfType<ArAgingWindow>()
            .FirstOrDefault(window => window.IsLoaded);
        if (existing is not null)
        {
            if (existing.WindowState == System.Windows.WindowState.Minimized)
                existing.WindowState = System.Windows.WindowState.Normal;
            existing.Activate();
            existing.Focus();
            return;
        }

        new ArAgingWindow
        {
            Owner = GetVisibleOwner(),
            WindowStartupLocation = System.Windows.WindowStartupLocation.CenterOwner
        }.Show();
    }

    private static void OpenGuestInquiryWindow()
    {
        // Guest 360 is the shared inquiry workspace; re-activate it when it is already open.
        OpenCashierWindow<GuestInquiryWindow>();
    }

    private static void OpenStayProfileWindow()
    {
        var existing = System.Windows.Application.Current.Windows
            .OfType<StayProfileWindow>()
            .FirstOrDefault(window => window.IsLoaded);
        if (existing is not null)
        {
            if (existing.WindowState == System.Windows.WindowState.Minimized)
                existing.WindowState = System.Windows.WindowState.Normal;
            existing.Activate();
            existing.Focus();
            return;
        }

        new StayProfileWindow
        {
            Owner = GetVisibleOwner(),
            WindowStartupLocation = System.Windows.WindowStartupLocation.CenterOwner
        }.Show();
    }

    private void OpenWorkforceWindow()
    {
        var existing = System.Windows.Application.Current.Windows
            .OfType<WorkforceWindow>()
            .FirstOrDefault(window => window.IsLoaded);
        if (existing is not null)
        {
            if (existing.WindowState == System.Windows.WindowState.Minimized)
                existing.WindowState = System.Windows.WindowState.Normal;
            existing.Activate();
            existing.Focus();
            return;
        }

        var service = (System.Windows.Application.Current as App)?.Services?.GetService(typeof(IWorkforceQueryService)) as IWorkforceQueryService;
        var commandService = (System.Windows.Application.Current as App)?.Services?.GetService(typeof(IWorkforceCommandService)) as IWorkforceCommandService;
        var services = (System.Windows.Application.Current as App)?.Services;
        var housekeepingBoard = services?.GetService<IHousekeepingBoardService>();
        var housekeepingWorkflow = services?.GetService<IHousekeepingWorkflowService>();
        var executionContext = services?.GetService<IFrontOfficeExecutionContextAccessor>();
        if (service is null || commandService is null || housekeepingBoard is null || housekeepingWorkflow is null || executionContext is null)
        {
            ErrorMessage = "Không thể mở Nhân sự & ca vì các dịch vụ vận hành chưa được đăng ký.";
            return;
        }

        var window = new WorkforceWindow(service, commandService, housekeepingBoard, housekeepingWorkflow, executionContext);
        SetVisibleOwner(window);
        window.Show();
    }

    private static void OpenTaskShiftWindow()
    {
        var existing = System.Windows.Application.Current.Windows
            .OfType<TaskShiftWindow>()
            .FirstOrDefault(window => window.IsLoaded);
        if (existing is not null)
        {
            if (existing.WindowState == System.Windows.WindowState.Minimized)
                existing.WindowState = System.Windows.WindowState.Normal;
            existing.Activate();
            existing.Focus();
            return;
        }

        OpenCashierWindow<TaskShiftWindow>();
    }

    private static void OpenGroupReservationWindow()
    {
        var existing = System.Windows.Application.Current.Windows
            .OfType<GroupReservationWindow>()
            .FirstOrDefault(window => window.IsLoaded);
        if (existing is not null)
        {
            if (existing.WindowState == System.Windows.WindowState.Minimized)
                existing.WindowState = System.Windows.WindowState.Normal;
            existing.Activate();
            existing.Focus();
            return;
        }

        var window = new GroupReservationWindow();
        SetVisibleOwner(window);
        window.Show();
    }

    private static void OpenCashierWindow<T>() where T : System.Windows.Window, new()
    {
        var existing = System.Windows.Application.Current.Windows
            .OfType<T>()
            .FirstOrDefault(window => window.IsLoaded);
        if (existing is not null)
        {
            if (existing.WindowState == System.Windows.WindowState.Minimized)
                existing.WindowState = System.Windows.WindowState.Normal;
            existing.Activate();
            existing.Focus();
            return;
        }

        var window = new T
        {
            WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen,
            ResizeMode = System.Windows.ResizeMode.CanResize,
            ShowInTaskbar = false
        };
        SetVisibleOwner(window);
        window.Show();
    }

    private static void OpenCashierWindow<T>(Func<T> factory) where T : System.Windows.Window
    {
        var existing = System.Windows.Application.Current.Windows
            .OfType<T>()
            .FirstOrDefault(window => window.IsLoaded);
        if (existing is not null)
        {
            if (existing.WindowState == System.Windows.WindowState.Minimized)
                existing.WindowState = System.Windows.WindowState.Normal;
            existing.Activate();
            existing.Focus();
            return;
        }

        var window = factory();
        window.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen;
        SetVisibleOwner(window);
        window.ResizeMode = System.Windows.ResizeMode.CanResize;
        window.ShowInTaskbar = false;
        window.Show();
    }

    private static void SetVisibleOwner(System.Windows.Window window)
    {
        var owner = System.Windows.Application.Current?.MainWindow;
        if (owner is { IsVisible: true } && !ReferenceEquals(owner, window))
        {
            window.Owner = owner;
            window.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterOwner;
        }
    }

    private static System.Windows.Window? GetVisibleOwner()
        => System.Windows.Application.Current?.MainWindow is { IsVisible: true } owner
            ? owner
            : null;

    private void OpenTelephoneCenter(TelephoneWorkspaceMode mode, string title)
    {
        var tag = $"telephone:center:{mode}";
        var existing = System.Windows.Application.Current.Windows
            .OfType<System.Windows.Window>()
            .FirstOrDefault(window => Equals(window.Tag, tag) && window.IsLoaded);
        if (existing is not null)
        {
            existing.Activate();
            existing.Focus();
            return;
        }

        IServiceScope? scope = null;
        TelephoneCenterViewModel? viewModel;
        try
        {
            scope = serviceProvider?.CreateScope();
            viewModel = scope?.ServiceProvider.GetService<TelephoneCenterViewModel>();
        }
        catch (Exception ex)
        {
            scope?.Dispose();
            ShowUnavailable(title, $"không khởi tạo được workspace: {ex.Message}");
            return;
        }

        if (viewModel is null || scope is null)
        {
            scope?.Dispose();
            ShowUnavailable(title, "chưa được đăng ký trong DI.");
            return;
        }

        viewModel.Mode = mode;
        var window = new System.Windows.Window
        {
            Title = title,
            Tag = tag,
            Content = new TelephoneCenterView { DataContext = viewModel },
            Owner = GetVisibleOwner(),
            WindowStartupLocation = System.Windows.WindowStartupLocation.CenterOwner,
            Width = 1220,
            Height = 760,
            MinWidth = 980,
            MinHeight = 620,
            ResizeMode = System.Windows.ResizeMode.CanResize,
            ShowInTaskbar = false
        };
        window.Closed += (_, _) => scope.Dispose();
        try
        {
            window.Show();
        }
        catch
        {
            scope.Dispose();
            throw;
        }
    }

    private static void OpenMarketingCenter(string section)
    {
        var existing = System.Windows.Application.Current.Windows
            .OfType<MarketingCenterWindow>()
            .FirstOrDefault(window => window.IsLoaded);
        if (existing is not null)
        {
            existing.ShowSection(section);
            if (existing.WindowState == System.Windows.WindowState.Minimized)
                existing.WindowState = System.Windows.WindowState.Normal;
            existing.Activate();
            existing.Focus();
            return;
        }

        new MarketingCenterWindow(section)
        {
            Owner = GetVisibleOwner(),
            WindowStartupLocation = System.Windows.WindowStartupLocation.CenterOwner,
            ResizeMode = System.Windows.ResizeMode.CanResize,
            ShowInTaskbar = false
        }.Show();
    }

    private static void OpenNightAuditCenter(string section)
    {
        var existing = System.Windows.Application.Current.Windows
            .OfType<NightAuditCenterWindow>()
            .FirstOrDefault(window => window.IsLoaded);
        if (existing is not null)
        {
            existing.ShowSection(section);
            if (existing.WindowState == System.Windows.WindowState.Minimized)
                existing.WindowState = System.Windows.WindowState.Normal;
            existing.Activate();
            existing.Focus();
            return;
        }

        new NightAuditCenterWindow(section)
        {
            Owner = GetVisibleOwner(),
            WindowStartupLocation = System.Windows.WindowStartupLocation.CenterOwner,
            ResizeMode = System.Windows.ResizeMode.CanResize,
            ShowInTaskbar = false
        }.Show();
    }

    private static void OpenReportCenter(string category, string source)
    {
        var w = new FrontOfficeReportCenterWindow(categoryFilter: category, sourceModule: source);
        SetVisibleOwner(w);
        w.Show();
    }

    private void BuildCards()
    {
        Cards.Clear();
        var frontOfficeEnabled = CanOpenFrontOffice;
        Cards.Add(new FrontOfficeHubCard
        {
            Id = "reservation",
            Title = "Đặt phòng",
            Description = "Quản lý đặt phòng khách sạn.",
            IconGlyph = "\uE787",
            StatusText = "Đang hoạt động",
            IsActive = true,
            IsEnabled = frontOfficeEnabled,
            DisabledReason = frontOfficeEnabled ? null : FrontOfficeDisabledReason,
            // Opening the reservation workspace can await navigation/audit persistence.
            // Keep the entry card clickable while that non-UI work is in flight; the
            // workspace/window manager already de-duplicates the opened module.
            Command = new AsyncRelayCommand(OpenReservationAsync, AsyncRelayCommandOptions.AllowConcurrentExecutions)
        });

        Cards.Add(new FrontOfficeHubCard
        {
            Id = "reception",
            Title = "Lễ tân",
            Description = "Trung tâm tác nghiệp tại quầy lễ tân.",
            IconGlyph = "\uE77B",
            StatusText = "Đang hoạt động",
            IsActive = true,
            IsEnabled = frontOfficeEnabled,
            DisabledReason = frontOfficeEnabled ? null : FrontOfficeDisabledReason,
            Command = new AsyncRelayCommand(OpenReceptionAsync)
        });

        Cards.Add(new FrontOfficeHubCard
        {
            Id = "cashier",
            Title = "Thu ngân",
            Description = "Thu tiền Folio, giao dịch và quản lý nghiệp vụ thu ngân.",
            IconGlyph = "\uE8C7",
            StatusText = "Đang hoạt động",
            IsActive = true,
            IsEnabled = frontOfficeEnabled,
            DisabledReason = frontOfficeEnabled ? null : FrontOfficeDisabledReason,
            Command = new AsyncRelayCommand(OpenCashierHubAsync)
        });
        Cards.Add(new FrontOfficeHubCard
        {
            Id = "key-management",
            Title = "Quản lý chìa khóa",
            Description = "Quản lý phát hành và thu hồi thẻ phòng.",
            IconGlyph = "\uE722",
            StatusText = "Đang hoạt động",
            IsActive = true,
            IsEnabled = frontOfficeEnabled,
            DisabledReason = frontOfficeEnabled ? null : FrontOfficeDisabledReason,
            Command = new AsyncRelayCommand(OpenKeyManagementAsync)
        });
        Cards.Add(new FrontOfficeHubCard
        {
            Id = "hotel-settings",
            Title = "Thiết lập khách sạn",
            Description = "Khai báo hạng phòng, phòng, nguồn khách, phân khúc, tỷ giá và tiện nghi.",
            IconGlyph = "\uE713",
            StatusText = "Đang hoạt động",
            IsActive = true,
            IsEnabled = frontOfficeEnabled,
            DisabledReason = frontOfficeEnabled ? null : FrontOfficeDisabledReason,
            Command = new AsyncRelayCommand(OpenHotelSetupAsync)
        });
        AddModuleEntry("group", "Đoàn", "Quan ly dat phong doan va ho so khach doan.", "\uE902");
        AddModuleEntry("housekeeping", "Nhà buồng", "Theo doi tinh trang phong va nghiep vu buong phong.", "\uE8D1");
        AddModuleEntry("telephone", "Tổng đài", "Quan ly cuoc goi, loi nhan va yeu cau cua khach.", "\uE717");
        AddModuleEntry("receivable", "Kế toán phải thu", "Theo doi cong no va tai khoan phai thu.", "\uEAFD");
        AddModuleEntry("inquiry", "Tìm kiếm toàn hệ thống", "Tra cuu khach, phong, dat phong, luu tru va giao dich.", "\uE721");
        AddModuleEntry("marketing", "Marketing", "Quan ly nguon khach, thi truong va chuong trinh tiep thi.", "\uE719");
        AddModuleEntry("reports", "Báo cáo", "Tong hop bao cao van hanh Front Office.", "\uE9D2");
        AddModuleEntry("night-audit", "Night Audit", "Kiem tra va dong ngay kinh doanh khach san.", "\uE787");
        ApplyFilter();
    }

    private void AddModuleEntry(string id, string title, string description, string glyph)
    {
        var targetHub = new ModuleHubViewModel();
        ConfigureHubCards(targetHub, title);
        var isAvailable = targetHub.Cards.Count > 0;
        var canOpen = CanOpenFrontOffice;

        var card = new FrontOfficeHubCard
        {
            Id = id,
            Title = title,
            Description = description,
            IconGlyph = glyph,
            StatusText = isAvailable ? "Đang hoạt động" : "Sắp có",
            IsActive = isAvailable,
            IsEnabled = canOpen,
            DisabledReason = canOpen ? null : FrontOfficeDisabledReason,
            Command = new RelayCommand(() => OpenModuleHubOrShowComingSoon(title))
        };

        Cards.Add(card);
    }

    partial void OnFilterTextChanged(string value) => ApplyFilter();
    partial void OnErrorMessageChanged(string value) => OnPropertyChanged(nameof(HasError));
    partial void OnActiveWorkspaceChanged(object? value)
    {
        OnPropertyChanged(nameof(IsMainHubOpen));
        OnPropertyChanged(nameof(IsReservationHubOpen));
        OnPropertyChanged(nameof(HasActiveWorkspace));
    }

    private void ApplyFilter()
    {
        var filter = FilterText.Trim();
        foreach (var card in Cards)
        {
            card.IsVisible = string.IsNullOrWhiteSpace(filter)
                || card.Title.Contains(filter, StringComparison.CurrentCultureIgnoreCase)
                || card.Description.Contains(filter, StringComparison.CurrentCultureIgnoreCase);
        }
    }

    private void OnPropertyContextChanged(object? sender, FrontOfficePropertyContextChangedEventArgs e)
    {
        ErrorMessage = e.Current is null ? "Vui lòng chọn chi nhánh trước khi sử dụng Front Office." : string.Empty;
        BuildCards();
        OnPropertyChanged(nameof(HasPropertyContext));
        OnPropertyChanged(nameof(CurrentPropertyDisplay));
        OnPropertyChanged(nameof(BusinessDateDisplay));
    }

    public void Dispose()
    {
        propertyContextHost.PropertyChanged -= OnPropertyContextChanged;
    }
}
