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
    private readonly IServiceProvider? serviceProvider;
    private IServiceScope? _createReservationScope;

    public ReservationHubViewModel(
        IReservationHubPermissionReader permissions,
        IFrontOfficePropertyContextHost propertyContextHost,
        IReservationWorkspaceModalService modalService,
        IServiceProvider? serviceProvider,
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
    private CreateReservationPopup? _createPopup;
    private CreateReservationPopup? _editPopup;
    private RoomAssignmentWindow? _assignWindow;
    private ConfirmReservationWindow? _confirmWindow;
    private FutureAvailabilityWindow? _availWindow;
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
            if (serviceProvider is null) { ErrorMessage = "Dịch vụ đặt phòng chưa được cấu hình."; return; }
            var workspace = serviceProvider.GetRequiredService<ReservationWorkspaceViewModel>();
            _statusWindow = new ReservationStatusWindow(workspace);
            _statusWindow.Closed += (_, _) => _statusWindow = null;
            _statusWindow.Show();
            await workspace.RefreshAsync();
        }
        catch (Exception ex) { ErrorMessage = $"Không thể mở: {ex.Message}"; _statusWindow = null; }
    }
    private async Task OpenWebWindowAsync()
    {
        if (_webWindow is not null) { _webWindow.Activate(); return; }
        if (serviceProvider is null) { ErrorMessage = "Dịch vụ đặt phòng chưa được cấu hình."; return; }
        var workspace = serviceProvider.GetRequiredService<ReservationWorkspaceViewModel>();
        workspace.FilterSource = "Online";
        _webWindow = new WebReservationWindow(workspace);
        _webWindow.Closed += (_, _) => { _webWindow = null; workspace.Dispose(); };
        _webWindow.Show();
        await workspace.RefreshAsync();
    }

    private async Task OpenDeniedWindowAsync()
    {
        if (_deniedWindow is not null) { _deniedWindow.Activate(); return; }
        if (serviceProvider is null) { ErrorMessage = "Dịch vụ theo dõi từ chối chưa được cấu hình."; return; }
        var viewModel = serviceProvider.GetRequiredService<DeniedReservationViewModel>();
        _deniedWindow = new DeniedReservationWindow(viewModel);
        _deniedWindow.Closed += (_, _) => _deniedWindow = null;
        _deniedWindow.Show();
        await viewModel.RefreshAsync();
    }

    private void OpenAvailabilityWindow()
    {
        if (_availWindow is not null) { _availWindow.Activate(); return; }
        _availWindow = new FutureAvailabilityWindow();
        _availWindow.Closed += (_, _) => _availWindow = null;
        _availWindow.Show();
    }

    private async Task OpenCancelWindowAsync()
    {
        ErrorMessage = string.Empty;
        try
        {
            if (_cancelWindow is not null) { _cancelWindow.Activate(); return; }
            if (serviceProvider is null) { ErrorMessage = "Dịch vụ đặt phòng chưa được cấu hình."; return; }
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
            if (serviceProvider is null) { ErrorMessage = "Dịch vụ đặt phòng chưa được cấu hình."; return; }
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
            if (serviceProvider is null) { ErrorMessage = "Dịch vụ phân phòng chưa được cấu hình."; return; }

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
            if (serviceProvider is null) { ErrorMessage = "Dịch vụ đặt phòng chưa được cấu hình."; return; }

            var workspace = serviceProvider.GetRequiredService<ReservationWorkspaceViewModel>();
            _searchWindow = new SearchReservationWindow(workspace)
            {
                EditAction = async reservation =>
                {
                    await OpenEditWizardAsync(reservation.ReservationId, reservation.ReservationNumber, workspace);
                }
            };
            _searchWindow.Closed += (_, _) => _searchWindow = null;
            _searchWindow.Title = "Chọn đặt phòng cần sửa";
            _searchWindow.Show();
            await workspace.RefreshAsync();
        }
        catch (Exception ex) { ErrorMessage = $"Không thể mở: {ex.Message}"; }
    }

    private async Task OpenEditWizardAsync(
        Guid reservationId,
        string reservationNumber,
        ReservationWorkspaceViewModel? workspace = null)
    {
        try
        {
            workspace ??= serviceProvider?.GetRequiredService<ReservationWorkspaceViewModel>();
            if (workspace is null) { ErrorMessage = "Dịch vụ đặt phòng chưa được cấu hình."; return; }
            if (!await workspace.OpenForEditAsync(reservationId))
            {
                ErrorMessage = workspace.ErrorMessage;
                throw new InvalidOperationException(string.IsNullOrWhiteSpace(ErrorMessage)
                    ? "Không tải được đặt phòng đã chọn để sửa."
                    : ErrorMessage);
            }
            var modal = new ReservationModalWorkspaceViewModel(workspace, ReservationModalKind.Edit, () => _editPopup?.Close());
            var popup = new CreateReservationPopup(modal)
            {
                Owner = _searchWindow ?? System.Windows.Application.Current?.MainWindow,
                WindowStartupLocation = System.Windows.WindowStartupLocation.CenterOwner
            };
            _editPopup = popup;
            _editPopup.Closed += (_, _) => _editPopup = null;
            popup.Wizard.SetEditMode(reservationId, reservationNumber);
            _editPopup.ShowDialog();
            await workspace.RefreshWithStatusAsync(workspace.SelectedStatus, reservationId);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Không thể mở màn hình sửa đặt phòng: {ex.Message}";
            _editPopup = null;
            throw new InvalidOperationException(ErrorMessage, ex);
        }
    }

    private async Task OpenSearchWorkspaceAsync()
    {
        ErrorMessage = string.Empty;
        try
        {
            if (_searchWindow is not null)
            {
                var existingSearchWindow = _searchWindow;
                existingSearchWindow.EditAction ??= reservation => OpenEditWizardAsync(
                    reservation.ReservationId,
                    reservation.ReservationNumber,
                    existingSearchWindow.Workspace);
                if (_searchWindow.WindowState == System.Windows.WindowState.Minimized)
                    _searchWindow.WindowState = System.Windows.WindowState.Normal;
                _searchWindow.Activate();
                return;
            }
            if (serviceProvider is null)
            {
                await ModalService.OpenAsync(ReservationModalKind.Search, "search");
                return;
            }
            var workspace = serviceProvider.GetRequiredService<ReservationWorkspaceViewModel>();
            _searchWindow = new SearchReservationWindow(workspace)
            {
                EditAction = reservation => OpenEditWizardAsync(
                    reservation.ReservationId,
                    reservation.ReservationNumber,
                    workspace)
            };
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
        Add("edit", "Sửa đặt phòng", "Chọn một đặt phòng để cập nhật thông tin.", "\uE70F", "", 20, permissions.CanUpdate && hasContext, ReservationModalKind.Search, "FrontOffice.Reservation.Update");
        Add("assign", "Phân phòng", "Phân hoặc đổi phòng theo loại phòng và thời gian ở.", "\uE8B7", "", 40, permissions.CanAssignRoom && hasContext, ReservationModalKind.AssignRoom, "FrontOffice.Reservation.AssignRoom");
        Add("confirm", "Xác nhận đặt phòng", "Xác nhận các đặt phòng nháp hoặc tạm giữ.", "\uE8FB", "", 50, permissions.CanChangeStatus && hasContext, ReservationModalKind.Confirm, "FrontOffice.Reservation.ChangeStatus");
        Add("cancel", "Hủy đặt phòng", "Hủy đặt phòng theo quyền và trạng thái hiện có.", "\uE711", "", 60, permissions.CanCancel && hasContext, ReservationModalKind.Cancel, "FrontOffice.Reservation.Cancel");
        Add("status", "Tình trạng đặt phòng", "Xem nhanh danh sách theo trạng thái.", "\uE9D2", "", 70, permissions.CanView && hasContext, ReservationModalKind.Status, "FrontOffice.Reservation.View");
        AddWindowCard("web", "Đặt phòng web", "Tra cứu và xử lý đặt phòng từ Website/OTA.", "\uE774", 80, () => _ = OpenWebWindowAsync());
        AddWindowCard("denied", "Theo dõi từ chối", "Ghi nhận và theo dõi các yêu cầu đặt phòng không thành công.", "\uE72E", 90, () => _ = OpenDeniedWindowAsync());
        AddWindowCard("availability", "Khả năng phòng tương lai", "Theo dõi tồn phòng và khả năng bán theo ngày.", "\uE81C", 100, OpenAvailabilityWindow);
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
            BadgeText = "Đang hoạt động",
            DisabledReason = allowed ? null : $"Yêu cầu quyền {permissionCode} và chi nhánh Front Office đang hoạt động.",
            Command = new AsyncRelayCommand(() => OpenModalAsync(modalKind, id))
        });
    }

    private void AddWindowCard(string id, string title, string description, string glyph, int order, Action open)
    {
        var allowed = permissions.CanView && HasPropertyContext;
        Cards.Add(new ReservationHubCard
        {
            Id = id,
            Title = title,
            Description = description,
            IconGlyph = glyph,
            Shortcut = string.Empty,
            DisplayOrder = order,
            IsVisible = allowed,
            IsEnabled = allowed,
            BadgeText = "Đang hoạt động",
            DisabledReason = allowed ? null : "Yêu cầu quyền Front Office.Reservation.View và chi nhánh đang hoạt động.",
            Command = new RelayCommand(open)
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
                await OpenNewReservationPopupAsync();
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

    private async Task OpenNewReservationPopupAsync()
    {
        if (_createPopup is not null)
        {
            _createPopup.Activate();
            return;
        }

        var provider = serviceProvider;
        if (provider is null)
        {
            await ModalService.OpenAsync(ReservationModalKind.New, "new");
            return;
        }

        try
        {
            _createReservationScope = provider.CreateScope();
            var workspace = _createReservationScope.ServiceProvider.GetRequiredService<ReservationWorkspaceViewModel>();
            var modal = new ReservationModalWorkspaceViewModel(workspace, ReservationModalKind.New, () => _createPopup?.Close());
            var popup = new CreateReservationPopup(modal);
            _createPopup = popup;
            popup.Closed += (_, _) =>
            {
                _createPopup = null;
                _createReservationScope?.Dispose();
                _createReservationScope = null;
            };

            // Show the wizard immediately. Room types are loaded after the
            // window is visible so a database delay cannot hide the modal.
            popup.ContentRendered += async (_, _) =>
            {
                try
                {
                    await modal.InitializeAsync();
                }
                catch (Exception ex)
                {
                    workspace.ErrorMessage = $"Không thể tải dữ liệu phòng: {ex.Message}";
                }
            };
            popup.ShowDialog();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Không thể mở: {ex.Message}";
            _createPopup = null;
            _createReservationScope?.Dispose();
            _createReservationScope = null;
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
        _createPopup?.Close();
        _createPopup = null;
        _createReservationScope?.Dispose();
        _createReservationScope = null;
        ModalService.Dispose();
        permissions.Dispose();
    }
}
