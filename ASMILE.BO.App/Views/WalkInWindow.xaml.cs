using ASMILE.BO.App.Services;
using ASMILE.BO.Application.FrontOffice.Foundation.Contexts;
using ASMILE.BO.Application.FrontOffice.Guest;
using ASMILE.BO.Application.FrontOffice.HotelMasterData;
using ASMILE.BO.Application.FrontOffice.Reservation;
using ASMILE.BO.Application.FrontOffice.Stay;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace ASMILE.BO.App.Views;

public partial class WalkInWindow : Window
{
    private readonly IServiceProvider _sp;
    private readonly IServiceScope _serviceScope;
    private HotelRoomMasterDataQueryHandlers _hotelQueries = null!;
    private System.Collections.Generic.List<string> _allRoomTypes = new();
    private System.Collections.Generic.List<HotelRoomDto> _availableRooms = new();
    private Guid? _selectedGuestId;
    private string? _selectedGuestName;
    private int _currentStep;
    private static readonly string[] StepLabels = { "A", "B", "C", "D", "E" };

    public WalkInWindow(IReceptionUiService service, IServiceProvider sp)
    {
        InitializeComponent();
        Owner = System.Windows.Application.Current.MainWindow;
        _ = service;
        _serviceScope = sp.GetRequiredService<IServiceScopeFactory>().CreateScope();
        _sp = _serviceScope.ServiceProvider;
        _hotelQueries = _sp.GetRequiredService<HotelRoomMasterDataQueryHandlers>();
        Loaded += async (_, _) => await LoadRoomTypes();
        Closed += (_, _) => _serviceScope.Dispose();
        WireRealtimeUpdates();
        UpdateNavigation();
    }

    private void WireRealtimeUpdates()
    {
        GuestName.TextChanged += (_, _) =>
        {
            if (_selectedGuestName is not null && !string.Equals(GuestName.Text?.Trim(), _selectedGuestName, StringComparison.OrdinalIgnoreCase))
            {
                _selectedGuestId = null;
                _selectedGuestName = null;
            }
            UpdateSummary();
        };
        GuestPhone.TextChanged += (_, _) => UpdateSummary();
        ArrivalDate.SelectedDateChanged += (_, _) => UpdateSummary();
        DepartureDate.SelectedDateChanged += (_, _) => UpdateSummary();
        Adults.TextChanged += (_, _) => UpdateSummary();
        Children.TextChanged += (_, _) => UpdateSummary();
        RoomB.SelectionChanged += (_, _) => UpdateSummary();
        RoomTypeB.SelectionChanged += (_, _) => UpdateSummary();
    }

    private int CurrentStep
    {
        get => _currentStep;
        set { _currentStep = value; UpdateNavigation(); UpdateSummary(); }
    }

    private void UpdateNavigation()
    {
        var step = CurrentStep;
        StepLabel.Text = $"Bước {StepLabels[step]}/5";
        BackBtn.Visibility = step > 0 ? Visibility.Visible : Visibility.Collapsed;
        NextBtn.Visibility = step < 4 ? Visibility.Visible : Visibility.Collapsed;
        ExecuteBtn.Visibility = step == 4 ? Visibility.Visible : Visibility.Collapsed;
        StepAContent.Visibility = step == 0 ? Visibility.Visible : Visibility.Collapsed;
        StepGuestContent.Visibility = step == 1 ? Visibility.Visible : Visibility.Collapsed;
        StepBContent.Visibility = step == 2 ? Visibility.Visible : Visibility.Collapsed;
        StepCContent.Visibility = step == 3 ? Visibility.Visible : Visibility.Collapsed;
        StepDContent.Visibility = step == 4 ? Visibility.Visible : Visibility.Collapsed;
        if (step == 4) UpdateConfirmationSummary();
        UpdateStepBar(step);
    }

    private void UpdateStepBar(int step)
    {
        var steps = new[] { (StepScanBox, StepScanLabel), (StepABox, StepALabel), (StepBBox, StepBLabel), (StepCBox, StepCLabel), (StepDBox, StepDLabel) };
        for (int i = 0; i < 5; i++)
        {
            var (box, lbl) = steps[i];
            if (i < step) { box.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F0FDF4")); box.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#86EFAC")); box.BorderThickness = new Thickness(1.5); lbl.Text = "Hoàn tất"; lbl.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#166534")); }
            else if (i == step) { box.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EFF6FF")); box.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3B82F6")); box.BorderThickness = new Thickness(1.5); lbl.Text = "Đang nhập"; lbl.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1E40AF")); }
            else { box.Background = Brushes.White; box.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E2E8F0")); box.BorderThickness = new Thickness(1); lbl.Text = "Chưa bắt đầu"; lbl.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#64748B")); }
        }
    }

    private async System.Threading.Tasks.Task LoadRoomTypes()
    {
        ErrorText.Text = string.Empty;
        _availableRooms.Clear();
        _allRoomTypes.Clear();
        RoomTypeB.ItemsSource = null;
        RoomB.ItemsSource = null;
        try
        {
            var result = await _hotelQueries.SearchRooms(null, null, null, null, null, true, new(1, 500), "number", true, default);
            if (!result.IsSuccess || result.Value is null)
            {
                ErrorText.Text = "Không thể tải danh sách phòng thật: " + (result.Error?.Message ?? "kết quả rỗng.");
                return;
            }

            _availableRooms = result.Value.Items.ToList();
            _allRoomTypes = _availableRooms.Select(r => r.HotelRoomTypeName).Where(n => !string.IsNullOrWhiteSpace(n)).Distinct().OrderBy(n => n).ToList();
            RoomTypeB.ItemsSource = _allRoomTypes;
        }
        catch (Exception ex)
        {
            ErrorText.Text = "Không thể tải danh sách phòng thật: " + ex.Message;
        }
    }

    private void OnRoomTypeBChanged(object sender, SelectionChangedEventArgs e)
    {
        RoomB.SelectedIndex = -1;
        RoomB.ItemsSource = RoomTypeB.SelectedItem is string rt
            ? _availableRooms
                .Where(r => r.HotelRoomTypeName == rt)
                .Select(r => r.RoomNumber)
                .OrderBy(r => r)
                .ToList()
            : null;
    }

    private void UpdateConfirmationSummary()
    {
        ConfirmGuest.Text = GuestName.Text?.Trim() is { Length: > 0 } n ? n : "—";
        ConfirmPhone.Text = GuestPhone.Text?.Trim() is { Length: > 0 } p ? p : "—";
        var arr = ArrivalDate.SelectedDate; var dep = DepartureDate.SelectedDate;
        ConfirmStay.Text = arr.HasValue && dep.HasValue ? $"{arr:dd/MM/yyyy} → {dep:dd/MM/yyyy}" : "—";
        ConfirmNights.Text = arr.HasValue && dep.HasValue && dep > arr ? $"{(dep.Value - arr.Value).Days} đêm" : "—";
        ConfirmRoom.Text = RoomB.SelectedItem?.ToString() ?? "—";
        ConfirmRoomTypeB.Text = RoomTypeB.SelectedItem?.ToString() ?? "—";
        ConfirmDoc.Text = string.IsNullOrWhiteSpace(DocNumber.Text) ? "—" : DocNumber.Text;
        ConfirmPrice.Text = $"{RoomRate.Text} VND";
        // Update VAT label
        VatLabel.Text = VatToggle.IsChecked == true ? "Đã gồm VAT" : "Chưa gồm VAT";
        // Update selected room card in step B
        if (RoomB.SelectedItem is not null)
        {
            RoomDetailB.Text = $"{RoomB.SelectedItem}";
            RoomFloorB.Text = RoomTypeB.SelectedItem?.ToString() ?? "";
            SelectedRoomCard.Visibility = Visibility.Visible;
        }
    }

    private void UpdateSummary()
    {
        SumGuest.Text = string.IsNullOrWhiteSpace(GuestName.Text) ? "—" : GuestName.Text;
        SumPhone.Text = string.IsNullOrWhiteSpace(GuestPhone.Text) ? "—" : GuestPhone.Text;
        SumRoomType.Text = RoomTypeB.SelectedItem?.ToString() ?? "—";
        SumRoom.Text = RoomB.SelectedItem?.ToString() ?? "—";
        var arr = ArrivalDate.SelectedDate; var dep = DepartureDate.SelectedDate;
        if (arr.HasValue && dep.HasValue && dep.Value > arr.Value)
        {
            int nights = (dep.Value - arr.Value).Days;
            SumStay.Text = $"{arr:dd/MM/yyyy} → {dep:dd/MM/yyyy}";
            SumNights.Text = $"{nights} đêm";
            NightsDisplay.Text = $"Số đêm: {nights}";
        }
        else { SumStay.Text = "—"; SumNights.Text = "—"; NightsDisplay.Text = "Số đêm: —"; }
    }

    private bool ValidateGuest()
    {
        if (string.IsNullOrWhiteSpace(GuestName.Text)) { ErrorText.Text = "Vui lòng nhập tên khách."; return false; }
        var arr = ArrivalDate.SelectedDate; var dep = DepartureDate.SelectedDate;
        if (arr is null || dep is null || dep <= arr) { ErrorText.Text = "Ngày đi phải sau ngày đến."; return false; }
        ErrorText.Text = ""; return true;
    }

    private bool ValidateRoom()
    {
        if (RoomB.SelectedItem is null && RoomTypeB.SelectedItem is null)
        { ErrorText.Text = "Vui lòng chọn hạng phòng và số phòng."; return false; }
        ErrorText.Text = ""; return true;
    }

    private bool ValidatePricing()
    {
        if (string.IsNullOrWhiteSpace(RoomRate.Text) || !decimal.TryParse(RoomRate.Text, out var r) || r <= 0)
        { ErrorText.Text = "Vui lòng nhập giá phòng hợp lệ."; return false; }
        ErrorText.Text = ""; return true;
    }

    private void OnNextClick(object sender, RoutedEventArgs e)
    {
        bool valid = CurrentStep switch { 0 => true, 1 => ValidateGuest(), 2 => ValidateRoom(), 3 => ValidatePricing(), _ => true };
        if (!valid) return;
        if (CurrentStep < 4) CurrentStep++;
    }

    private void OnBackClick(object sender, RoutedEventArgs e) { if (CurrentStep > 0) CurrentStep--; }

    private async void OnExecuteClick(object sender, RoutedEventArgs e)
    {
        var name = GuestName.Text?.Trim();
        if (string.IsNullOrWhiteSpace(name)) { ErrorText.Text = "Vui lòng nhập tên khách."; return; }
        var arr = ArrivalDate.SelectedDate; var dep = DepartureDate.SelectedDate;
        if (arr is null || dep is null || dep <= arr) { ErrorText.Text = "Ngày đi phải sau ngày đến."; return; }
        ErrorText.Text = "";
        try
        {
            var resSvc = _sp.GetRequiredService<IReservationUiService>();
            int.TryParse(Adults.Text, out var a); int.TryParse(Children.Text, out var c);
            var selectedRoom = _availableRooms.FirstOrDefault(r => string.Equals(r.RoomNumber, RoomB.SelectedItem?.ToString(), StringComparison.OrdinalIgnoreCase));
            if (selectedRoom is null)
            {
                ErrorText.Text = "Vui lòng chọn một phòng trống có thể nhận khách.";
                return;
            }

            if (_selectedGuestId is null)
            {
                ErrorText.Text = "Vui lòng chọn hồ sơ khách thật bằng nút tìm khách trước khi thực hiện Walk-In.";
                return;
            }

            var result = await resSvc.CreateAsync(name, GuestPhone.Text?.Trim(), null, _selectedGuestId, DateOnly.FromDateTime(arr.Value), DateOnly.FromDateTime(dep.Value), a > 0 ? a : 1, c, selectedRoom.HotelRoomTypeId, "Walk-In", default);
            if (result.IsFailure) { ErrorText.Text = "Không thể tạo Walk-In. Vui lòng thử lại."; return; }

            var assigned = await resSvc.AssignAsync(result.Value!, selectedRoom.HotelRoomId, default);
            if (assigned.IsFailure)
            {
                ErrorText.Text = $"Đã tạo hồ sơ {result.Value!.ReservationNumber} nhưng chưa gán được phòng: {assigned.Error?.Message ?? "vui lòng kiểm tra lại."}";
                return;
            }

            var tentative = await resSvc.TentativeAsync(assigned.Value!, default);
            if (tentative.IsFailure)
            {
                ErrorText.Text = $"Đã tạo và gán phòng {assigned.Value!.ReservationNumber}, nhưng chưa chuyển được sang chờ xác nhận: {tentative.Error?.Message ?? "vui lòng kiểm tra lại."}";
                return;
            }

            var confirmed = await resSvc.ConfirmAsync(tentative.Value!, default);
            if (confirmed.IsFailure)
            {
                ErrorText.Text = $"Đã tạo và gán phòng {assigned.Value!.ReservationNumber}, nhưng chưa xác nhận được đặt phòng: {confirmed.Error?.Message ?? "vui lòng kiểm tra lại."}";
                return;
            }

            var stayService = _sp.GetRequiredService<HotelStayApplicationService>();
            var prepared = await stayService.PrepareFromReservationAsync(
                new PrepareStayFromReservationCommand(confirmed.Value!.ReservationId),
                default);
            if (prepared.IsFailure || prepared.Value is null)
            {
                ErrorText.Text = $"Đã xác nhận đặt phòng {confirmed.Value.ReservationNumber}, nhưng chưa tạo được lưu trú: {prepared.Error?.Message ?? "vui lòng kiểm tra lại."}";
                return;
            }

            var checkedIn = await stayService.CheckInAsync(
                new CheckInHotelStayCommand(prepared.Value.HotelStayId, prepared.Value.RowVersion ?? []),
                default);
            if (checkedIn.IsFailure || checkedIn.Value is null)
            {
                ErrorText.Text = $"Đã tạo lưu trú {prepared.Value.StayNumber}, nhưng chưa nhận phòng được: {checkedIn.Error?.Message ?? "vui lòng kiểm tra lại."}";
                return;
            }

            MessageBox.Show($"Walk-In thành công! {checkedIn.Value.StayNumber} - phòng {checkedIn.Value.RoomNumber}", "Walk-In", MessageBoxButton.OK, MessageBoxImage.Information);
            Close();
        }
        catch (Exception ex) { ErrorText.Text = "Không thể lưu hồ sơ Walk-In: " + ex.Message; }
    }

    private void OnCloseClick(object sender, RoutedEventArgs e) => Close();
    private void OnRefreshClick(object sender, RoutedEventArgs e) => _ = LoadRoomTypes();

    private void OnStepClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.Tag is string tag && int.TryParse(tag, out var step))
            CurrentStep = step;
    }

    private async void OnFindGuestClick(object sender, RoutedEventArgs e)
    {
        var picker = new AddStayOccupantWindow(primaryGuest: true) { Owner = this };
        if (picker.ShowDialog() != true || picker.SelectedGuestId == Guid.Empty)
            return;

        var repository = _sp.GetService<IGuestProfileRepository>();
        var context = _sp.GetService<IFrontOfficeExecutionContextAccessor>()?.Current;
        if (repository is null || context?.Property.PropertyId is not { } propertyId)
        {
            ErrorText.Text = "Chưa cấu hình Guest service hoặc property context dữ liệu thật.";
            return;
        }

        try
        {
            var profile = await repository.GetAsync(propertyId.Value, picker.SelectedGuestId, default);
            if (profile is null)
            {
                ErrorText.Text = "Không tìm thấy hồ sơ khách đã chọn trong dữ liệu thật.";
                return;
            }

            _selectedGuestId = profile.GuestProfileId;
            _selectedGuestName = profile.FullName;
            GuestName.Text = profile.FullName;
            GuestPhone.Text = profile.Phone ?? "";
            var document = profile.Documents.FirstOrDefault(x => x.DocumentType == Core.FrontOffice.Guest.GuestDocumentType.Passport)
                           ?? profile.Documents.FirstOrDefault(x => x.DocumentType == Core.FrontOffice.Guest.GuestDocumentType.NationalId);
            if (document is not null) DocNumber.Text = document.DocumentNumber;
            ErrorText.Text = "";
            UpdateSummary();
        }
        catch (Exception ex)
        {
            ErrorText.Text = "Không tải được hồ sơ khách thật: " + ex.Message;
        }
    }

    private void OnCreateGuestClick(object sender, RoutedEventArgs e)
    {
        var creator = new CreateGuestProfileWindow { Owner = this };
        if (creator.ShowDialog() != true || creator.CreatedGuestId == Guid.Empty)
            return;

        _selectedGuestId = creator.CreatedGuestId;
        _selectedGuestName = creator.CreatedGuestName;
        GuestName.Text = creator.CreatedGuestName;
        ErrorText.Text = "Đã lưu và chọn hồ sơ khách thật: " + creator.CreatedGuestName;
        UpdateSummary();
    }

    private void OnClearScanClick(object sender, RoutedEventArgs e)
    {
        ScanFullName.Text = ""; ScanDob.Text = ""; ScanDocNumber.Text = "";
        ScanIssueDate.Text = ""; ScanIssuePlace.Text = ""; ScanBirthPlace.Text = ""; ScanAddress.Text = "";
        ScanStatus.Text = "Chưa quét";
    }

    private void OnApplyScanClick(object sender, RoutedEventArgs e)
    {
        // Copy scan data to guest form in Step B
        if (!string.IsNullOrWhiteSpace(ScanFullName.Text)) GuestName.Text = ScanFullName.Text;
        if (!string.IsNullOrWhiteSpace(ScanDocNumber.Text)) DocNumber.Text = ScanDocNumber.Text;
        ScanStatus.Text = "Đã áp dụng";
        ErrorText.Text = "";
        CurrentStep = 1; // Move to guest step
    }

    private void OnSkipScanClick(object sender, RoutedEventArgs e)
    {
        ScanStatus.Text = "Bỏ qua";
        CurrentStep = 1; // Move to guest step
    }
}
