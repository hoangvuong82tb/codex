using ASMILE.BO.App.Controls;
using ASMILE.BO.App.ViewModels;
using ASMILE.BO.Application.FrontOffice.Foundation.Contexts;
using ASMILE.BO.Application.FrontOffice.Group;
using ASMILE.BO.Core.FrontOffice.Group;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Extensions.DependencyInjection;

namespace ASMILE.BO.App.Views;

public partial class GroupCheckInWindow : Window
{
    private int _step;
    private readonly Panel[] _panels;
    private readonly Border[] _stepBoxes;
    private readonly TextBlock[] _labels;
    private readonly TablePagerViewModel<CheckInRow> _groupPager = new() { ItemName = "đoàn", SelectedPageSize = "20 / trang" };
    private readonly TablePagerViewModel<CheckInRow> _roomPager = new() { ItemName = "phòng", SelectedPageSize = "20 / trang" };
    private readonly IGroupReservationQueryService? _groupQuery;
    private readonly IGroupCheckInCommandService? _groupCheckIn;
    private readonly IFrontOfficeExecutionContextAccessor? _contextAccessor;
    private readonly IServiceScope? _serviceScope;
    private readonly SemaphoreSlim _dataOperationGate = new(1, 1);
    private IReadOnlyList<GroupReservationSummaryDto> _groups = [];
    private GroupReservationSummaryDto? _selectedGroup;
    private TextBlock? _groupPagerSummary;
    private TextBlock? _roomPagerSummary;
    private static readonly string[] StepLetters = { "A", "B", "C", "D", "E", "F" };

    public GroupCheckInWindow()
    {
        InitializeComponent();
        Owner = System.Windows.Application.Current.MainWindow;
        var applicationServices = (System.Windows.Application.Current as App)?.Services;
        // Keep this workflow on its own scoped EF graph. Group check-in can
        // coexist with checkout/rooming windows and must not share their
        // AppDbContext instance.
        _serviceScope = applicationServices?.CreateScope();
        var services = _serviceScope?.ServiceProvider;
        _groupQuery = services?.GetService(typeof(IGroupReservationQueryService)) as IGroupReservationQueryService;
        _groupCheckIn = services?.GetService(typeof(IGroupCheckInCommandService)) as IGroupCheckInCommandService;
        _contextAccessor = services?.GetService(typeof(IFrontOfficeExecutionContextAccessor)) as IFrontOfficeExecutionContextAccessor;
        GroupGrid.ItemsSource = _groupPager.Items;
        RoomGrid.ItemsSource = _roomPager.Items;
        PanelA.Children.Add(CreatePagerHost(_groupPager, out _groupPagerSummary));
        PanelB.Children.Add(CreatePagerHost(_roomPager, out _roomPagerSummary));
        _panels = new[] { PanelA, PanelB, PanelC, PanelD, PanelE, PanelF };
        _stepBoxes = new[] { Step0, Step1, Step2, Step3, Step4, Step5 };
        _labels = new[] { Lbl0, Lbl1, Lbl2, Lbl3, Lbl4, Lbl5 };
        Loaded += async (_, _) => await LoadDataAsync();
        Closed += async (_, _) =>
        {
            await _dataOperationGate.WaitAsync();
            _dataOperationGate.Release();
            _dataOperationGate.Dispose();
            _serviceScope?.Dispose();
        };
        UpdateNavigation();
    }

    private async Task LoadDataAsync()
    {
        await _dataOperationGate.WaitAsync();
        try
        {
            await LoadDataCoreAsync();
        }
        finally
        {
            _dataOperationGate.Release();
        }
    }

    private async Task LoadDataCoreAsync()
    {
        _groups = [];
        _selectedGroup = null;
        _roomPager.SetSource([]);
        if (_groupQuery is null || _contextAccessor?.Current is not { } context)
        {
            _groupPager.SetSource([]);
            UpdatePagerSummary();
            return;
        }

        try
        {
            _groups = await _groupQuery.GetGroupsAsync(context.Property.PropertyId.Value, CancellationToken.None);
            var today = DateOnly.FromDateTime(DateTime.Today);
            var rows = _groups
                .Where(x => x.Status is not (GroupReservationStatus.Cancelled or GroupReservationStatus.Closed or GroupReservationStatus.CheckedOut))
                .Where(x => x.ArrivalDate <= today)
                .Select(x => new CheckInRow(
                    x.GroupCode,
                    x.GroupName,
                    x.CompanyName ?? x.AgencyName ?? "—",
                    x.ArrivalDate.ToString("dd/MM/yyyy"),
                    x.DepartureDate.ToString("dd/MM/yyyy"),
                    x.ExpectedRooms.ToString(),
                    (x.TotalAdults + x.TotalChildren).ToString(),
                    x.Status.ToString()))
                .ToList();
            _groupPager.SetSource(rows);
        }
        catch (Exception ex)
        {
            _groupPager.SetSource([]);
            MessageBox.Show($"Không thể tải danh sách đoàn thật.\n{ex.Message}", "Check-in đoàn", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        UpdatePagerSummary();
        UpdateCheckInSummary();
    }

    private void UpdateCheckInSummary()
    {
        if (_selectedGroup is null)
        {
            CheckInKpiText.Text = "Chưa có dữ liệu kiểm tra.";
            CheckInSummaryText.Text = "Chưa chọn đoàn để xác nhận.";
            CheckInReadinessText.Text = "Chưa có dữ liệu kiểm tra.";
            return;
        }

        var assignments = _selectedGroup.RoomingList;
        var checkedIn = assignments.Count(x => x.IsCheckedIn);
        var pending = assignments.Count(x => !x.IsCheckedIn && !x.IsCheckedOut);
        CheckInKpiText.Text = $"Phòng đã phân: {assignments.Count} · Đã check-in: {checkedIn} · Chờ check-in: {pending} · Khách: {assignments.Sum(x => x.Adults + x.Children)}";
        CheckInSummaryText.Text = $"{_selectedGroup.GroupCode} · {_selectedGroup.GroupName} · {_selectedGroup.Status} · {assignments.Count} phòng phân thật";
        CheckInReadinessText.Text = _selectedGroup.Status is GroupReservationStatus.Confirmed or GroupReservationStatus.CheckedIn
            ? assignments.Count > 0 ? "Sẵn sàng kiểm tra và thực hiện Check-in đoàn" : "Chưa sẵn sàng: đoàn chưa có phòng được phân"
            : $"Chưa sẵn sàng: trạng thái đoàn là {_selectedGroup.Status}, cần Confirmed";
    }

    private static Grid CreatePagerHost(TablePagerViewModel<CheckInRow> pager, out TextBlock summary)
    {
        var host = new Grid { Margin = new Thickness(0, 8, 0, 0) };
        host.ColumnDefinitions.Add(new ColumnDefinition());
        host.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        summary = new TextBlock { VerticalAlignment = VerticalAlignment.Center, FontSize = 12, Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#64748B")) };
        var control = new ServerPagerControl { DataContext = pager };
        Grid.SetColumn(control, 1);
        host.Children.Add(summary);
        host.Children.Add(control);
        return host;
    }

    private void UpdatePagerSummary()
    {
        if (_groupPagerSummary is not null) _groupPagerSummary.Text = _groupPager.PageText;
        if (_roomPagerSummary is not null) _roomPagerSummary.Text = _roomPager.PageText;
    }

    private int CurrentStep { get => _step; set { _step = value; UpdateNavigation(); } }

    private void UpdateNavigation()
    {
        StepLabel.Text = $"Bước {StepLetters[CurrentStep]}/6";
        BackBtn.Visibility = CurrentStep > 0 ? Visibility.Visible : Visibility.Collapsed;
        NextBtn.Visibility = CurrentStep < 5 ? Visibility.Visible : Visibility.Collapsed;
        ExecuteBtn.Visibility = CurrentStep == 5 ? Visibility.Visible : Visibility.Collapsed;
        for (int i = 0; i < 6; i++)
        {
            _panels[i].Visibility = i == CurrentStep ? Visibility.Visible : Visibility.Collapsed;
            if (i < CurrentStep) { _stepBoxes[i].Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F0FDF4")); _stepBoxes[i].BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#86EFAC")); _labels[i].Text = "Hoàn tất"; _labels[i].Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#166534")); }
            else if (i == CurrentStep) { _stepBoxes[i].Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EFF6FF")); _stepBoxes[i].BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3B82F6")); _stepBoxes[i].BorderThickness = new Thickness(1.5); _labels[i].Text = "Đang nhập"; _labels[i].Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1E40AF")); }
            else { _stepBoxes[i].Background = Brushes.White; _stepBoxes[i].BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E2E8F0")); _stepBoxes[i].BorderThickness = new Thickness(1); _labels[i].Text = "Chưa bắt đầu"; _labels[i].Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#64748B")); }
        }
    }

    private void OnGroupSelected(object sender, SelectionChangedEventArgs e)
    {
        if (GroupGrid.SelectedItem is CheckInRow r)
        {
            _selectedGroup = _groups.FirstOrDefault(x => x.GroupCode == r.Col1);
            DetailGroup.Text = r.Col2; DetailGroupInfo.Text = $"{r.Col3} · {r.Col8}";
            if (_selectedGroup is not null)
            {
                DetailStay.Text = $"{_selectedGroup.ArrivalDate:dd/MM/yyyy} → {_selectedGroup.DepartureDate:dd/MM/yyyy}";
                DetailNights.Text = $"{_selectedGroup.DepartureDate.DayNumber - _selectedGroup.ArrivalDate.DayNumber} đêm";
                DetailRooms.Text = $"{_selectedGroup.ExpectedRooms} phòng";
                DetailGuests.Text = $"{_selectedGroup.TotalAdults + _selectedGroup.TotalChildren} khách";
                _roomPager.SetSource(_selectedGroup.RoomingList.Select(x => new CheckInRow(
                    x.RoomNumber,
                    x.RoomType,
                    "—",
                    "Đã phân",
                    $"{x.Adults + x.Children}",
                    x.IsCheckedIn ? "Đã nhận" : "Chưa nhận",
                    x.GuestName ?? "—",
                    x.IsCheckedOut ? "Đã trả" : x.IsCheckedIn ? "Đã check-in" : "Chưa check-in")));
                UpdatePagerSummary();
            }
            UpdateCheckInSummary();
        }
    }

    private void OnStepClick(object sender, MouseButtonEventArgs e) { if (sender is FrameworkElement fe && fe.Tag is string t && int.TryParse(t, out var s)) CurrentStep = s; }
    private void OnNext(object sender, RoutedEventArgs e) { if (CurrentStep < 5) CurrentStep++; }
    private void OnBack(object sender, RoutedEventArgs e) { if (CurrentStep > 0) CurrentStep--; }
    private async void OnExecute(object sender, RoutedEventArgs e)
    {
        await _dataOperationGate.WaitAsync();
        try
        {
            await ExecuteCoreAsync();
        }
        finally
        {
            _dataOperationGate.Release();
        }
    }

    private async Task ExecuteCoreAsync()
    {
        if (_selectedGroup is null)
        {
            MessageBox.Show("Hãy chọn một đoàn trước khi thực hiện.", "Check-in đoàn", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (_groupCheckIn is null)
        {
            MessageBox.Show("Chưa đăng ký command check-in đoàn thật. Thao tác bị chặn để không ghi dữ liệu giả.", "Check-in đoàn", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var assignments = _selectedGroup.RoomingList
            .Where(x => !x.IsCheckedOut)
            .Select(x => x.AssignmentId)
            .ToArray();
        if (assignments.Length == 0)
        {
            MessageBox.Show("Đoàn chưa có phòng được phân để check-in.", "Check-in đoàn", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            var result = await _groupCheckIn.CheckInAsync(_selectedGroup.GroupReservationId, assignments, CancellationToken.None);
            MessageBox.Show($"Đã check-in thật {result.CheckedInRooms} phòng / {result.CheckedInGuests} khách cho đoàn {result.GroupCode}.", "Check-in đoàn", MessageBoxButton.OK, MessageBoxImage.Information);
            await LoadDataCoreAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Không thể check-in đoàn. Dữ liệu chưa được ghi.\n{FormatCheckInError(ex)}", "Check-in đoàn", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private static string FormatCheckInError(Exception ex) => ex.Message switch
    {
        "GROUP_ROOM_RESERVATION_REQUIRED" => "Các phòng đã phân chưa có Reservation thật. Hãy hoàn tất liên kết đặt phòng trước khi check-in.",
        "GROUP_ROOM_STAY_REQUIRED" => "Reservation của một hoặc nhiều phòng chưa có Stay thật. Không thể đổi trạng thái check-in.",
        "GROUP_ROOM_FOLIO_REQUIRED" => "Stay của một hoặc nhiều phòng chưa có Folio thật. Không thể đổi trạng thái check-in.",
        "GROUP_ROOM_GUEST_REQUIRED" => "Mỗi phòng đoàn phải được gán một hồ sơ khách thật trước khi check-in.",
        "GROUP_GUEST_NOT_FOUND" => "Hồ sơ khách được gán không còn tồn tại trong cơ sở dữ liệu hiện tại.",
        "GROUP_GUEST_BLACKLISTED" => "Hồ sơ khách đang bị đánh dấu hạn chế; cần xử lý tại Guest Management trước khi check-in.",
        "HOTEL_ROOM_NOT_FOUND" => "Phòng được phân không còn tồn tại trong Hotel Master Data.",
        "HOTEL_ROOM_NOT_ASSIGNABLE" => "Phòng được phân hiện không ở trạng thái sẵn sàng để check-in.",
        "GROUP_ROOM_TYPE_MISMATCH" => "Loại phòng trong rooming list không khớp Hotel Master Data.",
        "GROUP_ROOM_RESERVATION_CONFLICT" => "Phòng đã có một đặt phòng khác bị trùng thời gian.",
        "GROUP_RESERVATION_MISMATCH" => "Reservation liên kết không khớp khách, phòng hoặc thời gian của assignment.",
        "GROUP_RESERVATION_NOT_CHECKINABLE" => "Reservation liên kết chưa ở trạng thái cho phép check-in.",
        "GROUP_STAY_MISMATCH" => "Stay liên kết không khớp khách hoặc phòng của assignment.",
        "GROUP_STAY_NOT_CHECKINABLE" => "Stay liên kết không ở trạng thái cho phép check-in.",
        _ => ex.Message
    };
    private void OnClose(object sender, RoutedEventArgs e) => Close();
    private async void OnRefresh(object sender, RoutedEventArgs e) => await LoadDataAsync();
}

public record CheckInRow(string Col1, string Col2 = "", string Col3 = "", string Col4 = "", string Col5 = "", string Col6 = "", string Col7 = "", string Col8 = "");
