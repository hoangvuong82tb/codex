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
    private readonly IReceptionUiService receptionUiService;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILockedRoomWindowService lockedRoomWindowService;

    public ICheckInStayModalService? CheckInModal => checkInModalService;
    public ICheckOutStayModalService? CheckOutModal => checkOutModalService;

    public ReceptionHubViewModel(
        IReceptionHubPermissionReader permissions,
        IFrontOfficePropertyContextHost propertyContextHost,
        IReceptionWorkspaceModalService modalService,
        IServiceProvider serviceProvider,
        ILockedRoomWindowService lockedRoomWindowService,
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
        this.receptionUiService = receptionUiService!;
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

        AddCard("room-status", "Tình trạng phòng", "Sơ đồ phòng trực quan: Vacant, Occupied, Dirty, Cleaning, OOO.", "", "Sẵn sàng", canView, () => OpenRoomStatusAsync());
        AddCard("check-in", "Check-in", "Nhận phòng cho khách có đặt phòng và đã sẵn sàng lưu trú.", "", "Sẵn sàng", canView && checkInModalService is not null, () => OpenCheckInWindowAsync());
        AddCard("walk-in", "Walk-In", "Tạo lưu trú nhanh cho khách không đặt trước.", "", "Sẵn sàng", hasContext, () => OpenWalkInWindowAsync());
        AddCard("group-checkin", "Check-in đoàn", "Nhận phòng hàng loạt cho booking đoàn.", "", "Sẵn sàng", canView, () => { OpenGroupCheckInWindow(); return Task.CompletedTask; });
        AddCard("stay-adjustment", "Điều chỉnh lưu trú", "Đổi phòng, gia hạn và điều chỉnh thông tin lưu trú.", "", "Sẵn sàng", canView, () => { ActiveWorkspace = new StayAdjustmentWorkspaceView(); return Task.CompletedTask; });
        AddCard("checkout", "Check-out", "Hoàn tất trả phòng và bàn giao.", "", "Sẵn sàng", canView, () => { ActiveWorkspace = new CheckoutWorkspaceView(); return Task.CompletedTask; });
        AddCard("cashiering", "Thu ngân / Folio", "Quản lý Folio và giao dịch Front Office.", "", "Sẵn sàng", canView, () => { ActiveWorkspace = new CashieringWorkspaceView(); return Task.CompletedTask; });
        AddCard("key-management", "Quản lý chìa khóa", "Quản lý phát hành và thu hồi thẻ phòng.", "", "Sẵn sàng", canView, () => { ActiveWorkspace = new KeyManagementWorkspaceView(); return Task.CompletedTask; });
        AddCard("fo-log", "Nhật ký FO", "Nhật ký vận hành và bàn giao ca lễ tân.", "", "Vận hành", canView, () => { ActiveWorkspace = new FrontOfficeLogWorkspaceView(); return Task.CompletedTask; });

        AddCard("stay-profile", "Hồ sơ lưu trú", "Tra cứu và xem hồ sơ các lượt lưu trú.", "", "Tra cứu", canView, () => { OpenStayProfileWindow(); return Task.CompletedTask; });
        AddCard("locked-room", "Phòng khóa", "Theo dõi phòng khóa, phòng tạm dừng và lý do.", "", "Tra cứu", canView, () =>
        {
            lockedRoomWindowService.Open();
            return Task.CompletedTask;
        });
        AddCard("current-guests", "Khách hiện tại", "Danh sách khách đang lưu trú trong khách sạn.", "", "Tra cứu", canView, () => { OpenCurrentGuestsWindow(); return Task.CompletedTask; });
        AddCard("arrivals", "Khách đến hôm nay", "Danh sách khách dự kiến đến trong ngày.", "", "Tra cứu", canView, () => { OpenTodayArrivalsWindow(); return Task.CompletedTask; });

        AddCard("departures", "Khách đi hôm nay", "Danh sách khách dự kiến rời trong ngày.", "", "Tra cứu", canView, () => { OpenTodayDeparturesWindow(); return Task.CompletedTask; });
        AddCard("housekeeping-message", "Nhắn nhà buồng", "Gửi và theo dõi yêu cầu phối hợp với nhà buồng.", "", "Sẵn sàng", canView, () => { OpenHousekeepingMessageWindow(); return Task.CompletedTask; });
        AddCard("exchange-rate", "Tỷ giá", "Tra cứu tỷ giá áp dụng trong ngày.", "", "Sẵn sàng", canView, () => { OpenExchangeRateWindow(); return Task.CompletedTask; });
        AddCard("reception-report", "Báo cáo", "Báo cáo hoạt động tại quầy lễ tân.", "", "Vận hành", canView, () => { OpenReportCenterWindow(); return Task.CompletedTask; });
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

    private RoomStatusWindow? _roomStatusWindow;
    private CheckInWindow? _checkInWindow;
    private GroupCheckInWindow? _groupCheckInWindow;
    private FrontOfficeReportCenterWindow? _reportCenterWindow;
    private StayProfileWindow? _stayProfileWindow;
    private CurrentGuestsWindow? _currentGuestsWindow;
    private TodayArrivalsWindow? _todayArrivalsWindow;
    private TodayDeparturesWindow? _todayDeparturesWindow;
    private HousekeepingMessageWindow? _housekeepingMessageWindow;
    private ExchangeRateWindow? _exchangeRateWindow;
    private WalkInWindow? _walkInWindow;

    private async Task OpenModalAsync(string kind)
    {
        if (!HasPropertyContext || !permissions.CanView) { ErrorMessage = "Vui lòng chọn chi nhánh Front Office và bảo đảm quyền xem trước khi tiếp tục."; return; }
        ErrorMessage = string.Empty;
        try { await ModalService.OpenAsync(ReceptionWorkspaceKind.StayProfiles, kind); }
        catch { ErrorMessage = "Không thể mở không gian tra cứu lễ tân. Vui lòng thử lại hoặc kiểm tra nhật ký hệ thống."; }
    }

    private async Task OpenRoomStatusAsync()
    {
        if (_roomStatusWindow is not null) { _roomStatusWindow.Activate(); return; }
        var queries = _serviceProvider.GetRequiredService<ASMILE.BO.Application.FrontOffice.HotelMasterData.HotelRoomMasterDataQueryHandlers>();
        _roomStatusWindow = new RoomStatusWindow(queries);
        _roomStatusWindow.Closed += (_, _) => _roomStatusWindow = null;
        _roomStatusWindow.Show();
    }

    private async Task OpenCheckInWindowAsync()
    {
        if (_checkInWindow is not null) { _checkInWindow.Activate(); return; }
        var workspace = _serviceProvider.GetRequiredService<ReservationWorkspaceViewModel>();
        workspace.SelectedStatus = Core.FrontOffice.Reservation.ReservationStatus.Confirmed;
        _checkInWindow = new CheckInWindow(workspace);
        _checkInWindow.Closed += (_, _) => _checkInWindow = null;
        _checkInWindow.Show();
    }

    private void OpenGroupCheckInWindow()
    {
        if (_groupCheckInWindow is not null) { _groupCheckInWindow.Activate(); return; }
        _groupCheckInWindow = new GroupCheckInWindow();
        _groupCheckInWindow.Closed += (_, _) => _groupCheckInWindow = null;
        _groupCheckInWindow.Show();
    }

    private void OpenReportCenterWindow()
    {
        if (_reportCenterWindow is not null) { _reportCenterWindow.Activate(); return; }
        _reportCenterWindow = new FrontOfficeReportCenterWindow(categoryFilter: "FrontOffice", sourceModule: "Lễ tân");
        _reportCenterWindow.Closed += (_, _) => _reportCenterWindow = null;
        _reportCenterWindow.Show();
    }

    private void OpenStayProfileWindow()
    {
        if (_stayProfileWindow is not null) { _stayProfileWindow.Activate(); return; }
        _stayProfileWindow = new StayProfileWindow();
        _stayProfileWindow.Closed += (_, _) => _stayProfileWindow = null;
        _stayProfileWindow.Show();
    }

    private void OpenCurrentGuestsWindow()
    {
        if (_currentGuestsWindow is not null) { _currentGuestsWindow.Activate(); return; }
        _currentGuestsWindow = new CurrentGuestsWindow();
        _currentGuestsWindow.Closed += (_, _) => _currentGuestsWindow = null;
        _currentGuestsWindow.Show();
    }

    private void OpenTodayArrivalsWindow()
    {
        if (_todayArrivalsWindow is not null) { _todayArrivalsWindow.Activate(); return; }
        _todayArrivalsWindow = new TodayArrivalsWindow();
        _todayArrivalsWindow.Closed += (_, _) => _todayArrivalsWindow = null;
        _todayArrivalsWindow.Show();
    }

    private void OpenTodayDeparturesWindow()
    {
        if (_todayDeparturesWindow is not null) { _todayDeparturesWindow.Activate(); return; }
        _todayDeparturesWindow = new TodayDeparturesWindow();
        _todayDeparturesWindow.Closed += (_, _) => _todayDeparturesWindow = null;
        _todayDeparturesWindow.Show();
    }

    private void OpenHousekeepingMessageWindow()
    {
        if (_housekeepingMessageWindow is not null) { _housekeepingMessageWindow.Activate(); return; }
        _housekeepingMessageWindow = new HousekeepingMessageWindow();
        _housekeepingMessageWindow.Closed += (_, _) => _housekeepingMessageWindow = null;
        _housekeepingMessageWindow.Show();
    }

    private void OpenExchangeRateWindow()
    {
        if (_exchangeRateWindow is not null) { _exchangeRateWindow.Activate(); return; }
        _exchangeRateWindow = new ExchangeRateWindow();
        _exchangeRateWindow.Closed += (_, _) => _exchangeRateWindow = null;
        _exchangeRateWindow.Show();
    }

    private async Task OpenWalkInWindowAsync()
    {
        if (_walkInWindow is not null) { _walkInWindow.Activate(); return; }
        _walkInWindow = new WalkInWindow(receptionUiService, _serviceProvider);
        _walkInWindow.Closed += (_, _) => _walkInWindow = null;
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
