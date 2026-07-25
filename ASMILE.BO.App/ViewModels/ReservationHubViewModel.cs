using System.Collections.ObjectModel;
using ASMILE.BO.App.Models;
using ASMILE.BO.App.Services;
using ASMILE.BO.App.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;

namespace ASMILE.BO.App.ViewModels;

/// <summary>
/// Front Office command center. It only maps UI cards to modal workspaces and owns no reservation business operation.
/// </summary>
public sealed partial class ReservationHubViewModel : ViewModelBase, IDisposable
{
    private readonly IReservationHubPermissionReader permissions;
    private readonly IFrontOfficePropertyContextHost propertyContextHost;
    private readonly INavigationService? navigationService;
    private readonly IServiceProvider serviceProvider;

    public ReservationHubViewModel(
        IReservationHubPermissionReader permissions,
        IFrontOfficePropertyContextHost propertyContextHost,
        IReservationWorkspaceModalService modalService,
        IServiceProvider serviceProvider,
        INavigationService? navigationService = null)
    {
        this.permissions = permissions;
        this.propertyContextHost = propertyContextHost;
        this.navigationService = navigationService;
        this.serviceProvider = serviceProvider;
        ModalService = modalService;
        propertyContextHost.PropertyChanged += OnPropertyContextChanged;
        BuildCards();
    }

    public ObservableCollection<ReservationHubCard> Cards { get; } = new();
    public IReservationWorkspaceModalService ModalService { get; }
    public ViewModelBase? ActiveWorkspace { get; private set; }
    public bool HasActiveWorkspace => ActiveWorkspace is not null;
    public Func<Task>? ReturnToParentAsync { get; set; }
    [ObservableProperty] private string errorMessage = string.Empty;
    [ObservableProperty] private string infoMessage = string.Empty;

    public bool HasPropertyContext => propertyContextHost.HasCurrentProperty && permissions.HasContext;
    public string CurrentPropertyDisplay => propertyContextHost.CurrentProperty?.DisplayName ?? "Chưa chọn chi nhánh";
    public string BusinessDateDisplay => DateTime.Today.ToString("dddd, dd/MM/yyyy", new System.Globalization.CultureInfo("vi-VN"));
    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);
    public bool HasInfo => !string.IsNullOrWhiteSpace(InfoMessage);

    [RelayCommand]
    private void Refresh()
    {
        ErrorMessage = string.Empty;
        InfoMessage = "Đã cập nhật quyền và ngữ cảnh Front Office.";
        BuildCards();
        NotifyContextState();
    }

    [RelayCommand]
    private Task OpenNew() => permissions.CanCreate && HasPropertyContext
        ? OpenModalAsync(ReservationModalKind.New, "new")
        : Task.CompletedTask;

    [RelayCommand]
    private Task OpenSearch() => permissions.CanView && HasPropertyContext
        ? OpenSearchWorkspaceAsync()
        : Task.CompletedTask;

    private SearchReservationWindow? _searchWindow;
    private CreateReservationPopup? _editPopup;
    private RoomAssignmentWindow? _assignWindow;
    private ConfirmReservationWindow? _confirmWindow;
    private FutureAvailabilityWindow? _availWindow;
    private ReservationReportsWindow? _reportsWindow;

    private DeniedReservationWindow? _deniedWindow;

    private CancelReservationWindow? _cancelWindow;
    private WebReservationWindow? _webWindow;

    private ReservationStatusWindow? _statusWindow;

    private async Task OpenStatusWindowAsync()
    {
        ErrorMessage = string.Empty;
        try
        {
            if (_statusWindow is not null) { _statusWindow.Activate(); return; }
            var workspace = serviceProvider.GetRequiredService<ReservationWorkspaceViewModel>();
            _statusWindow = new ReservationStatusWindow(workspace);
            _statusWindow.Closed += (_, _) => _statusWindow = null;
            _statusWindow.Show();
            await workspace.RefreshAsync();
        }
        catch (Exception ex) { ErrorMessage = $"Không thể mở: {ex.Message}"; _statusWindow = null; }
    }
    private void OpenWebWindow()
    {
        if (_webWindow is not null) { _webWindow.Activate(); return; }
        _webWindow = new WebReservationWindow();
        _webWindow.Closed += (_, _) => _webWindow = null;
        _webWindow.Show();
    }

    private void OpenDeniedWindow()
    {
        if (_deniedWindow is not null) { _deniedWindow.Activate(); return; }
        _deniedWindow = new DeniedReservationWindow();
        _deniedWindow.Closed += (_, _) => _deniedWindow = null;
        _deniedWindow.Show();
    }

    private void OpenAvailabilityWindow()
    {
        if (_availWindow is not null) { _availWindow.Activate(); return; }
        _availWindow = new FutureAvailabilityWindow();
        _availWindow.Closed += (_, _) => _availWindow = null;
        _availWindow.Show();
    }

    private void OpenReportsWindow()
    {
        if (_reportsWindow is not null) { _reportsWindow.Activate(); return; }
        _reportsWindow = new ReservationReportsWindow();
        _reportsWindow.Closed += (_, _) => _reportsWindow = null;
        _reportsWindow.Show();
    }


    private async Task OpenCancelWindowAsync()
    {
        ErrorMessage = string.Empty;
        try
        {
            if (_cancelWindow is not null) { _cancelWindow.Activate(); return; }
            var workspace = serviceProvider.GetRequiredService<ReservationWorkspaceViewModel>();
            workspace.SelectedStatus = Core.FrontOffice.Reservation.ReservationStatus.Confirmed;
            _cancelWindow = new CancelReservationWindow(workspace);
            _cancelWindow.Closed += (_, _) => _cancelWindow = null;
            _cancelWindow.Show();
            await workspace.RefreshAsync();
        }
        catch (Exception ex) { ErrorMessage = $"Không thể mở: {ex.Message}"; _cancelWindow = null; }
    }

    private async Task OpenConfirmWindowAsync()
    {
        ErrorMessage = string.Empty;
        try
        {
            if (_confirmWindow is not null) { _confirmWindow.Activate(); return; }
            var workspace = serviceProvider.GetRequiredService<ReservationWorkspaceViewModel>();
            workspace.SelectedStatus = Core.FrontOffice.Reservation.ReservationStatus.Draft;
            _confirmWindow = new ConfirmReservationWindow(workspace);
            _confirmWindow.Closed += (_, _) => _confirmWindow = null;
            _confirmWindow.Show();
            await workspace.RefreshAsync();
        }
        catch (Exception ex) { ErrorMessage = $"Không thể mở: {ex.Message}"; _confirmWindow = null; }
    }

    private async Task OpenAssignRoomFlowAsync()
    {
        ErrorMessage = string.Empty;
        try
        {
            if (_assignWindow is not null) { _assignWindow.Activate(); return; }
            if (_searchWindow is not null) { _searchWindow.Activate(); return; }

            var workspace = serviceProvider.GetRequiredService<ReservationWorkspaceViewModel>();
            _searchWindow = new SearchReservationWindow(workspace);
            _searchWindow.Closed += (_, _) => _searchWindow = null;
            _searchWindow.Title = "Chọn đặt phòng cần phân phòng";

            void OnSelectionChanged(object? s, System.ComponentModel.PropertyChangedEventArgs e)
            {
                if (e.PropertyName == nameof(workspace.SelectedReservation) && workspace.SelectedReservation is not null)
                {
                    workspace.PropertyChanged -= OnSelectionChanged;
                    _searchWindow?.Close();
                    _searchWindow = null;
                    _ = OpenAssignWindowAsync(workspace);
                }
            }
            workspace.PropertyChanged += OnSelectionChanged;
            _searchWindow.Closed += (_, _) => workspace.PropertyChanged -= OnSelectionChanged;
            _searchWindow.Show();
            await workspace.RefreshAsync();
        }
        catch (Exception ex) { ErrorMessage = $"Không thể mở: {ex.Message}"; }
    }

    private async Task OpenAssignWindowAsync(ReservationWorkspaceViewModel workspace)
    {
        try
        {
            _assignWindow = new RoomAssignmentWindow(workspace);
            _assignWindow.Closed += (_, _) => _assignWindow = null;
            _assignWindow.Show();
            await workspace.OpenRoomPanelAsync();
        }
        catch (Exception ex) { ErrorMessage = $"Không thể mở phân phòng: {ex.Message}"; _assignWindow = null; }
    }

    private async Task OpenEditFlowAsync()
    {
        ErrorMessage = string.Empty;
        try
        {
            if (_editPopup is not null) { _editPopup.Activate(); return; }
            if (_searchWindow is not null) { _searchWindow.Activate(); return; }

            var workspace = serviceProvider.GetRequiredService<ReservationWorkspaceViewModel>();
            _searchWindow = new SearchReservationWindow(workspace);
            _searchWindow.Closed += (_, _) => _searchWindow = null;
            _searchWindow.Title = "Chọn đặt phòng cần sửa";

            // Hook double-click to open edit wizard
            void OnSelectionChanged(object? s, System.ComponentModel.PropertyChangedEventArgs e)
            {
                if (e.PropertyName == nameof(workspace.SelectedReservation) && workspace.SelectedReservation is not null)
                {
                    var r = workspace.SelectedReservation;
                    workspace.PropertyChanged -= OnSelectionChanged;
                    _searchWindow?.Close();
                    _searchWindow = null;
                    _ = OpenEditWizardAsync(r.ReservationId, r.ReservationNumber);
                }
            }
            workspace.PropertyChanged += OnSelectionChanged;

            _searchWindow.Closed += (_, _) => workspace.PropertyChanged -= OnSelectionChanged;
            _searchWindow.Show();
            await workspace.RefreshAsync();
        }
        catch (Exception ex) { ErrorMessage = $"Không thể mở: {ex.Message}"; }
    }

    private async Task OpenEditWizardAsync(Guid reservationId, string reservationNumber)
    {
        try
        {
            var workspace = serviceProvider.GetRequiredService<ReservationWorkspaceViewModel>();
            var modal = new ReservationModalWorkspaceViewModel(workspace, ReservationModalKind.Edit, () => _editPopup?.Close());
            var popup = new CreateReservationPopup(modal);
            _editPopup = popup;
            _editPopup.Closed += (_, _) => _editPopup = null;
            popup.Wizard.SetEditMode(reservationId, reservationNumber);
            _editPopup.ShowDialog();
        }
        catch (Exception ex) { ErrorMessage = $"Không thể mở: {ex.Message}"; _editPopup = null; }
    }

    private async Task OpenSearchWorkspaceAsync()
    {
        ErrorMessage = string.Empty;
        try
        {
            if (_searchWindow is not null)
            {
                if (_searchWindow.WindowState == System.Windows.WindowState.Minimized)
                    _searchWindow.WindowState = System.Windows.WindowState.Normal;
                _searchWindow.Activate();
                return;
            }
            var workspace = serviceProvider.GetRequiredService<ReservationWorkspaceViewModel>();
            _searchWindow = new SearchReservationWindow(workspace);
            _searchWindow.Closed += (_, _) => _searchWindow = null;
            _searchWindow.Show();
            await workspace.RefreshAsync();
        }
        catch (Exception ex)
        {
            _searchWindow = null;
            ErrorMessage = $"Không thể mở Tìm đặt phòng: {ex.Message}";
        }
    }

    [RelayCommand]
    private void CloseWorkspace()
    {
        ActiveWorkspace = null;
        OnPropertyChanged(nameof(ActiveWorkspace));
        OnPropertyChanged(nameof(HasActiveWorkspace));
    }

    [RelayCommand]
    private async Task BackAsync()
    {
        ModalService.Close();
        if (ReturnToParentAsync is not null)
        {
            await ReturnToParentAsync();
            return;
        }

        if (navigationService is null) return;

        var result = await navigationService.GoBackAsync();
        if (!result.Success)
        {
            await navigationService.OpenModuleAsync("front-office");
        }
    }

    private void BuildCards()
    {
        Cards.Clear();
        var hasContext = HasPropertyContext;
        Add("new", "Đặt phòng mới", "Tạo yêu cầu lưu trú mới cho khách.", "\uE710", "Ctrl+N", 10, permissions.CanCreate && hasContext, ReservationModalKind.New, "FrontOffice.Reservation.Create");
        Add("search", "Tìm đặt phòng", "Tra cứu theo số đặt phòng, khách hoặc ngày ở.", "\uE721", "Ctrl+F", 20, permissions.CanView && hasContext, ReservationModalKind.Search, "FrontOffice.Reservation.View");
        Add("edit", "Sửa đặt phòng", "Tìm một đặt phòng để cập nhật thông tin.", "\uE70F", "", 30, permissions.CanUpdate && hasContext, ReservationModalKind.Search, "FrontOffice.Reservation.Update");
        Add("assign", "Phân phòng", "Phân hoặc đổi phòng theo loại phòng và thời gian ở.", "\uE8B7", "", 40, permissions.CanAssignRoom && hasContext, ReservationModalKind.AssignRoom, "FrontOffice.Reservation.AssignRoom");
        Add("confirm", "Xác nhận đặt phòng", "Xác nhận các đặt phòng nháp hoặc tạm giữ.", "\uE8FB", "", 50, permissions.CanChangeStatus && hasContext, ReservationModalKind.Confirm, "FrontOffice.Reservation.ChangeStatus");
        Add("cancel", "Hủy đặt phòng", "Hủy đặt phòng theo quyền và trạng thái hiện có.", "\uE711", "", 60, permissions.CanCancel && hasContext, ReservationModalKind.Cancel, "FrontOffice.Reservation.Cancel");
        Add("status", "Tình trạng đặt phòng", "Xem nhanh danh sách theo trạng thái.", "\uE9D2", "", 70, permissions.CanView && hasContext, ReservationModalKind.Status, "FrontOffice.Reservation.View");
        AddComingSoon("web", "Đặt phòng web", "Kênh đặt phòng trực tuyến sẽ được kết nối sau.", "\uE774", 80);
        AddComingSoon("denied", "Theo dõi từ chối", "Theo dõi yêu cầu không thể đáp ứng sẽ có ở phiên bản sau.", "\uE72E", 90);
        AddComingSoon("availability", "Khả năng phòng tương lai", "Chưa triển khai Availability Engine.", "\uE81C", 100);
        AddComingSoon("reports", "Báo cáo đặt phòng", "Báo cáo chuyên biệt sẽ được bổ sung sau.", "\uE9D2", 110);
        var wc = Cards.FirstOrDefault(c => c.Id == "web");
        if (wc is not null) wc.Command = new RelayCommand(OpenWebWindow);
        var dc = Cards.FirstOrDefault(c => c.Id == "denied");
        if (dc is not null) dc.Command = new RelayCommand(OpenDeniedWindow);
        var ac = Cards.FirstOrDefault(c => c.Id == "availability");
        if (ac is not null) ac.Command = new RelayCommand(OpenAvailabilityWindow);
        var rc = Cards.FirstOrDefault(c => c.Id == "reports");
        if (rc is not null) rc.Command = new RelayCommand(OpenReportsWindow);
    }

    private void Add(string id, string title, string description, string glyph, string shortcut, int order, bool allowed, ReservationModalKind modalKind, string permissionCode)
    {
        // Cards without permission are hidden, while backend authorization remains authoritative.
        Cards.Add(new ReservationHubCard
        {
            Id = id,
            Title = title,
            Description = description,
            IconGlyph = glyph,
            Shortcut = shortcut,
            DisplayOrder = order,
            IsVisible = allowed,
            IsEnabled = allowed,
            DisabledReason = allowed ? null : $"Yêu cầu quyền {permissionCode} và chi nhánh Front Office đang hoạt động.",
            Command = new AsyncRelayCommand(() => OpenModalAsync(modalKind, id))
        });
    }

    private void AddComingSoon(string id, string title, string description, string glyph, int order)
    {
        Cards.Add(new ReservationHubCard
        {
            Id = id,
            Title = title,
            Description = description,
            IconGlyph = glyph,
            Shortcut = string.Empty,
            DisplayOrder = order,
            IsVisible = HasPropertyContext,
            IsEnabled = false,
            IsComingSoon = true,
            BadgeText = "Sắp có",
            DisabledReason = "Chức năng này chưa có backend; hệ thống không tạo dữ liệu mô phỏng.",
            Command = new RelayCommand(() => { })
        });
    }

    private async Task OpenModalAsync(ReservationModalKind kind, string sourceCardId)
    {
        ErrorMessage = string.Empty;
        InfoMessage = string.Empty;
        try
        {
            if (kind == ReservationModalKind.New)
            {
                var workspace = serviceProvider.GetRequiredService<ReservationWorkspaceViewModel>();
                var popupRef = null as CreateReservationPopup;
                var modal = new ReservationModalWorkspaceViewModel(workspace, kind, () => popupRef?.Close());
                var popup = new CreateReservationPopup(modal);
                popupRef = popup;
                popup.ShowDialog();
                return;
            }
            if (kind == ReservationModalKind.Search)
            {
                await OpenSearchWorkspaceAsync();
                return;
            }
            if (kind == ReservationModalKind.Edit)
            {
                await OpenEditFlowAsync();
                return;
            }
            if (kind == ReservationModalKind.AssignRoom)
            {
                await OpenAssignRoomFlowAsync();
                return;
            }
            if (kind == ReservationModalKind.Confirm)
            {
                await OpenConfirmWindowAsync();
                return;
            }
            if (kind == ReservationModalKind.Cancel)
            {
                await OpenCancelWindowAsync();
                return;
            }
            if (kind == ReservationModalKind.Status)
            {
                await OpenStatusWindowAsync();
                return;
            }
            await ModalService.OpenAsync(kind, sourceCardId);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Không thể mở: {ex.Message}";
        }
    }

    partial void OnErrorMessageChanged(string value) => OnPropertyChanged(nameof(HasError));
    partial void OnInfoMessageChanged(string value) => OnPropertyChanged(nameof(HasInfo));

    private void OnPropertyContextChanged(object? sender, FrontOfficePropertyContextChangedEventArgs e)
    {
        ModalService.Close();
        ErrorMessage = e.Current is null
            ? "Vui lòng chọn chi nhánh trước khi sử dụng Đặt phòng."
            : string.Empty;
        InfoMessage = e.Current is null ? string.Empty : "Đã chuyển ngữ cảnh chi nhánh Front Office.";
        BuildCards();
        NotifyContextState();
    }

    private void NotifyContextState()
    {
        OnPropertyChanged(nameof(HasPropertyContext));
        OnPropertyChanged(nameof(CurrentPropertyDisplay));
        OnPropertyChanged(nameof(BusinessDateDisplay));
        OnPropertyChanged(nameof(HasError));
        OnPropertyChanged(nameof(HasInfo));
    }

    public void Dispose()
    {
        propertyContextHost.PropertyChanged -= OnPropertyContextChanged;
        ModalService.Dispose();
        permissions.Dispose();
    }
}
