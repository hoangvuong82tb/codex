using Microsoft.Extensions.DependencyInjection;
using System.Collections.ObjectModel;
using ASMILE.BO.App.Models;
using ASMILE.BO.App.Services;
using ASMILE.BO.App.Views;
using ASMILE.BO.Application.FrontOffice.Stay;
using ASMILE.BO.Core.FrontOffice.Stay;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ASMILE.BO.App.ViewModels;

public sealed partial class ReceptionHubViewModel : ViewModelBase, IDisposable
{
    private readonly IReceptionHubPermissionReader permissions;
    private readonly IFrontOfficePropertyContextHost propertyContextHost;
    private readonly INavigationService? navigationService;
    private readonly ICheckInStayModalService? checkInModalService;
    private readonly ICheckOutStayModalService? checkOutModalService;
    private readonly IReceptionUiService? receptionUiService;
    private readonly IServiceProvider? _serviceProvider;
    private readonly ILockedRoomWindowService? lockedRoomWindowService;

    public ICheckInStayModalService? CheckInModal => checkInModalService;
    public ICheckOutStayModalService? CheckOutModal => checkOutModalService;

    public ReceptionHubViewModel(
        IReceptionHubPermissionReader permissions,
        IFrontOfficePropertyContextHost propertyContextHost,
        IReceptionWorkspaceModalService modalService,
        IServiceProvider? serviceProvider,
        ILockedRoomWindowService? lockedRoomWindowService,
        INavigationService? navigationService = null,
        ICheckInStayModalService? checkInModalService = null,
        ICheckOutStayModalService? checkOutModalService = null,
        IReceptionUiService? receptionUiService = null)
    {
        this.permissions = permissions;
        this.propertyContextHost = propertyContextHost;
        this.navigationService = navigationService;
        this.checkInModalService = checkInModalService;
        this.checkOutModalService = checkOutModalService;
        this.receptionUiService = receptionUiService;
        _serviceProvider = serviceProvider;
        this.lockedRoomWindowService = lockedRoomWindowService;
        ModalService = modalService;
        propertyContextHost.PropertyChanged += OnPropertyContextChanged;
        BuildCards();
    }

    public ObservableCollection<ReceptionHubCard> Cards { get; } = new();
    public IReceptionWorkspaceModalService ModalService { get; }
    public Func<Task>? ReturnToParentAsync { get; set; }
    [ObservableProperty] private string errorMessage = string.Empty;
    [ObservableProperty] private string infoMessage = string.Empty;
    [ObservableProperty] private bool isComingSoonDialogOpen;
    [ObservableProperty] private string comingSoonTitle = string.Empty;
    [ObservableProperty] private object? activeOverlay;
    [ObservableProperty] private object? activeWorkspace;

    public bool HasPropertyContext => propertyContextHost.HasCurrentProperty && permissions.HasContext;
    public bool IsHubVisible => ActiveWorkspace == null;
    public bool IsWorkspaceVisible => ActiveWorkspace != null;
    public string CurrentPropertyDisplay => propertyContextHost.CurrentProperty?.DisplayName ?? "Chưa chọn chi nhánh";
    public string BusinessDateDisplay => DateTime.Today.ToString("dddd, dd/MM/yyyy", new System.Globalization.CultureInfo("vi-VN"));
    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);
    public bool HasInfo => !string.IsNullOrWhiteSpace(InfoMessage);

    [RelayCommand] private void Refresh() { ErrorMessage = string.Empty; InfoMessage = "Đã cập nhật quyền và ngữ cảnh Front Office."; BuildCards(); NotifyContextState(); }
    [RelayCommand] private void CloseComingSoon() => IsComingSoonDialogOpen = false;
    [RelayCommand] private void BackToHub() => ActiveWorkspace = null;
    [RelayCommand] private async Task BackAsync() { ModalService.Close(); checkInModalService?.Close(); checkOutModalService?.Close(); if (ActiveWorkspace is not null) { ActiveWorkspace = null; return; } if (ReturnToParentAsync is not null) { await ReturnToParentAsync(); return; } if (navigationService is null) return; var result = await navigationService.GoBackAsync(); if (!result.Success) await navigationService.OpenModuleAsync("front-office"); }

    private void BuildCards()
    {
        Cards.Clear();
        var hasContext = HasPropertyContext;
        var canView = hasContext && permissions.CanView;

        AddCard("room-status", "Sơ đồ phòng vận hành", "Sơ đồ phòng trực quan: Vacant, Occupied, Dirty, Cleaning, OOO.", "", "Sẵn sàng", canView, () => OpenRoomStatusAsync());
        AddCard("check-in", "Check-in", "Nhận phòng cho khách có đặt phòng và đã sẵn sàng lưu trú.", "", "Sẵn sàng", canView && checkInModalService is not null, () => OpenCheckInWindowAsync());
        AddCard("walk-in", "Walk-In", "Tạo lưu trú nhanh cho khách không đặt trước.", "", "Sẵn sàng", hasContext, () => OpenWalkInWindowAsync());
        AddCard("stay-adjustment", "Điều chỉnh lưu trú", "Đổi phòng, gia hạn và điều chỉnh thông tin lưu trú.", "", "Sẵn sàng", canView, () => { ActiveWorkspace = new StayAdjustmentWorkspaceView(); return Task.CompletedTask; });
        AddCard("checkout", "Check-out", "Hoàn tất trả phòng và bàn giao.", "", "Sẵn sàng", canView && checkOutModalService is not null, OpenCheckOutWindowAsync);

        AddCard("stay-profile", "Hồ sơ lưu trú", "Tra cứu và xem hồ sơ các lượt lưu trú.", "", "Tra cứu", canView, OpenStayProfileWindowAsync);
        AddCard("locked-room", "Phòng khóa", "Theo dõi phòng khóa, phòng tạm dừng và lý do.", "", "Tra cứu", canView, OpenLockedRoomWindowAsync);
        AddCard("current-guests", "Khách hiện tại", "Danh sách khách đang lưu trú trong khách sạn.", "", "Tra cứu", canView, OpenCurrentGuestsWindowAsync);
        AddCard("arrivals", "Khách đến hôm nay", "Danh sách khách dự kiến đến trong ngày.", "", "Tra cứu", canView, OpenTodayArrivalsWindowAsync);

        AddCard("departures", "Khách đi hôm nay", "Danh sách khách dự kiến rời trong ngày.", "", "Tra cứu", canView, OpenTodayDeparturesWindowAsync);
        AddCard("housekeeping-message", "Nhắn nhà buồng", "Gửi và theo dõi yêu cầu phối hợp với nhà buồng.", "", "Sẵn sàng", canView, () => { OpenHousekeepingMessageWindow(); return Task.CompletedTask; });
        AddCard("exchange-rate", "Tỷ giá", "Tra cứu tỷ giá áp dụng trong ngày.", "", "Tra cứu", canView, () => { OpenExchangeRateWindow(); return Task.CompletedTask; });
        AddCard("group-check-in", "Check-in đoàn", "Tiếp nhận và làm thủ tục check-in cho khách đoàn.", "", "Sẵn sàng", canView, () => { OpenGroupCheckInWindow(); return Task.CompletedTask; });
    }

    private void AddCard(string id, string title, string description, string glyph, string badge, bool allowed, Func<Task>? action = null, bool isComingSoon = false)
    {
        Cards.Add(new ReceptionHubCard
        {
            Id = id, Title = title, Description = description, IconGlyph = glyph,
            BadgeText = badge, IsEnabled = allowed, IsComingSoon = isComingSoon,
            Command = isComingSoon ? new RelayCommand(() => { ComingSoonTitle = title; IsComingSoonDialogOpen = true; })
                : action is not null ? new AsyncRelayCommand(action)
                : new AsyncRelayCommand(async () => await OpenModalAsync(id))
        });
    }

    private HousekeepingRoomStatusWindow? _roomStatusWindow;
    private CheckInWindow? _checkInWindow;
    private GroupCheckInWindow? _groupCheckInWindow;
    private HousekeepingMessageWindow? _housekeepingMessageWindow;
    private ExchangeRateWindow? _exchangeRateWindow;
    private InvoiceManagementWindow? _invoiceWindow;
    private WalkInWindow? _walkInWindow;

    private Task OpenStayProfileWindowAsync() => OpenReceptionWindowAsync(() => new StayProfileWindow(), ReceptionWorkspaceKind.StayProfiles, "stay-profile");

    private Task OpenLockedRoomWindowAsync()
    {
        if (CanOpenReceptionWindow() && lockedRoomWindowService is not null)
        {
            try
            {
                lockedRoomWindowService.Open();
                return Task.CompletedTask;
            }
            catch
            {
                ErrorMessage = "Không thể mở màn hình Phòng khóa. Vui lòng kiểm tra nhật ký hệ thống.";
                return Task.CompletedTask;
            }
        }

        return OpenModalAsync(ReceptionWorkspaceKind.LockedRooms, "locked-room");
    }

    private Task OpenCurrentGuestsWindowAsync() => OpenReceptionWindowAsync(() => new CurrentGuestsWindow(), ReceptionWorkspaceKind.CurrentInHouse, "current-guests");

    private Task OpenTodayArrivalsWindowAsync() => OpenReceptionWindowAsync(() => new TodayArrivalsWindow(), ReceptionWorkspaceKind.ArrivalsToday, "arrivals");

    private Task OpenTodayDeparturesWindowAsync() => OpenReceptionWindowAsync(() => new TodayDeparturesWindow(), ReceptionWorkspaceKind.DeparturesToday, "departures");

    private bool CanOpenReceptionWindow() => _serviceProvider is not null && System.Windows.Application.Current is not null;

    private Task OpenReceptionWindowAsync<TWindow>(Func<TWindow> factory, ReceptionWorkspaceKind fallbackKind, string sourceCardId)
        where TWindow : System.Windows.Window
    {
        if (!CanOpenReceptionWindow())
        {
            return OpenModalAsync(fallbackKind, sourceCardId);
        }

        var existing = System.Windows.Application.Current.Windows
            .OfType<TWindow>()
            .FirstOrDefault(window => window.IsLoaded);
        if (existing is not null)
        {
            if (existing.WindowState == System.Windows.WindowState.Minimized)
            {
                existing.WindowState = System.Windows.WindowState.Normal;
            }

            existing.Activate();
            existing.Focus();
            return Task.CompletedTask;
        }

        var window = factory();
        window.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen;
        SetVisibleOwner(window);
        window.ResizeMode = System.Windows.ResizeMode.CanResize;
        window.ShowInTaskbar = false;
        window.Show();
        return Task.CompletedTask;
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

    private Task OpenModalAsync(string kind)
    {
        var workspaceKind = kind switch
        {
            "locked-room" => ReceptionWorkspaceKind.LockedRooms,
            "arrivals" => ReceptionWorkspaceKind.ArrivalsToday,
            "departures" => ReceptionWorkspaceKind.DeparturesToday,
            "stay-profile" => ReceptionWorkspaceKind.StayProfiles,
            "current-guests" => ReceptionWorkspaceKind.CurrentInHouse,
            _ => ReceptionWorkspaceKind.StayProfiles
        };
        return OpenModalAsync(workspaceKind, kind);
    }

    private async Task OpenModalAsync(ReceptionWorkspaceKind workspaceKind, string sourceCardId)
    {
        if (!HasPropertyContext || !permissions.CanView) { ErrorMessage = "Vui lòng chọn chi nhánh Front Office và bảo đảm quyền xem trước khi tiếp tục."; return; }
        ErrorMessage = string.Empty;

        try { await ModalService.OpenAsync(workspaceKind, sourceCardId); }
        catch { ErrorMessage = "Không thể mở không gian tra cứu lễ tân. Vui lòng thử lại hoặc kiểm tra nhật ký hệ thống."; }
    }

    private Task OpenRoomStatusAsync()
    {
        if (_roomStatusWindow is not null) { _roomStatusWindow.Activate(); return Task.CompletedTask; }
        if (_serviceProvider is null) { ErrorMessage = "Dịch vụ dữ liệu phòng chưa được cấu hình."; return Task.CompletedTask; }
        _roomStatusWindow = new HousekeepingRoomStatusWindow();
        _roomStatusWindow.Closed += (_, _) => _roomStatusWindow = null;
        SetVisibleOwner(_roomStatusWindow);
        _roomStatusWindow.Show();
        return Task.CompletedTask;
    }

    private async Task OpenCheckInWindowAsync()
    {
        if (_checkInWindow is not null) { _checkInWindow.Activate(); return; }
        if (_serviceProvider is not null)
        {
            try
            {
                var workspace = _serviceProvider.GetRequiredService<ReservationWorkspaceViewModel>();
                workspace.SelectedStatus = Core.FrontOffice.Reservation.ReservationStatus.Confirmed;
                _checkInWindow = new CheckInWindow(workspace);
                _checkInWindow.Closed += (_, _) => _checkInWindow = null;
                SetVisibleOwner(_checkInWindow);
                _checkInWindow.Show();
                return;
            }
            catch
            {
                ErrorMessage = "Không thể mở không gian Check-in. Vui lòng thử lại hoặc kiểm tra nhật ký hệ thống.";
                return;
            }
        }
        if (checkInModalService is not null)
        {
            await checkInModalService.OpenAsync("check-in");
            return;
        }
        ErrorMessage = "Dịch vụ đặt phòng chưa được cấu hình.";
    }

    private async Task OpenCheckOutWindowAsync()
    {
        if (_serviceProvider is not null)
        {
            ActiveWorkspace = new CheckoutWorkspaceView();
            return;
        }
        if (checkOutModalService is null)
        {
            ErrorMessage = "Dịch vụ Check-out chưa được cấu hình.";
            return;
        }

        try
        {
            await checkOutModalService.OpenAsync("checkout");
        }
        catch
        {
            ErrorMessage = "Không thể mở không gian Check-out. Vui lòng thử lại hoặc kiểm tra nhật ký hệ thống.";
        }
    }

    private void OpenGroupCheckInWindow()
    {
        if (_groupCheckInWindow is not null) { _groupCheckInWindow.Activate(); return; }
        _groupCheckInWindow = new GroupCheckInWindow();
        _groupCheckInWindow.Closed += (_, _) => _groupCheckInWindow = null;
        SetVisibleOwner(_groupCheckInWindow);
        _groupCheckInWindow.Show();
    }

    private void OpenHousekeepingMessageWindow()
    {
        if (_housekeepingMessageWindow is not null) { _housekeepingMessageWindow.Activate(); return; }
        _housekeepingMessageWindow = new HousekeepingMessageWindow();
        _housekeepingMessageWindow.Closed += (_, _) => _housekeepingMessageWindow = null;
        SetVisibleOwner(_housekeepingMessageWindow);
        _housekeepingMessageWindow.Show();
    }

    private void OpenExchangeRateWindow()
    {
        if (_exchangeRateWindow is not null) { _exchangeRateWindow.Activate(); return; }
        _exchangeRateWindow = new ExchangeRateWindow();
        _exchangeRateWindow.Closed += (_, _) => _exchangeRateWindow = null;
        SetVisibleOwner(_exchangeRateWindow);
        _exchangeRateWindow.Show();
    }

    private void OpenInvoiceWindow()
    {
        if (_invoiceWindow is not null) { _invoiceWindow.Activate(); return; }
        _invoiceWindow = new InvoiceManagementWindow();
        _invoiceWindow.Closed += (_, _) => _invoiceWindow = null;
        SetVisibleOwner(_invoiceWindow);
        _invoiceWindow.Show();
    }

    private async Task OpenWalkInWindowAsync()
    {
        if (_walkInWindow is not null) { _walkInWindow.Activate(); return; }
        if (receptionUiService is null || _serviceProvider is null)
        {
            ErrorMessage = "Dịch vụ Walk-In chưa được cấu hình.";
            return;
        }
        _walkInWindow = new WalkInWindow(receptionUiService, _serviceProvider);
        _walkInWindow.Closed += (_, _) => _walkInWindow = null;
        SetVisibleOwner(_walkInWindow);
        _walkInWindow.Show();
    }

    partial void OnErrorMessageChanged(string value) => OnPropertyChanged(nameof(HasError));
    partial void OnInfoMessageChanged(string value) => OnPropertyChanged(nameof(HasInfo));
    partial void OnActiveWorkspaceChanged(object? value) { OnPropertyChanged(nameof(IsHubVisible)); OnPropertyChanged(nameof(IsWorkspaceVisible)); }
    private void OnPropertyContextChanged(object? sender, FrontOfficePropertyContextChangedEventArgs e)
    { ModalService.Close(); checkInModalService?.Close(); checkOutModalService?.Close(); permissions.ResetForContextChange(); ErrorMessage = e.Current is null ? "Vui lòng chọn chi nhánh trước khi sử dụng Lễ tân." : string.Empty; InfoMessage = e.Current is null ? string.Empty : "Đã chuyển ngữ cảnh chi nhánh Front Office."; BuildCards(); NotifyContextState(); }
    private void NotifyContextState() { OnPropertyChanged(nameof(HasPropertyContext)); OnPropertyChanged(nameof(CurrentPropertyDisplay)); OnPropertyChanged(nameof(BusinessDateDisplay)); OnPropertyChanged(nameof(HasError)); OnPropertyChanged(nameof(HasInfo)); }
    public void Dispose() { propertyContextHost.PropertyChanged -= OnPropertyContextChanged; ModalService.Dispose(); checkInModalService?.Dispose(); checkOutModalService?.Dispose(); permissions.Dispose(); }
}
