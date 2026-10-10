using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ASMILE.BO.Application.FrontOffice.Cashier;
using ASMILE.BO.Application.FrontOffice.Foundation.Contexts;
using ASMILE.BO.Application.FrontOffice.Group;
using ASMILE.BO.Application.FrontOffice.Guest;
using ASMILE.BO.Application.FrontOffice.Housekeeping;
using ASMILE.BO.Application.FrontOffice.Incident;
using ASMILE.BO.Application.FrontOffice.Stay;
using ASMILE.BO.Application.Interfaces;
using ASMILE.BO.Core.FrontOffice.Housekeeping;
using ASMILE.BO.App.Views.Dialogs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Win32;

namespace ASMILE.BO.App.Views;

public partial class ShiftHandoverWindow : Window
{
    private readonly IServiceScope? _scope;
    private int _centerTab;
    private int _inspectorTab;
    private readonly List<ShGuest> _guests;
    private readonly List<ShTask> _tasks;
    private readonly Dictionary<string, bool> _checklist = new();
    private readonly ICashShiftService? _cashShiftService;
    private readonly IHotelStayRepository? _stayRepository;
    private readonly IHousekeepingBoardService? _housekeepingBoard;
    private readonly IIncidentQueryService? _incidentQuery;
    private readonly IHotelStayOccupancyReader? _occupancyReader;
    private readonly IFrontOfficeExecutionContextAccessor? _executionContext;
    private readonly IGuestProfileRepository? _guestRepository;
    private readonly IGroupReservationQueryService? _groupReservationQuery;
    private readonly IAuditService? _auditService;
    private CashShiftDto? _activeShift;
    private int _incidentCount;
    private int _roomCount;
    private int _outOfOrderCount;
    private int _outOfServiceCount;
    private int _tomorrowArrivalCount;
    private int _tomorrowDepartureCount;
    private int _vipPropertyCount;
    private int _tomorrowGroupCount;
    private int _auditEntryCount;
    private readonly List<CashierOptionDto> _cashierOptions = new();
    private ComboBox? _targetCashierBox;
    private TextBox? _countedCashBox;
    private TextBox? _handoverNotesBox;
    private TextBox? _handoverIssuesBox;
    private TextBox? _handoverNextActionsBox;
    private string? _dataError;

    public ShiftHandoverWindow()
    {
        InitializeComponent();
        Owner = System.Windows.Application.Current.MainWindow;
        var rootServices = (System.Windows.Application.Current as App)?.Services;
        _scope = rootServices?.CreateScope();
        var services = _scope?.ServiceProvider;
        _cashShiftService = services?.GetService<ICashShiftService>();
        _stayRepository = services?.GetService<IHotelStayRepository>();
        _housekeepingBoard = services?.GetService<IHousekeepingBoardService>();
        _incidentQuery = services?.GetService<IIncidentQueryService>();
        _occupancyReader = services?.GetService<IHotelStayOccupancyReader>();
        _executionContext = services?.GetService<IFrontOfficeExecutionContextAccessor>();
        _guestRepository = services?.GetService<IGuestProfileRepository>();
        _groupReservationQuery = services?.GetService<IGroupReservationQueryService>();
        _auditService = services?.GetService<IAuditService>();
        _guests = new List<ShGuest>();
        _tasks = new List<ShTask>();
        InitCenterTabs();
        BuildKpi();
        BuildShiftInfo();
        BuildShiftSummary();
        InitInspectorTabs();
        FooterText.Text = "Đang tải dữ liệu bàn giao ca thật...";
        Loaded += OnLoadedAsync;
        Closed += (_, _) => _scope?.Dispose();
    }

    private async void OnLoadedAsync(object? sender, RoutedEventArgs e) => await OnLoadedAsyncCore();

    private async Task OnLoadedAsyncCore()
    {
        if (_executionContext?.Current is not { } context || _cashShiftService is null || _stayRepository is null || _housekeepingBoard is null || _occupancyReader is null)
        {
            _dataError = "Chưa cấu hình đủ context/ca thu ngân/Stay/occupancy/Housekeeping để tải dữ liệu thật.";
            UpdateRealDataView();
            return;
        }

        try
        {
            var propertyId = context.Property.PropertyId.Value;
            _activeShift = (await _cashShiftService.GetActiveShiftsAsync(propertyId, CancellationToken.None)).FirstOrDefault();
            _cashierOptions.Clear();
            _cashierOptions.AddRange(await _cashShiftService.GetCashierOptionsAsync(propertyId, CancellationToken.None));
            var arrivals = await _stayRepository.GetExpectedArrivalsAsync(propertyId, context.BusinessDate.BusinessDate, CancellationToken.None);
            var inHouse = await _stayRepository.GetCurrentInHouseAsync(propertyId, CancellationToken.None);
            var departures = await _stayRepository.GetExpectedDeparturesAsync(propertyId, context.BusinessDate.BusinessDate, CancellationToken.None);
            var tomorrow = context.BusinessDate.BusinessDate.AddDays(1);
            var tomorrowArrivals = await _stayRepository.GetExpectedArrivalsAsync(propertyId, tomorrow, CancellationToken.None);
            var tomorrowDepartures = await _stayRepository.GetExpectedDeparturesAsync(propertyId, tomorrow, CancellationToken.None);
            var occupancy = await _occupancyReader.GetRoomOccupancyAsync(propertyId, CancellationToken.None);
            _roomCount = occupancy.Count;
            _outOfOrderCount = occupancy.Count(x => IsOperationalStatus(x.OperationalStatus, "outoforder", "out of order"));
            _outOfServiceCount = occupancy.Count(x => IsOperationalStatus(x.OperationalStatus, "outofservice", "out of service"));
            _tomorrowArrivalCount = tomorrowArrivals.Count;
            _tomorrowDepartureCount = tomorrowDepartures.Count;
            var vipDashboard = _guestRepository is null
                ? null
                : await _guestRepository.GetVipDashboardAsync(propertyId, CancellationToken.None);
            _vipPropertyCount = vipDashboard?.TotalVip ?? 0;
            var groups = _groupReservationQuery is null
                ? Array.Empty<GroupReservationSummaryDto>()
                : await _groupReservationQuery.GetGroupsAsync(propertyId, CancellationToken.None);
            _tomorrowGroupCount = groups.Count(x => x.ArrivalDate == tomorrow);
            _auditEntryCount = _auditService is null
                ? 0
                : (await _auditService.QueryAsync(fromDate: DateTime.UtcNow.AddDays(-1), toDate: DateTime.UtcNow, cancellationToken: CancellationToken.None)).Count;
            var departureIds = departures.Select(x => x.HotelStayId).ToHashSet();
            var arrivalIds = arrivals.Select(x => x.HotelStayId).ToHashSet();
            _guests.Clear();
            foreach (var stay in inHouse.Concat(arrivals).Concat(departures).GroupBy(x => x.HotelStayId).Select(x => x.First()))
            {
                var status = departureIds.Contains(stay.HotelStayId) ? "Departure" : arrivalIds.Contains(stay.HotelStayId) ? "Arrival" : "In-House";
                _guests.Add(new ShGuest(stay.HotelStayId, stay.GuestName, stay.RoomNumber, stay.ReservationNumber ?? stay.StayNumber,
                    stay.ScheduledArrivalDate.ToString("dd/MM"), stay.ScheduledDepartureDate.ToString("dd/MM"), status, false, null, null));
            }
            var board = await _housekeepingBoard.GetTaskBoardAsync(propertyId, null, null, null, CancellationToken.None);
            _tasks.Clear();
            _tasks.AddRange(board.Where(x => x.TaskStatus != HousekeepingTaskStatus.Completed).Select(x => new ShTask(x.TaskId, x.TaskNumber, x.TaskType.ToString(), x.RoomNumber, x.Priority.ToString(), x.TaskStatus.ToString(), x.AssignedName ?? "—")));
            _incidentCount = _incidentQuery is null
                ? 0
                : (await _incidentQuery.SearchAsync(new IncidentSearchRequest(propertyId), CancellationToken.None)).Count;
            BuildDerivedChecklist();
            _dataError = null;
        }
        catch (Exception ex)
        {
            _activeShift = null;
            _guests.Clear();
            _tasks.Clear();
            _incidentCount = 0;
            _roomCount = 0;
            _outOfOrderCount = 0;
            _outOfServiceCount = 0;
            _tomorrowArrivalCount = 0;
            _tomorrowDepartureCount = 0;
            _vipPropertyCount = 0;
            _tomorrowGroupCount = 0;
            _auditEntryCount = 0;
            _checklist.Clear();
            _dataError = $"Không tải được dữ liệu vận hành thật: {ex.Message}";
        }
        UpdateRealDataView();
    }

    private void BuildDerivedChecklist()
    {
        _checklist.Clear();
        _checklist["Đã kiểm tra ca thu ngân"] = _activeShift is not null;
        _checklist["Đã kiểm tra khách đến, đang ở và khách đi"] = true;
        _checklist["Đã kiểm tra công việc nhà buồng tồn"] = true;
        _checklist["Đã kiểm tra trạng thái phòng"] = _roomCount > 0;
        _checklist["Đã kiểm tra sự cố trong cơ sở"] = _incidentQuery is not null;
    }

    private void UpdateRealDataView()
    {
        BuildKpi();
        BuildShiftInfo();
        BuildShiftSummary();
        InitCenterTabs();
        InitInspectorTabs();
        FooterText.Text = _dataError ?? $"Đã tải {_guests.Count:N0} khách, {_tasks.Count:N0} việc tồn, {_roomCount:N0} phòng và {(_activeShift is null ? 0 : 1):N0} ca mở từ dữ liệu thật.";
    }

    private void InitCenterTabs()
    {
        CenterTabs.Children.Clear();
        var tabs = new[] { "Tổng quan", "Khách lưu trú", "Công việc tồn", "Thu ngân", "Sự cố", "Nhật ký", "Bàn giao" };
        for (int i = 0; i < tabs.Length; i++) { var idx = i; var active = i == _centerTab; var b = TabBtn(tabs[i], active); b.MouseLeftButtonDown += (_, _) => SwitchTab(idx); CenterTabs.Children.Add(b); }
        ShowTab(_centerTab);
    }

    private Border TabBtn(string text, bool active) => new() { CornerRadius = new CornerRadius(8), Padding = new Thickness(12, 7, 12, 7), Margin = new Thickness(0, 0, 4, 0), Cursor = System.Windows.Input.Cursors.Hand, Background = active ? Cb("#EFF6FF") : Cb("Transparent"), BorderBrush = active ? Cb("#2F6BFF") : Cb("Transparent"), BorderThickness = new Thickness(1), Child = new TextBlock { Text = text, FontSize = 11, FontWeight = active ? FontWeights.SemiBold : FontWeights.Normal, Foreground = active ? C("#2F6BFF") : C("#475569") } };

    private void SwitchTab(int idx) { _centerTab = idx; InitCenterTabs(); ShowTab(idx); }

    private void ShowTab(int tab)
    {
        for (int i = 1; i <= 7; i++) { var el = FindName($"Tab{i}") as UIElement; if (el != null) el.Visibility = i == tab + 1 ? Visibility.Visible : Visibility.Collapsed; }
        switch (tab) { case 0: BuildOverview(); break; case 1: BuildGuestTab(); break; case 2: BuildTaskTab(); break; case 3: BuildCashierTab(); break; case 4: BuildIncidentTab(); break; case 5: BuildLogTab(); break; case 6: BuildHandoverTab(); break; }
    }

    private void BuildShiftInfo()
    {
        ShiftInfoPanel.Children.Clear();
        var context = _executionContext?.Current;
        var shift = _activeShift;
        var items = new[]
        {
            ("Ca hiện tại", shift?.ShiftNumber ?? "Chưa có ca mở"),
            ("Ca tiếp theo", "Chưa có dữ liệu"),
            ("Operator", string.IsNullOrWhiteSpace(shift?.CashierName) ? "Chưa có dữ liệu" : shift!.CashierName),
            ("Supervisor", "Chưa có dữ liệu"),
            ("Ngày KD", context?.BusinessDate.BusinessDate.ToString("dd/MM/yyyy") ?? "Chưa có dữ liệu"),
            ("Mở ca", shift?.OpenedAtUtc.ToLocalTime().ToString("dd/MM/yyyy HH:mm") ?? "—"),
            ("Đóng ca", shift?.ClosedAtUtc?.ToLocalTime().ToString("dd/MM/yyyy HH:mm") ?? "—")
        };
        foreach (var (label, value) in items) ShiftInfoPanel.Children.Add(InfoItem(label, value));
    }

    private FrameworkElement InfoItem(string l, string v) => new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(4, 0, 16, 0), Children = { new TextBlock { Text = l + ": ", FontSize = 11, Foreground = C("#94A3B8"), VerticalAlignment = VerticalAlignment.Center }, new TextBlock { Text = v, FontSize = 11, FontWeight = FontWeights.SemiBold, Foreground = C("#17365D"), VerticalAlignment = VerticalAlignment.Center } } };

    private void BuildKpi()
    {
        KpiStrip.Children.Clear();
        var kpis = new (string, string, string)[] {
            ("Check-in", _guests.Count(g => g.Status == "Arrival").ToString(), "#16A34A"),
            ("Check-out", _guests.Count(g => g.Status == "Departure").ToString(), "#DC2626"),
            ("In House", _guests.Count(g => g.Status == "In-House").ToString(), "#2F6BFF"),
            ("Arrival", _guests.Count(g => g.Status == "Arrival").ToString(), "#F59E0B"),
            ("Departure", _guests.Count(g => g.Status == "Departure").ToString(), "#8B5CF6"),
            ("VIP", _vipPropertyCount.ToString(), "#F59E0B"),
            ("Pending Task", _tasks.Count(t => t.Status != "Completed").ToString(), "#DC2626"),
            ("Outstanding Folio", _guests.Count(g => g.Balance > 0).ToString(), "#F59E0B"),
            ("OOO/OOS", (_outOfOrderCount + _outOfServiceCount).ToString(), "#64748B")
        };
        foreach (var (label, value, color) in kpis) KpiStrip.Children.Add(Kpi(label, value, color));
    }

    private static FrameworkElement Kpi(string l, string v, string color) => new Border { Background = Cb("#FAFBFD"), CornerRadius = new CornerRadius(8), Padding = new Thickness(14, 8, 14, 8), Margin = new Thickness(0, 0, 8, 0), Child = new StackPanel { Children = { new TextBlock { Text = v, FontSize = 20, FontWeight = FontWeights.Bold, Foreground = C(color) }, new TextBlock { Text = l, FontSize = 10, Foreground = C("#94A3B8") } } } };

    // ═══ TAB CONTENT ═══

    private void BuildOverview()
    {
        Tab1Panel.Children.Clear();
        Tab1Panel.Children.Add(STitle("Tổng quan vận hành"));
        var businessDate = _executionContext?.Current?.BusinessDate.BusinessDate;
        Tab1Panel.Children.Add(Row("Ngày kinh doanh", businessDate?.ToString("dd/MM/yyyy") ?? "Chưa có context"));
        var inHouseCount = _guests.Count(g => g.Status == "In-House");
        var occupancyText = _roomCount == 0 ? "Chưa có phòng trong dữ liệu thật" : $"{inHouseCount:N0}/{_roomCount:N0} phòng ({(decimal)inHouseCount / _roomCount:P1})";
        Tab1Panel.Children.Add(Row("Occupancy", occupancyText));
        Tab1Panel.Children.Add(Row("Arrival hôm nay", _guests.Count(g => g.Status == "Arrival") + " phòng")); Tab1Panel.Children.Add(Row("Departure hôm nay", _guests.Count(g => g.Status == "Departure") + " phòng"));
        Tab1Panel.Children.Add(Row("VIP trong cơ sở", $"{_vipPropertyCount:N0} hồ sơ VIP từ Guest Profile")); Tab1Panel.Children.Add(Row("OOO/OOS", $"{_outOfOrderCount + _outOfServiceCount:N0} phòng ({_outOfOrderCount:N0} OOO · {_outOfServiceCount:N0} OOS)"));
        Tab1Panel.Children.Add(new Border { Height = 1, Background = Cb("#E2E8F0"), Margin = new Thickness(0, 8, 0, 8) });
        Tab1Panel.Children.Add(STitle("Dự báo"));
        Tab1Panel.Children.Add(Row("Ngày mai Arrival", $"{_tomorrowArrivalCount:N0} khách")); Tab1Panel.Children.Add(Row("Ngày mai Departure", $"{_tomorrowDepartureCount:N0} khách"));
        Tab1Panel.Children.Add(Row("Đoàn ngày mai", $"{_tomorrowGroupCount:N0} đoàn từ Group Reservation"));
    }

    private void BuildGuestTab()
    {
        Tab2Panel.Children.Clear();
        var hdr = new Grid { Margin = new Thickness(0, 0, 0, 4) };
        var cw = new[] { 130.0, 50.0, 90.0, 70.0, 70.0, 55.0, 90.0, 120.0 };
        var ch = new[] { "Khách", "Phòng", "Reservation", "Đến", "Đi", "VIP", "Balance", "Ghi chú" };
        for (int j = 0; j < cw.Length; j++) { hdr.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(cw[j]) }); AddHdr(hdr, j, ch[j]); }
        Tab2Panel.Children.Add(hdr);
        foreach (var g in _guests.Take(50))
        {
            var row = new Grid { Margin = new Thickness(0, 0, 0, 1) };
            for (int j = 0; j < cw.Length; j++) row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(cw[j]) });
            var vals = new[] { g.Name, g.Room, g.Reservation, g.Arrival, g.Departure, g.IsVip ? "★ VIP" : "—", g.Balance?.ToString("N0") ?? "—", g.Remark ?? "—" };
            var colors = new[] { "#17365D", "#64748B", "#64748B", "#64748B", "#64748B", g.IsVip ? "#F59E0B" : "#94A3B8", g.Balance > 0 ? "#DC2626" : "#64748B", "#64748B" };
            for (int j = 0; j < vals.Length; j++) AddCell(row, j, vals[j], colors[j], j == 0);
            Tab2Panel.Children.Add(row);
        }
    }

    private void BuildTaskTab()
    {
        Tab3Panel.Children.Clear();
        var hdr = new Grid { Margin = new Thickness(0, 0, 0, 4) };
        var cw = new[] { 90.0, 100.0, 200.0, 70.0, 80.0, 80.0 };
        var ch = new[] { "Mã", "Danh mục", "Mô tả", "Ưu tiên", "Trạng thái", "Owner" };
        for (int j = 0; j < cw.Length; j++) { hdr.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(cw[j]) }); AddHdr(hdr, j, ch[j]); }
        Tab3Panel.Children.Add(hdr);
        foreach (var t in _tasks.Take(40))
        {
            var priColor = t.Priority switch { "Critical" => "#DC2626", "High" => "#F59E0B", _ => "#64748B" };
            var stColor = t.Status switch { "Open" => "#DC2626", "Pending" => "#F59E0B", "In Progress" => "#2F6BFF", _ => "#16A34A" };
            var row = new Grid { Margin = new Thickness(0, 0, 0, 1) };
            for (int j = 0; j < cw.Length; j++) row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(cw[j]) });
            var vals = new[] { t.Code, t.Category, t.Description, t.Priority, t.Status, t.Owner };
            var colors = new[] { "#64748B", "#64748B", "#17365D", priColor, stColor, "#64748B" };
            for (int j = 0; j < vals.Length; j++) AddCell(row, j, vals[j], colors[j], j == 2);
            Tab3Panel.Children.Add(row);
        }
    }

    private void BuildCashierTab()
    {
        Tab4Panel.Children.Clear();
        Tab4Panel.Children.Add(STitle("Thu ngân"));
        if (_activeShift is null)
        {
            Tab4Panel.Children.Add(Row("Trạng thái", "Không có ca mở trong dữ liệu thật"));
            return;
        }
        Tab4Panel.Children.Add(Row("Số ca", _activeShift.ShiftNumber));
        Tab4Panel.Children.Add(Row("Số dư đầu ca", $"{_activeShift.OpeningCash:N0} {_executionContext?.Current?.Property.CurrencyCode ?? "VND"}"));
        Tab4Panel.Children.Add(Row("Số dư kỳ vọng", $"{_activeShift.ExpectedCash:N0} {_executionContext?.Current?.Property.CurrencyCode ?? "VND"}"));
        Tab4Panel.Children.Add(Row("Chênh lệch", _activeShift.Variance is { } variance ? $"{variance:N0}" : "Chưa đối soát"));
        Tab4Panel.Children.Add(new Border { Height = 1, Background = Cb("#E2E8F0"), Margin = new Thickness(0, 6, 0, 6) });
        Tab4Panel.Children.Add(Row("Số biến động tiền", _activeShift.Movements.Count.ToString("N0")));
        Tab4Panel.Children.Add(Row("Trạng thái", _activeShift.Status.ToString()));
    }

    private void BuildIncidentTab()
    {
        Tab5Panel.Children.Clear();
        Tab5Panel.Children.Add(Row("Sự cố trong property", _incidentQuery is null ? "Chưa cấu hình query" : _incidentCount.ToString("N0")));
        Tab5Panel.Children.Add(Row("Nguồn", _incidentQuery is null ? "Fail-closed" : "fo.SuCo"));
    }

    private void BuildLogTab()
    {
        Tab6Panel.Children.Clear();
        Tab6Panel.Children.Add(STitle("Nhật ký thao tác"));
        Tab6Panel.Children.Add(Row("Trong 24 giờ", _auditService is null ? "Chưa cấu hình audit service" : $"{_auditEntryCount:N0} bản ghi từ AuditLog"));
        Tab6Panel.Children.Add(Row("Nguồn", _auditService is null ? "Fail-closed" : "AuditService.QueryAsync"));
    }

    private void BuildHandoverTab()
    {
        Tab7Panel.Children.Clear();
        Tab7Panel.Children.Add(STitle("Thông tin bàn giao tiền mặt"));
        Tab7Panel.Children.Add(Row("Ca nguồn", _activeShift?.ShiftNumber ?? "Không có ca mở"));
        Tab7Panel.Children.Add(Row("Số tiền kỳ vọng", _activeShift is null ? "—" : $"{_activeShift.ExpectedCash:N0} {_executionContext?.Current?.Property.CurrencyCode ?? "VND"}"));
        Tab7Panel.Children.Add(new TextBlock { Text = "Ca nhận *", Style = FindResource("FieldLabel") as Style });
        _targetCashierBox = new ComboBox { Height = 30, FontSize = 12, DisplayMemberPath = nameof(CashierOptionDto.DisplayName), ItemsSource = _cashierOptions.Where(x => x.UserId != _activeShift?.CashierId).ToList() };
        if (_targetCashierBox.Items.Count > 0) _targetCashierBox.SelectedIndex = 0;
        Tab7Panel.Children.Add(_targetCashierBox);
        Tab7Panel.Children.Add(new TextBlock { Text = "Tiền kiểm đếm *", Style = FindResource("FieldLabel") as Style, Margin = new Thickness(0, 10, 0, 3) });
        _countedCashBox = new TextBox { Height = 30, Padding = new Thickness(8, 0, 8, 0), FontSize = 12, Text = _activeShift?.ExpectedCash.ToString("0.##", CultureInfo.InvariantCulture) ?? string.Empty };
        Tab7Panel.Children.Add(_countedCashBox);

        if (_cashierOptions.Count == 0)
            Tab7Panel.Children.Add(new TextBlock { Text = "Chưa có người dùng active để chọn làm ca nhận.", FontSize = 11, Foreground = C("#DC2626"), TextWrapping = TextWrapping.Wrap });

        Tab7Panel.Children.Add(STitle("Ghi chú bàn giao"));
        Tab7Panel.Children.Add(new TextBlock { Text = "Ghi chú chung", Style = FindResource("FieldLabel") as Style });
        _handoverNotesBox = new TextBox { Height = 70, TextWrapping = TextWrapping.Wrap, AcceptsReturn = true, Style = FindResource("ModernTextBox") as Style, Margin = new Thickness(0, 0, 0, 10) };
        Tab7Panel.Children.Add(_handoverNotesBox);

        Tab7Panel.Children.Add(STitle("Vấn đề quan trọng"));
        _handoverIssuesBox = new TextBox { Height = 70, TextWrapping = TextWrapping.Wrap, AcceptsReturn = true, Style = FindResource("ModernTextBox") as Style, Margin = new Thickness(0, 0, 0, 10) };
        Tab7Panel.Children.Add(_handoverIssuesBox);

        Tab7Panel.Children.Add(STitle("Hành động ca sau"));
        _handoverNextActionsBox = new TextBox { Height = 70, TextWrapping = TextWrapping.Wrap, AcceptsReturn = true, Style = FindResource("ModernTextBox") as Style, Margin = new Thickness(0, 0, 0, 10) };
        Tab7Panel.Children.Add(_handoverNextActionsBox);
        var checklistPanel = new StackPanel();
        if (_checklist.Count == 0)
        {
            checklistPanel.Children.Add(new TextBlock { Text = "Chưa tải được checklist vì dữ liệu vận hành thật chưa sẵn sàng.", FontSize = 11, Foreground = C("#DC2626"), TextWrapping = TextWrapping.Wrap });
        }
        else
        {
            foreach (var item in _checklist.Keys.ToList())
            {
                var key = item;
                var checkBox = new CheckBox
                {
                    Content = key,
                    IsChecked = _checklist[key],
                    FontSize = 11,
                    Foreground = C("#475569"),
                    Margin = new Thickness(0, 0, 0, 6)
                };
                checkBox.Checked += (_, _) => { _checklist[key] = true; BuildShiftSummary(); };
                checkBox.Unchecked += (_, _) => { _checklist[key] = false; BuildShiftSummary(); };
                checklistPanel.Children.Add(checkBox);
            }
        }
        Tab7Panel.Children.Add(checklistPanel);

        Tab7Panel.Children.Add(new Border { Height = 1, Background = Cb("#E2E8F0"), Margin = new Thickness(0, 8, 0, 8) });
        Tab7Panel.Children.Add(STitle("Nhắc nhở bộ phận"));
        var reminders = new List<string>();
        if (_tasks.Count > 0) reminders.Add($"Nhà buồng còn {_tasks.Count:N0} công việc chưa hoàn tất.");
        if (_incidentCount > 0) reminders.Add($"Cần theo dõi {_incidentCount:N0} sự cố đang có trong cơ sở.");
        if (_tomorrowArrivalCount > 0) reminders.Add($"Ngày mai có {_tomorrowArrivalCount:N0} khách dự kiến đến.");
        if (_tomorrowDepartureCount > 0) reminders.Add($"Ngày mai có {_tomorrowDepartureCount:N0} khách dự kiến đi.");
        if (reminders.Count == 0) reminders.Add("Không có nhắc việc liên bộ phận từ dữ liệu vận hành hiện tại.");
        foreach (var reminder in reminders)
            Tab7Panel.Children.Add(new TextBlock { Text = "• " + reminder, FontSize = 11, Foreground = C("#64748B"), TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 0, 0, 5) });
    }

    private static void AddHdr(Grid g, int col, string text) { var tb = new TextBlock { Text = text, FontSize = 11, FontWeight = FontWeights.SemiBold, Foreground = C("#475569"), Margin = new Thickness(2, 3, 2, 3) }; Grid.SetColumn(tb, col); g.Children.Add(tb); }
    private static void AddCell(Grid g, int col, string text, string color, bool bold) { var tb = new TextBlock { Text = text, FontSize = 11, FontWeight = bold ? FontWeights.SemiBold : FontWeights.Normal, Foreground = C(color), Margin = new Thickness(2, 2, 2, 2) }; Grid.SetColumn(tb, col); g.Children.Add(tb); }

    private void BuildShiftSummary()
    {
        ShiftSummaryPanel.Children.Clear();
        ShiftSummaryPanel.Children.Add(STitle("Tóm tắt ca"));
        ShiftSummaryPanel.Children.Add(Row("Ca hiện tại", _activeShift?.ShiftNumber ?? "Chưa có ca mở"));
        ShiftSummaryPanel.Children.Add(Row("Operator", _activeShift?.CashierName ?? "Chưa có dữ liệu"));
        ShiftSummaryPanel.Children.Add(Row("Khách đang theo dõi", _guests.Count.ToString("N0")));
        ShiftSummaryPanel.Children.Add(Row("Công việc housekeeping tồn", _tasks.Count.ToString("N0")));
        ShiftSummaryPanel.Children.Add(new Border { Height = 1, Background = Cb("#E2E8F0"), Margin = new Thickness(0, 8, 0, 8) });
        ShiftSummaryPanel.Children.Add(STitle("Cần xử lý trước khi bàn giao"));
        AddSummaryAlert("Ca thu ngân", _activeShift is null ? "Không có ca mở" : $"Kỳ vọng {_activeShift.ExpectedCash:N0}", "#EFF6FF", "#2563EB");
        AddSummaryAlert("Housekeeping", _tasks.Count == 0 ? "Không có việc tồn trong dữ liệu thật" : $"{_tasks.Count:N0} việc chưa hoàn tất", "#FEF2F2", "#DC2626");
        AddSummaryAlert("Incident", _incidentQuery is null ? "Chưa cấu hình query" : $"{_incidentCount:N0} sự cố từ dữ liệu thật", "#FFF7ED", "#EA580C");
        ShiftSummaryPanel.Children.Add(new Border { Height = 1, Background = Cb("#E2E8F0"), Margin = new Thickness(0, 8, 0, 8) });
        ShiftSummaryPanel.Children.Add(STitle("Sẵn sàng bàn giao"));
        var checkedItems = _checklist.Count(x => x.Value);
        var totalItems = Math.Max(1, _checklist.Count);
        ShiftSummaryPanel.Children.Add(Row("Đã kiểm tra", $"{checkedItems} / {totalItems} mục"));
        ShiftSummaryPanel.Children.Add(new ProgressBar { Height = 8, Minimum = 0, Maximum = totalItems, Value = checkedItems, Foreground = C("#16A34A"), Background = Cb("#E2E8F0"), Margin = new Thickness(0, 5, 0, 5) });
        ShiftSummaryPanel.Children.Add(new TextBlock { Text = "Chỉ xác nhận bàn giao khi đã chọn ca nhận và hoàn tất kiểm tra.", FontSize = 10, Foreground = C("#64748B"), TextWrapping = TextWrapping.Wrap });
    }

    private void AddSummaryAlert(string title, string detail, string background, string foreground)
    {
        ShiftSummaryPanel.Children.Add(new Border
        {
            Background = Cb(background),
            CornerRadius = new CornerRadius(7),
            Padding = new Thickness(9, 7, 9, 7),
            Margin = new Thickness(0, 0, 0, 5),
            Child = new StackPanel
            {
                Children =
                {
                    new TextBlock { Text = title, FontSize = 11, FontWeight = FontWeights.SemiBold, Foreground = C(foreground) },
                    new TextBlock { Text = detail, FontSize = 10, Foreground = C("#64748B"), Margin = new Thickness(0, 2, 0, 0), TextWrapping = TextWrapping.Wrap }
                }
            }
        });
    }

    // ═══ INSPECTOR ═══

    private void InitInspectorTabs()
    {
        InspectorTabs.Children.Clear();
        var tabs = new[] { "Tổng quan", "Cash", "Khách", "Task", "Sự cố", "Lịch sử" };
        for (int i = 0; i < tabs.Length; i++)
        {
            var idx = i;
            var active = i == _inspectorTab;
            var b = new Border
            {
                CornerRadius = new CornerRadius(7, 7, 0, 0),
                Padding = new Thickness(8, 5, 8, 5),
                Margin = new Thickness(0, 0, 2, 0),
                Cursor = System.Windows.Input.Cursors.Hand,
                Background = active ? Cb("#EFF6FF") : Cb("Transparent"),
                BorderBrush = active ? Cb("#2F6BFF") : Cb("Transparent"),
                BorderThickness = new Thickness(1, 1, 1, 0),
                Child = new TextBlock { Text = tabs[i], FontSize = 10, FontWeight = active ? FontWeights.SemiBold : FontWeights.Normal, Foreground = active ? C("#2F6BFF") : C("#64748B") }
            };
            b.MouseLeftButtonDown += (_, e) => { e.Handled = true; SwitchInspectorTab(idx); };
            InspectorTabs.Children.Add(b);
        }
        BuildInspector();
    }

    private void SwitchInspectorTab(int idx)
    {
        _inspectorTab = idx;
        InitInspectorTabs();
    }

    private void BuildInspector()
    {
        InspectorPanel.Children.Clear();
        switch (_inspectorTab)
        {
            case 1:
                InspectorPanel.Children.Add(STitle("Thu ngân ca"));
                InspectorPanel.Children.Add(Row("Số ca", _activeShift?.ShiftNumber ?? "Không có dữ liệu"));
                InspectorPanel.Children.Add(Row("Đầu ca", _activeShift is null ? "—" : $"{_activeShift.OpeningCash:N0}"));
                InspectorPanel.Children.Add(Row("Kỳ vọng", _activeShift is null ? "—" : $"{_activeShift.ExpectedCash:N0}"));
                InspectorPanel.Children.Add(Row("Biến động", _activeShift?.Movements.Count.ToString("N0") ?? "—"));
                break;
            case 2:
                InspectorPanel.Children.Add(STitle("Khách lưu trú"));
                InspectorPanel.Children.Add(Row("In-house", _guests.Count(x => x.Status == "In-House").ToString("N0")));
                InspectorPanel.Children.Add(Row("Arrival hôm nay", _guests.Count(x => x.Status == "Arrival").ToString("N0")));
                InspectorPanel.Children.Add(Row("Departure hôm nay", _guests.Count(x => x.Status == "Departure").ToString("N0")));
                InspectorPanel.Children.Add(Row("Nguồn", "HotelStay thật"));
                break;
            case 3:
                InspectorPanel.Children.Add(STitle("Công việc tồn"));
                InspectorPanel.Children.Add(Row("Tổng số việc", _tasks.Count.ToString("N0")));
                InspectorPanel.Children.Add(Row("Nguồn", "Housekeeping thật"));
                break;
            case 4:
                InspectorPanel.Children.Add(STitle("Sự cố cần theo dõi"));
                InspectorPanel.Children.Add(Row("Số sự cố", _incidentQuery is null ? "Chưa cấu hình query" : $"{_incidentCount:N0}"));
                InspectorPanel.Children.Add(Row("Nguồn", _incidentQuery is null ? "Fail-closed" : "Incident query thật"));
                break;
            case 5:
                InspectorPanel.Children.Add(STitle("Lịch sử thao tác"));
                InspectorPanel.Children.Add(Row("Trong 24 giờ", _auditService is null ? "Chưa cấu hình audit service" : $"{_auditEntryCount:N0} bản ghi"));
                InspectorPanel.Children.Add(Row("Nguồn", _auditService is null ? "Fail-closed" : "AuditService.QueryAsync"));
                break;
            default:
                InspectorPanel.Children.Add(STitle("Tổng quan ca"));
                InspectorPanel.Children.Add(Row("Ca hiện tại", _activeShift?.ShiftNumber ?? "Chưa có ca mở"));
                InspectorPanel.Children.Add(Row("Operator", _activeShift?.CashierName ?? "Chưa có dữ liệu"));
                InspectorPanel.Children.Add(Row("Ngày KD", _executionContext?.Current?.BusinessDate.BusinessDate.ToString("dd/MM/yyyy") ?? "Chưa có dữ liệu"));
                InspectorPanel.Children.Add(new Border { Height = 1, Background = Cb("#E2E8F0"), Margin = new Thickness(0, 6, 0, 6) });
                InspectorPanel.Children.Add(Row("Khách", _guests.Count.ToString("N0")));
                InspectorPanel.Children.Add(Row("Pending task", _tasks.Count.ToString("N0")));
                InspectorPanel.Children.Add(Row("Incident", _incidentQuery is null ? "Chưa cấu hình query" : $"{_incidentCount:N0}"));
                break;
        }
    }

    private void AddInspectorWarning(string title, string detail, string status, string foreground, string background)
    {
        InspectorPanel.Children.Add(new Border
        {
            Background = Cb(background),
            CornerRadius = new CornerRadius(6),
            Padding = new Thickness(8, 6, 8, 6),
            Margin = new Thickness(0, 0, 0, 5),
            Child = new StackPanel
            {
                Children =
                {
                    new TextBlock { Text = title, FontSize = 10, FontWeight = FontWeights.SemiBold, Foreground = C(foreground) },
                    new TextBlock { Text = detail, FontSize = 10, Foreground = C("#475569"), Margin = new Thickness(0, 2, 0, 0), TextWrapping = TextWrapping.Wrap },
                    new TextBlock { Text = status, FontSize = 9, Foreground = C(foreground), Margin = new Thickness(0, 2, 0, 0) }
                }
            }
        });
    }

    // ═══ ACTIONS ═══

    private async void OnConfirmHandover(object s, RoutedEventArgs e)
    {
        if (_activeShift is null || _cashShiftService is null)
        {
            MessageBox.Show("Không có ca mở trong dữ liệu thật để bàn giao.", "Bàn giao ca", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        if (_targetCashierBox?.SelectedItem is not CashierOptionDto target || target.UserId == _activeShift.CashierId)
        {
            MessageBox.Show("Vui lòng chọn một người nhận khác ca nguồn.", "Bàn giao ca", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        if (!TryParseAmount(_countedCashBox?.Text, out var countedCash) || countedCash < 0)
        {
            MessageBox.Show("Tiền kiểm đếm không hợp lệ.", "Bàn giao ca", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        if (_checklist.Count == 0 || _checklist.Any(item => !item.Value))
        {
            MessageBox.Show("Vui lòng hoàn tất checklist bàn giao trước khi xác nhận.", "Bàn giao ca", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        try
        {
            var result = await _cashShiftService.HandOverAsync(_activeShift.ShiftId, target.UserId, countedCash, BuildHandoverNotes(), CancellationToken.None);
            MessageBox.Show($"Đã bàn giao {result.HandoverAmount:N0}. Chênh lệch: {result.Variance:N0}.", "Bàn giao ca", MessageBoxButton.OK, MessageBoxImage.Information);
            await OnLoadedAsyncCore();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Không thể ghi bàn giao ca: {ex.Message}", "Bàn giao ca", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private string? BuildHandoverNotes()
    {
        var sections = new List<string>();
        AddHandoverNoteSection(sections, "Ghi chú chung", _handoverNotesBox?.Text);
        AddHandoverNoteSection(sections, "Vấn đề quan trọng", _handoverIssuesBox?.Text);
        AddHandoverNoteSection(sections, "Hành động ca sau", _handoverNextActionsBox?.Text);

        if (_checklist.Count > 0)
        {
            var completed = _checklist.Count(x => x.Value);
            sections.Add($"Checklist bàn giao: {completed}/{_checklist.Count} mục đã xác nhận.");
        }

        return sections.Count == 0 ? null : string.Join(Environment.NewLine + Environment.NewLine, sections);
    }

    private static void AddHandoverNoteSection(List<string> sections, string title, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
            sections.Add($"{title}:\n{value.Trim()}");
    }

    private void OnRefresh(object s, RoutedEventArgs e) => OnLoadedAsync(this, e);
    private void OnExport(object s, RoutedEventArgs e)
    {
        if (_dataError is not null)
        {
            Show(_dataError);
            return;
        }

        var dialog = new SaveFileDialog
        {
            Title = "Xuất báo cáo bàn giao ca",
            Filter = "CSV UTF-8 (*.csv)|*.csv|Tất cả tệp (*.*)|*.*",
            FileName = $"ban-giao-ca-{DateTime.Now:yyyyMMdd-HHmmss}.csv",
            AddExtension = true,
            DefaultExt = ".csv",
            OverwritePrompt = true
        };
        if (dialog.ShowDialog(this) != true) return;

        try
        {
            var csv = new StringBuilder();
            csv.AppendLine("Nhóm,Mã,Chi tiết,Phòng,Trạng thái,Ưu tiên,Người phụ trách");
            foreach (var guest in _guests)
                csv.AppendLine(string.Join(",", Csv("Khách"), Csv(guest.Reservation), Csv(guest.Name), Csv(guest.Room), Csv(guest.Status), Csv(guest.Balance?.ToString("N0")), Csv(guest.Remark)));
            foreach (var task in _tasks)
                csv.AppendLine(string.Join(",", Csv("Housekeeping"), Csv(task.Code), Csv(task.Category), Csv(task.Description), Csv(task.Status), Csv(task.Priority), Csv(task.Owner)));
            csv.AppendLine(string.Join(",", Csv("Tổng hợp"), Csv(_activeShift?.ShiftNumber), Csv($"{_incidentCount:N0} sự cố; {_checklist.Count(x => x.Value)}/{_checklist.Count} mục đã kiểm tra"), Csv(_activeShift?.CashierName), Csv(_activeShift is null ? "Không có ca mở" : "Đang mở"), Csv(_activeShift?.ExpectedCash.ToString("N0")), Csv(string.Empty)));
            File.WriteAllText(dialog.FileName, csv.ToString(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
            Show($"Đã xuất dữ liệu bàn giao ca thật: {dialog.FileName}");
        }
        catch (Exception ex)
        {
            Show("Không thể xuất báo cáo bàn giao ca thật: " + ex.Message);
        }
    }

    private void OnPrint(object s, RoutedEventArgs e)
    {
        if (_dataError is not null)
        {
            Show(_dataError);
            return;
        }

        var content = new StringBuilder()
            .AppendLine("BÁO CÁO BÀN GIAO CA")
            .AppendLine($"Thời điểm: {DateTime.Now:dd/MM/yyyy HH:mm:ss}")
            .AppendLine($"Ca: {_activeShift?.ShiftNumber ?? "Không có ca mở"}")
            .AppendLine($"Thu ngân: {_activeShift?.CashierName ?? "-"}")
            .AppendLine($"Khách theo dõi: {_guests.Count:N0}; Housekeeping tồn: {_tasks.Count:N0}; Sự cố: {_incidentCount:N0}")
            .AppendLine($"Checklist: {_checklist.Count(x => x.Value)}/{_checklist.Count}")
            .AppendLine(new string('-', 100));
        foreach (var guest in _guests)
            content.AppendLine($"KHÁCH | {guest.Reservation} | {guest.Name} | Phòng {guest.Room} | {guest.Status} | Dư nợ: {guest.Balance}");
        foreach (var task in _tasks)
            content.AppendLine($"HK | {task.Code} | {task.Category} | Phòng {task.Description} | {task.Status} | {task.Priority} | {task.Owner}");

        new PrintPreviewDialog("Báo cáo bàn giao ca", content.ToString()).ShowDialog();
    }

    private static string Csv(string? value)
    {
        var text = value ?? string.Empty;
        return $"\"{text.Replace("\"", "\"\"")}\"";
    }

    private static bool IsOperationalStatus(string? status, params string[] expected)
    {
        if (string.IsNullOrWhiteSpace(status)) return false;
        var normalized = status.Replace("_", string.Empty, StringComparison.Ordinal)
            .Replace("-", string.Empty, StringComparison.Ordinal)
            .Replace(" ", string.Empty, StringComparison.Ordinal)
            .ToLowerInvariant();
        return expected.Any(x => normalized == x.Replace(" ", string.Empty, StringComparison.Ordinal).ToLowerInvariant());
    }

    private void OnClose(object s, RoutedEventArgs e) => Close();
    private static void Show(string a) => MessageBox.Show(a, "Bàn giao ca", MessageBoxButton.OK, MessageBoxImage.Information);

    private static bool TryParseAmount(string? value, out decimal amount)
        => decimal.TryParse(value, NumberStyles.Number, CultureInfo.CurrentCulture, out amount) || decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out amount);

    private static TextBlock STitle(string t) => new() { Text = t, FontSize = 13, FontWeight = FontWeights.SemiBold, Foreground = C("#102E5A"), Margin = new Thickness(0, 0, 0, 6) };
    private static TextBlock Row(string l, string v) { var tb = new TextBlock { FontSize = 12, Foreground = C("#6F8198"), Margin = new Thickness(0, 2, 0, 0) }; tb.WithRunsSh(new[] { (C("#94A3B8"), l + ": ", false), (C("#17365D"), v, true) }); return tb; }
    private static SolidColorBrush C(string h) => new((Color)ColorConverter.ConvertFromString(h));
    private static SolidColorBrush Cb(string h) => new((Color)ColorConverter.ConvertFromString(h));
}

internal record ShGuest(Guid Id, string Name, string Room, string Reservation, string Arrival, string Departure, string Status, bool IsVip, int? Balance, string? Remark);
internal record ShTask(Guid Id, string Code, string Category, string Description, string Priority, string Status, string Owner);
internal static class ShExt { public static TextBlock WithRunsSh(this TextBlock tb, (SolidColorBrush, string, bool)[] runs) { foreach (var (b, t, w) in runs) tb.Inlines.Add(new System.Windows.Documents.Run(t) { Foreground = b, FontWeight = w ? FontWeights.SemiBold : FontWeights.Normal }); return tb; } }
