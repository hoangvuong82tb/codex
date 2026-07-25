using System.Collections.ObjectModel;
using System.Windows.Media;
using ASMILE.BO.App.Models;
using ASMILE.BO.App.Services;
using ASMILE.BO.App.Views;
using ASMILE.BO.Application.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ASMILE.BO.App.ViewModels;

/// <summary>Enterprise entry point for Front Office. It routes to vertical hubs and owns no workflow logic.</summary>
public sealed partial class FrontOfficeHubViewModel : ViewModelBase, IDisposable
{
    private readonly IReservationHubPermissionReader permissions;
    private readonly IFrontOfficePropertyContextHost propertyContextHost;
    private readonly INavigationService navigationService;
    private readonly ICurrentUserService currentUserService;
    private readonly IServiceProvider serviceProvider;
    private readonly IWindowManagerService? windowManagerService;
    private ReservationHubViewModel? reservationHub;
    private ReceptionHubViewModel? receptionHub;

    public FrontOfficeHubViewModel(
        IReservationHubPermissionReader permissions,
        IFrontOfficePropertyContextHost propertyContextHost,
        INavigationService navigationService,
        ICurrentUserService currentUserService,
        IServiceProvider serviceProvider,
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

    private async Task OpenReservationAsync()
    {
        if (!HasPropertyContext || !permissions.CanView)
        {
            ErrorMessage = "Vui lòng chọn chi nhánh Front Office và bảo đảm quyền xem Đặt phòng trước khi tiếp tục.";
            return;
        }

        ErrorMessage = string.Empty;
        if (serviceProvider.GetService(typeof(ReservationHubViewModel)) is not ReservationHubViewModel workspace)
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

        if (serviceProvider.GetService(typeof(ReceptionHubViewModel)) is not ReceptionHubViewModel workspace)
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

    private void SetWindowTitle(string title)
    {
        var window = windowManagerService?.OpenWindows.FirstOrDefault(x => ReferenceEquals(x.ContentViewModel, this));
        if (window is not null)
        {
            window.Module.Title = title;
        }
    }

    private void ShowComingSoon(string title)
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

    private static void ConfigureHubCards(ModuleHubViewModel hub, string hubId)
    {
        switch (hubId)
        {
            case "Thu ngân":
                hub.Title = "Thu ngân"; hub.Subtitle = "Thanh toán, giao dịch và quản lý folio khách";
                hub.Cards.Add(CreateCard("payment", "Thanh toán", "", "#4A8EFF", "#1765F2"));
                hub.Cards.Add(CreateCard("folio", "Folio", "", "#31C9B8", "#00A58F"));
                hub.Cards.Add(CreateCard("deposit", "Tiền đặt cọc", "", "#FFAD39", "#ED7900"));
                hub.Cards.Add(CreateCard("refund", "Hoàn tiền", "", "#B158FF", "#7D2BE1"));
                hub.Cards.Add(CreateCard("invoice", "Hóa đơn", "", "#4ED86E", "#18B33F"));
                hub.Cards.Add(CreateCard("receipt", "Phiếu thu", "", "#4A91FF", "#1E6CF1"));
                hub.Cards.Add(CreateCard("voucher", "Phiếu chi", "", "#2CC7D8", "#00A7BA"));
                hub.Cards.Add(CreateCard("transactions", "Giao dịch", "", "#3E83F3", "#195FCE"));
                hub.Cards.Add(CreateCard("cashier-reports", "Báo cáo thu ngân", "", "#2661D9", "#1046B7", () => OpenReportCenter("Cashiering", "Thu ngân")));
                break;
            case "Đoàn":
                hub.Title = "Đoàn"; hub.Subtitle = "Quản lý đặt phòng đoàn và hồ sơ khách đoàn";
                hub.Cards.Add(CreateCard("group-create", "Tạo đoàn", "", "#4A8EFF", "#1765F2"));
                hub.Cards.Add(CreateCard("group-list", "Danh sách đoàn", "", "#31C9B8", "#00A58F"));
                hub.Cards.Add(CreateCard("group-booking", "Đặt phòng đoàn", "", "#FFAD39", "#ED7900"));
                hub.Cards.Add(CreateCard("group-assign", "Phân phòng đoàn", "", "#B158FF", "#7D2BE1"));
                hub.Cards.Add(CreateCard("rooming-list", "Rooming List", "", "#4ED86E", "#18B33F"));
                hub.Cards.Add(CreateCard("group-checkin", "Check-in đoàn", "", "#4A91FF", "#1E6CF1"));
                hub.Cards.Add(CreateCard("group-checkout", "Check-out đoàn", "", "#2CC7D8", "#00A7BA"));
                hub.Cards.Add(CreateCard("group-guests", "Khách trong đoàn", "", "#3E83F3", "#195FCE"));
                hub.Cards.Add(CreateCard("group-services", "Dịch vụ đoàn", "", "#FF5A94", "#E32F70"));
                hub.Cards.Add(CreateCard("group-reports", "Báo cáo đoàn", "", "#2661D9", "#1046B7", () => OpenReportCenter("Group", "Đoàn")));
                break;
            case "Nhà buồng":
                hub.Title = "Nhà buồng"; hub.Subtitle = "Theo dõi tình trạng phòng và nghiệp vụ buồng phòng";
                hub.Cards.Add(CreateCard("hk-status", "Tình trạng phòng", "", "#4A8EFF", "#1765F2"));
                hub.Cards.Add(CreateCard("hk-list", "Danh sách phòng", "", "#31C9B8", "#00A58F"));
                hub.Cards.Add(CreateCard("hk-clean", "Phòng sạch", "", "#4ED86E", "#18B33F"));
                hub.Cards.Add(CreateCard("hk-dirty", "Phòng bẩn", "", "#FFAD39", "#ED7900"));
                hub.Cards.Add(CreateCard("hk-cleaning", "Đang dọn", "", "#B158FF", "#7D2BE1"));
                hub.Cards.Add(CreateCard("hk-inspect", "Kiểm tra phòng", "", "#4A91FF", "#1E6CF1"));
                hub.Cards.Add(CreateCard("hk-lost-found", "Lost & Found", "", "#2CC7D8", "#00A7BA"));
                hub.Cards.Add(CreateCard("hk-minibar", "Minibar", "", "#3E83F3", "#195FCE"));
                hub.Cards.Add(CreateCard("hk-assign", "Phân công", "", "#FF5A94", "#E32F70"));
                hub.Cards.Add(CreateCard("hk-reports", "Báo cáo", "", "#2661D9", "#1046B7", () => OpenReportCenter("Housekeeping", "Nhà buồng")));
                break;
            case "Tổng đài":
                hub.Title = "Tổng đài"; hub.Subtitle = "Quản lý cuộc gọi, lời nhắn và yêu cầu của khách";
                hub.Cards.Add(CreateCard("tel-incoming", "Cuộc gọi đến", "", "#4A8EFF", "#1765F2"));
                hub.Cards.Add(CreateCard("tel-outgoing", "Cuộc gọi đi", "", "#31C9B8", "#00A58F"));
                hub.Cards.Add(CreateCard("tel-log", "Nhật ký cuộc gọi", "", "#FFAD39", "#ED7900"));
                hub.Cards.Add(CreateCard("tel-wakeup", "Đánh thức", "", "#B158FF", "#7D2BE1"));
                hub.Cards.Add(CreateCard("tel-messages", "Lời nhắn", "", "#4ED86E", "#18B33F"));
                hub.Cards.Add(CreateCard("tel-contacts", "Danh bạ", "", "#2CC7D8", "#00A7BA"));
                hub.Cards.Add(CreateCard("tel-requests", "Yêu cầu khách", "", "#3E83F3", "#195FCE"));
                hub.Cards.Add(CreateCard("tel-reports", "Báo cáo", "", "#2661D9", "#1046B7", () => OpenReportCenter("Telephone", "Tổng đài")));
                break;
            case "Kế toán phải thu":
                hub.Title = "Kế toán phải thu"; hub.Subtitle = "Theo dõi công nợ và tài khoản phải thu";
                hub.Cards.Add(CreateCard("ar-debt", "Công nợ", "", "#4A8EFF", "#1765F2"));
                hub.Cards.Add(CreateCard("ar-customers", "Khách hàng", "", "#31C9B8", "#00A58F"));
                hub.Cards.Add(CreateCard("ar-companies", "Doanh nghiệp", "", "#FFAD39", "#ED7900"));
                hub.Cards.Add(CreateCard("ar-invoices", "Hóa đơn", "", "#B158FF", "#7D2BE1"));
                hub.Cards.Add(CreateCard("ar-reconcile", "Đối chiếu", "", "#4ED86E", "#18B33F"));
                hub.Cards.Add(CreateCard("ar-payment", "Thanh toán", "", "#4A91FF", "#1E6CF1"));
                hub.Cards.Add(CreateCard("ar-aging", "Tuổi nợ", "", "#2CC7D8", "#00A7BA"));
                hub.Cards.Add(CreateCard("ar-reports", "Báo cáo", "", "#2661D9", "#1046B7", () => OpenReportCenter("AccountsReceivable", "Kế toán phải thu")));
                break;
            case "Truy vấn thông tin":
                hub.Title = "Truy vấn thông tin"; hub.Subtitle = "Tra cứu khách, phòng, đặt phòng và lịch sử lưu trú";
                hub.Cards.Add(CreateCard("inq-guest", "Tra cứu khách", "", "#4A8EFF", "#1765F2"));
                hub.Cards.Add(CreateCard("inq-booking", "Tra cứu đặt phòng", "", "#31C9B8", "#00A58F"));
                hub.Cards.Add(CreateCard("inq-room", "Tra cứu phòng", "", "#FFAD39", "#ED7900"));
                hub.Cards.Add(CreateCard("inq-stay", "Tra cứu lưu trú", "", "#B158FF", "#7D2BE1"));
                hub.Cards.Add(CreateCard("inq-history", "Lịch sử", "", "#4ED86E", "#18B33F"));
                hub.Cards.Add(CreateCard("inq-folio", "Folio", "", "#2CC7D8", "#00A7BA"));
                hub.Cards.Add(CreateCard("inq-transactions", "Giao dịch", "", "#3E83F3", "#195FCE"));
                hub.Cards.Add(CreateCard("inq-log", "Nhật ký", "", "#2661D9", "#1046B7"));
                break;
            case "Marketing":
                hub.Title = "Marketing"; hub.Subtitle = "Quản lý nguồn khách, thị trường và chương trình tiếp thị";
                hub.Cards.Add(CreateCard("mkt-source", "Nguồn khách", "", "#4A8EFF", "#1765F2"));
                hub.Cards.Add(CreateCard("mkt-segment", "Phân khúc", "", "#31C9B8", "#00A58F"));
                hub.Cards.Add(CreateCard("mkt-company", "Công ty", "", "#FFAD39", "#ED7900"));
                hub.Cards.Add(CreateCard("mkt-agency", "Đại lý", "", "#B158FF", "#7D2BE1"));
                hub.Cards.Add(CreateCard("mkt-customers", "Khách hàng", "", "#4ED86E", "#18B33F"));
                hub.Cards.Add(CreateCard("mkt-members", "Thành viên", "", "#4A91FF", "#1E6CF1"));
                hub.Cards.Add(CreateCard("mkt-promo", "Khuyến mại", "", "#FF5A94", "#E32F70"));
                hub.Cards.Add(CreateCard("mkt-reports", "Báo cáo", "", "#2661D9", "#1046B7", () => OpenReportCenter("Marketing", "Marketing")));
                break;
            case "Báo cáo":
                hub.Title = "Báo cáo"; hub.Subtitle = "Tổng hợp báo cáo vận hành Front Office";
                hub.Cards.Add(CreateCard("rpt-booking", "Đặt phòng", "", "#4A8EFF", "#1765F2", () => OpenReportCenter("Reservation", "Báo cáo")));
                hub.Cards.Add(CreateCard("rpt-stay", "Lưu trú", "", "#31C9B8", "#00A58F", () => OpenReportCenter("FrontOffice", "Báo cáo")));
                hub.Cards.Add(CreateCard("rpt-arrivals", "Khách đến", "", "#FFAD39", "#ED7900", () => OpenReportCenter("FrontOffice", "Báo cáo")));
                hub.Cards.Add(CreateCard("rpt-departures", "Khách đi", "", "#B158FF", "#7D2BE1", () => OpenReportCenter("FrontOffice", "Báo cáo")));
                hub.Cards.Add(CreateCard("rpt-occupancy", "Công suất", "", "#4ED86E", "#18B33F", () => OpenReportCenter("FrontOffice", "Báo cáo")));
                hub.Cards.Add(CreateCard("rpt-revenue", "Doanh thu", "", "#4A91FF", "#1E6CF1", () => OpenReportCenter("Cashiering", "Báo cáo")));
                hub.Cards.Add(CreateCard("rpt-transactions", "Giao dịch", "", "#2CC7D8", "#00A7BA", () => OpenReportCenter("Cashiering", "Báo cáo")));
                hub.Cards.Add(CreateCard("rpt-housekeeping", "Nhà buồng", "", "#FF5A94", "#E32F70", () => OpenReportCenter("Housekeeping", "Báo cáo")));
                hub.Cards.Add(CreateCard("rpt-night-audit", "Night Audit", "", "#2661D9", "#1046B7", () => OpenReportCenter("NightAudit", "Báo cáo")));
                break;
            case "Night Audit":
                hub.Title = "Night Audit"; hub.Subtitle = "Kiểm tra và đóng ngày kinh doanh khách sạn";
                hub.Cards.Add(CreateCard("na-check", "Kiểm tra", "", "#4A8EFF", "#1765F2"));
                hub.Cards.Add(CreateCard("na-reconcile", "Đối chiếu", "", "#31C9B8", "#00A58F"));
                hub.Cards.Add(CreateCard("na-revenue", "Doanh thu", "", "#FFAD39", "#ED7900"));
                hub.Cards.Add(CreateCard("na-folio", "Folio", "", "#B158FF", "#7D2BE1"));
                hub.Cards.Add(CreateCard("na-run", "Chạy Night Audit", "", "#4ED86E", "#18B33F"));
                hub.Cards.Add(CreateCard("na-close", "Đóng ngày", "", "#4A91FF", "#1E6CF1"));
                hub.Cards.Add(CreateCard("na-log", "Nhật ký", "", "#2CC7D8", "#00A7BA"));
                hub.Cards.Add(CreateCard("na-reports", "Báo cáo", "", "#2661D9", "#1046B7", () => OpenReportCenter("NightAudit", "Night Audit")));
                break;
        }
    }

    private static ModuleHubCard CreateCard(string id, string title, string glyph, string c1, string c2, Action? action = null) => new()
    {
        Id = id, Title = title, IconGlyph = glyph,
        IconColor = new LinearGradientBrush((Color)ColorConverter.ConvertFromString(c1), (Color)ColorConverter.ConvertFromString(c2), 45),
        Command = action is not null ? new RelayCommand(action) : new RelayCommand(() => { })
    };

    private static void OpenReportCenter(string category, string source)
    {
        var w = new FrontOfficeReportCenterWindow(categoryFilter: category, sourceModule: source);
        w.Owner = System.Windows.Application.Current.MainWindow;
        w.Show();
    }

    private void BuildCards()
    {
        Cards.Clear();
        var reservationEnabled = HasPropertyContext && permissions.CanView;
        Cards.Add(new FrontOfficeHubCard
        {
            Id = "reservation",
            Title = "Đặt phòng",
            Description = "Quản lý đặt phòng khách sạn.",
            IconGlyph = "\uE787",
            StatusText = "Đang hoạt động",
            IsActive = true,
            IsEnabled = reservationEnabled,
            DisabledReason = reservationEnabled ? null : "Yêu cầu quyền xem Đặt phòng và chi nhánh Front Office đang hoạt động.",
            Command = new AsyncRelayCommand(OpenReservationAsync)
        });

        Cards.Add(new FrontOfficeHubCard
        {
            Id = "reception",
            Title = "Lễ tân",
            Description = "Trung tâm tác nghiệp tại quầy lễ tân.",
            IconGlyph = "\uE77B",
            StatusText = "Đang hoạt động",
            IsActive = true,
            IsEnabled = reservationEnabled,
            DisabledReason = reservationEnabled ? null : "Yêu cầu quyền xem Đặt phòng và chi nhánh Front Office đang hoạt động.",
            Command = new AsyncRelayCommand(OpenReceptionAsync)
        });

        AddComingSoon("cashier", "Thu ngân", "Thanh toan, giao dich va quan ly folio khach.", "\uE8C7");
        AddComingSoon("group", "Đoàn", "Quan ly dat phong doan va ho so khach doan.", "\uE902");
        AddComingSoon("housekeeping", "Nhà buồng", "Theo doi tinh trang phong va nghiep vu buong phong.", "\uE8D1");
        AddComingSoon("telephone", "Tổng đài", "Quan ly cuoc goi, loi nhan va yeu cau cua khach.", "\uE717");
        AddComingSoon("receivable", "Kế toán phải thu", "Theo doi cong no va tai khoan phai thu.", "\uEAFD");
        AddComingSoon("inquiry", "Truy vấn thông tin", "Tra cuu khach, phong, dat phong va lich su luu tru.", "\uE721");
        AddComingSoon("marketing", "Marketing", "Quan ly nguon khach, thi truong va chuong trinh tiep thi.", "\uE719");
        AddComingSoon("reports", "Báo cáo", "Tong hop bao cao van hanh Front Office.", "\uE9D2");
        AddComingSoon("night-audit", "Night Audit", "Kiem tra va dong ngay kinh doanh khach san.", "\uE787");
        ApplyFilter();
    }

    private void AddComingSoon(string id, string title, string description, string glyph)
    {
        Cards.Add(new FrontOfficeHubCard
        {
            Id = id,
            Title = title,
            Description = description,
            IconGlyph = glyph,
            StatusText = "Sắp có",
            IsActive = false,
            Command = new RelayCommand(() => ShowComingSoon(title))
        });
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
